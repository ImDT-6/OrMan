using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;
using Microsoft.EntityFrameworkCore;

namespace OrMan.ViewModels.Admin
{
    public class BepOrderItem
    {
        public int SoBan { get; set; }
        public ChiTietHoaDon ChiTiet { get; set; }
        public string TenMonGoc { get; set; }
        public string BadgeCapDo { get; set; }
        public bool CoCapDo => !string.IsNullOrEmpty(BadgeCapDo);
        public string ThoiGianChoHienThi { get; set; }
        public string MauSacCanhBao { get; set; }
        public int TrangThai { get; set; }
    }

    public class BepViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer;
        private string _searchText = "";
        private bool _isHistoryMode = false;
        private ObservableCollection<BepOrderItem> _danhSachHienThi;

        public ObservableCollection<BepOrderItem> DanhSachHienThi
        {
            get => _danhSachHienThi;
            set { _danhSachHienThi = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); LoadDataAsync(); }
        }

        public bool IsHistoryMode
        {
            get => _isHistoryMode;
            set { _isHistoryMode = value; OnPropertyChanged(); LoadDataAsync(); }
        }

        public ICommand XongMonCommand { get; private set; }
        public ICommand HoanTacCommand { get; private set; }
        public ICommand XongTatCaCommand { get; private set; } // Lệnh xử lý nhanh

        public BepViewModel()
        {
            XongMonCommand = new RelayCommand<BepOrderItem>(XongMon);
            HoanTacCommand = new RelayCommand<BepOrderItem>(HoanTac);
            XongTatCaCommand = new RelayCommand<object>(XongTatCa);

            LoadDataAsync();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _timer.Tick += (s, e) => LoadDataAsync();
            _timer.Start();
        }

        private async void LoadDataAsync()
        {
            try
            {
                var data = await Task.Run(() =>
                {
                    using (var context = new MenuContext())
                    {
                        int targetStatus = IsHistoryMode ? 1 : 0;
                        var today = DateTime.Today; // Mốc thời gian hôm nay

                        var query = from ct in context.ChiTietHoaDons.Include(x => x.MonAn)
                                    join hd in context.HoaDons on ct.MaHoaDon equals hd.MaHoaDon
                                    // [QUAN TRỌNG] Chỉ lấy đơn từ hôm nay trở đi để tránh lag và đơn cũ
                                    where ct.TrangThaiCheBien == targetStatus && hd.NgayTao >= today
                                    select new { ct, hd };

                        if (!string.IsNullOrEmpty(SearchText))
                        {
                            string s = SearchText.ToLower();
                            query = query.Where(x => x.ct.TenMonHienThi.ToLower().Contains(s) || x.hd.SoBan.ToString().Contains(s));
                        }

                        if (IsHistoryMode)
                            query = query.OrderByDescending(x => x.ct.Id); // Lịch sử: Mới nhất lên đầu
                        else
                            query = query.OrderBy(x => x.hd.NgayTao); // Đang chờ: Cũ nhất làm trước

                        return query.ToList().Select(item => {
                            var thoiGian = item.ct.ThoiGianGoiMon ?? item.hd.NgayTao;
                            var phutCho = (DateTime.Now - thoiGian).TotalMinutes;

                            string tenFull = item.ct.TenMonHienThi ?? "";
                            string tenGoc = tenFull;
                            string badge = null;
                            int idx = tenFull.LastIndexOf("(Cấp");
                            if (idx > 0)
                            {
                                tenGoc = tenFull.Substring(0, idx).Trim();
                                badge = tenFull.Substring(idx).Replace("(", "").Replace(")", "");
                            }

                            return new BepOrderItem
                            {
                                SoBan = item.hd.SoBan,
                                ChiTiet = item.ct,
                                TenMonGoc = tenGoc,
                                BadgeCapDo = badge,
                                ThoiGianChoHienThi = IsHistoryMode ? $"{item.hd.NgayTao:HH:mm}" : $"{(int)phutCho} phút",
                                MauSacCanhBao = IsHistoryMode ? "#475569" : (phutCho > 20 ? "#EF4444" : (phutCho > 10 ? "#F59E0B" : "#22C55E")),
                                TrangThai = item.ct.TrangThaiCheBien
                            };
                        }).ToList();
                    }
                });

                Application.Current.Dispatcher.Invoke(() => DanhSachHienThi = new ObservableCollection<BepOrderItem>(data));
            }
            catch { }
        }

        private void XongMon(BepOrderItem item) => UpdateTrangThai(item, 1, -1);
        private void HoanTac(BepOrderItem item) => UpdateTrangThai(item, 0, 1);

        // Hàm dọn dẹp nhanh các đơn đang hiển thị
        private void XongTatCa(object obj)
        {
            if (DanhSachHienThi == null || DanhSachHienThi.Count == 0 || IsHistoryMode) return;

            Task.Run(() => {
                try
                {
                    using (var context = new MenuContext())
                    {
                        var ids = DanhSachHienThi.Select(x => x.ChiTiet.Id).ToList();
                        var details = context.ChiTietHoaDons.Where(x => ids.Contains(x.Id)).ToList();

                        foreach (var ct in details)
                        {
                            ct.TrangThaiCheBien = 1;
                            // Logic trừ kho (nếu cần xử lý hàng loạt)
                            var congThucs = context.CongThucs.Where(x => x.MaMon == ct.MaMon).ToList();
                            foreach (var f in congThucs)
                            {
                                var nl = context.NguyenLieus.Find(f.NguyenLieuId);
                                if (nl != null) nl.SoLuongTon -= (f.SoLuongCan * ct.SoLuong);
                            }
                        }
                        context.SaveChanges();
                        Application.Current.Dispatcher.Invoke(() => LoadDataAsync());
                    }
                }
                catch { }
            });
        }

        private void UpdateTrangThai(BepOrderItem item, int status, int inventoryMultiplier)
        {
            if (item?.ChiTiet == null) return;
            Task.Run(() => {
                try
                {
                    using (var context = new MenuContext())
                    {
                        var ct = context.ChiTietHoaDons.Find(item.ChiTiet.Id);
                        if (ct != null)
                        {
                            ct.TrangThaiCheBien = status;
                            var congThucs = context.CongThucs.Where(x => x.MaMon == ct.MaMon).ToList();
                            foreach (var f in congThucs)
                            {
                                var nl = context.NguyenLieus.Find(f.NguyenLieuId);
                                if (nl != null) nl.SoLuongTon += (f.SoLuongCan * ct.SoLuong * inventoryMultiplier);
                            }
                            context.SaveChanges();
                            Application.Current.Dispatcher.Invoke(() => LoadDataAsync());
                        }
                    }
                }
                catch { }
            });
        }

        public void Cleanup() { _timer?.Stop(); }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
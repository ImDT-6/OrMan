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
using System.Collections.Generic;
using System.Diagnostics;

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
            set { _searchText = value; OnPropertyChanged(); DebouncedLoad(); }
        }

        public bool IsHistoryMode
        {
            get => _isHistoryMode;
            set { _isHistoryMode = value; OnPropertyChanged(); LoadDataAsync(); }
        }

        public ICommand XongMonCommand { get; private set; }
        public ICommand HoanTacCommand { get; private set; }
        public ICommand XongTatCaCommand { get; private set; }

        private readonly object _loadLock = new object();
        private DateTime _lastRequested = DateTime.MinValue;

        public BepViewModel()
        {
            XongMonCommand = new RelayCommand<BepOrderItem>(XongMon);
            HoanTacCommand = new RelayCommand<BepOrderItem>(HoanTac);
            XongTatCaCommand = new RelayCommand<object>(XongTatCa);

            DanhSachHienThi = new ObservableCollection<BepOrderItem>();

            LoadDataAsync();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _timer.Tick += (s, e) => LoadDataAsync();
            _timer.Start();
        }

        // Debounce quick typing changes
        private async void DebouncedLoad()
        {
            lock (_loadLock)
            {
                _lastRequested = DateTime.Now;
            }

            await Task.Delay(300);
            DateTime t;
            lock (_loadLock) { t = _lastRequested; }
            if ((DateTime.Now - t).TotalMilliseconds >= 300)
            {
                LoadDataAsync();
            }
        }

        private async void LoadDataAsync()
        {
            try
            {
                var st = SearchText; // local copy
                var data = await Task.Run(() =>
                {
                    using (var context = new MenuContext())
                    {
                        int targetStatus = IsHistoryMode ? 1 : 0;
                        var today = DateTime.Today;

                        var q = from ct in context.ChiTietHoaDons.Include(x => x.MonAn)
                                join hd in context.HoaDons on ct.MaHoaDon equals hd.MaHoaDon
                                where ct.TrangThaiCheBien == targetStatus && hd.NgayTao >= today
                                select new { ct, hd };

                        if (!string.IsNullOrEmpty(st))
                        {
                            string sLower = st.ToLower();
                            q = q.Where(x => x.ct.TenMonHienThi.ToLower().Contains(sLower) || x.hd.SoBan.ToString().Contains(sLower));
                        }

                        q = IsHistoryMode ? q.OrderByDescending(x => x.ct.Id) : q.OrderBy(x => x.hd.NgayTao);

                        var list = q.AsNoTracking().ToList();

                        var now = DateTime.Now;
                        var result = new List<BepOrderItem>(list.Count);
                        foreach (var item in list)
                        {
                            var thoiGian = item.ct.ThoiGianGoiMon ?? item.hd.NgayTao;
                            var phutCho = (now - thoiGian).TotalMinutes;

                            string tenFull = item.ct.TenMonHienThi ?? "";
                            string tenGoc = tenFull;
                            string badge = null;
                            int idx = tenFull.LastIndexOf("(Cấp");
                            if (idx > 0)
                            {
                                tenGoc = tenFull.Substring(0, idx).Trim();
                                badge = tenFull.Substring(idx).Replace("(", "").Replace(")", "");
                            }

                            result.Add(new BepOrderItem
                            {
                                SoBan = item.hd.SoBan,
                                ChiTiet = item.ct,
                                TenMonGoc = tenGoc,
                                BadgeCapDo = badge,
                                ThoiGianChoHienThi = IsHistoryMode ? $"{item.hd.NgayTao:HH:mm}" : $"{(int)phutCho} phút",
                                MauSacCanhBao = IsHistoryMode ? "#475569" : (phutCho > 20 ? "#EF4444" : (phutCho > 10 ? "#F59E0B" : "#22C55E")),
                                TrangThai = item.ct.TrangThaiCheBien
                            });
                        }

                        return result;
                    }
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    DanhSachHienThi.Clear();
                    foreach (var it in data) DanhSachHienThi.Add(it);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadDataAsync error: " + ex.Message);
            }
        }

        private void XongMon(BepOrderItem item) => UpdateTrangThai(item, 1, -1);
        private void HoanTac(BepOrderItem item) => UpdateTrangThai(item, 0, 1);

        private void XongTatCa(object obj)
        {
            if (DanhSachHienThi == null || DanhSachHienThi.Count == 0 || IsHistoryMode) return;

            Task.Run(() =>
            {
                try
                {
                    using (var context = new MenuContext())
                    {
                        var ids = DanhSachHienThi.Select(x => x.ChiTiet.Id).ToList();
                        var details = context.ChiTietHoaDons.Where(x => ids.Contains(x.Id)).ToList();

                        // preload all công thức cho các món trong details
                        var maMons = details.Select(d => d.MaMon).Distinct().ToList();
                        var congThucs = context.CongThucs.Where(c => maMons.Contains(c.MaMon)).ToList();
                        var nguyenIds = congThucs.Select(c => c.NguyenLieuId).Distinct().ToList();
                        var nguyenDict = context.NguyenLieus.Where(n => nguyenIds.Contains(n.Id)).ToDictionary(n => n.Id);

                        foreach (var ct in details)
                        {
                            ct.TrangThaiCheBien = 1;
                            var congThucsFor = congThucs.Where(c => c.MaMon == ct.MaMon);
                            foreach (var f in congThucsFor)
                            {
                                if (nguyenDict.TryGetValue(f.NguyenLieuId, out var nl))
                                {
                                    nl.SoLuongTon -= (f.SoLuongCan * ct.SoLuong);
                                }
                            }
                        }

                        context.SaveChanges();
                        Application.Current.Dispatcher.Invoke(() => LoadDataAsync());
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("XongTatCa error: " + ex.Message);
                }
            });
        }

        private void UpdateTrangThai(BepOrderItem item, int status, int inventoryMultiplier)
        {
            if (item?.ChiTiet == null) return;
            Task.Run(() =>
            {
                try
                {
                    using (var context = new MenuContext())
                    {
                        var ct = context.ChiTietHoaDons.Find(item.ChiTiet.Id);
                        if (ct != null)
                        {
                            ct.TrangThaiCheBien = status;

                            var congThucs = context.CongThucs.Where(x => x.MaMon == ct.MaMon).ToList();
                            var nguyenIds = congThucs.Select(x => x.NguyenLieuId).Distinct().ToList();
                            var nguyenDict = context.NguyenLieus.Where(n => nguyenIds.Contains(n.Id)).ToDictionary(n => n.Id);

                            foreach (var f in congThucs)
                            {
                                if (nguyenDict.TryGetValue(f.NguyenLieuId, out var nl))
                                {
                                    nl.SoLuongTon += (f.SoLuongCan * ct.SoLuong * inventoryMultiplier);
                                }
                            }

                            context.SaveChanges();
                            Application.Current.Dispatcher.Invoke(() => LoadDataAsync());
                        }
                    }
                }                               
                catch (Exception ex)
                {
                    Debug.WriteLine("UpdateTrangThai error: " + ex.Message);
                }
            });
        }

        public void Cleanup() { _timer?.Stop(); }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
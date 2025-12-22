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
using Microsoft.EntityFrameworkCore; // Cần để dùng .Include

namespace OrMan.ViewModels.Admin
{
    // [CẢI TIẾN] Class chứa dữ liệu đã được xử lý sẵn cho UI
    public class BepOrderItem
    {
        public int SoBan { get; set; }
        public ChiTietHoaDon ChiTiet { get; set; }

        // 1. Tên món đã tách bỏ phần "(Cấp ...)"
        public string TenMonGoc { get; set; }

        // 2. Phần cấp độ cay tách riêng (nếu có)
        public string BadgeCapDo { get; set; }
        public bool CoCapDo => !string.IsNullOrEmpty(BadgeCapDo);

        // 3. Thời gian chờ (Ví dụ: "5 phút", "12 phút")
        public string ThoiGianChoHienThi { get; set; }

        // 4. Màu sắc cảnh báo (Green -> Yellow -> Red)
        public string MauSacCanhBao { get; set; }
    }

    public class BepViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer;

        private ObservableCollection<BepOrderItem> _danhSachCanLam;
        public ObservableCollection<BepOrderItem> DanhSachCanLam
        {
            get => _danhSachCanLam;
            set { _danhSachCanLam = value; OnPropertyChanged(); }
        }

        public ICommand XongMonCommand { get; private set; }

        public BepViewModel()
        {
            XongMonCommand = new RelayCommand<BepOrderItem>(XongMon);

            LoadDataAsync();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(10); // 10s cập nhật 1 lần
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
                        // Lấy dữ liệu thô
                        var query = from ct in context.ChiTietHoaDons.Include("MonAn")
                                    join hd in context.HoaDons on ct.MaHoaDon equals hd.MaHoaDon
                                    where !hd.DaThanhToan && ct.TrangThaiCheBien == 0
                                    orderby hd.NgayTao
                                    select new { ct, hd };

                        var rawList = query.ToList();

                        // [CẢI TIẾN UX] Xử lý dữ liệu ngay tại đây để View chỉ việc hiện
                        return rawList.Select(item => {
                            // 1. Tính thời gian chờ
                            var thoiGianGoi = item.ct.ThoiGianGoiMon ?? item.hd.NgayTao;
                            var phutCho = (DateTime.Now - thoiGianGoi).TotalMinutes;

                            // 2. Xác định màu sắc khẩn cấp
                            string mauSac = "#22C55E"; // Xanh (Mặc định)
                            if (phutCho > 20) mauSac = "#EF4444";      // Đỏ (Khẩn cấp)
                            else if (phutCho > 10) mauSac = "#F59E0B"; // Vàng (Cảnh báo)

                            // 3. Tách Cấp độ cay từ tên món
                            // Giả sử tên là "Mỳ Cay (Cấp 1)" -> Tách thành "Mỳ Cay" và "Cấp 1"
                            string tenDayDu = item.ct.TenMonHienThi ?? "";
                            string tenGoc = tenDayDu;
                            string badge = null;

                            int indexMoNgoac = tenDayDu.LastIndexOf("(Cấp");
                            if (indexMoNgoac > 0)
                            {
                                tenGoc = tenDayDu.Substring(0, indexMoNgoac).Trim();
                                badge = tenDayDu.Substring(indexMoNgoac).Replace("(", "").Replace(")", ""); // Lấy chữ "Cấp X"
                            }

                            return new BepOrderItem
                            {
                                SoBan = item.hd.SoBan,
                                ChiTiet = item.ct,
                                TenMonGoc = tenGoc,
                                BadgeCapDo = badge,
                                ThoiGianChoHienThi = $"{(int)phutCho} phút",
                                MauSacCanhBao = mauSac
                            };
                        }).ToList();
                    }
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    DanhSachCanLam = new ObservableCollection<BepOrderItem>(data);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi LoadData Bếp: " + ex.Message);
            }
        }

        private void XongMon(BepOrderItem item)
        {
            if (item == null || item.ChiTiet == null) return;
            // (Logic XongMon giữ nguyên như cũ, chỉ cần đổi tham số đầu vào là BepOrderItem)
            // ... Bạn copy lại phần logic trừ kho đã sửa ở câu trả lời trước vào đây ...
            Task.Run(() =>
            {
                try
                {
                    using (var context = new MenuContext())
                    {
                        var ct = context.ChiTietHoaDons.Find(item.ChiTiet.Id);
                        if (ct != null && ct.TrangThaiCheBien == 0)
                        {
                            ct.TrangThaiCheBien = 1;

                            // --- LOGIC TRỪ KHO ---
                            var congThucs = context.CongThucs.Where(x => x.MaMon == ct.MaMon).ToList();
                            foreach (var congThuc in congThucs)
                            {
                                var nguyenLieu = context.NguyenLieus.Find(congThuc.NguyenLieuId);
                                if (nguyenLieu != null)
                                {
                                    double soLuongTru = congThuc.SoLuongCan * ct.SoLuong;
                                    nguyenLieu.SoLuongTon -= soLuongTru;
                                }
                            }
                            // ---------------------

                            context.SaveChanges();
                            Application.Current.Dispatcher.Invoke(() => LoadDataAsync());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show("Lỗi: " + ex.Message));
                }
            });
        }

        public void Cleanup()
        {
            if (_timer != null) { _timer.Stop(); _timer = null; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
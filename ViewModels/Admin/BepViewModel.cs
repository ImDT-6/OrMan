using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;

// 1. Đổi Namespace cho khớp với Project hiện tại
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;

namespace OrMan.ViewModels.Admin
{
    // Class đại diện cho 1 món ăn hiển thị trên màn hình bếp
    public class BepOrderItem
    {
        public int SoBan { get; set; }
        public string ThoiGianOrder { get; set; }

        // Giữ nguyên object ChiTietHoaDon từ Database
        public ChiTietHoaDon ChiTiet { get; set; }
    }

    public class BepViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer;

        // Collection để Binding lên ItemsControl bên XAML
        private ObservableCollection<BepOrderItem> _danhSachCanLam;
        public ObservableCollection<BepOrderItem> DanhSachCanLam
        {
            get => _danhSachCanLam;
            set { _danhSachCanLam = value; OnPropertyChanged(); }
        }

        public ICommand XongMonCommand { get; private set; }

        public BepViewModel()
        {
            // Kết nối lệnh "Đã Xong" với hàm xử lý
            XongMonCommand = new RelayCommand<BepOrderItem>(XongMon);

            LoadData();

            // Setup Timer để tự động refresh dữ liệu mỗi 10 giây (Real-time giả lập)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(10);
            _timer.Tick += (s, e) => LoadData();
            _timer.Start();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new MenuContext())
                {
                    // Query lấy các món chưa chế biến (TrangThaiCheBien == 0) của các hóa đơn chưa thanh toán
                    var query = from ct in context.ChiTietHoaDons
                                join hd in context.HoaDons on ct.MaHoaDon equals hd.MaHoaDon
                                where !hd.DaThanhToan && ct.TrangThaiCheBien == 0
                                orderby hd.NgayTao
                                select new BepOrderItem
                                {
                                    SoBan = hd.SoBan,
                                    ThoiGianOrder = hd.NgayTao.ToString("HH:mm"),
                                    ChiTiet = ct
                                };

                    // Cập nhật lên giao diện
                    DanhSachCanLam = new ObservableCollection<BepOrderItem>(query.ToList());
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi kết nối database nếu có (tránh crash app)
                System.Diagnostics.Debug.WriteLine("Lỗi LoadData Bếp: " + ex.Message);
            }
        }

        // Thay thế nội dung hàm XongMon bằng đoạn này:
        private void XongMon(BepOrderItem item)
        {
            if (item == null || item.ChiTiet == null) return;

            try
            {
                using (var context = new MenuContext())
                {
                    var ct = context.ChiTietHoaDons.Find(item.ChiTiet.Id);
                    if (ct != null)
                    {
                        // 1. Cập nhật trạng thái
                        ct.TrangThaiCheBien = 1;

                        // 2. [MỚI] TRỪ KHO NGAY TẠI ĐÂY
                        // Tìm công thức của món này
                        var congThucs = context.CongThucs.Where(x => x.MaMon == ct.MaMon).ToList();
                        foreach (var congThuc in congThucs)
                        {
                            var nguyenLieu = context.NguyenLieus.Find(congThuc.NguyenLieuId);
                            if (nguyenLieu != null)
                            {
                                // Trừ tồn kho: Định lượng * Số lượng món
                                double soLuongTru = congThuc.SoLuongCan * ct.SoLuong;
                                nguyenLieu.SoLuongTon -= soLuongTru;
                            }
                        }

                        context.SaveChanges();
                    }
                }
                LoadData();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi cập nhật: " + ex.Message);
            }
        }

        // Implementation INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
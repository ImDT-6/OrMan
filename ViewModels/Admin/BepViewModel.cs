using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Cần thiết cho Task.Run
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;

namespace OrMan.ViewModels.Admin
{
    public class BepOrderItem
    {
        public int SoBan { get; set; }
        public string ThoiGianOrder { get; set; }
        public ChiTietHoaDon ChiTiet { get; set; }
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

            // [TỐI ƯU] Không gọi LoadData() trực tiếp để tránh đơ lúc khởi tạo
            LoadDataAsync();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(10); // Cập nhật mỗi 10s
            _timer.Tick += (s, e) => LoadDataAsync();
            _timer.Start();
        }

        // [QUAN TRỌNG] Chuyển sang xử lý bất đồng bộ
        private async void LoadDataAsync()
        {
            try
            {
                // 1. Chạy query nặng ở luồng phụ (Background Thread)
                var data = await Task.Run(() =>
                {
                    using (var context = new MenuContext())
                    {
                        var query = from ct in context.ChiTietHoaDons
                                    join hd in context.HoaDons on ct.MaHoaDon equals hd.MaHoaDon
                                    where !hd.DaThanhToan && ct.TrangThaiCheBien == 0
                                    orderby hd.NgayTao
                                    select new BepOrderItem
                                    {
                                        SoBan = hd.SoBan,
                                        ThoiGianOrder = (ct.ThoiGianGoiMon ?? hd.NgayTao).ToString("HH:mm"),
                                        ChiTiet = ct
                                    };
                        // ToList() để thực thi câu lệnh SQL ngay tại đây
                        return query.ToList();
                    }
                });

                // 2. Cập nhật UI ở luồng chính (Main Thread)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Tạo ObservableCollection mới từ danh sách đã lấy
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

            // Xử lý logic cập nhật trạng thái (Nhanh nên có thể để ở UI Thread hoặc chuyển Async nếu muốn)
            Task.Run(() =>
            {
                try
                {
                    using (var context = new MenuContext())
                    {
                        var ct = context.ChiTietHoaDons.Find(item.ChiTiet.Id);
                        if (ct != null)
                        {
                            ct.TrangThaiCheBien = 1;

                            // Trừ kho
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
                            context.SaveChanges();
                        }
                    }

                    // Sau khi lưu xong DB thì load lại dữ liệu
                    Application.Current.Dispatcher.Invoke(() => LoadDataAsync());
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show("Lỗi cập nhật: " + ex.Message));
                }
            });
        }

        // [QUAN TRỌNG] Hàm này được gọi từ View khi Unloaded
        public void Cleanup()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
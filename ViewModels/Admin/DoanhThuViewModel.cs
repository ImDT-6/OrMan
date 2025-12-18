using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Cần thiết
using System.Windows;
using System.Windows.Input;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Services;

namespace OrMan.ViewModels
{
    public class DoanhThuViewModel : INotifyPropertyChanged
    {
        private readonly DoanhThuRepository _repository;
        private List<HoaDon> _allHoaDons; // Dữ liệu gốc (List nhẹ hơn ObservableCollection khi xử lý ngầm)

        // [MỚI] Biến lưu trữ khoảng thời gian tùy chọn
        private DateTime _customFromDate;
        private DateTime _customToDate;

        private ObservableCollection<HoaDon> _danhSachHoaDon;
        public ObservableCollection<HoaDon> DanhSachHoaDon
        {
            get => _danhSachHoaDon;
            set { _danhSachHoaDon = value; OnPropertyChanged(); }
        }

        private string _tuKhoaTimKiem;
        public string TuKhoaTimKiem
        {
            get => _tuKhoaTimKiem;
            set
            {
                _tuKhoaTimKiem = value;
                OnPropertyChanged();
                // Tìm kiếm thì chạy nhanh, không cần Async cũng được, hoặc Async nếu muốn mượt tuyệt đối
                Task.Run(() => FilterDataAsync());
            }
        }

        private string _selectedTimeFilter = "Hôm nay";
        public string SelectedTimeFilter
        {
            get => _selectedTimeFilter;
            set
            {
                if (_selectedTimeFilter != value)
                {
                    _selectedTimeFilter = value;
                    OnPropertyChanged();
                    if (value != "Tùy chọn")
                    {
                        Task.Run(() => FilterDataAsync());
                    }
                }
            }
        }

        private string _tongDoanhThuText;
        public string TongDoanhThuText { get => _tongDoanhThuText; set { _tongDoanhThuText = value; OnPropertyChanged(); } }

        private int _tongSoDon;
        public int TongSoDon { get => _tongSoDon; set { _tongSoDon = value; OnPropertyChanged(); } }

        private string _trungBinhDon;
        public string TrungBinhDon { get => _trungBinhDon; set { _trungBinhDon = value; OnPropertyChanged(); } }

        public DoanhThuViewModel()
        {
            _repository = new DoanhThuRepository();

            // [TỐI ƯU] Chạy ngầm ngay khi khởi tạo
            Task.Run(() => LoadDataAsync());

            // Sự kiện thanh toán thành công cũng reload ngầm
            BanAnRepository.OnPaymentSuccess += () => Task.Run(() => LoadDataAsync());
        }

        public void Cleanup()
        {
            // Hủy đăng ký sự kiện để tránh leak memory
            BanAnRepository.OnPaymentSuccess -= () => Task.Run(() => LoadDataAsync());
        }

        // 1. Load toàn bộ dữ liệu từ DB (Nặng nhất)
        private async Task LoadDataAsync()
        {
            try
            {
                var data = await Task.Run(() => _repository.GetAll().ToList()); // Chuyển sang List
                _allHoaDons = data;
                await FilterDataAsync(); // Sau khi có dữ liệu thì lọc
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi Load Doanh Thu: " + ex.Message);
            }
        }

        public void LocTheoKhoangThoiGian(DateTime from, DateTime to)
        {
            _selectedTimeFilter = "Tùy chọn";
            _customFromDate = from;
            _customToDate = to;

            Task.Run(() => FilterDataAsync());
        }

        // 2. Lọc và Tính toán (Cũng có thể nặng nếu list dài)
        private async Task FilterDataAsync()
        {
            if (_allHoaDons == null) return;

            await Task.Run(() =>
            {
                IEnumerable<HoaDon> query = _allHoaDons;
                DateTime startDate = DateTime.MinValue;
                DateTime endDate = DateTime.MaxValue;

                switch (_selectedTimeFilter)
                {
                    case "Hôm nay":
                        startDate = DateTime.Today;
                        endDate = DateTime.Today.AddDays(1).AddTicks(-1);
                        break;
                    case "Tuần này":
                        int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
                        startDate = DateTime.Today.AddDays(-1 * diff).Date;
                        endDate = DateTime.Now;
                        break;
                    case "Tháng này":
                        startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        endDate = startDate.AddMonths(1).AddTicks(-1);
                        break;
                    case "Tùy chọn":
                        startDate = _customFromDate;
                        endDate = _customToDate;
                        break;
                }

                if (_selectedTimeFilter != "Tất cả")
                {
                    query = query.Where(h => h.NgayTao >= startDate && h.NgayTao <= endDate);
                }

                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                {
                    string k = TuKhoaTimKiem.ToLower();
                    query = query.Where(x => x.MaHoaDon.ToLower().Contains(k) ||
                                             (x.NguoiTao != null && x.NguoiTao.ToLower().Contains(k)));
                }

                var resultList = new ObservableCollection<HoaDon>(query.OrderByDescending(h => h.NgayTao));

                // Tính toán thống kê
                decimal total = 0;
                int count = resultList.Count;
                if (count > 0) total = resultList.Sum(x => x.TongTien);
                decimal avg = count > 0 ? total / count : 0;

                // Cập nhật UI (Main Thread)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DanhSachHoaDon = resultList;
                    TongDoanhThuText = total.ToString("N0") + " VNĐ";
                    TongSoDon = count;
                    TrungBinhDon = avg.ToString("N0") + " VNĐ";
                });
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
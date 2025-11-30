using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using GymManagement.Models;
using GymManagement.Services;
using System.Collections.Generic;
using System.Windows.Input;
using GymManagement.Helpers;
using System.Windows;

namespace GymManagement.ViewModels
{
    // Class hỗ trợ hiển thị Top món
    public class TopFoodItem
    {
        public MonAn MonAn { get; set; }
        public int SoLuongBan { get; set; }
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DoanhThuRepository _doanhThuRepo;
        private readonly ThucDonRepository _thucDonRepo;
        private readonly BanAnRepository _banRepo;
        private DispatcherTimer _timer;

        // Dữ liệu biểu đồ
        public ObservableCollection<KeyValuePair<string, double>> DoanhThuTheoGio { get; set; }

        // [MỚI] Giá trị doanh thu lớn nhất trong ngày (để tính tỷ lệ chiều cao cột)
        private double _maxRevenue;
        public double MaxRevenue
        {
            get => _maxRevenue;
            set { _maxRevenue = value; OnPropertyChanged(); }
        }

        // [ĐÃ KHÔI PHỤC] Danh sách Top món bán chạy
        private ObservableCollection<TopFoodItem> _topMonAn;
        public ObservableCollection<TopFoodItem> TopMonAn
        {
            get => _topMonAn;
            set { _topMonAn = value; OnPropertyChanged(); }
        }

        // Danh sách Bàn cần xử lý (Yêu cầu thanh toán)
        private ObservableCollection<BanAn> _banCanXuLy;
        public ObservableCollection<BanAn> BanCanXuLy
        {
            get => _banCanXuLy;
            set { _banCanXuLy = value; OnPropertyChanged(); }
        }

        // Các chỉ số thống kê tổng quan
        private string _doanhThuNgay;
        public string DoanhThuNgay { get => _doanhThuNgay; set { _doanhThuNgay = value; OnPropertyChanged(); } }

        private string _tangTruongText;
        public string TangTruongText { get => _tangTruongText; set { _tangTruongText = value; OnPropertyChanged(); } }

        private int _soDonHomNay;
        public int SoDonHomNay { get => _soDonHomNay; set { _soDonHomNay = value; OnPropertyChanged(); } }

        private string _trungBinhDon;
        public string TrungBinhDon { get => _trungBinhDon; set { _trungBinhDon = value; OnPropertyChanged(); } }

        public ICommand ResolveRequestCommand { get; private set; }

        public DashboardViewModel()
        {
            _doanhThuRepo = new DoanhThuRepository();
            _thucDonRepo = new ThucDonRepository();
            _banRepo = new BanAnRepository();

            DoanhThuTheoGio = new ObservableCollection<KeyValuePair<string, double>>();
            ResolveRequestCommand = new RelayCommand<BanAn>(ResolveRequest);

            LoadDashboardData();

            // [MỚI] Đăng ký sự kiện: Khi có thanh toán -> Tự động Load lại dữ liệu ngay
            BanAnRepository.OnPaymentSuccess += () => LoadDashboardData();

            // Timer vẫn giữ để cập nhật giờ hệ thống hoặc reset qua ngày mới
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30); // Tăng lên 30s cho đỡ tốn tài nguyên vì đã có Event rồi
            _timer.Tick += (s, e) => LoadDashboardData();
            _timer.Start();
        }

        private void ResolveRequest(BanAn ban)
        {
            if (ban == null) return;
            if (MessageBox.Show($"Xác nhận đã xử lý yêu cầu tại {ban.TenBan}?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _banRepo.ResolvePaymentRequest(ban.SoBan);
                LoadDashboardData(); // Refresh ngay sau khi xử lý
            }
        }

        private void LoadDashboardData()
        {
            // 1. Load số liệu tổng quan (Doanh thu, Tăng trưởng, Số đơn)
            decimal totalToday = _doanhThuRepo.GetTodayRevenue();
            decimal totalYesterday = _doanhThuRepo.GetYesterdayRevenue();

            DoanhThuNgay = totalToday > 1000000 ? (totalToday / 1000000).ToString("0.##") + "M" : totalToday.ToString("N0");

            if (totalYesterday == 0)
                TangTruongText = totalToday > 0 ? "+100% (Mới)" : "---";
            else
            {
                double percent = (double)((totalToday - totalYesterday) / totalYesterday) * 100;
                TangTruongText = (percent >= 0 ? "+" : "") + percent.ToString("0") + "% so với hôm qua";
            }

            SoDonHomNay = _doanhThuRepo.GetTodayOrderCount();
            if (SoDonHomNay > 0)
            {
                decimal avg = totalToday / SoDonHomNay;
                TrungBinhDon = "TB: " + (avg > 1000 ? (avg / 1000).ToString("0") + "k" : avg.ToString("N0")) + "/đơn";
            }
            else
            {
                TrungBinhDon = "Chưa có đơn";
            }

            // 2. Load biểu đồ theo giờ (Real-time)
            LoadChartData();

            // 3. Load bàn cần xử lý (Lấy từ DB -> Lọc những bàn YêuCauThanhToan=True)
            var allTables = _banRepo.GetAll(); // Lưu ý: Hàm này dùng fresh context nên data luôn mới
            var urgentTables = allTables.Where(b => b.YeuCauThanhToan).ToList();

            // Chỉ cập nhật nếu số lượng thay đổi để tránh giật list (đơn giản hóa)
            if (BanCanXuLy == null || BanCanXuLy.Count != urgentTables.Count)
                BanCanXuLy = new ObservableCollection<BanAn>(urgentTables);

            // 4. [ĐÃ KHÔI PHỤC] Load Top món bán chạy (Lấy Top 5 món)
            var topDict = _thucDonRepo.GetTopSellingFoods(5);
            var topList = topDict.Select(x => new TopFoodItem { MonAn = x.Key, SoLuongBan = x.Value }).ToList();

            // Cập nhật lại danh sách Top món
            TopMonAn = new ObservableCollection<TopFoodItem>(topList);
        }

        private void LoadChartData()
        {
            // Lấy dữ liệu thật từ DB (Dictionary: Giờ -> Doanh thu)
            var hourlyData = _doanhThuRepo.GetRevenueByHour();

            DoanhThuTheoGio.Clear();

            double max = 1; // Mặc định là 1 để tránh lỗi chia cho 0

            // Hiển thị khung giờ từ 8h sáng đến 22h đêm
            for (int hour = 8; hour <= 22; hour++)
            {
                decimal revenue = 0;
                if (hourlyData.ContainsKey(hour))
                {
                    revenue = hourlyData[hour];
                }

                double val = (double)revenue;
                if (val > max) max = val; // Tìm giá trị lớn nhất trong ngày

                // Trục X là giờ (VD: 10:00), Trục Y là tiền (Double)
                DoanhThuTheoGio.Add(new KeyValuePair<string, double>($"{hour}:00", val));
            }

            // Cập nhật MaxRevenue để View dùng RatioConverter tính chiều cao cột
            MaxRevenue = max;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
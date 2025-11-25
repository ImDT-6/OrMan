using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using GymManagement.Models;
using GymManagement.Services;
using System.Collections.Generic;
using System.Windows.Input; // Cho ICommand
using GymManagement.Helpers; // Cho RelayCommand
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

        public ObservableCollection<KeyValuePair<string, double>> DoanhThuTheoGio { get; set; }

        // Thay đổi kiểu dữ liệu để dễ hiển thị số lượng
        private ObservableCollection<TopFoodItem> _topMonAn;
        public ObservableCollection<TopFoodItem> TopMonAn
        {
            get => _topMonAn;
            set { _topMonAn = value; OnPropertyChanged(); }
        }

        private ObservableCollection<BanAn> _banCanXuLy;
        public ObservableCollection<BanAn> BanCanXuLy
        {
            get => _banCanXuLy;
            set { _banCanXuLy = value; OnPropertyChanged(); }
        }

        private string _doanhThuNgay;
        public string DoanhThuNgay { get => _doanhThuNgay; set { _doanhThuNgay = value; OnPropertyChanged(); } }

        private string _tangTruongText;
        public string TangTruongText { get => _tangTruongText; set { _tangTruongText = value; OnPropertyChanged(); } }

        private int _soDonHomNay;
        public int SoDonHomNay { get => _soDonHomNay; set { _soDonHomNay = value; OnPropertyChanged(); } }

        private string _trungBinhDon;
        public string TrungBinhDon { get => _trungBinhDon; set { _trungBinhDon = value; OnPropertyChanged(); } }

        // [MỚI] Command xử lý yêu cầu
        public ICommand ResolveRequestCommand { get; private set; }

        public DashboardViewModel()
        {
            _doanhThuRepo = new DoanhThuRepository();
            _thucDonRepo = new ThucDonRepository();
            _banRepo = new BanAnRepository();

            ResolveRequestCommand = new RelayCommand<BanAn>(ResolveRequest);

            InitChartData();
            LoadDashboardData();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3); // Cập nhật mỗi 3 giây
            _timer.Tick += (s, e) => LoadDashboardData();
            _timer.Start();
        }

        private void ResolveRequest(BanAn ban)
        {
            if (ban == null) return;

            if (MessageBox.Show($"Xác nhận đã xử lý yêu cầu tại {ban.TenBan}?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _banRepo.ResolvePaymentRequest(ban.SoBan);
                LoadDashboardData(); // Refresh lại ngay
            }
        }

        private void InitChartData()
        {
            // Dữ liệu biểu đồ vẫn giả lập cho đẹp (vì chưa có đủ data lịch sử từng giờ)
            DoanhThuTheoGio = new ObservableCollection<KeyValuePair<string, double>>
            {
                new KeyValuePair<string, double>("10:00", 0),
                new KeyValuePair<string, double>("12:00", 500000),
                new KeyValuePair<string, double>("14:00", 200000),
                new KeyValuePair<string, double>("16:00", 800000),
                new KeyValuePair<string, double>("18:00", 1200000),
                new KeyValuePair<string, double>("20:00", 2500000)
            };
        }

        private void LoadDashboardData()
        {
            // 1. Doanh thu & Tăng trưởng thật
            decimal totalToday = _doanhThuRepo.GetTodayRevenue();
            decimal totalYesterday = _doanhThuRepo.GetYesterdayRevenue();

            DoanhThuNgay = totalToday > 1000000 ? (totalToday / 1000000).ToString("0.##") + "M" : totalToday.ToString("N0");

            if (totalYesterday == 0)
            {
                TangTruongText = totalToday > 0 ? "+100% (Mới)" : "0% so với hôm qua";
            }
            else
            {
                double percent = (double)((totalToday - totalYesterday) / totalYesterday) * 100;
                TangTruongText = (percent >= 0 ? "+" : "") + percent.ToString("0") + "% so với hôm qua";
            }

            // 2. Số đơn & Trung bình đơn thật
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

            // 3. Bàn cần xử lý thật (Lấy từ DB)
            var allTables = _banRepo.GetAll(); // Hàm này đã sửa dùng fresh context
            var urgentTables = allTables.Where(b => b.YeuCauThanhToan).ToList();

            // Mẹo: Chỉ cập nhật nếu số lượng thay đổi để tránh giật list
            if (BanCanXuLy == null || BanCanXuLy.Count != urgentTables.Count)
                BanCanXuLy = new ObservableCollection<BanAn>(urgentTables);

            // 4. Top món thật
            var topDict = _thucDonRepo.GetTopSellingFoods(5);
            var topList = topDict.Select(x => new TopFoodItem { MonAn = x.Key, SoLuongBan = x.Value }).ToList();
            TopMonAn = new ObservableCollection<TopFoodItem>(topList);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
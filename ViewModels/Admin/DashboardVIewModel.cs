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

    // Class hỗ trợ vẽ biểu đồ
    public class ChartBarItem
    {
        public string TimeLabel { get; set; }
        public double Value { get; set; }
        public double BarHeight { get; set; }
        public string TooltipText => $"{Value:N0} VNĐ";
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DoanhThuRepository _doanhThuRepo;
        private readonly ThucDonRepository _thucDonRepo;
        private readonly BanAnRepository _banRepo;
        private DispatcherTimer _timer;

        public ObservableCollection<ChartBarItem> DoanhThuTheoGio { get; set; }

        private string _axisMax = "2M";
        private string _axisMidHigh = "1.5M";
        private string _axisMid = "1M";
        private string _axisMidLow = "500k";

        public string AxisMax { get => _axisMax; set { _axisMax = value; OnPropertyChanged(); } }
        public string AxisMidHigh { get => _axisMidHigh; set { _axisMidHigh = value; OnPropertyChanged(); } }
        public string AxisMid { get => _axisMid; set { _axisMid = value; OnPropertyChanged(); } }
        public string AxisMidLow { get => _axisMidLow; set { _axisMidLow = value; OnPropertyChanged(); } }

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

        public ICommand ResolveRequestCommand { get; private set; }

        // Command này sẽ được Binding vào nút "Xử lý" trên Dashboard
        public ICommand ProcessRequestCommand { get; private set; }

        // Event để View (Code-behind) lắng nghe và thực hiện điều hướng nếu cần
        public event Action<BanAn> RequestNavigationToTable;

        public DashboardViewModel()
        {
            _doanhThuRepo = new DoanhThuRepository();
            _thucDonRepo = new ThucDonRepository();
            _banRepo = new BanAnRepository();

            DoanhThuTheoGio = new ObservableCollection<ChartBarItem>();
            ResolveRequestCommand = new RelayCommand<BanAn>(ResolveRequest);
            ProcessRequestCommand = new RelayCommand<BanAn>(ProcessRequest);

            LoadDashboardData();

            BanAnRepository.OnPaymentSuccess += () => LoadDashboardData();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += (s, e) => LoadDashboardData();
            _timer.Start();
        }

        private void ResolveRequest(BanAn ban)
        {
            if (ban == null) return;
            // Hàm này giữ lại để tương thích cũ nếu cần, nhưng logic chính đã chuyển sang ProcessRequest
            ProcessRequest(ban);
        }

        private void ProcessRequest(BanAn ban)
        {
            if (ban == null) return;

            // TRƯỜNG HỢP 1: YÊU CẦU THANH TOÁN -> CẦN ĐIỀU HƯỚNG
            if (ban.YeuCauThanhToan)
            {
                RequestNavigationToTable?.Invoke(ban);
            }
            // TRƯỜNG HỢP 2: YÊU CẦU HỖ TRỢ (Xin đồ...) -> XỬ LÝ NHANH TẠI CHỖ
            else if (!string.IsNullOrEmpty(ban.YeuCauHoTro))
            {
                if (MessageBox.Show($"Đã mang \"{ban.YeuCauHoTro}\" cho {ban.TenBan} chưa?",
                                    "Xác nhận phục vụ",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _banRepo.ResolvePaymentRequest(ban.SoBan);
                    BanCanXuLy.Remove(ban);
                }
            }
        }

        private void LoadDashboardData()
        {
            decimal totalToday = _doanhThuRepo.GetTodayRevenue();
            decimal totalYesterday = _doanhThuRepo.GetYesterdayRevenue();

            DoanhThuNgay = FormatCurrencyShort(totalToday);

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
                TrungBinhDon = "TB: " + FormatCurrencyShort(avg) + "/đơn";
            }
            else
            {
                TrungBinhDon = "Chưa có đơn";
            }

            LoadChartData();

            var allTables = _banRepo.GetAll();
            var urgentTables = allTables.Where(b => b.YeuCauThanhToan || !string.IsNullOrEmpty(b.YeuCauHoTro)).ToList();
            if (BanCanXuLy == null || BanCanXuLy.Count != urgentTables.Count)
                BanCanXuLy = new ObservableCollection<BanAn>(urgentTables);

            var topDict = _thucDonRepo.GetTopSellingFoods(5);
            var topList = topDict.Select(x => new TopFoodItem { MonAn = x.Key, SoLuongBan = x.Value }).ToList();
            TopMonAn = new ObservableCollection<TopFoodItem>(topList);
        }

        private void LoadChartData()
        {
            var hourlyData = _doanhThuRepo.GetRevenueByHour();
            DoanhThuTheoGio.Clear();

            double maxVal = 0;
            if (hourlyData.Count > 0)
            {
                maxVal = (double)hourlyData.Values.Max();
            }

            // [FIX 1] Tăng khoảng đệm lên 20% (nhân 1.2) để số liệu trên đỉnh không bị sát viền
            double targetTop = maxVal * 1.2;

            double axisTop = 2000000;
            if (targetTop > 2000000)
            {
                // Làm tròn lên hàng triệu
                axisTop = Math.Ceiling(targetTop / 1000000) * 1000000;
            }

            AxisMax = FormatCurrencyShort((decimal)axisTop);
            AxisMidHigh = FormatCurrencyShort((decimal)(axisTop * 0.75));
            AxisMid = FormatCurrencyShort((decimal)(axisTop * 0.5));
            AxisMidLow = FormatCurrencyShort((decimal)(axisTop * 0.25));

            double chartAreaHeight = 220;

            // [FIX 2] Chạy từ 8h đến 24h
            for (int hour = 8; hour <= 24; hour++)
            {
                decimal revenue = 0;
                // Giờ 24 thực chất là 0h hôm sau, hoặc chỉ là mốc kết thúc, doanh thu = 0
                if (hour < 24 && hourlyData.ContainsKey(hour))
                    revenue = hourlyData[hour];

                double val = (double)revenue;
                double pixelHeight = (val / axisTop) * chartAreaHeight;

                if (val > 0 && pixelHeight < 2) pixelHeight = 2;

                // Logic hiển thị nhãn giờ: Chỉ hiện các giờ chẵn (8, 10, 12...)
                string label = (hour % 2 == 0) ? $"{hour}h" : "";

                DoanhThuTheoGio.Add(new ChartBarItem
                {
                    TimeLabel = label,
                    Value = val,
                    BarHeight = pixelHeight
                });
            }
        }

        private string FormatCurrencyShort(decimal amount)
        {
            if (amount >= 1000000) return (amount / 1000000).ToString("0.##") + "M";
            if (amount >= 1000) return (amount / 1000).ToString("0.#") + "k";
            return amount.ToString("N0");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
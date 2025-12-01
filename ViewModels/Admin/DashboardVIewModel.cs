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

    // [MỚI] Class hỗ trợ vẽ biểu đồ (Thay thế KeyValuePair)
    public class ChartBarItem
    {
        public string TimeLabel { get; set; } // Giờ (VD: "10:00")
        public double Value { get; set; }     // Doanh thu thật (VD: 4,500,000)
        public double BarHeight { get; set; } // Chiều cao pixel đã tính toán (VD: 200)
        public string TooltipText => $"{Value:N0} VNĐ";
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DoanhThuRepository _doanhThuRepo;
        private readonly ThucDonRepository _thucDonRepo;
        private readonly BanAnRepository _banRepo;
        private DispatcherTimer _timer;

        // [SỬA] Dữ liệu biểu đồ dùng class mới
        public ObservableCollection<ChartBarItem> DoanhThuTheoGio { get; set; }

        // [MỚI] Các nhãn trục tung (Y-Axis) động
        private string _axisMax = "2M";
        private string _axisMidHigh = "1.5M";
        private string _axisMid = "1M";
        private string _axisMidLow = "500k";

        public string AxisMax { get => _axisMax; set { _axisMax = value; OnPropertyChanged(); } }
        public string AxisMidHigh { get => _axisMidHigh; set { _axisMidHigh = value; OnPropertyChanged(); } }
        public string AxisMid { get => _axisMid; set { _axisMid = value; OnPropertyChanged(); } }
        public string AxisMidLow { get => _axisMidLow; set { _axisMidLow = value; OnPropertyChanged(); } }

        // Danh sách Top món
        private ObservableCollection<TopFoodItem> _topMonAn;
        public ObservableCollection<TopFoodItem> TopMonAn
        {
            get => _topMonAn;
            set { _topMonAn = value; OnPropertyChanged(); }
        }

        // Danh sách Bàn cần xử lý
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
            ProcessRequestCommand = new RelayCommand<BanAn>(ProcessRequest);

            LoadDashboardData();

            BanAnRepository.OnPaymentSuccess += () => LoadDashboardData();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += (s, e) => LoadDashboardData();
            _timer.Start();
        }

        private void ProcessRequest(BanAn ban)
        {
            if (ban == null) return;

            // TRƯỜNG HỢP 1: YÊU CẦU THANH TOÁN -> CẦN ĐIỀU HƯỚNG
            if (ban.YeuCauThanhToan)
            {
                // Bắn sự kiện để View biết mà chuyển trang
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
                    // 1. Cập nhật DB (Xóa yêu cầu hỗ trợ)
                    _banRepo.ResolvePaymentRequest(ban.SoBan);

                    // 2. Xóa khỏi danh sách hiển thị ngay lập tức
                    BanCanXuLy.Remove(ban);
                }
            }
        }

        private void LoadDashboardData()
        {
            // 1. Load số liệu tổng quan
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

            // 2. Load biểu đồ (Đã nâng cấp logic)
            LoadChartData();

            // 3. Load bàn cần xử lý [CẬP NHẬT LOGIC]
            var allTables = _banRepo.GetAll();

            // Lọc các bàn: Có yêu cầu thanh toán HOẶC Có yêu cầu hỗ trợ (khác null/empty)
            var urgentTables = allTables.Where(b => b.YeuCauThanhToan || !string.IsNullOrEmpty(b.YeuCauHoTro)).ToList();

            // Cập nhật danh sách hiển thị
            BanCanXuLy = new ObservableCollection<BanAn>(urgentTables);

            // 4. Load Top món
            var topDict = _thucDonRepo.GetTopSellingFoods(5);
            var topList = topDict.Select(x => new TopFoodItem { MonAn = x.Key, SoLuongBan = x.Value }).ToList();
            TopMonAn = new ObservableCollection<TopFoodItem>(topList);
        }

        // [LOGIC MỚI] Tính toán chiều cao cột và thang đo trục tung
        private void LoadChartData()
        {
            var hourlyData = _doanhThuRepo.GetRevenueByHour();
            DoanhThuTheoGio.Clear();

            // Bước 1: Tìm giá trị lớn nhất trong ngày
            double maxVal = 0;
            if (hourlyData.Count > 0)
            {
                maxVal = (double)hourlyData.Values.Max();
            }

            // Bước 2: Xác định thang đo (Max Axis)
            // Nếu max < 2tr thì để thang 2tr cho đẹp
            // Nếu max lớn hơn thì làm tròn lên (VD: 4.49M -> 5M)
            double axisTop = 2000000;
            if (maxVal > 2000000)
            {
                // Làm tròn lên mức đẹp tiếp theo (bội số của 1M)
                axisTop = Math.Ceiling(maxVal / 1000000) * 1000000;
                // Nếu sát nút quá (VD 4.9M so với 5M) thì cộng thêm 1 nấc cho thoáng
                if (maxVal > axisTop * 0.9) axisTop += 1000000;
            }

            // Bước 3: Cập nhật các nhãn trục tung
            AxisMax = FormatCurrencyShort((decimal)axisTop);
            AxisMidHigh = FormatCurrencyShort((decimal)(axisTop * 0.75));
            AxisMid = FormatCurrencyShort((decimal)(axisTop * 0.5));
            AxisMidLow = FormatCurrencyShort((decimal)(axisTop * 0.25));

            // Bước 4: Tạo dữ liệu cột với chiều cao Pixel (Max Height khung vẽ là 220px)
            double chartAreaHeight = 220;

            for (int hour = 8; hour <= 22; hour++)
            {
                decimal revenue = 0;
                if (hourlyData.ContainsKey(hour)) revenue = hourlyData[hour];

                double val = (double)revenue;

                // Công thức: (Giá trị / Thang đo max) * Chiều cao khung
                double pixelHeight = (val / axisTop) * chartAreaHeight;

                // Đảm bảo tối thiểu 2px để người dùng thấy có cột (nếu có tiền)
                if (val > 0 && pixelHeight < 2) pixelHeight = 2;

                DoanhThuTheoGio.Add(new ChartBarItem
                {
                    TimeLabel = $"{hour}:00",
                    Value = val,
                    BarHeight = pixelHeight
                });
            }
        }

        // Helper format tiền gọn (1.5M, 500k)
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
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using OrMan.Models;
using OrMan.Services;
using System.Collections.Generic;
using System.Windows.Input;
using OrMan.Helpers;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using OrMan.Data; // Thêm namespace để gọi MenuContext

namespace OrMan.ViewModels
{
    public enum ChartFilterType { Day, Week, Month, Year }

    public class TopFoodItem
    {
        public MonAn MonAn { get; set; }
        public int SoLuongBan { get; set; }
    }

    public class ChartBarItem
    {
        public string TimeLabel { get; set; }
        public double Value { get; set; }
        public double EmptyValue { get; set; }
        public string BarStar => $"{Value.ToString(CultureInfo.InvariantCulture)}*";
        public string EmptyStar => $"{EmptyValue.ToString(CultureInfo.InvariantCulture)}*";
        public string TooltipText => $"{Value:N0} VNĐ";
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DoanhThuRepository _doanhThuRepo;
        private readonly ThucDonRepository _thucDonRepo;
        private readonly BanAnRepository _banRepo;
        private DispatcherTimer _timer;

        private ChartFilterType _currentFilter = ChartFilterType.Day;
        public ICommand ChangeFilterCommand { get; private set; }

        private Brush _tangTruongColor;
        public Brush TangTruongColor
        {
            get => _tangTruongColor;
            set { _tangTruongColor = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ChartBarItem> DoanhThuTheoGio { get; set; } = new ObservableCollection<ChartBarItem>();

        private string _axisMax = "2M";
        private string _axisMidHigh = "1.5M";
        private string _axisMid = "1M";
        private string _axisMidLow = "500k";

        public string AxisMax { get => _axisMax; set { _axisMax = value; OnPropertyChanged(); } }
        public string AxisMidHigh { get => _axisMidHigh; set { _axisMidHigh = value; OnPropertyChanged(); } }
        public string AxisMid { get => _axisMid; set { _axisMid = value; OnPropertyChanged(); } }
        public string AxisMidLow { get => _axisMidLow; set { _axisMidLow = value; OnPropertyChanged(); } }

        private string _banHoatDongText;
        public string BanHoatDongText
        {
            get => _banHoatDongText;
            set { _banHoatDongText = value; OnPropertyChanged(); }
        }

        private string _congSuatText;
        public string CongSuatText
        {
            get => _congSuatText;
            set { _congSuatText = value; OnPropertyChanged(); }
        }
        private string _tangTruongDonHangText;
        public string TangTruongDonHangText
        {
            get => _tangTruongDonHangText;
            set { _tangTruongDonHangText = value; OnPropertyChanged(); }
        }

        private Brush _tangTruongDonHangColor;
        public Brush TangTruongDonHangColor
        {
            get => _tangTruongDonHangColor;
            set { _tangTruongDonHangColor = value; OnPropertyChanged(); }
        }

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

        // --- MỚI: Properties cho phần Đánh Giá ---
        private ObservableCollection<DanhGia> _danhSachDanhGia;
        public ObservableCollection<DanhGia> DanhSachDanhGia
        {
            get => _danhSachDanhGia;
            set { _danhSachDanhGia = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNoReviews)); }
        }

        private double _diemDanhGiaTB;
        public double DiemDanhGiaTB
        {
            get => _diemDanhGiaTB;
            set { _diemDanhGiaTB = value; OnPropertyChanged(); }
        }

        private int _soLuotDanhGia;
        public int SoLuotDanhGia
        {
            get => _soLuotDanhGia;
            set { _soLuotDanhGia = value; OnPropertyChanged(); }
        }

        public bool IsNoReviews => DanhSachDanhGia == null || DanhSachDanhGia.Count == 0;
        // ----------------------------------------

        private string _doanhThuNgay;
        public string DoanhThuNgay { get => _doanhThuNgay; set { _doanhThuNgay = value; OnPropertyChanged(); } }

        private string _tangTruongText;
        public string TangTruongText { get => _tangTruongText; set { _tangTruongText = value; OnPropertyChanged(); } }

        private int _soDonHomNay;
        public int SoDonHomNay { get => _soDonHomNay; set { _soDonHomNay = value; OnPropertyChanged(); } }

        private string _trungBinhDon;
        public string TrungBinhDon { get => _trungBinhDon; set { _trungBinhDon = value; OnPropertyChanged(); } }

        public ICommand ResolveRequestCommand { get; private set; }
        public ICommand ProcessRequestCommand { get; private set; }
        public event Action<BanAn> RequestNavigationToTable;

        public DashboardViewModel()
        {
            _doanhThuRepo = new DoanhThuRepository();
            _thucDonRepo = new ThucDonRepository();
            _banRepo = new BanAnRepository();

            ResolveRequestCommand = new RelayCommand<BanAn>(ResolveRequest);
            ProcessRequestCommand = new RelayCommand<BanAn>(ProcessRequest);
            ChangeFilterCommand = new RelayCommand<string>(ChangeFilter);

            // Load dữ liệu lần đầu
            Task.Run(() => LoadDashboardDataAsync());

            // Đăng ký sự kiện cập nhật
            BanAnRepository.OnPaymentSuccess += () => Task.Run(() => LoadDashboardDataAsync());
            BanAnRepository.OnTableChanged += () => Task.Run(() => LoadDashboardDataAsync());

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += (s, e) => Task.Run(() => LoadDashboardDataAsync());
            _timer.Start();
        }

        private void ChangeFilter(string type)
        {
            switch (type)
            {
                case "Day": _currentFilter = ChartFilterType.Day; break;
                case "Week": _currentFilter = ChartFilterType.Week; break;
                case "Month": _currentFilter = ChartFilterType.Month; break;
                case "Year": _currentFilter = ChartFilterType.Year; break;
            }
            Task.Run(() => LoadDashboardDataAsync());
        }

        private void ResolveRequest(BanAn ban)
        {
            if (ban == null) return;
            ProcessRequest(ban);
        }

        private void ProcessRequest(BanAn ban)
        {
            if (ban == null) return;

            if (ban.YeuCauThanhToan)
            {
                RequestNavigationToTable?.Invoke(ban);
            }
            else if (!string.IsNullOrEmpty(ban.YeuCauHoTro))
            {
                if (MessageBox.Show($"Đã mang \"{ban.YeuCauHoTro}\" cho {ban.TenBan} chưa?",
                                    "Xác nhận phục vụ",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _banRepo.ResolvePaymentRequest(ban.SoBan);
                    Application.Current.Dispatcher.Invoke(() => BanCanXuLy.Remove(ban));
                }
            }
        }

        private int _previousRequestCount = 0;

        private void LoadDashboardDataAsync()
        {
            try
            {
                // [BACKGROUND THREAD]
                decimal totalToday = _doanhThuRepo.GetTodayRevenue();
                decimal totalYesterday = _doanhThuRepo.GetYesterdayRevenue();
                int ordersToday = _doanhThuRepo.GetTodayOrderCount();
                int ordersYesterday = _doanhThuRepo.GetYesterdayOrderCount();

                var allTables = _banRepo.GetAll();
                int tongSoBan = allTables.Count;
                int banCoKhach = allTables.Count(b => b.TrangThai != "Trống");

                var urgentTables = allTables.Where(b => b.YeuCauThanhToan || !string.IsNullOrEmpty(b.YeuCauHoTro)).ToList();

                var topDict = _thucDonRepo.GetTopSellingFoods(5);
                var topList = topDict.Select(x => new TopFoodItem { MonAn = x.Key, SoLuongBan = x.Value }).ToList();

                // --- LOGIC MỚI: Lấy dữ liệu Đánh Giá ---
                List<DanhGia> recentReviews = new List<DanhGia>();
                double avgRating = 0;
                int countRating = 0;

                using (var db = new MenuContext())
                {
                    // Lấy tất cả để tính trung bình
                    var allReviews = db.DanhGias.ToList(); // Lưu ý: Nếu data lớn nên optimize query
                    if (allReviews.Any())
                    {
                        avgRating = allReviews.Average(x => x.SoSao);
                        countRating = allReviews.Count;
                    }

                    // Lấy 10 đánh giá gần nhất
                    recentReviews = db.DanhGias
                                      .OrderByDescending(x => x.NgayTao)
                                      .Take(10)
                                      .ToList();
                }
                // ---------------------------------------

                Dictionary<int, decimal> chartData = new Dictionary<int, decimal>();
                int start = 0, end = 0;
                switch (_currentFilter)
                {
                    case ChartFilterType.Day:
                        chartData = _doanhThuRepo.GetRevenueByHour(); start = 0; end = 24; break;
                    case ChartFilterType.Week:
                        chartData = _doanhThuRepo.GetRevenueByWeek(); start = 2; end = 8; break;
                    case ChartFilterType.Month:
                        chartData = _doanhThuRepo.GetRevenueByMonth(); start = 1; end = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month); break;
                    case ChartFilterType.Year:
                        chartData = _doanhThuRepo.GetRevenueByYear(); start = 1; end = 12; break;
                }

                // [UI THREAD] Cập nhật giao diện
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DoanhThuNgay = FormatCurrencyShort(totalToday);
                    decimal diff = totalToday - totalYesterday;
                    string sign = diff >= 0 ? "+" : "-";
                    string diffString = FormatCurrencyShort(Math.Abs(diff));

                    if (totalYesterday == 0)
                        TangTruongText = totalToday > 0 ? $"+{diffString} (Mới)" : "---";
                    else
                    {
                        double percent = (double)(Math.Abs(diff) / totalYesterday) * 100;
                        TangTruongText = $"{sign}{diffString} ({percent:0}%) so với hôm qua";
                    }

                    var converter = new BrushConverter();
                    TangTruongColor = (Brush)converter.ConvertFrom(diff >= 0 ? "#22C55E" : "#EF4444");

                    SoDonHomNay = ordersToday;
                    if (SoDonHomNay > 0)
                    {
                        decimal avg = totalToday / SoDonHomNay;
                        TrungBinhDon = "TB: " + FormatCurrencyShort(avg) + "/đơn";
                    }
                    else TrungBinhDon = "Chưa có đơn";

                    int diffOrder = ordersToday - ordersYesterday;
                    string signOrder = diffOrder >= 0 ? "+" : "-";
                    if (ordersYesterday == 0)
                        TangTruongDonHangText = ordersToday > 0 ? $"+{ordersToday} (Mới)" : "---";
                    else
                    {
                        double percentOrder = (double)Math.Abs(diffOrder) / ordersYesterday * 100;
                        TangTruongDonHangText = $"{signOrder}{Math.Abs(diffOrder)} ({percentOrder:0}%) so với hôm qua";
                    }
                    TangTruongDonHangColor = (Brush)converter.ConvertFrom(diffOrder >= 0 ? "#22C55E" : "#EF4444");

                    BanHoatDongText = $"{banCoKhach} / {tongSoBan}";
                    CongSuatText = tongSoBan > 0 ? $"Công suất: {((double)banCoKhach / tongSoBan) * 100:0}%" : "Công suất: 0%";

                    if (urgentTables.Count > _previousRequestCount) System.Media.SystemSounds.Exclamation.Play();
                    _previousRequestCount = urgentTables.Count;

                    BanCanXuLy = new ObservableCollection<BanAn>(urgentTables);
                    TopMonAn = new ObservableCollection<TopFoodItem>(topList);

                    // Update UI Đánh Giá
                    DiemDanhGiaTB = avgRating;
                    SoLuotDanhGia = countRating;
                    DanhSachDanhGia = new ObservableCollection<DanhGia>(recentReviews);

                    UpdateChartUI(chartData, start, end);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard Error: " + ex.Message);
            }
        }

        private void UpdateChartUI(Dictionary<int, decimal> data, int start, int end)
        {
            DoanhThuTheoGio.Clear();

            double maxVal = data.Count > 0 ? (double)data.Values.Max() : 0;
            double axisTop = 1000000;

            if (maxVal > 0)
            {
                double target = maxVal * 1.1;
                double exponent = Math.Floor(Math.Log10(target));
                double powerOf10 = Math.Pow(10, exponent);
                double fraction = target / powerOf10;
                double niceFraction;

                if (fraction <= 1.0) niceFraction = 1.0;
                else if (fraction <= 1.2) niceFraction = 1.2;
                else if (fraction <= 1.5) niceFraction = 1.5;
                else if (fraction <= 2.0) niceFraction = 2.0;
                else if (fraction <= 2.5) niceFraction = 2.5;
                else if (fraction <= 3.0) niceFraction = 3.0;
                else if (fraction <= 4.0) niceFraction = 4.0;
                else if (fraction <= 5.0) niceFraction = 5.0;
                else if (fraction <= 6.0) niceFraction = 6.0;
                else if (fraction <= 8.0) niceFraction = 8.0;
                else niceFraction = 10.0;

                axisTop = niceFraction * powerOf10;
            }

            AxisMax = FormatCurrencyShort((decimal)axisTop);
            AxisMidHigh = FormatCurrencyShort((decimal)(axisTop * 0.75));
            AxisMid = FormatCurrencyShort((decimal)(axisTop * 0.5));
            AxisMidLow = FormatCurrencyShort((decimal)(axisTop * 0.25));

            for (int i = start; i <= end; i++)
            {
                decimal revenue = data.ContainsKey(i) ? data[i] : 0;
                double val = (double)revenue;
                double empty = axisTop - val;
                if (empty < 0) empty = 0;

                string label = "";
                if (_currentFilter == ChartFilterType.Day) label = (i % 2 == 0) ? $"{i}h" : "";
                else if (_currentFilter == ChartFilterType.Week) label = i == 8 ? "CN" : $"T{i}";
                else if (_currentFilter == ChartFilterType.Month) label = (i % 2 != 0 || i == end) ? $"{i}" : "";
                else if (_currentFilter == ChartFilterType.Year) label = $"T{i}";

                DoanhThuTheoGio.Add(new ChartBarItem
                {
                    TimeLabel = label,
                    Value = val,
                    EmptyValue = empty
                });
            }
        }

        private string FormatCurrencyShort(decimal amount)
        {
            if (amount >= 1000000000) return (amount / 1000000000).ToString("0.##") + "B";
            if (amount >= 1000000) return (amount / 1000000).ToString("0.##") + "M";
            if (amount >= 1000) return (amount / 1000).ToString("0.#") + "k";
            return amount.ToString("N0");
        }

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
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
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

namespace OrMan.ViewModels
{
    // Enum để định nghĩa các loại lọc
    public enum ChartFilterType { Day, Week, Month }

    public class TopFoodItem
    {
        public MonAn MonAn { get; set; }
        public int SoLuongBan { get; set; }
    }

    public class ChartBarItem
    {
        public string TimeLabel { get; set; }
        public double Value { get; set; }
        public double EmptyValue { get; set; } // Phần trống phía trên cột

        // Binding cho Grid.RowDefinitions (Responsive tuyệt đối)
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

        // --- 1. Biến lưu trạng thái lọc ---
        private ChartFilterType _currentFilter = ChartFilterType.Day;
        public ICommand ChangeFilterCommand { get; private set; }

        public ObservableCollection<ChartBarItem> DoanhThuTheoGio { get; set; }

        // Các biến trục tung biểu đồ
        private string _axisMax = "2M";
        private string _axisMidHigh = "1.5M";
        private string _axisMid = "1M";
        private string _axisMidLow = "500k";

        public string AxisMax { get => _axisMax; set { _axisMax = value; OnPropertyChanged(); } }
        public string AxisMidHigh { get => _axisMidHigh; set { _axisMidHigh = value; OnPropertyChanged(); } }
        public string AxisMid { get => _axisMid; set { _axisMid = value; OnPropertyChanged(); } }
        public string AxisMidLow { get => _axisMidLow; set { _axisMidLow = value; OnPropertyChanged(); } }

        // Các biến hiển thị Dashboard
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
        public ICommand ProcessRequestCommand { get; private set; }
        public event Action<BanAn> RequestNavigationToTable;

        public DashboardViewModel()
        {
            _doanhThuRepo = new DoanhThuRepository();
            _thucDonRepo = new ThucDonRepository();
            _banRepo = new BanAnRepository();

            DoanhThuTheoGio = new ObservableCollection<ChartBarItem>();
            ResolveRequestCommand = new RelayCommand<BanAn>(ResolveRequest);
            ProcessRequestCommand = new RelayCommand<BanAn>(ProcessRequest);

            // --- 2. Khởi tạo Command lọc ---
            ChangeFilterCommand = new RelayCommand<string>(ChangeFilter);

            LoadDashboardData();

            BanAnRepository.OnPaymentSuccess += () => LoadDashboardData();
            BanAnRepository.OnTableChanged += () => LoadDashboardData();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += (s, e) => LoadDashboardData();
            _timer.Start();
        }

        // --- 3. Xử lý khi bấm nút RadioButton ---
        private void ChangeFilter(string type)
        {
            switch (type)
            {
                case "Day": _currentFilter = ChartFilterType.Day; break;
                case "Week": _currentFilter = ChartFilterType.Week; break;
                case "Month": _currentFilter = ChartFilterType.Month; break;
            }
            LoadChartData(); // Vẽ lại biểu đồ
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
                    BanCanXuLy.Remove(ban);
                }
            }
        }

        private int _previousRequestCount = 0;

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
            int tongSoBan = allTables.Count;
            int banCoKhach = allTables.Count(b => b.TrangThai != "Trống");

            BanHoatDongText = $"{banCoKhach} / {tongSoBan}";

            if (tongSoBan > 0)
            {
                double phanTram = ((double)banCoKhach / tongSoBan) * 100;
                CongSuatText = $"Công suất: {phanTram:0}%";
            }
            else
            {
                CongSuatText = "Công suất: 0%";
            }

            var urgentTables = allTables.Where(b => b.YeuCauThanhToan || !string.IsNullOrEmpty(b.YeuCauHoTro)).ToList();

            if (urgentTables.Count > _previousRequestCount)
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
            _previousRequestCount = urgentTables.Count;

            if (BanCanXuLy == null || BanCanXuLy.Count != urgentTables.Count)
                BanCanXuLy = new ObservableCollection<BanAn>(urgentTables);

            var topDict = _thucDonRepo.GetTopSellingFoods(5);
            var topList = topDict.Select(x => new TopFoodItem { MonAn = x.Key, SoLuongBan = x.Value }).ToList();
            TopMonAn = new ObservableCollection<TopFoodItem>(topList);
        }

        // --- 4. Logic LoadChartData Động ---
        private void LoadChartData()
        {
            DoanhThuTheoGio.Clear();

            // Khởi tạo các biến để xử lý động
            Dictionary<int, decimal> data = new Dictionary<int, decimal>();
            int start = 0, end = 0;

            // Lấy dữ liệu từ Repo dựa trên Filter
            switch (_currentFilter)
            {
                case ChartFilterType.Day:
                    data = _doanhThuRepo.GetRevenueByHour();
                    start = 8;
                    end = 24;
                    break;

                case ChartFilterType.Week:
                    data = _doanhThuRepo.GetRevenueByWeek(); // Đảm bảo Repo đã có hàm này
                    start = 2;
                    end = 8; // Thứ 2 -> Chủ Nhật (quy ước là 8)
                    break;

                case ChartFilterType.Month:
                    data = _doanhThuRepo.GetRevenueByMonth(); // Đảm bảo Repo đã có hàm này
                    start = 1;
                    end = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
                    break;
            }

            // Tính Max Value để chia trục
            double maxVal = data.Count > 0 ? (double)data.Values.Max() : 0;
            double axisTop = 1000000; // Mặc định 1M

            if (maxVal > 0)
            {
                double target = maxVal * 1.1; // Đệm 10%
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

            // Vòng lặp tạo cột biểu đồ
            for (int i = start; i <= end; i++)
            {
                decimal revenue = 0;
                if (data.ContainsKey(i))
                    revenue = data[i];

                double val = (double)revenue;
                double empty = axisTop - val;
                if (empty < 0) empty = 0;

                // Xử lý Label trục hoành (X-Axis)
                string label = "";
                if (_currentFilter == ChartFilterType.Day)
                {
                    label = (i % 2 == 0) ? $"{i}h" : "";
                }
                else if (_currentFilter == ChartFilterType.Week)
                {
                    label = i == 8 ? "CN" : $"T{i}";
                }
                else // Month
                {
                    // Chỉ hiện ngày lẻ hoặc cách nhật để đỡ rối nếu cần
                    label = (i % 2 != 0 || i == end) ? $"{i}" : "";
                }

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
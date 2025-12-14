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
using System.Globalization; // Thêm để format số chuẩn

namespace OrMan.ViewModels
{
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
        // Sử dụng InvariantCulture để đảm bảo dấu chấm thập phân đúng định dạng XAML
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

        public ObservableCollection<ChartBarItem> DoanhThuTheoGio { get; set; }

        private string _axisMax = "2M";
        private string _axisMidHigh = "1.5M";
        private string _axisMid = "1M";
        private string _axisMidLow = "500k";
        private string _banHoatDongText;
        public string BanHoatDongText
        {
            get => _banHoatDongText;
            set { _banHoatDongText = value; OnPropertyChanged(); }
        }

        // [MỚI] Thuộc tính hiển thị công suất (VD: "Công suất: 25%")
        private string _congSuatText;
        public string CongSuatText
        {
            get => _congSuatText;
            set { _congSuatText = value; OnPropertyChanged(); }
        }
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

            LoadDashboardData();

            BanAnRepository.OnPaymentSuccess += () => LoadDashboardData();

            // [MỚI] Cập nhật ngay khi Thêm hoặc Xóa bàn
            BanAnRepository.OnTableChanged += () => LoadDashboardData();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += (s, e) => LoadDashboardData();
            _timer.Start();
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

            // Bàn có khách là bàn có trạng thái KHÁC "Trống"
            int banCoKhach = allTables.Count(b => b.TrangThai != "Trống");

            // Cập nhật text "X / Y"
            BanHoatDongText = $"{banCoKhach} / {tongSoBan}";

            // Tính phần trăm công suất
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
            // [CODE MỚI - LOGIC ÂM THANH]
            if (urgentTables.Count > _previousRequestCount)
            {
                // Có yêu cầu mới -> Phát âm thanh hệ thống
                System.Media.SystemSounds.Exclamation.Play();
                // Hoặc nếu muốn tiếng Beep rõ hơn: 
                // Console.Beep(800, 500); 
            }
            _previousRequestCount = urgentTables.Count;
            // [HẾT CODE MỚI]
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

            double maxVal = hourlyData.Count > 0 ? (double)hourlyData.Values.Max() : 0;

            // --- LOGIC TÍNH TRỤC TUNG THÔNG MINH ---
            double axisTop = 1000000; // Mặc định 1M nếu không có data

            if (maxVal > 0)
            {
                // Thêm 10% đệm
                double target = maxVal * 1.1;

                // Tìm bậc của số (Ví dụ: 150k -> bậc 100k, 2.5M -> bậc 1M)
                double exponent = Math.Floor(Math.Log10(target));
                double powerOf10 = Math.Pow(10, exponent);
                double fraction = target / powerOf10;

                // Làm tròn lên các mốc đẹp: 1.0, 1.2, 1.5, 2.0, 2.5, 5.0, 10.0
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
            // ----------------------------------------

            AxisMax = FormatCurrencyShort((decimal)axisTop);
            AxisMidHigh = FormatCurrencyShort((decimal)(axisTop * 0.75));
            AxisMid = FormatCurrencyShort((decimal)(axisTop * 0.5));
            AxisMidLow = FormatCurrencyShort((decimal)(axisTop * 0.25));

            for (int hour = 8; hour <= 24; hour++)
            {
                decimal revenue = 0;
                if (hour < 24 && hourlyData.ContainsKey(hour))
                    revenue = hourlyData[hour];

                double val = (double)revenue;

                // Tính phần trống để Grid chia tỷ lệ
                double empty = axisTop - val;
                if (empty < 0) empty = 0; // Đề phòng lỗi làm tròn

                string label = (hour % 2 == 0) ? $"{hour}h" : "";

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
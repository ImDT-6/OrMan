using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using OrMan.Models;
using OrMan.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using OrMan.Helpers;

namespace OrMan.ViewModels
{
    public class DoanhThuViewModel : INotifyPropertyChanged
    {
        private readonly DoanhThuRepository _repository;
        private ObservableCollection<HoaDon> _allHoaDons; // Dữ liệu gốc

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
                FilterData();
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
                    // Lưu ý: Nếu set về các tag mặc định thì FilterData tự chạy
                    // Nhưng nếu set "Tùy chọn" thì ta chờ hàm LocTheoKhoangThoiGian gọi FilterData sau
                    if (value != "Tùy chọn")
                    {
                        FilterData();
                    }
                }
            }
        }

        // Các chỉ số thống kê
        private string _tongDoanhThuText;
        public string TongDoanhThuText { get => _tongDoanhThuText; set { _tongDoanhThuText = value; OnPropertyChanged(); } }

        private int _tongSoDon;
        public int TongSoDon { get => _tongSoDon; set { _tongSoDon = value; OnPropertyChanged(); } }

        private string _trungBinhDon;
        public string TrungBinhDon { get => _trungBinhDon; set { _trungBinhDon = value; OnPropertyChanged(); } }

        public DoanhThuViewModel()
        {
            _repository = new DoanhThuRepository();
            LoadData();
            BanAnRepository.OnPaymentSuccess += () => LoadData();
        }

        private void LoadData()
        {
            _allHoaDons = _repository.GetAll();
            FilterData();
        }

        // [MỚI] Hàm này được gọi từ View (Code-behind) khi nhấn nút "Xem thống kê"
        public void LocTheoKhoangThoiGian(DateTime from, DateTime to)
        {
            _selectedTimeFilter = "Tùy chọn"; // Chuyển chế độ lọc sang Tùy chọn
            _customFromDate = from;
            _customToDate = to;

            FilterData(); // Thực hiện lọc lại
        }

        // [CẬP NHẬT] Logic lọc dữ liệu
        private void FilterData()
        {
            if (_allHoaDons == null) return;

            IEnumerable<HoaDon> query = _allHoaDons;
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MaxValue;

            // 1. Logic lọc thời gian
            switch (_selectedTimeFilter)
            {
                case "Hôm nay":
                    startDate = DateTime.Today;
                    endDate = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;

                case "Tuần này":
                    int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDate = DateTime.Today.AddDays(-1 * diff).Date;
                    endDate = DateTime.Now; // Hoặc hết tuần tùy logic
                    break;

                case "Tháng này":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    endDate = startDate.AddMonths(1).AddTicks(-1);
                    break;

                case "Tùy chọn":
                    // [MỚI] Sử dụng ngày được truyền vào từ hàm LocTheoKhoangThoiGian
                    startDate = _customFromDate;
                    endDate = _customToDate;
                    break;

                case "Tất cả":
                    // Không làm gì cả, lấy hết
                    break;
            }

            // Thực hiện query lọc ngày (trừ trường hợp Tất cả)
            if (_selectedTimeFilter != "Tất cả")
            {
                query = query.Where(h => h.NgayTao >= startDate && h.NgayTao <= endDate);
            }

            // 2. Lọc theo từ khóa
            if (!string.IsNullOrEmpty(TuKhoaTimKiem))
            {
                string k = TuKhoaTimKiem.ToLower();
                query = query.Where(x => x.MaHoaDon.ToLower().Contains(k) ||
                                         (x.NguoiTao != null && x.NguoiTao.ToLower().Contains(k)));
            }

            // Cập nhật UI
            DanhSachHoaDon = new ObservableCollection<HoaDon>(query.OrderByDescending(h => h.NgayTao));
            TinhToanThongKe();
        }

        private void TinhToanThongKe()
        {
            if (DanhSachHoaDon == null || DanhSachHoaDon.Count == 0)
            {
                TongDoanhThuText = "0 VNĐ";
                TongSoDon = 0;
                TrungBinhDon = "0 VNĐ";
                return;
            }

            decimal total = DanhSachHoaDon.Sum(x => x.TongTien);
            TongDoanhThuText = total.ToString("N0") + " VNĐ";
            TongSoDon = DanhSachHoaDon.Count;

            decimal avg = total / TongSoDon;
            TrungBinhDon = avg.ToString("N0") + " VNĐ";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GymManagement.Models;
using GymManagement.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using GymManagement.Helpers;

namespace GymManagement.ViewModels
{
    public class DoanhThuViewModel : INotifyPropertyChanged
    {
        private readonly DoanhThuRepository _repository;
        private ObservableCollection<HoaDon> _allHoaDons; // Dữ liệu gốc (đầy đủ)

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
                FilterData(); // Gõ chữ là lọc ngay
            }
        }

        // [SỬA ĐỔI] Thêm Property cho bộ lọc thời gian (Tag của RadioButton)
        private string _selectedTimeFilter = "Hôm nay"; // Mặc định là "Hôm nay"
        public string SelectedTimeFilter
        {
            get => _selectedTimeFilter;
            set
            {
                if (_selectedTimeFilter != value)
                {
                    _selectedTimeFilter = value;
                    OnPropertyChanged();
                    FilterData(); // Lọc lại khi bộ lọc thời gian thay đổi
                }
            }
        }

        // Các chỉ số thống kê... (Giữ nguyên)
        private string _tongDoanhThuText;
        public string TongDoanhThuText { get => _tongDoanhThuText; set { _tongDoanhThuText = value; OnPropertyChanged(); } }

        private int _tongSoDon;
        public int TongSoDon { get => _tongSoDon; set { _tongSoDon = value; OnPropertyChanged(); } }

        private string _trungBinhDon;
        public string TrungBinhDon { get => _trungBinhDon; set { _trungBinhDon = value; OnPropertyChanged(); } }

        public DoanhThuViewModel()
        {
            _repository = new DoanhThuRepository();

            // Load lần đầu
            LoadData();

            // [MỚI] Đăng ký sự kiện: Khi có thanh toán -> Load lại danh sách hóa đơn
            BanAnRepository.OnPaymentSuccess += () => LoadData();
        }

        private void LoadData()
        {
            // [CẢI THIỆN] Lấy tất cả hóa đơn đã thanh toán để tránh lẫn lộn
            // Giả sử GetAll() đã lấy tất cả. Ta sẽ lọc sau.
            _allHoaDons = _repository.GetAll();
            FilterData(); // Lọc dữ liệu ngay sau khi load
        }

        // [SỬA LOGIC] Hàm lọc chính, kết hợp cả Lọc theo Thời gian và Lọc theo Từ khóa
        private void FilterData()
        {
            if (_allHoaDons == null) return;

            IEnumerable<HoaDon> query = _allHoaDons;
            DateTime startDate = DateTime.MinValue;

            // 1. Lọc theo thời gian
            if (_selectedTimeFilter == "Hôm nay")
            {
                startDate = DateTime.Today;
            }
            else if (_selectedTimeFilter == "Tuần này")
            {
                // Ngày đầu tuần (Thứ Hai)
                int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = DateTime.Today.AddDays(-1 * diff).Date;
            }
            else if (_selectedTimeFilter == "Tháng này")
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            // Thực hiện lọc theo thời gian
            if (_selectedTimeFilter != "Tất cả") // Nếu có bộ lọc thời gian
            {
                query = query.Where(h => h.NgayTao >= startDate);
            }


            // 2. Lọc theo từ khóa (Mã HĐ hoặc Tên nhân viên)
            if (!string.IsNullOrEmpty(TuKhoaTimKiem))
            {
                string k = TuKhoaTimKiem.ToLower();
                query = query.Where(x => x.MaHoaDon.ToLower().Contains(k) ||
                                         (x.NguoiTao != null && x.NguoiTao.ToLower().Contains(k)));
            }

            // Cập nhật danh sách hiển thị
            DanhSachHoaDon = new ObservableCollection<HoaDon>(query.OrderByDescending(h => h.NgayTao));

            // Tính lại thống kê theo danh sách mới lọc
            TinhToanThongKe();
        }

        private void TinhToanThongKe()
        {
            // Logic giữ nguyên
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
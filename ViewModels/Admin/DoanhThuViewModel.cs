using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        private List<HoaDon> _allHoaDons;
        public ICommand ViewDetailCommand { get; private set; }

        // Biến lưu khoảng thời gian tùy chọn
        private DateTime _customFromDate;
        private DateTime _customToDate;

        // [MỚI] Biến dùng chung để hiển thị Tổng Doanh Thu (Thực Thu)
        private string _tongDoanhThuHienTai = "0 VNĐ";
        public string TongDoanhThuHienTai
        {
            get => _tongDoanhThuHienTai;
            set { _tongDoanhThuHienTai = value; OnPropertyChanged(); }
        }

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

        private int _tongSoDon;
        public int TongSoDon { get => _tongSoDon; set { _tongSoDon = value; OnPropertyChanged(); } }

        private string _trungBinhDon;
        public string TrungBinhDon { get => _trungBinhDon; set { _trungBinhDon = value; OnPropertyChanged(); } }

        public DoanhThuViewModel()
        {
            _repository = new DoanhThuRepository();
            Task.Run(() => LoadDataAsync());
            BanAnRepository.OnPaymentSuccess += () => Task.Run(() => LoadDataAsync());
            ViewDetailCommand = new RelayCommand<HoaDon>(OpenDetailWindow);
        }

        // [MỚI] Hàm mở cửa sổ chi tiết
        private void OpenDetailWindow(HoaDon hd)
        {
            if (hd == null) return;
            var listMonAn = _repository.GetChiTietHoaDon(hd.MaHoaDon);
            string tenBan = "Bàn " + hd.SoBan.ToString("00");
            string hinhThuc = "Đã thanh toán";

            // Lưu ý: Cần import OrMan.Views.Admin
            OrMan.Views.Admin.HoaDonWindow view = new OrMan.Views.Admin.HoaDonWindow(tenBan, hd, listMonAn, hinhThuc);
            view.ShowDialog();
        }

        public void Cleanup()
        {
            BanAnRepository.OnPaymentSuccess -= () => Task.Run(() => LoadDataAsync());
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var data = await Task.Run(() => _repository.GetAll().ToList());
                _allHoaDons = data;
                await FilterDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi Load Doanh Thu: " + ex.Message);
            }
        }

        // [SỬA LẠI] Hàm lọc theo ngày chỉ cần set biến và gọi FilterDataAsync
        public void LocTheoKhoangThoiGian(DateTime from, DateTime to)
        {
            _selectedTimeFilter = "Tùy chọn";
            _customFromDate = from;
            _customToDate = to;

            // Gọi lọc dữ liệu (việc tính toán sẽ nằm trong FilterDataAsync)
            Task.Run(() => FilterDataAsync());
        }

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
                    string k = TuKhoaTimKiem.Trim().ToLower();
                    query = query.Where(x =>
                        (x.MaHoaDon != null && x.MaHoaDon.ToLower().Contains(k)) ||
                        (x.NguoiTao != null && x.NguoiTao.ToLower().Contains(k)) ||
                        x.SoBan.ToString().Contains(k) // Thêm tìm theo số bàn
                    );
                }

                var resultList = new ObservableCollection<HoaDon>(query.OrderByDescending(h => h.NgayTao));

                // [QUAN TRỌNG] Tính toán thống kê CHÍNH XÁC (Trừ giảm giá)
                decimal totalThucThu = 0;
                int count = resultList.Count;

                if (count > 0)
                {
                    // Công thức đúng: Tổng tiền - Giảm giá
                    totalThucThu = resultList.Sum(x => x.TongTien - x.GiamGia);
                }

                decimal avg = count > 0 ? totalThucThu / count : 0;

                // Cập nhật UI (Main Thread)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DanhSachHoaDon = resultList;

                    // Cập nhật biến hiển thị Tổng Doanh Thu (To bự dưới đáy)
                    TongDoanhThuHienTai = totalThucThu.ToString("N0") + " VNĐ";

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
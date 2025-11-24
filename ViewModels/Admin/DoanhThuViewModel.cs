using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GymManagement.Models;
using GymManagement.Services;
using System.Collections.Generic; // Thêm cái này để dùng IEnumerable

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

        // [MỚI] Biến từ khóa tìm kiếm
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
        }

        private void LoadData()
        {
            _allHoaDons = _repository.GetAll();
            // Ban đầu hiển thị hết
            DanhSachHoaDon = new ObservableCollection<HoaDon>(_allHoaDons);
            TinhToanThongKe();
        }

        // [MỚI] Hàm lọc dữ liệu
        private void FilterData()
        {
            if (_allHoaDons == null) return;

            IEnumerable<HoaDon> query = _allHoaDons;

            // Lọc theo từ khóa (Mã HĐ hoặc Tên nhân viên)
            if (!string.IsNullOrEmpty(TuKhoaTimKiem))
            {
                string k = TuKhoaTimKiem.ToLower();
                query = query.Where(x => x.MaHoaDon.ToLower().Contains(k) ||
                                         (x.NguoiTao != null && x.NguoiTao.ToLower().Contains(k)));
            }

            // Cập nhật danh sách hiển thị
            DanhSachHoaDon = new ObservableCollection<HoaDon>(query);

            // Tính lại thống kê theo danh sách mới lọc
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
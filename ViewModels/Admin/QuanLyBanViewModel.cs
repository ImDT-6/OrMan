using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;
using System.Collections.Generic;

namespace GymManagement.ViewModels
{
    public class QuanLyBanViewModel : INotifyPropertyChanged
    {
        private readonly BanAnRepository _repository;

        public ObservableCollection<BanAn> DanhSachBan { get; set; }

        // Dữ liệu chi tiết bàn đang chọn
        private BanAn _selectedBan;
        public BanAn SelectedBan
        {
            get => _selectedBan;
            set { _selectedBan = value; OnPropertyChanged(); LoadTableDetails(); }
        }

        private HoaDon _currentHoaDon; // Hóa đơn hiện tại của bàn

        private List<ChiTietHoaDon> _chiTietDonHang;
        public List<ChiTietHoaDon> ChiTietDonHang
        {
            get => _chiTietDonHang;
            set { _chiTietDonHang = value; OnPropertyChanged(); }
        }

        private decimal _tongTienCanThu;
        public decimal TongTienCanThu
        {
            get => _tongTienCanThu;
            set { _tongTienCanThu = value; OnPropertyChanged(); }
        }

        public ICommand SelectTableCommand { get; private set; }
        public ICommand CheckoutCommand { get; private set; }

        public QuanLyBanViewModel()
        {
            _repository = new BanAnRepository();
            // Nếu chưa có bàn thì tạo mẫu
            if (_repository.GetAll().Count == 0) _repository.InitTables();

            DanhSachBan = _repository.GetAll();

            SelectTableCommand = new RelayCommand<BanAn>(ban => SelectedBan = ban);
            CheckoutCommand = new RelayCommand<object>(Checkout);
        }

        private void LoadTableDetails()
        {
            if (SelectedBan == null) return;

            if (SelectedBan.TrangThai == "Có Khách")
            {
                // Lấy hóa đơn chưa thanh toán từ DB
                _currentHoaDon = _repository.GetActiveOrder(SelectedBan.SoBan);

                if (_currentHoaDon != null)
                {
                    ChiTietDonHang = _repository.GetOrderDetails(_currentHoaDon.MaHoaDon);
                    TongTienCanThu = _currentHoaDon.TongTien;
                }
                else
                {
                    // Lỗi data: Bàn có khách nhưng ko tìm thấy hóa đơn
                    ChiTietDonHang = null;
                    TongTienCanThu = 0;
                }
            }
            else
            {
                // Bàn trống
                ChiTietDonHang = null;
                TongTienCanThu = 0;
            }
        }

        private void Checkout(object obj)
        {
            if (SelectedBan == null || _currentHoaDon == null) return;

            if (MessageBox.Show($"Thanh toán cho {SelectedBan.TenBan}?\nTổng tiền: {TongTienCanThu:N0} VNĐ",
                                "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Gọi Repository xử lý DB
                _repository.CheckoutTable(SelectedBan.SoBan, _currentHoaDon.MaHoaDon);

                // Cập nhật UI
                SelectedBan.TrangThai = "Trống";
                LoadTableDetails(); // Reset chi tiết bên phải
                MessageBox.Show("Thanh toán thành công! Bàn đã trống.", "Thông báo");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
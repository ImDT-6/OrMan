using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;
using System.Collections.Generic;
using System.Windows.Threading;
using System;
using System.Linq;

namespace GymManagement.ViewModels
{
    public class QuanLyBanViewModel : INotifyPropertyChanged
    {
        private readonly BanAnRepository _repository;
        private DispatcherTimer _timer;

        private ObservableCollection<BanAn> _danhSachBan;
        public ObservableCollection<BanAn> DanhSachBan
        {
            get => _danhSachBan;
            set { _danhSachBan = value; OnPropertyChanged(); }
        }

        private BanAn _selectedBan;
        public BanAn SelectedBan
        {
            get => _selectedBan;
            set { _selectedBan = value; OnPropertyChanged(); LoadTableDetails(); }
        }

        private HoaDon _currentHoaDon;
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
        public ICommand AssignTableCommand { get; private set; }

        public QuanLyBanViewModel()
        {
            _repository = new BanAnRepository();
            if (_repository.GetAll().Count == 0) _repository.InitTables();

            LoadTables();

            SelectTableCommand = new RelayCommand<BanAn>(ban => SelectedBan = ban);
            CheckoutCommand = new RelayCommand<object>(Checkout);
            AssignTableCommand = new RelayCommand<object>(AssignTable);

            // Timer cập nhật mỗi 2 giây (nhanh hơn để thấy ngay)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (s, e) => RefreshTableStatus();
            _timer.Start();
        }

        private void LoadTables()
        {
            DanhSachBan = _repository.GetAll();
        }

        private void RefreshTableStatus()
        {
            // Lấy dữ liệu mới nhất từ DB
            var newData = _repository.GetAll();

            // Cập nhật vào danh sách hiện tại (để giữ nguyên selection)
            foreach (var banMoi in newData)
            {
                var banCu = DanhSachBan.FirstOrDefault(b => b.SoBan == banMoi.SoBan);
                if (banCu != null)
                {
                    // Chỉ cập nhật nếu có thay đổi để tránh nháy giao diện
                    if (banCu.TrangThai != banMoi.TrangThai)
                        banCu.TrangThai = banMoi.TrangThai;

                    if (banCu.YeuCauThanhToan != banMoi.YeuCauThanhToan)
                        banCu.YeuCauThanhToan = banMoi.YeuCauThanhToan;
                }
            }
        }

        private void AssignTable(object obj)
        {
            if (SelectedBan == null) return;

            if (SelectedBan.TrangThai == "Trống")
            {
                // Cập nhật DB
                _repository.UpdateStatus(SelectedBan.SoBan, "Có Khách");
                // Cập nhật ngay UI để Admin thấy liền
                SelectedBan.TrangThai = "Có Khách";
                MessageBox.Show($"Đã xếp bàn {SelectedBan.SoBan} cho khách!", "Thành công");
            }
            else
            {
                MessageBox.Show("Bàn này đang có khách hoặc đã đặt!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadTableDetails()
        {
            if (SelectedBan == null) return;

            if (SelectedBan.TrangThai == "Có Khách")
            {
                _currentHoaDon = _repository.GetActiveOrder(SelectedBan.SoBan);

                if (_currentHoaDon != null)
                {
                    ChiTietDonHang = _repository.GetOrderDetails(_currentHoaDon.MaHoaDon);
                    TongTienCanThu = _currentHoaDon.TongTien;
                }
                else
                {
                    ChiTietDonHang = new List<ChiTietHoaDon>();
                    TongTienCanThu = 0;
                }
            }
            else
            {
                ChiTietDonHang = null;
                TongTienCanThu = 0;
            }
        }

        private void Checkout(object obj)
        {
            if (SelectedBan == null) return;

            if (TongTienCanThu == 0 && SelectedBan.TrangThai != "Trống")
            {
                if (MessageBox.Show($"Bàn chưa gọi món. Bạn muốn hủy bàn/trả bàn?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _repository.UpdateStatus(SelectedBan.SoBan, "Trống");
                    SelectedBan.TrangThai = "Trống";
                    SelectedBan.YeuCauThanhToan = false;
                    LoadTableDetails();
                }
                return;
            }

            if (_currentHoaDon == null) return;

            if (MessageBox.Show($"Thanh toán cho {SelectedBan.TenBan}?\nTổng tiền: {TongTienCanThu:N0} VNĐ",
                                "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Thanh toán trong DB
                _repository.CheckoutTable(SelectedBan.SoBan, _currentHoaDon.MaHoaDon);

                // Cập nhật ngay lập tức trên UI
                SelectedBan.TrangThai = "Trống";
                SelectedBan.YeuCauThanhToan = false;

                LoadTableDetails();
                MessageBox.Show("Thanh toán thành công! Bàn đã trống.", "Thông báo");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
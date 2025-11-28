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

            if (SelectedBan.TrangThai == "Có Khách" || SelectedBan.TrangThai == "Đã Đặt")
            {
                _currentHoaDon = _repository.GetActiveOrder(SelectedBan.SoBan);

                if (_currentHoaDon != null)
                {
                    ChiTietDonHang = _repository.GetOrderDetails(_currentHoaDon.MaHoaDon);
                    TongTienCanThu = _currentHoaDon.TongTien;
                }
                else
                {
                    // Trường hợp bàn đang Có Khách nhưng chưa có đơn (Khách vừa vào)
                    ChiTietDonHang = new List<ChiTietHoaDon>();
                    TongTienCanThu = 0;
                }
            }
            else if (SelectedBan.YeuCauThanhToan)
            {
                // [FIX] Trường hợp bàn Trống nhưng vẫn có yêu cầu thanh toán (Khách gọi phục vụ khi bàn trống)
                ChiTietDonHang = new List<ChiTietHoaDon> { new ChiTietHoaDon { TenMonHienThi = "Yêu cầu hỗ trợ/phục vụ.", DonGia = 0, SoLuong = 1 } };
                TongTienCanThu = 0;
                _currentHoaDon = null; // Quan trọng: Đảm bảo không có Hóa đơn nào được thanh toán
            }
            else
            {
                ChiTietDonHang = null;
                TongTienCanThu = 0;
                _currentHoaDon = null;
            }
        }

        private void Checkout(object obj)
        {
            if (SelectedBan == null) return;

            // 1. [FIX LOGIC]: Xử lý trường hợp Yêu cầu phục vụ/Thanh toán nhưng không có đơn hàng (TongTienCanThu == 0)
            if (SelectedBan.YeuCauThanhToan && TongTienCanThu == 0)
            {
                // Trường hợp này là khách chỉ bấm nút "Gọi nhân viên"
                if (MessageBox.Show($"Bàn {SelectedBan.TenBan} đang yêu cầu hỗ trợ. Bạn xác nhận đã xử lý xong yêu cầu này?", "Xác nhận xử lý", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Reset trạng thái yêu cầu thanh toán
                    _repository.ResolvePaymentRequest(SelectedBan.SoBan);
                    // Nếu bàn đang Có Khách (tức là đã có khách vào), giữ nguyên trạng thái bàn là Có Khách.
                    if (SelectedBan.TrangThai != "Trống")
                    {
                        SelectedBan.YeuCauThanhToan = false;
                    }
                    else
                    {
                        // Nếu bàn đang Trống (vì khách vừa vào và ấn Gọi nhân viên), thì chuyển về Trống hẳn.
                        SelectedBan.TrangThai = "Trống";
                        SelectedBan.YeuCauThanhToan = false;
                        LoadTableDetails();
                    }
                }
                return;
            }

            // 2. Xử lý Thanh toán (Có đơn hàng)
            if (_currentHoaDon == null)
            {
                MessageBox.Show("Không tìm thấy hóa đơn hoạt động để thanh toán.", "Lỗi thanh toán", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
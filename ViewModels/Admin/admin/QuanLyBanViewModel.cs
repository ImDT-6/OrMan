using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Services;
using System.Collections.Generic;
using System.Windows.Threading;
using System;
using System.Linq;
using OrMan.Views.Admin; // [QUAN TRỌNG] Thêm dòng này để gọi ThanhToanWindow

namespace OrMan.ViewModels
{
    public class QuanLyBanViewModel : INotifyPropertyChanged
    {
        // ... (Giữ nguyên các khai báo biến cũ) ...
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
            var newData = _repository.GetAll();
            foreach (var banMoi in newData)
            {
                var banCu = DanhSachBan.FirstOrDefault(b => b.SoBan == banMoi.SoBan);
                if (banCu != null)
                {
                    if (banCu.TrangThai != banMoi.TrangThai) banCu.TrangThai = banMoi.TrangThai;
                    if (banCu.YeuCauThanhToan != banMoi.YeuCauThanhToan) banCu.YeuCauThanhToan = banMoi.YeuCauThanhToan;
                }
            }
        }

        private void AssignTable(object obj)
        {
            if (SelectedBan == null) return;
            if (SelectedBan.TrangThai == "Trống")
            {
                _repository.UpdateStatus(SelectedBan.SoBan, "Có Khách");
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

        // [ĐÃ SỬA] Cập nhật logic Thanh Toán
        private void Checkout(object obj)
        {
            if (SelectedBan == null) return;

            // Trường hợp 1: Bàn có khách nhưng chưa gọi món -> Hủy bàn
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

            // Trường hợp 2: Có tiền -> Mở cửa sổ Thanh Toán mới
            var paymentWindow = new ThanhToanWindow(SelectedBan.TenBan, TongTienCanThu);

            // Làm mờ cửa sổ chính
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            if (paymentWindow.ShowDialog() == true)
            {
                // Người dùng đã bấm "Xác nhận đã thu"
                _repository.CheckoutTable(SelectedBan.SoBan, _currentHoaDon.MaHoaDon);

                SelectedBan.TrangThai = "Trống";
                SelectedBan.YeuCauThanhToan = false;

                LoadTableDetails();
                MessageBox.Show("Thanh toán thành công! Bàn đã trống.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Trả lại độ sáng
            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
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
using OrMan.Views.Admin;

namespace OrMan.ViewModels.Admin
{
    public class QuanLyBanViewModel : INotifyPropertyChanged
    {
        private readonly BanAnRepository _repository;
        private DispatcherTimer _timer;

        // --- Các biến cũ ---
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
            set
            {
                _selectedBan = value;
                OnPropertyChanged();

                if (_selectedBan != null && !IsEditMode)
                {
                    LoadTableDetails();
                }
                else
                {
                    ChiTietDonHang = null;
                    TongTienCanThu = 0;
                }
            }
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

        // --- Biến cho chế độ sửa ---
        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
                if (!value) SelectedBan = null;
            }
        }

        // --- Commands ---
        public ICommand SelectTableCommand { get; private set; }
        public ICommand CheckoutCommand { get; private set; }
        public ICommand AssignTableCommand { get; private set; }
        public ICommand ToggleEditModeCommand { get; private set; }
        public ICommand AddTableCommand { get; private set; }
        public ICommand DeleteTableCommand { get; private set; }

        public QuanLyBanViewModel()
        {
            _repository = new BanAnRepository();
            if (_repository.GetAll().Count == 0) _repository.InitTables();

            LoadTables();

            SelectTableCommand = new RelayCommand<BanAn>(ban =>
            {
                if (IsEditMode) return;
                SelectedBan = ban;
            });

            CheckoutCommand = new RelayCommand<object>(Checkout);
            AssignTableCommand = new RelayCommand<object>(AssignTable);

            ToggleEditModeCommand = new RelayCommand<object>(p => IsEditMode = !IsEditMode);

            // [CẬP NHẬT] Logic thêm bàn với thông báo chi tiết
            AddTableCommand = new RelayCommand<object>(p =>
            {
                _repository.AddTable();
                LoadTables(); // Reload để lấy danh sách mới nhất từ DB

                // Tìm bàn vừa thêm (là bàn có số bàn lớn nhất)
                var banMoi = DanhSachBan.OrderByDescending(b => b.SoBan).FirstOrDefault();

                if (banMoi != null)
                {
                    // Tự động chọn bàn mới để người dùng thấy ngay vị trí
                    SelectedBan = banMoi;

                    // Thông báo cụ thể hơn
                    MessageBox.Show(
                        $"Đã thêm {banMoi.TenBan} thành công!\nTổng số bàn hiện tại: {DanhSachBan.Count}",
                        "Thêm bàn hoàn tất",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            });

            DeleteTableCommand = new RelayCommand<BanAn>(ban =>
            {
                if (ban == null) return;
                var confirm = MessageBox.Show($"Bạn có chắc muốn xóa {ban.TenBan}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.Yes)
                {
                    bool success = _repository.DeleteTable(ban.SoBan);
                    if (success)
                    {
                        LoadTables();
                        if (SelectedBan != null && SelectedBan.SoBan == ban.SoBan) SelectedBan = null;
                        MessageBox.Show($"Đã xóa {ban.TenBan} khỏi hệ thống.", "Đã xóa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Không thể xóa bàn đang có khách hoặc chưa thanh toán!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });

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

            var paymentWindow = new ThanhToanWindow(SelectedBan.TenBan, TongTienCanThu);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            if (paymentWindow.ShowDialog() == true)
            {
                _repository.CheckoutTable(SelectedBan.SoBan, _currentHoaDon.MaHoaDon);
                SelectedBan.TrangThai = "Trống";
                SelectedBan.YeuCauThanhToan = false;
                LoadTableDetails();
                MessageBox.Show("Thanh toán thành công! Bàn đã trống.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
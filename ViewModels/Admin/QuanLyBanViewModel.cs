using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Cần cho Task.Run
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Services;
using OrMan.Views.Admin;

namespace OrMan.ViewModels.Admin
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

        public ICommand SelectTableCommand { get; private set; }
        public ICommand CheckoutCommand { get; private set; }
        public ICommand AssignTableCommand { get; private set; }
        public ICommand ToggleEditModeCommand { get; private set; }
        public ICommand AddTableCommand { get; private set; }
        public ICommand DeleteTableCommand { get; private set; }
        public ICommand PrintBillCommand { get; private set; }
        public ICommand SaveTableSettingsCommand { get; private set; }

        public QuanLyBanViewModel()
        {
            _repository = new BanAnRepository();
            if (_repository.GetAll().Count == 0) _repository.InitTables();

            LoadTables();

            SelectTableCommand = new RelayCommand<BanAn>(ban => SelectedBan = ban);
            AssignTableCommand = new RelayCommand<object>(AssignTable);
            ToggleEditModeCommand = new RelayCommand<object>(p => IsEditMode = !IsEditMode);

            AddTableCommand = new RelayCommand<object>(p =>
            {
                _repository.AddTable();
                LoadTables();
                var banMoi = DanhSachBan.OrderByDescending(b => b.SoBan).FirstOrDefault();
                if (banMoi != null)
                {
                    SelectedBan = banMoi;
                    MessageBox.Show($"Đã thêm {banMoi.TenBan} thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        MessageBox.Show($"Đã xóa {ban.TenBan}.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Không thể xóa bàn đang có khách!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });

            // [TỐI ƯU] Tăng thời gian Timer lên 5s để giảm tải DB
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += async (s, e) => await RefreshTableStatusAsync();
            _timer.Start();

            PrintBillCommand = new RelayCommand<object>(p =>
            {
                if (SelectedBan == null || _currentHoaDon == null) return;
                var printWindow = new HoaDonWindow(SelectedBan.TenBan, _currentHoaDon, ChiTietDonHang, SelectedBan.HinhThucThanhToan ?? "Tiền mặt");
                if (printWindow.ShowDialog() == true)
                {
                    SelectedBan.DaInTamTinh = true;
                }
            });

            SaveTableSettingsCommand = new RelayCommand<object>(p =>
            {
                if (SelectedBan == null) return;
                _repository.UpdateTableInfo(SelectedBan.SoBan, SelectedBan.TenGoi);
                MessageBox.Show($"Đã lưu thông tin {SelectedBan.TenBan} thành công!", "Đã Lưu", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            CheckoutCommand = new RelayCommand<object>(Checkout);
        }

        private void LoadTables()
        {
            // Có thể chạy Async nếu muốn, nhưng ở đây load lần đầu nên chấp nhận Sync cũng được
            DanhSachBan = _repository.GetAll();
        }

        // [QUAN TRỌNG] Hàm cập nhật trạng thái bàn Real-time chạy Async
        private async Task RefreshTableStatusAsync()
        {
            try
            {
                // 1. Lấy dữ liệu mới từ DB ở Background Thread
                var newData = await Task.Run(() => _repository.GetAll());

                // 2. Cập nhật UI
                // Lưu ý: Không gán trực tiếp DanhSachBan = newData vì sẽ làm mất Selection/Focus
                // Chỉ cập nhật các thuộc tính thay đổi
                foreach (var banMoi in newData)
                {
                    var banCu = DanhSachBan.FirstOrDefault(b => b.SoBan == banMoi.SoBan);
                    if (banCu != null)
                    {
                        if (banCu.TrangThai != banMoi.TrangThai) banCu.TrangThai = banMoi.TrangThai;
                        if (banCu.YeuCauThanhToan != banMoi.YeuCauThanhToan) banCu.YeuCauThanhToan = banMoi.YeuCauThanhToan;
                        if (banCu.HinhThucThanhToan != banMoi.HinhThucThanhToan) banCu.HinhThucThanhToan = banMoi.HinhThucThanhToan;
                        if (banCu.YeuCauHoTro != banMoi.YeuCauHoTro) banCu.YeuCauHoTro = banMoi.YeuCauHoTro;
                    }
                }

                if (SelectedBan != null && SelectedBan.TrangThai == "Có Khách")
                {
                    LoadTableDetails(); // Cân nhắc chuyển hàm này thành Async nếu chi tiết đơn quá nhiều
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Refresh Error: " + ex.Message);
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
                MessageBox.Show("Bàn này đang có khách!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (SelectedBan == null || _currentHoaDon == null) return;

            string thongBao = $"Bạn có chắc chắn muốn kết thúc đơn hàng bàn {SelectedBan.TenBan}?\n" +
                              $"Tổng tiền thu: {TongTienCanThu:N0} VNĐ";

            var ketQua = MessageBox.Show(thongBao, "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (ketQua == MessageBoxResult.Yes)
            {
                try
                {
                    _repository.CheckoutTable(SelectedBan.SoBan, _currentHoaDon.MaHoaDon);

                    SelectedBan.TrangThai = "Trống";
                    SelectedBan.YeuCauThanhToan = false;
                    SelectedBan.HinhThucThanhToan = null;
                    SelectedBan.DaInTamTinh = false;
                    SelectedBan.YeuCauHoTro = null;

                    LoadTableDetails();

                    MessageBox.Show("Thanh toán thành công! Đã trả bàn.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi thanh toán: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // [QUAN TRỌNG] Hủy Timer
        public void Cleanup()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
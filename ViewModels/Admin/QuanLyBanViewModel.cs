using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

        // --- Các biến Binding ---
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

                // Khi chọn bàn, nếu không phải chế độ sửa thì load chi tiết đơn
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

        // --- Commands ---
        public ICommand SelectTableCommand { get; private set; }
        public ICommand CheckoutCommand { get; private set; }
        public ICommand AssignTableCommand { get; private set; }
        public ICommand ToggleEditModeCommand { get; private set; }
        public ICommand AddTableCommand { get; private set; }
        public ICommand DeleteTableCommand { get; private set; }
        public ICommand PrintBillCommand { get; private set; }

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

            AssignTableCommand = new RelayCommand<object>(AssignTable);
            ToggleEditModeCommand = new RelayCommand<object>(p => IsEditMode = !IsEditMode);

            // --- Logic Thêm Bàn ---
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

            // --- Logic Xóa Bàn ---
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

            // --- Timer cập nhật trạng thái bàn real-time ---
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (s, e) => RefreshTableStatus();
            _timer.Start();

            // ==========================================================
            // [QUAN TRỌNG] Logic Nút IN TẠM TÍNH (Sửa lỗi In/Hủy)
            // ==========================================================
            PrintBillCommand = new RelayCommand<object>(p =>
            {
                if (SelectedBan == null || _currentHoaDon == null) return;

                var printWindow = new HoaDonWindow(
                    SelectedBan.TenBan,
                    _currentHoaDon,
                    ChiTietDonHang,
                    SelectedBan.HinhThucThanhToan ?? "Tiền mặt"
                );

                // ShowDialog trả về true nết bấm In, false/null nếu bấm Hủy
                bool? ketQua = printWindow.ShowDialog();

                if (ketQua == true)
                {
                    // Chỉ khi bấm IN thật thì mới set trạng thái này
                    // Lúc này giao diện (nếu Binding đúng) sẽ tự đổi sang chữ "In Lại"
                    SelectedBan.DaInTamTinh = true;
                    MessageBox.Show("Đã nhận lệnh In thành công! Trạng thái bàn đã đổi.", "Test");
                }
            });

            // ==========================================================
            // Logic THANH TOÁN & TRẢ BÀN
            // ==========================================================
            CheckoutCommand = new RelayCommand<object>(Checkout);
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
                    // Chỉ cập nhật trạng thái từ DB, KHÔNG cập nhật DaInTamTinh
                    if (banCu.TrangThai != banMoi.TrangThai) banCu.TrangThai = banMoi.TrangThai;
                    if (banCu.YeuCauThanhToan != banMoi.YeuCauThanhToan) banCu.YeuCauThanhToan = banMoi.YeuCauThanhToan;

                    // XÓA hoặc COMMENT dòng này đi:
                    // if (banCu.DaInTamTinh != banMoi.DaInTamTinh) banCu.DaInTamTinh = banMoi.DaInTamTinh;
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

        // Trong file QuanLyBanViewModel.cs
        private void Checkout(object obj)
        {
            if (SelectedBan == null || _currentHoaDon == null) return;

            // 1. Tạo nội dung thông báo xác nhận
            string thongBao = $"Bạn có chắc chắn muốn kết thúc đơn hàng bàn {SelectedBan.TenBan}?\n" +
                              $"Tổng tiền thu: {TongTienCanThu:N0} VNĐ";

            // 2. Hiện MessageBox hỏi Yes/No
            var ketQua = MessageBox.Show(thongBao, "Xác nhận thanh toán",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            // 3. Nếu chọn Yes thì mới xử lý
            if (ketQua == MessageBoxResult.Yes)
            {
                try
                {
                    // Gọi xuống DB để chốt đơn
                    _repository.CheckoutTable(SelectedBan.SoBan, _currentHoaDon.MaHoaDon);

                    // Reset trạng thái bàn về Trống
                    SelectedBan.TrangThai = "Trống";
                    SelectedBan.YeuCauThanhToan = false;
                    SelectedBan.HinhThucThanhToan = null;
                    SelectedBan.DaInTamTinh = false; // Reset trạng thái in
                    SelectedBan.YeuCauHoTro = null;

                    // Load lại giao diện
                    LoadTableDetails();

                    MessageBox.Show("Thanh toán thành công! Đã trả bàn.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi thanh toán: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        // Dùng ObservableCollection nhưng KHÔNG set lại new instance để tránh mất cuộn
        public ObservableCollection<BanAn> DanhSachBan { get; set; } = new ObservableCollection<BanAn>();

        private decimal _tienGiamGia;
        public decimal TienGiamGia
        {
            get => _tienGiamGia;
            set { _tienGiamGia = value; OnPropertyChanged(); OnPropertyChanged(nameof(TongTienThanhToan)); }
        }

        public decimal TongTienThanhToan => TongTienCanThu - TienGiamGia;

        private BanAn _selectedBan;
        public BanAn SelectedBan
        {
            get => _selectedBan;
            set
            {
                if (_selectedBan != value)
                {
                    _selectedBan = value;
                    OnPropertyChanged();

                    if (_selectedBan != null && !IsEditMode)
                    {
                        LoadTableDetailsAsync();
                    }
                    else
                    {
                        ChiTietDonHang = null;
                        TongTienCanThu = 0;
                        TienGiamGia = 0;
                    }
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

        // Commands
        public ICommand SelectTableCommand { get; private set; }
        public ICommand CheckoutCommand { get; private set; }
        public ICommand AssignTableCommand { get; private set; }
        public ICommand ToggleEditModeCommand { get; private set; }
        public ICommand AddTableCommand { get; private set; }
        public ICommand DeleteTableCommand { get; private set; }
        public ICommand PrintBillCommand { get; private set; }
        public ICommand SaveTableSettingsCommand { get; private set; }
        public ICommand CancelTableCommand { get; private set; }

        public QuanLyBanViewModel()
        {
            _repository = new BanAnRepository();
            InitializeCommands();

            Task.Run(async () =>
            {
                if (_repository.GetAll().Count == 0) _repository.InitTables();
                await RefreshTableListAsync();
            });

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += async (s, e) => await RefreshTableListAsync();
            _timer.Start();
        }

        private void InitializeCommands()
        {
            SelectTableCommand = new RelayCommand<BanAn>(ban => SelectedBan = ban);
            ToggleEditModeCommand = new RelayCommand<object>(p => IsEditMode = !IsEditMode);
            AssignTableCommand = new RelayCommand<object>(AssignTable);
            CancelTableCommand = new RelayCommand<object>(CancelTable);
            CheckoutCommand = new RelayCommand<object>(Checkout);

            // [FIX] Thêm bàn thông minh: Tìm số nhỏ nhất còn thiếu
            AddTableCommand = new RelayCommand<object>(p =>
            {
                Task.Run(async () =>
                {
                    var allTables = _repository.GetAll();
                    var currentIds = allTables.Select(b => b.SoBan).OrderBy(x => x).ToList();

                    int nextId = 1;
                    foreach (var id in currentIds)
                    {
                        if (id == nextId) nextId++;
                        else break; // Tìm thấy khe hở (ví dụ 1, 3 -> thiếu 2)
                    }

                    // Gọi hàm AddTable mới có tham số ID
                    bool success = _repository.AddTable(nextId);

                    if (success)
                    {
                        await RefreshTableListAsync();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var banMoi = DanhSachBan.FirstOrDefault(b => b.SoBan == nextId);
                            if (banMoi != null) SelectedBan = banMoi;
                            MessageBox.Show($"Đã thêm lại bàn số {nextId} thành công!", "Thông báo");
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Lỗi thêm bàn. Kiểm tra lại Database.", "Lỗi"));
                    }
                });
            });

            DeleteTableCommand = new RelayCommand<BanAn>(ban =>
            {
                if (ban == null) return;
                if (MessageBox.Show($"Bạn có chắc muốn xóa bàn số {ban.SoBan} ({ban.TenGoi})?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Task.Run(async () =>
                    {
                        bool success = _repository.DeleteTable(ban.SoBan);
                        if (success)
                        {
                            await RefreshTableListAsync();
                            Application.Current.Dispatcher.Invoke(() => SelectedBan = null);
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Không thể xóa bàn đang có khách!", "Lỗi"));
                        }
                    });
                }
            });

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
                Task.Run(() =>
                {
                    _repository.UpdateTableInfo(SelectedBan.SoBan, SelectedBan.TenGoi);
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Đã lưu tên bàn thành công!", "Thông báo"));
                });
            });
        }

        // [FIX] Đồng bộ thông minh: Chỉ cập nhật item thay đổi, không load lại cả list
        private async Task RefreshTableListAsync()
        {
            try
            {
                var newData = await Task.Run(() => _repository.GetAll());

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 1. Xóa bàn không còn trong DB
                    var itemsToRemove = DanhSachBan.Where(x => !newData.Any(n => n.SoBan == x.SoBan)).ToList();
                    foreach (var item in itemsToRemove) DanhSachBan.Remove(item);

                    // 2. Cập nhật hoặc Thêm mới
                    foreach (var newBan in newData)
                    {
                        var existingBan = DanhSachBan.FirstOrDefault(b => b.SoBan == newBan.SoBan);
                        if (existingBan != null)
                        {
                            // Cập nhật các trường thay đổi
                            if (existingBan.TrangThai != newBan.TrangThai) existingBan.TrangThai = newBan.TrangThai;
                            if (existingBan.YeuCauThanhToan != newBan.YeuCauThanhToan) existingBan.YeuCauThanhToan = newBan.YeuCauThanhToan;
                            if (existingBan.HinhThucThanhToan != newBan.HinhThucThanhToan) existingBan.HinhThucThanhToan = newBan.HinhThucThanhToan;
                            if (existingBan.TenGoi != newBan.TenGoi) existingBan.TenGoi = newBan.TenGoi;
                            if (existingBan.DaInTamTinh != newBan.DaInTamTinh) existingBan.DaInTamTinh = newBan.DaInTamTinh;
                        }
                        else
                        {
                            // Chèn đúng vị trí số thứ tự
                            int insertIndex = 0;
                            while (insertIndex < DanhSachBan.Count && DanhSachBan[insertIndex].SoBan < newBan.SoBan)
                            {
                                insertIndex++;
                            }
                            DanhSachBan.Insert(insertIndex, newBan);
                        }
                    }
                });

                if (SelectedBan != null && SelectedBan.TrangThai == "Có Khách")
                {
                    await LoadTableDetailsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Sync Error: " + ex.Message);
            }
        }

        private async Task LoadTableDetailsAsync()
        {
            if (SelectedBan == null || SelectedBan.TrangThai != "Có Khách") return;

            var details = await Task.Run(() =>
            {
                var hd = _repository.GetActiveOrder(SelectedBan.SoBan);
                var list = hd != null ? _repository.GetOrderDetails(hd.MaHoaDon) : new List<ChiTietHoaDon>();
                return new { HoaDon = hd, List = list };
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                _currentHoaDon = details.HoaDon;
                ChiTietDonHang = details.List;
                TongTienCanThu = _currentHoaDon?.TongTien ?? 0;
                TienGiamGia = _currentHoaDon?.GiamGia ?? 0;
            });
        }

        private void AssignTable(object obj)
        {
            if (SelectedBan == null || SelectedBan.TrangThai != "Trống") return;

            Task.Run(async () =>
            {
                if (_repository.CheckInTable(SelectedBan.SoBan))
                {
                    await RefreshTableListAsync();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Lỗi mở bàn."));
                }
            });
        }

        private void Checkout(object obj)
        {
            if (SelectedBan == null || _currentHoaDon == null) return;

            if (MessageBox.Show($"Thanh toán bàn {SelectedBan.TenBan}?\nTổng: {TongTienThanhToan:N0} đ", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Task.Run(async () =>
                {
                    _repository.CheckoutTable(SelectedBan.SoBan, _currentHoaDon.MaHoaDon);
                    await RefreshTableListAsync();
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Đã thanh toán!"));
                });
            }
        }

        private void CancelTable(object obj)
        {
            if (SelectedBan == null || SelectedBan.TrangThai != "Có Khách") return;

            if (MessageBox.Show($"HỦY bàn {SelectedBan.TenBan}? Dữ liệu sẽ mất.", "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Task.Run(async () =>
                {
                    _repository.CancelTableSession(SelectedBan.SoBan);
                    await RefreshTableListAsync();
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Đã hủy bàn."));
                });
            }
        }

        public void Cleanup()
        {
            _timer?.Stop();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
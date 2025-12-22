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

        private ObservableCollection<BanAn> _danhSachBan;
        public ObservableCollection<BanAn> DanhSachBan
        {
            get => _danhSachBan;
            set { _danhSachBan = value; OnPropertyChanged(); }
        }
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
                _selectedBan = value;
                OnPropertyChanged();

                if (_selectedBan != null && !IsEditMode)
                {
                    LoadTableDetailsAsync(); // [TỐI ƯU] Async
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
        public ICommand CancelTableCommand { get; private set; }

        public QuanLyBanViewModel()
        {
            _repository = new BanAnRepository();

            // [TỐI ƯU] Chạy ngầm việc khởi tạo và load bàn
            Task.Run(async () =>
            {
                if (_repository.GetAll().Count == 0) _repository.InitTables();
                await LoadTablesAsync();
            });

            SelectTableCommand = new RelayCommand<BanAn>(ban => SelectedBan = ban);
            AssignTableCommand = new RelayCommand<object>(AssignTable);
            ToggleEditModeCommand = new RelayCommand<object>(p => IsEditMode = !IsEditMode);
            CancelTableCommand = new RelayCommand<object>(CancelTable);

            AddTableCommand = new RelayCommand<object>(p =>
            {
                Task.Run(async () =>
                {
                    _repository.AddTable();
                    await LoadTablesAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var banMoi = DanhSachBan.OrderByDescending(b => b.SoBan).FirstOrDefault();
                        if (banMoi != null)
                        {
                            SelectedBan = banMoi;
                            MessageBox.Show($"Đã thêm {banMoi.TenBan} thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    });
                });
            });

            DeleteTableCommand = new RelayCommand<BanAn>(ban =>
            {
                if (ban == null) return;
                var confirm = MessageBox.Show($"Bạn có chắc muốn xóa {ban.TenBan}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.Yes)
                {
                    Task.Run(async () =>
                    {
                        bool success = _repository.DeleteTable(ban.SoBan);
                        if (success)
                        {
                            await LoadTablesAsync();
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (SelectedBan != null && SelectedBan.SoBan == ban.SoBan) SelectedBan = null;
                                MessageBox.Show($"Đã xóa {ban.TenBan}.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                                MessageBox.Show("Không thể xóa bàn đang có khách!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error));
                        }
                    });
                }
            });

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += async (s, e) => await RefreshTableStatusAsync();
            _timer.Start();

            PrintBillCommand = new RelayCommand<object>(p =>
            {
                if (SelectedBan == null || _currentHoaDon == null) return;
                // In ấn vẫn phải chạy trên UI Thread để hiện Window
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
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"Đã lưu thông tin {SelectedBan.TenBan} thành công!", "Đã Lưu", MessageBoxButton.OK, MessageBoxImage.Information));
                });
            });

            CheckoutCommand = new RelayCommand<object>(Checkout);
        }

        private async Task LoadTablesAsync()
        {
            var data = await Task.Run(() => _repository.GetAll());
            Application.Current.Dispatcher.Invoke(() => DanhSachBan = data);
        }

        private async Task RefreshTableStatusAsync()
        {
            try
            {
                var newData = await Task.Run(() => _repository.GetAll());

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
                    // Refresh chi tiết đơn nếu bàn đang mở
                    await LoadTableDetailsAsync();
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
                // Chạy bất đồng bộ để UI mượt mà
                Task.Run(async () =>
                {
                    // Gọi hàm mới vừa viết bên Repository
                    bool thanhCong = _repository.CheckInTable(SelectedBan.SoBan);

                    if (thanhCong)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            // Cập nhật giao diện
                            SelectedBan.TrangThai = "Có Khách";

                            // QUAN TRỌNG: Load lại dữ liệu để lấy được MaHoaDon vừa tạo
                            // Nếu không có dòng này, bạn thêm món sẽ bị lỗi vì chưa nhận diện được hóa đơn mới
                            await LoadTableDetailsAsync();

                            // (Tùy chọn) Thông báo nhỏ
                            // MessageBox.Show($"Đã mở bàn {SelectedBan.TenBan}!", "Thành công");
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show("Không thể mở bàn. Có thể bàn đang có khách hoặc lỗi kết nối.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });
            }
            else
            {
                MessageBox.Show("Bàn này đang có khách!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadTableDetailsAsync()
        {
            if (SelectedBan == null) return;

            if (SelectedBan.TrangThai == "Có Khách")
            {
                var details = await Task.Run(() =>
                {
                    // Lấy hóa đơn đang hoạt động
                    var hd = _repository.GetActiveOrder(SelectedBan.SoBan);
                    // Lấy chi tiết món
                    var list = hd != null ? _repository.GetOrderDetails(hd.MaHoaDon) : new List<ChiTietHoaDon>();

                    return new { HoaDon = hd, List = list };
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _currentHoaDon = details.HoaDon;

                    // Nếu ChiTietDonHang là ObservableCollection thì cần bọc lại, nếu là List thì giữ nguyên
                    // (Tùy theo khai báo của bạn, đoạn này mình giữ theo code gốc của bạn)
                    ChiTietDonHang = details.List;

                    TongTienCanThu = _currentHoaDon?.TongTien ?? 0;

                    // [THÊM DÒNG NÀY] Lấy tiền giảm giá từ Hóa đơn lên giao diện
                    TienGiamGia = _currentHoaDon?.GiamGia ?? 0;
                });
            }
            else
            {
                ChiTietDonHang = null;
                TongTienCanThu = 0;
                // [THÊM DÒNG NÀY] Reset về 0 khi bàn trống
                TienGiamGia = 0;
            }
        }

        private void Checkout(object obj)
        {
            if (SelectedBan == null || _currentHoaDon == null) return;

            string thongBao = $"Bạn có chắc chắn muốn kết thúc đơn hàng bàn {SelectedBan.TenBan}?\n" +
                              $"Tổng tiền thu: {TongTienThanhToan:N0} VNĐ";

            if (MessageBox.Show(thongBao, "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        _repository.CheckoutTable(SelectedBan.SoBan, _currentHoaDon.MaHoaDon);

                        // Sau khi DB xong, cập nhật UI
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SelectedBan.TrangThai = "Trống";
                            SelectedBan.YeuCauThanhToan = false;
                            SelectedBan.HinhThucThanhToan = null;
                            SelectedBan.DaInTamTinh = false;
                            SelectedBan.YeuCauHoTro = null;
                        });

                        await LoadTableDetailsAsync();

                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show("Thanh toán thành công! Đã trả bàn.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information));
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show("Lỗi thanh toán: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });
            }
        }

        public void Cleanup()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }


        private void CancelTable(object obj)
        {
            if (SelectedBan == null || SelectedBan.TrangThai != "Có Khách") return;

            // Cảnh báo trước khi hủy
            var result = MessageBox.Show($"Bạn có chắc muốn HỦY phục vụ bàn {SelectedBan.TenBan}?\n(Hóa đơn hiện tại sẽ bị xóa hoàn toàn)",
                                         "Xác nhận hủy",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // Gọi hàm Repository vừa viết
                        _repository.CancelTableSession(SelectedBan.SoBan);

                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            // Cập nhật UI
                            SelectedBan.TrangThai = "Trống";

                            // Load lại để xóa sạch thông tin hiển thị bên phải
                            await LoadTableDetailsAsync();

                            MessageBox.Show("Đã hủy bàn thành công. Trạng thái về Trống.", "Thông báo");
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show("Lỗi hủy bàn: " + ex.Message));
                    }
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
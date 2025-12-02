using OrMan.Helpers;
using OrMan.Models;
using OrMan.Services;
using OrMan.Views.User;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Services;
using OrMan.Views.User;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace OrMan.ViewModels.User
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repo;
        private readonly BanAnRepository _banRepo;
        private ObservableCollection<MonAn> _allMonAn;

        // Biến lưu bàn hiện tại
        private int _currentTable;
        public int CurrentTable
        {
            get => _currentTable;
            set { _currentTable = value; OnPropertyChanged(); OnPropertyChanged(nameof(TableDisplayText)); }
        }

        public string TableDisplayText => _currentTable > 0 ? $"{_currentTable:00}" : "--";

        private ObservableCollection<MonAn> _menuHienThi;
        public ObservableCollection<MonAn> MenuHienThi
        {
            get => _menuHienThi;
            set { _menuHienThi = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CartItem> GioHang { get; set; } = new ObservableCollection<CartItem>();

        private decimal _tongTienCart;
        public decimal TongTienCart
        {
            get => _tongTienCart;
            set { _tongTienCart = value; OnPropertyChanged(); }
        }

        private int _tongSoLuong;
        public int TongSoLuong
        {
            get => _tongSoLuong;
            set { _tongSoLuong = value; OnPropertyChanged(); }
        }

        public ICommand CallStaffCommand { get; private set; }
        public ICommand ChonBanCommand { get; private set; }

        public UserViewModel()
        {
            _repo = new ThucDonRepository();
            _banRepo = new BanAnRepository();
            LoadData();

            CallStaffCommand = new RelayCommand<object>(CallStaff);
            ChonBanCommand = new RelayCommand<object>(OpenChonBanWindow);
        }

        private void OpenChonBanWindow(object obj)
        {
            var wd = new ChonBanWindow();
            if (wd.ShowDialog() == true)
            {
                CurrentTable = wd.SelectedTableId;
            }
        }

        private void LoadData()
        {
            _allMonAn = _repo.GetAvailableMenu();
            FilterMenu("Mì Cay");
        }

        public void FilterMenu(string loai)
        {
            if (_allMonAn == null) return;

            if (loai == "Mì Cay")
            {
                MenuHienThi = new ObservableCollection<MonAn>(_allMonAn.Where(x => x is MonMiCay));
            }
            else
            {
                MenuHienThi = new ObservableCollection<MonAn>(_allMonAn.OfType<MonPhu>().Where(x => x.TheLoai == loai));
            }
        }

        public void AddToCart(MonAn mon, int sl, int capDo, string ghiChu)
        {
            var itemDaCo = GioHang.FirstOrDefault(x => x.MonAn.MaMon == mon.MaMon
                                                    && x.CapDoCay == capDo
                                                    && x.GhiChu == ghiChu);

            if (itemDaCo != null)
            {
                itemDaCo.SoLuong += sl;
            }
            else
            {
                var item = new CartItem(mon, sl, capDo, ghiChu);
                GioHang.Add(item);
            }

            UpdateCartInfo();
        }

        private void UpdateCartInfo()
        {
            TongTienCart = GioHang.Sum(x => x.ThanhTien);
            TongSoLuong = GioHang.Sum(x => x.SoLuong);
        }

        // [SỬA ĐỔI] Logic gọi thanh toán chặt chẽ hơn
        private void CallStaff(object obj)
        {
            // 1. Bắt buộc chọn bàn trước
            if (CurrentTable <= 0)
            {
                OpenChonBanWindow(null);
                if (CurrentTable <= 0) return;
            }

            // 2. Kiểm tra trạng thái đơn hàng của bàn
            var activeOrder = _banRepo.GetActiveOrder(CurrentTable);
            bool hasActiveOrder = (activeOrder != null);

            // 3. Mở cửa sổ chọn loại yêu cầu
            // Truyền trạng thái đơn hàng vào để quyết định có hiện nút Thanh Toán hay không
            var requestWindow = new SupportRequestWindow(hasActiveOrder);

            // Hiệu ứng làm mờ màn hình chính
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            if (requestWindow.ShowDialog() == true)
            {
                if (requestWindow.SelectedRequest == RequestType.Checkout)
                {
                    // --- TRƯỜNG HỢP 1: GỌI THANH TOÁN ---
                    // Logic cũ: Kiểm tra kỹ lại lần nữa cho chắc
                    if (activeOrder == null)
                    {
                        if (GioHang.Count > 0)
                            MessageBox.Show("Bạn chưa gửi đơn bếp. Vui lòng bấm 'Gửi Đơn' trước.", "Chưa có đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
                        else
                            MessageBox.Show("Bàn chưa có món nào. Vui lòng gọi món trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        _banRepo.RequestPayment(CurrentTable);
                        MessageBox.Show($"Đã gửi yêu cầu THANH TOÁN cho Bàn {CurrentTable}!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (requestWindow.SelectedRequest == RequestType.Support)
                {
                    // --- TRƯỜNG HỢP 2: GỌI HỖ TRỢ / NHẮN TIN ---
                    string msg = requestWindow.SupportMessage;

                    // [MỚI] Lưu trực tiếp vào Database để Admin thấy
                    _banRepo.SendSupportRequest(CurrentTable, msg);

                    MessageBox.Show($"Đã gửi lời nhắn đến nhân viên:\n\"{msg}\"\n\nNhân viên sẽ đến bàn {CurrentTable} ngay!", "Đã gửi yêu cầu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            // Trả lại độ sáng
            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        public bool SubmitOrder()
        {
            if (GioHang.Count == 0) return false;

            if (CurrentTable <= 0)
            {
                MessageBox.Show("Vui lòng chọn số bàn bạn đang ngồi trước khi gửi đơn!", "Chưa chọn bàn", MessageBoxButton.OK, MessageBoxImage.Warning);
                OpenChonBanWindow(null);
                if (CurrentTable <= 0) return false;
            }

            // Gọi hàm tạo/gộp đơn (Hàm này đã được update ở bước trước để gộp đơn cũ)
            _repo.CreateOrder(CurrentTable, TongTienCart, "Đơn từ Tablet", GioHang);

            GioHang.Clear();
            UpdateCartInfo();
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
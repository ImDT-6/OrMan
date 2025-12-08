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
using OrMan.Data; // [QUAN TRỌNG] Nhớ thêm cái này để dùng MenuContext

namespace OrMan.ViewModels.User
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repo;
        private readonly BanAnRepository _banRepo;
        private ObservableCollection<MonAn> _allMonAn;

        // [THÊM MỚI] Biến lưu thông tin khách hàng đang đăng nhập
        private KhachHang _currentCustomer;
        public KhachHang CurrentCustomer
        {
            get => _currentCustomer;
            set
            {
                _currentCustomer = value;
                OnPropertyChanged();
                // Có thể thêm logic cập nhật câu chào ở đây nếu muốn
            }
        }

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

        // [THÊM MỚI] Hàm kiểm tra và đăng ký Khách Hàng nhanh
        public KhachHang CheckMember(string phoneNumber)
        {
            using (var db = new MenuContext())
            {
                // 1. Tìm trong DB xem có SĐT này chưa
                var khach = db.KhachHangs.FirstOrDefault(k => k.SoDienThoai == phoneNumber);

                if (khach == null)
                {
                    // 2. Nếu chưa -> Tạo mới (Khách vãng lai đăng ký nhanh)
                    khach = new KhachHang
                    {
                        SoDienThoai = phoneNumber,
                        HoTen = "Khách Mới", // Tên tạm, sau này update sau
                        HangThanhVien = "Mới",
                        DiemTichLuy = 0
                    };
                    db.KhachHangs.Add(khach);
                    db.SaveChanges();
                }

                // 3. Lưu vào biến toàn cục của ViewModel để giao diện hiển thị
                CurrentCustomer = khach;

                return khach;
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

        private void CallStaff(object obj)
        {
            if (CurrentTable <= 0)
            {
                OpenChonBanWindow(null);
                if (CurrentTable <= 0) return;
            }

            var activeOrder = _banRepo.GetActiveOrder(CurrentTable);
            bool hasActiveOrder = (activeOrder != null);

            var requestWindow = new SupportRequestWindow(hasActiveOrder);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            if (requestWindow.ShowDialog() == true)
            {
                if (requestWindow.SelectedRequest == RequestType.Checkout)
                {
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
                    string msg = requestWindow.SupportMessage;
                    _banRepo.SendSupportRequest(CurrentTable, msg);
                    MessageBox.Show($"Đã gửi lời nhắn đến nhân viên:\n\"{msg}\"\n\nNhân viên sẽ đến bàn {CurrentTable} ngay!", "Đã gửi yêu cầu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

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

            // [LƯU Ý] Hàm CreateOrder hiện tại của bạn chưa hỗ trợ lưu CustomerID.
            // Tạm thời mình vẫn giữ nguyên để code chạy được. 
            // Sau này nếu muốn lưu khách hàng vào hóa đơn, bạn cần sửa Repo nhận thêm tham số: (CurrentCustomer?.KhachHangID)
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
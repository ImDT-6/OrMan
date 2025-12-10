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
using OrMan.Data;

namespace OrMan.ViewModels.User
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repo;
        private readonly BanAnRepository _banRepo;
        private ObservableCollection<MonAn> _allMonAn;

        private KhachHang _currentCustomer;
        public KhachHang CurrentCustomer
        {
            get => _currentCustomer;
            set { _currentCustomer = value; OnPropertyChanged(); }
        }

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

        // ... (Các hàm CheckMember, LoadData, FilterMenu giữ nguyên) ...

        // --- [SECTION GIỎ HÀNG] ---

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

        // [MỚI] Tăng số lượng 1 món
        public void TangSoLuongMon(CartItem item)
        {
            if (item != null)
            {
                item.SoLuong++; // Class CartItem cần NotifyPropertyChanged để UI cập nhật số
                UpdateCartInfo(); // Tính lại tổng tiền
            }
        }

        // [MỚI] Giảm số lượng 1 món
        public void GiamSoLuongMon(CartItem item)
        {
            if (item != null)
            {
                if (item.SoLuong > 1)
                {
                    item.SoLuong--;
                    UpdateCartInfo();
                }
                else
                {
                    // Nếu đang là 1 mà bấm trừ -> Hỏi có muốn xóa luôn không
                    var result = MessageBox.Show($"Bạn có muốn xóa món '{item.TenHienThi}' khỏi giỏ hàng?",
                                                 "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        XoaMonKhoiGio(item);
                    }
                }
            }
        }

        // Hàm xóa hẳn món khỏi giỏ
        public void XoaMonKhoiGio(CartItem item)
        {
            if (item != null && GioHang.Contains(item))
            {
                GioHang.Remove(item);
                UpdateCartInfo();
            }
        }

        private void UpdateCartInfo()
        {
            // Cần đảm bảo Property ThanhTien trong CartItem trả về (DonGia * SoLuong)
            TongTienCart = GioHang.Sum(x => x.ThanhTien);
            TongSoLuong = GioHang.Sum(x => x.SoLuong);
        }

        // ... (Các hàm SubmitOrder, CallStaff giữ nguyên) ...

        // --- CÁC HÀM CŨ GIỮ NGUYÊN BÊN DƯỚI ĐỂ ĐẢM BẢO KHÔNG LỖI ---
        private void OpenChonBanWindow(object obj)
        {
            var wd = new ChonBanWindow();
            if (wd.ShowDialog() == true)
            {
                CurrentTable = wd.SelectedTableId;
            }
        }

        public KhachHang CheckMember(string phoneNumber)
        {
            using (var db = new MenuContext())
            {
                var khach = db.KhachHangs.FirstOrDefault(k => k.SoDienThoai == phoneNumber);
                if (khach == null)
                {
                    khach = new KhachHang { SoDienThoai = phoneNumber, HoTen = "Khách Mới", HangThanhVien = "Mới", DiemTichLuy = 0 };
                    db.KhachHangs.Add(khach);
                    db.SaveChanges();
                }
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
            if (loai == "Mì Cay") MenuHienThi = new ObservableCollection<MonAn>(_allMonAn.Where(x => x is MonMiCay));
            else MenuHienThi = new ObservableCollection<MonAn>(_allMonAn.OfType<MonPhu>().Where(x => x.TheLoai == loai));
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
            _repo.CreateOrder(CurrentTable, TongTienCart, "Đơn từ Tablet", GioHang);
            GioHang.Clear();
            UpdateCartInfo();
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
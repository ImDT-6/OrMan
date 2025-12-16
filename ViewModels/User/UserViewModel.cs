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
using System.Windows.Threading; // [QUAN TRỌNG] Để dùng Timer
using System; // Để dùng TimeSpan

namespace OrMan.ViewModels.User
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repo;
        private readonly BanAnRepository _banRepo;
        private ObservableCollection<MonAn> _allMonAn;
        private DispatcherTimer _timerCheckUpdate; // [MỚI] Timer để tự động cập nhật

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

        // Biến lưu loại món đang xem (Mì Cay/Đồ Chiên...) để khi reload không bị nhảy về tab đầu
        private string _currentCategoryTag = "Mì Cay";

        public ICommand CallStaffCommand { get; private set; }
        public ICommand ChonBanCommand { get; private set; }

        public UserViewModel()
        {
            _repo = new ThucDonRepository();
            _banRepo = new BanAnRepository();

            LoadData();

            CallStaffCommand = new RelayCommand<object>(CallStaff);
            ChonBanCommand = new RelayCommand<object>(OpenChonBanWindow);

            // [MỚI] KHỞI ĐỘNG CHẾ ĐỘ TỰ CẬP NHẬT
            SetupAutoUpdate();
        }

        // --- [LOGIC TỰ ĐỘNG CẬP NHẬT] ---
        private void SetupAutoUpdate()
        {
            _timerCheckUpdate = new DispatcherTimer();
            // Cứ 5 giây hỏi Database 1 lần
            _timerCheckUpdate.Interval = TimeSpan.FromSeconds(5);
            _timerCheckUpdate.Tick += (s, e) => ReloadMenuRealTime();
            _timerCheckUpdate.Start();
        }

        private void ReloadMenuRealTime()
        {
            // 1. Lấy dữ liệu mới nhất từ SQL (Hàm GetAvailableMenu phải dùng new Context)
            var newData = _repo.GetAvailableMenu();

            // 2. Kiểm tra sơ bộ xem số lượng có thay đổi không
            // (Hoặc nếu muốn kỹ hơn thì so sánh nội dung, nhưng check count cho nhanh)
            if (newData.Count != (_allMonAn?.Count ?? 0))
            {
                _allMonAn = newData;
                // Cập nhật lại giao diện theo Tab đang chọn
                FilterMenu(_currentCategoryTag);
            }
        }
        // --------------------------------

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

        public void TangSoLuongMon(CartItem item)
        {
            if (item != null)
            {
                item.SoLuong++;
                UpdateCartInfo();
            }
        }

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
                    var result = MessageBox.Show($"Bạn có muốn xóa món '{item.TenHienThi}' khỏi giỏ hàng?",
                                                 "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        XoaMonKhoiGio(item);
                    }
                }
            }
        }

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
            TongTienCart = GioHang.Sum(x => x.ThanhTien);
            TongSoLuong = GioHang.Sum(x => x.SoLuong);
        }

        // --- [SECTION KHÁCH HÀNG] ---
        public KhachHang CheckMember(string phoneNumber)
        {
            using (var db = new MenuContext())
            {
                var khach = db.KhachHangs.FirstOrDefault(k => k.SoDienThoai == phoneNumber);
                if (khach != null) CurrentCustomer = khach;
                return khach;
            }
        }

        public KhachHang RegisterCustomer(string phone, string name)
        {
            using (var db = new MenuContext())
            {
                var existing = db.KhachHangs.FirstOrDefault(k => k.SoDienThoai == phone);
                if (existing != null) return existing;

                var newKhach = new KhachHang
                {
                    SoDienThoai = phone,
                    HoTen = name,
                    HangThanhVien = "Khách Hàng Mới",
                    DiemTichLuy = 0
                };

                db.KhachHangs.Add(newKhach);
                db.SaveChanges();

                CurrentCustomer = newKhach;
                return newKhach;
            }
        }

        // --- [CÁC HÀM HỖ TRỢ KHÁC] ---
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
            // Lưu lại loại đang chọn để dùng cho Timer reload
            _currentCategoryTag = loai;

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
                        string method = requestWindow.SelectedPaymentMethod;
                        _banRepo.RequestPayment(CurrentTable, method);
                        MessageBox.Show($"Đã gửi yêu cầu THANH TOÁN ({method}) cho Bàn {CurrentTable}!",
                                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (requestWindow.SelectedRequest == RequestType.Support)
                {
                    string msg = requestWindow.SupportMessage;
                    _banRepo.SendSupportRequest(CurrentTable, msg);
                    MessageBox.Show($"Đã gửi lời nhắn: \"{msg}\"\nNhân viên sẽ đến ngay!", "Đã gửi", MessageBoxButton.OK, MessageBoxImage.Information);
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
        public void ResetSession()
        {
            // 1. Reset Khách hàng về mặc định (Khách lẻ/vãng lai)
            CurrentCustomer = new KhachHang
            {
                KhachHangID = 0,
                HoTen = "Khách Mới",
                SoDienThoai = "",
                HangThanhVien = "Mới",
                DiemTichLuy = 0
            };

            // 2. Nếu bạn muốn xóa sạch giỏ hàng khi reset thì dùng dòng này:
            // GioHang.Clear(); 

            // Tuy nhiên, theo logic "Hủy đăng ký tích điểm nhưng vẫn muốn thanh toán cho khách lẻ" 
            // thì KHÔNG NÊN xóa giỏ hàng ở đây. 
            // Ta chỉ reset thông tin khách hàng thôi.

            // 3. Cập nhật lại các thông số tiền nong (nếu cần)
            UpdateCartInfo();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
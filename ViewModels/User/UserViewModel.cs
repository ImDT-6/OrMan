using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Services;
using OrMan.Views.User;
using Microsoft.EntityFrameworkCore;

namespace OrMan.ViewModels.User
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repo;
        private readonly BanAnRepository _banRepo;
        private ObservableCollection<MonAn> _allMonAn;

        public decimal GiamGiaTamTinh { get; set; } = 0;
        private bool _isNutGoiHoTroEnabled = true;
        private int _secondsCountdown = 0;

        // --- CÁC THUỘC TÍNH BINDING ---

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
            set
            {
                _currentTable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TableDisplayText));
            }
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

        private string _currentCategoryTag = "Mì Cay";
        public string CurrentCategoryTag
        {
            get => _currentCategoryTag;
            set { _currentCategoryTag = value; OnPropertyChanged(); }
        }

        public string TextNutHoTro
        {
            get
            {
                if (IsNutGoiHoTroEnabled) return GetRes("Str_CallStaff");
                string template = GetRes("Str_WaitMoment");
                try { return string.Format(template, _secondsCountdown); }
                catch { return template; }
            }
        }

        public bool IsNutGoiHoTroEnabled
        {
            get => _isNutGoiHoTroEnabled;
            set
            {
                _isNutGoiHoTroEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextNutHoTro));
            }
        }

        public ICommand CallStaffCommand { get; private set; }
        public ICommand ChonBanCommand { get; private set; }

        // --- KHỞI TẠO ---

        public UserViewModel()
        {
            _repo = new ThucDonRepository();
            _banRepo = new BanAnRepository();

            LoadData();

            CallStaffCommand = new RelayCommand<object>(CallStaff);
            ChonBanCommand = new RelayCommand<object>(OpenChonBanWindow);
        }

        // --- LOGIC XỬ LÝ DỮ LIỆU ---

        private void LoadData()
        {
            _allMonAn = _repo.GetAvailableMenu();
            FilterMenu(_currentCategoryTag);
        }

        /// <summary>
        /// Hàm lọc thực đơn theo loại (Đã đồng bộ Tag với Admin)
        /// </summary>
        // Trong UserViewModel.cs
        public void FilterMenu(string loai)
        {
            if (string.IsNullOrEmpty(loai)) return;

            CurrentCategoryTag = loai;

            // Đảm bảo lấy dữ liệu mới nhất từ Repository thay vì chỉ dùng danh sách cũ
            _allMonAn = _repo.GetAvailableMenu();

            if (loai == "Mì Cay")
            {
                MenuHienThi = new ObservableCollection<MonAn>(_allMonAn.Where(x => x is MonMiCay));
            }
            else
            {
                // Lọc các món phụ có thể loại tương ứng
                MenuHienThi = new ObservableCollection<MonAn>(
                    _allMonAn.OfType<MonPhu>().Where(x => x.TheLoai == loai)
                );
            }
        }

        public bool KiemTraConMon(string maMon)
        {
            using (var db = new MenuContext())
            {
                var mon = db.MonAns.AsNoTracking().FirstOrDefault(x => x.MaMon == maMon);
                return mon != null && !mon.IsSoldOut;
            }
        }

        // --- GIỎ HÀNG ---

        public void AddToCart(MonAn mon, int sl, int capDo, string ghiChu)
        {
            var itemDaCo = GioHang.FirstOrDefault(x => x.MonAn.MaMon == mon.MaMon
                                                    && x.CapDoCay == capDo
                                                    && x.GhiChu == ghiChu);

            if (itemDaCo != null)
                itemDaCo.SoLuong += sl;
            else
                GioHang.Add(new CartItem(mon, sl, capDo, ghiChu));

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
            if (item == null) return;
            if (item.SoLuong > 1)
            {
                item.SoLuong--;
                UpdateCartInfo();
            }
            else
            {
                string msg = string.Format(GetRes("Str_Msg_RemoveItem"), item.TenHienThi);
                var result = MessageBox.Show(msg, GetRes("Str_Title_DeleteConfirm"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) XoaMonKhoiGio(item);
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

        // --- QUẢN LÝ THÀNH VIÊN ---

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
                    DiemTichLuy = 0,
                    NgayThamGia = DateTime.Now
                };

                db.KhachHangs.Add(newKhach);
                db.SaveChanges();

                CurrentCustomer = newKhach;
                return newKhach;
            }
        }

        // --- ĐẶT HÀNG & THANH TOÁN ---

        public bool SubmitOrder()
        {
            if (GioHang.Count == 0) return false;
            if (CurrentTable <= 0)
            {
                MessageBox.Show(GetRes("Str_Msg_SelectTableFirst"), GetRes("Str_Title_NoTable"), MessageBoxButton.OK, MessageBoxImage.Warning);
                OpenChonBanWindow(null);
                if (CurrentTable <= 0) return false;
            }

            _repo.CreateOrder(CurrentTable, TongTienCart, "Đơn từ Tablet", GioHang, GiamGiaTamTinh);

            if (CurrentCustomer != null && CurrentCustomer.KhachHangID > 0)
            {
                int diemCong = (int)(TongTienCart / 1000);
                if (diemCong > 0)
                {
                    using (var db = new MenuContext())
                    {
                        var khachDB = db.KhachHangs.FirstOrDefault(k => k.KhachHangID == CurrentCustomer.KhachHangID);
                        if (khachDB != null)
                        {
                            string hangCu = khachDB.HangThanhVien;
                            khachDB.DiemTichLuy += diemCong;
                            khachDB.CapNhatHang();
                            db.SaveChanges();

                            CurrentCustomer.DiemTichLuy = khachDB.DiemTichLuy;
                            CurrentCustomer.HangThanhVien = khachDB.HangThanhVien;
                            OnPropertyChanged(nameof(CurrentCustomer));

                            if (khachDB.HangThanhVien == "Kim Cương" && hangCu != "Kim Cương")
                            {
                                MessageBox.Show(GetRes("Str_Msg_DiamondUpgrade"), GetRes("Str_Title_Notice"));
                            }
                        }
                    }
                }
            }

            GiamGiaTamTinh = 0;
            GioHang.Clear();
            UpdateCartInfo();
            return true;
        }

        // --- ĐÁNH GIÁ & PHẢN HỒI ---

        public void GuiDanhGia(int soSao, string tags, string noiDung)
        {
            using (var context = new MenuContext())
            {
                string tenBan = CurrentTable > 0 ? $"Bàn {CurrentTable:00}" : "Khách vãng lai";
                string sdtKhach = (CurrentCustomer != null && !string.IsNullOrEmpty(CurrentCustomer.SoDienThoai)) ? CurrentCustomer.SoDienThoai : null;

                string dinhDanhNguoiGui = (sdtKhach != null) ? $"{tenBan} - {sdtKhach}" : $"{tenBan} (Ẩn danh)";

                var danhGia = new DanhGia
                {
                    SoSao = soSao,
                    CacTag = tags,
                    NoiDung = noiDung,
                    SoDienThoai = dinhDanhNguoiGui,
                    NgayTao = DateTime.Now
                };
                context.DanhGias.Add(danhGia);
                context.SaveChanges();
            }
        }

        // --- HỖ TRỢ & ĐẶT BÀN ---

        private void OpenChonBanWindow(object obj)
        {
            var wd = new ChonBanWindow();
            if (wd.ShowDialog() == true)
            {
                if (CurrentTable != wd.SelectedTableId)
                {
                    CurrentTable = wd.SelectedTableId;
                    ResetSession();
                }
            }
        }

        private async void CallStaff(object obj)
        {
            if (!IsNutGoiHoTroEnabled) return;
            if (CurrentTable <= 0) { OpenChonBanWindow(null); if (CurrentTable <= 0) return; }

            var activeOrder = _banRepo.GetActiveOrder(CurrentTable);
            var requestWindow = new SupportRequestWindow(activeOrder != null);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            bool? dialogResult = requestWindow.ShowDialog();
            if (mainWindow != null) mainWindow.Opacity = 1;

            if (dialogResult == true)
            {
                bool daGui = false;
                if (requestWindow.SelectedRequest == RequestType.Checkout && activeOrder != null)
                {
                    _banRepo.RequestPayment(CurrentTable, requestWindow.SelectedPaymentMethod);
                    string msg = string.Format(GetRes("Str_Msg_PaymentRequested"), requestWindow.SelectedPaymentMethod, CurrentTable);
                    MessageBox.Show(msg, GetRes("Str_Title_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                    daGui = true;
                }
                else if (requestWindow.SelectedRequest == RequestType.Support)
                {
                    _banRepo.SendSupportRequest(CurrentTable, requestWindow.SupportMessage);
                    string msg = string.Format(GetRes("Str_Msg_SupportSent"), requestWindow.SupportMessage);
                    MessageBox.Show(msg, GetRes("Str_Title_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                    daGui = true;
                }

                if (daGui)
                {
                    IsNutGoiHoTroEnabled = false;
                    for (int i = 5; i > 0; i--) { _secondsCountdown = i; OnPropertyChanged(nameof(TextNutHoTro)); await Task.Delay(1000); }
                    IsNutGoiHoTroEnabled = true;
                }
            }
        }

        public void ResetSession()
        {
            CurrentCustomer = new KhachHang { KhachHangID = 0, HoTen = "Khách Mới", SoDienThoai = "", HangThanhVien = "Mới", DiemTichLuy = 0 };
            UpdateCartInfo();
        }

        // --- HELPERS ---

        private string GetRes(string key) => Application.Current.TryFindResource(key) as string ?? key;

        public void RefreshLanguage() => OnPropertyChanged(nameof(TextNutHoTro));

        public void Cleanup() { }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
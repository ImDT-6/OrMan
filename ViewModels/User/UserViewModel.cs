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

        private bool _isNutGoiHoTroEnabled = true;
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

        public string TextNutHoTro => IsNutGoiHoTroEnabled ? "Gọi Nhân Viên" : "Đợi xíu nha!";

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

        private string _currentCategoryTag = "Mì Cay";
        public string CurrentCategoryTag
        {
            get => _currentCategoryTag;
            set { _currentCategoryTag = value; OnPropertyChanged(); }
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

        private void ReloadMenuRealTime()
        {
            var newData = _repo.GetAvailableMenu();
            if (newData.Count != (_allMonAn?.Count ?? 0))
            {
                _allMonAn = newData;
                FilterMenu(_currentCategoryTag);
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

        private void LoadData()
        {
            _allMonAn = _repo.GetAvailableMenu();
            FilterMenu("Mì Cay");
        }

        public void FilterMenu(string loai)
        {
            CurrentCategoryTag = loai;
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

        private async void CallStaff(object obj)
        {
            if (!IsNutGoiHoTroEnabled) return;

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

            bool? dialogResult = requestWindow.ShowDialog();

            if (mainWindow != null) mainWindow.Opacity = 1;

            if (dialogResult == true)
            {
                bool daGuiThanhCong = false;

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
                        daGuiThanhCong = true;
                    }
                }
                else if (requestWindow.SelectedRequest == RequestType.Support)
                {
                    string msg = requestWindow.SupportMessage;
                    _banRepo.SendSupportRequest(CurrentTable, msg);
                    MessageBox.Show($"Đã gửi lời nhắn: \"{msg}\"\nNhân viên sẽ đến ngay!", "Đã gửi", MessageBoxButton.OK, MessageBoxImage.Information);
                    daGuiThanhCong = true;
                }

                if (daGuiThanhCong)
                {
                    IsNutGoiHoTroEnabled = false;
                    await Task.Delay(10000); // Đợi 10s async không block UI
                    IsNutGoiHoTroEnabled = true;
                }
            }
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
                            khachDB.DiemTichLuy += diemCong;
                            khachDB.CapNhatHang();
                            db.SaveChanges();

                            CurrentCustomer.DiemTichLuy = khachDB.DiemTichLuy;
                            CurrentCustomer.HangThanhVien = khachDB.HangThanhVien;
                            OnPropertyChanged(nameof(CurrentCustomer));

                            if (khachDB.HangThanhVien == "Kim Cương" && CurrentCustomer.HangThanhVien != "Kim Cương")
                            {
                                MessageBox.Show("Chúc mừng! Quý khách đã thăng hạng KIM CƯƠNG!", "Thông báo");
                            }
                        }
                    }
                }
            }

            GioHang.Clear();
            UpdateCartInfo();
            return true;
        }

        public void ResetSession()
        {
            CurrentCustomer = new KhachHang
            {
                KhachHangID = 0,
                HoTen = "Khách Mới",
                SoDienThoai = "",
                HangThanhVien = "Mới",
                DiemTichLuy = 0
            };
            UpdateCartInfo();
        }

        // [QUAN TRỌNG] Hàm gửi đánh giá đã được cập nhật
        public void GuiDanhGia(int soSao, string tags, string noiDung)
        {
            using (var context = new MenuContext())
            {
                var danhGia = new DanhGia
                {
                    SoSao = soSao,
                    CacTag = tags,
                    NoiDung = noiDung,
                    // [TỰ ĐỘNG] Lấy số điện thoại từ khách hàng hiện tại (nếu có)
                    SoDienThoai = CurrentCustomer != null && !string.IsNullOrEmpty(CurrentCustomer.SoDienThoai)
                                  ? CurrentCustomer.SoDienThoai
                                  : "Ẩn danh",
                    NgayTao = DateTime.Now
                };
                context.DanhGias.Add(danhGia);
                context.SaveChanges();
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

        public void Cleanup()
        {
            // Hàm Cleanup để đồng bộ interface
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
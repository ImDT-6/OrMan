using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;
using GymManagement.Views.User; // Để mở cửa sổ chọn bàn

namespace GymManagement.ViewModels.User
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repo;
        private readonly BanAnRepository _banRepo;
        private ObservableCollection<MonAn> _allMonAn;

        // [MỚI] Biến lưu bàn hiện tại
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
        public ICommand ChonBanCommand { get; private set; } // [MỚI]

        public UserViewModel()
        {
            _repo = new ThucDonRepository();
            _banRepo = new BanAnRepository();
            LoadData();

            CallStaffCommand = new RelayCommand<object>(CallStaff);
            ChonBanCommand = new RelayCommand<object>(OpenChonBanWindow); // [MỚI]
        }

        // [MỚI] Hàm mở cửa sổ chọn bàn
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

        private void CallStaff(object obj)
        {
            // [MỚI] Kiểm tra đã chọn bàn chưa
            if (CurrentTable <= 0)
            {
                OpenChonBanWindow(null);
                if (CurrentTable <= 0) return; // Nếu vẫn chưa chọn thì thôi
            }

            _banRepo.RequestPayment(CurrentTable);
            MessageBox.Show($"Đã gửi yêu cầu từ Bàn {CurrentTable} tới Admin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public bool SubmitOrder()
        {
            if (GioHang.Count == 0) return false;

            // [MỚI] Bắt buộc chọn bàn trước khi gửi đơn
            if (CurrentTable <= 0)
            {
                MessageBox.Show("Vui lòng chọn số bàn bạn đang ngồi trước khi gửi đơn!", "Chưa chọn bàn", MessageBoxButton.OK, MessageBoxImage.Warning);
                OpenChonBanWindow(null);
                if (CurrentTable <= 0) return false; // Khách hủy chọn bàn
            }

            // Gửi đơn với số bàn thực tế
            _repo.CreateOrder(CurrentTable, TongTienCart, "Đơn từ Tablet", GioHang);
            GioHang.Clear();
            UpdateCartInfo();
            return true;
        }
    }
}
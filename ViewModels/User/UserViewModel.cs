using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.ViewModels.User
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repo;
        private readonly BanAnRepository _banRepo;
        private ObservableCollection<MonAn> _allMonAn;

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

        public UserViewModel()
        {
            _repo = new ThucDonRepository();
            _banRepo = new BanAnRepository();
            LoadData();

            CallStaffCommand = new RelayCommand<object>(CallStaff);
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
            var item = new CartItem(mon, sl, capDo, ghiChu);
            GioHang.Add(item);
            UpdateCartInfo();
        }

        private void UpdateCartInfo()
        {
            TongTienCart = GioHang.Sum(x => x.ThanhTien);
            TongSoLuong = GioHang.Sum(x => x.SoLuong);
        }

        private void CallStaff(object obj)
        {
            int currentTable = 1;
            _banRepo.RequestPayment(currentTable);
            MessageBox.Show("Đã gửi yêu cầu thanh toán/hỗ trợ tới Admin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public bool SubmitOrder()
        {
            if (GioHang.Count == 0) return false;
            _repo.CreateOrder(1, TongTienCart, "Đơn từ Tablet", GioHang);
            GioHang.Clear();
            UpdateCartInfo();
            return true;
        }
    }
}
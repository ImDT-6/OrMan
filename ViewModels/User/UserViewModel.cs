using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.ViewModels.User
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repo;
        private ObservableCollection<MonAn> _allMonAn; // Dữ liệu gốc từ DB

        // Danh sách hiển thị trên Menu
        private ObservableCollection<MonAn> _menuHienThi;
        public ObservableCollection<MonAn> MenuHienThi
        {
            get => _menuHienThi;
            set { _menuHienThi = value; OnPropertyChanged(); }
        }

        // Giỏ hàng
        public ObservableCollection<CartItem> GioHang { get; set; } = new ObservableCollection<CartItem>();

        // Tổng tiền tạm tính
        private decimal _tongTienCart;
        public decimal TongTienCart
        {
            get => _tongTienCart;
            set { _tongTienCart = value; OnPropertyChanged(); }
        }

        // Tổng số lượng món
        private int _tongSoLuong;
        public int TongSoLuong
        {
            get => _tongSoLuong;
            set { _tongSoLuong = value; OnPropertyChanged(); }
        }

        public UserViewModel()
        {
            _repo = new ThucDonRepository();
            LoadData();
        }

        private void LoadData()
        {
            // Lấy dữ liệu thật từ SQL Server
            _allMonAn = _repo.GetAll();

            // Mặc định hiện Mì Cay trước
            FilterMenu("Mì Cay");
        }

        // Hàm lọc món ăn (Gọi từ View)
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

        // Hàm thêm vào giỏ (Gọi từ View sau khi đóng Popup)
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public bool SubmitOrder()
        {
            if (GioHang.Count == 0) return false;

            // Gọi Repository để lưu (Truyền cả GioHang vào)
            _repo.CreateOrder(1, TongTienCart, "Đơn từ Tablet", GioHang);

            GioHang.Clear();
            UpdateCartInfo();
            return true;
        }
    }
}
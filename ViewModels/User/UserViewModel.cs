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

        // [FIX LỖI] Đã thêm Property MenuHienThi
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

            CallStaffCommand = new RelayCommand<object>(_ => { /* Not used directly for now */ });
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
                // Gán giá trị và gọi OnPropertyChanged()
                MenuHienThi = new ObservableCollection<MonAn>(_allMonAn.Where(x => x is MonMiCay));
            }
            else
            {
                // Gán giá trị và gọi OnPropertyChanged()
                MenuHienThi = new ObservableCollection<MonAn>(_allMonAn.OfType<MonPhu>().Where(x => x.TheLoai == loai));
            }
        }

        public void AddToCart(MonAn mon, int sl, int capDo, string ghiChu)
        {
            // 1. Tìm xem món này đã có trong giỏ chưa (Khớp Mã, Cấp độ và Ghi chú)
            var itemDaCo = GioHang.FirstOrDefault(x => x.MonAn.MaMon == mon.MaMon
                                                    && x.CapDoCay == capDo
                                                    && x.GhiChu == ghiChu);

            if (itemDaCo != null)
            {
                // 2. Nếu có rồi -> Cộng dồn số lượng
                itemDaCo.SoLuong += sl;
            }
            else
            {
                // 3. Nếu chưa có -> Tạo mới và thêm vào
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

        public bool HasActiveOrder(int soBan)
        {
            return _banRepo.GetActiveOrder(soBan) != null;
        }

        public void RequestCheckout(int soBan)
        {
            _banRepo.RequestPayment(soBan);
        }

        public void RequestSupport(int soBan)
        {
            _banRepo.RequestPayment(soBan);
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
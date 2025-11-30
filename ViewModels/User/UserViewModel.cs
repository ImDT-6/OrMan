using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;
using GymManagement.Views.User;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;
using GymManagement.Views.User;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace GymManagement.ViewModels.User
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
            // 1. Kiểm tra đã chọn bàn chưa
            if (CurrentTable <= 0)
            {
                OpenChonBanWindow(null);
                if (CurrentTable <= 0) return; // Nếu vẫn chưa chọn thì thôi
            }

            // 2. Lấy hóa đơn thực tế dưới Database
            var activeOrder = _banRepo.GetActiveOrder(CurrentTable);

            // 3. Logic chặn nếu chưa có món
            // Nếu (Không có hóa đơn đang ăn) VÀ (Giỏ hàng cũng đang rỗng)
            if (activeOrder == null)
            {
                if (GioHang.Count > 0)
                {
                    // Trường hợp khách đã chọn món vào giỏ nhưng QUÊN bấm gửi bếp
                    MessageBox.Show("Bạn có món trong giỏ hàng nhưng chưa Gửi Bếp.\nVui lòng bấm 'GỬI ĐƠN' trước khi thanh toán.",
                                    "Chưa gửi đơn",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                }
                else
                {
                    // Trường hợp chưa chọn gì cả (Đúng ý bạn yêu cầu)
                    MessageBox.Show("Giỏ hàng rỗng (Chưa chọn món).\nVui lòng gọi món trước khi yêu cầu thanh toán!",
                                    "Thông báo",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                }
                return; // Dừng lại, không gửi yêu cầu đi
            }

            // 4. Nếu hợp lệ -> Gửi yêu cầu thanh toán
            _banRepo.RequestPayment(CurrentTable);
            MessageBox.Show($"Đã gửi yêu cầu thanh toán từ Bàn {CurrentTable} tới Thu ngân!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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
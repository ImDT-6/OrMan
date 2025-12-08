using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OrMan.Models;
using OrMan.ViewModels;

namespace OrMan.Views.Admin
{
    public partial class AdminView : UserControl
    {
        // Biến lưu giữ các màn hình để không phải tạo lại (Caching)
        private DashboardView _dashboardView;
        private QuanLyBanView _banView;
        private ThucDonView _menuView;
        private DoanhThuView _revenueView;
        private KhachHangView _customerView;
        public AdminView()
        {
            InitializeComponent();
            // Khởi tạo DataContext cho AdminView (Nếu cần, thường là DashboardViewModel)
            // this.DataContext = new DashboardViewModel(); 

            // Mặc định vào là hiện Dashboard
            ChuyenSangChucNang("Tổng Quan"); // Truyền thêm nút mặc định
        }

        // [MỚI] Hàm công khai để Dashboard gọi
        public void ChuyenDenBanCanXuLy(BanAn ban)
        {
            // 1. Chuyển sang màn hình Quản Lý Bàn
            ChuyenSangChucNang("Quản Lý Bàn"); // Highlight nút Quản Lý Bàn

            // 2. Chọn đúng cái bàn cần xử lý trong ViewModel của màn hình đó
            if (_banView != null && _banView.DataContext is QuanLyBanViewModel vm)
            {
                // Tìm bàn trong danh sách của ViewModel (để đảm bảo object reference đúng)
                var banTrongList = vm.DanhSachBan.FirstOrDefault(b => b.SoBan == ban.SoBan);
                if (banTrongList != null)
                {
                    vm.SelectedBan = banTrongList;
                }
            }
        }

        // Hàm trung tâm xử lý việc chuyển đổi
        private void ChuyenSangChucNang(string chucNang)
        {
            UserControl viewToLoad = null;

            switch (chucNang)
            {
                case "Tổng Quan":
                    if (_dashboardView == null) _dashboardView = new DashboardView();
                    viewToLoad = _dashboardView;
                    break;
                case "Quản Lý Bàn":
                    if (_banView == null) _banView = new QuanLyBanView();
                    viewToLoad = _banView;
                    break;
                case "Thực Đơn":
                    if (_menuView == null) _menuView = new ThucDonView();
                    viewToLoad = _menuView;
                    break;
                case "Doanh Thu":
                    if (_revenueView == null) _revenueView = new DoanhThuView();
                    viewToLoad = _revenueView;
                    break;
                case "Khách Hàng":
                    if (_customerView == null) _customerView = new KhachHangView();
                    viewToLoad = _customerView;
                    break;
            }

            // [ĐÃ XÓA]: Đoạn code foreach reset background
            // [ĐÃ XÓA]: Đoạn code buttonClicked.SetResourceReference...

            // Chỉ còn lại việc hiển thị View
            if (viewToLoad != null)
            {
                AdminContentArea.Content = viewToLoad;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            // [QUAN TRỌNG] Đổi 'Button' thành 'RadioButton'
            var button = sender as RadioButton;

            if (button == null) return; // Kiểm tra an toàn

            string content = button.Content?.ToString();

            // Gọi hàm chuyển đổi và truyền chính nút đó vào
            // Lưu ý: Hàm ChuyenSangChucNang có thể cũng cần sửa tham số đầu vào nếu nó đang nhận Button
            ChuyenSangChucNang(content);
        }

        private void Button_DangXuat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Đăng xuất quyền quản trị?", "Xác nhận",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                // Giả định MainWindow có hàm ChuyenSangDangNhap
                mainWindow?.ChuyenSangDangNhap();
            }
        }
    }
}
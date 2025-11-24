using System.Windows;
using System.Windows.Controls;
using GymManagement.ViewModels;

namespace GymManagement.Views.Admin
{
    public partial class AdminView : UserControl
    {
        // Biến lưu giữ các màn hình để không phải tạo lại (Caching)
        private DashboardView _dashboardView;
        private QuanLyBanView _banView;
        private ThucDonView _menuView;
        private DoanhThuView _revenueView;

        public AdminView()
        {
            InitializeComponent();
            this.DataContext = new DashboardViewModel();

            // Mặc định vào là hiện Dashboard
            ChuyenSangChucNang("Tổng Quan");
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
            }

            // Thay đổi nội dung hiển thị bên dưới Menu
            if (viewToLoad != null)
            {
                AdminContentArea.Content = viewToLoad;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string content = button.Content?.ToString();

            // Gọi hàm chuyển đổi
            ChuyenSangChucNang(content);
        }

        private void Button_DangXuat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Đăng xuất quyền quản trị?", "Xác nhận",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ChuyenSangDangNhap();
            }
        }
    }
}
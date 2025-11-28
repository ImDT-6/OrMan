using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
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
            // Khởi tạo DataContext cho AdminView (Nếu cần, thường là DashboardViewModel)
            // this.DataContext = new DashboardViewModel(); 

            // Mặc định vào là hiện Dashboard
            ChuyenSangChucNang("Tổng Quan", MenuButtonTongQuan); // Truyền thêm nút mặc định
        }

        // [MỚI] Hàm công khai để Dashboard gọi
        public void ChuyenDenBanCanXuLy(BanAn ban)
        {
            // 1. Chuyển sang màn hình Quản Lý Bàn
            ChuyenSangChucNang("Quản Lý Bàn", MenuButtonQuanLyBan); // Highlight nút Quản Lý Bàn

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
        private void ChuyenSangChucNang(string chucNang, Button buttonClicked)
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

            // [FIX LỖI FOCUS] 1. Reset tất cả các nút
            // Tìm StackPanel chứa Menu Buttons (Giả định có tên là MenuItemsPanel)
            if (MenuItemsPanel != null)
            {
                foreach (var child in MenuItemsPanel.Children)
                {
                    if (child is Button btn)
                    {
                        // Đặt lại Background và Foreground về mặc định (Transparent và TextSecondary)
                        btn.SetResourceReference(Control.BackgroundProperty, "Transparent");
                        btn.SetResourceReference(Control.ForegroundProperty, "TextSecondary");
                    }
                }
            }


            // 2. Highlight nút được chọn
            if (buttonClicked != null)
            {
                // Set lại Style/Color cho nút đang được chọn (Dùng màu nổi bật đã định nghĩa)
                // Bạn cần đảm bảo các màu này có trong Resource Dictionary
                buttonClicked.SetResourceReference(Control.BackgroundProperty, "AccentGradient");
                buttonClicked.SetResourceReference(Control.ForegroundProperty, "TextPrimary"); // White
            }


            // 3. Thay đổi nội dung hiển thị bên dưới Menu
            if (viewToLoad != null)
            {
                AdminContentArea.Content = viewToLoad;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string content = button.Content?.ToString();

            // Gọi hàm chuyển đổi và truyền chính nút đó vào
            ChuyenSangChucNang(content, button);
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
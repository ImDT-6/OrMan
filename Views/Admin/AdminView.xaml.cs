using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Input; // [MỚI] Cần thêm cái này để bắt phím
using OrMan.Models;
using OrMan.ViewModels.Admin;

namespace OrMan.Views.Admin
{
    public partial class AdminView : UserControl
    {
        // ... (Giữ nguyên các biến khai báo cũ) ...
        private DashboardView _dashboardView;
        private QuanLyBanView _banView;
        private ThucDonView _menuView;
        private DoanhThuView _revenueView;
        private KhachHangView _customerView;
        private KhoView _khoView;
        private BepView _bepView;

        public AdminView()
        {
            InitializeComponent();

            _banView = new QuanLyBanView();
            ChuyenSangChucNang("Tổng Quan");
            this.Loaded += AdminView_Loaded;
        }
        private void AdminView_Loaded(object sender, RoutedEventArgs e)
        {
            // Yêu cầu bàn phím tập trung vào màn hình Admin ngay lập tức
            // Để không cần click chuột mới bấm được phím
            this.Focus();
            Keyboard.Focus(this);
        }
        // --- XỬ LÝ PHÍM TẮT (KEYBOARD NAVIGATION) [MỚI] ---
        // Trong AdminView.xaml.cs

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1. Nếu đang nhập liệu (TextBox/PasswordBox) thì không xử lý phím tắt
            // (Tránh việc đang gõ văn bản bấm lộn nút Esc bị văng ra ngoài)
            if (Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is PasswordBox)
                return;

            // --- [MỚI] XỬ LÝ PHÍM ESC ĐỂ ĐĂNG XUẤT ---
            if (e.Key == Key.Escape)
            {
                // Gọi lại hàm xử lý của nút Đăng xuất (hiện thông báo xác nhận)
                Button_DangXuat_Click(sender, null);

                e.Handled = true; // Đánh dấu đã xử lý để không lan truyền sự kiện tiếp
                return;
            }
            // ------------------------------------------

            // 2. Xử lý điều hướng Menu bằng phím mũi tên (Code cũ)
            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                var radioButtons = MenuStackPanel.Children.OfType<RadioButton>().ToList();

                var currentBtn = radioButtons.FirstOrDefault(r => r.IsChecked == true);
                if (currentBtn == null) return;

                int index = radioButtons.IndexOf(currentBtn);

                if (e.Key == Key.Right) index++;
                else index--;

                if (index >= radioButtons.Count) index = 0;
                if (index < 0) index = radioButtons.Count - 1;

                var nextBtn = radioButtons[index];
                nextBtn.IsChecked = true;

                MenuButton_Click(nextBtn, null);
                this.Focus();

                e.Handled = true;
            }
        }

        // ... (Giữ nguyên các hàm MenuButton_Loaded, MenuButton_Click, UpdateMenuIndicator cũ) ...

        private void MenuButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn && btn.IsChecked == true)
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateMenuIndicator(btn)), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null) return;

            UpdateMenuIndicator(button);

            string content = button.Content?.ToString();
            ChuyenSangChucNang(content);
        }

        private void UpdateMenuIndicator(RadioButton selectedButton)
        {
            if (MenuIndicator == null || MenuGrid == null) return;
            MenuIndicator.Opacity = 1;

            try
            {
                Point relativeLocation = selectedButton.TranslatePoint(new Point(0, 0), MenuGrid);

                var duration = TimeSpan.FromSeconds(0.4);
                var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

                DoubleAnimation widthAnimation = new DoubleAnimation
                {
                    To = selectedButton.ActualWidth,
                    Duration = duration,
                    EasingFunction = easing
                };
                MenuIndicator.BeginAnimation(WidthProperty, widthAnimation);

                DoubleAnimation translateAnimation = new DoubleAnimation
                {
                    To = relativeLocation.X,
                    Duration = duration,
                    EasingFunction = easing
                };
                MenuIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
            }
            catch { }
        }

        public void ChuyenDenBanCanXuLy(BanAn ban)
        {
            ChuyenSangChucNang("Quản Lý Bàn");
            MenuButtonQuanLyBan.IsChecked = true;
            UpdateMenuIndicator(MenuButtonQuanLyBan);

            if (_banView != null && _banView.DataContext is QuanLyBanViewModel vm)
            {
                var banTrongList = vm.DanhSachBan.FirstOrDefault(b => b.SoBan == ban.SoBan);
                if (banTrongList != null)
                {
                    vm.SelectedBan = banTrongList;
                }
            }
        }

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
                case "Kho Hàng":
                    if (_khoView == null) _khoView = new KhoView();
                    viewToLoad = _khoView;
                    break;
                case "Bếp":
                    if (_bepView == null) _bepView = new BepView();
                    viewToLoad = _bepView;
                    break;
            }

            if (viewToLoad != null)
            {
                AdminContentArea.Content = viewToLoad;
            }
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
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OrMan.Models;
using OrMan.ViewModels.User;
using OrMan.Helpers;
namespace OrMan.Views.User
{

    public partial class UserView : UserControl
    {
        private UserViewModel _vm;
        private DispatcherTimer _clockTimer;

        public UserView()
        {
            InitializeComponent();
            _vm = new UserViewModel();
            this.DataContext = _vm;

            // Đăng ký sự kiện Unloaded để dọn dẹp Timer
            this.Unloaded += UserControl_Unloaded;

            FilterByTag("Mì Cay");

            SetupTimer();
        }

        private void SetupTimer()
        {
            _clockTimer = new DispatcherTimer();
            // Cập nhật mỗi 30 giây
            _clockTimer.Interval = TimeSpan.FromSeconds(30);
            _clockTimer.Tick += (s, e) => UpdateGreeting();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _clockTimer?.Start();
            UpdateGreeting();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _clockTimer?.Stop();
        }

        private void UpdateGreeting()
        {
            var now = DateTime.Now;
            int hour = now.Hour;
            string greetingKey;

            if (hour >= 5 && hour < 11) greetingKey = "Str_GoodMorning";
            else if (hour >= 11 && hour < 14) greetingKey = "Str_GoodAfternoon";
            else if (hour >= 14 && hour < 18) greetingKey = "Str_GoodEvening";
            else greetingKey = "Str_GoodNight";

            // Lấy chuỗi từ Resource Dictionary hiện tại
            string greetingText = Application.Current.TryFindResource(greetingKey) as string;
            string guestText = Application.Current.TryFindResource("Str_Guest") as string;

            // Fallback nếu không tìm thấy resource
            if (string.IsNullOrEmpty(greetingText)) greetingText = "Hello";
            if (string.IsNullOrEmpty(guestText)) guestText = "Guest";

            string fullGreeting = $"{greetingText}, {guestText}";

            if (txtGreeting != null)
            {
                txtGreeting.Text = fullGreeting;
            }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null) return;

            if (button.Tag is string tag)
            {
                FilterByTag(tag);
            }

            int index = MenuPanel.Children.IndexOf(button);
            double targetY = index * 60;

            DoubleAnimation animation = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            MenuIndicatorTransform.BeginAnimation(TranslateTransform.YProperty, animation);
        }

        private void FilterByTag(string tag)
        {
            _vm.FilterMenu(tag);
            var itemsControl = this.FindName("ItemsControlMenu") as ItemsControl;
            if (itemsControl != null)
            {
                itemsControl.ItemsSource = _vm.MenuHienThi;
            }
        }

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MonAn monAn)
            {
                if (monAn.IsSoldOut)
                {
                    string msg = string.Format(GetRes("Str_Msg_ProductSoldOut"), monAn.TenMon);
                    MessageBox.Show(msg, GetRes("Str_Title_Notice"), 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                   
                    return;
                }

                if (_vm.CurrentTable <= 0)
                {
                    if (_vm.ChonBanCommand.CanExecute(null))
                    {
                        _vm.ChonBanCommand.Execute(null);
                    }
                    if (_vm.CurrentTable <= 0) return;
                }

                var popup = new ChiTietMonWindow(monAn);
                popup.Owner = Application.Current.MainWindow;
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null) mainWindow.Opacity = 0.4;

                if (popup.ShowDialog() == true)
                {
                    _vm.AddToCart(monAn, popup.SoLuong, popup.CapDoCay, popup.GhiChu);
                }

                if (mainWindow != null) mainWindow.Opacity = 1;
            }
        }

        private void BtnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.GioHang.Count == 0)
            {
                // [ĐÃ SỬA] Dùng GetRes
                MessageBox.Show(GetRes("Str_Msg_CartEmpty"), GetRes("Str_Title_Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cartWindow = new GioHangWindow(_vm);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            if (cartWindow.ShowDialog() == true)
            {
                if (_vm.SubmitOrder())
                {
                    // [ĐÃ SỬA] Dùng GetRes
                    MessageBox.Show(
                        GetRes("Str_Msg_OrderSuccess"),
                        GetRes("Str_Title_Success"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }

            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            // [ĐÃ SỬA] Dùng GetRes
            var result = MessageBox.Show(GetRes("Str_Msg_ConfirmLogout"), GetRes("Str_Title_Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ChuyenSangDangNhap();
            }
        }

        private void BtnTichDiem_Click(object sender, RoutedEventArgs e)
        {
            // [CŨ] MessageBox.Show("Tính năng Tích điểm đang được phát triển!", "Thông báo");

            // [MỚI]
            var popup = new TichDiemWindow(_vm);
            var mainWindow = Application.Current.MainWindow;

            // Làm mờ màn hình chính cho đẹp
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            popup.Owner = mainWindow;
            popup.ShowDialog();

            // Khôi phục độ sáng
            if (mainWindow != null) mainWindow.Opacity = 1;

            // Nếu đã đăng nhập thành công, cập nhật giao diện chính (nếu muốn)
            if (_vm.CurrentCustomer != null)
            {
                // Ví dụ: đổi icon tích điểm thành màu vàng để báo hiệu đã đăng nhập
            }
        }

        // Đã xóa hàm BtnNgonNgu_Click vì không còn dùng nữa
        private void BtnLanguage_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }
        private void MenuItem_VN_Click(object sender, RoutedEventArgs e)
        {
            LanguageHelper.SetLanguage("vi");
            txtLangFlag.Text = "🇻🇳";
            txtLangName.Text = "Tiếng Việt";
            UpdateGreeting(); // Cập nhật lại câu chào ngay
        }
        private string GetRes(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }
        private void MenuItem_EN_Click(object sender, RoutedEventArgs e)
        {
            LanguageHelper.SetLanguage("en");
            txtLangFlag.Text = "🇺🇸";
            txtLangName.Text = "English";
            UpdateGreeting(); // Cập nhật lại câu chào ngay
        }

    }

}
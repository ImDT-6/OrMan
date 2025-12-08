using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OrMan.Models;
using OrMan.ViewModels.User;

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
            string timeSession;

            // Logic khung giờ Tiếng Việt
            if (hour >= 5 && hour < 11)
                timeSession = "buổi sáng";
            else if (hour >= 11 && hour < 14)
                timeSession = "buổi trưa";
            else if (hour >= 14 && hour < 18)
                timeSession = "buổi chiều";
            else
                timeSession = "buổi tối";

            string greeting = $"Chào {timeSession}, Quý khách";

            if (txtGreeting != null && txtGreeting.Text != greeting)
            {
                txtGreeting.Text = greeting;
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
                    MessageBox.Show(
                        $"Món '{monAn.TenMon}' hiện đang tạm hết hàng.\nVui lòng chọn món khác nhé!",
                        "Rất tiếc",
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
                MessageBox.Show("Giỏ hàng đang trống! Vui lòng chọn món trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cartWindow = new GioHangWindow(_vm);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            if (cartWindow.ShowDialog() == true)
            {
                if (_vm.SubmitOrder())
                {
                    MessageBox.Show(
                        "Đã gửi đơn xuống bếp thành công!\nVui lòng đợi trong giây lát.",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }

            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn muốn kết thúc phiên gọi món?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ChuyenSangDangNhap();
            }
        }

        private void BtnTichDiem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tính năng Tích điểm đang được phát triển!", "Thông báo");
        }

        private void BtnDanhGia_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Cảm ơn Quý khách đã quan tâm! Tính năng Đánh giá sẽ sớm ra mắt.", "Thông báo");
        }

        // Đã xóa hàm BtnNgonNgu_Click vì không còn dùng nữa
    }
}
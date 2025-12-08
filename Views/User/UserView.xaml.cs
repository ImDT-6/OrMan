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

        // Biến trạng thái ngôn ngữ (Mặc định là Tiếng Việt)
        private bool _isVietnamese = true;

        public UserView()
        {
            InitializeComponent();
            _vm = new UserViewModel();
            this.DataContext = _vm;

            FilterByTag("Mì Cay");
            StartGreetingTimer();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGreeting();
        }

        private void StartGreetingTimer()
        {
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromMinutes(1);
            _clockTimer.Tick += (s, e) => UpdateGreeting();
            _clockTimer.Start();
        }

        private void UpdateGreeting()
        {
            var now = DateTime.Now;
            string greeting = "";

            if (_isVietnamese)
            {
                // Logic Tiếng Việt
                string timeSession;
                if (now.Hour >= 5 && now.Hour < 11) timeSession = "buổi sáng";
                else if (now.Hour >= 11 && now.Hour < 14) timeSession = "buổi trưa";
                else if (now.Hour >= 14 && now.Hour < 18) timeSession = "buổi chiều";
                else timeSession = "buổi tối";

                greeting = $"Chào {timeSession}, Quý khách";
            }
            else
            {
                // Logic Tiếng Anh
                string timeSession;
                if (now.Hour >= 5 && now.Hour < 12) timeSession = "Morning";
                else if (now.Hour >= 12 && now.Hour < 18) timeSession = "Afternoon";
                else timeSession = "Evening";

                greeting = $"Good {timeSession}, Dear Customer";
            }

            if (txtGreeting != null)
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
                    string msg = _isVietnamese
                        ? $"Món '{monAn.TenMon}' hiện đang tạm hết hàng.\nVui lòng chọn món khác nhé!"
                        : $"'{monAn.TenMon}' is currently sold out.\nPlease choose another dish!";

                    string title = _isVietnamese ? "Rất tiếc" : "Sorry";
                    MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
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
                string msg = _isVietnamese ? "Giỏ hàng đang trống! Vui lòng chọn món trước." : "Your cart is empty! Please select items first.";
                string title = _isVietnamese ? "Thông báo" : "Notice";
                MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cartWindow = new GioHangWindow(_vm);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            if (cartWindow.ShowDialog() == true)
            {
                if (_vm.SubmitOrder())
                {
                    string msg = _isVietnamese
                        ? "Đã gửi đơn xuống bếp thành công!\nVui lòng đợi trong giây lát."
                        : "Order sent to kitchen successfully!\nPlease wait a moment.";
                    string title = _isVietnamese ? "Thành công" : "Success";

                    MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            string msg = _isVietnamese ? "Bạn muốn kết thúc phiên gọi món?" : "Do you want to end this session?";
            string title = _isVietnamese ? "Xác nhận" : "Confirm";

            var result = MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ChuyenSangDangNhap();
            }
        }

        private void BtnTichDiem_Click(object sender, RoutedEventArgs e)
        {
            string msg = _isVietnamese ? "Tính năng Tích điểm đang được phát triển!" : "Loyalty feature is under development!";
            MessageBox.Show(msg, _isVietnamese ? "Thông báo" : "Notice");
        }

        private void BtnDanhGia_Click(object sender, RoutedEventArgs e)
        {
            string msg = _isVietnamese ? "Cảm ơn Quý khách đã quan tâm! Tính năng Đánh giá sẽ sớm ra mắt." : "Thank you! Rating feature is coming soon.";
            MessageBox.Show(msg, _isVietnamese ? "Thông báo" : "Notice");
        }

        private void BtnNgonNgu_Click(object sender, RoutedEventArgs e)
        {
            // Đổi trạng thái ngôn ngữ
            _isVietnamese = !_isVietnamese;

            // Cập nhật lại UI (những phần xử lý trong code C#)
            UpdateGreeting();

            // Gợi ý cho bạn: Đây là chỗ để chèn logic thay đổi ResourceDictionary cho toàn bộ App
            /*
            var dict = new ResourceDictionary();
            if (_isVietnamese)
                dict.Source = new Uri("..\\Resources\\Lang.vi-VN.xaml", UriKind.Relative);
            else
                dict.Source = new Uri("..\\Resources\\Lang.en-US.xaml", UriKind.Relative);
            
            // Xóa dictionary ngôn ngữ cũ và thêm cái mới vào MergedDictionaries của Application
            // Application.Current.Resources.MergedDictionaries...
            */

            // Thông báo ngắn để người dùng biết đã đổi (Tạm thời)
            string currentLang = _isVietnamese ? "Tiếng Việt" : "English";
            // MessageBox.Show($"Đã chuyển ngôn ngữ sang: {currentLang}", "Language Changed");
        }
    }
}
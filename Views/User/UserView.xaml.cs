using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OrMan.Helpers;
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

            this.Unloaded += UserControl_Unloaded;
            this.Loaded += UserControl_Loaded;

            // Khởi tạo Timer cho đồng hồ hiển thị lời chào
            SetupTimer();
        }

        private void SetupTimer()
        {
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(30);
            _clockTimer.Tick += (s, e) => UpdateGreeting();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _clockTimer?.Start();
            UpdateGreeting();

            // Gọi filter mặc định để hiển thị món ăn ngay khi load xong
            FilterByTag("Mì Cay");
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // 1. Dừng đồng hồ UI
            _clockTimer?.Stop();

            // 2. Dọn dẹp ViewModel (nếu có Timer ngầm)
            _vm.Cleanup();
        }

        // Logic cập nhật lời chào theo giờ
        private void UpdateGreeting(int? fakeHour = null)
        {
            var now = DateTime.Now;
            int hour = fakeHour ?? now.Hour;

            if (GridMorning != null) GridMorning.Visibility = Visibility.Collapsed;
            if (GridNoon != null) GridNoon.Visibility = Visibility.Collapsed;
            if (GridAfternoon != null) GridAfternoon.Visibility = Visibility.Collapsed;
            if (GridNight != null) GridNight.Visibility = Visibility.Collapsed;

            string greetingText = "Xin chào";

            if (hour >= 5 && hour < 11)
            {
                if (GridMorning != null) GridMorning.Visibility = Visibility.Visible;
                greetingText = "Chào buổi sáng";
            }
            else if (hour >= 11 && hour < 14)
            {
                if (GridNoon != null) GridNoon.Visibility = Visibility.Visible;
                greetingText = "Chào buổi trưa";
            }
            else if (hour >= 14 && hour < 18)
            {
                if (GridAfternoon != null) GridAfternoon.Visibility = Visibility.Visible;
                greetingText = "Chào buổi chiều";
            }
            else
            {
                if (GridNight != null) GridNight.Visibility = Visibility.Visible;
                greetingText = (hour >= 22 || hour < 4) ? "Chúc ngủ ngon" : "Chào buổi tối";
            }

            if (txtGreeting != null) txtGreeting.Text = greetingText;
        }

        private void TestTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateGreeting((int)e.NewValue);
            if (_clockTimer != null && _clockTimer.IsEnabled) _clockTimer.Stop();
        }

        // Xử lý Animation khi chọn Menu Category
        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null) return;

            if (button.Tag is string tag)
            {
                FilterByTag(tag);
            }

            // Animation di chuyển thanh indicator
            if (MenuPanel != null && MenuIndicatorTransform != null)
            {
                int index = MenuPanel.Children.IndexOf(button);
                double targetY = index * 60; // Giả sử chiều cao mỗi item là 60

                DoubleAnimation animation = new DoubleAnimation
                {
                    To = targetY,
                    Duration = TimeSpan.FromSeconds(0.25),
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
                };

                MenuIndicatorTransform.BeginAnimation(TranslateTransform.YProperty, animation);
            }
        }

        private void FilterByTag(string tag)
        {
            _vm.FilterMenu(tag);
            // Cập nhật ItemsSource nếu Binding không tự động (hoặc để ép refresh)
            var itemsControl = this.FindName("ItemsControlMenu") as ItemsControl;
            if (itemsControl != null)
            {
                itemsControl.ItemsSource = _vm.MenuHienThi;
            }
        }

        // Xử lý khi bấm vào Món ăn
        private void Product_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MonAn monAn)
            {
                // Kiểm tra nhanh xem món còn không (gọi thẳng ViewModel)
                bool isConMon = _vm.KiemTraConMon(monAn.MaMon);

                if (!isConMon)
                {
                    MessageBox.Show($"Món '{monAn.TenMon}' vừa mới HẾT HÀNG!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _vm.FilterMenu(_vm.CurrentCategoryTag);
                    return;
                }
                if (monAn.IsSoldOut)
                {
                    MessageBox.Show($"Món {monAn.TenMon} đã hết hàng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Yêu cầu chọn bàn nếu chưa có
                if (_vm.CurrentTable <= 0)
                {
                    if (_vm.ChonBanCommand.CanExecute(null))
                        _vm.ChonBanCommand.Execute(null);
                    if (_vm.CurrentTable <= 0) return;
                }

                // Mở popup chi tiết món
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

        // Xử lý nút Thanh toán / Giỏ hàng
        private void BtnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.GioHang.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cartWindow = new GioHangWindow(_vm);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            bool? isConfirmed = cartWindow.ShowDialog();

            if (mainWindow != null) mainWindow.Opacity = 1;

            if (isConfirmed == true)
            {
                // Logic hỏi Tích điểm
                bool isGuest = _vm.CurrentCustomer == null || _vm.CurrentCustomer.HoTen.Contains("Khách Mới");

                if (isGuest)
                {
                    var result = MessageBox.Show("Quý khách có muốn tích điểm không?", "Khách hàng thân thiết", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var tichDiemWin = new TichDiemWindow(_vm);
                        tichDiemWin.Owner = Application.Current.MainWindow;
                        bool? dialogResult = tichDiemWin.ShowDialog();

                        if (dialogResult != true)
                        {
                            _vm.ResetSession();
                            return; // Hủy gửi đơn nếu thoát tích điểm giữa chừng
                        }
                    }
                }

                if (_vm.SubmitOrder())
                {
                    MessageBox.Show("Đặt món thành công! Vui lòng đợi trong giây lát.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Xử lý Đăng xuất
        private async void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await Task.Delay(200);
                var mainWindow = Application.Current.MainWindow as MainWindow;
                // Gọi hàm chuyển view trong MainWindow
                mainWindow?.ChuyenSangDangNhap();
            }
        }

        private void BtnTichDiem_Click(object sender, RoutedEventArgs e)
        {
            var popup = new TichDiemWindow(_vm);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;
            popup.Owner = mainWindow;
            popup.ShowDialog();
            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        private void BtnDanhGia_Click(object sender, RoutedEventArgs e)
        {
            var reviewWindow = new DanhGiaWindow(_vm);

            // Làm mờ màn hình chính cho đẹp
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            reviewWindow.Owner = mainWindow;
            reviewWindow.ShowDialog();

            // Khôi phục độ sáng
            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        // --- Xử lý đổi ngôn ngữ ---

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
            if (txtLangFlag != null) txtLangFlag.Text = "🇻🇳";
            if (txtLangName != null) txtLangName.Text = "Tiếng Việt";
            UpdateGreeting(); // Cập nhật lại câu chào ngay
        }

        private void MenuItem_EN_Click(object sender, RoutedEventArgs e)
        {
            LanguageHelper.SetLanguage("en");
            if (txtLangFlag != null) txtLangFlag.Text = "🇺🇸";
            if (txtLangName != null) txtLangName.Text = "English";
            UpdateGreeting(); // Cập nhật lại câu chào ngay
        }

        private string GetRes(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }
    }
}
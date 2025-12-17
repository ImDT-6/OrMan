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

        private void TestTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Khi kéo slider, gọi hàm update với giá trị của slider
            // Ép kiểu double sang int
            if (_vm != null) // check null cho chắc
            {
                UpdateGreeting((int)e.NewValue);

                // Mẹo: Tạm dừng cái ClockTimer lại để nó không tự reset về giờ thật sau 30s
                if (_clockTimer != null && _clockTimer.IsEnabled)
                    _clockTimer.Stop();
            }
        }
        private void UpdateGreeting(int? fakeHour = null)
        {
            var now = DateTime.Now;
            int hour = fakeHour ?? now.Hour;

            // Reset ẩn hết các background
            if (GridMorning != null) GridMorning.Visibility = Visibility.Collapsed;
            if (GridNoon != null) GridNoon.Visibility = Visibility.Collapsed;
            if (GridAfternoon != null) GridAfternoon.Visibility = Visibility.Collapsed;
            if (GridNight != null) GridNight.Visibility = Visibility.Collapsed;

            string greetingText = "Xin chào";

            // --- LOGIC CHIA GIỜ VÀ SET TEXT ---
            if (hour >= 5 && hour < 11) // Sáng (5h - 11h)
            {
                if (GridMorning != null) GridMorning.Visibility = Visibility.Visible;

                // Cố gắng lấy từ Resource, nếu không có thì gán cứng
                string res = Application.Current.TryFindResource("Str_GoodMorning") as string;
                greetingText = !string.IsNullOrEmpty(res) ? res : "Chào buổi sáng";
            }
            else if (hour >= 11 && hour < 14) // Trưa (11h - 14h)
            {
                if (GridNoon != null) GridNoon.Visibility = Visibility.Visible;

                // Có thể bạn chưa có key "Str_GoodNoon", nên dùng fallback
                string res = Application.Current.TryFindResource("Str_GoodNoon") as string;
                greetingText = !string.IsNullOrEmpty(res) ? res : "Chào buổi trưa";
            }
            else if (hour >= 14 && hour < 18) // Chiều (14h - 18h)
            {
                if (GridAfternoon != null) GridAfternoon.Visibility = Visibility.Visible;

                string res = Application.Current.TryFindResource("Str_GoodAfternoon") as string;
                greetingText = !string.IsNullOrEmpty(res) ? res : "Chào buổi chiều";
            }
            else // Tối (18h - 5h sáng hôm sau)
            {
                if (GridNight != null) GridNight.Visibility = Visibility.Visible;

                // Tối muộn thì Chúc ngủ ngon, còn mới tối thì Chào buổi tối
                string key = (hour >= 22 || hour < 4) ? "Str_GoodNight" : "Str_GoodEvening";

                string res = Application.Current.TryFindResource(key) as string;
                // Fallback text Việt hóa luôn cho chắc
                if (string.IsNullOrEmpty(res))
                {
                    greetingText = (key == "Str_GoodNight") ? "Chúc ngủ ngon" : "Chào buổi tối";
                }
                else
                {
                    greetingText = res;
                }
            }

            // Cập nhật lên giao diện
            if (txtGreeting != null)
            {
                txtGreeting.Text = greetingText;
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
            // 1. Kiểm tra giỏ hàng
            if (_vm.GioHang.Count == 0)
            {
                MessageBox.Show(GetRes("Str_Msg_CartEmpty"), GetRes("Str_Title_Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Mở cửa sổ Giỏ hàng
            var cartWindow = new GioHangWindow(_vm);
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            bool? isConfirmed = cartWindow.ShowDialog();

            if (mainWindow != null) mainWindow.Opacity = 1;

            if (isConfirmed == true)
            {
                // Kiểm tra xem có phải khách vãng lai không
                bool isGuest = _vm.CurrentCustomer == null ||
                               _vm.CurrentCustomer.KhachHangID == 0 ||
                               _vm.CurrentCustomer.HoTen == "Khách Mới" ||
                               _vm.CurrentCustomer.HoTen == "Khách Hàng Mới";

                if (isGuest)
                {
                    var result = MessageBox.Show(GetRes("Str_Msg_AskLoyalty"), GetRes("Str_Title_Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Mở cửa sổ tích điểm
                        var tichDiemWin = new TichDiemWindow(_vm);
                        tichDiemWin.Owner = Application.Current.MainWindow;

                        // Lấy kết quả trả về
                        bool? dialogResult = tichDiemWin.ShowDialog();

                        // [LOGIC QUAN TRỌNG ĐỂ "LÀM LẠI TỪ ĐẦU"]
                        if (dialogResult != true)
                        {
                            // Trường hợp này là: Đã mở cửa sổ lên (thậm chí đã đăng ký xong) 
                            // nhưng lại bấm nút X thoát ra mà chưa bấm "HOÀN TẤT".

                            // -> Hủy bỏ khách hàng vừa đăng ký (nếu có), reset về khách lẻ
                            _vm.ResetSession();

                            // -> Dừng lại, KHÔNG gửi đơn
                            return;
                        }

                        // Nếu chạy xuống đây nghĩa là dialogResult == true (Đã bấm HOÀN TẤT đàng hoàng)
                    }
                }

                // 4. Gửi đơn hàng
                if (_vm.SubmitOrder())
                {
                    MessageBox.Show(GetRes("Str_Msg_OrderSuccess"), GetRes("Str_Title_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
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
        private void BtnDanhGia_Click(object sender, RoutedEventArgs e)
        {
            // Tạo màn hình đen mờ che phía sau (tùy chọn cho đẹp)
            // Hoặc đơn giản chỉ cần 2 dòng này:

            var reviewWindow = new DanhGiaWindow();

            // ShowDialog giúp chặn thao tác ở màn hình chính cho đến khi đóng đánh giá
            reviewWindow.ShowDialog();
        }
    }

}
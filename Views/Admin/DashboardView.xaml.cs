using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation; // [QUAN TRỌNG] Thêm dòng này để chạy Animation
using OrMan.Models;
using OrMan.ViewModels;

namespace OrMan.Views.Admin
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            var vm = new DashboardViewModel();
            this.DataContext = vm;

            // Lắng nghe yêu cầu chuyển trang từ ViewModel
            vm.RequestNavigationToTable += (ban) =>
            {
                var adminView = FindParent<AdminView>(this);
                if (adminView != null)
                {
                    adminView.ChuyenDenBanCanXuLy(ban);
                }
            };

            this.Unloaded += DashboardView_Unloaded;
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is DashboardViewModel vm)
            {
                vm.Cleanup();
            }
        }

        // --- [MỚI] XỬ LÝ SLIDING FILTER (THANH LỌC THỜI GIAN - VIÊN THUỐC TRƯỢT) ---
        private void FilterOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn)
            {
                UpdateFilterIndicator(btn);
            }
        }

        // Sự kiện Loaded của nút "Hôm nay" (hoặc nút mặc định) để khởi tạo vị trí Indicator ban đầu
        private void FilterOption_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn && btn.IsChecked == true)
            {
                // Gọi ngay để set vị trí ban đầu
                // Dùng Dispatcher để đảm bảo UI đã render xong layout mới tính toán vị trí chính xác
                Dispatcher.BeginInvoke(new Action(() => UpdateFilterIndicator(btn)), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void UpdateFilterIndicator(RadioButton selectedButton)
        {
            // Kiểm tra các element có tồn tại trong XAML không (FilterIndicator & FilterGrid phải có x:Name trong XAML)
            if (FilterIndicator == null || FilterGrid == null) return;

            // 1. Hiện Indicator (ban đầu Opacity=0 để tránh hiện sai vị trí lúc load)
            FilterIndicator.Opacity = 1;

            try
            {
                // 2. Tính vị trí tương đối của nút được chọn so với container cha (FilterGrid)
                Point relativeLocation = selectedButton.TranslatePoint(new Point(0, 0), FilterGrid);

                // 3. Animation thay đổi chiều rộng (Width) của viên thuốc cho bằng chiều rộng nút
                DoubleAnimation widthAnimation = new DoubleAnimation
                {
                    To = selectedButton.ActualWidth,
                    Duration = TimeSpan.FromSeconds(0.3), // Thời gian chạy animation (0.3s)
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } // Hiệu ứng trượt mượt mà (nhanh đầu, chậm dần cuối)
                };
                FilterIndicator.BeginAnimation(WidthProperty, widthAnimation);

                // 4. Animation di chuyển (Translate X) viên thuốc đến vị trí nút mới
                DoubleAnimation translateAnimation = new DoubleAnimation
                {
                    To = relativeLocation.X,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                FilterIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
            }
            catch { }
        }

        // --- XỬ LÝ SLIDING TAB (THANH TRƯỢT MÀU VÀNG Ở DƯỚI) ---

        private void FeedbackTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateIndicator();
        }

        private void FeedbackTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Chỉ chạy hiệu ứng khi sự kiện đến từ chính TabControl (tránh nhầm với sự kiện click của ListBox con bên trong)
            if (e.Source is TabControl)
            {
                UpdateIndicator();
            }
        }

        private void UpdateIndicator()
        {
            // Tìm TabItem đang được chọn
            if (!(FeedbackTabControl.SelectedItem is TabItem selectedTab)) return;

            // Tìm thanh trượt (PART_Indicator) trong template của TabControl
            var indicator = FeedbackTabControl.Template.FindName("PART_Indicator", FeedbackTabControl) as Border;
            if (indicator == null) return;

            // Tìm Transform để di chuyển thanh trượt
            var transform = indicator.RenderTransform as TranslateTransform;
            if (transform == null) return;

            // Dùng Dispatcher để đảm bảo UI đã render xong mới tính toán vị trí
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Lấy vị trí X của tab hiện tại so với TabControl cha
                    Point relativeLocation = selectedTab.TranslatePoint(new Point(0, 0), FeedbackTabControl);

                    // Animation 1: Thay đổi chiều rộng (Width) cho khớp với độ rộng của tab mới
                    DoubleAnimation widthAnimation = new DoubleAnimation
                    {
                        To = selectedTab.ActualWidth,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    indicator.BeginAnimation(WidthProperty, widthAnimation);

                    // Animation 2: Di chuyển (Translate X) đến vị trí mới
                    DoubleAnimation translateAnimation = new DoubleAnimation
                    {
                        To = relativeLocation.X,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    transform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
        // ----------------------------------------

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}
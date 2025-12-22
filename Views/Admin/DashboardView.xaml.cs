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

        // --- [MỚI] XỬ LÝ SLIDING TAB (TRƯỢT) ---

        private void FeedbackTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateIndicator();
        }

        private void FeedbackTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Chỉ chạy hiệu ứng khi sự kiện đến từ chính TabControl (tránh nhầm với ListBox con)
            if (e.Source is TabControl)
            {
                UpdateIndicator();
            }
        }

        private void UpdateIndicator()
        {
            // Tìm TabItem đang được chọn
            if (!(FeedbackTabControl.SelectedItem is TabItem selectedTab)) return;

            // Tìm thanh trượt (PART_Indicator) trong template
            var indicator = FeedbackTabControl.Template.FindName("PART_Indicator", FeedbackTabControl) as Border;
            var transform = indicator?.RenderTransform as TranslateTransform;

            if (indicator == null || transform == null) return;

            // Dùng Dispatcher để đảm bảo UI đã render xong mới tính toán vị trí
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Tính toán vị trí X của tab hiện tại so với TabControl
                    Point relativeLocation = selectedTab.TranslatePoint(new Point(0, 0), FeedbackTabControl);

                    // Animation 1: Thay đổi chiều rộng (Width) cho khớp với tab mới
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
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OrMan.ViewModels.Admin;

namespace OrMan.Views.Admin
{
    public partial class ThucDonView : UserControl
    {
        private ThucDonViewModel vm;
        private DispatcherTimer _searchTimer;

        public ThucDonView()
        {
            InitializeComponent();

            vm = new ThucDonViewModel();
            this.DataContext = vm; // [QUAN TRỌNG] Binding hoạt động nhờ dòng này

            this.Loaded += ThucDonView_Loaded;
            this.Unloaded += ThucDonView_Unloaded;

            // Cấu hình bộ đếm tìm kiếm (Debounce)
            _searchTimer = new DispatcherTimer();
            _searchTimer.Interval = TimeSpan.FromMilliseconds(100); // Đợi 0.3s
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private void ThucDonView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set tab mặc định lúc mở lên
            if (MenuTabControl.Items.Count > 0 && MenuTabControl.Items[0] is TabItem firstTab)
            {
                string tag = firstTab.Tag?.ToString() ?? "Mì Cay";
                // Báo ViewModel biết đang ở tab nào để lọc đúng danh sách
                vm.SetCurrentTab(tag);
            }
            UpdateIndicator();
        }

        private void ThucDonView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_searchTimer != null) _searchTimer.Stop();
        }

        // --- XỬ LÝ TÌM KIẾM (DEBOUNCE) ---
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Mỗi khi gõ phím, reset lại đồng hồ
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            // Hết giờ (ngừng gõ 0.3s) -> Mới đẩy từ khóa vào ViewModel
            // ViewModel sẽ tự lọc và cập nhật DanhSachHienThi -> UI tự đổi nhờ Binding
            vm.TuKhoaTimKiem = txtSearch.Text;
        }
        // ---------------------------------

        private void MenuTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateIndicator();
        }

        private void MenuTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is TabControl)
            {
                if (MenuTabControl.SelectedItem is TabItem selectedTab)
                {
                    string tag = selectedTab.Tag as string;
                    // Báo ViewModel đổi tab -> ViewModel tự lọc lại -> UI tự cập nhật
                    vm.SetCurrentTab(tag);
                    UpdateIndicator();
                }
            }
        }

        private void UpdateIndicator()
        {
            if (!(MenuTabControl.SelectedItem is TabItem selectedTab)) return;
            var indicator = MenuTabControl.Template.FindName("PART_Indicator", MenuTabControl) as Border;
            var transform = indicator?.RenderTransform as TranslateTransform;
            if (indicator == null || transform == null) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Point relativeLocation = selectedTab.TranslatePoint(new Point(0, 0), MenuTabControl);
                    DoubleAnimation widthAnimation = new DoubleAnimation { To = selectedTab.ActualWidth, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                    indicator.BeginAnimation(WidthProperty, widthAnimation);
                    DoubleAnimation translateAnimation = new DoubleAnimation { To = relativeLocation.X, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                    transform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            this.Focus();
        }
    }
}
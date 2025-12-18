using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OrMan.ViewModels.Admin;

namespace OrMan.Views.Admin
{
    public partial class ThucDonView : UserControl
    {
        private ThucDonViewModel vm;

        public ThucDonView()
        {
            InitializeComponent();

            vm = new ThucDonViewModel();
            this.DataContext = vm;

            vm.PropertyChanged += Vm_PropertyChanged;

            Loaded += (sender, e) => RefreshData();

            // [BỔ SUNG] Dọn dẹp khi rời trang
            this.Unloaded += ThucDonView_Unloaded;
        }

        private void ThucDonView_Unloaded(object sender, RoutedEventArgs e)
        {
            vm.PropertyChanged -= Vm_PropertyChanged;
            // Nếu sau này ThucDonViewModel có Timer, hãy gọi vm.Cleanup() ở đây
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Khi ViewModel tải xong dữ liệu (Async), nó sẽ báo hiệu vào đây
            if (e.PropertyName == "DataUpdated" || e.PropertyName == "TuKhoaTimKiem")
            {
                // Vì RefreshData thao tác UI, nên bọc trong Dispatcher cho chắc chắn
                Dispatcher.Invoke(() => RefreshData());
            }
        }

        private void MenuTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateIndicator();
        }

        private void MenuTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (MenuTabControl.SelectedItem is TabItem selectedTab)
                {
                    string tag = selectedTab.Tag as string;
                    vm.SetCurrentTab(tag);
                    RefreshData();
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

                    DoubleAnimation widthAnimation = new DoubleAnimation
                    {
                        To = selectedTab.ActualWidth,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    indicator.BeginAnimation(WidthProperty, widthAnimation);

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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void RefreshData()
        {
            // Lấy danh sách đã lọc từ ViewModel
            var filteredData = vm.GetFilteredList();

            if (MenuTabControl.SelectedItem is TabItem selectedTab)
            {
                string tag = selectedTab.Tag as string;

                if (tag == "Mì Cay" && ListBoxMiCay != null)
                    ListBoxMiCay.ItemsSource = filteredData;
                else if (tag == "Đồ Chiên" && ListBoxDoChien != null)
                    ListBoxDoChien.ItemsSource = filteredData;
                else if (tag == "Nước Uống" && ListBoxNuocUong != null)
                    ListBoxNuocUong.ItemsSource = filteredData;
            }
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            this.Focus();
        }
    }
}
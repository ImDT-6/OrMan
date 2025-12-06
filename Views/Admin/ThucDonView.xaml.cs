using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation; // Thêm thư viện Animation
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

            // Sự kiện Loaded của toàn bộ View (giữ nguyên logic cũ)
            Loaded += (sender, e) => RefreshData();
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DataUpdated" || e.PropertyName == "TuKhoaTimKiem")
            {
                RefreshData();
            }
        }

        // --- BỔ SUNG: Sự kiện Loaded riêng cho TabControl để khởi tạo vị trí thanh trượt ---
        private void MenuTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Cập nhật vị trí thanh trượt ngay khi TabControl load xong
            UpdateIndicator();
        }

        private void MenuTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Kiểm tra đúng là sự kiện của TabControl (tránh nhầm lẫn với ListBox bên trong)
            if (e.Source is TabControl)
            {
                if (MenuTabControl.SelectedItem is TabItem selectedTab)
                {
                    // Logic cũ: Update VM
                    string tag = selectedTab.Tag as string;
                    vm.SetCurrentTab(tag);
                    RefreshData(); // Gọi lại refresh data cho tab mới

                    // Logic mới: Chạy Animation thanh trượt
                    UpdateIndicator();
                }
            }
        }

        // --- BỔ SUNG: Hàm xử lý Animation thanh trượt ---
        private void UpdateIndicator()
        {
            if (!(MenuTabControl.SelectedItem is TabItem selectedTab)) return;

            // Tìm thanh trượt (PART_Indicator) trong Template
            var indicator = MenuTabControl.Template.FindName("PART_Indicator", MenuTabControl) as Border;
            var transform = indicator?.RenderTransform as TranslateTransform;

            if (indicator == null || transform == null) return;

            // Sử dụng Dispatcher để đảm bảo UI đã render xong trước khi lấy tọa độ
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Lấy vị trí tương đối của TabItem so với TabControl
                    Point relativeLocation = selectedTab.TranslatePoint(new Point(0, 0), MenuTabControl);

                    // 1. Animation chiều rộng (Width)
                    DoubleAnimation widthAnimation = new DoubleAnimation
                    {
                        To = selectedTab.ActualWidth,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    indicator.BeginAnimation(WidthProperty, widthAnimation);

                    // 2. Animation vị trí (X)
                    DoubleAnimation translateAnimation = new DoubleAnimation
                    {
                        To = relativeLocation.X,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    transform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
                }
                catch { } // Bỏ qua lỗi nếu UI chưa sẵn sàng
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Logic tìm kiếm đã được bind vào ViewModel qua PropertyChanged, 
            // nhưng nếu bạn muốn xử lý thêm gì ở View thì viết ở đây.
        }

        private void RefreshData()
        {
            // Giữ nguyên logic cũ của bạn
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
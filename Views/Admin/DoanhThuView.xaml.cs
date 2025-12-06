using OrMan.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation; // Thêm thư viện Animation

namespace OrMan.Views.Admin
{
    public partial class DoanhThuView : UserControl
    {
        private DoanhThuViewModel _viewModel;

        public DoanhThuView()
        {
            InitializeComponent();
            _viewModel = new DoanhThuViewModel();
            this.DataContext = _viewModel;
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            this.Focus();
        }

        // --- HÀM MỚI: Xử lý hiệu ứng trượt nút lọc ---
        private void TimeFilter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null) return;

            // 1. Tính toán vị trí cần đến
            // Tìm thứ tự (index) của nút bấm trong StackPanel (0, 1, 2, 3)
            int index = TimeFilterPanel.Children.IndexOf(button);

            // Chiều rộng mỗi nút là 90 (đã set trong XAML Style)
            double targetX = index * 90;

            // 2. Chạy Animation dịch chuyển thanh tím
            DoubleAnimation animation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            IndicatorTransform.BeginAnimation(TranslateTransform.XProperty, animation);

            // 3. Gọi ViewModel để lọc dữ liệu (Nếu ViewModel của bạn có hàm Filter)
            string filterType = button.Tag as string;
            // Ví dụ: _viewModel.FilterByTime(filterType); 
        }
    }
}
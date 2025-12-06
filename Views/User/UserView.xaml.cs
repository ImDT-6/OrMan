using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation; // Cần dòng này cho Animation
using OrMan.Models;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class UserView : UserControl
    {
        private UserViewModel _vm;

        public UserView()
        {
            InitializeComponent();
            _vm = new UserViewModel();
            this.DataContext = _vm;

            // Mặc định chọn tab đầu tiên
            FilterByTag("Mì Cay");
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null) return;

            // 1. Logic lọc món ăn
            if (button.Tag is string tag)
            {
                FilterByTag(tag);
            }

            // 2. Logic Animation Trượt Dọc [MỚI]
            // Tìm vị trí nút bấm trong danh sách (0, 1, 2)
            int index = MenuPanel.Children.IndexOf(button);

            // Mỗi nút cao 60px (như đã set trong Style)
            double targetY = index * 60;

            // Chạy Animation dịch chuyển thanh tím
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
                    MessageBox.Show($"Món '{monAn.TenMon}' hiện đang tạm hết hàng.\nVui lòng chọn món khác nhé!", "Rất tiếc", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    MessageBox.Show("Đã gửi đơn xuống bếp thành công!\nVui lòng đợi trong giây lát.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            if (mainWindow != null) mainWindow.Opacity = 1;
        }

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn muốn kết thúc phiên gọi món?", "Xác nhận",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ChuyenSangDangNhap();
            }
        }
    }
}
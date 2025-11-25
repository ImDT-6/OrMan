using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.ViewModels.User;

namespace GymManagement.Views.User
{
    public partial class UserView : UserControl
    {
        private UserViewModel _vm;

        public UserView()
        {
            InitializeComponent();
            _vm = new UserViewModel();
            this.DataContext = _vm;

            FilterByTag("Mì Cay");
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                FilterByTag(rb.Tag.ToString());
            }
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
                // [SỬA QUAN TRỌNG] Kiểm tra trạng thái hết hàng trước
                if (monAn.IsSoldOut)
                {
                    MessageBox.Show($"Món '{monAn.TenMon}' hiện đang tạm hết hàng.\nVui lòng chọn món khác nhé!", "Rất tiếc", MessageBoxButton.OK, MessageBoxImage.Information);
                    return; // Dừng lại, không mở popup
                }

                var popup = new ChiTietMonWindow(monAn);

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

            if (MessageBox.Show($"Xác nhận gửi đơn hàng?\nTổng tiền: {_vm.TongTienCart:N0} VNĐ", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (_vm.SubmitOrder())
                {
                    MessageBox.Show("Đã gửi đơn xuống bếp thành công!\nVui lòng đợi trong giây lát.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
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
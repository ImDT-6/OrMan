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

            // Mặc định load Mì Cay
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
            // Cập nhật lại UI
            ((ItemsControl)this.FindName("ItemsControlMenu")).ItemsSource = _vm.MenuHienThi;
        }

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MonAn monAn)
            {
                var popup = new ChiTietMonWindow(monAn);
                if (popup.ShowDialog() == true)
                {
                    _vm.AddToCart(monAn, popup.SoLuong, popup.CapDoCay, popup.GhiChu);
                }
            }
        }

        private void BtnGoiNhanVien_Click(object sender, RoutedEventArgs e)
        {
            // Tính năng mở rộng: Hiện popup chọn yêu cầu (Lấy nước, tính tiền...)
            MessageBox.Show("Đã gửi yêu cầu đến nhân viên phục vụ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.GioHang.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống! Vui lòng chọn món.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ở đây có thể mở một cửa sổ CartView chi tiết hơn
            MessageBox.Show($"Đã xác nhận đơn hàng!\nTổng tiền: {_vm.TongTienCart:N0} VNĐ\nBếp đang chuẩn bị món...", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // [MỚI] Logic Đăng Xuất
        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc muốn kết thúc phiên và đăng xuất?", "Xác nhận",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ChuyenSangDangNhap();
            }
        }
    }
}
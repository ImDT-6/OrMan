using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.ViewModels.User;

namespace GymManagement.Views.User
{
    public partial class UserView : UserControl
    {
        private UserViewModel _vm;
        private const int CurrentTable = 1; // Giả định số bàn hiện tại

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
            // [SỬA LỖI] CHỈ cần gọi ViewModel để lọc, không cần gán ItemsSource thủ công.
            // ItemsSource sẽ tự động cập nhật qua Binding và PropertyChanged của MenuHienThi.
            _vm.FilterMenu(tag);

            // Xóa đoạn code sau, vì nó là nguyên nhân gây lỗi hoặc là code thủ công không cần thiết:
            /*
            var itemsControl = this.FindName("ItemsControlMenu") as ItemsControl;
            if (itemsControl != null)
            {
                itemsControl.ItemsSource = _vm.MenuHienThi;
            }
            */
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

        // [MỚI] Hàm xử lý khi bấm nút "GỌI PHỤC VỤ"
        private void BtnCallStaff_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra xem bàn có đơn hàng đang hoạt động không
            bool hasActiveOrder = _vm.HasActiveOrder(CurrentTable);

            // 2. Mở Pop-up cho khách hàng chọn
            var requestWindow = new SupportRequestWindow(hasActiveOrder);

            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            if (requestWindow.ShowDialog() == true)
            {
                if (requestWindow.SelectedRequest == RequestType.Checkout)
                {
                    // Yêu cầu Thanh toán (Chỉ gọi khi có đơn)
                    _vm.RequestCheckout(CurrentTable);
                    MessageBox.Show("Đã gửi yêu cầu THANH TOÁN tới Admin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (requestWindow.SelectedRequest == RequestType.Support)
                {
                    // Yêu cầu Hỗ trợ/Phục vụ
                    _vm.RequestSupport(CurrentTable);
                    MessageBox.Show("Đã gửi yêu cầu GỌI PHỤC VỤ tới Admin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            if (mainWindow != null) mainWindow.Opacity = 1;
        }


        // [SỬA TÊN] Đổi tên hàm từ BtnThanhToan_Click thành BtnGuiDon_Click cho đúng ngữ nghĩa
        private void BtnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.GioHang.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống! Vui lòng chọn món trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Mở cửa sổ Giỏ Hàng
            var cartWindow = new GioHangWindow(_vm);

            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null) mainWindow.Opacity = 0.4;

            // Nếu khách bấm "Gửi Đơn" trong cửa sổ kia (DialogResult == true)
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
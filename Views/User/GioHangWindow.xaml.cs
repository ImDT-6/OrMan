using System.Windows;
using System.Windows.Controls; // Cần thêm cái này để dùng Button
using OrMan.Models;
using OrMan.ViewModels.User;
// Nhớ using namespace chứa class Model của Item trong giỏ hàng, ví dụ: OrMan.Models

namespace OrMan.Views.User
{
    public partial class GioHangWindow : Window
    {
        private UserViewModel _vm;

        public GioHangWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = _vm;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        // [MỚI] Xử lý sự kiện xóa
       

            // --- [ LOGIC XÓA HẲN ] ---
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var itemToDelete = btn.DataContext as CartItem;

            if (itemToDelete != null)
            {
                var result = MessageBox.Show($"Bạn có chắc muốn bỏ món '{itemToDelete.TenHienThi}' không?",
                                             "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _vm.XoaMonKhoiGio(itemToDelete);
                }
            }
        }

        // --- [MỚI] LOGIC GIẢM SỐ LƯỢNG ---
        private void BtnDecrease_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn.DataContext as CartItem;

            if (item != null)
            {
                // Gọi ViewModel xử lý giảm
                _vm.GiamSoLuongMon(item);
            }
        }

        // --- [MỚI] LOGIC TĂNG SỐ LƯỢNG ---
        private void BtnIncrease_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn.DataContext as CartItem;

            if (item != null)
            {
                // Gọi ViewModel xử lý tăng
                _vm.TangSoLuongMon(item);
            }
        }
    }
}
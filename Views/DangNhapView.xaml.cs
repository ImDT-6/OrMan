using System.Linq;
using System.Threading.Tasks; // Dùng cho Task.Delay
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Dùng cho KeyEventArgs
using OrMan.Models;
using OrMan.ViewModels;

namespace OrMan.Views
{
    public partial class DangNhapView : UserControl
    {
        private DangNhapViewModel vm;

        public DangNhapView()
        {
            InitializeComponent();
            vm = new DangNhapViewModel();
            DataContext = vm;
        }
        private void txtVisiblePass_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Bấm LÊN thì quay về ô Tài khoản
            if (e.Key == Key.Up)
            {
                txtUser.Focus();
                e.Handled = true;
            }
            // Bấm ENTER thì gọi lệnh Đăng nhập
            else if (e.Key == Key.Enter)
            {
                Button_Click(sender, e);
                e.Handled = true;
            }
        }
        // 1. XỬ LÝ TẠI Ô TÀI KHOẢN (Dùng PreviewKeyDown)


        private void txtUser_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Down)
            {
                // [SỬA] Kiểm tra xem đang ở chế độ hiện mật khẩu hay ẩn
                if (btnEye.IsChecked == true)
                {
                    // Nếu đang hiện pass -> Focus vào ô TextBox hiện
                    txtVisiblePass.Focus();
                    // Đặt con trỏ về cuối dòng cho tiện nhập tiếp
                    txtVisiblePass.CaretIndex = txtVisiblePass.Text.Length;
                }
                else
                {
                    // Nếu đang ẩn pass -> Focus vào PasswordBox như cũ
                    pwdBox.Focus();
                }

                e.Handled = true;
            }
        }

        // 2. XỬ LÝ TẠI Ô MẬT KHẨU (Dùng PreviewKeyDown)
        private void pwdBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                txtUser.Focus();
                e.Handled = true;
            }
        }

        // Xử lý nút Đăng nhập (Logic này giữ nguyên)
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            vm.MatKhau = pwdBox.Password;

            if (!vm.ValidateInput())
            {
                // Đợi 0.2 giây rồi mới focus
                await Task.Delay(200);

                // Nếu có lỗi, Focus vào ô lỗi đầu tiên tìm thấy
                if (vm.GetErrors(nameof(vm.TaiKhoan)) != null)
                {
                    txtUser.Focus();
                }
                else if (vm.GetErrors(nameof(vm.MatKhau)) != null)
                {
                    pwdBox.Focus();
                }
                return; // Dừng lại, không xử lý đăng nhập tiếp
            }

            // --- LOGIC ĐĂNG NHẬP SAU KHI PASS VALIDATION ---
            var admin = new OrMan.Models.Admin("admin", "123", "Quản lý nhà hàng");
            var user = new OrMan.Models.User("user", "123", "Nguyễn Văn A", "VIP Gold", 1500);

            if (vm.TaiKhoan == admin.TaiKhoan && vm.MatKhau == admin.MatKhau)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ChuyenSangAdmin();
            }
            else if (vm.TaiKhoan == user.TaiKhoan && vm.MatKhau == user.MatKhau)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ChuyenSangUser();
            }
            else
            {
                MessageBox.Show("Sai tài khoản hoặc mật khẩu!", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                pwdBox.Clear();
                pwdBox.Focus();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging; // Thư viện xử lý ảnh WPF
using OrMan.Models;
using OrMan.ViewModels;
using System.Threading.Tasks;

namespace OrMan.Views
{
    public partial class DangNhapView : UserControl
    {
        private DangNhapViewModel vm;

        // List chứa sẵn các ảnh đã load lên RAM để animation mượt mà không bị nháy
        private List<ImageSource> mascotFrames = new List<ImageSource>();
        private ImageSource blindfoldImage; // Ảnh che mắt
        private ImageSource defaultImage;   // Ảnh mặc định
        private ImageSource noblindfoldImage; // Ảnh không che mắt

        public DangNhapView()
        {
            InitializeComponent();
            vm = new DangNhapViewModel();
            DataContext = vm;

            // Load trước hình ảnh vào bộ nhớ khi mở form
            PreloadMascotImages();
            UpdateBearFace();
        }

        private void PreloadMascotImages()
        {
            try
            {
                // 1. Load ảnh mặc định (dùng Frame 1)
                defaultImage = new BitmapImage(new Uri("pack://application:,,,/Images/debut.png"));
                imgMascot.Source = defaultImage;
                // 2. Load ảnh che mắt
                blindfoldImage = new BitmapImage(new Uri("pack://application:,,,/Images/textbox_password.png"));
                noblindfoldImage = new BitmapImage(new Uri("pack://application:,,,/Images/nocover.jpg"));

                // 3. Load chuỗi ảnh animation (từ 1 đến 20)
                // Giả sử bạn có 20 ảnh: textbox_user_1.jpg -> textbox_user_20.jpg
                // Nếu bạn có nhiều hơn, hãy sửa số 20 thành số lượng thực tế
                for (int i = 0; i <= 24; i++)
                {
                    string path = $"pack://application:,,,/Images/textbox_user_{i}.jpg";
                    var bitmap = new BitmapImage(new Uri(path));
                    mascotFrames.Add(bitmap);
                }
            }
            catch (Exception ex)
            {
                // Nếu thiếu ảnh thì thôi, không crash app
                Console.WriteLine("Lỗi load ảnh Gấu: " + ex.Message);
            }
        }

        // --- SỰ KIỆN 1: KHI GÕ TÀI KHOẢN (Gấu nhìn theo) ---
        private void txtUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Nếu đang nhập mật khẩu thì không đổi hình (vì đang che mắt)
            if (pwdBox.IsKeyboardFocusWithin || txtVisiblePass.IsKeyboardFocusWithin) return;

            UpdateBearFace();
        }

        // Hàm cập nhật mặt gấu dựa trên độ dài chữ
        private void UpdateBearFace()
        {
            if (mascotFrames.Count == 0) return;

            int textLength = txtUser.Text.Length;

            if (textLength <= 0)
            {
                imgMascot.Source = defaultImage;
            }
            else
            {
                // Mapping: 1 ký tự = 1 khung hình. 
                // Nếu gõ dài hơn số ảnh có sẵn thì lấy ảnh cuối cùng.
                // Trừ 1 vì List bắt đầu từ 0 (Frame 1 là index 0)
                int frameIndex = textLength - 1;

                if (frameIndex >= mascotFrames.Count)
                    frameIndex = mascotFrames.Count - 1; // Giữ ở ảnh cuối

                imgMascot.Source = mascotFrames[frameIndex];
            }
        }

        // --- SỰ KIỆN 2: KHI VÀO Ô MẬT KHẨU (Che mắt) ---
        private void pwdBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (blindfoldImage != null)
                imgMascot.Source = blindfoldImage;
        }

        // --- SỰ KIỆN 3: KHI RỜI Ô MẬT KHẨU (Mở mắt lại) ---
        private void pwdBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Quay lại trạng thái nhìn theo độ dài tài khoản
            imgMascot.Source = defaultImage;
            UpdateBearFace();
        }

        // --- CÁC LOGIC CŨ (GIỮ NGUYÊN) ---

        private void txtUser_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Down)
            {
                if (btnEye.IsChecked == true)
                {
                    txtVisiblePass.Focus();
                    txtVisiblePass.CaretIndex = txtVisiblePass.Text.Length;
                }
                else
                {
                    pwdBox.Focus();
                }
                e.Handled = true;
            }
        }

        private void pwdBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                txtUser.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                Button_Click(sender, e);
                e.Handled = true;
            }
        }

        private void txtVisiblePass_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                txtUser.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                Button_Click(sender, e);
                e.Handled = true;
            }
        }
        private void txtVisiblePass_GotFocus(object sender, RoutedEventArgs e)
        {
            // Khi bấm vào ô hiện mật khẩu -> Gấu phải mở mắt ra
            imgMascot.Source = noblindfoldImage;
        }
        private void BtnEye_Click(object sender, RoutedEventArgs e)
        {
            if (btnEye.IsChecked == true)
            {
                // TRƯỜNG HỢP: ĐANG HIỆN MẬT KHẨU (IsChecked = True)
                // Hành động: Gấu bỏ tay ra, mở mắt nhìn

                // Gọi hàm này để gấu quay về trạng thái bình thường 
                // (hoặc nhìn theo độ dài tên đăng nhập nếu muốn)
               
                UpdateBearFace();
                // Tiện tay Focus luôn vào ô hiện mật khẩu để người dùng gõ tiếp
                txtVisiblePass.Focus();
                txtVisiblePass.CaretIndex = txtVisiblePass.Text.Length;
            }
            else
            {
                // TRƯỜNG HỢP: ĐANG ẨN MẬT KHẨU (IsChecked = False)
                // Hành động: Gấu lấy tay CHE MẮT NGAY LẬP TỨC

                if (blindfoldImage != null)
                {
                    imgMascot.Source = blindfoldImage;
                }

                // Tiện tay Focus lại vào ô ẩn mật khẩu
                pwdBox.Focus();
            }
        }
        private void txtUser_GotFocus(object sender, RoutedEventArgs e)
        {
            // Yêu cầu: Cả 2 trường hợp (bật mắt hay tắt mắt) đều hiện trạng thái mặc định.
            // Không cần kiểm tra if (btnEye.IsChecked == true) nữa.

            // Gọi hàm này để gấu quay về trạng thái nhìn theo chữ (hoặc nhìn thẳng nếu ô trống)
            UpdateBearFace();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            vm.MatKhau = pwdBox.Password;

            if (!vm.ValidateInput())
            {
                await Task.Delay(200);
                if (vm.GetErrors(nameof(vm.TaiKhoan)) != null) txtUser.Focus();
                else if (vm.GetErrors(nameof(vm.MatKhau)) != null) pwdBox.Focus();
                return;
            }

            var admin = new OrMan.Models.Admin("admin", "123", "Quản lý");
            var user = new OrMan.Models.User("user", "123", "Nhân viên", "VIP", 0);

            var mainWindow = Application.Current.MainWindow as MainWindow;

            if (vm.TaiKhoan == admin.TaiKhoan && vm.MatKhau == admin.MatKhau)
            {
                mainWindow?.ChuyenSangAdmin();
            }
            else if (vm.TaiKhoan == user.TaiKhoan && vm.MatKhau == user.MatKhau)
            {
                mainWindow?.ChuyenSangUser();
            }
            else
            {
                MessageBox.Show("Sai tài khoản hoặc mật khẩu!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                pwdBox.Clear();
                pwdBox.Focus();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OrMan.Models;
using OrMan.ViewModels;
using System.Threading.Tasks;

namespace OrMan.Views
{
    public partial class DangNhapView : UserControl
    {
        private DangNhapViewModel vm;

        // List chứa ảnh (khởi tạo rỗng để tránh null)
        private List<ImageSource> mascotFrames = new List<ImageSource>();
        private ImageSource blindfoldImage;
        private ImageSource defaultImage;
        private ImageSource noblindfoldImage;

        public DangNhapView()
        {
            InitializeComponent();
            vm = new DangNhapViewModel();
            DataContext = vm;

            // [TỐI ƯU] Không load ảnh ở đây nữa để tránh đơ lúc khởi tạo
            // Chuyển sang sự kiện Loaded
            this.Loaded += DangNhapView_Loaded;
        }

        private async void DangNhapView_Loaded(object sender, RoutedEventArgs e)
        {
            // Gọi hàm load ảnh bất đồng bộ
            await PreloadMascotImagesAsync();
            UpdateBearFace();
        }

        // [QUAN TRỌNG] Hàm load ảnh chạy ngầm (Async)
        private async Task PreloadMascotImagesAsync()
        {
            // Chạy việc nặng ở luồng phụ
            var loadedData = await Task.Run(() =>
            {
                var frames = new List<ImageSource>();
                ImageSource def = null, blind = null, noblind = null;

                try
                {
                    // 1. Load các ảnh tĩnh
                    // Lưu ý: BitmapCacheOption.OnLoad + Freeze() là bắt buộc để truyền ảnh giữa các Thread
                    def = LoadImageFrozen("pack://application:,,,/Images/debut.JPG");
                    blind = LoadImageFrozen("pack://application:,,,/Images/textbox_password.png");
                    noblind = LoadImageFrozen("pack://application:,,,/Images/nocover.jpg");

                    // 2. Load chuỗi ảnh animation
                    // Giả sử ảnh được đặt tên liên tiếp: textbox_user_0, textbox_user_1...
                    for (int i = 0; i <= 50; i++)
                    {
                        string path = $"pack://application:,,,/Images/textbox_user_{i}.jpg";
                        var img = LoadImageFrozen(path);

                        if (img != null)
                        {
                            frames.Add(img);
                        }
                        else
                        {
                            // [TỐI ƯU CỰC MẠNH] 
                            // Nếu không tìm thấy ảnh thứ 'i', ta DỪNG LUÔN vòng lặp.
                            // Giả định là bạn đặt tên file liên tiếp (0, 1, 2...). 
                            // Nếu file 5 không có thì khả năng cao file 6-50 cũng không có -> Không cần try-catch tốn time.
                            break;
                        }
                    }
                }
                catch { /* Bỏ qua lỗi chung */ }

                // Trả về kết quả gói gọn
                return new { Frames = frames, Default = def, Blind = blind, NoBlind = noblind };
            });

            // Cập nhật lại biến ở luồng chính (UI Thread)
            if (loadedData != null)
            {
                this.mascotFrames = loadedData.Frames;
                this.defaultImage = loadedData.Default;
                this.blindfoldImage = loadedData.Blind;
                this.noblindfoldImage = loadedData.NoBlind;

                // Gán ảnh mặc định ngay khi load xong
                if (this.defaultImage != null) imgMascot.Source = this.defaultImage;
            }
        }

        // Hàm hỗ trợ load và đóng băng ảnh (để dùng được đa luồng)
        private BitmapImage LoadImageFrozen(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load ngay lập tức
                bitmap.EndInit();
                bitmap.Freeze(); // Đóng băng để share sang UI Thread
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        // --- CÁC SỰ KIỆN KHÁC GIỮ NGUYÊN ---

        private void txtUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (pwdBox.IsKeyboardFocusWithin || txtVisiblePass.IsKeyboardFocusWithin) return;
            UpdateBearFace();
        }

        private void UpdateBearFace()
        {
            // Thêm check null an toàn
            if (mascotFrames == null || mascotFrames.Count == 0)
            {
                if (defaultImage != null && imgMascot.Source != defaultImage)
                    imgMascot.Source = defaultImage;
                return;
            }

            try
            {
                int textLength = txtUser.Text.Length;
                if (textLength <= 0)
                {
                    if (defaultImage != null) imgMascot.Source = defaultImage;
                    return;
                }

                int frameIndex = textLength - 1;
                if (frameIndex >= mascotFrames.Count) frameIndex = mascotFrames.Count - 1;
                if (frameIndex < 0) frameIndex = 0;

                imgMascot.Source = mascotFrames[frameIndex];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi UpdateBearFace: " + ex.Message);
            }
        }

        private void pwdBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (blindfoldImage != null) imgMascot.Source = blindfoldImage;
        }

        private void pwdBox_LostFocus(object sender, RoutedEventArgs e)
        {
            imgMascot.Source = defaultImage;
            UpdateBearFace();
        }

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
            if (noblindfoldImage != null)
                imgMascot.Source = noblindfoldImage;
        }

        private void BtnEye_Click(object sender, RoutedEventArgs e)
        {
            if (btnEye.IsChecked == true)
            {
                UpdateBearFace();
                txtVisiblePass.Focus();
                txtVisiblePass.CaretIndex = txtVisiblePass.Text.Length;
            }
            else
            {
                if (blindfoldImage != null) imgMascot.Source = blindfoldImage;
                pwdBox.Focus();
            }
        }

        private void txtUser_GotFocus(object sender, RoutedEventArgs e)
        {
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

            // [MẸO] Dùng Task.Run để so sánh/xử lý đăng nhập nếu logic phức tạp hơn
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
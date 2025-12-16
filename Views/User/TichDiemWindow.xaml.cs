using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class TichDiemWindow : Window
    {
        private UserViewModel _vm;
        private bool _isRegisterMode = false;

        public TichDiemWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            txtPhone.Focus();
        }
        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            // Nếu bấm Enter khi đang nhập tên -> Gọi hành động Đăng ký luôn
            if (e.Key == Key.Enter)
            {
                HandleAction();
            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void txtPhone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) HandleAction();
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            HandleAction();
        }

        // [LOGIC CHÍNH ĐÃ ĐƯỢC SỬA LẠI]
        private void HandleAction()
        {
            if (lblError != null) lblError.Visibility = Visibility.Collapsed;

            string phone = txtPhone.Text.Trim();

            // 1. Validate Số điện thoại
            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            {
                ShowError("Số điện thoại không hợp lệ (cần ít nhất 9 số).");
                return;
            }

            // 2. Chế độ Đăng ký (Người dùng bấm nút Lần 2 sau khi nhập tên)
            if (_isRegisterMode)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    ShowError("Vui lòng nhập tên của bạn.");
                    txtName.Focus();
                    return;
                }

                // Gọi ViewModel để lưu vào Database
                var newKhach = _vm.RegisterCustomer(phone, name);
                ShowCustomerInfo(newKhach);
                return;
            }

            // 3. Chế độ Tra cứu (Người dùng bấm nút Lần 1)
            var khach = _vm.CheckMember(phone);

            if (khach == null)
            {
                // Chưa có trong DB -> Chuyển sang chế độ nhập tên
                SwitchToRegisterMode();
            }
            else
            {
                // Đã có -> Hiển thị thông tin
                ShowCustomerInfo(khach);
            }
        }

        private void SwitchToRegisterMode()
        {
            _isRegisterMode = true;
            pnlNameInput.Visibility = Visibility.Visible;

            // Đổi nút thành "Đăng Ký"
            btnAction.Content = "Đăng Ký Thành Viên";
            btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Màu cam

            string msg = "Số điện thoại chưa tồn tại. Vui lòng nhập tên để đăng ký mới.";
            ShowError(msg);
            if (lblError != null) lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Màu cam cho thông báo

            txtName.Focus();
        }

        private void ShowCustomerInfo(OrMan.Models.KhachHang khach)
        {
            txtPhone.IsEnabled = false;
            pnlNameInput.Visibility = Visibility.Collapsed;
            if (lblError != null) lblError.Visibility = Visibility.Collapsed;

            pnlResult.Visibility = Visibility.Visible;

            // Hiển thị tên
            lblTenKhach.Text = $"Xin chào, {khach.HoTen}";

            // Hiển thị hạng & điểm
            lblHang.Text = $"Hạng: {khach.HangThanhVien}";
            lblDiem.Text = $"{khach.DiemTichLuy:N0}";

            _vm.CurrentCustomer = khach;

            // --- CẤU HÌNH NÚT HOÀN TẤT ---
            btnAction.Content = GetRes("Str_Btn_Done");
            btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#22C55E"); // Xanh lá

            // Xóa sự kiện cũ
            btnAction.Click -= BtnAction_Click;

            // [QUAN TRỌNG] Chỉ khi bấm nút này mới được tính là Thành công (True)
            btnAction.Click += (s, ev) =>
            {
                this.DialogResult = true; // Đóng cửa sổ và báo về UserView là OK
            };

            // Focus vào nút để bấm Enter là xong luôn
            btnAction.Focus();
        }
        private string GetRes(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }
        private void ShowError(string msg)
        {
            if (lblError != null)
            {
                lblError.Text = msg;
                lblError.Visibility = Visibility.Visible;
                lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#EF4444"); // Màu đỏ lỗi
            }
            else
            {
                MessageBox.Show(msg);
            }
        }
    }
}
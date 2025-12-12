using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class TichDiemWindow : Window
    {
        private UserViewModel _vm;
        private bool _isRegisterMode = false; // Cờ để biết đang ở chế độ nào

        public TichDiemWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            txtPhone.Focus();
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

        private void HandleAction()
        {
            lblError.Visibility = Visibility.Collapsed;
            string phone = txtPhone.Text.Trim();

            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            {
                ShowError("Vui lòng nhập số điện thoại hợp lệ (ít nhất 9 số).");
                return;
            }

            // TRƯỜNG HỢP 1: ĐANG Ở CHẾ ĐỘ ĐĂNG KÝ (Người dùng bấm nút Đăng Ký)
            if (_isRegisterMode)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    ShowError("Vui lòng nhập tên của bạn để tích điểm.");
                    txtName.Focus();
                    return;
                }

                // Gọi hàm đăng ký mới
                var newKhach = _vm.RegisterCustomer(phone, name);
                ShowCustomerInfo(newKhach);
                return;
            }

            // TRƯỜNG HỢP 2: TRA CỨU SỐ ĐIỆN THOẠI
            var khach = _vm.FindCustomer(phone);

            if (khach != null)
            {
                // -> Có khách: Hiển thị thông tin
                ShowCustomerInfo(khach);
            }
            else
            {
                // -> Không có khách: Chuyển sang chế độ đăng ký
                _isRegisterMode = true;

                // Thay đổi giao diện
                pnlNameInput.Visibility = Visibility.Visible; // Hiện ô nhập tên
                btnAction.Content = "ĐĂNG KÝ HỘI VIÊN";      // Đổi tên nút
                btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Đổi màu nút sang Cam

                ShowError("Số điện thoại mới. Vui lòng nhập tên để tạo tài khoản!");
                lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Màu cam thông báo
                lblError.Visibility = Visibility.Visible;

                txtName.Focus(); // Focus vào ô nhập tên
            }
        }

        private void ShowCustomerInfo(OrMan.Models.KhachHang khach)
        {
            // Ẩn các ô nhập liệu
            txtPhone.IsEnabled = false;
            pnlNameInput.Visibility = Visibility.Collapsed;
            lblError.Visibility = Visibility.Collapsed;

            // Hiện bảng kết quả
            pnlResult.Visibility = Visibility.Visible;
            lblTenKhach.Text = khach.HoTen;
            lblHang.Text = khach.HangThanhVien;
            lblDiem.Text = $"{khach.DiemTichLuy:N0}";

            // Cập nhật khách hàng hiện tại vào ViewModel chính để dùng cho đơn hàng
            _vm.CurrentCustomer = khach;

            // Đổi nút thành HOÀN TẤT
            btnAction.Content = "HOÀN TẤT";
            btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#22C55E"); // Màu xanh lá

            // Bấm lần nữa là đóng
            btnAction.Click -= BtnAction_Click;
            btnAction.Click += (s, e) => this.Close();
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visibility = Visibility.Visible;
            lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#EF4444"); // Đỏ mặc định
        }
    }
}
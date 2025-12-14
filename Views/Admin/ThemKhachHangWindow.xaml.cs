using System.Windows;
using System.Text.RegularExpressions;
using OrMan.Models;

namespace OrMan.Views.Admin
{
    public partial class ThemKhachHangWindow : Window
    {
        public KhachHang Result { get; private set; }

        public ThemKhachHangWindow(KhachHang khachCanSua)
        {
            InitializeComponent();

            // Đổ dữ liệu cũ lên giao diện
            if (khachCanSua != null)
            {
                txtPhone.Text = khachCanSua.SoDienThoai;
                txtName.Text = khachCanSua.HoTen;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Lấy dữ liệu và cắt khoảng trắng thừa
            string sdt = txtPhone.Text.Trim();
            string ten = txtName.Text.Trim();

            // 1. Kiểm tra tên
            if (string.IsNullOrEmpty(ten))
            {
                MessageBox.Show("Vui lòng nhập tên khách hàng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            // 2. [MỚI] Kiểm tra số điện thoại (Phải là số và đúng 10 ký tự)
            // Regex: ^\d{10}$ nghĩa là bắt đầu và kết thúc đều là số, tổng cộng 10 ký tự
            if (!Regex.IsMatch(sdt, @"^\d{10}$"))
            {
                MessageBox.Show("Số điện thoại không hợp lệ!\nVui lòng nhập đúng 10 chữ số.",
                                "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhone.Focus();
                return; // Dừng lại, không cho lưu
            }

            // Nếu mọi thứ OK thì tạo kết quả
            Result = new KhachHang
            {
                SoDienThoai = sdt,
                HoTen = ten
            };

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
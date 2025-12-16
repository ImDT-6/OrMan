using System.Windows;
using System.Windows.Input; // Để dùng Key.Enter
using System.Text.RegularExpressions;
using OrMan.Models;

namespace OrMan.Views.Admin
{
    public partial class ThemKhachHangWindow : Window
    {
        public KhachHang Result { get; private set; }
        private KhachHang _originalCustomer; // Lưu khách gốc để giữ ID và Điểm

        public ThemKhachHangWindow(KhachHang khachCanSua)
        {
            InitializeComponent();
            _originalCustomer = khachCanSua;

            // Đổ dữ liệu cũ lên giao diện
            if (khachCanSua != null)
            {
                txtPhone.Text = khachCanSua.SoDienThoai;
                txtName.Text = khachCanSua.HoTen;

                // Nếu sửa thì focus vào tên (vì SĐT thường ít sửa)
                txtName.Focus();
                txtName.SelectAll();
            }
            else
            {
                // Nếu thêm mới thì focus vào SĐT
                txtPhone.Focus();
            }
        }

        // --- XỬ LÝ PHÍM ENTER CHO TIỆN DỤNG ---
        private void txtPhone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtName.Focus(); // Bấm Enter ở SĐT thì nhảy sang Tên
                e.Handled = true;
            }
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveCustomer(); // Bấm Enter ở Tên thì Lưu luôn
            }
        }

        // --- SỰ KIỆN NÚT BẤM ---
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCustomer();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // --- LOGIC LƯU ---
        private void SaveCustomer()
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

            // 2. Kiểm tra số điện thoại
            // Regex: ^0\d{9}$
            // ^0     : Bắt buộc bắt đầu bằng số 0
            // \d{9}  : Theo sau là đúng 9 chữ số nữa
            // $      : Kết thúc chuỗi
            // => Tổng cộng là 10 số và bắt đầu bằng 0
            if (!Regex.IsMatch(sdt, @"^0\d{9}$"))
            {
                MessageBox.Show("Số điện thoại không hợp lệ!\nPhải bắt đầu bằng số 0 và có đúng 10 chữ số.",
                                "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhone.Focus();
                txtPhone.SelectAll();
                return;
            }

            // 3. Tạo kết quả (Giữ nguyên ID và Điểm nếu là sửa)
            Result = new KhachHang
            {
                // Nếu _originalCustomer khác null thì lấy ID cũ, ngược lại là 0 (Thêm mới)
                KhachHangID = _originalCustomer?.KhachHangID ?? 0,

                // Giữ nguyên điểm và hạng cũ
                DiemTichLuy = _originalCustomer?.DiemTichLuy ?? 0,
                HangThanhVien = _originalCustomer?.HangThanhVien ?? "Mới",

                // Cập nhật thông tin mới
                SoDienThoai = sdt,
                HoTen = ten
            };

            this.DialogResult = true;
            this.Close();
        }
    }
}
using System;
using System.Linq;

namespace GymManagement.ViewModels
{
    public class DangNhapViewModel : ValidationBase
    {
        private string _taiKhoan;
        private string _matKhau;

        public string TaiKhoan
        {
            get => _taiKhoan;
            set
            {
                _taiKhoan = value;
                // Đã xóa dòng Validate() ở đây để không báo lỗi tức thời
                // Chỉ xóa lỗi cũ nếu người dùng bắt đầu sửa lại
                ClearErrors(nameof(TaiKhoan));
                OnPropertyChanged();
            }
        }

        public string MatKhau
        {
            get => _matKhau;
            set
            {
                _matKhau = value;
                // Đã xóa dòng Validate() ở đây
                ClearErrors(nameof(MatKhau));
                OnPropertyChanged();
            }
        }

        // Hàm này sẽ được gọi khi bấm nút Đăng Nhập
        public bool ValidateInput()
        {
            // 1. Reset lỗi cũ
            ClearErrors(nameof(TaiKhoan));
            ClearErrors(nameof(MatKhau));

            bool isValid = true;

            // 2. Kiểm tra Tài khoản
            if (string.IsNullOrWhiteSpace(TaiKhoan))
            {
                AddError(nameof(TaiKhoan), "Vui lòng nhập tên tài khoản.");
                isValid = false;
            }

            // 3. Kiểm tra Mật khẩu
            if (string.IsNullOrWhiteSpace(MatKhau))
            {
                AddError(nameof(MatKhau), "Vui lòng nhập mật khẩu.");
                isValid = false;
            }

            return isValid;
        }
    }
}
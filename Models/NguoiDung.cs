namespace GymManagement.Models
{
    // Class cha (Base class) để chứa thông tin chung
    public class BaseAccount
    {
        public string TaiKhoan { get; set; }
        public string MatKhau { get; set; }
        public string HoTen { get; set; }

        public BaseAccount(string taiKhoan, string matKhau, string hoTen)
        {
            TaiKhoan = taiKhoan;
            MatKhau = matKhau;
            HoTen = hoTen;
        }
    }

    // Role 1: Admin
    public class Admin : BaseAccount
    {
        public string ChucVu { get; set; } // Ví dụ: Quản lý, Bếp trưởng

        public Admin(string taiKhoan, string matKhau, string chucVu)
            : base(taiKhoan, matKhau, "Admin")
        {
            ChucVu = chucVu;
        }
    }

    // Role 2: User (Đúng yêu cầu của bạn)
    public class User : BaseAccount
    {
        public string HangThanhVien { get; set; } // Gold, Silver, Diamond
        public int DiemTichLuy { get; set; }

        public User(string taiKhoan, string matKhau, string hoTen, string hangTv, int diem)
            : base(taiKhoan, matKhau, hoTen)
        {
            HangThanhVien = hangTv;
            DiemTichLuy = diem;
        }
    }
}
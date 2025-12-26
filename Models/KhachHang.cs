using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        public int KhachHangID { get; set; }

        [Required]
        [StringLength(20)]
        public string SoDienThoai { get; set; }

        [StringLength(100)]
        public string HoTen { get; set; }

        public int DiemTichLuy { get; set; } = 0;

        [StringLength(20)]
        public string HangThanhVien { get; set; } = "Khách Hàng Mới";

        // [MỚI] Thêm thuộc tính này để theo dõi ngày đăng ký
        // Dùng để hiển thị lên DataGrid và tính toán số lượng thành viên mới
        public DateTime NgayThamGia { get; set; } = DateTime.Now;

        public void CapNhatHang()
        {
            if (DiemTichLuy >= 5000) HangThanhVien = "Kim Cương";
            else if (DiemTichLuy >= 1000) HangThanhVien = "Vàng";
            else HangThanhVien = "Khách Hàng Mới";
        }
    }
}
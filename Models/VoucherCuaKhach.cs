using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    [Table("VoucherCuaKhach")]
    public class VoucherCuaKhach
    {
        [Key]
        public int Id { get; set; }

        public int KhachHangID { get; set; } // Link tới khách hàng

        [Required]
        [StringLength(100)]
        public string TenPhanThuong { get; set; } // Ví dụ: "Voucher 50k", "Free Pepsi"

        public DateTime NgayTrungThuong { get; set; } = DateTime.Now;

        public bool DaSuDung { get; set; } = false; // Mặc định là chưa dùng

        public DateTime? NgaySuDung { get; set; } // Khi nào dùng thì update ngày này

        // Loại phần thưởng: 1=Giảm tiền trực tiếp, 2=Giảm %, 3=Tặng món
        public int LoaiVoucher { get; set; }

        public double GiaTri { get; set; } // Ví dụ: 50000 (nếu giảm tiền), 10 (nếu giảm %), 0 (nếu tặng món)
    }
}
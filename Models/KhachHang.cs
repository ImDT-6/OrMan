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
        public string SoDienThoai { get; set; } // Đây là key để tìm kiếm

        [StringLength(100)]
        public string HoTen { get; set; } // Tên khách (có thể để trống ban đầu)

        public int DiemTichLuy { get; set; } = 0;

        [StringLength(20)]
        public string HangThanhVien { get; set; } = "Khách Hàng Mới"; // Bạc, Vàng, Kim Cương
    }
}
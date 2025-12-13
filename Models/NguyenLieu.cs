using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    [Table("NguyenLieu")]
    public class NguyenLieu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TenNguyenLieu { get; set; } // Ví dụ: Thịt bò, Mì, Kim chi

        public string DonViTinh { get; set; } // Ví dụ: kg, g, vắt, ml

        public double SoLuongTon { get; set; } // Tồn kho hiện tại

        public decimal GiaVon { get; set; } // Giá nhập vào (để tính lãi lỗ)

        public double DinhMucToiThieu { get; set; } // Dưới mức này thì cảnh báo nhập hàng
    }
}
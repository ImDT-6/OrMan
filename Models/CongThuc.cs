using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    [Table("CongThuc")]
    public class CongThuc
    {
        [Key]
        public int Id { get; set; }

        public string MaMon { get; set; } // Link tới MonAn
        public int NguyenLieuId { get; set; } // Link tới NguyenLieu

        public double SoLuongCan { get; set; } // Cần bao nhiêu cho 1 món?

        [ForeignKey("MaMon")]
        public virtual MonAn MonAn { get; set; }

        [ForeignKey("NguyenLieuId")]
        public virtual NguyenLieu NguyenLieu { get; set; }
    }
}
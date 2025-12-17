using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    [Table("DanhGia")]
    public class DanhGia
    {
        [Key]
        public int Id { get; set; }

        public int SoSao { get; set; } // 1 đến 5
        public string CacTag { get; set; } // Lưu dạng chuỗi: "Món ngon,Phục vụ tốt"
        public string NoiDung { get; set; }
        public string SoDienThoai { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace OrMan.Models
{
    public class HoaDon
    {
        [Key]
        public string MaHoaDon { get; set; }
        public DateTime NgayTao { get; set; }
        public decimal TongTien { get; set; }

        // Thuộc tính giảm giá
        public decimal GiamGia { get; set; } = 0;
        public decimal ThanhTien => TongTien - GiamGia;

        public string NguoiTao { get; set; }
        public int SoBan { get; set; }
        public bool DaThanhToan { get; set; }

        public HoaDon() { }

        // [SỬA LẠI DÒNG NÀY]: Thêm ", decimal giamGia = 0" vào cuối
        public HoaDon(string ma, decimal tien, string user, int ban, decimal giamGia = 0)
        {
            MaHoaDon = ma;
            NgayTao = DateTime.Now;
            TongTien = tien;
            NguoiTao = user;
            SoBan = ban;
            DaThanhToan = false;

            // Bây giờ dòng này mới hoạt động vì đã có biến 'giamGia' ở trên truyền xuống
            GiamGia = giamGia;
        }
    }
}
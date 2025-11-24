using System;
using System.ComponentModel.DataAnnotations;

namespace GymManagement.Models
{
    public class HoaDon
    {
        [Key]
        public string MaHoaDon { get; set; }
        public DateTime NgayTao { get; set; }
        public decimal TongTien { get; set; }
        public string NguoiTao { get; set; }
        public int SoBan { get; set; }
        public bool DaThanhToan { get; set; } // [MỚI] True = Đã tính tiền, False = Khách đang ăn

        protected HoaDon() { }

        public HoaDon(string ma, decimal tien, string user, int ban)
        {
            MaHoaDon = ma;
            NgayTao = DateTime.Now;
            TongTien = tien;
            NguoiTao = user;
            SoBan = ban;
            DaThanhToan = false; // Mặc định là chưa thanh toán
        }
    }
}
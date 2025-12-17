using System;
using System.ComponentModel.DataAnnotations.Schema;

using System; // [MỚI] Cần cái này để dùng DateTime
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    public class ChiTietHoaDon
    {
        [Key]
        public int Id { get; set; }
        public string MaHoaDon { get; set; }
        public string MaMon { get; set; }
        public string TenMonHienThi { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public int CapDoCay { get; set; }
        public string GhiChu { get; set; }

        // 0: Chờ chế biến, 1: Đã xong
        public int TrangThaiCheBien { get; set; }

        // [MỚI - SỬA LỖI] Thêm thuộc tính này để fix lỗi bên BepViewModel
        // Dùng DateTime? (nullable) để tránh lỗi với dữ liệu cũ chưa có giờ
        public DateTime? ThoiGianGoiMon { get; set; }

        [ForeignKey("MaMon")]
        public virtual MonAn MonAn { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;

        public ChiTietHoaDon() { }

        public ChiTietHoaDon(string maHD, MonAn mon, int sl, int capDo, string ghiChu)
        {
            MaHoaDon = maHD;
            MaMon = mon.MaMon;
            TenMonHienThi = mon is MonMiCay ? $"{mon.TenMon} (Cấp {capDo})" : mon.TenMon;
            SoLuong = sl;
            DonGia = mon.GiaBan;
            CapDoCay = capDo;
            GhiChu = ghiChu;

            // [MỚI] Tự động gán giờ hiện tại khi tạo món mới
            ThoiGianGoiMon = DateTime.Now;
        }
    }
}
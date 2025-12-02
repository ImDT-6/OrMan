using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    public class ChiTietHoaDon
    {
        [Key]
        public int Id { get; set; } // ID tự tăng
        public string MaHoaDon { get; set; } // Khóa ngoại tới HoaDon
        public string MaMon { get; set; }    // Khóa ngoại tới MonAn
        public string TenMonHienThi { get; set; } // Lưu cứng tên món tại thời điểm bán (đề phòng sau này đổi tên)
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }  // Lưu cứng giá tại thời điểm bán
        public int CapDoCay { get; set; }    // Cho Mì Cay
        public string GhiChu { get; set; }

        public decimal ThanhTien => SoLuong * DonGia;

        // [SỬA LỖI] Đổi từ protected sang public để có thể khởi tạo đối tượng 
        // trong các lớp khác (ví dụ: QuanLyBanViewModel)
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
        }
    }
}
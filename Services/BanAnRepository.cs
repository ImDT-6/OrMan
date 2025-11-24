using System.Collections.ObjectModel;
using System.Linq;
using GymManagement.Data;
using GymManagement.Models;
using Microsoft.EntityFrameworkCore; // Cần cho Include/Where
using System.Collections.Generic;

namespace GymManagement.Services
{
    public class BanAnRepository
    {
        // [QUAN TRỌNG] Chỉ khai báo biến _context MỘT LẦN duy nhất ở đây
        private readonly MenuContext _context;

        public BanAnRepository()
        {
            _context = new MenuContext();
            _context.Database.EnsureCreated();
        }

        // 1. Lấy danh sách tất cả bàn
        public ObservableCollection<BanAn> GetAll()
        {
            var list = _context.BanAns.OrderBy(b => b.SoBan).ToList();
            return new ObservableCollection<BanAn>(list);
        }

        // 2. Khởi tạo bàn mẫu (nếu chưa có)
        public void InitTables()
        {
            if (!_context.BanAns.Any())
            {
                for (int i = 1; i <= 20; i++)
                {
                    _context.BanAns.Add(new BanAn(i, "Trống"));
                }
                _context.SaveChanges();
            }
        }

        // 3. Cập nhật trạng thái bàn
        public void UpdateStatus(BanAn ban, string status)
        {
            var item = _context.BanAns.Find(ban.SoBan);
            if (item != null)
            {
                item.TrangThai = status;
                _context.SaveChanges();
            }
        }

        // 4. Lấy hóa đơn CHƯA THANH TOÁN của một bàn cụ thể (Cho Admin xem)
        public HoaDon GetActiveOrder(int soBan)
        {
            // Tìm hóa đơn mới nhất của bàn này mà chưa thanh toán
            return _context.HoaDons
                           .Where(h => h.SoBan == soBan && !h.DaThanhToan)
                           .OrderByDescending(h => h.NgayTao)
                           .FirstOrDefault();
        }

        // 5. Lấy chi tiết món ăn của hóa đơn đó
        public List<ChiTietHoaDon> GetOrderDetails(string maHoaDon)
        {
            return _context.ChiTietHoaDons
                           .Where(ct => ct.MaHoaDon == maHoaDon)
                           .ToList();
        }

        // 6. Thanh toán hóa đơn & Trả bàn
        public void CheckoutTable(int soBan, string maHoaDon)
        {
            // Đánh dấu hóa đơn đã thanh toán
            var hd = _context.HoaDons.Find(maHoaDon);
            if (hd != null) hd.DaThanhToan = true;

            // Trả bàn về trạng thái Trống
            var ban = _context.BanAns.Find(soBan);
            if (ban != null) ban.TrangThai = "Trống";

            _context.SaveChanges();
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using GymManagement.Data;
using GymManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace GymManagement.Services
{
    public class BanAnRepository
    {
        private readonly MenuContext _context;

        public BanAnRepository()
        {
            _context = new MenuContext();
            _context.Database.EnsureCreated();
        }

        // [SỬA QUAN TRỌNG] Dùng context mới mỗi khi gọi để đảm bảo dữ liệu Real-time chính xác nhất
        public ObservableCollection<BanAn> GetAll()
        {
            using (var freshContext = new MenuContext())
            {
                var list = freshContext.BanAns.OrderBy(b => b.SoBan).ToList();
                return new ObservableCollection<BanAn>(list);
            }
        }

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

        public void UpdateStatus(int soBan, string status)
        {
            var item = _context.BanAns.Find(soBan);
            if (item != null)
            {
                item.TrangThai = status;
                _context.SaveChanges();
            }
        }

        public void RequestPayment(int soBan)
        {
            // Dùng context mới để đảm bảo User update được ngay dù Admin đang mở
            using (var context = new MenuContext())
            {
                var item = context.BanAns.Find(soBan);
                if (item != null)
                {
                    item.YeuCauThanhToan = true;
                    context.SaveChanges();
                }
            }
        }

        public HoaDon GetActiveOrder(int soBan)
        {
            return _context.HoaDons
                           .Where(h => h.SoBan == soBan && !h.DaThanhToan)
                           .OrderByDescending(h => h.NgayTao)
                           .FirstOrDefault();
        }

        public List<ChiTietHoaDon> GetOrderDetails(string maHoaDon)
        {
            return _context.ChiTietHoaDons
                           .Where(ct => ct.MaHoaDon == maHoaDon)
                           .ToList();
        }
        public static event Action OnPaymentSuccess;

        public void CheckoutTable(int soBan, string maHoaDon)
        {
            using (var context = new MenuContext())
            {
                var hd = context.HoaDons.Find(maHoaDon);
                if (hd != null)
                {
                    hd.DaThanhToan = true; // Đánh dấu đã trả tiền -> Lúc này mới tính doanh thu
                }

                var ban = context.BanAns.Find(soBan);
                if (ban != null)
                {
                    ban.TrangThai = "Trống";
                    ban.YeuCauThanhToan = false;
                }
                context.SaveChanges();

                // [MỚI] Bắn tín hiệu cập nhật cho toàn bộ hệ thống
                OnPaymentSuccess?.Invoke();
            }
        }

        public void ResolvePaymentRequest(int soBan)
        {
            using (var context = new MenuContext())
            {
                var ban = context.BanAns.Find(soBan);
                if (ban != null)
                {
                    ban.YeuCauThanhToan = false; // Tắt yêu cầu
                    context.SaveChanges();
                }
            }
        }
    }
}
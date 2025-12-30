using System.Collections.ObjectModel;
using System.Linq;
using OrMan.Data;
using OrMan.Models;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using OrMan.Helpers;

namespace OrMan.Services
{
    public class ThucDonRepository
    {
        private readonly MenuContext _context;

        public ThucDonRepository()
        {
            _context = new MenuContext();
            _context.Database.EnsureCreated();
        }

        // ... (Giữ nguyên các hàm GetAll, Add, Update, ToggleSoldOut cũ) ...

        public ObservableCollection<MonAn> GetAll()
        {
            var list = _context.MonAns.ToList();
            return new ObservableCollection<MonAn>(list);
        }

        public ObservableCollection<MonAn> GetAvailableMenu()
        {
            using (var freshContext = new MenuContext())
            {
                var list = freshContext.MonAns.ToList();
                return new ObservableCollection<MonAn>(list);
            }
        }

        public void Add(MonAn monAn) { _context.MonAns.Add(monAn); _context.SaveChanges(); }

        public void Delete(MonAn monAn)
        {
            var monCanXoa = _context.MonAns.Find(monAn.MaMon);
            if (monCanXoa != null)
            {
                _context.MonAns.Remove(monCanXoa);
                _context.SaveChanges();
            }
        }

        public void Update(MonAn monAn)
        {
            _context.Entry(monAn).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void ToggleSoldOut(string maMon)
        {
            using (var context = new MenuContext())
            {
                var mon = context.MonAns.Find(maMon);
                if (mon != null)
                {
                    mon.IsSoldOut = !mon.IsSoldOut;
                    context.SaveChanges();
                }
            }
        }

        // [QUAN TRỌNG] Đã thêm tham số 'decimal tienGiamGia = 0' vào cuối dòng này
        // File: Services/ThucDonRepository.cs

        public void CreateOrder(int soBan, decimal tongTien, string ghiChu, IEnumerable<CartItem> gioHang, decimal tienGiamGia = 0)
        {
            using (var context = new MenuContext())
            {
                // 1. Tìm hóa đơn chưa thanh toán
                var hoaDon = context.HoaDons.FirstOrDefault(h => h.SoBan == soBan && !h.DaThanhToan);
                string maHD;

                if (hoaDon == null)
                {
                    // Use shared helper to generate order id consistently
                    maHD = OrderHelper.GenerateOrderId();
                    hoaDon = new HoaDon(maHD, tongTien, "Khách tại bàn", soBan, tienGiamGia);
                    context.HoaDons.Add(hoaDon);
                }
                else
                {
                    maHD = hoaDon.MaHoaDon;
                    hoaDon.TongTien += tongTien;
                    hoaDon.GiamGia += tienGiamGia;
                }

                // 2. Thêm món vào chi tiết
                foreach (var item in gioHang)
                {
                    var chiTiet = new ChiTietHoaDon(maHD, item.MonAn, item.SoLuong, item.CapDoCay, item.GhiChu);

                    // [QUAN TRỌNG] Gán thời gian gọi món để Bếp sắp xếp thứ tự
                    chiTiet.ThoiGianGoiMon = DateTime.Now;

                    context.ChiTietHoaDons.Add(chiTiet);
                }

                // --- [SỬA LỖI TẠI ĐÂY] ---
                // 3. Cập nhật trạng thái Bàn -> "Có Khách"
                var banAn = context.BanAns.Find(soBan);
                if (banAn != null)
                {
                    banAn.TrangThai = "Có Khách"; // Dòng này giúp bên Admin đổi màu bàn
                }
                // -------------------------

                context.SaveChanges();
            }
        }
        public Dictionary<MonAn, int> GetTopSellingFoods(int topCount)
        {
            using (var context = new MenuContext())
            {
                var topList = context.ChiTietHoaDons
                                     .AsEnumerable()
                                     .GroupBy(ct => ct.MaMon)
                                     .Select(g => new { MaMon = g.Key, TongSoLuong = g.Sum(x => x.SoLuong) })
                                     .OrderByDescending(x => x.TongSoLuong)
                                     .Take(topCount)
                                     .ToList();

                var result = new Dictionary<MonAn, int>();
                foreach (var item in topList)
                {
                    var monAn = context.MonAns.Find(item.MaMon);
                    if (monAn != null)
                    {
                        result.Add(monAn, item.TongSoLuong);
                    }
                }
                return result;
            }
        }
    }
}
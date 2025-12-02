using System.Collections.ObjectModel;
using System.Linq;
using OrMan.Data;
using OrMan.Models;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore; // Cần thêm dòng này

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

        // [SỬA LỖI QUAN TRỌNG] Triển khai hàm Delete
        public void Delete(MonAn monAn)
        {
            // Tìm món ăn trong Context và xóa
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

        public void ToggleSoldOut(string maMon) { /*...*/ } // Giữ nguyên code cũ

        public void CreateOrder(int soBan, decimal tongTien, string ghiChu, IEnumerable<CartItem> gioHang)
        {
            // ... (Logic CreateOrder giữ nguyên)
            string maHD = "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var hoaDon = new HoaDon(maHD, tongTien, "Khách tại bàn", soBan);
            _context.HoaDons.Add(hoaDon);

            foreach (var item in gioHang)
            {
                var chiTiet = new ChiTietHoaDon(maHD, item.MonAn, item.SoLuong, item.CapDoCay, item.GhiChu);
                _context.ChiTietHoaDons.Add(chiTiet);
            }

            var ban = _context.BanAns.Find(soBan);
            if (ban == null) { ban = new BanAn(soBan, "Có Khách"); _context.BanAns.Add(ban); }
            else { ban.TrangThai = "Có Khách"; ban.YeuCauThanhToan = false; }
            _context.SaveChanges();
        }

        public Dictionary<MonAn, int> GetTopSellingFoods(int topCount)
        {
            using (var context = new MenuContext())
            {
                // 1. Group theo Mã Món và tính tổng số lượng
                var topList = context.ChiTietHoaDons
                                     .AsEnumerable()
                                     .GroupBy(ct => ct.MaMon)
                                     .Select(g => new { MaMon = g.Key, TongSoLuong = g.Sum(x => x.SoLuong) })
                                     .OrderByDescending(x => x.TongSoLuong)
                                     .Take(topCount)
                                     .ToList();

                // 2. Lấy thông tin chi tiết món ăn
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
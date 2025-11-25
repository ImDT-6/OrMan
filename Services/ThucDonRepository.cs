using System.Collections.ObjectModel;
using System.Linq;
using GymManagement.Data;
using GymManagement.Models;
using System.Collections.Generic;
using System;

namespace GymManagement.Services
{
    public class ThucDonRepository
    {
        private readonly MenuContext _context;

        public ThucDonRepository()
        {
            _context = new MenuContext();
            _context.Database.EnsureCreated();
        }

        // ... (Giữ nguyên các hàm GetAll, Add, Delete, Update, ToggleSoldOut cũ) ...

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
        public void Delete(MonAn monAn) { /*...*/ } // Giữ nguyên code cũ
        public void Update(MonAn monAn) { /*...*/ } // Giữ nguyên code cũ
        public void ToggleSoldOut(string maMon) { /*...*/ } // Giữ nguyên code cũ
        public void CreateOrder(int soBan, decimal tongTien, string ghiChu, IEnumerable<CartItem> gioHang)
        {
            // ... (Giữ nguyên code tạo hóa đơn cũ)
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

        // [MỚI] Lấy Top món bán chạy nhất dựa trên số lượng bán trong ChiTietHoaDon
        // Trả về Dictionary: Key = MonAn, Value = Số lượng đã bán
        public Dictionary<MonAn, int> GetTopSellingFoods(int topCount)
        {
            using (var context = new MenuContext())
            {
                // 1. Group theo Mã Món và tính tổng số lượng
                var topList = context.ChiTietHoaDons
                                     .AsEnumerable() // Chuyển về xử lý client nếu EF Core gặp khó với GroupBy phức tạp
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
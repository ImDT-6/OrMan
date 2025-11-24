using System.Collections.ObjectModel;
using System.Linq;
using GymManagement.Data; // [QUAN TRỌNG] Đã thêm using Data
using GymManagement.Models;
using Microsoft.EntityFrameworkCore;
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

        public ObservableCollection<MonAn> GetAll()
        {
            var list = _context.MonAns.ToList();
            return new ObservableCollection<MonAn>(list);
        }

        public void Add(MonAn monAn)
        {
            _context.MonAns.Add(monAn);
            _context.SaveChanges();
        }

        public void Delete(MonAn monAn)
        {
            var item = _context.MonAns.Find(monAn.MaMon);
            if (item != null)
            {
                _context.MonAns.Remove(item);
                _context.SaveChanges();
            }
        }

        public void Update(MonAn monAn)
        {
            var item = _context.MonAns.Find(monAn.MaMon);
            if (item != null)
            {
                item.TenMon = monAn.TenMon;
                item.GiaBan = monAn.GiaBan;
                item.DonViTinh = monAn.DonViTinh;
                item.HinhAnhUrl = monAn.HinhAnhUrl;
                _context.SaveChanges();
            }
        }

        public void CreateOrder(int soBan, decimal tongTien, string ghiChu, IEnumerable<CartItem> gioHang)
        {
            string maHD = "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var hoaDon = new HoaDon(maHD, tongTien, "Khách tại bàn", soBan);
            _context.HoaDons.Add(hoaDon);

            foreach (var item in gioHang)
            {
                var chiTiet = new ChiTietHoaDon(maHD, item.MonAn, item.SoLuong, item.CapDoCay, item.GhiChu);
                _context.ChiTietHoaDons.Add(chiTiet);
            }

            var ban = _context.BanAns.Find(soBan);
            if (ban == null)
            {
                ban = new BanAn(soBan, "Có Khách");
                _context.BanAns.Add(ban);
            }
            else
            {
                ban.TrangThai = "Có Khách";
            }

            _context.SaveChanges();
        }
    }
}
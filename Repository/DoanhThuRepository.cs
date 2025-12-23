using Microsoft.EntityFrameworkCore;
using OrMan.Data;
using OrMan.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OrMan.Services
{
    public class DoanhThuRepository
    {
        public ObservableCollection<HoaDon> GetAll()
        {
            using (var context = new MenuContext())
            {
                var list = context.HoaDons
                                  .Where(h => h.DaThanhToan == true)
                                  .OrderByDescending(h => h.NgayTao)
                                  .ToList();
                return new ObservableCollection<HoaDon>(list);
            }
        }

        public decimal GetTodayRevenue()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                // [FIX] Doanh thu thực = Tổng tiền - Giảm giá
                return context.HoaDons
                              .Where(h => h.NgayTao >= today && h.DaThanhToan == true)
                              .Sum(h => h.TongTien - h.GiamGia);
            }
        }

        public decimal GetYesterdayRevenue()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);

                // [FIX] Doanh thu thực = Tổng tiền - Giảm giá
                return context.HoaDons
                              .Where(h => h.NgayTao >= yesterday && h.NgayTao < today && h.DaThanhToan == true)
                              .Sum(h => h.TongTien - h.GiamGia);
            }
        }

        public int GetTodayOrderCount()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                return context.HoaDons.Count(h => h.NgayTao >= today && h.DaThanhToan == true);
            }
        }

        public int GetYesterdayOrderCount()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);

                return context.HoaDons
                    .Count(h => h.DaThanhToan && h.NgayTao >= yesterday && h.NgayTao < today);
            }
        }

        public Dictionary<int, decimal> GetRevenueByYear()
        {
            using (var context = new MenuContext())
            {
                int currentYear = DateTime.Now.Year;

                // [FIX] Tính thực thu ngay lúc Select
                var query = context.HoaDons
                    .Where(h => h.DaThanhToan && h.NgayTao.Year == currentYear)
                    .Select(h => new { h.NgayTao.Month, ThucThu = h.TongTien - h.GiamGia })
                    .ToList();

                return query.GroupBy(x => x.Month)
                            .ToDictionary(g => g.Key, g => g.Sum(x => x.ThucThu));
            }
        }

        public Dictionary<int, decimal> GetRevenueByHour()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // [FIX] Tính thực thu
                var rawData = context.HoaDons
                    .Where(h => h.NgayTao >= today && h.NgayTao < tomorrow && h.DaThanhToan == true)
                    .Select(h => new { h.NgayTao, ThucThu = h.TongTien - h.GiamGia })
                    .ToList();

                return rawData.GroupBy(h => h.NgayTao.Hour)
                              .ToDictionary(g => g.Key, g => g.Sum(h => h.ThucThu));
            }
        }

        public Dictionary<int, decimal> GetRevenueByWeek()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;

                // Tính ngày Thứ 2 đầu tuần hiện tại
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                var startOfWeek = today.AddDays(-1 * diff).Date;
                var endOfWeek = startOfWeek.AddDays(7);

                // [FIX] Tính thực thu
                var rawData = context.HoaDons
                    .Where(h => h.NgayTao >= startOfWeek && h.NgayTao < endOfWeek && h.DaThanhToan == true)
                    .Select(h => new { h.NgayTao, ThucThu = h.TongTien - h.GiamGia })
                    .ToList();

                // Group theo thứ (Convert DayOfWeek sang kiểu VN: T2->2, CN->8)
                return rawData.GroupBy(h => h.NgayTao.DayOfWeek)
                              .ToDictionary(
                                  g => g.Key == DayOfWeek.Sunday ? 8 : (int)g.Key + 1,
                                  g => g.Sum(h => h.ThucThu)
                              );
            }
        }
        public List<ChiTietHoaDon> GetChiTietHoaDon(string maHD)
        {
            using (var context = new MenuContext())
            {
                // Lấy danh sách món ăn trong hóa đơn đó
                // Include(x => x.MonAn) để lấy được tên món ăn
                return context.ChiTietHoaDons
                              .Include(x => x.MonAn)
                              .Where(x => x.MaHoaDon == maHD)
                              .ToList();
            }
        }
        public Dictionary<int, decimal> GetRevenueByMonth()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var startOfNextMonth = startOfMonth.AddMonths(1);

                // [FIX] Tính thực thu
                var rawData = context.HoaDons
                    .Where(h => h.NgayTao >= startOfMonth && h.NgayTao < startOfNextMonth && h.DaThanhToan == true)
                    .Select(h => new { h.NgayTao, ThucThu = h.TongTien - h.GiamGia })
                    .ToList();

                return rawData.GroupBy(h => h.NgayTao.Day)
                              .ToDictionary(g => g.Key, g => g.Sum(h => h.ThucThu));
            }
        }
    }
}
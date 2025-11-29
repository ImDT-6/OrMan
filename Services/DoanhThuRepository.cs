using System.Collections.ObjectModel;
using System.Linq;
using GymManagement.Data;
using GymManagement.Models;
using System;
using System.Collections.Generic;

namespace GymManagement.Services
{
    public class DoanhThuRepository
    {
        private readonly MenuContext _context;

        public DoanhThuRepository()
        {
            _context = new MenuContext();
        }

        public ObservableCollection<HoaDon> GetAll()
        {
            using (var context = new MenuContext())
            {
                var list = context.HoaDons.OrderByDescending(h => h.NgayTao).ToList();
                return new ObservableCollection<HoaDon>(list);
            }
        }

        public decimal GetTodayRevenue()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                return context.HoaDons
                               .Where(h => h.NgayTao >= today)
                               .Sum(h => h.TongTien);
            }
        }

        public decimal GetYesterdayRevenue()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);
                return context.HoaDons
                               .Where(h => h.NgayTao >= yesterday && h.NgayTao < today)
                               .Sum(h => h.TongTien);
            }
        }

        public int GetTodayOrderCount()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                return context.HoaDons.Count(h => h.NgayTao >= today);
            }
        }

        // [MỚI] Hàm lấy doanh thu theo giờ trong ngày hôm nay
        public Dictionary<int, decimal> GetRevenueByHour()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Lấy dữ liệu thô về trước (để tránh lỗi GroupBy của EF Core 3.1 với DatePart)
                var rawData = context.HoaDons
                    .Where(h => h.NgayTao >= today && h.NgayTao < tomorrow)
                    .Select(h => new { h.NgayTao, h.TongTien })
                    .ToList();

                // Group by ở phía Client (RAM)
                var result = rawData
                    .GroupBy(h => h.NgayTao.Hour)
                    .ToDictionary(g => g.Key, g => g.Sum(h => h.TongTien));

                return result;
            }
        }
    }
}
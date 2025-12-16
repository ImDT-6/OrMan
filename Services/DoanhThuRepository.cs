using OrMan.Data;
using OrMan.Models;
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
                // [FIX] Chỉ lấy các hóa đơn ĐÃ THANH TOÁN
                var list = context.HoaDons
                                  .Where(h => h.DaThanhToan == true)
                                  .OrderByDescending(h => h.NgayTao).ToList();
                return new ObservableCollection<HoaDon>(list);
            }
        }

        public decimal GetTodayRevenue()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                // [FIX] Thêm điều kiện h.DaThanhToan
                return context.HoaDons
                              .Where(h => h.NgayTao >= today && h.DaThanhToan == true)
                              .Sum(h => h.TongTien);
            }
        }

        public decimal GetYesterdayRevenue()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);
                // [FIX] Thêm điều kiện h.DaThanhToan
                return context.HoaDons
                              .Where(h => h.NgayTao >= yesterday && h.NgayTao < today && h.DaThanhToan == true)
                              .Sum(h => h.TongTien);
            }
        }

        public int GetTodayOrderCount()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                // [FIX] Chỉ đếm đơn đã thanh toán
                return context.HoaDons.Count(h => h.NgayTao >= today && h.DaThanhToan == true);
            }
        }

        public Dictionary<int, decimal> GetRevenueByHour()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // [FIX] Chỉ lấy đơn đã thanh toán để vẽ biểu đồ
                var rawData = context.HoaDons
                    .Where(h => h.NgayTao >= today && h.NgayTao < tomorrow && h.DaThanhToan == true)
                    .Select(h => new { h.NgayTao, h.TongTien })
                    .ToList();

                return rawData.GroupBy(h => h.NgayTao.Hour)
                              .ToDictionary(g => g.Key, g => g.Sum(h => h.TongTien));
            }
        }

        // ... (Các hàm cũ giữ nguyên) ...

        /// <summary>
        /// Lấy doanh thu Tuần này (Từ Thứ 2 -> Chủ Nhật)
        /// Trả về Dictionary với Key: 2=Thứ 2, ..., 7=Thứ 7, 8=Chủ Nhật
        /// </summary>
        public Dictionary<int, decimal> GetRevenueByWeek()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;

                // Tính ngày Thứ 2 đầu tuần hiện tại
                // (DayOfWeek của C#: Sunday=0, Monday=1, ... Saturday=6)
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                var startOfWeek = today.AddDays(-1 * diff).Date;
                var endOfWeek = startOfWeek.AddDays(7); // Đầu thứ 2 tuần sau

                var rawData = context.HoaDons
                    .Where(h => h.NgayTao >= startOfWeek && h.NgayTao < endOfWeek && h.DaThanhToan == true)
                    .Select(h => new { h.NgayTao, h.TongTien })
                    .ToList();

                // Group theo thứ (Cần convert DayOfWeek sang kiểu VN: T2->2, CN->8)
                return rawData.GroupBy(h => h.NgayTao.DayOfWeek)
                              .ToDictionary(
                                  g => g.Key == DayOfWeek.Sunday ? 8 : (int)g.Key + 1,
                                  g => g.Sum(h => h.TongTien)
                              );
            }
        }

        /// <summary>
        /// Lấy doanh thu Tháng này (Từ ngày 1 -> Cuối tháng)
        /// Trả về Dictionary với Key: Ngày trong tháng (1, 2, ..., 31)
        /// </summary>
        public Dictionary<int, decimal> GetRevenueByMonth()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var startOfNextMonth = startOfMonth.AddMonths(1);

                var rawData = context.HoaDons
                    .Where(h => h.NgayTao >= startOfMonth && h.NgayTao < startOfNextMonth && h.DaThanhToan == true)
                    .Select(h => new { h.NgayTao, h.TongTien })
                    .ToList();

                // Group theo ngày trong tháng (1 -> 31)
                return rawData.GroupBy(h => h.NgayTao.Day)
                              .ToDictionary(g => g.Key, g => g.Sum(h => h.TongTien));
            }
        }
    }
}
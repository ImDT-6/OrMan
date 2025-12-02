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
    }
}
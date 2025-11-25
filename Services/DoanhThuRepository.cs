using System.Collections.ObjectModel;
using System.Linq;
using GymManagement.Data;
using GymManagement.Models;
using System;

namespace GymManagement.Services
{
    public class DoanhThuRepository
    {
        private readonly MenuContext _context;

        public DoanhThuRepository()
        {
            _context = new MenuContext();
            // Nếu chưa có dữ liệu mẫu thì thêm vào để test
            
        }

        public ObservableCollection<HoaDon> GetAll()
        {
            using (var context = new MenuContext())
            {
                var list = context.HoaDons.OrderByDescending(h => h.NgayTao).ToList();
                return new ObservableCollection<HoaDon>(list);
            }
        }

        // Tính tổng tiền hôm nay
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

        // [MỚI] Tính tổng tiền HÔM QUA (để so sánh)
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

        // [MỚI] Đếm số đơn hôm nay
        public int GetTodayOrderCount()
        {
            using (var context = new MenuContext())
            {
                var today = DateTime.Today;
                return context.HoaDons.Count(h => h.NgayTao >= today);
            }
        }
    }
}
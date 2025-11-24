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
            // Thêm dữ liệu mẫu nếu chưa có
            if (!_context.HoaDons.Any())
            {
                _context.HoaDons.Add(new HoaDon("HD001", 150000, "Admin", 1));
                _context.HoaDons.Add(new HoaDon("HD002", 320000, "Admin", 5));
                _context.SaveChanges();
            }
        }

        public ObservableCollection<HoaDon> GetAll()
        {
            var list = _context.HoaDons.OrderByDescending(h => h.NgayTao).ToList();
            return new ObservableCollection<HoaDon>(list);
        }

        // Tính tổng tiền hôm nay
        public decimal GetTodayRevenue()
        {
            var today = DateTime.Today;
            return _context.HoaDons
                           .Where(h => h.NgayTao >= today)
                           .Sum(h => h.TongTien);
        }
    }
}
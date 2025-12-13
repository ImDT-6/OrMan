using System;
using System.Collections.ObjectModel;
using System.Linq;
using OrMan.Data;
using OrMan.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace OrMan.Services
{
    public class BanAnRepository
    {
        private readonly MenuContext _context;

        public BanAnRepository()
        {
            _context = new MenuContext();
            _context.Database.EnsureCreated();
        }

        public ObservableCollection<BanAn> GetAll()
        {
            using (var freshContext = new MenuContext())
            {
                var list = freshContext.BanAns.OrderBy(b => b.SoBan).ToList();
                return new ObservableCollection<BanAn>(list);
            }
        }

        public void InitTables()
        {
            if (!_context.BanAns.Any())
            {
                for (int i = 1; i <= 20; i++)
                {
                    _context.BanAns.Add(new BanAn(i, "Trống"));
                }
                _context.SaveChanges();
            }
        }

        // [MỚI] Thêm bàn mới
        public void AddTable()
        {
            using (var context = new MenuContext())
            {
                int maxSoBan = context.BanAns.Any() ? context.BanAns.Max(b => b.SoBan) : 0;
                // Constructor của BanAn nhận (SoBan, TrangThai)
                var newBan = new BanAn(maxSoBan + 1, "Trống");
                context.BanAns.Add(newBan);
                context.SaveChanges();
            }
        }

        // [MỚI] Xóa bàn
        public bool DeleteTable(int soBan)
        {
            using (var context = new MenuContext())
            {
                var ban = context.BanAns.Find(soBan);
                // Chỉ xóa được nếu bàn Trống và không nợ tiền
                if (ban != null && ban.TrangThai == "Trống" && ban.YeuCauThanhToan == false)
                {
                    context.BanAns.Remove(ban);
                    context.SaveChanges();
                    return true;
                }
                return false;
            }
        }

        public void UpdateStatus(int soBan, string status)
        {
            using (var context = new MenuContext())
            {
                var item = context.BanAns.Find(soBan);
                if (item != null)
                {
                    item.TrangThai = status;
                    context.SaveChanges();
                }
            }
        }

        // File: BanAnRepository.cs
        // Sửa hàm RequestPayment cũ thành:
        public void RequestPayment(int soBan, string method = "Tiền mặt")
        {
            using (var context = new MenuContext())
            {
                var item = context.BanAns.Find(soBan);
                if (item != null)
                {
                    item.YeuCauThanhToan = true;
                    item.HinhThucThanhToan = method; // [MỚI] Lưu phương thức
                    item.YeuCauHoTro = null;
                    context.SaveChanges();
                }
            }
        }

        public void SendSupportRequest(int soBan, string message)
        {
            using (var context = new MenuContext())
            {
                var item = context.BanAns.Find(soBan);
                if (item != null)
                {
                    item.YeuCauHoTro = message;
                    context.SaveChanges();
                }
            }
        }

        public HoaDon GetActiveOrder(int soBan)
        {
            // Cần context mới để lấy dữ liệu mới nhất
            using (var context = new MenuContext())
            {
                return context.HoaDons
                           .Where(h => h.SoBan == soBan && !h.DaThanhToan)
                           .OrderByDescending(h => h.NgayTao)
                           .FirstOrDefault();
            }
        }

        public List<ChiTietHoaDon> GetOrderDetails(string maHoaDon)
        {
            using (var context = new MenuContext())
            {
                return context.ChiTietHoaDons
                           .Include(ct => ct.MonAn) // Tạm bỏ include nếu model chưa update relation
                           .Where(ct => ct.MaHoaDon == maHoaDon)
                           .ToList();
            }
        }

        public static event Action OnPaymentSuccess;

        public void CheckoutTable(int soBan, string maHoaDon)
        {
            using (var context = new MenuContext())
            {
                var hd = context.HoaDons.Find(maHoaDon);
                if (hd != null)
                {
                    hd.DaThanhToan = true;

                    // [MỚI] --- LOGIC TRỪ KHO ---
                    // 1. Lấy tất cả món đã ăn trong hóa đơn này
                    var chiTietHD = context.ChiTietHoaDons.Where(x => x.MaHoaDon == maHoaDon).ToList();

                    foreach (var item in chiTietHD)
                    {
                        // 2. Tìm công thức của từng món
                        var congThucs = context.CongThucs.Where(ct => ct.MaMon == item.MaMon).ToList();

                        foreach (var ct in congThucs)
                        {
                            // 3. Tìm nguyên liệu trong kho
                            var nguyenLieu = context.NguyenLieus.Find(ct.NguyenLieuId);
                            if (nguyenLieu != null)
                            {
                                // 4. Trừ tồn kho: (Định lượng 1 món) * (Số lượng khách gọi)
                                double soLuongTru = ct.SoLuongCan * item.SoLuong;
                                nguyenLieu.SoLuongTon -= soLuongTru;
                            }
                        }
                    }
                    // ---------------------------
                }

                var ban = context.BanAns.Find(soBan);
                if (ban != null)
                {
                    ban.TrangThai = "Trống";
                    ban.YeuCauThanhToan = false;
                    ban.YeuCauHoTro = null;
                    ban.DaInTamTinh = false;
                }
                context.SaveChanges();
                OnPaymentSuccess?.Invoke();
            }
        }

        public void ResolvePaymentRequest(int soBan)
        {
            using (var context = new MenuContext())
            {
                var ban = context.BanAns.Find(soBan);
                if (ban != null)
                {
                    ban.YeuCauThanhToan = false;
                    ban.YeuCauHoTro = null;
                    context.SaveChanges();
                }
            }
        }
    }
}
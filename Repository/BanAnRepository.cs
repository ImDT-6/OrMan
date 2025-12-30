using System;
using System.Collections.ObjectModel;
using System.Linq;
using OrMan.Data;
using OrMan.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using OrMan.Helpers;

namespace OrMan.Services
{
    public class BanAnRepository
    {
        public static event Action OnTableChanged;
        public static event Action OnPaymentSuccess; // Khôi phục event này

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
            using (var context = new MenuContext())
            {
                if (!context.BanAns.Any())
                {
                    for (int i = 1; i <= 20; i++)
                    {
                        context.BanAns.Add(new BanAn(i, "Trống"));
                    }
                    context.SaveChanges();
                }
            }
        }

        // [ĐÃ SỬA] Hàm thêm bàn nhận vào ID cụ thể để lấp chỗ trống
        public bool AddTable(int specificId)
        {
            using (var context = new MenuContext())
            {
                if (context.BanAns.Any(b => b.SoBan == specificId)) return false;

                try
                {
                    var newBan = new BanAn(specificId, "Trống");
                    context.BanAns.Add(newBan);
                    context.SaveChanges();
                    OnTableChanged?.Invoke();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Lỗi thêm bàn: " + ex.Message);
                    return false;
                }
            }
        }

        public bool DeleteTable(int soBan)
        {
            using (var context = new MenuContext())
            {
                var ban = context.BanAns.Find(soBan);
                if (ban != null && ban.TrangThai == "Trống" && ban.YeuCauThanhToan == false)
                {
                    context.BanAns.Remove(ban);
                    context.SaveChanges();
                    OnTableChanged?.Invoke();
                    return true;
                }
                return false;
            }
        }

        // [KHÔI PHỤC] Hàm cập nhật trạng thái đơn giản
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

        public void UpdateTableInfo(int soBan, string tenGoi)
        {
            using (var context = new MenuContext())
            {
                var item = context.BanAns.Find(soBan);
                if (item != null)
                {
                    item.TenGoi = tenGoi;
                    context.SaveChanges();
                }
            }
        }

        // [MỚI - QUAN TRỌNG] Hàm cập nhật trạng thái đã in tạm tính
        public void UpdateTablePrintStatus(int soBan, bool status)
        {
            using (var context = new MenuContext())
            {
                var item = context.BanAns.Find(soBan);
                if (item != null)
                {
                    item.DaInTamTinh = status;
                    context.SaveChanges();
                }
            }
        }

        // [KHÔI PHỤC] Khách yêu cầu thanh toán
        public void RequestPayment(int soBan, string method = "Tiền mặt")
        {
            using (var context = new MenuContext())
            {
                var item = context.BanAns.Find(soBan);
                if (item != null)
                {
                    item.YeuCauThanhToan = true;
                    item.HinhThucThanhToan = method;
                    item.YeuCauHoTro = null;
                    context.SaveChanges();
                }
            }
        }

        // [KHÔI PHỤC] Khách gọi hỗ trợ (kèm logic nối chuỗi tin nhắn)
        public void SendSupportRequest(int soBan, string message)
        {
            using (var context = new MenuContext())
            {
                var item = context.BanAns.Find(soBan);
                if (item != null)
                {
                    string yeuCauHienTai = item.YeuCauHoTro ?? "";
                    string yeuCauMoi = ", " + message;

                    if (yeuCauHienTai.Length + yeuCauMoi.Length > 250)
                    {
                        item.YeuCauHoTro = message; // Quá dài thì reset
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(yeuCauHienTai))
                            item.YeuCauHoTro = message;
                        else
                            item.YeuCauHoTro = yeuCauHienTai + yeuCauMoi;
                    }
                    context.SaveChanges();
                }
            }
        }

        // [KHÔI PHỤC] Nhân viên đã xử lý yêu cầu thanh toán (nhưng chưa checkout)
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

        public bool CheckInTable(int soBan)
        {
            using (var context = new MenuContext())
            {
                var ban = context.BanAns.Find(soBan);
                if (ban != null && ban.TrangThai == "Trống")
                {
                    ban.TrangThai = "Có Khách";

                    var hoaDonMoi = new HoaDon
                    {
                        // Use shared helper to generate order id consistently
                        MaHoaDon = OrderHelper.GenerateOrderId(),
                        SoBan = soBan,
                        NgayTao = DateTime.Now,
                        DaThanhToan = false,
                        TongTien = 0,
                        GiamGia = 0
                    };

                    context.HoaDons.Add(hoaDonMoi);
                    context.SaveChanges();
                    return true;
                }
                return false;
            }
        }

        public HoaDon GetActiveOrder(int soBan)
        {
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
                var rawList = context.ChiTietHoaDons
                           .Include(ct => ct.MonAn)
                           .Where(ct => ct.MaHoaDon == maHoaDon)
                           .ToList();

                var groupedList = rawList
                    .GroupBy(x => new { x.MaMon, x.CapDoCay, x.GhiChu, x.DonGia })
                    .Select(g => new ChiTietHoaDon
                    {
                        MaHoaDon = maHoaDon,
                        MaMon = g.Key.MaMon,
                        TenMonHienThi = g.First().TenMonHienThi,
                        MonAn = g.First().MonAn,
                        CapDoCay = g.Key.CapDoCay,
                        GhiChu = g.Key.GhiChu,
                        DonGia = g.Key.DonGia,
                        SoLuong = g.Sum(x => x.SoLuong),
                        TrangThaiCheBien = g.Min(x => x.TrangThaiCheBien)
                    })
                    .ToList();

                return groupedList;
            }
        }

        public void CheckoutTable(int soBan, string maHoaDon)
        {
            using (var context = new MenuContext())
            {
                var hd = context.HoaDons.Find(maHoaDon);
                if (hd != null)
                {
                    hd.DaThanhToan = true;

                    // Logic trừ kho (nếu có) để ở đây
                    // var chiTietHD = context.ChiTietHoaDons.Where(x => x.MaHoaDon == maHoaDon).ToList();
                }

                var ban = context.BanAns.Find(soBan);
                if (ban != null)
                {
                    ban.TrangThai = "Trống";
                    ban.YeuCauThanhToan = false;
                    ban.YeuCauHoTro = null;
                    ban.DaInTamTinh = false;
                    ban.HinhThucThanhToan = null;
                }
                context.SaveChanges();
                OnPaymentSuccess?.Invoke(); // Gọi event
            }
        }

        public void CancelTableSession(int soBan)
        {
            using (var context = new MenuContext())
            {
                var hd = context.HoaDons.FirstOrDefault(h => h.SoBan == soBan && !h.DaThanhToan);
                if (hd != null)
                {
                    var chiTiet = context.ChiTietHoaDons.Where(ct => ct.MaHoaDon == hd.MaHoaDon);
                    context.ChiTietHoaDons.RemoveRange(chiTiet);
                    context.HoaDons.Remove(hd);
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
            }
        }
    }
}
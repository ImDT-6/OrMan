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
        public static event Action OnTableChanged;

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
                var newBan = new BanAn(maxSoBan + 1, "Trống");
                context.BanAns.Add(newBan);
                context.SaveChanges();

                // [MỚI] 2. Bắn tín hiệu cập nhật
                OnTableChanged?.Invoke();
            }
        }

        // [MỚI] Xóa bàn
        public bool DeleteTable(int soBan)
        {
            using (var context = new MenuContext())
            {
                var ban = context.BanAns.Find(soBan);
                if (ban != null && ban.TrangThai == "Trống" && ban.YeuCauThanhToan == false)
                {
                    context.BanAns.Remove(ban);
                    context.SaveChanges();

                    // [MỚI] 3. Bắn tín hiệu cập nhật
                    OnTableChanged?.Invoke();
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
                    string yeuCauHienTai = item.YeuCauHoTro ?? ""; // Lấy chuỗi hiện tại, nếu null thì là rỗng
                    string yeuCauMoi = ", " + message;

                    // [LỚP BẢO VỆ]: Kiểm tra độ dài
                    // Nếu độ dài hiện tại + độ dài mới > 250 (giới hạn an toàn của cột NVARCHAR 255)
                    if (yeuCauHienTai.Length + yeuCauMoi.Length > 250)
                    {
                        // QUÁ DÀI -> Reset lại, chỉ lưu yêu cầu mới nhất để tránh lỗi DB
                        item.YeuCauHoTro = message;
                    }
                    else
                    {
                        // CÒN CHỖ -> Nối thêm vào
                        if (string.IsNullOrEmpty(yeuCauHienTai))
                        {
                            item.YeuCauHoTro = message;
                        }
                        else
                        {
                            item.YeuCauHoTro = yeuCauHienTai + yeuCauMoi;
                        }
                    }

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
                // 1. Lấy toàn bộ dữ liệu thô từ SQL lên trước
                var rawList = context.ChiTietHoaDons
                           .Include(ct => ct.MonAn)
                           .Where(ct => ct.MaHoaDon == maHoaDon)
                           .ToList();

                // 2. Dùng C# để GỘP các món giống hệt nhau
                var groupedList = rawList
                    .GroupBy(x => new { x.MaMon, x.CapDoCay, x.GhiChu, x.DonGia }) // Nhóm các món có cùng Mã, Cấp độ, Ghi chú, Giá
                    .Select(g => new ChiTietHoaDon
                    {
                        MaHoaDon = maHoaDon,
                        MaMon = g.Key.MaMon,

                        // Lấy thông tin hiển thị từ dòng đầu tiên trong nhóm
                        TenMonHienThi = g.First().TenMonHienThi,
                        MonAn = g.First().MonAn,
                        CapDoCay = g.Key.CapDoCay,
                        GhiChu = g.Key.GhiChu,
                        DonGia = g.Key.DonGia,

                        // [QUAN TRỌNG] Cộng dồn số lượng
                        SoLuong = g.Sum(x => x.SoLuong),

                        // (Tùy chọn) Có thể lấy trạng thái chế biến
                        TrangThaiCheBien = g.Min(x => x.TrangThaiCheBien)
                    })
                    .ToList();

                return groupedList;
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

        // [MỚI] Hàm cập nhật thông tin bàn (Tên gọi,...)
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
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrMan.Data;
using OrMan.Models;

namespace OrMan.Services
{
    public static class DatabaseInitializer
    {
        public static void EnsureDatabaseUpdated()
        {
            using (var context = new MenuContext())
            {
                try
                {
                    context.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EnsureCreated failed: " + ex.Message);
                }

                // Tập hợp script và chạy batch (idempotent). Log thay vì swallow.
                var scripts = GetSchemaScripts();
                foreach (var sql in scripts)
                {
                    ExecuteSqlSafe(context, sql);
                }

                // Seed dữ liệu (tối ưu: kiểm tra và thêm 1 lần)
                try
                {
                    SeedNguyenLieu(context);
                    SeedMoreSampleData(context, targetNguyenLieuCount: 20, targetKhachHangCount: 20);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Seeding error: " + ex.Message);
                }
            }
        }

        private static IEnumerable<string> GetSchemaScripts()
        {
            return new[]
            {
                // BanAn: YeuCauThanhToan
                @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'YeuCauThanhToan' AND Object_ID = Object_ID(N'BanAn'))
BEGIN
    ALTER TABLE BanAn ADD YeuCauThanhToan BIT NOT NULL DEFAULT 0;
END",

                // BanAn: YeuCauHoTro
                @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'YeuCauHoTro' AND Object_ID = Object_ID(N'BanAn'))
BEGIN
    ALTER TABLE BanAn ADD YeuCauHoTro NVARCHAR(MAX) NULL;
END",

                // MonAn: IsSoldOut
                @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsSoldOut' AND Object_ID = Object_ID(N'MonAn'))
BEGIN
    ALTER TABLE MonAn ADD IsSoldOut BIT NOT NULL DEFAULT 0;
END",

                // BanAn: HinhThucThanhToan
                @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'HinhThucThanhToan' AND Object_ID = Object_ID(N'BanAn'))
BEGIN
    ALTER TABLE BanAn ADD HinhThucThanhToan NVARCHAR(50) NULL;
END",

                // KhachHang table
                @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KhachHang]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[KhachHang](
        [KhachHangID] [int] IDENTITY(1,1) NOT NULL,
        [SoDienThoai] [nvarchar](20) NOT NULL,
        [HoTen] [nvarchar](100) NULL,
        [DiemTichLuy] [int] NOT NULL DEFAULT 0,
        [HangThanhVien] [nvarchar](20) DEFAULT N'Khách Hàng Mới',
        [NgayThamGia] [datetime] NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_KhachHang] PRIMARY KEY CLUSTERED ([KhachHangID] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_KhachHang_SoDienThoai] ON [dbo].[KhachHang] ([SoDienThoai] ASC);
END",

                // NguyenLieu table
                @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NguyenLieu]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NguyenLieu](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TenNguyenLieu] [nvarchar](100) NOT NULL,
        [DonViTinh] [nvarchar](20) NULL,
        [SoLuongTon] [float] NOT NULL DEFAULT 0,
        [GiaVon] [decimal](18, 0) NOT NULL DEFAULT 0,
        [DinhMucToiThieu] [float] NOT NULL DEFAULT 0
    );
END",

                // CongThuc table
                @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CongThuc]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CongThuc](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MaMon] [nvarchar](450) NOT NULL,
        [NguyenLieuId] [int] NOT NULL,
        [SoLuongCan] [float] NOT NULL DEFAULT 0
    );
END",

                // ChiTietHoaDon: TrangThaiCheBien (2 tên bảng dự phòng)
                @"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrangThaiCheBien' AND Object_ID = Object_ID(N'ChiTietHoaDon'))
BEGIN
    ALTER TABLE ChiTietHoaDon ADD TrangThaiCheBien INT NOT NULL DEFAULT 0;
END",
                @"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrangThaiCheBien' AND Object_ID = Object_ID(N'ChiTietHoaDons'))
BEGIN
    ALTER TABLE ChiTietHoaDons ADD TrangThaiCheBien INT NOT NULL DEFAULT 0;
END",

                // BanAn: TenGoi
                @"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TenGoi' AND Object_ID = Object_ID(N'BanAn'))
BEGIN
    ALTER TABLE BanAn ADD TenGoi NVARCHAR(50) NULL;
END",

                // Expand YeuCauHoTro (safe attempt)
                @"BEGIN TRY
    ALTER TABLE BanAn ALTER COLUMN YeuCauHoTro NVARCHAR(MAX) NULL;
END TRY
BEGIN CATCH
    -- ignore if not possible
END CATCH",

                // ChiTietHoaDon: ThoiGianGoiMon
                @"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ThoiGianGoiMon' AND Object_ID = Object_ID(N'ChiTietHoaDon'))
BEGIN
    ALTER TABLE ChiTietHoaDon ADD ThoiGianGoiMon DATETIME NULL;
END",

                // DanhGia table
                @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DanhGia]'))
CREATE TABLE [dbo].[DanhGia](
    [Id] int IDENTITY(1,1) PRIMARY KEY,
    [SoSao] int NOT NULL,
    [CacTag] nvarchar(500),
    [NoiDung] nvarchar(MAX),
    [SoDienThoai] nvarchar(20),
    [NgayTao] datetime DEFAULT GETDATE()
);",

                // VoucherCuaKhach
                @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VoucherCuaKhach]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VoucherCuaKhach](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [KhachHangID] [int] NOT NULL,
        [TenPhanThuong] [nvarchar](100) NOT NULL,
        [NgayTrungThuong] [datetime] NOT NULL DEFAULT GETDATE(),
        [DaSuDung] [bit] NOT NULL DEFAULT 0,
        [NgaySuDung] [datetime] NULL,
        [LoaiVoucher] [int] NOT NULL DEFAULT 0, 
        [GiaTri] [float] NOT NULL DEFAULT 0
    );
    CREATE NONCLUSTERED INDEX [IX_VoucherCuaKhach_KhachHangID] ON [dbo].[VoucherCuaKhach] ([KhachHangID] ASC);
END",

                // HoaDon: GiamGia
                @"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'GiamGia' AND Object_ID = Object_ID(N'HoaDon'))
BEGIN
    ALTER TABLE HoaDon ADD GiamGia DECIMAL(18,0) NOT NULL DEFAULT 0;
END",

                // KhachHang: NgayThamGia
                @"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'NgayThamGia' AND Object_ID = Object_ID(N'KhachHang'))
BEGIN
    ALTER TABLE KhachHang ADD NgayThamGia DATETIME NOT NULL DEFAULT GETDATE();
END"
            };
        }

        private static void ExecuteSqlSafe(MenuContext context, string sql)
        {
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ExecuteSqlRaw failed: " + ex.Message + " | SQL: " + (sql?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ""));
            }
        }

        private static void SeedNguyenLieu(MenuContext db)
        {
            try
            {
                if (db.NguyenLieus.AsNoTracking().Any()) return;

                var danhSachMau = new List<NguyenLieu>
                {
                    new NguyenLieu { TenNguyenLieu = "Mì Ramen Hàn Quốc (Koreno)", DonViTinh = "Gói", GiaVon = 6500, SoLuongTon = 120, DinhMucToiThieu = 50 },
                    new NguyenLieu { TenNguyenLieu = "Kim chi cải thảo", DonViTinh = "Kg", GiaVon = 45000, SoLuongTon = 2.5, DinhMucToiThieu = 10 },
                    new NguyenLieu { TenNguyenLieu = "Mực ống cắt khoanh", DonViTinh = "Kg", GiaVon = 220000, SoLuongTon = 1.2, DinhMucToiThieu = 5.0 },
                    new NguyenLieu { TenNguyenLieu = "Thịt bò Mỹ thái lát", DonViTinh = "Kg", GiaVon = 320000, SoLuongTon = 8.5, DinhMucToiThieu = 5.0 },
                    new NguyenLieu { TenNguyenLieu = "Cá viên thập cẩm", DonViTinh = "Kg", GiaVon = 75000, SoLuongTon = 0, DinhMucToiThieu = 4.0 },
                    new NguyenLieu { TenNguyenLieu = "Ớt bột Hàn Quốc", DonViTinh = "Kg", GiaVon = 150000, SoLuongTon = 0.8, DinhMucToiThieu = 2.0 }
                };

                db.NguyenLieus.AddRange(danhSachMau);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SeedNguyenLieu error: " + ex.Message);
            }
        }

        private static void SeedMoreSampleData(MenuContext db, int targetNguyenLieuCount = 20, int targetKhachHangCount = 20)
        {
            try
            {
                // ---- NguyenLieu ----
                int currentNL = db.NguyenLieus.Count();
                if (currentNL < targetNguyenLieuCount)
                {
                    var candidates = new List<NguyenLieu>
                    {
                        new NguyenLieu{ TenNguyenLieu = "Gạo Tẻ", DonViTinh = "Kg", GiaVon = 15000, SoLuongTon = 50, DinhMucToiThieu = 10},
                        new NguyenLieu{ TenNguyenLieu = "Đường", DonViTinh = "Kg", GiaVon = 12000, SoLuongTon = 30, DinhMucToiThieu = 5},
                        new NguyenLieu{ TenNguyenLieu = "Muối I-ốt", DonViTinh = "Kg", GiaVon = 8000, SoLuongTon = 20, DinhMucToiThieu = 5},
                        new NguyenLieu{ TenNguyenLieu = "Dầu ăn", DonViTinh = "Lít", GiaVon = 25000, SoLuongTon = 10, DinhMucToiThieu = 3},
                        new NguyenLieu{ TenNguyenLieu = "Tỏi", DonViTinh = "Kg", GiaVon = 40000, SoLuongTon = 8, DinhMucToiThieu = 2},
                        new NguyenLieu{ TenNguyenLieu = "Hành tây", DonViTinh = "Kg", GiaVon = 20000, SoLuongTon = 15, DinhMucToiThieu = 5},
                        new NguyenLieu{ TenNguyenLieu = "Nước mắm", DonViTinh = "Lít", GiaVon = 60000, SoLuongTon = 6, DinhMucToiThieu = 2},
                        new NguyenLieu{ TenNguyenLieu = "Xì dầu (Soy sauce)", DonViTinh = "Lít", GiaVon = 50000, SoLuongTon = 5, DinhMucToiThieu = 1},
                        new NguyenLieu{ TenNguyenLieu = "Giấm", DonViTinh = "Lít", GiaVon = 12000, SoLuongTon = 4, DinhMucToiThieu = 1},
                        new NguyenLieu{ TenNguyenLieu = "Gừng", DonViTinh = "Kg", GiaVon = 45000, SoLuongTon = 3, DinhMucToiThieu = 1},
                        new NguyenLieu{ TenNguyenLieu = "Hành lá", DonViTinh = "Bó", GiaVon = 3000, SoLuongTon = 40, DinhMucToiThieu = 5},
                        new NguyenLieu{ TenNguyenLieu = "Nấm Mèo", DonViTinh = "Kg", GiaVon = 90000, SoLuongTon = 2, DinhMucToiThieu = 1},
                        new NguyenLieu{ TenNguyenLieu = "Đậu hũ", DonViTinh = "Kg", GiaVon = 25000, SoLuongTon = 6, DinhMucToiThieu = 2},
                        new NguyenLieu{ TenNguyenLieu = "Bột ngọt", DonViTinh = "Kg", GiaVon = 45000, SoLuongTon = 5, DinhMucToiThieu = 1},
                        new NguyenLieu{ TenNguyenLieu = "Mì ăn liền", DonViTinh = "Thùng", GiaVon = 200000, SoLuongTon = 3, DinhMucToiThieu = 1},
                        new NguyenLieu{ TenNguyenLieu = "Thịt gà xé", DonViTinh = "Kg", GiaVon = 90000, SoLuongTon = 7, DinhMucToiThieu = 2},
                        new NguyenLieu{ TenNguyenLieu = "Thịt heo băm", DonViTinh = "Kg", GiaVon = 120000, SoLuongTon = 5, DinhMucToiThieu = 2},
                        new NguyenLieu{ TenNguyenLieu = "Ớt tươi", DonViTinh = "Kg", GiaVon = 60000, SoLuongTon = 6, DinhMucToiThieu = 1},
                        new NguyenLieu{ TenNguyenLieu = "Hạt tiêu", DonViTinh = "Kg", GiaVon = 180000, SoLuongTon = 1, DinhMucToiThieu = 0.5}
                    };

                    var existingNames = new HashSet<string>(db.NguyenLieus.Select(n => n.TenNguyenLieu));

                    var toAdd = new List<NguyenLieu>();
                    foreach (var cand in candidates)
                    {
                        if (existingNames.Contains(cand.TenNguyenLieu)) continue;
                        toAdd.Add(cand);
                        if (existingNames.Count + toAdd.Count >= targetNguyenLieuCount) break;
                    }

                    if (toAdd.Any())
                    {
                        db.NguyenLieus.AddRange(toAdd);
                        db.SaveChanges();
                    }
                }

                // ---- KhachHang ----
                int currentKH = 0;
                try { currentKH = db.KhachHangs.Count(); } catch { currentKH = 0; }

                if (currentKH < targetKhachHangCount)
                {
                    var listKH = new List<KhachHang>();
                    var rnd = new Random();
                    // avoid duplicates
                    var existingPhones = new HashSet<string>(db.KhachHangs.Select(k => k.SoDienThoai));

                    for (int i = currentKH + 1; i <= targetKhachHangCount; i++)
                    {
                        string phone;
                        do
                        {
                            phone = "090" + (1000000 + i).ToString();
                        } while (existingPhones.Contains(phone));

                        existingPhones.Add(phone);

                        var kh = new KhachHang
                        {
                            SoDienThoai = phone,
                            HoTen = $"Khách Mẫu {i}",
                            DiemTichLuy = rnd.Next(0, 6000),
                            HangThanhVien = "Khách Hàng Mới",
                            NgayThamGia = DateTime.Now.AddDays(-rnd.Next(0, 365))
                        };

                        if (kh.DiemTichLuy >= 5000) kh.HangThanhVien = "Kim Cương";
                        else if (kh.DiemTichLuy >= 1000) kh.HangThanhVien = "Vàng";

                        listKH.Add(kh);
                    }

                    if (listKH.Any())
                    {
                        db.KhachHangs.AddRange(listKH);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SeedMoreSampleData error: " + ex.Message);
            }
        }

        // kept for compatibility
        private static void ExecuteRawSql(MenuContext context, string sql)
        {
            ExecuteSqlSafe(context, sql);
        }
    }
}
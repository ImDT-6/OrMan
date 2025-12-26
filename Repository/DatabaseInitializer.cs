using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OrMan.Data;
using OrMan.Models;
using System.Linq;
namespace OrMan.Services
{
    public static class DatabaseInitializer
    {
        public static void EnsureDatabaseUpdated()
        {
            using (var context = new MenuContext())
            {
                // 1. Đảm bảo DB đã được tạo
                context.Database.EnsureCreated();

                // 2. Cột YeuCauThanhToan (Cho Bàn Ăn)
                try
                {
                    string sqlBanAn = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'YeuCauThanhToan' AND Object_ID = Object_ID(N'BanAn'))
                        BEGIN
                            ALTER TABLE BanAn ADD YeuCauThanhToan BIT NOT NULL DEFAULT 0;
                        END";
                    context.Database.ExecuteSqlRaw(sqlBanAn);
                }
                catch { }

                // 3. Cột YeuCauHoTro
                try
                {
                    string sqlHoTro = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'YeuCauHoTro' AND Object_ID = Object_ID(N'BanAn'))
                        BEGIN
                            ALTER TABLE BanAn ADD YeuCauHoTro NVARCHAR(MAX) NULL;
                        END";
                    context.Database.ExecuteSqlRaw(sqlHoTro);
                }
                catch { }

                // 4. Cột IsSoldOut (Cho Món Ăn)
                try
                {
                    string sqlMonAn = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IsSoldOut' AND Object_ID = Object_ID(N'MonAn'))
                        BEGIN
                            ALTER TABLE MonAn ADD IsSoldOut BIT NOT NULL DEFAULT 0;
                        END";
                    context.Database.ExecuteSqlRaw(sqlMonAn);
                }
                catch { }

                // 5. Cột HinhThucThanhToan
                try
                {
                    string sql = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'HinhThucThanhToan' AND Object_ID = Object_ID(N'BanAn'))
                        BEGIN
                            ALTER TABLE BanAn ADD HinhThucThanhToan NVARCHAR(50) NULL;
                        END";
                    context.Database.ExecuteSqlRaw(sql);
                }
                catch { }

                // 6. Tạo bảng KhachHang (Nếu chưa có)
                try
                {
                    string sqlTaoBangKhach = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KhachHang]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[KhachHang](
                                [KhachHangID] [int] IDENTITY(1,1) NOT NULL,
                                [SoDienThoai] [nvarchar](20) NOT NULL,
                                [HoTen] [nvarchar](100) NULL,
                                [DiemTichLuy] [int] NOT NULL DEFAULT 0,
                                [HangThanhVien] [nvarchar](20) DEFAULT N'Khách Hàng Mới',
                                [NgayThamGia] [datetime] NOT NULL DEFAULT GETDATE(), -- Có sẵn cột này khi tạo mới
                                CONSTRAINT [PK_KhachHang] PRIMARY KEY CLUSTERED ([KhachHangID] ASC)
                            );
                            -- Index cho SĐT
                            CREATE UNIQUE NONCLUSTERED INDEX [IX_KhachHang_SoDienThoai] ON [dbo].[KhachHang] ([SoDienThoai] ASC);
                        END";

                    context.Database.ExecuteSqlRaw(sqlTaoBangKhach);
                }
                catch { }

                // 7. Tạo bảng Nguyên Liệu
                try
                {
                    string sqlNguyenLieu = @"
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
                        END";
                    context.Database.ExecuteSqlRaw(sqlNguyenLieu);
                }
                catch { }

                // 8. Tạo bảng Công Thức
                try
                {
                    string sqlCongThuc = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CongThuc]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[CongThuc](
                                [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [MaMon] [nvarchar](450) NOT NULL, 
                                [NguyenLieuId] [int] NOT NULL,
                                [SoLuongCan] [float] NOT NULL DEFAULT 0
                            );
                        END";
                    context.Database.ExecuteSqlRaw(sqlCongThuc);
                }
                catch { }

                // 9. Cột TrangThaiCheBien (Cho Bếp)
                try
                {
                    string sqlBep = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrangThaiCheBien' AND Object_ID = Object_ID(N'ChiTietHoaDon'))
                        BEGIN
                            ALTER TABLE ChiTietHoaDon ADD TrangThaiCheBien INT NOT NULL DEFAULT 0;
                        END";
                    context.Database.ExecuteSqlRaw(sqlBep);
                }
                catch
                {
                    // Fallback nếu tên bảng có 's'
                    try
                    {
                        string sqlBepBackup = @"
                            IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrangThaiCheBien' AND Object_ID = Object_ID(N'ChiTietHoaDons'))
                            BEGIN
                                ALTER TABLE ChiTietHoaDons ADD TrangThaiCheBien INT NOT NULL DEFAULT 0;
                            END";
                        context.Database.ExecuteSqlRaw(sqlBepBackup);
                    }
                    catch { }
                }

                // 10. Các cập nhật khác (TenGoi, ThoiGianGoiMon, DanhGia...)
                try
                {
                    string sqlTenGoi = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TenGoi' AND Object_ID = Object_ID(N'BanAn'))
                        BEGIN
                            ALTER TABLE BanAn ADD TenGoi NVARCHAR(50) NULL;
                        END";
                    context.Database.ExecuteSqlRaw(sqlTenGoi);
                }
                catch { }

                try
                {
                    string sqlExpand = "ALTER TABLE BanAn ALTER COLUMN YeuCauHoTro NVARCHAR(MAX) NULL";
                    context.Database.ExecuteSqlRaw(sqlExpand);
                }
                catch { }

                try
                {
                    string sqlTime = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ThoiGianGoiMon' AND Object_ID = Object_ID(N'ChiTietHoaDon'))
                        BEGIN
                            ALTER TABLE ChiTietHoaDon ADD ThoiGianGoiMon DATETIME NULL;
                        END";
                    context.Database.ExecuteSqlRaw(sqlTime);
                }
                catch { }

                try
                {
                    string sqlDanhGia = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DanhGia]'))
                       CREATE TABLE [dbo].[DanhGia](
                           [Id] int IDENTITY(1,1) PRIMARY KEY,
                           [SoSao] int NOT NULL,
                           [CacTag] nvarchar(500),
                           [NoiDung] nvarchar(MAX),
                           [SoDienThoai] nvarchar(20),
                           [NgayTao] datetime DEFAULT GETDATE()
                       );";
                    context.Database.ExecuteSqlRaw(sqlDanhGia);
                }
                catch { }

                // 11. Bảng Voucher
                try
                {
                    string sqlVoucher = @"
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
                        END";
                    context.Database.ExecuteSqlRaw(sqlVoucher);
                }
                catch { }

                try
                {
                    string sqlGiamGia = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'GiamGia' AND Object_ID = Object_ID(N'HoaDon'))
                        BEGIN
                            ALTER TABLE HoaDon ADD GiamGia DECIMAL(18,0) NOT NULL DEFAULT 0;
                        END";
                    context.Database.ExecuteSqlRaw(sqlGiamGia);
                }
                catch { }

                // ============================================================
                // 12. [MỚI - QUAN TRỌNG] Thêm cột NgayThamGia cho bảng KhachHang
                // Script này sẽ chạy nếu Database đã tồn tại nhưng chưa có cột này
                // ============================================================
                try
                {
                    string sqlNgayThamGia = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'NgayThamGia' AND Object_ID = Object_ID(N'KhachHang'))
                        BEGIN
                            ALTER TABLE KhachHang ADD NgayThamGia DATETIME NOT NULL DEFAULT GETDATE();
                        END";
                    context.Database.ExecuteSqlRaw(sqlNgayThamGia);
                }
                catch { }

                SeedNguyenLieu(context);

            }
        }
        private static void SeedNguyenLieu(MenuContext db)
        {
            // SỬA LỖI: Sử dụng Enumerable.Any hoặc kiểm tra Count để đảm bảo trình biên dịch nhận diện được
            // Nếu bảng NguyenLieus đã có bất kỳ bản ghi nào thì thoát ra, không seed nữa
            if (db.NguyenLieus.Any()) return;

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

        private static void ExecuteRawSql(MenuContext context, string sql)
        {
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch
            {
                // Bỏ qua lỗi nếu lệnh SQL không thực hiện được (ví dụ cột đã tồn tại)
            }
        }

    }
}
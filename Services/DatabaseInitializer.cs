using OrMan.Data;
using Microsoft.EntityFrameworkCore;
using System;

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

                // 2. Cột YeuCauThanhToan
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
                            ALTER TABLE BanAn ADD YeuCauHoTro NVARCHAR(255) NULL;
                        END";
                    context.Database.ExecuteSqlRaw(sqlHoTro);
                }
                catch { }

                // 4. Cột IsSoldOut
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

                // [MỚI] Thêm cột HinhThucThanhToan nếu chưa có
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

                // 5. [MỚI] Tự động tạo bảng KhachHang nếu chưa có
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
                                [HangKhachHang  ] [nvarchar](20) DEFAULT N'Khách Hàng Mới',
                                CONSTRAINT [PK_KhachHang] PRIMARY KEY CLUSTERED ([KhachHangID] ASC)
                            );

                            -- Tạo thêm Index cho SĐT để tìm nhanh hơn
                            CREATE UNIQUE NONCLUSTERED INDEX [IX_KhachHang_SoDienThoai] ON [dbo].[KhachHang] ([SoDienThoai] ASC);
                        END";

                    context.Database.ExecuteSqlRaw(sqlTaoBangKhach);
                }
                catch (Exception ex)
                {
                    // Nếu lỗi thì ghi ra console để biết đường sửa
                    Console.WriteLine("Lỗi tạo bảng KhachHang: " + ex.Message);
                }
                // 6. [MỚI] Tạo bảng Nguyên Liệu
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

                // 7. [MỚI] Tạo bảng Công Thức
                try
                {
                    string sqlCongThuc = @"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CongThuc]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [dbo].[CongThuc](
                [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [MaMon] [nvarchar](450) NOT NULL, -- Chú ý kiểu dữ liệu phải khớp MaMon cũ
                [NguyenLieuId] [int] NOT NULL,
                [SoLuongCan] [float] NOT NULL DEFAULT 0
            );
            -- Tạo Foreign Key (Tùy chọn, để đảm bảo dữ liệu chuẩn)
        END";
                    context.Database.ExecuteSqlRaw(sqlCongThuc);
                }
                catch { }

                // 8. [MỚI - SỬA LỖI] Thêm cột TrangThaiCheBien cho bảng ChiTietHoaDons
                try
                {
                    // Lưu ý: Tên bảng trong SQL thường là 'ChiTietHoaDons' (có s) do EF tự đặt
                    string sqlBep = @"
        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrangThaiCheBien' AND Object_ID = Object_ID(N'ChiTietHoaDons'))
        BEGIN
            ALTER TABLE ChiTietHoaDons ADD TrangThaiCheBien INT NOT NULL DEFAULT 0;
        END";
                    context.Database.ExecuteSqlRaw(sqlBep);
                }
                catch (Exception ex)
                {
                    // Nếu lỗi tên bảng, thử tên không có 's'
                    try
                    {
                        string sqlBepBackup = @"
            IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrangThaiCheBien' AND Object_ID = Object_ID(N'ChiTietHoaDon'))
            BEGIN
                ALTER TABLE ChiTietHoaDon ADD TrangThaiCheBien INT NOT NULL DEFAULT 0;
            END";
                        context.Database.ExecuteSqlRaw(sqlBepBackup);
                    }
                    catch { }

                    // ... (Các đoạn code cũ tạo bảng KhachHang, NguyenLieu...)

                    // 9. [MỚI] Thêm cột TenGoi cho bảng BanAn
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
                }
            }

        }
    }
}
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

                // 3. Cột YeuCauHoTro (Tạo mới nếu chưa có)
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
                                [MaMon] [nvarchar](450) NOT NULL, 
                                [NguyenLieuId] [int] NOT NULL,
                                [SoLuongCan] [float] NOT NULL DEFAULT 0
                            );
                        END";
                    context.Database.ExecuteSqlRaw(sqlCongThuc);
                }
                catch { }

                // 8. [MỚI - SỬA LỖI] Thêm cột TrangThaiCheBien
                // Tách riêng Try-Catch này ra, không lồng cái khác vào trong Catch
                try
                {
                    string sqlBep = @"
                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrangThaiCheBien' AND Object_ID = Object_ID(N'ChiTietHoaDons'))
                        BEGIN
                            ALTER TABLE ChiTietHoaDons ADD TrangThaiCheBien INT NOT NULL DEFAULT 0;
                        END";
                    context.Database.ExecuteSqlRaw(sqlBep);
                }
                catch
                {
                    // Nếu lỗi (thường do tên bảng không có 's'), thử phương án dự phòng
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
                }

                // 9. [MỚI] Thêm cột TenGoi cho bảng BanAn
                // (Đã đưa ra ngoài Try-Catch của mục 8)
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

                // 10. [QUAN TRỌNG] Mở rộng cột YeuCauHoTro lên tối đa
                // (Đã đưa ra ngoài Try-Catch của mục 8)
                try
                {
                    // Câu lệnh này ép kiểu dữ liệu từ cũ (ví dụ 255) lên NVARCHAR(MAX)
                    // Nếu cột chưa có thì lệnh ALTER sẽ lỗi nhẹ nhưng không sao vì lệnh CREATE ở mục 3 đã tạo rồi.
                    // Mục đích chính là để UPDATE những DB cũ đang bị giới hạn 255 ký tự.
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

                    // Cập nhật dữ liệu cũ để không bị null (lấy theo giờ hóa đơn)
                    context.Database.ExecuteSqlRaw("UPDATE ChiTietHoaDon SET ThoiGianGoiMon = (SELECT NgayTao FROM HoaDon WHERE HoaDon.MaHoaDon = ChiTietHoaDon.MaHoaDon) WHERE ThoiGianGoiMon IS NULL");
                }
                catch { }
            }
        }
    }
}
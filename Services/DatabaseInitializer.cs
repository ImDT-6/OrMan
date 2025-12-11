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
            }
        }
    }
}
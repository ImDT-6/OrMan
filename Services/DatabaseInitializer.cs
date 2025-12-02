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

                // 3. [MỚI] Thêm cột YeuCauHoTro (NVARCHAR)
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
            }
        }
    }
}
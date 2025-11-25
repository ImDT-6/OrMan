using GymManagement.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace GymManagement.Services
{
    public static class DatabaseInitializer
    {
        public static void EnsureDatabaseUpdated()
        {
            using (var context = new MenuContext())
            {
                // 1. Đảm bảo DB đã được tạo
                context.Database.EnsureCreated();

                // 2. Chạy lệnh SQL để thêm cột 'YeuCauThanhToan' vào bảng BanAn (nếu chưa có)
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

                // 3. Chạy lệnh SQL để thêm cột 'IsSoldOut' vào bảng MonAn (nếu chưa có)
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
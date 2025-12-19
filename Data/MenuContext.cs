using Microsoft.EntityFrameworkCore;
using OrMan.Models; // Chỉ cần using Models thôi

namespace OrMan.Data
{
    public class MenuContext : DbContext
    {
        // Danh sách các bảng trong Database
        public DbSet<MonAn> MonAns { get; set; }
        public DbSet<MonMiCay> MonMiCays { get; set; }
        public DbSet<MonPhu> MonPhus { get; set; }
        public DbSet<BanAn> BanAns { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        // ...
        public DbSet<NguyenLieu> NguyenLieus { get; set; }
        public DbSet<CongThuc> CongThucs { get; set; }
        public DbSet<DanhGia> DanhGias { get; set; }

        public DbSet<VoucherCuaKhach> VoucherCuaKhachs { get; set; }
        // ...
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Cấu hình chuỗi kết nối tới SQL Server
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MiCayDB_Final_V2;Integrated Security=True;")
                          .UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình bảng
            modelBuilder.Entity<KhachHang>().ToTable("KhachHang");
            modelBuilder.Entity<KhachHang>()
                .HasIndex(k => k.SoDienThoai)
                .IsUnique();

            modelBuilder.Entity<MonAn>().ToTable("MonAn");
            modelBuilder.Entity<BanAn>().ToTable("BanAn");
            modelBuilder.Entity<HoaDon>().ToTable("HoaDon");
            modelBuilder.Entity<ChiTietHoaDon>().ToTable("ChiTietHoaDon");
        }
    }
}
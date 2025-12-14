using System.ComponentModel;
using System.Runtime.CompilerServices;
using OrMan.Services; // Thêm namespace này

namespace OrMan.ViewModels
{
    public class AdminViewModel : INotifyPropertyChanged
    {
        // Khai báo Repository
        private readonly BanAnRepository _banRepo;
        private readonly DoanhThuRepository _doanhThuRepo;

        private int _banDaDat;
        public int BanDaDat
        {
            get => _banDaDat;
            set { _banDaDat = value; OnPropertyChanged(); }
        }

        private string _doanhThu;
        public string DoanhThu
        {
            get => _doanhThu;
            set { _doanhThu = value; OnPropertyChanged(); }
        }

        // Đã xóa DonChoBep vì logic bếp nằm ở ViewModel khác, 
        // hoặc bạn có thể giữ lại và query từ ChiTietHoaDon nếu muốn.

        public AdminViewModel()
        {
            _banRepo = new BanAnRepository();
            _doanhThuRepo = new DoanhThuRepository();
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            // [CODE MỚI - LẤY DỮ LIỆU THẬT]

            // 1. Đếm số bàn có khách
            var danhSachBan = _banRepo.GetAll();
            // Đếm bàn có trạng thái không phải "Trống"
            BanDaDat = System.Linq.Enumerable.Count(danhSachBan, b => b.TrangThai != "Trống");

            // 2. Tính doanh thu hôm nay
            decimal tongTien = _doanhThuRepo.GetTodayRevenue();

            // Format rút gọn: 1.500.000 -> 1.5M
            if (tongTien >= 1000000)
                DoanhThu = (tongTien / 1000000).ToString("0.##") + "M";
            else if (tongTien >= 1000)
                DoanhThu = (tongTien / 1000).ToString("0.##") + "k";
            else
                DoanhThu = tongTien.ToString("N0");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
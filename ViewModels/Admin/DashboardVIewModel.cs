using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GymManagement.Services; // Thêm namespace

namespace GymManagement.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly BanAnRepository _banRepo;
        private readonly DoanhThuRepository _doanhThuRepo;

        private int _banDaDat;
        private string _doanhThu;
        private int _donChoBep;

        public int BanDaDat
        {
            get => _banDaDat;
            set { _banDaDat = value; OnPropertyChanged(); }
        }

        public string DoanhThu
        {
            get => _doanhThu;
            set { _doanhThu = value; OnPropertyChanged(); }
        }

        public int DonChoBep
        {
            get => _donChoBep;
            set { _donChoBep = value; OnPropertyChanged(); }
        }

        public DashboardViewModel()
        {
            _banRepo = new BanAnRepository();
            _doanhThuRepo = new DoanhThuRepository();

            LoadRealData();
        }

        public void LoadRealData()
        {
            // 1. Đếm số bàn đang có khách hoặc đã đặt
            var listBan = _banRepo.GetAll();
            BanDaDat = listBan.Count(b => b.TrangThai != "Trống");

            // 2. Tính tổng tiền hôm nay
            decimal totalToday = _doanhThuRepo.GetTodayRevenue();

            // Rút gọn số tiền hiển thị (Ví dụ: 1.5M)
            if (totalToday > 1000000)
                DoanhThu = (totalToday / 1000000).ToString("0.##") + "M";
            else
                DoanhThu = totalToday.ToString("N0");

            // 3. Đơn chờ (Giả lập, vì chưa làm module Bếp)
            DonChoBep = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
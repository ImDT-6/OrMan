using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OrMan.ViewModels
{
    public class AdminViewModel : INotifyPropertyChanged
    {
        // Các biến lưu dữ liệu
        private int _banDaDat;
        private string _doanhThu;
        private int _donChoBep;

        // Property để Binding ra màn hình
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

        public AdminViewModel()
        {
            // Giả lập load dữ liệu từ Database
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            // Số liệu giả
            BanDaDat = 18;      // 18/30 bàn
            DoanhThu = "8.5M";  // 8.5 Triệu
            DonChoBep = 3;      // 3 đơn đang chờ
        }

        // Code chuẩn MVVM để thông báo thay đổi dữ liệu
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
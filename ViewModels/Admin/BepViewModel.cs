using System.Collections.ObjectModel;
using System.ComponentModel; // Cần thiết
using System.Linq;
using System.Runtime.CompilerServices; // Cần thiết
using System.Windows.Input;
using System.Windows.Threading;
using GymManagement.Data;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.ViewModels
{
    public class BepOrderItem
    {
        public int SoBan { get; set; }
        public string ThoiGianOrder { get; set; }
        public ChiTietHoaDon ChiTiet { get; set; }
    }

    // [FIX] Thêm kế thừa INotifyPropertyChanged
    public class BepViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer;

        private ObservableCollection<BepOrderItem> _danhSachCanLam;
        public ObservableCollection<BepOrderItem> DanhSachCanLam
        {
            get => _danhSachCanLam;
            set { _danhSachCanLam = value; OnPropertyChanged(); }
        }

        public ICommand XongMonCommand { get; private set; }

        public BepViewModel()
        {
            XongMonCommand = new RelayCommand<BepOrderItem>(XongMon);
            LoadData();

            _timer = new DispatcherTimer();
            _timer.Interval = System.TimeSpan.FromSeconds(10);
            _timer.Tick += (s, e) => LoadData();
            _timer.Start();
        }

        private void LoadData()
        {
            using (var context = new MenuContext())
            {
                var query = from ct in context.ChiTietHoaDons
                            join hd in context.HoaDons on ct.MaHoaDon equals hd.MaHoaDon
                            where !hd.DaThanhToan && ct.TrangThaiCheBien == 0
                            orderby hd.NgayTao
                            select new BepOrderItem
                            {
                                SoBan = hd.SoBan,
                                ThoiGianOrder = hd.NgayTao.ToString("HH:mm"),
                                ChiTiet = ct
                            };

                DanhSachCanLam = new ObservableCollection<BepOrderItem>(query.ToList());
            }
        }

        private void XongMon(BepOrderItem item)
        {
            using (var context = new MenuContext())
            {
                var ct = context.ChiTietHoaDons.Find(item.ChiTiet.Id);
                if (ct != null)
                {
                    ct.TrangThaiCheBien = 1;
                    context.SaveChanges();
                }
            }
            LoadData();
        }

        // [FIX LỖI] Triển khai Interface INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
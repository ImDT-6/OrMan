using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Cần thiết cho Task.Run
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading; // Cần thiết cho Timer
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Views.Admin;

namespace OrMan.ViewModels.Admin
{
    public class KhoViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer; // Timer để tự động cập nhật kho

        private ObservableCollection<NguyenLieu> _danhSachNguyenLieu;
        public ObservableCollection<NguyenLieu> DanhSachNguyenLieu
        {
            get => _danhSachNguyenLieu;
            set { _danhSachNguyenLieu = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public KhoViewModel()
        {
            // [TỐI ƯU] Không gọi LoadData() trực tiếp, chạy ngầm
            Task.Run(() => LoadDataAsync());

            AddCommand = new RelayCommand<object>((p) =>
            {
                var wd = new ThemNguyenLieuWindow();
                if (wd.ShowDialog() == true && wd.Result != null)
                {
                    // Thao tác ghi DB này nhanh nên có thể để sync, hoặc chuyển async nếu muốn
                    using (var db = new MenuContext())
                    {
                        db.NguyenLieus.Add(wd.Result);
                        db.SaveChanges();
                    }
                    // Load lại danh sách (Async)
                    Task.Run(() => LoadDataAsync());
                }
            });

            EditCommand = new RelayCommand<NguyenLieu>((nl) =>
            {
                if (nl == null) return;
                var wd = new ThemNguyenLieuWindow(nl);
                if (wd.ShowDialog() == true)
                {
                    using (var db = new MenuContext())
                    {
                        var item = db.NguyenLieus.Find(nl.Id);
                        if (item != null)
                        {
                            item.TenNguyenLieu = wd.Result.TenNguyenLieu;
                            item.DonViTinh = wd.Result.DonViTinh;
                            item.GiaVon = wd.Result.GiaVon;
                            item.SoLuongTon = wd.Result.SoLuongTon;
                            item.DinhMucToiThieu = wd.Result.DinhMucToiThieu;
                            db.SaveChanges();
                        }
                    }
                    Task.Run(() => LoadDataAsync());
                }
            });

            DeleteCommand = new RelayCommand<NguyenLieu>((nl) =>
            {
                if (MessageBox.Show($"Xóa nguyên liệu '{nl.TenNguyenLieu}'?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (var db = new MenuContext())
                    {
                        var item = db.NguyenLieus.Find(nl.Id);
                        if (item != null) db.NguyenLieus.Remove(item);
                        db.SaveChanges();
                    }
                    Task.Run(() => LoadDataAsync());
                }
            });

            // [TỐI ƯU] Tự động cập nhật kho mỗi 30s (để đồng bộ khi Bếp trừ kho)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += (s, e) => Task.Run(() => LoadDataAsync());
            _timer.Start();
        }

        // [QUAN TRỌNG] Hàm load dữ liệu chạy ngầm
        private async void LoadDataAsync()
        {
            try
            {
                // 1. Lấy dữ liệu ở Background Thread
                var list = await Task.Run(() =>
                {
                    using (var db = new MenuContext())
                    {
                        return db.NguyenLieus.OrderBy(x => x.TenNguyenLieu).ToList();
                    }
                });

                // 2. Cập nhật UI ở Main Thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DanhSachNguyenLieu = new ObservableCollection<NguyenLieu>(list);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi Load Kho: " + ex.Message);
            }
        }

        // [QUAN TRỌNG] Hàm dọn dẹp Timer
        public void Cleanup()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Views.Admin;

namespace OrMan.ViewModels.Admin
{
    public class KhoViewModel : INotifyPropertyChanged
    {
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
            LoadData();

            AddCommand = new RelayCommand<object>((p) =>
            {
                var wd = new ThemNguyenLieuWindow();
                if (wd.ShowDialog() == true && wd.Result != null)
                {
                    using (var db = new MenuContext())
                    {
                        db.NguyenLieus.Add(wd.Result);
                        db.SaveChanges();
                    }
                    LoadData(); // Reload lại list
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
                        // Cập nhật vào DB
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
                    LoadData();
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
                    LoadData();
                }
            });
        }

        private void LoadData()
        {
            using (var db = new MenuContext())
            {
                // Load danh sách từ DB
                var list = db.NguyenLieus.OrderBy(x => x.TenNguyenLieu).ToList();
                DanhSachNguyenLieu = new ObservableCollection<NguyenLieu>(list);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
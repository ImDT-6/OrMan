using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Views.Admin;
using Microsoft.EntityFrameworkCore;

namespace OrMan.ViewModels.Admin
{
    public class KhoViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer;
        private bool _isLoading;
        private string _searchText;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                // Tự động tìm kiếm khi gõ phím
                _ = LoadDataAsync();
            }
        }

        private ObservableCollection<NguyenLieu> _danhSachNguyenLieu;
        public ObservableCollection<NguyenLieu> DanhSachNguyenLieu
        {
            get => _danhSachNguyenLieu;
            set { _danhSachNguyenLieu = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        public KhoViewModel()
        {
            _ = LoadDataAsync();

            AddCommand = new RelayCommand<object>(async (p) =>
            {
                var wd = new ThemNguyenLieuWindow();
                if (wd.ShowDialog() == true && wd.Result != null)
                {
                    using (var db = new MenuContext())
                    {
                        db.NguyenLieus.Add(wd.Result);
                        await db.SaveChangesAsync();
                    }
                    await LoadDataAsync();
                }
            });

            EditCommand = new RelayCommand<NguyenLieu>(async (nl) =>
            {
                if (nl == null) return;
                var wd = new ThemNguyenLieuWindow(nl);
                if (wd.ShowDialog() == true)
                {
                    using (var db = new MenuContext())
                    {
                        var item = await db.NguyenLieus.FindAsync(nl.Id);
                        if (item != null)
                        {
                            item.TenNguyenLieu = wd.Result.TenNguyenLieu;
                            item.DonViTinh = wd.Result.DonViTinh;
                            item.GiaVon = wd.Result.GiaVon;
                            item.SoLuongTon = wd.Result.SoLuongTon;
                            item.DinhMucToiThieu = wd.Result.DinhMucToiThieu;
                            await db.SaveChangesAsync();
                        }
                    }
                    await LoadDataAsync();
                }
            });

            DeleteCommand = new RelayCommand<NguyenLieu>(async (nl) =>
            {
                if (MessageBox.Show($"Xác nhận xóa nguyên liệu '{nl.TenNguyenLieu}'?", "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using (var db = new MenuContext())
                    {
                        var item = await db.NguyenLieus.FindAsync(nl.Id);
                        if (item != null) db.NguyenLieus.Remove(item);
                        await db.SaveChangesAsync();
                    }
                    await LoadDataAsync();
                }
            });

            RefreshCommand = new RelayCommand<object>(async (p) => await LoadDataAsync());

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += async (s, e) => await LoadDataAsync();
            _timer.Start();
        }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                using (var db = new MenuContext())
                {
                    var query = db.NguyenLieus.AsNoTracking().AsQueryable();

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        string search = SearchText.ToLower();
                        query = query.Where(x => x.TenNguyenLieu.ToLower().Contains(search) ||
                                               x.Id.ToString().Contains(search));
                    }

                    var list = await query.OrderBy(x => x.TenNguyenLieu).ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DanhSachNguyenLieu = new ObservableCollection<NguyenLieu>(list);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi Load Kho: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

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
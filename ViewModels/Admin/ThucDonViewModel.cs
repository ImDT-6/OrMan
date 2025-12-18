using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Cần thiết
using System.Windows;
using System.Windows.Input;
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Services;
using OrMan.Views.Admin;
using static OrMan.Views.Admin.ThemSuaMonWindow;

namespace OrMan.ViewModels.Admin
{
    public class ThucDonViewModel : INotifyPropertyChanged
    {
        private readonly ThucDonRepository _repository;
        private ObservableCollection<MonAn> _danhSachGoc;
        private string _tuKhoaTimKiem;
        private string _currentTabTag = "Mì Cay";

        public ICommand EditCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand AddCommand { get; private set; }
        public ICommand ToggleSoldOutCommand { get; private set; }

        // Biến để Binding lên UI thay vì gọi hàm GetFilteredList
        private ObservableCollection<MonAn> _danhSachHienThi;
        public ObservableCollection<MonAn> DanhSachHienThi
        {
            get => _danhSachHienThi;
            set { _danhSachHienThi = value; OnPropertyChanged(); }
        }

        public ThucDonViewModel()
        {
            _repository = new ThucDonRepository();

            // [TỐI ƯU] Load dữ liệu bất đồng bộ
            Task.Run(() => LoadDataFromDbAsync());

            DeleteCommand = new RelayCommand<MonAn>(DeleteMonAn);
            EditCommand = new RelayCommand<MonAn>(EditMonAn);
            AddCommand = new RelayCommand<object>(AddMonAn);
            ToggleSoldOutCommand = new RelayCommand<MonAn>(ToggleSoldOut);
        }

        private async Task LoadDataFromDbAsync()
        {
            var data = await Task.Run(() =>
            {
                var list = _repository.GetAll();
                if (list.Count == 0)
                {
                    AddSampleData(); // Hàm này sync nhưng chạy 1 lần đầu thì chấp nhận được
                    list = _repository.GetAll();
                }
                return list;
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                _danhSachGoc = data;
                RefeshList(); // Cập nhật lại list hiển thị
            });
        }

        private void AddSampleData()
        {
            _repository.Add(new MonMiCay("MC001", "Mì Cay Bò Mỹ", 65000, "Bò", 1, 7) { HinhAnhUrl = "/Images/mi_bo.png" });
        }

        private void ToggleSoldOut(MonAn mon)
        {
            if (mon == null) return;
            // DB Update chạy nhanh nên có thể để sync, hoặc bọc Task.Run nếu muốn
            Task.Run(() =>
            {
                _repository.Update(mon);
                Application.Current.Dispatcher.Invoke(() => RefeshList());
            });
        }

        private void DeleteMonAn(MonAn mon)
        {
            if (mon == null) return;

            if (MessageBox.Show($"Xác nhận xóa món '{mon.TenMon}'? Thao tác này không thể hoàn tác!", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Task.Run(() =>
                {
                    try
                    {
                        _repository.Delete(mon);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _danhSachGoc.Remove(mon);
                            RefeshList();
                            MessageBox.Show("Xóa món thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show($"Lỗi khi xóa món: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });
            }
        }

        private void AddMonAn(object obj)
        {
            var window = new ThemSuaMonWindow(null);
            if (window.ShowDialog() == true && window.MonAnResult != null)
            {
                var monMoi = window.MonAnResult;
                if (_danhSachGoc != null && _danhSachGoc.Any(x => x.MaMon == monMoi.MaMon))
                {
                    MessageBox.Show("Mã món này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Task.Run(() =>
                {
                    _repository.Add(monMoi);
                    SaveRecipe(monMoi.MaMon, window.ListCongThucResult);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _danhSachGoc.Add(monMoi);
                        RefeshList();
                    });
                });
            }
        }

        private void EditMonAn(MonAn mon)
        {
            var window = new ThemSuaMonWindow(mon);
            if (window.ShowDialog() == true)
            {
                mon.TenMon = window.MonAnResult.TenMon;
                mon.GiaBan = window.MonAnResult.GiaBan;
                mon.DonViTinh = window.MonAnResult.DonViTinh;
                mon.HinhAnhUrl = window.MonAnResult.HinhAnhUrl;

                Task.Run(() =>
                {
                    _repository.Update(mon);
                    SaveRecipe(mon.MaMon, window.ListCongThucResult);
                    Application.Current.Dispatcher.Invoke(() => RefeshList());
                });
            }
        }

        private void SaveRecipe(string maMon, List<CongThucDTO> listDTO)
        {
            using (var db = new MenuContext())
            {
                var oldList = db.CongThucs.Where(x => x.MaMon == maMon);
                db.CongThucs.RemoveRange(oldList);
                foreach (var dto in listDTO)
                {
                    var ct = new CongThuc { MaMon = maMon, NguyenLieuId = dto.NguyenLieuId, SoLuongCan = dto.SoLuongCan };
                    db.CongThucs.Add(ct);
                }
                db.SaveChanges();
            }
        }

        public string TuKhoaTimKiem
        {
            get => _tuKhoaTimKiem;
            set { _tuKhoaTimKiem = value; OnPropertyChanged(); RefeshList(); }
        }

        public void SetCurrentTab(string tabTag)
        {
            _currentTabTag = tabTag;
            RefeshList();
        }

        // Logic lọc dữ liệu (In-Memory)
        private void RefeshList()
        {
            if (_danhSachGoc == null) return;

            var query = _danhSachGoc.AsEnumerable();

            if (_currentTabTag == "Mì Cay") query = query.Where(x => x is MonMiCay);
            else query = query.OfType<MonPhu>().Where(x => x.TheLoai == _currentTabTag);

            if (!string.IsNullOrEmpty(TuKhoaTimKiem))
            {
                string k = TuKhoaTimKiem.ToLower();
                query = query.Where(x => x.TenMon.ToLower().Contains(k) || x.MaMon.ToLower().Contains(k));
            }

            DanhSachHienThi = new ObservableCollection<MonAn>(query);
        }

        // Để tương thích với code cũ nếu View gọi GetFilteredList (Optional)
        public ObservableCollection<MonAn> GetFilteredList() => DanhSachHienThi;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
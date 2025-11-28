using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;
using GymManagement.Views.Admin;

namespace GymManagement.ViewModels.Admin
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

        public ThucDonViewModel()
        {
            _repository = new ThucDonRepository();
            LoadDataFromDb();

            DeleteCommand = new RelayCommand<MonAn>(DeleteMonAn);
            EditCommand = new RelayCommand<MonAn>(EditMonAn);
            AddCommand = new RelayCommand<object>(AddMonAn);
            ToggleSoldOutCommand = new RelayCommand<MonAn>(ToggleSoldOut);
        }

        private void LoadDataFromDb()
        {
            _danhSachGoc = _repository.GetAll();
            if (_danhSachGoc.Count == 0)
            {
                AddSampleData();
                // [FIX LỖI] Reload data sau khi thêm mẫu để đảm bảo dữ liệu mới được load lại
                _danhSachGoc = _repository.GetAll();
            }
        }

        private void AddSampleData()
        {
            _repository.Add(new MonMiCay("MC001", "Mì Cay Bò Mỹ", 65000, "Bò", 1, 7) { HinhAnhUrl = "/Images/mi_bo.png" });
        }

        private void ToggleSoldOut(MonAn mon)
        {
            if (mon == null) return;

            // [SỬA QUAN TRỌNG]
            // Vì CheckBox đã tự đổi trạng thái IsSoldOut trên giao diện rồi,
            // nên ở đây ta chỉ cần gọi Update để lưu trạng thái đó xuống DB.
            // Không gọi ToggleSoldOut (đảo ngược) nữa.
            _repository.Update(mon);

            OnPropertyChanged("DataUpdated");
        }

        private void DeleteMonAn(MonAn mon)
        {
            if (mon == null) return;

            if (MessageBox.Show($"Xác nhận xóa món '{mon.TenMon}'? Thao tác này không thể hoàn tác!", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // 1. Xóa trong DB
                    _repository.Delete(mon);
                    // 2. Xóa khỏi danh sách gốc trong bộ nhớ
                    _danhSachGoc.Remove(mon);
                    // 3. Cập nhật lại UI
                    RefeshList();
                    MessageBox.Show("Xóa món thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa món: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddMonAn(object obj)
        {
            var window = new ThemSuaMonWindow(null);
            if (window.ShowDialog() == true && window.MonAnResult != null)
            {
                var monMoi = window.MonAnResult;
                if (_danhSachGoc.Any(x => x.MaMon == monMoi.MaMon))
                {
                    MessageBox.Show("Mã món này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _repository.Add(monMoi);
                _danhSachGoc.Add(monMoi);
                RefeshList();
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
                _repository.Update(mon);
                RefeshList();
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

        public ObservableCollection<MonAn> GetFilteredList()
        {
            if (_danhSachGoc == null) return new ObservableCollection<MonAn>();
            var query = _danhSachGoc.AsEnumerable();

            if (_currentTabTag == "Mì Cay") query = query.Where(x => x is MonMiCay);
            else query = query.OfType<MonPhu>().Where(x => x.TheLoai == _currentTabTag);

            if (!string.IsNullOrEmpty(TuKhoaTimKiem))
            {
                string k = TuKhoaTimKiem.ToLower();
                query = query.Where(x => x.TenMon.ToLower().Contains(k) || x.MaMon.ToLower().Contains(k));
            }
            return new ObservableCollection<MonAn>(query);
        }

        private void RefeshList()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DataUpdated"));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.Services;
using GymManagement.Views.Admin; // [QUAN TRỌNG] để gọi ThemSuaMonWindow

namespace GymManagement.ViewModels.Admin // Namespace đúng
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

        public ThucDonViewModel()
        {
            _repository = new ThucDonRepository();
            LoadDataFromDb();

            DeleteCommand = new RelayCommand<MonAn>(DeleteMonAn);
            EditCommand = new RelayCommand<MonAn>(EditMonAn);
            AddCommand = new RelayCommand<object>(AddMonAn);
        }

        private void LoadDataFromDb()
        {
            _danhSachGoc = _repository.GetAll();

            if (_danhSachGoc.Count == 0)
            {
                AddSampleData();
                _danhSachGoc = _repository.GetAll();
            }
        }

        private void AddSampleData()
        {
            _repository.Add(new MonMiCay("MC001", "Mì Cay Bò Mỹ", 65000, "Bò", 1, 7) { HinhAnhUrl = "/Images/mi_bo.png" });
            _repository.Add(new MonPhu("PC001", "Tokbokki", 35000, "Phần", "Đồ Chiên") { HinhAnhUrl = "/Images/tokbokki.png" });
        }

        // --- CÁC HÀM THAO TÁC (GỌI XUỐNG DB) ---

        private void DeleteMonAn(MonAn mon)
        {
            if (MessageBox.Show($"Xóa món '{mon.TenMon}'?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _repository.Delete(mon);
                _danhSachGoc.Remove(mon);
                RefeshList();
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

        // --- LOGIC LỌC ---
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;       // Thêm dòng này để dùng MessageBox
using System.Windows.Input; // Thêm dòng này để dùng ICommand
using OrMan.Data;
using OrMan.Helpers;        // Thêm dòng này để dùng RelayCommand
using OrMan.Models;
using OrMan.Views.Admin;    // Thêm dòng này để gọi Window mới tạo

namespace OrMan.ViewModels
{
    public class KhachHangViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<KhachHang> _allKhachHang;
        private ObservableCollection<KhachHang> _danhSachKhachHang;

        public ObservableCollection<KhachHang> DanhSachKhachHang
        {
            get => _danhSachKhachHang;
            set { _danhSachKhachHang = value; OnPropertyChanged(); }
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                _searchKeyword = value;
                OnPropertyChanged();
                FilterData();
            }
        }

        // --- KHAI BÁO COMMAND MỚI ---
        public ICommand EditCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public KhachHangViewModel()
        {
            LoadData();

            // 1. LOGIC SỬA
            // 1. LOGIC SỬA (ĐÃ CẬP NHẬT)
            EditCommand = new RelayCommand<KhachHang>((k) =>
            {
                if (k == null) return;

                // Mở cửa sổ sửa
                var wd = new ThemKhachHangWindow(k);
                if (wd.ShowDialog() == true)
                {
                    using (var db = new MenuContext())
                    {
                        var itemToUpdate = db.KhachHangs.Find(k.KhachHangID);
                        if (itemToUpdate != null)
                        {
                            // [MỚI] Kiểm tra nếu Số điện thoại thay đổi
                            if (itemToUpdate.SoDienThoai != wd.Result.SoDienThoai)
                            {
                                // Kiểm tra xem số mới này đã có ai dùng chưa?
                                bool daTonTai = db.KhachHangs.Any(x => x.SoDienThoai == wd.Result.SoDienThoai);

                                if (daTonTai)
                                {
                                    MessageBox.Show($"Số điện thoại '{wd.Result.SoDienThoai}' đã tồn tại cho khách hàng khác!",
                                                    "Lỗi trùng lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return; // Dừng lại, không lưu
                                }

                                // Nếu không trùng thì cập nhật số mới
                                itemToUpdate.SoDienThoai = wd.Result.SoDienThoai;
                            }

                            // Cập nhật tên
                            itemToUpdate.HoTen = wd.Result.HoTen;

                            db.SaveChanges();
                        }
                    }
                    LoadData(); // Tải lại danh sách
                    MessageBox.Show("Cập nhật thành công!", "Thông báo");
                }
            });

            // 2. LOGIC XÓA
            DeleteCommand = new RelayCommand<KhachHang>((k) =>
            {
                if (k == null) return;

                var confirm = MessageBox.Show($"Bạn có chắc muốn xóa khách hàng '{k.HoTen}'?\nĐiểm tích lũy sẽ bị mất vĩnh viễn.",
                                              "Xác nhận xóa",
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    using (var db = new MenuContext())
                    {
                        var itemToDelete = db.KhachHangs.Find(k.KhachHangID);
                        if (itemToDelete != null)
                        {
                            db.KhachHangs.Remove(itemToDelete);
                            db.SaveChanges();
                        }
                    }
                    LoadData(); // Tải lại danh sách
                }
            });
        }

        private void LoadData()
        {
            using (var db = new MenuContext())
            {
                var list = db.KhachHangs.OrderByDescending(k => k.DiemTichLuy).ToList();
                _allKhachHang = new ObservableCollection<KhachHang>(list);
                FilterData(); // Gọi Filter để gán vào DanhSachKhachHang
            }
        }

        private void FilterData()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                DanhSachKhachHang = _allKhachHang;
            }
            else
            {
                var keyword = SearchKeyword.ToLower();
                var filtered = _allKhachHang.Where(k =>
                    k.SoDienThoai.Contains(keyword) ||
                    (k.HoTen != null && k.HoTen.ToLower().Contains(keyword))
                );
                DanhSachKhachHang = new ObservableCollection<KhachHang>(filtered);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
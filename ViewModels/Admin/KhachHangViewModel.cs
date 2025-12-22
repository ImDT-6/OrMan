using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows; // Thêm cái này để dùng MessageBox
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Views.Admin;

namespace OrMan.ViewModels
{
    public class KhachHangViewModel : INotifyPropertyChanged
    {
        // --- 1. KHAI BÁO BIẾN & PROPERTY ---
        private ObservableCollection<KhachHang> _allKhachHang;
        private ObservableCollection<KhachHang> _danhSachKhachHang;
        private string _searchKeyword;

        public ObservableCollection<KhachHang> DanhSachKhachHang
        {
            get => _danhSachKhachHang;
            set { _danhSachKhachHang = value; OnPropertyChanged(); }
        }

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

        // [QUAN TRỌNG] Phải khai báo Command ở đây thì bên dưới mới dùng được
        public RelayCommand<KhachHang> EditCommand { get; set; }
        public RelayCommand<KhachHang> DeleteCommand { get; set; }

        // --- 2. CONSTRUCTOR (HÀM KHỞI TẠO) ---
        public KhachHangViewModel()
        {
            LoadData();

            // --- LOGIC SỬA (Edit) ---
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
                            // Kiểm tra nếu Số điện thoại thay đổi
                            if (itemToUpdate.SoDienThoai != wd.Result.SoDienThoai)
                            {
                                // Kiểm tra trùng SĐT
                                bool daTonTai = db.KhachHangs.Any(x => x.SoDienThoai == wd.Result.SoDienThoai);
                                if (daTonTai)
                                {
                                    MessageBox.Show($"Số điện thoại '{wd.Result.SoDienThoai}' đã tồn tại!",
                                        "Lỗi trùng lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
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

            // --- LOGIC XÓA (Delete) ---
            DeleteCommand = new RelayCommand<KhachHang>((k) =>
            {
                if (k == null) return;

                var confirm = MessageBox.Show($"Bạn có chắc muốn xóa khách hàng '{k.HoTen}'?\nĐiểm tích lũy sẽ bị mất vĩnh viễn.",
                                              "Xác nhận xóa",
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
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
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                    }
                }
            });
        }

        // --- 3. CÁC HÀM HỖ TRỢ (LOAD, FILTER) ---
        private void LoadData()
        {
            using (var db = new MenuContext())
            {
                // Load dữ liệu mới nhất từ DB
                var list = db.KhachHangs.OrderByDescending(k => k.DiemTichLuy).ToList();
                _allKhachHang = new ObservableCollection<KhachHang>(list);
                FilterData();
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

        // --- 4. INotifyPropertyChanged ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
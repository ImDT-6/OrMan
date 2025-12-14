using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using OrMan.Data;
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

        public KhachHangViewModel()
        {
            // Khởi tạo các Command
            // Lưu ý: Cần có class RelayCommand trong project
            EditCommand = new RelayCommand<KhachHang>(ExecuteEdit);
            DeleteCommand = new RelayCommand<KhachHang>(ExecuteDelete);

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

        // 2. Xử lý logic Sửa
        private void ExecuteEdit(KhachHang khachHang)
        {
            if (khachHang == null) return;

            // Ví dụ: Mở window sửa (Bạn cần thay thế bằng Window thực tế của bạn)
            // var editWindow = new ThemSuaKhachHangWindow(khachHang);
            // if (editWindow.ShowDialog() == true) 
            // {
            //     LoadData(); // Load lại dữ liệu nếu có thay đổi
            // }

            MessageBox.Show($"Tính năng sửa đang phát triển cho khách: {khachHang.HoTen}", "Thông báo");
        }

        // 3. Xử lý logic Xóa
        private void ExecuteDelete(KhachHang khachHang)
        {
            if (khachHang == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa khách hàng: {khachHang.HoTen}?\nHành động này không thể hoàn tác.",
                                         "Xác nhận xóa",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new MenuContext())
                    {
                        // Tìm đối tượng trong DB để xóa (Tránh lỗi context tracking)
                        var itemToDelete = db.KhachHangs.FirstOrDefault(k => k.SoDienThoai == khachHang.SoDienThoai);
                        if (itemToDelete != null)
                        {
                            db.KhachHangs.Remove(itemToDelete);
                            db.SaveChanges();

                            // Xóa trên giao diện (UI) để không phải load lại toàn bộ DB
                            _allKhachHang.Remove(khachHang);
                            FilterData(); // Cập nhật lại danh sách hiển thị

                            MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
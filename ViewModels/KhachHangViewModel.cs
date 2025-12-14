using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;           // Thêm thư viện cho MessageBox
using System.Windows.Input;     // Thêm thư viện cho ICommand
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;

namespace OrMan.ViewModels
{
    public class KhachHangViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<KhachHang> _allKhachHang; // Danh sách gốc
        private ObservableCollection<KhachHang> _danhSachKhachHang; // Danh sách hiển thị (đã lọc)

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
                FilterData(); // Tìm kiếm ngay khi gõ
            }
        }

        // 1. Khai báo Command
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }

        public KhachHangViewModel()
        {
            // Khởi tạo các Command
            // Lưu ý: Cần có class RelayCommand trong project
            EditCommand = new RelayCommand<KhachHang>(ExecuteEdit);
            DeleteCommand = new RelayCommand<KhachHang>(ExecuteDelete);

            LoadData();
        }

        private void LoadData()
        {
            using (var db = new MenuContext())
            {
                // Lấy danh sách từ DB, sắp xếp theo điểm cao xuống thấp
                var list = db.KhachHangs.OrderByDescending(k => k.DiemTichLuy).ToList();
                _allKhachHang = new ObservableCollection<KhachHang>(list);
                DanhSachKhachHang = _allKhachHang;
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
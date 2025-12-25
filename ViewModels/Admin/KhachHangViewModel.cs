using System; // Thêm
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using OrMan.Data;
using OrMan.Helpers;
using OrMan.Models;
using OrMan.Views.Admin;

namespace OrMan.ViewModels
{
    public class KhachHangViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<KhachHang> _allKhachHang;
        private ObservableCollection<KhachHang> _danhSachKhachHang;
        private string _searchKeyword;

        // [MỚI] Biến chứa số lượng thành viên mới
        private int _soLuongThanhVienMoi;

        public ObservableCollection<KhachHang> DanhSachKhachHang
        {
            get => _danhSachKhachHang;
            set { _danhSachKhachHang = value; OnPropertyChanged(); }
        }

        // [MỚI] Property binding ra giao diện
        public int SoLuongThanhVienMoi
        {
            get => _soLuongThanhVienMoi;
            set { _soLuongThanhVienMoi = value; OnPropertyChanged(); }
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

        public RelayCommand<KhachHang> EditCommand { get; set; }
        public RelayCommand<KhachHang> DeleteCommand { get; set; }

        public KhachHangViewModel()
        {
            LoadData();

            // --- Logic Edit (Giữ nguyên của bạn) ---
            EditCommand = new RelayCommand<KhachHang>((k) =>
            {
                if (k == null) return;
                var wd = new ThemKhachHangWindow(k);
                if (wd.ShowDialog() == true)
                {
                    using (var db = new MenuContext())
                    {
                        var itemToUpdate = db.KhachHangs.Find(k.KhachHangID);
                        if (itemToUpdate != null)
                        {
                            if (itemToUpdate.SoDienThoai != wd.Result.SoDienThoai)
                            {
                                bool daTonTai = db.KhachHangs.Any(x => x.SoDienThoai == wd.Result.SoDienThoai);
                                if (daTonTai)
                                {
                                    MessageBox.Show($"SĐT '{wd.Result.SoDienThoai}' đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                                itemToUpdate.SoDienThoai = wd.Result.SoDienThoai;
                            }
                            itemToUpdate.HoTen = wd.Result.HoTen;
                            db.SaveChanges();
                        }
                    }
                    LoadData();
                    MessageBox.Show("Cập nhật thành công!", "Thông báo");
                }
            });

            // --- Logic Delete (Giữ nguyên của bạn) ---
            DeleteCommand = new RelayCommand<KhachHang>((k) =>
            {
                if (k == null) return;
                var confirm = MessageBox.Show($"Xóa khách hàng '{k.HoTen}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new MenuContext())
                        {
                            var item = db.KhachHangs.Find(k.KhachHangID);
                            if (item != null) { db.KhachHangs.Remove(item); db.SaveChanges(); }
                        }
                        LoadData();
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
                }
            });
        }

        private void LoadData()
        {
            using (var db = new MenuContext())
            {
                var list = db.KhachHangs.OrderByDescending(k => k.DiemTichLuy).ToList();
                _allKhachHang = new ObservableCollection<KhachHang>(list);

                // --- [MỚI] LOGIC TÍNH TOÁN THÀNH VIÊN MỚI ---
                // Tính những người tham gia trong 7 ngày gần nhất
                var bayNgayTruoc = DateTime.Now.Date.AddDays(-7);
                SoLuongThanhVienMoi = list.Count(x => x.NgayThamGia >= bayNgayTruoc);

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
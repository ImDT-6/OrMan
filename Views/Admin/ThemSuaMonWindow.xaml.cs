using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OrMan.Data;
using OrMan.Models;
namespace OrMan.Views.Admin
{
    public partial class ThemSuaMonWindow : Window
    {
        // Class phụ để hiển thị trên danh sách
        public class CongThucDTO
        {
            public int NguyenLieuId { get; set; }
            public string TenNguyenLieu { get; set; }
            public double SoLuongCan { get; set; }
            public string DonViTinh { get; set; }
        }
        public MonAn MonAnResult { get; private set; }

        // [MỚI] Danh sách công thức để trả về cho ViewModel lưu
        public List<CongThucDTO> ListCongThucResult { get; private set; } = new List<CongThucDTO>();
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // Chỉ cho chọn file ảnh
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                // Gán đường dẫn file đã chọn vào TextBox
                txtHinhAnh.Text = openFileDialog.FileName;
            }
        }
        public ThemSuaMonWindow(MonAn monAnEdit = null)
        {
            InitializeComponent();
            LoadNguyenLieu();

            if (monAnEdit != null)
            {
                // CHẾ ĐỘ SỬA
                txtTitle.Text = "CHỈNH SỬA MÓN";
                txtMaMon.Text = monAnEdit.MaMon;
                txtMaMon.IsEnabled = false;
                txtTenMon.Text = monAnEdit.TenMon;
                txtGiaBan.Text = monAnEdit.GiaBan.ToString("F0");
                txtDonVi.Text = monAnEdit.DonViTinh;
                txtHinhAnh.Text = monAnEdit.HinhAnhUrl;
                cboLoaiMon.IsEnabled = false;

                // Disable ComboBox khi sửa để tránh lỗi đổi loại lung tung
                cboLoaiMon.IsEnabled = false;

                // Set loại món tương ứng vào ComboBox
                if (monAnEdit is MonMiCay) cboLoaiMon.SelectedIndex = 0;
                else if (monAnEdit is MonPhu p && p.TheLoai == "Đồ Chiên") cboLoaiMon.SelectedIndex = 1;
                else cboLoaiMon.SelectedIndex = 2;

                // [MỚI] Load công thức cũ của món này
                LoadCongThucCu(monAnEdit.MaMon);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenMon.Text) || string.IsNullOrWhiteSpace(txtGiaBan.Text))
            {
                MessageBox.Show("Vui lòng nhập tên món và giá bán!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtGiaBan.Text, out decimal giaBan))
            {
                MessageBox.Show("Giá bán phải là số!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Lấy loại món từ ComboBox
            var selectedItem = cboLoaiMon.SelectedItem as ComboBoxItem;
            string loaiTag = selectedItem?.Tag.ToString();

            if (loaiTag == "Mì Cay")
            {
                // Tạo món Mì Cay (Mặc định level 0-7)
                MonAnResult = new MonMiCay(txtMaMon.Text, txtTenMon.Text, giaBan, "Mì Hàn Quốc", 0, 7)
                {
                    DonViTinh = txtDonVi.Text,
                    HinhAnhUrl = txtHinhAnh.Text
                };
            }
            else
            {
                // Tạo món phụ (Đồ Chiên hoặc Nước)
                MonAnResult = new MonPhu(txtMaMon.Text, txtTenMon.Text, giaBan, txtDonVi.Text, loaiTag)
                {
                    HinhAnhUrl = txtHinhAnh.Text
                };
            }

            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void txtMaMon_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        // [MỚI] Hàm load danh sách nguyên liệu vào ComboBox
        private void LoadNguyenLieu()
        {
            using (var db = new MenuContext())
            {
                var list = db.NguyenLieus.ToList();
                cboNguyenLieu.ItemsSource = list;
            }
        }

        // [MỚI] Hàm load công thức cũ
        private void LoadCongThucCu(string maMon)
        {
            using (var db = new MenuContext())
            {
                var listCT = db.CongThucs.Where(x => x.MaMon == maMon).ToList();
                foreach (var ct in listCT)
                {
                    var nl = db.NguyenLieus.Find(ct.NguyenLieuId);
                    if (nl != null)
                    {
                        ListCongThucResult.Add(new CongThucDTO
                        {
                            NguyenLieuId = nl.Id,
                            TenNguyenLieu = nl.TenNguyenLieu,
                            SoLuongCan = ct.SoLuongCan,
                            DonViTinh = nl.DonViTinh
                        });
                    }
                }
                lvCongThuc.ItemsSource = null;
                lvCongThuc.ItemsSource = ListCongThucResult;
            }
        }

        // [MỚI] Nút thêm nguyên liệu vào list tạm
        private void BtnAddIngredient_Click(object sender, RoutedEventArgs e)
        {
            var selectedNL = cboNguyenLieu.SelectedItem as NguyenLieu;
            if (selectedNL == null) return;

            if (double.TryParse(txtSoLuongNL.Text, out double sl))
            {
                // Kiểm tra xem đã có trong list chưa, nếu có thì cộng dồn
                var existing = ListCongThucResult.FirstOrDefault(x => x.NguyenLieuId == selectedNL.Id);
                if (existing != null)
                {
                    existing.SoLuongCan += sl;
                }
                else
                {
                    ListCongThucResult.Add(new CongThucDTO
                    {
                        NguyenLieuId = selectedNL.Id,
                        TenNguyenLieu = selectedNL.TenNguyenLieu,
                        SoLuongCan = sl,
                        DonViTinh = selectedNL.DonViTinh
                    });
                }

                // Refresh ListView
                lvCongThuc.ItemsSource = null;
                lvCongThuc.ItemsSource = ListCongThucResult;
            }
        }

        // [MỚI] Xóa dòng nguyên liệu (Context Menu)
        // [ĐÃ SỬA] Xóa dòng nguyên liệu (Dùng Button trực tiếp)
        private void BtnRemoveIngredient_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                // Lấy dữ liệu (CongThucDTO) từ DataContext của dòng chứa nút bấm
                var dto = btn.DataContext as CongThucDTO;

                if (dto != null)
                {
                    ListCongThucResult.Remove(dto);

                    // Refresh lại list
                    lvCongThuc.ItemsSource = null;
                    lvCongThuc.ItemsSource = ListCongThucResult;
                }
            }
        }
    }

}
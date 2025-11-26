using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using Microsoft.Win32;
namespace GymManagement.Views.Admin
{
    public partial class ThemSuaMonWindow : Window
    {
        public MonAn MonAnResult { get; private set; }
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

                // Disable ComboBox khi sửa để tránh lỗi đổi loại lung tung
                cboLoaiMon.IsEnabled = false;

                // Set loại món tương ứng vào ComboBox
                if (monAnEdit is MonMiCay) cboLoaiMon.SelectedIndex = 0;
                else if (monAnEdit is MonPhu p && p.TheLoai == "Đồ Chiên") cboLoaiMon.SelectedIndex = 1;
                else cboLoaiMon.SelectedIndex = 2;
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
                // Tạo món phụ (Đồ chiên hoặc Nước)
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
    }
}
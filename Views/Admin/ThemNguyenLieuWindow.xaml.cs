using System.Windows;
using OrMan.Models;

namespace OrMan.Views.Admin
{
    public partial class ThemNguyenLieuWindow : Window
    {
        public NguyenLieu Result { get; private set; }

        public ThemNguyenLieuWindow(NguyenLieu editItem = null)
        {
            InitializeComponent();
            if (editItem != null)
            {
                lblTitle.Text = "CẬP NHẬT NGUYÊN LIỆU";
                txtTen.Text = editItem.TenNguyenLieu;
                txtDVT.Text = editItem.DonViTinh;
                txtGia.Text = editItem.GiaVon.ToString("F0");
                txtTonKho.Text = editItem.SoLuongTon.ToString();
                txtMin.Text = editItem.DinhMucToiThieu.ToString();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtTen.Text)) return;

            decimal.TryParse(txtGia.Text, out decimal gia);
            double.TryParse(txtTonKho.Text, out double ton);
            double.TryParse(txtMin.Text, out double min);

            Result = new NguyenLieu
            {
                TenNguyenLieu = txtTen.Text,
                DonViTinh = txtDVT.Text,
                GiaVon = gia,
                SoLuongTon = ton,
                DinhMucToiThieu = min
            };

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
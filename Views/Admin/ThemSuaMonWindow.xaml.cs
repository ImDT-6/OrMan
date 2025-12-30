using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OrMan.Data;
using OrMan.Models;
using Microsoft.EntityFrameworkCore;

namespace OrMan.Views.Admin
{
    public partial class ThemSuaMonWindow : Window
    {
        public class CongThucDTO
        {
            public int NguyenLieuId { get; set; }
            public string TenNguyenLieu { get; set; }
            public double SoLuongCan { get; set; }
            public string DonViTinh { get; set; }
        }
        public MonAn MonAnResult { get; private set; }

        public List<CongThucDTO> ListCongThucResult { get; private set; } = new List<CongThucDTO>();

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                txtHinhAnh.Text = openFileDialog.FileName;
            }
        }

        public ThemSuaMonWindow(MonAn monAnEdit = null)
        {
            InitializeComponent();
            // load async to avoid UI blocking
            _ = LoadNguyenLieuAsync();

            if (monAnEdit != null)
            {
                txtTitle.Text = "CHỈNH SỬA MÓN";
                txtMaMon.Text = monAnEdit.MaMon;
                txtMaMon.IsEnabled = false;
                txtTenMon.Text = monAnEdit.TenMon;
                txtGiaBan.Text = monAnEdit.GiaBan.ToString("F0");
                txtDonVi.Text = monAnEdit.DonViTinh;
                txtHinhAnh.Text = monAnEdit.HinhAnhUrl;
                cboLoaiMon.IsEnabled = false;
                cboLoaiMon.IsEnabled = false;

                if (monAnEdit is MonMiCay) cboLoaiMon.SelectedIndex = 0;
                else if (monAnEdit is MonPhu p && p.TheLoai == "Đồ Chiên") cboLoaiMon.SelectedIndex = 1;
                else cboLoaiMon.SelectedIndex = 2;

                _ = LoadCongThucCuAsync(monAnEdit.MaMon);
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

            var selectedItem = cboLoaiMon.SelectedItem as ComboBoxItem;
            string loaiTag = selectedItem?.Tag.ToString();

            if (loaiTag == "Mì Cay")
            {
                MonAnResult = new MonMiCay(txtMaMon.Text, txtTenMon.Text, giaBan, "Mì Hàn Quốc", 0, 7)
                {
                    DonViTinh = txtDonVi.Text,
                    HinhAnhUrl = txtHinhAnh.Text
                };
            }
            else
            {
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

        private async Task LoadNguyenLieuAsync()
        {
            var list = await Task.Run(() =>
            {
                using (var db = new MenuContext())
                {
                    // cần using Microsoft.EntityFrameworkCore để có AsNoTracking()
                    return db.NguyenLieus.AsNoTracking().ToList();
                }
            });

            cboNguyenLieu.ItemsSource = list;
        }

        private async Task LoadCongThucCuAsync(string maMon)
        {
            var dtos = await Task.Run(() =>
            {
                var result = new List<CongThucDTO>();
                using (var db = new MenuContext())
                {
                    var listCT = db.CongThucs.Where(x => x.MaMon == maMon).ToList();
                    var nlIds = listCT.Select(c => c.NguyenLieuId).Distinct().ToList();
                    var nguyenDict = db.NguyenLieus.Where(n => nlIds.Contains(n.Id)).ToDictionary(n => n.Id);

                    foreach (var ct in listCT)
                    {
                        if (nguyenDict.TryGetValue(ct.NguyenLieuId, out var nl))
                        {
                            result.Add(new CongThucDTO
                            {
                                NguyenLieuId = nl.Id,
                                TenNguyenLieu = nl.TenNguyenLieu,
                                SoLuongCan = ct.SoLuongCan,
                                DonViTinh = nl.DonViTinh
                            });
                        }
                    }
                }
                return result;
            });

            ListCongThucResult = dtos;
            lvCongThuc.ItemsSource = null;
            lvCongThuc.ItemsSource = ListCongThucResult;
        }

        private void BtnAddIngredient_Click(object sender, RoutedEventArgs e)
        {
            var selectedNL = cboNguyenLieu.SelectedItem as NguyenLieu;
            if (selectedNL == null) return;

            if (double.TryParse(txtSoLuongNL.Text, out double sl))
            {
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

                lvCongThuc.ItemsSource = null;
                lvCongThuc.ItemsSource = ListCongThucResult;
            }
        }

        private void BtnRemoveIngredient_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                var dto = btn.DataContext as CongThucDTO;
                if (dto != null)
                {
                    ListCongThucResult.Remove(dto);
                    lvCongThuc.ItemsSource = null;
                    lvCongThuc.ItemsSource = ListCongThucResult;
                }
            }
        }
    }

}
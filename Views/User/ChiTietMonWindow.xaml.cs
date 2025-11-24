using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;

namespace GymManagement.Views.User
{
    public partial class ChiTietMonWindow : Window
    {
        private MonAn _monAn;
        private int _soLuong = 1;
        private int _capDoCay = 0;

        public int SoLuong => _soLuong;
        public string GhiChu => txtNote.Text;
        public int CapDoCay => _capDoCay;

        public ChiTietMonWindow(MonAn monAn)
        {
            InitializeComponent();
            _monAn = monAn;
            this.DataContext = monAn;

            UpdateTotalButton();

            // Nếu là Mì Cay -> Hiện 7 cấp độ
            if (monAn is MonMiCay)
            {
                var levels = new List<string>();
                for (int i = 0; i <= 7; i++) levels.Add($"Cấp {i}");
                LevelItemsControl.ItemsSource = levels;
            }
            else
            {
                PnlCapDoCay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnTang_Click(object sender, RoutedEventArgs e)
        {
            _soLuong++;
            txtSoLuong.Text = _soLuong.ToString();
            UpdateTotalButton();
        }

        private void BtnGiam_Click(object sender, RoutedEventArgs e)
        {
            if (_soLuong > 1)
            {
                _soLuong--;
                txtSoLuong.Text = _soLuong.ToString();
                UpdateTotalButton();
            }
        }

        private void UpdateTotalButton()
        {
            decimal total = _monAn.GiaBan * _soLuong;
            btnConfirm.Content = $"Thêm vào giỏ - {total:N0} đ";
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Content != null)
            {
                string content = rb.Content.ToString();
                int.TryParse(content.Replace("Cấp ", ""), out _capDoCay);
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
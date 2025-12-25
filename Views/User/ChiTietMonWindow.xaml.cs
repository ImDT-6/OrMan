using System.Collections.Generic;
using System.Text.RegularExpressions; // [CẦN THÊM] Để dùng Regex
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OrMan.Models;

namespace OrMan.Views.User
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

            // Gọi lại lần nữa ở đây để đảm bảo nút hiển thị đúng giá sau khi _monAn đã có dữ liệu
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

        // [LOGIC CŨ] Nút Tăng Giảm vẫn hoạt động tốt với TextBox
        private void BtnTang_Click(object sender, RoutedEventArgs e)
        {
            _soLuong++;
            txtSoLuong.Text = _soLuong.ToString(); // Dòng này sẽ kích hoạt sự kiện TextChanged bên dưới
        }

        private void BtnGiam_Click(object sender, RoutedEventArgs e)
        {
            if (_soLuong > 1)
            {
                _soLuong--;
                txtSoLuong.Text = _soLuong.ToString();
            }
        }

        // [MỚI] Chỉ cho phép nhập số
        private void TxtSoLuong_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text, "[0-9]+");
        }

        // [MỚI] Khi nhập số -> Tự động cập nhật biến _soLuong và Tổng tiền
        private void TxtSoLuong_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSoLuong.Text))
            {
                _soLuong = 0; // Tạm thời
                // Không cập nhật nút vội, đợi người dùng nhập xong hoặc LostFocus
                return;
            }

            if (int.TryParse(txtSoLuong.Text, out int result))
            {
                _soLuong = result;
                UpdateTotalButton();
            }
        }

        // [MỚI] Khi người dùng bấm ra ngoài -> Nếu ô trống hoặc = 0 thì reset về 1
        private void TxtSoLuong_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_soLuong <= 0)
            {
                _soLuong = 1;
                txtSoLuong.Text = "1";
                UpdateTotalButton();
            }
        }

        private void UpdateTotalButton()
        {
            // [FIX QUAN TRỌNG] Kiểm tra null để tránh lỗi khi InitializeComponent kích hoạt TextChanged trước khi _monAn được gán
            if (_monAn == null) return;

            decimal total = _monAn.GiaBan * (_soLuong > 0 ? _soLuong : 0);
            string template = Application.Current.TryFindResource("Str_Btn_AddToCart") as string
                       ?? "Thêm vào giỏ - {0:N0} đ";

            // Disable nút thêm nếu số lượng = 0
            if (btnConfirm != null) // Kiểm tra null cho an toàn
            {
                btnConfirm.Content = string.Format(template, total);
                btnConfirm.IsEnabled = _soLuong > 0;
                btnConfirm.Opacity = _soLuong > 0 ? 1 : 0.5;
            }
        }

        // ... (Các hàm khác giữ nguyên: RadioButton_Checked, BtnConfirm_Click...) ...

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

        private void NoteBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtNote.Focus();
            txtNote.CaretIndex = txtNote.Text.Length;
        }
    }
}
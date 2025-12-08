using System.Windows;
using System.Windows.Input;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class TichDiemWindow : Window
    {
        private UserViewModel _vm;

        public TichDiemWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            txtPhone.Focus(); // Focus ngay vào ô nhập
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            PerformCheck();
        }

        private void txtPhone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformCheck();
            }
        }

        private void PerformCheck()
        {
            string phone = txtPhone.Text.Trim();
            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            {
                MessageBox.Show("Vui lòng nhập số điện thoại hợp lệ!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Gọi ViewModel để check database
            var khach = _vm.CheckMember(phone);

            // Hiển thị kết quả lên giao diện
            pnlResult.Visibility = Visibility.Visible;
            lblTenKhach.Text = $"Xin chào: {khach.HoTen}";
            lblHang.Text = khach.HangThanhVien;
            lblDiem.Text = khach.DiemTichLuy.ToString("N0") + " điểm";

            // Ẩn nút check đi, đổi thành nút Xong
            btnCheck.Content = "HOÀN TẤT";
            btnCheck.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#22C55E"); // Màu xanh lá
            btnCheck.Click -= BtnCheck_Click;
            btnCheck.Click += (s, ev) => this.Close();
        }
    }
}
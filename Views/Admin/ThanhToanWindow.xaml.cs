using System.Windows;

namespace GymManagement.Views.Admin
{
    public partial class ThanhToanWindow : Window
    {
        public ThanhToanWindow(string tenBan, decimal tongTien)
        {
            InitializeComponent();

            // Hiển thị thông tin truyền vào
            txtTenBan.Text = tenBan;
            txtTongTien.Text = $"{tongTien:N0} VNĐ";
        }

        private void Method_Click(object sender, RoutedEventArgs e)
        {
            if (pnlTienMat == null || pnlChuyenKhoan == null) return;

            if (radTienMat.IsChecked == true)
            {
                pnlTienMat.Visibility = Visibility.Visible;
                pnlChuyenKhoan.Visibility = Visibility.Collapsed;
            }
            else
            {
                pnlTienMat.Visibility = Visibility.Collapsed;
                pnlChuyenKhoan.Visibility = Visibility.Visible;
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Trả về True để báo là đã thanh toán xong
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
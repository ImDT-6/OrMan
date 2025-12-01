using System.Windows;

namespace GymManagement.Views.User
{
    public enum RequestType { None, Support, Checkout }

    public partial class SupportRequestWindow : Window
    {
        public RequestType SelectedRequest { get; private set; } = RequestType.None;
        public string SupportMessage { get; private set; } = ""; // Lưu lời nhắn

        private bool _hasActiveOrder = false;

        public SupportRequestWindow(bool hasActiveOrder)
        {
            InitializeComponent();
            _hasActiveOrder = hasActiveOrder;

            // Nếu chưa có đơn (Khách mới vào), ẩn phần thanh toán
            if (!_hasActiveOrder)
            {
                pnlCheckout.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSupport_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.Support;
            SupportMessage = cboMessage.Text; // Lấy nội dung khách nhập hoặc chọn

            if (string.IsNullOrWhiteSpace(SupportMessage))
                SupportMessage = "Cần hỗ trợ"; // Mặc định

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.Checkout;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
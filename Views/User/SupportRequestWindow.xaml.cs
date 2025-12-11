using System.Windows;

namespace OrMan.Views.User
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

        

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // [MỚI] Biến lưu phương thức khách chọn
        public string SelectedPaymentMethod { get; private set; } = "Tiền mặt";

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.Checkout;

            // Kiểm tra xem khách chọn gì
            if (radTienMat.IsChecked == true) SelectedPaymentMethod = "Tiền mặt";
            else if (radCK.IsChecked == true) SelectedPaymentMethod = "Chuyển khoản";
            else if (radThe.IsChecked == true) SelectedPaymentMethod = "Thẻ";

            this.DialogResult = true;
            this.Close();
        }
    }
}
using System.Windows;
using System.Windows.Controls;

namespace OrMan.Views.User
{
    public enum RequestType { None, Support, Checkout }

    public partial class SupportRequestWindow : Window
    {
        // Properties để lưu kết quả trả về
        public RequestType SelectedRequest { get; private set; } = RequestType.None;
        public string SupportMessage { get; private set; } = "";
        public string SelectedPaymentMethod { get; private set; } = "Tiền mặt";

        private bool _hasActiveOrder = false;

        public SupportRequestWindow(bool hasActiveOrder)
        {
            InitializeComponent();
            _hasActiveOrder = hasActiveOrder;

            // Xử lý giao diện: Nếu khách mới vào (chưa có đơn), ẩn phần thanh toán
            if (!_hasActiveOrder)
            {
                // Ẩn nút thanh toán
                if (BtnCheckout != null) BtnCheckout.Visibility = Visibility.Collapsed;

                // Ẩn các lựa chọn phương thức
                if (radTienMat != null) radTienMat.Visibility = Visibility.Collapsed;
                if (radCK != null) radCK.Visibility = Visibility.Collapsed;
                if (radThe != null) radThe.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSupport_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.Support;

            // Lấy nội dung từ ComboBox (Text là phần người dùng nhập hoặc chọn)
            SupportMessage = cboMessage.Text;

            if (string.IsNullOrWhiteSpace(SupportMessage))
            {
                SupportMessage = "Cần hỗ trợ"; // Nội dung mặc định nếu để trống
            }

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.Checkout;

            // Xác định phương thức thanh toán khách chọn
            if (radTienMat.IsChecked == true)
                SelectedPaymentMethod = "Tiền mặt";
            else if (radCK.IsChecked == true)
                SelectedPaymentMethod = "Chuyển khoản";
            else if (radThe.IsChecked == true)
                SelectedPaymentMethod = "Thẻ";

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.None;
            this.DialogResult = false;
            this.Close();
        }
    }
}
using System.Windows;

namespace GymManagement.Views.User
{
    // Enum để xác định loại yêu cầu mà User chọn
    public enum RequestType { None, Support, Checkout }

    public partial class SupportRequestWindow : Window
    {
        public RequestType SelectedRequest { get; private set; } = RequestType.None;
        private bool _hasActiveOrder = false;

        public SupportRequestWindow(bool hasActiveOrder)
        {
            InitializeComponent();
            _hasActiveOrder = hasActiveOrder;

            // Ẩn nút Thanh toán nếu chưa có đơn đang hoạt động
            if (!_hasActiveOrder)
            {
                BtnCheckout.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSupport_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.Support;
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
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

        // [ĐÃ SỬA] Không gán cứng "Tiền mặt" ở đây nữa, sẽ gán khi click nút
        public string SelectedPaymentMethod { get; private set; } = "";

        private bool _hasActiveOrder = false;

        public SupportRequestWindow(bool hasActiveOrder)
        {
            InitializeComponent();
            _hasActiveOrder = hasActiveOrder;

            // Xử lý giao diện: Nếu khách mới vào (chưa có đơn), ẩn phần thanh toán
            if (!_hasActiveOrder)
            {
                if (BtnCheckout != null) BtnCheckout.Visibility = Visibility.Collapsed;
                if (radTienMat != null) radTienMat.Visibility = Visibility.Collapsed;
                if (radCK != null) radCK.Visibility = Visibility.Collapsed;
                if (radThe != null) radThe.Visibility = Visibility.Collapsed;
            }

            // [MỚI] Gán mặc định phương thức thanh toán là Tiền mặt (theo ngôn ngữ hiện tại)
            SelectedPaymentMethod = GetRes("Str_Pay_Cash");
        }

        // [Hàm hỗ trợ] Lấy chuỗi từ Resource
        private string GetRes(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }

        private void BtnSupport_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.Support;

            // Lấy nội dung từ ComboBox
            SupportMessage = cboMessage.Text;

            if (string.IsNullOrWhiteSpace(SupportMessage))
            {
                // [ĐÃ SỬA] Lấy nội dung mặc định từ Resource
                SupportMessage = GetRes("Str_Msg_DefaultSupport");
            }

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            SelectedRequest = RequestType.Checkout;

            // [ĐÃ SỬA] Xác định phương thức thanh toán theo Resource đa ngôn ngữ
            if (radTienMat.IsChecked == true)
                SelectedPaymentMethod = GetRes("Str_Pay_Cash");
            else if (radCK.IsChecked == true)
                SelectedPaymentMethod = GetRes("Str_Pay_Transfer");
            else if (radThe.IsChecked == true)
                SelectedPaymentMethod = GetRes("Str_Pay_Card");

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
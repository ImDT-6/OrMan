using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// Bỏ bớt các using thừa liên quan đến FlowDocument
using OrMan.Models;

namespace OrMan.Views.Admin
{
    public partial class HoaDonWindow : Window
    {
        // Cấu hình ngân hàng
        private const string BANK_ID = "MB";
        private const string ACCOUNT_NO = "06618706666";
        private const string ACCOUNT_NAME = "TRAN DUC TRONG";
        private const string TEMPLATE = "qr_only";

        private string _hinhThucThanhToan;
        private string _tenBan;

        public HoaDonWindow(string tenBan, HoaDon hoaDon, List<ChiTietHoaDon> chiTiet, string hinhThucThanhToan)
        {
            InitializeComponent();

            _hinhThucThanhToan = hinhThucThanhToan;
            _tenBan = tenBan;

            // 1. Gán dữ liệu cơ bản
            txtBan.Text = tenBan;
            if (!string.IsNullOrEmpty(hoaDon.MaHoaDon) && hoaDon.MaHoaDon.Length >= 8)
                txtMaHD.Text = $"HD: #{hoaDon.MaHoaDon.Substring(0, 8).ToUpper()}";
            else
                txtMaHD.Text = $"HD: #{hoaDon.MaHoaDon}";

            txtNgay.Text = hoaDon.NgayTao.ToString("dd/MM/yyyy HH:mm");

            // --- Xử lý hiển thị tiền nong ---
            decimal tongTienHang = hoaDon.TongTien;
            decimal giamGia = hoaDon.GiamGia;
            decimal thucTra = tongTienHang - giamGia;

            txtTongTienHang.Text = tongTienHang.ToString("N0") + " đ";

            if (giamGia > 0)
            {
                txtGiamGia.Text = "-" + giamGia.ToString("N0") + " đ";
                gridGiamGia.Visibility = Visibility.Visible;
            }
            else
            {
                gridGiamGia.Visibility = Visibility.Collapsed;
                txtGiamGia.Text = "0";
            }

            txtThanhToan.Text = thucTra.ToString("N0") + " đ";
            txtHinhThuc.Text = hinhThucThanhToan;
            ListMonAn.ItemsSource = chiTiet;

            // 2. Xử lý QR Code
            if (_hinhThucThanhToan == "Chuyển khoản")
            {
                if (pnlQR != null) pnlQR.Visibility = Visibility.Visible;

                string content = Uri.EscapeDataString($"{tenBan} TT");
                string qrUrl = $"https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-{TEMPLATE}.png?amount={(long)thucTra}&addInfo={content}&accountName={Uri.EscapeDataString(ACCOUNT_NAME)}";

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(qrUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    if (imgQR != null) imgQR.Source = bitmap;
                }
                catch
                {
                    if (pnlQR != null) pnlQR.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (pnlQR != null) pnlQR.Visibility = Visibility.Collapsed;
            }
        }

        // [LOGIC IN ĐƠN GIẢN HÓA] In trực tiếp những gì đang hiển thị (WYSIWYG)
        private void BtnIn_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                try
                {
                    // 1. Tắt hiệu ứng bóng đổ tạm thời để bản in sắc nét hơn (tùy chọn)
                    var originalEffect = BillArea.Effect;
                    BillArea.Effect = null;

                    // 2. Tự động co giãn (Scale) để vừa khổ giấy máy in
                    // Lấy chiều rộng vùng in khả dụng của máy in (đơn vị pixel)
                    double printableWidth = printDialog.PrintableAreaWidth;

                    // Tính tỷ lệ: Nếu hóa đơn to hơn giấy -> Thu nhỏ. Nếu nhỏ hơn -> Giữ nguyên (hoặc phóng to nếu muốn)
                    // BillArea.ActualWidth là chiều rộng thực tế trên màn hình
                    double scale = Math.Min(printableWidth / BillArea.ActualWidth, 1.0);

                    // Áp dụng biến hình (Transform) cho BillArea trước khi in
                    BillArea.LayoutTransform = new ScaleTransform(scale, scale);

                    // 3. Thực hiện lệnh in Visual (In đúng đối tượng BillArea)
                    printDialog.PrintVisual(BillArea, "Hóa Đơn " + _tenBan);

                    // 4. Khôi phục lại trạng thái cũ sau khi in xong
                    BillArea.LayoutTransform = null; // Bỏ scale
                    BillArea.Effect = originalEffect; // Trả lại hiệu ứng bóng đổ

                    // Đóng cửa sổ và báo thành công
                    this.DialogResult = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Có lỗi xảy ra khi in: {ex.Message}", "Lỗi In Ấn", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
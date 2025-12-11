using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging; // Quan trọng: Để xử lý ảnh Bitmap
using OrMan.Models;

namespace OrMan.Views.Admin
{
    public partial class HoaDonWindow : Window
    {
        // Cấu hình tài khoản ngân hàng nhận tiền
        private const string BANK_ID = "MB";            // Ngân hàng MBBank
        private const string ACCOUNT_NO = "06618706666"; // Số tài khoản
        private const string ACCOUNT_NAME = "TRAN DUC TRONG"; // Tên tài khoản
        private const string TEMPLATE = "qr_only";     // Mẫu QR gọn

        // Constructor nhận thêm tham số 'hinhThucThanhToan'
        public HoaDonWindow(string tenBan, HoaDon hoaDon, List<ChiTietHoaDon> chiTiet, string hinhThucThanhToan)
        {
            InitializeComponent();

            // 1. Gán dữ liệu cơ bản lên giao diện
            txtBan.Text = tenBan;

            // Xử lý mã hóa đơn cho ngắn gọn
            if (!string.IsNullOrEmpty(hoaDon.MaHoaDon) && hoaDon.MaHoaDon.Length >= 8)
                txtMaHD.Text = $"HD: #{hoaDon.MaHoaDon.Substring(0, 8).ToUpper()}";
            else
                txtMaHD.Text = $"HD: #{hoaDon.MaHoaDon}";

            txtNgay.Text = hoaDon.NgayTao.ToString("dd/MM/yyyy HH:mm");
            txtTongTien.Text = hoaDon.TongTien.ToString("N0") + " VNĐ";

            // Hiển thị hình thức thanh toán (Tiền mặt / Chuyển khoản...)
            txtHinhThuc.Text = hinhThucThanhToan;

            // Đổ danh sách món ăn vào ItemsControl
            ListMonAn.ItemsSource = chiTiet;

            // 2. Xử lý Logic QR Code Động
            if (hinhThucThanhToan == "Chuyển khoản")
            {
                // Hiện khung chứa QR
                if (pnlQR != null) pnlQR.Visibility = Visibility.Visible;

                // Tạo nội dung chuyển khoản: "BAN 01 TT" (Nên bỏ dấu tiếng Việt để tránh lỗi)
                string content = Uri.EscapeDataString($"{tenBan} TT");

                // Tạo link API VietQR
                string qrUrl = $"https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-{TEMPLATE}.png?amount={hoaDon.TongTien}&addInfo={content}&accountName={Uri.EscapeDataString(ACCOUNT_NAME)}";
                try
                {
                    // Tải ảnh từ URL
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(qrUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Tải xong mới hiển thị
                    bitmap.EndInit();

                    if (imgQR != null) imgQR.Source = bitmap;
                }
                catch
                {
                    // Nếu lỗi mạng, ẩn QR đi để không hiện khung trống
                    if (pnlQR != null) pnlQR.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Nếu là Tiền mặt hoặc Thẻ -> Ẩn QR
                if (pnlQR != null) pnlQR.Visibility = Visibility.Collapsed;
            }


        }

        private void BtnIn_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Chuẩn bị in: Tắt bóng đổ và margin để in sát lề
                BillArea.Effect = null;
                BillArea.Margin = new Thickness(0);

                // Lệnh in vùng BillArea
                printDialog.PrintVisual(BillArea, "Hoa Don Thanh Toan");

                // In xong thì đóng cửa sổ và báo thành công
                this.DialogResult = true;
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
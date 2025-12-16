using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;           // [QUAN TRỌNG] Thêm dòng này để dùng FontFamily
using System.Windows.Media.Imaging;   // Để xử lý ảnh Bitmap (QR Code)
using System.Windows.Documents;       // [QUAN TRỌNG] Để dùng FlowDocument, Paragraph...
using System.Windows.Markup;          // Để dùng XamlWriter (nếu cần)
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
                string qrUrl = $"https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-{TEMPLATE}.png?amount={(long)hoaDon.TongTien}&addInfo={content}&accountName={Uri.EscapeDataString(ACCOUNT_NAME)}";
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
                // [CÁCH MỚI] Tạo FlowDocument để in sắc nét
                FlowDocument doc = new FlowDocument();
                doc.PagePadding = new Thickness(10);
                doc.ColumnWidth = printDialog.PrintableAreaWidth; // Tự chỉnh theo khổ giấy

                // [ĐÃ SỬA] Dòng này sẽ hết lỗi vì đã có using System.Windows.Media
                doc.FontFamily = new FontFamily("Arial");

                // 1. Tiêu đề
                Paragraph title = new Paragraph(new Run("HÓA ĐƠN THANH TOÁN"));
                title.FontSize = 16;
                title.FontWeight = FontWeights.Bold;
                title.TextAlignment = TextAlignment.Center;
                doc.Blocks.Add(title);

                // 2. Thông tin chung
                Paragraph info = new Paragraph();
                info.FontSize = 12;
                info.Inlines.Add(new Run($"Bàn: {txtBan.Text}\n"));
                info.Inlines.Add(new Run($"{txtMaHD.Text}\n"));
                info.Inlines.Add(new Run($"Ngày: {txtNgay.Text}"));
                info.TextAlignment = TextAlignment.Left;
                doc.Blocks.Add(info);

                // 3. Kẻ ngang
                doc.Blocks.Add(new Paragraph(new Run("--------------------------------")) { TextAlignment = TextAlignment.Center });

                // 4. Danh sách món (Lấy từ ItemsSource của ListBox)
                var listMon = ListMonAn.ItemsSource as IEnumerable<OrMan.Models.ChiTietHoaDon>;
                if (listMon != null)
                {
                    Table table = new Table();
                    // Định nghĩa 3 cột: Tên, SL, Tiền
                    table.Columns.Add(new TableColumn() { Width = new GridLength(3, GridUnitType.Star) }); // Tên rộng nhất
                    table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // SL
                    table.Columns.Add(new TableColumn() { Width = new GridLength(1.5, GridUnitType.Star) }); // Thành tiền

                    TableRowGroup group = new TableRowGroup();
                    foreach (var item in listMon)
                    {
                        TableRow row = new TableRow();
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.TenMonHienThi))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.SoLuong.ToString())) { TextAlignment = TextAlignment.Center }));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.ThanhTien.ToString("N0"))) { TextAlignment = TextAlignment.Right }));
                        group.Rows.Add(row);
                    }
                    table.RowGroups.Add(group);
                    doc.Blocks.Add(table);
                }

                // 5. Kẻ ngang & Tổng tiền
                doc.Blocks.Add(new Paragraph(new Run("--------------------------------")) { TextAlignment = TextAlignment.Center });
                Paragraph total = new Paragraph();
                total.FontSize = 14;
                total.FontWeight = FontWeights.Bold;
                total.Inlines.Add(new Run("TỔNG CỘNG: " + txtTongTien.Text));
                total.TextAlignment = TextAlignment.Right;
                doc.Blocks.Add(total);

                // 6. Footer
                Paragraph footer = new Paragraph(new Run("Cảm ơn quý khách & Hẹn gặp lại!"));
                footer.FontSize = 10;
                footer.FontStyle = FontStyles.Italic;
                footer.TextAlignment = TextAlignment.Center;
                doc.Blocks.Add(footer);

                // In Document thay vì in Visual
                IDocumentPaginatorSource idpSource = doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "Hoa Don Ban " + txtBan.Text);

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
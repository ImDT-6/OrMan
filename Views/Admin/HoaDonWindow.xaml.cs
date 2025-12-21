using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Windows.Markup;
using OrMan.Models;
using System.IO; // Cần thêm để xử lý MemoryStream cho ảnh QR

namespace OrMan.Views.Admin
{
    public partial class HoaDonWindow : Window
    {
        // Cấu hình ngân hàng
        private const string BANK_ID = "MB";
        private const string ACCOUNT_NO = "06618706666";
        private const string ACCOUNT_NAME = "TRAN DUC TRONG";
        private const string TEMPLATE = "qr_only";

        // [MỚI] Biến lưu hình thức thanh toán để dùng khi bấm nút In
        private string _hinhThucThanhToan;
        private string _tenBan; // Lưu lại tên bàn để in tiêu đề

        public HoaDonWindow(string tenBan, HoaDon hoaDon, List<ChiTietHoaDon> chiTiet, string hinhThucThanhToan)
        {
            InitializeComponent();

            // Lưu lại giá trị
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

            // 2. Xử lý QR Code trên màn hình xem trước (UI)
            // Chỉ hiện QR nếu là "Chuyển khoản"
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
                // [QUAN TRỌNG] Nếu không phải CK thì ẩn đi
                if (pnlQR != null) pnlQR.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnIn_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument();
                doc.PagePadding = new Thickness(10);
                doc.ColumnWidth = printDialog.PrintableAreaWidth;
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
                info.Inlines.Add(new Run($"Bàn: {_tenBan}\n"));
                info.Inlines.Add(new Run($"{txtMaHD.Text}\n"));
                info.Inlines.Add(new Run($"Ngày: {txtNgay.Text}"));
                info.TextAlignment = TextAlignment.Left;
                doc.Blocks.Add(info);

                // 3. Kẻ ngang
                doc.Blocks.Add(new Paragraph(new Run("--------------------------------")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0) });

                // 4. Danh sách món
                var listMon = ListMonAn.ItemsSource as IEnumerable<OrMan.Models.ChiTietHoaDon>;
                if (listMon != null)
                {
                    Table table = new Table();
                    table.CellSpacing = 0;
                    table.Columns.Add(new TableColumn() { Width = new GridLength(3, GridUnitType.Star) });
                    table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                    table.Columns.Add(new TableColumn() { Width = new GridLength(1.5, GridUnitType.Star) });

                    TableRowGroup group = new TableRowGroup();
                    foreach (var item in listMon)
                    {
                        TableRow row = new TableRow();
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.TenMonHienThi))) { Padding = new Thickness(0, 2, 0, 2) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.SoLuong.ToString())) { TextAlignment = TextAlignment.Center }) { Padding = new Thickness(0, 2, 0, 2) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.ThanhTien.ToString("N0"))) { TextAlignment = TextAlignment.Right }) { Padding = new Thickness(0, 2, 0, 2) });
                        group.Rows.Add(row);
                    }
                    table.RowGroups.Add(group);
                    doc.Blocks.Add(table);
                }

                // 5. Footer tính tiền
                doc.Blocks.Add(new Paragraph(new Run("--------------------------------")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0) });

                Paragraph pTong = new Paragraph();
                pTong.Inlines.Add(new Run("Tổng tiền:   "));
                pTong.Inlines.Add(new Run(txtTongTienHang.Text));
                pTong.TextAlignment = TextAlignment.Right;
                pTong.Margin = new Thickness(0, 5, 0, 0);
                doc.Blocks.Add(pTong);

                if (gridGiamGia.Visibility == Visibility.Visible)
                {
                    Paragraph pGiam = new Paragraph();
                    pGiam.Inlines.Add(new Run("Voucher:    "));
                    pGiam.Inlines.Add(new Run(txtGiamGia.Text) { FontStyle = FontStyles.Italic });
                    pGiam.TextAlignment = TextAlignment.Right;
                    pGiam.Margin = new Thickness(0);
                    doc.Blocks.Add(pGiam);
                }

                Paragraph pFinal = new Paragraph();
                pFinal.FontSize = 14;
                pFinal.FontWeight = FontWeights.Bold;
                pFinal.Inlines.Add(new Run("THANH TOÁN: " + txtThanhToan.Text));
                pFinal.TextAlignment = TextAlignment.Right;
                pFinal.Margin = new Thickness(0, 5, 0, 10);
                doc.Blocks.Add(pFinal);

                // --- [MỚI] 6. IN MÃ QR CODE VÀO BILL (CHỈ KHI CHUYỂN KHOẢN) ---
                if (_hinhThucThanhToan == "Chuyển khoản" && imgQR.Source != null)
                {
                    // Kẻ ngang ngăn cách
                    doc.Blocks.Add(new Paragraph(new Run("- - - - - - - - - - - - - - - -")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10, 0, 0) });

                    Paragraph pQRHeader = new Paragraph(new Run("QUÉT MÃ ĐỂ THANH TOÁN"));
                    pQRHeader.FontSize = 10;
                    pQRHeader.FontWeight = FontWeights.Bold;
                    pQRHeader.TextAlignment = TextAlignment.Center;
                    pQRHeader.Margin = new Thickness(0, 5, 0, 0);
                    doc.Blocks.Add(pQRHeader);

                    // Tạo ảnh để chèn vào FlowDocument
                    Image qrImage = new Image();
                    qrImage.Source = imgQR.Source;
                    qrImage.Width = 150; // Kích thước in ra
                    qrImage.Stretch = Stretch.Uniform;

                    BlockUIContainer imgContainer = new BlockUIContainer(qrImage);
                    doc.Blocks.Add(imgContainer); // Thêm ảnh vào văn bản in

                    Paragraph pQRInfo = new Paragraph();
                    pQRInfo.Inlines.Add(new Run($"MB Bank: {ACCOUNT_NO}\n"));
                    pQRInfo.Inlines.Add(new Run(ACCOUNT_NAME));
                    pQRInfo.FontSize = 10;
                    pQRInfo.TextAlignment = TextAlignment.Center;
                    doc.Blocks.Add(pQRInfo);
                }
                // -------------------------------------------------------------

                // 7. Lời cảm ơn
                Paragraph footer = new Paragraph(new Run("\nCảm ơn quý khách & Hẹn gặp lại!"));
                footer.FontSize = 10;
                footer.FontStyle = FontStyles.Italic;
                footer.TextAlignment = TextAlignment.Center;
                doc.Blocks.Add(footer);

                IDocumentPaginatorSource idpSource = doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "Hoa Don Ban " + _tenBan);

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
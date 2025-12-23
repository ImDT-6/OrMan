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
                // [MỚI] Cố định Pagewidth để khi in không bị thay đổi
                doc.PageWidth = 400;
                doc.ColumnWidth = 380;
                doc.FontFamily = new FontFamily("Arial");

                // 1. Tiêu đề
                Paragraph title = new Paragraph(new Run("HÓA ĐƠN THANH TOÁN"));
                title.FontSize = 16;
                title.FontWeight = FontWeights.Bold;
                title.TextAlignment = TextAlignment.Center;
                doc.Blocks.Add(title);

                // 2. Thông tin chung
                Table infoTable = new Table();
                infoTable.CellSpacing = 0;
                infoTable.FontSize = 12;
                infoTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                infoTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });

                TableRowGroup infoGroup = new TableRowGroup();

                // Dòng 1
                TableRow row1 = new TableRow();
                row1.Cells.Add(new TableCell(new Paragraph(new Run($"Mã HĐ: {txtMaHD.Text}")) { TextAlignment = TextAlignment.Left }) { Padding = new Thickness(0, 2, 0, 2) });
                row1.Cells.Add(new TableCell(new Paragraph(new Run("TN: Thu ngân Admin")) { TextAlignment = TextAlignment.Right }) { Padding = new Thickness(0, 2, 0, 2) });
                infoGroup.Rows.Add(row1);

                // Dòng 2
                TableRow row2 = new TableRow();
                row2.Cells.Add(new TableCell(new Paragraph(new Run($"{_tenBan}") { FontWeight = FontWeights.Bold}) { TextAlignment = TextAlignment.Left }) { Padding = new Thickness(0, 2, 0, 2) });
                row2.Cells.Add(new TableCell(new Paragraph(new Run($"{DateTime.Now}")) { TextAlignment = TextAlignment.Right }) { Padding = new Thickness(0, 2, 0, 2) });
                infoGroup.Rows.Add(row2);

                // Dòng 3
                TableRow row3 = new TableRow();
                row3.Cells.Add(new TableCell(new Paragraph(new Run("Giờ vào: --:--")) { TextAlignment = TextAlignment.Left }) { Padding = new Thickness(0, 2, 0, 2) });
                row3.Cells.Add(new TableCell(new Paragraph(new Run("Giờ ra: --:--")) { TextAlignment = TextAlignment.Right }) { Padding = new Thickness(0, 2, 0, 2) });
                infoGroup.Rows.Add(row3);
                infoTable.RowGroups.Add(infoGroup);
                doc.Blocks.Add(infoTable);

                // [MỚI] Hình thức thanh toán
                Paragraph phuongthuc = new Paragraph();
                phuongthuc.Inlines.Add(new Run("Hình thức thanh toán: "));
                phuongthuc.Inlines.Add(new Run(_hinhThucThanhToan) { FontWeight = FontWeights.Bold });
                phuongthuc.FontSize = 12;
                phuongthuc.TextAlignment = TextAlignment.Left;
                doc.Blocks.Add(phuongthuc);

                // 3. Danh sách món
                var listMon = ListMonAn.ItemsSource as IEnumerable<OrMan.Models.ChiTietHoaDon>;
                Table table = new Table();
                table.CellSpacing = 0;
                table.FontSize = 12;
                table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                table.Columns.Add(new TableColumn() { Width = new GridLength(3, GridUnitType.Star) });
                table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                table.Columns.Add(new TableColumn() { Width = new GridLength(1.5, GridUnitType.Star) });

                // [Mới] Thêm Header với top và bottom border
                TableRowGroup headerGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.FontWeight = FontWeights.Bold;

                TableCell headerCell1 = new TableCell(new Paragraph(new Run("STT")) { TextAlignment = TextAlignment.Left });
                headerCell1.Padding = new Thickness(0, 5, 0, 5);
                headerCell1.BorderBrush = Brushes.Black;
                headerCell1.BorderThickness = new Thickness(0, 1, 0, 1); //Tạo border trên và dưới
                headerRow.Cells.Add(headerCell1);

                TableCell headerCell2 = new TableCell(new Paragraph(new Run("Tên món")) { TextAlignment = TextAlignment.Left });
                headerCell2.Padding = new Thickness(0, 5, 0, 5);
                headerCell2.BorderBrush = Brushes.Black;
                headerCell2.BorderThickness = new Thickness(0, 1, 0, 1); //Tạo border trên và dưới
                headerRow.Cells.Add(headerCell2);

                TableCell headerCell3 = new TableCell(new Paragraph(new Run("SL")) { TextAlignment = TextAlignment.Center });
                headerCell3.Padding = new Thickness(0, 5, 0, 5);
                headerCell3.BorderBrush = Brushes.Black;
                headerCell3.BorderThickness = new Thickness(0, 1, 0, 1);
                headerRow.Cells.Add(headerCell3);

                TableCell headerCell4 = new TableCell(new Paragraph(new Run("Thành tiền")) { TextAlignment = TextAlignment.Right });
                headerCell4.Padding = new Thickness(0, 5, 0, 5);
                headerCell4.BorderBrush = Brushes.Black;
                headerCell4.BorderThickness = new Thickness(0, 1, 0, 1);
                headerRow.Cells.Add(headerCell4);

                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                if (listMon != null)
                {
                    TableRowGroup group = new TableRowGroup();
                    foreach (var item in listMon)
                    {
                        TableRow row = new TableRow();
                        row.Cells.Add(new TableCell(new Paragraph(new Run("#")) { TextAlignment = TextAlignment.Left }) { Padding = new Thickness(0, 2, 0, 2) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.TenMonHienThi))) { Padding = new Thickness(0, 2, 0, 2) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.SoLuong.ToString())) { TextAlignment = TextAlignment.Center }) { Padding = new Thickness(0, 2, 0, 2) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(item.ThanhTien.ToString("N0"))) { TextAlignment = TextAlignment.Right }) { Padding = new Thickness(0, 2, 0, 2) });
                        group.Rows.Add(row);
                    }
                    table.RowGroups.Add(group);
                    doc.Blocks.Add(table);
                }

                // 4. Footer tính tiền

                // [Mới] Đổi đường thẳng thay vì gạch nối
                Border line1 = new Border();
                line1.BorderBrush = Brushes.Black;
                line1.BorderThickness = new Thickness(0, 1, 0, 0);
                line1.Margin = new Thickness(0, 5, 0, 5);
                line1.Width = 400;

                BlockUIContainer lineContainer2 = new BlockUIContainer(line1);
                doc.Blocks.Add(lineContainer2);

                Table pTongTable = new Table();
                pTongTable.CellSpacing = 0;
                pTongTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                pTongTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                TableRowGroup pTongGroup = new TableRowGroup();

                TableRow pTongRow1 = new TableRow();
                pTongRow1.FontSize = 14;
                pTongRow1.Cells.Add(new TableCell(new Paragraph(new Run("Thành tiền:")) { TextAlignment = TextAlignment.Left }) { Padding = new Thickness(0, 2, 0, 2) });
                pTongRow1.Cells.Add(new TableCell(new Paragraph(new Run(txtTongTienHang.Text + " VNĐ") { FontWeight = FontWeights.Bold }) { TextAlignment = TextAlignment.Right }) { Padding = new Thickness(0, 2, 0, 2) });
                pTongGroup.Rows.Add(pTongRow1);

                if (gridGiamGia.Visibility == Visibility.Visible)
                {
                    TableRow pTongRow2 = new TableRow();
                    pTongRow2.FontSize = 14;
                    pTongRow2.FontStyle = FontStyles.Italic;
                    pTongRow2.Cells.Add(new TableCell(new Paragraph(new Run("Voucher / Giảm giá:")) { TextAlignment = TextAlignment.Left }) { Padding = new Thickness(0, 2, 0, 2) });
                    pTongRow2.Cells.Add(new TableCell(new Paragraph(new Run(txtGiamGia.Text + " VNĐ") { FontWeight = FontWeights.Bold }) { TextAlignment = TextAlignment.Right }) { Padding = new Thickness(0, 2, 0, 2) });
                    pTongGroup.Rows.Add(pTongRow2);
                    Paragraph pGiam = new Paragraph();
                }

                TableRow pTongRow3 = new TableRow();
                pTongRow3.FontWeight = FontWeights.Bold;
                pTongRow3.FontSize = 16;
                pTongRow3.Cells.Add(new TableCell(new Paragraph(new Run("Tổng cộng: ")) { TextAlignment = TextAlignment.Left }) { Padding = new Thickness(0, 2, 0, 2) });
                pTongRow3.Cells.Add(new TableCell(new Paragraph(new Run(txtThanhToan.Text + " VNĐ")){ TextAlignment = TextAlignment.Right }) { Padding = new Thickness(0, 2, 0, 2) });
                pTongGroup.Rows.Add(pTongRow3);
                pTongTable.RowGroups.Add(pTongGroup);

                doc.Blocks.Add(pTongTable);

                // --- [MỚI] 5. IN MÃ QR CODE VÀO BILL (CHỈ KHI CHUYỂN KHOẢN) ---
                if (_hinhThucThanhToan == "Chuyển khoản" && imgQR.Source != null)
                {
                    // Kẻ ngang ngăn cách
                    doc.Blocks.Add(new Paragraph(new Run("- - - - - - - - - - - - - - - -")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10, 0, 0) });

                    Paragraph pQRInfo = new Paragraph();
                    pQRInfo.Inlines.Add(new Run("STK: "));
                    pQRInfo.Inlines.Add(new Run($"{ACCOUNT_NO}\n") { FontWeight = FontWeights.Bold});
                    pQRInfo.Inlines.Add(new Run(ACCOUNT_NAME) { FontWeight = FontWeights.Bold });
                    pQRInfo.FontSize = 12;
                    pQRInfo.TextAlignment = TextAlignment.Center;
                    doc.Blocks.Add(pQRInfo);

                    // Tạo ảnh để chèn vào FlowDocument
                    Image qrImage = new Image();
                    qrImage.Source = imgQR.Source;
                    qrImage.Width = 150; // Kích thước in ra
                    qrImage.Stretch = Stretch.Uniform;

                    BlockUIContainer imgContainer = new BlockUIContainer(qrImage);
                    doc.Blocks.Add(imgContainer); // Thêm ảnh vào văn bản in

                    Paragraph pQRHeader = new Paragraph(new Run("Quét mã để thanh toán"));
                    pQRHeader.FontSize = 10;
                    pQRHeader.TextAlignment = TextAlignment.Center;
                    pQRHeader.Margin = new Thickness(0, 5, 0, 0);
                    doc.Blocks.Add(pQRHeader);

                    
                }
                // -------------------------------------------------------------

                // 6. Lời cảm ơn
                Paragraph footer = new Paragraph();
                footer.Inlines.Add(new Run("\nCảm ơn quý khách & Hẹn gặp lại!\n"));
                footer.Inlines.Add(new Run("I LOVE UIT\n") { FontWeight = FontWeights.Bold});
                footer.Inlines.Add(new Run("Địa chỉ: P5.02, Tòa B, UIT\n"));
                footer.Inlines.Add(new Run("Hotline: 1900 0000"));
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
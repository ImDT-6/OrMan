using ClosedXML.Excel;
using Microsoft.Win32;
using OrMan.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks; // Cần cho Task.Run
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OrMan.Views.Admin
{
    public partial class DoanhThuView : UserControl
    {
        private DoanhThuViewModel _viewModel;

        public DoanhThuView()
        {
            InitializeComponent();
            _viewModel = new DoanhThuViewModel();
            this.DataContext = _viewModel;

            this.Unloaded += DoanhThuView_Unloaded;
        }

        private void DoanhThuView_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.Cleanup();
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            this.Focus();
        }

        private async void TimeFilter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null) return;

            if (ActiveIndicator.Visibility != Visibility.Visible)
            {
                ActiveIndicator.Visibility = Visibility.Visible;
                ActiveIndicator.Opacity = 1;
            }

            int index = TimeFilterPanel.Children.IndexOf(button);
            double targetX = index * 90;

            DoubleAnimation animation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromSeconds(0.2), // Animation chạy trong 200ms
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            IndicatorTransform.BeginAnimation(TranslateTransform.XProperty, animation);

            // [FIX LAG] Đợi 250ms cho animation chạy xong rồi mới load dữ liệu
            // Việc này tách rời quá trình Render UI và Xử lý Logic, giúp nút trượt mượt mà không bị khựng
            await Task.Delay(250);

            if (_viewModel != null)
            {
                _viewModel.SelectedTimeFilter = button.Tag.ToString();
            }
        }

        private void DpTuNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpTuNgay.SelectedDate.HasValue)
            {
                DateTime tuNgay = dpTuNgay.SelectedDate.Value;
                dpDenNgay.DisplayDateStart = tuNgay;
                if (dpDenNgay.SelectedDate.HasValue && dpDenNgay.SelectedDate.Value < tuNgay)
                {
                    dpDenNgay.SelectedDate = tuNgay;
                }
            }
            else
            {
                dpDenNgay.DisplayDateStart = null;
            }
        }

        private void DpDenNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpDenNgay.SelectedDate.HasValue)
            {
                DateTime denNgay = dpDenNgay.SelectedDate.Value;
                dpTuNgay.DisplayDateEnd = denNgay;
                if (dpTuNgay.SelectedDate.HasValue && dpTuNgay.SelectedDate.Value > denNgay)
                {
                    dpTuNgay.SelectedDate = denNgay;
                }
            }
            else
            {
                dpTuNgay.DisplayDateEnd = null;
            }
        }

        private void BtnLocCustom_Click(object sender, RoutedEventArgs e)
        {
            if (dpTuNgay.SelectedDate == null || dpDenNgay.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn đầy đủ 'Từ ngày' và 'Đến ngày'!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpTuNgay.SelectedDate > dpDenNgay.SelectedDate)
            {
                MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ActiveIndicator.Visibility = Visibility.Collapsed;
            foreach (var child in TimeFilterPanel.Children)
            {
                if (child is RadioButton rb) rb.IsChecked = false;
            }

            if (_viewModel != null)
            {
                DateTime fromDate = dpTuNgay.SelectedDate.Value.Date;
                DateTime toDate = dpDenNgay.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                _viewModel.LocTheoKhoangThoiGian(fromDate, toDate);
            }
        }

        private async void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null || _viewModel.DanhSachHoaDon == null || _viewModel.DanhSachHoaDon.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel File (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                var dataToExport = _viewModel.DanhSachHoaDon;

                try
                {
                    await Task.Run(() =>
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("DoanhThu");

                            worksheet.Cell(1, 1).Value = "Mã Hóa Đơn";
                            worksheet.Cell(1, 2).Value = "Thời Gian";
                            worksheet.Cell(1, 3).Value = "Bàn";
                            worksheet.Cell(1, 4).Value = "Nhân Viên / Khách";
                            worksheet.Cell(1, 5).Value = "Tổng Tiền Hàng";
                            worksheet.Cell(1, 6).Value = "Giảm Giá";
                            worksheet.Cell(1, 7).Value = "Thực Thu";

                            var headerRange = worksheet.Range(1, 1, 1, 7);
                            headerRange.Style.Font.Bold = true;
                            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
                            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            int row = 2;
                            foreach (var item in dataToExport)
                            {
                                worksheet.Cell(row, 1).Value = item.MaHoaDon ?? "";
                                worksheet.Cell(row, 2).Value = item.NgayTao;
                                worksheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                                worksheet.Cell(row, 3).Value = $"Bàn {item.SoBan}";
                                worksheet.Cell(row, 4).Value = item.NguoiTao ?? "Khách vãng lai";

                                worksheet.Cell(row, 5).Value = item.TongTien;
                                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                                worksheet.Cell(row, 6).Value = item.GiamGia;
                                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                                if (item.GiamGia > 0)
                                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Red;

                                decimal thucThu = item.TongTien - item.GiamGia;
                                worksheet.Cell(row, 7).Value = thucThu;
                                worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                                worksheet.Cell(row, 7).Style.Font.Bold = true;

                                row++;
                            }

                            worksheet.Cell(row, 4).Value = "TỔNG CỘNG:";
                            worksheet.Cell(row, 4).Style.Font.Bold = true;

                            worksheet.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
                            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                            worksheet.Cell(row, 6).FormulaA1 = $"SUM(F2:F{row - 1})";
                            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";

                            worksheet.Cell(row, 7).FormulaA1 = $"SUM(G2:G{row - 1})";
                            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(row, 7).Style.Font.Bold = true;
                            worksheet.Cell(row, 7).Style.Fill.BackgroundColor = XLColor.Yellow;

                            worksheet.Columns().AdjustToContents();
                            workbook.SaveAs(filePath);
                        }
                    });

                    var result = MessageBox.Show("Xuất file thành công! Bạn có muốn mở file ngay không?",
                                                 "Thành công", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Có lỗi khi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
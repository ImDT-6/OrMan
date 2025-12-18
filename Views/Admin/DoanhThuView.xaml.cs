using OrMan.ViewModels;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks; // Cần cho Task.Run
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

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

            // Đăng ký sự kiện Unloaded để dọn dẹp (nếu ViewModel có Timer/Event)
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

        private void TimeFilter_Click(object sender, RoutedEventArgs e)
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
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            IndicatorTransform.BeginAnimation(TranslateTransform.XProperty, animation);

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

        // [TỐI ƯU] Chuyển thành Async để không đơ máy khi ghi file
        private async void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null || _viewModel.DanhSachHoaDon == null || _viewModel.DanhSachHoaDon.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel File (*.csv)|*.csv",
                FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                var dataToExport = _viewModel.DanhSachHoaDon; // Copy tham chiếu để đưa vào luồng phụ

                try
                {
                    // Chạy việc ghi file ở Background Thread
                    await Task.Run(() =>
                    {
                        StringBuilder csvContent = new StringBuilder();
                        csvContent.AppendLine("Mã Hóa Đơn,Thời Gian,Bàn,Nhân Viên,Tổng Tiền");

                        foreach (var item in dataToExport)
                        {
                            string ngayTao = item.NgayTao.ToString("dd/MM/yyyy HH:mm");
                            // Dùng culture Invariant để đảm bảo dấu chấm/phẩy đồng nhất
                            string tongTien = item.TongTien.ToString("N0").Replace(",", ".");
                            // Bọc trong ngoặc kép để an toàn với CSV
                            string newLine = $"\"{item.MaHoaDon}\",\"{ngayTao}\",\"Bàn {item.SoBan}\",\"{item.NguoiTao}\",\"{tongTien}\"";
                            csvContent.AppendLine(newLine);
                        }

                        File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
                    });

                    var result = MessageBox.Show("Xuất file thành công! Bạn có muốn mở file ngay không?",
                                                 "Thành công", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
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
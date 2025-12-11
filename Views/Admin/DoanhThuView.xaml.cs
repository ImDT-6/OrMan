using OrMan.ViewModels;
using System;
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
        }

        // Bỏ focus khỏi ô text khi click ra ngoài
        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            this.Focus();
        }

        // --- 1. XỬ LÝ BỘ LỌC NHANH (Tabs) ---
        private void TimeFilter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null) return;

            // Hiển thị lại thanh trượt nếu nó đang bị ẩn (do trước đó chọn Lọc tùy chỉnh)
            if (ActiveIndicator.Visibility != Visibility.Visible)
            {
                ActiveIndicator.Visibility = Visibility.Visible;
                ActiveIndicator.Opacity = 1;
            }

            // Tính toán vị trí animation
            int index = TimeFilterPanel.Children.IndexOf(button);
            double targetX = index * 90; // 90 là Width của RadioButton trong XAML

            DoubleAnimation animation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            IndicatorTransform.BeginAnimation(TranslateTransform.XProperty, animation);

            // Cập nhật ViewModel
            if (_viewModel != null)
            {
                _viewModel.SelectedTimeFilter = button.Tag.ToString();
            }
        }

        // --- 2. LOGIC RÀNG BUỘC NGÀY (Mới thêm) ---

        // Khi chọn "Từ ngày" -> Giới hạn "Đến ngày" phải lớn hơn hoặc bằng
        private void DpTuNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpTuNgay.SelectedDate.HasValue)
            {
                DateTime tuNgay = dpTuNgay.SelectedDate.Value;

                // Cài đặt ngày tối thiểu cho ô "Đến ngày"
                dpDenNgay.DisplayDateStart = tuNgay;

                // Nếu "Đến ngày" hiện tại đang nhỏ hơn "Từ ngày" vừa chọn -> Tự động sửa lại cho bằng
                if (dpDenNgay.SelectedDate.HasValue && dpDenNgay.SelectedDate.Value < tuNgay)
                {
                    dpDenNgay.SelectedDate = tuNgay;
                }
            }
            else
            {
                // Nếu xóa "Từ ngày" thì bỏ giới hạn
                dpDenNgay.DisplayDateStart = null;
            }
        }

        // Khi chọn "Đến ngày" -> Giới hạn "Từ ngày" phải nhỏ hơn hoặc bằng
        private void DpDenNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpDenNgay.SelectedDate.HasValue)
            {
                DateTime denNgay = dpDenNgay.SelectedDate.Value;

                // Cài đặt ngày tối đa cho ô "Từ ngày"
                dpTuNgay.DisplayDateEnd = denNgay;

                // Nếu "Từ ngày" hiện tại đang lớn hơn "Đến ngày" vừa chọn -> Tự động sửa lại cho bằng
                if (dpTuNgay.SelectedDate.HasValue && dpTuNgay.SelectedDate.Value > denNgay)
                {
                    dpTuNgay.SelectedDate = denNgay;
                }
            }
            else
            {
                // Nếu xóa "Đến ngày" thì bỏ giới hạn
                dpTuNgay.DisplayDateEnd = null;
            }
        }

        // --- 3. XỬ LÝ NÚT "XEM THỐNG KÊ" ---
        private void BtnLocCustom_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra tính hợp lệ
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

            // A. Xử lý Giao diện: Tắt các tab lọc nhanh
            ActiveIndicator.Visibility = Visibility.Collapsed; // Ẩn thanh tím

            foreach (var child in TimeFilterPanel.Children)
            {
                if (child is RadioButton rb) rb.IsChecked = false;
            }

            // B. Gọi ViewModel lọc dữ liệu
            if (_viewModel != null)
            {
                DateTime fromDate = dpTuNgay.SelectedDate.Value.Date; // 00:00:00
                DateTime toDate = dpDenNgay.SelectedDate.Value.Date.AddDays(1).AddTicks(-1); // 23:59:59

                _viewModel.LocTheoKhoangThoiGian(fromDate, toDate);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OrMan.Models;
using OrMan.ViewModels;
using System.Windows.Data;
using System.Globalization;

namespace OrMan.Views.Admin
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            var vm = new DashboardViewModel();
            this.DataContext = vm;

            vm.RequestNavigationToTable += (ban) =>
            {
                var adminView = FindParent<AdminView>(this);
                if (adminView != null)
                {
                    adminView.ChuyenDenBanCanXuLy(ban);
                }
            };

            this.Unloaded += DashboardView_Unloaded;
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is DashboardViewModel vm)
            {
                vm.Cleanup();
            }
        }

        // --- XỬ LÝ SLIDING FILTER ---
        private void FilterOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn)
            {
                UpdateFilterIndicator(btn);
            }
        }

        private void FilterOption_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn && btn.IsChecked == true)
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateFilterIndicator(btn)), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void UpdateFilterIndicator(RadioButton selectedButton)
        {
            if (FilterIndicator == null || FilterGrid == null) return;
            FilterIndicator.Opacity = 1;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Point relativeLocation = selectedButton.TranslatePoint(new Point(0, 0), FilterGrid);
                    DoubleAnimation widthAnimation = new DoubleAnimation
                    {
                        To = selectedButton.ActualWidth,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    FilterIndicator.BeginAnimation(WidthProperty, widthAnimation);

                    DoubleAnimation translateAnimation = new DoubleAnimation
                    {
                        To = relativeLocation.X,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    FilterIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        // --- XỬ LÝ SLIDING TAB ---
        private void FeedbackTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateIndicator();
        }

        private void FeedbackTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                UpdateIndicator();
            }
        }

        private void UpdateIndicator()
        {
            if (!(FeedbackTabControl.SelectedItem is TabItem selectedTab)) return;

            var indicator = FeedbackTabControl.Template.FindName("PART_Indicator", FeedbackTabControl) as Border;
            if (indicator == null) return;

            var transform = indicator.RenderTransform as TranslateTransform;
            if (transform == null) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Point relativeLocation = selectedTab.TranslatePoint(new Point(0, 0), FeedbackTabControl);

                    DoubleAnimation widthAnimation = new DoubleAnimation
                    {
                        To = selectedTab.ActualWidth,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    indicator.BeginAnimation(WidthProperty, widthAnimation);

                    DoubleAnimation translateAnimation = new DoubleAnimation
                    {
                        To = relativeLocation.X,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    transform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }

    // [CẬP NHẬT] Converter màu sắc: Hỗ trợ nhận cả string HOẶC đối tượng BanAn để tô màu chính xác
    public class RequestTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = "";

            // Nếu value là BanAn -> Tự suy ra loại yêu cầu
            if (value is BanAn ban)
            {
                if (ban.YeuCauThanhToan && !string.IsNullOrEmpty(ban.YeuCauHoTro)) type = "hỗn hợp";
                else if (ban.YeuCauThanhToan) type = "thanh toán";
                else if (!string.IsNullOrEmpty(ban.YeuCauHoTro)) type = "hỗ trợ";
                else type = ban.HienThiYeuCau ?? "";
            }
            else
            {
                type = value as string ?? "";
            }

            string param = parameter as string ?? "Solid";
            type = type.ToLower();

            Color baseColor;

            if (type.Contains("hỗn hợp"))
                baseColor = (Color)ColorConverter.ConvertFromString("#F59E0B"); // Cam (Vừa gọi món vừa thanh toán)
            else if (type.Contains("thanh toán") || type.Contains("payment") || type.Contains("tính tiền"))
                baseColor = (Color)ColorConverter.ConvertFromString("#10B981"); // Green
            else if (type.Contains("hỗ trợ") || type.Contains("menu") || type.Contains("gọi") || type.Contains("phục vụ"))
                baseColor = (Color)ColorConverter.ConvertFromString("#3B82F6"); // Blue
            else if (type.Contains("gấp") || type.Contains("lỗi") || type.Contains("khẩn"))
                baseColor = (Color)ColorConverter.ConvertFromString("#EF4444"); // Red
            else
                baseColor = (Color)ColorConverter.ConvertFromString("#EF4444"); // Mặc định đỏ

            if (param == "Light") baseColor.A = 40;
            else if (param == "Hover") baseColor.A = 60;

            return new SolidColorBrush(baseColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // [CẬP NHẬT] Converter Header: Dùng MultiValueConverter để fix lỗi cập nhật chậm (Fix số 2)
    public class RequestCollectionToStatusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: Danh sách (BanCanXuLy)
            // values[1]: Số lượng (BanCanXuLy.Count) -> Binding cái này để trigger update giao diện ngay lập tức

            var collection = values.FirstOrDefault() as IEnumerable<BanAn>;

            // Xử lý null hoặc rỗng
            if (collection == null || !collection.Any())
            {
                if (parameter as string == "Text") return "DANH SÁCH YÊU CẦU";
                if (parameter as string == "Brush") return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                if (parameter as string == "BrushLight") return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2094A3B8"));
                return null;
            }

            int count = collection.Count();
            bool hasPayment = false;
            bool hasSupport = false;
            bool hasExplicitUrgent = false;

            foreach (var ban in collection)
            {
                string req = (ban.HienThiYeuCau ?? "").ToLower();

                // Check kỹ cờ bool trực tiếp từ object
                if (ban.YeuCauThanhToan) hasPayment = true;
                if (!string.IsNullOrEmpty(ban.YeuCauHoTro)) hasSupport = true;

                if (req.Contains("gấp") || req.Contains("lỗi") || req.Contains("khẩn")) hasExplicitUrgent = true;
            }

            string colorCode = "#94A3B8";
            string text = "DANH SÁCH YÊU CẦU";

            // LOGIC ƯU TIÊN:
            if (hasExplicitUrgent)
            {
                colorCode = "#EF4444"; // Red
                text = "⚠️ CẦN XỬ LÝ GẤP";
            }
            else if (count > 4)
            {
                colorCode = "#EF4444"; // Red (Quá tải)
                text = $"⚠️ QUÁ TẢI ({count} YÊU CẦU)";
            }
            else if (hasPayment && hasSupport)
            {
                colorCode = "#F59E0B"; // Orange (Hỗn hợp)
                text = "🔔 DANH SÁCH HỖN HỢP";
            }
            else if (hasPayment)
            {
                colorCode = "#10B981"; // Green
                text = "🔔 YÊU CẦU THANH TOÁN";
            }
            else if (hasSupport)
            {
                colorCode = "#3B82F6"; // Blue
                text = "ℹ️ YÊU CẦU HỖ TRỢ";
            }
            else
            {
                colorCode = "#F59E0B";
                text = "📝 DANH SÁCH YÊU CẦU";
            }

            if (parameter as string == "Text") return text;

            Color color = (Color)ColorConverter.ConvertFromString(colorCode);
            if (parameter as string == "Brush") return new SolidColorBrush(color);

            if (parameter as string == "BrushLight")
            {
                color.A = 40;
                return new SolidColorBrush(color);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // [MỚI] Converter hiển thị Text thông minh cho từng dòng (Fix lỗi 1)
    public class BanAnToDisplayStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BanAn ban)
            {
                bool hasPayment = ban.YeuCauThanhToan;
                bool hasSupport = !string.IsNullOrEmpty(ban.YeuCauHoTro);

                // Ưu tiên hiển thị cả 2 nếu có
                if (hasPayment && hasSupport)
                    return $"Thanh toán & {ban.YeuCauHoTro}";

                if (hasPayment) return "Yêu cầu thanh toán";
                if (hasSupport) return ban.YeuCauHoTro;

                return ban.HienThiYeuCau; // Fallback
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
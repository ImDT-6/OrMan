using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OrMan.Helpers
{
    // 1. Chuyển đổi Null hoặc Chuỗi rỗng sang Visibility (Dùng cho Ghi chú)
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return Visibility.Collapsed;
            return Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // 2. Chuyển đổi trạng thái số (int) sang Visibility (Dùng cho nút Xong/Hoàn tác)
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int status && parameter is string target)
                return status.ToString() == target ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // 3. Đảo ngược giá trị Đúng/Sai (Dùng cho tab chuyển đổi)
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : false;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : false;
    }
}
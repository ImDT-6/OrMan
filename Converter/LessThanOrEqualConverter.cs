using System;
using System.Globalization;
using System.Windows.Data;

namespace OrMan.Helpers
{
    /// <summary>
    /// So sánh Tồn kho thực tế với Định mức tối thiểu.
    /// Trả về true nếu cần cảnh báo (Tồn <= Định mức).
    /// </summary>
    public class LessThanOrEqualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;

            // Chuyển đổi giá trị sang double để so sánh
            if (double.TryParse(values[0]?.ToString(), out double currentStock) &&
                double.TryParse(values[1]?.ToString(), out double minThreshold))
            {
                return currentStock <= minThreshold;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
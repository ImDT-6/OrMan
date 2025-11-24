using System;
using System.Globalization;
using System.Windows.Data;

namespace GymManagement.Converters // <-- Đảm bảo namespace này khớp với project
{
    public class StringIsNullOrWhiteSpaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            return string.IsNullOrWhiteSpace(s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
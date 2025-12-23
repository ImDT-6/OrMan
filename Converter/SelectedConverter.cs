using System;
using System.Globalization;
using System.Windows.Data;

namespace OrMan.Helpers
{
    public class SelectedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] là Bàn hiện tại (trong vòng lặp)
            // values[1] là SelectedBan (từ ViewModel)

            if (values.Length >= 2 && values[0] != null && values[1] != null)
            {
                // So sánh xem 2 đối tượng có phải là 1 không
                return values[0] == values[1];
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
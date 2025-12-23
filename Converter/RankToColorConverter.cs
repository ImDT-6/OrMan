using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OrMan.Helpers
{
    // Bộ chuyển đổi màu sắc dựa trên thứ hạng
    public class RankToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = (ListBoxItem)value;
            var itemsControl = ItemsControl.ItemsControlFromItemContainer(item);
            int index = itemsControl.ItemContainerGenerator.IndexFromContainer(item);

            // Màu sắc cho Top 1, 2, 3 và các hạng sau
            switch (index)
            {
                case 0: return new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Top 1: Vàng (Gold)
                case 1: return new SolidColorBrush(Color.FromRgb(192, 192, 192)); // Top 2: Bạc (Silver)
                case 2: return new SolidColorBrush(Color.FromRgb(205, 127, 50));  // Top 3: Đồng (Bronze)
                default: return new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Còn lại: Xám (#94A3B8)
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
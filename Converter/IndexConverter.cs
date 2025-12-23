using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace OrMan.Helpers
{
    // Bộ chuyển đổi để lấy số thứ tự của item trong ListBox
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Lấy ListBoxItem hiện tại
            var item = (ListBoxItem)value;
            // Tìm ListBox chứa nó
            var itemsControl = ItemsControl.ItemsControlFromItemContainer(item);
            // Lấy index và cộng thêm 1 để bắt đầu từ 1
            int index = itemsControl.ItemContainerGenerator.IndexFromContainer(item) + 1;
            return $"#{index}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
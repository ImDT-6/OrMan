using System;
using System.Globalization;
using System.Windows.Data;

namespace GymManagement.Helpers
{
    public class HeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Mặc định chiều cao biểu đồ là 220px (khớp với Border Height 350 trừ đi header/footer)
            double maxHeight = 220;
            double maxDoanhThu = 1000000;

            if (parameter is string paramStr)
            {
                // Hỗ trợ format: "MaxDoanhThu|MaxHeight" (Ví dụ: "2000000|220")
                if (paramStr.Contains("|"))
                {
                    var parts = paramStr.Split('|');
                    double.TryParse(parts[0], out maxDoanhThu);
                    if (parts.Length > 1) double.TryParse(parts[1], out maxHeight);
                }
                else
                {
                    double.TryParse(paramStr, out maxDoanhThu);
                }
            }

            if (value is double doanhThu)
            {
                if (maxDoanhThu == 0) return 0;

                // Tính toán chiều cao theo tỷ lệ
                double height = (doanhThu / maxDoanhThu) * maxHeight;

                // Giới hạn không vượt quá khung và không quá nhỏ
                if (height > maxHeight) return maxHeight;
                if (height < 2 && doanhThu > 0) return 2; // Tối thiểu 2px để thấy có đơn

                return height;
            }
            else if (value is int doanhThuInt) // Phòng trường hợp binding vào số int
            {
                return Convert((double)doanhThuInt, targetType, parameter, culture);
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
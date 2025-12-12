using System;
using System.Windows;

namespace OrMan.Helpers
{
    public static class LanguageHelper
    {
        public static void SetLanguage(string cultureCode) // "vi" hoặc "en"
        {
            var dict = new ResourceDictionary();

            // Đường dẫn này phụ thuộc vào tên Project của bạn
            // Nếu tên project là OrMan thì giữ nguyên, nếu khác thì đổi lại
            string source = $"pack://application:,,,/Languages/Lang.{cultureCode}.xaml";

            try
            {
                dict.Source = new Uri(source);

                // Xóa file ngôn ngữ cũ (nếu có) và thêm file mới
                // Cách đơn giản nhất là tìm dict cũ có chứa key đặc trưng và xóa
                // Nhưng ở đây ta sẽ Add vào MergedDictionaries của App

                // 1. Tìm và xóa dictionary ngôn ngữ cũ (nếu có)
                ResourceDictionary oldDict = null;
                foreach (var d in Application.Current.Resources.MergedDictionaries)
                {
                    // Kiểm tra xem dict này có chứa key ngôn ngữ không
                    if (d.Source != null && d.Source.OriginalString.Contains("/Languages/Lang."))
                    {
                        oldDict = d;
                        break;
                    }
                }

                if (oldDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                }

                // 2. Thêm dict mới
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đổi ngôn ngữ: " + ex.Message);
            }
        }
    }
}
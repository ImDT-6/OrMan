using System.Windows;
using System.Windows.Controls;

namespace Helpers
{
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty SyncTextWithTagProperty =
            DependencyProperty.RegisterAttached(
                "SyncTextWithTag",
                typeof(bool),
                typeof(TextBoxHelper),
                new PropertyMetadata(false, OnSyncTextWithTagChanged));

        public static bool GetSyncTextWithTag(DependencyObject obj) =>
            (bool)obj.GetValue(SyncTextWithTagProperty);

        public static void SetSyncTextWithTag(DependencyObject obj, bool value) =>
            obj.SetValue(SyncTextWithTagProperty, value);

        private static void OnSyncTextWithTagChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.TextChanged += TextBox_TextChanged;
                }
                else
                {
                    textBox.TextChanged -= TextBox_TextChanged;
                }
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Tag = textBox.Text; // Đồng bộ giá trị Text với Tag
            }
        }
    }
}
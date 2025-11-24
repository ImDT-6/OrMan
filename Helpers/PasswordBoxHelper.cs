using System.Windows;
using System.Windows.Controls;

namespace GymManagement.Helpers
{
    public static class PasswordBoxHelper
    {
        // 1. BOUND PASSWORD
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper),
                new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached("BindPassword", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnBindPasswordChanged));

        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        // 2. MONITOR & HAS TEXT
        public static readonly DependencyProperty MonitorPasswordProperty =
            DependencyProperty.RegisterAttached("MonitorPassword", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnMonitorPasswordChanged));

        public static readonly DependencyProperty HasTextProperty =
            DependencyProperty.RegisterAttached("HasText", typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata(false));

        // GETTERS & SETTERS
        public static void SetBoundPassword(DependencyObject dp, string value) => dp.SetValue(BoundPasswordProperty, value);
        public static string GetBoundPassword(DependencyObject dp) => (string)dp.GetValue(BoundPasswordProperty);
        public static void SetBindPassword(DependencyObject dp, bool value) => dp.SetValue(BindPasswordProperty, value);
        public static bool GetBindPassword(DependencyObject dp) => (bool)dp.GetValue(BindPasswordProperty);
        public static void SetMonitorPassword(DependencyObject dp, bool value) => dp.SetValue(MonitorPasswordProperty, value);
        public static bool GetMonitorPassword(DependencyObject dp) => (bool)dp.GetValue(MonitorPasswordProperty);
        public static void SetHasText(DependencyObject dp, bool value) => dp.SetValue(HasTextProperty, value);
        public static bool GetHasText(DependencyObject dp) => (bool)dp.GetValue(HasTextProperty);

        // HANDLERS
        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                if (!(bool)passwordBox.GetValue(UpdatingPasswordProperty))
                {
                    passwordBox.Password = e.NewValue?.ToString() ?? string.Empty;
                }
                passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }
        }

        private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            if (dp is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                if ((bool)e.NewValue) passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }
        }

        private static void OnMonitorPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                if ((bool)e.NewValue)
                {
                    SetHasText(passwordBox, passwordBox.Password.Length > 0);
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                }
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetHasText(passwordBox, passwordBox.Password.Length > 0);
                if (GetBindPassword(passwordBox))
                {
                    passwordBox.SetValue(UpdatingPasswordProperty, true);
                    SetBoundPassword(passwordBox, passwordBox.Password);
                    passwordBox.SetValue(UpdatingPasswordProperty, false);
                }
            }
        }
    }
}
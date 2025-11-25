using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GymManagement.Models;
using GymManagement.ViewModels;

namespace GymManagement.Views.Admin
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            this.DataContext = new DashboardViewModel();
        }

        // [MỚI] Hàm xử lý khi bấm nút "Xem & Xử lý"
        private void BtnXuLy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BanAn banCanXuLy)
            {
                // Tìm AdminView cha để gọi hàm chuyển trang
                var adminView = FindParent<AdminView>(this);
                if (adminView != null)
                {
                    adminView.ChuyenDenBanCanXuLy(banCanXuLy);
                }
            }
        }

        // Hàm hỗ trợ tìm Control cha (Helper)
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}
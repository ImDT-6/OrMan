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

            var vm = new DashboardViewModel();
            this.DataContext = vm;

            // Lắng nghe sự kiện từ ViewModel: Khi nào cần chuyển trang thì mới chuyển
            vm.RequestNavigationToTable += (ban) =>
            {
                var adminView = FindParent<AdminView>(this);
                if (adminView != null)
                {
                    adminView.ChuyenDenBanCanXuLy(ban);
                }
            };
        }

        // Helper tìm cha (Giữ nguyên)
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}
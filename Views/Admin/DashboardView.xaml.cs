using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OrMan.Models;
using OrMan.ViewModels;

namespace OrMan.Views.Admin
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            var vm = new DashboardViewModel();
            this.DataContext = vm;

            // Lắng nghe yêu cầu chuyển trang từ ViewModel (khi bấm vào thông báo bàn cần thanh toán)
            vm.RequestNavigationToTable += (ban) =>
            {
                var adminView = FindParent<AdminView>(this);
                if (adminView != null)
                {
                    // Giả sử AdminView có hàm public để chuyển tab
                    // Nếu tên hàm khác, bạn hãy sửa lại cho khớp với AdminView.xaml.cs của bạn
                    adminView.ChuyenDenBanCanXuLy(ban);
                }
            };

            this.Unloaded += DashboardView_Unloaded;
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is DashboardViewModel vm)
            {
                vm.Cleanup(); // Dừng Timer cập nhật doanh thu
            }
        }

        // Helper để tìm AdminView chứa UserControl này
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}
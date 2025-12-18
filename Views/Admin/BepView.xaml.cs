using System.Windows;
using System.Windows.Controls;
using OrMan.ViewModels.Admin;

namespace OrMan.Views.Admin
{
    public partial class BepView : UserControl
    {
        public BepView()
        {
            InitializeComponent();

            // Khởi tạo ViewModel
            var vm = new BepViewModel();
            this.DataContext = vm;

            // Đăng ký sự kiện Unloaded để dọn dẹp Timer khi rời trang
            this.Unloaded += BepView_Unloaded;
        }

        private void BepView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is BepViewModel vm)
            {
                vm.Cleanup(); // Dừng Timer cập nhật món ăn
            }
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using OrMan.ViewModels.Admin;

namespace OrMan.Views.Admin
{
    public partial class KhoView : UserControl
    {
        public KhoView()
        {
            InitializeComponent();

            // Khởi tạo ViewModel
            var vm = new KhoViewModel();
            this.DataContext = vm;

            // Đăng ký sự kiện dọn dẹp khi rời trang
            this.Unloaded += KhoView_Unloaded;
        }

        private void KhoView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is KhoViewModel vm)
            {
                vm.Cleanup(); // Dừng Timer cập nhật kho
            }
        }
    }
}
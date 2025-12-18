using OrMan.ViewModels.Admin;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OrMan.Views.Admin
{
    public partial class QuanLyBanView : UserControl
    {
        public QuanLyBanView()
        {
            InitializeComponent();

            var vm = new QuanLyBanViewModel();
            this.DataContext = vm;

            this.Unloaded += QuanLyBanView_Unloaded;
        }

        private void QuanLyBanView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is QuanLyBanViewModel vm)
            {
                vm.Cleanup(); // Dừng Timer cập nhật trạng thái bàn
            }
        }

        // Xử lý nút In tạm tính để tránh spam click
        private async void BtnInTamTinh_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            btn.IsEnabled = false;

            // Đợi 3 giây trước khi cho phép bấm lại
            await Task.Delay(3000);

            btn.IsEnabled = true;
        }
    }
}
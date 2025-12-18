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

            // Đăng ký sự kiện dọn dẹp Timer
            this.Unloaded += QuanLyBanView_Unloaded;
        }

        private void QuanLyBanView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is QuanLyBanViewModel vm)
            {
                vm.Cleanup();
            }
        }

        private async void BtnInTamTinh_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            btn.IsEnabled = false;
            // Delay 3s để tránh spam lệnh in
            await Task.Delay(3000);
            btn.IsEnabled = true;
        }
    }
}
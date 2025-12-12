using OrMan.ViewModels.Admin;
using System;
using System.Threading.Tasks; // Nhớ thêm dòng này để dùng Task.Delay
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Thêm dòng này để đổi màu nút

namespace OrMan.Views.Admin
{
    public partial class QuanLyBanView : UserControl
    {
        public QuanLyBanView()
        {
            InitializeComponent();
            this.DataContext = new QuanLyBanViewModel();
        }

        // Thêm hàm xử lý sự kiện Click vào đây
        // Trong file QuanLyBanView.xaml.cs
        private async void BtnInTamTinh_Click(object sender, RoutedEventArgs e)
        {
            // 1. Chỉ khóa nút để tránh bấm liên tục
            var btn = sender as Button;
            btn.IsEnabled = false;

            // 2. Chờ 3 giây
            await Task.Delay(3000);

            // 3. Mở lại nút (KHÔNG đổi chữ ở đây nữa)
            // Việc đổi chữ để XAML tự lo dựa trên trạng thái bàn
            btn.IsEnabled = true;
        }
    }
}
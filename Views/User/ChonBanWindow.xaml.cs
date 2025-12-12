using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using OrMan.Services; // [MỚI] Thêm namespace này
using OrMan.Models;   // [MỚI] Thêm namespace này

namespace OrMan.Views.User
{
    public partial class ChonBanWindow : Window
    {
        public int SelectedTableId { get; private set; } = 0;
        private readonly BanAnRepository _repository; // [MỚI] Khai báo Repository

        public ChonBanWindow()
        {
            InitializeComponent();
            _repository = new BanAnRepository(); // [MỚI] Khởi tạo
            LoadTables();
        }

        private void LoadTables()
        {
            // [CẬP NHẬT] Lấy danh sách thật từ Database thay vì vòng lặp for cứng
            // Bất kỳ thay đổi nào bên Admin (Thêm/Xóa) sẽ hiện ra ở đây ngay lập tức
            var listBan = _repository.GetAll();
            ItemsControlBan.ItemsSource = listBan;
        }

        private void BtnChonBan_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            // [CẬP NHẬT] Lấy DataContext là object BanAn
            if (btn != null && btn.DataContext is BanAn ban)
            {
                // Kiểm tra nếu bàn đang có khách thì cảnh báo (tùy chọn)
                //if (ban.TrangThai == "Có Khách")
                //{
                //    MessageBox.Show($"Bàn {ban.SoBan} đang có khách!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                //    return;
                //}

                SelectedTableId = ban.SoBan;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
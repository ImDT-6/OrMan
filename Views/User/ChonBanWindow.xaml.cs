using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GymManagement.Views.User
{
    public partial class ChonBanWindow : Window
    {
        public int SelectedTableId { get; private set; } = 0;

        public ChonBanWindow()
        {
            InitializeComponent();
            LoadTables();
        }

        private void LoadTables()
        {
            // Tạo danh sách 20 bàn (hoặc lấy từ DB nếu muốn chuẩn xác trạng thái)
            var listBan = new List<int>();
            for (int i = 1; i <= 20; i++)
            {
                listBan.Add(i);
            }
            ItemsControlBan.ItemsSource = listBan;
        }

        private void BtnChonBan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Content.ToString(), out int soBan))
            {
                SelectedTableId = soBan;
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
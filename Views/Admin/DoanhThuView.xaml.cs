using OrMan.ViewModels;
using System.Windows.Controls;
using System.Windows.Input; // Cần cái này để dùng Keyboard

namespace OrMan.Views.Admin
{
    public partial class DoanhThuView : UserControl
    {
        public DoanhThuView()
        {
            InitializeComponent();
            this.DataContext = new DoanhThuViewModel();
        }

        // [MỚI] Hàm này chạy khi bạn bấm vào vùng trống của màn hình
        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Lệnh này sẽ làm mất focus của ô đang nhập (TextBox)
            Keyboard.ClearFocus();

            // Hoặc focus vào chính UserControl để chắc chắn
            this.Focus();
        }
    }
}
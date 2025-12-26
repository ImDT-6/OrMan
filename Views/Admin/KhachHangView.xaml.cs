using System.Windows.Controls;
using System.Windows.Input;
using OrMan.ViewModels;

namespace OrMan.Views.Admin
{
    public partial class KhachHangView : UserControl
    {
        public KhachHangView()
        {
            InitializeComponent();
            // Gán ViewModel để hiển thị dữ liệu
            this.DataContext = new KhachHangViewModel();
        }

        // Hàm này xử lý sự kiện MouseDown được khai báo trong XAML
        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Xóa focus khỏi các ô nhập liệu (TextBox) khi click ra vùng trống
            Keyboard.ClearFocus();
            // Focus lại vào UserControl để đảm bảo các phím tắt (nếu có) hoạt động đúng
            this.Focus();
        }
    }
}


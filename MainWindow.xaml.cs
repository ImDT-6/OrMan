using System.Windows;
using OrMan.Views;
using OrMan.Views.Admin; // Nhớ using đúng folder
using OrMan.Views.User;

namespace OrMan
{
    public partial class MainWindow : Window
    {
        // KHÔNG CẦN KHAI BÁO BIẾN CACHE (_adminView...) NỮA

        public MainWindow()
        {
            InitializeComponent();
            ChuyenSangDangNhap();
        }

        public void ChuyenSangAdmin()
        {
            // Luôn tạo mới (new) để Admin load lại dữ liệu mới nhất từ Database
            ContentArea.Content = new AdminView();
        }

        public void ChuyenSangUser()
        {
            // Luôn tạo mới để Reset giỏ hàng cho khách mới
            ContentArea.Content = new UserView();
        }

        public void ChuyenSangDangNhap()
        {
            ContentArea.Content = new DangNhapView();
        }
    }
}
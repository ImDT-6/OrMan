using System.Windows;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class GioHangWindow : Window
    {
        private UserViewModel _vm;

        public GioHangWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = _vm; // Kế thừa DataContext để hiện danh sách
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Khi bấm Gửi đơn ở đây mới thực sự Submit
            this.DialogResult = true;
            this.Close();
        }
    }
}
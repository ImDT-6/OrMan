using System.Windows.Controls;
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
    }
}
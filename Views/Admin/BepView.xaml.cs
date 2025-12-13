using System.Windows.Controls;
using OrMan.ViewModels.Admin; // Đảm bảo đã có ViewModel này

namespace OrMan.Views.Admin
{
    /// <summary>
    /// Interaction logic for BepView.xaml
    /// </summary>
    public partial class BepView : UserControl
    {
        public BepView()
        {
            InitializeComponent();

            // Gán DataContext để kết nối giao diện với logic xử lý
            this.DataContext = new BepViewModel();
        }
    }
}
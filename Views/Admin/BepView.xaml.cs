using System.Windows.Controls;
using GymManagement.ViewModels;

namespace GymManagement.Views.Admin
{
    public partial class BepView : UserControl
    {
        public BepView()
        {
            InitializeComponent();

            // Gán DataContext là BepViewModel để kết nối giao diện với logic
            this.DataContext = new BepViewModel();
        }
    }
}
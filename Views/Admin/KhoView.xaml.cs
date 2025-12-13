using System.Windows.Controls;
using OrMan.ViewModels.Admin;

namespace OrMan.Views.Admin
{
    public partial class KhoView : UserControl
    {
        public KhoView()
        {
            InitializeComponent();
            this.DataContext = new KhoViewModel();
        }
    }
}
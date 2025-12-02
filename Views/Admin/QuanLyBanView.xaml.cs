using OrMan.ViewModels;
using System.Windows.Controls;
using OrMan.ViewModels.Admin; // [SỬA LỖI] Đảm bảo using ViewModel

namespace OrMan.Views.Admin // [QUAN TRỌNG] Namespace phải là Admin
{
    // [FIX] Phải có từ khóa 'partial'
    public partial class QuanLyBanView : UserControl
    {
        public QuanLyBanView()
        {
            InitializeComponent(); // Lúc này hàm sẽ hoạt động
            this.DataContext = new QuanLyBanViewModel();
        }
    }
}
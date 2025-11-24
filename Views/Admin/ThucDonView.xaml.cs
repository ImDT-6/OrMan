using System.Windows.Controls;
using GymManagement.ViewModels.Admin; // [QUAN TRỌNG] Namespace của ViewModel Admin
using System.Windows;

namespace GymManagement.Views.Admin // [QUAN TRỌNG] Namespace của View Admin
{
    public partial class ThucDonView : UserControl
    {
        private ThucDonViewModel vm;

        public ThucDonView()
        {
            InitializeComponent();

            // Khởi tạo ViewModel và gán làm DataContext
            vm = new ThucDonViewModel();
            this.DataContext = vm;

            // Lắng nghe sự kiện thay đổi dữ liệu từ ViewModel (nếu cần thiết)
            vm.PropertyChanged += Vm_PropertyChanged;

            // Tải dữ liệu lần đầu khi View được load
            Loaded += (sender, e) => {
                RefreshData();
            };
        }

        // Hàm xử lý khi ViewModel thông báo dữ liệu thay đổi (ví dụ sau khi tìm kiếm)
        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DataUpdated" || e.PropertyName == "TuKhoaTimKiem")
            {
                RefreshData();
            }
        }

        // Sự kiện khi người dùng chuyển Tab (Mì Cay / Đồ Chiên / Nước Uống)
        private void MenuTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuTabControl.SelectedItem is TabItem selectedTab)
            {
                // Lấy Tag từ TabItem (được gán trong XAML)
                if (selectedTab.Tag is string tag)
                {
                    vm.SetCurrentTab(tag);
                    // Hàm RefreshData sẽ được gọi tự động thông qua PropertyChanged "DataUpdated"
                    // Hoặc gọi thủ công nếu cần: RefreshData(); 
                }
            }
        }

        // Sự kiện khi gõ chữ vào ô tìm kiếm
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Việc tìm kiếm đã được Binding TwoWay vào Property "TuKhoaTimKiem" trong ViewModel
            // ViewModel sẽ tự động gọi RefeshList() khi property thay đổi.
        }

        // Hàm cập nhật lại nguồn dữ liệu cho các DataGrid/ListBox
        private void RefreshData()
        {
            // Lấy danh sách đã lọc từ ViewModel
            var filteredData = vm.GetFilteredList();

            // Xác định đang ở Tab nào để gán dữ liệu vào đúng ListBox
            if (MenuTabControl.SelectedItem is TabItem selectedTab)
            {
                string tag = selectedTab.Tag as string;

                // Kiểm tra null để tránh lỗi khi khởi tạo
                if (tag == "Mì Cay" && ListBoxMiCay != null)
                {
                    ListBoxMiCay.ItemsSource = filteredData;
                }
                else if (tag == "Đồ Chiên" && ListBoxDoChien != null)
                {
                    ListBoxDoChien.ItemsSource = filteredData;
                }
                else if (tag == "Nước Uống" && ListBoxNuocUong != null)
                {
                    ListBoxNuocUong.ItemsSource = filteredData;
                }
            }
        }
    }
}
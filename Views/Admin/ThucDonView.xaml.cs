using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OrMan.ViewModels.Admin;

namespace OrMan.Views.Admin
{
    public partial class ThucDonView : UserControl
    {
        private ThucDonViewModel vm;

        public ThucDonView()
        {
            InitializeComponent();

            vm = new ThucDonViewModel();
            this.DataContext = vm;

            vm.PropertyChanged += Vm_PropertyChanged;

            Loaded += (sender, e) => RefreshData();
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DataUpdated" || e.PropertyName == "TuKhoaTimKiem")
            {
                RefreshData();
            }
        }

        private void MenuTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (MenuTabControl.SelectedItem is TabItem selectedTab)
                {
                    string tag = selectedTab.Tag as string;
                    vm.SetCurrentTab(tag);
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void RefreshData()
        {
            var filteredData = vm.GetFilteredList();

            if (MenuTabControl.SelectedItem is TabItem selectedTab)
            {
                string tag = selectedTab.Tag as string;

                if (tag == "Mì Cay" && ListBoxMiCay != null)
                    ListBoxMiCay.ItemsSource = filteredData;
                else if (tag == "Đồ Chiên" && ListBoxDoChien != null)
                    ListBoxDoChien.ItemsSource = filteredData;
                else if (tag == "Nước Uống" && ListBoxNuocUong != null)
                    ListBoxNuocUong.ItemsSource = filteredData;
            }
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Lệnh này sẽ làm mất focus của ô đang nhập (TextBox)
            Keyboard.ClearFocus();

            // Hoặc focus vào chính UserControl để chắc chắn
            this.Focus();
        }
    }
}
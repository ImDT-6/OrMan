using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class TichDiemWindow : Window
    {
        private UserViewModel _vm;
        private bool _isRegisterMode = false;

        public TichDiemWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            txtPhone.Focus();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void txtPhone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) HandleAction();
        }

        // [SỬA LẠI TÊN HÀM CHO KHỚP VỚI XAML]
        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            HandleAction();
        }

        private void HandleAction()
        {
            if (lblError != null) lblError.Visibility = Visibility.Collapsed;

            string phone = txtPhone.Text.Trim();

            // 1. Validate
            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            {
                string msg = GetRes("Str_Msg_InvalidPhone");
                ShowError(msg);
                return;
            }

            // 2. Chế độ Đăng ký
            if (_isRegisterMode)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    ShowError("Vui lòng nhập tên.");
                    txtName.Focus();
                    return;
                }

                // Gọi ViewModel đăng ký (Cần đảm bảo VM có hàm này)
                var newKhach = _vm.RegisterCustomer(phone, name);
                ShowCustomerInfo(newKhach);
                return;
            }

            // 3. Chế độ Tra cứu
            var khach = _vm.CheckMember(phone);

            // Logic kiểm tra khách mới hay cũ
            if (khach.HoTen == "Khách Mới" || khach.HoTen == "Khách Hàng Mới" || khach.KhachHangID == 0)
            {
                SwitchToRegisterMode();
            }
            else
            {
                ShowCustomerInfo(khach);
            }
        }

        private void SwitchToRegisterMode()
        {
            _isRegisterMode = true;
            pnlNameInput.Visibility = Visibility.Visible;

            // [ĐÃ SỬA] Đổi tên biến btnCheck -> btnAction
            btnAction.Content = GetRes("Str_Btn_Register");
            btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Màu cam

            string msg = GetRes("Str_Msg_NewPhoneRegister");
            ShowError(msg);
            if (lblError != null) lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B");

            txtName.Focus();
        }

        private void ShowCustomerInfo(OrMan.Models.KhachHang khach)
        {
            txtPhone.IsEnabled = false;
            pnlNameInput.Visibility = Visibility.Collapsed;
            if (lblError != null) lblError.Visibility = Visibility.Collapsed;

            pnlResult.Visibility = Visibility.Visible;

            string helloPrefix = GetRes("Str_Hello_Prefix");
            string tenHienThi = khach.HoTen;

            if (tenHienThi == "Khách Mới" || tenHienThi == "Khách Hàng Mới")
            {
                string guestName = GetRes("Str_Guest");
                if (!string.IsNullOrEmpty(guestName)) tenHienThi = guestName;
            }
            lblTenKhach.Text = $"{helloPrefix}{tenHienThi}";

            string rankLabel = GetRes("Str_Rank_Label");
            string rankName = GetTranslatedRank(khach.HangThanhVien);
            lblHang.Text = $"{rankLabel}{rankName}";

           
            lblDiem.Text = $"{khach.DiemTichLuy:N0}";

            _vm.CurrentCustomer = khach;

            // [ĐÃ SỬA] Đổi tên biến btnCheck -> btnAction
            btnAction.Content = GetRes("Str_Btn_Done");
            btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#22C55E"); // Xanh lá

            btnAction.Click -= BtnAction_Click;
            btnAction.Click += (s, e) => this.Close();
        }

        private void ShowError(string msg)
        {
            if (lblError != null)
            {
                lblError.Text = msg;
                lblError.Visibility = Visibility.Visible;
                lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#EF4444");
            }
            else
            {
                MessageBox.Show(msg);
            }
        }

        private string GetRes(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }

        private string GetTranslatedRank(string dbRank)
        {
            string resourceKey = "";
            switch (dbRank)
            {
                case "Mới": case "Khách Hàng Mới": resourceKey = "Str_Rank_New"; break;
                case "Bạc": resourceKey = "Str_Rank_Silver"; break;
                case "Vàng": resourceKey = "Str_Rank_Gold"; break;
                case "Kim Cương": resourceKey = "Str_Rank_Diamond"; break;
                default: return dbRank;
            }
            return GetRes(resourceKey);
        }
    }
}
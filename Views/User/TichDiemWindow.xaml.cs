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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Bấm X -> Đóng cửa sổ, DialogResult mặc định là false -> Không gửi đơn
            this.Close();
        }

        // --- XỬ LÝ PHÍM ENTER ---
        private void txtPhone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) HandleAction();
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) HandleAction();
        }

        // --- SỰ KIỆN NÚT BẤM ---
        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            HandleAction();
        }

        // --- LOGIC CHÍNH ---
        private void HandleAction()
        {
            if (lblError != null) lblError.Visibility = Visibility.Collapsed;

            string phone = txtPhone.Text.Trim();

            // 1. Validate Số điện thoại (Bắt buộc 10 số, bắt đầu bằng 0)
            long n;
            bool isNumeric = long.TryParse(phone, out n);

            if (string.IsNullOrEmpty(phone) || phone.Length != 10 || !phone.StartsWith("0") || !isNumeric)
            {
                ShowError("Số điện thoại không hợp lệ. Vui lòng nhập đúng 10 số (VD: 09xx...)!");
                txtPhone.SelectAll();
                txtPhone.Focus();
                return;
            }

            // 2. Chế độ Đăng ký (Khi đang hiện ô nhập tên)
            if (_isRegisterMode)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    ShowError("Vui lòng nhập tên của bạn.");
                    txtName.Focus();
                    return;
                }

                // Đăng ký và hiển thị thông tin
                var newKhach = _vm.RegisterCustomer(phone, name);
                ShowCustomerInfo(newKhach);
                return;
            }

            // 3. Chế độ Tra cứu (Mặc định)
            var khach = _vm.CheckMember(phone);

            // Kiểm tra nếu là khách mới chưa có trong DB
            bool isNew = (khach == null || khach.KhachHangID == 0 || khach.HoTen == "Khách Mới" || khach.HoTen == "Khách Hàng Mới");

            if (isNew)
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

            // Đổi nút thành "ĐĂNG KÝ"
            btnAction.Content = GetRes("Str_Btn_Register");
            btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Màu cam

            // Thông báo
            ShowError(GetRes("Str_Msg_NewPhoneRegister"));
            if (lblError != null) lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B");

            txtName.Focus();
        }

        private void ShowCustomerInfo(OrMan.Models.KhachHang khach)
        {
            // Ẩn ô nhập, hiện kết quả
            txtPhone.IsEnabled = false;
            pnlNameInput.Visibility = Visibility.Collapsed;
            if (lblError != null) lblError.Visibility = Visibility.Collapsed;
            pnlResult.Visibility = Visibility.Visible;

            // Hiển thị thông tin
            string helloPrefix = GetRes("Str_Hello_Prefix");
            lblTenKhach.Text = $"{helloPrefix.Trim()} {khach.HoTen}";

            string rankLabel = GetRes("Str_Rank_Label");
            string rankName = GetTranslatedRank(khach.HangThanhVien);
            lblHang.Text = $"{rankLabel.Trim()} {rankName}";

            string pointLabel = GetRes("Str_Points_Label");
            lblDiem.Text = $"{pointLabel.Trim()} {khach.DiemTichLuy:N0}";

            // Cập nhật ViewModel
            _vm.CurrentCustomer = khach;

            // --- CẤU HÌNH NÚT HOÀN TẤT ---
            btnAction.Content = GetRes("Str_Btn_Done");
            btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#22C55E"); // Xanh lá

            // Gán lại sự kiện: Bấm Hoàn Tất -> Trả về TRUE
            btnAction.Click -= BtnAction_Click;
            btnAction.Click += (s, ev) =>
            {
                this.DialogResult = true; // Chỉ ở đây mới trả về True
            };

            // Focus vào nút để người dùng có thể bấm Enter ngay
            btnAction.Focus();
        }

        // --- HÀM HỖ TRỢ ---
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
                case "Mới":
                case "Khách Hàng Mới":
                    resourceKey = "Str_Rank_New"; break;
                case "Bạc":
                    resourceKey = "Str_Rank_Silver"; break;
                case "Vàng":
                    resourceKey = "Str_Rank_Gold"; break;
                case "Kim Cương":
                    resourceKey = "Str_Rank_Diamond"; break;
                default:
                    return dbRank;
            }
            return GetRes(resourceKey);
        }
    }
}
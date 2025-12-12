using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class TichDiemWindow : Window
    {
        private UserViewModel _vm;
        private bool _isRegisterMode = false; // Cờ để biết đang ở chế độ nào

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

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            HandleAction();
        }

        private void HandleAction()
        {
            lblError.Visibility = Visibility.Collapsed;
            string phone = txtPhone.Text.Trim();

            // Validate số điện thoại
            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            {
                // [ĐÃ SỬA] Lấy thông báo từ file ngôn ngữ
                string msg = Application.Current.TryFindResource("Str_Msg_InvalidPhone") as string;
                ShowError(msg ?? "Số điện thoại không hợp lệ");
                return;
            }

            // TRƯỜNG HỢP 1: ĐANG Ở CHẾ ĐỘ ĐĂNG KÝ
            if (_isRegisterMode)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    string msg = Application.Current.TryFindResource("Str_Msg_EnterName") as string;
                    ShowError(msg ?? "Vui lòng nhập tên");
                    txtName.Focus();
                    return;
                }

                // Gọi hàm đăng ký mới
                var newKhach = _vm.RegisterCustomer(phone, name);
                ShowCustomerInfo(newKhach);
                return;
            }

            // TRƯỜNG HỢP 2: TRA CỨU SỐ ĐIỆN THOẠI
            var khach = _vm.FindCustomer(phone);

            if (khach != null)
            {
                // -> Có khách: Hiển thị thông tin
                ShowCustomerInfo(khach);
            }
            else
            {
                // -> Không có khách: Chuyển sang chế độ đăng ký
                _isRegisterMode = true;

                // Thay đổi giao diện
                pnlNameInput.Visibility = Visibility.Visible; // Hiện ô nhập tên

                // Đổi tên nút thành "ĐĂNG KÝ HỘI VIÊN" (Lấy từ Resource)
                btnAction.Content = Application.Current.TryFindResource("Str_Btn_Register") as string;
                btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Màu cam

                string msg = Application.Current.TryFindResource("Str_Msg_PhoneNotFound") as string;
                ShowError(msg ?? "SĐT mới. Vui lòng nhập tên!");

                // Đổi màu thông báo thành cam (cảnh báo nhẹ)
                lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B");

                txtName.Focus();
            }
        }

        private void ShowCustomerInfo(OrMan.Models.KhachHang khach)
        {
            // Ẩn các ô nhập liệu
            txtPhone.IsEnabled = false;
            pnlNameInput.Visibility = Visibility.Collapsed;
            lblError.Visibility = Visibility.Collapsed;

            // Hiện bảng kết quả
            pnlResult.Visibility = Visibility.Visible;

            // 1. Xử lý Tên Khách ("Hello + Tên")
            string helloPrefix = Application.Current.TryFindResource("Str_Hello_Prefix") as string;
            string tenHienThi = khach.HoTen;

            // Nếu tên trong DB là mặc định "Khách Mới", dịch sang ngôn ngữ hiện tại
            if (tenHienThi == "Khách Mới" || tenHienThi == "Khách Hàng Mới")
            {
                string guestName = Application.Current.TryFindResource("Str_Guest1") as string;
                if (!string.IsNullOrEmpty(guestName)) tenHienThi = guestName;
            }
            lblTenKhach.Text = $"{helloPrefix} {tenHienThi}";

            // 2. Xử lý Hạng (Mapping từ DB sang Resource)
            string rankTranslated = GetTranslatedRank(khach.HangThanhVien);
            lblHang.Text = rankTranslated;

            // 3. Xử lý Điểm
            lblDiem.Text = $"{khach.DiemTichLuy:N0}";

            // Cập nhật khách hàng hiện tại vào ViewModel chính
            _vm.CurrentCustomer = khach;

            // Đổi nút thành HOÀN TẤT
            btnAction.Content = Application.Current.TryFindResource("Str_Btn_Done") as string;
            btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#22C55E"); // Màu xanh lá

            // Bấm lần nữa là đóng
            btnAction.Click -= BtnAction_Click;
            btnAction.Click += (s, e) => this.Close();
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visibility = Visibility.Visible;
            lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#EF4444"); // Đỏ mặc định
        }

        // Hàm Mapping Hạng thành viên từ Database sang Resource
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
                    return dbRank; // Không khớp thì trả về nguyên gốc
            }

            return Application.Current.TryFindResource(resourceKey) as string;
        }
    }
}
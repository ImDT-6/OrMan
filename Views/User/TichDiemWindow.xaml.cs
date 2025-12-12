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

<<<<<<< HEAD
            // Validate số điện thoại
            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            {
                // [ĐÃ SỬA] Lấy thông báo từ file ngôn ngữ thay vì chữ cứng
                string msg = Application.Current.TryFindResource("Str_Msg_InvalidPhone") as string;
                string title = Application.Current.TryFindResource("Str_Title_Error") as string; // Key này đã tạo ở bước trước

                MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
=======
            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            {
                ShowError("Vui lòng nhập số điện thoại hợp lệ (ít nhất 9 số).");
>>>>>>> 21a4c6bbfcc3a007446c5793f0723090df56088e
                return;
            }

            // TRƯỜNG HỢP 1: ĐANG Ở CHẾ ĐỘ ĐĂNG KÝ (Người dùng bấm nút Đăng Ký)
            if (_isRegisterMode)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    ShowError("Vui lòng nhập tên của bạn để tích điểm.");
                    txtName.Focus();
                    return;
                }

<<<<<<< HEAD
            pnlResult.Visibility = Visibility.Visible;

            // 1. Xử lý Tên Khách ("Khách Mới" -> "New Customer")
            string helloPrefix = Application.Current.TryFindResource("Str_Hello_Prefix") as string;
            string tenHienThi = khach.HoTen;

            // Nếu tên trong DB là "Khách Mới" (mặc định), ta dịch nó luôn
            if (tenHienThi == "Khách Mới" || tenHienThi == "Khách Hàng Mới")
            {
                // Lấy chữ "New" từ tài nguyên, ghép thêm chữ Customer/Khách nếu cần
                // Hoặc đơn giản là hiển thị tên gốc nếu bạn không muốn dịch tên người
                // Ở đây ví dụ dịch chữ "Khách Mới" -> "New Customer" nếu bạn muốn:
                // tenHienThi = Application.Current.TryFindResource("Str_Guest") as string; 
                string guestName = Application.Current.TryFindResource("Str_Guest1") as string;
                if (!string.IsNullOrEmpty(guestName))
                {
                    tenHienThi = guestName;
                }
            }
            lblTenKhach.Text = $"{helloPrefix}{tenHienThi}";


            // 2. Xử lý Hạng (QUAN TRỌNG: Mapping từ DB sang Resource)
          
            string rankTranslated = GetTranslatedRank(khach.HangThanhVien); // Hàm tự viết bên dưới

            lblHang.Text = $"{rankTranslated}";


            // 3. Xử lý Điểm
            lblDiem.Text = $"{khach.DiemTichLuy:N0}";
            // Ẩn nút check đi, đổi thành nút Xong
            // [ĐÃ SỬA] Lấy chuỗi "HOÀN TẤT" từ Resource
            btnCheck.Content = Application.Current.TryFindResource("Str_Btn_Done") as string;

            btnCheck.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#22C55E"); // Màu xanh lá
            btnCheck.Click -= BtnCheck_Click;
            btnCheck.Click += (s, ev) => this.Close();
=======
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
                btnAction.Content = "ĐĂNG KÝ HỘI VIÊN";      // Đổi tên nút
                btnAction.Background = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Đổi màu nút sang Cam

                ShowError("Số điện thoại mới. Vui lòng nhập tên để tạo tài khoản!");
                lblError.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Màu cam thông báo
                lblError.Visibility = Visibility.Visible;

                txtName.Focus(); // Focus vào ô nhập tên
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
            lblTenKhach.Text = khach.HoTen;
            lblHang.Text = khach.HangThanhVien;
            lblDiem.Text = $"{khach.DiemTichLuy:N0}";

            // Cập nhật khách hàng hiện tại vào ViewModel chính để dùng cho đơn hàng
            _vm.CurrentCustomer = khach;

            // Đổi nút thành HOÀN TẤT
            btnAction.Content = "HOÀN TẤT";
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
>>>>>>> 21a4c6bbfcc3a007446c5793f0723090df56088e
        }
        private string GetTranslatedRank(string dbRank)
        {
            string resourceKey = "";

            // So sánh dữ liệu gốc trong Database
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
                    return dbRank; // Nếu không khớp cái nào thì giữ nguyên
            }

            return Application.Current.TryFindResource(resourceKey) as string;
        }
    }
}
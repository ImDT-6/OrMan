using System.Windows;
using System.Windows.Input;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class TichDiemWindow : Window
    {
        private UserViewModel _vm;

        public TichDiemWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            txtPhone.Focus(); // Focus ngay vào ô nhập
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            PerformCheck();
        }

        private void txtPhone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformCheck();
            }
        }

        private void PerformCheck()
        {
            string phone = txtPhone.Text.Trim();

            // Validate số điện thoại
            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            {
                // [ĐÃ SỬA] Lấy thông báo từ file ngôn ngữ thay vì chữ cứng
                string msg = Application.Current.TryFindResource("Str_Msg_InvalidPhone") as string;
                string title = Application.Current.TryFindResource("Str_Title_Error") as string; // Key này đã tạo ở bước trước

                MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Gọi ViewModel để check database
            var khach = _vm.CheckMember(phone);

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
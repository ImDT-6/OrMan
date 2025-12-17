using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class DanhGiaWindow : Window
    {
        private int _currentRating = 0;
        private UserViewModel _vm;

        // 1. Định nghĩa danh sách Tag
        private readonly List<string> _tagsPositive = new List<string>
        {
            "Món ăn ngon", "Giá cả hợp lý", "Nhân viên thân thiện",
            "Phục vụ nhiệt tình", "Không gian tuyệt vời", "Lên món nhanh"
        };

        private readonly List<string> _tagsNegative = new List<string>
        {
            "Vệ sinh không sạch sẽ", "Nhân viên không nhiệt tình", "Món ăn không ngon",
            "Món ăn phục vụ lâu", "Giá không phù hợp", "Không gian ồn", "Không gian bất tiện"
        };

        public DanhGiaWindow()
        {
            InitializeComponent();
            _vm = new UserViewModel();

            SetDefaultState();
        }
        private void SetDefaultState()
        {
            _currentRating = 0;

            // Hiện lời nhắc chọn sao
            txtRatingStatus.Text = "Vui lòng chọn số sao";

            // Ẩn câu hỏi và Tag cho đến khi khách chọn sao
            txtQuestion.Text = "";
            wrapPanelTags.Children.Clear();

            // Đảm bảo tất cả sao đều màu xám
            var brushInactive = (Brush)FindResource("TextSecondary");
            SetStarColor(Star1, brushInactive);
            SetStarColor(Star2, brushInactive);
            SetStarColor(Star3, brushInactive);
            SetStarColor(Star4, brushInactive);
            SetStarColor(Star5, brushInactive);
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int rating = int.Parse(btn.Tag.ToString());
            _currentRating = rating;

            UpdateStarUI(rating);
            UpdateTagsUI(rating); // [QUAN TRỌNG] Gọi hàm cập nhật Tag
        }

        // Hàm cập nhật giao diện Sao (Màu sắc & Text trạng thái)
        private void UpdateStarUI(int rating)
        {
            var brushActive = (Brush)FindResource("WarningYellow"); // Vàng (#F59E0B)
            var brushInactive = (Brush)FindResource("TextSecondary"); // Xám

            SetStarColor(Star1, rating >= 1 ? brushActive : brushInactive);
            SetStarColor(Star2, rating >= 2 ? brushActive : brushInactive);
            SetStarColor(Star3, rating >= 3 ? brushActive : brushInactive);
            SetStarColor(Star4, rating >= 4 ? brushActive : brushInactive);
            SetStarColor(Star5, rating >= 5 ? brushActive : brushInactive);

            switch (rating)
            {
                case 1: txtRatingStatus.Text = "Rất thất vọng"; break;
                case 2: txtRatingStatus.Text = "Thất vọng"; break;
                case 3: txtRatingStatus.Text = "Bình thường"; break;
                case 4: txtRatingStatus.Text = "Hài lòng"; break;
                case 5: txtRatingStatus.Text = "Quá tuyệt vời"; break;
            }
        }

        // [MỚI] Hàm thay đổi Tags và Câu hỏi dựa trên số sao
        private void UpdateTagsUI(int rating)
        {
            wrapPanelTags.Children.Clear(); // Xóa tag cũ

            List<string> tagsToShow;

            if (rating == 5)
            {
                // 5 Sao -> Khen ngợi
                txtQuestion.Text = "Bạn cảm thấy hài lòng nhất ở điều gì?";
                tagsToShow = _tagsPositive;
            }
            else
            {
                // 1 đến 4 Sao -> Góp ý cải thiện
                txtQuestion.Text = "Bạn có điều gì chưa hài lòng phải không?";
                tagsToShow = _tagsNegative;
            }

            // Tạo các nút Tag động bằng Code
            foreach (var tagContent in tagsToShow)
            {
                var checkBox = new CheckBox
                {
                    Content = tagContent,
                    Style = (Style)FindResource("GlassTagStyle") // Dùng lại Style đã định nghĩa trong XAML
                };
                wrapPanelTags.Children.Add(checkBox);
            }
        }

        private void SetStarColor(Button btn, Brush color)
        {
            var template = btn.Template;
            var path = (System.Windows.Shapes.Path)template.FindName("starPath", btn);
            if (path != null) path.Fill = color;
        }

        private void BtnGui_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRating == 0)
            {
                MessageBox.Show("Vui lòng chọn số sao đánh giá!", "Thông báo");
                return;
            }

            List<string> selectedTags = new List<string>();

            // Duyệt qua các con của WrapPanel để lấy Tag đã chọn
            foreach (var child in wrapPanelTags.Children)
            {
                if (child is CheckBox cb && cb.IsChecked == true)
                {
                    selectedTags.Add(cb.Content.ToString());
                }
            }

            string tags = string.Join(", ", selectedTags);
            string gopY = txtGopY.Text;

            try
            {
                _vm.GuiDanhGia(_currentRating, tags, gopY);
                MessageBox.Show("Cảm ơn bạn đã đánh giá!", "Thành công");
                this.Close();
            }
            catch
            {
                MessageBox.Show("Có lỗi xảy ra, vui lòng thử lại.");
            }
        }
    }
}
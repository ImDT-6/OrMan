using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Cần thêm dòng này
using System.Windows.Media;
using System.Windows.Media.Animation;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class DanhGiaWindow : Window
    {
        private int _currentRating = 0; // Số sao ĐÃ CHỐT
        private UserViewModel _vm;

        private readonly List<string> _tagsPositive = new List<string>
        {
            "Món ăn ngon", "Giá hợp lý", "Nhân viên thân thiện",
            "Không gian đẹp", "Lên món nhanh", "Sạch sẽ"
        };

        private readonly List<string> _tagsNegative = new List<string>
        {
            "Vệ sinh kém", "Nhân viên thái độ", "Món ăn tệ",
            "Chờ quá lâu", "Giá đắt", "Ồn ào", "Nóng nực"
        };

        public DanhGiaWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            SetDefaultState();
        }

        private void SetDefaultState()
        {
            _currentRating = 0;
            txtRatingStatus.Text = "Chạm vào sao để đánh giá";
            txtRatingStatus.Foreground = (Brush)new BrushConverter().ConvertFrom("#A0AEC0");

            if (pnlDetails != null)
            {
                pnlDetails.Opacity = 0;
                pnlDetails.Visibility = Visibility.Collapsed;
            }

            UpdateStarUI(0); // Tắt hết sao lúc đầu
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        // --- 1. XỬ LÝ RÊ CHUỘT (Hover = Select) ---
        // Logic mới: Rê chuột vào đâu là CHỌN luôn tới đó
        private void Star_MouseEnter(object sender, MouseEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            if (int.TryParse(btn.Tag.ToString(), out int rating))
            {
                _currentRating = rating; // 1. Lưu ngay trạng thái chọn

                UpdateStarUI(rating);    // 2. Cập nhật màu sao và text trạng thái
                UpdateTagsUI(rating);    // 3. Cập nhật Tags câu hỏi tương ứng

                // Hiển thị panel chi tiết (Tags/Góp ý) nếu chưa hiện
                if (pnlDetails.Visibility != Visibility.Visible)
                {
                    pnlDetails.Visibility = Visibility.Visible;
                    DoubleAnimation fade = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
                    pnlDetails.BeginAnimation(UIElement.OpacityProperty, fade);
                }
            }
        }

        // Khi chuột rời khỏi vùng chọn sao:
        // Do ta đã lưu _currentRating ngay khi rê chuột, nên khi rời ra nó sẽ giữ nguyên trạng thái đó.
        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            // Đảm bảo UI hiển thị đúng số sao đang chọn (dư thừa nhưng an toàn)
            UpdateStarUI(_currentRating);
        }

        // --- 2. XỬ LÝ CLICK (Vẫn giữ để hỗ trợ Click/Touch) ---
        private void Star_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int rating = int.Parse(btn.Tag.ToString());

            _currentRating = rating;
            UpdateStarUI(rating);
            UpdateTagsUI(rating);
        }

        // --- 3. HÀM TÔ MÀU SAO ---
        private void HighlightStars(int rating)
        {
            var brushActive = (Brush)new BrushConverter().ConvertFrom("#F59E0B"); // Vàng
            var brushInactive = (Brush)new BrushConverter().ConvertFrom("#4A5568"); // Xám đậm

            SetStarColor(Star1, rating >= 1 ? brushActive : brushInactive);
            SetStarColor(Star2, rating >= 2 ? brushActive : brushInactive);
            SetStarColor(Star3, rating >= 3 ? brushActive : brushInactive);
            SetStarColor(Star4, rating >= 4 ? brushActive : brushInactive);
            SetStarColor(Star5, rating >= 5 ? brushActive : brushInactive);
        }

        // Hàm cập nhật toàn bộ UI
        private void UpdateStarUI(int rating)
        {
            HighlightStars(rating); // Tô màu

            var brushActive = (Brush)new BrushConverter().ConvertFrom("#F59E0B");
            var brushGrey = (Brush)new BrushConverter().ConvertFrom("#A0AEC0");

            if (rating == 0)
            {
                txtRatingStatus.Text = "Chạm vào sao để đánh giá";
                txtRatingStatus.Foreground = brushGrey;
                return;
            }

            txtRatingStatus.Foreground = brushActive;
            switch (rating)
            {
                case 1: txtRatingStatus.Text = "Rất thất vọng 😞"; break;
                case 2: txtRatingStatus.Text = "Thất vọng 😕"; break;
                case 3: txtRatingStatus.Text = "Bình thường 😐"; break;
                case 4: txtRatingStatus.Text = "Hài lòng 😊"; break;
                case 5: txtRatingStatus.Text = "Tuyệt vời! 😍"; break;
            }
        }

        private void UpdateTagsUI(int rating)
        {
            wrapPanelTags.Children.Clear();
            List<string> tagsToShow;

            if (rating >= 4)
            {
                txtQuestion.Text = "Bạn hài lòng nhất về điều gì?";
                tagsToShow = _tagsPositive;
            }
            else
            {
                txtQuestion.Text = "Chúng tôi cần cải thiện điều gì?";
                tagsToShow = _tagsNegative;
            }

            foreach (var tagContent in tagsToShow)
            {
                var checkBox = new CheckBox
                {
                    Content = tagContent,
                    Style = (Style)FindResource("GlassTagStyle")
                };
                wrapPanelTags.Children.Add(checkBox);
            }
        }

        private void SetStarColor(Button btn, Brush color)
        {
            if (btn == null) return;
            var template = btn.Template;
            var path = (System.Windows.Shapes.Path)template.FindName("starPath", btn);
            if (path != null) path.Fill = color;
        }

        private void BtnGui_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRating == 0)
            {
                MessageBox.Show("Vui lòng chọn số sao trước khi gửi!", "Chưa đánh giá", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<string> selectedTags = new List<string>();
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
                MessageBox.Show("Cảm ơn quý khách đã đóng góp ý kiến!", "Đã gửi", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
        private void TxtGopY_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // e.Delta > 0 là lăn lên, < 0 là lăn xuống
            if (e.Delta > 0)
            {
                // Lăn lên: Gọi LineUp vài lần để tốc độ cuộn tự nhiên hơn
                textBox.LineUp();
                textBox.LineUp();
                textBox.LineUp();
            }
            else
            {
                // Lăn xuống
                textBox.LineDown();
                textBox.LineDown();
                textBox.LineDown();
            }

            // Quan trọng: Đánh dấu sự kiện đã được xử lý để tránh xung đột với control cha
            e.Handled = true;
        }
    }
}
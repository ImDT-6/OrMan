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

        private readonly List<string> _tagsNegative = new List<string>
{
    "Str_Tag_Hygiene", "Str_Tag_Attitude", "Str_Tag_Food",
    "Str_Tag_Wait", "Str_Tag_Price", "Str_Tag_Noise", "Str_Tag_Hot"
};

        
        private readonly List<string> _tagsPositive = new List<string>
{
    "Str_Tag_GoodFood", "Str_Tag_GoodService", "Str_Tag_Clean",
    "Str_Tag_NiceView", "Str_Tag_GoodPrice"
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
            HighlightStars(rating); // Hàm tô màu sao (Giữ nguyên)

            var brushActive = (Brush)new BrushConverter().ConvertFrom("#F59E0B");
            var brushGrey = (Brush)new BrushConverter().ConvertFrom("#A0AEC0");

            // Trường hợp chưa chọn sao (0 sao)
            if (rating == 0)
            {
                // "Vui lòng chọn số sao" (Hoặc "Chạm vào sao...")
                txtRatingStatus.SetResourceReference(TextBlock.TextProperty, "Str_PlsSelectStar");
                txtRatingStatus.Foreground = brushGrey;
                return;
            }

            // Trường hợp đã chọn sao
            txtRatingStatus.Foreground = brushActive;
            string resourceKey = "";

            switch (rating)
            {
                case 1: resourceKey = "Str_Rate_1"; break; // Thất vọng
                case 2: resourceKey = "Str_Rate_2"; break; // Tệ
                case 3: resourceKey = "Str_Rate_3"; break; // Bình thường
                case 4: resourceKey = "Str_Rate_4"; break; // Hài lòng
                case 5: resourceKey = "Str_Rate_5"; break; // Tuyệt vời
            }

            // Gán Key Resource để TextBlock tự động dịch ngôn ngữ
            if (!string.IsNullOrEmpty(resourceKey))
            {
                txtRatingStatus.SetResourceReference(TextBlock.TextProperty, resourceKey);
            }
        }

        private void UpdateTagsUI(int rating)
        {
            // 1. Xóa các tag cũ
            wrapPanelTags.Children.Clear();

            List<string> currentTagKeys;

            // 2. Cập nhật câu hỏi và chọn danh sách Tag tương ứng
            if (rating >= 4)
            {
                // Câu hỏi: "Bạn hài lòng nhất về điều gì?"
                txtQuestion.SetResourceReference(TextBlock.TextProperty, "Str_WhatYouLike");
                currentTagKeys = _tagsPositive;
            }
            else
            {
                // Câu hỏi: "Chúng tôi cần cải thiện điều gì?"
                txtQuestion.SetResourceReference(TextBlock.TextProperty, "Str_ImprovementHeader");
                currentTagKeys = _tagsNegative;
            }

            // 3. Tạo các nút Tag động
            foreach (string resourceKey in currentTagKeys)
            {
                // [LƯU Ý] Nên dùng CheckBox để khớp với Style "GlassTagStyle" bạn đã định nghĩa
                CheckBox btn = new CheckBox();

                // Gán Style (để nút bo tròn, có hiệu ứng kính)
                btn.Style = (Style)FindResource("GlassTagStyle");

                // [QUAN TRỌNG] Dùng lệnh này để tự động dịch ngôn ngữ
                // (Nếu dùng btn.Content = resourceKey thì nó sẽ hiện tên Key như lỗi bạn gặp)
                btn.SetResourceReference(ContentControl.ContentProperty, resourceKey);

                // Gán Tag là key để sau này lấy dữ liệu gửi về server
                btn.Tag = resourceKey;

                // Thêm vào giao diện
                wrapPanelTags.Children.Add(btn);
            }

            // Hiện panel tags lên với hiệu ứng
            pnlDetails.Visibility = Visibility.Visible;

            // Animation hiện dần (Optional)
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            pnlDetails.BeginAnimation(OpacityProperty, fadeIn);
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
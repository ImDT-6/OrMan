using System.Globalization;
using System.Threading;
using System.Windows;
using OrMan.Services; // [QUAN TRỌNG] Thêm dòng này

namespace OrMan
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // [QUAN TRỌNG] Gọi hàm kiểm tra và cập nhật Database trước khi mở màn hình chính
            DatabaseInitializer.EnsureDatabaseUpdated();

            // Tạo culture Việt Nam
            var culture = new CultureInfo("vi-VN");
            // 2. ÉP BUỘC ĐỔI TÊN THÁNG (Đây là cái bạn cần)
            // Thay vì "Tháng Mười", "Tháng Mười Một"... ta đổi thành "Tháng 10", "Tháng 11"...
            culture.DateTimeFormat.MonthNames = new string[]
            {
                "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6",
                "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12", ""
            };
            // Ép buộc định dạng ngày tháng theo ý muốn (nếu vi-VN mặc định chưa đúng ý)
            culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            culture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            // Đảm bảo định dạng tiêu đề lịch là: "Tháng 11 2025"
            culture.DateTimeFormat.YearMonthPattern = "MMMM yyyy";
            // Áp dụng cho toàn bộ luồng chạy của App
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Dòng này để fix lỗi format cho các element của WPF
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }
    }
}
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
        }
    }
}
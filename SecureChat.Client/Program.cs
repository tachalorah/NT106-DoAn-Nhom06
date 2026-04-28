using System.Windows.Forms;
using SecureChat.Client.Forms.Settings;
using SecureChat.Client.Models;

namespace SecureChat.Client
{
    // SecureChat.Client/Program.cs
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // 1. Xử lý lỗi trên UI Thread (WinForms)
            Application.ThreadException += new ThreadExceptionEventHandler(UIThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // 2. Xử lý lỗi trên các luồng không phải UI (Non-UI threads)
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // 3. Xử lý lỗi trong các tác vụ bất đồng bộ (Async/Await Tasks)
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogException(e.Exception);
                e.SetObserved();
            };

            ApplicationConfiguration.Initialize();
            Application.Run(new frmLoginRegister());
        }

        private static void UIThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogException(e.Exception);
            MessageBox.Show("Có lỗi xảy ra trong hệ thống giao diện. Vui lòng thử lại.", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((Exception)e.ExceptionObject);
            MessageBox.Show("Ứng dụng gặp lỗi nghiêm trọng và cần khởi động lại.", "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        private static void LogException(Exception ex)
        {
            // Tại đây bạn có thể ghi lỗi vào file log hoặc dùng Serilog
            Console.WriteLine($"[ERROR] {DateTime.Now}: {ex.Message}");
        }
    }
}

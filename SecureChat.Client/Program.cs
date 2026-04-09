using System;
using System.Windows.Forms;
using SecureChat.Client.Forms.Call;

namespace SecureChat.Client
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Quick test entry for video call UI
            Application.Run(new frmVideoCall("Hoàng Minh Hiếu"));
        }
    }
}

using System;
using System.Windows.Forms;
using SecureChat.Client.Forms.Chat;

namespace SecureChat.Client
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new frmMainChat());
        }
    }
}

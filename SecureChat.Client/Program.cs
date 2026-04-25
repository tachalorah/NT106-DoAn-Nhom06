using System.Windows.Forms;
using SecureChat.Client.Forms.Settings;
using SecureChat.Client.Models;

namespace SecureChat.Client
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            // Application.Run(new frmSettings(new ProfileModel()));
            Application.Run(new frmLoginRegister());
        }
    }
}

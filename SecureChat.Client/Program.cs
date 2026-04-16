using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


using SecureChat.Client.Components.Call;
using SecureChat.Client.Forms.Call;

using SecureChat.Client.Forms.Settings;
using SecureChat.Client.Models;

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

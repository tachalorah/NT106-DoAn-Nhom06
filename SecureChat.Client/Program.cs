using System;
using System.Windows.Forms;
using SecureChat.Client.Models;
using SecureChat.Client.Forms.Settings;

namespace SecureChat.Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var sampleProfile = new ProfileModel
            {
                FullName = "Hoàng Hi?u",
                PhoneNumber = "+84 903187536",
                Username = "hoanghieu",
                Birthday = new DateTime(1998, 5, 12),
                StatusText = "online"
            };

            Application.Run(new frmSettings(sampleProfile));
        }
    }
}

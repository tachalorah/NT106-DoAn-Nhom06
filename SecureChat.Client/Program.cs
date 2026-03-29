using System;
using System.Windows.Forms;
using SecureChat.Client.Forms.Profile;
using SecureChat.Client.Models;

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
                FullName = "Ho\u00E0ng Hi\u1EBFu",
                PhoneNumber = "+84 903187536",
                Username = "hoanghieu",
                Birthday = new DateTime(1998, 5, 12),
                StatusText = "online"
            };

            Application.Run(new frmMyProfile(sampleProfile));
        }
    }
}

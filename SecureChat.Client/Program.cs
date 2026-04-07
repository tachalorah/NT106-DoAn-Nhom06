using System;
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

            var fakeProfile = new ProfileModel
            {
                FullName = "Hoàng Minh Hiếu",
                PhoneNumber = "0903187536",
                Username = "minhhieu_dev1",
                Birthday = new DateTime(2003, 9, 15),
                StatusText = "online"
            };

            Application.Run(new frmSettings(fakeProfile));
        }
    }
}

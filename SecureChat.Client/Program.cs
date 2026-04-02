using System;
using System.Windows.Forms;
using SecureChat.Client.Models;
using SecureChat.Client.Forms.Settings;
using SecureChat.Client.Forms.Chat;
using SecureChat.Client.Forms.Profile;

namespace SecureChat.Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            // Application.Run(new frmTwoFA());
            // Application.Run(new frmMainChat());
            // Application.Run(new frmLoginRegister());
            //Application.Run(new frmForgot());
              Application.Run(new frmContacts());
            // Application.Run(new frmCreateGroup()); // sau khi bâm New Group 
            // Application.Run(new frmGroupInfo()); // bấm ba chấm 
            // Application.Run(new frmMyProfile(new ProfileModel()));  // sau khi bâm vào My Profile
            // Application.Run(new frmProfileInfo(new ProfileModel()));   // từ My Profile bấm vào avatar hoặc tên để xem thông tin chi tiết, sau đó bấm Edit để chỉnh sửa thông tin cá nhân
            // Application.Run(new frmSettings(new ProfileModel())); // bấm vào hamburger -> biểu tượng bánh răng  Settings
            // Application.Run(new frmNotificationsSounds()); // trong frmSetting
            // Application.Run(new frmPrivacySecurity());   // trong frmSetting

            // Application.Run(new frmChatSettings());   // trong frmSetting


        }
    }
}

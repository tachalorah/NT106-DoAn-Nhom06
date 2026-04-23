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

            // Main app loop: login -> settings -> logout -> login
            while (true)
            {
                var loginForm = new frmLoginRegister();
                Application.Run(loginForm);

                // If login was successful (DialogResult = OK), show settings
                if (loginForm.DialogResult == DialogResult.OK)
                {
                    var demoProfile = new ProfileModel
                    {
                        FullName = "SecureChat Tester",
                        PhoneNumber = "+84 912 345 678",
                        Username = "securechat.demo",
                        StatusText = "online"
                    };

                    var settingsForm = new frmSettings(demoProfile);
                    Application.Run(settingsForm);

                    // If settings closed with No (logout), loop back to login
                    if (settingsForm.DialogResult == DialogResult.No)
                    {
                        continue; // Loop back to login
                    }
                    else
                    {
                        break; // Exit if settings closed normally
                    }
                }
                else
                {
                    break; // Exit if login failed or cancelled
                }
            }
        }
    }
}

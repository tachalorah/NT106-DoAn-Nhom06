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
            // Application.Run(new TwoFAForm());
            // Application.Run(new frmMainChat());
             // Application.Run(new frmLoginRegister());
             Application.Run(new frmForgot());
            //  Application.Run(new frmContacts());
        }
    }
}

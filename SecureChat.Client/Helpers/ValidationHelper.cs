using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SecureChat.Client.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var parsed = new MailAddress(email);
                return parsed.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        public static bool IsStrongPassword(string? password, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                errorMessage = "Password must be at least 8 characters long.";
                return false;
            }

            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                errorMessage = "Password must include at least one uppercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, "[a-z]"))
            {
                errorMessage = "Password must include at least one lowercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, "[0-9]"))
            {
                errorMessage = "Password must include at least one number.";
                return false;
            }

            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
            {
                errorMessage = "Password must include at least one special character.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}

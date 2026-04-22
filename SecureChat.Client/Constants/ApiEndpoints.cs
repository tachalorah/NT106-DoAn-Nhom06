namespace SecureChat.Client.Constants
{
    public static class ApiEndpoints
    {
        public static class Auth
        {
            public const string RequestPasswordOtp = "api/auth/forgot-password/request-otp";
            public const string VerifyPasswordOtp = "api/auth/forgot-password/verify-otp";
            public const string ResetPassword = "api/auth/forgot-password/reset";
        }
    }
}

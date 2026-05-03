using System.Collections.Concurrent;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SecureChat.Services
{
    public enum OtpVerifyStatus
    {
        Success,
        Invalid,
        Expired
    }

    public enum ResetTokenStatus
    {
        Valid,
        Invalid,
        Expired
    }

    public sealed class ForgotPasswordService(EmailService emailService, ILogger<ForgotPasswordService> logger)
    {
        private readonly ConcurrentDictionary<string, OtpEntry> _otpByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ResetTokenEntry> _resetTokens = new();
        private readonly ILogger<ForgotPasswordService> _logger = logger;
        private readonly EmailService _emailService = emailService;

        public Task CreateOtpAsync(string email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            PruneExpiredEntries();

            var key = email.Trim();
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var expiresAt = DateTime.UtcNow.AddMinutes(5);

            _otpByEmail[key] = new OtpEntry(otp, expiresAt);

            foreach (var token in _resetTokens.Where(x => string.Equals(x.Value.Email, key, StringComparison.OrdinalIgnoreCase)).Select(x => x.Key).ToList())
            {
                _resetTokens.TryRemove(token, out _);
            }

            _logger.LogInformation("ForgotPassword OTP generated for {Email}. OTP={Otp}. ExpiresAt={ExpiresAt}", MaskEmail(key), otp, expiresAt);
            // Attempt to send OTP email asynchronously. Fire-and-forget but log result.
            _ = Task.Run(async () =>
            {
                try
                {
                    var ok = await _emailService.SendOtpEmailAsync(key, otp);
                    if (!ok)
                        _logger.LogWarning("Sending OTP email failed for {Email}", MaskEmail(key));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error while sending OTP email to {Email}", MaskEmail(key));
                }
            });

            return Task.CompletedTask;
        }

        public Task<(OtpVerifyStatus Status, string? ResetToken)> VerifyOtpAsync(string email, string otp)
        {
            var key = email.Trim();
            if (!_otpByEmail.TryGetValue(key, out var otpEntry))
            {
                _logger.LogInformation("ForgotPassword OTP verify failed for {Email}: OTP not found", MaskEmail(key));
                return Task.FromResult((OtpVerifyStatus.Invalid, (string?)null));
            }

            if (otpEntry.ExpiresAt <= DateTime.UtcNow)
            {
                _otpByEmail.TryRemove(key, out _);
                _logger.LogInformation("ForgotPassword OTP verify failed for {Email}: OTP expired", MaskEmail(key));
                return Task.FromResult((OtpVerifyStatus.Expired, (string?)null));
            }

            if (!string.Equals(otpEntry.Code, otp, StringComparison.Ordinal))
            {
                _logger.LogInformation("ForgotPassword OTP verify failed for {Email}: OTP invalid", MaskEmail(key));
                return Task.FromResult((OtpVerifyStatus.Invalid, (string?)null));
            }

            _otpByEmail.TryRemove(key, out _);

            var resetToken = GenerateSecureToken();
            _resetTokens[resetToken] = new ResetTokenEntry(key, DateTime.UtcNow.AddMinutes(15));
            _logger.LogInformation("ForgotPassword OTP verified for {Email}. Reset token created.", MaskEmail(key));

            return Task.FromResult((OtpVerifyStatus.Success, resetToken));
        }

        public Task<(ResetTokenStatus Status, string? Email)> ValidateResetTokenAsync(string resetToken)
        {
            if (!_resetTokens.TryGetValue(resetToken, out var tokenEntry))
            {
                _logger.LogInformation("ForgotPassword reset token validation failed: token not found");
                return Task.FromResult((ResetTokenStatus.Invalid, (string?)null));
            }

            if (tokenEntry.ExpiresAt <= DateTime.UtcNow)
            {
                _resetTokens.TryRemove(resetToken, out _);
                _logger.LogInformation("ForgotPassword reset token validation failed for {Email}: token expired", MaskEmail(tokenEntry.Email));
                return Task.FromResult((ResetTokenStatus.Expired, (string?)null));
            }

            _resetTokens.TryRemove(resetToken, out _);
            _logger.LogInformation("ForgotPassword reset token validated for {Email}", MaskEmail(tokenEntry.Email));
            return Task.FromResult((ResetTokenStatus.Valid, tokenEntry.Email));
        }

        private void PruneExpiredEntries()
        {
            var now = DateTime.UtcNow;

            foreach (var item in _otpByEmail.Where(x => x.Value.ExpiresAt <= now).ToList())
            {
                _otpByEmail.TryRemove(item.Key, out _);
            }

            foreach (var item in _resetTokens.Where(x => x.Value.ExpiresAt <= now).ToList())
            {
                _resetTokens.TryRemove(item.Key, out _);
            }
        }

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1)
            {
                return "***";
            }

            return $"{email[0]}***{email[(at - 1)..]}";
        }

        private sealed record OtpEntry(string Code, DateTime ExpiresAt);
        private sealed record ResetTokenEntry(string Email, DateTime ExpiresAt);
    }
}

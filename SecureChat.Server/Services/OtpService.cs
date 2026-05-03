using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SecureChat.Services
{
    public sealed class OtpService
    {
        private readonly ConcurrentDictionary<string, OtpEntry> _store = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<OtpService> _logger;

        public OtpService(ILogger<OtpService> logger)
        {
            _logger = logger;
        }

        public Task<string> GenerateOtpAsync(string email, string purpose = "forgot-password", int ttlMinutes = 5)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            PruneExpiredEntries();
            var key = MakeKey(email, purpose);
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var expiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);
            _store[key] = new OtpEntry(otp, expiresAt);
            _logger.LogInformation("OTP generated for {Email}. ExpiresAt={ExpiresAt}", MaskEmail(email), expiresAt);
            return Task.FromResult(otp);
        }

        public Task<OtpVerifyStatus> VerifyOtpAsync(string email, string otp, string purpose = "forgot-password")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            if (string.IsNullOrWhiteSpace(otp)) return Task.FromResult(OtpVerifyStatus.Invalid);

            var key = MakeKey(email, purpose);
            if (!_store.TryGetValue(key, out var entry))
            {
                _logger.LogInformation("OTP verify failed for {Email}: not found", MaskEmail(email));
                return Task.FromResult(OtpVerifyStatus.Invalid);
            }

            if (entry.ExpiresAt <= DateTime.UtcNow)
            {
                _store.TryRemove(key, out _);
                _logger.LogInformation("OTP verify failed for {Email}: expired", MaskEmail(email));
                return Task.FromResult(OtpVerifyStatus.Expired);
            }

            if (!string.Equals(entry.Code, otp, StringComparison.Ordinal))
            {
                _logger.LogInformation("OTP verify failed for {Email}: invalid", MaskEmail(email));
                return Task.FromResult(OtpVerifyStatus.Invalid);
            }

            _store.TryRemove(key, out _);
            _logger.LogInformation("OTP verified for {Email}", MaskEmail(email));
            return Task.FromResult(OtpVerifyStatus.Success);
        }

        private static string MakeKey(string email, string purpose)
        {
            return $"{purpose}:{email.Trim().ToLowerInvariant()}";
        }

        private void PruneExpiredEntries()
        {
            var now = DateTime.UtcNow;
            foreach (var item in _store.Where(x => x.Value.ExpiresAt <= now).Select(x => x.Key).ToList())
            {
                _store.TryRemove(item, out _);
            }
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return "***";
            return $"{email[0]}***{email[(at - 1)..]}";
        }

        private sealed record OtpEntry(string Code, DateTime ExpiresAt);
    }
}

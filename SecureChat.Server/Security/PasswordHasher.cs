using System;
using System.Security.Cryptography;
using System.Text;

using Konscious.Security.Cryptography;

namespace SecureChat.Server.Security
{
    public static class PasswordHasher
    {
        public static string NormalizeForStorage(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            return IsArgon2Hash(input) ? input : HashPassword(input);
        }

        public static bool Verify(string passwordOrHash, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            if (!IsArgon2Hash(storedHash))
            {
                return string.Equals(passwordOrHash, storedHash, StringComparison.Ordinal);
            }

            var parts = storedHash.Split(':', 2);
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(passwordOrHash))
            {
                Salt = salt,
                DegreeOfParallelism = 4,
                MemorySize = 1024 * 64,
                Iterations = 4
            };

            var actual = argon2.GetBytes(expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        private static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 4,
                MemorySize = 1024 * 64,
                Iterations = 4
            };

            var hash = argon2.GetBytes(32);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        private static bool IsArgon2Hash(string value)
        {
            var parts = value.Split(':', 2);
            if (parts.Length != 2)
            {
                return false;
            }

            try
            {
                _ = Convert.FromBase64String(parts[0]);
                _ = Convert.FromBase64String(parts[1]);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}

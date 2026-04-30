using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace SecureChat.Server.Security
{
    public static class PasswordHasher
    {
        // Trả về Tuple gồm Hash và Salt riêng biệt
        public static (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = saltBytes,
                DegreeOfParallelism = 4,
                MemorySize = 1024 * 64,
                Iterations = 4
            };

            var hashBytes = argon2.GetBytes(32);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        // Nhận thêm tham số storedSalt từ Database
        public static bool Verify(string passwordOrHash, string storedHash, string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedSalt))
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(storedSalt);
                var expectedHash = Convert.FromBase64String(storedHash);

                var argon2 = new Argon2id(Encoding.UTF8.GetBytes(passwordOrHash))
                {
                    Salt = salt,
                    DegreeOfParallelism = 4,
                    MemorySize = 1024 * 64,
                    Iterations = 4
                };

                var actualHash = argon2.GetBytes(expectedHash.Length);
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
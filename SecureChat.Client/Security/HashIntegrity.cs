using System;
using System.Text;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;

namespace SecureChat.Client.Security
{
    /// ✅ FIX: Tên class là Argon2Hasher để không bị trùng tên với thư viện
    /// Sử dụng random salt cho mỗi password (không hardcoded salt)
    public static class Argon2Hasher
    {
        /// <summary>
        /// Băm mật khẩu người dùng bằng thuật toán Argon2id (An toàn nhất hiện nay)
        /// Trả về chuỗi: "{base64_salt}:{base64_hash}"
        /// </summary>
        public static string HashPassword(string password)
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = saltBytes,
                DegreeOfParallelism = 4, // Số luồng CPU sử dụng
                MemorySize = 1024 * 64,  // Tốn 64MB RAM khi băm (chống máy tính đào coin hack pass)
                Iterations = 4           // Lặp 4 vòng
            };

            byte[] hashBytes = argon2.GetBytes(32); // Lấy ra 32 byte mã băm

            // FIX: Trả về salt + hash cách nhau bởi ':' để có thể verify sau
            // Format: "salt_base64:hash_base64"
            string saltBase64 = Convert.ToBase64String(saltBytes);
            string hashBase64 = Convert.ToBase64String(hashBytes);
            return $"{saltBase64}:{hashBase64}";
        }

        /// <summary>
        /// Verify mật khẩu người dùng nhập vào với hash đã lưu
        /// </summary>
        /// <param name="password">Mật khẩu người dùng nhập</param>
        /// <param name="storedHash">Hash đã lưu từ database (format: "salt_base64:hash_base64")</param>
        /// <returns>true nếu match, false nếu sai</returns>
        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                // Tách salt và hash từ chuỗi lưu trữ
                var parts = storedHash.Split(':');
                if (parts.Length != 2)
                    return false;

                string saltBase64 = parts[0];
                string storedHashBase64 = parts[1];

                // Decode salt từ base64
                byte[] saltBytes = Convert.FromBase64String(saltBase64);

                // Bám lại password với salt đó
                var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
                {
                    Salt = saltBytes,
                    DegreeOfParallelism = 4,
                    MemorySize = 1024 * 64,
                    Iterations = 4
                };

                byte[] computedHashBytes = argon2.GetBytes(32);
                string computedHashBase64 = Convert.ToBase64String(computedHashBytes);

                // So sánh hash mới tính với hash lưu trữ
                return computedHashBase64 == storedHashBase64;
            }
            catch
            {
                return false;
            }
        }
    }
}

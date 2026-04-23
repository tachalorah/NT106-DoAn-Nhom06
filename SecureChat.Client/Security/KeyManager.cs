using System;
using System.Security.Cryptography;

namespace SecureChat.Client.Security
{
    public static class RSAKeyManager
    {
        /// <summary>
        /// Sinh ra cặp khóa: Public (đưa cho Server) và Private (Giữ lại máy Client)
        /// </summary>
        public static (string PublicKey, string PrivateKey) GenerateRSAKeys()
        {
            using (var rsa = RSA.Create(2048))
            {
                // Export khóa Public (Dùng để người khác mã hóa tin nhắn gửi cho mình)
                string publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());

                // Export khóa Private (Dùng để tự mình giải mã tin nhắn - TUYỆT ĐỐI KHÔNG GỬI LÊN MẠNG)
                string privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

                return (publicKey, privateKey);
            }
        }
    }
}

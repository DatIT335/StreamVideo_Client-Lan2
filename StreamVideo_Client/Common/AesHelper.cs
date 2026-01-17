using System.Security.Cryptography;
using System.Text;

namespace StreamVideo_Client.Common // Đổi namespace phù hợp cho Client khi copy qua
{
    public static class AesHelper
    {
        // Key và IV cứng (Trong thực tế nên Diffie-Hellman để trao đổi, nhưng làm đồ án có thể fix cứng để demo)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes

        /// <summary>
        /// Mã hóa mảng byte (Video/Image chunk)
        /// </summary>
        public static byte[] Encrypt(byte[] data)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);

            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        /// <summary>
        /// Giải mã mảng byte
        /// </summary>
        public static byte[] Decrypt(byte[] cipherData)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);

            cs.Write(cipherData, 0, cipherData.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
    }
}
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StreamVideo_Cliennt.Network // Lưu ý: Bên Client nhớ đổi namespace lại cho khớp
{
    public static class SecurityHelper
    {
        // Khóa bí mật (Key) - 32 bytes cho AES-256
        // Trong thực tế, Key này sẽ được sinh ngẫu nhiên và trao đổi qua RSA. 
        // Để test trước, t đang để cố định (Hardcode).
        private static readonly string KeyString = "12345678901234567890123456789012"; // 32 ký tự

        // Vector khởi tạo (IV) - 16 bytes
        private static readonly string IVString = "1234567890123456"; // 16 ký tự

        public static byte[] Encrypt(byte[] dataToEncrypt)
        {
            if (dataToEncrypt == null || dataToEncrypt.Length == 0) return null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(KeyString);
                aesAlg.IV = Encoding.UTF8.GetBytes(IVString);
                aesAlg.Padding = PaddingMode.PKCS7; // Tự động thêm padding nếu dữ liệu hụt

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                        csEncrypt.FlushFinalBlock();
                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        public static byte[] Decrypt(byte[] cipherData)
        {
            if (cipherData == null || cipherData.Length == 0) return null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(KeyString);
                aesAlg.IV = Encoding.UTF8.GetBytes(IVString);
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherData))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream msPlain = new MemoryStream())
                        {
                            csDecrypt.CopyTo(msPlain);
                            return msPlain.ToArray();
                        }
                    }
                }
            }
        }
    }
}
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace ZoomServer
{
    public static class SymmetricEncryptionManager
    {
        /// <summary>
        /// Contains the AES key and Iv, used for encryption and decryption.
        /// </summary>
        public static SymmetricKeyBundle EncryptionData;

        /// <summary>
        /// Static constructor that generates a new AES key and IV and assigns them to the <see cref="EncryptionData"/> property.
        /// </summary>
        static SymmetricEncryptionManager()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();
                EncryptionData = new SymmetricKeyBundle();
                EncryptionData.Key = aes.Key;
                EncryptionData.Iv = aes.IV;
            }
        }

        /// <summary>
        /// Encrypts a plain text string using the AES key and Iv, returning the result as a Base64-encoded string.
        /// </summary>
        /// <param name="plainText">The plain text string to encrypt.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plainText"/> is null or empty.</exception>
        public static string EncodeText(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionData.Key;
                aes.IV = EncryptionData.Iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts a Base64-encoded encrypted string using the AES key and Iv, returning the original plain text.
        /// </summary>
        /// <param name="cipherText">The Base64-encoded string to decrypt.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipherText"/> is null or empty.</exception>
        public static string DecodeText(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            Console.WriteLine("aes decrypt: " + cipherText);
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionData.Key;
                aes.IV = EncryptionData.Iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(buffer))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                {
                    string plainText = srDecrypt.ReadToEnd();
                    Console.WriteLine("aes : " + plainText);
                    return plainText;
                }
            }
        }
    }

}



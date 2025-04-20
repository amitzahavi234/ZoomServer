using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ZoomServer
{
    internal class RsaFunctions
    {
        public static RSAParameters PublicKey;

        /// <summary>
        /// Encrypts a plain text string using the RSA public key.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Encrypt(string plainText)
        {
            byte[] encrypted;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(PublicKey);
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
                encrypted = rsa.Encrypt(dataToEncrypt, true);
            }
            return Convert.ToBase64String(encrypted);
        }
    }
}

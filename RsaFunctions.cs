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
             /// <summary>
        /// Encrypts a plain text string using the RSA public key.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Encrypt(string plainText, RSAParameters publicKey)
        {
            byte[] encrypted;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(publicKey);
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
                encrypted = rsa.Encrypt(dataToEncrypt, true);
            }
            return Convert.ToBase64String(encrypted);
        }
    }
}

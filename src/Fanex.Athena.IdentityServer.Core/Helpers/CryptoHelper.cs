using System.Security.Cryptography;
using System.Text;

namespace Fanex.Athena.IdentityServer.Core.Helpers
{
    public static class CryptoHelper
    {
        public static string HashSHA1(string content, string salt)
        {
            var sha1 = new SHA1CryptoServiceProvider();
            var hashed = sha1.ComputeHash(Encoding.UTF8.GetBytes(content + salt));
            return BinaryToHex(hashed);
        }

        public static string BinaryToHex(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            char[] hex = new char[checked(data.Length * 2)];

            for (int i = 0; i < data.Length; i++)
            {
                byte thisByte = data[i];
                hex[2 * i] = NibbleToHex((byte)(thisByte >> 4)); // high nibble
                hex[2 * i + 1] = NibbleToHex((byte)(thisByte & 0xf)); // low nibble
            }

            return new string(hex);
        }

        private static char NibbleToHex(byte nibble)
        {
            return (char)((nibble < 10) ? (nibble + '0') : (nibble - 10 + 'A'));
        }
    }
}
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

            const int even = 2;
            const int rightShift = 4;
            const int hexValue = 0xf;

            for (int i = 0; i < data.Length; i++)
            {
                byte thisByte = data[i];
                hex[even * i] = NibbleToHex((byte)(thisByte >> rightShift));
                hex[even * i + 1] = NibbleToHex((byte)(thisByte & hexValue));
            }

            return new string(hex);
        }

        private static char NibbleToHex(byte nibble)
        {
            const int @decimal = 10;
            return (char)((nibble < @decimal) ? (nibble + '0') : (nibble - @decimal + 'A'));
        }
    }
}
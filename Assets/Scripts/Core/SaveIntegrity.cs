using System;
using System.Security.Cryptography;
using System.Text;

namespace DiamondTilt.Core
{
    public static class SaveIntegrity
    {
        public static byte[] DeriveKey(uint seed)
        {
            var rng = new Mulberry32Rng(seed);
            var key = new byte[32];
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (byte)(rng.NextDouble() * 256.0);
            }
            return key;
        }

        public static string Tag(string payload, byte[] key)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            ValidateKey(key);

            using var hmac = new HMACSHA256(key);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return ToHex(hash);
        }

        public static bool Verify(string payload, string tag, byte[] key)
        {
            if (payload == null || tag == null) return false;
            try
            {
                ValidateKey(key);
                using var hmac = new HMACSHA256(key);
                byte[] expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                byte[] provided = FromHex(tag);
                if (provided.Length != expected.Length) return false;
                return CryptographicOperations.FixedTimeEquals(expected, provided);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void ValidateKey(byte[] key)
        {
            if (key == null || key.Length < 16) throw new ArgumentException("key must be at least 16 bytes", nameof(key));
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static byte[] FromHex(string hex)
        {
            if (hex.Length % 2 != 0) throw new FormatException();
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}

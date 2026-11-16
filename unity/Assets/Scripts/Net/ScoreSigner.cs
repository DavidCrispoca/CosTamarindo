using System;
using System.Security.Cryptography;
using System.Text;

namespace CosTamarindo.Net
{
    public static class ScoreSigner
    {
        public static string Canonical(string sessionId, int score, int durationMs, int catches, string nonce)
        {
            return $"{sessionId}|{score}|{durationMs}|{catches}|{nonce}";
        }

        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
            int len = hex.Length;
            byte[] bytes = new byte[len / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        public static string SignHex(byte[] key, string message)
        {
            using (var hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message ?? string.Empty));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string SignSessionHex(string sessionKeyHex, string sessionId, int score, int durationMs, int catches, string nonce)
        {
            var key = HexToBytes(sessionKeyHex);
            var canonical = Canonical(sessionId, score, durationMs, catches, nonce);
            return SignHex(key, canonical);
        }
    }
}
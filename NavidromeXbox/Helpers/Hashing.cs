using System;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;

namespace NavidromeXbox.Helpers
{
    /// <summary>
    /// MD5 + random-salt helpers for the Subsonic token authentication scheme
    /// (token = md5(password + salt)). Uses the UWP-native crypto provider, which is
    /// always available on Xbox (System.Security.Cryptography.MD5 is not in the UWP profile).
    /// </summary>
    public static class Hashing
    {
        public static string Md5Hex(string input)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            var buffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buffer);
            // Subsonic expects lowercase hex.
            return CryptographicBuffer.EncodeToHexString(hashed).ToLowerInvariant();
        }

        /// <summary>A fresh lowercase-hex salt for every request (Subsonic requires per-request salts).</summary>
        public static string RandomSalt(int bytes = 8)
        {
            var buf = CryptographicBuffer.GenerateRandom((uint)bytes);
            return CryptographicBuffer.EncodeToHexString(buf).ToLowerInvariant();
        }
    }
}

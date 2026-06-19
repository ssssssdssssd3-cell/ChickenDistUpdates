using System;
using System.Security.Cryptography;
using System.Text;

namespace ChickenDist.Core
{
    /// <summary>
    /// أدوات تشفير كلمات المرور باستخدام PBKDF2 مع salt عشوائي.
    /// الصيغة المخزّنة: "pbkdf2$SALT_HEX$HASH_HEX"
    /// </summary>
    public static class PasswordHelper
    {
        private const int SaltSize    = 16;   // 128 bit
        private const int HashSize    = 32;   // 256 bit
        private const int Iterations  = 10000;
        private const string Prefix   = "pbkdf2$";

        /// <summary>
        /// تشفير كلمة مرور جديدة وإرجاع السلسلة المُخزَّنة.
        /// </summary>
        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("كلمة المرور لا يمكن أن تكون فارغة");

            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);

            byte[] hash = Pbkdf2(password, salt);
            return $"{Prefix}{ToHex(salt)}${ToHex(hash)}";
        }

        /// <summary>
        /// التحقق من تطابق كلمة المرور المُدخلة مع القيمة المخزّنة.
        /// يدعم كلمات المرور القديمة (plain text) للتوافق مع البيانات الحالية.
        /// </summary>
        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored))
                return false;

            // كلمة مرور قديمة (plain text) — مقارنة مباشرة للتوافق
            if (!stored.StartsWith(Prefix))
                return password == stored;

            // كلمة مرور جديدة مشفّرة
            try
            {
                string[] parts = stored.Split('$');
                // parts[0]="pbkdf2", parts[1]=salt_hex, parts[2]=hash_hex
                if (parts.Length != 3) return false;

                byte[] salt = FromHex(parts[1]);
                byte[] expectedHash = FromHex(parts[2]);
                byte[] actualHash   = Pbkdf2(password, salt);

                return SlowEquals(expectedHash, actualHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>هل كلمة المرور لا تزال plain text وتحتاج ترقية؟</summary>
        public static bool NeedsUpgrade(string stored)
        {
            return !string.IsNullOrEmpty(stored) && !stored.StartsWith(Prefix);
        }

        // ── خاص ──────────────────────────────────────────────────────────────
        private static byte[] Pbkdf2(string password, byte[] salt)
        {
            // FIX: تحديد HashAlgorithmName.SHA256 صراحةً
            // الكود القديم كان يستخدم HMAC-SHA1 افتراضياً وهو أقل أماناً
            using (var prf = new Rfc2898DeriveBytes(password, salt, Iterations, System.Security.Cryptography.HashAlgorithmName.SHA256))
                return prf.GetBytes(HashSize);
        }

        /// <summary>مقارنة ثابتة الوقت لتجنب Timing attacks</summary>
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        private static byte[] FromHex(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
    }
}

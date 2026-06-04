using System;
using System.Text;

namespace ChickenDist.Core
{
    /// <summary>
    /// تشفير وفك تشفير البيانات السحابية.
    /// المفتاح = مفتاح قاعدة ثابت + رمز النشاط (TenantKey) من الإعدادات.
    /// هذا يضمن عزلاً تاماً بين الأنشطة المختلفة التي تستخدم نفس البرنامج.
    /// </summary>
    public static class SecurityHelper
    {
        private const string BaseKey = "ChickenDistSecureKey2026";

        /// <summary>
        /// يُحسب المفتاح الفعلي = BaseKey + TenantKey من الإعدادات.
        /// لو كل نشاط وضع رمزه الخاص، يصبح المفتاح فريداً لكل نشاط.
        /// </summary>
        private static string EffectiveKey
        {
            get
            {
                string tenant = AppConfig.TenantKey;
                // لو لم يُعيَّن بعد، نستخدم المفتاح الأساسي فقط
                return string.IsNullOrWhiteSpace(tenant) ? BaseKey : BaseKey + tenant;
            }
        }

        /// <summary>المفتاح الفعلي للاستخدام في JS — يُحقن في صفحة المندوب</summary>
        public static string GetEffectiveKeyForJs() => EffectiveKey;

        /// <summary>تشفير النص باستخدام XOR وتحويله لـ Base64 لرفعه بأمان للسحاب</summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            byte[] bytes    = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(EffectiveKey);
            byte[] result   = new byte[bytes.Length];

            for (int i = 0; i < bytes.Length; i++)
                result[i] = (byte)(bytes[i] ^ keyBytes[i % keyBytes.Length]);

            return Convert.ToBase64String(result);
        }

        /// <summary>فك تشفير النص المستلم من السحاب</summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";

            try
            {
                byte[] bytes    = Convert.FromBase64String(cipherText);
                byte[] keyBytes = Encoding.UTF8.GetBytes(EffectiveKey);
                byte[] result   = new byte[bytes.Length];

                for (int i = 0; i < bytes.Length; i++)
                    result[i] = (byte)(bytes[i] ^ keyBytes[i % keyBytes.Length]);

                return Encoding.UTF8.GetString(result);
            }
            catch (Exception ex)
            {
                throw new Exception("فشل فك تشفير البيانات — تأكد أن رمز النشاط (TenantKey) صحيح ومطابق للمندوب. التفاصيل: " + ex.Message);
            }
        }
    }
}

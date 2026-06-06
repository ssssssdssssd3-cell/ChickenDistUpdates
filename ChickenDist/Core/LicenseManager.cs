using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ChickenDist.Core
{
    /// <summary>
    /// يتحقق من ترخيص ChickenDist عند كل تشغيل باستخدام نظام الـ 6 مفاتيح و Settings.ini
    /// </summary>
    public static class LicenseManager
    {
        // !! يجب أن تكون مطابقة لـ LicenseGenerator.cs في أداة التفعيل !!
        private static readonly byte[] SECRET_KEY = new byte[]
        {
            0x43, 0x68, 0x69, 0x63, 0x6B, 0x65, 0x6E, 0x44,
            0x69, 0x73, 0x74, 0x4B, 0x65, 0x79, 0x32, 0x30,
            0x32, 0x36, 0x21, 0x40, 0x23, 0x24, 0x25, 0x5E,
            0x26, 0x2A, 0x28, 0x29, 0x5F, 0x2B, 0x3D, 0x7B
        };

        private static readonly byte[] IV_SEED = new byte[]
        {
            0x44, 0x69, 0x73, 0x74, 0x72, 0x69, 0x62, 0x75,
            0x74, 0x6F, 0x72, 0x32, 0x30, 0x32, 0x36, 0x21
        };

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);

        public static bool   IsActivated    { get; private set; }
        public static bool   AllowNegative  { get; private set; }
        public static string DeviceName     { get; private set; }
        public static DateTime ExpiryDate   { get; private set; }

        private static readonly string SettingsFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");

        /// <summary>
        /// يقرأ قيمة من ملف الـ INI ويفك تشفيرها لو كانت مشفرة بـ DPAPI
        /// </summary>
        public static string ReadIniValue(string section, string key, string defaultValue = "")
        {
            if (!File.Exists(SettingsFilePath)) return defaultValue;
            var temp = new StringBuilder(2048);
            GetPrivateProfileString(section, key, defaultValue, temp, 2048, SettingsFilePath);
            string val = temp.ToString();
            return DecryptDPAPI(val);
        }

        /// <summary>
        /// يقرأ قيمة من ملف الـ INI كما هي بدون أي فك تشفير (للمفاتيح المشفرة بـ AES)
        /// </summary>
        public static string ReadIniRaw(string section, string key, string defaultValue = "")
        {
            if (!File.Exists(SettingsFilePath)) return defaultValue;
            var temp = new StringBuilder(4096);
            GetPrivateProfileString(section, key, defaultValue, temp, 4096, SettingsFilePath);
            return temp.ToString();
        }

        /// <summary>
        /// يكتب قيمة في ملف الـ INI ويشفرها بـ DPAPI لو encrypt = true
        /// </summary>
        public static void WriteIniValue(string section, string key, string value, bool encrypt = false)
        {
            if (!File.Exists(SettingsFilePath))
            {
                // تهيئة الملف مع BOM لترميز UTF-16
                File.WriteAllText(SettingsFilePath, "; ChickenDist Configuration\r\n[General]\r\n", Encoding.Unicode);
            }
            string finalVal = value ?? "";
            if (encrypt) finalVal = EncryptDPAPI(finalVal);
            WritePrivateProfileString(section, key, finalVal, SettingsFilePath);
        }

        /// <summary>
        /// يتحقق من الترخيص عند بدء التشغيل.
        /// </summary>
        public static bool CheckLicense()
        {
            try
            {
                string machineId = GetMachineId();
                string hddSerial = GetHddSerial();

                // 1. إذا كان الملف غير موجود، ننشئه ونكتب بيانات الجهاز فقط (بدون مفاتيح)
                if (!File.Exists(SettingsFilePath))
                {
                    WriteIniValue("General", "MachineID", machineId);
                    WriteIniValue("General", "HddSerial", hddSerial);
                    WriteIniValue("General", "Key1", "");
                    WriteIniValue("General", "Key2", "");
                    WriteIniValue("General", "Key3", "");
                    WriteIniValue("General", "Key4", "");
                    WriteIniValue("General", "Key5", "");
                    WriteIniValue("General", "Key6", "");
                }
                else
                {
                    // لضمان وجود قيم الماشين أي دي والسيريال مقروءة للعميل في الملف دائماً
                    string existingMachine = "";
                    var temp = new StringBuilder(256);
                    GetPrivateProfileString("General", "MachineID", "", temp, 256, SettingsFilePath);
                    existingMachine = temp.ToString();
                    if (string.IsNullOrEmpty(existingMachine))
                    {
                        WritePrivateProfileString("General", "MachineID", machineId, SettingsFilePath);
                        WritePrivateProfileString("General", "HddSerial", hddSerial, SettingsFilePath);
                    }
                }

                // 2. قراءة المفاتيح الـ 6 (AES base64 مباشر بدون DPAPI)
                string k1 = ReadIniRaw("General", "Key1");
                string k2 = ReadIniRaw("General", "Key2");
                string k3 = ReadIniRaw("General", "Key3");
                string k4 = ReadIniRaw("General", "Key4");
                string k5 = ReadIniRaw("General", "Key5");
                string k6 = ReadIniRaw("General", "Key6");

                string errorReason = "";
                bool isValid = false;

                if (!string.IsNullOrEmpty(k1) && !string.IsNullOrEmpty(k2) && !string.IsNullOrEmpty(k3) && 
                    !string.IsNullOrEmpty(k4) && !string.IsNullOrEmpty(k5) && !string.IsNullOrEmpty(k6))
                {
                    string fullKey = $"{k1}|{k2}|{k3}|{k4}|{k5}|{k6}";
                    isValid = ValidateLicense(fullKey, out errorReason);
                }
                else
                {
                    errorReason = "البرنامج غير مفعّل. يرجى إدخال كود التفعيل.";
                }

                if (isValid)
                {
                    // تحميل قيم التفعيل للبرنامج
                    string fullKey = $"{k1}|{k2}|{k3}|{k4}|{k5}|{k6}";
                    var parts = fullKey.Split('|');
                    string name = AesDecrypt(parts[3]);
                    string expiryTicksRaw = AesDecrypt(parts[2]);
                    long.TryParse(expiryTicksRaw, out long ticks);

                    IsActivated   = true;
                    DeviceName    = name;
                    ExpiryDate    = ticks == 0 ? DateTime.MaxValue : new DateTime(ticks);
                    AllowNegative = true; // السماح بالبيع بالسالب افتراضياً لـ ChickenDist

                    return true;
                }

                // عرض شاشة التفعيل لإدخال الكود
                using (var frm = new ChickenDist.Forms.FrmActivation(errorReason))
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        return CheckLicense(); // إعادة التحقق
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                AppLogger.Error("LicenseManager.CheckLicense failed", ex, "LicenseManager");
                IsActivated = true;
                return true;
            }
        }

        /// <summary>
        /// يتحقق من صحة كود التفعيل المجمع (k1|k2|k3|k4|k5|k6)
        /// </summary>
        public static bool ValidateLicense(string licenseKey, out string errorReason)
        {
            errorReason = "";
            try
            {
                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    errorReason = "كود التفعيل فارغ.";
                    return false;
                }

                var parts = licenseKey.Split('|');
                if (parts.Length < 6)
                {
                    errorReason = "كود التفعيل غير مكتمل (يجب أن يحتوي على 6 مقاطع).";
                    return false;
                }

                // فك تشفير البيانات بـ AES
                string machineId = AesDecrypt(parts[0]);
                string hddSerial = AesDecrypt(parts[1]);
                string expiryTicksRaw = AesDecrypt(parts[2]);
                string customerName = AesDecrypt(parts[3]); 
                string metaData = AesDecrypt(parts[4]); 
                string signature = parts[5]; 

                if (string.IsNullOrEmpty(machineId) || string.IsNullOrEmpty(hddSerial) || string.IsNullOrEmpty(expiryTicksRaw))
                {
                    errorReason = "كود التفعيل غير صالح (فشل فك التشفير).";
                    return false;
                }

                // التحقق من تطابق هوية الهارد
                string currentMachine = GetMachineId();
                string currentHdd = GetHddSerial();

                if (!string.Equals(machineId, currentMachine, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(hddSerial, currentHdd, StringComparison.OrdinalIgnoreCase))
                {
                    errorReason = "هذا التفعيل غير مخصص للعمل على هذا الجهاز.";
                    return false;
                }

                // التحقق من التوقيع الرقمي (Payload: ID|HDD|Ticks|Cust|Meta)
                string payloadToSign = machineId + "|" + hddSerial + "|" + expiryTicksRaw + "|" + customerName + "|" + metaData;
                using (var hmac = new HMACSHA256(SECRET_KEY))
                {
                    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadToSign));
                    string computedSignature = Convert.ToBase64String(computedHash);
                    string expectedEncryptedSignature = AesEncrypt(computedSignature);

                    if (signature != expectedEncryptedSignature)
                    {
                        errorReason = "مفتاح التفعيل مزور أو تم التعديل عليه.";
                        return false;
                    }
                }

                // التحقق من تاريخ الانتهاء
                if (long.TryParse(expiryTicksRaw, out long expiryTicks))
                {
                    DateTime expiryDate = expiryTicks == 0 ? DateTime.MaxValue : new DateTime(expiryTicks);
                    if (expiryDate < DateTime.Today && expiryDate != DateTime.MaxValue)
                    {
                        errorReason = $"انتهت صلاحية هذا الترخيص في {expiryDate:yyyy-MM-dd}.";
                        return false;
                    }
                }
                else
                {
                    errorReason = "تاريخ الانتهاء في الترخيص غير صالح.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorReason = "خطأ أثناء التحقق: " + ex.Message;
                return false;
            }
        }

        public static string GetLicenseInfo()
        {
            if (!IsActivated) return "غير مفعّل";
            string expiry = ExpiryDate == DateTime.MaxValue ? "دائم" : ExpiryDate.ToString("yyyy-MM-dd");
            return $"الجهاز: {DeviceName}\n" +
                   $"الانتهاء: {expiry}\n" +
                   $"البيع بالسالب: {(AllowNegative ? "مسموح" : "غير مسموح")}";
        }

        public static string GetCurrentMachineId() => GetMachineId();
        public static string GetCurrentHddSerial() => GetHddSerial();

        // ─── AES Encryption / Decryption ──────────────────────────────────────

        public static string AesEncrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = SECRET_KEY;
                aes.IV  = IV_SEED;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var encryptor = aes.CreateEncryptor();
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
        }

        public static string AesDecrypt(string cipherB64)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = SECRET_KEY;
                    aes.IV  = IV_SEED;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    var decryptor = aes.CreateDecryptor();
                    byte[] cipherBytes = Convert.FromBase64String(cipherB64);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch { return null; }
        }

        // ─── DPAPI Encryption / Decryption (Local Machine Bound) ───────────────

        public static string EncryptDPAPI(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            try {
                byte[] data = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                return "ENC:" + Convert.ToBase64String(encrypted);
            } catch { return plainText; }
        }

        public static string DecryptDPAPI(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText) || !encryptedText.StartsWith("ENC:")) return encryptedText;
            try {
                byte[] data = Convert.FromBase64String(encryptedText.Substring(4));
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            } catch { return encryptedText; }
        }

        // ─── Hardware Fingerprinting ────────────────────────────────────────────

        private static string GetMachineId()
        {
            try
            {
                string cpu = GetCpuId();
                string mac = GetMacAddress();
                string raw = cpu + "|" + mac;

                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                    var sb = new StringBuilder(16);
                    for (int i = 0; i < 8; i++)
                        sb.AppendFormat("{0:X2}", hash[i]);
                    return sb.ToString();
                }
            }
            catch { return Environment.MachineName; }
        }

        private static string GetHddSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string s = obj["SerialNumber"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(s) && s != "None")
                            return s.ToUpper();
                    }
                }
            }
            catch { }
            return "UNKNOWN";
        }

        private static string GetCpuId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string id = obj["ProcessorId"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(id)) return id;
                    }
                }
            }
            catch { }
            return Environment.MachineName;
        }

        private static string GetMacAddress()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT MACAddress FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True AND MACAddress IS NOT NULL"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string mac = obj["MACAddress"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(mac)) return mac;
                    }
                }
            }
            catch { }
            return "";
        }
    }
}

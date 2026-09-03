using System;
using System.Collections.Generic;
using System.IO;

namespace ChickenDist.Core
{
    /// <summary>
    /// إعدادات التطبيق — تُحفظ في ملف app_settings.ini بجانب الـ EXE
    /// مستقل عن ملف .exe.config حتى لا تُفقد الإعدادات عند التحديث
    /// </summary>
    public static class AppConfig
    {
        // المسار الثابت بجانب الـ EXE الحالي
        private static readonly string SettingsFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.ini");

        // ذاكرة مؤقتة لتفادي القراءة المتكررة من الملف
        private static Dictionary<string, string> _cache;

        // ===== خصائص الإعدادات =====

        public static string CompanyName
        {
            get => Get("CompanyName", "موزع قطع الغيار والمخازن");
            set => Set("CompanyName", value);
        }

        public static string BusinessType
        {
            get => Get("BusinessType", "Supermarket");
            set => Set("BusinessType", value);
        }

        public static bool IsRestaurant
        {
            get => BusinessType == "Restaurant";
        }

        public static bool IsManufacturing
        {
            get => BusinessType == "Factories" || BusinessType == "Manufacturing";
        }

        public static string AppTheme
        {
            get => Get("AppTheme", "Dark");
            set => Set("AppTheme", value);
        }

        public static string CompanyPhone1
        {
            get => Get("CompanyPhone1", "");
            set => Set("CompanyPhone1", value);
        }

        public static string CompanyPhone2
        {
            get => Get("CompanyPhone2", "");
            set => Set("CompanyPhone2", value);
        }

        public static string CompanyPhone
        {
            get => !string.IsNullOrEmpty(CompanyPhone1) ? CompanyPhone1 : CompanyPhone2;
        }

        public static string CompanyAddress
        {
            get => Get("CompanyAddress", "");
            set => Set("CompanyAddress", value);
        }

        public static string ReceiptPrintMode
        {
            get => Get("ReceiptPrintMode", "Detailed");
            set => Set("ReceiptPrintMode", value);
        }

        public static string ReceiptPrinterName
        {
            get => Get("ReceiptPrinterName", "");
            set => Set("ReceiptPrinterName", value);
        }

        public static string A4PrinterName
        {
            get => Get("A4PrinterName", "");
            set => Set("A4PrinterName", value);
        }

        public static string BarcodePrinterName
        {
            get => Get("BarcodePrinterName", "");
            set => Set("BarcodePrinterName", value);
        }

        public static string BarcodeStickerSize
        {
            get => Get("BarcodeStickerSize", "50x30");
            set => Set("BarcodeStickerSize", value);
        }

        public static string DefaultInvoiceFormat
        {
            get => Get("DefaultInvoiceFormat", "Receipt");
            set => Set("DefaultInvoiceFormat", value);
        }

        public static string DefaultReportFormat
        {
            get => Get("DefaultReportFormat", "A4");
            set => Set("DefaultReportFormat", value);
        }

        public static string PrintBehaviorOnSave
        {
            get => Get("PrintBehaviorOnSave", "Prompt"); // "Prompt" (سؤال واختيار), "Direct" (طباعة مباشرة فورية), "None" (عدم الطباعة تلقائياً)
            set => Set("PrintBehaviorOnSave", value);
        }

        public static string ReceiptTemplate
        {
            get => Get("ReceiptTemplate", "Standard");
            set => Set("ReceiptTemplate", value);
        }

        public static string A4Template
        {
            get => Get("A4Template", "AlTarekGrid");
            set => Set("A4Template", value);
        }

        public static string VoucherTemplate
        {
            get => Get("VoucherTemplate", "AlTarekVoucher");
            set => Set("VoucherTemplate", value);
        }

        public static string PreparationSlipTemplate
        {
            get => Get("PreparationSlipTemplate", "Disbursement"); // "Disbursement", "Standard", "Modern"
            set => Set("PreparationSlipTemplate", value);
        }

        public static string PreparationPaperSize
        {
            get => Get("PreparationPaperSize", "A4"); // "A4", "A5", "Receipt"
            set => Set("PreparationPaperSize", value);
        }

        public static int A5ShiftRight
        {
            get
            {
                // إزاحة طباعة A5 لليمين: 3.0 سم = 118 نقطة (سنة شمال لإظهار الجزء المقصوص على اليمين)
                string val = Get("A5ShiftRight", "118");
                return int.TryParse(val, out int s) ? s : 118;
            }
            set => Set("A5ShiftRight", value.ToString());
        }

        public static bool PreparationA5UseCustomShift
        {
            get => Get("PreparationA5UseCustomShift", "false") == "true";
            set => Set("PreparationA5UseCustomShift", value ? "true" : "false");
        }

        public static int PreparationA5ShiftRight
        {
            get
            {
                string val = Get("PreparationA5ShiftRight", "118");
                return int.TryParse(val, out int s) ? s : 118;
            }
            set => Set("PreparationA5ShiftRight", value.ToString());
        }

        /// <summary>
        /// الحصول على إزاحة إذن التحضير A5 (إما مخصصة أو مطابقة تلقائياً لإزاحة الفاتورة A5)
        /// </summary>
        public static int GetPreparationA5Shift()
        {
            return PreparationA5UseCustomShift ? PreparationA5ShiftRight : A5ShiftRight;
        }

        public static string BarcodeTemplate
        {
            get => Get("BarcodeTemplate", "Standard");
            set => Set("BarcodeTemplate", value);
        }

        public static string BarcodeEncoding
        {
            get => Get("BarcodeEncoding", "Code128");
            set => Set("BarcodeEncoding", value);
        }

        public static string ShopLogoPath
        {
            get => Get("ShopLogoPath", "");
            set => Set("ShopLogoPath", value);
        }

        public static bool PrintShopLogo
        {
            get => Get("PrintShopLogo", "false") == "true";
            set => Set("PrintShopLogo", value ? "true" : "false");
        }

        /// <summary>إظهار عمود الخصم في جدول أصناف الريسيت</summary>
        public static bool ReceiptShowDiscount
        {
            get => Get("ReceiptShowDiscount", "true") == "true";
            set => Set("ReceiptShowDiscount", value ? "true" : "false");
        }

        /// <summary>إظهار بيانات العميل (اسم + هاتف + عنوان) في رأس الريسيت</summary>
        public static bool ReceiptShowClientInfo
        {
            get => Get("ReceiptShowClientInfo", "false") == "true";
            set => Set("ReceiptShowClientInfo", value ? "true" : "false");
        }

        /// <summary>نص ترويسة/مقدمة الريسيت الحراري (أعلى الفاتورة)</summary>
        public static string ReceiptHeaderNote
        {
            get => Get("ReceiptHeaderNote", "");
            set => Set("ReceiptHeaderNote", value);
        }

        /// <summary>نص تذييل/خاتمة الريسيت الحراري (أسفل الفاتورة)</summary>
        public static string ReceiptFooterNote
        {
            get => Get("ReceiptFooterNote", "شكراً لتعاملكم معنا | نسعد بزيارتكم دائماً");
            set => Set("ReceiptFooterNote", value);
        }

        /// <summary>
        /// وضع طباعة رسيت البيع التلقائي من نقطة البيع
        /// Always = طباعة تلقائية بدون سؤال (الافتراضي)
        /// Ask = اسأل العميل كل مرة
        /// Never = لا تطبع رسيت أبداً (بون المطبخ فقط)
        /// </summary>
        public static string POSReceiptMode
        {
            get => Get("POSReceiptMode", "Always");
            set => Set("POSReceiptMode", value);
        }

        // ===== إعدادات Firebase السحابية الديناميكية لكل عميل =====

        public static string FirebaseApiKey
        {
            get => Get("FirebaseApiKey", "AIzaSyCjdqMOaMTn-6_DrAd62fXLcMlEqLqVzWk");
            set => Set("FirebaseApiKey", value);
        }

        public static string FirebaseProjectId
        {
            get => Get("FirebaseProjectId", "checkin-192ab");
            set => Set("FirebaseProjectId", value);
        }

        public static string FirebaseStorageBucket
        {
            get => Get("FirebaseStorageBucket", "checkin-192ab.firebasestorage.app");
            set => Set("FirebaseStorageBucket", value);
        }

        public static string FirebaseWebUrl
        {
            get => Get("FirebaseWebUrl", "https://checkin-192ab.web.app");
            set => Set("FirebaseWebUrl", value);
        }



        public static void SetPrinter(System.Drawing.Printing.PrintDocument pd, string printerName)
        {
            if (string.IsNullOrEmpty(printerName)) return;
            try
            {
                foreach (string p in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    if (string.Equals(p, printerName, StringComparison.OrdinalIgnoreCase))
                    {
                        pd.PrinterSettings.PrinterName = p;
                        break;
                    }
                }
            }
            catch { /* Ignored */ }
        }

        public static void SetPaperSize(System.Drawing.Printing.PrintDocument pd, string sizeKey)
        {
            try
            {
                bool is38x26 = (sizeKey == "38x26" || sizeKey == "38x26_double");
                bool is50x25 = (sizeKey == "50x25" || sizeKey == "50x25_double");
                int targetWidth = is38x26 ? 150 : is50x25 ? 200 : 200;
                int targetHeight = is38x26 ? 102 : is50x25 ? 98 : 120;

                string part1 = is38x26 ? "38" : "50";
                string part2 = is38x26 ? "26" : is50x25 ? "25" : "30";

                System.Drawing.Printing.PaperSize matchedSize = null;

                // 1. Search by name first (e.g. contains "38" and "26")
                foreach (System.Drawing.Printing.PaperSize size in pd.PrinterSettings.PaperSizes)
                {
                    string name = size.PaperName;
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (name.IndexOf(part1, StringComparison.OrdinalIgnoreCase) >= 0 && 
                            name.IndexOf(part2, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matchedSize = size;
                            break;
                        }
                    }
                }

                // 2. Search by dimensions with a tolerance of 10 hundredths of an inch
                if (matchedSize == null)
                {
                    foreach (System.Drawing.Printing.PaperSize size in pd.PrinterSettings.PaperSizes)
                    {
                        if (Math.Abs(size.Width - targetWidth) <= 10 && Math.Abs(size.Height - targetHeight) <= 10)
                        {
                            matchedSize = size;
                            break;
                        }
                    }
                }

                if (matchedSize != null)
                {
                    pd.DefaultPageSettings.PaperSize = matchedSize;
                    pd.PrinterSettings.DefaultPageSettings.PaperSize = matchedSize;
                }
                else
                {
                    var customSize = new System.Drawing.Printing.PaperSize("StickerLabel", targetWidth, targetHeight);
                    try
                    {
                        customSize.RawKind = 256; // Standard Custom RawKind
                    }
                    catch { }
                    pd.DefaultPageSettings.PaperSize = customSize;
                    pd.PrinterSettings.DefaultPageSettings.PaperSize = customSize;
                }

                // Apply margins and layout to both DefaultPageSettings and PrinterSettings
                var margins = new System.Drawing.Printing.Margins(5, 5, 5, 5);
                pd.DefaultPageSettings.Margins = margins;
                pd.PrinterSettings.DefaultPageSettings.Margins = margins;

                pd.DefaultPageSettings.Landscape = false;
                pd.PrinterSettings.DefaultPageSettings.Landscape = false;
            }
            catch { }
        }

        /// <summary>
        /// تشغيل أمر الطباعة في الخلفية (Background Thread) مع منع تجميد/تهنيج البرنامج
        /// وإخفاء نافذة الـ Popup المزعجة (الصفحة X من document) عبر StandardPrintController
        /// </summary>
        public static void PrintInBackground(System.Drawing.Printing.PrintDocument pd, Action onCompleted = null, Action<Exception> onError = null)
        {
            if (pd == null) return;
            try
            {
                pd.PrintController = new System.Drawing.Printing.StandardPrintController();
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        pd.Print();
                        onCompleted?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("PrintInBackground execution failed", ex, "Printer");
                        onError?.Invoke(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error("PrintInBackground initialization failed", ex, "Printer");
                onError?.Invoke(ex);
            }
        }

        public static int DriverPortalPort
        {
            get => int.TryParse(Get("DriverPortalPort", "8080"), out int p) ? p : 8080;
            set => Set("DriverPortalPort", value.ToString());
        }

        public static bool DriverPortalAutoStart
        {
            get => Get("DriverPortalAutoStart", "true") == "true";
            set => Set("DriverPortalAutoStart", value ? "true" : "false");
        }

        /// <summary>
        /// رمز عزل النشاط — يستخدم كجزء من مفتاح التشفير لضمان عدم تداخل بيانات الأنشطة المختلفة.
        /// يجب أن يكون فريداً لكل نشاط/شركة (مثال: ABC123 أو ChickenKing2026).
        /// يُحفظ في app_settings.ini ولا يتغير عند التحديث.
        /// </summary>
        public static string TenantKey
        {
            get => Get("TenantKey", "");
            set => Set("TenantKey", value);
        }

        // ===== إعدادات الميزان الإلكتروني =====
        public static bool ScaleEnabled
        {
            get => Get("ScaleEnabled", "true") == "true";
            set => Set("ScaleEnabled", value ? "true" : "false");
        }
        public static string ScaleComPort
        {
            get => Get("ScaleComPort", "COM1");
            set => Set("ScaleComPort", value);
        }
        public static int ScaleBaudRate
        {
            get => int.TryParse(Get("ScaleBaudRate", "9600"), out int b) ? b : 9600;
            set => Set("ScaleBaudRate", value.ToString());
        }

        // ===== إعدادات ميزان الباركود =====
        public static string BarcodeScalePrefix
        {
            get => Get("BarcodeScalePrefix", "20,99,21,22,27,9");
            set => Set("BarcodeScalePrefix", value);
        }
        public static int BarcodeScaleItemCodeLength
        {
            get => int.TryParse(Get("BarcodeScaleItemCodeLength", "5"), out int len) ? len : 5;
            set => Set("BarcodeScaleItemCodeLength", value.ToString());
        }
        public static int BarcodeScaleWeightLength
        {
            get => int.TryParse(Get("BarcodeScaleWeightLength", "5"), out int len) ? len : 5;
            set => Set("BarcodeScaleWeightLength", value.ToString());
        }
        public static decimal BarcodeScaleDivideBy
        {
            get => decimal.TryParse(Get("BarcodeScaleDivideBy", "1000"), out decimal d) ? d : 1000m;
            set => Set("BarcodeScaleDivideBy", value.ToString());
        }

        public static string TelegramBotToken
        {
            get => Get("TelegramBotToken", "");
            set => Set("TelegramBotToken", value);
        }

        public static string TelegramChatId
        {
            get => Get("TelegramChatId", "");
            set => Set("TelegramChatId", value);
        }

        public static bool BackupOnExit
        {
            get => Get("BackupOnExit", "False") == "True";
            set => Set("BackupOnExit", value ? "True" : "False");
        }

        public static string BackupLocalPath
        {
            get => Get("BackupLocalPath", "");
            set => Set("BackupLocalPath", value);
        }

        public static string WhatsAppBackupPhone
        {
            get => Get("WhatsAppBackupPhone", "");
            set => Set("WhatsAppBackupPhone", value);
        }

        public static string WhatsAppInvoiceTemplate
        {
            get => Get("WhatsAppInvoiceTemplate", "Detailed");
            set => Set("WhatsAppInvoiceTemplate", value);
        }

        public static int BackupIntervalHours
        {
            get => int.TryParse(Get("BackupIntervalHours", "0"), out int val) ? val : 0;
            set => Set("BackupIntervalHours", value.ToString());
        }

        public static bool EnableCratesTracking
        {
            get => Get("EnableCratesTracking", "True") == "True";
            set => Set("EnableCratesTracking", value ? "True" : "False");
        }

        // ===== إعدادات نقاط الولاء =====
        public static bool LoyaltyEnabled
        {
            get => Get("LoyaltyEnabled", "False") == "True";
            set => Set("LoyaltyEnabled", value ? "True" : "False");
        }

        /// <summary>كل كم جنيه يكسب العميل نقطة واحدة (افتراضي: كل 10 جنيه = 1 نقطة)</summary>
        public static decimal LoyaltyPointsPerCurrency
        {
            get => decimal.TryParse(Get("LoyaltyPointsPerCurrency", "10"), out decimal v) ? v : 10m;
            set => Set("LoyaltyPointsPerCurrency", value.ToString());
        }

        /// <summary>قيمة النقطة عند الاسترداد (افتراضي: 1 نقطة = 0.10 جنيه)</summary>
        public static decimal LoyaltyRedemptionRate
        {
            get => decimal.TryParse(Get("LoyaltyRedemptionRate", "0.1"), out decimal v) ? v : 0.1m;
            set => Set("LoyaltyRedemptionRate", value.ToString());
        }

        // ===== إعدادات الوردية =====
        public static bool ShiftRequired
        {
            get => Get("ShiftRequired", "False") == "True";
            set => Set("ShiftRequired", value ? "True" : "False");
        }

        // ===== إعدادات الصلاحية =====
        public static bool AllowSellExpired
        {
            get => Get("AllowSellExpired", "True") == "True";
            set => Set("AllowSellExpired", value ? "True" : "False");
        }


        // ===== دوال القراءة والكتابة =====

        public static string Get(string key, string defaultValue)
        {
            EnsureLoaded();
            return _cache.TryGetValue(key, out string val) ? val : defaultValue;
        }

        public static void Set(string key, string value)
        {
            EnsureLoaded();
            _cache[key] = value ?? "";
            Save();
        }

        private static void EnsureLoaded()
        {
            if (_cache != null) return;
            _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // قراءة الملف لو موجود
            if (File.Exists(SettingsFile))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(SettingsFile, System.Text.Encoding.UTF8))
                    {
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                            continue;
                        int idx = trimmed.IndexOf('=');
                        if (idx > 0)
                        {
                            string k = trimmed.Substring(0, idx).Trim();
                            string v = trimmed.Substring(idx + 1).Trim();
                            _cache[k] = v;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("فشل قراءة ملف الإعدادات app_settings.ini", ex, "AppConfig");
                }
            }
            else
            {
                // ترحيل الإعدادات القديمة من App.config لو موجودة
                MigrateFromAppConfig();
            }
        }

        private static void Save()
        {
            try
            {
                var lines = new List<string>
                {
                    "; ملف إعدادات تطبيق موزع قطع الغيار والمخازن",
                    "; لا تحذف هذا الملف — يحفظ إعدادات الشركة وتبقى بعد التحديث",
                    ""
                };
                foreach (var kv in _cache)
                    lines.Add($"{kv.Key}={kv.Value}");

                File.WriteAllLines(SettingsFile, lines, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "فشل حفظ الإعدادات:\n" + ex.Message, "خطأ",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ترحيل الإعدادات القديمة من App.config (للتوافق مع الإصدارات القديمة)
        /// </summary>
        private static void MigrateFromAppConfig()
        {
            try
            {
                var configFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                if (!File.Exists(configFile)) return;

                // قراءة بسيطة من App.config بدون ConfigurationManager
                string content = File.ReadAllText(configFile);
                ExtractAppConfigValue(content, "CompanyName");
                ExtractAppConfigValue(content, "ReceiptPrintMode");

                if (_cache.Count > 0)
                    Save(); // حفظ المُرحَّل في الملف الجديد
            }
            catch { /* لا نوقف البرنامج لو التحويل فشل */ }
        }

        private static void ExtractAppConfigValue(string xml, string key)
        {
            string search = $"key=\"{key}\"";
            int pos = xml.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (pos < 0) return;
            int valPos = xml.IndexOf("value=\"", pos, StringComparison.OrdinalIgnoreCase);
            if (valPos < 0) return;
            valPos += 7;
            int endPos = xml.IndexOf("\"", valPos);
            if (endPos < 0) return;
            string value = xml.Substring(valPos, endPos - valPos);
            if (!string.IsNullOrEmpty(value))
                _cache[key] = value;
        }

        /// <summary>
        /// إعادة تحميل الإعدادات من الملف (مفيد بعد التحديث)
        /// </summary>
        public static void Reload()
        {
            _cache = null;
            EnsureLoaded();
        }
    }
}

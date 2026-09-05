using System;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    /// <summary>
    /// يدير عمليات النسخ الاحتياطي لقاعدة البيانات ويتحقق من حالتها
    /// </summary>
    public static class BackupManager
    {
        // ===== الإعدادات =====

        /// <summary>الحد الأقصى للوقت بدون نسخة احتياطية قبل إظهار التحذير (بالساعات)</summary>
        private const int WarningAfterHours = 24;

        /// <summary>المفتاح المستخدم لحفظ مسار مجلد الباكب في الإعدادات</summary>
        private const string BackupFolderKey = "BackupFolder";

        /// <summary>المفتاح المستخدم لتخزين وقت آخر باكب ناجح</summary>
        private const string LastBackupTimeKey = "LastBackupTime";

        // ===== خصائص عامة =====

        public static string BackupFolder
        {
            get
            {
                string saved = AppConfig.Get(BackupFolderKey, "");
                if (string.IsNullOrWhiteSpace(saved))
                {
                    // المسار الافتراضي: مجلد Backup بجانب الـ EXE
                    saved = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
                }
                return saved;
            }
            set => AppConfig.Set(BackupFolderKey, value);
        }

        public static DateTime? LastBackupTime
        {
            get
            {
                string val = AppConfig.Get(LastBackupTimeKey, "");
                if (DateTime.TryParse(val, out DateTime dt))
                    return dt;
                return null;
            }
            private set
            {
                AppConfig.Set(LastBackupTimeKey, value?.ToString("o") ?? "");
            }
        }

        // ===== التحقق من حالة الباكب =====

        /// <summary>
        /// هل الباكب متأخر؟ (مضى أكثر من 24 ساعة أو لا يوجد باكب سابق)
        /// </summary>
        public static bool IsBackupOverdue()
        {
            var last = LastBackupTime;
            if (last == null) return true;
            return (DateTime.Now - last.Value).TotalHours >= WarningAfterHours;
        }

        /// <summary>
        /// يعرض رسالة تحذير عند الفتح إذا كان الباكب متأخراً.
        /// يُستدعى من Program.cs عند بدء التشغيل.
        /// </summary>
        public static void CheckAndWarnIfOverdue()
        {
            if (!IsBackupOverdue()) return;

            var last = LastBackupTime;
            string lastStr = last.HasValue
                ? last.Value.ToString("dd/MM/yyyy hh:mm tt")
                : "لم يتم عمل نسخة احتياطية من قبل";

            string msg =
                "⚠️ تحذير: النسخ الاحتياطي لقاعدة البيانات متأخر!\n\n" +
                $"آخر نسخة احتياطية: {lastStr}\n\n" +
                "يُنصح بعمل نسخة احتياطية الآن من قائمة:\n" +
                "الإعدادات → النسخ الاحتياطي\n\n" +
                "هل تريد عمل نسخة احتياطية الآن؟";

            var result = MessageBox.Show(
                msg,
                "⚠️ تحذير النسخ الاحتياطي",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

            if (result == DialogResult.Yes)
            {
                DoBackup(silent: false);
            }
        }

        // ===== تنفيذ الباكب =====

        /// <summary>
        /// ينفذ نسخة احتياطية كاملة لقاعدة البيانات ويقوم بضغطها ورفعها للتلجرام.
        /// يُرجع true لو نجح.
        /// </summary>
        public static bool DoBackup(bool silent = false)
        {
            try
            {
                // إنشاء مجلد الباكب لو مش موجود
                string folder = BackupFolder;
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // اسم الملف يتضمن التاريخ والوقت
                string fileName = $"ProSoft_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string fullPath = Path.Combine(folder, fileName);

                // تنفيذ أمر BACKUP DATABASE
                string sql = $@"
                    BACKUP DATABASE [ProSoftDB]
                    TO DISK = N'{fullPath}'
                    WITH FORMAT, INIT,
                         NAME = N'ProSoft Backup',
                         SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                using (var con = DbHelper.GetConnection())
                {
                    con.Open();
                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandTimeout = 300; // 5 دقائق للباكب الكبير
                        cmd.ExecuteNonQuery();
                    }
                }

                // ضغط الملف لـ ZIP
                string zipPath = Path.ChangeExtension(fullPath, ".zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(fullPath, Path.GetFileName(fullPath));
                }

                // حذف ملف الـ .bak المؤقت لتوفير المساحة
                try { File.Delete(fullPath); } catch { }

                // نسخ للمسار المحلي السحابي (لو مُهيأ)
                string localCloudPath = AppConfig.BackupLocalPath;
                if (!string.IsNullOrWhiteSpace(localCloudPath) && Directory.Exists(localCloudPath))
                {
                    try
                    {
                        string localCloudDest = Path.Combine(localCloudPath, Path.GetFileName(zipPath));
                        File.Copy(zipPath, localCloudDest, true);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("BackupManager: فشل نسخ الملف للمسار المحلي السحابي", ex, "BackupManager");
                    }
                }

                // إرسال عبر الواتساب (لو مهيأ)
                bool waSuccess = false;
                string waError = "";
                if (!string.IsNullOrWhiteSpace(AppConfig.WhatsAppBackupPhone))
                {
                    waSuccess = UploadToWhatsApp(zipPath, out waError);
                }

                // رفع النسخة الاحتياطية للسحاب تلقائياً وإتاحتها للتحميل من تطبيق المالك
                bool cloudSuccess = false;
                string cloudError = "";
                try
                {
                    cloudSuccess = UploadToCloudBackup(zipPath, out cloudError);
                }
                catch (Exception ex)
                {
                    cloudError = ex.Message;
                }

                // حفظ وقت آخر باكب ناجح
                LastBackupTime = DateTime.Now;

                // حذف الملفات القديمة (الاحتفاظ بأحدث 5 ملفات فقط على مدار اليوم)
                CleanOldBackups(folder, keepCount: 5);

                // إشعار المزامنة السحابية بتحديث بيانات الباكب في الخلفية
                System.Threading.ThreadPool.QueueUserWorkItem(async _ =>
                {
                    try { await ChickenDist.Services.CloudSyncService.PushLiveStatsToFirebaseAsync(); } catch { }
                });

                if (!silent)
                {
                    string successMsg = $"✅ تم عمل النسخة الاحتياطية بنجاح وضغطها لملف ZIP!\n\nالملف:\n{zipPath}";
                    if (cloudSuccess)
                    {
                        successMsg += "\n\n☁️ تم رفع وتأمين النسخة سحابياً وإتاحتها للتحميل من تطبيق المالك! ✅";
                    }
                    else
                    {
                        successMsg += $"\n\n⚠️ فشل الرفع السحابي التلقائي للنسخة: {cloudError}";
                    }

                    if (!string.IsNullOrWhiteSpace(localCloudPath) && Directory.Exists(localCloudPath))
                    {
                        successMsg += "\n\n📂 تم نسخ الملف للمجلد المحلي السحابي بنجاح!";
                    }
                    if (!string.IsNullOrWhiteSpace(AppConfig.WhatsAppBackupPhone))
                    {
                        if (waSuccess)
                            successMsg += "\n\n📬 تم إرسال النسخة الاحتياطية للواتساب بنجاح!";
                        else
                            successMsg += $"\n\n⚠️ فشل الإرسال عبر الواتساب: {waError}";
                    }

                    MessageBox.Show(
                        successMsg,
                        "النسخ الاحتياطي",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                }

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("BackupManager.DoBackup فشل", ex, "BackupManager");

                if (!silent)
                {
                    MessageBox.Show(
                        $"❌ فشل عمل النسخة الاحتياطية:\n\n{ex.Message}\n\n" +
                        "تأكد من:\n" +
                        "• صلاحيات الكتابة على مجلد الباكب\n" +
                        "• أن SQL Server يعمل\n" +
                        "• المساحة الكافية على القرص",
                        "خطأ في النسخ الاحتياطي",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                }

                return false;
            }
        }

        /// <summary>
        /// يرفع ملف النسخة الاحتياطية إلى التلجرام باستخدام Bot API
        /// </summary>
        /// <summary>
        /// يرسل ملف النسخة الاحتياطية إلى خادر البوت المحلي بالواتساب
        /// </summary>
        public static bool UploadToWhatsApp(string filePath, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                string phone = AppConfig.WhatsAppBackupPhone;
                if (string.IsNullOrWhiteSpace(phone))
                {
                    errorMessage = "رقم هاتف الواتساب للنسخ الاحتياطي غير مهيأ.";
                    return false;
                }

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    string escapedPath = filePath.Replace("\\", "\\\\");
                    string json = "{" +
                                  "\"fields\": {" +
                                  "\"type\": {\"stringValue\": \"send_backup\"}," +
                                  "\"filePath\": {\"stringValue\": \"" + escapedPath + "\"}," +
                                  "\"phone\": {\"stringValue\": \"" + phone + "\"}," +
                                  "\"status\": {\"stringValue\": \"pending\"}," +
                                  "\"time\": {\"stringValue\": \"" + DateTime.UtcNow.ToString("o") + "\"}" +
                                  "}" +
                                  "}";
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = httpClient.PostAsync($"https://firestore.googleapis.com/v1/projects/{AppConfig.FirebaseProjectId}/databases/(default)/documents/commands", content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        errorMessage = $"Firebase Error ({response.StatusCode}): {responseContent}";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// ينفذ نسخة احتياطية تلقائية عند إغلاق البرنامج لو كان الخيار مفعلاً
        /// </summary>
        public static void AutoBackupOnExit()
        {
            if (AppConfig.BackupOnExit)
            {
                DoBackup(silent: true);
            }
        }

        /// <summary>
        /// يحذف الملفات القديمة، يحتفظ بأحدث keepCount ملفات فقط (الافتراضي 5 ملفات بحد أقصى على مدار اليوم)
        /// </summary>
        public static void CleanOldBackups(string folder, int keepCount = 5)
        {
            try
            {
                var dir = new DirectoryInfo(folder);
                if (!dir.Exists) return;

                // 1. تنظيف ملفات .bak
                var files = dir.GetFiles("*Backup_*.bak");
                Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                for (int i = keepCount; i < files.Length; i++)
                {
                    try { files[i].Delete(); } catch { }
                }

                // 2. تنظيف ملفات .zip (الاحتفاظ بأحدث 5 نسخ فقط)
                var zipFiles = dir.GetFiles("*Backup_*.zip");
                Array.Sort(zipFiles, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                for (int i = keepCount; i < zipFiles.Length; i++)
                {
                    try { zipFiles[i].Delete(); } catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// يجلب تفاصيل أحدث نسخة احتياطية محلية موجودة على القرص
        /// </summary>
        public static (string fileName, double sizeMB, string formattedTime) GetLatestBackupInfo()
        {
            try
            {
                string folder = BackupFolder;
                if (Directory.Exists(folder))
                {
                    var zipFiles = new DirectoryInfo(folder).GetFiles("*Backup_*.zip");
                    if (zipFiles.Length > 0)
                    {
                        Array.Sort(zipFiles, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                        var latest = zipFiles[0];
                        double mb = Math.Round(latest.Length / (1024.0 * 1024.0), 2);
                        string ft = latest.LastWriteTime.ToString("yyyy/MM/dd hh:mm tt");
                        return (latest.Name, mb, ft);
                    }
                }
            }
            catch { }
            return ("ProSoft_Backup.zip", 0.0, "");
        }

        /// <summary>
        /// يفتح مجلد الباكب في مستكشف الملفات
        /// </summary>
        public static void OpenBackupFolder()
        {
            try
            {
                string folder = BackupFolder;
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل فتح مجلد الباكب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// يرفع ملف النسخة الاحتياطية إلى سحابة Firebase Realtime Cloud مضغوطاً ومشفر Base64
        /// مع تطبيق سياسة الاحتفاظ بحد أقصى 5 نسخ سحابية فقط على مدار اليوم
        /// </summary>
        public static bool UploadToCloudBackup(string filePath, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = "ملف النسخة الاحتياطية غير موجود على القرص.";
                    return false;
                }

                FileInfo fi = new FileInfo(filePath);
                double sizeKB = Math.Round(fi.Length / 1024.0, 1);
                double sizeMB = Math.Round(fi.Length / (1024.0 * 1024.0), 2);
                string fileName = Path.GetFileName(filePath);
                string backupId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string formattedTime = DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");

                // تحويل محتويات ملف الـ ZIP المضغوط إلى Base64
                byte[] fileBytes = File.ReadAllBytes(filePath);
                string base64Data = Convert.ToBase64String(fileBytes);

                string projectId = AppConfig.Get("FirebaseProjectId", "elra7ma-grop");
                if (string.IsNullOrWhiteSpace(projectId)) projectId = "elra7ma-grop";

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(3);

                    // 1. تجهيز حزمة النسخة الأحدث (Latest Backup Payload)
                    string latestPayload = "{" +
                        "\"BackupId\": \"" + backupId + "\"," +
                        "\"FileName\": \"" + fileName + "\"," +
                        "\"FileSizeKB\": " + sizeKB.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"FileSizeMB\": " + sizeMB.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"BackupDate\": \"" + nowIso + "\"," +
                        "\"FormattedTime\": \"" + formattedTime + "\"," +
                        "\"Base64Data\": \"" + base64Data + "\"" +
                        "}";

                    var content = new System.Net.Http.StringContent(latestPayload, System.Text.Encoding.UTF8, "application/json");

                    // الرفع للمسار الأساسي default-rtdb
                    bool uploadOk = false;
                    try
                    {
                        var resp = httpClient.PutAsync($"https://{projectId}-default-rtdb.firebaseio.com/cloud_backups/latest.json", content).Result;
                        if (resp != null && resp.IsSuccessStatusCode)
                        {
                            uploadOk = true;
                        }
                    }
                    catch { }

                    // تجربة الرابط الاحتياطي
                    if (!uploadOk)
                    {
                        try
                        {
                            var contentFallback = new System.Net.Http.StringContent(latestPayload, System.Text.Encoding.UTF8, "application/json");
                            var respFallback = httpClient.PutAsync($"https://{projectId}.firebaseio.com/cloud_backups/latest.json", contentFallback).Result;
                            if (respFallback != null && respFallback.IsSuccessStatusCode)
                            {
                                uploadOk = true;
                            }
                        }
                        catch { }
                    }

                    if (!uploadOk)
                    {
                        errorMessage = "تعذر إرسال ملف الباكب إلى سيرفر Firebase Realtime Database.";
                        return false;
                    }

                    // 2. تحديث سجل التاريخ السحابي وإدارة سياسة الـ 5 نسخ كحد أقصى
                    try
                    {
                        string metaPayload = "{" +
                            "\"BackupId\": \"" + backupId + "\"," +
                            "\"FileName\": \"" + fileName + "\"," +
                            "\"FileSizeMB\": " + sizeMB.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                            "\"BackupDate\": \"" + nowIso + "\"," +
                            "\"FormattedTime\": \"" + formattedTime + "\"" +
                            "}";
                        var metaContent = new System.Net.Http.StringContent(metaPayload, System.Text.Encoding.UTF8, "application/json");
                        httpClient.PutAsync($"https://{projectId}-default-rtdb.firebaseio.com/cloud_backups/history/{backupId}.json", metaContent).Wait();

                        // فحص النسخ السحابية القديمة وحذف ما يتجاوز الـ 5 نسخ
                        CleanOldCloudBackups(httpClient, projectId, keepCount: 5);
                    }
                    catch (Exception exHist)
                    {
                        AppLogger.Error("فشل تحديث سجل الباكب السحابي", exHist, "BackupManager");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// يحذف النسخ الاحتياطية السحابية القديمة ويحتفظ بحد أقصى keepCount نسخ (5 نسخ)
        /// </summary>
        private static void CleanOldCloudBackups(System.Net.Http.HttpClient client, string projectId, int keepCount = 5)
        {
            try
            {
                var resp = client.GetAsync($"https://{projectId}-default-rtdb.firebaseio.com/cloud_backups/history.json?shallow=true").Result;
                if (resp != null && resp.IsSuccessStatusCode)
                {
                    string json = resp.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrWhiteSpace(json) && json.StartsWith("{"))
                    {
                        var matches = System.Text.RegularExpressions.Regex.Matches(json, "\"([^\"]+)\"\\s*:");
                        var keys = new System.Collections.Generic.List<string>();
                        foreach (System.Text.RegularExpressions.Match m in matches)
                        {
                            if (m.Groups.Count > 1) keys.Add(m.Groups[1].Value);
                        }

                        if (keys.Count > keepCount)
                        {
                            keys.Sort(); // ترتيب تصاعدي زمني لحذف الأقدم
                            int toDelete = keys.Count - keepCount;
                            for (int i = 0; i < toDelete; i++)
                            {
                                string oldKey = keys[i];
                                try
                                {
                                    client.DeleteAsync($"https://{projectId}-default-rtdb.firebaseio.com/cloud_backups/history/{oldKey}.json").Wait();
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}

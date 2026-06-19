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
                string fileName = $"ChickenDist_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string fullPath = Path.Combine(folder, fileName);

                // تنفيذ أمر BACKUP DATABASE
                string sql = $@"
                    BACKUP DATABASE [ChickenDist]
                    TO DISK = N'{fullPath}'
                    WITH FORMAT, INIT,
                         NAME = N'ChickenDist Backup',
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

                // حفظ وقت آخر باكب ناجح
                LastBackupTime = DateTime.Now;

                // حذف الملفات القديمة (الاحتفاظ بأحدث 10 ملفات فقط)
                CleanOldBackups(folder, keepCount: 10);

                if (!silent)
                {
                    string successMsg = $"✅ تم عمل النسخة الاحتياطية بنجاح وضغطها لملف ZIP!\n\nالملف:\n{zipPath}";
                    if (!string.IsNullOrWhiteSpace(localCloudPath) && Directory.Exists(localCloudPath))
                    {
                        successMsg += "\n\n☁️ تم نسخ الملف للمجلد المحلي السحابي بنجاح!";
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
                    httpClient.Timeout = TimeSpan.FromMinutes(10);
                    string escapedPath = filePath.Replace("\\", "\\\\");
                    string json = $"{{\"filePath\":\"{escapedPath}\",\"phone\":\"{phone}\"}}";
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = httpClient.PostAsync("http://localhost:5000/api/backup", content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        errorMessage = $"Server Error ({response.StatusCode}): {responseContent}";
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
        /// يحذف الملفات القديمة، يحتفظ بأحدث keepCount ملفات فقط
        /// </summary>
        private static void CleanOldBackups(string folder, int keepCount)
        {
            try
            {
                var files = new DirectoryInfo(folder).GetFiles("ChickenDist_Backup_*.bak");
                Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                for (int i = keepCount; i < files.Length; i++)
                {
                    try { files[i].Delete(); } catch { /* تجاهل أخطاء الحذف */ }
                }

                var zipFiles = new DirectoryInfo(folder).GetFiles("ChickenDist_Backup_*.zip");
                Array.Sort(zipFiles, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                for (int i = keepCount; i < zipFiles.Length; i++)
                {
                    try { zipFiles[i].Delete(); } catch { /* تجاهل أخطاء الحذف */ }
                }
            }
            catch { /* لا نوقف البرنامج */ }
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
    }
}

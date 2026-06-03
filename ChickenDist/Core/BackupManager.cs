using System;
using System.Data.SqlClient;
using System.IO;
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
        /// ينفذ نسخة احتياطية كاملة لقاعدة البيانات.
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

                // حفظ وقت آخر باكب ناجح
                LastBackupTime = DateTime.Now;

                // حذف الملفات القديمة (الاحتفاظ بأحدث 10 ملفات فقط)
                CleanOldBackups(folder, keepCount: 10);

                if (!silent)
                {
                    MessageBox.Show(
                        $"✅ تم عمل النسخة الاحتياطية بنجاح!\n\nالملف:\n{fullPath}",
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

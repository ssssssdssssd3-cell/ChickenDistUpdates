using System;
using System.IO;
using System.Diagnostics;

namespace ChickenDist.Core
{
    /// <summary>
    /// مسجّل أحداث التطبيق — يكتب سجلات الأخطاء والمراجعة في ملف نصي.
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _lock = new object();

        private static string LogPath
        {
            get
            {
                try
                {
                    string dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    return Path.Combine(dir, "app.log");
                }
                catch
                {
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                }
            }
        }

        /// <summary>تسجيل خطأ مع تفاصيل الـ Exception</summary>
        public static void Error(string message, Exception ex, string source = "")
        {
            string text = string.IsNullOrEmpty(source)
                ? $"{message}\n  النوع: {ex?.GetType().Name}\n  الرسالة: {ex?.Message}\n  StackTrace: {ex?.StackTrace}"
                : $"[{source}] {message}\n  النوع: {ex?.GetType().Name}\n  الرسالة: {ex?.Message}\n  StackTrace: {ex?.StackTrace}";
            Write("ERROR", text);
        }

        /// <summary>تسجيل حدث مراجعة (Audit) لتتبع العمليات الحساسة</summary>
        public static void Audit(string action, string details)
        {
            string user = Session.EmpID > 0 ? $"{Session.EmpName} ({Session.UserName})" : "غير معروف";
            Write("AUDIT", $"{action} | المستخدم: {user} | {details}");
        }

        /// <summary>تسجيل رسالة معلوماتية</summary>
        public static void Info(string message)
        {
            Write("INFO", message);
        }

        // ── الكتابة الفعلية في الملف ───────────────────────────────────────────
        private static void Write(string level, string text)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {text}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}";
                lock (_lock)
                {
                    File.AppendAllText(LogPath, line, System.Text.Encoding.UTF8);
                }

                // تدوير الملف إذا تجاوز 5 ميجابايت
                RotateIfNeeded();
            }
            catch
            {
                // تجاهل أخطاء الكتابة لتجنب تعطل البرنامج
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                lock (_lock)
                {
                    string path = LogPath;
                    var fi = new FileInfo(path);
                    if (fi.Exists && fi.Length > 5 * 1024 * 1024) // 5 MB
                    {
                        string archive = Path.ChangeExtension(path,
                            $".{DateTime.Now:yyyyMMdd_HHmmss}.log");
                        File.Move(path, archive);

                        // Clean up old archives, keep only the most recent 3
                        string dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir))
                        {
                            var logFiles = new DirectoryInfo(dir).GetFiles("app.*.log");
                            if (logFiles.Length > 3)
                            {
                                Array.Sort(logFiles, (a, b) => b.CreationTime.CompareTo(a.CreationTime));
                                for (int i = 3; i < logFiles.Length; i++)
                                {
                                    try { logFiles[i].Delete(); } catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}

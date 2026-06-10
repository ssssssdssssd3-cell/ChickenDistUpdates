using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;

namespace ChickenDist.Core
{
    public static class UpdateManager
    {
        // الإصدار الحالي للبرنامج
        public const string CurrentVersion = "1.4.5";
        
        // رابط ملف التحديث النصي على GitHub
        private const string UpdateUrl = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/update.txt";

        public static void CheckForUpdates(bool showNoUpdateMsg = false)
        {
            try
            {
                using (var client = new WebClient())
                {
                    // تفعيل بروتوكول TLS 1.2 والبروتوكولات الأخرى للاتصال الآمن بـ GitHub
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                    client.Encoding = System.Text.Encoding.UTF8;
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
                    
                    // تحميل بيانات ملف التحديث
                    string rawData = client.DownloadString(UpdateUrl);
                    string remoteVersion = "";
                    string downloadUrl = "";
                    string changelog = "";

                    string[] lines = rawData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        int index = line.IndexOf('=');
                        if (index > 0)
                        {
                            string key = line.Substring(0, index).Trim().ToLower();
                            string val = line.Substring(index + 1).Trim();
                            if (key == "version") remoteVersion = val;
                            else if (key == "url") downloadUrl = val;
                            else if (key == "changelog") changelog = val;
                        }
                    }

                    if (string.IsNullOrEmpty(remoteVersion) || string.IsNullOrEmpty(downloadUrl))
                    {
                        if (showNoUpdateMsg)
                        {
                            MessageBox.Show("لم يتم العثور على معلومات تحديث صالحة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        return;
                    }

                    // مقارنة الإصدار الحالي بالجديد
                    Version local = new Version(CurrentVersion);
                    Version remote = new Version(remoteVersion);

                    if (remote > local)
                    {
                        var result = MessageBox.Show(
                            $"🔄 يوجد تحديث جديد متاح للبرنامج!\n\n" +
                            $"الإصدار الحالي لديك:  {CurrentVersion}\n" +
                            $"الإصدار الجديد المتاح: {remoteVersion}\n\n" +
                            $"📝 ما الجديد في هذا الإصدار:\n{changelog}\n\n" +
                            $"هل ترغب في تنزيل التحديث وتثبيته الآن؟",
                            "تحديث جديد متاح",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading
                        );

                        if (result == DialogResult.Yes)
                        {
                            PerformUpdate(downloadUrl, remoteVersion);
                        }
                    }
                    else
                    {
                        if (showNoUpdateMsg)
                        {
                            MessageBox.Show(
                                $"✅ أنت تستخدم أحدث إصدار بالفعل.\n\n" +
                                $"إصدارك الحالي: {CurrentVersion}\n\n" +
                                $"📝 آخر التحديثات في هذا الإصدار:\n{changelog}",
                                "تحديث البرنامج",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information,
                                MessageBoxDefaultButton.Button1,
                                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("CheckForUpdates failed", ex);
                if (showNoUpdateMsg)
                {
                    MessageBox.Show("فشل الاتصال بسيرفر التحديث للتأكد من وجود إصدار جديد:\n" + ex.Message, "خطأ في التحديث", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ── كتابة سجل الأخطاء ─────────────────────────────────────────────────
        private static void WriteLog(string context, Exception ex)
        {
            try
            {
                string logPath = Path.Combine(
                    Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                    "update_log.txt");
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}]\r\n" +
                               $"  النوع: {ex.GetType().Name}\r\n" +
                               $"  الرسالة: {ex.Message}\r\n" +
                               $"  StackTrace: {ex.StackTrace}\r\n" +
                               new string('-', 60) + "\r\n";
                File.AppendAllText(logPath, entry, System.Text.Encoding.UTF8);
            }
            catch { /* لا نريد خطأ داخل معالج الخطأ */ }
        }

        private static void PerformUpdate(string downloadUrl, string remoteVersion)
        {
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            string currentDir    = Path.GetDirectoryName(currentExePath);
            
            string updatesDir = Path.Combine(currentDir, "Updates");
            string versionedExeName = $"ChickenDist_{remoteVersion}.exe";
            string newExePath = Path.Combine(updatesDir, versionedExeName);

            int maxRetries = 3;
            bool downloaded = false;
            Exception lastEx = null;

            // إظهار نافذة تحميل
            using (var progressForm = new Form())
            {
                progressForm.Text = "جاري تحميل التحديث...";
                progressForm.Size = new System.Drawing.Size(400, 130);
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.MaximizeBox = false;
                progressForm.MinimizeBox = false;
                progressForm.RightToLeft = RightToLeft.Yes;
                progressForm.RightToLeftLayout = true;

                var lbl = new Label
                {
                    Text = "جاري تحميل الملفات الجديدة من السيرفر، يرجى الانتظار...",
                    AutoSize = true,
                    Location = new System.Drawing.Point(20, 20)
                };
                var pb = new ProgressBar
                {
                    Width = 340,
                    Height = 23,
                    Location = new System.Drawing.Point(20, 50),
                    Style = ProgressBarStyle.Marquee
                };

                progressForm.Controls.Add(lbl);
                progressForm.Controls.Add(pb);
                progressForm.Show();
                progressForm.Refresh();

                try
                {
                    if (!Directory.Exists(updatesDir))
                        Directory.CreateDirectory(updatesDir);
                }
                catch (Exception ex)
                {
                    progressForm.Close();
                    MessageBox.Show("فشل إنشاء مجلد التحديثات Updates:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        lbl.Text = $"محاولة {attempt} من {maxRetries}...";
                        progressForm.Refresh();

                        if (File.Exists(newExePath))
                        {
                            try
                            {
                                File.Delete(newExePath);
                            }
                            catch { }
                        }

                        using (var client = new WebClient())
                        {
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                            client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
                            client.DownloadFile(downloadUrl, newExePath);
                        }
                        downloaded = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        WriteLog($"Download attempt {attempt} failed", ex);
                        System.Threading.Thread.Sleep(1500);
                    }
                }

                progressForm.Close();
            }

            if (downloaded)
            {
                try
                {
                    if (!File.Exists(newExePath))
                    {
                        throw new Exception("فشل حفظ ملف التحديث");
                    }
                    FileInfo fi = new FileInfo(newExePath);
                    if (fi.Length < 100000)
                    {
                        throw new Exception("ملف التحديث تالف أو غير مكتمل");
                    }
                }
                catch (Exception ex)
                {
                    downloaded = false;
                    lastEx = ex;
                }
            }

            if (!downloaded)
            {
                MessageBox.Show(
                    $"فشل تنزيل التحديث بعد {maxRetries} محاولات.\n\nالسبب: {lastEx?.Message}",
                    "خطأ في التحديث",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return;
            }

            try
            {
                MessageBox.Show(
                    "✅ تم تحميل التحديث بنجاح!\n\n" +
                    $"📁 تم حفظ ملف البرنامج الجديد باسم {versionedExeName} داخل مجلد (Updates) في مسار تثبيت البرنامج.\n\n" +
                    "سيتم الآن تحديد الملف الجديد تلقائياً وإغلاق البرنامج الحالي لتتمكن من نقل (نسخ واستبدال) الملف الجديد بالملف الحالي بسهولة.",
                    "اكتمل تحميل التحديث",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading
                );

                // فتح المجلد في المستكشف وتحديد الملف
                Process.Start("explorer.exe", $"/select,\"{newExePath}\"");
                
                // إغلاق البرنامج الحالي ليتسنى للمستخدم استبداله
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل فتح مجلد التحديثات:\n" + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

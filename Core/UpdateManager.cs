using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;

namespace ChickenDist.Core
{
    public static class UpdateManager
    {
        // الإصدار الحالي للبرنامج
        public const string CurrentVersion = "2.0.467";
        
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
                    
                    // تحميل بيانات ملف التحديث مع كسر التخزين المؤقت (Cache Busting)
                    string cacheBustedUrl = UpdateUrl + (UpdateUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                    string rawData = client.DownloadString(cacheBustedUrl);
                    rawData = rawData.TrimStart('\uFEFF');
                    string remoteVersion = "";
                    string downloadUrl   = "";
                    string changelog     = "";
                    string expectedSha256 = ""; // checksum للتحقق من سلامة الملف

                    string[] lines = rawData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        int index = line.IndexOf('=');
                        if (index > 0)
                        {
                            string key = line.Substring(0, index).Trim().ToLower();
                            string val = line.Substring(index + 1).Trim();
                            if      (key == "version")   remoteVersion  = val;
                            else if (key == "url" || key == "download")        downloadUrl    = val;
                            else if (key == "changelog")  changelog      = val;
                            else if (key == "sha256")     expectedSha256 = val; // قراءة الـ checksum
                        }
                    }

                    if (string.IsNullOrEmpty(remoteVersion) || string.IsNullOrEmpty(downloadUrl))
                    {
                        if (showNoUpdateMsg)
                            MessageBox.Show("لم يتم العثور على معلومات تحديث صالحة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // مقارنة الإصدار الحالي بالجديد
                    Version local  = new Version(CurrentVersion);
                    Version remote = new Version(remoteVersion);

                    if (remote > local)
                    {
                        var result = MessageBox.Show(
                            $"🚀 يوجد تحديث جديد متاح للبرنامج!\n\n" +
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
                            PerformUpdate(downloadUrl, remoteVersion, expectedSha256);
                    }
                    else
                    {
                        if (showNoUpdateMsg)
                        {
                            MessageBox.Show(
                                $"✅ أنت تستخدم أحدث إصدار بالفعل.\n\n" +
                                $"إصدارك الحالي: {CurrentVersion}\n\n" +
                                $"📌 آخر التحديثات في هذا الإصدار:\n{changelog}",
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
                AppLogger.Error("CheckForUpdates failed", ex, "UpdateManager");
                if (showNoUpdateMsg)
                    MessageBox.Show("فشل الاتصال بسيرفر التحديث.\nيرجى التحقق من الاتصال بالإنترنت.", "خطأ في التحديث", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── التحقق من سلامة الملف عبر SHA-256 ───
        private static string ComputeSha256(string filePath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        private static void PerformUpdate(string downloadUrl, string remoteVersion, string expectedSha256)
        {
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            string currentDir     = Path.GetDirectoryName(currentExePath);

            string updatesDir       = Path.Combine(currentDir, "Updates");
            string versionedExeName = $"ChickenDist_{remoteVersion}.exe";
            string newExePath       = Path.Combine(updatesDir, versionedExeName);

            try
            {
                if (!Directory.Exists(updatesDir))
                    Directory.CreateDirectory(updatesDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل إنشاء مجلد التحديثات Updates:\n" + ex.Message,
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // استخدام BackgroundWorker بدلاً من DownloadFile المتزامن
            // الكود القديم كان يجمد الـ UI Thread طوال مدة التحميل مع Thread.Sleep على UI
            // الحل: التحميل في background thread مع ProgressBar حقيقي يتحدث عبر ReportProgress

            Exception downloadException = null;
            bool downloadSuccess = false;

            using (var progressForm = new Form())
            using (var worker = new System.ComponentModel.BackgroundWorker())
            {
                progressForm.Text = "جاري تحميل التحديث...";
                progressForm.Size = new System.Drawing.Size(420, 160);
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.MaximizeBox = false;
                progressForm.MinimizeBox = false;
                progressForm.RightToLeft = RightToLeft.Yes;
                progressForm.RightToLeftLayout = true;
                progressForm.ControlBox = false; // منع الإغلاق أثناء التحميل

                var lbl = new Label
                {
                    Text = "جاري تحميل الملف من السيرفر، يرجى الانتظار...",
                    AutoSize = false,
                    Width = 380, Height = 22,
                    Location = new System.Drawing.Point(20, 18),
                    TextAlign = System.Drawing.ContentAlignment.MiddleRight
                };
                var pb = new ProgressBar
                {
                    Width = 380, Height = 26,
                    Location = new System.Drawing.Point(20, 50),
                    Minimum = 0, Maximum = 100, Value = 0,
                    Style = ProgressBarStyle.Continuous
                };
                var lblPct = new Label
                {
                    Text = "0%",
                    AutoSize = true,
                    Location = new System.Drawing.Point(190, 84)
                };
                progressForm.Controls.AddRange(new System.Windows.Forms.Control[] { lbl, pb, lblPct });

                int maxRetries = 3;

                worker.WorkerReportsProgress = true;
                worker.WorkerSupportsCancellation = false;

                worker.DoWork += (s, e) =>
                {
                    for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        try
                        {
                            worker.ReportProgress(0, $"محاولة {attempt} من {maxRetries}...");

                            if (File.Exists(newExePath))
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    try
                                    {
                                        File.Delete(newExePath);
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        if (i == 2)
                                        {
                                            AppLogger.Error("Failed to delete old update file after 3 attempts", ex, "UpdateManager");
                                            throw;
                                        }
                                        System.Threading.Thread.Sleep(500);
                                    }
                                }
                            }

                            using (var client = new WebClient())
                            {
                                ServicePointManager.SecurityProtocol =
                                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 |
                                    SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                                client.Headers.Add("User-Agent",
                                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");

                                client.DownloadProgressChanged += (cs, ce) =>
                                    worker.ReportProgress(ce.ProgressPercentage, null);

                                // DownloadFileAsync + AutoResetEvent لتحويل async -> sync في background thread
                                var done = new System.Threading.AutoResetEvent(false);
                                Exception innerEx = null;
                                client.DownloadFileCompleted += (cs, ce) =>
                                {
                                    innerEx = ce.Error;
                                    done.Set();
                                };
                                string cacheBustedDownloadUrl = downloadUrl + (downloadUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                                client.DownloadFileAsync(new Uri(cacheBustedDownloadUrl), newExePath);
                                done.WaitOne(); // ننتظر في الـ background thread — لا يجمد الـ UI

                                if (innerEx != null) throw innerEx;
                            }

                            downloadSuccess = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            downloadException = ex;
                            AppLogger.Error($"Download attempt {attempt} failed", ex, "UpdateManager");
                            if (attempt < maxRetries)
                                System.Threading.Thread.Sleep(1500); // آمن هنا لأننا في background thread
                        }
                    }
                };

                worker.ProgressChanged += (s, e) =>
                {
                    pb.Value = Math.Min(e.ProgressPercentage, 100);
                    lblPct.Text = $"{e.ProgressPercentage}%";
                    if (e.UserState is string msg && !string.IsNullOrEmpty(msg))
                        lbl.Text = msg;
                };

                worker.RunWorkerCompleted += (s, e) =>
                {
                    progressForm.Close();
                };

                worker.RunWorkerAsync();
                progressForm.ShowDialog(); // يبقى UI مستجيباً لأن التحميل في background
            }

            // التحقق من سلامة الملف بعد التحميل
            if (downloadSuccess)
            {
                try
                {
                    if (!File.Exists(newExePath))
                        throw new Exception("فشل حفظ ملف التحديث");

                    FileInfo fi = new FileInfo(newExePath);
                    if (fi.Length < 100000)
                        throw new Exception("ملف التحديث تالف أو غير مكتمل (الحجم أصغر من المتوقع)");

                    if (!string.IsNullOrEmpty(expectedSha256))
                    {
                        string actualSha256 = ComputeSha256(newExePath);
                        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(newExePath);
                            throw new Exception(
                                $"فشل التحقق من سلامة الملف (checksum غير مطابق).\n" +
                                $"المتوقع: {expectedSha256}\n" +
                                $"الفعلي:  {actualSha256}\n\n" +
                                "تم حذف الملف لأسباب أمنية. يرجى المحاولة مرة أخرى.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    downloadSuccess = false;
                    MessageBox.Show(
                        $"فشل التحقق من ملف التحديث.\n\nالسبب: {ex.Message}",
                        "خطأ في التحديث", MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    return;
                }
            }

            if (!downloadSuccess)
            {
                MessageBox.Show(
                    $"فشل تنزيل التحديث بعد 3 محاولات.\n\nالسبب: {downloadException?.Message}",
                    "خطأ في التحديث", MessageBoxButtons.OK, MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return;
            }

            try
            {
                MessageBox.Show(
                    "✅ تم تحميل التحديث بنجاح!\n\n" +
                    $"📝 تم حفظ ملف البرنامج الجديد باسم {versionedExeName} داخل مجلد (Updates).\n\n" +
                    "سيتم الآن تحديد الملف الجديد تلقائياً وإغلاق البرنامج الحالي.",
                    "اكتمل تحميل التحديث",
                    MessageBoxButtons.OK, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                Process.Start("explorer.exe", $"/select,\"{newExePath}\"");
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل فتح مجلد التحديثات:\n" + ex.Message,
                    "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

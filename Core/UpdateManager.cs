using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;
using System.Data;

namespace ChickenDist.Core
{
    public static class UpdateManager
    {
        // الإصدار الحالي للبرنامج
        public const string CurrentVersion = "2.6.9";
        
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
        public static string ComputeSha256(string filePath)
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
                                using (var done = new System.Threading.AutoResetEvent(false))
                                {
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
                    $"✅ تم تحميل التحديث (v{remoteVersion}) بنجاح!\n\n" +
                    "سيتم الآن استبدال ملف البرنامج وإعادة التشغيل فوراً.",
                    "اكتمل تحميل التحديث",
                    MessageBoxButtons.OK, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                ApplyAndReplaceExe(newExePath);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Auto apply update failed", ex, "UpdateManager");
                try { Process.Start("explorer.exe", $"/select,\"{newExePath}\""); } catch { }
                Application.Exit();
            }
        }

        /// <summary>
        /// استبدال الملف التنفيذي الحالي للبرنامج (الأيقونة الحالية) بالملف الجديد المحدث مباشرة
        /// وإعادة تشغيله فوراً من نفس المسار الأصلي مع تنظيف مجلد التحديثات
        /// </summary>
        public static void ApplyAndReplaceExe(string newExePath)
        {
            try
            {
                if (!File.Exists(newExePath))
                {
                    MessageBox.Show("ملف التحديث غير موجود:\n" + newExePath, "خطأ في التحديث", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                string currentDir     = Path.GetDirectoryName(currentExePath);
                int currentPid        = Process.GetCurrentProcess().Id;

                string ps1Path = Path.Combine(currentDir, "apply_update.ps1");
                string batPath = Path.Combine(currentDir, "apply_update.bat");

                // 1. إنشاء سكريبت PowerShell المتقدم لضمان الاستبدال وإعادة التشغيل بدقة 100%
                string psScript = @"
$ErrorActionPreference = 'SilentlyContinue'
$targetPid = " + currentPid + @"
$mainExe = '" + currentExePath.Replace("'", "''") + @"'
$newExe = '" + newExePath.Replace("'", "''") + @"'
$appDir = '" + currentDir.Replace("'", "''") + @"'

# 1. انتظار إغلاق البرنامج بالكامل
for ($i = 0; $i -lt 40; $i++) {
    $proc = Get-Process -Id $targetPid -ErrorAction SilentlyContinue
    if (-not $proc) { break }
    Start-Sleep -Milliseconds 200
}
Stop-Process -Id $targetPid -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

# 2. إغلاق أي عمليات أخرى تعمل من نفس الملف التنفيذي
try {
    $exeName = [System.IO.Path]::GetFileNameWithoutExtension($mainExe)
    Get-Process -Name $exeName -ErrorAction SilentlyContinue | Where-Object { 
        try { $_.MainModule.FileName -eq $mainExe } catch { $false }
    } | Stop-Process -Force -ErrorAction SilentlyContinue
} catch {}

# 3. إزالة أي حماية للقراءة فقط واستبدال الملف الأصلي مباشرة
$replaced = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        if (Test-Path $mainExe) {
            Set-ItemProperty -Path $mainExe -Name IsReadOnly -Value $false -ErrorAction SilentlyContinue
            [System.IO.File]::SetAttributes($mainExe, [System.IO.FileAttributes]::Normal)
        }
        Copy-Item -Path $newExe -Destination $mainExe -Force -ErrorAction Stop
        $replaced = $true
        break
    } catch {
        Start-Sleep -Milliseconds 300
    }
}

# 4. تنظيف ملف التحديث من مجلد Updates ليبقى ملف تنفيذي واحد فقط
if ($replaced) {
    Remove-Item -Path $newExe -Force -ErrorAction SilentlyContinue
}

# 5. تشغيل البرنامج المحدث مباشرة من مساره الأصلي
Start-Sleep -Milliseconds 200
Set-Location -Path $appDir
Start-Process -FilePath $mainExe -WorkingDirectory $appDir

# 6. حذف سكريبت التحديث المؤقت
Remove-Item -Path $PSCommandPath -Force -ErrorAction SilentlyContinue
";

                File.WriteAllText(ps1Path, psScript, Encoding.UTF8);

                // 2. إنشاء سكريبت CMD احتياطي في حال تعذر تشغيل PowerShell
                string batScript = $@"@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

:: إيقاف العملية السابقة وانتظار تحرير الملف
ping 127.0.0.1 -n 2 > nul
taskkill /f /pid {currentPid} >nul 2>&1

:: محاولات استبدال الملف الأصلي
set /a attempts=0
:retry_copy
attrib -r -s -h ""{currentExePath}"" >nul 2>&1
copy /y ""{newExePath}"" ""{currentExePath}"" > nul 2>&1
if errorlevel 1 (
    set /a attempts+=1
    ping 127.0.0.1 -n 2 > nul
    if !attempts! lss 25 goto retry_copy
)

:: تشغيل النسخة المحدثة من مسارها الأصلي فوراً
cd /d ""{currentDir}""
start """" ""{currentExePath}""

:: تنظيف الملف المؤقت
del /f /q ""{newExePath}"" > nul 2>&1
(goto) 2>nul & del ""%~f0""
";
                File.WriteAllText(batPath, batScript, Encoding.UTF8);

                // 3. إطلاق عملية التحديث في الخلفية بأعلى أولوية
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{ps1Path}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        WorkingDirectory = currentDir
                    };
                    Process.Start(psi);
                }
                catch
                {
                    // Fallback to cmd batch if powershell is disabled
                    var psiBat = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{batPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        WorkingDirectory = currentDir
                    };
                    Process.Start(psiBat);
                }

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                AppLogger.Error("ApplyAndReplaceExe failed", ex, "UpdateManager");
                MessageBox.Show("فشل استبدال ملف البرنامج تلقائياً:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// تحميل ملف البرنامج المعتمد مباشرة من قاعدة بيانات السيرفر الرئيسي عبر الشبكة المحلية LAN
        /// </summary>
        public static bool DownloadFromDatabase(string targetVersion, string destinationExePath, Action<int, string> onProgress, out string error)
        {
            error = "";
            try
            {
                onProgress?.Invoke(10, "جاري الاستعلام عن ملف التحديث من السيرفر الرئيسي...");
                byte[] bytes = null;
                string expectedSha = null;

                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new System.Data.SqlClient.SqlCommand(
                        "SELECT TOP 1 [AppBinary], [BinarySha256], [BinaryLength] FROM [versions] WHERE [version] = @ver AND [AppBinary] IS NOT NULL", conn))
                    {
                        cmd.Parameters.AddWithValue("@ver", targetVersion);
                        using (var r = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                        {
                            if (r.Read() && r["AppBinary"] != DBNull.Value)
                            {
                                bytes = (byte[])r["AppBinary"];
                                expectedSha = r["BinarySha256"] != DBNull.Value ? r["BinarySha256"].ToString() : null;
                            }
                        }
                    }
                }

                if (bytes == null || bytes.Length < 500000)
                {
                    error = "لم يتم العثور على ملف البرنامج في قاعدة البيانات، أو حجم الملف غير صالح.";
                    return false;
                }

                onProgress?.Invoke(60, "جاري حفظ ملف التحديث على هذا الجهاز...");
                string dir = Path.GetDirectoryName(destinationExePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.WriteAllBytes(destinationExePath, bytes);

                onProgress?.Invoke(90, "جاري التحقق من سلامة الملف...");
                if (!string.IsNullOrEmpty(expectedSha))
                {
                    string actualSha = ComputeSha256(destinationExePath);
                    if (!string.Equals(actualSha, expectedSha, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(destinationExePath); } catch { }
                        error = "فشل التحقق من صحة الملف المحمل (SHA-256 غير مطابق).";
                        return false;
                    }
                }

                onProgress?.Invoke(100, "تم استلام الملف بنجاح!");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                AppLogger.Error("UpdateManager.DownloadFromDatabase", ex);
                return false;
            }
        }

        /// <summary>
        /// تحميل ملف البرنامج المعتمد من السيرفر السحابي (GitHub CDN) كخيار بديل
        /// </summary>
        public static bool DownloadFromWeb(string targetVersion, string destinationExePath, Action<int, string> onProgress, out string error)
        {
            error = "";
            try
            {
                onProgress?.Invoke(15, "جاري فحص خادم التحديثات السحابي...");
                string dlUrl = "";
                string expectedSha = "";

                using (var client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add("User-Agent", "Mozilla/5.0 ProSoftAutoUpdater");

                    string cacheBustedUrl = UpdateUrl + (UpdateUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                    string rawData = client.DownloadString(cacheBustedUrl).TrimStart('\uFEFF');
                    string[] lines = rawData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        int idx = line.IndexOf('=');
                        if (idx > 0)
                        {
                            string k = line.Substring(0, idx).Trim().ToLower();
                            string v = line.Substring(idx + 1).Trim();
                            if (k == "url" || k == "download") dlUrl = v;
                            if (k == "sha256") expectedSha = v;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(dlUrl))
                {
                    error = "تعذر الحصول على رابط التحديث من السيرفر السحابي.";
                    return false;
                }

                onProgress?.Invoke(30, "جاري تنزيل التحديث السحابي...");
                string dir = Path.GetDirectoryName(destinationExePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using (var client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                    client.Headers.Add("User-Agent", "Mozilla/5.0 ProSoftAutoUpdater");

                    client.DownloadProgressChanged += (s, e) =>
                    {
                        int p = 30 + (int)(e.ProgressPercentage * 0.60);
                        onProgress?.Invoke(p, $"جاري التحميل: {e.ProgressPercentage}% ({e.BytesReceived / 1024 / 1024:0.#} ميجابايت)...");
                    };

                    using (var done = new System.Threading.AutoResetEvent(false))
                    {
                        Exception dlEx = null;
                        client.DownloadFileCompleted += (s, e) =>
                        {
                            dlEx = e.Error;
                            done.Set();
                        };
                        string finalDlUrl = dlUrl + (dlUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                        client.DownloadFileAsync(new Uri(finalDlUrl), destinationExePath);
                        done.WaitOne();

                        if (dlEx != null) throw dlEx;
                    }
                }

                onProgress?.Invoke(92, "جاري التحقق من سلامة الملف المحمل...");
                FileInfo fi = new FileInfo(destinationExePath);
                if (!fi.Exists || fi.Length < 500000)
                {
                    error = "الملف المحمل غير صالح أو تالف.";
                    return false;
                }

                if (!string.IsNullOrEmpty(expectedSha))
                {
                    string actualSha = ComputeSha256(destinationExePath);
                    if (!string.Equals(actualSha, expectedSha, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(destinationExePath); } catch { }
                        error = "فشل التحقق من صحة الملف المحمل (SHA-256 غير مطابق).";
                        return false;
                    }
                }

                onProgress?.Invoke(100, "اكتمل التحميل بنجاح!");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                AppLogger.Error("UpdateManager.DownloadFromWeb", ex);
                return false;
            }
        }

        /// <summary>
        /// تحميل وتثبيت التحديث للأجهزة الفرعية تلقائياً (يحاول من السيرفر المحلي أولاً ثم السحابي)
        /// </summary>
        public static bool DownloadAndInstallClientUpdate(string targetVersion, Action<int, string> onProgress, out string error)
        {
            error = "";
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string currentDir = Path.GetDirectoryName(currentExe);
            string updatesDir = Path.Combine(currentDir, "Updates");
            if (!Directory.Exists(updatesDir)) Directory.CreateDirectory(updatesDir);

            string destPath = Path.Combine(updatesDir, $"ProSoft_v{targetVersion}.exe");

            onProgress?.Invoke(5, "جاري محاولة التحميل المباشر من السيرفر الرئيسي (LAN)...");
            bool ok = DownloadFromDatabase(targetVersion, destPath, onProgress, out string dbErr);

            if (!ok)
            {
                onProgress?.Invoke(15, "جاري محاولة التحميل من خادم التحديثات السحابي...");
                ok = DownloadFromWeb(targetVersion, destPath, onProgress, out string webErr);
                if (!ok)
                {
                    error = $"فشل التحميل من السيرفر المحلي: {dbErr}\n\nوفشل التحميل السحابي: {webErr}";
                    return false;
                }
            }

            onProgress?.Invoke(98, "تم التحميل بنجاح! جاري تثبيت الإصدار وإعادة التشغيل...");
            System.Threading.Thread.Sleep(500);
            ApplyAndReplaceExe(destPath);
            return true;
        }
    }
}

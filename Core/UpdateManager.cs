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
        // Ø§Ù„Ø¥ØµØ¯Ø§Ø± Ø§Ù„Ø­Ø§Ù„ÙŠ Ù„Ù„Ø¨Ø±Ù†Ø§Ù…Ø¬
        public const string CurrentVersion = "1.9.44";
        
        // Ø±Ø§Ø¨Ø· Ù…Ù„Ù Ø§Ù„ØªØ­Ø¯ÙŠØ« Ø§Ù„Ù†ØµÙŠ Ø¹Ù„Ù‰ GitHub
        private const string UpdateUrl = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/update.txt";

        public static void CheckForUpdates(bool showNoUpdateMsg = false)
        {
            try
            {
                using (var client = new WebClient())
                {
                    // ØªÙØ¹ÙŠÙ„ Ø¨Ø±ÙˆØªÙˆÙƒÙˆÙ„ TLS 1.2 ÙˆØ§Ù„Ø¨Ø±ÙˆØªÙˆÙƒÙˆÙ„Ø§Øª Ø§Ù„Ø£Ø®Ø±Ù‰ Ù„Ù„Ø§ØªØµØ§Ù„ Ø§Ù„Ø¢Ù…Ù† Ø¨Ù€ GitHub
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                    client.Encoding = System.Text.Encoding.UTF8;
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
                    
                    // ØªØ­Ù…ÙŠÙ„ Ø¨ÙŠØ§Ù†Ø§Øª Ù…Ù„Ù Ø§Ù„ØªØ­Ø¯ÙŠØ« Ù…Ø¹ ÙƒØ³Ø± Ø§Ù„ØªØ®Ø²ÙŠÙ† Ø§Ù„Ù…Ø¤Ù‚Øª (Cache Busting)
                    string cacheBustedUrl = UpdateUrl + (UpdateUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                    string rawData = client.DownloadString(cacheBustedUrl);
                    rawData = rawData.TrimStart('\uFEFF');
                    string remoteVersion = "";
                    string downloadUrl   = "";
                    string changelog     = "";
                    string expectedSha256 = ""; // checksum Ù„Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ø³Ù„Ø§Ù…Ø© Ø§Ù„Ù…Ù„Ù

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
                            else if (key == "sha256")     expectedSha256 = val; // Ù‚Ø±Ø§Ø¡Ø© Ø§Ù„Ù€ checksum
                        }
                    }

                    if (string.IsNullOrEmpty(remoteVersion) || string.IsNullOrEmpty(downloadUrl))
                    {
                        if (showNoUpdateMsg)
                            MessageBox.Show("Ù„Ù… ÙŠØªÙ… Ø§Ù„Ø¹Ø«ÙˆØ± Ø¹Ù„Ù‰ Ù…Ø¹Ù„ÙˆÙ…Ø§Øª ØªØ­Ø¯ÙŠØ« ØµØ§Ù„Ø­Ø©.", "ØªÙ†Ø¨ÙŠÙ‡", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Ù…Ù‚Ø§Ø±Ù†Ø© Ø§Ù„Ø¥ØµØ¯Ø§Ø± Ø§Ù„Ø­Ø§Ù„ÙŠ Ø¨Ø§Ù„Ø¬Ø¯ÙŠØ¯
                    Version local  = new Version(CurrentVersion);
                    Version remote = new Version(remoteVersion);

                    if (remote > local)
                    {
                        var result = MessageBox.Show(
                            $"ðŸ”„ ÙŠÙˆØ¬Ø¯ ØªØ­Ø¯ÙŠØ« Ø¬Ø¯ÙŠØ¯ Ù…ØªØ§Ø­ Ù„Ù„Ø¨Ø±Ù†Ø§Ù…Ø¬!\n\n" +
                            $"Ø§Ù„Ø¥ØµØ¯Ø§Ø± Ø§Ù„Ø­Ø§Ù„ÙŠ Ù„Ø¯ÙŠÙƒ:  {CurrentVersion}\n" +
                            $"Ø§Ù„Ø¥ØµØ¯Ø§Ø± Ø§Ù„Ø¬Ø¯ÙŠØ¯ Ø§Ù„Ù…ØªØ§Ø­: {remoteVersion}\n\n" +
                            $"ðŸ“ Ù…Ø§ Ø§Ù„Ø¬Ø¯ÙŠØ¯ ÙÙŠ Ù‡Ø°Ø§ Ø§Ù„Ø¥ØµØ¯Ø§Ø±:\n{changelog}\n\n" +
                            $"Ù‡Ù„ ØªØ±ØºØ¨ ÙÙŠ ØªÙ†Ø²ÙŠÙ„ Ø§Ù„ØªØ­Ø¯ÙŠØ« ÙˆØªØ«Ø¨ÙŠØªÙ‡ Ø§Ù„Ø¢Ù†ØŸ",
                            "ØªØ­Ø¯ÙŠØ« Ø¬Ø¯ÙŠØ¯ Ù…ØªØ§Ø­",
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
                                $"âœ… Ø£Ù†Øª ØªØ³ØªØ®Ø¯Ù… Ø£Ø­Ø¯Ø« Ø¥ØµØ¯Ø§Ø± Ø¨Ø§Ù„ÙØ¹Ù„.\n\n" +
                                $"Ø¥ØµØ¯Ø§Ø±Ùƒ Ø§Ù„Ø­Ø§Ù„ÙŠ: {CurrentVersion}\n\n" +
                                $"ðŸ“ Ø¢Ø®Ø± Ø§Ù„ØªØ­Ø¯ÙŠØ«Ø§Øª ÙÙŠ Ù‡Ø°Ø§ Ø§Ù„Ø¥ØµØ¯Ø§Ø±:\n{changelog}",
                                "ØªØ­Ø¯ÙŠØ« Ø§Ù„Ø¨Ø±Ù†Ø§Ù…Ø¬",
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
                    MessageBox.Show("ÙØ´Ù„ Ø§Ù„Ø§ØªØµØ§Ù„ Ø¨Ø³ÙŠØ±ÙØ± Ø§Ù„ØªØ­Ø¯ÙŠØ«.\nÙŠØ±Ø¬Ù‰ Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ø§Ù„Ø§ØªØµØ§Ù„ Ø¨Ø§Ù„Ø¥Ù†ØªØ±Ù†Øª.", "Ø®Ø·Ø£ ÙÙŠ Ø§Ù„ØªØ­Ø¯ÙŠØ«", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // â”€â”€ Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† SHA-256 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                MessageBox.Show("ÙØ´Ù„ Ø¥Ù†Ø´Ø§Ø¡ Ù…Ø¬Ù„Ø¯ Ø§Ù„ØªØ­Ø¯ÙŠØ«Ø§Øª Updates:\n" + ex.Message,
                    "Ø®Ø·Ø£", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // FIX: Ø§Ø³ØªØ®Ø¯Ø§Ù… BackgroundWorker Ø¨Ø¯Ù„Ø§Ù‹ Ù…Ù† DownloadFile Ø§Ù„Ù…ØªØ²Ø§Ù…Ù†
            // Ø§Ù„ÙƒÙˆØ¯ Ø§Ù„Ù‚Ø¯ÙŠÙ… ÙƒØ§Ù† ÙŠØ¬Ù…Ø¯ Ø§Ù„Ù€ UI Thread Ø·ÙˆØ§Ù„ Ù…Ø¯Ø© Ø§Ù„ØªØ­Ù…ÙŠÙ„ Ù…Ø¹ Thread.Sleep Ø¹Ù„Ù‰ UI
            // Ø§Ù„Ø­Ù„: Ø§Ù„ØªØ­Ù…ÙŠÙ„ ÙÙŠ background thread Ù…Ø¹ ProgressBar Ø­Ù‚ÙŠÙ‚ÙŠ ÙŠØªØ­Ø¯Ø« Ø¹Ø¨Ø± ReportProgress

            Exception downloadException = null;
            bool downloadSuccess = false;

            using (var progressForm = new Form())
            using (var worker = new System.ComponentModel.BackgroundWorker())
            {
                progressForm.Text = "Ø¬Ø§Ø±ÙŠ ØªØ­Ù…ÙŠÙ„ Ø§Ù„ØªØ­Ø¯ÙŠØ«...";
                progressForm.Size = new System.Drawing.Size(420, 160);
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.MaximizeBox = false;
                progressForm.MinimizeBox = false;
                progressForm.RightToLeft = RightToLeft.Yes;
                progressForm.RightToLeftLayout = true;
                progressForm.ControlBox = false; // Ù…Ù†Ø¹ Ø§Ù„Ø¥ØºÙ„Ø§Ù‚ Ø£Ø«Ù†Ø§Ø¡ Ø§Ù„ØªØ­Ù…ÙŠÙ„

                var lbl = new Label
                {
                    Text = "Ø¬Ø§Ø±ÙŠ ØªØ­Ù…ÙŠÙ„ Ø§Ù„Ù…Ù„Ù Ù…Ù† Ø§Ù„Ø³ÙŠØ±ÙØ±ØŒ ÙŠØ±Ø¬Ù‰ Ø§Ù„Ø§Ù†ØªØ¸Ø§Ø±...",
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
                            worker.ReportProgress(0, $"Ù…Ø­Ø§ÙˆÙ„Ø© {attempt} Ù…Ù† {maxRetries}...");

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

                                // DownloadFileAsync + AutoResetEvent Ù„ØªØ­ÙˆÙŠÙ„ async -> sync ÙÙŠ background thread
                                var done = new System.Threading.AutoResetEvent(false);
                                Exception innerEx = null;
                                client.DownloadFileCompleted += (cs, ce) =>
                                {
                                    innerEx = ce.Error;
                                    done.Set();
                                };
                                string cacheBustedDownloadUrl = downloadUrl + (downloadUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                                client.DownloadFileAsync(new Uri(cacheBustedDownloadUrl), newExePath);
                                done.WaitOne(); // Ù†Ù†ØªØ¸Ø± ÙÙŠ Ø§Ù„Ù€ background thread â€” Ù„Ø§ ÙŠØ¬Ù…Ø¯ Ø§Ù„Ù€ UI

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
                                System.Threading.Thread.Sleep(1500); // Ø¢Ù…Ù† Ù‡Ù†Ø§ Ù„Ø£Ù†Ù†Ø§ ÙÙŠ background thread
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
                progressForm.ShowDialog(); // ÙŠØ¨Ù‚Ù‰ UI Ù…Ø³ØªØ¬ÙŠØ¨Ø§Ù‹ Ù„Ø£Ù† Ø§Ù„ØªØ­Ù…ÙŠÙ„ ÙÙŠ background
            }

            // Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ø³Ù„Ø§Ù…Ø© Ø§Ù„Ù…Ù„Ù Ø¨Ø¹Ø¯ Ø§Ù„ØªØ­Ù…ÙŠÙ„
            if (downloadSuccess)
            {
                try
                {
                    if (!File.Exists(newExePath))
                        throw new Exception("ÙØ´Ù„ Ø­ÙØ¸ Ù…Ù„Ù Ø§Ù„ØªØ­Ø¯ÙŠØ«");

                    FileInfo fi = new FileInfo(newExePath);
                    if (fi.Length < 100000)
                        throw new Exception("Ù…Ù„Ù Ø§Ù„ØªØ­Ø¯ÙŠØ« ØªØ§Ù„Ù Ø£Ùˆ ØºÙŠØ± Ù…ÙƒØªÙ…Ù„ (Ø§Ù„Ø­Ø¬Ù… Ø£ØµØºØ± Ù…Ù† Ø§Ù„Ù…ØªÙˆÙ‚Ø¹)");

                    if (!string.IsNullOrEmpty(expectedSha256))
                    {
                        string actualSha256 = ComputeSha256(newExePath);
                        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(newExePath);
                            throw new Exception(
                                $"ÙØ´Ù„ Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ø³Ù„Ø§Ù…Ø© Ø§Ù„Ù…Ù„Ù (checksum ØºÙŠØ± Ù…Ø·Ø§Ø¨Ù‚).\n" +
                                $"Ø§Ù„Ù…ØªÙˆÙ‚Ø¹: {expectedSha256}\n" +
                                $"Ø§Ù„ÙØ¹Ù„ÙŠ:  {actualSha256}\n\n" +
                                "ØªÙ… Ø­Ø°Ù Ø§Ù„Ù…Ù„Ù Ù„Ø£Ø³Ø¨Ø§Ø¨ Ø£Ù…Ù†ÙŠØ©. ÙŠØ±Ø¬Ù‰ Ø§Ù„Ù…Ø­Ø§ÙˆÙ„Ø© Ù…Ø±Ø© Ø£Ø®Ø±Ù‰.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    downloadSuccess = false;
                    MessageBox.Show(
                        $"ÙØ´Ù„ Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ù…Ù„Ù Ø§Ù„ØªØ­Ø¯ÙŠØ«.\n\nØ§Ù„Ø³Ø¨Ø¨: {ex.Message}",
                        "Ø®Ø·Ø£ ÙÙŠ Ø§Ù„ØªØ­Ø¯ÙŠØ«", MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    return;
                }
            }

            if (!downloadSuccess)
            {
                MessageBox.Show(
                    $"ÙØ´Ù„ ØªÙ†Ø²ÙŠÙ„ Ø§Ù„ØªØ­Ø¯ÙŠØ« Ø¨Ø¹Ø¯ 3 Ù…Ø­Ø§ÙˆÙ„Ø§Øª.\n\nØ§Ù„Ø³Ø¨Ø¨: {downloadException?.Message}",
                    "Ø®Ø·Ø£ ÙÙŠ Ø§Ù„ØªØ­Ø¯ÙŠØ«", MessageBoxButtons.OK, MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return;
            }

            try
            {
                MessageBox.Show(
                    "âœ… ØªÙ… ØªØ­Ù…ÙŠÙ„ Ø§Ù„ØªØ­Ø¯ÙŠØ« Ø¨Ù†Ø¬Ø§Ø­!\n\n" +
                    $"ðŸ“ ØªÙ… Ø­ÙØ¸ Ù…Ù„Ù Ø§Ù„Ø¨Ø±Ù†Ø§Ù…Ø¬ Ø§Ù„Ø¬Ø¯ÙŠØ¯ Ø¨Ø§Ø³Ù… {versionedExeName} Ø¯Ø§Ø®Ù„ Ù…Ø¬Ù„Ø¯ (Updates).\n\n" +
                    "Ø³ÙŠØªÙ… Ø§Ù„Ø¢Ù† ØªØ­Ø¯ÙŠØ¯ Ø§Ù„Ù…Ù„Ù Ø§Ù„Ø¬Ø¯ÙŠØ¯ ØªÙ„Ù‚Ø§Ø¦ÙŠØ§Ù‹ ÙˆØ¥ØºÙ„Ø§Ù‚ Ø§Ù„Ø¨Ø±Ù†Ø§Ù…Ø¬ Ø§Ù„Ø­Ø§Ù„ÙŠ.",
                    "Ø§ÙƒØªÙ…Ù„ ØªØ­Ù…ÙŠÙ„ Ø§Ù„ØªØ­Ø¯ÙŠØ«",
                    MessageBoxButtons.OK, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                Process.Start("explorer.exe", $"/select,\"{newExePath}\"");
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ÙØ´Ù„ ÙØªØ­ Ù…Ø¬Ù„Ø¯ Ø§Ù„ØªØ­Ø¯ÙŠØ«Ø§Øª:\n" + ex.Message,
                    "ØªÙ†Ø¨ÙŠÙ‡", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

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
        // Ã˜Â§Ã™â€žÃ˜Â¥Ã˜ÂµÃ˜Â¯Ã˜Â§Ã˜Â± Ã˜Â§Ã™â€žÃ˜Â­Ã˜Â§Ã™â€žÃ™Å  Ã™â€žÃ™â€žÃ˜Â¨Ã˜Â±Ã™â€ Ã˜Â§Ã™â€¦Ã˜Â¬
        public const string CurrentVersion = "1.9.57";
        
        // Ã˜Â±Ã˜Â§Ã˜Â¨Ã˜Â· Ã™â€¦Ã™â€žÃ™Â Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã˜Â§Ã™â€žÃ™â€ Ã˜ÂµÃ™Å  Ã˜Â¹Ã™â€žÃ™â€° GitHub
        private const string UpdateUrl = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/update.txt";

        public static void CheckForUpdates(bool showNoUpdateMsg = false)
        {
            try
            {
                using (var client = new WebClient())
                {
                    // Ã˜ÂªÃ™ÂÃ˜Â¹Ã™Å Ã™â€ž Ã˜Â¨Ã˜Â±Ã™Ë†Ã˜ÂªÃ™Ë†Ã™Æ’Ã™Ë†Ã™â€ž TLS 1.2 Ã™Ë†Ã˜Â§Ã™â€žÃ˜Â¨Ã˜Â±Ã™Ë†Ã˜ÂªÃ™Ë†Ã™Æ’Ã™Ë†Ã™â€žÃ˜Â§Ã˜Âª Ã˜Â§Ã™â€žÃ˜Â£Ã˜Â®Ã˜Â±Ã™â€° Ã™â€žÃ™â€žÃ˜Â§Ã˜ÂªÃ˜ÂµÃ˜Â§Ã™â€ž Ã˜Â§Ã™â€žÃ˜Â¢Ã™â€¦Ã™â€  Ã˜Â¨Ã™â‚¬ GitHub
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                    client.Encoding = System.Text.Encoding.UTF8;
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
                    
                    // Ã˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž Ã˜Â¨Ã™Å Ã˜Â§Ã™â€ Ã˜Â§Ã˜Âª Ã™â€¦Ã™â€žÃ™Â Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã™â€¦Ã˜Â¹ Ã™Æ’Ã˜Â³Ã˜Â± Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â®Ã˜Â²Ã™Å Ã™â€  Ã˜Â§Ã™â€žÃ™â€¦Ã˜Â¤Ã™â€šÃ˜Âª (Cache Busting)
                    string cacheBustedUrl = UpdateUrl + (UpdateUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                    string rawData = client.DownloadString(cacheBustedUrl);
                    rawData = rawData.TrimStart('\uFEFF');
                    string remoteVersion = "";
                    string downloadUrl   = "";
                    string changelog     = "";
                    string expectedSha256 = ""; // checksum Ã™â€žÃ™â€žÃ˜ÂªÃ˜Â­Ã™â€šÃ™â€š Ã™â€¦Ã™â€  Ã˜Â³Ã™â€žÃ˜Â§Ã™â€¦Ã˜Â© Ã˜Â§Ã™â€žÃ™â€¦Ã™â€žÃ™Â

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
                            else if (key == "sha256")     expectedSha256 = val; // Ã™â€šÃ˜Â±Ã˜Â§Ã˜Â¡Ã˜Â© Ã˜Â§Ã™â€žÃ™â‚¬ checksum
                        }
                    }

                    if (string.IsNullOrEmpty(remoteVersion) || string.IsNullOrEmpty(downloadUrl))
                    {
                        if (showNoUpdateMsg)
                            MessageBox.Show("Ã™â€žÃ™â€¦ Ã™Å Ã˜ÂªÃ™â€¦ Ã˜Â§Ã™â€žÃ˜Â¹Ã˜Â«Ã™Ë†Ã˜Â± Ã˜Â¹Ã™â€žÃ™â€° Ã™â€¦Ã˜Â¹Ã™â€žÃ™Ë†Ã™â€¦Ã˜Â§Ã˜Âª Ã˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã˜ÂµÃ˜Â§Ã™â€žÃ˜Â­Ã˜Â©.", "Ã˜ÂªÃ™â€ Ã˜Â¨Ã™Å Ã™â€¡", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Ã™â€¦Ã™â€šÃ˜Â§Ã˜Â±Ã™â€ Ã˜Â© Ã˜Â§Ã™â€žÃ˜Â¥Ã˜ÂµÃ˜Â¯Ã˜Â§Ã˜Â± Ã˜Â§Ã™â€žÃ˜Â­Ã˜Â§Ã™â€žÃ™Å  Ã˜Â¨Ã˜Â§Ã™â€žÃ˜Â¬Ã˜Â¯Ã™Å Ã˜Â¯
                    Version local  = new Version(CurrentVersion);
                    Version remote = new Version(remoteVersion);

                    if (remote > local)
                    {
                        var result = MessageBox.Show(
                            $"Ã°Å¸â€â€ž Ã™Å Ã™Ë†Ã˜Â¬Ã˜Â¯ Ã˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã˜Â¬Ã˜Â¯Ã™Å Ã˜Â¯ Ã™â€¦Ã˜ÂªÃ˜Â§Ã˜Â­ Ã™â€žÃ™â€žÃ˜Â¨Ã˜Â±Ã™â€ Ã˜Â§Ã™â€¦Ã˜Â¬!\n\n" +
                            $"Ã˜Â§Ã™â€žÃ˜Â¥Ã˜ÂµÃ˜Â¯Ã˜Â§Ã˜Â± Ã˜Â§Ã™â€žÃ˜Â­Ã˜Â§Ã™â€žÃ™Å  Ã™â€žÃ˜Â¯Ã™Å Ã™Æ’:  {CurrentVersion}\n" +
                            $"Ã˜Â§Ã™â€žÃ˜Â¥Ã˜ÂµÃ˜Â¯Ã˜Â§Ã˜Â± Ã˜Â§Ã™â€žÃ˜Â¬Ã˜Â¯Ã™Å Ã˜Â¯ Ã˜Â§Ã™â€žÃ™â€¦Ã˜ÂªÃ˜Â§Ã˜Â­: {remoteVersion}\n\n" +
                            $"Ã°Å¸â€œÂ Ã™â€¦Ã˜Â§ Ã˜Â§Ã™â€žÃ˜Â¬Ã˜Â¯Ã™Å Ã˜Â¯ Ã™ÂÃ™Å  Ã™â€¡Ã˜Â°Ã˜Â§ Ã˜Â§Ã™â€žÃ˜Â¥Ã˜ÂµÃ˜Â¯Ã˜Â§Ã˜Â±:\n{changelog}\n\n" +
                            $"Ã™â€¡Ã™â€ž Ã˜ÂªÃ˜Â±Ã˜ÂºÃ˜Â¨ Ã™ÂÃ™Å  Ã˜ÂªÃ™â€ Ã˜Â²Ã™Å Ã™â€ž Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã™Ë†Ã˜ÂªÃ˜Â«Ã˜Â¨Ã™Å Ã˜ÂªÃ™â€¡ Ã˜Â§Ã™â€žÃ˜Â¢Ã™â€ Ã˜Å¸",
                            "Ã˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã˜Â¬Ã˜Â¯Ã™Å Ã˜Â¯ Ã™â€¦Ã˜ÂªÃ˜Â§Ã˜Â­",
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
                                $"Ã¢Å“â€¦ Ã˜Â£Ã™â€ Ã˜Âª Ã˜ÂªÃ˜Â³Ã˜ÂªÃ˜Â®Ã˜Â¯Ã™â€¦ Ã˜Â£Ã˜Â­Ã˜Â¯Ã˜Â« Ã˜Â¥Ã˜ÂµÃ˜Â¯Ã˜Â§Ã˜Â± Ã˜Â¨Ã˜Â§Ã™â€žÃ™ÂÃ˜Â¹Ã™â€ž.\n\n" +
                                $"Ã˜Â¥Ã˜ÂµÃ˜Â¯Ã˜Â§Ã˜Â±Ã™Æ’ Ã˜Â§Ã™â€žÃ˜Â­Ã˜Â§Ã™â€žÃ™Å : {CurrentVersion}\n\n" +
                                $"Ã°Å¸â€œÂ Ã˜Â¢Ã˜Â®Ã˜Â± Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«Ã˜Â§Ã˜Âª Ã™ÂÃ™Å  Ã™â€¡Ã˜Â°Ã˜Â§ Ã˜Â§Ã™â€žÃ˜Â¥Ã˜ÂµÃ˜Â¯Ã˜Â§Ã˜Â±:\n{changelog}",
                                "Ã˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã˜Â§Ã™â€žÃ˜Â¨Ã˜Â±Ã™â€ Ã˜Â§Ã™â€¦Ã˜Â¬",
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
                    MessageBox.Show("Ã™ÂÃ˜Â´Ã™â€ž Ã˜Â§Ã™â€žÃ˜Â§Ã˜ÂªÃ˜ÂµÃ˜Â§Ã™â€ž Ã˜Â¨Ã˜Â³Ã™Å Ã˜Â±Ã™ÂÃ˜Â± Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«.\nÃ™Å Ã˜Â±Ã˜Â¬Ã™â€° Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€šÃ™â€š Ã™â€¦Ã™â€  Ã˜Â§Ã™â€žÃ˜Â§Ã˜ÂªÃ˜ÂµÃ˜Â§Ã™â€ž Ã˜Â¨Ã˜Â§Ã™â€žÃ˜Â¥Ã™â€ Ã˜ÂªÃ˜Â±Ã™â€ Ã˜Âª.", "Ã˜Â®Ã˜Â·Ã˜Â£ Ã™ÂÃ™Å  Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Ã¢â€â‚¬Ã¢â€â‚¬ Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€šÃ™â€š Ã™â€¦Ã™â€  SHA-256 Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
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
                MessageBox.Show("Ã™ÂÃ˜Â´Ã™â€ž Ã˜Â¥Ã™â€ Ã˜Â´Ã˜Â§Ã˜Â¡ Ã™â€¦Ã˜Â¬Ã™â€žÃ˜Â¯ Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«Ã˜Â§Ã˜Âª Updates:\n" + ex.Message,
                    "Ã˜Â®Ã˜Â·Ã˜Â£", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // FIX: Ã˜Â§Ã˜Â³Ã˜ÂªÃ˜Â®Ã˜Â¯Ã˜Â§Ã™â€¦ BackgroundWorker Ã˜Â¨Ã˜Â¯Ã™â€žÃ˜Â§Ã™â€¹ Ã™â€¦Ã™â€  DownloadFile Ã˜Â§Ã™â€žÃ™â€¦Ã˜ÂªÃ˜Â²Ã˜Â§Ã™â€¦Ã™â€ 
            // Ã˜Â§Ã™â€žÃ™Æ’Ã™Ë†Ã˜Â¯ Ã˜Â§Ã™â€žÃ™â€šÃ˜Â¯Ã™Å Ã™â€¦ Ã™Æ’Ã˜Â§Ã™â€  Ã™Å Ã˜Â¬Ã™â€¦Ã˜Â¯ Ã˜Â§Ã™â€žÃ™â‚¬ UI Thread Ã˜Â·Ã™Ë†Ã˜Â§Ã™â€ž Ã™â€¦Ã˜Â¯Ã˜Â© Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž Ã™â€¦Ã˜Â¹ Thread.Sleep Ã˜Â¹Ã™â€žÃ™â€° UI
            // Ã˜Â§Ã™â€žÃ˜Â­Ã™â€ž: Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž Ã™ÂÃ™Å  background thread Ã™â€¦Ã˜Â¹ ProgressBar Ã˜Â­Ã™â€šÃ™Å Ã™â€šÃ™Å  Ã™Å Ã˜ÂªÃ˜Â­Ã˜Â¯Ã˜Â« Ã˜Â¹Ã˜Â¨Ã˜Â± ReportProgress

            Exception downloadException = null;
            bool downloadSuccess = false;

            using (var progressForm = new Form())
            using (var worker = new System.ComponentModel.BackgroundWorker())
            {
                progressForm.Text = "Ã˜Â¬Ã˜Â§Ã˜Â±Ã™Å  Ã˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«...";
                progressForm.Size = new System.Drawing.Size(420, 160);
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.MaximizeBox = false;
                progressForm.MinimizeBox = false;
                progressForm.RightToLeft = RightToLeft.Yes;
                progressForm.RightToLeftLayout = true;
                progressForm.ControlBox = false; // Ã™â€¦Ã™â€ Ã˜Â¹ Ã˜Â§Ã™â€žÃ˜Â¥Ã˜ÂºÃ™â€žÃ˜Â§Ã™â€š Ã˜Â£Ã˜Â«Ã™â€ Ã˜Â§Ã˜Â¡ Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž

                var lbl = new Label
                {
                    Text = "Ã˜Â¬Ã˜Â§Ã˜Â±Ã™Å  Ã˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž Ã˜Â§Ã™â€žÃ™â€¦Ã™â€žÃ™Â Ã™â€¦Ã™â€  Ã˜Â§Ã™â€žÃ˜Â³Ã™Å Ã˜Â±Ã™ÂÃ˜Â±Ã˜Å’ Ã™Å Ã˜Â±Ã˜Â¬Ã™â€° Ã˜Â§Ã™â€žÃ˜Â§Ã™â€ Ã˜ÂªÃ˜Â¸Ã˜Â§Ã˜Â±...",
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
                            worker.ReportProgress(0, $"Ã™â€¦Ã˜Â­Ã˜Â§Ã™Ë†Ã™â€žÃ˜Â© {attempt} Ã™â€¦Ã™â€  {maxRetries}...");

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

                                // DownloadFileAsync + AutoResetEvent Ã™â€žÃ˜ÂªÃ˜Â­Ã™Ë†Ã™Å Ã™â€ž async -> sync Ã™ÂÃ™Å  background thread
                                var done = new System.Threading.AutoResetEvent(false);
                                Exception innerEx = null;
                                client.DownloadFileCompleted += (cs, ce) =>
                                {
                                    innerEx = ce.Error;
                                    done.Set();
                                };
                                string cacheBustedDownloadUrl = downloadUrl + (downloadUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                                client.DownloadFileAsync(new Uri(cacheBustedDownloadUrl), newExePath);
                                done.WaitOne(); // Ã™â€ Ã™â€ Ã˜ÂªÃ˜Â¸Ã˜Â± Ã™ÂÃ™Å  Ã˜Â§Ã™â€žÃ™â‚¬ background thread Ã¢â‚¬â€ Ã™â€žÃ˜Â§ Ã™Å Ã˜Â¬Ã™â€¦Ã˜Â¯ Ã˜Â§Ã™â€žÃ™â‚¬ UI

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
                                System.Threading.Thread.Sleep(1500); // Ã˜Â¢Ã™â€¦Ã™â€  Ã™â€¡Ã™â€ Ã˜Â§ Ã™â€žÃ˜Â£Ã™â€ Ã™â€ Ã˜Â§ Ã™ÂÃ™Å  background thread
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
                progressForm.ShowDialog(); // Ã™Å Ã˜Â¨Ã™â€šÃ™â€° UI Ã™â€¦Ã˜Â³Ã˜ÂªÃ˜Â¬Ã™Å Ã˜Â¨Ã˜Â§Ã™â€¹ Ã™â€žÃ˜Â£Ã™â€  Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž Ã™ÂÃ™Å  background
            }

            // Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€šÃ™â€š Ã™â€¦Ã™â€  Ã˜Â³Ã™â€žÃ˜Â§Ã™â€¦Ã˜Â© Ã˜Â§Ã™â€žÃ™â€¦Ã™â€žÃ™Â Ã˜Â¨Ã˜Â¹Ã˜Â¯ Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž
            if (downloadSuccess)
            {
                try
                {
                    if (!File.Exists(newExePath))
                        throw new Exception("Ã™ÂÃ˜Â´Ã™â€ž Ã˜Â­Ã™ÂÃ˜Â¸ Ã™â€¦Ã™â€žÃ™Â Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«");

                    FileInfo fi = new FileInfo(newExePath);
                    if (fi.Length < 100000)
                        throw new Exception("Ã™â€¦Ã™â€žÃ™Â Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã˜ÂªÃ˜Â§Ã™â€žÃ™Â Ã˜Â£Ã™Ë† Ã˜ÂºÃ™Å Ã˜Â± Ã™â€¦Ã™Æ’Ã˜ÂªÃ™â€¦Ã™â€ž (Ã˜Â§Ã™â€žÃ˜Â­Ã˜Â¬Ã™â€¦ Ã˜Â£Ã˜ÂµÃ˜ÂºÃ˜Â± Ã™â€¦Ã™â€  Ã˜Â§Ã™â€žÃ™â€¦Ã˜ÂªÃ™Ë†Ã™â€šÃ˜Â¹)");

                    if (!string.IsNullOrEmpty(expectedSha256))
                    {
                        string actualSha256 = ComputeSha256(newExePath);
                        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(newExePath);
                            throw new Exception(
                                $"Ã™ÂÃ˜Â´Ã™â€ž Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€šÃ™â€š Ã™â€¦Ã™â€  Ã˜Â³Ã™â€žÃ˜Â§Ã™â€¦Ã˜Â© Ã˜Â§Ã™â€žÃ™â€¦Ã™â€žÃ™Â (checksum Ã˜ÂºÃ™Å Ã˜Â± Ã™â€¦Ã˜Â·Ã˜Â§Ã˜Â¨Ã™â€š).\n" +
                                $"Ã˜Â§Ã™â€žÃ™â€¦Ã˜ÂªÃ™Ë†Ã™â€šÃ˜Â¹: {expectedSha256}\n" +
                                $"Ã˜Â§Ã™â€žÃ™ÂÃ˜Â¹Ã™â€žÃ™Å :  {actualSha256}\n\n" +
                                "Ã˜ÂªÃ™â€¦ Ã˜Â­Ã˜Â°Ã™Â Ã˜Â§Ã™â€žÃ™â€¦Ã™â€žÃ™Â Ã™â€žÃ˜Â£Ã˜Â³Ã˜Â¨Ã˜Â§Ã˜Â¨ Ã˜Â£Ã™â€¦Ã™â€ Ã™Å Ã˜Â©. Ã™Å Ã˜Â±Ã˜Â¬Ã™â€° Ã˜Â§Ã™â€žÃ™â€¦Ã˜Â­Ã˜Â§Ã™Ë†Ã™â€žÃ˜Â© Ã™â€¦Ã˜Â±Ã˜Â© Ã˜Â£Ã˜Â®Ã˜Â±Ã™â€°.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    downloadSuccess = false;
                    MessageBox.Show(
                        $"Ã™ÂÃ˜Â´Ã™â€ž Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã™â€šÃ™â€š Ã™â€¦Ã™â€  Ã™â€¦Ã™â€žÃ™Â Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«.\n\nÃ˜Â§Ã™â€žÃ˜Â³Ã˜Â¨Ã˜Â¨: {ex.Message}",
                        "Ã˜Â®Ã˜Â·Ã˜Â£ Ã™ÂÃ™Å  Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«", MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    return;
                }
            }

            if (!downloadSuccess)
            {
                MessageBox.Show(
                    $"Ã™ÂÃ˜Â´Ã™â€ž Ã˜ÂªÃ™â€ Ã˜Â²Ã™Å Ã™â€ž Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã˜Â¨Ã˜Â¹Ã˜Â¯ 3 Ã™â€¦Ã˜Â­Ã˜Â§Ã™Ë†Ã™â€žÃ˜Â§Ã˜Âª.\n\nÃ˜Â§Ã™â€žÃ˜Â³Ã˜Â¨Ã˜Â¨: {downloadException?.Message}",
                    "Ã˜Â®Ã˜Â·Ã˜Â£ Ã™ÂÃ™Å  Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«", MessageBoxButtons.OK, MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return;
            }

            try
            {
                MessageBox.Show(
                    "Ã¢Å“â€¦ Ã˜ÂªÃ™â€¦ Ã˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â« Ã˜Â¨Ã™â€ Ã˜Â¬Ã˜Â§Ã˜Â­!\n\n" +
                    $"Ã°Å¸â€œÂ Ã˜ÂªÃ™â€¦ Ã˜Â­Ã™ÂÃ˜Â¸ Ã™â€¦Ã™â€žÃ™Â Ã˜Â§Ã™â€žÃ˜Â¨Ã˜Â±Ã™â€ Ã˜Â§Ã™â€¦Ã˜Â¬ Ã˜Â§Ã™â€žÃ˜Â¬Ã˜Â¯Ã™Å Ã˜Â¯ Ã˜Â¨Ã˜Â§Ã˜Â³Ã™â€¦ {versionedExeName} Ã˜Â¯Ã˜Â§Ã˜Â®Ã™â€ž Ã™â€¦Ã˜Â¬Ã™â€žÃ˜Â¯ (Updates).\n\n" +
                    "Ã˜Â³Ã™Å Ã˜ÂªÃ™â€¦ Ã˜Â§Ã™â€žÃ˜Â¢Ã™â€  Ã˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â¯ Ã˜Â§Ã™â€žÃ™â€¦Ã™â€žÃ™Â Ã˜Â§Ã™â€žÃ˜Â¬Ã˜Â¯Ã™Å Ã˜Â¯ Ã˜ÂªÃ™â€žÃ™â€šÃ˜Â§Ã˜Â¦Ã™Å Ã˜Â§Ã™â€¹ Ã™Ë†Ã˜Â¥Ã˜ÂºÃ™â€žÃ˜Â§Ã™â€š Ã˜Â§Ã™â€žÃ˜Â¨Ã˜Â±Ã™â€ Ã˜Â§Ã™â€¦Ã˜Â¬ Ã˜Â§Ã™â€žÃ˜Â­Ã˜Â§Ã™â€žÃ™Å .",
                    "Ã˜Â§Ã™Æ’Ã˜ÂªÃ™â€¦Ã™â€ž Ã˜ÂªÃ˜Â­Ã™â€¦Ã™Å Ã™â€ž Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«",
                    MessageBoxButtons.OK, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                Process.Start("explorer.exe", $"/select,\"{newExePath}\"");
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ã™ÂÃ˜Â´Ã™â€ž Ã™ÂÃ˜ÂªÃ˜Â­ Ã™â€¦Ã˜Â¬Ã™â€žÃ˜Â¯ Ã˜Â§Ã™â€žÃ˜ÂªÃ˜Â­Ã˜Â¯Ã™Å Ã˜Â«Ã˜Â§Ã˜Âª:\n" + ex.Message,
                    "Ã˜ÂªÃ™â€ Ã˜Â¨Ã™Å Ã™â€¡", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

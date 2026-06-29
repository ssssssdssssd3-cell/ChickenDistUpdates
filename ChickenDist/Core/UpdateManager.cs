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
        // ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂµÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â± ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã…Â  Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¬
        public const string CurrentVersion = "1.9.61";
        
        // ÃƒËœÃ‚Â±ÃƒËœÃ‚Â§ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â· Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚ÂµÃƒâ„¢Ã…Â  ÃƒËœÃ‚Â¹Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â° GitHub
        private const string UpdateUrl = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/update.txt";

        public static void CheckForUpdates(bool showNoUpdateMsg = false)
        {
            try
            {
                using (var client = new WebClient())
                {
                    // ÃƒËœÃ‚ÂªÃƒâ„¢Ã‚ÂÃƒËœÃ‚Â¹Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â±Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚ÂªÃƒâ„¢Ã‹â€ Ãƒâ„¢Ã†â€™Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Å¾ TLS 1.2 Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â±Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚ÂªÃƒâ„¢Ã‹â€ Ãƒâ„¢Ã†â€™Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§ÃƒËœÃ‚Âª ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â£ÃƒËœÃ‚Â®ÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â° Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§ÃƒËœÃ‚ÂªÃƒËœÃ‚ÂµÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¢Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â¨Ãƒâ„¢Ã¢â€šÂ¬ GitHub
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                    client.Encoding = System.Text.Encoding.UTF8;
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
                    
                    // ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â¨Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§ÃƒËœÃ‚Âª Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¹ Ãƒâ„¢Ã†â€™ÃƒËœÃ‚Â³ÃƒËœÃ‚Â± ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â®ÃƒËœÃ‚Â²Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¤Ãƒâ„¢Ã¢â‚¬Å¡ÃƒËœÃ‚Âª (Cache Busting)
                    string cacheBustedUrl = UpdateUrl + (UpdateUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                    string rawData = client.DownloadString(cacheBustedUrl);
                    rawData = rawData.TrimStart('\uFEFF');
                    string remoteVersion = "";
                    string downloadUrl   = "";
                    string changelog     = "";
                    string expectedSha256 = ""; // checksum Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã¢â‚¬Å¡ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â³Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â© ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â

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
                            else if (key == "sha256")     expectedSha256 = val; // Ãƒâ„¢Ã¢â‚¬Å¡ÃƒËœÃ‚Â±ÃƒËœÃ‚Â§ÃƒËœÃ‚Â¡ÃƒËœÃ‚Â© ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â€šÂ¬ checksum
                        }
                    }

                    if (string.IsNullOrEmpty(remoteVersion) || string.IsNullOrEmpty(downloadUrl))
                    {
                        if (showNoUpdateMsg)
                            MessageBox.Show("Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦ Ãƒâ„¢Ã…Â ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¹ÃƒËœÃ‚Â«Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚Â± ÃƒËœÃ‚Â¹Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â° Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¹Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â§ÃƒËœÃ‚Âª ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚ÂµÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­ÃƒËœÃ‚Â©.", "ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â¨Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Â¡", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¡ÃƒËœÃ‚Â§ÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â© ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂµÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â± ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã…Â  ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¯
                    Version local  = new Version(CurrentVersion);
                    Version remote = new Version(remoteVersion);

                    if (remote > local)
                    {
                        var result = MessageBox.Show(
                            $"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ Ãƒâ„¢Ã…Â Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯ ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¯ Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚ÂªÃƒËœÃ‚Â§ÃƒËœÃ‚Â­ Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¬!\n\n" +
                            $"ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂµÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â± ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã…Â  Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â Ãƒâ„¢Ã†â€™:  {CurrentVersion}\n" +
                            $"ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂµÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â± ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚ÂªÃƒËœÃ‚Â§ÃƒËœÃ‚Â­: {remoteVersion}\n\n" +
                            $"ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‚Â Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â§ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¯ Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  Ãƒâ„¢Ã¢â‚¬Â¡ÃƒËœÃ‚Â°ÃƒËœÃ‚Â§ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂµÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â±:\n{changelog}\n\n" +
                            $"Ãƒâ„¢Ã¢â‚¬Â¡Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚ÂªÃƒËœÃ‚Â±ÃƒËœÃ‚ÂºÃƒËœÃ‚Â¨ Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â²Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚ÂªÃƒËœÃ‚Â«ÃƒËœÃ‚Â¨Ãƒâ„¢Ã…Â ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¡ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¢Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ…Â¸",
                            "ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¯ Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚ÂªÃƒËœÃ‚Â§ÃƒËœÃ‚Â­",
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
                                $"ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ ÃƒËœÃ‚Â£Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Âª ÃƒËœÃ‚ÂªÃƒËœÃ‚Â³ÃƒËœÃ‚ÂªÃƒËœÃ‚Â®ÃƒËœÃ‚Â¯Ãƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚Â£ÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯ÃƒËœÃ‚Â« ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂµÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â± ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â¹Ãƒâ„¢Ã¢â‚¬Å¾.\n\n" +
                                $"ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂµÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â±Ãƒâ„¢Ã†â€™ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã…Â : {CurrentVersion}\n\n" +
                                $"ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‚Â ÃƒËœÃ‚Â¢ÃƒËœÃ‚Â®ÃƒËœÃ‚Â± ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«ÃƒËœÃ‚Â§ÃƒËœÃ‚Âª Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  Ãƒâ„¢Ã¢â‚¬Â¡ÃƒËœÃ‚Â°ÃƒËœÃ‚Â§ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂµÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â±:\n{changelog}",
                                "ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¬",
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
                    MessageBox.Show("Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â´Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§ÃƒËœÃ‚ÂªÃƒËœÃ‚ÂµÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â³Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â±Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â± ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«.\nÃƒâ„¢Ã…Â ÃƒËœÃ‚Â±ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Â° ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã¢â‚¬Å¡ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§ÃƒËœÃ‚ÂªÃƒËœÃ‚ÂµÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¥Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚ÂªÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Âª.", "ÃƒËœÃ‚Â®ÃƒËœÃ‚Â·ÃƒËœÃ‚Â£ Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã¢â‚¬Å¡ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  SHA-256 ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
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
                MessageBox.Show("Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â´Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â¥Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â´ÃƒËœÃ‚Â§ÃƒËœÃ‚Â¡ Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«ÃƒËœÃ‚Â§ÃƒËœÃ‚Âª Updates:\n" + ex.Message,
                    "ÃƒËœÃ‚Â®ÃƒËœÃ‚Â·ÃƒËœÃ‚Â£", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // FIX: ÃƒËœÃ‚Â§ÃƒËœÃ‚Â³ÃƒËœÃ‚ÂªÃƒËœÃ‚Â®ÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ BackgroundWorker ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â¯Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¹ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  DownloadFile ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚ÂªÃƒËœÃ‚Â²ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â 
            // ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã†â€™Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Å¡ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Â¦ Ãƒâ„¢Ã†â€™ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â  Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â€šÂ¬ UI Thread ÃƒËœÃ‚Â·Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¯ÃƒËœÃ‚Â© ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¹ Thread.Sleep ÃƒËœÃ‚Â¹Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â° UI
            // ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Å¾: ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  background thread Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¹ ProgressBar ÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã…Â  Ãƒâ„¢Ã…Â ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯ÃƒËœÃ‚Â« ÃƒËœÃ‚Â¹ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â± ReportProgress

            Exception downloadException = null;
            bool downloadSuccess = false;

            using (var progressForm = new Form())
            using (var worker = new System.ComponentModel.BackgroundWorker())
            {
                progressForm.Text = "ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â§ÃƒËœÃ‚Â±Ãƒâ„¢Ã…Â  ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«...";
                progressForm.Size = new System.Drawing.Size(420, 160);
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.MaximizeBox = false;
                progressForm.MinimizeBox = false;
                progressForm.RightToLeft = RightToLeft.Yes;
                progressForm.RightToLeftLayout = true;
                progressForm.ControlBox = false; // Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â¹ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂºÃƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¡ ÃƒËœÃ‚Â£ÃƒËœÃ‚Â«Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§ÃƒËœÃ‚Â¡ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾

                var lbl = new Label
                {
                    Text = "ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â§ÃƒËœÃ‚Â±Ãƒâ„¢Ã…Â  ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â³Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â±Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â±ÃƒËœÃ…â€™ Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â±ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Â° ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚ÂªÃƒËœÃ‚Â¸ÃƒËœÃ‚Â§ÃƒËœÃ‚Â±...",
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
                            worker.ReportProgress(0, $"Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â­ÃƒËœÃ‚Â§Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â© {attempt} Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  {maxRetries}...");

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

                                // DownloadFileAsync + AutoResetEvent Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ async -> sync Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  background thread
                                var done = new System.Threading.AutoResetEvent(false);
                                Exception innerEx = null;
                                client.DownloadFileCompleted += (cs, ce) =>
                                {
                                    innerEx = ce.Error;
                                    done.Set();
                                };
                                string cacheBustedDownloadUrl = downloadUrl + (downloadUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                                client.DownloadFileAsync(new Uri(cacheBustedDownloadUrl), newExePath);
                                done.WaitOne(); // Ãƒâ„¢Ã¢â‚¬Â Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚ÂªÃƒËœÃ‚Â¸ÃƒËœÃ‚Â± Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â€šÂ¬ background thread ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§ Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â€šÂ¬ UI

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
                                System.Threading.Thread.Sleep(1500); // ÃƒËœÃ‚Â¢Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  Ãƒâ„¢Ã¢â‚¬Â¡Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§ Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â£Ãƒâ„¢Ã¢â‚¬Â Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§ Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  background thread
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
                progressForm.ShowDialog(); // Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¨Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã¢â‚¬Â° UI Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â³ÃƒËœÃ‚ÂªÃƒËœÃ‚Â¬Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¹ Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â£Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  background
            }

            // ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã¢â‚¬Å¡ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â³Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â© ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â¹ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾
            if (downloadSuccess)
            {
                try
                {
                    if (!File.Exists(newExePath))
                        throw new Exception("Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â´Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â­Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â¸ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«");

                    FileInfo fi = new FileInfo(newExePath);
                    if (fi.Length < 100000)
                        throw new Exception("Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚ÂªÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â£Ãƒâ„¢Ã‹â€  ÃƒËœÃ‚ÂºÃƒâ„¢Ã…Â ÃƒËœÃ‚Â± Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã†â€™ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾ (ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚Â£ÃƒËœÃ‚ÂµÃƒËœÃ‚ÂºÃƒËœÃ‚Â± Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚ÂªÃƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Å¡ÃƒËœÃ‚Â¹)");

                    if (!string.IsNullOrEmpty(expectedSha256))
                    {
                        string actualSha256 = ComputeSha256(newExePath);
                        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(newExePath);
                            throw new Exception(
                                $"Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â´Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã¢â‚¬Å¡ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚Â³Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â© ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â (checksum ÃƒËœÃ‚ÂºÃƒâ„¢Ã…Â ÃƒËœÃ‚Â± Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â·ÃƒËœÃ‚Â§ÃƒËœÃ‚Â¨Ãƒâ„¢Ã¢â‚¬Å¡).\n" +
                                $"ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚ÂªÃƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Å¡ÃƒËœÃ‚Â¹: {expectedSha256}\n" +
                                $"ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â¹Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã…Â :  {actualSha256}\n\n" +
                                "ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚Â­ÃƒËœÃ‚Â°Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â£ÃƒËœÃ‚Â³ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â§ÃƒËœÃ‚Â¨ ÃƒËœÃ‚Â£Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â©. Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â±ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Â° ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â­ÃƒËœÃ‚Â§Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â© Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â±ÃƒËœÃ‚Â© ÃƒËœÃ‚Â£ÃƒËœÃ‚Â®ÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â°.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    downloadSuccess = false;
                    MessageBox.Show(
                        $"Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â´Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Å¡Ãƒâ„¢Ã¢â‚¬Å¡ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Â  Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«.\n\nÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â³ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â¨: {ex.Message}",
                        "ÃƒËœÃ‚Â®ÃƒËœÃ‚Â·ÃƒËœÃ‚Â£ Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«", MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    return;
                }
            }

            if (!downloadSuccess)
            {
                MessageBox.Show(
                    $"Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â´Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â²Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â¹ÃƒËœÃ‚Â¯ 3 Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â­ÃƒËœÃ‚Â§Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§ÃƒËœÃ‚Âª.\n\nÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â³ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â¨: {downloadException?.Message}",
                    "ÃƒËœÃ‚Â®ÃƒËœÃ‚Â·ÃƒËœÃ‚Â£ Ãƒâ„¢Ã‚ÂÃƒâ„¢Ã…Â  ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«", MessageBoxButtons.OK, MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return;
            }

            try
            {
                MessageBox.Show(
                    "ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚Â¨Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â§ÃƒËœÃ‚Â­!\n\n" +
                    $"ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‚Â ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚Â­Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â¸ Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¬ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â§ÃƒËœÃ‚Â³Ãƒâ„¢Ã¢â‚¬Â¦ {versionedExeName} ÃƒËœÃ‚Â¯ÃƒËœÃ‚Â§ÃƒËœÃ‚Â®Ãƒâ„¢Ã¢â‚¬Å¾ Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¯ (Updates).\n\n" +
                    "ÃƒËœÃ‚Â³Ãƒâ„¢Ã…Â ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¢Ãƒâ„¢Ã¢â‚¬Â  ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã‚Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â¯ ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã¢â‚¬Å¡ÃƒËœÃ‚Â§ÃƒËœÃ‚Â¦Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¹ Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚Â¥ÃƒËœÃ‚ÂºÃƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¡ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¨ÃƒËœÃ‚Â±Ãƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¬ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾Ãƒâ„¢Ã…Â .",
                    "ÃƒËœÃ‚Â§Ãƒâ„¢Ã†â€™ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­Ãƒâ„¢Ã¢â‚¬Â¦Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«",
                    MessageBoxButtons.OK, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                Process.Start("explorer.exe", $"/select,\"{newExePath}\"");
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â´Ãƒâ„¢Ã¢â‚¬Å¾ Ãƒâ„¢Ã‚ÂÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«ÃƒËœÃ‚Â§ÃƒËœÃ‚Âª:\n" + ex.Message,
                    "ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â ÃƒËœÃ‚Â¨Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Â¡", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

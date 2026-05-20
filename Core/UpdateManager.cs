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
        public const string CurrentVersion = "1.0.4";
        
        // رابط ملف التحديث النصي على GitHub
        private const string UpdateUrl = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/update.txt";

        public static void CheckForUpdates(bool showNoUpdateMsg = false)
        {
            try
            {
                using (var client = new WebClient())
                {
                    // تفعيل بروتوكول TLS 1.2 للاتصال الآمن بـ GitHub
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.Encoding = System.Text.Encoding.UTF8;
                    
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
                            PerformUpdate(downloadUrl);
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
                if (showNoUpdateMsg)
                {
                    MessageBox.Show("فشل الاتصال بسيرفر التحديث للتأكد من وجود إصدار جديد:\n" + ex.Message, "خطأ في التحديث", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void PerformUpdate(string downloadUrl)
        {
            try
            {
                string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                string currentDir = Path.GetDirectoryName(currentExePath);
                string newExePath = Path.Combine(currentDir, "ChickenDist_New.exe");
                string updaterBatPath = Path.Combine(currentDir, "updater.bat");

                // إظهار نافذة تحميل مبسطة
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

                    using (var client = new WebClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DownloadFile(downloadUrl, newExePath);
                    }
                    progressForm.Close();
                }

                // كتابة ملف الـ batch لاستبدال ملف الـ EXE القديم بالجديد
                // يتم الانتظار لمدة ثانيتين لضمان إغلاق البرنامج بالكامل قبل محاولة الكتابة فوقه
                string batContent = $@"@echo off
chcp 65001 > nul
echo.
echo ====================================================
echo             جاري تثبيت تحديث البرنامج...
echo ====================================================
echo.
timeout /t 2 /nobreak > nul
copy /y ""{newExePath}"" ""{currentExePath}"" > nul
del ""{newExePath}"" > nul
echo تم التحديث بنجاح! جاري تشغيل التطبيق...
start """" ""{currentExePath}""
del ""%~f0""
";

                File.WriteAllText(updaterBatPath, batContent, System.Text.Encoding.UTF8);

                // تشغيل ملف الـ bat وإغلاق التطبيق الحالي فوراً
                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterBatPath,
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Process.Start(startInfo);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تنزيل أو تثبيت التحديث الجديد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

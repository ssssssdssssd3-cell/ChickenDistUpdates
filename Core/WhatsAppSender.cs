using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    public static class WhatsAppSender
    {
        public static string CleanPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            string clean = Regex.Replace(phone, @"[^\d]", "");
            if (clean.StartsWith("0")) clean = "20" + clean.Substring(1);
            return clean;
        }

        public static void OpenWhatsApp(string phone, string message)
        {
            string clean = CleanPhone(phone);
            if (string.IsNullOrEmpty(clean))
            {
                MessageBox.Show("رقم الهاتف غير صحيح!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string encoded = Uri.EscapeDataString(message);
            string appUrl = $"whatsapp://send?phone={clean}&text={encoded}";

            try
            {
                Process.Start(new ProcessStartInfo(appUrl) { UseShellExecute = true });
            }
            catch
            {
                string waUrl = $"https://wa.me/{clean}?text={encoded}";
                try
                {
                    Process.Start(new ProcessStartInfo(waUrl) { UseShellExecute = true });
                }
                catch
                {
                    try
                    {
                        Process.Start("explorer.exe", $"\"{waUrl}\"");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل فتح واتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public static void OpenWhatsAppChat(string phone)
        {
            string clean = CleanPhone(phone);
            if (string.IsNullOrEmpty(clean)) return;

            string appUrl = $"whatsapp://send?phone={clean}";
            try
            {
                Process.Start(new ProcessStartInfo(appUrl) { UseShellExecute = true });
            }
            catch
            {
                string waUrl = $"https://wa.me/{clean}";
                try
                {
                    Process.Start(new ProcessStartInfo(waUrl) { UseShellExecute = true });
                }
                catch
                {
                    Process.Start("explorer.exe", $"\"{waUrl}\"");
                }
            }
        }
    }
}

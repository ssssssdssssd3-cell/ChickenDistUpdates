using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

        public static void SendImage(string phone, Image img, string caption = "")
        {
            if (img == null)
            {
                MessageBox.Show("عذراً، الصورة غير متوفرة للإرسال!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Clipboard.SetImage(img);
                string tempFile = Path.Combine(Path.GetTempPath(), $"whatsapp_{DateTime.Now.Ticks}.png");
                img.Save(tempFile, ImageFormat.Png);

                OpenWhatsApp(phone, caption);

                MessageBox.Show(
                    "📋 تم نسخ صورة السند/الفاتورة إلى حافظة الوينيدوز وتجهيزها للإرسال!\n\n" +
                    "عند فتح محادثة الواتساب:\n" +
                    "1. اضغط (Ctrl + V) داخل المحادثة للصق الصورة مباشرة.\n" +
                    "2. ثم اضغط Enter للإرسال.",
                    "جاهز للإرسال عبر الواتساب",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("WhatsAppSender.SendImage", ex);
                MessageBox.Show("فشل إرسال صورة الواتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// نافذة حوار موحدة وشاملة لاختيار طريقة إرسال الواتساب (نصية أو صورة) عبر أي شاشة بالبرنامج
        /// </summary>
        public static void ShowWhatsAppSendOptionsDialog(Form parentForm, string clientPhone, string textMessage, Func<Image> imageGenerator, string dialogTitle = "📱 إرسال عبر الواتساب")
        {
            using (var dlg = new Form())
            {
                dlg.Text = dialogTitle;
                dlg.Size = new Size(540, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = Theme.FontMain;

                var pnlHeader = Theme.MakeTitleBar("📱 إرسال عبر الواتساب", "اختر نوع الإرسال للعميل (رسالة نصية تفصيلية أو تصميم صورة كارت عالي الجودة)");
                dlg.Controls.Add(pnlHeader);

                int y = 74;

                // Client Phone Input
                var lblPhone = new Label { Text = "رقم هاتف العميل (واتساب):", Location = new Point(20, y), Width = 480, Height = 20, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.TextMain };
                dlg.Controls.Add(lblPhone);
                y += 24;

                var txtPhone = new TextBox { Text = clientPhone ?? "", Location = new Point(20, y), Width = 480, Font = new Font("Segoe UI", 11f, FontStyle.Bold), BackColor = Color.White, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
                dlg.Controls.Add(txtPhone);
                y += 38;

                // Options (Text vs Image)
                var lblMode = new Label { Text = "اختر صيغة الإرسال المطلوبة:", Location = new Point(20, y), Width = 480, Height = 20, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.TextMain };
                dlg.Controls.Add(lblMode);
                y += 24;

                var rbText = new RadioButton { Text = "📝 إرسال نصي (رسالة نصية تفصيلية تحتوي على البيانات والحساب)", Location = new Point(30, y), Width = 460, Checked = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain };
                dlg.Controls.Add(rbText);
                y += 30;

                var rbImage = new RadioButton { Text = "🖼️ إرسال صورة (تصميم كارت الفاتورة/السند بصورة احترافية)", Location = new Point(30, y), Width = 460, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain };
                dlg.Controls.Add(rbImage);
                y += 38;

                // Preview Box
                var txtPreview = new TextBox
                {
                    Location = new Point(20, y),
                    Width = 480,
                    Height = 110,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = textMessage,
                    Font = new Font("Segoe UI", 9.5f),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(30, 41, 59)
                };
                dlg.Controls.Add(txtPreview);
                y += 120;

                rbText.CheckedChanged += (s, e) =>
                {
                    if (rbText.Checked)
                    {
                        txtPreview.Text = textMessage;
                    }
                };

                rbImage.CheckedChanged += (s, e) =>
                {
                    if (rbImage.Checked)
                    {
                        txtPreview.Text = "🖼️ سيتم إنشاء صورة كارت احترافية للفاتورة/السند ونسخها للحافظة وفتح محادثة الواتساب للصقها مباشرة بـ (Ctrl + V).";
                    }
                };

                // Footer Actions
                var btnSend = Theme.MakeButton("🚀 إرسال الآن عبر الواتساب", 280, y, 220, 42, Color.FromArgb(37, 211, 102));
                btnSend.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                dlg.Controls.Add(btnSend);

                var btnClose = Theme.MakeButton("❌ إلغاء", 20, y, 120, 42, Color.FromArgb(100, 116, 139));
                btnClose.Click += (s, e) => dlg.Close();
                dlg.Controls.Add(btnClose);

                btnSend.Click += (s, e) =>
                {
                    string targetPhone = txtPhone.Text.Trim();
                    if (string.IsNullOrWhiteSpace(targetPhone))
                    {
                        MessageBox.Show("يرجى إدخال رقم هاتف الواتساب للعميل أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPhone.Focus();
                        return;
                    }

                    if (rbText.Checked)
                    {
                        OpenWhatsApp(targetPhone, textMessage);
                    }
                    else
                    {
                        Image img = null;
                        if (imageGenerator != null)
                        {
                            try { img = imageGenerator(); } catch { }
                        }
                        if (img != null)
                        {
                            SendImage(targetPhone, img, "📄 صورة السند / الفاتورة");
                        }
                        else
                        {
                            OpenWhatsApp(targetPhone, textMessage);
                        }
                    }

                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                dlg.ShowDialog(parentForm);
            }
        }
    }
}

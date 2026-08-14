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
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            string clean = Regex.Replace(phone, @"[^\d]", string.Empty);
            if (clean.StartsWith("00")) clean = clean.Substring(2);

            if (clean.Length == 11 && clean.StartsWith("01"))
            {
                clean = "2" + clean;
            }
            else if (clean.Length == 10 && clean.StartsWith("1"))
            {
                clean = "20" + clean;
            }
            else if (clean.Length == 10 && clean.StartsWith("05"))
            {
                clean = "966" + clean.Substring(1);
            }
            else if (clean.Length == 9 && clean.StartsWith("5"))
            {
                clean = "966" + clean;
            }

            return clean;
        }

        public static void OpenWhatsApp(string phone, string message)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("يرجى إدخال رقم هاتف العميل أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string clean = NormalizePhone(phone);
            string encoded = Uri.EscapeDataString(message ?? "");

            try
            {
                string appUrl = $"whatsapp://send?phone={clean}&text={encoded}";
                try
                {
                    Process.Start(new ProcessStartInfo(appUrl) { UseShellExecute = true });
                    return;
                }
                catch { }

                string apiUri = $"https://api.whatsapp.com/send?phone={clean}&text={encoded}";
                try
                {
                    Process.Start(new ProcessStartInfo(apiUri) { UseShellExecute = true });
                    return;
                }
                catch { }

                string waUri = $"https://wa.me/{clean}?text={encoded}";
                try
                {
                    Process.Start(new ProcessStartInfo(waUri) { UseShellExecute = true });
                    return;
                }
                catch { }

                string webUrl = $"https://web.whatsapp.com/send?phone={clean}&text={encoded}";
                Process.Start("explorer.exe", webUrl);
            }
            catch (Exception ex)
            {
                AppLogger.Error("WhatsAppSender.OpenWhatsApp", ex);
                MessageBox.Show("فشل فتح الواتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void OpenWhatsAppChat(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return;
            string clean = NormalizePhone(phone);

            try
            {
                string appUrl = $"whatsapp://send?phone={clean}";
                try
                {
                    Process.Start(new ProcessStartInfo(appUrl) { UseShellExecute = true });
                    return;
                }
                catch { }

                string apiUri = $"https://api.whatsapp.com/send?phone={clean}";
                try
                {
                    Process.Start(new ProcessStartInfo(apiUri) { UseShellExecute = true });
                    return;
                }
                catch { }

                string waUri = $"https://wa.me/{clean}";
                try
                {
                    Process.Start(new ProcessStartInfo(waUri) { UseShellExecute = true });
                    return;
                }
                catch { }

                string webUrl = $"https://web.whatsapp.com/send?phone={clean}";
                Process.Start("explorer.exe", webUrl);
            }
            catch (Exception ex)
            {
                AppLogger.Error("WhatsAppSender.OpenWhatsAppChat", ex);
            }
        }

        public static void SendImage(string phone, Image img, string caption = "")
        {
            if (img == null) return;

            try
            {
                Clipboard.SetImage(img);

                string tempDir = Path.Combine(Path.GetTempPath(), "ProSoft_WhatsApp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string tempFile = Path.Combine(tempDir, $"Receipt_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                img.Save(tempFile, ImageFormat.Png);

                OpenWhatsAppChat(phone);

                MessageBox.Show(
                    "✅ تم تصميم الصورة بنجاح ونسخها إلى حافظة الويندوز (Clipboard)!\n\n" +
                    "عند فتح محادثة الواتساب للعميل:\n" +
                    "1. اضغط (Ctrl + V) داخل مربع الكتابة للصق الصورة فوراً.\n" +
                    "2. اضغط Enter للإرسال.",
                    "تم النسخ للحافظة - جاهز للإرسال",
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
        /// نافذة حوار موحدة لاختيار طريقة إرسال الواتساب (نصية أو صورة) مع معاينة حية فورية لكلا الخيارين
        /// </summary>
        public static void ShowWhatsAppSendOptionsDialog(Form parentForm, string clientPhone, string textMessage, Func<Image> imageGenerator = null, string dialogTitle = "📱 إرسال عبر الواتساب")
        {
            using (var dlg = new Form())
            {
                dlg.Text = dialogTitle;
                dlg.Size = new Size(680, 570);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = Theme.FontMain;

                var pnlHeader = Theme.MakeTitleBar(dialogTitle, "اختر طريقة الإرسال المناسبة للعميل: رسالة نصية تفصيلية أو تصميم صورة كارت عالي الجودة");
                dlg.Controls.Add(pnlHeader);

                int y = 72;

                // Client Phone Panel
                var lblPhone = new Label { Text = "رقم هاتف العميل (واتساب):", Location = new Point(24, y), Width = 180, Height = 22, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.TextMain };
                dlg.Controls.Add(lblPhone);

                var txtPhone = new TextBox { Text = clientPhone ?? "", Location = new Point(210, y - 2), Width = 440, Font = new Font("Segoe UI", 11f, FontStyle.Bold), BackColor = Color.White, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
                dlg.Controls.Add(txtPhone);
                y += 36;

                // Send Mode Buttons (Tab-like selectors)
                var pnlTabs = new Panel { Location = new Point(24, y), Size = new Size(626, 42), BackColor = Color.FromArgb(226, 232, 240) };
                
                var btnTabText = new Button
                {
                    Text = "📝 إرسال رسالة نصية (Text Message)",
                    Location = new Point(314, 2),
                    Size = new Size(310, 38),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnTabText.FlatAppearance.BorderSize = 0;

                var btnTabImage = new Button
                {
                    Text = "🖼️ إرسال كارت مصمم (Image Card)",
                    Location = new Point(2, 2),
                    Size = new Size(310, 38),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnTabImage.FlatAppearance.BorderSize = 0;

                pnlTabs.Controls.Add(btnTabText);
                pnlTabs.Controls.Add(btnTabImage);
                dlg.Controls.Add(pnlTabs);
                y += 48;

                // Content Panels (Text vs Image Preview)
                var pnlContent = new Panel { Location = new Point(24, y), Size = new Size(626, 300), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                var txtPreview = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.None,
                    Text = textMessage,
                    Font = new Font("Segoe UI", 10f),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(30, 41, 59),
                    RightToLeft = RightToLeft.Yes
                };

                var picPreview = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(241, 245, 249),
                    Visible = false
                };

                pnlContent.Controls.Add(txtPreview);
                pnlContent.Controls.Add(picPreview);
                dlg.Controls.Add(pnlContent);
                y += 310;

                // State & Mode Switcher
                bool isImageMode = false;
                Image cachedImage = null;

                Action updateTabSelection = () =>
                {
                    if (isImageMode)
                    {
                        btnTabImage.BackColor = Theme.Accent;
                        btnTabImage.ForeColor = Color.White;
                        btnTabText.BackColor = Color.FromArgb(241, 245, 249);
                        btnTabText.ForeColor = Theme.TextMain;

                        txtPreview.Visible = false;
                        picPreview.Visible = true;

                        if (cachedImage == null)
                        {
                            try
                            {
                                if (imageGenerator != null) cachedImage = imageGenerator();
                            }
                            catch { }

                            if (cachedImage == null)
                            {
                                cachedImage = ReceiptImageGenerator.GenerateTextCardImage(dialogTitle, textMessage);
                            }
                        }

                        picPreview.Image = cachedImage;
                    }
                    else
                    {
                        btnTabText.BackColor = Theme.Primary;
                        btnTabText.ForeColor = Color.White;
                        btnTabImage.BackColor = Color.FromArgb(241, 245, 249);
                        btnTabImage.ForeColor = Theme.TextMain;

                        picPreview.Visible = false;
                        txtPreview.Visible = true;
                    }
                };

                btnTabText.Click += (s, e) => { isImageMode = false; updateTabSelection(); };
                btnTabImage.Click += (s, e) => { isImageMode = true; updateTabSelection(); };

                // Initial Tab State
                updateTabSelection();

                // Action Buttons Footer
                var btnSend = Theme.MakeButton("🚀 إرسال الآن عبر الواتساب", 410, y, 240, 44, Color.FromArgb(37, 211, 102));
                btnSend.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                btnSend.ForeColor = Color.White;
                dlg.Controls.Add(btnSend);

                var btnCopy = Theme.MakeButton("📋 نسخ النص / الصورة", 220, y, 180, 44, Color.FromArgb(70, 80, 100));
                btnCopy.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                dlg.Controls.Add(btnCopy);

                var btnClose = Theme.MakeButton("❌ إلغاء", 24, y, 110, 44, Color.FromArgb(100, 116, 139));
                btnClose.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                btnClose.Click += (s, e) => dlg.Close();
                dlg.Controls.Add(btnClose);

                btnCopy.Click += (s, e) =>
                {
                    try
                    {
                        if (isImageMode && cachedImage != null)
                        {
                            Clipboard.SetImage(cachedImage);
                            MessageBox.Show("✅ تم نسخ صورة الكارت إلى الحافظة بنجاح!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            Clipboard.SetText(txtPreview.Text);
                            MessageBox.Show("✅ تم نسخ نص الرسالة إلى الحافظة بنجاح!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل النسخ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                btnSend.Click += (s, e) =>
                {
                    string targetPhone = txtPhone.Text.Trim();
                    if (string.IsNullOrWhiteSpace(targetPhone))
                    {
                        MessageBox.Show("يرجى إدخال رقم هاتف العميل أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPhone.Focus();
                        return;
                    }

                    if (isImageMode)
                    {
                        if (cachedImage == null)
                        {
                            if (imageGenerator != null) cachedImage = imageGenerator();
                            if (cachedImage == null) cachedImage = ReceiptImageGenerator.GenerateTextCardImage(dialogTitle, textMessage);
                        }

                        if (cachedImage != null)
                        {
                            SendImage(targetPhone, cachedImage, "📄 إشعار إلكتروني");
                            dlg.Close();
                        }
                        else
                        {
                            OpenWhatsApp(targetPhone, textMessage);
                            dlg.Close();
                        }
                    }
                    else
                    {
                        OpenWhatsApp(targetPhone, txtPreview.Text);
                        dlg.Close();
                    }
                };

                dlg.ShowDialog(parentForm);
            }
        }
    }
}

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
        /// نافذة حوار موحدة تقدم زري الاختيار المباشرين (إرسال نص أو إرسال صورة) مع معاينة حية
        /// </summary>
        public static void ShowWhatsAppSendOptionsDialog(Form parentForm, string clientPhone, string textMessage, Func<Image> imageGenerator = null, string dialogTitle = "📱 إرسال عبر الواتساب")
        {
            using (var dlg = new Form())
            {
                dlg.Text = dialogTitle;
                dlg.Size = new Size(620, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = Theme.FontMain;

                var pnlHeader = Theme.MakeTitleBar(dialogTitle, "اختر نوع الإرسال المطلوب للعميل (رسالة نصية تفصيلية أو كارت صورة مصمم)");
                dlg.Controls.Add(pnlHeader);

                int y = 72;

                // Client Phone Panel
                var lblPhone = new Label { Text = "📱 رقم هاتف العميل (واتساب):", Location = new Point(20, y), Width = 190, Height = 22, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain };
                dlg.Controls.Add(lblPhone);

                var txtPhone = new TextBox { Text = clientPhone ?? "", Location = new Point(220, y - 2), Width = 360, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), BorderStyle = BorderStyle.FixedSingle };
                dlg.Controls.Add(txtPhone);
                y += 38;

                // ── Direct Action Buttons (Text vs Image) ──
                var pnlChoiceBtns = new TableLayoutPanel
                {
                    Location = new Point(20, y),
                    Size = new Size(560, 52),
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = Color.Transparent
                };
                pnlChoiceBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                pnlChoiceBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

                var btnSendText = new Button
                {
                    Text = "💬 إرسال واتساب (رسالة نصية)",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(37, 211, 102),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSendText.FlatAppearance.BorderSize = 0;

                var btnSendImage = new Button
                {
                    Text = "🖼️ إرسال واتساب (كارت صورة)",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(18, 140, 126),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSendImage.FlatAppearance.BorderSize = 0;

                pnlChoiceBtns.Controls.Add(btnSendText, 0, 0);
                pnlChoiceBtns.Controls.Add(btnSendImage, 1, 0);
                dlg.Controls.Add(pnlChoiceBtns);
                y += 60;

                // ── Preview Section with Tab switcher ──
                var pnlPreviewHeader = new Panel { Location = new Point(20, y), Size = new Size(560, 28), BackColor = Color.Transparent };
                var lblPreviewTitle = new Label { Text = "📋 معاينة المحتوى المراد إرساله:", Location = new Point(360, 3), Width = 200, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.TextMain };
                
                var btnShowText = new Button { Text = "معاينة النص 📝", Location = new Point(140, 0), Size = new Size(100, 26), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), BackColor = Theme.Primary, ForeColor = Color.White, Cursor = Cursors.Hand };
                btnShowText.FlatAppearance.BorderSize = 0;

                var btnShowImg = new Button { Text = "معاينة الصورة 🖼️", Location = new Point(30, 0), Size = new Size(105, 26), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Theme.TextMain, Cursor = Cursors.Hand };
                btnShowImg.FlatAppearance.BorderSize = 0;

                pnlPreviewHeader.Controls.Add(lblPreviewTitle);
                pnlPreviewHeader.Controls.Add(btnShowText);
                pnlPreviewHeader.Controls.Add(btnShowImg);
                dlg.Controls.Add(pnlPreviewHeader);
                y += 32;

                // Content Box
                var pnlContent = new Panel { Location = new Point(20, y), Size = new Size(560, 235), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                var txtPreview = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.None,
                    Text = textMessage,
                    Font = new Font("Segoe UI", 9.5f),
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
                y += 245;

                Image cachedImage = null;
                Action showImagePreview = () =>
                {
                    btnShowImg.BackColor = Theme.Accent;
                    btnShowImg.ForeColor = Color.White;
                    btnShowText.BackColor = Color.FromArgb(226, 232, 240);
                    btnShowText.ForeColor = Theme.TextMain;

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
                };

                Action showTextPreview = () =>
                {
                    btnShowText.BackColor = Theme.Primary;
                    btnShowText.ForeColor = Color.White;
                    btnShowImg.BackColor = Color.FromArgb(226, 232, 240);
                    btnShowImg.ForeColor = Theme.TextMain;

                    picPreview.Visible = false;
                    txtPreview.Visible = true;
                };

                btnShowText.Click += (s, e) => showTextPreview();
                btnShowImg.Click += (s, e) => showImagePreview();

                // ── Footer Buttons (Copy & Cancel) ──
                var btnCopy = Theme.MakeButton("📋 نسخ النص للحافظة", 370, y, 210, 38, Color.FromArgb(70, 80, 100));
                btnCopy.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnCopy.Click += (s, e) =>
                {
                    try
                    {
                        if (picPreview.Visible && cachedImage != null)
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
                dlg.Controls.Add(btnCopy);

                var btnClose = Theme.MakeButton("❌ إلغاء", 20, y, 100, 38, Color.FromArgb(120, 130, 140));
                btnClose.Click += (s, e) => dlg.Close();
                dlg.Controls.Add(btnClose);

                // ── Send Handlers ──
                btnSendText.Click += (s, e) =>
                {
                    string targetPhone = txtPhone.Text.Trim();
                    if (string.IsNullOrWhiteSpace(targetPhone))
                    {
                        MessageBox.Show("يرجى إدخال رقم هاتف العميل أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPhone.Focus();
                        return;
                    }

                    OpenWhatsApp(targetPhone, txtPreview.Text);
                    dlg.Close();
                };

                btnSendImage.Click += (s, e) =>
                {
                    string targetPhone = txtPhone.Text.Trim();
                    if (string.IsNullOrWhiteSpace(targetPhone))
                    {
                        MessageBox.Show("يرجى إدخال رقم هاتف العميل أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPhone.Focus();
                        return;
                    }

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
                };

                dlg.ShowDialog(parentForm);
            }
        }
    }
}

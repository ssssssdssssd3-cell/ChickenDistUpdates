using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ChickenDist.Forms;
using ChickenDist.DAL;

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
                    "تم تصميم الصورة بنجاح ونسخها إلى حافظة الويندوز (Clipboard)!\n\n" +
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
        /// نافذة حوار موحدة تقدم زري الاختيار المباشرين (إرسال نص أو إرسال صورة) مع معاينة حية ودعم الصفحات المتعددة وملفات الـ PDF
        /// </summary>
        public static void ShowWhatsAppSendOptionsDialog(Form parentForm, string clientPhone, string textMessage, Func<Image> imageGenerator = null, string dialogTitle = "إرسال عبر الواتساب", Func<string> pdfGenerator = null, Func<System.Collections.Generic.List<Bitmap>> multiImageGenerator = null)
        {
            using (var dlg = new Form())
            {
                dlg.Text = dialogTitle;
                dlg.Size = new Size(780, 690);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = Theme.FontMain;

                var pnlHeader = Theme.MakeTitleBar(dialogTitle, "اختر نوع الإرسال المطلوب للعميل (رسالة نصية تفصيلية، كارت صورة عالي الدقة، أو ملف PDF رسمي)");
                dlg.Controls.Add(pnlHeader);

                int y = 72;

                // Client Phone Panel
                var lblPhone = new Label { Text = "رقم هاتف العميل (واتساب):", Location = new Point(20, y), Width = 190, Height = 22, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain };
                dlg.Controls.Add(lblPhone);

                var txtPhone = new TextBox { Text = clientPhone ?? "", Location = new Point(220, y - 2), Width = 520, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), BorderStyle = BorderStyle.FixedSingle };
                dlg.Controls.Add(txtPhone);
                y += 38;

                // ── Direct Action Buttons (Text vs Image vs PDF) ──
                int btnColumns = pdfGenerator != null ? 3 : 2;
                var pnlChoiceBtns = new TableLayoutPanel
                {
                    Location = new Point(20, y),
                    Size = new Size(720, 52),
                    ColumnCount = btnColumns,
                    RowCount = 1,
                    BackColor = Color.Transparent
                };
                for (int c = 0; c < btnColumns; c++)
                {
                    pnlChoiceBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / btnColumns));
                }

                var btnSendText = new Button
                {
                    Text = "إرسال (نص تفصيلي)",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(37, 211, 102),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSendText.FlatAppearance.BorderSize = 0;

                var btnSendImage = new Button
                {
                    Text = "إرسال (صورة تفصيلية)",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(18, 140, 126),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSendImage.FlatAppearance.BorderSize = 0;

                Button btnSendPdf = null;
                if (pdfGenerator != null)
                {
                    btnSendPdf = new Button
                    {
                        Text = "إرسال (ملف PDF)",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(3),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(220, 38, 38),
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    btnSendPdf.FlatAppearance.BorderSize = 0;
                }

                pnlChoiceBtns.Controls.Add(btnSendText, 0, 0);
                pnlChoiceBtns.Controls.Add(btnSendImage, 1, 0);
                if (btnSendPdf != null) pnlChoiceBtns.Controls.Add(btnSendPdf, 2, 0);
                dlg.Controls.Add(pnlChoiceBtns);
                y += 60;

                // ── Preview Section with Tab switcher & Multi-Page Controls ──
                var pnlPreviewHeader = new Panel { Location = new Point(20, y), Size = new Size(720, 30), BackColor = Color.Transparent };
                var lblPreviewTitle = new Label { Text = "معاينة المحتوى:", Location = new Point(580, 4), Width = 135, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.TextMain };
                
                var btnShowText = new Button { Text = "معاينة النص", Location = new Point(455, 0), Size = new Size(115, 28), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), BackColor = Theme.Primary, ForeColor = Color.White, Cursor = Cursors.Hand };
                btnShowText.FlatAppearance.BorderSize = 0;

                var btnShowImg = new Button { Text = "معاينة الصورة", Location = new Point(330, 0), Size = new Size(120, 28), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Theme.TextMain, Cursor = Cursors.Hand };
                btnShowImg.FlatAppearance.BorderSize = 0;

                var pnlPageNav = new Panel { Location = new Point(0, 0), Size = new Size(250, 28), BackColor = Color.Transparent, Visible = false };
                var btnPrevPage = new Button { Text = "سابقة", Location = new Point(160, 0), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold), BackColor = Color.FromArgb(226, 232, 240), Cursor = Cursors.Hand };
                btnPrevPage.FlatAppearance.BorderSize = 0;
                var lblPageInd = new Label { Text = "صفحة 1 من 1", Location = new Point(70, 5), Size = new Size(85, 20), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Theme.Primary };
                var btnNextPage = new Button { Text = "تالية", Location = new Point(0, 0), Size = new Size(65, 28), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold), BackColor = Color.FromArgb(226, 232, 240), Cursor = Cursors.Hand };
                btnNextPage.FlatAppearance.BorderSize = 0;

                pnlPageNav.Controls.Add(btnPrevPage);
                pnlPageNav.Controls.Add(lblPageInd);
                pnlPageNav.Controls.Add(btnNextPage);

                pnlPreviewHeader.Controls.Add(lblPreviewTitle);
                pnlPreviewHeader.Controls.Add(btnShowText);
                pnlPreviewHeader.Controls.Add(btnShowImg);
                pnlPreviewHeader.Controls.Add(pnlPageNav);
                dlg.Controls.Add(pnlPreviewHeader);
                y += 34;

                // Content Box
                var pnlContent = new Panel { Location = new Point(20, y), Size = new Size(720, 380), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                var txtPreview = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.None,
                    Text = textMessage,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
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
                y += 388;

                System.Collections.Generic.List<Image> cachedImages = null;
                int currentImageIndex = 0;

                Action ensureImagesLoaded = () =>
                {
                    if (cachedImages == null)
                    {
                        cachedImages = new System.Collections.Generic.List<Image>();
                        try
                        {
                            if (multiImageGenerator != null)
                            {
                                var list = multiImageGenerator();
                                if (list != null && list.Count > 0)
                                {
                                    foreach (var b in list) if (b != null) cachedImages.Add(b);
                                }
                            }
                        }
                        catch { }

                        if (cachedImages.Count == 0 && imageGenerator != null)
                        {
                            try
                            {
                                var single = imageGenerator();
                                if (single != null) cachedImages.Add(single);
                            }
                            catch { }
                        }

                        if (cachedImages.Count == 0)
                        {
                            var textCard = ReceiptImageGenerator.GenerateTextCardImage(dialogTitle, textMessage);
                            if (textCard != null) cachedImages.Add(textCard);
                        }
                    }
                };

                Action updateImageDisplay = () =>
                {
                    ensureImagesLoaded();
                    if (cachedImages.Count > 0)
                    {
                        if (currentImageIndex < 0) currentImageIndex = 0;
                        if (currentImageIndex >= cachedImages.Count) currentImageIndex = cachedImages.Count - 1;

                        picPreview.Image = cachedImages[currentImageIndex];
                        lblPageInd.Text = $"صفحة {currentImageIndex + 1} من {cachedImages.Count}";
                        pnlPageNav.Visible = cachedImages.Count > 1;
                        btnPrevPage.Enabled = currentImageIndex > 0;
                        btnNextPage.Enabled = currentImageIndex < cachedImages.Count - 1;
                    }
                };

                btnPrevPage.Click += (s, e) => { if (currentImageIndex > 0) { currentImageIndex--; updateImageDisplay(); } };
                btnNextPage.Click += (s, e) => { if (cachedImages != null && currentImageIndex < cachedImages.Count - 1) { currentImageIndex++; updateImageDisplay(); } };

                Action showImagePreview = () =>
                {
                    btnShowImg.BackColor = Theme.Accent;
                    btnShowImg.ForeColor = Color.White;
                    btnShowText.BackColor = Color.FromArgb(226, 232, 240);
                    btnShowText.ForeColor = Theme.TextMain;

                    txtPreview.Visible = false;
                    picPreview.Visible = true;
                    updateImageDisplay();
                };

                Action showTextPreview = () =>
                {
                    btnShowText.BackColor = Theme.Primary;
                    btnShowText.ForeColor = Color.White;
                    btnShowImg.BackColor = Color.FromArgb(226, 232, 240);
                    btnShowImg.ForeColor = Theme.TextMain;

                    pnlPageNav.Visible = false;
                    picPreview.Visible = false;
                    txtPreview.Visible = true;
                };

                btnShowText.Click += (s, e) => showTextPreview();
                btnShowImg.Click += (s, e) => showImagePreview();

                if (imageGenerator != null || multiImageGenerator != null)
                {
                    showImagePreview();
                }

                // ── Footer Buttons (Copy & Cancel) ──
                var btnCopy = Theme.MakeButton("نسخ المعروض للحافظة", 520, y, 220, 38, Color.FromArgb(70, 80, 100));
                btnCopy.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnCopy.Click += (s, e) =>
                {
                    try
                    {
                        if (picPreview.Visible)
                        {
                            ensureImagesLoaded();
                            if (cachedImages != null && cachedImages.Count > currentImageIndex)
                            {
                                Clipboard.SetImage(cachedImages[currentImageIndex]);
                                MessageBox.Show($"تم نسخ [صورة الصفحة {currentImageIndex + 1}] إلى الحافظة بنجاح!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            Clipboard.SetText(txtPreview.Text);
                            MessageBox.Show("تم نسخ نص الرسالة إلى الحافظة بنجاح!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل النسخ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                dlg.Controls.Add(btnCopy);

                var btnClose = Theme.MakeButton("إغلاق", 20, y, 100, 38, Color.FromArgb(120, 130, 140));
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

                    ensureImagesLoaded();
                    if (cachedImages != null && cachedImages.Count > 0)
                    {
                        if (cachedImages.Count == 1)
                        {
                            SendImage(targetPhone, cachedImages[0], "إشعار إلكتروني");
                            dlg.Close();
                        }
                        else
                        {
                            // 2 or more images
                            try
                            {
                                Clipboard.SetImage(cachedImages[0]);
                                OpenWhatsAppChat(targetPhone);

                                var res = MessageBox.Show(
                                    $"تم نسخ [الصفحة الأولى] إلى الحافظة بنجاح!\n\n" +
                                    "تم فتح محادثة الواتساب للعميل:\n" +
                                    "1. اضغط (Ctrl + V) داخل شات الواتساب للصق الصفحة الأولى وإرسالها.\n" +
                                    "2. اضغط زر (موافق) هنا لنسخ [الصفحة الثانية] فوراً ولصقها أيضاً.\n\n" +
                                    "هل ترغب في نسخ الصفحة الثانية الآن؟",
                                    "إرسال الكشف التفصيلي (صفحة 1 من 2)",
                                    MessageBoxButtons.OKCancel,
                                    MessageBoxIcon.Information);

                                if (res == DialogResult.OK && cachedImages.Count > 1)
                                {
                                    Clipboard.SetImage(cachedImages[1]);
                                    MessageBox.Show("تم نسخ [الصفحة الثانية] للحافظة!\n\nاضغط الآن (Ctrl + V) داخل شات الواتساب للصقها.", "تم نسخ الصفحة الثانية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Error("WhatsAppSender.SendMultiImages", ex);
                            }
                            dlg.Close();
                        }
                    }
                    else
                    {
                        OpenWhatsApp(targetPhone, textMessage);
                        dlg.Close();
                    }
                };

                if (btnSendPdf != null)
                {
                    btnSendPdf.Click += (s, e) =>
                    {
                        string targetPhone = txtPhone.Text.Trim();
                        if (string.IsNullOrWhiteSpace(targetPhone))
                        {
                            MessageBox.Show("يرجى إدخال رقم هاتف العميل أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtPhone.Focus();
                            return;
                        }

                        try
                        {
                            string pdfPath = pdfGenerator();
                            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                            {
                                MessageBox.Show("تعذر إنشاء ملف الـ PDF!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            // Copy file to Windows Clipboard as FileDrop
                            var sc = new System.Collections.Specialized.StringCollection();
                            sc.Add(pdfPath);
                            Clipboard.SetFileDropList(sc);

                            // Open WhatsApp chat
                            OpenWhatsAppChat(targetPhone);

                            // Notify user and provide open option
                            var res = MessageBox.Show(
                                "تم إنشاء ملف الـ PDF ونسخه للحافظة بنجاح!\n\n" +
                                $"الملف: {Path.GetFileName(pdfPath)}\n\n" +
                                "تم فتح محادثة الواتساب للعميل.\n" +
                                "يمكنك الآن الضغط على (Ctrl + V) داخل شات الواتساب للصق وإرسال الملف فوراً.\n\n" +
                                "هل ترغب في فتح ملف الـ PDF الآن؟",
                                "تم تجهيز ملف الـ PDF للإرسال",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);

                            if (res == DialogResult.Yes)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
                                }
                                catch { }
                            }

                            dlg.Close();
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error("WhatsAppSender.btnSendPdf", ex);
                            MessageBox.Show("فشل تجهيز ملف PDF: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };
                }

                dlg.ShowDialog(parentForm);
            }
        }

        /// <summary>
        /// إرسال إيصال مرتجع المبيعات عبر الواتساب بنموذج الطارق المعتمد
        /// </summary>
        public static void SendReturnReceipt(Form parentForm, int returnID, string overridePhone = null)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT sr.ReturnID, sr.ReturnDate, sr.TotalAmount, sr.Notes,
                           ISNULL(sr.PaymentType, N'Cash') AS PaymentType,
                           ISNULL(s.SaleCode, N'مرتجع عام') AS SaleCode,
                           ISNULL(c.ClientName, N'عميل نقدي / عام') AS ClientName,
                           ISNULL(c.Phone, N'') AS ClientPhone
                    FROM SalesReturns sr
                    LEFT JOIN Sales s ON sr.SaleID = s.SaleID
                    LEFT JOIN Clients c ON sr.ClientID = c.ClientID
                    WHERE sr.ReturnID = @id", DbHelper.P("@id", returnID));

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("لم يتم العثور على بيانات المرتجع المطلوب!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var row = dt.Rows[0];
                string clientName = row["ClientName"]?.ToString() ?? "العميل الكريم";
                string phone = !string.IsNullOrWhiteSpace(overridePhone) ? overridePhone : (row["ClientPhone"]?.ToString() ?? "");
                decimal totalAmount = Convert.ToDecimal(row["TotalAmount"]);
                string saleCode = row["SaleCode"]?.ToString() ?? "مرتجع عام";
                string returnCode = "RET-" + returnID;
                string payTypeFormatted = FrmPrintReturn.FormatPaymentType(row["PaymentType"]?.ToString());

                // تجهيز نص رسالة المرتجع
                var sb = new StringBuilder();
                sb.AppendLine($"*إشعار مرتجع مبيعات*");
                sb.AppendLine($"*{AppConfig.CompanyName}*");
                if (!string.IsNullOrEmpty(AppConfig.CompanyPhone))
                    sb.AppendLine($"هاتف: {AppConfig.CompanyPhone}");
                sb.AppendLine("───────────────────");
                sb.AppendLine($"العميل: *{clientName}*");
                sb.AppendLine($"رقم المرتجع: *#{returnCode}*");
                sb.AppendLine($"الفاتورة الأصلية: *#{saleCode}*");
                sb.AppendLine($"التاريخ: {Convert.ToDateTime(row["ReturnDate"]):yyyy/MM/dd hh:mm tt}");
                sb.AppendLine($"طريقة رد القيمة: *{payTypeFormatted}*");
                sb.AppendLine("───────────────────");

                var dtItems = DbHelper.Query(@"
                    SELECT ISNULL(p.ProductName, N'صنف عام') AS ProductName, 
                           ISNULL(ri.UnitName, ISNULL(p.Unit, N'')) AS UnitName,
                           ri.Quantity, ri.UnitPrice, 
                           ISNULL(ri.TotalPrice, ri.Quantity * ri.UnitPrice) AS TotalPrice,
                           ISNULL(ri.IMEI, N'') AS IMEI
                    FROM ReturnItems ri
                    LEFT JOIN Products p ON ri.ProductID = p.ProductID
                    WHERE ri.ReturnID = @id", DbHelper.P("@id", returnID));

                if (dtItems.Rows.Count > 0)
                {
                    sb.AppendLine("*الأصناف المرتجعة:*");
                    foreach (DataRow ir in dtItems.Rows)
                    {
                        string pName = ir["ProductName"]?.ToString();
                        string imei = ir.Table.Columns.Contains("IMEI") && ir["IMEI"] != DBNull.Value ? ir["IMEI"].ToString().Trim() : "";
                        string imeiStr = !string.IsNullOrEmpty(imei) ? $" (سيريال: {imei})" : "";
                        string u = ir["UnitName"]?.ToString();
                        decimal q = Convert.ToDecimal(ir["Quantity"]);
                        decimal p = Convert.ToDecimal(ir["UnitPrice"]);
                        decimal t = Convert.ToDecimal(ir["TotalPrice"]);
                        string uStr = !string.IsNullOrEmpty(u) ? $" {u}" : "";
                        sb.AppendLine($"{pName}{imeiStr}: {q:0.##}{uStr} × {p:N2} = *{t:N2} ج*");
                    }
                    sb.AppendLine("───────────────────");
                }

                sb.AppendLine($"*إجمالي قيمة المرتجع:* *{totalAmount:N2} جنيه*");
                string notes = row["Notes"]?.ToString();
                if (!string.IsNullOrWhiteSpace(notes))
                    sb.AppendLine($"ملاحظات: {notes}");
                sb.AppendLine("شكراً لتعاملكم معنا!");

                ShowWhatsAppSendOptionsDialog(
                    parentForm,
                    phone,
                    sb.ToString(),
                    () => ReceiptImageGenerator.GenerateReturnReceiptImage(returnID, "الطارق"),
                    "إرسال إيصال المرتجع عبر الواتساب (نموذج الطارق)"
                );
            }
            catch (Exception ex)
            {
                AppLogger.Error("WhatsAppSender.SendReturnReceipt", ex);
                MessageBox.Show("فشل إرسال إيصال المرتجع: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// إرسال فاتورة المشتريات للمورد عبر الواتساب (نص تفصيلي + صورة إيصال احترافية)
        /// </summary>
        public static void SendPurchaseInvoice(Form parentForm, int purchaseID, string overridePhone = null)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT p.PurchaseID, p.PurchaseCode, p.PurchaseDate, p.PurchaseType, p.SupplierID, p.TotalAmount, p.Notes,
                           ISNULL(p.PaidAmount, 0) + ISNULL(p.VisaPaid, 0) AS PaidAmount,
                           CASE 
                               WHEN p.PurchaseType = 'Cash' THEN 0 
                               ELSE ISNULL(p.TotalAmount, 0) - (ISNULL(p.PaidAmount, 0) + ISNULL(p.VisaPaid, 0)) 
                           END AS RemainingAmount,
                           COALESCE(s.SupplierName, N'مورد عام') AS SupplierName,
                           ISNULL(s.Phone, N'') AS SupplierPhone,
                           COALESCE(w.WarehouseName, N'المخزن الرئيسي') AS WarehouseName,
                           COALESCE(sa.AccountName, N'الخزينة الرئيسية') AS SafeName
                    FROM Purchases p WITH (NOLOCK)
                    LEFT JOIN Suppliers s WITH (NOLOCK) ON p.SupplierID = s.SupplierID
                    LEFT JOIN Warehouses w WITH (NOLOCK) ON p.WarehouseID = w.WarehouseID
                    LEFT JOIN SafeAccounts sa WITH (NOLOCK) ON p.SafeAccountID = sa.AccountID
                    WHERE p.PurchaseID = @id", DbHelper.P("@id", purchaseID));

                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("لم يتم العثور على بيانات الفاتورة المحددة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var row = dt.Rows[0];
                string suppName = row["SupplierName"]?.ToString() ?? "المورد الكريم";
                string phone = !string.IsNullOrWhiteSpace(overridePhone) ? overridePhone : (row["SupplierPhone"]?.ToString() ?? "");

                if (string.IsNullOrWhiteSpace(phone))
                {
                    using (var inputDlg = new Form())
                    {
                        inputDlg.Text = "رقم واتساب المورد";
                        inputDlg.Size = new Size(380, 160);
                        inputDlg.StartPosition = FormStartPosition.CenterParent;
                        inputDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                        inputDlg.MaximizeBox = false;
                        inputDlg.MinimizeBox = false;
                        inputDlg.RightToLeft = RightToLeft.Yes;
                        inputDlg.RightToLeftLayout = true;
                        inputDlg.BackColor = Theme.BgMain;
                        inputDlg.Font = Theme.FontMain;

                        var lbl = new Label { Text = $"رقم موبايل/واتساب المورد ({suppName}):", Location = new Point(15, 15), AutoSize = true, ForeColor = Theme.TextMain };
                        var txt = new TextBox { Location = new Point(15, 40), Width = 330, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
                        var btn = Theme.MakeButton("متابعة الإرسال", 200, 75, 145, 32, Theme.Success);
                        btn.Click += (s, e) => { inputDlg.DialogResult = DialogResult.OK; inputDlg.Close(); };

                        inputDlg.Controls.AddRange(new Control[] { lbl, txt, btn });
                        if (inputDlg.ShowDialog(parentForm) == DialogResult.OK)
                        {
                            phone = txt.Text.Trim();
                        }
                    }
                }

                string shopName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة والتجارة العامة";
                string purCode = row["PurchaseCode"]?.ToString();
                if (string.IsNullOrWhiteSpace(purCode)) purCode = "#PUR-" + purchaseID;
                string purDate = Convert.ToDateTime(row["PurchaseDate"]).ToString("yyyy/MM/dd hh:mm tt");
                decimal totalAmount = Convert.ToDecimal(row["TotalAmount"]);
                decimal paidAmount = Convert.ToDecimal(row["PaidAmount"]);
                decimal remainingAmount = Convert.ToDecimal(row["RemainingAmount"]);
                string pType = (row["PurchaseType"]?.ToString() == "Cash") ? "نقدي (مسدد)" : "آجل (على الحساب)";
                string warehouse = row["WarehouseName"]?.ToString() ?? "المخزن الرئيسي";

                var dtItems = PurchaseDAL.GetItems(purchaseID);

                var sb = new StringBuilder();
                sb.AppendLine($"*فاتورة مشتريات بضاعة*");
                sb.AppendLine($"*{shopName}*");
                if (!string.IsNullOrEmpty(AppConfig.CompanyPhone))
                    sb.AppendLine($"هاتف: {AppConfig.CompanyPhone}");
                sb.AppendLine("───────────────────");
                sb.AppendLine($"المورد: *{suppName}*");
                sb.AppendLine($"رقم الفاتورة: *{purCode}*");
                sb.AppendLine($"التاريخ: {purDate}");
                sb.AppendLine($"طريقة السداد: *{pType}*");
                sb.AppendLine($"المستودع: {warehouse}");
                sb.AppendLine("───────────────────");

                if (dtItems != null && dtItems.Rows.Count > 0)
                {
                    sb.AppendLine("*الأصناف والكميات الواردة:*");
                    foreach (DataRow ir in dtItems.Rows)
                    {
                        string pName = ir["ProductName"]?.ToString() ?? "صنف";
                        string u = ir.Table.Columns.Contains("UnitName") ? (ir["UnitName"]?.ToString() ?? "") : "";
                        decimal q = ir["Quantity"] != DBNull.Value ? Convert.ToDecimal(ir["Quantity"]) : 0m;
                        decimal bonus = ir.Table.Columns.Contains("BonusQuantity") && ir["BonusQuantity"] != DBNull.Value ? Convert.ToDecimal(ir["BonusQuantity"]) : 0m;
                        decimal p = ir["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(ir["UnitPrice"]) : 0m;
                        decimal t = ir["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(ir["TotalPrice"]) : (q * p);
                        string uStr = !string.IsNullOrEmpty(u) ? $" {u}" : "";
                        string bonusStr = bonus > 0 ? $" (بونص: {bonus:0.##})" : "";
                        sb.AppendLine($"{pName}: {q:0.##}{uStr} × {p:N2} = *{t:N2} ج*{bonusStr}");
                        string imei = ir.Table.Columns.Contains("IMEI") ? (ir["IMEI"]?.ToString()?.Trim() ?? "") : "";
                        if (!string.IsNullOrWhiteSpace(imei))
                        {
                            sb.AppendLine($"   السيريال (IMEI): `{imei}`");
                        }
                    }
                    sb.AppendLine("───────────────────");
                }

                sb.AppendLine($"*إجمالي الفاتورة:* *{totalAmount:N2} جنيه*");
                sb.AppendLine($"*المدفوع / المسدد:* *{paidAmount:N2} جنيه*");
                if (remainingAmount > 0)
                    sb.AppendLine($"*المتبقي:* *{remainingAmount:N2} جنيه*");

                int suppId = row["SupplierID"] != DBNull.Value ? Convert.ToInt32(row["SupplierID"]) : 0;
                if (suppId > 0)
                {
                    try
                    {
                        decimal suppBal = SupplierDAL.GetBalance(suppId);
                        sb.AppendLine($"*رصيد الحساب الحالي طرفنا:* *{suppBal:N2} جنيه*");
                    }
                    catch { }
                }

                string notes = row["Notes"]?.ToString();
                if (!string.IsNullOrWhiteSpace(notes))
                    sb.AppendLine($"ملاحظات: {notes}");

                sb.AppendLine("شكراً لحسن تعاونكم المستمر معنا!");

                ShowWhatsAppSendOptionsDialog(
                    parentForm,
                    phone,
                    sb.ToString(),
                    () => ReceiptImageGenerator.GeneratePurchaseReceiptImage(purchaseID),
                    "إرسال فاتورة المشتريات عبر الواتساب"
                );
            }
            catch (Exception ex)
            {
                AppLogger.Error("WhatsAppSender.SendPurchaseInvoice", ex);
                MessageBox.Show("فشل إرسال فاتورة المشتريات عبر الواتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

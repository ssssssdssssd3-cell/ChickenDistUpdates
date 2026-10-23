using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ChickenDist.DAL;

namespace ChickenDist.Core
{
    /// <summary>
    /// فئة متخصصة لإدارة وتنسيق طباعة إيصالات كروت الضمان والاستلام
    /// وتوليد صور إيصالات عالية الدقة ومشاركتها عبر الواتساب.
    /// </summary>
    public static class WarrantyReceiptHelper
    {
        private static readonly StringFormat SfCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.DirectionRightToLeft
        };

        private static readonly StringFormat SfRight = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.DirectionRightToLeft
        };

        private static readonly StringFormat SfLeft = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.DirectionRightToLeft
        };

        private static string CleanEmoji(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(text, @"\p{Cs}|\p{So}|\p{Sk}|\p{Cn}", "");
        }

        /// <summary>
        /// قياس الارتفاع المطلوب أو رسم إيصال الضمان بالكامل بالتناسق المناسب
        /// </summary>
        public static float RenderWarrantyReceipt(Graphics g, WarrantyTicket ticket, float width, bool isMeasureOnly)
        {
            if (ticket == null) return 0f;

            float factor = width / 285f;
            if (factor < 0.8f) factor = 0.8f;

            float pad = 10f * factor;
            float contentWidth = width - (pad * 2f);
            float curY = 12f * factor;

            using (var fCompany = new Font("Segoe UI", 12.5f * factor, FontStyle.Bold))
            using (var fSubTitle = new Font("Segoe UI", 9.5f * factor, FontStyle.Bold))
            using (var fSection = new Font("Segoe UI", 9f * factor, FontStyle.Bold))
            using (var fBold = new Font("Segoe UI", 8.5f * factor, FontStyle.Bold))
            using (var fRegular = new Font("Segoe UI", 8.5f * factor, FontStyle.Regular))
            using (var fSerial = new Font("Consolas", 9.5f * factor, FontStyle.Bold))
            using (var fTerms = new Font("Segoe UI", 7.5f * factor, FontStyle.Regular))
            using (var fFooter = new Font("Segoe UI", 7.5f * factor, FontStyle.Italic))
            using (var penSolid = new Pen(Color.FromArgb(50, 50, 50), 1.2f * factor))
            using (var penDash = new Pen(Color.FromArgb(160, 160, 160), 1f * factor) { DashStyle = DashStyle.Dash })
            using (var brText = new SolidBrush(Color.Black))
            using (var brMuted = new SolidBrush(Color.FromArgb(70, 70, 70)))
            using (var brNavy = new SolidBrush(Color.FromArgb(20, 50, 120)))
            using (var brHighlight = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var sfWrapNear = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
            using (var sfWrapCenter = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                // ── 1. LOGO & COMPANY HEADER ──
                string company = CleanEmoji(AppConfig.CompanyName);
                if (string.IsNullOrWhiteSpace(company)) company = "مركز المبيعات والصيانة";

                if (!isMeasureOnly)
                {
                    g.DrawString(company, fCompany, brHighlight, new RectangleF(pad, curY, contentWidth, 24f * factor), sfWrapCenter);
                }
                curY += 26f * factor;

                // Subtitle Badge
                float badgeHeight = 22f * factor;
                if (!isMeasureOnly)
                {
                    using (var brBadge = new SolidBrush(Color.FromArgb(240, 244, 252)))
                    using (var penBadge = new Pen(Color.FromArgb(190, 205, 230), 1f))
                    {
                        var rectBadge = new RectangleF(pad + (15f * factor), curY, contentWidth - (30f * factor), badgeHeight);
                        g.FillRectangle(brBadge, rectBadge);
                        g.DrawRectangle(penBadge, rectBadge.X, rectBadge.Y, rectBadge.Width, rectBadge.Height);
                        g.DrawString("إيصال استلام جهاز (ضمان وصيانة)", fSubTitle, brNavy, rectBadge, SfCenter);
                    }
                }
                curY += badgeHeight + (6f * factor);

                // Phone & Address if available
                string phone = AppConfig.CompanyPhone;
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    if (!isMeasureOnly)
                    {
                        g.DrawString("هاتف: " + phone, fRegular, brMuted, new RectangleF(pad, curY, contentWidth, 16f * factor), SfCenter);
                    }
                    curY += 17f * factor;
                }

                string address = AppConfig.CompanyAddress;
                if (!string.IsNullOrWhiteSpace(address))
                {
                    if (!isMeasureOnly)
                    {
                        g.DrawString(address, fTerms, brMuted, new RectangleF(pad, curY, contentWidth, 15f * factor), SfCenter);
                    }
                    curY += 16f * factor;
                }

                // Top Separator
                if (!isMeasureOnly)
                {
                    g.DrawLine(penSolid, pad, curY, width - pad, curY);
                }
                curY += 7f * factor;

                // ── 2. RECEIPT CODE & DATE ──
                if (!isMeasureOnly)
                {
                    // Ticket Code on Right
                    g.DrawString("رقم الإيصال:", fBold, brText, new RectangleF(pad, curY, 80f * factor, 18f * factor), SfRight);
                    g.DrawString(ticket.TicketCode ?? "---", fSection, brNavy, new RectangleF(pad + (75f * factor), curY, 110f * factor, 18f * factor), SfRight);

                    // Status on Left
                    g.DrawString(ticket.Status ?? "مستلم بالمحل", fBold, brText, new RectangleF(pad, curY, contentWidth, 18f * factor), SfLeft);
                }
                curY += 20f * factor;

                if (!isMeasureOnly)
                {
                    g.DrawString("تاريخ الاستلام:", fRegular, brMuted, new RectangleF(pad, curY, 80f * factor, 16f * factor), SfRight);
                    string dtStr = ticket.ReceiveDate.ToString("yyyy-MM-dd  hh:mm tt");
                    g.DrawString(dtStr, fRegular, brText, new RectangleF(pad + (75f * factor), curY, contentWidth - (75f * factor), 16f * factor), SfRight);
                }
                curY += 19f * factor;

                if (!isMeasureOnly)
                {
                    g.DrawLine(penDash, pad, curY, width - pad, curY);
                }
                curY += 6f * factor;

                // ── 3. CUSTOMER DETAILS ──
                if (!isMeasureOnly)
                {
                    g.DrawString("👤 بيانات العميل:", fSection, brNavy, new RectangleF(pad, curY, contentWidth, 18f * factor), SfRight);
                }
                curY += 20f * factor;

                // Name
                if (!isMeasureOnly)
                {
                    g.DrawString("الاسم:", fBold, brText, new RectangleF(pad, curY, 45f * factor, 17f * factor), SfRight);
                    g.DrawString(ticket.CustomerName ?? "---", fBold, brText, new RectangleF(pad + (45f * factor), curY, contentWidth - (45f * factor), 17f * factor), SfRight);
                }
                curY += 18f * factor;

                // Phone
                if (!isMeasureOnly)
                {
                    g.DrawString("الهاتف:", fBold, brText, new RectangleF(pad, curY, 45f * factor, 17f * factor), SfRight);
                    g.DrawString(ticket.CustomerPhone ?? "---", fBold, brText, new RectangleF(pad + (45f * factor), curY, contentWidth - (45f * factor), 17f * factor), SfRight);
                }
                curY += 18f * factor;

                // Address (if present)
                if (!string.IsNullOrWhiteSpace(ticket.CustomerAddress))
                {
                    SizeF szAddr = g.MeasureString("العنوان: " + ticket.CustomerAddress, fRegular, (int)contentWidth, sfWrapNear);
                    float h = Math.Max(szAddr.Height, 16f * factor);
                    if (!isMeasureOnly)
                    {
                        g.DrawString("العنوان: " + ticket.CustomerAddress, fRegular, brText, new RectangleF(pad, curY, contentWidth, h), sfWrapNear);
                    }
                    curY += h + (3f * factor);
                }

                if (!isMeasureOnly)
                {
                    g.DrawLine(penDash, pad, curY, width - pad, curY);
                }
                curY += 6f * factor;

                // ── 4. DEVICE DETAILS ──
                if (!isMeasureOnly)
                {
                    g.DrawString("📦 بيانات الجهاز المسلّم:", fSection, brNavy, new RectangleF(pad, curY, contentWidth, 18f * factor), SfRight);
                }
                curY += 20f * factor;

                // Product Name & Brand
                string devName = ticket.ProductName ?? "";
                if (!string.IsNullOrWhiteSpace(ticket.Brand)) devName += " (" + ticket.Brand + ")";
                SizeF szDev = g.MeasureString("الجهاز: " + devName, fBold, (int)contentWidth, sfWrapNear);
                float hDev = Math.Max(szDev.Height, 18f * factor);
                if (!isMeasureOnly)
                {
                    g.DrawString("الجهاز: " + devName, fBold, brText, new RectangleF(pad, curY, contentWidth, hDev), sfWrapNear);
                }
                curY += hDev + (3f * factor);

                // Serial / IMEI Box
                if (!string.IsNullOrWhiteSpace(ticket.SerialNumber))
                {
                    float boxH = 22f * factor;
                    if (!isMeasureOnly)
                    {
                        using (var brSn = new SolidBrush(Color.FromArgb(245, 248, 255)))
                        using (var penSn = new Pen(Color.FromArgb(180, 200, 240), 1f))
                        {
                            var rSn = new RectangleF(pad, curY, contentWidth, boxH);
                            g.FillRectangle(brSn, rSn);
                            g.DrawRectangle(penSn, rSn.X, rSn.Y, rSn.Width, rSn.Height);

                            g.DrawString("IMEI / سيريال:", fBold, brNavy, new RectangleF(pad + (5f * factor), curY + (2f * factor), 90f * factor, boxH), SfRight);
                            g.DrawString(ticket.SerialNumber, fSerial, brNavy, new RectangleF(pad + (95f * factor), curY + (2f * factor), contentWidth - (100f * factor), boxH), SfRight);
                        }
                    }
                    curY += boxH + (5f * factor);
                }

                // Invoice Number & Date
                if (!string.IsNullOrWhiteSpace(ticket.InvoiceNumber))
                {
                    string invText = "رقم الفاتورة: " + ticket.InvoiceNumber;
                    if (ticket.InvoiceDate.HasValue)
                    {
                        invText += "  (" + ticket.InvoiceDate.Value.ToString("yyyy-MM-dd") + ")";
                    }
                    if (!isMeasureOnly)
                    {
                        g.DrawString(invText, fRegular, brMuted, new RectangleF(pad, curY, contentWidth, 17f * factor), SfRight);
                    }
                    curY += 18f * factor;
                }

                if (!isMeasureOnly)
                {
                    g.DrawLine(penDash, pad, curY, width - pad, curY);
                }
                curY += 6f * factor;

                // ── 5. COMPLAINT & ACCESSORIES ──
                if (!isMeasureOnly)
                {
                    g.DrawString("🛠️ العطل وشكوى العميل:", fSection, brNavy, new RectangleF(pad, curY, contentWidth, 18f * factor), SfRight);
                }
                curY += 19f * factor;

                // Problem Description
                string problem = ticket.ProblemDescription ?? "فحص وصيانة عامة";
                SizeF szProb = g.MeasureString(problem, fBold, (int)contentWidth, sfWrapNear);
                float hProb = Math.Max(szProb.Height, 18f * factor);
                if (!isMeasureOnly)
                {
                    g.DrawString(problem, fBold, brHighlight, new RectangleF(pad, curY, contentWidth, hProb), sfWrapNear);
                }
                curY += hProb + (4f * factor);

                // Accessories
                string acc = string.IsNullOrWhiteSpace(ticket.Accessories) ? "الجهاز فقط بدون ملحقات" : ticket.Accessories;
                SizeF szAcc = g.MeasureString("المشتملات: " + acc, fRegular, (int)contentWidth, sfWrapNear);
                float hAcc = Math.Max(szAcc.Height, 16f * factor);
                if (!isMeasureOnly)
                {
                    g.DrawString("المشتملات: " + acc, fRegular, brText, new RectangleF(pad, curY, contentWidth, hAcc), sfWrapNear);
                }
                curY += hAcc + (3f * factor);

                // Device Condition
                string cond = string.IsNullOrWhiteSpace(ticket.DeviceCondition) ? "جيدة بدون كسور ظاهرة" : ticket.DeviceCondition;
                SizeF szCond = g.MeasureString("الحالة الظاهرية: " + cond, fRegular, (int)contentWidth, sfWrapNear);
                float hCond = Math.Max(szCond.Height, 16f * factor);
                if (!isMeasureOnly)
                {
                    g.DrawString("الحالة الظاهرية: " + cond, fRegular, brText, new RectangleF(pad, curY, contentWidth, hCond), sfWrapNear);
                }
                curY += hCond + (4f * factor);

                // Additional Notes (if any)
                if (!string.IsNullOrWhiteSpace(ticket.Notes))
                {
                    SizeF szNotes = g.MeasureString("ملاحظات: " + ticket.Notes, fRegular, (int)contentWidth, sfWrapNear);
                    float hNotes = Math.Max(szNotes.Height, 16f * factor);
                    if (!isMeasureOnly)
                    {
                        g.DrawString("ملاحظات: " + ticket.Notes, fRegular, brMuted, new RectangleF(pad, curY, contentWidth, hNotes), sfWrapNear);
                    }
                    curY += hNotes + (4f * factor);
                }

                if (!isMeasureOnly)
                {
                    g.DrawLine(penSolid, pad, curY, width - pad, curY);
                }
                curY += 7f * factor;

                // ── 6. TERMS & DISCLAIMER BOX ──
                string t1 = "• هذا الإيصال هو المستند الرسمي والوحيد لإثبات استلام الجهاز وتسليمه.";
                string t2 = "• يرجى فحص واستلام الجهاز خلال 30 يوماً من إشعار الجاهزية تجنباً لإسقاط المسؤولية.";
                string t3 = "• الضمان لا يشمل الكسر، السوائل، الحريق أو سوء الاستخدام وفقاً لتقرير التوكيل.";
                string fullTerms = t1 + "\n" + t2 + "\n" + t3;

                SizeF szTerms = g.MeasureString(fullTerms, fTerms, (int)(contentWidth - (10f * factor)), sfWrapNear);
                float termsBoxHeight = szTerms.Height + (18f * factor);

                if (!isMeasureOnly)
                {
                    using (var brBox = new SolidBrush(Color.FromArgb(250, 250, 250)))
                    using (var penBox = new Pen(Color.FromArgb(215, 215, 215), 1f))
                    {
                        var rectTerms = new RectangleF(pad, curY, contentWidth, termsBoxHeight);
                        g.FillRectangle(brBox, rectTerms);
                        g.DrawRectangle(penBox, rectTerms.X, rectTerms.Y, rectTerms.Width, rectTerms.Height);

                        g.DrawString("⚠️ تنبيهات وشروط الاستلام:", fBold, brText, new RectangleF(pad + (5f * factor), curY + (3f * factor), contentWidth - (10f * factor), 15f * factor), SfRight);
                        g.DrawString(fullTerms, fTerms, brMuted, new RectangleF(pad + (5f * factor), curY + (18f * factor), contentWidth - (10f * factor), szTerms.Height), sfWrapNear);
                    }
                }
                curY += termsBoxHeight + (9f * factor);

                // ── 7. SIGNATURES ROW ──
                if (!isMeasureOnly)
                {
                    float sigWidth = (contentWidth - (15f * factor)) / 2f;
                    g.DrawString("توقيع الفني / المستلم: ............", fTerms, brText, new RectangleF(pad, curY, sigWidth, 18f * factor), SfRight);
                    g.DrawString("توقيع العميل: ............", fTerms, brText, new RectangleF(pad + sigWidth + (15f * factor), curY, sigWidth, 18f * factor), SfRight);
                }
                curY += 24f * factor;

                // ── 8. BARCODE OF TICKET CODE ──
                if (!string.IsNullOrWhiteSpace(ticket.TicketCode))
                {
                    float barcodeW = Math.Min(180f * factor, contentWidth * 0.75f);
                    float barcodeH = 28f * factor;
                    float barcodeX = pad + ((contentWidth - barcodeW) / 2f);

                    if (!isMeasureOnly)
                    {
                        try
                        {
                            BarcodeEngine.DrawBarcode(g, ticket.TicketCode, "Code128", barcodeX, curY, barcodeW, barcodeH);
                        }
                        catch
                        {
                            BarcodeEngine.DrawCode128(g, ticket.TicketCode, barcodeX, curY, barcodeW, barcodeH);
                        }

                        g.DrawString("*" + ticket.TicketCode + "*", fBold, brText, new RectangleF(pad, curY + barcodeH + (2f * factor), contentWidth, 16f * factor), SfCenter);
                    }
                    curY += barcodeH + (20f * factor);
                }

                // ── 9. FOOTER ──
                if (!isMeasureOnly)
                {
                    g.DrawString("نسعد بخدمتكم دائماً - نشكركم لثقتكم بنا! 🙏", fFooter, brNavy, new RectangleF(pad, curY, contentWidth, 16f * factor), SfCenter);
                }
                curY += 17f * factor;

                if (!isMeasureOnly)
                {
                    string prnDate = "طُبع في: " + DateTime.Now.ToString("yyyy-MM-dd  HH:mm");
                    g.DrawString(prnDate, fTerms, brMuted, new RectangleF(pad, curY, contentWidth, 14f * factor), SfCenter);
                }
                curY += 22f * factor;
            }

            return curY;
        }

        /// <summary>
        /// توليد صورة عالية الدقة لإيصال كرت الضمان للاستخدام في الواتساب والحفظ
        /// </summary>
        public static Bitmap GenerateReceiptImage(WarrantyTicket ticket, int width = 560)
        {
            if (ticket == null) return null;

            try
            {
                float targetWidth = width > 300 ? width : 560;

                // 1. Measure required height
                float reqHeight;
                using (var measureBmp = new Bitmap(1, 1))
                using (var gMeasure = Graphics.FromImage(measureBmp))
                {
                    reqHeight = RenderWarrantyReceipt(gMeasure, ticket, targetWidth, isMeasureOnly: true);
                }

                int finalHeight = (int)Math.Ceiling(reqHeight) + 4;
                var bmp = new Bitmap((int)targetWidth, finalHeight);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // Subtle border around entire image
                    using (var penBorder = new Pen(Color.FromArgb(210, 220, 235), 2f))
                    {
                        g.DrawRectangle(penBorder, 1, 1, targetWidth - 3, finalHeight - 3);
                    }

                    RenderWarrantyReceipt(g, ticket, targetWidth, isMeasureOnly: false);
                }

                return bmp;
            }
            catch (Exception ex)
            {
                AppLogger.Error("WarrantyReceiptHelper.GenerateReceiptImage", ex);
                return null;
            }
        }

        /// <summary>
        /// تكوين نص رسالة الواتساب المنسقة الجاهزة للإرسال
        /// </summary>
        public static string BuildWhatsAppText(WarrantyTicket ticket)
        {
            if (ticket == null) return "";

            string company = AppConfig.CompanyName;
            if (string.IsNullOrWhiteSpace(company)) company = "مركز المبيعات والصيانة";

            var sb = new StringBuilder();
            sb.AppendLine($"🛡️ *إيصال استلام جهاز (ضمان وصيانة)*");
            sb.AppendLine($"🏢 *{company}*");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"📄 *رقم الإيصال:* {ticket.TicketCode}");
            sb.AppendLine($"📅 *تاريخ الاستلام:* {ticket.ReceiveDate:yyyy-MM-dd  hh:mm tt}");
            sb.AppendLine($"👤 *اسم العميل:* {ticket.CustomerName}");
            if (!string.IsNullOrWhiteSpace(ticket.CustomerPhone))
                sb.AppendLine($"📱 *الهاتف:* {ticket.CustomerPhone}");
            if (!string.IsNullOrWhiteSpace(ticket.CustomerAddress))
                sb.AppendLine($"📍 *العنوان:* {ticket.CustomerAddress}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"📦 *الجهاز / الصنف:* {ticket.ProductName} {(string.IsNullOrWhiteSpace(ticket.Brand) ? "" : "(" + ticket.Brand + ")")}");
            if (!string.IsNullOrWhiteSpace(ticket.SerialNumber))
                sb.AppendLine($"🔢 *السيريال / IMEI:* `{ticket.SerialNumber}`");
            if (!string.IsNullOrWhiteSpace(ticket.InvoiceNumber))
                sb.AppendLine($"🧾 *فاتورة الشراء:* {ticket.InvoiceNumber}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"🛠️ *العطل وشكوى العميل:*");
            sb.AppendLine($"   {ticket.ProblemDescription}");
            sb.AppendLine($"🔌 *المشتملات:* {(string.IsNullOrWhiteSpace(ticket.Accessories) ? "الجهاز فقط" : ticket.Accessories)}");
            sb.AppendLine($"🔍 *الحالة الظاهرية:* {(string.IsNullOrWhiteSpace(ticket.DeviceCondition) ? "جيدة" : ticket.DeviceCondition)}");
            sb.AppendLine($"⚡ *الحالة الحالية:* {ticket.Status}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("⚠️ *تنبيه هام:* يرجى الاحتفاظ بنسخة هذا الإيصال لإبرازها عند استلام الجهاز بعد انتهاء الصيانة أو فحص الضمان.");
            if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone))
                sb.AppendLine($"📞 *للاستفسار والمتابعة:* {AppConfig.CompanyPhone}");
            sb.AppendLine("\n🙏 *شكراً لتعاملكم معنا ونسعد بخدمتكم دائماً.*");

            return sb.ToString();
        }

        /// <summary>
        /// فتح شاشة إرسال الإيصال عبر الواتساب (رسالة نصية أو كرت صورة أو نسخ للحافظة)
        /// </summary>
        public static void SendWhatsApp(Form parentForm, WarrantyTicket ticket)
        {
            if (ticket == null) return;

            string phone = ticket.CustomerPhone?.Trim() ?? "";
            string msg = BuildWhatsAppText(ticket);

            try
            {
                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                    parentForm,
                    phone,
                    msg,
                    () => GenerateReceiptImage(ticket, 560),
                    "📱 إرسال إيصال استلام الضمان عبر الواتساب"
                );
            }
            catch (Exception ex)
            {
                AppLogger.Error("WarrantyReceiptHelper.SendWhatsApp", ex);
                MessageBox.Show("فشل إرسال الإيصال عبر الواتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// طباعة إيصال استلام الضمان على طابعة الريسيت أو الطابعة الافتراضية مع دعم المعاينة
        /// </summary>
        public static void PrintReceipt(WarrantyTicket ticket, bool showPreview = false, string printerName = null, int copies = 1)
        {
            if (ticket == null) return;

            try
            {
                string targetPrinter = !string.IsNullOrWhiteSpace(printerName) 
                    ? printerName 
                    : AppConfig.ReceiptPrinterName;

                var pd = new PrintDocument();
                pd.DocumentName = "WarrantyReceipt_" + (ticket.TicketCode ?? ticket.TicketID.ToString());

                if (!string.IsNullOrWhiteSpace(targetPrinter))
                {
                    AppConfig.SetPrinter(pd, targetPrinter);
                }

                // 80mm thermal receipt standard printable width (~285 points)
                const float receiptWidth = 285f;

                // Measure height for custom roll paper length if thermal
                float reqHeight;
                using (var bmp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bmp))
                {
                    reqHeight = RenderWarrantyReceipt(g, ticket, receiptWidth, isMeasureOnly: true);
                }

                int pageHeightPoints = (int)Math.Ceiling(reqHeight) + 20;

                try
                {
                    pd.DefaultPageSettings.PaperSize = new PaperSize("ReceiptRoll", (int)receiptWidth, pageHeightPoints);
                    pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                }
                catch { }

                pd.PrintPage += (s, ev) =>
                {
                    float printableWidth = ev.PageBounds.Width > 100 && ev.PageBounds.Width < 400 
                        ? ev.PageBounds.Width 
                        : receiptWidth;

                    ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    ev.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    RenderWarrantyReceipt(ev.Graphics, ticket, printableWidth, isMeasureOnly: false);
                    ev.HasMorePages = false;
                };

                if (showPreview)
                {
                    using (var dlg = new PrintPreviewDialog
                    {
                        Document = pd,
                        Width = 440,
                        Height = 680,
                        Text = "معاينة إيصال استلام الجهاز"
                    })
                    {
                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog();
                    }
                }
                else
                {
                    AppConfig.PrintInBackground(pd, copies: Math.Max(1, copies));
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("WarrantyReceiptHelper.PrintReceipt", ex);
                MessageBox.Show("حدث خطأ أثناء تشغيل أمر طباعة الإيصال: " + ex.Message, "خطأ في الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>طباعة وإرسال سند/إشعار التوريد والتحصيل من العميل تفصيلياً مع الرصيد النهائي والواتساب</summary>
    public class FrmPrintClientPayment
    {
        private int _clientID;
        private decimal _amount;
        private string _notes;
        private int? _safeAccountID;

        private string _clientName = "---";
        private string _clientPhone = "";
        private decimal _prevBalance = 0m;
        private decimal _currentBalance = 0m;
        private string _safeName = "---";
        private string _employeeName = "---";
        private DateTime _transDate = DateTime.Now;
        private string _voucherCode = "";

        public FrmPrintClientPayment(int clientID, decimal amount, string notes = "", int? safeAccountID = null, string clientName = null)
        {
            _clientID = clientID;
            _amount = amount;
            _notes = notes;
            _safeAccountID = safeAccountID;
            if (!string.IsNullOrEmpty(clientName)) _clientName = clientName;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Client Info & Current Balance
                DataRow cRow = ClientDAL.GetByID(_clientID);
                if (cRow != null)
                {
                    _clientName = cRow["ClientName"].ToString();
                    _clientPhone = cRow["Phone"]?.ToString() ?? "";
                }

                // Current balance after payment
                _currentBalance = ClientDAL.GetBalance(_clientID);
                // Previous balance before this payment
                _prevBalance = _currentBalance + _amount;

                // Safe name
                if (_safeAccountID.HasValue && _safeAccountID.Value > 0)
                {
                    var safeObj = DbHelper.Scalar("SELECT AccountName FROM SafeAccounts WHERE AccountID = @id", DbHelper.P("@id", _safeAccountID.Value));
                    if (safeObj != null && safeObj != DBNull.Value)
                        _safeName = safeObj.ToString();
                }

                _employeeName = Session.EmpName;

                // ── رقم السند التسلسلي من قاعدة البيانات ──
                try
                {
                    var maxId = DbHelper.Scalar(
                        "SELECT COALESCE(MAX(TransID), 0) FROM ClientTransactions WHERE TransType='Payment'");
                    long seq = (maxId != null && maxId != DBNull.Value) ? Convert.ToInt64(maxId) : 0;
                    if (seq == 0)
                    {
                        var cnt = DbHelper.Scalar("SELECT COUNT(*) FROM ClientTransactions WHERE TransType='Payment'");
                        seq = (cnt != null && cnt != DBNull.Value) ? Convert.ToInt64(cnt) : 1;
                    }
                    _voucherCode = seq.ToString();
                }
                catch
                {
                    _voucherCode = DateTime.Now.ToString("HHmmss");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadData in FrmPrintClientPayment failed", ex, "FrmPrintClientPayment");
            }
        }

        public void ShowOptionsDialog(IWin32Window owner = null)
        {
            using (var dlg = new FrmPaymentPrintDialog(
                "سند تحصيل نقدية / إشعار توريد عميل",
                _clientName,
                _amount,
                () => Print("Receipt"),
                () => Print("A4"),
                () => SendWhatsAppText(),
                () => SendWhatsAppImage()
            ))
            {
                dlg.ShowDialog(owner);
            }
        }

        public void Print(string format = "Receipt")
        {
            var pd = new PrintDocument();
            bool isReceipt = string.Equals(format, "Receipt", StringComparison.OrdinalIgnoreCase);

            if (isReceipt)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 300, 700);
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
            }
            else
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);
                pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            }

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                int pageW = e.PageBounds.Width;
                int left  = e.MarginBounds.Left;
                int right = e.MarginBounds.Right;
                int width = e.MarginBounds.Width;

                var fTitle  = new Font("Arial", isReceipt ? 12 : 16, FontStyle.Bold);
                var fHeader = new Font("Arial", isReceipt ? 9  : 11, FontStyle.Bold);
                var fBody   = new Font("Arial", isReceipt ? 8.5f : 10, FontStyle.Regular);
                var fBold   = new Font("Arial", isReceipt ? 9f  : 11, FontStyle.Bold);

                int y = 15;

                // Company Name
                string company = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                SizeF szComp = g.MeasureString(company, fTitle);
                g.DrawString(company, fTitle, Brushes.Black, (pageW - szComp.Width) / 2, y);
                y += (int)szComp.Height + 5;

                // Title
                string docTitle = "سند تحصيل نقدية / إشعار توريد";
                SizeF szTitle = g.MeasureString(docTitle, fHeader);
                g.DrawString(docTitle, fHeader, Brushes.Black, (pageW - szTitle.Width) / 2, y);
                y += (int)szTitle.Height + 10;

                g.DrawLine(Pens.Black, left, y, right, y);
                y += 8;

                // رقم السند + التاريخ
                g.DrawString($"رقم السند: {_voucherCode}", fBody, Brushes.Black,
                    right - g.MeasureString($"رقم السند: {_voucherCode}", fBody).Width, y);
                g.DrawString($"التاريخ: {_transDate:dd/MM/yyyy HH:mm}", fBody, Brushes.Black, left, y);
                y += 22;

                // اسم العميل + الهاتف
                g.DrawString($"اسم العميل: {_clientName}", fBold, Brushes.Black,
                    right - g.MeasureString($"اسم العميل: {_clientName}", fBold).Width, y);
                if (!string.IsNullOrEmpty(_clientPhone))
                    g.DrawString($"الهاتف: {_clientPhone}", fBody, Brushes.Black, left, y);
                y += 22;

                g.DrawLine(Pens.Gray, left, y, right, y);
                y += 8;

                // المبلغ
                string amountTafqeet = TafqeetHelper.ConvertToArabicWords(_amount);
                var fAmt = new Font("Arial", isReceipt ? 11 : 14, FontStyle.Bold);
                g.DrawString($"المبلغ المحصَّل: {_amount:N2} ج", fAmt, Brushes.DarkGreen,
                    right - g.MeasureString($"المبلغ المحصَّل: {_amount:N2} ج", fAmt).Width, y);
                y += 26;
                g.DrawString($"تفييد المبلغ: ({amountTafqeet})", fBody, Brushes.Black,
                    right - g.MeasureString($"تفييد المبلغ: ({amountTafqeet})", fBody).Width, y);
                y += 24;

                g.DrawLine(Pens.LightGray, left, y, right, y);
                y += 8;

                // الأرصدة
                g.DrawString($"الرصيد السابق قبل التوريد: {_prevBalance:N2} ج", fBody, Brushes.Black,
                    right - g.MeasureString($"الرصيد السابق قبل التوريد: {_prevBalance:N2} ج", fBody).Width, y);
                y += 22;

                var fBal = new Font("Arial", isReceipt ? 9.5f : 11.5f, FontStyle.Bold);
                g.DrawString($"الرصيد النهائي المتبقي للعميل: {_currentBalance:N2} ج", fBal, Brushes.DarkBlue,
                    right - g.MeasureString($"الرصيد النهائي المتبقي للعميل: {_currentBalance:N2} ج", fBal).Width, y);
                y += 25;

                g.DrawLine(Pens.Gray, left, y, right, y);
                y += 8;

                if (!string.IsNullOrWhiteSpace(_safeName) && _safeName != "---")
                {
                    g.DrawString($"الخزنة / الحساب: {_safeName}", fBody, Brushes.Black,
                        right - g.MeasureString($"الخزنة / الحساب: {_safeName}", fBody).Width, y);
                    y += 20;
                }

                g.DrawString($"المستلم (الموظف): {_employeeName}", fBody, Brushes.Black,
                    right - g.MeasureString($"المستلم (الموظف): {_employeeName}", fBody).Width, y);
                y += 20;

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    g.DrawString($"البيان / ملاحظات: {_notes}", fBody, Brushes.Black,
                        right - g.MeasureString($"البيان / ملاحظات: {_notes}", fBody).Width, y);
                    y += 22;
                }

                y += 15;
                g.DrawLine(Pens.Black, left, y, right, y);
                y += 12;

                g.DrawString("توقيع المحصّل: ....................", fBody, Brushes.Black, right - 180, y);
                g.DrawString("توقيع العميل: ....................",  fBody, Brushes.Black, left, y);
            };

            try { pd.Print(); }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SendWhatsAppText()
        {
            if (string.IsNullOrWhiteSpace(_clientPhone))
            {
                MessageBox.Show("عذراً، رقم هاتف العميل غير مسجّل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string amountTafqeet = TafqeetHelper.ConvertToArabicWords(_amount);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("🧾 *إشعار وسند تحصيل نقدية*");
            sb.AppendLine("===============================");
            sb.AppendLine($"👤 *اسم العميل:* {_clientName}");
            sb.AppendLine($"🔢 *رقم السند:* {_voucherCode}");
            sb.AppendLine($"📅 *التاريخ:* {_transDate:dd/MM/yyyy HH:mm}");
            sb.AppendLine("-------------------------------");
            sb.AppendLine($"💵 *المبلغ المحصَّل:* {_amount:N2} ج");
            sb.AppendLine($"💬 *تفييد المبلغ:* ({amountTafqeet})");
            sb.AppendLine("-------------------------------");
            sb.AppendLine($"📊 *الرصيد السابق قبل التوريد:* {_prevBalance:N2} ج");
            sb.AppendLine($"📌 *الرصيد النهائي المتبقي:* {_currentBalance:N2} ج");
            if (!string.IsNullOrWhiteSpace(_notes))
                sb.AppendLine($"📝 *البيان:* {_notes}");
            sb.AppendLine("===============================");
            sb.AppendLine("✨ *شكراً لتعاملكم معنا!*");

            WhatsAppSender.OpenWhatsApp(_clientPhone, sb.ToString());
        }

        public void SendWhatsAppImage()
        {
            if (string.IsNullOrWhiteSpace(_clientPhone))
            {
                MessageBox.Show("عذراً، رقم هاتف العميل غير مسجّل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (Bitmap bmp = DrawVoucherBitmap())
                    Clipboard.SetImage(bmp);

                MessageBox.Show("✅ تم تصميم إشعار التوريد ونسخ الصورة للحافظة بنجاح!\nسيتم فتح محادثة الواتساب للعميل الآن، فقط اضغط (Ctrl+V) للصق وإرسال الصورة.",
                    "تم النسخ للحافظة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                WhatsAppSender.OpenWhatsAppChat(_clientPhone);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تصميم صورة الإشعار: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  رسم صورة الإشعار (واتساب) – تصميم احترافي RTL
        // ══════════════════════════════════════════════════════════════
        private Bitmap DrawVoucherBitmap()
        {
            int bw = 560, bh = 620;
            var bmp = new Bitmap(bw, bh);

            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // ── خلفية بيضاء ناعمة ─────────────────────────────────
                g.FillRectangle(new SolidBrush(Color.FromArgb(248, 252, 250)), 0, 0, bw, bh);

                // ── هيدر أخضر متدرج ────────────────────────────────────
                var grad = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, bw, 95),
                    Color.FromArgb(16, 84, 64),
                    Color.FromArgb(22, 120, 90),
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                g.FillRectangle(grad, 0, 0, bw, 95);
                grad.Dispose();

                // ── إطار خارجي ────────────────────────────────────────
                g.DrawRectangle(new Pen(Color.FromArgb(16, 100, 75), 2.5f), 8, 8, bw - 16, bh - 16);

                // ── StringFormats ──────────────────────────────────────
                var sfC  = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfR  = new StringFormat { Alignment = StringAlignment.Far };   // محاذاة يمين
                var sfL  = new StringFormat { Alignment = StringAlignment.Near };  // محاذاة يسار

                // ── Fonts ──────────────────────────────────────────────
                var fComp   = new Font("Arial", 15, FontStyle.Bold);
                var fSub    = new Font("Arial", 10, FontStyle.Regular);
                var fLbl    = new Font("Arial", 10, FontStyle.Bold);
                var fVal    = new Font("Arial", 10, FontStyle.Regular);
                var fAmt    = new Font("Arial", 19, FontStyle.Bold);
                var fSmall  = new Font("Arial",  9, FontStyle.Regular);

                // ── اسم الشركة (وسط الهيدر) ───────────────────────────
                string company = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                g.DrawString(company, fComp, Brushes.White, new RectangleF(20, 10, bw - 40, 38), sfC);
                g.DrawString("إشعار وسند تحصيل نقدية", fSub, new SolidBrush(Color.FromArgb(200, 255, 220)),
                    new RectangleF(20, 52, bw - 40, 28), sfC);

                // ── خط فاصل تحت الهيدر ────────────────────────────────
                g.DrawLine(new Pen(Color.FromArgb(180, 215, 195)), 20, 96, bw - 20, 96);

                int y  = 110;
                int lx = 24;          // يسار
                int rx = bw - 24;     // يمين

                // ── اسم العميل ────────────────────────────────────────
                DrawPair(g, fLbl, fLbl, lx, rx, y, "العميل:", _clientName,
                    Color.FromArgb(70, 70, 70), Color.FromArgb(10, 80, 55));
                y += 30;

                // ── رقم السند والتاريخ ────────────────────────────────
                g.DrawString($"التاريخ: {_transDate:dd/MM/yyyy HH:mm}", fSmall,
                    new SolidBrush(Color.FromArgb(110, 110, 110)), lx, y);
                g.DrawString($"رقم السند: {_voucherCode}", fSmall,
                    new SolidBrush(Color.FromArgb(110, 110, 110)),
                    new RectangleF(lx, y, rx - lx, 20), sfR);
                y += 32;

                // ── خط فاصل ───────────────────────────────────────────
                g.DrawLine(new Pen(Color.FromArgb(210, 225, 215)), lx, y, rx, y);
                y += 14;

                // ── المبلغ (بارز) ──────────────────────────────────────
                g.FillRectangle(new SolidBrush(Color.FromArgb(232, 252, 242)), lx, y - 4, rx - lx, 50);
                g.DrawString($"المبلغ المحصَّل: {_amount:N2} ج", fAmt,
                    new SolidBrush(Color.FromArgb(10, 140, 85)),
                    new RectangleF(lx, y, rx - lx, 44), sfC);
                y += 52;

                // ── التفييد ───────────────────────────────────────────
                string tafqeet = TafqeetHelper.ConvertToArabicWords(_amount);
                g.DrawString($"({tafqeet})", fSmall, new SolidBrush(Color.FromArgb(90, 90, 90)),
                    new RectangleF(lx, y, rx - lx, 20), sfC);
                y += 32;

                // ── خط فاصل ───────────────────────────────────────────
                g.DrawLine(new Pen(Color.FromArgb(210, 225, 215)), lx, y, rx, y);
                y += 12;

                // ── الأرصدة ───────────────────────────────────────────
                DrawPair(g, fVal, fLbl, lx, rx, y,
                    "الرصيد السابق:", $"{_prevBalance:N2} ج",
                    Color.FromArgb(80, 80, 80), Color.FromArgb(70, 70, 70));
                y += 28;

                DrawPair(g, new Font("Arial", 11, FontStyle.Bold), fLbl, lx, rx, y,
                    "الرصيد المتبقي النهائي:", $"{_currentBalance:N2} ج",
                    Color.FromArgb(15, 60, 180), Color.FromArgb(15, 60, 180));
                y += 34;

                // ── خط فاصل ───────────────────────────────────────────
                g.DrawLine(new Pen(Color.FromArgb(210, 225, 215)), lx, y, rx, y);
                y += 12;

                // ── الخزنة والمستلم ───────────────────────────────────
                if (!string.IsNullOrWhiteSpace(_safeName) && _safeName != "---")
                {
                    DrawPair(g, fSmall, fSmall, lx, rx, y, "الخزنة:", _safeName,
                        Color.FromArgb(90,90,90), Color.FromArgb(90,90,90));
                    y += 24;
                }
                DrawPair(g, fSmall, fSmall, lx, rx, y, "المستلم:", _employeeName,
                    Color.FromArgb(90,90,90), Color.FromArgb(90,90,90));
                y += 24;

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    DrawPair(g, fSmall, fSmall, lx, rx, y, "البيان:", _notes,
                        Color.FromArgb(90,90,90), Color.FromArgb(90,90,90));
                    y += 24;
                }

                // ── شكر ختامي ─────────────────────────────────────────
                y += 10;
                g.DrawLine(new Pen(Color.FromArgb(16, 100, 75), 1.5f), lx, y, rx, y);
                y += 10;
                g.DrawString("✨ شكراً لتعاملكم معنا", fSmall,
                    new SolidBrush(Color.FromArgb(16, 100, 75)),
                    new RectangleF(lx, y, rx - lx, 20), sfC);
            }
            return bmp;
        }

        /// <summary>يرسم صف: التسمية على اليسار، القيمة على اليمين</summary>
        private void DrawPair(Graphics g, Font fVal, Font fLbl, int lx, int rx, int y,
            string label, string value, Color lblColor, Color valColor)
        {
            g.DrawString(label, fLbl, new SolidBrush(lblColor), lx, y);
            SizeF sz = g.MeasureString(value, fVal);
            g.DrawString(value, fVal, new SolidBrush(valColor), rx - sz.Width, y);
        }
    }
}

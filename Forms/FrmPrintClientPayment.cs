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
                // Previous balance before this payment (Payment reduced client balance, so prev = current + amount)
                _prevBalance = _currentBalance + _amount;

                // Safe name
                if (_safeAccountID.HasValue && _safeAccountID.Value > 0)
                {
                    var safeObj = DbHelper.Scalar("SELECT AccountName FROM SafeAccounts WHERE AccountID = @id", DbHelper.P("@id", _safeAccountID.Value));
                    if (safeObj != null && safeObj != DBNull.Value)
                    {
                        _safeName = safeObj.ToString();
                    }
                }

                _employeeName = Session.EmpName;
                _voucherCode = "PAY-C-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
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
                int left = e.MarginBounds.Left;
                int right = e.MarginBounds.Right;
                int width = e.MarginBounds.Width;

                var fTitle = new Font("Arial", isReceipt ? 12 : 16, FontStyle.Bold);
                var fHeader = new Font("Arial", isReceipt ? 9 : 11, FontStyle.Bold);
                var fBody = new Font("Arial", isReceipt ? 8.5f : 10, FontStyle.Regular);
                var fBold = new Font("Arial", isReceipt ? 9f : 11, FontStyle.Bold);

                int y = 15;

                // Company Name Header
                string company = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                SizeF szComp = g.MeasureString(company, fTitle);
                g.DrawString(company, fTitle, Brushes.Black, (pageW - szComp.Width) / 2, y);
                y += (int)szComp.Height + 5;

                // Title
                string docTitle = "🧾 سند تحصيل نقدية / إشعار توريد";
                SizeF szTitle = g.MeasureString(docTitle, fHeader);
                g.DrawString(docTitle, fHeader, Brushes.Black, (pageW - szTitle.Width) / 2, y);
                y += (int)szTitle.Height + 10;

                g.DrawLine(Pens.Black, left, y, right, y);
                y += 8;

                // Info block
                g.DrawString($"رقم السند: {_voucherCode}", fBody, Brushes.Black, right - g.MeasureString($"رقم السند: {_voucherCode}", fBody).Width, y);
                g.DrawString($"التاريخ: {_transDate:dd/MM/yyyy HH:mm}", fBody, Brushes.Black, left, y);
                y += 22;

                g.DrawString($"اسم العميل: {_clientName}", fBold, Brushes.Black, right - g.MeasureString($"اسم العميل: {_clientName}", fBold).Width, y);
                if (!string.IsNullOrEmpty(_clientPhone))
                {
                    g.DrawString($"الهاتف: {_clientPhone}", fBody, Brushes.Black, left, y);
                }
                y += 22;

                g.DrawLine(Pens.Gray, left, y, right, y);
                y += 8;

                // Financial details
                string amountTafqeet = TafqeetHelper.ConvertToArabicWords(_amount);
                g.DrawString($"المبلغ المحصَّل: {_amount:N2} ج", new Font("Arial", isReceipt ? 11 : 14, FontStyle.Bold), Brushes.DarkGreen, right - g.MeasureString($"المبلغ المحصَّل: {_amount:N2} ج", new Font("Arial", isReceipt ? 11 : 14, FontStyle.Bold)).Width, y);
                y += 26;

                g.DrawString($"تفييد المبلغ: ({amountTafqeet})", fBody, Brushes.Black, right - g.MeasureString($"تفييد المبلغ: ({amountTafqeet})", fBody).Width, y);
                y += 24;

                g.DrawLine(Pens.LightGray, left, y, right, y);
                y += 8;

                // Account Balances
                g.DrawString($"الرصيد السابق قبل التوريد: {_prevBalance:N2} ج", fBody, Brushes.Black, right - g.MeasureString($"الرصيد السابق قبل التوريد: {_prevBalance:N2} ج", fBody).Width, y);
                y += 22;

                g.DrawString($"الرصيد النهائي المتبقي للعميل: {_currentBalance:N2} ج", new Font("Arial", isReceipt ? 9.5f : 11.5f, FontStyle.Bold), Brushes.DarkBlue, right - g.MeasureString($"الرصيد النهائي المتبقي للعميل: {_currentBalance:N2} ج", new Font("Arial", isReceipt ? 9.5f : 11.5f, FontStyle.Bold)).Width, y);
                y += 25;

                g.DrawLine(Pens.Gray, left, y, right, y);
                y += 8;

                if (!string.IsNullOrWhiteSpace(_safeName) && _safeName != "---")
                {
                    g.DrawString($"الخزنة / الحساب: {_safeName}", fBody, Brushes.Black, right - g.MeasureString($"الخزنة / الحساب: {_safeName}", fBody).Width, y);
                    y += 20;
                }

                g.DrawString($"المستلم (الموظف): {_employeeName}", fBody, Brushes.Black, right - g.MeasureString($"المستلم (الموظف): {_employeeName}", fBody).Width, y);
                y += 20;

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    g.DrawString($"البيان / ملاحظات: {_notes}", fBody, Brushes.Black, right - g.MeasureString($"البيان / ملاحظات: {_notes}", fBody).Width, y);
                    y += 22;
                }

                y += 15;
                g.DrawLine(Pens.Black, left, y, right, y);
                y += 12;

                g.DrawString("توقيع المحصّل: ....................", fBody, Brushes.Black, right - 180, y);
                g.DrawString("توقيع العميل: ....................", fBody, Brushes.Black, left, y);
            };

            try
            {
                pd.Print();
            }
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
            {
                sb.AppendLine($"📝 *البيان:* {_notes}");
            }
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
                {
                    Clipboard.SetImage(bmp);
                }

                MessageBox.Show("✅ تم تصميم إشعار التوريد ونسخ الصورة للحافظة بنجاح!\nسيتم فتح محادثة الواتساب للعميل الآن، فقط اضغط (Ctrl+V) للصق وإرسال الصورة.",
                    "تم النسخ للحافظة", MessageBoxButtons.OK, MessageBoxIcon.Information);

                WhatsAppSender.OpenWhatsAppChat(_clientPhone);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تصميم صورة الإشعار: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Bitmap DrawVoucherBitmap()
        {
            int w = 450;
            int h = 550;
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                var fTitle = new Font("Arial", 14, FontStyle.Bold);
                var fHeader = new Font("Arial", 11, FontStyle.Bold);
                var fBody = new Font("Arial", 10, FontStyle.Regular);
                var fBold = new Font("Arial", 11, FontStyle.Bold);

                int y = 20;

                // Border
                g.DrawRectangle(new Pen(Color.FromArgb(5, 122, 85), 3), 10, 10, w - 20, h - 20);

                string company = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                SizeF szC = g.MeasureString(company, fTitle);
                g.DrawString(company, fTitle, Brushes.Black, (w - szC.Width) / 2, y);
                y += 30;

                string title = "🧾 إشعار وسند تحصيل نقدية";
                SizeF szT = g.MeasureString(title, fHeader);
                g.DrawString(title, fHeader, Brushes.DarkGreen, (w - szT.Width) / 2, y);
                y += 30;

                g.DrawLine(Pens.Gray, 20, y, w - 20, y);
                y += 15;

                g.DrawString($"العميل: {_clientName}", fBold, Brushes.Black, 30, y);
                y += 25;

                g.DrawString($"رقم السند: {_voucherCode}", fBody, Brushes.DimGray, 30, y);
                g.DrawString($"التاريخ: {_transDate:dd/MM/yyyy HH:mm}", fBody, Brushes.DimGray, 220, y);
                y += 30;

                g.DrawLine(Pens.LightGray, 20, y, w - 20, y);
                y += 15;

                g.DrawString($"المبلغ المحصَّل: {_amount:N2} ج", new Font("Arial", 15, FontStyle.Bold), Brushes.Green, 30, y);
                y += 30;

                string tafqeet = TafqeetHelper.ConvertToArabicWords(_amount);
                g.DrawString($"({tafqeet})", fBody, Brushes.DarkSlateGray, 30, y);
                y += 30;

                g.DrawLine(Pens.LightGray, 20, y, w - 20, y);
                y += 15;

                g.DrawString($"الرصيد السابق: {_prevBalance:N2} ج", fBody, Brushes.Black, 30, y);
                y += 25;
                g.DrawString($"الرصيد المتبقي النهائى: {_currentBalance:N2} ج", fBold, Brushes.DarkBlue, 30, y);
                y += 30;

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    g.DrawString($"البيان: {_notes}", fBody, Brushes.Black, 30, y);
                    y += 25;
                }

                g.DrawString($"المستلم: {_employeeName}", fBody, Brushes.DimGray, 30, y);
            }
            return bmp;
        }
    }
}

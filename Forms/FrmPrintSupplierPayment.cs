using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>طباعة وإرسال سند/إشعار صرف نقدية لمورد تفصيلياً مع رصيد المورد النهائي والواتساب</summary>
    public class FrmPrintSupplierPayment
    {
        private int _supplierID;
        private decimal _amount;
        private string _notes;
        private int? _safeAccountID;

        private string _supplierName = "---";
        private string _supplierPhone = "";
        private decimal _prevBalance = 0m;
        private decimal _currentBalance = 0m;
        private string _safeName = "---";
        private string _employeeName = "---";
        private DateTime _transDate = DateTime.Now;
        private string _voucherCode = "";

        public FrmPrintSupplierPayment(int supplierID, decimal amount, string notes = "", int? safeAccountID = null, string supplierName = null)
        {
            _supplierID = supplierID;
            _amount = amount;
            _notes = notes;
            _safeAccountID = safeAccountID;
            if (!string.IsNullOrEmpty(supplierName)) _supplierName = supplierName;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Supplier Info & Current Balance
                DataRow sRow = SupplierDAL.GetByID(_supplierID);
                if (sRow != null)
                {
                    _supplierName = sRow["SupplierName"]?.ToString() ?? _supplierName;
                    _supplierPhone = sRow.Table.Columns.Contains("Phone") && sRow["Phone"] != DBNull.Value ? sRow["Phone"].ToString() : "";
                }

                // Current balance after payment
                _currentBalance = SupplierDAL.GetBalance(_supplierID);
                // Previous balance before this payment (Payment reduced supplier liability, so prev = current + amount)
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
                _voucherCode = "PAY-S-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadData in FrmPrintSupplierPayment failed", ex, "FrmPrintSupplierPayment");
            }
        }

        public void ShowOptionsDialog(IWin32Window owner = null)
        {
            using (var dlg = new FrmPaymentPrintDialog(
                "سند صرف نقدية لمورد",
                _supplierName,
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

                var fTitle = new Font("Arial", isReceipt ? 12 : 16, FontStyle.Bold);
                var fHeader = new Font("Arial", isReceipt ? 9 : 11, FontStyle.Bold);
                var fBody = new Font("Arial", isReceipt ? 8.5f : 10, FontStyle.Regular);
                var fBold = new Font("Arial", isReceipt ? 9f : 11, FontStyle.Bold);

                int y = 15;

                // Company Header
                string company = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                SizeF szComp = g.MeasureString(company, fTitle);
                g.DrawString(company, fTitle, Brushes.Black, (pageW - szComp.Width) / 2, y);
                y += (int)szComp.Height + 5;

                // Title
                string docTitle = "💸 سند صرف نقدية لمورد";
                SizeF szTitle = g.MeasureString(docTitle, fHeader);
                g.DrawString(docTitle, fHeader, Brushes.Black, (pageW - szTitle.Width) / 2, y);
                y += (int)szTitle.Height + 10;

                g.DrawLine(Pens.Black, left, y, right, y);
                y += 8;

                // Info block
                g.DrawString($"رقم السند: {_voucherCode}", fBody, Brushes.Black, right - g.MeasureString($"رقم السند: {_voucherCode}", fBody).Width, y);
                g.DrawString($"التاريخ: {_transDate:dd/MM/yyyy HH:mm}", fBody, Brushes.Black, left, y);
                y += 22;

                g.DrawString($"اسم المورد: {_supplierName}", fBold, Brushes.Black, right - g.MeasureString($"اسم المورد: {_supplierName}", fBold).Width, y);
                if (!string.IsNullOrEmpty(_supplierPhone))
                {
                    g.DrawString($"الهاتف: {_supplierPhone}", fBody, Brushes.Black, left, y);
                }
                y += 22;

                g.DrawLine(Pens.Gray, left, y, right, y);
                y += 8;

                // Financial details
                string amountTafqeet = TafqeetHelper.ConvertToArabicWords(_amount);
                g.DrawString($"المبلغ المَصروف: {_amount:N2} ج", new Font("Arial", isReceipt ? 11 : 14, FontStyle.Bold), Brushes.DarkRed, right - g.MeasureString($"المبلغ المَصروف: {_amount:N2} ج", new Font("Arial", isReceipt ? 11 : 14, FontStyle.Bold)).Width, y);
                y += 26;

                g.DrawString($"تفييد المبلغ: ({amountTafqeet})", fBody, Brushes.Black, right - g.MeasureString($"تفييد المبلغ: ({amountTafqeet})", fBody).Width, y);
                y += 24;

                g.DrawLine(Pens.LightGray, left, y, right, y);
                y += 8;

                // Account Balances
                g.DrawString($"رصيد المورد السابق قبل الصرف: {_prevBalance:N2} ج", fBody, Brushes.Black, right - g.MeasureString($"رصيد المورد السابق قبل الصرف: {_prevBalance:N2} ج", fBody).Width, y);
                y += 22;

                g.DrawString($"رصيد المورد النهائي المتبقي: {_currentBalance:N2} ج", new Font("Arial", isReceipt ? 9.5f : 11.5f, FontStyle.Bold), Brushes.DarkBlue, right - g.MeasureString($"رصيد المورد النهائي المتبقي: {_currentBalance:N2} ج", new Font("Arial", isReceipt ? 9.5f : 11.5f, FontStyle.Bold)).Width, y);
                y += 25;

                g.DrawLine(Pens.Gray, left, y, right, y);
                y += 8;

                if (!string.IsNullOrWhiteSpace(_safeName) && _safeName != "---")
                {
                    g.DrawString($"الخزنة / الحساب: {_safeName}", fBody, Brushes.Black, right - g.MeasureString($"الخزنة / الحساب: {_safeName}", fBody).Width, y);
                    y += 20;
                }

                g.DrawString($"القائم بالصرف (الموظف): {_employeeName}", fBody, Brushes.Black, right - g.MeasureString($"القائم بالصرف (الموظف): {_employeeName}", fBody).Width, y);
                y += 20;

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    g.DrawString($"البيان / ملاحظات: {_notes}", fBody, Brushes.Black, right - g.MeasureString($"البيان / ملاحظات: {_notes}", fBody).Width, y);
                    y += 22;
                }

                y += 15;
                g.DrawLine(Pens.Black, left, y, right, y);
                y += 12;

                g.DrawString("توقيع المستلم (المورد): ....................", fBody, Brushes.Black, right - 220, y);
                g.DrawString("توقيع الصراف: ....................", fBody, Brushes.Black, left, y);
            };

            try
            {
                AppConfig.PrintInBackground(pd);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SendWhatsAppText()
        {
            if (string.IsNullOrWhiteSpace(_supplierPhone))
            {
                MessageBox.Show("عذراً، رقم هاتف المورد غير مسجّل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string amountTafqeet = TafqeetHelper.ConvertToArabicWords(_amount);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("💸 *إشعار وسند صرف نقدية لمورد*");
            sb.AppendLine("===============================");
            sb.AppendLine($"🤝 *اسم المورد:* {_supplierName}");
            sb.AppendLine($"🔢 *رقم السند:* {_voucherCode}");
            sb.AppendLine($"📅 *التاريخ:* {_transDate:dd/MM/yyyy HH:mm}");
            sb.AppendLine("-------------------------------");
            sb.AppendLine($"💵 *المبلغ المَصروف:* {_amount:N2} ج");
            sb.AppendLine($"💬 *تفييد المبلغ:* ({amountTafqeet})");
            sb.AppendLine("-------------------------------");
            sb.AppendLine($"📊 *الرصيد السابق قبل الصرف:* {_prevBalance:N2} ج");
            sb.AppendLine($"📌 *رصيدكم النهائي المتبقي:* {_currentBalance:N2} ج");
            if (!string.IsNullOrWhiteSpace(_notes))
            {
                sb.AppendLine($"📝 *البيان:* {_notes}");
            }
            sb.AppendLine("===============================");
            sb.AppendLine("✨ *شكراً لتعاملكم معنا!*");

            WhatsAppSender.OpenWhatsApp(_supplierPhone, sb.ToString());
        }

        public void SendWhatsAppImage()
        {
            if (string.IsNullOrWhiteSpace(_supplierPhone))
            {
                MessageBox.Show("عذراً، رقم هاتف المورد غير مسجّل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (Bitmap bmp = DrawVoucherBitmap())
                {
                    Clipboard.SetImage(bmp);
                }

                MessageBox.Show("✅ تم تصميم إشعار الصرف ونسخ الصورة للحافظة بنجاح!\nسيتم فتح محادثة الواتساب للمورد الآن، فقط اضغط (Ctrl+V) للصق وإرسال الصورة.",
                    "تم النسخ للحافظة", MessageBoxButtons.OK, MessageBoxIcon.Information);

                WhatsAppSender.OpenWhatsAppChat(_supplierPhone);
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
                g.DrawRectangle(new Pen(Color.FromArgb(120, 53, 15), 3), 10, 10, w - 20, h - 20);

                string company = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                SizeF szC = g.MeasureString(company, fTitle);
                g.DrawString(company, fTitle, Brushes.Black, (w - szC.Width) / 2, y);
                y += 30;

                string title = "💸 إشعار وسند صرف نقدية لمورد";
                SizeF szT = g.MeasureString(title, fHeader);
                g.DrawString(title, fHeader, Brushes.DarkRed, (w - szT.Width) / 2, y);
                y += 30;

                g.DrawLine(Pens.Gray, 20, y, w - 20, y);
                y += 15;

                g.DrawString($"المورد: {_supplierName}", fBold, Brushes.Black, 30, y);
                y += 25;

                g.DrawString($"رقم السند: {_voucherCode}", fBody, Brushes.DimGray, 30, y);
                g.DrawString($"التاريخ: {_transDate:dd/MM/yyyy HH:mm}", fBody, Brushes.DimGray, 220, y);
                y += 30;

                g.DrawLine(Pens.LightGray, 20, y, w - 20, y);
                y += 15;

                g.DrawString($"المبلغ المَصروف: {_amount:N2} ج", new Font("Arial", 15, FontStyle.Bold), Brushes.DarkRed, 30, y);
                y += 30;

                string tafqeet = TafqeetHelper.ConvertToArabicWords(_amount);
                g.DrawString($"({tafqeet})", fBody, Brushes.DarkSlateGray, 30, y);
                y += 30;

                g.DrawLine(Pens.LightGray, 20, y, w - 20, y);
                y += 15;

                g.DrawString($"الرصيد السابق قبل الصرف: {_prevBalance:N2} ج", fBody, Brushes.Black, 30, y);
                y += 25;
                g.DrawString($"الرصيد النهائي المتبقي للمورد: {_currentBalance:N2} ج", fBold, Brushes.DarkBlue, 30, y);
                y += 30;

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    g.DrawString($"البيان: {_notes}", fBody, Brushes.Black, 30, y);
                    y += 25;
                }

                g.DrawString($"القائم بالصرف: {_employeeName}", fBody, Brushes.DimGray, 30, y);
            }
            return bmp;
        }
    }
}

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// طباعة وإرسال إيصال سداد أقساط العقود (حراري / A5 / A4 / واتساب نص وصورة)
    /// مع إظهار قيمة السداد والمديونية المتبقية على العقد والمديونية الكلية
    /// </summary>
    public class FrmPrintInstallmentPayment
    {
        private int _contractID;
        private decimal _collectedAmount;
        private string _paymentMethod = "نقدي";
        private int _safeID = 0;
        private string _safeName = "---";
        private string _notes = "";
        private int? _installmentNo = null;
        private string _printFormat;
        private bool _showPreview;

        private int _customerID = 0;
        private string _customerName = "---";
        private string _customerPhone = "";
        private string _contractCode = "---";
        private decimal _contractAmount = 0m;
        private decimal _financedAmount = 0m;
        private decimal _prevContractBalance = 0m;
        private decimal _remainingBalance = 0m;
        private decimal _overallClientDebt = 0m;
        private DateTime? _nextDueDate = null;
        private decimal _nextDueAmount = 0m;
        private int? _nextInstallmentNo = null;
        private string _employeeName = "---";
        private DateTime _transDate = DateTime.Now;
        private string _voucherCode = "";

        public FrmPrintInstallmentPayment(int contractID, decimal collectedAmount, string paymentMethod = "نقدي", int safeID = 0, string notes = "", int? installmentNo = null, string format = null, bool showPreview = false)
        {
            _contractID = contractID;
            _collectedAmount = collectedAmount;
            _paymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "نقدي" : paymentMethod;
            _safeID = safeID;
            _notes = notes ?? "";
            _installmentNo = installmentNo;
            _printFormat = format ?? AppConfig.DefaultInvoiceFormat;
            if (string.IsNullOrEmpty(_printFormat))
                _printFormat = "Receipt";
            _showPreview = showPreview;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // 1. بيانات العقد والعميل
                var dtContract = DbHelper.Query(@"
                    SELECT ic.ContractID, ic.ContractCode, ic.CustomerID, c.ClientName AS CustomerName, c.Phone AS CustomerPhone,
                           ic.ContractAmount, ic.DownPayment, ic.FinancedAmount, ic.InstallmentCount, ic.InstallmentValue
                    FROM InstallmentContracts ic
                    JOIN Clients c ON ic.CustomerID = c.ClientID
                    WHERE ic.ContractID = @cid", DbHelper.P("@cid", _contractID));

                if (dtContract.Rows.Count > 0)
                {
                    var row = dtContract.Rows[0];
                    _contractCode = row["ContractCode"]?.ToString() ?? "---";
                    _customerID = row["CustomerID"] != DBNull.Value ? Convert.ToInt32(row["CustomerID"]) : 0;
                    _customerName = row["CustomerName"]?.ToString() ?? "---";
                    _customerPhone = row["CustomerPhone"]?.ToString() ?? "";
                    _contractAmount = row["ContractAmount"] != DBNull.Value ? Convert.ToDecimal(row["ContractAmount"]) : 0m;
                    _financedAmount = row["FinancedAmount"] != DBNull.Value ? Convert.ToDecimal(row["FinancedAmount"]) : 0m;
                }

                // 2. القسط القادم وتاريخ استحقاقه
                var dtNext = DbHelper.Query(@"
                    SELECT TOP 1 InstallmentNo, DueDate, RemainingAmount 
                    FROM InstallmentSchedules 
                    WHERE ContractID = @cid AND Status <> 'Paid' AND RemainingAmount > 0 
                    ORDER BY InstallmentNo", DbHelper.P("@cid", _contractID));

                if (dtNext.Rows.Count > 0)
                {
                    _nextDueDate = Convert.ToDateTime(dtNext.Rows[0]["DueDate"]);
                    _nextDueAmount = Convert.ToDecimal(dtNext.Rows[0]["RemainingAmount"]);
                    _nextInstallmentNo = Convert.ToInt32(dtNext.Rows[0]["InstallmentNo"]);
                }

                // 3. إجمالي المديونية المتبقية على العقد
                var remObj = DbHelper.Scalar(@"
                    SELECT COALESCE(SUM(RemainingAmount), 0) 
                    FROM InstallmentSchedules 
                    WHERE ContractID = @cid", DbHelper.P("@cid", _contractID));

                if (remObj != null && remObj != DBNull.Value)
                {
                    _remainingBalance = Convert.ToDecimal(remObj);
                }
                _prevContractBalance = _remainingBalance + _collectedAmount;

                // 4. إجمالي مديونية العميل العامة في المحل
                if (_customerID > 0)
                {
                    try { _overallClientDebt = ClientDAL.GetBalance(_customerID); } catch { }
                }

                // 5. اسم الخزينة
                if (_safeID > 0)
                {
                    var safeObj = DbHelper.Scalar("SELECT AccountName FROM SafeAccounts WHERE AccountID = @sid", DbHelper.P("@sid", _safeID));
                    if (safeObj != null && safeObj != DBNull.Value)
                    {
                        _safeName = safeObj.ToString();
                    }
                }

                _employeeName = Session.EmpName ?? "المدير العام";
                _voucherCode = $"{_contractCode}-{_transDate:ddHHmm}";
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadData in FrmPrintInstallmentPayment failed", ex, "FrmPrintInstallmentPayment");
            }
        }

        public void ShowOptionsDialog(IWin32Window owner = null)
        {
            string title = _installmentNo.HasValue 
                ? $"إيصال سداد قسط رقم ({_installmentNo.Value}) - العقد {_contractCode}"
                : $"إيصال سداد دفعة تقسيط - العقد {_contractCode}";

            using (var dlg = new FrmPaymentPrintDialog(
                title,
                _customerName,
                _collectedAmount,
                () => Print("Receipt"),
                () => Print("A5"),
                () => SendWhatsAppText(),
                () => SendWhatsAppImage()
            ))
            {
                dlg.ShowDialog(owner);
            }
        }

        public void Print(string format = null, bool showPreview = false)
        {
            string chosenFormat = format ?? _printFormat;
            bool isReceipt = string.Equals(chosenFormat, "Receipt", StringComparison.OrdinalIgnoreCase);

            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();

            if (isReceipt)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 300, 850);
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
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var fTitle = new Font("Arial", isReceipt ? 12 : 16, FontStyle.Bold);
                var fHeader = new Font("Arial", isReceipt ? 10 : 13, FontStyle.Bold);
                var fBody = new Font("Arial", isReceipt ? 8.5f : 10.5f, FontStyle.Regular);
                var fBold = new Font("Arial", isReceipt ? 9f : 11.5f, FontStyle.Bold);
                var fLargeBold = new Font("Arial", isReceipt ? 11f : 14f, FontStyle.Bold);
                var fSmall = new Font("Arial", isReceipt ? 8f : 9.5f, FontStyle.Regular);

                int pageW = e.PageBounds.Width;
                int lMargin = isReceipt ? 12 : 25;
                int rMargin = isReceipt ? 12 : 25;
                int printableW = pageW - lMargin - rMargin;
                int y = isReceipt ? 20 : 25;

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };
                var left = new StringFormat { Alignment = StringAlignment.Near };

                // Draw Logo
                DrawShopLogo(g, pageW, ref y, isReceipt);

                // Shop / Company Name
                string compName = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                g.DrawString(compName, fTitle, Brushes.Black, new RectangleF(lMargin, y, printableW, 24), center);
                y += 24;

                string docTitle = _installmentNo.HasValue
                    ? $"إيصال سداد قسط تقسيط (قسط رقم {_installmentNo.Value})"
                    : "إيصال سداد وتحصيل دفعة تقسيط";
                g.DrawString(docTitle, fHeader, Brushes.DarkGreen, new RectangleF(lMargin, y, printableW, 22), center);
                y += 24;

                g.DrawLine(new Pen(Color.Black, 1.5f), lMargin, y, pageW - rMargin, y);
                y += 8;

                // Receipt Header Info (RTL)
                g.DrawString($"التاريخ: {_transDate:dd/MM/yyyy hh:mm tt}", fBody, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), left);
                g.DrawString($"رقم السند: {_voucherCode}", fBody, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                y += 20;

                g.DrawString($"رقم العقد: {_contractCode}", fBold, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), right);
                y += 20;

                g.DrawString($"العميل: {_customerName}", fBold, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), right);
                if (!string.IsNullOrWhiteSpace(_customerPhone))
                {
                    g.DrawString($"الهاتف: {_customerPhone}", fBody, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), left);
                }
                y += 22;

                g.DrawLine(Pens.LightGray, lMargin, y, pageW - rMargin, y);
                y += 8;

                // ── المبلغ المسدد البارز (Paid Amount) ──
                g.FillRectangle(new SolidBrush(Color.FromArgb(235, 248, 240)), lMargin, y, printableW, isReceipt ? 36 : 42);
                g.DrawRectangle(new Pen(Color.FromArgb(16, 140, 85)), lMargin, y, printableW, isReceipt ? 36 : 42);
                g.DrawString($"المبلغ المسدد: {_collectedAmount:N2} ج.م", fLargeBold, Brushes.DarkGreen, new RectangleF(lMargin + 6, y + (isReceipt ? 7 : 9), printableW - 12, 24), right);
                y += isReceipt ? 42 : 48;

                string tafqeet = TafqeetHelper.ConvertToArabicWords(_collectedAmount);
                g.DrawString($"فقط: ({tafqeet})", fSmall, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                y += 20;

                string payMethodText = _paymentMethod;
                if (!string.IsNullOrEmpty(_safeName) && _safeName != "---")
                    payMethodText += $" ({_safeName})";
                
                g.DrawString($"طريقة السداد: {payMethodText}", fBody, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                g.DrawString($"المستلم: {_employeeName}", fBody, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), left);
                y += 20;

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    g.DrawString($"ملاحظات: {_notes}", fSmall, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 28), right);
                    y += 22;
                }

                g.DrawLine(Pens.Gray, lMargin, y, pageW - rMargin, y);
                y += 8;

                // ── الأرصدة والمديونية المتبقية (Remaining Debt) ──
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 245, 255)), lMargin, y, printableW, isReceipt ? 32 : 36);
                g.DrawRectangle(new Pen(Color.FromArgb(30, 80, 180)), lMargin, y, printableW, isReceipt ? 32 : 36);
                g.DrawString($"المديونية المتبقية على العقد: {_remainingBalance:N2} ج.م", fBold, Brushes.DarkBlue, new RectangleF(lMargin + 6, y + (isReceipt ? 6 : 8), printableW - 12, 22), right);
                y += isReceipt ? 38 : 42;

                string nextDueText = _nextDueDate.HasValue 
                    ? $"{_nextDueDate.Value:yyyy-MM-dd} (قيمة: {_nextDueAmount:N2} ج)"
                    : "لا يوجد (تم سداد كامل أقساط العقد بنجاح ✅)";
                
                g.DrawString($"استحقاق القسط القادم:", fBold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                y += 18;
                g.DrawString(nextDueText, fBold, _nextDueDate.HasValue ? Brushes.DarkRed : Brushes.DarkGreen, new RectangleF(lMargin, y, printableW, 18), right);
                y += 22;

                if (_overallClientDebt > 0 && Math.Abs(_overallClientDebt - _remainingBalance) > 0.05m)
                {
                    g.DrawString($"إجمالي مديونية العميل الشاملة: {_overallClientDebt:N2} ج.م", fSmall, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 18), right);
                    y += 18;
                }

                g.DrawLine(new Pen(Color.Black, 1.2f), lMargin, y, pageW - rMargin, y);
                y += 10;

                // Signatures & Footer
                g.DrawString("توقيع المستلم / المحصل: ....................", fSmall, Brushes.Black, new RectangleF(pageW - rMargin - 200, y, 200, 18), right);
                g.DrawString("توقيع العميل: ....................", fSmall, Brushes.Black, new RectangleF(lMargin, y, 180, 18), left);
                y += 24;

                g.DrawString("✨ شكراً لالتزامكم وحسن تعاملكم معنا", fBold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), center);
                y += 18;
                g.DrawString("نظام إدارة المبيعات والأقساط ProSoft ERP", fSmall, Brushes.Gray, new RectangleF(lMargin, y, printableW, 14), center);
            };

            if (showPreview || _showPreview)
            {
                var preview = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = isReceipt ? 450 : 700,
                    Height = 750,
                    Text = "معاينة إيصال سداد القسط"
                };
                preview.ShowDialog();
            }
            else
            {
                try { AppConfig.PrintInBackground(pd); }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ أثناء الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void SendWhatsAppText()
        {
            if (string.IsNullOrWhiteSpace(_customerPhone))
            {
                MessageBox.Show("عذراً، رقم هاتف العميل غير مسجّل في بطاقة العميل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string amountTafqeet = TafqeetHelper.ConvertToArabicWords(_collectedAmount);
            string compName = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
            string installmentTitle = _installmentNo.HasValue ? $"سداد القسط رقم ({_installmentNo.Value})" : "سداد دفعة على العقد";
            
            string nextDueText = _nextDueDate.HasValue 
                ? $"{_nextDueDate.Value:yyyy-MM-dd} (قيمة القسط: {_nextDueAmount:N2} ج.م)"
                : "تم سداد كامل أقساط العقد بنجاح ✅";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🧾 *إيصال سداد قسط تقسيط - {compName}*");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"👤 *العميل:* {_customerName}");
            sb.AppendLine($"📋 *رقم العقد:* {_contractCode}");
            sb.AppendLine($"🔢 *البيان:* {installmentTitle}");
            sb.AppendLine($"📅 *تاريخ التحصيل:* {_transDate:dd/MM/yyyy hh:mm tt}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"💵 *المبلغ المسدد:* {_collectedAmount:N2} ج.م");
            sb.AppendLine($"💬 *فقط:* ({amountTafqeet})");
            sb.AppendLine($"💳 *طريقة الدفع:* {_paymentMethod}" + (!string.IsNullOrEmpty(_safeName) && _safeName != "---" ? $" ({_safeName})" : ""));
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"📊 *المديونية المتبقية على العقد:* {_remainingBalance:N2} ج.م");
            sb.AppendLine($"🔔 *استحقاق القسط القادم:* {nextDueText}");
            if (_overallClientDebt > 0 && Math.Abs(_overallClientDebt - _remainingBalance) > 0.05m)
            {
                sb.AppendLine($"📌 *إجمالي مديونية الحساب الشاملة:* {_overallClientDebt:N2} ج.م");
            }
            if (!string.IsNullOrWhiteSpace(_notes))
            {
                sb.AppendLine($"📝 *ملاحظات:* {_notes}");
            }
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine("✨ *شكراً لالتزامكم وحسن تعاملكم معنا!*");

            WhatsAppSender.OpenWhatsApp(_customerPhone, sb.ToString());
        }

        public void SendWhatsAppImage()
        {
            if (string.IsNullOrWhiteSpace(_customerPhone))
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

                MessageBox.Show("✅ تم تصميم صورة إيصال سداد القسط ونسخها إلى الحافظة بنجاح!\nسيتم فتح محادثة الواتساب للعميل الآن، فقط اضغط (Ctrl + V) للصق وإرسال الصورة مباشرة.",
                    "تم النسخ للحافظة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                WhatsAppSender.OpenWhatsAppChat(_customerPhone);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تصميم صورة الإيصال: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Bitmap DrawVoucherBitmap()
        {
            int bw = 580, bh = 660;
            var bmp = new Bitmap(bw, bh);

            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Background
                g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), 0, 0, bw, bh);

                // Top Gradient Header
                using (var grad = new LinearGradientBrush(new Rectangle(0, 0, bw, 100), Color.FromArgb(16, 84, 64), Color.FromArgb(30, 130, 95), 45f))
                {
                    g.FillRectangle(grad, 0, 0, bw, 100);
                }

                string compName = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                var fComp = new Font("Arial", 14f, FontStyle.Bold);
                var fSub = new Font("Arial", 10.5f, FontStyle.Bold);
                var fLbl = new Font("Arial", 10f, FontStyle.Regular);
                var fVal = new Font("Arial", 10.5f, FontStyle.Bold);
                var fAmt = new Font("Arial", 16f, FontStyle.Bold);
                var fSmall = new Font("Arial", 9f, FontStyle.Regular);

                var sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfR = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

                g.DrawString(compName, fComp, Brushes.White, new RectangleF(10, 12, bw - 20, 30), sfC);
                string voucherTitle = _installmentNo.HasValue 
                    ? $"إيصال سداد قسط تقسيط (قسط رقم {_installmentNo.Value})"
                    : "إيصال سداد وتحصيل دفعة تقسيط";
                g.DrawString(voucherTitle, fSub, new SolidBrush(Color.FromArgb(255, 215, 100)), new RectangleF(10, 48, bw - 20, 25), sfC);
                g.DrawString("سند محاسبي معتمد", fSmall, new SolidBrush(Color.FromArgb(200, 235, 220)), new RectangleF(10, 74, bw - 20, 20), sfC);

                int y = 115;
                int lx = 25;
                int rx = bw - 25;

                // العميل ورقم العقد
                DrawPair(g, fVal, fLbl, lx, rx, y, "العميل:", _customerName, Color.FromArgb(80, 80, 80), Color.FromArgb(10, 80, 55));
                y += 28;

                DrawPair(g, fVal, fLbl, lx, rx, y, "رقم العقد:", _contractCode, Color.FromArgb(80, 80, 80), Color.Black);
                y += 28;

                g.DrawString($"التاريخ: {_transDate:dd/MM/yyyy hh:mm tt}", fSmall, new SolidBrush(Color.FromArgb(120, 120, 120)), lx, y);
                g.DrawString($"رقم السند: {_voucherCode}", fSmall, new SolidBrush(Color.FromArgb(120, 120, 120)), new RectangleF(lx, y, rx - lx, 20), sfR);
                y += 28;

                g.DrawLine(new Pen(Color.FromArgb(210, 225, 215)), lx, y, rx, y);
                y += 12;

                // المبلغ المسدد
                g.FillRectangle(new SolidBrush(Color.FromArgb(232, 252, 242)), lx, y, rx - lx, 54);
                g.DrawRectangle(new Pen(Color.FromArgb(20, 150, 90), 1.5f), lx, y, rx - lx, 54);
                g.DrawString($"المبلغ المسدد: {_collectedAmount:N2} ج.م", fAmt, new SolidBrush(Color.FromArgb(10, 130, 75)), new RectangleF(lx, y + 4, rx - lx, 46), sfC);
                y += 58;

                string tafqeet = TafqeetHelper.ConvertToArabicWords(_collectedAmount);
                g.DrawString($"فقط: ({tafqeet})", fSmall, new SolidBrush(Color.FromArgb(90, 90, 90)), new RectangleF(lx, y, rx - lx, 20), sfC);
                y += 28;

                g.DrawLine(new Pen(Color.FromArgb(210, 225, 215)), lx, y, rx, y);
                y += 12;

                // المديونية المتبقية
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 245, 255)), lx, y, rx - lx, 40);
                g.DrawRectangle(new Pen(Color.FromArgb(30, 80, 180)), lx, y, rx - lx, 40);
                DrawPair(g, new Font("Arial", 11.5f, FontStyle.Bold), fLbl, lx + 10, rx - 10, y + 8, "المديونية المتبقية على العقد:", $"{_remainingBalance:N2} ج.م", Color.FromArgb(30, 80, 180), Color.FromArgb(30, 80, 180));
                y += 48;

                string nextDueText = _nextDueDate.HasValue 
                    ? $"{_nextDueDate.Value:yyyy-MM-dd} (قيمة: {_nextDueAmount:N2} ج)"
                    : "تم سداد كامل أقساط العقد بنجاح ✅";
                DrawPair(g, fVal, fLbl, lx, rx, y, "استحقاق القسط التالي:", nextDueText, Color.FromArgb(70, 70, 70), _nextDueDate.HasValue ? Color.FromArgb(180, 30, 30) : Color.FromArgb(10, 130, 75));
                y += 28;

                string payMethodText = _paymentMethod + (!string.IsNullOrEmpty(_safeName) && _safeName != "---" ? $" ({_safeName})" : "");
                DrawPair(g, fSmall, fSmall, lx, rx, y, "طريقة الدفع:", payMethodText, Color.FromArgb(100, 100, 100), Color.FromArgb(80, 80, 80));
                y += 22;

                DrawPair(g, fSmall, fSmall, lx, rx, y, "المستلم:", _employeeName, Color.FromArgb(100, 100, 100), Color.FromArgb(80, 80, 80));
                y += 22;

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    DrawPair(g, fSmall, fSmall, lx, rx, y, "ملاحظات:", _notes, Color.FromArgb(100, 100, 100), Color.FromArgb(80, 80, 80));
                    y += 22;
                }

                y += 8;
                g.DrawLine(new Pen(Color.FromArgb(16, 100, 75), 1.5f), lx, y, rx, y);
                y += 10;
                g.DrawString("✨ شكراً لالتزامكم وحسن تعاملكم معنا", fSmall, new SolidBrush(Color.FromArgb(16, 100, 75)), new RectangleF(lx, y, rx - lx, 20), sfC);
            }
            return bmp;
        }

        private void DrawPair(Graphics g, Font fVal, Font fLbl, int lx, int rx, int y, string label, string value, Color lblColor, Color valColor)
        {
            g.DrawString(label, fLbl, new SolidBrush(lblColor), lx, y);
            SizeF sz = g.MeasureString(value, fVal);
            g.DrawString(value, fVal, new SolidBrush(valColor), rx - sz.Width, y);
        }

        private void DrawShopLogo(Graphics g, int pageW, ref int y, bool isReceipt)
        {
            if (!AppConfig.PrintShopLogo || string.IsNullOrEmpty(AppConfig.ShopLogoPath))
                return;

            try
            {
                if (System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                    {
                        int logoW = isReceipt ? 70 : 90;
                        int logoH = (int)((double)img.Height / img.Width * logoW);
                        int logoX = (pageW - logoW) / 2;
                        g.DrawImage(img, logoX, y, logoW, logoH);
                        y += logoH + 8;
                    }
                }
            }
            catch { }
        }
    }
}

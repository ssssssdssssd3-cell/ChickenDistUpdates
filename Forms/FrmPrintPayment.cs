using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// طباعة سندات التوريد والتحصيل وكشوف حركة النقدية بمختلف النماذج A4 / A5
    /// (منها نموذج الطارق بجدول الشبكة الكاملة والتفقيط بالعربية والواترمارك)
    /// </summary>
    public class FrmPrintPayment
    {
        private DataTable _dtTransactions;
        private string _accountName;
        private decimal _startBalance;
        private decimal _totalIn;
        private decimal _totalOut;
        private decimal _netBalance;
        private string _voucherTemplate;
        private bool _showPreview;
        private string _format;
        private int _currentRowIndex = 0;
        private int _pageNumber = 0;

        public FrmPrintPayment(DataTable dtTrans, string accountName, decimal startBalance, string template = null, bool showPreview = true, string format = null)
        {
            _dtTransactions = dtTrans;
            _accountName = accountName ?? "الخزينة الرئيسية";
            _startBalance = startBalance;
            _voucherTemplate = template ?? AppConfig.VoucherTemplate;
            if (string.IsNullOrEmpty(_voucherTemplate))
                _voucherTemplate = "AlTarekVoucher";
            _showPreview = showPreview;
            _format = ResolveFormat(format);
            if (string.IsNullOrEmpty(_format)) return; // Canceled

            CalculateTotals();
            DoPrint();
        }

        public FrmPrintPayment(int cashTransID, string template = null, bool showPreview = true, string format = null)
        {
            _voucherTemplate = template ?? AppConfig.VoucherTemplate;
            if (string.IsNullOrEmpty(_voucherTemplate))
                _voucherTemplate = "AlTarekVoucher";
            _showPreview = showPreview;
            _format = ResolveFormat(format);
            if (string.IsNullOrEmpty(_format)) return; // Canceled

            LoadSingleTrans(cashTransID);
            CalculateTotals();
            DoPrint();
        }

        private string ResolveFormat(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                if (string.Equals(format, "Receipt", StringComparison.OrdinalIgnoreCase)) return "Receipt";
                if (string.Equals(format, "A5", StringComparison.OrdinalIgnoreCase)) return "A5";
                if (string.Equals(format, "A4", StringComparison.OrdinalIgnoreCase)) return "A4";
                if (string.Equals(format, "Ask", StringComparison.OrdinalIgnoreCase))
                {
                    using (var dlg = new FrmPrintChoiceDialog("يرجى اختيار مقاس ونوع طباعة سند الصرف / التوريد:", allowPrep: false))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedChoice))
                            return dlg.SelectedChoice;
                        return null;
                    }
                }
            }

            string configFormat = AppConfig.VoucherPaperSize;
            if (string.Equals(configFormat, "Ask", StringComparison.OrdinalIgnoreCase))
            {
                using (var dlg = new FrmPrintChoiceDialog("يرجى اختيار مقاس ونوع طباعة سند الصرف / التوريد:", allowPrep: false))
                {
                    if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedChoice))
                        return dlg.SelectedChoice;
                    return null;
                }
            }

            if (string.Equals(configFormat, "A5", StringComparison.OrdinalIgnoreCase)) return "A5";
            if (string.Equals(configFormat, "A4", StringComparison.OrdinalIgnoreCase)) return "A4";
            return "Receipt"; // Default is Receipt
        }

        private void LoadSingleTrans(int transID)
        {
            _dtTransactions = DbHelper.Query(@"
                SELECT c.CashID AS TransID, c.TransDate, c.TransType, c.AmountIn, c.AmountOut, c.Notes, c.CreatedBy,
                       ISNULL(e.EmpName, N'---') AS CreatedByName,
                       ISNULL(acc.AccountName, N'الخزينة الرئيسية') AS AccountName
                FROM CashBox c
                LEFT JOIN Employees e ON c.CreatedBy = e.EmpID
                LEFT JOIN SafeAccounts acc ON c.AccountID = acc.AccountID
                WHERE c.CashID = @id", DbHelper.P("@id", transID));

            if (_dtTransactions.Rows.Count > 0)
            {
                _accountName = _dtTransactions.Rows[0]["AccountName"].ToString();
            }
            _startBalance = 0;
        }

        private void CalculateTotals()
        {
            _totalIn = 0;
            _totalOut = 0;
            if (_dtTransactions != null)
            {
                foreach (DataRow r in _dtTransactions.Rows)
                {
                    _totalIn += r.Table.Columns.Contains("AmountIn") && r["AmountIn"] != DBNull.Value ? Convert.ToDecimal(r["AmountIn"]) : 0m;
                    _totalOut += r.Table.Columns.Contains("AmountOut") && r["AmountOut"] != DBNull.Value ? Convert.ToDecimal(r["AmountOut"]) : 0m;
                }
            }
            _netBalance = _startBalance + _totalIn - _totalOut;
        }

        private void DoPrint()
        {
            if (string.Equals(_format, "Receipt", StringComparison.OrdinalIgnoreCase))
            {
                DoPrintReceipt();
            }
            else
            {
                DoPrintA4A5();
            }
        }

        private void DoPrintA4A5()
        {
            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            bool isA5 = string.Equals(_format, "A5", StringComparison.OrdinalIgnoreCase);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);

            pd.BeginPrint += (s, e) =>
            {
                _currentRowIndex = 0;
                _pageNumber = 0;
            };

            pd.PrintPage += (s, e) =>
            {
                _pageNumber++;
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int pageW = e.PageBounds.Width;
                int pageH = e.PageBounds.Height;
                int lMargin = 20;
                int rMargin = 20;
                int printableW = pageW - lMargin - rMargin;
                int y = 20;

                var boldTitle = new Font("Arial", 14, FontStyle.Bold);
                var boldMain = new Font("Arial", 10, FontStyle.Bold);
                var boldSmall = new Font("Arial", 8.5f, FontStyle.Bold);
                var normal = new Font("Arial", 8.5f, FontStyle.Regular);
                var smallFont = new Font("Arial", 8f, FontStyle.Regular);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap };
                var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };

                if (string.Equals(_voucherTemplate, "AlTarekVoucher", StringComparison.OrdinalIgnoreCase))
                {
                    // ════════════════════════════════════════════════════════════════════════
                    // NATIVE AL-TAREK HOME VOUCHER / STATEMENT LAYOUT (نموذج الطارق هوم)
                    // ════════════════════════════════════════════════════════════════════════

                    // 1. Watermark logo in center
                    DrawWatermarkLogo(g, pageW, pageH);

                    // 2. Top Header Block (Left Brand Logo, Right Company Info)
                    int headerTopY = y;
                    g.DrawString(AppConfig.CompanyName, boldTitle, Brushes.Black, new RectangleF(pageW - rMargin - 350, y, 350, 24), sfRight);
                    g.DrawString($"العنوان: {AppConfig.CompanyAddress}", normal, Brushes.DarkSlateGray, new RectangleF(pageW - rMargin - 350, y + 24, 350, 18), sfRight);
                    g.DrawString($"موبايل: {AppConfig.CompanyPhone}", normal, Brushes.DarkSlateGray, new RectangleF(pageW - rMargin - 350, y + 42, 350, 18), sfRight);

                    // Logo on Top Left
                    DrawLogoOnLeft(g, lMargin, y, 160, 60);

                    y += 68;

                    // 3. SubHeader Light Blue Banner
                    g.FillRectangle(new SolidBrush(Color.FromArgb(224, 242, 254)), lMargin, y, printableW, 30);
                    g.DrawRectangle(new Pen(Color.FromArgb(186, 230, 253), 1.2f), lMargin, y, printableW, 30);

                    g.DrawString($"كشف حركة ونقدية / سند توريد وتحصيل | {_accountName}", boldMain, new SolidBrush(Color.FromArgb(15, 23, 42)), new RectangleF(lMargin + 260, y, printableW - 270, 30), sfRight);
                    g.DrawString($"صفحة {_pageNumber}/1   {DateTime.Now:dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin + 10, y, 250, 30), sfLeft);

                    y += 38;

                    // 4. Grid Table Headers (RTL: Columns start from Far Right)
                    // Columns: [م] [العملية / البيان] [مدين] [دائن] [رصيد] [الخزنة] [نوع الحركة] [التاريخ] [ملاحظات] [المستخدم] [الوقت]
                    int[] colW = { 24, 100, 72, 72, 75, 70, 65, 65, 115, 65, 64 };
                    string[] colNames = { "م", "العملية", "مدين", "دائن", "رصيد", "الخزنة", "نوع الحركة", "التاريخ", "ملاحظات", "المستخدم", "الوقت" };

                    int xRight = lMargin + printableW;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), lMargin, y, printableW, 24);
                    g.DrawRectangle(Pens.Black, lMargin, y, printableW, 24);

                    int xCur = xRight;
                    for (int i = 0; i < colNames.Length; i++)
                    {
                        xCur -= colW[i];
                        g.DrawString(colNames[i], boldSmall, Brushes.Black, new RectangleF(xCur, y, colW[i], 24), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + 24);
                    }
                    y += 24;

                    // Row 1: Opening / Carried Over Balance Row if page 1
                    if (_pageNumber == 1 && _startBalance != 0)
                    {
                        g.DrawRectangle(Pens.Black, lMargin, y, printableW, 20);
                        xCur = xRight;

                        // col 0: م
                        xCur -= colW[0];
                        g.DrawString("1", smallFont, Brushes.Black, new RectangleF(xCur, y, colW[0], 20), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + 20);

                        // col 1: العملية
                        xCur -= colW[1];
                        g.DrawString("رصيد مرحل / سابق", boldSmall, Brushes.DarkBlue, new RectangleF(xCur, y, colW[1], 20), sfRight);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + 20);

                        string startDeb = _startBalance > 0 ? _startBalance.ToString("N2") : "";
                        string startCred = _startBalance < 0 ? Math.Abs(_startBalance).ToString("N2") : "";

                        // col 2: مدين
                        xCur -= colW[2];
                        DrawStringFit(g, startDeb, boldSmall, Brushes.DarkRed, new RectangleF(xCur, y, colW[2], 20), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + 20);

                        // col 3: دائن
                        xCur -= colW[3];
                        DrawStringFit(g, startCred, boldSmall, Brushes.DarkGreen, new RectangleF(xCur, y, colW[3], 20), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + 20);

                        // col 4: رصيد
                        xCur -= colW[4];
                        DrawStringFit(g, _startBalance.ToString("N2"), boldSmall, Brushes.Black, new RectangleF(xCur, y, colW[4], 20), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + 20);

                        for (int i = 5; i < colW.Length; i++)
                        {
                            xCur -= colW[i];
                            g.DrawLine(Pens.Black, xCur, y, xCur, y + 20);
                        }

                        y += 20;
                    }

                    // 5. Transaction Rows
                    decimal running = _startBalance;
                    int rowNo = (_startBalance != 0) ? 2 : 1;

                    while (_currentRowIndex < _dtTransactions.Rows.Count)
                    {
                        if (y + 40 > pageH - 80)
                        {
                            e.HasMorePages = true;
                            return;
                        }

                        DataRow r = _dtTransactions.Rows[_currentRowIndex];
                        decimal inAmt = r.Table.Columns.Contains("AmountIn") && r["AmountIn"] != DBNull.Value ? Convert.ToDecimal(r["AmountIn"]) : 0m;
                        decimal outAmt = r.Table.Columns.Contains("AmountOut") && r["AmountOut"] != DBNull.Value ? Convert.ToDecimal(r["AmountOut"]) : 0m;
                        running += (inAmt - outAmt);

                        DateTime dt = Convert.ToDateTime(r["TransDate"]);
                        string datePart = dt.ToString("yyyy/MM/dd");
                        string timePart = dt.ToString("hh:mm tt");
                        string tType = r["TransType"].ToString();
                        string notes = r["Notes"]?.ToString() ?? "";
                        string user = r.Table.Columns.Contains("CreatedByName") ? r["CreatedByName"].ToString() : "---";

                        int rowH = 22;
                        g.DrawRectangle(Pens.Black, lMargin, y, printableW, rowH);

                        xCur = xRight;
                        // col 0: م
                        xCur -= colW[0];
                        g.DrawString((rowNo++).ToString(), smallFont, Brushes.Black, new RectangleF(xCur, y, colW[0], rowH), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 1: العملية
                        xCur -= colW[1];
                        g.DrawString(GetTransTypeName(tType), smallFont, Brushes.Black, new RectangleF(xCur + 2, y, colW[1] - 4, rowH), sfRight);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 2: مدين
                        xCur -= colW[2];
                        DrawStringFit(g, inAmt > 0 ? inAmt.ToString("N2") : "", boldSmall, Brushes.DarkRed, new RectangleF(xCur, y, colW[2], rowH), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 3: دائن
                        xCur -= colW[3];
                        DrawStringFit(g, outAmt > 0 ? outAmt.ToString("N2") : "", boldSmall, Brushes.DarkGreen, new RectangleF(xCur, y, colW[3], rowH), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 4: رصيد
                        xCur -= colW[4];
                        DrawStringFit(g, running.ToString("N2"), boldSmall, new SolidBrush(Color.FromArgb(15, 23, 42)), new RectangleF(xCur, y, colW[4], rowH), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 5: الخزنة
                        xCur -= colW[5];
                        g.DrawString(_accountName, smallFont, Brushes.Black, new RectangleF(xCur + 2, y, colW[5] - 4, rowH), sfRight);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 6: نوع الحركة
                        xCur -= colW[6];
                        g.DrawString(GetTransTypeArabicShort(tType), smallFont, Brushes.Black, new RectangleF(xCur, y, colW[6], rowH), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 7: التاريخ
                        xCur -= colW[7];
                        g.DrawString(datePart, smallFont, Brushes.Black, new RectangleF(xCur, y, colW[7], rowH), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 8: ملاحظات
                        xCur -= colW[8];
                        g.DrawString(notes, smallFont, Brushes.Black, new RectangleF(xCur + 2, y, colW[8] - 4, rowH), sfRight);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 9: المستخدم
                        xCur -= colW[9];
                        g.DrawString(user, smallFont, Brushes.Black, new RectangleF(xCur, y, colW[9], rowH), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        // col 10: الوقت
                        xCur -= colW[10];
                        g.DrawString(timePart, smallFont, Brushes.Black, new RectangleF(xCur, y, colW[10], rowH), sfCenter);
                        g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);

                        y += rowH;
                        _currentRowIndex++;
                    }

                    e.HasMorePages = false;

                    // 6. Summary Totals Row (Highlighted Background)
                    int summaryH = 28;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(254, 226, 226)), lMargin, y, printableW, summaryH);
                    g.DrawRectangle(new Pen(Color.Red, 1.2f), lMargin, y, printableW, summaryH);

                    xCur = xRight;
                    // col 0 + col 1 (إجمالي الحركة)
                    xCur -= (colW[0] + colW[1]);
                    g.DrawString("إجمالي الحركة", boldMain, Brushes.DarkRed, new RectangleF(xCur, y, colW[0] + colW[1], summaryH), sfCenter);
                    g.DrawLine(Pens.Red, xCur, y, xCur, y + summaryH);

                    // col 2 (إجمالي المقبوضات/مدين)
                    xCur -= colW[2];
                    DrawStringFit(g, _totalIn.ToString("N2"), boldMain, Brushes.DarkRed, new RectangleF(xCur, y, colW[2], summaryH), sfCenter);
                    g.DrawLine(Pens.Red, xCur, y, xCur, y + summaryH);

                    // col 3 (إجمالي المدفوعات/دائن)
                    xCur -= colW[3];
                    DrawStringFit(g, _totalOut.ToString("N2"), boldMain, Brushes.DarkGreen, new RectangleF(xCur, y, colW[3], summaryH), sfCenter);
                    g.DrawLine(Pens.Red, xCur, y, xCur, y + summaryH);

                    // col 4 (الصافي / رصيد)
                    xCur -= colW[4];
                    DrawStringFit(g, _netBalance.ToString("N2"), boldMain, Brushes.Black, new RectangleF(xCur, y, colW[4], summaryH), sfCenter);
                    g.DrawLine(Pens.Red, xCur, y, xCur, y + summaryH);

                    y += summaryH + 12;

                    // 7. Tafqeet Text (تفقيط المبلغ بالحروف العربية بالعريضة)
                    string tafqeetText = TafqeetHelper.ConvertToArabicWords(Math.Abs(_netBalance));
                    g.DrawString(tafqeetText, boldMain, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), sfCenter);

                    y += 35;

                    // 8. Signatures Block
                    g.DrawString("توقيع المستلم: ....................", boldSmall, Brushes.Black, lMargin + 10, y);
                    g.DrawString("توقيع المحاسب: ....................", boldSmall, Brushes.Black, lMargin + (printableW - 160) / 2, y);
                    g.DrawString("توقيع أمين الخزينة: ....................", boldSmall, Brushes.Black, lMargin + printableW - 190, y);

                    y += 35;

                    // 9. Bottom Footer Bar
                    g.DrawLine(new Pen(Color.Black, 2f), lMargin, pageH - 45, pageW - rMargin, pageH - 45);
                    g.DrawString($"العنوان: {AppConfig.CompanyAddress}   |   {AppConfig.CompanyPhone}", boldMain, Brushes.Black, new RectangleF(lMargin, pageH - 40, printableW, 20), sfCenter);
                }
                else
                {
                    // Official standard receipt template
                    g.DrawString(AppConfig.CompanyName, boldTitle, Brushes.Black, new RectangleF(0, y, pageW, 25), sfCenter); y += 25;
                    g.DrawString("سند قبض وتوريد نقدية", boldMain, Brushes.DarkBlue, new RectangleF(0, y, pageW, 20), sfCenter); y += 25;
                    g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); y += 15;

                    string tafqeetText = TafqeetHelper.ConvertToArabicWords(Math.Abs(_netBalance));
                    g.DrawString($"استلمنا من السيد/ة: {_accountName}", boldMain, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), sfRight); y += 25;
                    g.DrawString($"مبلغ وقدره: {_netBalance:N2} جنيه  ({tafqeetText})", boldMain, Brushes.DarkBlue, new RectangleF(lMargin, y, printableW, 22), sfRight); y += 30;

                    g.DrawString($"ذلك عن: حركات توريد وتحصيل بقيمة إجمالية {_totalIn:N2} ج", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), sfRight); y += 40;

                    g.DrawString("توقيع المستلم: ....................", normal, Brushes.Black, lMargin + 50, y);
                    g.DrawString("توقيع أمين الصندوق: ....................", normal, Brushes.Black, new RectangleF(0, y, pageW - rMargin - 50, 20), sfRight);
                }

            };

            if (_showPreview)
            {
                var preview = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 800,
                    Height = 700,
                    Text = "معاينة طباعة سند التوريد والتحصيل"
                };
                preview.ShowDialog();
            }
            else
            {
                AppConfig.PrintInBackground(pd);
            }
        }

        private void DoPrintReceipt()
        {
            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();

            int paperW = AppConfig.ReceiptPaperWidth == 58 ? 205 : 300;
            pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", paperW, 1000);
            int margin = paperW == 205 ? 6 : 10;
            pd.DefaultPageSettings.Margins = new Margins(margin, margin, 10, 10);
            AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int pageW = paperW;
                int lMargin = margin;
                int rMargin = margin;
                int printableW = pageW - lMargin - rMargin;
                int y = 10;

                bool is58 = (paperW == 205);
                var fontCompany = new Font("Arial", is58 ? 11f : 13f, FontStyle.Bold);
                var fontTitle = new Font("Arial", is58 ? 10f : 12f, FontStyle.Bold);
                var fontSection = new Font("Arial", is58 ? 8.5f : 9.5f, FontStyle.Bold);
                var fontRegular = new Font("Arial", is58 ? 7.5f : 8.5f, FontStyle.Regular);
                var fontBold = new Font("Arial", is58 ? 8f : 9f, FontStyle.Bold);
                var fontAmount = new Font("Arial", is58 ? 12.5f : 15f, FontStyle.Bold);
                var fontTafqeet = new Font("Arial", is58 ? 7.5f : 8.5f, FontStyle.Bold);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

                // 1. Logo (if enabled)
                if (AppConfig.PrintShopLogo && !string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    try
                    {
                        using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                        {
                            int maxLogoW = is58 ? 100 : 140;
                            int maxLogoH = 45;
                            double ratio = Math.Min((double)maxLogoW / img.Width, (double)maxLogoH / img.Height);
                            int lw = (int)(img.Width * ratio);
                            int lh = (int)(img.Height * ratio);
                            int lx = (pageW - lw) / 2;
                            g.DrawImage(img, lx, y, lw, lh);
                            y += lh + 6;
                        }
                    }
                    catch { }
                }

                // 2. Company Info
                string compName = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "مؤسسة التوزيع والتجارة" : AppConfig.CompanyName;
                g.DrawString(compName, fontCompany, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), sfCenter);
                y += 24;

                if (!string.IsNullOrWhiteSpace(AppConfig.CompanyAddress))
                {
                    g.DrawString(AppConfig.CompanyAddress, fontRegular, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, 16), sfCenter);
                    y += 18;
                }
                if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone))
                {
                    g.DrawString($"هاتف: {AppConfig.CompanyPhone}", fontRegular, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, 16), sfCenter);
                    y += 18;
                }

                y += 4;
                DrawDottedLine(g, lMargin, y, printableW);
                y += 8;

                // 3. Single Voucher vs Multi-Row
                if (_dtTransactions != null && _dtTransactions.Rows.Count == 1)
                {
                    DataRow row = _dtTransactions.Rows[0];
                    int transId = row.Table.Columns.Contains("TransID") && row["TransID"] != DBNull.Value ? Convert.ToInt32(row["TransID"]) : 0;
                    DateTime dt = row.Table.Columns.Contains("TransDate") && row["TransDate"] != DBNull.Value ? Convert.ToDateTime(row["TransDate"]) : DateTime.Now;
                    string tType = row["TransType"]?.ToString() ?? "";
                    decimal inAmt = row.Table.Columns.Contains("AmountIn") && row["AmountIn"] != DBNull.Value ? Convert.ToDecimal(row["AmountIn"]) : 0m;
                    decimal outAmt = row.Table.Columns.Contains("AmountOut") && row["AmountOut"] != DBNull.Value ? Convert.ToDecimal(row["AmountOut"]) : 0m;
                    decimal amount = inAmt > 0 ? inAmt : outAmt;
                    bool isDeposit = inAmt > 0 || tType == "Deposit" || tType == "ClientPayment" || tType == "SaleIncome";
                    string rawNotes = row["Notes"]?.ToString() ?? "";
                    string safeName = row.Table.Columns.Contains("AccountName") && row["AccountName"] != DBNull.Value ? row["AccountName"].ToString() : _accountName;
                    string userName = row.Table.Columns.Contains("CreatedByName") && row["CreatedByName"] != DBNull.Value ? row["CreatedByName"].ToString() : (Session.EmpName ?? "---");

                    // Extract party name, balance note, and clean notes
                    string partyName = "";
                    string balanceNote = "";
                    string cleanNotes = rawNotes;

                    if (cleanNotes.Contains("[") && cleanNotes.Contains("]"))
                    {
                        int startB = cleanNotes.IndexOf('[');
                        int closeB = cleanNotes.IndexOf(']');
                        if (closeB > startB)
                        {
                            partyName = cleanNotes.Substring(startB + 1, closeB - startB - 1).Trim();
                            cleanNotes = (cleanNotes.Substring(0, startB) + " " + cleanNotes.Substring(closeB + 1)).Trim();
                            if (partyName.StartsWith("عميل:")) partyName = partyName.Substring(5).Trim();
                            else if (partyName.StartsWith("مورد:")) partyName = partyName.Substring(5).Trim();
                        }
                    }

                    if (cleanNotes.Contains("(رصيد"))
                    {
                        int balIdx = cleanNotes.IndexOf("(رصيد");
                        int closeP = cleanNotes.IndexOf(')', balIdx);
                        if (closeP > balIdx)
                        {
                            balanceNote = cleanNotes.Substring(balIdx + 1, closeP - balIdx - 1).Trim();
                            cleanNotes = (cleanNotes.Substring(0, balIdx) + " " + cleanNotes.Substring(closeP + 1)).Trim();
                        }
                    }

                    // Clean prefixes
                    if (cleanNotes.StartsWith("سداد من عميل -")) cleanNotes = cleanNotes.Substring("سداد من عميل -".Length).Trim();
                    if (cleanNotes.StartsWith("سداد للمورد -")) cleanNotes = cleanNotes.Substring("سداد للمورد -".Length).Trim();
                    if (cleanNotes.StartsWith("توريد نقدية -")) cleanNotes = cleanNotes.Substring("توريد نقدية -".Length).Trim();
                    if (cleanNotes.StartsWith("صرف نقدية -")) cleanNotes = cleanNotes.Substring("صرف نقدية -".Length).Trim();
                    cleanNotes = cleanNotes.TrimStart('-', ':', ' ');

                    // Fallback party name if empty
                    if (string.IsNullOrEmpty(partyName))
                    {
                        if (tType == "Expense") partyName = "مصروفات عامة / تشغيل";
                        else if (tType == "ClientPayment") partyName = "عميل";
                        else if (tType == "SupplierPayment") partyName = "مورد";
                    }

                    // Determine Title & Theme Colors
                    string voucherTitle = isDeposit ? "سند توريد نقدي (قبض)" : "سند صرف نقدي (دفع)";
                    Color titleBg = isDeposit ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242);
                    Color titleBorder = isDeposit ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
                    Color amountColor = isDeposit ? Color.FromArgb(22, 101, 52) : Color.FromArgb(153, 27, 27);

                    if (tType.Contains("Transfer"))
                    {
                        voucherTitle = "سند تحويل نقدية";
                        titleBg = Color.FromArgb(245, 243, 255);
                        titleBorder = Color.FromArgb(147, 51, 234);
                        amountColor = Color.FromArgb(107, 33, 168);
                    }
                    else if (tType.Contains("Deficit") || tType.Contains("Surplus") || tType.Contains("Adjustment") || tType.Contains("Reconcile"))
                    {
                        voucherTitle = "سند تسوية حساب نقدي";
                        titleBg = Color.FromArgb(254, 252, 232);
                        titleBorder = Color.FromArgb(234, 179, 8);
                        amountColor = Color.FromArgb(133, 77, 14);
                    }

                    // Title Banner
                    int titleH = 28;
                    g.FillRectangle(new SolidBrush(titleBg), lMargin, y, printableW, titleH);
                    using (var p = new Pen(titleBorder, 1.2f))
                    {
                        g.DrawRectangle(p, lMargin, y, printableW, titleH);
                    }
                    g.DrawString(voucherTitle, fontTitle, Brushes.Black, new RectangleF(lMargin, y, printableW, titleH), sfCenter);
                    y += titleH + 8;

                    // Details
                    DrawReceiptKeyValue(g, "رقم السند:", $"#{transId}", fontBold, fontRegular, lMargin, ref y, printableW);
                    DrawReceiptKeyValue(g, "تاريخ السند:", dt.ToString("yyyy/MM/dd  hh:mm tt"), fontBold, fontRegular, lMargin, ref y, printableW);
                    DrawReceiptKeyValue(g, "الخزنة / الحساب:", safeName, fontBold, fontRegular, lMargin, ref y, printableW);
                    DrawReceiptKeyValue(g, "المستخدم:", userName, fontBold, fontRegular, lMargin, ref y, printableW);

                    if (!string.IsNullOrEmpty(partyName))
                    {
                        y += 2;
                        DrawDottedLine(g, lMargin, y, printableW);
                        y += 6;
                        string partyLabel = isDeposit ? "ورد من السيد/ة:" : "صرف إلى السيد/ة:";
                        DrawReceiptKeyValue(g, partyLabel, partyName, fontSection, fontSection, lMargin, ref y, printableW);
                    }

                    if (!string.IsNullOrEmpty(balanceNote))
                    {
                        y += 3;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), lMargin, y, printableW, 20);
                        g.DrawRectangle(Pens.LightGray, lMargin, y, printableW, 20);
                        g.DrawString(balanceNote, fontRegular, Brushes.DarkSlateGray, new RectangleF(lMargin + 4, y, printableW - 8, 20), sfCenter);
                        y += 24;
                    }

                    y += 4;
                    DrawDottedLine(g, lMargin, y, printableW);
                    y += 8;

                    // Prominent Amount Box
                    int amtBoxH = 56;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), lMargin, y, printableW, amtBoxH);
                    using (var p = new Pen(amountColor, 1.5f))
                    {
                        g.DrawRectangle(p, lMargin, y, printableW, amtBoxH);
                    }

                    string amtText = $"{amount:N2} ج.م";
                    using (var br = new SolidBrush(amountColor))
                    {
                        g.DrawString(amtText, fontAmount, br, new RectangleF(lMargin, y + 4, printableW, 26), sfCenter);
                    }

                    string tafqeet = TafqeetHelper.ConvertToArabicWords(amount);
                    g.DrawString($"فقط {tafqeet} لا غير", fontTafqeet, Brushes.Black, new RectangleF(lMargin + 4, y + 30, printableW - 8, 22), sfCenter);
                    y += amtBoxH + 10;

                    // Notes / Reason
                    if (!string.IsNullOrWhiteSpace(cleanNotes))
                    {
                        g.DrawString("البيان والسبب المحاسبي:", fontBold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), sfRight);
                        y += 18;

                        var sfNotes = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                        SizeF szNotes = g.MeasureString(cleanNotes, fontRegular, printableW, sfNotes);
                        int notesH = Math.Max(20, (int)Math.Ceiling(szNotes.Height));
                        g.DrawString(cleanNotes, fontRegular, Brushes.Black, new RectangleF(lMargin, y, printableW, notesH), sfNotes);
                        y += notesH + 8;
                    }

                    y += 4;
                    DrawDottedLine(g, lMargin, y, printableW);
                    y += 8;

                    // Footer Note
                    if (!string.IsNullOrWhiteSpace(AppConfig.ReceiptFooterNote))
                    {
                        g.DrawString(AppConfig.ReceiptFooterNote, fontRegular, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, 18), sfCenter);
                        y += 22;
                        DrawDottedLine(g, lMargin, y, printableW);
                        y += 8;
                    }

                    // Signatures
                    y += 6;
                    int halfW = printableW / 2;
                    g.DrawString("توقيع المستلم:", fontBold, Brushes.Black, new RectangleF(lMargin + halfW, y, halfW, 18), sfRight);
                    g.DrawString("أمين الخزينة:", fontBold, Brushes.Black, new RectangleF(lMargin, y, halfW, 18), sfRight);
                    y += 22;

                    g.DrawString("............................", fontRegular, Brushes.Gray, new RectangleF(lMargin + halfW, y, halfW, 16), sfCenter);
                    g.DrawString("............................", fontRegular, Brushes.Gray, new RectangleF(lMargin, y, halfW, 16), sfCenter);
                    y += 35;
                }
                else
                {
                    // Multi-row transaction statement on receipt
                    int titleH = 28;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(224, 242, 254)), lMargin, y, printableW, titleH);
                    g.DrawRectangle(Pens.SteelBlue, lMargin, y, printableW, titleH);
                    g.DrawString($"كشف حركة نقدية | {_accountName}", fontTitle, Brushes.Black, new RectangleF(lMargin, y, printableW, titleH), sfCenter);
                    y += titleH + 8;

                    DrawReceiptKeyValue(g, "تاريخ الطباعة:", DateTime.Now.ToString("yyyy/MM/dd hh:mm tt"), fontBold, fontRegular, lMargin, ref y, printableW);
                    DrawReceiptKeyValue(g, "عدد الحركات:", $"{_dtTransactions.Rows.Count}", fontBold, fontRegular, lMargin, ref y, printableW);

                    y += 4;
                    DrawDottedLine(g, lMargin, y, printableW);
                    y += 8;

                    foreach (DataRow r in _dtTransactions.Rows)
                    {
                        decimal inAmt = r.Table.Columns.Contains("AmountIn") && r["AmountIn"] != DBNull.Value ? Convert.ToDecimal(r["AmountIn"]) : 0m;
                        decimal outAmt = r.Table.Columns.Contains("AmountOut") && r["AmountOut"] != DBNull.Value ? Convert.ToDecimal(r["AmountOut"]) : 0m;
                        DateTime dt = Convert.ToDateTime(r["TransDate"]);
                        string tType = r["TransType"]?.ToString() ?? "";
                        string n = r["Notes"]?.ToString() ?? "";
                        if (n.Length > 25) n = n.Substring(0, 22) + "...";

                        string typeName = GetTransTypeArabicShort(tType);
                        string amtStr = inAmt > 0 ? $"+{inAmt:N2}" : $"-{outAmt:N2}";
                        Brush amtBrush = inAmt > 0 ? Brushes.DarkGreen : Brushes.DarkRed;

                        g.DrawString($"{dt:dd/MM} {typeName}", fontRegular, Brushes.Black, new RectangleF(lMargin + printableW - 140, y, 140, 18), sfRight);
                        g.DrawString(amtStr, fontBold, amtBrush, new RectangleF(lMargin, y, printableW - 145, 18), sfLeft);
                        y += 18;

                        if (!string.IsNullOrEmpty(n))
                        {
                            g.DrawString(n, fontRegular, Brushes.Gray, new RectangleF(lMargin, y, printableW, 16), sfRight);
                            y += 16;
                        }
                    }

                    y += 4;
                    DrawDottedLine(g, lMargin, y, printableW);
                    y += 8;

                    DrawReceiptKeyValue(g, "إجمالي المقبوضات:", $"{_totalIn:N2} ج", fontBold, fontBold, lMargin, ref y, printableW);
                    DrawReceiptKeyValue(g, "إجمالي المدفوعات:", $"{_totalOut:N2} ج", fontBold, fontBold, lMargin, ref y, printableW);
                    DrawReceiptKeyValue(g, "الصافي:", $"{_netBalance:N2} ج", fontSection, fontSection, lMargin, ref y, printableW);

                    y += 30;
                }
            };

            if (_showPreview)
            {
                var preview = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 460,
                    Height = 720,
                    Text = "معاينة طباعة سند الصرف والتوريد (بون حراري)"
                };
                preview.ShowDialog();
            }
            else
            {
                AppConfig.PrintInBackground(pd);
            }
        }

        private string GetTransTypeName(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "---";
            return type switch
            {
                "Deposit" => "سداد / توريد نقدي",
                "Withdraw" => "صرف نقدي",
                "SaleIncome" => "تحصيل مبيعات",
                "ClientPayment" => "سداد نقدي / تحصيل",
                "SupplierPayment" => "سداد للمورد / صرف",
                "Expense" => "مصروفات",
                "ShiftOpen" => "فتح وردية جديدة",
                "ShiftClose" => "تقفيل وردية",
                "ShiftDeficit" => "سند تسوية عجز وردية",
                "ShiftSurplus" => "سند تسوية زيادة وردية",
                "ReturnOutcome" => "صرف مرتجع مبيعات",
                "DriverHandover" => "تقفيل حساب مندوب",
                "Adjustment" => "تسوية حساب",
                "Transfer" or "TransferOut" or "TransferIn" => "تحويل مالي",
                "ShiftCloseOut" => "توريد تقفيل وردية إلى الخزنة",
                "ShiftCloseIn" => "استلام وارد تقفيل وردية",
                "InitialBalance" => "رصيد افتتاحي",
                _ => GetArabicFallback(type)
            };
        }

        private string GetTransTypeArabicShort(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "---";
            return type switch
            {
                "Deposit" => "توريد نقدي",
                "Withdraw" => "صرف نقدي",
                "SaleIncome" => "مبيعات نقدي",
                "ClientPayment" => "تحصيل عميل",
                "SupplierPayment" => "صرف للمورد",
                "Expense" => "مصروفات",
                "ShiftOpen" => "فتح وردية",
                "ShiftClose" => "إغلاق وردية",
                "ShiftDeficit" => "تسوية عجز وردية",
                "ShiftSurplus" => "تسوية زيادة وردية",
                "ReturnOutcome" => "مرتجع مبيعات",
                "DriverHandover" => "تقفيل مندوب",
                "Adjustment" => "تسوية حساب",
                "Transfer" or "TransferOut" or "TransferIn" => "تحويل مالي",
                "ShiftCloseOut" => "توريد للخزنة",
                "ShiftCloseIn" => "استلام من الدرج",
                "InitialBalance" => "رصيد افتتاحي",
                _ => GetArabicFallback(type)
            };
        }

        private string GetArabicFallback(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "---";
            if (type.Contains("ShiftOpen")) return "فتح وردية";
            if (type.Contains("ShiftClose")) return "تقفيل وردية";
            if (type.Contains("Sale")) return "مبيعات";
            if (type.Contains("Return")) return "مرتجع مبيعات";
            if (type.Contains("Client")) return "تحصيل عميل";
            if (type.Contains("Supplier")) return "صرف للمورد";
            if (type.Contains("Expense")) return "مصروفات";
            if (type.Contains("Deposit")) return "توريد نقدي";
            if (type.Contains("Withdraw")) return "صرف نقدي";
            if (type.Contains("Transfer")) return "تحويل مالي";
            return type;
        }

        private void DrawLogoOnLeft(Graphics g, int x, int y, int maxW, int maxH)
        {
            if (!AppConfig.PrintShopLogo || string.IsNullOrEmpty(AppConfig.ShopLogoPath)) return;
            try
            {
                if (System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                    {
                        double ratioX = (double)maxW / img.Width;
                        double ratioY = (double)maxH / img.Height;
                        double ratio = Math.Min(ratioX, ratioY);
                        int newW = (int)(img.Width * ratio);
                        int newH = (int)(img.Height * ratio);
                        g.DrawImage(img, x, y, newW, newH);
                    }
                }
            }
            catch { }
        }

        private void DrawWatermarkLogo(Graphics g, int pageW, int pageH)
        {
            if (string.IsNullOrEmpty(AppConfig.ShopLogoPath)) return;
            try
            {
                if (System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                    {
                        int w = 260;
                        int h = (int)(img.Height * ((double)w / img.Width));
                        int x = (pageW - w) / 2;
                        int y = (pageH - h) / 2;

                        // Render image with light transparency watermark matrix
                        var cm = new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.12f };
                        var ia = new System.Drawing.Imaging.ImageAttributes();
                        ia.SetColorMatrix(cm);
                        g.DrawImage(img, new Rectangle(x, y, w, h), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, ia);
                    }
                }
            }
            catch { }
        }

        private void DrawStringFit(Graphics g, string text, Font font, Brush brush, RectangleF rect, StringFormat sf, float minFontSize = 7f)
        {
            if (string.IsNullOrEmpty(text)) return;

            var sfNoWrap = (StringFormat)sf.Clone();
            sfNoWrap.FormatFlags |= StringFormatFlags.NoWrap;

            Font currentFont = font;
            SizeF sz = g.MeasureString(text, currentFont, (int)rect.Width, sfNoWrap);

            while (sz.Width > rect.Width - 1 && currentFont.Size > minFontSize)
            {
                float newSize = currentFont.Size - 0.5f;
                currentFont = new Font(font.FontFamily, newSize, font.Style);
                sz = g.MeasureString(text, currentFont, (int)rect.Width, sfNoWrap);
            }

            g.DrawString(text, currentFont, brush, rect, sfNoWrap);

            if (currentFont != font) currentFont.Dispose();
            sfNoWrap.Dispose();
        }

        private void DrawReceiptKeyValue(Graphics g, string label, string value, Font fontLbl, Font fontVal, int lMargin, ref int y, int printableW)
        {
            var sfR = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap };
            var sfL = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };

            int h = 20;
            int lblW = Math.Min(120, printableW / 2);
            int valW = printableW - lblW - 2;
            g.DrawString(label, fontLbl, Brushes.Black, new RectangleF(lMargin + valW, y, lblW, h), sfR);
            g.DrawString(value, fontVal, Brushes.Black, new RectangleF(lMargin, y, valW, h), sfL);
            y += h;
        }

        private void DrawDottedLine(Graphics g, int x, int y, int width)
        {
            using (var p = new Pen(Color.Gray, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
            {
                g.DrawLine(p, x, y, x + width, y);
            }
        }
    }
}

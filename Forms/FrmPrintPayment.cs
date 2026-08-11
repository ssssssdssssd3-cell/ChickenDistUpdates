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
        private int _currentRowIndex = 0;
        private int _pageNumber = 0;

        public FrmPrintPayment(DataTable dtTrans, string accountName, decimal startBalance, string template = null, bool showPreview = true)
        {
            _dtTransactions = dtTrans;
            _accountName = accountName ?? "الخزينة الرئيسية";
            _startBalance = startBalance;
            _voucherTemplate = template ?? AppConfig.VoucherTemplate;
            if (string.IsNullOrEmpty(_voucherTemplate))
                _voucherTemplate = "AlTarekVoucher";
            _showPreview = showPreview;

            CalculateTotals();
            DoPrint();
        }

        public FrmPrintPayment(int cashTransID, string template = null, bool showPreview = true)
        {
            _voucherTemplate = template ?? AppConfig.VoucherTemplate;
            if (string.IsNullOrEmpty(_voucherTemplate))
                _voucherTemplate = "AlTarekVoucher";
            _showPreview = showPreview;

            LoadSingleTrans(cashTransID);
            CalculateTotals();
            DoPrint();
        }

        private void LoadSingleTrans(int transID)
        {
            _dtTransactions = DbHelper.Query(@"
                SELECT c.TransID, c.TransDate, c.TransType, c.AmountIn, c.AmountOut, c.Notes, c.CreatedBy,
                       ISNULL(e.EmpName, N'---') AS CreatedByName,
                       ISNULL(acc.AccountName, N'الخزينة الرئيسية') AS AccountName
                FROM CashBox c
                LEFT JOIN Employees e ON c.CreatedBy = e.EmpID
                LEFT JOIN Accounts acc ON c.AccountID = acc.AccountID
                WHERE c.TransID = @id", DbHelper.P("@id", transID));

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
            var pd = new PrintDocument();
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

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

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

                    g.DrawString($"صفحة {_pageNumber}/1   {DateTime.Now:dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin + 10, y, 250, 30), sfLeft);
                    g.DrawString($"كشف حركة ونقدية / سند توريد وتحصيل | {_accountName}", boldMain, new SolidBrush(Color.FromArgb(15, 23, 42)), new RectangleF(lMargin + 260, y, printableW - 270, 30), sfRight);

                    y += 38;

                    // 4. Grid Table Headers
                    // Columns: [م] [العملية / البيان] [مدين] [دائن] [رصيد] [الخزنة] [نوع الحركة] [التاريخ] [ملاحظات] [المستخدم] [الوقت]
                    int[] colW = { 25, 110, 60, 60, 65, 75, 70, 65, 120, 80, 57 };
                    string[] colNames = { "م", "العملية", "مدين", "دائن", "رصيد", "الخزنة", "نوع الحركة", "التاريخ", "ملاحظات", "المستخدم", "الوقت" };

                    int xCur = lMargin;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), lMargin, y, printableW, 24);
                    g.DrawRectangle(Pens.Black, lMargin, y, printableW, 24);

                    for (int i = 0; i < colNames.Length; i++)
                    {
                        g.DrawString(colNames[i], boldSmall, Brushes.Black, new RectangleF(xCur, y, colW[i], 24), sfCenter);
                        if (i > 0)
                        {
                            g.DrawLine(Pens.Black, xCur, y, xCur, y + 24);
                        }
                        xCur += colW[i];
                    }
                    y += 24;

                    // Row 1: Opening / Carried Over Balance Row if page 1
                    if (_pageNumber == 1 && _startBalance != 0)
                    {
                        xCur = lMargin;
                        g.DrawRectangle(Pens.Black, lMargin, y, printableW, 20);

                        g.DrawString("1", smallFont, Brushes.Black, new RectangleF(xCur, y, colW[0], 20), sfCenter); xCur += colW[0];
                        g.DrawString("رصيد مرحل / سابق", boldSmall, Brushes.DarkBlue, new RectangleF(xCur, y, colW[1], 20), sfRight); xCur += colW[1];

                        string startDeb = _startBalance > 0 ? _startBalance.ToString("N2") : "";
                        string startCred = _startBalance < 0 ? Math.Abs(_startBalance).ToString("N2") : "";

                        g.DrawString(startDeb, boldSmall, Brushes.DarkRed, new RectangleF(xCur, y, colW[2], 20), sfCenter); xCur += colW[2];
                        g.DrawString(startCred, boldSmall, Brushes.DarkGreen, new RectangleF(xCur, y, colW[3], 20), sfCenter); xCur += colW[3];
                        g.DrawString(_startBalance.ToString("N2"), boldSmall, Brushes.Black, new RectangleF(xCur, y, colW[4], 20), sfCenter); xCur += colW[4];

                        for (int i = 5; i < colW.Length; i++)
                        {
                            xCur += colW[i];
                        }

                        // vertical grid lines for row 1
                        xCur = lMargin;
                        for (int i = 0; i < colW.Length; i++)
                        {
                            g.DrawLine(Pens.Black, xCur, y, xCur, y + 20);
                            xCur += colW[i];
                        }
                        g.DrawLine(Pens.Black, lMargin + printableW, y, lMargin + printableW, y + 20);

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

                        int rowH = 20;
                        g.DrawRectangle(Pens.Black, lMargin, y, printableW, rowH);

                        xCur = lMargin;
                        // م
                        g.DrawString((rowNo++).ToString(), smallFont, Brushes.Black, new RectangleF(xCur, y, colW[0], rowH), sfCenter); xCur += colW[0];
                        // العملية
                        g.DrawString(GetTransTypeName(tType), smallFont, Brushes.Black, new RectangleF(xCur + 2, y, colW[1] - 4, rowH), sfRight); xCur += colW[1];
                        // مدين
                        g.DrawString(inAmt > 0 ? inAmt.ToString("N2") : "", boldSmall, Brushes.DarkRed, new RectangleF(xCur, y, colW[2], rowH), sfCenter); xCur += colW[2];
                        // دائن
                        g.DrawString(outAmt > 0 ? outAmt.ToString("N2") : "", boldSmall, Brushes.DarkGreen, new RectangleF(xCur, y, colW[3], rowH), sfCenter); xCur += colW[3];
                        // رصيد
                        g.DrawString(running.ToString("N2"), boldSmall, new SolidBrush(Color.FromArgb(15, 23, 42)), new RectangleF(xCur, y, colW[4], rowH), sfCenter); xCur += colW[4];
                        // الخزنة
                        g.DrawString(_accountName, smallFont, Brushes.Black, new RectangleF(xCur + 2, y, colW[5] - 4, rowH), sfRight); xCur += colW[5];
                        // نوع الحركة
                        g.DrawString(tType, smallFont, Brushes.DimGray, new RectangleF(xCur, y, colW[6], rowH), sfCenter); xCur += colW[6];
                        // التاريخ
                        g.DrawString(datePart, smallFont, Brushes.Black, new RectangleF(xCur, y, colW[7], rowH), sfCenter); xCur += colW[7];
                        // ملاحظات
                        g.DrawString(notes, smallFont, Brushes.Black, new RectangleF(xCur + 2, y, colW[8] - 4, rowH), sfRight); xCur += colW[8];
                        // المستخدم
                        g.DrawString(user, smallFont, Brushes.Black, new RectangleF(xCur, y, colW[9], rowH), sfCenter); xCur += colW[9];
                        // الوقت
                        g.DrawString(timePart, smallFont, Brushes.Black, new RectangleF(xCur, y, colW[10], rowH), sfCenter); xCur += colW[10];

                        // Vertical lines
                        xCur = lMargin;
                        for (int i = 0; i < colW.Length; i++)
                        {
                            g.DrawLine(Pens.Black, xCur, y, xCur, y + rowH);
                            xCur += colW[i];
                        }
                        g.DrawLine(Pens.Black, lMargin + printableW, y, lMargin + printableW, y + rowH);

                        y += rowH;
                        _currentRowIndex++;
                    }

                    e.HasMorePages = false;

                    // 6. Summary Totals Row (Highlighted Background)
                    g.FillRectangle(new SolidBrush(Color.FromArgb(254, 226, 226)), lMargin, y, printableW, 24);
                    g.DrawRectangle(new Pen(Color.Red, 1.2f), lMargin, y, printableW, 24);

                    xCur = lMargin;
                    g.DrawString("إجمالي الحركة", boldMain, Brushes.DarkRed, new RectangleF(xCur, y, colW[0] + colW[1], 24), sfCenter);
                    xCur += colW[0] + colW[1];

                    g.DrawString(_totalIn.ToString("N2"), boldMain, Brushes.DarkRed, new RectangleF(xCur, y, colW[2], 24), sfCenter); xCur += colW[2];
                    g.DrawString(_totalOut.ToString("N2"), boldMain, Brushes.DarkGreen, new RectangleF(xCur, y, colW[3], 24), sfCenter); xCur += colW[3];
                    g.DrawString(_netBalance.ToString("N2"), boldMain, Brushes.Black, new RectangleF(xCur, y, colW[4], 24), sfCenter);

                    y += 32;

                    // 7. Tafqeet Text (تفقيط المبلغ بالحروف العربية بالعريضة)
                    string tafqeetText = TafqeetHelper.ConvertToArabicWords(Math.Abs(_netBalance));
                    g.DrawString(tafqeetText, boldMain, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), sfCenter);

                    y += 40;

                    // 8. Signatures Block
                    g.DrawString("توقيع المستلم: ....................", boldSmall, Brushes.Black, lMargin + 20, y);
                    g.DrawString("توقيع المحاسب: ....................", boldSmall, Brushes.Black, pageW / 2 - 70, y);
                    g.DrawString("توقيع أمين الخزينة: ....................", boldSmall, Brushes.Black, new RectangleF(0, y, pageW - rMargin - 20, 20), sfRight);

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
                pd.Print();
            }
        }

        private string GetTransTypeName(string type)
        {
            return type switch
            {
                "Deposit" => "سداد / توريد نقدي",
                "Withdraw" => "صرف نقدي",
                "SaleIncome" => "تحصيل مبيعات",
                "ClientPayment" => "سداد نقدي / تحصيل",
                "Expense" => "مصروفات",
                "ShiftClose" => "تقفيل وردية",
                _ => type
            };
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
    }
}

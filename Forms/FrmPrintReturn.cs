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
    /// معالج طباعة إيصالات وتقارير مرتجع المبيعات (حراري 80mm / A4 / A5) مع تفاصيل طريقة الدفع
    /// </summary>
    public class FrmPrintReturn
    {
        private int _returnID;
        private DataRow _returnRow;
        private DataTable _items;
        private int _printItemIndex = 0;
        private decimal _runningTotal = 0;
        private decimal _runningQtyTotal = 0;
        private string _printFormat;
        private bool _showPreview;
        private bool _isMultiPagePrinting = false;

        public static string FormatPaymentType(string payType)
        {
            if (string.IsNullOrEmpty(payType)) return "نقدي (كاش - من الدرج)";
            switch (payType.Trim())
            {
                case "Cash": return "نقدي (كاش - من الدرج)";
                case "Visa": return "فيزا (إلكتروني / بنك)";
                case "Credit": return "آجل (خصم من رصيد العميل)";
                case "Mixed": return "مختلط (كاش + فيزا)";
                default: return payType;
            }
        }

        public FrmPrintReturn(int returnID, string format = null, bool showPreview = false)
        {
            _returnID = returnID;
            _printFormat = format ?? AppConfig.DefaultInvoiceFormat;
            if (string.IsNullOrEmpty(_printFormat))
                _printFormat = "Receipt";
            _showPreview = showPreview;

            LoadData();
            DoPrint();
        }

        private void LoadData()
        {
            var dt = DbHelper.Query(@"
                SELECT sr.ReturnID, sr.ReturnDate, sr.TotalAmount, sr.Notes, sr.ReturnType,
                       ISNULL(sr.PaymentType, N'Cash') AS PaymentType,
                       ISNULL(s.SaleCode, N'مرتجع عام') AS SaleCode,
                       s.SaleDate AS OriginalSaleDate,
                       s.SaleType AS OriginalSaleType,
                       ISNULL(c.ClientName, N'عميل نقدي / عام') AS ClientName,
                       ISNULL(c.Phone, N'') AS ClientPhone,
                       ISNULL(c.Address, N'') AS ClientAddress,
                       ISNULL(e.EmpName, N'كاشير') AS CashierName,
                       ISNULL(w.WarehouseName, N'المخزن الرئيسي') AS WarehouseName
                FROM SalesReturns sr
                LEFT JOIN Sales s ON sr.SaleID = s.SaleID
                LEFT JOIN Clients c ON sr.ClientID = c.ClientID
                LEFT JOIN Employees e ON sr.CreatedBy = e.EmpID
                LEFT JOIN Warehouses w ON sr.WarehouseID = w.WarehouseID
                WHERE sr.ReturnID = @id", DbHelper.P("@id", _returnID));

            if (dt.Rows.Count > 0)
                _returnRow = dt.Rows[0];

            _items = DbHelper.Query(@"
                SELECT ri.ReturnItemID, ri.ProductID, ri.Quantity, ri.UnitPrice, 
                       ISNULL(ri.TotalPrice, ri.Quantity * ri.UnitPrice) AS TotalPrice,
                       ISNULL(ri.UnitName, ISNULL(p.Unit, N'')) AS UnitName,
                       ISNULL(p.ProductName, N'صنف عام') AS ProductName, 
                       ISNULL(p.ProductCode, N'') AS ProductCode
                FROM ReturnItems ri
                LEFT JOIN Products p ON ri.ProductID = p.ProductID
                WHERE ri.ReturnID = @id
                ORDER BY ri.ReturnItemID", DbHelper.P("@id", _returnID));
        }

        private void DoPrint()
        {
            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            bool isReceipt = string.Equals(_printFormat, "Receipt", StringComparison.OrdinalIgnoreCase);

            if (isReceipt)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 300, 1000);
                pd.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
            }
            else
            {
                bool isA4 = string.Equals(_printFormat, "A4", StringComparison.OrdinalIgnoreCase);
                if (isA4)
                {
                    pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                    pd.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);
                }
                else
                {
                    pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);
                    pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
                }
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            }

            pd.BeginPrint += (s, e) =>
            {
                _printItemIndex = 0;
                _runningTotal = 0;
                _runningQtyTotal = 0;
                _isMultiPagePrinting = false;
            };

            pd.PrintPage += (s, e) =>
            {
                // Reset per-job rendering state on first page of every pass
                if (!_isMultiPagePrinting)
                {
                    _printItemIndex = 0;
                    _runningTotal = 0;
                    _runningQtyTotal = 0;
                }

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int pageW = e.PageBounds.Width;
                bool isA4Page = (pageW > 700); // A4 = 827, A5 = 583, Receipt = 300
                float scaleFactor = isReceipt ? 1f : (isA4Page ? 1.4f : 1f);

                var boldBig = new Font("Segoe UI", (isReceipt ? 12f : (isA4Page ? 16f : 12f)), FontStyle.Bold);
                var boldMed = new Font("Segoe UI", (isReceipt ? 10f : (isA4Page ? 13f : 10f)), FontStyle.Bold);
                var bold    = new Font("Segoe UI", (isReceipt ? 9f : (isA4Page ? 11f : 9f)), FontStyle.Bold);
                var normal  = new Font("Segoe UI", (isReceipt ? 8.5f : (isA4Page ? 10.5f : 8.5f)), FontStyle.Regular);
                var small   = new Font("Segoe UI", (isReceipt ? 8f : (isA4Page ? 9.5f : 8f)), FontStyle.Regular);

                int lMargin = isReceipt ? 10 : 20;
                int rMargin = pageW - (isReceipt ? 10 : 20);
                int printableW = rMargin - lMargin;
                int y = isReceipt ? 10 : 20;

                var sfRight  = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                var sfLeft   = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Far,  LineAlignment = StringAlignment.Center };
                var sfCenter = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // 1. الشعار واسم المنشأة
                if (AppConfig.PrintShopLogo && !string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    try
                    {
                        using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                        {
                            int logoW = isReceipt ? 70 : (isA4Page ? 130 : 90);
                            int logoH = (int)((float)img.Height / img.Width * logoW);
                            g.DrawImage(img, (pageW - logoW) / 2, y, logoW, logoH);
                            y += logoH + 5;
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(AppConfig.CompanyName))
                {
                    g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), sfCenter);
                    y += 24;
                }

                if (!string.IsNullOrEmpty(AppConfig.CompanyPhone))
                {
                    g.DrawString("هاتف: " + AppConfig.CompanyPhone, small, Brushes.Black, new RectangleF(lMargin, y, printableW, 14), sfCenter);
                    y += 16;
                }

                // 2. عنوان الإيصال
                y += 2;
                int titleBarH = isReceipt ? 24 : (isA4Page ? 34 : 24);
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), lMargin, y, printableW, titleBarH);
                g.DrawRectangle(new Pen(Color.FromArgb(100, 116, 139), 1f), lMargin, y, printableW, titleBarH);
                g.DrawString("إيصال مرتجع مبيعات", boldMed, Brushes.Black, new RectangleF(lMargin, y, printableW, titleBarH), sfCenter);
                y += titleBarH + 6;

                if (_returnRow != null)
                {
                    string returnCode = "RET-" + _returnID;
                    DateTime returnDate = Convert.ToDateTime(_returnRow["ReturnDate"]);
                    string saleCode = _returnRow["SaleCode"]?.ToString() ?? "مرتجع عام";
                    string clientName = _returnRow["ClientName"]?.ToString() ?? "عميل نقدي";
                    string payType = _returnRow["PaymentType"]?.ToString() ?? "Cash";
                    string cashier = _returnRow["CashierName"]?.ToString() ?? "كاشير";
                    string formattedPayType = FormatPaymentType(payType);

                    // تفاصيل الإيصال (رقم المرتجع والتاريخ)
                    int halfW = printableW / 2;
                    int infoRowH = isReceipt ? 18 : (isA4Page ? 24 : 18);
                    int infoSpacing = isReceipt ? 20 : (isA4Page ? 28 : 20);
                    g.DrawString($"رقم المرتجع: {returnCode}", bold, Brushes.Black, new RectangleF(lMargin + halfW, y, halfW, infoRowH), sfRight);
                    g.DrawString($"التاريخ: {returnDate:yyyy/MM/dd hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, halfW, infoRowH), sfLeft);
                    y += infoSpacing;

                    // الفاتورة الأصلية والعميل
                    g.DrawString($"الفاتورة الأصلية: {saleCode}", normal, Brushes.Black, new RectangleF(lMargin + halfW, y, halfW, infoRowH), sfRight);
                    g.DrawString($"العميل: {clientName}", normal, Brushes.Black, new RectangleF(lMargin, y, halfW, infoRowH), sfLeft);
                    y += infoSpacing;

                    // إبراز طريقة دفع / رد المرتجع
                    int payBoxH = isReceipt ? 22 : (isA4Page ? 30 : 22);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), lMargin, y, printableW, payBoxH);
                    g.DrawRectangle(new Pen(Color.FromArgb(59, 130, 246), 1.2f), lMargin, y, printableW, payBoxH);
                    g.DrawString($"طريقة رد القيمة: {formattedPayType}", bold, Brushes.DarkBlue, new RectangleF(lMargin + 6, y, printableW - 12, payBoxH), sfRight);
                    y += payBoxH + 6;

                    // جدول الأصناف
                    int colTotW   = isReceipt ? 65 : (isA4Page ? 130 : 100);
                    int colPriceW = isReceipt ? 45 : (isA4Page ? 100 : 75);
                    int colQtyW   = isReceipt ? 45 : (isA4Page ? 100 : 75);
                    int colProdW  = printableW - colTotW - colPriceW - colQtyW;

                    int xProd  = lMargin + colTotW + colPriceW + colQtyW;
                    int xQty   = lMargin + colTotW + colPriceW;
                    int xPrice = lMargin + colTotW;
                    int xTot   = lMargin;

                    // رأس جدول الأصناف
                    int tblHeaderH = isReceipt ? 22 : (isA4Page ? 30 : 22);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(226, 232, 240)), lMargin, y, printableW, tblHeaderH);
                    g.DrawRectangle(Pens.Gray, lMargin, y, printableW, tblHeaderH);

                    g.DrawString("الصنف", bold, Brushes.Black, new RectangleF(xProd, y, colProdW, tblHeaderH), sfRight);
                    g.DrawString("الكمية", bold, Brushes.Black, new RectangleF(xQty, y, colQtyW, tblHeaderH), sfCenter);
                    g.DrawString("السعر", bold, Brushes.Black, new RectangleF(xPrice, y, colPriceW, tblHeaderH), sfCenter);
                    g.DrawString("الإجمالي", bold, Brushes.Black, new RectangleF(xTot, y, colTotW, tblHeaderH), sfLeft);

                    y += tblHeaderH;

                    // صفوف الأصناف
                    int itemRowH = isReceipt ? 22 : (isA4Page ? 28 : 22);
                    if (_items != null && _items.Rows.Count > 0)
                    {
                        while (_printItemIndex < _items.Rows.Count)
                        {
                            var r = _items.Rows[_printItemIndex];
                            string pName = r["ProductName"]?.ToString() ?? "صنف";
                            string unit = r["UnitName"]?.ToString() ?? "";
                            decimal q = r["Quantity"] != DBNull.Value ? Convert.ToDecimal(r["Quantity"]) : 0m;
                            decimal p = r["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(r["UnitPrice"]) : 0m;
                            decimal tot = r["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(r["TotalPrice"]) : (q * p);

                            _runningQtyTotal += q;
                            _runningTotal += tot;

                            string nameWithUnit = !string.IsNullOrWhiteSpace(unit) ? $"{pName} ({unit})" : pName;

                            // رسم خلفية خفيفة متناوبة
                            if (_printItemIndex % 2 == 1)
                            {
                                g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), lMargin, y, printableW, itemRowH);
                            }
                            g.DrawRectangle(new Pen(Color.FromArgb(226, 232, 240)), lMargin, y, printableW, itemRowH);

                            g.DrawString(nameWithUnit, normal, Brushes.Black, new RectangleF(xProd, y, colProdW, itemRowH), sfRight);
                            g.DrawString(q.ToString("0.##"), normal, Brushes.Black, new RectangleF(xQty, y, colQtyW, itemRowH), sfCenter);
                            g.DrawString(p.ToString("N2"), normal, Brushes.Black, new RectangleF(xPrice, y, colPriceW, itemRowH), sfCenter);
                            g.DrawString(tot.ToString("N2"), normal, Brushes.Black, new RectangleF(xTot, y, colTotW, itemRowH), sfLeft);

                            y += itemRowH;
                            _printItemIndex++;

                            if (y > e.PageBounds.Height - 150 && _printItemIndex < _items.Rows.Count)
                            {
                                _isMultiPagePrinting = true;
                                e.HasMorePages = true;
                                return;
                            }
                        }
                    }

                    _isMultiPagePrinting = false;
                    e.HasMorePages = false;

                    // خط فاصل بعد الأصناف
                    y += 4;

                    // صندوق الإجمالي النهائي
                    decimal totalAmount = Convert.ToDecimal(_returnRow["TotalAmount"]);
                    int totalBoxH = isReceipt ? 28 : (isA4Page ? 38 : 28);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 253, 244)), lMargin, y, printableW, totalBoxH);
                    g.DrawRectangle(new Pen(Color.FromArgb(22, 163, 74), 1.5f), lMargin, y, printableW, totalBoxH);
                    g.DrawString($"إجمالي المرتجع: {totalAmount:N2} جنيه", boldBig, Brushes.Black, new RectangleF(lMargin + 8, y, printableW - 16, totalBoxH), sfRight);
                    y += totalBoxH + 6;

                    // ملخص الكميات
                    int summaryH = isReceipt ? 16 : (isA4Page ? 22 : 16);
                    g.DrawString($"عدد الأصناف: {_items?.Rows.Count ?? 0}  |  إجمالي كميات المرتجع: {_runningQtyTotal:0.##}", small, Brushes.DimGray, new RectangleF(lMargin, y, printableW, summaryH), sfCenter);
                    y += summaryH + 4;

                    // ملاحظات إن وجدت
                    string notes = _returnRow["Notes"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        int noteH = isA4Page ? 28 : 22;
                        g.DrawString($"ملاحظات: {notes}", normal, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, noteH), sfRight);
                        y += noteH + 4;
                    }

                    // خط فاصل رفيع
                    g.DrawLine(new Pen(Color.FromArgb(203, 213, 225)), lMargin, y, rMargin, y);
                    y += isA4Page ? 14 : 8;

                    // معلومات الكاشير والتوقيع
                    int sigH = isA4Page ? 22 : 16;
                    g.DrawString($"الكاشير: {cashier}", small, Brushes.Black, new RectangleF(lMargin + halfW, y, halfW, sigH), sfRight);
                    g.DrawString("توقيع العميل / المستلم: ...........................................", small, Brushes.Black, new RectangleF(lMargin, y, halfW, sigH), sfLeft);
                    y += sigH + 8;

                    // عنوان الشركة إن وجد
                    if (!isReceipt && !string.IsNullOrEmpty(AppConfig.CompanyAddress))
                    {
                        g.DrawString($"العنوان: {AppConfig.CompanyAddress}", small, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 18), sfCenter);
                        y += 22;
                    }

                    g.DrawString("شكراً لتعاملكم معنا", small, Brushes.Gray, new RectangleF(lMargin, y, printableW, 14), sfCenter);
                }
            };

            try
            {
                if (_showPreview)
                {
                    var dlg = new PrintPreviewDialog { Document = pd, Width = 850, Height = 650 };
                    dlg.StartPosition = FormStartPosition.CenterScreen;
                    dlg.ShowDialog();
                }
                else
                {
                    pd.Print();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPrintReturn.DoPrint", ex);
                MessageBox.Show($"تعذر إتمام الطباعة: {ex.Message}", "خطأ في الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

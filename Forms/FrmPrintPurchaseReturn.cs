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
    /// معالج طباعة إيصالات وتقارير مرتجع المشتريات للموردين (حراري 80mm / A4 / A5) مع تفاصيل السيريالات وطريقة السداد
    /// </summary>
    public class FrmPrintPurchaseReturn
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
            if (string.IsNullOrEmpty(payType)) return "آجل (خصم من رصيد المورد)";
            switch (payType.Trim())
            {
                case "Cash": return "نقدي (استرداد كاش للخزنة)";
                case "Visa": return "فيزا / بنكي (استرداد للبنك)";
                case "Credit": return "آجل (خصم من حساب المورد)";
                default: return payType;
            }
        }

        public FrmPrintPurchaseReturn(int returnID, string format = null, bool showPreview = false)
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
                SELECT pr.ReturnID, pr.ReturnDate, pr.TotalAmount, pr.Notes,
                       ISNULL(pr.PaymentType, N'Credit') AS PaymentType,
                       ISNULL(p.PurchaseCode, N'مرتجع عام') AS PurchaseCode,
                       p.PurchaseDate AS OriginalPurchaseDate,
                       ISNULL(s.SupplierName, N'مورد عام') AS SupplierName,
                       ISNULL(s.Phone, N'') AS SupplierPhone,
                       ISNULL(s.Address, N'') AS SupplierAddress,
                       ISNULL(e.EmpName, N'كاشير') AS CashierName,
                       ISNULL(w.WarehouseName, N'المخزن الرئيسي') AS WarehouseName
                FROM PurchaseReturns pr
                LEFT JOIN Purchases p ON pr.PurchaseID = p.PurchaseID
                LEFT JOIN Suppliers s ON pr.SupplierID = s.SupplierID
                LEFT JOIN Employees e ON pr.CreatedBy = e.EmpID
                LEFT JOIN Warehouses w ON pr.WarehouseID = w.WarehouseID
                WHERE pr.ReturnID = @id", DbHelper.P("@id", _returnID));

            if (dt.Rows.Count > 0)
                _returnRow = dt.Rows[0];

            _items = DbHelper.Query(@"
                SELECT ISNULL(p.ProductName, N'صنف عام') AS ProductName, 
                       ISNULL(p.ProductCode, N'') AS ProductCode,
                       ISNULL(pri.UnitName, ISNULL(p.Unit, N'')) AS UnitName,
                       pri.Quantity, pri.UnitPrice, 
                       ISNULL(pri.TotalPrice, pri.Quantity * pri.UnitPrice) AS TotalPrice,
                       pri.ProductID,
                       ISNULL(pri.IMEI, N'') AS IMEI
                FROM PurchaseReturnItems pri
                LEFT JOIN Products p ON pri.ProductID = p.ProductID
                WHERE pri.ReturnID = @id", DbHelper.P("@id", _returnID));
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
                if (!_isMultiPagePrinting)
                {
                    _printItemIndex = 0;
                    _runningTotal = 0;
                    _runningQtyTotal = 0;
                }

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                bool isA5 = string.Equals(_printFormat, "A5", StringComparison.OrdinalIgnoreCase);
                bool isA4Page = !isReceipt && !isA5;

                Font boldBig = new Font("Segoe UI", isReceipt ? 12 : (isA4Page ? 16 : 13), FontStyle.Bold);
                Font boldMed = new Font("Segoe UI", isReceipt ? 10 : (isA4Page ? 13 : 11), FontStyle.Bold);
                Font bold    = new Font("Segoe UI", isReceipt ? 8.5f : (isA4Page ? 10.5f : 9f), FontStyle.Bold);
                Font normal  = new Font("Segoe UI", isReceipt ? 8f : (isA4Page ? 10f : 8.5f));
                Font small   = new Font("Segoe UI", isReceipt ? 7f : (isA4Page ? 8.5f : 7.5f));

                int pageW = e.PageBounds.Width;
                int lMargin = e.MarginBounds.Left;
                int rMargin = e.MarginBounds.Right;
                int printableW = rMargin - lMargin;
                int y = isReceipt ? 10 : (isA4Page ? 25 : 15);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRight  = new StringFormat { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center };
                var sfLeft   = new StringFormat { Alignment = StringAlignment.Near,   LineAlignment = StringAlignment.Center };

                // 1. ترويسة الشركة
                if (AppConfig.PrintShopLogo && !string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    try
                    {
                        using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                        {
                            int logoW = isReceipt ? 70 : (isA4Page ? 110 : 80);
                            int logoH = (int)((float)img.Height / img.Width * logoW);
                            g.DrawImage(img, new Rectangle(lMargin + (printableW - logoW) / 2, y, logoW, logoH));
                            y += logoH + 6;
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(AppConfig.CompanyName))
                {
                    g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), sfCenter);
                    y += 24;
                }

                if (AppConfig.PrintTaxAndCommercial && !string.IsNullOrWhiteSpace(AppConfig.GetTaxAndCommercialShortText()))
                {
                    g.DrawString(AppConfig.GetTaxAndCommercialShortText(), small, Brushes.Black, new RectangleF(lMargin, y, printableW, 14), sfCenter);
                    y += 16;
                }

                // 2. عنوان الإيصال
                y += 2;
                int titleBarH = isReceipt ? 24 : (isA4Page ? 34 : 24);
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), lMargin, y, printableW, titleBarH);
                g.DrawRectangle(new Pen(Color.FromArgb(180, 80, 50), 1.2f), lMargin, y, printableW, titleBarH);
                g.DrawString("إيصال مرتجع مشتريات (للمورد)", boldMed, Brushes.DarkRed, new RectangleF(lMargin, y, printableW, titleBarH), sfCenter);
                y += titleBarH + 6;

                if (_returnRow != null)
                {
                    string returnCode = "PRT-" + _returnID;
                    DateTime returnDate = Convert.ToDateTime(_returnRow["ReturnDate"]);
                    string purCode = _returnRow["PurchaseCode"]?.ToString() ?? "مرتجع عام";
                    string supplierName = _returnRow["SupplierName"]?.ToString() ?? "مورد عام";
                    string payType = _returnRow["PaymentType"]?.ToString() ?? "Credit";
                    string cashier = _returnRow["CashierName"]?.ToString() ?? "كاشير";
                    string warehouse = _returnRow["WarehouseName"]?.ToString() ?? "المخزن الرئيسي";
                    string formattedPayType = FormatPaymentType(payType);

                    int halfW = printableW / 2;
                    int infoRowH = isReceipt ? 18 : (isA4Page ? 24 : 18);
                    int infoSpacing = isReceipt ? 20 : (isA4Page ? 28 : 20);

                    g.DrawString($"رقم المرتجع: {returnCode}", bold, Brushes.Black, new RectangleF(lMargin + halfW, y, halfW, infoRowH), sfRight);
                    g.DrawString($"التاريخ: {returnDate:yyyy/MM/dd hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, halfW, infoRowH), sfLeft);
                    y += infoSpacing;

                    g.DrawString($"فاتورة الشراء: {purCode}", normal, Brushes.Black, new RectangleF(lMargin + halfW, y, halfW, infoRowH), sfRight);
                    g.DrawString($"المورد: {supplierName}", bold, Brushes.DarkBlue, new RectangleF(lMargin, y, halfW, infoRowH), sfLeft);
                    y += infoSpacing;

                    g.DrawString($"المخزن: {warehouse}", normal, Brushes.Black, new RectangleF(lMargin + halfW, y, halfW, infoRowH), sfRight);
                    g.DrawString($"طريقة السداد: {formattedPayType}", normal, Brushes.DarkGreen, new RectangleF(lMargin, y, halfW, infoRowH), sfLeft);
                    y += infoSpacing;
                }

                // 3. جدول الأصناف المرتجعة
                y += 4;
                int colTotW   = (int)(printableW * (isReceipt ? 0.22f : 0.18f));
                int colPriceW = (int)(printableW * (isReceipt ? 0.18f : 0.16f));
                int colQtyW   = (int)(printableW * (isReceipt ? 0.16f : 0.14f));
                int colProdW  = printableW - (colTotW + colPriceW + colQtyW);

                int xProd  = lMargin;
                int xQty   = xProd + colProdW;
                int xPrice = xQty + colQtyW;
                int xTot   = xPrice + colPriceW;

                int headerH = isReceipt ? 20 : (isA4Page ? 28 : 22);
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, 40, 40)), lMargin, y, printableW, headerH);
                g.DrawString("الصنف والسيريال", bold, Brushes.White, new RectangleF(xProd, y, colProdW, headerH), sfRight);
                g.DrawString("الكمية", bold, Brushes.White, new RectangleF(xQty, y, colQtyW, headerH), sfCenter);
                g.DrawString("السعر", bold, Brushes.White, new RectangleF(xPrice, y, colPriceW, headerH), sfCenter);
                g.DrawString("الإجمالي", bold, Brushes.White, new RectangleF(xTot, y, colTotW, headerH), sfLeft);
                y += headerH;

                int itemRowH = isReceipt ? 19 : (isA4Page ? 25 : 20);

                if (_items != null)
                {
                    while (_printItemIndex < _items.Rows.Count)
                    {
                        var r = _items.Rows[_printItemIndex];
                        string pName = r["ProductName"]?.ToString() ?? "صنف";
                        string imei = r.Table.Columns.Contains("IMEI") && r["IMEI"] != DBNull.Value ? r["IMEI"].ToString().Trim() : "";
                        if (!string.IsNullOrEmpty(imei))
                        {
                            pName = $"{pName} (سيريال: {imei})";
                        }
                        string unit = r["UnitName"]?.ToString() ?? "";
                        decimal q = r["Quantity"] != DBNull.Value ? Convert.ToDecimal(r["Quantity"]) : 0m;
                        decimal p = r["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(r["UnitPrice"]) : 0m;
                        decimal tot = r["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(r["TotalPrice"]) : (q * p);

                        _runningQtyTotal += q;
                        _runningTotal += tot;

                        string nameWithUnit = !string.IsNullOrWhiteSpace(unit) ? $"{pName} ({unit})" : pName;

                        if (_printItemIndex % 2 == 1)
                        {
                            g.FillRectangle(new SolidBrush(Color.FromArgb(250, 250, 252)), lMargin, y, printableW, itemRowH);
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

                y += 4;

                // 4. صندوق الإجمالي النهائي
                decimal totalAmount = _returnRow != null && _returnRow["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(_returnRow["TotalAmount"]) : _runningTotal;
                int totalBoxH = isReceipt ? 28 : (isA4Page ? 38 : 28);
                g.FillRectangle(new SolidBrush(Color.FromArgb(254, 242, 242)), lMargin, y, printableW, totalBoxH);
                g.DrawRectangle(new Pen(Color.FromArgb(185, 28, 28), 1.5f), lMargin, y, printableW, totalBoxH);
                g.DrawString($"إجمالي المرتجع: {totalAmount:N2} جنيه", boldBig, Brushes.DarkRed, new RectangleF(lMargin + 8, y, printableW - 16, totalBoxH), sfRight);
                y += totalBoxH + 6;

                // ملخص الكميات
                int summaryH = isReceipt ? 16 : (isA4Page ? 22 : 16);
                g.DrawString($"عدد الأصناف: {_items?.Rows.Count ?? 0}  |  إجمالي كميات المرتجع: {_runningQtyTotal:0.##}", small, Brushes.DimGray, new RectangleF(lMargin, y, printableW, summaryH), sfCenter);
                y += summaryH + 4;

                // ملاحظات
                string notes = _returnRow != null && _returnRow["Notes"] != DBNull.Value ? _returnRow["Notes"].ToString() : "";
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    g.DrawString($"ملاحظات: {notes}", small, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, 28), sfRight);
                    y += 30;
                }

                // 5. التوقيعات والتذييل
                if (!isReceipt)
                {
                    y += 15;
                    int sigW = printableW / 2;
                    g.DrawString("توقيع أمين المخزن: ....................", normal, Brushes.Black, new RectangleF(lMargin + sigW, y, sigW, 20), sfRight);
                    g.DrawString("استلام المورد / المندوب: ....................", normal, Brushes.Black, new RectangleF(lMargin, y, sigW, 20), sfLeft);
                    y += 25;
                }

                y += 6;
                g.DrawLine(new Pen(Color.FromArgb(200, 200, 200), 1f), lMargin, y, rMargin, y);
                y += 4;
                g.DrawString("تم إصدار هذا الإيصال إلكترونياً عبر برنامج ProSoft ERP", small, Brushes.Gray, new RectangleF(lMargin, y, printableW, 14), sfCenter);
            };

            if (_showPreview)
            {
                var ppd = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 900,
                    Height = 700,
                    StartPosition = FormStartPosition.CenterScreen
                };
                ppd.ShowDialog();
            }
            else
            {
                try
                {
                    pd.Print();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("FrmPrintPurchaseReturn.DoPrint", ex);
                    MessageBox.Show("فشلت عملية الطباعة: " + ex.Message, "خطأ طباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

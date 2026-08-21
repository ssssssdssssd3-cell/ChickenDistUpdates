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

        public static string FormatPaymentType(string payType)
        {
            if (string.IsNullOrEmpty(payType)) return "نقدي";
            switch (payType.Trim())
            {
                case "Cash": return "💵 نقدي (كاش)";
                case "Visa": return "💳 فيزا (شبكة / بطاقة)";
                case "Credit": return "📋 آجل (خصم من الحساب)";
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
                SELECT ri.ReturnItemID, ri.ProductID, ri.Quantity, ri.UnitPrice, ri.TotalPrice,
                       ISNULL(ri.UnitName, p.Unit) AS UnitName,
                       p.ProductName, p.ProductCode
                FROM ReturnItems ri
                JOIN Products p ON ri.ProductID = p.ProductID
                WHERE ri.ReturnID = @id", DbHelper.P("@id", _returnID));
        }

        private void DoPrint()
        {
            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            bool isReceipt = string.Equals(_printFormat, "Receipt", StringComparison.OrdinalIgnoreCase);

            if (isReceipt)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 300, 1000);
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
            }
            else
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);
                pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            }

            pd.BeginPrint += (s, e) =>
            {
                _printItemIndex = 0;
                _runningTotal = 0;
                _runningQtyTotal = 0;
            };

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 12, FontStyle.Bold);
                var bold = new Font("Arial", 9.5f, FontStyle.Bold);
                var normal = new Font("Arial", 8.5f);
                var small = new Font("Arial", 7.5f);

                int pageW = e.PageBounds.Width;
                int lMargin = isReceipt ? 12 : 20;
                int rMargin = pageW - (isReceipt ? 12 : 20);
                int printableW = rMargin - lMargin;
                int y = isReceipt ? 10 : 20;

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right  = new StringFormat { Alignment = StringAlignment.Far };
                var left   = new StringFormat { Alignment = StringAlignment.Near };

                // 1. الشعار واسم المنشأة
                if (AppConfig.PrintShopLogo && !string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    try
                    {
                        using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                        {
                            int logoW = isReceipt ? 70 : 90;
                            int logoH = (int)((float)img.Height / img.Width * logoW);
                            g.DrawImage(img, (pageW - logoW) / 2, y, logoW, logoH);
                            y += logoH + 5;
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(AppConfig.CompanyName))
                {
                    g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), center);
                    y += 24;
                }

                if (!string.IsNullOrEmpty(AppConfig.CompanyPhone))
                {
                    g.DrawString("هاتف: " + AppConfig.CompanyPhone, small, Brushes.Black, new RectangleF(lMargin, y, printableW, 14), center);
                    y += 15;
                }

                // 2. عنوان الإيصال
                y += 4;
                g.FillRectangle(new SolidBrush(Color.FromArgb(230, 230, 230)), lMargin, y, printableW, 22);
                g.DrawRectangle(Pens.Gray, lMargin, y, printableW, 22);
                g.DrawString("إيصال مرتجع مبيعات", bold, Brushes.Black, new RectangleF(lMargin, y + 2, printableW, 18), center);
                y += 28;

                if (_returnRow != null)
                {
                    string returnCode = "RET-" + _returnID;
                    DateTime returnDate = Convert.ToDateTime(_returnRow["ReturnDate"]);
                    string saleCode = _returnRow["SaleCode"]?.ToString() ?? "مرتجع عام";
                    string clientName = _returnRow["ClientName"]?.ToString() ?? "عميل نقدي";
                    string payType = _returnRow["PaymentType"]?.ToString() ?? "Cash";
                    string cashier = _returnRow["CashierName"]?.ToString() ?? "كاشير";
                    string formattedPayType = FormatPaymentType(payType);

                    // تفاصيل الإيصال
                    g.DrawString($"رقم المرتجع: {returnCode}", bold, Brushes.Black, new RectangleF(lMargin, y, printableW / 2, 16), right);
                    g.DrawString($"التاريخ: {returnDate:yyyy/MM/dd hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin + (printableW / 2), y, printableW / 2, 16), left);
                    y += 18;

                    g.DrawString($"الفاتورة الأصلية: {saleCode}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW / 2, 16), right);
                    g.DrawString($"العميل: {clientName}", normal, Brushes.Black, new RectangleF(lMargin + (printableW / 2), y, printableW / 2, 16), left);
                    y += 18;

                    // إبراز طريقة دفع / رد المرتجع
                    g.FillRectangle(new SolidBrush(Color.FromArgb(245, 245, 255)), lMargin, y, printableW, 20);
                    g.DrawRectangle(new Pen(Color.FromArgb(59, 130, 246)), lMargin, y, printableW, 20);
                    g.DrawString($"طريقة رد القيمة: {formattedPayType}", bold, Brushes.DarkBlue, new RectangleF(lMargin + 5, y + 2, printableW - 10, 16), right);
                    y += 26;

                    // خط فاصل
                    g.DrawLine(Pens.Black, lMargin, y, rMargin, y);
                    y += 4;

                    // رأس جدول الأصناف
                    int colProdW = (int)(printableW * 0.45f);
                    int colQtyW  = (int)(printableW * 0.18f);
                    int colPriceW = (int)(printableW * 0.18f);
                    int colTotW  = printableW - colProdW - colQtyW - colPriceW;

                    int x = rMargin;
                    g.DrawString("الصنف", bold, Brushes.Black, new RectangleF(x - colProdW, y, colProdW, 16), right);
                    x -= colProdW;
                    g.DrawString("الكمية", bold, Brushes.Black, new RectangleF(x - colQtyW, y, colQtyW, 16), center);
                    x -= colQtyW;
                    g.DrawString("السعر", bold, Brushes.Black, new RectangleF(x - colPriceW, y, colPriceW, 16), center);
                    x -= colPriceW;
                    g.DrawString("الإجمالي", bold, Brushes.Black, new RectangleF(x - colTotW, y, colTotW, 16), left);

                    y += 18;
                    g.DrawLine(Pens.Black, lMargin, y, rMargin, y);
                    y += 4;

                    // صفوف الأصناف
                    if (_items != null)
                    {
                        while (_printItemIndex < _items.Rows.Count)
                        {
                            var r = _items.Rows[_printItemIndex];
                            string pName = r["ProductName"]?.ToString() ?? "";
                            string unit = r["UnitName"]?.ToString() ?? "";
                            decimal q = Convert.ToDecimal(r["Quantity"]);
                            decimal p = Convert.ToDecimal(r["UnitPrice"]);
                            decimal tot = Convert.ToDecimal(r["TotalPrice"]);

                            _runningQtyTotal += q;
                            _runningTotal += tot;

                            x = rMargin;
                            string nameWithUnit = !string.IsNullOrEmpty(unit) ? $"{pName} ({unit})" : pName;
                            g.DrawString(nameWithUnit, normal, Brushes.Black, new RectangleF(x - colProdW, y, colProdW, 16), right);
                            x -= colProdW;
                            g.DrawString(q.ToString("G29"), normal, Brushes.Black, new RectangleF(x - colQtyW, y, colQtyW, 16), center);
                            x -= colQtyW;
                            g.DrawString(p.ToString("N2"), normal, Brushes.Black, new RectangleF(x - colPriceW, y, colPriceW, 16), center);
                            x -= colPriceW;
                            g.DrawString(tot.ToString("N2"), normal, Brushes.Black, new RectangleF(x - colTotW, y, colTotW, 16), left);

                            y += 18;
                            _printItemIndex++;

                            if (y > e.PageBounds.Height - 120 && _printItemIndex < _items.Rows.Count)
                            {
                                e.HasMorePages = true;
                                return;
                            }
                        }
                    }

                    // خط ختامي
                    g.DrawLine(Pens.Black, lMargin, y, rMargin, y);
                    y += 6;

                    // الإجمالي النهائي
                    decimal totalAmount = Convert.ToDecimal(_returnRow["TotalAmount"]);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(235, 235, 235)), lMargin, y, printableW, 24);
                    g.DrawRectangle(Pens.Black, lMargin, y, printableW, 24);
                    g.DrawString($"إجمالي المرتجع: {totalAmount:N2} ج", boldBig, Brushes.Black, new RectangleF(lMargin, y + 3, printableW - 10, 20), right);
                    y += 30;

                    // ملاحظات
                    string notes = _returnRow["Notes"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        g.DrawString($"ملاحظات: {notes}", normal, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, 30), right);
                        y += 32;
                    }

                    // معلومات الكاشير والتوقيع
                    y += 8;
                    g.DrawString($"الكاشير: {cashier}", small, Brushes.Black, new RectangleF(lMargin, y, printableW / 2, 14), right);
                    g.DrawString("توقيع العميل / المستلم: .................", small, Brushes.Black, new RectangleF(lMargin + (printableW / 2), y, printableW / 2, 14), left);
                    y += 20;

                    g.DrawString("شكراً لتعاملكم معنا", small, Brushes.Gray, new RectangleF(lMargin, y, printableW, 14), center);
                }

                e.HasMorePages = false;
            };

            try
            {
                if (_showPreview)
                {
                    var dlg = new PrintPreviewDialog { Document = pd, Width = 800, Height = 600 };
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

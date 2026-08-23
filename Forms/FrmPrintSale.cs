using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPrintSale
    {
        private int _saleID;
        private DataRow _saleRow;
        private DataTable _items;
        private int _printItemIndex = 0;
        private decimal _runningTotal = 0;
        private decimal _runningQtyTotal = 0;
        private string _printFormat;
        private bool _showPreview;

        public static string FormatSaleType(string saleType, string visaAccountName = null)
        {
            if (string.IsNullOrEmpty(saleType)) return "نقدي";
            switch (saleType)
            {
                case "Cash": return "نقدي";
                case "Credit": return "آجل";
                case "Visa": return !string.IsNullOrEmpty(visaAccountName) ? $"فيزا ({visaAccountName})" : "فيزا / شبكة";
                case "Mixed": return !string.IsNullOrEmpty(visaAccountName) ? $"مختلط (كاش + فيزا {visaAccountName})" : "مختلط (كاش + فيزا)";
                case "Installment": return "تقسيط شرعي";
                case "DriverLoad": return "تحميل مندوب";
                default: return saleType;
            }
        }

        public FrmPrintSale(int saleID, string format = null, bool showPreview = false)
        {
            _saleID = saleID;
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
                SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType, s.ClientID, s.TotalAmount, s.Notes, s.CashPaid,
                       ISNULL(s.VisaPaid, 0) AS VisaPaid, s.VisaAccountID, sa.AccountName AS VisaAccountName,
                       COALESCE(s.CratesOut, 0) AS CratesOut, COALESCE(s.CratesIn, 0) AS CratesIn,
                       COALESCE(s.DiscountAmount, 0) AS DiscountAmount, COALESCE(s.DiscountPct, 0) AS DiscountPct,
                       COALESCE(s.ShippingCharge, 0) AS ShippingCharge,
                       CASE WHEN s.ClientID IS NULL AND (s.SaleType = 'Cash' OR s.SaleType = 'Visa') THEN (CASE WHEN s.SaleType = 'Visa' THEN N'عميل فيزا' ELSE N'عميل نقدي' END) ELSE COALESCE(c.ClientName, N'---') END AS ClientName,
                       COALESCE(c.Phone, N'') AS ClientPhone,
                       COALESCE(c.Address, N'') AS ClientAddress,
                       COALESCE(e.EmpName, N'---') AS DriverName
                 FROM Sales s
                 LEFT JOIN Clients c ON s.ClientID = c.ClientID
                 LEFT JOIN Employees e ON s.DriverID = e.EmpID
                 LEFT JOIN SafeAccounts sa ON s.VisaAccountID = sa.AccountID
                 WHERE s.SaleID = @id", DbHelper.P("@id", _saleID));
            if (dt.Rows.Count > 0)
                _saleRow = dt.Rows[0];
            
            _items = SaleDAL.GetItems(_saleID);
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
                bool isA4 = string.Equals(_printFormat, "A4", StringComparison.OrdinalIgnoreCase) ||
                            (string.Equals(AppConfig.DefaultInvoiceFormat, "A4", StringComparison.OrdinalIgnoreCase) && !string.Equals(_printFormat, "A5", StringComparison.OrdinalIgnoreCase));
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

                // Prepend/append IMEI to ProductName for mobile business type
                if (AppConfig.BusinessType == "Mobiles" && _items != null && _items.Columns.Contains("IMEI"))
                {
                    foreach (DataRow r in _items.Rows)
                    {
                        string imei = r["IMEI"]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(imei))
                        {
                            r["ProductName"] = r["ProductName"].ToString() + " (IMEI: " + imei + ")";
                        }
                    }
                }
            };

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                int pageW = e.PageBounds.Width;
                bool isA4Page = !isReceipt && pageW > 700;
                int lMargin = isReceipt ? 12 : (isA4Page ? 30 : 20);
                int rMargin = isReceipt ? 28 : (isA4Page ? 30 : 20);
                int printableW = pageW - lMargin - rMargin;
                int margin = lMargin;
                int y = isReceipt ? 5 : (isA4Page ? 20 : 15);

                var boldBig = new Font("Arial", isReceipt ? 12 : (isA4Page ? 16 : 13), FontStyle.Bold);
                var bold = new Font("Arial", isReceipt ? 9 : (isA4Page ? 11 : 9.5f), FontStyle.Bold);
                var normal = new Font("Arial", isReceipt ? 8.5f : (isA4Page ? 10f : 8.5f));
                var small = new Font("Arial", isReceipt ? 7.5f : (isA4Page ? 9f : 7.5f));

                DrawShopLogo(g, pageW, ref y, isReceipt);

                if (isReceipt && y < 50) y = 50; // ensure minimum top margin after logo

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };
                var left = new StringFormat { Alignment = StringAlignment.Near };
                bool detailedPrint = AppConfig.ReceiptPrintMode != "Compact";

                if (isReceipt)
                {
                    // ==========================================
                    // THERMAL RECEIPT LAYOUT (80mm width)
                    // ==========================================
                    string template = AppConfig.ReceiptTemplate;
                    
                    if (string.Equals(template, "MiniMarket", StringComparison.OrdinalIgnoreCase))
                    {
                        // MiniMarket template: optimized for grocery lists
                        g.DrawString(AppConfig.CompanyName, bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), center); y += 18;
                        if (!string.IsNullOrWhiteSpace(AppConfig.ReceiptHeaderNote))
                        {
                            var headerSize = g.MeasureString(AppConfig.ReceiptHeaderNote, normal, printableW);
                            g.DrawString(AppConfig.ReceiptHeaderNote, normal, Brushes.DimGray, new RectangleF(lMargin, y, printableW, headerSize.Height + 2), center);
                            y += (int)headerSize.Height + 4;
                        }
                        g.DrawString("فاتورة مبيعات مبسطة", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), center); y += 16;
                        g.DrawLine(new Pen(Color.Black, 1.2f), lMargin, y, pageW - rMargin, y); y += 6;
                        
                        if (_saleRow != null)
                        {
                            g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            
                            string typeLabel = FormatSaleType(_saleRow["SaleType"]?.ToString());
                            g.DrawString($"العميل: {_saleRow["ClientName"]} ({typeLabel})", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 18;
                        }
                        
                        g.DrawLine(new Pen(Color.Black, 1.2f), lMargin, y, pageW - rMargin, y); y += 6;
                        
                        // Table header
                        g.DrawString("بيان الأصناف والكميات", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right);
                        y += 18;
                        g.DrawLine(Pens.Gray, lMargin, y, pageW - rMargin, y); y += 6;
                        
                        if (_items != null)
                        {
                            while (_printItemIndex < _items.Rows.Count)
                            {
                                if (y + 40 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }
                                DataRow r = _items.Rows[_printItemIndex];
                                string prodName = r["ProductName"].ToString();
                                decimal qty = Convert.ToDecimal(r["Quantity"]);
                                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                                decimal tot = Convert.ToDecimal(r["TotalPrice"]);
                                
                                // Line 1: Item Name
                                g.DrawString(prodName, bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right);
                                y += 16;
                                
                                // Line 2: Qty x Price = Total
                                g.DrawString($"{qty:0.##} × {price:N2}", normal, Brushes.DimGray, new RectangleF(lMargin + 80, y, printableW - 80, 14), right);
                                g.DrawString(tot.ToString("N2"), bold, Brushes.Black, new RectangleF(lMargin, y, 80, 14), left);
                                y += 15;
                                g.DrawLine(Pens.LightGray, lMargin, y, pageW - rMargin, y);
                                y += 4;
                                
                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        
                        e.HasMorePages = false;
                        g.DrawLine(new Pen(Color.Black, 1.2f), lMargin, y, pageW - rMargin, y); y += 6;
                    }
                    else if (string.Equals(template, "Modern", StringComparison.OrdinalIgnoreCase))
                    {
                        // Fill a modern slate gray banner with white text
                        g.FillRectangle(Brushes.DarkSlateGray, lMargin, y, printableW, 26);
                        g.DrawString(AppConfig.CompanyName, bold, Brushes.White, new RectangleF(lMargin, y + 4, printableW, 20), center);
                        y += 32;
                        if (!string.IsNullOrWhiteSpace(AppConfig.ReceiptHeaderNote))
                        {
                            var headerSize = g.MeasureString(AppConfig.ReceiptHeaderNote, normal, printableW);
                            g.DrawString(AppConfig.ReceiptHeaderNote, normal, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, headerSize.Height + 2), center);
                            y += (int)headerSize.Height + 4;
                        }
                        
                        g.DrawString("فاتورة مبيعات", boldBig, Brushes.Black, new RectangleF(lMargin, y, printableW, 25), center);
                        y += 24;
                        g.DrawLine(new Pen(Color.Black, 1.5f), lMargin, y, pageW - rMargin, y);
                        y += 6;
                        
                        // Sale Info
                        if (_saleRow != null)
                        {
                            g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 18;
                            g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 16;
                            
                            string typeLabel = FormatSaleType(_saleRow["SaleType"]?.ToString());
                            string driverText = _saleRow["DriverName"].ToString() != "---" ? $" | مندوب: {_saleRow["DriverName"]}" : "";
                            g.DrawString($"العميل: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 16;
                            g.DrawString($"طريقة الدفع: {typeLabel}{driverText}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 20;
                        }
                        
                        g.DrawLine(new Pen(Color.Black, 1.5f), lMargin, y, pageW - rMargin, y);
                        y += 6;
                        
                        // Table header for items
                        int colSplit = lMargin + 70;
                        int wNameModern = printableW - 70;
                        int headerY = y - 2;

                        g.DrawString("الصنف والكمية", bold, Brushes.Black, new RectangleF(colSplit, y, wNameModern - 4, 16), right);
                        g.DrawString("الإجمالي", bold, Brushes.Black, new RectangleF(lMargin + 4, y, 70 - 4, 16), left);
                        y += 18;
                        
                        // Draw header borders
                        g.DrawLine(Pens.Black, lMargin, headerY, pageW - rMargin, headerY); // top
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); // bottom
                        g.DrawLine(Pens.Black, lMargin, headerY, lMargin, y); // left
                        g.DrawLine(Pens.Black, colSplit, headerY, colSplit, y); // middle
                        g.DrawLine(Pens.Black, pageW - rMargin, headerY, pageW - rMargin, y); // right
                        
                        y += 6;
                        
                        if (_items != null)
                        {
                            while (_printItemIndex < _items.Rows.Count)
                            {
                                if (y + 50 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }
                                DataRow r = _items.Rows[_printItemIndex];
                                string prodName = r["ProductName"].ToString();
                                decimal qty = Convert.ToDecimal(r["Quantity"]);
                                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                                decimal tot = Convert.ToDecimal(r["TotalPrice"]);
                                
                                int itemY = y - 2;

                                g.DrawString(prodName, bold, Brushes.Black, new RectangleF(colSplit, y, wNameModern - 4, 16), right);
                                g.DrawString(tot.ToString("N2"), bold, Brushes.Black, new RectangleF(lMargin + 4, y, 70 - 4, 16), left);
                                y += 15;
                                
                                g.DrawString($"{qty:0.##} × {price:N2}", small, Brushes.DimGray, new RectangleF(colSplit, y, wNameModern - 4, 14), right);
                                y += 15;
                                
                                // Draw item borders
                                g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); // bottom
                                g.DrawLine(Pens.Black, lMargin, itemY, lMargin, y); // left
                                g.DrawLine(Pens.Black, colSplit, itemY, colSplit, y); // middle
                                g.DrawLine(Pens.Black, pageW - rMargin, itemY, pageW - rMargin, y); // right
                                
                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        
                        e.HasMorePages = false;
                        g.DrawLine(new Pen(Color.Black, 1.5f), lMargin, y, pageW - rMargin, y);
                        y += 6;
                    }
                    else if (string.Equals(template, "Compact", StringComparison.OrdinalIgnoreCase))
                    {
                        // Compact format: smaller fonts, tight layout, single-line items
                        g.DrawString(AppConfig.CompanyName, bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), center); y += 16;
                        if (!string.IsNullOrWhiteSpace(AppConfig.ReceiptHeaderNote))
                        {
                            var headerSize = g.MeasureString(AppConfig.ReceiptHeaderNote, small, printableW);
                            g.DrawString(AppConfig.ReceiptHeaderNote, small, Brushes.DimGray, new RectangleF(lMargin, y, printableW, headerSize.Height + 2), center);
                            y += (int)headerSize.Height + 2;
                        }
                        g.DrawString("فاتورة مبيعات مبسطة", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), center); y += 14;
                        g.DrawLine(Pens.Gray, lMargin, y, pageW - rMargin, y); y += 4;
                        
                        if (_saleRow != null)
                        {
                            g.DrawString($"فاتورة: {_saleRow["SaleCode"]} | {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy}", small, Brushes.Black, new RectangleF(lMargin, y, printableW, 14), right); y += 14;
                            g.DrawString($"العميل: {_saleRow["ClientName"]}", small, Brushes.Black, new RectangleF(lMargin, y, printableW, 14), right); y += 14;
                        }
                        g.DrawLine(Pens.Gray, lMargin, y, pageW - rMargin, y); y += 4;
                        
                        if (_items != null)
                        {
                            while (_printItemIndex < _items.Rows.Count)
                            {
                                if (y + 30 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }
                                DataRow r = _items.Rows[_printItemIndex];
                                string prodName = r["ProductName"].ToString();
                                decimal qty = Convert.ToDecimal(r["Quantity"]);
                                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                                decimal tot = Convert.ToDecimal(r["TotalPrice"]);
                                
                                // Print name on right, details on left to fit in one line
                                string itemLineRight = prodName;
                                string itemLineLeft = $"{qty:0.##}x{price:0.##}={tot:0.##}";
                                g.DrawString(itemLineRight, small, Brushes.Black, new RectangleF(lMargin + 120, y, printableW - 120, 14), right);
                                g.DrawString(itemLineLeft, small, Brushes.Black, new RectangleF(lMargin, y, 120, 14), left);
                                y += 13;
                                g.DrawLine(Pens.LightGray, lMargin, y, pageW - rMargin, y);
                                y += 3;
                                
                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        
                        e.HasMorePages = false;
                        g.DrawLine(Pens.Gray, lMargin, y, pageW - rMargin, y); y += 4;
                    }
                    else if (string.Equals(template, "Elegant", StringComparison.OrdinalIgnoreCase))
                    {
                        // Elegant format: stylish headers, decorative lines, fancy layout
                        g.DrawString("❀ " + AppConfig.CompanyName + " ❀", boldBig, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, 25), center); y += 24;
                        if (!string.IsNullOrWhiteSpace(AppConfig.ReceiptHeaderNote))
                        {
                            var headerSize = g.MeasureString(AppConfig.ReceiptHeaderNote, normal, printableW);
                            g.DrawString(AppConfig.ReceiptHeaderNote, normal, Brushes.DimGray, new RectangleF(lMargin, y, printableW, headerSize.Height + 2), center);
                            y += (int)headerSize.Height + 4;
                        }
                        g.DrawString("ـ ــ ـــ فاتورة مبيعات ـــ ــ ـ", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), center); y += 20;
                        g.DrawString("✿ ✿ ✿ ✿ ✿ ✿ ✿ ✿ ✿ ✿ ✿", small, Brushes.Gray, new RectangleF(lMargin, y, printableW, 14), center); y += 14;
                        
                        if (_saleRow != null)
                        {
                            g.DrawString($"رقم المستند: {_saleRow["SaleCode"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 16;
                            g.DrawString($"تاريخ الإصدار: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 16;
                            g.DrawString($"السيد/ة: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 18;
                        }
                        g.DrawLine(new Pen(Color.Black, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }, lMargin, y, pageW - rMargin, y); y += 6;
                        
                        if (_items != null)
                        {
                            while (_printItemIndex < _items.Rows.Count)
                            {
                                if (y + 60 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }
                                DataRow r = _items.Rows[_printItemIndex];
                                string prodName = r["ProductName"].ToString();
                                decimal qty = Convert.ToDecimal(r["Quantity"]);
                                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                                decimal tot = Convert.ToDecimal(r["TotalPrice"]);
                                
                                g.DrawString(prodName, bold, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, 18), right);
                                y += 16;
                                
                                string details = $"{qty:0.##} وحدة × {price:N2} = {tot:N2}";
                                g.DrawString(details, normal, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 16), right);
                                y += 16;
                                
                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        
                        e.HasMorePages = false;
                        g.DrawLine(new Pen(Color.Black, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }, lMargin, y, pageW - rMargin, y); y += 6;
                    }
                    else if (string.Equals(template, "GridReceipt", StringComparison.OrdinalIgnoreCase))
                    {
                        // GridReceipt: Highly readable, columns and rows separated clearly by clean lines
                        g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(lMargin, y, printableW, 25), center); y += 22;
                        if (!string.IsNullOrWhiteSpace(AppConfig.ReceiptHeaderNote))
                        {
                            var headerSize = g.MeasureString(AppConfig.ReceiptHeaderNote, normal, printableW);
                            g.DrawString(AppConfig.ReceiptHeaderNote, normal, Brushes.DimGray, new RectangleF(lMargin, y, printableW, headerSize.Height + 2), center);
                            y += (int)headerSize.Height + 4;
                        }
                        g.DrawString("فاتورة مبيعات", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), center); y += 20;
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); y += 6;

                        if (_saleRow != null)
                        {
                            g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            g.DrawString($"العميل: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 18;
                        }
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); y += 4;

                        // Grid Columns Setup
                        int wTotGrid = 60;
                        int wPriceGrid = 55;
                        int wQtyGrid = 40;
                        int wNameGrid = printableW - wTotGrid - wPriceGrid - wQtyGrid;

                        int colTotG = lMargin;
                        int colPriceG = colTotG + wTotGrid;
                        int colQtyG = colPriceG + wPriceGrid;
                        int colNameG = colQtyG + wQtyGrid;

                        int headY = y - 2;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), lMargin, headY, printableW, 20);
                        var alignCenter = new StringFormat { Alignment = StringAlignment.Center };

                        g.DrawString("الإجمالي", small, Brushes.Black, new RectangleF(colTotG, y + 1, wTotGrid, 16), alignCenter);
                        g.DrawString("السعر", small, Brushes.Black, new RectangleF(colPriceG, y + 1, wPriceGrid, 16), alignCenter);
                        g.DrawString("الكمية", small, Brushes.Black, new RectangleF(colQtyG, y + 1, wQtyGrid, 16), alignCenter);
                        g.DrawString("الصنف", small, Brushes.Black, new RectangleF(colNameG, y + 1, wNameGrid - 4, 16), right);
                        y += 20;

                        g.DrawLine(Pens.Black, lMargin, headY, pageW - rMargin, headY); // top
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); // bottom
                        g.DrawLine(Pens.Black, colTotG, headY, colTotG, y);
                        g.DrawLine(Pens.Black, colPriceG, headY, colPriceG, y);
                        g.DrawLine(Pens.Black, colQtyG, headY, colQtyG, y);
                        g.DrawLine(Pens.Black, colNameG, headY, colNameG, y);
                        g.DrawLine(Pens.Black, pageW - rMargin, headY, pageW - rMargin, y);

                        y += 4;

                        if (_items != null)
                        {
                            while (_printItemIndex < _items.Rows.Count)
                            {
                                if (y + 35 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }

                                DataRow r = _items.Rows[_printItemIndex];
                                string prodName = r["ProductName"].ToString();
                                decimal qty = Convert.ToDecimal(r["Quantity"]);
                                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                                decimal tot = Convert.ToDecimal(r["TotalPrice"]);

                                int itemY = y - 2;

                                g.DrawString(prodName, small, Brushes.Black, new RectangleF(colNameG, y, wNameGrid - 4, 16), right);
                                g.DrawString(qty.ToString("0.##"), small, Brushes.Black, new RectangleF(colQtyG, y, wQtyGrid, 16), alignCenter);
                                g.DrawString(price.ToString("N2"), small, Brushes.Black, new RectangleF(colPriceG, y, wPriceGrid, 16), alignCenter);
                                g.DrawString(tot.ToString("N2"), small, Brushes.Black, new RectangleF(colTotG, y, wTotGrid, 16), alignCenter);
                                y += 18;

                                g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); // row separator
                                g.DrawLine(Pens.Black, colTotG, itemY, colTotG, y);
                                g.DrawLine(Pens.Black, colPriceG, itemY, colPriceG, y);
                                g.DrawLine(Pens.Black, colQtyG, itemY, colQtyG, y);
                                g.DrawLine(Pens.Black, colNameG, itemY, colNameG, y);
                                g.DrawLine(Pens.Black, pageW - rMargin, itemY, pageW - rMargin, y);

                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        e.HasMorePages = false;
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); y += 6;
                    }
                    else if (string.Equals(template, "FancyReceipt", StringComparison.OrdinalIgnoreCase))
                    {
                        // FancyReceipt: Ornamented borders, elegant labels and divider lines
                        g.DrawString("*** " + AppConfig.CompanyName + " ***", boldBig, Brushes.DarkBlue, new RectangleF(lMargin, y, printableW, 25), center); y += 24;
                        if (!string.IsNullOrWhiteSpace(AppConfig.ReceiptHeaderNote))
                        {
                            var headerSize = g.MeasureString(AppConfig.ReceiptHeaderNote, normal, printableW);
                            g.DrawString(AppConfig.ReceiptHeaderNote, normal, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, headerSize.Height + 2), center);
                            y += (int)headerSize.Height + 4;
                        }
                        g.DrawString("==========================", small, Brushes.DarkBlue, new RectangleF(lMargin, y, printableW, 14), center); y += 14;
                        g.DrawString("فاتورة مبيعات", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), center); y += 20;

                        if (_saleRow != null)
                        {
                            g.DrawString($"رقم المستند: {_saleRow["SaleCode"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            g.DrawString($"التاريخ والوقت: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            g.DrawString($"العميل: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 18;
                        }
                        g.DrawString("--------------------------", small, Brushes.Gray, new RectangleF(lMargin, y, printableW, 14), center); y += 14;

                        if (_items != null)
                        {
                            while (_printItemIndex < _items.Rows.Count)
                            {
                                if (y + 40 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }

                                DataRow r = _items.Rows[_printItemIndex];
                                string prodName = r["ProductName"].ToString();
                                decimal qty = Convert.ToDecimal(r["Quantity"]);
                                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                                decimal tot = Convert.ToDecimal(r["TotalPrice"]);

                                g.DrawString(prodName, bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right);
                                y += 16;
                                g.DrawString($"{qty:0.##} x {price:N2} = {tot:N2} ج.م", normal, Brushes.DarkSlateGray, new RectangleF(lMargin, y, printableW, 14), right);
                                y += 16;

                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        e.HasMorePages = false;
                        g.DrawString("--------------------------", small, Brushes.Gray, new RectangleF(lMargin, y, printableW, 14), center); y += 14;
                    }
                    else
                    {
                        // ==========================================
                        // STANDARD (DEFAULT) LAYOUT — جدول أعمدة واضح مع خطوط طول وعرض
                        // ==========================================
                        g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(lMargin, y, printableW, 25), center); y += 22;
                        if (!string.IsNullOrWhiteSpace(AppConfig.ReceiptHeaderNote))
                        {
                            var headerSize = g.MeasureString(AppConfig.ReceiptHeaderNote, normal, printableW);
                            g.DrawString(AppConfig.ReceiptHeaderNote, normal, Brushes.DimGray, new RectangleF(lMargin, y, printableW, headerSize.Height + 2), center);
                            y += (int)headerSize.Height + 4;
                        }
                        g.DrawString("فاتورة مبيعات", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), center); y += 20;
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); y += 6;

                        if (_saleRow != null)
                        {
                            g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 16;
                            g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 16;

                            string typeLabel = FormatSaleType(_saleRow["SaleType"]?.ToString());
                            string driverText = _saleRow["DriverName"].ToString() != "---" ? $" | مندوب: {_saleRow["DriverName"]}" : "";
                            g.DrawString($"العميل: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 16;
                            g.DrawString($"طريقة الدفع: {typeLabel}{driverText}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 16;

                            // بيانات العميل الإضافية (هاتف + عنوان)
                            if (AppConfig.ReceiptShowClientInfo)
                            {
                                string phone = _saleRow.Table.Columns.Contains("ClientPhone") ? _saleRow["ClientPhone"].ToString() : "";
                                string addr  = _saleRow.Table.Columns.Contains("ClientAddress") ? _saleRow["ClientAddress"].ToString() : "";
                                if (!string.IsNullOrEmpty(phone))
                                { g.DrawString($"الهاتف: {phone}", normal, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 16), right); y += 15; }
                                if (!string.IsNullOrEmpty(addr))
                                { g.DrawString($"العنوان: {addr}", normal, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 16), right); y += 15; }
                            }
                            y += 2;
                        }
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); y += 4;

                        // ─── جدول رأس الأعمدة ──────────────────────────────────────
                        // توزيع العرض (من اليسار إلى اليمين RTL):
                        // [  الإجمالي ] [ خصم  ] [ سعر  ] [ كمية ] [    اسم الصنف     ]
                        //    55px       40px     50px     35px        (remainder)
                        // ─── جدول رأس الأعمدة ──────────────────────────────────────
                        // توزيع العرض (من اليسار إلى اليمين RTL):
                        // [  الإجمالي ] [ خصم  ] [ سعر  ] [ كمية ] [    اسم الصنف     ]
                        //    52px       35px     46px     30px        (remainder)
                        bool showDisc = AppConfig.ReceiptShowDiscount;
                        int wTot    = 52;
                        int wDisc   = showDisc ? 35 : 0;
                        int wPrice  = 46;
                        int wQty    = 30;
                        int wName   = printableW - wTot - wDisc - wPrice - wQty;

                        int colTot   = lMargin;
                        int colDisc  = colTot + wTot;
                        int colPrice = colDisc + wDisc;
                        int colQty   = colPrice + wPrice;
                        int colName  = colQty + wQty;

                        // لون رأس الجدول
                        int headerY = y - 2;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 45)), lMargin, headerY, printableW, 20);
                        var hdr = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString("الإجمالي", small, Brushes.White, new RectangleF(colTot,   y + 1, wTot,   16), hdr);
                        if (showDisc)
                            g.DrawString("خصم",     small, Brushes.White, new RectangleF(colDisc,  y + 1, wDisc,  16), hdr);
                        g.DrawString("سعر",      small, Brushes.White, new RectangleF(colPrice, y + 1, wPrice, 16), hdr);
                        g.DrawString("كمية",    small, Brushes.White, new RectangleF(colQty,   y + 1, wQty,   16), hdr);
                        g.DrawString("الصنف",   small, Brushes.White, new RectangleF(colName,  y + 1, wName - 4,  16), right);
                        y += 20;

                        // Draw header borders
                        g.DrawLine(Pens.Black, lMargin, headerY, pageW - rMargin, headerY); // top
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); // bottom
                        g.DrawLine(Pens.Black, colTot, headerY, colTot, y); // left border
                        if (showDisc) g.DrawLine(Pens.Black, colDisc, headerY, colDisc, y);
                        g.DrawLine(Pens.Black, colPrice, headerY, colPrice, y);
                        g.DrawLine(Pens.Black, colQty, headerY, colQty, y);
                        g.DrawLine(Pens.Black, colName, headerY, colName, y);
                        g.DrawLine(Pens.Black, pageW - rMargin, headerY, pageW - rMargin, y); // right border

                        y += 4;

                        if (_items != null)
                        {
                            while (_printItemIndex < _items.Rows.Count)
                            {
                                DataRow r = _items.Rows[_printItemIndex];
                                string prodName = r["ProductName"].ToString();
                                decimal qty   = Convert.ToDecimal(r["Quantity"]);
                                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                                decimal tot   = Convert.ToDecimal(r["TotalPrice"]);
                                decimal itemDisc    = r.Table.Columns.Contains("DiscountAmt")  && r["DiscountAmt"]  != DBNull.Value ? Convert.ToDecimal(r["DiscountAmt"])  : 0m;
                                decimal itemDiscPct = r.Table.Columns.Contains("DiscountPct")  && r["DiscountPct"]  != DBNull.Value ? Convert.ToDecimal(r["DiscountPct"])  : 0m;

                                // حساب ارتفاع السطر ديناميكياً بناءً على عدد أسطر اسم الصنف
                                SizeF nameSize = g.MeasureString(prodName, small, (int)(wName - 4));
                                int rowHeight = Math.Max(18, (int)Math.Ceiling(nameSize.Height) + 4);

                                if (y + rowHeight + 20 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }

                                int itemY = y - 2;
                                float textY = y + (rowHeight - 14) / 2f;

                                // رسم الاسم (RTL — يمتد ديناميكياً حسـب عدد الأسطر)
                                g.DrawString(prodName, small, Brushes.Black, new RectangleF(colName, y, wName - 4, rowHeight), right);
                                // كمية
                                g.DrawString(qty.ToString("0.##"), small, Brushes.Black, new RectangleF(colQty, textY, wQty, 14), hdr);
                                // سعر الوحدة
                                g.DrawString(price.ToString("N2"), small, Brushes.Black, new RectangleF(colPrice, textY, wPrice, 14), hdr);
                                // خصم
                                if (showDisc)
                                {
                                    string discTxt = itemDiscPct > 0 ? $"{itemDiscPct:0.#}%" : (itemDisc > 0 ? itemDisc.ToString("0.##") : "-");
                                    g.DrawString(discTxt, small, Brushes.DimGray, new RectangleF(colDisc, textY, wDisc, 14), hdr);
                                }
                                // إجمالي
                                g.DrawString(tot.ToString("N2"), small, Brushes.Black, new RectangleF(colTot, textY, wTot, 14), hdr);
                                y += rowHeight;

                                // Draw item row borders
                                g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); // bottom line
                                g.DrawLine(Pens.Black, colTot, itemY, colTot, y); // left border
                                if (showDisc) g.DrawLine(Pens.Black, colDisc, itemY, colDisc, y);
                                g.DrawLine(Pens.Black, colPrice, itemY, colPrice, y);
                                g.DrawLine(Pens.Black, colQty, itemY, colQty, y);
                                g.DrawLine(Pens.Black, colName, itemY, colName, y);
                                g.DrawLine(Pens.Black, pageW - rMargin, itemY, pageW - rMargin, y); // right border

                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }

                        e.HasMorePages = false;
                        g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y); y += 6;
                    }
                    
                    // ==========================================
                    // COMMON TOTALS AND FOOTER SECTION FOR RECEIPTS
                    // ==========================================
                    decimal invDiscountAmt = 0;
                    decimal invDiscountPct = 0;
                    decimal netAmount = _runningTotal;
                    decimal shippingAmt = 0;
                    if (_saleRow != null)
                    {
                        invDiscountAmt = _saleRow.Table.Columns.Contains("DiscountAmount") && _saleRow["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountAmount"]) : 0m;
                        invDiscountPct = _saleRow.Table.Columns.Contains("DiscountPct") && _saleRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountPct"]) : 0m;
                        shippingAmt = _saleRow.Table.Columns.Contains("ShippingCharge") && _saleRow["ShippingCharge"] != DBNull.Value ? Convert.ToDecimal(_saleRow["ShippingCharge"]) : 0m;
                        netAmount = Convert.ToDecimal(_saleRow["TotalAmount"]);
                    }

                    if (invDiscountAmt > 0 || shippingAmt > 0)
                    {
                        g.DrawString($"إجمالي الأصناف: {_runningTotal:N2}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                    }
                    if (invDiscountAmt > 0)
                    {
                        g.DrawString($"خصم الفاتورة: -{invDiscountAmt:N2} ({invDiscountPct:0.##}%)", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                    }
                    if (shippingAmt > 0)
                    {
                        g.DrawString($"خدمة الشحن: +{shippingAmt:N2}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                    }

                    g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه", boldBig, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), right); y += 22;

                    bool isReceiptCredit = _saleRow["SaleType"].ToString() == "Credit";
                    decimal receiptCashPaid = _saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(_saleRow["CashPaid"]) : (isReceiptCredit ? 0m : netAmount);
                    decimal receiptRemaining = netAmount - receiptCashPaid;

                    if (_saleRow["SaleType"].ToString() == "Cash")
                    {
                        g.DrawString($"المدفوع نقداً: {receiptCashPaid:N2} جنيه", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 18;
                        if (receiptRemaining > 0)
                        {
                            g.DrawString($"المتبقي (آجل): {receiptRemaining:N2} جنيه", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 18;
                            g.DrawString("(سيتم إضافة المتبقي على حساب العميل)", small, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 14), right); y += 15;
                        }
                        else if (receiptRemaining < 0)
                        {
                            g.DrawString($"الزيادة: {-receiptRemaining:N2} جنيه", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 18;
                            g.DrawString("(سيتم خصم الزيادة من حساب العميل)", small, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 14), right); y += 15;
                        }
                        else
                        {
                            g.DrawString("(تم سداد الفاتورة بالكامل)", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                        }
                    }

                    // Client Balance (Skipped in Compact)
                    if (!string.Equals(template, "Compact", StringComparison.OrdinalIgnoreCase) && detailedPrint && _saleRow != null && _saleRow["ClientID"] != DBNull.Value)
                    {
                        int clientID = Convert.ToInt32(_saleRow["ClientID"]);
                        if (clientID > 0)
                        {
                            int saleID = Convert.ToInt32(_saleRow["SaleID"]);
                            decimal currentBalance = 0;
                            decimal paymentToday = 0;
                            decimal returnToday = 0;
                            decimal previousBalance = ClientDAL.GetPreviousBalanceBeforeSale(clientID, saleID);

                            DateTime saleDate = Convert.ToDateTime(_saleRow["SaleDate"]);

                            int saleTransID = 0;
                            var dtTrans = DbHelper.Query(@"
                                SELECT TOP 1 TransID 
                                FROM ClientTransactions 
                                WHERE ClientID = @cid AND TransType = 'Sale' AND RefID = @sid 
                                ORDER BY TransID DESC",
                                DbHelper.P("@cid", clientID), DbHelper.P("@sid", saleID));
                            if (dtTrans.Rows.Count > 0)
                            {
                                saleTransID = Convert.ToInt32(dtTrans.Rows[0]["TransID"]);
                            }

                            var dtPay = DbHelper.Query(@"
                                SELECT 
                                    COALESCE(SUM(CASE WHEN TransType = 'Payment' THEN Credit ELSE 0 END), 0) AS TotalPayment,
                                    COALESCE(SUM(CASE WHEN TransType = 'Return' THEN Credit ELSE 0 END), 0) AS TotalReturn
                                FROM ClientTransactions
                                WHERE ClientID = @cid 
                                  AND CAST(TransDate AS DATE) = CAST(@dt AS DATE)
                                  AND TransID >= @saleTransID
                                  AND NOT (RefID = @sid AND TransType = 'Payment')",
                                DbHelper.P("@cid", clientID), 
                                DbHelper.P("@dt", saleDate),
                                DbHelper.P("@saleTransID", saleTransID),
                                DbHelper.P("@sid", saleID));
                            if (dtPay.Rows.Count > 0)
                            {
                                paymentToday = Convert.ToDecimal(dtPay.Rows[0]["TotalPayment"]);
                                returnToday  = Convert.ToDecimal(dtPay.Rows[0]["TotalReturn"]);
                            }

                            decimal remainingFromInvoice = netAmount - receiptCashPaid;
                            currentBalance = previousBalance + remainingFromInvoice - paymentToday - returnToday;

                            g.DrawLine(Pens.LightGray, lMargin, y, pageW - rMargin, y); y += 6;
                            g.DrawString($"الرصيد السابق: {previousBalance:N2}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            if (returnToday > 0)
                            {
                                g.DrawString($"المرتجع اليوم: {returnToday:N2}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            }
                            g.DrawString($"المدفوع (التحصيل): {paymentToday:N2}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                            g.DrawString($"الرصيد الحالي: {currentBalance:N2}", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 20;

                            if (AppConfig.EnableCratesTracking)
                            {
                                int cratesOut = _saleRow.Table.Columns.Contains("CratesOut") && _saleRow["CratesOut"] != DBNull.Value ? Convert.ToInt32(_saleRow["CratesOut"]) : 0;
                                int cratesIn = _saleRow.Table.Columns.Contains("CratesIn") && _saleRow["CratesIn"] != DBNull.Value ? Convert.ToInt32(_saleRow["CratesIn"]) : 0;
                                if (cratesOut > 0 || cratesIn > 0)
                                {
                                    string cratesText = "";
                                    if (cratesOut > 0) cratesText += $"صادر: {cratesOut} ";
                                    if (cratesIn > 0) cratesText += $"وارد: {cratesIn} ";
                                    g.DrawString($"الفوارغ بالفاتورة: {cratesText}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), right); y += 16;
                                }
                                int cratesBal = ClientDAL.GetClientCratesBalance(clientID);
                                g.DrawString($"رصيد الفوارغ الحالي: {cratesBal} فارغ", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right); y += 20;
                            }
                        }
                    }

                    g.DrawLine(Pens.LightGray, lMargin, y, pageW - rMargin, y); y += 6;
                    if (AppConfig.BusinessType == "Clothing")
                    {
                        g.DrawString("سياسة الاستبدال والاسترجاع:\nالاستبدال خلال 14 يوماً والاسترجاع خلال 7 أيام من تاريخ الفاتورة بشرط وجود تكت الملابس والفاتورة.", small, Brushes.Black, new RectangleF(lMargin, y, printableW, 32), center);
                        y += 34;
                        g.DrawLine(Pens.LightGray, lMargin, y, pageW - rMargin, y); y += 6;
                    }
                    // تذييل الفاتورة المخصص (ReceiptFooterNote)
                    string footerText = !string.IsNullOrWhiteSpace(AppConfig.ReceiptFooterNote)
                        ? AppConfig.ReceiptFooterNote
                        : (string.Equals(template, "Elegant", StringComparison.OrdinalIgnoreCase) 
                            ? "نشكركم لزيارتكم الكريمة وثقتكم بنا" 
                            : "شكراً لتعاملكم معنا");

                    var footerSize = g.MeasureString(footerText, small, printableW);
                    g.DrawString(footerText, small, Brushes.Black, new RectangleF(lMargin, y, printableW, footerSize.Height + 4), center);
                    y += (int)footerSize.Height + 4;

                    // عنوان الشركة ورقم الهاتف في أسفل الفاتورة
                    if (!string.IsNullOrWhiteSpace(AppConfig.CompanyAddress) || !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone))
                    {
                        g.DrawLine(Pens.LightGray, lMargin + 15, y, pageW - rMargin - 15, y); y += 4;
                        if (!string.IsNullOrWhiteSpace(AppConfig.CompanyAddress))
                        {
                            string addrText = "📍 " + AppConfig.CompanyAddress.Trim();
                            var addrSize = g.MeasureString(addrText, small, printableW);
                            g.DrawString(addrText, small, Brushes.Black, new RectangleF(lMargin, y, printableW, addrSize.Height + 2), center);
                            y += (int)addrSize.Height + 4;
                        }
                        if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone))
                        {
                            string phoneText = "📞 " + AppConfig.CompanyPhone.Trim();
                            g.DrawString(phoneText, small, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), center);
                            y += 18;
                        }
                    }
                }
                else
                {
                    // ==========================================
                    // STANDARD A4/A5 SHEET LAYOUT
                    // ==========================================
                    string a4Template = AppConfig.A4Template;
                    var boldBigSheet = new Font("Arial", isA4Page ? 16 : 14, FontStyle.Bold);
                    var boldSheet = new Font("Arial", isA4Page ? 11 : 10, FontStyle.Bold);

                    if (string.Equals(a4Template, "AlTarekGrid", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a4Template, "AlTarekHome", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a4Template, "AlTarekNoDiscount", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a4Template, "AlTarekHomeNoDiscount", StringComparison.OrdinalIgnoreCase))
                    {
                        // ════════════════════════════════════════════════════════════════════════
                        // AL TAREK HOME FULL GRID & BALANCE INVOICE TEMPLATE (نموذج الطارق هوم)
                        // ════════════════════════════════════════════════════════════════════════

                        // 1. Watermark Logo / Text
                        if (!string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                        {
                            try
                            {
                                using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                                {
                                    int w = 320;
                                    int h = (int)(img.Height * ((double)w / img.Width));
                                    int x = (pageW - w) / 2;
                                    int yWm = (e.PageBounds.Height - h) / 2;
                                    var cm = new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.10f };
                                    var ia = new System.Drawing.Imaging.ImageAttributes();
                                    ia.SetColorMatrix(cm);
                                    g.DrawImage(img, new Rectangle(x, yWm, w, h), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, ia);
                                }
                            }
                            catch { }
                        }

                        // 2. Yellow/Gold Polygon Accent Banner
                        using (var yellowBrush = new SolidBrush(Color.FromArgb(245, 158, 11)))
                        {
                            PointF[] yellowPoly = {
                                new PointF(pageW - margin - 250, margin),
                                new PointF(pageW - margin, margin),
                                new PointF(pageW - margin, margin + 45),
                                new PointF(pageW - margin - 190, margin + 45)
                            };
                            g.FillPolygon(yellowBrush, yellowPoly);
                        }
                        using (var darkPolyBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
                        {
                            PointF[] darkPoly = {
                                new PointF(pageW - margin - 270, margin),
                                new PointF(pageW - margin - 250, margin),
                                new PointF(pageW - margin - 190, margin + 45),
                                new PointF(pageW - margin - 210, margin + 45)
                            };
                            g.FillPolygon(darkPolyBrush, darkPoly);
                        }

                        // 3. Top Header Content (Brand Name)
                        if (y < 20) y = 20;

                        string compName = !string.IsNullOrEmpty(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة الطارق للاستيراد و التصدير";
                        string compAddr = !string.IsNullOrEmpty(AppConfig.CompanyAddress) ? AppConfig.CompanyAddress : "العنوان: البحيرة - إيتاي البارود";
                        string compPhone = !string.IsNullOrEmpty(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "موبايل: 01091800089";

                        g.DrawString(compName, boldBigSheet, Brushes.Black, new RectangleF(0, y, pageW - margin - 10, 24), right);
                        g.DrawString(compAddr, normal, Brushes.Black, new RectangleF(0, y + 24, pageW - margin - 10, 18), right);
                        g.DrawString(compPhone, normal, Brushes.Black, new RectangleF(0, y + 42, pageW - margin - 10, 18), right);

                        y += 66;

                        // 4. Customer Info Box & Metadata
                        if (_saleRow != null)
                        {
                            int boxW = pageW - 2 * margin;
                            int boxH = 56;
                            g.DrawRectangle(new Pen(Color.Black, 1.2f), margin, y, boxW, boxH);

                            string clientName = _saleRow["ClientName"]?.ToString() ?? "";
                            string phone = _saleRow.Table.Columns.Contains("ClientPhone") ? _saleRow["ClientPhone"].ToString() : "";
                            string addr = _saleRow.Table.Columns.Contains("ClientAddress") ? _saleRow["ClientAddress"].ToString() : "";
                            string saleCode = _saleRow["SaleCode"]?.ToString() ?? "";
                            string saleDateStr = Convert.ToDateTime(_saleRow["SaleDate"]).ToString("yyyy/MM/dd hh:mm tt");
                            string branchName = _saleRow.Table.Columns.Contains("WarehouseName") && _saleRow["WarehouseName"] != DBNull.Value
                                ? _saleRow["WarehouseName"].ToString()
                                : compName;

                            int halfW = boxW / 2;
                            int rightColX = margin + halfW;
                            int leftColX = margin + 10;

                            // Vertical subtle separator
                            g.DrawLine(Pens.LightGray, margin + halfW, y + 4, margin + halfW, y + boxH - 4);

                            // Right Column (Client Information) - RTL Right Aligned
                            g.DrawString($"اسم العميل :  {clientName}", boldSheet, Brushes.Black, new RectangleF(rightColX + 5, y + 6, halfW - 15, 20), right);

                            string addrDisplay = !string.IsNullOrEmpty(addr) ? addr : "الرئيسي";
                            string phoneDisplay = !string.IsNullOrEmpty(phone) ? $"   |   رقم الهاتف : {phone}" : "";
                            g.DrawString($"العنـوان :  {addrDisplay}{phoneDisplay}", normal, Brushes.Black, new RectangleF(rightColX + 5, y + 30, halfW - 15, 20), right);

                            // Left Column (Branch & Invoice Metadata) - LTR Left Aligned
                            g.DrawString($"الفرع / الحساب: {branchName}", normal, Brushes.Black, new RectangleF(leftColX, y + 6, halfW - 20, 20), left);
                            g.DrawString($"رقم : {saleCode}  |  {saleDateStr}", boldSheet, Brushes.Black, new RectangleF(leftColX, y + 30, halfW - 20, 20), left);

                            y += boxH + 8;
                        }
                    }
                    else if (string.Equals(a4Template, "CommercialGrid", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(a4Template, "AlRahmaGrid", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(a4Template, "CommercialGridNoDiscount", StringComparison.OrdinalIgnoreCase))
                    {
                        // ════════════════════════════════════════════════════════════════════════
                        // COMMERCIAL FULL GRID A4 TEMPLATE (نموذج بيان الأسعار والأجهزة الكهربائية)
                        // ════════════════════════════════════════════════════════════════════════
                        if (y < 20) y = 20;

                        string compName = !string.IsNullOrEmpty(AppConfig.CompanyName) ? AppConfig.CompanyName : "الرحمة جروب لتجارة الأجهزة الكهربائية والأدوات المنزلية";
                        string compPhone = !string.IsNullOrEmpty(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "01070909181 - 01070909185";

                        g.DrawString(compName, boldBigSheet, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, isA4Page ? 28 : 24), center);
                        y += isA4Page ? 28 : 24;

                        if (!string.IsNullOrEmpty(compPhone))
                        {
                            g.DrawString(compPhone, boldSheet, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin - 10, 20), right);
                        }

                        if (!string.IsNullOrEmpty(AppConfig.CompanyAddress))
                        {
                            g.DrawString(AppConfig.CompanyAddress, boldSheet, Brushes.DarkSlateGray, new RectangleF(margin, y, pageW - 2 * margin, 20), center);
                        }
                        y += isA4Page ? 24 : 22;

                        g.DrawLine(new Pen(Color.Black, 1.2f), margin, y, pageW - margin, y);
                        y += isA4Page ? 10 : 8;

                        if (_saleRow != null)
                        {
                            string clientName = _saleRow["ClientName"]?.ToString() ?? "";
                            string saleCode = _saleRow["SaleCode"]?.ToString() ?? "";
                            string saleDateStr = Convert.ToDateTime(_saleRow["SaleDate"]).ToString("yyyy/MM/dd");
                            string userName = _saleRow.Table.Columns.Contains("DriverName") && _saleRow["DriverName"].ToString() != "---" 
                                ? _saleRow["DriverName"].ToString() 
                                : (!string.IsNullOrEmpty(Session.UserName) ? Session.UserName : (!string.IsNullOrEmpty(Session.EmpName) ? Session.EmpName : "المسؤول"));

                            int metaHalfW = (pageW - 2 * margin) / 2;
                            g.DrawString($"اسم العميل /  {clientName}", boldSheet, Brushes.Black, new RectangleF(margin + metaHalfW, y, metaHalfW, 22), right);
                            g.DrawString($"المستخدم /  {userName}", normal, Brushes.Black, new RectangleF(margin, y, metaHalfW, 22), right);
                            y += isA4Page ? 24 : 20;

                            g.DrawString($"رقم البيان :  {saleCode}", boldSheet, Brushes.Black, new RectangleF(margin + metaHalfW, y, metaHalfW, 22), right);
                            g.DrawString($"تاريخ البيان :  {saleDateStr}", normal, Brushes.Black, new RectangleF(margin, y, metaHalfW, 22), right);
                            y += isA4Page ? 26 : 24;
                        }
                    }
                    else if (string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase))
                    {
                        // Modern minimalist header
                        g.DrawString(AppConfig.CompanyName, boldBigSheet, Brushes.DarkSlateGray, margin, y);
                        g.DrawString("فاتورة مبيعات", boldBigSheet, Brushes.Black, new RectangleF(0, y, pageW - margin, 30), right);
                        y += 32;
                        g.DrawLine(new Pen(Color.DarkSlateGray, 1.5f), margin, y, pageW - margin, y);
                        y += 8;

                        if (_saleRow != null)
                        {
                            string typeLabel = _saleRow["SaleType"].ToString() == "Credit" ? "آجل" : "نقدي";
                            string driverText = _saleRow["DriverName"].ToString() != "---" ? $" | المندوب: {_saleRow["DriverName"]}" : "";
                            
                            g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy}  |  رقم الفاتورة: {_saleRow["SaleCode"]}", normal, Brushes.Black, margin, y);
                            g.DrawString($"العميل: {_saleRow["ClientName"]}{driverText} ({typeLabel})", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 24;
                        }
                        g.DrawLine(new Pen(Color.LightGray, 1f), margin, y, pageW - margin, y);
                        y += 10;
                    }
                    else if (string.Equals(a4Template, "SparePartsGrid", StringComparison.OrdinalIgnoreCase))
                    {
                        // Industrial Spare Parts Header
                        g.DrawString("فاتورة بيع قطع غيار ومستلزمات", boldBigSheet, Brushes.SteelBlue, new RectangleF(0, y, pageW, 28), center); y += 28;
                        g.DrawString(AppConfig.CompanyName, boldSheet, Brushes.Black, new RectangleF(0, y, pageW, 20), center); y += 22;
                        g.DrawLine(new Pen(Color.SteelBlue, 2f), margin, y, pageW - margin, y); y += 10;

                        if (_saleRow != null)
                        {
                            g.DrawString($"كود الفاتورة: {_saleRow["SaleCode"]}", boldSheet, Brushes.Black, margin, y);
                            g.DrawString($"تاريخ البيع: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy HH:mm}", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 20;
                            g.DrawString($"العميل (الحساب): {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 25;
                        }
                    }
                    else if (string.Equals(a4Template, "SupermarketA4", StringComparison.OrdinalIgnoreCase))
                    {
                        // Supermarket Header
                        g.DrawString("فاتورة مبيعات التجزئة والماركت", boldBigSheet, Brushes.OliveDrab, new RectangleF(0, y, pageW, 28), center); y += 28;
                        g.DrawString(AppConfig.CompanyName, boldSheet, Brushes.Black, new RectangleF(0, y, pageW, 20), center); y += 22;
                        g.DrawLine(new Pen(Color.OliveDrab, 1.5f), margin, y, pageW - margin, y); y += 10;

                        if (_saleRow != null)
                        {
                            string payType = FormatSaleType(_saleRow["SaleType"]?.ToString());
                            g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", normal, Brushes.Black, margin, y);
                            g.DrawString($"تاريخ المعاملة: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy HH:mm}", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 20;
                            g.DrawString($"العميل: {_saleRow["ClientName"]}  ({payType})", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 25;
                        }
                    }
                    else if (string.Equals(a4Template, "Official", StringComparison.OrdinalIgnoreCase))
                    {
                        // Official header
                        g.DrawString(AppConfig.CompanyName, boldBigSheet, Brushes.Black, new RectangleF(0, y, pageW, 25), center);
                        y += 25;
                        g.DrawString("فاتورة مبيعات رسمية", boldSheet, Brushes.DarkSlateGray, new RectangleF(0, y, pageW, 20), center);
                        y += 22;

                        if (_saleRow != null)
                        {
                            // Draw structured metadata box
                            g.DrawRectangle(new Pen(Color.Black, 1f), margin, y, pageW - 2 * margin, 50);
                            g.DrawLine(new Pen(Color.Black, 1f), margin + 270, y, margin + 270, y + 50);
                            g.DrawLine(new Pen(Color.Black, 1f), margin, y + 25, pageW - margin, y + 25);

                            string typeLabel = FormatSaleType(_saleRow["SaleType"]?.ToString());
                            string driverText = _saleRow["DriverName"].ToString() != "---" ? _saleRow["DriverName"].ToString() : "---";

                            // Top-Right: Client
                            g.DrawString($"العميل: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(margin + 275, y + 4, 230, 18), right);
                            // Bottom-Right: Driver
                            g.DrawString($"المندوب: {driverText}", normal, Brushes.Black, new RectangleF(margin + 275, y + 29, 230, 18), right);
                            // Top-Left: Code
                            g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", normal, Brushes.Black, new RectangleF(margin + 5, y + 4, 250, 18), right);
                            // Bottom-Left: Date
                            g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(margin + 5, y + 29, 250, 18), right);
                            y += 60;
                        }
                    }
                    else if (string.Equals(a4Template, "Simple", StringComparison.OrdinalIgnoreCase))
                    {
                        // Simple header
                        g.DrawString(AppConfig.CompanyName, boldBigSheet, Brushes.Black, margin, y);
                        g.DrawString("فاتورة مبيعات", boldSheet, Brushes.Black, new RectangleF(0, y + 4, pageW - margin, 20), right);
                        y += 25;
                        g.DrawLine(Pens.Black, margin, y, pageW - margin, y);
                        y += 8;

                        if (_saleRow != null)
                        {
                            string typeLabel = FormatSaleType(_saleRow["SaleType"]?.ToString());
                            g.DrawString($"فاتورة رقم: {_saleRow["SaleCode"]} | تاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy} | العميل: {_saleRow["ClientName"]} ({typeLabel})", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right);
                            y += 18;
                        }
                        g.DrawLine(Pens.Black, margin, y, pageW - margin, y);
                        y += 8;
                    }
                    else
                    {
                        // Classic Standard Blue
                        g.DrawString("فاتورة مبيعات", boldBigSheet, Brushes.DarkBlue, new RectangleF(0, y, pageW, 30), center); y += 30;
                        g.DrawString(AppConfig.CompanyName, boldSheet, Brushes.Black, new RectangleF(0, y, pageW, 22), center); y += 25;
                        g.DrawLine(new Pen(Color.DarkBlue, 2), margin, y, pageW - margin, y); y += 10;

                        if (_saleRow != null)
                        {
                            g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy}", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", normal, Brushes.Black, margin, y);
                            y += 20;

                            string typeLabel = FormatSaleType(_saleRow["SaleType"]?.ToString());
                            string driverText = _saleRow["DriverName"].ToString() != "---" ? $"  |  المندوب: {_saleRow["DriverName"]}" : "";
                            g.DrawString($"العميل: {_saleRow["ClientName"]}{driverText}   |   النوع: {typeLabel}", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 25;
                        }
                    }

                    // ===== Table Header =====
                    bool isCommercial = string.Equals(a4Template, "CommercialGrid", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(a4Template, "AlRahmaGrid", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(a4Template, "CommercialGridNoDiscount", StringComparison.OrdinalIgnoreCase);
                    bool isAlTarek = string.Equals(a4Template, "AlTarekGrid", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(a4Template, "AlTarekHome", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(a4Template, "AlTarekNoDiscount", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(a4Template, "AlTarekHomeNoDiscount", StringComparison.OrdinalIgnoreCase);

                    bool hideDiscountCol = string.Equals(a4Template, "AlTarekNoDiscount", StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(a4Template, "AlTarekHomeNoDiscount", StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(a4Template, "CommercialGridNoDiscount", StringComparison.OrdinalIgnoreCase);

                    int xNotes, colWNotes, xTotal, colWTotal, xDiscount, colWDiscount, xPrice, colWPrice, xQty, colWQty, xUnit, colWUnit, xIndex, colWIndex, xCode, colWCode, xName, colWName, xProduct, colWProduct;
                    int xWh = 0, colWWh = 0;

                    if (isCommercial || isAlTarek)
                    {
                        if (isA4Page)
                        {
                            colWIndex    = 36;
                            colWCode     = 78;
                            colWUnit     = 65;
                            colWQty      = 75;
                            colWPrice    = 90;
                            colWDiscount = hideDiscountCol ? 0 : 65;
                            colWTotal    = 105;
                            colWNotes    = 85;

                            xNotes       = margin;
                            xTotal       = xNotes + colWNotes;
                            xDiscount    = hideDiscountCol ? xTotal : (xTotal + colWTotal);
                            xPrice       = hideDiscountCol ? (xTotal + colWTotal) : (xDiscount + colWDiscount);
                            xQty         = xPrice + colWPrice;
                            xUnit        = xQty + colWQty;
                            xIndex       = pageW - margin - colWIndex;
                            xCode        = xIndex - colWCode;
                            xName        = xUnit + colWUnit;
                            colWName     = xCode - xName;
                            xProduct     = xName;
                            colWProduct  = colWName;
                        }
                        else if (hideDiscountCol)
                        {
                            colWIndex    = 26;
                            colWCode     = 58;
                            colWUnit     = 45;
                            colWQty      = 50;
                            colWPrice    = 70;
                            colWDiscount = 0;
                            colWTotal    = 80;
                            colWNotes    = 65;

                            xNotes       = margin;
                            xTotal       = xNotes + colWNotes;
                            xDiscount    = xTotal;
                            xPrice       = xTotal + colWTotal;
                            xQty         = xPrice + colWPrice;
                            xUnit        = xQty + colWQty;
                            xIndex       = pageW - margin - colWIndex;
                            xCode        = xIndex - colWCode;
                            xName        = xUnit + colWUnit;
                            colWName     = xCode - xName;
                            xProduct     = xName;
                            colWProduct  = colWName;
                        }
                        else
                        {
                            colWIndex    = 26;
                            colWCode     = 58;
                            colWUnit     = 45;
                            colWQty      = 48;
                            colWPrice    = 65;
                            colWDiscount = 45;
                            colWTotal    = 75;
                            colWNotes    = 65;

                            xNotes       = margin;
                            xTotal       = xNotes + colWNotes;
                            xDiscount    = xTotal + colWTotal;
                            xPrice       = xDiscount + colWDiscount;
                            xQty         = xPrice + colWPrice;
                            xUnit        = xQty + colWQty;
                            xIndex       = pageW - margin - colWIndex;
                            xCode        = xIndex - colWCode;
                            xName        = xUnit + colWUnit;
                            colWName     = xCode - xName;
                            xProduct     = xName;
                            colWProduct  = colWName;
                        }
                    }
                    else
                    {
                        xNotes       = margin;
                        colWNotes    = isA4Page ? 130 : 95;
                        xWh          = xNotes + colWNotes;
                        colWWh       = isA4Page ? 150 : 110;
                        xTotal       = xWh + colWWh;
                        colWTotal    = isA4Page ? 100 : 75;
                        xPrice       = xTotal + colWTotal;
                        colWPrice    = isA4Page ? 90 : 65;
                        xQty         = xPrice + colWPrice;
                        colWQty      = isA4Page ? 75 : 55;
                        xDiscount    = xTotal;
                        colWDiscount = 0;
                        xIndex       = pageW - margin - (isA4Page ? 36 : 28);
                        colWIndex    = isA4Page ? 36 : 28;
                        xCode        = xIndex;
                        colWCode     = 0;
                        xUnit        = xQty;
                        colWUnit     = 0;
                        xName        = xQty + colWQty;
                        colWName     = xIndex - xName;
                        xProduct     = xName;
                        colWProduct  = colWName;
                    }

                    int tblHeaderH = isA4Page ? 28 : 24;
                    if (isCommercial || isAlTarek)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(226, 232, 240)), margin, y, pageW - 2 * margin, tblHeaderH);
                        g.DrawRectangle(Pens.Black, margin, y, pageW - 2 * margin, tblHeaderH);

                        DrawColHeader(g, boldSheet, "م", xIndex, colWIndex, y + (isA4Page ? 5 : 3));
                        DrawColHeader(g, boldSheet, "الكود", xCode, colWCode, y + (isA4Page ? 5 : 3));
                        DrawColHeader(g, boldSheet, "اسم الصنف", xName, colWName, y + (isA4Page ? 5 : 3));
                        DrawColHeader(g, boldSheet, "الوحدة", xUnit, colWUnit, y + (isA4Page ? 5 : 3));
                        DrawColHeader(g, boldSheet, "الكمية", xQty, colWQty, y + (isA4Page ? 5 : 3));
                        DrawColHeader(g, boldSheet, "سعر البيع", xPrice, colWPrice, y + (isA4Page ? 5 : 3));
                        if (!hideDiscountCol)
                        {
                            DrawColHeader(g, boldSheet, "الخصم", xDiscount, colWDiscount, y + (isA4Page ? 5 : 3));
                        }
                        DrawColHeader(g, boldSheet, "إجمالي البيع", xTotal, colWTotal, y + (isA4Page ? 5 : 3));
                        DrawColHeader(g, boldSheet, "ملاحظات", xNotes, colWNotes, y + (isA4Page ? 5 : 3));
                        y += tblHeaderH;
                    }
                    else if (string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase))
                    {
                        // Charcoal filled header block
                        g.FillRectangle(Brushes.DarkSlateGray, margin, y, pageW - 2 * margin, 22);
                        
                        DrawColHeader(g, boldSheet, "الإجمالي", xTotal, colWTotal, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "الخصم", xDiscount, colWDiscount, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "السعر", xPrice, colWPrice, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "الكمية", xQty, colWQty, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "الصنف", xProduct, colWProduct, y + 2, Brushes.White);
                        y += 22;
                    }
                    else if (string.Equals(a4Template, "SparePartsGrid", StringComparison.OrdinalIgnoreCase))
                    {
                        // SteelBlue filled header block for SpareParts
                        g.FillRectangle(Brushes.SteelBlue, margin, y, pageW - 2 * margin, 24);
                        
                        DrawColHeader(g, boldSheet, "الإجمالي", xTotal, colWTotal, y + 3, Brushes.White);
                        DrawColHeader(g, boldSheet, "الخصم", xDiscount, colWDiscount, y + 3, Brushes.White);
                        DrawColHeader(g, boldSheet, "السعر", xPrice, colWPrice, y + 3, Brushes.White);
                        DrawColHeader(g, boldSheet, "الكمية", xQty, colWQty, y + 3, Brushes.White);
                        DrawColHeader(g, boldSheet, "بيان قطع الغيار والصنف", xProduct, colWProduct, y + 3, Brushes.White);
                        y += 24;
                    }
                    else if (string.Equals(a4Template, "SupermarketA4", StringComparison.OrdinalIgnoreCase))
                    {
                        // OliveDrab filled header block for Supermarket
                        g.FillRectangle(Brushes.OliveDrab, margin, y, pageW - 2 * margin, 22);
                        
                        DrawColHeader(g, boldSheet, "الإجمالي", xTotal, colWTotal, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "الخصم", xDiscount, colWDiscount, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "السعر", xPrice, colWPrice, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "الكمية", xQty, colWQty, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "المنتج", xProduct, colWProduct, y + 2, Brushes.White);
                        y += 22;
                    }
                    else if (string.Equals(a4Template, "Official", StringComparison.OrdinalIgnoreCase))
                    {
                        // Black filled header block
                        g.FillRectangle(Brushes.Black, margin, y, pageW - 2 * margin, 22);
                        
                        DrawColHeader(g, boldSheet, "الإجمالي", xTotal, colWTotal, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "الخصم", xDiscount, colWDiscount, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "السعر", xPrice, colWPrice, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "الكمية", xQty, colWQty, y + 2, Brushes.White);
                        DrawColHeader(g, boldSheet, "الصنف", xProduct, colWProduct, y + 2, Brushes.White);
                        y += 22;
                    }
                    else if (string.Equals(a4Template, "Simple", StringComparison.OrdinalIgnoreCase))
                    {
                        // Simple: plain text list
                        DrawColHeader(g, boldSheet, "الإجمالي", xTotal, colWTotal, y);
                        DrawColHeader(g, boldSheet, "الخصم", xDiscount, colWDiscount, y);
                        DrawColHeader(g, boldSheet, "السعر", xPrice, colWPrice, y);
                        DrawColHeader(g, boldSheet, "الكمية", xQty, colWQty, y);
                        DrawColHeader(g, boldSheet, "الصنف", xProduct, colWProduct, y);
                        y += 20;
                        g.DrawLine(Pens.Black, margin, y, pageW - margin, y);
                        y += 4;
                    }
                    else
                    {
                        // Classic Standard Blue
                        DrawColHeader(g, boldSheet, "الإجمالي", xTotal, colWTotal, y);
                        DrawColHeader(g, boldSheet, "الخصم", xDiscount, colWDiscount, y);
                        DrawColHeader(g, boldSheet, "السعر", xPrice, colWPrice, y);
                        DrawColHeader(g, boldSheet, "الكمية", xQty, colWQty, y);
                        DrawColHeader(g, boldSheet, "الصنف", xProduct, colWProduct, y);
                        y += 20;
                        g.DrawLine(Pens.Gray, margin, y, pageW - margin, y);
                        y += 4;
                    }

                    // ===== Items =====
                    if (_items != null)
                    {
                        while (_printItemIndex < _items.Rows.Count)
                        {
                            DataRow r = _items.Rows[_printItemIndex];
                            decimal qty   = Convert.ToDecimal(r["Quantity"]);
                            decimal price = Convert.ToDecimal(r["UnitPrice"]);
                            decimal tot   = Convert.ToDecimal(r["TotalPrice"]);
                            decimal itemDiscPct = r.Table.Columns.Contains("DiscountPct") && r["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(r["DiscountPct"]) : 0m;
                            decimal itemDiscAmt = r.Table.Columns.Contains("DiscountAmt") && r["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(r["DiscountAmt"]) : 0m;
                            
                            string discText = "-";
                            if (itemDiscPct > 0)
                                discText = $"{itemDiscPct:0.##}%";
                            else if (itemDiscAmt > 0)
                                discText = itemDiscAmt.ToString("F2");

                            string prodName = r["ProductName"]?.ToString() ?? "";

                            if (isCommercial || isAlTarek)
                            {
                                SizeF nameSize = g.MeasureString(prodName, normal, Math.Max(10, colWName - 4));
                                int minRowH = isA4Page ? 28 : 22;
                                int maxRowH = isA4Page ? 56 : 46;
                                int rowHeight = Math.Max(minRowH, (int)Math.Ceiling(nameSize.Height) + (isA4Page ? 6 : 4));
                                if (rowHeight > maxRowH) rowHeight = maxRowH;

                                if (y + rowHeight + 120 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }

                                _runningTotal += tot; 
                                _runningQtyTotal += qty;

                                g.DrawRectangle(Pens.Black, xIndex, y, colWIndex, rowHeight);
                                g.DrawRectangle(Pens.Black, xCode, y, colWCode, rowHeight);
                                g.DrawRectangle(Pens.Black, xName, y, colWName, rowHeight);
                                g.DrawRectangle(Pens.Black, xUnit, y, colWUnit, rowHeight);
                                g.DrawRectangle(Pens.Black, xQty, y, colWQty, rowHeight);
                                g.DrawRectangle(Pens.Black, xPrice, y, colWPrice, rowHeight);
                                if (!hideDiscountCol)
                                {
                                    g.DrawRectangle(Pens.Black, xDiscount, y, colWDiscount, rowHeight);
                                }
                                g.DrawRectangle(Pens.Black, xTotal, y, colWTotal, rowHeight);
                                g.DrawRectangle(Pens.Black, xNotes, y, colWNotes, rowHeight);

                                DrawColCell(g, normal, (_printItemIndex + 1).ToString(), xIndex, colWIndex, y, rowHeight, center);
                                string rawCode = r.Table.Columns.Contains("ProductCode") && r["ProductCode"] != DBNull.Value ? r["ProductCode"].ToString() : (r.Table.Columns.Contains("ProductID") ? r["ProductID"].ToString() : "");
                                string pCode = FormatProductCode(rawCode);
                                DrawColCell(g, normal, pCode, xCode, colWCode, y, rowHeight, center);

                                var nameSf = new StringFormat
                                {
                                    Alignment = StringAlignment.Far,
                                    LineAlignment = StringAlignment.Center,
                                    FormatFlags = StringFormatFlags.FitBlackBox
                                };
                                g.DrawString(prodName, normal, Brushes.Black, new RectangleF(xName + 2, y + 1, colWName - 4, rowHeight - 2), nameSf);

                                string unitStr = r.Table.Columns.Contains("UnitName") && !string.IsNullOrWhiteSpace(r["UnitName"]?.ToString()) ? r["UnitName"].ToString() : (r.Table.Columns.Contains("BaseUnitName") && !string.IsNullOrWhiteSpace(r["BaseUnitName"]?.ToString()) ? r["BaseUnitName"].ToString() : "قطعة");
                                DrawColCell(g, normal, unitStr, xUnit, colWUnit, y, rowHeight, center);
                                DrawColCell(g, normal, qty.ToString("N2"), xQty, colWQty, y, rowHeight, center);
                                DrawColCell(g, normal, price.ToString("N2"), xPrice, colWPrice, y, rowHeight, center);
                                if (!hideDiscountCol)
                                {
                                    DrawColCell(g, normal, discText, xDiscount, colWDiscount, y, rowHeight, center);
                                }
                                DrawColCell(g, normal, tot.ToString("N2"), xTotal, colWTotal, y, rowHeight, center);

                                string itemNotes = r.Table.Columns.Contains("Notes") && r["Notes"] != DBNull.Value ? r["Notes"].ToString() : "";
                                if (string.IsNullOrEmpty(itemNotes) && r.Table.Columns.Contains("KitchenNotes") && r["KitchenNotes"] != DBNull.Value) itemNotes = r["KitchenNotes"].ToString();
                                DrawColCell(g, normal, itemNotes, xNotes, colWNotes, y, rowHeight, center);
                                y += rowHeight;
                            }
                            else if (string.Equals(a4Template, "Official", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(a4Template, "SparePartsGrid", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(a4Template, "SupermarketA4", StringComparison.OrdinalIgnoreCase))
                            {
                                SizeF nameSize = g.MeasureString(prodName, normal, Math.Max(10, colWProduct - 4));
                                int rowHeight = Math.Max(18, (int)Math.Ceiling(nameSize.Height) + 4);
                                if (rowHeight > 42) rowHeight = 42;

                                if (y + rowHeight + 120 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }

                                _runningTotal += tot; 
                                _runningQtyTotal += qty;

                                Pen borderPen = string.Equals(a4Template, "SparePartsGrid", StringComparison.OrdinalIgnoreCase) ? new Pen(Color.SteelBlue, 1f) :
                                                string.Equals(a4Template, "SupermarketA4", StringComparison.OrdinalIgnoreCase) ? new Pen(Color.OliveDrab, 1f) :
                                                Pens.Black;

                                g.DrawRectangle(borderPen, xTotal, y, colWTotal, rowHeight);
                                if (colWDiscount > 0) g.DrawRectangle(borderPen, xDiscount, y, colWDiscount, rowHeight);
                                g.DrawRectangle(borderPen, xPrice, y, colWPrice, rowHeight);
                                g.DrawRectangle(borderPen, xQty, y, colWQty, rowHeight);
                                g.DrawRectangle(borderPen, xProduct, y, colWProduct, rowHeight);

                                DrawColCell(g, normal, tot.ToString("N2"),     xTotal,    colWTotal,    y, rowHeight, center);
                                if (colWDiscount > 0) DrawColCell(g, normal, discText, xDiscount, colWDiscount, y, rowHeight, center);
                                DrawColCell(g, normal, price.ToString("N2"),   xPrice,    colWPrice,    y, rowHeight, center);
                                DrawColCell(g, normal, qty.ToString("N2"),     xQty,      colWQty,      y, rowHeight, center);

                                var prodSf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.FitBlackBox };
                                g.DrawString(prodName, normal, Brushes.Black, new RectangleF(xProduct + 2, y + 1, colWProduct - 4, rowHeight - 2), prodSf);
                                y += rowHeight;
                            }
                            else
                            {
                                SizeF nameSize = g.MeasureString(prodName, normal, Math.Max(10, colWProduct - 4));
                                int rowHeight = Math.Max(18, (int)Math.Ceiling(nameSize.Height) + 4);
                                if (rowHeight > 42) rowHeight = 42;

                                if (y + rowHeight + 120 > e.PageBounds.Height)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }

                                _runningTotal += tot; 
                                _runningQtyTotal += qty;

                                g.DrawLine(Pens.LightGray, margin, y + rowHeight, pageW - margin, y + rowHeight);
                                if (colWDiscount > 0) g.DrawLine(Pens.LightGray, xDiscount, y, xDiscount, y + rowHeight);
                                g.DrawLine(Pens.LightGray, xPrice, y, xPrice, y + rowHeight);
                                g.DrawLine(Pens.LightGray, xQty, y, xQty, y + rowHeight);
                                g.DrawLine(Pens.LightGray, xProduct, y, xProduct, y + rowHeight);

                                DrawColCell(g, normal, tot.ToString("N2"),     xTotal,    colWTotal,    y, rowHeight, center);
                                if (colWDiscount > 0) DrawColCell(g, normal, discText, xDiscount, colWDiscount, y, rowHeight, center);
                                DrawColCell(g, normal, price.ToString("N2"),   xPrice,    colWPrice,    y, rowHeight, center);
                                DrawColCell(g, normal, qty.ToString("N2"),     xQty,      colWQty,      y, rowHeight, center);

                                var prodSf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.FitBlackBox };
                                g.DrawString(prodName, normal, Brushes.Black, new RectangleF(xProduct + 2, y + 1, colWProduct - 4, rowHeight - 2), prodSf);
                                y += rowHeight;
                            }

                            _printItemIndex++;
                        }
                    }

                    if (isCommercial || isAlTarek)
                    {
                        int sumRowH = isA4Page ? 26 : 22;
                        // Items Table Total Summary Row
                        g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), margin, y, pageW - 2 * margin, sumRowH);
                        g.DrawRectangle(Pens.Black, margin, y, pageW - 2 * margin, sumRowH);
                        g.DrawRectangle(Pens.Black, xQty, y, colWQty, sumRowH);
                        g.DrawRectangle(Pens.Black, xTotal, y, colWTotal, sumRowH);

                        g.DrawString(_runningQtyTotal.ToString("N2"), boldSheet, Brushes.Black, new RectangleF(xQty, y + (isA4Page ? 4 : 3), colWQty, sumRowH - 4), center);
                        g.DrawString(_runningTotal.ToString("N2"), boldSheet, Brushes.Black, new RectangleF(xTotal, y + (isA4Page ? 4 : 3), colWTotal, sumRowH - 4), center);
                        y += sumRowH + 6;
                    }

                    e.HasMorePages = false;

                    // ===== Totals section =====
                    decimal invDiscountAmt = 0;
                    decimal invDiscountPct = 0;
                    decimal netAmount = _runningTotal;
                    decimal shippingAmt = 0;
                    if (_saleRow != null)
                    {
                        invDiscountAmt = _saleRow.Table.Columns.Contains("DiscountAmount") && _saleRow["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountAmount"]) : 0m;
                        invDiscountPct = _saleRow.Table.Columns.Contains("DiscountPct") && _saleRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountPct"]) : 0m;
                        shippingAmt = _saleRow.Table.Columns.Contains("ShippingCharge") && _saleRow["ShippingCharge"] != DBNull.Value ? Convert.ToDecimal(_saleRow["ShippingCharge"]) : 0m;
                        netAmount = Convert.ToDecimal(_saleRow["TotalAmount"]);
                    }

                    if (isCommercial)
                    {
                        // ════════════════════════════════════════════════════════════════════════
                        // 2-COLUMN FINANCIAL SUMMARY TABLE & SIGNATURES (نموذج بيان الأسعار والأجهزة)
                        // ════════════════════════════════════════════════════════════════════════
                        int boxW = isA4Page ? 340 : 270;
                        int boxX = margin;
                        int boxRowH = isA4Page ? 26 : 22;
                        int labelColW = isA4Page ? 170 : 135;
                        int valColW = boxW - labelColW;
                        decimal discVal = invDiscountAmt > 0 ? invDiscountAmt : (invDiscountPct > 0 ? (_runningTotal * invDiscountPct / 100m) : 0m);

                        int clientID = (_saleRow != null && _saleRow["ClientID"] != DBNull.Value) ? Convert.ToInt32(_saleRow["ClientID"]) : 0;
                        decimal prevBal = 0m;
                        decimal curBal = 0m;
                        decimal paidAmt = (_saleRow != null && _saleRow["CashPaid"] != DBNull.Value) ? Convert.ToDecimal(_saleRow["CashPaid"]) : (_saleRow != null && _saleRow["SaleType"].ToString() == "Cash" ? netAmount : 0m);
                        decimal remainAmt = netAmount - paidAmt;

                        if (clientID > 0)
                        {
                            try
                            {
                                int saleID = Convert.ToInt32(_saleRow["SaleID"]);
                                prevBal = ClientDAL.GetPreviousBalanceBeforeSale(clientID, saleID);
                                curBal = prevBal + remainAmt;
                            }
                            catch { }
                        }

                        string[] sumLabels = { "الإجمالي قبل الخصم", "الخصم", "الإجمالي بعد الخصم", "المدفوع", "المتبقي", "الرصيد السابق", "الرصيد الحالي" };
                        string[] sumValues = {
                            _runningTotal.ToString("N2"),
                            discVal > 0 ? $"-{discVal:N2}" : "0.00",
                            netAmount.ToString("N2"),
                            paidAmt.ToString("N2"),
                            remainAmt.ToString("N2"),
                            prevBal.ToString("N2"),
                            curBal.ToString("N2")
                        };

                        g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), boxX, y, boxW, sumLabels.Length * boxRowH);
                        g.DrawRectangle(Pens.Black, boxX, y, boxW, sumLabels.Length * boxRowH);

                        for (int si = 0; si < sumLabels.Length; si++)
                        {
                            int rowY = y + si * boxRowH;
                            g.DrawRectangle(Pens.Black, boxX, rowY, boxW, boxRowH);
                            g.DrawLine(Pens.Black, boxX + valColW, rowY, boxX + valColW, rowY + boxRowH);

                            bool isBold = (si == 2 || si == 6);
                            var rowF = isBold ? boldSheet : normal;
                            Brush rowB = isBold ? Brushes.DarkBlue : Brushes.Black;

                            g.DrawString(sumLabels[si], boldSheet, Brushes.Black, new RectangleF(boxX + valColW + 2, rowY + 2, labelColW - 4, boxRowH - 4), right);
                            g.DrawString(sumValues[si], rowF, rowB, new RectangleF(boxX + 2, rowY + 2, valColW - 4, boxRowH - 4), center);
                        }

                        // Right side: notes or tafqeet
                        string tafStr = TafqeetHelper.ConvertToArabicWords(netAmount);
                        int rightNotesX = boxX + boxW + (isA4Page ? 20 : 15);
                        int rightNotesW = pageW - margin - rightNotesX;
                        if (rightNotesW > 80)
                        {
                            g.DrawString($"فقط وقدره: {tafStr}", boldSheet, Brushes.Black, new RectangleF(rightNotesX, y + 8, rightNotesW, isA4Page ? 50 : 40), right);
                        }

                        y += sumLabels.Length * boxRowH + (isA4Page ? 30 : 25);

                        // Signatures
                        if (y + 35 <= e.PageBounds.Height)
                        {
                            g.DrawString("توقيع المستلم: .......................................", boldSheet, Brushes.Black, margin + 10, y);
                            g.DrawString("توقيع البائع: .......................................", boldSheet, Brushes.Black, new RectangleF(0, y, pageW - margin - 10, 24), right);
                            y += (isA4Page ? 32 : 28);
                        }
                    }
                    else if (string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase))
                    {
                        g.DrawLine(new Pen(Color.DarkSlateGray, 1.5f), margin, y, pageW - margin, y); y += 8;
                        if (invDiscountAmt > 0 || shippingAmt > 0)
                        {
                            g.DrawString($"إجمالي الأصناف: {_runningTotal:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (invDiscountAmt > 0)
                        {
                            g.DrawString($"خصم الفاتورة: -{invDiscountAmt:N2} جنيه ({invDiscountPct:0.##}%)", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (shippingAmt > 0)
                        {
                            g.DrawString($"خدمة الشحن: +{shippingAmt:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه", boldSheet, Brushes.DarkSlateGray, new RectangleF(0, y, pageW - margin, 25), right); y += 25;
                    }
                    else if (string.Equals(a4Template, "SparePartsGrid", StringComparison.OrdinalIgnoreCase))
                    {
                        g.DrawLine(new Pen(Color.SteelBlue, 2f), margin, y, pageW - margin, y); y += 8;
                        if (invDiscountAmt > 0 || shippingAmt > 0)
                        {
                            g.DrawString($"إجمالي الفاتورة: {_runningTotal:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (invDiscountAmt > 0)
                        {
                            g.DrawString($"الخصم الممنوح: -{invDiscountAmt:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (shippingAmt > 0)
                        {
                            g.DrawString($"مصاريف شحن وتوزيع: +{shippingAmt:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"صافي المطلوب: {netAmount:N2} جنيه", boldSheet, Brushes.SteelBlue, new RectangleF(0, y, pageW - margin, 25), right); y += 25;
                    }
                    else if (string.Equals(a4Template, "SupermarketA4", StringComparison.OrdinalIgnoreCase))
                    {
                        g.DrawLine(new Pen(Color.OliveDrab, 1.5f), margin, y, pageW - margin, y); y += 8;
                        if (invDiscountAmt > 0 || shippingAmt > 0)
                        {
                            g.DrawString($"إجمالي المنتجات: {_runningTotal:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (invDiscountAmt > 0)
                        {
                            g.DrawString($"الخصم التجاري: -{invDiscountAmt:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (shippingAmt > 0)
                        {
                            g.DrawString($"خدمة توصيل الطلبات: +{shippingAmt:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"إجمالي الحساب الصافي: {netAmount:N2} جنيه", boldSheet, Brushes.OliveDrab, new RectangleF(0, y, pageW - margin, 25), right); y += 25;
                    }
                    else if (string.Equals(a4Template, "Official", StringComparison.OrdinalIgnoreCase))
                    {
                        g.DrawLine(Pens.Black, margin, y, pageW - margin, y); y += 8;
                        
                        // Draw totals in a structured box
                        g.DrawRectangle(Pens.Black, pageW - margin - 220, y, 220, 50);
                        g.DrawLine(Pens.Black, pageW - margin - 220, y + 25, pageW - margin, y + 25);
                        g.DrawLine(Pens.Black, pageW - margin - 100, y, pageW - margin - 100, y + 50);
                        
                        g.DrawString("الإجمالي", boldSheet, Brushes.Black, new RectangleF(pageW - margin - 100, y + 4, 95, 18), right);
                        g.DrawString($"{_runningTotal:N2}", normal, Brushes.Black, new RectangleF(pageW - margin - 215, y + 4, 110, 18), left);
                        
                        g.DrawString("الصافي", boldSheet, Brushes.Black, new RectangleF(pageW - margin - 100, y + 29, 95, 18), right);
                        g.DrawString($"{netAmount:N2}", boldSheet, Brushes.Black, new RectangleF(pageW - margin - 215, y + 29, 110, 18), left);
                        
                        y += 60;
                    }
                    else if (string.Equals(a4Template, "Simple", StringComparison.OrdinalIgnoreCase))
                    {
                        g.DrawLine(Pens.Black, margin, y, pageW - margin, y); y += 8;
                        if (invDiscountAmt > 0 || shippingAmt > 0)
                        {
                            string summaryStr = $"الإجمالي: {_runningTotal:N2}";
                            if (invDiscountAmt > 0) summaryStr += $" | الخصم: {invDiscountAmt:N2}";
                            if (shippingAmt > 0) summaryStr += $" | الشحن: {shippingAmt:N2}";
                            g.DrawString(summaryStr, normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"صافي المطلوب: {netAmount:N2} جنيه", boldSheet, Brushes.Black, new RectangleF(0, y, pageW - margin, 25), right); y += 25;
                    }
                    else if (isAlTarek)
                    {
                        // صافي المطلوب + التفقيط + الشعار - يظهران دائماً في نموذج الطارق هوم
                        if (invDiscountAmt > 0 || shippingAmt > 0)
                        {
                            g.DrawString($"إجمالي الأصناف: {_runningTotal:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (invDiscountAmt > 0)
                        {
                            g.DrawString($"الخصم: -{invDiscountAmt:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (shippingAmt > 0)
                        {
                            g.DrawString($"الشحن: +{shippingAmt:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"صافي المطلوب: {netAmount:N2} جنيه", boldSheet, Brushes.Black, new RectangleF(0, y, pageW - margin, 24), right); y += 30;

                        int sumTableWt = pageW - 2 * margin;
                        string tafStrMain = TafqeetHelper.ConvertToArabicWords(netAmount);
                        using (var pinkBrushMain = new SolidBrush(Color.FromArgb(219, 39, 119)))
                        {
                            g.DrawString(tafStrMain, boldSheet, pinkBrushMain, new RectangleF(margin, y, sumTableWt, 20), center);
                            y += 24;
                            var sloganFontMain = new Font("Arial", 12, FontStyle.Bold | FontStyle.Italic);
                            g.DrawString("لأنك تستحق الأفضل", sloganFontMain, pinkBrushMain, new RectangleF(margin, y, sumTableWt, 22), center);
                            y += 32;
                        }
                    }
                    else if (!isAlTarek && !isCommercial)
                    {
                        g.DrawLine(new Pen(Color.DarkBlue, 1.5f), margin, y, pageW - margin, y); y += 8;
                        if (invDiscountAmt > 0 || shippingAmt > 0)
                        {
                            g.DrawString($"إجمالي الأصناف: {_runningTotal:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (invDiscountAmt > 0)
                        {
                            g.DrawString($"خصم الفاتورة: -{invDiscountAmt:N2} جنيه ({invDiscountPct:0.##}%)", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        if (shippingAmt > 0)
                        {
                            g.DrawString($"خدمة الشحن: +{shippingAmt:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه", boldSheet, Brushes.DarkRed, new RectangleF(0, y, pageW - margin, 25), right); y += 25;
                    }

                    // print cash paid details for A4 sheet if Cash sale
                    if (_saleRow != null && _saleRow["SaleType"].ToString() == "Cash" && !isAlTarek)
                    {
                        decimal sheetCashPaid = _saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(_saleRow["CashPaid"]) : netAmount;
                        decimal remainingFromInvoice = netAmount - sheetCashPaid;

                        y += 5;
                        g.DrawString($"المدفوع نقداً: {sheetCashPaid:N2} جنيه", boldSheet, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        if (remainingFromInvoice > 0)
                        {
                            g.DrawString($"المتبقي (آجل): {remainingFromInvoice:N2} جنيه (سيتم إضافة المتبقي على حساب العميل)", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 18), right); y += 20;
                        }
                        else if (remainingFromInvoice < 0)
                        {
                            g.DrawString($"الزيادة: {-remainingFromInvoice:N2} جنيه (سيتم خصم الزيادة من حساب العميل)", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 18), right); y += 20;
                        }
                        else
                        {
                            g.DrawString("(تم سداد الفاتورة بالكامل)", boldSheet, Brushes.Black, new RectangleF(0, y, pageW - margin, 18), right); y += 20;
                        }
                        y += 5;
                    }

                    // ===== Balance Section =====
                    if (detailedPrint && _saleRow != null && _saleRow["ClientID"] != DBNull.Value)
                    {
                        int clientID = Convert.ToInt32(_saleRow["ClientID"]);
                        if (clientID > 0)
                        {
                            int saleID = Convert.ToInt32(_saleRow["SaleID"]);
                            DateTime saleDate = Convert.ToDateTime(_saleRow["SaleDate"]);
                            decimal previousBalance = ClientDAL.GetPreviousBalanceBeforeSale(clientID, saleID);
                            bool isCredit = _saleRow["SaleType"].ToString() == "Credit";
                            decimal currentBalance  = 0;
                            decimal paymentToday    = 0;
                            decimal returnToday     = 0;

                            int saleTransID = 0;
                            var dtTrans = DbHelper.Query(@"
                                SELECT TOP 1 TransID 
                                FROM ClientTransactions 
                                WHERE ClientID = @cid AND TransType = 'Sale' AND RefID = @sid 
                                ORDER BY TransID DESC",
                                DbHelper.P("@cid", clientID), DbHelper.P("@sid", saleID));
                            if (dtTrans.Rows.Count > 0)
                            {
                                saleTransID = Convert.ToInt32(dtTrans.Rows[0]["TransID"]);
                            }

                            var dtPay = DbHelper.Query(@"
                                SELECT 
                                    COALESCE(SUM(CASE WHEN TransType = 'Payment' THEN Credit ELSE 0 END), 0) AS TotalPayment,
                                    COALESCE(SUM(CASE WHEN TransType = 'Return' THEN Credit ELSE 0 END), 0) AS TotalReturn
                                FROM ClientTransactions
                                WHERE ClientID = @cid 
                                  AND CAST(TransDate AS DATE) = CAST(@dt AS DATE)
                                  AND TransID >= @saleTransID
                                  AND NOT (RefID = @sid AND TransType = 'Payment')",
                                DbHelper.P("@cid", clientID), 
                                DbHelper.P("@dt", saleDate),
                                DbHelper.P("@saleTransID", saleTransID),
                                DbHelper.P("@sid", saleID));
                            if (dtPay.Rows.Count > 0)
                            {
                                paymentToday = Convert.ToDecimal(dtPay.Rows[0]["TotalPayment"]);
                                returnToday  = Convert.ToDecimal(dtPay.Rows[0]["TotalReturn"]);
                            }

                            decimal sheetCashPaid = _saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(_saleRow["CashPaid"]) : (isCredit ? 0m : netAmount);
                            decimal remainingFromInvoice = isCredit ? (netAmount - sheetCashPaid) : (netAmount - sheetCashPaid);
                            currentBalance = previousBalance + remainingFromInvoice - paymentToday - returnToday;

                            if (!isAlTarek) g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;

                            if (isAlTarek)
                            {
                                // ════════════════════════════════════════════════════════════════════════
                                // 5-Column Invoice Summary Table: [ إجمالي | مدفوع | أجل | سابق | حالي ]
                                // ════════════════════════════════════════════════════════════════════════
                                int sumTableW = pageW - 2 * margin;
                                float colW5 = sumTableW / 5f;
                                float x5 = margin;

                                // Header row
                                g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), margin, y, sumTableW, 22);
                                g.DrawRectangle(Pens.Black, margin, y, sumTableW, 22);

                                string[] h5 = { "إجمالي", "مدفوع", "أجل", "سابق", "حالي" };
                                for (int i = 0; i < 5; i++)
                                {
                                    g.DrawString(h5[i], boldSheet, Brushes.Black, new RectangleF(x5 + i * colW5, y + 2, colW5, 18), center);
                                    if (i > 0) g.DrawLine(Pens.Black, x5 + i * colW5, y, x5 + i * colW5, y + 22);
                                }
                                y += 22;

                                // Values row
                                g.DrawRectangle(Pens.Black, margin, y, sumTableW, 24);
                                string[] v5 = { netAmount.ToString("N2"), sheetCashPaid.ToString("N2"), remainingFromInvoice.ToString("N2"), previousBalance.ToString("N2"), currentBalance.ToString("N2") };
                                for (int i = 0; i < 5; i++)
                                {
                                    g.DrawString(v5[i], boldSheet, Brushes.Black, new RectangleF(x5 + i * colW5, y + 3, colW5, 18), center);
                                    if (i > 0) g.DrawLine(Pens.Black, x5 + i * colW5, y, x5 + i * colW5, y + 24);
                                }
                                y += 30;

                                // التفقيط والشعار طُبعا أعلاه في قسم الإجمالي (يظهران دائماً)
                            }
                            else if (string.Equals(a4Template, "Official", StringComparison.OrdinalIgnoreCase))
                            {
                                string balText = $"الرصيد السابق: {previousBalance:N2} | ";
                                if (returnToday > 0)
                                {
                                    balText += $"المرتجع اليوم: {returnToday:N2} | ";
                                }
                                balText += $"المدفوع اليوم: {paymentToday:N2} | الرصيد المتبقي: {currentBalance:N2}";
                                g.DrawString(balText, boldSheet, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 20), right);
                                y += 25;
                            }
                            else if (string.Equals(a4Template, "Simple", StringComparison.OrdinalIgnoreCase))
                            {
                                string balText = $"السابق: {previousBalance:N2} | ";
                                if (returnToday > 0)
                                {
                                    balText += $"المرتجع: {returnToday:N2} | ";
                                }
                                balText += $"المدفوع: {paymentToday:N2} | الرصيد الحالي: {currentBalance:N2}";
                                g.DrawString(balText, normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right);
                                y += 20;
                            }
                            else if (string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase))
                            {
                                string balText = $"الرصيد السابق: {previousBalance:N2} جنيه  |  ";
                                if (returnToday > 0)
                                {
                                    balText += $"المرتجع: {returnToday:N2} جنيه  |  ";
                                }
                                balText += $"المسدد: {paymentToday:N2} جنيه";
                                g.DrawString(balText, normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                                y += 20;
                                g.DrawString($"الرصيد الحالي: {currentBalance:N2} جنيه", boldSheet, Brushes.DarkSlateGray, new RectangleF(0, y, pageW - margin, 22), right);
                                y += 25;
                            }
                            else
                            {
                                g.DrawString($"الرصيد السابق: {previousBalance:N2} جنيه", boldSheet, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                                g.DrawString($"المدفوع (التحصيل): {paymentToday:N2} جنيه", boldSheet, Brushes.Black, margin, y);
                                y += 25;

                                g.DrawString($"الرصيد الحالي: {currentBalance:N2} جنيه", boldBigSheet, Brushes.DarkBlue, new RectangleF(0, y, pageW, 28), center);
                                y += 35;
                            }

                            if (AppConfig.EnableCratesTracking)
                            {
                                // طباعة الأقفاص في تقرير A4/A5
                                int cratesOut = _saleRow.Table.Columns.Contains("CratesOut") && _saleRow["CratesOut"] != DBNull.Value ? Convert.ToInt32(_saleRow["CratesOut"]) : 0;
                                int cratesIn = _saleRow.Table.Columns.Contains("CratesIn") && _saleRow["CratesIn"] != DBNull.Value ? Convert.ToInt32(_saleRow["CratesIn"]) : 0;
                                int cratesBal = ClientDAL.GetClientCratesBalance(clientID);

                                string cratesText = "";
                                if (cratesOut > 0) cratesText += $"فوارغ صادرة: {cratesOut} | ";
                                if (cratesIn > 0) cratesText += $"فوارغ واردة: {cratesIn} | ";
                                cratesText += $"رصيد الفوارغ الحالي للعميل: {cratesBal} فارغ";

                                g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                                g.DrawString(cratesText, boldSheet, Brushes.DarkSlateGray, new RectangleF(margin, y, pageW - 2 * margin, 20), right);
                                y += 25;
                            }
                        }
                    }

                    // Official Signatures
                    if (string.Equals(a4Template, "Official", StringComparison.OrdinalIgnoreCase))
                    {
                        y += 15;
                        if (y + 50 <= e.PageBounds.Height)
                        {
                            g.DrawString("توقيع المستلم: ....................", normal, Brushes.Black, margin, y);
                            g.DrawString("توقيع المحاسب: ....................", normal, Brushes.Black, pageW / 2 - 80, y);
                            g.DrawString("أمين المستودع: ....................", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 30;
                        }
                    }

                    if (isAlTarek)
                    {
                        // Draw bottom black footer bar for Al Tarek Home
                        int footerH = 28;
                        int safeBottom = e.MarginBounds.Bottom > 0 ? Math.Min(e.MarginBounds.Bottom - 8, e.PageBounds.Height - 45) : (e.PageBounds.Height - 50);
                        int footerY = safeBottom - footerH;

                        g.FillRectangle(Brushes.Black, margin, footerY, pageW - 2 * margin, footerH);

                        string companyFooterAddr = !string.IsNullOrEmpty(AppConfig.CompanyAddress)
                            ? $"العنوان : {AppConfig.CompanyAddress}"
                            : "العنوان : إيتاي البارود - شارع الجمهورية - بجوار مسجد المحطة";

                        if (_saleRow != null)
                        {
                            string saleCode = _saleRow["SaleCode"]?.ToString() ?? "";
                            DrawSimpleBarcode(g, saleCode, margin + 8, footerY + 3, 120, 22);
                        }

                        g.DrawString(companyFooterAddr, boldSheet, Brushes.White, new RectangleF(margin + 130, footerY + 5, pageW - 2 * margin - 140, 20), center);
                    }
                    else
                    {
                        g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                        if (AppConfig.BusinessType == "Clothing")
                        {
                            g.DrawString("سياسة الاستبدال والاسترجاع: الاستبدال خلال 14 يوماً والاسترجاع خلال 7 أيام من تاريخ الفاتورة بشرط وجود تكت الملابس والفاتورة.", small, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 20), center);
                            y += 22;
                            g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                        }
                        string footerTextText = string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase) 
                            ? "نشكركم لزيارتكم وثقتكم بنا" 
                            : "شكراً لتعاملكم معنا";
                        g.DrawString(footerTextText, small, Brushes.Black, new RectangleF(0, y, pageW, 20), center);
                    }
                }
            };

            if (_showPreview)
            {
                var preview = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = isReceipt ? 400 : 650,
                    Height = 700,
                    Text = "معاينة الطباعة"
                };

                Form owner = null;
                try
                {
                    if (Application.OpenForms.Count > 0)
                    {
                        foreach (Form f in Application.OpenForms)
                        {
                            if (f.Visible && f.GetType().Name == "FrmPOS")
                            {
                                owner = f;
                                break;
                            }
                        }
                        if (owner == null)
                        {
                            for (int i = Application.OpenForms.Count - 1; i >= 0; i--)
                            {
                                Form f = Application.OpenForms[i];
                                if (f.Visible)
                                {
                                    owner = f;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }

                if (owner != null)
                {
                    preview.ShowDialog(owner);
                    try
                    {
                        owner.Activate();
                        owner.Focus();
                    }
                    catch { }
                }
                else
                {
                    preview.ShowDialog();
                }
            }
            else
            {
                AppConfig.PrintInBackground(pd);
            }
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
                        bool isA4 = pageW > 700;
                        int maxW = isReceipt ? 120 : (isA4 ? 180 : 130);
                        int maxH = isReceipt ? 60 : (isA4 ? 90 : 65);
                        
                        int newW = img.Width;
                        int newH = img.Height;

                        double ratioX = (double)maxW / img.Width;
                        double ratioY = (double)maxH / img.Height;
                        double ratio = Math.Min(ratioX, ratioY);

                        if (ratio < 1.0)
                        {
                            newW = (int)(img.Width * ratio);
                            newH = (int)(img.Height * ratio);
                        }

                        int x = (pageW - newW) / 2;
                        g.DrawImage(img, x, y, newW, newH);
                        y += newH + (isA4 ? 12 : 8);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل طباعة شعار الشركة", ex, "FrmPrintSale");
            }
        }

        public static string FormatProductCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "";
            string c = code.Trim();
            string trimmed = c.TrimStart('0');
            return string.IsNullOrEmpty(trimmed) ? (c.Contains("0") ? "0" : c) : trimmed;
        }

        private void DrawColHeader(Graphics g, Font f, string text, int x, int w, int y, Brush brush = null)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, f, brush ?? Brushes.Black, new RectangleF(x, y, w, 18), sf);
        }

        private void DrawColCell(Graphics g, Font f, string text, int x, int w, int y, StringFormat sf = null)
        {
            var format = sf ?? new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, 18), format);
        }

        private void DrawColCell(Graphics g, Font f, string text, int x, int w, int y, int h, StringFormat sf = null)
        {
            var format = sf ?? new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, h), format);
        }

        private void DrawSimpleBarcode(Graphics g, string text, float x, float y, float w, float h)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try
            {
                g.FillRectangle(Brushes.White, x, y, w, h);
                float barX = x + 4;
                float barWidth = (w - 8) / (text.Length * 6f);
                if (barWidth < 1f) barWidth = 1.2f;

                using (var blackBrush = new SolidBrush(Color.Black))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        int charVal = (int)text[i];
                        for (int b = 0; b < 4; b++)
                        {
                            bool isBlack = ((charVal >> b) & 1) == 1 || b == 0;
                            float currentBarW = ((charVal + b) % 3 == 0) ? barWidth * 2f : barWidth;
                            if (isBlack && barX + currentBarW < x + w - 4)
                            {
                                g.FillRectangle(blackBrush, barX, y + 2, currentBarW, h - 4);
                            }
                            barX += currentBarW + 1.2f;
                        }
                    }
                }
            }
            catch { }
        }
    }
}

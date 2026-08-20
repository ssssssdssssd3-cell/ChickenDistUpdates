using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPrintPurchase
    {
        private int _purchaseID;
        private DataRow _purchaseRow;
        private DataTable _items;
        private int _printItemIndex = 0;
        private decimal _runningTotal = 0;
        private string _printFormat;

        public FrmPrintPurchase(int purchaseID, string format = null)
        {
            _purchaseID = purchaseID;
            _printFormat = format ?? AppConfig.DefaultInvoiceFormat;
            if (string.IsNullOrEmpty(_printFormat))
                _printFormat = "Receipt";

            LoadData();
            DoPrint();
        }

        private void LoadData()
        {
            var dt = DbHelper.Query(@"
                SELECT p.PurchaseID, p.PurchaseCode, p.PurchaseDate, p.PurchaseType, p.SupplierID, p.TotalAmount, p.Notes,
                       COALESCE(p.DiscountAmount, 0) AS DiscountAmount, COALESCE(p.DiscountPct, 0) AS DiscountPct,
                       COALESCE(p.TaxAmount, 0) AS TaxAmount, COALESCE(p.TaxPct, 0) AS TaxPct,
                       COALESCE(s.SupplierName, N'---') AS SupplierName
                 FROM Purchases p
                 LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                 WHERE p.PurchaseID = @id", DbHelper.P("@id", _purchaseID));
            if (dt.Rows.Count > 0)
                _purchaseRow = dt.Rows[0];
            
            _items = PurchaseDAL.GetItems(_purchaseID);
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
            };

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 12, FontStyle.Bold);
                var bold = new Font("Arial", 9, FontStyle.Bold);
                var normal = new Font("Arial", 8.5f);
                var small = new Font("Arial", 7.5f);

                int pageW = e.PageBounds.Width;
                int margin = isReceipt ? 10 : 20;
                int y = 15;

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };

                if (isReceipt)
                {
                    // ==========================================
                    // THERMAL RECEIPT LAYOUT (80mm width)
                    // ==========================================
                    
                    // Title & Company Name
                    g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(0, y, pageW, 25), center); y += 22;
                    g.DrawString("فاتورة مشتريات", bold, Brushes.Black, new RectangleF(0, y, pageW, 20), center); y += 20;
                    g.DrawLine(Pens.Black, margin, y, pageW - margin, y); y += 6;

                    // Purchase Info (RTL alignment)
                    if (_purchaseRow != null)
                    {
                        g.DrawString($"رقم الفاتورة: {_purchaseRow["PurchaseCode"]}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                        g.DrawString($"التاريخ: {Convert.ToDateTime(_purchaseRow["PurchaseDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                        
                        string typeLabel = _purchaseRow["PurchaseType"].ToString() == "Credit" ? "آجل" : "نقدي";
                        g.DrawString($"المورد: {_purchaseRow["SupplierName"]}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                        g.DrawString($"الدفع: {typeLabel}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 18;
                    }
                    g.DrawLine(Pens.Black, margin, y, pageW - margin, y); y += 6;

                    // Items List
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
                            decimal itemDisc = Convert.ToDecimal(r["DiscountAmt"]);
                            decimal itemDiscPct = Convert.ToDecimal(r["DiscountPct"]);

                            // Line 1: Product Name (right aligned)
                            g.DrawString(prodName, bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right);
                            y += 16;

                            // Line 2: Qty x UnitPrice = Total (and maybe Discount)
                            string details = $"{qty:0.##} x {price:N2} = {tot:N2}";
                            if (itemDiscPct > 0) details += $" (خصم {itemDiscPct:0.##}%)";
                            else if (itemDisc > 0) details += $" (خصم {itemDisc:N2})";

                            g.DrawString(details, normal, Brushes.DimGray, new RectangleF(margin, y, pageW - 2 * margin, 16), right);
                            y += 16;

                            _runningTotal += tot;
                            _printItemIndex++;
                        }
                    }

                    e.HasMorePages = false;
                    g.DrawLine(Pens.Black, margin, y, pageW - margin, y); y += 6;

                    // Totals
                    decimal invDiscountAmt = 0;
                    decimal invDiscountPct = 0;
                    decimal invTaxAmt = 0;
                    decimal invTaxPct = 0;
                    decimal netAmount = _runningTotal;
                    if (_purchaseRow != null)
                    {
                        invDiscountAmt = Convert.ToDecimal(_purchaseRow["DiscountAmount"]);
                        invDiscountPct = Convert.ToDecimal(_purchaseRow["DiscountPct"]);
                        invTaxAmt      = Convert.ToDecimal(_purchaseRow["TaxAmount"]);
                        invTaxPct      = Convert.ToDecimal(_purchaseRow["TaxPct"]);
                        netAmount      = Convert.ToDecimal(_purchaseRow["TotalAmount"]);
                    }

                    if (invDiscountAmt > 0)
                    {
                        g.DrawString($"إجمالي الأصناف: {_runningTotal:N2}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                        g.DrawString($"خصم الفاتورة: {invDiscountAmt:N2} ({invDiscountPct:0.##}%)", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                    }

                    if (invTaxAmt > 0)
                    {
                        g.DrawString($"الضريبة: {invTaxAmt:N2} ({invTaxPct:0.##}%)", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                    }

                    g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 20), right); y += 22;

                    // Notes
                    if (_purchaseRow != null && !string.IsNullOrWhiteSpace(_purchaseRow["Notes"]?.ToString()))
                    {
                        g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 6;
                        g.DrawString($"ملاحظات: {_purchaseRow["Notes"]}", small, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 32), right); y += 34;
                    }

                    g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 6;
                    g.DrawString("تمت طباعته عبر نظام توزيع الكتاكيت", small, Brushes.Gray, new RectangleF(0, y, pageW, 16), center);
                }
                else
                {
                    // ==========================================
                    // STANDARD A4/A5 SHEET LAYOUT
                    // ==========================================
                    var boldBigSheet = new Font("Arial", 14, FontStyle.Bold);
                    var boldSheet = new Font("Arial", 10, FontStyle.Bold);

                    // ===== Header =====
                    g.DrawString("فاتورة مشتريات", boldBigSheet, Brushes.DarkBlue, new RectangleF(0, y, pageW, 30), center); y += 30;
                    g.DrawString(AppConfig.CompanyName, boldSheet, Brushes.Black, new RectangleF(0, y, pageW, 22), center); y += 25;
                    g.DrawLine(new Pen(Color.DarkBlue, 2), margin, y, pageW - margin, y); y += 10;

                    // ===== Purchase Info (RTL) =====
                    if (_purchaseRow != null)
                    {
                        g.DrawString($"التاريخ: {Convert.ToDateTime(_purchaseRow["PurchaseDate"]):dd/MM/yyyy}",
                            normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                        g.DrawString($"رقم الفاتورة: {_purchaseRow["PurchaseCode"]}",
                            normal, Brushes.Black, margin, y);
                        y += 20;

                        string typeLabel = _purchaseRow["PurchaseType"].ToString() == "Credit" ? "آجل" : "نقدي";
                        g.DrawString($"المورد: {_purchaseRow["SupplierName"]}   |   النوع: {typeLabel}",
                            normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                        y += 25;
                    }

                    // ===== Table Header =====
                    int xTotal    = margin;       // Total amount
                    int xDiscount = 105;          // Discount
                    int xPrice    = 170;          // Unit price
                    int xQty      = 255;          // Quantity
                    int xProduct  = 340;          // Product Name

                    int colWTotal    = 80;
                    int colWDiscount = 60;
                    int colWPrice    = 80;
                    int colWQty      = 80;
                    int colWProduct  = 220;

                    DrawColHeader(g, boldSheet, "الإجمالي",  xTotal,    colWTotal,    y);
                    DrawColHeader(g, boldSheet, "الخصم",     xDiscount, colWDiscount, y);
                    DrawColHeader(g, boldSheet, "السعر",     xPrice,    colWPrice,    y);
                    DrawColHeader(g, boldSheet, "الكمية",    xQty,      colWQty,      y);
                    DrawColHeader(g, boldSheet, "الصنف",     xProduct,  colWProduct,  y);
                    y += 20;
                    g.DrawLine(Pens.Gray, margin, y, pageW - margin, y); y += 4;

                    // ===== Items =====
                    if (_items != null)
                    {
                        while (_printItemIndex < _items.Rows.Count)
                        {
                            if (y + 150 > e.PageBounds.Height)
                            {
                                e.HasMorePages = true;
                                return;
                            }

                            DataRow r = _items.Rows[_printItemIndex];
                            decimal qty   = Convert.ToDecimal(r["Quantity"]);
                            decimal price = Convert.ToDecimal(r["UnitPrice"]);
                            decimal tot   = Convert.ToDecimal(r["TotalPrice"]);
                            decimal itemDiscPct = Convert.ToDecimal(r["DiscountPct"]);
                            decimal itemDiscAmt = Convert.ToDecimal(r["DiscountAmt"]);
                            
                            string discText = "-";
                            if (itemDiscPct > 0)
                                discText = $"{itemDiscPct:0.##}%";
                            else if (itemDiscAmt > 0)
                                discText = itemDiscAmt.ToString("F2");

                            DrawColCell(g, normal, tot.ToString("N2"),     xTotal,    colWTotal,    y);
                            DrawColCell(g, normal, discText,               xDiscount, colWDiscount, y);
                            DrawColCell(g, normal, price.ToString("N2"),   xPrice,    colWPrice,    y);
                            DrawColCell(g, normal, qty.ToString("N2"),     xQty,      colWQty,      y);
                            DrawColCell(g, normal, r["ProductName"].ToString(), xProduct, colWProduct,  y);
                            _runningTotal += tot; y += 18;
                            _printItemIndex++;
                        }
                    }
                    
                    e.HasMorePages = false;

                    g.DrawLine(new Pen(Color.DarkBlue, 1.5f), margin, y, pageW - margin, y); y += 8;

                    // Get invoice discount details
                    decimal invDiscountAmt = 0;
                    decimal invDiscountPct = 0;
                    decimal invTaxAmt = 0;
                    decimal invTaxPct = 0;
                    decimal netAmount = _runningTotal;
                    if (_purchaseRow != null)
                    {
                        invDiscountAmt = Convert.ToDecimal(_purchaseRow["DiscountAmount"]);
                        invDiscountPct = Convert.ToDecimal(_purchaseRow["DiscountPct"]);
                        invTaxAmt      = Convert.ToDecimal(_purchaseRow["TaxAmount"]);
                        invTaxPct      = Convert.ToDecimal(_purchaseRow["TaxPct"]);
                        netAmount      = Convert.ToDecimal(_purchaseRow["TotalAmount"]);
                    }

                    if (invDiscountAmt > 0)
                    {
                        g.DrawString($"إجمالي الأصناف: {_runningTotal:N2} جنيه",
                            normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        g.DrawString($"خصم الفاتورة: {invDiscountAmt:N2} جنيه ({invDiscountPct:0.##}%)",
                            normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                    }

                    if (invTaxAmt > 0)
                    {
                        g.DrawString($"الضريبة: {invTaxAmt:N2} جنيه ({invTaxPct:0.##}%)",
                            normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                    }

                    // Net Amount
                    g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه",
                        boldSheet, Brushes.DarkRed, new RectangleF(0, y, pageW - margin, 25), right); y += 25;

                    // Notes
                    if (_purchaseRow != null && !string.IsNullOrWhiteSpace(_purchaseRow["Notes"]?.ToString()))
                    {
                        g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                        g.DrawString($"ملاحظات: {_purchaseRow["Notes"]}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 40), right); y += 45;
                    }

                    g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                    g.DrawString("تمت طباعته عبر نظام توزيع الكتاكيت", small, Brushes.Gray, new RectangleF(0, y, pageW, 20), center);
                }
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = isReceipt ? 400 : 650,
                Height = 700,
                Text = "معاينة طباعة المشتريات"
            };
            preview.ShowDialog();
        }

        private void DrawColHeader(Graphics g, Font f, string text, int x, int w, int y)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(text, f, Brushes.DarkBlue, new RectangleF(x, y, w, 18), sf);
        }

        private void DrawColCell(Graphics g, Font f, string text, int x, int w, int y)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, 18), sf);
        }
    }
}

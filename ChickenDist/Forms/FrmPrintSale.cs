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
        private string _printFormat;

        public FrmPrintSale(int saleID, string format = null)
        {
            _saleID = saleID;
            _printFormat = format ?? AppConfig.DefaultInvoiceFormat;
            if (string.IsNullOrEmpty(_printFormat))
                _printFormat = "Receipt";

            LoadData();
            DoPrint();
        }

        private void LoadData()
        {
            var dt = DbHelper.Query(@"
                SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType, s.ClientID, s.TotalAmount, s.Notes,
                       COALESCE(s.DiscountAmount, 0) AS DiscountAmount, COALESCE(s.DiscountPct, 0) AS DiscountPct,
                       COALESCE(c.ClientName, N'---') AS ClientName,
                       COALESCE(e.EmpName, N'---') AS DriverName
                 FROM Sales s
                 LEFT JOIN Clients c ON s.ClientID = c.ClientID
                 LEFT JOIN Employees e ON s.DriverID = e.EmpID
                 WHERE s.SaleID = @id", DbHelper.P("@id", _saleID));
            if (dt.Rows.Count > 0)
                _saleRow = dt.Rows[0];
            
            _items = SaleDAL.GetItems(_saleID);
        }

        private void DoPrint()
        {
            var pd = new PrintDocument();
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
                bool detailedPrint = AppConfig.ReceiptPrintMode != "Compact";

                if (isReceipt)
                {
                    // ==========================================
                    // THERMAL RECEIPT LAYOUT (80mm width)
                    // ==========================================
                    
                    // Title & Company Name
                    g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(0, y, pageW, 25), center); y += 22;
                    g.DrawString("فاتورة مبيعات", bold, Brushes.Black, new RectangleF(0, y, pageW, 20), center); y += 20;
                    g.DrawLine(Pens.Black, margin, y, pageW - margin, y); y += 6;

                    // Sale Info (RTL alignment)
                    if (_saleRow != null)
                    {
                        g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                        g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                        
                        string typeLabel = _saleRow["SaleType"].ToString() == "Credit" ? "آجل"
                                         : _saleRow["SaleType"].ToString() == "Cash"   ? "نقدي"
                                         : "تحميل مندوب";
                        string driverText = _saleRow["DriverName"].ToString() != "---" ? $" | مندوب: {_saleRow["DriverName"]}" : "";
                        g.DrawString($"العميل: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                        g.DrawString($"الدفع: {typeLabel}{driverText}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 18;
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
                            decimal itemDisc = r.Table.Columns.Contains("DiscountAmt") && r["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(r["DiscountAmt"]) : 0m;
                            decimal itemDiscPct = r.Table.Columns.Contains("DiscountPct") && r["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(r["DiscountPct"]) : 0m;

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
                    decimal netAmount = _runningTotal;
                    if (_saleRow != null)
                    {
                        invDiscountAmt = _saleRow.Table.Columns.Contains("DiscountAmount") && _saleRow["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountAmount"]) : 0m;
                        invDiscountPct = _saleRow.Table.Columns.Contains("DiscountPct") && _saleRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountPct"]) : 0m;
                        netAmount = Convert.ToDecimal(_saleRow["TotalAmount"]);
                    }

                    if (invDiscountAmt > 0)
                    {
                        g.DrawString($"إجمالي الأصناف: {_runningTotal:N2}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                        g.DrawString($"خصم الفاتورة: {invDiscountAmt:N2} ({invDiscountPct:0.##}%)", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                    }

                    g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 20), right); y += 22;

                    // Client Balance
                    if (detailedPrint && _saleRow != null && _saleRow["ClientID"] != DBNull.Value)
                    {
                        int clientID = Convert.ToInt32(_saleRow["ClientID"]);
                        if (clientID > 0)
                        {
                            decimal currentBalance = 0;
                            decimal paymentToday = 0;
                            decimal previousBalance = 0;
                            bool isCredit = _saleRow["SaleType"].ToString() == "Credit";

                            var dtBal = DbHelper.Query(@"
                                SELECT COALESCE(cb.Balance, c.OpeningBalance) AS Balance
                                FROM Clients c
                                LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                                WHERE c.ClientID = @cid", DbHelper.P("@cid", clientID));
                            if (dtBal.Rows.Count > 0)
                                currentBalance = Convert.ToDecimal(dtBal.Rows[0]["Balance"]);

                            DateTime saleDate = Convert.ToDateTime(_saleRow["SaleDate"]);
                            var dtPay = DbHelper.Query(@"
                                SELECT COALESCE(SUM(Credit), 0) AS TotalPayment
                                FROM ClientTransactions
                                WHERE ClientID = @cid AND TransType = 'Payment' AND CAST(TransDate AS DATE) = CAST(@dt AS DATE)",
                                DbHelper.P("@cid", clientID), DbHelper.P("@dt", saleDate));
                            if (dtPay.Rows.Count > 0)
                                paymentToday = Convert.ToDecimal(dtPay.Rows[0]["TotalPayment"]);

                            if (isCredit)
                                previousBalance = currentBalance - netAmount + paymentToday;
                            else
                                previousBalance = currentBalance + paymentToday;

                            g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 6;
                            g.DrawString($"الرصيد السابق: {previousBalance:N2}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                            g.DrawString($"المدفوع (التحصيل): {paymentToday:N2}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                            g.DrawString($"الرصيد الحالي: {currentBalance:N2}", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 20;
                        }
                    }

                    g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 6;
                    g.DrawString("شكراً لتعاملكم معنا", small, Brushes.Gray, new RectangleF(0, y, pageW, 16), center);
                }
                else
                {
                    // ==========================================
                    // STANDARD A4/A5 SHEET LAYOUT
                    // ==========================================
                    var boldBigSheet = new Font("Arial", 14, FontStyle.Bold);
                    var boldSheet = new Font("Arial", 10, FontStyle.Bold);

                    // ===== Header =====
                    g.DrawString("فاتورة مبيعات", boldBigSheet, Brushes.DarkBlue, new RectangleF(0, y, pageW, 30), center); y += 30;
                    g.DrawString(AppConfig.CompanyName, boldSheet, Brushes.Black, new RectangleF(0, y, pageW, 22), center); y += 25;
                    g.DrawLine(new Pen(Color.DarkBlue, 2), margin, y, pageW - margin, y); y += 10;

                    // ===== Sale Info (RTL) =====
                    if (_saleRow != null)
                    {
                        g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy}",
                            normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                        g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}",
                            normal, Brushes.Black, margin, y);
                        y += 20;

                        string typeLabel = _saleRow["SaleType"].ToString() == "Credit" ? "آجل"
                                         : _saleRow["SaleType"].ToString() == "Cash"   ? "نقدي"
                                         : "تحميل مندوب";
                        string driverText = _saleRow["DriverName"].ToString() != "---" ? $"  |  المندوب: {_saleRow["DriverName"]}" : "";
                        g.DrawString($"العميل: {_saleRow["ClientName"]}{driverText}   |   النوع: {typeLabel}",
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
                            decimal itemDiscPct = r.Table.Columns.Contains("DiscountPct") && r["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(r["DiscountPct"]) : 0m;
                            decimal itemDiscAmt = r.Table.Columns.Contains("DiscountAmt") && r["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(r["DiscountAmt"]) : 0m;
                            
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
                    decimal netAmount = _runningTotal;
                    if (_saleRow != null)
                    {
                        invDiscountAmt = _saleRow.Table.Columns.Contains("DiscountAmount") && _saleRow["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountAmount"]) : 0m;
                        invDiscountPct = _saleRow.Table.Columns.Contains("DiscountPct") && _saleRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountPct"]) : 0m;
                        netAmount = Convert.ToDecimal(_saleRow["TotalAmount"]);
                    }

                    if (invDiscountAmt > 0)
                    {
                        g.DrawString($"إجمالي الأصناف: {_runningTotal:N2} جنيه",
                            normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        g.DrawString($"خصم الفاتورة: {invDiscountAmt:N2} جنيه ({invDiscountPct:0.##}%)",
                            normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                    }

                    // Net Amount
                    g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه",
                        boldSheet, Brushes.DarkRed, new RectangleF(0, y, pageW - margin, 25), right); y += 25;

                    // ===== Balance Section =====
                    if (detailedPrint && _saleRow != null && _saleRow["ClientID"] != DBNull.Value)
                    {
                        int clientID = Convert.ToInt32(_saleRow["ClientID"]);
                        if (clientID > 0)
                        {
                            decimal currentBalance  = 0;
                            decimal paymentToday    = 0;
                            decimal previousBalance = 0;
                            bool isCredit = _saleRow["SaleType"].ToString() == "Credit";

                            var dtBal = DbHelper.Query(@"
                                SELECT COALESCE(cb.Balance, c.OpeningBalance) AS Balance
                                FROM Clients c
                                LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                                WHERE c.ClientID = @cid", DbHelper.P("@cid", clientID));
                            if (dtBal.Rows.Count > 0)
                                currentBalance = Convert.ToDecimal(dtBal.Rows[0]["Balance"]);

                            DateTime saleDate = Convert.ToDateTime(_saleRow["SaleDate"]);
                            var dtPay = DbHelper.Query(@"
                                SELECT COALESCE(SUM(Credit), 0) AS TotalPayment
                                FROM ClientTransactions
                                WHERE ClientID = @cid AND TransType = 'Payment' AND CAST(TransDate AS DATE) = CAST(@dt AS DATE)",
                                DbHelper.P("@cid", clientID), DbHelper.P("@dt", saleDate));
                            if (dtPay.Rows.Count > 0)
                                paymentToday = Convert.ToDecimal(dtPay.Rows[0]["TotalPayment"]);

                            if (isCredit)
                                previousBalance = currentBalance - netAmount + paymentToday;
                            else
                                previousBalance = currentBalance + paymentToday;

                            g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;

                            g.DrawString($"الرصيد السابق: {previousBalance:N2} جنيه",
                                boldSheet, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            g.DrawString($"المدفوع (التحصيل): {paymentToday:N2} جنيه",
                                boldSheet, Brushes.Black, margin, y);
                            y += 25;

                            g.DrawString($"الرصيد الحالي: {currentBalance:N2} جنيه",
                                boldBigSheet, Brushes.DarkBlue, new RectangleF(0, y, pageW, 28), center);
                            y += 35;
                        }
                    }

                    g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                    g.DrawString("شكراً لتعاملكم معنا", small, Brushes.Gray, new RectangleF(0, y, pageW, 20), center);
                }
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = isReceipt ? 400 : 650,
                Height = 700,
                Text = "معاينة الطباعة"
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

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
                SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType, s.ClientID, s.TotalAmount, s.Notes, s.CashPaid,
                       COALESCE(s.CratesOut, 0) AS CratesOut, COALESCE(s.CratesIn, 0) AS CratesIn,
                       COALESCE(s.DiscountAmount, 0) AS DiscountAmount, COALESCE(s.DiscountPct, 0) AS DiscountPct,
                       CASE WHEN s.ClientID IS NULL AND s.SaleType = 'Cash' THEN N'عميل نقدي' ELSE COALESCE(c.ClientName, N'---') END AS ClientName,
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
                var left = new StringFormat { Alignment = StringAlignment.Near };
                bool detailedPrint = AppConfig.ReceiptPrintMode != "Compact";

                if (isReceipt)
                {
                    // ==========================================
                    // THERMAL RECEIPT LAYOUT (80mm width)
                    // ==========================================
                    string template = AppConfig.ReceiptTemplate;
                    
                    if (string.Equals(template, "Modern", StringComparison.OrdinalIgnoreCase))
                    {
                        // Fill a modern slate gray banner with white text
                        g.FillRectangle(Brushes.DarkSlateGray, margin, y, pageW - 2 * margin, 26);
                        g.DrawString(AppConfig.CompanyName, bold, Brushes.White, new RectangleF(margin, y + 4, pageW - 2 * margin, 20), center);
                        y += 32;
                        
                        g.DrawString("فاتورة مبيعات", boldBig, Brushes.Black, new RectangleF(0, y, pageW, 25), center);
                        y += 24;
                        g.DrawLine(new Pen(Color.Black, 1.5f), margin, y, pageW - margin, y);
                        y += 6;
                        
                        // Sale Info
                        if (_saleRow != null)
                        {
                            g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 18;
                            g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                            
                            string typeLabel = _saleRow["SaleType"].ToString() == "Credit" ? "آجل" : "نقدي";
                            string driverText = _saleRow["DriverName"].ToString() != "---" ? $" | مندوب: {_saleRow["DriverName"]}" : "";
                            g.DrawString($"العميل: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                            g.DrawString($"طريقة الدفع: {typeLabel}{driverText}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 20;
                        }
                        
                        g.DrawLine(new Pen(Color.Black, 1.5f), margin, y, pageW - margin, y);
                        y += 6;
                        
                        // Table header for items
                        g.DrawString("الصنف والكمية", bold, Brushes.Black, new RectangleF(margin + 70, y, pageW - 2 * margin - 70, 16), right);
                        g.DrawString("الإجمالي", bold, Brushes.Black, new RectangleF(margin, y, 70, 16), left);
                        y += 18;
                        g.DrawLine(Pens.Gray, margin, y, pageW - margin, y);
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
                                
                                g.DrawString(prodName, bold, Brushes.Black, new RectangleF(margin + 70, y, pageW - 2 * margin - 70, 16), right);
                                g.DrawString(tot.ToString("N2"), bold, Brushes.Black, new RectangleF(margin, y, 70, 16), left);
                                y += 15;
                                
                                g.DrawString($"{qty:0.##} × {price:N2}", small, Brushes.DimGray, new RectangleF(margin + 70, y, pageW - 2 * margin - 70, 14), right);
                                y += 15;
                                
                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        
                        e.HasMorePages = false;
                        g.DrawLine(new Pen(Color.Black, 1.5f), margin, y, pageW - margin, y);
                        y += 6;
                    }
                    else if (string.Equals(template, "Compact", StringComparison.OrdinalIgnoreCase))
                    {
                        // Compact format: smaller fonts, tight layout, single-line items
                        g.DrawString(AppConfig.CompanyName, bold, Brushes.Black, new RectangleF(0, y, pageW, 18), center); y += 16;
                        g.DrawString("فاتورة مبيعات مبسطة", bold, Brushes.Black, new RectangleF(0, y, pageW, 16), center); y += 14;
                        g.DrawLine(Pens.Gray, margin, y, pageW - margin, y); y += 4;
                        
                        if (_saleRow != null)
                        {
                            g.DrawString($"فاتورة: {_saleRow["SaleCode"]} | {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy}", small, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 14), right); y += 14;
                            g.DrawString($"العميل: {_saleRow["ClientName"]}", small, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 14), right); y += 14;
                        }
                        g.DrawLine(Pens.Gray, margin, y, pageW - margin, y); y += 4;
                        
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
                                g.DrawString(itemLineRight, small, Brushes.Black, new RectangleF(margin + 120, y, pageW - 2 * margin - 120, 14), right);
                                g.DrawString(itemLineLeft, small, Brushes.Black, new RectangleF(margin, y, 120, 14), left);
                                y += 13;
                                
                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        
                        e.HasMorePages = false;
                        g.DrawLine(Pens.Gray, margin, y, pageW - margin, y); y += 4;
                    }
                    else if (string.Equals(template, "Elegant", StringComparison.OrdinalIgnoreCase))
                    {
                        // Elegant format: stylish headers, decorative lines, fancy layout
                        g.DrawString("❀ " + AppConfig.CompanyName + " ❀", boldBig, Brushes.DarkSlateGray, new RectangleF(0, y, pageW, 25), center); y += 24;
                        g.DrawString("ـ ــ ـــ فاتورة مبيعات ـــ ــ ـ", bold, Brushes.Black, new RectangleF(0, y, pageW, 20), center); y += 20;
                        g.DrawString("✿ ✿ ✿ ✿ ✿ ✿ ✿ ✿ ✿ ✿ ✿", small, Brushes.Gray, new RectangleF(0, y, pageW, 14), center); y += 14;
                        
                        if (_saleRow != null)
                        {
                            g.DrawString($"رقم المستند: {_saleRow["SaleCode"]}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                            g.DrawString($"تاريخ الإصدار: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 16;
                            g.DrawString($"السيد/ة: {_saleRow["ClientName"]}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 18;
                        }
                        g.DrawLine(new Pen(Color.Black, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }, margin, y, pageW - margin, y); y += 6;
                        
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
                                
                                g.DrawString(prodName, bold, Brushes.DarkSlateGray, new RectangleF(margin, y, pageW - 2 * margin, 18), right);
                                y += 16;
                                
                                string details = $"{qty:0.##} وحدة × {price:N2} = {tot:N2}";
                                g.DrawString(details, normal, Brushes.DimGray, new RectangleF(margin, y, pageW - 2 * margin, 16), right);
                                y += 16;
                                
                                _runningTotal += tot;
                                _printItemIndex++;
                            }
                        }
                        
                        e.HasMorePages = false;
                        g.DrawLine(new Pen(Color.Black, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }, margin, y, pageW - margin, y); y += 6;
                    }
                    else
                    {
                        // ==========================================
                        // STANDARD (DEFAULT) LAYOUT
                        // ==========================================
                        g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(0, y, pageW, 25), center); y += 22;
                        g.DrawString("فاتورة مبيعات", bold, Brushes.Black, new RectangleF(0, y, pageW, 20), center); y += 20;
                        g.DrawLine(Pens.Black, margin, y, pageW - margin, y); y += 6;

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

                                g.DrawString(prodName, bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right);
                                y += 16;

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
                    }
                    
                    // ==========================================
                    // COMMON TOTALS AND FOOTER SECTION FOR RECEIPTS
                    // ==========================================
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

                    g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه", boldBig, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 20), right); y += 22;

                    bool isReceiptCredit = _saleRow["SaleType"].ToString() == "Credit";
                    decimal receiptCashPaid = _saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(_saleRow["CashPaid"]) : netAmount;
                    decimal receiptRemaining = isReceiptCredit ? netAmount : (netAmount - receiptCashPaid);

                    if (_saleRow["SaleType"].ToString() == "Cash")
                    {
                        g.DrawString($"المدفوع نقداً: {receiptCashPaid:N2} جنيه", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 18;
                        if (receiptRemaining > 0)
                        {
                            g.DrawString($"المتبقي (آجل): {receiptRemaining:N2} جنيه", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 18;
                            g.DrawString("(سيتم إضافة المتبقي على حساب العميل)", small, Brushes.DimGray, new RectangleF(margin, y, pageW - 2 * margin, 14), right); y += 15;
                        }
                        else if (receiptRemaining < 0)
                        {
                            g.DrawString($"الزيادة: {-receiptRemaining:N2} جنيه", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 18;
                            g.DrawString("(سيتم خصم الزيادة من حساب العميل)", small, Brushes.DimGray, new RectangleF(margin, y, pageW - 2 * margin, 14), right); y += 15;
                        }
                        else
                        {
                            g.DrawString("(تم سداد الفاتورة بالكامل)", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
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

                            decimal remainingFromInvoice = isReceiptCredit ? netAmount : (netAmount - receiptCashPaid);
                            currentBalance = previousBalance + remainingFromInvoice - paymentToday - returnToday;

                            g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 6;
                            g.DrawString($"الرصيد السابق: {previousBalance:N2}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                            if (returnToday > 0)
                            {
                                g.DrawString($"المرتجع اليوم: {returnToday:N2}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                            }
                            g.DrawString($"المدفوع (التحصيل): {paymentToday:N2}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                            g.DrawString($"الرصيد الحالي: {currentBalance:N2}", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 20;

                            int cratesOut = _saleRow.Table.Columns.Contains("CratesOut") && _saleRow["CratesOut"] != DBNull.Value ? Convert.ToInt32(_saleRow["CratesOut"]) : 0;
                            int cratesIn = _saleRow.Table.Columns.Contains("CratesIn") && _saleRow["CratesIn"] != DBNull.Value ? Convert.ToInt32(_saleRow["CratesIn"]) : 0;
                            if (cratesOut > 0 || cratesIn > 0)
                            {
                                string cratesText = "";
                                if (cratesOut > 0) cratesText += $"صادر: {cratesOut} ";
                                if (cratesIn > 0) cratesText += $"وارد: {cratesIn} ";
                                g.DrawString($"الأقفاص بالفاتورة: {cratesText}", normal, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 16), right); y += 16;
                            }
                            int cratesBal = ClientDAL.GetClientCratesBalance(clientID);
                            g.DrawString($"رصيد الأقفاص الحالي: {cratesBal} قفص", bold, Brushes.Black, new RectangleF(margin, y, pageW - 2 * margin, 18), right); y += 20;
                        }
                    }

                    g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 6;
                    string footerText = string.Equals(template, "Elegant", StringComparison.OrdinalIgnoreCase) 
                        ? "نشكركم لزيارتكم الكريمة وثقتكم بنا" 
                        : "شكراً لتعاملكم معنا";
                    g.DrawString(footerText, small, Brushes.Gray, new RectangleF(0, y, pageW, 16), center);
                }
                else
                {
                    // ==========================================
                    // STANDARD A4/A5 SHEET LAYOUT
                    // ==========================================
                    string a4Template = AppConfig.A4Template;
                    var boldBigSheet = new Font("Arial", 14, FontStyle.Bold);
                    var boldSheet = new Font("Arial", 10, FontStyle.Bold);

                    if (string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase))
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

                            string typeLabel = _saleRow["SaleType"].ToString() == "Credit" ? "آجل" : "نقدي";
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
                            string typeLabel = _saleRow["SaleType"].ToString() == "Credit" ? "آجل" : "نقدي";
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

                            string typeLabel = _saleRow["SaleType"].ToString() == "Credit" ? "آجل" : "نقدي";
                            string driverText = _saleRow["DriverName"].ToString() != "---" ? $"  |  المندوب: {_saleRow["DriverName"]}" : "";
                            g.DrawString($"العميل: {_saleRow["ClientName"]}{driverText}   |   النوع: {typeLabel}", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 25;
                        }
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

                    if (string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase))
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

                            if (string.Equals(a4Template, "Official", StringComparison.OrdinalIgnoreCase))
                            {
                                // Draw grid boxes for official template
                                g.DrawRectangle(Pens.Black, xTotal, y, colWTotal, 18);
                                g.DrawRectangle(Pens.Black, xDiscount, y, colWDiscount, 18);
                                g.DrawRectangle(Pens.Black, xPrice, y, colWPrice, 18);
                                g.DrawRectangle(Pens.Black, xQty, y, colWQty, 18);
                                g.DrawRectangle(Pens.Black, xProduct, y, colWProduct, 18);
                            }

                            DrawColCell(g, normal, tot.ToString("N2"),     xTotal,    colWTotal,    y + 2);
                            DrawColCell(g, normal, discText,               xDiscount, colWDiscount, y + 2);
                            DrawColCell(g, normal, price.ToString("N2"),   xPrice,    colWPrice,    y + 2);
                            DrawColCell(g, normal, qty.ToString("N2"),     xQty,      colWQty,      y + 2);
                            DrawColCell(g, normal, r["ProductName"].ToString(), xProduct, colWProduct,  y + 2);
                            
                            _runningTotal += tot; 
                            y += 18;

                            if (string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase))
                            {
                                g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y);
                            }

                            _printItemIndex++;
                        }
                    }
                    
                    e.HasMorePages = false;

                    // ===== Totals section =====
                    decimal invDiscountAmt = 0;
                    decimal invDiscountPct = 0;
                    decimal netAmount = _runningTotal;
                    if (_saleRow != null)
                    {
                        invDiscountAmt = _saleRow.Table.Columns.Contains("DiscountAmount") && _saleRow["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountAmount"]) : 0m;
                        invDiscountPct = _saleRow.Table.Columns.Contains("DiscountPct") && _saleRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(_saleRow["DiscountPct"]) : 0m;
                        netAmount = Convert.ToDecimal(_saleRow["TotalAmount"]);
                    }

                    if (string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase))
                    {
                        g.DrawLine(new Pen(Color.DarkSlateGray, 1.5f), margin, y, pageW - margin, y); y += 8;
                        if (invDiscountAmt > 0)
                        {
                            g.DrawString($"إجمالي الأصناف: {_runningTotal:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                            g.DrawString($"خصم الفاتورة: {invDiscountAmt:N2} جنيه ({invDiscountPct:0.##}%)", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه", boldSheet, Brushes.DarkSlateGray, new RectangleF(0, y, pageW - margin, 25), right); y += 25;
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
                        if (invDiscountAmt > 0)
                        {
                            g.DrawString($"الإجمالي: {_runningTotal:N2} | الخصم: {invDiscountAmt:N2}", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"الصافي المطلوب: {netAmount:N2} جنيه", boldSheet, Brushes.Black, new RectangleF(0, y, pageW - margin, 25), right); y += 25;
                    }
                    else
                    {
                        g.DrawLine(new Pen(Color.DarkBlue, 1.5f), margin, y, pageW - margin, y); y += 8;
                        if (invDiscountAmt > 0)
                        {
                            g.DrawString($"إجمالي الأصناف: {_runningTotal:N2} جنيه", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                            g.DrawString($"خصم الفاتورة: {invDiscountAmt:N2} جنيه ({invDiscountPct:0.##}%)", normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                        }
                        g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه", boldSheet, Brushes.DarkRed, new RectangleF(0, y, pageW - margin, 25), right); y += 25;
                    }

                    // print cash paid details for A4 sheet if Cash sale
                    if (_saleRow != null && _saleRow["SaleType"].ToString() == "Cash")
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

                            decimal sheetCashPaid = _saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(_saleRow["CashPaid"]) : netAmount;
                            decimal remainingFromInvoice = isCredit ? netAmount : (netAmount - sheetCashPaid);
                            currentBalance = previousBalance + remainingFromInvoice - paymentToday - returnToday;

                            g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;

                            if (string.Equals(a4Template, "Official", StringComparison.OrdinalIgnoreCase))
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

                            // طباعة الأقفاص في تقرير A4/A5
                            int cratesOut = _saleRow.Table.Columns.Contains("CratesOut") && _saleRow["CratesOut"] != DBNull.Value ? Convert.ToInt32(_saleRow["CratesOut"]) : 0;
                            int cratesIn = _saleRow.Table.Columns.Contains("CratesIn") && _saleRow["CratesIn"] != DBNull.Value ? Convert.ToInt32(_saleRow["CratesIn"]) : 0;
                            int cratesBal = ClientDAL.GetClientCratesBalance(clientID);

                            string cratesText = "";
                            if (cratesOut > 0) cratesText += $"أقفاص صادرة: {cratesOut} | ";
                            if (cratesIn > 0) cratesText += $"أقفاص واردة: {cratesIn} | ";
                            cratesText += $"رصيد الأقفاص الحالي للعميل: {cratesBal} قفص";

                            g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                            g.DrawString(cratesText, boldSheet, Brushes.DarkSlateGray, new RectangleF(margin, y, pageW - 2 * margin, 20), right);
                            y += 25;
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

                    g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                    string footerTextText = string.Equals(a4Template, "Modern", StringComparison.OrdinalIgnoreCase) 
                        ? "نشكركم لزيارتكم وثقتكم بنا" 
                        : "شكراً لتعاملكم معنا";
                    g.DrawString(footerTextText, small, Brushes.Gray, new RectangleF(0, y, pageW, 20), center);
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

        private void DrawColHeader(Graphics g, Font f, string text, int x, int w, int y, Brush brush = null)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(text, f, brush ?? Brushes.DarkBlue, new RectangleF(x, y, w, 18), sf);
        }

        private void DrawColCell(Graphics g, Font f, string text, int x, int w, int y)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, 18), sf);
        }
    }
}

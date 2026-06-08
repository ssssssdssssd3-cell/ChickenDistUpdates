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
        private bool _directPrint = false;

        public FrmPrintSale(int saleID, bool directPrint = false)
        {
            _saleID = saleID;
            _directPrint = directPrint;
            LoadData();
            DoPrint();
        }

        private void LoadData()
        {
            var dt = DbHelper.Query(@"
                SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType, s.ClientID, s.TotalAmount, s.Notes,
                       COALESCE(s.DiscountAmount, 0) AS DiscountAmount, COALESCE(s.DiscountPct, 0) AS DiscountPct,
                       CASE WHEN s.ClientID IS NULL AND s.SaleType = 'Cash' THEN N'عميل نقدي عشوائي' ELSE COALESCE(c.ClientName, N'---') END AS ClientName,
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
            bool isThermal = AppConfig.ThermalPrinterEnabled && _directPrint;

            if (isThermal)
            {
                pd.PrinterSettings.PrinterName = AppConfig.ThermalPrinterName;
                int paperWidth = (AppConfig.ThermalPaperWidth == 58) ? 220 : 300; // width in hundredths of an inch
                pd.DefaultPageSettings.PaperSize = new PaperSize("ThermalRoll", paperWidth, 1200);
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
            }
            else
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);
                pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
            }

            pd.BeginPrint += (s, e) => 
            {
                _printItemIndex = 0;
                _runningTotal = 0;
            };

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                int pageW = e.PageBounds.Width;
                int margin = isThermal ? 10 : 20;
                int y = isThermal ? 10 : 20;

                // ===== Fonts =====
                var boldBig = new Font("Arial", isThermal ? 11 : 14, FontStyle.Bold);
                var bold = new Font("Arial", isThermal ? 9 : 10, FontStyle.Bold);
                var normal = new Font("Arial", isThermal ? 8 : 9);
                var small = new Font("Arial", isThermal ? 7 : 8);

                // ===== Colors =====
                Brush textBrush = isThermal ? Brushes.Black : Brushes.DarkBlue;
                Brush valBrush = Brushes.Black;
                Pen borderPen = isThermal ? Pens.Black : new Pen(Color.DarkBlue, 1.5f);

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };

                // ===== Header =====
                g.DrawString("فاتورة مبيعات", boldBig, textBrush, new RectangleF(0, y, pageW, isThermal ? 22 : 30), center); y += isThermal ? 22 : 30;
                g.DrawString(AppConfig.CompanyName, bold, valBrush, new RectangleF(0, y, pageW, isThermal ? 18 : 22), center); y += isThermal ? 20 : 25;
                g.DrawLine(borderPen, margin, y, pageW - margin, y); y += 10;

                // ===== Sale Info (RTL) =====
                if (_saleRow != null)
                {
                    // Date
                    g.DrawString($"التاريخ: {Convert.ToDateTime(_saleRow["SaleDate"]):dd/MM/yyyy}",
                        normal, valBrush, new RectangleF(0, y, pageW - margin, 18), right);
                    g.DrawString($"رقم الفاتورة: {_saleRow["SaleCode"]}",
                        normal, valBrush, margin, y);
                    y += 18;

                    string typeLabel = _saleRow["SaleType"].ToString() == "Credit" ? "آجل"
                                     : _saleRow["SaleType"].ToString() == "Cash"   ? "نقدي"
                                     : "تحميل مندوب";
                    string driverText = _saleRow["DriverName"].ToString() != "---" ? $" | المندوب: {_saleRow["DriverName"]}" : "";
                    
                    if (isThermal)
                    {
                        g.DrawString($"العميل: {_saleRow["ClientName"]}", normal, valBrush, new RectangleF(0, y, pageW - margin, 18), right);
                        y += 18;
                        g.DrawString($"النوع: {typeLabel}{driverText}", normal, valBrush, new RectangleF(0, y, pageW - margin, 18), right);
                        y += 22;
                    }
                    else
                    {
                        g.DrawString($"العميل: {_saleRow["ClientName"]}{driverText}   |   النوع: {typeLabel}",
                            normal, valBrush, new RectangleF(0, y, pageW - margin, 20), right);
                        y += 25;
                    }
                }

                // ===== Table Header =====
                int xTotal, xDiscount, xPrice, xQty, xProduct;
                int colWTotal, colWDiscount, colWPrice, colWQty, colWProduct;

                if (isThermal)
                {
                    xTotal = margin;
                    colWTotal = (AppConfig.ThermalPaperWidth == 58) ? 45 : 60;

                    xPrice = xTotal + colWTotal;
                    colWPrice = (AppConfig.ThermalPaperWidth == 58) ? 40 : 50;

                    xQty = xPrice + colWPrice;
                    colWQty = (AppConfig.ThermalPaperWidth == 58) ? 35 : 45;

                    xProduct = xQty + colWQty;
                    colWProduct = pageW - margin - xProduct;

                    xDiscount = 0;
                    colWDiscount = 0;

                    DrawColHeader(g, bold, "الإجمالي", xTotal, colWTotal, y, textBrush);
                    DrawColHeader(g, bold, "السعر", xPrice, colWPrice, y, textBrush);
                    DrawColHeader(g, bold, "الكمية", xQty, colWQty, y, textBrush);
                    DrawColHeader(g, bold, "الصنف", xProduct, colWProduct, y, textBrush);
                }
                else
                {
                    xTotal    = margin;
                    xDiscount = 105;
                    xPrice    = 170;
                    xQty      = 255;
                    xProduct  = 340;

                    colWTotal    = 80;
                    colWDiscount = 60;
                    colWPrice    = 80;
                    colWQty      = 80;
                    colWProduct  = 220;

                    DrawColHeader(g, bold, "الإجمالي",  xTotal,    colWTotal,    y, textBrush);
                    DrawColHeader(g, bold, "الخصم",     xDiscount, colWDiscount, y, textBrush);
                    DrawColHeader(g, bold, "السعر",     xPrice,    colWPrice,    y, textBrush);
                    DrawColHeader(g, bold, "الكمية",    xQty,      colWQty,      y, textBrush);
                    DrawColHeader(g, bold, "الصنف",     xProduct,  colWProduct,  y, textBrush);
                }
                
                y += 18;
                g.DrawLine(isThermal ? Pens.Black : Pens.Gray, margin, y, pageW - margin, y); y += 4;

                // ===== Items =====
                if (_items != null)
                {
                    while (_printItemIndex < _items.Rows.Count)
                    {
                        if (y + (isThermal ? 80 : 150) > e.PageBounds.Height)
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
                        DrawColCell(g, normal, price.ToString("N2"),   xPrice,    colWPrice,    y);
                        DrawColCell(g, normal, qty.ToString("N2"),     xQty,      colWQty,      y);
                        DrawColCell(g, normal, r["ProductName"].ToString(), xProduct, colWProduct,  y);
                        
                        if (!isThermal)
                        {
                            DrawColCell(g, normal, discText,           xDiscount, colWDiscount, y);
                        }

                        y += isThermal ? 16 : 18;

                        // For thermal: If item has discount, print it on next line indented
                        if (isThermal && (itemDiscPct > 0 || itemDiscAmt > 0))
                        {
                            string discountDesc = itemDiscPct > 0 ? $"خصم {itemDiscPct:0.##}%" : $"خصم {itemDiscAmt:N2} جنيه";
                            g.DrawString($"* {discountDesc}", small, Brushes.Gray, new RectangleF(xProduct, y, colWProduct, 14), right);
                            y += 14;
                        }

                        _runningTotal += tot;
                        _printItemIndex++;
                    }
                }
                
                e.HasMorePages = false;

                g.DrawLine(borderPen, margin, y, pageW - margin, y); y += 8;

                // Get invoice discount details
                decimal invDiscountAmt = 0;
                decimal invDiscountPct = 0;
                decimal netAmount = _runningTotal;
                if (_saleRow != null)
                {
                    invDiscountAmt = Convert.ToDecimal(_saleRow["DiscountAmount"]);
                    invDiscountPct = Convert.ToDecimal(_saleRow["DiscountPct"]);
                    netAmount = Convert.ToDecimal(_saleRow["TotalAmount"]);
                }

                if (invDiscountAmt > 0)
                {
                    g.DrawString($"إجمالي الأصناف: {_runningTotal:N2} جنيه",
                        normal, valBrush, new RectangleF(0, y, pageW - margin, 18), right); y += 18;
                    g.DrawString($"خصم الفاتورة: {invDiscountAmt:N2} جنيه ({invDiscountPct:0.##}%)",
                        normal, valBrush, new RectangleF(0, y, pageW - margin, 18), right); y += 18;
                }

                // Net Amount (Final invoice amount)
                g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه",
                    bold, isThermal ? Brushes.Black : Brushes.DarkRed, new RectangleF(0, y, pageW - margin, 22), right); y += 22;

                // ===== Balance Section =====
                if (_saleRow != null && _saleRow["ClientID"] != DBNull.Value)
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

                        g.DrawLine(isThermal ? Pens.Black : Pens.LightGray, margin, y, pageW - margin, y); y += 8;

                        if (isThermal)
                        {
                            g.DrawString($"الرصيد السابق: {previousBalance:N2} ج", normal, valBrush, new RectangleF(0, y, pageW - margin, 18), right);
                            y += 18;
                            g.DrawString($"المدفوع اليوم: {paymentToday:N2} ج", normal, valBrush, new RectangleF(0, y, pageW - margin, 18), right);
                            y += 18;
                            g.DrawString($"الرصيد الحالي: {currentBalance:N2} ج", bold, textBrush, new RectangleF(0, y, pageW - margin, 20), right);
                            y += 22;
                        }
                        else
                        {
                            g.DrawString($"الرصيد السابق: {previousBalance:N2} جنيه",
                                bold, valBrush, new RectangleF(0, y, pageW - margin, 20), right);
                            g.DrawString($"المدفوع (التحصيل): {paymentToday:N2} جنيه",
                                bold, valBrush, margin, y);
                            y += 25;

                            g.DrawString($"الرصيد الحالي: {currentBalance:N2} جنيه",
                                boldBig, textBrush, new RectangleF(0, y, pageW, 28), center);
                            y += 35;
                        }
                    }
                }

                g.DrawLine(isThermal ? Pens.Black : Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                g.DrawString("شكراً لتعاملكم معنا", small, Brushes.Gray, new RectangleF(0, y, pageW, 18), center);
            };

            if (isThermal)
            {
                try
                {
                    pd.Print();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ فشلت الطباعة المباشرة:\n{ex.Message}\nسيتم عرض معاينة الطباعة بدلاً من ذلك.", 
                        "خطأ طباعة حرارية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ShowPreview(pd);
                }
            }
            else
            {
                ShowPreview(pd);
            }
        }

        private void ShowPreview(PrintDocument pd)
        {
            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 650,
                Height = 700,
                Text = "معاينة الطباعة"
            };
            preview.ShowDialog();
        }

        private void DrawColHeader(Graphics g, Font f, string text, int x, int w, int y, Brush brush)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(text, f, brush, new RectangleF(x, y, w, 18), sf);
        }

        private void DrawColCell(Graphics g, Font f, string text, int x, int w, int y)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, 18), sf);
        }
    }
}

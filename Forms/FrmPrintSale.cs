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

        public FrmPrintSale(int saleID)
        {
            _saleID = saleID;
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
            pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 14, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var normal = new Font("Arial", 9);
                var small = new Font("Arial", 8);

                int pageW = e.PageBounds.Width;
                int margin = 20;
                int y = 20;

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };

                // ===== Header =====
                g.DrawString("فاتورة مبيعات", boldBig, Brushes.DarkBlue, new RectangleF(0, y, pageW, 30), center); y += 30;
                g.DrawString("شركة توزيع الكتاكيت", bold, Brushes.Black, new RectangleF(0, y, pageW, 22), center); y += 25;
                g.DrawLine(new Pen(Color.DarkBlue, 2), margin, y, pageW - margin, y); y += 10;

                // ===== Sale Info (RTL: Info on right, Code on left) =====
                if (_saleRow != null)
                {
                    // Date on the right, invoice number on the left
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

                // ===== Table Header - RTL: Price right - Name left =====
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

                // Draw header cells right-aligned inside each column box
                DrawColHeader(g, bold, "الإجمالي",  xTotal,    colWTotal,    y);
                DrawColHeader(g, bold, "الخصم",     xDiscount, colWDiscount, y);
                DrawColHeader(g, bold, "السعر",     xPrice,    colWPrice,    y);
                DrawColHeader(g, bold, "الكمية",    xQty,      colWQty,      y);
                DrawColHeader(g, bold, "الصنف",     xProduct,  colWProduct,  y);
                y += 20;
                g.DrawLine(Pens.Gray, margin, y, pageW - margin, y); y += 4;

                // ===== Items =====
                decimal itemsTotal = 0;
                if (_items != null)
                    foreach (DataRow r in _items.Rows)
                    {
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
                        itemsTotal += tot; y += 18;
                    }

                g.DrawLine(new Pen(Color.DarkBlue, 1.5f), margin, y, pageW - margin, y); y += 8;

                // Get invoice discount details
                decimal invDiscountAmt = 0;
                decimal invDiscountPct = 0;
                decimal netAmount = itemsTotal;
                if (_saleRow != null)
                {
                    invDiscountAmt = Convert.ToDecimal(_saleRow["DiscountAmount"]);
                    invDiscountPct = Convert.ToDecimal(_saleRow["DiscountPct"]);
                    netAmount = Convert.ToDecimal(_saleRow["TotalAmount"]);
                }

                if (invDiscountAmt > 0)
                {
                    g.DrawString($"إجمالي الأصناف: {itemsTotal:N2} جنيه",
                        normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                    g.DrawString($"خصم الفاتورة: {invDiscountAmt:N2} جنيه ({invDiscountPct:0.##}%)",
                        normal, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right); y += 20;
                }

                // Net Amount (Final invoice amount)
                g.DrawString($"صافي الفاتورة: {netAmount:N2} جنيه",
                    bold, Brushes.DarkRed, new RectangleF(0, y, pageW - margin, 25), right); y += 25;

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

                        g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;

                        // RTL layout: previous balance on right, payment on left
                        g.DrawString($"الرصيد السابق: {previousBalance:N2} جنيه",
                            bold, Brushes.Black, new RectangleF(0, y, pageW - margin, 20), right);
                        g.DrawString($"المدفوع (التحصيل): {paymentToday:N2} جنيه",
                            bold, Brushes.Black, margin, y);
                        y += 25;

                        // Current Balance - centered and bold
                        g.DrawString($"الرصيد الحالي: {currentBalance:N2} جنيه",
                            boldBig, Brushes.DarkBlue, new RectangleF(0, y, pageW, 28), center);
                        y += 35;
                    }
                }

                g.DrawLine(Pens.LightGray, margin, y, pageW - margin, y); y += 8;
                g.DrawString("شكراً لتعاملكم معنا", small, Brushes.Gray, new RectangleF(0, y, pageW, 20), center);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 650,
                Height = 700,
                Text = "معاينة الطباعة"
            };
            preview.ShowDialog();
        }

        // Helper: draw a right-aligned header label inside a column box
        private void DrawColHeader(Graphics g, Font f, string text, int x, int w, int y)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(text, f, Brushes.DarkBlue, new RectangleF(x, y, w, 18), sf);
        }

        // Helper: draw a right-aligned data cell inside a column box
        private void DrawColCell(Graphics g, Font f, string text, int x, int w, int y)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, 18), sf);
        }
    }
}

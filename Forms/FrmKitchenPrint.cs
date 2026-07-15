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
    /// فئة لطباعة بون تحضير مستقل للمطبخ بدون أسعار
    /// </summary>
    public class FrmKitchenPrint
    {
        private int _saleID;
        private DataRow _saleRow;
        private DataTable _items;
        private int _printItemIndex = 0;

        public FrmKitchenPrint(int saleID)
        {
            _saleID = saleID;
            LoadData();
            if (_saleRow != null && _items != null && _items.Rows.Count > 0)
            {
                DoPrint();
            }
        }

        private void LoadData()
        {
            var dt = DbHelper.Query(@"
                SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType, s.Notes,
                       COALESCE(s.OrderType, N'Takeaway') AS OrderType,
                       COALESCE(s.TableNumber, N'') AS TableNumber,
                       COALESCE(c.ClientName, N'---') AS ClientName,
                       COALESCE(e.EmpName, N'---') AS DriverName
                 FROM Sales s
                 LEFT JOIN Clients c ON s.ClientID = c.ClientID
                 LEFT JOIN Employees e ON s.DriverID = e.EmpID
                 WHERE s.SaleID = @id", DbHelper.P("@id", _saleID));
            if (dt.Rows.Count > 0)
                _saleRow = dt.Rows[0];
            
            _items = DbHelper.Query(@"
                SELECT si.ProductID, p.ProductName, si.Quantity, si.UnitName,
                       COALESCE(si.KitchenNotes, N'') AS KitchenNotes
                FROM SaleItems si
                INNER JOIN Products p ON si.ProductID = p.ProductID
                WHERE si.SaleID = @sid", DbHelper.P("@sid", _saleID));
        }

        private void DoPrint()
        {
            try
            {
                var pd = new PrintDocument();
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 300, 1000);
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
                
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);

                pd.BeginPrint += (s, e) => 
                {
                    _printItemIndex = 0;
                };

                pd.PrintPage += Pd_PrintPage;

                pd.Print();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmKitchenPrint.DoPrint failed", ex);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            float y = 10;
            float margin = 10;
            float usableWidth = 280;

            Font fTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
            Font fHeader = new Font("Segoe UI", 12f, FontStyle.Bold);
            Font fBody = new Font("Segoe UI", 10f);
            Font fBodyBold = new Font("Segoe UI", 10f, FontStyle.Bold);
            Font fSmall = new Font("Segoe UI", 9f);
            Font fSmallItalic = new Font("Segoe UI", 9f, FontStyle.Italic);

            StringFormat formatCenter = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat formatRight = new StringFormat { Alignment = StringAlignment.Far };
            StringFormat formatLeft = new StringFormat { Alignment = StringAlignment.Near };

            g.DrawString("⚡ بون تحضير المطبخ ⚡", fHeader, Brushes.Black, new RectangleF(margin, y, usableWidth, 25), formatCenter);
            y += 28;

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 8;

            string orderCode = _saleRow["SaleCode"]?.ToString() ?? _saleID.ToString();
            DateTime orderDate = Convert.ToDateTime(_saleRow["SaleDate"]);
            g.DrawString($"طلب رقم: #{orderCode}", fBodyBold, Brushes.Black, margin, y);
            y += 20;
            g.DrawString($"التاريخ: {orderDate:yyyy-MM-dd   hh:mm tt}", fSmall, Brushes.Black, margin, y);
            y += 24;

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 8;

            string orderType = _saleRow["OrderType"].ToString();
            string tableNum = _saleRow["TableNumber"].ToString();
            string orderTypeAr = orderType == "DineIn" ? "صالة" : orderType == "Delivery" ? "توصيل" : "تيك اواي";

            g.DrawString($"نوع الطلب: {orderTypeAr}", fHeader, Brushes.Black, margin, y);
            y += 22;

            if (orderType == "DineIn" && !string.IsNullOrEmpty(tableNum))
            {
                g.FillRectangle(Brushes.LightGray, margin, y, usableWidth, 40);
                g.DrawRectangle(Pens.Black, margin, y, usableWidth, 40);
                g.DrawString($"طاولة رقم [ {tableNum} ]", fTitle, Brushes.Black, new RectangleF(margin, y + 4, usableWidth, 32), formatCenter);
                y += 48;
            }
            else if (orderType == "Delivery")
            {
                string driver = _saleRow["DriverName"].ToString();
                g.DrawString($"الطيار: {driver}", fBodyBold, Brushes.Black, margin, y);
                y += 20;
            }

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 8;

            g.DrawString("الصنف", fBodyBold, Brushes.Black, margin, y);
            g.DrawString("الكمية", fBodyBold, Brushes.Black, usableWidth - 20, y, formatRight);
            y += 22;

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 6;

            while (_printItemIndex < _items.Rows.Count)
            {
                DataRow item = _items.Rows[_printItemIndex];
                string name = item["ProductName"].ToString();
                decimal qty = Convert.ToDecimal(item["Quantity"]);
                string unit = item["UnitName"]?.ToString() ?? "";
                string note = item["KitchenNotes"].ToString();

                string qtyStr = qty.ToString("G");
                if (!string.IsNullOrEmpty(unit)) qtyStr += $" {unit}";

                SizeF sizeName = g.MeasureString(name, fBody, (int)(usableWidth - 80));
                g.DrawString(name, fBodyBold, Brushes.Black, new RectangleF(margin, y, usableWidth - 80, sizeName.Height));
                g.DrawString(qtyStr, fBodyBold, Brushes.Black, usableWidth, y, formatRight);
                
                y += sizeName.Height + 2;

                if (!string.IsNullOrEmpty(note))
                {
                    g.DrawString($"** ملاحظة: {note}", fSmallItalic, Brushes.Red, margin + 15, y);
                    y += 18;
                }

                y += 6;
                _printItemIndex++;

                if (y > e.MarginBounds.Bottom - 50)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            e.HasMorePages = false;

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 8;

            string generalNotes = _saleRow["Notes"]?.ToString() ?? "";
            // تصفية النصوص الداخلية (POS / POS_DRAFT) من الطباعة
            bool isInternalNote = string.IsNullOrWhiteSpace(generalNotes)
                || generalNotes == "POS"
                || generalNotes == "POS_DRAFT"
                || generalNotes.StartsWith("POS", StringComparison.OrdinalIgnoreCase);
            if (!isInternalNote)
            {
                g.DrawString($"ملاحظات: {generalNotes}", fSmall, Brushes.Black, margin, y);
                y += 22;
            }

            g.DrawString("بالهناء والشفاء", fBody, Brushes.Black, new RectangleF(margin, y, usableWidth, 20), formatCenter);
            y += 20;
        }
    }
}

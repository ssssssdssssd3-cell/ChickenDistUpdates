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
                // 8 سم = 315 وحدة (1/100 إنش) — تصحيح عرض الورق
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 315, 1200);
                pd.DefaultPageSettings.Margins = new Margins(5, 5, 10, 10);
                
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
            float y = 8;
            float margin = 5;
            float usableWidth = 305; // 315 - 5 - 5

            Font fTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
            Font fHeader = new Font("Segoe UI", 11f, FontStyle.Bold);
            Font fBody = new Font("Segoe UI", 9.5f);
            Font fBodyBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            Font fSmall = new Font("Segoe UI", 8.5f);
            Font fSmallItalic = new Font("Segoe UI", 8.5f, FontStyle.Italic);

            StringFormat formatCenter = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat formatRight = new StringFormat { Alignment = StringAlignment.Far };
            StringFormat formatLeft = new StringFormat { Alignment = StringAlignment.Near };

            g.DrawString("⚡ بون تحضير المطبخ ⚡", fHeader, Brushes.Black, new RectangleF(margin, y, usableWidth, 24), formatCenter);
            y += 26;

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 7;

            string orderCode = _saleRow["SaleCode"]?.ToString() ?? _saleID.ToString();
            DateTime orderDate = Convert.ToDateTime(_saleRow["SaleDate"]);
            g.DrawString($"طلب رقم: #{orderCode}", fBodyBold, Brushes.Black, new RectangleF(margin, y, usableWidth, 20), formatRight);
            y += 20;
            g.DrawString($"التاريخ: {orderDate:dd-MM-yyyy  hh:mm tt}", fSmall, Brushes.Black, new RectangleF(margin, y, usableWidth, 18), formatRight);
            y += 22;

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 7;

            string orderType = _saleRow["OrderType"].ToString();
            string tableNum = _saleRow["TableNumber"].ToString();
            string orderTypeAr = orderType == "DineIn" ? "صالة" : orderType == "Delivery" ? "توصيل" : "تيك اواي";

            g.DrawString($"نوع الطلبة: {orderTypeAr}", fHeader, Brushes.Black, new RectangleF(margin, y, usableWidth, 22), formatRight);
            y += 22;

            if (orderType == "DineIn" && !string.IsNullOrEmpty(tableNum))
            {
                g.FillRectangle(Brushes.LightGray, margin, y, usableWidth, 38);
                g.DrawRectangle(Pens.Black, margin, y, usableWidth, 38);
                g.DrawString($"طاولة رقم [ {tableNum} ]", fTitle, Brushes.Black, new RectangleF(margin, y + 4, usableWidth, 30), formatCenter);
                y += 46;
            }
            else if (orderType == "Delivery")
            {
                string driver = _saleRow["DriverName"].ToString();
                g.DrawString($"الطيار: {driver}", fBodyBold, Brushes.Black, new RectangleF(margin, y, usableWidth, 20), formatRight);
                y += 20;
            }

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 7;

            // رؤوس أعمدة جدول الأصناف — عمود الكمية على اليسار وعمود الصنف على اليمين
            float colQtyWidth = 90f;  // عرض عمود الكمية
            float colNameWidth = usableWidth - colQtyWidth - 4;
            g.DrawString("الصنف", fBodyBold, Brushes.Black, new RectangleF(margin + colQtyWidth + 4, y, colNameWidth, 20), formatRight);
            g.DrawString("الكمية", fBodyBold, Brushes.Black, new RectangleF(margin, y, colQtyWidth, 20), formatLeft);
            y += 22;

            g.DrawLine(Pens.Black, margin, y, usableWidth + margin, y);
            y += 5;

            while (_printItemIndex < _items.Rows.Count)
            {
                DataRow item = _items.Rows[_printItemIndex];
                string name = item["ProductName"].ToString();
                decimal qty = Convert.ToDecimal(item["Quantity"]);
                string unit = item["UnitName"]?.ToString() ?? "";
                string note = item["KitchenNotes"].ToString();

                string qtyStr = qty.ToString("G");
                if (!string.IsNullOrEmpty(unit)) qtyStr += $" {unit}";

                SizeF sizeName = g.MeasureString(name, fBodyBold, (int)colNameWidth);
                float rowHeight = Math.Max(sizeName.Height, 18f);

                g.DrawString(name, fBodyBold, Brushes.Black, new RectangleF(margin + colQtyWidth + 4, y, colNameWidth, sizeName.Height), formatRight);
                g.DrawString(qtyStr, fBodyBold, Brushes.Black, new RectangleF(margin, y, colQtyWidth, rowHeight), formatLeft);
                
                y += rowHeight + 2;

                if (!string.IsNullOrEmpty(note))
                {
                    g.DrawString($"** ملاحظة: {note}", fSmallItalic, Brushes.Red, new RectangleF(margin, y, usableWidth, 18), formatRight);
                    y += 18;
                }

                y += 5;
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
                g.DrawString($"ملاحظات: {generalNotes}", fSmall, Brushes.Black, new RectangleF(margin, y, usableWidth, 22), formatRight);
                y += 22;
            }

            g.DrawString("بالهناء والشفاء", fBody, Brushes.Black, new RectangleF(margin, y, usableWidth, 20), formatCenter);
            y += 20;
        }
    }
}

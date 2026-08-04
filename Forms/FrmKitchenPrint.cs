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
                
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);

                // محاولة قراءة عرض الورق الفعلي من الطابعة
                // لو الطابعة 58mm ≈ 228 وحدة، لو 80mm ≈ 315 وحدة
                int paperW = 228; // افتراضي 58mm — الأكثر شيوعاً
                try
                {
                    int driverW = pd.DefaultPageSettings.PaperSize.Width;
                    if (driverW > 180 && driverW < 400) paperW = driverW;
                }
                catch { /* استخدم الافتراضي */ }

                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", paperW, 1200);
                pd.DefaultPageSettings.Margins = new Margins(2, 2, 5, 5);

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

            // === حساب العرض الفعلي القابل للطباعة بشكل ديناميكي ===
            float pageWidth;
            try
            {
                // VisibleClipBounds تعطي المساحة الفعلية المتاحة من الطابعة
                pageWidth = g.VisibleClipBounds.Width;
            }
            catch
            {
                pageWidth = 220f; // fallback لطابعة 58mm
            }

            float margin = 8;
            float usableWidth = pageWidth - (margin * 2);
            if (usableWidth < 100) usableWidth = 200; // حد أدنى آمن
            float y = 8;

            // تعديل أحجام الخطوط حسب عرض الورق
            bool isNarrow = (pageWidth < 260); // طابعة 58mm
            float titleSize = isNarrow ? 11f : 15f;
            float headerSize = isNarrow ? 9f : 11f;
            float bodySize = isNarrow ? 8f : 9.5f;
            float smallSize = isNarrow ? 7f : 8.5f;

            Font fTitle = new Font("Segoe UI", titleSize, FontStyle.Bold);
            Font fHeader = new Font("Segoe UI", headerSize, FontStyle.Bold);
            Font fBody = new Font("Segoe UI", bodySize);
            Font fBodyBold = new Font("Segoe UI", bodySize, FontStyle.Bold);
            Font fSmall = new Font("Segoe UI", smallSize);
            Font fSmallItalic = new Font("Segoe UI", smallSize, FontStyle.Italic);

            StringFormat formatCenter = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat formatRight = new StringFormat { Alignment = StringAlignment.Far };
            StringFormat formatLeft = new StringFormat { Alignment = StringAlignment.Near };

            // العنوان
            g.DrawString("⚡ بون تحضير المطبخ ⚡", fHeader, Brushes.Black, new RectangleF(margin, y, usableWidth, 22), formatCenter);
            y += 24;

            g.DrawLine(Pens.Black, margin, y, margin + usableWidth, y);
            y += 5;

            // رقم الطلب والتاريخ
            string orderCode = _saleRow["SaleCode"]?.ToString() ?? _saleID.ToString();
            DateTime orderDate = Convert.ToDateTime(_saleRow["SaleDate"]);
            g.DrawString($"طلب رقم: #{orderCode}", fBodyBold, Brushes.Black, new RectangleF(margin, y, usableWidth, 18), formatRight);
            y += 18;
            g.DrawString($"التاريخ: {orderDate:dd/MM/yyyy hh:mm tt}", fSmall, Brushes.Black, new RectangleF(margin, y, usableWidth, 16), formatRight);
            y += 18;

            g.DrawLine(Pens.Black, margin, y, margin + usableWidth, y);
            y += 5;

            // نوع الطلب
            string orderType = _saleRow["OrderType"].ToString();
            string tableNum = _saleRow["TableNumber"].ToString();
            string orderTypeAr = orderType == "DineIn" ? "صالة" : orderType == "Delivery" ? "توصيل" : "تيك اواي";

            g.DrawString($"نوع الطلبة: {orderTypeAr}", fHeader, Brushes.Black, new RectangleF(margin, y, usableWidth, 20), formatRight);
            y += 22;

            if (orderType == "DineIn" && !string.IsNullOrEmpty(tableNum))
            {
                g.FillRectangle(Brushes.LightGray, margin, y, usableWidth, 32);
                g.DrawRectangle(Pens.Black, margin, y, usableWidth, 32);
                g.DrawString($"طاولة رقم [ {tableNum} ]", fTitle, Brushes.Black, new RectangleF(margin, y + 2, usableWidth, 28), formatCenter);
                y += 38;
            }
            else if (orderType == "Delivery")
            {
                string driver = _saleRow["DriverName"].ToString();
                g.DrawString($"الطيار: {driver}", fBodyBold, Brushes.Black, new RectangleF(margin, y, usableWidth, 18), formatRight);
                y += 20;
            }

            g.DrawLine(Pens.Black, margin, y, margin + usableWidth, y);
            y += 5;

            // === جدول الأصناف ===
            // عمود الكمية يكون ضيق (يسار) — عمود الصنف يكون عريض (يمين)
            float colQtyWidth = isNarrow ? 55f : 75f;
            float gap = 4f;
            float colNameWidth = usableWidth - colQtyWidth - gap;

            // رؤوس الأعمدة
            g.DrawString("الصنف", fBodyBold, Brushes.Black, new RectangleF(margin + colQtyWidth + gap, y, colNameWidth, 18), formatRight);
            g.DrawString("الكمية", fBodyBold, Brushes.Black, new RectangleF(margin, y, colQtyWidth, 18), formatLeft);
            y += 20;

            g.DrawLine(Pens.Black, margin, y, margin + usableWidth, y);
            y += 4;

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
                float rowHeight = Math.Max(sizeName.Height, 16f);

                g.DrawString(name, fBodyBold, Brushes.Black, new RectangleF(margin + colQtyWidth + gap, y, colNameWidth, sizeName.Height), formatRight);
                g.DrawString(qtyStr, fBodyBold, Brushes.Black, new RectangleF(margin, y, colQtyWidth, rowHeight), formatLeft);
                
                y += rowHeight + 2;

                if (!string.IsNullOrEmpty(note))
                {
                    g.DrawString($"** ملاحظة: {note}", fSmallItalic, Brushes.Red, new RectangleF(margin, y, usableWidth, 16), formatRight);
                    y += 16;
                }

                y += 4;
                _printItemIndex++;

                if (y > e.MarginBounds.Bottom - 40)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            e.HasMorePages = false;

            g.DrawLine(Pens.Black, margin, y, margin + usableWidth, y);
            y += 6;

            // ملاحظات عامة
            string generalNotes = _saleRow["Notes"]?.ToString() ?? "";
            bool isInternalNote = string.IsNullOrWhiteSpace(generalNotes)
                || generalNotes == "POS"
                || generalNotes == "POS_DRAFT"
                || generalNotes.StartsWith("POS", StringComparison.OrdinalIgnoreCase);
            if (!isInternalNote)
            {
                g.DrawString($"ملاحظات: {generalNotes}", fSmall, Brushes.Black, new RectangleF(margin, y, usableWidth, 20), formatRight);
                y += 20;
            }

            g.DrawString("بالهناء والشفاء", fBody, Brushes.Black, new RectangleF(margin, y, usableWidth, 18), formatCenter);
            y += 18;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    public static class PdfReportHelper
    {
        /// <summary>
        /// يُنشئ ملف PDF قياسي عالي الجودة يحتوي على صفحات من الصور الممررة
        /// متوافق 100% مع جميع برامج قراءة الـ PDF والواتساب ومتصفحات الويب
        /// </summary>
        public static void SaveBitmapsAsPdf(List<Bitmap> pages, string outputPdfPath)
        {
            if (pages == null || pages.Count == 0)
                throw new ArgumentException("No pages provided for PDF creation.");

            // Page dimensions in PDF points (A4: 595.28 x 841.89 points)
            double pdfPageWidth = 595.28;
            double pdfPageHeight = 841.89;

            var stream = new MemoryStream();
            var offsets = new List<long>();
            var enc = Encoding.ASCII;

            // 1. Header
            byte[] header = enc.GetBytes("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
            stream.Write(header, 0, header.Length);

            int pageCount = pages.Count;
            // Object IDs allocation:
            // 1: Catalog
            // 2: Pages Tree
            // For each page i (0 to pageCount-1):
            //   Page Obj: 3 + i*3
            //   Content Obj: 3 + i*3 + 1
            //   Image XObject Obj: 3 + i*3 + 2
            int totalObjects = 2 + (pageCount * 3);

            // Placeholder for object 1 (Catalog)
            offsets.Add(stream.Position);
            byte[] obj1 = enc.GetBytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
            stream.Write(obj1, 0, obj1.Length);

            // Object 2: Pages Tree
            offsets.Add(stream.Position);
            var kidsSb = new StringBuilder();
            kidsSb.Append("2 0 obj\n<< /Type /Pages /Count " + pageCount + " /Kids [");
            for (int i = 0; i < pageCount; i++)
            {
                int pageObjId = 3 + (i * 3);
                kidsSb.Append(" " + pageObjId + " 0 R");
            }
            kidsSb.Append(" ] >>\nendobj\n");
            byte[] obj2 = enc.GetBytes(kidsSb.ToString());
            stream.Write(obj2, 0, obj2.Length);

            // For each page
            for (int i = 0; i < pageCount; i++)
            {
                var bmp = pages[i];
                int pageObjId = 3 + (i * 3);
                int contentObjId = pageObjId + 1;
                int imageObjId = pageObjId + 2;

                // Compress bitmap to JPEG
                byte[] jpegBytes;
                using (var msJpeg = new MemoryStream())
                {
                    var encoder = GetEncoder(ImageFormat.Jpeg);
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 92L);
                    bmp.Save(msJpeg, encoder, encoderParams);
                    jpegBytes = msJpeg.ToArray();
                }

                // Page Object
                offsets.Add(stream.Position);
                string pageObjStr = $"{pageObjId} 0 obj\n" +
                                    $"<< /Type /Page /Parent 2 0 R\n" +
                                    $"/MediaBox [0 0 {pdfPageWidth:F2} {pdfPageHeight:F2}]\n" +
                                    $"/Contents {contentObjId} 0 R\n" +
                                    $"/Resources << /XObject << /Im0 {imageObjId} 0 R >> /ProcSet [/PDF /ImageC] >>\n" +
                                    $">> \nendobj\n";
                byte[] pageObjBytes = enc.GetBytes(pageObjStr);
                stream.Write(pageObjBytes, 0, pageObjBytes.Length);

                // Content Stream (scales the image to fill the A4 page)
                string contentStreamData = $"q\n{pdfPageWidth:F2} 0 0 {pdfPageHeight:F2} 0 0 cm\n/Im0 Do\nQ\n";
                byte[] contentDataBytes = enc.GetBytes(contentStreamData);

                offsets.Add(stream.Position);
                string contentObjStr = $"{contentObjId} 0 obj\n<< /Length {contentDataBytes.Length} >>\nstream\n";
                byte[] contentObjBytes = enc.GetBytes(contentObjStr);
                stream.Write(contentObjBytes, 0, contentObjBytes.Length);
                stream.Write(contentDataBytes, 0, contentDataBytes.Length);
                byte[] contentEndBytes = enc.GetBytes("\nendstream\nendobj\n");
                stream.Write(contentEndBytes, 0, contentEndBytes.Length);

                // Image XObject
                offsets.Add(stream.Position);
                string imgHeaderStr = $"{imageObjId} 0 obj\n" +
                                      $"<< /Type /XObject\n" +
                                      $"/Subtype /Image\n" +
                                      $"/Width {bmp.Width}\n" +
                                      $"/Height {bmp.Height}\n" +
                                      $"/ColorSpace /DeviceRGB\n" +
                                      $"/BitsPerComponent 8\n" +
                                      $"/Filter /DCTDecode\n" +
                                      $"/Length {jpegBytes.Length}\n" +
                                      $">> \nstream\n";
                byte[] imgHeaderBytes = enc.GetBytes(imgHeaderStr);
                stream.Write(imgHeaderBytes, 0, imgHeaderBytes.Length);
                stream.Write(jpegBytes, 0, jpegBytes.Length);
                byte[] imgEndBytes = enc.GetBytes("\nendstream\nendobj\n");
                stream.Write(imgEndBytes, 0, imgEndBytes.Length);
            }

            // Cross-Reference Table
            long xrefOffset = stream.Position;
            var xrefSb = new StringBuilder();
            xrefSb.Append("xref\n");
            xrefSb.Append($"0 {offsets.Count + 1}\n");
            xrefSb.Append("0000000000 65535 f \n");
            foreach (var off in offsets)
            {
                xrefSb.Append(off.ToString("D10") + " 00000 n \n");
            }
            xrefSb.Append("trailer\n");
            xrefSb.Append($"<< /Size {offsets.Count + 1} /Root 1 0 R >>\n");
            xrefSb.Append("startxref\n");
            xrefSb.Append(xrefOffset.ToString() + "\n");
            xrefSb.Append("%%EOF\n");

            byte[] xrefBytes = enc.GetBytes(xrefSb.ToString());
            stream.Write(xrefBytes, 0, xrefBytes.Length);

            // Write to file
            File.WriteAllBytes(outputPdfPath, stream.ToArray());
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }

        /// <summary>
        /// توليد ملف PDF متكامل لكشف حساب أصناف العميل (Product Statement)
        /// </summary>
        public static string GenerateItemizedStatementPdf(string clientName, string clientPhone, DateTime fromDate, DateTime toDate, DataGridView dgItemized, string outputFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ProSoft_Reports");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string cleanName = MakeValidFileName(clientName);
                outputFilePath = Path.Combine(tempDir, $"كشف_حساب_أصناف_{cleanName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }

            // Render pages as 150 DPI A4 bitmaps (1240 x 1754 px)
            int width = 1240;
            int height = 1754;
            var pages = new List<Bitmap>();

            int rowCount = dgItemized != null ? dgItemized.Rows.Count : 0;
            int rowsPerPage = 28;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)rowCount / rowsPerPage));

            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
            var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

            for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
            {
                var bmp = new Bitmap(width, height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    // Border Frame
                    using (var pBorder = new Pen(Color.FromArgb(15, 45, 90), 3f))
                    using (var pThin = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
                    using (var pBlack = new Pen(Color.FromArgb(30, 41, 59), 1f))
                    using (var brNavy = new SolidBrush(Color.FromArgb(15, 45, 90)))
                    using (var brSecondary = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    using (var brHeaderRow = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    using (var brAlt = new SolidBrush(Color.FromArgb(248, 250, 252)))
                    using (var fTitle = new Font("Arial", 22f, FontStyle.Bold))
                    using (var fSub = new Font("Arial", 13f, FontStyle.Bold))
                    using (var fBold = new Font("Arial", 11.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Arial", 10.5f, FontStyle.Regular))
                    using (var fSmall = new Font("Arial", 9.5f, FontStyle.Regular))
                    {
                        g.DrawRectangle(pBorder, 20, 20, width - 40, height - 40);

                        int y = 40;

                        // Header Bar
                        using (var brHeaderGrad = new LinearGradientBrush(new Rectangle(25, 25, width - 50, 100), Color.FromArgb(15, 45, 90), Color.FromArgb(30, 64, 175), LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(brHeaderGrad, 25, 25, width - 50, 100);
                        }

                        string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة للتجارة والتوزيع";
                        g.DrawString(comp, fTitle, Brushes.White, new RectangleF(30, y, width - 60, 40), sfCenter);
                        g.DrawString("كشف حساب مسحوبات الأصناف التفصيلي (Detailed Product Statement)", fSub, Brushes.LightCyan, new RectangleF(30, y + 44, width - 60, 28), sfCenter);
                        y += 105;

                        // Info Meta Card
                        g.FillRectangle(brAlt, 40, y, width - 80, 80);
                        g.DrawRectangle(pThin, 40, y, width - 80, 80);
                        g.DrawLine(pThin, width / 2, y, width / 2, y + 80);

                        g.DrawString($"العميل: {clientName}", fBold, brNavy, new RectangleF(width / 2 + 20, y + 10, width / 2 - 60, 28), sfRight);
                        if (!string.IsNullOrWhiteSpace(clientPhone))
                        {
                            g.DrawString($"هاتف العميل: {clientPhone}", fNorm, Brushes.Black, new RectangleF(width / 2 + 20, y + 42, width / 2 - 60, 28), sfRight);
                        }

                        g.DrawString($"الفترة من: {fromDate:yyyy/MM/dd} إلى: {toDate:yyyy/MM/dd}", fBold, Brushes.DarkSlateGray, new RectangleF(50, y + 10, width / 2 - 80, 28), sfRight);
                        g.DrawString($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd hh:mm tt}", fSmall, Brushes.Gray, new RectangleF(50, y + 42, width / 2 - 80, 28), sfRight);

                        y += 95;

                        // Columns: كود(100) | اسم الصنف(370) | الوحدة(80) | المبيعات(110) | المرتجع(100) | صافي الكمية(120) | متوسط السعر(120) | صافي المبلغ(160)
                        int tLeft = 40;
                        int tWidth = width - 80;
                        int thH = 42;

                        g.FillRectangle(brNavy, tLeft, y, tWidth, thH);
                        g.DrawRectangle(pBorder, tLeft, y, tWidth, thH);

                        int[] colW = { 100, 370, 80, 110, 100, 120, 120, 160 };
                        string[] colHeaders = { "كود الصنف", "اسم الصنف", "الوحدة", "المبيعات", "المرتجع", "صافي الكمية", "متوسط السعر", "صافي المبلغ (ج)" };

                        int curX = tLeft + tWidth;
                        for (int c = 0; c < colHeaders.Length; c++)
                        {
                            curX -= colW[c];
                            g.DrawString(colHeaders[c], fBold, Brushes.White, new RectangleF(curX, y + 8, colW[c], thH - 8), sfCenter);
                            if (c > 0) g.DrawLine(Pens.White, curX, y, curX, y + thH);
                        }
                        y += thH;

                        // Table Rows
                        int startRow = pageIdx * rowsPerPage;
                        int endRow = Math.Min(rowCount, startRow + rowsPerPage);
                        int rowH = 36;
                        bool alt = false;

                        for (int r = startRow; r < endRow; r++)
                        {
                            var dgr = dgItemized.Rows[r];
                            if (alt) g.FillRectangle(brAlt, tLeft, y, tWidth, rowH);
                            g.DrawRectangle(pThin, tLeft, y, tWidth, rowH);

                            string code = dgr.Cells[0].Value?.ToString() ?? "";
                            string name = dgr.Cells[1].Value?.ToString() ?? "";
                            string unit = dgr.Cells[2].Value?.ToString() ?? "";
                            string sold = dgr.Cells[3].Value?.ToString() ?? "0";
                            string ret  = dgr.Cells[4].Value?.ToString() ?? "0";
                            string netQ = dgr.Cells[5].Value?.ToString() ?? "0";
                            string avgP = dgr.Cells[6].Value?.ToString() ?? "0";
                            string netV = dgr.Cells[7].Value?.ToString() ?? "0";

                            string[] rowVals = { code, name, unit, sold, ret, netQ, avgP, netV };

                            curX = tLeft + tWidth;
                            for (int c = 0; c < rowVals.Length; c++)
                            {
                                curX -= colW[c];
                                var sf = (c == 1) ? sfRight : sfCenter;
                                var brush = (c == 7) ? brSecondary : Brushes.Black;
                                var font = (c == 7 || c == 1) ? fBold : fNorm;
                                g.DrawString(rowVals[c], font, brush, new RectangleF(curX + 4, y + 6, colW[c] - 8, rowH - 6), sf);
                                if (c > 0) g.DrawLine(pThin, curX, y, curX, y + rowH);
                            }

                            y += rowH;
                            alt = !alt;
                        }

                        // Summary Box (Only on Last Page)
                        if (pageIdx == totalPages - 1)
                        {
                            y += 15;
                            decimal grandQty = 0m, grandVal = 0m;
                            if (dgItemized != null)
                            {
                                foreach (DataGridViewRow dgr in dgItemized.Rows)
                                {
                                    if (decimal.TryParse(dgr.Cells[5].Value?.ToString(), out decimal q)) grandQty += q;
                                    string vStr = (dgr.Cells[7].Value?.ToString() ?? "").Replace("ج", "").Trim();
                                    if (decimal.TryParse(vStr, out decimal v)) grandVal += v;
                                }
                            }

                            g.FillRectangle(brAlt, tLeft, y, tWidth, 60);
                            g.DrawRectangle(pBorder, tLeft, y, tWidth, 60);

                            g.DrawString($"عدد الأصناف: {rowCount} صنف", fBold, Brushes.Black, new RectangleF(tLeft + tWidth - 280, y + 16, 260, 28), sfRight);
                            g.DrawString($"إجمالي مسحوبات الكميات: {grandQty:N2}", fBold, Color.FromArgb(5, 150, 105) != Color.Empty ? new SolidBrush(Color.FromArgb(5, 150, 105)) : Brushes.Green, new RectangleF(tLeft + 450, y + 16, 320, 28), sfCenter);
                            g.DrawString($"إجمالي قيمة المبيعات: {grandVal:N2} ج.م", fTitle, brNavy, new RectangleF(tLeft + 20, y + 12, 380, 36), sfLeft);
                        }

                        // Footer Page Number & Sign
                        int footY = height - 70;
                        g.DrawLine(pThin, 40, footY, width - 40, footY);
                        g.DrawString($"صفحة {pageIdx + 1} من {totalPages}", fSmall, Brushes.Gray, new RectangleF(40, footY + 10, width - 80, 24), sfCenter);
                        g.DrawString("✨ تم إنشاء هذا التقرير آلياً بواسطة Pro System", fSmall, Brushes.Gray, new RectangleF(40, footY + 10, width - 80, 24), sfRight);
                    }
                }
                pages.Add(bmp);
            }

            SaveBitmapsAsPdf(pages, outputFilePath);
            return outputFilePath;
        }

        /// <summary>
        /// توليد ملف PDF متكامل لكشف الحساب المالي للعميل (Financial Statement)
        /// </summary>
        public static string GenerateFinancialStatementPdf(string clientName, string clientPhone, DateTime fromDate, DateTime toDate, DataGridView dgStatement, decimal totalSales, decimal totalReturns, decimal totalPayments, decimal runBalance, string outputFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ProSoft_Reports");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string cleanName = MakeValidFileName(clientName);
                outputFilePath = Path.Combine(tempDir, $"كشف_حساب_مالي_{cleanName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }

            int width = 1240;
            int height = 1754;
            var pages = new List<Bitmap>();

            int rowCount = dgStatement != null ? dgStatement.Rows.Count : 0;
            int rowsPerPage = 26;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)rowCount / rowsPerPage));

            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
            var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

            for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
            {
                var bmp = new Bitmap(width, height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    using (var pBorder = new Pen(Color.FromArgb(15, 45, 90), 3f))
                    using (var pThin = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
                    using (var brNavy = new SolidBrush(Color.FromArgb(15, 45, 90)))
                    using (var brSecondary = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    using (var brAlt = new SolidBrush(Color.FromArgb(248, 250, 252)))
                    using (var brRed = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    using (var brGreen = new SolidBrush(Color.FromArgb(5, 150, 105)))
                    using (var fTitle = new Font("Arial", 22f, FontStyle.Bold))
                    using (var fSub = new Font("Arial", 13f, FontStyle.Bold))
                    using (var fBold = new Font("Arial", 11.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Arial", 10f, FontStyle.Regular))
                    using (var fSmall = new Font("Arial", 9.5f, FontStyle.Regular))
                    {
                        g.DrawRectangle(pBorder, 20, 20, width - 40, height - 40);

                        int y = 40;

                        // Header Bar
                        using (var brHeaderGrad = new LinearGradientBrush(new Rectangle(25, 25, width - 50, 100), Color.FromArgb(15, 45, 90), Color.FromArgb(30, 64, 175), LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(brHeaderGrad, 25, 25, width - 50, 100);
                        }

                        string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة للتجارة والتوزيع";
                        g.DrawString(comp, fTitle, Brushes.White, new RectangleF(30, y, width - 60, 40), sfCenter);
                        g.DrawString("كشف الحساب المالي التفصيلي للمعاملات والمديونية (Financial Ledger Statement)", fSub, Brushes.LightCyan, new RectangleF(30, y + 44, width - 60, 28), sfCenter);
                        y += 105;

                        // Info Meta Card
                        g.FillRectangle(brAlt, 40, y, width - 80, 80);
                        g.DrawRectangle(pThin, 40, y, width - 80, 80);
                        g.DrawLine(pThin, width / 2, y, width / 2, y + 80);

                        g.DrawString($"العميل: {clientName}", fBold, brNavy, new RectangleF(width / 2 + 20, y + 10, width / 2 - 60, 28), sfRight);
                        if (!string.IsNullOrWhiteSpace(clientPhone))
                        {
                            g.DrawString($"هاتف العميل: {clientPhone}", fNorm, Brushes.Black, new RectangleF(width / 2 + 20, y + 42, width / 2 - 60, 28), sfRight);
                        }

                        g.DrawString($"الفترة من: {fromDate:yyyy/MM/dd} إلى: {toDate:yyyy/MM/dd}", fBold, Brushes.DarkSlateGray, new RectangleF(50, y + 10, width / 2 - 80, 28), sfRight);
                        g.DrawString($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd hh:mm tt}", fSmall, Brushes.Gray, new RectangleF(50, y + 42, width / 2 - 80, 28), sfRight);

                        y += 95;

                        // Columns: التاريخ والوقت(170) | النوع(130) | مدين(110) | دائن(110) | الرصيد الجاري(130) | القائم بالعمل(120) | تفاصيل البيان والأصناف(390)
                        int tLeft = 40;
                        int tWidth = width - 80;
                        int thH = 42;

                        g.FillRectangle(brNavy, tLeft, y, tWidth, thH);
                        g.DrawRectangle(pBorder, tLeft, y, tWidth, thH);

                        int[] colW = { 170, 130, 110, 110, 130, 120, 390 };
                        string[] colHeaders = { "التاريخ والوقت", "النوع", "مدين (فاتورة)", "دائن (تحصيل)", "الرصيد الجاري", "القائم بالعمل", "تفاصيل الأصناف والبيان المالي" };

                        int curX = tLeft + tWidth;
                        for (int c = 0; c < colHeaders.Length; c++)
                        {
                            curX -= colW[c];
                            g.DrawString(colHeaders[c], fBold, Brushes.White, new RectangleF(curX, y + 8, colW[c], thH - 8), sfCenter);
                            if (c > 0) g.DrawLine(Pens.White, curX, y, curX, y + thH);
                        }
                        y += thH;

                        // Table Rows
                        int startRow = pageIdx * rowsPerPage;
                        int endRow = Math.Min(rowCount, startRow + rowsPerPage);
                        int rowH = 38;
                        bool alt = false;

                        for (int r = startRow; r < endRow; r++)
                        {
                            var dgr = dgStatement.Rows[r];
                            if (alt) g.FillRectangle(brAlt, tLeft, y, tWidth, rowH);
                            g.DrawRectangle(pThin, tLeft, y, tWidth, rowH);

                            string dtStr   = dgr.Cells["TransDate"]?.Value?.ToString() ?? "";
                            string typeStr = dgr.Cells["TransType"]?.Value?.ToString() ?? "";
                            string debit   = dgr.Cells["Debit"]?.Value?.ToString() ?? "";
                            string credit  = dgr.Cells["Credit"]?.Value?.ToString() ?? "";
                            string bal     = dgr.Cells["Balance"]?.Value?.ToString() ?? "";
                            string user    = dgStatement.Columns.Contains("CreatedBy") ? (dgr.Cells["CreatedBy"]?.Value?.ToString() ?? "") : "";
                            string details = dgr.Cells["Details"]?.Value?.ToString() ?? "";

                            string[] rowVals = { dtStr, typeStr, debit, credit, bal, user, details };

                            curX = tLeft + tWidth;
                            for (int c = 0; c < rowVals.Length; c++)
                            {
                                curX -= colW[c];
                                var sf = (c == 6) ? sfRight : sfCenter;
                                Brush brush = Brushes.Black;
                                if (c == 2 && !string.IsNullOrEmpty(debit)) brush = brRed;
                                else if (c == 3 && !string.IsNullOrEmpty(credit)) brush = brGreen;
                                else if (c == 4) brush = brNavy;

                                var font = (c == 4 || c == 2 || c == 3) ? fBold : fNorm;
                                g.DrawString(rowVals[c], font, brush, new RectangleF(curX + 4, y + 7, colW[c] - 8, rowH - 7), sf);
                                if (c > 0) g.DrawLine(pThin, curX, y, curX, y + rowH);
                            }

                            y += rowH;
                            alt = !alt;
                        }

                        // Summary Box (Only on Last Page)
                        if (pageIdx == totalPages - 1)
                        {
                            y += 15;
                            g.FillRectangle(brAlt, tLeft, y, tWidth, 65);
                            g.DrawRectangle(pBorder, tLeft, y, tWidth, 65);

                            g.DrawString($"إجمالي المديونية: {totalSales:N2} ج", fBold, brRed, new RectangleF(tLeft + tWidth - 280, y + 18, 260, 28), sfRight);
                            g.DrawString($"إجمالي التحصيل: {totalPayments:N2} ج  |  المرتجع: {totalReturns:N2} ج", fBold, brGreen, new RectangleF(tLeft + 350, y + 18, 480, 28), sfCenter);
                            g.DrawString($"الصافي المستحق: {runBalance:N2} ج", fTitle, brNavy, new RectangleF(tLeft + 20, y + 14, 320, 36), sfLeft);
                        }

                        // Footer Page Number & Sign
                        int footY = height - 70;
                        g.DrawLine(pThin, 40, footY, width - 40, footY);
                        g.DrawString($"صفحة {pageIdx + 1} من {totalPages}", fSmall, Brushes.Gray, new RectangleF(40, footY + 10, width - 80, 24), sfCenter);
                        g.DrawString("✨ تم إنشاء هذا التقرير آلياً بواسطة Pro System", fSmall, Brushes.Gray, new RectangleF(40, footY + 10, width - 80, 24), sfRight);
                    }
                }
                pages.Add(bmp);
            }

            SaveBitmapsAsPdf(pages, outputFilePath);
            return outputFilePath;
        }

        private static string MakeValidFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "عميل";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    public static class ReceiptImageGenerator
    {
        private static readonly StringFormat SfCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        private static readonly StringFormat SfRtlRight = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.DirectionRightToLeft
        };

        private static readonly StringFormat SfRtlLeft = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.DirectionRightToLeft
        };

        private static readonly StringFormat SfRtlCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.DirectionRightToLeft
        };

        public static Bitmap GenerateSaleReceiptImage(int saleID)
        {
            try
            {
                var dtSale = DbHelper.Query(@"
                    SELECT s.SaleCode, s.TotalAmount, s.DiscountAmount, s.CashPaid, s.SaleDate, s.Notes,
                           c.ClientName, c.Phone
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE s.SaleID = @id", DbHelper.P("@id", saleID));

                if (dtSale.Rows.Count == 0) return null;
                var row = dtSale.Rows[0];

                var dtItems = DbHelper.Query(@"
                    SELECT p.ProductName, si.Quantity, si.UnitName, si.UnitPrice, si.TotalPrice
                    FROM SaleItems si
                    JOIN Products p ON si.ProductID = p.ProductID
                    WHERE si.SaleID = @id", DbHelper.P("@id", saleID));

                int width = 620;
                int baseHeight = 350 + (dtItems.Rows.Count * 36);
                var bmp = new Bitmap(width, baseHeight);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    // Header Gradient
                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 85), Color.FromArgb(24, 43, 73), Color.FromArgb(15, 23, 42), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 85);
                    }

                    string companyName = AppConfig.CompanyName;
                    if (string.IsNullOrWhiteSpace(companyName)) companyName = "شركة برو سوفت للأنظمة الإلكترونية";

                    using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 10f, FontStyle.Regular))
                    {
                        g.DrawString(companyName, fTitle, Brushes.White, new RectangleF(0, 12, width, 35), SfCenter);
                        g.DrawString("فاتورة مبيعات إلكترونية موثقة", fSub, Brushes.LightGray, new RectangleF(0, 48, width, 25), SfCenter);
                    }

                    int y = 100;
                    using (var fBold = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 9.5f))
                    using (var brClient = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    using (var brHeaderBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    using (var penBorder = new Pen(Color.FromArgb(203, 213, 225)))
                    using (var penRowBorder = new Pen(Color.FromArgb(241, 245, 249)))
                    using (var penLine = new Pen(Color.FromArgb(148, 163, 184)))
                    using (var brBoxBg = new SolidBrush(Color.FromArgb(236, 253, 245)))
                    using (var penBoxBorder = new Pen(Color.FromArgb(167, 243, 208)))
                    using (var brTotalText = new SolidBrush(Color.FromArgb(6, 95, 70)))
                    using (var brSubText = new SolidBrush(Color.FromArgb(30, 41, 59)))
                    {
                        // Sale Meta Info (RTL)
                        g.DrawString($"رقم الفاتورة: #{row["SaleCode"]}", fBold, Brushes.Black, new RectangleF(320, y, 280, 26), SfRtlRight);
                        g.DrawString($"التاريخ: {Convert.ToDateTime(row["SaleDate"]):yyyy/MM/dd HH:mm}", fNorm, Brushes.DarkSlateGray, new RectangleF(20, y, 280, 26), SfRtlLeft);
                        y += 30;

                        string clientName = row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "عميل نقدي";
                        g.DrawString($"العميل: {clientName}", fBold, brClient, new RectangleF(20, y, 580, 26), SfRtlRight);
                        y += 34;

                        // Table Header
                        g.FillRectangle(brHeaderBg, 20, y, width - 40, 30);
                        g.DrawRectangle(penBorder, 20, y, width - 40, 30);

                        g.DrawString("الصنف", fBold, Brushes.Black, new RectangleF(340, y, 250, 30), SfRtlRight);
                        g.DrawString("الكمية", fBold, Brushes.Black, new RectangleF(220, y, 110, 30), SfRtlCenter);
                        g.DrawString("السعر", fBold, Brushes.Black, new RectangleF(120, y, 90, 30), SfRtlCenter);
                        g.DrawString("الإجمالي", fBold, Brushes.Black, new RectangleF(30, y, 80, 30), SfRtlLeft);
                        y += 34;

                        // Table Items
                        foreach (DataRow item in dtItems.Rows)
                        {
                            string prod = item["ProductName"].ToString();
                            decimal qty = Convert.ToDecimal(item["Quantity"]);
                            string unit = item["UnitName"] != DBNull.Value ? item["UnitName"].ToString() : "";
                            decimal price = Convert.ToDecimal(item["UnitPrice"]);
                            decimal itemTot = Convert.ToDecimal(item["TotalPrice"]);

                            g.DrawString(prod, fNorm, Brushes.Black, new RectangleF(340, y, 250, 28), SfRtlRight);
                            g.DrawString($"{qty:0.##} {unit}", fNorm, Brushes.Black, new RectangleF(220, y, 110, 28), SfRtlCenter);
                            g.DrawString($"{price:N2}", fNorm, Brushes.Black, new RectangleF(120, y, 90, 28), SfRtlCenter);
                            g.DrawString($"{itemTot:N2} ج", fBold, Brushes.Black, new RectangleF(30, y, 80, 28), SfRtlLeft);

                            y += 32;
                            g.DrawLine(penRowBorder, 20, y - 2, width - 20, y - 2);
                        }

                        y += 8;
                        g.DrawLine(penLine, 20, y, width - 20, y);
                        y += 14;

                        // Totals Summary Box
                        decimal total = Convert.ToDecimal(row["TotalAmount"]);
                        decimal paid = Convert.ToDecimal(row["CashPaid"]);
                        decimal remaining = total - paid;

                        g.FillRectangle(brBoxBg, 20, y, width - 40, 72);
                        g.DrawRectangle(penBoxBorder, 20, y, width - 40, 72);

                        using (var fTotal = new Font("Segoe UI", 13.5f, FontStyle.Bold))
                        {
                            g.DrawString("إجمالي الفاتورة المطلوب:", fTotal, brTotalText, new RectangleF(300, y + 8, 290, 30), SfRtlRight);
                            g.DrawString($"{total:N2} جنيه مصري", fTotal, brTotalText, new RectangleF(30, y + 8, 260, 30), SfRtlLeft);
                        }

                        g.DrawString($"المدفوع نقداً: {paid:N2} ج  |  المتبقي: {remaining:N2} ج", fBold, brSubText, new RectangleF(30, y + 42, 560, 24), SfRtlRight);
                        y += 88;

                        // Footer Thank You
                        using (var fFooter = new Font("Segoe UI", 10f, FontStyle.Italic))
                        {
                            g.DrawString("شكراً لتعاملكم معنا ونتمنى لكم دوام التوفيق والنجاح", fFooter, Brushes.Gray, new RectangleF(0, y, width, 25), SfCenter);
                        }
                    }
                }

                return bmp;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateSaleReceiptImage", ex);
                return null;
            }
        }

        public static Bitmap GenerateVoucherReceiptImage(int voucherID)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT v.VoucherID, v.VoucherCode, v.VoucherDate, v.VoucherType, v.Amount, v.Notes,
                           c.ClientName, c.Phone
                    FROM ReceiptVouchers v
                    LEFT JOIN Clients c ON v.ClientID = c.ClientID
                    WHERE v.VoucherID = @id", DbHelper.P("@id", voucherID));

                if (dt.Rows.Count == 0) return null;
                var row = dt.Rows[0];

                int width = 620;
                int height = 360;
                var bmp = new Bitmap(width, height);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 80), Color.FromArgb(15, 118, 110), Color.FromArgb(13, 148, 136), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 80);
                    }

                    string title = row["VoucherType"].ToString().Contains("In") || row["VoucherType"].ToString().Contains("Receipt") ? "إيصال استلام نقدية (سند قبض)" : "إيصال صرف نقدية (سند صرف)";

                    using (var fTitle = new Font("Segoe UI", 15.5f, FontStyle.Bold))
                    {
                        g.DrawString(title, fTitle, Brushes.White, new RectangleF(0, 18, width, 40), SfCenter);
                    }

                    int y = 95;

                    using (var fBold = new Font("Segoe UI", 11f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 10f))
                    using (var brClient = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    using (var brAmtBg = new SolidBrush(Color.FromArgb(240, 253, 244)))
                    using (var penAmtBorder = new Pen(Color.FromArgb(187, 247, 208)))
                    using (var brAmtText = new SolidBrush(Color.FromArgb(22, 101, 52)))
                    {
                        g.DrawString($"رقم السند: #{row["VoucherCode"]}", fBold, Brushes.Black, new RectangleF(320, y, 280, 28), SfRtlRight);
                        g.DrawString($"التاريخ: {Convert.ToDateTime(row["VoucherDate"]):yyyy/MM/dd HH:mm}", fNorm, Brushes.DarkSlateGray, new RectangleF(20, y, 280, 28), SfRtlLeft);
                        y += 36;

                        string clientName = row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "العميل الكريم";
                        g.DrawString($"استلمنا من السيد: {clientName}", fBold, brClient, new RectangleF(20, y, 580, 28), SfRtlRight);
                        y += 36;

                        decimal amount = Convert.ToDecimal(row["Amount"]);
                        g.FillRectangle(brAmtBg, 20, y, width - 40, 56);
                        g.DrawRectangle(penAmtBorder, 20, y, width - 40, 56);

                        using (var fAmt = new Font("Segoe UI", 14f, FontStyle.Bold))
                        {
                            g.DrawString("المبلغ المدفوع:", fAmt, brAmtText, new RectangleF(320, y + 10, 270, 36), SfRtlRight);
                            g.DrawString($"{amount:N2} جنيه مصري", fAmt, brAmtText, new RectangleF(30, y + 10, 280, 36), SfRtlLeft);
                        }

                        y += 72;
                        string notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : "سداد دفعة من الحساب";
                        g.DrawString($"وذلك عن: {notes}", fNorm, Brushes.Black, new RectangleF(20, y, 580, 28), SfRtlRight);
                        y += 45;

                        g.DrawString("التوقيع والإحاطة: ___________________", fNorm, Brushes.Gray, new RectangleF(20, y, 580, 28), SfRtlLeft);
                    }
                }

                return bmp;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateVoucherReceiptImage", ex);
                return null;
            }
        }

        public static Bitmap GenerateClientStatementImage(int clientID, string clientName, decimal balance, string extraNotes = "")
        {
            try
            {
                int width = 620;
                int height = 370;
                var bmp = new Bitmap(width, height);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    // Header
                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 85), Color.FromArgb(30, 58, 138), Color.FromArgb(29, 78, 216), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 85);
                    }

                    string companyName = AppConfig.CompanyName;
                    if (string.IsNullOrWhiteSpace(companyName)) companyName = "شركة برو سوفت للأنظمة الإلكترونية";

                    using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 10f))
                    {
                        g.DrawString(companyName, fTitle, Brushes.White, new RectangleF(0, 12, width, 35), SfCenter);
                        g.DrawString("كشف حساب وموقف مالي للعميل", fSub, Brushes.WhiteSmoke, new RectangleF(0, 48, width, 25), SfCenter);
                    }

                    int y = 100;

                    using (var fBold = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 10f))
                    using (var brClient = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brBoxBg = new SolidBrush(balance > 0 ? Color.FromArgb(254, 242, 242) : Color.FromArgb(240, 253, 244)))
                    using (var penBox = new Pen(balance > 0 ? Color.FromArgb(254, 202, 202) : Color.FromArgb(187, 247, 208)))
                    using (var brBalText = new SolidBrush(balance > 0 ? Color.FromArgb(185, 28, 28) : Color.FromArgb(22, 101, 52)))
                    {
                        g.DrawString($"اسم العميل: {clientName} (كود: {clientID})", fBold, brClient, new RectangleF(260, y, 340, 28), SfRtlRight);
                        g.DrawString($"التاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}", fNorm, Brushes.DarkSlateGray, new RectangleF(20, y, 230, 28), SfRtlLeft);
                        y += 38;

                        // Balance Highlight Card
                        g.FillRectangle(brBoxBg, 20, y, width - 40, 78);
                        g.DrawRectangle(penBox, 20, y, width - 40, 78);

                        using (var fBal = new Font("Segoe UI", 15f, FontStyle.Bold))
                        {
                            string balLabel = balance > 0 ? "الرصيد المستحق (مديونية):" : (balance < 0 ? "الرصيد المتبقي (دائن):" : "الرصيد الحالي:");
                            g.DrawString(balLabel, fBold, brBalText, new RectangleF(320, y + 10, 270, 32), SfRtlRight);
                            g.DrawString($"{Math.Abs(balance):N2} جنيه مصري", fBal, brBalText, new RectangleF(30, y + 10, 280, 32), SfRtlLeft);
                        }

                        string statusText = balance > 0 ? "نرجو التكرم بسداد المبلغ المستحق في أقرب وقت" : "الحساب خالص ومطابق تماماً";
                        g.DrawString(statusText, fNorm, brBalText, new RectangleF(30, y + 46, 560, 24), SfRtlRight);
                        y += 96;

                        if (!string.IsNullOrWhiteSpace(extraNotes))
                        {
                            g.DrawString($"ملاحظات: {extraNotes}", fNorm, Brushes.Black, new RectangleF(20, y, 580, 28), SfRtlRight);
                            y += 36;
                        }

                        using (var fFooter = new Font("Segoe UI", 10f, FontStyle.Italic))
                        {
                            g.DrawString("شكراً لتعاملكم ونتمنى لكم دوام التوفيق والنجاح", fFooter, Brushes.Gray, new RectangleF(0, y + 8, width, 25), SfCenter);
                        }
                    }
                }

                return bmp;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateClientStatementImage", ex);
                return null;
            }
        }

        public static Bitmap GenerateTextCardImage(string title, string textBody, string subtitle = "")
        {
            try
            {
                string[] lines = textBody.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int lineCount = Math.Max(lines.Length, 4);

                int width = 620;
                int rowHeight = 26;
                int baseHeight = 170 + (lineCount * rowHeight);
                var bmp = new Bitmap(width, baseHeight);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    // Header Gradient
                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 85), Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 85);
                    }

                    string companyName = AppConfig.CompanyName;
                    if (string.IsNullOrWhiteSpace(companyName)) companyName = "شركة برو سوفت للأنظمة المتكاملة";

                    using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 10f))
                    {
                        g.DrawString(companyName, fTitle, Brushes.White, new RectangleF(0, 12, width, 35), SfCenter);
                        string sub = !string.IsNullOrWhiteSpace(title) ? title : (!string.IsNullOrWhiteSpace(subtitle) ? subtitle : "إشعار موثق من النظام");
                        // Clean emoji from sub
                        sub = CleanEmoji(sub);
                        g.DrawString(sub, fSub, Brushes.LightGray, new RectangleF(0, 48, width, 25), SfCenter);
                    }

                    int y = 98;
                    using (var fBold = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 10f))
                    using (var penLine = new Pen(Color.FromArgb(226, 232, 240)))
                    using (var brPrimary = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brAccent = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    {
                        foreach (string rawLine in lines)
                        {
                            string line = CleanEmoji(rawLine?.Trim() ?? "");
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                y += 10;
                                continue;
                            }

                            if (line.StartsWith("---") || line.StartsWith("===") || line.StartsWith("───") || line.StartsWith("━━━"))
                            {
                                g.DrawLine(penLine, 25, y + 10, width - 25, y + 10);
                                y += 20;
                                continue;
                            }

                            bool isHeader = line.StartsWith("*") && line.EndsWith("*");
                            string cleanText = line.Trim('*').Trim();

                            if (isHeader)
                            {
                                g.DrawString(cleanText, fBold, brAccent, new RectangleF(25, y, width - 50, 26), SfRtlRight);
                            }
                            else
                            {
                                g.DrawString(cleanText, fNorm, brPrimary, new RectangleF(25, y, width - 50, 26), SfRtlRight);
                            }

                            y += rowHeight;
                        }

                        using (var fFooter = new Font("Segoe UI", 9.5f, FontStyle.Italic))
                        {
                            g.DrawString("تم إنشاء هذا المستند آلياً بواسطة النظام", fFooter, Brushes.Gray, new RectangleF(0, y + 6, width, 22), SfCenter);
                        }
                    }
                }

                return bmp;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateTextCardImage", ex);
                return null;
            }
        }

        private static string CleanEmoji(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(input, @"\p{Cs}|\p{So}|\p{Sk}|\p{Cn}", "");
        }
    }
}

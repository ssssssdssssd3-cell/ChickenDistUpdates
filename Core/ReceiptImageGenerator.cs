using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    public static class ReceiptImageGenerator
    {
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

                int width = 560;
                int baseHeight = 320 + (dtItems.Rows.Count * 36);
                var bmp = new Bitmap(width, baseHeight);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    // Header Gradient
                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 80), Color.FromArgb(30, 41, 59), Color.FromArgb(15, 23, 42), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 80);
                    }

                    // Company Name
                    string companyName = AppConfig.CompanyName;
                    if (string.IsNullOrWhiteSpace(companyName)) companyName = "شركة برو سوفت للأنظمة الإلكترونية";

                    using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 10f, FontStyle.Regular))
                    {
                        var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(companyName, fTitle, Brushes.White, new RectangleF(0, 10, width, 35), sfCenter);
                        g.DrawString("🧾 فاتورة مبيعات إلكترونية موثقة", fSub, Brushes.LightGray, new RectangleF(0, 45, width, 25), sfCenter);
                    }

                    int y = 95;
                    var sfRight = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                    using (var fBold = new Font("Segoe UI", 10f, FontStyle.Bold))
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
                        // Sale Meta Info
                        g.DrawString($"رقم الفاتورة: #{row["SaleCode"]}", fBold, Brushes.Black, 20, y);
                        g.DrawString($"التاريخ: {Convert.ToDateTime(row["SaleDate"]):yyyy/MM/dd HH:mm}", fNorm, Brushes.DarkGray, width - 20, y, sfRight);
                        y += 26;

                        string clientName = row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "عميل نقدي";
                        g.DrawString($"العميل: {clientName}", fBold, brClient, 20, y);
                        y += 32;

                        // Table Header
                        g.FillRectangle(brHeaderBg, 16, y, width - 32, 28);
                        g.DrawRectangle(penBorder, 16, y, width - 32, 28);

                        g.DrawString("الصنف", fBold, Brushes.Black, 30, y + 4);
                        g.DrawString("الكمية", fBold, Brushes.Black, 280, y + 4);
                        g.DrawString("السعر", fBold, Brushes.Black, 370, y + 4);
                        g.DrawString("الإجمالي", fBold, Brushes.Black, width - 30, y + 4, sfRight);
                        y += 34;

                        // Table Items
                        foreach (DataRow item in dtItems.Rows)
                        {
                            string prod = item["ProductName"].ToString();
                            decimal qty = Convert.ToDecimal(item["Quantity"]);
                            string unit = item["UnitName"] != DBNull.Value ? item["UnitName"].ToString() : "";
                            decimal price = Convert.ToDecimal(item["UnitPrice"]);
                            decimal itemTot = Convert.ToDecimal(item["TotalPrice"]);

                            g.DrawString(prod, fNorm, Brushes.Black, 30, y);
                            g.DrawString($"{qty} {unit}", fNorm, Brushes.Black, 280, y);
                            g.DrawString($"{price:N2}", fNorm, Brushes.Black, 370, y);
                            g.DrawString($"{itemTot:N2} ج", fBold, Brushes.Black, width - 30, y, sfRight);

                            y += 32;
                            g.DrawLine(penRowBorder, 20, y - 4, width - 20, y - 4);
                        }

                        y += 10;
                        g.DrawLine(penLine, 16, y, width - 16, y);
                        y += 16;

                        // Totals Summary Box
                        decimal total = Convert.ToDecimal(row["TotalAmount"]);
                        decimal discount = Convert.ToDecimal(row["DiscountAmount"]);
                        decimal paid = Convert.ToDecimal(row["CashPaid"]);
                        decimal remaining = total - paid;

                        g.FillRectangle(brBoxBg, 16, y, width - 32, 70);
                        g.DrawRectangle(penBoxBorder, 16, y, width - 32, 70);

                        using (var fTotal = new Font("Segoe UI", 13f, FontStyle.Bold))
                        {
                            g.DrawString("إجمالي الفاتورة:", fTotal, brTotalText, 30, y + 10);
                            g.DrawString($"{total:N2} ج", fTotal, brTotalText, width - 30, y + 10, sfRight);
                        }

                        g.DrawString($"المدفوع: {paid:N2} ج  |  المتبقي: {remaining:N2} ج", fBold, brSubText, 30, y + 42);
                        y += 85;

                        // Footer Thank You
                        using (var fFooter = new Font("Segoe UI", 9.5f, FontStyle.Italic))
                        {
                            var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                            g.DrawString("شكراً لتعاملكم معنا ونتمنى لكم يوم سعيد! 🙏", fFooter, Brushes.Gray, new RectangleF(0, y, width, 25), sfCenter);
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

                int width = 540;
                int height = 340;
                var bmp = new Bitmap(width, height);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 70), Color.FromArgb(15, 118, 110), Color.FromArgb(13, 148, 136), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 70);
                    }

                    var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    string title = row["VoucherType"].ToString().Contains("In") || row["VoucherType"].ToString().Contains("Receipt") ? "إيصال استلام نقدية (سند قبض)" : "إيصال صرف نقدية (سند صرف)";

                    using (var fTitle = new Font("Segoe UI", 15f, FontStyle.Bold))
                    {
                        g.DrawString($"📄 {title}", fTitle, Brushes.White, new RectangleF(0, 15, width, 40), sfCenter);
                    }

                    int y = 85;
                    var sfRight = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                    using (var fBold = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 10f))
                    using (var brClient = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    using (var brAmtBg = new SolidBrush(Color.FromArgb(240, 253, 244)))
                    using (var penAmtBorder = new Pen(Color.FromArgb(187, 247, 208)))
                    using (var brAmtText = new SolidBrush(Color.FromArgb(22, 101, 52)))
                    {
                        g.DrawString($"رقم السند: #{row["VoucherCode"]}", fBold, Brushes.Black, 20, y);
                        g.DrawString($"التاريخ: {Convert.ToDateTime(row["VoucherDate"]):yyyy/MM/dd HH:mm}", fNorm, Brushes.DarkGray, width - 20, y, sfRight);
                        y += 35;

                        string clientName = row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "عميل عام";
                        g.DrawString($"استلمنا من السيد/العميل: {clientName}", fBold, brClient, 20, y);
                        y += 35;

                        decimal amount = Convert.ToDecimal(row["Amount"]);
                        g.FillRectangle(brAmtBg, 16, y, width - 32, 50);
                        g.DrawRectangle(penAmtBorder, 16, y, width - 32, 50);

                        using (var fAmt = new Font("Segoe UI", 14f, FontStyle.Bold))
                        {
                            g.DrawString("المبلغ المدفوع:", fAmt, brAmtText, 30, y + 10);
                            g.DrawString($"{amount:N2} جنيه مصري", fAmt, brAmtText, width - 30, y + 10, sfRight);
                        }

                        y += 65;
                        string notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : "سداد جزء من الحساب";
                        g.DrawString($"وذلك عن: {notes}", fNorm, Brushes.Black, 20, y);
                        y += 45;

                        g.DrawString("التوقيع والإحاطة: ___________________", fNorm, Brushes.DarkGray, width - 20, y, sfRight);
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
    }
}

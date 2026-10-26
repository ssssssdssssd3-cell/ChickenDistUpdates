using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ChickenDist.DAL;

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

        public static Bitmap GenerateSaleReceiptImage(int saleID, string templateName = null)
        {
            try
            {
                var dtSale = DbHelper.Query(@"
                    SELECT s.SaleID, s.SaleCode, s.TotalAmount, s.DiscountAmount, s.DiscountPct, s.CashPaid, s.SaleDate, s.Notes, s.SaleType, s.ClientID, s.CustomClientName,
                           COALESCE(NULLIF(s.CustomClientName, N''), c.ClientName, N'عميل نقدي') AS ClientName, c.Phone
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE s.SaleID = @id", DbHelper.P("@id", saleID));

                if (dtSale.Rows.Count == 0) return null;
                var row = dtSale.Rows[0];

                var dtItems = SaleDAL.GetItems(saleID);
                int clientID = row["ClientID"] != DBNull.Value ? Convert.ToInt32(row["ClientID"]) : 0;
                decimal prevBalance = 0m;
                decimal todayPayments = 0m;
                decimal todayReturns = 0m;
                decimal currentBalance = 0m;
                decimal lastPaymentAmt = 0m;
                DateTime lastPaymentDate = DateTime.MinValue;

                if (clientID > 0)
                {
                    try
                    {
                        prevBalance = ClientDAL.GetPreviousBalanceBeforeSale(clientID, saleID);
                        decimal netVal = Convert.ToDecimal(row["TotalAmount"]);
                        bool isCredit = row["SaleType"] != DBNull.Value && row["SaleType"].ToString() == "Credit";
                        decimal cashPaid = isCredit ? 0m : (row["CashPaid"] != DBNull.Value ? Convert.ToDecimal(row["CashPaid"]) : netVal);
                        decimal rem = isCredit ? netVal : (netVal - cashPaid);
                        currentBalance = prevBalance + rem;

                        var dtLastPay = DbHelper.Query(@"
                            SELECT TOP 1 Credit AS PaymentAmount, TransDate AS PaymentDate 
                            FROM ClientTransactions 
                            WHERE ClientID = @cid AND TransType = 'Payment' AND Credit > 0
                              AND (RefID IS NULL OR RefID <> @sid)
                            ORDER BY TransDate DESC, TransID DESC", DbHelper.P("@cid", clientID), DbHelper.P("@sid", saleID));
                        if (dtLastPay.Rows.Count > 0)
                        {
                            lastPaymentAmt = Convert.ToDecimal(dtLastPay.Rows[0]["PaymentAmount"]);
                            lastPaymentDate = Convert.ToDateTime(dtLastPay.Rows[0]["PaymentDate"]);
                        }
                    }
                    catch { }
                }
                else
                {
                    decimal netVal = Convert.ToDecimal(row["TotalAmount"]);
                    bool isCredit = row["SaleType"] != DBNull.Value && row["SaleType"].ToString() == "Credit";
                    decimal cashPaid = isCredit ? 0m : (row["CashPaid"] != DBNull.Value ? Convert.ToDecimal(row["CashPaid"]) : netVal);
                    currentBalance = isCredit ? netVal : (netVal - cashPaid);
                    if (!isCredit && cashPaid > 0)
                    {
                        lastPaymentAmt = cashPaid;
                        lastPaymentDate = Convert.ToDateTime(row["SaleDate"]);
                    }
                }

                return GenerateSaleReceiptImage(row, dtItems, prevBalance, lastPaymentAmt, lastPaymentDate, todayPayments, todayReturns, currentBalance, templateName);
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateSaleReceiptImage", ex);
                return null;
            }
        }

        public static Bitmap GenerateSampleReceiptImage(string templateName = null)
        {
            DataTable dtMock = new DataTable();
            dtMock.Columns.Add("SaleID", typeof(int));
            dtMock.Columns.Add("SaleCode", typeof(string));
            dtMock.Columns.Add("SaleDate", typeof(DateTime));
            dtMock.Columns.Add("SaleType", typeof(string));
            dtMock.Columns.Add("ClientID", typeof(int));
            dtMock.Columns.Add("ClientName", typeof(string));
            dtMock.Columns.Add("Phone", typeof(string));
            dtMock.Columns.Add("TotalAmount", typeof(decimal));
            dtMock.Columns.Add("DiscountAmount", typeof(decimal));
            dtMock.Columns.Add("DiscountPct", typeof(decimal));
            dtMock.Columns.Add("CashPaid", typeof(decimal));
            dtMock.Columns.Add("Notes", typeof(string));

            DataRow r = dtMock.NewRow();
            r["SaleID"] = 101;
            r["SaleCode"] = "INV-2026-088";
            r["SaleDate"] = DateTime.Now;
            r["SaleType"] = "Credit";
            r["ClientID"] = 1;
            r["ClientName"] = "معرض الأمل للتجارة والتوزيع";
            r["Phone"] = "01070909181";
            r["TotalAmount"] = 24850.00m;
            r["DiscountAmount"] = 350.00m;
            r["DiscountPct"] = 0m;
            r["CashPaid"] = 10000.00m;
            r["Notes"] = "تسليم المخزن الرئيسي - بضاعة معتمدة";
            dtMock.Rows.Add(r);

            DataTable items = new DataTable();
            items.Columns.Add("ProductCode", typeof(string));
            items.Columns.Add("ProductName", typeof(string));
            items.Columns.Add("UnitName", typeof(string));
            items.Columns.Add("Quantity", typeof(decimal));
            items.Columns.Add("UnitPrice", typeof(decimal));
            items.Columns.Add("DiscountAmt", typeof(decimal));
            items.Columns.Add("TotalPrice", typeof(decimal));
            items.Columns.Add("Notes", typeof(string));

            items.Rows.Add("TV-55-4K", "شاشة 55 بوصة سمارت 4K Ultra HD", "جهاز", 2m, 8500.00m, 200.00m, 16800.00m, "ضمان سنتين");
            items.Rows.Add("WM-8KG-INV", "غسالة أوتوماتيك 8 كيلو انفرتر ديجيتال", "جهاز", 1m, 6200.00m, 150.00m, 6050.00m, "إيطالي أصلي");
            items.Rows.Add("IR-TEF-22", "مكواة بخار تيفال سيراميك 2200W", "قطعة", 3m, 450.00m, 0m, 1350.00m, "");
            items.Rows.Add("MX-MUL-60", "خلاط ومطحنة مولينكس 600W", "طقم", 2m, 325.00m, 0m, 650.00m, "");

            return GenerateSaleReceiptImage(r, items, 15000.00m, 5000.00m, DateTime.Now.AddDays(-3), 5000.00m, 0m, 29850.00m, templateName);
        }

        public static Bitmap GenerateSaleReceiptImage(DataRow row, DataTable dtItems, decimal prevBalance, decimal lastPaymentAmt, DateTime lastPaymentDate, decimal todayPayments, decimal todayReturns, decimal actualCurrentBalance, string templateName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(templateName))
                {
                    templateName = AppConfig.WhatsAppInvoiceTemplate;
                }

                bool isAlTarek    = string.Equals(templateName, "ImageCardAlTarek", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(templateName, "AlTarek", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(templateName, "AlTarekGrid", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(templateName, "AlTarekHome", StringComparison.OrdinalIgnoreCase);
                bool isCommercial = string.Equals(templateName, "ImageCardCommercial", StringComparison.OrdinalIgnoreCase);
                bool isModern     = string.Equals(templateName, "ImageCardModern", StringComparison.OrdinalIgnoreCase);
                bool isEmerald    = string.Equals(templateName, "ImageCardEmerald", StringComparison.OrdinalIgnoreCase);
                bool isGold       = string.Equals(templateName, "ImageCardGold", StringComparison.OrdinalIgnoreCase);
                // default is Royal Navy
                bool isNavy       = !isCommercial && !isModern && !isEmerald && !isGold && !isAlTarek;

                if (AppConfig.BusinessType == "Mobiles" && dtItems != null && dtItems.Columns.Contains("IMEI"))
                {
                    foreach (DataRow r in dtItems.Rows)
                    {
                        string imei = r["IMEI"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(imei) && !r["ProductName"].ToString().Contains(imei))
                        {
                            r["ProductName"] = r["ProductName"].ToString() + " (IMEI: " + imei + ")";
                        }
                    }
                }

                int itemCount = dtItems != null ? dtItems.Rows.Count : 0;
                bool showFinancial = row.Table.Columns.Contains("ClientID") && row["ClientID"] != DBNull.Value;
                decimal netVal = Convert.ToDecimal(row["TotalAmount"]);
                bool isCredit = row.Table.Columns.Contains("SaleType") && row["SaleType"] != DBNull.Value && row["SaleType"].ToString() == "Credit";
                decimal paidVal = isCredit ? 0m : (row.Table.Columns.Contains("CashPaid") && row["CashPaid"] != DBNull.Value ? Convert.ToDecimal(row["CashPaid"]) : (row["SaleType"].ToString() == "Cash" ? netVal : 0m));
                decimal remainVal = isCredit ? netVal : (netVal - paidVal);

                // Color Themes
                Color cPrimary   = isAlTarek    ? Color.FromArgb(24, 34, 53)
                                 : isNavy       ? Color.FromArgb(0, 51, 153)
                                 : isModern     ? Color.FromArgb(15, 23, 42)
                                 : isEmerald    ? Color.FromArgb(5, 150, 105)
                                 : isGold       ? Color.FromArgb(24, 24, 27)
                                 : Color.FromArgb(30, 41, 59);

                Color cSecondary = isAlTarek    ? Color.FromArgb(37, 52, 78)
                                 : isNavy       ? Color.FromArgb(30, 64, 175)
                                 : isModern     ? Color.FromArgb(30, 41, 59)
                                 : isEmerald    ? Color.FromArgb(4, 120, 87)
                                 : isGold       ? Color.FromArgb(217, 119, 6)
                                 : Color.FromArgb(51, 65, 85);

                Color cAccent    = isAlTarek    ? Color.FromArgb(245, 158, 11)
                                 : isNavy       ? Color.FromArgb(245, 158, 11)
                                 : isModern     ? Color.FromArgb(16, 185, 129)
                                 : isEmerald    ? Color.FromArgb(217, 119, 6)
                                 : isGold       ? Color.FromArgb(245, 158, 11)
                                 : Color.FromArgb(2, 132, 199);

                Color cAltBg     = isAlTarek    ? Color.FromArgb(248, 250, 253)
                                 : isNavy       ? Color.FromArgb(248, 250, 252)
                                 : isModern     ? Color.FromArgb(241, 245, 249)
                                 : isEmerald    ? Color.FromArgb(236, 253, 245)
                                 : isGold       ? Color.FromArgb(254, 252, 232)
                                 : Color.FromArgb(248, 250, 252);

                int width = (isCommercial || isAlTarek) ? 680 : 620;
                int rowH = (isCommercial || isAlTarek) ? 28 : 32;
                int headerH = isAlTarek ? 130 : (isCommercial ? 120 : 100);
                int metaH = (isCommercial || isAlTarek) ? 65 : 75;
                int tableHeaderH = 34;
                int itemsH = (itemCount * rowH);
                int netH = (isCommercial || isAlTarek) ? 0 : 45;
                int financialH = isAlTarek ? 260 : (showFinancial ? (isCommercial ? 190 : 190) : 0);
                int footerH = (isAlTarek ? 30 : 70) + 70;

                int totalH = headerH + metaH + tableHeaderH + itemsH + netH + 20 + financialH + footerH + 40;
                var bmp = new Bitmap(width, totalH);
                bmp.SetResolution(144f, 144f);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    using (var pThick = new Pen(cPrimary, 2.5f))
                    using (var pThin = new Pen(Color.FromArgb(203, 213, 225), 1f))
                    using (var pBlack = new Pen(Color.Black, 1f))
                    using (var pAccent = new Pen(cAccent, 1.5f))
                    using (var brPrimary = new SolidBrush(cPrimary))
                    using (var brSecondary = new SolidBrush(cSecondary))
                    using (var brAccent = new SolidBrush(cAccent))
                    using (var brAlt = new SolidBrush(cAltBg))
                    using (var brRed = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    using (var fBig = new Font("Arial", 16f, FontStyle.Bold))
                    using (var fSub = new Font("Arial", 10.5f, FontStyle.Bold))
                    using (var fBold = new Font("Arial", 9.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Arial", 9f, FontStyle.Regular))
                    using (var fSmall = new Font("Arial", 8.5f, FontStyle.Regular))
                    {
                        // Outer Frame
                        g.DrawRectangle(pThick, 4, 4, width - 8, totalH - 8);
                        if (isGold || isNavy || isAlTarek)
                        {
                            g.DrawRectangle(pAccent, 8, 8, width - 16, totalH - 16);
                        }

                        int y = 14;
                        string compName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة التجارية";
                        string compPhone = !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "";

                        // ── 1. HEADER SECTION ───────────────────────
                        if (isAlTarek)
                        {
                            // 1. Draw Company Logo on Top-Left (أعلى الفاتورة من الشمال)
                            int logoW = 0;
                            if (!string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                            {
                                try
                                {
                                    using (var logoImg = Image.FromFile(AppConfig.ShopLogoPath))
                                    {
                                        int maxLogoW = 140;
                                        int maxLogoH = 70;
                                        double ratioX = (double)maxLogoW / logoImg.Width;
                                        double ratioY = (double)maxLogoH / logoImg.Height;
                                        double ratio = Math.Min(ratioX, ratioY);
                                        int lw = (int)(logoImg.Width * ratio);
                                        int lh = (int)(logoImg.Height * ratio);
                                        g.DrawImage(logoImg, 20, y, lw, lh);
                                        logoW = lw + 15;
                                    }
                                }
                                catch { }
                            }

                            // 2. Draw Company Name (اسم المكان) on Top-Right - NO YELLOW BOX
                            float headerRightW = width - 40 - logoW;
                            g.DrawString(compName, fBig, brPrimary, new RectangleF(20 + logoW, y, headerRightW, 32), SfRtlRight);
                            y += 32;
                            if (!string.IsNullOrEmpty(compPhone))
                            {
                                string addrStr = !string.IsNullOrEmpty(AppConfig.CompanyAddress) ? $"  |  العنوان: {AppConfig.CompanyAddress}" : "";
                                g.DrawString($"موبايل: {compPhone}{addrStr}", fSmall, Brushes.DarkSlateGray, new RectangleF(20 + logoW, y, headerRightW, 20), SfRtlRight);
                            }
                            y += 22;
                            g.DrawString("فاتورة مبيعات معتمدة", fSub, brAccent, new RectangleF(15, y, width - 30, 22), SfCenter);
                            y += 24;
                            g.DrawLine(pThick, 20, y, width - 20, y);
                            y += 8;

                            // Metadata Box
                            string clientName = row.Table.Columns.Contains("ClientName") && row["ClientName"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["ClientName"].ToString())
                                ? row["ClientName"].ToString()
                                : (row.Table.Columns.Contains("CustomClientName") && row["CustomClientName"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["CustomClientName"].ToString())
                                    ? row["CustomClientName"].ToString()
                                    : "عميل نقدي");
                            string saleCode = row["SaleCode"]?.ToString() ?? "";
                            string saleDateStr = Convert.ToDateTime(row["SaleDate"]).ToString("yyyy/MM/dd hh:mm tt");
                            string userName = (!string.IsNullOrEmpty(Session.UserName) ? Session.UserName : (!string.IsNullOrEmpty(Session.EmpName) ? Session.EmpName : "المسؤول"));
                            string saleType = row["SaleType"].ToString() == "Credit" ? "آجل" : "نقدي";

                            g.FillRectangle(brAlt, 20, y, width - 40, metaH);
                            g.DrawRectangle(pThin, 20, y, width - 40, metaH);
                            g.DrawLine(pThin, width / 2, y, width / 2, y + metaH);

                            g.DrawString($"العميل:  {clientName}", fBold, Brushes.Black, new RectangleF(width / 2 + 10, y + 8, width / 2 - 25, 22), SfRtlRight);
                            g.DrawString($"رقم الفاتورة:  #{saleCode}", fBold, brPrimary, new RectangleF(width / 2 + 10, y + 34, width / 2 - 25, 22), SfRtlRight);

                            g.DrawString($"التاريخ:  {saleDateStr}", fNorm, Brushes.DarkSlateGray, new RectangleF(25, y + 8, width / 2 - 35, 22), SfRtlRight);
                            g.DrawString($"نوع البيع:  {saleType}  |  المستخدم: {userName}", fNorm, Brushes.Black, new RectangleF(25, y + 34, width / 2 - 35, 22), SfRtlRight);
                            y += metaH + 10;
                        }
                        else if (isCommercial)
                        {
                            g.DrawString(compName, fBig, brPrimary, new RectangleF(15, y, width - 30, 30), SfCenter);
                            y += 30;
                            if (!string.IsNullOrEmpty(compPhone))
                            {
                                g.DrawString($"موبايل: {compPhone}", fSub, Brushes.Black, new RectangleF(25, y, width - 50, 20), SfRtlRight);
                            }
                            g.DrawString("بيان أسعار ومبيعات موثق", fSub, brAccent, new RectangleF(25, y, width - 50, 20), SfCenter);
                            y += 26;
                            g.DrawLine(pBlack, 20, y, width - 20, y);
                            y += 8;

                            // Metadata (Commercial layout)
                            string clientName = row.Table.Columns.Contains("ClientName") && row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "عميل نقدي";
                            string saleCode = row["SaleCode"]?.ToString() ?? "";
                            string saleDateStr = Convert.ToDateTime(row["SaleDate"]).ToString("yyyy/MM/dd");
                            string userName = (!string.IsNullOrEmpty(Session.UserName) ? Session.UserName : (!string.IsNullOrEmpty(Session.EmpName) ? Session.EmpName : "المسؤول"));

                            g.DrawString($"اسم العميل /  {clientName}", fBold, Brushes.Black, new RectangleF(280, y, width - 300, 20), SfRtlRight);
                            g.DrawString($"المستخدم /  {userName}", fNorm, Brushes.Black, new RectangleF(25, y, 250, 20), SfRtlRight);
                            y += 22;

                            g.DrawString($"رقم البيان :  #{saleCode}", fBold, Brushes.Black, new RectangleF(280, y, width - 300, 20), SfRtlRight);
                            g.DrawString($"تاريخ البيان :  {saleDateStr}", fNorm, Brushes.Black, new RectangleF(25, y, 250, 20), SfRtlRight);
                            y += 26;
                        }
                        else
                        {
                            // Header Banner Box
                            using (var brHeaderGrad = new LinearGradientBrush(new Rectangle(12, 12, width - 24, 75), cPrimary, cSecondary, LinearGradientMode.Vertical))
                            {
                                g.FillRectangle(brHeaderGrad, 12, 12, width - 24, 75);
                            }
                            g.DrawString(compName, fBig, Brushes.White, new RectangleF(15, 20, width - 30, 32), SfCenter);
                            string tplBadge = isModern ? "فاتورة مبيعات إلكترونية موثقة (Modern Digital)"
                                            : isEmerald ? "إيصال مبيعات التجزئة والماركت (Retail Receipt)"
                                            : isGold ? "فاتورة مبيعات معتمدة للشركات (Corporate Invoice)"
                                            : "فاتورة مبيعات موثقة (Royal Blue)";
                            g.DrawString(tplBadge, fSmall, Brushes.LightGray, new RectangleF(15, 54, width - 30, 22), SfCenter);

                            y = 96;

                            // Meta Box
                            g.FillRectangle(brAlt, 20, y, width - 40, metaH);
                            g.DrawRectangle(pThin, 20, y, width - 40, metaH);
                            g.DrawLine(pThin, width / 2, y, width / 2, y + metaH);

                            string clientName = row.Table.Columns.Contains("ClientName") && row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "عميل نقدي";
                            string saleType = row["SaleType"].ToString() == "Credit" ? "آجل" : "نقدي";

                            g.DrawString($"رقم الفاتورة:  #{row["SaleCode"]}", fBold, brPrimary, new RectangleF(width / 2 + 10, y + 8, width / 2 - 25, 22), SfRtlRight);
                            g.DrawString($"التاريخ:  {Convert.ToDateTime(row["SaleDate"]):yyyy/MM/dd HH:mm}", fNorm, Brushes.DarkSlateGray, new RectangleF(width / 2 + 10, y + 36, width / 2 - 25, 22), SfRtlRight);

                            g.DrawString($"العميل:  {clientName}", fBold, Brushes.Black, new RectangleF(25, y + 8, width / 2 - 35, 22), SfRtlRight);
                            g.DrawString($"نوع البيع:  {saleType}", fNorm, Brushes.Black, new RectangleF(25, y + 36, width / 2 - 35, 22), SfRtlRight);

                            y += metaH + 12;
                        }

                        // ── 2. ITEMS TABLE ──────────────────────────
                        if (isCommercial || isAlTarek)
                        {
                            // 8 Columns: م(28) | كود(55) | صنف(215) | وحدة(45) | كمية(48) | سعر(65) | خصم(45) | إجمالي(75)
                            int wTot = 78, wDisc = 45, wPrice = 65, wQty = 48, wUnit = 45, wCode = 55, wIdx = 28;
                            int wName = (width - 40) - (wTot + wDisc + wPrice + wQty + wUnit + wCode + wIdx);

                            int curX = width - 20;
                            
                            // Header Row
                            Brush thBrush = isAlTarek ? brSecondary : new SolidBrush(Color.FromArgb(226, 232, 240));
                            Brush thTextBrush = isAlTarek ? Brushes.White : Brushes.Black;

                            g.FillRectangle(thBrush, 20, y, width - 40, tableHeaderH);
                            g.DrawRectangle(pBlack, 20, y, width - 40, tableHeaderH);

                            curX -= wIdx; g.DrawRectangle(pBlack, curX, y, wIdx, tableHeaderH); g.DrawString("م", fBold, thTextBrush, new RectangleF(curX, y + 6, wIdx, tableHeaderH - 6), SfCenter);
                            curX -= wCode; g.DrawRectangle(pBlack, curX, y, wCode, tableHeaderH); g.DrawString("الكود", fBold, thTextBrush, new RectangleF(curX, y + 6, wCode, tableHeaderH - 6), SfCenter);
                            curX -= wName; g.DrawRectangle(pBlack, curX, y, wName, tableHeaderH); g.DrawString(isAlTarek ? "اسم الصنف والبيان" : "اسم الصنف", fBold, thTextBrush, new RectangleF(curX, y + 6, wName, tableHeaderH - 6), SfCenter);
                            curX -= wUnit; g.DrawRectangle(pBlack, curX, y, wUnit, tableHeaderH); g.DrawString("الوحدة", fBold, thTextBrush, new RectangleF(curX, y + 6, wUnit, tableHeaderH - 6), SfCenter);
                            curX -= wQty; g.DrawRectangle(pBlack, curX, y, wQty, tableHeaderH); g.DrawString("الكمية", fBold, thTextBrush, new RectangleF(curX, y + 6, wQty, tableHeaderH - 6), SfCenter);
                            curX -= wPrice; g.DrawRectangle(pBlack, curX, y, wPrice, tableHeaderH); g.DrawString("سعر البيع", fBold, thTextBrush, new RectangleF(curX, y + 6, wPrice, tableHeaderH - 6), SfCenter);
                            curX -= wDisc; g.DrawRectangle(pBlack, curX, y, wDisc, tableHeaderH); g.DrawString("الخصم", fBold, thTextBrush, new RectangleF(curX, y + 6, wDisc, tableHeaderH - 6), SfCenter);
                            curX -= wTot; g.DrawRectangle(pBlack, curX, y, wTot, tableHeaderH); g.DrawString("إجمالي البيع", fBold, thTextBrush, new RectangleF(curX, y + 6, wTot, tableHeaderH - 6), SfCenter);

                            y += tableHeaderH;

                            int itemIdx = 1;
                            if (dtItems != null)
                            {
                                foreach (DataRow itm in dtItems.Rows)
                                {
                                    curX = width - 20;
                                    g.DrawRectangle(pBlack, 20, y, width - 40, rowH);

                                    string rawCode = itm.Table.Columns.Contains("ProductCode") ? itm["ProductCode"]?.ToString() : "";
                                    string pCode = ChickenDist.Forms.FrmPrintSale.FormatProductCode(rawCode);
                                    string pName = itm["ProductName"]?.ToString() ?? "";
                                    string uName = itm.Table.Columns.Contains("UnitName") && !string.IsNullOrWhiteSpace(itm["UnitName"]?.ToString()) ? itm["UnitName"].ToString() : "قطعة";
                                    decimal qVal = Convert.ToDecimal(itm["Quantity"]);
                                    decimal prVal = Convert.ToDecimal(itm["UnitPrice"]);
                                    decimal dVal = itm.Table.Columns.Contains("DiscountAmt") && itm["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(itm["DiscountAmt"]) : 0m;
                                    decimal tVal = Convert.ToDecimal(itm["TotalPrice"]);

                                    curX -= wIdx; g.DrawRectangle(pBlack, curX, y, wIdx, rowH); g.DrawString(itemIdx.ToString(), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wIdx, rowH - 4), SfCenter);
                                    curX -= wCode; g.DrawRectangle(pBlack, curX, y, wCode, rowH); g.DrawString(pCode, fSmall, Brushes.Black, new RectangleF(curX, y + 4, wCode, rowH - 4), SfCenter);
                                    curX -= wName; g.DrawRectangle(pBlack, curX, y, wName, rowH); g.DrawString(pName, fBold, Brushes.Black, new RectangleF(curX + 5, y + 4, wName - 10, rowH - 4), SfRtlRight);
                                    curX -= wUnit; g.DrawRectangle(pBlack, curX, y, wUnit, rowH); g.DrawString(uName, fSmall, Brushes.Black, new RectangleF(curX, y + 4, wUnit, rowH - 4), SfCenter);
                                    curX -= wQty; g.DrawRectangle(pBlack, curX, y, wQty, rowH); g.DrawString(qVal.ToString("0.##"), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wQty, rowH - 4), SfCenter);
                                    curX -= wPrice; g.DrawRectangle(pBlack, curX, y, wPrice, rowH); g.DrawString(prVal.ToString("N2"), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wPrice, rowH - 4), SfCenter);
                                    curX -= wDisc; g.DrawRectangle(pBlack, curX, y, wDisc, rowH); g.DrawString(dVal > 0 ? dVal.ToString("F1") : "-", fSmall, Brushes.Black, new RectangleF(curX, y + 4, wDisc, rowH - 4), SfCenter);
                                    curX -= wTot; g.DrawRectangle(pBlack, curX, y, wTot, rowH); g.DrawString(tVal.ToString("N2"), fBold, Brushes.Black, new RectangleF(curX, y + 4, wTot, rowH - 4), SfCenter);

                                    y += rowH;
                                    itemIdx++;
                                }
                            }
                        }
                        else
                        {
                            // 4 Columns: صنف (260) | كمية (100) | سعر (100) | إجمالي (120)
                            g.FillRectangle(brPrimary, 20, y, width - 40, tableHeaderH);
                            g.DrawRectangle(pThin, 20, y, width - 40, tableHeaderH);

                            g.DrawString("بيان الصنف", fBold, Brushes.White, new RectangleF(340, y + 7, width - 360, tableHeaderH - 7), SfRtlRight);
                            g.DrawString("الكمية", fBold, Brushes.White, new RectangleF(230, y + 7, 100, tableHeaderH - 7), SfCenter);
                            g.DrawString("السعر", fBold, Brushes.White, new RectangleF(130, y + 7, 95, tableHeaderH - 7), SfCenter);
                            g.DrawString("الإجمالي", fBold, Brushes.White, new RectangleF(25, y + 7, 100, tableHeaderH - 7), SfCenter);

                            y += tableHeaderH;

                            bool alt = false;
                            if (dtItems != null)
                            {
                                foreach (DataRow itm in dtItems.Rows)
                                {
                                    if (alt) g.FillRectangle(brAlt, 20, y, width - 40, rowH);
                                    g.DrawRectangle(pThin, 20, y, width - 40, rowH);
                                    g.DrawLine(pThin, 340, y, 340, y + rowH);
                                    g.DrawLine(pThin, 230, y, 230, y + rowH);
                                    g.DrawLine(pThin, 130, y, 130, y + rowH);

                                    string pName = itm["ProductName"]?.ToString() ?? "";
                                    decimal qVal = Convert.ToDecimal(itm["Quantity"]);
                                    string uName = itm.Table.Columns.Contains("UnitName") && !string.IsNullOrWhiteSpace(itm["UnitName"]?.ToString()) ? itm["UnitName"].ToString() : "";
                                    decimal prVal = Convert.ToDecimal(itm["UnitPrice"]);
                                    decimal tVal = Convert.ToDecimal(itm["TotalPrice"]);

                                    g.DrawString(pName, fBold, Brushes.Black, new RectangleF(345, y + 5, width - 370, rowH - 5), SfRtlRight);
                                    g.DrawString($"{qVal:0.##} {uName}".Trim(), fNorm, Brushes.Black, new RectangleF(230, y + 5, 100, rowH - 5), SfCenter);
                                    g.DrawString(prVal.ToString("N2"), fNorm, Brushes.Black, new RectangleF(130, y + 5, 95, rowH - 5), SfCenter);
                                    g.DrawString($"{tVal:N2} ج", fBold, brPrimary, new RectangleF(25, y + 5, 100, rowH - 5), SfCenter);

                                    y += rowH;
                                    alt = !alt;
                                }
                            }

                            y += 8;

                            // Net Total Highlight Box
                            g.FillRectangle(brPrimary, 300, y, width - 320, netH);
                            g.DrawString("صافي الفاتورة:", fBold, Brushes.White, new RectangleF(300, y + 10, width - 320, netH - 10), SfCenter);

                            g.FillRectangle(brAlt, 20, y, 270, netH);
                            g.DrawRectangle(pThin, 20, y, 270, netH);
                            g.DrawString($"{netVal:N2} ج.م", fBig, isEmerald ? brSecondary : (isGold ? brAccent : brRed), new RectangleF(20, y + 6, 270, netH - 6), SfCenter);

                            y += netH + 16;
                        }

                        // ── 3. FINANCIAL SUMMARY BOX ────────────────
                        if (isAlTarek)
                        {
                            // ════════ Al-Tarek 5-Column Grid Table ════════
                            decimal discVal = row.Table.Columns.Contains("DiscountAmount") && row["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmount"]) : 0m;
                            decimal beforeDisc = netVal + discVal;

                            int tLeft = 20;
                            int tWidth = width - 40;
                            int col5W = tWidth / 5;

                            // 5 Columns Headers
                            string[] h5 = { "إجمالي الفاتورة", "الخصم", "الصافي المطلوب", "المدفوع", "المتبقي (آجل)" };
                            string[] v5 = {
                                $"{beforeDisc:N2} ج",
                                discVal > 0 ? $"-{discVal:N2} ج" : "0.00 ج",
                                $"{netVal:N2} ج",
                                $"{paidVal:N2} ج",
                                $"{remainVal:N2} ج"
                            };

                            g.FillRectangle(brSecondary, tLeft, y, tWidth, 26);
                            g.DrawRectangle(pThin, tLeft, y, tWidth, 26);
                            for (int c = 0; c < 5; c++)
                            {
                                int cx = tLeft + tWidth - (c + 1) * col5W;
                                if (c < 4) g.DrawLine(Pens.White, cx, y, cx, y + 26);
                                g.DrawString(h5[c], fSmall, Brushes.White, new RectangleF(cx, y + 4, col5W, 20), SfCenter);
                            }
                            y += 26;

                            // 5 Columns Values
                            g.FillRectangle(brAlt, tLeft, y, tWidth, 28);
                            g.DrawRectangle(pThin, tLeft, y, tWidth, 28);
                            for (int c = 0; c < 5; c++)
                            {
                                int cx = tLeft + tWidth - (c + 1) * col5W;
                                if (c < 4) g.DrawLine(pThin, cx, y, cx, y + 28);
                                Brush vBr = (c == 2) ? brPrimary : (c == 4 && remainVal > 0) ? brRed : Brushes.Black;
                                g.DrawString(v5[c], fBold, vBr, new RectangleF(cx, y + 5, col5W, 22), SfCenter);
                            }
                            y += 32;

                            // Balance & Last Payment Box (صندوق الرصيد السابق والحالي وآخر توريد)
                            int balBoxW = tWidth;
                            int balBoxH = 54;
                            g.FillRectangle(new SolidBrush(Color.FromArgb(254, 242, 242)), tLeft, y, balBoxW, balBoxH);
                            g.DrawRectangle(pThin, tLeft, y, balBoxW, balBoxH);
                            g.DrawLine(pThin, tLeft + balBoxW / 2, y, tLeft + balBoxW / 2, y + 27);
                            g.DrawLine(pThin, tLeft, y + 27, tLeft + balBoxW, y + 27);

                            // Row 1: الرصيد السابق | الرصيد الحالي
                            g.DrawString($"الرصيد السابق للعميل:  {prevBalance:N2} ج.م", fNorm, brPrimary, new RectangleF(tLeft + balBoxW / 2 + 10, y + 5, balBoxW / 2 - 20, 20), SfRtlRight);
                            g.DrawString($"إجمالي الرصيد الحالي المستحق:  {actualCurrentBalance:N2} ج.م", fBold, brRed, new RectangleF(tLeft + 10, y + 5, balBoxW / 2 - 20, 20), SfRtlRight);

                            // Row 2: آخر توريد
                            string lastPayStr;
                            if (lastPaymentAmt > 0)
                            {
                                string dStr = lastPaymentDate > DateTime.MinValue ? $" بتاريخ {lastPaymentDate:yyyy/MM/dd}" : "";
                                lastPayStr = $"آخر توريد / سداد سابق للعميل:  {lastPaymentAmt:N2} ج.م{dStr}";
                            }
                            else if (!isCredit && paidVal > 0)
                            {
                                lastPayStr = $"آخر سداد:  {paidVal:N2} ج.م (مسدد مع الفاتورة)";
                            }
                            else if (isCredit)
                            {
                                lastPayStr = "حالة الفاتورة:  فاتورة آجل (غير مسددة نقداً)";
                            }
                            else
                            {
                                lastPayStr = "آخر توريد / سداد للعميل:  لا يوجد توريدات مسجلة";
                            }
                            g.DrawString(lastPayStr, fBold, Brushes.DarkGreen, new RectangleF(tLeft + 10, y + 31, balBoxW - 20, 20), SfRtlRight);
                            y += balBoxH + 8;

                            // Tafqeet Row
                            try
                            {
                                string taf = TafqeetHelper.ConvertToArabicWords(netVal);
                                g.FillRectangle(brAlt, tLeft, y, tWidth, 24);
                                g.DrawRectangle(pThin, tLeft, y, tWidth, 24);
                                g.DrawString($"فقط {taf} لا غير.", fSmall, Brushes.DarkSlateGray, new RectangleF(tLeft + 10, y + 3, tWidth - 20, 20), SfCenter);
                                y += 28;
                            }
                            catch { }

                            // Signatures
                            y += 8;
                            g.DrawString("توقيع المستلم: ...............................", fSmall, Brushes.Black, new RectangleF(30, y, 220, 20), SfRtlRight);
                            g.DrawString("توقيع الحسابات: ...............................", fSmall, Brushes.Black, new RectangleF(width - 250, y, 220, 20), SfRtlRight);
                            y += 24;
                        }
                        else if (showFinancial)
                        {
                            if (isCommercial)
                            {
                                int boxW = 270;
                                int boxX = 20;
                                int boxRowH = 24;

                                decimal discVal = row.Table.Columns.Contains("DiscountAmount") && row["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmount"]) : 0m;
                                decimal beforeDisc = netVal + discVal;

                                string[] sumLabels = { "الإجمالي قبل الخصم", "الخصم", "الإجمالي بعد الخصم", "المدفوع نقداً", "المتبقي (آجل)", "الرصيد السابق", "الرصيد الحالي" };
                                string[] sumValues = {
                                    $"{beforeDisc:N2} ج",
                                    discVal > 0 ? $"-{discVal:N2} ج" : "0.00 ج",
                                    $"{netVal:N2} ج",
                                    $"{paidVal:N2} ج",
                                    $"{remainVal:N2} ج",
                                    $"{prevBalance:N2} ج",
                                    $"{actualCurrentBalance:N2} ج"
                                };

                                g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), boxX, y, boxW, sumLabels.Length * boxRowH);
                                g.DrawRectangle(pBlack, boxX, y, boxW, sumLabels.Length * boxRowH);

                                for (int si = 0; si < sumLabels.Length; si++)
                                {
                                    int rowY = y + si * boxRowH;
                                    g.DrawRectangle(pBlack, boxX, rowY, boxW, boxRowH);
                                    g.DrawLine(pBlack, boxX + 130, rowY, boxX + 130, rowY + boxRowH);

                                    bool isBoldLine = (si == 2 || si == 6);
                                    g.DrawString(sumLabels[si], isBoldLine ? fBold : fNorm, isBoldLine ? brPrimary : Brushes.Black, new RectangleF(boxX + 132, rowY + 3, 135, boxRowH - 4), SfRtlRight);
                                    g.DrawString(sumValues[si], isBoldLine ? fBold : fNorm, isBoldLine ? brRed : Brushes.Black, new RectangleF(boxX + 5, rowY + 3, 122, boxRowH - 4), SfCenter);
                                }

                                // Right side info
                                int rX = boxX + boxW + 20;
                                int rW = width - 20 - rX;
                                g.DrawString("ملاحظات الفاتورة:", fBold, brPrimary, new RectangleF(rX, y + 10, rW, 20), SfRtlRight);
                                string noteStr = row.Table.Columns.Contains("Notes") && !string.IsNullOrWhiteSpace(row["Notes"]?.ToString()) ? row["Notes"].ToString() : "البضاعة المباعة ترد وتستبدل خلال 14 يوماً طبقاً للشروط";
                                g.DrawString(noteStr, fSmall, Brushes.DarkSlateGray, new RectangleF(rX, y + 32, rW, 60), SfRtlRight);

                                y += sumLabels.Length * boxRowH + 15;

                                // Commercial Signatures
                                g.DrawString("توقيع المستلم: ...............................", fBold, Brushes.Black, 30, y);
                                g.DrawString("توقيع البائع: ...............................", fBold, Brushes.Black, new RectangleF(0, y, width - 30, 20), SfRtlLeft);
                                y += 28;
                            }
                            else
                            {
                                // Standard / Modern / Gold Financial Box
                                g.FillRectangle(brSecondary, 20, y, width - 40, 28);
                                g.DrawString("الوضع المالي وحساب العميل", fBold, Brushes.White, new RectangleF(20, y + 4, width - 40, 24), SfCenter);
                                y += 28;

                                var finLabels = new List<string> { "الرصيد السابق للعميل" };
                                var finVals   = new List<string> { $"{prevBalance:N2} ج.م" };

                                if (isCredit)
                                {
                                    finLabels.Add("قيمة الفاتورة (آجل)");
                                    finVals.Add($"{netVal:N2} ج.م");
                                }
                                else
                                {
                                    if (paidVal > 0)
                                    {
                                        finLabels.Add("المدفوع من الفاتورة");
                                        finVals.Add($"{paidVal:N2} ج.م");
                                    }
                                    if (remainVal > 0)
                                    {
                                        finLabels.Add("المتبقي من الفاتورة (آجل)");
                                        finVals.Add($"{remainVal:N2} ج.م");
                                    }
                                }
                                if (todayPayments > 0)
                                {
                                    finLabels.Add("المسدد اليوم");
                                    finVals.Add($"{todayPayments:N2} ج.م");
                                }
                                if (lastPaymentAmt > 0)
                                {
                                    string dStr = lastPaymentDate > DateTime.MinValue ? $" ({lastPaymentDate:yyyy/MM/dd})" : "";
                                    finLabels.Add("آخر توريد سابق");
                                    finVals.Add($"{lastPaymentAmt:N2} ج.م{dStr}");
                                }

                                finLabels.Add("الرصيد الحالي المستحق");
                                finVals.Add($"{actualCurrentBalance:N2} ج.م");

                                for (int i = 0; i < finLabels.Count; i++)
                                {
                                    bool isLast = (i == finLabels.Count - 1);
                                    if (isLast) g.FillRectangle(new SolidBrush(Color.FromArgb(254, 242, 242)), 20, y, width - 40, 28);
                                    g.DrawRectangle(pThin, 20, y, width - 40, 28);
                                    g.DrawLine(pThin, width / 2, y, width / 2, y + 28);

                                    g.DrawString(finLabels[i], isLast ? fBold : fNorm, isLast ? brRed : brPrimary, new RectangleF(width / 2 + 10, y + 5, width / 2 - 25, 22), SfRtlRight);
                                    g.DrawString(finVals[i], isLast ? fBold : fNorm, isLast ? brRed : Brushes.Black, new RectangleF(25, y + 5, width / 2 - 35, 22), SfRtlRight);

                                    y += 28;
                                }
                                y += 12;
                            }
                        }

                        // ── 4. FOOTER ───────────────────────────────
                        if (!isAlTarek)
                        {
                            if (!isCommercial)
                            {
                                g.DrawLine(pAccent, 20, y, width - 20, y);
                                y += 8;
                                g.DrawString("شكراً لتعاملكم معنا ونتمنى لكم دوام التوفيق والنجاح", fSub, brPrimary, new RectangleF(0, y, width, 24), SfCenter);
                                y += 24;
                            }

                            // باركود الفاتورة للرجوع والاسترجاع بالاسكانر
                            string invoiceCode = (row.Table.Columns.Contains("SaleCode") && row["SaleCode"] != DBNull.Value) ? row["SaleCode"].ToString() : "";
                            if (string.IsNullOrWhiteSpace(invoiceCode) && row.Table.Columns.Contains("SaleID") && row["SaleID"] != DBNull.Value)
                                invoiceCode = row["SaleID"].ToString();
                            if (!string.IsNullOrWhiteSpace(invoiceCode))
                            {
                                y += 6;
                                int bcW = 240;
                                int bcH = 40;
                                int bcX = (width - bcW) / 2;
                                g.FillRectangle(Brushes.White, bcX - 6, y, bcW + 12, bcH + 18);
                                BarcodeEngine.DrawCode128(g, invoiceCode, bcX, y, bcW, bcH, isWide: false);
                                y += bcH + 2;
                                g.DrawString($"*{invoiceCode}*", fSmall, Brushes.Black, new RectangleF(0, y, width, 16), SfCenter);
                                y += 18;
                            }

                            // System signature
                            using (var fPromo = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                            {
                                g.DrawString("تم إنشاء هذا المستند آلياً بواسطة Pro System لإدارة المبيعات والتوزيع", fPromo, Brushes.Gray, new RectangleF(0, y, width, 20), SfCenter);
                            }
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
                DbHelper.EnsureCashBoxColumnsExist();

                // 1. استرجاع بيانات السند من جدول الخزنة الرئيسي CashBox مع اسم الجهة والخزنة والموظف
                var dt = DbHelper.Query(@"
                    SELECT cb.CashID, cb.TransDate, cb.TransType, cb.AmountIn, cb.AmountOut,
                           cb.RefID, cb.Notes,
                           ISNULL(sa.AccountName, N'الخزينة الرئيسية') AS AccountName,
                           ISNULL(emp.EmpName, N'المشرف العام') AS CreatedByName,
                           COALESCE(
                               NULLIF(LTRIM(RTRIM(cb.PartyName)), ''),
                               CASE WHEN cb.TransType = 'ClientPayment' AND cb.Notes LIKE N'%تحصيل أقساط العقد%'
                                    THEN (SELECT TOP 1 c.ClientName FROM InstallmentContracts ic JOIN Clients c ON ic.CustomerID = c.ClientID WHERE ic.ContractID = cb.RefID)
                                    WHEN cb.TransType = 'ClientPayment' AND cb.Notes LIKE N'%مقدم عقد التقسيط%'
                                    THEN (SELECT TOP 1 c.ClientName FROM Sales s JOIN Clients c ON s.ClientID = c.ClientID WHERE s.SaleID = cb.RefID)
                                    WHEN cb.TransType = 'ClientPayment'
                                    THEN COALESCE(
                                        (SELECT TOP 1 c.ClientName FROM Clients c WHERE c.ClientID = cb.RefID),
                                        (SELECT TOP 1 c.ClientName FROM Sales s JOIN Clients c ON s.ClientID = c.ClientID WHERE s.SaleID = cb.RefID)
                                    )
                                    ELSE NULL END,
                               CASE WHEN cb.TransType = 'SupplierPayment'
                                    THEN (SELECT TOP 1 s.SupplierName FROM Suppliers s WHERE s.SupplierID = cb.RefID)
                                    ELSE NULL END,
                               CASE WHEN cb.TransType IN ('Deposit', 'Withdraw') AND (cb.Notes LIKE N'%[مورد:%' OR cb.Notes LIKE N'%صرف للمورد%')
                                    THEN (SELECT TOP 1 s.SupplierName FROM Suppliers s WHERE s.SupplierID = cb.RefID)
                                    WHEN cb.TransType IN ('Deposit', 'Withdraw') AND (cb.Notes LIKE N'%[عميل:%' OR cb.Notes LIKE N'%تحصيل من عميل%')
                                    THEN (SELECT TOP 1 c.ClientName FROM Clients c WHERE c.ClientID = cb.RefID)
                                    WHEN cb.TransType = 'Deposit' AND cb.RefID IS NOT NULL
                                    THEN COALESCE(
                                        (SELECT TOP 1 c.ClientName FROM Clients c WHERE c.ClientID = cb.RefID),
                                        (SELECT TOP 1 s.SupplierName FROM Suppliers s WHERE s.SupplierID = cb.RefID)
                                    )
                                    WHEN cb.TransType = 'Withdraw' AND cb.RefID IS NOT NULL
                                    THEN COALESCE(
                                        (SELECT TOP 1 s.SupplierName FROM Suppliers s WHERE s.SupplierID = cb.RefID),
                                        (SELECT TOP 1 c.ClientName FROM Clients c WHERE c.ClientID = cb.RefID)
                                    )
                                    ELSE NULL END,
                               CASE WHEN cb.TransType IN ('SaleIncome', 'Sale')
                                    THEN (SELECT TOP 1 COALESCE(NULLIF(LTRIM(RTRIM(c.ClientName)), ''), NULLIF(LTRIM(RTRIM(s.CustomClientName)), ''), N'عميل نقدي')
                                          FROM Sales s LEFT JOIN Clients c ON s.ClientID = c.ClientID WHERE s.SaleID = cb.RefID)
                                    ELSE NULL END,
                               CASE WHEN cb.TransType IN ('ClientPurchaseCash', 'PurchaseExpense')
                                    THEN (SELECT TOP 1 COALESCE(NULLIF(LTRIM(RTRIM(s.SupplierName)), ''), NULLIF(LTRIM(RTRIM(c.ClientName)), ''), N'مورد نقدي')
                                          FROM Purchases p LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID LEFT JOIN Clients c ON p.ClientID = c.ClientID WHERE p.PurchaseID = cb.RefID)
                                    ELSE NULL END,
                               CASE WHEN cb.TransType = 'Expense'
                                    THEN (SELECT TOP 1 COALESCE(NULLIF(LTRIM(RTRIM(s.SupplierName)), ''), e.ExpenseType)
                                          FROM Expenses e LEFT JOIN Suppliers s ON e.SupplierID = s.SupplierID WHERE e.ExpenseID = cb.RefID)
                                    ELSE NULL END,
                               ''
                           ) AS ResolvedPartyName
                    FROM CashBox cb
                    LEFT JOIN SafeAccounts sa ON cb.AccountID = sa.AccountID
                    LEFT JOIN Employees emp ON cb.CreatedBy = emp.EmpID
                    WHERE cb.CashID = @id", DbHelper.P("@id", voucherID));

                string voucherCode = voucherID.ToString();
                DateTime voucherDate = DateTime.Now;
                string rawType = "Deposit";
                decimal inAmt = 0m, outAmt = 0m, amount = 0m;
                string rawNotes = "";
                string safeName = "الخزينة الرئيسية";
                string createdByName = Session.EmpName ?? "المشرف العام";
                string partyName = "";
                int refID = 0;

                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    voucherCode = row["CashID"].ToString();
                    voucherDate = row["TransDate"] != DBNull.Value ? Convert.ToDateTime(row["TransDate"]) : DateTime.Now;
                    rawType = row["TransType"]?.ToString() ?? "Deposit";
                    inAmt = row["AmountIn"] != DBNull.Value ? Convert.ToDecimal(row["AmountIn"]) : 0m;
                    outAmt = row["AmountOut"] != DBNull.Value ? Convert.ToDecimal(row["AmountOut"]) : 0m;
                    amount = inAmt > 0 ? inAmt : outAmt;
                    rawNotes = row["Notes"]?.ToString() ?? "";
                    safeName = row["AccountName"]?.ToString() ?? "الخزينة الرئيسية";
                    createdByName = row["CreatedByName"]?.ToString() ?? "المشرف العام";
                    partyName = row["ResolvedPartyName"]?.ToString()?.Trim() ?? "";
                    refID = row["RefID"] != DBNull.Value ? Convert.ToInt32(row["RefID"]) : 0;
                }
                else
                {
                    // Fallback للجدول القديم إن وجد
                    var dtLegacy = DbHelper.Query(@"
                        SELECT v.VoucherID, v.VoucherCode, v.VoucherDate, v.VoucherType, v.Amount, v.Notes,
                               c.ClientName, c.ClientID
                        FROM ReceiptVouchers v
                        LEFT JOIN Clients c ON v.ClientID = c.ClientID
                        WHERE v.VoucherID = @id", DbHelper.P("@id", voucherID));
                    if (dtLegacy == null || dtLegacy.Rows.Count == 0) return null;

                    var row = dtLegacy.Rows[0];
                    voucherCode = row["VoucherCode"]?.ToString() ?? voucherID.ToString();
                    voucherDate = row["VoucherDate"] != DBNull.Value ? Convert.ToDateTime(row["VoucherDate"]) : DateTime.Now;
                    rawType = row["VoucherType"]?.ToString() ?? "Deposit";
                    bool legIn = rawType.Contains("In") || rawType.Contains("Receipt") || rawType.Contains("Deposit");
                    amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : 0m;
                    inAmt = legIn ? amount : 0m;
                    outAmt = legIn ? 0m : amount;
                    rawNotes = row["Notes"]?.ToString() ?? "";
                    partyName = row["ClientName"]?.ToString() ?? "";
                    refID = row["ClientID"] != DBNull.Value ? Convert.ToInt32(row["ClientID"]) : 0;
                }

                // استخلاص اسم الجهة وتنظيف الملاحظات
                string cleanNotes = rawNotes;
                string balanceNoteFromText = "";

                if (string.IsNullOrWhiteSpace(partyName))
                {
                    partyName = Forms.FrmCashBox.ExtractPartyFromNotes(rawNotes);
                }

                // إزالة الوسوم [عميل: ...] أو [مورد: ...] من نص البيان المعروض ليكون نظيفاً
                while (cleanNotes.Contains("[") && cleanNotes.Contains("]"))
                {
                    int sIdx = cleanNotes.IndexOf('[');
                    int eIdx = cleanNotes.IndexOf(']', sIdx);
                    if (eIdx > sIdx)
                    {
                        string tagContent = cleanNotes.Substring(sIdx + 1, eIdx - sIdx - 1).Trim();
                        if (string.IsNullOrWhiteSpace(partyName))
                        {
                            int cIdx = tagContent.IndexOf(':');
                            partyName = cIdx >= 0 ? tagContent.Substring(cIdx + 1).Trim() : tagContent;
                        }
                        cleanNotes = (cleanNotes.Substring(0, sIdx) + " " + cleanNotes.Substring(eIdx + 1)).Trim();
                    }
                    else break;
                }

                if (cleanNotes.Contains("(رصيد"))
                {
                    int balIdx = cleanNotes.IndexOf("(رصيد");
                    int closeP = cleanNotes.IndexOf(')', balIdx);
                    if (closeP > balIdx)
                    {
                        balanceNoteFromText = cleanNotes.Substring(balIdx + 1, closeP - balIdx - 1).Trim();
                        cleanNotes = (cleanNotes.Substring(0, balIdx) + " " + cleanNotes.Substring(closeP + 1)).Trim();
                    }
                }

                string[] stripPrefixes = { "تحصيل من عميل -", "تحصيل من عميل:", "تحصيل -", "سداد من عميل -", "سداد للمورد -", "صرف للمورد -", "صرف للمورد:", "توريد نقدية -", "صرف نقدية -" };
                foreach (var pref in stripPrefixes)
                {
                    if (cleanNotes.StartsWith(pref, StringComparison.OrdinalIgnoreCase))
                        cleanNotes = cleanNotes.Substring(pref.Length).Trim();
                }
                cleanNotes = cleanNotes.Trim('-', ':', ' ');
                if (string.IsNullOrWhiteSpace(cleanNotes))
                {
                    cleanNotes = (inAmt > 0) ? "توريد نقدي / دفعة تحت الحساب" : "صرف نقدي / دفعة تحت الحساب";
                }

                bool isDeposit = inAmt > 0 || rawType == "Deposit" || rawType == "ClientPayment" || rawType == "SaleIncome";
                bool isTransfer = rawType.Contains("Transfer") || rawType.Contains("ShiftClose");

                // تحديد هل الجهة عميل أم مورد لحساب الموقف المالي (الرصيد السابق والحالي)
                bool isClient = rawType == "ClientPayment" || rawNotes.Contains("[عميل:") || rawNotes.Contains("تحصيل من عميل");
                bool isSupplier = rawType == "SupplierPayment" || rawNotes.Contains("[مورد:") || rawNotes.Contains("صرف للمورد");
                string partyRoleBadge = isClient ? "عميل" : (isSupplier ? "مورد" : "");
                string partyPhone = "";
                bool hasFinancialSummary = false;
                decimal prevBalance = 0m;
                decimal currentBalance = 0m;

                if (!isClient && !isSupplier && refID > 0 && (rawType == "Deposit" || rawType == "Withdraw"))
                {
                    if (isDeposit)
                    {
                        var cCheck = DbHelper.Scalar("SELECT COUNT(1) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", refID));
                        if (Convert.ToInt32(cCheck) > 0) { isClient = true; partyRoleBadge = "عميل"; }
                        else
                        {
                            var sCheck = DbHelper.Scalar("SELECT COUNT(1) FROM Suppliers WHERE SupplierID=@id", DbHelper.P("@id", refID));
                            if (Convert.ToInt32(sCheck) > 0) { isSupplier = true; partyRoleBadge = "مورد"; }
                        }
                    }
                    else
                    {
                        var sCheck = DbHelper.Scalar("SELECT COUNT(1) FROM Suppliers WHERE SupplierID=@id", DbHelper.P("@id", refID));
                        if (Convert.ToInt32(sCheck) > 0) { isSupplier = true; partyRoleBadge = "مورد"; }
                        else
                        {
                            var cCheck = DbHelper.Scalar("SELECT COUNT(1) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", refID));
                            if (Convert.ToInt32(cCheck) > 0) { isClient = true; partyRoleBadge = "عميل"; }
                        }
                    }
                }

                if (isClient && refID > 0 && rawType != "SaleIncome" && rawType != "Sale")
                {
                    try
                    {
                        currentBalance = ClientDAL.GetBalance(refID);
                        prevBalance = isDeposit ? (currentBalance + amount) : (currentBalance - amount);
                        hasFinancialSummary = true;
                        var phObj = DbHelper.Scalar("SELECT Phone FROM Clients WHERE ClientID=@id", DbHelper.P("@id", refID));
                        if (phObj != null && phObj != DBNull.Value) partyPhone = phObj.ToString().Trim();
                    }
                    catch { }
                }
                else if (isSupplier && refID > 0)
                {
                    try
                    {
                        currentBalance = SupplierDAL.GetBalance(refID);
                        prevBalance = !isDeposit ? (currentBalance + amount) : (currentBalance - amount);
                        hasFinancialSummary = true;
                        var phObj = DbHelper.Scalar("SELECT Phone FROM Suppliers WHERE SupplierID=@id", DbHelper.P("@id", refID));
                        if (phObj != null && phObj != DBNull.Value) partyPhone = phObj.ToString().Trim();
                    }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(partyName))
                {
                    partyName = isClient ? "عميل" : (isSupplier ? "مورد" : (rawType == "Expense" ? "مصروفات تشغيلية" : "تعامل نقدي عام"));
                }

                string typeArabic = rawType switch
                {
                    "Deposit" => isClient ? "سند توريد نقدي (عميل)" : (isSupplier ? "سند توريد نقدي (مورد)" : "سند توريد نقدي"),
                    "Withdraw" => isSupplier ? "سند صرف نقدي (مورد)" : (isClient ? "سند صرف نقدي (عميل)" : "سند صرف نقدي"),
                    "ClientPayment" => "سند تحصيل نقدي من عميل",
                    "SupplierPayment" => "سند سداد نقدي لمورد",
                    "SaleIncome" => "توريد مبيعات نقدية",
                    "Sale" => rawNotes.Contains("فيزا") ? "تحصيل مبيعات (فيزا)" : "توريد مبيعات نقدية",
                    "PurchaseExpense" => "صرف مشتريات نقدية",
                    "ClientPurchaseCash" => "صرف شراء نقدي من عميل",
                    "ReturnOutcome" => "صرف مرتجع مبيعات",
                    "PurchaseReturn" => "توريد مرتجع مشتريات",
                    "Expense" => "سند صرف مصروفات",
                    "Transfer" => "سند تحويل نقدية بين الحسابات",
                    _ => isDeposit ? "سند توريد نقدي (قبض)" : "سند صرف نقدي (دفع)"
                };

                // ألوان الهوية البصرية حسب نوع السند
                Color cHeaderDark   = isTransfer ? Color.FromArgb(30, 27, 75)   : (isDeposit ? Color.FromArgb(6, 78, 59)    : Color.FromArgb(127, 29, 29));
                Color cHeaderLight  = isTransfer ? Color.FromArgb(55, 48, 163)  : (isDeposit ? Color.FromArgb(4, 120, 87)   : Color.FromArgb(185, 28, 28));
                Color cAccent       = isTransfer ? Color.FromArgb(79, 70, 229)  : (isDeposit ? Color.FromArgb(5, 150, 105)  : Color.FromArgb(220, 38, 38));
                Color cSoftBg       = isTransfer ? Color.FromArgb(238, 242, 255): (isDeposit ? Color.FromArgb(236, 253, 245): Color.FromArgb(254, 242, 242));
                Color cSoftBorder   = isTransfer ? Color.FromArgb(165, 180, 252): (isDeposit ? Color.FromArgb(110, 231, 183): Color.FromArgb(252, 165, 165));
                Color cGoldStripe   = Color.FromArgb(245, 158, 11);

                string docMainTitle = isTransfer ? "سند تحويل نقدية بين الحسابات"
                                    : (isDeposit ? "إيصال استلام نقدية  (سند توريد / قبض)"
                                                 : "إيصال صرف نقدية  (سند صرف / دفع)");

                string partyLabel   = isTransfer ? "الحساب / الجهة:"
                                    : (isDeposit ? "استلمنا من السيد / السادة:"
                                                 : "صرفنا إلى السيد / السادة:");

                string amountLabel  = isTransfer ? "مبلغ التحويل النقدي:"
                                    : (isDeposit ? "المبلغ المستلم (وارد للخزينة):"
                                                 : "المبلغ المصروف (صادر من الخزينة):");

                string tafqeetWords = "";
                try { tafqeetWords = TafqeetHelper.ConvertToArabicWords(amount); } catch { }

                int width = 880;
                // حساب ارتفاع البيان التفصيلي
                int notesBoxHeight = 56;
                using (var tmpBmp = new Bitmap(1, 1))
                using (var tmpG = Graphics.FromImage(tmpBmp))
                using (var fNotesMeasure = new Font("Segoe UI", 14.5f, FontStyle.Bold))
                {
                    SizeF sz = tmpG.MeasureString(cleanNotes, fNotesMeasure, 580);
                    notesBoxHeight = Math.Max(54, (int)Math.Ceiling(sz.Height) + 22);
                }

                int finSectionHeight = (hasFinancialSummary || !string.IsNullOrWhiteSpace(balanceNoteFromText)) ? 118 : 0;
                int height = 565 + notesBoxHeight + finSectionHeight;

                var bmp = new Bitmap(width, height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.FromArgb(248, 250, 252));

                    // إطار خارجي أنيق للكارت
                    using (var penOuter = new Pen(Color.FromArgb(203, 213, 225), 2f))
                    {
                        g.DrawRectangle(penOuter, 1, 1, width - 3, height - 3);
                    }

                    // ── 1. الهيدر العلوي الرسمي للشركة ──────────────────────────
                    int headerH = 112;
                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, headerH), cHeaderDark, cHeaderLight, LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, headerH);
                    }
                    using (var brStripe = new SolidBrush(cGoldStripe))
                    {
                        g.FillRectangle(brStripe, 0, headerH - 5, width, 5);
                    }

                    string companyName = AppConfig.CompanyName;
                    if (string.IsNullOrWhiteSpace(companyName)) companyName = "شركة برو سوفت للأنظمة المتكاملة";

                    using (var fComp = new Font("Segoe UI", 22f, FontStyle.Bold))
                    using (var fInfo = new Font("Segoe UI", 12f, FontStyle.Bold))
                    using (var brSubInfo = new SolidBrush(Color.FromArgb(226, 232, 240)))
                    {
                        g.DrawString(companyName, fComp, Brushes.White, new RectangleF(200, 12, width - 230, 44), SfRtlRight);

                        string subLine = "";
                        if (!string.IsNullOrWhiteSpace(AppConfig.CompanyAddress)) subLine += "" + AppConfig.CompanyAddress.Trim();
                        if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone))
                            subLine += (subLine.Length > 0 ? "   |   " : "") + "هاتف: " + AppConfig.CompanyPhone.Trim();
                        if (string.IsNullOrWhiteSpace(subLine))
                            subLine = "إيصال مالي رسمي معتمد وموثق إلكترونياً";

                        g.DrawString(CleanEmoji(subLine), fInfo, brSubInfo, new RectangleF(200, 60, width - 230, 34), SfRtlRight);
                    }

                    // لوجو الشركة في اليسار إن وجد، أو بادج رقم السند
                    bool drewLogo = false;
                    if (!string.IsNullOrWhiteSpace(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                    {
                        try
                        {
                            using (var logoImg = Image.FromFile(AppConfig.ShopLogoPath))
                            {
                                g.FillRectangle(Brushes.White, 28, 14, 140, 78);
                                g.DrawImage(logoImg, new Rectangle(32, 18, 132, 70));
                                drewLogo = true;
                            }
                        }
                        catch { }
                    }
                    if (!drewLogo)
                    {
                        using (var brBadge = new SolidBrush(Color.FromArgb(45, 255, 255, 255)))
                        using (var penBadge = new Pen(Color.FromArgb(120, 255, 255, 255), 1.5f))
                        using (var fBadgeTitle = new Font("Segoe UI", 11f, FontStyle.Bold))
                        using (var fBadgeNum = new Font("Segoe UI", 18f, FontStyle.Bold))
                        {
                            g.FillRectangle(brBadge, 28, 16, 165, 76);
                            g.DrawRectangle(penBadge, 28, 16, 165, 76);
                            g.DrawString("رقم السند المالي", fBadgeTitle, Brushes.WhiteSmoke, new RectangleF(28, 22, 165, 24), SfCenter);
                            g.DrawString($"#{voucherCode}", fBadgeNum, Brushes.White, new RectangleF(28, 48, 165, 38), SfCenter);
                        }
                    }

                    int y = headerH + 14;
                    int marginX = 26;
                    int contentW = width - (marginX * 2);

                    // ── 2. شريط عنوان السند الرئيسي ────────────────────────────
                    int titleBarH = 54;
                    using (var brTitleBg = new SolidBrush(cSoftBg))
                    using (var penTitle = new Pen(cAccent, 2f))
                    using (var fDocTitle = new Font("Segoe UI", 19f, FontStyle.Bold))
                    using (var brDocTitle = new SolidBrush(cHeaderDark))
                    {
                        g.FillRectangle(brTitleBg, marginX, y, contentW, titleBarH);
                        g.DrawRectangle(penTitle, marginX, y, contentW, titleBarH);
                        g.DrawString(docMainTitle, fDocTitle, brDocTitle, new RectangleF(marginX, y, contentW, titleBarH), SfCenter);
                    }
                    y += titleBarH + 12;

                    // ── 3. شريط رقم السند والتاريخ والوقت ──────────────────────
                    int metaH = 48;
                    int halfW = (contentW - 14) / 2;
                    using (var penBox = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                    using (var fMetaLbl = new Font("Segoe UI", 13.5f, FontStyle.Bold))
                    using (var fMetaVal = new Font("Segoe UI", 15.5f, FontStyle.Bold))
                    using (var brDark = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brMuted = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    {
                        // يمين: رقم السند ونوعه
                        int rightBoxX = marginX + halfW + 14;
                        g.FillRectangle(Brushes.White, rightBoxX, y, halfW, metaH);
                        g.DrawRectangle(penBox, rightBoxX, y, halfW, metaH);
                        g.DrawString("رقم السند:", fMetaLbl, brMuted, new RectangleF(rightBoxX + halfW - 125, y, 115, metaH), SfRtlRight);
                        using (var brAccentTxt = new SolidBrush(cAccent))
                        {
                            g.DrawString($"#{voucherCode}   ({typeArabic})", fMetaVal, brAccentTxt, new RectangleF(rightBoxX + 10, y, halfW - 135, metaH), SfRtlRight);
                        }

                        // يسار: التاريخ والوقت
                        int leftBoxX = marginX;
                        g.FillRectangle(Brushes.White, leftBoxX, y, halfW, metaH);
                        g.DrawRectangle(penBox, leftBoxX, y, halfW, metaH);
                        g.DrawString("التاريخ والوقت:", fMetaLbl, brMuted, new RectangleF(leftBoxX + halfW - 145, y, 135, metaH), SfRtlRight);
                        g.DrawString(voucherDate.ToString("yyyy/MM/dd   hh:mm tt"), fMetaVal, brDark, new RectangleF(leftBoxX + 10, y, halfW - 155, metaH), SfRtlRight);
                    }
                    y += metaH + 12;

                    // ── 4. كارت اسم الجهة (العميل / المورد) بخط كبير وواضح ──────
                    int partyCardH = 66;
                    using (var penParty = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
                    using (var brAccentBar = new SolidBrush(cAccent))
                    using (var fPartyLbl = new Font("Segoe UI", 14.5f, FontStyle.Bold))
                    using (var fPartyVal = new Font("Segoe UI", 19.5f, FontStyle.Bold))
                    using (var brPartyVal = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brMuted = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    {
                        g.FillRectangle(Brushes.White, marginX, y, contentW, partyCardH);
                        g.DrawRectangle(penParty, marginX, y, contentW, partyCardH);
                        g.FillRectangle(brAccentBar, marginX + contentW - 8, y, 8, partyCardH);

                        g.DrawString(partyLabel, fPartyLbl, brMuted, new RectangleF(marginX + contentW - 245, y, 228, partyCardH), SfRtlRight);

                        string displayParty = partyName;
                        if (!string.IsNullOrWhiteSpace(partyRoleBadge) && !displayParty.Contains(partyRoleBadge))
                            displayParty = $"{partyName}   [{partyRoleBadge}]";
                        if (!string.IsNullOrWhiteSpace(partyPhone))
                            displayParty += $"   -   هاتف: {partyPhone}";

                        g.DrawString(displayParty, fPartyVal, brPartyVal, new RectangleF(marginX + 16, y, contentW - 265, partyCardH), SfRtlRight);
                    }
                    y += partyCardH + 12;

                    // ── 5. كارت المبلغ الرئيسي والتفقيط بالحروف ─────────────────
                    int amtCardH = 116;
                    using (var brAmtBg = new SolidBrush(cSoftBg))
                    using (var penAmt = new Pen(cAccent, 2.2f))
                    using (var penDiv = new Pen(cSoftBorder, 1.5f))
                    using (var fAmtLbl = new Font("Segoe UI", 16.5f, FontStyle.Bold))
                    using (var fAmtDigits = new Font("Segoe UI", 27f, FontStyle.Bold))
                    using (var fTafqeet = new Font("Segoe UI", 14.5f, FontStyle.Bold))
                    using (var brAmtColor = new SolidBrush(cHeaderDark))
                    using (var brAccentColor = new SolidBrush(cAccent))
                    {
                        g.FillRectangle(brAmtBg, marginX, y, contentW, amtCardH);
                        g.DrawRectangle(penAmt, marginX, y, contentW, amtCardH);

                        // السطر الأول: المبلغ بالأرقام الكبيرة
                        g.DrawString(amountLabel, fAmtLbl, brAmtColor, new RectangleF(marginX + contentW - 360, y + 8, 340, 56), SfRtlRight);
                        g.DrawString($"{amount:N2} جنيه مصري", fAmtDigits, brAccentColor, new RectangleF(marginX + 20, y + 6, contentW - 385, 58), SfRtlLeft);

                        // خط فاصل
                        g.DrawLine(penDiv, marginX + 20, y + 68, marginX + contentW - 20, y + 68);

                        // السطر الثاني: التفقيط بالحروف العربية
                        string tafqeetLine = !string.IsNullOrWhiteSpace(tafqeetWords)
                            ? $"فقط وقدره:  {tafqeetWords} لا غير"
                            : $"إجمالي المبلغ المسدد: {amount:N2} ج.م";
                        g.DrawString(tafqeetLine, fTafqeet, brAmtColor, new RectangleF(marginX + 12, y + 72, contentW - 24, 38), SfCenter);
                    }
                    y += amtCardH + 12;

                    // ── 6. جدول البيان المحاسبي والتفاصيل والخزنة ──────────────
                    using (var penGrid = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                    using (var brHeaderCell = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    using (var fCellLbl = new Font("Segoe UI", 13.5f, FontStyle.Bold))
                    using (var fCellVal = new Font("Segoe UI", 14.5f, FontStyle.Bold))
                    using (var brLbl = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    using (var brVal = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        // سطر البيان والتفاصيل
                        int lblColW = 190;
                        int valColW = contentW - lblColW;

                        g.FillRectangle(Brushes.White, marginX, y, valColW, notesBoxHeight);
                        g.FillRectangle(brHeaderCell, marginX + valColW, y, lblColW, notesBoxHeight);
                        g.DrawRectangle(penGrid, marginX, y, contentW, notesBoxHeight);
                        g.DrawLine(penGrid, marginX + valColW, y, marginX + valColW, y + notesBoxHeight);

                        g.DrawString("البيان والتفاصيل:", fCellLbl, brLbl, new RectangleF(marginX + valColW + 8, y, lblColW - 16, notesBoxHeight), SfRtlRight);
                        g.DrawString(cleanNotes, fCellVal, brVal, new RectangleF(marginX + 12, y + 6, valColW - 24, notesBoxHeight - 12), SfRtlRight);
                        y += notesBoxHeight;

                        // سطر الخزنة ومحرر السند
                        int row2H = 46;
                        int halfColW = contentW / 2;
                        int subLblW = 145;
                        int subValW = halfColW - subLblW;

                        g.FillRectangle(Brushes.White, marginX, y, contentW, row2H);
                        g.FillRectangle(brHeaderCell, marginX + halfColW + subValW, y, subLblW, row2H);
                        g.FillRectangle(brHeaderCell, marginX + subValW, y, subLblW, row2H);
                        g.DrawRectangle(penGrid, marginX, y, contentW, row2H);
                        g.DrawLine(penGrid, marginX + halfColW, y, marginX + halfColW, y + row2H);
                        g.DrawLine(penGrid, marginX + halfColW + subValW, y, marginX + halfColW + subValW, y + row2H);
                        g.DrawLine(penGrid, marginX + subValW, y, marginX + subValW, y + row2H);

                        // النصف الأيمن: الخزنة / الحساب
                        g.DrawString("الخزنة / الحساب:", fCellLbl, brLbl, new RectangleF(marginX + halfColW + subValW + 6, y, subLblW - 12, row2H), SfRtlRight);
                        g.DrawString(safeName, fCellVal, brVal, new RectangleF(marginX + halfColW + 10, y, subValW - 20, row2H), SfRtlRight);

                        // النصف الأيسر: محرر السند
                        g.DrawString("محرر السند:", fCellLbl, brLbl, new RectangleF(marginX + subValW + 6, y, subLblW - 12, row2H), SfRtlRight);
                        g.DrawString(createdByName, fCellVal, brVal, new RectangleF(marginX + 10, y, subValW - 20, row2H), SfRtlRight);
                        y += row2H + 14;
                    }

                    // ── 7. قسم الموقف المالي وكشف الرصيد (للعميل أو المورد) ────
                    if (hasFinancialSummary)
                    {
                        int finH = 98;
                        int cardW = (contentW - 24) / 3;

                        string prevState = isClient
                            ? (prevBalance > 0 ? "عليه (مدين)" : (prevBalance < 0 ? "له (دائن)" : "خالص"))
                            : (prevBalance > 0 ? "له (دائن)" : (prevBalance < 0 ? "عليه (مدين)" : "خالص"));

                        string curState = isClient
                            ? (currentBalance > 0 ? "عليه (مدين)" : (currentBalance < 0 ? "له (دائن)" : "خالص تماماً"))
                            : (currentBalance > 0 ? "له (دائن)" : (currentBalance < 0 ? "عليه (مدين)" : "خالص تماماً"));

                        using (var fFinHeader = new Font("Segoe UI", 13f, FontStyle.Bold))
                        using (var fFinNum = new Font("Segoe UI", 18.5f, FontStyle.Bold))
                        using (var fFinSub = new Font("Segoe UI", 12f, FontStyle.Bold))
                        {
                            // الكارت الأيمن: الرصيد السابق قبل السند
                            int c1X = marginX + (cardW + 12) * 2;
                            DrawFinancialCard(g, c1X, y, cardW, finH,
                                "الرصيد السابق قبل السند",
                                $"{Math.Abs(prevBalance):N2} ج",
                                prevState,
                                Color.FromArgb(241, 245, 249), Color.FromArgb(148, 163, 184), Color.FromArgb(30, 41, 59),
                                fFinHeader, fFinNum, fFinSub);

                            // الكارت الأوسط: قيمة السند الحالي
                            int c2X = marginX + (cardW + 12);
                            DrawFinancialCard(g, c2X, y, cardW, finH,
                                isDeposit ? "المدفوع / المورّد بهذا السند" : "المصروف / المسدد بهذا السند",
                                $"{amount:N2} ج",
                                isDeposit ? "تم خصمه من المديونية" : "تم قيده بالحساب",
                                cSoftBg, cAccent, cHeaderDark,
                                fFinHeader, fFinNum, fFinSub);

                            // الكارت الأيسر: الرصيد الحالي المتبقي
                            int c3X = marginX;
                            bool hasDebt = Math.Abs(currentBalance) > 0.01m;
                            Color c3Bg = hasDebt ? Color.FromArgb(254, 242, 242) : Color.FromArgb(236, 253, 245);
                            Color c3Border = hasDebt ? Color.FromArgb(239, 68, 68) : Color.FromArgb(16, 185, 129);
                            Color c3Txt = hasDebt ? Color.FromArgb(153, 27, 27) : Color.FromArgb(6, 95, 70);
                            DrawFinancialCard(g, c3X, y, cardW, finH,
                                "الرصيد النهائي المتبقي الآن",
                                $"{Math.Abs(currentBalance):N2} ج",
                                curState,
                                c3Bg, c3Border, c3Txt,
                                fFinHeader, fFinNum, fFinSub);
                        }
                        y += finH + 14;
                    }
                    else if (!string.IsNullOrWhiteSpace(balanceNoteFromText))
                    {
                        int bNoteH = 54;
                        using (var brBNote = new SolidBrush(Color.FromArgb(254, 249, 195)))
                        using (var penBNote = new Pen(Color.FromArgb(234, 179, 8), 1.5f))
                        using (var fBNote = new Font("Segoe UI", 14.5f, FontStyle.Bold))
                        using (var brBNoteTxt = new SolidBrush(Color.FromArgb(113, 63, 18)))
                        {
                            g.FillRectangle(brBNote, marginX, y, contentW, bNoteH);
                            g.DrawRectangle(penBNote, marginX, y, contentW, bNoteH);
                            g.DrawString(balanceNoteFromText, fBNote, brBNoteTxt, new RectangleF(marginX + 10, y, contentW - 20, bNoteH), SfCenter);
                        }
                        y += bNoteH + 14;
                    }

                    // ── 8. التوقيعات والباركود والتذييل الرسمي ─────────────────
                    using (var penDash = new Pen(Color.FromArgb(203, 213, 225), 1.5f) { DashStyle = DashStyle.Dash })
                    using (var fSigTitle = new Font("Segoe UI", 13f, FontStyle.Bold))
                    using (var fFooter = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                    using (var brSig = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    using (var brFooter = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawLine(penDash, marginX, y, marginX + contentW, y);
                        y += 10;

                        // التوقيع الأيمن
                        g.DrawString("توقيع المستلم / المتعامل", fSigTitle, brSig, new RectangleF(width - 280, y, 250, 26), SfCenter);
                        g.DrawString("........................................", fSigTitle, Brushes.Gray, new RectangleF(width - 280, y + 28, 250, 24), SfCenter);

                        // الباركود في المنتصف
                        try
                        {
                            int bcW = 220, bcH = 38;
                            int bcX = (width - bcW) / 2;
                            g.FillRectangle(Brushes.White, bcX - 6, y, bcW + 12, bcH + 8);
                            BarcodeEngine.DrawCode128(g, voucherCode, bcX, y + 4, bcW, bcH, isWide: false);
                        }
                        catch { }

                        // التوقيع الأيسر
                        g.DrawString("أمين الخزينة / الاعتماد", fSigTitle, brSig, new RectangleF(30, y, 250, 26), SfCenter);
                        g.DrawString("........................................", fSigTitle, Brushes.Gray, new RectangleF(30, y + 28, 250, 24), SfCenter);

                        y += 62;
                        g.DrawString($"شكراً لتعاملكم مع {companyName}  —  مستند إلكتروني رسمي معتمد (#{voucherCode})", fFooter, brFooter, new RectangleF(0, y, width, 26), SfCenter);
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

        private static void DrawFinancialCard(Graphics g, int x, int y, int w, int h, string title, string value, string subtitle, Color bgColor, Color borderColor, Color textColor, Font fHeader, Font fNum, Font fSub)
        {
            using (var brBg = new SolidBrush(bgColor))
            using (var penBorder = new Pen(borderColor, 2f))
            using (var brTopBar = new SolidBrush(borderColor))
            using (var brText = new SolidBrush(textColor))
            using (var brTitle = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                g.FillRectangle(brBg, x, y, w, h);
                g.DrawRectangle(penBorder, x, y, w, h);
                g.FillRectangle(brTopBar, x, y, w, 5);

                g.DrawString(title, fHeader, brTitle, new RectangleF(x + 4, y + 8, w - 8, 24), SfCenter);
                g.DrawString(value, fNum, brText, new RectangleF(x + 4, y + 33, w - 8, 34), SfCenter);
                g.DrawString(subtitle, fSub, brText, new RectangleF(x + 4, y + 68, w - 8, 24), SfCenter);
            }
        }

        public static Bitmap GenerateClientStatementImage(int clientID, string clientName, decimal balance, string extraNotes = "")
        {
            try
            {
                int width = 820;
                int height = 460;
                var bmp = new Bitmap(width, height);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.FromArgb(248, 250, 252));

                    // Header
                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 105), Color.FromArgb(30, 58, 138), Color.FromArgb(29, 78, 216), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 105);
                    }

                    string companyName = AppConfig.CompanyName;
                    if (string.IsNullOrWhiteSpace(companyName)) companyName = "شركة برو سوفت للأنظمة الإلكترونية";

                    using (var fTitle = new Font("Segoe UI", 21f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 13.5f, FontStyle.Bold))
                    {
                        g.DrawString(companyName, fTitle, Brushes.White, new RectangleF(0, 14, width, 42), SfCenter);
                        g.DrawString("إشعار كشف حساب وموقف مالي للعميل", fSub, Brushes.WhiteSmoke, new RectangleF(0, 58, width, 32), SfCenter);
                    }

                    int y = 122;

                    using (var fBold = new Font("Segoe UI", 15f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 13.5f, FontStyle.Bold))
                    using (var brClient = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brBoxBg = new SolidBrush(balance > 0 ? Color.FromArgb(254, 242, 242) : Color.FromArgb(240, 253, 244)))
                    using (var penBox = new Pen(balance > 0 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(34, 197, 94), 2f))
                    using (var brBalText = new SolidBrush(balance > 0 ? Color.FromArgb(185, 28, 28) : Color.FromArgb(22, 101, 52)))
                    {
                        g.DrawString($"اسم العميل: {clientName} (كود: #{clientID})", fBold, brClient, new RectangleF(320, y, 470, 36), SfRtlRight);
                        g.DrawString($"التاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}", fNorm, Brushes.DarkSlateGray, new RectangleF(30, y, 280, 36), SfRtlLeft);
                        y += 48;

                        // Balance Highlight Card
                        g.FillRectangle(brBoxBg, 30, y, width - 60, 110);
                        g.DrawRectangle(penBox, 30, y, width - 60, 110);

                        using (var fBal = new Font("Segoe UI", 22f, FontStyle.Bold))
                        {
                            string balLabel = balance > 0 ? "الرصيد المستحق (مديونية على العميل):" : (balance < 0 ? "الرصيد المتبقي (دائن للعميل):" : "الرصيد الحالي:");
                            g.DrawString(balLabel, fBold, brBalText, new RectangleF(380, y + 14, 390, 42), SfRtlRight);
                            g.DrawString($"{Math.Abs(balance):N2} جنيه مصري", fBal, brBalText, new RectangleF(45, y + 12, 330, 44), SfRtlLeft);
                        }

                        string statusText = balance > 0 ? "نرجو التكرم بسداد المبلغ المستحق في أقرب وقت، شاكرين حسن تعاونكم." : "الحساب خالص ومطابق تماماً.";
                        g.DrawString(statusText, fNorm, brBalText, new RectangleF(45, y + 66, width - 90, 32), SfCenter);
                        y += 130;

                        if (!string.IsNullOrWhiteSpace(extraNotes))
                        {
                            g.DrawString($"ملاحظات: {extraNotes}", fNorm, Brushes.Black, new RectangleF(30, y, width - 60, 34), SfRtlRight);
                            y += 44;
                        }

                        using (var fFooter = new Font("Segoe UI", 12f, FontStyle.Bold))
                        {
                            g.DrawString("شكراً لتعاملكم معنا ونتمنى لكم دوام التوفيق والنجاح", fFooter, Brushes.DimGray, new RectangleF(0, y + 10, width, 30), SfCenter);
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

                int width = 820;
                int rowHeight = 40;
                int baseHeight = 220 + (lineCount * rowHeight);
                var bmp = new Bitmap(width, baseHeight);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.FromArgb(248, 250, 252));

                    using (var penOuter = new Pen(Color.FromArgb(203, 213, 225), 2f))
                    {
                        g.DrawRectangle(penOuter, 1, 1, width - 3, baseHeight - 3);
                    }

                    // Header Gradient
                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 105), Color.FromArgb(15, 23, 42), Color.FromArgb(30, 58, 138), LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 105);
                    }
                    using (var brGold = new SolidBrush(Color.FromArgb(245, 158, 11)))
                    {
                        g.FillRectangle(brGold, 0, 100, width, 5);
                    }

                    string companyName = AppConfig.CompanyName;
                    if (string.IsNullOrWhiteSpace(companyName)) companyName = "شركة برو سوفت للأنظمة المتكاملة";

                    using (var fTitle = new Font("Segoe UI", 21f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 14f, FontStyle.Bold))
                    {
                        g.DrawString(companyName, fTitle, Brushes.White, new RectangleF(0, 14, width, 42), SfCenter);
                        string sub = !string.IsNullOrWhiteSpace(title) ? title : (!string.IsNullOrWhiteSpace(subtitle) ? subtitle : "إشعار رسمي موثق من النظام");
                        sub = CleanEmoji(sub).Replace("*", "").Trim();
                        g.DrawString(sub, fSub, Brushes.WhiteSmoke, new RectangleF(0, 58, width, 32), SfCenter);
                    }

                    int y = 122;
                    using (var fBold = new Font("Segoe UI", 15.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 14.5f, FontStyle.Bold))
                    using (var penLine = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                    using (var brPrimary = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brAccent = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    {
                        g.FillRectangle(Brushes.White, 24, y - 6, width - 48, baseHeight - y - 48);
                        g.DrawRectangle(penLine, 24, y - 6, width - 48, baseHeight - y - 48);

                        foreach (string rawLine in lines)
                        {
                            string line = CleanEmoji(rawLine?.Trim() ?? "");
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                y += 14;
                                continue;
                            }

                            if (line.StartsWith("---") || line.StartsWith("===") || line.StartsWith("───") || line.StartsWith("━━━"))
                            {
                                g.DrawLine(penLine, 40, y + 14, width - 40, y + 14);
                                y += 26;
                                continue;
                            }

                            bool isHeader = line.StartsWith("*") && line.EndsWith("*") && line.IndexOf('*', 1) == line.Length - 1;
                            // إزالة كل علامات النجمة الخاصة بالماركداون حتى لا تظهر في الصورة
                            string cleanText = line.Replace("*", "").Trim();

                            if (isHeader)
                            {
                                g.DrawString(cleanText, fBold, brAccent, new RectangleF(40, y, width - 80, 36), SfRtlRight);
                            }
                            else
                            {
                                g.DrawString(cleanText, fNorm, brPrimary, new RectangleF(40, y, width - 80, 36), SfRtlRight);
                            }

                            y += rowHeight;
                        }

                        using (var fFooter = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                        {
                            g.DrawString("تم إنشاء هذا المستند آلياً بواسطة النظام", fFooter, Brushes.DimGray, new RectangleF(0, baseHeight - 38, width, 28), SfCenter);
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

        /// <summary>
        /// توليد صور كشف حساب العميل المالي التفصيلي (حتى صورتين بجودة عالية ووضوح كامل للجداول)
        /// </summary>
        public static List<Bitmap> GenerateDetailedClientStatementImages(string clientName, string phone, DateTime fromDate, DateTime toDate, DataGridView dgStatement, decimal totalSales, decimal totalReturns, decimal totalPayments, decimal balance)
        {
            var pages = new List<Bitmap>();
            try
            {
                var rows = new List<string[]>();
                if (dgStatement != null)
                {
                    int idx = 1;
                    foreach (DataGridViewRow dgr in dgStatement.Rows)
                    {
                        if (dgr.IsNewRow) continue;
                        string dtStr   = dgStatement.Columns.Contains("TransDate") ? (dgr.Cells["TransDate"]?.Value?.ToString() ?? "") : (dgr.Cells.Count > 0 ? dgr.Cells[0].Value?.ToString() ?? "" : "");
                        string typeStr = dgStatement.Columns.Contains("TransType") ? (dgr.Cells["TransType"]?.Value?.ToString() ?? "") : (dgr.Cells.Count > 1 ? dgr.Cells[1].Value?.ToString() ?? "" : "");
                        string debit   = dgStatement.Columns.Contains("Debit") ? (dgr.Cells["Debit"]?.Value?.ToString() ?? "") : "";
                        string credit  = dgStatement.Columns.Contains("Credit") ? (dgr.Cells["Credit"]?.Value?.ToString() ?? "") : "";
                        string bal     = dgStatement.Columns.Contains("Balance") ? (dgr.Cells["Balance"]?.Value?.ToString() ?? "") : "";
                        string details = dgStatement.Columns.Contains("Notes") ? (dgr.Cells["Notes"]?.Value?.ToString() ?? "") : (dgStatement.Columns.Contains("Details") ? (dgr.Cells["Details"]?.Value?.ToString() ?? "") : "");

                        // Format numbers nicely
                        if (decimal.TryParse(debit, out decimal dVal) && dVal > 0) debit = dVal.ToString("N2");
                        if (decimal.TryParse(credit, out decimal cVal) && cVal > 0) credit = cVal.ToString("N2");
                        if (decimal.TryParse(bal, out decimal bVal)) bal = bVal.ToString("N2");

                        rows.Add(new[] { idx.ToString(), dtStr, typeStr, details, debit, credit, bal });
                        idx++;
                    }
                }

                int totalRowCount = rows.Count;
                int maxRowsPerPage = totalRowCount > 25 ? 25 : Math.Max(totalRowCount, 1);
                int totalPages = totalRowCount > 25 ? 2 : 1;

                int width = 880;
                int[] colW = { 35, 110, 85, 335, 90, 90, 95 }; // Total = 840 (توسيع خانة البيان وتعديل باقي الأعمدة)
                string[] colHeaders = { "م", "التاريخ والوقت", "نوع الحركة", "تفاصيل البيان / الفاتورة", "مدين (+)", "دائن (-)", "الرصيد (ج)" };

                using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                using (var fSub = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (var fBold = new Font("Segoe UI", 10f, FontStyle.Bold))
                using (var fNorm = new Font("Segoe UI", 9.5f))
                using (var fSmall = new Font("Segoe UI", 8.5f))
                using (var tempBmp = new Bitmap(1, 1))
                using (var gTemp = Graphics.FromImage(tempBmp))
                {
                    for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
                    {
                        int startRow = pageIdx * 25;
                        int takeCount = pageIdx == 0 && totalPages > 1 ? 25 : (totalRowCount - startRow);
                        if (takeCount < 0) takeCount = 0;

                        // قياس الارتفاع الديناميكي لكل سطر حسب محتوى البيان لعدم قص أي كلام
                        int[] pageRowHeights = new int[takeCount];
                        int totalPageRowsHeight = 0;
                        for (int r = 0; r < takeCount; r++)
                        {
                            int actualRowIdx = startRow + r;
                            if (actualRowIdx >= totalRowCount) break;
                            string det = rows[actualRowIdx][3] ?? "";
                            var sz = gTemp.MeasureString(det, fNorm, colW[3] - 8);
                            int h = (int)Math.Ceiling(sz.Height) + 12;
                            int rH = Math.Max(34, Math.Min(h, 75));
                            pageRowHeights[r] = rH;
                            totalPageRowsHeight += rH;
                        }

                        int dynamicHeight = 220 + totalPageRowsHeight + (pageIdx == totalPages - 1 ? 160 : 70);
                        dynamicHeight = Math.Max(dynamicHeight, 450);

                        var bmp = new Bitmap(width, dynamicHeight);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                            g.Clear(Color.White);

                            using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 85), Color.FromArgb(15, 45, 90), Color.FromArgb(30, 64, 175), LinearGradientMode.Vertical))
                            using (var brNavy = new SolidBrush(Color.FromArgb(15, 45, 90)))
                            using (var brAlt = new SolidBrush(Color.FromArgb(248, 250, 252)))
                            using (var brRed = new SolidBrush(Color.FromArgb(185, 28, 28)))
                            using (var brGreen = new SolidBrush(Color.FromArgb(22, 101, 52)))
                            using (var penBorder = new Pen(Color.FromArgb(15, 45, 90), 2f))
                            using (var penGrid = new Pen(Color.FromArgb(203, 213, 225), 1f))
                            {
                                // Outer Border
                                g.DrawRectangle(penBorder, 10, 10, width - 20, dynamicHeight - 20);

                                // Header Banner
                                g.FillRectangle(brHeader, 12, 12, width - 24, 75);
                                string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة للأنظمة والتجارة";
                                g.DrawString(comp, fTitle, Brushes.White, new RectangleF(20, 18, width - 40, 32), SfCenter);
                                g.DrawString("كشف حساب مالي تفصيلي للمعاملات والمديونية", fSub, Brushes.LightCyan, new RectangleF(20, 50, width - 40, 24), SfCenter);

                                int y = 96;

                                // Meta Card
                                g.FillRectangle(brAlt, 20, y, 840, 58);
                                g.DrawRectangle(penGrid, 20, y, 840, 58);
                                g.DrawLine(penGrid, width / 2, y, width / 2, y + 58);

                                g.DrawString($"العميل: {clientName}" + (!string.IsNullOrWhiteSpace(phone) ? $" | {phone}" : ""), fBold, brNavy, new RectangleF(width / 2 + 10, y + 6, width / 2 - 30, 24), SfRtlRight);
                                g.DrawString($"الفترة: من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}", fNorm, Brushes.DarkSlateGray, new RectangleF(width / 2 + 10, y + 30, width / 2 - 30, 22), SfRtlRight);

                                g.DrawString($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", fSmall, Brushes.Gray, new RectangleF(30, y + 6, width / 2 - 50, 22), SfRtlRight);
                                if (totalPages > 1)
                                {
                                    g.DrawString($"صفحة {pageIdx + 1} من {totalPages}", fBold, brNavy, new RectangleF(30, y + 30, width / 2 - 50, 22), SfRtlRight);
                                }

                                y += 66;

                                // Table Header
                                int tLeft = 20;
                                int thH = 34;
                                g.FillRectangle(brNavy, tLeft, y, 840, thH);
                                g.DrawRectangle(penBorder, tLeft, y, 840, thH);

                                int curX = tLeft + 840;
                                for (int c = 0; c < colHeaders.Length; c++)
                                {
                                    curX -= colW[c];
                                    g.DrawString(colHeaders[c], fBold, Brushes.White, new RectangleF(curX, y + 4, colW[c], thH - 6), SfCenter);
                                    if (c > 0) g.DrawLine(Pens.White, curX, y, curX, y + thH);
                                }
                                y += thH;

                                // Rows
                                bool alt = false;
                                for (int r = 0; r < takeCount; r++)
                                {
                                    int actualRowIdx = startRow + r;
                                    if (actualRowIdx >= totalRowCount) break;

                                    var rowData = rows[actualRowIdx];
                                    int thisRowH = pageRowHeights[r];
                                    if (alt) g.FillRectangle(brAlt, tLeft, y, 840, thisRowH);
                                    g.DrawRectangle(penGrid, tLeft, y, 840, thisRowH);

                                    curX = tLeft + 840;
                                    for (int c = 0; c < rowData.Length; c++)
                                    {
                                        curX -= colW[c];
                                        var sf = (c == 3) ? SfRtlRight : SfCenter;
                                        Brush brush = Brushes.Black;
                                        Font font = fNorm;

                                        if (c == 4 && !string.IsNullOrEmpty(rowData[c])) { brush = brRed; font = fBold; }
                                        else if (c == 5 && !string.IsNullOrEmpty(rowData[c])) { brush = brGreen; font = fBold; }
                                        else if (c == 6) { brush = brNavy; font = fBold; }
                                        else if (c == 0) { font = fSmall; }

                                        float textY = (c == 3) ? (y + 5) : (y + (thisRowH - 20) / 2f);
                                        if (textY < y + 3) textY = y + 3;
                                        g.DrawString(rowData[c], font, brush, new RectangleF(curX + 3, textY, colW[c] - 6, thisRowH - 6), sf);
                                        if (c > 0) g.DrawLine(penGrid, curX, y, curX, y + thisRowH);
                                    }

                                    y += thisRowH;
                                    alt = !alt;
                                }

                                // Summary Box (Only on Last Page)
                                if (pageIdx == totalPages - 1)
                                {
                                    y += 12;
                                    g.FillRectangle(brAlt, tLeft, y, 840, 78);
                                    g.DrawRectangle(penBorder, tLeft, y, 840, 78);

                                    g.DrawString($"إجمالي المبيعات: {totalSales:N2} ج", fBold, brRed, new RectangleF(tLeft + 560, y + 8, 260, 26), SfRtlRight);
                                    g.DrawString($"إجمالي التحصيلات: {totalPayments:N2} ج" + (totalReturns > 0 ? $" | مرتجع: {totalReturns:N2} ج" : ""), fBold, brGreen, new RectangleF(tLeft + 560, y + 42, 260, 26), SfRtlRight);

                                    // Final Balance Highlight Box
                                    string balTitle = balance > 0 ? "الرصيد المستحق (مديونية مطلوبة):" : (balance < 0 ? "الرصيد المتبقي (دائن لصالح العميل):" : "الرصيد النهائي:");
                                    Brush balBr = balance > 0 ? brRed : (balance < 0 ? brGreen : brNavy);
                                    using (var fBalNum = new Font("Segoe UI", 15f, FontStyle.Bold))
                                    {
                                        g.DrawString(balTitle, fBold, balBr, new RectangleF(tLeft + 20, y + 8, 520, 24), SfRtlLeft);
                                        g.DrawString($"{Math.Abs(balance):N2} جنيه مصري", fBalNum, balBr, new RectangleF(tLeft + 20, y + 36, 520, 36), SfRtlLeft);
                                    }

                                    y += 88;
                                }

                                // Footer
                                g.DrawString("شكراً لتعاملكم ونتمنى لكم دوام التوفيق والنجاح", fSmall, Brushes.Gray, new RectangleF(20, y + 4, 840, 20), SfCenter);
                            }
                        }
                        pages.Add(bmp);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateDetailedClientStatementImages", ex);
            }

            if (pages.Count == 0)
            {
                var fallback = GenerateClientStatementImage(0, clientName, balance);
                if (fallback != null) pages.Add(fallback);
            }

            return pages;
        }

        /// <summary>
        /// توليد صور كشف حساب المورد المالي التفصيلي (حتى صورتين بجودة عالية)
        /// </summary>
        public static List<Bitmap> GenerateDetailedSupplierStatementImages(string supplierName, string phone, DateTime fromDate, DateTime toDate, DataGridView dgStatement, decimal totalPurchases, decimal totalPayments, decimal balance)
        {
            var pages = new List<Bitmap>();
            try
            {
                var rows = new List<string[]>();
                if (dgStatement != null)
                {
                    int idx = 1;
                    foreach (DataGridViewRow dgr in dgStatement.Rows)
                    {
                        if (dgr.IsNewRow) continue;
                        string dtStr   = dgStatement.Columns.Contains("TransDate") ? (dgr.Cells["TransDate"]?.Value?.ToString() ?? "") : (dgr.Cells.Count > 0 ? dgr.Cells[0].Value?.ToString() ?? "" : "");
                        string typeStr = dgStatement.Columns.Contains("TransType") ? (dgr.Cells["TransType"]?.Value?.ToString() ?? "") : (dgr.Cells.Count > 1 ? dgr.Cells[1].Value?.ToString() ?? "" : "");
                        string debit   = dgStatement.Columns.Contains("Debit") ? (dgr.Cells["Debit"]?.Value?.ToString() ?? "") : (dgStatement.Columns.Contains("Paid") ? dgr.Cells["Paid"]?.Value?.ToString() ?? "" : "");
                        string credit  = dgStatement.Columns.Contains("Credit") ? (dgr.Cells["Credit"]?.Value?.ToString() ?? "") : (dgStatement.Columns.Contains("Purchases") ? dgr.Cells["Purchases"]?.Value?.ToString() ?? "" : "");
                        string bal     = dgStatement.Columns.Contains("Balance") ? (dgr.Cells["Balance"]?.Value?.ToString() ?? "") : "";
                        string details = dgStatement.Columns.Contains("Notes") ? (dgr.Cells["Notes"]?.Value?.ToString() ?? "") : (dgStatement.Columns.Contains("Details") ? (dgr.Cells["Details"]?.Value?.ToString() ?? "") : "");

                        if (decimal.TryParse(debit, out decimal dVal) && dVal > 0) debit = dVal.ToString("N2");
                        if (decimal.TryParse(credit, out decimal cVal) && cVal > 0) credit = cVal.ToString("N2");
                        if (decimal.TryParse(bal, out decimal bVal)) bal = bVal.ToString("N2");

                        rows.Add(new[] { idx.ToString(), dtStr, typeStr, details, debit, credit, bal });
                        idx++;
                    }
                }

                int totalRowCount = rows.Count;
                int totalPages = totalRowCount > 25 ? 2 : 1;
                int width = 880;
                int[] colW = { 35, 110, 85, 335, 90, 90, 95 }; // Total = 840 (توسيع خانة البيان وتعديل باقي الأعمدة)
                string[] colHeaders = { "م", "التاريخ والوقت", "نوع الحركة", "تفاصيل البيان / الفاتورة", "مدين (سداد)", "دائن (مشتريات)", "الرصيد (ج)" };

                using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                using (var fSub = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (var fBold = new Font("Segoe UI", 10f, FontStyle.Bold))
                using (var fNorm = new Font("Segoe UI", 9.5f))
                using (var fSmall = new Font("Segoe UI", 8.5f))
                using (var tempBmp = new Bitmap(1, 1))
                using (var gTemp = Graphics.FromImage(tempBmp))
                {
                    for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
                    {
                        int startRow = pageIdx * 25;
                        int takeCount = pageIdx == 0 && totalPages > 1 ? 25 : (totalRowCount - startRow);
                        if (takeCount < 0) takeCount = 0;

                        // قياس الارتفاع الديناميكي لكل سطر حسب محتوى البيان لعدم قص أي كلام
                        int[] pageRowHeights = new int[takeCount];
                        int totalPageRowsHeight = 0;
                        for (int r = 0; r < takeCount; r++)
                        {
                            int actualRowIdx = startRow + r;
                            if (actualRowIdx >= totalRowCount) break;
                            string det = rows[actualRowIdx][3] ?? "";
                            var sz = gTemp.MeasureString(det, fNorm, colW[3] - 8);
                            int h = (int)Math.Ceiling(sz.Height) + 12;
                            int rH = Math.Max(34, Math.Min(h, 75));
                            pageRowHeights[r] = rH;
                            totalPageRowsHeight += rH;
                        }

                        int dynamicHeight = 220 + totalPageRowsHeight + (pageIdx == totalPages - 1 ? 160 : 70);
                        dynamicHeight = Math.Max(dynamicHeight, 450);

                        var bmp = new Bitmap(width, dynamicHeight);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                            g.Clear(Color.White);

                            using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 85), Color.FromArgb(120, 53, 15), Color.FromArgb(180, 83, 9), LinearGradientMode.Vertical))
                            using (var brAmber = new SolidBrush(Color.FromArgb(120, 53, 15)))
                            using (var brAlt = new SolidBrush(Color.FromArgb(254, 252, 232)))
                            using (var brRed = new SolidBrush(Color.FromArgb(185, 28, 28)))
                            using (var brGreen = new SolidBrush(Color.FromArgb(22, 101, 52)))
                            using (var penBorder = new Pen(Color.FromArgb(180, 83, 9), 2f))
                            using (var penGrid = new Pen(Color.FromArgb(226, 232, 240), 1f))
                            {
                                g.DrawRectangle(penBorder, 10, 10, width - 20, dynamicHeight - 20);

                                g.FillRectangle(brHeader, 12, 12, width - 24, 75);
                                string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة للأنظمة والتجارة";
                                g.DrawString(comp, fTitle, Brushes.White, new RectangleF(20, 18, width - 40, 32), SfCenter);
                                g.DrawString("كشف حساب مالي تفصيلي للمورد", fSub, Brushes.LightYellow, new RectangleF(20, 50, width - 40, 24), SfCenter);

                                int y = 96;

                                // Meta Card
                                g.FillRectangle(brAlt, 20, y, 840, 58);
                                g.DrawRectangle(penGrid, 20, y, 840, 58);
                                g.DrawLine(penGrid, width / 2, y, width / 2, y + 58);

                                g.DrawString($"المورد: {supplierName}" + (!string.IsNullOrWhiteSpace(phone) ? $" | {phone}" : ""), fBold, brAmber, new RectangleF(width / 2 + 10, y + 6, width / 2 - 30, 24), SfRtlRight);
                                g.DrawString($"الفترة: من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}", fNorm, Brushes.DarkSlateGray, new RectangleF(width / 2 + 10, y + 30, width / 2 - 30, 22), SfRtlRight);

                                g.DrawString($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", fSmall, Brushes.Gray, new RectangleF(30, y + 6, width / 2 - 50, 22), SfRtlRight);
                                if (totalPages > 1)
                                {
                                    g.DrawString($"صفحة {pageIdx + 1} من {totalPages}", fBold, brAmber, new RectangleF(30, y + 30, width / 2 - 50, 22), SfRtlRight);
                                }

                                y += 66;

                                // Table Header
                                int tLeft = 20;
                                int thH = 34;
                                g.FillRectangle(brAmber, tLeft, y, 840, thH);
                                g.DrawRectangle(penBorder, tLeft, y, 840, thH);

                                int curX = tLeft + 840;
                                for (int c = 0; c < colHeaders.Length; c++)
                                {
                                    curX -= colW[c];
                                    g.DrawString(colHeaders[c], fBold, Brushes.White, new RectangleF(curX, y + 4, colW[c], thH - 6), SfCenter);
                                    if (c > 0) g.DrawLine(Pens.White, curX, y, curX, y + thH);
                                }
                                y += thH;

                                // Rows
                                bool alt = false;
                                for (int r = 0; r < takeCount; r++)
                                {
                                    int actualRowIdx = startRow + r;
                                    if (actualRowIdx >= totalRowCount) break;

                                    var rowData = rows[actualRowIdx];
                                    int thisRowH = pageRowHeights[r];
                                    if (alt) g.FillRectangle(brAlt, tLeft, y, 840, thisRowH);
                                    g.DrawRectangle(penGrid, tLeft, y, 840, thisRowH);

                                    curX = tLeft + 840;
                                    for (int c = 0; c < rowData.Length; c++)
                                    {
                                        curX -= colW[c];
                                        var sf = (c == 3) ? SfRtlRight : SfCenter;
                                        Brush brush = Brushes.Black;
                                        Font font = fNorm;

                                        if (c == 4 && !string.IsNullOrEmpty(rowData[c])) { brush = brGreen; font = fBold; }
                                        else if (c == 5 && !string.IsNullOrEmpty(rowData[c])) { brush = brRed; font = fBold; }
                                        else if (c == 6) { brush = brAmber; font = fBold; }
                                        else if (c == 0) { font = fSmall; }

                                        float textY = (c == 3) ? (y + 5) : (y + (thisRowH - 20) / 2f);
                                        if (textY < y + 3) textY = y + 3;
                                        g.DrawString(rowData[c], font, brush, new RectangleF(curX + 3, textY, colW[c] - 6, thisRowH - 6), sf);
                                        if (c > 0) g.DrawLine(penGrid, curX, y, curX, y + thisRowH);
                                    }

                                    y += thisRowH;
                                    alt = !alt;
                                }

                                // Summary Box (Only on Last Page)
                                if (pageIdx == totalPages - 1)
                                {
                                    y += 12;
                                    g.FillRectangle(brAlt, tLeft, y, 840, 78);
                                    g.DrawRectangle(penBorder, tLeft, y, 840, 78);

                                    g.DrawString($"إجمالي المشتريات: {totalPurchases:N2} ج", fBold, brRed, new RectangleF(tLeft + 560, y + 8, 260, 26), SfRtlRight);
                                    g.DrawString($"إجمالي المسدد: {totalPayments:N2} ج", fBold, brGreen, new RectangleF(tLeft + 560, y + 42, 260, 26), SfRtlRight);

                                    string balTitle = balance > 0 ? "الرصيد المستحق للمورد:" : (balance < 0 ? "رصيد دائن لصالحنا:" : "الرصيد النهائي:");
                                    Brush balBr = balance > 0 ? brRed : brGreen;
                                    using (var fBalNum = new Font("Segoe UI", 15f, FontStyle.Bold))
                                    {
                                        g.DrawString(balTitle, fBold, balBr, new RectangleF(tLeft + 20, y + 8, 520, 24), SfRtlLeft);
                                        g.DrawString($"{Math.Abs(balance):N2} جنيه مصري", fBalNum, balBr, new RectangleF(tLeft + 20, y + 36, 520, 36), SfRtlLeft);
                                    }

                                    y += 88;
                                }

                                g.DrawString("مع تحيات إدارة الحسابات", fSmall, Brushes.Gray, new RectangleF(20, y + 4, 840, 20), SfCenter);
                            }
                        }
                        pages.Add(bmp);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateDetailedSupplierStatementImages", ex);
            }

            return pages;
        }

        /// <summary>
        /// توليد كارت صورة عالي الدقة لبيان الأسعار بنموذج الطارق المعتمد المماثل لفاتورة المبيعات
        /// </summary>
        public static Bitmap GeneratePriceQuoteReceiptImage(string clientName, string phone, string quoteCode, string tier, List<SaleItemDTO> items, decimal discount, string notes, string templateName = null)
        {
            try
            {
                int itemCount = items != null ? items.Count : 0;
                int width = 680;
                int rowH = 28;
                int headerH = 130;
                int metaH = 65;
                int tableHeaderH = 34;
                int itemsH = itemCount * rowH;
                int summaryH = 26 + 28 + 8 + 28 + (string.IsNullOrWhiteSpace(notes) ? 0 : 28) + 22 + 26 + 6 + 46;
                int totalH = headerH + metaH + tableHeaderH + itemsH + summaryH + 40;

                var bmp = new Bitmap(width, totalH);
                bmp.SetResolution(144f, 144f);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    // Al-Tarek Colors
                    Color cPrimary   = Color.FromArgb(24, 34, 53);
                    Color cSecondary = Color.FromArgb(37, 52, 78);
                    Color cAccent    = Color.FromArgb(245, 158, 11);
                    Color cAltBg     = Color.FromArgb(248, 250, 253);

                    using (var pThick = new Pen(cPrimary, 2.5f))
                    using (var pThin = new Pen(Color.FromArgb(203, 213, 225), 1f))
                    using (var pBlack = new Pen(Color.Black, 1f))
                    using (var pAccent = new Pen(cAccent, 1.5f))
                    using (var brPrimary = new SolidBrush(cPrimary))
                    using (var brSecondary = new SolidBrush(cSecondary))
                    using (var brAccent = new SolidBrush(cAccent))
                    using (var brAlt = new SolidBrush(cAltBg))
                    using (var brRed = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    using (var fBig = new Font("Arial", 16f, FontStyle.Bold))
                    using (var fSub = new Font("Arial", 10.5f, FontStyle.Bold))
                    using (var fBold = new Font("Arial", 9.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Arial", 9f, FontStyle.Regular))
                    using (var fSmall = new Font("Arial", 8.5f, FontStyle.Regular))
                    {
                        // Outer Frame (Al-Tarek double border)
                        g.DrawRectangle(pThick, 4, 4, width - 8, totalH - 8);
                        g.DrawRectangle(pAccent, 8, 8, width - 16, totalH - 16);

                        int y = 16;
                        string compName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة التجارية";
                        string compPhone = !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "";

                        // ── 1. HEADER SECTION (Al-Tarek) ──
                        int logoW = 0;
                        if (!string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                        {
                            try
                            {
                                using (var logoImg = Image.FromFile(AppConfig.ShopLogoPath))
                                {
                                    int maxLogoW = 140;
                                    int maxLogoH = 70;
                                    double ratioX = (double)maxLogoW / logoImg.Width;
                                    double ratioY = (double)maxLogoH / logoImg.Height;
                                    double ratio = Math.Min(ratioX, ratioY);
                                    int lw = (int)(logoImg.Width * ratio);
                                    int lh = (int)(logoImg.Height * ratio);
                                    g.DrawImage(logoImg, 20, y, lw, lh);
                                    logoW = lw + 15;
                                }
                            }
                            catch { }
                        }

                        float headerRightW = width - 40 - logoW;
                        g.DrawString(compName, fBig, brPrimary, new RectangleF(20 + logoW, y, headerRightW, 32), SfRtlRight);
                        y += 32;
                        if (!string.IsNullOrEmpty(compPhone))
                        {
                            string addrStr = !string.IsNullOrEmpty(AppConfig.CompanyAddress) ? $"  |  العنوان: {AppConfig.CompanyAddress}" : "";
                            g.DrawString($"موبايل: {compPhone}{addrStr}", fSmall, Brushes.DarkSlateGray, new RectangleF(20 + logoW, y, headerRightW, 20), SfRtlRight);
                        }
                        y += 22;
                        g.DrawString("بيان أسعار وعرض أسعار معتمد", fSub, brAccent, new RectangleF(15, y, width - 30, 22), SfCenter);
                        y += 24;
                        g.DrawLine(pThick, 20, y, width - 20, y);
                        y += 8;

                        // ── 2. METADATA CARD (Al-Tarek) ──
                        string cName = !string.IsNullOrWhiteSpace(clientName) ? clientName : "عميل نقدي";
                        string qCode = !string.IsNullOrEmpty(quoteCode) ? quoteCode : "---";
                        string dateStr = DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");
                        string userName = (!string.IsNullOrEmpty(Session.UserName) ? Session.UserName : (!string.IsNullOrEmpty(Session.EmpName) ? Session.EmpName : "المسؤول"));
                        string pTier = !string.IsNullOrEmpty(tier) ? tier : "قطاعي";

                        g.FillRectangle(brAlt, 20, y, width - 40, metaH);
                        g.DrawRectangle(pThin, 20, y, width - 40, metaH);
                        g.DrawLine(pThin, width / 2, y, width / 2, y + metaH);

                        g.DrawString($"العميل:  {cName}", fBold, Brushes.Black, new RectangleF(width / 2 + 10, y + 8, width / 2 - 25, 22), SfRtlRight);
                        g.DrawString($"رقم العرض:  #{qCode}", fBold, brPrimary, new RectangleF(width / 2 + 10, y + 34, width / 2 - 25, 22), SfRtlRight);

                        g.DrawString($"التاريخ:  {dateStr}", fNorm, Brushes.DarkSlateGray, new RectangleF(25, y + 8, width / 2 - 35, 22), SfRtlRight);
                        g.DrawString($"فئة السعر:  {pTier}  |  المستخدم: {userName}", fNorm, Brushes.Black, new RectangleF(25, y + 34, width / 2 - 35, 22), SfRtlRight);
                        y += metaH + 10;

                        // ── 3. ITEMS TABLE (Al-Tarek 8-Columns) ──
                        int tLeft = 20;
                        int tWidth = width - 40;
                        int wTot = 78, wDisc = 45, wPrice = 65, wQty = 48, wUnit = 45, wCode = 55, wIdx = 28;
                        int wName = tWidth - (wTot + wDisc + wPrice + wQty + wUnit + wCode + wIdx);

                        // Table Header
                        g.FillRectangle(brSecondary, tLeft, y, tWidth, tableHeaderH);
                        g.DrawRectangle(pBlack, tLeft, y, tWidth, tableHeaderH);

                        int curX = width - 20;
                        curX -= wIdx; g.DrawRectangle(pBlack, curX, y, wIdx, tableHeaderH); g.DrawString("م", fBold, Brushes.White, new RectangleF(curX, y + 6, wIdx, tableHeaderH - 6), SfCenter);
                        curX -= wCode; g.DrawRectangle(pBlack, curX, y, wCode, tableHeaderH); g.DrawString("الكود", fBold, Brushes.White, new RectangleF(curX, y + 6, wCode, tableHeaderH - 6), SfCenter);
                        curX -= wName; g.DrawRectangle(pBlack, curX, y, wName, tableHeaderH); g.DrawString("اسم الصنف والبيان", fBold, Brushes.White, new RectangleF(curX, y + 6, wName, tableHeaderH - 6), SfCenter);
                        curX -= wUnit; g.DrawRectangle(pBlack, curX, y, wUnit, tableHeaderH); g.DrawString("الوحدة", fBold, Brushes.White, new RectangleF(curX, y + 6, wUnit, tableHeaderH - 6), SfCenter);
                        curX -= wQty; g.DrawRectangle(pBlack, curX, y, wQty, tableHeaderH); g.DrawString("الكمية", fBold, Brushes.White, new RectangleF(curX, y + 6, wQty, tableHeaderH - 6), SfCenter);
                        curX -= wPrice; g.DrawRectangle(pBlack, curX, y, wPrice, tableHeaderH); g.DrawString("سعر البيع", fBold, Brushes.White, new RectangleF(curX, y + 6, wPrice, tableHeaderH - 6), SfCenter);
                        curX -= wDisc; g.DrawRectangle(pBlack, curX, y, wDisc, tableHeaderH); g.DrawString("الخصم", fBold, Brushes.White, new RectangleF(curX, y + 6, wDisc, tableHeaderH - 6), SfCenter);
                        curX -= wTot; g.DrawRectangle(pBlack, curX, y, wTot, tableHeaderH); g.DrawString("إجمالي البيع", fBold, Brushes.White, new RectangleF(curX, y + 6, wTot, tableHeaderH - 6), SfCenter);

                        y += tableHeaderH;

                        int itemIdx = 1;
                        decimal gross = 0m;
                        if (items != null)
                        {
                            foreach (var itm in items)
                            {
                                curX = width - 20;
                                decimal lineTot = (itm.Quantity * itm.UnitPrice) - itm.DiscountAmt;
                                gross += (itm.Quantity * itm.UnitPrice);

                                if (itemIdx % 2 == 0) g.FillRectangle(brAlt, tLeft, y, tWidth, rowH);
                                g.DrawRectangle(pBlack, tLeft, y, tWidth, rowH);

                                string rawCode = itm.ProductCode ?? "";
                                string pCode = ChickenDist.Forms.FrmPrintSale.FormatProductCode(rawCode);
                                string pName = itm.ProductName ?? "";
                                string uName = !string.IsNullOrWhiteSpace(itm.UnitName) ? itm.UnitName : "قطعة";
                                decimal qVal = itm.Quantity;
                                decimal prVal = itm.UnitPrice;
                                decimal dVal = itm.DiscountAmt;
                                decimal tVal = lineTot;

                                curX -= wIdx; g.DrawRectangle(pBlack, curX, y, wIdx, rowH); g.DrawString(itemIdx.ToString(), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wIdx, rowH - 4), SfCenter);
                                curX -= wCode; g.DrawRectangle(pBlack, curX, y, wCode, rowH); g.DrawString(pCode, fSmall, Brushes.Black, new RectangleF(curX, y + 4, wCode, rowH - 4), SfCenter);
                                curX -= wName; g.DrawRectangle(pBlack, curX, y, wName, rowH); g.DrawString(pName, fBold, Brushes.Black, new RectangleF(curX + 5, y + 4, wName - 10, rowH - 4), SfRtlRight);
                                curX -= wUnit; g.DrawRectangle(pBlack, curX, y, wUnit, rowH); g.DrawString(uName, fSmall, Brushes.Black, new RectangleF(curX, y + 4, wUnit, rowH - 4), SfCenter);
                                curX -= wQty; g.DrawRectangle(pBlack, curX, y, wQty, rowH); g.DrawString(qVal.ToString("0.##"), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wQty, rowH - 4), SfCenter);
                                curX -= wPrice; g.DrawRectangle(pBlack, curX, y, wPrice, rowH); g.DrawString(prVal.ToString("N2"), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wPrice, rowH - 4), SfCenter);
                                curX -= wDisc; g.DrawRectangle(pBlack, curX, y, wDisc, rowH); g.DrawString(dVal > 0 ? dVal.ToString("F1") : "-", fSmall, Brushes.Black, new RectangleF(curX, y + 4, wDisc, rowH - 4), SfCenter);
                                curX -= wTot; g.DrawRectangle(pBlack, curX, y, wTot, rowH); g.DrawString(tVal.ToString("N2"), fBold, Brushes.Black, new RectangleF(curX, y + 4, wTot, rowH - 4), SfCenter);

                                y += rowH;
                                itemIdx++;
                            }
                        }

                        // ── 4. FINANCIAL SUMMARY (Al-Tarek 3-Column Grid) ──
                        decimal net = Math.Max(0m, gross - discount);
                        y += 10;

                        int col3W = tWidth / 3;
                        string[] h3 = { "إجمالي البضاعة", "الخصم الممنوح", "الصافي المطلوب" };
                        string[] v3 = {
                            $"{gross:N2} ج",
                            discount > 0 ? $"-{discount:N2} ج" : "0.00 ج",
                            $"{net:N2} ج"
                        };

                        // Header row
                        g.FillRectangle(brSecondary, tLeft, y, tWidth, 26);
                        g.DrawRectangle(pThin, tLeft, y, tWidth, 26);
                        for (int c = 0; c < 3; c++)
                        {
                            int cx = tLeft + tWidth - (c + 1) * col3W;
                            if (c < 2) g.DrawLine(Pens.White, cx, y, cx, y + 26);
                            g.DrawString(h3[c], fSmall, Brushes.White, new RectangleF(cx, y + 4, col3W, 20), SfCenter);
                        }
                        y += 26;

                        // Values row
                        g.FillRectangle(brAlt, tLeft, y, tWidth, 28);
                        g.DrawRectangle(pThin, tLeft, y, tWidth, 28);
                        for (int c = 0; c < 3; c++)
                        {
                            int cx = tLeft + tWidth - (c + 1) * col3W;
                            if (c < 2) g.DrawLine(pThin, cx, y, cx, y + 28);
                            Brush vBr = (c == 2) ? brPrimary : (c == 1 && discount > 0) ? brRed : Brushes.Black;
                            Font vFont = (c == 2) ? fBold : fNorm;
                            g.DrawString(v3[c], vFont, vBr, new RectangleF(cx, y + 5, col3W, 22), SfCenter);
                        }
                        y += 34;

                        // Tafqeet Row
                        try
                        {
                            string taf = TafqeetHelper.ConvertToArabicWords(net);
                            g.FillRectangle(brAlt, tLeft, y, tWidth, 24);
                            g.DrawRectangle(pThin, tLeft, y, tWidth, 24);
                            g.DrawString($"فقط {taf} لا غير.", fSmall, Brushes.DarkSlateGray, new RectangleF(tLeft + 10, y + 3, tWidth - 20, 20), SfCenter);
                            y += 28;
                        }
                        catch { }

                        // Notes row if any
                        if (!string.IsNullOrWhiteSpace(notes))
                        {
                            g.FillRectangle(new SolidBrush(Color.FromArgb(254, 252, 232)), tLeft, y, tWidth, 24);
                            g.DrawRectangle(pThin, tLeft, y, tWidth, 24);
                            g.DrawString($"ملاحظات: {notes}", fSmall, Brushes.DarkSlateGray, new RectangleF(tLeft + 10, y + 3, tWidth - 20, 20), SfRtlRight);
                            y += 28;
                        }

                        // Disclaimer
                        g.DrawString("بيان تقديري استرشادي - الأسعار سارية حتى انتهاء الكميات أو تحديث الأسعار", fSmall, Brushes.Gray, new RectangleF(tLeft, y, tWidth, 20), SfCenter);
                        y += 22;

                        // Signatures
                        g.DrawString("توقيع المستلم: ...............................", fSmall, Brushes.Black, new RectangleF(30, y, 220, 20), SfRtlRight);
                        g.DrawString("توقيع المسؤول: ...............................", fSmall, Brushes.Black, new RectangleF(width - 250, y, 220, 20), SfRtlRight);
                        y += 26;

                        // Separator & Footer
                        g.DrawLine(pAccent, 20, y, width - 20, y);
                        y += 6;
                        g.DrawString("شكراً لتعاملكم معنا ونتشرف بخدمتكم دائماً", fSub, brPrimary, new RectangleF(0, y, width, 22), SfCenter);
                        using (var fPromo = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                        {
                            g.DrawString("تم إنشاء هذا المستند آلياً بواسطة Pro System لإدارة المبيعات والتوزيع", fPromo, Brushes.Gray, new RectangleF(0, y + 22, width, 18), SfCenter);
                        }
                    }
                }

                return bmp;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GeneratePriceQuoteReceiptImage", ex);
                return null;
            }
        }

        /// <summary>
        /// توليد صور بيان تسعير وعرض أسعار بضائع تفصيلي بنموذج الطارق المعتمد (صفحة واحدة أو متعدد الصفحات)
        /// </summary>
        public static List<Bitmap> GeneratePriceQuoteImages(string clientName, string phone, string quoteCode, string tier, List<SaleItemDTO> items, decimal discount, string notes, string templateName = null)
        {
            var pages = new List<Bitmap>();
            try
            {
                if (items == null || items.Count == 0) return pages;

                // إذا كان عدد الأصناف يناسب صفحة واحدة عادية (حتى 25 صنف)، نستخدم كارت الطارق الكامل
                if (items.Count <= 25)
                {
                    var singleCard = GeneratePriceQuoteReceiptImage(clientName, phone, quoteCode, tier, items, discount, notes, templateName);
                    if (singleCard != null) pages.Add(singleCard);
                    return pages;
                }

                // متعدد الصفحات بنموذج الطارق
                int totalRowCount = items.Count;
                int rowsPerPage = 25;
                int totalPages = (int)Math.Ceiling((double)totalRowCount / rowsPerPage);
                int width = 680;
                int rowH = 28;
                int headerH = 130;
                int metaH = 65;
                int tableHeaderH = 34;

                decimal gross = 0m;
                foreach (var it in items) gross += (it.Quantity * it.UnitPrice) - it.DiscountAmt;
                decimal net = Math.Max(0m, gross - discount);

                Color cPrimary   = Color.FromArgb(24, 34, 53);
                Color cSecondary = Color.FromArgb(37, 52, 78);
                Color cAccent    = Color.FromArgb(245, 158, 11);
                Color cAltBg     = Color.FromArgb(248, 250, 253);

                for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
                {
                    int startRow = pageIdx * rowsPerPage;
                    int takeCount = Math.Min(rowsPerPage, totalRowCount - startRow);
                    if (takeCount < 0) takeCount = 0;

                    bool isLastPage = (pageIdx == totalPages - 1);
                    int summaryH = isLastPage ? (26 + 28 + 8 + 28 + (string.IsNullOrWhiteSpace(notes) ? 0 : 28) + 22 + 26 + 6 + 46) : 50;
                    int dynamicHeight = headerH + metaH + tableHeaderH + (takeCount * rowH) + summaryH + 40;

                    var bmp = new Bitmap(width, dynamicHeight);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                        g.Clear(Color.White);

                        using (var pThick = new Pen(cPrimary, 2.5f))
                        using (var pThin = new Pen(Color.FromArgb(203, 213, 225), 1f))
                        using (var pBlack = new Pen(Color.Black, 1f))
                        using (var pAccent = new Pen(cAccent, 1.5f))
                        using (var brPrimary = new SolidBrush(cPrimary))
                        using (var brSecondary = new SolidBrush(cSecondary))
                        using (var brAccent = new SolidBrush(cAccent))
                        using (var brAlt = new SolidBrush(cAltBg))
                        using (var brRed = new SolidBrush(Color.FromArgb(220, 38, 38)))
                        using (var fBig = new Font("Arial", 16f, FontStyle.Bold))
                        using (var fSub = new Font("Arial", 10.5f, FontStyle.Bold))
                        using (var fBold = new Font("Arial", 9.5f, FontStyle.Bold))
                        using (var fNorm = new Font("Arial", 9f, FontStyle.Regular))
                        using (var fSmall = new Font("Arial", 8.5f, FontStyle.Regular))
                        {
                            g.DrawRectangle(pThick, 4, 4, width - 8, dynamicHeight - 8);
                            g.DrawRectangle(pAccent, 8, 8, width - 16, dynamicHeight - 16);

                            int y = 16;
                            string compName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة التجارية";
                            string compPhone = !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "";

                            // Header
                            int logoW = 0;
                            if (!string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                            {
                                try
                                {
                                    using (var logoImg = Image.FromFile(AppConfig.ShopLogoPath))
                                    {
                                        int maxLogoW = 140;
                                        int maxLogoH = 70;
                                        double ratioX = (double)maxLogoW / logoImg.Width;
                                        double ratioY = (double)maxLogoH / logoImg.Height;
                                        double ratio = Math.Min(ratioX, ratioY);
                                        int lw = (int)(logoImg.Width * ratio);
                                        int lh = (int)(logoImg.Height * ratio);
                                        g.DrawImage(logoImg, 20, y, lw, lh);
                                        logoW = lw + 15;
                                    }
                                }
                                catch { }
                            }

                            float headerRightW = width - 40 - logoW;
                            g.DrawString(compName, fBig, brPrimary, new RectangleF(20 + logoW, y, headerRightW, 32), SfRtlRight);
                            y += 32;
                            if (!string.IsNullOrEmpty(compPhone))
                            {
                                string addrStr = !string.IsNullOrEmpty(AppConfig.CompanyAddress) ? $"  |  العنوان: {AppConfig.CompanyAddress}" : "";
                                g.DrawString($"موبايل: {compPhone}{addrStr}", fSmall, Brushes.DarkSlateGray, new RectangleF(20 + logoW, y, headerRightW, 20), SfRtlRight);
                            }
                            y += 22;
                            g.DrawString($"بيان أسعار وعرض أسعار معتمد (صفحة {pageIdx + 1} من {totalPages})", fSub, brAccent, new RectangleF(15, y, width - 30, 22), SfCenter);
                            y += 24;
                            g.DrawLine(pThick, 20, y, width - 20, y);
                            y += 8;

                            // Metadata Card
                            string cName = !string.IsNullOrWhiteSpace(clientName) ? clientName : "عميل نقدي";
                            string qCode = !string.IsNullOrEmpty(quoteCode) ? quoteCode : "---";
                            string dateStr = DateTime.Now.ToString("yyyy/MM/dd hh:mm tt");
                            string userName = (!string.IsNullOrEmpty(Session.UserName) ? Session.UserName : (!string.IsNullOrEmpty(Session.EmpName) ? Session.EmpName : "المسؤول"));
                            string pTier = !string.IsNullOrEmpty(tier) ? tier : "قطاعي";

                            g.FillRectangle(brAlt, 20, y, width - 40, metaH);
                            g.DrawRectangle(pThin, 20, y, width - 40, metaH);
                            g.DrawLine(pThin, width / 2, y, width / 2, y + metaH);

                            g.DrawString($"العميل:  {cName}", fBold, Brushes.Black, new RectangleF(width / 2 + 10, y + 8, width / 2 - 25, 22), SfRtlRight);
                            g.DrawString($"رقم العرض:  #{qCode}", fBold, brPrimary, new RectangleF(width / 2 + 10, y + 34, width / 2 - 25, 22), SfRtlRight);

                            g.DrawString($"التاريخ:  {dateStr}", fNorm, Brushes.DarkSlateGray, new RectangleF(25, y + 8, width / 2 - 35, 22), SfRtlRight);
                            g.DrawString($"فئة السعر:  {pTier}  |  المستخدم: {userName}", fNorm, Brushes.Black, new RectangleF(25, y + 34, width / 2 - 35, 22), SfRtlRight);
                            y += metaH + 10;

                            // Table Header
                            int tLeft = 20;
                            int tWidth = width - 40;
                            int wTot = 78, wDisc = 45, wPrice = 65, wQty = 48, wUnit = 45, wCode = 55, wIdx = 28;
                            int wName = tWidth - (wTot + wDisc + wPrice + wQty + wUnit + wCode + wIdx);

                            g.FillRectangle(brSecondary, tLeft, y, tWidth, tableHeaderH);
                            g.DrawRectangle(pBlack, tLeft, y, tWidth, tableHeaderH);

                            int curX = width - 20;
                            curX -= wIdx; g.DrawRectangle(pBlack, curX, y, wIdx, tableHeaderH); g.DrawString("م", fBold, Brushes.White, new RectangleF(curX, y + 6, wIdx, tableHeaderH - 6), SfCenter);
                            curX -= wCode; g.DrawRectangle(pBlack, curX, y, wCode, tableHeaderH); g.DrawString("الكود", fBold, Brushes.White, new RectangleF(curX, y + 6, wCode, tableHeaderH - 6), SfCenter);
                            curX -= wName; g.DrawRectangle(pBlack, curX, y, wName, tableHeaderH); g.DrawString("اسم الصنف والبيان", fBold, Brushes.White, new RectangleF(curX, y + 6, wName, tableHeaderH - 6), SfCenter);
                            curX -= wUnit; g.DrawRectangle(pBlack, curX, y, wUnit, tableHeaderH); g.DrawString("الوحدة", fBold, Brushes.White, new RectangleF(curX, y + 6, wUnit, tableHeaderH - 6), SfCenter);
                            curX -= wQty; g.DrawRectangle(pBlack, curX, y, wQty, tableHeaderH); g.DrawString("الكمية", fBold, Brushes.White, new RectangleF(curX, y + 6, wQty, tableHeaderH - 6), SfCenter);
                            curX -= wPrice; g.DrawRectangle(pBlack, curX, y, wPrice, tableHeaderH); g.DrawString("سعر البيع", fBold, Brushes.White, new RectangleF(curX, y + 6, wPrice, tableHeaderH - 6), SfCenter);
                            curX -= wDisc; g.DrawRectangle(pBlack, curX, y, wDisc, tableHeaderH); g.DrawString("الخصم", fBold, Brushes.White, new RectangleF(curX, y + 6, wDisc, tableHeaderH - 6), SfCenter);
                            curX -= wTot; g.DrawRectangle(pBlack, curX, y, wTot, tableHeaderH); g.DrawString("إجمالي البيع", fBold, Brushes.White, new RectangleF(curX, y + 6, wTot, tableHeaderH - 6), SfCenter);

                            y += tableHeaderH;

                            for (int r = 0; r < takeCount; r++)
                            {
                                int actualRowIdx = startRow + r;
                                if (actualRowIdx >= totalRowCount) break;

                                var itm = items[actualRowIdx];
                                curX = width - 20;
                                decimal lineTot = (itm.Quantity * itm.UnitPrice) - itm.DiscountAmt;

                                if ((r + 1) % 2 == 0) g.FillRectangle(brAlt, tLeft, y, tWidth, rowH);
                                g.DrawRectangle(pBlack, tLeft, y, tWidth, rowH);

                                string rawCode = itm.ProductCode ?? "";
                                string pCode = ChickenDist.Forms.FrmPrintSale.FormatProductCode(rawCode);
                                string pName = itm.ProductName ?? "";
                                string uName = !string.IsNullOrWhiteSpace(itm.UnitName) ? itm.UnitName : "قطعة";
                                decimal qVal = itm.Quantity;
                                decimal prVal = itm.UnitPrice;
                                decimal dVal = itm.DiscountAmt;
                                decimal tVal = lineTot;

                                curX -= wIdx; g.DrawRectangle(pBlack, curX, y, wIdx, rowH); g.DrawString((actualRowIdx + 1).ToString(), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wIdx, rowH - 4), SfCenter);
                                curX -= wCode; g.DrawRectangle(pBlack, curX, y, wCode, rowH); g.DrawString(pCode, fSmall, Brushes.Black, new RectangleF(curX, y + 4, wCode, rowH - 4), SfCenter);
                                curX -= wName; g.DrawRectangle(pBlack, curX, y, wName, rowH); g.DrawString(pName, fBold, Brushes.Black, new RectangleF(curX + 5, y + 4, wName - 10, rowH - 4), SfRtlRight);
                                curX -= wUnit; g.DrawRectangle(pBlack, curX, y, wUnit, rowH); g.DrawString(uName, fSmall, Brushes.Black, new RectangleF(curX, y + 4, wUnit, rowH - 4), SfCenter);
                                curX -= wQty; g.DrawRectangle(pBlack, curX, y, wQty, rowH); g.DrawString(qVal.ToString("0.##"), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wQty, rowH - 4), SfCenter);
                                curX -= wPrice; g.DrawRectangle(pBlack, curX, y, wPrice, rowH); g.DrawString(prVal.ToString("N2"), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wPrice, rowH - 4), SfCenter);
                                curX -= wDisc; g.DrawRectangle(pBlack, curX, y, wDisc, rowH); g.DrawString(dVal > 0 ? dVal.ToString("F1") : "-", fSmall, Brushes.Black, new RectangleF(curX, y + 4, wDisc, rowH - 4), SfCenter);
                                curX -= wTot; g.DrawRectangle(pBlack, curX, y, wTot, rowH); g.DrawString(tVal.ToString("N2"), fBold, Brushes.Black, new RectangleF(curX, y + 4, wTot, rowH - 4), SfCenter);

                                y += rowH;
                            }

                            if (isLastPage)
                            {
                                y += 10;
                                int col3W = tWidth / 3;
                                string[] h3 = { "إجمالي البضاعة", "الخصم الممنوح", "الصافي المطلوب" };
                                string[] v3 = {
                                    $"{gross:N2} ج",
                                    discount > 0 ? $"-{discount:N2} ج" : "0.00 ج",
                                    $"{net:N2} ج"
                                };

                                g.FillRectangle(brSecondary, tLeft, y, tWidth, 26);
                                g.DrawRectangle(pThin, tLeft, y, tWidth, 26);
                                for (int c = 0; c < 3; c++)
                                {
                                    int cx = tLeft + tWidth - (c + 1) * col3W;
                                    if (c < 2) g.DrawLine(Pens.White, cx, y, cx, y + 26);
                                    g.DrawString(h3[c], fSmall, Brushes.White, new RectangleF(cx, y + 4, col3W, 20), SfCenter);
                                }
                                y += 26;

                                g.FillRectangle(brAlt, tLeft, y, tWidth, 28);
                                g.DrawRectangle(pThin, tLeft, y, tWidth, 28);
                                for (int c = 0; c < 3; c++)
                                {
                                    int cx = tLeft + tWidth - (c + 1) * col3W;
                                    if (c < 2) g.DrawLine(pThin, cx, y, cx, y + 28);
                                    Brush vBr = (c == 2) ? brPrimary : (c == 1 && discount > 0) ? brRed : Brushes.Black;
                                    Font vFont = (c == 2) ? fBold : fNorm;
                                    g.DrawString(v3[c], vFont, vBr, new RectangleF(cx, y + 5, col3W, 22), SfCenter);
                                }
                                y += 34;

                                try
                                {
                                    string taf = TafqeetHelper.ConvertToArabicWords(net);
                                    g.FillRectangle(brAlt, tLeft, y, tWidth, 24);
                                    g.DrawRectangle(pThin, tLeft, y, tWidth, 24);
                                    g.DrawString($"فقط {taf} لا غير.", fSmall, Brushes.DarkSlateGray, new RectangleF(tLeft + 10, y + 3, tWidth - 20, 20), SfCenter);
                                    y += 28;
                                }
                                catch { }

                                if (!string.IsNullOrWhiteSpace(notes))
                                {
                                    g.FillRectangle(new SolidBrush(Color.FromArgb(254, 252, 232)), tLeft, y, tWidth, 24);
                                    g.DrawRectangle(pThin, tLeft, y, tWidth, 24);
                                    g.DrawString($"ملاحظات: {notes}", fSmall, Brushes.DarkSlateGray, new RectangleF(tLeft + 10, y + 3, tWidth - 20, 20), SfRtlRight);
                                    y += 28;
                                }

                                g.DrawString("بيان تقديري استرشادي - الأسعار سارية حتى انتهاء الكميات أو تحديث الأسعار", fSmall, Brushes.Gray, new RectangleF(tLeft, y, tWidth, 20), SfCenter);
                                y += 22;

                                g.DrawString("توقيع المستلم: ...............................", fSmall, Brushes.Black, new RectangleF(30, y, 220, 20), SfRtlRight);
                                g.DrawString("توقيع المسؤول: ...............................", fSmall, Brushes.Black, new RectangleF(width - 250, y, 220, 20), SfRtlRight);
                                y += 26;

                                g.DrawLine(pAccent, 20, y, width - 20, y);
                                y += 6;
                                g.DrawString("شكراً لتعاملكم معنا ونتشرف بخدمتكم دائماً", fSub, brPrimary, new RectangleF(0, y, width, 22), SfCenter);
                                using (var fPromo = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                                {
                                    g.DrawString("تم إنشاء هذا المستند آلياً بواسطة Pro System لإدارة المبيعات والتوزيع", fPromo, Brushes.Gray, new RectangleF(0, y + 22, width, 18), SfCenter);
                                }
                            }
                            else
                            {
                                y += 14;
                                g.DrawString("يتبع باقي بنود عرض الأسعار في الصفحة التالية", fBold, brPrimary, new RectangleF(20, y, width - 40, 24), SfCenter);
                            }
                        }
                    }

                    pages.Add(bmp);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GeneratePriceQuoteImages", ex);
            }

            return pages;
        }

        /// <summary>
        /// توليد كارت صورة عالي الدقة لإيصال مرتجع المبيعات بنموذج الطارق المعتمد
        /// </summary>
        public static Bitmap GenerateReturnReceiptImage(int returnID, string templateName = null)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT sr.ReturnID, sr.ReturnDate, sr.TotalAmount, sr.Notes, sr.ReturnType,
                           ISNULL(sr.PaymentType, N'Cash') AS PaymentType,
                           ISNULL(s.SaleCode, N'مرتجع عام') AS SaleCode,
                           s.SaleDate AS OriginalSaleDate,
                           ISNULL(c.ClientName, N'عميل نقدي / عام') AS ClientName,
                           ISNULL(c.Phone, N'') AS ClientPhone,
                           ISNULL(c.Address, N'') AS ClientAddress,
                           ISNULL(e.EmpName, N'كاشير') AS CashierName,
                           ISNULL(w.WarehouseName, N'المخزن الرئيسي') AS WarehouseName
                    FROM SalesReturns sr
                    LEFT JOIN Sales s ON sr.SaleID = s.SaleID
                    LEFT JOIN Clients c ON sr.ClientID = c.ClientID
                    LEFT JOIN Employees e ON sr.CreatedBy = e.EmpID
                    LEFT JOIN Warehouses w ON sr.WarehouseID = w.WarehouseID
                    WHERE sr.ReturnID = @id", DbHelper.P("@id", returnID));

                if (dt.Rows.Count == 0) return null;
                var retRow = dt.Rows[0];

                var dtItems = DbHelper.Query(@"
                    SELECT ISNULL(p.ProductName, N'صنف عام') AS ProductName, 
                           ISNULL(p.ProductCode, N'') AS ProductCode,
                           ISNULL(ri.UnitName, ISNULL(p.Unit, N'')) AS UnitName,
                           ri.Quantity, ri.UnitPrice, 
                           ISNULL(ri.TotalPrice, ri.Quantity * ri.UnitPrice) AS TotalPrice,
                           ri.ProductID,
                           ISNULL(ri.IMEI, N'') AS IMEI
                    FROM ReturnItems ri
                    LEFT JOIN Products p ON ri.ProductID = p.ProductID
                    WHERE ri.ReturnID = @id", DbHelper.P("@id", returnID));

                int itemCount = dtItems.Rows.Count;
                int width = 680;
                int rowH = 28;
                int headerH = 130;
                int metaH = 88;
                int tableHeaderH = 34;
                int itemsH = Math.Max(itemCount, 1) * rowH;
                string notes = retRow["Notes"]?.ToString() ?? "";
                int summaryH = 32 + 28 + (string.IsNullOrWhiteSpace(notes) ? 0 : 28) + 24 + 48;
                int totalH = headerH + metaH + tableHeaderH + itemsH + summaryH + 40;

                var bmp = new Bitmap(width, totalH);
                bmp.SetResolution(144f, 144f);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    // Al-Tarek Colors
                    Color cPrimary   = Color.FromArgb(24, 34, 53);
                    Color cSecondary = Color.FromArgb(37, 52, 78);
                    Color cAccent    = Color.FromArgb(220, 38, 38);
                    Color cAltBg     = Color.FromArgb(248, 250, 253);

                    using (var pThick = new Pen(cPrimary, 2.5f))
                    using (var pThin = new Pen(Color.FromArgb(203, 213, 225), 1f))
                    using (var pBlack = new Pen(Color.Black, 1f))
                    using (var pAccent = new Pen(cAccent, 1.5f))
                    using (var brPrimary = new SolidBrush(cPrimary))
                    using (var brSecondary = new SolidBrush(cSecondary))
                    using (var brAccent = new SolidBrush(cAccent))
                    using (var brAlt = new SolidBrush(cAltBg))
                    using (var fBig = new Font("Arial", 16f, FontStyle.Bold))
                    using (var fSub = new Font("Arial", 11f, FontStyle.Bold))
                    using (var fBold = new Font("Arial", 9.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Arial", 9f, FontStyle.Regular))
                    using (var fSmall = new Font("Arial", 8.5f, FontStyle.Regular))
                    {
                        // Outer Frame (Al-Tarek double border)
                        g.DrawRectangle(pThick, 4, 4, width - 8, totalH - 8);
                        g.DrawRectangle(pAccent, 8, 8, width - 16, totalH - 16);

                        int y = 16;
                        string compName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة التجارية";
                        string compPhone = !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "";

                        // ── 1. HEADER SECTION ──
                        int logoW = 0;
                        if (!string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                        {
                            try
                            {
                                using (var logoImg = Image.FromFile(AppConfig.ShopLogoPath))
                                {
                                    int maxLogoW = 140;
                                    int maxLogoH = 70;
                                    double ratio = Math.Min((double)maxLogoW / logoImg.Width, (double)maxLogoH / logoImg.Height);
                                    int lw = (int)(logoImg.Width * ratio);
                                    int lh = (int)(logoImg.Height * ratio);
                                    g.DrawImage(logoImg, 20, y, lw, lh);
                                    logoW = lw + 15;
                                }
                            }
                            catch { }
                        }

                        float headerRightW = width - 40 - logoW;
                        g.DrawString(compName, fBig, brPrimary, new RectangleF(20 + logoW, y, headerRightW, 32), SfRtlRight);
                        y += 32;
                        if (!string.IsNullOrEmpty(compPhone))
                        {
                            string addrStr = !string.IsNullOrEmpty(AppConfig.CompanyAddress) ? $"  |  العنوان: {AppConfig.CompanyAddress}" : "";
                            g.DrawString($"هاتف: {compPhone}{addrStr}", fSmall, Brushes.DarkSlateGray, new RectangleF(20 + logoW, y, headerRightW, 20), SfRtlRight);
                        }
                        y += 22;
                        g.DrawString("إيصال مرتجع مبيعات معتمد", fSub, brAccent, new RectangleF(15, y, width - 30, 24), SfCenter);
                        y += 26;
                        g.DrawLine(pThick, 20, y, width - 20, y);
                        y += 8;

                        // ── 2. METADATA CARD (Al-Tarek) ──
                        string returnCode = "RET-" + returnID;
                        DateTime returnDate = Convert.ToDateTime(retRow["ReturnDate"]);
                        string saleCode = retRow["SaleCode"]?.ToString() ?? "مرتجع عام";
                        string clientName = retRow["ClientName"]?.ToString() ?? "عميل نقدي / عام";
                        string clientPhone = retRow["ClientPhone"]?.ToString() ?? "";
                        string payType = retRow["PaymentType"]?.ToString() ?? "Cash";
                        string cashier = retRow["CashierName"]?.ToString() ?? "كاشير";
                        string formattedPayType = FormatReturnPaymentType(payType);

                        g.FillRectangle(brAlt, 20, y, width - 40, metaH);
                        g.DrawRectangle(pThin, 20, y, width - 40, metaH);
                        g.DrawLine(pThin, width / 2, y, width / 2, y + metaH);

                        string cDisplay = !string.IsNullOrWhiteSpace(clientPhone) ? $"{clientName} ({clientPhone})" : clientName;
                        g.DrawString($"العميل:  {cDisplay}", fBold, Brushes.Black, new RectangleF(width / 2 + 10, y + 8, width / 2 - 25, 22), SfRtlRight);
                        g.DrawString($"رقم المرتجع:  #{returnCode}", fBold, brAccent, new RectangleF(width / 2 + 10, y + 32, width / 2 - 25, 22), SfRtlRight);
                        g.DrawString($"الفاتورة الأصلية:  {saleCode}", fNorm, Brushes.Black, new RectangleF(width / 2 + 10, y + 56, width / 2 - 25, 22), SfRtlRight);

                        g.DrawString($"التاريخ:  {returnDate:yyyy/MM/dd hh:mm tt}", fNorm, Brushes.DarkSlateGray, new RectangleF(25, y + 8, width / 2 - 35, 22), SfRtlRight);
                        g.DrawString($"طريقة رد القيمة:  {formattedPayType}", fBold, brPrimary, new RectangleF(25, y + 32, width / 2 - 35, 22), SfRtlRight);
                        g.DrawString($"الكاشير:  {cashier}", fNorm, Brushes.DarkSlateGray, new RectangleF(25, y + 56, width / 2 - 35, 22), SfRtlRight);

                        y += metaH + 10;

                        // ── 3. ITEMS TABLE (Al-Tarek Style) ──
                        int tLeft = 20;
                        int tWidth = width - 40;
                        int wTot = 95, wPrice = 75, wQty = 65, wUnit = 55, wCode = 70, wIdx = 30;
                        int wName = tWidth - (wTot + wPrice + wQty + wUnit + wCode + wIdx);

                        // Header
                        g.FillRectangle(brSecondary, tLeft, y, tWidth, tableHeaderH);
                        g.DrawRectangle(pBlack, tLeft, y, tWidth, tableHeaderH);

                        int curX = width - 20;
                        curX -= wIdx; g.DrawRectangle(pBlack, curX, y, wIdx, tableHeaderH); g.DrawString("م", fBold, Brushes.White, new RectangleF(curX, y + 6, wIdx, tableHeaderH - 6), SfCenter);
                        curX -= wCode; g.DrawRectangle(pBlack, curX, y, wCode, tableHeaderH); g.DrawString("كود الصنف", fBold, Brushes.White, new RectangleF(curX, y + 6, wCode, tableHeaderH - 6), SfCenter);
                        curX -= wName; g.DrawRectangle(pBlack, curX, y, wName, tableHeaderH); g.DrawString("اسم الصنف المرتجع", fBold, Brushes.White, new RectangleF(curX, y + 6, wName, tableHeaderH - 6), SfCenter);
                        curX -= wUnit; g.DrawRectangle(pBlack, curX, y, wUnit, tableHeaderH); g.DrawString("الوحدة", fBold, Brushes.White, new RectangleF(curX, y + 6, wUnit, tableHeaderH - 6), SfCenter);
                        curX -= wQty; g.DrawRectangle(pBlack, curX, y, wQty, tableHeaderH); g.DrawString("الكمية المرتجعة", fBold, Brushes.White, new RectangleF(curX, y + 6, wQty, tableHeaderH - 6), SfCenter);
                        curX -= wPrice; g.DrawRectangle(pBlack, curX, y, wPrice, tableHeaderH); g.DrawString("سعر الوحدة", fBold, Brushes.White, new RectangleF(curX, y + 6, wPrice, tableHeaderH - 6), SfCenter);
                        curX -= wTot; g.DrawRectangle(pBlack, curX, y, wTot, tableHeaderH); g.DrawString("إجمالي المرتجع", fBold, Brushes.White, new RectangleF(curX, y + 6, wTot, tableHeaderH - 6), SfCenter);

                        y += tableHeaderH;

                        int itemIdx = 1;
                        decimal totalQty = 0m;
                        decimal calcTotal = 0m;

                        if (itemCount > 0)
                        {
                            foreach (DataRow ir in dtItems.Rows)
                            {
                                string pName = ir["ProductName"]?.ToString() ?? "صنف";
                                string imei = ir.Table.Columns.Contains("IMEI") && ir["IMEI"] != DBNull.Value ? ir["IMEI"].ToString().Trim() : "";
                                if (!string.IsNullOrEmpty(imei))
                                {
                                    pName = $"{pName} ({imei})";
                                }
                                string pCode = ir["ProductCode"]?.ToString() ?? "";
                                string pUnit = ir["UnitName"]?.ToString() ?? "";
                                decimal q = ir["Quantity"] != DBNull.Value ? Convert.ToDecimal(ir["Quantity"]) : 0m;
                                decimal p = ir["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(ir["UnitPrice"]) : 0m;
                                decimal t = ir["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(ir["TotalPrice"]) : (q * p);

                                totalQty += q;
                                calcTotal += t;

                                curX = width - 20;
                                if (itemIdx % 2 == 0) g.FillRectangle(brAlt, tLeft, y, tWidth, rowH);

                                curX -= wIdx; g.DrawRectangle(pThin, curX, y, wIdx, rowH); g.DrawString(itemIdx.ToString(), fSmall, Brushes.Black, new RectangleF(curX, y + 4, wIdx, rowH - 4), SfCenter);
                                curX -= wCode; g.DrawRectangle(pThin, curX, y, wCode, rowH); g.DrawString(pCode, fSmall, Brushes.DarkSlateGray, new RectangleF(curX, y + 4, wCode, rowH - 4), SfCenter);
                                curX -= wName; g.DrawRectangle(pThin, curX, y, wName, rowH); g.DrawString(pName, fBold, Brushes.Black, new RectangleF(curX + 6, y + 4, wName - 12, rowH - 4), SfRtlRight);
                                curX -= wUnit; g.DrawRectangle(pThin, curX, y, wUnit, rowH); g.DrawString(pUnit, fSmall, Brushes.Black, new RectangleF(curX, y + 4, wUnit, rowH - 4), SfCenter);
                                curX -= wQty; g.DrawRectangle(pThin, curX, y, wQty, rowH); g.DrawString(q.ToString("0.##"), fBold, Brushes.DarkBlue, new RectangleF(curX, y + 4, wQty, rowH - 4), SfCenter);
                                curX -= wPrice; g.DrawRectangle(pThin, curX, y, wPrice, rowH); g.DrawString(p.ToString("N2"), fNorm, Brushes.Black, new RectangleF(curX, y + 4, wPrice, rowH - 4), SfCenter);
                                curX -= wTot; g.DrawRectangle(pThin, curX, y, wTot, rowH); g.DrawString(t.ToString("N2"), fBold, brAccent, new RectangleF(curX, y + 4, wTot, rowH - 4), SfCenter);

                                y += rowH;
                                itemIdx++;
                            }
                        }

                        // ── 4. TOTALS & SUMMARY SECTION ──
                        decimal returnTotalAmount = Convert.ToDecimal(retRow["TotalAmount"]);
                        if (returnTotalAmount <= 0 && calcTotal > 0) returnTotalAmount = calcTotal;

                        y += 8;
                        int totalBoxH = 34;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(254, 242, 242)), tLeft, y, tWidth, totalBoxH);
                        g.DrawRectangle(new Pen(cAccent, 1.8f), tLeft, y, tWidth, totalBoxH);
                        g.DrawString($"إجمالي قيمة المرتجع المسترد:  {returnTotalAmount:N2} جنيه", fBig, brAccent, new RectangleF(tLeft + 10, y + 2, tWidth - 20, totalBoxH - 4), SfRtlRight);

                        y += totalBoxH + 6;
                        g.DrawString($"عدد الأصناف المرتجعة: {itemCount} صنف  |  إجمالي كميات المرتجع: {totalQty:0.##}", fBold, Brushes.DarkSlateGray, new RectangleF(tLeft, y, tWidth, 22), SfCenter);

                        string tafqeet = TafqeetHelper.ConvertToArabicWords(returnTotalAmount);
                        y += 24;
                        g.DrawString($"فقط وقدره: {tafqeet} لا غير", fNorm, Brushes.DimGray, new RectangleF(tLeft, y, tWidth, 22), SfCenter);

                        if (!string.IsNullOrWhiteSpace(notes))
                        {
                            y += 26;
                            g.FillRectangle(brAlt, tLeft, y, tWidth, 26);
                            g.DrawRectangle(pThin, tLeft, y, tWidth, 26);
                            g.DrawString($"ملاحظات: {notes}", fNorm, Brushes.DarkSlateGray, new RectangleF(tLeft + 8, y + 3, tWidth - 16, 20), SfRtlRight);
                        }

                        // ── 5. SIGNATURES & FOOTER ──
                        y += 30;
                        g.DrawLine(pThin, 20, y, width - 20, y);
                        y += 8;

                        g.DrawString("توقيع العميل / المستلم: ....................", fNorm, Brushes.Black, new RectangleF(width / 2 + 10, y, width / 2 - 30, 22), SfRtlRight);
                        g.DrawString("توقيع الكاشير / المسؤول: ....................", fNorm, Brushes.Black, new RectangleF(25, y, width / 2 - 30, 22), SfRtlRight);

                        y += 24;
                        g.DrawString("شكراً لتعاملكم معنا", fBold, brPrimary, new RectangleF(20, y, width - 40, 22), SfCenter);
                    }

                    return bmp;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateReturnReceiptImage", ex);
                return null;
            }
        }

        private static string FormatReturnPaymentType(string payType)
        {
            if (string.IsNullOrEmpty(payType)) return "نقدي (كاش - من الدرج)";
            switch (payType.Trim())
            {
                case "Cash": return "نقدي (كاش - من الدرج)";
                case "Visa": return "فيزا (إلكتروني / بنك)";
                case "Credit": return "آجل (خصم من رصيد العميل)";
                case "Mixed": return "مختلط (كاش + فيزا)";
                default: return payType;
            }
        }

        /// <summary>
        /// تصميم كارت صورة احترافي عالي الدقة لفاتورة المشتريات متضمنة الأصناف والمورد ومجاميع الفاتورة
        /// </summary>
        public static Bitmap GeneratePurchaseReceiptImage(int purchaseID, string templateName = null)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT p.PurchaseID, p.PurchaseCode, p.PurchaseDate, p.PurchaseType, p.SupplierID, p.TotalAmount, p.Notes,
                           ISNULL(p.PaidAmount, 0) + ISNULL(p.VisaPaid, 0) AS PaidAmount,
                           CASE 
                               WHEN p.PurchaseType = 'Cash' THEN 0 
                               ELSE ISNULL(p.TotalAmount, 0) - (ISNULL(p.PaidAmount, 0) + ISNULL(p.VisaPaid, 0)) 
                           END AS RemainingAmount,
                           COALESCE(p.DiscountAmount, 0) AS DiscountAmount,
                           COALESCE(p.DiscountPct, 0) AS DiscountPct,
                           COALESCE(p.TaxAmount, 0) AS TaxAmount,
                           COALESCE(p.TaxPct, 0) AS TaxPct,
                           COALESCE(s.SupplierName, N'مورد عام') AS SupplierName,
                           ISNULL(s.Phone, N'') AS SupplierPhone,
                           COALESCE(w.WarehouseName, N'المخزن الرئيسي') AS WarehouseName,
                           COALESCE(sa.AccountName, N'الخزينة الرئيسية') AS SafeName
                    FROM Purchases p WITH (NOLOCK)
                    LEFT JOIN Suppliers s WITH (NOLOCK) ON p.SupplierID = s.SupplierID
                    LEFT JOIN Warehouses w WITH (NOLOCK) ON p.WarehouseID = w.WarehouseID
                    LEFT JOIN SafeAccounts sa WITH (NOLOCK) ON p.SafeAccountID = sa.AccountID
                    WHERE p.PurchaseID = @id", DbHelper.P("@id", purchaseID));

                if (dt == null || dt.Rows.Count == 0) return null;
                var purRow = dt.Rows[0];

                var dtItems = PurchaseDAL.GetItems(purchaseID);

                int itemCount = dtItems != null ? dtItems.Rows.Count : 0;
                int width = 920;
                int headerH = 145;
                int metaH = 105;
                int tableHeaderH = 38;

                int itemsH = 0;
                if (itemCount > 0)
                {
                    foreach (DataRow ir in dtItems.Rows)
                    {
                        string imei = ir.Table.Columns.Contains("IMEI") ? (ir["IMEI"]?.ToString()?.Trim() ?? "") : "";
                        itemsH += !string.IsNullOrWhiteSpace(imei) ? 50 : 34;
                    }
                }
                else
                {
                    itemsH = 34;
                }

                string notes = purRow["Notes"]?.ToString() ?? "";
                int summaryH = 165 + (string.IsNullOrWhiteSpace(notes) ? 0 : 32);
                int totalH = headerH + metaH + tableHeaderH + itemsH + summaryH + 45;

                var bmp = new Bitmap(width, totalH);
                bmp.SetResolution(144f, 144f);
                bmp.SetResolution(144f, 144f);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    Color cPrimary   = Color.FromArgb(15, 23, 42);
                    Color cSecondary = Color.FromArgb(30, 41, 59);
                    Color cAccent    = Color.FromArgb(5, 150, 105);
                    Color cAltBg     = Color.FromArgb(248, 250, 252);
                    Color cBorder    = Color.FromArgb(203, 213, 225);

                    using (var pThin = new Pen(cBorder, 1f))
                    using (var pBlack = new Pen(Color.FromArgb(51, 65, 85), 1f))
                    using (var pAccent = new Pen(cAccent, 2f))
                    using (var brPrimary = new SolidBrush(cPrimary))
                    using (var brSecondary = new SolidBrush(cSecondary))
                    using (var brAccent = new SolidBrush(cAccent))
                    using (var brAlt = new SolidBrush(cAltBg))
                    using (var fBigTitle = new Font("Segoe UI", 18f, FontStyle.Bold))
                    using (var fTitle = new Font("Segoe UI", 13f, FontStyle.Bold))
                    using (var fBig = new Font("Segoe UI", 12f, FontStyle.Bold))
                    using (var fBold = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 10f, FontStyle.Regular))
                    using (var fSmall = new Font("Segoe UI", 9f, FontStyle.Regular))
                    using (var fImei = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        var SfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        var SfRtlRight = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap,
                            Trimming = StringTrimming.EllipsisCharacter
                        };

                        int y = 16;
                        int pad = 20;
                        int boxW = width - (pad * 2);

                        // 1. Header Bar
                        g.FillRectangle(brPrimary, pad, y, boxW, 52);
                        string storeTitle = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة والتجارة العامة";
                        g.DrawString(storeTitle, fBigTitle, Brushes.White, new RectangleF(pad, y + 8, boxW, 36), SfCenter);

                        y += 60;
                        g.FillRectangle(brAccent, pad, y, boxW, 34);
                        g.DrawString("فاتورة مشتريات بضاعة (وارد مخزن)", fTitle, Brushes.White, new RectangleF(pad, y + 5, boxW, 26), SfCenter);

                        y += 40;
                        string phoneHeader = !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone) ? $"هاتف: {AppConfig.CompanyPhone}" : "";
                        if (!string.IsNullOrEmpty(phoneHeader))
                        {
                            g.DrawString(phoneHeader, fSmall, Brushes.DarkSlateGray, new RectangleF(pad, y, boxW, 18), SfCenter);
                            y += 24;
                        }

                        // 2. Meta details box (Clean RTL from the right)
                        int metaBoxH = 88;
                        g.FillRectangle(brAlt, pad, y, boxW, metaBoxH);
                        g.DrawRectangle(pThin, pad, y, boxW, metaBoxH);

                        string purCode = purRow["PurchaseCode"]?.ToString();
                        if (string.IsNullOrWhiteSpace(purCode)) purCode = "#PUR-" + purchaseID;
                        string dateStr = Convert.ToDateTime(purRow["PurchaseDate"]).ToString("yyyy/MM/dd - hh:mm tt");
                        string suppName = purRow["SupplierName"]?.ToString() ?? "مورد عام";
                        string suppPhone = purRow["SupplierPhone"]?.ToString() ?? "";
                        string warehouse = purRow["WarehouseName"]?.ToString() ?? "المخزن الرئيسي";
                        string safeName = purRow["SafeName"]?.ToString() ?? "الخزينة";
                        string pType = (purRow["PurchaseType"]?.ToString() == "Cash") ? "نقدي (مسدد)" : "آجل (على الحساب)";

                        int halfW = (boxW - 40) / 2;
                        int rightColX = pad + boxW - halfW - 15;
                        int leftColX  = pad + 15;

                        // Right column (Right-aligned to right margin)
                        g.DrawString($"رقم الفاتورة:  {purCode}", fBold, brPrimary, new RectangleF(rightColX, y + 8, halfW, 22), SfRtlRight);
                        g.DrawString($"المورد:  {suppName}{(string.IsNullOrEmpty(suppPhone) ? "" : " (" + suppPhone + ")")}", fBold, brPrimary, new RectangleF(rightColX, y + 33, halfW, 22), SfRtlRight);
                        g.DrawString($"المستودع:  {warehouse}", fNorm, Brushes.DarkSlateGray, new RectangleF(rightColX, y + 58, halfW, 22), SfRtlRight);

                        // Left column (Right-aligned inside left box)
                        g.DrawString($"تاريخ الشراء:  {dateStr}", fNorm, Brushes.Black, new RectangleF(leftColX, y + 8, halfW, 22), SfRtlRight);
                        g.DrawString($"نوع السداد:  {pType}", fBold, (purRow["PurchaseType"]?.ToString() == "Cash" ? Brushes.DarkGreen : Brushes.DarkRed), new RectangleF(leftColX, y + 33, halfW, 22), SfRtlRight);
                        g.DrawString($"الخزينة:  {safeName}", fNorm, Brushes.DarkSlateGray, new RectangleF(leftColX, y + 58, halfW, 22), SfRtlRight);

                        int divX = pad + halfW + 20;
                        g.DrawLine(pThin, divX, y + 10, divX, y + metaBoxH - 10);

                        y += metaBoxH + 16;

                        // 3. Items Table Header
                        int tLeft = pad;
                        int tWidth = boxW;
                        int curX = pad + boxW;
                        int wIdx = 42;
                        int wCode = 78;
                        int wUnit = 62;
                        int wQty = 68;
                        int wBonus = 58;
                        int wPrice = 100;
                        int wTot = 112;
                        int wName = tWidth - (wIdx + wCode + wUnit + wQty + wBonus + wPrice + wTot);

                        g.FillRectangle(brSecondary, tLeft, y, tWidth, tableHeaderH);
                        g.DrawRectangle(pBlack, tLeft, y, tWidth, tableHeaderH);

                        curX -= wIdx; g.DrawRectangle(pBlack, curX, y, wIdx, tableHeaderH); g.DrawString("م", fBold, Brushes.White, new RectangleF(curX, y + 7, wIdx, tableHeaderH - 7), SfCenter);
                        curX -= wCode; g.DrawRectangle(pBlack, curX, y, wCode, tableHeaderH); g.DrawString("الكود", fBold, Brushes.White, new RectangleF(curX, y + 7, wCode, tableHeaderH - 7), SfCenter);
                        curX -= wName; g.DrawRectangle(pBlack, curX, y, wName, tableHeaderH); g.DrawString("اسم الصنف والبيان / السيريال", fBold, Brushes.White, new RectangleF(curX + 8, y + 7, wName - 16, tableHeaderH - 7), SfRtlRight);
                        curX -= wUnit; g.DrawRectangle(pBlack, curX, y, wUnit, tableHeaderH); g.DrawString("الوحدة", fBold, Brushes.White, new RectangleF(curX, y + 7, wUnit, tableHeaderH - 7), SfCenter);
                        curX -= wQty; g.DrawRectangle(pBlack, curX, y, wQty, tableHeaderH); g.DrawString("الكمية", fBold, Brushes.White, new RectangleF(curX, y + 7, wQty, tableHeaderH - 7), SfCenter);
                        curX -= wBonus; g.DrawRectangle(pBlack, curX, y, wBonus, tableHeaderH); g.DrawString("بونص", fBold, Brushes.White, new RectangleF(curX, y + 7, wBonus, tableHeaderH - 7), SfCenter);
                        curX -= wPrice; g.DrawRectangle(pBlack, curX, y, wPrice, tableHeaderH); g.DrawString("سعر الشراء", fBold, Brushes.White, new RectangleF(curX, y + 7, wPrice, tableHeaderH - 7), SfCenter);
                        curX -= wTot; g.DrawRectangle(pBlack, curX, y, wTot, tableHeaderH); g.DrawString("الإجمالي", fBold, Brushes.White, new RectangleF(curX, y + 7, wTot, tableHeaderH - 7), SfCenter);

                        y += tableHeaderH;

                        int itemIdx = 1;
                        decimal totalQty = 0m;
                        decimal totalBonus = 0m;

                        if (itemCount > 0)
                        {
                            foreach (DataRow ir in dtItems.Rows)
                            {
                                string pName = ir["ProductName"]?.ToString() ?? "صنف";
                                string pCode = ir.Table.Columns.Contains("ProductCode") ? (ir["ProductCode"]?.ToString() ?? "") : "";
                                string pUnit = ir.Table.Columns.Contains("UnitName") ? (ir["UnitName"]?.ToString() ?? "") : "";
                                string imei = ir.Table.Columns.Contains("IMEI") ? (ir["IMEI"]?.ToString()?.Trim() ?? "") : "";
                                decimal q = ir["Quantity"] != DBNull.Value ? Convert.ToDecimal(ir["Quantity"]) : 0m;
                                decimal bonus = ir.Table.Columns.Contains("BonusQuantity") && ir["BonusQuantity"] != DBNull.Value ? Convert.ToDecimal(ir["BonusQuantity"]) : 0m;
                                decimal p = ir["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(ir["UnitPrice"]) : 0m;
                                decimal t = ir["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(ir["TotalPrice"]) : (q * p);

                                totalQty += q;
                                totalBonus += bonus;

                                bool hasImei = !string.IsNullOrWhiteSpace(imei);
                                int thisRowH = hasImei ? 50 : 34;

                                if (itemIdx % 2 == 0) g.FillRectangle(brAlt, tLeft, y, tWidth, thisRowH);

                                curX = pad + boxW;
                                curX -= wIdx; g.DrawRectangle(pThin, curX, y, wIdx, thisRowH); g.DrawString(itemIdx.ToString(), fSmall, Brushes.Black, new RectangleF(curX, y + (thisRowH / 2) - 10, wIdx, 20), SfCenter);
                                curX -= wCode; g.DrawRectangle(pThin, curX, y, wCode, thisRowH); g.DrawString(pCode, fSmall, Brushes.DarkSlateGray, new RectangleF(curX, y + (thisRowH / 2) - 10, wCode, 20), SfCenter);

                                curX -= wName; g.DrawRectangle(pThin, curX, y, wName, thisRowH);
                                if (hasImei)
                                {
                                    g.DrawString(pName, fBold, Brushes.Black, new RectangleF(curX + 8, y + 5, wName - 16, 20), SfRtlRight);
                                    g.DrawString($"السيريال: {imei}", fImei, Brushes.DarkSlateBlue, new RectangleF(curX + 8, y + 27, wName - 16, 18), SfRtlRight);
                                }
                                else
                                {
                                    g.DrawString(pName, fBold, Brushes.Black, new RectangleF(curX + 8, y + (thisRowH / 2) - 10, wName - 16, 20), SfRtlRight);
                                }

                                curX -= wUnit; g.DrawRectangle(pThin, curX, y, wUnit, thisRowH); g.DrawString(pUnit, fSmall, Brushes.Black, new RectangleF(curX, y + (thisRowH / 2) - 10, wUnit, 20), SfCenter);
                                curX -= wQty; g.DrawRectangle(pThin, curX, y, wQty, thisRowH); g.DrawString(q.ToString("0.##"), fBold, Brushes.DarkBlue, new RectangleF(curX, y + (thisRowH / 2) - 10, wQty, 20), SfCenter);
                                curX -= wBonus; g.DrawRectangle(pThin, curX, y, wBonus, thisRowH); g.DrawString(bonus > 0 ? bonus.ToString("0.##") : "-", fSmall, (bonus > 0 ? Brushes.DarkGreen : Brushes.Gray), new RectangleF(curX, y + (thisRowH / 2) - 10, wBonus, 20), SfCenter);
                                curX -= wPrice; g.DrawRectangle(pThin, curX, y, wPrice, thisRowH); g.DrawString(p.ToString("N2"), fNorm, Brushes.Black, new RectangleF(curX, y + (thisRowH / 2) - 10, wPrice, 20), SfCenter);
                                curX -= wTot; g.DrawRectangle(pThin, curX, y, wTot, thisRowH); g.DrawString(t.ToString("N2"), fBold, brAccent, new RectangleF(curX, y + (thisRowH / 2) - 10, wTot, 20), SfCenter);

                                y += thisRowH;
                                itemIdx++;
                            }
                        }

                        // 4. Summary & Totals
                        decimal totalAmount = Convert.ToDecimal(purRow["TotalAmount"]);
                        decimal paidAmount = Convert.ToDecimal(purRow["PaidAmount"]);
                        decimal remainingAmount = Convert.ToDecimal(purRow["RemainingAmount"]);
                        decimal discountAmt = Convert.ToDecimal(purRow["DiscountAmount"]);
                        decimal taxAmt = Convert.ToDecimal(purRow["TaxAmount"]);

                        y += 12;
                        int summaryBoxH = 80;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(236, 253, 245)), tLeft, y, tWidth, summaryBoxH);
                        g.DrawRectangle(pAccent, tLeft, y, tWidth, summaryBoxH);

                        int sCol = tWidth / 3;
                        g.DrawString($"إجمالي الفاتورة:  {totalAmount:N2} ج", fBig, brAccent, new RectangleF(tLeft + sCol * 2, y + 12, sCol, 26), SfCenter);
                        g.DrawString($"المسدد / المدفوع:  {paidAmount:N2} ج", fBig, Brushes.DarkGreen, new RectangleF(tLeft + sCol, y + 12, sCol, 26), SfCenter);
                        g.DrawString($"المتبقي:  {remainingAmount:N2} ج", fBig, (remainingAmount > 0 ? Brushes.DarkRed : Brushes.DarkGreen), new RectangleF(tLeft, y + 12, sCol, 26), SfCenter);

                        string statsLine = $"عدد الأصناف: {itemCount}  |  إجمالي الكميات: {totalQty:0.##}";
                        if (totalBonus > 0) statsLine += $"  |  إجمالي البونص: {totalBonus:0.##}";
                        if (discountAmt > 0) statsLine += $"  |  الخصم: {discountAmt:N2} ج";
                        if (taxAmt > 0) statsLine += $"  |  الضريبة: {taxAmt:N2} ج";
                        g.DrawString(statsLine, fSmall, Brushes.DarkSlateGray, new RectangleF(tLeft, y + 48, tWidth, 20), SfCenter);

                        y += summaryBoxH + 10;

                        // Supplier Balance if credit
                        int suppId = purRow["SupplierID"] != DBNull.Value ? Convert.ToInt32(purRow["SupplierID"]) : 0;
                        if (suppId > 0)
                        {
                            try
                            {
                                decimal suppBal = SupplierDAL.GetBalance(suppId);
                                g.FillRectangle(brAlt, tLeft, y, tWidth, 28);
                                g.DrawRectangle(pThin, tLeft, y, tWidth, 28);
                                g.DrawString($"رصيد حساب المورد الحالي طرفنا:  {suppBal:N2} جنيه", fBold, Brushes.DarkSlateBlue, new RectangleF(tLeft, y + 4, tWidth, 20), SfCenter);
                                y += 36;
                            }
                            catch { }
                        }

                        if (!string.IsNullOrWhiteSpace(notes))
                        {
                            g.FillRectangle(brAlt, tLeft, y, tWidth, 26);
                            g.DrawRectangle(pThin, tLeft, y, tWidth, 26);
                            g.DrawString($"ملاحظات: {notes}", fNorm, Brushes.DarkSlateGray, new RectangleF(tLeft + 8, y + 3, tWidth - 16, 20), SfRtlRight);
                            y += 32;
                        }

                        // Footer
                        g.DrawLine(pThin, tLeft, y, tLeft + tWidth, y);
                        y += 8;
                        g.DrawString("توقيع أمين المستودع / المستلم: ....................", fSmall, Brushes.Black, new RectangleF(tLeft + tWidth / 2, y, tWidth / 2, 20), SfRtlRight);
                        g.DrawString("توقيع المورد / المندوب: ....................", fSmall, Brushes.Black, new RectangleF(tLeft, y, tWidth / 2, 20), SfRtlRight);

                        y += 24;
                        g.DrawString("نظام ProSoft ERP لإدارة الأعمال والمخازن", fSmall, Brushes.Gray, new RectangleF(tLeft, y, tWidth, 18), SfCenter);
                    }

                    return bmp;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GeneratePurchaseReceiptImage", ex);
                return null;
            }
        }

        /// <summary>
        /// تصميم كارت صورة احترافي عالي الدقة لأمر الشراء / طلب التوريد للمورد
        /// </summary>
        public static Bitmap GeneratePurchaseOrderImage(PurchaseOrderDTO order)
        {
            if (order == null) return null;

            try
            {
                int itemsCount = (order.Items != null && order.Items.Count > 0) ? order.Items.Count : order.TotalItemsCount;
                decimal totalQty = (order.Items != null && order.Items.Count > 0) ? order.Items.Sum(x => x.Quantity) : order.TotalQuantity;
                decimal totalCost = (order.Items != null && order.Items.Count > 0) ? order.Items.Sum(x => x.TotalPrice) : order.TotalEstimatedCost;
                int width = 720;
                int rowH = 30;
                int headerH = 135;
                int metaH = 100;
                int tableHeaderH = 34;
                int itemsH = Math.Max(itemsCount, 1) * rowH;
                string notes = order.Notes ?? "";
                int summaryH = 140 + (string.IsNullOrWhiteSpace(notes) ? 0 : 30);
                int totalH = headerH + metaH + tableHeaderH + itemsH + summaryH + 40;

                var bmp = new Bitmap(width, totalH);
                bmp.SetResolution(144f, 144f);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    Color cPrimary   = Color.FromArgb(15, 23, 42);
                    Color cSecondary = Color.FromArgb(30, 41, 59);
                    Color cAccent    = Color.FromArgb(139, 92, 246);
                    Color cAltBg     = Color.FromArgb(248, 250, 252);
                    Color cBorder    = Color.FromArgb(226, 232, 240);

                    using (var penOuter = new Pen(Color.FromArgb(203, 213, 225), 2f))
                    {
                        g.DrawRectangle(penOuter, 1, 1, width - 3, totalH - 3);
                    }

                    // 1. Header Banner
                    using (var brHead = new LinearGradientBrush(new Rectangle(0, 0, width, headerH), cPrimary, cSecondary, LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(brHead, 0, 0, width, headerH);
                    }
                    using (var brAccent = new SolidBrush(cAccent))
                    {
                        g.FillRectangle(brAccent, 0, headerH - 5, width, 5);
                    }

                    string coName = AppConfig.CompanyName;
                    if (string.IsNullOrWhiteSpace(coName)) coName = "الشركة للأنظمة والتوزيع";

                    using (var fCo = new Font("Segoe UI", 19f, FontStyle.Bold))
                    using (var fTitle = new Font("Segoe UI", 13f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 9.5f))
                    using (var brWhite = new SolidBrush(Color.White))
                    using (var brGold = new SolidBrush(Color.FromArgb(253, 224, 71)))
                    using (var brMuted = new SolidBrush(Color.FromArgb(203, 213, 225)))
                    {
                        g.DrawString(CleanEmoji(coName), fCo, brWhite, new RectangleF(20, 16, width - 40, 36), SfCenter);
                        g.DrawString("طلب توريد بضاعة / أمر شراء رسمي (Purchase Order)", fTitle, brGold, new RectangleF(20, 54, width - 40, 28), SfCenter);

                        int midX = width / 2;
                        using (var brChipBg = new SolidBrush(Color.FromArgb(45, 255, 255, 255)))
                        using (var penChip = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
                        using (var fChip = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                        {
                            var rChipDate = new Rectangle(midX + 10, 88, 195, 26);
                            var rChipCode = new Rectangle(midX - 205, 88, 195, 26);

                            g.FillRectangle(brChipBg, rChipDate);
                            g.DrawRectangle(penChip, rChipDate);
                            g.FillRectangle(brChipBg, rChipCode);
                            g.DrawRectangle(penChip, rChipCode);

                            g.DrawString($"تاريخ الطلب: {order.OrderDate:yyyy/MM/dd}", fChip, brWhite, rChipDate, SfCenter);
                            g.DrawString($"رقم الطلبية: {order.OrderCode}", fChip, brGold, rChipCode, SfCenter);
                        }
                    }

                    int y = headerH + 12;

                    // 2. Meta Info Card
                    int tableX = 20;
                    int tableW = width - 40;

                    using (var brMetaBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                    using (var penMeta = new Pen(cBorder, 1f))
                    using (var penDiv = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    {
                        var rMeta = new Rectangle(tableX, y, tableW, metaH);
                        g.FillRectangle(brMetaBg, rMeta);
                        g.DrawRectangle(penMeta, rMeta);
                        g.DrawLine(penDiv, tableX + 350, y + 8, tableX + 350, y + metaH - 8);
                    }

                    using (var fLabel = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    using (var fVal = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                    using (var fPhone = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                    using (var brDark = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    using (var brPrimary = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brBlue = new SolidBrush(Color.FromArgb(2, 132, 199)))
                    using (var brAmber = new SolidBrush(Color.FromArgb(180, 83, 9)))
                    {
                        // Right column: المورد المكرم وموعد التوريد
                        int colRightX = tableX + tableW; // 700
                        g.DrawString("المورد المكرم:", fLabel, brDark, new RectangleF(colRightX - 100, y + 12, 90, 22), SfRtlRight);
                        string supName = CleanEmoji(order.SupplierName ?? "");
                        g.DrawString(supName, fVal, brPrimary, new RectangleF(tableX + 360, y + 10, colRightX - 100 - (tableX + 360), 44), SfRtlRight);

                        string expDate = order.ExpectedDeliveryDate.HasValue ? order.ExpectedDeliveryDate.Value.ToString("yyyy/MM/dd") : "عاجل في أقرب وقت";
                        g.DrawString("موعد التوريد:", fLabel, brDark, new RectangleF(colRightX - 100, y + 58, 90, 22), SfRtlRight);
                        g.DrawString(expDate, fVal, brAmber, new RectangleF(tableX + 360, y + 58, colRightX - 100 - (tableX + 360), 24), SfRtlRight);

                        // Left column: هاتف المورد وحالة الطلبية
                        int colLeftX = tableX + 340; // 360
                        g.DrawString("هاتف المورد:", fLabel, brDark, new RectangleF(colLeftX - 90, y + 12, 85, 22), SfRtlRight);
                        string phoneVal = string.IsNullOrWhiteSpace(order.SupplierPhone) ? "---" : order.SupplierPhone.Trim();
                        g.DrawString(phoneVal, fPhone, brBlue, new RectangleF(tableX + 15, y + 12, colLeftX - 90 - (tableX + 15), 24), SfCenter);

                        g.DrawString("حالة الطلبية:", fLabel, brDark, new RectangleF(colLeftX - 90, y + 58, 85, 22), SfRtlRight);
                        string statusVal = string.IsNullOrWhiteSpace(order.Status) ? "معلقة" : order.Status.Trim();
                        Color statusColor = statusVal.Contains("مكتمل") ? Color.FromArgb(22, 163, 74) : (statusVal.Contains("ملغ") ? Color.FromArgb(220, 38, 38) : Color.FromArgb(217, 119, 6));
                        Color statusBg = statusVal.Contains("مكتمل") ? Color.FromArgb(220, 252, 231) : (statusVal.Contains("ملغ") ? Color.FromArgb(254, 226, 226) : Color.FromArgb(254, 243, 199));
                        
                        var rStatus = new Rectangle(tableX + 45, y + 56, 140, 26);
                        using (var brStBg = new SolidBrush(statusBg))
                        using (var penSt = new Pen(statusColor, 1f))
                        using (var brStText = new SolidBrush(statusColor))
                        {
                            g.FillRectangle(brStBg, rStatus);
                            g.DrawRectangle(penSt, rStatus);
                            g.DrawString(statusVal, fVal, brStText, rStatus, SfCenter);
                        }
                    }

                    y += metaH + 16;

                    // 3. Table Header

                    int wNum = 35, wCode = 90, wName = 235, wUnit = 60, wQty = 85, wPrice = 85, wTotal = 90;

                    using (var brTh = new SolidBrush(Color.FromArgb(30, 41, 59)))
                    {
                        g.FillRectangle(brTh, tableX, y, tableW, tableHeaderH);
                    }

                    using (var fTh = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    using (var brWhite = new SolidBrush(Color.White))
                    {
                        int curX = tableX + tableW;
                        DrawCellRtl(g, "#", fTh, brWhite, curX, y, wNum, tableHeaderH, SfCenter); curX -= wNum;
                        DrawCellRtl(g, "كود الصنف", fTh, brWhite, curX, y, wCode, tableHeaderH, SfCenter); curX -= wCode;
                        DrawCellRtl(g, "اسم الصنف والبيان", fTh, brWhite, curX, y, wName, tableHeaderH, SfRtlRight); curX -= wName;
                        DrawCellRtl(g, "الوحدة", fTh, brWhite, curX, y, wUnit, tableHeaderH, SfCenter); curX -= wUnit;
                        DrawCellRtl(g, "الكمية المطلوبة", fTh, brWhite, curX, y, wQty, tableHeaderH, SfCenter); curX -= wQty;
                        DrawCellRtl(g, "السعر المتوقع", fTh, brWhite, curX, y, wPrice, tableHeaderH, SfCenter); curX -= wPrice;
                        DrawCellRtl(g, "الإجمالي", fTh, brWhite, curX, y, wTotal, tableHeaderH, SfCenter);
                    }

                    y += tableHeaderH;

                    // 4. Table Rows
                    using (var fCell = new Font("Segoe UI", 9f))
                    using (var fCellBold = new Font("Segoe UI", 9f, FontStyle.Bold))
                    using (var penLine = new Pen(cBorder, 1f))
                    using (var brRowDark = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brQty = new SolidBrush(Color.FromArgb(21, 128, 61)))
                    using (var brAlt = new SolidBrush(cAltBg))
                    {
                        int rowIdx = 0;
                        if (order.Items != null)
                        {
                            foreach (var itm in order.Items)
                            {
                                if (rowIdx % 2 == 1)
                                {
                                    g.FillRectangle(brAlt, tableX, y, tableW, rowH);
                                }
                                g.DrawLine(penLine, tableX, y + rowH, tableX + tableW, y + rowH);

                                int curX = tableX + tableW;
                                DrawCellRtl(g, (rowIdx + 1).ToString(), fCell, brRowDark, curX, y, wNum, rowH, SfCenter); curX -= wNum;
                                DrawCellRtl(g, itm.ProductCode ?? "", fCell, brRowDark, curX, y, wCode, rowH, SfCenter); curX -= wCode;
                                DrawCellRtl(g, CleanEmoji(itm.ProductName ?? ""), fCellBold, brRowDark, curX, y, wName, rowH, SfRtlRight); curX -= wName;
                                DrawCellRtl(g, itm.UnitName ?? "قطعة", fCell, brRowDark, curX, y, wUnit, rowH, SfCenter); curX -= wUnit;
                                DrawCellRtl(g, itm.Quantity.ToString("0.##"), fCellBold, brQty, curX, y, wQty, rowH, SfCenter); curX -= wQty;
                                DrawCellRtl(g, itm.EstimatedUnitPrice > 0 ? itm.EstimatedUnitPrice.ToString("N2") : "-", fCell, brRowDark, curX, y, wPrice, rowH, SfCenter); curX -= wPrice;
                                DrawCellRtl(g, itm.TotalPrice > 0 ? itm.TotalPrice.ToString("N2") : "-", fCellBold, brRowDark, curX, y, wTotal, rowH, SfCenter);

                                y += rowH;
                                rowIdx++;
                            }
                        }
                    }

                    // Outer border for table
                    using (var penTable = new Pen(Color.FromArgb(203, 213, 225), 1f))
                    {
                        g.DrawRectangle(penTable, tableX, headerH + metaH + 28, tableW, tableHeaderH + itemsH);
                    }

                    y += 14;

                    // 5. Summary & Totals Card
                    using (var brSumBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                    using (var penSum = new Pen(Color.FromArgb(203, 213, 225), 1f))
                    {
                        var rSum = new Rectangle(tableX, y, tableW, summaryH - 12);
                        g.FillRectangle(brSumBg, rSum);
                        g.DrawRectangle(penSum, rSum);
                    }

                    using (var fMetricLabel = new Font("Segoe UI", 9f, FontStyle.Bold))
                    using (var fMetricVal = new Font("Segoe UI", 12f, FontStyle.Bold))
                    using (var brDark = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    using (var brBlue = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    using (var brGreen = new SolidBrush(Color.FromArgb(16, 149, 78)))
                    using (var brSlate = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        int sumY = y + 10;

                        // 3 Balanced Metric Boxes
                        int boxW = (tableW - 30) / 3; // ~216px each
                        int boxH = 46;

                        var rBox1 = new Rectangle(tableX + tableW - 10 - boxW, sumY, boxW, boxH);
                        var rBox2 = new Rectangle(tableX + tableW - 15 - (boxW * 2), sumY, boxW, boxH);
                        var rBox3 = new Rectangle(tableX + 10, sumY, boxW, boxH);

                        using (var brBoxBg = new SolidBrush(Color.White))
                        using (var penBox = new Pen(Color.FromArgb(226, 232, 240), 1f))
                        {
                            g.FillRectangle(brBoxBg, rBox1); g.DrawRectangle(penBox, rBox1);
                            g.FillRectangle(brBoxBg, rBox2); g.DrawRectangle(penBox, rBox2);
                            g.FillRectangle(brBoxBg, rBox3); g.DrawRectangle(penBox, rBox3);
                        }

                        // Box 1 (Right): Items Count
                        g.DrawString("عدد الأصناف المطلوبة", fMetricLabel, brDark, new RectangleF(rBox1.X, rBox1.Y + 4, rBox1.Width, 18), SfCenter);
                        g.DrawString($"{itemsCount} صنف", fMetricVal, brBlue, new RectangleF(rBox1.X, rBox1.Y + 20, rBox1.Width, 24), SfCenter);

                        // Box 2 (Center): Total Quantity
                        g.DrawString("إجمالي الكميات", fMetricLabel, brDark, new RectangleF(rBox2.X, rBox2.Y + 4, rBox2.Width, 18), SfCenter);
                        g.DrawString($"{totalQty:0.##}", fMetricVal, brSlate, new RectangleF(rBox2.X, rBox2.Y + 20, rBox2.Width, 24), SfCenter);

                        // Box 3 (Left): Total Estimated Cost
                        g.DrawString("إجمالي القيمة التقديرية", fMetricLabel, brDark, new RectangleF(rBox3.X, rBox3.Y + 4, rBox3.Width, 18), SfCenter);
                        string costTxt = totalCost > 0 ? $"{totalCost:N2} ج" : "غير محدد";
                        g.DrawString(costTxt, fMetricVal, brGreen, new RectangleF(rBox3.X, rBox3.Y + 20, rBox3.Width, 24), SfCenter);

                        sumY += boxH + 8;

                        if (!string.IsNullOrWhiteSpace(order.Notes))
                        {
                            using (var penDash = new Pen(Color.FromArgb(203, 213, 225), 1f) { DashStyle = DashStyle.Dash })
                            {
                                g.DrawLine(penDash, tableX + 15, sumY, tableX + tableW - 15, sumY);
                            }
                            sumY += 6;
                            g.DrawString("ملاحظات الطلبية: " + CleanEmoji(order.Notes), new Font("Segoe UI", 9.5f), brSlate, new RectangleF(tableX + 15, sumY, tableW - 30, 22), SfRtlRight);
                            sumY += 24;
                        }

                        g.DrawString("برجاء مراجعة الأصناف والكميات الموضحة وتأكيد موعد استلام البضاعة فور استلامكم لأمر الشراء.", new Font("Segoe UI", 9f, FontStyle.Italic), Brushes.Gray, new RectangleF(tableX + 15, sumY + 2, tableW - 30, 20), SfCenter);
                    }

                    y += summaryH;

                    // 6. Footer
                    using (var fFoot = new Font("Segoe UI", 8.5f))
                    {
                        string coPhone = AppConfig.CompanyPhone;
                        string coAddr = AppConfig.CompanyAddress;
                        string footerText = $"تم الإصدار آلياً بواسطة نظام {CleanEmoji(coName)}";
                        if (!string.IsNullOrWhiteSpace(coPhone)) footerText += $"  |  هاتف: {coPhone}";
                        if (!string.IsNullOrWhiteSpace(coAddr)) footerText += $"  |  {coAddr}";

                        g.DrawString(footerText, fFoot, Brushes.Gray, new RectangleF(20, y, width - 40, 18), SfCenter);
                    }

                    return bmp;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GeneratePurchaseOrderImage", ex);
                return null;
            }
        }

        private static void DrawCellRtl(Graphics g, string text, Font font, Brush brush, int rightX, int y, int width, int height, StringFormat sf)
        {
            RectangleF rect = new RectangleF(rightX - width, y, width, height);
            g.DrawString(text ?? "", font, brush, rect, sf);
        }

        public static Bitmap GenerateOnlineOrderReceiptImage(int onlineOrderID)
        {
            try
            {
                var dtOrder = DbHelper.Query(@"
                    SELECT OnlineOrderID,
                           ISNULL(OrderNumber, '#' + CAST(OnlineOrderID AS NVARCHAR(20))) AS OrderNumber,
                           OrderDate, CustomerName, CustomerPhone,
                           ISNULL(CustomerAddress, '') AS CustomerAddress,
                           ISNULL(Notes, '') AS Notes,
                           ISNULL(SubTotal, 0) AS SubTotal,
                           ISNULL(DeliveryCharge, 0) AS DeliveryCharge,
                           ISNULL(TotalAmount, 0) AS TotalAmount,
                           ISNULL(PriceTier, N'قطاعي') AS PriceTier,
                           ISNULL(Status, N'جديد') AS Status,
                           CreatedSaleID
                    FROM OnlineOrders WITH (NOLOCK)
                    WHERE OnlineOrderID = @id", DbHelper.P("@id", onlineOrderID));

                if (dtOrder == null || dtOrder.Rows.Count == 0) return null;
                var row = dtOrder.Rows[0];

                var dtItems = OnlineOrdersDAL.GetOrderItems(onlineOrderID, false);
                int itemCount = dtItems != null ? dtItems.Rows.Count : 0;

                int width = 640;
                int baseHeight = 440;
                int rowH = 28;
                int totalH = baseHeight + (itemCount * rowH);

                var bmp = new Bitmap(width, totalH);
                bmp.SetResolution(144f, 144f);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.White);

                    // 1. Header Gradient
                    using (var brHeader = new LinearGradientBrush(new Rectangle(0, 0, width, 90), Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brHeader, 0, 0, width, 90);
                    }

                    string companyName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المتجر الإلكتروني";
                    using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                    using (var fSmall = new Font("Segoe UI", 8.5f))
                    {
                        g.DrawString(companyName, fTitle, Brushes.White, new RectangleF(0, 10, width, 32), SfCenter);
                        g.DrawString("إشعار تأكيد طلب المتجر الإلكتروني", fSub, new SolidBrush(Color.FromArgb(56, 189, 248)), new RectangleF(0, 44, width, 24), SfCenter);
                        string coPhone = !string.IsNullOrWhiteSpace(AppConfig.Store_OrderNotificationWhatsApp) ? AppConfig.Store_OrderNotificationWhatsApp : AppConfig.CompanyPhone;
                        if (!string.IsNullOrWhiteSpace(coPhone))
                        {
                            g.DrawString($"خدمة العملاء والواتساب: {coPhone}", fSmall, Brushes.LightGray, new RectangleF(0, 68, width, 18), SfCenter);
                        }
                    }

                    int y = 100;
                    int tLeft = 20;
                    int tWidth = width - 40;

                    using (var pBorder = new Pen(Color.FromArgb(203, 213, 225)))
                    using (var pThin = new Pen(Color.FromArgb(226, 232, 240)))
                    using (var fBold = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    using (var fNorm = new Font("Segoe UI", 9f))
                    using (var fTotal = new Font("Segoe UI", 13f, FontStyle.Bold))
                    using (var brNavy = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var brBlue = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    using (var brEmerald = new SolidBrush(Color.FromArgb(5, 150, 105)))
                    {
                        // 2. Order Metadata Box
                        int metaH = 80;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), tLeft, y, tWidth, metaH);
                        g.DrawRectangle(pBorder, tLeft, y, tWidth, metaH);

                        string orderNum = row["OrderNumber"]?.ToString() ?? "";
                        DateTime oDate = row["OrderDate"] != DBNull.Value ? Convert.ToDateTime(row["OrderDate"]) : DateTime.Now;
                        string custName = row["CustomerName"]?.ToString() ?? "";
                        string custPhone = row["CustomerPhone"]?.ToString() ?? "";
                        string custAddr = row["CustomerAddress"]?.ToString() ?? "";
                        string status = row["Status"]?.ToString() ?? "جديد";

                        // Line 1: Order Num & Date
                        g.DrawString($"رقم الطلب:  {orderNum}", fBold, brBlue, new RectangleF(tLeft + tWidth / 2, y + 6, tWidth / 2 - 10, 22), SfRtlRight);
                        g.DrawString($"تاريخ الطلب:  {oDate:yyyy/MM/dd HH:mm}", fNorm, brNavy, new RectangleF(tLeft + 10, y + 6, tWidth / 2 - 10, 22), SfRtlRight);

                        // Line 2: Customer Name & Phone
                        g.DrawString($"اسم العميل:  {custName}", fBold, brNavy, new RectangleF(tLeft + tWidth / 2, y + 30, tWidth / 2 - 10, 22), SfRtlRight);
                        g.DrawString($"رقم الهاتف:  {custPhone}", fBold, brNavy, new RectangleF(tLeft + 10, y + 30, tWidth / 2 - 10, 22), SfRtlRight);

                        // Line 3: Address & Status
                        string addrStr = string.IsNullOrWhiteSpace(custAddr) ? "استلام من المحل" : custAddr;
                        g.DrawString($"العنوان:  {addrStr}", fNorm, Brushes.DarkSlateGray, new RectangleF(tLeft + tWidth / 2, y + 54, tWidth / 2 - 10, 22), SfRtlRight);
                        g.DrawString($"حالة الطلب:  {status}", fBold, brEmerald, new RectangleF(tLeft + 10, y + 54, tWidth / 2 - 10, 22), SfRtlRight);

                        y += metaH + 12;

                        // 3. Items Table Header
                        int tableH = 28;
                        int wSeq = 35;
                        int wName = 250;
                        int wUnit = 70;
                        int wQty = 65;
                        int wPrice = 80;
                        int wTot = tWidth - (wSeq + wName + wUnit + wQty + wPrice);

                        g.FillRectangle(new SolidBrush(Color.FromArgb(30, 41, 59)), tLeft, y, tWidth, tableH);
                        g.DrawRectangle(pBorder, tLeft, y, tWidth, tableH);

                        int curX = tLeft + tWidth;
                        DrawCell(g, "م", ref curX, wSeq, fBold, Brushes.White, y, tableH, SfCenter);
                        DrawCell(g, "اسم الصنف", ref curX, wName, fBold, Brushes.White, y, tableH, SfRtlRight);
                        DrawCell(g, "الوحدة", ref curX, wUnit, fBold, Brushes.White, y, tableH, SfCenter);
                        DrawCell(g, "الكمية", ref curX, wQty, fBold, Brushes.White, y, tableH, SfCenter);
                        DrawCell(g, "السعر", ref curX, wPrice, fBold, Brushes.White, y, tableH, SfCenter);
                        DrawCell(g, "الإجمالي", ref curX, wTot, fBold, Brushes.White, y, tableH, SfCenter);

                        y += tableH;

                        // 4. Items Rows
                        int seq = 1;
                        if (dtItems != null && dtItems.Rows.Count > 0)
                        {
                            foreach (DataRow ir in dtItems.Rows)
                            {
                                string pName = ir["ProductName"]?.ToString() ?? "";
                                string unit = ir["UnitName"]?.ToString() ?? "";
                                decimal qty = ir["Quantity"] != DBNull.Value ? Convert.ToDecimal(ir["Quantity"]) : 0m;
                                decimal price = ir["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(ir["UnitPrice"]) : 0m;
                                decimal tot = ir["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(ir["TotalPrice"]) : (qty * price);

                                if (seq % 2 == 0)
                                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), tLeft, y, tWidth, rowH);

                                g.DrawRectangle(pThin, tLeft, y, tWidth, rowH);

                                curX = tLeft + tWidth;
                                DrawCell(g, seq.ToString(), ref curX, wSeq, fNorm, Brushes.Black, y, rowH, SfCenter);
                                DrawCell(g, pName, ref curX, wName, fBold, brNavy, y, rowH, SfRtlRight);
                                DrawCell(g, unit, ref curX, wUnit, fNorm, Brushes.DarkSlateGray, y, rowH, SfCenter);
                                DrawCell(g, qty.ToString("0.##"), ref curX, wQty, fBold, brBlue, y, rowH, SfCenter);
                                DrawCell(g, price.ToString("N2"), ref curX, wPrice, fNorm, Brushes.Black, y, rowH, SfCenter);
                                DrawCell(g, tot.ToString("N2"), ref curX, wTot, fBold, brEmerald, y, rowH, SfCenter);

                                y += rowH;
                                seq++;
                            }
                        }

                        y += 10;

                        // 5. Totals Box
                        decimal subTotal = row["SubTotal"] != DBNull.Value ? Convert.ToDecimal(row["SubTotal"]) : 0m;
                        decimal delivery = row["DeliveryCharge"] != DBNull.Value ? Convert.ToDecimal(row["DeliveryCharge"]) : 0m;
                        decimal totalAmt = row["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(row["TotalAmount"]) : 0m;

                        if (delivery > 0)
                        {
                            g.DrawString($"المجموع الفرعي للأصناف: {subTotal:N2} ج.م", fNorm, brNavy, new RectangleF(tLeft + 10, y, tWidth - 20, 20), SfRtlRight);
                            y += 22;
                            g.DrawString($"خدمة التوصيل: {delivery:N2} ج.م", fNorm, Brushes.DarkSlateGray, new RectangleF(tLeft + 10, y, tWidth - 20, 20), SfRtlRight);
                            y += 24;
                        }

                        int totBoxH = 42;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(236, 253, 245)), tLeft, y, tWidth, totBoxH);
                        g.DrawRectangle(new Pen(Color.FromArgb(16, 185, 129), 1.8f), tLeft, y, tWidth, totBoxH);
                        g.DrawString($"الإجمالي المطلوب سداده:   {totalAmt:N2} ج.م", fTotal, brEmerald, new RectangleF(tLeft + 15, y + 8, tWidth - 30, 26), SfRtlRight);
                        y += totBoxH + 6;

                        try
                        {
                            string tafqeet = TafqeetHelper.ConvertToArabicWords(totalAmt);
                            g.DrawString($"فقط وقدره: {tafqeet} لا غير.", fNorm, Brushes.DimGray, new RectangleF(tLeft, y, tWidth, 20), SfCenter);
                            y += 24;
                        }
                        catch { }

                        string notes = row["Notes"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(notes))
                        {
                            g.FillRectangle(new SolidBrush(Color.FromArgb(254, 249, 195)), tLeft, y, tWidth, 26);
                            g.DrawRectangle(new Pen(Color.FromArgb(234, 179, 8)), tLeft, y, tWidth, 26);
                            g.DrawString($"ملاحظات العميل: {notes}", fNorm, Brushes.DarkGoldenrod, new RectangleF(tLeft + 8, y + 3, tWidth - 16, 20), SfRtlRight);
                            y += 32;
                        }

                        // 6. Footer
                        y += 10;
                        g.DrawLine(pBorder, tLeft, y, tLeft + tWidth, y);
                        y += 8;
                        g.DrawString("نسعد دائماً بخدمتكم وتلبية طلباتكم | نشكركم لتسوقكم معنا عبر المتجر الإلكتروني", fBold, brNavy, new RectangleF(0, y, width, 22), SfCenter);
                    }

                    return bmp;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ReceiptImageGenerator.GenerateOnlineOrderReceiptImage", ex);
                return null;
            }
        }

        private static void DrawCell(Graphics g, string text, ref int x, int w, Font f, Brush br, int y, int h, StringFormat sf)
        {
            x -= w;
            var rect = new RectangleF(x + 4, y + (h - f.Height) / 2f, w - 8, f.Height + 2);
            g.DrawString(text, f, br, rect, sf);
        }

        private static string CleanEmoji(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(input, @"\p{Cs}|\p{So}|\p{Sk}|\p{Cn}", "");
        }
    }
}

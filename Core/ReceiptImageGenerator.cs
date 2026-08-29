using System;
using System.Collections.Generic;
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
                        bool isCredit = row["SaleType"].ToString() == "Credit";
                        decimal cashPaid = row["CashPaid"] != DBNull.Value ? Convert.ToDecimal(row["CashPaid"]) : netVal;
                        decimal rem = isCredit ? netVal : (netVal - cashPaid);
                        currentBalance = prevBalance + rem;

                        var dtLastPay = DbHelper.Query(@"
                            SELECT TOP 1 Credit AS PaymentAmount, TransDate AS PaymentDate 
                            FROM ClientTransactions 
                            WHERE ClientID = @cid AND TransType = 'Payment' AND Credit > 0
                            ORDER BY TransDate DESC, TransID DESC", DbHelper.P("@cid", clientID));
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
                    bool isCredit = row["SaleType"].ToString() == "Credit";
                    decimal cashPaid = row["CashPaid"] != DBNull.Value ? Convert.ToDecimal(row["CashPaid"]) : netVal;
                    currentBalance = isCredit ? netVal : (netVal - cashPaid);
                    if (cashPaid > 0)
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

                int itemCount = dtItems != null ? dtItems.Rows.Count : 0;
                bool showFinancial = row.Table.Columns.Contains("ClientID") && row["ClientID"] != DBNull.Value;
                decimal netVal = Convert.ToDecimal(row["TotalAmount"]);
                decimal paidVal = row.Table.Columns.Contains("CashPaid") && row["CashPaid"] != DBNull.Value ? Convert.ToDecimal(row["CashPaid"]) : (row["SaleType"].ToString() == "Cash" ? netVal : 0m);
                decimal remainVal = netVal - paidVal;

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
                int footerH = isAlTarek ? 30 : 70;

                int totalH = headerH + metaH + tableHeaderH + itemsH + netH + 20 + financialH + footerH + 40;
                var bmp = new Bitmap(width, totalH);

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
                            int x0 = 20;
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
                                lastPayStr = $"آخر توريد / سداد للعميل:  {lastPaymentAmt:N2} ج.م{dStr}";
                            }
                            else if (paidVal > 0)
                            {
                                lastPayStr = $"آخر سداد:  {paidVal:N2} ج.م (مسدد مع الفاتورة)";
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

                                string[] sumLabels = { "الإجمالي قبل الخصم", "الخصم", "الإجمالي بعد الخصم", "المدفوع", "المتبقي", "الرصيد السابق", "الرصيد الحالي" };
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
                                if (todayPayments > 0)
                                {
                                    finLabels.Add("المسدد اليوم");
                                    finVals.Add($"{todayPayments:N2} ج.م");
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
                                g.DrawString("🙏 شكراً لتعاملكم معنا ونتمنى لكم دوام التوفيق والنجاح", fSub, brPrimary, new RectangleF(0, y, width, 24), SfCenter);
                                y += 24;
                            }

                            // System signature
                            using (var fPromo = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                            {
                                g.DrawString("✨ تم إنشاء هذا المستند آلياً بواسطة Pro System لإدارة المبيعات والتوزيع", fPromo, Brushes.Gray, new RectangleF(0, y, width, 20), SfCenter);
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
                int[] colW = { 40, 130, 105, 240, 105, 105, 115 }; // Total = 840
                string[] colHeaders = { "م", "التاريخ والوقت", "نوع الحركة", "تفاصيل البيان / الفاتورة", "مدين (+)", "دائن (-)", "الرصيد (ج)" };

                for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
                {
                    int startRow = pageIdx * 25;
                    int takeCount = pageIdx == 0 && totalPages > 1 ? 25 : (totalRowCount - startRow);
                    if (takeCount < 0) takeCount = 0;

                    int pageRowCount = takeCount;
                    int rowH = 30;
                    int dynamicHeight = 220 + (pageRowCount * rowH) + (pageIdx == totalPages - 1 ? 160 : 70);
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
                        using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                        using (var fSub = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                        using (var fBold = new Font("Segoe UI", 10f, FontStyle.Bold))
                        using (var fNorm = new Font("Segoe UI", 9.5f))
                        using (var fSmall = new Font("Segoe UI", 8.5f))
                        {
                            // Outer Border
                            g.DrawRectangle(penBorder, 10, 10, width - 20, dynamicHeight - 20);

                            // Header Banner
                            g.FillRectangle(brHeader, 12, 12, width - 24, 75);
                            string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة للأنظمة والتجارة";
                            g.DrawString(comp, fTitle, Brushes.White, new RectangleF(20, 18, width - 40, 32), SfCenter);
                            g.DrawString("📋 كشف حساب مالي تفصيلي للمعاملات والمديونية", fSub, Brushes.LightCyan, new RectangleF(20, 50, width - 40, 24), SfCenter);

                            int y = 96;

                            // Meta Card
                            g.FillRectangle(brAlt, 20, y, 840, 58);
                            g.DrawRectangle(penGrid, 20, y, 840, 58);
                            g.DrawLine(penGrid, width / 2, y, width / 2, y + 58);

                            g.DrawString($"👤 العميل: {clientName}" + (!string.IsNullOrWhiteSpace(phone) ? $" | 📱 {phone}" : ""), fBold, brNavy, new RectangleF(width / 2 + 10, y + 6, width / 2 - 30, 24), SfRtlRight);
                            g.DrawString($"📅 الفترة: من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}", fNorm, Brushes.DarkSlateGray, new RectangleF(width / 2 + 10, y + 30, width / 2 - 30, 22), SfRtlRight);

                            g.DrawString($"🕒 تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", fSmall, Brushes.Gray, new RectangleF(30, y + 6, width / 2 - 50, 22), SfRtlRight);
                            if (totalPages > 1)
                            {
                                g.DrawString($"📄 صفحة {pageIdx + 1} من {totalPages}", fBold, brNavy, new RectangleF(30, y + 30, width / 2 - 50, 22), SfRtlRight);
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
                                if (alt) g.FillRectangle(brAlt, tLeft, y, 840, rowH);
                                g.DrawRectangle(penGrid, tLeft, y, 840, rowH);

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

                                    g.DrawString(rowData[c], font, brush, new RectangleF(curX + 3, y + 4, colW[c] - 6, rowH - 6), sf);
                                    if (c > 0) g.DrawLine(penGrid, curX, y, curX, y + rowH);
                                }

                                y += rowH;
                                alt = !alt;
                            }

                            // Summary Box (Only on Last Page)
                            if (pageIdx == totalPages - 1)
                            {
                                y += 12;
                                g.FillRectangle(brAlt, tLeft, y, 840, 78);
                                g.DrawRectangle(penBorder, tLeft, y, 840, 78);

                                g.DrawString($"🔴 إجمالي المبيعات: {totalSales:N2} ج", fBold, brRed, new RectangleF(tLeft + 560, y + 8, 260, 26), SfRtlRight);
                                g.DrawString($"🟢 إجمالي التحصيلات: {totalPayments:N2} ج" + (totalReturns > 0 ? $" | 🔄 مرتجع: {totalReturns:N2} ج" : ""), fBold, brGreen, new RectangleF(tLeft + 560, y + 42, 260, 26), SfRtlRight);

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
                            g.DrawString("شكراً لتعاملكم ونتمنى لكم دوام التوفيق والنجاح 🙏", fSmall, Brushes.Gray, new RectangleF(20, y + 4, 840, 20), SfCenter);
                        }
                    }
                    pages.Add(bmp);
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
                int[] colW = { 40, 130, 105, 240, 105, 105, 115 };
                string[] colHeaders = { "م", "التاريخ والوقت", "نوع الحركة", "تفاصيل البيان / الفاتورة", "مدين (سداد)", "دائن (مشتريات)", "الرصيد (ج)" };

                for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
                {
                    int startRow = pageIdx * 25;
                    int takeCount = pageIdx == 0 && totalPages > 1 ? 25 : (totalRowCount - startRow);
                    if (takeCount < 0) takeCount = 0;

                    int pageRowCount = takeCount;
                    int rowH = 30;
                    int dynamicHeight = 220 + (pageRowCount * rowH) + (pageIdx == totalPages - 1 ? 160 : 70);
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
                        using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                        using (var fSub = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                        using (var fBold = new Font("Segoe UI", 10f, FontStyle.Bold))
                        using (var fNorm = new Font("Segoe UI", 9.5f))
                        using (var fSmall = new Font("Segoe UI", 8.5f))
                        {
                            g.DrawRectangle(penBorder, 10, 10, width - 20, dynamicHeight - 20);

                            g.FillRectangle(brHeader, 12, 12, width - 24, 75);
                            string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة للأنظمة والتجارة";
                            g.DrawString(comp, fTitle, Brushes.White, new RectangleF(20, 18, width - 40, 32), SfCenter);
                            g.DrawString("📋 كشف حساب مالي تفصيلي للمورد", fSub, Brushes.LightYellow, new RectangleF(20, 50, width - 40, 24), SfCenter);

                            int y = 96;

                            // Meta Card
                            g.FillRectangle(brAlt, 20, y, 840, 58);
                            g.DrawRectangle(penGrid, 20, y, 840, 58);
                            g.DrawLine(penGrid, width / 2, y, width / 2, y + 58);

                            g.DrawString($"👤 المورد: {supplierName}" + (!string.IsNullOrWhiteSpace(phone) ? $" | 📱 {phone}" : ""), fBold, brAmber, new RectangleF(width / 2 + 10, y + 6, width / 2 - 30, 24), SfRtlRight);
                            g.DrawString($"📅 الفترة: من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}", fNorm, Brushes.DarkSlateGray, new RectangleF(width / 2 + 10, y + 30, width / 2 - 30, 22), SfRtlRight);

                            g.DrawString($"🕒 تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", fSmall, Brushes.Gray, new RectangleF(30, y + 6, width / 2 - 50, 22), SfRtlRight);
                            if (totalPages > 1)
                            {
                                g.DrawString($"📄 صفحة {pageIdx + 1} من {totalPages}", fBold, brAmber, new RectangleF(30, y + 30, width / 2 - 50, 22), SfRtlRight);
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
                                if (alt) g.FillRectangle(brAlt, tLeft, y, 840, rowH);
                                g.DrawRectangle(penGrid, tLeft, y, 840, rowH);

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

                                    g.DrawString(rowData[c], font, brush, new RectangleF(curX + 3, y + 4, colW[c] - 6, rowH - 6), sf);
                                    if (c > 0) g.DrawLine(penGrid, curX, y, curX, y + rowH);
                                }

                                y += rowH;
                                alt = !alt;
                            }

                            // Summary Box (Only on Last Page)
                            if (pageIdx == totalPages - 1)
                            {
                                y += 12;
                                g.FillRectangle(brAlt, tLeft, y, 840, 78);
                                g.DrawRectangle(penBorder, tLeft, y, 840, 78);

                                g.DrawString($"📥 إجمالي المشتريات: {totalPurchases:N2} ج", fBold, brRed, new RectangleF(tLeft + 560, y + 8, 260, 26), SfRtlRight);
                                g.DrawString($"📤 إجمالي المسدد: {totalPayments:N2} ج", fBold, brGreen, new RectangleF(tLeft + 560, y + 42, 260, 26), SfRtlRight);

                                string balTitle = balance > 0 ? "الرصيد المستحق للمورد:" : (balance < 0 ? "رصيد دائن لصالحنا:" : "الرصيد النهائي:");
                                Brush balBr = balance > 0 ? brRed : brGreen;
                                using (var fBalNum = new Font("Segoe UI", 15f, FontStyle.Bold))
                                {
                                    g.DrawString(balTitle, fBold, balBr, new RectangleF(tLeft + 20, y + 8, 520, 24), SfRtlLeft);
                                    g.DrawString($"{Math.Abs(balance):N2} جنيه مصري", fBalNum, balBr, new RectangleF(tLeft + 20, y + 36, 520, 36), SfRtlLeft);
                                }

                                y += 88;
                            }

                            g.DrawString("مع تحيات إدارة الحسابات 🙏", fSmall, Brushes.Gray, new RectangleF(20, y + 4, 840, 20), SfCenter);
                        }
                    }
                    pages.Add(bmp);
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
                        g.DrawString("📋 بيان أسعار وعرض أسعار معتمد", fSub, brAccent, new RectangleF(15, y, width - 30, 22), SfCenter);
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
                            g.DrawString($"📝 ملاحظات: {notes}", fSmall, Brushes.DarkSlateGray, new RectangleF(tLeft + 10, y + 3, tWidth - 20, 20), SfRtlRight);
                            y += 28;
                        }

                        // Disclaimer
                        g.DrawString("📌 بيان تقديري استرشادي - الأسعار سارية حتى انتهاء الكميات أو تحديث الأسعار", fSmall, Brushes.Gray, new RectangleF(tLeft, y, tWidth, 20), SfCenter);
                        y += 22;

                        // Signatures
                        g.DrawString("توقيع المستلم: ...............................", fSmall, Brushes.Black, new RectangleF(30, y, 220, 20), SfRtlRight);
                        g.DrawString("توقيع المسؤول: ...............................", fSmall, Brushes.Black, new RectangleF(width - 250, y, 220, 20), SfRtlRight);
                        y += 26;

                        // Separator & Footer
                        g.DrawLine(pAccent, 20, y, width - 20, y);
                        y += 6;
                        g.DrawString("🙏 شكراً لتعاملكم معنا ونتشرف بخدمتكم دائماً", fSub, brPrimary, new RectangleF(0, y, width, 22), SfCenter);
                        using (var fPromo = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                        {
                            g.DrawString("✨ تم إنشاء هذا المستند آلياً بواسطة Pro System لإدارة المبيعات والتوزيع", fPromo, Brushes.Gray, new RectangleF(0, y + 22, width, 18), SfCenter);
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
                            g.DrawString($"📋 بيان أسعار وعرض أسعار معتمد (صفحة {pageIdx + 1} من {totalPages})", fSub, brAccent, new RectangleF(15, y, width - 30, 22), SfCenter);
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
                                    g.DrawString($"📝 ملاحظات: {notes}", fSmall, Brushes.DarkSlateGray, new RectangleF(tLeft + 10, y + 3, tWidth - 20, 20), SfRtlRight);
                                    y += 28;
                                }

                                g.DrawString("📌 بيان تقديري استرشادي - الأسعار سارية حتى انتهاء الكميات أو تحديث الأسعار", fSmall, Brushes.Gray, new RectangleF(tLeft, y, tWidth, 20), SfCenter);
                                y += 22;

                                g.DrawString("توقيع المستلم: ...............................", fSmall, Brushes.Black, new RectangleF(30, y, 220, 20), SfRtlRight);
                                g.DrawString("توقيع المسؤول: ...............................", fSmall, Brushes.Black, new RectangleF(width - 250, y, 220, 20), SfRtlRight);
                                y += 26;

                                g.DrawLine(pAccent, 20, y, width - 20, y);
                                y += 6;
                                g.DrawString("🙏 شكراً لتعاملكم معنا ونتشرف بخدمتكم دائماً", fSub, brPrimary, new RectangleF(0, y, width, 22), SfCenter);
                                using (var fPromo = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                                {
                                    g.DrawString("✨ تم إنشاء هذا المستند آلياً بواسطة Pro System لإدارة المبيعات والتوزيع", fPromo, Brushes.Gray, new RectangleF(0, y + 22, width, 18), SfCenter);
                                }
                            }
                            else
                            {
                                y += 14;
                                g.DrawString("📄 يتبع باقي بنود عرض الأسعار في الصفحة التالية ◀", fBold, brPrimary, new RectangleF(20, y, width - 40, 24), SfCenter);
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

        private static string CleanEmoji(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(input, @"\p{Cs}|\p{So}|\p{Sk}|\p{Cn}", "");
        }
    }
}

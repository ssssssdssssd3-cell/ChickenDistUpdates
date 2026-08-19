using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// محرك طباعة تقارير إغلاق الوردية المتطور — تقسيم شبكي محاسبي وجداول أنيقة بنموذجي A4 والريسيت الحراري 80mm
    /// </summary>
    public class FrmPrintShift
    {
        private int _shiftID;
        private string _printFormat; // "Receipt" or "A4"
        private bool _showPreview;

        // بيانات الوردية المحملة
        private DataRow _shiftRow;
        private DataTable _movementsDt;
        private string _openedByName = "---";
        private string _closedByName = "---";
        private string _safeName = "درج الكاشير";
        private string _targetSafeName = "---";
        private string _posStationName = "POS-01";
        private string _branchName = "الفرع الرئيسي";
        private DateTime _openTime = DateTime.Today;
        private DateTime _closeTime = DateTime.Now;
        private string _status = "Open";
        private string _approvalStatus = "PendingApproval";
        private string _approvedByName = "---";
        private DateTime? _approvalTime = null;

        private decimal _openingCash = 0;
        private int _invoiceCount = 0;
        private decimal _totalSales = 0;
        private decimal _totalDiscounts = 0;
        private decimal _netSales = 0;
        private decimal _cashSales = 0;
        private decimal _visaSales = 0;
        private decimal _walletSales = 0;
        private decimal _creditSales = 0;
        private decimal _otherSales = 0;
        private decimal _totalReturns = 0;
        private decimal _cashReturns = 0;
        private decimal _totalExpenses = 0;
        private decimal _totalCollections = 0;
        private decimal _expectedCash = 0;
        private decimal _actualCash = 0;
        private decimal _difference = 0;
        private decimal _transferredAmount = 0;
        private decimal _remainingInDrawer = 0;
        private string _deficitReason = "";
        private string _denominationsJson = "";
        private string _notes = "";

        public FrmPrintShift(int shiftID, string format = "A4", bool showPreview = true)
        {
            _shiftID = shiftID;
            _printFormat = string.Equals(format, "Receipt", StringComparison.OrdinalIgnoreCase) ? "Receipt" : "A4";
            _showPreview = showPreview;

            LoadData();
            DoPrint();
        }

        /// <summary>
        /// عرض قائمة خيارات الطباعة للوردية (ريسيت حراري / A4 شبكي / معاينة / واتساب)
        /// </summary>
        public static void ShowPrintOptions(int shiftID, Control anchorControl = null)
        {
            if (shiftID <= 0) return;

            var menu = new ContextMenuStrip
            {
                RightToLeft = RightToLeft.Yes,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var itemReceiptDirect = new ToolStripMenuItem("🖨️ طباعة ريسيت حراري (Thermal 80mm) - مباشر", null, (s, e) =>
            {
                new FrmPrintShift(shiftID, "Receipt", showPreview: false);
            });

            var itemReceiptPreview = new ToolStripMenuItem("🔍 معاينة ريسيت حراري (Thermal 80mm)", null, (s, e) =>
            {
                new FrmPrintShift(shiftID, "Receipt", showPreview: true);
            });

            var itemA4Direct = new ToolStripMenuItem("📄 طباعة تقرير ورق (A4) - شبكي وجداول احترافية - مباشر", null, (s, e) =>
            {
                new FrmPrintShift(shiftID, "A4", showPreview: false);
            });

            var itemA4Preview = new ToolStripMenuItem("🔍 معاينة تقرير ورق (A4) - شبكي وجداول احترافية", null, (s, e) =>
            {
                new FrmPrintShift(shiftID, "A4", showPreview: true);
            });

            var itemWhatsApp = new ToolStripMenuItem("📲 إرسال تقرير الوردية واتساب", null, (s, e) =>
            {
                SendShiftWhatsApp(shiftID, anchorControl?.FindForm());
            });

            menu.Items.Add(itemA4Preview);
            menu.Items.Add(itemA4Direct);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemReceiptPreview);
            menu.Items.Add(itemReceiptDirect);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemWhatsApp);

            if (anchorControl != null && anchorControl.IsHandleCreated)
            {
                menu.Show(anchorControl, new Point(0, anchorControl.Height));
            }
            else
            {
                menu.Show(Cursor.Position);
            }
        }

        private void LoadData()
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT s.*, 
                           ISNULL(eOpen.EmpName, N'---') AS OpenedByName,
                           ISNULL(eClose.EmpName, N'---') AS ClosedByName,
                           ISNULL(sa.AccountName, N'درج الكاشير') AS SafeName,
                           ISNULL(st.AccountName, N'---') AS TargetSafeName
                    FROM Shifts s
                    LEFT JOIN Employees eOpen ON s.OpenedBy = eOpen.EmpID
                    LEFT JOIN Employees eClose ON s.ClosedBy = eClose.EmpID
                    LEFT JOIN SafeAccounts sa ON s.SafeAccountID = sa.AccountID
                    LEFT JOIN SafeAccounts st ON s.TransferToSafeID = st.AccountID
                    WHERE s.ShiftID = @id", DbHelper.P("@id", _shiftID));

                if (dt.Rows.Count > 0)
                {
                    _shiftRow = dt.Rows[0];
                    _openedByName = _shiftRow["OpenedByName"].ToString();
                    _closedByName = _shiftRow["ClosedByName"].ToString();
                    _safeName = _shiftRow["SafeName"].ToString();
                    _targetSafeName = _shiftRow["TargetSafeName"].ToString();
                    _openTime = Convert.ToDateTime(_shiftRow["OpenTime"]);
                    _closeTime = _shiftRow["CloseTime"] != DBNull.Value ? Convert.ToDateTime(_shiftRow["CloseTime"]) : DateTime.Now;
                    _status = _shiftRow["Status"]?.ToString() ?? "Open";
                    _notes = _shiftRow["Notes"]?.ToString() ?? "";

                    if (_shiftRow.Table.Columns.Contains("POSStationName") && _shiftRow["POSStationName"] != DBNull.Value) _posStationName = _shiftRow["POSStationName"].ToString();
                    if (_shiftRow.Table.Columns.Contains("BranchName") && _shiftRow["BranchName"] != DBNull.Value) _branchName = _shiftRow["BranchName"].ToString();
                    if (_shiftRow.Table.Columns.Contains("ApprovalStatus") && _shiftRow["ApprovalStatus"] != DBNull.Value) _approvalStatus = _shiftRow["ApprovalStatus"].ToString();
                    if (_shiftRow.Table.Columns.Contains("ApprovedByName") && _shiftRow["ApprovedByName"] != DBNull.Value) _approvedByName = _shiftRow["ApprovedByName"].ToString();
                    if (_shiftRow.Table.Columns.Contains("ApprovalTime") && _shiftRow["ApprovalTime"] != DBNull.Value) _approvalTime = Convert.ToDateTime(_shiftRow["ApprovalTime"]);
                    if (_shiftRow.Table.Columns.Contains("DeficitReason") && _shiftRow["DeficitReason"] != DBNull.Value) _deficitReason = _shiftRow["DeficitReason"].ToString();
                    if (_shiftRow.Table.Columns.Contains("DenominationsJson") && _shiftRow["DenominationsJson"] != DBNull.Value) _denominationsJson = _shiftRow["DenominationsJson"].ToString();

                    if (_shiftRow["OpeningCash"] != DBNull.Value) _openingCash = Convert.ToDecimal(_shiftRow["OpeningCash"]);
                    if (_shiftRow.Table.Columns.Contains("InvoiceCount") && _shiftRow["InvoiceCount"] != DBNull.Value) _invoiceCount = Convert.ToInt32(_shiftRow["InvoiceCount"]);
                    if (_shiftRow["TotalSales"] != DBNull.Value) _totalSales = Convert.ToDecimal(_shiftRow["TotalSales"]);
                    if (_shiftRow.Table.Columns.Contains("TotalDiscounts") && _shiftRow["TotalDiscounts"] != DBNull.Value) _totalDiscounts = Convert.ToDecimal(_shiftRow["TotalDiscounts"]);
                    if (_shiftRow.Table.Columns.Contains("NetSales") && _shiftRow["NetSales"] != DBNull.Value) _netSales = Convert.ToDecimal(_shiftRow["NetSales"]);
                    if (_shiftRow["CashSales"] != DBNull.Value) _cashSales = Convert.ToDecimal(_shiftRow["CashSales"]);
                    if (_shiftRow["VisaSales"] != DBNull.Value) _visaSales = Convert.ToDecimal(_shiftRow["VisaSales"]);
                    if (_shiftRow.Table.Columns.Contains("WalletSales") && _shiftRow["WalletSales"] != DBNull.Value) _walletSales = Convert.ToDecimal(_shiftRow["WalletSales"]);
                    if (_shiftRow.Table.Columns.Contains("CreditSales") && _shiftRow["CreditSales"] != DBNull.Value) _creditSales = Convert.ToDecimal(_shiftRow["CreditSales"]);
                    if (_shiftRow["OtherSales"] != DBNull.Value) _otherSales = Convert.ToDecimal(_shiftRow["OtherSales"]);
                    if (_shiftRow["TotalReturns"] != DBNull.Value) _totalReturns = Convert.ToDecimal(_shiftRow["TotalReturns"]);
                    if (_shiftRow.Table.Columns.Contains("CashReturns") && _shiftRow["CashReturns"] != DBNull.Value) _cashReturns = Convert.ToDecimal(_shiftRow["CashReturns"]);
                    if (_shiftRow["ExpectedCash"] != DBNull.Value) _expectedCash = Convert.ToDecimal(_shiftRow["ExpectedCash"]);
                    if (_shiftRow["ActualCash"] != DBNull.Value) _actualCash = Convert.ToDecimal(_shiftRow["ActualCash"]);
                    if (_shiftRow["Difference"] != DBNull.Value) _difference = Convert.ToDecimal(_shiftRow["Difference"]);
                    if (_shiftRow["TransferredAmount"] != DBNull.Value) _transferredAmount = Convert.ToDecimal(_shiftRow["TransferredAmount"]);
                    if (_shiftRow["RemainingInDrawer"] != DBNull.Value) _remainingInDrawer = Convert.ToDecimal(_shiftRow["RemainingInDrawer"]);
                }

                int drawerSafeID = _shiftRow != null && _shiftRow["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(_shiftRow["SafeAccountID"]) : 1;

                // المصروفات والتوريدات
                var dtExp = DbHelper.Query(@"
                    SELECT 
                        ISNULL(SUM(AmountOut), 0) AS TotalExpenses,
                        ISNULL(SUM(AmountIn), 0) AS TotalCashIn
                    FROM CashBox 
                    WHERE (ShiftID = @sid OR (ShiftID IS NULL AND TransDate >= @dt))
                      AND (AccountID = @accId OR AccountID = 1 OR AccountID IS NULL OR @accId = 0)
                      AND TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')",
                    DbHelper.P("@sid", _shiftID),
                    DbHelper.P("@dt", _openTime),
                    DbHelper.P("@accId", drawerSafeID));

                if (dtExp.Rows.Count > 0)
                {
                    _totalExpenses = Convert.ToDecimal(dtExp.Rows[0]["TotalExpenses"]);
                    _totalCollections = Convert.ToDecimal(dtExp.Rows[0]["TotalCashIn"]);
                }

                // لو الوردية مفتوحة أو أرقام المبيعات لم تُخزن بعد
                if (_status == "Open" || _totalSales == 0)
                {
                    var dtSales = DbHelper.Query(@"
                        SELECT
                            COUNT(SaleID) AS InvoiceCount,
                            ISNULL(SUM(TotalAmount), 0) AS TotalSales,
                            ISNULL(SUM(DiscountAmount), 0) AS TotalDiscounts,
                            ISNULL(SUM(CASE WHEN SaleType = 'Cash' THEN ISNULL(CashPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(CashPaid, 0) ELSE 0 END), 0) AS CashSales,
                            ISNULL(SUM(CASE WHEN SaleType = 'Visa' THEN ISNULL(VisaPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(VisaPaid, 0) ELSE 0 END), 0) AS VisaSales,
                            ISNULL(SUM(CASE WHEN SaleType IN ('Wallet','Instapay','VodafoneCash') THEN TotalAmount ELSE 0 END), 0) AS WalletSales,
                            ISNULL(SUM(CASE WHEN SaleType = 'Credit' THEN (TotalAmount - ISNULL(CashPaid, 0) - ISNULL(VisaPaid, 0)) WHEN SaleType = 'Mixed' THEN (TotalAmount - ISNULL(CashPaid, 0) - ISNULL(VisaPaid, 0)) ELSE 0 END), 0) AS CreditSales,
                            ISNULL(SUM(CASE WHEN SaleType NOT IN ('Cash','Credit','Visa','Mixed','Wallet','Instapay','VodafoneCash') THEN TotalAmount ELSE 0 END), 0) AS OtherSales
                        FROM Sales 
                        WHERE (ShiftID = @sid OR (ShiftID IS NULL AND SaleDate >= @dt)) AND IsPosted = 1",
                        DbHelper.P("@sid", _shiftID), DbHelper.P("@dt", _openTime));

                    if (dtSales.Rows.Count > 0)
                    {
                        _invoiceCount = Convert.ToInt32(dtSales.Rows[0]["InvoiceCount"]);
                        _totalSales = Convert.ToDecimal(dtSales.Rows[0]["TotalSales"]);
                        _totalDiscounts = Convert.ToDecimal(dtSales.Rows[0]["TotalDiscounts"]);
                        _cashSales = Convert.ToDecimal(dtSales.Rows[0]["CashSales"]);
                        _visaSales = Convert.ToDecimal(dtSales.Rows[0]["VisaSales"]);
                        _walletSales = Convert.ToDecimal(dtSales.Rows[0]["WalletSales"]);
                        _creditSales = Convert.ToDecimal(dtSales.Rows[0]["CreditSales"]);
                        _otherSales = Convert.ToDecimal(dtSales.Rows[0]["OtherSales"]);
                        _netSales = _totalSales - _totalReturns - _totalDiscounts;
                    }

                    var dtR = DbHelper.Query(@"
                        SELECT 
                            ISNULL(SUM(sr.TotalAmount), 0) AS TotalReturns,
                            ISNULL(SUM(CASE WHEN s.SaleType IN ('Cash','Mixed') THEN sr.TotalAmount ELSE 0 END), 0) AS CashReturns
                        FROM SalesReturns sr
                        JOIN Sales s ON sr.SaleID = s.SaleID
                        WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt))",
                        DbHelper.P("@sid", _shiftID), DbHelper.P("@dt", _openTime));

                    if (dtR.Rows.Count > 0)
                    {
                        _totalReturns = Convert.ToDecimal(dtR.Rows[0]["TotalReturns"]);
                        _cashReturns = Convert.ToDecimal(dtR.Rows[0]["CashReturns"]);
                        if (_cashReturns == 0 && _totalReturns > 0) _cashReturns = _totalReturns;
                    }

                    _expectedCash = _openingCash + _cashSales + _totalCollections - _cashReturns - _totalExpenses;
                    if (_actualCash == 0) _actualCash = _expectedCash;
                    _difference = _actualCash - _expectedCash;
                }

                // الحركات
                _movementsDt = DbHelper.Query(@"
                    SELECT 
                        s.SaleDate AS TransTime,
                        CASE WHEN s.SaleType = 'Cash' THEN N'مبيعات نقدي' WHEN s.SaleType = 'Visa' THEN N'مبيعات فيزا' ELSE N'مبيعات' END AS TransType,
                        s.SaleCode AS RefCode,
                        ISNULL(c.ClientName, N'عميل نقدي') AS Details,
                        CASE WHEN s.SaleType = 'Cash' THEN s.TotalAmount ELSE 0 END AS Amount
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt)) AND s.IsPosted = 1
                    UNION ALL
                    SELECT sr.ReturnDate AS TransTime, 'مرتجع' AS TransType, CAST(sr.ReturnID AS NVARCHAR) AS RefCode, 'مرتجع مبيعات' AS Details, -sr.TotalAmount AS Amount
                    FROM SalesReturns sr JOIN Sales s ON sr.SaleID=s.SaleID WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt))
                    UNION ALL
                    SELECT 
                        cb.TransDate AS TransTime,
                        CASE WHEN cb.AmountIn > 0 THEN N'وارد للدرج' ELSE N'مصروفات' END AS TransType,
                        CAST(cb.CashID AS NVARCHAR) AS RefCode,
                        ISNULL(cb.Notes, N'حركة نقدية') AS Details,
                        CASE WHEN cb.AmountIn > 0 THEN cb.AmountIn ELSE -cb.AmountOut END AS Amount
                    FROM CashBox cb WHERE cb.TransDate >= @dt AND cb.TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')
                    ORDER BY TransTime DESC",
                    DbHelper.P("@sid", _shiftID), DbHelper.P("@dt", _openTime));
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPrintShift.LoadData", ex);
            }
        }

        private void DoPrint()
        {
            var pd = new PrintDocument();
            bool isReceipt = string.Equals(_printFormat, "Receipt", StringComparison.OrdinalIgnoreCase);

            if (isReceipt)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 310, 1400);
                pd.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);
                if (!string.IsNullOrEmpty(AppConfig.ReceiptPrinterName))
                    AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
            }
            else
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
                if (!string.IsNullOrEmpty(AppConfig.A4PrinterName))
                    AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            }

            pd.PrintPage += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                if (isReceipt)
                {
                    DrawReceiptPage(e.Graphics, e.PageBounds);
                }
                else
                {
                    DrawA4Page(e.Graphics, e.PageBounds);
                }
                e.HasMorePages = false;
            };

            if (_showPreview)
            {
                using (var dlg = new PrintPreviewDialog())
                {
                    dlg.Document = pd;
                    dlg.Width = 950;
                    dlg.Height = 780;
                    dlg.StartPosition = FormStartPosition.CenterScreen;
                    dlg.Text = isReceipt ? "معاينة ريسيت إغلاق الوردية" : "معاينة تقرير إغلاق الوردية (A4 شبكي)";
                    try
                    {
                        if (dlg.Controls.Count > 1 && dlg.Controls[1] is ToolStrip ts)
                        {
                            ts.RightToLeft = RightToLeft.Yes;
                        }
                    }
                    catch { }
                    dlg.ShowDialog();
                }
            }
            else
            {
                try
                {
                    pd.Print();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("فشل إرسال التقرير إلى الطابعة:\n" + ex.Message, "خطأ في الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// رسم تقرير إغلاق الوردية بنظام الشبكة والجداول الاحترافية A4
        /// </summary>
        private void DrawA4Page(Graphics g, Rectangle bounds)
        {
            int lMargin = 25;
            int rMargin = 25;
            int topMargin = 25;
            int printableW = bounds.Width - lMargin - rMargin;
            int y = topMargin;

            var fontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
            var fontHeader = new Font("Segoe UI", 11f, FontStyle.Bold);
            var fontSub = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var fontNormal = new Font("Segoe UI", 9f, FontStyle.Regular);
            var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
            var fontSmall = new Font("Segoe UI", 8f, FontStyle.Regular);

            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var sfRight = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
            var sfLeft = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

            Color colPrimary = Color.FromArgb(30, 58, 138);      // أزرق داكن
            Color colSecondary = Color.FromArgb(71, 85, 105);   // رمادي سلايت
            Color colRowAlt = Color.FromArgb(248, 250, 252);     // رمادي ناعم
            Color colBorder = Color.FromArgb(203, 213, 225);     // خط شبكي

            // ── 1. رأس التقرير ─────────────────────────
            int headerH = 75;
            g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), lMargin, y, printableW, headerH);
            g.DrawRectangle(new Pen(colBorder, 1.2f), lMargin, y, printableW, headerH);

            string compName = string.IsNullOrEmpty(AppConfig.CompanyName) ? "شركة برو سوفت للأنظمة المتكاملة" : AppConfig.CompanyName;
            g.DrawString(compName, fontTitle, new SolidBrush(colPrimary), new RectangleF(lMargin + 15, y + 10, printableW - 30, 28), sfRight);
            
            string subHeader = $"الفرع: {_branchName}  •  جهاز الكاشير: {_posStationName}  •  تاريخ وتوقيت الطباعة: {DateTime.Now:yyyy-MM-dd   hh:mm tt}";
            g.DrawString(subHeader, fontSmall, new SolidBrush(colSecondary), new RectangleF(lMargin + 15, y + 42, printableW - 30, 20), sfRight);

            y += headerH + 8;

            // ── 2. شريط عنوان التقرير ───────────────
            int ribbonH = 34;
            g.FillRectangle(new SolidBrush(colPrimary), lMargin, y, printableW, ribbonH);
            string approvalLabel = _approvalStatus == "Approved" ? " [معتمدة ومصادقة ✔]" : (_approvalStatus == "Rejected" ? " [مرفوضة للمراجعة 🔴]" : " [بانتظار الاعتماد ⏳]");
            g.DrawString($"📊 تقرير إغلاق الوردية المحاسبي — وردية رقم #{_shiftID}{approvalLabel}", fontHeader, Brushes.White, new RectangleF(lMargin, y, printableW, ribbonH), sfCenter);

            y += ribbonH + 8;

            // ── 3. كارت معلومات الوردية والمسؤولين (شبكة 2 × 4) ───
            int metaH = 58;
            g.FillRectangle(Brushes.White, lMargin, y, printableW, metaH);
            g.DrawRectangle(new Pen(colBorder, 1.2f), lMargin, y, printableW, metaH);

            int colW = printableW / 4;
            int rowH = metaH / 2;

            for (int i = 1; i < 4; i++)
            {
                g.DrawLine(new Pen(colBorder, 1f), lMargin + (i * colW), y, lMargin + (i * colW), y + metaH);
            }
            g.DrawLine(new Pen(colBorder, 1f), lMargin, y + rowH, lMargin + printableW, y + rowH);

            void DrawMetaCell(int col, int row, string label, string val, bool isHighlighted = false)
            {
                int cx = lMargin + (3 - col) * colW;
                int cy = y + row * rowH;
                var rect = new RectangleF(cx + 6, cy + 2, colW - 12, rowH - 4);
                string txt = $"{label}:  {val}";
                g.DrawString(txt, isHighlighted ? fontBold : fontNormal, new SolidBrush(isHighlighted ? colPrimary : Color.Black), rect, sfRight);
            }

            DrawMetaCell(0, 0, "رقم الوردية", $"#{_shiftID}", true);
            DrawMetaCell(1, 0, "الكاشير", _openedByName, true);
            DrawMetaCell(2, 0, "درج النقدية", _safeName);
            DrawMetaCell(3, 0, "الخزنة المستهدفة", string.IsNullOrEmpty(_targetSafeName) ? "---" : _targetSafeName);

            DrawMetaCell(0, 1, "وقت الفتح", _openTime.ToString("yyyy-MM-dd hh:mm tt"));
            DrawMetaCell(1, 1, "وقت الإغلاق", _closeTime.ToString("yyyy-MM-dd hh:mm tt"));
            DrawMetaCell(2, 1, "مسؤول الإغلاق", _closedByName);
            DrawMetaCell(3, 1, "عدد الفواتير", _invoiceCount > 0 ? $"{_invoiceCount} فاتورة" : "---");

            y += metaH + 10;

            // ── 4. جدول التقسيم الشبكي المالي (Financial Summary Grid) ───
            int tableRowH = 25;
            int thH = 28;

            int wSeq = 35;
            int wLabel = 235;
            int wAmount = 130;
            int wNotes = printableW - (wSeq + wLabel + wAmount);

            g.FillRectangle(new SolidBrush(colSecondary), lMargin, y, printableW, thH);
            g.DrawRectangle(new Pen(colBorder, 1.2f), lMargin, y, printableW, thH);

            int curX = lMargin + printableW;
            void DrawThCell(string txt, int w, StringFormat sf)
            {
                curX -= w;
                g.DrawString(txt, fontSub, Brushes.White, new RectangleF(curX, y, w, thH), sf);
                g.DrawLine(new Pen(Color.FromArgb(148, 163, 184)), curX, y, curX, y + thH);
            }

            DrawThCell("م", wSeq, sfCenter);
            DrawThCell("البيان المالي المحاسبي", wLabel, sfRight);
            DrawThCell("المبلغ (ج.م)", wAmount, sfCenter);
            DrawThCell("التفاصيل والبيان الإيضاحي للمطابقة", wNotes, sfRight);

            y += thH;

            var financialRows = new List<(string seq, string title, decimal amt, string note, Color col, bool bold, Color bg)>
            {
                ("1", "رصيد بداية الوردية (الافتتاحي)", _openingCash, "النقدية المسجلة بالدرج عند بدء الوردية", Color.Black, false, Color.White),
                ("2", "مبيعات نقدية (كاش الدرج) 🛒", _cashSales, "السيولة النقدية المحصلة بالدرج من المبيعات", Color.FromArgb(15, 118, 110), true, colRowAlt),
                ("3", "مبيعات فيزا وماكينات إلكترونية 💳", _visaSales, "إيرادات بحسابات وماكينات الفيزا (لا تدخل بسيولة الدرج)", Color.FromArgb(109, 40, 217), false, Color.White),
                ("4", "مبيعات محافظ إلكترونية (إنستاباي/فودافون) 📱", _walletSales, "إيرادات بالمحافظ الإلكترونية (لا تدخل بسيولة الدرج)", Color.FromArgb(0, 168, 232), false, colRowAlt),
                ("5", "مبيعات آجل / عملاء 📑", (_creditSales + _otherSales), "مبيعات ذمم وعملاء آجل", Color.FromArgb(52, 152, 219), false, Color.White),
                ("6", "إجمالي مبيعات الوردية الكلي", _totalSales, $"الفواتير: {_invoiceCount} | الخصومات: {_totalDiscounts:N2} ج | الصافي: {_netSales:N2} ج", Color.FromArgb(30, 58, 138), true, Color.FromArgb(241, 245, 249)),
                ("7", "توريدات وإيداعات وتحصيل للدرج ➕", _totalCollections, "سندات قبض وتحويلات نقدية واردة للدرج", _totalCollections > 0 ? Color.FromArgb(21, 128, 61) : Color.Black, false, Color.White),
                ("8", "مرتجعات مبيعات كاش الدرج ↩ ➖", _cashReturns > 0 ? _cashReturns : _totalReturns, "مرتجع مبيعات نقدية مخصوم من نقدية الدرج", _totalReturns > 0 ? Color.FromArgb(185, 28, 28) : Color.Black, false, colRowAlt),
                ("9", "مصروفات ونثريات وسحوبات من الدرج ➖", _totalExpenses, "نثريات ومصروفات وسندات صرف مسحوبة من الدرج", _totalExpenses > 0 ? Color.FromArgb(185, 28, 28) : Color.Black, false, Color.White),
                ("10", "النقدية المتوقعة بالدرج (السيولة الواجبة)", _expectedCash, "الرصيد الواجب توفره بالدرج (الافتتاحي + الكاش + التوريدات - المرتجع - المصروف)", colPrimary, true, Color.FromArgb(238, 242, 255)),
                ("11", "النقدية الفعلية المحصورة بالدرج", _actualCash, "المبلغ الفعلي المعدود بمعرفة الكاشير", Color.FromArgb(30, 64, 175), true, Color.FromArgb(240, 249, 255)),
                ("12", "الفرق المحاسبي (عجز / زيادة)", _difference, _difference == 0 ? "مطابق تماماً بدون أي فروقات ✔" : (_difference < 0 ? $"عجز نقدي قدره {_difference:N2} ج 🔴" : $"زيادة نقدية قدرها {_difference:N2} ج 🟢"), _difference == 0 ? Color.FromArgb(21, 128, 61) : (_difference < 0 ? Color.FromArgb(220, 38, 38) : Color.FromArgb(217, 119, 6)), true, _difference == 0 ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242))
            };

            foreach (var r in financialRows)
            {
                g.FillRectangle(new SolidBrush(r.bg), lMargin, y, printableW, tableRowH);
                g.DrawRectangle(new Pen(colBorder, 1f), lMargin, y, printableW, tableRowH);

                curX = lMargin + printableW;
                void DrawTd(string txt, int w, StringFormat sf, Color textColor, bool isBold)
                {
                    curX -= w;
                    g.DrawString(txt, isBold ? fontBold : fontNormal, new SolidBrush(textColor), new RectangleF(curX + 4, y, w - 8, tableRowH), sf);
                    g.DrawLine(new Pen(colBorder, 1f), curX, y, curX, y + tableRowH);
                }

                DrawTd(r.seq, wSeq, sfCenter, Color.FromArgb(100, 116, 139), false);
                DrawTd(r.title, wLabel, sfRight, r.col, r.bold);
                DrawTd(r.amt.ToString("N2") + " ج", wAmount, sfCenter, r.col, r.bold);
                DrawTd(r.note, wNotes, sfRight, Color.FromArgb(51, 65, 85), r.bold);

                y += tableRowH;
            }

            y += 8;

            // ── 5. جدول حركة التوريد والتسوية ──────────────────
            int transH = 26;
            g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), lMargin, y, printableW, transH);
            g.DrawRectangle(new Pen(colBorder, 1.2f), lMargin, y, printableW, transH);

            int halfW = printableW / 2;
            string txtTransfer = $"🏦 المحول للخزنة:  {_transferredAmount:N2} ج   (إلى: {(string.IsNullOrEmpty(_targetSafeName) ? "---" : _targetSafeName)})";
            string txtRem = $"📌 المتبقي بالدرج:  {_remainingInDrawer:N2} ج   (رصيد افتتاحي للوردية القادمة)";

            g.DrawString(txtTransfer, fontBold, new SolidBrush(colPrimary), new RectangleF(lMargin + halfW + 6, y, halfW - 12, transH), sfRight);
            g.DrawLine(new Pen(colBorder, 1.2f), lMargin + halfW, y, lMargin + halfW, y + transH);
            g.DrawString(txtRem, fontBold, new SolidBrush(Color.FromArgb(15, 118, 110)), new RectangleF(lMargin + 6, y, halfW - 12, transH), sfRight);

            y += transH + 8;

            // ── 6. جرد الفئات النقدية وتبرير العجز (إن وجدا) ───────
            string denomsFormatted = FrmCashDenominations.FormatDenominationsForPrint(_denominationsJson);
            if (!string.IsNullOrWhiteSpace(denomsFormatted) || !string.IsNullOrWhiteSpace(_deficitReason))
            {
                int extraH = 46;
                g.FillRectangle(new SolidBrush(Color.FromArgb(254, 252, 232)), lMargin, y, printableW, extraH);
                g.DrawRectangle(new Pen(Color.FromArgb(234, 179, 8), 1f), lMargin, y, printableW, extraH);

                string extraNotes = "";
                if (!string.IsNullOrWhiteSpace(_deficitReason)) extraNotes += $"⚠️ تبرير سبب العجز: {_deficitReason}   ";
                if (!string.IsNullOrWhiteSpace(denomsFormatted)) extraNotes += $"|  🧮 جرد الفئات: {denomsFormatted.Replace("\r\n", "  •  ")}";

                g.DrawString(extraNotes, fontBold, new SolidBrush(Color.FromArgb(113, 63, 18)), new RectangleF(lMargin + 10, y + 4, printableW - 20, extraH - 8), sfRight);
                y += extraH + 8;
            }

            // ── 7. منطقة التوقيعات والاعتماد ─────────
            int footerY = bounds.Height - 110;
            if (y > footerY - 40) footerY = y + 20;

            int sigW = printableW / 3;
            g.DrawLine(new Pen(colBorder, 1.2f), lMargin, footerY, lMargin + printableW, footerY);

            void DrawSigBox(int idx, string title, string subTitle = "")
            {
                int sx = lMargin + (2 - idx) * sigW;
                g.DrawString(title, fontBold, new SolidBrush(colSecondary), new RectangleF(sx, footerY + 6, sigW, 18), sfCenter);
                if (!string.IsNullOrEmpty(subTitle))
                {
                    g.DrawString(subTitle, fontSmall, new SolidBrush(colPrimary), new RectangleF(sx, footerY + 24, sigW, 16), sfCenter);
                }
                g.DrawLine(new Pen(Color.FromArgb(148, 163, 184), 1f) { DashStyle = DashStyle.Dot }, sx + 20, footerY + 65, sx + sigW - 20, footerY + 65);
            }

            DrawSigBox(0, "توقيع الكاشير المسؤول", _openedByName);
            DrawSigBox(1, "توقيع مستلم النقدية", _closedByName);
            DrawSigBox(2, "اعتماد الإدارة والمراجعة", _approvalStatus == "Approved" ? $"معتمد بواسطة: {_approvedByName}" : "في انتظار الاعتماد");

            g.DrawString("تم استخراج هذا التقرير المحاسبي تلقائياً بواسطة نظام برو سوفت لإدارة الشركات والمخازن", fontSmall, new SolidBrush(Color.FromArgb(148, 163, 184)), new RectangleF(lMargin, bounds.Height - 35, printableW, 20), sfCenter);
        }

        /// <summary>
        /// رسم تقرير إغلاق الوردية بتقسيم شبكي مخصص لطابعات الريسيت الحراري (80mm)
        /// </summary>
        private void DrawReceiptPage(Graphics g, Rectangle bounds)
        {
            int lMargin = 8;
            int rMargin = 8;
            int printableW = bounds.Width - lMargin - rMargin;
            int y = 8;

            var fontTitle = new Font("Segoe UI", 12f, FontStyle.Bold);
            var fontSub = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
            var fontNormal = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            var fontSmall = new Font("Segoe UI", 7.5f, FontStyle.Regular);

            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var sfRight = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
            var sfLeft = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

            // 1. رأس الشركة
            string compName = string.IsNullOrEmpty(AppConfig.CompanyName) ? "شركة برو سوفت" : AppConfig.CompanyName;
            g.DrawString(compName, fontTitle, Brushes.Black, new RectangleF(lMargin, y, printableW, 24), sfCenter);
            y += 24;

            g.DrawString($"تقرير إغلاق الوردية #{_shiftID}", fontSub, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), sfCenter);
            y += 20;

            g.DrawLine(new Pen(Color.Black, 1.5f), lMargin, y, lMargin + printableW, y);
            y += 6;

            // 2. بيانات الوردية
            void DrawRecInfo(string label, string val)
            {
                g.DrawString(label, fontBold, Brushes.Black, new RectangleF(lMargin + printableW / 2, y, printableW / 2, 18), sfRight);
                g.DrawString(val, fontNormal, Brushes.Black, new RectangleF(lMargin, y, printableW / 2, 18), sfLeft);
                y += 18;
            }

            DrawRecInfo("الفرع والجهاز:", $"{_branchName} - {_posStationName}");
            DrawRecInfo("الكاشير:", _openedByName);
            DrawRecInfo("وقت الفتح:", _openTime.ToString("dd/MM/yyyy hh:mm tt"));
            DrawRecInfo("وقت الإغلاق:", _closeTime.ToString("dd/MM/yyyy hh:mm tt"));
            DrawRecInfo("عدد الفواتير:", $"{_invoiceCount} فاتورة");

            y += 4;
            g.DrawLine(new Pen(Color.Black, 1.2f), lMargin, y, lMargin + printableW, y);
            y += 6;

            // 3. جدول البيانات المالية
            void DrawRecGridRow(string label, decimal val, bool isBold = false, bool isHighlighted = false)
            {
                int rh = isHighlighted ? 24 : 19;
                if (isHighlighted)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(235, 235, 235)), lMargin, y, printableW, rh);
                    g.DrawRectangle(new Pen(Color.Black, 1f), lMargin, y, printableW, rh);
                }
                else
                {
                    g.DrawLine(new Pen(Color.FromArgb(210, 210, 210), 0.8f), lMargin, y + rh, lMargin + printableW, y + rh);
                }

                g.DrawString(label, isBold ? fontBold : fontNormal, Brushes.Black, new RectangleF(lMargin + 105, y, printableW - 110, rh), sfRight);
                g.DrawString(val.ToString("N2") + " ج", isBold ? fontBold : fontNormal, Brushes.Black, new RectangleF(lMargin + 4, y, 100, rh), sfLeft);
                y += rh + (isHighlighted ? 3 : 1);
            }

            DrawRecGridRow("رصيد بداية الوردية:", _openingCash);
            DrawRecGridRow("إجمالي المبيعات:", _totalSales, true);
            DrawRecGridRow("  • مبيعات كاش بالدرج:", _cashSales);
            DrawRecGridRow("  • مبيعات فيزا (إلكتروني):", _visaSales);
            if (_walletSales > 0) DrawRecGridRow("  • مبيعات محافظ:", _walletSales);
            DrawRecGridRow("  • مبيعات آجل:", _creditSales + _otherSales);
            DrawRecGridRow("توريدات وإيداعات الدرج ➕:", _totalCollections);
            DrawRecGridRow("مرتجعات كاش الدرج ↩ ➖:", _cashReturns > 0 ? _cashReturns : _totalReturns);
            DrawRecGridRow("المصروفات والسحب من الدرج ➖:", _totalExpenses);
            y += 3;

            DrawRecGridRow("المتوقع بالدرج (السيولة):", _expectedCash, true, true);
            DrawRecGridRow("الفعلي بالدرج:", _actualCash, true, true);
            
            string diffTxt = _difference == 0 ? "0.00 ج (مطابق ✔)" : (_difference < 0 ? $"{_difference:N2} ج (عجز 🔴)" : $"+{_difference:N2} ج (زيادة 🟢)");
            int diffRh = 24;
            g.FillRectangle(new SolidBrush(Color.FromArgb(225, 225, 225)), lMargin, y, printableW, diffRh);
            g.DrawRectangle(new Pen(Color.Black, 1.2f), lMargin, y, printableW, diffRh);
            g.DrawString("الفرق المحاسبي:", fontBold, Brushes.Black, new RectangleF(lMargin + 125, y, printableW - 130, diffRh), sfRight);
            g.DrawString(diffTxt, fontBold, Brushes.Black, new RectangleF(lMargin + 4, y, 120, diffRh), sfLeft);
            y += diffRh + 4;

            if (!string.IsNullOrWhiteSpace(_deficitReason))
            {
                g.DrawString($"⚠️ سبب العجز: {_deficitReason}", fontBold, Brushes.Black, new RectangleF(lMargin, y, printableW, 20), sfRight);
                y += 20;
            }

            // 4. التوريد
            if (_transferredAmount > 0 || _remainingInDrawer > 0)
            {
                g.DrawLine(new Pen(Color.Black, 1f), lMargin, y, lMargin + printableW, y);
                y += 4;
                DrawRecInfo("المحول للخزنة:", $"{_transferredAmount:N2} ج");
                DrawRecInfo("المتبقي بالدرج:", $"{_remainingInDrawer:N2} ج");
                y += 4;
            }

            string denomsPrint = FrmCashDenominations.FormatDenominationsForPrint(_denominationsJson);
            if (!string.IsNullOrWhiteSpace(denomsPrint))
            {
                g.DrawLine(new Pen(Color.Black, 1f), lMargin, y, lMargin + printableW, y);
                y += 4;
                g.DrawString("🧮 جرد الفئات النقدية:", fontBold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), sfRight);
                y += 18;
                g.DrawString(denomsPrint, fontSmall, Brushes.Black, new RectangleF(lMargin, y, printableW, 80), sfRight);
                y += 80;
            }

            g.DrawLine(new Pen(Color.Black, 1.5f), lMargin, y, lMargin + printableW, y);
            y += 8;

            g.DrawString($"طُبع بتاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}", fontSmall, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), sfCenter);
            y += 16;
            g.DrawString("نشكركم لاستخدام برنامج برو سوفت", fontSmall, Brushes.Black, new RectangleF(lMargin, y, printableW, 16), sfCenter);
        }

        /// <summary>
        /// إنشاء صورة عالية الجودة لتقرير الوردية لإرسالها للواتساب
        /// </summary>
        public static Bitmap GenerateShiftImage(int shiftID, string format = "A4")
        {
            var printer = new FrmPrintShift(shiftID, format, showPreview: false);
            bool isReceipt = string.Equals(format, "Receipt", StringComparison.OrdinalIgnoreCase);
            int w = isReceipt ? 380 : 1000;
            int h = isReceipt ? 1000 : 1400;

            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                if (isReceipt)
                {
                    printer.DrawReceiptPage(g, new Rectangle(0, 0, w, h));
                }
                else
                {
                    printer.DrawA4Page(g, new Rectangle(0, 0, w, h));
                }
            }
            return bmp;
        }

        /// <summary>
        /// إرسال تقرير الوردية عبر الواتساب بخيارات (صورة / PDF / نص)
        /// </summary>
        public static void SendShiftWhatsApp(int shiftID, Form parentForm = null)
        {
            try
            {
                var printer = new FrmPrintShift(shiftID, "A4", showPreview: false);
                string textMsg = $"📊 *تقرير إغلاق الوردية #{shiftID}*\n" +
                                 $"🏢 {AppConfig.CompanyName}\n" +
                                 $"🏢 الفرع: {printer._branchName} | 💻 جهاز: {printer._posStationName}\n" +
                                 $"👤 الكاشير: {printer._openedByName}\n" +
                                 $"🕒 وقت الفتح: {printer._openTime:yyyy-MM-dd hh:mm tt}\n" +
                                 $"🕒 وقت الإغلاق: {printer._closeTime:yyyy-MM-dd hh:mm tt}\n" +
                                 $"━━━━━━━━━━━━━━\n" +
                                 $"🧾 عدد الفواتير: {printer._invoiceCount}\n" +
                                 $"💰 رصيد الفتح: {printer._openingCash:N2} ج\n" +
                                 $"🛒 إجمالي المبيعات: {printer._totalSales:N2} ج (نقدي: {printer._cashSales:N2} ج | فيزا: {printer._visaSales:N2} ج)\n" +
                                 $"↩️ المرتجعات: {printer._totalReturns:N2} ج\n" +
                                 $"💸 المصروفات: {printer._totalExpenses:N2} ج\n" +
                                 $"━━━━━━━━━━━━━━\n" +
                                 $"💼 المتوقع بالدرج: {printer._expectedCash:N2} ج\n" +
                                 $"💵 الفعلي بالدرج: {printer._actualCash:N2} ج\n" +
                                 $"⚖️ الفرق المحاسبي: {printer._difference:N2} ج\n" +
                                 (!string.IsNullOrWhiteSpace(printer._deficitReason) ? $"⚠️ سبب العجز: {printer._deficitReason}\n" : "") +
                                 $"🏦 المحول للخزنة: {printer._transferredAmount:N2} ج\n" +
                                 $"📌 المتبقي بالدرج: {printer._remainingInDrawer:N2} ج";

                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                    parentForm: parentForm,
                    clientPhone: "",
                    textMessage: textMsg,
                    imageGenerator: () => GenerateShiftImage(shiftID, "A4"),
                    dialogTitle: $"📱 إرسال تقرير وردية #{shiftID} عبر الواتساب"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء إعداد إرسال تقرير الوردية واتساب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

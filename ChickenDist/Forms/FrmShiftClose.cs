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
    /// شاشة إدارة الوردية — فتح وإغلاق وطباعة تقرير الوردية
    /// </summary>
    public class FrmShiftClose : Form
    {
        private DataRow _openShift = null;
        private ShiftSummary _summary = null;
        private Panel   pnlStatus, pnlSummary, pnlBottom;
        private Label   lblShiftStatus, lblShiftInfo;
        private Label   lblTotalSales, lblCashSales, lblVisaSales, lblOtherSales;
        private Label   lblTotalReturns, lblExpected, lblDiff;
        private TextBox txtActualCash, txtNotes, txtOpeningCash;
        private Button  btnOpenShift, btnCloseShift, btnPrintReport, btnRefresh;

        public FrmShiftClose()
        {
            InitUI();
            LoadCurrentShift();
        }

        private void InitUI()
        {
            this.Text            = "إدارة الوردية";
            this.Size            = new Size(860, 680);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = Theme.BgMain;
            this.Font            = Theme.FontMain;
            this.RightToLeft     = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlTop = Theme.MakeTitleBar("🔄 إدارة الوردية", "فتح وإغلاق وطباعة تقرير وردية الكاشير");
            this.Controls.Add(pnlTop);

            pnlStatus = new Panel { Location = new Point(20, 90), Size = new Size(810, 90), BackColor = Theme.BgCard };
            pnlStatus.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlStatus);

            lblShiftStatus = new Label { Font = new Font("Segoe UI", 14f, FontStyle.Bold), Location = new Point(10, 10), Size = new Size(790, 35), TextAlign = ContentAlignment.MiddleCenter };
            lblShiftInfo   = new Label { Font = Theme.FontMain, ForeColor = Theme.TextSub, Location = new Point(10, 50), Size = new Size(790, 25), TextAlign = ContentAlignment.MiddleCenter };
            pnlStatus.Controls.Add(lblShiftStatus);
            pnlStatus.Controls.Add(lblShiftInfo);
            this.Controls.Add(pnlStatus);

            pnlSummary = new Panel { Location = new Point(20, 195), Size = new Size(810, 360), BackColor = Theme.BgCard };
            pnlSummary.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlSummary);

            int y = 15;
            void AddLabelRow(string lText, ref Label valLbl, Color col)
            {
                var l = new Label { Text = lText, Location = new Point(20, y), Size = new Size(210, 28), ForeColor = Theme.TextMain, Font = Theme.FontMain, TextAlign = ContentAlignment.MiddleRight };
                valLbl = new Label { Text = "---", Location = new Point(240, y), Size = new Size(200, 28), ForeColor = col, Font = new Font("Segoe UI", 11f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
                pnlSummary.Controls.Add(l); pnlSummary.Controls.Add(valLbl); y += 38;
            }

            AddLabelRow("إجمالي المبيعات:", ref lblTotalSales, Theme.Success);
            AddLabelRow("منها نقدي:", ref lblCashSales, Theme.TextMain);
            AddLabelRow("منها فيزا/بطاقة:", ref lblVisaSales, Theme.TextMain);
            AddLabelRow("منها آجل/أخرى:", ref lblOtherSales, Theme.TextMain);
            AddLabelRow("إجمالي المرتجعات:", ref lblTotalReturns, Theme.Danger);
            AddLabelRow("المتوقع في الخزنة:", ref lblExpected, Theme.Accent);

            var lAct = new Label { Text = "الفعلي في الخزنة:", Location = new Point(20, y), Size = new Size(210, 28), ForeColor = Theme.TextMain, Font = Theme.FontMain, TextAlign = ContentAlignment.MiddleRight };
            txtActualCash = new TextBox { Location = new Point(240, y), Size = new Size(160, 28), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11f), BorderStyle = BorderStyle.FixedSingle, Text = "0" };
            txtActualCash.TextChanged += (s, e) => RecalcDiff();
            pnlSummary.Controls.Add(lAct); pnlSummary.Controls.Add(txtActualCash); y += 38;

            AddLabelRow("الفرق (عجز/زيادة):", ref lblDiff, Theme.Accent);

            var lNotes = new Label { Text = "ملاحظات:", Location = new Point(20, y), Size = new Size(210, 28), ForeColor = Theme.TextMain, Font = Theme.FontMain, TextAlign = ContentAlignment.MiddleRight };
            txtNotes = new TextBox { Location = new Point(240, y), Size = new Size(360, 28), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, BorderStyle = BorderStyle.FixedSingle };
            pnlSummary.Controls.Add(lNotes); pnlSummary.Controls.Add(txtNotes);

            var lOpen = new Label { Text = "رصيد الفتح:", Location = new Point(460, 15), Size = new Size(150, 28), ForeColor = Theme.TextMain, Font = Theme.FontMain, TextAlign = ContentAlignment.MiddleRight };
            txtOpeningCash = new TextBox { Location = new Point(620, 15), Size = new Size(130, 28), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11f), BorderStyle = BorderStyle.FixedSingle, Text = "0" };
            pnlSummary.Controls.Add(lOpen); pnlSummary.Controls.Add(txtOpeningCash);

            this.Controls.Add(pnlSummary);

            pnlBottom = new Panel { Location = new Point(20, 570), Size = new Size(810, 60), BackColor = Color.Transparent };

            btnOpenShift   = Theme.MakeButton("✅ فتح وردية جديدة", Theme.Success,                    new Point(0,   10), new Size(190, 40));
            btnCloseShift  = Theme.MakeButton("🔒 إغلاق الوردية",   Theme.Danger,                     new Point(200, 10), new Size(190, 40));
            btnPrintReport = Theme.MakeButton("🖨️ طباعة التقرير",   Theme.Primary,                    new Point(400, 10), new Size(180, 40));
            btnRefresh     = Theme.MakeButton("🔄 تحديث",            Color.FromArgb(60, 70, 85),       new Point(590, 10), new Size(120, 40));

            btnOpenShift.Click   += BtnOpenShift_Click;
            btnCloseShift.Click  += BtnCloseShift_Click;
            btnPrintReport.Click += BtnPrintReport_Click;
            btnRefresh.Click     += (s, e) => LoadCurrentShift();

            pnlBottom.Controls.Add(btnOpenShift);
            pnlBottom.Controls.Add(btnCloseShift);
            pnlBottom.Controls.Add(btnPrintReport);
            pnlBottom.Controls.Add(btnRefresh);
            this.Controls.Add(pnlBottom);
        }

        private void LoadCurrentShift()
        {
            try
            {
                var dt = DbHelper.Query(
                    "SELECT TOP 1 s.*, e.EmpName AS OpenedByName FROM Shifts s JOIN Employees e ON s.OpenedBy = e.EmpID WHERE s.Status = 'Open' ORDER BY s.OpenTime DESC");

                if (dt.Rows.Count > 0)
                {
                    _openShift = dt.Rows[0];
                    int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
                    Session.CurrentShiftID  = shiftID;
                    lblShiftStatus.Text     = "🟢  وردية مفتوحة";
                    lblShiftStatus.ForeColor = Theme.Success;
                    lblShiftInfo.Text       = $"فُتحت بواسطة: {_openShift["OpenedByName"]}   |   الوقت: {Convert.ToDateTime(_openShift["OpenTime"]):yyyy-MM-dd HH:mm}";
                    LoadShiftSummary(shiftID);
                    btnOpenShift.Enabled   = false;
                    btnCloseShift.Enabled  = true;
                    btnPrintReport.Enabled = true;
                }
                else
                {
                    _openShift             = null;
                    Session.CurrentShiftID = null;
                    lblShiftStatus.Text    = "🔴  لا توجد وردية مفتوحة";
                    lblShiftStatus.ForeColor = Theme.Danger;
                    lblShiftInfo.Text      = "اضغط فتح وردية جديدة لبدء يوم العمل";
                    ClearSummary();
                    btnOpenShift.Enabled   = true;
                    btnCloseShift.Enabled  = false;
                    btnPrintReport.Enabled = false;
                }
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftClose.LoadCurrentShift", ex); }
        }

        private void LoadShiftSummary(int shiftID)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT
                        ISNULL(SUM(TotalAmount), 0) AS TotalSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Cash' THEN TotalAmount ELSE 0 END), 0) AS CashSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Credit' THEN TotalAmount ELSE 0 END), 0) AS CreditSales,
                        ISNULL(SUM(CASE WHEN SaleType NOT IN ('Cash','Credit') THEN TotalAmount ELSE 0 END), 0) AS OtherSales
                    FROM Sales WHERE ShiftID = @sid AND IsPosted = 1",
                    DbHelper.P("@sid", shiftID));

                var dtR = DbHelper.Query(@"
                    SELECT ISNULL(SUM(sr.TotalAmount), 0) AS TotalReturns
                    FROM SalesReturns sr
                    JOIN Sales s ON sr.SaleID = s.SaleID
                    WHERE s.ShiftID = @sid",
                    DbHelper.P("@sid", shiftID));

                decimal ts  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["TotalSales"])   : 0;
                decimal cs  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["CashSales"])    : 0;
                decimal cr  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["CreditSales"])  : 0;
                decimal os  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["OtherSales"])   : 0;
                decimal tr  = dtR.Rows.Count > 0 ? Convert.ToDecimal(dtR.Rows[0]["TotalReturns"]): 0;
                decimal oc  = _openShift != null ? Convert.ToDecimal(_openShift["OpeningCash"])   : 0;
                decimal exp = oc + cs - tr;

                _summary = new ShiftSummary { TotalSales=ts, CashSales=cs, CreditSales=cr, OtherSales=os, TotalReturns=tr, OpeningCash=oc, Expected=exp };

                lblTotalSales.Text   = ts.ToString("N2")  + " ج";
                lblCashSales.Text    = cs.ToString("N2")  + " ج";
                lblVisaSales.Text    = cr.ToString("N2")  + " ج";
                lblOtherSales.Text   = os.ToString("N2")  + " ج";
                lblTotalReturns.Text = tr.ToString("N2")  + " ج";
                lblExpected.Text     = exp.ToString("N2") + " ج";
                txtActualCash.Text   = exp.ToString("N2");
                RecalcDiff();
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftClose.LoadShiftSummary", ex); }
        }

        private void RecalcDiff()
        {
            if (_summary == null) return;
            if (!decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual)) return;
            decimal diff = actual - _summary.Expected;
            lblDiff.Text      = diff.ToString("N2") + " ج";
            lblDiff.ForeColor = diff < 0 ? Theme.Danger : diff > 0 ? Theme.Success : Theme.TextMain;
        }

        private void ClearSummary()
        {
            _summary = null;
            lblTotalSales.Text = lblCashSales.Text = lblVisaSales.Text = lblOtherSales.Text =
            lblTotalReturns.Text = lblExpected.Text = lblDiff.Text = "---";
        }

        private void BtnOpenShift_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtOpeningCash.Text, out decimal opening)) opening = 0;
            if (MessageBox.Show($"فتح وردية جديدة برصيد فتح {opening:N2} ج؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                int shiftID = DbHelper.ExecuteInsert(
                    "INSERT INTO Shifts (ShiftDate,OpenTime,OpenedBy,OpeningCash,Status) VALUES (CAST(GETDATE() AS DATE),GETDATE(),@emp,@cash,'Open')",
                    DbHelper.P("@emp", Session.EmpID), DbHelper.P("@cash", opening));
                if (shiftID > 0) { Session.CurrentShiftID = shiftID; MessageBox.Show("✅ تم فتح الوردية!", "فتح الوردية", MessageBoxButtons.OK, MessageBoxIcon.Information); LoadCurrentShift(); }
            }
            catch (Exception ex) { MessageBox.Show("خطأ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnCloseShift_Click(object sender, EventArgs e)
        {
            if (_openShift == null) return;
            if (!decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual)) { MessageBox.Show("أدخل المبلغ الفعلي أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("تأكيد إغلاق الوردية؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
                decimal diff = actual - (_summary?.Expected ?? 0);
                DbHelper.Execute(@"UPDATE Shifts SET CloseTime=GETDATE(),ClosedBy=@emp,TotalSales=@ts,CashSales=@cs,OtherSales=@os,TotalReturns=@tr,ExpectedCash=@exp,ActualCash=@act,Difference=@diff,Notes=@n,Status='Closed' WHERE ShiftID=@sid",
                    DbHelper.P("@emp",Session.EmpID), DbHelper.P("@ts",_summary?.TotalSales??0), DbHelper.P("@cs",_summary?.CashSales??0),
                    DbHelper.P("@os",_summary?.OtherSales??0), DbHelper.P("@tr",_summary?.TotalReturns??0), DbHelper.P("@exp",_summary?.Expected??0),
                    DbHelper.P("@act",actual), DbHelper.P("@diff",diff), DbHelper.P("@n",txtNotes.Text.Trim()), DbHelper.P("@sid",shiftID));
                Session.CurrentShiftID = null;
                if (MessageBox.Show("طباعة تقرير الوردية؟", "طباعة", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    PrintShiftReport(shiftID, actual, diff);
                MessageBox.Show("✅ تم إغلاق الوردية!", "إغلاق الوردية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCurrentShift();
            }
            catch (Exception ex) { MessageBox.Show("خطأ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnPrintReport_Click(object sender, EventArgs e)
        {
            if (_openShift == null) return;
            decimal.TryParse(txtActualCash.Text.Replace(",",""), out decimal actual);
            PrintShiftReport(Convert.ToInt32(_openShift["ShiftID"]), actual, actual - (_summary?.Expected ?? 0));
        }

        private void PrintShiftReport(int shiftID, decimal actual, decimal diff)
        {
            var pd = new PrintDocument();
            if (!string.IsNullOrEmpty(AppConfig.ReceiptPrinterName)) AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
            pd.PrintPage += (s2, e2) =>
            {
                var g = e2.Graphics; var fnt = new Font("Courier New", 9f); var fntB = new Font("Courier New", 10f, FontStyle.Bold);
                int px = 10, py = 10; int pw = (int)e2.PageBounds.Width - 20;
                void Ln(string t, bool bold=false, bool center=false) { var sf=new System.Drawing.StringFormat(); if(center) sf.Alignment=StringAlignment.Center; g.DrawString(t, bold?fntB:fnt, Brushes.Black, center?new RectangleF(px,py,pw,16):new RectangleF(px,py,pw,16),sf); py+=18; }
                void Sep() { g.DrawLine(Pens.Black, px, py, px+pw, py); py+=6; }
                Ln(AppConfig.CompanyName, true, true);
                Ln($"تقرير الوردية #{shiftID}", true, true);
                Sep();
                if (_openShift != null) { Ln($"بداية: {Convert.ToDateTime(_openShift["OpenTime"]):yyyy-MM-dd HH:mm}"); Ln($"الكاشير: {_openShift["OpenedByName"]}"); }
                Ln($"انتهاء: {DateTime.Now:yyyy-MM-dd HH:mm}");
                Sep();
                Ln($"رصيد الفتح:        {(_summary?.OpeningCash??0),10:N2} ج");
                Ln($"إجمالي المبيعات:   {(_summary?.TotalSales??0),10:N2} ج");
                Ln($"  نقدي:            {(_summary?.CashSales??0),10:N2} ج");
                Ln($"  آجل/أخرى:        {(_summary?.OtherSales??0),10:N2} ج");
                Ln($"إجمالي المرتجعات:  {(_summary?.TotalReturns??0),10:N2} ج");
                Sep();
                Ln($"المتوقع:           {(_summary?.Expected??0),10:N2} ج");
                Ln($"الفعلي:            {actual,10:N2} ج");
                Ln($"الفرق:             {diff,10:N2} ج");
                Sep();
                if (!string.IsNullOrEmpty(txtNotes?.Text?.Trim())) Ln($"ملاحظات: {txtNotes.Text.Trim()}");
                Ln($"طُبع: {DateTime.Now:yyyy-MM-dd HH:mm}", false, true);
            };
            try { pd.Print(); } catch (Exception ex) { MessageBox.Show("فشل الطباعة:\n"+ex.Message,"خطأ",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }

        private class ShiftSummary
        {
            public decimal TotalSales, CashSales, CreditSales, OtherSales, TotalReturns, OpeningCash, Expected;
        }
    }
}
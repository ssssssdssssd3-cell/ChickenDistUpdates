using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة تقرير تقفيل الوردية التفصيلية وحساب العجز والزيادة</summary>
    public class FrmShiftReport : Form
    {
        private ComboBox cboShifts;
        private Label lblShiftHeader;
        private Label lblOpeningCashVal, lblCashSalesVal, lblVisaSalesVal, lblCreditSalesVal, lblCashInVal, lblReturnsVal, lblExpensesVal, lblExpectedVal, lblDiffVal;
        private DataGridView dgMovements;
        private Button btnPrint, btnClose, btnRefresh;

        private int? _targetShiftID;

        public FrmShiftReport(int? shiftID = null)
        {
            if (!Session.CanViewShiftDetails())
            {
                MessageBox.Show("⛔ غير مصرح لك بعرض تقرير وتفاصيل الورديات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }
            _targetShiftID = shiftID;
            InitUI();
            LoadShiftsList();
            if (_targetShiftID.HasValue && _targetShiftID.Value > 0)
            {
                SelectShiftInCombo(_targetShiftID.Value);
            }
        }

        private void InitUI()
        {
            this.Text = "📊 تقرير تفاصيل الوردية والعجز والزيادة";
            this.Size = new Size(1020, 750);
            this.MinimumSize = new Size(940, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // 1. رأس الشاشة
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "📊 تقرير تقفيل الوردية التفصيلي (تسوية العجز والزيادة)",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 26
            };

            var lblSub = new Label
            {
                Text = "عرض المبيعات والمرتجعات والمصروفات وصافي عجز أو زيادة نقدية الدرج",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // 2. شريط اختيار الوردية
            var pnlSelect = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8),
                Margin = new Padding(0, 0, 0, 6)
            };

            var lblSelTitle = new Label
            {
                Text = "🔍 اختر الوردية:",
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain,
                AutoSize = true,
                Location = new Point(880, 14)
            };

            cboShifts = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 420,
                Location = new Point(440, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboShifts.SelectedIndexChanged += (s, e) => LoadShiftDetails();

            lblShiftHeader = new Label
            {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(20, 14)
            };

            pnlSelect.Controls.Add(lblShiftHeader);
            pnlSelect.Controls.Add(cboShifts);
            pnlSelect.Controls.Add(lblSelTitle);

            // 3. المحتوى الرئيسي
            TableLayoutPanel tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12, 8, 12, 8),
                RightToLeft = RightToLeft.Yes
            };
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 95f));  // كروت المؤشرات
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // جدول الحركات

            // كروت المؤشرات KPI (9 كروت)
            Panel pnlKpis = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
            TableLayoutPanel tblKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 9,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            for (int i = 0; i < 9; i++) tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11f));

            lblOpeningCashVal = MakeCard(tblKpis, "💵 فتح الوردية", "0.00 ج", Theme.TextMain, 0);
            lblCashSalesVal   = MakeCard(tblKpis, "🛒 مبيعات كاش", "0.00 ج", Theme.Success, 1);
            lblVisaSalesVal   = MakeCard(tblKpis, "💳 مبيعات فيزا", "0.00 ج", Color.FromArgb(142, 68, 173), 2);
            lblCreditSalesVal = MakeCard(tblKpis, "📑 مبيعات آجل", "0.00 ج", Color.FromArgb(52, 152, 219), 3);
            lblCashInVal      = MakeCard(tblKpis, "➕ توريدات الدرج", "0.00 ج", Color.FromArgb(16, 185, 129), 4);
            lblReturnsVal     = MakeCard(tblKpis, "↩ مرتجعات كاش", "0.00 ج", Theme.Danger, 5);
            lblExpensesVal    = MakeCard(tblKpis, "💸 مصروفات", "0.00 ج", Color.FromArgb(230, 126, 34), 6);
            lblExpectedVal    = MakeCard(tblKpis, "💰 المتوقع بالدرج", "0.00 ج", Theme.Accent, 7);
            lblDiffVal        = MakeCard(tblKpis, "⚖️ عجز/زيادة", "0.00 ج", Theme.Success, 8);

            pnlKpis.Controls.Add(tblKpis);
            tblMain.Controls.Add(pnlKpis, 0, 0);

            // DataGridView حركات الوردية
            dgMovements = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5f),
                RowTemplate = { Height = 32 },
                EnableHeadersVisualStyles = false
            };
            dgMovements.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 50, 65);
            dgMovements.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgMovements.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgMovements.ColumnHeadersHeight = 36;
            dgMovements.DefaultCellStyle.BackColor = Theme.BgCard;
            dgMovements.DefaultCellStyle.SelectionBackColor = Theme.Accent;
            dgMovements.DefaultCellStyle.SelectionForeColor = Color.White;

            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع الحركة", FillWeight = 60f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefCode", HeaderText = "رقم المرجع", FillWeight = 60f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransTime", HeaderText = "الوقت", FillWeight = 70f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "البيان / العميل", FillWeight = 140f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "المبلغ النقدي (ج)", FillWeight = 70f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });

            tblMain.Controls.Add(dgMovements, 0, 1);

            // 4. الشريط السفلي
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnPrint   = Theme.MakeButton("🖨️ طباعة التقرير", Theme.Primary, new Point(0, 0), new Size(160, 40));
            btnRefresh = Theme.MakeButton("🔄 تحديث", Color.FromArgb(60, 70, 85), new Point(0, 0), new Size(110, 40));
            btnClose   = Theme.MakeButton("❌ إغلاق", Theme.Danger, new Point(0, 0), new Size(110, 40));

            btnPrint.Click   += BtnPrint_Click;
            btnRefresh.Click += (s, e) => LoadShiftDetails();
            btnClose.Click   += (s, e) => this.Close();

            FlowLayoutPanel flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };
            btnPrint.Margin   = new Padding(6, 0, 0, 0);
            btnRefresh.Margin = new Padding(6, 0, 0, 0);
            btnClose.Margin   = new Padding(6, 0, 0, 0);

            flowBottom.Controls.Add(btnPrint);
            flowBottom.Controls.Add(btnRefresh);
            flowBottom.Controls.Add(btnClose);
            pnlBottom.Controls.Add(flowBottom);

            // ترتيب إضافة عناصر النموذج لمنع تداخل الرص
            this.Controls.Add(pnlBottom);
            this.Controls.Add(tblMain);
            this.Controls.Add(pnlSelect);
            this.Controls.Add(pnlHeader);
            tblMain.BringToFront();
        }

        private Label MakeCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIdx)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(2),
                Padding = new Padding(4)
            };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            Label lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblV = new Label
            {
                Text = val,
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.Add(lblV);
            pnl.Controls.Add(lblT);
            parent.Controls.Add(pnl, colIdx, 0);
            return lblV;
        }

        private void LoadShiftsList()
        {
            try
            {
                DataTable dt = DbHelper.Query(@"
                    SELECT TOP 50 
                        s.ShiftID, s.Status, s.OpenTime, e.EmpName
                    FROM Shifts s
                    LEFT JOIN Employees e ON s.OpenedBy = e.EmpID
                    ORDER BY s.ShiftID DESC");

                cboShifts.Items.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    int sid = Convert.ToInt32(r["ShiftID"]);
                    string status = r["Status"].ToString() == "Open" ? "🟢 مفتوحة" : "🔴 مغلقة";
                    string emp = r["EmpName"] != DBNull.Value ? r["EmpName"].ToString() : "كاشير";
                    DateTime dtOpen = Convert.ToDateTime(r["OpenTime"]);
                    string text = $"وردية #{sid} ({status}) - {emp} [{dtOpen:yyyy-MM-dd HH:mm}]";
                    cboShifts.Items.Add(new ComboItem(sid, text));
                }

                if (cboShifts.Items.Count > 0)
                    cboShifts.SelectedIndex = 0;
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftReport.LoadShiftsList", ex); }
        }

        private void SelectShiftInCombo(int shiftID)
        {
            for (int i = 0; i < cboShifts.Items.Count; i++)
            {
                if (cboShifts.Items[i] is ComboItem item && item.ID == shiftID)
                {
                    cboShifts.SelectedIndex = i;
                    break;
                }
            }
        }

        private void LoadShiftDetails()
        {
            if (!(cboShifts.SelectedItem is ComboItem selectedItem) || selectedItem.ID <= 0) return;
            int shiftID = selectedItem.ID;

            try
            {
                DbHelper.EnsureShiftSchema();
                DataTable dtShift = DbHelper.Query(@"
                    SELECT s.*, e.EmpName AS OpenedByName, ec.EmpName AS ClosedByName, sa.AccountName AS SafeName
                    FROM Shifts s
                    LEFT JOIN Employees e ON s.OpenedBy = e.EmpID
                    LEFT JOIN Employees ec ON s.ClosedBy = ec.EmpID
                    LEFT JOIN SafeAccounts sa ON s.SafeAccountID = sa.AccountID
                    WHERE s.ShiftID = @sid",
                    DbHelper.P("@sid", shiftID));
                if (dtShift.Rows.Count == 0) return;
                DataRow sRow = dtShift.Rows[0];

                DateTime openTime = Convert.ToDateTime(sRow["OpenTime"]);
                string status = sRow["Status"].ToString();
                string openedBy = sRow["OpenedByName"] != DBNull.Value ? sRow["OpenedByName"].ToString() : "---";
                string closedBy = sRow["ClosedByName"] != DBNull.Value ? sRow["ClosedByName"].ToString() : "---";
                string safeName = sRow["SafeName"] != DBNull.Value ? sRow["SafeName"].ToString() : "درج الكاشير";

                lblShiftHeader.Text = status == "Open"
                    ? $"🟢 الوردية مفتوحة | الكاشير: {openedBy} | الخزنة: {safeName}"
                    : $"🔴 الوردية مغلقة | فتح: {openedBy} | أغلقت بواسطة: {closedBy} | الخزنة: {safeName}";

                int drawerSafeID = sRow["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(sRow["SafeAccountID"]) : 1;
                int openedByEmpID = sRow["OpenedBy"] != DBNull.Value ? Convert.ToInt32(sRow["OpenedBy"]) : Session.EmpID;

                // 1. مبيعات الوردية
                var dtSales = DbHelper.Query(@"
                    SELECT
                        ISNULL(SUM(TotalAmount), 0) AS TotalSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Cash' THEN ISNULL(CashPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(CashPaid, 0) ELSE 0 END), 0) AS CashSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Visa' THEN ISNULL(VisaPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(VisaPaid, 0) ELSE 0 END), 0) AS VisaSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Credit' THEN (TotalAmount - ISNULL(CashPaid, 0) - ISNULL(VisaPaid, 0)) WHEN SaleType = 'Mixed' THEN (TotalAmount - ISNULL(CashPaid, 0) - ISNULL(VisaPaid, 0)) ELSE 0 END), 0) AS CreditSales,
                        ISNULL(SUM(CASE WHEN SaleType NOT IN ('Cash','Credit','Visa','Mixed') THEN TotalAmount ELSE 0 END), 0) AS OtherSales
                    FROM Sales WHERE (ShiftID = @sid OR (ShiftID IS NULL AND CreatedBy = @emp AND SaleDate >= @dt)) AND IsPosted = 1",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime), DbHelper.P("@emp", openedByEmpID));

                // 2. مرتجعات الوردية
                var dtReturns = DbHelper.Query(@"
                    SELECT ISNULL(SUM(sr.TotalAmount), 0) AS TotalReturns
                    FROM SalesReturns sr
                    JOIN Sales s ON sr.SaleID = s.SaleID
                    WHERE (sr.ShiftID = @sid OR (sr.ShiftID IS NULL AND (sr.CreatedBy = @emp OR s.CreatedBy = @emp) AND sr.ReturnDate >= @dt))",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime), DbHelper.P("@emp", openedByEmpID));

                // 3. مصروفات وتوريدات الوردية للدرج حصراً
                var dtExp = DbHelper.Query(@"
                    SELECT 
                        ISNULL(SUM(AmountOut), 0) AS TotalExpenses,
                        ISNULL(SUM(AmountIn), 0) AS TotalCashIn
                    FROM CashBox 
                    WHERE (ShiftID = @sid OR (ShiftID IS NULL AND CreatedBy = @emp AND TransDate >= @dt))
                      AND (AccountID = @accId OR (@accId = 0 AND (AccountID IS NULL OR AccountID = 1)))
                      AND TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')",
                    DbHelper.P("@sid", shiftID),
                    DbHelper.P("@dt", openTime),
                    DbHelper.P("@accId", drawerSafeID),
                    DbHelper.P("@emp", openedByEmpID));

                decimal ts  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["TotalSales"])   : 0;
                decimal cs  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["CashSales"])    : 0;
                decimal vs  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["VisaSales"])    : 0;
                decimal cr  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["CreditSales"])  : 0;
                decimal os  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["OtherSales"])   : 0;
                
                decimal calcCredit = Math.Max(0m, ts - (cs + vs));
                if (calcCredit > cr) cr = calcCredit;

                decimal tr  = dtReturns.Rows.Count > 0 ? Convert.ToDecimal(dtReturns.Rows[0]["TotalReturns"]) : 0;
                decimal ex  = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalExpenses"]) : 0;
                decimal cin = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalCashIn"]) : 0;
                decimal oc  = sRow["OpeningCash"] != DBNull.Value ? Convert.ToDecimal(sRow["OpeningCash"]) : 0;

                decimal expected = oc + cs + cin - tr - ex;
                decimal actual   = sRow["ActualCash"] != DBNull.Value ? Convert.ToDecimal(sRow["ActualCash"]) : expected;
                decimal diff     = actual - expected;

                lblOpeningCashVal.Text = oc.ToString("N2") + " ج";
                lblCashSalesVal.Text   = cs.ToString("N2") + " ج";
                lblVisaSalesVal.Text   = vs.ToString("N2") + " ج";
                lblCreditSalesVal.Text = (cr + os).ToString("N2") + " ج";
                lblCashInVal.Text      = cin.ToString("N2") + " ج";
                lblReturnsVal.Text     = tr.ToString("N2") + " ج";
                lblExpensesVal.Text    = ex.ToString("N2") + " ج";
                lblExpectedVal.Text    = expected.ToString("N2") + " ج";

                if (diff == 0)
                {
                    lblDiffVal.Text = "0.00 ج (مطابق)";
                    lblDiffVal.ForeColor = Theme.Success;
                }
                else if (diff > 0)
                {
                    lblDiffVal.Text = $"+{diff:N2} ج (زيادة)";
                    lblDiffVal.ForeColor = Theme.Accent;
                }
                else
                {
                    lblDiffVal.Text = $"{diff:N2} ج (عجز)";
                    lblDiffVal.ForeColor = Theme.Danger;
                }

                // 4. جدول الحركات التفصيلية
                dgMovements.Rows.Clear();
                var dtMovements = DbHelper.Query(@"
                    SELECT 'مبيعات' AS TransType, s.SaleCode AS RefCode, s.SaleDate AS TransTime, ISNULL(c.ClientName, N'عميل نقدي') AS Details, s.TotalAmount AS Amount
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.CreatedBy = @emp AND s.SaleDate >= @dt)) AND s.IsPosted = 1
                    UNION ALL
                    SELECT 'مرتجع' AS TransType, CAST(sr.ReturnID AS NVARCHAR) AS RefCode, sr.ReturnDate AS TransTime, 'مرتجع فاتورة' AS Details, sr.TotalAmount AS Amount
                    FROM SalesReturns sr JOIN Sales s ON sr.SaleID = s.SaleID 
                    WHERE (sr.ShiftID = @sid OR (sr.ShiftID IS NULL AND (sr.CreatedBy = @emp OR s.CreatedBy = @emp) AND sr.ReturnDate >= @dt))
                    UNION ALL
                    SELECT 
                        CASE 
                            WHEN TransType = 'ClientPayment' THEN N'تحصيل من عميل'
                            WHEN TransType = 'SupplierPayment' THEN N'صرف لمورد'
                            WHEN TransType = 'EmpAdvance' THEN N'سلفة موظف'
                            WHEN TransType = 'EmpPaymentOut' THEN N'راتب/مستحقات'
                            WHEN TransType = 'EmpPaymentIn' THEN N'توريد موظف'
                            WHEN TransType = 'ReceiptIn' THEN N'سند قبض'
                            WHEN TransType = 'ReceiptOut' THEN N'سند صرف'
                            WHEN AmountIn > 0 THEN N'وارد للخزنة'
                            ELSE N'مصروفات'
                        END AS TransType, 
                        CAST(CashID AS NVARCHAR) AS RefCode, 
                        TransDate AS TransTime, 
                        Notes AS Details, 
                        CASE WHEN AmountIn > 0 THEN AmountIn ELSE -AmountOut END AS Amount
                    FROM CashBox 
                    WHERE (ShiftID = @sid OR (ShiftID IS NULL AND CreatedBy = @emp AND TransDate >= @dt)) 
                      AND (AccountID = @accId OR (@accId = 0 AND (AccountID IS NULL OR AccountID = 1)))
                      AND TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')
                    ORDER BY TransTime DESC",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime), DbHelper.P("@accId", drawerSafeID), DbHelper.P("@emp", openedByEmpID));

                foreach (DataRow r in dtMovements.Rows)
                {
                    decimal amt = Convert.ToDecimal(r["Amount"]);
                    int ri = dgMovements.Rows.Add(
                        r["TransType"],
                        r["RefCode"],
                        Convert.ToDateTime(r["TransTime"]).ToString("HH:mm:ss"),
                        r["Details"],
                        amt.ToString("N2"));

                    var rowStyle = dgMovements.Rows[ri].DefaultCellStyle;
                    if (amt > 0)
                    {
                        rowStyle.ForeColor = Color.FromArgb(15, 120, 50);
                    }
                    else if (amt < 0)
                    {
                        rowStyle.ForeColor = Color.FromArgb(180, 20, 20);
                    }
                }
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftReport.LoadShiftDetails", ex); }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (!(cboShifts.SelectedItem is ComboItem selectedItem) || selectedItem.ID <= 0) return;
            int shiftID = selectedItem.ID;
            FrmPrintShift.ShowPrintOptions(shiftID, btnPrint);
        }
    }
}

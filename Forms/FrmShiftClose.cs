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
    /// شاشة إدارة وإغلاق الوردية المتقدمة — مع دعم الإغلاق الأعمى وصلاحيات الكشف عن التفاصيل
    /// </summary>
    public class FrmShiftClose : Form
    {
        private DataRow _openShift = null;
        private ShiftSummary _summary = null;
        
        private Panel pnlHeader;
        private Panel pnlStatus;
        private Panel pnlKpiContainer;
        private Panel pnlActualContainer;
        private Panel pnlBottom;
        
        private Label lblShiftStatus;
        private Label lblShiftInfo;
        
        private Label lblOpeningCashVal;
        private Label lblCashSalesVal;
        private Label lblCreditSalesVal;
        private Label lblReturnsVal;
        private Label lblExpensesVal;
        private Label lblExpectedVal;
        private Label lblDiffVal;

        private TextBox txtActualCash;
        private TextBox txtNotes;
        private TextBox txtOpeningCash;
        
        private Button btnOpenShift;
        private Button btnCloseShift;
        private Button btnPrintReport;
        private Button btnRefresh;
        private Button btnToggleDetails;

        private DataGridView dgMovements;
        private bool _forceShowDetails = false;

        public FrmShiftClose()
        {
            InitUI();
            LoadCurrentShift();
        }

        private void InitUI()
        {
            this.Text = "إدارة وإغلاق الوردية";
            this.Size = new Size(1020, 760);
            this.MinimumSize = new Size(960, 710);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // ── 1. رأس الشاشة ──────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnToggleDetails = new Button
            {
                Text = "👁️ إظهار/إخفاء التفاصيل",
                Size = new Size(170, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 80, 95),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnToggleDetails.FlatAppearance.BorderSize = 0;
            btnToggleDetails.Click += (s, e) =>
            {
                if (Session.Role == "Admin" || Session.CanViewDetails("ShiftClose"))
                {
                    _forceShowDetails = !_forceShowDetails;
                    if (_openShift != null)
                    {
                        int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
                        LoadShiftSummary(shiftID);
                    }
                }
                else
                {
                    MessageBox.Show("🔒 ليس لديك صلاحية لعرض التفاصيل المباشرة للوردية (الإغلاق الأعمى).", "تنبيه الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            var pnlTitleBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text = "🔄 إدارة وإغلاق وردية الكاشير",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 25
            };

            var lblSub = new Label
            {
                Text = "متابعة حركة الخزنة والمبيعات وإغلاق الحسابات بمرونة وأمان",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlTitleBox.Controls.Add(lblSub);
            pnlTitleBox.Controls.Add(lblTitle);

            pnlHeader.Controls.Add(pnlTitleBox);
            pnlHeader.Controls.Add(btnToggleDetails);
            this.Controls.Add(pnlHeader);

            // ── 2. المحتوى الرئيسي ──────────────────────────────────────
            TableLayoutPanel tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12, 8, 12, 8),
                RightToLeft = RightToLeft.Yes
            };
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));  // كارت حالة الوردية والموظف
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 85f));  // كروت المؤشرات KPI
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 65f));  // كارت العد الفعلي والفرق
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // جدول حركات الوردية

            // أ) كارت حالة الوردية
            pnlStatus = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(0, 0, 0, 6) };
            pnlStatus.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlStatus);

            TableLayoutPanel tblStatus = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 6, 10, 6),
                RightToLeft = RightToLeft.Yes
            };
            tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72f)); // معلومات الوردية والكاشير
            tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f)); // رصيد فتح الوردية

            Panel pnlStatusRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            lblShiftStatus = new Label
            {
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };
            lblShiftInfo = new Label
            {
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlStatusRight.Controls.Add(lblShiftInfo);
            pnlStatusRight.Controls.Add(lblShiftStatus);
            tblStatus.Controls.Add(pnlStatusRight, 0, 0);

            Panel pnlStatusLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblOpeningTitle = new Label
            {
                Text = "رصيد فتح الوردية (ج):",
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };
            txtOpeningCash = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "0",
                TextAlign = HorizontalAlignment.Center
            };
            pnlStatusLeft.Controls.Add(txtOpeningCash);
            pnlStatusLeft.Controls.Add(lblOpeningTitle);
            tblStatus.Controls.Add(pnlStatusLeft, 1, 0);

            pnlStatus.Controls.Add(tblStatus);
            tblMain.Controls.Add(pnlStatus, 0, 0);

            // ب) كروت المؤشرات KPI (6 كروت)
            pnlKpiContainer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
            TableLayoutPanel tblKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            for (int i = 0; i < 6; i++) tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));

            lblOpeningCashVal = MakeKpiCard(tblKpi, "💵 رصيد البداية", "0.00 ج", Theme.TextMain, 0);
            lblCashSalesVal   = MakeKpiCard(tblKpi, "🛒 مبيعات كاش", "0.00 ج", Theme.Success, 1);
            lblCreditSalesVal = MakeKpiCard(tblKpi, "💳 مبيعات آجل/فيزا", "0.00 ج", Color.FromArgb(52, 152, 219), 2);
            lblReturnsVal     = MakeKpiCard(tblKpi, "↩ مرتجعات", "0.00 ج", Theme.Danger, 3);
            lblExpensesVal    = MakeKpiCard(tblKpi, "💸 مصروفات/خارج", "0.00 ج", Color.FromArgb(230, 126, 34), 4);
            lblExpectedVal    = MakeKpiCard(tblKpi, "💰 المتوقع بالخزنة", "0.00 ج", Theme.Accent, 5);

            pnlKpiContainer.Controls.Add(tblKpi);
            tblMain.Controls.Add(pnlKpiContainer, 0, 1);

            // ج) لوحة العد الفعلي والفرق والملحوظات
            pnlActualContainer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(0, 0, 0, 6) };
            pnlActualContainer.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlActualContainer);

            TableLayoutPanel tblActual = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10, 4, 10, 4),
                RightToLeft = RightToLeft.Yes
            };
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230f)); // الفعلي
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f)); // الفرق
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // الملاحظات

            // حقل الفعلي
            Panel pnlAct = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblActTitle = new Label { Text = "💵 المبلغ الفعلي الموجود بالخزنة:", Dock = DockStyle.Top, Height = 20, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            txtActualCash = new TextBox { Dock = DockStyle.Bottom, Height = 28, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, Text = "0", TextAlign = HorizontalAlignment.Center };
            txtActualCash.TextChanged += (s, e) => RecalcDiff();
            pnlAct.Controls.Add(txtActualCash);
            pnlAct.Controls.Add(lblActTitle);
            tblActual.Controls.Add(pnlAct, 0, 0);

            // حقل الفرق
            Panel pnlDiff = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblDiffTitle = new Label { Text = "⚖️ الفرق (عجز / زيادة):", Dock = DockStyle.Top, Height = 20, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            lblDiffVal = new Label { Text = "0.00 ج", Dock = DockStyle.Bottom, Height = 28, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Theme.Accent, TextAlign = ContentAlignment.MiddleCenter };
            pnlDiff.Controls.Add(lblDiffVal);
            pnlDiff.Controls.Add(lblDiffTitle);
            tblActual.Controls.Add(pnlDiff, 1, 0);

            // حقل الملاحظات
            Panel pnlNotes = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblNotesTitle = new Label { Text = "📝 ملاحظات الإغلاق:", Dock = DockStyle.Top, Height = 20, Font = Theme.FontMain, ForeColor = Theme.TextMain };
            txtNotes = new TextBox { Dock = DockStyle.Bottom, Height = 28, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, BorderStyle = BorderStyle.FixedSingle };
            pnlNotes.Controls.Add(txtNotes);
            pnlNotes.Controls.Add(lblNotesTitle);
            tblActual.Controls.Add(pnlNotes, 2, 0);

            pnlActualContainer.Controls.Add(tblActual);
            tblMain.Controls.Add(pnlActualContainer, 0, 2);

            // د) جدول حركات الوردية
            dgMovements = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 35,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع الحركة", FillWeight = 60f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefCode", HeaderText = "رقم المرجع", FillWeight = 60f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransTime", HeaderText = "الوقت", FillWeight = 70f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "البيان / العميل", FillWeight = 140f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "المبلغ النقدي (ج)", FillWeight = 70f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });

            tblMain.Controls.Add(dgMovements, 0, 3);
            this.Controls.Add(tblMain);

            // ── 3. شريط التحكم السفلي ──────────────────────────────────
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            btnOpenShift   = Theme.MakeButton("✅ فتح وردية جديدة", Theme.Success, new Point(0, 0), new Size(180, 42));
            btnCloseShift  = Theme.MakeButton("🔒 إغلاق الوردية",   Theme.Danger,  new Point(0, 0), new Size(180, 42));
            btnPrintReport = Theme.MakeButton("🖨️ طباعة التقرير",   Theme.Primary, new Point(0, 0), new Size(170, 42));
            btnRefresh     = Theme.MakeButton("🔄 تحديث", Color.FromArgb(60, 70, 85), new Point(0, 0), new Size(110, 42));

            FlowLayoutPanel flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };
            btnOpenShift.Margin   = new Padding(6, 0, 0, 0);
            btnCloseShift.Margin  = new Padding(6, 0, 0, 0);
            btnPrintReport.Margin = new Padding(6, 0, 0, 0);
            btnRefresh.Margin     = new Padding(6, 0, 0, 0);

            btnOpenShift.Click   += BtnOpenShift_Click;
            btnCloseShift.Click  += BtnCloseShift_Click;
            btnPrintReport.Click += BtnPrintReport_Click;
            btnRefresh.Click     += (s, e) => LoadCurrentShift();

            flowBottom.Controls.Add(btnOpenShift);
            flowBottom.Controls.Add(btnCloseShift);
            flowBottom.Controls.Add(btnPrintReport);
            flowBottom.Controls.Add(btnRefresh);

            pnlBottom.Controls.Add(flowBottom);
            this.Controls.Add(pnlBottom);
        }

        private Label MakeKpiCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIdx)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(3),
                Padding = new Padding(6)
            };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            Label lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.TopRight
            };

            Label lblV = new Label
            {
                Text = val,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.BottomRight
            };

            pnl.Controls.Add(lblV);
            pnl.Controls.Add(lblT);
            parent.Controls.Add(pnl, colIdx, 0);
            return lblV;
        }

        private void LoadCurrentShift()
        {
            try
            {
                var dt = DbHelper.Query(
                    @"SELECT TOP 1 s.*, e.EmpName AS OpenedByName, sa.AccountName AS SafeName 
                      FROM Shifts s 
                      JOIN Employees e ON s.OpenedBy = e.EmpID 
                      LEFT JOIN SafeAccounts sa ON s.SafeAccountID = sa.AccountID 
                      WHERE s.Status = 'Open' ORDER BY s.OpenTime DESC");

                if (dt.Rows.Count > 0)
                {
                    _openShift = dt.Rows[0];
                    int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
                    Session.CurrentShiftID = shiftID;
                    
                    DateTime openTime = Convert.ToDateTime(_openShift["OpenTime"]);
                    TimeSpan duration = DateTime.Now - openTime;
                    string durationStr = $"{(int)duration.TotalHours} ساعة و {duration.Minutes} دقيقة";
                    string safeName = _openShift["SafeName"] != DBNull.Value ? _openShift["SafeName"].ToString() : "درج الكاشير / الخزنة العامة";

                    lblShiftStatus.Text = $"🟢  وردية مفتوحة #{shiftID}";
                    lblShiftStatus.ForeColor = Theme.Success;
                    lblShiftInfo.Text = $"👤 الكاشير: {_openShift["OpenedByName"]}   |   📅 فتح الوردية: {openTime:yyyy-MM-dd  hh:mm tt}   |   🏦 الخزنة/الدرج: {safeName}   |   ⏱️ المدة: {durationStr}";
                    
                    txtOpeningCash.Text = Convert.ToDecimal(_openShift["OpeningCash"]).ToString("N2");
                    txtOpeningCash.Enabled = false;

                    LoadShiftSummary(shiftID);
                    LoadShiftMovements(shiftID, openTime);

                    btnOpenShift.Enabled   = false;
                    btnCloseShift.Enabled  = true;
                    btnPrintReport.Enabled = true;
                }
                else
                {
                    _openShift = null;
                    Session.CurrentShiftID = null;
                    lblShiftStatus.Text = "🔴  لا توجد وردية مفتوحة حالياً";
                    lblShiftStatus.ForeColor = Theme.Danger;
                    lblShiftInfo.Text = "اضغط على (فتح وردية جديدة) لبدء يوم عمل جديد وتسجيل النقدية.";
                    txtOpeningCash.Enabled = true;

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

                DateTime openTime = _openShift != null ? Convert.ToDateTime(_openShift["OpenTime"]) : DateTime.Today;

                var dtExp = DbHelper.Query(@"
                    SELECT 
                        ISNULL(SUM(AmountOut), 0) AS TotalExpenses,
                        ISNULL(SUM(AmountIn), 0) AS TotalCashIn
                    FROM CashBox 
                    WHERE TransDate >= @dt AND TransType NOT IN ('Sale', 'SaleReturn')",
                    DbHelper.P("@dt", openTime));

                decimal ts  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["TotalSales"])   : 0;
                decimal cs  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["CashSales"])    : 0;
                decimal cr  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["CreditSales"])  : 0;
                decimal os  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["OtherSales"])   : 0;
                decimal tr  = dtR.Rows.Count > 0 ? Convert.ToDecimal(dtR.Rows[0]["TotalReturns"]): 0;
                decimal ex  = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalExpenses"]) : 0;
                decimal cin = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalCashIn"]) : 0;
                decimal oc  = _openShift != null ? Convert.ToDecimal(_openShift["OpeningCash"])   : 0;
                
                decimal exp = oc + cs + cin - tr - ex;

                _summary = new ShiftSummary { TotalSales = ts, CashSales = cs, CreditSales = cr, OtherSales = os, TotalReturns = tr, Expenses = ex, OpeningCash = oc, Expected = exp };

                bool canViewDetails = (Session.Role == "Admin" || Session.CanViewDetails("ShiftClose") || _forceShowDetails);

                if (canViewDetails)
                {
                    lblOpeningCashVal.Text = oc.ToString("N2")  + " ج";
                    lblCashSalesVal.Text   = cs.ToString("N2")  + " ج";
                    lblCreditSalesVal.Text = cr.ToString("N2")  + " ج";
                    lblReturnsVal.Text     = tr.ToString("N2")  + " ج";
                    lblExpensesVal.Text    = ex.ToString("N2")  + " ج";
                    lblExpectedVal.Text    = exp.ToString("N2") + " ج";
                    txtActualCash.Text     = exp.ToString("N2");
                }
                else
                {
                    // الإغلاق الأعمى — إخفاء الأرقام المتوقعة من الكاشير
                    lblOpeningCashVal.Text = "🔒 مخفي";
                    lblCashSalesVal.Text   = "🔒 مخفي";
                    lblCreditSalesVal.Text = "🔒 مخفي";
                    lblReturnsVal.Text     = "🔒 مخفي";
                    lblExpensesVal.Text    = "🔒 مخفي";
                    lblExpectedVal.Text    = "🔒 مخفي (أعمى)";
                    txtActualCash.Text     = "0.00";
                }

                RecalcDiff();
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftClose.LoadShiftSummary", ex); }
        }

        private void LoadShiftMovements(int shiftID, DateTime openTime)
        {
            dgMovements.Rows.Clear();
            bool canViewDetails = (Session.Role == "Admin" || Session.CanViewDetails("ShiftClose") || _forceShowDetails);
            if (!canViewDetails)
            {
                dgMovements.Visible = false;
                return;
            }

            dgMovements.Visible = true;
            try
            {
                var dtSales = DbHelper.Query(@"
                    SELECT 'مبيعات' AS TransType, s.SaleCode AS RefCode, s.SaleDate AS TransTime, ISNULL(c.ClientName, N'عميل نقدي') AS Details, s.TotalAmount AS Amount
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE s.ShiftID=@sid AND s.IsPosted=1
                    UNION ALL
                    SELECT 'مرتجع' AS TransType, CAST(sr.ReturnID AS NVARCHAR) AS RefCode, sr.ReturnDate AS TransTime, 'مرتجع فاتورة' AS Details, sr.TotalAmount AS Amount
                    FROM SalesReturns sr JOIN Sales s ON sr.SaleID=s.SaleID WHERE s.ShiftID=@sid
                    UNION ALL
                    SELECT 'مصروف/حركة' AS TransType, CAST(CashID AS NVARCHAR) AS RefCode, TransDate AS TransTime, Notes AS Details, (AmountOut - AmountIn) AS Amount
                    FROM CashBox WHERE TransDate >= @dt AND TransType NOT IN ('Sale', 'SaleReturn')
                    ORDER BY TransTime DESC",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime));

                foreach (DataRow r in dtSales.Rows)
                {
                    dgMovements.Rows.Add(
                        r["TransType"],
                        r["RefCode"],
                        Convert.ToDateTime(r["TransTime"]).ToString("HH:mm:ss"),
                        r["Details"],
                        Convert.ToDecimal(r["Amount"]).ToString("N2"));
                }
            }
            catch { }
        }

        private void RecalcDiff()
        {
            if (_summary == null) return;
            bool canViewDetails = (Session.Role == "Admin" || Session.CanViewDetails("ShiftClose") || _forceShowDetails);

            if (!decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual)) return;

            if (!canViewDetails)
            {
                lblDiffVal.Text = "🔒 (إغلاق أعمى)";
                lblDiffVal.ForeColor = Theme.TextSub;
                return;
            }

            decimal diff = actual - _summary.Expected;
            if (diff == 0)
            {
                lblDiffVal.Text = "0.00 ج (مطابق ✔)";
                lblDiffVal.ForeColor = Theme.Success;
            }
            else if (diff < 0)
            {
                lblDiffVal.Text = $"{diff:N2} ج (عجز 🔴)";
                lblDiffVal.ForeColor = Theme.Danger;
            }
            else
            {
                lblDiffVal.Text = $"+{diff:N2} ج (زيادة 🔵)";
                lblDiffVal.ForeColor = Color.FromArgb(52, 152, 219);
            }
        }

        private void ClearSummary()
        {
            _summary = null;
            lblOpeningCashVal.Text = lblCashSalesVal.Text = lblCreditSalesVal.Text =
            lblReturnsVal.Text = lblExpensesVal.Text = lblExpectedVal.Text = lblDiffVal.Text = "---";
            dgMovements.Rows.Clear();
        }

        private void BtnOpenShift_Click(object sender, EventArgs e)
        {
            using (var dlg = new FrmOpenShift())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadCurrentShift();
                }
            }
        }

        private void BtnCloseShift_Click(object sender, EventArgs e)
        {
            if (_openShift == null) return;
            if (!decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual))
            {
                MessageBox.Show("الرجاء إدخال المبلغ الفعلي الموجود بالخزنة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("هل أنت تأكد من إغلاق الوردية الحالية وتسوية الحسابات؟", "تأكيد إغلاق الوردية", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
                decimal diff = actual - (_summary?.Expected ?? 0);

                DbHelper.Execute(@"
                    UPDATE Shifts SET 
                        CloseTime = GETDATE(),
                        ClosedBy = @emp,
                        TotalSales = @ts,
                        CashSales = @cs,
                        OtherSales = @os,
                        TotalReturns = @tr,
                        ExpectedCash = @exp,
                        ActualCash = @act,
                        Difference = @diff,
                        Notes = @n,
                        Status = 'Closed' 
                    WHERE ShiftID = @sid",
                    DbHelper.P("@emp", Session.EmpID),
                    DbHelper.P("@ts", _summary?.TotalSales ?? 0),
                    DbHelper.P("@cs", _summary?.CashSales ?? 0),
                    DbHelper.P("@os", _summary?.OtherSales ?? 0),
                    DbHelper.P("@tr", _summary?.TotalReturns ?? 0),
                    DbHelper.P("@exp", _summary?.Expected ?? 0),
                    DbHelper.P("@act", actual),
                    DbHelper.P("@diff", diff),
                    DbHelper.P("@n", txtNotes.Text.Trim()),
                    DbHelper.P("@sid", shiftID));

                Session.CurrentShiftID = null;

                if (MessageBox.Show("✅ تم إغلاق الوردية بنجاح!\nهل تريد طباعة تقرير إغلاق الوردية الآن؟", "إغلاق الوردية", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    PrintShiftReport(shiftID, actual, diff);
                }

                LoadCurrentShift();
            }
            catch (Exception ex) { MessageBox.Show("خطأ عند إغلاق الوردية:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnPrintReport_Click(object sender, EventArgs e)
        {
            if (_openShift == null) return;
            decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual);
            PrintShiftReport(Convert.ToInt32(_openShift["ShiftID"]), actual, actual - (_summary?.Expected ?? 0));
        }

        private void PrintShiftReport(int shiftID, decimal actual, decimal diff)
        {
            var pd = new PrintDocument();
            if (!string.IsNullOrEmpty(AppConfig.ReceiptPrinterName))
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);

            pd.PrintPage += (s2, e2) =>
            {
                var g = e2.Graphics;
                var fnt = new Font("Courier New", 9f);
                var fntB = new Font("Courier New", 10f, FontStyle.Bold);
                int px = 10, py = 10;
                int pw = (int)e2.PageBounds.Width - 20;

                void Ln(string t, bool bold = false, bool center = false)
                {
                    var sf = new System.Drawing.StringFormat();
                    if (center) sf.Alignment = StringAlignment.Center;
                    g.DrawString(t, bold ? fntB : fnt, Brushes.Black, center ? new RectangleF(px, py, pw, 16) : new RectangleF(px, py, pw, 16), sf);
                    py += 18;
                }
                void Sep() { g.DrawLine(Pens.Black, px, py, px + pw, py); py += 6; }

                Ln(AppConfig.CompanyName, true, true);
                Ln($"تقرير إغلاق الوردية #{shiftID}", true, true);
                Sep();
                if (_openShift != null)
                {
                    Ln($"وقت الفتح: {Convert.ToDateTime(_openShift["OpenTime"]):yyyy-MM-dd HH:mm}");
                    Ln($"الكاشير: {_openShift["OpenedByName"]}");
                }
                Ln($"وقت الإغلاق: {DateTime.Now:yyyy-MM-dd HH:mm}");
                Sep();
                Ln($"رصيد الفتح:        {(_summary?.OpeningCash ?? 0),10:N2} ج");
                Ln($"إجمالي المبيعات:   {(_summary?.TotalSales ?? 0),10:N2} ج");
                Ln($"  نقدي:            {(_summary?.CashSales ?? 0),10:N2} ج");
                Ln($"  آجل/بطاقات:      {(_summary?.CreditSales + _summary?.OtherSales ?? 0),10:N2} ج");
                Ln($"إجمالي المرتجعات:  {(_summary?.TotalReturns ?? 0),10:N2} ج");
                Ln($"المصروفات والسحب:  {(_summary?.Expenses ?? 0),10:N2} ج");
                Sep();
                Ln($"المتوقع بالخزنة:   {(_summary?.Expected ?? 0),10:N2} ج");
                Ln($"الفعلي بالخزنة:    {actual,10:N2} ج");
                Ln($"الفرق (عجز/زيادة): {diff,10:N2} ج");
                Sep();
                if (!string.IsNullOrEmpty(txtNotes?.Text?.Trim()))
                    Ln($"ملاحظات: {txtNotes.Text.Trim()}");
                Ln($"طُبع بتاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}", false, true);
            };

            try { pd.Print(); }
            catch (Exception ex) { MessageBox.Show("فشل إرسال التقرير للطابعة:\n" + ex.Message, "خطأ طباعة", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private class ShiftSummary
        {
            public decimal TotalSales, CashSales, CreditSales, OtherSales, TotalReturns, Expenses, OpeningCash, Expected;
        }
    }
}
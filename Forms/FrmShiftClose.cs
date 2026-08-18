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
        private Label lblVisaSalesVal;
        private Label lblCreditSalesVal;
        private Label lblCashInVal;
        private Label lblReturnsVal;
        private Label lblExpensesVal;
        private Label lblExpectedVal;
        private Label lblDiffVal;
        private Label lblRemainingVal;

        private TextBox txtActualCash;
        private TextBox txtTransferAmount;
        private TextBox txtNotes;
        private TextBox txtOpeningCash;
        private ComboBox cboTargetSafe;
        
        private Button btnOpenShift;
        private Button btnCloseShift;
        private Button btnDetailedReport;
        private Button btnPrintReport;
        private Button btnRefresh;
        private Button btnToggleDetails;
        private Button btnDrawerMovementDetails;

        private DataGridView dgMovements;
        private bool _forceShowDetails = true;

        public FrmShiftClose()
        {
            InitUI();
            LoadTargetSafes();
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
                Size = new Size(160, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 80, 95),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnToggleDetails.FlatAppearance.BorderSize = 0;
            btnToggleDetails.Click += (s, e) =>
            {
                if (Session.IsAdmin || Session.CanViewDetails("ShiftClose") || Session.CanAccess("ShiftDetails"))
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

            btnDrawerMovementDetails = new Button
            {
                Text = "🔍 حركة وتفاصيل الدرج",
                Size = new Size(170, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDrawerMovementDetails.FlatAppearance.BorderSize = 0;
            btnDrawerMovementDetails.Click += (s, e) =>
            {
                if (Session.IsAdmin || Session.CanAccess("ShiftDetails") || Session.CanAccess("ShiftCloseDetails") || Session.CanViewDetails("ShiftClose"))
                {
                    if (_openShift != null)
                    {
                        int sid = Convert.ToInt32(_openShift["ShiftID"]);
                        using (var dlg = new FrmShiftDrawerDetails(sid)) { dlg.ShowDialog(this); }
                    }
                    else
                    {
                        MessageBox.Show("⚠️ لا توجد وردية مفتوحة حالياً لعرض حركتها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("🔒 عفوًا: لا تملك صلاحية لعرض حركة وتفاصيل الدرج خلال الشيفت!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            pnlHeader.Controls.Add(btnDrawerMovementDetails);
            pnlHeader.Controls.Add(btnToggleDetails);

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
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 95f));  // كروت المؤشرات KPI
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));  // كارت العد الفعلي والفرق
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
                Dock = DockStyle.Top,
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

            // ب) كروت المؤشرات KPI (8 كروت)
            pnlKpiContainer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
            TableLayoutPanel tblKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            for (int i = 0; i < 8; i++) tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));

            lblOpeningCashVal = MakeKpiCard(tblKpi, "💵 رصيد البداية", "0.00 ج", Theme.TextMain, 0);
            lblCashSalesVal   = MakeKpiCard(tblKpi, "🛒 مبيعات كاش", "0.00 ج", Theme.Success, 1);
            lblVisaSalesVal   = MakeKpiCard(tblKpi, "💳 مبيعات فيزا", "0.00 ج", Color.FromArgb(142, 68, 173), 2);
            lblCreditSalesVal = MakeKpiCard(tblKpi, "📑 مبيعات آجل", "0.00 ج", Color.FromArgb(52, 152, 219), 3);
            lblCashInVal      = MakeKpiCard(tblKpi, "➕ توريدات للدرج", "0.00 ج", Color.FromArgb(16, 185, 129), 4);
            lblReturnsVal     = MakeKpiCard(tblKpi, "↩ مرتجعات كاش", "0.00 ج", Theme.Danger, 5);
            lblExpensesVal    = MakeKpiCard(tblKpi, "💸 مصروفات وسحب", "0.00 ج", Color.FromArgb(230, 126, 34), 6);
            lblExpectedVal    = MakeKpiCard(tblKpi, "💰 المتوقع بالدرج", "0.00 ج", Theme.Accent, 7);

            pnlKpiContainer.Controls.Add(tblKpi);
            tblMain.Controls.Add(pnlKpiContainer, 0, 1);

            // ج) لوحة العد الفعلي والتحويل والفرق والملحوظات
            pnlActualContainer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(0, 0, 0, 6) };
            pnlActualContainer.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlActualContainer);

            TableLayoutPanel tblActual = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(10, 6, 10, 6),
                RightToLeft = RightToLeft.Yes
            };
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f)); // 1. الفعلي بالدرج
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f)); // 2. وجهة النقدية
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f)); // 3. مبلغ التحويل للخزنة
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f)); // 4. الفرق (عجز/زيادة)
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f)); // 5. رصيد الدرج بعد التحويل
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // 6. الملاحظات

            // 1. حقل الفعلي
            Panel pnlAct = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblActTitle = new Label { Text = "💵 المبلغ الفعلي بالدرج:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            txtActualCash = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, Text = "0", TextAlign = HorizontalAlignment.Center };
            txtActualCash.TextChanged += (s, e) => OnActualCashChanged();
            pnlAct.Controls.Add(txtActualCash);
            pnlAct.Controls.Add(lblActTitle);
            tblActual.Controls.Add(pnlAct, 0, 0);

            // 2. حقل وجهة نقدية الوردية (التحويل للخزنة أو إبقائها كرصيد افتتاحي)
            Panel pnlTargetSafe = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblTargetTitle = new Label { Text = "🏦 وجهة النقدية:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            cboTargetSafe = new ComboBox { Dock = DockStyle.Top, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, RightToLeft = RightToLeft.Yes };
            cboTargetSafe.SelectedIndexChanged += (s, e) => OnTargetSafeChanged();
            pnlTargetSafe.Controls.Add(cboTargetSafe);
            pnlTargetSafe.Controls.Add(lblTargetTitle);
            tblActual.Controls.Add(pnlTargetSafe, 1, 0);

            // 3. حقل مبلغ التحويل للخزنة
            Panel pnlTransfer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblTransferTitle = new Label { Text = "💸 مبلغ التحويل للخزنة:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            txtTransferAmount = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, Text = "0", TextAlign = HorizontalAlignment.Center, Enabled = false };
            txtTransferAmount.TextChanged += (s, e) => RecalcDiff();
            pnlTransfer.Controls.Add(txtTransferAmount);
            pnlTransfer.Controls.Add(lblTransferTitle);
            tblActual.Controls.Add(pnlTransfer, 2, 0);

            // 4. حقل الفرق (عجز/زيادة)
            Panel pnlDiff = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblDiffTitle = new Label { Text = "⚖️ الفرق (عجز / زيادة):", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            lblDiffVal = new Label { Text = "0.00 ج", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Theme.Accent, TextAlign = ContentAlignment.MiddleCenter };
            pnlDiff.Controls.Add(lblDiffVal);
            pnlDiff.Controls.Add(lblDiffTitle);
            tblActual.Controls.Add(pnlDiff, 3, 0);

            // 5. حقل رصيد الدرج بعد التحويل (الباقي بالدرج)
            Panel pnlRemaining = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblRemainingTitle = new Label { Text = "🪙 الباقي بالدرج بعد التحويل:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            lblRemainingVal = new Label { Text = "0.00 ج", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Theme.TextSub, TextAlign = ContentAlignment.MiddleCenter };
            pnlRemaining.Controls.Add(lblRemainingVal);
            pnlRemaining.Controls.Add(lblRemainingTitle);
            tblActual.Controls.Add(pnlRemaining, 4, 0);

            // 6. حقل الملاحظات
            Panel pnlNotes = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblNotesTitle = new Label { Text = "📝 ملاحظات الإغلاق:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontMain, ForeColor = Theme.TextMain };
            txtNotes = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, BorderStyle = BorderStyle.FixedSingle };
            pnlNotes.Controls.Add(txtNotes);
            pnlNotes.Controls.Add(lblNotesTitle);
            tblActual.Controls.Add(pnlNotes, 5, 0);

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

            // ── 3. شريط التحكم السفلي ──────────────────────────────────
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            btnOpenShift       = Theme.MakeButton("✅ فتح وردية جديدة", Theme.Success, new Point(0, 0), new Size(180, 42));
            btnCloseShift      = Theme.MakeButton("🔒 إغلاق الوردية",   Theme.Danger,  new Point(0, 0), new Size(180, 42));
            btnDetailedReport  = Theme.MakeButton("📊 تقرير تفصيلي",   Theme.Accent,  new Point(0, 0), new Size(160, 42));
            btnPrintReport     = Theme.MakeButton("🖨️ طباعة", Theme.Primary, new Point(0, 0), new Size(120, 42));
            btnRefresh         = Theme.MakeButton("🔄 تحديث", Color.FromArgb(60, 70, 85), new Point(0, 0), new Size(110, 42));

            FlowLayoutPanel flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };
            btnOpenShift.Margin      = new Padding(6, 0, 0, 0);
            btnCloseShift.Margin     = new Padding(6, 0, 0, 0);
            btnDetailedReport.Margin = new Padding(6, 0, 0, 0);
            btnPrintReport.Margin    = new Padding(6, 0, 0, 0);
            btnRefresh.Margin        = new Padding(6, 0, 0, 0);

            btnOpenShift.Click      += BtnOpenShift_Click;
            btnCloseShift.Click     += BtnCloseShift_Click;
            btnDetailedReport.Click += (s, e) => {
                int? sid = _openShift != null ? Convert.ToInt32(_openShift["ShiftID"]) : (int?)null;
                using (var dlg = new FrmShiftReport(sid)) { dlg.ShowDialog(this); }
            };
            btnPrintReport.Click    += BtnPrintReport_Click;
            btnRefresh.Click        += (s, e) => LoadCurrentShift();

            flowBottom.Controls.Add(btnOpenShift);
            flowBottom.Controls.Add(btnCloseShift);
            flowBottom.Controls.Add(btnDetailedReport);
            flowBottom.Controls.Add(btnPrintReport);
            flowBottom.Controls.Add(btnRefresh);
            pnlBottom.Controls.Add(flowBottom);
            
            // Add top and bottom panels first, then tblMain in Fill, then bring tblMain to front so it docks between top and bottom
            this.Controls.Add(pnlBottom);
            this.Controls.Add(tblMain);
            this.Controls.Add(pnlHeader);
            tblMain.BringToFront();
        }

        private Label MakeKpiCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIdx)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(3),
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
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.MiddleCenter
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
                DbHelper.EnsureShiftSchema();
                // Auto-heal any orphan sales in DB created during open shift timeframe
                try
                {
                    DbHelper.Execute(@"
                        UPDATE Sales 
                        SET ShiftID = s.ShiftID 
                        FROM Sales 
                        CROSS JOIN (SELECT TOP 1 ShiftID, OpenTime FROM Shifts WHERE Status = 'Open' ORDER BY ShiftID DESC) s
                        WHERE Sales.ShiftID IS NULL AND Sales.SaleDate >= s.OpenTime");
                }
                catch { }

                DataTable dt;
                try
                {
                    dt = DbHelper.Query(
                        @"SELECT TOP 1 s.*, e.EmpName AS OpenedByName, sa.AccountName AS SafeName 
                          FROM Shifts s 
                          JOIN Employees e ON s.OpenedBy = e.EmpID 
                          LEFT JOIN SafeAccounts sa ON s.SafeAccountID = sa.AccountID 
                          WHERE s.Status = 'Open' ORDER BY s.OpenTime DESC");
                }
                catch
                {
                    dt = DbHelper.Query(
                        @"SELECT TOP 1 s.*, e.EmpName AS OpenedByName, NULL AS SafeName 
                          FROM Shifts s 
                          JOIN Employees e ON s.OpenedBy = e.EmpID 
                          WHERE s.Status = 'Open' ORDER BY s.OpenTime DESC");
                }

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
                DateTime openTime = _openShift != null ? Convert.ToDateTime(_openShift["OpenTime"]) : DateTime.Today;
                int drawerSafeID = _openShift != null && _openShift["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(_openShift["SafeAccountID"]) : (Session.DefaultSafeID ?? 1);

                var dt = DbHelper.Query(@"
                    SELECT
                        ISNULL(SUM(TotalAmount), 0) AS TotalSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Cash' THEN ISNULL(CashPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(CashPaid, 0) ELSE 0 END), 0) AS CashSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Visa' THEN ISNULL(VisaPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(VisaPaid, 0) ELSE 0 END), 0) AS VisaSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Credit' THEN (TotalAmount - ISNULL(CashPaid, 0) - ISNULL(VisaPaid, 0)) WHEN SaleType = 'Mixed' THEN (TotalAmount - ISNULL(CashPaid, 0) - ISNULL(VisaPaid, 0)) ELSE 0 END), 0) AS CreditSales,
                        ISNULL(SUM(CASE WHEN SaleType NOT IN ('Cash','Credit','Visa','Mixed') THEN TotalAmount ELSE 0 END), 0) AS OtherSales
                    FROM Sales 
                    WHERE (ShiftID = @sid OR (ShiftID IS NULL AND SaleDate >= @dt)) AND IsPosted = 1",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime));

                var dtR = DbHelper.Query(@"
                    SELECT ISNULL(SUM(sr.TotalAmount), 0) AS TotalReturns
                    FROM SalesReturns sr
                    JOIN Sales s ON sr.SaleID = s.SaleID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt))",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime));

                var dtExp = DbHelper.Query(@"
                    SELECT 
                        ISNULL(SUM(AmountOut), 0) AS TotalExpenses,
                        ISNULL(SUM(AmountIn), 0) AS TotalCashIn
                    FROM CashBox 
                    WHERE TransDate >= @dt 
                      AND (AccountID = @accId OR AccountID = 1 OR AccountID IS NULL)
                      AND TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')",
                    DbHelper.P("@dt", openTime),
                    DbHelper.P("@accId", drawerSafeID));

                decimal ts  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["TotalSales"])   : 0;
                decimal cs  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["CashSales"])    : 0;
                decimal vs  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["VisaSales"])    : 0;
                decimal cr  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["CreditSales"])  : 0;
                decimal os  = dt.Rows.Count  > 0 ? Convert.ToDecimal(dt.Rows[0]["OtherSales"])   : 0;
                decimal tr  = dtR.Rows.Count > 0 ? Convert.ToDecimal(dtR.Rows[0]["TotalReturns"]): 0;
                decimal ex  = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalExpenses"]) : 0;
                decimal cin = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalCashIn"])   : 0;

                decimal oc = 0m;
                if (_openShift != null && _openShift["OpeningCash"] != DBNull.Value)
                    oc = Convert.ToDecimal(_openShift["OpeningCash"]);

                // مبيعات الفيزا تذهب لحساب الفيزا/البنك ولا تضاف لنقدية الدرج الفعلية
                // إذا تم تحويل نقدية من الفيزا للدرج تظهر تلقائياً ضمن توريدات الدرج (cin)
                decimal expected = oc + cs + cin - tr - ex;

                _summary = new ShiftSummary
                {
                    TotalSales = ts,
                    CashSales = cs,
                    VisaSales = vs,
                    CreditSales = cr,
                    OtherSales = os,
                    TotalReturns = tr,
                    Expenses = ex,
                    TotalCashIn = cin,
                    OpeningCash = oc,
                    Expected = expected
                };

                lblCashSalesVal.Text   = cs.ToString("N2") + " ج";
                lblVisaSalesVal.Text   = vs.ToString("N2") + " ج";
                lblCreditSalesVal.Text = (cr + os).ToString("N2") + " ج";
                lblCashInVal.Text      = cin.ToString("N2") + " ج";
                lblReturnsVal.Text     = tr.ToString("N2") + " ج";
                lblExpensesVal.Text    = ex.ToString("N2") + " ج";
                lblExpectedVal.Text    = expected.ToString("N2") + " ج";
                txtActualCash.Text     = Math.Max(0m, expected).ToString("N2");

                bool canViewDetails = (Session.IsAdmin || Session.CanViewDetails("ShiftClose") || _forceShowDetails);
                if (canViewDetails)
                {
                    lblOpeningCashVal.Text = oc.ToString("N2") + " ج";
                    lblCashSalesVal.Visible = lblVisaSalesVal.Visible = lblCreditSalesVal.Visible =
                    lblCashInVal.Visible = lblReturnsVal.Visible = lblExpensesVal.Visible = lblExpectedVal.Visible = true;
                }
                else
                {
                    lblOpeningCashVal.Text = "غير مصرح";
                    lblCashSalesVal.Visible = lblVisaSalesVal.Visible = lblCreditSalesVal.Visible =
                    lblCashInVal.Visible = lblReturnsVal.Visible = lblExpensesVal.Visible = lblExpectedVal.Visible = false;
                }
                RecalcDiff();
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftClose.LoadShiftSummary", ex); }
        }

        private void LoadShiftMovements(int shiftID, DateTime openTime)
        {
            dgMovements.Rows.Clear();
            bool canViewDetails = (Session.IsAdmin || Session.CanViewDetails("ShiftClose") || _forceShowDetails);
            if (!canViewDetails)
            {
                dgMovements.Visible = false;
                return;
            }

            dgMovements.Visible = true;
            try
            {
                int drawerSafeID = _openShift != null && _openShift["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(_openShift["SafeAccountID"]) : (Session.DefaultSafeID ?? 1);

                var dtSales = DbHelper.Query(@"
                    SELECT 
                        CASE 
                            WHEN s.SaleType = 'Cash' THEN N'مبيعات نقدي (كاش)'
                            WHEN s.SaleType = 'Visa' THEN N'مبيعات فيزا/شبكة'
                            WHEN s.SaleType = 'Credit' THEN N'مبيعات آجل'
                            WHEN s.SaleType = 'Mixed' THEN N'مبيعات مختلط (كاش+فيزا)'
                            ELSE N'مبيعات'
                        END AS TransType, 
                        s.SaleCode AS RefCode, 
                        s.SaleDate AS TransTime, 
                        ISNULL(c.ClientName, N'عميل نقدي') + 
                        CASE 
                            WHEN s.SaleType = 'Visa' THEN N' (فيزا - ' + ISNULL(sa.AccountName, N'حساب فيزا') + N')'
                            WHEN s.SaleType = 'Mixed' THEN N' (كاش: ' + CAST(ISNULL(s.CashPaid,0) AS NVARCHAR) + N' + فيزا: ' + CAST(ISNULL(s.VisaPaid,0) AS NVARCHAR) + N')'
                            ELSE N''
                        END AS Details, 
                        s.TotalAmount AS Amount
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    LEFT JOIN SafeAccounts sa ON s.VisaAccountID = sa.AccountID
                    WHERE s.ShiftID=@sid AND s.IsPosted=1
                    UNION ALL
                    SELECT 'مرتجع' AS TransType, CAST(sr.ReturnID AS NVARCHAR) AS RefCode, sr.ReturnDate AS TransTime, 'مرتجع فاتورة' AS Details, sr.TotalAmount AS Amount
                    FROM SalesReturns sr JOIN Sales s ON sr.SaleID=s.SaleID WHERE s.ShiftID=@sid
                    UNION ALL
                    SELECT 
                        CASE 
                            WHEN TransType = 'ClientPayment' THEN N'تحصيل من عميل (+)'
                            WHEN TransType = 'SupplierPayment' THEN N'صرف لمورد (-)'
                            WHEN TransType = 'EmpAdvance' THEN N'سلفة موظف (-)'
                            WHEN TransType = 'EmpPaymentOut' THEN N'راتب/مستحقات (-)'
                            WHEN TransType = 'EmpPaymentIn' THEN N'توريد موظف (+)'
                            WHEN TransType = 'ReceiptIn' THEN N'سند قبض (+)'
                            WHEN TransType = 'ReceiptOut' THEN N'سند صرف (-)'
                            WHEN TransType = 'Transfer' AND AmountIn > 0 THEN N'تحويل وارد للدرج (+)'
                            WHEN TransType = 'Transfer' AND AmountOut > 0 THEN N'تحويل صادر من الدرج (-)'
                            WHEN AmountIn > 0 THEN N'وارد للدرج (+)'
                            ELSE N'مصروفات (-)'
                        END AS TransType, 
                        CAST(CashID AS NVARCHAR) AS RefCode, 
                        TransDate AS TransTime, 
                        Notes AS Details, 
                        CASE WHEN AmountIn > 0 THEN AmountIn ELSE -AmountOut END AS Amount
                    FROM CashBox 
                    WHERE TransDate >= @dt 
                      AND (AccountID = @accId OR AccountID = 1 OR AccountID IS NULL)
                      AND TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')
                    ORDER BY TransTime DESC",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime), DbHelper.P("@accId", drawerSafeID));

                foreach (DataRow r in dtSales.Rows)
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
            catch { }
        }

        private void OnActualCashChanged()
        {
            if (decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual))
            {
                if (cboTargetSafe != null && cboTargetSafe.SelectedItem is ComboItem si && si.ID > 0)
                {
                    txtTransferAmount.Text = actual.ToString("N2");
                }
            }
            RecalcDiff();
        }

        private void OnTargetSafeChanged()
        {
            if (cboTargetSafe != null && cboTargetSafe.SelectedItem is ComboItem si && si.ID > 0)
            {
                txtTransferAmount.Enabled = true;
                if (decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual) && actual > 0)
                {
                    if (string.IsNullOrWhiteSpace(txtTransferAmount.Text) || txtTransferAmount.Text == "0")
                        txtTransferAmount.Text = actual.ToString("N2");
                }
            }
            else
            {
                txtTransferAmount.Text = "0.00";
                txtTransferAmount.Enabled = false;
            }
            RecalcDiff();
        }

        private void RecalcDiff()
        {
            if (_summary == null) return;
            bool canViewDetails = (Session.IsAdmin || Session.CanViewDetails("ShiftClose") || _forceShowDetails);

            if (!decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual)) return;

            if (!canViewDetails)
            {
                lblDiffVal.Text = "🔒 (إغلاق أعمى)";
                lblDiffVal.ForeColor = Theme.TextSub;
                lblRemainingVal.Text = "🔒";
                lblRemainingVal.ForeColor = Theme.TextSub;
                return;
            }

            // 1. العجز والزيادة يتحسب من إجمالي الفعلي بالدرج مقارنة بالمبلغ المتوقع
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

            // 2. حساب المبلغ المحوّل والباقي بالدرج بعد التحويل
            bool isTransferring = cboTargetSafe != null &&
                                  cboTargetSafe.SelectedItem is ComboItem si && si.ID > 0;
            decimal transferred = 0;
            if (isTransferring)
            {
                decimal.TryParse(txtTransferAmount.Text.Replace(",", ""), out transferred);
                if (transferred > actual) transferred = actual;
                if (transferred < 0) transferred = 0;
            }

            decimal remaining = actual - transferred;

            // رصيد الدرج بعد التحويل (الباقي بالدرج)
            lblRemainingVal.Text = remaining.ToString("N2") + " ج";
            lblRemainingVal.ForeColor = remaining == 0 ? Theme.TextSub : Theme.Success;
        }

        private void ClearSummary()
        {
            _summary = null;
            lblOpeningCashVal.Text = lblCashSalesVal.Text = lblVisaSalesVal.Text = lblCreditSalesVal.Text =
            lblCashInVal.Text = lblReturnsVal.Text = lblExpensesVal.Text = lblExpectedVal.Text = lblDiffVal.Text = "---";
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
            if (!decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual) || actual < 0)
            {
                MessageBox.Show("الرجاء إدخال المبلغ الفعلي الموجود بالخزنة بشكل صحيح (لا يمكن أن يكون سالباً).", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("هل أنت تأكد من إغلاق الوردية الحالية وتسوية الحسابات وتحويل النقدية؟", "تأكيد إغلاق الوردية", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
                decimal diff = actual - (_summary?.Expected ?? 0);

                int sourceAccountID = _openShift["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(_openShift["SafeAccountID"]) : (Session.DefaultSafeID ?? 1);
                string sourceDrawerName = _openShift["SafeName"] != DBNull.Value ? _openShift["SafeName"].ToString().Replace(" / الدرج", "").Replace("/الدرج", "").Trim() : "الخزينة";

                int targetSafeID = 0;
                string targetSafeName = "";
                if (cboTargetSafe != null && cboTargetSafe.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
                {
                    targetSafeID = safeItem.ID;
                    targetSafeName = safeItem.Text;
                }

                decimal transferred = 0;
                if (targetSafeID > 0 && targetSafeID != sourceAccountID)
                {
                    decimal.TryParse(txtTransferAmount.Text.Replace(",", ""), out transferred);
                    if (transferred > actual) transferred = actual;
                    if (transferred < 0) transferred = 0;
                }

                decimal remainingInDrawer = Math.Max(0m, actual - transferred);

                DbHelper.Execute(@"
                    UPDATE Shifts SET 
                        CloseTime = GETDATE(),
                        ClosedBy = @emp,
                        TotalSales = @ts,
                        CashSales = @cs,
                        VisaSales = @vs,
                        OtherSales = @os,
                        TotalReturns = @tr,
                        ExpectedCash = @exp,
                        ActualCash = @act,
                        Difference = @diff,
                        TransferToSafeID = @tsafe,
                        TransferredAmount = @tamt,
                        RemainingInDrawer = @rem,
                        Notes = @n,
                        Status = 'Closed' 
                    WHERE ShiftID = @sid",
                    DbHelper.P("@emp", Session.EmpID),
                    DbHelper.P("@ts", _summary?.TotalSales ?? 0),
                    DbHelper.P("@cs", _summary?.CashSales ?? 0),
                    DbHelper.P("@vs", _summary?.VisaSales ?? 0),
                    DbHelper.P("@os", _summary?.OtherSales ?? 0),
                    DbHelper.P("@tr", _summary?.TotalReturns ?? 0),
                    DbHelper.P("@exp", _summary?.Expected ?? 0),
                    DbHelper.P("@act", actual),
                    DbHelper.P("@diff", diff),
                    DbHelper.P("@tsafe", targetSafeID > 0 ? (object)targetSafeID : DBNull.Value),
                    DbHelper.P("@tamt", transferred),
                    DbHelper.P("@rem", remainingInDrawer),
                    DbHelper.P("@n", txtNotes.Text.Trim()),
                    DbHelper.P("@sid", shiftID));

                // ── 1. تسوية العجز أو الزيادة بالدرج أولاً لضبط رصيد الدرج بدقة 100% ──
                if (diff < 0)
                {
                    DbHelper.Execute(
                        @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy)
                          VALUES (GETDATE(), 'ShiftDeficit', 0, @amt, @acc, @notes, @uid)",
                        DbHelper.P("@amt", Math.Abs(diff)),
                        DbHelper.P("@acc", sourceAccountID),
                        DbHelper.P("@notes", $"عجز تقفيل وردية #{shiftID} (الكاشير: {Session.EmpName})"),
                        DbHelper.P("@uid", Session.EmpID));
                }
                else if (diff > 0)
                {
                    DbHelper.Execute(
                        @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy)
                          VALUES (GETDATE(), 'ShiftSurplus', @amt, 0, @acc, @notes, @uid)",
                        DbHelper.P("@amt", diff),
                        DbHelper.P("@acc", sourceAccountID),
                        DbHelper.P("@notes", $"زيادة تقفيل وردية #{shiftID} (الكاشير: {Session.EmpName})"),
                        DbHelper.P("@uid", Session.EmpID));
                }

                // ── 2. تسجيل حركة سند تحويل النقدية المحولة فقط إلى الخزنة المستهدفة ──
                if (transferred > 0 && targetSafeID > 0 && targetSafeID != sourceAccountID)
                {
                    string cleanTargetName = targetSafeName.Replace("🏦 تحويل إلى: ", "").Trim();
                    // أ) حركة صادر من الدرج بمبلغ التحويل المحدد خصماً من رصيد الدرج
                    DbHelper.Execute(
                        @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy, RefID)
                          VALUES (GETDATE(), 'Transfer', 0, @amt, @acc, @notes, @uid, @ref)",
                        DbHelper.P("@amt", transferred),
                        DbHelper.P("@acc", sourceAccountID),
                        DbHelper.P("@notes", $"تقفيل وردية #{shiftID} - تحويل صادر من [{sourceDrawerName}] إلى [{cleanTargetName}]"),
                        DbHelper.P("@uid", Session.EmpID),
                        DbHelper.P("@ref", shiftID));

                    // ب) حركة وارد إلى الخزنة المستهدفة إضافةً إلى رصيد الخزنة
                    DbHelper.Execute(
                        @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy, RefID)
                          VALUES (GETDATE(), 'Transfer', @amt, 0, @acc, @notes, @uid, @ref)",
                        DbHelper.P("@amt", transferred),
                        DbHelper.P("@acc", targetSafeID),
                        DbHelper.P("@notes", $"تقفيل وردية #{shiftID} - تحويل وارد إلى [{cleanTargetName}] من [{sourceDrawerName}] (الكاشير: {Session.EmpName})"),
                        DbHelper.P("@uid", Session.EmpID),
                        DbHelper.P("@ref", shiftID));
                }

                // ── 3. سند تقفيل يومية للرصيد المتبقي الفعلي بالدرج ──
                DbHelper.Execute(
                    @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy)
                      VALUES (GETDATE(), 'ShiftClose', 0, 0, @acc, @notes, @uid)",
                    DbHelper.P("@acc", sourceAccountID),
                    DbHelper.P("@notes", $"سند تقفيل وردية #{shiftID} - الرصيد الفعلي المتبقي بالدرج: {remainingInDrawer:N2} ج (الكاشير: {Session.EmpName})"),
                    DbHelper.P("@uid", Session.EmpID));

                Session.CurrentShiftID = null;

                if (MessageBox.Show("✅ تم إغلاق الوردية وتوريد النقدية وتسوية الحسابات بنجاح!\nهل تريد طباعة تقرير إغلاق الوردية الآن؟", "إغلاق الوردية", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    FrmPrintShift.ShowPrintOptions(shiftID, btnCloseShift);
                }

                var resStartNew = MessageBox.Show(
                    "🔄 هل ترغب في فتح وردية عمل جديدة فوراً للكاشير التالي لتواصل العمل بدون توقف؟",
                    "بداية وردية جديدة تلقائياً",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resStartNew == DialogResult.Yes)
                {
                    using (var frm = new FrmOpenShift())
                    {
                        frm.ShowDialog();
                    }
                }

                LoadCurrentShift();
            }
            catch (Exception ex) { MessageBox.Show("خطأ عند إغلاق الوردية:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void LoadTargetSafes()
        {
            try
            {
                DataTable dt = AccountDAL.GetActiveSafeAccounts();
                if (cboTargetSafe == null) return;
                cboTargetSafe.Items.Clear();
                cboTargetSafe.Items.Add(new ComboItem(0, "📌 إبقاء بالدرج (رصيد افتتاحي للوردية القادمة)"));

                int defaultTargetIdx = 0;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow r = dt.Rows[i];
                    int id = Convert.ToInt32(r["AccountID"]);
                    string name = r["AccountName"].ToString();
                    var item = new ComboItem(id, $"🏦 تحويل إلى: {name}");
                    int added = cboTargetSafe.Items.Add(item);

                    if (name.Contains("الرئيسية") || name.Contains("الخزنة") || name.Contains("العامة"))
                    {
                        defaultTargetIdx = added;
                    }
                }
                cboTargetSafe.DisplayMember = "Text";
                if (cboTargetSafe.Items.Count > 0) cboTargetSafe.SelectedIndex = defaultTargetIdx > 0 ? defaultTargetIdx : (cboTargetSafe.Items.Count > 1 ? 1 : 0);
            }
            catch { }
        }

        private void BtnPrintReport_Click(object sender, EventArgs e)
        {
            if (_openShift == null) return;
            int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
            FrmPrintShift.ShowPrintOptions(shiftID, btnPrintReport);
        }

        private class ShiftSummary
        {
            public decimal TotalSales, CashSales, VisaSales, CreditSales, OtherSales, TotalReturns, Expenses, TotalCashIn, OpeningCash, Expected;
        }
    }
}
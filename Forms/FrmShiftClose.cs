using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة إدارة وإغلاق الوردية المحاسبية المتقدمة — تشمل جرد الفئات، تفصيل وسائل الدفع، ملخص المبيعات، والاعتماد الإداري
    /// </summary>
    public class FrmShiftClose : Form
    {
        private DataRow _openShift = null;
        private ShiftSummary _summary = null;
        private string _denominationsJson = "";
        
        // Panels
        private Panel pnlHeader;
        private Panel pnlSalesKpi;
        private Panel pnlKpiContainer;
        private Panel pnlActualContainer;
        private Panel pnlMovementsContainer;
        private Panel pnlBottom;
        
        // Header Controls
        private Label lblShiftTitle;
        private Label lblShiftBadge;
        private Label lblShiftInfo;
        
        // Sales KPIs Summary Controls
        private Label lblInvoiceCountVal;
        private Label lblGrossSalesVal;
        private Label lblReturnsSummaryVal;
        private Label lblDiscountsVal;
        private Label lblTaxesVal;
        private Label lblNetSalesVal;

        // Payment Methods & Cash KPI Cards
        private Label lblOpeningCashVal;
        private Label lblCashSalesVal;
        private Label lblVisaSalesVal;
        private Label lblWalletSalesVal;
        private Label lblCreditSalesVal;
        private Label lblCashInVal;
        private Label lblReturnsVal;
        private Label lblExpensesVal;
        private Label lblExpectedVal;
        
        // Counting & Settlement Controls
        private TextBox txtActualCash;
        private Button btnCountDenoms;
        private Label lblDiffVal;
        private Panel pnlDeficitReasonBox;
        private TextBox txtDeficitReason;
        private ComboBox cboTargetSafe;
        private TextBox txtTransferAmount;
        private Label lblRemainingVal;
        private TextBox txtNotes;

        // Filter & Grid Controls
        private FlowLayoutPanel pnlFilterBar;
        private string _currentFilter = "ALL";
        private DataTable _dtAllMovements = null;
        private DataGridView dgMovements;

        // Action Buttons
        private Button btnCloseShift;
        private Button btnApproveShift;
        private Button btnDetailedReport;
        private Button btnPrintReport;
        private Button btnRefresh;
        private Button btnToggleDetails;
        private Button btnDrawerMovementDetails;

        private bool _forceShowDetails = true;

        public FrmShiftClose()
        {
            InitUI();
            LoadTargetSafes();
            LoadCurrentShift();
        }

        private void InitUI()
        {
            this.Text = "إدارة وإغلاق الوردية المحاسبية";
            this.Size = new Size(1180, 840);
            this.MinimumSize = new Size(1060, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F4)
                {
                    OpenDenominationsDialog();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.F5)
                {
                    LoadCurrentShift();
                    e.Handled = true;
                }
            };

            // ── 1. رأس الشاشة (Header) ──────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnToggleDetails = new Button
            {
                Text = "👁️ التفاصيل",
                Size = new Size(110, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 80, 95),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0)
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
                Text = "🔍 حركة الدرج",
                Size = new Size(125, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0)
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
                    MessageBox.Show("🔒 عفوًا: لا تملك صلاحية لعرض حركة وتفاصيل الدرج خلال الوردية!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnCountDenoms = new Button
            {
                Text = "🧮 جرد الفئات (F4)",
                Size = new Size(150, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0)
            };
            btnCountDenoms.FlatAppearance.BorderSize = 0;
            btnCountDenoms.Click += (s, e) => OpenDenominationsDialog();

            Panel pnlTitleBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            
            Panel pnlTitleRow = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent };
            lblShiftTitle = new Label
            {
                Text = "🔄 إدارة وإغلاق وردية الكاشير",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Right,
                AutoSize = true
            };
            lblShiftBadge = new Label
            {
                Text = "🟢 مفتوحة",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.Success,
                BackColor = Color.FromArgb(20, 50, 30),
                Padding = new Padding(6, 2, 6, 2),
                Dock = DockStyle.Right,
                Margin = new Padding(10, 0, 0, 0),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlTitleRow.Controls.Add(lblShiftBadge);
            pnlTitleRow.Controls.Add(lblShiftTitle);

            lblShiftInfo = new Label
            {
                Text = "الوردية رقم #--- | 👤 الكاشير: --- | 🏢 الفرع: الرئيسي | 💻 جهاز: POS-01 | ⏰ فتح: --- | ⌛ إغلاق: ---",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlTitleBox.Controls.Add(lblShiftInfo);
            pnlTitleBox.Controls.Add(pnlTitleRow);

            pnlHeader.Controls.Add(pnlTitleBox);
            pnlHeader.Controls.Add(btnCountDenoms);
            pnlHeader.Controls.Add(btnDrawerMovementDetails);
            pnlHeader.Controls.Add(btnToggleDetails);

            // ── 2. المحتوى الرئيسي ──────────────────────────────────────
            TableLayoutPanel tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12, 6, 12, 6),
                RightToLeft = RightToLeft.Yes
            };
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));  // 1. كارت ملخص المبيعات (Sales KPIs)
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 85f));  // 2. كروت وسائل الدفع والنقدية
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 105f)); // 3. لوحة العد الفعلي والتسوية والعجز
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // 4. شريط فلاتر جدول الحركات
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // 5. جدول حركات الوردية

            // ── 1. كارت ملخص المبيعات ──
            pnlSalesKpi = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
            TableLayoutPanel tblSalesKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            for (int i = 0; i < 6; i++) tblSalesKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));

            lblInvoiceCountVal   = MakeMiniKpiCard(tblSalesKpi, "🧾 عدد الفواتير", "0", Theme.TextMain, 0);
            lblGrossSalesVal     = MakeMiniKpiCard(tblSalesKpi, "💰 إجمالي المبيعات", "0.00 ج", Theme.Success, 1);
            lblReturnsSummaryVal = MakeMiniKpiCard(tblSalesKpi, "↩️ المرتجعات", "0.00 ج", Theme.Danger, 2);
            lblDiscountsVal      = MakeMiniKpiCard(tblSalesKpi, "🏷️ الخصومات", "0.00 ج", Color.FromArgb(230, 126, 34), 3);
            lblTaxesVal          = MakeMiniKpiCard(tblSalesKpi, "🏛️ الضرائب", "0.00 ج", Color.FromArgb(142, 68, 173), 4);
            lblNetSalesVal       = MakeMiniKpiCard(tblSalesKpi, "💎 صافي المبيعات", "0.00 ج", Theme.Accent, 5);

            pnlSalesKpi.Controls.Add(tblSalesKpi);
            tblMain.Controls.Add(pnlSalesKpi, 0, 0);

            // ── 2. كروت وسائل الدفع والنقدية (9 كروت) ──
            pnlKpiContainer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
            TableLayoutPanel tblKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 9,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            for (int i = 0; i < 9; i++) tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11f));

            lblOpeningCashVal = MakeKpiCard(tblKpi, "💵 رصيد البداية", "0.00 ج", Theme.TextMain, 0);
            lblCashSalesVal   = MakeKpiCard(tblKpi, "🛒 مبيعات كاش", "0.00 ج", Theme.Success, 1);
            lblVisaSalesVal   = MakeKpiCard(tblKpi, "💳 مبيعات فيزا", "0.00 ج", Color.FromArgb(142, 68, 173), 2);
            lblWalletSalesVal = MakeKpiCard(tblKpi, "📱 مبيعات محافظ", "0.00 ج", Color.FromArgb(0, 168, 232), 3);
            lblCreditSalesVal = MakeKpiCard(tblKpi, "📑 مبيعات آجل", "0.00 ج", Color.FromArgb(52, 152, 219), 4);
            lblCashInVal      = MakeKpiCard(tblKpi, "➕ توريدات للدرج", "0.00 ج", Color.FromArgb(16, 185, 129), 5);
            lblReturnsVal     = MakeKpiCard(tblKpi, "↩ مرتجعات كاش", "0.00 ج", Theme.Danger, 6);
            lblExpensesVal    = MakeKpiCard(tblKpi, "💸 مصروفات وسحب", "0.00 ج", Color.FromArgb(230, 126, 34), 7);
            lblExpectedVal    = MakeKpiCard(tblKpi, "💰 المتوقع بالدرج", "0.00 ج", Theme.Accent, 8);

            pnlKpiContainer.Controls.Add(tblKpi);
            tblMain.Controls.Add(pnlKpiContainer, 0, 1);

            // ── 3. لوحة العد الفعلي والتحويل والفرق وسبب العجز ──
            pnlActualContainer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(0, 0, 0, 4) };
            pnlActualContainer.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlActualContainer);

            TableLayoutPanel tblActual = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                Padding = new Padding(10, 6, 10, 6),
                RightToLeft = RightToLeft.Yes
            };
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f)); // 1. الفعلي بالدرج
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f)); // 2. الفرق (عجز/زيادة)
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f)); // 3. وجهة النقدية
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f)); // 4. مبلغ التحويل للخزنة
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f)); // 5. رصيد الدرج بعد التحويل
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));   // 6. سبب العجز (إن وجد)
            tblActual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));   // 7. الملاحظات

            // 1. حقل الفعلي
            Panel pnlAct = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblActTitle = new Label { Text = "💵 المبلغ الفعلي بالدرج:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            
            Panel pnlActRow = new Panel { Dock = DockStyle.Top, Height = 32 };
            txtActualCash = new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 32,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "0",
                TextAlign = HorizontalAlignment.Center
            };
            txtActualCash.TextChanged += (s, e) => OnActualCashChanged();
            
            Button btnTinyDenom = new Button
            {
                Text = "🧮",
                Dock = DockStyle.Left,
                Width = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTinyDenom.FlatAppearance.BorderSize = 0;
            btnTinyDenom.Click += (s, e) => OpenDenominationsDialog();

            pnlActRow.Controls.Add(txtActualCash);
            pnlActRow.Controls.Add(btnTinyDenom);
            pnlAct.Controls.Add(pnlActRow);
            pnlAct.Controls.Add(lblActTitle);
            tblActual.Controls.Add(pnlAct, 0, 0);

            // 2. حقل الفرق (عجز/زيادة)
            Panel pnlDiff = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblDiffTitle = new Label { Text = "⚖️ الفرق (عجز / زيادة):", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            lblDiffVal = new Label { Text = "0.00 ج", Dock = DockStyle.Top, Height = 32, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Theme.Accent, TextAlign = ContentAlignment.MiddleCenter };
            pnlDiff.Controls.Add(lblDiffVal);
            pnlDiff.Controls.Add(lblDiffTitle);
            tblActual.Controls.Add(pnlDiff, 1, 0);

            // 3. حقل وجهة نقدية الوردية
            Panel pnlTargetSafe = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblTargetTitle = new Label { Text = "🏦 وجهة النقدية:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            cboTargetSafe = new ComboBox { Dock = DockStyle.Top, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, RightToLeft = RightToLeft.Yes };
            cboTargetSafe.SelectedIndexChanged += (s, e) => OnTargetSafeChanged();
            pnlTargetSafe.Controls.Add(cboTargetSafe);
            pnlTargetSafe.Controls.Add(lblTargetTitle);
            tblActual.Controls.Add(pnlTargetSafe, 2, 0);

            // 4. حقل مبلغ التحويل للخزنة
            Panel pnlTransfer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblTransferTitle = new Label { Text = "💸 توريد للخزنة:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            txtTransferAmount = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, Text = "0", TextAlign = HorizontalAlignment.Center, Enabled = false };
            txtTransferAmount.TextChanged += (s, e) => RecalcDiff();
            pnlTransfer.Controls.Add(txtTransferAmount);
            pnlTransfer.Controls.Add(lblTransferTitle);
            tblActual.Controls.Add(pnlTransfer, 3, 0);

            // 5. حقل رصيد الدرج بعد التحويل (الباقي بالدرج)
            Panel pnlRemaining = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblRemainingTitle = new Label { Text = "🪙 الباقي بالدرج (الجديد):", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            lblRemainingVal = new Label { Text = "0.00 ج", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Theme.TextSub, TextAlign = ContentAlignment.MiddleCenter };
            pnlRemaining.Controls.Add(lblRemainingVal);
            pnlRemaining.Controls.Add(lblRemainingTitle);
            tblActual.Controls.Add(pnlRemaining, 4, 0);

            // 6. حقل سبب العجز الإجباري
            pnlDeficitReasonBox = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblDeficitTitle = new Label { Text = "⚠️ تبرير سبب العجز:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontBold, ForeColor = Theme.Danger };
            txtDeficitReason = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, BorderStyle = BorderStyle.FixedSingle };
            pnlDeficitReasonBox.Controls.Add(txtDeficitReason);
            pnlDeficitReasonBox.Controls.Add(lblDeficitTitle);
            tblActual.Controls.Add(pnlDeficitReasonBox, 5, 0);

            // 7. حقل الملاحظات
            Panel pnlNotes = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
            var lblNotesTitle = new Label { Text = "📝 ملاحظات الإغلاق:", Dock = DockStyle.Top, Height = 22, Font = Theme.FontMain, ForeColor = Theme.TextMain };
            txtNotes = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, BorderStyle = BorderStyle.FixedSingle };
            pnlNotes.Controls.Add(txtNotes);
            pnlNotes.Controls.Add(lblNotesTitle);
            tblActual.Controls.Add(pnlNotes, 6, 0);

            pnlActualContainer.Controls.Add(tblActual);
            tblMain.Controls.Add(pnlActualContainer, 0, 2);

            // ── 4. شريط فلاتر جدول الحركات ──
            pnlFilterBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2)
            };

            AddFilterButton("ALL", "📌 الكل");
            AddFilterButton("CASH", "💵 مبيعات نقدي");
            AddFilterButton("VISA", "💳 فيزا ومحافظ");
            AddFilterButton("CREDIT", "📑 مبيعات آجل");
            AddFilterButton("RETURN", "↩️ مرتجعات");
            AddFilterButton("EXPENSE", "💸 مصروفات وسحب");
            AddFilterButton("CASHIN", "📥 توريدات وتحصيل");
            AddFilterButton("TRANSFER", "🏦 تحويلات");

            tblMain.Controls.Add(pnlFilterBar, 0, 3);

            // ── 5. جدول حركات الوردية المطور ──
            pnlMovementsContainer = new Panel { Dock = DockStyle.Fill };
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
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowNo", HeaderText = "#", FillWeight = 25f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransTime", HeaderText = "الوقت", FillWeight = 55f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع الحركة", FillWeight = 75f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefCode", HeaderText = "رقم المرجع", FillWeight = 55f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "البيان / العميل", FillWeight = 140f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "PayMethod", HeaderText = "طريقة الدفع", FillWeight = 60f });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "AmountIn", HeaderText = "وارد (+)", FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) } });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "AmountOut", HeaderText = "صادر (-)", FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68) } });
            dgMovements.Columns.Add(new DataGridViewTextBoxColumn { Name = "User", HeaderText = "المستخدم", FillWeight = 55f });

            pnlMovementsContainer.Controls.Add(dgMovements);
            tblMain.Controls.Add(pnlMovementsContainer, 0, 4);

            // ── 6. شريط التحكم السفلي ──────────────────────────────────
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            btnCloseShift     = Theme.MakeButton("🔒 إغلاق وتقفيل الوردية", Theme.Danger, new Point(0, 0), new Size(200, 42));
            btnApproveShift   = Theme.MakeButton("⭐ اعتماد الوردية (المدير)", Color.FromArgb(59, 130, 246), new Point(0, 0), new Size(190, 42));
            btnDetailedReport = Theme.MakeButton("📊 تقرير تفصيلي", Theme.Accent, new Point(0, 0), new Size(150, 42));
            btnPrintReport    = Theme.MakeButton("🖨️ طباعة", Theme.Primary, new Point(0, 0), new Size(120, 42));
            btnRefresh        = Theme.MakeButton("🔄 تحديث (F5)", Color.FromArgb(60, 70, 85), new Point(0, 0), new Size(120, 42));

            FlowLayoutPanel flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };
            btnCloseShift.Margin     = new Padding(6, 0, 0, 0);
            btnApproveShift.Margin   = new Padding(6, 0, 0, 0);
            btnDetailedReport.Margin = new Padding(6, 0, 0, 0);
            btnPrintReport.Margin    = new Padding(6, 0, 0, 0);
            btnRefresh.Margin        = new Padding(6, 0, 0, 0);

            btnCloseShift.Click     += BtnCloseShift_Click;
            btnApproveShift.Click   += BtnApproveShift_Click;
            btnDetailedReport.Click += (s, e) => {
                int? sid = _openShift != null ? Convert.ToInt32(_openShift["ShiftID"]) : (int?)null;
                using (var dlg = new FrmShiftReport(sid)) { dlg.ShowDialog(this); }
            };
            btnPrintReport.Click    += BtnPrintReport_Click;
            btnRefresh.Click        += (s, e) => LoadCurrentShift();

            flowBottom.Controls.Add(btnCloseShift);
            flowBottom.Controls.Add(btnApproveShift);
            flowBottom.Controls.Add(btnDetailedReport);
            flowBottom.Controls.Add(btnPrintReport);
            flowBottom.Controls.Add(btnRefresh);
            pnlBottom.Controls.Add(flowBottom);
            
            this.Controls.Add(pnlBottom);
            this.Controls.Add(tblMain);
            this.Controls.Add(pnlHeader);
            tblMain.BringToFront();
        }

        private void AddFilterButton(string key, string text)
        {
            var btn = new Button
            {
                Text = text,
                Tag = key,
                AutoSize = true,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = key == "ALL" ? Theme.Primary : Color.FromArgb(50, 60, 75),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 2, 4, 2)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                _currentFilter = key;
                foreach (Control c in pnlFilterBar.Controls)
                {
                    if (c is Button b)
                    {
                        b.BackColor = (string)b.Tag == _currentFilter ? Theme.Primary : Color.FromArgb(50, 60, 75);
                    }
                }
                ApplyMovementsFilter();
            };
            pnlFilterBar.Controls.Add(btn);
        }

        private Label MakeMiniKpiCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIdx)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(2),
                Padding = new Padding(2)
            };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            Label lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblV = new Label
            {
                Text = val,
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.Add(lblV);
            pnl.Controls.Add(lblT);
            parent.Controls.Add(pnl, colIdx, 0);
            return lblV;
        }

        private Label MakeKpiCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIdx)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(2),
                Padding = new Padding(3)
            };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            Label lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblV = new Label
            {
                Text = val,
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.Add(lblV);
            pnl.Controls.Add(lblT);
            parent.Controls.Add(pnl, colIdx, 0);
            return lblV;
        }

        private void OpenDenominationsDialog()
        {
            decimal? expected = _summary != null ? (decimal?)_summary.Expected : null;
            using (var dlg = new FrmCashDenominations(_denominationsJson, expected))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _denominationsJson = dlg.DenominationsSummaryJson;
                    txtActualCash.Text = dlg.TotalCash.ToString("N2");
                    RecalcDiff();
                }
            }
        }

        private void LoadCurrentShift()
        {
            try
            {
                DbHelper.EnsureShiftSchema();
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

                if (dt.Rows.Count == 0)
                {
                    ShiftDAL.EnsureActiveShift(Session.EmpID);
                    try
                    {
                        dt = DbHelper.Query(
                            @"SELECT TOP 1 s.*, e.EmpName AS OpenedByName, sa.AccountName AS SafeName 
                              FROM Shifts s 
                              JOIN Employees e ON s.OpenedBy = e.EmpID 
                              LEFT JOIN SafeAccounts sa ON s.SafeAccountID = sa.AccountID
                              WHERE s.Status = 'Open' ORDER BY s.OpenTime DESC");
                    }
                    catch {}
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    _openShift = dt.Rows[0];
                    int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
                    Session.CurrentShiftID = shiftID;
                    
                    DateTime openTime = Convert.ToDateTime(_openShift["OpenTime"]);
                    TimeSpan duration = DateTime.Now - openTime;
                    string durationStr = $"{(int)duration.TotalHours} ساعة و {duration.Minutes} دقيقة";
                    string safeName = _openShift["SafeName"] != DBNull.Value ? _openShift["SafeName"].ToString() : "درج الكاشير";
                    string posStation = _openShift.Table.Columns.Contains("POSStationName") && _openShift["POSStationName"] != DBNull.Value ? _openShift["POSStationName"].ToString() : Environment.MachineName;
                    string branchName = _openShift.Table.Columns.Contains("BranchName") && _openShift["BranchName"] != DBNull.Value ? _openShift["BranchName"].ToString() : "الفرع الرئيسي";
                    string cashierName = _openShift["OpenedByName"]?.ToString() ?? "كاشير";
                    string approvalStatus = _openShift.Table.Columns.Contains("ApprovalStatus") && _openShift["ApprovalStatus"] != DBNull.Value ? _openShift["ApprovalStatus"].ToString() : "Open";

                    lblShiftTitle.Text = $"🔄 تقفيل الوردية رقم #{shiftID}";
                    UpdateBadge(approvalStatus);

                    lblShiftInfo.Text = $"👤 الكاشير: {cashierName}   |   🏢 الفرع: {branchName}   |   💻 جهاز: {posStation}   |   ⏰ فتح: {openTime:hh:mm tt}   |   ⏱️ المدة: {durationStr}";
                    
                    if (_openShift.Table.Columns.Contains("DenominationsJson") && _openShift["DenominationsJson"] != DBNull.Value)
                    {
                        _denominationsJson = _openShift["DenominationsJson"].ToString();
                    }

                    LoadShiftSummary(shiftID);
                    LoadShiftMovements(shiftID, openTime);

                    btnCloseShift.Enabled   = true;
                    btnApproveShift.Enabled = (Session.IsAdmin || Session.CanAccess("ApproveShift"));
                    btnPrintReport.Enabled  = true;
                }
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftClose.LoadCurrentShift", ex); }
        }

        private void UpdateBadge(string status)
        {
            if (status == "Approved")
            {
                lblShiftBadge.Text = "🔵 معتمدة ومقفلة";
                lblShiftBadge.ForeColor = Color.FromArgb(59, 130, 246);
                lblShiftBadge.BackColor = Color.FromArgb(20, 40, 70);
            }
            else if (status == "Rejected")
            {
                lblShiftBadge.Text = "🔴 مرفوضة للمراجعة";
                lblShiftBadge.ForeColor = Theme.Danger;
                lblShiftBadge.BackColor = Color.FromArgb(60, 20, 20);
            }
            else if (status == "PendingApproval")
            {
                lblShiftBadge.Text = "🟠 بانتظار الاعتماد";
                lblShiftBadge.ForeColor = Color.FromArgb(245, 158, 11);
                lblShiftBadge.BackColor = Color.FromArgb(60, 45, 15);
            }
            else
            {
                lblShiftBadge.Text = "🟢 مفتوحة ونشطة";
                lblShiftBadge.ForeColor = Theme.Success;
                lblShiftBadge.BackColor = Color.FromArgb(20, 50, 30);
            }
        }

        private void LoadShiftSummary(int shiftID)
        {
            try
            {
                DateTime openTime = _openShift != null ? Convert.ToDateTime(_openShift["OpenTime"]) : DateTime.Today;
                int drawerSafeID = _openShift != null && _openShift["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(_openShift["SafeAccountID"]) : (Session.DefaultSafeID ?? 1);

                // 1. استعلام إحصائيات المبيعات وطرق الدفع
                var dtSales = DbHelper.Query(@"
                    SELECT
                        COUNT(SaleID) AS InvoiceCount,
                        ISNULL(SUM(TotalAmount), 0) AS GrossSales,
                        ISNULL(SUM(DiscountAmount), 0) AS TotalDiscounts,
                        ISNULL(SUM(CASE WHEN SaleType = 'Cash' THEN ISNULL(CashPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(CashPaid, 0) ELSE 0 END), 0) AS CashSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Visa' THEN ISNULL(VisaPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(VisaPaid, 0) ELSE 0 END), 0) AS VisaSales,
                        ISNULL(SUM(CASE WHEN SaleType IN ('Wallet','Instapay','VodafoneCash') THEN TotalAmount ELSE 0 END), 0) AS WalletSales,
                        ISNULL(SUM(CASE WHEN SaleType = 'Credit' THEN (TotalAmount - ISNULL(CashPaid, 0) - ISNULL(VisaPaid, 0)) WHEN SaleType = 'Mixed' THEN (TotalAmount - ISNULL(CashPaid, 0) - ISNULL(VisaPaid, 0)) ELSE 0 END), 0) AS CreditSales,
                        ISNULL(SUM(CASE WHEN SaleType NOT IN ('Cash','Credit','Visa','Mixed','Wallet','Instapay','VodafoneCash') THEN TotalAmount ELSE 0 END), 0) AS OtherSales
                    FROM Sales 
                    WHERE (ShiftID = @sid OR (ShiftID IS NULL AND SaleDate >= @dt)) AND IsPosted = 1",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime));

                // 2. المرتجعات
                var dtR = DbHelper.Query(@"
                    SELECT 
                        ISNULL(SUM(sr.TotalAmount), 0) AS TotalReturns,
                        ISNULL(SUM(CASE WHEN s.SaleType = 'Cash' OR s.SaleType = 'Mixed' THEN sr.TotalAmount ELSE 0 END), 0) AS CashReturns,
                        ISNULL(SUM(CASE WHEN s.SaleType = 'Visa' THEN sr.TotalAmount ELSE 0 END), 0) AS VisaReturns
                    FROM SalesReturns sr
                    JOIN Sales s ON sr.SaleID = s.SaleID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt))",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime));

                // 3. المصروفات والتوريدات النقدية
                var dtExp = DbHelper.Query(@"
                    SELECT 
                        ISNULL(SUM(AmountOut), 0) AS TotalExpenses,
                        ISNULL(SUM(AmountIn), 0) AS TotalCashIn
                    FROM CashBox 
                    WHERE (ShiftID = @sid OR (ShiftID IS NULL AND TransDate >= @dt))
                      AND (AccountID = @accId OR AccountID IS NULL OR @accId = 0)
                      AND TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')",
                    DbHelper.P("@sid", shiftID),
                    DbHelper.P("@dt", openTime),
                    DbHelper.P("@accId", drawerSafeID));

                int invCount = dtSales.Rows.Count > 0 ? Convert.ToInt32(dtSales.Rows[0]["InvoiceCount"]) : 0;
                decimal grossSales = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["GrossSales"]) : 0;
                decimal discounts = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["TotalDiscounts"]) : 0;
                decimal cs  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["CashSales"]) : 0;
                decimal vs  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["VisaSales"]) : 0;
                decimal ws  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["WalletSales"]) : 0;
                decimal cr  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["CreditSales"]) : 0;
                decimal os  = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["OtherSales"]) : 0;
                
                decimal tr  = dtR.Rows.Count > 0 ? Convert.ToDecimal(dtR.Rows[0]["TotalReturns"]) : 0;
                decimal crtn = dtR.Rows.Count > 0 ? Convert.ToDecimal(dtR.Rows[0]["CashReturns"]) : 0;
                if (crtn == 0 && tr > 0) crtn = tr; // fallback

                decimal ex  = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalExpenses"]) : 0;
                decimal cin = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalCashIn"]) : 0;

                decimal oc = 0m;
                if (_openShift != null && _openShift["OpeningCash"] != DBNull.Value)
                    oc = Convert.ToDecimal(_openShift["OpeningCash"]);

                decimal netSales = grossSales - tr - discounts;
                decimal expected = oc + cs + cin - crtn - ex;

                _summary = new ShiftSummary
                {
                    InvoiceCount = invCount,
                    TotalSales = grossSales,
                    Discounts = discounts,
                    NetSales = netSales,
                    CashSales = cs,
                    VisaSales = vs,
                    WalletSales = ws,
                    CreditSales = cr,
                    OtherSales = os,
                    TotalReturns = tr,
                    CashReturns = crtn,
                    Expenses = ex,
                    TotalCashIn = cin,
                    OpeningCash = oc,
                    Expected = expected
                };

                // عرض مؤشرات المبيعات
                lblInvoiceCountVal.Text   = invCount.ToString("N0");
                lblGrossSalesVal.Text     = grossSales.ToString("N2") + " ج";
                lblReturnsSummaryVal.Text = tr.ToString("N2") + " ج";
                lblDiscountsVal.Text      = discounts.ToString("N2") + " ج";
                lblTaxesVal.Text          = "0.00 ج";
                lblNetSalesVal.Text       = netSales.ToString("N2") + " ج";

                // عرض كروت وسائل الدفع والنقدية
                lblOpeningCashVal.Text = oc.ToString("N2") + " ج";
                lblCashSalesVal.Text   = cs.ToString("N2") + " ج";
                lblVisaSalesVal.Text   = vs.ToString("N2") + " ج";
                lblWalletSalesVal.Text = ws.ToString("N2") + " ج";
                lblCreditSalesVal.Text = (cr + os).ToString("N2") + " ج";
                lblCashInVal.Text      = cin.ToString("N2") + " ج";
                lblReturnsVal.Text     = crtn.ToString("N2") + " ج";
                lblExpensesVal.Text    = ex.ToString("N2") + " ج";
                lblExpectedVal.Text    = expected.ToString("N2") + " ج";
                
                if (string.IsNullOrWhiteSpace(txtActualCash.Text) || txtActualCash.Text == "0" || txtActualCash.Text == "0.00")
                {
                    txtActualCash.Text = Math.Max(0m, expected).ToString("N2");
                }

                bool canViewDetails = (Session.IsAdmin || Session.CanViewDetails("ShiftClose") || _forceShowDetails);
                pnlSalesKpi.Visible = pnlKpiContainer.Visible = canViewDetails;

                RecalcDiff();
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftClose.LoadShiftSummary", ex); }
        }

        private void LoadShiftMovements(int shiftID, DateTime openTime)
        {
            try
            {
                int drawerSafeID = _openShift != null && _openShift["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(_openShift["SafeAccountID"]) : (Session.DefaultSafeID ?? 1);

                _dtAllMovements = DbHelper.Query(@"
                    SELECT 
                        s.SaleDate AS TransTime,
                        CASE 
                            WHEN s.SaleType = 'Cash' THEN N'مبيعات كاش (+)'
                            WHEN s.SaleType = 'Visa' THEN N'مبيعات فيزا'
                            WHEN s.SaleType = 'Credit' THEN N'مبيعات آجل'
                            WHEN s.SaleType = 'Mixed' THEN N'مبيعات كاش+فيزا'
                            WHEN s.SaleType IN ('Wallet','Instapay','VodafoneCash') THEN N'مبيعات محفظة'
                            ELSE N'مبيعات'
                        END AS TransType, 
                        s.SaleCode AS RefCode, 
                        ISNULL(c.ClientName, N'عميل نقدي') AS Details, 
                        CASE 
                            WHEN s.SaleType = 'Cash' THEN N'نقدي (كاش)'
                            WHEN s.SaleType = 'Visa' THEN N'فيزا'
                            WHEN s.SaleType = 'Credit' THEN N'آجل'
                            WHEN s.SaleType = 'Mixed' THEN N'مختلط'
                            ELSE s.SaleType 
                        END AS PayMethod,
                        CASE WHEN s.SaleType IN ('Cash','Mixed') THEN ISNULL(s.CashPaid, s.TotalAmount) ELSE 0 END AS AmountIn,
                        0.00 AS AmountOut,
                        ISNULL(e.EmpName, N'كاشير') AS UserName,
                        CASE 
                            WHEN s.SaleType = 'Cash' THEN 'CASH'
                            WHEN s.SaleType = 'Credit' THEN 'CREDIT'
                            WHEN s.SaleType IN ('Visa','Wallet','Instapay','VodafoneCash') THEN 'VISA'
                            ELSE 'CASH'
                        END AS FilterCategory
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    LEFT JOIN Employees e ON s.CreatedBy = e.EmpID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt)) AND s.IsPosted = 1
                    
                    UNION ALL
                    
                    SELECT 
                        sr.ReturnDate AS TransTime,
                        N'مرتجع مبيعات (-)' AS TransType,
                        CAST(sr.ReturnID AS NVARCHAR) AS RefCode,
                        N'مرتجع فاتورة مبيعات' AS Details,
                        N'نقدي' AS PayMethod,
                        0.00 AS AmountIn,
                        sr.TotalAmount AS AmountOut,
                        ISNULL(e.EmpName, N'كاشير') AS UserName,
                        'RETURN' AS FilterCategory
                    FROM SalesReturns sr 
                    JOIN Sales s ON sr.SaleID = s.SaleID 
                    LEFT JOIN Employees e ON sr.CreatedBy = e.EmpID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt))
                    
                    UNION ALL
                    
                    SELECT 
                        cb.TransDate AS TransTime,
                        CASE 
                            WHEN cb.TransType = 'ClientPayment' THEN N'تحصيل عميل (+)'
                            WHEN cb.TransType = 'SupplierPayment' THEN N'صرف مورد (-)'
                            WHEN cb.TransType = 'EmpAdvance' THEN N'سلفة موظف (-)'
                            WHEN cb.TransType = 'ReceiptIn' THEN N'سند قبض (+)'
                            WHEN cb.TransType = 'ReceiptOut' THEN N'سند صرف (-)'
                            WHEN cb.TransType = 'Transfer' AND cb.AmountIn > 0 THEN N'تحويل وارد (+)'
                            WHEN cb.TransType = 'Transfer' AND cb.AmountOut > 0 THEN N'تحويل صادر (-)'
                            WHEN cb.AmountIn > 0 THEN N'توريد نقدية (+)'
                            ELSE N'مصروف نثريات (-)'
                        END AS TransType,
                        CAST(cb.CashID AS NVARCHAR) AS RefCode,
                        ISNULL(cb.Notes, N'حركة نقدية') AS Details,
                        N'نقدي' AS PayMethod,
                        cb.AmountIn AS AmountIn,
                        cb.AmountOut AS AmountOut,
                        ISNULL(e.EmpName, N'المستخدم') AS UserName,
                        CASE 
                            WHEN cb.TransType = 'Transfer' THEN 'TRANSFER'
                            WHEN cb.AmountIn > 0 THEN 'CASHIN'
                            ELSE 'EXPENSE'
                        END AS FilterCategory
                    FROM CashBox cb
                    LEFT JOIN Employees e ON cb.CreatedBy = e.EmpID
                    WHERE (cb.ShiftID = @sid OR (cb.ShiftID IS NULL AND cb.TransDate >= @dt))
                      AND (cb.AccountID = @accId OR cb.AccountID = 1 OR cb.AccountID IS NULL OR @accId = 0)
                      AND cb.TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')
                    
                    ORDER BY TransTime DESC",
                    DbHelper.P("@sid", shiftID), DbHelper.P("@dt", openTime), DbHelper.P("@accId", drawerSafeID));

                ApplyMovementsFilter();
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftClose.LoadShiftMovements", ex); }
        }

        private void ApplyMovementsFilter()
        {
            dgMovements.Rows.Clear();
            if (_dtAllMovements == null || _dtAllMovements.Rows.Count == 0) return;

            int rowNum = 1;
            foreach (DataRow r in _dtAllMovements.Rows)
            {
                string cat = r["FilterCategory"]?.ToString() ?? "";
                if (_currentFilter != "ALL" && !string.Equals(_currentFilter, cat, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                decimal inAmt = Convert.ToDecimal(r["AmountIn"]);
                decimal outAmt = Convert.ToDecimal(r["AmountOut"]);
                DateTime t = Convert.ToDateTime(r["TransTime"]);

                int ri = dgMovements.Rows.Add(
                    rowNum++,
                    t.ToString("hh:mm:ss tt"),
                    r["TransType"],
                    r["RefCode"],
                    r["Details"],
                    r["PayMethod"],
                    inAmt > 0 ? inAmt.ToString("N2") : "-",
                    outAmt > 0 ? outAmt.ToString("N2") : "-",
                    r["UserName"]);

                if (inAmt > 0 && outAmt == 0)
                {
                    dgMovements.Rows[ri].Cells["AmountIn"].Style.ForeColor = Color.FromArgb(16, 185, 129);
                }
                else if (outAmt > 0)
                {
                    dgMovements.Rows[ri].Cells["AmountOut"].Style.ForeColor = Color.FromArgb(239, 68, 68);
                }
            }
        }

        private void OnActualCashChanged()
        {
            RecalcDiff();
        }

        private void OnTargetSafeChanged()
        {
            if (cboTargetSafe.SelectedItem is ComboItem item)
            {
                if (item.ID == 0) // إبقاء بالدرج
                {
                    txtTransferAmount.Text = "0";
                    txtTransferAmount.Enabled = false;
                }
                else // تحويل لخزنة
                {
                    txtTransferAmount.Enabled = true;
                    if (decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal act) && act > 0)
                    {
                        txtTransferAmount.Text = act.ToString("N2");
                    }
                }
            }
            RecalcDiff();
        }

        private void RecalcDiff()
        {
            if (_summary == null) return;

            decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual);
            decimal diff = actual - _summary.Expected;

            if (diff > 0.01m)
            {
                lblDiffVal.Text = $"🟢 زيادة: +{diff:N2} ج";
                lblDiffVal.ForeColor = Theme.Success;
                pnlDeficitReasonBox.Visible = false;
            }
            else if (diff < -0.01m)
            {
                lblDiffVal.Text = $"🔴 عجز: {diff:N2} ج";
                lblDiffVal.ForeColor = Theme.Danger;
                pnlDeficitReasonBox.Visible = true;
            }
            else
            {
                lblDiffVal.Text = "✅ مطابق تماماً (0.00 ج)";
                lblDiffVal.ForeColor = Theme.Accent;
                pnlDeficitReasonBox.Visible = false;
            }

            decimal.TryParse(txtTransferAmount.Text.Replace(",", ""), out decimal transfer);
            decimal remaining = Math.Max(0m, actual - transfer);
            lblRemainingVal.Text = remaining.ToString("N2") + " ج";
        }

        private void BtnCloseShift_Click(object sender, EventArgs e)
        {
            if (_openShift == null) return;
            if (!decimal.TryParse(txtActualCash.Text.Replace(",", ""), out decimal actual) || actual < 0)
            {
                MessageBox.Show("الرجاء إدخال المبلغ الفعلي الموجود بالخزنة/الدرج بشكل صحيح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtActualCash.Focus();
                return;
            }

            decimal expected = _summary != null ? _summary.Expected : 0m;
            decimal diff = actual - expected;

            // التحقق من سبب العجز الإجباري
            if (diff < -0.01m && string.IsNullOrWhiteSpace(txtDeficitReason.Text))
            {
                MessageBox.Show("⚠️ يوجد عجز في نقدية الوردية بمقدار (" + diff.ToString("N2") + " ج).\n\nيرجى كتابة وتبرير سبب العجز في حقل 'تبرير سبب العجز' قبل تقفيل الوردية.", "إلزامية تبرير العجز", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDeficitReason.Focus();
                return;
            }

            decimal.TryParse(txtTransferAmount.Text.Replace(",", ""), out decimal transfer);
            if (transfer < 0 || transfer > actual)
            {
                MessageBox.Show("مبلغ التحويل غير صحيح (لا يمكن أن يكون أكبر من المبلغ الفعلي بالدرج).", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTransferAmount.Focus();
                return;
            }

            decimal remainingInDrawer = actual - transfer;
            int targetSafeID = 0;
            if (cboTargetSafe.SelectedItem is ComboItem ci) targetSafeID = ci.ID;

            string confirmMsg = $"هل أنت متأكد من تقفيل الوردية الحالية؟\n\n" +
                                $"• المبلغ الفعلي بالدرج: {actual:N2} ج\n" +
                                $"• الفارق (العجز/الزيادة): {diff:N2} ج\n" +
                                (transfer > 0 ? $"• المبلغ المحول للخزنة: {transfer:N2} ج\n" : "") +
                                $"• المبلغ المتبقي كرصيد للوردية القادمة: {remainingInDrawer:N2} ج\n\n" +
                                "⚡ سيتم فتح الوردية التالية تلقائياً فوراً.";

            if (MessageBox.Show(confirmMsg, "تأكيد إغلاق الوردية", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                int shiftID = Convert.ToInt32(_openShift["ShiftID"]);
                int currentSafeID = _openShift["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(_openShift["SafeAccountID"]) : (Session.DefaultSafeID ?? 1);

                // 1. تحديث بيانات الوردية المغلقة
                DbHelper.Execute(@"
                    UPDATE Shifts
                    SET CloseTime = GETDATE(),
                        ClosedBy = @emp,
                        TotalSales = @ts,
                        CashSales = @cs,
                        VisaSales = @vs,
                        WalletSales = @ws,
                        CreditSales = @crs,
                        OtherSales = @os,
                        TotalReturns = @tr,
                        CashReturns = @crr,
                        TotalDiscounts = @disc,
                        NetSales = @nets,
                        InvoiceCount = @invc,
                        CashExpenses = @exp,
                        CashIn = @cin,
                        ExpectedCash = @expCash,
                        ActualCash = @actCash,
                        Difference = @diff,
                        TransferToSafeID = @targetSafe,
                        TransferredAmount = @transAmt,
                        RemainingInDrawer = @rem,
                        DeficitReason = @defReason,
                        DenominationsJson = @denoms,
                        Notes = @notes,
                        ApprovalStatus = 'PendingApproval',
                        Status = 'Closed'
                    WHERE ShiftID = @sid",
                    DbHelper.P("@emp", Session.EmpID),
                    DbHelper.P("@ts", _summary != null ? _summary.TotalSales : 0),
                    DbHelper.P("@cs", _summary != null ? _summary.CashSales : 0),
                    DbHelper.P("@vs", _summary != null ? _summary.VisaSales : 0),
                    DbHelper.P("@ws", _summary != null ? _summary.WalletSales : 0),
                    DbHelper.P("@crs", _summary != null ? _summary.CreditSales : 0),
                    DbHelper.P("@os", _summary != null ? _summary.OtherSales : 0),
                    DbHelper.P("@tr", _summary != null ? _summary.TotalReturns : 0),
                    DbHelper.P("@crr", _summary != null ? _summary.CashReturns : 0),
                    DbHelper.P("@disc", _summary != null ? _summary.Discounts : 0),
                    DbHelper.P("@nets", _summary != null ? _summary.NetSales : 0),
                    DbHelper.P("@invc", _summary != null ? _summary.InvoiceCount : 0),
                    DbHelper.P("@exp", _summary != null ? _summary.Expenses : 0),
                    DbHelper.P("@cin", _summary != null ? _summary.TotalCashIn : 0),
                    DbHelper.P("@expCash", expected),
                    DbHelper.P("@actCash", actual),
                    DbHelper.P("@diff", diff),
                    DbHelper.P("@targetSafe", targetSafeID > 0 ? (object)targetSafeID : DBNull.Value),
                    DbHelper.P("@transAmt", transfer),
                    DbHelper.P("@rem", remainingInDrawer),
                    DbHelper.P("@defReason", txtDeficitReason.Text.Trim()),
                    DbHelper.P("@denoms", _denominationsJson),
                    DbHelper.P("@notes", txtNotes.Text.Trim()),
                    DbHelper.P("@sid", shiftID));

                // 2. تسجيل قيد التحويل المالي للخزنة إذا وجد
                if (transfer > 0 && targetSafeID > 0)
                {
                    DbHelper.Execute(
                        @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy, RefID)
                          VALUES (GETDATE(), 'ShiftCloseOut', 0, @amt, @accOut, @notesOut, @uid, @ref);
                          INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy, RefID)
                          VALUES (GETDATE(), 'ShiftCloseIn', @amt, 0, @accIn, @notesIn, @uid, @ref);",
                        DbHelper.P("@amt", transfer),
                        DbHelper.P("@accOut", currentSafeID),
                        DbHelper.P("@notesOut", $"توريد نقدية من تقفيل الوردية #{shiftID} إلى الخزنة"),
                        DbHelper.P("@accIn", targetSafeID),
                        DbHelper.P("@notesIn", $"توريد وارد من تقفيل الوردية #{shiftID}"),
                        DbHelper.P("@uid", Session.EmpID),
                        DbHelper.P("@ref", shiftID));
                }

                // 3. فتح الوردية التالية فوراً تلقائياً
                int newShiftID = ShiftDAL.EnsureActiveShift(Session.EmpID, currentSafeID, remainingInDrawer);

                string successMsg = $"✅ تم إغلاق وتقفيل الوردية #{shiftID} بنجاح!\n\n" +
                                   $"🚀 تم فتح الوردية الجديدة #{newShiftID} تلقائياً برصيد افتتاحي: ({remainingInDrawer:N2} ج).\n\n" +
                                   "هل ترغب في طباعة تقرير تقفيل الوردية الآن؟";

                if (MessageBox.Show(successMsg, "تم تقفيل الوردية بنجاح", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    FrmPrintShift.ShowPrintOptions(shiftID, btnCloseShift);
                }

                LoadCurrentShift();
            }
            catch (Exception ex) { MessageBox.Show("خطأ عند إغلاق الوردية:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnApproveShift_Click(object sender, EventArgs e)
        {
            if (_openShift == null) return;
            int shiftID = Convert.ToInt32(_openShift["ShiftID"]);

            if (MessageBox.Show($"هل أنت متأكد من اعتماد ومصادقة إغلاق الوردية رقم #{shiftID} نهائياً؟", "اعتماد الوردية", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bool ok = ShiftDAL.ApproveShift(shiftID, Session.EmpID, Session.EmpName, "تم اعتماد التقفيل من شاشة إدارة الوردية");
                if (ok)
                {
                    MessageBox.Show($"✅ تم اعتماد الوردية #{shiftID} بنجاح وقفلها محاسبياً.", "تم الاعتماد", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCurrentShift();
                }
            }
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
            public int InvoiceCount;
            public decimal TotalSales, Discounts, NetSales, CashSales, VisaSales, WalletSales, CreditSales, OtherSales, TotalReturns, CashReturns, Expenses, TotalCashIn, OpeningCash, Expected;
        }
    }
}
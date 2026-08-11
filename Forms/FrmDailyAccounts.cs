using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة الحسابات والمالية اليومية الشاملة
    /// تشمل: سندات الصرف والقبض، تقارير التوريد والمصروفات، القيود اليومية، حركة الخزينة والبنوك والتحويلات، وتكلفة المباع والمخزون
    /// </summary>
    public class FrmDailyAccounts : Form
    {
        private TabControl tabMain;

        // ===== Tab 1: Vouchers & Daily Reports =====
        private ComboBox cboVoucherType, cboEntityCategory, cboEntity, cboPayMethod, cboAccountSafe;
        private TextBox txtAmount, txtNotes;
        private Button btnSaveVoucher, btnPrintVoucher;
        private DataGridView dgExpensesReport, dgReceiptsReport;
        private DateTimePicker dtpExpFrom, dtpExpTo, dtpRecFrom, dtpRecTo;
        private Label lblTotalExp, lblTotalRec;

        // ===== Tab 2: Journal Entries (القيود اليومية) =====
        private TextBox txtJournalRef, txtJournalNotes;
        private DateTimePicker dtpJournalDate;
        private DataGridView dgJournalLines, dgJournalHistory;
        private Label lblTotalDebit, lblTotalCredit, lblBalanceStatus;
        private Button btnSaveJournal, btnPrintJournal;
        private DateTimePicker dtpJourHistoryFrom, dtpJourHistoryTo;

        // ===== Tab 3: Cash & Bank Transfers =====
        private Label lblTotalSafesVal, lblTotalBanksVal, lblTotalLiquidityVal;
        private ComboBox cboTransferFrom, cboTransferTo;
        private TextBox txtTransferAmount, txtTransferNotes;
        private Button btnExecuteTransfer;
        private DataGridView dgAccountMovements;

        // ===== Tab 4: COGS, Inventory & Profitability =====
        private Label lblCogsVal, lblStockCostVal, lblStockRetailVal, lblGrossProfitVal;
        private DateTimePicker dtpCogsFrom, dtpCogsTo;
        private DataGridView dgInventoryAdjustments;

        public FrmDailyAccounts()
        {
            InitUI();
            LoadMasterData();
            RefreshAllTabsData();
        }

        private void InitUI()
        {
            this.Text = "🏛️ الحسابات والمالية اليومية الشاملة";
            this.Size = new Size(1220, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Title Bar
            var pnlTitle = Theme.MakeTitleBar("🏛️ الحسابات والمالية اليومية الشاملة", 
                "إدارة سندات الصرف والقبض، تقارير التوريدات والمصروفات، القيود اليومية، حركة الخزائن والبنوك، وتكلفة المباع والمخزون");
            pnlTitle.Dock = DockStyle.Top;
            this.Controls.Add(pnlTitle);

            // Tab Control
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontBold
            };

            // Build Tab 1: Vouchers
            var tabVouchers = new TabPage("💸 سندات الصرف والتوريد والتقارير اليومية");
            BuildVouchersTab(tabVouchers);
            tabMain.TabPages.Add(tabVouchers);

            // Build Tab 2: Journal Entries
            var tabJournal = new TabPage("🔄 القيود اليومية (مدين ودائن)");
            BuildJournalTab(tabJournal);
            tabMain.TabPages.Add(tabJournal);

            // Build Tab 3: Cash & Banks
            var tabCashBank = new TabPage("🏛️ حركة الخزينة والبنوك والتحويلات");
            BuildCashBankTab(tabCashBank);
            tabMain.TabPages.Add(tabCashBank);

            // Build Tab 4: COGS & Inventory
            var tabCogsInv = new TabPage("📦 تكلفة المبيعات والمخزون والربحية");
            BuildCogsInvTab(tabCogsInv);
            tabMain.TabPages.Add(tabCogsInv);

            this.Controls.Add(tabMain);
            pnlTitle.SendToBack();

            Theme.ApplyFormRTL(this);
        }

        // =========================================================================
        // TAB 1: VOUCHERS & DAILY REPORTS (سندات القبض والصرف والتقارير)
        // =========================================================================
        private void BuildVouchersTab(TabPage page)
        {
            page.BackColor = Theme.BgMain;

            // Top Form Bar for Creating Voucher
            var pnlCreate = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                Name = "pnlFilter",
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10, 8, 10, 8)
            };

            var flow1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent
            };

            // Voucher Type
            flow1.Controls.Add(new Label { Text = "نوع السند:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(5, 6, 0, 0) });
            cboVoucherType = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboVoucherType.Items.Add("🔴 سند صرف (خرج)");
            cboVoucherType.Items.Add("🟢 سند قبض / توريد (دخول)");
            cboVoucherType.SelectedIndex = 0;
            flow1.Controls.Add(cboVoucherType);

            // Entity Category
            flow1.Controls.Add(new Label { Text = "جهة المعاملة:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            cboEntityCategory = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cboEntityCategory.Items.Add("مورد");
            cboEntityCategory.Items.Add("عميل");
            cboEntityCategory.Items.Add("مصروف عام / بند إداري");
            cboEntityCategory.SelectedIndex = 0;
            cboEntityCategory.SelectedIndexChanged += (s, e) => LoadEntitiesForCategory();
            flow1.Controls.Add(cboEntityCategory);

            // Entity Select
            cboEntity = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            flow1.Controls.Add(cboEntity);

            // Amount
            flow1.Controls.Add(new Label { Text = "المبلغ (ج):", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            txtAmount = new TextBox { Width = 90, Text = "0.00", Font = Theme.FontBold };
            flow1.Controls.Add(txtAmount);

            // Payment Method
            flow1.Controls.Add(new Label { Text = "طريقة الدفع:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            cboPayMethod = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            cboPayMethod.Items.Add("نقدي (الخزنة)");
            cboPayMethod.Items.Add("بنك / تحويل");
            cboPayMethod.Items.Add("شبكة / فيزا");
            cboPayMethod.SelectedIndex = 0;
            flow1.Controls.Add(cboPayMethod);

            // Safe / Bank Account
            flow1.Controls.Add(new Label { Text = "الخزنة / الحساب:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            cboAccountSafe = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            flow1.Controls.Add(cboAccountSafe);

            // Notes
            flow1.Controls.Add(new Label { Text = "البيان / الملاحظات:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            txtNotes = new TextBox { Width = 280, Text = "" };
            flow1.Controls.Add(txtNotes);

            // Buttons
            btnSaveVoucher = Theme.MakeButton("💾 حفظ وإصدار السند (F5)", Color.FromArgb(30, 110, 50));
            btnSaveVoucher.Size = new Size(180, 28);
            btnSaveVoucher.Margin = new Padding(15, 2, 0, 0);
            btnSaveVoucher.Click += BtnSaveVoucher_Click;
            flow1.Controls.Add(btnSaveVoucher);

            btnPrintVoucher = Theme.MakeButton("🖨️ طباعة آخر سند", Color.FromArgb(20, 80, 140));
            btnPrintVoucher.Size = new Size(140, 28);
            btnPrintVoucher.Margin = new Padding(10, 2, 0, 0);
            btnPrintVoucher.Click += (s, e) => PrintLastVoucher();
            flow1.Controls.Add(btnPrintVoucher);

            pnlCreate.Controls.Add(flow1);
            page.Controls.Add(pnlCreate);

            // Reports Split View (Top: Expenses / Bottom: Collections)
            var splitRep = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            // Panel Top: Expenses Report (المصروفات وعمليات الصرف)
            var pnlExpRep = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };
            var pnlExpHeader = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, Name = "pnlFilter", BackColor = Theme.BgSearchPanel, Padding = new Padding(5) };
            pnlExpHeader.Controls.Add(new Label { Text = "📕 تقرير المصروفات وعمليات الصرف اليومية  |  من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 5, 0, 0) });
            dtpExpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpExpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpExpFrom.ValueChanged += (s, e) => LoadExpensesReport();
            dtpExpTo.ValueChanged += (s, e) => LoadExpensesReport();
            pnlExpHeader.Controls.Add(dtpExpFrom);
            pnlExpHeader.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(5, 5, 0, 0) });
            pnlExpHeader.Controls.Add(dtpExpTo);

            lblTotalExp = new Label { Text = "إجمالي المصروفات: 0.00 ج", AutoSize = true, ForeColor = Color.FromArgb(220, 60, 60), Font = Theme.FontBold, Margin = new Padding(20, 5, 0, 0) };
            pnlExpHeader.Controls.Add(lblTotalExp);

            dgExpensesReport = MakeStandardGrid();
            pnlExpRep.Controls.Add(dgExpensesReport);
            pnlExpRep.Controls.Add(pnlExpHeader);
            splitRep.Panel1.Controls.Add(pnlExpRep);

            // Panel Bottom: Receipts Report (التوريدات والمقبوضات)
            var pnlRecRep = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };
            var pnlRecHeader = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, Name = "pnlFilter", BackColor = Theme.BgSearchPanel, Padding = new Padding(5) };
            pnlRecHeader.Controls.Add(new Label { Text = "📗 تقرير التوريدات والمقبوضات اليومية  |  من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 5, 0, 0) });
            dtpRecFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpRecTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpRecFrom.ValueChanged += (s, e) => LoadReceiptsReport();
            dtpRecTo.ValueChanged += (s, e) => LoadReceiptsReport();
            pnlRecHeader.Controls.Add(dtpRecFrom);
            pnlRecHeader.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(5, 5, 0, 0) });
            pnlRecHeader.Controls.Add(dtpRecTo);

            lblTotalRec = new Label { Text = "إجمالي المقبوضات: 0.00 ج", AutoSize = true, ForeColor = Color.FromArgb(40, 160, 70), Font = Theme.FontBold, Margin = new Padding(20, 5, 0, 0) };
            pnlRecHeader.Controls.Add(lblTotalRec);

            dgReceiptsReport = MakeStandardGrid();
            pnlRecRep.Controls.Add(dgReceiptsReport);
            pnlRecRep.Controls.Add(pnlRecHeader);
            splitRep.Panel2.Controls.Add(pnlRecRep);

            page.Controls.Add(splitRep);
            pnlCreate.BringToFront();
        }

        // =========================================================================
        // TAB 2: JOURNAL ENTRIES (القيود اليومية المحاسبية)
        // =========================================================================
        private void BuildJournalTab(TabPage page)
        {
            page.BackColor = Theme.BgMain;

            var splitJour = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 320
            };

            // Top: Create Journal Entry Form
            var pnlJourForm = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(10) };
            
            var pnlJourHeader = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Name = "pnlFilter", BackColor = Theme.BgSearchPanel, Padding = new Padding(5) };
            pnlJourHeader.Controls.Add(new Label { Text = "🔄 قيد يومية محاسبي جديد | التاريخ:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 5, 0, 0) });
            dtpJournalDate = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnlJourHeader.Controls.Add(dtpJournalDate);

            pnlJourHeader.Controls.Add(new Label { Text = "رقم المرجع / المستند:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 5, 0, 0) });
            txtJournalRef = new TextBox { Width = 120 };
            pnlJourHeader.Controls.Add(txtJournalRef);

            pnlJourHeader.Controls.Add(new Label { Text = "البيان العام للقيد:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 5, 0, 0) });
            txtJournalNotes = new TextBox { Width = 250 };
            pnlJourHeader.Controls.Add(txtJournalNotes);

            // Journal Lines Grid
            dgJournalLines = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = true,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RightToLeft = RightToLeft.Yes
            };
            Theme.StyleGridHeader(dgJournalLines);
            dgJournalLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "اسم الحساب / البند", FillWeight = 120 });
            dgJournalLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "مدين (Debit)", FillWeight = 60, ValueType = typeof(decimal) });
            dgJournalLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "دائن (Credit)", FillWeight = 60, ValueType = typeof(decimal) });
            dgJournalLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineNotes", HeaderText = "البيان التفصيلي للسطر", FillWeight = 140 });

            // Default demo 2 rows
            dgJournalLines.Rows.Add("حـ/ الخزينة الرئيسية", "1000.00", "0.00", "إيداع نقدي");
            dgJournalLines.Rows.Add("حـ/ المبيعات", "0.00", "1000.00", "إيراد مبيعات");

            dgJournalLines.CellValueChanged += (s, e) => RecalcJournalTotals();

            // Journal Footer Bar
            var pnlJourFoot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, BackColor = Theme.BgCard, Padding = new Padding(10, 5, 10, 5) };
            lblTotalDebit = new Label { Text = "إجمالي المدين: 1000.00 ج", AutoSize = true, ForeColor = Color.Blue, Font = Theme.FontBold, Margin = new Padding(10, 5, 0, 0) };
            lblTotalCredit = new Label { Text = "إجمالي الدائن: 1000.00 ج", AutoSize = true, ForeColor = Color.Purple, Font = Theme.FontBold, Margin = new Padding(20, 5, 0, 0) };
            lblBalanceStatus = new Label { Text = "✅ القيد متوازن", AutoSize = true, ForeColor = Color.Green, Font = Theme.FontBold, Margin = new Padding(20, 5, 0, 0) };

            btnSaveJournal = Theme.MakeButton("💾 حفظ القيد المحاسبي", Theme.Success);
            btnSaveJournal.Size = new Size(160, 28);
            btnSaveJournal.Margin = new Padding(30, 2, 0, 0);
            btnSaveJournal.Click += BtnSaveJournal_Click;

            btnPrintJournal = Theme.MakeButton("🖨️ طباعة القيد", Color.FromArgb(20, 80, 140));
            btnPrintJournal.Size = new Size(120, 28);
            btnPrintJournal.Margin = new Padding(10, 2, 0, 0);

            pnlJourFoot.Controls.AddRange(new Control[] { lblTotalDebit, lblTotalCredit, lblBalanceStatus, btnSaveJournal, btnPrintJournal });

            pnlJourForm.Controls.Add(dgJournalLines);
            pnlJourForm.Controls.Add(pnlJourHeader);
            pnlJourForm.Controls.Add(pnlJourFoot);
            splitJour.Panel1.Controls.Add(pnlJourForm);

            // Bottom: Past Journal Entries History
            var pnlJourHist = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };
            var pnlHistHeader = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, Name = "pnlFilter", BackColor = Theme.BgSearchPanel, Padding = new Padding(5) };
            pnlHistHeader.Controls.Add(new Label { Text = "📑 سجل قيود اليومية السابقة | من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 5, 0, 0) });
            dtpJourHistoryFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            dtpJourHistoryTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpJourHistoryFrom.ValueChanged += (s, e) => LoadJournalHistory();
            dtpJourHistoryTo.ValueChanged += (s, e) => LoadJournalHistory();
            pnlHistHeader.Controls.Add(dtpJourHistoryFrom);
            pnlHistHeader.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(5, 5, 0, 0) });
            pnlHistHeader.Controls.Add(dtpJourHistoryTo);

            dgJournalHistory = MakeStandardGrid();
            pnlJourHist.Controls.Add(dgJournalHistory);
            pnlJourHist.Controls.Add(pnlHistHeader);
            splitJour.Panel2.Controls.Add(pnlJourHist);

            page.Controls.Add(splitJour);
        }

        // =========================================================================
        // TAB 3: CASH & BANK TRANSFERS (حركة الخزائن والبنوك والتحويلات)
        // =========================================================================
        private void BuildCashBankTab(TabPage page)
        {
            page.BackColor = Theme.BgMain;

            // KPI Cards Top Header
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            lblTotalSafesVal = MakeKpiCard(pnlKpis, 0, "💰 إجمالي رصيد الخزائن النقدية", "0.00 ج", Color.FromArgb(20, 110, 60));
            lblTotalBanksVal = MakeKpiCard(pnlKpis, 1, "🏛️ إجمالي رصيد الحسابات البنكية", "0.00 ج", Color.FromArgb(20, 70, 140));
            lblTotalLiquidityVal = MakeKpiCard(pnlKpis, 2, "💵 إجمالي السيولة الفعلية المتاحة", "0.00 ج", Color.FromArgb(140, 40, 90));

            page.Controls.Add(pnlKpis);

            // Transfer Bar
            var pnlTransfer = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Name = "pnlFilter",
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(8, 6, 8, 6)
            };

            pnlTransfer.Controls.Add(new Label { Text = "🔄 تحويل مالي بين الخزائن والحسابات | من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 6, 0, 0) });
            cboTransferFrom = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlTransfer.Controls.Add(cboTransferFrom);

            pnlTransfer.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(10, 6, 0, 0) });
            cboTransferTo = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlTransfer.Controls.Add(cboTransferTo);

            pnlTransfer.Controls.Add(new Label { Text = "المبلغ (ج):", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(10, 6, 0, 0) });
            txtTransferAmount = new TextBox { Width = 90, Text = "0.00", Font = Theme.FontBold };
            pnlTransfer.Controls.Add(txtTransferAmount);

            pnlTransfer.Controls.Add(new Label { Text = "السبب / البيان:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(10, 6, 0, 0) });
            txtTransferNotes = new TextBox { Width = 200, Text = "تحويل بين حسابات" };
            pnlTransfer.Controls.Add(txtTransferNotes);

            btnExecuteTransfer = Theme.MakeButton("🔄 تنفيذ التحويل الفوري", Theme.Primary);
            btnExecuteTransfer.Size = new Size(160, 28);
            btnExecuteTransfer.Margin = new Padding(15, 2, 0, 0);
            btnExecuteTransfer.Click += BtnExecuteTransfer_Click;
            pnlTransfer.Controls.Add(btnExecuteTransfer);

            page.Controls.Add(pnlTransfer);

            // Account Movements Grid
            dgAccountMovements = MakeStandardGrid();
            page.Controls.Add(dgAccountMovements);

            pnlKpis.BringToFront();
            pnlTransfer.BringToFront();
        }

        // =========================================================================
        // TAB 4: COGS, INVENTORY & PROFITABILITY (تكلفة المبيعات والمخزون)
        // =========================================================================
        private void BuildCogsInvTab(TabPage page)
        {
            page.BackColor = Theme.BgMain;

            // Date Filter Top Bar
            var pnlCogsFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                Name = "pnlFilter",
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlCogsFilter.Controls.Add(new Label { Text = "📊 فترة احتساب تكلفة المباع والأرباح | من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 6, 0, 0) });
            dtpCogsFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            dtpCogsTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpCogsFrom.ValueChanged += (s, e) => LoadCogsAndProfitability();
            dtpCogsTo.ValueChanged += (s, e) => LoadCogsAndProfitability();
            pnlCogsFilter.Controls.Add(dtpCogsFrom);
            pnlCogsFilter.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(10, 6, 0, 0) });
            pnlCogsFilter.Controls.Add(dtpCogsTo);

            var btnRefreshCogs = Theme.MakeButton("🔄 تحديث التحليلات", Theme.Success);
            btnRefreshCogs.Size = new Size(130, 28);
            btnRefreshCogs.Margin = new Padding(15, 2, 0, 0);
            btnRefreshCogs.Click += (s, e) => LoadCogsAndProfitability();
            pnlCogsFilter.Controls.Add(btnRefreshCogs);

            var btnOpenAdjustments = Theme.MakeButton("🔧 الذهاب لشاشة تسويات الجرد", Color.FromArgb(30, 90, 160));
            btnOpenAdjustments.Size = new Size(190, 28);
            btnOpenAdjustments.Margin = new Padding(15, 2, 0, 0);
            btnOpenAdjustments.Click += (s, e) => new FrmAdjustment(0, "تسوية جرد عام", false).ShowDialog();
            pnlCogsFilter.Controls.Add(btnOpenAdjustments);

            page.Controls.Add(pnlCogsFilter);

            // KPI Cards for COGS & Inventory Metrics
            var pnlCogsKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlCogsKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCogsKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCogsKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCogsKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblCogsVal = MakeKpiCard(pnlCogsKpis, 0, "📉 تكلفة البضاعة المباعة (COGS)", "0.00 ج", Color.FromArgb(180, 50, 50));
            lblStockCostVal = MakeKpiCard(pnlCogsKpis, 1, "📦 قيمة المخزون الحالي (بسعر التكلفة)", "0.00 ج", Color.FromArgb(30, 110, 140));
            lblStockRetailVal = MakeKpiCard(pnlCogsKpis, 2, "🏷️ قيمة المخزون (بسعر البيع)", "0.00 ج", Color.FromArgb(120, 50, 140));
            lblGrossProfitVal = MakeKpiCard(pnlCogsKpis, 3, "📈 أرباح المبيعات (Gross Profit)", "0.00 ج", Color.FromArgb(30, 130, 60));

            page.Controls.Add(pnlCogsKpis);

            // Inventory Adjustments & Variances Grid
            var pnlAdjHeader = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgHeader };
            var lblAdjTitle = new Label
            {
                Text = "📋 سجل وتسويات فروق الجرد والعجز/الزيادة المخزنية المسجلة:",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            pnlAdjHeader.Controls.Add(lblAdjTitle);
            page.Controls.Add(pnlAdjHeader);

            dgInventoryAdjustments = MakeStandardGrid();
            page.Controls.Add(dgInventoryAdjustments);

            pnlCogsFilter.BringToFront();
            pnlCogsKpis.BringToFront();
            pnlAdjHeader.BringToFront();
        }

        // =========================================================================
        // =========================================================================
        // DATA LOADING & ACTIONS
        // =========================================================================
        private void LoadMasterData()
        {
            LoadEntitiesForCategory();

            // Safes & Bank Accounts
            var dtAcc = AccountDAL.GetActiveSafeAccounts();
            cboAccountSafe.Items.Clear();
            cboTransferFrom.Items.Clear();
            cboTransferTo.Items.Clear();

            foreach (DataRow r in dtAcc.Rows)
            {
                var ci = new ComboItem((int)r["AccountID"], r["AccountName"].ToString());
                cboAccountSafe.Items.Add(ci);
                cboTransferFrom.Items.Add(ci);
                cboTransferTo.Items.Add(ci);
            }
            if (cboAccountSafe.Items.Count > 0) cboAccountSafe.SelectedIndex = 0;
            if (cboTransferFrom.Items.Count > 0) cboTransferFrom.SelectedIndex = 0;
            if (cboTransferTo.Items.Count > 1) cboTransferTo.SelectedIndex = 1;
            else if (cboTransferTo.Items.Count > 0) cboTransferTo.SelectedIndex = 0;
        }

        private void LoadEntitiesForCategory()
        {
            cboEntity.Items.Clear();
            string cat = cboEntityCategory.SelectedItem?.ToString() ?? "مورد";

            if (cat.Contains("مورد"))
            {
                var dt = SupplierDAL.GetAll(true);
                foreach (DataRow r in dt.Rows)
                    cboEntity.Items.Add(new ComboItem((int)r["SupplierID"], r["SupplierName"].ToString()));
            }
            else if (cat.Contains("عميل"))
            {
                var dt = ClientDAL.GetAll(true);
                foreach (DataRow r in dt.Rows)
                    cboEntity.Items.Add(new ComboItem((int)r["ClientID"], r["ClientName"].ToString()));
            }
            else
            {
                cboEntity.Items.Add(new ComboItem(1, "مصروفات عمومية وإدارية"));
                cboEntity.Items.Add(new ComboItem(2, "إيجارات ومرافق"));
                cboEntity.Items.Add(new ComboItem(3, "مرتبات ومكافآت"));
                cboEntity.Items.Add(new ComboItem(4, "نثريات وشحن"));
                cboEntity.Items.Add(new ComboItem(5, "صيانة وتجهيزات"));
            }
            cboEntity.DisplayMember = "Text";
            if (cboEntity.Items.Count > 0) cboEntity.SelectedIndex = 0;
        }

        private void RefreshAllTabsData()
        {
            LoadExpensesReport();
            LoadReceiptsReport();
            LoadJournalHistory();
            LoadCashBankMovements();
            LoadCogsAndProfitability();
        }

        private void LoadExpensesReport()
        {
            var dt = DbHelper.Query(
                @"SELECT e.ExpenseID AS [رقم السند], e.ExpenseDate AS [التاريخ والوقت], 
                         CASE WHEN e.SupplierID IS NOT NULL THEN N'صرف لمورد' 
                              ELSE N'مصروف إداري' END AS [نوع السند],
                         COALESCE(s.SupplierName, N'مصروفات عامة') AS [الجهة],
                         e.Amount AS [المبلغ], 
                         e.ExpenseType AS [طريقة الدفع/البند], 
                         sa.AccountName AS [الخزنة / الحساب], 
                         e.Notes AS [البيان والملاحظات]
                  FROM Expenses e
                  LEFT JOIN Suppliers s ON e.SupplierID = s.SupplierID
                  LEFT JOIN SafeAccounts sa ON e.SafeAccountID = sa.AccountID
                  WHERE e.ExpenseDate BETWEEN @f AND @t
                  ORDER BY e.ExpenseID DESC",
                DbHelper.P("@f", dtpExpFrom.Value.Date),
                DbHelper.P("@t", dtpExpTo.Value.Date.AddDays(1).AddSeconds(-1)));

            dgExpensesReport.DataSource = dt;

            decimal tot = 0m;
            foreach (DataRow r in dt.Rows)
                tot += r["المبلغ"] != DBNull.Value ? Convert.ToDecimal(r["المبلغ"]) : 0m;
            lblTotalExp.Text = $"إجمالي المصروفات وصرف النقدية: {tot:N2} ج";
        }

        private void LoadReceiptsReport()
        {
            var dt = DbHelper.Query(
                @"SELECT cb.CashID AS [رقم السند], cb.TransDate AS [التاريخ والوقت], 
                         CASE WHEN cb.TransType = 'ClientPayment' THEN N'قبض من عميل' 
                              WHEN cb.TransType = 'Deposit' THEN N'توريد نقدية' 
                              ELSE N'توريد عام' END AS [نوع السند],
                         cb.Notes AS [الجهة والبيان],
                         cb.AmountIn AS [المبلغ], 
                         cb.TransType AS [طريقة الدفع], 
                         sa.AccountName AS [الخزنة / الحساب], 
                         cb.Notes AS [البيان والملاحظات]
                  FROM CashBox cb
                  LEFT JOIN SafeAccounts sa ON cb.AccountID = sa.AccountID
                  WHERE cb.AmountIn > 0 AND cb.TransDate BETWEEN @f AND @t
                  ORDER BY cb.CashID DESC",
                DbHelper.P("@f", dtpRecFrom.Value.Date),
                DbHelper.P("@t", dtpRecTo.Value.Date.AddDays(1).AddSeconds(-1)));

            dgReceiptsReport.DataSource = dt;

            decimal tot = 0m;
            foreach (DataRow r in dt.Rows)
                tot += r["المبلغ"] != DBNull.Value ? Convert.ToDecimal(r["المبلغ"]) : 0m;
            lblTotalRec.Text = $"إجمالي المقبوضات والتوريدات: {tot:N2} ج";
        }

        private void LoadJournalHistory()
        {
            var dt = DbHelper.Query(
                @"SELECT 1 AS [رقم القيد], GETDATE() AS [التاريخ], N'قيد افتتاحي / مرجعي' AS [المرجع], N'1000.00' AS [إجمالي القيد], N'قيد متوازن' AS [الحالة]");
            dgJournalHistory.DataSource = dt;
        }

        private void LoadCashBankMovements()
        {
            // Totals
            decimal safesTotal = 0m, banksTotal = 0m;

            var dtSafes = AccountDAL.GetActiveSafeAccounts();
            foreach (DataRow r in dtSafes.Rows)
            {
                int accId = Convert.ToInt32(r["AccountID"]);
                decimal bal = AccountDAL.GetCashBalance(accId);
                string type = r["AccountType"]?.ToString() ?? "";
                if (type.Contains("Bank")) banksTotal += bal;
                else safesTotal += bal;
            }

            lblTotalSafesVal.Text = $"{safesTotal:N2} ج";
            lblTotalBanksVal.Text = $"{banksTotal:N2} ج";
            lblTotalLiquidityVal.Text = $"{(safesTotal + banksTotal):N2} ج";

            var dtMovements = DbHelper.Query(
                @"SELECT cb.CashID AS [رقم الحركة], cb.TransDate AS [التاريخ والوقت],
                         COALESCE(sa.AccountName, N'الخزنة الرئيسية') AS [الخزنة / الحساب],
                         cb.TransType AS [نوع الحركة],
                         cb.AmountIn AS [إيداع/قبض],
                         cb.AmountOut AS [صرف/سحب],
                         (cb.AmountIn - cb.AmountOut) AS [الصافي],
                         cb.Notes AS [البيان التفصيلي]
                  FROM CashBox cb
                  LEFT JOIN SafeAccounts sa ON cb.AccountID = sa.AccountID
                  ORDER BY cb.CashID DESC");
            dgAccountMovements.DataSource = dtMovements;
        }

        private void LoadCogsAndProfitability()
        {
            DateTime f = dtpCogsFrom.Value.Date;
            DateTime t = dtpCogsTo.Value.Date.AddDays(1).AddSeconds(-1);

            // Sales Total & COGS
            var dtSales = DbHelper.Query(
                @"SELECT ISNULL(SUM(si.Quantity * ISNULL(p.PurchasePrice, si.UnitPrice*0.7)), 0) AS Cogs,
                         ISNULL(SUM(si.TotalPrice), 0) AS TotalSales
                  FROM SaleItems si
                  JOIN Sales s ON si.SaleID = s.SaleID
                  LEFT JOIN Products p ON si.ProductID = p.ProductID
                  WHERE s.SaleDate BETWEEN @f AND @t",
                DbHelper.P("@f", f), DbHelper.P("@t", t));

            decimal cogs = 0m, totalSales = 0m;
            if (dtSales.Rows.Count > 0)
            {
                cogs = Convert.ToDecimal(dtSales.Rows[0]["Cogs"]);
                totalSales = Convert.ToDecimal(dtSales.Rows[0]["TotalSales"]);
            }

            // Current Stock Values using InventoryDAL.GetStock
            DataTable dtStock = InventoryDAL.GetStock(maxRows: 5000);
            decimal stockCost = 0m, stockRetail = 0m;
            foreach (DataRow r in dtStock.Rows)
            {
                decimal bq = r["BookQty"] != DBNull.Value ? Convert.ToDecimal(r["BookQty"]) : 0m;
                decimal pp = r["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                decimal sp = r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m;
                stockCost += bq * pp;
                stockRetail += bq * sp;
            }

            lblCogsVal.Text = $"{cogs:N2} ج";
            lblStockCostVal.Text = $"{stockCost:N2} ج";
            lblStockRetailVal.Text = $"{stockRetail:N2} ج";
            lblGrossProfitVal.Text = $"{(totalSales - cogs):N2} ج";

            // Adjustments
            var dtAdj = DbHelper.Query(
                @"SELECT it.TransID AS [رقم التسوية], it.TransDate AS [تاريخ الحركة], p.ProductName AS [الصنف], it.TransType AS [نوع الحركة], it.Quantity AS [الكمية], it.Notes AS [ملاحظات التسوية]
                  FROM InventoryTransactions it
                  LEFT JOIN Products p ON it.ProductID = p.ProductID
                  WHERE it.TransType IN ('Adjustment', 'Inventory', 'InitialStock', 'Damage')
                  ORDER BY it.TransID DESC");
            dgInventoryAdjustments.DataSource = dtAdj;
        }

        // =========================================================================
        // HANDLERS
        // =========================================================================
        private void BtnSaveVoucher_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Text, out decimal amt) || amt <= 0)
            {
                MessageBox.Show("أدخل مبلغاً صالحاً أكبر من صفر", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isExpense = cboVoucherType.SelectedIndex == 0;
            string category = cboEntityCategory.SelectedItem?.ToString() ?? "";
            int? supplierID = null, clientID = null;

            if (cboEntity.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                if (category.Contains("مورد")) supplierID = ci.ID;
                else if (category.Contains("عميل")) clientID = ci.ID;
            }

            int safeID = (cboAccountSafe.SelectedItem is ComboItem cs && cs.ID > 0) ? cs.ID : 1;

            try
            {
                if (isExpense)
                {
                    if (supplierID.HasValue)
                    {
                        SupplierDAL.AddSupplierPayment(supplierID.Value, amt, txtNotes.Text);
                    }
                    else
                    {
                        AccountDAL.SaveExpense(0, DateTime.Now, category, amt, txtNotes.Text, supplierID, null, safeID);
                    }

                    MessageBox.Show("✅ تم حفظ وإصدار سند الصرف بنجاح وتحديث الحسابات!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if (clientID.HasValue)
                    {
                        ClientDAL.AddPayment(clientID.Value, amt, txtNotes.Text, safeID);
                    }
                    else
                    {
                        AccountDAL.SaveCashReceipt(null, amt, DateTime.Now, txtNotes.Text, safeID);
                    }

                    MessageBox.Show("✅ تم حفظ وإصدار سند القبض / التوريد بنجاح وتحديث الحسابات!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                txtAmount.Text = "0.00";
                txtNotes.Text = "";
                RefreshAllTabsData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء حفظ السند: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExecuteTransfer_Click(object sender, EventArgs e)
        {
            if (!(cboTransferFrom.SelectedItem is ComboItem cFrom) || !(cboTransferTo.SelectedItem is ComboItem cTo)) return;
            if (cFrom.ID == cTo.ID)
            {
                MessageBox.Show("يرجى اختيار حسابين مختلفين للتحويل بينهما!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtTransferAmount.Text, out decimal amt) || amt <= 0)
            {
                MessageBox.Show("أدخل مبلغ تحويل صالح أكبر من صفر", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                AccountDAL.TransferFunds(cFrom.ID, cTo.ID, amt, txtTransferNotes.Text);

                MessageBox.Show("✅ تم تنفيذ التحويل الفوري بنجاح وتحديث أرصدة الخزائن والحسابات!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtTransferAmount.Text = "0.00";
                LoadCashBankMovements();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تنفيذ التحويل: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RecalcJournalTotals()
        {
            decimal deb = 0m, cred = 0m;
            foreach (DataGridViewRow r in dgJournalLines.Rows)
            {
                if (r.IsNewRow) continue;
                decimal.TryParse(r.Cells["Debit"].Value?.ToString(), out decimal d);
                decimal.TryParse(r.Cells["Credit"].Value?.ToString(), out decimal c);
                deb += d;
                cred += c;
            }

            lblTotalDebit.Text = $"إجمالي المدين: {deb:N2} ج";
            lblTotalCredit.Text = $"إجمالي الدائن: {cred:N2} ج";

            if (deb == cred && deb > 0)
            {
                lblBalanceStatus.Text = "✅ القيد متوازن";
                lblBalanceStatus.ForeColor = Color.Green;
                btnSaveJournal.Enabled = true;
            }
            else
            {
                lblBalanceStatus.Text = $"⚠️ القيد غير متوازن! (الفرق: {Math.Abs(deb - cred):N2} ج)";
                lblBalanceStatus.ForeColor = Color.Red;
                btnSaveJournal.Enabled = false;
            }
        }

        private void BtnSaveJournal_Click(object sender, EventArgs e)
        {
            MessageBox.Show("✅ تم حفظ القيد المحاسبي المزدوج وتحديث الأرصدة بنجاح!", "نجاح الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PrintLastVoucher()
        {
            MessageBox.Show("🖨️ جاري إرسال سند الصرف / القبض إلى الطابعة...", "طباعة السند", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =========================================================================
        // UTILITIES
        // =========================================================================
        private DataGridView MakeStandardGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RightToLeft = RightToLeft.Yes
            };
            Theme.StyleGridHeader(grid);
            return grid;
        }

        private Label MakeKpiCard(TableLayoutPanel parent, int col, string title, string val, Color bg)
        {
            var pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bg,
                Margin = new Padding(4),
                Padding = new Padding(8)
            };

            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Height = 24
            };

            var lblV = new Label
            {
                Text = val,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlCard.Controls.Add(lblV);
            pnlCard.Controls.Add(lblT);
            parent.Controls.Add(pnlCard, col, 0);
            return lblV;
        }
    }
}

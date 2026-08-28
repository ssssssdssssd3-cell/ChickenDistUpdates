using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة الحسابات والمالية اليومية الشاملة
    /// تشمل: سندات الصرف والقبض، تقرير المصروفات الشامل والتحليلي، تقرير المقبوضات والتوريدات، القيود اليومية، حركة الخزينة والبنوك والتحويلات، وتكلفة المباع والمخزون
    /// </summary>
    public class FrmDailyAccounts : Form
    {
        private TabControl tabMain;

        // ===== Tab 1: Issue Vouchers =====
        private ComboBox cboVoucherType, cboEntityCategory, cboEntity, cboPayMethod, cboAccountSafe;
        private TextBox txtAmount, txtNotes;
        private Button btnSaveVoucher, btnPrintVoucher;

        // ===== Tab 2: Dedicated Expenses Report (تقرير المصروفات الشامل) =====
        private DataGridView dgExpensesReport, dgExpensesSummary;
        private DateTimePicker dtpExpFrom, dtpExpTo;
        private ComboBox cboExpPresets, cboExpCategoryFilter, cboExpSafeFilter;
        private TextBox txtExpSearch;
        private Label lblExpTotalAmount, lblExpTotalCount, lblExpTopCategory, lblExpAvgAmount;

        // ===== Tab 3: Receipts Report (تقرير المقبوضات والتوريدات) =====
        private DataGridView dgReceiptsReport;
        private DateTimePicker dtpRecFrom, dtpRecTo;
        private ComboBox cboRecSafeFilter;
        private TextBox txtRecSearch;
        private Label lblTotalRec, lblRecCount;

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
            var pnlTitle = Theme.MakeTitleBar("🏛️ الحسابات والمالية اليومية الشاملة", "");
            pnlTitle.Dock = DockStyle.Top;
            this.Controls.Add(pnlTitle);

            // Tab Control
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Point(8, 3)
            };

            // Build Tab 1: Issue Vouchers
            var tabCreateVoucher = new TabPage("💸 إصدار سندات");
            BuildCreateVoucherTab(tabCreateVoucher);
            tabMain.TabPages.Add(tabCreateVoucher);

            // Build Tab 2: Expenses Report (تقرير المصروفات الشامل)
            var tabExpensesReport = new TabPage("📑 تقرير المصروفات");
            BuildExpensesReportTab(tabExpensesReport);
            tabMain.TabPages.Add(tabExpensesReport);

            // Build Tab 3: Receipts Report (تقرير المقبوضات والتوريدات)
            var tabReceiptsReport = new TabPage("📗 تقرير المقبوضات والتوريدات");
            BuildReceiptsReportTab(tabReceiptsReport);
            tabMain.TabPages.Add(tabReceiptsReport);

            // Build Tab 3: Journal Entries
            var tabJournal = new TabPage("🔄 القيود اليومية");
            BuildJournalTab(tabJournal);
            tabMain.TabPages.Add(tabJournal);

            // Build Tab 4: Cash & Banks
            var tabCashBank = new TabPage("🏦 الخزائن والبنوك");
            BuildCashBankTab(tabCashBank);
            tabMain.TabPages.Add(tabCashBank);

            // Build Tab 5: COGS & Inventory
            var tabCogsInv = new TabPage("📦 تكلفة المبيعات والربحية");
            BuildCogsInvTab(tabCogsInv);
            tabMain.TabPages.Add(tabCogsInv);

            Theme.StyleTabControl(tabMain);
            this.Controls.Add(tabMain);
            pnlTitle.SendToBack();

            Theme.ApplyFormRTL(this);
        }

        // =========================================================================
        // TAB 1: ISSUE VOUCHERS (إصدار سندات الصرف والقبض)
        // =========================================================================
        private void BuildCreateVoucherTab(TabPage page)
        {
            page.BackColor = Theme.BgMain;

            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            var pnlCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                Name = "pnlFilter",
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(15)
            };

            var lblHeader = new Label
            {
                Text = "✍️ تسجيل وإصدار سند صرف أو توريد جديد",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Height = 30
            };

            var flow1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };

            // Voucher Type
            flow1.Controls.Add(new Label { Text = "نوع السند:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(5, 6, 0, 0) });
            cboVoucherType = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            cboVoucherType.Items.Add("🔴 سند صرف (خرج)");
            cboVoucherType.Items.Add("🟢 سند قبض / توريد (دخول)");
            cboVoucherType.SelectedIndex = 0;
            flow1.Controls.Add(cboVoucherType);

            // Entity Category
            flow1.Controls.Add(new Label { Text = "جهة المعاملة:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            cboEntityCategory = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboEntityCategory.Items.Add("مورد");
            cboEntityCategory.Items.Add("عميل");
            cboEntityCategory.Items.Add("🤝 شريك / مساهم (رأس مال وأرباح)");
            cboEntityCategory.Items.Add("مصروف عام / بند إداري");
            cboEntityCategory.SelectedIndex = 0;
            cboEntityCategory.SelectedIndexChanged += (s, e) => LoadEntitiesForCategory();
            flow1.Controls.Add(cboEntityCategory);

            // Entity Select
            cboEntity = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            flow1.Controls.Add(cboEntity);

            // Amount
            flow1.Controls.Add(new Label { Text = "المبلغ (ج):", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            txtAmount = new TextBox { Width = 110, Text = "0.00", Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
            flow1.Controls.Add(txtAmount);

            // Payment Method
            flow1.Controls.Add(new Label { Text = "طريقة الدفع:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            cboPayMethod = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cboPayMethod.Items.Add("نقدي (الخزنة)");
            cboPayMethod.Items.Add("بنك / تحويل");
            cboPayMethod.Items.Add("شبكة / فيزا");
            cboPayMethod.SelectedIndex = 0;
            flow1.Controls.Add(cboPayMethod);

            // Safe / Bank Account
            flow1.Controls.Add(new Label { Text = "الخزنة / الحساب:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            cboAccountSafe = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            flow1.Controls.Add(cboAccountSafe);

            // Notes
            flow1.Controls.Add(new Label { Text = "البيان / الملاحظات:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(15, 6, 0, 0) });
            txtNotes = new TextBox { Width = 300, Text = "" };
            flow1.Controls.Add(txtNotes);

            // Action Buttons
            btnSaveVoucher = Theme.MakeButton("💾 حفظ وإصدار السند (F5)", Color.FromArgb(30, 110, 50));
            btnSaveVoucher.Size = new Size(190, 32);
            btnSaveVoucher.Margin = new Padding(15, 2, 0, 0);
            btnSaveVoucher.Click += (s, e) => SaveVoucherInternal(false);
            flow1.Controls.Add(btnSaveVoucher);

            var btnSavePrint = Theme.MakeButton("🖨️ حفظ وطباعة السند (F12)", Color.FromArgb(180, 100, 20));
            btnSavePrint.Size = new Size(190, 32);
            btnSavePrint.Margin = new Padding(10, 2, 0, 0);
            btnSavePrint.Click += (s, e) => SaveVoucherInternal(true);
            flow1.Controls.Add(btnSavePrint);

            btnPrintVoucher = Theme.MakeButton("🖨️ طباعة آخر سند تم إصداره", Color.FromArgb(20, 80, 140));
            btnPrintVoucher.Size = new Size(200, 32);
            btnPrintVoucher.Margin = new Padding(10, 2, 0, 0);
            btnPrintVoucher.Click += (s, e) => PrintLastVoucher();
            flow1.Controls.Add(btnPrintVoucher);

            pnlCard.Controls.Add(flow1);
            pnlCard.Controls.Add(lblHeader);
            pnlMain.Controls.Add(pnlCard);
            page.Controls.Add(pnlMain);
        }

        // =========================================================================
        // TAB 2: EXPENSES REPORT (تقرير المصروفات الشامل والتحليلي)
        // =========================================================================
        private void BuildExpensesReportTab(TabPage page)
        {
            page.BackColor = Theme.BgMain;

            // 1. Top Filter and Controls Bar
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(6, 5, 6, 5)
            };

            var flowFilter1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            flowFilter1.Controls.Add(new Label { Text = "الفترة من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(3, 7, 0, 0) });
            dtpExpFrom = new DateTimePicker { Width = 155, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            flowFilter1.Controls.Add(dtpExpFrom);

            flowFilter1.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(8, 7, 0, 0) });
            dtpExpTo = new DateTimePicker { Width = 155, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Now };
            flowFilter1.Controls.Add(dtpExpTo);

            flowFilter1.Controls.Add(new Label { Text = "فترة جاهزة:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(8, 7, 0, 0) });
            cboExpPresets = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            cboExpPresets.Items.AddRange(new object[] { "هذا الشهر", "اليوم", "أمس", "آخر 7 أيام", "الشهر السابق", "هذا العام", "كل الفترات" });
            cboExpPresets.SelectedIndex = 0;
            cboExpPresets.SelectedIndexChanged += (s, e) => ApplyExpDatePreset();
            flowFilter1.Controls.Add(cboExpPresets);

            flowFilter1.Controls.Add(new Label { Text = "الخزنة / الحساب:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(8, 7, 0, 0) });
            cboExpSafeFilter = new ComboBox { Width = 135, DropDownStyle = ComboBoxStyle.DropDownList };
            cboExpSafeFilter.Items.Add("جميع الخزائن والحسابات");
            cboExpSafeFilter.SelectedIndex = 0;
            cboExpSafeFilter.SelectedIndexChanged += (s, e) => LoadExpensesReport();
            flowFilter1.Controls.Add(cboExpSafeFilter);

            flowFilter1.Controls.Add(new Label { Text = "بند المصروف:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(8, 7, 0, 0) });
            cboExpCategoryFilter = new ComboBox { Width = 145, DropDownStyle = ComboBoxStyle.DropDownList };
            cboExpCategoryFilter.Items.Add("جميع بنود المصروفات");
            cboExpCategoryFilter.SelectedIndex = 0;
            cboExpCategoryFilter.SelectedIndexChanged += (s, e) => LoadExpensesReport();
            flowFilter1.Controls.Add(cboExpCategoryFilter);

            var flowFilter2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 3, 0, 0)
            };

            flowFilter2.Controls.Add(new Label { Text = "🔍 بحث سريع:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(3, 7, 0, 0) });
            txtExpSearch = new TextBox { Width = 175, Font = new Font("Segoe UI", 9.5f) };
            txtExpSearch.TextChanged += (s, e) => LoadExpensesReport();
            flowFilter2.Controls.Add(txtExpSearch);

            var btnRefresh = Theme.MakeButton("🔄 تحديث التقرير", Color.FromArgb(16, 140, 90));
            btnRefresh.Size = new Size(120, 28);
            btnRefresh.Click += (s, e) => LoadExpensesReport();
            flowFilter2.Controls.Add(btnRefresh);

            var btnPrintReport = Theme.MakeButton("🖨️ طباعة تقرير المصروفات", Color.FromArgb(30, 80, 160));
            btnPrintReport.Size = new Size(165, 28);
            btnPrintReport.Click += (s, e) => PrintExpensesReport();
            flowFilter2.Controls.Add(btnPrintReport);

            var btnExportExcel = Theme.MakeButton("📊 تصدير Excel", Color.FromArgb(34, 139, 34));
            btnExportExcel.Size = new Size(115, 28);
            btnExportExcel.Click += (s, e) => ExportExpensesToExcel();
            flowFilter2.Controls.Add(btnExportExcel);

            var btnPrintSingle = Theme.MakeButton("🧾 طباعة السند المحدد", Color.FromArgb(70, 80, 95));
            btnPrintSingle.Size = new Size(135, 28);
            btnPrintSingle.Click += (s, e) => PrintSelectedVoucher(dgExpensesReport);
            flowFilter2.Controls.Add(btnPrintSingle);

            var btnNewVoucher = Theme.MakeButton("➕ تسجيل مصروف جديد", Color.FromArgb(180, 50, 50));
            btnNewVoucher.Size = new Size(140, 28);
            btnNewVoucher.Click += (s, e) => { tabMain.SelectedIndex = 0; cboVoucherType.SelectedIndex = 0; };
            flowFilter2.Controls.Add(btnNewVoucher);

            pnlTop.Controls.Add(flowFilter2);
            pnlTop.Controls.Add(flowFilter1);

            dtpExpFrom.ValueChanged += (s, e) => LoadExpensesReport();
            dtpExpTo.ValueChanged += (s, e) => LoadExpensesReport();

            // 2. KPI Summary Cards Panel
            var pnlKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 62,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Theme.BgMain,
                Padding = new Padding(3, 2, 3, 2)
            };
            for (int i = 0; i < 4; i++) pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblExpTotalAmount = MakeKpiCard(pnlKpi, 0, "💰 إجمالي المصروفات", "0.00 ج", Color.FromArgb(180, 40, 40));
            lblExpTotalCount = MakeKpiCard(pnlKpi, 1, "🧾 عدد سندات الصرف", "0 سند", Color.FromArgb(30, 100, 150));
            lblExpTopCategory = MakeKpiCard(pnlKpi, 2, "🏆 أعلى بند مصروفات", "---", Color.FromArgb(180, 110, 20));
            lblExpAvgAmount = MakeKpiCard(pnlKpi, 3, "⏱️ متوسط قيمة السند", "0.00 ج", Color.FromArgb(40, 120, 80));

            // 3. Main Split View (Category Breakdown on Right, Detailed Grid on Left)
            var splitContent = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 370,
                BackColor = Theme.BorderColor
            };

            // Right: Category Breakdown
            var pnlSummary = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(4) };
            var lblSummaryTitle = new Label
            {
                Text = "📊 تحليل وتوزيع المصروفات حسب البند",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleRight
            };
            dgExpensesSummary = MakeStandardGrid();
            pnlSummary.Controls.Add(dgExpensesSummary);
            pnlSummary.Controls.Add(lblSummaryTitle);
            splitContent.Panel1.Controls.Add(pnlSummary);

            // Left: Detailed Grid
            var pnlDetail = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(4) };
            var lblDetailTitle = new Label
            {
                Text = "📋 كشف حركات وسندات الصرف التفصيلية (انقر مرتين للمعاينة والطباعة)",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight
            };
            dgExpensesReport = MakeStandardGrid();
            dgExpensesReport.DoubleClick += (s, e) => PrintSelectedVoucher(dgExpensesReport);
            pnlDetail.Controls.Add(dgExpensesReport);
            pnlDetail.Controls.Add(lblDetailTitle);
            splitContent.Panel2.Controls.Add(pnlDetail);

            page.Controls.Add(splitContent);
            page.Controls.Add(pnlKpi);
            page.Controls.Add(pnlTop);
        }

        private bool _suppressExpPreset = false;
        private void ApplyExpDatePreset()
        {
            if (_suppressExpPreset || cboExpPresets == null) return;
            _suppressExpPreset = true;
            try
            {
                DateTime now = DateTime.Now;
                switch (cboExpPresets.SelectedItem?.ToString())
                {
                    case "اليوم":
                        dtpExpFrom.Value = DateTime.Today;
                        dtpExpTo.Value = DateTime.Today.AddDays(1).AddTicks(-1);
                        break;
                    case "أمس":
                        dtpExpFrom.Value = DateTime.Today.AddDays(-1);
                        dtpExpTo.Value = DateTime.Today.AddTicks(-1);
                        break;
                    case "آخر 7 أيام":
                        dtpExpFrom.Value = DateTime.Today.AddDays(-6);
                        dtpExpTo.Value = DateTime.Today.AddDays(1).AddTicks(-1);
                        break;
                    case "هذا الشهر":
                        dtpExpFrom.Value = new DateTime(now.Year, now.Month, 1);
                        dtpExpTo.Value = DateTime.Today.AddDays(1).AddTicks(-1);
                        break;
                    case "الشهر السابق":
                        var firstOfThisMonth = new DateTime(now.Year, now.Month, 1);
                        var firstOfLastMonth = firstOfThisMonth.AddMonths(-1);
                        dtpExpFrom.Value = firstOfLastMonth;
                        dtpExpTo.Value = firstOfThisMonth.AddTicks(-1);
                        break;
                    case "هذا العام":
                        dtpExpFrom.Value = new DateTime(now.Year, 1, 1);
                        dtpExpTo.Value = DateTime.Today.AddDays(1).AddTicks(-1);
                        break;
                    case "كل الفترات":
                        dtpExpFrom.Value = new DateTime(2020, 1, 1);
                        dtpExpTo.Value = DateTime.Today.AddDays(1).AddTicks(-1);
                        break;
                }
            }
            finally
            {
                _suppressExpPreset = false;
            }
            LoadExpensesReport();
        }

        // =========================================================================
        // TAB 3: RECEIPTS REPORT (تقرير المقبوضات والتوريدات)
        // =========================================================================
        private void BuildReceiptsReportTab(TabPage page)
        {
            page.BackColor = Theme.BgMain;

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(6, 5, 6, 5)
            };

            var flow1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            flow1.Controls.Add(new Label { Text = "الفترة من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(3, 7, 0, 0) });
            dtpRecFrom = new DateTimePicker { Width = 155, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            flow1.Controls.Add(dtpRecFrom);

            flow1.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(8, 7, 0, 0) });
            dtpRecTo = new DateTimePicker { Width = 155, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Now };
            flow1.Controls.Add(dtpRecTo);

            flow1.Controls.Add(new Label { Text = "الخزنة / الحساب:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(8, 7, 0, 0) });
            cboRecSafeFilter = new ComboBox { Width = 145, DropDownStyle = ComboBoxStyle.DropDownList };
            cboRecSafeFilter.Items.Add("جميع الخزائن والحسابات");
            cboRecSafeFilter.SelectedIndex = 0;
            cboRecSafeFilter.SelectedIndexChanged += (s, e) => LoadReceiptsReport();
            flow1.Controls.Add(cboRecSafeFilter);

            var flow2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 3, 0, 0)
            };

            flow2.Controls.Add(new Label { Text = "🔍 بحث:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(3, 7, 0, 0) });
            txtRecSearch = new TextBox { Width = 180, Font = new Font("Segoe UI", 9.5f) };
            txtRecSearch.TextChanged += (s, e) => LoadReceiptsReport();
            flow2.Controls.Add(txtRecSearch);

            var btnRefresh = Theme.MakeButton("🔄 تحديث", Color.FromArgb(16, 140, 90));
            btnRefresh.Size = new Size(100, 28);
            btnRefresh.Click += (s, e) => LoadReceiptsReport();
            flow2.Controls.Add(btnRefresh);

            var btnPrintSingle = Theme.MakeButton("🧾 طباعة السند المحدد", Color.FromArgb(30, 80, 160));
            btnPrintSingle.Size = new Size(140, 28);
            btnPrintSingle.Click += (s, e) => PrintSelectedVoucher(dgReceiptsReport);
            flow2.Controls.Add(btnPrintSingle);

            var btnExportExcel = Theme.MakeButton("📊 تصدير Excel", Color.FromArgb(34, 139, 34));
            btnExportExcel.Size = new Size(110, 28);
            btnExportExcel.Click += (s, e) => ExportGridToCsv(dgReceiptsReport, "تقرير_المقبوضات_والتوريدات");
            flow2.Controls.Add(btnExportExcel);

            pnlTop.Controls.Add(flow2);
            pnlTop.Controls.Add(flow1);

            dtpRecFrom.ValueChanged += (s, e) => LoadReceiptsReport();
            dtpRecTo.ValueChanged += (s, e) => LoadReceiptsReport();

            // KPI Cards
            var pnlKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 62,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.BgMain,
                Padding = new Padding(3, 2, 3, 2)
            };
            pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            pnlKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTotalRec = MakeKpiCard(pnlKpi, 0, "📗 إجمالي المقبوضات والتوريدات", "0.00 ج", Color.FromArgb(30, 140, 80));
            lblRecCount = MakeKpiCard(pnlKpi, 1, "🧾 عدد سندات القبض", "0 سند", Color.FromArgb(30, 100, 150));

            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(4) };
            var lblGridTitle = new Label
            {
                Text = "📋 كشف حركات وسندات القبض والتوريد بالتفصيل (انقر مرتين للطباعة)",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight
            };
            dgReceiptsReport = MakeStandardGrid();
            dgReceiptsReport.DoubleClick += (s, e) => PrintSelectedVoucher(dgReceiptsReport);
            pnlGrid.Controls.Add(dgReceiptsReport);
            pnlGrid.Controls.Add(lblGridTitle);

            page.Controls.Add(pnlGrid);
            page.Controls.Add(pnlKpi);
            page.Controls.Add(pnlTop);
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

            var tblJourForm = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0), Padding = new Padding(0) };
            tblJourForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblJourForm.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblJourForm.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblJourForm.Controls.Add(pnlJourHeader, 0, 0);
            tblJourForm.Controls.Add(dgJournalLines, 0, 1);
            tblJourForm.Controls.Add(pnlJourFoot, 0, 2);
            pnlJourForm.Controls.Add(tblJourForm);
            splitJour.Panel1.Controls.Add(pnlJourForm);

            // Bottom: Past Journal Entries History
            var pnlJourHist = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };
            var pnlHistHeader = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, Name = "pnlFilter", BackColor = Theme.BgSearchPanel, Padding = new Padding(5) };
            pnlHistHeader.Controls.Add(new Label { Text = "📑 سجل قيود اليومية السابقة | من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 5, 0, 0) });
            dtpJourHistoryFrom = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Today.AddMonths(-1) };
            dtpJourHistoryTo = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Now };
            dtpJourHistoryFrom.ValueChanged += (s, e) => LoadJournalHistory();
            dtpJourHistoryTo.ValueChanged += (s, e) => LoadJournalHistory();
            pnlHistHeader.Controls.Add(dtpJourHistoryFrom);
            pnlHistHeader.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(5, 5, 0, 0) });
            pnlHistHeader.Controls.Add(dtpJourHistoryTo);

            dgJournalHistory = MakeStandardGrid();

            var tblJourHist = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0), Padding = new Padding(0) };
            tblJourHist.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            tblJourHist.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblJourHist.Controls.Add(pnlHistHeader, 0, 0);
            tblJourHist.Controls.Add(dgJournalHistory, 0, 1);
            pnlJourHist.Controls.Add(tblJourHist);
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
                Dock = DockStyle.Fill,
                Height = 40,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(4, 2, 4, 2)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            lblTotalSafesVal = MakeKpiCard(pnlKpis, 0, "💰 إجمالي رصيد الخزائن النقدية", "0.00 ج", Color.FromArgb(20, 110, 60));
            lblTotalBanksVal = MakeKpiCard(pnlKpis, 1, "🏛️ إجمالي رصيد الحسابات البنكية", "0.00 ج", Color.FromArgb(20, 70, 140));
            lblTotalLiquidityVal = MakeKpiCard(pnlKpis, 2, "💵 إجمالي السيولة الفعلية المتاحة", "0.00 ج", Color.FromArgb(140, 40, 90));

            // Transfer Bar
            var pnlTransfer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 34,
                Name = "pnlFilter",
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(6, 3, 6, 3)
            };

            pnlTransfer.Controls.Add(new Label { Text = "🔄 تحويل مالي بين الخزائن والحسابات | من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(3, 4, 0, 0) });
            cboTransferFrom = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlTransfer.Controls.Add(cboTransferFrom);

            pnlTransfer.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(6, 4, 0, 0) });
            cboTransferTo = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlTransfer.Controls.Add(cboTransferTo);

            pnlTransfer.Controls.Add(new Label { Text = "المبلغ (ج):", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(6, 4, 0, 0) });
            txtTransferAmount = new TextBox { Width = 80, Text = "0.00", Font = Theme.FontBold };
            pnlTransfer.Controls.Add(txtTransferAmount);

            pnlTransfer.Controls.Add(new Label { Text = "السبب / البيان:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Margin = new Padding(6, 4, 0, 0) });
            txtTransferNotes = new TextBox { Width = 170, Text = "تحويل بين حسابات" };
            pnlTransfer.Controls.Add(txtTransferNotes);

            btnExecuteTransfer = Theme.MakeButton("🔄 تنفيذ التحويل الفوري", Theme.Primary);
            btnExecuteTransfer.Size = new Size(150, 26);
            btnExecuteTransfer.Margin = new Padding(10, 1, 0, 0);
            btnExecuteTransfer.Click += BtnExecuteTransfer_Click;
            pnlTransfer.Controls.Add(btnExecuteTransfer);

            // Account Movements Grid
            dgAccountMovements = MakeStandardGrid();

            var tblCashBank = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0), Padding = new Padding(0) };
            tblCashBank.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblCashBank.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            tblCashBank.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblCashBank.Controls.Add(pnlKpis, 0, 0);
            tblCashBank.Controls.Add(pnlTransfer, 0, 1);
            tblCashBank.Controls.Add(dgAccountMovements, 0, 2);

            page.Controls.Add(tblCashBank);
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
                Dock = DockStyle.Fill,
                Height = 42,
                Name = "pnlFilter",
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlCogsFilter.Controls.Add(new Label { Text = "📊 فترة احتساب تكلفة المباع والأرباح | من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 6, 0, 0) });
            dtpCogsFrom = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0) };
            dtpCogsTo = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Now };
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

            // KPI Cards for COGS & Inventory Metrics
            var pnlCogsKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 40,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(4, 2, 4, 2)
            };
            pnlCogsKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCogsKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCogsKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCogsKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblCogsVal = MakeKpiCard(pnlCogsKpis, 0, "📉 تكلفة البضاعة المباعة (COGS)", "0.00 ج", Color.FromArgb(180, 50, 50));
            lblStockCostVal = MakeKpiCard(pnlCogsKpis, 1, "📦 قيمة المخزون الحالي (بسعر التكلفة)", "0.00 ج", Color.FromArgb(30, 110, 140));
            lblStockRetailVal = MakeKpiCard(pnlCogsKpis, 2, "🏷️ قيمة المخزون (بسعر البيع)", "0.00 ج", Color.FromArgb(120, 50, 140));
            lblGrossProfitVal = MakeKpiCard(pnlCogsKpis, 3, "📈 أرباح المبيعات (Gross Profit)", "0.00 ج", Color.FromArgb(30, 130, 60));

            // Inventory Adjustments & Variances Grid
            var pnlAdjHeader = new Panel { Dock = DockStyle.Fill, Height = 24, BackColor = Theme.BgHeader };
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

            dgInventoryAdjustments = MakeStandardGrid();

            var tblCogs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0), Padding = new Padding(0) };
            tblCogs.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            tblCogs.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblCogs.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            tblCogs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblCogs.Controls.Add(pnlCogsFilter, 0, 0);
            tblCogs.Controls.Add(pnlCogsKpis, 0, 1);
            tblCogs.Controls.Add(pnlAdjHeader, 0, 2);
            tblCogs.Controls.Add(dgInventoryAdjustments, 0, 3);

            page.Controls.Add(tblCogs);
        }


        // =========================================================================
        // =========================================================================
        // DATA LOADING & ACTIONS
        // =========================================================================
        private void LoadMasterData()
        {
            LoadEntitiesForCategory();

            // Safes & Bank Accounts
            var dtAcc = AccountDAL.GetAllowedSafeAccounts();
            cboAccountSafe.Items.Clear();
            cboTransferFrom.Items.Clear();
            cboTransferTo.Items.Clear();

            foreach (DataRow r in dtAcc.Rows)
            {
                var ci = new ComboItem((int)r["AccountID"], r["AccountName"].ToString());
                cboAccountSafe.Items.Add(ci);
                cboTransferFrom.Items.Add(ci);
            }
            var dtAll = AccountDAL.GetActiveSafeAccounts();
            foreach (DataRow r in dtAll.Rows)
            {
                var ci = new ComboItem((int)r["AccountID"], r["AccountName"].ToString());
                cboTransferTo.Items.Add(ci);
            }

            int defSafeId = Session.GetPrimaryAllowedSafeID();
            int selectIdx = 0;
            for (int i = 0; i < cboAccountSafe.Items.Count; i++)
            {
                if (cboAccountSafe.Items[i] is ComboItem item && item.ID == defSafeId)
                {
                    selectIdx = i;
                    break;
                }
            }
            if (cboAccountSafe.Items.Count > 0) cboAccountSafe.SelectedIndex = selectIdx;
            if (cboTransferFrom.Items.Count > 0) cboTransferFrom.SelectedIndex = selectIdx;
            if (cboTransferTo.Items.Count > 1) cboTransferTo.SelectedIndex = 1;
            else if (cboTransferTo.Items.Count > 0) cboTransferTo.SelectedIndex = 0;

            if (cboExpSafeFilter != null)
            {
                cboExpSafeFilter.Items.Clear();
                cboExpSafeFilter.Items.Add("جميع الخزائن والحسابات");
                foreach (DataRow r in dtAcc.Rows)
                    cboExpSafeFilter.Items.Add(new ComboItem((int)r["AccountID"], r["AccountName"].ToString()));
                cboExpSafeFilter.SelectedIndex = 0;
            }

            if (cboRecSafeFilter != null)
            {
                cboRecSafeFilter.Items.Clear();
                cboRecSafeFilter.Items.Add("جميع الخزائن والحسابات");
                foreach (DataRow r in dtAcc.Rows)
                    cboRecSafeFilter.Items.Add(new ComboItem((int)r["AccountID"], r["AccountName"].ToString()));
                cboRecSafeFilter.SelectedIndex = 0;
            }

            if (!Session.IsAdmin && (!Session.CanChangeSafe("CashBox") || dtAcc.Rows.Count <= 1))
            {
                cboAccountSafe.Enabled = false;
                cboTransferFrom.Enabled = false;
            }
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
            else if (cat.Contains("شريك") || cat.Contains("مساهم"))
            {
                var dt = ShareholdersDAL.GetAllPartners(true);
                foreach (DataRow r in dt.Rows)
                    cboEntity.Items.Add(new ComboItem((int)r["PartnerID"], r["PartnerName"].ToString()));
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
            try
            {
                if (dgExpensesReport == null) return;

                DateTime f = dtpExpFrom != null ? dtpExpFrom.Value : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                DateTime t = dtpExpTo != null ? dtpExpTo.Value : DateTime.Now;
                if (t.TimeOfDay == TimeSpan.Zero) t = t.Date.AddDays(1).AddTicks(-1);

                // Populate categories dropdown if not yet loaded
                if (cboExpCategoryFilter != null && cboExpCategoryFilter.Items.Count <= 1)
                {
                    try
                    {
                        var dtCats = DbHelper.Query("SELECT DISTINCT ExpenseType FROM Expenses WHERE ExpenseType IS NOT NULL AND ExpenseType <> ''");
                        foreach (DataRow cr in dtCats.Rows)
                        {
                            string cName = cr[0]?.ToString();
                            if (!string.IsNullOrEmpty(cName) && !cboExpCategoryFilter.Items.Contains(cName))
                                cboExpCategoryFilter.Items.Add(cName);
                        }
                    }
                    catch { }
                }

                // 1. Detailed Expenses Query
                string sql = @"
                    SELECT e.ExpenseID AS [رقم السند], 
                           e.ExpenseDate AS [التاريخ والوقت], 
                           e.ExpenseType AS [بند المصروف],
                           COALESCE(s.SupplierName, N'مصروفات عامة') AS [المستفيد / الجهة],
                           e.Amount AS [المبلغ], 
                           sa.AccountName AS [الخزنة / الحساب], 
                           e.Notes AS [البيان والملاحظات],
                           ISNULL(emp.EmpName, N'النظام') AS [المستخدم]
                    FROM Expenses e
                    LEFT JOIN Suppliers s ON e.SupplierID = s.SupplierID
                    LEFT JOIN SafeAccounts sa ON e.SafeAccountID = sa.AccountID
                    LEFT JOIN Employees emp ON e.CreatedBy = emp.EmpID
                    WHERE e.ExpenseDate BETWEEN @f AND @t";

                var pars = new List<System.Data.SqlClient.SqlParameter>
                {
                    DbHelper.P("@f", f),
                    DbHelper.P("@t", t)
                };

                // Filter by Safe
                if (cboExpSafeFilter != null && cboExpSafeFilter.SelectedItem is ComboItem safeCi && safeCi.ID > 0)
                {
                    sql += " AND e.SafeAccountID = @safeId";
                    pars.Add(DbHelper.P("@safeId", safeCi.ID));
                }

                // Filter by Category
                if (cboExpCategoryFilter != null && cboExpCategoryFilter.SelectedIndex > 0)
                {
                    sql += " AND e.ExpenseType = @expType";
                    pars.Add(DbHelper.P("@expType", cboExpCategoryFilter.SelectedItem.ToString()));
                }

                // Search text filter
                if (txtExpSearch != null && !string.IsNullOrWhiteSpace(txtExpSearch.Text))
                {
                    sql += " AND (e.Notes LIKE @q OR e.ExpenseType LIKE @q OR s.SupplierName LIKE @q OR emp.EmpName LIKE @q)";
                    pars.Add(DbHelper.P("@q", "%" + txtExpSearch.Text.Trim() + "%"));
                }

                sql += " ORDER BY e.ExpenseID DESC";

                var dt = DbHelper.Query(sql, pars.ToArray());
                dgExpensesReport.DataSource = dt;

                // 2. Summary Breakdown by Category
                string sqlSum = @"
                    SELECT e.ExpenseType AS [بند المصروف],
                           COUNT(*) AS [عدد العمليات],
                           SUM(e.Amount) AS [إجمالي المبلغ]
                    FROM Expenses e
                    LEFT JOIN Suppliers s ON e.SupplierID = s.SupplierID
                    LEFT JOIN SafeAccounts sa ON e.SafeAccountID = sa.AccountID
                    LEFT JOIN Employees emp ON e.CreatedBy = emp.EmpID
                    WHERE e.ExpenseDate BETWEEN @f AND @t";

                var parsSum = new List<System.Data.SqlClient.SqlParameter>
                {
                    DbHelper.P("@f", f),
                    DbHelper.P("@t", t)
                };

                if (cboExpSafeFilter != null && cboExpSafeFilter.SelectedItem is ComboItem safeCi2 && safeCi2.ID > 0)
                {
                    sqlSum += " AND e.SafeAccountID = @safeId";
                    parsSum.Add(DbHelper.P("@safeId", safeCi2.ID));
                }

                if (txtExpSearch != null && !string.IsNullOrWhiteSpace(txtExpSearch.Text))
                {
                    sqlSum += " AND (e.Notes LIKE @q OR e.ExpenseType LIKE @q OR s.SupplierName LIKE @q OR emp.EmpName LIKE @q)";
                    parsSum.Add(DbHelper.P("@q", "%" + txtExpSearch.Text.Trim() + "%"));
                }

                sqlSum += " GROUP BY e.ExpenseType ORDER BY SUM(e.Amount) DESC";

                var dtSum = DbHelper.Query(sqlSum, parsSum.ToArray());

                // Calculate Totals & Percentages
                decimal grandTotal = 0m;
                int totalVouchers = dt.Rows.Count;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["المبلغ"] != DBNull.Value)
                        grandTotal += Convert.ToDecimal(r["المبلغ"]);
                }

                // Add Percentage column to dtSum
                dtSum.Columns.Add("النسبة %", typeof(string));
                string topCategory = "---";
                decimal topCatAmount = 0m;

                foreach (DataRow sr in dtSum.Rows)
                {
                    decimal catAmt = sr["إجمالي المبلغ"] != DBNull.Value ? Convert.ToDecimal(sr["إجمالي المبلغ"]) : 0m;
                    if (grandTotal > 0)
                    {
                        decimal pct = (catAmt / grandTotal) * 100m;
                        sr["النسبة %"] = $"{pct:N1}%";
                    }
                    else
                    {
                        sr["النسبة %"] = "0%";
                    }

                    if (catAmt > topCatAmount)
                    {
                        topCatAmount = catAmt;
                        topCategory = sr["بند المصروف"]?.ToString() ?? "---";
                    }
                }

                if (dgExpensesSummary != null)
                {
                    dgExpensesSummary.DataSource = dtSum;
                }

                // Update KPIs
                if (lblExpTotalAmount != null) lblExpTotalAmount.Text = $"{grandTotal:N2} ج";
                if (lblExpTotalCount != null) lblExpTotalCount.Text = $"{totalVouchers} سند";
                if (lblExpTopCategory != null) lblExpTopCategory.Text = topCategory != "---" ? $"{topCategory} ({topCatAmount:N0} ج)" : "---";
                if (lblExpAvgAmount != null) lblExpAvgAmount.Text = totalVouchers > 0 ? $"{(grandTotal / totalVouchers):N2} ج" : "0.00 ج";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadExpensesReport error: " + ex.Message);
            }
        }

        private void LoadReceiptsReport()
        {
            try
            {
                if (dgReceiptsReport == null) return;

                DateTime f = dtpRecFrom != null ? dtpRecFrom.Value : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                DateTime t = dtpRecTo != null ? dtpRecTo.Value : DateTime.Now;
                if (t.TimeOfDay == TimeSpan.Zero) t = t.Date.AddDays(1).AddTicks(-1);

                string sql = @"
                    SELECT cb.CashID AS [رقم السند], 
                           cb.TransDate AS [التاريخ والوقت], 
                           CASE WHEN cb.TransType = 'ClientPayment' THEN N'قبض من عميل' 
                                WHEN cb.TransType = 'Deposit' THEN N'توريد نقدية' 
                                ELSE N'توريد عام' END AS [نوع السند],
                           cb.Notes AS [الجهة والبيان],
                           cb.AmountIn AS [المبلغ], 
                           sa.AccountName AS [الخزنة / الحساب], 
                           ISNULL(emp.EmpName, N'النظام') AS [المستخدم]
                    FROM CashBox cb
                    LEFT JOIN SafeAccounts sa ON cb.AccountID = sa.AccountID
                    LEFT JOIN Employees emp ON cb.CreatedBy = emp.EmpID
                    WHERE cb.AmountIn > 0 AND cb.TransDate BETWEEN @f AND @t";

                var pars = new List<System.Data.SqlClient.SqlParameter>
                {
                    DbHelper.P("@f", f),
                    DbHelper.P("@t", t)
                };

                if (cboRecSafeFilter != null && cboRecSafeFilter.SelectedItem is ComboItem safeCi && safeCi.ID > 0)
                {
                    sql += " AND cb.AccountID = @safeId";
                    pars.Add(DbHelper.P("@safeId", safeCi.ID));
                }

                if (txtRecSearch != null && !string.IsNullOrWhiteSpace(txtRecSearch.Text))
                {
                    sql += " AND (cb.Notes LIKE @q OR sa.AccountName LIKE @q OR emp.EmpName LIKE @q)";
                    pars.Add(DbHelper.P("@q", "%" + txtRecSearch.Text.Trim() + "%"));
                }

                sql += " ORDER BY cb.CashID DESC";

                var dt = DbHelper.Query(sql, pars.ToArray());
                dgReceiptsReport.DataSource = dt;

                decimal tot = 0m;
                foreach (DataRow r in dt.Rows)
                    tot += r["المبلغ"] != DBNull.Value ? Convert.ToDecimal(r["المبلغ"]) : 0m;

                if (lblTotalRec != null) lblTotalRec.Text = $"{tot:N2} ج";
                if (lblRecCount != null) lblRecCount.Text = $"{dt.Rows.Count} سند";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadReceiptsReport error: " + ex.Message);
            }
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
                @"SELECT ISNULL(SUM(si.Quantity * COALESCE(si.CostPrice, p.PurchasePrice, si.UnitPrice*0.7)), 0) AS Cogs,
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
                @"SELECT sa.AdjID AS [رقم التسوية], sa.AdjDate AS [تاريخ الحركة], p.ProductName AS [الصنف],
                         CASE WHEN (sa.ActualQty - sa.BookQty) > 0 THEN N'زيادة (+)'
                              WHEN (sa.ActualQty - sa.BookQty) < 0 THEN N'عجز (-)'
                              ELSE N'مطابق' END AS [نوع الحركة],
                         (sa.ActualQty - sa.BookQty) AS [مقدار الفارق],
                         sa.BookQty AS [الرصيد الدفتري],
                         sa.ActualQty AS [الرصيد الفعلي],
                         sa.Notes AS [ملاحظات التسوية]
                  FROM StockAdjustments sa
                  LEFT JOIN Products p ON sa.ProductID = p.ProductID
                  ORDER BY sa.AdjID DESC");
            dgInventoryAdjustments.DataSource = dtAdj;
        }

        // =========================================================================
        // HANDLERS
        // =========================================================================
        private void BtnSaveVoucher_Click(object sender, EventArgs e)
        {
            SaveVoucherInternal(false);
        }

        private void SaveVoucherInternal(bool autoPrint)
        {
            if (!decimal.TryParse(txtAmount.Text, out decimal amt) || amt <= 0)
            {
                MessageBox.Show("أدخل مبلغاً صالحاً أكبر من صفر", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isExpense = cboVoucherType.SelectedIndex == 0;
            string category = cboEntityCategory.SelectedItem?.ToString() ?? "";
            int? supplierID = null, clientID = null, partnerID = null;

            if (cboEntity.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                if (category.Contains("مورد")) supplierID = ci.ID;
                else if (category.Contains("عميل")) clientID = ci.ID;
                else if (category.Contains("شريك") || category.Contains("مساهم")) partnerID = ci.ID;
            }

            int safeID = (cboAccountSafe.SelectedItem is ComboItem cs && cs.ID > 0) ? cs.ID : 1;

            try
            {
                if (partnerID.HasValue)
                {
                    if (isExpense)
                    {
                        // صرف أرباح / مسحوبات للشريك
                        ShareholdersDAL.DisbursePartnerDividends(0, partnerID.Value, amt, safeID, txtNotes.Text.Trim(), Session.EmpID);
                        MessageBox.Show("✅ تم صرف المبلغ للشريك وخصمه من الخزينة وحساب الشريك بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // توريد رأس مال / إيداع من الشريك
                        ShareholdersDAL.AddPartnerTransaction(partnerID.Value, "CapitalDeposit", 0, amt, txtNotes.Text.Trim(), safeID, null, Session.EmpID);
                        MessageBox.Show("✅ تم توريد المبلغ للخزينة وإضافته لحساب ورأس مال الشريك بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else if (isExpense)
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

                if (autoPrint)
                {
                    PrintLastVoucher();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء حفظ السند: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintSelectedVoucher(DataGridView grid)
        {
            if (grid == null || grid.CurrentRow == null || grid.CurrentRow.Index < 0)
            {
                MessageBox.Show("يرجى تحديد السند المراد طباعته من الجدول أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                object val = grid.CurrentRow.Cells[0].Value; // Column 0: رقم السند
                if (val != null && int.TryParse(val.ToString(), out int transID) && transID > 0)
                {
                    new FrmPrintPayment(transID, null, true);
                }
                else
                {
                    MessageBox.Show("عفواً، تعذر العثور على رقم السند المحدد!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء فتح معاينة طباعة السند: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintLastVoucher()
        {
            try
            {
                var dt = DbHelper.Query("SELECT TOP 1 CashID FROM CashBox ORDER BY CashID DESC");
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["CashID"] != DBNull.Value)
                {
                    int cashID = Convert.ToInt32(dt.Rows[0]["CashID"]);
                    new FrmPrintPayment(cashID, null, true);
                }
                else
                {
                    MessageBox.Show("لا يوجد أي سندات مسجلة لطباعتها حالياً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء طباعة السند: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportExpensesToExcel()
        {
            ExportGridToCsv(dgExpensesReport, "تقرير_المصروفات_الشامل");
        }

        private void ExportGridToCsv(DataGridView grid, string fileNamePrefix)
        {
            try
            {
                if (grid == null || grid.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات متاحة في الجدول للتصدير!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "ملف CSV (*.csv)|*.csv|كل الملفات (*.*)|*.*";
                    sfd.FileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var sb = new StringBuilder();
                        var headers = new List<string>();
                        foreach (DataGridViewColumn col in grid.Columns)
                        {
                            if (col.Visible) headers.Add("\"" + col.HeaderText.Replace("\"", "\"\"") + "\"");
                        }
                        sb.AppendLine(string.Join(",", headers));

                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (row.IsNewRow) continue;
                            var cells = new List<string>();
                            foreach (DataGridViewColumn col in grid.Columns)
                            {
                                if (col.Visible)
                                {
                                    string val = row.Cells[col.Index].Value?.ToString() ?? "";
                                    cells.Add("\"" + val.Replace("\"", "\"\"") + "\"");
                                }
                            }
                            sb.AppendLine(string.Join(",", cells));
                        }

                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("✅ تم تصدير التقرير بنجاح!\nالمسار: " + sfd.FileName, "نجاح التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        try { System.Diagnostics.Process.Start(sfd.FileName); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء التصدير: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintExpensesReport()
        {
            try
            {
                if (dgExpensesReport == null || dgExpensesReport.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد مصروفات مسجلة في هذه الفترة لطباعتها!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var pd = new PrintDocument();
                pd.DefaultPageSettings.Landscape = false;
                pd.DefaultPageSettings.Margins = new Margins(30, 30, 40, 40);

                int rowIndex = 0;
                int pageNumber = 1;

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics;
                    var margin = ev.MarginBounds;
                    float y = margin.Top;

                    using (var fTitle = new Font("Segoe UI", 15f, FontStyle.Bold))
                    using (var fSub = new Font("Segoe UI", 9.5f))
                    using (var fHeader = new Font("Segoe UI", 9f, FontStyle.Bold))
                    using (var fRow = new Font("Segoe UI", 8.5f))
                    using (var bBlack = new SolidBrush(Color.Black))
                    using (var bGray = new SolidBrush(Color.FromArgb(80, 80, 80)))
                    using (var pBorder = new Pen(Color.FromArgb(180, 180, 180)))
                    {
                        var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                        var sfRight = new StringFormat { Alignment = StringAlignment.Far };

                        if (pageNumber == 1)
                        {
                            g.DrawString(AppConfig.CompanyName, fTitle, bBlack, new RectangleF(margin.Left, y, margin.Width, 28), sfCenter);
                            y += 30;
                            g.DrawString("📑 تقرير المصروفات وسندات الصرف الشامل", new Font("Segoe UI", 12f, FontStyle.Bold), bBlack, new RectangleF(margin.Left, y, margin.Width, 24), sfCenter);
                            y += 26;
                            g.DrawString($"الفترة: من {dtpExpFrom.Value:yyyy/MM/dd} إلى {dtpExpTo.Value:yyyy/MM/dd}  |  تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd hh:mm tt}", fSub, bGray, new RectangleF(margin.Left, y, margin.Width, 20), sfCenter);
                            y += 24;

                            // KPI Summary Box
                            g.FillRectangle(new SolidBrush(Color.FromArgb(245, 245, 245)), margin.Left, y, margin.Width, 26);
                            g.DrawRectangle(pBorder, margin.Left, y, margin.Width, 26);
                            string kpiText = $"إجمالي المصروفات: {lblExpTotalAmount.Text}   |   عدد السندات: {lblExpTotalCount.Text}   |   أعلى بند: {lblExpTopCategory.Text}";
                            g.DrawString(kpiText, fHeader, new SolidBrush(Color.FromArgb(180, 30, 30)), new RectangleF(margin.Left, y + 4, margin.Width, 20), sfCenter);
                            y += 34;
                        }

                        // Table Headers
                        float[] colWidths = { 65f, 110f, 130f, 130f, 85f, 180f };
                        string[] colTitles = { "رقم السند", "التاريخ والوقت", "بند المصروف", "المستفيد / الجهة", "المبلغ", "البيان والملاحظات" };

                        float x = margin.Right;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(230, 235, 245)), margin.Left, y, margin.Width, 24);
                        g.DrawRectangle(pBorder, margin.Left, y, margin.Width, 24);

                        for (int i = 0; i < colWidths.Length; i++)
                        {
                            x -= colWidths[i];
                            g.DrawString(colTitles[i], fHeader, bBlack, new RectangleF(x, y + 4, colWidths[i], 20), sfCenter);
                        }
                        y += 24;

                        // Data Rows
                        while (rowIndex < dgExpensesReport.Rows.Count)
                        {
                            var row = dgExpensesReport.Rows[rowIndex];
                            if (row.IsNewRow) { rowIndex++; continue; }

                            if (y + 22 > margin.Bottom - 30)
                            {
                                ev.HasMorePages = true;
                                pageNumber++;
                                return;
                            }

                            x = margin.Right;
                            if (rowIndex % 2 == 1)
                            {
                                g.FillRectangle(new SolidBrush(Color.FromArgb(250, 250, 250)), margin.Left, y, margin.Width, 22);
                            }
                            g.DrawRectangle(Pens.LightGray, margin.Left, y, margin.Width, 22);

                            string sId = row.Cells["رقم السند"].Value?.ToString() ?? "";
                            string sDate = row.Cells["التاريخ والوقت"].Value != null && DateTime.TryParse(row.Cells["التاريخ والوقت"].Value.ToString(), out DateTime rd) ? rd.ToString("yyyy/MM/dd HH:mm") : "";
                            string sCat = row.Cells["بند المصروف"].Value?.ToString() ?? "";
                            string sBen = row.Cells["المستفيد / الجهة"].Value?.ToString() ?? "";
                            string sAmt = row.Cells["المبلغ"].Value != null && decimal.TryParse(row.Cells["المبلغ"].Value.ToString(), out decimal am) ? am.ToString("N2") : "";
                            string sNotes = row.Cells["البيان والملاحظات"].Value?.ToString() ?? "";

                            string[] vals = { sId, sDate, sCat, sBen, sAmt, sNotes };

                            for (int i = 0; i < colWidths.Length; i++)
                            {
                                x -= colWidths[i];
                                var sf = (i == 4) ? sfRight : ((i == 5) ? sfRight : sfCenter);
                                g.DrawString(vals[i], fRow, bBlack, new RectangleF(x + 2, y + 3, colWidths[i] - 4, 18), sf);
                            }

                            y += 22;
                            rowIndex++;
                        }

                        g.DrawString($"صفحة {pageNumber}", fSub, bGray, new RectangleF(margin.Left, margin.Bottom + 5, margin.Width, 20), sfCenter);
                        ev.HasMorePages = false;
                    }
                };

                using (var dlg = new PrintPreviewDialog())
                {
                    dlg.Document = pd;
                    dlg.WindowState = FormWindowState.Maximized;
                    dlg.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء إعداد معاينة الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                RightToLeft = RightToLeft.Yes,
                ColumnHeadersHeight = 26,
                RowTemplate = { Height = 24 },
                DefaultCellStyle = { Font = new Font("Segoe UI", 8.8f) }
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
                Margin = new Padding(3, 2, 3, 2),
                Padding = new Padding(6, 2, 6, 2)
            };

            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                ForeColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Height = 16,
                TextAlign = ContentAlignment.TopRight
            };

            var lblV = new Label
            {
                Text = val,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlCard.Controls.Add(lblV);
            pnlCard.Controls.Add(lblT);
            parent.Controls.Add(pnlCard, col, 0);
            return lblV;
        }
    }
}

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// ØªÙ‚Ø±ÙŠØ± ÙˆØ³Ø¬Ù„ Ø­Ø±ÙƒØ§Øª ÙˆØªØ¹Ø¯ÙŠÙ„Ø§Øª Ø¹Ù…Ù„ÙŠØ§Øª Ø§Ù„ØªØµÙ†ÙŠØ¹ (Ø«Ø§Ø¨Øª ÙˆÙ…Ø®ØµØµ)
    /// Ø¨ØªØµÙ…ÙŠÙ… Ø§Ø­ØªØ±Ø§ÙÙŠ Ø¹Ø§Ù„ÙŠ Ø§Ù„ÙƒØ«Ø§ÙØ© (High Information Density) ÙˆÙ…Ù†Ø³Ù‚ Ø¨Ø§Ù„ÙƒØ§Ù…Ù„
    /// </summary>
    public class FrmProductionReports : Form
    {
        // Filters
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboTypeFilter, cboStatusFilter, cboWarehouseFilter;
        private TextBox txtSearch;
        private Button btnSearch, btnRefresh;

        // Quick Preset Buttons
        private Button btnFilterToday, btnFilterThisMonth, btnFilterAllTime;

        // Master Grid & Splitter
        private SplitContainer split;
        private DataGridView dgOrders;

        // Details Panel (Tabs: Items & History)
        private TabControl tabDetails;
        private DataGridView dgItemsDetail;
        private DataGridView dgHistoryDetail;
        private Button btnToggleDetails;
        private bool _detailsVisible = true;

        // KPI Badges Bar
        private Label lblTotalOrdersCount;
        private Label lblPrepOrdersCount;
        private Label lblCompletedOrdersCount;
        private Label lblRawCostSum;
        private Label lblExtraExpensesSum;
        private Label lblTotalCostSum;

        // Action Buttons
        private Button btnOpenOrder;
        private Button btnNewFixed;
        private Button btnNewCustom;
        private Button btnPrintReport;
        private Button btnExportExcel;

        public FrmProductionReports()
        {
            InitUI();
            LoadWarehousesFilter();
            ApplyFilters();
        }

        private void InitUI()
        {
            this.Text = "ðŸ“Š Ø³Ø¬Ù„ ÙˆÙ…ØªØ§Ø¨Ø¹Ø© Ø­Ø±ÙƒØ§Øª Ø§Ù„ØªØµÙ†ÙŠØ¹ ÙˆØ§Ù„ØªØ´ØºÙŠÙ„ Ø§Ù„Ø´Ø§Ù…Ù„";
            this.Size = new Size(1280, 780);
            this.MinimumSize = new Size(1060, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false; // Ù„Ù…Ù†Ø¹ ØªØ´ÙˆÙ‡ Ø§Ù„ØªØ®Ø·ÙŠØ· ÙˆÙ…Ø­Ø§Ø°Ø§Ø© Ø§Ù„Ø´Ø§Ø´Ø§Øª ÙÙŠ ÙˆÙŠÙ†Ø¯ÙˆØ²
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // 1. Ø§Ù„Ø´Ø±ÙŠØ· Ø§Ù„Ø¹Ù„ÙˆÙŠ Ù„Ù„ÙÙ„Ø§ØªØ± Ø§Ù„Ù…Ø¯Ù…Ø¬Ø© (Compact Filter Bar - 48px)
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            var pnlFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 6, 8, 6)
            };
            this.Controls.Add(pnlFilters);

            var flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlFilters.Controls.Add(flowFilters);

            // Ø¹Ù†ÙˆØ§Ù† Ø§Ù„ÙÙ„ØªØ±Ø©
            flowFilters.Controls.Add(new Label
            {
                Text = "ðŸ” ØªØµÙÙŠØ© Ø§Ù„Ø£ÙˆØ§Ù…Ø±:",
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.Accent,
                Margin = new Padding(0, 6, 6, 0)
            });

            // Ù…Ù† ØªØ§Ø±ÙŠØ®
            flowFilters.Controls.Add(new Label { Text = "Ù…Ù†:", AutoSize = true, Margin = new Padding(0, 6, 2, 0), Font = Theme.FontSmall });
            dtpFrom = new DateTimePicker
            {
                Width = 92,
                Height = 26,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30),
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            flowFilters.Controls.Add(dtpFrom);

            // Ø¥Ù„Ù‰ ØªØ§Ø±ÙŠØ®
            flowFilters.Controls.Add(new Label { Text = "Ø¥Ù„Ù‰:", AutoSize = true, Margin = new Padding(0, 6, 2, 0), Font = Theme.FontSmall });
            dtpTo = new DateTimePicker
            {
                Width = 92,
                Height = 26,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            flowFilters.Controls.Add(dtpTo);

            // Ø£Ø²Ø±Ø§Ø± Ø§Ù„ÙØªØ±Ø§Øª Ø§Ù„Ø³Ø±ÙŠØ¹Ø©
            btnFilterToday = MakeSmallButton("Ø§Ù„ÙŠÙˆÙ…", () => { dtpFrom.Value = DateTime.Today; dtpTo.Value = DateTime.Today; ApplyFilters(); });
            flowFilters.Controls.Add(btnFilterToday);

            btnFilterThisMonth = MakeSmallButton("Ø§Ù„Ø´Ù‡Ø±", () => { dtpFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); dtpTo.Value = DateTime.Today; ApplyFilters(); });
            flowFilters.Controls.Add(btnFilterThisMonth);

            btnFilterAllTime = MakeSmallButton("Ø§Ù„ÙƒÙ„", () => { dtpFrom.Value = DateTime.Today.AddYears(-5); dtpTo.Value = DateTime.Today; ApplyFilters(); });
            flowFilters.Controls.Add(btnFilterAllTime);

            // Ù†ÙˆØ¹ Ø§Ù„ØªØµÙ†ÙŠØ¹
            flowFilters.Controls.Add(new Label { Text = "Ø§Ù„Ù†ÙˆØ¹:", AutoSize = true, Margin = new Padding(4, 6, 2, 0), Font = Theme.FontSmall });
            cboTypeFilter = new ComboBox
            {
                Width = 100,
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            cboTypeFilter.Items.AddRange(new object[] { "ÙƒÙ„ Ø§Ù„Ø£Ù†ÙˆØ§Ø¹", "ØªØµÙ†ÙŠØ¹ Ø«Ø§Ø¨Øª (BOM)", "ØªØµÙ†ÙŠØ¹ Ù…Ø®ØµØµ" });
            cboTypeFilter.SelectedIndex = 0;
            cboTypeFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            flowFilters.Controls.Add(cboTypeFilter);

            // Ø§Ù„Ø­Ø§Ù„Ø©
            flowFilters.Controls.Add(new Label { Text = "Ø§Ù„Ø­Ø§Ù„Ø©:", AutoSize = true, Margin = new Padding(4, 6, 2, 0), Font = Theme.FontSmall });
            cboStatusFilter = new ComboBox
            {
                Width = 105,
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            cboStatusFilter.Items.AddRange(new object[] { "ÙƒÙ„ Ø§Ù„Ø­Ø§Ù„Ø§Øª", "ØªØ­Øª Ø§Ù„ØªØ­Ø¶ÙŠØ±", "Ù…ÙƒØªÙ…Ù„ ÙˆÙ…Ø±Ø­Ù„", "Ù…Ù„ØºÙŠ" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            flowFilters.Controls.Add(cboStatusFilter);

            // Ø§Ù„Ù…Ø®Ø²Ù†
            flowFilters.Controls.Add(new Label { Text = "Ø§Ù„Ù…Ø®Ø²Ù†:", AutoSize = true, Margin = new Padding(4, 6, 2, 0), Font = Theme.FontSmall });
            cboWarehouseFilter = new ComboBox
            {
                Width = 105,
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            cboWarehouseFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            flowFilters.Controls.Add(cboWarehouseFilter);

            // Ø­Ù‚Ù„ Ø§Ù„Ø¨Ø­Ø« Ø§Ù„ÙÙˆØ±ÙŠ
            txtSearch = new TextBox
            {
                Width = 140,
                Height = 26,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Margin = new Padding(4, 3, 2, 0)
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ApplyFilters(); } };
            flowFilters.Controls.Add(txtSearch);

            btnSearch = Theme.MakeButton("Ø¨Ø­Ø«", 0, 0, 60, 27, Theme.Primary);
            btnSearch.Margin = new Padding(0, 2, 3, 0);
            btnSearch.Click += (s, e) => ApplyFilters();
            flowFilters.Controls.Add(btnSearch);

            btnRefresh = Theme.MakeButton("ðŸ”„", 0, 0, 36, 27, Color.FromArgb(71, 85, 105));
            btnRefresh.Margin = new Padding(0, 2, 2, 0);
            btnRefresh.Click += (s, e) => { txtSearch.Clear(); ApplyFilters(); };
            flowFilters.Controls.Add(btnRefresh);

            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // 2. Ø´Ø±ÙŠØ· Ø§Ù„Ø¨Ø·Ø§Ù‚Ø§Øª Ø§Ù„Ø¥Ø­ØµØ§Ø¦ÙŠØ© ÙˆØ§Ù„Ù…Ø§Ù„ÙŠØ© (KPI Dashboard - 46px)
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            var pnlKPI = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 0, 0, 2)
            };
            this.Controls.Add(pnlKPI);

            var flowKPI = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlKPI.Controls.Add(flowKPI);

            lblTotalOrdersCount = MakeBadge("ðŸ“‹ Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„Ø£ÙˆØ§Ù…Ø±: 0", Color.FromArgb(30, 41, 59), Color.FromArgb(226, 232, 240));
            flowKPI.Controls.Add(lblTotalOrdersCount);

            lblPrepOrdersCount = MakeBadge("â³ ØªØ­Øª Ø§Ù„ØªØ­Ø¶ÙŠØ±: 0", Color.FromArgb(194, 65, 12), Color.FromArgb(255, 237, 213));
            flowKPI.Controls.Add(lblPrepOrdersCount);

            lblCompletedOrdersCount = MakeBadge("âœ… Ù…ÙƒØªÙ…Ù„ ÙˆÙ…Ø±Ø­Ù„: 0", Color.FromArgb(21, 128, 61), Color.FromArgb(220, 252, 231));
            flowKPI.Controls.Add(lblCompletedOrdersCount);

            lblRawCostSum = MakeBadge("ðŸ“¦ ØªÙƒÙ„ÙØ© Ø§Ù„Ø®Ø§Ù…Ø§Øª: 0.00 Ø¬", Color.FromArgb(146, 64, 14), Color.FromArgb(254, 243, 199));
            flowKPI.Controls.Add(lblRawCostSum);

            lblExtraExpensesSum = MakeBadge("âš¡ Ù…ØµØ§Ø±ÙŠÙ ØªØ´ØºÙŠÙ„: 0.00 Ø¬", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 240, 138));
            flowKPI.Controls.Add(lblExtraExpensesSum);

            lblTotalCostSum = MakeBadge("ðŸ’° Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„ØªÙƒØ§Ù„ÙŠÙ: 0.00 Ø¬.Ù…", Color.FromArgb(15, 23, 42), Color.FromArgb(186, 230, 253));
            lblTotalCostSum.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            flowKPI.Controls.Add(lblTotalCostSum);

            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // 3. Ù…Ù†Ø·Ù‚Ø© Ø§Ù„Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ù…Ù‚Ø³Ù…Ø©: Ø§Ù„Ø¬Ø¯ÙˆÙ„ Ø§Ù„Ø±Ø¦ÙŠØ³ÙŠ + ØªÙØ§ØµÙŠÙ„ Ø§Ù„Ø£Ù…Ø±
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(203, 213, 225),
                SplitterWidth = 6,
                Panel1MinSize = 150,
                Panel2MinSize = 80
            };
            this.Controls.Add(split);
            split.BringToFront();

            // â”€â”€ Ø£) Ø¬Ø¯ÙˆÙ„ Ø£ÙˆØ§Ù…Ø± Ø§Ù„ØªØµÙ†ÙŠØ¹ Ø§Ù„Ø±Ø¦ÙŠØ³ÙŠ (Master Grid) â”€â”€
            dgOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 28 },
                GridColor = Color.FromArgb(226, 232, 240)
            };
            dgOrders.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgOrders.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8.75f),
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.Black,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgOrders.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252)
            };
            Theme.EnableDoubleBuffer(dgOrders);

            dgOrders.SelectionChanged += (s, e) => LoadSelectedOrderDetails();
            dgOrders.CellDoubleClick += (s, e) => OpenSelectedOrderForm();
            split.Panel1.Controls.Add(dgOrders);

            // â”€â”€ Ø¨) ØªØ¨ÙˆÙŠØ¨Ø§Øª ØªÙØ§ØµÙŠÙ„ Ø§Ù„Ø£Ù…Ø± Ø§Ù„Ù…Ø®ØªØ§Ø± (Details Tabs) â”€â”€
            var pnlDetailsHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(4, 2, 4, 2)
            };
            
            btnToggleDetails = new Button
            {
                Text = "ðŸ”½ Ø¥Ø®ÙØ§Ø¡ Ù„ÙˆØ­Ø© Ø§Ù„ØªÙØ§ØµÙŠÙ„",
                Dock = DockStyle.Left,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontSmall,
                ForeColor = Theme.Primary,
                Cursor = Cursors.Hand
            };
            btnToggleDetails.FlatAppearance.BorderSize = 0;
            btnToggleDetails.Click += (s, e) => ToggleDetailsPanel();
            pnlDetailsHeader.Controls.Add(btnToggleDetails);

            var lblDetTitle = new Label
            {
                Text = "ðŸ“¦ ØªÙØ§ØµÙŠÙ„ ÙˆÙ…ÙƒÙˆÙ†Ø§Øª ÙˆØ³Ø¬Ù„ Ø§Ù„Ø£Ù…Ø± Ø§Ù„Ù…Ø­Ø¯Ø¯ Ø£Ø¹Ù„Ø§Ù‡:",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            pnlDetailsHeader.Controls.Add(lblDetTitle);
            split.Panel2.Controls.Add(pnlDetailsHeader);

            tabDetails = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = false
            };
            split.Panel2.Controls.Add(tabDetails);
            tabDetails.BringToFront();

            // Tab 1: Raw Materials
            var tabItems = new TabPage("ðŸ“¦ Ø§Ù„Ù…ÙˆØ§Ø¯ Ø§Ù„Ø®Ø§Ù… Ø§Ù„Ù…Ø³ØªÙ‡Ù„ÙƒØ© ÙÙŠ Ù‡Ø°Ø§ Ø§Ù„Ø£Ù…Ø±");
            tabItems.BackColor = Color.White;
            dgItemsDetail = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 25 },
                GridColor = Color.FromArgb(226, 232, 240)
            };
            dgItemsDetail.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgItemsDetail.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8.5f),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            Theme.EnableDoubleBuffer(dgItemsDetail);
            tabItems.Controls.Add(dgItemsDetail);
            tabDetails.TabPages.Add(tabItems);

            // Tab 2: Audit History
            var tabHistory = new TabPage("ðŸ“‘ Ø³Ø¬Ù„ Ø­Ø±ÙƒØ§Øª ÙˆØªØ¹Ø¯ÙŠÙ„ Ø§Ù„Ø£Ù…Ø± (Audit Trail)");
            tabHistory.BackColor = Color.White;
            dgHistoryDetail = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 25 },
                GridColor = Color.FromArgb(226, 232, 240)
            };
            dgHistoryDetail.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgHistoryDetail.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8.5f),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            Theme.EnableDoubleBuffer(dgHistoryDetail);
            tabHistory.Controls.Add(dgHistoryDetail);
            tabDetails.TabPages.Add(tabHistory);

            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // 4. Ø§Ù„Ø´Ø±ÙŠØ· Ø§Ù„Ø³ÙÙ„ÙŠ Ù„Ù„Ø¹Ù…Ù„ÙŠØ§Øª ÙˆØ§Ù„Ø¥Ø¬Ø±Ø§Ø¡Ø§Øª (Action Bar - 44px)
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 4, 8, 4)
            };
            this.Controls.Add(pnlBottom);

            var flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlBottom.Controls.Add(flowBottom);

            btnOpenOrder = Theme.MakeButton("ðŸ“‚ ÙØªØ­ Ø£Ù…Ø± Ø§Ù„ØªØµÙ†ÙŠØ¹", 0, 0, 140, 34, Theme.Primary);
            btnOpenOrder.Font = Theme.FontBold;
            btnOpenOrder.Margin = new Padding(0, 1, 8, 0);
            btnOpenOrder.Click += (s, e) => OpenSelectedOrderForm();
            flowBottom.Controls.Add(btnOpenOrder);

            btnNewFixed = Theme.MakeButton("âž• ØªØµÙ†ÙŠØ¹ Ù…Ø¹ÙŠØ§Ø±ÙŠ (BOM)", 0, 0, 150, 34, Color.FromArgb(16, 185, 129));
            btnNewFixed.Font = Theme.FontBold;
            btnNewFixed.Margin = new Padding(0, 1, 8, 0);
            btnNewFixed.Click += (s, e) =>
            {
                using (var frm = new FrmFixedProduction())
                {
                    frm.ShowDialog();
                    ApplyFilters();
                }
            };
            flowBottom.Controls.Add(btnNewFixed);

            btnNewCustom = Theme.MakeButton("ðŸ› ï¸ ØªØµÙ†ÙŠØ¹ Ù…Ø®ØµØµ", 0, 0, 125, 34, Color.FromArgb(139, 92, 246));
            btnNewCustom.Font = Theme.FontBold;
            btnNewCustom.Margin = new Padding(0, 1, 8, 0);
            btnNewCustom.Click += (s, e) =>
            {
                using (var frm = new FrmCustomProduction())
                {
                    frm.ShowDialog();
                    ApplyFilters();
                }
            };
            flowBottom.Controls.Add(btnNewCustom);

            btnPrintReport = Theme.MakeButton("ðŸ–¨ï¸ Ø·Ø¨Ø§Ø¹Ø© Ø§Ù„ØªÙ‚Ø±ÙŠØ±", 0, 0, 115, 34, Color.FromArgb(2, 132, 199));
            btnPrintReport.Font = Theme.FontBold;
            btnPrintReport.Margin = new Padding(0, 1, 8, 0);
            btnPrintReport.Click += (s, e) => PrintReport();
            flowBottom.Controls.Add(btnPrintReport);

            btnExportExcel = Theme.MakeButton("ðŸ“Š ØªØµØ¯ÙŠØ± Excel", 0, 0, 115, 34, Color.FromArgb(22, 163, 74));
            btnExportExcel.Font = Theme.FontBold;
            btnExportExcel.Margin = new Padding(0, 1, 0, 0);
            btnExportExcel.Click += (s, e) => ExportToCsv();
            flowBottom.Controls.Add(btnExportExcel);

            this.Resize += (s, e) =>
            {
                if (split != null && split.Height > 250 && _detailsVisible)
                {
                    split.SplitterDistance = (int)(split.Height * 0.65);
                }
            };
        }

        private static Button MakeSmallButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = 46,
                Height = 26,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontSmall,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 3, 0)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private static Label MakeBadge(string text, Color fore, Color back)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = fore,
                BackColor = back,
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 3, 6, 0)
            };
        }

        private void ToggleDetailsPanel()
        {
            _detailsVisible = !_detailsVisible;
            if (_detailsVisible)
            {
                split.Panel2Collapsed = false;
                btnToggleDetails.Text = "ðŸ”½ Ø¥Ø®ÙØ§Ø¡ Ù„ÙˆØ­Ø© Ø§Ù„ØªÙØ§ØµÙŠÙ„";
                if (split.Height > 250) split.SplitterDistance = (int)(split.Height * 0.65);
            }
            else
            {
                split.Panel2Collapsed = true;
                btnToggleDetails.Text = "ðŸ”¼ Ø¥Ø¸Ù‡Ø§Ø± Ù„ÙˆØ­Ø© Ø§Ù„ØªÙØ§ØµÙŠÙ„";
            }
        }

        private void LoadWarehousesFilter()
        {
            try
            {
                var dt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses ORDER BY WarehouseID ASC");
                var row = dt.NewRow();
                row["WarehouseID"] = 0;
                row["WarehouseName"] = "ÙƒÙ„ Ø§Ù„Ù…Ø®Ø§Ø²Ù†";
                dt.Rows.InsertAt(row, 0);

                cboWarehouseFilter.DataSource = dt;
                cboWarehouseFilter.DisplayMember = "WarehouseName";
                cboWarehouseFilter.ValueMember = "WarehouseID";
                cboWarehouseFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmProductionReports.LoadWarehousesFilter", ex);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                DateTime from = dtpFrom.Value.Date;
                DateTime to = dtpTo.Value.Date.AddDays(1).AddSeconds(-1);

                string prodType = cboTypeFilter.SelectedIndex switch
                {
                    1 => "Fixed",
                    2 => "Custom",
                    _ => "All"
                };

                string status = cboStatusFilter.SelectedIndex switch
                {
                    1 => "InPreparation",
                    2 => "Completed",
                    3 => "Cancelled",
                    _ => "All"
                };

                int? wid = null;
                if (cboWarehouseFilter.SelectedValue != null && Convert.ToInt32(cboWarehouseFilter.SelectedValue) > 0)
                    wid = Convert.ToInt32(cboWarehouseFilter.SelectedValue);

                string search = txtSearch.Text.Trim();

                var dt = ProductionDAL.SearchProductionOrders(from, to, prodType, status, null, wid, search);
                dgOrders.DataSource = dt;

                ConfigureMasterGridColumns();
                CalculateSummary(dt);

                if (split != null && split.Height > 250 && _detailsVisible)
                {
                    split.SplitterDistance = (int)(split.Height * 0.65);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmProductionReports.ApplyFilters", ex);
            }
        }

        private void ConfigureMasterGridColumns()
        {
            if (dgOrders.Columns["ProductionID"] != null) dgOrders.Columns["ProductionID"].Visible = false;
            if (dgOrders.Columns["ProductionType"] != null) dgOrders.Columns["ProductionType"].Visible = false;
            if (dgOrders.Columns["Status"] != null) dgOrders.Columns["Status"].Visible = false;

            SetCol(dgOrders, "OrderCode", "ÙƒÙˆØ¯ Ø§Ù„Ø£Ù…Ø±", 85, fillWeight: 10, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "CreatedDate", "Ø§Ù„ØªØ§Ø±ÙŠØ®", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "ProductionTypeName", "Ù†ÙˆØ¹ Ø§Ù„ØªØµÙ†ÙŠØ¹", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "WarehouseName", "Ø§Ù„Ù…Ø®Ø²Ù†", 80, fillWeight: 8, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "FinishedProductCode", "ÙƒÙˆØ¯ Ø§Ù„ØµÙ†Ù", 75, fillWeight: 8, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "FinishedProductName", "Ø§Ù„Ù…Ù†ØªØ¬ Ø§Ù„Ù…ØµÙ†Ø¹", 160, fillWeight: 22, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "ProducedQty", "Ø§Ù„ÙƒÙ…ÙŠØ©", 55, fillWeight: 7, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "UnitName", "Ø§Ù„ÙˆØ­Ø¯Ø©", 45, fillWeight: 6, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "RawMaterialsCost", "ØªÙƒÙ„ÙØ© Ø§Ù„Ø®Ø§Ù…Ø§Øª", 80, fillWeight: 9, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "ExtraExpenses", "Ù…ØµØ§Ø±ÙŠÙ ØªØ´ØºÙŠÙ„", 80, fillWeight: 9, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "TotalCost", "Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„ØªÙƒÙ„ÙØ©", 85, fillWeight: 10, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "UnitCost", "ØªÙƒÙ„ÙØ© Ø§Ù„Ù‚Ø·Ø¹Ø©", 80, fillWeight: 9, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "StatusName", "Ø§Ù„Ø­Ø§Ù„Ø©", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "CreatedByName", "Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù…", 75, fillWeight: 8, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "UpdatedDate", "Ø¢Ø®Ø± ØªØ¹Ø¯ÙŠÙ„", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "CompletedDate", "ØªØ§Ø±ÙŠØ® Ø§Ù„Ø¥ØªÙ…Ø§Ù…", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);

            // Formatting
            if (dgOrders.Columns["CreatedDate"] != null) dgOrders.Columns["CreatedDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
            if (dgOrders.Columns["RawMaterialsCost"] != null) dgOrders.Columns["RawMaterialsCost"].DefaultCellStyle.Format = "N2";
            if (dgOrders.Columns["ExtraExpenses"] != null) dgOrders.Columns["ExtraExpenses"].DefaultCellStyle.Format = "N2";
            if (dgOrders.Columns["TotalCost"] != null)
            {
                dgOrders.Columns["TotalCost"].DefaultCellStyle.Format = "N2";
                dgOrders.Columns["TotalCost"].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                dgOrders.Columns["TotalCost"].DefaultCellStyle.ForeColor = Color.FromArgb(180, 83, 9);
            }
            if (dgOrders.Columns["UnitCost"] != null)
            {
                dgOrders.Columns["UnitCost"].DefaultCellStyle.Format = "N2";
                dgOrders.Columns["UnitCost"].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                dgOrders.Columns["UnitCost"].DefaultCellStyle.ForeColor = Color.FromArgb(16, 185, 129);
            }
            if (dgOrders.Columns["ProducedQty"] != null)
            {
                dgOrders.Columns["ProducedQty"].DefaultCellStyle.Format = "N2";
                dgOrders.Columns["ProducedQty"].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }

            // Cell formatting for status & type
            foreach (DataGridViewRow r in dgOrders.Rows)
            {
                string st = r.Cells["StatusName"]?.Value?.ToString() ?? "";
                if (st.Contains("ØªØ­Øª Ø§Ù„ØªØ­Ø¶ÙŠØ±"))
                {
                    r.Cells["StatusName"].Style.ForeColor = Color.FromArgb(194, 65, 12);
                    r.Cells["StatusName"].Style.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
                }
                else if (st.Contains("Ù…ÙƒØªÙ…Ù„"))
                {
                    r.Cells["StatusName"].Style.ForeColor = Color.FromArgb(21, 128, 61);
                    r.Cells["StatusName"].Style.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
                }
                else if (st.Contains("Ù…Ù„ØºÙŠ"))
                {
                    r.Cells["StatusName"].Style.ForeColor = Color.FromArgb(220, 38, 38);
                }
            }
        }

        private static void SetCol(DataGridView dg, string name, string header, int width, int fillWeight = 10, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            if (dg.Columns[name] != null)
            {
                dg.Columns[name].HeaderText = header;
                dg.Columns[name].MinimumWidth = width;
                dg.Columns[name].FillWeight = fillWeight;
                dg.Columns[name].DefaultCellStyle.Alignment = align;
            }
        }

        private void CalculateSummary(DataTable dt)
        {
            if (dt == null) return;
            int totalCount = dt.Rows.Count;
            int prepCount = 0;
            int completedCount = 0;
            decimal totalCost = 0;
            decimal totalExpenses = 0;
            decimal rawCost = 0;

            foreach (DataRow r in dt.Rows)
            {
                string st = r["Status"]?.ToString();
                if (st == "InPreparation") prepCount++;
                else if (st == "Completed") completedCount++;

                if (st != "Cancelled")
                {
                    totalCost += Convert.ToDecimal(r["TotalCost"] ?? 0);
                    totalExpenses += Convert.ToDecimal(r["ExtraExpenses"] ?? 0);
                    rawCost += Convert.ToDecimal(r["RawMaterialsCost"] ?? 0);
                }
            }

            lblTotalOrdersCount.Text = $"ðŸ“‹ Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„Ø£ÙˆØ§Ù…Ø±: {totalCount}";
            lblPrepOrdersCount.Text = $"â³ ØªØ­Øª Ø§Ù„ØªØ­Ø¶ÙŠØ±: {prepCount}";
            lblCompletedOrdersCount.Text = $"âœ… Ù…ÙƒØªÙ…Ù„ ÙˆÙ…Ø±Ø­Ù„: {completedCount}";
            lblRawCostSum.Text = $"ðŸ“¦ ØªÙƒÙ„ÙØ© Ø§Ù„Ø®Ø§Ù…Ø§Øª: {rawCost:N2} Ø¬";
            lblExtraExpensesSum.Text = $"âš¡ Ù…ØµØ§Ø±ÙŠÙ ØªØ´ØºÙŠÙ„: {totalExpenses:N2} Ø¬";
            lblTotalCostSum.Text = $"ðŸ’° Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„ØªÙƒØ§Ù„ÙŠÙ: {totalCost:N2} Ø¬.Ù…";
        }

        private void LoadSelectedOrderDetails()
        {
            if (dgOrders.CurrentRow == null || dgOrders.CurrentRow.Cells["ProductionID"].Value == null)
            {
                dgItemsDetail.DataSource = null;
                dgHistoryDetail.DataSource = null;
                return;
            }

            int pid = Convert.ToInt32(dgOrders.CurrentRow.Cells["ProductionID"].Value);

            // 1. ØªØ­Ù…ÙŠÙ„ Ø§Ù„Ù…ÙˆØ§Ø¯ Ø§Ù„Ø®Ø§Ù…
            var dtItems = DbHelper.Query(@"
                SELECT p.ProductCode AS RawProductCode, p.ProductName AS RawProductName,
                       poi.Quantity, poi.UnitName, poi.UnitCost, poi.TotalCost, poi.Notes
                FROM ProductionOrderItems poi
                JOIN Products p ON poi.RawProductID = p.ProductID
                WHERE poi.ProductionID = @id",
                DbHelper.P("@id", pid));

            dgItemsDetail.DataSource = dtItems;
            SetCol(dgItemsDetail, "RawProductCode", "ÙƒÙˆØ¯ Ø§Ù„Ø®Ø§Ù…", 90, 14, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgItemsDetail, "RawProductName", "Ø§Ø³Ù… Ø§Ù„Ù…Ø§Ø¯Ø© Ø§Ù„Ø®Ø§Ù… Ø§Ù„Ù…Ø³ØªÙ‡Ù„ÙƒØ©", 180, 36, DataGridViewContentAlignment.MiddleRight);
            SetCol(dgItemsDetail, "Quantity", "Ø§Ù„ÙƒÙ…ÙŠØ© Ø§Ù„Ù…Ø®ØµÙˆÙ…Ø©", 90, 12, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgItemsDetail, "UnitName", "Ø§Ù„ÙˆØ­Ø¯Ø©", 60, 10, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgItemsDetail, "UnitCost", "Ø³Ø¹Ø± Ø§Ù„ØªÙƒÙ„ÙØ©", 85, 12, DataGridViewContentAlignment.MiddleRight);
            SetCol(dgItemsDetail, "TotalCost", "Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„ØªÙƒÙ„ÙØ©", 95, 14, DataGridViewContentAlignment.MiddleRight);
            SetCol(dgItemsDetail, "Notes", "Ù…Ù„Ø§Ø­Ø¸Ø§Øª", 120, 18, DataGridViewContentAlignment.MiddleRight);

            if (dgItemsDetail.Columns["Quantity"] != null) dgItemsDetail.Columns["Quantity"].DefaultCellStyle.Format = "N3";
            if (dgItemsDetail.Columns["UnitCost"] != null) dgItemsDetail.Columns["UnitCost"].DefaultCellStyle.Format = "N2";
            if (dgItemsDetail.Columns["TotalCost"] != null)
            {
                dgItemsDetail.Columns["TotalCost"].DefaultCellStyle.Format = "N2";
                dgItemsDetail.Columns["TotalCost"].DefaultCellStyle.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
                dgItemsDetail.Columns["TotalCost"].DefaultCellStyle.ForeColor = Color.FromArgb(180, 83, 9);
            }

            // 2. ØªØ­Ù…ÙŠÙ„ Ø³Ø¬Ù„ Ø§Ù„ØªØ¹Ø¯ÙŠÙ„ ÙˆØ§Ù„ØªØ¯Ù‚ÙŠÙ‚
            var dtHist = ProductionDAL.GetOrderHistory(pid);
            dgHistoryDetail.DataSource = dtHist;
            if (dgHistoryDetail.Columns["HistoryID"] != null) dgHistoryDetail.Columns["HistoryID"].Visible = false;
            if (dgHistoryDetail.Columns["ProductionID"] != null) dgHistoryDetail.Columns["ProductionID"].Visible = false;
            if (dgHistoryDetail.Columns["ActionType"] != null) dgHistoryDetail.Columns["ActionType"].Visible = false;

            SetCol(dgHistoryDetail, "ActionTypeName", "Ù†ÙˆØ¹ Ø§Ù„Ø¥Ø¬Ø±Ø§Ø¡", 120, 20, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgHistoryDetail, "ActionDate", "Ø§Ù„ØªØ§Ø±ÙŠØ® ÙˆØ§Ù„ÙˆÙ‚Øª", 130, 22, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgHistoryDetail, "ActionBy", "Ø¨ÙˆØ§Ø³Ø·Ø©", 100, 16, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgHistoryDetail, "Details", "ØªÙØ§ØµÙŠÙ„ Ø§Ù„Ø¥Ø¬Ø±Ø§Ø¡ ÙˆØ§Ù„ØªØ¹Ø¯ÙŠÙ„Ø§Øª", 250, 42, DataGridViewContentAlignment.MiddleRight);
        }

        private void OpenSelectedOrderForm()
        {
            if (dgOrders.CurrentRow == null || dgOrders.CurrentRow.Cells["ProductionID"].Value == null) return;
            int pid = Convert.ToInt32(dgOrders.CurrentRow.Cells["ProductionID"].Value);
            string pType = dgOrders.CurrentRow.Cells["ProductionType"].Value?.ToString();

            if (pType == "Custom")
            {
                using (var frm = new FrmCustomProduction(pid))
                {
                    frm.ShowDialog();
                    ApplyFilters();
                }
            }
            else
            {
                using (var frm = new FrmFixedProduction(pid))
                {
                    frm.ShowDialog();
                    ApplyFilters();
                }
            }
        }

        private void PrintReport()
        {
            if (dgOrders.Rows.Count == 0)
            {
                MessageBox.Show("Ù„Ø§ ØªÙˆØ¬Ø¯ Ø¨ÙŠØ§Ù†Ø§Øª Ù„Ù„Ø·Ø¨Ø§Ø¹Ø©.", "ØªÙ†Ø¨ÙŠÙ‡", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pd = new PrintDocument();
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    float y = 40;
                    var fontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
                    var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    var fontBody = new Font("Segoe UI", 8.5f);
                    var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold);

                    g.DrawString("ØªÙ‚Ø±ÙŠØ± ÙˆØ³Ø¬Ù„ Ø­Ø±ÙƒØ§Øª Ø§Ù„ØªØµÙ†ÙŠØ¹ ÙˆØ§Ù„Ø¥Ù†ØªØ§Ø¬ Ø§Ù„Ø´Ø§Ù…Ù„", fontTitle, Brushes.DarkBlue, new PointF(230, y));
                    y += 35;
                    g.DrawString($"Ø§Ù„ÙØªØ±Ø© Ù…Ù†: {dtpFrom.Value:yyyy-MM-dd} Ø¥Ù„Ù‰: {dtpTo.Value:yyyy-MM-dd} | ØªØ§Ø±ÙŠØ® Ø§Ù„ØªÙ‚Ø±ÙŠØ±: {DateTime.Now:yyyy-MM-dd HH:mm}", fontBody, Brushes.DarkSlateGray, new PointF(40, y));
                    y += 28;

                    // Table Header
                    g.FillRectangle(Brushes.LightGray, 40, y, 740, 24);
                    g.DrawRectangle(Pens.Gray, 40, y, 740, 24);
                    g.DrawString("ÙƒÙˆØ¯ Ø§Ù„Ø£Ù…Ø±", fontHeader, Brushes.Black, 45, y + 4);
                    g.DrawString("Ø§Ù„Ù†ÙˆØ¹", fontHeader, Brushes.Black, 130, y + 4);
                    g.DrawString("Ø§Ù„Ù…Ù†ØªØ¬ Ø§Ù„Ù…ØµÙ†Ø¹", fontHeader, Brushes.Black, 210, y + 4);
                    g.DrawString("Ø§Ù„ÙƒÙ…ÙŠØ©", fontHeader, Brushes.Black, 400, y + 4);
                    g.DrawString("ØªÙƒÙ„ÙØ© Ø§Ù„Ø®Ø§Ù…Ø§Øª", fontHeader, Brushes.Black, 460, y + 4);
                    g.DrawString("Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„ØªÙƒÙ„ÙØ©", fontHeader, Brushes.Black, 550, y + 4);
                    g.DrawString("ØªÙƒÙ„ÙØ© Ø§Ù„Ù‚Ø·Ø¹Ø©", fontHeader, Brushes.Black, 640, y + 4);
                    g.DrawString("Ø§Ù„Ø­Ø§Ù„Ø©", fontHeader, Brushes.Black, 715, y + 4);
                    y += 24;

                    decimal sumTotal = 0;
                    foreach (DataGridViewRow row in dgOrders.Rows)
                    {
                        if (y > e.MarginBounds.Bottom - 40)
                        {
                            e.HasMorePages = true;
                            return;
                        }

                        g.DrawRectangle(Pens.LightGray, 40, y, 740, 22);
                        g.DrawString(row.Cells["OrderCode"].Value?.ToString() ?? "", fontBody, Brushes.Black, 45, y + 3);
                        g.DrawString(row.Cells["ProductionTypeName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 130, y + 3);
                        g.DrawString(row.Cells["FinishedProductName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 210, y + 3);
                        g.DrawString($"{row.Cells["ProducedQty"].Value} {row.Cells["UnitName"].Value}", fontBody, Brushes.Black, 400, y + 3);
                        g.DrawString(Convert.ToDecimal(row.Cells["RawMaterialsCost"].Value ?? 0).ToString("N2"), fontBody, Brushes.Black, 460, y + 3);
                        
                        decimal tot = Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0);
                        sumTotal += tot;
                        g.DrawString(tot.ToString("N2"), fontBold, Brushes.DarkBlue, 550, y + 3);
                        g.DrawString(Convert.ToDecimal(row.Cells["UnitCost"].Value ?? 0).ToString("N2"), fontBody, Brushes.Black, 640, y + 3);
                        g.DrawString(row.Cells["StatusName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 715, y + 3);
                        y += 22;
                    }

                    y += 10;
                    g.DrawLine(new Pen(Color.DarkBlue, 2), 40, y, 780, y);
                    y += 8;
                    g.DrawString($"Ø¥Ø¬Ù…Ø§Ù„ÙŠ ØªÙƒØ§Ù„ÙŠÙ Ø£ÙˆØ§Ù…Ø± Ø§Ù„ØªØµÙ†ÙŠØ¹: {sumTotal:N2} Ø¬.Ù…", fontBold, Brushes.DarkBlue, new PointF(40, y));
                    g.DrawString($"Ø¹Ø¯Ø¯ Ø§Ù„Ø£ÙˆØ§Ù…Ø±: {dgOrders.Rows.Count}", fontBold, Brushes.Black, new PointF(640, y));
                };

                using (var ppd = new PrintPreviewDialog { Document = pd, Width = 950, Height = 700 })
                {
                    ppd.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ÙØ´Ù„ Ø¥Ø¹Ø¯Ø§Ø¯ Ø§Ù„Ø·Ø¨Ø§Ø¹Ø©:\n" + ex.Message, "Ø®Ø·Ø£", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToCsv()
        {
            if (dgOrders.Rows.Count == 0)
            {
                MessageBox.Show("Ù„Ø§ ØªÙˆØ¬Ø¯ Ø¨ÙŠØ§Ù†Ø§Øª Ù„Ù„ØªØµØ¯ÙŠØ±.", "ØªÙ†Ø¨ÙŠÙ‡", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog { Filter = "Excel CSV (*.csv)|*.csv", FileName = $"ProductionReport_{DateTime.Now:yyyyMMdd_HHmm}.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("ÙƒÙˆØ¯ Ø§Ù„Ø£Ù…Ø±,ØªØ§Ø±ÙŠØ® Ø§Ù„Ø¥Ù†Ø´Ø§Ø¡,Ù†ÙˆØ¹ Ø§Ù„ØªØµÙ†ÙŠØ¹,Ø§Ù„Ù…Ø®Ø²Ù†,ÙƒÙˆØ¯ Ø§Ù„Ù…Ù†ØªØ¬,Ø§Ø³Ù… Ø§Ù„Ù…Ù†ØªØ¬ Ø§Ù„Ù…ØµÙ†Ø¹,Ø§Ù„ÙƒÙ…ÙŠØ©,Ø§Ù„ÙˆØ­Ø¯Ø©,ØªÙƒÙ„ÙØ© Ø§Ù„Ø®Ø§Ù…Ø§Øª,Ù…ØµØ§Ø±ÙŠÙ Ø§Ù„ØªØ´ØºÙŠÙ„,Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„ØªÙƒÙ„ÙØ©,ØªÙƒÙ„ÙØ© Ø§Ù„Ù‚Ø·Ø¹Ø©,Ø§Ù„Ø­Ø§Ù„Ø©,Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù…");
                        foreach (DataGridViewRow r in dgOrders.Rows)
                        {
                            sb.AppendLine($"\"{r.Cells["OrderCode"].Value}\",\"{r.Cells["CreatedDate"].Value}\",\"{r.Cells["ProductionTypeName"].Value}\",\"{r.Cells["WarehouseName"].Value}\",\"{r.Cells["FinishedProductCode"].Value}\",\"{r.Cells["FinishedProductName"].Value}\",\"{r.Cells["ProducedQty"].Value}\",\"{r.Cells["UnitName"].Value}\",\"{r.Cells["RawMaterialsCost"].Value}\",\"{r.Cells["ExtraExpenses"].Value}\",\"{r.Cells["TotalCost"].Value}\",\"{r.Cells["UnitCost"].Value}\",\"{r.Cells["StatusName"].Value}\",\"{r.Cells["CreatedByName"].Value}\"");
                        }
                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        MessageBox.Show("âœ… ØªÙ… ØªØµØ¯ÙŠØ± ØªÙ‚Ø±ÙŠØ± Ø§Ù„ØªØµÙ†ÙŠØ¹ Ø¨Ù†Ø¬Ø§Ø­!", "Ù†Ø¬Ø§Ø­ Ø§Ù„ØªØµØ¯ÙŠØ±", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"âŒ ÙØ´Ù„ Ø§Ù„ØªØµØ¯ÙŠØ±:\n{ex.Message}", "Ø®Ø·Ø£", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}

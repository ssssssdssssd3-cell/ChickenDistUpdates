using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;
using ChickenDist.Services;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة استقبال وإدارة طلبات المتجر الإلكتروني الاحترافية (Online Orders Management Screen)
    /// تتيح متابعة الطلبات اللحظية، مراجعة الأصناف، طباعة أذون التحضير، مراسلة العملاء واتساب،
    /// وتحويل الطلبات مباشرة إلى فواتير مبيعات بنقرة واحدة F10.
    /// </summary>
    public class FrmOnlineOrders : Form
    {
        // KPI Controls
        private Label lblKpiNew;
        private Label lblKpiInPrep;
        private Label lblKpiCompleted;
        private Label lblKpiTodayTotal;

        // Toolbar & Filters
        private TextBox txtSearch;
        private Button btnFilterAll;
        private Button btnFilterNew;
        private Button btnFilterInPrep;
        private Button btnFilterDelivery;
        private Button btnFilterCompleted;
        private Button btnFilterCancelled;
        private string _activeFilter = "الكل";

        // Orders List
        private DataGridView dgvOrders;

        // Order Details (Left Panel)
        private Label lblDetailOrderNum;
        private Label lblDetailStatus;
        private Label lblDetailTier;
        private Label lblDetailCustName;
        private Label lblDetailCustPhone;
        private Label lblDetailCustAddress;
        private TextBox txtDetailNotes;
        private Label lblDetailSubtotal;
        private Label lblDetailDelivery;
        private Label lblDetailTotal;
        private DataGridView dgvItems;

        // Action Buttons
        private Button btnConvertToSale;
        private Button btnPrintPrepSlip;
        private Button btnWhatsApp;
        private ComboBox cboChangeStatus;
        private Button btnRefresh;
        private Button btnSettings;
        private Button btnOpenStore;
        private Button btnCopyUrl;

        private int _selectedOrderID = 0;
        private DataRow _selectedOrderRow = null;

        public FrmOnlineOrders()
        {
            InitializeComponent();
            InitEventSubscriptions();
            LoadOrders();
            RefreshStats();
        }

        private void InitializeComponent()
        {
            this.Text = "🌐 استقبال وإدارة طلبات المتجر الإلكتروني | ProSoft Online Orders";
            this.Size = new Size(1180, 680);
            this.MinimumSize = new Size(900, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.Font = new Font("Segoe UI", 9f);
            this.KeyPreview = true;
            this.KeyDown += FrmOnlineOrders_KeyDown;

            // ==========================================
            // 1. الشريط العلوي المدمج (Header Bar - 40px)
            // ==========================================
            var pnlHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(8, 3, 8, 3),
                Margin = new Padding(0)
            };
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Right: Title
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // Left: Action buttons flow

            var pnlTitle = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0)
            };

            var lblTitle = new Label
            {
                Text = "🌐 طلبات المتجر الإلكتروني",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0)
            };

            var lblSubtitle = new Label
            {
                Text = "متابعة الطلبات، أذون التحضير والتحويل لفواتير (F10)",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Padding = new Padding(0, 3, 0, 0)
            };

            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(lblSubtitle);

            var pnlHeaderButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };

            btnRefresh = CreateTopButton("🔄 تحديث (F5)", Color.FromArgb(37, 99, 235));
            btnRefresh.Click += async (s, e) =>
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "⏳ سحب...";
                try
                {
                    await CloudSyncService.PullOnlineOrdersFromFirebaseAsync();
                }
                catch { }
                LoadOrders();
                RefreshStats();
                btnRefresh.Text = "🔄 تحديث (F5)";
                btnRefresh.Enabled = true;
            };

            btnSettings = CreateTopButton("⚙️ الإعدادات (F2)", Color.FromArgb(51, 65, 85));
            btnSettings.Click += (s, e) =>
            {
                using (var dlg = new FrmOnlineStoreSettings())
                {
                    dlg.ShowDialog();
                    LoadOrders();
                }
            };

            btnOpenStore = CreateTopButton("🌐 فتح المتجر", Color.FromArgb(16, 185, 129));
            btnOpenStore.Click += (s, e) =>
            {
                string projectId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
                if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";
                string url = $"https://{projectId}.web.app/store.html";
                try { System.Diagnostics.Process.Start(url); } catch { }
            };

            btnCopyUrl = CreateTopButton("📋 نسخ الرابط", Color.FromArgb(71, 85, 105));
            btnCopyUrl.Click += (s, e) =>
            {
                string projectId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
                if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";
                string url = $"https://{projectId}.web.app/store.html";
                Clipboard.SetText(url);
                MessageBox.Show("تم نسخ رابط المتجر الإلكتروني بنجاح ✅", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            pnlHeaderButtons.Controls.Add(btnRefresh);
            pnlHeaderButtons.Controls.Add(btnSettings);
            pnlHeaderButtons.Controls.Add(btnOpenStore);
            pnlHeaderButtons.Controls.Add(btnCopyUrl);

            pnlHeader.Controls.Add(pnlTitle, 0, 0);
            pnlHeader.Controls.Add(pnlHeaderButtons, 1, 0);

            // ==========================================
            // 2. بطاقات المؤشرات المدمجة (KPIs - 42px)
            // ==========================================
            var pnlKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(8, 2, 8, 2),
                BackColor = Color.FromArgb(18, 26, 43),
                Margin = new Padding(0)
            };
            pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblKpiNew = MakeKPICard("طلبات جديدة 🔴", "0", Color.FromArgb(244, 63, 94), out Panel pKpi1);
            lblKpiInPrep = MakeKPICard("قيد التجهيز 🟡", "0", Color.FromArgb(245, 158, 11), out Panel pKpi2);
            lblKpiCompleted = MakeKPICard("طلبات مكتملة 🟢", "0", Color.FromArgb(16, 185, 129), out Panel pKpi3);
            lblKpiTodayTotal = MakeKPICard("إجمالي اليوم 💰", "0.00 ج.م", Color.FromArgb(56, 189, 248), out Panel pKpi4);

            pnlKpi.Controls.Add(pKpi1, 0, 0);
            pnlKpi.Controls.Add(pKpi2, 1, 0);
            pnlKpi.Controls.Add(pKpi3, 2, 0);
            pnlKpi.Controls.Add(pKpi4, 3, 0);

            // ==========================================
            // 3. شريط الفلاتر والبحث (Toolbar - 34px)
            // ==========================================
            var pnlToolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                RowCount = 1,
                ColumnCount = 3,
                BackColor = Color.FromArgb(24, 33, 53),
                Padding = new Padding(8, 2, 8, 2),
                Margin = new Padding(0)
            };
            pnlToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Filters on Right
            pnlToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // Space in between
            pnlToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f)); // Search box on Left

            var flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0)
            };

            btnFilterAll = CreateFilterButton("الكل", true);
            btnFilterNew = CreateFilterButton("جديدة 🔴", false);
            btnFilterInPrep = CreateFilterButton("قيد التجهيز 🟡", false);
            btnFilterDelivery = CreateFilterButton("توصيل 🚚", false);
            btnFilterCompleted = CreateFilterButton("مكتمل 🟢", false);
            btnFilterCancelled = CreateFilterButton("ملغي ⚪", false);

            btnFilterAll.Click += (s, e) => SetFilter("الكل", btnFilterAll);
            btnFilterNew.Click += (s, e) => SetFilter("جديد", btnFilterNew);
            btnFilterInPrep.Click += (s, e) => SetFilter("قيد التجهيز", btnFilterInPrep);
            btnFilterDelivery.Click += (s, e) => SetFilter("جاري التوصيل", btnFilterDelivery);
            btnFilterCompleted.Click += (s, e) => SetFilter("مكتمل", btnFilterCompleted);
            btnFilterCancelled.Click += (s, e) => SetFilter("ملغي", btnFilterCancelled);

            flowFilters.Controls.Add(btnFilterAll);
            flowFilters.Controls.Add(btnFilterNew);
            flowFilters.Controls.Add(btnFilterInPrep);
            flowFilters.Controls.Add(btnFilterDelivery);
            flowFilters.Controls.Add(btnFilterCompleted);
            flowFilters.Controls.Add(btnFilterCancelled);

            var pnlSearch = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 1, 0, 1)
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => LoadOrders();

            var lblSearch = new Label
            {
                Text = "🔍",
                Dock = DockStyle.Right,
                Width = 26,
                ForeColor = Color.FromArgb(203, 213, 225),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f)
            };

            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(lblSearch);

            pnlToolbar.Controls.Add(flowFilters, 0, 0);
            pnlToolbar.Controls.Add(new Panel { Dock = DockStyle.Fill }, 1, 0);
            pnlToolbar.Controls.Add(pnlSearch, 2, 0);

            // ==========================================
            // 4. المحتوى المنقسم (Master - Detail Split)
            // ==========================================
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 720,
                SplitterWidth = 5,
                Panel1MinSize = 420,
                Panel2MinSize = 340,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            // الجانب الأيمن (Panel1): جدول الطلبات الرئيسي
            var pnlOrdersGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(6, 4, 6, 4)
            };

            dgvOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 8.5f)
            };
            dgvOrders.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvOrders.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(56, 189, 248);
            dgvOrders.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvOrders.ColumnHeadersHeight = 30;
            dgvOrders.RowTemplate.Height = 28;
            dgvOrders.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvOrders.DefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 235);
            dgvOrders.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvOrders.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(20, 29, 47);
            dgvOrders.SelectionChanged += DgvOrders_SelectionChanged;
            dgvOrders.CellFormatting += DgvOrders_CellFormatting;

            pnlOrdersGrid.Controls.Add(dgvOrders);
            splitContainer.Panel1.Controls.Add(pnlOrdersGrid);

            // الجانب الأيسر (Panel2): كارت تفاصيل الطلب والأصناف والإجراءات
            var pnlDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 29, 47),
                Padding = new Padding(6)
            };

            // 4.1 رأس كارت التفاصيل (Order Header - 32px)
            var pnlDetailHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                RowCount = 1,
                ColumnCount = 3,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(0, 0, 0, 4)
            };
            pnlDetailHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            pnlDetailHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            pnlDetailHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblDetailOrderNum = new Label
            {
                Text = "طلب: ---",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            lblDetailStatus = new Label
            {
                Text = "الحالة: ---",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 63, 94),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblDetailTier = new Label
            {
                Text = "فئة: ---",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlDetailHeader.Controls.Add(lblDetailOrderNum, 0, 0);
            pnlDetailHeader.Controls.Add(lblDetailStatus, 1, 0);
            pnlDetailHeader.Controls.Add(lblDetailTier, 2, 0);

            // 4.2 بيانات العميل والتوصيل (Customer Card - 78px)
            var grpCustomer = new GroupBox
            {
                Text = "👤 بيانات العميل والتوصيل",
                Dock = DockStyle.Top,
                Height = 78,
                ForeColor = Color.FromArgb(96, 165, 250),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(0, 0, 0, 4)
            };

            var tblCustomer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(2)
            };
            tblCustomer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tblCustomer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tblCustomer.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblCustomer.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            lblDetailCustName = new Label
            {
                Text = "الاسم: ---",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            lblDetailCustPhone = new Label
            {
                Text = "الهاتف: --- 📱",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand
            };
            lblDetailCustPhone.Click += (s, e) => SendWhatsAppMessage();

            lblDetailCustAddress = new Label
            {
                Text = "العنوان: ---",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(226, 232, 240),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            txtDetailNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(253, 224, 71),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "ملاحظات: ---"
            };

            tblCustomer.Controls.Add(lblDetailCustName, 0, 0);
            tblCustomer.Controls.Add(lblDetailCustPhone, 1, 0);
            tblCustomer.Controls.Add(lblDetailCustAddress, 0, 1);
            tblCustomer.Controls.Add(txtDetailNotes, 1, 1);
            grpCustomer.Controls.Add(tblCustomer);

            // 4.3 بنود وأصناف الطلب (Items Table - Fill)
            var grpItems = new GroupBox
            {
                Text = "🛒 بنود الطلب",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(96, 165, 250),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Padding = new Padding(4)
            };

            dgvItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 8.5f)
            };
            dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(56, 189, 248);
            dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvItems.ColumnHeadersHeight = 26;
            dgvItems.RowTemplate.Height = 24;
            dgvItems.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);

            grpItems.Controls.Add(dgvItems);

            // 4.4 ملخص الحسابات المالي (Financial Totals - 28px)
            var pnlTotals = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                RowCount = 1,
                ColumnCount = 3,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(0)
            };
            pnlTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            pnlTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            pnlTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            lblDetailSubtotal = new Label
            {
                Text = "المجموع: 0.00 ج.م",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            lblDetailDelivery = new Label
            {
                Text = "التوصيل: 0.00 ج.م",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblDetailTotal = new Label
            {
                Text = "الإجمالي: 0.00 ج.م",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlTotals.Controls.Add(lblDetailSubtotal, 0, 0);
            pnlTotals.Controls.Add(lblDetailDelivery, 1, 0);
            pnlTotals.Controls.Add(lblDetailTotal, 2, 0);

            // 4.5 أزرار الإجراءات السريعة (Actions Panel - 68px)
            var pnlActions = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                RowCount = 2,
                ColumnCount = 2,
                BackColor = Color.FromArgb(20, 29, 47),
                Padding = new Padding(2, 2, 2, 2),
                Margin = new Padding(0)
            };
            pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            pnlActions.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));
            pnlActions.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));

            btnConvertToSale = new Button
            {
                Text = "⚡ تحويل لفاتورة (F10)",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 1, 2, 1)
            };
            btnConvertToSale.FlatAppearance.BorderSize = 0;
            btnConvertToSale.Click += (s, e) => ConvertOrderToSaleInvoice();

            btnPrintPrepSlip = new Button
            {
                Text = "🖨️ طباعة تحضير (F9)",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 1, 2, 1)
            };
            btnPrintPrepSlip.FlatAppearance.BorderSize = 0;
            btnPrintPrepSlip.Click += (s, e) => PrintPreparationSlip();

            btnWhatsApp = new Button
            {
                Text = "💬 واتساب العميل",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 1, 2, 1)
            };
            btnWhatsApp.FlatAppearance.BorderSize = 0;
            btnWhatsApp.Click += (s, e) => SendWhatsAppMessage();

            var pnlStatusMini = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Margin = new Padding(2, 1, 2, 1)
            };
            pnlStatusMini.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlStatusMini.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var lblChangeStatus = new Label
            {
                Text = "الحالة:",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(203, 213, 225),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 8f),
                AutoSize = true,
                Margin = new Padding(0, 0, 4, 0)
            };

            cboChangeStatus = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f)
            };
            cboChangeStatus.Items.AddRange(new object[] { "جديد", "قيد التجهيز", "جاري التوصيل", "مكتمل", "ملغي" });
            cboChangeStatus.SelectedIndexChanged += CboChangeStatus_SelectedIndexChanged;

            pnlStatusMini.Controls.Add(lblChangeStatus, 0, 0);
            pnlStatusMini.Controls.Add(cboChangeStatus, 1, 0);

            pnlActions.Controls.Add(btnConvertToSale, 0, 0);
            pnlActions.Controls.Add(btnPrintPrepSlip, 1, 0);
            pnlActions.Controls.Add(btnWhatsApp, 0, 1);
            pnlActions.Controls.Add(pnlStatusMini, 1, 1);

            // ترتيب الإضافة إلى pnlDetails (قاعدة WinForms Docking)
            // نُضيف Fill أولاً ثم عناصر Bottom ثم عناصر Top حتى لا تحجب أي مساحة
            pnlDetails.Controls.Add(grpItems);
            pnlDetails.Controls.Add(pnlTotals);
            pnlDetails.Controls.Add(pnlActions);
            pnlDetails.Controls.Add(grpCustomer);
            pnlDetails.Controls.Add(pnlDetailHeader);

            splitContainer.Panel2.Controls.Add(pnlDetails);

            this.Controls.Add(splitContainer);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlKpi);
            this.Controls.Add(pnlHeader);
        }

        private Button CreateTopButton(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 28,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(6, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateFilterButton(string text, bool active)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 26,
                BackColor = active ? Color.FromArgb(37, 99, 235) : Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 1, 2, 1),
                Padding = new Padding(5, 0, 5, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Label MakeKPICard(string title, string value, Color accent, out Panel card)
        {
            card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(4, 2, 4, 2),
                Margin = new Padding(2, 1, 2, 1)
            };

            var pnlAccent = new Panel
            {
                Dock = DockStyle.Right,
                Width = 3,
                BackColor = accent
            };

            var pnlText = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(2, 0, 4, 0)
            };
            pnlText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            pnlText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            var lblTitle = new Label
            {
                Text = title,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 7.5f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            var lblVal = new Label
            {
                Text = value,
                ForeColor = accent,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlText.Controls.Add(lblTitle, 0, 0);
            pnlText.Controls.Add(lblVal, 1, 0);

            card.Controls.Add(pnlText);
            card.Controls.Add(pnlAccent);

            return lblVal;
        }

        private void InitEventSubscriptions()
        {
            CloudSyncService.OnNewOrdersReceived += OnNewOrdersReceivedHandler;
            this.FormClosed += (s, e) => CloudSyncService.OnNewOrdersReceived -= OnNewOrdersReceivedHandler;
        }

        private void OnNewOrdersReceivedHandler(int count)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            this.BeginInvoke((MethodInvoker)delegate
            {
                LoadOrders();
                RefreshStats();
            });
        }

        private void SetFilter(string status, Button activeBtn)
        {
            _activeFilter = status;

            btnFilterAll.BackColor = Color.FromArgb(30, 41, 59);
            btnFilterNew.BackColor = Color.FromArgb(30, 41, 59);
            btnFilterInPrep.BackColor = Color.FromArgb(30, 41, 59);
            btnFilterDelivery.BackColor = Color.FromArgb(30, 41, 59);
            btnFilterCompleted.BackColor = Color.FromArgb(30, 41, 59);
            btnFilterCancelled.BackColor = Color.FromArgb(30, 41, 59);

            activeBtn.BackColor = Color.FromArgb(37, 99, 235);
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                string search = txtSearch.Text.Trim();
                DataTable dt = OnlineOrdersDAL.GetOrders(_activeFilter, null, null, search);

                dgvOrders.DataSource = dt;

                if (dgvOrders.Columns["OnlineOrderID"] != null) dgvOrders.Columns["OnlineOrderID"].Visible = false;
                if (dgvOrders.Columns["RemoteOrderID"] != null) dgvOrders.Columns["RemoteOrderID"].Visible = false;
                if (dgvOrders.Columns["Notes"] != null) dgvOrders.Columns["Notes"].Visible = false;
                if (dgvOrders.Columns["CreatedAt"] != null) dgvOrders.Columns["CreatedAt"].Visible = false;

                if (dgvOrders.Columns["OrderNumber"] != null)
                {
                    dgvOrders.Columns["OrderNumber"].HeaderText = "رقم الطلب";
                    dgvOrders.Columns["OrderNumber"].FillWeight = 85;
                    dgvOrders.Columns["OrderNumber"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvOrders.Columns["OrderDate"] != null)
                {
                    dgvOrders.Columns["OrderDate"].HeaderText = "تاريخ الطلب";
                    dgvOrders.Columns["OrderDate"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                    dgvOrders.Columns["OrderDate"].FillWeight = 110;
                    dgvOrders.Columns["OrderDate"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvOrders.Columns["CustomerName"] != null)
                {
                    dgvOrders.Columns["CustomerName"].HeaderText = "اسم العميل";
                    dgvOrders.Columns["CustomerName"].FillWeight = 135;
                    dgvOrders.Columns["CustomerName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (dgvOrders.Columns["CustomerPhone"] != null)
                {
                    dgvOrders.Columns["CustomerPhone"].HeaderText = "الهاتف";
                    dgvOrders.Columns["CustomerPhone"].FillWeight = 95;
                    dgvOrders.Columns["CustomerPhone"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvOrders.Columns["CustomerAddress"] != null)
                {
                    dgvOrders.Columns["CustomerAddress"].HeaderText = "العنوان";
                    dgvOrders.Columns["CustomerAddress"].FillWeight = 135;
                    dgvOrders.Columns["CustomerAddress"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (dgvOrders.Columns["ItemsCount"] != null)
                {
                    dgvOrders.Columns["ItemsCount"].HeaderText = "الأصناف";
                    dgvOrders.Columns["ItemsCount"].FillWeight = 55;
                    dgvOrders.Columns["ItemsCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvOrders.Columns["SubTotal"] != null) dgvOrders.Columns["SubTotal"].Visible = false;
                if (dgvOrders.Columns["DeliveryCharge"] != null) dgvOrders.Columns["DeliveryCharge"].Visible = false;
                if (dgvOrders.Columns["TotalAmount"] != null)
                {
                    dgvOrders.Columns["TotalAmount"].HeaderText = "الإجمالي";
                    dgvOrders.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
                    dgvOrders.Columns["TotalAmount"].FillWeight = 85;
                    dgvOrders.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvOrders.Columns["TotalAmount"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                }
                if (dgvOrders.Columns["PriceTier"] != null)
                {
                    dgvOrders.Columns["PriceTier"].HeaderText = "الفئة";
                    dgvOrders.Columns["PriceTier"].FillWeight = 60;
                    dgvOrders.Columns["PriceTier"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvOrders.Columns["Status"] != null)
                {
                    dgvOrders.Columns["Status"].HeaderText = "الحالة";
                    dgvOrders.Columns["Status"].FillWeight = 85;
                    dgvOrders.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvOrders.Columns["CreatedSaleID"] != null)
                {
                    dgvOrders.Columns["CreatedSaleID"].HeaderText = "رقم الفاتورة";
                    dgvOrders.Columns["CreatedSaleID"].FillWeight = 70;
                    dgvOrders.Columns["CreatedSaleID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dgvOrders.Rows.Count > 0 && _selectedOrderID <= 0)
                {
                    dgvOrders.Rows[0].Selected = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadOrders error: " + ex.Message);
            }
        }

        private void RefreshStats()
        {
            try
            {
                OnlineOrdersDAL.GetStats(out int newC, out int inPrepC, out int compC, out decimal tot);
                lblKpiNew.Text = newC.ToString();
                lblKpiInPrep.Text = inPrepC.ToString();
                lblKpiCompleted.Text = compC.ToString();
                lblKpiTodayTotal.Text = tot.ToString("N2") + " ج.م";
            }
            catch { }
        }

        private void DgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;

            var row = dgvOrders.SelectedRows[0];
            _selectedOrderID = Convert.ToInt32(row.Cells["OnlineOrderID"].Value);

            if (row.DataBoundItem is DataRowView drv)
            {
                _selectedOrderRow = drv.Row;
                DisplayOrderDetails(_selectedOrderRow);
            }
        }

        private void DisplayOrderDetails(DataRow r)
        {
            if (r == null) return;

            string orderNum = r["OrderNumber"]?.ToString() ?? "";
            string status = r["Status"]?.ToString() ?? "جديد";
            string tier = r["PriceTier"]?.ToString() ?? "قطاعي";
            string name = r["CustomerName"]?.ToString() ?? "";
            string phone = r["CustomerPhone"]?.ToString() ?? "";
            string addr = r["CustomerAddress"]?.ToString() ?? "";
            string notes = r["Notes"]?.ToString() ?? "";

            decimal sub = r["SubTotal"] != DBNull.Value ? Convert.ToDecimal(r["SubTotal"]) : 0m;
            decimal del = r["DeliveryCharge"] != DBNull.Value ? Convert.ToDecimal(r["DeliveryCharge"]) : 0m;
            decimal tot = r["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(r["TotalAmount"]) : sub + del;

            int saleId = r["CreatedSaleID"] != DBNull.Value ? Convert.ToInt32(r["CreatedSaleID"]) : 0;

            lblDetailOrderNum.Text = $"طلب: {orderNum}";
            lblDetailStatus.Text = $"الحالة: {status}";
            lblDetailTier.Text = $"فئة السعر: {tier}";

            if (status == "جديد") lblDetailStatus.ForeColor = Color.FromArgb(244, 63, 94);
            else if (status == "قيد التجهيز" || status == "جاري التوصيل") lblDetailStatus.ForeColor = Color.FromArgb(245, 158, 11);
            else if (status == "مكتمل") lblDetailStatus.ForeColor = Color.FromArgb(52, 211, 153);
            else lblDetailStatus.ForeColor = Color.FromArgb(148, 163, 184);

            lblDetailCustName.Text = $"الاسم: {name}";
            lblDetailCustPhone.Text = $"الهاتف: {phone} 📱";
            lblDetailCustAddress.Text = $"العنوان: {addr}";
            txtDetailNotes.Text = string.IsNullOrEmpty(notes) ? "ملاحظات: لا توجد" : $"ملاحظات: {notes}";

            lblDetailSubtotal.Text = $"المجموع: {sub:N2} ج.م";
            lblDetailDelivery.Text = $"التوصيل: {del:N2} ج.م";
            lblDetailTotal.Text = $"الإجمالي: {tot:N2} ج.م";

            // تعطيل زر التحويل إذا كان محولاً مسبقاً
            if (saleId > 0)
            {
                btnConvertToSale.Text = $"✅ محول لفاتورة #{saleId}";
                btnConvertToSale.BackColor = Color.FromArgb(51, 65, 85);
            }
            else
            {
                btnConvertToSale.Text = "⚡ تحويل لفاتورة مبيعات (F10)";
                btnConvertToSale.BackColor = Color.FromArgb(16, 185, 129);
            }

            // تعيين حالة ComboBox
            cboChangeStatus.SelectedIndexChanged -= CboChangeStatus_SelectedIndexChanged;
            cboChangeStatus.SelectedItem = status;
            cboChangeStatus.SelectedIndexChanged += CboChangeStatus_SelectedIndexChanged;

            // تحميل أصناف الطلب
            LoadOrderItems(_selectedOrderID);
        }

        private void LoadOrderItems(int orderId)
        {
            try
            {
                DataTable dt = OnlineOrdersDAL.GetOrderItems(orderId);
                dgvItems.DataSource = dt;

                if (dgvItems.Columns["ItemRowID"] != null) dgvItems.Columns["ItemRowID"].Visible = false;
                if (dgvItems.Columns["OnlineOrderID"] != null) dgvItems.Columns["OnlineOrderID"].Visible = false;
                if (dgvItems.Columns["ProductID"] != null) dgvItems.Columns["ProductID"].Visible = false;
                if (dgvItems.Columns["Notes"] != null) dgvItems.Columns["Notes"].Visible = false;

                if (dgvItems.Columns["ProductName"] != null)
                {
                    dgvItems.Columns["ProductName"].HeaderText = "اسم الصنف";
                    dgvItems.Columns["ProductName"].FillWeight = 140;
                    dgvItems.Columns["ProductName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (dgvItems.Columns["UnitName"] != null)
                {
                    dgvItems.Columns["UnitName"].HeaderText = "الوحدة";
                    dgvItems.Columns["UnitName"].FillWeight = 65;
                    dgvItems.Columns["UnitName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvItems.Columns["Quantity"] != null)
                {
                    dgvItems.Columns["Quantity"].HeaderText = "الكمية";
                    dgvItems.Columns["Quantity"].DefaultCellStyle.Format = "G29";
                    dgvItems.Columns["Quantity"].FillWeight = 60;
                    dgvItems.Columns["Quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvItems.Columns["UnitPrice"] != null)
                {
                    dgvItems.Columns["UnitPrice"].HeaderText = "السعر";
                    dgvItems.Columns["UnitPrice"].DefaultCellStyle.Format = "N2";
                    dgvItems.Columns["UnitPrice"].FillWeight = 70;
                    dgvItems.Columns["UnitPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (dgvItems.Columns["TotalPrice"] != null)
                {
                    dgvItems.Columns["TotalPrice"].HeaderText = "الإجمالي";
                    dgvItems.Columns["TotalPrice"].DefaultCellStyle.Format = "N2";
                    dgvItems.Columns["TotalPrice"].FillWeight = 80;
                    dgvItems.Columns["TotalPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvItems.Columns["TotalPrice"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                }
                if (dgvItems.Columns["ProductCode"] != null)
                {
                    dgvItems.Columns["ProductCode"].HeaderText = "الكود";
                    dgvItems.Columns["ProductCode"].FillWeight = 60;
                    dgvItems.Columns["ProductCode"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadOrderItems error: " + ex.Message);
            }
        }

        private void DgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvOrders.Rows.Count) return;

            var row = dgvOrders.Rows[e.RowIndex];
            string status = row.Cells["Status"]?.Value?.ToString() ?? "";

            if (dgvOrders.Columns[e.ColumnIndex].Name == "Status")
            {
                if (status == "جديد")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(244, 63, 94);
                    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else if (status == "قيد التجهيز" || status == "جاري التوصيل")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(245, 158, 11);
                    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else if (status == "مكتمل")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(52, 211, 153);
                    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
            }
        }

        private void CboChangeStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_selectedOrderID <= 0) return;

            string newStatus = cboChangeStatus.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(newStatus)) return;

            OnlineOrdersDAL.UpdateStatus(_selectedOrderID, newStatus);
            LoadOrders();
            RefreshStats();
        }

        private void ConvertOrderToSaleInvoice()
        {
            if (_selectedOrderID <= 0 || _selectedOrderRow == null)
            {
                MessageBox.Show("يرجى اختيار طلب أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int existingSaleId = _selectedOrderRow["CreatedSaleID"] != DBNull.Value ? Convert.ToInt32(_selectedOrderRow["CreatedSaleID"]) : 0;
            if (existingSaleId > 0)
            {
                var ask = MessageBox.Show($"هذا الطلب تم تحويله مسبقاً إلى فاتورة مبيعات رقم #{existingSaleId}.\nهل ترغب في فتح الفاتورة الآن؟", "فاتورة مسبقة", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (ask == DialogResult.Yes)
                {
                    using (var frm = new FrmSale(existingSaleId))
                    {
                        frm.ShowDialog();
                    }
                }
                return;
            }

            DataTable dtItems = OnlineOrdersDAL.GetOrderItems(_selectedOrderID);
            if (dtItems == null || dtItems.Rows.Count == 0)
            {
                MessageBox.Show("الطلب لا يحتوي على أصناف صالحة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string custName = _selectedOrderRow["CustomerName"]?.ToString() ?? "عميل أونلاين";
            string custPhone = _selectedOrderRow["CustomerPhone"]?.ToString() ?? "";
            string custAddr = _selectedOrderRow["CustomerAddress"]?.ToString() ?? "";
            string orderNum = _selectedOrderRow["OrderNumber"]?.ToString() ?? "";
            string notes = _selectedOrderRow["Notes"]?.ToString() ?? "";
            decimal delivery = _selectedOrderRow["DeliveryCharge"] != DBNull.Value ? Convert.ToDecimal(_selectedOrderRow["DeliveryCharge"]) : 0m;
            decimal total = _selectedOrderRow["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(_selectedOrderRow["TotalAmount"]) : 0m;
            string tier = _selectedOrderRow["PriceTier"]?.ToString() ?? "قطاعي";

            var saleItemsList = new List<SaleItemDTO>();
            foreach (DataRow r in dtItems.Rows)
            {
                int pid = r["ProductID"] != DBNull.Value ? Convert.ToInt32(r["ProductID"]) : 0;
                string pName = r["ProductName"]?.ToString() ?? "";
                string uName = r["UnitName"]?.ToString() ?? "قطعة";
                decimal qty = r["Quantity"] != DBNull.Value ? Convert.ToDecimal(r["Quantity"]) : 1m;
                decimal price = r["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(r["UnitPrice"]) : 0m;

                saleItemsList.Add(new SaleItemDTO
                {
                    ProductID = pid,
                    ProductName = pName,
                    UnitName = uName,
                    Quantity = qty,
                    UnitPrice = price,
                    Factor = 1m
                });
            }

            string saleNotes = $"طلب أونلاين: {orderNum} | العميل: {custName} - هاتف: {custPhone} - عنوان: {custAddr}";
            if (!string.IsNullOrEmpty(notes)) saleNotes += $" | ملاحظات: {notes}";

            try
            {
                int newSaleID = SaleDAL.SaveSale(
                    saleType: 2, // Cash
                    clientID: null,
                    driverID: null,
                    total: total,
                    notes: saleNotes,
                    items: saleItemsList,
                    discountAmount: 0m,
                    discountPct: 0m,
                    isDraft: false,
                    warehouseID: null,
                    priceTier: tier,
                    shippingCharge: delivery,
                    customClientName: $"{custName} ({custPhone})"
                );

                if (newSaleID > 0)
                {
                    OnlineOrdersDAL.LinkToSale(_selectedOrderID, newSaleID);
                    LoadOrders();
                    RefreshStats();

                    var result = MessageBox.Show(
                        $"تم تحويل الطلب بنجاح إلى فاتورة مبيعات رسمية رقم #{newSaleID} 🔥\n\nهل ترغب في فتح الفاتورة لمعاينتها أو طباعتها الآن؟",
                        "نجاح التحويل", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        using (var frm = new FrmSale(newSaleID))
                        {
                            frm.ShowDialog();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحويل الطلب لفاتورة مبيعات: " + ex.Message, "خطأ في التحويل", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendWhatsAppMessage()
        {
            if (_selectedOrderRow == null) return;

            string phone = _selectedOrderRow["CustomerPhone"]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("لا يوجد رقم هاتف مسجل لهذا الطلب!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rawPhone = System.Text.RegularExpressions.Regex.Replace(phone, "[^0-9]", "");
            if (rawPhone.StartsWith("0")) rawPhone = "2" + rawPhone;
            else if (!rawPhone.StartsWith("20")) rawPhone = "20" + rawPhone;

            string orderNum = _selectedOrderRow["OrderNumber"]?.ToString() ?? "";
            string custName = _selectedOrderRow["CustomerName"]?.ToString() ?? "";
            decimal total = _selectedOrderRow["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(_selectedOrderRow["TotalAmount"]) : 0m;
            string status = _selectedOrderRow["Status"]?.ToString() ?? "قيد التجهيز";

            string msg = $"السلام عليكم أستاذ {custName} 👋\nبخصوص طلبكم رقم ({orderNum}) بقيمة {total:N2} ج.م.\nحالة الطلب الحالية: {status}.\nنشكركم لتسوقكم معنا في {AppConfig.CompanyName}!";

            string url = $"https://wa.me/{rawPhone}?text={Uri.EscapeDataString(msg)}";
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر فتح الواتساب: " + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PrintPreparationSlip()
        {
            if (_selectedOrderRow == null) return;

            try
            {
                var pd = new PrintDocument();
                pd.PrintPage += PrintDocument_PrintPage;

                string printerName = AppConfig.ReceiptPrinterName;
                if (!string.IsNullOrEmpty(printerName))
                {
                    pd.PrinterSettings.PrinterName = printerName;
                }

                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر طباعة إذن التحضير: " + ex.Message, "خطأ في الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_selectedOrderRow == null) return;

            Graphics g = e.Graphics;
            float y = 10;
            float width = e.PageBounds.Width;
            var fontTitle = new Font("Segoe UI", 12, FontStyle.Bold);
            var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var fontRegular = new Font("Segoe UI", 9, FontStyle.Regular);
            var fontSmall = new Font("Segoe UI", 8, FontStyle.Regular);
            var brush = Brushes.Black;
            var pen = Pens.Black;

            var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
            var sfRight = new StringFormat { Alignment = StringAlignment.Far };
            var sfLeft = new StringFormat { Alignment = StringAlignment.Near };

            // 1. ترويسة إذن التحضير
            g.DrawString(AppConfig.CompanyName, fontTitle, brush, width / 2, y, sfCenter);
            y += 24;
            g.DrawString("📋 إذن تحضير طلب أونلاين", fontHeader, brush, width / 2, y, sfCenter);
            y += 22;

            string orderNum = _selectedOrderRow["OrderNumber"]?.ToString() ?? "";
            DateTime oDate = _selectedOrderRow["OrderDate"] != DBNull.Value ? Convert.ToDateTime(_selectedOrderRow["OrderDate"]) : DateTime.Now;

            g.DrawString($"رقم الطلب: {orderNum}", fontHeader, brush, width - 10, y, sfRight);
            y += 18;
            g.DrawString($"التاريخ: {oDate:yyyy-MM-dd HH:mm}", fontSmall, brush, width - 10, y, sfRight);
            y += 20;

            g.DrawLine(pen, 10, y, width - 10, y);
            y += 6;

            // 2. بيانات العميل
            string custName = _selectedOrderRow["CustomerName"]?.ToString() ?? "";
            string custPhone = _selectedOrderRow["CustomerPhone"]?.ToString() ?? "";
            string custAddr = _selectedOrderRow["CustomerAddress"]?.ToString() ?? "";

            g.DrawString($"العميل: {custName}", fontHeader, brush, width - 10, y, sfRight);
            y += 18;
            g.DrawString($"الهاتف: {custPhone}", fontRegular, brush, width - 10, y, sfRight);
            y += 18;
            g.DrawString($"العنوان: {custAddr}", fontRegular, brush, width - 10, y, sfRight);
            y += 22;

            g.DrawLine(pen, 10, y, width - 10, y);
            y += 8;

            // 3. جدول الأصناف
            g.DrawString("الصنف", fontHeader, brush, width - 10, y, sfRight);
            g.DrawString("الكمية", fontHeader, brush, 80, y, sfLeft);
            g.DrawString("السعر", fontHeader, brush, 10, y, sfLeft);
            y += 20;
            g.DrawLine(pen, 10, y, width - 10, y);
            y += 6;

            DataTable dtItems = OnlineOrdersDAL.GetOrderItems(_selectedOrderID);
            if (dtItems != null)
            {
                foreach (DataRow ir in dtItems.Rows)
                {
                    string pName = ir["ProductName"]?.ToString() ?? "";
                    string uName = ir["UnitName"]?.ToString() ?? "";
                    decimal qty = ir["Quantity"] != DBNull.Value ? Convert.ToDecimal(ir["Quantity"]) : 0m;
                    decimal price = ir["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(ir["TotalPrice"]) : 0m;

                    g.DrawString(pName, fontRegular, brush, width - 10, y, sfRight);
                    g.DrawString($"{qty:G29} {uName}", fontRegular, brush, 80, y, sfLeft);
                    g.DrawString($"{price:N2}", fontRegular, brush, 10, y, sfLeft);
                    y += 18;
                }
            }

            y += 6;
            g.DrawLine(pen, 10, y, width - 10, y);
            y += 8;

            // 4. الإجماليات
            decimal total = _selectedOrderRow["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(_selectedOrderRow["TotalAmount"]) : 0m;
            decimal delivery = _selectedOrderRow["DeliveryCharge"] != DBNull.Value ? Convert.ToDecimal(_selectedOrderRow["DeliveryCharge"]) : 0m;

            if (delivery > 0)
            {
                g.DrawString($"خدمة التوصيل: {delivery:N2} ج.م", fontSmall, brush, width - 10, y, sfRight);
                y += 16;
            }
            g.DrawString($"الإجمالي المطلوب: {total:N2} ج.م", fontHeader, brush, width - 10, y, sfRight);
            y += 24;

            string notes = _selectedOrderRow["Notes"]?.ToString();
            if (!string.IsNullOrEmpty(notes))
            {
                g.DrawString($"ملاحظات العميل: {notes}", fontSmall, brush, width - 10, y, sfRight);
                y += 20;
            }

            g.DrawString("*** شكراً لتسوقكم معنا ***", fontSmall, brush, width / 2, y, sfCenter);
        }

        private void FrmOnlineOrders_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                btnRefresh.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F10)
            {
                btnConvertToSale.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F9)
            {
                btnPrintPrepSlip.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                btnSettings.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}

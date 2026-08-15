using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmInventory : Form
    {
        private TabControl tabMain;
        private TabPage tabStock, tabLogs;

        // Tab Stock
        private DataGridView dgStock;
        private TextBox txtSearch;
        private ComboBox cboWarehouse;
        private Button btnSearch, btnMovement, btnPrintStock, btnAddExpiryRow;
        private CheckBox chkBelowMin, chkHideZeroStock, chkExpiryOnly, chkScaleOnly;
        private ComboBox cboCategory, cboMaxRows, cboPriceType;
        private Label lblCount, lblTotalCost, lblTotalSale;

        private Button btnSaveAdj, btnClearAdj;
        private int _selectedProductID = 0;
        private decimal _selectedBookQty = 0;
        private string _selectedProductName = "";
        private string _selectedProductUnit = "";
        private bool _selectedHasExpiry = false;
        private int? _selectedDefaultExpiryDays = null;
        // حفظ الأرصدة الفعلية المدخلة عبر إعادات التحميل
        private readonly System.Collections.Generic.Dictionary<int, decimal> _enteredActualQty
            = new System.Collections.Generic.Dictionary<int, decimal>();

        // قائمة الأصناف التي تم إخفاؤها مؤقتاً بالزر الأيمن لربطها بالجرد لاحقاً
        private readonly System.Collections.Generic.HashSet<int> _hiddenProductIDs
            = new System.Collections.Generic.HashSet<int>();

        // ── دورة الجرد الحالية ──────────────────────────────
        private Button btnStartInventory;
        private Label lblInventoryStart;
        private CheckBox chkUninventoriedOnly;
        private ComboBox cboLocation;
        private DateTime? _inventoryStartDate = null;
        private System.Collections.Generic.HashSet<int> _inventoriedProductIDs
            = new System.Collections.Generic.HashSet<int>();

        // Tab Logs
        private DataGridView dgLogs;
        private DateTimePicker dtpFrom, dtpTo;
        private TextBox txtSearchLog;
        private ComboBox cboLogWarehouse;
        private Button btnLoadLogs, btnPrintLogs;
        private Timer _searchTimer;

        public FrmInventory(bool belowMinOnly = false)
        {
            _searchTimer = new Timer { Interval = 220 };
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); LoadStock(); };
            InitUI();
            LoadWarehouses();
            LoadLocations();
            LoadLogWarehouses();
            if (belowMinOnly && chkBelowMin != null)
            {
                chkBelowMin.Checked = true;
            }
            LoadStock();
            LoadLogs();
        }

        private void InitUI()
        {
            this.Text = "جرد وتعديل الأسعار";
            this.Size = new Size(1050, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            tabMain = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontMain };
            tabStock = new TabPage("📦 جرد وتعديل أسعار الأصناف") { BackColor = Theme.BgMain };
            tabLogs = new TabPage("📜 سجل تسويات الجرد") { BackColor = Theme.BgMain };
            tabMain.TabPages.AddRange(new[] { tabStock, tabLogs });
            this.Controls.Add(tabMain);

            BuildStockTab();
            BuildLogsTab();

            Theme.ApplyFormRTL(this);
        }

        private void BuildStockTab()
        {
            // Panel Left: Grid and filters (Dock Fill)
            var pnlLeft = new Panel { Dock = DockStyle.Fill };

            var pnlHeaderContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 122,
                BackColor = Theme.BgCard
            };

            // الصف 1: فلاتر البحث الرئيسية
            var pnlRow1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Theme.BgCard,
                Padding = new Padding(6, 5, 6, 2),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            // الصف 2: الخيارات والمرشحات الإضافية
            var pnlRow2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(240, 244, 248),
                Padding = new Padding(6, 4, 6, 2),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            // الصف 3: أزرار العمليات
            var pnlRow3 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(230, 236, 244),
                Padding = new Padding(6, 5, 6, 2),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            var lblWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(4, 6, 2, 0) };
            cboWarehouse = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(2, 3, 10, 0) };
            cboWarehouse.SelectedIndexChanged += (s, e) => LoadStock();

            var lblCat = new Label { Text = "التصنيف:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(4, 6, 2, 0) };
            cboCategory = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(2, 3, 10, 0) };
            try
            {
                cboCategory.Items.Add(new ComboItem(0, "(كل التصنيفات)"));
                var dtCat = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories WHERE IsActive = 1 ORDER BY CategoryName");
                foreach (DataRow r in dtCat.Rows)
                {
                    cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                }
                cboCategory.SelectedIndex = 0;
            }
            catch { }
            cboCategory.SelectedIndexChanged += (s, e) => LoadStock();

            var lblLoc = new Label { Text = "📍 المكان/الرف:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(4, 6, 2, 0) };
            cboLocation = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(2, 3, 10, 0) };
            cboLocation.SelectedIndexChanged += (s, e) => LoadStock();

            var lblSch = new Label { Text = "بحث صنف:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(4, 6, 2, 0) };
            txtSearch = new TextBox { Width = 110, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Margin = new Padding(2, 3, 2, 0) };
            txtSearch.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { _searchTimer.Stop(); LoadStock(); } };

            btnSearch = Theme.MakeButton("🔍 بحث", Color.FromArgb(60, 100, 60));
            btnSearch.Size = new Size(65, 26);
            btnSearch.Margin = new Padding(2, 2, 10, 0);
            btnSearch.Click += (s, e) => LoadStock();

            var lblLimit = new Label { Text = "عدد العرض:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(4, 6, 2, 0) };
            cboMaxRows = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(2, 3, 10, 0) };
            cboMaxRows.Items.AddRange(new object[] { "300 صنف", "500 صنف", "1000 صنف", "5000 صنف", "عرض الكل (الجميع)" });
            cboMaxRows.SelectedIndex = 0;
            cboMaxRows.SelectedIndexChanged += (s, e) => LoadStock();

            chkBelowMin = new CheckBox
            {
                Text = "⚠️ حد الطلب",
                ForeColor = Color.FromArgb(180, 90, 0),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(6, 5, 6, 0),
                RightToLeft = RightToLeft.Yes
            };
            chkBelowMin.CheckedChanged += (s, e) => LoadStock();

            chkHideZeroStock = new CheckBox
            {
                Text = "🚫 بدون رصيد صفري",
                ForeColor = Color.FromArgb(0, 102, 204),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(6, 5, 6, 0),
                RightToLeft = RightToLeft.Yes,
                Checked = false
            };
            chkHideZeroStock.CheckedChanged += (s, e) => LoadStock();

            chkExpiryOnly = new CheckBox
            {
                Text = "📗 صلاحية فقط",
                ForeColor = Color.FromArgb(0, 130, 50),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(6, 5, 6, 0),
                RightToLeft = RightToLeft.Yes,
                Checked = false
            };
            chkExpiryOnly.CheckedChanged += (s, e) => LoadStock();

            chkScaleOnly = new CheckBox
            {
                Text = "⚖️ أصناف الميزان",
                ForeColor = Color.FromArgb(120, 40, 180),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(6, 5, 6, 0),
                RightToLeft = RightToLeft.Yes,
                Checked = false
            };
            chkScaleOnly.CheckedChanged += (s, e) => LoadStock();

            chkUninventoriedOnly = new CheckBox
            {
                Text = "⏳ لم تُجرد بعد",
                ForeColor = Color.FromArgb(180, 50, 50),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(6, 5, 6, 0),
                RightToLeft = RightToLeft.Yes,
                Checked = false,
                Enabled = true
            };
            chkUninventoriedOnly.CheckedChanged += (s, e) => LoadStock();

            // الصف 1: المخزن، التصنيف، المكان، البحث
            pnlRow1.Controls.AddRange(new Control[] {
                lblWh, cboWarehouse,
                lblCat, cboCategory,
                lblLoc, cboLocation,
                lblSch, txtSearch, btnSearch
            });

            // ── الصف 2: عدد العرض + خيارات التصفية ──────────────────────────────
            var lblLimit2 = new Label { Text = "عرض:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(4, 6, 2, 0) };

            // ── قائمة نوع السعر ───────────────────────────────────────────────────
            var lblPriceType = new Label
            {
                Text = "💰 الجرد بسعر:",
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 100, 50),
                Font = Theme.FontBold,
                Margin = new Padding(10, 6, 2, 0)
            };
            cboPriceType = new ComboBox
            {
                Name = "cboPriceType",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 110,
                BackColor = Color.FromArgb(240, 255, 240),
                ForeColor = Color.FromArgb(0, 90, 40),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 3, 8, 0),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold)
            };
            cboPriceType.Items.Add("قطاعي");
            cboPriceType.Items.Add("نص جملة");
            cboPriceType.Items.Add("جملة");
            cboPriceType.SelectedIndex = 0;
            cboPriceType.SelectedIndexChanged += (s, e) => LoadStock();

            pnlRow2.Controls.AddRange(new Control[] {
                lblLimit2, cboMaxRows,
                chkBelowMin, chkHideZeroStock, chkExpiryOnly, chkScaleOnly, chkUninventoriedOnly,
                lblPriceType, cboPriceType
            });

            // ── الصف 3: أزرار العمليات ─────────────────────────────────────────
            btnStartInventory = Theme.MakeButton("🚀 بدء جرد جديد", Color.FromArgb(140, 80, 20));
            btnStartInventory.Size = new Size(120, 28);
            btnStartInventory.Margin = new Padding(2, 2, 6, 0);
            btnStartInventory.Click += BtnStartInventory_Click;

            lblInventoryStart = new Label
            {
                Text = "📅 بدء الجرد: غير محدد",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 70, 0),
                BackColor = Color.FromArgb(254, 243, 199),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5, 3, 5, 3),
                Margin = new Padding(2, 4, 12, 0)
            };

            btnSaveAdj = Theme.MakeButton("💾 حفظ كل", Theme.Accent);
            btnSaveAdj.Size = new Size(90, 28);
            btnSaveAdj.Margin = new Padding(2, 2, 6, 0);
            btnSaveAdj.Click += BtnSaveAdj_Click;

            btnClearAdj = Theme.MakeButton("❌ إلغاء", Color.FromArgb(140, 40, 40));
            btnClearAdj.Size = new Size(72, 28);
            btnClearAdj.Margin = new Padding(2, 2, 6, 0);
            btnClearAdj.Click += (s, e) => ClearAdjustmentForm();

            var btnScaleReport = Theme.MakeButton("⚖️ أصناف الميزان", Color.FromArgb(90, 40, 160));
            btnScaleReport.Size = new Size(125, 28);
            btnScaleReport.Margin = new Padding(2, 2, 6, 0);
            btnScaleReport.Click += (s, e) => PrintScaleProductsReport();

            var btnVarianceReport = Theme.MakeButton("📊 تقرير فروق", Color.FromArgb(120, 50, 150));
            btnVarianceReport.Size = new Size(105, 28);
            btnVarianceReport.Margin = new Padding(2, 2, 6, 0);
            btnVarianceReport.Click += (s, e) => new FrmInventoryVarianceReport().ShowDialog();

            btnPrintStock = Theme.MakeButton("🖨 طباعة", Color.FromArgb(200, 100, 0));
            btnPrintStock.Size = new Size(72, 28);
            btnPrintStock.Margin = new Padding(2, 2, 6, 0);
            btnPrintStock.Click += (s, e) => PrintStocktakeReport();

            btnMovement = Theme.MakeButton("📜 كشف", Theme.Primary);
            btnMovement.Size = new Size(72, 28);
            btnMovement.Margin = new Padding(2, 2, 6, 0);
            btnMovement.Click += BtnMovement_Click;

            btnAddExpiryRow = Theme.MakeButton("➕ إضافة صلاحية", Color.FromArgb(40, 120, 60));
            btnAddExpiryRow.Size = new Size(115, 28);
            btnAddExpiryRow.Margin = new Padding(2, 2, 6, 0);
            btnAddExpiryRow.Click += BtnAddExpiryRow_Click;
            btnAddExpiryRow.Enabled = false;

            pnlRow3.Controls.AddRange(new Control[] {
                btnStartInventory, lblInventoryStart,
                btnSaveAdj, btnClearAdj,
                btnScaleReport, btnVarianceReport, btnPrintStock, btnMovement, btnAddExpiryRow
            });

            pnlHeaderContainer.Controls.Add(pnlRow3);
            pnlHeaderContainer.Controls.Add(pnlRow2);
            pnlHeaderContainer.Controls.Add(pnlRow1);

            // ── شبكة الجرد ─────────────────────────────────
            dgStock = MakeGrid();
            Theme.EnableDoubleBuffer(dgStock);
            dgStock.ReadOnly = false;
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID",     Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BatchID",       Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode",   HeaderText = "كود الصنف", ReadOnly = true,  FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScalePLU",      HeaderText = "كود الميزان", ReadOnly = true,  FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName",   HeaderText = "اسم الصنف",  ReadOnly = true,  FillWeight = 85 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLocation", HeaderText = "المكان/الرف", ReadOnly = true,  FillWeight = 45 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate",    HeaderText = "تاريخ الصلاحية", ReadOnly = false, FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",          HeaderText = "الوحدة 🔽", ReadOnly = true,  FillWeight = 42 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", HeaderText = "سعر الشراء", ReadOnly = false, FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice",   HeaderText = "سعر البيع", ReadOnly = false,  FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty",     HeaderText = "الرصيد الدفتري", ReadOnly = true,  FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualQty",   HeaderText = "الرصيد الفعلي", ReadOnly = false, FillWeight = 55, DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(255, 255, 225), ForeColor = Color.FromArgb(80, 50, 0) } });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffQty",     HeaderText = "الفارق",       ReadOnly = true,  FillWeight = 48 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",       HeaderText = "ملاحظات",      ReadOnly = false, FillWeight = 70 });

            // أعمدة مخفية للوحدات
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseUnit",          Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit1Name",         Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Name",         Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Factor",       Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit3Factor",       Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentFactor",     Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseBookQty",           Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BasePurchasePrice",     Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseSalePrice",         Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseWholesalePrice",    Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseSemiWholesalePrice",Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "HasExpiry",             Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "DefaultExpiryDays",     Visible = false });
            Theme.AdjustGridHeaders(dgStock);

            // ── قائمة القائمة اليمنى التفاعلية ─────────────────────────────
            var ctxStock = new ContextMenuStrip();

            var itemMark = new ToolStripMenuItem("✅ تمييز تم جرده (مطابق للدفتري)");
            itemMark.Click += (s, e) => MarkSelectedRowAsInventoried();

            var itemHide = new ToolStripMenuItem("🗑️ إخفاء الصنف من العرض لجرده لاحقاً");
            itemHide.Click += (s, e) => HideSelectedRowForLater();

            var itemCard = new ToolStripMenuItem("🏷️ فتح كارت الصنف");
            itemCard.Click += (s, e) => OpenSelectedProductCard();

            ctxStock.Items.AddRange(new ToolStripItem[] { itemMark, itemHide, new ToolStripSeparator(), itemCard });
            dgStock.ContextMenuStrip = ctxStock;

            dgStock.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.RowIndex < dgStock.Rows.Count)
                {
                    dgStock.ClearSelection();
                    dgStock.Rows[e.RowIndex].Selected = true;
                }
            };

            dgStock.CellEndEdit += DgStock_CellEndEdit;
            dgStock.SelectionChanged += DgStock_SelectionChanged;
            dgStock.CellClick += DgStock_CellClick;
            dgStock.CellDoubleClick += (s, e) => { if (e.ColumnIndex >= 0 && dgStock.Columns[e.ColumnIndex].Name != "ActualQty" && dgStock.Columns[e.ColumnIndex].Name != "Notes" && dgStock.Columns[e.ColumnIndex].Name != "ExpiryDate" && dgStock.Columns[e.ColumnIndex].Name != "Unit") BtnMovement_Click(s, e); };
            dgStock.CellFormatting += DgStock_CellFormatting;

            var pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(235, 240, 246),
                Padding = new Padding(10, 5, 10, 5),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight
            };

            lblCount = new Label
            {
                Text = "📦 الأصناف المعروضة: 0 صنف",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Margin = new Padding(5, 2, 25, 0)
            };

            lblTotalCost = new Label
            {
                Text = "💰 إجمالي التكلفة (الشراء): 0.00 ج",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 50, 0),
                Margin = new Padding(5, 2, 25, 0)
            };

            lblTotalSale = new Label
            {
                Text = "🏷️ إجمالي قيمة البيع: 0.00 ج",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 180),
                Margin = new Padding(5, 2, 10, 0)
            };

            pnlSummary.Controls.AddRange(new Control[] { lblCount, lblTotalCost, lblTotalSale });

            pnlLeft.Controls.Add(dgStock);    // Fill
            pnlLeft.Controls.Add(pnlSummary); // Top (below header)
            pnlLeft.Controls.Add(pnlHeaderContainer); // Top (Header 2 rows)

            tabStock.Controls.Add(pnlLeft);
        }

        private void BuildLogsTab()
        {
            var pnlTop = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Top, 
                Height = 55, 
                BackColor = Theme.BgCard, 
                Padding = new Padding(10, 10, 10, 10),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight
            };
            
            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 5, 0) };
            dtpFrom = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            
            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 5, 0) };
            dtpTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var lblLogWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 5, 0) };
            cboLogWarehouse = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            cboLogWarehouse.SelectedIndexChanged += (s, e) => LoadLogs();

            var lblSearchLog = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 5, 0) };
            txtSearchLog = new TextBox { Width = 140 };
            txtSearchLog.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadLogs(); };

            btnLoadLogs = Theme.MakeButton("🔍 عرض السجل", Color.FromArgb(60, 100, 60));
            btnLoadLogs.Size = new Size(110, 30);
            btnLoadLogs.Click += (s, e) => LoadLogs();

            btnPrintLogs = Theme.MakeButton("🖨 طباعة السجل", Theme.Accent);
            btnPrintLogs.Size = new Size(110, 30);
            btnPrintLogs.Click += (s, e) => PrintAdjustmentsLog();

            var btnVarianceReportLogs = Theme.MakeButton("📊 تقرير فروق الجرد المالي", Color.FromArgb(120, 50, 150));
            btnVarianceReportLogs.Size = new Size(180, 30);
            btnVarianceReportLogs.Click += (s, e) => new FrmInventoryVarianceReport().ShowDialog();

            pnlTop.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblLogWh, cboLogWarehouse, lblSearchLog, txtSearchLog, btnLoadLogs, btnPrintLogs, btnVarianceReportLogs });
            tabLogs.Controls.Add(pnlTop);

            dgLogs = MakeGrid();
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "AdjDate", HeaderText = "التاريخ والوقت", FillWeight = 50 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الكود", FillWeight = 30 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 80 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 30 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty", HeaderText = "الرصيد الدفتري", FillWeight = 40 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualQty", HeaderText = "الرصيد الفعلي", FillWeight = 40 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffQty", HeaderText = "الفارق", FillWeight = 35 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات التسوية", FillWeight = 70 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy", HeaderText = "بواسطة", FillWeight = 50 });
            Theme.AdjustGridHeaders(dgLogs);
            tabLogs.Controls.Add(dgLogs);
            
            pnlTop.BringToFront();
            dgLogs.BringToFront();
        }

        private Color GetInventoriedRowColor()
        {
            return AppConfig.AppTheme switch
            {
                "Light" => Color.FromArgb(170, 235, 180), // لون أخضر أوضح وبارز جداً لتأكيد جرد الصنف
                "Slate" => Color.FromArgb(145, 215, 160),
                _       => Color.FromArgb(20, 95, 45)    // أخضر زمردي غني في الداكن
            };
        }

        private void LoadStock()
        {
            if (dgStock == null) return;
            dgStock.SuspendLayout();
            var oldMode = dgStock.AutoSizeColumnsMode;
            dgStock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgStock.Rows.Clear();
            int? wid = null;
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
                wid = ci.ID;

            bool hideZero = chkHideZeroStock != null && chkHideZeroStock.Checked;
            bool expOnly  = chkExpiryOnly  != null && chkExpiryOnly.Checked;
            if (dgStock.Columns.Contains("ExpiryDate"))
            {
                dgStock.Columns["ExpiryDate"].Visible = expOnly;
            }
            int? catId = null;
            if (cboCategory != null && cboCategory.SelectedItem is ComboItem catCi && catCi.ID > 0)
                catId = catCi.ID;

            // ── حساب الحد الأقصى للعرض أولاً لتمريره لـ SQL ───────
            int maxDisplay = 300;
            if (cboMaxRows != null)
            {
                string sel = cboMaxRows.SelectedItem?.ToString() ?? "";
                if (sel.Contains("5000")) maxDisplay = 5000;
                else if (sel.Contains("1000")) maxDisplay = 1000;
                else if (sel.Contains("500")) maxDisplay = 500;
                else if (sel.Contains("الكل") || sel.Contains("الجميع")) maxDisplay = int.MaxValue;
                else maxDisplay = 300;
            }

            // ── تحديث حالة تاريخ بداية دورة الجرد ──────────────────
            _inventoryStartDate = InventoryDAL.GetInventoryStartDate(wid);
            if (!_inventoryStartDate.HasValue && wid.HasValue)
            {
                // محاولة الجلب العام لكل المخازن
                _inventoryStartDate = InventoryDAL.GetInventoryStartDate(null);
            }

            if (_inventoryStartDate.HasValue)
            {
                if (lblInventoryStart != null)
                    lblInventoryStart.Text = $"📅 بدء الجرد: {_inventoryStartDate.Value:dd/MM/yyyy HH:mm}";
                _inventoriedProductIDs = InventoryDAL.GetInventoriedProductIDs(_inventoryStartDate.Value, wid);
                foreach (var kv in _enteredActualQty)
                    _inventoriedProductIDs.Add(kv.Key);
            }
            else
            {
                if (lblInventoryStart != null)
                    lblInventoryStart.Text = "📅 تاريخ بدء الجرد: (لم يبدأ - انقر بدء جرد جديد)";
                _inventoriedProductIDs.Clear();
            }

            if (chkUninventoriedOnly != null)
            {
                chkUninventoriedOnly.Enabled = true; // متاح دائماً للاختيار
            }

            bool showUninventoriedOnly = chkUninventoriedOnly != null && chkUninventoriedOnly.Checked;
            if (showUninventoriedOnly && !_inventoryStartDate.HasValue)
            {
                // إذا تم تفعيل الخيار ولم يحدد تاريخ بدء سابقاً -> الاعتماد الافتراضي اليوم
                _inventoryStartDate = DateTime.Today;
                if (lblInventoryStart != null)
                    lblInventoryStart.Text = $"📅 بدء الجرد: {_inventoryStartDate.Value:dd/MM/yyyy 00:00}";
                _inventoriedProductIDs = InventoryDAL.GetInventoriedProductIDs(_inventoryStartDate.Value, wid);
                foreach (var kv in _enteredActualQty)
                    _inventoriedProductIDs.Add(kv.Key);
            }

            int queryMaxRows = (maxDisplay == int.MaxValue) ? 100000 : maxDisplay;
            string selectedLocation = (cboLocation != null && cboLocation.SelectedIndex > 0) ? cboLocation.SelectedItem.ToString() : null;
            bool scaleOnly = chkScaleOnly != null && chkScaleOnly.Checked;
            var dt  = InventoryDAL.GetStock(wid, txtSearch.Text, chkBelowMin != null && chkBelowMin.Checked, hideZero, expOnly, catId, maxRows: queryMaxRows, location: selectedLocation, scaleOnly: scaleOnly);
            var inv = System.Globalization.CultureInfo.InvariantCulture;

            int displayedCount = 0;
            decimal totalCost = 0m;
            decimal totalSale = 0m;

            foreach (DataRow r in dt.Rows)
            {
                if (displayedCount >= maxDisplay) break;
                int pidCheck = Convert.ToInt32(r["ProductID"]);
                // استبعاد الأصناف المخفية مؤقتاً للجرد لاحقاً
                if (_hiddenProductIDs.Contains(pidCheck)) continue;
                // تطبيق فلتر الأصناف التي لم تُجرد بعد
                if (showUninventoriedOnly && _inventoriedProductIDs.Contains(pidCheck)) continue;
                displayedCount++;
                decimal baseBookQty = Convert.ToDecimal(r["BookQty"]);
                decimal basePP = r["PurchasePrice"]     != DBNull.Value ? Convert.ToDecimal(r["PurchasePrice"])     : 0m;
                decimal baseSP = r["SalePrice"]          != DBNull.Value ? Convert.ToDecimal(r["SalePrice"])          : 0m;
                decimal baseWP = r.Table.Columns.Contains("WholesalePrice")     && r["WholesalePrice"]     != DBNull.Value ? Convert.ToDecimal(r["WholesalePrice"])     : baseSP;
                decimal baseSWP= r.Table.Columns.Contains("SemiWholesalePrice") && r["SemiWholesalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SemiWholesalePrice"]) : baseSP;

                // اختيار السعر بناءً على القائمة المنسدلة
                int priceTypeIdx = (cboPriceType != null && cboPriceType.SelectedIndex >= 0) ? cboPriceType.SelectedIndex : 0;
                // 0 = قطاعي (SalePrice), 1 = نص جملة (SemiWholesalePrice), 2 = جملة (WholesalePrice)
                decimal selectedBaseSP = priceTypeIdx == 2 ? baseWP : (priceTypeIdx == 1 ? baseSWP : baseSP);

                totalCost += (baseBookQty * basePP);
                totalSale += (baseBookQty * selectedBaseSP);
                int pid = Convert.ToInt32(r["ProductID"]);

                string baseUnit = r["Unit"] != DBNull.Value ? r["Unit"].ToString() : "";
                string unit1    = r["Unit1Name"] != DBNull.Value ? r["Unit1Name"].ToString() : "";
                string unit2    = r["Unit2Name"] != DBNull.Value ? r["Unit2Name"].ToString() : "";

                decimal u2Factor = r["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit2Factor"]) : 1m;
                decimal u3Factor = r["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit3Factor"]) : 1m;

                decimal f2 = u2Factor > 0 ? u2Factor : 1m;
                decimal f3 = u3Factor > 0 ? u3Factor : 1m;
                decimal factor1 = f2 * f3;

                string displayUnit = baseUnit;
                decimal curFactor = 1.0m;

                if (!string.IsNullOrWhiteSpace(unit1))
                {
                    displayUnit = unit1;
                    curFactor = factor1;
                }

                bool hasMultiUnits = !string.IsNullOrWhiteSpace(unit1) || !string.IsNullOrWhiteSpace(unit2);
                string unitCellText = displayUnit + (hasMultiUnits ? " 🔽" : "");

                decimal displayedBookQty = baseBookQty / (curFactor > 0 ? curFactor : 1m);
                decimal displayedPP  = basePP           * curFactor;
                decimal displayedSP  = selectedBaseSP   * curFactor; // السعر المحدد من القائمة
                decimal displayedWP  = baseWP            * curFactor;
                decimal displayedSWP = baseSWP           * curFactor;

                string actualVal = "";
                string diffVal   = "";
                if (_enteredActualQty.TryGetValue(pid, out decimal savedActual))
                {
                    actualVal = savedActual.ToString("N3");
                    decimal diff = savedActual - displayedBookQty;
                    diffVal = (diff > 0 ? "+" : "") + diff.ToString("N3");
                }

                string expiryVal = "";
                if (dt.Columns.Contains("ExpiryDate") && r["ExpiryDate"] != DBNull.Value)
                {
                    DateTime expDt = Convert.ToDateTime(r["ExpiryDate"]);
                    expiryVal = expDt.ToString("yyyy-MM-dd");
                }
                object batchIdVal = dt.Columns.Contains("BatchID") && r["BatchID"] != DBNull.Value
                    ? r["BatchID"]
                    : (object)DBNull.Value;

                string shelfLoc = (dt.Columns.Contains("ShelfLocation") && r["ShelfLocation"] != DBNull.Value) ? r["ShelfLocation"].ToString() : "---";
                string scalePlu = (dt.Columns.Contains("ScalePLU") && r["ScalePLU"] != DBNull.Value) ? r["ScalePLU"].ToString() : "";

                int ri = dgStock.Rows.Add(
                    r["ProductID"],
                    batchIdVal,
                    r["ProductCode"],
                    scalePlu,
                    r["ProductName"],
                    shelfLoc,
                    expiryVal,
                    unitCellText,
                    displayedPP.ToString("N2"),
                    displayedSP.ToString("N2"),
                    displayedBookQty.ToString("N3"),
                    actualVal,
                    diffVal,
                    "" // Notes
                );

                dgStock.Rows[ri].Cells["PurchasePrice"].Tag = displayedPP;
                dgStock.Rows[ri].Cells["SalePrice"].Tag = displayedSP;

                dgStock.Rows[ri].Cells["BaseUnit"].Value          = baseUnit;
                dgStock.Rows[ri].Cells["Unit1Name"].Value         = unit1;
                dgStock.Rows[ri].Cells["Unit2Name"].Value         = unit2;
                dgStock.Rows[ri].Cells["Unit2Factor"].Value       = u2Factor;
                dgStock.Rows[ri].Cells["Unit3Factor"].Value       = u3Factor;
                dgStock.Rows[ri].Cells["CurrentFactor"].Value     = curFactor;
                dgStock.Rows[ri].Cells["BaseBookQty"].Value            = baseBookQty;
                dgStock.Rows[ri].Cells["BasePurchasePrice"].Value        = basePP;
                dgStock.Rows[ri].Cells["BaseSalePrice"].Value            = baseSP;
                dgStock.Rows[ri].Cells["BaseWholesalePrice"].Value       = baseWP;
                dgStock.Rows[ri].Cells["BaseSemiWholesalePrice"].Value   = baseSWP;
                dgStock.Rows[ri].Cells["HasExpiry"].Value                = r["HasExpiry"];
                dgStock.Rows[ri].Cells["DefaultExpiryDays"].Value        = r["DefaultExpiryDays"];

                // تمييز لون السطر والأصناف المجرودة باللون الأخضر الناصع الجلي
                bool isInventoried = _enteredActualQty.ContainsKey(pid) || _inventoriedProductIDs.Contains(pid);
                if (isInventoried)
                {
                    Color invColor = GetInventoriedRowColor();
                    dgStock.Rows[ri].DefaultCellStyle.BackColor = invColor;
                    dgStock.Rows[ri].Cells["ActualQty"].Style.BackColor = invColor;
                }

                if (!string.IsNullOrEmpty(diffVal))
                {
                    decimal diff2 = savedActual - displayedBookQty;
                    dgStock.Rows[ri].Cells["DiffQty"].Style.ForeColor = diff2 > 0 ? Color.DarkGreen : Color.OrangeRed;
                }
            }

            if (lblCount != null) lblCount.Text = $"📦 الأصناف المعروضة: {displayedCount:N0} صنف";
            if (lblTotalCost != null) lblTotalCost.Text = $"💰 إجمالي قيمة التكلفة (الشراء): {totalCost:N2} ج";
            if (lblTotalSale != null) lblTotalSale.Text = $"🏷️ إجمالي قيمة البيع: {totalSale:N2} ج";

            dgStock.AutoSizeColumnsMode = oldMode;
            dgStock.ResumeLayout();
            ClearAdjustmentForm();
        }

        private void LoadLogs()
        {
            dgLogs.Rows.Clear();
            int? wid = null;
            if (cboLogWarehouse != null && cboLogWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                wid = ci.ID;
            }
            var dt = InventoryDAL.GetAdjustments(dtpFrom.Value, dtpTo.Value, wid, txtSearchLog.Text);
            foreach (DataRow r in dt.Rows)
            {
                decimal diff = Convert.ToDecimal(r["DiffQty"]);
                int ri = dgLogs.Rows.Add(
                    Convert.ToDateTime(r["AdjDate"]).ToString("dd/MM/yyyy HH:mm"),
                    r["ProductCode"],
                    r["ProductName"],
                    r["Unit"],
                    Convert.ToDecimal(r["BookQty"]).ToString("N3"),
                    Convert.ToDecimal(r["ActualQty"]).ToString("N3"),
                    (diff > 0 ? "+" : "") + diff.ToString("N3"),
                    r["Notes"],
                    r["CreatedBy"]
                );

                if (diff > 0)
                    dgLogs.Rows[ri].Cells["DiffQty"].Style.ForeColor = Color.LightGreen;
                else if (diff < 0)
                    dgLogs.Rows[ri].Cells["DiffQty"].Style.ForeColor = Color.OrangeRed;
            }
        }

        private void BtnStartInventory_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("Inventory")) { MessageBox.Show("⛔ ليس لديك صلاحية بدء جرد جديد.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int? wid = null;
            string whName = "كل المخازن";
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                wid = ci.ID;
                whName = ci.Text;
            }

            var confirm = MessageBox.Show(
                $"هل ترغب في بدء عملية جرد جديدة لـ ({whName}) ابتداءً من الآن ({DateTime.Now:dd/MM/yyyy HH:mm})؟\n\n" +
                "سيتم اعتماد هذا التاريخ لبدء الجرد الجديد وتحديد الأصناف التي تم/لم يتم جردها بعد.",
                "تأكيد بدء جرد جديد",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

            if (confirm == DialogResult.Yes)
            {
                DateTime now = DateTime.Now;
                InventoryDAL.SetInventoryStartDate(wid, now);
                _enteredActualQty.Clear();
                if (chkUninventoriedOnly != null) chkUninventoriedOnly.Checked = false;
                LoadStock();
                Theme.ShowMsg($"تم بدء عملية جرد جديدة لـ ({whName}) بنجاح!\nالتاريخ: {now:dd/MM/yyyy HH:mm}", "بدء الجرد");
            }
        }

        private void DgStock_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Row background colors are set once in LoadStock and CellEndEdit to avoid lag and repaint cascades
        }

        private void DgStock_SelectionChanged(object sender, EventArgs e)
        {
            if (dgStock.SelectedRows.Count == 0) return;
            var r = dgStock.SelectedRows[0];
            _selectedProductID   = Convert.ToInt32(r.Cells["ProductID"].Value);
            _selectedProductName = r.Cells["ProductName"].Value?.ToString();
            _selectedProductUnit = r.Cells["Unit"].Value?.ToString();
            _selectedHasExpiry   = r.Cells["HasExpiry"].Value != DBNull.Value && Convert.ToBoolean(r.Cells["HasExpiry"].Value);
            _selectedDefaultExpiryDays = r.Cells["DefaultExpiryDays"].Value != DBNull.Value ? Convert.ToInt32(r.Cells["DefaultExpiryDays"].Value) : (int?)null;
            _selectedBookQty = decimal.TryParse(r.Cells["BookQty"].Value?.ToString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal bq) ? bq : 0;
            if (btnAddExpiryRow != null)
                btnAddExpiryRow.Enabled = _selectedHasExpiry && _selectedProductID > 0;
        }

        private void DgStock_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgStock.Rows[e.RowIndex];
            if (dgStock.Columns[e.ColumnIndex].Name == "ActualQty")
            {
                var inv       = System.Globalization.CultureInfo.InvariantCulture;
                var numStyles = System.Globalization.NumberStyles.Any;
                decimal.TryParse(row.Cells["BookQty"].Value?.ToString(), numStyles, inv, out decimal bookQty);
                string cellText = row.Cells["ActualQty"].Value?.ToString();
                int rowPid = Convert.ToInt32(row.Cells["ProductID"].Value);

                if (!string.IsNullOrWhiteSpace(cellText) &&
                    decimal.TryParse(cellText, numStyles, inv, out decimal actualQty))
                {
                    decimal diff = actualQty - bookQty;
                    row.Cells["DiffQty"].Value = (diff > 0 ? "+" : "") + diff.ToString("N3");
                    row.Cells["DiffQty"].Style.ForeColor = diff > 0 ? Color.DarkGreen : (diff < 0 ? Color.OrangeRed : Theme.TextMain);
                    if (rowPid > 0)
                    {
                        _enteredActualQty[rowPid] = actualQty;
                        _inventoriedProductIDs.Add(rowPid);
                    }
                    Color invColor = GetInventoriedRowColor();
                    row.DefaultCellStyle.BackColor = invColor;
                    row.Cells["ActualQty"].Style.BackColor = invColor;
                    dgStock.InvalidateRow(e.RowIndex);
                }
                else
                {
                    row.Cells["ActualQty"].Value = "";
                    row.Cells["DiffQty"].Value   = "";
                    row.Cells["DiffQty"].Style.ForeColor = Theme.TextMain;
                    if (rowPid > 0) _enteredActualQty.Remove(rowPid);
                    row.DefaultCellStyle.BackColor = Color.Empty;
                    row.Cells["ActualQty"].Style.BackColor = Color.FromArgb(255, 255, 225);
                    dgStock.InvalidateRow(e.RowIndex);
                }
            }
            else if (dgStock.Columns[e.ColumnIndex].Name == "ExpiryDate")
            {
                var cell = row.Cells["ExpiryDate"];
                string val = cell.Value?.ToString();
                var parsed = DbHelper.ParseExpiryInput(val);
                if (parsed.HasValue)
                {
                    cell.Value = parsed.Value.ToString("yyyy-MM-dd");
                }
                else if (string.IsNullOrWhiteSpace(val))
                {
                    cell.Value = "";
                }
                else
                {
                    MessageBox.Show("تاريخ غير صالح. يرجى إدخال التاريخ بالصيغة الصحيحة (شهر وسنة مثل 0326 أو yyyy-MM-dd).", "تاريخ غير صالح", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cell.Value = "";
                }
            }
        }

        private void DgStock_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgStock.Columns[e.ColumnIndex].Name == "Unit")
            {
                OpenUnitMenuForRow(e.RowIndex);
            }
        }

        private void OpenUnitMenuForRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgStock.Rows.Count) return;
            var row = dgStock.Rows[rowIndex];

            string baseUnit = row.Cells["BaseUnit"].Value?.ToString() ?? "";
            string unit1 = row.Cells["Unit1Name"].Value?.ToString() ?? "";
            string unit2 = row.Cells["Unit2Name"].Value?.ToString() ?? "";

            decimal u2Factor = row.Cells["Unit2Factor"].Value != DBNull.Value ? Convert.ToDecimal(row.Cells["Unit2Factor"].Value) : 1m;
            decimal u3Factor = row.Cells["Unit3Factor"].Value != DBNull.Value ? Convert.ToDecimal(row.Cells["Unit3Factor"].Value) : 1m;

            decimal f2 = u2Factor > 0 ? u2Factor : 1m;
            decimal f3 = u3Factor > 0 ? u3Factor : 1m;
            decimal factor1 = f2 * f3;
            decimal factor2 = f3;

            var menu = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

            if (!string.IsNullOrWhiteSpace(unit1))
            {
                var mi1 = menu.Items.Add($"📦 {unit1} (معامل: {factor1:G29})");
                mi1.Click += (s, e) => SwitchRowUnit(row, unit1, factor1);
            }

            if (!string.IsNullOrWhiteSpace(unit2) && !string.Equals(unit2, unit1, StringComparison.OrdinalIgnoreCase))
            {
                var mi2 = menu.Items.Add($"📦 {unit2} (معامل: {factor2:G29})");
                mi2.Click += (s, e) => SwitchRowUnit(row, unit2, factor2);
            }

            if (!string.IsNullOrWhiteSpace(baseUnit) && !string.Equals(baseUnit, unit1, StringComparison.OrdinalIgnoreCase) && !string.Equals(baseUnit, unit2, StringComparison.OrdinalIgnoreCase))
            {
                var miBase = menu.Items.Add($"📦 {baseUnit} (معامل: 1)");
                miBase.Click += (s, e) => SwitchRowUnit(row, baseUnit, 1.0m);
            }

            if (menu.Items.Count > 0)
            {
                Point pt = dgStock.PointToClient(Cursor.Position);
                menu.Show(dgStock, pt);
            }
        }

        private void SwitchRowUnit(DataGridViewRow row, string selectedUnit, decimal factor)
        {
            if (row == null || factor <= 0) return;

            var numStyles = System.Globalization.NumberStyles.Any;
            var inv = System.Globalization.CultureInfo.InvariantCulture;

            decimal baseBookQty = row.Cells["BaseBookQty"].Value != DBNull.Value ? Convert.ToDecimal(row.Cells["BaseBookQty"].Value) : 0m;
            decimal basePP = row.Cells["BasePurchasePrice"].Value != DBNull.Value ? Convert.ToDecimal(row.Cells["BasePurchasePrice"].Value) : 0m;
            decimal baseSP = row.Cells["BaseSalePrice"].Value != DBNull.Value ? Convert.ToDecimal(row.Cells["BaseSalePrice"].Value) : 0m;

            decimal newBookQty = baseBookQty / factor;
            decimal newPP = basePP * factor;
            decimal newSP = baseSP * factor;

            row.Cells["Unit"].Value = selectedUnit + " 🔽";
            row.Cells["CurrentFactor"].Value = factor;
            row.Cells["BookQty"].Value = newBookQty.ToString("N3");
            row.Cells["PurchasePrice"].Value = newPP.ToString("N2");
            row.Cells["SalePrice"].Value = newSP.ToString("N2");

            row.Cells["PurchasePrice"].Tag = newPP;
            row.Cells["SalePrice"].Tag = newSP;

            // Recalculate ActualQty & DiffQty
            string actualText = row.Cells["ActualQty"].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(actualText) && decimal.TryParse(actualText, numStyles, inv, out decimal actualVal))
            {
                decimal diff = actualVal - newBookQty;
                row.Cells["DiffQty"].Value = (diff > 0 ? "+" : "") + diff.ToString("N3");
                row.Cells["DiffQty"].Style.ForeColor = diff > 0 ? Color.DarkGreen : (diff < 0 ? Color.OrangeRed : Theme.TextMain);
            }
        }

        private void ClearAdjustmentForm()
        {
            _selectedProductID   = 0;
            _selectedBookQty     = 0;
            _selectedProductName = "";
            _selectedProductUnit = "";
            if (btnAddExpiryRow != null) btnAddExpiryRow.Enabled = false;
        }


        private void BtnSaveAdj_Click(object sender, EventArgs e)
        {
            if (!Session.CanEdit("Inventory")) { MessageBox.Show("⛔ ليس لديك صلاحية حفظ تسوية الجرد.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int? selectedWid = null;
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                selectedWid = ci.ID;
            }

            if (!selectedWid.HasValue)
            {
                MessageBox.Show("من فضلك اختر مستودعاً محدداً أولاً لإجراء التسوية الجردية فيه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int wid = selectedWid.Value;

            // 1. تجميع كافة الأصناف التي تم تعديل كميتها أو أسعارها
            var modifiedRows = new System.Collections.Generic.List<DataGridViewRow>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var numStyles = System.Globalization.NumberStyles.Any;

            foreach (DataGridViewRow row in dgStock.Rows)
            {
                if (row.Cells["ProductID"].Value == null) continue;

                bool isQtyModified = false;
                bool isPriceModified = false;

                string cellVal = row.Cells["ActualQty"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(cellVal) && decimal.TryParse(cellVal, numStyles, inv, out _))
                {
                    isQtyModified = true;
                }

                // Check PurchasePrice
                string purVal = row.Cells["PurchasePrice"].Value?.ToString();
                if (decimal.TryParse(purVal, numStyles, inv, out decimal curPurPrice))
                {
                    decimal originalPur = row.Cells["PurchasePrice"].Tag != null ? (decimal)row.Cells["PurchasePrice"].Tag : 0m;
                    if (Math.Round(curPurPrice, 2) != Math.Round(originalPur, 2))
                    {
                        isPriceModified = true;
                    }
                }

                // Check SalePrice
                string saleVal = row.Cells["SalePrice"].Value?.ToString();
                if (decimal.TryParse(saleVal, numStyles, inv, out decimal curSalePrice))
                {
                    decimal originalSale = row.Cells["SalePrice"].Tag != null ? (decimal)row.Cells["SalePrice"].Tag : 0m;
                    if (Math.Round(curSalePrice, 2) != Math.Round(originalSale, 2))
                    {
                        isPriceModified = true;
                    }
                }

                if (isQtyModified || isPriceModified)
                {
                    modifiedRows.Add(row);
                }
            }

            if (modifiedRows.Count > 0)
            {
                string msg = "هل أنت متأكد من حفظ التعديلات التالية على الكميات أو الأسعار؟\n\n";
                int count = 0;
                foreach (var row in modifiedRows)
                {
                    string name = row.Cells["ProductName"].Value?.ToString();
                    msg += $"• {name}: ";

                    // Check Qty change
                    string actualQtyStr = row.Cells["ActualQty"].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(actualQtyStr) && decimal.TryParse(actualQtyStr, numStyles, inv, out decimal actual))
                    {
                        decimal.TryParse(row.Cells["BookQty"].Value?.ToString(), numStyles, inv, out decimal book);
                        decimal diff = actual - book;
                        msg += $"الكمية ({book:N3} ➔ {actual:N3}) ";
                    }

                    // Check PurchasePrice change
                    string purPriceStr = row.Cells["PurchasePrice"].Value?.ToString();
                    if (decimal.TryParse(purPriceStr, numStyles, inv, out decimal purPrice))
                    {
                        decimal originalPur = row.Cells["PurchasePrice"].Tag != null ? (decimal)row.Cells["PurchasePrice"].Tag : 0m;
                        if (Math.Round(purPrice, 2) != Math.Round(originalPur, 2))
                        {
                            msg += $"شراء ({originalPur:N2} ➔ {purPrice:N2}) ";
                        }
                    }

                    // Check SalePrice change
                    string salePriceStr = row.Cells["SalePrice"].Value?.ToString();
                    if (decimal.TryParse(salePriceStr, numStyles, inv, out decimal salePrice))
                    {
                        decimal originalSale = row.Cells["SalePrice"].Tag != null ? (decimal)row.Cells["SalePrice"].Tag : 0m;
                        if (Math.Round(salePrice, 2) != Math.Round(originalSale, 2))
                        {
                            msg += $"بيع ({originalSale:N2} ➔ {salePrice:N2}) ";
                        }
                    }

                    msg += "\n";
                    count++;
                    if (count >= 10)
                    {
                        if (modifiedRows.Count > 10)
                        {
                            msg += $"\n... وعدد {modifiedRows.Count - 10} أصناف أخرى.";
                        }
                        break;
                    }
                }

                if (MessageBox.Show(msg, "تأكيد حفظ التسويات والأسعار", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    int savedCount = 0;
                    try
                    {
                        DbHelper.RunInTransaction((con, trans) =>
                        {
                            foreach (var row in modifiedRows)
                            {
                                int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                                decimal factor = row.Cells["CurrentFactor"].Value != DBNull.Value ? Convert.ToDecimal(row.Cells["CurrentFactor"].Value) : 1.0m;
                                if (factor <= 0) factor = 1.0m;

                                string displayUnit = row.Cells["Unit"].Value?.ToString()?.Replace(" 🔽", "").Trim() ?? "";
                                string rowNotes    = row.Cells["Notes"].Value?.ToString() ?? "";
                                bool hasExpiry     = row.Cells["HasExpiry"].Value != DBNull.Value && Convert.ToBoolean(row.Cells["HasExpiry"].Value);

                                // ── 1. حفظ تعديلات الأسعار ──
                                string purPriceStr = row.Cells["PurchasePrice"].Value?.ToString();
                                string salePriceStr = row.Cells["SalePrice"].Value?.ToString();

                                if (decimal.TryParse(purPriceStr, numStyles, inv, out decimal purPrice))
                                {
                                    decimal originalPur = row.Cells["PurchasePrice"].Tag != null ? (decimal)row.Cells["PurchasePrice"].Tag : 0m;
                                    if (Math.Round(purPrice, 2) != Math.Round(originalPur, 2))
                                    {
                                        decimal basePurPrice = purPrice / factor;
                                        DbHelper.ExecuteTrans(trans, "UPDATE Products SET PurchasePrice = @pur WHERE ProductID = @pid",
                                            DbHelper.P("@pur", basePurPrice),
                                            DbHelper.P("@pid", pid));
                                    }
                                }

                                if (decimal.TryParse(salePriceStr, numStyles, inv, out decimal salePrice))
                                {
                                    decimal originalSale = row.Cells["SalePrice"].Tag != null ? (decimal)row.Cells["SalePrice"].Tag : 0m;
                                    if (Math.Round(salePrice, 2) != Math.Round(originalSale, 2))
                                    {
                                        decimal baseSalePrice = salePrice / factor;
                                        DbHelper.ExecuteTrans(trans, "UPDATE Products SET SalePrice = @sale WHERE ProductID = @pid",
                                            DbHelper.P("@sale", baseSalePrice),
                                            DbHelper.P("@pid", pid));

                                        DbHelper.ExecuteTrans(trans,
                                            @"INSERT INTO PriceChangesLog (ProductID, OldPrice, NewPrice, ChangeSource, SourceRefID, UserID, Notes)
                                              VALUES (@pid, @old, @new, 'InventoryAdjust', NULL, @uid, N'تعديل السعر من شاشة جرد وتعديل الأسعار')",
                                            DbHelper.P("@pid", pid),
                                            DbHelper.P("@old", originalSale / factor),
                                            DbHelper.P("@new", baseSalePrice),
                                            DbHelper.P("@uid", Session.EmpID));
                                    }
                                }

                                // ── 2. حفظ تسويات كميات الجرد ──
                                string actualQtyStr = row.Cells["ActualQty"].Value?.ToString();
                                if (!string.IsNullOrWhiteSpace(actualQtyStr) && decimal.TryParse(actualQtyStr, numStyles, inv, out decimal actualEntered))
                                {
                                    decimal baseActual = actualEntered * factor;
                                    decimal baseBook = row.Cells["BaseBookQty"].Value != DBNull.Value ? Convert.ToDecimal(row.Cells["BaseBookQty"].Value) : 0m;

                                    if (hasExpiry)
                                    {
                                        object batchIdVal = row.Cells["BatchID"].Value;
                                        string expStr = row.Cells["ExpiryDate"].Value?.ToString();
                                        DateTime? exp = null;
                                        if (!string.IsNullOrWhiteSpace(expStr) && DateTime.TryParse(expStr, out DateTime parsedExp))
                                            exp = parsedExp;

                                        if (batchIdVal != null && batchIdVal != DBNull.Value)
                                        {
                                            int bid = Convert.ToInt32(batchIdVal);
                                            DbHelper.ExecuteTrans(trans,
                                                "UPDATE ProductBatches SET ExpiryDate=@exp, Quantity=@qty WHERE BatchID=@bid",
                                                DbHelper.P("@exp", exp.HasValue ? (object)exp.Value : DBNull.Value),
                                                DbHelper.P("@qty", baseActual),
                                                DbHelper.P("@bid", bid));
                                        }
                                        else
                                        {
                                            DbHelper.ExecuteTrans(trans,
                                                "INSERT INTO ProductBatches (ProductID, WarehouseID, Quantity, ExpiryDate) VALUES (@pid, @wid, @qty, @exp)",
                                                DbHelper.P("@pid", pid),
                                                DbHelper.P("@wid", wid),
                                                DbHelper.P("@qty", baseActual),
                                                DbHelper.P("@exp", exp.HasValue ? (object)exp.Value : DBNull.Value));
                                        }

                                        // Sync ProductStock
                                        DbHelper.ExecuteTrans(trans,
                                            @"IF EXISTS (SELECT 1 FROM ProductStock WHERE ProductID=@pid AND WarehouseID=@wid)
                                                UPDATE ProductStock SET Quantity = (SELECT COALESCE(SUM(Quantity), 0) FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid) WHERE ProductID=@pid AND WarehouseID=@wid
                                              ELSE
                                                INSERT INTO ProductStock (ProductID, WarehouseID, Quantity) VALUES (@pid, @wid, (SELECT COALESCE(SUM(Quantity), 0) FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid))",
                                            DbHelper.P("@pid", pid),
                                            DbHelper.P("@wid", wid));

                                        // Log to StockAdjustments
                                        string expText = exp.HasValue ? exp.Value.ToString("yyyy-MM-dd") : "بدون";
                                        string logNotes = $"[تسوية صلاحية: {expText}] " + rowNotes;
                                        DbHelper.ExecuteTrans(trans,
                                            @"INSERT INTO StockAdjustments (ProductID, WarehouseID, BookQty, ActualQty, Notes, CreatedBy, UnitName, Factor)
                                              VALUES (@pid, @wid, @bq, @aq, @notes, @by, @un, @fac)",
                                            DbHelper.P("@pid", pid),
                                            DbHelper.P("@wid", wid),
                                            DbHelper.P("@bq", baseBook),
                                            DbHelper.P("@aq", baseActual),
                                            DbHelper.P("@notes", logNotes),
                                            DbHelper.P("@by", Session.EmpID),
                                            DbHelper.P("@un", displayUnit),
                                            DbHelper.P("@fac", factor));
                                    }
                                    else
                                    {
                                        // Normal adjustment
                                        DbHelper.ExecuteTrans(trans,
                                            @"INSERT INTO StockAdjustments (ProductID, WarehouseID, BookQty, ActualQty, Notes, CreatedBy, UnitName, Factor)
                                              VALUES (@pid, @wid, @bq, @aq, @notes, @by, @un, @fac)",
                                            DbHelper.P("@pid", pid),
                                            DbHelper.P("@wid", wid),
                                            DbHelper.P("@bq", baseBook),
                                            DbHelper.P("@aq", baseActual),
                                            DbHelper.P("@notes", rowNotes),
                                            DbHelper.P("@by", Session.EmpID),
                                            DbHelper.P("@un", displayUnit),
                                            DbHelper.P("@fac", factor));
                                    }

                                    _enteredActualQty.Remove(pid);
                                }

                                savedCount++;
                            }
                        });

                        if (savedCount > 0)
                        {
                            MessageBox.Show($"✅ تم حفظ وتطبيق التسوية الجردية لعدد ({savedCount}) أصناف بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadStock();
                            LoadLogs();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ فشل حفظ التعديلات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("لم يتم إدخال أي رصيد فعلي في الجدول بعد.\nاكتب الرصيد الفعلي في عمود «الرصيد الفعلي» لأي صنف ثم اضغط حفظ.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        private void BtnMovement_Click(object sender, EventArgs e)
        {
            if (dgStock.SelectedRows.Count == 0)
            {
                MessageBox.Show("من فضلك اختر صنفاً أولاً من الجدول لمشاهدة حركته.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var r = dgStock.SelectedRows[0];
            int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
            string name = r.Cells["ProductName"].Value?.ToString();
            string unit = r.Cells["Unit"].Value?.ToString();

            var frm = new FrmProductMovement(pid, name, unit);
            frm.ShowDialog();
        }

        private void MarkSelectedRowAsInventoried()
        {
            if (dgStock.SelectedRows.Count == 0) return;
            var r = dgStock.SelectedRows[0];
            if (r.Cells["ProductID"].Value == null) return;

            int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
            string bookQtyStr = r.Cells["BookQty"].Value?.ToString() ?? "0";
            r.Cells["ActualQty"].Value = bookQtyStr;
            r.Cells["DiffQty"].Value = "0.000";
            r.Cells["DiffQty"].Style.ForeColor = Theme.TextMain;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (decimal.TryParse(bookQtyStr, System.Globalization.NumberStyles.Any, inv, out decimal bq))
            {
                _enteredActualQty[pid] = bq;
                _inventoriedProductIDs.Add(pid);
            }
            dgStock.InvalidateRow(r.Index);
        }

        private void HideSelectedRowForLater()
        {
            if (dgStock.SelectedRows.Count == 0) return;
            var r = dgStock.SelectedRows[0];
            if (r.Cells["ProductID"].Value == null) return;

            int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
            string pName = r.Cells["ProductName"].Value?.ToString() ?? "";
            _hiddenProductIDs.Add(pid);
            dgStock.Rows.Remove(r);

            if (lblCount != null)
                lblCount.Text = $"الأصناف المعروضة: {dgStock.Rows.Count:N0}";

            Theme.ShowMsg($"تم إخفاء الصنف ({pName}) من العرض لجرده بوقت لاحق بنجاح.", "إخفاء صنف");
        }

        private void OpenSelectedProductCard()
        {
            if (dgStock.SelectedRows.Count == 0) return;
            var r = dgStock.SelectedRows[0];
            if (r.Cells["ProductID"].Value == null) return;

            int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
            try
            {
                new FrmProductCard(pid).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر فتح كارت الصنف:\n" + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int _printStockRowIndex = 0;
        private int _printStockPageNum = 1;

        private void PrintStocktakeReport()
        {
            _printStockRowIndex = 0;
            _printStockPageNum = 1;
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var boldBig = new Font("Arial", 16, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var boldHead = new Font("Arial", 9.5f, FontStyle.Bold);
                var normal = new Font("Arial", 9.5f, FontStyle.Regular);
                var small = new Font("Arial", 8.5f, FontStyle.Regular);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRtlRight = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap };
                var sfRtlCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                int startX = 23;
                int pageW = 780;
                int y = 25;

                // 1. Header & Title
                string company = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
                g.DrawString(company, bold, Brushes.DarkBlue, new RectangleF(startX, y, pageW, 22), sfCenter); y += 24;
                g.DrawString("ورقة عمل الجرد المخزني الفعلي", boldBig, Brushes.Black, new RectangleF(startX, y, pageW, 30), sfCenter); y += 30;
                g.DrawString("اطبع هذه الورقة لتدوين الرصيد الفعلي يدوياً من داخل المستودع", small, Brushes.DarkSlateGray, new RectangleF(startX, y, pageW, 20), sfCenter); y += 22;

                g.DrawLine(new Pen(Color.FromArgb(24, 43, 73), 1.5f), startX, y, startX + pageW, y); y += 8;

                string whName = cboWarehouse.SelectedItem != null ? cboWarehouse.Text : "المستودع الرئيسي";
                g.DrawString($"المستودع: {whName}   |   تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", small, Brushes.Black, new RectangleF(startX, y, pageW, 20), sfRtlRight);
                y += 24;

                // 2. Table Grid Setup (RTL columns)
                // Columns: الكود (80), اسم الصنف (365), الوحدة (75), الرصيد الدفتري (105), الرصيد الفعلي (155) = 780
                int[] colWidths = { 80, 365, 75, 105, 155 };
                string[] headers = { "الكود", "اسم الصنف", "الوحدة", "الرصيد الدفتري", "الرصيد الفعلي (يدوي)" };

                int headH = 30;
                int rowH = 28;

                var brushHeaderBg = new SolidBrush(Color.FromArgb(24, 43, 73));
                var brushRowAlt = new SolidBrush(Color.FromArgb(248, 250, 252));
                var penGrid = new Pen(Color.FromArgb(170, 185, 205), 1f);
                var penDark = new Pen(Color.FromArgb(24, 43, 73), 1.5f);

                // Draw Table Header
                g.FillRectangle(brushHeaderBg, startX, y, pageW, headH);
                g.DrawRectangle(penDark, startX, y, pageW, headH);

                int curX = startX + pageW;
                for (int i = 0; i < headers.Length; i++)
                {
                    curX -= colWidths[i];
                    var hRect = new RectangleF(curX, y, colWidths[i], headH);
                    g.DrawRectangle(penGrid, curX, y, colWidths[i], headH);
                    g.DrawString(headers[i], boldHead, Brushes.White, hRect, sfCenter);
                }
                y += headH;

                int maxY = 1070; // A4 page limit

                // Draw Rows
                while (_printStockRowIndex < dgStock.Rows.Count)
                {
                    var row = dgStock.Rows[_printStockRowIndex];
                    if (row.IsNewRow) { _printStockRowIndex++; continue; }

                    string code = row.Cells["ProductCode"].Value?.ToString() ?? "";
                    string name = row.Cells["ProductName"].Value?.ToString() ?? "";
                    string unit = row.Cells["Unit"].Value?.ToString()?.Replace(" 🔽", "") ?? "";
                    string bookQty = row.Cells["BookQty"].Value?.ToString() ?? "0";

                    Brush bgBrush = (_printStockRowIndex % 2 == 1) ? brushRowAlt : Brushes.White;
                    g.FillRectangle(bgBrush, startX, y, pageW, rowH);
                    g.DrawRectangle(penGrid, startX, y, pageW, rowH);

                    curX = startX + pageW;

                    // Col 0: Code
                    curX -= colWidths[0];
                    var rCode = new RectangleF(curX, y, colWidths[0], rowH);
                    g.DrawRectangle(penGrid, curX, y, colWidths[0], rowH);
                    g.DrawString(code, small, Brushes.Black, rCode, sfCenter);

                    // Col 1: Name (Huge Width, Right aligned)
                    curX -= colWidths[1];
                    var rName = new RectangleF(curX + 6, y, colWidths[1] - 12, rowH);
                    g.DrawRectangle(penGrid, curX, y, colWidths[1], rowH);
                    g.DrawString(name, normal, Brushes.Black, rName, sfRtlRight);

                    // Col 2: Unit
                    curX -= colWidths[2];
                    var rUnit = new RectangleF(curX, y, colWidths[2], rowH);
                    g.DrawRectangle(penGrid, curX, y, colWidths[2], rowH);
                    g.DrawString(unit, small, Brushes.Black, rUnit, sfCenter);

                    // Col 3: Book Qty
                    curX -= colWidths[3];
                    var rBook = new RectangleF(curX, y, colWidths[3], rowH);
                    g.DrawRectangle(penGrid, curX, y, colWidths[3], rowH);
                    g.DrawString(bookQty, boldHead, Brushes.Black, rBook, sfCenter);

                    // Col 4: Actual Qty (Manual Writing Box with clean dotted guide)
                    curX -= colWidths[4];
                    var rActual = new RectangleF(curX, y, colWidths[4], rowH);
                    g.DrawRectangle(penGrid, curX, y, colWidths[4], rowH);

                    // Writing area rectangle
                    var rectWrite = new Rectangle(curX + 15, y + 4, colWidths[4] - 30, rowH - 8);
                    g.DrawRectangle(new Pen(Color.FromArgb(200, 210, 225), 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot }, rectWrite);

                    y += rowH;
                    _printStockRowIndex++;

                    if (y >= maxY && _printStockRowIndex < dgStock.Rows.Count)
                    {
                        g.DrawString($"صفحة {_printStockPageNum}", small, Brushes.Gray, startX + pageW - 70, e.PageBounds.Height - 35);
                        _printStockPageNum++;
                        e.HasMorePages = true;
                        return;
                    }
                }

                e.HasMorePages = false;
                y += 15;
                if (y < maxY)
                {
                    g.DrawLine(new Pen(Color.FromArgb(24, 43, 73), 1.5f), startX, y, startX + pageW, y); y += 8;
                    g.DrawString("المسؤول عن الجرد: .......................................         توقيع لجنة الجرد: .......................................", bold, Brushes.Black, new RectangleF(startX, y, pageW, 25), sfRtlRight);
                }

                g.DrawString($"صفحة {_printStockPageNum}", small, Brushes.Gray, startX + pageW - 70, e.PageBounds.Height - 35);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 950,
                Height = 800,
                Text = "طباعة ورقة عمل الجرد المخزني (تقسيم شبكي A4)"
            };
            preview.ShowDialog();
        }

        private int _printLogIndex = 0;
        private int _printLogPageNum = 1;

        private void PrintAdjustmentsLog()
        {
            _printLogIndex = 0;
            _printLogPageNum = 1;
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var boldBig = new Font("Arial", 16, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var boldHead = new Font("Arial", 9.5f, FontStyle.Bold);
                var normal = new Font("Arial", 9.5f, FontStyle.Regular);
                var small = new Font("Arial", 8.5f, FontStyle.Regular);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRtlRight = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap };

                int startX = 23;
                int pageW = 780;
                int y = 25;

                // Title
                string company = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
                g.DrawString(company, bold, Brushes.DarkBlue, new RectangleF(startX, y, pageW, 22), sfCenter); y += 24;
                g.DrawString("تقرير سجل تسويات فروقات الجرد المخزني", boldBig, Brushes.Black, new RectangleF(startX, y, pageW, 30), sfCenter); y += 30;
                g.DrawString($"الفترة: من {dtpFrom.Value:dd/MM/yyyy} إلى {dtpTo.Value:dd/MM/yyyy}   |   تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", small, Brushes.DarkSlateGray, new RectangleF(startX, y, pageW, 20), sfCenter); y += 22;

                g.DrawLine(new Pen(Color.FromArgb(24, 43, 73), 1.5f), startX, y, startX + pageW, y); y += 10;

                // Columns (RTL): التاريخ والوقت (130), اسم الصنف (320), الدفتري (80), الفعلي (80), الفارق (85), المسؤول (85) = 780
                int[] colWidths = { 130, 320, 80, 80, 85, 85 };
                string[] headers = { "التاريخ والوقت", "اسم الصنف", "الدفتري", "الفعلي", "الفارق", "المسؤول" };

                int headH = 30;
                int rowH = 26;

                var brushHeaderBg = new SolidBrush(Color.FromArgb(24, 43, 73));
                var brushRowAlt = new SolidBrush(Color.FromArgb(248, 250, 252));
                var penGrid = new Pen(Color.FromArgb(170, 185, 205), 1f);
                var penDark = new Pen(Color.FromArgb(24, 43, 73), 1.5f);

                // Table Header
                g.FillRectangle(brushHeaderBg, startX, y, pageW, headH);
                g.DrawRectangle(penDark, startX, y, pageW, headH);

                int curX = startX + pageW;
                for (int i = 0; i < headers.Length; i++)
                {
                    curX -= colWidths[i];
                    var hRect = new RectangleF(curX, y, colWidths[i], headH);
                    g.DrawRectangle(penGrid, curX, y, colWidths[i], headH);
                    g.DrawString(headers[i], boldHead, Brushes.White, hRect, sfCenter);
                }
                y += headH;

                int maxY = 1080;

                while (_printLogIndex < dgLogs.Rows.Count)
                {
                    var row = dgLogs.Rows[_printLogIndex];
                    if (row.IsNewRow) { _printLogIndex++; continue; }

                    string date = row.Cells["AdjDate"].Value?.ToString() ?? "";
                    string name = row.Cells["ProductName"].Value?.ToString() ?? "";
                    string book = row.Cells["BookQty"].Value?.ToString() ?? "0";
                    string actual = row.Cells["ActualQty"].Value?.ToString() ?? "0";
                    string diff = row.Cells["DiffQty"].Value?.ToString() ?? "0";
                    string user = row.Cells["CreatedBy"].Value?.ToString() ?? "";

                    Brush bgBrush = (_printLogIndex % 2 == 1) ? brushRowAlt : Brushes.White;
                    g.FillRectangle(bgBrush, startX, y, pageW, rowH);
                    g.DrawRectangle(penGrid, startX, y, pageW, rowH);

                    curX = startX + pageW;

                    // Col 0: Date
                    curX -= colWidths[0];
                    g.DrawRectangle(penGrid, curX, y, colWidths[0], rowH);
                    g.DrawString(date, small, Brushes.Black, new RectangleF(curX, y, colWidths[0], rowH), sfCenter);

                    // Col 1: Name
                    curX -= colWidths[1];
                    g.DrawRectangle(penGrid, curX, y, colWidths[1], rowH);
                    g.DrawString(name, normal, Brushes.Black, new RectangleF(curX + 6, y, colWidths[1] - 12, rowH), sfRtlRight);

                    // Col 2: Book
                    curX -= colWidths[2];
                    g.DrawRectangle(penGrid, curX, y, colWidths[2], rowH);
                    g.DrawString(book, small, Brushes.Black, new RectangleF(curX, y, colWidths[2], rowH), sfCenter);

                    // Col 3: Actual
                    curX -= colWidths[3];
                    g.DrawRectangle(penGrid, curX, y, colWidths[3], rowH);
                    g.DrawString(actual, small, Brushes.Black, new RectangleF(curX, y, colWidths[3], rowH), sfCenter);

                    // Col 4: Diff
                    curX -= colWidths[4];
                    g.DrawRectangle(penGrid, curX, y, colWidths[4], rowH);
                    Brush diffBrush = diff.StartsWith("+") ? Brushes.Green : (diff.StartsWith("-") ? Brushes.Red : Brushes.Black);
                    g.DrawString(diff, boldHead, diffBrush, new RectangleF(curX, y, colWidths[4], rowH), sfCenter);

                    // Col 5: User
                    curX -= colWidths[5];
                    g.DrawRectangle(penGrid, curX, y, colWidths[5], rowH);
                    g.DrawString(user, small, Brushes.Black, new RectangleF(curX, y, colWidths[5], rowH), sfCenter);

                    y += rowH;
                    _printLogIndex++;

                    if (y >= maxY && _printLogIndex < dgLogs.Rows.Count)
                    {
                        g.DrawString($"صفحة {_printLogPageNum}", small, Brushes.Gray, startX + pageW - 70, e.PageBounds.Height - 35);
                        _printLogPageNum++;
                        e.HasMorePages = true;
                        return;
                    }
                }

                e.HasMorePages = false;
                y += 15;
                if (y < maxY)
                {
                    g.DrawLine(new Pen(Color.FromArgb(24, 43, 73), 1.5f), startX, y, startX + pageW, y);
                }

                g.DrawString($"صفحة {_printLogPageNum}", small, Brushes.Gray, startX + pageW - 70, e.PageBounds.Height - 35);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 950,
                Height = 800,
                Text = "طباعة سجل التسويات الجردية (تقسيم شبكي A4)"
            };
            preview.ShowDialog();
        }

        private void LoadWarehouses()
        {
            try
            {
                var dt = WarehouseDAL.GetAll(true);
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add(new ComboItem(0, "--- كل المخازن ---"));
                foreach (DataRow r in dt.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
                }
                cboWarehouse.DisplayMember = "Text";
                cboWarehouse.ValueMember = "ID";
                if (cboWarehouse.Items.Count > 0)
                {
                    cboWarehouse.SelectedIndex = 0;
                    for (int i = 0; i < cboWarehouse.Items.Count; i++)
                    {
                        if (cboWarehouse.Items[i] is ComboItem ci && (ci.Text.Contains("الرئيسي") || ci.Text.Contains("الرئيسى")))
                        {
                            cboWarehouse.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل قائمة المخازن:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLogWarehouses()
        {
            try
            {
                var dt = WarehouseDAL.GetAll(true);
                cboLogWarehouse.Items.Clear();
                cboLogWarehouse.Items.Add(new ComboItem(0, "--- كل المخازن ---"));
                foreach (DataRow r in dt.Rows)
                {
                    cboLogWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
                }
                cboLogWarehouse.DisplayMember = "Text";
                cboLogWarehouse.ValueMember = "ID";
                if (cboLogWarehouse.Items.Count > 0)
                {
                    cboLogWarehouse.SelectedIndex = 0;
                    for (int i = 0; i < cboLogWarehouse.Items.Count; i++)
                    {
                        if (cboLogWarehouse.Items[i] is ComboItem ci && (ci.Text.Contains("الرئيسي") || ci.Text.Contains("الرئيسى")))
                        {
                            cboLogWarehouse.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل قائمة المخازن في السجل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLocations()
        {
            try
            {
                if (cboLocation == null) return;
                cboLocation.Items.Clear();
                cboLocation.Items.Add("--- كل الأماكن ---");
                var locations = InventoryDAL.GetAllLocations();
                foreach (var loc in locations)
                {
                    cboLocation.Items.Add(loc);
                }
                cboLocation.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadLocations", ex);
            }
        }

        private DataGridView MakeGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(210, 210, 215),
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(240, 242, 245), ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
        }

        private void BtnAddExpiryRow_Click(object sender, EventArgs e)
        {
            if (dgStock.SelectedRows.Count == 0) return;
            var r = dgStock.SelectedRows[0];
            
            int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
            string code = r.Cells["ProductCode"].Value?.ToString();
            string name = r.Cells["ProductName"].Value?.ToString();
            string unit = r.Cells["Unit"].Value?.ToString();
            string baseUnit = r.Cells["BaseUnit"].Value?.ToString();
            string unit1 = r.Cells["Unit1Name"].Value?.ToString();
            string unit2 = r.Cells["Unit2Name"].Value?.ToString();
            decimal u2f = r.Cells["Unit2Factor"].Value != DBNull.Value ? Convert.ToDecimal(r.Cells["Unit2Factor"].Value) : 0m;
            decimal u3f = r.Cells["Unit3Factor"].Value != DBNull.Value ? Convert.ToDecimal(r.Cells["Unit3Factor"].Value) : 0m;
            bool hasExp = r.Cells["HasExpiry"].Value != DBNull.Value && Convert.ToBoolean(r.Cells["HasExpiry"].Value);
            int? defDays = r.Cells["DefaultExpiryDays"].Value != DBNull.Value ? Convert.ToInt32(r.Cells["DefaultExpiryDays"].Value) : (int?)null;
            string price = r.Cells["SalePrice"].Value?.ToString();

            // Clean the name of "(صلاحية إضافية)" if we are adding from an already modified row
            if (name.Contains(" (صلاحية إضافية)"))
            {
                name = name.Replace(" (صلاحية إضافية)", "");
            }

            int ri = dgStock.Rows.Add();
            var newRow = dgStock.Rows[ri];

            newRow.Cells["ProductID"].Value = pid;
            newRow.Cells["BatchID"].Value = DBNull.Value;
            newRow.Cells["ProductCode"].Value = code;
            newRow.Cells["ProductName"].Value = name + " (صلاحية إضافية)";
            newRow.Cells["ExpiryDate"].Value = "";
            newRow.Cells["Unit"].Value = unit;
            newRow.Cells["SalePrice"].Value = price;
            newRow.Cells["BookQty"].Value = "0.000";
            newRow.Cells["ActualQty"].Value = "";
            newRow.Cells["DiffQty"].Value = "";
            newRow.Cells["Notes"].Value = "";

            newRow.Cells["BaseUnit"].Value = baseUnit;
            newRow.Cells["Unit1Name"].Value = unit1;
            newRow.Cells["Unit2Name"].Value = unit2;
            newRow.Cells["Unit2Factor"].Value = u2f;
            newRow.Cells["Unit3Factor"].Value = u3f;
            newRow.Cells["HasExpiry"].Value = hasExp;
            newRow.Cells["DefaultExpiryDays"].Value = defDays;

            dgStock.ClearSelection();
            newRow.Selected = true;
            dgStock.CurrentCell = newRow.Cells["ExpiryDate"];
            dgStock.BeginEdit(true);
        }

        private int _printScaleRowIndex = 0;

        private void PrintScaleProductsReport()
        {
            _printScaleRowIndex = 0;
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);

            int? wid = null;
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem wci && wci.ID > 0)
                wid = wci.ID;

            string whName = (cboWarehouse != null && cboWarehouse.SelectedItem != null) ? cboWarehouse.SelectedItem.ToString() : "كل المخازن";

            // Query all Scale Products directly
            var dtScale = InventoryDAL.GetStock(wid, scaleOnly: true, maxRows: 10000);

            if (dtScale == null || dtScale.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف تحتوي على كود ميزان (PLU) مسجل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 16, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var normal = new Font("Arial", 9);
                var center = new StringFormat { Alignment = StringAlignment.Center };

                int y = 30;
                int pageW = 800;

                // Header
                g.DrawString("⚖️ تقرير الأصناف التي تحتوي على كود ميزان (PLU)", boldBig, Brushes.DarkBlue, new RectangleF(20, y, pageW - 40, 32), center);
                y += 35;
                g.DrawString($"المخزن: {whName} | تاريخ التقرير: {DateTime.Now:dd/MM/yyyy HH:mm} | إجمالي الأصناف: {dtScale.Rows.Count}", normal, Brushes.Black, new RectangleF(20, y, pageW - 40, 20), center);
                y += 25;
                g.DrawLine(new Pen(Color.DarkBlue, 2), 20, y, pageW - 20, y);
                y += 15;

                // Columns: Local Code, Name, Scale PLU, Unit, Price, Available Qty
                int[] xCols = { 20, 130, 420, 520, 600, 700 };
                string[] headers = { "كود المحلي", "اسم الصنف", "كود الميزان", "الوحدة", "سعر البيع", "الرصيد المتاح" };

                g.FillRectangle(new SolidBrush(Color.FromArgb(230, 236, 244)), 20, y - 3, pageW - 40, 26);
                for (int i = 0; i < headers.Length; i++)
                    g.DrawString(headers[i], bold, Brushes.DarkBlue, xCols[i], y);
                y += 26;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y);
                y += 8;

                int maxY = 1080;

                while (_printScaleRowIndex < dtScale.Rows.Count)
                {
                    var row = dtScale.Rows[_printScaleRowIndex];
                    string code = row["ProductCode"]?.ToString();
                    string name = row["ProductName"]?.ToString();
                    string scalePlu = row["ScalePLU"]?.ToString();
                    string unit = row["Unit"]?.ToString();
                    decimal price = row["SalePrice"] != DBNull.Value ? Convert.ToDecimal(row["SalePrice"]) : 0m;
                    decimal qty = row["BookQty"] != DBNull.Value ? Convert.ToDecimal(row["BookQty"]) : 0m;

                    g.DrawString(code ?? "", normal, Brushes.Black, xCols[0], y);
                    g.DrawString(name ?? "", normal, Brushes.Black, xCols[1], y);
                    g.DrawString(scalePlu ?? "", bold, Brushes.DarkRed, xCols[2], y);
                    g.DrawString(unit ?? "", normal, Brushes.Black, xCols[3], y);
                    g.DrawString(price.ToString("N2") + " ج", normal, Brushes.Black, xCols[4], y);
                    g.DrawString(qty.ToString("N3"), bold, Brushes.DarkGreen, xCols[5], y);

                    y += 24;
                    _printScaleRowIndex++;

                    if (y >= maxY && _printScaleRowIndex < dtScale.Rows.Count)
                    {
                        e.HasMorePages = true;
                        return;
                    }
                }

                e.HasMorePages = false;
                y += 15;
                if (y < maxY)
                {
                    g.DrawLine(new Pen(Color.DarkBlue, 1.5f), 20, y, pageW - 20, y);
                    y += 10;
                    g.DrawString($"إجمالي عدد أصناف الميزان المسجلة: {dtScale.Rows.Count} صنف", bold, Brushes.DarkBlue, 20, y);
                }
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 900,
                Height = 800,
                Text = "طباعة تقرير أصناف الميزان (PLU)"
            };
            preview.ShowDialog();
        }
    }


    public class FrmAdjustExpiryBatches : Form
    {
        private int _productID;
        private int _warehouseID;
        private DataGridView dgBatches;
        private Button btnSave, btnCancel, btnAddBatch;
        private DateTimePicker dtpNewExpiry;
        private NumericUpDown nudNewQty;

        public FrmAdjustExpiryBatches(int productID, string productName, int warehouseID)
        {
            _productID = productID;
            _warehouseID = warehouseID;

            this.Text = $"تعديل أرصدة صلاحية الصنف: {productName}";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10) };
            var lblInfo = new Label { Text = $"صلاحيات المخزن الحالي لمجموعة الدفعات الخاصة بالصنف", Dock = DockStyle.Fill, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlTop.Controls.Add(lblInfo);

            dgBatches = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgBatches.Columns.Add(new DataGridViewTextBoxColumn { Name = "BatchID", HeaderText = "رقم الدفعة", ReadOnly = true, FillWeight = 25 });
            dgBatches.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate", HeaderText = "تاريخ الصلاحية (yyyy-MM-dd)", ReadOnly = false, FillWeight = 50 });
            dgBatches.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", ReadOnly = false, FillWeight = 35 });

            dgBatches.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgBatches.Columns[e.ColumnIndex].Name == "ExpiryDate")
                {
                    var cell = dgBatches.Rows[e.RowIndex].Cells["ExpiryDate"];
                    string val = cell.Value?.ToString();
                    var parsed = DbHelper.ParseExpiryInput(val);
                    if (parsed.HasValue)
                    {
                        cell.Value = parsed.Value.ToString("yyyy-MM-dd");
                    }
                    else if (string.IsNullOrWhiteSpace(val))
                    {
                        cell.Value = "";
                    }
                    else
                    {
                        MessageBox.Show("تاريخ غير صالح. يرجى إدخال التاريخ بالصيغة الصحيحة (شهر وسنة مثل 0326 أو yyyy-MM-dd).", "تاريخ غير صالح", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cell.Value = "";
                    }
                }
            };

            var pnlAdd = new GroupBox
            {
                Text = "إضافة دفعة جديدة",
                Dock = DockStyle.Bottom,
                Height = 85,
                ForeColor = Theme.Accent,
                Padding = new Padding(10)
            };
            var lblDate = new Label { Text = "تاريخ الانتهاء:", Location = new Point(370, 25), Size = new Size(90, 20), ForeColor = Theme.TextMain };
            dtpNewExpiry = new DateTimePicker { Location = new Point(230, 22), Width = 130, Format = DateTimePickerFormat.Short };
            var lblQty = new Label { Text = "الكمية:", Location = new Point(160, 25), Size = new Size(50, 20), ForeColor = Theme.TextMain };
            nudNewQty = new NumericUpDown { Location = new Point(80, 22), Width = 80, DecimalPlaces = 3, Maximum = 999999, Minimum = 0 };
            btnAddBatch = Theme.MakeButton("➕ إضافة", Color.FromArgb(40, 120, 60));
            btnAddBatch.Location = new Point(10, 20);
            btnAddBatch.Size = new Size(60, 28);
            btnAddBatch.Click += BtnAddBatch_Click;

            pnlAdd.Controls.AddRange(new Control[] { lblDate, dtpNewExpiry, lblQty, nudNewQty, btnAddBatch });

            var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };
            btnSave = Theme.MakeButton("💾 حفظ التعديلات", Theme.Accent);
            btnSave.Location = new Point(10, 10);
            btnSave.Size = new Size(130, 30);
            btnSave.Click += BtnSave_Click;
            btnCancel = Theme.MakeButton("❌ إلغاء", Color.FromArgb(120, 40, 40));
            btnCancel.Location = new Point(150, 10);
            btnCancel.Size = new Size(90, 30);
            btnCancel.Click += (s, e) => this.Close();
            pnlActions.Controls.AddRange(new Control[] { btnSave, btnCancel });

            this.Controls.Add(dgBatches);
            this.Controls.Add(pnlAdd);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlActions);

            LoadBatches();
        }

        private void LoadBatches()
        {
            dgBatches.Rows.Clear();
            var dt = DbHelper.Query(@"
                SELECT BatchID, ExpiryDate, Quantity 
                FROM ProductBatches 
                WHERE ProductID = @pid AND WarehouseID = @wid 
                ORDER BY ExpiryDate ASC",
                DbHelper.P("@pid", _productID), DbHelper.P("@wid", _warehouseID));
            foreach (DataRow r in dt.Rows)
            {
                DateTime? exp = r["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(r["ExpiryDate"]) : (DateTime?)null;
                dgBatches.Rows.Add(
                    r["BatchID"],
                    exp.HasValue ? exp.Value.ToString("yyyy-MM-dd") : "",
                    Convert.ToDecimal(r["Quantity"]).ToString("N3")
                );
            }
        }

        private void BtnAddBatch_Click(object sender, EventArgs e)
        {
            if (nudNewQty.Value <= 0)
            {
                MessageBox.Show("من فضلك أدخل كمية أكبر من الصفر للدفعة الجديدة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            dgBatches.Rows.Add("", dtpNewExpiry.Value.ToString("yyyy-MM-dd"), nudNewQty.Value.ToString("N3"));
            nudNewQty.Value = 0;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgBatches.Rows)
            {
                string expStr = row.Cells["ExpiryDate"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(expStr) || !DateTime.TryParse(expStr, out _))
                {
                    MessageBox.Show("❌ خطأ: يجب تسجيل تاريخ صلاحية صحيح لكل الدفعات المعروضة قبل الحفظ!", "تاريخ صلاحية مفقود", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    foreach (DataGridViewRow row in dgBatches.Rows)
                    {
                        string batchIdStr = row.Cells["BatchID"].Value?.ToString();
                        string expStr = row.Cells["ExpiryDate"].Value?.ToString();
                        string qtyStr = row.Cells["Quantity"].Value?.ToString();

                        DateTime? exp = null;
                        if (!string.IsNullOrWhiteSpace(expStr) && DateTime.TryParse(expStr, out DateTime parsedExp))
                            exp = parsedExp;

                        decimal.TryParse(qtyStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal qty);

                        if (!string.IsNullOrEmpty(batchIdStr))
                        {
                            int bid = Convert.ToInt32(batchIdStr);
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE ProductBatches SET ExpiryDate=@exp, Quantity=@qty WHERE BatchID=@bid",
                                DbHelper.P("@exp", exp.HasValue ? (object)exp.Value : DBNull.Value),
                                DbHelper.P("@qty", qty),
                                DbHelper.P("@bid", bid));
                        }
                        else
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ProductBatches (ProductID, WarehouseID, Quantity, ExpiryDate) VALUES (@pid, @wid, @qty, @exp)",
                                DbHelper.P("@pid", _productID),
                                DbHelper.P("@wid", _warehouseID),
                                DbHelper.P("@qty", qty),
                                DbHelper.P("@exp", exp.HasValue ? (object)exp.Value : DBNull.Value));
                        }
                    }

                    // Sync ProductStock
                    DbHelper.ExecuteTrans(trans,
                        @"IF EXISTS (SELECT 1 FROM ProductStock WHERE ProductID=@pid AND WarehouseID=@wid)
                            UPDATE ProductStock SET Quantity = (SELECT COALESCE(SUM(Quantity), 0) FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid) WHERE ProductID=@pid AND WarehouseID=@wid
                          ELSE
                            INSERT INTO ProductStock (ProductID, WarehouseID, Quantity) VALUES (@pid, @wid, (SELECT COALESCE(SUM(Quantity), 0) FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid))",
                        DbHelper.P("@pid", _productID),
                        DbHelper.P("@wid", _warehouseID));
                });

                MessageBox.Show("✅ تم حفظ تواريخ الصلاحية وتحديث أرصدة الدفعات بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل حفظ التعديلات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

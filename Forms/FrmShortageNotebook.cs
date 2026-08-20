using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmShortageNotebook : Form
    {
        private DataTable _dtCurrentShortages;

        // Top Header
        private Panel pnlHeaderBanner;
        private Label lblHeaderTitle;
        private Label lblHeaderSubtitle;

        // Action Toolbar
        private Panel pnlActionToolbar;
        private FlowLayoutPanel flowActions;
        private Button btnAddManual;
        private Button btnChangeStatus;
        private Button btnCreatePurchase;
        private Button btnMinStockEdit;
        private Button btnPrint;
        private Button btnExportExcel;
        private Button btnRefresh;

        // Filter Strip
        private Panel pnlFilterCard;
        private FlowLayoutPanel flowFilters;
        private TextBox txtSearch;
        private ComboBox cboSupplierFilter;
        private ComboBox cboCategoryFilter;
        private ComboBox cboBrandFilter;
        private ComboBox cboStockCondition;
        private ComboBox cboStatusFilter;
        private ComboBox cboWarehouseFilter;
        private DateTimePicker dtpDateFrom;
        private DateTimePicker dtpDateTo;
        private CheckBox chkUseDateFilter;
        private Button btnResetFilters;

        // Main Data Grid
        private Panel pnlGridWrapper;
        private DataGridView dgShortages;

        public FrmShortageNotebook()
        {
            ShortageDAL.EnsureTable();
            InitializeComponentCustom();
            LoadDropdowns();
            LoadData();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "📓 كشكول النواقص والطلبات الخاصة";
            this.Size = new Size(1340, 820);
            this.MinimumSize = new Size(1024, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // ══════════════════════════════════════════════════════════════
            // 1. TOP HEADER BANNER (شريط ترويسة رئيسي أنيق ومستقل)
            // ══════════════════════════════════════════════════════════════
            pnlHeaderBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 4, 15, 4)
            };

            lblHeaderTitle = new Label
            {
                Text = "📓 كشكول النواقص والطلبات الخاصة",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                AutoSize = true,
                Location = new Point(15, 4)
            };

            lblHeaderSubtitle = new Label
            {
                Text = "متابعة شاملة للأصناف الناقصة وحد الطلب مع إمكانية تعديل الكميات المطلوبة فورياً وتوريدها",
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = Theme.TextGray,
                AutoSize = true,
                Location = new Point(15, 23)
            };

            pnlHeaderBanner.Controls.Add(lblHeaderTitle);
            pnlHeaderBanner.Controls.Add(lblHeaderSubtitle);

            // ══════════════════════════════════════════════════════════════
            // 2. ACTION TOOLBAR (شريط الأزرار الرئيسي - يبدأ من اليمين)
            // ══════════════════════════════════════════════════════════════
            pnlActionToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 2, 15, 4)
            };

            flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 1, 0, 0),
                AutoScroll = false,
                RightToLeft = RightToLeft.Yes
            };

            btnAddManual = Theme.MakeButton("➕ إضافة طلب/نقص", 0, 0, 145, 32, Theme.Success);
            btnAddManual.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnAddManual.Click += BtnAddManual_Click;

            btnChangeStatus = Theme.MakeButton("📝 تغيير الحالة", 0, 0, 110, 32, Color.FromArgb(41, 128, 185));
            btnChangeStatus.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnChangeStatus.Click += BtnChangeStatus_Click;

            btnCreatePurchase = Theme.MakeButton("🛒 فتح فاتورة شراء", 0, 0, 135, 32, Theme.Primary);
            btnCreatePurchase.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnCreatePurchase.Click += BtnCreatePurchase_Click;

            btnMinStockEdit = Theme.MakeButton("🎯 تعديل حد الطلب", 0, 0, 125, 32, Color.FromArgb(13, 148, 136));
            btnMinStockEdit.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnMinStockEdit.Click += (s, e) => {
                new FrmMinStockEdit().ShowDialog();
                LoadData();
            };

            btnPrint = Theme.MakeButton("🖨️ طباعة الكشكول", 0, 0, 120, 32, Color.FromArgb(142, 68, 173));
            btnPrint.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnPrint.Click += BtnPrint_Click;

            btnExportExcel = Theme.MakeButton("📊 تصدير إكسل", 0, 0, 110, 32, Color.FromArgb(46, 117, 89));
            btnExportExcel.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnExportExcel.Click += BtnExportExcel_Click;

            btnRefresh = Theme.MakeButton("🔄 تحديث", 0, 0, 85, 32, Color.FromArgb(70, 80, 95));
            btnRefresh.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnRefresh.Click += (s, e) => { LoadDropdowns(); LoadData(); };

            flowActions.Controls.AddRange(new Control[] {
                btnAddManual,
                btnChangeStatus,
                btnCreatePurchase,
                btnMinStockEdit,
                btnPrint,
                btnExportExcel,
                btnRefresh
            });

            pnlActionToolbar.Controls.Add(flowActions);

            // ══════════════════════════════════════════════════════════════
            // 3. FILTER PANEL (شريط فلاتر أنيق ومريح وغير مضغوط)
            // ══════════════════════════════════════════════════════════════
            pnlFilterCard = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 58),
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 4, 12, 6)
            };

            flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                BackColor = Color.Transparent,
                AutoScroll = false,
                Padding = new Padding(0),
                Margin = new Padding(0),
                RightToLeft = RightToLeft.Yes
            };

            // Helper to create compact filter group (label + control)
            Control MakeFilterGroup(string labelText, Control ctrl, int ctrlWidth)
            {
                var grp = new Panel { AutoSize = false, Width = ctrlWidth, Height = 48, Margin = new Padding(3, 2, 3, 2) };
                var lbl = new Label
                {
                    Text = labelText, AutoSize = false, Width = ctrlWidth, Height = 16,
                    Location = new Point(0, 0), ForeColor = Theme.TextGray,
                    Font = new Font("Segoe UI", 8.25f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight
                };
                ctrl.Location = new Point(0, 18);
                ctrl.Width = ctrlWidth;
                ctrl.Height = 26;
                if (ctrl is ComboBox cb)
                {
                    cb.DropDownStyle = ComboBoxStyle.DropDownList;
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.Font = new Font("Segoe UI", 9f);
                }
                else if (ctrl is TextBox tb)
                {
                    tb.Font = new Font("Segoe UI", 9f);
                }
                grp.Controls.Add(lbl);
                grp.Controls.Add(ctrl);
                return grp;
            }

            // Search box
            txtSearch = new TextBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f), BorderStyle = BorderStyle.FixedSingle };
            txtSearch.TextChanged += (s, e) => LoadData();

            // Supplier
            cboSupplierFilter = new ComboBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f) };
            cboSupplierFilter.SelectedIndexChanged += (s, e) => LoadData();

            // Category
            cboCategoryFilter = new ComboBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f) };
            cboCategoryFilter.SelectedIndexChanged += (s, e) => LoadData();

            // Brand
            cboBrandFilter = new ComboBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f) };
            cboBrandFilter.SelectedIndexChanged += (s, e) => LoadData();

            // Stock Condition
            cboStockCondition = new ComboBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f) };
            cboStockCondition.Items.AddRange(new object[]
            {
                "الكل", "⚠️ تحت حد الطلب", "🔴 رصيد صفر", "🟡 بين الصفر والحد"
            });
            cboStockCondition.SelectedIndex = 0;
            cboStockCondition.SelectedIndexChanged += (s, e) => LoadData();

            // Status
            cboStatusFilter = new ComboBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f) };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "جديد", "تم الطلب", "تم التوفير", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => LoadData();

            // Warehouse
            cboWarehouseFilter = new ComboBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f) };
            cboWarehouseFilter.SelectedIndexChanged += (s, e) => LoadData();

            // Date filter toggle
            chkUseDateFilter = new CheckBox
            {
                Text = "تفعيل", AutoSize = true, ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Checked = false
            };
            chkUseDateFilter.CheckedChanged += (s, e) =>
            {
                dtpDateFrom.Enabled = chkUseDateFilter.Checked;
                dtpDateTo.Enabled = chkUseDateFilter.Checked;
                LoadData();
            };

            dtpDateFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Font = new Font("Segoe UI", 8.5f),
                Enabled = false
            };
            dtpDateFrom.ValueChanged += (s, e) => { if (chkUseDateFilter.Checked) LoadData(); };

            dtpDateTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Font = new Font("Segoe UI", 8.5f),
                Enabled = false
            };
            dtpDateTo.ValueChanged += (s, e) => { if (chkUseDateFilter.Checked) LoadData(); };

            // Reset button
            btnResetFilters = Theme.MakeButton("🧹 مسح", 0, 0, 70, 26, Color.FromArgb(100, 116, 139));
            btnResetFilters.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnResetFilters.Margin = new Padding(3, 18, 3, 2);
            btnResetFilters.Click += (s, e) => ResetFilters();

            // Date filter group inline
            var pnlDate = new Panel { Width = 260, Height = 48, Margin = new Padding(3, 2, 3, 2) };
            var lblDateLbl = new Label { Text = "📅 الفترة (من - إلى):", AutoSize = false, Width = 260, Height = 16, Location = new Point(0, 0), ForeColor = Theme.TextGray, Font = new Font("Segoe UI", 8.25f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
            chkUseDateFilter.Location = new Point(195, 20);
            dtpDateFrom.Location = new Point(100, 18); dtpDateFrom.Width = 90; dtpDateFrom.Height = 24;
            var lblArrow = new Label { Text = "←", Location = new Point(82, 21), AutoSize = true, ForeColor = Theme.TextGray, Font = new Font("Segoe UI", 8f) };
            dtpDateTo.Location = new Point(0, 18); dtpDateTo.Width = 80; dtpDateTo.Height = 24;
            pnlDate.Controls.AddRange(new Control[] { lblDateLbl, chkUseDateFilter, dtpDateFrom, lblArrow, dtpDateTo });

            // إضافة الفلاتر بالترتيب الصحيح من اليمين لليسار
            flowFilters.Controls.Add(MakeFilterGroup("🔍 بحث سريع", txtSearch, 175));
            flowFilters.Controls.Add(MakeFilterGroup("المورد", cboSupplierFilter, 125));
            flowFilters.Controls.Add(MakeFilterGroup("القسم", cboCategoryFilter, 115));
            flowFilters.Controls.Add(MakeFilterGroup("الماركة", cboBrandFilter, 105));
            flowFilters.Controls.Add(MakeFilterGroup("المخزن", cboWarehouseFilter, 105));
            flowFilters.Controls.Add(MakeFilterGroup("نوع النقص", cboStockCondition, 125));
            flowFilters.Controls.Add(MakeFilterGroup("الحالة", cboStatusFilter, 90));
            flowFilters.Controls.Add(pnlDate);
            flowFilters.Controls.Add(btnResetFilters);

            pnlFilterCard.Controls.Add(flowFilters);

            // ══════════════════════════════════════════════════════════════
            // 4. MAIN DATA GRID (الجدول الرئيسي مع إمكانية تعديل الكمية مباشرة)
            // ══════════════════════════════════════════════════════════════
            pnlGridWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 4, 15, 12)
            };

            dgShortages = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false, // Editable for DeficitQty column
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain,
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(38, 44, 58) : Color.FromArgb(246, 249, 253),
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain,
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                ColumnHeadersHeight = 38,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                RowTemplate = { Height = 32 },
                EnableHeadersVisualStyles = false
            };

            // Grid Columns
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShortageID", Visible = false, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierID", Visible = false, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 65, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 210, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "القسم / التصنيف", FillWeight = 95, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Brand", HeaderText = "الشركة / الماركة", FillWeight = 95, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "المورد", FillWeight = 110, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStock", HeaderText = "الرصيد الحالي", FillWeight = 80, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinStockLimit", HeaderText = "حد الطلب", FillWeight = 75, ReadOnly = true });

            // EDITABLE COLUMN: DeficitQty (الكمية المطلوبة)
            var colDeficit = new DataGridViewTextBoxColumn
            {
                Name = "DeficitQty",
                HeaderText = "الكمية المطلوبة ✏️",
                FillWeight = 95,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(45, 55, 72) : Color.FromArgb(254, 243, 199), // Soft amber editable highlight
                    ForeColor = Color.Black,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };
            dgShortages.Columns.Add(colDeficit);

            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", HeaderText = "سعر الشراء", FillWeight = 80, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "إجمالي التكلفة", FillWeight = 95, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 80, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "المصدر", FillWeight = 90, ReadOnly = true });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات", FillWeight = 120, ReadOnly = true });

            // Context Menu
            var cms = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };
            cms.Items.Add("✏️ تعديل الكمية المطلوبة لهذا الصنف", null, (s, e) => EditSelectedRequestedQty());
            cms.Items.Add("📝 تغيير حالة الصنف (جديد / تم الطلب / تم التوفير)", null, (s, e) => BtnChangeStatus_Click(null, null));
            cms.Items.Add("🛒 فتح فاتورة شراء لهذا الصنف/المورد", null, (s, e) => BtnCreatePurchase_Click(null, null));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("📊 مقارنة الموردين لهذا الصنف", null, (s, e) => ShowSupplierComparison());
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("🎯 تعديل حد الطلب للأصناف", null, (s, e) => {
                new FrmMinStockEdit().ShowDialog();
                LoadData();
            });
            cms.Items.Add("🏷️ فتح بطاقة الصنف", null, (s, e) => {
                if (dgShortages.SelectedRows.Count > 0)
                {
                    int pId = Convert.ToInt32(dgShortages.SelectedRows[0].Cells["ProductID"].Value);
                    if (pId > 0 && new FrmProductCard(pId).ShowDialog() == DialogResult.OK) LoadData();
                }
            });
            cms.Items.Add("🖨️ طباعة قائمة النواقص", null, (s, e) => BtnPrint_Click(null, null));
            dgShortages.ContextMenuStrip = cms;

            // Cell End Edit for Inline Quantity Editing
            dgShortages.CellEndEdit += DgShortages_CellEndEdit;
            dgShortages.CellDoubleClick += (s, e) => {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    if (dgShortages.Columns[e.ColumnIndex].Name == "DeficitQty")
                    {
                        // Allow typing
                    }
                    else
                    {
                        BtnChangeStatus_Click(null, null);
                    }
                }
            };

            pnlGridWrapper.Controls.Add(dgShortages);

            this.Controls.Add(pnlGridWrapper);
            this.Controls.Add(pnlFilterCard);
            this.Controls.Add(pnlActionToolbar);
            this.Controls.Add(pnlHeaderBanner);

            // Reorder controls for proper top-to-bottom docking
            pnlHeaderBanner.SendToBack();
            pnlActionToolbar.SendToBack();
            pnlFilterCard.SendToBack();
            pnlGridWrapper.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private void LoadDropdowns()
        {
            try
            {
                // Suppliers
                cboSupplierFilter.Items.Clear();
                cboSupplierFilter.Items.Add(new ComboItem(0, "كل الموردين"));
                var dtSup = DbHelper.Query("SELECT SupplierID, SupplierName FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName");
                foreach (DataRow r in dtSup.Rows)
                    cboSupplierFilter.Items.Add(new ComboItem(Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
                cboSupplierFilter.SelectedIndex = 0;

                // Categories
                cboCategoryFilter.Items.Clear();
                cboCategoryFilter.Items.Add(new ComboItem(0, "كل الأقسام"));
                var dtCat = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName");
                foreach (DataRow r in dtCat.Rows)
                    cboCategoryFilter.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                cboCategoryFilter.SelectedIndex = 0;

                // Brands
                cboBrandFilter.Items.Clear();
                cboBrandFilter.Items.Add("كل الماركات");
                var brands = ShortageDAL.GetAvailableBrands();
                foreach (string b in brands) cboBrandFilter.Items.Add(b);
                cboBrandFilter.SelectedIndex = 0;

                // Warehouses
                cboWarehouseFilter.Items.Clear();
                cboWarehouseFilter.Items.Add(new ComboItem(0, "كل المخازن"));
                try
                {
                    var dtWh = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive=1 ORDER BY WarehouseName");
                    foreach (DataRow r in dtWh.Rows)
                        cboWarehouseFilter.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
                }
                catch { /* Warehouses table may not exist */ }
                cboWarehouseFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmShortageNotebook.LoadDropdowns", ex);
            }
        }

        private void ResetFilters()
        {
            txtSearch.Text = "";
            if (cboSupplierFilter.Items.Count > 0) cboSupplierFilter.SelectedIndex = 0;
            if (cboCategoryFilter.Items.Count > 0) cboCategoryFilter.SelectedIndex = 0;
            if (cboBrandFilter.Items.Count > 0) cboBrandFilter.SelectedIndex = 0;
            if (cboWarehouseFilter.Items.Count > 0) cboWarehouseFilter.SelectedIndex = 0;
            cboStockCondition.SelectedIndex = 0;
            cboStatusFilter.SelectedIndex = 0;
            chkUseDateFilter.Checked = false;
            dtpDateFrom.Value = DateTime.Today;
            dtpDateTo.Value = DateTime.Today;
            LoadData();
        }

        public void LoadData()
        {
            dgShortages.Rows.Clear();

            int? supId = (cboSupplierFilter.SelectedItem is ComboItem cis && cis.ID > 0) ? cis.ID : (int?)null;
            int? catId = (cboCategoryFilter.SelectedItem is ComboItem cic && cic.ID > 0) ? cic.ID : (int?)null;
            string brand = cboBrandFilter.SelectedItem?.ToString();
            if (cboBrandFilter.SelectedIndex == 0 || (brand != null && brand.StartsWith("كل "))) brand = null;
            string status = cboStatusFilter.SelectedItem?.ToString() ?? "الكل";

            string stockCond = "ALL";
            if (cboStockCondition.SelectedIndex == 1) stockCond = "BELOW_MIN";
            else if (cboStockCondition.SelectedIndex == 2) stockCond = "ZERO_ONLY";
            else if (cboStockCondition.SelectedIndex == 3) stockCond = "BETWEEN_ZERO_AND_MIN";

            try
            {
                _dtCurrentShortages = ShortageDAL.GetComprehensiveShortages(
                    searchTerm: txtSearch.Text.Trim(),
                    supplierID: supId,
                    categoryID: catId,
                    brand: brand,
                    stockCondition: stockCond,
                    statusFilter: status
                );

                int zeroCount = 0;
                int belowMinCount = 0;
                decimal totalDeficitCost = 0m;

                if (_dtCurrentShortages != null)
                {
                    foreach (DataRow r in _dtCurrentShortages.Rows)
                    {
                        decimal stock = Convert.ToDecimal(r["CurrentStock"]);
                        decimal minLimit = Convert.ToDecimal(r["MinStockLimit"]);
                        decimal deficit = Convert.ToDecimal(r["DeficitQty"]);
                        decimal buyPrice = r["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                        decimal totalCost = deficit * buyPrice;

                        if (stock <= 0) zeroCount++;
                        if (minLimit > 0 && stock <= minLimit) belowMinCount++;
                        totalDeficitCost += totalCost;

                        string st = r["Status"].ToString();

                        int ri = dgShortages.Rows.Add(
                            r.Table.Columns.Contains("ProductID") ? r["ProductID"] : null,
                            r.Table.Columns.Contains("ShortageID") ? r["ShortageID"] : null,
                            r.Table.Columns.Contains("SupplierID") ? r["SupplierID"] : null,
                            r.Table.Columns.Contains("ProductCode") ? r["ProductCode"] : "",
                            r.Table.Columns.Contains("ProductName") ? r["ProductName"] : "",
                            r.Table.Columns.Contains("CategoryName") ? r["CategoryName"] : "عام",
                            r.Table.Columns.Contains("Brand") ? r["Brand"] : "-",
                            r.Table.Columns.Contains("SupplierName") ? r["SupplierName"] : "---",
                            stock.ToString("N2"),
                            minLimit.ToString("N2"),
                            deficit.ToString("N2"),
                            buyPrice.ToString("N2"),
                            totalCost.ToString("N2"),
                            st,
                            r.Table.Columns.Contains("Source") ? r["Source"] : "آلي",
                            r.Table.Columns.Contains("Notes") ? r["Notes"] : "-"
                        );

                        var row = dgShortages.Rows[ri];

                        // Stock Severity Styling
                        if (stock <= 0)
                        {
                            row.Cells["CurrentStock"].Style.ForeColor = Color.FromArgb(220, 38, 38); // Red
                            row.Cells["CurrentStock"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        }
                        else if (minLimit > 0 && stock <= minLimit)
                        {
                            row.Cells["CurrentStock"].Style.ForeColor = Color.FromArgb(217, 119, 6); // Amber
                            row.Cells["CurrentStock"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        }

                        // Status Color Styling
                        if (st == "جديد")
                        {
                            row.Cells["Status"].Style.ForeColor = Color.FromArgb(230, 126, 34);
                            row.Cells["Status"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                        else if (st == "تم الطلب")
                        {
                            row.Cells["Status"].Style.ForeColor = Color.FromArgb(37, 99, 235);
                            row.Cells["Status"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                        else if (st == "تم التوفير")
                        {
                            row.Cells["Status"].Style.ForeColor = Color.FromArgb(16, 185, 129);
                            row.Cells["Status"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                    }
                }

                if (lblHeaderSubtitle != null)
                {
                    lblHeaderSubtitle.Text = $"المعروض حالياً: {dgShortages.Rows.Count} صنف (نفد بالكامل: {zeroCount} | تحت حد الطلب: {belowMinCount}) | تكلفة التوفير التقديرية: {totalDeficitCost:N2} ج";
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmShortageNotebook.LoadData", ex);
            }
        }

        private void ShowSupplierComparison()
        {
            if (dgShortages.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى تحديد صنف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selRow = dgShortages.SelectedRows[0];
            int productID = selRow.Cells["ProductID"].Value != DBNull.Value ? Convert.ToInt32(selRow.Cells["ProductID"].Value) : 0;
            if (productID <= 0)
            {
                MessageBox.Show("هذا الصنف ليس مسجلاً في النظام (تم إضافته يدوياً).", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string productName = selRow.Cells["ProductName"].Value.ToString();

            try
            {
                // Fetch all purchase history for this product grouped by supplier
                string sql = @"
                    SELECT 
                        s.SupplierName,
                        COUNT(pi.ItemID) AS PurchaseCount,
                        MIN(pi.UnitPrice) AS MinPrice,
                        MAX(pi.UnitPrice) AS MaxPrice,
                        AVG(pi.UnitPrice) AS AvgPrice,
                        MAX(p.PurchaseDate) AS LastPurchaseDate,
                        SUM(pi.Quantity) AS TotalQtyPurchased,
                        (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2
                         INNER JOIN Purchases p2 ON pi2.PurchaseID=p2.PurchaseID
                         WHERE pi2.ProductID=pi.ProductID AND p2.SupplierID=p.SupplierID
                         ORDER BY p2.PurchaseDate DESC) AS LastPrice
                    FROM PurchaseItems pi
                    INNER JOIN Purchases p ON pi.PurchaseID = p.PurchaseID
                    INNER JOIN Suppliers s ON p.SupplierID = s.SupplierID
                    WHERE pi.ProductID = @pid
                    GROUP BY s.SupplierName, p.SupplierID, pi.ProductID
                    ORDER BY AvgPrice ASC";

                var dt = DbHelper.Query(sql, DbHelper.P("@pid", productID));

                // Build dialog
                using (var dlg = new Form())
                {
                    dlg.Text = $"📊 مقارنة الموردين - {productName}";
                    dlg.Size = new Size(780, 480);
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.BackColor = Theme.BgMain;
                    dlg.RightToLeft = RightToLeft.Yes;
                    dlg.RightToLeftLayout = true;
                    dlg.Font = Theme.FontMain;

                    var lblTitle = new Label
                    {
                        Text = $"📊 مقارنة أسعار الموردين للصنف: {productName}",
                        Dock = DockStyle.Top,
                        Height = 38,
                        ForeColor = Theme.Primary,
                        Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                        TextAlign = ContentAlignment.MiddleRight,
                        Padding = new Padding(10, 0, 10, 0),
                        BackColor = Theme.BgCard
                    };
                    dlg.Controls.Add(lblTitle);

                    var grid = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false,
                        RowHeadersVisible = false,
                        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                        BackgroundColor = Theme.BgCard,
                        BorderStyle = BorderStyle.None,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        GridColor = Theme.BorderColor,
                        RightToLeft = RightToLeft.Yes,
                        ColumnHeadersHeight = 36,
                        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                        {
                            BackColor = Theme.Primary, ForeColor = Color.White,
                            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                            Alignment = DataGridViewContentAlignment.MiddleCenter
                        },
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            BackColor = Theme.BgCard, ForeColor = Theme.TextMain,
                            SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White,
                            Alignment = DataGridViewContentAlignment.MiddleCenter,
                            Font = Theme.FontMain
                        },
                        RowTemplate = { Height = 30 },
                        EnableHeadersVisualStyles = false
                    };

                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "اسم المورد", FillWeight = 140 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseCount", HeaderText = "عدد المشتريات", FillWeight = 80 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalQty", HeaderText = "إجمالي الكمية", FillWeight = 90 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastPrice", HeaderText = "آخر سعر شراء", FillWeight = 90 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvgPrice", HeaderText = "متوسط السعر", FillWeight = 90 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinPrice", HeaderText = "أقل سعر", FillWeight = 80 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaxPrice", HeaderText = "أعلى سعر", FillWeight = 80 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastDate", HeaderText = "آخر عملية شراء", FillWeight = 110 });

                    if (dt.Rows.Count == 0)
                    {
                        var lblNoData = new Label
                        {
                            Text = "⚠️ لا توجد سجلات شراء لهذا الصنف في النظام بعد.",
                            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                            ForeColor = Theme.TextGray, Font = new Font("Segoe UI", 10f)
                        };
                        dlg.Controls.Add(lblNoData);
                    }
                    else
                    {
                        decimal bestAvg = decimal.MaxValue;
                        int bestRowIndex = -1;

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            DataRow r = dt.Rows[i];
                            decimal avg = r["AvgPrice"] != DBNull.Value ? Convert.ToDecimal(r["AvgPrice"]) : 0m;
                            decimal lastP = r["LastPrice"] != DBNull.Value ? Convert.ToDecimal(r["LastPrice"]) : 0m;
                            decimal minP = r["MinPrice"] != DBNull.Value ? Convert.ToDecimal(r["MinPrice"]) : 0m;
                            decimal maxP = r["MaxPrice"] != DBNull.Value ? Convert.ToDecimal(r["MaxPrice"]) : 0m;
                            string lastDate = r["LastPurchaseDate"] != DBNull.Value ? Convert.ToDateTime(r["LastPurchaseDate"]).ToString("yyyy/MM/dd") : "-";

                            int ri = grid.Rows.Add(
                                r["SupplierName"].ToString(),
                                r["PurchaseCount"].ToString(),
                                Convert.ToDecimal(r["TotalQtyPurchased"]).ToString("N2"),
                                lastP.ToString("N2"),
                                avg.ToString("N2"),
                                minP.ToString("N2"),
                                maxP.ToString("N2"),
                                lastDate
                            );

                            if (avg < bestAvg && avg > 0)
                            {
                                bestAvg = avg;
                                bestRowIndex = ri;
                            }
                        }

                        // Highlight best supplier (lowest avg price)
                        if (bestRowIndex >= 0)
                        {
                            var bestRow = grid.Rows[bestRowIndex];
                            foreach (DataGridViewCell cell in bestRow.Cells)
                            {
                                cell.Style.BackColor = Color.FromArgb(220, 252, 231); // green tint
                                cell.Style.ForeColor = Color.FromArgb(5, 120, 60);
                                cell.Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                            }
                            bestRow.Cells["SupplierName"].Value = "🏆 " + bestRow.Cells["SupplierName"].Value;
                        }

                        dlg.Controls.Add(grid);
                    }

                    var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Theme.BgCard };
                    var btnClose = Theme.MakeButton("✖ إغلاق", 10, 5, 110, 30, Color.FromArgb(100, 116, 139));
                    btnClose.Click += (s2, e2) => dlg.Close();
                    pnlBottom.Controls.Add(btnClose);

                    var lblHint = new Label
                    {
                        Text = "🏆 الصف المظلل بالأخضر = أفضل مورد بأقل متوسط سعر شراء",
                        Location = new Point(130, 12), AutoSize = true,
                        ForeColor = Color.FromArgb(5, 120, 60),
                        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
                    };
                    pnlBottom.Controls.Add(lblHint);
                    dlg.Controls.Add(pnlBottom);

                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل بيانات مقارنة الموردين:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgShortages_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (dgShortages.Columns[e.ColumnIndex].Name == "DeficitQty")
            {
                var row = dgShortages.Rows[e.RowIndex];
                if (decimal.TryParse(row.Cells["DeficitQty"].Value?.ToString(), out decimal newQty))
                {
                    if (newQty <= 0) newQty = 1;
                    row.Cells["DeficitQty"].Value = newQty.ToString("N2");

                    int sId = row.Cells["ShortageID"].Value != DBNull.Value ? Convert.ToInt32(row.Cells["ShortageID"].Value) : 0;
                    int? pId = row.Cells["ProductID"].Value != DBNull.Value ? (int?)Convert.ToInt32(row.Cells["ProductID"].Value) : null;

                    // Save to Database
                    ShortageDAL.UpdateRequestedQty(sId, pId, newQty);

                    // Recalculate Row Cost
                    decimal buyPrice = decimal.TryParse(row.Cells["PurchasePrice"].Value?.ToString(), out decimal p) ? p : 0m;
                    decimal rowCost = newQty * buyPrice;
                    row.Cells["TotalCost"].Value = rowCost.ToString("N2");

                    // Recalculate Total KPI Cost
                    RecalculateTotalKpiCost();
                }
                else
                {
                    row.Cells["DeficitQty"].Value = "1.00";
                }
            }
        }

        private void RecalculateTotalKpiCost()
        {
            decimal totalCost = 0m;
            int zeroCount = 0;
            int belowMinCount = 0;
            foreach (DataGridViewRow r in dgShortages.Rows)
            {
                if (decimal.TryParse(r.Cells["CurrentStock"].Value?.ToString(), out decimal st) && st <= 0) zeroCount++;
                if (decimal.TryParse(r.Cells["MinStockLimit"].Value?.ToString(), out decimal ml) && decimal.TryParse(r.Cells["CurrentStock"].Value?.ToString(), out decimal cs) && ml > 0 && cs <= ml) belowMinCount++;
                if (decimal.TryParse(r.Cells["TotalCost"].Value?.ToString(), out decimal c))
                {
                    totalCost += c;
                }
            }
            if (lblHeaderSubtitle != null)
            {
                lblHeaderSubtitle.Text = $"المعروض حالياً: {dgShortages.Rows.Count} صنف (نفد بالكامل: {zeroCount} | تحت حد الطلب: {belowMinCount}) | تكلفة التوفير التقديرية: {totalCost:N2} ج";
            }
        }

        private void EditSelectedRequestedQty()
        {
            if (dgShortages.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى تحديد صنف أولاً من الجدول لتعديل الكمية المطلوبة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgShortages.SelectedRows[0];
            string pName = row.Cells["ProductName"].Value.ToString();
            decimal currentQty = decimal.TryParse(row.Cells["DeficitQty"].Value?.ToString(), out decimal q) ? q : 1;

            using (var dlg = new Form())
            {
                dlg.Text = "تعديل الكمية المطلوبة للصنف";
                dlg.Size = new Size(380, 210);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = Theme.BgMain;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lbl = new Label { Text = $"الصنف: [{pName}]\nأدخل الكمية المطلوبة لتوفيرها:", Location = new Point(20, 15), Size = new Size(320, 38), ForeColor = Theme.TextMain, Font = Theme.FontMain };
                dlg.Controls.Add(lbl);

                var nud = new NumericUpDown
                {
                    Location = new Point(20, 60),
                    Width = 320,
                    Minimum = 1,
                    Maximum = 100000,
                    Value = currentQty > 0 ? currentQty : 1,
                    BackColor = Theme.BgInput,
                    ForeColor = Theme.TextMain,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold)
                };
                dlg.Controls.Add(nud);

                var btnSave = Theme.MakeButton("💾 حفظ الكمية", 20, 110, 150, 36, Theme.Success);
                btnSave.Click += (s2, e2) =>
                {
                    row.Cells["DeficitQty"].Value = nud.Value.ToString("N2");
                    int sId = row.Cells["ShortageID"].Value != DBNull.Value ? Convert.ToInt32(row.Cells["ShortageID"].Value) : 0;
                    int? pId = row.Cells["ProductID"].Value != DBNull.Value ? (int?)Convert.ToInt32(row.Cells["ProductID"].Value) : null;
                    ShortageDAL.UpdateRequestedQty(sId, pId, nud.Value);

                    decimal buyPrice = decimal.TryParse(row.Cells["PurchasePrice"].Value?.ToString(), out decimal p) ? p : 0m;
                    row.Cells["TotalCost"].Value = (nud.Value * buyPrice).ToString("N2");
                    RecalculateTotalKpiCost();

                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                dlg.Controls.Add(btnSave);

                var btnCancel = Theme.MakeButton("❌ إلغاء", 190, 110, 150, 36, Theme.Danger);
                btnCancel.Click += (s2, e2) => dlg.Close();
                dlg.Controls.Add(btnCancel);

                dlg.ShowDialog(this);
            }
        }

        private void BtnAddManual_Click(object sender, EventArgs e)
        {
            using (var dlg = new FrmAddShortageItem())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnChangeStatus_Click(object sender, EventArgs e)
        {
            if (dgShortages.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى تحديد صنف من الجدول لتغيير حالته", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selRow = dgShortages.SelectedRows[0];
            int pId = Convert.ToInt32(selRow.Cells["ProductID"].Value);
            string pName = selRow.Cells["ProductName"].Value.ToString();
            decimal stock = Convert.ToDecimal(selRow.Cells["CurrentStock"].Value);
            decimal minLimit = Convert.ToDecimal(selRow.Cells["MinStockLimit"].Value);
            decimal deficit = Convert.ToDecimal(selRow.Cells["DeficitQty"].Value);
            string currentStatus = selRow.Cells["Status"].Value.ToString();

            using (var dlg = new Form())
            {
                dlg.Text = "تغيير حالة الصنف في كشكول النواقص";
                dlg.Size = new Size(380, 240);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = Theme.BgMain;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lbl = new Label { Text = $"الصنف: [{pName}]\nاختر الحالة الجديدة:", Location = new Point(20, 20), Size = new Size(320, 40), ForeColor = Theme.TextMain, Font = Theme.FontMain };
                dlg.Controls.Add(lbl);

                var cbo = new ComboBox
                {
                    Location = new Point(20, 70),
                    Width = 320,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Theme.BgInput,
                    ForeColor = Theme.TextMain,
                    Font = Theme.FontMain,
                    FlatStyle = FlatStyle.Flat
                };
                cbo.Items.AddRange(new object[] { "جديد", "تم الطلب", "تم التوفير", "ملغي" });
                cbo.SelectedItem = currentStatus == "تحت الحد" ? "جديد" : currentStatus;
                dlg.Controls.Add(cbo);

                var btnSave = Theme.MakeButton("💾 حفظ وتحديث", 20, 130, 150, 36, Theme.Success);
                btnSave.Click += (s2, e2) =>
                {
                    string newSt = cbo.SelectedItem.ToString();
                    ShortageDAL.AddOrUpdateShortage(
                        productID: pId,
                        productName: pName,
                        requestedQty: deficit,
                        currentStock: stock,
                        minStockLimit: minLimit,
                        notes: "تم تغيير الحالة يدوياً من الشاشة",
                        source: "كشكول النواقص",
                        status: newSt
                    );
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                dlg.Controls.Add(btnSave);

                var btnCancel = Theme.MakeButton("❌ إلغاء", 190, 130, 150, 36, Theme.Danger);
                btnCancel.Click += (s2, e2) => dlg.Close();
                dlg.Controls.Add(btnCancel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnCreatePurchase_Click(object sender, EventArgs e)
        {
            new FrmPurchase().ShowDialog();
            LoadData();
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (dgShortages.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var sfd = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv", FileName = $"Shortages_{DateTime.Now:yyyyMMdd_HHmm}.csv" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var lines = new List<string>();
                        lines.Add("كود الصنف,اسم الصنف,القسم,الشركة/الماركة,المورد,الرصيد الحالي,حد الطلب,الكمية المطلوبة,سعر الشراء,إجمالي التكلفة,الحالة,المصدر,الملاحظات");
                        foreach (DataGridViewRow r in dgShortages.Rows)
                        {
                            string line = $"\"{r.Cells["ProductCode"].Value}\",\"{r.Cells["ProductName"].Value}\",\"{r.Cells["CategoryName"].Value}\",\"{r.Cells["Brand"].Value}\",\"{r.Cells["SupplierName"].Value}\",\"{r.Cells["CurrentStock"].Value}\",\"{r.Cells["MinStockLimit"].Value}\",\"{r.Cells["DeficitQty"].Value}\",\"{r.Cells["PurchasePrice"].Value}\",\"{r.Cells["TotalCost"].Value}\",\"{r.Cells["Status"].Value}\",\"{r.Cells["Source"].Value}\",\"{r.Cells["Notes"].Value}\"";
                            lines.Add(line);
                        }
                        System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
                        MessageBox.Show("تم تصدير ملف النواقص بنجاح!", "نجاح التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء التصدير: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (dgShortages.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لطباعتها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var pd = new PrintDocument())
                {
                    pd.PrintController = new StandardPrintController();
                    AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
                    int printRowIndex = 0;

                    pd.PrintPage += (s, e2) =>
                    {
                        Graphics g = e2.Graphics;
                        Font fontTitle = new Font("Segoe UI", 13f, FontStyle.Bold);
                        Font fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        Font fontBody = new Font("Segoe UI", 8.5f);
                        Font fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold);

                        float y = 25;
                        float leftMargin = 25;
                        float rightMargin = e2.PageBounds.Width - 25;
                        float contentWidth = rightMargin - leftMargin;

                        StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                        StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };

                        // Header
                        g.DrawString(AppConfig.CompanyName ?? "المؤسسة التجارية", fontTitle, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 25), sfCenter);
                        y += 26;
                        g.DrawString("📓 تقرير كشكول النواقص وأوامر التوريد المطلوبة", fontHeader, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 22), sfCenter);
                        y += 24;

                        string filterDesc = $"التاريخ: {DateTime.Now:yyyy/MM/dd HH:mm} | المورد: {cboSupplierFilter.Text} | القسم: {cboCategoryFilter.Text} | النوع: {cboStockCondition.Text}";
                        g.DrawString(filterDesc, fontBody, Brushes.DimGray, new RectangleF(leftMargin, y, contentWidth, 18), sfCenter);
                        y += 22;

                        g.DrawLine(new Pen(Color.Black, 1.2f), leftMargin, y, rightMargin, y);
                        y += 6;

                        // Table Columns
                        float wCost = 70;
                        float wPrice = 65;
                        float wDeficit = 65;
                        float wMin = 55;
                        float wStock = 60;
                        float wSup = 100;
                        float wName = contentWidth - (wCost + wPrice + wDeficit + wMin + wStock + wSup);

                        float colCost = leftMargin;
                        float colPrice = colCost + wCost;
                        float colDeficit = colPrice + wPrice;
                        float colMin = colDeficit + wDeficit;
                        float colStock = colMin + wMin;
                        float colSup = colStock + wStock;
                        float colName = colSup + wSup;

                        g.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), leftMargin, y, contentWidth, 22);
                        g.DrawRectangle(Pens.Black, leftMargin, y, contentWidth, 22);

                        g.DrawString("إجمالي التكلفة", fontHeader, Brushes.Black, new RectangleF(colCost, y + 2, wCost, 18), sfCenter);
                        g.DrawString("سعر الشراء", fontHeader, Brushes.Black, new RectangleF(colPrice, y + 2, wPrice, 18), sfCenter);
                        g.DrawString("المطلوب", fontHeader, Brushes.Black, new RectangleF(colDeficit, y + 2, wDeficit, 18), sfCenter);
                        g.DrawString("حد الطلب", fontHeader, Brushes.Black, new RectangleF(colMin, y + 2, wMin, 18), sfCenter);
                        g.DrawString("الرصيد", fontHeader, Brushes.Black, new RectangleF(colStock, y + 2, wStock, 18), sfCenter);
                        g.DrawString("المورد", fontHeader, Brushes.Black, new RectangleF(colSup, y + 2, wSup, 18), sfCenter);
                        g.DrawString("اسم الصنف والشركة", fontHeader, Brushes.Black, new RectangleF(colName, y + 2, wName - 5, 18), sfRight);
                        y += 24;

                        while (printRowIndex < dgShortages.Rows.Count)
                        {
                            if (y + 30 > e2.PageBounds.Height - 40)
                            {
                                e2.HasMorePages = true;
                                return;
                            }

                            var r = dgShortages.Rows[printRowIndex];
                            string pName = r.Cells["ProductName"].Value.ToString();
                            string brandStr = r.Cells["Brand"].Value.ToString();
                            if (brandStr != "-" && !string.IsNullOrWhiteSpace(brandStr)) pName += $" ({brandStr})";

                            string supName = r.Cells["SupplierName"].Value.ToString();
                            string stockStr = r.Cells["CurrentStock"].Value.ToString();
                            string minStr = r.Cells["MinStockLimit"].Value.ToString();
                            string reqStr = r.Cells["DeficitQty"].Value.ToString();
                            string priceStr = r.Cells["PurchasePrice"].Value.ToString();
                            string costStr = r.Cells["TotalCost"].Value.ToString();

                            Brush textBrush = Convert.ToDecimal(r.Cells["CurrentStock"].Value) <= 0 ? Brushes.Red : Brushes.Black;

                            g.DrawString(costStr, fontBody, Brushes.Black, new RectangleF(colCost, y + 2, wCost, 18), sfCenter);
                            g.DrawString(priceStr, fontBody, Brushes.Black, new RectangleF(colPrice, y + 2, wPrice, 18), sfCenter);
                            g.DrawString(reqStr, fontBold, Brushes.Black, new RectangleF(colDeficit, y + 2, wDeficit, 18), sfCenter);
                            g.DrawString(minStr, fontBody, Brushes.DimGray, new RectangleF(colMin, y + 2, wMin, 18), sfCenter);
                            g.DrawString(stockStr, fontBold, textBrush, new RectangleF(colStock, y + 2, wStock, 18), sfCenter);
                            g.DrawString(supName, fontBody, Brushes.Black, new RectangleF(colSup, y + 2, wSup, 18), sfCenter);
                            g.DrawString(pName, fontBody, Brushes.Black, new RectangleF(colName, y + 2, wName - 5, 18), sfRight);

                            y += 20;
                            g.DrawLine(Pens.LightGray, leftMargin, y, rightMargin, y);
                            y += 2;

                            printRowIndex++;
                        }

                        e2.HasMorePages = false;
                        printRowIndex = 0;

                        // Footer Summary
                        y += 10;
                        g.DrawLine(new Pen(Color.Black, 1.2f), leftMargin, y, rightMargin, y);
                        y += 6;
                        int pTotalCount = dgShortages.Rows.Count;
                        int pZeroCount = 0;
                        int pBelowMinCount = 0;
                        decimal pTotalCost = 0m;
                        foreach (DataGridViewRow r in dgShortages.Rows)
                        {
                            if (decimal.TryParse(r.Cells["CurrentStock"].Value?.ToString(), out decimal st) && st <= 0) pZeroCount++;
                            if (decimal.TryParse(r.Cells["MinStockLimit"].Value?.ToString(), out decimal ml) && decimal.TryParse(r.Cells["CurrentStock"].Value?.ToString(), out decimal cs) && ml > 0 && cs <= ml) pBelowMinCount++;
                            if (decimal.TryParse(r.Cells["TotalCost"].Value?.ToString(), out decimal tc)) pTotalCost += tc;
                        }
                        g.DrawString($"إجمالي الأصناف: {pTotalCount} صنف   |   رصيد صفر: {pZeroCount} صنف   |   تحت حد الطلب: {pBelowMinCount} صنف   |   التكلفة التقديرية: {pTotalCost:N2} ج", fontBold, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 20), sfCenter);
                    };

                    using (var dlg = new PrintPreviewDialog { Document = pd, Width = 900, Height = 650, StartPosition = FormStartPosition.CenterParent })
                    {
                        dlg.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء طباعة الكشكول:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// نافذة إضافة صنف أو طلب يدوي لكشكول النواقص
    /// </summary>
    public class FrmAddShortageItem : Form
    {
        private ComboBox cboProduct;
        private TextBox txtProductName;
        private NumericUpDown nudQty;
        private TextBox txtNotes;
        private ComboBox cboSupplier;
        private CheckBox chkCustomProduct;

        public FrmAddShortageItem(int? defaultProductID = null)
        {
            InitializeComponentCustom(defaultProductID);
        }

        private void InitializeComponentCustom(int? defaultProductID)
        {
            this.Text = "➕ إضافة صنف / طلب لكشكول النواقص";
            this.Size = new Size(520, 460);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblTitle = Theme.MakeTitleBar("➕ إضافة صنف / طلب كشكول", "تسجيل طلب صنف غير متوفر أو نقص بناءً على طلب موظف أو عميل.");
            this.Controls.Add(lblTitle);

            chkCustomProduct = new CheckBox
            {
                Text = "صنف غير مسجل بالنظام (كتابة اسم يدوي)",
                Location = new Point(25, 72),
                AutoSize = true,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            chkCustomProduct.CheckedChanged += (s, e) =>
            {
                cboProduct.Enabled = !chkCustomProduct.Checked;
                txtProductName.Enabled = chkCustomProduct.Checked;
                if (chkCustomProduct.Checked) txtProductName.Focus();
            };
            this.Controls.Add(chkCustomProduct);

            var lblProduct = new Label { Text = "اختيار صنف مسجل:", Location = new Point(25, 105), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblProduct);

            cboProduct = new ComboBox
            {
                Location = new Point(25, 130),
                Width = 450,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                FlatStyle = FlatStyle.Flat
            };
            cboProduct.Items.Add(new ComboItem(0, "-- اختر صنف من القائمة --"));
            var dtP = DbHelper.Query("SELECT ProductID, ProductName, ProductCode FROM Products WHERE IsActive=1 ORDER BY ProductName");
            int selectedIdx = 0;
            int idx = 1;
            foreach (DataRow r in dtP.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                string code = r["ProductCode"] != DBNull.Value ? $" [{r["ProductCode"]}]" : "";
                cboProduct.Items.Add(new ComboItem(pid, r["ProductName"].ToString() + code));
                if (defaultProductID.HasValue && defaultProductID.Value == pid)
                {
                    selectedIdx = idx;
                }
                idx++;
            }
            cboProduct.SelectedIndex = selectedIdx;
            this.Controls.Add(cboProduct);

            txtProductName = new TextBox
            {
                Location = new Point(25, 130),
                Width = 450,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Enabled = false
            };
            this.Controls.Add(txtProductName);
            txtProductName.SendToBack();

            var lblSup = new Label { Text = "المورد المقترح (اختياري):", Location = new Point(25, 170), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblSup);

            cboSupplier = new ComboBox
            {
                Location = new Point(25, 195),
                Width = 450,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                FlatStyle = FlatStyle.Flat
            };
            cboSupplier.Items.Add(new ComboItem(0, "-- اختياري / بدون تحديد مورد --"));
            var dtSup = DbHelper.Query("SELECT SupplierID, SupplierName FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName");
            foreach (DataRow r in dtSup.Rows)
            {
                cboSupplier.Items.Add(new ComboItem(Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
            }
            cboSupplier.SelectedIndex = 0;
            this.Controls.Add(cboSupplier);

            var lblQty = new Label { Text = "الكمية المطلوبة لتوفيرها:", Location = new Point(25, 235), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblQty);

            nudQty = new NumericUpDown
            {
                Location = new Point(25, 260),
                Width = 140,
                Minimum = 1,
                Maximum = 100000,
                Value = 1,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold)
            };
            this.Controls.Add(nudQty);

            var lblNotes = new Label { Text = "ملاحظات (اسم العميل / رقم الهاتف / تفاصيل إضافية):", Location = new Point(25, 295), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblNotes);

            txtNotes = new TextBox
            {
                Location = new Point(25, 320),
                Width = 450,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            this.Controls.Add(txtNotes);

            var btnSave = Theme.MakeButton("💾 حفظ في الكشكول", 25, 370, 210, 38, Theme.Success);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            var btnCancel = Theme.MakeButton("❌ إلغاء", 265, 370, 210, 38, Theme.Danger);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string pName = "";
            int? pId = null;

            if (chkCustomProduct.Checked)
            {
                pName = txtProductName.Text.Trim();
                if (string.IsNullOrWhiteSpace(pName))
                {
                    MessageBox.Show("يرجى كتابة اسم الصنف المطلوب", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    pId = ci.ID;
                    pName = ci.Name;
                }
                else
                {
                    MessageBox.Show("يرجى اختيار صنف من القائمة أو تفعيل كتابة اسم يدوي", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            int? supId = (cboSupplier.SelectedItem is ComboItem cis && cis.ID > 0) ? cis.ID : (int?)null;
            string supName = (cboSupplier.SelectedItem is ComboItem cis2 && cis2.ID > 0) ? cis2.Name : null;

            try
            {
                decimal stock = 0m;
                decimal minLimit = 0m;
                if (pId.HasValue && pId.Value > 0)
                {
                    var dtS = DbHelper.Query(@"
                        SELECT p.MinStockLimit, ISNULL(stk.TotalStock, 0) AS TotalStock
                        FROM Products p
                        OUTER APPLY (SELECT SUM(Quantity) AS TotalStock FROM ProductBatches WHERE ProductID = p.ProductID) stk
                        WHERE p.ProductID = @pid",
                        DbHelper.P("@pid", pId.Value));

                    if (dtS.Rows.Count > 0)
                    {
                        stock = Convert.ToDecimal(dtS.Rows[0]["TotalStock"]);
                        minLimit = dtS.Rows[0]["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(dtS.Rows[0]["MinStockLimit"]) : 0m;
                    }
                }

                bool ok = ShortageDAL.AddOrUpdateShortage(
                    productID: pId,
                    productName: pName,
                    requestedQty: nudQty.Value,
                    currentStock: stock,
                    minStockLimit: minLimit,
                    notes: string.IsNullOrWhiteSpace(txtNotes.Text) ? "طلب يدوي" : txtNotes.Text.Trim(),
                    source: "يدوي (طلب عميل/موظف)",
                    status: "جديد",
                    supplierID: supId,
                    supplierName: supName
                );

                if (ok)
                {
                    MessageBox.Show("تمت إضافة الصنف إلى كشكول النواقص بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("تعذر حفظ الطلب في كشكول النواقص", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

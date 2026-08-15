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

        // Header / Action Controls
        private Panel pnlTop;
        private Panel pnlTitleText;
        private FlowLayoutPanel flowActions;

        // KPI Controls
        private Panel pnlKpiContainer;
        private Panel pnlKpiTotal;
        private Panel pnlKpiZero;
        private Panel pnlKpiBelowMin;
        private Panel pnlKpiCost;

        private Label lblKpiTotalVal;
        private Label lblKpiZeroVal;
        private Label lblKpiBelowMinVal;
        private Label lblKpiCostVal;

        // Filter Controls
        private Panel pnlFilterCard;
        private Label lblSearch;
        private TextBox txtSearch;
        private Label lblSup;
        private ComboBox cboSupplierFilter;
        private Label lblCat;
        private ComboBox cboCategoryFilter;
        private Label lblBrand;
        private ComboBox cboBrandFilter;
        private Label lblCondition;
        private ComboBox cboStockCondition;
        private Label lblStatus;
        private ComboBox cboStatusFilter;
        private Button btnResetFilters;

        // Data Table
        private Panel pnlGridWrapper;
        private DataGridView dgShortages;

        // Action Buttons
        private Button btnAddManual;
        private Button btnChangeStatus;
        private Button btnCreatePurchase;
        private Button btnMinStockEdit;
        private Button btnPrint;
        private Button btnExportExcel;
        private Button btnRefresh;

        public FrmShortageNotebook()
        {
            ShortageDAL.EnsureTable();
            InitializeComponentCustom();
            LoadDropdowns();
            LoadData();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "📓 كشكول النواقص وأوامر التوريد";
            this.Size = new Size(1320, 800);
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // ══════════════════════════════════════════════════════════════
            // 1. TOP HEADER & ACTION BAR
            // ══════════════════════════════════════════════════════════════
            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            // Title & Subtitle (Right side)
            pnlTitleText = new Panel
            {
                Dock = DockStyle.Right,
                Width = 430,
                BackColor = Color.Transparent
            };

            var lblMainTitle = new Label
            {
                Text = "📓 كشكول النواقص والطلبات الخاصة",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                AutoSize = true,
                Location = new Point(5, 6)
            };

            var lblSubTitle = new Label
            {
                Text = "متابعة شاملة للأصناف الناقصة، المنتهية، وتجاوز حد الطلب مع فلترة دقيقة وتوريد فوري",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextGray,
                AutoSize = true,
                Location = new Point(5, 33)
            };

            pnlTitleText.Controls.Add(lblMainTitle);
            pnlTitleText.Controls.Add(lblSubTitle);
            pnlTop.Controls.Add(pnlTitleText);

            // Action Buttons Flow (Left side)
            flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 10, 0),
                AutoScroll = false
            };

            btnAddManual = Theme.MakeButton("➕ إضافة طلب/نقص", 0, 0, 140, 36, Theme.Success);
            btnAddManual.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnAddManual.Click += BtnAddManual_Click;

            btnChangeStatus = Theme.MakeButton("📝 تغيير الحالة", 0, 0, 110, 36, Color.FromArgb(41, 128, 185));
            btnChangeStatus.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnChangeStatus.Click += BtnChangeStatus_Click;

            btnCreatePurchase = Theme.MakeButton("🛒 أمر شراء / مشتريات", 0, 0, 145, 36, Theme.Primary);
            btnCreatePurchase.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnCreatePurchase.Click += BtnCreatePurchase_Click;

            btnMinStockEdit = Theme.MakeButton("🎯 حد الطلب", 0, 0, 105, 36, Color.FromArgb(13, 148, 136));
            btnMinStockEdit.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnMinStockEdit.Click += (s, e) => {
                new FrmMinStockEdit().ShowDialog();
                LoadData();
            };

            btnPrint = Theme.MakeButton("🖨️ طباعة", 0, 0, 95, 36, Color.FromArgb(142, 68, 173));
            btnPrint.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPrint.Click += BtnPrint_Click;

            btnExportExcel = Theme.MakeButton("📊 إكسل", 0, 0, 90, 36, Color.FromArgb(46, 117, 89));
            btnExportExcel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnExportExcel.Click += BtnExportExcel_Click;

            btnRefresh = Theme.MakeButton("🔄 تحديث", 0, 0, 85, 36, Color.FromArgb(70, 80, 95));
            btnRefresh.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
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

            pnlTop.Controls.Add(flowActions);
            this.Controls.Add(pnlTop);

            // ══════════════════════════════════════════════════════════════
            // 2. KPI SUMMARY METRIC TILES
            // ══════════════════════════════════════════════════════════════
            pnlKpiContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.BgMain,
                Padding = new Padding(15, 6, 15, 6)
            };

            var tableKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tableKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            tableKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            tableKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            tableKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

            pnlKpiTotal = CreateKpiCard("📋 إجمالي الأصناف الناقصة", "0 صنف", Color.FromArgb(30, 41, 59), Color.FromArgb(241, 245, 249), out lblKpiTotalVal);
            pnlKpiZero = CreateKpiCard("🔴 أصناف نفدت بالكامل (رصيد 0)", "0 صنف", Color.FromArgb(220, 38, 38), Color.FromArgb(254, 242, 242), out lblKpiZeroVal);
            pnlKpiBelowMin = CreateKpiCard("⚠️ أصناف بلغت أو تجاوزت حد الطلب", "0 صنف", Color.FromArgb(217, 119, 6), Color.FromArgb(254, 243, 199), out lblKpiBelowMinVal);
            pnlKpiCost = CreateKpiCard("💰 تكلفة التوفير التقديرية", "0.00 ج", Color.FromArgb(13, 148, 136), Color.FromArgb(240, 253, 250), out lblKpiCostVal);

            tableKpi.Controls.Add(pnlKpiTotal, 0, 0);
            tableKpi.Controls.Add(pnlKpiZero, 1, 0);
            tableKpi.Controls.Add(pnlKpiBelowMin, 2, 0);
            tableKpi.Controls.Add(pnlKpiCost, 3, 0);

            pnlKpiContainer.Controls.Add(tableKpi);
            this.Controls.Add(pnlKpiContainer);

            // ══════════════════════════════════════════════════════════════
            // 3. ADVANCED MULTI-DIMENSIONAL FILTER STRIP
            // ══════════════════════════════════════════════════════════════
            pnlFilterCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 6, 15, 6)
            };

            // Controls
            lblSearch = new Label { Text = "🔍 بحث:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtSearch = new TextBox
            {
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => LoadData();

            lblSup = new Label { Text = "🏢 المورد:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboSupplierFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                FlatStyle = FlatStyle.Flat
            };
            cboSupplierFilter.SelectedIndexChanged += (s, e) => LoadData();

            lblCat = new Label { Text = "📁 القسم:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboCategoryFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                FlatStyle = FlatStyle.Flat
            };
            cboCategoryFilter.SelectedIndexChanged += (s, e) => LoadData();

            lblBrand = new Label { Text = "🏭 الشركة/الماركة:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboBrandFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                FlatStyle = FlatStyle.Flat
            };
            cboBrandFilter.SelectedIndexChanged += (s, e) => LoadData();

            lblCondition = new Label { Text = "🎯 نوع النواقص:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboStockCondition = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                FlatStyle = FlatStyle.Flat
            };
            cboStockCondition.Items.AddRange(new object[]
            {
                "الكل (جميع الأصناف الناقصة والمسجلة)",
                "⚠️ وصل أو نزل عن حد الطلب (<= MinStock)",
                "🔴 رصيد صفر أو سالب فقط (نفد بالكامل)",
                "🟡 تحت حد الطلب ومتبقي رصيد (0 < Stock <= Min)"
            });
            cboStockCondition.SelectedIndex = 0;
            cboStockCondition.SelectedIndexChanged += (s, e) => LoadData();

            lblStatus = new Label { Text = "📊 الحالة:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                FlatStyle = FlatStyle.Flat
            };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "جديد", "تم الطلب", "تم التوفير", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => LoadData();

            btnResetFilters = Theme.MakeButton("🧹 تفريغ الفلاتر", 0, 0, 115, 30, Color.FromArgb(100, 116, 139));
            btnResetFilters.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnResetFilters.Click += (s, e) => ResetFilters();

            pnlFilterCard.Controls.AddRange(new Control[] {
                lblSearch, txtSearch,
                lblSup, cboSupplierFilter,
                lblCat, cboCategoryFilter,
                lblBrand, cboBrandFilter,
                lblCondition, cboStockCondition,
                lblStatus, cboStatusFilter,
                btnResetFilters
            });
            this.Controls.Add(pnlFilterCard);

            // ══════════════════════════════════════════════════════════════
            // 4. MAIN DATA GRID
            // ══════════════════════════════════════════════════════════════
            pnlGridWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 6, 15, 12)
            };

            dgShortages = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
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
                    BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(38, 44, 58) : Color.FromArgb(245, 248, 252),
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

            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShortageID", Visible = false });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierID", Visible = false });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 65 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 210 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "القسم / التصنيف", FillWeight = 95 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Brand", HeaderText = "الشركة / الماركة", FillWeight = 95 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "المورد", FillWeight = 110 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStock", HeaderText = "الرصيد الحالي", FillWeight = 85 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinStockLimit", HeaderText = "حد الطلب", FillWeight = 75 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "DeficitQty", HeaderText = "المطلوب توفيره", FillWeight = 90 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", HeaderText = "سعر الشراء", FillWeight = 80 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "إجمالي التكلفة", FillWeight = 95 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 80 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "المصدر", FillWeight = 90 });
            dgShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات", FillWeight = 120 });

            // Context Menu
            var cms = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };
            cms.Items.Add("➕ إضافة طلب/نقص جديد للكشكول", null, (s, e) => BtnAddManual_Click(null, null));
            cms.Items.Add("📝 تغيير حالة الصنف (جديد / تم الطلب / تم التوفير)", null, (s, e) => BtnChangeStatus_Click(null, null));
            cms.Items.Add("🛒 إنشاء أمر / فاتورة شراء لهذا الصنف", null, (s, e) => BtnCreatePurchase_Click(null, null));
            cms.Items.Add("🎯 تعديل حد الطلب للأصناف", null, (s, e) => {
                new FrmMinStockEdit().ShowDialog();
                LoadData();
            });
            cms.Items.Add("🏷️ فتح بطاقة الصنف", null, (s, e) => {
                if (dgShortages.SelectedRows.Count > 0)
                {
                    int pId = Convert.ToInt32(dgShortages.SelectedRows[0].Cells["ProductID"].Value);
                    if (pId > 0)
                    {
                        if (new FrmProductCard(pId).ShowDialog() == DialogResult.OK) LoadData();
                    }
                }
            });
            cms.Items.Add("🖨️ طباعة قائمة النواقص المحددة", null, (s, e) => BtnPrint_Click(null, null));
            dgShortages.ContextMenuStrip = cms;

            dgShortages.CellDoubleClick += (s, e) => {
                if (dgShortages.SelectedRows.Count > 0)
                {
                    BtnChangeStatus_Click(null, null);
                }
            };

            pnlGridWrapper.Controls.Add(dgShortages);
            this.Controls.Add(pnlGridWrapper);

            // Reorder controls for proper docking
            pnlTop.SendToBack();
            pnlKpiContainer.SendToBack();
            pnlFilterCard.SendToBack();
            pnlGridWrapper.BringToFront();

            this.Resize += (s, e) => LayoutFilterControls();
            LayoutFilterControls();

            Theme.ApplyFormRTL(this);
        }

        private void LayoutFilterControls()
        {
            if (pnlFilterCard == null || txtSearch == null) return;
            int totalW = pnlFilterCard.ClientSize.Width - 30;
            int colW = Math.Max(180, (totalW - 60) / 4);

            // Row 1 (Y = 8): Search, Supplier, Category, Brand
            int x = 15;
            lblSearch.Location = new Point(x, 12);
            txtSearch.Location = new Point(x + 60, 8);
            txtSearch.Width = colW - 65;

            x += colW + 15;
            lblSup.Location = new Point(x, 12);
            cboSupplierFilter.Location = new Point(x + 55, 8);
            cboSupplierFilter.Width = colW - 60;

            x += colW + 15;
            lblCat.Location = new Point(x, 12);
            cboCategoryFilter.Location = new Point(x + 55, 8);
            cboCategoryFilter.Width = colW - 60;

            x += colW + 15;
            lblBrand.Location = new Point(x, 12);
            cboBrandFilter.Location = new Point(x + 105, 8);
            cboBrandFilter.Width = colW - 110;

            // Row 2 (Y = 46): Stock Condition, Status, Reset
            lblCondition.Location = new Point(15, 48);
            cboStockCondition.Location = new Point(115, 44);
            cboStockCondition.Width = 270;

            lblStatus.Location = new Point(400, 48);
            cboStatusFilter.Location = new Point(455, 44);
            cboStatusFilter.Width = 140;

            btnResetFilters.Location = new Point(610, 42);
        }

        private Panel CreateKpiCard(string title, string initialValue, Color accentColor, Color bgTint, out Label valLabel)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(33, 38, 52) : bgTint,
                Margin = new Padding(4, 2, 4, 2),
                Padding = new Padding(8, 5, 8, 5)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.TopRight
            };

            valLabel = new Label
            {
                Text = initialValue,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnl.Controls.Add(valLabel);
            pnl.Controls.Add(lblTitle);

            pnl.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(180, accentColor), 1.5f))
                {
                    var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            return pnl;
        }

        private void LoadDropdowns()
        {
            try
            {
                // Suppliers
                cboSupplierFilter.Items.Clear();
                cboSupplierFilter.Items.Add(new ComboItem(0, "-- كل الموردين --"));
                var dtSup = DbHelper.Query("SELECT SupplierID, SupplierName FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName");
                foreach (DataRow r in dtSup.Rows)
                {
                    cboSupplierFilter.Items.Add(new ComboItem(Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
                }
                cboSupplierFilter.SelectedIndex = 0;

                // Categories
                cboCategoryFilter.Items.Clear();
                cboCategoryFilter.Items.Add(new ComboItem(0, "-- كل الأقسام --"));
                var dtCat = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName");
                foreach (DataRow r in dtCat.Rows)
                {
                    cboCategoryFilter.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                }
                cboCategoryFilter.SelectedIndex = 0;

                // Brands / Producing Companies
                cboBrandFilter.Items.Clear();
                cboBrandFilter.Items.Add("-- كل الشركات / الماركات --");
                var brands = ShortageDAL.GetAvailableBrands();
                foreach (string b in brands)
                {
                    cboBrandFilter.Items.Add(b);
                }
                cboBrandFilter.SelectedIndex = 0;
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
            cboStockCondition.SelectedIndex = 0;
            cboStatusFilter.SelectedIndex = 0;
            LoadData();
        }

        public void LoadData()
        {
            dgShortages.Rows.Clear();

            int? supId = (cboSupplierFilter.SelectedItem is ComboItem cis && cis.ID > 0) ? cis.ID : (int?)null;
            int? catId = (cboCategoryFilter.SelectedItem is ComboItem cic && cic.ID > 0) ? cic.ID : (int?)null;
            string brand = cboBrandFilter.SelectedItem?.ToString();
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
                            r["ProductID"],
                            r["ShortageID"],
                            r["SupplierID"],
                            r["ProductCode"],
                            r["ProductName"],
                            r["CategoryName"],
                            r["Brand"],
                            r["SupplierName"],
                            stock.ToString("N2"),
                            minLimit.ToString("N2"),
                            deficit.ToString("N2"),
                            buyPrice.ToString("N2"),
                            totalCost.ToString("N2"),
                            st,
                            r["Source"],
                            r["Notes"]
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

                // Update KPI Cards
                lblKpiTotalVal.Text = $"{dgShortages.Rows.Count} صنف";
                lblKpiZeroVal.Text = $"{zeroCount} صنف";
                lblKpiBelowMinVal.Text = $"{belowMinCount} صنف";
                lblKpiCostVal.Text = $"{totalDeficitCost:N2} ج";
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmShortageNotebook.LoadData", ex);
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
                        g.DrawString($"إجمالي الأصناف: {lblKpiTotalVal.Text}   |   رصيد صفر: {lblKpiZeroVal.Text}   |   تحت حد الطلب: {lblKpiBelowMinVal.Text}   |   التكلفة التقديرية: {lblKpiCostVal.Text}", fontBold, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 20), sfCenter);
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

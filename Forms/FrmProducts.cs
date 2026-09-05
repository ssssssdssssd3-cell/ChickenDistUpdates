using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmProducts : Form
    {
        private DataGridView dgProducts;
        private TextBox txtSearch;
        private ComboBox cboSearchType, cboCategory, cboBrand, cboDealStatus, cboStockStatus, cboStatus, cboShelf;
        private Label lblItemCount, lblSearch, lblCat, lblBrand, lblDealStatus, lblStockStatus, lblStatus, lblShelf;
        private Button btnResetFilters;
        private Button btnNew, btnEdit, btnMovement, btnDelete;
        private int _selectedID = 0;
        private DataTable _dtProducts;
        private Timer _searchTimer;

        // Caches for transaction status and stock quantities
        private HashSet<int> _transactedProductIDs = new HashSet<int>();
        private HashSet<int> _soldProductIDs = new HashSet<int>();
        private HashSet<int> _purchasedProductIDs = new HashSet<int>();
        private Dictionary<int, decimal> _stockTotals = new Dictionary<int, decimal>();

        public FrmProducts()
        {
            _searchTimer = new Timer { Interval = 220 };
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); FilterProducts(); };
            InitUI();
            LoadLookupsAndCombos();
            LoadProducts();
            FrmQuickAdd.ProductSaved += FrmProducts_ProductSaved;
            this.FormClosing += (s, e) => FrmQuickAdd.ProductSaved -= FrmProducts_ProductSaved;
        }

        private void FrmProducts_ProductSaved(object sender, EventArgs e)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        ProductCache.Refresh();
                        LoadProducts();
                    }
                    catch { }
                }));
            }
        }

        private void InitUI()
        {
            this.Text = "إدارة الأصناف";
            this.Size = new Size(1250, 740);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Title Bar
            var titleBar = Theme.MakeTitleBar("إدارة الأصناف", "قائمة عرض وبحث الأصناف والأسعار والشركات المنتجة وحركة التعامل والمخزون");
            this.Controls.Add(titleBar);

            // Search and Filters Panel (Top)
            var pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 98, 
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(12, 8, 12, 8)
            };
            Theme.StyleSearchHeaderPanel(pnlHeader);

            // Row 1 Controls
            lblSearch = new Label 
            { 
                Text = "🔍 نوع البحث:", 
                AutoSize = true, 
                ForeColor = Theme.TextSearchLabel, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            cboSearchType = new ComboBox
            {
                Width = 145,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cboSearchType.Items.AddRange(new object[] {
                "🔍 بحث شامل (الكل)",
                "🔢 كود الصنف فقط",
                "🏷️ اسم الصنف فقط",
                "📦 الباركود / كود الميزان",
                "⚙️ رقم القطعة (Part No)"
            });
            cboSearchType.SelectedIndex = 0;
            cboSearchType.SelectedIndexChanged += (s, e) => FilterProducts();
            
            txtSearch = new TextBox 
            { 
                Width = 200, 
                BackColor = Color.White, 
                ForeColor = Color.FromArgb(15, 23, 42), 
                BorderStyle = BorderStyle.FixedSingle, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            txtSearch.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };

            lblCat = new Label 
            { 
                Text = "📂 التصنيف:", 
                AutoSize = true, 
                ForeColor = Theme.TextSearchLabel, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            cboCategory = new ComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cboCategory.SelectedIndexChanged += (s, e) => FilterProducts();

            lblBrand = new Label 
            { 
                Text = "🏭 الشركة/الماركة:", 
                AutoSize = true, 
                ForeColor = Theme.TextSearchLabel, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            cboBrand = new ComboBox
            {
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cboBrand.SelectedIndexChanged += (s, e) => FilterProducts();

            lblShelf = new Label 
            { 
                Text = "🏷️ موقع الرف:", 
                AutoSize = true, 
                ForeColor = Theme.TextSearchLabel, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            cboShelf = new ComboBox
            {
                Width = 135,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cboShelf.SelectedIndexChanged += (s, e) => FilterProducts();

            btnResetFilters = Theme.MakeButton("🔄 مسح الفلاتر", Color.FromArgb(100, 116, 139));
            btnResetFilters.Width = 110;
            btnResetFilters.Height = 28;
            btnResetFilters.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnResetFilters.Click += (s, e) => ResetAllFilters();

            // Row 2 Controls
            lblDealStatus = new Label 
            { 
                Text = "🔄 حركة التعامل:", 
                AutoSize = true, 
                ForeColor = Theme.TextSearchLabel, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            cboDealStatus = new ComboBox
            {
                Width = 185,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cboDealStatus.Items.AddRange(new object[] {
                "جميع الأصناف (الكل)",
                "✅ أصناف تم التعامل عليها (حركات)",
                "💤 أصناف راكدة (لم يتم التعامل)",
                "🛒 أصناف مباعة (فواتير بيع)",
                "📥 أصناف مشتراة (فواتير شراء)"
            });
            cboDealStatus.SelectedIndex = 0;
            cboDealStatus.SelectedIndexChanged += (s, e) => FilterProducts();

            lblStockStatus = new Label 
            { 
                Text = "📦 حالة المخزون:", 
                AutoSize = true, 
                ForeColor = Theme.TextSearchLabel, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            cboStockStatus = new ComboBox
            {
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cboStockStatus.Items.AddRange(new object[] {
                "جميع حالات المخزون",
                "🟢 متوفر بالمخزن (رصيد > 0)",
                "🔴 نفد من المخزن (رصيد = 0)",
                "⚠️ رصيد سالب (عجز < 0)",
                "🎯 تحت حد الطلب (نواقص)"
            });
            cboStockStatus.SelectedIndex = 0;
            cboStockStatus.SelectedIndexChanged += (s, e) => FilterProducts();

            lblStatus = new Label 
            { 
                Text = "⚡ النشاط:", 
                AutoSize = true, 
                ForeColor = Theme.TextSearchLabel, 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            cboStatus = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cboStatus.Items.AddRange(new object[] { "الكل", "النشطة فقط", "المعطلة فقط" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => FilterProducts();

            lblItemCount = new Label
            {
                Text = "📊 الأصناف: 0",
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 83, 9),
                BackColor = Color.FromArgb(254, 243, 199),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4)
            };

            pnlHeader.Controls.AddRange(new Control[] { 
                lblSearch, cboSearchType, txtSearch, 
                lblCat, cboCategory, 
                lblBrand, cboBrand, 
                lblShelf, cboShelf, 
                btnResetFilters,
                lblDealStatus, cboDealStatus,
                lblStockStatus, cboStockStatus,
                lblStatus, cboStatus,
                lblItemCount 
            });

            Action layoutHeader = () =>
            {
                if (pnlHeader.ClientSize.Width <= 0) return;
                int w = pnlHeader.ClientSize.Width;

                // --- Row 1 (Y = 12) ---
                int curX1 = w - 12;

                lblSearch.Location = new Point(curX1 - lblSearch.PreferredWidth, 15);
                curX1 -= (lblSearch.PreferredWidth + 5);

                cboSearchType.Location = new Point(curX1 - cboSearchType.Width, 12);
                curX1 -= (cboSearchType.Width + 6);

                txtSearch.Location = new Point(curX1 - txtSearch.Width, 12);
                curX1 -= (txtSearch.Width + 15);

                lblCat.Location = new Point(curX1 - lblCat.PreferredWidth, 15);
                curX1 -= (lblCat.PreferredWidth + 5);

                cboCategory.Location = new Point(curX1 - cboCategory.Width, 12);
                curX1 -= (cboCategory.Width + 15);

                lblBrand.Location = new Point(curX1 - lblBrand.PreferredWidth, 15);
                curX1 -= (lblBrand.PreferredWidth + 5);

                cboBrand.Location = new Point(curX1 - cboBrand.Width, 12);
                curX1 -= (cboBrand.Width + 15);

                lblShelf.Location = new Point(curX1 - lblShelf.PreferredWidth, 15);
                curX1 -= (lblShelf.PreferredWidth + 5);

                cboShelf.Location = new Point(curX1 - cboShelf.Width, 12);
                curX1 -= (cboShelf.Width + 15);

                btnResetFilters.Location = new Point(15, 12);

                // --- Row 2 (Y = 54) ---
                int curX2 = w - 12;

                lblDealStatus.Location = new Point(curX2 - lblDealStatus.PreferredWidth, 57);
                curX2 -= (lblDealStatus.PreferredWidth + 5);

                cboDealStatus.Location = new Point(curX2 - cboDealStatus.Width, 54);
                curX2 -= (cboDealStatus.Width + 15);

                lblStockStatus.Location = new Point(curX2 - lblStockStatus.PreferredWidth, 57);
                curX2 -= (lblStockStatus.PreferredWidth + 5);

                cboStockStatus.Location = new Point(curX2 - cboStockStatus.Width, 54);
                curX2 -= (cboStockStatus.Width + 15);

                lblStatus.Location = new Point(curX2 - lblStatus.PreferredWidth, 57);
                curX2 -= (lblStatus.PreferredWidth + 5);

                cboStatus.Location = new Point(curX2 - cboStatus.Width, 54);

                lblItemCount.Location = new Point(15, 54);
            };

            pnlHeader.Resize += (s, e) => layoutHeader();
            pnlHeader.HandleCreated += (s, e) => layoutHeader();
            this.Controls.Add(pnlHeader);

            // Footer Panel (Bottom FlowLayoutPanel)
            var pnlFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            btnNew = Theme.MakeButton("➕ إضافة صنف جديد", Theme.Success);
            btnNew.Width = 140;
            btnNew.Click += (s, e) => {
                if (!Session.CanAdd("Products"))
                {
                    MessageBox.Show("❌ عفوًا: لا تملك صلاحية إضافة أصناف جديدة!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (new FrmProductCard(0).ShowDialog() == DialogResult.OK)
                {
                    ProductCache.Refresh();
                    LoadProducts();
                }
            };

            btnEdit = Theme.MakeButton("📝 تعديل ومعاينة", Theme.Accent);
            btnEdit.Width = 135;
            btnEdit.Click += BtnEdit_Click;

            btnMovement = Theme.MakeButton("📈 حركة الصنف", Color.FromArgb(41, 128, 185));
            btnMovement.Width = 130;
            btnMovement.Click += (s, e) => OpenSelectedProductMovement();

            btnDelete = Theme.MakeButton("🗑 إيقاف الصنف", Theme.Danger);
            btnDelete.Width = 115;
            btnDelete.Click += BtnDelete_Click;

            var btnQuickAdd = Theme.MakeButton("⚡ إدخال سريع", Color.FromArgb(60, 100, 60));
            btnQuickAdd.Width = 120;
            btnQuickAdd.Click += (s, e) => {
                if (new FrmQuickAdd().ShowDialog() == DialogResult.OK)
                {
                    ProductCache.Refresh();
                    LoadProducts();
                }
            };

            var btnImportExcel = Theme.MakeButton("📥 استيراد إكسل", Theme.Primary);
            btnImportExcel.Width = 120;
            btnImportExcel.Click += (s, e) => {
                if (!PromptImportPassword(this)) return;
                if (new FrmImportProducts().ShowDialog() == DialogResult.OK)
                {
                    ProductCache.Refresh();
                    LoadProducts();
                }
            };

            var btnPrintBarcode = Theme.MakeButton("🏷️ طباعة الباركود", Theme.Primary);
            btnPrintBarcode.Width = 130;
            btnPrintBarcode.Click += BtnPrintBarcode_Click;

            var btnPricePoster = Theme.MakeButton("📋 لستة الأصناف", Color.FromArgb(120, 80, 140));
            btnPricePoster.Width = 130;
            btnPricePoster.Click += (s, e) => new FrmPricePoster().ShowDialog();

            var btnMinStock = Theme.MakeButton("🎯 حد الطلب والنواقص", Color.FromArgb(13, 148, 136));
            btnMinStock.Width = 145;
            btnMinStock.Click += (s, e) => {
                new FrmMinStockEdit().ShowDialog();
                ProductCache.Refresh();
                LoadProducts();
            };

            var btnAddToShortages = Theme.MakeButton("📓 إضافة للنواقص", Color.FromArgb(217, 119, 6));
            btnAddToShortages.Width = 140;
            btnAddToShortages.Click += (s, e) => AddSelectedToShortages();

            pnlFooter.Controls.AddRange(new Control[] { 
                btnNew, 
                btnEdit, 
                btnMovement, 
                btnDelete, 
                btnQuickAdd, 
                btnAddToShortages,
                btnMinStock, 
                btnImportExcel, 
                btnPrintBarcode, 
                btnPricePoster 
            });
            this.Controls.Add(pnlFooter);

            // Grid (Center)
            dgProducts = new DataGridView
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
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 26 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartNumber", HeaderText = "رقم القطعة", FillWeight = 26 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 160 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 38 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Brand", HeaderText = "الشركة / الماركة", FillWeight = 38 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLocation", HeaderText = "الرف", FillWeight = 24 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 20 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "SalePrice", 
                HeaderText = "سعر البيع", 
                FillWeight = 42, 
                MinimumWidth = 115,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter, 
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) 
                } 
            });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "TotalStock", 
                HeaderText = "الرصيد", 
                FillWeight = 28, 
                MinimumWidth = 85,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter 
                } 
            });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "DealStatus", HeaderText = "حركة التعامل", FillWeight = 32, MinimumWidth = 90 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "نشط", FillWeight = 16, MinimumWidth = 50 });
            
            Theme.AdjustGridHeaders(dgProducts);

            var cmsProducts = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };
            cmsProducts.Items.Add("🔍 تعديل ومعاينة بطاقة الصنف (F4)", null, (s, e) => {
                if (dgProducts.SelectedRows.Count > 0)
                {
                    int productID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
                    if (new FrmProductCard(productID).ShowDialog() == DialogResult.OK)
                    {
                        ProductCache.Refresh();
                        LoadProducts();
                    }
                }
            });
            cmsProducts.Items.Add("📈 كارت حركة الصنف ومبيعاته", null, (s, e) => OpenSelectedProductMovement());
            cmsProducts.Items.Add("🏷️ طباعة باركود الصنف", null, (s, e) => {
                if (dgProducts.SelectedRows.Count > 0)
                {
                    int pid = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
                    var dt = DbHelper.Query("SELECT ProductName, ProductCode, InternationalCode, ShelfLocation, SalePrice FROM Products WHERE ProductID = @pid", DbHelper.P("@pid", pid));
                    if (dt.Rows.Count > 0)
                    {
                        string name = dt.Rows[0]["ProductName"]?.ToString() ?? "";
                        string code = dt.Rows[0]["ProductCode"]?.ToString() ?? "";
                        string intCode = dt.Rows[0]["InternationalCode"]?.ToString() ?? "";
                        string loc = dt.Rows[0]["ShelfLocation"]?.ToString() ?? "";
                        decimal price = dt.Rows[0]["SalePrice"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["SalePrice"]) : 0m;
                        using (var frm = new FrmPrintProductBarcode(pid, name, code, intCode, price, loc))
                        {
                            frm.ShowDialog(this);
                        }
                    }
                }
            });
            cmsProducts.Items.Add("📊 فحص رصيد الصنف في المخازن", null, (s, e) => {
                if (dgProducts.SelectedRows.Count > 0)
                {
                    int pid = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
                    string name = dgProducts.SelectedRows[0].Cells["ProductName"].Value?.ToString() ?? "";
                    var dt = DbHelper.Query(@"
                        SELECT w.WarehouseName, ISNULL(ps.Quantity, 0) AS Qty
                        FROM Warehouses w
                        LEFT JOIN ProductStock ps ON w.WarehouseID = ps.WarehouseID AND ps.ProductID = @pid",
                        DbHelper.P("@pid", pid));
                    string msg = $"📦 تفاصيل رصيد الصنف: {name}\n" + new string('-', 40) + "\n";
                    decimal totalStock = 0;
                    foreach (DataRow r in dt.Rows)
                    {
                        decimal q = Convert.ToDecimal(r["Qty"]);
                        totalStock += q;
                        msg += $"• {r["WarehouseName"]}: {q:N2}\n";
                    }
                    msg += new string('-', 40) + $"\nالإجمالي الكلي: {totalStock:N2}";
                    MessageBox.Show(msg, "رصيد المخازن", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
            cmsProducts.Items.Add(new ToolStripSeparator());
            cmsProducts.Items.Add("📓 إضافة الصنف لكشكول النواقص", null, (s, e) => AddSelectedToShortages());
            cmsProducts.Items.Add("🎯 تعديل حد الطلب والنواقص", null, (s, e) => {
                new FrmMinStockEdit().ShowDialog();
                ProductCache.Refresh();
                LoadProducts();
            });
            cmsProducts.Items.Add(new ToolStripSeparator());
            cmsProducts.Items.Add("📋 نسخ كود / باركود الصنف", null, (s, e) => {
                if (dgProducts.SelectedRows.Count > 0 && dgProducts.Columns.Contains("ProductCode"))
                {
                    string code = dgProducts.SelectedRows[0].Cells["ProductCode"].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(code))
                    {
                        Clipboard.SetText(code);
                    }
                }
            });
            dgProducts.ContextMenuStrip = cmsProducts;
            dgProducts.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = dgProducts.HitTest(e.X, e.Y);
                    if (hit.RowIndex >= 0)
                    {
                        dgProducts.ClearSelection();
                        dgProducts.Rows[hit.RowIndex].Selected = true;
                        dgProducts.CurrentCell = dgProducts.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
                    }
                }
            };

            dgProducts.SelectionChanged += DgProducts_SelectionChanged;
            dgProducts.CellDoubleClick += (s, e) => {
                if (dgProducts.SelectedRows.Count > 0)
                {
                    int productID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
                    if (new FrmProductCard(productID).ShowDialog() == DialogResult.OK)
                    {
                        ProductCache.Refresh();
                        LoadProducts();
                    }
                }
            };

            this.Controls.Add(dgProducts);

            // Send title bar to back so layout docking works correctly
            titleBar.SendToBack();
            pnlHeader.SendToBack();
            pnlFooter.SendToBack();
            dgProducts.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private void ResetAllFilters()
        {
            txtSearch.Text = "";
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
            if (cboBrand.Items.Count > 0) cboBrand.SelectedIndex = 0;
            if (cboShelf.Items.Count > 0) cboShelf.SelectedIndex = 0;
            if (cboDealStatus.Items.Count > 0) cboDealStatus.SelectedIndex = 0;
            if (cboStockStatus.Items.Count > 0) cboStockStatus.SelectedIndex = 0;
            if (cboStatus.Items.Count > 0) cboStatus.SelectedIndex = 0;
            FilterProducts();
        }

        private void LoadLookupsAndCombos()
        {
            LoadCategoriesCombo();
            LoadBrandsCombo();
            LoadShelvesCombo();
        }

        private void LoadCategoriesCombo()
        {
            cboCategory.Items.Clear();
            cboCategory.Items.Add(new ComboItem(0, "جميع التصنيفات"));
            try
            {
                DataTable dtCat = CategoryDAL.GetAll(true);
                foreach (DataRow r in dtCat.Rows)
                {
                    cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                }
            }
            catch { }
            cboCategory.SelectedIndex = 0;
        }

        private void LoadBrandsCombo()
        {
            cboBrand.Items.Clear();
            cboBrand.Items.Add("جميع الشركات والماركات");
            try
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var dtProducers = LookupDAL.GetAll("ProducerCompanies", "ProducerName");
                foreach (DataRow r in dtProducers.Rows)
                {
                    string p = r["ProducerName"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(p) && set.Add(p)) cboBrand.Items.Add(p);
                }

                var dtBrands = LookupDAL.GetAll("Brands", "BrandName");
                foreach (DataRow r in dtBrands.Rows)
                {
                    string b = r["BrandName"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(b) && set.Add(b)) cboBrand.Items.Add(b);
                }
            }
            catch { }
            cboBrand.SelectedIndex = 0;
        }

        private void LoadShelvesCombo()
        {
            cboShelf.Items.Clear();
            cboShelf.Items.Add("جميع أماكن الرفوف");
            try
            {
                var dtShelves = LookupDAL.GetAll("ShelfLocations", "ShelfName");
                foreach (DataRow r in dtShelves.Rows)
                {
                    string s = r["ShelfName"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(s) && !cboShelf.Items.Contains(s))
                        cboShelf.Items.Add(s);
                }
            }
            catch { }
            cboShelf.SelectedIndex = 0;
        }

        private void LoadTransactionAndStockData()
        {
            try
            {
                _transactedProductIDs.Clear();
                _soldProductIDs.Clear();
                _purchasedProductIDs.Clear();
                _stockTotals.Clear();

                // 1. Sold items
                var dtSold = DbHelper.Query("SELECT DISTINCT ProductID FROM SaleItems WHERE ProductID IS NOT NULL");
                foreach (DataRow r in dtSold.Rows)
                {
                    if (r[0] != DBNull.Value)
                    {
                        int pid = Convert.ToInt32(r[0]);
                        _soldProductIDs.Add(pid);
                        _transactedProductIDs.Add(pid);
                    }
                }

                // 2. Purchased items
                var dtPurchased = DbHelper.Query("SELECT DISTINCT ProductID FROM PurchaseItems WHERE ProductID IS NOT NULL");
                foreach (DataRow r in dtPurchased.Rows)
                {
                    if (r[0] != DBNull.Value)
                    {
                        int pid = Convert.ToInt32(r[0]);
                        _purchasedProductIDs.Add(pid);
                        _transactedProductIDs.Add(pid);
                    }
                }

                // 3. Other transactions (Returns, Adjustments, Transfers)
                var dtOther = DbHelper.Query(@"
                    SELECT DISTINCT ProductID FROM ReturnItems WHERE ProductID IS NOT NULL
                    UNION
                    SELECT DISTINCT ProductID FROM StockAdjustments WHERE ProductID IS NOT NULL
                    UNION
                    SELECT DISTINCT ProductID FROM WarehouseTransferItems WHERE ProductID IS NOT NULL");
                foreach (DataRow r in dtOther.Rows)
                {
                    if (r[0] != DBNull.Value)
                    {
                        int pid = Convert.ToInt32(r[0]);
                        _transactedProductIDs.Add(pid);
                    }
                }

                // 4. Stock totals
                var dtStock = DbHelper.Query("SELECT ProductID, SUM(Quantity) AS TotalQty FROM ProductStock GROUP BY ProductID");
                foreach (DataRow r in dtStock.Rows)
                {
                    if (r["ProductID"] != DBNull.Value)
                    {
                        int pid = Convert.ToInt32(r["ProductID"]);
                        decimal qty = r["TotalQty"] != DBNull.Value ? Convert.ToDecimal(r["TotalQty"]) : 0m;
                        _stockTotals[pid] = qty;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmProducts.LoadTransactionAndStockData", ex);
            }
        }

        private void LoadProducts()
        {
            LoadTransactionAndStockData();
            _dtProducts = ProductCache.GetAll();

            // Refresh brands combo if dynamic items in Products table exist
            try
            {
                if (_dtProducts != null)
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 1; i < cboBrand.Items.Count; i++) set.Add(cboBrand.Items[i].ToString());

                    foreach (DataRow r in _dtProducts.Rows)
                    {
                        if (r.Table.Columns.Contains("Brand") && r["Brand"] != DBNull.Value)
                        {
                            string b = r["Brand"].ToString().Trim();
                            if (!string.IsNullOrEmpty(b) && set.Add(b)) cboBrand.Items.Add(b);
                        }
                        if (r.Table.Columns.Contains("ProducerCompany") && r["ProducerCompany"] != DBNull.Value)
                        {
                            string p = r["ProducerCompany"].ToString().Trim();
                            if (!string.IsNullOrEmpty(p) && set.Add(p)) cboBrand.Items.Add(p);
                        }
                    }
                }
            }
            catch { }

            FilterProducts();
        }

        private void FilterProducts()
        {
            if (_dtProducts == null) return;

            string query = txtSearch.Text?.Trim().ToLower() ?? "";

            int selectedCatID = 0;
            if (cboCategory.SelectedItem is ComboItem ciCat)
            {
                selectedCatID = ciCat.ID;
            }

            string selectedBrand = cboBrand.SelectedIndex > 0 ? cboBrand.SelectedItem?.ToString()?.Trim().ToLower() : "";
            string selectedShelf = cboShelf.SelectedIndex > 0 ? cboShelf.SelectedItem?.ToString()?.Trim().ToLower() : "";
            int selectedDeal = cboDealStatus.SelectedIndex; // 0=الكل, 1=تم التعامل, 2=راكد, 3=مباع, 4=مشترى
            int selectedStock = cboStockStatus.SelectedIndex; // 0=الكل, 1=متوفر >0, 2=نفد =0, 3=سالب <0, 4=تحت حد الطلب
            int selectedStatus = cboStatus.SelectedIndex; // 0=الكل, 1=النشطة, 2=المعطلة

            int totalCount = _dtProducts.Rows.Count;
            int matchedCount = 0;
            int displayedCount = 0;
            int transactedCount = 0;
            int stagnantCount = 0;
            int lowStockCount = 0;

            bool hasActiveFilters = !string.IsNullOrEmpty(query) || selectedCatID > 0 || !string.IsNullOrEmpty(selectedBrand) ||
                                    !string.IsNullOrEmpty(selectedShelf) || selectedDeal > 0 || selectedStock > 0 || selectedStatus > 0;

            int maxDisplay = hasActiveFilters ? 600 : 350;

            dgProducts.SuspendLayout();
            var prevMode = dgProducts.AutoSizeColumnsMode;
            dgProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            try
            {
                dgProducts.Rows.Clear();

                foreach (DataRow r in _dtProducts.Rows)
                {
                    int pid = Convert.ToInt32(r["ProductID"]);
                    bool active = Convert.ToBoolean(r["IsActive"]);

                    // Status filter (Active / Disabled)
                    if (selectedStatus == 1 && !active) continue;
                    if (selectedStatus == 2 && active) continue;

                    // Category filter
                    if (selectedCatID > 0)
                    {
                        int catId = r["CategoryID"] != DBNull.Value ? Convert.ToInt32(r["CategoryID"]) : 0;
                        if (catId != selectedCatID) continue;
                    }

                    // Brand / Producer Company filter
                    string brand = r.Table.Columns.Contains("Brand") && r["Brand"] != DBNull.Value ? r["Brand"].ToString().Trim() : "";
                    string producer = r.Table.Columns.Contains("ProducerCompany") && r["ProducerCompany"] != DBNull.Value ? r["ProducerCompany"].ToString().Trim() : "";
                    if (!string.IsNullOrEmpty(selectedBrand))
                    {
                        bool matchesBrand = (!string.IsNullOrEmpty(brand) && brand.ToLower().Contains(selectedBrand)) ||
                                            (!string.IsNullOrEmpty(producer) && producer.ToLower().Contains(selectedBrand));
                        if (!matchesBrand) continue;
                    }

                    // Shelf Location filter
                    string shelf = r.Table.Columns.Contains("ShelfLocation") && r["ShelfLocation"] != DBNull.Value ? r["ShelfLocation"].ToString().Trim() : "";
                    if (!string.IsNullOrEmpty(selectedShelf))
                    {
                        if (string.IsNullOrEmpty(shelf) || !shelf.ToLower().Contains(selectedShelf)) continue;
                    }

                    // Transaction / Deals filter
                    bool isTransacted = _transactedProductIDs.Contains(pid);
                    bool isSold = _soldProductIDs.Contains(pid);
                    bool isPurchased = _purchasedProductIDs.Contains(pid);

                    if (selectedDeal == 1 && !isTransacted) continue;
                    if (selectedDeal == 2 && isTransacted) continue;
                    if (selectedDeal == 3 && !isSold) continue;
                    if (selectedDeal == 4 && !isPurchased) continue;

                    // Stock filter
                    decimal stock = _stockTotals.ContainsKey(pid) ? _stockTotals[pid] : 0m;
                    decimal minLimit = r.Table.Columns.Contains("MinStockLimit") && r["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(r["MinStockLimit"]) : 0m;

                    if (selectedStock == 1 && stock <= 0) continue;
                    if (selectedStock == 2 && stock != 0) continue;
                    if (selectedStock == 3 && stock >= 0) continue;
                    if (selectedStock == 4 && (minLimit <= 0 || stock > minLimit)) continue;

                    // Free-text query
                    string name = r["ProductName"]?.ToString() ?? "";
                    string code = r["ProductCode"]?.ToString() ?? "";
                    string partNum = r.Table.Columns.Contains("PartNumber") && r["PartNumber"] != DBNull.Value ? r["PartNumber"].ToString() : "";
                    string barcode = r.Table.Columns.Contains("InternationalCode") && r["InternationalCode"] != DBNull.Value ? r["InternationalCode"].ToString() : "";
                    string model = r.Table.Columns.Contains("CarModel") && r["CarModel"] != DBNull.Value ? r["CarModel"].ToString() : "";
                    string enName = r.Table.Columns.Contains("EnglishName") && r["EnglishName"] != DBNull.Value ? r["EnglishName"].ToString() : "";

                    if (!string.IsNullOrEmpty(query))
                    {
                        int searchType = cboSearchType != null ? cboSearchType.SelectedIndex : 0;
                        bool match = false;

                        if (searchType == 1) // 🔢 كود الصنف فقط
                        {
                            match = code.Trim().Equals(query, StringComparison.OrdinalIgnoreCase) ||
                                    code.ToLower().StartsWith(query) ||
                                    code.ToLower().Contains(query);
                        }
                        else if (searchType == 2) // 🏷️ اسم الصنف فقط
                        {
                            match = name.ToLower().Contains(query) || enName.ToLower().Contains(query);
                        }
                        else if (searchType == 3) // 📦 الباركود / كود الميزان
                        {
                            match = barcode.ToLower().Contains(query) || 
                                    (r.Table.Columns.Contains("Unit1Barcode") && r["Unit1Barcode"] != DBNull.Value && r["Unit1Barcode"].ToString().ToLower().Contains(query)) ||
                                    (r.Table.Columns.Contains("Unit2Barcode") && r["Unit2Barcode"] != DBNull.Value && r["Unit2Barcode"].ToString().ToLower().Contains(query)) ||
                                    (r.Table.Columns.Contains("ScalePLU") && r["ScalePLU"] != DBNull.Value && r["ScalePLU"].ToString().ToLower().Contains(query));
                        }
                        else if (searchType == 4) // ⚙️ رقم القطعة
                        {
                            match = partNum.ToLower().Contains(query);
                        }
                        else // 🔍 بحث شامل (في كل الحقول)
                        {
                            match = name.ToLower().Contains(query) ||
                                    code.ToLower().Contains(query) ||
                                    partNum.ToLower().Contains(query) ||
                                    barcode.ToLower().Contains(query) ||
                                    brand.ToLower().Contains(query) ||
                                    producer.ToLower().Contains(query) ||
                                    model.ToLower().Contains(query) ||
                                    shelf.ToLower().Contains(query) ||
                                    enName.ToLower().Contains(query);
                        }

                        if (!match) continue;
                    }

                    matchedCount++;
                    if (isTransacted) transactedCount++; else stagnantCount++;
                    if (minLimit > 0 && stock <= minLimit) lowStockCount++;

                    if (displayedCount < maxDisplay)
                    {
                        displayedCount++;
                        string brandDisplay = !string.IsNullOrEmpty(brand) ? brand : (!string.IsNullOrEmpty(producer) ? producer : "-");
                        string dealDisplay = isTransacted ? "✅ تم التعامل" : "💤 راكد";
                        string stockDisplay = stock.ToString("G");

                        var ri = dgProducts.Rows.Add(
                            pid, 
                            code, 
                            partNum, 
                            name,
                            r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "---",
                            brandDisplay,
                            !string.IsNullOrEmpty(shelf) ? shelf : "-",
                            r["Unit"], 
                            Convert.ToDecimal(r["SalePrice"]).ToString("N2"),
                            stockDisplay,
                            dealDisplay,
                            active ? "✓" : "✗");

                        // Row visual indicators
                        if (!active)
                        {
                            dgProducts.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
                        }
                        else if (!isTransacted)
                        {
                            dgProducts.Rows[ri].Cells["DealStatus"].Style.ForeColor = Color.FromArgb(140, 100, 20);
                        }
                        else
                        {
                            dgProducts.Rows[ri].Cells["DealStatus"].Style.ForeColor = Color.FromArgb(22, 101, 52);
                            dgProducts.Rows[ri].Cells["DealStatus"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        }

                        if (stock <= 0)
                        {
                            dgProducts.Rows[ri].Cells["TotalStock"].Style.ForeColor = stock < 0 ? Color.Red : Color.FromArgb(180, 83, 9);
                            dgProducts.Rows[ri].Cells["TotalStock"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        }
                    }
                }
            }
            finally
            {
                dgProducts.AutoSizeColumnsMode = prevMode;
                dgProducts.ResumeLayout();
            }

            if (lblItemCount != null)
            {
                if (matchedCount > displayedCount)
                    lblItemCount.Text = $"📊 الأصناف: {matchedCount} (معروض {displayedCount}) | تم التعامل: {transactedCount} | راكد: {stagnantCount} | نواقص: {lowStockCount}";
                else
                    lblItemCount.Text = $"📊 الأصناف: {matchedCount} | تم التعامل: {transactedCount} | راكد: {stagnantCount} | نواقص: {lowStockCount}";
            }
        }

        private void DgProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgProducts.SelectedRows.Count == 0)
            {
                _selectedID = 0;
                return;
            }
            _selectedID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
        }

        private void OpenSelectedProductMovement()
        {
            if (_selectedID == 0 && dgProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً لعرض حركة تعاملاته ومبيعاته.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pid = _selectedID > 0 ? _selectedID : Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
            string name = dgProducts.SelectedRows[0].Cells["ProductName"].Value?.ToString() ?? "";
            string unit = dgProducts.SelectedRows[0].Cells["Unit"].Value?.ToString() ?? "";

            using (var frm = new FrmProductMovement(pid, name, unit))
            {
                frm.ShowDialog(this);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (!Session.CanEdit("Products"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية تعديل كارت الصنف!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً لتعديله.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (new FrmProductCard(_selectedID).ShowDialog() == DialogResult.OK)
            {
                ProductCache.Refresh();
                LoadProducts();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!Session.CanDelete("Products"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية حذف وإيقاف الأصناف!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً لإيقافه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("هل أنت متأكد من إيقاف هذا الصنف؟", "تأكيد الإيقاف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ProductDAL.Delete(_selectedID);
                ProductCache.Refresh();
                LoadProducts();
            }
        }

        private void BtnPrintBarcode_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dr = ProductDAL.GetByID(_selectedID);
            if (dr == null) return;

            string name = dr["ProductName"]?.ToString() ?? "";
            string code = dr["ProductCode"]?.ToString() ?? "";
            string intCode = dr["InternationalCode"] != DBNull.Value ? dr["InternationalCode"].ToString() : "";
            decimal price = Convert.ToDecimal(dr["SalePrice"]);
            string shelfLocation = dr["ShelfLocation"] != DBNull.Value ? dr["ShelfLocation"].ToString() : "";

            using (var dlg = new FrmPrintProductBarcode(_selectedID, name, code, intCode, price, shelfLocation))
            {
                dlg.ShowDialog(this);
            }
        }

        private void AddSelectedToShortages()
        {
            if (_selectedID == 0 && dgProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى تحديد صنف أولاً من الجدول لإضافته لكشكول النواقص.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pId = _selectedID > 0 ? _selectedID : Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
            using (var dlg = new FrmAddShortageItem(pId))
            {
                dlg.ShowDialog(this);
            }
        }

        internal static bool PromptImportPassword(Form owner)
        {
            using (var passForm = new Form())
            {
                passForm.Text = "كلمة المرور مطلوبة";
                passForm.Size = new Size(340, 155);
                passForm.StartPosition = FormStartPosition.CenterParent;
                passForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                passForm.MaximizeBox = false;
                passForm.MinimizeBox = false;
                passForm.RightToLeft = RightToLeft.Yes;
                passForm.RightToLeftLayout = true;
                var lbl = new Label { Text = "أدخل كلمة المرور للاستيراد:", Dock = DockStyle.Top, Height = 30, TextAlign = System.Drawing.ContentAlignment.MiddleRight, Padding = new Padding(8, 5, 8, 0) };
                var txt = new TextBox { Dock = DockStyle.Top, PasswordChar = '*', Height = 28, Font = new Font("Segoe UI", 11f), RightToLeft = RightToLeft.Yes };
                var btnOk = new Button { Text = "موافق", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 36 };
                passForm.Controls.Add(btnOk);
                passForm.Controls.Add(txt);
                passForm.Controls.Add(lbl);
                passForm.AcceptButton = btnOk;
                if (passForm.ShowDialog(owner) == DialogResult.OK)
                {
                    if (txt.Text == "Pro@soft2026")
                        return true;
                    MessageBox.Show("كلمة المرور غير صحيحة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }
    }
}

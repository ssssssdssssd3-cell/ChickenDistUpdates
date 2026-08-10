using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة بحث متقدم احترافية ومبسطة عن الأصناف بدون تقسيم الشاشة</summary>
    public class FrmProductSearch : Form
    {
        private TextBox txtSearch, txtBrandFilter, txtColorFilter, txtCompanyFilter, txtPriceFrom, txtPriceTo;
        private TextBox txtSelectedQty, txtSelectedPurchasePrice, txtSelectedSalePrice, txtSelectedDiscount;
        private Label lblPricePermissionNotice;
        private ComboBox cboCategory, cboUnits;
        private CheckBox chkShowZeroStock;
        private DataGridView dgProducts;
        private Button btnSelect, btnCancel;
        private DataTable _dtProducts;
        private DataView _dvProducts;
        private int? _warehouseID;
        private bool _isPurchaseMode = false;
        private Dictionary<int, decimal> _stockCache = new Dictionary<int, decimal>();
        private Dictionary<int, decimal> _globalStockCache = new Dictionary<int, decimal>();
        private Timer _searchTimer;

        public int SelectedProductID { get; private set; } = 0;
        public decimal SelectedPrice { get; private set; } = 0m;
        public string SelectedUnitName { get; private set; } = "";
        public int? SelectedBatchID { get; private set; } = null;
        public DateTime? SelectedExpiryDate { get; private set; } = null;

        public decimal SelectedQuantity { get; private set; } = 1m;
        public decimal SelectedPurchasePrice { get; private set; } = 0m;
        public decimal SelectedSalePrice { get; private set; } = 0m;
        public decimal SelectedDiscount { get; private set; } = 0m;

        private class UnitComboItem
        {
            public string DisplayText { get; set; }
            public string UnitName { get; set; }
            public decimal SalePrice { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal Factor { get; set; }
            public decimal StockQty { get; set; }
            public decimal GlobalStockQty { get; set; }
            public string MatchedUnit { get; set; }
            public int? BatchID { get; set; }
            public DateTime? ExpiryDate { get; set; }

            public override string ToString() => DisplayText;
        }

        public FrmProductSearch(int? warehouseID = null, bool isPurchaseMode = false, bool defaultShowZeroStock = false)
        {
            _warehouseID = warehouseID;
            _isPurchaseMode = isPurchaseMode;
            _searchTimer = new Timer { Interval = 220 };
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); ApplyFilter(); };
            InitUI();
            LoadCategories();
            LoadProducts();
            if (defaultShowZeroStock && chkShowZeroStock != null)
            {
                chkShowZeroStock.Checked = true;
            }
        }

        private void InitUI()
        {
            this.Text = _isPurchaseMode ? "🔍 بحث متقدم عن صنف - فواتير الشراء والتوريد" : "🔍 بحث متقدم عن صنف - مبيعات ونقطة البيع";
            this.Size = new Size(1000, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── Top panel (Filters) ──────────────────────────────────────────
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 145, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(12, 8, 12, 8) };
            
            Color labelColor = Color.FromArgb(30, 41, 59);
            Color inputBg = Color.White;
            Color inputFg = Color.FromArgb(15, 23, 42);

            // Row 1: Search name/code & Category
            var lblSearch = new Label { Text = "ابحث بالاسم أو الكود :", Location = new Point(620, 16), AutoSize = false, Width = 140, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtSearch = new TextBox { Location = new Point(120, 12), Width = 490, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            txtSearch.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };
            txtSearch.KeyDown += TxtSearch_KeyDown;
            
            var lblCat = new Label { Text = "التصنيف:", Location = new Point(620, 48), AutoSize = false, Width = 140, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            cboCategory = new ComboBox { Location = new Point(120, 44), Width = 490, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = inputBg, ForeColor = inputFg, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
            cboCategory.SelectedIndexChanged += (s, e) => ApplyFilter();

            // Row 2: Brand, Color & Company
            var lblBrand = new Label { Text = "الماركة:", Location = new Point(620, 78), AutoSize = false, Width = 140, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtBrandFilter = new TextBox { Location = new Point(510, 76), Width = 100, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
            txtBrandFilter.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };

            var lblColor = new Label { Text = "اللون:", Location = new Point(445, 78), AutoSize = false, Width = 55, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtColorFilter = new TextBox { Location = new Point(345, 76), Width = 95, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
            txtColorFilter.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };

            var lblCompany = new Label { Text = "الشركة المنتجة:", Location = new Point(220, 78), AutoSize = false, Width = 120, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtCompanyFilter = new TextBox { Location = new Point(10, 76), Width = 205, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
            txtCompanyFilter.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };

            // Row 3: Price range & Zero Stock checkbox
            var lblPriceRange = new Label { Text = "السعر من:", Location = new Point(620, 110), AutoSize = false, Width = 140, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtPriceFrom = new TextBox { Location = new Point(510, 108), Width = 100, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
            txtPriceFrom.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };

            var lblPriceTo = new Label { Text = "إلى:", Location = new Point(445, 110), AutoSize = false, Width = 55, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleCenter };
            txtPriceTo = new TextBox { Location = new Point(345, 108), Width = 95, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
            txtPriceTo.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };

            chkShowZeroStock = new CheckBox
            {
                Text = "إظهار الأصناف ذات الرصيد الصفري",
                Location = new Point(10, 108),
                Width = 290,
                Height = 24,
                ForeColor = labelColor,
                Font = Theme.FontBold,
                Checked = false
            };
            chkShowZeroStock.CheckedChanged += (s, e) => RefreshGrid();
            
            pnlSearch.Controls.AddRange(new Control[] { 
                lblSearch, txtSearch, lblCat, cboCategory, 
                lblBrand, txtBrandFilter, lblColor, txtColorFilter, lblCompany, txtCompanyFilter, lblPriceRange, txtPriceFrom, lblPriceTo, txtPriceTo, 
                chkShowZeroStock 
            });

            // ── Grid Panel (Full Height) ──────────────────────────────────────
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
            
            dgProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(226, 232, 240),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(15, 23, 42),
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    WrapMode = DataGridViewTriState.False
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            Theme.EnableDoubleBuffer(dgProducts);
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 22 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 55 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductSize", HeaderText = "المقاس", FillWeight = 18 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "اللون", FillWeight = 18 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة الرئيسية", FillWeight = 20 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 26 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 22 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQty", HeaderText = "الرصيد الفعلي", FillWeight = 25 });
            
            dgProducts.DoubleClick += DgProducts_DoubleClick;
            dgProducts.KeyDown += DgProducts_KeyDown;
            dgProducts.SelectionChanged += DgProducts_SelectionChanged;

            pnlGrid.Controls.Add(dgProducts);

            // ── Bottom Actions & Input Panel ─────────────────────────────────
            var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 105, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(8, 4, 8, 4) };

            var pnlEditInputs = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(226, 232, 240), Padding = new Padding(6, 6, 6, 6) };

            Color labelDark = Color.FromArgb(15, 23, 42);

            // Unit Selector Dropdown
            var lblUnitSelect = new Label { Text = "📐 الوحدة:", Location = new Point(915, 13), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cboUnits = new ComboBox
            {
                Location = new Point(690, 9),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = labelDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cboUnits.SelectedIndexChanged += CboUnits_SelectedIndexChanged;

            var lblQty = new Label { Text = "📦 الكمية:", Location = new Point(610, 13), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtSelectedQty = new TextBox { Location = new Point(525, 9), Width = 80, Text = "1.00", BackColor = Color.White, ForeColor = labelDark, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };

            var lblPurchasePrice = new Label { Text = "💰 الشراء:", Location = new Point(435, 13), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Visible = _isPurchaseMode };
            txtSelectedPurchasePrice = new TextBox { Location = new Point(345, 9), Width = 85, Text = "0.00", BackColor = Color.White, ForeColor = labelDark, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, Visible = _isPurchaseMode };

            int salePriceLabelX = _isPurchaseMode ? 260 : 435;
            int salePriceTxtX = _isPurchaseMode ? 175 : 345;

            var lblSalePrice = new Label { Text = "🏷️ البيع:", Location = new Point(salePriceLabelX, 13), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtSelectedSalePrice = new TextBox { Location = new Point(salePriceTxtX, 9), Width = 85, Text = "0.00", BackColor = Color.White, ForeColor = labelDark, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };

            int discLabelX = _isPurchaseMode ? 100 : 255;
            int discTxtX = _isPurchaseMode ? 15 : 170;

            var lblDiscount = new Label { Text = "🎁 الخصم:", Location = new Point(discLabelX, 13), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtSelectedDiscount = new TextBox { Location = new Point(discTxtX, 9), Width = 80, Text = "0.00", BackColor = Color.White, ForeColor = labelDark, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };

            int permNoticeX = _isPurchaseMode ? 175 : 345;
            lblPricePermissionNotice = new Label { Text = "🔒 تعديل السعر يتطلب صلاحية", Location = new Point(permNoticeX, 32), AutoSize = true, ForeColor = Color.FromArgb(220, 38, 38), Font = new Font("Segoe UI", 7.5f, FontStyle.Bold) };

            bool canEditPrice = _isPurchaseMode 
                ? (Session.Role == "Admin" || Session.CanEditPrice("ProductSearch") || Session.CanEditPrice("Purchases"))
                : (Session.Role == "Admin" || Session.CanEditPrice("ProductSearch") || Session.CanEditPrice("Sales"));

            if (!canEditPrice)
            {
                txtSelectedPurchasePrice.ReadOnly = true;
                txtSelectedPurchasePrice.BackColor = Color.FromArgb(226, 232, 240);
                txtSelectedPurchasePrice.ForeColor = Color.FromArgb(100, 116, 139);
                txtSelectedSalePrice.ReadOnly = true;
                txtSelectedSalePrice.BackColor = Color.FromArgb(226, 232, 240);
                txtSelectedSalePrice.ForeColor = Color.FromArgb(100, 116, 139);
                lblPricePermissionNotice.Visible = true;
            }
            else
            {
                lblPricePermissionNotice.Visible = false;
            }

            pnlEditInputs.Controls.AddRange(new Control[] {
                lblUnitSelect, cboUnits,
                lblQty, txtSelectedQty,
                lblPurchasePrice, txtSelectedPurchasePrice,
                lblSalePrice, txtSelectedSalePrice,
                lblDiscount, txtSelectedDiscount,
                lblPricePermissionNotice
            });

            var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = Color.Transparent };
            btnSelect = Theme.MakeButton("✅ اختيار وانزال الصنف للفاتورة", 450, 6, 240, 34, Theme.Accent);
            btnSelect.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);

            var btnAddNewProduct = Theme.MakeButton("➕ إضافة صنف جديد", 270, 6, 165, 34, Theme.Success);
            btnAddNewProduct.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnAddNewProduct.Visible = _isPurchaseMode;
            btnAddNewProduct.Click += (s, e) =>
            {
                using (var frm = new FrmProducts())
                {
                    frm.ShowDialog(this);
                }
                LoadProducts();
            };
            btnCancel = Theme.MakeButton("❌ إلغاء", 130, 6, 125, 34, Color.FromArgb(120, 40, 40));
            btnCancel.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            
            btnSelect.Click += BtnSelect_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            pnlButtons.Controls.AddRange(new Control[] { btnSelect, btnAddNewProduct, btnCancel });

            pnlActions.Controls.Add(pnlButtons);
            pnlActions.Controls.Add(pnlEditInputs);

            // Add in docking Z-order
            this.Controls.Add(pnlGrid);      // Fill
            this.Controls.Add(pnlActions);   // Bottom
            this.Controls.Add(pnlSearch);    // Top

            Theme.ApplyFormRTL(this);
        }

        private void LoadCategories()
        {
            try
            {
                DataTable dt = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories WHERE IsActive = 1 ORDER BY CategoryName");
                cboCategory.Items.Clear();
                cboCategory.Items.Add(new ComboItem(0, "-- كل التصنيفات --"));
                foreach (DataRow r in dt.Rows)
                {
                    cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                }
                cboCategory.DisplayMember = "Text";
                cboCategory.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadProducts()
        {
            _dtProducts = ProductCache.GetActive();
            _dvProducts = new DataView(_dtProducts);
            LoadStockCache();
            RefreshGrid();
        }

        private void LoadStockCache()
        {
            _stockCache.Clear();
            _globalStockCache.Clear();
            try
            {
                _stockCache = InventoryDAL.GetStockSummary(_warehouseID);
                _globalStockCache = InventoryDAL.GetStockSummary(null);
            }
            catch { }
        }

        private void RefreshGrid()
        {
            if (dgProducts == null) return;
            dgProducts.SuspendLayout();
            var oldMode = dgProducts.AutoSizeColumnsMode;
            dgProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgProducts.Rows.Clear();
            int displayedCount = 0;
            int maxDisplay = 300;

            foreach (DataRowView drv in _dvProducts)
            {
                if (displayedCount >= maxDisplay) break;
                var row = drv.Row;
                int pid = Convert.ToInt32(row["ProductID"]);
                decimal totalStock = _stockCache.TryGetValue(pid, out var cached) ? cached : 0m;

                decimal price = Convert.ToDecimal(row["SalePrice"]);
                decimal pendingPrice = row["PendingSalePrice"] != DBNull.Value ? Convert.ToDecimal(row["PendingSalePrice"]) : 0m;
                decimal threshold = row["PendingQtyThreshold"] != DBNull.Value ? Convert.ToDecimal(row["PendingQtyThreshold"]) : 0m;
                string catName = row.Table.Columns.Contains("CategoryName") && row["CategoryName"] != DBNull.Value ? row["CategoryName"].ToString() : "";
                string pSize = row.Table.Columns.Contains("ProductSize") && row["ProductSize"] != DBNull.Value ? row["ProductSize"].ToString() : "";
                string pColor = row.Table.Columns.Contains("Color") && row["Color"] != DBNull.Value ? row["Color"].ToString() : "";

                if (pendingPrice > 0m && threshold > 0m)
                {
                    decimal oldStockAvailable = Math.Max(0m, Math.Min(totalStock, threshold));
                    decimal newStockAvailable = Math.Max(0m, totalStock - oldStockAvailable);

                    if (chkShowZeroStock.Checked || totalStock > 0m)
                    {
                        displayedCount++;
                        int rowIdx = dgProducts.Rows.Add(
                            row["ProductID"], 
                            row["ProductCode"], 
                            row["ProductName"].ToString() + " (السعر الحالي)", 
                            pSize,
                            pColor,
                            row["Unit"],
                            catName,
                            price.ToString("F2"), 
                            oldStockAvailable.ToString("F2")
                        );
                        ColorStockCell(rowIdx, oldStockAvailable);

                        int rowIdx2 = dgProducts.Rows.Add(
                            row["ProductID"], 
                            row["ProductCode"], 
                            row["ProductName"].ToString() + " (السعر المعلق)", 
                            pSize,
                            pColor,
                            row["Unit"],
                            catName,
                            pendingPrice.ToString("F2"), 
                            newStockAvailable.ToString("F2")
                        );
                        ColorStockCell(rowIdx2, newStockAvailable);
                    }
                }
                else
                {
                    if (chkShowZeroStock.Checked || totalStock > 0m)
                    {
                        displayedCount++;
                        int rowIdx = dgProducts.Rows.Add(
                            row["ProductID"], 
                            row["ProductCode"], 
                            row["ProductName"], 
                            pSize,
                            pColor,
                            row["Unit"],
                            catName,
                            price.ToString("F2"), 
                            totalStock.ToString("F2")
                        );
                        ColorStockCell(rowIdx, totalStock);
                    }
                }
            }

            dgProducts.AutoSizeColumnsMode = oldMode;
            dgProducts.ResumeLayout();
            if (dgProducts.Rows.Count > 0)
            {
                dgProducts.Rows[0].Selected = true;
                UpdateUnitsCombo();
            }
        }

        private void ColorStockCell(int rowIdx, decimal stock)
        {
            var cell = dgProducts.Rows[rowIdx].Cells["StockQty"];
            if (stock <= 0)
                cell.Style.ForeColor = Color.FromArgb(220, 70, 70);
            else if (stock < 10)
                cell.Style.ForeColor = Color.FromArgb(220, 150, 40);
            else
                cell.Style.ForeColor = Color.FromArgb(60, 190, 100);
        }

        private void ApplyFilter()
        {
            if (_dvProducts == null) return;
            string term = txtSearch.Text.Trim().Replace("'", "''");
            string brandTerm = txtBrandFilter.Text.Trim().Replace("'", "''");
            string colorTerm = txtColorFilter != null ? txtColorFilter.Text.Trim().Replace("'", "''") : "";
            string companyTerm = txtCompanyFilter.Text.Trim().Replace("'", "''");
            int catID = 0;
            if (cboCategory.SelectedItem is ComboItem ci)
                catID = ci.ID;

            string filter = "";
            if (!string.IsNullOrEmpty(term))
            {
                filter = $"(ProductName LIKE '%{term}%' OR ProductCode LIKE '%{term}%' OR PartNumber LIKE '%{term}%' OR InternationalCode LIKE '%{term}%' OR Unit1Barcode LIKE '%{term}%' OR Unit2Barcode LIKE '%{term}%')";
            }

            if (!string.IsNullOrEmpty(brandTerm))
            {
                string brandFilter = $"(Brand LIKE '%{brandTerm}%' OR CarModel LIKE '%{brandTerm}%')";
                if (!string.IsNullOrEmpty(filter)) filter = $"({filter}) AND ({brandFilter})";
                else filter = brandFilter;
            }

            if (!string.IsNullOrEmpty(colorTerm))
            {
                string colorFilter = $"(Color LIKE '%{colorTerm}%')";
                if (!string.IsNullOrEmpty(filter)) filter = $"({filter}) AND ({colorFilter})";
                else filter = colorFilter;
            }

            if (!string.IsNullOrEmpty(companyTerm))
            {
                string companyFilter = $"(ProducerCompany LIKE '%{companyTerm}%')";
                if (!string.IsNullOrEmpty(filter)) filter = $"({filter}) AND ({companyFilter})";
                else filter = companyFilter;
            }

            if (catID > 0)
            {
                if (!string.IsNullOrEmpty(filter)) filter = $"({filter}) AND ";
                filter += $"CategoryID = {catID}";
            }

            decimal priceFrom = 0m;
            decimal priceTo = 0m;
            bool hasPriceFrom = decimal.TryParse(txtPriceFrom.Text.Trim(), out priceFrom);
            bool hasPriceTo = decimal.TryParse(txtPriceTo.Text.Trim(), out priceTo);

            if (hasPriceFrom)
            {
                if (!string.IsNullOrEmpty(filter)) filter = $"({filter}) AND ";
                filter += $"SalePrice >= {priceFrom}";
            }
            if (hasPriceTo)
            {
                if (!string.IsNullOrEmpty(filter)) filter = $"({filter}) AND ";
                filter += $"SalePrice <= {priceTo}";
            }

            _dvProducts.RowFilter = filter;
            RefreshGrid();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (dgProducts.Rows.Count > 0)
                {
                    dgProducts.Focus();
                    dgProducts.CurrentCell = dgProducts.Rows[0].Cells[1];
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (dgProducts.Rows.Count > 0)
                {
                    SelectAndClose();
                    e.Handled = true;
                }
            }
        }

        private void DgProducts_DoubleClick(object sender, EventArgs e)
        {
            SelectAndClose();
        }

        private void DgProducts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SelectAndClose();
                e.Handled = true;
            }
        }

        private void DgProducts_SelectionChanged(object sender, EventArgs e)
        {
            UpdateUnitsCombo();
        }

        private void UpdateUnitsCombo()
        {
            if (cboUnits == null) return;
            cboUnits.Items.Clear();
            if (dgProducts.SelectedRows.Count == 0) return;

            var selectedRow = dgProducts.SelectedRows[0];
            int productID = Convert.ToInt32(selectedRow.Cells["ProductID"].Value);

            DataRow prodRow = (selectedRow.DataBoundItem is DataRowView drv) ? drv.Row : null;
            if (prodRow == null)
            {
                foreach (DataRowView dr in _dvProducts)
                {
                    if (Convert.ToInt32(dr.Row["ProductID"]) == productID)
                    {
                        prodRow = dr.Row;
                        break;
                    }
                }
            }
            if (prodRow == null) return;

            decimal stock = _stockCache.TryGetValue(productID, out var s) ? s : 0m;
            decimal globalStock = _globalStockCache.TryGetValue(productID, out var gs) ? gs : 0m;

            string baseUnit = prodRow.Table.Columns.Contains("Unit") && prodRow["Unit"] != DBNull.Value ? prodRow["Unit"].ToString() : "وحدة";
            string unit1 = prodRow.Table.Columns.Contains("Unit1Name") && prodRow["Unit1Name"] != DBNull.Value ? prodRow["Unit1Name"].ToString() : "";
            string unit2 = prodRow.Table.Columns.Contains("Unit2Name") && prodRow["Unit2Name"] != DBNull.Value ? prodRow["Unit2Name"].ToString() : "";

            decimal unit2Factor = prodRow.Table.Columns.Contains("Unit2Factor") && prodRow["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(prodRow["Unit2Factor"]) : 1m;
            decimal unit3Factor = prodRow.Table.Columns.Contains("Unit3Factor") && prodRow["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(prodRow["Unit3Factor"]) : 1m;

            decimal unit2FactorVal = unit2Factor > 0 ? unit2Factor : 1m;
            decimal unit3FactorVal = unit3Factor > 0 ? unit3Factor : 1m;
            decimal baseFactor = unit2FactorVal * unit3FactorVal;

            decimal basePrice = Convert.ToDecimal(prodRow["SalePrice"]);
            decimal unit1Price = prodRow.Table.Columns.Contains("Unit1SalePrice") && prodRow["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(prodRow["Unit1SalePrice"]) : 0m;
            decimal unit2Price = prodRow.Table.Columns.Contains("Unit2SalePrice") && prodRow["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(prodRow["Unit2SalePrice"]) : 0m;

            if (unit1Price <= 0) unit1Price = basePrice;

            decimal basePP = Convert.ToDecimal(prodRow["PurchasePrice"]);
            decimal unit1PP = prodRow.Table.Columns.Contains("Unit1PurchasePrice") && prodRow["Unit1PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(prodRow["Unit1PurchasePrice"]) : 0m;
            decimal unit2PP = prodRow.Table.Columns.Contains("Unit2PurchasePrice") && prodRow["Unit2PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(prodRow["Unit2PurchasePrice"]) : 0m;

            if (unit1PP <= 0) unit1PP = basePP;

            // 1. Base Unit (الكبرى)
            decimal baseStock = stock / baseFactor;
            decimal baseGlobalStock = globalStock / baseFactor;
            cboUnits.Items.Add(new UnitComboItem
            {
                DisplayText = $"{baseUnit} - {basePrice:F2} ج (رصيد: {baseStock:N0})",
                UnitName = baseUnit,
                SalePrice = basePrice,
                PurchasePrice = basePP,
                Factor = baseFactor,
                StockQty = baseStock,
                GlobalStockQty = baseGlobalStock,
                MatchedUnit = baseUnit
            });

            // 2. Unit 2 (الوسطى)
            if (!string.IsNullOrEmpty(unit2))
            {
                decimal u2Stock = stock / unit2FactorVal;
                decimal u2GlobalStock = globalStock / unit2FactorVal;
                cboUnits.Items.Add(new UnitComboItem
                {
                    DisplayText = $"{unit2} - {unit2Price:F2} ج (رصيد: {u2Stock:N0})",
                    UnitName = unit2,
                    SalePrice = unit2Price,
                    PurchasePrice = unit2PP,
                    Factor = unit2FactorVal,
                    StockQty = u2Stock,
                    GlobalStockQty = u2GlobalStock,
                    MatchedUnit = unit2
                });
            }

            // 3. Unit 1 (الصغرى)
            if (!string.IsNullOrEmpty(unit1) && unit1 != baseUnit)
            {
                cboUnits.Items.Add(new UnitComboItem
                {
                    DisplayText = $"{unit1} - {unit1Price:F2} ج (رصيد: {stock:N0})",
                    UnitName = unit1,
                    SalePrice = unit1Price,
                    PurchasePrice = unit1PP,
                    Factor = 1m,
                    StockQty = stock,
                    GlobalStockQty = globalStock,
                    MatchedUnit = unit1
                });
            }

            // 4. Batches / Expiry Dates
            bool hasExpiry = prodRow.Table.Columns.Contains("HasExpiry") && prodRow["HasExpiry"] != DBNull.Value && Convert.ToBoolean(prodRow["HasExpiry"]);
            if (hasExpiry)
            {
                int whId = _warehouseID ?? 1;
                DataTable dtBatches = DbHelper.Query(@"
                    SELECT BatchID, ExpiryDate, Quantity 
                    FROM ProductBatches 
                    WHERE ProductID = @pid AND WarehouseID = @wid AND Quantity > 0
                    ORDER BY ExpiryDate ASC, BatchID ASC",
                    DbHelper.P("@pid", productID), DbHelper.P("@wid", whId));
                
                foreach (DataRow bRow in dtBatches.Rows)
                {
                    int batchID = Convert.ToInt32(bRow["BatchID"]);
                    DateTime? expDate = bRow["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(bRow["ExpiryDate"]) : (DateTime?)null;
                    decimal batchQty = Convert.ToDecimal(bRow["Quantity"]);
                    string expStr = expDate.HasValue ? expDate.Value.ToString("yyyy-MM-dd") : "بدون تاريخ";
                    
                    cboUnits.Items.Add(new UnitComboItem
                    {
                        DisplayText = $"صلاحية: {expStr} (دفعة #{batchID})",
                        UnitName = baseUnit,
                        SalePrice = basePrice,
                        PurchasePrice = basePP,
                        Factor = 1m,
                        StockQty = batchQty,
                        GlobalStockQty = batchQty,
                        MatchedUnit = $"BATCH:{batchID}:{expStr}",
                        BatchID = batchID,
                        ExpiryDate = expDate
                    });
                }
            }

            if (cboUnits.Items.Count > 0)
            {
                cboUnits.SelectedIndex = 0;
            }
            else
            {
                UpdateSelectedInputFields();
            }
        }

        private void CboUnits_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectedInputFields();
        }

        private void UpdateSelectedInputFields()
        {
            if (txtSelectedPurchasePrice == null || txtSelectedSalePrice == null) return;

            decimal sp = 0m;
            decimal pp = 0m;

            if (cboUnits != null && cboUnits.SelectedItem is UnitComboItem uItem)
            {
                sp = uItem.SalePrice;
                pp = uItem.PurchasePrice;
            }
            else if (dgProducts != null && dgProducts.SelectedRows.Count > 0)
            {
                var row = dgProducts.SelectedRows[0];
                if (row.Cells["SalePrice"].Value != null)
                    decimal.TryParse(row.Cells["SalePrice"].Value.ToString(), out sp);

                int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                if (row.DataBoundItem is DataRowView drv && drv.Row.Table.Columns.Contains("PurchasePrice") && drv.Row["PurchasePrice"] != DBNull.Value)
                {
                    pp = Convert.ToDecimal(drv.Row["PurchasePrice"]);
                }
            }

            txtSelectedPurchasePrice.Text = pp.ToString("F2");
            txtSelectedSalePrice.Text = sp.ToString("F2");
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            SelectAndClose();
        }

        private void SelectAndClose()
        {
            if (dgProducts.SelectedRows.Count == 0) return;
            SelectedProductID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);

            SelectedBatchID = null;
            SelectedExpiryDate = null;

            if (cboUnits != null && cboUnits.SelectedItem is UnitComboItem uItem)
            {
                SelectedPrice = _isPurchaseMode ? uItem.PurchasePrice : uItem.SalePrice;
                SelectedUnitName = uItem.UnitName;
                SelectedBatchID = uItem.BatchID;
                SelectedExpiryDate = uItem.ExpiryDate;
            }
            else
            {
                SelectedPrice = Convert.ToDecimal(dgProducts.SelectedRows[0].Cells["SalePrice"].Value);
                SelectedUnitName = dgProducts.SelectedRows[0].Cells["Unit"].Value?.ToString() ?? "";
            }

            if (txtSelectedQty != null && decimal.TryParse(txtSelectedQty.Text.Trim(), out decimal q) && q > 0)
                SelectedQuantity = q;
            else
                SelectedQuantity = 1m;

            if (txtSelectedPurchasePrice != null && decimal.TryParse(txtSelectedPurchasePrice.Text.Trim(), out decimal ppVal) && ppVal >= 0)
            {
                SelectedPurchasePrice = ppVal;
                if (_isPurchaseMode) SelectedPrice = ppVal;
            }
            else
                SelectedPurchasePrice = SelectedPrice;

            if (txtSelectedSalePrice != null && decimal.TryParse(txtSelectedSalePrice.Text.Trim(), out decimal spVal) && spVal >= 0)
            {
                SelectedSalePrice = spVal;
                if (!_isPurchaseMode) SelectedPrice = spVal;
            }
            else
                SelectedSalePrice = SelectedPrice;

            if (txtSelectedDiscount != null && decimal.TryParse(txtSelectedDiscount.Text.Trim(), out decimal dVal))
                SelectedDiscount = dVal;
            else
                SelectedDiscount = 0m;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة بحث متقدم عن الأصناف</summary>
    public class FrmProductSearch : Form
    {
        private TextBox txtSearch, txtBrandFilter, txtColorFilter, txtCompanyFilter, txtPriceFrom, txtPriceTo;
        private TextBox txtSelectedQty, txtSelectedPurchasePrice, txtSelectedSalePrice, txtSelectedDiscount;
        private Label lblPricePermissionNotice;
        private ComboBox cboCategory;
        private CheckBox chkShowZeroStock;
        private DataGridView dgProducts, dgUnits;
        private Button btnSelect, btnCancel;
        private DataTable _dtProducts;
        private DataView _dvProducts;
        private int? _warehouseID;
        private bool _isPurchaseMode = false;
        private Dictionary<int, decimal> _stockCache = new Dictionary<int, decimal>();
        private Dictionary<int, decimal> _globalStockCache = new Dictionary<int, decimal>();

        public int SelectedProductID { get; private set; } = 0;
        public decimal SelectedPrice { get; private set; } = 0m;
        public string SelectedUnitName { get; private set; } = "";
        public int? SelectedBatchID { get; private set; } = null;
        public DateTime? SelectedExpiryDate { get; private set; } = null;

        public decimal SelectedQuantity { get; private set; } = 1m;
        public decimal SelectedPurchasePrice { get; private set; } = 0m;
        public decimal SelectedSalePrice { get; private set; } = 0m;
        public decimal SelectedDiscount { get; private set; } = 0m;

        public FrmProductSearch(int? warehouseID = null, bool isPurchaseMode = false)
        {
            _warehouseID = warehouseID;
            _isPurchaseMode = isPurchaseMode;
            InitUI();
            LoadCategories();
            LoadProducts();
        }

        private void InitUI()
        {
            this.Text = _isPurchaseMode ? "🔍 بحث متقدم عن صنف - فواتير الشراء والتوريد" : "🔍 بحث متقدم عن صنف - مبيعات ونقطة البيع";
            this.Size = new Size(960, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Top panel (Filters)
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 175, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(12) };
            
            Color labelColor = Color.FromArgb(30, 41, 59); // High contrast dark slate
            Color inputBg = Color.White;
            Color inputFg = Color.FromArgb(15, 23, 42);

            // Row 1: Search name/code & Category
            var lblSearch = new Label { Text = "ابحث بالاسم أو الكود :", Location = new Point(580, 18), AutoSize = false, Width = 140, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtSearch = new TextBox { Location = new Point(70, 14), Width = 500, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            txtSearch.KeyDown += TxtSearch_KeyDown;
            
            var lblCat = new Label { Text = "التصنيف:", Location = new Point(580, 50), AutoSize = false, Width = 140, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            cboCategory = new ComboBox { Location = new Point(70, 46), Width = 500, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = inputBg, ForeColor = inputFg, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
            cboCategory.SelectedIndexChanged += (s, e) => ApplyFilter();

            // Row 2: Brand, Color & Company
            var lblBrand = new Label { Text = "الماركة:", Location = new Point(580, 80), AutoSize = false, Width = 140, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtBrandFilter = new TextBox { Location = new Point(470, 78), Width = 100, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtBrandFilter.TextChanged += (s, e) => ApplyFilter();

            var lblColor = new Label { Text = "اللون:", Location = new Point(410, 80), AutoSize = false, Width = 55, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtColorFilter = new TextBox { Location = new Point(310, 78), Width = 95, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtColorFilter.TextChanged += (s, e) => ApplyFilter();

            var lblCompany = new Label { Text = "الشركة المنتجة / الخامة:", Location = new Point(160, 80), AutoSize = false, Width = 145, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtCompanyFilter = new TextBox { Location = new Point(10, 78), Width = 145, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtCompanyFilter.TextChanged += (s, e) => ApplyFilter();

            // Row 3: Price range & Zero Stock checkbox
            var lblPriceRange = new Label { Text = "السعر من:", Location = new Point(580, 112), AutoSize = false, Width = 140, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleRight };
            txtPriceFrom = new TextBox { Location = new Point(470, 110), Width = 100, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtPriceFrom.TextChanged += (s, e) => ApplyFilter();

            var lblPriceTo = new Label { Text = "إلى:", Location = new Point(410, 112), AutoSize = false, Width = 55, ForeColor = labelColor, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleCenter };
            txtPriceTo = new TextBox { Location = new Point(310, 110), Width = 95, BackColor = inputBg, ForeColor = inputFg, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtPriceTo.TextChanged += (s, e) => ApplyFilter();

            chkShowZeroStock = new CheckBox
            {
                Text = "إظهار الأصناف ذات الرصيد الصفري",
                Location = new Point(10, 140),
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

            // Grid Panel
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            
            dgProducts = new DataGridView
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
                GridColor = Color.FromArgb(226, 232, 240),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(15, 23, 42),
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeight = 42,
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
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 25 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 50 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductSize", HeaderText = "المقاس", FillWeight = 20 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "اللون", FillWeight = 20 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 18 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 28 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 25 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQty", HeaderText = "الرصيد الفعلي", FillWeight = 27 });
            
            dgProducts.DoubleClick += DgProducts_DoubleClick;
            dgProducts.KeyDown += DgProducts_KeyDown;
            dgProducts.SelectionChanged += DgProducts_SelectionChanged;

            // Units Section (Bottom of grid panel)
            var pnlUnitsSection = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                Padding = new Padding(0, 4, 0, 0)
            };

            var lblUnitsTitle = new Label
            {
                Text = "📋 الوحدات والأسعار المتاحة للصنف المحدد :",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlUnitsSection.Controls.Add(lblUnitsTitle);

            dgUnits = new DataGridView
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
                GridColor = Color.FromArgb(226, 232, 240),
                DefaultCellStyle = dgProducts.DefaultCellStyle.Clone(),
                ColumnHeadersHeight = 50,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    WrapMode = DataGridViewTriState.True
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 30 });
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 25 });
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQty", HeaderText = "الرصيد بالمخزن الحالي", FillWeight = 35 });
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "GlobalStockQty", HeaderText = "الرصيد في كل المخازن", FillWeight = 35 });
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "Factor", Visible = false });
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", Visible = false });
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "MatchedUnit", Visible = false });

            dgUnits.DoubleClick += (s, e) => SelectAndClose();
            dgUnits.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SelectAndClose(); e.Handled = true; } };
            dgUnits.SelectionChanged += (s, e) => UpdateSelectedInputFields();

            pnlUnitsSection.Controls.Add(dgUnits);

            pnlGrid.Controls.Add(dgProducts);
            pnlGrid.Controls.Add(pnlUnitsSection);

            // Bottom Actions & Input Panel (High Contrast Light Blue Panel)
            var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 108, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(8, 4, 8, 4) };

            var pnlEditInputs = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(226, 232, 240), Padding = new Padding(6, 5, 6, 5) };

            Color labelDark = Color.FromArgb(15, 23, 42); // Ultra crisp dark blue/black

            var lblQty = new Label { Text = "📦 الكمية:", Location = new Point(780, 14), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtSelectedQty = new TextBox { Location = new Point(680, 10), Width = 95, Text = "1.00", BackColor = Color.White, ForeColor = labelDark, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };

            var lblPurchasePrice = new Label { Text = "💰 سعر الشراء:", Location = new Point(570, 14), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Visible = _isPurchaseMode };
            txtSelectedPurchasePrice = new TextBox { Location = new Point(460, 10), Width = 105, Text = "0.00", BackColor = Color.White, ForeColor = labelDark, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, Visible = _isPurchaseMode };

            int salePriceLabelX = _isPurchaseMode ? 345 : 540;
            int salePriceTxtX = _isPurchaseMode ? 235 : 425;

            var lblSalePrice = new Label { Text = "🏷️ سعر البيع:", Location = new Point(salePriceLabelX, 14), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtSelectedSalePrice = new TextBox { Location = new Point(salePriceTxtX, 10), Width = 110, Text = "0.00", BackColor = Color.White, ForeColor = labelDark, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };

            int discLabelX = _isPurchaseMode ? 155 : 290;
            int discTxtX = _isPurchaseMode ? 65 : 180;

            var lblDiscount = new Label { Text = "🎁 الخصم:", Location = new Point(discLabelX, 14), AutoSize = true, ForeColor = labelDark, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtSelectedDiscount = new TextBox { Location = new Point(discTxtX, 10), Width = 100, Text = "0.00", BackColor = Color.White, ForeColor = labelDark, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };

            int permNoticeX = _isPurchaseMode ? 235 : 425;
            lblPricePermissionNotice = new Label { Text = "🔒 تعديل السعر يتطلب صلاحية", Location = new Point(permNoticeX, 32), AutoSize = true, ForeColor = Color.FromArgb(220, 38, 38), Font = new Font("Segoe UI", 7.5f, FontStyle.Bold) };

            // فحص صلاحية تعديل السعر حسب وضع الشاشة (شراء أم بيع) أو صلاحية ProductSearch المباشرة
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
                lblQty, txtSelectedQty,
                lblPurchasePrice, txtSelectedPurchasePrice,
                lblSalePrice, txtSelectedSalePrice,
                lblDiscount, txtSelectedDiscount,
                lblPricePermissionNotice
            });

            var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };
            btnSelect = Theme.MakeButton("✅ اختيار وانزال الصنف للفاتورة", 420, 8, 220, 34, Theme.Accent);
            var btnAddNewProduct = Theme.MakeButton("➕ إضافة صنف جديد", 240, 8, 165, 34, Theme.Success);
            // إخفاء زر إضافة صنف جديد في شاشة المبيعات — يظهر فقط في شاشة الشراء
            btnAddNewProduct.Visible = _isPurchaseMode;
            btnAddNewProduct.Click += (s, e) =>
            {
                using (var frm = new FrmProducts())
                {
                    frm.ShowDialog(this);
                }
                LoadProducts();
            };
            btnCancel = Theme.MakeButton("❌ إلغاء", 100, 8, 125, 34, Color.FromArgb(120, 40, 40));
            
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
            _dtProducts = ProductDAL.GetAll(true);
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
                // Current Warehouse Stock
                DataTable dtStock = InventoryDAL.GetStock(_warehouseID);
                foreach (DataRow r in dtStock.Rows)
                {
                    int pid = Convert.ToInt32(r["ProductID"]);
                    decimal qty = r["BookQty"] != DBNull.Value ? Convert.ToDecimal(r["BookQty"]) : 0m;
                    _stockCache[pid] = qty;
                }

                // Global Stock across all Warehouses
                DataTable dtGlobal = InventoryDAL.GetStock(null);
                foreach (DataRow r in dtGlobal.Rows)
                {
                    int pid = Convert.ToInt32(r["ProductID"]);
                    decimal qty = r["BookQty"] != DBNull.Value ? Convert.ToDecimal(r["BookQty"]) : 0m;
                    _globalStockCache[pid] = qty;
                }
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
            foreach (DataRowView drv in _dvProducts)
            {
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
                        // Row 1: Old Price
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

                        // Row 2: Pending Price
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
        }

        private void ColorStockCell(int rowIdx, decimal stock)
        {
            var cell = dgProducts.Rows[rowIdx].Cells["StockQty"];
            if (stock <= 0)
                cell.Style.ForeColor = System.Drawing.Color.FromArgb(220, 70, 70);
            else if (stock < 10)
                cell.Style.ForeColor = System.Drawing.Color.FromArgb(220, 150, 40);
            else
                cell.Style.ForeColor = System.Drawing.Color.FromArgb(60, 190, 100);
        }

        private void ApplyFilter()
        {
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

            // Price range filter
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
            UpdateUnitsGrid();
            UpdateSelectedInputFields();
        }

        private void UpdateSelectedInputFields()
        {
            if (txtSelectedPurchasePrice == null || txtSelectedSalePrice == null) return;

            decimal sp = 0m;
            decimal pp = 0m;

            if (dgUnits != null && dgUnits.SelectedRows.Count > 0)
            {
                var row = dgUnits.SelectedRows[0];
                if (row.Cells["SalePrice"].Value != null)
                    decimal.TryParse(row.Cells["SalePrice"].Value.ToString(), out sp);
                if (row.Cells["PurchasePrice"].Value != null)
                    decimal.TryParse(row.Cells["PurchasePrice"].Value.ToString(), out pp);
            }
            else if (dgProducts != null && dgProducts.SelectedRows.Count > 0)
            {
                var row = dgProducts.SelectedRows[0];
                if (row.Cells["SalePrice"].Value != null)
                    decimal.TryParse(row.Cells["SalePrice"].Value.ToString(), out sp);

                int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                if (_dvProducts != null)
                {
                    foreach (DataRowView drv in _dvProducts)
                    {
                        if (Convert.ToInt32(drv.Row["ProductID"]) == pid)
                        {
                            if (drv.Row.Table.Columns.Contains("PurchasePrice") && drv.Row["PurchasePrice"] != DBNull.Value)
                                pp = Convert.ToDecimal(drv.Row["PurchasePrice"]);
                            break;
                        }
                    }
                }
            }

            txtSelectedPurchasePrice.Text = pp.ToString("F2");
            txtSelectedSalePrice.Text = sp.ToString("F2");
        }

        private void UpdateUnitsGrid()
        {
            dgUnits.Rows.Clear();
            if (dgProducts.SelectedRows.Count == 0) return;

            var selectedRow = dgProducts.SelectedRows[0];
            int productID = Convert.ToInt32(selectedRow.Cells["ProductID"].Value);

            // Find product row
            DataRow prodRow = null;
            foreach (DataRowView drv in _dvProducts)
            {
                if (Convert.ToInt32(drv.Row["ProductID"]) == productID)
                {
                    prodRow = drv.Row;
                    break;
                }
            }
            if (prodRow == null) return;

            decimal stock = _stockCache.TryGetValue(productID, out var s) ? s : 0m;
            decimal globalStock = _globalStockCache.TryGetValue(productID, out var gs) ? gs : 0m;

            // Units info
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
            dgUnits.Rows.Add(baseUnit, basePrice.ToString("F2"), baseStock.ToString("F2"), baseGlobalStock.ToString("F2"), baseFactor, basePP, baseUnit);

            // 2. Unit 2 (الوسطى)
            if (!string.IsNullOrEmpty(unit2))
            {
                decimal u2Stock = stock / unit2FactorVal;
                decimal u2GlobalStock = globalStock / unit2FactorVal;
                dgUnits.Rows.Add(unit2, unit2Price.ToString("F2"), u2Stock.ToString("F2"), u2GlobalStock.ToString("F2"), unit2FactorVal, unit2PP, unit2);
            }

            // 3. Unit 1 (الصغرى)
            if (!string.IsNullOrEmpty(unit1) && unit1 != baseUnit)
            {
                dgUnits.Rows.Add(unit1, unit1Price.ToString("F2"), stock.ToString("F2"), globalStock.ToString("F2"), 1m, unit1PP, unit1);
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
                    
                    dgUnits.Rows.Add($"صلاحية: {expStr} (دفعة #{batchID})", basePrice.ToString("F2"), batchQty.ToString("F2"), batchQty.ToString("F2"), 1m, basePP, $"BATCH:{batchID}:{expStr}");
                }
            }
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

            if (dgUnits.SelectedRows.Count > 0)
            {
                SelectedPrice = Convert.ToDecimal(dgUnits.SelectedRows[0].Cells["SalePrice"].Value);
                string matchedUnit = dgUnits.SelectedRows[0].Cells["MatchedUnit"].Value?.ToString() ?? "";
                if (matchedUnit.StartsWith("BATCH:"))
                {
                    var parts = matchedUnit.Split(':');
                    if (parts.Length >= 3)
                    {
                        SelectedBatchID = Convert.ToInt32(parts[1]);
                        if (DateTime.TryParse(parts[2], out DateTime exp))
                            SelectedExpiryDate = exp;
                    }
                    SelectedUnitName = dgProducts.SelectedRows[0].Cells["Unit"].Value?.ToString() ?? "";
                }
                else
                {
                    SelectedUnitName = matchedUnit;
                }
            }
            else if (dgUnits.Rows.Count > 0)
            {
                SelectedPrice = Convert.ToDecimal(dgUnits.Rows[0].Cells["SalePrice"].Value);
                string matchedUnit = dgUnits.Rows[0].Cells["MatchedUnit"].Value?.ToString() ?? "";
                if (matchedUnit.StartsWith("BATCH:"))
                {
                    var parts = matchedUnit.Split(':');
                    if (parts.Length >= 3)
                    {
                        SelectedBatchID = Convert.ToInt32(parts[1]);
                        if (DateTime.TryParse(parts[2], out DateTime exp))
                            SelectedExpiryDate = exp;
                    }
                    SelectedUnitName = dgProducts.SelectedRows[0].Cells["Unit"].Value?.ToString() ?? "";
                }
                else
                {
                    SelectedUnitName = matchedUnit;
                }
            }
            else
            {
                SelectedPrice = Convert.ToDecimal(dgProducts.SelectedRows[0].Cells["SalePrice"].Value);
                SelectedUnitName = dgProducts.SelectedRows[0].Cells["Unit"].Value?.ToString() ?? "";
            }

            // Parse Qty, Prices, Discount entered by user
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

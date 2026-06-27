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
        private TextBox txtSearch, txtBrandFilter, txtCompanyFilter, txtPriceFrom, txtPriceTo;
        private ComboBox cboCategory;
        private CheckBox chkShowZeroStock;
        private DataGridView dgProducts, dgUnits;
        private Button btnSelect, btnCancel;
        private DataTable _dtProducts;
        private DataView _dvProducts;
        private int? _warehouseID;
        private Dictionary<int, decimal> _stockCache = new Dictionary<int, decimal>();
        private Dictionary<int, decimal> _globalStockCache = new Dictionary<int, decimal>();

        public int SelectedProductID { get; private set; } = 0;
        public decimal SelectedPrice { get; private set; } = 0m;
        public string SelectedUnitName { get; private set; } = "";
        public int? SelectedBatchID { get; private set; } = null;
        public DateTime? SelectedExpiryDate { get; private set; } = null;

        public FrmProductSearch(int? warehouseID = null)
        {
            _warehouseID = warehouseID;
            InitUI();
            LoadCategories();
            LoadProducts();
        }

        private void InitUI()
        {
            this.Text = "🔍 بحث متقدم عن صنف";
            this.Size = new Size(820, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Top panel (Filters)
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 175, BackColor = Theme.BgCard, Padding = new Padding(12) };
            
            // Row 1: Search name/code & Category
            var lblSearch = new Label { Text = "ابحث بالاسم أو الكود :", Location = new Point(480, 20), AutoSize = false, Width = 150, ForeColor = Theme.TextMain, TextAlign = ContentAlignment.MiddleRight };
            txtSearch = new TextBox { Location = new Point(70, 16), Width = 400, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11) };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            txtSearch.KeyDown += TxtSearch_KeyDown;
            
            var lblCat = new Label { Text = "التصنيف:", Location = new Point(480, 52), AutoSize = false, Width = 150, ForeColor = Theme.TextMain, TextAlign = ContentAlignment.MiddleRight };
            cboCategory = new ComboBox { Location = new Point(70, 48), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            cboCategory.SelectedIndexChanged += (s, e) => ApplyFilter();

            // Row 2: Brand & Company
            var lblBrand = new Label { Text = "الماركة:", Location = new Point(480, 84), AutoSize = false, Width = 150, ForeColor = Theme.TextMain, TextAlign = ContentAlignment.MiddleRight };
            txtBrandFilter = new TextBox { Location = new Point(350, 80), Width = 120, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtBrandFilter.TextChanged += (s, e) => ApplyFilter();

            var lblCompany = new Label { Text = "الشركة المنتجة:", Location = new Point(230, 84), AutoSize = false, Width = 110, ForeColor = Theme.TextMain, TextAlign = ContentAlignment.MiddleRight };
            txtCompanyFilter = new TextBox { Location = new Point(70, 80), Width = 150, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtCompanyFilter.TextChanged += (s, e) => ApplyFilter();

            var lblPriceRange = new Label { Text = "السعر من:", Location = new Point(480, 116), AutoSize = false, Width = 150, ForeColor = Theme.TextMain, TextAlign = ContentAlignment.MiddleRight };
            txtPriceFrom = new TextBox { Location = new Point(320, 112), Width = 150, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtPriceFrom.TextChanged += (s, e) => ApplyFilter();

            var lblPriceTo = new Label { Text = "إلى:", Location = new Point(250, 116), AutoSize = false, Width = 60, ForeColor = Theme.TextMain, TextAlign = ContentAlignment.MiddleCenter };
            txtPriceTo = new TextBox { Location = new Point(70, 112), Width = 170, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            txtPriceTo.TextChanged += (s, e) => ApplyFilter();

            // Row 3: Zero Stock checkbox
            chkShowZeroStock = new CheckBox
            {
                Text = "إظهار الأصناف ذات الرصيد الصفري",
                Location = new Point(70, 144),
                Width = 400,
                Height = 24,
                ForeColor = Theme.TextMain,
                Checked = false
            };
            chkShowZeroStock.CheckedChanged += (s, e) => RefreshGrid();
            
            pnlSearch.Controls.AddRange(new Control[] { 
                lblSearch, txtSearch, lblCat, cboCategory, 
                lblBrand, txtBrandFilter, lblCompany, txtCompanyFilter, lblPriceRange, txtPriceFrom, lblPriceTo, txtPriceTo, 
                chkShowZeroStock 
            });

            // Grid Panel
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            
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
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 25 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 50 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 18 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 30 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 25 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQty", HeaderText = "الرصيد الفعلي", FillWeight = 27 });
            
            dgProducts.DoubleClick += DgProducts_DoubleClick;
            dgProducts.KeyDown += DgProducts_KeyDown;
            dgProducts.SelectionChanged += DgProducts_SelectionChanged;

            // Units Section (Bottom of grid panel)
            var pnlUnitsSection = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 180,
                Padding = new Padding(0, 8, 0, 0)
            };

            var lblUnitsTitle = new Label
            {
                Text = "📋 الوحدات والأسعار المتاحة للصنف المحدد :",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlUnitsSection.Controls.Add(lblUnitsTitle);

            dgUnits = new DataGridView
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
                DefaultCellStyle = dgProducts.DefaultCellStyle.Clone(),
                ColumnHeadersDefaultCellStyle = dgProducts.ColumnHeadersDefaultCellStyle.Clone(),
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

            pnlUnitsSection.Controls.Add(dgUnits);

            pnlGrid.Controls.Add(dgProducts);
            pnlGrid.Controls.Add(pnlUnitsSection);

            // Bottom panel (Actions)
            var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(10) };
            btnSelect = Theme.MakeButton("✅ اختيار", 470, 10, 100, 32, Theme.Accent);
            btnCancel = Theme.MakeButton("❌ إلغاء", 360, 10, 100, 32, Color.FromArgb(120, 40, 40));
            
            btnSelect.Click += BtnSelect_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            pnlActions.Controls.AddRange(new Control[] { btnSelect, btnCancel });

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
                            row["Unit"],
                            catName,
                            price.ToString("F2"), 
                            totalStock.ToString("F2")
                        );
                        ColorStockCell(rowIdx, totalStock);
                    }
                }
            }
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

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

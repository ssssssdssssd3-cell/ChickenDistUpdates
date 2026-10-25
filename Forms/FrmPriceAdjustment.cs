using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPriceAdjustment : Form
    {
        private ComboBox cboWarehouse, cboSupplier, cboCategory, cboAdjustmentMode, cboRounding;
        private TextBox txtSearch;
        private NumericUpDown nudValue;
        private CheckBox chkUpdateSubUnits, chkSelectAll;
        private Button btnLoad, btnPreview, btnSavePrices, btnPrintBarcodes, btnHistory, btnClose;
        private DataGridView dgvProducts;
        private Label lblSummary;
        private bool _isApplyingRule = false;

        public FrmPriceAdjustment()
        {
            InitUI();
            LoadCombos();
            LoadProducts();
        }

        private void InitUI()
        {
            this.Text = "⚡ تغيير أسعار البيع والتكلفة للأصناف والمخازن";
            this.Size = new Size(1300, 780);
            this.MinimumSize = new Size(1050, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Normal;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── 1. Top Filter Panel ──
            var pnlFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 10, 12, 8)
            };

            var lblWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 7, 6, 3) };
            cboWarehouse = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, Margin = new Padding(3, 4, 12, 3) };

            var lblSupp = new Label { Text = "المورد:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 7, 6, 3) };
            cboSupplier = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, Margin = new Padding(3, 4, 12, 3) };

            var lblCat = new Label { Text = "التصنيف:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 7, 6, 3) };
            cboCategory = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, Margin = new Padding(3, 4, 12, 3) };

            var lblSrch = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 7, 6, 3) };
            txtSearch = new TextBox { Width = 140, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain, Margin = new Padding(3, 4, 12, 3) };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadProducts(); };

            btnLoad = Theme.MakeButton("🔍 جلب الأصناف", 0, 0, 120, 32, Theme.Accent);
            btnLoad.Margin = new Padding(3, 2, 6, 3);
            btnLoad.Click += (s, e) => LoadProducts();

            pnlFilters.Controls.AddRange(new Control[] { lblWh, cboWarehouse, lblSupp, cboSupplier, lblCat, cboCategory, lblSrch, txtSearch, btnLoad });
            this.Controls.Add(pnlFilters);

            // ── 2. Adjustment Rules Bar ──
            var pnlRules = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12, 10, 12, 8)
            };

            var lblRule = new Label { Text = "قاعدة التعديل:", AutoSize = true, ForeColor = Color.White, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 7, 6, 3) };
            cboAdjustmentMode = new ComboBox { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 4, 12, 3) };
            cboAdjustmentMode.Items.AddRange(new string[]
            {
                "📈 حساب سعر البيع = سعر الشراء + نسبة ربح %",
                "📈 زيادة سعر البيع الحالي بنسبة %",
                "📉 تخفيض سعر البيع الحالي بنسبة %",
                "💵 زيادة سعر البيع الحالي بمبلغ ثابت (ج.م)",
                "💵 تخفيض سعر البيع الحالي بمبلغ ثابت (ج.م)",
                "🏷️ تعديل سعر الشراء / التكلفة بنسبة %",
                "⚡ تطبيق نسبة الربح المسجلة في كارت كل صنف"
            });
            cboAdjustmentMode.SelectedIndex = 0;
            cboAdjustmentMode.SelectedIndexChanged += (s, e) =>
            {
                nudValue.Enabled = cboAdjustmentMode.SelectedIndex != 6;
            };

            var lblVal = new Label { Text = "القيمة / النسبة:", AutoSize = true, ForeColor = Color.White, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 7, 6, 3) };
            nudValue = new NumericUpDown { Width = 75, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, Value = 20, TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(254, 240, 138), Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 4, 12, 3) };

            var lblRound = new Label { Text = "التقريب:", AutoSize = true, ForeColor = Color.White, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(3, 7, 6, 3) };
            cboRounding = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, Font = Theme.FontMain, Margin = new Padding(3, 4, 12, 3) };
            cboRounding.Items.AddRange(new string[] { "بدون تقريب (عشري)", "لأقرب 1 جنيه", "للأعلى لأقرب 1 جنيه", "لأقرب 5 جنيه" });
            cboRounding.SelectedIndex = 0;

            chkUpdateSubUnits = new CheckBox { Text = "تحديث أسعار الوحدات الفرعية تلقائياً", AutoSize = true, ForeColor = Color.FromArgb(254, 240, 138), Checked = true, Font = new Font(Theme.FontMain, FontStyle.Bold), Margin = new Padding(6, 7, 12, 3) };

            btnPreview = Theme.MakeButton("⚡ تطبيق المعادلة", 0, 0, 135, 32, Color.FromArgb(14, 116, 144));
            btnPreview.Margin = new Padding(3, 2, 6, 3);
            btnPreview.Click += (s, e) => ApplyAdjustmentRule();

            pnlRules.Controls.AddRange(new Control[] { lblRule, cboAdjustmentMode, lblVal, nudValue, lblRound, cboRounding, chkUpdateSubUnits, btnPreview });
            this.Controls.Add(pnlRules);

            // ── 3. DataGridView ──
            dgvProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 }
            };

            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font(Theme.FontMain, FontStyle.Bold);
            dgvProducts.ColumnHeadersHeight = 36;
            dgvProducts.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            var colSel = new DataGridViewCheckBoxColumn { Name = "colSelect", HeaderText = "✔", Width = 40, FillWeight = 25 };
            dgvProducts.Columns.Add(colSel);
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", ReadOnly = true, FillWeight = 55 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", ReadOnly = true, FillWeight = 140 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", ReadOnly = true, FillWeight = 65 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", ReadOnly = true, FillWeight = 45 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "المورد", ReadOnly = true, FillWeight = 75 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQty", HeaderText = "الرصيد", ReadOnly = true, FillWeight = 50, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "OldBuyPrice", HeaderText = "شراء حالي", ReadOnly = true, FillWeight = 55, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewBuyPrice", HeaderText = "شراء جديد ✏️", ReadOnly = false, FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, BackColor = Color.FromArgb(254, 249, 195), Font = new Font(Theme.FontMain, FontStyle.Bold) } });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "OldSalePrice", HeaderText = "بيع حالي", ReadOnly = true, FillWeight = 55, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewSalePrice", HeaderText = "بيع جديد ✏️", ReadOnly = false, FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, BackColor = Color.FromArgb(220, 252, 231), Font = new Font(Theme.FontMain, FontStyle.Bold) } });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffAmount", HeaderText = "الفرق", ReadOnly = true, FillWeight = 45, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font(Theme.FontMain, FontStyle.Bold) } });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewMarginPct", HeaderText = "نسبة الربح %", ReadOnly = true, FillWeight = 50, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CardMarginPct", Visible = false });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Factor", Visible = false });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit3Factor", Visible = false });

            dgvProducts.CellValueChanged += DgvProducts_CellValueChanged;
            dgvProducts.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvProducts.IsCurrentCellDirty) dgvProducts.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            this.Controls.Add(dgvProducts);

            // ── 4. Bottom Action Panel ──
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 8, 12, 8)
            };

            var pnlBottomRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            chkSelectAll = new CheckBox
            {
                Text = "تحديد الكل",
                AutoSize = true,
                Checked = true,
                ForeColor = Theme.TextMain,
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                Margin = new Padding(3, 5, 12, 3)
            };
            chkSelectAll.CheckedChanged += (s, e) =>
            {
                foreach (DataGridViewRow row in dgvProducts.Rows)
                {
                    row.Cells["colSelect"].Value = chkSelectAll.Checked;
                }
                UpdateSummary();
            };

            lblSummary = new Label
            {
                Text = "إجمالي الأصناف: 0 | المحدد: 0",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                Margin = new Padding(3, 7, 10, 3)
            };

            pnlBottomRight.Controls.AddRange(new Control[] { chkSelectAll, lblSummary });

            var pnlBottomLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnSavePrices = Theme.MakeButton("💾 اعتماد وحفظ الأسعار الجديدة", 0, 0, 210, 36, Theme.Accent);
            btnSavePrices.Margin = new Padding(4, 0, 4, 0);
            btnSavePrices.Click += BtnSavePrices_Click;

            btnPrintBarcodes = Theme.MakeButton("🖨️ طباعة باركود المعدل", 0, 0, 160, 36, Color.FromArgb(30, 64, 175));
            btnPrintBarcodes.Margin = new Padding(4, 0, 4, 0);
            btnPrintBarcodes.Click += BtnPrintBarcodes_Click;

            btnHistory = Theme.MakeButton("📜 سجل تغيرات الأسعار", 0, 0, 160, 36, Color.FromArgb(71, 85, 105));
            btnHistory.Margin = new Padding(4, 0, 4, 0);
            btnHistory.Click += (s, e) => new FrmPriceChangeHistory().ShowDialog();

            btnClose = Theme.MakeButton("❌ إغلاق", 0, 0, 90, 36, Color.FromArgb(100, 116, 139));
            btnClose.Margin = new Padding(4, 0, 4, 0);
            btnClose.Click += (s, e) => this.Close();

            pnlBottomLeft.Controls.AddRange(new Control[] { btnSavePrices, btnPrintBarcodes, btnHistory, btnClose });

            pnlBottom.Controls.Add(pnlBottomRight);
            pnlBottom.Controls.Add(pnlBottomLeft);
            this.Controls.Add(pnlBottom);

            dgvProducts.BringToFront();

            Theme.ApplyFormRTL(this);
            this.RightToLeftLayout = false;
        }

        private void LoadCombos()
        {
            try
            {
                // Warehouses
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add(new ComboItem(0, "-- جميع المخازن --"));
                var dtWh = WarehouseDAL.GetAll();
                foreach (DataRow r in dtWh.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
                }
                cboWarehouse.SelectedIndex = 0;

                // Categories
                cboCategory.Items.Clear();
                cboCategory.Items.Add(new ComboItem(0, "-- جميع التصنيفات --"));
                var dtCat = CategoryDAL.GetAll(true);
                foreach (DataRow r in dtCat.Rows)
                {
                    cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                }
                cboCategory.SelectedIndex = 0;

                // Suppliers
                cboSupplier.Items.Clear();
                cboSupplier.Items.Add(new ComboItem(0, "-- جميع الموردين --"));
                var dtSupp = SupplierDAL.GetAll();
                foreach (DataRow r in dtSupp.Rows)
                {
                    cboSupplier.Items.Add(new ComboItem(Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
                }
                cboSupplier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadCombos in FrmPriceAdjustment", ex);
            }
        }

        private void LoadProducts()
        {
            try
            {
                DbHelper.EnsurePriceMarginAndHistorySchema();
                int whId = (cboWarehouse.SelectedItem is ComboItem w && w.ID > 0) ? w.ID : 0;
                int suppId = (cboSupplier.SelectedItem is ComboItem s && s.ID > 0) ? s.ID : 0;
                int catId = (cboCategory.SelectedItem is ComboItem c && c.ID > 0) ? c.ID : 0;
                string srch = txtSearch.Text.Trim();

                string sql = @"
                    SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit,
                           ISNULL(c.CategoryName, N'-- بدون --') AS CategoryName,
                           ISNULL(p.PurchasePrice, 0) AS PurchasePrice,
                           ISNULL(p.LastPurchasePrice, ISNULL(p.PurchasePrice, 0)) AS EffectiveBuyPrice,
                           ISNULL(p.SalePrice, 0) AS SalePrice,
                           ISNULL(p.SemiWholesalePrice, 0) AS SemiWholesalePrice,
                           ISNULL(p.WholesalePrice, 0) AS WholesalePrice,
                           ISNULL(p.ProfitMarginPct, 0) AS ProfitMarginPct,
                           ISNULL(p.Unit2Factor, 1) AS Unit2Factor,
                           ISNULL(p.Unit3Factor, 1) AS Unit3Factor,
                           ISNULL(supp.SupplierName, N'--') AS SupplierName,
                           ISNULL(stk.TotalStock, 0) AS CurrentStock
                    FROM Products p WITH (NOLOCK)
                    LEFT JOIN Categories c WITH (NOLOCK) ON p.CategoryID = c.CategoryID
                    OUTER APPLY (
                        SELECT TOP 1 s.SupplierName
                        FROM PurchaseItems pi WITH (NOLOCK)
                        JOIN Purchases pur WITH (NOLOCK) ON pi.PurchaseID = pur.PurchaseID
                        JOIN Suppliers s WITH (NOLOCK) ON pur.SupplierID = s.SupplierID
                        WHERE pi.ProductID = p.ProductID
                        ORDER BY pur.PurchaseDate DESC, pur.PurchaseID DESC
                    ) supp
                    OUTER APPLY (
                        SELECT SUM(Quantity) AS TotalStock
                        FROM ProductStock WITH (NOLOCK)
                        WHERE ProductID = p.ProductID" + (whId > 0 ? " AND WarehouseID = " + whId : "") + @"
                    ) stk
                    WHERE p.IsActive = 1";

                var pars = new List<System.Data.SqlClient.SqlParameter>();

                if (catId > 0)
                {
                    sql += " AND p.CategoryID = @catId";
                    pars.Add(DbHelper.P("@catId", catId));
                }

                if (suppId > 0)
                {
                    sql += @" AND EXISTS (
                                SELECT 1 FROM PurchaseItems pi WITH (NOLOCK)
                                JOIN Purchases pur WITH (NOLOCK) ON pi.PurchaseID = pur.PurchaseID
                                WHERE pi.ProductID = p.ProductID AND pur.SupplierID = @suppId
                             )";
                    pars.Add(DbHelper.P("@suppId", suppId));
                }

                if (!string.IsNullOrWhiteSpace(srch))
                {
                    sql += " AND (p.ProductCode LIKE @srch OR p.ProductName LIKE @srch OR p.InternationalCode LIKE @srch OR p.PartNumber LIKE @srch)";
                    pars.Add(DbHelper.P("@srch", "%" + srch + "%"));
                }

                sql += " ORDER BY p.ProductName";

                var dt = DbHelper.Query(sql, pars.ToArray());
                dgvProducts.Rows.Clear();

                foreach (DataRow r in dt.Rows)
                {
                    decimal buy = Convert.ToDecimal(r["EffectiveBuyPrice"]);
                    if (buy <= 0) buy = Convert.ToDecimal(r["PurchasePrice"]);
                    decimal sale = Convert.ToDecimal(r["SalePrice"]);
                    decimal semi = Convert.ToDecimal(r["SemiWholesalePrice"]);
                    decimal whole = Convert.ToDecimal(r["WholesalePrice"]);
                    decimal margin = buy > 0 && sale > buy ? Math.Round((sale - buy) / buy * 100m, 1) : 0m;

                    dgvProducts.Rows.Add(
                        true,
                        r["ProductID"],
                        r["ProductCode"],
                        r["ProductName"],
                        r["CategoryName"],
                        r["Unit"],
                        r["SupplierName"],
                        Convert.ToDecimal(r["CurrentStock"]).ToString("G29"),
                        buy.ToString("N2"),
                        buy.ToString("N2"),
                        sale.ToString("N2"),
                        sale.ToString("N2"),
                        semi.ToString("N2"),
                        semi.ToString("N2"),
                        whole.ToString("N2"),
                        whole.ToString("N2"),
                        "0.00",
                        margin.ToString("G29") + "%",
                        r["ProfitMarginPct"],
                        r["Unit2Factor"],
                        r["Unit3Factor"]
                    );
                }

                chkSelectAll.Checked = true;
                UpdateSummary();
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadProducts in FrmPriceAdjustment", ex);
                MessageBox.Show("حدث خطأ أثناء تحميل الأصناف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyAdjustmentRule()
        {
            if (dgvProducts.Rows.Count == 0)
            {
                MessageBox.Show("يرجى جلب الأصناف أولاً لتطبيق القاعدة عليها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int mode = cboAdjustmentMode.SelectedIndex;
            decimal val = nudValue.Value;
            int rounding = cboRounding.SelectedIndex;

            _isApplyingRule = true;
            int countModified = 0;

            try
            {
                foreach (DataGridViewRow row in dgvProducts.Rows)
                {
                    bool isSelected = Convert.ToBoolean(row.Cells["colSelect"].Value ?? false);
                    if (!isSelected) continue;

                    decimal oldBuy = Convert.ToDecimal(row.Cells["OldBuyPrice"].Value ?? 0);
                    decimal oldSale = Convert.ToDecimal(row.Cells["OldSalePrice"].Value ?? 0);
                    decimal newBuy = oldBuy;
                    decimal newSale = oldSale;

                    switch (mode)
                    {
                        case 0: // حساب سعر البيع من سعر الشراء بنسبة ربح %
                            if (oldBuy > 0)
                            {
                                newSale = oldBuy * (1m + val / 100m);
                            }
                            break;

                        case 1: // زيادة سعر البيع الحالي بنسبة %
                            newSale = oldSale * (1m + val / 100m);
                            break;

                        case 2: // تخفيض سعر البيع الحالي بنسبة %
                            newSale = Math.Max(0, oldSale * (1m - val / 100m));
                            break;

                        case 3: // زيادة سعر البيع الحالي بمبلغ ثابت
                            newSale = oldSale + val;
                            break;

                        case 4: // تخفيض سعر البيع الحالي بمبلغ ثابت
                            newSale = Math.Max(0, oldSale - val);
                            break;

                        case 5: // تعديل سعر الشراء / التكلفة بنسبة %
                            newBuy = oldBuy * (1m + val / 100m);
                            break;

                        case 6: // تطبيق نسبة الربح المسجلة في كارت كل صنف
                            decimal cardMargin = Convert.ToDecimal(row.Cells["CardMarginPct"].Value ?? 0);
                            if (cardMargin > 0 && oldBuy > 0)
                            {
                                newSale = oldBuy * (1m + cardMargin / 100m);
                            }
                            break;
                    }

                    // تطبيق خيارات التقريب
                    newSale = ApplyRounding(newSale, rounding);
                    newBuy = ApplyRounding(newBuy, rounding);

                    row.Cells["NewBuyPrice"].Value = newBuy.ToString("N2");
                    row.Cells["NewSalePrice"].Value = newSale.ToString("N2");

                    decimal diff = newSale - oldSale;
                    row.Cells["DiffAmount"].Value = (diff >= 0 ? "+" : "") + diff.ToString("N2");

                    var diffCell = row.Cells["DiffAmount"];
                    if (diff > 0)
                    {
                        diffCell.Style.ForeColor = Color.FromArgb(22, 101, 52);
                    }
                    else if (diff < 0)
                    {
                        diffCell.Style.ForeColor = Color.FromArgb(185, 28, 28);
                    }
                    else
                    {
                        diffCell.Style.ForeColor = Theme.TextMain;
                    }

                    decimal margin = newBuy > 0 && newSale > newBuy ? Math.Round((newSale - newBuy) / newBuy * 100m, 1) : 0m;
                    row.Cells["NewMarginPct"].Value = margin.ToString("G29") + "%";

                    countModified++;
                }

                MessageBox.Show($"✅ تم احتساب وتطبيق المعادلة على ({countModified}) صنف محدد.\nيمكنك مراجعة الأرقام بالجدول وتعديل أي صنف يدوياً، ثم الضغط على [اعتماد وحفظ الأسعار].",
                    "معاينة الأسعار الجديدة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                _isApplyingRule = false;
            }
        }

        private decimal ApplyRounding(decimal val, int roundingMode)
        {
            switch (roundingMode)
            {
                case 1: // لأقرب 1 جنيه
                    return Math.Round(val, MidpointRounding.AwayFromZero);
                case 2: // للأعلى لأقرب 1 جنيه
                    return Math.Ceiling(val);
                case 3: // لأقرب 5 جنيه
                    return Math.Round(val / 5m, MidpointRounding.AwayFromZero) * 5m;
                default: // بدون تقريب (عشري)
                    return Math.Round(val, 2);
            }
        }

        private void DgvProducts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isApplyingRule || e.RowIndex < 0) return;
            var colName = dgvProducts.Columns[e.ColumnIndex].Name;

            if (colName == "NewSalePrice" || colName == "NewBuyPrice" || colName == "NewSemiPrice" || colName == "NewWholePrice")
            {
                var row = dgvProducts.Rows[e.RowIndex];
                decimal oldSale = Convert.ToDecimal(row.Cells["OldSalePrice"].Value ?? 0);
                decimal.TryParse(row.Cells["NewSalePrice"].Value?.ToString(), out decimal newSale);
                decimal.TryParse(row.Cells["NewBuyPrice"].Value?.ToString(), out decimal newBuy);

                decimal diff = newSale - oldSale;
                row.Cells["DiffAmount"].Value = (diff >= 0 ? "+" : "") + diff.ToString("N2");

                var diffCell = row.Cells["DiffAmount"];
                if (diff > 0) diffCell.Style.ForeColor = Color.FromArgb(22, 101, 52);
                else if (diff < 0) diffCell.Style.ForeColor = Color.FromArgb(185, 28, 28);
                else diffCell.Style.ForeColor = Theme.TextMain;

                decimal margin = newBuy > 0 && newSale > newBuy ? Math.Round((newSale - newBuy) / newBuy * 100m, 1) : 0m;
                row.Cells["NewMarginPct"].Value = margin.ToString("G29") + "%";
            }
            else if (colName == "colSelect")
            {
                UpdateSummary();
            }
        }

        private void UpdateSummary()
        {
            int total = dgvProducts.Rows.Count;
            int selected = 0;
            foreach (DataGridViewRow r in dgvProducts.Rows)
            {
                if (Convert.ToBoolean(r.Cells["colSelect"].Value ?? false)) selected++;
            }
            lblSummary.Text = $"إجمالي الأصناف: {total} | الأصناف المحددة للتعديل: {selected}";
        }

        private void BtnSavePrices_Click(object sender, EventArgs e)
        {
            var rowsToSave = new List<DataGridViewRow>();
            foreach (DataGridViewRow r in dgvProducts.Rows)
            {
                if (Convert.ToBoolean(r.Cells["colSelect"].Value ?? false))
                {
                    decimal oldBuy = Convert.ToDecimal(r.Cells["OldBuyPrice"].Value ?? 0);
                    decimal oldSale = Convert.ToDecimal(r.Cells["OldSalePrice"].Value ?? 0);
                    decimal oldSemi = Convert.ToDecimal(r.Cells["OldSemiPrice"].Value ?? 0);
                    decimal oldWhole = Convert.ToDecimal(r.Cells["OldWholePrice"].Value ?? 0);
                    decimal.TryParse(r.Cells["NewBuyPrice"].Value?.ToString(), out decimal newBuy);
                    decimal.TryParse(r.Cells["NewSalePrice"].Value?.ToString(), out decimal newSale);
                    decimal.TryParse(r.Cells["NewSemiPrice"].Value?.ToString(), out decimal newSemi);
                    decimal.TryParse(r.Cells["NewWholePrice"].Value?.ToString(), out decimal newWhole);

                    if (Math.Abs(newBuy - oldBuy) > 0.005m || Math.Abs(newSale - oldSale) > 0.005m || Math.Abs(newSemi - oldSemi) > 0.005m || Math.Abs(newWhole - oldWhole) > 0.005m)
                    {
                        rowsToSave.Add(r);
                    }
                }
            }

            if (rowsToSave.Count == 0)
            {
                MessageBox.Show("لم يتم تعديل أي أسعار للأصناف المحددة حتى الآن لحفظها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"هل أنت متأكد من اعتماد وتحديث الأسعار الجديدة لـ ({rowsToSave.Count}) صنف بقاعدة البيانات؟\nسيتم تسجيل كل حركة بسجل تغيرات الأسعار للرقابة والمراجعة.",
                "تأكيد اعتماد الأسعار", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            int savedCount = 0;
            int? whId = (cboWarehouse.SelectedItem is ComboItem w && w.ID > 0) ? (int?)w.ID : null;
            int? suppId = (cboSupplier.SelectedItem is ComboItem s && s.ID > 0) ? (int?)s.ID : null;
            string suppName = cboSupplier.Text.Trim();
            if (suppName.StartsWith("--")) suppName = null;

            bool updateSubUnits = chkUpdateSubUnits.Checked;

            try
            {
                foreach (var row in rowsToSave)
                {
                    int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                    string code = row.Cells["ProductCode"].Value?.ToString();
                    string name = row.Cells["ProductName"].Value?.ToString();

                    decimal oldBuy = Convert.ToDecimal(row.Cells["OldBuyPrice"].Value ?? 0);
                    decimal oldSale = Convert.ToDecimal(row.Cells["OldSalePrice"].Value ?? 0);
                    decimal.TryParse(row.Cells["NewBuyPrice"].Value?.ToString(), out decimal newBuy);
                    decimal.TryParse(row.Cells["NewSalePrice"].Value?.ToString(), out decimal newSale);

                    decimal u2f = Convert.ToDecimal(row.Cells["Unit2Factor"].Value ?? 1m);
                    decimal u3f = Convert.ToDecimal(row.Cells["Unit3Factor"].Value ?? 1m);
                    if (u2f <= 0) u2f = 1m;
                    if (u3f <= 0) u3f = 1m;
                    decimal totalFactor = u2f * u3f;

                    decimal margin = newBuy > 0 && newSale > newBuy ? Math.Round((newSale - newBuy) / newBuy * 100m, 2) : 0m;

                    // 1. تحديث جدول الأصناف Products
                    if (updateSubUnits && totalFactor > 1m)
                    {
                        decimal newU2Sale = Math.Round(newSale / u3f, 2);
                        decimal newU1Sale = Math.Round(newU2Sale / u2f, 2);
                        decimal newU2Buy = Math.Round(newBuy / u3f, 2);
                        decimal newU1Buy = Math.Round(newU2Buy / u2f, 2);

                        DbHelper.Execute(@"
                            UPDATE Products 
                            SET PurchasePrice = @np,
                                LastPurchasePrice = @np,
                                SalePrice = @nsp,
                                ProfitMarginPct = @margin,
                                Unit2SalePrice = @u2sp,
                                Unit1SalePrice = @u1sp,
                                Unit2PurchasePrice = @u2pp,
                                Unit1PurchasePrice = @u1pp,
                                CostPrice = CASE WHEN @useLastCost = 1 THEN @u1pp ELSE CostPrice END
                            WHERE ProductID = @pid",
                            DbHelper.P("@np", newBuy),
                            DbHelper.P("@bc", totalFactor > 1m ? Math.Round(newBuy / totalFactor, 4) : newBuy),
                            DbHelper.P("@nsp", newSale),
                            DbHelper.P("@margin", margin),
                            DbHelper.P("@u2sp", newU2Sale),
                            DbHelper.P("@u1sp", newU1Sale),
                            DbHelper.P("@u2pp", newU2Buy),
                            DbHelper.P("@u1pp", newU1Buy),
                            DbHelper.P("@useLastCost", AppConfig.UseLastPurchasePriceAsCost ? 1 : 0),
                            DbHelper.P("@pid", pid));
                    }
                    else
                    {
                        DbHelper.Execute(@"
                            UPDATE Products 
                            SET PurchasePrice = @np,
                                LastPurchasePrice = @np,
                                SalePrice = @nsp,
                                ProfitMarginPct = @margin,
                                CostPrice = CASE WHEN @useLastCost = 1 THEN @bc ELSE CostPrice END
                            WHERE ProductID = @pid",
                            DbHelper.P("@np", newBuy),
                            DbHelper.P("@bc", totalFactor > 1m ? Math.Round(newBuy / totalFactor, 4) : newBuy),
                            DbHelper.P("@nsp", newSale),
                            DbHelper.P("@margin", margin),
                            DbHelper.P("@useLastCost", AppConfig.UseLastPurchasePriceAsCost ? 1 : 0),
                            DbHelper.P("@pid", pid));
                    }

                    // 2. تسجيل التغيير في سجل تاريخ الأسعار PriceChangeHistory
                    decimal changePct = oldSale > 0 ? Math.Round(((newSale - oldSale) / oldSale) * 100m, 2) : 0m;
                    string changeTypeDesc = cboAdjustmentMode.Text.Trim();

                    ProductDAL.RecordPriceChange(
                        pid, code, name,
                        oldBuy, newBuy, oldSale, newSale,
                        changeTypeDesc, changePct,
                        suppId, suppName, whId,
                        Session.EmpName ?? "المستخدم",
                        $"تعديل أسعار جماعي - {changeTypeDesc}");

                    savedCount++;
                }

                ProductCache.Refresh();
                StockCache.Invalidate();
                FrmQuickAdd.RaiseProductSaved();

                MessageBox.Show($"✅ تم اعتماد وتحديث أسعار ({savedCount}) صنف بنجاح في قاعدة البيانات وتسجيلها في سجل الأسعار.",
                    "تم الحفظ بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadProducts();
            }
            catch (Exception ex)
            {
                AppLogger.Error("BtnSavePrices_Click in FrmPriceAdjustment", ex);
                MessageBox.Show("حدث خطأ أثناء حفظ الأسعار: " + ex.Message, "خطأ في الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrintBarcodes_Click(object sender, EventArgs e)
        {
            try
            {
                var printItems = new List<BarcodePrintItem>();
                foreach (DataGridViewRow r in dgvProducts.Rows)
                {
                    if (Convert.ToBoolean(r.Cells["colSelect"].Value ?? false))
                    {
                        int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
                        string code = r.Cells["ProductCode"].Value?.ToString();
                        string name = r.Cells["ProductName"].Value?.ToString();
                        decimal.TryParse(r.Cells["NewSalePrice"].Value?.ToString(), out decimal salePrice);

                        printItems.Add(new BarcodePrintItem
                        {
                            ProductID = pid,
                            ProductCode = code,
                            ProductName = name,
                            Price = salePrice,
                            PrintQty = 1
                        });
                    }
                }

                if (printItems.Count == 0)
                {
                    MessageBox.Show("يرجى اختيار أصناف أولاً لطباعة باركود لها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var frm = new FrmBulkPrintBarcodes(printItems))
                {
                    frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("BtnPrintBarcodes_Click in FrmPriceAdjustment", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة طباعة الباركود: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

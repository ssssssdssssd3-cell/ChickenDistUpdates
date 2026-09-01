using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة التصنيع الثابت (وفق الوصفة المعيارية BOM مع مصاريف التشغيل والتعليق تحت التحضير)
    /// </summary>
    public class FrmFixedProduction : Form
    {
        private int _currentProductionID = 0;
        private string _currentOrderCode = "";
        private string _currentStatus = "InPreparation";
        private BOMModel _currentBOM = null;

        private int _selectedFinishedProductID = 0;
        private string _selectedFinishedProductCode = "";
        private string _selectedFinishedProductName = "";

        // Controls - Header
        private Label lblOrderCode;
        private Label lblStatusBadge;
        private DateTimePicker dtpOrderDate;
        private ComboBox cboWarehouse;
        private TextBox txtFinishedProduct;
        private Button btnBrowseFinished;
        private Button btnBrowseBOM;
        private NumericUpDown numProducedQty;
        private TextBox txtUnitName;
        private TextBox txtNotes;

        // Controls - Expenses
        private NumericUpDown numExtraExpenses;
        private TextBox txtExpensesNotes;

        // Grid
        private DataGridView dgItems;

        // Quick add extra raw material
        private Button btnAddExtraRaw;

        // Cost Summary Cards
        private Label lblRawCost;
        private Label lblExtraCost;
        private Label lblTotalCost;
        private Label lblUnitCost;

        // Action Buttons
        private Button btnSuspend;
        private Button btnComplete;
        private Button btnResume;
        private Button btnCancelOrder;
        private Button btnNew;
        private Button btnPrint;

        public FrmFixedProduction(int productionId = 0)
        {
            _currentProductionID = productionId;
            InitUI();
            LoadWarehouses();

            if (_currentProductionID > 0)
            {
                LoadExistingOrder(_currentProductionID);
            }
            else
            {
                ResetForm();
            }
        }

        private void InitUI()
        {
            this.Text = "🏭 أمر تصنيع ثابت (وفق الوصفة المعيارية BOM)";
            this.Size = new Size(1180, 750);
            this.MinimumSize = new Size(1020, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── Top Header Panel ──
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Theme.BgCard,
                Padding = new Padding(12)
            };
            this.Controls.Add(pnlHeader);

            // Row 1: Title, OrderCode, Status Badge, Date, Warehouse
            var lblTitle = new Label
            {
                Text = "🏭 أمر تصنيع ثابت",
                Location = new Point(12, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlHeader.Controls.Add(lblTitle);

            lblOrderCode = new Label
            {
                Text = "كود الأمر: PRD-...",
                Location = new Point(200, 14),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(243, 198, 35)
            };
            pnlHeader.Controls.Add(lblOrderCode);

            lblStatusBadge = new Label
            {
                Text = "⏳ مسودة جديدة",
                Location = new Point(390, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(100, 116, 139),
                Padding = new Padding(8, 4, 8, 4)
            };
            pnlHeader.Controls.Add(lblStatusBadge);

            var lblDate = new Label { Text = "التاريخ:", Location = new Point(570, 14), AutoSize = true };
            pnlHeader.Controls.Add(lblDate);

            dtpOrderDate = new DateTimePicker
            {
                Location = new Point(620, 11),
                Width = 140,
                Format = DateTimePickerFormat.Short,
                Font = Theme.FontMain
            };
            pnlHeader.Controls.Add(dtpOrderDate);

            var lblWh = new Label { Text = "المخزن:", Location = new Point(780, 14), AutoSize = true };
            pnlHeader.Controls.Add(lblWh);

            cboWarehouse = new ComboBox
            {
                Location = new Point(835, 11),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlHeader.Controls.Add(cboWarehouse);

            btnResume = Theme.MakeButton("🔄 استرجاع أمر معلق", 1030, 8, 130, 34, Color.FromArgb(51, 65, 85));
            btnResume.Click += (s, e) => ShowSuspendedOrdersDialog();
            pnlHeader.Controls.Add(btnResume);

            // Row 2: Finished Product selection, Produced Quantity, Unit
            var lblFpTitle = new Label
            {
                Text = "🎯 المنتج النهائي المطلوب تصنيعه:",
                Location = new Point(12, 55),
                AutoSize = true,
                ForeColor = Color.Silver
            };
            pnlHeader.Controls.Add(lblFpTitle);

            txtFinishedProduct = new TextBox
            {
                Location = new Point(12, 78),
                Width = 270,
                Height = 32,
                ReadOnly = false,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            txtFinishedProduct.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    string q = txtFinishedProduct.Text.Trim();
                    if (string.IsNullOrEmpty(q)) { SelectFinishedProduct(); return; }
                    var dt = DbHelper.Query("SELECT TOP 1 ProductID FROM Products WHERE ProductCode = @q OR Unit1Barcode = @q OR Unit2Barcode = @q OR ProductName LIKE '%' + @q + '%'", DbHelper.P("@q", q));
                    if (dt != null && dt.Rows.Count > 0) LoadFinishedProduct(Convert.ToInt32(dt.Rows[0]["ProductID"]));
                    else SelectFinishedProduct(q);
                }
            };
            pnlHeader.Controls.Add(txtFinishedProduct);

            btnBrowseFinished = Theme.MakeButton("🔍 بحث بالأصناف", 288, 76, 115, 34, Theme.Primary);
            btnBrowseFinished.Click += (s, e) => SelectFinishedProduct();
            pnlHeader.Controls.Add(btnBrowseFinished);

            btnBrowseBOM = Theme.MakeButton("📋 الوصفات الجاهزة", 408, 76, 125, 34, Color.FromArgb(14, 116, 144));
            btnBrowseBOM.Click += (s, e) => SelectFromRegisteredBOMs();
            pnlHeader.Controls.Add(btnBrowseBOM);

            var lblQtyTitle = new Label { Text = "الكمية المراد إنتاجها:", Location = new Point(545, 55), AutoSize = true, ForeColor = Color.Silver };
            pnlHeader.Controls.Add(lblQtyTitle);

            numProducedQty = new NumericUpDown
            {
                Location = new Point(545, 78),
                Width = 110,
                Height = 32,
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 1000000m,
                Value = 1m,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            numProducedQty.ValueChanged += (s, e) => OnProducedQtyChanged();
            pnlHeader.Controls.Add(numProducedQty);

            var lblUnit = new Label { Text = "الوحدة:", Location = new Point(665, 55), AutoSize = true, ForeColor = Color.Silver };
            pnlHeader.Controls.Add(lblUnit);

            txtUnitName = new TextBox
            {
                Location = new Point(665, 78),
                Width = 80,
                Height = 32,
                Text = "قطعة",
                ReadOnly = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlHeader.Controls.Add(txtUnitName);

            var lblNotesTitle = new Label { Text = "ملاحظات أمر التشغيل:", Location = new Point(755, 55), AutoSize = true, ForeColor = Color.Silver };
            pnlHeader.Controls.Add(lblNotesTitle);

            txtNotes = new TextBox
            {
                Location = new Point(755, 78),
                Width = 280,
                Height = 32,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlHeader.Controls.Add(txtNotes);

            // Row 3: Operating Expenses
            var lblExp = new Label { Text = "⚡ مصاريف تشغيل إضافية (كهرباء/عمالة/صيانة):", Location = new Point(12, 122), AutoSize = true, ForeColor = Color.Orange };
            pnlHeader.Controls.Add(lblExp);

            numExtraExpenses = new NumericUpDown
            {
                Location = new Point(310, 120),
                Width = 120,
                Height = 28,
                DecimalPlaces = 2,
                Minimum = 0m,
                Maximum = 1000000m,
                Value = 0m,
                Font = Theme.FontBold,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            numExtraExpenses.ValueChanged += (s, e) => RecalculateTotals();
            pnlHeader.Controls.Add(numExtraExpenses);

            var lblExpNotes = new Label { Text = "بيان المصروفات:", Location = new Point(445, 122), AutoSize = true, ForeColor = Color.Silver };
            pnlHeader.Controls.Add(lblExpNotes);

            txtExpensesNotes = new TextBox
            {
                Location = new Point(545, 120),
                Width = 320,
                Height = 28,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlHeader.Controls.Add(txtExpensesNotes);

            btnAddExtraRaw = Theme.MakeButton("➕ إضافة مادة خام إضافية", 880, 118, 170, 30, Color.FromArgb(51, 65, 85));
            btnAddExtraRaw.Click += (s, e) => AddExtraRawMaterial();
            pnlHeader.Controls.Add(btnAddExtraRaw);

            // ── Center Grid ──
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowNum", HeaderText = "م", FillWeight = 8, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductCode", HeaderText = "كود المادة الخام", FillWeight = 18, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductName", HeaderText = "اسم المادة الخام", FillWeight = 38, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المستهلكة", FillWeight = 16 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 12 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "سعر التكلفة", FillWeight = 15, ReadOnly = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "إجمالي التكلفة", FillWeight = 18, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 20 });

            var colDelete = new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "حذف",
                Text = "❌",
                UseColumnTextForButtonValue = true,
                FillWeight = 10
            };
            dgItems.Columns.Add(colDelete);

            dgItems.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && (dgItems.Columns[e.ColumnIndex].Name == "Quantity" || dgItems.Columns[e.ColumnIndex].Name == "UnitCost"))
                {
                    UpdateRowTotal(e.RowIndex);
                    RecalculateTotals();
                }
            };
            dgItems.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dgItems.Columns["colDelete"].Index)
                {
                    if (_currentStatus == "Completed")
                    {
                        MessageBox.Show("لا يمكن حذف أصناف من أمر تم إتمامه مسبقاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    dgItems.Rows.RemoveAt(e.RowIndex);
                    ReindexGrid();
                    RecalculateTotals();
                }
            };
            // ── Bottom Summary & Action Bar ──
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = Theme.BgCard,
                Padding = new Padding(12)
            };

            // Row 1: Cost summaries
            lblRawCost = new Label
            {
                Text = "📦 تكلفة المواد الخام: 0.00 ج.م",
                Location = new Point(12, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };
            pnlBottom.Controls.Add(lblRawCost);

            lblExtraCost = new Label
            {
                Text = "⚡ مصاريف التشغيل: 0.00 ج.م",
                Location = new Point(260, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.Orange
            };
            pnlBottom.Controls.Add(lblExtraCost);

            lblTotalCost = new Label
            {
                Text = "💰 إجمالي تكلفة الأمر: 0.00 ج.م",
                Location = new Point(510, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 198, 35)
            };
            pnlBottom.Controls.Add(lblTotalCost);

            lblUnitCost = new Label
            {
                Text = "🏷️ تكلفة الوحدة المصنعة: 0.00 ج.م",
                Location = new Point(810, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94)
            };
            pnlBottom.Controls.Add(lblUnitCost);

            // Row 2: Action Buttons
            btnSuspend = Theme.MakeButton("⏸️ تعليق (تحت التحضير) وخروج الخامات للمصنع", 12, 50, 310, 42, Color.FromArgb(234, 88, 12));
            btnSuspend.Click += (s, e) => SaveOrder(false);
            pnlBottom.Controls.Add(btnSuspend);

            btnComplete = Theme.MakeButton("✅ إتمام وترحيل التصنيع (إضافة للمخزن)", 335, 50, 260, 42, Color.FromArgb(22, 163, 74));
            btnComplete.Click += (s, e) => SaveOrder(true);
            pnlBottom.Controls.Add(btnComplete);

            btnCancelOrder = Theme.MakeButton("❌ إلغاء أمر التصنيع", 605, 50, 150, 42, Color.FromArgb(220, 53, 69));
            btnCancelOrder.Click += (s, e) => CancelOrder();
            pnlBottom.Controls.Add(btnCancelOrder);

            btnNew = Theme.MakeButton("➕ أمر جديد", 765, 50, 110, 42, Color.FromArgb(51, 65, 85));
            btnNew.Click += (s, e) => ResetForm();
            pnlBottom.Controls.Add(btnNew);

            btnPrint = Theme.MakeButton("🖨️ طباعة إذن التشغيل", 885, 50, 170, 42, Color.FromArgb(40, 120, 180));
            btnPrint.Click += (s, e) => PrintOrder();
            pnlBottom.Controls.Add(btnPrint);

            // ── Clean Docking ──
            this.Controls.Clear();
            this.Controls.Add(dgItems);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlBottom);
            dgItems.BringToFront();
        }

        private void LoadWarehouses()
        {
            try
            {
                var dt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses ORDER BY WarehouseID ASC");
                cboWarehouse.DataSource = dt;
                cboWarehouse.DisplayMember = "WarehouseName";
                cboWarehouse.ValueMember = "WarehouseID";
                if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmFixedProduction.LoadWarehouses", ex);
            }
        }

        private void SelectFinishedProduct(string initialSearch = "")
        {
            using (var frm = new FrmProductSearch(defaultShowZeroStock: true, initialSearchText: initialSearch))
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    LoadFinishedProduct(frm.SelectedProductID);
                }
            }
        }

        private void SelectFromRegisteredBOMs()
        {
            var dtBOMs = ProductionDAL.GetAllBOMs();
            if (dtBOMs == null || dtBOMs.Rows.Count == 0)
            {
                var ask = MessageBox.Show(
                    "لا توجد وصفات أو شجر تصنيع (BOM) مسجلة في النظام حتى الآن!\nهل تريد فتح شاشة شجرة التصنيع لإضافة وصفة ومكونات تصنيع جديدة؟",
                    "تنبيه - لا توجد وصفات مسجلة", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (ask == DialogResult.Yes)
                {
                    using (var frm = new FrmBOM())
                    {
                        frm.ShowDialog();
                    }
                }
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = "📋 اختيار من شجر ووصفات التصنيع المسجلة";
                dlg.Size = new Size(780, 480);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.BackColor = Theme.BgMain;
                dlg.Font = Theme.FontMain;

                var pnlTop = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.BgCard, Padding = new Padding(10, 8, 10, 8) };
                var lblSearch = new Label { Text = "🔍 بحث في الوصفات:", AutoSize = true, Location = new Point(10, 14), ForeColor = Theme.TextMain };
                pnlTop.Controls.Add(lblSearch);

                var txtFilter = new TextBox { Location = new Point(150, 10), Width = 300, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
                pnlTop.Controls.Add(txtFilter);

                dlg.Controls.Add(pnlTop);

                var dg = new DataGridView
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
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };

                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود المنتج", FillWeight = 20 });
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم المنتج النهائي المصنع", FillWeight = 45 });
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "OutputQty", HeaderText = "الكمية المعيارية", FillWeight = 15 });
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 10 });
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemsCount", HeaderText = "عدد الخامات", FillWeight = 12 });

                Action populate = () =>
                {
                    dg.Rows.Clear();
                    string filter = txtFilter.Text.Trim().ToLower();
                    foreach (DataRow r in dtBOMs.Rows)
                    {
                        string code = r["ProductCode"]?.ToString() ?? "";
                        string name = r["ProductName"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(filter) && !code.ToLower().Contains(filter) && !name.ToLower().Contains(filter))
                            continue;

                        dg.Rows.Add(
                            r["ProductID"],
                            code,
                            name,
                            Convert.ToDecimal(r["OutputQty"]).ToString("N2"),
                            r["UnitName"]?.ToString() ?? "قطعة",
                            r["ItemsCount"]
                        );
                    }
                };

                txtFilter.TextChanged += (s, e) => populate();
                populate();
                dlg.Controls.Add(dg);
                dg.BringToFront();

                var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(10) };
                var btnSelect = Theme.MakeButton("✅ اختيار وتحميل المكونات", 10, 10, 190, 36, Theme.Primary);
                var btnClose = Theme.MakeButton("إلغاء", 210, 10, 100, 36, Color.FromArgb(100, 116, 139));

                Action selectRow = () =>
                {
                    if (dg.SelectedRows.Count > 0 && dg.SelectedRows[0].Cells["ProductID"].Value != null)
                    {
                        int pid = Convert.ToInt32(dg.SelectedRows[0].Cells["ProductID"].Value);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadFinishedProduct(pid);
                    }
                };

                btnSelect.Click += (s, e) => selectRow();
                dg.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) selectRow(); };
                btnClose.Click += (s, e) => dlg.Close();

                pnlBottom.Controls.Add(btnSelect);
                pnlBottom.Controls.Add(btnClose);
                dlg.Controls.Add(pnlBottom);

                dlg.ShowDialog(this);
            }
        }

        private void LoadFinishedProduct(int productId)
        {
            var dt = DbHelper.Query(@"
                SELECT ProductID, ProductCode, ProductName, 
                       COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.PurchaseItemID DESC), 0) AS CostPrice, 
                       COALESCE(Unit1Name, Unit, N'قطعة') AS UnitName 
                FROM Products WHERE ProductID = @id",
                DbHelper.P("@id", productId));

            if (dt != null && dt.Rows.Count > 0)
            {
                _selectedFinishedProductID = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                _selectedFinishedProductCode = dt.Rows[0]["ProductCode"]?.ToString();
                _selectedFinishedProductName = dt.Rows[0]["ProductName"]?.ToString();
                txtFinishedProduct.Text = $"{_selectedFinishedProductCode} - {_selectedFinishedProductName}";
                txtUnitName.Text = dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة";

                // Check BOM
                _currentBOM = ProductionDAL.GetBOMByProductID(_selectedFinishedProductID);
                if (_currentBOM == null || _currentBOM.Items.Count == 0)
                {
                    var ask = MessageBox.Show(
                        "هذا الصنف ليس له شجرة ومواد تصنيع (BOM) مسجلة مسبقاً!\nهل تريد فتح شاشة تحديد مواد التصنيع لتعريف مكوناته المعيارية أولاً؟\n(أو يمكنك استخدام شاشة 'تصنيع مخصص' لإدخال المكونات يدوياً مباشرة)",
                        "تنبيه - عدم وجود وصفة تصنيع", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (ask == DialogResult.Yes)
                    {
                        using (var frmBom = new FrmBOM(_selectedFinishedProductID))
                        {
                            frmBom.ShowDialog();
                        }
                        _currentBOM = ProductionDAL.GetBOMByProductID(_selectedFinishedProductID);
                        if (_currentBOM != null && _currentBOM.Items.Count > 0)
                        {
                            PopulateGridFromBOM();
                        }
                    }
                    else
                    {
                        dgItems.Rows.Clear();
                    }
                }
                else
                {
                    PopulateGridFromBOM();
                }
            }
        }

        private void PopulateGridFromBOM()
        {
            if (_currentBOM == null) return;
            dgItems.Rows.Clear();

            decimal baseOutQty = _currentBOM.OutputQty > 0 ? _currentBOM.OutputQty : 1m;
            decimal multiplier = numProducedQty.Value / baseOutQty;

            int rowNum = 1;
            foreach (var itm in _currentBOM.Items)
            {
                decimal cost = itm.RawCostPrice;
                if (cost <= 0)
                {
                    var fallbackCostObj = DbHelper.Scalar(
                        "SELECT COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = @pid ORDER BY pi2.PurchaseItemID DESC), 0) FROM Products WHERE ProductID = @pid",
                        DbHelper.P("@pid", itm.RawProductID));
                    if (fallbackCostObj != null && fallbackCostObj != DBNull.Value)
                        cost = Convert.ToDecimal(fallbackCostObj);
                }

                decimal scaledQty = Math.Round(itm.Quantity * multiplier, 4);
                decimal totCost = scaledQty * cost;

                dgItems.Rows.Add(
                    itm.RawProductID,
                    rowNum++,
                    itm.RawProductCode,
                    itm.RawProductName,
                    scaledQty,
                    itm.UnitName,
                    cost.ToString("N2"),
                    totCost.ToString("N2"),
                    itm.Notes,
                    "❌"
                );
            }

            RecalculateTotals();
        }

        private void OnProducedQtyChanged()
        {
            if (_currentBOM != null && _currentBOM.Items.Count > 0 && dgItems.Rows.Count > 0)
            {
                decimal baseOutQty = _currentBOM.OutputQty > 0 ? _currentBOM.OutputQty : 1m;
                decimal multiplier = numProducedQty.Value / baseOutQty;

                // Adjust quantities for items that exist in BOM
                var bomDict = new Dictionary<int, decimal>();
                foreach (var itm in _currentBOM.Items) bomDict[itm.RawProductID] = itm.Quantity;

                foreach (DataGridViewRow row in dgItems.Rows)
                {
                    int pid = Convert.ToInt32(row.Cells["RawProductID"].Value);
                    if (bomDict.TryGetValue(pid, out decimal baseQty))
                    {
                        decimal newQty = Math.Round(baseQty * multiplier, 4);
                        row.Cells["Quantity"].Value = newQty;
                        UpdateRowTotal(row.Index);
                    }
                }
            }
            RecalculateTotals();
        }

        private void AddExtraRawMaterial()
        {
            if (_currentStatus == "Completed")
            {
                MessageBox.Show("لا يمكن التعديل على أمر مكتمل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var frm = new FrmProductSearch(defaultShowZeroStock: true))
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    int pid = frm.SelectedProductID;
                    if (pid == _selectedFinishedProductID)
                    {
                        MessageBox.Show("لا يمكن اختيار نفس الصنف النهائي كمادة خام لنفسه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var dt = DbHelper.Query(@"
                        SELECT ProductID, ProductCode, ProductName,                                COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.PurchaseItemID DESC), 0) AS CostPrice, 
                                COALESCE(Unit1Name, Unit, N'قطعة') AS UnitName 
                        FROM Products WHERE ProductID = @id",
                        DbHelper.P("@id", pid));

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        string code = dt.Rows[0]["ProductCode"]?.ToString();
                        string name = dt.Rows[0]["ProductName"]?.ToString();
                        decimal cost = Convert.ToDecimal(dt.Rows[0]["CostPrice"] ?? 0);
                        string unit = dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة";

                        dgItems.Rows.Add(
                            pid,
                            dgItems.Rows.Count + 1,
                            code,
                            name,
                            1m,
                            unit,
                            cost.ToString("N2"),
                            cost.ToString("N2"),
                            "مادة إضافية",
                            "❌"
                        );

                        RecalculateTotals();
                    }
                }
            }
        }

        private void UpdateRowTotal(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgItems.Rows.Count) return;
            var row = dgItems.Rows[rowIndex];
            decimal qty = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0);
            decimal cost = Convert.ToDecimal(row.Cells["UnitCost"].Value ?? 0);
            row.Cells["TotalCost"].Value = (qty * cost).ToString("N2");
        }

        private void ReindexGrid()
        {
            for (int i = 0; i < dgItems.Rows.Count; i++)
            {
                dgItems.Rows[i].Cells["RowNum"].Value = i + 1;
            }
        }

        private void RecalculateTotals()
        {
            decimal rawCost = 0;
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                decimal tot = Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0);
                rawCost += tot;
            }

            decimal extra = numExtraExpenses.Value;
            decimal totalCost = rawCost + extra;
            decimal pQty = numProducedQty.Value > 0 ? numProducedQty.Value : 1m;
            decimal unitCost = totalCost / pQty;

            lblRawCost.Text = $"📦 تكلفة المواد الخام: {rawCost:N2} ج.م";
            lblExtraCost.Text = $"⚡ مصاريف التشغيل: {extra:N2} ج.م";
            lblTotalCost.Text = $"💰 إجمالي تكلفة الأمر: {totalCost:N2} ج.م";
            lblUnitCost.Text = $"🏷️ تكلفة الوحدة المصنعة: {unitCost:N2} ج.م";
        }

        private void SaveOrder(bool complete)
        {
            if (_selectedFinishedProductID <= 0)
            {
                MessageBox.Show("يرجى اختيار المنتج النهائي المطلوب تصنيعه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgItems.Rows.Count == 0)
            {
                MessageBox.Show("لا يمكن حفظ أمر تصنيع بدون مواد خام مستهلكة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentStatus == "Completed" && !complete)
            {
                MessageBox.Show("هذا الأمر مكتمل ومرحل بالفعل، لا يمكن إعادته إلى تحت التحضير.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int wid = cboWarehouse.SelectedValue != null ? Convert.ToInt32(cboWarehouse.SelectedValue) : 1;

            var order = new ProductionOrderModel
            {
                ProductionID = _currentProductionID,
                OrderCode = _currentOrderCode,
                ProductionType = "Fixed",
                BOMID = _currentBOM != null ? (int?)_currentBOM.BOMID : null,
                FinishedProductID = _selectedFinishedProductID,
                ProducedQty = numProducedQty.Value,
                UnitName = txtUnitName.Text.Trim(),
                WarehouseID = wid,
                ExtraExpenses = numExtraExpenses.Value,
                ExpensesNotes = txtExpensesNotes.Text.Trim(),
                Notes = txtNotes.Text.Trim()
            };

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                order.Items.Add(new ProductionOrderItemModel
                {
                    RawProductID = Convert.ToInt32(row.Cells["RawProductID"].Value),
                    Quantity = Convert.ToDecimal(row.Cells["Quantity"].Value),
                    UnitCost = Convert.ToDecimal(row.Cells["UnitCost"].Value),
                    UnitName = row.Cells["UnitName"].Value?.ToString(),
                    Notes = row.Cells["Notes"].Value?.ToString()
                });
            }

            try
            {
                string actionName = Session.EmpName ?? "المستخدم";
                _currentProductionID = ProductionDAL.SaveProductionOrder(order, complete, actionName);

                if (complete)
                {
                    MessageBox.Show(
                        $"تم إتمام وترحيل التصنيع بنجاح!\n- تمت إضافة {order.ProducedQty} {order.UnitName} إلى رصيد المخزن.\n- تم تحديث سعر تكلفة المنتج الجديد إلى {order.UnitCost:N2} ج.م للوحدة.",
                        "تم الإتمام والترحيل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadExistingOrder(_currentProductionID);
                }
                else
                {
                    MessageBox.Show(
                        "تم تعليق أمر التصنيع بنجاح بحالة (تحت التحضير)!\n- تم خصم المواد الخام المستهلكة من المخزن لخروجها إلى المصنع.\n- يمكنك استرجاع الأمر في أي وقت للتعديل أو الإتمام.",
                        "تم التعليق تحت التحضير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadExistingOrder(_currentProductionID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل حفظ أمر التصنيع: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadExistingOrder(int productionId)
        {
            var order = ProductionDAL.GetProductionOrderByID(productionId);
            if (order == null) return;

            _currentProductionID = order.ProductionID;
            _currentOrderCode = order.OrderCode;
            _currentStatus = order.Status;

            lblOrderCode.Text = $"كود الأمر: {_currentOrderCode}";
            dtpOrderDate.Value = order.CreatedDate;
            if (order.WarehouseID > 0) cboWarehouse.SelectedValue = order.WarehouseID;

            _selectedFinishedProductID = order.FinishedProductID;
            _selectedFinishedProductCode = order.FinishedProductCode;
            _selectedFinishedProductName = order.FinishedProductName;
            txtFinishedProduct.Text = $"{order.FinishedProductCode} - {order.FinishedProductName}";
            numProducedQty.Value = order.ProducedQty;
            txtUnitName.Text = order.UnitName ?? "قطعة";
            txtNotes.Text = order.Notes ?? "";

            numExtraExpenses.Value = order.ExtraExpenses;
            txtExpensesNotes.Text = order.ExpensesNotes ?? "";

            UpdateStatusBadge(order.Status);

            dgItems.Rows.Clear();
            int rNum = 1;
            foreach (var itm in order.Items)
            {
                dgItems.Rows.Add(
                    itm.RawProductID,
                    rNum++,
                    itm.RawProductCode,
                    itm.RawProductName,
                    itm.Quantity,
                    itm.UnitName,
                    itm.UnitCost.ToString("N2"),
                    itm.TotalCost.ToString("N2"),
                    itm.Notes,
                    "❌"
                );
            }

            RecalculateTotals();

            // Disable edits if completed or cancelled
            bool isReadOnly = (order.Status == "Completed" || order.Status == "Cancelled");
            btnSuspend.Enabled = !isReadOnly;
            btnComplete.Enabled = !isReadOnly;
            btnCancelOrder.Enabled = !isReadOnly;
            btnBrowseFinished.Enabled = !isReadOnly;
            btnAddExtraRaw.Enabled = !isReadOnly;
        }

        private void UpdateStatusBadge(string status)
        {
            switch (status)
            {
                case "InPreparation":
                    lblStatusBadge.Text = "⏳ تحت التحضير (المواد مخصومة بالمصنع)";
                    lblStatusBadge.BackColor = Color.FromArgb(234, 88, 12);
                    break;
                case "Completed":
                    lblStatusBadge.Text = "✅ مكتمل ومرحل (المنتج بالمخزن)";
                    lblStatusBadge.BackColor = Color.FromArgb(22, 163, 74);
                    break;
                case "Cancelled":
                    lblStatusBadge.Text = "❌ أمر تصنيع ملغي";
                    lblStatusBadge.BackColor = Color.FromArgb(220, 53, 69);
                    break;
                default:
                    lblStatusBadge.Text = status;
                    lblStatusBadge.BackColor = Color.Gray;
                    break;
            }
        }

        private void ShowSuspendedOrdersDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "📋 استرجاع أوامر التصنيع المعلقة (تحت التحضير)";
                dlg.Size = new Size(880, 480);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain;
                dlg.Font = Theme.FontMain;

                var dgSuspended = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = Theme.BgCard,
                    BorderStyle = BorderStyle.None,
                    RowHeadersVisible = false,
                    AllowUserToAddRows = false,
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };

                var dt = ProductionDAL.GetSuspendedOrders("Fixed");
                dgSuspended.DataSource = dt;

                if (dgSuspended.Columns["ProductionID"] != null) dgSuspended.Columns["ProductionID"].Visible = false;
                if (dgSuspended.Columns["ProductionType"] != null) dgSuspended.Columns["ProductionType"].Visible = false;
                if (dgSuspended.Columns["ProductionTypeName"] != null) dgSuspended.Columns["ProductionTypeName"].HeaderText = "النوع";
                if (dgSuspended.Columns["OrderCode"] != null) dgSuspended.Columns["OrderCode"].HeaderText = "كود الأمر";
                if (dgSuspended.Columns["ProductCode"] != null) dgSuspended.Columns["ProductCode"].HeaderText = "كود المنتج";
                if (dgSuspended.Columns["ProductName"] != null) dgSuspended.Columns["ProductName"].HeaderText = "المنتج المصنع";
                if (dgSuspended.Columns["ProducedQty"] != null) dgSuspended.Columns["ProducedQty"].HeaderText = "الكمية";
                if (dgSuspended.Columns["UnitName"] != null) dgSuspended.Columns["UnitName"].HeaderText = "الوحدة";
                if (dgSuspended.Columns["TotalCost"] != null) dgSuspended.Columns["TotalCost"].HeaderText = "إجمالي التكلفة";
                if (dgSuspended.Columns["UnitCost"] != null) dgSuspended.Columns["UnitCost"].HeaderText = "تكلفة الوحدة";
                if (dgSuspended.Columns["UpdatedDate"] != null) dgSuspended.Columns["UpdatedDate"].HeaderText = "آخر تعديل";

                var pnlDlgBottom = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(10) };
                var btnOpen = Theme.MakeButton("📂 استرجاع الأمر المختار", 15, 10, 180, 36, Theme.Primary);
                btnOpen.Click += (s, e) =>
                {
                    if (dgSuspended.CurrentRow != null)
                    {
                        int pid = Convert.ToInt32(dgSuspended.CurrentRow.Cells["ProductionID"].Value);
                        dlg.DialogResult = DialogResult.OK;
                        LoadExistingOrder(pid);
                    }
                };
                pnlDlgBottom.Controls.Add(btnOpen);

                dlg.Controls.Add(dgSuspended);
                dlg.Controls.Add(pnlDlgBottom);

                dgSuspended.CellDoubleClick += (s, e) =>
                {
                    if (e.RowIndex >= 0)
                    {
                        int pid = Convert.ToInt32(dgSuspended.Rows[e.RowIndex].Cells["ProductionID"].Value);
                        dlg.DialogResult = DialogResult.OK;
                        LoadExistingOrder(pid);
                    }
                };

                dlg.ShowDialog();
            }
        }

        private void CancelOrder()
        {
            if (_currentProductionID <= 0) return;
            if (_currentStatus == "Completed")
            {
                MessageBox.Show("لا يمكن إلغاء أمر تم إتمامه وترحيله بالفعل للمخزن!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var res = MessageBox.Show(
                "هل أنت متأكد من رغبتك في إلغاء هذا الأمر؟\nسيتم إرجاع كافة المواد الخام المستهلكة إلى المخزن فوراً.",
                "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                if (ProductionDAL.CancelProductionOrder(_currentProductionID, Session.EmpName, "إلغاء بواسطة المستخدم"))
                {
                    MessageBox.Show("تم إلغاء أمر التصنيع واسترجاع المواد الخام للمخزن بنجاح.", "تم الإلغاء", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadExistingOrder(_currentProductionID);
                }
            }
        }

        private void ResetForm()
        {
            _currentProductionID = 0;
            _currentOrderCode = ProductionDAL.GenerateOrderCode("PRD");
            _currentStatus = "Draft";
            _currentBOM = null;

            _selectedFinishedProductID = 0;
            _selectedFinishedProductCode = "";
            _selectedFinishedProductName = "";

            lblOrderCode.Text = $"كود الأمر: {_currentOrderCode}";
            lblStatusBadge.Text = "⏳ مسودة جديدة";
            lblStatusBadge.BackColor = Color.FromArgb(100, 116, 139);
            dtpOrderDate.Value = DateTime.Now;

            txtFinishedProduct.Clear();
            numProducedQty.Value = 1m;
            txtUnitName.Text = "قطعة";
            txtNotes.Clear();
            numExtraExpenses.Value = 0m;
            txtExpensesNotes.Clear();

            dgItems.Rows.Clear();
            RecalculateTotals();

            btnSuspend.Enabled = true;
            btnComplete.Enabled = true;
            btnCancelOrder.Enabled = false;
            btnBrowseFinished.Enabled = true;
            btnAddExtraRaw.Enabled = true;
        }

        private void PrintOrder()
        {
            if (dgItems.Rows.Count == 0 || _selectedFinishedProductID <= 0)
            {
                MessageBox.Show("لا توجد بيانات أمر تصنيع للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pd = new PrintDocument();
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                float y = 40;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 12f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 10f);
                var fontBold = new Font("Segoe UI", 10f, FontStyle.Bold);

                g.DrawString("إذن تشغيل وتصنيع داخلي (Manufacturing Order)", fontTitle, Brushes.DarkBlue, new PointF(180, y));
                y += 40;

                g.DrawString($"كود الأمر: {_currentOrderCode} | التاريخ: {dtpOrderDate.Value:yyyy-MM-dd} | الحالة: {lblStatusBadge.Text}", fontHeader, Brushes.Black, new PointF(40, y));
                y += 25;
                g.DrawString($"المنتج النهائي: {_selectedFinishedProductCode} - {_selectedFinishedProductName} | الكمية المنتجة: {numProducedQty.Value} {txtUnitName.Text.Trim()}", fontHeader, Brushes.DarkSlateGray, new PointF(40, y));
                y += 25;
                g.DrawString($"المخزن: {cboWarehouse.Text} | مصاريف التشغيل: {numExtraExpenses.Value:N2} ج.م ({txtExpensesNotes.Text.Trim()})", fontBody, Brushes.Black, new PointF(40, y));
                y += 35;

                // Table Header
                g.FillRectangle(Brushes.LightGray, 40, y, 740, 26);
                g.DrawRectangle(Pens.Gray, 40, y, 740, 26);
                g.DrawString("م", fontBold, Brushes.Black, 50, y + 4);
                g.DrawString("كود الخام", fontBold, Brushes.Black, 90, y + 4);
                g.DrawString("اسم المادة الخام", fontBold, Brushes.Black, 220, y + 4);
                g.DrawString("الكمية المستهلكة", fontBold, Brushes.Black, 450, y + 4);
                g.DrawString("الوحدة", fontBold, Brushes.Black, 550, y + 4);
                g.DrawString("سعر التكلفة", fontBold, Brushes.Black, 610, y + 4);
                g.DrawString("الإجمالي", fontBold, Brushes.Black, 680, y + 4);
                y += 26;

                int num = 1;
                decimal rawTot = 0;
                foreach (DataGridViewRow row in dgItems.Rows)
                {
                    g.DrawRectangle(Pens.LightGray, 40, y, 740, 24);
                    g.DrawString(num++.ToString(), fontBody, Brushes.Black, 50, y + 3);
                    g.DrawString(row.Cells["RawProductCode"].Value?.ToString() ?? "", fontBody, Brushes.Black, 90, y + 3);
                    g.DrawString(row.Cells["RawProductName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 220, y + 3);
                    g.DrawString(row.Cells["Quantity"].Value?.ToString() ?? "", fontBody, Brushes.Black, 450, y + 3);
                    g.DrawString(row.Cells["UnitName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 550, y + 3);
                    g.DrawString(row.Cells["UnitCost"].Value?.ToString() ?? "", fontBody, Brushes.Black, 610, y + 3);
                    g.DrawString(row.Cells["TotalCost"].Value?.ToString() ?? "", fontBody, Brushes.Black, 680, y + 3);

                    rawTot += Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0);
                    y += 24;
                }

                y += 15;
                decimal grandTot = rawTot + numExtraExpenses.Value;
                decimal uCost = numProducedQty.Value > 0 ? grandTot / numProducedQty.Value : grandTot;

                g.DrawString($"إجمالي تكلفة المواد الخام: {rawTot:N2} ج.م", fontBold, Brushes.Black, 450, y);
                y += 20;
                g.DrawString($"مصاريف التشغيل الإضافية: {numExtraExpenses.Value:N2} ج.م", fontBold, Brushes.Black, 450, y);
                y += 20;
                g.DrawString($"إجمالي تكلفة أمر الإنتاج: {grandTot:N2} ج.م", fontHeader, Brushes.Black, 450, y);
                y += 25;
                g.DrawString($"تكلفة الوحدة الواحدة المصنعة: {uCost:N2} ج.م", fontHeader, Brushes.DarkGreen, 450, y);

                y += 40;
                g.DrawString("توقيع مسؤول الإنتاج: ..............................", fontBody, Brushes.Black, 60, y);
                g.DrawString("توقيع أمين المخزن: ..............................", fontBody, Brushes.Black, 460, y);
            };

            using (var ppd = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700 })
            {
                ppd.ShowDialog();
            }
        }
    }
}

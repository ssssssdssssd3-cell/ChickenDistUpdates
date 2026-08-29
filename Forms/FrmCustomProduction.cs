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
    /// شاشة التصنيع المخصص (إدخال حر للمواد الخام والمصروفات دون اشتراط وصفة مسبقة مع التعليق تحت التحضير)
    /// </summary>
    public class FrmCustomProduction : Form
    {
        private int _currentProductionID = 0;
        private string _currentOrderCode = "";
        private string _currentStatus = "InPreparation";

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
        private NumericUpDown numProducedQty;
        private TextBox txtUnitName;
        private TextBox txtNotes;

        // Controls - Quick Add Raw Material Bar
        private int _selectedRawProductID = 0;
        private string _selectedRawProductCode = "";
        private string _selectedRawProductName = "";
        private decimal _selectedRawCostPrice = 0;
        private TextBox txtRawProduct;
        private Button btnBrowseRaw;
        private NumericUpDown numRawQty;
        private TextBox txtRawUnit;
        private Label lblRawCost;
        private Button btnAddRaw;

        // Controls - Expenses
        private NumericUpDown numExtraExpenses;
        private TextBox txtExpensesNotes;

        // Grid
        private DataGridView dgItems;

        // Cost Summary Cards
        private Label lblRawCostSummary;
        private Label lblExtraCostSummary;
        private Label lblTotalCostSummary;
        private Label lblUnitCostSummary;

        // Action Buttons
        private Button btnSuspend;
        private Button btnComplete;
        private Button btnResume;
        private Button btnCancelOrder;
        private Button btnNew;
        private Button btnPrint;

        public FrmCustomProduction(int productionId = 0)
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
            this.Text = "🛠️ أمر تصنيع مخصص (إدخال حر مباشر للمكونات والمصروفات)";
            this.Size = new Size(1180, 750);
            this.MinimumSize = new Size(1020, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
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

            // Row 1: Title, OrderCode, Status Badge, Date, Warehouse, Resume
            var lblTitle = new Label
            {
                Text = "🛠️ أمر تصنيع مخصص",
                Location = new Point(12, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(168, 85, 247)
            };
            pnlHeader.Controls.Add(lblTitle);

            lblOrderCode = new Label
            {
                Text = "كود الأمر: CPRD-...",
                Location = new Point(200, 14),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(243, 198, 35)
            };
            pnlHeader.Controls.Add(lblOrderCode);

            lblStatusBadge = new Label
            {
                Text = "⏳ مسودة جديدة",
                Location = new Point(400, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(100, 116, 139),
                Padding = new Padding(8, 4, 8, 4)
            };
            pnlHeader.Controls.Add(lblStatusBadge);

            var lblDate = new Label { Text = "التاريخ:", Location = new Point(580, 14), AutoSize = true };
            pnlHeader.Controls.Add(lblDate);

            dtpOrderDate = new DateTimePicker
            {
                Location = new Point(630, 11),
                Width = 140,
                Format = DateTimePickerFormat.Short,
                Font = Theme.FontMain
            };
            pnlHeader.Controls.Add(dtpOrderDate);

            var lblWh = new Label { Text = "المخزن:", Location = new Point(785, 14), AutoSize = true };
            pnlHeader.Controls.Add(lblWh);

            cboWarehouse = new ComboBox
            {
                Location = new Point(840, 11),
                Width = 175,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlHeader.Controls.Add(cboWarehouse);

            btnResume = Theme.MakeButton("🔄 استرجاع أمر معلق", 1030, 8, 130, 34, Color.FromArgb(51, 65, 85));
            btnResume.Click += (s, e) => ShowSuspendedOrdersDialog();
            pnlHeader.Controls.Add(btnResume);

            // Row 2: Finished Product selection, Produced Quantity, Unit, Notes
            var lblFpTitle = new Label
            {
                Text = "🎯 المنتج النهائي المصنع (في الأعلى):",
                Location = new Point(12, 55),
                AutoSize = true,
                ForeColor = Color.Silver
            };
            pnlHeader.Controls.Add(lblFpTitle);

            txtFinishedProduct = new TextBox
            {
                Location = new Point(12, 78),
                Width = 320,
                Height = 32,
                ReadOnly = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            pnlHeader.Controls.Add(txtFinishedProduct);

            btnBrowseFinished = Theme.MakeButton("🔍 اختيار منتج", 340, 76, 120, 34, Theme.Primary);
            btnBrowseFinished.Click += (s, e) => SelectFinishedProduct();
            pnlHeader.Controls.Add(btnBrowseFinished);

            var lblQtyTitle = new Label { Text = "الكمية المنتجة الناتجة:", Location = new Point(480, 55), AutoSize = true, ForeColor = Color.Silver };
            pnlHeader.Controls.Add(lblQtyTitle);

            numProducedQty = new NumericUpDown
            {
                Location = new Point(480, 78),
                Width = 120,
                Height = 32,
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 1000000m,
                Value = 1m,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            numProducedQty.ValueChanged += (s, e) => RecalculateTotals();
            pnlHeader.Controls.Add(numProducedQty);

            var lblUnit = new Label { Text = "الوحدة:", Location = new Point(615, 55), AutoSize = true, ForeColor = Color.Silver };
            pnlHeader.Controls.Add(lblUnit);

            txtUnitName = new TextBox
            {
                Location = new Point(615, 78),
                Width = 80,
                Height = 32,
                Text = "قطعة",
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlHeader.Controls.Add(txtUnitName);

            var lblNotesTitle = new Label { Text = "ملاحظات وبيان التشغيل:", Location = new Point(710, 55), AutoSize = true, ForeColor = Color.Silver };
            pnlHeader.Controls.Add(lblNotesTitle);

            txtNotes = new TextBox
            {
                Location = new Point(710, 78),
                Width = 440,
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

            var lblExpNotes = new Label { Text = "بيان وتفاصيل المصروفات:", Location = new Point(445, 122), AutoSize = true, ForeColor = Color.Silver };
            pnlHeader.Controls.Add(lblExpNotes);

            txtExpensesNotes = new TextBox
            {
                Location = new Point(595, 120),
                Width = 555,
                Height = 28,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlHeader.Controls.Add(txtExpensesNotes);

            // ── Quick Add Raw Material Bar ──
            var pnlQuickAdd = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10)
            };
            this.Controls.Add(pnlQuickAdd);
            pnlQuickAdd.BringToFront();

            var lblAddRawTitle = new Label { Text = "📦 مادة التصنيع المراد خصمها:", Location = new Point(12, 8), AutoSize = true, ForeColor = Color.WhiteSmoke };
            pnlQuickAdd.Controls.Add(lblAddRawTitle);

            txtRawProduct = new TextBox
            {
                Location = new Point(12, 28),
                Width = 270,
                Height = 30,
                ReadOnly = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlQuickAdd.Controls.Add(txtRawProduct);

            btnBrowseRaw = Theme.MakeButton("🔍 بحث أصناف", 290, 26, 110, 32, Color.FromArgb(51, 65, 85));
            btnBrowseRaw.Click += (s, e) => SelectRawProduct();
            pnlQuickAdd.Controls.Add(btnBrowseRaw);

            var lblRawQtyTitle = new Label { Text = "الكمية:", Location = new Point(415, 8), AutoSize = true, ForeColor = Color.WhiteSmoke };
            pnlQuickAdd.Controls.Add(lblRawQtyTitle);

            numRawQty = new NumericUpDown
            {
                Location = new Point(415, 28),
                Width = 90,
                Height = 30,
                DecimalPlaces = 3,
                Minimum = 0.001m,
                Maximum = 1000000m,
                Value = 1m,
                Font = Theme.FontBold,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlQuickAdd.Controls.Add(numRawQty);

            var lblRawUnitTitle = new Label { Text = "الوحدة:", Location = new Point(515, 8), AutoSize = true, ForeColor = Color.WhiteSmoke };
            pnlQuickAdd.Controls.Add(lblRawUnitTitle);

            txtRawUnit = new TextBox
            {
                Location = new Point(515, 28),
                Width = 80,
                Height = 30,
                Text = "قطعة",
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlQuickAdd.Controls.Add(txtRawUnit);

            lblRawCost = new Label
            {
                Text = "سعر التكلفة: 0.00 ج.م",
                Location = new Point(605, 30),
                Width = 160,
                ForeColor = Color.FromArgb(243, 198, 35),
                Font = Theme.FontBold
            };
            pnlQuickAdd.Controls.Add(lblRawCost);

            btnAddRaw = Theme.MakeButton("➕ إضافة لقائمة الخصم", 780, 24, 160, 34, Color.FromArgb(34, 197, 94));
            btnAddRaw.Click += (s, e) => AddCurrentRawToGrid();
            pnlQuickAdd.Controls.Add(btnAddRaw);

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
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductCode", HeaderText = "كود الصنف المستهلك", FillWeight = 18, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductName", HeaderText = "اسم الصنف المراد خصمه", FillWeight = 38, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المخصومة", FillWeight = 16 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 12 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "سعر التكلفة", FillWeight = 15, ReadOnly = true });
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
                if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "Quantity")
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
            this.Controls.Add(dgItems);
            dgItems.BringToFront();

            // ── Bottom Summary & Action Bar ──
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = Theme.BgCard,
                Padding = new Padding(12)
            };
            this.Controls.Add(pnlBottom);

            lblRawCostSummary = new Label
            {
                Text = "📦 تكلفة المواد المستهلكة: 0.00 ج.م",
                Location = new Point(12, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };
            pnlBottom.Controls.Add(lblRawCostSummary);

            lblExtraCostSummary = new Label
            {
                Text = "⚡ مصاريف التشغيل: 0.00 ج.م",
                Location = new Point(270, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.Orange
            };
            pnlBottom.Controls.Add(lblExtraCostSummary);

            lblTotalCostSummary = new Label
            {
                Text = "💰 إجمالي تكلفة الأمر: 0.00 ج.م",
                Location = new Point(520, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 198, 35)
            };
            pnlBottom.Controls.Add(lblTotalCostSummary);

            lblUnitCostSummary = new Label
            {
                Text = "🏷️ تكلفة الوحدة المصنعة: 0.00 ج.م",
                Location = new Point(815, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94)
            };
            pnlBottom.Controls.Add(lblUnitCostSummary);

            // Action Buttons
            btnSuspend = Theme.MakeButton("⏸️ تعليق (تحت التحضير) وخصم المواد من المخزن", 12, 50, 310, 42, Color.FromArgb(234, 88, 12));
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
                AppLogger.Error("FrmCustomProduction.LoadWarehouses", ex);
            }
        }

        private void SelectFinishedProduct()
        {
            using (var frm = new FrmProductSearch(defaultShowZeroStock: true))
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    var dt = DbHelper.Query(@"
                        SELECT ProductID, ProductCode, ProductName, 
                               COALESCE(CostPrice, PurchasePrice, 0) AS CostPrice, 
                               COALESCE(Unit1Name, Unit, N'قطعة') AS UnitName 
                        FROM Products WHERE ProductID = @id",
                        DbHelper.P("@id", frm.SelectedProductID));

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        _selectedFinishedProductID = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                        _selectedFinishedProductCode = dt.Rows[0]["ProductCode"]?.ToString();
                        _selectedFinishedProductName = dt.Rows[0]["ProductName"]?.ToString();
                        txtFinishedProduct.Text = $"{_selectedFinishedProductCode} - {_selectedFinishedProductName}";
                        txtUnitName.Text = dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة";
                        RecalculateTotals();
                    }
                }
            }
        }

        private void SelectRawProduct()
        {
            using (var frm = new FrmProductSearch(defaultShowZeroStock: true))
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    if (frm.SelectedProductID == _selectedFinishedProductID)
                    {
                        MessageBox.Show("لا يمكن اختيار نفس الصنف النهائي كمادة خام مستهلكة لنفسه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var dt = DbHelper.Query(@"
                        SELECT ProductID, ProductCode, ProductName, 
                               COALESCE(CostPrice, PurchasePrice, 0) AS CostPrice, 
                               COALESCE(Unit1Name, Unit, N'قطعة') AS UnitName 
                        FROM Products WHERE ProductID = @id",
                        DbHelper.P("@id", frm.SelectedProductID));

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        _selectedRawProductID = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                        _selectedRawProductCode = dt.Rows[0]["ProductCode"]?.ToString();
                        _selectedRawProductName = dt.Rows[0]["ProductName"]?.ToString();
                        _selectedRawCostPrice = Convert.ToDecimal(dt.Rows[0]["CostPrice"] ?? 0);

                        txtRawProduct.Text = $"{_selectedRawProductCode} - {_selectedRawProductName}";
                        txtRawUnit.Text = dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة";
                        lblRawCost.Text = $"سعر التكلفة: {_selectedRawCostPrice:N2} ج.م";
                        numRawQty.Focus();
                    }
                }
            }
        }

        private void AddCurrentRawToGrid()
        {
            if (_selectedRawProductID <= 0)
            {
                MessageBox.Show("يرجى اختيار مادة تصنيع أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal qty = numRawQty.Value;
            if (qty <= 0)
            {
                MessageBox.Show("الكمية يجب أن تكون أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (Convert.ToInt32(row.Cells["RawProductID"].Value) == _selectedRawProductID)
                {
                    decimal cur = Convert.ToDecimal(row.Cells["Quantity"].Value);
                    row.Cells["Quantity"].Value = cur + qty;
                    UpdateRowTotal(row.Index);
                    RecalculateTotals();
                    ClearRawInputs();
                    return;
                }
            }

            decimal tot = qty * _selectedRawCostPrice;
            dgItems.Rows.Add(
                _selectedRawProductID,
                dgItems.Rows.Count + 1,
                _selectedRawProductCode,
                _selectedRawProductName,
                qty,
                txtRawUnit.Text.Trim(),
                _selectedRawCostPrice.ToString("N2"),
                tot.ToString("N2"),
                "",
                "❌"
            );

            ClearRawInputs();
            RecalculateTotals();
        }

        private void ClearRawInputs()
        {
            _selectedRawProductID = 0;
            _selectedRawProductCode = "";
            _selectedRawProductName = "";
            _selectedRawCostPrice = 0;
            txtRawProduct.Clear();
            numRawQty.Value = 1m;
            lblRawCost.Text = "سعر التكلفة: 0.00 ج.م";
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

            lblRawCostSummary.Text = $"📦 تكلفة المواد المستهلكة: {rawCost:N2} ج.م";
            lblExtraCostSummary.Text = $"⚡ مصاريف التشغيل: {extra:N2} ج.م";
            lblTotalCostSummary.Text = $"💰 إجمالي تكلفة الأمر: {totalCost:N2} ج.م";
            lblUnitCostSummary.Text = $"🏷️ تكلفة الوحدة المصنعة: {unitCost:N2} ج.م";
        }

        private void SaveOrder(bool complete)
        {
            if (_selectedFinishedProductID <= 0)
            {
                MessageBox.Show("يرجى اختيار المنتج النهائي في الأعلى.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgItems.Rows.Count == 0)
            {
                MessageBox.Show("لا يمكن حفظ أمر تصنيع مخصص بدون إضافة مواد تصنيع مستهلكة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                ProductionType = "Custom",
                BOMID = null,
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
                        $"تم إتمام وترحيل التصنيع المخصص بنجاح!\n- تمت إضافة {order.ProducedQty} {order.UnitName} إلى رصيد المخزن.\n- تم تحديث سعر تكلفة المنتج الجديد إلى {order.UnitCost:N2} ج.م للوحدة.",
                        "تم الإتمام والترحيل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadExistingOrder(_currentProductionID);
                }
                else
                {
                    MessageBox.Show(
                        "تم تعليق أمر التصنيع المخصص بنجاح بحالة (تحت التحضير)!\n- تم خصم المواد المستهلكة من رصيد المخزن.\n- يمكنك استرجاع الأمر في أي وقت للتعديل أو الإتمام.",
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

            bool isReadOnly = (order.Status == "Completed" || order.Status == "Cancelled");
            btnSuspend.Enabled = !isReadOnly;
            btnComplete.Enabled = !isReadOnly;
            btnCancelOrder.Enabled = !isReadOnly;
            btnBrowseFinished.Enabled = !isReadOnly;
            btnBrowseRaw.Enabled = !isReadOnly;
            btnAddRaw.Enabled = !isReadOnly;
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
                dlg.Text = "📋 استرجاع أوامر التصنيع المخصصة المعلقة (تحت التحضير)";
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

                var dt = ProductionDAL.GetSuspendedOrders("Custom");
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
                "هل أنت متأكد من رغبتك في إلغاء أمر التصنيع هذا؟\nسيتم إرجاع كافة المواد المستهلكة إلى المخزن فوراً.",
                "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                if (ProductionDAL.CancelProductionOrder(_currentProductionID, Session.EmpName, "إلغاء بواسطة المستخدم"))
                {
                    MessageBox.Show("تم إلغاء أمر التصنيع واسترجاع المواد للمخزن بنجاح.", "تم الإلغاء", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadExistingOrder(_currentProductionID);
                }
            }
        }

        private void ResetForm()
        {
            _currentProductionID = 0;
            _currentOrderCode = ProductionDAL.GenerateOrderCode("CPRD");
            _currentStatus = "Draft";

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

            ClearRawInputs();
            dgItems.Rows.Clear();
            RecalculateTotals();

            btnSuspend.Enabled = true;
            btnComplete.Enabled = true;
            btnCancelOrder.Enabled = false;
            btnBrowseFinished.Enabled = true;
            btnBrowseRaw.Enabled = true;
            btnAddRaw.Enabled = true;
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

                g.DrawString("إذن تصنيع مخصص (Custom Manufacturing Order)", fontTitle, Brushes.Purple, new PointF(160, y));
                y += 40;

                g.DrawString($"كود الأمر: {_currentOrderCode} | التاريخ: {dtpOrderDate.Value:yyyy-MM-dd} | الحالة: {lblStatusBadge.Text}", fontHeader, Brushes.Black, new PointF(40, y));
                y += 25;
                g.DrawString($"المنتج النهائي: {_selectedFinishedProductCode} - {_selectedFinishedProductName} | الكمية المنتجة: {numProducedQty.Value} {txtUnitName.Text.Trim()}", fontHeader, Brushes.DarkSlateGray, new PointF(40, y));
                y += 25;
                g.DrawString($"المخزن: {cboWarehouse.Text} | مصاريف التشغيل: {numExtraExpenses.Value:N2} ج.م ({txtExpensesNotes.Text.Trim()})", fontBody, Brushes.Black, new PointF(40, y));
                y += 35;

                g.FillRectangle(Brushes.LightGray, 40, y, 740, 26);
                g.DrawRectangle(Pens.Gray, 40, y, 740, 26);
                g.DrawString("م", fontBold, Brushes.Black, 50, y + 4);
                g.DrawString("كود الصنف", fontBold, Brushes.Black, 90, y + 4);
                g.DrawString("اسم الصنف المستهلك", fontBold, Brushes.Black, 220, y + 4);
                g.DrawString("الكمية المخصومة", fontBold, Brushes.Black, 450, y + 4);
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

                g.DrawString($"إجمالي تكلفة المواد المستهلكة: {rawTot:N2} ج.م", fontBold, Brushes.Black, 450, y);
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

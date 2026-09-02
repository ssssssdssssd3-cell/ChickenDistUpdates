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
        private Label lblEstimatedDuration;

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
            this.Size = new Size(1260, 780);
            this.MinimumSize = new Size(1080, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false;
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // ══════════════════════════════════════════════════════════════
            // الحاوية الرئيسية للهيكل العام (5 صفوف منظمة ومستقلة تماماً)
            // ══════════════════════════════════════════════════════════════
            var tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(8, 6, 8, 6)
            };
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));  // Row 0: شريط الترويسة وكود الأمر
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 108f)); // Row 1: بطاقة المنتج النهائي والكميات
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));  // Row 2: شريط مصاريف التشغيل والإضافات
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 3: جدول المواد الخام المستهلكة
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 66f));  // Row 4: شريط العمليات والملخص المالي السفلي
            this.Controls.Add(tblMain);

            // ──────────────────────────────────────────────────────────────
            // [صف 0]: شريط الترويسة الرئيسي (كود الأمر، الحالة، المخزن، التاريخ)
            // ──────────────────────────────────────────────────────────────
            var pnlHeaderTop = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 0, 0, 6)
            };
            pnlHeaderTop.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlHeaderTop.Width - 1, pnlHeaderTop.Height - 1);
                }
            };

            var pnlHeaderRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "🏭 أمر تصنيع ثابت",
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Margin = new Padding(0, 4, 12, 0)
            };
            pnlHeaderRight.Controls.Add(lblTitle);

            lblOrderCode = new Label
            {
                Text = "كود الأمر: PRD-...",
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 2, 8, 0)
            };
            pnlHeaderRight.Controls.Add(lblOrderCode);

            lblStatusBadge = new Label
            {
                Text = "⏳ مسودة جديدة",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(100, 116, 139),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 2, 8, 0)
            };
            pnlHeaderRight.Controls.Add(lblStatusBadge);
            pnlHeaderTop.Controls.Add(pnlHeaderRight);

            var pnlHeaderLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnResume = Theme.MakeButton("🔄 استرجاع أمر معلق", 0, 0, 150, 32, Color.FromArgb(51, 65, 85));
            btnResume.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnResume.Margin = new Padding(0, 2, 12, 0);
            btnResume.Click += (s, e) => ShowSuspendedOrdersDialog();
            pnlHeaderLeft.Controls.Add(btnResume);

            cboWarehouse = new ComboBox
            {
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Margin = new Padding(0, 4, 8, 0)
            };
            pnlHeaderLeft.Controls.Add(cboWarehouse);

            var lblWh = new Label
            {
                Text = "المخزن:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Margin = new Padding(0, 7, 12, 0)
            };
            pnlHeaderLeft.Controls.Add(lblWh);

            dtpOrderDate = new DateTimePicker
            {
                Width = 115,
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Margin = new Padding(0, 4, 8, 0)
            };
            pnlHeaderLeft.Controls.Add(dtpOrderDate);

            var lblDate = new Label
            {
                Text = "التاريخ:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Margin = new Padding(0, 7, 0, 0)
            };
            pnlHeaderLeft.Controls.Add(lblDate);

            pnlHeaderTop.Controls.Add(pnlHeaderLeft);
            tblMain.Controls.Add(pnlHeaderTop, 0, 0);

            // ──────────────────────────────────────────────────────────────
            // [صف 1]: بطاقة المنتج النهائي المطلوب تصنيعه والكميات
            // ──────────────────────────────────────────────────────────────
            var pnlProductCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12, 6, 12, 6),
                Margin = new Padding(0, 0, 0, 6)
            };
            pnlProductCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlProductCard.Width - 1, pnlProductCard.Height - 1);
                }
            };

            var pnlFpHeader = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Color.Transparent };
            var lblFpTitle = new Label
            {
                Text = "🎯 بيانات المنتج النهائي المراد تصنيعه:",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            pnlFpHeader.Controls.Add(lblFpTitle);

            lblEstimatedDuration = new Label
            {
                Text = "⏱️ مدة التصنيع: غير محددة",
                Dock = DockStyle.Left,
                AutoSize = true,
                ForeColor = Color.FromArgb(16, 185, 129),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            pnlFpHeader.Controls.Add(lblEstimatedDuration);
            pnlProductCard.Controls.Add(pnlFpHeader);

            var tblFp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f)); // 0: الصنف النهائي + بحث + وصفات
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f)); // 1: الكمية المراد إنتاجها
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f)); // 2: الوحدة
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f)); // 3: ملاحظات أمر التشغيل
            tblFp.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));     // Row 0: Labels
            tblFp.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));     // Row 1: Inputs

            // Labels
            tblFp.Controls.Add(new Label { Text = "المنتج النهائي (كود/اسم/بحث):", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomRight }, 0, 0);
            tblFp.Controls.Add(new Label { Text = "الكمية المراد إنتاجها:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 1, 0);
            tblFp.Controls.Add(new Label { Text = "الوحدة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 2, 0);
            tblFp.Controls.Add(new Label { Text = "ملاحظات أمر التشغيل:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomRight }, 3, 0);

            // Controls
            // 0: المنتج النهائي + الأزرار
            var pnlFpInputs = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 6, 0) };
            txtFinishedProduct = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
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
            txtFinishedProduct.Leave += (s, e) =>
            {
                string q = txtFinishedProduct.Text.Trim();
                if (!string.IsNullOrEmpty(q) && _selectedFinishedProductID <= 0)
                {
                    var dt = DbHelper.Query("SELECT TOP 1 ProductID FROM Products WHERE ProductCode = @q OR Unit1Barcode = @q OR Unit2Barcode = @q OR ProductName LIKE '%' + @q + '%'", DbHelper.P("@q", q));
                    if (dt != null && dt.Rows.Count > 0) LoadFinishedProduct(Convert.ToInt32(dt.Rows[0]["ProductID"]));
                }
            };

            btnBrowseBOM = Theme.MakeButton("📋 الوصفات", 0, 0, 85, 30, Color.FromArgb(14, 116, 144));
            btnBrowseBOM.Dock = DockStyle.Left;
            btnBrowseBOM.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnBrowseBOM.Click += (s, e) => SelectFromRegisteredBOMs();

            btnBrowseFinished = Theme.MakeButton("🔍 بحث", 0, 0, 68, 30, Theme.Primary);
            btnBrowseFinished.Dock = DockStyle.Left;
            btnBrowseFinished.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnBrowseFinished.Margin = new Padding(0, 0, 4, 0);
            btnBrowseFinished.Click += (s, e) => SelectFinishedProduct();

            pnlFpInputs.Controls.Add(txtFinishedProduct);
            pnlFpInputs.Controls.Add(btnBrowseFinished);
            pnlFpInputs.Controls.Add(btnBrowseBOM);
            tblFp.Controls.Add(pnlFpInputs, 0, 1);

            // 1: الكمية
            numProducedQty = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 1000000m,
                Value = 1m,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(2, 132, 199),
                TextAlign = HorizontalAlignment.Center
            };
            numProducedQty.ValueChanged += (s, e) => OnProducedQtyChanged();
            tblFp.Controls.Add(numProducedQty, 1, 1);

            // 2: الوحدة
            txtUnitName = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                Text = "قطعة",
                ReadOnly = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
            };
            tblFp.Controls.Add(txtUnitName, 2, 1);

            // 3: الملاحظات
            txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 0),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            tblFp.Controls.Add(txtNotes, 3, 1);

            pnlProductCard.Controls.Add(tblFp);
            tblMain.Controls.Add(pnlProductCard, 0, 1);

            // ──────────────────────────────────────────────────────────────
            // [صف 2]: شريط مصاريف التشغيل والإضافات
            // ──────────────────────────────────────────────────────────────
            var pnlExpensesBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0, 0, 0, 6)
            };
            pnlExpensesBar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlExpensesBar.Width - 1, pnlExpensesBar.Height - 1);
                }
            };

            var pnlExpRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            var lblExp = new Label
            {
                Text = "⚡ مصاريف تشغيل إضافية (كهرباء/عمالة):",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                Margin = new Padding(0, 6, 6, 0)
            };
            pnlExpRight.Controls.Add(lblExp);

            numExtraExpenses = new NumericUpDown
            {
                Width = 110,
                DecimalPlaces = 2,
                Minimum = 0m,
                Maximum = 1000000m,
                Value = 0m,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(180, 83, 9),
                TextAlign = HorizontalAlignment.Center,
                Margin = new Padding(0, 3, 14, 0)
            };
            numExtraExpenses.ValueChanged += (s, e) => RecalculateTotals();
            pnlExpRight.Controls.Add(numExtraExpenses);

            var lblExpNotes = new Label
            {
                Text = "بيان المصروفات:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Margin = new Padding(0, 6, 6, 0)
            };
            pnlExpRight.Controls.Add(lblExpNotes);

            txtExpensesNotes = new TextBox
            {
                Width = 260,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Margin = new Padding(0, 3, 14, 0)
            };
            pnlExpRight.Controls.Add(txtExpensesNotes);

            btnAddExtraRaw = Theme.MakeButton("➕ إضافة مادة خام إضافية", 0, 0, 175, 30, Color.FromArgb(51, 65, 85));
            btnAddExtraRaw.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnAddExtraRaw.Margin = new Padding(0, 1, 0, 0);
            btnAddExtraRaw.Click += (s, e) => AddExtraRawMaterial();
            pnlExpRight.Controls.Add(btnAddExtraRaw);

            pnlExpensesBar.Controls.Add(pnlExpRight);
            tblMain.Controls.Add(pnlExpensesBar, 0, 2);

            // ──────────────────────────────────────────────────────────────
            // [صف 3]: جدول المواد الخام المستهلكة (DataGrid)
            // ──────────────────────────────────────────────────────────────
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 34 },
                GridColor = Color.FromArgb(226, 232, 240),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                Margin = new Padding(0, 0, 0, 6)
            };
            dgItems.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgItems.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                SelectionBackColor = Color.FromArgb(224, 242, 254),
                SelectionForeColor = Color.Black,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgItems.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252)
            };
            Theme.EnableDoubleBuffer(dgItems);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowNum", HeaderText = "م", FillWeight = 8, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductCode", HeaderText = "كود المادة الخام", FillWeight = 18, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductName", HeaderText = "اسم المادة الخام", FillWeight = 38, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المستهلكة", FillWeight = 16, DefaultCellStyle = { ForeColor = Color.FromArgb(2, 132, 199), Font = new Font("Segoe UI", 10f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 12 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "سعر التكلفة", FillWeight = 15, ReadOnly = false, DefaultCellStyle = { ForeColor = Color.FromArgb(180, 83, 9), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "إجمالي التكلفة", FillWeight = 18, ReadOnly = true, DefaultCellStyle = { ForeColor = Color.FromArgb(217, 119, 6), Font = new Font("Segoe UI", 10f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 20 });

            var colDelete = new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "حذف",
                Text = "🗑️",
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
            tblMain.Controls.Add(dgItems, 0, 3);

            // ──────────────────────────────────────────────────────────────
            // [صف 4]: شريط العمليات والملخص المالي السفلي
            // ──────────────────────────────────────────────────────────────
            var tblBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.White,
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(0)
            };
            tblBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f)); // Column 0 (Right): الأزرار
            tblBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f)); // Column 1 (Left): الملخص المالي
            tblBottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblBottom.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, tblBottom.Width - 1, tblBottom.Height - 1);
                }
            };

            // Column 0: الأزرار
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            tblBottom.Controls.Add(pnlActions, 0, 0);

            btnComplete = Theme.MakeButton("✅ إتمام وترحيل التصنيع", 0, 0, 175, 40, Color.FromArgb(22, 163, 74));
            btnComplete.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnComplete.Margin = new Padding(0, 0, 6, 0);
            btnComplete.Click += (s, e) => SaveOrder(true);
            pnlActions.Controls.Add(btnComplete);

            btnSuspend = Theme.MakeButton("⏸️ تعليق (خروج الخامات)", 0, 0, 165, 40, Color.FromArgb(234, 88, 12));
            btnSuspend.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSuspend.Margin = new Padding(0, 0, 6, 0);
            btnSuspend.Click += (s, e) => SaveOrder(false);
            pnlActions.Controls.Add(btnSuspend);

            btnNew = Theme.MakeButton("➕ أمر جديد", 0, 0, 105, 40, Color.FromArgb(51, 65, 85));
            btnNew.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnNew.Margin = new Padding(0, 0, 6, 0);
            btnNew.Click += (s, e) => ResetForm();
            pnlActions.Controls.Add(btnNew);

            btnPrint = Theme.MakeButton("🖨️ طباعة إذن التشغيل", 0, 0, 145, 40, Color.FromArgb(2, 132, 199));
            btnPrint.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnPrint.Margin = new Padding(0, 0, 6, 0);
            btnPrint.Click += (s, e) => PrintOrder();
            pnlActions.Controls.Add(btnPrint);

            btnCancelOrder = Theme.MakeButton("❌ إلغاء الأمر", 0, 0, 100, 40, Color.FromArgb(220, 53, 69));
            btnCancelOrder.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnCancelOrder.Margin = new Padding(0, 0, 0, 0);
            btnCancelOrder.Click += (s, e) => CancelOrder();
            pnlActions.Controls.Add(btnCancelOrder);

            // Column 1: الملخص المالي
            var pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            tblBottom.Controls.Add(pnlSummary, 1, 0);

            lblUnitCost = new Label
            {
                Text = "🏷️ تكلفة الوحدة: 0.00 ج.م",
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                BackColor = Color.FromArgb(236, 253, 245),
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(2, 2, 4, 2)
            };
            pnlSummary.Controls.Add(lblUnitCost);

            lblTotalCost = new Label
            {
                Text = "💰 الإجمالي: 0.00 ج.م",
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(2, 2, 4, 2)
            };
            pnlSummary.Controls.Add(lblTotalCost);

            lblExtraCost = new Label
            {
                Text = "⚡ المصاريف: 0.00 ج.م",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),
                BackColor = Color.FromArgb(255, 237, 213),
                Padding = new Padding(6, 6, 6, 6),
                Margin = new Padding(2, 2, 4, 2)
            };
            pnlSummary.Controls.Add(lblExtraCost);

            lblRawCost = new Label
            {
                Text = "📦 خامات: 0.00 ج.م",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(6, 6, 6, 6),
                Margin = new Padding(2, 2, 2, 2)
            };
            pnlSummary.Controls.Add(lblRawCost);

            tblMain.Controls.Add(tblBottom, 0, 4);
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
                       COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.ItemID DESC), 0) AS CostPrice, 
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

            if (lblEstimatedDuration != null)
            {
                lblEstimatedDuration.Text = !string.IsNullOrEmpty(_currentBOM.EstimatedDuration)
                    ? $"⏱️ مدة التصنيع: {_currentBOM.EstimatedDuration}"
                    : "⏱️ مدة التصنيع: غير محددة";
            }

            decimal baseOutQty = _currentBOM.OutputQty > 0 ? _currentBOM.OutputQty : 1m;
            decimal multiplier = numProducedQty.Value / baseOutQty;

            int rowNum = 1;
            foreach (var itm in _currentBOM.Items)
            {
                decimal cost = itm.RawCostPrice;
                if (cost <= 0)
                {
                    var fallbackCostObj = DbHelper.Scalar(
                        "SELECT COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = @pid ORDER BY pi2.ItemID DESC), 0) FROM Products WHERE ProductID = @pid",
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
                        SELECT ProductID, ProductCode, ProductName,                                COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.ItemID DESC), 0) AS CostPrice, 
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
                EstimatedDuration = _currentBOM != null ? (_currentBOM.EstimatedDuration ?? "") : "",
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

            if (lblEstimatedDuration != null)
            {
                lblEstimatedDuration.Text = !string.IsNullOrEmpty(order.EstimatedDuration)
                    ? $"⏱️ مدة التصنيع: {order.EstimatedDuration}"
                    : "⏱️ مدة التصنيع: غير محددة";
            }

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
            using (var dlg = new FrmSuspendedOrdersDialog("Fixed"))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedProductionID > 0)
                {
                    if (dlg.SelectedProductionType == "Custom")
                    {
                        using (var frmCust = new FrmCustomProduction(dlg.SelectedProductionID))
                        {
                            frmCust.ShowDialog(this);
                        }
                    }
                    else
                    {
                        LoadExistingOrder(dlg.SelectedProductionID);
                    }
                }
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
            if (lblEstimatedDuration != null) lblEstimatedDuration.Text = "⏱️ مدة التصنيع: غير محددة";

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

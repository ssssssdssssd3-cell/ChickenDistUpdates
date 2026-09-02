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
        private TextBox txtEstimatedDuration;
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
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 108f)); // Row 1: بطاقة المنتج النهائي المصنع
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 98f));  // Row 2: شريط اختيار الخامات ومصاريف التشغيل
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 3: جدول المواد المستهلكة
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
                Text = "🛠️ أمر تصنيع مخصص",
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(147, 51, 234),
                Margin = new Padding(0, 4, 12, 0)
            };
            pnlHeaderRight.Controls.Add(lblTitle);

            lblOrderCode = new Label
            {
                Text = "كود الأمر: CPRD-...",
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
                Text = "🎯 بيانات المنتج النهائي المصنع (المطلوب إنتاجه):",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            pnlFpHeader.Controls.Add(lblFpTitle);
            pnlProductCard.Controls.Add(pnlFpHeader);

            var tblFp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f)); // 0: الصنف النهائي + بحث
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f)); // 1: الكمية المنتجة
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f)); // 2: الوحدة
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f)); // 3: مدة التصنيع
            tblFp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f)); // 4: الملاحظات
            tblFp.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));     // Row 0: Labels
            tblFp.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));     // Row 1: Inputs

            // Labels
            tblFp.Controls.Add(new Label { Text = "المنتج النهائي (كود/اسم/بحث):", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomRight }, 0, 0);
            tblFp.Controls.Add(new Label { Text = "الكمية الناتجة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 1, 0);
            tblFp.Controls.Add(new Label { Text = "الوحدة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 2, 0);
            tblFp.Controls.Add(new Label { Text = "⏱️ مدة التصنيع:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 3, 0);
            tblFp.Controls.Add(new Label { Text = "ملاحظات التشغيل:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomRight }, 4, 0);

            // Controls
            // 0: المنتج النهائي + زر البحث
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

            btnBrowseFinished = Theme.MakeButton("🔍 بحث", 0, 0, 75, 30, Theme.Primary);
            btnBrowseFinished.Dock = DockStyle.Left;
            btnBrowseFinished.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnBrowseFinished.Click += (s, e) => SelectFinishedProduct();

            pnlFpInputs.Controls.Add(txtFinishedProduct);
            pnlFpInputs.Controls.Add(btnBrowseFinished);
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
            numProducedQty.ValueChanged += (s, e) => RecalculateTotals();
            tblFp.Controls.Add(numProducedQty, 1, 1);

            // 2: الوحدة
            txtUnitName = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                Text = "قطعة",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
            };
            tblFp.Controls.Add(txtUnitName, 2, 1);

            // 3: مدة التصنيع
            txtEstimatedDuration = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
            };
            tblFp.Controls.Add(txtEstimatedDuration, 3, 1);

            // 4: الملاحظات
            txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 0),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            tblFp.Controls.Add(txtNotes, 4, 1);

            pnlProductCard.Controls.Add(tblFp);
            tblMain.Controls.Add(pnlProductCard, 0, 1);

            // ──────────────────────────────────────────────────────────────
            // [صف 2]: بطاقة اختيار الخامات ومصاريف التشغيل
            // ──────────────────────────────────────────────────────────────
            var pnlQuickAdd = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10, 4, 10, 4),
                Margin = new Padding(0, 0, 0, 6)
            };
            pnlQuickAdd.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlQuickAdd.Width - 1, pnlQuickAdd.Height - 1);
                }
            };

            var tblQuickLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            tblQuickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f)); // Row 0: شريط إضافة المادة الخام
            tblQuickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // Row 1: شريط المصاريف الإضافية

            // 1. شريط المادة الخام
            var tblRawAdd = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            tblRawAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f)); // المادة الخام
            tblRawAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f)); // الوحدة
            tblRawAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11f)); // الكمية
            tblRawAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f)); // سعر التكلفة
            tblRawAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f)); // التكلفة التقديرية
            tblRawAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f)); // زر الإضافة
            tblRawAdd.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var pnlRawBox = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 6, 0) };
            txtRawProduct = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            txtRawProduct.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    string q = txtRawProduct.Text.Trim();
                    if (string.IsNullOrEmpty(q)) { SelectRawProduct(true); return; }
                    var dt = DbHelper.Query("SELECT TOP 1 ProductID FROM Products WHERE ProductCode = @q OR Unit1Barcode = @q OR Unit2Barcode = @q OR ProductName LIKE '%' + @q + '%'", DbHelper.P("@q", q));
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        LoadRawProductByID(Convert.ToInt32(dt.Rows[0]["ProductID"]), true);
                    }
                    else
                    {
                        SelectRawProduct(true, q);
                    }
                }
            };
            btnBrowseRaw = Theme.MakeButton("🔍 خامات", 0, 0, 72, 28, Color.FromArgb(71, 85, 105));
            btnBrowseRaw.Dock = DockStyle.Left;
            btnBrowseRaw.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnBrowseRaw.Click += (s, e) => SelectRawProduct(true);

            pnlRawBox.Controls.Add(txtRawProduct);
            pnlRawBox.Controls.Add(btnBrowseRaw);
            tblRawAdd.Controls.Add(pnlRawBox, 0, 0);

            txtRawUnit = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 6, 0),
                Text = "قطعة",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black,
                TextAlign = HorizontalAlignment.Center
            };
            tblRawAdd.Controls.Add(txtRawUnit, 1, 0);

            numRawQty = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 6, 0),
                DecimalPlaces = 3,
                Minimum = 0.001m,
                Maximum = 1000000m,
                Value = 1m,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(2, 132, 199),
                TextAlign = HorizontalAlignment.Center
            };
            tblRawAdd.Controls.Add(numRawQty, 2, 0);

            lblRawCost = new Label
            {
                Text = "سعر التكلفة: 0.00 ج.م",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 6, 0),
                ForeColor = Color.FromArgb(180, 83, 9),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            tblRawAdd.Controls.Add(lblRawCost, 3, 0);

            var lblRawHint = new Label
            {
                Text = "اضغط Enter للإضافة",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 6, 0),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter
            };
            tblRawAdd.Controls.Add(lblRawHint, 4, 0);

            btnAddRaw = Theme.MakeButton("➕ إضافة للصرف", 0, 0, 130, 30, Color.FromArgb(16, 185, 129));
            btnAddRaw.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnAddRaw.Dock = DockStyle.Fill;
            btnAddRaw.Margin = new Padding(0, 4, 0, 0);
            btnAddRaw.Click += (s, e) => AddCurrentRawToGrid();
            tblRawAdd.Controls.Add(btnAddRaw, 5, 0);

            tblQuickLayout.Controls.Add(tblRawAdd, 0, 0);

            // 2. شريط مصاريف التشغيل
            var pnlExpRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            var lblExp = new Label
            {
                Text = "⚡ مصاريف تشغيل إضافية (كهرباء/عمالة):",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                Margin = new Padding(0, 6, 6, 0)
            };
            pnlExpRow.Controls.Add(lblExp);

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
            pnlExpRow.Controls.Add(numExtraExpenses);

            var lblExpNotes = new Label
            {
                Text = "بيان وتفاصيل المصروفات:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Margin = new Padding(0, 6, 6, 0)
            };
            pnlExpRow.Controls.Add(lblExpNotes);

            txtExpensesNotes = new TextBox
            {
                Width = 450,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Margin = new Padding(0, 3, 0, 0)
            };
            pnlExpRow.Controls.Add(txtExpensesNotes);

            tblQuickLayout.Controls.Add(pnlExpRow, 0, 1);
            pnlQuickAdd.Controls.Add(tblQuickLayout);
            tblMain.Controls.Add(pnlQuickAdd, 0, 2);

            // ──────────────────────────────────────────────────────────────
            // [صف 3]: جدول المواد المستهلكة (DataGrid)
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
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductCode", HeaderText = "كود الصنف", FillWeight = 18, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductName", HeaderText = "اسم الصنف", FillWeight = 38, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 16, DefaultCellStyle = { ForeColor = Color.FromArgb(2, 132, 199), Font = new Font("Segoe UI", 10f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 12 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "سعر التكلفة", FillWeight = 15, ReadOnly = false, DefaultCellStyle = { ForeColor = Color.FromArgb(180, 83, 9), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "الإجمالي", FillWeight = 18, ReadOnly = true, DefaultCellStyle = { ForeColor = Color.FromArgb(217, 119, 6), Font = new Font("Segoe UI", 10f, FontStyle.Bold) } });
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

            btnSuspend = Theme.MakeButton("⏸️ تعليق (خصم المواد)", 0, 0, 165, 40, Color.FromArgb(234, 88, 12));
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

            lblUnitCostSummary = new Label
            {
                Text = "🏷️ تكلفة الوحدة: 0.00 ج.م",
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                BackColor = Color.FromArgb(236, 253, 245),
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(2, 2, 4, 2)
            };
            pnlSummary.Controls.Add(lblUnitCostSummary);

            lblTotalCostSummary = new Label
            {
                Text = "💰 الإجمالي: 0.00 ج.م",
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(2, 2, 4, 2)
            };
            pnlSummary.Controls.Add(lblTotalCostSummary);

            lblExtraCostSummary = new Label
            {
                Text = "⚡ المصاريف: 0.00 ج.م",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),
                BackColor = Color.FromArgb(255, 237, 213),
                Padding = new Padding(6, 6, 6, 6),
                Margin = new Padding(2, 2, 4, 2)
            };
            pnlSummary.Controls.Add(lblExtraCostSummary);

            lblRawCostSummary = new Label
            {
                Text = "📦 خامات: 0.00 ج.م",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(6, 6, 6, 6),
                Margin = new Padding(2, 2, 2, 2)
            };
            pnlSummary.Controls.Add(lblRawCostSummary);

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
                AppLogger.Error("FrmCustomProduction.LoadWarehouses", ex);
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
                RecalculateTotals();
                txtRawProduct.Focus();
            }
        }

        private void SelectRawProduct(bool autoAddToGrid = true, string initialSearch = "")
        {
            using (var frm = new FrmProductSearch(defaultShowZeroStock: true, initialSearchText: initialSearch))
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    if (_selectedFinishedProductID > 0 && frm.SelectedProductID == _selectedFinishedProductID)
                    {
                        MessageBox.Show("لا يمكن اختيار نفس الصنف النهائي كمادة خام مستهلكة لنفسه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    LoadRawProductByID(frm.SelectedProductID, autoAddToGrid, frm.SelectedUnitName, frm.SelectedPurchasePrice, frm.SelectedQuantity);
                }
            }
        }

        private void LoadRawProductByID(int productId, bool autoAddToGrid = true, string unitOverride = "", decimal costOverride = 0, decimal qtyOverride = 1m)
        {
            var dt = DbHelper.Query(@"
                SELECT ProductID, ProductCode, ProductName, 
                       COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.ItemID DESC), 0) AS CostPrice, 
                       COALESCE(Unit1Name, Unit, N'قطعة') AS UnitName 
                FROM Products WHERE ProductID = @id",
                DbHelper.P("@id", productId));

            if (dt != null && dt.Rows.Count > 0)
            {
                _selectedRawProductID = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                _selectedRawProductCode = dt.Rows[0]["ProductCode"]?.ToString();
                _selectedRawProductName = dt.Rows[0]["ProductName"]?.ToString();
                _selectedRawCostPrice = costOverride > 0 ? costOverride : Convert.ToDecimal(dt.Rows[0]["CostPrice"] ?? 0);

                txtRawProduct.Text = $"{_selectedRawProductCode} - {_selectedRawProductName}";
                txtRawUnit.Text = !string.IsNullOrEmpty(unitOverride) ? unitOverride : (dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة");
                numRawQty.Value = qtyOverride > 0 ? qtyOverride : 1m;
                lblRawCost.Text = $"سعر التكلفة: {_selectedRawCostPrice:N2} ج.م";

                if (autoAddToGrid)
                {
                    CommitCurrentRawToGrid();
                }
                else
                {
                    numRawQty.Focus();
                }
            }
        }

        private void CommitCurrentRawToGrid()
        {
            if (_selectedRawProductID <= 0) return;

            decimal qty = numRawQty.Value > 0 ? numRawQty.Value : 1m;

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

        private void AddCurrentRawToGrid()
        {
            if (_selectedRawProductID <= 0)
            {
                SelectRawProduct(autoAddToGrid: true);
                return;
            }

            CommitCurrentRawToGrid();
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
                EstimatedDuration = txtEstimatedDuration != null ? txtEstimatedDuration.Text.Trim() : "",
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
            if (txtEstimatedDuration != null) txtEstimatedDuration.Text = order.EstimatedDuration ?? "";
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
            using (var dlg = new FrmSuspendedOrdersDialog("Custom"))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedProductionID > 0)
                {
                    if (dlg.SelectedProductionType == "Fixed")
                    {
                        using (var frmFixed = new FrmFixedProduction(dlg.SelectedProductionID))
                        {
                            frmFixed.ShowDialog(this);
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
            if (txtEstimatedDuration != null) txtEstimatedDuration.Clear();
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

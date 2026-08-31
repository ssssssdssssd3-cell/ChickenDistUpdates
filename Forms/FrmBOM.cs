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
    /// <summary>
    /// شاشة تحديد وتعديل شجرة ومكونات التصنيع (Bill of Materials / BOM)
    /// بتصميم ذكي وعملي فائق السرعة يدعم الباركود والبحث المباشر
    /// </summary>
    public class FrmBOM : Form
    {
        private int _currentBOMID = 0;
        private int _selectedFinishedProductID = 0;
        private string _selectedFinishedProductCode = "";
        private string _selectedFinishedProductName = "";

        // Controls - Top Panel (Finished Product)
        private TextBox txtFinishedProduct;
        private Button btnBrowseFinished;
        private NumericUpDown numOutputQty;
        private TextBox txtUnitName;
        private TextBox txtNotes;
        private Label lblHeaderUnitCostBadge;

        // Controls - Raw Material Quick Add Bar
        private int _selectedRawProductID = 0;
        private string _selectedRawProductCode = "";
        private string _selectedRawProductName = "";
        private decimal _selectedRawCostPrice = 0;
        private TextBox txtRawProduct;
        private Button btnBrowseRaw;
        private NumericUpDown numRawQty;
        private TextBox txtRawUnit;
        private NumericUpDown numRawCostPrice;
        private Label lblRawTotalPreview;
        private Button btnAddRaw;

        // Grid
        private DataGridView dgItems;

        // Summary Badges
        private Label lblTotalRawCost;
        private Label lblUnitCost;
        private Label lblItemsCount;

        // Action Buttons
        private Button btnSave;
        private Button btnNew;
        private Button btnDelete;
        private Button btnPrint;

        // Side List of Saved BOMs
        private TextBox txtSearchBOM;
        private Label lblBOMCount;
        private DataGridView dgBOMList;

        public FrmBOM(int preselectedProductID = 0)
        {
            _selectedFinishedProductID = preselectedProductID;
            InitUI();
            LoadSavedBOMsList();

            if (_selectedFinishedProductID > 0)
            {
                LoadFinishedProductByID(_selectedFinishedProductID);
            }
        }

        private void InitUI()
        {
            this.Text = "🌿 شجرة ومكونات التصنيع (BOM) - تحديد وتعديل وصفات الإنتاج المعيارية";
            this.Size = new Size(1220, 760);
            this.MinimumSize = new Size(1060, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false; // لمنع تشوه التخطيط في ويندوز فورمز
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ══════════════════════════════════════════════════════════════
            // الحاوية الرئيسية: محرر الوصفة (يمين 70%) + قائمة الوصفات (يسار 30%)
            // ══════════════════════════════════════════════════════════════
            var tblContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain,
                Padding = new Padding(8)
            };
            tblContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72f)); // 0: المحرر (على اليمين)
            tblContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f)); // 1: القائمة الجانبية (على اليسار)
            tblContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.Controls.Add(tblContainer);

            // ──────────────────────────────────────────────────────────────
            // 1. المحرر الرئيسي للوصفة (الجانب الأيمن)
            // ──────────────────────────────────────────────────────────────
            var pnlEditor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgMain,
                Padding = new Padding(4)
            };
            tblContainer.Controls.Add(pnlEditor, 0, 0);

            // ── أ) بطاقة المنتج النهائي المصنع (Top Card) ──
            var pnlFinishedCard = CreateCardPanel(130);

            // ترويسة البطاقة
            var pnlFinishedHeader = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.Transparent };
            var lblFpTitle = new Label
            {
                Text = "🎯 بيانات المنتج النهائي المصنع (المعياري):",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlFinishedHeader.Controls.Add(lblFpTitle);

            lblHeaderUnitCostBadge = new Label
            {
                Text = "🏷️ تكلفة الوحدة المصنعة: 0.00 ج.م",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(8, 3, 8, 3)
            };
            pnlFinishedHeader.Controls.Add(lblHeaderUnitCostBadge);
            pnlFinishedCard.Controls.Add(pnlFinishedHeader);

            // حقول المنتج النهائي
            var flowFinished = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };
            pnlFinishedCard.Controls.Add(flowFinished);
            flowFinished.BringToFront();

            // 1. حقل الصنف النهائي
            var pnlFpInput = new Panel { Width = 360, Height = 58, Margin = new Padding(0, 0, 10, 0) };
            pnlFpInput.Controls.Add(new Label { Text = "الصنف النهائي (اكتب كود/اسم أو اضغط بحث):", Dock = DockStyle.Top, Height = 20, ForeColor = Theme.TextSub, Font = Theme.FontSmall });
            
            txtFinishedProduct = new TextBox
            {
                Location = new Point(90, 22),
                Width = 270,
                Height = 32,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            txtFinishedProduct.KeyDown += TxtFinishedProduct_KeyDown;
            pnlFpInput.Controls.Add(txtFinishedProduct);

            btnBrowseFinished = Theme.MakeButton("🔍 بحث", 0, 21, 85, 33, Theme.Primary);
            btnBrowseFinished.Click += (s, e) => SelectFinishedProduct();
            pnlFpInput.Controls.Add(btnBrowseFinished);
            flowFinished.Controls.Add(pnlFpInput);

            // 2. الكمية المعيارية
            var pnlOutputQty = new Panel { Width = 110, Height = 58, Margin = new Padding(0, 0, 10, 0) };
            pnlOutputQty.Controls.Add(new Label { Text = "الكمية المعيارية:", Dock = DockStyle.Top, Height = 20, ForeColor = Theme.TextSub, Font = Theme.FontSmall });
            numOutputQty = new NumericUpDown
            {
                Location = new Point(0, 22),
                Width = 110,
                Height = 32,
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 1000000m,
                Value = 1m,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                TextAlign = HorizontalAlignment.Center
            };
            numOutputQty.ValueChanged += (s, e) => RecalculateTotals();
            pnlOutputQty.Controls.Add(numOutputQty);
            flowFinished.Controls.Add(pnlOutputQty);

            // 3. الوحدة
            var pnlUnit = new Panel { Width = 95, Height = 58, Margin = new Padding(0, 0, 10, 0) };
            pnlUnit.Controls.Add(new Label { Text = "الوحدة:", Dock = DockStyle.Top, Height = 20, ForeColor = Theme.TextSub, Font = Theme.FontSmall });
            txtUnitName = new TextBox
            {
                Location = new Point(0, 22),
                Width = 95,
                Height = 32,
                Text = "قطعة",
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                TextAlign = HorizontalAlignment.Center
            };
            pnlUnit.Controls.Add(txtUnitName);
            flowFinished.Controls.Add(pnlUnit);

            // 4. الملاحظات
            var pnlNotes = new Panel { Width = 230, Height = 58, Margin = new Padding(0, 0, 0, 0) };
            pnlNotes.Controls.Add(new Label { Text = "ملاحظات الوصفة المعيارية:", Dock = DockStyle.Top, Height = 20, ForeColor = Theme.TextSub, Font = Theme.FontSmall });
            txtNotes = new TextBox
            {
                Location = new Point(0, 22),
                Width = 230,
                Height = 32,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlNotes.Controls.Add(txtNotes);
            flowFinished.Controls.Add(pnlNotes);

            // ── ب) شريط الإضافة السريع للمواد الخام (Quick Add Bar - Height 85px) ──
            var pnlQuickAdd = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 6, 0, 6)
            };
            pnlEditor.Controls.Add(pnlQuickAdd);
            pnlQuickAdd.BringToFront();

            var flowQuick = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlQuickAdd.Controls.Add(flowQuick);

            // 1. المادة الخام
            var pnlRawInput = new Panel { Width = 310, Height = 64, Margin = new Padding(0, 0, 8, 0) };
            pnlRawInput.Controls.Add(new Label { Text = "📦 المادة الخام (كود/اسم/باركود):", Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) });
            
            txtRawProduct = new TextBox
            {
                Location = new Point(80, 24),
                Width = 230,
                Height = 30,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            txtRawProduct.KeyDown += TxtRawProduct_KeyDown;
            pnlRawInput.Controls.Add(txtRawProduct);

            btnBrowseRaw = Theme.MakeButton("🔍 خامات", 0, 23, 76, 31, Color.FromArgb(71, 85, 105));
            btnBrowseRaw.Click += (s, e) => SelectRawProduct("", autoAddToGrid: true);
            pnlRawInput.Controls.Add(btnBrowseRaw);
            flowQuick.Controls.Add(pnlRawInput);

            // 2. الوحدة
            var pnlRawU = new Panel { Width = 75, Height = 64, Margin = new Padding(0, 0, 8, 0) };
            pnlRawU.Controls.Add(new Label { Text = "الوحدة:", Dock = DockStyle.Top, Height = 20, Font = Theme.FontSmall, ForeColor = Color.FromArgb(71, 85, 105) });
            txtRawUnit = new TextBox
            {
                Location = new Point(0, 24),
                Width = 75,
                Height = 30,
                Text = "قطعة",
                Font = Theme.FontMain,
                BackColor = Color.White,
                ForeColor = Color.Black,
                TextAlign = HorizontalAlignment.Center
            };
            pnlRawU.Controls.Add(txtRawUnit);
            flowQuick.Controls.Add(pnlRawU);

            // 3. الكمية المطلوبة
            var pnlRawQ = new Panel { Width = 95, Height = 64, Margin = new Padding(0, 0, 8, 0) };
            pnlRawQ.Controls.Add(new Label { Text = "الكمية المطلوبة:", Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) });
            numRawQty = new NumericUpDown
            {
                Location = new Point(0, 24),
                Width = 95,
                Height = 30,
                DecimalPlaces = 3,
                Minimum = 0.001m,
                Maximum = 1000000m,
                Value = 1m,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black,
                TextAlign = HorizontalAlignment.Center
            };
            numRawQty.ValueChanged += (s, e) => UpdateRawTotalPreview();
            numRawQty.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddCurrentRawToGrid(); } };
            pnlRawQ.Controls.Add(numRawQty);
            flowQuick.Controls.Add(pnlRawQ);

            // 4. سعر التكلفة للوحدة
            var pnlRawC = new Panel { Width = 95, Height = 64, Margin = new Padding(0, 0, 8, 0) };
            pnlRawC.Controls.Add(new Label { Text = "سعر التكلفة:", Dock = DockStyle.Top, Height = 20, Font = Theme.FontSmall, ForeColor = Color.FromArgb(71, 85, 105) });
            numRawCostPrice = new NumericUpDown
            {
                Location = new Point(0, 24),
                Width = 95,
                Height = 30,
                DecimalPlaces = 2,
                Minimum = 0m,
                Maximum = 1000000m,
                Value = 0m,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(180, 83, 9),
                TextAlign = HorizontalAlignment.Center
            };
            numRawCostPrice.ValueChanged += (s, e) => UpdateRawTotalPreview();
            numRawCostPrice.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddCurrentRawToGrid(); } };
            pnlRawC.Controls.Add(numRawCostPrice);
            flowQuick.Controls.Add(pnlRawC);

            // 5. إجمالي تكلفة البند
            var pnlRawTot = new Panel { Width = 105, Height = 64, Margin = new Padding(0, 0, 8, 0) };
            pnlRawTot.Controls.Add(new Label { Text = "إجمالي التكلفة:", Dock = DockStyle.Top, Height = 20, Font = Theme.FontSmall, ForeColor = Color.FromArgb(71, 85, 105) });
            lblRawTotalPreview = new Label
            {
                Text = "0.00 ج.م",
                Location = new Point(0, 24),
                Width = 105,
                Height = 30,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlRawTot.Controls.Add(lblRawTotalPreview);
            flowQuick.Controls.Add(pnlRawTot);

            // 6. زر الإضافة الكبير
            btnAddRaw = Theme.MakeButton("➕ إضافة للشجرة", 0, 0, 135, 34, Color.FromArgb(16, 185, 129));
            btnAddRaw.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnAddRaw.Margin = new Padding(0, 22, 4, 0);
            btnAddRaw.Click += (s, e) => AddCurrentRawToGrid();
            flowQuick.Controls.Add(btnAddRaw);

            // ── ج) جدول بنود وخامات الوصفة (DataGrid) ──
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 34 },
                GridColor = Color.FromArgb(226, 232, 240)
            };
            dgItems.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgItems.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
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
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowNum", HeaderText = "م", FillWeight = 6, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductCode", HeaderText = "كود الخام", FillWeight = 14, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductName", HeaderText = "اسم المادة الخام", FillWeight = 34, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المطلوبة", FillWeight = 14, DefaultCellStyle = { ForeColor = Color.FromArgb(2, 132, 199), Font = new Font("Segoe UI", 10f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 10, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "سعر التكلفة", FillWeight = 14, DefaultCellStyle = { ForeColor = Color.FromArgb(180, 83, 9), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "إجمالي التكلفة", FillWeight = 16, ReadOnly = true, DefaultCellStyle = { ForeColor = Color.FromArgb(217, 119, 6), Font = new Font("Segoe UI", 10f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostPercent", HeaderText = "المساهمة %", FillWeight = 12, ReadOnly = true, DefaultCellStyle = { ForeColor = Color.FromArgb(71, 85, 105) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 18 });

            var colDelete = new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "حذف",
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
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
                    dgItems.Rows.RemoveAt(e.RowIndex);
                    ReindexGrid();
                    RecalculateTotals();
                }
            };

            // ── د) شريط الملخص والعمليات السفلي (Bottom Bar - Height 75px) ──
            var pnlBottom = new Panel
            {
                Height = 74,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8)
            };

            // الجانب الأيمن: الملخص المالي
            var pnlSummaryRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 520,
                Height = 58,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlBottom.Controls.Add(pnlSummaryRight);

            lblItemsCount = CreateBadge("📦 المكونات: 0", Color.FromArgb(71, 85, 105), Color.FromArgb(241, 245, 249));
            pnlSummaryRight.Controls.Add(lblItemsCount);

            lblTotalRawCost = CreateBadge("💰 إجمالي الخامات: 0.00 ج.م", Color.FromArgb(217, 119, 6), Color.FromArgb(254, 243, 199));
            pnlSummaryRight.Controls.Add(lblTotalRawCost);

            lblUnitCost = CreateBadge("🏷️ تكلفة الوحدة: 0.00 ج.م", Color.FromArgb(16, 185, 129), Color.FromArgb(236, 253, 245));
            pnlSummaryRight.Controls.Add(lblUnitCost);

            // الجانب الأيسر: أزرار العمليات
            var pnlActionsLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 460,
                Height = 58,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlBottom.Controls.Add(pnlActionsLeft);

            btnSave = Theme.MakeButton("💾 حفظ شجرة التصنيع", 0, 0, 160, 42, Color.FromArgb(16, 185, 129));
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSave.Margin = new Padding(0, 6, 8, 0);
            btnSave.Click += (s, e) => SaveCurrentBOM();
            pnlActionsLeft.Controls.Add(btnSave);

            btnNew = Theme.MakeButton("➕ وصفة جديدة", 0, 0, 120, 42, Color.FromArgb(71, 85, 105));
            btnNew.Font = Theme.FontBold;
            btnNew.Margin = new Padding(0, 6, 8, 0);
            btnNew.Click += (s, e) => ResetForm();
            pnlActionsLeft.Controls.Add(btnNew);

            btnPrint = Theme.MakeButton("🖨️ طباعة", 0, 0, 95, 42, Color.FromArgb(2, 132, 199));
            btnPrint.Font = Theme.FontBold;
            btnPrint.Margin = new Padding(0, 6, 8, 0);
            btnPrint.Click += (s, e) => PrintBOM();
            pnlActionsLeft.Controls.Add(btnPrint);

            btnDelete = Theme.MakeButton("🗑️ حذف", 0, 0, 85, 42, Color.FromArgb(239, 68, 68));
            btnDelete.Font = Theme.FontBold;
            btnDelete.Margin = new Padding(0, 6, 0, 0);
            btnDelete.Click += (s, e) => DeleteCurrentBOM();
            pnlActionsLeft.Controls.Add(btnDelete);

            // تجميع عناصر المحرر في TableLayoutPanel لضمان ثبات التخطيط وظهور جدول الخامات
            var tblEditorLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tblEditorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 135f)); // 0: Finished Card
            tblEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86f));  // 1: Quick Add Bar
            tblEditorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // 2: Items DataGrid (dgItems)
            tblEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));  // 3: Bottom Summary & Buttons

            pnlFinishedCard.Dock = DockStyle.Fill;
            pnlQuickAdd.Dock = DockStyle.Fill;
            dgItems.Dock = DockStyle.Fill;
            pnlBottom.Dock = DockStyle.Fill;

            tblEditorLayout.Controls.Add(pnlFinishedCard, 0, 0);
            tblEditorLayout.Controls.Add(pnlQuickAdd, 0, 1);
            tblEditorLayout.Controls.Add(dgItems, 0, 2);
            tblEditorLayout.Controls.Add(pnlBottom, 0, 3);
            pnlEditor.Controls.Add(tblEditorLayout);


            // ──────────────────────────────────────────────────────────────
            // 2. القائمة الجانبية (الوصفات وشجر الإنتاج المسجلة - على اليسار)
            // ──────────────────────────────────────────────────────────────
            var pnlSidebar = CreateCardPanel(0);
            pnlSidebar.Dock = DockStyle.Fill;
            pnlSidebar.Padding = new Padding(8);
            tblContainer.Controls.Add(pnlSidebar, 1, 0);

            var pnlSideTop = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
            
            var lblSideTitle = new Label
            {
                Text = "📋 شجر الإنتاج والوصفات المسجلة",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlSideTop.Controls.Add(lblSideTitle);

            txtSearchBOM = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            txtSearchBOM.TextChanged += (s, e) => LoadSavedBOMsList(txtSearchBOM.Text.Trim());
            pnlSideTop.Controls.Add(txtSearchBOM);
            pnlSidebar.Controls.Add(pnlSideTop);

            lblBOMCount = new Label
            {
                Text = "إجمالي الوصفات: 0",
                Dock = DockStyle.Bottom,
                Height = 26,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlSidebar.Controls.Add(lblBOMCount);

            dgBOMList = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 },
                Margin = new Padding(0, 8, 0, 8)
            };
            dgBOMList.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgBOMList.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                SelectionBackColor = Color.FromArgb(224, 242, 254),
                SelectionForeColor = Color.Black
            };
            Theme.EnableDoubleBuffer(dgBOMList);

            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "BOMID", Visible = false });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الكود", FillWeight = 25, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "المنتج النهائي", FillWeight = 45, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold) } });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemsCount", HeaderText = "الخامات", FillWeight = 15, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalEstCost", HeaderText = "التكلفة", FillWeight = 20, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(217, 119, 6) } });

            dgBOMList.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int bomId = Convert.ToInt32(dgBOMList.Rows[e.RowIndex].Cells["BOMID"].Value);
                    LoadBOMByID(bomId);
                }
            };
            pnlSidebar.Controls.Add(dgBOMList);
            dgBOMList.BringToFront();
        }

        private Panel CreateCardPanel(int height)
        {
            var pnl = new Panel
            {
                Height = height,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 0, 8)
            };
            pnl.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
                }
            };
            return pnl;
        }

        private Label CreateBadge(string text, Color fore, Color back)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = fore,
                BackColor = back,
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(4, 8, 4, 4)
            };
        }

        // ══════════════════════════════════════════════════════════════
        //  معالجة اختيار وبحث المنتج النهائي
        // ══════════════════════════════════════════════════════════════
        private void TxtFinishedProduct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string query = txtFinishedProduct.Text.Trim();
                if (string.IsNullOrEmpty(query))
                {
                    SelectFinishedProduct();
                    return;
                }

                // محاولة مطابقة كود أو باركود أو اسم
                var dt = DbHelper.Query(@"
                    SELECT TOP 2 ProductID, ProductCode, ProductName 
                    FROM Products 
                    WHERE ProductCode = @q OR Unit1Barcode = @q OR Unit2Barcode = @q OR ProductName = @q OR ProductName LIKE '%' + @q + '%' OR ProductCode LIKE @q + '%'",
                    DbHelper.P("@q", query));

                if (dt != null && dt.Rows.Count == 1)
                {
                    int pid = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                    LoadFinishedProductByID(pid);
                }
                else
                {
                    SelectFinishedProduct(query);
                }
            }
        }

        private void SelectFinishedProduct(string initialSearch = "")
        {
            using (var frm = new FrmProductSearch(defaultShowZeroStock: true, initialSearchText: initialSearch))
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    LoadFinishedProductByID(frm.SelectedProductID);
                }
            }
        }

        private void LoadFinishedProductByID(int productId)
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

                // فحص إذا كان هناك وصفة مسجلة مسبقاً لهذا الصنف
                var existing = ProductionDAL.GetBOMByProductID(_selectedFinishedProductID);
                if (existing != null)
                {
                    LoadBOM(existing);
                }
                else
                {
                    _currentBOMID = 0;
                    dgItems.Rows.Clear();
                    numOutputQty.Value = 1m;
                    RecalculateTotals();
                }

                txtRawProduct.Focus();
                txtRawProduct.SelectAll();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  معالجة اختيار وبحث المادة الخام
        // ══════════════════════════════════════════════════════════════
        private void TxtRawProduct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string query = txtRawProduct.Text.Trim();
                if (string.IsNullOrEmpty(query))
                {
                    SelectRawProduct("", autoAddToGrid: true);
                    return;
                }

                // محاولة مطابقة كود أو باركود أو اسم
                var dt = DbHelper.Query(@"
                    SELECT TOP 2 ProductID, ProductCode, ProductName,
                           COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.PurchaseItemID DESC), 0) AS CostPrice,
                           COALESCE(Unit1Name, Unit, N'قطعة') AS UnitName
                    FROM Products 
                    WHERE ProductCode = @q OR Unit1Barcode = @q OR Unit2Barcode = @q OR ProductName = @q OR ProductName LIKE '%' + @q + '%' OR ProductCode LIKE @q + '%'",
                    DbHelper.P("@q", query));

                if (dt != null && dt.Rows.Count == 1)
                {
                    SetRawProduct(
                        Convert.ToInt32(dt.Rows[0]["ProductID"]),
                        dt.Rows[0]["ProductCode"]?.ToString(),
                        dt.Rows[0]["ProductName"]?.ToString(),
                        dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة",
                        Convert.ToDecimal(dt.Rows[0]["CostPrice"] ?? 0)
                    );
                    CommitRawItemToGrid();
                }
                else
                {
                    SelectRawProduct(query, autoAddToGrid: true);
                }
            }
        }

        private void SelectRawProduct(string initialSearch = "", bool autoAddToGrid = false)
        {
            using (var frm = new FrmProductSearch(defaultShowZeroStock: true, initialSearchText: initialSearch))
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    if (frm.SelectedProductID == _selectedFinishedProductID)
                    {
                        MessageBox.Show("لا يمكن اختيار نفس الصنف النهائي كمادة خام لنفسه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var dt = DbHelper.Query(@"
                        SELECT ProductID, ProductCode, ProductName, 
                               COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.PurchaseItemID DESC), 0) AS CostPrice, 
                               COALESCE(Unit1Name, Unit, N'قطعة') AS UnitName 
                        FROM Products WHERE ProductID = @id",
                        DbHelper.P("@id", frm.SelectedProductID));

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        string uName = !string.IsNullOrEmpty(frm.SelectedUnitName) ? frm.SelectedUnitName : (dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة");
                        decimal cost = frm.SelectedPurchasePrice > 0 ? frm.SelectedPurchasePrice : Convert.ToDecimal(dt.Rows[0]["CostPrice"] ?? 0);
                        decimal qty = frm.SelectedQuantity > 0 ? frm.SelectedQuantity : 1m;

                        SetRawProduct(
                            Convert.ToInt32(dt.Rows[0]["ProductID"]),
                            dt.Rows[0]["ProductCode"]?.ToString(),
                            dt.Rows[0]["ProductName"]?.ToString(),
                            uName,
                            cost,
                            qty
                        );

                        if (autoAddToGrid)
                        {
                            CommitRawItemToGrid();
                        }
                    }
                }
            }
        }

        private void SetRawProduct(int pid, string code, string name, string unit, decimal cost, decimal qty = 1m)
        {
            if (pid == _selectedFinishedProductID)
            {
                MessageBox.Show("لا يمكن اختيار نفس الصنف النهائي كمادة خام لنفسه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedRawProductID = pid;
            _selectedRawProductCode = code;
            _selectedRawProductName = name;
            _selectedRawCostPrice = cost;

            txtRawProduct.Text = $"{_selectedRawProductCode} - {_selectedRawProductName}";
            txtRawUnit.Text = unit;
            numRawCostPrice.Value = cost;
            numRawQty.Value = qty > 0 ? qty : 1m;

            UpdateRawTotalPreview();
            numRawQty.Focus();
            numRawQty.Select(0, numRawQty.Text.Length);
        }

        private void UpdateRawTotalPreview()
        {
            decimal qty = numRawQty.Value;
            decimal cost = numRawCostPrice.Value;
            lblRawTotalPreview.Text = $"{(qty * cost):N2} ج.م";
        }

        private void CommitRawItemToGrid()
        {
            if (_selectedRawProductID <= 0) return;

            decimal qty = numRawQty.Value > 0 ? numRawQty.Value : 1m;
            decimal cost = numRawCostPrice.Value;

            // فحص هل المادة مضافة مسبقاً في الجدول
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (Convert.ToInt32(row.Cells["RawProductID"].Value) == _selectedRawProductID)
                {
                    decimal cur = Convert.ToDecimal(row.Cells["Quantity"].Value);
                    row.Cells["Quantity"].Value = cur + qty;
                    row.Cells["UnitCost"].Value = cost.ToString("N2");
                    UpdateRowTotal(row.Index);
                    RecalculateTotals();
                    ClearRawInputs();
                    txtRawProduct.Focus();
                    return;
                }
            }

            decimal tot = qty * cost;
            dgItems.Rows.Add(
                _selectedRawProductID,
                dgItems.Rows.Count + 1,
                _selectedRawProductCode,
                _selectedRawProductName,
                qty.ToString("N3"),
                txtRawUnit.Text.Trim(),
                cost.ToString("N2"),
                tot.ToString("N2"),
                "0.0%",
                "",
                "🗑️"
            );

            ClearRawInputs();
            RecalculateTotals();
            txtRawProduct.Focus();
        }

        private void AddCurrentRawToGrid()
        {
            if (_selectedRawProductID <= 0)
            {
                string rawText = txtRawProduct.Text.Trim();
                if (!string.IsNullOrEmpty(rawText))
                {
                    var dt = DbHelper.Query(@"
                        SELECT TOP 2 ProductID, ProductCode, ProductName,
                               COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.PurchaseItemID DESC), 0) AS CostPrice,
                               COALESCE(Unit1Name, Unit, N'قطعة') AS UnitName
                        FROM Products 
                        WHERE ProductCode = @q OR Unit1Barcode = @q OR Unit2Barcode = @q OR ProductName = @q OR ProductName LIKE '%' + @q + '%' OR ProductCode LIKE @q + '%'",
                        DbHelper.P("@q", rawText));

                    if (dt != null && dt.Rows.Count == 1)
                    {
                        SetRawProduct(
                            Convert.ToInt32(dt.Rows[0]["ProductID"]),
                            dt.Rows[0]["ProductCode"]?.ToString(),
                            dt.Rows[0]["ProductName"]?.ToString(),
                            dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة",
                            Convert.ToDecimal(dt.Rows[0]["CostPrice"] ?? 0)
                        );
                        CommitRawItemToGrid();
                        return;
                    }
                    else
                    {
                        SelectRawProduct(rawText, autoAddToGrid: true);
                        return;
                    }
                }
            }

            if (_selectedRawProductID <= 0)
            {
                SelectRawProduct("", autoAddToGrid: true);
                return;
            }

            CommitRawItemToGrid();
        }

        private void ClearRawInputs()
        {
            _selectedRawProductID = 0;
            _selectedRawProductCode = "";
            _selectedRawProductName = "";
            _selectedRawCostPrice = 0;
            txtRawProduct.Clear();
            txtRawUnit.Text = "قطعة";
            numRawQty.Value = 1m;
            numRawCostPrice.Value = 0m;
            lblRawTotalPreview.Text = "0.00 ج.م";
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
            decimal totalCost = 0;
            int count = dgItems.Rows.Count;

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                decimal tot = Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0);
                totalCost += tot;
            }

            // تحديث نسب المساهمة لكل مادة خام
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                decimal tot = Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0);
                decimal pct = totalCost > 0 ? (tot / totalCost) * 100m : 0m;
                row.Cells["CostPercent"].Value = $"{pct:N1}%";
            }

            decimal outQty = numOutputQty.Value > 0 ? numOutputQty.Value : 1m;
            decimal unitCost = totalCost / outQty;

            lblItemsCount.Text = $"📦 المكونات: {count} صنف";
            lblTotalRawCost.Text = $"💰 إجمالي الخامات: {totalCost:N2} ج.م";
            lblUnitCost.Text = $"🏷️ تكلفة الوحدة: {unitCost:N2} ج.م";
            lblHeaderUnitCostBadge.Text = $"🏷️ تكلفة الوحدة المصنعة: {unitCost:N2} ج.م";
        }

        // ══════════════════════════════════════════════════════════════
        //  حفظ وحذف وطباعة وتحميل الوصفات
        // ══════════════════════════════════════════════════════════════
        private void SaveCurrentBOM()
        {
            if (_selectedFinishedProductID <= 0)
            {
                string fpText = txtFinishedProduct.Text.Trim();
                if (!string.IsNullOrEmpty(fpText))
                {
                    var dt = DbHelper.Query(@"
                        SELECT TOP 2 ProductID, ProductCode, ProductName 
                        FROM Products 
                        WHERE ProductCode = @q OR Unit1Barcode = @q OR Unit2Barcode = @q OR ProductName = @q OR ProductName LIKE '%' + @q + '%' OR ProductCode LIKE @q + '%'",
                        DbHelper.P("@q", fpText));

                    if (dt != null && dt.Rows.Count == 1)
                    {
                        LoadFinishedProductByID(Convert.ToInt32(dt.Rows[0]["ProductID"]));
                    }
                }
            }

            if (_selectedFinishedProductID <= 0)
            {
                MessageBox.Show("يرجى اختيار المنتج النهائي المراد تحديد مواد تصنيعه أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFinishedProduct.Focus();
                return;
            }

            if (dgItems.Rows.Count == 0)
            {
                MessageBox.Show("يجب إضافة مادة خام واحدة على الأقل في شجرة ومكونات التصنيع.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRawProduct.Focus();
                return;
            }

            var bom = new BOMModel
            {
                BOMID = _currentBOMID,
                ProductID = _selectedFinishedProductID,
                OutputQty = numOutputQty.Value,
                UnitName = txtUnitName.Text.Trim(),
                Notes = txtNotes.Text.Trim()
            };

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                bom.Items.Add(new BOMItemModel
                {
                    RawProductID = Convert.ToInt32(row.Cells["RawProductID"].Value),
                    Quantity = Convert.ToDecimal(row.Cells["Quantity"].Value),
                    UnitName = row.Cells["UnitName"].Value?.ToString(),
                    Notes = row.Cells["Notes"].Value?.ToString()
                });
            }

            try
            {
                _currentBOMID = ProductionDAL.SaveBOM(bom);
                MessageBox.Show("✅ تم حفظ شجرة ومكونات التصنيع بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSavedBOMsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل حفظ شجرة التصنيع:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSavedBOMsList(string search = "")
        {
            try
            {
                var dt = ProductionDAL.GetAllBOMs(search);
                dgBOMList.Rows.Clear();
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        decimal estCost = r["TotalEstCost"] != DBNull.Value ? Convert.ToDecimal(r["TotalEstCost"]) : 0m;
                        dgBOMList.Rows.Add(
                            r["BOMID"],
                            r["ProductCode"],
                            r["ProductName"],
                            $"{r["ItemsCount"]} صنف",
                            $"{estCost:N2} ج"
                        );
                    }
                    lblBOMCount.Text = $"إجمالي الوصفات المسجلة: {dt.Rows.Count}";
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmBOM.LoadSavedBOMsList", ex);
            }
        }

        private void LoadBOMByID(int bomId)
        {
            var bom = ProductionDAL.GetBOMByID(bomId);
            if (bom != null)
            {
                LoadBOM(bom);
            }
        }

        private void LoadBOM(BOMModel bom)
        {
            _currentBOMID = bom.BOMID;
            _selectedFinishedProductID = bom.ProductID;
            _selectedFinishedProductCode = bom.ProductCode;
            _selectedFinishedProductName = bom.ProductName;

            txtFinishedProduct.Text = $"{bom.ProductCode} - {bom.ProductName}";
            numOutputQty.Value = bom.OutputQty > 0 ? bom.OutputQty : 1m;
            txtUnitName.Text = bom.UnitName ?? "قطعة";
            txtNotes.Text = bom.Notes ?? "";

            dgItems.Rows.Clear();
            int rowNum = 1;
            foreach (var itm in bom.Items)
            {
                dgItems.Rows.Add(
                    itm.RawProductID,
                    rowNum++,
                    itm.RawProductCode,
                    itm.RawProductName,
                    itm.Quantity.ToString("N3"),
                    itm.UnitName,
                    itm.RawCostPrice.ToString("N2"),
                    itm.TotalCost.ToString("N2"),
                    "0.0%",
                    itm.Notes,
                    "🗑️"
                );
            }

            RecalculateTotals();
        }

        private void ResetForm()
        {
            _currentBOMID = 0;
            _selectedFinishedProductID = 0;
            _selectedFinishedProductCode = "";
            _selectedFinishedProductName = "";
            txtFinishedProduct.Clear();
            numOutputQty.Value = 1m;
            txtUnitName.Text = "قطعة";
            txtNotes.Clear();
            dgItems.Rows.Clear();
            ClearRawInputs();
            RecalculateTotals();
            txtFinishedProduct.Focus();
        }

        private void DeleteCurrentBOM()
        {
            if (_currentBOMID <= 0)
            {
                MessageBox.Show("لا توجد وصفة محفوظة محددة حالياً للحذف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var res = MessageBox.Show("هل أنت متأكد من رغبتك في حذف شجرة ومكونات التصنيع المحددة؟", "تأكيد الحذف",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                if (ProductionDAL.DeleteBOM(_currentBOMID))
                {
                    MessageBox.Show("✅ تم حذف شجرة التصنيع بنجاح.", "تم الحذف", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetForm();
                    LoadSavedBOMsList();
                }
            }
        }

        private void PrintBOM()
        {
            if (_selectedFinishedProductID <= 0 || dgItems.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات وصفة للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pd = new PrintDocument();
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    float y = 40;
                    var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                    var fontHeader = new Font("Segoe UI", 11.5f, FontStyle.Bold);
                    var fontBody = new Font("Segoe UI", 10f);
                    var fontBold = new Font("Segoe UI", 10f, FontStyle.Bold);

                    // ترويسة الصفحة
                    g.DrawString("بطاقة شجرة التصنيع والمعايير الفنية (BOM)", fontTitle, Brushes.DarkBlue, new PointF(200, y));
                    y += 40;

                    g.DrawString($"المنتج النهائي: {_selectedFinishedProductCode} - {_selectedFinishedProductName}", fontHeader, Brushes.Black, new PointF(40, y));
                    y += 25;
                    g.DrawString($"كمية الإنتاج المعيارية: {numOutputQty.Value} {txtUnitName.Text.Trim()} | تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm}", fontBody, Brushes.DarkSlateGray, new PointF(40, y));
                    y += 35;

                    // Table Header
                    g.FillRectangle(Brushes.LightGray, 40, y, 740, 28);
                    g.DrawRectangle(Pens.Gray, 40, y, 740, 28);
                    g.DrawString("م", fontBold, Brushes.Black, 50, y + 5);
                    g.DrawString("كود الخام", fontBold, Brushes.Black, 85, y + 5);
                    g.DrawString("اسم المادة الخام", fontBold, Brushes.Black, 190, y + 5);
                    g.DrawString("الكمية", fontBold, Brushes.Black, 450, y + 5);
                    g.DrawString("الوحدة", fontBold, Brushes.Black, 520, y + 5);
                    g.DrawString("سعر التكلفة", fontBold, Brushes.Black, 585, y + 5);
                    g.DrawString("إجمالي التكلفة", fontBold, Brushes.Black, 675, y + 5);
                    y += 28;

                    int num = 1;
                    decimal total = 0;
                    foreach (DataGridViewRow row in dgItems.Rows)
                    {
                        g.DrawRectangle(Pens.LightGray, 40, y, 740, 24);
                        g.DrawString(num++.ToString(), fontBody, Brushes.Black, 50, y + 3);
                        g.DrawString(row.Cells["RawProductCode"].Value?.ToString() ?? "", fontBody, Brushes.Black, 85, y + 3);
                        g.DrawString(row.Cells["RawProductName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 190, y + 3);
                        g.DrawString(row.Cells["Quantity"].Value?.ToString() ?? "", fontBody, Brushes.Black, 450, y + 3);
                        g.DrawString(row.Cells["UnitName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 520, y + 3);
                        g.DrawString(row.Cells["UnitCost"].Value?.ToString() ?? "", fontBody, Brushes.Black, 585, y + 3);
                        
                        string totStr = row.Cells["TotalCost"].Value?.ToString() ?? "0";
                        g.DrawString(totStr, fontBody, Brushes.Black, 675, y + 3);
                        
                        decimal.TryParse(totStr, out decimal rTot);
                        total += rTot;
                        y += 24;

                        if (y > e.MarginBounds.Bottom - 60)
                        {
                            e.HasMorePages = true;
                            return;
                        }
                    }

                    y += 10;
                    g.DrawLine(new Pen(Color.DarkBlue, 2), 40, y, 780, y);
                    y += 10;
                    decimal outQ = numOutputQty.Value > 0 ? numOutputQty.Value : 1m;
                    decimal uCost = total / outQ;

                    g.DrawString($"إجمالي تكلفة الخامات المطلوبة: {total:N2} ج.م", fontBold, Brushes.DarkBlue, new PointF(40, y));
                    g.DrawString($"تكلفة الوحدة المعيارية الواحدة: {uCost:N2} ج.م", fontBold, Brushes.DarkGreen, new PointF(440, y));
                };

                using (var ppd = new PrintPreviewDialog { Document = pd, Width = 950, Height = 700 })
                {
                    ppd.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل إعداد الطباعة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

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
        private TextBox txtEstimatedDuration;
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
            this.Size = new Size(1260, 780);
            this.MinimumSize = new Size(1080, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false;
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // ══════════════════════════════════════════════════════════════
            // الحاوية الرئيسية: محرر الوصفة (يمين مرن) + قائمة الوصفات (يسار ثابت 310px)
            // ══════════════════════════════════════════════════════════════
            var tblContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(6)
            };
            tblContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // 0: المحرر (اليمين)
            tblContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310f)); // 1: القائمة الجانبية (اليسار)
            tblContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.Controls.Add(tblContainer);

            // ──────────────────────────────────────────────────────────────
            // 1. المحرر الرئيسي للوصفة (الجانب الأيمن)
            // ──────────────────────────────────────────────────────────────
            var tblEditorLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tblEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 138f)); // Row 0: بطاقة المنتج النهائي
            tblEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118f)); // Row 1: بطاقة اختيار وإضافة المواد الخام
            tblEditorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 2: جدول بنود وخامات الوصفة
            tblEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));   // Row 3: شريط العمليات والملخص السفلي
            tblContainer.Controls.Add(tblEditorLayout, 0, 0);

            // ══════════════════════════════════════════════════════════════
            // [الجزء 1 - أعلى]: بطاقة المنتج النهائي المصنع (المعياري)
            // ══════════════════════════════════════════════════════════════
            var pnlFinishedCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 0, 0, 6)
            };
            pnlFinishedCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlFinishedCard.Width - 1, pnlFinishedCard.Height - 1);
                }
            };

            // ترويسة بطاقة المنتج النهائي
            var pnlFinishedHeader = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.Transparent };
            var lblFpTitle = new Label
            {
                Text = "🎯 بيانات المنتج النهائي المصنع (المعياري):",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            pnlFinishedHeader.Controls.Add(lblFpTitle);

            lblHeaderUnitCostBadge = new Label
            {
                Text = "🏷️ تكلفة الوحدة المصنعة: 0.00 ج.م",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(10, 3, 10, 3)
            };
            pnlFinishedHeader.Controls.Add(lblHeaderUnitCostBadge);
            pnlFinishedCard.Controls.Add(pnlFinishedHeader);

            // جدول حقول المنتج النهائي (Row 0: Labels, Row 1: Controls)
            var tblFinished = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0)
            };
            tblFinished.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f)); // 0: الصنف النهائي + بحث
            tblFinished.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f)); // 1: الكمية
            tblFinished.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11f)); // 2: الوحدة
            tblFinished.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f)); // 3: مدة التصنيع
            tblFinished.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f)); // 4: الملاحظات
            tblFinished.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));      // Row 0: Labels
            tblFinished.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));      // Row 1: Inputs

            // Labels Row
            tblFinished.Controls.Add(new Label { Text = "الصنف النهائي (كود/اسم/بحث):", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomRight }, 0, 0);
            tblFinished.Controls.Add(new Label { Text = "الكمية المعيارية:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 1, 0);
            tblFinished.Controls.Add(new Label { Text = "الوحدة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 2, 0);
            tblFinished.Controls.Add(new Label { Text = "⏱️ مدة التصنيع:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 3, 0);
            tblFinished.Controls.Add(new Label { Text = "ملاحظات الوصفة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomRight }, 4, 0);

            // Controls Row
            // 0: الصنف النهائي + زر البحث
            var pnlFpBox = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 6, 0) };
            txtFinishedProduct = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            txtFinishedProduct.KeyDown += TxtFinishedProduct_KeyDown;
            
            btnBrowseFinished = Theme.MakeButton("🔍 بحث", 0, 0, 72, 30, Theme.Primary);
            btnBrowseFinished.Dock = DockStyle.Left;
            btnBrowseFinished.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnBrowseFinished.Click += (s, e) => SelectFinishedProduct();

            pnlFpBox.Controls.Add(txtFinishedProduct);
            pnlFpBox.Controls.Add(btnBrowseFinished);
            tblFinished.Controls.Add(pnlFpBox, 0, 1);

            // 1: الكمية
            numOutputQty = new NumericUpDown
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
            numOutputQty.ValueChanged += (s, e) => RecalculateTotals();
            tblFinished.Controls.Add(numOutputQty, 1, 1);

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
            tblFinished.Controls.Add(txtUnitName, 2, 1);

            // 3: مدة التصنيع
            txtEstimatedDuration = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
            };
            tblFinished.Controls.Add(txtEstimatedDuration, 3, 1);

            // 4: الملاحظات
            txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 0),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            tblFinished.Controls.Add(txtNotes, 4, 1);

            pnlFinishedCard.Controls.Add(tblFinished);
            tblEditorLayout.Controls.Add(pnlFinishedCard, 0, 0);

            // ══════════════════════════════════════════════════════════════
            // [الجزء 2 - أوسط]: بطاقة اختيار وإضافة مواد وخامات التصنيع
            // ══════════════════════════════════════════════════════════════
            var pnlQuickAdd = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 6, 12, 6),
                Margin = new Padding(0, 0, 0, 6)
            };
            pnlQuickAdd.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlQuickAdd.Width - 1, pnlQuickAdd.Height - 1);
                }
            };

            // ترويسة اختيار المواد الخام
            var pnlRawHeader = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Color.Transparent };
            var lblRawSectionTitle = new Label
            {
                Text = "📦 اختيار وإضافة مواد وخامات التصنيع (المكونات):",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlRawHeader.Controls.Add(lblRawSectionTitle);

            var lblRawHint = new Label
            {
                Text = "💡 اضغط Enter في أي خانة لإضافة الخامة فوراً",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            pnlRawHeader.Controls.Add(lblRawHint);
            pnlQuickAdd.Controls.Add(pnlRawHeader);

            // جدول حقول المواد الخام
            var tblQuick = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 2,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };
            tblQuick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f)); // 0: المادة الخام
            tblQuick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f)); // 1: الوحدة
            tblQuick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11f)); // 2: الكمية
            tblQuick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f)); // 3: سعر التكلفة
            tblQuick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f)); // 4: إجمالي التكلفة
            tblQuick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17f)); // 5: زر الإضافة
            tblQuick.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));      // Row 0: Labels
            tblQuick.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));      // Row 1: Inputs

            // Labels
            tblQuick.Controls.Add(new Label { Text = "المادة الخام (كود/اسم/باركود):", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomRight }, 0, 0);
            tblQuick.Controls.Add(new Label { Text = "الوحدة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 1, 0);
            tblQuick.Controls.Add(new Label { Text = "الكمية المطلوبة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 2, 0);
            tblQuick.Controls.Add(new Label { Text = "سعر التكلفة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 3, 0);
            tblQuick.Controls.Add(new Label { Text = "إجمالي التكلفة:", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), TextAlign = ContentAlignment.BottomCenter }, 4, 0);
            tblQuick.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 5, 0);

            // Controls
            // 0: المادة الخام + زر البحث
            var pnlRawBox = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 6, 0) };
            txtRawProduct = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            txtRawProduct.KeyDown += TxtRawProduct_KeyDown;
            
            btnBrowseRaw = Theme.MakeButton("🔍 خامات", 0, 0, 75, 30, Color.FromArgb(71, 85, 105));
            btnBrowseRaw.Dock = DockStyle.Left;
            btnBrowseRaw.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnBrowseRaw.Click += (s, e) => SelectRawProduct("", autoAddToGrid: true);

            pnlRawBox.Controls.Add(txtRawProduct);
            pnlRawBox.Controls.Add(btnBrowseRaw);
            tblQuick.Controls.Add(pnlRawBox, 0, 1);

            // 1: الوحدة
            txtRawUnit = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                Text = "قطعة",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black,
                TextAlign = HorizontalAlignment.Center
            };
            tblQuick.Controls.Add(txtRawUnit, 1, 1);

            // 2: الكمية
            numRawQty = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                DecimalPlaces = 3,
                Minimum = 0.001m,
                Maximum = 1000000m,
                Value = 1m,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(2, 132, 199),
                TextAlign = HorizontalAlignment.Center
            };
            numRawQty.ValueChanged += (s, e) => UpdateRawTotalPreview();
            numRawQty.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddCurrentRawToGrid(); } };
            tblQuick.Controls.Add(numRawQty, 2, 1);

            // 3: سعر التكلفة
            numRawCostPrice = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                DecimalPlaces = 2,
                Minimum = 0m,
                Maximum = 1000000m,
                Value = 0m,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(180, 83, 9),
                TextAlign = HorizontalAlignment.Center
            };
            numRawCostPrice.ValueChanged += (s, e) => UpdateRawTotalPreview();
            numRawCostPrice.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddCurrentRawToGrid(); } };
            tblQuick.Controls.Add(numRawCostPrice, 3, 1);

            // 4: إجمالي تكلفة البند
            lblRawTotalPreview = new Label
            {
                Text = "0.00 ج.م",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),
                BackColor = Color.FromArgb(254, 243, 199),
                TextAlign = ContentAlignment.MiddleCenter
            };
            tblQuick.Controls.Add(lblRawTotalPreview, 4, 1);

            // 5: زر الإضافة الكبير
            btnAddRaw = Theme.MakeButton("➕ إضافة للشجرة", 0, 0, 130, 32, Color.FromArgb(16, 185, 129));
            btnAddRaw.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnAddRaw.Dock = DockStyle.Fill;
            btnAddRaw.Margin = new Padding(0, 2, 0, 0);
            btnAddRaw.Click += (s, e) => AddCurrentRawToGrid();
            tblQuick.Controls.Add(btnAddRaw, 5, 1);

            pnlQuickAdd.Controls.Add(tblQuick);
            tblEditorLayout.Controls.Add(pnlQuickAdd, 0, 1);

            // ══════════════════════════════════════════════════════════════
            // [الجزء 3 - رئيسي]: جدول بنود وخامات الوصفة (DataGrid)
            // ══════════════════════════════════════════════════════════════
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
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
            tblEditorLayout.Controls.Add(dgItems, 0, 2);

            // ══════════════════════════════════════════════════════════════
            // [الجزء 4 - أسفل]: شريط العمليات والملخص السفلي
            // ══════════════════════════════════════════════════════════════
            var tblBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.White,
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(0)
            };
            tblBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f)); // Column 0 (Right): الأزرار
            tblBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f)); // Column 1 (Left): الملخص المالي
            tblBottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblBottom.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, tblBottom.Width - 1, tblBottom.Height - 1);
                }
            };

            // Column 0: الأزرار
            var pnlActionsRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            tblBottom.Controls.Add(pnlActionsRight, 0, 0);

            btnSave = Theme.MakeButton("💾 حفظ الشجرة", 0, 0, 130, 36, Color.FromArgb(16, 185, 129));
            btnSave.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSave.Margin = new Padding(0, 2, 6, 0);
            btnSave.Click += (s, e) => SaveCurrentBOM();
            pnlActionsRight.Controls.Add(btnSave);

            btnNew = Theme.MakeButton("➕ وصفة جديدة", 0, 0, 115, 36, Color.FromArgb(71, 85, 105));
            btnNew.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnNew.Margin = new Padding(0, 2, 6, 0);
            btnNew.Click += (s, e) => ResetForm();
            pnlActionsRight.Controls.Add(btnNew);

            btnPrint = Theme.MakeButton("🖨️ طباعة", 0, 0, 85, 36, Color.FromArgb(2, 132, 199));
            btnPrint.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnPrint.Margin = new Padding(0, 2, 6, 0);
            btnPrint.Click += (s, e) => PrintBOM();
            pnlActionsRight.Controls.Add(btnPrint);

            btnDelete = Theme.MakeButton("🗑️ حذف", 0, 0, 80, 36, Color.FromArgb(239, 68, 68));
            btnDelete.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnDelete.Margin = new Padding(0, 2, 0, 0);
            btnDelete.Click += (s, e) => DeleteCurrentBOM();
            pnlActionsRight.Controls.Add(btnDelete);

            // Column 1: الملخص المالي
            var pnlSummaryLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            tblBottom.Controls.Add(pnlSummaryLeft, 1, 0);

            lblUnitCost = CreateBadge("🏷️ تكلفة الوحدة: 0.00 ج.م", Color.FromArgb(16, 185, 129), Color.FromArgb(236, 253, 245));
            pnlSummaryLeft.Controls.Add(lblUnitCost);

            lblTotalRawCost = CreateBadge("💰 الخامات: 0.00 ج.م", Color.FromArgb(217, 119, 6), Color.FromArgb(254, 243, 199));
            pnlSummaryLeft.Controls.Add(lblTotalRawCost);

            lblItemsCount = CreateBadge("📦 الأصناف: 0", Color.FromArgb(71, 85, 105), Color.FromArgb(241, 245, 249));
            pnlSummaryLeft.Controls.Add(lblItemsCount);

            tblEditorLayout.Controls.Add(tblBottom, 0, 3);

            // ──────────────────────────────────────────────────────────────
            // 2. القائمة الجانبية (الوصفات وشجر الإنتاج المسجلة - على اليسار)
            // ──────────────────────────────────────────────────────────────
            var pnlSidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(8),
                Margin = new Padding(6, 0, 0, 0)
            };
            pnlSidebar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlSidebar.Width - 1, pnlSidebar.Height - 1);
                }
            };
            tblContainer.Controls.Add(pnlSidebar, 1, 0);

            var pnlSideTop = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
            
            var lblSideTitle = new Label
            {
                Text = "📋 شجر الإنتاج والوصفات المسجلة",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            pnlSideTop.Controls.Add(lblSideTitle);

            txtSearchBOM = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.Black
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
                ForeColor = Color.FromArgb(100, 116, 139),
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
                RowTemplate = { Height = 34 },
                Margin = new Padding(0, 8, 0, 8),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 34
            };
            dgBOMList.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgBOMList.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = Theme.FontMain,
                SelectionBackColor = Color.FromArgb(224, 242, 254),
                SelectionForeColor = Color.Black
            };
            Theme.EnableDoubleBuffer(dgBOMList);

            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "BOMID", Visible = false });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الكود", FillWeight = 18, MinimumWidth = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "المنتج النهائي", FillWeight = 42, MinimumWidth = 125, DefaultCellStyle = { Font = new Font("Segoe UI", 9f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemsCount", HeaderText = "الخامات", FillWeight = 16, MinimumWidth = 55, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalEstCost", HeaderText = "التكلفة", FillWeight = 24, MinimumWidth = 75, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(217, 119, 6) } });

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
                           COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.ItemID DESC), 0) AS CostPrice,
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
                               COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.ItemID DESC), 0) AS CostPrice, 
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
                    row.Cells["Quantity"].Value = (cur + qty).ToString("N3");
                    row.Cells["UnitCost"].Value = cost.ToString("N2");
                    UpdateRowTotal(row.Index);
                    RecalculateTotals();
                    ClearRawInputs();
                    dgItems.ClearSelection();
                    row.Selected = true;
                    dgItems.FirstDisplayedScrollingRowIndex = row.Index;
                    txtRawProduct.Focus();
                    return;
                }
            }

            decimal tot = qty * cost;
            int newIdx = dgItems.Rows.Add(
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

            if (newIdx >= 0 && newIdx < dgItems.Rows.Count)
            {
                dgItems.ClearSelection();
                dgItems.Rows[newIdx].Selected = true;
                dgItems.FirstDisplayedScrollingRowIndex = newIdx;
            }

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
                               COALESCE(NULLIF(CostPrice, 0), NULLIF(PurchasePrice, 0), (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 WHERE pi2.ProductID = Products.ProductID ORDER BY pi2.ItemID DESC), 0) AS CostPrice,
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
                EstimatedDuration = txtEstimatedDuration != null ? txtEstimatedDuration.Text.Trim() : "",
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
            if (txtEstimatedDuration != null) txtEstimatedDuration.Text = bom.EstimatedDuration ?? "";
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
            if (txtEstimatedDuration != null) txtEstimatedDuration.Clear();
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

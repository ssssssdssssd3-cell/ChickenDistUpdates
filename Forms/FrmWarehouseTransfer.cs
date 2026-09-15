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
    /// <summary>عنصر وحدة التحويل المخزني مع معامل التحويل والمستوى</summary>
    public class TransferUnitOption
    {
        public string UnitName { get; set; } = "";
        public decimal Factor { get; set; } = 1.0m;
        public int Level { get; set; } = 1; // 1 = صغرى, 2 = وسطى, 3 = كبرى
        public string BaseUnitName { get; set; } = "";

        public override string ToString()
        {
            if (Factor > 1m && !string.IsNullOrWhiteSpace(BaseUnitName) && BaseUnitName != UnitName)
                return $"{UnitName} ({Factor:G29} {BaseUnitName})";
            return UnitName;
        }
    }

    /// <summary>شاشة التحويل المخزني بين المستودعات - تصميم متطور ومتجاوب</summary>
    public class FrmWarehouseTransfer : Form
    {
        private ComboBox cboFromWarehouse, cboToWarehouse, cboUnit;
        private NumericUpDown nudQty;
        private Label lblAvailableStock;
        private TextBox txtNotes, txtBarcodeTransfer, txtSelectedProduct;
        private Button btnSearchProduct, btnAddItem, btnSave, btnSaveAndPrint, btnNew, btnTransfersHistory;
        private DataGridView dgItems;
        private Label lblCountBadge, lblTotalQtyBadge;
        private List<TransferItemDTO> _items = new List<TransferItemDTO>();
        private Dictionary<int, List<TransferUnitOption>> _productUnitsCache = new Dictionary<int, List<TransferUnitOption>>();
        private bool _isRefreshingGrid = false;

        private int _selectedProductID = 0;
        private string _selectedProductCode = "";
        private string _selectedProductName = "";
        private decimal _selectedProductStock = 0m; // الرصيد الخام في المستودع بالوحدة الصغرى

        public FrmWarehouseTransfer()
        {
            InitUI();
            LoadWarehouses();
        }

        private void InitUI()
        {
            this.Text = "تحويل المخزون بين المستودعات";
            this.Size = new Size(1150, 720);
            this.MinimumSize = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;

            // اختصارات لوحة المفاتيح السريعة
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F5) { BtnSave_Click(false); e.Handled = true; }
                else if (e.KeyCode == Keys.F2) { ClearForm(); e.Handled = true; }
                else if (e.KeyCode == Keys.F3) { OpenProductSearch(); e.Handled = true; }
                else if (e.KeyCode == Keys.F4) { OpenTransfersList(); e.Handled = true; }
            };

            // ── الحاوية العلوية (Top Container) ───────────────────────────────
            var pnlTopContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 230,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 6, 12, 6)
            };


            // 1. شريط العنوان والأزرار السريعة
            var pnlTitleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 4)
            };

            var lblTitle = new Label
            {
                Text = "🔄  تحويل المخزون بين المستودعات والمستودعات الفرعية",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Dock = DockStyle.Right,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            btnTransfersHistory = new Button
            {
                Text = "📋 سجل التحويلات السابقة [F4]",
                Dock = DockStyle.Left,
                Width = 205,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 55, 75),
                ForeColor = Color.FromArgb(220, 235, 255),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTransfersHistory.FlatAppearance.BorderColor = Color.FromArgb(65, 85, 115);
            btnTransfersHistory.Click += (s, e) => OpenTransfersList();

            var lblShortcutsHint = new Label
            {
                Text = "💡 اختصارات سريعة:  [F3] بحث أصناف  |  [Enter] إضافة  |  [F5] حفظ  |  [F2] جديد",
                Dock = DockStyle.Fill,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlTitleBar.Controls.Add(lblShortcutsHint);
            pnlTitleBar.Controls.Add(btnTransfersHistory);
            pnlTitleBar.Controls.Add(lblTitle);


            // 2. بطاقة مسار وبيانات التحويل (Route Card)
            var pnlRouteCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(28, 38, 55),
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 4, 0, 4)
            };
            pnlRouteCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(45, 60, 85), 1.2f);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlRouteCard.Width - 1, pnlRouteCard.Height - 1);
            };

            var tblRoute = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            tblRoute.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // 0: تسمية من
            tblRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190f));       // 1: كومبو من
            tblRoute.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // 2: سهم الاتجاه
            tblRoute.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // 3: تسمية إلى
            tblRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190f));       // 4: كومبو إلى
            tblRoute.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // 5: تسمية ملاحظات
            tblRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));         // 6: خانة الملاحظات

            var lblFrom = new Label
            {
                Text = "📤 من مستودع (المصدر):",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 245, 255),
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight
            };

            cboFromWarehouse = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill
            };
            cboFromWarehouse.SelectedIndexChanged += CboWarehouse_Changed;

            var lblArrow = new Label
            {
                Text = "  ⬅️ تحويل إلى ⬅️  ",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblTo = new Label
            {
                Text = "📥 إلى مستودع (الوجهة):",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 245, 255),
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight
            };

            cboToWarehouse = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill
            };

            var lblNotes = new Label
            {
                Text = "📝 ملاحظات التحويل:",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(200, 215, 235),
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight
            };

            txtNotes = new TextBox
            {
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };

            tblRoute.Controls.Add(lblFrom, 0, 0);
            tblRoute.Controls.Add(cboFromWarehouse, 1, 0);
            tblRoute.Controls.Add(lblArrow, 2, 0);
            tblRoute.Controls.Add(lblTo, 3, 0);
            tblRoute.Controls.Add(cboToWarehouse, 4, 0);
            tblRoute.Controls.Add(lblNotes, 5, 0);
            tblRoute.Controls.Add(txtNotes, 6, 0);
            pnlRouteCard.Controls.Add(tblRoute);


            // 3. شريط الإدخال السريع للأصناف (Fast Entry Strip)
            var pnlFastEntryCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 94,
                BackColor = Color.FromArgb(22, 30, 45),
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(0, 4, 0, 0)
            };
            pnlFastEntryCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.BorderSearchPanel, 1.2f);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlFastEntryCard.Width - 1, pnlFastEntryCard.Height - 1);
            };

            var tblFastEntry = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            tblFastEntry.RowStyles.Clear();
            tblFastEntry.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFastEntry.ColumnStyles.Clear();
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));      // 0: الاسكنر
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));      // 1: زر بحث F3
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));       // 2: اسم الصنف المختار (مرن يملأ الشاشة)
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175f));      // 3: شارة الرصيد المتاح
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));       // 4: خانة الكمية
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));      // 5: كومبو الوحدة
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145f));      // 6: زر الإضافة

            // 0: حاوية الاسكنر مع عنوانه
            var pnlScanner = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblScannerTitle = new Label
            {
                Text = "📷 الباركود:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 100),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            txtBarcodeTransfer = new TextBox
            {
                Name = "txtBarcodeTransfer",
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtBarcodeTransfer.KeyDown += TxtBarcodeTransfer_KeyDown;
            pnlScanner.Controls.Add(txtBarcodeTransfer);
            pnlScanner.Controls.Add(lblScannerTitle);

            // 1: زر بحث الأصناف [F3]
            var pnlSearchBtn = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblSearchTitle = new Label
            {
                Text = "🔍 بحث الأصناف:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 100),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnSearchProduct = Theme.MakeButton("🔍 بحث [F3]", Theme.Primary);
            btnSearchProduct.Dock = DockStyle.Bottom;
            btnSearchProduct.Height = 35;
            btnSearchProduct.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSearchProduct.Click += (s, e) => OpenProductSearch();
            pnlSearchBtn.Controls.Add(btnSearchProduct);
            pnlSearchBtn.Controls.Add(lblSearchTitle);

            // 2: حاوية الصنف المختار
            var pnlSelectedProd = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblSelectedTitle = new Label
            {
                Text = "📦 الصنف المحدد (انقر للاختيار):",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(200, 220, 245),
                Dock = DockStyle.Top,
                Height = 20
            };
            txtSelectedProduct = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                ReadOnly = true,
                BackColor = Color.FromArgb(32, 44, 62),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Text = "اضغط [F3] أو امسح الباركود لاختيار صنف...",
                Cursor = Cursors.Hand
            };
            txtSelectedProduct.Click += (s, e) => OpenProductSearch();
            pnlSelectedProd.Controls.Add(txtSelectedProduct);
            pnlSelectedProd.Controls.Add(lblSelectedTitle);

            // 3: شارة الرصيد المتاح بالمصدر
            var pnlStockBadge = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblStockTitle = new Label
            {
                Text = "المتاح بالمصدر:",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(170, 185, 205),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblAvailableStock = new Label
            {
                Text = "متاح: --",
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(35, 48, 68),
                ForeColor = Color.FromArgb(160, 175, 195),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlStockBadge.Controls.Add(lblAvailableStock);
            pnlStockBadge.Controls.Add(lblStockTitle);

            // 4: حاوية الكمية
            var pnlQty = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblQtyTitle = new Label
            {
                Text = "الكمية:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 100),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            nudQty = new NumericUpDown
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                DecimalPlaces = 3,
                Minimum = 0.001m,
                Maximum = 999999m,
                Value = 1m,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            nudQty.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnAddItem_Click(null, null);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            pnlQty.Controls.Add(nudQty);
            pnlQty.Controls.Add(lblQtyTitle);

            // 5: حاوية الوحدة
            var pnlUnit = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblUnitTitle = new Label
            {
                Text = "الوحدة المحولة:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 100),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            cboUnit = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            cboUnit.SelectedIndexChanged += (s, e) => UpdateAvailableStockDisplay();
            pnlUnit.Controls.Add(cboUnit);
            pnlUnit.Controls.Add(lblUnitTitle);

            // 6: زر الإضافة
            var pnlAddBtn = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblAddTitle = new Label
            {
                Text = "إضافة الصنف:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 100),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnAddItem = Theme.MakeButton("➕ إضافة [Enter]", Theme.Accent);
            btnAddItem.Dock = DockStyle.Bottom;
            btnAddItem.Height = 35;
            btnAddItem.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnAddItem.Click += BtnAddItem_Click;
            pnlAddBtn.Controls.Add(btnAddItem);
            pnlAddBtn.Controls.Add(lblAddTitle);

            tblFastEntry.Controls.Add(pnlScanner, 0, 0);
            tblFastEntry.Controls.Add(pnlSearchBtn, 1, 0);
            tblFastEntry.Controls.Add(pnlSelectedProd, 2, 0);
            tblFastEntry.Controls.Add(pnlStockBadge, 3, 0);
            tblFastEntry.Controls.Add(pnlQty, 4, 0);
            tblFastEntry.Controls.Add(pnlUnit, 5, 0);
            tblFastEntry.Controls.Add(pnlAddBtn, 6, 0);

            pnlFastEntryCard.Controls.Add(tblFastEntry);

            // إضافة الألواح الثلاثة بترتيب Z-Order سليم من الأعلى للأسفل (TitleBar -> RouteCard -> FastEntryCard)
            pnlTopContainer.Controls.Add(pnlFastEntryCard);
            pnlTopContainer.Controls.Add(pnlRouteCard);
            pnlTopContainer.Controls.Add(pnlTitleBar);


            // ── جدول الأصناف المحولة (DataGrid) ──────────────────────────────
            var pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Theme.BgMain
            };

            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(50, 60, 75),
                RowTemplate = { Height = 36 },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Color.FromArgb(13, 110, 253),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(20, 35, 60),
                    ForeColor = Color.FromArgb(255, 220, 110),
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                ColumnHeadersHeight = 40,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // تعريف أعمدة الجدول
            var colIndex = new DataGridViewTextBoxColumn
            {
                Name = "RowIndex",
                HeaderText = "#",
                FillWeight = 25,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            var colPid = new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false };

            var colCode = new DataGridViewTextBoxColumn
            {
                Name = "ProductCode",
                HeaderText = "كود الصنف",
                FillWeight = 55,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            };

            var colName = new DataGridViewTextBoxColumn
            {
                Name = "ProductName",
                HeaderText = "اسم الصنف",
                FillWeight = 190,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                }
            };

            var colUnit = new DataGridViewComboBoxColumn
            {
                Name = "Unit",
                HeaderText = "الوحدة (✏️ تعديل)",
                FillWeight = 65,
                ReadOnly = false,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(30, 42, 60),
                    ForeColor = Color.FromArgb(140, 215, 255),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            };

            var colFactor = new DataGridViewTextBoxColumn
            {
                Name = "Factor",
                HeaderText = "المعامل",
                FillWeight = 35,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(200, 215, 235),
                    Font = new Font("Segoe UI", 9f)
                }
            };

            var colAvail = new DataGridViewTextBoxColumn
            {
                Name = "AvailableStock",
                HeaderText = "المتاح بالوحدة",
                FillWeight = 60,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(80, 210, 130),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                }
            };

            var colQty = new DataGridViewTextBoxColumn
            {
                Name = "Quantity",
                HeaderText = "الكمية المحولة (✏️ تعديل)",
                FillWeight = 70,
                ReadOnly = false, // متاح للتعديل المباشر في الخلية!
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(255, 215, 80),
                    BackColor = Color.FromArgb(30, 42, 60),
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold)
                }
            };

            var colTotalBase = new DataGridViewTextBoxColumn
            {
                Name = "TotalBaseQty",
                HeaderText = "إجمالي بالصغرى",
                FillWeight = 55,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(140, 200, 255),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            };

            var colDel = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "حذف",
                Text = "🗑️ حذف",
                UseColumnTextForButtonValue = true,
                FillWeight = 35,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(240, 80, 80),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            };

            dgItems.Columns.AddRange(new DataGridViewColumn[] { colIndex, colPid, colCode, colName, colUnit, colFactor, colAvail, colQty, colTotalBase, colDel });
            dgItems.DataError += (s, e) => { e.Cancel = true; e.ThrowException = false; };
            dgItems.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgItems.IsCurrentCellDirty && dgItems.CurrentCell is DataGridViewComboBoxCell)
                {
                    dgItems.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgItems.EditingControlShowing += DgItems_EditingControlShowing;
            dgItems.CellValueChanged += DgItems_CellValueChanged;
            dgItems.CellClick += DgItems_CellClick;
            dgItems.CellEndEdit += DgItems_CellEndEdit;
            dgItems.KeyDown += DgItems_KeyDown;
            pnlGridContainer.Controls.Add(dgItems);

            // ── شريط الإحصائيات والإجراءات السفلي (Footer) ────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 10, 12, 10)
            };

            // شارات الإحصائيات الفورية على اليمين (Right side)
            var pnlStats = new Panel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            lblCountBadge = new Label
            {
                Text = "🏷️  البنود المحولة: 0 صنف",
                Dock = DockStyle.Right,
                AutoSize = true,
                Height = 42,
                BackColor = Color.FromArgb(30, 42, 60),
                ForeColor = Color.FromArgb(225, 235, 250),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(14, 0, 14, 0),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblTotalQtyBadge = new Label
            {
                Text = "⚖️  إجمالي الكميات: 0.000",
                Dock = DockStyle.Right,
                AutoSize = true,
                Height = 42,
                BackColor = Color.FromArgb(25, 50, 40),
                ForeColor = Color.FromArgb(100, 240, 160),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(16, 0, 16, 0),
                Margin = new Padding(8, 0, 0, 0),
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlStats.Controls.Add(lblTotalQtyBadge);
            pnlStats.Controls.Add(lblCountBadge);

            // أزرار الحفظ والإجراءات على اليسار (Left side)
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false
            };

            btnSave = Theme.MakeButton("💾  حفظ التحويل [F5]", Theme.Accent);
            btnSave.Size = new Size(195, 44);
            btnSave.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnSave.Click += (s, e) => BtnSave_Click(false);

            btnSaveAndPrint = Theme.MakeButton("🖨️  حفظ وطباعة الإذن", Color.FromArgb(25, 135, 84));
            btnSaveAndPrint.Size = new Size(185, 44);
            btnSaveAndPrint.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSaveAndPrint.Click += (s, e) => BtnSave_Click(true);

            btnNew = Theme.MakeButton("🆕  تحويل جديد [F2]", Color.FromArgb(60, 70, 85));
            btnNew.Size = new Size(150, 44);
            btnNew.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnNew.Click += (s, e) => ClearForm();

            pnlActions.Controls.Add(btnSave);
            pnlActions.Controls.Add(btnSaveAndPrint);
            pnlActions.Controls.Add(btnNew);

            pnlFooter.Controls.Add(pnlStats);
            pnlFooter.Controls.Add(pnlActions);

            // ── تجميع واجهة النموذج ─────────────────────────────────────────
            this.Controls.Add(pnlGridContainer);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlTopContainer);

            Theme.ApplyFormRTL(this);
        }

        private void LoadWarehouses()
        {
            var dt = WarehouseDAL.GetAll(true);
            cboFromWarehouse.Items.Clear();
            cboToWarehouse.Items.Clear();
            cboFromWarehouse.Items.Add(new ComboItem(0, "-- اختر مستودع المصدر --"));
            cboToWarehouse.Items.Add(new ComboItem(0, "-- اختر مستودع الوجهة --"));

            foreach (DataRow r in dt.Rows)
            {
                int wid = Convert.ToInt32(r["WarehouseID"]);
                string wname = r["WarehouseName"].ToString();
                cboFromWarehouse.Items.Add(new ComboItem(wid, wname));
                cboToWarehouse.Items.Add(new ComboItem(wid, wname));
            }

            cboFromWarehouse.DisplayMember = "Text";
            cboToWarehouse.DisplayMember = "Text";
            cboFromWarehouse.SelectedIndex = 0;
            cboToWarehouse.SelectedIndex = 0;
        }

        private void CboWarehouse_Changed(object sender, EventArgs e)
        {
            if (_items.Count > 0)
            {
                if (MessageBox.Show("تنبيه: تغيير مستودع المصدر سيؤدي إلى تفريغ الأصناف الحالية لإعادة فحص أرصدتها بالمستودع الجديد.\nهل تريد المتابعة؟", "تغيير المستودع", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _items.Clear();
                    RefreshGrid();
                }
            }

            if (_selectedProductID > 0 && cboFromWarehouse.SelectedItem is ComboItem wh && wh.ID > 0)
            {
                _selectedProductStock = InventoryDAL.GetProductStock(_selectedProductID, wh.ID);
            }
            else
            {
                _selectedProductStock = 0m;
            }
            UpdateAvailableStockDisplay();
        }

        private void OpenProductSearch()
        {
            if (!Session.CanAccess("ProductSearch"))
            {
                MessageBox.Show("عفواً، ليس لديك صلاحية استخدام شاشة بحث الأصناف السريعة.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(cboFromWarehouse.SelectedItem is ComboItem wh) || wh.ID <= 0)
            {
                MessageBox.Show("⚠️ يرجى اختيار مستودع المصدر أولاً لعرض أرصدة الأصناف المتوفرة به بدقة!", "تحديد مستودع المصدر مطلوب", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboFromWarehouse.Focus();
                return;
            }

            using var frm = new FrmProductSearch(warehouseID: wh.ID, isPurchaseMode: false, defaultShowZeroStock: false);
            if (frm.ShowDialog(this) == DialogResult.OK && frm.SelectedProductID > 0)
            {
                decimal qty = frm.SelectedQuantity > 0 ? frm.SelectedQuantity : 1m;
                string preferredUnit = frm.SelectedUnitName;

                // ✅ تحديد الصنف مع الوحدة المختارة
                SelectProductByID(frm.SelectedProductID, wh.ID, qty, preferredUnitName: preferredUnit);
                BtnAddItem_Click(null, null);
            }
        }

        private List<TransferUnitOption> GetProductUnits(int productID)
        {
            if (_productUnitsCache.TryGetValue(productID, out var cached) && cached != null && cached.Count > 0)
                return cached;

            var list = new List<TransferUnitOption>();
            var dt = DbHelper.Query(@"
                SELECT Unit, Unit1Name, Unit2Name, Unit2Factor, Unit3Factor 
                FROM Products 
                WHERE ProductID = @id", DbHelper.P("@id", productID));

            if (dt != null && dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                string majorUnit = row["Unit"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(majorUnit)) majorUnit = "قطعة";

                string unit1 = row.Table.Columns.Contains("Unit1Name") && row["Unit1Name"] != DBNull.Value ? row["Unit1Name"].ToString().Trim() : "";
                string unit2 = row.Table.Columns.Contains("Unit2Name") && row["Unit2Name"] != DBNull.Value ? row["Unit2Name"].ToString().Trim() : "";

                decimal u2f = 1m;
                if (row.Table.Columns.Contains("Unit2Factor") && row["Unit2Factor"] != DBNull.Value && decimal.TryParse(row["Unit2Factor"].ToString(), out decimal parsedU2) && parsedU2 > 0)
                    u2f = parsedU2;

                decimal u3f = 1m;
                if (row.Table.Columns.Contains("Unit3Factor") && row["Unit3Factor"] != DBNull.Value && decimal.TryParse(row["Unit3Factor"].ToString(), out decimal parsedU3) && parsedU3 > 0)
                    u3f = parsedU3;

                decimal majorFactor = u2f * u3f;
                string baseUnitName = !string.IsNullOrWhiteSpace(unit1) ? unit1 : majorUnit;

                // 1. الوحدة الكبرى
                list.Add(new TransferUnitOption
                {
                    UnitName = majorUnit,
                    Factor = majorFactor,
                    Level = 3,
                    BaseUnitName = baseUnitName
                });

                // 2. الوحدة الوسطى إن وجدت
                if (!string.IsNullOrWhiteSpace(unit2) && !string.Equals(unit2, majorUnit, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(new TransferUnitOption
                    {
                        UnitName = unit2,
                        Factor = u2f,
                        Level = 2,
                        BaseUnitName = baseUnitName
                    });
                }

                // 3. الوحدة الصغرى إن وجدت ومختلفة
                if (!string.IsNullOrWhiteSpace(unit1) && !string.Equals(unit1, majorUnit, StringComparison.OrdinalIgnoreCase) && !string.Equals(unit1, unit2, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(new TransferUnitOption
                    {
                        UnitName = unit1,
                        Factor = 1.0m,
                        Level = 1,
                        BaseUnitName = baseUnitName
                    });
                }
                else if (majorFactor > 1m && list.Count == 1)
                {
                    list.Add(new TransferUnitOption
                    {
                        UnitName = "قطعة",
                        Factor = 1.0m,
                        Level = 1,
                        BaseUnitName = "قطعة"
                    });
                }
            }

            _productUnitsCache[productID] = list;
            return list;
        }

        private void SelectProductByID(int productID, int warehouseID, decimal initialQty = 1m, string preferredUnitName = null, int? matchedUnitLevel = null)
        {
            var dt = DbHelper.Query(@"
                SELECT ProductID, ProductCode, ProductName, Unit, 
                       Unit1Name, Unit2Name, Unit2Factor, Unit3Factor, DefaultSaleUnit 
                FROM Products 
                WHERE ProductID=@id", DbHelper.P("@id", productID));

            if (dt != null && dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                _selectedProductID = Convert.ToInt32(row["ProductID"]);
                _selectedProductCode = row["ProductCode"]?.ToString() ?? "";
                _selectedProductName = row["ProductName"]?.ToString() ?? "";

                txtSelectedProduct.Text = $"{_selectedProductCode} - {_selectedProductName}";

                var unitOptions = GetProductUnits(_selectedProductID);
                cboUnit.Items.Clear();
                foreach (var opt in unitOptions)
                {
                    cboUnit.Items.Add(opt);
                }

                var optMajor = unitOptions.Find(u => u.Level == 3);
                var optMiddle = unitOptions.Find(u => u.Level == 2);
                var optMinor = unitOptions.Find(u => u.Level == 1);

                // تحديد الوحدة الافتراضية المناسبة
                int selectedIdx = -1;
                if (!string.IsNullOrWhiteSpace(preferredUnitName))
                {
                    for (int i = 0; i < cboUnit.Items.Count; i++)
                    {
                        if (cboUnit.Items[i] is TransferUnitOption opt && string.Equals(opt.UnitName, preferredUnitName, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedIdx = i;
                            break;
                        }
                    }
                }

                if (selectedIdx < 0 && matchedUnitLevel.HasValue)
                {
                    for (int i = 0; i < cboUnit.Items.Count; i++)
                    {
                        if (cboUnit.Items[i] is TransferUnitOption opt && opt.Level == matchedUnitLevel.Value)
                        {
                            selectedIdx = i;
                            break;
                        }
                    }
                }

                if (selectedIdx < 0)
                {
                    string defUnit = row.Table.Columns.Contains("DefaultSaleUnit") && row["DefaultSaleUnit"] != DBNull.Value ? row["DefaultSaleUnit"].ToString().Trim() : "";
                    if (defUnit == "الصغرى" && optMinor != null)
                        selectedIdx = cboUnit.Items.IndexOf(optMinor);
                    else if (defUnit == "الوسطى" && optMiddle != null)
                        selectedIdx = cboUnit.Items.IndexOf(optMiddle);
                    else
                        selectedIdx = 0; // الكبرى
                }

                if (selectedIdx >= 0 && selectedIdx < cboUnit.Items.Count)
                    cboUnit.SelectedIndex = selectedIdx;
                else if (cboUnit.Items.Count > 0)
                    cboUnit.SelectedIndex = 0;

                // رصيد المستودع الحالي بالوحدة الصغرى
                _selectedProductStock = InventoryDAL.GetProductStock(_selectedProductID, warehouseID);
                UpdateAvailableStockDisplay(initialQty);
            }
        }

        private void UpdateAvailableStockDisplay(decimal? suggestQtyParam = null)
        {
            if (_selectedProductID <= 0 || !(cboFromWarehouse.SelectedItem is ComboItem wh) || wh.ID <= 0)
            {
                lblAvailableStock.Text = "متاح: --";
                lblAvailableStock.BackColor = Color.FromArgb(35, 48, 68);
                lblAvailableStock.ForeColor = Color.FromArgb(160, 175, 195);
                return;
            }

            var opt = cboUnit.SelectedItem as TransferUnitOption;
            decimal factor = opt != null && opt.Factor > 0 ? opt.Factor : 1.0m;
            string unitName = opt != null ? opt.UnitName : "قطعة";

            decimal availInUnit = factor > 0 ? (_selectedProductStock / factor) : _selectedProductStock;

            if (factor > 1m && !string.IsNullOrWhiteSpace(opt?.BaseUnitName) && opt.BaseUnitName != unitName)
            {
                lblAvailableStock.Text = $"متاح: {availInUnit:G29} {unitName}\n({_selectedProductStock:G29} {opt.BaseUnitName})";
            }
            else
            {
                lblAvailableStock.Text = $"متاح: {availInUnit:G29} {unitName}";
            }

            if (_selectedProductStock > 0)
            {
                lblAvailableStock.BackColor = Color.FromArgb(20, 60, 40);
                lblAvailableStock.ForeColor = Color.FromArgb(90, 240, 150);
            }
            else
            {
                lblAvailableStock.BackColor = Color.FromArgb(70, 25, 25);
                lblAvailableStock.ForeColor = Color.FromArgb(255, 120, 120);
            }

            if (suggestQtyParam.HasValue)
            {
                decimal suggestQty = availInUnit > 0 ? Math.Min(suggestQtyParam.Value, availInUnit) : suggestQtyParam.Value;
                nudQty.Value = suggestQty > 0 ? suggestQty : 1m;
                nudQty.Focus();
                nudQty.Select(0, nudQty.Text.Length);
            }
        }

        private void TxtBarcodeTransfer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string barcode = txtBarcodeTransfer.Text.Trim();
                if (string.IsNullOrEmpty(barcode)) return;

                if (!(cboFromWarehouse.SelectedItem is ComboItem wh) || wh.ID <= 0)
                {
                    MessageBox.Show("⚠️ يرجى اختيار مستودع المصدر أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboFromWarehouse.Focus();
                    return;
                }

                var dt = ProductDAL.FindByCode(barcode);
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    int pid = Convert.ToInt32(row["ProductID"]);
                    int matchedUnit = dt.Columns.Contains("MatchedUnit") && row["MatchedUnit"] != DBNull.Value ? Convert.ToInt32(row["MatchedUnit"]) : 3;

                    SelectProductByID(pid, wh.ID, 1m, matchedUnitLevel: matchedUnit);
                    BtnAddItem_Click(null, null);
                }
                else
                {
                    MessageBox.Show($"لم يتم العثور على صنف بهذا الكود: {barcode}", "صنف غير موجود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                txtBarcodeTransfer.Clear();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (_selectedProductID <= 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً بالضغط على زر [بحث الأصناف] أو مسح الباركود!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                OpenProductSearch();
                return;
            }

            if (!(cboFromWarehouse.SelectedItem is ComboItem wh) || wh.ID <= 0)
            {
                MessageBox.Show("اختر مستودع المصدر أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboFromWarehouse.Focus();
                return;
            }

            decimal qty = nudQty.Value;
            if (qty <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية صحيحة أكبر من الصفر!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nudQty.Focus();
                return;
            }

            var opt = cboUnit.SelectedItem as TransferUnitOption;
            string unit = opt != null ? opt.UnitName : "قطعة";
            decimal factor = opt != null && opt.Factor > 0 ? opt.Factor : 1.0m;

            AddItemToGrid(_selectedProductID, _selectedProductCode, _selectedProductName, unit, factor, qty, wh.ID);
        }

        private void AddItemToGrid(int productID, string productCode, string productName, string unit, decimal factor, decimal qty, int sourceWarehouseID)
        {
            decimal availableStockSmallest = InventoryDAL.GetProductStock(productID, sourceWarehouseID);
            decimal currentBaseQtyInGrid = 0m;
            TransferItemDTO existingSameUnit = null;

            foreach (var it in _items)
            {
                if (it.ProductID == productID)
                {
                    currentBaseQtyInGrid += it.TotalBaseQty;
                    if (string.Equals(it.Unit, unit, StringComparison.OrdinalIgnoreCase) && Math.Abs(it.Factor - factor) < 0.001m)
                    {
                        existingSameUnit = it;
                    }
                }
            }

            decimal requestedBaseQty = qty * (factor > 0 ? factor : 1.0m);
            if (currentBaseQtyInGrid + requestedBaseQty > availableStockSmallest)
            {
                decimal availInSelectedUnit = factor > 0 ? (availableStockSmallest / factor) : availableStockSmallest;
                MessageBox.Show($"❌ الكمية المطلوبة تتجاوز الرصيد المتوفر في المستودع المصدر!\n" +
                                $"• الرصيد المتاح بالمصدر: {availInSelectedUnit:G29} {unit} ({availableStockSmallest:G29} بالصغرى)\n" +
                                $"• المضاف مسبقاً بالإذن: {currentBaseQtyInGrid:G29} بالصغرى\n" +
                                $"• المطلوب إضافته: {qty:G29} {unit} ({requestedBaseQty:G29} بالصغرى)",
                                "عجز في الرصيد المتوفر", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (existingSameUnit != null)
            {
                existingSameUnit.Quantity += qty;
                existingSameUnit.AvailableStock = factor > 0 ? (availableStockSmallest / factor) : availableStockSmallest;
            }
            else
            {
                _items.Add(new TransferItemDTO
                {
                    ProductID = productID,
                    ProductCode = productCode,
                    ProductName = productName,
                    Quantity = qty,
                    AvailableStock = factor > 0 ? (availableStockSmallest / factor) : availableStockSmallest,
                    Unit = string.IsNullOrWhiteSpace(unit) ? "قطعة" : unit,
                    Factor = factor > 0 ? factor : 1.0m
                });
            }

            RefreshGrid();
            ResetSelectedItem();
        }

        private void ResetSelectedItem()
        {
            _selectedProductID = 0;
            _selectedProductCode = "";
            _selectedProductName = "";
            _selectedProductStock = 0m;
            txtSelectedProduct.Text = "اضغط [F3] أو امسح الباركود لاختيار صنف...";
            lblAvailableStock.Text = "متاح: --";
            lblAvailableStock.BackColor = Color.FromArgb(35, 48, 68);
            lblAvailableStock.ForeColor = Color.FromArgb(160, 175, 195);
            cboUnit.Items.Clear();
            nudQty.Value = 1m;
            txtBarcodeTransfer.Focus();
        }

        private void RefreshGrid()
        {
            _isRefreshingGrid = true;
            try
            {
                if (dgItems.IsCurrentCellInEditMode)
                {
                    dgItems.CancelEdit();
                }
                dgItems.EndEdit();
                dgItems.Rows.Clear();

                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    int rIndex = dgItems.Rows.Add();
                    var row = dgItems.Rows[rIndex];

                    row.Cells["RowIndex"].Value = (i + 1).ToString();
                    row.Cells["ProductID"].Value = item.ProductID;
                    row.Cells["ProductCode"].Value = item.ProductCode;
                    row.Cells["ProductName"].Value = item.ProductName;

                    // تهيئة خلايا قائمة الوحدات لهذا الصنف
                    if (row.Cells["Unit"] is DataGridViewComboBoxCell unitCell)
                    {
                        unitCell.DataSource = null;
                        unitCell.Items.Clear();
                        var units = GetProductUnits(item.ProductID);
                        foreach (var u in units)
                        {
                            if (!unitCell.Items.Contains(u.UnitName))
                                unitCell.Items.Add(u.UnitName);
                        }
                        if (!string.IsNullOrEmpty(item.Unit) && !unitCell.Items.Contains(item.Unit))
                            unitCell.Items.Add(item.Unit);

                        unitCell.Value = item.Unit;
                    }

                    row.Cells["Factor"].Value = item.Factor.ToString("G29");
                    row.Cells["AvailableStock"].Value = item.AvailableStock.ToString("G29");
                    row.Cells["Quantity"].Value = item.Quantity.ToString("G29");
                    row.Cells["TotalBaseQty"].Value = item.TotalBaseQty.ToString("G29");
                }

                UpdateSummaryTotals();
            }
            finally
            {
                _isRefreshingGrid = false;
            }
        }

        private void UpdateSummaryTotals()
        {
            decimal totalQty = 0m;
            decimal totalBaseQty = 0m;
            for (int i = 0; i < _items.Count; i++)
            {
                totalQty += _items[i].Quantity;
                totalBaseQty += _items[i].TotalBaseQty;
            }

            lblCountBadge.Text = $"🏷️  البنود المحولة: {_items.Count} صنف";
            lblTotalQtyBadge.Text = $"⚖️  إجمالي بالوحدات: {totalQty:N2} | بالصغرى: {totalBaseQty:N0}";
        }

        private void DgItems_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dgItems.CurrentCell != null && dgItems.CurrentCell.OwningColumn.Name == "Unit")
            {
                if (e.Control is ComboBox cb)
                {
                    cb.DropDownStyle = ComboBoxStyle.DropDownList;
                    cb.DroppedDown = true;
                }
            }
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isRefreshingGrid || e.RowIndex < 0 || e.RowIndex >= _items.Count || e.ColumnIndex < 0)
                return;

            if (dgItems.Columns[e.ColumnIndex].Name == "Unit")
            {
                string newUnit = dgItems.Rows[e.RowIndex].Cells["Unit"].Value?.ToString() ?? "";
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (e.RowIndex >= 0 && e.RowIndex < _items.Count)
                    {
                        HandleGridUnitChange(e.RowIndex, _items[e.RowIndex], newUnit);
                    }
                });
            }
        }

        private void HandleGridUnitChange(int rowIndex, TransferItemDTO item, string newUnit)
        {
            if (string.IsNullOrWhiteSpace(newUnit) || item == null) return;
            if (string.Equals(item.Unit, newUnit, StringComparison.OrdinalIgnoreCase)) return;

            var units = GetProductUnits(item.ProductID);
            var selectedOpt = units.Find(u => string.Equals(u.UnitName, newUnit, StringComparison.OrdinalIgnoreCase));
            decimal newFactor = selectedOpt != null && selectedOpt.Factor > 0 ? selectedOpt.Factor : 1.0m;

            int sourceWarehouseID = (cboFromWarehouse.SelectedItem is ComboItem wh) ? wh.ID : 0;
            decimal baseStock = sourceWarehouseID > 0 ? InventoryDAL.GetProductStock(item.ProductID, sourceWarehouseID) : (item.AvailableStock * item.Factor);

            decimal otherBaseQty = 0m;
            for (int i = 0; i < _items.Count; i++)
            {
                if (i != rowIndex && _items[i].ProductID == item.ProductID)
                    otherBaseQty += _items[i].TotalBaseQty;
            }

            decimal remainingBaseStock = baseStock - otherBaseQty;
            if (remainingBaseStock < 0) remainingBaseStock = 0;

            decimal newBaseQty = item.Quantity * newFactor;

            if (remainingBaseStock <= 0)
            {
                MessageBox.Show($"❌ لا يتوفر رصيد متبقٍ لهذا الصنف بالمستودع المصدر لتغيير الوحدة إلى '{newUnit}'!\n" +
                                $"• الرصيد الإجمالي: {baseStock:G29} بالصغرى\n" +
                                $"• المحجوز في باقي بنود الإذن: {otherBaseQty:G29} بالصغرى",
                                "رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (rowIndex >= 0 && rowIndex < dgItems.Rows.Count)
                    dgItems.Rows[rowIndex].Cells["Unit"].Value = item.Unit;
                return;
            }

            if (newBaseQty > remainingBaseStock)
            {
                decimal maxInNewUnit = newFactor > 0 ? Math.Floor(remainingBaseStock / newFactor * 1000m) / 1000m : remainingBaseStock;
                if (maxInNewUnit <= 0) maxInNewUnit = remainingBaseStock / newFactor;

                MessageBox.Show($"⚠️ تنبيه: الكمية الحالية ({item.Quantity:G29} {newUnit}) تعادل ({newBaseQty:G29} بالصغرى) وتتجاوز الرصيد المتاح بالمصدر ({remainingBaseStock:G29} بالصغرى)!\n" +
                                $"• تم تعديل الكمية تلقائياً للحد الأقصى المتاح بالوحدة الجديدة: {maxInNewUnit:G29} {newUnit}.",
                                "تعديل تلقائي للكمية المتاحة", MessageBoxButtons.OK, MessageBoxIcon.Information);

                item.Quantity = maxInNewUnit;
            }

            item.Unit = newUnit;
            item.Factor = newFactor;
            item.AvailableStock = newFactor > 0 ? (baseStock / newFactor) : baseStock;

            if (rowIndex >= 0 && rowIndex < dgItems.Rows.Count)
            {
                var row = dgItems.Rows[rowIndex];
                row.Cells["Unit"].Value = item.Unit;
                row.Cells["Factor"].Value = item.Factor.ToString("G29");
                row.Cells["AvailableStock"].Value = item.AvailableStock.ToString("G29");
                row.Cells["Quantity"].Value = item.Quantity.ToString("G29");
                row.Cells["TotalBaseQty"].Value = item.TotalBaseQty.ToString("G29");
            }

            UpdateSummaryTotals();
        }

        private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgItems.Columns[e.ColumnIndex].Name != "Delete") return;

            string prodName = _items[e.RowIndex].ProductName;
            if (MessageBox.Show($"هل تريد حذف صنف «{prodName}» من إذن التحويل؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _items.RemoveAt(e.RowIndex);
                RefreshGrid();
            }
        }

        private void DgItems_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            if (dgItems.Columns[e.ColumnIndex].Name != "Quantity") return;

            var item = _items[e.RowIndex];
            var cellVal = dgItems.Rows[e.RowIndex].Cells["Quantity"].Value?.ToString();

            if (decimal.TryParse(cellVal, out decimal newQty) && newQty > 0)
            {
                // التحقق من الرصيد المتوفر بالمستودع المصدر بالوحدات الصغرى
                int sourceWarehouseID = (cboFromWarehouse.SelectedItem is ComboItem wh) ? wh.ID : 0;
                decimal baseStock = sourceWarehouseID > 0 ? InventoryDAL.GetProductStock(item.ProductID, sourceWarehouseID) : (item.AvailableStock * item.Factor);

                decimal otherBaseQty = 0m;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (i != e.RowIndex && _items[i].ProductID == item.ProductID)
                        otherBaseQty += _items[i].TotalBaseQty;
                }

                decimal newBaseQty = newQty * (item.Factor > 0 ? item.Factor : 1.0m);
                if (otherBaseQty + newBaseQty > baseStock)
                {
                    decimal maxAllowedInThisUnit = item.Factor > 0 ? ((baseStock - otherBaseQty) / item.Factor) : (baseStock - otherBaseQty);
                    if (maxAllowedInThisUnit < 0) maxAllowedInThisUnit = 0;

                    MessageBox.Show($"❌ الكمية المدخلة ({newQty:G29} {item.Unit}) تتجاوز الرصيد المتاح بالمصدر!\n" +
                                    $"• الحد الأقصى المتاح بالوحدة المختارة: {maxAllowedInThisUnit:G29} {item.Unit}\n" +
                                    $"• الرصيد الإجمالي المتاح: {baseStock:G29} بالصغرى", "تنبيه رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dgItems.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity.ToString("G29");
                    return;
                }

                item.Quantity = newQty;
                dgItems.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity.ToString("G29");
                dgItems.Rows[e.RowIndex].Cells["TotalBaseQty"].Value = item.TotalBaseQty.ToString("G29");
                UpdateSummaryTotals();
            }
            else
            {
                MessageBox.Show("يرجى إدخال قيمة كمية عددية صحيحة أكبر من الصفر!", "خطأ في الإدخال", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dgItems.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity.ToString("G29");
            }
        }

        private void DgItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dgItems.CurrentRow != null && !dgItems.IsCurrentCellInEditMode)
            {
                int index = dgItems.CurrentRow.Index;
                if (index >= 0 && index < _items.Count)
                {
                    _items.RemoveAt(index);
                    RefreshGrid();
                    e.Handled = true;
                }
            }
            else if (!dgItems.IsCurrentCellInEditMode &&
                     (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1 ||
                      e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2 ||
                      e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3))
            {
                int index = dgItems.CurrentRow != null ? dgItems.CurrentRow.Index : -1;
                if (index >= 0 && index < _items.Count)
                {
                    var item = _items[index];
                    var units = GetProductUnits(item.ProductID);
                    TransferUnitOption targetOpt = null;
                    if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
                        targetOpt = units.Find(u => u.Level == 3);
                    else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
                        targetOpt = units.Find(u => u.Level == 2);
                    else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
                        targetOpt = units.Find(u => u.Level == 1);

                    if (targetOpt != null && !string.Equals(targetOpt.UnitName, item.Unit, StringComparison.OrdinalIgnoreCase))
                    {
                        dgItems.Rows[index].Cells["Unit"].Value = targetOpt.UnitName;
                        HandleGridUnitChange(index, item, targetOpt.UnitName);
                        e.Handled = true;
                    }
                }
            }
        }

        private void BtnSave_Click(bool printAfterSave)
        {
            if (!Session.CanAdd("WarehouseTransfer"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية حفظ التحويلات المخزنية.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(cboFromWarehouse.SelectedItem is ComboItem from) || from.ID <= 0)
            {
                MessageBox.Show("يرجى اختيار مستودع المصدر المنقول منه!", "بيانات ناقصة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboFromWarehouse.Focus();
                return;
            }

            if (!(cboToWarehouse.SelectedItem is ComboItem to) || to.ID <= 0)
            {
                MessageBox.Show("يرجى اختيار مستودع الوجهة المنقول إليه!", "بيانات ناقصة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboToWarehouse.Focus();
                return;
            }

            if (from.ID == to.ID)
            {
                MessageBox.Show("لا يمكن التحويل لنفس المستودع! يجب اختيار مستودع مختلف.", "خطأ في المسار", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cboToWarehouse.Focus();
                return;
            }

            if (_items.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف في إذن التحويل!\nيرجى إضافة صنف واحد على الأقل عبر مسح الباركود أو الضغط على [F3].", "إذن فارغ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBarcodeTransfer.Focus();
                return;
            }

            decimal totalQty = 0m;
            decimal totalBaseQty = 0m;
            foreach (var it in _items)
            {
                totalQty += it.Quantity;
                totalBaseQty += it.TotalBaseQty;
            }

            string confirmMsg = $"هل تريد بالتأكيد ترحيل وحفظ إذن التحويل المخزني التالي؟\n\n" +
                               $"• من مستودع: {from.Text}\n" +
                               $"• إلى مستودع: {to.Text}\n" +
                               $"• عدد البنود: {_items.Count} صنف\n" +
                               $"• إجمالي الكميات بالوحدات المختارة: {totalQty:N2}\n" +
                               $"• إجمالي الكميات المحولة بالوحدة الصغرى: {totalBaseQty:N0}\n" +
                               (string.IsNullOrWhiteSpace(txtNotes.Text) ? "" : $"• ملاحظات: {txtNotes.Text.Trim()}\n");

            if (MessageBox.Show(confirmMsg, "تأكيد التحويل المخزني", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                string notes = txtNotes.Text.Trim();
                var itemsCopy = new List<TransferItemDTO>(_items);
                string fromName = from.Text;
                string toName = to.Text;

                int transferID = TransferDAL.SaveTransfer(from.ID, to.ID, notes, _items);
                if (transferID > 0)
                {
                    string transferCode = $"TRF-{transferID}";
                    MessageBox.Show($"✅ تم ترحيل وحفظ إذن التحويل المخزني بنجاح!\nرقم الإذن: {transferCode}\nتم تحديث أرصدة المستودعات فوراً.", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (printAfterSave)
                    {
                        PrintTransferSlip(transferID, transferCode, fromName, toName, notes, itemsCopy);
                    }

                    ClearForm();
                }
                else
                {
                    MessageBox.Show("❌ فشل في حفظ التحويل المخزني.", "خطأ في النظام", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ أثناء حفظ التحويل المخزني:\n" + ex.Message, "خطأ في الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintTransferSlip(int transferID, string transferCode, string fromWarehouse, string toWarehouse, string notes, List<TransferItemDTO> items)
        {
            try
            {
                var pd = new PrintDocument();
                pd.DocumentName = $"TransferSlip_{transferCode}";

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    float left = ev.MarginBounds.Left;
                    float right = ev.MarginBounds.Right;
                    float top = ev.MarginBounds.Top;
                    float width = ev.MarginBounds.Width;
                    float y = top;

                    var fTitle = new Font("Arial", 16f, FontStyle.Bold);
                    var fHeader = new Font("Arial", 12f, FontStyle.Bold);
                    var fRegular = new Font("Arial", 10f);
                    var fBold = new Font("Arial", 10f, FontStyle.Bold);

                    var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    var sfRtlRight = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                    var sfRtlCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                    // عنوان الشركة والترويسة
                    string company = AppConfig.CompanyName ?? "نظام إدارة المبيعات والمخازن";
                    g.DrawString(company, fHeader, Brushes.Black, new RectangleF(left, y, width, 26), sfCenter);
                    y += 28;

                    g.DrawString("إذن تحويل مخزني داخلي", fTitle, Brushes.DarkBlue, new RectangleF(left, y, width, 32), sfCenter);
                    y += 36;

                    // إطار بيانات الإذن
                    var rectInfo = new RectangleF(left, y, width, 55);
                    g.FillRectangle(Brushes.WhiteSmoke, rectInfo);
                    g.DrawRectangle(Pens.LightGray, rectInfo.X, rectInfo.Y, rectInfo.Width, rectInfo.Height);

                    g.DrawString($"رقم الإذن: {transferCode}", fBold, Brushes.Black, new RectangleF(right - 220, y + 6, 210, 22), sfRtlRight);
                    g.DrawString($"التاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}", fRegular, Brushes.Black, new RectangleF(left + 10, y + 6, 250, 22), sfRtlRight);

                    g.DrawString($"من مستودع: {fromWarehouse}", fBold, Brushes.Black, new RectangleF(right - 260, y + 28, 250, 22), sfRtlRight);
                    g.DrawString($"إلى مستودع: {toWarehouse}", fBold, Brushes.Black, new RectangleF(left + 10, y + 28, 250, 22), sfRtlRight);
                    y += 65;

                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        g.DrawString($"ملاحظات: {notes}", fRegular, Brushes.DarkSlateGray, new RectangleF(left, y, width, 20), sfRtlRight);
                        y += 24;
                    }

                    // ترويسة الجدول
                    float colW1 = 35f;  // #
                    float colW2 = 100f; // الكود
                    float colW4 = 85f;  // الوحدة
                    float colW5 = 85f;  // الكمية
                    float colW3 = width - (colW1 + colW2 + colW4 + colW5); // الاسم

                    var headerRect = new RectangleF(left, y, width, 28);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(235, 240, 250)), headerRect);
                    g.DrawRectangle(Pens.Gray, headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height);

                    float cx = right;
                    g.DrawString("#", fBold, Brushes.Black, new RectangleF(cx - colW1, y, colW1, 28), sfRtlCenter); cx -= colW1;
                    g.DrawString("كود الصنف", fBold, Brushes.Black, new RectangleF(cx - colW2, y, colW2, 28), sfRtlCenter); cx -= colW2;
                    g.DrawString("اسم الصنف والبيان", fBold, Brushes.Black, new RectangleF(cx - colW3, y, colW3, 28), sfRtlRight); cx -= colW3;
                    g.DrawString("الوحدة", fBold, Brushes.Black, new RectangleF(cx - colW4, y, colW4, 28), sfRtlCenter); cx -= colW4;
                    g.DrawString("الكمية المحولة", fBold, Brushes.Black, new RectangleF(cx - colW5, y, colW5, 28), sfRtlCenter);

                    y += 28;

                    // سطور الأصناف
                    decimal totalQty = 0m;
                    decimal totalBaseQty = 0m;
                    for (int i = 0; i < items.Count; i++)
                    {
                        var it = items[i];
                        totalQty += it.Quantity;
                        totalBaseQty += it.TotalBaseQty;
                        float rowH = 26f;

                        if (i % 2 == 1)
                        {
                            g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), new RectangleF(left, y, width, rowH));
                        }
                        g.DrawRectangle(Pens.LightGray, left, y, width, rowH);

                        string unitDisplay = it.Factor > 1m ? $"{it.Unit} (×{it.Factor:G29})" : (it.Unit ?? "قطعة");

                        cx = right;
                        g.DrawString((i + 1).ToString(), fRegular, Brushes.Black, new RectangleF(cx - colW1, y, colW1, rowH), sfRtlCenter); cx -= colW1;
                        g.DrawString(it.ProductCode, fRegular, Brushes.Black, new RectangleF(cx - colW2, y, colW2, rowH), sfRtlCenter); cx -= colW2;
                        g.DrawString(it.ProductName, fBold, Brushes.Black, new RectangleF(cx - colW3, y, colW3, rowH), sfRtlRight); cx -= colW3;
                        g.DrawString(unitDisplay, fRegular, Brushes.Black, new RectangleF(cx - colW4, y, colW4, rowH), sfRtlCenter); cx -= colW4;
                        g.DrawString(it.Quantity.ToString("N3"), fBold, Brushes.Black, new RectangleF(cx - colW5, y, colW5, rowH), sfRtlCenter);

                        y += rowH;
                    }

                    // سطر الإجمالي
                    y += 4;
                    var totRect = new RectangleF(left, y, width, 28);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 245, 235)), totRect);
                    g.DrawRectangle(Pens.DarkGray, totRect.X, totRect.Y, totRect.Width, totRect.Height);

                    g.DrawString($"إجمالي عدد البنود: {items.Count} صنف", fBold, Brushes.Black, new RectangleF(right - 220, y, 210, 28), sfRtlRight);
                    g.DrawString($"إجمالي المحول: {totalQty:N2} | بالصغرى: {totalBaseQty:N0}", fBold, Brushes.DarkGreen, new RectangleF(left + 15, y, 320, 28), sfRtlRight);
                    y += 50;

                    // خانات التوقيعات
                    float sigW = width / 3f;
                    g.DrawString("أمين مستودع المصدر", fBold, Brushes.Black, new RectangleF(right - sigW, y, sigW, 22), sfCenter);
                    g.DrawString("السائق / المستلم", fBold, Brushes.Black, new RectangleF(right - (sigW * 2), y, sigW, 22), sfCenter);
                    g.DrawString("أمين مستودع الوجهة", fBold, Brushes.Black, new RectangleF(left, y, sigW, 22), sfCenter);
                    y += 35;

                    g.DrawLine(Pens.Gray, right - sigW + 20, y, right - 20, y);
                    g.DrawLine(Pens.Gray, right - (sigW * 2) + 20, y, right - sigW - 20, y);
                    g.DrawLine(Pens.Gray, left + 20, y, left + sigW - 20, y);
                };

                using var dlg = new PrintDialog { Document = pd };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    pd.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء طباعة إذن التحويل:\n" + ex.Message, "خطأ في الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenTransfersList()
        {
            try
            {
                if (this.ParentForm is FrmMain main)
                {
                    main.NavigateTo(new FrmWarehouseTransfersList());
                }
                else
                {
                    using var listForm = new FrmWarehouseTransfersList();
                    listForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في فتح سجل التحويلات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearForm()
        {
            _productUnitsCache.Clear();
            _items.Clear();
            dgItems.Rows.Clear();
            cboFromWarehouse.SelectedIndex = 0;
            cboToWarehouse.SelectedIndex = 0;
            txtNotes.Clear();
            txtBarcodeTransfer.Clear();
            ResetSelectedItem();
            lblCountBadge.Text = "🏷️  البنود المحولة: 0 صنف";
            lblTotalQtyBadge.Text = "⚖️  إجمالي الكميات: 0.000";
        }
    }
}

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
    /// <summary>شاشة التحويل المخزني بين المستودعات - تصميم متطور ومتجاوب</summary>
    public class FrmWarehouseTransfer : Form
    {
        private ComboBox cboFromWarehouse, cboToWarehouse;
        private NumericUpDown nudQty;
        private Label lblAvailableStock, lblUnitTag;
        private TextBox txtNotes, txtBarcodeTransfer, txtSelectedProduct;
        private Button btnSearchProduct, btnAddItem, btnSave, btnSaveAndPrint, btnNew, btnTransfersHistory;
        private DataGridView dgItems;
        private Label lblCountBadge, lblTotalQtyBadge;
        private List<TransferItemDTO> _items = new List<TransferItemDTO>();

        private int _selectedProductID = 0;
        private string _selectedProductCode = "";
        private string _selectedProductName = "";
        private string _selectedProductUnit = "";
        private decimal _selectedProductStock = 0m;

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
                Height = 205,
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
            pnlTopContainer.Controls.Add(pnlTitleBar);

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
            pnlTopContainer.Controls.Add(pnlRouteCard);

            // 3. شريط الإدخال السريع للأصناف (Fast Entry Strip)
            var pnlFastEntryCard = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 82,
                BackColor = Color.FromArgb(22, 30, 45),
                Padding = new Padding(12, 10, 12, 8)
            };
            pnlFastEntryCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.BorderSearchPanel, 1.2f);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlFastEntryCard.Width - 1, pnlFastEntryCard.Height - 1);
            };

            var tblFastEntry = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));      // 0: الاسكنر
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));      // 1: زر بحث F3
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));       // 2: اسم الصنف المختار (مرن يملأ الشاشة)
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165f));      // 3: شارة الرصيد المتاح
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105f));      // 4: خانة الكمية
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65f));       // 5: تسمية الوحدة
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155f));      // 6: زر الإضافة
            tblFastEntry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10f));       // 7: هامش

            // حاوية الاسكنر مع عنوانه
            var pnlScanner = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblScannerTitle = new Label
            {
                Text = "📷 الاسكنر / الباركود:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 100),
                Dock = DockStyle.Top,
                Height = 20
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

            // زر بحث الأصناف [F3]
            btnSearchProduct = Theme.MakeButton("🔍 بحث الأصناف [F3]", Theme.Primary);
            btnSearchProduct.Dock = DockStyle.Bottom;
            btnSearchProduct.Height = 35;
            btnSearchProduct.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSearchProduct.Click += (s, e) => OpenProductSearch();

            // حاوية الصنف المختار
            var pnlSelectedProd = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var lblSelectedTitle = new Label
            {
                Text = "📦 الصنف المحدد للتحويل (انقر للاختيار):",
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

            // شارة الرصيد المتاح بالمصدر
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

            // حاوية الكمية
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

            // تسمية الوحدة
            lblUnitTag = new Label
            {
                Text = "قطعة",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 215, 235),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomCenter,
                Padding = new Padding(0, 0, 0, 6)
            };

            // زر الإضافة
            btnAddItem = Theme.MakeButton("➕ إضافة [Enter]", Theme.Accent);
            btnAddItem.Dock = DockStyle.Bottom;
            btnAddItem.Height = 35;
            btnAddItem.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnAddItem.Click += BtnAddItem_Click;

            tblFastEntry.Controls.Add(pnlScanner, 0, 0);
            tblFastEntry.Controls.Add(btnSearchProduct, 1, 0);
            tblFastEntry.Controls.Add(pnlSelectedProd, 2, 0);
            tblFastEntry.Controls.Add(pnlStockBadge, 3, 0);
            tblFastEntry.Controls.Add(pnlQty, 4, 0);
            tblFastEntry.Controls.Add(lblUnitTag, 5, 0);
            tblFastEntry.Controls.Add(btnAddItem, 6, 0);

            pnlFastEntryCard.Controls.Add(tblFastEntry);
            pnlTopContainer.Controls.Add(pnlFastEntryCard);

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

            var colUnit = new DataGridViewTextBoxColumn
            {
                Name = "Unit",
                HeaderText = "الوحدة",
                FillWeight = 45,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            var colAvail = new DataGridViewTextBoxColumn
            {
                Name = "AvailableStock",
                HeaderText = "المتاح بالمصدر",
                FillWeight = 65,
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
                FillWeight = 75,
                ReadOnly = false, // متاح للتعديل المباشر في الخلية!
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(255, 215, 80),
                    BackColor = Color.FromArgb(30, 42, 60),
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold)
                }
            };

            var colDel = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "حذف",
                Text = "🗑️ حذف",
                UseColumnTextForButtonValue = true,
                FillWeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(240, 80, 80),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            };

            dgItems.Columns.AddRange(new DataGridViewColumn[] { colIndex, colPid, colCode, colName, colUnit, colAvail, colQty, colDel });
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
            UpdateAvailableStock();
        }

        private void OpenProductSearch()
        {
            if (!(cboFromWarehouse.SelectedItem is ComboItem wh) || wh.ID <= 0)
            {
                MessageBox.Show("⚠️ يرجى اختيار مستودع المصدر أولاً لعرض أرصدة الأصناف المتوفرة به بدقة!", "تحديد مستودع المصدر مطلوب", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboFromWarehouse.Focus();
                return;
            }

            using var frm = new FrmProductSearch(warehouseID: wh.ID, isPurchaseMode: false, defaultShowZeroStock: false);
            if (frm.ShowDialog(this) == DialogResult.OK && frm.SelectedProductID > 0)
            {
                SelectProductByID(frm.SelectedProductID, wh.ID, frm.SelectedQuantity > 0 ? frm.SelectedQuantity : 1m);
            }
        }

        private void SelectProductByID(int productID, int warehouseID, decimal initialQty = 1m)
        {
            var dt = DbHelper.Query("SELECT ProductID, ProductCode, ProductName, Unit FROM Products WHERE ProductID=@id", DbHelper.P("@id", productID));
            if (dt != null && dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                _selectedProductID = Convert.ToInt32(row["ProductID"]);
                _selectedProductCode = row["ProductCode"]?.ToString() ?? "";
                _selectedProductName = row["ProductName"]?.ToString() ?? "";
                _selectedProductUnit = row["Unit"]?.ToString() ?? "قطعة";

                txtSelectedProduct.Text = $"{_selectedProductCode} - {_selectedProductName}";
                lblUnitTag.Text = _selectedProductUnit;

                _selectedProductStock = InventoryDAL.GetProductStock(_selectedProductID, warehouseID);
                lblAvailableStock.Text = $"متاح: {_selectedProductStock:G29} {_selectedProductUnit}";

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

                decimal suggestQty = _selectedProductStock > 0 ? Math.Min(initialQty, _selectedProductStock) : initialQty;
                nudQty.Value = suggestQty > 0 ? suggestQty : 1m;
                nudQty.Focus();
                nudQty.Select(0, nudQty.Text.Length);
            }
        }

        private void UpdateAvailableStock()
        {
            if (_selectedProductID > 0 && cboFromWarehouse.SelectedItem is ComboItem wh && wh.ID > 0)
            {
                _selectedProductStock = InventoryDAL.GetProductStock(_selectedProductID, wh.ID);
                lblAvailableStock.Text = $"متاح: {_selectedProductStock:G29} {_selectedProductUnit}";
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
            }
            else
            {
                _selectedProductStock = 0m;
                lblAvailableStock.Text = "متاح: --";
                lblAvailableStock.BackColor = Color.FromArgb(35, 48, 68);
                lblAvailableStock.ForeColor = Color.FromArgb(160, 175, 195);
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
                    int pid = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                    SelectProductByID(pid, wh.ID, 1m);
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

            AddItemToGrid(_selectedProductID, _selectedProductCode, _selectedProductName, _selectedProductUnit, qty, wh.ID);
        }

        private void AddItemToGrid(int productID, string productCode, string productName, string unit, decimal qty, int sourceWarehouseID)
        {
            decimal available = InventoryDAL.GetProductStock(productID, sourceWarehouseID);

            foreach (var existing in _items)
            {
                if (existing.ProductID == productID)
                {
                    decimal newQty = existing.Quantity + qty;
                    if (newQty > available)
                    {
                        MessageBox.Show($"❌ الكمية الإجمالية المطلوبة ({newQty:G29}) تتجاوز الرصيد المتوفر في المستودع المصدر ({available:G29})!", "عجز في الرصيد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    existing.Quantity = newQty;
                    existing.AvailableStock = available;
                    RefreshGrid();
                    ResetSelectedItem();
                    return;
                }
            }

            if (qty > available)
            {
                MessageBox.Show($"❌ الكمية المطلوبة ({qty:G29}) تتجاوز الرصيد المتوفر في المستودع المصدر ({available:G29})!", "عجز في الرصيد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _items.Add(new TransferItemDTO
            {
                ProductID = productID,
                ProductCode = productCode,
                ProductName = productName,
                Quantity = qty,
                AvailableStock = available,
                Unit = string.IsNullOrWhiteSpace(unit) ? "قطعة" : unit
            });

            RefreshGrid();
            ResetSelectedItem();
        }

        private void ResetSelectedItem()
        {
            _selectedProductID = 0;
            _selectedProductCode = "";
            _selectedProductName = "";
            _selectedProductUnit = "";
            _selectedProductStock = 0m;
            txtSelectedProduct.Text = "اضغط [F3] أو امسح الباركود لاختيار صنف...";
            lblAvailableStock.Text = "متاح: --";
            lblAvailableStock.BackColor = Color.FromArgb(35, 48, 68);
            lblAvailableStock.ForeColor = Color.FromArgb(160, 175, 195);
            lblUnitTag.Text = "قطعة";
            nudQty.Value = 1m;
            txtBarcodeTransfer.Focus();
        }

        private void RefreshGrid()
        {
            dgItems.Rows.Clear();
            decimal totalQty = 0m;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                totalQty += item.Quantity;

                dgItems.Rows.Add(
                    (i + 1).ToString(),
                    item.ProductID,
                    item.ProductCode,
                    item.ProductName,
                    item.Unit ?? "قطعة",
                    item.AvailableStock.ToString("G29"),
                    item.Quantity.ToString("G29")
                );
            }

            lblCountBadge.Text = $"🏷️  البنود المحولة: {_items.Count} صنف";
            lblTotalQtyBadge.Text = $"⚖️  إجمالي الكميات: {totalQty:N3}";
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
                if (newQty > item.AvailableStock)
                {
                    MessageBox.Show($"❌ الكمية المدخلة ({newQty:G29}) تتجاوز الرصيد المتاح بالمصدر ({item.AvailableStock:G29})!", "تنبيه رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dgItems.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity.ToString("G29");
                    return;
                }

                item.Quantity = newQty;
                RefreshGrid();
            }
            else
            {
                MessageBox.Show("يرجى إدخال قيمة كمية عددية صحيحة أكبر من الصفر!", "خطأ في الإدخال", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dgItems.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity.ToString("G29");
            }
        }

        private void DgItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dgItems.CurrentRow != null)
            {
                int index = dgItems.CurrentRow.Index;
                if (index >= 0 && index < _items.Count)
                {
                    _items.RemoveAt(index);
                    RefreshGrid();
                    e.Handled = true;
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
            foreach (var it in _items) totalQty += it.Quantity;

            string confirmMsg = $"هل تريد بالتأكيد ترحيل وحفظ إذن التحويل المخزني التالي؟\n\n" +
                               $"• من مستودع: {from.Text}\n" +
                               $"• إلى مستودع: {to.Text}\n" +
                               $"• عدد البنود: {_items.Count} صنف\n" +
                               $"• إجمالي الكمية المحولة: {totalQty:N3}\n" +
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
                    float colW3 = width - (colW1 + colW2 + 75f + 90f); // الاسم
                    float colW4 = 75f;  // الوحدة
                    float colW5 = 90f;  // الكمية

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
                    for (int i = 0; i < items.Count; i++)
                    {
                        var it = items[i];
                        totalQty += it.Quantity;
                        float rowH = 26f;

                        if (i % 2 == 1)
                        {
                            g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), new RectangleF(left, y, width, rowH));
                        }
                        g.DrawRectangle(Pens.LightGray, left, y, width, rowH);

                        cx = right;
                        g.DrawString((i + 1).ToString(), fRegular, Brushes.Black, new RectangleF(cx - colW1, y, colW1, rowH), sfRtlCenter); cx -= colW1;
                        g.DrawString(it.ProductCode, fRegular, Brushes.Black, new RectangleF(cx - colW2, y, colW2, rowH), sfRtlCenter); cx -= colW2;
                        g.DrawString(it.ProductName, fBold, Brushes.Black, new RectangleF(cx - colW3, y, colW3, rowH), sfRtlRight); cx -= colW3;
                        g.DrawString(it.Unit ?? "قطعة", fRegular, Brushes.Black, new RectangleF(cx - colW4, y, colW4, rowH), sfRtlCenter); cx -= colW4;
                        g.DrawString(it.Quantity.ToString("N3"), fBold, Brushes.Black, new RectangleF(cx - colW5, y, colW5, rowH), sfRtlCenter);

                        y += rowH;
                    }

                    // سطر الإجمالي
                    y += 4;
                    var totRect = new RectangleF(left, y, width, 28);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 245, 235)), totRect);
                    g.DrawRectangle(Pens.DarkGray, totRect.X, totRect.Y, totRect.Width, totRect.Height);

                    g.DrawString($"إجمالي عدد البنود: {items.Count} صنف", fBold, Brushes.Black, new RectangleF(right - 250, y, 240, 28), sfRtlRight);
                    g.DrawString($"إجمالي الكميات المحولة: {totalQty:N3}", fBold, Brushes.DarkGreen, new RectangleF(left + 15, y, 250, 28), sfRtlRight);
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

using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة فاتورة المشتريات الاحترافية — مع تعليق/استرجاع وخصم الصنف والضريبة</summary>
    public class FrmPurchase : Form
    {
        // ── أنواع الفاتورة ─────────────────────────────────────────────────────
        private Button btnTypeCredit, btnTypeCash;
        private string _purchaseType = "Credit";

        // ── حقول الرأس ─────────────────────────────────────────────────────────
        private ComboBox cboSupplier, cboProduct, cboWarehouse;
        private DateTimePicker dtpDate;
        private TextBox txtNotes;
        private Label lblCashBalance;
        private Button btnSearchProduct;

        // ── حقول إضافة صنف ─────────────────────────────────────────────────────
        private NumericUpDown nudQty, nudPrice, nudItemDisc, nudSalePrice;
        private Label lblMarginPct; // عرض هامش الربح أثناء الإدخال
        private Button btnAddItem;

        // ── جدول الأصناف ───────────────────────────────────────────────────────
        private DataGridView dgItems;
        private Panel pnlItems, pnlFooter;

        // ── الذيل — خصم الفاتورة ───────────────────────────────────────────────
        private Label lblTotalVal, lblNetVal, lblDiscType, lblDiscVal;
        private TextBox txtInvoiceDiscount;
        private ComboBox cboInvoiceDiscountType;

        // ── الذيل — ضريبة الشراء (جديد) ───────────────────────────────────────
        private NumericUpDown nudTaxPct;
        private Label lblTaxAmt;

        // ── أزرار الذيل ────────────────────────────────────────────────────────
        private Button btnSave, btnNew, btnPrint, btnHold, btnLoadHold;

        // ── بيانات الفاتورة ────────────────────────────────────────────────────
        private List<PurchaseItemDTO> _items = new List<PurchaseItemDTO>();
        private int _lastPurchaseID = 0;
        private bool _isDirty = false;
        private int _draftPurchaseID = 0; // 0 = فاتورة جديدة، >0 = مسودة محملة

        // ══════════════════════════════════════════════════════════════════════
        public FrmPurchase()
        {
            InitUI();
            LoadCombos();
            ClearInvoice();
        }

        // ── مساعد Label ────────────────────────────────────────────────────────
        private Label MakeLabel(string text, int x, int y, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = color ?? Theme.TextMain,
                Font = Theme.FontMain
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        private void InitUI()
        {
            this.Text = "فاتورة مشتريات";
            this.Size = new Size(1150, 760);
            this.MinimumSize = new Size(950, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmPurchase_KeyDown;
            this.FormClosing += FrmPurchase_FormClosing;

            // ══════════════════════════════════════════════════════════════════
            // ── لوحة الرأس — تستخدم TableLayoutPanel للتخطيط المنظم ──────────
            // ══════════════════════════════════════════════════════════════════
            var pnlHeader = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 205,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 8, 12, 8)
            };

            // ── صف 0: نوع الفاتورة + رصيد الخزنة ────────────────────────────
            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 4,
                ColumnCount = 6,
                BackColor   = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            // عرض الأعمدة بالنسبة
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // col0: label
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // col1: control
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // col2: label
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f)); // col3: control
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // col4: label
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f)); // col5: control / buttons
            // ارتفاع الصفوف
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            // ── صف 0: نوع الفاتورة | المورد | التاريخ ────────────────────────
            // أزرار نوع الفاتورة (col5 صف 0)
            btnTypeCredit = Theme.MakeButton("📋 آجل",  0, 0, 95, 30, Theme.Primary);
            btnTypeCash   = Theme.MakeButton("💵 نقدي", 0, 0, 95, 30, Color.FromArgb(60, 60, 60));
            btnTypeCredit.Margin = new Padding(2);
            btnTypeCash.Margin   = new Padding(2);
            btnTypeCredit.Click += (s, e) => { _purchaseType = "Credit"; ToggleType(); };
            btnTypeCash.Click   += (s, e) => { _purchaseType = "Cash";   ToggleType(); };
            var pnlTypeBtns = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = Color.Transparent,
                Dock          = DockStyle.Fill,
                WrapContents  = false,
                Margin        = new Padding(0, 4, 0, 0)
            };
            pnlTypeBtns.Controls.Add(btnTypeCash);
            pnlTypeBtns.Controls.Add(btnTypeCredit);

            var lblType = MakeLabel("نوع الفاتورة:", 0, 0);
            lblType.Dock = DockStyle.Fill;
            lblType.TextAlign = ContentAlignment.MiddleRight;
            lblType.Margin = new Padding(2);

            // المورد (col0-col1 صف 0)
            var lblSupp = MakeLabel("المورد:", 0, 0);
            lblSupp.Dock = DockStyle.Fill;
            lblSupp.TextAlign = ContentAlignment.MiddleRight;
            lblSupp.Margin = new Padding(2);
            cboSupplier = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 6, 2, 6)
            };

            // التاريخ
            var lblDate = MakeLabel("التاريخ:", 0, 0);
            lblDate.Dock = DockStyle.Fill;
            lblDate.TextAlign = ContentAlignment.MiddleRight;
            lblDate.Margin = new Padding(2);
            dtpDate = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 6, 2, 6)
            };

            // رصيد الخزنة
            lblCashBalance = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(100, 180, 100),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Margin = new Padding(2)
            };

            // إضافة إلى الجدول — صف 0
            tbl.Controls.Add(lblSupp,       0, 0);
            tbl.Controls.Add(cboSupplier,   1, 0);
            tbl.Controls.Add(lblDate,       2, 0);
            tbl.Controls.Add(dtpDate,       3, 0);
            tbl.Controls.Add(lblType,       4, 0);
            tbl.Controls.Add(pnlTypeBtns,   5, 0);

            // ── صف 1: ملاحظات | الصنف | رصيد نقدي ───────────────────────────
            var lblNotes = MakeLabel("ملاحظات:", 0, 0);
            lblNotes.Dock = DockStyle.Fill;
            lblNotes.TextAlign = ContentAlignment.MiddleRight;
            lblNotes.Margin = new Padding(2);
            txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 6, 2, 6)
            };

            var lblProd = MakeLabel("الصنف:", 0, 0);
            lblProd.Dock = DockStyle.Fill;
            lblProd.TextAlign = ContentAlignment.MiddleRight;
            lblProd.Margin = new Padding(2);
            cboProduct = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 6, 2, 6)
            };

            // إضافة — صف 1
            tbl.Controls.Add(lblNotes,    0, 1);
            tbl.Controls.Add(txtNotes,    1, 1);
            tbl.Controls.Add(lblProd,     2, 1);
            tbl.Controls.Add(cboProduct,  3, 1);
            tbl.Controls.Add(lblCashBalance, 4, 1);
            tbl.SetColumnSpan(lblCashBalance, 2);

            // ── صف 2: المخزن | زر بحث الأصناف ───────────────────────────────────
            var lblWarehouse = MakeLabel("المخزن:", 0, 0);
            lblWarehouse.Dock = DockStyle.Fill;
            lblWarehouse.TextAlign = ContentAlignment.MiddleRight;
            lblWarehouse.Margin = new Padding(2);
            cboWarehouse = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 6, 2, 6)
            };

            btnSearchProduct = new Button
            {
                Text = "🔍 بحث صنف",
                Dock = DockStyle.Fill,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 6, 2, 6),
                Font = Theme.FontBold
            };
            btnSearchProduct.FlatAppearance.BorderSize = 0;
            btnSearchProduct.Click += (s, e) =>
            {
                // فتح نافذة بحث الأصناف
                using (var dlgSearch = new FrmProductSearch())
                {
                    if (dlgSearch.ShowDialog(this) == DialogResult.OK && dlgSearch.SelectedProductID > 0)
                    {
                        // تحديد الصنف في الكومبو
                        for (int si = 0; si < cboProduct.Items.Count; si++)
                        {
                            if (cboProduct.Items[si] is ComboItem ci && ci.ID == dlgSearch.SelectedProductID)
                            {
                                cboProduct.SelectedIndex = si;
                                break;
                            }
                        }
                    }
                }
            };

            // إضافة — صف 2
            tbl.Controls.Add(lblWarehouse, 0, 2);
            tbl.Controls.Add(cboWarehouse, 1, 2);
            tbl.Controls.Add(btnSearchProduct, 2, 2);
            tbl.SetColumnSpan(btnSearchProduct, 2);

            // ── صف 3: الكمية | السعر | خصم% | زر إضافة ──────────────────────
            var pnlAddRow = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            tbl.Controls.Add(pnlAddRow, 0, 3);
            tbl.SetColumnSpan(pnlAddRow, 6);

            // عناصر صف الإضافة بداخل pnlAddRow بـ FlowLayoutPanel
            var flowAdd = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            Func<string, Control> makeLblInline = txt =>
            {
                var l = new Label
                {
                    Text = txt, AutoSize = false,
                    Width = 60, Height = 30,
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Theme.TextMain, Font = Theme.FontMain,
                    Margin = new Padding(2, 4, 4, 0)
                };
                return l;
            };

            var lblQty = makeLblInline("الكمية:");
            nudQty = new NumericUpDown
            {
                Width = 80, Height = 28,
                DecimalPlaces = 3, Minimum = 0.001m, Maximum = 999999, Value = 1,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 4, 6, 0)
            };

            var lblPrice = makeLblInline("السعر:");
            nudPrice = new NumericUpDown
            {
                Width = 80, Height = 28,
                DecimalPlaces = 2, Minimum = 0, Maximum = 9999999,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 4, 6, 0)
            };
            nudPrice.ValueChanged += (s, e) => UpdateMarginLabel();

            var lblSalePrice = makeLblInline("سعر البيع:");
            nudSalePrice = new NumericUpDown
            {
                Width = 80, Height = 28,
                DecimalPlaces = 2, Minimum = 0, Maximum = 9999999,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 4, 6, 0)
            };
            nudSalePrice.ValueChanged += (s, e) => UpdateMarginLabel();

            lblMarginPct = new Label
            {
                Text = "0.0%",
                AutoSize = false,
                Width = 60, Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Margin = new Padding(2, 4, 6, 0)
            };

            var lblItemDiscLbl = makeLblInline("خصم%:");
            nudItemDisc = new NumericUpDown
            {
                Width = 70, Height = 28,
                DecimalPlaces = 2, Minimum = 0, Maximum = 100,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 4, 6, 0)
            };
            nudItemDisc.Value = 0;

            btnAddItem = Theme.MakeButton("➕ إضافة", 0, 0, 110, 30, Theme.Accent);
            btnAddItem.Margin = new Padding(4, 4, 4, 0);
            btnAddItem.Click += BtnAddItem_Click;

            // ترتيب RTL: الأول في الكود = الأيسر في الشاشة (لأن FlowDirection = RightToLeft)
            flowAdd.Controls.Add(btnAddItem);
            flowAdd.Controls.Add(nudItemDisc);
            flowAdd.Controls.Add((Label)lblItemDiscLbl);
            flowAdd.Controls.Add(lblMarginPct);
            flowAdd.Controls.Add(nudSalePrice);
            flowAdd.Controls.Add(lblSalePrice);
            flowAdd.Controls.Add(nudPrice);
            flowAdd.Controls.Add((Label)lblPrice);
            flowAdd.Controls.Add(nudQty);
            flowAdd.Controls.Add((Label)lblQty);
            pnlAddRow.Controls.Add(flowAdd);

            pnlHeader.Controls.Add(tbl);

            // ── جدول الأصناف ───────────────────────────────────────────────────
            pnlItems = new Panel { Dock = DockStyle.Fill };
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
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
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID",   Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName",  HeaderText = "الصنف",       ReadOnly = true, FillWeight = 120 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",     HeaderText = "الكمية",      FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",    HeaderText = "سعر الشراء",  FillWeight = 65 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountPct",  HeaderText = "خصم %",       FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice",   HeaderText = "الإجمالي",    ReadOnly = true, FillWeight = 65 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "SuggestedSalePrice", HeaderText = "سعر البيع", FillWeight = 60 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "MarginPct",    HeaderText = "الهامش",      ReadOnly = true, FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewButtonColumn  { Name = "Delete",       HeaderText = "حذف",
                Text = "❌", UseColumnTextForButtonValue = true, FillWeight = 30 });

            dgItems.CellValueChanged  += DgItems_CellValueChanged;
            dgItems.CellClick         += DgItems_CellClick;
            dgItems.CellEndEdit       += (s, e) => RecalcTotals();

            pnlItems.Controls.Add(dgItems);

            // ══════════════════════════════════════════════════════════════════
            // ── الذيل — تخطيط منظم: إجماليات يمين + أزرار يسار ───────────────
            // ══════════════════════════════════════════════════════════════════
            pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 6, 8, 6)
            };

            // ── قسم الإجماليات (يمين) ─────────────────────────────────────────
            var pnlTotals = new Panel
            {
                Width = 650,
                Dock  = DockStyle.Right,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var tblTotals = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 6,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(4)
            };
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // label
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // value
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));  // خصم label
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));  // نوع خصم combo
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45));  // قيمة label
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // قيمة textbox
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            // صف 0: إجمالي الأصناف + خصم الفاتورة
            var lblItemsTotalLbl = new Label
            {
                Text = "إجمالي الأصناف:",
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2)
            };
            // ── قسم الإجماليات: صف 0 (إجمالي + خصم) ────────────────────────
            lblTotalVal = new Label
            {
                Text = "0.00 ج",
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(2)
            };

            lblDiscType = new Label
            {
                Text = "خصم:",
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2)
            };
            cboInvoiceDiscountType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 4, 2, 4)
            };
            cboInvoiceDiscountType.Items.AddRange(new object[] { "مبلغ", "%" });
            cboInvoiceDiscountType.SelectedIndex = 0;
            cboInvoiceDiscountType.SelectedIndexChanged += (s, e) => RecalcTotals();

            lblDiscVal = new Label
            {
                Text = "قيمة:",
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2)
            };
            txtInvoiceDiscount = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "0",
                Margin = new Padding(2, 4, 2, 4)
            };
            txtInvoiceDiscount.TextChanged += (s, e) => RecalcTotals();

            // صف 1: ضريبة + صافي الفاتورة ────────────────────────────────────
            var lblTaxLbl = new Label
            {
                Text = "ضريبة %:",
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2)
            };
            nudTaxPct = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2, Minimum = 0, Maximum = 100,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 4, 2, 4)
            };
            nudTaxPct.ValueChanged += (s, e) => RecalcTotals();

            lblTaxAmt = new Label
            {
                Text = "0.00 ج",
                ForeColor = Color.FromArgb(230, 162, 60),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(2)
            };

            var lblNetTitle = new Label
            {
                Text = "📦 الصافي:",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2)
            };
            lblNetVal = new Label
            {
                Text = "0.00 ج",
                ForeColor = Color.FromArgb(46, 204, 113),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(2)
            };

            // ── ملء جدول الإجماليات ───────────────────────────────────────────
            // صف 0: [إجمالي lbl][إجمالي val][خصم lbl][نوع combo][قيمة lbl][قيمة txt]
            tblTotals.Controls.Add(lblItemsTotalLbl,         0, 0);
            tblTotals.Controls.Add(lblTotalVal,              1, 0);
            tblTotals.Controls.Add(lblDiscType,              2, 0);
            tblTotals.Controls.Add(cboInvoiceDiscountType,   3, 0);
            tblTotals.Controls.Add(lblDiscVal,               4, 0);
            tblTotals.Controls.Add(txtInvoiceDiscount,       5, 0);
            // صف 1: [ضريبة% lbl][nudTax][قيمة الضريبة][صافي lbl][صافي val span2]
            tblTotals.Controls.Add(lblTaxLbl,   0, 1);
            tblTotals.Controls.Add(nudTaxPct,   1, 1);
            tblTotals.Controls.Add(lblTaxAmt,   2, 1);
            tblTotals.Controls.Add(lblNetTitle, 3, 1);
            tblTotals.Controls.Add(lblNetVal,   4, 1);
            tblTotals.SetColumnSpan(lblNetVal, 2);

            pnlTotals.Controls.Add(tblTotals);

            // ── قسم الأزرار (يسار) ────────────────────────────────────────────
            var pnlBtnArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(4, 8, 4, 4)
            };

            btnSave     = Theme.MakeButton("💾 حفظ [F5]",    0, 0, 125, 33, Theme.Accent);
            btnHold     = Theme.MakeButton("⏸️ تعليق [F7]",  0, 0, 120, 33, Color.FromArgb(200, 140, 50));
            btnLoadHold = Theme.MakeButton("📂 معلقات [F8]", 0, 0, 128, 33, Color.FromArgb(100, 100, 160));
            btnNew      = Theme.MakeButton("🆕 جديد [F2]",   0, 0, 115, 33, Color.FromArgb(60, 100, 60));
            btnPrint    = Theme.MakeButton("🖨️ طباعة",       0, 0, 90,  33, Color.FromArgb(80, 80, 80));

            btnSave.Click     += BtnSave_Click;
            btnHold.Click     += BtnHold_Click;
            btnLoadHold.Click += BtnLoadHold_Click;
            btnNew.Click      += (s, e) => ClearInvoice();

            var flowBtns = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.Transparent,
                WrapContents = false,
                Padding = new Padding(0)
            };
            foreach (var b in new[] { btnSave, btnHold, btnLoadHold, btnNew, btnPrint })
            {
                b.Margin = new Padding(0, 0, 6, 0);
                flowBtns.Controls.Add(b);
            }

            var lblHotkeys = new Label
            {
                Text = "[F2] جديد  |  [F5] حفظ  |  [F7] تعليق  |  [F8] معلقات  |  [F12] بحث صنف",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 8f),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 4, 0, 0)
            };

            pnlBtnArea.Controls.Add(lblHotkeys);
            pnlBtnArea.Controls.Add(flowBtns);

            // ── تجميع الذيل ───────────────────────────────────────────────────
            pnlFooter.Controls.Add(pnlBtnArea);
            pnlFooter.Controls.Add(pnlTotals);

            // ── تجميع عناصر النموذج ────────────────────────────────────────────
            base.Controls.Add(pnlItems);
            base.Controls.Add(pnlFooter);
            base.Controls.Add(pnlHeader);
            pnlItems.BringToFront();
            ToggleType();
            Theme.ApplyFormRTL(this);
        }

        // ══════════════════════════════════════════════════════════════════════
        // اختصارات لوحة المفاتيح
        private void FrmPurchase_KeyDown(object sender, KeyEventArgs e)
        {
            if (dgItems.IsCurrentCellInEditMode &&
                (e.KeyCode == Keys.F2 || e.KeyCode == Keys.F5 ||
                 e.KeyCode == Keys.F7 || e.KeyCode == Keys.F8))
            {
                dgItems.EndEdit();
            }

            if      (e.KeyCode == Keys.F2)  { ClearInvoice();          e.Handled = true; }
            else if (e.KeyCode == Keys.F5)  { BtnSave_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F7)  { BtnHold_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F8)  { BtnLoadHold_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F12) { cboProduct.Focus();       e.Handled = true; }
        }

        // تنقل بمفتاح Enter داخل الجدول
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter &&
                (dgItems.Focused || dgItems.EditingControl != null))
            {
                var cur = dgItems.CurrentCell;
                if (cur != null)
                {
                    dgItems.EndEdit();
                    for (int col = cur.ColumnIndex + 1; col < dgItems.ColumnCount; col++)
                    {
                        if (!dgItems.Columns[col].ReadOnly && dgItems.Columns[col].Visible)
                        {
                            dgItems.CurrentCell = dgItems.Rows[cur.RowIndex].Cells[col];
                            dgItems.BeginEdit(true);
                            return true;
                        }
                    }
                    cboProduct.Focus();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // تحذير عند الإغلاق بفاتورة غير محفوظة
        private void FrmPurchase_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isDirty && _items.Count > 0)
            {
                var res = MessageBox.Show(
                    "توجد فاتورة قيد الإدخال لم يتم حفظها.\nهل تريد الإغلاق بدون حفظ؟",
                    "تنبيه", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (res == DialogResult.No)
                    e.Cancel = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // تحديد نوع الفاتورة (آجل / نقدي)
        private void ToggleType()
        {
            bool isCredit = _purchaseType == "Credit";
            btnTypeCredit.BackColor = isCredit ? Theme.Primary : Color.FromArgb(60, 60, 60);
            btnTypeCash.BackColor   = !isCredit ? Theme.Accent : Color.FromArgb(60, 60, 60);

            if (!isCredit)
            {
                var cashResult = DbHelper.Scalar(
                    "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                decimal cashBal = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                lblCashBalance.Text = $"💰 رصيد الخزنة: {cashBal:N2} ج";
                lblCashBalance.ForeColor = cashBal > 0 ? Color.FromArgb(100,180,100) : Color.OrangeRed;
            }
            else
            {
                lblCashBalance.Text = "";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // تحميل الكومبوات
        private void LoadCombos()
        {
            // الموردون
            DataTable dtSup = SupplierDAL.GetAll(true);
            cboSupplier.Items.Clear();
            cboSupplier.Items.Add(new ComboItem(0, "-- اختر المورد --"));
            foreach (DataRow r in dtSup.Rows)
                cboSupplier.Items.Add(new ComboItem(
                    Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
            cboSupplier.DisplayMember = "Text";
            cboSupplier.SelectedIndex = 0;

            // الأصناف
            DataTable dtProd = ProductDAL.GetAll(true);
            cboProduct.Items.Clear();
            cboProduct.Items.Add(new ComboItem(0, "-- اختر الصنف --"));
            foreach (DataRow r in dtProd.Rows)
            {
                var ci = new ComboItem(
                    Convert.ToInt32(r["ProductID"]),
                    r["ProductName"].ToString(),
                    r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m);
                ci.Extra = r["PurchasePrice"] != DBNull.Value
                    ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                cboProduct.Items.Add(ci);
            }
            cboProduct.DisplayMember = "Text";
            cboProduct.SelectedIndex = 0;
            cboProduct.SelectedIndexChanged += (s, e) =>
            {
                if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    nudPrice.Value     = ci.Extra;   // سعر الشراء
                    nudSalePrice.Value = ci.Price;   // سعر البيع
                    UpdateMarginLabel();

                    // ── إضافة تلقائية فور اختيار الصنف ──────────────────────
                    // نؤخر قليلاً لضمان تحديث الـ UI قبل الإضافة
                    var timer = new System.Windows.Forms.Timer { Interval = 50 };
                    timer.Tick += (ts, te) =>
                    {
                        timer.Stop();
                        timer.Dispose();
                        BtnAddItem_Click(null, null);
                    };
                    timer.Start();
                }
                else
                {
                    nudPrice.Value     = 0;
                    nudSalePrice.Value = 0;
                    lblMarginPct.Text  = "0.0%";
                }
            };

            // ── Enter على الكومبو = إضافة + رجوع للكومبو ─────────────────────
            cboProduct.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnAddItem_Click(null, null);
                }
            };

            // المخازن
            try
            {
                var whDt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive=1 ORDER BY WarehouseID");
                cboWarehouse.Items.Clear();
                cboWarehouse.DisplayMember = "Text";
                foreach (DataRow whRow in whDt.Rows)
                    cboWarehouse.Items.Add(new ComboItem(Convert.ToInt32(whRow["WarehouseID"]), whRow["WarehouseName"].ToString()));
                if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
            }
            catch { /* تجاهل لو مافيش مخازن */ }
        }

        // ══════════════════════════════════════════════════════════════════════
        // إضافة صنف
        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (!(cboProduct.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر صنفاً أولاً"); return;
            }
            decimal qty   = nudQty.Value;
            decimal price = nudPrice.Value;
            decimal disc  = nudItemDisc.Value;
            decimal salePrice = nudSalePrice.Value;

            if (qty <= 0 || price <= 0)
            {
                MessageBox.Show("أدخل كمية وسعر صحيحين"); return;
            }

            // دمج إذا كان الصنف موجوداً مسبقاً
            foreach (var item in _items)
            {
                if (item.ProductID == ci.ID)
                {
                    item.Quantity   += qty;
                    if (disc > 0) item.DiscountPct = disc; // تحديث الخصم
                    item.SuggestedSalePrice = salePrice; // تحديث سعر البيع المقترح
                    RefreshGrid();
                    ResetAddRow();
                    return;
                }
            }

            _items.Add(new PurchaseItemDTO
            {
                ProductID   = ci.ID,
                ProductName = ci.Text,
                Quantity    = qty,
                UnitPrice   = price,
                DiscountPct = disc,
                SuggestedSalePrice = salePrice
            });

            RefreshGrid();
            ResetAddRow();
            _isDirty = true;
        }

        private void ResetAddRow()
        {
            cboProduct.SelectedIndex = 0;
            nudQty.Value   = 1;
            nudItemDisc.Value = 0;
            nudSalePrice.Value = 0;
            lblMarginPct.Text = "0.0%";
            cboProduct.Focus();
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── 🔗 طريقة تحديث هامش الربح للإدخال المباشر ─────────────────────────────
        private void UpdateMarginLabel()
        {
            decimal buy = nudPrice.Value;
            decimal sell = nudSalePrice.Value;
            if (buy > 0)
            {
                decimal margin = (sell - buy) / buy * 100m;
                lblMarginPct.Text = margin.ToString("F1") + "%";
                lblMarginPct.ForeColor = margin >= 0 ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60);
            }
            else
            {
                lblMarginPct.Text = "0.0%";
                lblMarginPct.ForeColor = Theme.TextSub;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // تحديث الجدول
        private void RefreshGrid()
        {
            dgItems.CellValueChanged -= DgItems_CellValueChanged;
            dgItems.Rows.Clear();
            foreach (var item in _items)
            {
                decimal buy = item.UnitPrice;
                decimal sell = item.SuggestedSalePrice ?? 0m;
                decimal margin = buy > 0 ? (sell - buy) / buy * 100m : 0m;

                dgItems.Rows.Add(
                    item.ProductID,
                    item.ProductName,
                    item.Quantity.ToString("F3"),
                    item.UnitPrice.ToString("F2"),
                    item.DiscountPct.ToString("F2"),
                    item.TotalPrice.ToString("F2"),
                    sell.ToString("F2"),
                    margin.ToString("F1") + "%");
            }
            dgItems.CellValueChanged += DgItems_CellValueChanged;
            RecalcTotals();
        }

        // ══════════════════════════════════════════════════════════════════════
        // تعديل الكميات/الأسعار/الخصم من الجدول مباشرة
        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var item    = _items[e.RowIndex];
            var colName = dgItems.Columns[e.ColumnIndex].Name;
            var cellVal = dgItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();

            if (colName == "Quantity")
            {
                if (decimal.TryParse(cellVal, out decimal q) && q > 0)
                    item.Quantity = q;
                else
                    dgItems.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity.ToString("F3");
            }
            else if (colName == "UnitPrice")
            {
                if (decimal.TryParse(cellVal, out decimal p) && p > 0)
                {
                    item.UnitPrice = p;
                    decimal sell = item.SuggestedSalePrice ?? 0m;
                    decimal margin = p > 0 ? (sell - p) / p * 100m : 0m;
                    dgItems.Rows[e.RowIndex].Cells["MarginPct"].Value = margin.ToString("F1") + "%";
                }
                else
                    dgItems.Rows[e.RowIndex].Cells["UnitPrice"].Value = item.UnitPrice.ToString("F2");
            }
            else if (colName == "DiscountPct")
            {
                if (decimal.TryParse(cellVal, out decimal d) && d >= 0 && d <= 100)
                {
                    item.DiscountPct = d;
                    item.DiscountAmt = 0m; // مسح القيمة المباشرة عند وجود نسبة
                }
                else
                    dgItems.Rows[e.RowIndex].Cells["DiscountPct"].Value = item.DiscountPct.ToString("F2");
            }
            else if (colName == "SuggestedSalePrice")
            {
                if (decimal.TryParse(cellVal, out decimal s) && s >= 0)
                {
                    item.SuggestedSalePrice = s;
                    decimal buy = item.UnitPrice;
                    decimal margin = buy > 0 ? (s - buy) / buy * 100m : 0m;
                    dgItems.Rows[e.RowIndex].Cells["MarginPct"].Value = margin.ToString("F1") + "%";
                }
                else
                    dgItems.Rows[e.RowIndex].Cells["SuggestedSalePrice"].Value = (item.SuggestedSalePrice ?? 0m).ToString("F2");
            }

            // تحديث عمود الإجمالي
            dgItems.Rows[e.RowIndex].Cells["TotalPrice"].Value = item.TotalPrice.ToString("F2");
            _isDirty = true;
            RecalcTotals();
        }

        // حذف صف بضغطة الزر
        private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "Delete")
            {
                _items.RemoveAt(e.RowIndex);
                RefreshGrid();
                _isDirty = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // حساب الإجماليات (أصناف + خصم الفاتورة + ضريبة = الصافي)
        private void RecalcTotals()
        {
            decimal itemsTotal = 0m;
            foreach (var item in _items) itemsTotal += item.TotalPrice;
            lblTotalVal.Text = itemsTotal.ToString("N2") + " ج";

            // خصم الفاتورة
            decimal discVal = 0m;
            decimal.TryParse(txtInvoiceDiscount.Text, out discVal);
            decimal discAmt = cboInvoiceDiscountType.SelectedIndex == 1
                ? itemsTotal * discVal / 100m
                : discVal;
            decimal afterDisc = Math.Max(0m, itemsTotal - discAmt);

            // ضريبة الشراء
            decimal taxPct = nudTaxPct.Value;
            decimal taxAmt = Math.Round(afterDisc * taxPct / 100m, 2);
            lblTaxAmt.Text = taxAmt.ToString("N2") + " ج";

            // الصافي النهائي
            decimal net = afterDisc + taxAmt;
            lblNetVal.Text = net.ToString("N2") + " ج";
        }

        // ══════════════════════════════════════════════════════════════════════
        // مسح الفاتورة (جديدة)
        private void ClearInvoice()
        {
            _items.Clear();
            RefreshGrid();
            if (cboSupplier.Items.Count > 0) cboSupplier.SelectedIndex = 0;
            if (cboProduct.Items.Count  > 0) cboProduct.SelectedIndex  = 0;
            txtNotes.Clear();
            txtInvoiceDiscount.Text = "0";
            nudTaxPct.Value  = 0;
            nudQty.Value     = 1;
            nudPrice.Value   = 0;
            nudItemDisc.Value = 0;
            _purchaseType    = "Credit";
            _isDirty         = false;
            _draftPurchaseID = 0;
            dtpDate.Value    = DateTime.Today;
            ToggleType();
            this.Text = "فاتورة مشتريات";
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── تعليق الفاتورة ────────────────────────────────────────────────────
        private void BtnHold_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("أضف أصنافاً أولاً للتعليق", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // حذف المسودة القديمة إذا كنا نعيد تعليق مسودة محملة
            if (_draftPurchaseID > 0)
            {
                try { PurchaseDAL.DeleteDraftPurchase(_draftPurchaseID); }
                catch { /* تجاهل */ }
            }

            int? supplierID = GetSelectedSupplier();
            decimal gross, discAmt, discPct, net, taxPct, taxAmt;
            CalcAmounts(out gross, out discAmt, out discPct, out net, out taxPct, out taxAmt);

            int? warehouseID = null;
            if (cboWarehouse.SelectedItem is ComboItem wci) warehouseID = wci.ID;

            try
            {
                int draftID = PurchaseDAL.SavePurchase(
                    _purchaseType, supplierID, net, txtNotes.Text, _items,
                    discAmt, discPct, taxPct, taxAmt, isDraft: true, warehouseID: warehouseID);

                if (draftID > 0)
                {
                    MessageBox.Show(
                        $"✅ تم تعليق الفاتورة بنجاح.\nيمكنك استدعاؤها لاحقاً من زر 📂 معلقات.",
                        "تعليق", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearInvoice();
                }
                else
                {
                    MessageBox.Show("❌ فشل تعليق الفاتورة.", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل تعليق فاتورة المشتريات", ex, "FrmPurchase.BtnHold_Click");
                MessageBox.Show("❌ حدث خطأ:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── استرجاع الفواتير المعلقة ──────────────────────────────────────────
        private void BtnLoadHold_Click(object sender, EventArgs e)
        {
            DataTable dt = PurchaseDAL.GetDraftPurchases();
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد فواتير مشتريات معلقة حالياً.",
                    "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // نافذة قائمة المعلقات
            var dlg = new Form
            {
                Width = 850, Height = 460,
                Text = "📂 فواتير المشتريات المعلقة",
                StartPosition = FormStartPosition.CenterParent,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain
            };

            var dgDrafts = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = dt,
                BackgroundColor = Theme.BgCard,
                RowHeadersVisible = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = Theme.FontMain, BackColor = Theme.BgCard, ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = Theme.FontBold, BackColor = Theme.Primary, ForeColor = Color.White
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dlg.Load += (sl, el) =>
            {
                foreach (DataGridViewColumn col in dgDrafts.Columns)
                {
                    string cn = col.Name;
                    if (cn == "PurchaseID" || cn == "SupplierID" ||
                        cn == "DiscountAmount" || cn == "DiscountPct" ||
                        cn == "TaxPct" || cn == "TaxAmount")
                    {
                        col.Visible = false; continue;
                    }
                    switch (cn)
                    {
                        case "PurchaseCode":  col.HeaderText = "كود الفاتورة";  break;
                        case "PurchaseDate":  col.HeaderText = "التاريخ";       break;
                        case "PurchaseType":  col.HeaderText = "النوع";         break;
                        case "SupplierName":  col.HeaderText = "المورد";        break;
                        case "TotalAmount":   col.HeaderText = "الإجمالي";     break;
                        case "Notes":         col.HeaderText = "ملاحظات";       break;
                    }
                }
            };

            var pnlBtns = new Panel
            {
                Dock = DockStyle.Bottom, Height = 48,
                BackColor = Theme.BgCard, Padding = new Padding(8)
            };

            var btnLoad = Theme.MakeButton("✅ استدعاء الفاتورة", 0, 6, 175, 35, Theme.Success);
            btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoad.Click += (s2, e2) =>
            {
                if (dgDrafts.SelectedRows.Count == 0) return;
                var row = (DataRowView)dgDrafts.SelectedRows[0].DataBoundItem;

                if (_isDirty && _items.Count > 0)
                {
                    if (MessageBox.Show(
                        "توجد فاتورة حالية قيد الإدخال — سيتم مسحها لتحميل الفاتورة المعلقة.\nهل أنت متأكد؟",
                        "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                }

                int pid = Convert.ToInt32(row["PurchaseID"]);
                ClearInvoice();
                _draftPurchaseID = pid;

                // نوع الفاتورة
                string typeStr = row["PurchaseType"].ToString();
                _purchaseType = typeStr;
                ToggleType();

                // المورد
                if (row["SupplierID"] != DBNull.Value)
                {
                    int sid = Convert.ToInt32(row["SupplierID"]);
                    for (int i = 0; i < cboSupplier.Items.Count; i++)
                    {
                        if (cboSupplier.Items[i] is ComboItem ci && ci.ID == sid)
                        {
                            cboSupplier.SelectedIndex = i; break;
                        }
                    }
                }

                // التاريخ والملاحظات
                dtpDate.Value = Convert.ToDateTime(row["PurchaseDate"]);
                txtNotes.Text = row["Notes"].ToString();

                // الخصم
                decimal dAmt = Convert.ToDecimal(row["DiscountAmount"]);
                decimal dPct = Convert.ToDecimal(row["DiscountPct"]);
                if (dPct > 0)
                {
                    cboInvoiceDiscountType.SelectedIndex = 1;
                    txtInvoiceDiscount.Text = dPct.ToString("G29");
                }
                else
                {
                    cboInvoiceDiscountType.SelectedIndex = 0;
                    txtInvoiceDiscount.Text = dAmt.ToString("G29");
                }

                // الضريبة
                decimal tPct = Convert.ToDecimal(row["TaxPct"]);
                nudTaxPct.Value = tPct > 100 ? 100 : tPct;

                // الأصناف
                var itemsDt = PurchaseDAL.GetItems(pid);
                _items.Clear();
                foreach (DataRow iRow in itemsDt.Rows)
                {
                    _items.Add(new PurchaseItemDTO
                    {
                        ProductID   = Convert.ToInt32(iRow["ProductID"]),
                        ProductName = iRow["ProductName"].ToString(),
                        Quantity    = Convert.ToDecimal(iRow["Quantity"]),
                        UnitPrice   = Convert.ToDecimal(iRow["UnitPrice"]),
                        DiscountPct = Convert.ToDecimal(iRow["DiscountPct"]),
                        DiscountAmt = Convert.ToDecimal(iRow["DiscountAmt"]),
                        SuggestedSalePrice = iRow["SuggestedSalePrice"] != DBNull.Value ? Convert.ToDecimal(iRow["SuggestedSalePrice"]) : (decimal?)null
                    });
                }
                RefreshGrid();
                _isDirty = true;
                this.Text = $"فاتورة مشتريات — مستردة [مسودة #{pid}]";

                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            var btnDelDraft = Theme.MakeButton("❌ حذف المعلقة", 185, 6, 145, 35,
                Color.FromArgb(180, 60, 60));
            btnDelDraft.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDelDraft.Click += (s2, e2) =>
            {
                if (dgDrafts.SelectedRows.Count == 0) return;
                var row = (DataRowView)dgDrafts.SelectedRows[0].DataBoundItem;
                if (MessageBox.Show("هل أنت متأكد من حذف هذه الفاتورة المعلقة نهائياً؟",
                    "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    PurchaseDAL.DeleteDraftPurchase(Convert.ToInt32(row["PurchaseID"]));
                    dgDrafts.DataSource = PurchaseDAL.GetDraftPurchases();
                    if (((DataTable)dgDrafts.DataSource).Rows.Count == 0)
                        dlg.Close();
                }
            };

            pnlBtns.Controls.Add(btnLoad);
            pnlBtns.Controls.Add(btnDelDraft);
            dlg.Controls.Add(dgDrafts);
            dlg.Controls.Add(pnlBtns);
            dlg.ShowDialog(this);
        }

        // ══════════════════════════════════════════════════════════════════════
        // حفظ الفاتورة النهائية
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("أضف أصنافاً أولاً", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? supplierID = GetSelectedSupplier();
            if (_purchaseType == "Credit" && !supplierID.HasValue)
            {
                MessageBox.Show("اختر المورد أولاً للفواتير الآجلة", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal gross, discAmt, discPct, net, taxPct, taxAmt;
            CalcAmounts(out gross, out discAmt, out discPct, out net, out taxPct, out taxAmt);

            // 1. تحقق من تعديل أسعار البيع المقترحة للأصناف
            var changedPricesList = new List<string>();
            var itemsToUpdate = new List<PurchaseItemDTO>();
            
            foreach (var item in _items)
            {
                if (item.SuggestedSalePrice.HasValue)
                {
                    var currentPriceObj = DbHelper.Scalar("SELECT SalePrice FROM Products WHERE ProductID = @id", DbHelper.P("@id", item.ProductID));
                    decimal currentPrice = currentPriceObj != null ? Convert.ToDecimal(currentPriceObj) : 0m;
                    
                    if (item.SuggestedSalePrice.Value != currentPrice)
                    {
                        changedPricesList.Add($"• {item.ProductName}: السعر الحالي {currentPrice:N2} ج -> المقترح {item.SuggestedSalePrice.Value:N2} ج");
                        itemsToUpdate.Add(item);
                    }
                }
            }

            string priceDecision = "Ignore";
            if (itemsToUpdate.Count > 0)
            {
                using (var dlg = new FrmPriceUpdateDecision(string.Join("\r\n", changedPricesList)))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        priceDecision = dlg.Decision;
                    }
                    else
                    {
                        // نلغي عملية الحفظ بالكامل لو أغلق المستخدم الشاشة لتجنب حفظ خاطئ
                        return;
                    }
                }
            }

            // إذا كنا نحفظ مسودة محملة — نحذفها أولاً
            if (_draftPurchaseID > 0)
            {
                try { PurchaseDAL.DeleteDraftPurchase(_draftPurchaseID); }
                catch { /* تجاهل */ }
                _draftPurchaseID = 0;
            }

            try
            {
                int? warehouseID = null;
                if (cboWarehouse.SelectedItem is ComboItem wci2) warehouseID = wci2.ID;

                int id = PurchaseDAL.SavePurchase(
                    _purchaseType, supplierID, net, txtNotes.Text, _items,
                    discAmt, discPct, taxPct, taxAmt, isDraft: false, warehouseID: warehouseID);

                if (id > 0)
                {
                    // تطبيق قرار تعديل أسعار البيع
                    if (priceDecision == "ApplyNow")
                    {
                        foreach (var item in itemsToUpdate)
                        {
                            ProductDAL.SetPendingPrice(item.ProductID, item.SuggestedSalePrice.Value, item.UnitPrice, applyNow: true);
                        }
                    }
                    else if (priceDecision == "Pending")
                    {
                        foreach (var item in itemsToUpdate)
                        {
                            ProductDAL.SetPendingPrice(item.ProductID, item.SuggestedSalePrice.Value, item.UnitPrice, applyNow: false);
                        }
                    }
                    else
                    {
                        // حتى لو تم تجاهل سعر البيع، نقوم بتحديث سعر التكلفة (سعر الشراء الأخير) لكل صنف
                        foreach (var item in _items)
                        {
                            DbHelper.Execute(
                                "UPDATE Products SET CostPrice = @cp, PurchasePrice = @cp WHERE ProductID = @id",
                                DbHelper.P("@cp", item.UnitPrice),
                                DbHelper.P("@id", item.ProductID));
                        }
                    }

                    _lastPurchaseID = id;
                    MessageBox.Show(
                        $"✅ تم حفظ فاتورة المشتريات بنجاح\nرقم الفاتورة: PUR-{id}" +
                        (taxAmt > 0 ? $"\n(شاملة ضريبة {taxPct:N2}% = {taxAmt:N2} ج)" : "") +
                        (priceDecision == "Pending" ? "\n⚠️ تم تعليق أسعار البيع الجديدة وسوف تتفعل تلقائياً عند نفاد الكميات الحالية." : ""),
                        "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearInvoice();
                    LoadCombos();
                }
                else
                {
                    MessageBox.Show("❌ فشل حفظ الفاتورة", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل حفظ فاتورة المشتريات", ex, "FrmPurchase.BtnSave_Click");
                MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // مساعدات خاصة

        private int? GetSelectedSupplier()
        {
            if (cboSupplier.SelectedItem is ComboItem ci && ci.ID > 0)
                return ci.ID;
            return null;
        }

        private void CalcAmounts(
            out decimal gross, out decimal discAmt, out decimal discPct,
            out decimal net,   out decimal taxPct,  out decimal taxAmt)
        {
            gross = 0m;
            foreach (var item in _items) gross += item.TotalPrice;

            decimal rawDisc = 0m;
            decimal.TryParse(txtInvoiceDiscount.Text, out rawDisc);

            if (cboInvoiceDiscountType.SelectedIndex == 1) // نسبة %
            {
                discPct = rawDisc;
                discAmt = Math.Round(gross * discPct / 100m, 2);
            }
            else // مبلغ
            {
                discAmt = rawDisc;
                discPct = gross > 0 ? Math.Round(discAmt / gross * 100m, 2) : 0m;
            }

            decimal afterDisc = Math.Max(0m, gross - discAmt);

            taxPct = nudTaxPct.Value;
            taxAmt = Math.Round(afterDisc * taxPct / 100m, 2);

            net = afterDisc + taxAmt;
        }
    }

    /// <summary>نافذة حوار منسقة لاختيار طريقة تعديل أسعار البيع</summary>
    public class FrmPriceUpdateDecision : Form
    {
        public string Decision { get; private set; } = "Ignore"; // "ApplyNow", "Pending", "Ignore"

        public FrmPriceUpdateDecision(string itemsText)
        {
            this.Text = "تحديث أسعار البيع";
            this.Size = new Size(500, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            var lblMsg = new Label
            {
                Text = "تم تغيير أسعار البيع المقترحة للأصناف التالية:",
                Location = new Point(15, 15),
                Size = new Size(460, 25),
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };

            var txtItems = new TextBox
            {
                Text = itemsText,
                Location = new Point(15, 45),
                Size = new Size(460, 140),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnApplyNow = Theme.MakeButton("⚡ تطبيق فوري", 15, 205, 130, 35, Theme.Success);
            var btnPending = Theme.MakeButton("⏳ سعر معلق (حسب الكمية)", 155, 205, 190, 35, Theme.Accent);
            var btnIgnore = Theme.MakeButton("❌ تجاهل التغييرات", 355, 205, 120, 35, Color.FromArgb(120, 120, 120));

            btnApplyNow.Click += (s, e) => { Decision = "ApplyNow"; this.DialogResult = DialogResult.OK; this.Close(); };
            btnPending.Click += (s, e) => { Decision = "Pending"; this.DialogResult = DialogResult.OK; this.Close(); };
            btnIgnore.Click += (s, e) => { Decision = "Ignore"; this.DialogResult = DialogResult.OK; this.Close(); };

            this.Controls.Add(lblMsg);
            this.Controls.Add(txtItems);
            this.Controls.Add(btnApplyNow);
            this.Controls.Add(btnPending);
            this.Controls.Add(btnIgnore);
            
            Theme.ApplyFormRTL(this);
        }
    }
}

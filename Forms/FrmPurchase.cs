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
        private TextBox txtNotes, txtSupplierInvoiceNo;
        private Label lblCashBalance;
        private Button btnSearchProduct;

        // ── حقول إضافة صنف ─────────────────────────────────────────────────────

        // ── جدول الأصناف ───────────────────────────────────────────────────────
        private DataGridView dgItems;
        private Panel pnlItems, pnlFooter;
        private Button btnCustomizeCols; // زر تخصيص الأعمدة

        // ── الذيل — خصم الفاتورة ───────────────────────────────────────────────
        private Label lblTotalVal, lblNetVal, lblDiscType, lblDiscVal;
        private Label lblItemCount;
        private TextBox txtInvoiceDiscount;
        private ComboBox cboInvoiceDiscountType;

        // ── الذيل — ضريبة الشراء ومصاريف الشحن ──────────────────────────────────
        private NumericUpDown nudTaxPct;
        private Label lblTaxAmt;
        private TextBox txtShippingCost;
        private ComboBox cboShippingOn;
        private Label lblShippingDisplay;

        // ── أزرار الذيل ────────────────────────────────────────────────────────
        private Button btnSave, btnNew, btnPrint, btnHold, btnLoadHold;

        // ── بيانات الفاتورة ────────────────────────────────────────────────────
        private List<PurchaseItemDTO> _items = new List<PurchaseItemDTO>();
        private int _lastPurchaseID = 0;
        private bool _isDirty = false;
        private bool _isScanningBarcode = false;
        private int _supplierId = 0;
        private decimal? _pendingBarcodeWeight = null;
        private decimal? _pendingScaleWeight = null; 
        private int _draftPurchaseID = 0; // 0 = فاتورة جديدة، >0 = مسودة محملة
        private int _editPurchaseID = 0;
        private bool _isCopyMode = false;
        private DateTime? _loadedLastModified = null;

        public FrmPurchase(int purchaseID, bool isCopyMode = false) : this()
        {
            _editPurchaseID = isCopyMode ? 0 : purchaseID;
            _isCopyMode = isCopyMode;
            if (purchaseID > 0)
            {
                LoadInvoiceForEdit(purchaseID);
            }
        }

        // ── Auto-barcode detection ─────────────────────────────────────────────
        private System.Windows.Forms.Timer _barcodeTimer;
        private DateTime _lastKeyTime = DateTime.MinValue;
        private const int BARCODE_INTERVAL_MS = 50;
        private const int BARCODE_MIN_LENGTH = 4;
        private int _pendingRowIdx = -1; // سطر إدخال الكود المعلق
        private int? _pendingMatchedUnit = null;

        // ══════════════════════════════════════════════════════════════════════
        public FrmPurchase()
        {
            InitUI();
            LoadCombos();
            ClearInvoice();
            
            if (AppConfig.ScaleEnabled)
            {
                ScaleService.Instance.WeightChanged += ScaleService_WeightChanged;
            }
            this.Load += (s, e) =>
            {
                try
                {
                    if (cboProduct != null && cboProduct.Visible && cboProduct.Enabled)
                    {
                        this.ActiveControl = cboProduct;
                        cboProduct.Focus();
                    }
                }
                catch { /* تجاهل أخطاء التركيز عند فتح الشاشة */ }
            };
        }

        private void ScaleService_WeightChanged(decimal weight, bool isStable)
        {
            if (isStable)
            {
                _pendingScaleWeight = weight;
            }
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
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.KeyPreview = true;
            this.KeyDown += FrmPurchase_KeyDown;
            this.FormClosing += FrmPurchase_FormClosing;
            // ── تهيئة Timer الباركود التلقائي ──
            _barcodeTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _barcodeTimer.Tick += BarcodeTimer_Tick;

            // ══════════════════════════════════════════════════════════════════
            // ── لوحة الرأس — تستخدم TableLayoutPanel للتخطيط المنظم ──────────
            // ══════════════════════════════════════════════════════════════════
            var pnlHeader = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 165,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 5, 12, 5)
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
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

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
            var pnlSupplier = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
            cboSupplier = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 3, 2, 3)
            };
            Button btnSupplierAdd = new Button
            {
                Text = "➕",
                Width = 30,
                Font = Theme.FontBold,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Success,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left,
                Margin = new Padding(2)
            };
            btnSupplierAdd.FlatAppearance.BorderSize = 0;
            btnSupplierAdd.Click += (s, e) =>
            {
                new FrmSuppliers().ShowDialog();
                // Reload combos
                LoadCombos();
                // Try to select the latest supplier
                object latestIdObj = DbHelper.Scalar("SELECT TOP 1 SupplierID FROM Suppliers ORDER BY SupplierID DESC");
                if (latestIdObj != null && int.TryParse(latestIdObj.ToString(), out int latestId) && latestId > 0)
                {
                    for (int i = 0; i < cboSupplier.Items.Count; i++)
                    {
                        if (cboSupplier.Items[i] is ComboItem ci && ci.ID == latestId)
                        {
                            cboSupplier.SelectedIndex = i;
                            break;
                        }
                    }
                }
            };
            pnlSupplier.Controls.Add(cboSupplier);
            pnlSupplier.Controls.Add(btnSupplierAdd);
            cboSupplier.SendToBack();

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
                Margin = new Padding(2, 3, 2, 3),
                Enabled = Session.CanAccess("EditInvoiceDate")
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
            tbl.Controls.Add(pnlSupplier,   1, 0);
            tbl.Controls.Add(lblDate,       2, 0);
            tbl.Controls.Add(dtpDate,       3, 0);
            tbl.Controls.Add(lblType,       4, 0);
            tbl.Controls.Add(pnlTypeBtns,   5, 0);

            // ── صف 1: رقم فاتورة المورد | ملاحظات | الصنف ───────────────────────────
            var lblSupplierInv = MakeLabel("رقم فاتورة المورد:", 0, 0);
            lblSupplierInv.Dock = DockStyle.Fill;
            lblSupplierInv.TextAlign = ContentAlignment.MiddleRight;
            lblSupplierInv.Margin = new Padding(2);
            txtSupplierInvoiceNo = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 3, 2, 3)
            };

            var lblNotes = MakeLabel("ملاحظات:", 0, 0);
            lblNotes.Dock = DockStyle.Fill;
            lblNotes.TextAlign = ContentAlignment.MiddleRight;
            lblNotes.Margin = new Padding(2);
            txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Margin = new Padding(2, 3, 2, 3)
            };

            var lblProd = MakeLabel("الصنف:", 0, 0);
            lblProd.Dock = DockStyle.Fill;
            lblProd.TextAlign = ContentAlignment.MiddleRight;
            lblProd.Margin = new Padding(2);

            var pnlProduct = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
            cboProduct = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 3, 2, 3)
            };
            
            var btnManualAdd = new Button
            {
                Text = "➕",
                Width = 30,
                Height = 24,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left,
                Margin = new Padding(2, 3, 2, 3)
            };
            btnManualAdd.FlatAppearance.BorderSize = 0;
            btnManualAdd.Click += BtnManualAdd_Click;

            pnlProduct.Controls.Add(cboProduct);
            pnlProduct.Controls.Add(btnManualAdd);

            // إضافة — صف 1
            tbl.Controls.Add(lblSupplierInv,       0, 1);
            tbl.Controls.Add(txtSupplierInvoiceNo, 1, 1);
            tbl.Controls.Add(lblNotes,             2, 1);
            tbl.Controls.Add(txtNotes,             3, 1);
            tbl.Controls.Add(lblCashBalance,       4, 1);
            tbl.SetColumnSpan(lblCashBalance, 2);

            cboProduct.KeyDown += CboProduct_KeyDown;
            cboProduct.KeyPress += CboProduct_KeyPress_BarcodeDetect; // اكتشاف الباركود التلقائي

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
                Margin = new Padding(2, 3, 2, 3)
            };

            btnSearchProduct = new Button
            {
                Text = "🔍 بحث صنف",
                Dock = DockStyle.Fill,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 3, 2, 3),
                Font = Theme.FontBold
            };
            btnSearchProduct.FlatAppearance.BorderSize = 0;
            btnSearchProduct.Click += (s, e) =>
            {
                // فتح نافذة بحث الأصناف (وضع الشراء)
                using (var dlgSearch = new FrmProductSearch(isPurchaseMode: true))
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

                        ComboItem prodItem = GetProductComboItem(dlgSearch.SelectedProductID);
                        string prodCode = prodItem?.ProductCode ?? "";
                        string prodName = prodItem?.Text ?? "";

                        decimal qty = dlgSearch.SelectedQuantity > 0 ? dlgSearch.SelectedQuantity : 1m;
                        decimal purchasePrice = dlgSearch.SelectedPurchasePrice > 0 ? dlgSearch.SelectedPurchasePrice : dlgSearch.SelectedPrice;
                        decimal salePrice = dlgSearch.SelectedSalePrice > 0 ? dlgSearch.SelectedSalePrice : dlgSearch.SelectedPrice;
                        decimal discount = dlgSearch.SelectedDiscount;

                        AddProductToGrid(
                            dlgSearch.SelectedProductID,
                            prodCode,
                            prodName,
                            qty,
                            purchasePrice,
                            discount,
                            salePrice
                        );

                        if (!string.IsNullOrEmpty(dlgSearch.SelectedUnitName) && _items.Count > 0)
                        {
                            var lastItem = _items.FindLast(i => i.ProductID == dlgSearch.SelectedProductID);
                            if (lastItem != null)
                            {
                                lastItem.UnitName = dlgSearch.SelectedUnitName;
                                RefreshGrid();
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

            // ── تمت إزالة صف الإضافة ────────────────────────────────────────────────

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
                ScrollBars = ScrollBars.Both,
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
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode",  HeaderText = "كود الصنف", ReadOnly = false, FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName",  HeaderText = "الصنف",       ReadOnly = true, FillWeight = 120 });
            dgItems.Columns.Add(new DataGridViewComboBoxColumn { Name = "UnitName", HeaderText = "الوحدة", ReadOnly = false, FillWeight = 40f });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",     HeaderText = "الكمية",      FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",    HeaderText = "سعر الشراء",  FillWeight = 65 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountPct",  HeaderText = "خصم %",       FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice",   HeaderText = "الإجمالي",    ReadOnly = true, FillWeight = 65 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "SuggestedSalePrice", HeaderText = "سعر البيع", FillWeight = 60 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "MarginPct",    HeaderText = "الهامش",      ReadOnly = true, FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate",   HeaderText = "تاريخ الصلاحية", FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            dgItems.Columns.Add(new DataGridViewButtonColumn  { Name = "Delete",       HeaderText = "حذف",
                Text = "❌", UseColumnTextForButtonValue = true, FillWeight = 30 });
            Theme.AdjustGridHeaders(dgItems);

            foreach (DataGridViewColumn col in dgItems.Columns)
            {
                col.MinimumWidth = 95;
            }
            if (dgItems.Columns.Contains("ProductName"))
            {
                dgItems.Columns["ProductName"].MinimumWidth = 160;
            }

            dgItems.AllowUserToOrderColumns = Session.CanOrderColumns("Purchases");
            Session.LoadColumnOrder(dgItems, "Purchases");

            dgItems.CellValueChanged  += DgItems_CellValueChanged;
            dgItems.CellClick         += DgItems_CellClick;
            dgItems.CellEndEdit       += DgItems_CellEndEdit_Purchase;
            dgItems.EditingControlShowing += DgItems_EditingControlShowing;

            dgItems.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    string colName = dgItems.Columns[e.ColumnIndex].Name;
                    if (colName == "Quantity" || colName == "UnitPrice" || colName == "DiscountPct" || colName == "SuggestedSalePrice" || colName == "UnitName" || colName == "ExpiryDate" || colName == "Delete")
                    {
                        return; // السماح بتعديل الخانات التفاعلية أو حذف السطر مباشرة
                    }
                }
                btnSearchProduct.PerformClick();
            };

            dgItems.DoubleClick += (s, e) =>
            {
                if (dgItems.SelectedCells.Count == 0 || (dgItems.CurrentCell != null && dgItems.CurrentCell.ReadOnly))
                {
                    btnSearchProduct.PerformClick();
                }
            };

            // سهم لأسفل في آخر سطر → سطر كود جديد | Insert = نفس الشيء
            dgItems.KeyDown += (s, ke) =>
            {
                if (ke.KeyCode == Keys.Down && dgItems.CurrentCell != null)
                {
                    int lastReal = _items.Count - 1;
                    if (dgItems.CurrentCell.RowIndex >= lastReal && _pendingRowIdx < 0)
                    {
                        ke.Handled = true;
                        AddNewCodeRow();
                    }
                }
                else if (ke.KeyCode == Keys.Insert)
                {
                    ke.Handled = true;
                    AddNewCodeRow();
                }
            };

            pnlItems.Controls.Add(dgItems);

            // ── زر تخصيص الأعمدة ⚙️ (يظهر في زاوية الجدول) ─────────────────────
            btnCustomizeCols = new Button
            {
                Text      = "⚙️ الأعمدة",
                Size      = new Size(90, 26),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left,
                Location  = new Point(5, 5),
                BackColor = Color.FromArgb(55, 65, 81),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnCustomizeCols.FlatAppearance.BorderSize = 0;
            btnCustomizeCols.Click += (s, e) => ShowColumnCustomizer();
            btnCustomizeCols.Visible = Session.CanOrderColumns("Purchases");
            pnlItems.Controls.Add(btnCustomizeCols);
            btnCustomizeCols.BringToFront();

            // تحميل إعدادات الأعمدة المحفوظة
            LoadColumnSettings();

            // ══════════════════════════════════════════════════════════════════
            // ── الذيل — تخطيط منظم: إجماليات يمين + أزرار يسار ───────────────
            // ══════════════════════════════════════════════════════════════════
            pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 82,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 4, 8, 4)
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
                RowCount = 3,
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
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

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
            lblItemCount = new Label
            {
                Text = "📦 عدد الأصناف: 0",
                ForeColor = Theme.TextSub,
                Font = Theme.FontMain,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(2)
            };

            // صف 1: [ضريبة% lbl][nudTax][قيمة الضريبة][صافي lbl][صافي val][عدد الأصناف]
            tblTotals.Controls.Add(lblTaxLbl,   0, 1);
            tblTotals.Controls.Add(nudTaxPct,   1, 1);
            tblTotals.Controls.Add(lblTaxAmt,   2, 1);
            tblTotals.Controls.Add(lblNetTitle, 3, 1);
            tblTotals.Controls.Add(lblNetVal,   4, 1);
            tblTotals.Controls.Add(lblItemCount, 5, 1);

            // صف 2: مصاريف الشحن ─────────────────────────────────────────────────

            var lblShippingLbl = new Label
            {
                Text = "🚚 مصاريف الشحن:",
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2)
            };
            txtShippingCost = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "0",
                Margin = new Padding(2, 4, 2, 4)
            };
            txtShippingCost.TextChanged += (s, e) => RecalcTotals();

            var lblShippingOnLbl = new Label
            {
                Text = "على:",
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2)
            };
            cboShippingOn = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 4, 2, 4)
            };
            cboShippingOn.Items.AddRange(new object[] { "الشركة", "المورد" });
            cboShippingOn.SelectedIndex = 0; // الافتراضي: على الشركة
            cboShippingOn.SelectedIndexChanged += (s, e) => RecalcTotals();

            lblShippingDisplay = new Label
            {
                Text = "0.00 ج",
                ForeColor = Color.FromArgb(52, 152, 219),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(2)
            };

            tblTotals.Controls.Add(lblShippingLbl,    0, 2);
            tblTotals.Controls.Add(txtShippingCost,   1, 2);
            tblTotals.Controls.Add(lblShippingOnLbl,  2, 2);
            tblTotals.Controls.Add(cboShippingOn,     3, 2);
            tblTotals.Controls.Add(lblShippingDisplay, 4, 2);

            pnlTotals.Controls.Add(tblTotals);

            // ── قسم الأزرار (في الجانب الأيسر) ───────────────────────────────
            var pnlSideButtons = new Panel
            {
                Dock = DockStyle.Left,
                Width = 140,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 15, 8, 15)
            };

            btnSave     = Theme.MakeButton("💾 حفظ [F5]",    0, 0, 120, 40, Theme.Accent);
            btnHold     = Theme.MakeButton("⏸️ تعليق [F7]",  0, 0, 120, 40, Color.FromArgb(200, 140, 50));
            btnLoadHold = Theme.MakeButton("📂 معلقات [F8]", 0, 0, 120, 40, Color.FromArgb(100, 100, 160));
            Button btnSarf = Theme.MakeButton("💵 صرف", 0, 0, 120, 40, Theme.Success);
            btnNew      = Theme.MakeButton("🆕 جديد [F2]",   0, 0, 120, 40, Color.FromArgb(60, 100, 60));
            btnPrint    = Theme.MakeButton("🖨️ طباعة",       0, 0, 120, 40, Color.FromArgb(80, 80, 80));
            Button btnWhatsApp = Theme.MakeButton("📲 واتساب", 0, 0, 120, 40, Color.FromArgb(37, 211, 102));

            btnSave.Click     += BtnSave_Click;
            btnHold.Click     += BtnHold_Click;
            btnLoadHold.Click += BtnLoadHold_Click;
            btnSarf.Click     += (s, e) => MessageBox.Show("تحت التطوير", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnNew.Click      += (s, e) => ClearInvoice();
            btnWhatsApp.Click += (s, e) => MessageBox.Show("تحت التطوير", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var flowBtns = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                WrapContents = false,
                Padding = new Padding(0)
            };
            foreach (var b in new[] { btnSave, btnHold, btnLoadHold, btnSarf, btnNew, btnPrint, btnWhatsApp })
            {
                b.Margin = new Padding(0, 0, 0, 8);
                flowBtns.Controls.Add(b);
            }

            pnlSideButtons.Controls.Add(flowBtns);

            // ── تجميع الذيل ───────────────────────────────────────────────────
            pnlFooter.Controls.Add(pnlTotals);

            // ── تجميع عناصر النموذج ────────────────────────────────────────────
            var pnlScrollWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                RightToLeft = RightToLeft.No // منع الـ RTL على لوحة التمرير لتفادي مشكلة اختفاء شريط التمرير في WinForms
            };

            var pnlFormContent = new Panel
            {
                Dock = DockStyle.Top,
                Height = 630,
                BackColor = Color.Transparent,
                RightToLeft = RightToLeft.Yes // تفعيل الـ RTL على المحتوى الداخلي لتنسيق الحقول والجدول باللغة العربية
            };

            var lblHotkeys = new Label
            {
                Text = "[F2] جديد\n[F5] حفظ\n[F7] تعليق\n[F8] معلقات\n[F12] بحث صنف",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Bottom,
                Height = 100,
                TextAlign = ContentAlignment.BottomCenter,
                Margin = new Padding(0, 10, 0, 0)
            };

            pnlSideButtons.Controls.Add(lblHotkeys);

            pnlFormContent.Controls.Add(pnlItems);
            pnlFormContent.Controls.Add(pnlSideButtons);
            pnlFormContent.Controls.Add(pnlFooter);
            pnlFormContent.Controls.Add(pnlHeader);
            pnlItems.BringToFront();

            pnlScrollWrapper.Controls.Add(pnlFormContent);
            base.Controls.Add(pnlScrollWrapper);

            // تعديل ارتفاع المحتوى ديناميكياً للتجاوب مع تكبير حجم الشاشة أو تصغيرها
            pnlScrollWrapper.Resize += (s, e) =>
            {
                pnlFormContent.Height = Math.Max(630, pnlScrollWrapper.Height - 2);
            };

            ToggleType();
            Theme.ApplyFormRTL(this);
            ApplyInputStyles(this);
        }

        private void ApplyInputStyles(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox || c is ComboBox || c is DateTimePicker || c is NumericUpDown)
                {
                    c.BackColor = Theme.BgInput;
                    c.ForeColor = Theme.TextInput;
                }
                else if (c.HasChildren)
                {
                    ApplyInputStyles(c);
                }
            }
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

			// ─── اختصارات تغيير الوحدات بالكيبورد (Ctrl + 1/2/3) ───
			if (e.Control && (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1 || e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2 || e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3))
			{
				if (dgItems.CurrentRow != null && dgItems.CurrentRow.Index >= 0 && dgItems.CurrentRow.Index < _items.Count)
				{
					int rowIndex = dgItems.CurrentRow.Index;
					var dto = _items[rowIndex];
					ComboItem prod = GetProductComboItem(dto.ProductID);
					if (prod != null)
					{
						string targetUnit = null;
						if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
						{
							targetUnit = prod.BaseUnitName; // الوحدة الكبرى
						}
						else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
						{
							targetUnit = prod.Unit2Name; // الوحدة المتوسطة
						}
						else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
						{
							targetUnit = prod.Unit1Name; // الوحدة الصغرى
						}

						if (!string.IsNullOrEmpty(targetUnit))
						{
							if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
							
							if (dgItems.Rows[rowIndex].Cells["UnitName"] is DataGridViewComboBoxCell cell)
							{
								if (cell.Items.Contains(targetUnit))
								{
									cell.Value = targetUnit;
									HandleUnitChange(dgItems.Rows[rowIndex], dto, targetUnit);
									e.Handled = true;
								}
								else
								{
									MessageBox.Show($"⚠️ الوحدة '{targetUnit}' غير متوفرة لهذا الصنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
								}
							}
						}
					}
				}
			}

            if      (e.KeyCode == Keys.F2)  { ClearInvoice();           e.Handled = true; }
            else if (e.KeyCode == Keys.F5)  { BtnSave_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F7)  { BtnHold_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F8)  { BtnLoadHold_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F12) { cboProduct.Focus();       e.Handled = true; }
            else if (e.KeyCode == Keys.F3)  { btnSearchProduct.PerformClick(); e.Handled = true; } // F3 = شاشة البحث
        }

        // تنقل بمفتاح Enter داخل الجدول
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Insert)
            {
                AddNewCodeRow();
                return true;
            }
            if (keyData == Keys.Enter &&
                (dgItems.Focused || dgItems.EditingControl != null))
            {
                dgItems.EndEdit();
                var cur = dgItems.CurrentCell;
                if (cur != null && cur.RowIndex >= 0 && cur.RowIndex < dgItems.Rows.Count)
                {
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
                else
                {
                    cboProduct.Focus();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // تحذير عند الإغلاق بفاتورة غير محفوظة
        private void FrmPurchase_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AppConfig.ScaleEnabled)
            {
                ScaleService.Instance.WeightChanged -= ScaleService_WeightChanged;
            }
            if (_isDirty && _items.Count > 0)
            {
                var res = MessageBox.Show(
                    "توجد فاتورة قيد الإدخال لم يتم حفظها.\nهل تريد الإغلاق بدون حفظ؟",
                    "تنبيه", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (res == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            // ✅ حفظ ترتيب الأعمدة عند إغلاق الشاشة
            if (!e.Cancel)
            {
                SaveColumnSettings();
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
            cboSupplier.BeginUpdate();
            cboSupplier.Items.Clear();
            List<ComboItem> supplierItems = new List<ComboItem>();
            supplierItems.Add(new ComboItem(0, "-- اختر المورد --"));
            foreach (DataRow r in dtSup.Rows)
                supplierItems.Add(new ComboItem(
                    Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
            cboSupplier.Items.AddRange(supplierItems.ToArray());
            cboSupplier.DisplayMember = "Text";
            cboSupplier.SelectedIndex = 0;
            cboSupplier.EndUpdate();

            // الأصناف
            DataTable dtProd = ProductDAL.GetAll(true);
            cboProduct.BeginUpdate();
            cboProduct.Items.Clear();
            List<ComboItem> productItems = new List<ComboItem>();
            productItems.Add(new ComboItem(0, "-- اختر الصنف --"));
            foreach (DataRow r in dtProd.Rows)
            {
                var ci = new ComboItem(
                    Convert.ToInt32(r["ProductID"]),
                    r["ProductName"].ToString(),
                    r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m);
                ci.Extra = r["PurchasePrice"] != DBNull.Value
                    ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                ci.ProductCode = r["ProductCode"]?.ToString() ?? "";
                ci.InternationalCode = r["InternationalCode"]?.ToString() ?? "";
                ci.PartNumber = r["PartNumber"]?.ToString() ?? "";

                // وحدات متعددة
                ci.BaseUnitName = r["Unit"]?.ToString() ?? "";
                ci.Unit1Name = r["Unit1Name"] != DBNull.Value ? r["Unit1Name"].ToString() : null;
                ci.Unit1SalePrice = r["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit1SalePrice"]) : 0m;
                ci.Unit1PurchasePrice = r["Unit1PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit1PurchasePrice"]) : 0m;
                ci.Unit1Factor = 1m;
                ci.Unit2Name = r["Unit2Name"] != DBNull.Value ? r["Unit2Name"].ToString() : null;
                ci.Unit2Factor = r["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit2Factor"]) : 1m;
                ci.Unit2SalePrice = r["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit2SalePrice"]) : 0m;
                ci.Unit2PurchasePrice = r["Unit2PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit2PurchasePrice"]) : 0m;
                ci.Unit3Factor = r["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit3Factor"]) : 1m;
                ci.Unit1Barcode = r["Unit1Barcode"] != DBNull.Value ? r["Unit1Barcode"].ToString() : "";
                ci.Unit2Barcode = r["Unit2Barcode"] != DBNull.Value ? r["Unit2Barcode"].ToString() : "";
                ci.HasExpiry = r.Table.Columns.Contains("HasExpiry") && r["HasExpiry"] != DBNull.Value && Convert.ToBoolean(r["HasExpiry"]);
                ci.DefaultExpiryDays = r.Table.Columns.Contains("DefaultExpiryDays") && r["DefaultExpiryDays"] != DBNull.Value ? Convert.ToInt32(r["DefaultExpiryDays"]) : (int?)null;

                productItems.Add(ci);
            }
            cboProduct.Items.AddRange(productItems.ToArray());
            cboProduct.DisplayMember = "Text";
            cboProduct.SelectedIndex = 0;
            cboProduct.EndUpdate();
            SetupSearchableCombo(cboProduct);
            cboProduct.SelectedIndexChanged += (s, e) =>
            {
                if (_isScanningBarcode) return;
                if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    decimal price = ci.Extra;
                    decimal salePrice = ci.Price;
                    
                    var timer = new System.Windows.Forms.Timer { Interval = 50 };
                    timer.Tick += (ts, te) =>
                    {
                        timer.Stop();
                        timer.Dispose();
                        if (_isScanningBarcode) return;
                        decimal qtyToAdd = _pendingBarcodeWeight ?? (_pendingScaleWeight ?? 1m);
                        _pendingBarcodeWeight = null;
                        _pendingScaleWeight = null;
                        AddProductToGrid(ci.ID, ci.ProductCode, ci.Text, qtyToAdd, price, 0m, salePrice);
                        cboProduct.SelectedIndex = 0;
                    };
                    timer.Start();
                }
            };

            // المخازن
            try
            {
                var whDt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive=1 ORDER BY WarehouseID");
                cboWarehouse.BeginUpdate();
                cboWarehouse.Items.Clear();
                List<ComboItem> warehouseItems = new List<ComboItem>();
                foreach (DataRow whRow in whDt.Rows)
                    warehouseItems.Add(new ComboItem(Convert.ToInt32(whRow["WarehouseID"]), whRow["WarehouseName"].ToString()));
                cboWarehouse.Items.AddRange(warehouseItems.ToArray());
                cboWarehouse.DisplayMember = "Text";
                if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
                cboWarehouse.EndUpdate();
            }
            catch { /* تجاهل لو مافيش مخازن */ }
        }

        private ComboItem GetProductComboItem(int productID)
        {
            foreach (var item in cboProduct.Items)
            {
                if (item is ComboItem ci && ci.ID == productID)
                    return ci;
            }
            if (cboProduct.Tag is List<ComboItem> allItems)
            {
                foreach (var ci in allItems)
                {
                    if (ci.ID == productID)
                        return ci;
                }
            }
            return null;
        }

        // ══════════════════════════════════════════════════════════════════════
        // إضافة صنف
        private void AddProductToGrid(int prodId, string prodCode, string prodName, decimal qty, decimal price, decimal disc, decimal salePrice)
        {
            if (prodId <= 0) return;

            ComboItem product = GetProductComboItem(prodId);

            string defaultUnit = null;
            decimal defaultFactor = 1m;
            decimal defaultPrice = price;
            decimal defaultSalePrice = salePrice;

            int matchedUnit = _pendingMatchedUnit ?? 3;
            _pendingMatchedUnit = null; // إعادة تعيين

            if (product != null)
            {
                defaultUnit = !string.IsNullOrEmpty(product.Unit1Name) ? product.Unit1Name
                            : !string.IsNullOrEmpty(product.BaseUnitName) ? product.BaseUnitName
                            : null;

                if (matchedUnit == 1 && !string.IsNullOrEmpty(product.Unit1Name))
                {
                    defaultUnit = product.Unit1Name;
                    defaultFactor = 1m;
                    defaultPrice = product.Unit1PurchasePrice > 0 ? product.Unit1PurchasePrice : price;
                    defaultSalePrice = product.Unit1SalePrice > 0 ? product.Unit1SalePrice : salePrice;
                }
                else if (matchedUnit == 2 && !string.IsNullOrEmpty(product.Unit2Name))
                {
                    defaultUnit = product.Unit2Name;
                    defaultFactor = product.Unit2Factor > 0 ? product.Unit2Factor : 1m;
                    defaultPrice = product.Unit2PurchasePrice > 0 ? product.Unit2PurchasePrice : price;
                    defaultSalePrice = product.Unit2SalePrice > 0 ? product.Unit2SalePrice : salePrice;
                }
                else if (matchedUnit == 3)
                {
                    if (!string.IsNullOrEmpty(product.BaseUnitName))
                    {
                        defaultUnit = product.BaseUnitName;
                        defaultFactor = (product.Unit3Factor > 0 ? product.Unit3Factor : 1m) * (product.Unit2Factor > 0 ? product.Unit2Factor : 1m);
                        defaultPrice = product.PurchasePrice > 0 ? product.PurchasePrice : price;
                        defaultSalePrice = product.Price > 0 ? product.Price : salePrice;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(product.Unit1Name))
                    {
                        defaultPrice = product.Unit1PurchasePrice > 0 ? product.Unit1PurchasePrice : price;
                        defaultSalePrice = product.Unit1SalePrice > 0 ? product.Unit1SalePrice : salePrice;
                    }
                }
            }

            // دمج إذا كان الصنف موجوداً مسبقاً بنفس الوحدة
            foreach (var item in _items)
            {
                if (item.ProductID == prodId && item.UnitName == defaultUnit)
                {
                    item.Quantity += qty;
                    RefreshGrid();
                    SelectQuantityCell(prodId);
                    return;
                }
            }

            DateTime? defaultExpiry = null;
            if (product != null && product.HasExpiry)
            {
                int days = product.DefaultExpiryDays ?? 0;
                if (days > 0)
                {
                    defaultExpiry = DateTime.Today.AddDays(days);
                }
            }

            _items.Add(new PurchaseItemDTO
            {
                ProductID   = prodId,
                ProductCode = prodCode,
                ProductName = prodName,
                Quantity    = qty,
                UnitPrice   = defaultPrice,
                DiscountPct = disc,
                SuggestedSalePrice = defaultSalePrice,
                UnitName = defaultUnit,
                Factor = defaultFactor,
                ExpiryDate = defaultExpiry
            });

            RefreshGrid();
            SelectQuantityCell(prodId);
            _isDirty = true;
        }

        private bool MatchBarcode(string barcodes, string scanText)
        {
            if (string.IsNullOrEmpty(barcodes)) return false;
            var parts = barcodes.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (string.Equals(part.Trim(), scanText, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void BtnManualAdd_Click(object sender, EventArgs e)
        {
            AddNewCodeRow();
        }

        /// <summary>يضيف سطراً فارغاً ويضع الكيرسور على عمود كود الصنف</summary>
        private void AddNewCodeRow()
        {
            if (_pendingRowIdx >= 0 && _pendingRowIdx < dgItems.Rows.Count)
            {
                var prevCell = dgItems.Rows[_pendingRowIdx].Cells["ProductCode"];
                if (prevCell.Value == null || string.IsNullOrEmpty(prevCell.Value.ToString()))
                    dgItems.Rows.RemoveAt(_pendingRowIdx);
            }
            _pendingRowIdx = dgItems.Rows.Add();
            dgItems.Rows[_pendingRowIdx].DefaultCellStyle.BackColor = Color.FromArgb(30, 120, 190, 80);
            try
            {
                dgItems.ClearSelection();
                dgItems.CurrentCell = dgItems.Rows[_pendingRowIdx].Cells["ProductCode"];
                dgItems.BeginEdit(true);
                dgItems.FirstDisplayedScrollingRowIndex = _pendingRowIdx;
            }
            catch { }
        }

        private void DgItems_CellEndEdit_Purchase(object sender, DataGridViewCellEventArgs e)
        {
            // معالجة عمود كود الصنف (السطر المعلق)
            if (e.RowIndex == _pendingRowIdx && e.ColumnIndex >= 0
                && dgItems.Columns[e.ColumnIndex].Name == "ProductCode")
            {
                string code  = dgItems.Rows[e.RowIndex].Cells["ProductCode"].Value?.ToString()?.Trim() ?? "";
                int rowIdx   = e.RowIndex;
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (string.IsNullOrEmpty(code))
                    {
                        if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
                            dgItems.Rows.RemoveAt(rowIdx);
                        _pendingRowIdx = -1;
                        return;
                    }
                    var dt = ProductDAL.FindByCode(code);
                    if (dt.Rows.Count > 0)
                    {
                        var row     = dt.Rows[0];
                        int pid     = Convert.ToInt32(row["ProductID"]);
                        string pCode= row["ProductCode"].ToString();
                        string pName= row["ProductName"].ToString();
                        decimal pp  = row["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["PurchasePrice"]) : 0m;
                        decimal sp  = row["SalePrice"]     != DBNull.Value ? Convert.ToDecimal(row["SalePrice"])     : 0m;

                        if (dt.Columns.Contains("MatchedUnit") && row["MatchedUnit"] != DBNull.Value)
                        {
                            _pendingMatchedUnit = Convert.ToInt32(row["MatchedUnit"]);
                        }
                        else
                        {
                            _pendingMatchedUnit = 3; // Default to main/base unit
                        }

                        if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
                            dgItems.Rows.RemoveAt(rowIdx);
                        _pendingRowIdx = -1;

                        AddProductToGrid(pid, pCode, pName, 1.00m, pp, 0m, sp);
                        // فتح سطر جديد للإدخال التالي
                        AddNewCodeRow();
                    }
                    else
                    {
                        MessageBox.Show("❌ لم يتم العثور على صنف بالكود: " + code, "خطأ في الكود", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
                        {
                            dgItems.CurrentCell = dgItems.Rows[rowIdx].Cells["ProductCode"];
                            dgItems.BeginEdit(true);
                        }
                    }
                });
                return;
            }
            RecalcTotals();
        }

        private void CboProduct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(cboProduct.Text))
            {
                var res = BarcodeParser.Parse(cboProduct.Text);
                
                // Get unfiltered product list
                List<ComboItem> allItems = cboProduct.Tag as List<ComboItem>;
                if (allItems == null)
                {
                    allItems = new List<ComboItem>();
                    foreach (var item in cboProduct.Items)
                    {
                        if (item is ComboItem ci) allItems.Add(ci);
                    }
                }

				ComboItem foundItem = null;

                if (res.IsScaleBarcode)
                {
                    _pendingBarcodeWeight = res.WeightOrPrice;
                    
                    // Search in unfiltered list
                    foreach (var ci in allItems)
                    {
                        if (ci.ID > 0 && ci.ID.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode)
                        {
                            foundItem = ci;
                            break;
                        }
                    }
                    if (foundItem == null)
                    {
                        MessageBox.Show("لم يتم العثور على الصنف الخاص بباركود الميزان!");
						_pendingBarcodeWeight = null;
                        return;
                    }
                }
                else
                {
                    string scanText = cboProduct.Text.Trim();
                    foreach (var ci in allItems)
                    {
                        if (ci.ID > 0 && 
                            (string.Equals(ci.ProductCode, scanText, StringComparison.OrdinalIgnoreCase) || 
                             string.Equals(ci.PartNumber, scanText, StringComparison.OrdinalIgnoreCase) || 
                             MatchBarcode(ci.InternationalCode, scanText)))
                        {
                            foundItem = ci;
                            break;
                        }
                    }
                }

                if (foundItem != null)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    decimal qtyToAdd = _pendingBarcodeWeight ?? (_pendingScaleWeight ?? 1m);
                    _pendingBarcodeWeight = null;
                    _pendingScaleWeight = null;

                    _isScanningBarcode = true;
                    try
                    {
                        decimal price = foundItem.Extra;
                        decimal salePrice = foundItem.Price;
                        AddProductToGrid(foundItem.ID, foundItem.ProductCode, foundItem.Text, qtyToAdd, price, 0m, salePrice);
                        
                        cboProduct.Text = "";
                        cboProduct.Items.Clear();
                        cboProduct.Items.AddRange(allItems.ToArray());
                        cboProduct.SelectedIndex = 0;
                        cboProduct.Focus();
                    }
                    finally
                    {
                        _isScanningBarcode = false;
                    }
                    return;
                }
            }
        }

        // ── الاكتشاف التلقائي للباركود ──────────────────────────────────────
        private void CboProduct_KeyPress_BarcodeDetect(object sender, KeyPressEventArgs e)
        {
            var now = DateTime.Now;
            var interval = (now - _lastKeyTime).TotalMilliseconds;
            _lastKeyTime = now;
            _barcodeTimer.Stop();
            if (interval <= BARCODE_INTERVAL_MS || interval == (DateTime.Now - DateTime.MinValue).TotalMilliseconds)
                _barcodeTimer.Start();
        }

        private void BarcodeTimer_Tick(object sender, EventArgs e)
        {
            _barcodeTimer.Stop();
            string text = cboProduct.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Length < BARCODE_MIN_LENGTH) return;

            var res = BarcodeParser.Parse(text);

            List<ComboItem> allItems = cboProduct.Tag as List<ComboItem>;
            if (allItems == null)
            {
                allItems = new List<ComboItem>();
                foreach (var item in cboProduct.Items)
                    if (item is ComboItem ci) allItems.Add(ci);
            }

            ComboItem foundItem = null;

            if (res.IsScaleBarcode)
            {
                _pendingBarcodeWeight = res.WeightOrPrice;
                foreach (var ci in allItems)
                {
                    if (ci.ID > 0 && ci.ID.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode)
                    {
                        foundItem = ci;
                        break;
                    }
                }
                if (foundItem == null) { _pendingBarcodeWeight = null; return; }
            }
            else
            {
                foreach (var ci in allItems)
                {
                    if (ci.ID > 0 &&
                        (string.Equals(ci.ProductCode, text, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(ci.PartNumber, text, StringComparison.OrdinalIgnoreCase) ||
                         MatchBarcode(ci.InternationalCode, text)))
                    {
                        foundItem = ci;
                        break;
                    }
                }
            }

            if (foundItem != null)
            {
                decimal qtyToAdd = _pendingBarcodeWeight ?? (_pendingScaleWeight ?? 1m);
                _pendingBarcodeWeight = null;
                _pendingScaleWeight = null;

                _isScanningBarcode = true;
                try
                {
                    decimal price = foundItem.Extra;
                    decimal salePrice = foundItem.Price;
                    AddProductToGrid(foundItem.ID, foundItem.ProductCode, foundItem.Text, qtyToAdd, price, 0m, salePrice);

                    cboProduct.Text = "";
                    cboProduct.Items.Clear();
                    cboProduct.Items.AddRange(allItems.ToArray());
                    cboProduct.SelectedIndex = 0;
                    cboProduct.Focus();
                }
                finally
                {
                    _isScanningBarcode = false;
                }
            }
        }

        private void SelectQuantityCell(int prodId)
        {
            if (_isScanningBarcode) return;
            if (dgItems.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in dgItems.Rows)
                {
                    if (row.Cells["ProductID"].Value != null && Convert.ToInt32(row.Cells["ProductID"].Value) == prodId)
                    {
                        dgItems.CurrentCell = row.Cells["Quantity"];
                        dgItems.BeginEdit(true);
                        break;
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // تحديث الجدول
        private void RefreshGrid()
        {
            _pendingRowIdx = -1;
            dgItems.CellValueChanged -= DgItems_CellValueChanged;
            dgItems.Rows.Clear();
            foreach (var item in _items)
            {
                decimal buy = item.UnitPrice;
                decimal sell = item.SuggestedSalePrice ?? 0m;
                decimal margin = buy > 0 ? (sell - buy) / buy * 100m : 0m;

                int rIdx = dgItems.Rows.Add(
                    item.ProductID,
                    item.ProductCode,
                    item.ProductName,
                    null, // UnitName
                    item.Quantity.ToString("F3"),
                    item.UnitPrice.ToString("F2"),
                    item.DiscountPct.ToString("F2"),
                    item.TotalPrice.ToString("F2"),
                    sell.ToString("F2"),
                    margin.ToString("F1") + "%",
                    item.ExpiryDate?.ToString("yyyy-MM-dd") ?? "");
                // عمود الكود للسطور المضافة = قراءة فقط
                dgItems.Rows[rIdx].Cells["ProductCode"].ReadOnly = true;

                // ─── تهيئة ComboBox الوحدة ──────────────────────────────────────────
                if (dgItems.Columns.Contains("UnitName") && dgItems.Columns["UnitName"] is DataGridViewComboBoxColumn unitCol)
                {
                    var unitCell = (DataGridViewComboBoxCell)dgItems.Rows[rIdx].Cells["UnitName"];
                    var unitList = new System.Collections.ArrayList();

                    ComboItem prod = GetProductComboItem(item.ProductID);
                    if (prod != null)
                    {
                        // 1. الوحدة الكبرى (الأساسية)
                        if (!string.IsNullOrEmpty(prod.BaseUnitName))
                        {
                            unitList.Add(prod.BaseUnitName);
                        }
                        else
                        {
                            unitList.Add("وحدة");
                        }

                        // 2. الوحدة الوسطى (إن وُجدت)
                        if (!string.IsNullOrEmpty(prod.Unit2Name))
                        {
                            unitList.Add(prod.Unit2Name);
                        }

                        // 3. الوحدة الصغرى (إن وُجدت وليست مكررة مع الكبرى)
                        if (!string.IsNullOrEmpty(prod.Unit1Name) && prod.Unit1Name != prod.BaseUnitName)
                        {
                            unitList.Add(prod.Unit1Name);
                        }
                    }
                    else
                    {
                        unitList.Add(!string.IsNullOrEmpty(item.UnitName) ? item.UnitName : "وحدة");
                    }

                    unitCell.DataSource = unitList;
                    string savedUnit = item.UnitName;
                    if (!string.IsNullOrEmpty(savedUnit) && unitList.Contains(savedUnit))
                        unitCell.Value = savedUnit;
                    else if (unitList.Count > 0)
                        unitCell.Value = unitList[0];
                }
            }
            dgItems.CellValueChanged += DgItems_CellValueChanged;
            RecalcTotals();
        }

        private void HandleUnitChange(DataGridViewRow row, PurchaseItemDTO dto, string newUnit)
        {
            if (string.IsNullOrEmpty(newUnit)) return;
            ComboItem prod = GetProductComboItem(dto.ProductID);
            if (prod == null) return;

            dto.UnitName = newUnit;

            if (!string.IsNullOrEmpty(prod.Unit2Name) && newUnit == prod.Unit2Name)
            {
                // 1. الوحدة الوسطى
                dto.Factor = prod.Unit2Factor > 0 ? prod.Unit2Factor : 1m;
                if (prod.Unit2PurchasePrice > 0) dto.UnitPrice = prod.Unit2PurchasePrice;
                if (prod.Unit2SalePrice > 0) dto.SuggestedSalePrice = prod.Unit2SalePrice;
            }
            else if (!string.IsNullOrEmpty(prod.Unit1Name) && newUnit == prod.Unit1Name)
            {
                // 2. الوحدة الصغرى (التجزئة)
                dto.Factor = 1m;
                if (prod.Unit1PurchasePrice > 0) dto.UnitPrice = prod.Unit1PurchasePrice;
                else dto.UnitPrice = prod.PurchasePrice;
                if (prod.Unit1SalePrice > 0) dto.SuggestedSalePrice = prod.Unit1SalePrice;
                else dto.SuggestedSalePrice = prod.Price;
            }
            else if (!string.IsNullOrEmpty(prod.BaseUnitName) && newUnit == prod.BaseUnitName)
            {
                // 3. الوحدة الكبرى (الأساسية)
                dto.Factor = (prod.Unit3Factor > 0 ? prod.Unit3Factor : 1m) * (prod.Unit2Factor > 0 ? prod.Unit2Factor : 1m);
                dto.UnitPrice = prod.PurchasePrice;
                dto.SuggestedSalePrice = prod.Price;
            }
            else
            {
                // احتياطي
                dto.Factor = 1m;
                dto.UnitPrice = prod.PurchasePrice;
                dto.SuggestedSalePrice = prod.Price;
            }

            // تحديث الجدول
            row.Cells["UnitPrice"].Value = dto.UnitPrice.ToString("F2");
            row.Cells["TotalPrice"].Value = dto.TotalPrice.ToString("F2");
            row.Cells["SuggestedSalePrice"].Value = (dto.SuggestedSalePrice ?? 0m).ToString("F2");

            decimal buy = dto.UnitPrice;
            decimal sell = dto.SuggestedSalePrice ?? 0m;
            decimal margin = buy > 0 ? (sell - buy) / buy * 100m : 0m;
            row.Cells["MarginPct"].Value = margin.ToString("F1") + "%";

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

            if (colName == "UnitName")
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (e.RowIndex >= 0 && e.RowIndex < _items.Count)
                        HandleUnitChange(dgItems.Rows[e.RowIndex], _items[e.RowIndex], cellVal);
                });
                return;
            }

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
            else if (colName == "ExpiryDate")
            {
                var parsedDate = DbHelper.ParseExpiryInput(cellVal);
                if (parsedDate.HasValue)
                {
                    item.ExpiryDate = parsedDate.Value;
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (e.RowIndex >= 0 && e.RowIndex < dgItems.Rows.Count)
                        {
                            dgItems.Rows[e.RowIndex].Cells["ExpiryDate"].Value = parsedDate.Value.ToString("yyyy-MM-dd");
                        }
                    });
                }
                else if (string.IsNullOrWhiteSpace(cellVal))
                {
                    item.ExpiryDate = null;
                }
                else
                {
                    dgItems.Rows[e.RowIndex].Cells["ExpiryDate"].Value = item.ExpiryDate?.ToString("yyyy-MM-dd") ?? "";
                }
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
            else if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "UnitName")
            {
                dgItems.CurrentCell = dgItems.Rows[e.RowIndex].Cells[e.ColumnIndex];
                dgItems.BeginEdit(true);
            }
        }

        private void DgItems_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dgItems.CurrentCell != null && dgItems.CurrentCell.OwningColumn.Name == "UnitName")
            {
                if (e.Control is ComboBox comboBox)
                {
                    comboBox.DroppedDown = true;
                }
            }
        }

        // حساب الإجماليات (أصناف + خصم + ضريبة + شحن = الصافي)
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

            // مصاريف الشحن
            decimal shippingCost = 0m;
            decimal.TryParse(txtShippingCost?.Text, out shippingCost);
            bool onCompany = cboShippingOn == null || cboShippingOn.SelectedIndex == 0;
            decimal shippingEffect = onCompany ? shippingCost : -shippingCost;
            if (lblShippingDisplay != null)
                lblShippingDisplay.Text = (onCompany ? "+" : "-") + shippingCost.ToString("N2") + " ج";

            // الصافي النهائي
            decimal net = afterDisc + taxAmt + shippingEffect;
            lblNetVal.Text = net.ToString("N2") + " ج";

            if (lblItemCount != null)
            {
                lblItemCount.Text = "📦 عدد الأصناف: " + _items.Count;
            }
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
            if (txtSupplierInvoiceNo != null) txtSupplierInvoiceNo.Clear();
            if (txtShippingCost != null) txtShippingCost.Text = "0";
            if (cboShippingOn != null) cboShippingOn.SelectedIndex = 0;
            txtInvoiceDiscount.Text = "0";
            nudTaxPct.Value  = 0;
            txtNotes.Text    = "";
            _purchaseType    = "Credit";
            _isDirty         = false;
            _draftPurchaseID = 0;
            _editPurchaseID  = 0;
            _isCopyMode      = false;
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
            decimal gross, discAmt, discPct, net, taxPct, taxAmt, shippingCost;
            string shippingOn;
            CalcAmounts(out gross, out discAmt, out discPct, out net, out taxPct, out taxAmt, out shippingCost, out shippingOn);

            int? warehouseID = null;
            if (cboWarehouse.SelectedItem is ComboItem wci) warehouseID = wci.ID;

            try
            {
                int draftID = PurchaseDAL.SavePurchase(
                    _purchaseType, supplierID, net, txtNotes.Text, _items,
                    discAmt, discPct, taxPct, taxAmt, isDraft: true, warehouseID: warehouseID,
                    supplierInvoiceNo: txtSupplierInvoiceNo != null ? txtSupplierInvoiceNo.Text.Trim() : "",
                    shippingCost: shippingCost, shippingOn: shippingOn);

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
                decimal dAmt = row.Row.Table.Columns.Contains("DiscountAmount") && row["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmount"]) : 0m;
                decimal dPct = row.Row.Table.Columns.Contains("DiscountPct") && row["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(row["DiscountPct"]) : 0m;
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

                // مصاريف الشحن
                if (txtShippingCost != null)
                {
                    decimal sCost = row.Row.Table.Columns.Contains("ShippingCost") && row["ShippingCost"] != DBNull.Value ? Convert.ToDecimal(row["ShippingCost"]) : 0m;
                    txtShippingCost.Text = sCost.ToString("G29");
                }
                if (cboShippingOn != null)
                {
                    string sOn = row.Row.Table.Columns.Contains("ShippingOn") && row["ShippingOn"] != DBNull.Value ? row["ShippingOn"].ToString() : "Company";
                    cboShippingOn.SelectedIndex = (sOn == "Supplier") ? 1 : 0;
                }

                // الأصناف
                var itemsDt = PurchaseDAL.GetItems(pid);
                _items.Clear();
                foreach (DataRow iRow in itemsDt.Rows)
                {
                    _items.Add(new PurchaseItemDTO
                    {
                        ProductID   = Convert.ToInt32(iRow["ProductID"]),
                        ProductCode = iRow["ProductCode"].ToString(),
                        ProductName = iRow["ProductName"].ToString(),
                        Quantity    = Convert.ToDecimal(iRow["Quantity"]),
                        UnitPrice   = Convert.ToDecimal(iRow["UnitPrice"]),
                        DiscountPct = iRow.Table.Columns.Contains("DiscountPct") && iRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(iRow["DiscountPct"]) : 0m,
                        DiscountAmt = iRow.Table.Columns.Contains("DiscountAmt") && iRow["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(iRow["DiscountAmt"]) : 0m,
                        SuggestedSalePrice = iRow["SuggestedSalePrice"] != DBNull.Value ? Convert.ToDecimal(iRow["SuggestedSalePrice"]) : (decimal?)null,
                        UnitName = iRow.Table.Columns.Contains("UnitName") && iRow["UnitName"] != DBNull.Value ? iRow["UnitName"].ToString() : null,
                        Factor = iRow.Table.Columns.Contains("Factor") && iRow["Factor"] != DBNull.Value ? Convert.ToDecimal(iRow["Factor"]) : 1.0m,
                        ExpiryDate = iRow.Table.Columns.Contains("ExpiryDate") && iRow["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(iRow["ExpiryDate"]) : (DateTime?)null
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

            // تحقق من وجود تاريخ صلاحية للأصناف التي تتطلب ذلك
            foreach (var item in _items)
            {
                var hasExpiryObj = DbHelper.Scalar("SELECT HasExpiry FROM Products WHERE ProductID = @id", DbHelper.P("@id", item.ProductID));
                bool prodHasExpiry = hasExpiryObj != null && Convert.ToBoolean(hasExpiryObj);
                if (prodHasExpiry)
                {
                    if (!item.ExpiryDate.HasValue)
                    {
                        MessageBox.Show($"❌ خطأ: الصنف \"{item.ProductName}\" له تاريخ صلاحية ويجب تسجيل تاريخ الصلاحية له قبل الحفظ!", "تنبيه تاريخ الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            decimal gross, discAmt, discPct, net, taxPct, taxAmt, shippingCost;
            string shippingOn;
            CalcAmounts(out gross, out discAmt, out discPct, out net, out taxPct, out taxAmt, out shippingCost, out shippingOn);

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

                int id = 0;
                string suppInvNo = txtSupplierInvoiceNo != null ? txtSupplierInvoiceNo.Text.Trim() : "";
                if (_editPurchaseID > 0)
                {
                    bool ok = PurchaseDAL.UpdatePurchase(_editPurchaseID, _purchaseType, supplierID, net, txtNotes.Text, _items,
                        discAmt, discPct, taxPct, taxAmt, warehouseID, supplierInvoiceNo: suppInvNo,
                        shippingCost: shippingCost, shippingOn: shippingOn);
                    if (ok) id = _editPurchaseID;
                }
                else
                {
                    id = PurchaseDAL.SavePurchase(
                        _purchaseType, supplierID, net, txtNotes.Text, _items,
                        discAmt, discPct, taxPct, taxAmt, isDraft: false, warehouseID: warehouseID, supplierInvoiceNo: suppInvNo,
                        shippingCost: shippingCost, shippingOn: shippingOn);
                }

                if (id > 0)
                {
                    // تطبيق قرار تعديل أسعار البيع
                    if (priceDecision == "ApplyNow")
                    {
                        foreach (var item in itemsToUpdate)
                        {
                            ProductDAL.SetPendingPrice(item.ProductID, item.SuggestedSalePrice.Value, item.UnitPrice, applyNow: true, purchaseID: id);
                        }
                    }
                    else if (priceDecision == "Pending")
                    {
                        foreach (var item in itemsToUpdate)
                        {
                            ProductDAL.SetPendingPrice(item.ProductID, item.SuggestedSalePrice.Value, item.UnitPrice, applyNow: false, purchaseID: id);
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
                        $"✅ تم حفظ فاتورة المشتريات بنجاح\nرقم الفاتورة: {id}" +
                        (taxAmt > 0 ? $"\n(شاملة ضريبة {taxPct:N2}% = {taxAmt:N2} ج)" : "") +
                        (priceDecision == "Pending" ? "\n⚠️ تم تعليق أسعار البيع الجديدة وسوف تتفعل تلقائياً عند نفاد الكميات الحالية." : ""),
                        "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    try
                    {
                        using (var printDlg = new FrmPrintPurchaseBarcodes(id, id.ToString()))
                        {
                            printDlg.ShowDialog(this);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("Failed to open FrmPrintPurchaseBarcodes automatically", ex, "FrmPurchase.BtnSave_Click");
                    }

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
            out decimal net,   out decimal taxPct,  out decimal taxAmt,
            out decimal shippingCost, out string shippingOn)
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

            shippingCost = 0m;
            decimal.TryParse(txtShippingCost?.Text, out shippingCost);
            shippingOn = (cboShippingOn != null && cboShippingOn.SelectedIndex == 1) ? "Supplier" : "Company";
            decimal shippingEffect = (shippingOn == "Company") ? shippingCost : -shippingCost;

            net = afterDisc + taxAmt + shippingEffect;
        }

        private void SetupSearchableCombo(ComboBox cbo)
        {
            cbo.AutoCompleteMode = AutoCompleteMode.None;
            cbo.TextUpdate += delegate
            {
                if (cbo.Tag == null)
                {
                    List<ComboItem> list = new List<ComboItem>();
                    foreach (ComboItem item in cbo.Items)
                    {
                        list.Add(item);
                    }
                    cbo.Tag = list;
                }
                List<ComboItem> list2 = (List<ComboItem>)cbo.Tag;
                string text = cbo.Text;
                cbo.BeginUpdate();
                cbo.Items.Clear();
                if (string.IsNullOrWhiteSpace(text))
                {
                    cbo.Items.AddRange(list2.ToArray());
                }
                else
                {
                    List<ComboItem> filtered = new List<ComboItem>();
                    if (list2.Count > 0 && list2[0].ID == 0)
                    {
                        filtered.Add(list2[0]);
                    }
                    int count = 0;
                    foreach (ComboItem item2 in list2)
                    {
                        if (item2.ID == 0) continue;
                        if (item2.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (item2.ProductCode != null && item2.ProductCode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (item2.PartNumber != null && item2.PartNumber.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (item2.InternationalCode != null && item2.InternationalCode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            filtered.Add(item2);
                            count++;
                            if (count >= 100)
                                break;
                        }
                    }
                    cbo.Items.AddRange(filtered.ToArray());
                }
                cbo.EndUpdate();
                cbo.SelectionStart = text.Length;
                cbo.SelectionLength = 0;
                if (!cbo.DroppedDown)
                {
                    cbo.DroppedDown = true;
                    Cursor.Current = Cursors.Default;
                }
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── تخصيص أعمدة الجدول ────────────────────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>يفتح نافذة تخصيص الأعمدة (إظهار/إخفاء + ترتيب)</summary>
        private void ShowColumnCustomizer()
        {
            var dlg = new Form
            {
                Text            = "⚙️ تخصيص أعمدة الفاتورة",
                Size            = new Size(360, 480),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                RightToLeft     = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor       = Color.FromArgb(30, 30, 45),
                Font            = new Font("Segoe UI", 10f)
            };

            var lblHint = new Label
            {
                Text      = "✅ تفعيل/إيقاف الأعمدة  |  ▲▼ لتغيير الترتيب",
                Dock      = DockStyle.Top,
                Height    = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(150, 200, 255),
                Font      = new Font("Segoe UI", 9f)
            };

            var clb = new CheckedListBox
            {
                Dock            = DockStyle.Fill,
                CheckOnClick    = true,
                BackColor       = Color.FromArgb(40, 42, 58),
                ForeColor       = Color.White,
                BorderStyle     = BorderStyle.None,
                Font            = new Font("Segoe UI", 10f),
                RightToLeft     = RightToLeft.Yes
            };

            // ملء القائمة بالأعمدة (ما عدا عمود الحذف)
            foreach (DataGridViewColumn col in dgItems.Columns)
            {
                if (col.Name == "Delete") continue;
                clb.Items.Add(new ColEntry(col.Name, col.HeaderText), col.Visible);
            }

            // أزرار ▲▼
            var btnUp   = new Button { Text = "▲ أعلى",   Width = 90, Height = 30, BackColor = Color.FromArgb(55,65,81), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnDown = new Button { Text = "▼ أسفل",   Width = 90, Height = 30, BackColor = Color.FromArgb(55,65,81), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnUp.FlatAppearance.BorderSize = btnDown.FlatAppearance.BorderSize = 0;

            btnUp.Click += (s, e) =>
            {
                int i = clb.SelectedIndex;
                if (i <= 0) return;
                var item    = clb.Items[i];
                bool chk    = clb.GetItemChecked(i);
                clb.Items.RemoveAt(i);
                clb.Items.Insert(i - 1, item);
                clb.SetItemChecked(i - 1, chk);
                clb.SelectedIndex = i - 1;
            };
            btnDown.Click += (s, e) =>
            {
                int i = clb.SelectedIndex;
                if (i < 0 || i >= clb.Items.Count - 1) return;
                var item    = clb.Items[i];
                bool chk    = clb.GetItemChecked(i);
                clb.Items.RemoveAt(i);
                clb.Items.Insert(i + 1, item);
                clb.SetItemChecked(i + 1, chk);
                clb.SelectedIndex = i + 1;
            };

            var btnOk     = new Button { Text = "✅ حفظ",   Width = 100, Height = 32, BackColor = Color.FromArgb(46,204,113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "❌ إلغاء", Width = 80,  Height = 32, BackColor = Color.FromArgb(200,50,50),  ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnOk.FlatAppearance.BorderSize = btnCancel.FlatAppearance.BorderSize = 0;

            var pnlArrows = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 40,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = Color.Transparent,
                Padding       = new Padding(5, 5, 5, 0)
            };
            pnlArrows.Controls.AddRange(new Control[] { btnDown, btnUp });

            var pnlFooter = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 44,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = Color.Transparent,
                Padding       = new Padding(5, 5, 5, 0)
            };
            pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnOk });

            dlg.Controls.Add(clb);
            dlg.Controls.Add(pnlArrows);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(lblHint);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // تطبيق الترتيب والإظهار على الجدول
                int displayIndex = 0;
                var hiddenNames  = new System.Collections.Generic.List<string>();
                var orderedNames = new System.Collections.Generic.List<string>();

                for (int i = 0; i < clb.Items.Count; i++)
                {
                    if (!(clb.Items[i] is ColEntry ce)) continue;
                    orderedNames.Add(ce.ColName);
                    bool visible = clb.GetItemChecked(i);
                    if (!visible) hiddenNames.Add(ce.ColName);

                    if (dgItems.Columns.Contains(ce.ColName))
                    {
                        dgItems.Columns[ce.ColName].Visible      = visible;
                        dgItems.Columns[ce.ColName].DisplayIndex = displayIndex++;
                    }
                }
                // عمود الحذف دائماً في الآخر
                if (dgItems.Columns.Contains("Delete"))
                    dgItems.Columns["Delete"].DisplayIndex = dgItems.ColumnCount - 1;

                SaveColumnSettings(orderedNames, hiddenNames);
            }
        }

        /// <summary>يحفظ ترتيب الأعمدة وما هو مخفي في Settings.ini</summary>
        private void SaveColumnSettings(
            System.Collections.Generic.List<string> ordered = null,
            System.Collections.Generic.List<string> hidden = null)
        {
            try
            {
                if (ordered == null || hidden == null)
                {
                    ordered = new System.Collections.Generic.List<string>();
                    hidden = new System.Collections.Generic.List<string>();

                    var cols = new System.Collections.Generic.List<DataGridViewColumn>();
                    foreach (DataGridViewColumn col in dgItems.Columns)
                    {
                        if (col.Name == "Delete") continue;
                        cols.Add(col);
                    }
                    cols.Sort((x, y) => x.DisplayIndex.CompareTo(y.DisplayIndex));

                    foreach (var col in cols)
                    {
                        ordered.Add(col.Name);
                        if (!col.Visible) hidden.Add(col.Name);
                    }
                }

                Core.LicenseManager.WriteIniValue("PurchaseGridColumns", "Order",  string.Join(",", ordered));
                Core.LicenseManager.WriteIniValue("PurchaseGridColumns", "Hidden", string.Join(",", hidden));
            }
            catch { }
        }

        /// <summary>يحمّل ترتيب الأعمدة من Settings.ini عند بداية التشغيل</summary>
        private void LoadColumnSettings()
        {
            try
            {
                string orderVal  = Core.LicenseManager.ReadIniValue("PurchaseGridColumns", "Order",  "");
                string hiddenVal = Core.LicenseManager.ReadIniValue("PurchaseGridColumns", "Hidden", "");

                if (string.IsNullOrWhiteSpace(orderVal)) return;

                var ordered = new System.Collections.Generic.List<string>(
                    orderVal.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));
                var hidden  = new System.Collections.Generic.List<string>(
                    string.IsNullOrEmpty(hiddenVal) ? new string[0]
                    : hiddenVal.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));

                // تأمين: أي أعمدة موجودة في الجدول برمجياً وغير مسجلة في الإعدادات (ترقية جديدة)، نقوم بإضافتها في النهاية
                foreach (System.Windows.Forms.DataGridViewColumn col in dgItems.Columns)
                {
                    if (col.Name == "Delete") continue;
                    if (!ordered.Contains(col.Name))
                    {
                        ordered.Add(col.Name);
                    }
                }

                int displayIndex = 0;
                foreach (string colName in ordered)
                {
                    if (!dgItems.Columns.Contains(colName)) continue;
                    dgItems.Columns[colName].Visible      = !hidden.Contains(colName);
                    dgItems.Columns[colName].DisplayIndex = displayIndex++;
                }
                if (dgItems.Columns.Contains("Delete"))
                    dgItems.Columns["Delete"].DisplayIndex = dgItems.ColumnCount - 1;
            }
            catch { }
        }

        // مساعد: تمثيل عمود في القائمة
        private class ColEntry
        {
            public string ColName    { get; }
            public string HeaderText { get; }
            public ColEntry(string n, string h) { ColName = n; HeaderText = h; }
            public override string ToString() => HeaderText;
        }

        private void LoadInvoiceForEdit(int purchaseID)
        {
            var dtPurchase = DbHelper.Query(
                @"SELECT p.PurchaseType, p.PurchaseDate, p.SupplierID, p.Notes,
                         COALESCE(p.DiscountAmount, 0) AS DiscountAmount,
                         COALESCE(p.DiscountPct, 0) AS DiscountPct,
                         COALESCE(p.TaxPct, 0) AS TaxPct,
                         p.WarehouseID
                  FROM Purchases p WHERE p.PurchaseID=@id",
                DbHelper.P("@id", purchaseID));

            if (dtPurchase.Rows.Count == 0)
            {
                MessageBox.Show("لم يتم العثور على الفاتورة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var row = dtPurchase.Rows[0];
            _loadedLastModified = Convert.ToDateTime(row["PurchaseDate"]);

            // نوع الفاتورة
            string typeStr = row["PurchaseType"].ToString();
            _purchaseType = typeStr;
            ToggleType();

            // التاريخ
            dtpDate.Value = _isCopyMode ? DateTime.Today : Convert.ToDateTime(row["PurchaseDate"]);

            // المستودع
            if (row["WarehouseID"] != DBNull.Value)
            {
                int wid = Convert.ToInt32(row["WarehouseID"]);
                for (int i = 0; i < cboWarehouse.Items.Count; i++)
                    if (cboWarehouse.Items[i] is ComboItem wci && wci.ID == wid)
                        { cboWarehouse.SelectedIndex = i; break; }
            }

            // المورد
            if (row["SupplierID"] != DBNull.Value)
            {
                int sid = Convert.ToInt32(row["SupplierID"]);
                for (int i = 0; i < cboSupplier.Items.Count; i++)
                    if (cboSupplier.Items[i] is ComboItem ci && ci.ID == sid)
                        { cboSupplier.SelectedIndex = i; break; }
            }

            // ملاحظات ورقم فاتورة المورد
            txtNotes.Text = row["Notes"].ToString();
            if (txtSupplierInvoiceNo != null && row.Table.Columns.Contains("SupplierInvoiceNo"))
                txtSupplierInvoiceNo.Text = row["SupplierInvoiceNo"].ToString();

            // الخصم
            decimal dAmt = row.Table.Columns.Contains("DiscountAmount") && row["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmount"]) : 0m;
            decimal dPct = row.Table.Columns.Contains("DiscountPct") && row["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(row["DiscountPct"]) : 0m;
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

            // مصاريف الشحن
            if (txtShippingCost != null)
            {
                decimal sCost = row.Table.Columns.Contains("ShippingCost") && row["ShippingCost"] != DBNull.Value ? Convert.ToDecimal(row["ShippingCost"]) : 0m;
                txtShippingCost.Text = sCost.ToString("G29");
            }
            if (cboShippingOn != null)
            {
                string sOn = row.Table.Columns.Contains("ShippingOn") && row["ShippingOn"] != DBNull.Value ? row["ShippingOn"].ToString() : "Company";
                cboShippingOn.SelectedIndex = (sOn == "Supplier") ? 1 : 0;
            }

            // الأصناف
            var itemsDt = PurchaseDAL.GetItems(purchaseID);
            _items.Clear();
            foreach (DataRow iRow in itemsDt.Rows)
            {
                _items.Add(new PurchaseItemDTO
                {
                    ProductID   = Convert.ToInt32(iRow["ProductID"]),
                    ProductCode = iRow["ProductCode"].ToString(),
                    ProductName = iRow["ProductName"].ToString(),
                    Quantity    = Convert.ToDecimal(iRow["Quantity"]),
                    UnitPrice   = Convert.ToDecimal(iRow["UnitPrice"]),
                    DiscountPct = iRow.Table.Columns.Contains("DiscountPct") && iRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(iRow["DiscountPct"]) : 0m,
                    DiscountAmt = iRow.Table.Columns.Contains("DiscountAmt") && iRow["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(iRow["DiscountAmt"]) : 0m,
                    SuggestedSalePrice = iRow["SuggestedSalePrice"] != DBNull.Value ? Convert.ToDecimal(iRow["SuggestedSalePrice"]) : (decimal?)null,
                    UnitName = iRow.Table.Columns.Contains("UnitName") && iRow["UnitName"] != DBNull.Value ? iRow["UnitName"].ToString() : null,
                    Factor = iRow.Table.Columns.Contains("Factor") && iRow["Factor"] != DBNull.Value ? Convert.ToDecimal(iRow["Factor"]) : 1.0m,
                    ExpiryDate = iRow.Table.Columns.Contains("ExpiryDate") && iRow["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(iRow["ExpiryDate"]) : (DateTime?)null
                });
            }
            RefreshGrid();

            if (_isCopyMode)
                this.Text = "نسخة من فاتورة مشتريات";
            else
                this.Text = $"تعديل فاتورة مشتريات رقم {purchaseID}";

            _isDirty = false;
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة نقطة البيع السريعة — مصممة للسوبر ماركت والكاشير
    /// </summary>
    public class FrmPOS : Form
    {
        // ── عناصر الواجهة ─────────────────────────────────────
        private TextBox txtBarcode;
        private ComboBox cboWarehouse, cboPriceTier;
        private Label lblTitle, lblWh, lblTier;
        private DataGridView dgItems;
        private Label lblTotal, lblPaid, lblChange, lblItemCount, lblClientName, lblClientPoints;
        private Label lblInvoiceItemsBadge;
        private CheckBox chkQuickInStockOnly;
        private int? _currentQuickCategoryId = null;
        private Label _lPaid, _lVisaPaid;
        private Button _btnPrint, _btnWhatsApp, btnOpenDrawer;
        private TextBox txtPaid, txtVisaPaid;
        private Button btnPay, btnNew, btnCancel, btnSearchProduct, btnCustomizeCols;
        private Button btnTypeCash, btnTypeVisa, btnTypeCredit, btnTypeMixed;
        private Panel pnlPaymentTypes;
        private string _selectedSaleType = "Cash";
        private ComboBox cboClient;
        private Panel pnlClient;
        private FlowLayoutPanel flowQuickItems;
        private Panel pnlTotals, pnlQuick, pnlTop;
        private CheckBox chkRedeemPoints;
        private Label lblClock;
        private System.Windows.Forms.Timer _clockTimer;

        // ── عناصر واجهة المطعم ─────────────────────────────────
        private FlowLayoutPanel flowCategories;
        private Panel pnlOrderType;
        private RadioButton rbDineIn, rbTakeaway, rbDelivery;
        private Label lblTableNum;
        private TextBox txtTableNum;
        private ComboBox cboDeliveryDriver;
        private Button btnSuspend, btnRecall, btnModelLookup, btnIncompletePOS, btnKitchenPrint;
        private int _loadedDraftSaleID = 0;
        private bool _isSaving = false;
        private int? _selectedVisaAccountID = null;
        private string _selectedVisaAccountName = "";

        // ── البيانات ──────────────────────────────────────────
        private List<POSItem> _items = new List<POSItem>();
        private int _lastSaleID = 0;
        private Dictionary<int, decimal> _stockCache = new Dictionary<int, decimal>();
        private string _activeDraftKey = null;
        private int _activeDraftID = 0;

        // Barcode auto-detection
        private System.Windows.Forms.Timer _barcodeTimer;
        private string _barcodeBuffer = "";
        private DateTime _lastKeyTime = DateTime.MinValue;
        private const int BARCODE_INTERVAL_MS = 50;
        private const int BARCODE_MIN_LENGTH = 4;
        private string _lastScannedBarcode = null;
        private DateTime _lastScanTime = DateTime.MinValue;
        private const int BARCODE_DEBOUNCE_MS = 750;

        // سطر إدخال الكود الجديد المعلق
        private int _pendingRowIdx = -1;

        // جلسة البحث السريع - لمنع تدخل FocusQtyCell أثناء تكرار شاشة البحث
        private bool _searchSessionActive = false;
        private decimal? _pendingScaleWeight = null;

        // ── بونات الخصم ──────────────────────────────────────
        private TextBox txtVoucherCode;
        private Label lblVoucherDiscount;
        private Button btnApplyVoucher;
        private decimal _voucherDiscount = 0m;
        private int _appliedVoucherID = 0;


        public FrmPOS()
        {
            InitUI();
            LoadQuickItems();
            LoadCategories();
            LoadDeliveryDrivers();
            LoadClients();
            LoadStockCache();
            if (AppConfig.ScaleEnabled)
            {
                try { ScaleService.Instance.WeightChanged += ScaleService_WeightChanged; } catch { }
            }
            this.FormClosing += (s, e) => {
                if (AppConfig.ScaleEnabled)
                {
                    try { ScaleService.Instance.WeightChanged -= ScaleService_WeightChanged; } catch { }
                }
            };
            this.Load += (s, e) => {
                LayoutPanels();
                this.ActiveControl = txtBarcode;
                txtBarcode.Focus();
            };
        }

        private void ScaleService_WeightChanged(decimal weight, bool isStable)
        {
            if (isStable && weight > 0)
            {
                _pendingScaleWeight = weight;
            }
        }

        private void InitUI()
        {
            this.Text = "🛒 نقطة البيع السريعة - POS";
            this.Size = new Size(1100, 750);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.KeyPreview = true;
            this.KeyDown += FrmPOS_KeyDown;
            this.WindowState = FormWindowState.Maximized;

            // ── الشريط العلوي ─────────────────────────────────
            pnlTop = new Panel { Dock = DockStyle.Top, Height = 75, BackColor = Theme.BgHeader };
            lblTitle = new Label { Text = "🛒 نقطة البيع السريعة", Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = Theme.Accent, AutoSize = true };
            
            txtBarcode = new TextBox
            {
                Location = new Point(20, 35), Size = new Size(300, 32),
                Font = new Font("Segoe UI", 14f), BackColor = Theme.BgInput, ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                RightToLeft = RightToLeft.Yes,
                TextAlign = HorizontalAlignment.Left
            };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;

            btnSearchProduct = Theme.MakeButton("🔍", Theme.Primary, new Point(325, 35), new Size(40, 32));
            btnSearchProduct.Visible = Session.CanAccess("ProductSearch");
            btnSearchProduct.Click += (s, e) => OpenProductSearch();

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(txtBarcode);
            pnlTop.Controls.Add(btnSearchProduct);
            txtBarcode.BringToFront();
            btnSearchProduct.BringToFront();

            btnCustomizeCols = new Button
            {
                Text      = "⚙️ الأعمدة",
                Size      = new Size(95, 32),
                Location  = new Point(375, 35),
                BackColor = Color.FromArgb(55, 65, 81),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnCustomizeCols.FlatAppearance.BorderSize = 0;
            btnCustomizeCols.Click += (s, e) => ShowColumnCustomizer();
            btnCustomizeCols.Visible = Session.CanOrderColumns("POS");
            pnlTop.Controls.Add(btnCustomizeCols);
            btnCustomizeCols.BringToFront();

            // ── اختيار المخزن وشريحة السعر الافتراضية ──────────
            lblWh = new Label
            {
                Text = "المخزن:",
                Location = new Point(480, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextMain
            };
            cboWarehouse = new ComboBox
            {
                Location = new Point(480, 35),
                Size = new Size(160, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                BackColor = Theme.BgInput
            };

            var whDt = Session.GetAllowedWarehouses(true);
            cboWarehouse.Items.Clear();
            int defWhId = Session.GetDefaultWarehouseID();
            int selIdx = 0;
            for (int i = 0; i < whDt.Rows.Count; i++)
            {
                int wid = Convert.ToInt32(whDt.Rows[i]["WarehouseID"]);
                string wname = whDt.Rows[i]["WarehouseName"].ToString();
                cboWarehouse.Items.Add(new ComboItem(wid, wname));
                if (wid == defWhId) selIdx = i;
            }
            cboWarehouse.DisplayMember = "Text";
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = selIdx;
            cboWarehouse.Enabled = Session.IsAdmin || whDt.Rows.Count > 1;
            cboWarehouse.SelectedIndexChanged += (s, e) =>
            {
                LoadStockCache();
                RefreshGrid();
                FilterQuickItems(_currentQuickCategoryId);
            };

            lblTier = new Label
            {
                Text = "شريحة السعر:",
                Location = new Point(650, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextMain
            };
            cboPriceTier = new ComboBox
            {
                Location = new Point(650, 35),
                Size = new Size(115, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                BackColor = Theme.BgInput
            };
            cboPriceTier.Items.AddRange(new object[] { "قطاعي", "نصف جملة", "جملة" });
            cboPriceTier.SelectedItem = Session.GetDefaultPriceTier();
            if (cboPriceTier.SelectedIndex < 0) cboPriceTier.SelectedIndex = 0;
            cboPriceTier.Enabled = Session.IsAdmin;
            cboPriceTier.SelectedIndexChanged += (s, e) =>
            {
                if (_items.Count > 0 && cboPriceTier.SelectedItem != null)
                {
                    string newTier = cboPriceTier.SelectedItem.ToString();
                    foreach (var itm in _items)
                    {
                        decimal p = GetProductPriceByTier(itm.ProductID, newTier);
                        if (p > 0) itm.Price = p;
                    }
                    RefreshGrid();
                }
                FilterQuickItems(_currentQuickCategoryId);
            };

            pnlTop.Controls.Add(lblWh);
            pnlTop.Controls.Add(cboWarehouse);
            pnlTop.Controls.Add(lblTier);
            pnlTop.Controls.Add(cboPriceTier);
            lblWh.BringToFront();
            cboWarehouse.BringToFront();
            lblTier.BringToFront();
            cboPriceTier.BringToFront();

            // ── ساعة مباشرة ──────────────────────────────────────
            lblClock = new Label
            {
                Text = DateTime.Now.ToString("hh:mm tt"),
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                AutoSize = true,
                Location = new Point(10, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            pnlTop.Controls.Add(lblClock);
            lblClock.BringToFront();
            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 }; // تحديث حي كل ثانية
            _clockTimer.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("hh:mm:ss tt");
            _clockTimer.Start();

            this.Controls.Add(pnlTop);

            // ── جدول الأصناف (يسار) ──────────────────────────
            dgItems = new DataGridView
            {
                Location = new Point(10, 85), Size = new Size(640, 400),
                BackgroundColor = Color.White, ForeColor = Theme.TextMain,
                AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ScrollBars = ScrollBars.Both,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) },
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Accent, SelectionForeColor = Color.White },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(240, 242, 245), ForeColor = Theme.TextMain, SelectionBackColor = Theme.Accent, SelectionForeColor = Color.White },
                GridColor = Color.FromArgb(210, 210, 215), BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.Single
            };
            dgItems.Columns.Add("Code", AppConfig.BusinessType switch
            {
                "Mobiles"   => "كود الموديل",
                "Clothing"  => "كود الموديل",
                "SpareParts" => "رقم القطعة",
                _           => "الكود"
            });
            dgItems.Columns.Add("Name", AppConfig.BusinessType switch
            {
                "Mobiles"   => "الجهاز / الصنف",
                "Clothing"  => "القطعة / الصنف",
                _           => "الصنف"
            });
            var colStock = new DataGridViewTextBoxColumn
            {
                Name = "StockQty",
                HeaderText = "الرصيد",
                ReadOnly = true,
                Width = 65,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(46, 204, 113)
                }
            };
            dgItems.Columns.Add(colStock);

            var colUnit = new DataGridViewComboBoxColumn
            {
                Name = "UnitName",
                HeaderText = "الوحدة",
                ReadOnly = false,
                Width = 80,
                FlatStyle = FlatStyle.Flat
            };
            dgItems.Columns.Add(colUnit);

            dgItems.Columns.Add("Qty", "الكمية");
            // ── أزرار +/- للكمية ─────────────────────────────────
            var plusCol = new DataGridViewButtonColumn
            {
                Name = "QtyPlus", HeaderText = "+",
                Text = "+", UseColumnTextForButtonValue = true,
                Width = 32, FlatStyle = FlatStyle.Flat
            };
            plusCol.DefaultCellStyle.BackColor = Color.FromArgb(25, 135, 84);
            plusCol.DefaultCellStyle.ForeColor = Color.White;
            plusCol.DefaultCellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            plusCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgItems.Columns.Add(plusCol);

            var minusCol = new DataGridViewButtonColumn
            {
                Name = "QtyMinus", HeaderText = "-",
                Text = "-", UseColumnTextForButtonValue = true,
                Width = 32, FlatStyle = FlatStyle.Flat
            };
            minusCol.DefaultCellStyle.BackColor = Color.FromArgb(220, 53, 69);
            minusCol.DefaultCellStyle.ForeColor = Color.White;
            minusCol.DefaultCellStyle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            minusCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgItems.Columns.Add(minusCol);

            var colPrice = new DataGridViewTextBoxColumn
            {
                Name = "Price",
                HeaderText = "السعر",
                MinimumWidth = 95,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            };
            dgItems.Columns.Add(colPrice);
            
            var colLastPrice = new DataGridViewTextBoxColumn
            {
                Name = "LastClientPrice",
                HeaderText = "آخر سعر للعميل 🏷️",
                Visible = false,
                ReadOnly = true,
                Width = 115,
                MinimumWidth = 115,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(230, 126, 34), Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            };
            dgItems.Columns.Add(colLastPrice);

            var colIMEI = new DataGridViewTextBoxColumn
            {
                Name = "IMEI",
                HeaderText = "السيريال",
                Visible = AppConfig.BusinessType == "Mobiles",
                ReadOnly = false,
                Width = 100
            };
            dgItems.Columns.Add(colIMEI);

            dgItems.Columns.Add("Discount", "الخصم");
            var colTotal = new DataGridViewTextBoxColumn
            {
                Name = "Total",
                HeaderText = "الإجمالي",
                MinimumWidth = 105,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            };
            dgItems.Columns.Add(colTotal);
            if (AppConfig.IsRestaurant)
            {
                var colKn = new DataGridViewTextBoxColumn
                {
                    Name = "KitchenNotes",
                    HeaderText = "📝 ملاحظات المطبخ",
                    Visible = false,
                    ReadOnly = false,
                    Width = 130
                };
                dgItems.Columns.Add(colKn);
            }
            var delCol = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "حذف",
                Text = "🗑",
                UseColumnTextForButtonValue = true,
                Width = 45,
                FlatStyle = FlatStyle.Flat
            };
            delCol.DefaultCellStyle.ForeColor = Color.Red;
            delCol.DefaultCellStyle.SelectionForeColor = Color.Red;
            dgItems.Columns.Add(delCol);
            
            dgItems.Columns["Code"].ReadOnly = false;
            dgItems.Columns["Name"].ReadOnly = true;
            dgItems.Columns["StockQty"].ReadOnly = true;
            dgItems.Columns["UnitName"].ReadOnly = false;
            dgItems.Columns["Qty"].ReadOnly = false;
            dgItems.Columns["Price"].ReadOnly = !Session.CanEditPrice("POS");
            dgItems.Columns["Discount"].ReadOnly = false;
            dgItems.Columns["Total"].ReadOnly = true;

            dgItems.Columns["Code"].Width = 70;
            dgItems.Columns["Name"].Width = 180;
            dgItems.Columns["Name"].MinimumWidth = 140;
            dgItems.Columns["Name"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgItems.Columns["StockQty"].Width = 65;
            dgItems.Columns["UnitName"].Width = 85;
            dgItems.Columns["Qty"].Width = 55;
            dgItems.Columns["Price"].Width = 75;
            dgItems.Columns["Discount"].Width = 60;
            dgItems.Columns["Total"].Width = 85;
            if (dgItems.Columns.Contains("Delete")) dgItems.Columns["Delete"].Width = 42;
            dgItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            dgItems.AllowUserToOrderColumns = Session.CanOrderColumns("POS");
            Session.LoadColumnOrder(dgItems, "POS");
            LoadColumnSettings();

            dgItems.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex < dgItems.Columns.Count)
                {
                    string colName = dgItems.Columns[e.ColumnIndex].Name;
                    if (colName == "Delete")
                    {
                        if (e.RowIndex < _items.Count)
                        {
                            _items.RemoveAt(e.RowIndex);
                            RefreshGrid();
                        }
                    }
                    else if (colName == "UnitName")
                    {
                        dgItems.BeginEdit(true);
                        if (dgItems.EditingControl is ComboBox cbo)
                        {
                            cbo.DroppedDown = true;
                        }
                    }
                    else if (colName == "QtyPlus" && e.RowIndex < _items.Count)
                    {
                        var item = _items[e.RowIndex];
                        decimal targetBaseQty = (item.Qty + 1) * item.Factor;
                        if (!CheckAvailableStock(item.ProductID, item.BatchID, targetBaseQty, out decimal avail, out string err))
                        {
                            decimal maxAvail = avail / (item.Factor > 0 ? item.Factor : 1m);
                            MessageBox.Show($"⚠️ لا يمكن زيادة الكمية.\nالرصيد المتاح بالمخزن ({maxAvail:G29}) لا يكفي!\nالبيع بالسالب غير مسموح للأصناف العادية.", "تنبيه المخزون", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        item.Qty += 1;
                        item.Total = (item.Qty * item.Price) - item.DiscountAmt;
                        RefreshGrid();
                        try { SystemSounds.Asterisk.Play(); } catch { }
                    }
                    else if (colName == "QtyMinus" && e.RowIndex < _items.Count)
                    {
                        if (_items[e.RowIndex].Qty > 1)
                        {
                            _items[e.RowIndex].Qty -= 1;
                            _items[e.RowIndex].Total = (_items[e.RowIndex].Qty * _items[e.RowIndex].Price) - _items[e.RowIndex].DiscountAmt;
                            RefreshGrid();
                        }
                    }
                }
            };

            dgItems.CellEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "UnitName")
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (dgItems.CurrentCell != null && dgItems.CurrentCell.RowIndex == e.RowIndex && dgItems.Columns[dgItems.CurrentCell.ColumnIndex].Name == "UnitName")
                        {
                            dgItems.BeginEdit(true);
                            if (dgItems.EditingControl is ComboBox cbo)
                            {
                                cbo.DroppedDown = true;
                            }
                        }
                    });
                }
            };

            dgItems.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgItems.IsCurrentCellDirty && dgItems.CurrentCell != null && dgItems.Columns[dgItems.CurrentCell.ColumnIndex].Name == "UnitName")
                {
                    dgItems.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgItems.DataError += (s, e) => { e.ThrowException = false; };

            dgItems.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    string colName = dgItems.Columns[e.ColumnIndex].Name;
                    if (colName == "Qty" || colName == "Price" || colName == "Discount" || colName == "KitchenNotes" || colName == "Delete" || colName == "QtyPlus" || colName == "QtyMinus" || colName == "UnitName")
                    {
                        return; // السماح بتعديل الخانات التفاعلية مباشرة
                    }
                }
                OpenProductSearch();
            };

            dgItems.DoubleClick += (s, e) =>
            {
                if (dgItems.SelectedCells.Count == 0 || (dgItems.CurrentCell != null && dgItems.CurrentCell.ReadOnly))
                {
                    OpenProductSearch();
                }
            };

            dgItems.EditingControlShowing += (s, e) =>
            {
                if (dgItems.CurrentCell != null && dgItems.CurrentCell.OwningColumn.Name == "UnitName")
                {
                    if (e.Control is ComboBox cbo)
                    {
                        cbo.DroppedDown = true;
                    }
                }
                else if (e.Control is TextBox tb)
                {
                    tb.ForeColor = Color.Black;
                    tb.BackColor = Color.FromArgb(255, 255, 200); // High contrast soft yellow
                    tb.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);

                    tb.KeyDown -= CellTextBox_KeyDown;
                    tb.KeyDown += CellTextBox_KeyDown;
                }
            };
            dgItems.CellEndEdit += DgItems_CellEndEdit;
            dgItems.KeyDown += DgItems_KeyDown;

            var cmsPOS = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };
            cmsPOS.Items.Add("🔍 كارت الصنف السريع (F4)", null, (s, e) => {
                if (!Session.IsAdmin && !Session.CanAccess("ProductCard") && !Session.CanAccess("Products") && !Session.CanEdit("Products"))
                {
                    MessageBox.Show("❌ عفوًا: ليس لديك صلاحية الدخول على كارت الصنف!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (dgItems.SelectedRows.Count > 0 && dgItems.SelectedRows[0].Index < _items.Count)
                {
                    int pId = _items[dgItems.SelectedRows[0].Index].ProductID;
                    if (pId > 0)
                    {
                        if (new FrmProductCard(pId).ShowDialog(this) == DialogResult.OK)
                        {
                            RefreshGrid();
                        }
                    }
                }
            });
            cmsPOS.Items.Add("📊 فحص رصيد الصنف بالمخازن", null, (s, e) => {
                if (dgItems.SelectedRows.Count > 0 && dgItems.SelectedRows[0].Index < _items.Count)
                {
                    int pId = _items[dgItems.SelectedRows[0].Index].ProductID;
                    string name = _items[dgItems.SelectedRows[0].Index].Name;
                    string unit = _items[dgItems.SelectedRows[0].Index].UnitName ?? "";
                    if (pId > 0)
                    {
                        var dtWarehouses = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseID");
                        string msg = $"📦 تفاصيل رصيد الصنف: {name}\n" + new string('-', 40) + "\n";
                        decimal totalStock = 0;
                        foreach (DataRow r in dtWarehouses.Rows)
                        {
                            int wid = Convert.ToInt32(r["WarehouseID"]);
                            string wName = r["WarehouseName"]?.ToString() ?? "";
                            decimal q = InventoryDAL.GetProductStock(pId, wid);
                            totalStock += q;
                            msg += $"• {wName}: {q:N2} {unit}\n";
                        }
                        msg += new string('-', 40) + $"\nالإجمالي الكلي: {totalStock:N2} {unit}";
                        MessageBox.Show(msg, "رصيد المخازن", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            });
            cmsPOS.Items.Add("📈 كشف حركة الصنف التفصيلي", null, (s, e) => {
                if (dgItems.SelectedRows.Count > 0 && dgItems.SelectedRows[0].Index < _items.Count)
                {
                    int pId = _items[dgItems.SelectedRows[0].Index].ProductID;
                    string name = _items[dgItems.SelectedRows[0].Index].Name;
                    string unit = _items[dgItems.SelectedRows[0].Index].UnitName ?? "";
                    if (pId > 0)
                    {
                        new FrmProductMovement(pId, name, unit).ShowDialog(this);
                    }
                }
            });
            cmsPOS.Items.Add("🏷️ طباعة باركود الصنف", null, (s, e) => {
                if (dgItems.SelectedRows.Count > 0 && dgItems.SelectedRows[0].Index < _items.Count)
                {
                    int pId = _items[dgItems.SelectedRows[0].Index].ProductID;
                    if (pId > 0)
                    {
                        var dt = DbHelper.Query("SELECT ProductName, ProductCode, InternationalCode, ShelfLocation, SalePrice FROM Products WHERE ProductID = @pid", DbHelper.P("@pid", pId));
                        if (dt.Rows.Count > 0)
                        {
                            string name = dt.Rows[0]["ProductName"]?.ToString() ?? "";
                            string code = dt.Rows[0]["ProductCode"]?.ToString() ?? "";
                            string intCode = dt.Rows[0]["InternationalCode"]?.ToString() ?? "";
                            string loc = dt.Rows[0]["ShelfLocation"]?.ToString() ?? "";
                            decimal price = dt.Rows[0]["SalePrice"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["SalePrice"]) : _items[dgItems.SelectedRows[0].Index].Price;
                            using (var frm = new FrmPrintProductBarcode(pId, name, code, intCode, price, loc))
                            {
                                frm.ShowDialog(this);
                            }
                        }
                    }
                }
            });
            cmsPOS.Items.Add(new ToolStripSeparator());
            cmsPOS.Items.Add("📓 إضافة الصنف المحدد لكشكول النواقص", null, (s, e) => {
                if (dgItems.SelectedRows.Count > 0 && dgItems.SelectedRows[0].Index < _items.Count)
                {
                    int pId = _items[dgItems.SelectedRows[0].Index].ProductID;
                    if (pId > 0)
                    {
                        using (var dlg = new FrmAddShortageItem(pId))
                        {
                            dlg.ShowDialog(this);
                        }
                    }
                }
            });
            cmsPOS.Items.Add("🎯 تعديل حد الطلب للأصناف", null, (s, e) => new FrmMinStockEdit().ShowDialog());
            cmsPOS.Items.Add(new ToolStripSeparator());
            cmsPOS.Items.Add("🗑️ حذف الصنف من الفاتورة (Del)", null, (s, e) => {
                if (dgItems.SelectedRows.Count > 0 && dgItems.SelectedRows[0].Index < _items.Count)
                {
                    _items.RemoveAt(dgItems.SelectedRows[0].Index);
                    RefreshGrid();
                }
            });
            dgItems.ContextMenuStrip = cmsPOS;
            dgItems.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = dgItems.HitTest(e.X, e.Y);
                    if (hit.RowIndex >= 0 && hit.RowIndex < _items.Count)
                    {
                        dgItems.ClearSelection();
                        dgItems.Rows[hit.RowIndex].Selected = true;
                        dgItems.CurrentCell = dgItems.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
                    }
                }
            };

            this.Controls.Add(dgItems);

            // ── لوحة العميل ───────────────────────────────────
            pnlClient = new Panel { Location = new Point(660, 85), Size = new Size(420, 55), BackColor = Theme.BgCard };
            var lClient = new Label { Text = "العميل:", Location = new Point(5, 5), Size = new Size(60, 25), ForeColor = Theme.TextMain, Font = Theme.FontMain };
            cboClient = new ComboBox { Location = new Point(70, 3), Size = new Size(165, 28), DropDownStyle = ComboBoxStyle.DropDown, Font = Theme.FontMain, BackColor = Theme.BgInput };
            cboClient.SelectedIndexChanged += CboClient_Changed;

            var btnClientSearch = Theme.MakeButton("🔍", Theme.Accent);
            btnClientSearch.Location = new Point(238, 3);
            btnClientSearch.Size = new Size(36, 28);
            btnClientSearch.Click += (s, e) =>
            {
                using (var frm = new FrmClientSearch())
                {
                    if (frm.ShowDialog() == DialogResult.OK && frm.SelectedClientID > 0)
                    {
                        int cid = frm.SelectedClientID;
                        var allClients = cboClient.Tag as List<ComboItem>;
                        if (allClients != null)
                        {
                            cboClient.BeginUpdate();
                            cboClient.Items.Clear();
                            cboClient.Items.AddRange(allClients.ToArray());
                            cboClient.EndUpdate();
                        }
                        for (int i = 0; i < cboClient.Items.Count; i++)
                        {
                            if (cboClient.Items[i] is ComboItem ci && ci.ID == cid)
                            {
                                cboClient.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            };

            lblClientPoints = new Label { Text = "", Location = new Point(280, 5), Size = new Size(130, 25), ForeColor = Theme.Accent, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            chkRedeemPoints = new CheckBox { Text = "استرداد نقاط", Location = new Point(280, 28), Size = new Size(120, 22), ForeColor = Theme.TextMain, Font = Theme.FontMain, Checked = false };
            chkRedeemPoints.CheckedChanged += (s, e) => RefreshGrid();
            pnlClient.Controls.Add(lClient);
            pnlClient.Controls.Add(cboClient);
            pnlClient.Controls.Add(btnClientSearch);
            pnlClient.Controls.Add(lblClientPoints);
            pnlClient.Controls.Add(chkRedeemPoints);
            this.Controls.Add(pnlClient);

            // ── لوحة نوع الطلب (مطاعم فقط) ───────────────────
            if (AppConfig.IsRestaurant)
            {
                pnlOrderType = new Panel
                {
                    BackColor = Color.FromArgb(30, 30, 46),
                    BorderStyle = BorderStyle.None,
                    Padding = new Padding(6)
                };

                rbDineIn = new RadioButton   { Text = "🍽️ صالة",    Checked = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true };
                rbTakeaway = new RadioButton { Text = "🛍️ تيك أواي", ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true };
                rbDelivery = new RadioButton { Text = "🛵 توصيل",   ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true };

                lblTableNum = new Label { Text = "رقم الطاولة:", ForeColor = Color.White, Font = new Font("Segoe UI", 9f), AutoSize = true };
                txtTableNum = new TextBox { Width = 60, Font = new Font("Segoe UI", 10f, FontStyle.Bold), BackColor = Theme.BgInput, ForeColor = Color.Black };

                var lblDriverRest = new Label { Text = "الطيار:", ForeColor = Color.White, Font = new Font("Segoe UI", 9f), AutoSize = true };
                cboDeliveryDriver = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f), BackColor = Theme.BgInput };
                cboDeliveryDriver.Visible = false;
                lblDriverRest.Visible = false;

                var toggleVisibility = new Action(() => {
                    bool isDineIn = rbDineIn.Checked;
                    bool isDelivery = rbDelivery.Checked;
                    lblTableNum.Visible = isDineIn;
                    txtTableNum.Visible = isDineIn;
                    cboDeliveryDriver.Visible = isDelivery;
                    lblDriverRest.Visible = isDelivery;
                });
                rbDineIn.CheckedChanged += (s, e) => toggleVisibility();
                rbTakeaway.CheckedChanged += (s, e) => toggleVisibility();
                rbDelivery.CheckedChanged += (s, e) => toggleVisibility();

                var flowOT = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true, AutoSize = true };
                flowOT.Controls.AddRange(new Control[] { rbDineIn, rbTakeaway, rbDelivery, lblTableNum, txtTableNum, lblDriverRest, cboDeliveryDriver });
                pnlOrderType.Controls.Add(flowOT);
                this.Controls.Add(pnlOrderType);
            }

            // ── لوحة الأصناف السريعة (يمين) ──────────────────
            pnlQuick = new Panel { Location = new Point(660, 150), Size = new Size(420, 335), BackColor = Color.FromArgb(240, 242, 245), Padding = new Padding(4), Visible = Session.CanViewQuickItems("POS") };
            pnlQuick.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlQuick);

            // شريط الأقسام التفاعلي
            flowCategories = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.RightToLeft,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.FromArgb(30, 30, 46),
                Visible = true
            };
            flowCategories.MouseWheel += (s, e) =>
            {
                try
                {
                    int scrollAmount = 60;
                    int newVal = flowCategories.HorizontalScroll.Value - (e.Delta > 0 ? scrollAmount : -scrollAmount);
                    if (newVal < flowCategories.HorizontalScroll.Minimum) newVal = flowCategories.HorizontalScroll.Minimum;
                    if (newVal > flowCategories.HorizontalScroll.Maximum) newVal = flowCategories.HorizontalScroll.Maximum;
                    flowCategories.HorizontalScroll.Value = newVal;
                }
                catch { }
            };

            var pnlQuickHeader = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent, Padding = new Padding(4, 2, 4, 2) };

            chkQuickInStockOnly = new CheckBox
            {
                Text = "رصيد متوفر فقط",
                Dock = DockStyle.Left,
                Width = 140,
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Checked = AppConfig.POSQuickInStockOnly,
                Cursor = Cursors.Hand,
                RightToLeft = RightToLeft.Yes
            };
            chkQuickInStockOnly.CheckedChanged += (s, e) =>
            {
                AppConfig.POSQuickInStockOnly = chkQuickInStockOnly.Checked;
                FilterQuickItems(_currentQuickCategoryId);
            };

            var lQuick = new Label { Text = "⚡ أصناف سريعة", Dock = DockStyle.Fill, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            pnlQuickHeader.Controls.Add(lQuick);
            pnlQuickHeader.Controls.Add(chkQuickInStockOnly);

            flowQuickItems = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent, FlowDirection = FlowDirection.RightToLeft, RightToLeft = RightToLeft.Yes };
            pnlQuick.Controls.Add(flowQuickItems);
            pnlQuick.Controls.Add(flowCategories);
            pnlQuick.Controls.Add(pnlQuickHeader);
            this.Controls.Add(pnlQuick);

            // ── لوحة الإجماليات ───────────────────────────────
            pnlTotals = new Panel { Location = new Point(10, 495), Size = new Size(1070, 200), BackColor = Theme.BgCard };
            pnlTotals.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlTotals);

            // ترتيب RTL: الإجمالي (يمين) → المدفوع ونوع الدفع (وسط) → الباقي وعدد الأصناف (يسار)
            lblTotal     = new Label { Text = "الإجمالي: 0.00 ج",  Location = new Point(700, 45), Size = new Size(340, 40), ForeColor = Theme.Success, Font = new Font("Segoe UI", 20f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
            lblItemCount = new Label { Text = "عدد الأصناف: 0",    Location = new Point(700, 10), Size = new Size(340, 30), ForeColor = Theme.TextSub,  Font = new Font("Segoe UI", 11f),              TextAlign = ContentAlignment.MiddleRight };

            lblInvoiceItemsBadge = new Label
            {
                Text = "📦 أصناف الفاتورة: 0",
                Location = new Point(20, 46),
                Size = new Size(205, 42),
                ForeColor = Color.FromArgb(255, 220, 110),
                BackColor = Color.FromArgb(30, 41, 59),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── شريط اختيار نوع الدفع (كاش - فيزا - آجل - مختلط) ──
            pnlPaymentTypes = new Panel { Size = new Size(400, 36), BackColor = Color.Transparent };
            
            btnTypeCash = new Button
            {
                Text = "💵 كاش (F7)",
                Size = new Size(95, 34),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Theme.Primary,
                ForeColor = Color.White
            };
            btnTypeCash.FlatAppearance.BorderSize = 0;
            btnTypeCash.Click += (s, e) => SetPaymentType("Cash");

            btnTypeVisa = new Button
            {
                Text = "💳 فيزا (F8)",
                Size = new Size(95, 34),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(45, 52, 70),
                ForeColor = Color.FromArgb(180, 195, 215)
            };
            btnTypeVisa.FlatAppearance.BorderSize = 0;
            btnTypeVisa.Click += (s, e) =>
            {
                if (_selectedSaleType == "Visa")
                {
                    decimal vTotal = 0;
                    foreach (var it in _items) vTotal += it.Total;
                    if (FrmSelectVisaAccount.SelectVisaAccount(this, vTotal, _selectedVisaAccountID, out int vId, out string vName))
                    {
                        _selectedVisaAccountID = vId;
                        _selectedVisaAccountName = vName;
                        UpdatePaymentTypeButtons();
                    }
                }
                else
                {
                    SetPaymentType("Visa");
                }
            };

            btnTypeCredit = new Button
            {
                Text = "📑 آجل (F9)",
                Size = new Size(95, 34),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(45, 52, 70),
                ForeColor = Color.FromArgb(180, 195, 215)
            };
            btnTypeCredit.FlatAppearance.BorderSize = 0;
            btnTypeCredit.Click += (s, e) => SetPaymentType("Credit");

            btnTypeMixed = new Button
            {
                Text = "🔀 مختلط (F10)",
                Size = new Size(105, 34),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(45, 52, 70),
                ForeColor = Color.FromArgb(180, 195, 215)
            };
            btnTypeMixed.FlatAppearance.BorderSize = 0;
            btnTypeMixed.Click += (s, e) => SetPaymentType("Mixed");

            pnlPaymentTypes.Controls.AddRange(new Control[] { btnTypeCash, btnTypeVisa, btnTypeCredit, btnTypeMixed });

            _lPaid = new Label { Text = "المدفوع كاش:", Location = new Point(370, 50), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold) };
            txtPaid = new TextBox { Location = new Point(255, 46), Size = new Size(110, 34), Font = new Font("Segoe UI", 15f, FontStyle.Bold), BackColor = Theme.BgInput, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle, Text = "0", TextAlign = HorizontalAlignment.Center };
            txtPaid.TextChanged += (s, e) => RecalcChange();

            _lVisaPaid = new Label { Text = "المدفوع فيزا:", Location = new Point(160, 50), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), Visible = false };
            txtVisaPaid = new TextBox { Location = new Point(50, 46), Size = new Size(105, 34), Font = new Font("Segoe UI", 15f, FontStyle.Bold), BackColor = Theme.BgInput, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle, Text = "0", TextAlign = HorizontalAlignment.Center, Visible = false };
            txtVisaPaid.TextChanged += (s, e) => RecalcChange();

            lblChange = new Label { Text = "الباقي: 0.00 ج", Location = new Point(230, 46), Size = new Size(240, 42), ForeColor = Theme.Accent, Font = new Font("Segoe UI", 17.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };

            // ── أزرار الأسفل ──────────────────────────────────
            btnPay = Theme.MakeButton("💰 إتمام البيع (F5)", Theme.Success, new Point(20, 130), new Size(250, 55));
            btnPay.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            btnPay.Click += BtnPay_Click;

            btnNew = Theme.MakeButton("🔄 فاتورة جديدة (F2)", Color.FromArgb(60, 70, 85), new Point(280, 130), new Size(160, 55));
            btnNew.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            btnNew.Click += (s, e) => NewInvoice();

            btnCancel = Theme.MakeButton("❌ إلغاء (Esc)", Theme.Danger, new Point(450, 130), new Size(130, 55));
            btnCancel.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnCancel.Click += (s, e) => { if (_items.Count > 0 && MessageBox.Show("إلغاء الفاتورة؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes) NewInvoice(); };

            _btnPrint = Theme.MakeButton("🖨️ طباعة (F6)", Theme.Primary, new Point(680, 130), new Size(110, 55));
            _btnPrint.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            _btnPrint.Click += (s, e) => { if (_lastSaleID > 0) PrintReceipt(_lastSaleID, askFirst: true); };

            _btnWhatsApp = Theme.MakeButton("💬 واتساب", Color.FromArgb(37, 211, 102), new Point(795, 130), new Size(95, 55));
            _btnWhatsApp.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            _btnWhatsApp.ForeColor = Color.White;
            _btnWhatsApp.Click += (s, e) => { if (_lastSaleID > 0) SendWhatsAppReceipt(_lastSaleID); };

            btnOpenDrawer = Theme.MakeButton("🔓 فتح الدرج\n(Ctrl+D)", Color.FromArgb(70, 70, 70), new Point(895, 130), new Size(150, 55));
            btnOpenDrawer.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnOpenDrawer.Click += (s, e) => { RawPrinterHelper.OpenCashDrawer(); };

            btnSuspend = Theme.MakeButton("⏳ تعليق\nالطلب (F3)", Color.FromArgb(230, 126, 34), new Point(0, 130), new Size(130, 55));
            btnSuspend.Name = "btnSuspend";
            btnSuspend.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnSuspend.Click += (s, e) => SuspendCurrentOrder();

            btnRecall = Theme.MakeButton("📋 الطلبات\nالمعلقة (F4)", Color.FromArgb(52, 152, 219), new Point(0, 130), new Size(140, 55));
            btnRecall.Name = "btnRecall";
            btnRecall.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnRecall.Click += (s, e) => RecallDraftSale();

            btnIncompletePOS = Theme.MakeButton("📂 فواتير\nلم تكتمل", Color.FromArgb(70, 40, 130), new Point(0, 128), new Size(115, 56));
            btnIncompletePOS.Name = "btnIncompletePOS";
            btnIncompletePOS.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnIncompletePOS.Click += (s, e) => OpenIncompletePOSDialog();

            if (AppConfig.IsClothing)
            {
                btnModelLookup = Theme.MakeButton("👗 ألوان ومقاسات", Color.FromArgb(142, 68, 173), new Point(0, 128), new Size(125, 56));
                btnModelLookup.Name = "btnModelLookup";
                btnModelLookup.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                btnModelLookup.Click += (s, e) => OpenModelLookup();
                pnlTotals.Controls.Add(btnModelLookup);
            }

            pnlTotals.Controls.Add(lblInvoiceItemsBadge);
            pnlTotals.Controls.Add(lblItemCount);
            pnlTotals.Controls.Add(lblTotal);
            pnlTotals.Controls.Add(pnlPaymentTypes);
            pnlTotals.Controls.Add(_lPaid);
            pnlTotals.Controls.Add(txtPaid);
            pnlTotals.Controls.Add(_lVisaPaid);
            pnlTotals.Controls.Add(txtVisaPaid);
            pnlTotals.Controls.Add(lblChange);
            pnlTotals.Controls.Add(btnPay);
            pnlTotals.Controls.Add(btnNew);
            pnlTotals.Controls.Add(btnCancel);
            pnlTotals.Controls.Add(_btnWhatsApp);
            pnlTotals.Controls.Add(btnOpenDrawer);
            pnlTotals.Controls.Add(btnSuspend);
            pnlTotals.Controls.Add(btnRecall);
            pnlTotals.Controls.Add(btnIncompletePOS);

            // ── بون الخصم (Voucher Code) ──
            txtVoucherCode = new TextBox
            {
                Location = new Point(20, 10),
                Size = new Size(130, 28),
                Font = new Font("Consolas", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(253, 230, 138),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtVoucherCode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ApplyVoucherCode(); };

            btnApplyVoucher = new Button
            {
                Text = "✔ تطبيق",
                Location = new Point(158, 10),
                Size = new Size(65, 28),
                BackColor = Color.FromArgb(109, 40, 217),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnApplyVoucher.FlatAppearance.BorderSize = 0;
            btnApplyVoucher.Click += (s, e) => ApplyVoucherCode();

            lblVoucherDiscount = new Label
            {
                Location = new Point(20, 42),
                Size = new Size(203, 22),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                Text = ""
            };

            pnlTotals.Controls.Add(txtVoucherCode);
            pnlTotals.Controls.Add(btnApplyVoucher);
            pnlTotals.Controls.Add(lblVoucherDiscount);

            if (AppConfig.IsRestaurant)
            {
                btnKitchenPrint = Theme.MakeButton("🍳 بون\nمطبخ", Color.FromArgb(230, 120, 20), new Point(0, 128), new Size(95, 56));
                btnKitchenPrint.Name = "btnKitchenPrint";
                btnKitchenPrint.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                btnKitchenPrint.Click += (s, e) =>
                {
                    if (_lastSaleID <= 0) return;
                    var ans = MessageBox.Show("هل تريد طباعة بون التحضير؟", "طباعة",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    if (ans == DialogResult.Yes)
                        try { new FrmKitchenPrint(_lastSaleID); } catch { }
                };
                pnlTotals.Controls.Add(btnKitchenPrint);
            }

            this.Controls.Add(pnlTotals);

            this.FormClosing += FrmPOS_FormClosing;
            FrmQuickAdd.ProductSaved += FrmPOS_ProductSaved;
            this.Resize += (s, e) => LayoutPanels();
            this.Shown += (s, e) => LayoutPanels();
            LayoutPanels();
        }

        private void LayoutPanels()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            bool showQuick = Session.CanViewQuickItems("POS");
            if (pnlQuick != null) pnlQuick.Visible = showQuick;

            if (showQuick)
            {
                int rightW = Math.Max(320, Math.Min(450, (int)(w * 0.35)));
                int leftW = w - rightW - 30;

                // ضبط مواقع لوحات العميل والأصناف السريعة لتكون على اليمين (X = 10)
                if (pnlClient != null) { pnlClient.Location = new Point(10, 85); pnlClient.Size = new Size(rightW, 55); }

                if (pnlOrderType != null)
                {
                    pnlOrderType.Location = new Point(10, 142);
                    pnlOrderType.Size = new Size(rightW, 40);
                    pnlQuick.Location = new Point(10, 184);
                    pnlQuick.Size = new Size(rightW, h - 394);
                }
                else
                {
                    pnlQuick.Location = new Point(10, 150);
                    pnlQuick.Size = new Size(rightW, h - 360);
                }

                // ضبط موقع جدول الأصناف ليكون على اليسار (X = rightW + 20)
                dgItems.Location = new Point(rightW + 20, 85);
                dgItems.Size = new Size(leftW, h - 290);
            }
            else
            {
                // إذا تم إلغاء صلاحية الأصناف السريعة، يتم إخفاء اللوحة وتوسيع جدول الأصناف لكامل الشاشة
                int topH = 85;
                if (pnlClient != null)
                {
                    pnlClient.Location = new Point(10, topH);
                    pnlClient.Size = new Size(Math.Min(480, w - 20), 55);
                    topH += 60;
                }
                if (pnlOrderType != null)
                {
                    pnlOrderType.Location = new Point(10, topH);
                    pnlOrderType.Size = new Size(Math.Min(480, w - 20), 40);
                    topH += 45;
                }
                dgItems.Location = new Point(10, topH);
                dgItems.Size = new Size(w - 20, h - topH - 215);
            }

            pnlTotals.Location = new Point(10, h - 210);
            pnlTotals.Size = new Size(w - 20, 200);

            // ضبط مواقع عناصر الشريط العلوي لتكون على اليمين (لأن الـ Panel لا تعكس الاتجاه تلقائياً)
            if (txtBarcode != null) txtBarcode.Location = new Point(w - 320, 35);
            if (btnSearchProduct != null) btnSearchProduct.Location = new Point(w - 365, 35);
            if (btnCustomizeCols != null) btnCustomizeCols.Location = new Point(w - 465, 35);

            // تموضع اسم الشاشة وبجانبه المخزن وشريحة السعر لمنع أي تداخل
            int topX = w - 485;
            if (lblTitle != null)
            {
                int tWidth = lblTitle.PreferredWidth > 0 ? lblTitle.PreferredWidth : 200;
                topX -= tWidth;
                lblTitle.Location = new Point(topX, 26);
                topX -= 25;
            }

            int whWidth = 145;
            topX -= whWidth;
            if (lblWh != null) lblWh.Location = new Point(topX, 12);
            if (cboWarehouse != null) { cboWarehouse.Location = new Point(topX, 35); cboWarehouse.Size = new Size(whWidth, 32); }
            topX -= 15;

            int tierWidth = 110;
            topX -= tierWidth;
            if (lblTier != null) lblTier.Location = new Point(topX, 12);
            if (cboPriceTier != null) { cboPriceTier.Location = new Point(topX, 35); cboPriceTier.Size = new Size(tierWidth, 32); }

            // ── توزيع ديناميكي لعناصر لوحة الإجماليات ──────────
            int totW = pnlTotals.Width;
            // الإجمالي: أقصى اليمين
            lblTotal.Location     = new Point(totW - 360, 45);
            lblTotal.Size         = new Size(340, 40);
            lblItemCount.Location = new Point(totW - 360, 10);
            lblItemCount.Size     = new Size(340, 28);

            // نوع الدفع وحقول المدفوع: الوسط
            int midX = totW / 2;
            if (pnlPaymentTypes != null)
            {
                pnlPaymentTypes.Location = new Point(midX - 210, 8);
                pnlPaymentTypes.Size = new Size(420, 36);
                if (btnTypeCash != null) { btnTypeCash.Location = new Point(315, 0); btnTypeCash.Size = new Size(100, 34); }
                if (btnTypeVisa != null) { btnTypeVisa.Location = new Point(210, 0); btnTypeVisa.Size = new Size(100, 34); }
                if (btnTypeCredit != null) { btnTypeCredit.Location = new Point(105, 0); btnTypeCredit.Size = new Size(100, 34); }
                if (btnTypeMixed != null) { btnTypeMixed.Location = new Point(0, 0); btnTypeMixed.Size = new Size(100, 34); }
            }

            if (_selectedSaleType == "Mixed")
            {
                if (_lPaid != null) _lPaid.Location = new Point(midX + 115, 56);
                if (txtPaid != null) { txtPaid.Location = new Point(midX + 15, 52); txtPaid.Size = new Size(95, 34); }
                if (_lVisaPaid != null) _lVisaPaid.Location = new Point(midX - 45, 56);
                if (txtVisaPaid != null) { txtVisaPaid.Location = new Point(midX - 150, 52); txtVisaPaid.Size = new Size(100, 34); }
            }
            else if (_selectedSaleType == "Visa")
            {
                if (_lVisaPaid != null) _lVisaPaid.Location = new Point(midX + 20, 56);
                if (txtVisaPaid != null) { txtVisaPaid.Location = new Point(midX - 110, 52); txtVisaPaid.Size = new Size(125, 34); }
            }
            else
            {
                if (_lPaid != null) _lPaid.Location = new Point(midX + 20, 56);
                if (txtPaid != null) { txtPaid.Location = new Point(midX - 110, 52); txtPaid.Size = new Size(125, 34); }
            }

            // ── ضبط شارة أصناف الفاتورة وحقل الباقي ─────────────
            int paymentLeftEdge = (_selectedSaleType == "Mixed") ? (midX - 150) : (midX - 110);

            // شارة أصناف الفاتورة: أقصى اليسار
            int badgeW = 205;
            if (lblInvoiceItemsBadge != null)
            {
                badgeW = Math.Min(205, Math.Max(140, (paymentLeftEdge - 35) / 2));
                lblInvoiceItemsBadge.Location = new Point(20, 46);
                lblInvoiceItemsBadge.Size     = new Size(badgeW, 42);
            }

            // الباقي: يقع بين شارة الأصناف ومربع المدفوع مع هامش أمان مؤكد لمنع أي تداخل أو قص
            int changeX = 20 + badgeW + 12;
            int changeRightLimit = paymentLeftEdge - 15;
            int changeW = Math.Max(140, changeRightLimit - changeX);

            lblChange.Location = new Point(changeX, 46);
            lblChange.Size     = new Size(changeW, 42);
            lblChange.TextAlign = ContentAlignment.MiddleCenter;
            lblChange.Font     = new Font("Segoe UI", changeW < 180 ? 15f : 17.5f, FontStyle.Bold);

            // ── توزيع ديناميكي ذكي ومحكم للأزرار السفلية لمنع أي تداخل نهائياً ──
            int btnY = 128;
            int btnH = 56;
            int margin = 15;
            int gap = 6;

            var orderedButtons = new List<(Control ctrl, int baseWidth, int minWidth)>();

            // 1) الإجراءات النقدية والأساسية (أقصى اليسار)
            if (btnPay != null && btnPay.Visible)
                orderedButtons.Add((btnPay, 210, 140));

            if (btnNew != null && btnNew.Visible)
                orderedButtons.Add((btnNew, 155, 115));

            if (btnCancel != null && btnCancel.Visible)
                orderedButtons.Add((btnCancel, 120, 90));

            // 2) أدوات النشاط: فحص الألوان والمقاسات مخصص لنشاط الملابس فقط
            if (btnModelLookup != null && btnModelLookup.Visible)
                orderedButtons.Add((btnModelLookup, 125, 95));

            // 3) إدارة طلبات وحالات الفواتير (الوسط)
            var incompleteBtn = btnIncompletePOS ?? pnlTotals.Controls["btnIncompletePOS"];
            if (incompleteBtn != null && incompleteBtn.Visible)
                orderedButtons.Add((incompleteBtn, 115, 85));

            if (btnRecall != null && btnRecall.Visible)
                orderedButtons.Add((btnRecall, 120, 90));

            if (btnSuspend != null && btnSuspend.Visible)
                orderedButtons.Add((btnSuspend, 115, 85));

            // 4) خدمات المطاعم (إن وُجدت)
            var kitchenBtn = btnKitchenPrint ?? pnlTotals.Controls["btnKitchenPrint"];
            if (kitchenBtn != null && kitchenBtn.Visible)
                orderedButtons.Add((kitchenBtn, 95, 75));

            // 5) أدوات الأجهزة والتواصل (اليمين)
            if (_btnWhatsApp != null && _btnWhatsApp.Visible)
                orderedButtons.Add((_btnWhatsApp, 110, 85));

            if (btnOpenDrawer != null && btnOpenDrawer.Visible)
                orderedButtons.Add((btnOpenDrawer, 135, 100));

            int count = orderedButtons.Count;
            if (count > 0)
            {
                int totalGaps = (count - 1) * gap;
                int availableW = totW - (2 * margin) - totalGaps;
                int totalBaseWidth = 0;
                int totalMinWidth = 0;
                foreach (var b in orderedButtons)
                {
                    totalBaseWidth += b.baseWidth;
                    totalMinWidth += b.minWidth;
                }

                int remainingW = Math.Max(totalMinWidth, availableW);
                int remainingBase = totalBaseWidth;
                int curX = margin;

                for (int i = 0; i < count; i++)
                {
                    var (ctrl, baseW, minW) = orderedButtons[i];
                    int btnW;
                    if (i == count - 1)
                    {
                        btnW = remainingW;
                    }
                    else
                    {
                        btnW = (int)Math.Round((double)baseW * remainingW / remainingBase);
                        if (btnW < minW) btnW = minW;
                    }

                    ctrl.Location = new Point(curX, btnY);
                    ctrl.Size = new Size(btnW, btnH);
                    curX += btnW + gap;
                    remainingW -= btnW;
                    remainingBase -= baseW;
                }

                if (btnPay != null && btnPay.Visible)
                    btnPay.Font = new Font("Segoe UI", btnPay.Width < 170 ? 11.5f : 13.5f, FontStyle.Bold);

                if (btnNew != null && btnNew.Visible)
                    btnNew.Font = new Font("Segoe UI", btnNew.Width < 135 ? 10f : 11.5f, FontStyle.Bold);
            }
        }

        // ── اختصارات لوحة المفاتيح ───────────────────────────
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Down || keyData == Keys.Insert)
            {
                if (keyData == Keys.Down)
                {
                    if (dgItems != null && (dgItems.IsCurrentCellInEditMode || dgItems.EditingControl != null))
                    {
                        var cell = dgItems.CurrentCell;
                        if (cell != null && cell.RowIndex == dgItems.Rows.Count - 1 && cell.OwningColumn.Name != "Qty")
                        {
                            dgItems.EndEdit();
                            AddNewCodeRow();
                            return true;
                        }
                        return base.ProcessCmdKey(ref msg, keyData);
                    }
                }

                if (dgItems != null && dgItems.CurrentCell != null && dgItems.CurrentCell.RowIndex >= dgItems.Rows.Count - 1 && _pendingRowIdx < 0)
                {
                    AddNewCodeRow();
                    return true;
                }
                else if (dgItems != null && dgItems.Rows.Count == 0 && _pendingRowIdx < 0)
                {
                    AddNewCodeRow();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FrmPOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) { NewInvoice(); e.Handled = true; }
            else if (e.KeyCode == Keys.F3) { SuspendCurrentOrder(); e.Handled = true; }
            else if (e.KeyCode == Keys.F4) { RecallDraftSale(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { BtnPay_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.F6) { if (_lastSaleID > 0) PrintReceipt(_lastSaleID, askFirst: false); e.Handled = true; }
            else if (e.KeyCode == Keys.F7) { SetPaymentType("Cash"); e.Handled = true; }
            else if (e.KeyCode == Keys.F8) { SetPaymentType("Visa"); e.Handled = true; }
            else if (e.KeyCode == Keys.F9) { SetPaymentType("Credit"); e.Handled = true; }
            else if (e.KeyCode == Keys.F10) { SetPaymentType("Mixed"); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape && _items.Count == 0) { this.Close(); e.Handled = true; }
            else if (e.KeyCode == Keys.F12) { txtBarcode.Focus(); txtBarcode.SelectAll(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.D) { RawPrinterHelper.OpenCashDrawer(); e.Handled = true; }
        }

        private void SetPaymentType(string type)
        {
            if (type == "Credit")
            {
                if (!Session.IsAdmin && !Session.CanSellCredit)
                {
                    MessageBox.Show("⛔ عفوًا: ليس لديك صلاحية البيع بالأجل!", "صلاحية غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!(cboClient.SelectedItem is ComboItem ci) || ci.ID <= 0)
                {
                    MessageBox.Show("⚠️ تنبيه: البيع بالأجل (آجل) يتطلب اختيار عميل مسجل أولاً!\nيرجى تحديد العميل من قائمة العملاء بالأعلى.", "اختيار العميل مطلوب", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboClient.Focus();
                    return;
                }

                DataRow clientRow = ClientDAL.GetByID(ci.ID);
                if (clientRow != null && clientRow.Table.Columns.Contains("DefaultPaymentType") && clientRow["DefaultPaymentType"] != DBNull.Value)
                {
                    string ptype = clientRow["DefaultPaymentType"].ToString();
                    if (string.Equals(ptype, "Cash", StringComparison.OrdinalIgnoreCase) || ptype == "كاش")
                    {
                        MessageBox.Show("⚠️ هذا العميل محدَّد في كارت العميل لـ (كاش فقط)، لا يمكن البيع له بالأجل!", "طريقة الدفع غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            else if (type == "Visa" || type == "Mixed")
            {
                if (!Session.IsAdmin && !Session.CanSellVisa)
                {
                    MessageBox.Show("⛔ عفوًا: ليس لديك صلاحية البيع بالفيزا / البطاقة!", "صلاحية غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_selectedVisaAccountID == null || _selectedVisaAccountID <= 0)
                {
                    var dtVisa = AccountDAL.GetActiveVisaAccounts();
                    if (dtVisa.Rows.Count == 1)
                    {
                        _selectedVisaAccountID = Convert.ToInt32(dtVisa.Rows[0]["AccountID"]);
                        _selectedVisaAccountName = dtVisa.Rows[0]["AccountName"].ToString();
                    }
                }
            }
            else if (type == "Cash")
            {
                if (!Session.IsAdmin && !Session.CanSellCash)
                {
                    MessageBox.Show("⛔ عفوًا: ليس لديك صلاحية البيع النقدي!", "صلاحية غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            _selectedSaleType = type;
            UpdatePaymentTypeButtons();
            LayoutPanels();
            RecalcChange();
        }

        private void UpdatePaymentTypeButtons()
        {
            var activeColor = Theme.Primary;
            var inactiveColor = Color.FromArgb(45, 52, 70);
            var activeText = Color.White;
            var inactiveText = Color.FromArgb(180, 195, 215);

            if (btnTypeCash != null)
            {
                btnTypeCash.BackColor = _selectedSaleType == "Cash" ? activeColor : inactiveColor;
                btnTypeCash.ForeColor = _selectedSaleType == "Cash" ? activeText : inactiveText;
            }

            if (btnTypeVisa != null)
            {
                btnTypeVisa.BackColor = _selectedSaleType == "Visa" ? Color.FromArgb(142, 68, 173) : inactiveColor;
                btnTypeVisa.ForeColor = _selectedSaleType == "Visa" ? activeText : inactiveText;
                if (_selectedSaleType == "Visa" && !string.IsNullOrEmpty(_selectedVisaAccountName))
                {
                    btnTypeVisa.Text = $"💳 {_selectedVisaAccountName}";
                }
                else
                {
                    btnTypeVisa.Text = "💳 فيزا (F8)";
                }
            }

            if (btnTypeCredit != null)
            {
                btnTypeCredit.BackColor = _selectedSaleType == "Credit" ? Color.FromArgb(230, 126, 34) : inactiveColor;
                btnTypeCredit.ForeColor = _selectedSaleType == "Credit" ? activeText : inactiveText;
            }

            if (btnTypeMixed != null)
            {
                btnTypeMixed.BackColor = _selectedSaleType == "Mixed" ? Color.FromArgb(22, 160, 133) : inactiveColor;
                btnTypeMixed.ForeColor = _selectedSaleType == "Mixed" ? activeText : inactiveText;
            }

            decimal total = 0;
            foreach (var item in _items) total += item.Total;
            if (chkRedeemPoints != null && chkRedeemPoints.Checked && AppConfig.LoyaltyEnabled && cboClient != null && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", ci.ID));
                decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                total -= Math.Min(points * AppConfig.LoyaltyRedemptionRate, total);
            }

            if (_selectedSaleType == "Cash")
            {
                if (_lPaid != null) { _lPaid.Visible = true; _lPaid.Text = "المدفوع كاش:"; }
                if (txtPaid != null) { txtPaid.Visible = true; txtPaid.Text = total.ToString("N2"); }
                if (_lVisaPaid != null) _lVisaPaid.Visible = false;
                if (txtVisaPaid != null) { txtVisaPaid.Visible = false; txtVisaPaid.Text = "0"; }
            }
            else if (_selectedSaleType == "Visa")
            {
                if (_lPaid != null) _lPaid.Visible = false;
                if (txtPaid != null) { txtPaid.Visible = false; txtPaid.Text = "0"; }
                if (_lVisaPaid != null) { _lVisaPaid.Visible = true; _lVisaPaid.Text = "المدفوع فيزا:"; }
                if (txtVisaPaid != null) { txtVisaPaid.Visible = true; txtVisaPaid.Text = total.ToString("N2"); }
            }
            else if (_selectedSaleType == "Credit")
            {
                if (_lPaid != null) { _lPaid.Visible = true; _lPaid.Text = "المسدد مقدماً:"; }
                if (txtPaid != null) { txtPaid.Visible = true; txtPaid.Text = "0"; }
                if (_lVisaPaid != null) _lVisaPaid.Visible = false;
                if (txtVisaPaid != null) { txtVisaPaid.Visible = false; txtVisaPaid.Text = "0"; }
            }
            else if (_selectedSaleType == "Mixed")
            {
                if (_lPaid != null) { _lPaid.Visible = true; _lPaid.Text = "كاش:"; }
                if (txtPaid != null) { txtPaid.Visible = true; }
                if (_lVisaPaid != null) { _lVisaPaid.Visible = true; _lVisaPaid.Text = "فيزا:"; }
                if (txtVisaPaid != null) { txtVisaPaid.Visible = true; }

                if (txtPaid != null && txtVisaPaid != null)
                {
                    if (!decimal.TryParse(txtPaid.Text.Replace(",", ""), out decimal cp) || cp == 0)
                    {
                        txtPaid.Text = (total / 2m).ToString("N2");
                        txtVisaPaid.Text = (total - (total / 2m)).ToString("N2");
                    }
                }
            }
        }

        // ── مسح الباركود ──────────────────────────────────────
        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string rawInput = txtBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(rawInput))
                {
                    decimal multiQty = 1m;
                    string code = rawInput;
                    if (rawInput.Contains("*"))
                    {
                        var parts = rawInput.Split(new char[] { '*' }, 2);
                        if (decimal.TryParse(parts[0].Trim(), out decimal q) && q > 0)
                        {
                            multiQty = q;
                            code = parts[1].Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(_lastScannedBarcode) &&
                        string.Equals(_lastScannedBarcode, code, StringComparison.OrdinalIgnoreCase) &&
                        (DateTime.Now - _lastScanTime).TotalMilliseconds < BARCODE_DEBOUNCE_MS)
                    {
                        txtBarcode.Clear();
                        return;
                    }

                    AddProductByCode(code, multiQty, focusQty: false);
                    txtBarcode.Clear();
                    txtBarcode.Focus();
                }
            }
            else if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                AddNewCodeRow();
            }
        }

        private void OpenModelLookup()
        {
            using (var dlg = new FrmModelLookup())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedProductID > 0)
                {
                    var dt = DbHelper.Query("SELECT ProductCode FROM Products WHERE ProductID=@id", DbHelper.P("@id", dlg.SelectedProductID));
                    if (dt.Rows.Count > 0)
                    {
                        AddProductByCode(dt.Rows[0]["ProductCode"].ToString());
                    }
                }
            }
        }

        private void AddProductByCode(string code, decimal requestedQty = 1m, bool focusQty = false)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            if (!string.IsNullOrEmpty(_lastScannedBarcode) &&
                string.Equals(_lastScannedBarcode, code, StringComparison.OrdinalIgnoreCase) &&
                (DateTime.Now - _lastScanTime).TotalMilliseconds < BARCODE_DEBOUNCE_MS)
            {
                if (txtBarcode != null) txtBarcode.Clear();
                return;
            }
            _lastScannedBarcode = code;
            _lastScanTime = DateTime.Now;

            // بحث بالباركود أو الكود
            string trimmedC = code.TrimStart('0');
            if (string.IsNullOrEmpty(trimmedC)) trimmedC = "0";
            string paddedC = code;
            if (int.TryParse(code, out int cVal))
            {
                paddedC = cVal.ToString("D8");
            }

            var dt = DbHelper.Query(@"
                SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice,
                       p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                       p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice, p.Unit2Factor,
                       p.Unit3Factor, p.DefaultSaleUnit,
                       p.WholesalePrice, p.SemiWholesalePrice, p.MinStockLimit, COALESCE(p.IsService, 0) AS IsService,
                       p.InternationalCode, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                FROM Products p
                WHERE p.IsActive = 1 AND (
                    p.ProductCode = @c OR p.ProductCode = @trimmed OR p.ProductCode = @padded OR 
                    p.InternationalCode = @c OR p.InternationalCode = @trimmed OR ',' + p.InternationalCode + ',' LIKE '%,' + @c + ',%' OR 
                    p.Unit1Barcode = @c OR p.Unit1Barcode = @trimmed OR p.Unit1Barcode = @padded OR ',' + p.Unit1Barcode + ',' LIKE '%,' + @c + ',%' OR 
                    p.Unit2Barcode = @c OR p.Unit2Barcode = @trimmed OR p.Unit2Barcode = @padded OR ',' + p.Unit2Barcode + ',' LIKE '%,' + @c + ',%'
                )",
                DbHelper.P("@c", code), DbHelper.P("@trimmed", trimmedC), DbHelper.P("@padded", paddedC));

            if (dt.Rows.Count == 0)
            {
                // Handle barcode-weight (e.g. prefix 99, 20, 21, 22, 27, 9)
                var parseRes = BarcodeParser.Parse(code);
                if (parseRes.IsScaleBarcode)
                {
                    string itemCode = parseRes.ItemCode;
                    string trimmedItemCode = parseRes.TrimmedItemCode;
                    decimal weight = parseRes.WeightOrPrice;
                    string paddedItemCode = itemCode;
                    if (int.TryParse(itemCode, out int itemCodeVal))
                    {
                        paddedItemCode = itemCodeVal.ToString("D8");
                    }

                    // 1) First try to find product by ScalePLU
                    dt = DbHelper.Query(@"
                        SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                               p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                               p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice, p.Unit2Factor,
                               p.Unit3Factor, p.DefaultSaleUnit,
                               p.WholesalePrice, p.SemiWholesalePrice, p.MinStockLimit, COALESCE(p.IsService, 0) AS IsService,
                               p.InternationalCode, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                        FROM Products p 
                        WHERE p.IsActive = 1 AND (
                            p.ScalePLU = @c OR 
                            p.ScalePLU = @trimmed OR 
                            p.ScalePLU = @padded OR
                            (@itemCodeVal > 0 AND ISNUMERIC(p.ScalePLU) = 1 AND CAST(p.ScalePLU AS INT) = @itemCodeVal)
                        )", 
                        DbHelper.P("@c", itemCode), DbHelper.P("@trimmed", trimmedItemCode), DbHelper.P("@padded", paddedItemCode), DbHelper.P("@itemCodeVal", itemCodeVal));

                    // 2) Fall back to ProductCode/ProductID if ScalePLU is not set
                    if (dt.Rows.Count == 0)
                    {
                        dt = DbHelper.Query(@"
                            SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                                   p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                                   p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice, p.Unit2Factor,
                                   p.Unit3Factor, p.DefaultSaleUnit,
                                   p.WholesalePrice, p.SemiWholesalePrice, p.MinStockLimit, COALESCE(p.IsService, 0) AS IsService,
                                   p.InternationalCode, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                            FROM Products p 
                            WHERE p.IsActive = 1 AND (
                                p.ProductCode = @c OR 
                                p.ProductCode = @trimmed OR 
                                p.ProductCode = @padded OR 
                                p.InternationalCode = @c OR 
                                p.InternationalCode = @trimmed OR
                                (@itemCodeVal > 0 AND p.ProductID = @itemCodeVal) OR
                                (@itemCodeVal > 0 AND CAST(p.ProductID AS VARCHAR) = @trimmed) OR
                                (ISNUMERIC(p.ProductCode) = 1 AND CAST(p.ProductCode AS INT) = @itemCodeVal)
                            )", 
                            DbHelper.P("@c", itemCode), DbHelper.P("@trimmed", trimmedItemCode), DbHelper.P("@padded", paddedItemCode), DbHelper.P("@itemCodeVal", itemCodeVal));
                    }
                    if (dt.Rows.Count > 0 && weight > 0)
                    {
                        var row2 = dt.Rows[0];
                        int pid2 = Convert.ToInt32(row2["ProductID"]);
                        int? bid2 = null;
                        DateTime? exp2 = null;
                        bool isInt2 = (row2["InternationalCode"] != DBNull.Value && code == row2["InternationalCode"].ToString());
                        if (row2["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row2["HasExpiry"]))
                        {
                            var batches = DbHelper.Query("SELECT BatchID, ExpiryDate FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", pid2), DbHelper.P("@wid", GetSelectedWarehouseID()));
                            if (batches.Rows.Count > 0)
                            {
                                int oldestId = Convert.ToInt32(batches.Rows[0]["BatchID"]);
                                DateTime? oldestExp = batches.Rows[0]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[0]["ExpiryDate"]) : (DateTime?)null;
                                if (isInt2)
                                {
                                    bid2 = oldestId; exp2 = oldestExp;
                                }
                                else if (oldestExp.HasValue)
                                {
                                    if (MessageBox.Show("يوجد تاريخ أقرب سينتهي، هل تريد بيعه أولاً؟", "تنبيه تاريخ الصلاحية", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                                    {
                                        bid2 = oldestId; exp2 = oldestExp;
                                    }
                                    else
                                    {
                                        if (batches.Rows.Count > 1)
                                        {
                                            bid2 = Convert.ToInt32(batches.Rows[1]["BatchID"]);
                                            exp2 = batches.Rows[1]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[1]["ExpiryDate"]) : (DateTime?)null;
                                        }
                                        else
                                        {
                                            bid2 = oldestId; exp2 = oldestExp;
                                        }
                                    }
                                }
                                else
                                {
                                    bid2 = oldestId; exp2 = oldestExp;
                                }
                            }
                            else
                            {
                                MessageBox.Show("❌ عجز: لا توجد أي تشغيلات (صلاحيات) متوفرة لهذا الصنف في هذا المخزن حالياً!", "عجز الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                        AddItemFromRow(row2, weight, null, 1m, 0, bid2, exp2);
                        return;
                    }
                }

                MessageBox.Show("لم يتم العثور على صنف بهذا الكود.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dt.Rows[0];
            int productID = Convert.ToInt32(row["ProductID"]);
            // Check if barcode matches a sub-unit
            string unitName = null;
            decimal factor = 1m;
            decimal price = Convert.ToDecimal(row["SalePrice"]);
            string u1b = row["Unit1Barcode"] != DBNull.Value ? row["Unit1Barcode"].ToString() : "";
            string u2b = row["Unit2Barcode"] != DBNull.Value ? row["Unit2Barcode"].ToString() : "";

            if (!string.IsNullOrEmpty(u1b) && ProductDAL.BarcodeMatches(u1b, code))
            {
                unitName = row["Unit1Name"]?.ToString();
                if (row["Unit1SalePrice"] != DBNull.Value && Convert.ToDecimal(row["Unit1SalePrice"]) > 0) 
                    price = Convert.ToDecimal(row["Unit1SalePrice"]);
                factor = 1m;
            }
            else if (!string.IsNullOrEmpty(u2b) && ProductDAL.BarcodeMatches(u2b, code))
            {
                unitName = row["Unit2Name"]?.ToString();
                if (row["Unit2SalePrice"] != DBNull.Value && Convert.ToDecimal(row["Unit2SalePrice"]) > 0) 
                    price = Convert.ToDecimal(row["Unit2SalePrice"]);
                if (row["Unit2Factor"] != DBNull.Value && Convert.ToDecimal(row["Unit2Factor"]) > 0) 
                    factor = Convert.ToDecimal(row["Unit2Factor"]);
            }

            int? batchID = null;
            DateTime? expiryDate = null;
            bool isInternational = (row["InternationalCode"] != DBNull.Value && code == row["InternationalCode"].ToString());
            if (row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]))
            {
                var batches = DbHelper.Query("SELECT BatchID, ExpiryDate FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", productID), DbHelper.P("@wid", GetSelectedWarehouseID()));
                if (batches.Rows.Count > 0)
                {
                    int oldestId = Convert.ToInt32(batches.Rows[0]["BatchID"]);
                    DateTime? oldestExp = batches.Rows[0]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[0]["ExpiryDate"]) : (DateTime?)null;
                    if (isInternational)
                    {
                        batchID = oldestId; expiryDate = oldestExp;
                    }
                    else if (oldestExp.HasValue)
                    {
                        if (MessageBox.Show("يوجد تاريخ أقرب سينتهي، هل تريد بيعه أولاً؟", "تنبيه تاريخ الصلاحية", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            batchID = oldestId; expiryDate = oldestExp;
                        }
                        else
                        {
                            if (batches.Rows.Count > 1)
                            {
                                batchID = Convert.ToInt32(batches.Rows[1]["BatchID"]);
                                expiryDate = batches.Rows[1]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[1]["ExpiryDate"]) : (DateTime?)null;
                            }
                            else
                            {
                                batchID = oldestId; expiryDate = oldestExp;
                            }
                        }
                    }
                    else
                    {
                        batchID = oldestId; expiryDate = oldestExp;
                    }
                }
                else
                {
                    MessageBox.Show("❌ عجز: لا توجد أي تشغيلات (صلاحيات) متوفرة لهذا الصنف في هذا المخزن حالياً!", "عجز الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            decimal qtyToAdd = _pendingScaleWeight.HasValue ? _pendingScaleWeight.Value : (requestedQty > 0 ? requestedQty : 1m);
            _pendingScaleWeight = null;
            AddItemFromRow(row, qtyToAdd, unitName, factor, price, batchID, expiryDate, 0m, focusQty);
        }

        private void AddItemFromRow(DataRow row, decimal qty, string unitName, decimal factor, decimal overridePrice = 0, int? batchID = null, DateTime? expiryDate = null, decimal discountAmt = 0m, bool focusQty = false)
        {
            if (expiryDate.HasValue && expiryDate.Value < DateTime.Today && !AppConfig.AllowSellExpired)
            {
                MessageBox.Show("❌ عجز: هذا الصنف منتهي الصلاحية ولا يسمح النظام ببيعه حسب الإعدادات الحالية!", "تنبيه الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int productID = Convert.ToInt32(row["ProductID"]);
            string code = row["ProductCode"]?.ToString() ?? "";
            string name = row["ProductName"]?.ToString() ?? "";
            decimal tierMajorPrice = Convert.ToDecimal(row["SalePrice"]);
            string curTier = GetSelectedPriceTier();
            if (curTier == "جملة" && row.Table.Columns.Contains("WholesalePrice") && row["WholesalePrice"] != DBNull.Value && Convert.ToDecimal(row["WholesalePrice"]) > 0)
                tierMajorPrice = Convert.ToDecimal(row["WholesalePrice"]);
            else if (curTier == "نصف جملة" && row.Table.Columns.Contains("SemiWholesalePrice") && row["SemiWholesalePrice"] != DBNull.Value && Convert.ToDecimal(row["SemiWholesalePrice"]) > 0)
                tierMajorPrice = Convert.ToDecimal(row["SemiWholesalePrice"]);

            decimal price = overridePrice > 0 ? overridePrice : tierMajorPrice;
            decimal majorCost = row["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["PurchasePrice"]) : 0;

            if (string.IsNullOrEmpty(unitName))
            {
                string defUnit = row.Table.Columns.Contains("DefaultSaleUnit") && row["DefaultSaleUnit"] != DBNull.Value 
                    ? row["DefaultSaleUnit"].ToString() : "";
                if (string.IsNullOrEmpty(defUnit)) defUnit = "الكبرى";

                string u1Name = row.Table.Columns.Contains("Unit1Name") && row["Unit1Name"] != DBNull.Value ? row["Unit1Name"].ToString() : null;
                string u2Name = row.Table.Columns.Contains("Unit2Name") && row["Unit2Name"] != DBNull.Value ? row["Unit2Name"].ToString() : null;
                string baseUnit = row.Table.Columns.Contains("Unit") && row["Unit"] != DBNull.Value ? row["Unit"].ToString() : null;

                if (defUnit == "الوسطى" && !string.IsNullOrEmpty(u2Name))
                {
                    unitName = u2Name;
                    if (row["Unit2SalePrice"] != DBNull.Value) price = Convert.ToDecimal(row["Unit2SalePrice"]);
                    if (row["Unit2Factor"] != DBNull.Value) factor = Convert.ToDecimal(row["Unit2Factor"]);
                }
                else if (defUnit == "الصغرى" && !string.IsNullOrEmpty(u1Name))
                {
                    unitName = u1Name;
                    if (row["Unit1SalePrice"] != DBNull.Value) price = Convert.ToDecimal(row["Unit1SalePrice"]);
                    factor = 1m;
                }
                else // "الكبرى" or default
                {
                    unitName = !string.IsNullOrEmpty(baseUnit) ? baseUnit : u1Name;
                    decimal u2f = row.Table.Columns.Contains("Unit2Factor") && row["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(row["Unit2Factor"]) : 1m;
                    decimal u3f = row.Table.Columns.Contains("Unit3Factor") && row["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(row["Unit3Factor"]) : 1m;
                    factor = u2f * u3f;
                    price = overridePrice > 0 ? overridePrice : tierMajorPrice;
                }
            }

            decimal u2fVal = row.Table.Columns.Contains("Unit2Factor") && row["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(row["Unit2Factor"]) : 1m;
            decimal u3fVal = row.Table.Columns.Contains("Unit3Factor") && row["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(row["Unit3Factor"]) : 1m;
            if (u2fVal <= 0) u2fVal = 1m;
            if (u3fVal <= 0) u3fVal = 1m;
            decimal totalFactorVal = u2fVal * u3fVal;

            decimal cost = majorCost;
            if (row.Table.Columns.Contains("Unit1Name") && row["Unit1Name"] != DBNull.Value && string.Equals(unitName, row["Unit1Name"].ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (row.Table.Columns.Contains("Unit1PurchasePrice") && row["Unit1PurchasePrice"] != DBNull.Value && Convert.ToDecimal(row["Unit1PurchasePrice"]) > 0)
                    cost = Convert.ToDecimal(row["Unit1PurchasePrice"]);
                else if (totalFactorVal > 0)
                    cost = Math.Round(majorCost / totalFactorVal, 2);
            }
            else if (row.Table.Columns.Contains("Unit2Name") && row["Unit2Name"] != DBNull.Value && string.Equals(unitName, row["Unit2Name"].ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (row.Table.Columns.Contains("Unit2PurchasePrice") && row["Unit2PurchasePrice"] != DBNull.Value && Convert.ToDecimal(row["Unit2PurchasePrice"]) > 0)
                    cost = Convert.ToDecimal(row["Unit2PurchasePrice"]);
                else if (u3fVal > 0)
                    cost = Math.Round(majorCost / u3fVal, 2);
            }

            bool hasExpiry = row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]);

            // Check if item already in list (same product + unit + price + same batch if hasExpiry)
            var existing = _items.Find(i => i.ProductID == productID && 
                                            i.Price == price &&
                                            i.UnitName == unitName && 
                                            (!hasExpiry || (i.BatchID == batchID && i.ExpiryDate == expiryDate)));

            decimal targetQty = qty;
            if (existing != null)
            {
                targetQty += existing.Qty;
            }

            bool isService = false;
            var isServiceObj = DbHelper.Scalar("SELECT IsService FROM Products WHERE ProductID=@pid", DbHelper.P("@pid", productID));
            if (isServiceObj != null && isServiceObj != DBNull.Value && Convert.ToBoolean(isServiceObj))
            {
                isService = true;
            }

            if (!isService)
            {
                int curWhId = GetSelectedWarehouseID();
                decimal availableStock = 0m;
                if (batchID.HasValue)
                {
                    var qtyObj = DbHelper.Scalar("SELECT Quantity FROM ProductBatches WHERE BatchID=@bid AND WarehouseID=@wid", DbHelper.P("@bid", batchID.Value), DbHelper.P("@wid", curWhId));
                    availableStock = qtyObj != null && qtyObj != DBNull.Value ? Convert.ToDecimal(qtyObj) : 0m;
                }
                else
                {
                    availableStock = InventoryDAL.GetProductStock(productID, curWhId);
                }

                if (availableStock <= 0m)
                {
                    string curWhName = cboWarehouse != null ? cboWarehouse.Text : "المخزن المحدد";
                    MessageBox.Show($"❌ عجز: الصنف '{name}' ليس لديه رصيد متاح في {curWhName} حالياً (الرصيد: 0)!\nالبيع بالسالب غير مسموح.", "رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal maxAvailInUnit = availableStock / (factor > 0 ? factor : 1m);

                if (targetQty > maxAvailInUnit)
                {
                    MessageBox.Show($"❌ عجز: الكمية المطلوبة ({targetQty:G29}) أكبر من الرصيد المتاح بالمخزن ({maxAvailInUnit:G29}) للصنف '{name}'!\nالبيع بالسالب غير مسموح.", "رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (existing != null)
            {
                existing.Qty = targetQty;
                if (discountAmt > 0)
                {
                    existing.DiscountAmt = discountAmt;
                }
                existing.Total = (existing.Qty * existing.Price) - existing.DiscountAmt;
                RefreshGrid();
                if (focusQty) FocusQtyCell(existing);
                return;
            }

            var newItem = new POSItem
            {
                ProductID = productID,
                Code = code,
                Name = name,
                Unit = row.Table.Columns.Contains("Unit") && row["Unit"] != DBNull.Value ? row["Unit"].ToString() : "",
                UnitName = unitName,
                BaseUnitName = row.Table.Columns.Contains("Unit") && row["Unit"] != DBNull.Value ? row["Unit"].ToString() : "",
                Unit1Name = row.Table.Columns.Contains("Unit1Name") && row["Unit1Name"] != DBNull.Value ? row["Unit1Name"].ToString() : "",
                Unit2Name = row.Table.Columns.Contains("Unit2Name") && row["Unit2Name"] != DBNull.Value ? row["Unit2Name"].ToString() : "",
                Unit1SalePrice = row.Table.Columns.Contains("Unit1SalePrice") && row["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(row["Unit1SalePrice"]) : 0m,
                Unit2SalePrice = row.Table.Columns.Contains("Unit2SalePrice") && row["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(row["Unit2SalePrice"]) : 0m,
                Unit1Cost = row.Table.Columns.Contains("Unit1PurchasePrice") && row["Unit1PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["Unit1PurchasePrice"]) : 0m,
                Unit2Cost = row.Table.Columns.Contains("Unit2PurchasePrice") && row["Unit2PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["Unit2PurchasePrice"]) : 0m,
                Unit2Factor = u2fVal,
                Unit3Factor = u3fVal,
                MajorPrice = tierMajorPrice,
                MajorCost = majorCost,
                MinStockLimit = row.Table.Columns.Contains("MinStockLimit") && row["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(row["MinStockLimit"]) : 0m,
                IsService = isService,
                Factor = factor,
                Qty = qty,
                Price = price,
                Cost = cost,
                Total = (qty * price) - discountAmt,
                DiscountAmt = discountAmt,
                HasExpiry = row.Table.Columns.Contains("HasExpiry") && row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]),
                DefaultExpiryDays = row.Table.Columns.Contains("DefaultExpiryDays") && row["DefaultExpiryDays"] != DBNull.Value ? Convert.ToInt32(row["DefaultExpiryDays"]) : (int?)null,
                BatchID = batchID,
                ExpiryDate = expiryDate
            };
            _items.Add(newItem);
            RefreshGrid();
            if (focusQty) FocusQtyCell(newItem);
            try { SystemSounds.Asterisk.Play(); } catch { } // صوت تنبيه عند إضافة صنف
        }

        private void FocusQtyCell(POSItem item)
        {
            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    int rowIndex = _items.IndexOf(item);
                    if (rowIndex >= 0 && rowIndex < dgItems.Rows.Count)
                    {
                        dgItems.Focus();
                        dgItems.CurrentCell = dgItems.Rows[rowIndex].Cells["Qty"]; // استخدام الاسم للأمان
                        dgItems.BeginEdit(true); // يدخل وضع التعديل فوراً والوقوف على الكمية
                    }
                }
                catch { }
            }));
        }

        private void RefreshGrid()
        {
            dgItems.Rows.Clear();
            _pendingRowIdx = -1;
            decimal total = 0;
            int clientID = (cboClient != null && cboClient.SelectedItem is ComboItem ciClient) ? ciClient.ID : 0;
            foreach (var item in _items)
            {
                item.Total = (item.Qty * item.Price) - item.DiscountAmt;
                decimal? lastPrice = (clientID > 0) ? SaleDAL.GetLastPriceForClient(item.ProductID, clientID) : null;
                string lastPriceStr = lastPrice.HasValue ? lastPrice.Value.ToString("N2") + " ج" : "-";

                EnsureItemUnitMetadata(item);

                decimal availBaseStock = GetProductAvailableStock(item.ProductID, item.BatchID);
                decimal f = item.Factor > 0 ? item.Factor : 1m;
                decimal stockInUnit = item.IsService ? 9999m : (availBaseStock / f);
                item.StockQty = stockInUnit;

                int rIdx = dgItems.Rows.Add();
                var row = dgItems.Rows[rIdx];
                row.Cells["Code"].Value = item.Code;
                row.Cells["Name"].Value = item.Name;
                row.Cells["StockQty"].Value = item.IsService ? "خدمي" : stockInUnit.ToString("G29");
                row.Cells["Qty"].Value = item.Qty.ToString("G");
                row.Cells["QtyPlus"].Value = "+";
                row.Cells["QtyMinus"].Value = "-";
                row.Cells["Price"].Value = item.Price.ToString("N2");
                row.Cells["LastClientPrice"].Value = lastPriceStr;
                row.Cells["IMEI"].Value = item.IMEI ?? "";
                row.Cells["Discount"].Value = item.DiscountAmt.ToString("N2");
                row.Cells["Total"].Value = item.Total.ToString("N2");
                if (AppConfig.IsRestaurant && dgItems.Columns.Contains("KitchenNotes"))
                {
                    row.Cells["KitchenNotes"].Value = item.KitchenNotes ?? "";
                }

                SetupPosUnitCombo(rIdx, item);
                SetupPosSerialCombo(rIdx, item);

                if (rIdx >= 0)
                {
                    row.Cells["Code"].ReadOnly = true;

                    var stockCell = row.Cells["StockQty"];
                    if (stockCell != null && !item.IsService)
                    {
                        if (stockInUnit <= 0)
                        {
                            stockCell.Style.ForeColor = Color.FromArgb(231, 76, 60); // Red
                            stockCell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                            stockCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        }
                        else if (item.MinStockLimit > 0 && stockInUnit <= item.MinStockLimit)
                        {
                            stockCell.Style.ForeColor = Color.FromArgb(230, 126, 34); // Orange
                            stockCell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                            stockCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        }
                        else
                        {
                            stockCell.Style.ForeColor = Color.FromArgb(46, 204, 113); // Green
                            stockCell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                            stockCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        }
                    }
                }
                total += item.Total;
            }

            decimal loyaltyDiscount = 0;
            if (chkRedeemPoints != null && chkRedeemPoints.Checked && AppConfig.LoyaltyEnabled && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", ci.ID));
                decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                loyaltyDiscount = Math.Min(points * AppConfig.LoyaltyRedemptionRate, total);
            }

            lblTotal.Text = $"الإجمالي: {(total - loyaltyDiscount - _voucherDiscount):N2} ج";
            decimal totalPieces = 0;
            foreach (var it in _items) totalPieces += it.Qty;
            lblItemCount.Text = $"عدد الأصناف: {_items.Count}   |   عدد القطع: {totalPieces:G29}";
            if (lblInvoiceItemsBadge != null)
            {
                lblInvoiceItemsBadge.Text = totalPieces > 0 && totalPieces != _items.Count
                    ? $"📦 أصناف الفاتورة: {_items.Count} ({totalPieces:G29} ق)"
                    : $"📦 أصناف الفاتورة: {_items.Count}";
            }
            decimal netTotal = total - loyaltyDiscount - _voucherDiscount;
            if (_selectedSaleType == "Cash" && txtPaid != null) txtPaid.Text = netTotal.ToString("N2");
            else if (_selectedSaleType == "Visa" && txtVisaPaid != null) txtVisaPaid.Text = netTotal.ToString("N2");

            // تحديث label الخصم لو البون طبّق نسبة مئوية (القيمة تتغير بتغير الإجمالي)
            if (_appliedVoucherID > 0 && _voucherDiscount > 0 && lblVoucherDiscount != null)
            {
                var vDto = DiscountVouchersDAL.GetByID(_appliedVoucherID);
                if (vDto != null)
                {
                    decimal recalc = vDto.CalculateDiscount(total - loyaltyDiscount);
                    if (recalc != _voucherDiscount)
                    {
                        _voucherDiscount = recalc;
                        string discLabel = vDto.DiscountType == "Percent"
                            ? $"🎟️ خصم {vDto.DiscountValue:G29}%: -{recalc:N2} ج"
                            : $"🎟️ خصم: -{recalc:N2} ج";
                        lblVoucherDiscount.Text = discLabel;
                    }
                }
            }

            // توسيع خانة اسم الصنف تلقائياً إذا كان اسم أي صنف أكبر من الخانة
            if (dgItems.Columns.Contains("Name"))
            {
                float maxNameWidth = 180f;
                using (var g = dgItems.CreateGraphics())
                {
                    var font = dgItems.Columns["Name"].DefaultCellStyle.Font ?? dgItems.Font;
                    foreach (var itm in _items)
                    {
                        if (!string.IsNullOrEmpty(itm.Name))
                        {
                            var sz = g.MeasureString(itm.Name, font);
                            if (sz.Width + 28 > maxNameWidth)
                            {
                                maxNameWidth = sz.Width + 28;
                            }
                        }
                    }
                }
                int prefWidth = dgItems.Columns["Name"].GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true);
                dgItems.Columns["Name"].Width = Math.Max((int)maxNameWidth, prefWidth);
            }

            RecalcChange();
            AutoSavePOSDraft();
        }

        private void SetupPosUnitCombo(int rIndex, POSItem item)
        {
            if (rIndex < 0 || rIndex >= dgItems.Rows.Count) return;
            if (!dgItems.Columns.Contains("UnitName")) return;

            EnsureItemUnitMetadata(item);

            var unitCell = dgItems.Rows[rIndex].Cells["UnitName"] as DataGridViewComboBoxCell;
            if (unitCell == null) return;

            var unitList = new System.Collections.ArrayList();

            // 1. الوحدة الكبرى (الأساسية)
            string baseU = !string.IsNullOrEmpty(item.BaseUnitName) ? item.BaseUnitName : (!string.IsNullOrEmpty(item.Unit) ? item.Unit : "");
            if (!string.IsNullOrEmpty(baseU) && !unitList.Contains(baseU))
            {
                unitList.Add(baseU);
            }

            // 2. الوحدة الوسطى (إن وُجدت وليست مكررة)
            if (!string.IsNullOrEmpty(item.Unit2Name) && !unitList.Contains(item.Unit2Name))
            {
                unitList.Add(item.Unit2Name);
            }

            // 3. الوحدة الصغرى (إن وُجدت وليست مكررة)
            if (!string.IsNullOrEmpty(item.Unit1Name) && !unitList.Contains(item.Unit1Name))
            {
                unitList.Add(item.Unit1Name);
            }

            if (unitList.Count == 0)
            {
                string defU = !string.IsNullOrEmpty(item.UnitName) ? item.UnitName : "وحدة";
                unitList.Add(defU);
            }

            unitCell.DataSource = null;
            unitCell.Items.Clear();
            foreach (var u in unitList)
            {
                if (u != null && !unitCell.Items.Contains(u.ToString()))
                    unitCell.Items.Add(u.ToString());
            }

            // تعيين القيمة المحفوظة (أو الافتراضية) مع ضمان وجودها في القائمة
            string savedUnit = item.UnitName;
            if (!string.IsNullOrEmpty(savedUnit))
            {
                if (!unitCell.Items.Contains(savedUnit))
                    unitCell.Items.Add(savedUnit);
                unitCell.Value = savedUnit;
            }
            else if (unitCell.Items.Count > 0)
            {
                unitCell.Value = unitCell.Items[0];
                item.UnitName = unitCell.Items[0].ToString();
            }
        }

        private void EnsureItemUnitMetadata(POSItem item)
        {
            if (item == null || item.ProductID <= 0) return;
            if (!string.IsNullOrEmpty(item.BaseUnitName) || !string.IsNullOrEmpty(item.Unit1Name) || !string.IsNullOrEmpty(item.Unit2Name))
            {
                return; // already loaded
            }

            try
            {
                var dt = DbHelper.Query(@"
                    SELECT Unit, Unit1Name, Unit2Name, 
                           Unit1SalePrice, Unit2SalePrice, 
                           Unit1PurchasePrice, Unit2PurchasePrice, 
                           Unit2Factor, Unit3Factor, 
                           SalePrice, PurchasePrice, WholesalePrice, SemiWholesalePrice,
                           MinStockLimit, IsService
                    FROM Products WITH (NOLOCK)
                    WHERE ProductID = @id", DbHelper.P("@id", item.ProductID));

                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    item.BaseUnitName = r["Unit"] != DBNull.Value ? r["Unit"].ToString() : "";
                    item.Unit1Name = r["Unit1Name"] != DBNull.Value ? r["Unit1Name"].ToString() : "";
                    item.Unit2Name = r["Unit2Name"] != DBNull.Value ? r["Unit2Name"].ToString() : "";

                    item.Unit1SalePrice = r["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit1SalePrice"]) : 0m;
                    item.Unit2SalePrice = r["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit2SalePrice"]) : 0m;
                    item.Unit1Cost = r["Unit1PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit1PurchasePrice"]) : 0m;
                    item.Unit2Cost = r["Unit2PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit2PurchasePrice"]) : 0m;

                    item.Unit2Factor = r["Unit2Factor"] != DBNull.Value && Convert.ToDecimal(r["Unit2Factor"]) > 0 ? Convert.ToDecimal(r["Unit2Factor"]) : 1m;
                    item.Unit3Factor = r["Unit3Factor"] != DBNull.Value && Convert.ToDecimal(r["Unit3Factor"]) > 0 ? Convert.ToDecimal(r["Unit3Factor"]) : 1m;

                    string curTier = GetSelectedPriceTier();
                    decimal tierMajorPrice = r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m;
                    if (curTier == "جملة" && r["WholesalePrice"] != DBNull.Value && Convert.ToDecimal(r["WholesalePrice"]) > 0)
                        tierMajorPrice = Convert.ToDecimal(r["WholesalePrice"]);
                    else if (curTier == "نصف جملة" && r["SemiWholesalePrice"] != DBNull.Value && Convert.ToDecimal(r["SemiWholesalePrice"]) > 0)
                        tierMajorPrice = Convert.ToDecimal(r["SemiWholesalePrice"]);

                    item.MajorPrice = tierMajorPrice;
                    item.MajorCost = r["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                    item.MinStockLimit = r["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(r["MinStockLimit"]) : 0m;
                    item.IsService = r["IsService"] != DBNull.Value && Convert.ToBoolean(r["IsService"]);
                }
            }
            catch { }
        }

        private decimal GetProductAvailableStock(int productID, int? batchID = null)
        {
            try
            {
                if (batchID.HasValue && batchID.Value > 0)
                {
                    int curWhId = GetSelectedWarehouseID();
                    var qtyObj = DbHelper.Scalar("SELECT Quantity FROM ProductBatches WITH (NOLOCK) WHERE BatchID=@bid AND WarehouseID=@wid", DbHelper.P("@bid", batchID.Value), DbHelper.P("@wid", curWhId));
                    return qtyObj != null && qtyObj != DBNull.Value ? Convert.ToDecimal(qtyObj) : 0m;
                }

                if (_stockCache != null && _stockCache.TryGetValue(productID, out decimal cachedStock))
                {
                    return cachedStock;
                }

                int wid = GetSelectedWarehouseID();
                return InventoryDAL.GetProductStock(productID, wid);
            }
            catch
            {
                return 0m;
            }
        }

        private void HandlePosUnitChange(int rowIndex, string newUnit)
        {
            if (rowIndex < 0 || rowIndex >= _items.Count) return;
            var item = _items[rowIndex];
            if (item == null || string.IsNullOrEmpty(newUnit)) return;
            if (item.UnitName == newUnit && item.Factor > 0) return;

            EnsureItemUnitMetadata(item);
            item.UnitName = newUnit;

            decimal u2f = item.Unit2Factor > 0 ? item.Unit2Factor : 1m;
            decimal u3f = item.Unit3Factor > 0 ? item.Unit3Factor : 1m;
            decimal totalFactor = u2f * u3f;

            if (!string.IsNullOrEmpty(item.Unit2Name) && string.Equals(newUnit, item.Unit2Name, StringComparison.OrdinalIgnoreCase))
            {
                // 1. الوحدة الوسطى
                item.Factor = u2f;
                if (item.Unit2SalePrice > 0)
                {
                    item.Price = item.Unit2SalePrice;
                }
                else if (u3f > 0 && item.MajorPrice > 0)
                {
                    item.Price = Math.Round(item.MajorPrice / u3f, 2);
                }
                else
                {
                    item.Price = item.MajorPrice;
                }

                if (item.Unit2Cost > 0)
                {
                    item.Cost = item.Unit2Cost;
                }
                else if (u3f > 0 && item.MajorCost > 0)
                {
                    item.Cost = Math.Round(item.MajorCost / u3f, 2);
                }
                else
                {
                    item.Cost = item.MajorCost;
                }
            }
            else if (!string.IsNullOrEmpty(item.Unit1Name) && string.Equals(newUnit, item.Unit1Name, StringComparison.OrdinalIgnoreCase))
            {
                // 2. الوحدة الصغرى (التجزئة)
                item.Factor = 1m;
                if (item.Unit1SalePrice > 0)
                {
                    item.Price = item.Unit1SalePrice;
                }
                else if (totalFactor > 0 && item.MajorPrice > 0)
                {
                    item.Price = Math.Round(item.MajorPrice / totalFactor, 2);
                }
                else
                {
                    item.Price = item.MajorPrice;
                }

                if (item.Unit1Cost > 0)
                {
                    item.Cost = item.Unit1Cost;
                }
                else if (totalFactor > 0 && item.MajorCost > 0)
                {
                    item.Cost = Math.Round(item.MajorCost / totalFactor, 2);
                }
                else
                {
                    item.Cost = item.MajorCost;
                }
            }
            else if (!string.IsNullOrEmpty(item.BaseUnitName) && string.Equals(newUnit, item.BaseUnitName, StringComparison.OrdinalIgnoreCase))
            {
                // 3. الوحدة الكبرى (الأساسية)
                item.Factor = totalFactor;
                item.Price = item.MajorPrice;
                item.Cost = item.MajorCost;
            }
            else
            {
                // احتياطي
                item.Factor = 1m;
                item.Price = item.MajorPrice > 0 ? item.MajorPrice : item.Price;
                item.Cost = item.MajorCost > 0 ? item.MajorCost : item.Cost;
            }

            // Check stock warning for the new unit
            if (!item.IsService)
            {
                decimal availBase = GetProductAvailableStock(item.ProductID, item.BatchID);
                decimal maxAvailInUnit = availBase / (item.Factor > 0 ? item.Factor : 1m);
                if (item.Qty > maxAvailInUnit)
                {
                    MessageBox.Show($"⚠️ تنبيه: الكمية المطلوبة ({item.Qty:G29}) أكبر من الرصيد المتاح للوحدة المختارة ({maxAvailInUnit:G29}) للصنف '{item.Name}'!", "تنبيه المخزون", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            item.Total = (item.Qty * item.Price) - item.DiscountAmt;
            RefreshGrid();
        }

        private void SetupPosSerialCombo(int rIndex, POSItem item)
        {
            if (dgItems.Columns.Contains("IMEI"))
            {
                var availableSerials = PurchaseDAL.GetAvailableSerialsForProduct(item.ProductID);
                if (availableSerials != null && availableSerials.Count > 0)
                {
                    var comboCell = new DataGridViewComboBoxCell();
                    comboCell.Items.Add("");
                    foreach (var s in availableSerials)
                    {
                        comboCell.Items.Add(s);
                    }
                    dgItems.Rows[rIndex].Cells["IMEI"] = comboCell;
                    if (!string.IsNullOrEmpty(item.IMEI) && comboCell.Items.Contains(item.IMEI))
                    {
                        comboCell.Value = item.IMEI;
                    }
                    else if (comboCell.Items.Count > 1)
                    {
                        comboCell.Value = comboCell.Items[1];
                        item.IMEI = comboCell.Value.ToString();
                    }
                }
            }
        }

        private void RecalcChange()
        {
            decimal total = 0;
            foreach (var item in _items)
            {
                item.Total = (item.Qty * item.Price) - item.DiscountAmt;
                total += item.Total;
            }

            // Loyalty redemption
            decimal loyaltyDiscount = 0;
            if (chkRedeemPoints != null && chkRedeemPoints.Checked && AppConfig.LoyaltyEnabled && cboClient != null && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", ci.ID));
                decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                loyaltyDiscount = Math.Min(points * AppConfig.LoyaltyRedemptionRate, total);
                total -= loyaltyDiscount;
            }

            decimal cashPaid = 0;
            decimal visaPaid = 0;
            if (txtPaid != null && decimal.TryParse(txtPaid.Text.Replace(",", ""), out decimal cp)) cashPaid = cp;
            if (txtVisaPaid != null && decimal.TryParse(txtVisaPaid.Text.Replace(",", ""), out decimal vp)) visaPaid = vp;

            if (_selectedSaleType == "Cash")
            {
                decimal change = cashPaid - total;
                lblChange.Text = $"الباقي: {change:N2} ج";
                lblChange.ForeColor = change >= 0 ? Theme.Accent : Theme.Danger;
            }
            else if (_selectedSaleType == "Visa")
            {
                decimal change = visaPaid - total;
                lblChange.Text = $"الباقي: {change:N2} ج";
                lblChange.ForeColor = change >= 0 ? Theme.Accent : Theme.Danger;
            }
            else if (_selectedSaleType == "Credit")
            {
                decimal remainingCredit = total - cashPaid;
                if (remainingCredit <= 0)
                {
                    lblChange.Text = $"الباقي: {Math.Abs(remainingCredit):N2} ج";
                    lblChange.ForeColor = Theme.Accent;
                }
                else
                {
                    lblChange.Text = $"المتبقي آجل: {remainingCredit:N2} ج";
                    lblChange.ForeColor = Color.FromArgb(243, 156, 18);
                }
            }
            else if (_selectedSaleType == "Mixed")
            {
                decimal totalPaid = cashPaid + visaPaid;
                decimal change = totalPaid - total;
                if (change >= 0)
                {
                    lblChange.Text = $"الباقي: {change:N2} ج";
                    lblChange.ForeColor = Theme.Accent;
                }
                else
                {
                    lblChange.Text = $"المتبقي عجز: {Math.Abs(change):N2} ج";
                    lblChange.ForeColor = Theme.Danger;
                }
            }
        }

        private bool CheckAvailableStock(int productID, int? batchID, decimal qtyInFactor, out decimal available, out string errorMessage)
        {
            available = 0;
            errorMessage = "";

            var isServiceObj = DbHelper.Scalar("SELECT IsService FROM Products WITH (NOLOCK) WHERE ProductID=@pid", DbHelper.P("@pid", productID));
            if (isServiceObj != null && isServiceObj != DBNull.Value && Convert.ToBoolean(isServiceObj))
            {
                return true;
            }

            int wid = GetSelectedWarehouseID();
            if (batchID.HasValue)
            {
                var qtyObj = DbHelper.Scalar("SELECT Quantity FROM ProductBatches WITH (NOLOCK) WHERE BatchID=@bid AND WarehouseID=@wid", DbHelper.P("@bid", batchID.Value), DbHelper.P("@wid", wid));
                available = qtyObj != null && qtyObj != DBNull.Value ? Convert.ToDecimal(qtyObj) : 0m;
                if (qtyInFactor > available)
                {
                    errorMessage = $"❌ عجز: الكمية المطلوبة ({qtyInFactor:G29}) أكبر من الكمية المتاحة في تشغيلية الصلاحية المحددة ({available:G29})!";
                    return false;
                }
            }
            else
            {
                available = InventoryDAL.GetProductStock(productID, wid);
                if (qtyInFactor > available)
                {
                    errorMessage = $"❌ عجز: الكمية المطلوبة ({qtyInFactor:G29}) أكبر من الكمية المتاحة في المخزن حالياً ({available:G29})!";
                    return false;
                }
            }
            return true;
        }

        /// <summary>يضيف سطراً فارغاً في الجدول ويضع الكيرسور على عمود كود الصنف مباشرة</summary>
        private void AddNewCodeRow()
        {
            try
            {
                // إزالة سطر الكود المعلق السابق إذا كان فارغاً
                if (_pendingRowIdx >= 0 && _pendingRowIdx < dgItems.Rows.Count)
                {
                    var prevCell = dgItems.Rows[_pendingRowIdx].Cells["Code"];
                    if (prevCell.Value == null || string.IsNullOrEmpty(prevCell.Value.ToString()))
                    {
                        dgItems.Rows.RemoveAt(_pendingRowIdx);
                    }
                }

                // إضافة سطر فارغ جديد
                _pendingRowIdx = dgItems.Rows.Add();
                dgItems.Rows[_pendingRowIdx].DefaultCellStyle.BackColor = Color.FromArgb(235, 245, 255);
                dgItems.Rows[_pendingRowIdx].Cells["Code"].ReadOnly = false;

                // الانتقال لخلية الكود في السطر الجديد والبدء في وضع التعديل
                dgItems.ClearSelection();
                dgItems.CurrentCell = dgItems.Rows[_pendingRowIdx].Cells["Code"];
                dgItems.BeginEdit(true);
                dgItems.FirstDisplayedScrollingRowIndex = _pendingRowIdx;
            }
            catch { }
        }

        private bool IsProductCodeOrBarcode(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 2) return false;
            try
            {
                string trimmedC = text.TrimStart('0');
                if (string.IsNullOrEmpty(trimmedC)) trimmedC = "0";
                string paddedC = text;
                if (int.TryParse(text, out int cv)) paddedC = cv.ToString("D8");

                var dtScan = DbHelper.Query(@"
                    SELECT TOP 1 ProductID FROM Products
                    WHERE IsActive = 1 AND (
                        ProductCode = @c OR ProductCode = @tr OR ProductCode = @pd
                        OR InternationalCode = @c OR ',' + InternationalCode + ',' LIKE '%,' + @c + ',%'
                        OR Unit1Barcode = @c OR Unit1Barcode = @tr OR ',' + Unit1Barcode + ',' LIKE '%,' + @c + ',%'
                        OR Unit2Barcode = @c OR Unit2Barcode = @tr OR ',' + Unit2Barcode + ',' LIKE '%,' + @c + ',%'
                        OR ScalePLU = @c OR ScalePLU = @tr
                    )",
                    DbHelper.P("@c", text), DbHelper.P("@tr", trimmedC), DbHelper.P("@pd", paddedC));

                if (dtScan.Rows.Count > 0) return true;

                if (text.Length >= 6 && BarcodeParser.Parse(text).IsScaleBarcode) return true;
            }
            catch { }
            return false;
        }

        private void CellTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                {
                    string text = tb.Text.Trim();
                    if (!string.IsNullOrEmpty(text) && dgItems.CurrentCell != null)
                    {
                        string colName = dgItems.CurrentCell.OwningColumn.Name;
                        if (colName == "Qty" || colName == "Price" || colName == "Discount")
                        {
                            // فحص هل ما تم مسحه هو باركود صنف لتفادي كتابة الباركود في خانة الكمية وزيادة عدد الصنف بدلاً من ذلك
                            if (IsProductCodeOrBarcode(text))
                            {
                                e.Handled = true;
                                e.SuppressKeyPress = true;
                                dgItems.CancelEdit();
                                if (dgItems.CurrentRow != null && dgItems.CurrentRow.Index < _items.Count)
                                {
                                    var curItem = _items[dgItems.CurrentRow.Index];
                                    dgItems.Rows[dgItems.CurrentRow.Index].Cells["Qty"].Value = curItem.Qty.ToString("G");
                                }
                                this.BeginInvoke(new Action(() =>
                                {
                                    AddProductByCode(text);
                                }));
                                return;
                            }
                        }
                        else if (colName == "Code")
                        {
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                            dgItems.EndEdit();
                            return;
                        }
                    }
                }
                else if (e.KeyCode == Keys.Down)
                {
                    if (dgItems.CurrentCell != null && dgItems.CurrentCell.RowIndex >= dgItems.Rows.Count - 1)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        dgItems.EndEdit();
                        AddNewCodeRow();
                        return;
                    }
                }
            }
        }

        // ── تعديل الكمية من الجدول ────────────────────────────
        private void DgItems_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgItems.Rows.Count) return;
            string colName = dgItems.Columns[e.ColumnIndex].Name;

            // معالجة خلية كود الصنف (السطر المعلق)
            if (colName == "Code")
            {
                string code = dgItems.Rows[e.RowIndex].Cells["Code"].Value?.ToString()?.Trim() ?? "";
                int rowIdx = e.RowIndex;
                this.BeginInvoke(new Action(() =>
                {
                    if (string.IsNullOrEmpty(code))
                    {
                        if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count && rowIdx == _pendingRowIdx)
                            dgItems.Rows.RemoveAt(rowIdx);
                        _pendingRowIdx = -1;
                        return;
                    }
                    if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count && rowIdx == _pendingRowIdx)
                        dgItems.Rows.RemoveAt(rowIdx);
                    _pendingRowIdx = -1;
                    AddProductByCode(code);
                }));
                return;
            }

            if (e.RowIndex >= _items.Count) return;
            var item = _items[e.RowIndex];

            // ── معالجة تغيير الوحدة ──
            if (colName == "UnitName")
            {
                string newUnit = dgItems.Rows[e.RowIndex].Cells["UnitName"].Value?.ToString() ?? "";
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (e.RowIndex >= 0 && e.RowIndex < _items.Count)
                        HandlePosUnitChange(e.RowIndex, newUnit);
                });
                return;
            }

            if (colName == "Qty")
            {
                string cellText = dgItems.Rows[e.RowIndex].Cells["Qty"].Value?.ToString()?.Trim() ?? "";

                // ── كشف سكانر: لو ما كُتب في خانة الكمية يطابق كود/باركود منتج ──
                if (IsProductCodeOrBarcode(cellText))
                {
                    dgItems.Rows[e.RowIndex].Cells["Qty"].Value = item.Qty.ToString("G");
                    string scannedCode = cellText;
                    this.BeginInvoke(new Action(() =>
                    {
                        AddProductByCode(scannedCode);
                    }));
                    return;
                }

                // ── كمية عادية ──
                if (decimal.TryParse(cellText, out decimal newQty) && newQty > 0)
                {
                    if (!CheckAvailableStock(item.ProductID, item.BatchID, newQty * item.Factor, out decimal available, out string err))
                    {
                        MessageBox.Show(err + "\nالبيع بالسالب غير مسموح.", "تنبيه عجز رصيد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        dgItems.Rows[e.RowIndex].Cells["Qty"].Value = item.Qty.ToString("G");
                        return;
                    }
                    else
                    {
                        item.Qty = newQty;
                    }
                    item.Total = (item.Qty * item.Price) - item.DiscountAmt;
                    this.BeginInvoke(new Action(RefreshGrid));
                }
                else
                {
                    dgItems.Rows[e.RowIndex].Cells["Qty"].Value = item.Qty.ToString("G");
                }
            }
            else if (colName == "Price")
            {
                if (decimal.TryParse(dgItems.Rows[e.RowIndex].Cells["Price"].Value?.ToString(), out decimal newPrice) && newPrice >= 0)
                {
                    if (!Session.CanSellBelowCost("POS") && item.Cost > 0 && newPrice < item.Cost)
                    {
                        string costNotice = Session.CanViewCost("POS") ? $" أقل من سعر التكلفة ({item.Cost:N2})." : " أقل من الحد الأدنى المسموح به للبيع.";
                        MessageBox.Show($"❌ غير مسموح ببيع الصنف '{item.Name}' بسعر ({newPrice:N2}){costNotice}", "تنبيه سعر البيع", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        dgItems.Rows[e.RowIndex].Cells["Price"].Value = item.Price.ToString("N2");
                        return;
                    }

                    decimal testNetTotal = (item.Qty * newPrice) - item.DiscountAmt;
                    decimal testNetUnit = item.Qty > 0 ? (testNetTotal / item.Qty) : newPrice;
                    if (!Session.CanSellBelowCost("POS") && item.Cost > 0 && testNetUnit < item.Cost)
                    {
                        string costNotice = Session.CanViewCost("POS") ? $" أقل من سعر التكلفة ({item.Cost:N2})." : " أقل من الحد الأدنى المسموح به للبيع.";
                        MessageBox.Show($"❌ السعر المدخل مع الخصم الحالي يجعل صافي سعر الصنف '{item.Name}' ({testNetUnit:N2}){costNotice}", "تنبيه سعر البيع", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        dgItems.Rows[e.RowIndex].Cells["Price"].Value = item.Price.ToString("N2");
                        return;
                    }

                    item.Price = newPrice;
                    item.Total = (item.Qty * newPrice) - item.DiscountAmt;
                    this.BeginInvoke(new Action(RefreshGrid));
                }
                else
                {
                    dgItems.Rows[e.RowIndex].Cells["Price"].Value = item.Price.ToString("N2");
                }
            }
            else if (colName == "Discount")
            {
                if (decimal.TryParse(dgItems.Rows[e.RowIndex].Cells["Discount"].Value?.ToString(), out decimal newDisc) && newDisc >= 0)
                {
                    decimal netTotal = (item.Qty * item.Price) - newDisc;
                    decimal netUnitPrice = item.Qty > 0 ? (netTotal / item.Qty) : item.Price;
                    if (!Session.CanSellBelowCost("POS") && item.Cost > 0 && netUnitPrice < item.Cost)
                    {
                        string costNotice = Session.CanViewCost("POS") ? $" أقل من سعر التكلفة ({item.Cost:N2})." : " أقل من الحد الأدنى المسموح به للبيع.";
                        MessageBox.Show($"❌ قيمة الخصم تجعل صافي سعر بيع الصنف '{item.Name}' ({netUnitPrice:N2}){costNotice}", "تنبيه سعر البيع", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        dgItems.Rows[e.RowIndex].Cells["Discount"].Value = item.DiscountAmt.ToString("N2");
                        return;
                    }

                    item.DiscountAmt = newDisc;
                    item.Total = (item.Qty * item.Price) - newDisc;
                    this.BeginInvoke(new Action(RefreshGrid));
                }
                else
                {
                    dgItems.Rows[e.RowIndex].Cells["Discount"].Value = item.DiscountAmt.ToString("N2");
                }
            }
            else if (colName == "KitchenNotes")
            {
                item.KitchenNotes = dgItems.Rows[e.RowIndex].Cells["KitchenNotes"].Value?.ToString() ?? "";
            }
            else if (colName == "IMEI")
            {
                item.IMEI = dgItems.Rows[e.RowIndex].Cells["IMEI"].Value?.ToString()?.Trim() ?? "";
            }
        }

        private void DgItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dgItems.CurrentRow != null)
            {
                int idx = dgItems.CurrentRow.Index;
                if (idx >= 0 && idx < _items.Count)
                {
                    _items.RemoveAt(idx);
                    RefreshGrid();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Insert)
            {
                if (dgItems.CurrentCell != null && dgItems.CurrentCell.RowIndex >= dgItems.Rows.Count - 1 && _pendingRowIdx < 0)
                {
                    e.Handled = true;
                    AddNewCodeRow();
                }
            }
        }

        // ── إتمام البيع ──────────────────────────────────────
        private void BtnPay_Click(object sender, EventArgs e)
        {
            if (_isSaving) return;
            if (_items.Count == 0) { MessageBox.Show("لا يوجد أصناف في الفاتورة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            _isSaving = true;
            int draftToDelete = _loadedDraftSaleID;
            _loadedDraftSaleID = 0;

            try
            {
                // ── ضمان وجود وردية مفتوحة للكاشير تلقائياً ──
                if (!ShiftDAL.GetActiveShiftID().HasValue)
                {
                    ShiftDAL.EnsureActiveShift(Session.EmpID);
                }

                int clientID = 0;
                if (cboClient != null && cboClient.SelectedItem is ComboItem ci) clientID = ci.ID;

                // ── التحقق من متطلبات طريقة الدفع وصلاحيات الموظف ──
                if (_selectedSaleType == "Credit")
                {
                    if (clientID <= 0)
                    {
                        MessageBox.Show("⚠️ تنبيه: البيع بالأجل (آجل) يتطلب اختيار عميل مسجل أولاً!\nيرجى تحديد العميل من قائمة العملاء بالأعلى.", "اختيار العميل مطلوب", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (cboClient != null) cboClient.Focus();
                        _isSaving = false;
                        return;
                    }
                    if (!Session.IsAdmin && !Session.CanSellCredit)
                    {
                        MessageBox.Show("⛔ عفوًا: ليس لديك صلاحية البيع بالأجل!", "صلاحية غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _isSaving = false;
                        return;
                    }
                }
                else if (_selectedSaleType == "Visa" || _selectedSaleType == "Mixed")
                {
                    if (!Session.IsAdmin && !Session.CanSellVisa)
                    {
                        MessageBox.Show("⛔ عفوًا: ليس لديك صلاحية البيع بالفيزا / البطاقة!", "صلاحية غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _isSaving = false;
                        return;
                    }
                }
                else if (_selectedSaleType == "Cash")
                {
                    if (!Session.IsAdmin && !Session.CanSellCash)
                    {
                        MessageBox.Show("⛔ عفوًا: ليس لديك صلاحية البيع النقدي!", "صلاحية غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _isSaving = false;
                        return;
                    }
                }

                decimal total = 0;
                foreach (var item in _items) total += item.Total;

                // Loyalty
                decimal loyaltyDiscount = 0;
                decimal pointsToRedeem = 0;
                if (chkRedeemPoints != null && chkRedeemPoints.Checked && AppConfig.LoyaltyEnabled && clientID > 0)
                {
                    var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", clientID));
                    decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                    loyaltyDiscount = Math.Min(points * AppConfig.LoyaltyRedemptionRate, total);
                    pointsToRedeem = loyaltyDiscount / AppConfig.LoyaltyRedemptionRate;
                    total -= loyaltyDiscount;
                }

                // خصم بون الخصم
                if (_appliedVoucherID > 0 && _voucherDiscount > 0)
                {
                    total = Math.Max(0m, total - _voucherDiscount);
                }

                // حساب المدفوع كاش وفيزا حسب نوع الدفع المختار
                decimal cashPaidVal = 0;
                decimal visaPaidVal = 0;

                if (_selectedSaleType == "Cash")
                {
                    cashPaidVal = (txtPaid != null && decimal.TryParse(txtPaid.Text.Replace(",", ""), out decimal cp)) ? cp : total;
                    visaPaidVal = 0;
                }
                else if (_selectedSaleType == "Visa")
                {
                    cashPaidVal = 0;
                    visaPaidVal = (txtVisaPaid != null && decimal.TryParse(txtVisaPaid.Text.Replace(",", ""), out decimal vp)) ? vp : total;
                }
                else if (_selectedSaleType == "Credit")
                {
                    cashPaidVal = (txtPaid != null && decimal.TryParse(txtPaid.Text.Replace(",", ""), out decimal cp)) ? cp : 0;
                    visaPaidVal = 0;
                }
                else if (_selectedSaleType == "Mixed")
                {
                    if (txtPaid != null && decimal.TryParse(txtPaid.Text.Replace(",", ""), out decimal cp)) cashPaidVal = cp;
                    if (txtVisaPaid != null && decimal.TryParse(txtVisaPaid.Text.Replace(",", ""), out decimal vp)) visaPaidVal = vp;

                    decimal totalPaid = cashPaidVal + visaPaidVal;
                    if (totalPaid < total && clientID <= 0)
                    {
                        MessageBox.Show("⚠️ إجمالي المدفوع (كاش + فيزا) أقل من قيمة الفاتورة!\nيلزم اختيار عميل مسجل لتسجيل باقي المبلغ كآجل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (cboClient != null) cboClient.Focus();
                        _isSaving = false;
                        return;
                    }
                }

                // تحديد حساب / ماكينة الفيزا إن وُجد سداد بالفيزا
                if (visaPaidVal > 0)
                {
                    if (_selectedVisaAccountID == null || _selectedVisaAccountID <= 0)
                    {
                        if (!FrmSelectVisaAccount.SelectVisaAccount(this, visaPaidVal, _selectedVisaAccountID, out int chosenVid, out string chosenVname))
                        {
                            _isSaving = false;
                            return;
                        }
                        _selectedVisaAccountID = chosenVid;
                        _selectedVisaAccountName = chosenVname;
                    }
                    else
                    {
                        var dtActiveVisa = AccountDAL.GetActiveVisaAccounts();
                        if (dtActiveVisa.Rows.Count > 1 && string.IsNullOrEmpty(_selectedVisaAccountName))
                        {
                            if (!FrmSelectVisaAccount.SelectVisaAccount(this, visaPaidVal, _selectedVisaAccountID, out int chosenVid, out string chosenVname))
                            {
                                _isSaving = false;
                                return;
                            }
                            _selectedVisaAccountID = chosenVid;
                            _selectedVisaAccountName = chosenVname;
                        }
                    }
                }

                // Extract restaurant fields if active
                string orderType = null;
                string tableNum = null;
                int? selectedDriver = null;
                if (AppConfig.IsRestaurant)
                {
                    orderType = rbDineIn.Checked ? "DineIn" : rbDelivery.Checked ? "Delivery" : "Takeaway";
                    tableNum = rbDineIn.Checked ? txtTableNum.Text.Trim() : null;
                    if (rbDelivery.Checked && cboDeliveryDriver.SelectedItem is ComboItem driverItem && driverItem.ID > 0)
                    {
                        selectedDriver = driverItem.ID;
                    }
                }

                // ── التحقق من المخزون الحي قبل الحفظ (منع البيع بالسالب للأصناف غير الخدمية) ──
                var groupedStockItems = _items
                    .GroupBy(x => new { x.ProductID, x.BatchID, x.Name })
                    .Select(g => new { g.Key.ProductID, g.Key.BatchID, g.Key.Name, TotalBaseQty = g.Sum(x => x.Qty * x.Factor) });

                foreach (var gItem in groupedStockItems)
                {
                    if (!CheckAvailableStock(gItem.ProductID, gItem.BatchID, gItem.TotalBaseQty, out decimal avail, out string errMsg))
                    {
                        MessageBox.Show($"عذراً، الصنف '{gItem.Name}' رصيده لا يكفي لإتمام البيع!\n{errMsg}\n\n⚠️ تم منع البيع لأن الصنف ليس صنف خدمة وغير مسموح ببيعه بالسالب.", "منع البيع بالسالب", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _isSaving = false;
                        return;
                    }
                }

                // ── التحقق من عدم بيع أي صنف بأقل من سعر التكلفة ──
                if (!Session.CanSellBelowCost("POS"))
                {
                    foreach (var item in _items)
                    {
                        if (item.Cost > 0)
                        {
                            decimal netUnit = item.Qty > 0 ? (item.Total / item.Qty) : item.Price;
                            if (netUnit < item.Cost - 0.001m)
                            {
                                string costNotice = Session.CanViewCost("POS") ? $" أقل من سعر التكلفة ({item.Cost:N2})." : " يقل عن الحد الأدنى المسموح به.";
                                MessageBox.Show($"❌ لا يمكن حفظ الفاتورة لأن صافي سعر بيع الصنف '{item.Name}' بعد الخصم ({netUnit:N2}){costNotice}", "تنبيه سعر البيع", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                _isSaving = false;
                                return;
                            }
                        }
                    }
                }

                DbHelper.RunInTransaction((con, trans) =>
                {
                    if (draftToDelete > 0)
                    {
                        DbHelper.ExecuteTrans(trans, "DELETE FROM SaleItems WHERE SaleID=@id", DbHelper.P("@id", draftToDelete));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM Sales WHERE SaleID=@id AND IsPosted=0", DbHelper.P("@id", draftToDelete));
                    }

                    var nextSaleResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                    string saleCode = nextSaleResult != null ? nextSaleResult.ToString() : "1";
                    int warehouseID = GetSelectedWarehouseID();
                    string salePriceTier = GetSelectedPriceTier();

                    decimal sumItemDiscounts = 0;
                    foreach (var item in _items) sumItemDiscounts += item.DiscountAmt;
                    decimal totalDisc = loyaltyDiscount + sumItemDiscounts;

                    int saleID = DbHelper.ExecuteInsertTrans(trans,
                        @"INSERT INTO Sales (SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,DiscountAmount,DiscountPct,Notes,CreatedBy,IsPosted,WarehouseID,PriceTier,ShiftID,CashPaid,VisaPaid,VisaAccountID,ShippingCharge,OrderType,TableNumber)
                          VALUES (@sc,GETDATE(),@stype,@cid,@did,@tot,@disc,0,'POS',@emp,1,@wid,@tier,@sid,@paid,@vpaid,@vaid,0,@ot,@tn)",
                        DbHelper.P("@sc", saleCode),
                        DbHelper.P("@stype", _selectedSaleType),
                        DbHelper.P("@cid", clientID > 0 ? (object)clientID : DBNull.Value),
                        DbHelper.P("@did", selectedDriver.HasValue ? (object)selectedDriver.Value : DBNull.Value),
                        DbHelper.P("@tot", total), DbHelper.P("@disc", totalDisc),
                        DbHelper.P("@emp", Session.EmpID), DbHelper.P("@wid", warehouseID),
                        DbHelper.P("@tier", salePriceTier),
                        DbHelper.P("@sid", Session.CurrentShiftID.HasValue ? (object)Session.CurrentShiftID.Value : DBNull.Value),
                        DbHelper.P("@paid", cashPaidVal),
                        DbHelper.P("@vpaid", visaPaidVal),
                        DbHelper.P("@vaid", _selectedVisaAccountID.HasValue ? (object)_selectedVisaAccountID.Value : DBNull.Value),
                        DbHelper.P("@ot", string.IsNullOrEmpty(orderType) ? DBNull.Value : (object)orderType),
                        DbHelper.P("@tn", string.IsNullOrEmpty(tableNum) ? DBNull.Value : (object)tableNum));

                    if (saleID <= 0) throw new Exception("فشل حفظ الفاتورة.");

                    // 2. Save items + update stock
                    foreach (var item in _items)
                    {
                        decimal costToSave = item.Unit1Cost > 0m ? item.Unit1Cost : (item.Cost > 0m && item.Factor > 0m ? item.Cost / item.Factor : item.Cost);
                        if (costToSave <= 0m)
                        {
                            var costObj = DbHelper.ScalarTrans(trans,
                                "SELECT COALESCE(NULLIF(CostPrice, 0), NULLIF(Unit1PurchasePrice, 0), PurchasePrice, 0) FROM Products WHERE ProductID = @pid",
                                DbHelper.P("@pid", item.ProductID));
                            costToSave = (costObj != null && costObj != DBNull.Value) ? Convert.ToDecimal(costObj) : 0m;
                        }

                        if (costToSave <= 0m)
                        {
                            var lastPurCost = DbHelper.ScalarTrans(trans,
                                "SELECT TOP 1 (UnitPrice / NULLIF(Factor, 0)) FROM PurchaseItems WHERE ProductID = @pid AND UnitPrice > 0 ORDER BY PurchaseItemID DESC",
                                DbHelper.P("@pid", item.ProductID));
                            if (lastPurCost != null && lastPurCost != DBNull.Value)
                                costToSave = Convert.ToDecimal(lastPurCost);
                        }

                        DbHelper.ExecuteInsertTrans(trans,
                            @"INSERT INTO SaleItems (SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier,UnitName,Factor,ExpiryDate,BatchID,KitchenNotes,IMEI,CostPrice)
                              VALUES (@sid,@pid,@qty,@up,@tp,0,@discAmt,@tier,@un,@f,@exp,@bid,@kn,@imei,@cp)",
                            DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                            DbHelper.P("@qty", item.Qty), DbHelper.P("@up", item.Price), DbHelper.P("@tp", item.Total),
                            DbHelper.P("@discAmt", item.DiscountAmt),
                            DbHelper.P("@tier", salePriceTier),
                            DbHelper.P("@un", (object)item.UnitName ?? DBNull.Value),
                            DbHelper.P("@f", item.Factor),
                            DbHelper.P("@exp", item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value : DBNull.Value),
                            DbHelper.P("@bid", item.BatchID.HasValue ? (object)item.BatchID.Value : DBNull.Value),
                            DbHelper.P("@kn", string.IsNullOrEmpty(item.KitchenNotes) ? DBNull.Value : (object)item.KitchenNotes),
                            DbHelper.P("@imei", string.IsNullOrEmpty(item.IMEI) ? DBNull.Value : (object)item.IMEI.Trim()),
                            DbHelper.P("@cp", costToSave));

                        // Deduct from ProductBatches table
                        if (item.BatchID.HasValue)
                        {
                            decimal baseQty = item.Qty * item.Factor;
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                DbHelper.P("@q", baseQty), DbHelper.P("@bid", item.BatchID.Value));
                        }
                        else
                        {
                            var hasExpObj = DbHelper.ScalarTrans(trans, "SELECT HasExpiry FROM Products WHERE ProductID = @pid", DbHelper.P("@pid", item.ProductID));
                            if (hasExpObj != null && hasExpObj != DBNull.Value && Convert.ToBoolean(hasExpObj))
                            {
                                decimal remainingQty = item.Qty * item.Factor;
                                var batchesDt = DbHelper.QueryTrans(trans, 
                                    "SELECT BatchID, Quantity FROM ProductBatches WHERE ProductID = @pid AND WarehouseID = @wid AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC",
                                    DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", warehouseID));
                                foreach (DataRow bRow in batchesDt.Rows)
                                {
                                    int bId = Convert.ToInt32(bRow["BatchID"]);
                                    decimal bQty = Convert.ToDecimal(bRow["Quantity"]);
                                    decimal toDeduct = Math.Min(remainingQty, bQty);
                                    if (toDeduct > 0)
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                            DbHelper.P("@q", toDeduct), DbHelper.P("@bid", bId));
                                        remainingQty -= toDeduct;
                                        if (remainingQty <= 0) break;
                                    }
                                }
                                if (remainingQty > 0)
                                {
                                    var oldestBatchId = DbHelper.ScalarTrans(trans, "SELECT TOP 1 BatchID FROM ProductBatches WHERE ProductID = @pid AND WarehouseID = @wid ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", warehouseID));
                                    if (oldestBatchId != null && oldestBatchId != DBNull.Value)
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                            DbHelper.P("@q", remainingQty), DbHelper.P("@bid", oldestBatchId));
                                    }
                                    else
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "INSERT INTO ProductBatches (ProductID, WarehouseID, Quantity, ExpiryDate) VALUES (@pid, @wid, -@q, @exp)",
                                            DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", warehouseID), DbHelper.P("@q", remainingQty), DbHelper.P("@exp", DateTime.Today.AddDays(30)));
                                    }
                                }
                            }
                        }

                        // Update stock
                        decimal baseQty2 = item.Qty * item.Factor;
                        DbHelper.ExecuteTrans(trans,
                            @"IF EXISTS (SELECT 1 FROM ProductStock WHERE ProductID=@pid AND WarehouseID=@wid)
                              UPDATE ProductStock SET Quantity = Quantity - @q, LastUpdated=GETDATE() WHERE ProductID=@pid AND WarehouseID=@wid
                              ELSE INSERT INTO ProductStock (ProductID,WarehouseID,Quantity) VALUES (@pid,@wid,-@q)",
                            DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", warehouseID), DbHelper.P("@q", baseQty2));
                    }

                    // 3. CashBox entry (نقدية الدرج الفعلي والفيزا بحسابها المحدد)
                    if (cashPaidVal > 0)
                    {
                        int defaultSafe = Session.GetDefaultSafeID();
                        DbHelper.ExecuteInsertTrans(trans,
                            "INSERT INTO CashBox (TransDate,TransType,Notes,AmountIn,AmountOut,RefID,CreatedBy,AccountID) VALUES (GETDATE(),'Sale',@desc,@amt,0,@ref,@emp,@aid)",
                            DbHelper.P("@desc", $"فاتورة POS #{saleCode} (نقدية درج)"),
                            DbHelper.P("@amt", cashPaidVal),
                            DbHelper.P("@ref", saleID),
                            DbHelper.P("@emp", Session.EmpID),
                            DbHelper.P("@aid", defaultSafe > 0 ? defaultSafe : 1));
                    }

                    if (visaPaidVal > 0 && _selectedVisaAccountID.HasValue)
                    {
                        DbHelper.ExecuteInsertTrans(trans,
                            "INSERT INTO CashBox (TransDate,TransType,Notes,AmountIn,AmountOut,RefID,CreatedBy,AccountID) VALUES (GETDATE(),'Sale',@desc,@amt,0,@ref,@emp,@aid)",
                            DbHelper.P("@desc", $"فاتورة POS #{saleCode} (سداد فيزا: {_selectedVisaAccountName})"),
                            DbHelper.P("@amt", visaPaidVal),
                            DbHelper.P("@ref", saleID),
                            DbHelper.P("@emp", Session.EmpID),
                            DbHelper.P("@aid", _selectedVisaAccountID.Value));
                    }

                    // Client ledger statement entries (كشف حساب العميل)
                    if (clientID > 0)
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO ClientTransactions (ClientID, TransDate, TransType, Debit, RefID, Notes, CreatedBy) VALUES (@cid, GETDATE(), 'Sale', @amt, @ref, @notes, @by)",
                            DbHelper.P("@cid", clientID),
                            DbHelper.P("@amt", total),
                            DbHelper.P("@ref", saleID),
                            DbHelper.P("@notes", $"فاتورة POS #{saleCode} [{_selectedSaleType}]"),
                            DbHelper.P("@by", Session.EmpID));

                        if (cashPaidVal > 0)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions (ClientID, TransDate, TransType, Credit, RefID, Notes, CreatedBy) VALUES (@cid, GETDATE(), 'Payment', @amt, @ref, @notes, @by)",
                                DbHelper.P("@cid", clientID),
                                DbHelper.P("@amt", cashPaidVal),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@notes", $"سداد نقدي فاتورة POS #{saleCode}"),
                                DbHelper.P("@by", Session.EmpID));
                        }

                        if (visaPaidVal > 0)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions (ClientID, TransDate, TransType, Credit, RefID, Notes, CreatedBy) VALUES (@cid, GETDATE(), 'Payment', @amt, @ref, @notes, @by)",
                                DbHelper.P("@cid", clientID),
                                DbHelper.P("@amt", visaPaidVal),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@notes", $"سداد فيزا فاتورة POS #{saleCode}"),
                                DbHelper.P("@by", Session.EmpID));
                        }
                    }

                    // 4. Loyalty points
                    if (AppConfig.LoyaltyEnabled && clientID > 0)
                    {
                        // Earn points
                        decimal earnedPoints = Math.Floor((total + loyaltyDiscount) / AppConfig.LoyaltyPointsPerCurrency);
                        if (earnedPoints > 0)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE Clients SET LoyaltyPoints = ISNULL(LoyaltyPoints,0) + @p, TotalPointsEarned = ISNULL(TotalPointsEarned,0) + @p WHERE ClientID=@cid",
                                DbHelper.P("@p", earnedPoints), DbHelper.P("@cid", clientID));
                            DbHelper.ExecuteInsertTrans(trans,
                                "INSERT INTO LoyaltyTransactions (ClientID,TransType,Points,RefSaleID,Notes,CreatedBy) VALUES (@cid,'Earn',@p,@sid,@n,@emp)",
                                DbHelper.P("@cid", clientID), DbHelper.P("@p", earnedPoints),
                                DbHelper.P("@sid", saleID), DbHelper.P("@n", $"كسب {earnedPoints:N0} نقطة من فاتورة POS"),
                                DbHelper.P("@emp", Session.EmpID));
                        }

                        // Redeem points
                        if (pointsToRedeem > 0)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE Clients SET LoyaltyPoints = ISNULL(LoyaltyPoints,0) - @p WHERE ClientID=@cid",
                                DbHelper.P("@p", pointsToRedeem), DbHelper.P("@cid", clientID));
                            DbHelper.ExecuteInsertTrans(trans,
                                "INSERT INTO LoyaltyTransactions (ClientID,TransType,Points,RefSaleID,Notes,CreatedBy) VALUES (@cid,'Redeem',@p,@sid,@n,@emp)",
                                DbHelper.P("@cid", clientID), DbHelper.P("@p", pointsToRedeem),
                                DbHelper.P("@sid", saleID), DbHelper.P("@n", $"استرداد {pointsToRedeem:N0} نقطة = خصم {loyaltyDiscount:N2} ج"),
                                DbHelper.P("@emp", Session.EmpID));
                        }
                    }

                    _lastSaleID = saleID;
                });

                // زيادة عداد استخدام بون الخصم لو طبّق
                if (_appliedVoucherID > 0 && _voucherDiscount > 0)
                {
                    try { DiscountVouchersDAL.IncrementUsage(_appliedVoucherID); } catch { }
                }

                // فتح درج النقدية تلقائياً عند السداد النقدي أو المختلط
                if (_selectedSaleType == "Cash" || (_selectedSaleType == "Mixed" && cashPaidVal > 0))
                {
                    try { RawPrinterHelper.OpenCashDrawer(); } catch { }
                }

                // طباعة تلقائية بعد الدفع — حسب إعداد وضع طباعة الرسيت
                string receiptMode = AppConfig.POSReceiptMode; // Always | Ask | Never
                if (receiptMode == "Always")
                {
                    PrintReceipt(_lastSaleID, askFirst: false);
                }
                else if (receiptMode == "Ask")
                {
                    PrintReceipt(_lastSaleID, askFirst: true);
                }
                // لو "Never" — لا يتم طباعة رسيت خالص

                if (AppConfig.IsRestaurant)
                {
                    try { new FrmKitchenPrint(_lastSaleID); } catch { }
                }

                try
                {
                    List<int> soldPids = _items.ConvertAll(x => x.ProductID);
                    // تسجيل النواقص آلياً في الخلفية عند حد الطلب أو نفاد المخزون دون إظهار نوافذ منبثقة مربكة للكاشير
                    ShortageDAL.ProcessStockChangesAfterSale(soldPids);
                }
                catch { }

                if (_activeDraftID > 0)
                {
                    DraftManager.MarkRecovered(_activeDraftID);
                    DraftManager.DeleteDraftByID(_activeDraftID);
                }
                if (!string.IsNullOrEmpty(_activeDraftKey))
                {
                    DraftManager.DeleteDraft(_activeDraftKey);
                }
                DraftManager.DeleteDraft($"POS_User_{Session.EmpID}");
                _activeDraftID = 0;
                _activeDraftKey = null;

                NewInvoice();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPOS.BtnPay_Click", ex);
                MessageBox.Show("❌ فشل حفظ الفاتورة:\n" + ex.Message, "خطأ في الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isSaving = false;
                this.BeginInvoke(new Action(() =>
                {
                    if (txtBarcode != null && !this.IsDisposed)
                    {
                        this.ActiveControl = txtBarcode;
                        txtBarcode.Focus();
                        txtBarcode.SelectAll();
                    }
                }));
            }
        }

        /// <summary>
        /// يتحقق من كود بون الخصم ويطبّق الخصم على الفاتورة الحالية
        /// </summary>
        private void ApplyVoucherCode()
        {
            string code = txtVoucherCode?.Text?.Trim().ToUpper();
            if (string.IsNullOrEmpty(code)) return;

            var dto = DiscountVouchersDAL.GetByCode(code);
            if (dto == null)
            {
                lblVoucherDiscount.Text = "❌ كود الخصم غير موجود";
                lblVoucherDiscount.ForeColor = Color.FromArgb(239, 68, 68);
                _voucherDiscount = 0m;
                _appliedVoucherID = 0;
                RefreshGrid();
                return;
            }

            string reason;
            if (!dto.IsValid(out reason))
            {
                lblVoucherDiscount.Text = "⚠️ " + reason;
                lblVoucherDiscount.ForeColor = Color.FromArgb(251, 146, 60);
                _voucherDiscount = 0m;
                _appliedVoucherID = 0;
                RefreshGrid();
                return;
            }

            // حساب الإجمالي قبل الخصم
            decimal total = 0m;
            foreach (var item in _items) total += item.Total;

            decimal disc = dto.CalculateDiscount(total);
            _voucherDiscount = disc;
            _appliedVoucherID = dto.VoucherID;

            string discLabel = dto.DiscountType == "Percent"
                ? $"🎟️ خصم {dto.DiscountValue:G29}%: -{disc:N2} ج"
                : $"🎟️ خصم: -{disc:N2} ج";

            lblVoucherDiscount.Text = discLabel;
            lblVoucherDiscount.ForeColor = Color.FromArgb(52, 211, 153);

            RefreshGrid();
        }

        private void NewInvoice()
        {
            _activeDraftKey = null;
            _activeDraftID = 0;
            _isSaving = false;
            _items.Clear();
            _loadedDraftSaleID = 0;
            _selectedSaleType = "Cash";
            _selectedVisaAccountID = null;
            _selectedVisaAccountName = "";
            // إعادة ضبط بون الخصم
            _voucherDiscount = 0m;
            _appliedVoucherID = 0;
            if (txtVoucherCode != null) txtVoucherCode.Clear();
            if (lblVoucherDiscount != null) lblVoucherDiscount.Text = "";


            UpdatePaymentTypeButtons();
            if (cboClient != null)
            {
                cboClient.Tag = null;
                LoadClients();
            }
            RefreshGrid();
            if (txtPaid != null) txtPaid.Text = "0";
            if (txtVisaPaid != null) txtVisaPaid.Text = "0";
            if (txtBarcode != null) txtBarcode.Clear();
            if (chkRedeemPoints != null) chkRedeemPoints.Checked = false;
            if (AppConfig.IsRestaurant)
            {
                if (txtTableNum != null) txtTableNum.Clear();
                if (rbDineIn != null) rbDineIn.Checked = true;
                if (cboDeliveryDriver != null && cboDeliveryDriver.Items.Count > 0) cboDeliveryDriver.SelectedIndex = 0;
            }
            if (cboWarehouse != null && cboWarehouse.Items.Count > 0)
            {
                int defWhId = Session.GetDefaultWarehouseID();
                for (int i = 0; i < cboWarehouse.Items.Count; i++)
                {
                    if (cboWarehouse.Items[i] is ComboItem ci && ci.ID == defWhId)
                    {
                        cboWarehouse.SelectedIndex = i;
                        break;
                    }
                }
            }
            if (cboPriceTier != null && cboPriceTier.Items.Count > 0)
            {
                string defTier = Session.GetDefaultPriceTier();
                for (int i = 0; i < cboPriceTier.Items.Count; i++)
                {
                    if (string.Equals(cboPriceTier.Items[i]?.ToString(), defTier, StringComparison.OrdinalIgnoreCase))
                    {
                        cboPriceTier.SelectedIndex = i;
                        break;
                    }
                }
            }
            FilterQuickItems(_currentQuickCategoryId);
            this.BeginInvoke(new Action(() =>
            {
                if (txtBarcode != null && !this.IsDisposed)
                {
                    this.ActiveControl = txtBarcode;
                    txtBarcode.Focus();
                    txtBarcode.SelectAll();
                }
            }));
        }

        private void OpenIncompletePOSDialog()
        {
            using (var frm = new FrmIncompleteInvoices("POS"))
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.IsRestored && !string.IsNullOrEmpty(frm.SelectedDraftJson))
                {
                    RestorePOSFromDraft(frm.SelectedDraftJson, frm.SelectedDraftID, frm.SelectedDraftKey);
                }
            }
        }

        private void RestorePOSFromDraft(string json, int draftId, string draftKey = null)
        {
            try
            {
                var data = DraftManager.Deserialize<SaleDraftData>(json);
                if (data == null) return;

                if (_items.Count > 0)
                {
                    if (MessageBox.Show("توجد فاتورة حالية في الكاشير، هل تريد استبدالها بالفاتورة المسترجعة؟", "تأكيد الاسترجاع", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                }

                NewInvoice();

                _activeDraftID = draftId;
                _activeDraftKey = !string.IsNullOrEmpty(draftKey) ? draftKey : $"POS_User_{Session.EmpID}_{draftId}";

                if (data.ClientID > 0)
                {
                    for (int i = 0; i < cboClient.Items.Count; i++)
                    {
                        if (cboClient.Items[i] is ComboItem ci && ci.ID == data.ClientID)
                        {
                            cboClient.SelectedIndex = i;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(data.InvoiceType))
                {
                    SetPaymentType(data.InvoiceType);
                }

                _items.Clear();
                if (data.Items != null)
                {
                    foreach (var itm in data.Items)
                    {
                        _items.Add(new POSItem
                        {
                            ProductID = itm.ProductID,
                            Code = itm.ProductCode,
                            Name = itm.ProductName,
                            UnitName = itm.Unit,
                            Qty = itm.Quantity,
                            Price = itm.UnitPrice,
                            DiscountAmt = itm.LineDiscount,
                            Factor = itm.Factor > 0 ? itm.Factor : 1.0m,
                            BatchID = itm.BatchID,
                            ExpiryDate = !string.IsNullOrEmpty(itm.ExpiryDate) && DateTime.TryParse(itm.ExpiryDate, out DateTime exp) ? (DateTime?)exp : null,
                            IMEI = itm.IMEI
                        });
                    }
                }

                RefreshGrid();
                MessageBox.Show($"✅ تم استرجاع فاتورة الكاشير بنجاح ({_items.Count} صنف)!\nستظل محفوظة في قائمة الفواتير غير المكتملة لحين حفظها نهائياً أو حذفها.", "استرجاع الفاتورة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء استرجاع فاتورة الكاشير:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AutoSavePOSDraft()
        {
            if (_items == null || _items.Count == 0 || _isSaving) return;

            try
            {
                int clientId = 0;
                string clientName = "عميل نقدي";
                if (cboClient != null && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    clientId = ci.ID;
                    clientName = ci.Text;
                }

                decimal total = 0;
                foreach (var itm in _items) total += itm.Total;

                var data = new SaleDraftData
                {
                    ClientID = clientId,
                    ClientName = clientName,
                    InvoiceType = _selectedSaleType,
                    DiscountVal = 0,
                    Notes = "مسودة POS تم حفظها تلقائياً",
                    Items = new List<SaleDraftItem>()
                };

                foreach (var itm in _items)
                {
                    data.Items.Add(new SaleDraftItem
                    {
                        ProductID = itm.ProductID,
                        ProductCode = itm.Code,
                        ProductName = itm.Name,
                        Unit = itm.UnitName,
                        Quantity = itm.Qty,
                        UnitPrice = itm.Price,
                        LineDiscount = itm.DiscountAmt,
                        Factor = itm.Factor,
                        LineTotal = itm.Total,
                        BatchID = itm.BatchID,
                        ExpiryDate = itm.ExpiryDate?.ToString("yyyy-MM-dd"),
                        IMEI = itm.IMEI
                    });
                }

                if (string.IsNullOrEmpty(_activeDraftKey))
                {
                    _activeDraftKey = $"POS_User_{Session.EmpID}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                }
                DraftManager.SaveDraft("POS", _activeDraftKey, Session.EmpID, clientId, clientName, _selectedSaleType, total, _items.Count, data);
            }
            catch { }
        }

        // ── طباعة الإيصال ─────────────────────────────────────
        private void PrintReceipt(int saleID, bool askFirst = false)
        {
            try
            {
                if (askFirst)
                {
                    FrmPrintChoiceDialog.PromptAndPrintSale(this, saleID);
                }
                else
                {
                    new FrmPrintSale(saleID, "Receipt", false);
                }
            }
            catch (Exception ex) { AppLogger.Error("FrmPOS.PrintReceipt", ex); }
            finally
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (txtBarcode != null && !this.IsDisposed)
                    {
                        this.ActiveControl = txtBarcode;
                        txtBarcode.Focus();
                        txtBarcode.SelectAll();
                    }
                }));
            }
        }

        // ── بحث أصناف ────────────────────────────────────────
        private void OpenProductSearch()
        {
            if (!Session.CanAccess("ProductSearch"))
            {
                MessageBox.Show("عفواً، ليس لديك صلاحية استخدام شاشة بحث الأصناف السريعة.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // تظل الشاشة تُعاد فتحها بعد كل اختيار
                // حتى يضغط المستخدم إلغاء أو يُغلق الشاشة
                _searchSessionActive = true;
                string lastSearchText = "";
                while (true)
                {
                    int posClientID = (cboClient != null && cboClient.SelectedItem is ComboItem ciClient) ? ciClient.ID : 0;
                    using var frm = new FrmProductSearch(warehouseID: GetSelectedWarehouseID(), isPurchaseMode: false, defaultShowZeroStock: false, clientID: posClientID > 0 ? posClientID : (int?)null, initialSearchText: lastSearchText);
                    frm.ShowDialog();

                    if (frm.DialogResult == DialogResult.OK && frm.SelectedProductID > 0)
                    {
                        lastSearchText = frm.SearchText;
                        var dt = DbHelper.Query(@"
                            SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                                   p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                                   p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice, p.Unit2Factor,
                                   p.Unit3Factor, p.DefaultSaleUnit,
                                   p.WholesalePrice, p.SemiWholesalePrice, p.MinStockLimit, COALESCE(p.IsService, 0) AS IsService,
                                   COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                            FROM Products p 
                            WHERE p.ProductID = @id", DbHelper.P("@id", frm.SelectedProductID));
                        if (dt.Rows.Count > 0)
                        {
                            var row = dt.Rows[0];
                            decimal factor = 1m;
                            if (!string.IsNullOrEmpty(frm.SelectedUnitName))
                            {
                                if (row["Unit2Name"] != DBNull.Value && frm.SelectedUnitName == row["Unit2Name"].ToString())
                                {
                                    if (row["Unit2Factor"] != DBNull.Value) factor = Convert.ToDecimal(row["Unit2Factor"]);
                                }
                                else if (row["Unit1Name"] != DBNull.Value && frm.SelectedUnitName == row["Unit1Name"].ToString())
                                {
                                    factor = 1m;
                                }
                            }
                            decimal posPrice = frm.SelectedSalePrice > 0 ? frm.SelectedSalePrice : frm.SelectedPrice;
                            decimal posQty = frm.SelectedQuantity > 0 ? frm.SelectedQuantity : 1m;
                            decimal posDisc = frm.SelectedDiscount;
                            decimal discAmt = 0m;
                            if (posDisc > 0)
                            {
                                if (posDisc <= 100m)
                                    discAmt = Math.Round((posQty * posPrice) * posDisc / 100m, 2);
                                else
                                    discAmt = posDisc;
                            }
                            AddItemFromRow(row, posQty, frm.SelectedUnitName, factor, posPrice, frm.SelectedBatchID, frm.SelectedExpiryDate, discAmt);
                        }
                        // فتح الشاشة مرة أخرى لاختيار صنف تاني
                        continue;
                    }
                    else
                    {
                        // المستخدم ضغط إلغاء أو أغلق الشاشة → نخرج من الحلقة
                        break;
                    }
                }
            }
            catch { }
            finally
            {
                _searchSessionActive = false;
                // إرجاع الفوكس لخانة الباركود
                this.BeginInvoke((Action)(() => txtBarcode.Focus()));
            }
        }

        // ── Quick Items ──────────────────────────────────────
        private void LoadQuickItems()
        {
            FilterQuickItems(null);
        }

        private void FilterQuickItems(int? categoryID)
        {
            _currentQuickCategoryId = categoryID;
            flowQuickItems.Controls.Clear();

            bool inStockOnly = chkQuickInStockOnly == null || chkQuickInStockOnly.Checked;
            int whId = GetSelectedWarehouseID();
            var stockMap = InventoryDAL.GetStockSummary(whId);

            string query = @"
                SELECT p.ProductID, p.ProductCode, p.ProductName, p.SalePrice, p.WholesalePrice, p.SemiWholesalePrice,
                       COALESCE(p.IsService, 0) AS IsService
                FROM Products p WITH (NOLOCK)
                WHERE p.IsActive = 1 AND ISNULL(p.IsQuickItem, 0) = 1";

            var pList = new List<System.Data.SqlClient.SqlParameter>();

            if (categoryID.HasValue)
            {
                query += " AND p.CategoryID = @catId";
                pList.Add(DbHelper.P("@catId", categoryID.Value));
            }

            query += " ORDER BY p.ProductName";
            DataTable dt = DbHelper.Query(query, pList.ToArray());

            var colors = new Color[] {
                Color.FromArgb(13, 110, 253),  // Royal Blue
                Color.FromArgb(253, 126, 20),  // Vibrant Orange
                Color.FromArgb(25, 135, 84),   // Green
                Color.FromArgb(111, 66, 193),  // Purple
                Color.FromArgb(23, 162, 184),  // Teal
                Color.FromArgb(220, 53, 69)    // Red
            };
            int colorIndex = 0;
            string curTier = GetSelectedPriceTier();

            foreach (DataRow row in dt.Rows)
            {
                int pid = Convert.ToInt32(row["ProductID"]);
                string name = row["ProductName"].ToString();
                bool isService = Convert.ToBoolean(row["IsService"]);
                decimal stock = stockMap.TryGetValue(pid, out var s) ? s : 0m;

                if (inStockOnly && stock <= 0 && !isService)
                {
                    continue; // إخفاء الأصناف غير المتوفرة في المخزن المحدد
                }

                decimal price = Convert.ToDecimal(row["SalePrice"]);
                if (curTier == "جملة" && row.Table.Columns.Contains("WholesalePrice") && row["WholesalePrice"] != DBNull.Value && Convert.ToDecimal(row["WholesalePrice"]) > 0)
                    price = Convert.ToDecimal(row["WholesalePrice"]);
                else if (curTier == "نصف جملة" && row.Table.Columns.Contains("SemiWholesalePrice") && row["SemiWholesalePrice"] != DBNull.Value && Convert.ToDecimal(row["SemiWholesalePrice"]) > 0)
                    price = Convert.ToDecimal(row["SemiWholesalePrice"]);

                Color btnColor = colors[colorIndex++ % colors.Length];
                if (stock <= 0 && !isService)
                {
                    btnColor = Color.FromArgb(108, 117, 125); // رمادي للأصناف غير المتوفرة
                }

                string priceText = price > 0 ? $"{price:N2} ج" : "⚠️ بدون سعر";
                string stockText = isService ? "خدمة" : (stock > 0 ? $"رصيد: {stock:G29}" : "❌ نفد");

                float nameFontSize = name.Length > 24 ? 9.5f : (name.Length > 14 ? 10.5f : 11.5f);
                string pText = priceText;
                string sText = stockText;
                string pName = name;
                bool isOut = (stock <= 0 && !isService);

                var btn = new Button
                {
                    Text = "", // يتم الرسم يدوياً عبر حدث Paint للحصول على خط كبير وواضح للصنف وإبراز السعر
                    Size = new Size(122, 95),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = btnColor,
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(4),
                    Tag = pid
                };
                btn.FlatAppearance.BorderSize = isOut ? 2 : 0;
                if (isOut)
                {
                    btn.FlatAppearance.BorderColor = Color.FromArgb(220, 53, 69);
                }

                btn.Paint += (s, pe) =>
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    var rect = btn.ClientRectangle;

                    // 1. رسم اسم الصنف في النصف العلوي بخط كبير وواضح مع التفاف الكلمات
                    var nameRect = new Rectangle(3, 4, rect.Width - 6, (int)(rect.Height * 0.49));
                    using (var fontName = new Font("Segoe UI", nameFontSize, FontStyle.Bold))
                    using (var sfName = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisWord,
                        FormatFlags = StringFormatFlags.NoClip
                    })
                    using (var brushName = new SolidBrush(Color.White))
                    {
                        g.DrawString(pName, fontName, brushName, nameRect, sfName);
                    }

                    // فاصل خفيف
                    using (var pen = new Pen(Color.FromArgb(50, 255, 255, 255), 1f))
                    {
                        int lineY = (int)(rect.Height * 0.53);
                        g.DrawLine(pen, 10, lineY, rect.Width - 10, lineY);
                    }

                    // 2. رسم السعر في خط بارز بلون ذهبي مشرق
                    var priceRect = new Rectangle(2, (int)(rect.Height * 0.55), rect.Width - 4, (int)(rect.Height * 0.23));
                    using (var fontPrice = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    using (var sfPrice = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var brushPrice = new SolidBrush(Color.FromArgb(255, 245, 160)))
                    {
                        g.DrawString(pText, fontPrice, brushPrice, priceRect, sfPrice);
                    }

                    // 3. رسم الرصيد في الأسفل
                    var stockRect = new Rectangle(2, (int)(rect.Height * 0.77), rect.Width - 4, (int)(rect.Height * 0.21));
                    using (var fontStock = new Font("Segoe UI", 8.25f, isOut ? FontStyle.Bold : FontStyle.Regular))
                    using (var sfStock = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var brushStock = new SolidBrush(isOut ? Color.FromArgb(255, 180, 180) : Color.FromArgb(235, 245, 255)))
                    {
                        g.DrawString($"({sText})", fontStock, brushStock, stockRect, sfStock);
                    }
                };

                btn.Click += QuickItemBtn_Click;
                flowQuickItems.Controls.Add(btn);
            }
        }

        private void QuickItemBtn_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            int pid = (int)btn.Tag;
            int wid = GetSelectedWarehouseID();
            decimal stock = InventoryDAL.GetProductStock(pid, wid);

            var dtP = DbHelper.Query("SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice, p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice, p.Unit2Factor, p.Unit3Factor, p.DefaultSaleUnit, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays, p.WholesalePrice, p.SemiWholesalePrice, p.MinStockLimit, COALESCE(p.IsService, 0) AS IsService FROM Products p WHERE p.ProductID=@id", DbHelper.P("@id", pid));
            if (dtP.Rows.Count > 0)
            {
                var row = dtP.Rows[0];
                bool isService = Convert.ToBoolean(row["IsService"]);
                if (stock <= 0 && !isService)
                {
                    string whName = cboWarehouse != null ? cboWarehouse.Text : "المخزن المحدد";
                    MessageBox.Show($"❌ عجز: الصنف '{row["ProductName"]}' ليس لديه رصيد متاح في {whName} حالياً (الرصيد: 0)!\nالبيع بالسالب غير مسموح.", "عجز الرصيد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int? bid = null;
                DateTime? exp = null;
                if (row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]))
                {
                    var batches = DbHelper.Query("SELECT BatchID, ExpiryDate FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", Convert.ToInt32(row["ProductID"])), DbHelper.P("@wid", wid));
                    if (batches.Rows.Count > 0)
                    {
                        bid = Convert.ToInt32(batches.Rows[0]["BatchID"]);
                        exp = batches.Rows[0]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[0]["ExpiryDate"]) : (DateTime?)null;
                    }
                    else
                    {
                        MessageBox.Show("❌ عجز: لا توجد أي تشغيلات (صلاحيات) متوفرة لهذا الصنف في هذا المخزن حالياً!", "عجز الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                AddItemFromRow(row, 1, null, 1m, 0, bid, exp);
            }
        }

        private void LoadCategories()
        {
            flowCategories.Controls.Clear();

            var dt = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories WHERE IsActive=1 ORDER BY CategoryName");
            if (dt.Rows.Count == 0)
            {
                flowCategories.Visible = false;
                return;
            }
            flowCategories.Visible = true;

            Font catFont = new Font("Segoe UI", 9f, FontStyle.Bold);

            var btnAll = new Button
            {
                Text = "الكل",
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = catFont,
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 4, 3, 4)
            };
            btnAll.FlatAppearance.BorderSize = 0;
            btnAll.Click += (s, e) => {
                FilterQuickItems(null);
                HighlightCategoryButton((Button)s);
            };
            flowCategories.Controls.Add(btnAll);

            foreach (DataRow row in dt.Rows)
            {
                int catId = Convert.ToInt32(row["CategoryID"]);
                string catName = row["CategoryName"].ToString();

                int btnWidth = Math.Max(75, TextRenderer.MeasureText(catName, catFont).Width + 18);

                var btnCat = new Button
                {
                    Text = catName,
                    Size = new Size(btnWidth, 28),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 70, 85),
                    ForeColor = Color.White,
                    Font = catFont,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3, 4, 3, 4),
                    Tag = catId
                };
                btnCat.FlatAppearance.BorderSize = 0;
                btnCat.Click += (s, e) => {
                    FilterQuickItems((int)((Button)s).Tag);
                    HighlightCategoryButton((Button)s);
                };
                flowCategories.Controls.Add(btnCat);
            }
        }

        private void HighlightCategoryButton(Button selected)
        {
            foreach (Control ctrl in flowCategories.Controls)
            {
                if (ctrl is Button btn)
                {
                    if (btn == selected)
                    {
                        btn.BackColor = Theme.Primary;
                        btn.ForeColor = Color.White;
                    }
                    else
                    {
                        btn.BackColor = Color.FromArgb(60, 70, 85);
                        btn.ForeColor = Color.White;
                    }
                }
            }
        }

        private void LoadDeliveryDrivers()
        {
            if (!AppConfig.IsRestaurant) return;
            cboDeliveryDriver.BeginUpdate();
            cboDeliveryDriver.Items.Clear();

            List<ComboItem> driverItems = new List<ComboItem>();
            driverItems.Add(new ComboItem(0, "-- اختر طيار --"));

            try
            {
                DataTable drivers = EmployeeDAL.GetDrivers();
                foreach (DataRow row in drivers.Rows)
                {
                    driverItems.Add(new ComboItem((int)row["EmpID"], row["EmpName"].ToString()));
                }
            }
            catch { }

            cboDeliveryDriver.Items.AddRange(driverItems.ToArray());
            cboDeliveryDriver.DisplayMember = "Text";
            cboDeliveryDriver.SelectedIndex = 0;
            cboDeliveryDriver.EndUpdate();
        }

        private void LoadClients()
        {
            cboClient.BeginUpdate();
            cboClient.Items.Clear();
            List<ComboItem> clientItems = new List<ComboItem>();
            clientItems.Add(new ComboItem(0, "-- بدون عميل --"));
            var dt = DbHelper.Query("SELECT ClientID, ClientCode, ClientName, Phone, Phone2 FROM Clients WHERE IsActive=1 ORDER BY ClientName");
            foreach (DataRow row in dt.Rows)
            {
                string code = row["ClientCode"]?.ToString() ?? "";
                string name = row["ClientName"]?.ToString() ?? "";
                string phone = row["Phone"]?.ToString() ?? "";
                string phone2 = row["Phone2"]?.ToString() ?? "";
                string combinedPhone = string.IsNullOrEmpty(phone2) ? phone : $"{phone} / {phone2}";
                clientItems.Add(new ComboItem(Convert.ToInt32(row["ClientID"]), name, combinedPhone, code));
            }
            cboClient.Items.AddRange(clientItems.ToArray());
            cboClient.Tag = clientItems;
            cboClient.SelectedIndex = 0;
            cboClient.EndUpdate();
            SetupSearchableCombo(cboClient);
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
                        if ((item2.Text != null && item2.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrEmpty(item2.Code) && item2.Code.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrEmpty(item2.Phone) && item2.Phone.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
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

        private void CboClient_Changed(object sender, EventArgs e)
        {
            if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                if (AppConfig.LoyaltyEnabled)
                {
                    var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", ci.ID));
                    decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                    lblClientPoints.Text = $"🎁 {points:N0} نقطة";
                }
                else { lblClientPoints.Text = ""; }

                // تطبيق شريحة السعر الافتراضية للعميل
                try
                {
                    var cr = DbHelper.Query("SELECT DefaultPriceTier FROM Clients WHERE ClientID=@id", DbHelper.P("@id", ci.ID));
                    if (cr.Rows.Count > 0 && cr.Rows[0]["DefaultPriceTier"] != DBNull.Value && !string.IsNullOrWhiteSpace(cr.Rows[0]["DefaultPriceTier"].ToString()))
                    {
                        string clientTier = cr.Rows[0]["DefaultPriceTier"].ToString().Trim();
                        if (cboPriceTier != null && (Session.IsAdmin || string.Equals(clientTier, Session.GetDefaultPriceTier(), StringComparison.OrdinalIgnoreCase)))
                            cboPriceTier.SelectedItem = clientTier;
                    }
                }
                catch { }
            }
            else
            {
                lblClientPoints.Text = "";
                if (cboPriceTier != null) cboPriceTier.SelectedItem = Session.GetDefaultPriceTier();
            }
            RefreshGrid();
        }

        public int GetSelectedWarehouseID()
        {
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
                return ci.ID;
            return Session.GetDefaultWarehouseID();
        }

        public string GetSelectedPriceTier()
        {
            if (cboPriceTier != null && !string.IsNullOrWhiteSpace(cboPriceTier.Text))
                return cboPriceTier.Text.Trim();
            return Session.GetDefaultPriceTier();
        }

        private decimal GetProductPriceByTier(int productID, string tier)
        {
            try
            {
                var dt = DbHelper.Query("SELECT SalePrice, WholesalePrice, SemiWholesalePrice FROM Products WHERE ProductID=@id", DbHelper.P("@id", productID));
                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    if (tier == "جملة" && r["WholesalePrice"] != DBNull.Value && Convert.ToDecimal(r["WholesalePrice"]) > 0)
                        return Convert.ToDecimal(r["WholesalePrice"]);
                    if (tier == "نصف جملة" && r["SemiWholesalePrice"] != DBNull.Value && Convert.ToDecimal(r["SemiWholesalePrice"]) > 0)
                        return Convert.ToDecimal(r["SemiWholesalePrice"]);
                    if (r["SalePrice"] != DBNull.Value)
                        return Convert.ToDecimal(r["SalePrice"]);
                }
            }
            catch { }
            return 0;
        }

        private void LoadStockCache()
        {
            try
            {
                _stockCache.Clear();
                int wid = GetSelectedWarehouseID();
                _stockCache = InventoryDAL.GetStockSummary(wid);
            }
            catch { }
        }

        private class POSItem
        {
            public int ProductID; public string Code, Name, Unit, UnitName;
            public decimal Qty, Price, Cost, Total, Factor;
            public decimal DiscountAmt;
            public bool HasExpiry;
            public int? DefaultExpiryDays;
            public DateTime? ExpiryDate;
            public int? BatchID;
            public string KitchenNotes = "";
            public string IMEI = "";

            // Unit metadata & stock
            public string BaseUnitName;
            public string Unit1Name;
            public string Unit2Name;
            public decimal Unit1SalePrice;
            public decimal Unit2SalePrice;
            public decimal Unit1Cost;
            public decimal Unit2Cost;
            public decimal Unit2Factor;
            public decimal Unit3Factor;
            public decimal MajorPrice;
            public decimal MajorCost;
            public decimal MinStockLimit;
            public bool IsService;
            public decimal StockQty;
        }

        public class ComboItem
        {
            public int ID; public string Text; public string Phone; public string Code;
            public ComboItem(int id, string text, string phone = "", string code = "") { ID = id; Text = text; Phone = phone; Code = code; }
            public override string ToString() => Text;
        }

        private void SendWhatsAppReceipt(int saleID)
        {
            try
            {
                // 1. Query sale details and client phone
                var dtSale = DbHelper.Query(@"
                    SELECT s.SaleCode, s.TotalAmount, s.DiscountAmount, s.CashPaid, s.SaleDate,
                           c.ClientName, c.Phone
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE s.SaleID = @id", DbHelper.P("@id", saleID));

                if (dtSale.Rows.Count == 0) return;

                var row = dtSale.Rows[0];
                string saleCode = row["SaleCode"].ToString();
                decimal total = Convert.ToDecimal(row["TotalAmount"]);
                decimal discount = Convert.ToDecimal(row["DiscountAmount"]);
                decimal paid = Convert.ToDecimal(row["CashPaid"]);
                decimal remaining = total - paid;
                string clientName = row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "عميل نقدي";
                string phone = row["Phone"] != DBNull.Value ? row["Phone"].ToString() : "";

                // Query sale items
                var dtItems = DbHelper.Query(@"
                    SELECT p.ProductName, si.Quantity, si.UnitName, si.UnitPrice, si.TotalPrice
                    FROM SaleItems si
                    JOIN Products p ON si.ProductID = p.ProductID
                    WHERE si.SaleID = @id", DbHelper.P("@id", saleID));

                // 2. If phone is empty, prompt the user to enter it
                if (string.IsNullOrWhiteSpace(phone))
                {
                    string inputVal = "";
                    if (ShowPhoneInputDialog("إرسال عبر واتساب", "يرجى إدخال رقم هاتف العميل:", ref inputVal))
                    {
                        phone = inputVal;
                    }
                    else
                    {
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(phone)) return;

                // Normalize phone number (remove spaces, plus sign, ensure country code)
                phone = phone.Replace(" ", "").Replace("+", "").Trim();
                if (phone.StartsWith("0"))
                {
                    if (phone.Length == 11 && phone.StartsWith("01"))
                    {
                        phone = "2" + phone;
                    }
                    else if (phone.Length == 10 && phone.StartsWith("05"))
                    {
                        phone = "966" + phone.Substring(1);
                    }
                }

                // 3. Format message
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"📄 *فاتورة مبيعات رقم: {saleCode}*");
                sb.AppendLine($"📅 *التاريخ:* {Convert.ToDateTime(row["SaleDate"]):yyyy-MM-dd HH:mm}");
                sb.AppendLine($"👤 *العميل:* {clientName}");
                sb.AppendLine();
                sb.AppendLine("📋 *الأصناف:*");
                
                foreach (DataRow item in dtItems.Rows)
                {
                    string prodName = item["ProductName"].ToString();
                    decimal qty = Convert.ToDecimal(item["Quantity"]);
                    string unit = item["UnitName"] != DBNull.Value ? item["UnitName"].ToString() : "";
                    decimal price = Convert.ToDecimal(item["UnitPrice"]);
                    decimal itemTotal = Convert.ToDecimal(item["TotalPrice"]);
                    sb.AppendLine($"- {prodName} ({qty} {unit} × {price:N2}) = {itemTotal:N2} ج");
                }

                sb.AppendLine();
                sb.AppendLine($"💵 *الإجمالي:* {total:N2} ج");
                if (discount > 0) sb.AppendLine($"🎁 *الخصم:* {discount:N2} ج");
                sb.AppendLine($"💳 *المدفوع:* {paid:N2} ج");
                if (remaining > 0) sb.AppendLine($"⚠️ *المتبقي:* {remaining:N2} ج");
                
                sb.AppendLine();
                sb.AppendLine("شكراً لتعاملكم معنا! 🙏");

                string message = sb.ToString();

                // Open Universal WhatsApp Options Dialog (Text vs Image)
                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                    this,
                    phone,
                    message,
                    () => ReceiptImageGenerator.GenerateSaleReceiptImage(saleID),
                    "📱 إرسال فاتورة المبيعات عبر الواتساب");
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPOS.SendWhatsAppReceipt", ex);
                MessageBox.Show("فشل فتح واتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool ShowPhoneInputDialog(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "موافق";
            buttonCancel.Text = "إلغاء";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;
            
            form.RightToLeft = RightToLeft.Yes;
            form.RightToLeftLayout = true;
            form.Font = Theme.FontMain;
            form.BackColor = Theme.BgMain;
            label.ForeColor = Theme.TextMain;
            textBox.BackColor = Theme.BgInput;
            textBox.ForeColor = Theme.TextMain;

            var result = form.ShowDialog();
            value = textBox.Text;
            return result == DialogResult.OK;
        }

        private void FrmPOS_ProductSaved(object sender, EventArgs e)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        LoadCategories();
                        LoadQuickItems();
                    }
                    catch { }
                }));
            }
        }

        private void FrmPOS_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_items != null && _items.Count > 0)
            {
                var confirm = MessageBox.Show(
                    "توجد أصناف في الفاتورة الحالية لم يتم حفظها أو إتمام دفعها.\nهل أنت متأكد من إغلاق شاشة نقاط البيع والكاشير (POS)؟",
                    "تنبيه - فاتورة معلقة",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                if (confirm == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            FrmQuickAdd.ProductSaved -= FrmPOS_ProductSaved;
            if (Session.CanOrderColumns("POS"))
            {
                Session.SaveColumnOrder(dgItems, "POS");
            }
        }

        private void ShowColumnCustomizer()
        {
            if (!Session.PromptAdminPassword(this, "لتخصيص وترتيب أعمدة نقطة البيع POS"))
                return;

            var dlg = new Form
            {
                Text            = "⚙️ تخصيص أعمدة المبيعات السريعة",
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

            foreach (DataGridViewColumn col in dgItems.Columns)
            {
                clb.Items.Add(new ColEntry(col.Name, col.HeaderText), col.Visible);
            }

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
                int displayIndex = 0;
                var hiddenNames  = new List<string>();
                var orderedNames = new List<string>();

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

                SaveColumnSettings(orderedNames, hiddenNames);
            }
        }

        private void SaveColumnSettings(List<string> ordered = null, List<string> hidden = null)
        {
            try
            {
                if (ordered == null)
                {
                    ordered = new List<string>();
                    hidden = new List<string>();
                    foreach (DataGridViewColumn col in dgItems.Columns)
                    {
                        ordered.Add(col.Name);
                        if (!col.Visible) hidden.Add(col.Name);
                    }
                }
                Core.LicenseManager.WriteIniValue("POSGridColumns", "Order",  string.Join(",", ordered));
                Core.LicenseManager.WriteIniValue("POSGridColumns", "Hidden", string.Join(",", hidden));
            }
            catch { }
        }

        private void LoadColumnSettings()
        {
            try
            {
                string orderVal  = Core.LicenseManager.ReadIniValue("POSGridColumns", "Order",  "");
                string hiddenVal = Core.LicenseManager.ReadIniValue("POSGridColumns", "Hidden", "");

                var hidden  = new List<string>(string.IsNullOrEmpty(hiddenVal) ? new string[0] : hiddenVal.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));

                // Always ensure optional/extra columns default to hidden
                if (!hidden.Contains("LastClientPrice")) hidden.Add("LastClientPrice");
                if (!hidden.Contains("KitchenNotes")) hidden.Add("KitchenNotes");
                if (AppConfig.BusinessType != "Mobiles" && !hidden.Contains("IMEI")) hidden.Add("IMEI");

                if (string.IsNullOrWhiteSpace(orderVal))
                {
                    foreach (DataGridViewColumn col in dgItems.Columns)
                    {
                        if (hidden.Contains(col.Name)) col.Visible = false;
                    }
                    return;
                }

                var ordered = new List<string>(orderVal.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));

                if (!ordered.Contains("StockQty"))
                {
                    int nameIdx = ordered.IndexOf("Name");
                    if (nameIdx >= 0) ordered.Insert(nameIdx + 1, "StockQty");
                    else ordered.Add("StockQty");
                }
                if (!ordered.Contains("UnitName"))
                {
                    int stockIdx = ordered.IndexOf("StockQty");
                    if (stockIdx >= 0) ordered.Insert(stockIdx + 1, "UnitName");
                    else ordered.Add("UnitName");
                }

                foreach (DataGridViewColumn col in dgItems.Columns)
                {
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
            }
            catch { }
        }

        private void SuspendCurrentOrder()
        {
            if (_isSaving) return;
            if (_items.Count == 0) { MessageBox.Show("لا يوجد أصناف في الفاتورة لتعليقها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            _isSaving = true;
            int draftToDelete = _loadedDraftSaleID;
            _loadedDraftSaleID = 0;

            string orderType = null;
            string tableNum = null;
            int? selectedDriver = null;
            if (AppConfig.IsRestaurant)
            {
                orderType = rbDineIn.Checked ? "DineIn" : rbDelivery.Checked ? "Delivery" : "Takeaway";
                tableNum = rbDineIn.Checked ? txtTableNum.Text.Trim() : null;
                if (rbDelivery.Checked && cboDeliveryDriver.SelectedItem is ComboItem driverItem && driverItem.ID > 0)
                {
                    selectedDriver = driverItem.ID;
                }
            }
            int clientID = 0;
            if (cboClient.SelectedItem is ComboItem ci) clientID = ci.ID;

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    // If we are updating an existing draft, we can delete the old one first
                    if (draftToDelete > 0)
                    {
                        DbHelper.ExecuteTrans(trans, "DELETE FROM SaleItems WHERE SaleID=@id", DbHelper.P("@id", draftToDelete));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM Sales WHERE SaleID=@id AND IsPosted=0", DbHelper.P("@id", draftToDelete));
                    }

                    var nextSaleResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                    string saleCode = nextSaleResult != null ? nextSaleResult.ToString() : "1";
                    int warehouseID = GetSelectedWarehouseID();
                    string priceTier = GetSelectedPriceTier();
                    decimal total = 0;
                    foreach (var item in _items) total += item.Total;

                    // Insert Sales as IsPosted = 0 (Draft)
                    int saleID = DbHelper.ExecuteInsertTrans(trans,
                        @"INSERT INTO Sales (SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,DiscountAmount,DiscountPct,Notes,CreatedBy,IsPosted,WarehouseID,PriceTier,ShiftID,CashPaid,ShippingCharge,OrderType,TableNumber)
                          VALUES (@sc,GETDATE(),'Cash',@cid,@did,@tot,0,0,'POS_DRAFT',@emp,0,@wid,@tier,@sid,0,0,@ot,@tn)",
                        DbHelper.P("@sc", saleCode), DbHelper.P("@cid", clientID > 0 ? (object)clientID : DBNull.Value),
                        DbHelper.P("@did", selectedDriver.HasValue ? (object)selectedDriver.Value : DBNull.Value),
                        DbHelper.P("@tot", total),
                        DbHelper.P("@emp", Session.EmpID), DbHelper.P("@wid", warehouseID),
                        DbHelper.P("@tier", priceTier),
                        DbHelper.P("@sid", Session.CurrentShiftID.HasValue ? (object)Session.CurrentShiftID.Value : DBNull.Value),
                        DbHelper.P("@ot", string.IsNullOrEmpty(orderType) ? DBNull.Value : (object)orderType),
                        DbHelper.P("@tn", string.IsNullOrEmpty(tableNum) ? DBNull.Value : (object)tableNum));

                    if (saleID <= 0) throw new Exception("فشل حفظ تعليق الطلب.");

                    foreach (var item in _items)
                    {
                        decimal costToSave = item.Unit1Cost > 0m ? item.Unit1Cost : (item.Cost > 0m && item.Factor > 0m ? item.Cost / item.Factor : item.Cost);
                        if (costToSave <= 0m)
                        {
                            var costObj = DbHelper.ScalarTrans(trans,
                                "SELECT COALESCE(NULLIF(CostPrice, 0), NULLIF(Unit1PurchasePrice, 0), PurchasePrice, 0) FROM Products WHERE ProductID = @pid",
                                DbHelper.P("@pid", item.ProductID));
                            costToSave = (costObj != null && costObj != DBNull.Value) ? Convert.ToDecimal(costObj) : 0m;
                        }

                        if (costToSave <= 0m)
                        {
                            var lastPurCost = DbHelper.ScalarTrans(trans,
                                "SELECT TOP 1 (UnitPrice / NULLIF(Factor, 0)) FROM PurchaseItems WHERE ProductID = @pid AND UnitPrice > 0 ORDER BY PurchaseItemID DESC",
                                DbHelper.P("@pid", item.ProductID));
                            if (lastPurCost != null && lastPurCost != DBNull.Value)
                                costToSave = Convert.ToDecimal(lastPurCost);
                        }

                        DbHelper.ExecuteInsertTrans(trans,
                            @"INSERT INTO SaleItems (SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier,UnitName,Factor,ExpiryDate,BatchID,KitchenNotes,IMEI,CostPrice)
                              VALUES (@sid,@pid,@qty,@up,@tp,0,@discAmt,@tier,@un,@f,@exp,@bid,@kn,@imei,@cp)",
                            DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                            DbHelper.P("@qty", item.Qty), DbHelper.P("@up", item.Price), DbHelper.P("@tp", item.Total),
                            DbHelper.P("@discAmt", item.DiscountAmt),
                            DbHelper.P("@tier", priceTier),
                            DbHelper.P("@un", (object)item.UnitName ?? DBNull.Value),
                            DbHelper.P("@f", item.Factor),
                            DbHelper.P("@exp", item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value : DBNull.Value),
                            DbHelper.P("@bid", item.BatchID.HasValue ? (object)item.BatchID.Value : DBNull.Value),
                            DbHelper.P("@kn", string.IsNullOrEmpty(item.KitchenNotes) ? DBNull.Value : (object)item.KitchenNotes),
                            DbHelper.P("@imei", string.IsNullOrEmpty(item.IMEI) ? DBNull.Value : (object)item.IMEI.Trim()),
                            DbHelper.P("@cp", costToSave));
                    }
                    _lastSaleID = saleID;
                });

                if (AppConfig.IsRestaurant)
                {
                    try { new FrmKitchenPrint(_lastSaleID); } catch { }
                }

                MessageBox.Show("تم تعليق الطلب بنجاح.", "تم التعليق", MessageBoxButtons.OK, MessageBoxIcon.Information);
                NewInvoice();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPOS.SuspendCurrentOrder", ex);
                MessageBox.Show("حدث خطأ أثناء تعليق الطلب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isSaving = false;
            }
        }

        private void RecallDraftSale()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "📋 الطلبات المعلقة والطاولات النشطة";
                dlg.Size = new Size(760, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.Font = this.Font;
                dlg.BackColor = Theme.BgMain;

                // ── شريط الأزرار السفلي ──────────────────────────── (يُضاف أولاً)
                var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = Theme.BgHeader, Padding = new Padding(10, 12, 10, 10) };
                var btnLoad = Theme.MakeButton("✅ استرجاع الطلب", Theme.Primary, new Point(10, 10), new Size(165, 42));
                var btnDelete = Theme.MakeButton("❌ حذف المعلق", Theme.Danger, new Point(185, 10), new Size(145, 42));
                var btnCancelDraft = Theme.MakeButton("رجوع", Color.FromArgb(70,70,70), new Point(340, 10), new Size(105, 42));
                pnlButtons.Controls.Add(btnLoad);
                pnlButtons.Controls.Add(btnDelete);
                pnlButtons.Controls.Add(btnCancelDraft);
                dlg.Controls.Add(pnlButtons);

                // ── جدول الطلبات المعلقة ─────────────────────────── (يُضاف ثانياً - Fill)
                var dg = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = Color.White,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    Font = new Font("Segoe UI", 11f),
                    GridColor = Color.FromArgb(210, 215, 220),
                    ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                    EnableHeadersVisualStyles = false,
                    ColumnHeadersHeight = 38,
                    RowTemplate = { Height = 34 },
                    ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Theme.Primary,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Padding = new Padding(4)
                    },
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.White,
                        ForeColor = Theme.TextMain,
                        SelectionBackColor = Theme.Accent,
                        SelectionForeColor = Color.White,
                        Padding = new Padding(4),
                        Alignment = DataGridViewContentAlignment.MiddleCenter
                    },
                    AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(245, 247, 250),
                        ForeColor = Theme.TextMain,
                        SelectionBackColor = Theme.Accent,
                        SelectionForeColor = Color.White
                    }
                };
                dlg.Controls.Add(dg);

                // ── شريط البحث العلوي ─────────────────────────────── (يُضاف أخيراً - Top)
                var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.BgCard, Padding = new Padding(10, 10, 10, 6) };
                var txtSearch = new TextBox { Width = 260, Height = 30, Font = new Font("Segoe UI", 11f), BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.BgInput };
                var lblSearch = new Label { Text = "🔍 بحث (طاولة / عميل):", Width = 165, Height = 28, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain };
                var flowSearch = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                flowSearch.Controls.Add(lblSearch);
                flowSearch.Controls.Add(txtSearch);
                pnlSearch.Controls.Add(flowSearch);
                dlg.Controls.Add(pnlSearch);

                // Fetch Draft Sales
                Action refreshDrafts = () =>
                {
                    DataTable dt = DbHelper.Query(
                        @"SELECT s.SaleID, s.SaleCode AS [رقم الفاتورة], 
                                 s.SaleDate AS [التاريخ والوقت],
                                 CASE s.OrderType 
                                    WHEN 'DineIn' THEN N'🍽️ صالة (' + ISNULL(s.TableNumber,'') + ')'
                                    WHEN 'Delivery' THEN N'🛵 توصيل'
                                    ELSE N'🛍️ تيك أواي'
                                 END AS [نوع الطلب],
                                 ISNULL(s.TableNumber, '') AS [رقم الطاولة],
                                 ISNULL(c.ClientName, N'---') AS [العميل],
                                 s.TotalAmount AS [الإجمالي]
                          FROM Sales s
                          LEFT JOIN Clients c ON s.ClientID = c.ClientID
                          WHERE s.IsPosted = 0 AND (s.Notes = 'POS_DRAFT' OR s.Notes = 'POS')
                          ORDER BY s.SaleDate DESC");
                    
                    dg.DataSource = dt;
                    if (dg.Columns.Contains("SaleID")) dg.Columns["SaleID"].Visible = false;
                    if (dg.Columns.Contains("رقم الطاولة")) dg.Columns["رقم الطاولة"].Visible = false;
                };

                txtSearch.TextChanged += (s, e) =>
                {
                    if (dg.DataSource is DataTable dt)
                    {
                        string val = txtSearch.Text.Trim().Replace("'", "''");
                        dt.DefaultView.RowFilter = string.Format("[نوع الطلب] LIKE '%{0}%' OR [العميل] LIKE '%{0}%' OR [رقم الفاتورة] LIKE '%{0}%'", val);
                    }
                };

                btnLoad.Click += (s, e) =>
                {
                    if (dg.SelectedRows.Count == 0) return;
                    int saleId = Convert.ToInt32(dg.SelectedRows[0].Cells["SaleID"].Value);

                    // Load client info, order type, table num, driver ID
                    DataRow saleRow = DbHelper.Query("SELECT ClientID, OrderType, TableNumber, DriverID FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleId)).Rows[0];
                    
                    int clientId = saleRow["ClientID"] != DBNull.Value ? Convert.ToInt32(saleRow["ClientID"]) : 0;
                    string ot = saleRow["OrderType"]?.ToString();
                    string tn = saleRow["TableNumber"]?.ToString();
                    int driverId = saleRow["DriverID"] != DBNull.Value ? Convert.ToInt32(saleRow["DriverID"]) : 0;

                    // Set client
                    if (cboClient != null)
                    {
                        cboClient.SelectedIndex = 0;
                        for (int i = 0; i < cboClient.Items.Count; i++)
                        {
                            if (cboClient.Items[i] is ComboItem ci && ci.ID == clientId)
                            {
                                cboClient.SelectedIndex = i;
                                break;
                            }
                        }
                    }

                    // Set Order Type and Table Number
                    if (AppConfig.IsRestaurant)
                    {
                        if (ot == "DineIn") { rbDineIn.Checked = true; txtTableNum.Text = tn; }
                        else if (ot == "Delivery") { rbDelivery.Checked = true; }
                        else { rbTakeaway.Checked = true; }

                        if (cboDeliveryDriver != null)
                        {
                            cboDeliveryDriver.SelectedIndex = 0;
                            for (int i = 0; i < cboDeliveryDriver.Items.Count; i++)
                            {
                                if (cboDeliveryDriver.Items[i] is ComboItem di && di.ID == driverId)
                                {
                                    cboDeliveryDriver.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }

                    // Load items
                    var itemsDt = SaleDAL.GetItems(saleId);
                    _items.Clear();
                    foreach (DataRow iRow in itemsDt.Rows)
                    {
                        _items.Add(new POSItem
                        {
                            ProductID = Convert.ToInt32(iRow["ProductID"]),
                            Code = iRow["PartNumber"]?.ToString() ?? iRow["ProductID"].ToString(),
                            Name = iRow["ProductName"].ToString(),
                            UnitName = iRow["UnitName"]?.ToString() ?? "",
                            Factor = Convert.ToDecimal(iRow["Factor"]),
                            Qty = Convert.ToDecimal(iRow["Quantity"]),
                            Price = Convert.ToDecimal(iRow["UnitPrice"]),
                            Total = Convert.ToDecimal(iRow["TotalPrice"]),
                            DiscountAmt = Convert.ToDecimal(iRow["DiscountAmt"]),
                            KitchenNotes = iRow.Table.Columns.Contains("KitchenNotes") && iRow["KitchenNotes"] != DBNull.Value ? iRow["KitchenNotes"].ToString() : ""
                        });
                    }

                    _loadedDraftSaleID = saleId;
                    RefreshGrid();
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                btnDelete.Click += (s, e) =>
                {
                    if (dg.SelectedRows.Count == 0) return;
                    if (MessageBox.Show("هل أنت متأكد من حذف هذا الطلب المعلق نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        int saleId = Convert.ToInt32(dg.SelectedRows[0].Cells["SaleID"].Value);
                        SaleDAL.DeleteDraftSale(saleId);
                        refreshDrafts();
                    }
                };

                btnCancelDraft.Click += (s, e) => dlg.Close();
                dg.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) btnLoad.PerformClick(); };

                refreshDrafts();
                dlg.ShowDialog();
            }
        }

        private class ColEntry
        {
            public string ColName { get; }
            public string HeaderText { get; }
            public ColEntry(string name, string header)
            {
                ColName = name;
                HeaderText = header;
            }
            public override string ToString() => HeaderText;
        }
    }
}
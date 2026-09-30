using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;
using System.Linq;

namespace ChickenDist.Forms
{
    /// <summary>شاشة مرتجع مبيعات واستبدال أصناف متطورة</summary>
    public class FrmReturn : Form
    {
        private DataGridView dgSales, dgItems, dgExchangeNewItems;
        private TextBox txtSearch, txtInvoiceBarcode, txtNotes, txtGenQty, txtGenPrice, txtNewGenQty, txtNewGenPrice;
        private ComboBox cboClient, cboMode, cboWarehouse, cboReturnType, cboAllProducts, cboNewExchangeProducts, cboSaleTypeFilter, cboProductFilter, cboEmployeeFilter;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnSearch, btnSave, btnAddGenItem, btnAddNewGenItem;
        private Label lblTotal, lblExchangeSummary;
        private SplitContainer _mainSplit;
        private TableLayoutPanel _pnlGridsContainer;
        private FlowLayoutPanel pnlFilter, _pnlGenItemBar, _pnlNewItemBar;
        private Control _pnlFrom, _pnlTo, _pnlSearch, _pnlBarcode, _pnlEmp;
        private DataTable _salesDt;
        private bool _isFilteringCombo = false;
        private bool _isLoadingSales = false;
        private bool _isLoadingSaleItems = false;
        private System.Windows.Forms.Timer _searchDebounceTimer;
        private decimal _selectedSaleTotalAmount = 0m;
        private decimal _selectedSaleShippingCharge = 0m;
        private decimal _selectedSalePrevReturnedAmount = 0m;
        private int _lastSavedReturnID = 0;
        private readonly System.Text.StringBuilder _scannerBuffer = new System.Text.StringBuilder();
        private DateTime _lastScannerTime = DateTime.MinValue;

        private int GetSelectedReturnID()
        {
            if (dgSales.CurrentRow != null && dgSales.CurrentRow.Cells["SaleID"].Value != null)
            {
                int saleID = Convert.ToInt32(dgSales.CurrentRow.Cells["SaleID"].Value);
                var dtRet = DbHelper.Query("SELECT TOP 1 ReturnID FROM SalesReturns WHERE SaleID = @id ORDER BY ReturnID DESC", DbHelper.P("@id", saleID));
                if (dtRet.Rows.Count > 0)
                    return Convert.ToInt32(dtRet.Rows[0]["ReturnID"]);
            }
            return _lastSavedReturnID;
        }

        public FrmReturn()
        {
            if (!Session.CanAccess("Returns"))
            {
                this.Load += (s, e) =>
                {
                    MessageBox.Show("⛔ غير مصرح لك بالوصول لشاشة مرتجع المبيعات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                };
                return;
            }
            DbHelper.EnsureShiftSchema();
            InitUI();
            LoadCombos();
            this.Load += (s, e) =>
            {
                try
                {
                    if (_mainSplit != null && _mainSplit.Height > 300)
                    {
                        _mainSplit.Panel1MinSize = 100;
                        _mainSplit.Panel2MinSize = 150;
                        int safeDist = Math.Min(210, Math.Max(100, _mainSplit.Height - 160));
                        _mainSplit.SplitterDistance = safeDist;
                    }
                }
                catch { }
            };
            this.Shown += (s, e) => { if (!_isLoadingSales) LoadSales(); };
        }

        private void LoadCombos()
        {
            LoadClients();

            // المخازن
            if (cboWarehouse != null)
            {
                var dtWh = Session.GetAllowedWarehouses(true);
                cboWarehouse.Items.Clear();
                int defWhId = Session.GetDefaultWarehouseID();
                int selIdx = 0;
                for (int i = 0; i < dtWh.Rows.Count; i++)
                {
                    int wid = Convert.ToInt32(dtWh.Rows[i]["WarehouseID"]);
                    string wname = dtWh.Rows[i]["WarehouseName"].ToString();
                    cboWarehouse.Items.Add(new ComboItem(wid, wname));
                    if (wid == defWhId) selIdx = i;
                }
                cboWarehouse.DisplayMember = "Text";
                if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = selIdx;
                cboWarehouse.Enabled = Session.IsAdmin || dtWh.Rows.Count > 1;
            }

            // تحميل أصناف الكتالوج للمرتجع العام والبديل وتصفية الفواتير بالصنف
            DataTable dtProducts = ProductDAL.GetAll(true);
            if (cboProductFilter != null)
            {
                cboProductFilter.SelectedIndexChanged -= CboProductFilter_SelectedIndexChanged;
                cboProductFilter.Tag = null;
                cboProductFilter.Items.Clear();
                cboProductFilter.Items.Add(new ComboItem(0, "الكل (جميع الأصناف)"));
                foreach (DataRow r in dtProducts.Rows)
                {
                    cboProductFilter.Items.Add(new ComboItem((int)r["ProductID"], r["ProductName"].ToString()));
                }
                cboProductFilter.DisplayMember = "Text";
                if (cboProductFilter.Items.Count > 0) cboProductFilter.SelectedIndex = 0;
                cboProductFilter.SelectedIndexChanged += CboProductFilter_SelectedIndexChanged;
            }

            if (cboAllProducts != null)
            {
                cboAllProducts.SelectedIndexChanged -= CboAllProducts_SelectedIndexChanged;
                cboAllProducts.Items.Clear();
                cboAllProducts.Items.Add(new ComboItem(0, "-- اختر / ابحث عن الصنف المرتجع --"));
                foreach (DataRow r in dtProducts.Rows)
                {
                    var ci = new ComboItem((int)r["ProductID"], r["ProductName"].ToString());
                    ci.Extra = r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m;
                    cboAllProducts.Items.Add(ci);
                }
                cboAllProducts.DisplayMember = "Text";
                if (cboAllProducts.Items.Count > 0) cboAllProducts.SelectedIndex = 0;
                cboAllProducts.SelectedIndexChanged += CboAllProducts_SelectedIndexChanged;
            }

            if (cboNewExchangeProducts != null)
            {
                cboNewExchangeProducts.SelectedIndexChanged -= CboNewExchangeProducts_SelectedIndexChanged;
                cboNewExchangeProducts.Items.Clear();
                cboNewExchangeProducts.Items.Add(new ComboItem(0, "-- اختر الصنف البديل الجديد --"));
                foreach (DataRow r in dtProducts.Rows)
                {
                    var ci = new ComboItem((int)r["ProductID"], r["ProductName"].ToString());
                    ci.Extra = r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m;
                    cboNewExchangeProducts.Items.Add(ci);
                }
                cboNewExchangeProducts.DisplayMember = "Text";
                if (cboNewExchangeProducts.Items.Count > 0) cboNewExchangeProducts.SelectedIndex = 0;
                cboNewExchangeProducts.SelectedIndexChanged += CboNewExchangeProducts_SelectedIndexChanged;
            }

            // الموظفون (فلتر البحث بالموظف)
            if (cboEmployeeFilter != null)
            {
                cboEmployeeFilter.SelectedIndexChanged -= CboEmployeeFilter_SelectedIndexChanged;
                cboEmployeeFilter.Tag = null;
                cboEmployeeFilter.Items.Clear();

                bool canReturnAll = Session.IsAdmin || Session.CanReturnAllSales();
                if (canReturnAll)
                {
                    cboEmployeeFilter.Items.Add(new ComboItem(0, "الكل (جميع الموظفين)"));
                }

                try
                {
                    DataTable dtEmp = EmployeeDAL.GetAll();
                    foreach (DataRow r in dtEmp.Rows)
                    {
                        int eid = Convert.ToInt32(r["EmpID"]);
                        if (canReturnAll || eid == Session.EmpID)
                        {
                            cboEmployeeFilter.Items.Add(new ComboItem(eid, r["EmpName"].ToString()));
                        }
                    }
                }
                catch { }
                cboEmployeeFilter.DisplayMember = "Text";

                if (!canReturnAll)
                {
                    for (int i = 0; i < cboEmployeeFilter.Items.Count; i++)
                    {
                        if (cboEmployeeFilter.Items[i] is ComboItem ci && ci.ID == Session.EmpID)
                        {
                            cboEmployeeFilter.SelectedIndex = i;
                            break;
                        }
                    }
                    cboEmployeeFilter.Enabled = false;
                }
                else
                {
                    if (cboEmployeeFilter.Items.Count > 0) cboEmployeeFilter.SelectedIndex = 0;
                    cboEmployeeFilter.Enabled = true;
                }
                cboEmployeeFilter.SelectedIndexChanged += CboEmployeeFilter_SelectedIndexChanged;
            }
        }

        private void CboAllProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboAllProducts.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                txtGenPrice.Text = ci.Extra.ToString("N2");
                BtnAddGenItem_Click(sender, e);
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        cboAllProducts.SelectedIndexChanged -= CboAllProducts_SelectedIndexChanged;
                        cboAllProducts.SelectedIndex = 0;
                        cboAllProducts.SelectedIndexChanged += CboAllProducts_SelectedIndexChanged;
                        cboAllProducts.Focus();
                    }));
                }
            }
        }

        private void CboNewExchangeProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboNewExchangeProducts.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                txtNewGenPrice.Text = ci.Extra.ToString("N2");
                BtnAddNewGenItem_Click(sender, e);
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        cboNewExchangeProducts.SelectedIndexChanged -= CboNewExchangeProducts_SelectedIndexChanged;
                        cboNewExchangeProducts.SelectedIndex = 0;
                        cboNewExchangeProducts.SelectedIndexChanged += CboNewExchangeProducts_SelectedIndexChanged;
                        cboNewExchangeProducts.Focus();
                    }));
                }
            }
        }

        private void LoadClients()
        {
            if (cboClient == null) return;
            cboClient.SelectedIndexChanged -= CboClient_SelectedIndexChanged;
            cboClient.Items.Clear();
            cboClient.Items.Add(new ComboItem(0, "👤 عميل نقدي / عام (بدون عميل مسجل)"));
            try
            {
                var dtC = ClientDAL.GetAll(true);
                foreach (DataRow r in dtC.Rows)
                {
                    cboClient.Items.Add(new ComboItem(Convert.ToInt32(r["ClientID"]), r["ClientName"].ToString()));
                }
            }
            catch { }
            cboClient.DisplayMember = "Text";
            cboClient.SelectedIndexChanged += CboClient_SelectedIndexChanged;
            if (cboClient.Items.Count > 0)
                cboClient.SelectedIndex = 0;
        }

        private void CboClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboMode != null && cboMode.SelectedIndex == 0)
                LoadSales();
        }

        private void FrmReturn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
                if (dgExchangeNewItems != null && dgExchangeNewItems.IsCurrentCellInEditMode) dgExchangeNewItems.EndEdit();
                btnSave.PerformClick();
                e.Handled = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (msg.Msg == 0x0100) // WM_KEYDOWN
            {
                double elapsed = (DateTime.Now - _lastScannerTime).TotalMilliseconds;
                _lastScannerTime = DateTime.Now;
                if (elapsed > 100) _scannerBuffer.Clear();

                if (keyData == Keys.Enter)
                {
                    if (txtSearch != null && txtSearch.Focused)
                    {
                        _searchDebounceTimer?.Stop();
                        DoBarcodeSearch(txtSearch.Text.Trim());
                        return true;
                    }
                    if (txtInvoiceBarcode != null && txtInvoiceBarcode.Focused)
                    {
                        DoBarcodeSearch(txtInvoiceBarcode.Text.Trim());
                        return true;
                    }
                    if (_scannerBuffer.Length >= 2 && elapsed < 85)
                    {
                        string scanned = _scannerBuffer.ToString().Trim();
                        _scannerBuffer.Clear();
                        DoBarcodeSearch(scanned);
                        return true;
                    }
                    _scannerBuffer.Clear();
                }
                else
                {
                    char c = (char)(keyData & Keys.KeyCode);
                    if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '*' || c == '/')
                    {
                        _scannerBuffer.Append(c);
                    }
                }
            }

            if (keyData == Keys.F2)
            {
                if (cboMode != null && cboMode.SelectedIndex != 0)
                {
                    OpenProductSearchDialog();
                    return true;
                }
            }
            else if (keyData == Keys.F5)
            {
                if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
                if (dgExchangeNewItems != null && dgExchangeNewItems.IsCurrentCellInEditMode) dgExchangeNewItems.EndEdit();
                BtnSave_Click(this, EventArgs.Empty);
                return true;
            }
            else if (keyData == Keys.Enter)
            {
                if (dgItems.Focused || dgItems.EditingControl != null)
                {
                    dgItems.EndEdit();
                    var curCell = dgItems.CurrentCell;
                    if (curCell != null && curCell.RowIndex >= 0 && curCell.RowIndex < dgItems.Rows.Count)
                    {
                        int nextCol = -1;
                        for (int col = curCell.ColumnIndex + 1; col < dgItems.ColumnCount; col++)
                        {
                            if (!dgItems.Columns[col].ReadOnly && dgItems.Columns[col].Visible)
                            {
                                nextCol = col;
                                break;
                            }
                        }

                        if (nextCol != -1)
                        {
                            dgItems.CurrentCell = dgItems.Rows[curCell.RowIndex].Cells[nextCol];
                            dgItems.BeginEdit(true);
                            return true;
                        }
                        else
                        {
                            txtNotes.Focus();
                            return true;
                        }
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitUI()
        {
            this.Text = "مرتجع مبيعات واستبدال أصناف";
            this.Size = new Size(1180, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmReturn_KeyDown;

            // ===== 1. Top Filter panel =====
            pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 8, 8, 8),
                WrapContents = true
            };

            // دالة مساعدة لتطبيق لون مميز ومقروء على جميع خانات البحث والإدخال (خط داكن عالي التباين)
            void StyleSearchInput(Control c)
            {
                c.BackColor = Theme.BgInput;
                c.ForeColor = Theme.TextInput;
                c.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            }

            // دالة مساعدة لإنشاء حاوية (الاسم أولاً ثم الخانة) لضمان عدم انفصال التسمية عن الحقل أبداً
            Panel MakeFilterPanel(string labelText, Control inputCtrl, int inputWidth, Control extraCtrl = null)
            {
                var lbl = new Label
                {
                    Text = labelText,
                    AutoSize = true,
                    ForeColor = Theme.TextMain,
                    Font = Theme.FontBold,
                    Margin = new Padding(0, 5, 2, 0)
                };

                inputCtrl.Width = inputWidth;
                inputCtrl.Height = 26;

                var pnl = new FlowLayoutPanel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    FlowDirection = FlowDirection.LeftToRight,
                    BackColor = Color.Transparent,
                    Margin = new Padding(4, 2, 4, 2),
                    Padding = new Padding(0),
                    WrapContents = false
                };

                pnl.Controls.Add(lbl);
                pnl.Controls.Add(inputCtrl);
                if (extraCtrl != null) pnl.Controls.Add(extraCtrl);
                return pnl;
            }

            cboMode = new ComboBox
            {
                Width = 225, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboMode.Items.AddRange(new object[] { 
                "🔁 مرتجع مبيعات من فاتورة", 
                "↩ مرتجع مبيعات عام (بدون فاتورة)", 
                "🔄 استبدال فوري (مرتجع + أصناف بديلة)" 
            });
            cboMode.SelectedIndex = 0;
            StyleSearchInput(cboMode);
            cboMode.SelectedIndexChanged += (s, e) => ToggleReturnMode();
            var pnlMode = MakeFilterPanel("العملية:", cboMode, 225);

            cboClient = new ComboBox 
            { 
                Width = 150, 
                DropDownStyle = ComboBoxStyle.DropDown, 
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            StyleSearchInput(cboClient);
            SetupSearchableCombo(cboClient);

            var btnClientSearch = new Button
            {
                Text = "🔍",
                Width = 32,
                Height = 26,
                Font = Theme.FontBold,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 0, 0)
            };
            btnClientSearch.FlatAppearance.BorderSize = 0;
            btnClientSearch.Click += (s, e) =>
            {
                using (var frm = new FrmClientSearch())
                {
                    if (frm.ShowDialog() == DialogResult.OK && frm.SelectedClientID > 0)
                    {
                        int cid = frm.SelectedClientID;
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
            var pnlClient = MakeFilterPanel("العميل:", cboClient, 150, btnClientSearch);

            cboSaleTypeFilter = new ComboBox
            {
                Width = 125, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboSaleTypeFilter.Items.AddRange(new object[] { "الكل", "💵 نقدي", "💳 فيزا", "📋 آجل", "📅 تقسيط", "🚚 حمولة مندوب" });
            cboSaleTypeFilter.SelectedIndex = 0;
            cboSaleTypeFilter.SelectedIndexChanged += CboSaleTypeFilter_SelectedIndexChanged;
            StyleSearchInput(cboSaleTypeFilter);
            var pnlSaleType = MakeFilterPanel("نوع الفاتورة:", cboSaleTypeFilter, 125);

            cboProductFilter = new ComboBox
            {
                Width = 150, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            StyleSearchInput(cboProductFilter);
            SetupSearchableCombo(cboProductFilter);
            var pnlProduct = MakeFilterPanel("بحث بالصنف:", cboProductFilter, 150);

            cboEmployeeFilter = new ComboBox
            {
                Width = 140, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextInput,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            StyleSearchInput(cboEmployeeFilter);
            _pnlEmp = MakeFilterPanel("الموظف:", cboEmployeeFilter, 140);

            cboReturnType = new ComboBox
            {
                Width = 145, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextInput,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cboReturnType.Items.Add("💵 نقدي (كاش)");
            cboReturnType.Items.Add("💳 فيزا (شبكة / بطاقة)");
            cboReturnType.Items.Add("📋 آجل (على الحساب)");
            cboReturnType.SelectedIndex = 0;
            var pnlRetType = MakeFilterPanel("طريقة دفع المرتجع:", cboReturnType, 145);

            cboWarehouse = new ComboBox
            {
                Width = 120, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextInput,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            var pnlWh = MakeFilterPanel("المخزن:", cboWarehouse, 120);

            dtpFrom = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Today.AddMonths(-1) };
            dtpFrom.ValueChanged += (s, e) => { if (cboMode != null && cboMode.SelectedIndex == 0) LoadSales(); };
            _pnlFrom = MakeFilterPanel("من:", dtpFrom, 180);

            dtpTo = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Now };
            dtpTo.ValueChanged += (s, e) => { if (cboMode != null && cboMode.SelectedIndex == 0) LoadSales(); };
            _pnlTo = MakeFilterPanel("إلى:", dtpTo, 180);

            txtSearch = new TextBox { Width = 230, RightToLeft = RightToLeft.Yes };
            StyleSearchInput(txtSearch);
            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 350 };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                if (cboMode != null && cboMode.SelectedIndex == 0) LoadSales();
            };
            txtSearch.TextChanged += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    _searchDebounceTimer.Stop();
                    DoBarcodeSearch(txtSearch.Text.Trim());
                }
            };
            _pnlSearch = MakeFilterPanel("بحث (صنف/كود/رقم/باركود فاتورة):", txtSearch, 230);

            txtInvoiceBarcode = new TextBox { Width = 140, RightToLeft = RightToLeft.No };
            StyleSearchInput(txtInvoiceBarcode);
            txtInvoiceBarcode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DoBarcodeSearch(txtInvoiceBarcode.Text.Trim());
                }
            };
            _pnlBarcode = MakeFilterPanel("📷 مسح بالاسكانر:", txtInvoiceBarcode, 140);

            btnSearch = Theme.MakeButton("🔍 تحديث", Theme.Accent);
            btnSearch.Size = new Size(85, 28);
            btnSearch.Margin = new Padding(6, 4, 6, 2);
            btnSearch.Click += (s, e) => LoadSales();

            pnlFilter.Controls.AddRange(new Control[] { 
                pnlMode, 
                pnlClient, 
                pnlSaleType, 
                pnlProduct, 
                _pnlEmp,
                pnlRetType, 
                pnlWh, 
                _pnlFrom, 
                _pnlTo, 
                _pnlSearch, 
                _pnlBarcode, 
                btnSearch 
            });

            // ===== شريط إضافة صنف مرتجع عام / بديل =====
            _pnlGenItemBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.FromArgb(45, 35, 45),
                Padding = new Padding(10, 6, 10, 6),
                Visible = false
            };

            var lblGenTitle = new Label { Text = "↩ صنف مرتجع:", AutoSize = true, ForeColor = Color.LightCoral, Margin = new Padding(5, 5, 0, 0), Font = Theme.FontBold };
            cboAllProducts = new ComboBox
            {
                Width = 240, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, ForeColor = Theme.TextInput,
                Font = Theme.FontBold
            };
            var lblGenQtyL = new Label { Text = "الكمية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtGenQty = new TextBox { Width = 60, Text = "1", BackColor = Theme.BgInput, ForeColor = Theme.TextInput, RightToLeft = RightToLeft.Yes, Font = Theme.FontBold };
            var lblGenPriceL = new Label { Text = "السعر:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtGenPrice = new TextBox { Width = 70, Text = "0", BackColor = Theme.BgInput, ForeColor = Theme.TextInput, RightToLeft = RightToLeft.Yes, Font = Theme.FontBold, ReadOnly = !Session.IsAdmin && !Session.CanEditPrice("Returns") };
            
            btnAddGenItem = Theme.MakeButton("🔍 بحث (F2)", Color.FromArgb(40, 110, 160));
            btnAddGenItem.Size = new Size(110, 28);
            btnAddGenItem.Margin = new Padding(8, 0, 0, 0);
            btnAddGenItem.Click += (s, e) => OpenProductSearchDialog();

            var btnAddGenToList = Theme.MakeButton("➕ إضافة صنف", Color.FromArgb(40, 140, 70));
            btnAddGenToList.Size = new Size(115, 28);
            btnAddGenToList.Margin = new Padding(8, 0, 0, 0);
            btnAddGenToList.Click += BtnAddGenItem_Click;

            txtGenPrice.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BtnAddGenItem_Click(s, e); e.Handled = true; e.SuppressKeyPress = true; } };
            txtGenQty.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { txtGenPrice.Focus(); e.Handled = true; e.SuppressKeyPress = true; } };

            _pnlGenItemBar.Controls.AddRange(new Control[] { lblGenTitle, cboAllProducts, lblGenQtyL, txtGenQty, lblGenPriceL, txtGenPrice, btnAddGenToList, btnAddGenItem });

            // شريط إضافة صنف جديد للاستبدال
            _pnlNewItemBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.FromArgb(35, 45, 35),
                Padding = new Padding(10, 6, 10, 6),
                Visible = false
            };

            var lblNewTitle = new Label { Text = "🆕 صنف بديل جديد:", AutoSize = true, ForeColor = Color.LightGreen, Margin = new Padding(5, 5, 0, 0), Font = Theme.FontBold };
            cboNewExchangeProducts = new ComboBox
            {
                Width = 220, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, ForeColor = Theme.TextInput,
                Font = Theme.FontBold
            };
            var lblNewQtyL = new Label { Text = "الكمية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtNewGenQty = new TextBox { Width = 60, Text = "1", BackColor = Theme.BgInput, ForeColor = Theme.TextInput, RightToLeft = RightToLeft.Yes, Font = Theme.FontBold };
            var lblNewPriceL = new Label { Text = "السعر:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtNewGenPrice = new TextBox { Width = 70, Text = "0", BackColor = Theme.BgInput, ForeColor = Theme.TextInput, RightToLeft = RightToLeft.Yes, Font = Theme.FontBold, ReadOnly = !Session.IsAdmin && !Session.CanEditPrice("Returns") };
            btnAddNewGenItem = Theme.MakeButton("➕ إضافة بديل", Color.FromArgb(50, 140, 70));
            btnAddNewGenItem.Size = new Size(110, 26);
            btnAddNewGenItem.Margin = new Padding(10, 0, 0, 0);
            btnAddNewGenItem.Click += BtnAddNewGenItem_Click;

            _pnlNewItemBar.Controls.AddRange(new Control[] { lblNewTitle, cboNewExchangeProducts, lblNewQtyL, txtNewGenQty, lblNewPriceL, txtNewGenPrice, btnAddNewGenItem });

            // ===== 2. SplitContainer =====
            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6,
                FixedPanel = FixedPanel.Panel1
            };
            _mainSplit.Panel1.Padding = new Padding(10, 5, 10, 5);
            _mainSplit.Panel2.Padding = new Padding(10, 5, 10, 5);

            // Top Grid: Sales Invoices
            dgSales = MakeGrid();
            dgSales.AutoGenerateColumns = false;
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleID", DataPropertyName = "SaleID", Visible = false });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy", DataPropertyName = "CreatedBy", Visible = false });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", DataPropertyName = "SaleCode", HeaderText = "رقم الفاتورة", FillWeight = 40f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleDate", DataPropertyName = "SaleDate", HeaderText = "التاريخ والوقت", FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleType", DataPropertyName = "SaleType", HeaderText = "نوع الفاتورة", FillWeight = 42f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", DataPropertyName = "WarehouseName", HeaderText = "المخزن", FillWeight = 48f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ClientCode", 
                DataPropertyName = "ClientCode", 
                HeaderText = "كود العميل", 
                FillWeight = 38f,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", DataPropertyName = "ClientName", HeaderText = "اسم العميل", FillWeight = 85f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "DriverName", DataPropertyName = "DriverName", HeaderText = "المندوب", FillWeight = 60f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ItemsCount", 
                DataPropertyName = "ItemsCount", 
                HeaderText = "عدد الأصناف", 
                FillWeight = 34f,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "TotalQty", 
                DataPropertyName = "TotalQty", 
                HeaderText = "إجمالي القطع", 
                FillWeight = 38f, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(14, 165, 233), Format = "N2" } 
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "TotalBeforeDiscount", 
                DataPropertyName = "TotalBeforeDiscount", 
                HeaderText = "قبل الخصم", 
                FillWeight = 45f, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } 
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "DiscountAmount", 
                DataPropertyName = "DiscountAmount", 
                HeaderText = "الخصم ✂", 
                FillWeight = 40f, 
                DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(249, 115, 22), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } 
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "TotalAmount", 
                DataPropertyName = "TotalAmount", 
                HeaderText = "بعد الخصم (قبل المرتجع)", 
                FillWeight = 50f, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } 
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ShippingCharge", 
                DataPropertyName = "ShippingCharge", 
                HeaderText = "خدمة شحن", 
                FillWeight = 36f, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } 
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ReturnAmount", 
                DataPropertyName = "ReturnAmount", 
                HeaderText = "المرتجع ↩", 
                FillWeight = 40f, 
                DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(231, 76, 60), Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2" } 
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "NetAmount", 
                DataPropertyName = "NetAmount", 
                HeaderText = "الصافي النهائي ✔", 
                FillWeight = 48f, 
                DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } 
            });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", DataPropertyName = "CreatedByName", HeaderText = "القائم بالحركة", FillWeight = 50f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", DataPropertyName = "Notes", HeaderText = "الملاحظات", FillWeight = 70f });

            dgSales.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var row = dgSales.Rows[e.RowIndex];
                    if (row.Cells["ReturnAmount"].Value != null && decimal.TryParse(row.Cells["ReturnAmount"].Value.ToString(), out decimal retAmt) && retAmt > 0)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242); // خلفية هادئة مائلة للوردي الفاتح بدلاً من الأصفر
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27); // خط عنابي داكن واضح
                        row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 38, 38);
                        row.DefaultCellStyle.SelectionForeColor = Color.White;
                    }

                    if (dgSales.Columns[e.ColumnIndex].Name == "ClientCode" && e.Value != null)
                    {
                        string cCode = e.Value.ToString();
                        if (cCode == "0")
                        {
                            e.CellStyle.ForeColor = Color.FromArgb(160, 160, 160);
                            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
                        }
                        else
                        {
                            e.CellStyle.ForeColor = Color.FromArgb(5, 120, 75); // أخضر داكن واضح بدلاً من الفاتح المضيء
                            e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        }
                    }

                    if (dgSales.Columns[e.ColumnIndex].Name == "SaleType" && e.Value != null)
                    {
                        string val = e.Value.ToString();
                        if (val == "Cash") e.Value = "💵 نقدي";
                        else if (val == "Visa") e.Value = "💳 فيزا";
                        else if (val == "Credit") e.Value = "📋 آجل";
                        else if (val == "Mixed") e.Value = "💳 مختلط (كاش+فيزا)";
                        else if (val == "DriverLoad") e.Value = "🚚 حمولة مندوب";
                        else if (val == "Installment") e.Value = "📅 تقسيط";
                    }
                }
            };

            var ctxSales = new ContextMenuStrip();
            var miPrintRet = new ToolStripMenuItem("🖨️ طباعة إيصال مرتجع هذه الفاتورة (اختيار المقاس)");
            miPrintRet.Click += (s, e) =>
            {
                int retID = GetSelectedReturnID();
                if (retID > 0)
                {
                    FrmPrintChoiceDialog.PromptAndPrintReturn(this, retID);
                }
                else
                {
                    MessageBox.Show("لا يوجد مرتجع مسجل لهذه الفاتورة بعد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            var miWhatsAppRet = new ToolStripMenuItem("📱 إرسال إيصال المرتجع واتساب (نموذج الطارق)");
            miWhatsAppRet.Click += (s, e) =>
            {
                int retID = GetSelectedReturnID();
                if (retID > 0)
                {
                    WhatsAppSender.SendReturnReceipt(this, retID);
                }
                else
                {
                    MessageBox.Show("لا يوجد مرتجع مسجل لهذه الفاتورة بعد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            ctxSales.Items.AddRange(new ToolStripItem[] { miPrintRet, miWhatsAppRet });
            dgSales.ContextMenuStrip = ctxSales;

            dgSales.SelectionChanged += DgSales_SelectionChanged;
            _mainSplit.Panel1.Controls.Add(dgSales);

            // Bottom Grid: Selected Sale Items / Return Items
            dgItems = MakeGrid();
            dgItems.ReadOnly = false;
            dgItems.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgItems.RowTemplate.Height = 28;
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });

            // 1. اسم الصنف المرتجع - مساحة موسعة ومريحة لظهور الاسم بالكامل
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ProductName", 
                HeaderText = "الصنف المرتجع", 
                ReadOnly = true, 
                FillWeight = 160f, 
                MinimumWidth = 200,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(20, 25, 35)
                }
            });

            // 2. اللون
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Color", 
                HeaderText = "اللون", 
                ReadOnly = true, 
                FillWeight = 35f, 
                MinimumWidth = 65, 
                Visible = AppConfig.IsClothing,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(20, 25, 35) }
            });

            // 3. المقاس
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ProductSize", 
                HeaderText = "المقاس", 
                ReadOnly = true, 
                FillWeight = 35f, 
                MinimumWidth = 65, 
                Visible = AppConfig.IsClothing,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(20, 25, 35) }
            });

            // 4. الكمية الأصلية
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "SoldQty", 
                HeaderText = "الكمية الأصلية", 
                ReadOnly = true, 
                FillWeight = 42f, 
                MinimumWidth = 80,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(20, 25, 35)
                }
            });

            // 5. المرتجع السابق
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "PrevReturnedQty", 
                HeaderText = "المرتجع السابق", 
                ReadOnly = true, 
                FillWeight = 45f, 
                MinimumWidth = 85,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(185, 28, 28)
                }
            });

            // 6. الرصيد الفعلي 📦
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "CurrentStock", 
                HeaderText = "الرصيد الفعلي 📦", 
                ReadOnly = true, 
                FillWeight = 45f, 
                MinimumWidth = 90, 
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    ForeColor = Color.FromArgb(5, 120, 75), 
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), 
                    Alignment = DataGridViewContentAlignment.MiddleCenter 
                } 
            });

            // 7. الوحدة
            dgItems.Columns.Add(new DataGridViewComboBoxColumn 
            { 
                Name = "UnitName", 
                HeaderText = "الوحدة", 
                ReadOnly = false, 
                FillWeight = 40f, 
                MinimumWidth = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(20, 25, 35) }
            });
            
            // 8. المرتجع الجديد
            var colNew = new DataGridViewTextBoxColumn 
            { 
                Name = "NewReturnedQty", 
                HeaderText = "المرتجع الجديد", 
                ReadOnly = false, 
                FillWeight = 50f, 
                MinimumWidth = 90, 
                ValueType = typeof(decimal),
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    BackColor = Color.FromArgb(240, 249, 255), 
                    ForeColor = Color.FromArgb(15, 23, 42), 
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter 
                }
            };
            dgItems.Columns.Add(colNew);
            
            // 8.5. سعر الفاتورة الأصلي قبل الخصم
            var colOrigPrice = new DataGridViewTextBoxColumn
            {
                Name = "OriginalUnitPrice",
                HeaderText = "سعر الفاتورة",
                ReadOnly = true,
                FillWeight = 42f,
                MinimumWidth = 80,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter, 
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular)
                }
            };
            dgItems.Columns.Add(colOrigPrice);

            // الخصم
            var colDiscount = new DataGridViewTextBoxColumn
            {
                Name = "Discount",
                HeaderText = "الخصم ✂",
                ReadOnly = true,
                FillWeight = 40f,
                MinimumWidth = 80,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter, 
                    ForeColor = Color.FromArgb(249, 115, 22),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                }
            };
            dgItems.Columns.Add(colDiscount);

            // 9. سعر المرتجع (الصافي بعد احتساب الخصم)
            var colUnitPrice = new DataGridViewTextBoxColumn
            {
                Name = "UnitPrice",
                HeaderText = "سعر المرتجع",
                ReadOnly = !Session.IsAdmin && !Session.CanEditPrice("Returns"),
                FillWeight = 45f,
                MinimumWidth = 85,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    BackColor = Color.FromArgb(240, 253, 244), 
                    ForeColor = Color.FromArgb(22, 101, 52), 
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter 
                }
            };
            dgItems.Columns.Add(colUnitPrice);

            // 10. إجمالي المرتجع
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "TotalPrice", 
                HeaderText = "إجمالي المرتجع", 
                ReadOnly = true, 
                FillWeight = 50f, 
                MinimumWidth = 95,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(185, 28, 28)
                } 
            });

            // Hidden helper columns
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "OriginalFactor", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetLineTotal", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetUnitPrice", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldQtyInSmallest", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedQtyInSmallest", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseUnitName", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit1Name", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit1SalePrice", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Name", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Factor", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2SalePrice", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit3Factor", Visible = false });

            dgItems.CellValidating += DgItems_CellValidating;
            dgItems.CellValueChanged += DgItems_CellValueChanged;
            dgItems.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "UnitName")
                {
                    dgItems.CurrentCell = dgItems.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    dgItems.BeginEdit(true);
                }
            };
            dgItems.EditingControlShowing += (s, e) =>
            {
                if (dgItems.CurrentCell != null && dgItems.CurrentCell.OwningColumn.Name == "UnitName" && e.Control is ComboBox cb)
                {
                    cb.DroppedDown = true;
                }
            };
            dgItems.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "CurrentStock")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(5, 120, 75); // أخضر داكن واضح
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            };
            dgItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(140, 40, 40);

            dgItems.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    if (cboMode != null && cboMode.SelectedIndex != 0)
                    {
                        if (dgItems.CurrentRow != null && !dgItems.CurrentRow.IsNewRow)
                        {
                            dgItems.Rows.Remove(dgItems.CurrentRow);
                            RecalcTotals();
                        }
                    }
                    else
                    {
                        if (dgItems.CurrentRow != null)
                        {
                            dgItems.CurrentRow.Cells["NewReturnedQty"].Value = 0m;
                            dgItems.CurrentRow.Cells["TotalPrice"].Value = "0.00";
                            RecalcTotals();
                        }
                    }
                }
            };

            var ctxItems = new ContextMenuStrip();
            var miResetOrDel = new ToolStripMenuItem("❌ إلغاء تحديد / تصفير المرتجع لهذا الصنف");
            miResetOrDel.Click += (s, e) =>
            {
                if (cboMode != null && cboMode.SelectedIndex != 0)
                {
                    if (dgItems.CurrentRow != null && !dgItems.CurrentRow.IsNewRow)
                    {
                        dgItems.Rows.Remove(dgItems.CurrentRow);
                        RecalcTotals();
                    }
                }
                else
                {
                    if (dgItems.CurrentRow != null)
                    {
                        dgItems.CurrentRow.Cells["NewReturnedQty"].Value = 0m;
                        dgItems.CurrentRow.Cells["TotalPrice"].Value = "0.00";
                        RecalcTotals();
                    }
                }
            };
            ctxItems.Items.Add(miResetOrDel);
            dgItems.ContextMenuStrip = ctxItems;

            // جدول أصناف البديل الجديد في الاستبدال
            dgExchangeNewItems = MakeGrid();
            dgExchangeNewItems.ReadOnly = false;
            dgExchangeNewItems.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgExchangeNewItems.RowTemplate.Height = 28;
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ProductName", 
                HeaderText = "الصنف البديل الجديد", 
                ReadOnly = true, 
                FillWeight = 140f,
                MinimumWidth = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 25, 35) }
            });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Color", 
                HeaderText = "اللون", 
                ReadOnly = true, 
                FillWeight = 35f, 
                MinimumWidth = 60, 
                Visible = AppConfig.IsClothing, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(20, 25, 35) } 
            });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ProductSize", 
                HeaderText = "المقاس", 
                ReadOnly = true, 
                FillWeight = 35f, 
                MinimumWidth = 60, 
                Visible = AppConfig.IsClothing, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(20, 25, 35) } 
            });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "NewQty", 
                HeaderText = "الكمية", 
                ReadOnly = false, 
                FillWeight = 45f, 
                MinimumWidth = 70, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), BackColor = Color.FromArgb(240, 249, 255) } 
            });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "UnitPrice", 
                HeaderText = "سعر البيع", 
                ReadOnly = false, 
                FillWeight = 45f, 
                MinimumWidth = 70, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(22, 101, 52), BackColor = Color.FromArgb(240, 253, 244) } 
            });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "TotalPrice", 
                HeaderText = "إجمالي الصرف", 
                ReadOnly = true, 
                FillWeight = 50f, 
                MinimumWidth = 80, 
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 58, 138) } 
            });
            dgExchangeNewItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 110, 60);
            dgExchangeNewItems.CellValueChanged += (s, e) => RecalcTotals();
            dgExchangeNewItems.Visible = false;

            dgExchangeNewItems.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && dgExchangeNewItems.CurrentRow != null && !dgExchangeNewItems.CurrentRow.IsNewRow)
                {
                    dgExchangeNewItems.Rows.Remove(dgExchangeNewItems.CurrentRow);
                    RecalcTotals();
                }
            };
            var ctxExchange = new ContextMenuStrip();
            var miDelEx = new ToolStripMenuItem("❌ حذف هذا الصنف البديل");
            miDelEx.Click += (s, e) =>
            {
                if (dgExchangeNewItems.CurrentRow != null && !dgExchangeNewItems.CurrentRow.IsNewRow)
                {
                    dgExchangeNewItems.Rows.Remove(dgExchangeNewItems.CurrentRow);
                    RecalcTotals();
                }
            };
            ctxExchange.Items.Add(miDelEx);
            dgExchangeNewItems.ContextMenuStrip = ctxExchange;

            _pnlGridsContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1
            };
            _pnlGridsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _pnlGridsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _pnlGridsContainer.Controls.Add(dgItems, 0, 0);

            _mainSplit.Panel2.Controls.Add(_pnlGridsContainer);

            // ===== 3. Footer panel =====
            var pnlFoot = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 62, 
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10),
                WrapContents = false
            };

            var lblNotesL = new Label { Text = "ملاحظات العملية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 0, 0), Font = Theme.FontBold };
            txtNotes = new TextBox { Width = 220, Height = 28, BackColor = Theme.BgInput, ForeColor = Theme.TextInput, RightToLeft = RightToLeft.Yes, Font = Theme.FontBold, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5, 5, 20, 0) };
            
            lblTotal = new Label 
            { 
                Text = "إجمالي المرتجع: 0.00 ج", 
                ForeColor = Color.FromArgb(185, 28, 28), 
                AutoSize = true,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Margin = new Padding(20, 4, 30, 0)
            };

            lblExchangeSummary = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(30, 58, 138),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Margin = new Padding(10, 8, 10, 0),
                Visible = false
            };

            btnSave = Theme.MakeButton("💾 حفظ العملية (F5)", Color.FromArgb(160, 50, 50));
            btnSave.Width = 180;
            btnSave.Height = 38;
            btnSave.Margin = new Padding(20, 0, 0, 0);
            btnSave.Font = Theme.FontBold;
            btnSave.Click += BtnSave_Click;

            var btnReprint = Theme.MakeButton("🖨️ طباعة المرتجع", Color.FromArgb(70, 70, 95));
            btnReprint.Width = 150;
            btnReprint.Height = 38;
            btnReprint.Margin = new Padding(8, 0, 0, 0);
            btnReprint.Click += (s, e) =>
            {
                int retID = GetSelectedReturnID();
                if (retID > 0)
                {
                    FrmPrintChoiceDialog.PromptAndPrintReturn(this, retID);
                }
                else
                {
                    MessageBox.Show("يرجى اختيار فاتورة مسجل لها مرتجع أو حفظ مرتجع جديد أولاً للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            var btnWhatsApp = Theme.MakeButton("📱 واتساب (نموذج الطارق)", Color.FromArgb(37, 211, 102));
            btnWhatsApp.Width = 175;
            btnWhatsApp.Height = 38;
            btnWhatsApp.Margin = new Padding(8, 0, 0, 0);
            btnWhatsApp.Font = Theme.FontBold;
            btnWhatsApp.Click += (s, e) =>
            {
                int retID = GetSelectedReturnID();
                if (retID > 0)
                {
                    WhatsAppSender.SendReturnReceipt(this, retID);
                }
                else
                {
                    MessageBox.Show("يرجى اختيار فاتورة مسجل لها مرتجع أو حفظ مرتجع جديد أولاً لإرسال الإيصال عبر الواتساب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            
            pnlFoot.Controls.AddRange(new Control[] { lblNotesL, txtNotes, lblExchangeSummary, lblTotal, btnSave, btnReprint, btnWhatsApp });

            // ===== 4. Add controls =====
            this.Controls.Add(_mainSplit);
            this.Controls.Add(_pnlNewItemBar);
            this.Controls.Add(_pnlGenItemBar);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlFilter);
            _mainSplit.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private void ToggleReturnMode()
        {
            int mode = cboMode.SelectedIndex;
            bool isInvoice = mode == 0;
            bool isGeneral = mode == 1;
            bool isExchange = mode == 2;

            _mainSplit.Panel1Collapsed = !isInvoice;
            _pnlGenItemBar.Visible = !isInvoice;
            _pnlNewItemBar.Visible = isExchange;
            UpdateGridsLayout(isExchange);

            if (_pnlFrom != null) _pnlFrom.Visible = isInvoice;
            if (_pnlTo != null) _pnlTo.Visible = isInvoice;
            if (_pnlEmp != null) _pnlEmp.Visible = isInvoice;
            if (_pnlSearch != null) _pnlSearch.Visible = isInvoice;
            if (_pnlBarcode != null) _pnlBarcode.Visible = isInvoice;
            if (btnSearch != null) btnSearch.Visible = isInvoice;

            lblExchangeSummary.Visible = isExchange;

            dgItems.Rows.Clear();
            dgExchangeNewItems.Rows.Clear();
            if (dgItems.Columns.Contains("OriginalUnitPrice")) dgItems.Columns["OriginalUnitPrice"].Visible = isInvoice;
            if (dgItems.Columns.Contains("Discount")) dgItems.Columns["Discount"].Visible = isInvoice;
            if (dgItems.Columns.Contains("Color")) dgItems.Columns["Color"].Visible = AppConfig.IsClothing;
            if (dgItems.Columns.Contains("ProductSize")) dgItems.Columns["ProductSize"].Visible = AppConfig.IsClothing;
            if (dgExchangeNewItems.Columns.Contains("Color")) dgExchangeNewItems.Columns["Color"].Visible = AppConfig.IsClothing;
            if (dgExchangeNewItems.Columns.Contains("ProductSize")) dgExchangeNewItems.Columns["ProductSize"].Visible = AppConfig.IsClothing;
            RecalcTotals();
        }

        private void UpdateGridsLayout(bool isExchange)
        {
            if (_pnlGridsContainer == null) return;
            _pnlGridsContainer.SuspendLayout();
            if (isExchange)
            {
                _pnlGridsContainer.ColumnCount = 2;
                _pnlGridsContainer.ColumnStyles.Clear();
                _pnlGridsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                _pnlGridsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                if (!_pnlGridsContainer.Controls.Contains(dgExchangeNewItems))
                    _pnlGridsContainer.Controls.Add(dgExchangeNewItems, 1, 0);
                dgExchangeNewItems.Visible = true;
            }
            else
            {
                dgExchangeNewItems.Visible = false;
                if (_pnlGridsContainer.Controls.Contains(dgExchangeNewItems))
                    _pnlGridsContainer.Controls.Remove(dgExchangeNewItems);
                _pnlGridsContainer.ColumnCount = 1;
                _pnlGridsContainer.ColumnStyles.Clear();
                _pnlGridsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            }
            _pnlGridsContainer.ResumeLayout();
        }

        private void BtnAddGenItem_Click(object sender, EventArgs e)
        {
            if (!(cboAllProducts.SelectedItem is ComboItem ci) || ci.ID == 0) return;

            if (!decimal.TryParse(txtGenQty.Text, out decimal qty) || qty <= 0) qty = 1m;

            if (!decimal.TryParse(txtGenPrice.Text, out decimal price) || price < 0) price = ci.Extra;

            // إذا كان الصنف موجوداً في القائمة مسبقاً نكتفي بزيادة الكمية
            foreach (DataGridViewRow r in dgItems.Rows)
            {
                if (r.Cells["ProductID"].Value != null && Convert.ToInt32(r.Cells["ProductID"].Value) == ci.ID)
                {
                    decimal currentQty = 0m;
                    if (r.Cells["NewReturnedQty"].Value != null)
                        decimal.TryParse(r.Cells["NewReturnedQty"].Value.ToString(), out currentQty);

                    decimal newQty = currentQty + qty;
                    r.Cells["NewReturnedQty"].Value = newQty;
                    r.Cells["UnitPrice"].Value = price.ToString("N2");
                    r.Cells["TotalPrice"].Value = (newQty * price).ToString("N2");
                    RecalcTotals();
                    return;
                }
            }

            int idx = dgItems.Rows.Add();
            var row = dgItems.Rows[idx];
            int? selectedWh = (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem cw2 && cw2.ID > 0) ? (int?)cw2.ID : null;
            decimal actStock = GetProductActualStock(ci.ID, selectedWh);

            var dtProdInfo = DbHelper.Query("SELECT Unit, Unit1Name, Unit1SalePrice, Unit2Name, Unit2Factor, Unit2SalePrice, Unit3Factor, SalePrice, Color, ProductSize FROM Products WHERE ProductID = @id", DbHelper.P("@id", ci.ID));
            string baseUnit = "";
            string u1Name = null, u2Name = null;
            decimal u2Factor = 1m, u3Factor = 1m;
            if (dtProdInfo.Rows.Count > 0)
            {
                var pr = dtProdInfo.Rows[0];
                baseUnit = pr["Unit"]?.ToString() ?? "";
                u1Name = pr["Unit1Name"]?.ToString();
                u2Name = pr["Unit2Name"]?.ToString();
                if (pr["Unit2Factor"] != DBNull.Value) decimal.TryParse(pr["Unit2Factor"].ToString(), out u2Factor);
                if (pr["Unit3Factor"] != DBNull.Value) decimal.TryParse(pr["Unit3Factor"].ToString(), out u3Factor);
                row.Cells["BaseUnitName"].Value = baseUnit;
                row.Cells["Unit1Name"].Value = u1Name;
                row.Cells["Unit1SalePrice"].Value = pr["Unit1SalePrice"]?.ToString();
                row.Cells["Unit2Name"].Value = u2Name;
                row.Cells["Unit2Factor"].Value = u2Factor;
                row.Cells["Unit2SalePrice"].Value = pr["Unit2SalePrice"]?.ToString();
                row.Cells["Unit3Factor"].Value = u3Factor;

                string pColor = pr.Table.Columns.Contains("Color") && pr["Color"] != DBNull.Value ? pr["Color"].ToString().Trim() : "";
                string pSize = pr.Table.Columns.Contains("ProductSize") && pr["ProductSize"] != DBNull.Value ? pr["ProductSize"].ToString().Trim() : "";
                if (dgItems.Columns.Contains("Color"))
                {
                    row.Cells["Color"].Value = pColor;
                    if (!string.IsNullOrEmpty(pColor)) dgItems.Columns["Color"].Visible = true;
                }
                if (dgItems.Columns.Contains("ProductSize"))
                {
                    row.Cells["ProductSize"].Value = pSize;
                    if (!string.IsNullOrEmpty(pSize)) dgItems.Columns["ProductSize"].Visible = true;
                }
            }

            var comboCell = (DataGridViewComboBoxCell)row.Cells["UnitName"];
            comboCell.Items.Clear();
            if (!string.IsNullOrEmpty(u1Name)) comboCell.Items.Add(u1Name);
            if (!string.IsNullOrEmpty(u2Name) && !comboCell.Items.Contains(u2Name)) comboCell.Items.Add(u2Name);
            if (!string.IsNullOrEmpty(baseUnit) && !comboCell.Items.Contains(baseUnit)) comboCell.Items.Add(baseUnit);

            if (comboCell.Items.Count > 0)
                comboCell.Value = comboCell.Items[0];

            row.Cells["ProductID"].Value       = ci.ID;
            row.Cells["ProductName"].Value     = ci.Text;
            row.Cells["SoldQty"].Value         = "عام";
            row.Cells["PrevReturnedQty"].Value = "0";
            row.Cells["CurrentStock"].Value    = actStock.ToString("G29");
            row.Cells["NewReturnedQty"].Value  = qty;
            if (row.Cells["OriginalUnitPrice"] != null) row.Cells["OriginalUnitPrice"].Value = price.ToString("N2");
            if (row.Cells["Discount"] != null) row.Cells["Discount"].Value = "-";
            row.Cells["UnitPrice"].Value       = price.ToString("N2");
            row.Cells["TotalPrice"].Value      = (qty * price).ToString("N2");

            RecalcTotals();
        }

        private void BtnAddNewGenItem_Click(object sender, EventArgs e)
        {
            if (!(cboNewExchangeProducts.SelectedItem is ComboItem ci) || ci.ID == 0) return;

            if (!decimal.TryParse(txtNewGenQty.Text, out decimal qty) || qty <= 0) qty = 1m;

            if (!decimal.TryParse(txtNewGenPrice.Text, out decimal price) || price < 0) price = ci.Extra;

            foreach (DataGridViewRow r in dgExchangeNewItems.Rows)
            {
                if (r.Cells["ProductID"].Value != null && Convert.ToInt32(r.Cells["ProductID"].Value) == ci.ID)
                {
                    decimal currentQty = 0m;
                    if (r.Cells["NewQty"].Value != null)
                        decimal.TryParse(r.Cells["NewQty"].Value.ToString(), out currentQty);

                    decimal newQty = currentQty + qty;
                    r.Cells["NewQty"].Value = newQty;
                    r.Cells["UnitPrice"].Value = price.ToString("N2");
                    r.Cells["TotalPrice"].Value = (newQty * price).ToString("N2");
                    RecalcTotals();
                    return;
                }
            }

            int idx = dgExchangeNewItems.Rows.Add();
            var row = dgExchangeNewItems.Rows[idx];
            row.Cells["ProductID"].Value   = ci.ID;
            row.Cells["ProductName"].Value = ci.Text;

            var dtExProd = DbHelper.Query("SELECT Color, ProductSize FROM Products WHERE ProductID = @id", DbHelper.P("@id", ci.ID));
            if (dtExProd.Rows.Count > 0)
            {
                string exColor = dtExProd.Rows[0]["Color"]?.ToString() ?? "";
                string exSize = dtExProd.Rows[0]["ProductSize"]?.ToString() ?? "";
                if (dgExchangeNewItems.Columns.Contains("Color"))
                {
                    row.Cells["Color"].Value = exColor;
                    if (!string.IsNullOrEmpty(exColor)) dgExchangeNewItems.Columns["Color"].Visible = true;
                }
                if (dgExchangeNewItems.Columns.Contains("ProductSize"))
                {
                    row.Cells["ProductSize"].Value = exSize;
                    if (!string.IsNullOrEmpty(exSize)) dgExchangeNewItems.Columns["ProductSize"].Visible = true;
                }
            }

            row.Cells["NewQty"].Value      = qty;
            row.Cells["UnitPrice"].Value   = price.ToString("N2");
            row.Cells["TotalPrice"].Value  = (qty * price).ToString("N2");

            RecalcTotals();
        }

        private void RecalcTotals()
        {
            decimal totalRet = 0m;
            foreach (DataGridViewRow r in dgItems.Rows)
            {
                decimal.TryParse(r.Cells["TotalPrice"].Value?.ToString(), out decimal t);
                totalRet += t;
            }

            decimal totalNew = 0m;
            foreach (DataGridViewRow r in dgExchangeNewItems.Rows)
            {
                decimal.TryParse(r.Cells["NewQty"].Value?.ToString(), out decimal q);
                decimal.TryParse(r.Cells["UnitPrice"].Value?.ToString(), out decimal p);
                decimal t = q * p;
                r.Cells["TotalPrice"].Value = t.ToString("N2");
                totalNew += t;
            }

            if (cboMode.SelectedIndex == 2)
            {
                decimal diff = totalNew - totalRet;
                lblExchangeSummary.Text = $"مرتجع: {totalRet:N2} | بديل: {totalNew:N2}";
                if (diff >= 0)
                {
                    lblTotal.Text = $"الصافي للدفع: {diff:N2} ج";
                    lblTotal.ForeColor = Color.FromArgb(22, 101, 52); // أخضر داكن واضح
                }
                else
                {
                    lblTotal.Text = $"الصافي للمسترجع: {Math.Abs(diff):N2} ج";
                    lblTotal.ForeColor = Color.FromArgb(185, 28, 28); // عنابي داكن واضح
                }
            }
            else
            {
                lblTotal.Text = $"إجمالي المرتجع: {totalRet:N2} ج";
                lblTotal.ForeColor = Color.FromArgb(185, 28, 28); // عنابي داكن عالي التباين
            }
        }

        private DataGridView MakeGrid()
        {
            var dg = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(40, 50, 70), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = { Height = 28 },
                AllowUserToResizeRows = false,
                EnableHeadersVisualStyles = false
            };
            return dg;
        }

        private decimal GetProductActualStock(int productID, int? warehouseID)
        {
            if (productID <= 0) return 0m;
            try
            {
                return InventoryDAL.GetProductStock(productID, warehouseID);
            }
            catch { }
            return 0m;
        }

        private void CboEmployeeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboMode != null && cboMode.SelectedIndex == 0)
                LoadSales();
        }

        private void CboProductFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboMode != null && cboMode.SelectedIndex == 0)
                LoadSales();
        }

        private void CboSaleTypeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboMode != null && cboMode.SelectedIndex == 0)
                LoadSales();
        }

        private void LoadSales()
        {
            if (!this.IsHandleCreated) return;
            if (_isLoadingSales || _isFilteringCombo) return;
            if (cboMode != null && cboMode.SelectedIndex != 0) return;
            try
            {
                _isLoadingSales = true;
                int? clientID = null;
                if (cboClient != null && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
                    clientID = ci.ID;

                int? warehouseID = (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? (int?)cw.ID : null;

                string productSearch = null;
                if (txtSearch != null && !string.IsNullOrWhiteSpace(txtSearch.Text))
                    productSearch = txtSearch.Text.Trim();
                else if (cboProductFilter != null && cboProductFilter.SelectedItem is ComboItem pci && pci.ID > 0)
                    productSearch = pci.Text;
                else if (cboProductFilter != null && !string.IsNullOrWhiteSpace(cboProductFilter.Text) && cboProductFilter.Text.Trim() != "الكل" && cboProductFilter.Text.Trim() != "الكل (جميع الأصناف)")
                    productSearch = cboProductFilter.Text.Trim();

                string saleType = null;
                if (cboSaleTypeFilter != null && cboSaleTypeFilter.SelectedIndex > 0)
                {
                    string sel = cboSaleTypeFilter.SelectedItem.ToString();
                    if (sel.Contains("Cash") || sel.Contains("نقدي")) saleType = "Cash";
                    else if (sel.Contains("Visa") || sel.Contains("فيزا")) saleType = "Visa";
                    else if (sel.Contains("Credit") || sel.Contains("آجل")) saleType = "Credit";
                    else if (sel.Contains("Installment") || sel.Contains("تقسيط")) saleType = "Installment";
                    else if (sel.Contains("DriverLoad") || sel.Contains("حمولة") || sel.Contains("تحميل")) saleType = "DriverLoad";
                }

                bool canReturnAll = Session.IsAdmin || Session.CanReturnAllSales();
                int? empID = null;
                if (!canReturnAll)
                {
                    empID = Session.EmpID;
                }
                else
                {
                    if (cboEmployeeFilter != null && cboEmployeeFilter.SelectedItem is ComboItem cei && cei.ID > 0)
                        empID = cei.ID;
                }

                _salesDt = SaleDAL.GetAll(dtpFrom.Value, dtpTo.Value, clientID, productSearch, warehouseID, saleType, empID);
                dgSales.DataSource = _salesDt;
            }
            catch (Exception ex)
            {
                AppLogger.Error("خطأ أثناء تحميل فواتير البيع للمرتجع", ex, "FrmReturn.LoadSales");
            }
            finally
            {
                _isLoadingSales = false;
            }
        }

        private void DgSales_SelectionChanged(object sender, EventArgs e)
        {
            dgItems.Rows.Clear();
            if (dgItems.Columns.Contains("Color")) dgItems.Columns["Color"].Visible = AppConfig.IsClothing;
            if (dgItems.Columns.Contains("ProductSize")) dgItems.Columns["ProductSize"].Visible = AppConfig.IsClothing;
            lblTotal.Text = "الإجمالي: 0.00 ج";
            _selectedSaleTotalAmount = 0m;
            _selectedSaleShippingCharge = 0m;
            _selectedSalePrevReturnedAmount = 0m;

            if (dgSales.CurrentRow == null || dgSales.CurrentRow.Cells["SaleID"].Value == null)
                return;

            _isLoadingSaleItems = true;
            try
            {
                int saleID = Convert.ToInt32(dgSales.CurrentRow.Cells["SaleID"].Value);
            
            // ضبط نوع دفع المرتجع تلقائياً بناءً على نوع الفاتورة الأصلية
            if (dgSales.CurrentRow.Cells["SaleType"] != null && dgSales.CurrentRow.Cells["SaleType"].Value != null)
            {
                string origSaleType = dgSales.CurrentRow.Cells["SaleType"].Value.ToString();
                if (origSaleType == "Visa")
                    cboReturnType.SelectedIndex = 1; // فيزا
                else if (origSaleType == "Credit")
                    cboReturnType.SelectedIndex = 2; // آجل
                else
                    cboReturnType.SelectedIndex = 0; // كاش نقدي
            }

            var dtSaleInfo = DbHelper.Query("SELECT ISNULL(TotalAmount,0) AS TotalAmount, ISNULL(ShippingCharge,0) AS ShippingCharge, ISNULL(DiscountAmount,0) AS DiscountAmount, ISNULL(DiscountPct,0) AS DiscountPct FROM Sales WHERE SaleID = @id", DbHelper.P("@id", saleID));
            decimal saleDiscountAmount = 0m;
            decimal saleDiscountPct = 0m;
            if (dtSaleInfo.Rows.Count > 0)
            {
                _selectedSaleTotalAmount = Convert.ToDecimal(dtSaleInfo.Rows[0]["TotalAmount"]);
                _selectedSaleShippingCharge = Convert.ToDecimal(dtSaleInfo.Rows[0]["ShippingCharge"]);
                saleDiscountAmount = Convert.ToDecimal(dtSaleInfo.Rows[0]["DiscountAmount"]);
                saleDiscountPct = Convert.ToDecimal(dtSaleInfo.Rows[0]["DiscountPct"]);
            }

            var dtPrevRetInfo = DbHelper.Query("SELECT ISNULL(SUM(TotalAmount),0) AS PrevReturned FROM SalesReturns WHERE SaleID = @id", DbHelper.P("@id", saleID));
            if (dtPrevRetInfo.Rows.Count > 0)
            {
                _selectedSalePrevReturnedAmount = Convert.ToDecimal(dtPrevRetInfo.Rows[0]["PrevReturned"]);
            }

            DataTable dtItems = SaleDAL.GetItems(saleID);

            // 1. حساب إجمالي بنود الفاتورة قبل الخصم العام للفاتورة
            decimal sumLinesTotal = 0m;
            foreach (DataRow r in dtItems.Rows)
            {
                decimal q = dtItems.Columns.Contains("Quantity") && r["Quantity"] != DBNull.Value ? Convert.ToDecimal(r["Quantity"]) : 0m;
                decimal up = dtItems.Columns.Contains("UnitPrice") && r["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(r["UnitPrice"]) : 0m;
                decimal tp = dtItems.Columns.Contains("TotalPrice") && r["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(r["TotalPrice"]) : (q * up);
                if (tp <= 0m && q > 0m && up > 0m) tp = q * up;
                sumLinesTotal += tp;
            }

            // 2. صافي قيمة الأصناف بالفاتورة (بعد خصم الفاتورة واستبعاد الشحن)
            decimal invoiceNetProducts = Math.Max(0m, _selectedSaleTotalAmount - _selectedSaleShippingCharge);

            // 3. نسبة الخصم العام للفاتورة لتوزيعه على الأصناف بالتناسب
            decimal invoiceDiscountRatio = 1.0m;
            if (sumLinesTotal > 0m && invoiceNetProducts < (sumLinesTotal - 0.001m))
            {
                invoiceDiscountRatio = invoiceNetProducts / sumLinesTotal;
            }

            // إشعار أو تنبيه مرئي بوجود خصم على الفاتورة وتوزيعه
            if (cboMode != null && cboMode.SelectedIndex == 0)
            {
                decimal diffDiscount = sumLinesTotal - invoiceNetProducts;
                if (diffDiscount > 0.005m || saleDiscountAmount > 0m || saleDiscountPct > 0m)
                {
                    decimal dispDisc = diffDiscount > 0.005m ? diffDiscount : saleDiscountAmount;
                    lblExchangeSummary.Text = $"🏷️ خصم الفاتورة: {dispDisc:N2} ج (تم توزيع الخصم واحتساب صافي سعر المرتجع لكل صنف تلقائياً)";
                    lblExchangeSummary.ForeColor = Color.FromArgb(180, 83, 9);
                    lblExchangeSummary.Visible = true;
                }
                else
                {
                    lblExchangeSummary.Visible = false;
                }
            }

            foreach (DataRow row in dtItems.Rows)
            {
                int rowIndex = dgItems.Rows.Add();
                var dgRow = dgItems.Rows[rowIndex];

                dgRow.Cells["ProductID"].Value = row["ProductID"];
                dgRow.Cells["ProductName"].Value = row["ProductName"];

                string pColor = row.Table.Columns.Contains("Color") && row["Color"] != DBNull.Value ? row["Color"].ToString().Trim() : "";
                string pSize = row.Table.Columns.Contains("ProductSize") && row["ProductSize"] != DBNull.Value ? row["ProductSize"].ToString().Trim() : "";

                if (dgItems.Columns.Contains("Color"))
                {
                    dgRow.Cells["Color"].Value = pColor;
                    if (!string.IsNullOrEmpty(pColor)) dgItems.Columns["Color"].Visible = true;
                }
                if (dgItems.Columns.Contains("ProductSize"))
                {
                    dgRow.Cells["ProductSize"].Value = pSize;
                    if (!string.IsNullOrEmpty(pSize)) dgItems.Columns["ProductSize"].Visible = true;
                }

                decimal soldQty = dtItems.Columns.Contains("SoldQty") ? Convert.ToDecimal(row["SoldQty"]) : (dtItems.Columns.Contains("Quantity") ? Convert.ToDecimal(row["Quantity"]) : 0m);
                decimal prevRetQty = dtItems.Columns.Contains("PrevReturnedQty") ? Convert.ToDecimal(row["PrevReturnedQty"]) : 0m;
                decimal origUnitPrice = dtItems.Columns.Contains("UnitPrice") ? Convert.ToDecimal(row["UnitPrice"]) : 0m;
                decimal lineTotalPrice = dtItems.Columns.Contains("TotalPrice") && row["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(row["TotalPrice"]) : (soldQty * origUnitPrice);
                if (lineTotalPrice <= 0m && soldQty > 0m && origUnitPrice > 0m) lineTotalPrice = soldQty * origUnitPrice;

                // صافي قيمة السطر بعد توزيع الخصم التناسبي للفاتورة
                decimal lineNetPaid = Math.Round(lineTotalPrice * invoiceDiscountRatio, 2);

                // صافي سعر الوحدة المرتجعة
                decimal netUnitPrice = soldQty > 0m ? Math.Round(lineNetPaid / soldQty, 2) : origUnitPrice;
                if (netUnitPrice < 0m) netUnitPrice = 0m;

                string baseUnit = dtItems.Columns.Contains("BaseUnitName") ? row["BaseUnitName"]?.ToString() ?? "" : (dtItems.Columns.Contains("Unit") ? row["Unit"]?.ToString() ?? "" : "");
                string u1Name = dtItems.Columns.Contains("Unit1Name") ? row["Unit1Name"]?.ToString() : null;
                string u1PriceObj = dtItems.Columns.Contains("Unit1SalePrice") ? row["Unit1SalePrice"]?.ToString() : null;
                string u2Name = dtItems.Columns.Contains("Unit2Name") ? row["Unit2Name"]?.ToString() : null;
                string u2FactorObj = dtItems.Columns.Contains("Unit2Factor") ? row["Unit2Factor"]?.ToString() : null;
                string u2PriceObj = dtItems.Columns.Contains("Unit2SalePrice") ? row["Unit2SalePrice"]?.ToString() : null;
                string u3FactorObj = dtItems.Columns.Contains("Unit3Factor") ? row["Unit3Factor"]?.ToString() : null;

                decimal u2Factor = 1m;
                if (!string.IsNullOrEmpty(u2FactorObj) && decimal.TryParse(u2FactorObj, out decimal parsedU2) && parsedU2 > 0)
                    u2Factor = parsedU2;

                decimal u3Factor = 1m;
                if (!string.IsNullOrEmpty(u3FactorObj) && decimal.TryParse(u3FactorObj, out decimal parsedU3) && parsedU3 > 0)
                    u3Factor = parsedU3;

                string invoiceUnitName = row["UnitName"]?.ToString();
                if (string.IsNullOrEmpty(invoiceUnitName))
                {
                    invoiceUnitName = !string.IsNullOrEmpty(u1Name) ? u1Name : baseUnit;
                }

                decimal invoiceFactor = 1m;
                if (!string.IsNullOrEmpty(u2Name) && invoiceUnitName == u2Name)
                {
                    invoiceFactor = u2Factor;
                }
                else if (!string.IsNullOrEmpty(baseUnit) && invoiceUnitName == baseUnit)
                {
                    invoiceFactor = u2Factor * u3Factor;
                }

                decimal soldQtyInSmallest = soldQty * invoiceFactor;
                decimal prevQtyInSmallest = prevRetQty * invoiceFactor;

                dgRow.Cells["OriginalFactor"].Value = invoiceFactor;
                dgRow.Cells["OriginalUnitPrice"].Value = origUnitPrice.ToString("N2");

                decimal itemDiscPct = dtItems.Columns.Contains("DiscountPct") && row["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(row["DiscountPct"]) : 0m;
                decimal itemDiscAmt = dtItems.Columns.Contains("DiscountAmt") && row["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmt"]) : 0m;

                decimal unitDiscount = Math.Max(0m, origUnitPrice - netUnitPrice);
                string discText = "-";
                if (unitDiscount > 0.005m)
                {
                    if (itemDiscPct > 0m && Math.Abs(unitDiscount - (origUnitPrice * itemDiscPct / 100m)) < 0.01m)
                    {
                        discText = $"{itemDiscPct:0.##}% ({unitDiscount:N2} ج)";
                    }
                    else
                    {
                        decimal effPct = origUnitPrice > 0m ? Math.Round((unitDiscount / origUnitPrice) * 100m, 1) : 0m;
                        if (effPct > 0m && effPct < 100m)
                            discText = $"{unitDiscount:N2} ج ({effPct:0.#}%)";
                        else
                            discText = $"{unitDiscount:N2} ج";
                    }
                }
                else if (itemDiscPct > 0m)
                {
                    discText = $"{itemDiscPct:0.##}%";
                }
                else if (itemDiscAmt > 0m)
                {
                    discText = $"{itemDiscAmt:N2} ج";
                }

                dgRow.Cells["Discount"].Value = discText;
                dgRow.Cells["NetLineTotal"].Value = lineNetPaid;
                dgRow.Cells["NetUnitPrice"].Value = netUnitPrice;
                dgRow.Cells["SoldQtyInSmallest"].Value = soldQtyInSmallest;
                dgRow.Cells["PrevReturnedQtyInSmallest"].Value = prevQtyInSmallest;
                dgRow.Cells["BaseUnitName"].Value = baseUnit;
                dgRow.Cells["Unit1Name"].Value = u1Name;
                dgRow.Cells["Unit1SalePrice"].Value = u1PriceObj;
                dgRow.Cells["Unit2Name"].Value = u2Name;
                dgRow.Cells["Unit2Factor"].Value = u2Factor;
                dgRow.Cells["Unit2SalePrice"].Value = u2PriceObj;
                dgRow.Cells["Unit3Factor"].Value = u3Factor;

                var comboCell = (DataGridViewComboBoxCell)dgRow.Cells["UnitName"];
                comboCell.Items.Clear();

                if (!string.IsNullOrEmpty(u1Name)) comboCell.Items.Add(u1Name);
                if (!string.IsNullOrEmpty(u2Name) && !comboCell.Items.Contains(u2Name)) comboCell.Items.Add(u2Name);
                if (!string.IsNullOrEmpty(baseUnit) && !comboCell.Items.Contains(baseUnit)) comboCell.Items.Add(baseUnit);

                if (comboCell.Items.Contains(invoiceUnitName))
                    comboCell.Value = invoiceUnitName;
                int pid = Convert.ToInt32(row["ProductID"]);
                int? selectedWh = (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem cw2 && cw2.ID > 0) ? (int?)cw2.ID : null;
                decimal actStock = GetProductActualStock(pid, selectedWh);

                dgRow.Cells["SoldQty"].Value = soldQty.ToString("G29");
                dgRow.Cells["PrevReturnedQty"].Value = prevRetQty.ToString("G29");
                dgRow.Cells["CurrentStock"].Value = actStock.ToString("G29");
                dgRow.Cells["NewReturnedQty"].Value = 0m;
                dgRow.Cells["UnitPrice"].Value = netUnitPrice.ToString("F2");
                dgRow.Cells["TotalPrice"].Value = "0.00";

                if (prevRetQty > 0)
                {
                    dgRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242); // خلفية هادئة للصفوف ذات المرتجع السابق
                    dgRow.DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27);
                    dgRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 38, 38);
                    dgRow.DefaultCellStyle.SelectionForeColor = Color.White;
                }
            }
            }
            finally
            {
                _isLoadingSaleItems = false;
            }
        }

        private void DgItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = dgItems.Columns[e.ColumnIndex].Name;

            if (colName == "NewReturnedQty")
            {
                if (string.IsNullOrWhiteSpace(e.FormattedValue?.ToString())) return;

                if (!decimal.TryParse(e.FormattedValue.ToString(), out decimal newQty) || newQty < 0)
                {
                    MessageBox.Show("يرجى إدخال كمية صحيحة أكبر من أو تساوي الصفر.", "إدخال غير صحيح", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                if (cboMode.SelectedIndex != 0) return; // للمرتجع العام لا نشترط الفاتورة

                var row = dgItems.Rows[e.RowIndex];
                string selectedUnit = row.Cells["UnitName"].Value?.ToString();
                string baseUnit = row.Cells["BaseUnitName"].Value?.ToString() ?? "";
                string u1Name = row.Cells["Unit1Name"].Value?.ToString();
                string u2Name = row.Cells["Unit2Name"].Value?.ToString();

                decimal selectedFactor = 1m;
                if (!string.IsNullOrEmpty(u2Name) && selectedUnit == u2Name)
                {
                    decimal u2Factor = Convert.ToDecimal(row.Cells["Unit2Factor"].Value);
                    selectedFactor = u2Factor > 0 ? u2Factor : 1m;
                }
                else if (!string.IsNullOrEmpty(u1Name) && selectedUnit == u1Name)
                {
                    selectedFactor = 1m;
                }
                else if (!string.IsNullOrEmpty(baseUnit) && selectedUnit == baseUnit)
                {
                    decimal u2Factor = Convert.ToDecimal(row.Cells["Unit2Factor"].Value);
                    decimal u3Factor = Convert.ToDecimal(row.Cells["Unit3Factor"].Value);
                    selectedFactor = (u3Factor > 0 ? u3Factor : 1m) * (u2Factor > 0 ? u2Factor : 1m);
                }

                decimal soldQtyInSmallest = Convert.ToDecimal(row.Cells["SoldQtyInSmallest"].Value);
                decimal prevQtyInSmallest = Convert.ToDecimal(row.Cells["PrevReturnedQtyInSmallest"].Value);
                decimal newQtyInSmallest = newQty * selectedFactor;

                if (newQtyInSmallest + prevQtyInSmallest > soldQtyInSmallest)
                {
                    decimal maxAllowedInSmallest = soldQtyInSmallest - prevQtyInSmallest;
                    decimal maxAllowedInSelected = maxAllowedInSmallest / selectedFactor;
                    MessageBox.Show($"الكمية المرتجعة الجديدة ({newQty} {selectedUnit}) تتجاوز الكمية الأصلية بالفاتورة.\nالحد الأقصى المسموح به: {maxAllowedInSelected:N3} {selectedUnit}", "تجاوز الكمية المتاحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _isLoadingSaleItems) return;
            var row = dgItems.Rows[e.RowIndex];
            var colName = dgItems.Columns[e.ColumnIndex].Name;

            if (colName == "NewReturnedQty" || colName == "UnitPrice")
            {
                decimal newQty = 0;
                if (row.Cells["NewReturnedQty"].Value != null)
                    decimal.TryParse(row.Cells["NewReturnedQty"].Value.ToString(), out newQty);

                decimal price = 0;
                if (row.Cells["UnitPrice"].Value != null)
                    decimal.TryParse(row.Cells["UnitPrice"].Value.ToString(), out price);

                if (colName == "UnitPrice")
                {
                    // تنسيق السعر الجديد
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (e.RowIndex < dgItems.Rows.Count)
                                dgItems.Rows[e.RowIndex].Cells["UnitPrice"].Value = price.ToString("F2");
                        });
                    }
                }

                decimal rowTotal = Math.Round(newQty * price, 2);

                // معالجة فرق كسور القروش في حالة إرجاع كامل الكمية المتبقية للصنف من الفاتورة
                if (cboMode != null && cboMode.SelectedIndex == 0 && newQty > 0)
                {
                    decimal soldQty = 0m;
                    decimal prevRetQty = 0m;
                    decimal netLineTotal = 0m;
                    decimal netUnitPrice = 0m;

                    if (row.Cells["SoldQty"].Value != null) decimal.TryParse(row.Cells["SoldQty"].Value.ToString(), out soldQty);
                    if (row.Cells["PrevReturnedQty"].Value != null) decimal.TryParse(row.Cells["PrevReturnedQty"].Value.ToString(), out prevRetQty);
                    if (row.Cells["NetLineTotal"].Value != null) decimal.TryParse(row.Cells["NetLineTotal"].Value.ToString(), out netLineTotal);
                    if (row.Cells["NetUnitPrice"].Value != null) decimal.TryParse(row.Cells["NetUnitPrice"].Value.ToString(), out netUnitPrice);

                    decimal maxAvailQty = Math.Max(0m, soldQty - prevRetQty);

                    // إذا كان السعر هو سعر الصافي المحسوب وتم طلب كامل الكمية المتبقية
                    if (Math.Abs(price - netUnitPrice) <= 0.01m && newQty == maxAvailQty && maxAvailQty > 0)
                    {
                        decimal prevNetUsed = prevRetQty > 0 ? Math.Round(prevRetQty * netUnitPrice, 2) : 0m;
                        decimal remainingLineNet = Math.Max(0m, netLineTotal - prevNetUsed);
                        if (remainingLineNet > 0m)
                        {
                            rowTotal = remainingLineNet;
                        }
                    }
                }

                row.Cells["TotalPrice"].Value = rowTotal.ToString("F2");
                RecalcTotals();
            }
            else if (colName == "UnitName")
            {
                string selectedUnit = row.Cells["UnitName"].Value?.ToString();
                string u1Name = row.Cells["Unit1Name"]?.Value?.ToString();
                string u2Name = row.Cells["Unit2Name"]?.Value?.ToString();

                decimal u1Price = 0m, u2Price = 0m;
                if (row.Cells["Unit1SalePrice"]?.Value != null) decimal.TryParse(row.Cells["Unit1SalePrice"].Value.ToString(), out u1Price);
                if (row.Cells["Unit2SalePrice"]?.Value != null) decimal.TryParse(row.Cells["Unit2SalePrice"].Value.ToString(), out u2Price);

                decimal autoPrice = 0m;
                if (!string.IsNullOrEmpty(u2Name) && selectedUnit == u2Name && u2Price > 0)
                    autoPrice = u2Price;
                else if (!string.IsNullOrEmpty(u1Name) && selectedUnit == u1Name && u1Price > 0)
                    autoPrice = u1Price;

                if (autoPrice > 0)
                {
                    row.Cells["UnitPrice"].Value = autoPrice.ToString("F2");
                }

                decimal.TryParse(row.Cells["NewReturnedQty"]?.Value?.ToString(), out decimal newQty);
                decimal.TryParse(row.Cells["UnitPrice"]?.Value?.ToString(), out decimal price);
                row.Cells["TotalPrice"].Value = (newQty * price).ToString("F2");
                RecalcTotals();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("Returns")) 
            { 
                MessageBox.Show("⛔ ليس لديك صلاحية حفظ مرتجعات المبيعات.\nيرجى مراجعة إدارة النظام لتفعيل صلاحية حفظ المرتجع لحسابك.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
                return; 
            }

            bool canReturnAll = Session.IsAdmin || Session.CanReturnAllSales();
            int mode = cboMode.SelectedIndex;
            int? warehouseID = (cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? (int?)cw.ID : Session.GetDefaultWarehouseID();
            
            string returnType = "Cash";
            if (cboReturnType.SelectedIndex == 1 || cboReturnType.Text.Contains("Visa") || cboReturnType.Text.Contains("فيزا"))
                returnType = "Visa";
            else if (cboReturnType.SelectedIndex == 2 || cboReturnType.Text.Contains("Credit") || cboReturnType.Text.Contains("آجل"))
                returnType = "Credit";
            else
                returnType = "Cash";

            int? clientID = (cboClient.SelectedItem is ComboItem cc && cc.ID > 0) ? (int?)cc.ID : null;
            int? shiftID = Session.CurrentShiftID;
            if (!shiftID.HasValue || shiftID.Value <= 0)
            {
                try { shiftID = ShiftDAL.GetActiveShiftID(); } catch { }
            }

            if (mode == 0) // مرتجع فاتورة معينة
            {
                if (dgSales.CurrentRow == null || dgSales.CurrentRow.Cells["SaleID"].Value == null)
                {
                    MessageBox.Show("يرجى اختيار الفاتورة المراد الإرجاع منها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!canReturnAll)
                {
                    if (dgSales.CurrentRow.Cells["CreatedBy"] != null && dgSales.CurrentRow.Cells["CreatedBy"].Value != null && dgSales.CurrentRow.Cells["CreatedBy"].Value != DBNull.Value)
                    {
                        int creatorId = Convert.ToInt32(dgSales.CurrentRow.Cells["CreatedBy"].Value);
                        if (creatorId > 0 && creatorId != Session.EmpID)
                        {
                            MessageBox.Show("⛔ غير مصرح لك بعمل مرتجع لفواتير الموظفين الآخرين.\nصلاحية حسابك مقصورة على عمل مرتجع لمبيعاتك الشخصية فقط.", "صلاحية محددة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }

                int saleID = Convert.ToInt32(dgSales.CurrentRow.Cells["SaleID"].Value);
                var returnItems = new List<SaleItemDTO>();
                decimal totalReturnAmount = 0;

                foreach (DataGridViewRow row in dgItems.Rows)
                {
                    int prodID = Convert.ToInt32(row.Cells["ProductID"].Value);
                    string prodName = row.Cells["ProductName"].Value.ToString();
                    decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal newQty);

                    if (newQty > 0)
                    {
                        decimal price = Convert.ToDecimal(row.Cells["UnitPrice"].Value);
                        decimal rowTotal = Math.Round(newQty * price, 2);
                        if (row.Cells["TotalPrice"].Value != null && decimal.TryParse(row.Cells["TotalPrice"].Value.ToString(), out decimal parsedTotal) && parsedTotal > 0)
                        {
                            rowTotal = parsedTotal;
                        }
                        decimal effectiveUnitPrice = newQty > 0 ? Math.Round(rowTotal / newQty, 2) : price;

                        returnItems.Add(new SaleItemDTO 
                        { 
                            ProductID = prodID, 
                            ProductName = prodName, 
                            Quantity = newQty, 
                            UnitPrice = effectiveUnitPrice,
                            TotalPrice = rowTotal,
                            UnitName = row.Cells["UnitName"].Value?.ToString(),
                            Factor = 1m,
                            Color = row.Cells["Color"]?.Value?.ToString() ?? "",
                            ProductSize = row.Cells["ProductSize"]?.Value?.ToString() ?? ""
                        });
                        totalReturnAmount += rowTotal;
                    }
                }

                if (returnItems.Count == 0)
                {
                    MessageBox.Show("يرجى إدخال كمية مرتجعة جديدة صالحة لصنف واحد على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_selectedSaleTotalAmount > 0)
                {
                    decimal maxInvoiceRefundable = Math.Max(0m, _selectedSaleTotalAmount - _selectedSalePrevReturnedAmount);
                    if (totalReturnAmount > maxInvoiceRefundable + 0.05m)
                    {
                        MessageBox.Show($"⛔ إجمالي قيمة المرتجع ({totalReturnAmount:N2} ج) يتجاوز القيمة المتبقية من الفاتورة الأصلية بعد الخصومات ({maxInvoiceRefundable:N2} ج).", "تجاوز قيمة الفاتورة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    else if (totalReturnAmount > maxInvoiceRefundable)
                    {
                        totalReturnAmount = maxInvoiceRefundable;
                    }
                }

                try
                {
                    int id = ReturnDAL.SaveReturn(saleID, clientID, totalReturnAmount, txtNotes.Text, returnItems, warehouseID, returnType, shiftID);
                    if (id > 0) 
                    { 
                        _lastSavedReturnID = id;
                        FrmPrintChoiceDialog.PromptAndPrintReturn(this, id, "✅ تم حفظ مرتجع البيع بنجاح!");
                        txtNotes.Text = "";
                        LoadSales();
                    }
                    else 
                    {
                        MessageBox.Show("فشل حفظ المرتجع", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("فشل حفظ مرتجع المبيعات", ex, "FrmReturn.BtnSave_Click");
                    MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (mode == 1) // مرتجع بيع عام
            {
                if (returnType == "Credit" && !clientID.HasValue)
                {
                    MessageBox.Show("يرجى اختيار العميل أولاً لمرتجع البيع العام الآجل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var returnItems = new List<SaleItemDTO>();
                decimal totalReturnAmount = 0;

                foreach (DataGridViewRow row in dgItems.Rows)
                {
                    int prodID = Convert.ToInt32(row.Cells["ProductID"].Value);
                    string prodName = row.Cells["ProductName"].Value.ToString();
                    decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal newQty);
                    decimal.TryParse(row.Cells["UnitPrice"].Value?.ToString(), out decimal price);

                    if (newQty > 0)
                    {
                        string selectedUnit = row.Cells["UnitName"].Value?.ToString();
                        string u1Name = row.Cells["Unit1Name"]?.Value?.ToString();
                        string u2Name = row.Cells["Unit2Name"]?.Value?.ToString();
                        string baseUnit = row.Cells["BaseUnitName"]?.Value?.ToString();

                        decimal factor = 1m;
                        if (!string.IsNullOrEmpty(u2Name) && selectedUnit == u2Name)
                        {
                            decimal u2Factor = row.Cells["Unit2Factor"]?.Value != null ? Convert.ToDecimal(row.Cells["Unit2Factor"].Value) : 1m;
                            factor = u2Factor > 0 ? u2Factor : 1m;
                        }
                        else if (!string.IsNullOrEmpty(baseUnit) && selectedUnit == baseUnit)
                        {
                            decimal u2Factor = row.Cells["Unit2Factor"]?.Value != null ? Convert.ToDecimal(row.Cells["Unit2Factor"].Value) : 1m;
                            decimal u3Factor = row.Cells["Unit3Factor"]?.Value != null ? Convert.ToDecimal(row.Cells["Unit3Factor"].Value) : 1m;
                            factor = (u2Factor > 0 ? u2Factor : 1m) * (u3Factor > 0 ? u3Factor : 1m);
                        }

                        returnItems.Add(new SaleItemDTO 
                        { 
                            ProductID = prodID, 
                            ProductName = prodName, 
                            Quantity = newQty, 
                            UnitPrice = price,
                            UnitName = selectedUnit,
                            Factor = factor,
                            Color = row.Cells["Color"]?.Value?.ToString() ?? "",
                            ProductSize = row.Cells["ProductSize"]?.Value?.ToString() ?? ""
                        });
                        totalReturnAmount += (newQty * price);
                    }
                }

                if (returnItems.Count == 0)
                {
                    MessageBox.Show("أضف صنفاً مرتجعاً واحداً على الأقل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    int id = ReturnDAL.SaveReturn(0, clientID, totalReturnAmount, txtNotes.Text, returnItems, warehouseID, returnType, shiftID);
                    if (id > 0)
                    {
                        _lastSavedReturnID = id;
                        FrmPrintChoiceDialog.PromptAndPrintReturn(this, id, "✅ تم حفظ مرتجع البيع العام بنجاح!");
                        txtNotes.Text = "";
                        dgItems.Rows.Clear();
                        RecalcTotals();
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("فشل حفظ مرتجع البيع العام", ex, "FrmReturn.BtnSave_Click");
                    MessageBox.Show($"❌ حدث خطأ:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (mode == 2) // استبدال أصناف
            {
                if (returnType == "Credit" && !clientID.HasValue)
                {
                    MessageBox.Show("يرجى اختيار العميل أولاً لعملية الاستبدال الآجلة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var retItems = new List<SaleItemDTO>();
                foreach (DataGridViewRow r in dgItems.Rows)
                {
                    int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
                    string name = r.Cells["ProductName"].Value.ToString();
                    decimal.TryParse(r.Cells["NewReturnedQty"].Value?.ToString(), out decimal q);
                    decimal.TryParse(r.Cells["UnitPrice"].Value?.ToString(), out decimal p);
                    if (q > 0)
                    {
                        decimal rowTotal = Math.Round(q * p, 2);
                        if (r.Cells["TotalPrice"].Value != null && decimal.TryParse(r.Cells["TotalPrice"].Value.ToString(), out decimal parsedTotal) && parsedTotal > 0)
                        {
                            rowTotal = parsedTotal;
                        }
                        decimal effectiveUnitPrice = q > 0 ? Math.Round(rowTotal / q, 2) : p;

                        retItems.Add(new SaleItemDTO 
                        { 
                            ProductID = pid, 
                            ProductName = name, 
                            Quantity = q, 
                            UnitPrice = effectiveUnitPrice,
                            TotalPrice = rowTotal,
                            Color = r.Cells["Color"]?.Value?.ToString() ?? "",
                            ProductSize = r.Cells["ProductSize"]?.Value?.ToString() ?? ""
                        });
                    }
                }

                var newItems = new List<SaleItemDTO>();
                foreach (DataGridViewRow r in dgExchangeNewItems.Rows)
                {
                    int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
                    string name = r.Cells["ProductName"].Value.ToString();
                    decimal.TryParse(r.Cells["NewQty"].Value?.ToString(), out decimal q);
                    decimal.TryParse(r.Cells["UnitPrice"].Value?.ToString(), out decimal p);
                    if (q > 0) newItems.Add(new SaleItemDTO 
                    { 
                        ProductID = pid, 
                        ProductName = name, 
                        Quantity = q, 
                        UnitPrice = p,
                        Color = r.Cells["Color"]?.Value?.ToString() ?? "",
                        ProductSize = r.Cells["ProductSize"]?.Value?.ToString() ?? ""
                    });
                }

                if (retItems.Count == 0 || newItems.Count == 0)
                {
                    MessageBox.Show("يجب إضافة صنف مرتجع وصنف بديل جديد على الأقل لعملية الاستبدال!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    bool ok = ReturnDAL.SaveItemExchange(clientID, warehouseID.Value, retItems, newItems, returnType, txtNotes.Text, shiftID);
                    if (ok)
                    {
                        MessageBox.Show("✅ تم إنجاز عملية استبدال الأصناف وتصفية الفرق بنجاح!", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtNotes.Text = "";
                        dgItems.Rows.Clear();
                        dgExchangeNewItems.Rows.Clear();
                        RecalcTotals();
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("فشل عملية استبدال الأصناف", ex, "FrmReturn.BtnSave_Click");
                    MessageBox.Show($"❌ حدث خطأ أثناء الاستبدال:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DoBarcodeSearch(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            code = code.Trim();
            // إزالة علامات النجوم المحيطة بالباركود إن وجدت (مثل *INV-1001*)
            if (code.StartsWith("*") && code.EndsWith("*") && code.Length > 2)
                code = code.Substring(1, code.Length - 2).Trim();

            try
            {
                // 1. أولاً: التحقق مما إذا كان الكود هو رقم أو باركود فاتورة مبيعات
                var dt = DbHelper.Query(@"
                    SELECT s.SaleID, s.SaleCode, s.SaleDate 
                    FROM Sales s 
                    WHERE s.SaleCode = @code OR CAST(s.SaleID AS NVARCHAR(50)) = @code", 
                    DbHelper.P("@code", code));

                if (dt.Rows.Count > 0)
                {
                    int targetSaleID = Convert.ToInt32(dt.Rows[0]["SaleID"]);
                    DateTime saleDate = Convert.ToDateTime(dt.Rows[0]["SaleDate"]);

                    // التحويل التلقائي لنمط مرتجع من فاتورة
                    if (cboMode != null && cboMode.SelectedIndex != 0)
                    {
                        cboMode.SelectedIndex = 0;
                    }

                    // إذا كان تاريخ الفاتورة قبل تاريخ البداية، توسيع نطاق التاريخ تلقائياً ليشمل الفاتورة
                    if (saleDate < dtpFrom.Value)
                    {
                        dtpFrom.Value = saleDate.Date.AddDays(-1);
                    }
                    if (saleDate > dtpTo.Value)
                    {
                        dtpTo.Value = saleDate.Date.AddDays(1);
                    }

                    // إفراغ خانة البحث النصي مؤقتاً لضمان عدم حجب الفاتورة
                    if (txtSearch != null && txtSearch.Text.Trim() == code)
                    {
                        txtSearch.Text = "";
                    }

                    // تحميل الفواتير
                    LoadSales();

                    // البحث عن الفاتورة وتحديدها في الجدول
                    bool found = false;
                    dgSales.SelectionChanged -= DgSales_SelectionChanged;
                    foreach (DataGridViewRow row in dgSales.Rows)
                    {
                        if (row.Cells["SaleID"].Value != null && Convert.ToInt32(row.Cells["SaleID"].Value) == targetSaleID)
                        {
                            dgSales.CurrentCell = row.Cells["SaleCode"];
                            row.Selected = true;
                            found = true;
                            break;
                        }
                    }
                    dgSales.SelectionChanged += DgSales_SelectionChanged;

                    if (found)
                    {
                        DgSales_SelectionChanged(dgSales, EventArgs.Empty);
                        if (dgItems.Rows.Count > 0)
                        {
                            dgItems.Focus();
                            dgItems.CurrentCell = dgItems.Rows[0].Cells["NewReturnedQty"];
                        }
                        SoundAlertHelper.PlayBeep();
                    }
                    if (txtInvoiceBarcode != null) txtInvoiceBarcode.Text = "";
                    return;
                }

                // 2. إذا لم تكن فاتورة، فحص هل الكود يمثل صنفاً (باركود دولي / كود صنف / باركود ميزان)
                DataRow pRow = ProductDAL.GetByBarcodeOrScaleCode(code, out decimal scanQty);
                if (pRow == null)
                {
                    var dtProd = DbHelper.Query(@"
                        SELECT TOP 1 * FROM Products 
                        WHERE ProductCode = @code OR Barcode = @code OR CAST(ProductID AS NVARCHAR(50)) = @code", 
                        DbHelper.P("@code", code));
                    if (dtProd.Rows.Count > 0)
                    {
                        pRow = dtProd.Rows[0];
                        scanQty = 1m;
                    }
                }

                if (pRow != null)
                {
                    int pid = Convert.ToInt32(pRow["ProductID"]);
                    string pname = pRow["ProductName"].ToString();

                    // أ) إذا كنا في وضع "مرتجع من فاتورة" وتم تحديد فاتورة مسبقاً:
                    if (cboMode != null && cboMode.SelectedIndex == 0 && dgSales.CurrentRow != null)
                    {
                        bool itemFoundInInvoice = false;
                        foreach (DataGridViewRow r in dgItems.Rows)
                        {
                            if (r.Cells["ProductID"].Value != null && Convert.ToInt32(r.Cells["ProductID"].Value) == pid)
                            {
                                dgItems.Focus();
                                dgItems.CurrentCell = r.Cells["NewReturnedQty"];
                                r.Selected = true;
                                itemFoundInInvoice = true;
                                SoundAlertHelper.PlayBeep();
                                break;
                            }
                        }

                        if (!itemFoundInInvoice)
                        {
                            MessageBox.Show($"الصنف [{pname}] غير موجود ضمن بنود الفاتورة المحددة حالياً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        if (txtInvoiceBarcode != null) txtInvoiceBarcode.Text = "";
                        return;
                    }

                    // ب) إذا كنا في وضع "مرتجع من فاتورة" ولكن لم يتم اختيار فاتورة:
                    if (cboMode != null && cboMode.SelectedIndex == 0)
                    {
                        if (txtSearch != null)
                        {
                            txtSearch.Text = pname;
                        }
                        LoadSales();
                        if (dgSales.Rows.Count > 0)
                        {
                            dgSales.Focus();
                        }
                        else
                        {
                            MessageBox.Show($"لا توجد فواتير تحتوي على الصنف [{pname}] في الفترة المحددة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        if (txtInvoiceBarcode != null) txtInvoiceBarcode.Text = "";
                        return;
                    }

                    // ج) إذا كنا في وضع "مرتجع عام" أو "استبدال": إضافة الصنف مباشرةً للجدول
                    decimal price = pRow["SalePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["SalePrice"]) : 0m;
                    int mu = ProductDAL.DetermineMatchedUnit(pRow, code);
                    if (mu == 1 && pRow.Table.Columns.Contains("Unit1SalePrice") && pRow["Unit1SalePrice"] != DBNull.Value && Convert.ToDecimal(pRow["Unit1SalePrice"]) > 0)
                        price = Convert.ToDecimal(pRow["Unit1SalePrice"]);
                    else if (mu == 2 && pRow.Table.Columns.Contains("Unit2SalePrice") && pRow["Unit2SalePrice"] != DBNull.Value && Convert.ToDecimal(pRow["Unit2SalePrice"]) > 0)
                        price = Convert.ToDecimal(pRow["Unit2SalePrice"]);
                    decimal qty = scanQty > 0 ? scanQty : 1m;

                    foreach (DataGridViewRow r in dgItems.Rows)
                    {
                        if (r.Cells["ProductID"].Value != null && Convert.ToInt32(r.Cells["ProductID"].Value) == pid)
                        {
                            decimal currentQty = 0m;
                            if (r.Cells["NewReturnedQty"].Value != null)
                                decimal.TryParse(r.Cells["NewReturnedQty"].Value.ToString(), out currentQty);

                            decimal newQty = currentQty + qty;
                            r.Cells["NewReturnedQty"].Value = newQty;
                            r.Cells["UnitPrice"].Value = price.ToString("N2");
                            r.Cells["TotalPrice"].Value = (newQty * price).ToString("N2");
                            RecalcTotals();
                            SoundAlertHelper.PlayBeep();
                            if (txtInvoiceBarcode != null) txtInvoiceBarcode.Text = "";
                            return;
                        }
                    }

                    int idx = dgItems.Rows.Add();
                    var row = dgItems.Rows[idx];
                    int? selectedWh = (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem cw2 && cw2.ID > 0) ? (int?)cw2.ID : null;
                    decimal actStock = GetProductActualStock(pid, selectedWh);

                    row.Cells["ProductID"].Value       = pid;
                    row.Cells["ProductName"].Value     = pname;

                    if (dgItems.Columns.Contains("Color"))
                    {
                        string pColor = pRow.Table.Columns.Contains("Color") && pRow["Color"] != DBNull.Value ? pRow["Color"].ToString().Trim() : "";
                        row.Cells["Color"].Value = pColor;
                        if (!string.IsNullOrEmpty(pColor)) dgItems.Columns["Color"].Visible = true;
                    }
                    if (dgItems.Columns.Contains("ProductSize"))
                    {
                        string pSize = pRow.Table.Columns.Contains("ProductSize") && pRow["ProductSize"] != DBNull.Value ? pRow["ProductSize"].ToString().Trim() : "";
                        row.Cells["ProductSize"].Value = pSize;
                        if (!string.IsNullOrEmpty(pSize)) dgItems.Columns["ProductSize"].Visible = true;
                    }

                    row.Cells["SoldQty"].Value         = "عام";
                    row.Cells["PrevReturnedQty"].Value = "0";
                    row.Cells["CurrentStock"].Value    = actStock.ToString("G29");
                    row.Cells["NewReturnedQty"].Value  = qty;
                    if (row.Cells["OriginalUnitPrice"] != null) row.Cells["OriginalUnitPrice"].Value = price.ToString("N2");
                    if (row.Cells["Discount"] != null) row.Cells["Discount"].Value = "-";
                    row.Cells["UnitPrice"].Value       = price.ToString("N2");
                    row.Cells["TotalPrice"].Value      = (qty * price).ToString("N2");

                    RecalcTotals();
                    SoundAlertHelper.PlayBeep();
                    if (txtInvoiceBarcode != null) txtInvoiceBarcode.Text = "";
                    return;
                }

                // 3. إذا لم يتم العثور على فاتورة أو صنف
                MessageBox.Show($"عذراً، لم يتم العثور على أي فاتورة أو صنف يطابق الكود [{code}].", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppLogger.Error("خطأ أثناء معالجة باركود الفاتورة أو الصنف في المرتجع", ex, "FrmReturn.DoBarcodeSearch");
                MessageBox.Show($"حدث خطأ أثناء البحث:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenProductSearchDialog()
        {
            int? whId = (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? (int?)cw.ID : null;
            string lastSearchText = "";
            while (true)
            {
                using (var dlg = new FrmProductSearch(whId, false, initialSearchText: lastSearchText))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedProductID > 0)
                    {
                        lastSearchText = dlg.SearchText;
                    var pRow = ProductDAL.GetByID(dlg.SelectedProductID);
                    string pname = pRow != null ? pRow["ProductName"].ToString() : "صنف " + dlg.SelectedProductID;
                    decimal price = dlg.SelectedSalePrice > 0 ? dlg.SelectedSalePrice : (pRow != null && pRow["SalePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["SalePrice"]) : 0m);
                    decimal qty = dlg.SelectedQuantity > 0 ? dlg.SelectedQuantity : 1m;

                    foreach (DataGridViewRow r in dgItems.Rows)
                    {
                        if (r.Cells["ProductID"].Value != null && Convert.ToInt32(r.Cells["ProductID"].Value) == dlg.SelectedProductID)
                        {
                            decimal currentQty = 0m;
                            if (r.Cells["NewReturnedQty"].Value != null)
                                decimal.TryParse(r.Cells["NewReturnedQty"].Value.ToString(), out currentQty);

                            decimal newQty = currentQty + qty;
                            r.Cells["NewReturnedQty"].Value = newQty;
                            r.Cells["UnitPrice"].Value = price.ToString("N2");
                            r.Cells["TotalPrice"].Value = (newQty * price).ToString("N2");
                            RecalcTotals();
                            return;
                        }
                    }

                    int idx = dgItems.Rows.Add();
                    var row = dgItems.Rows[idx];
                    decimal actStock = GetProductActualStock(dlg.SelectedProductID, whId);

                    row.Cells["ProductID"].Value       = dlg.SelectedProductID;
                    row.Cells["ProductName"].Value     = pname;

                    var dtSrchProd = DbHelper.Query("SELECT Color, ProductSize FROM Products WHERE ProductID = @id", DbHelper.P("@id", dlg.SelectedProductID));
                    if (dtSrchProd.Rows.Count > 0)
                    {
                        string pColor = dtSrchProd.Rows[0]["Color"]?.ToString().Trim() ?? "";
                        string pSize = dtSrchProd.Rows[0]["ProductSize"]?.ToString().Trim() ?? "";
                        if (dgItems.Columns.Contains("Color"))
                        {
                            row.Cells["Color"].Value = pColor;
                            if (!string.IsNullOrEmpty(pColor)) dgItems.Columns["Color"].Visible = true;
                        }
                        if (dgItems.Columns.Contains("ProductSize"))
                        {
                            row.Cells["ProductSize"].Value = pSize;
                            if (!string.IsNullOrEmpty(pSize)) dgItems.Columns["ProductSize"].Visible = true;
                        }
                    }

                    row.Cells["SoldQty"].Value         = "عام";
                    row.Cells["PrevReturnedQty"].Value = "0";
                    row.Cells["CurrentStock"].Value    = actStock.ToString("G29");
                    row.Cells["NewReturnedQty"].Value  = qty;
                    if (row.Cells["OriginalUnitPrice"] != null) row.Cells["OriginalUnitPrice"].Value = price.ToString("N2");
                    if (row.Cells["Discount"] != null) row.Cells["Discount"].Value = "-";
                    row.Cells["UnitPrice"].Value       = price.ToString("N2");
                    row.Cells["TotalPrice"].Value      = (qty * price).ToString("N2");

                    RecalcTotals();
                    continue;
                }
                else
                {
                    break;
                }
            }
        }
    }

        private void SetupSearchableCombo(ComboBox cbo)
        {
            cbo.AutoCompleteMode = AutoCompleteMode.None;
            cbo.TextUpdate += delegate
            {
                if (_isFilteringCombo) return;
                _isFilteringCombo = true;
                try
                {
                    if (cbo.Tag == null)
                    {
                        var originalItems = new List<ComboItem>();
                        foreach (var item in cbo.Items)
                        {
                            if (item is ComboItem ci) originalItems.Add(ci);
                        }
                        cbo.Tag = originalItems;
                    }

                    var allList = cbo.Tag as List<ComboItem>;
                    if (allList == null) return;

                    string filter = cbo.Text.Trim();
                    int selStart = cbo.SelectionStart;

                    cbo.BeginUpdate();
                    cbo.Items.Clear();

                    if (string.IsNullOrEmpty(filter))
                    {
                        cbo.Items.AddRange(allList.ToArray());
                    }
                    else
                    {
                        var filtered = allList.Where(x => x.Text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || x.ID.ToString().Contains(filter)).ToArray();
                        cbo.Items.AddRange(filtered);
                    }

                    cbo.EndUpdate();
                    cbo.SelectionStart = selStart;
                    cbo.SelectionLength = 0;
                    if (!cbo.DroppedDown)
                    {
                        cbo.DroppedDown = true;
                        Cursor.Current = Cursors.Default;
                    }
                }
                finally
                {
                    _isFilteringCombo = false;
                }
            };
        }
    }
}

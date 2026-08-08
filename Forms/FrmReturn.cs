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
        private ComboBox cboClient, cboMode, cboWarehouse, cboReturnType, cboAllProducts, cboNewExchangeProducts;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnSearch, btnSave, btnAddGenItem, btnAddNewGenItem;
        private Label lblTotal, lblExchangeSummary, lblSearch, lblBarcode, lblFrom, lblTo, lblClient;
        private SplitContainer _mainSplit;
        private FlowLayoutPanel pnlFilter, _pnlGenItemBar, _pnlNewItemBar;
        private DataTable _salesDt;
        private bool _isFilteringCombo = false;
        private decimal _selectedSaleTotalAmount = 0m;
        private decimal _selectedSaleShippingCharge = 0m;
        private decimal _selectedSalePrevReturnedAmount = 0m;

        public FrmReturn()
        {
            InitUI();
            LoadCombos();
            LoadSales();
        }

        private void LoadCombos()
        {
            LoadClients();

            // المخازن
            if (cboWarehouse != null)
            {
                var dtWh = WarehouseDAL.GetAll(true);
                cboWarehouse.Items.Clear();
                foreach (DataRow r in dtWh.Rows)
                    cboWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
                cboWarehouse.DisplayMember = "Text";
                if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
            }

            // تحميل أصناف الكتالوج للمرتجع العام والبديل
            DataTable dtProducts = ProductDAL.GetAll(true);
            if (cboAllProducts != null)
            {
                cboAllProducts.Items.Clear();
                cboAllProducts.Items.Add(new ComboItem(0, "-- اختر الصنف المرتجع --"));
                foreach (DataRow r in dtProducts.Rows)
                {
                    var ci = new ComboItem((int)r["ProductID"], r["ProductName"].ToString());
                    ci.Extra = r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m;
                    cboAllProducts.Items.Add(ci);
                }
                cboAllProducts.DisplayMember = "Text";
                if (cboAllProducts.Items.Count > 0) cboAllProducts.SelectedIndex = 0;
                cboAllProducts.SelectedIndexChanged += (s, e) =>
                {
                    if (cboAllProducts.SelectedItem is ComboItem ci && ci.ID > 0)
                        txtGenPrice.Text = ci.Extra.ToString("N2");
                };
            }

            if (cboNewExchangeProducts != null)
            {
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
                cboNewExchangeProducts.SelectedIndexChanged += (s, e) =>
                {
                    if (cboNewExchangeProducts.SelectedItem is ComboItem ci && ci.ID > 0)
                        txtNewGenPrice.Text = ci.Extra.ToString("N2");
                };
            }
        }

        private void LoadClients()
        {
            if (cboClient == null) return;
            cboClient.SelectedIndexChanged -= CboClient_SelectedIndexChanged;
            cboClient.Items.Clear();
            cboClient.Items.Add(new ComboItem(0, "-- الكل --"));
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
            if (keyData == Keys.Enter)
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
                Height = 85,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10),
                WrapContents = true
            };

            var lblMode = new Label { Text = "العملية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 5, 0, 0), Font = Theme.FontBold };
            cboMode = new ComboBox
            {
                Width = 220, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            cboMode.Items.Add("🧾 مرتجع على فاتورة بيع محددة");
            cboMode.Items.Add("🌐 مرتجع بيع عام (بدون فاتورة)");
            cboMode.Items.Add("🔄 استبدال أصناف (مرتجع + بديل)");
            cboMode.SelectedIndex = 0;
            cboMode.SelectedIndexChanged += (s, e) => ToggleReturnMode();

            var lblWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0), Font = Theme.FontBold };
            cboWarehouse = new ComboBox
            {
                Width = 130, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            var lblRetType = new Label { Text = "نوع التسوية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0), Font = Theme.FontBold };
            cboReturnType = new ComboBox
            {
                Width = 110, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            cboReturnType.Items.Add("📋 آجل");
            cboReturnType.Items.Add("💵 نقدي");
            cboReturnType.SelectedIndex = 0;

            lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            dtpFrom = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Today.AddMonths(-1) };
            dtpFrom.ValueChanged += (s, e) => LoadSales();

            lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            dtpTo = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Now };
            dtpTo.ValueChanged += (s, e) => LoadSales();

            lblClient = new Label { Text = "العميل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            cboClient = new ComboBox 
            { 
                Width = 180, 
                DropDownStyle = ComboBoxStyle.DropDown, 
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain 
            };
            SetupSearchableCombo(cboClient);

            lblSearch = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            txtSearch = new TextBox { Width = 130, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
            txtSearch.TextChanged += (s, e) => LoadSales();

            lblBarcode = new Label { Text = "باركود:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            txtInvoiceBarcode = new TextBox { Width = 110, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.No };
            txtInvoiceBarcode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DoBarcodeSearch(txtInvoiceBarcode.Text.Trim());
                }
            };

            btnSearch = Theme.MakeButton("🔍 تحديث", Theme.Accent);
            btnSearch.Size = new Size(100, 28);
            btnSearch.Margin = new Padding(15, 0, 0, 0);
            btnSearch.Click += (s, e) => LoadSales();

            pnlFilter.Controls.AddRange(new Control[] { 
                lblMode, cboMode, 
                lblWh, cboWarehouse, 
                lblRetType, cboReturnType, 
                lblFrom, dtpFrom, lblTo, dtpTo, 
                lblClient, cboClient, 
                lblSearch, txtSearch, 
                lblBarcode, txtInvoiceBarcode, btnSearch 
            });

            // ===== شريط إضافة صنف مرتجع عام / بديل =====
            _pnlGenItemBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.FromArgb(45, 35, 45),
                Padding = new Padding(10, 6, 10, 6),
                Visible = false
            };

            var lblGenTitle = new Label { Text = "↩ صنف مرتجع:", AutoSize = true, ForeColor = Color.LightCoral, Margin = new Padding(5, 5, 0, 0), Font = Theme.FontBold };
            cboAllProducts = new ComboBox
            {
                Width = 220, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            var lblGenQtyL = new Label { Text = "الكمية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtGenQty = new TextBox { Width = 60, Text = "1", BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
            var lblGenPriceL = new Label { Text = "السعر:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtGenPrice = new TextBox { Width = 70, Text = "0", BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
            btnAddGenItem = Theme.MakeButton("➕ إضافة مرتجع", Color.FromArgb(160, 60, 60));
            btnAddGenItem.Size = new Size(110, 26);
            btnAddGenItem.Margin = new Padding(10, 0, 0, 0);
            btnAddGenItem.Click += BtnAddGenItem_Click;

            _pnlGenItemBar.Controls.AddRange(new Control[] { lblGenTitle, cboAllProducts, lblGenQtyL, txtGenQty, lblGenPriceL, txtGenPrice, btnAddGenItem });

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
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            var lblNewQtyL = new Label { Text = "الكمية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtNewGenQty = new TextBox { Width = 60, Text = "1", BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
            var lblNewPriceL = new Label { Text = "السعر:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtNewGenPrice = new TextBox { Width = 70, Text = "0", BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
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
                SplitterDistance = 250
            };
            _mainSplit.Panel1.Padding = new Padding(10, 5, 10, 5);
            _mainSplit.Panel2.Padding = new Padding(10, 5, 10, 5);

            // Top Grid: Sales Invoices
            dgSales = MakeGrid();
            dgSales.AutoGenerateColumns = false;
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleID", DataPropertyName = "SaleID", Visible = false });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", DataPropertyName = "SaleCode", HeaderText = "رقم الفاتورة", FillWeight = 50f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleDate", DataPropertyName = "SaleDate", HeaderText = "التاريخ والوقت", FillWeight = 75f, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleType", DataPropertyName = "SaleType", HeaderText = "نوع الفاتورة", FillWeight = 50f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", DataPropertyName = "ClientName", HeaderText = "اسم العميل", FillWeight = 110f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "DriverName", DataPropertyName = "DriverName", HeaderText = "اسم المندوب", FillWeight = 80f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", DataPropertyName = "TotalAmount", HeaderText = "إجمالي الفاتورة", FillWeight = 60f, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnAmount", DataPropertyName = "ReturnAmount", HeaderText = "المسترجع سابقاً", FillWeight = 60f, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", DataPropertyName = "CreatedByName", HeaderText = "المستخدم", FillWeight = 85f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", DataPropertyName = "Notes", HeaderText = "الملاحظات", FillWeight = 120f });

            dgSales.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgSales.Columns[e.ColumnIndex].Name == "SaleType" && e.Value != null)
                {
                    string val = e.Value.ToString();
                    if (val == "Cash") e.Value = "💵 نقدي";
                    else if (val == "Credit") e.Value = "📋 آجل";
                    else if (val == "DriverLoad") e.Value = "🚚 حمولة مندوب";
                    else if (val == "Installment") e.Value = "📅 تقسيط";
                }
            };

            dgSales.SelectionChanged += DgSales_SelectionChanged;
            _mainSplit.Panel1.Controls.Add(dgSales);

            // Bottom Grid: Selected Sale Items / Return Items
            dgItems = MakeGrid();
            dgItems.ReadOnly = false;
            dgItems.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف المرتجع", ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldQty", HeaderText = "الكمية الأصلية", ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedQty", HeaderText = "المرتجع السابق", ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewComboBoxColumn { Name = "UnitName", HeaderText = "الوحدة", ReadOnly = false, FillWeight = 40 });
            
            var colNew = new DataGridViewTextBoxColumn 
            { 
                Name = "NewReturnedQty", 
                HeaderText = "المرتجع الجديد", 
                ReadOnly = false, 
                FillWeight = 50,
                ValueType = typeof(decimal)
            };
            colNew.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 60);
            colNew.DefaultCellStyle.ForeColor = Color.Yellow;
            colNew.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgItems.Columns.Add(colNew);
            
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر المرتجع", ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "إجمالي المرتجع", ReadOnly = true, FillWeight = 50 });

            // Hidden helper columns
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "OriginalFactor", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "OriginalUnitPrice", Visible = false });
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
            dgItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(140, 40, 40);

            // جدول أصناف البديل الجديد في الاستبدال
            dgExchangeNewItems = MakeGrid();
            dgExchangeNewItems.ReadOnly = false;
            dgExchangeNewItems.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف البديل الجديد", ReadOnly = true });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewQty", HeaderText = "الكمية", ReadOnly = false, FillWeight = 50 });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر البيع", ReadOnly = false, FillWeight = 50 });
            dgExchangeNewItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "إجمالي الصرف", ReadOnly = true, FillWeight = 60 });
            dgExchangeNewItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 110, 60);
            dgExchangeNewItems.CellValueChanged += (s, e) => RecalcTotals();
            dgExchangeNewItems.Visible = false;

            var pnlGridsContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            pnlGridsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            pnlGridsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            pnlGridsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            pnlGridsContainer.Controls.Add(dgItems, 0, 0);
            pnlGridsContainer.Controls.Add(dgExchangeNewItems, 1, 0);

            _mainSplit.Panel2.Controls.Add(pnlGridsContainer);

            // ===== 3. Footer panel =====
            var pnlFoot = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 65, 
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblNotesL = new Label { Text = "ملاحظات العملية:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(15, 20), Anchor = AnchorStyles.Left };
            txtNotes = new TextBox { Width = 250, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle, Location = new Point(125, 16), Anchor = AnchorStyles.Left };
            
            lblTotal = new Label 
            { 
                Text = "الإجمالي: 0.00 ج", 
                ForeColor = Theme.Accent, 
                Dock = DockStyle.Right,
                Width = 250,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            lblExchangeSummary = new Label
            {
                Text = "",
                ForeColor = Color.Gold,
                Dock = DockStyle.Right,
                Width = 350,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false
            };

            btnSave = Theme.MakeButton("💾 حفظ العملية", Color.FromArgb(160, 50, 50));
            btnSave.Width = 180;
            btnSave.Height = 38;
            btnSave.Location = new Point(400, 12);
            btnSave.Anchor = AnchorStyles.None;
            btnSave.Font = Theme.FontBold;
            btnSave.Click += BtnSave_Click;
            
            Label lblHotkeys = new Label
            {
                Text = "الاختصارات: [F5] حفظ",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(600, 20),
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            pnlFoot.Controls.AddRange(new Control[] { lblNotesL, txtNotes, lblExchangeSummary, lblTotal, btnSave, lblHotkeys });

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
            dgExchangeNewItems.Visible = isExchange;

            lblFrom.Visible = isInvoice; dtpFrom.Visible = isInvoice;
            lblTo.Visible = isInvoice; dtpTo.Visible = isInvoice;
            lblSearch.Visible = isInvoice; txtSearch.Visible = isInvoice;
            lblBarcode.Visible = isInvoice; txtInvoiceBarcode.Visible = isInvoice;
            btnSearch.Visible = isInvoice;

            lblExchangeSummary.Visible = isExchange;

            dgItems.Rows.Clear();
            dgExchangeNewItems.Rows.Clear();
            RecalcTotals();
        }

        private void BtnAddGenItem_Click(object sender, EventArgs e)
        {
            if (!(cboAllProducts.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر الصنف المراد إرجاعه أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtGenQty.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("أدخل كمية صالحة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtGenPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("أدخل سعر صالح", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idx = dgItems.Rows.Add();
            var row = dgItems.Rows[idx];
            row.Cells["ProductID"].Value       = ci.ID;
            row.Cells["ProductName"].Value     = ci.Text;
            row.Cells["SoldQty"].Value         = "عام";
            row.Cells["PrevReturnedQty"].Value = "0";
            row.Cells["NewReturnedQty"].Value  = qty;
            row.Cells["UnitPrice"].Value       = price.ToString("N2");
            row.Cells["TotalPrice"].Value      = (qty * price).ToString("N2");

            RecalcTotals();
        }

        private void BtnAddNewGenItem_Click(object sender, EventArgs e)
        {
            if (!(cboNewExchangeProducts.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر الصنف البديل الجديد أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtNewGenQty.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("أدخل كمية صالحة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtNewGenPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("أدخل سعر صالح", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idx = dgExchangeNewItems.Rows.Add();
            var row = dgExchangeNewItems.Rows[idx];
            row.Cells["ProductID"].Value   = ci.ID;
            row.Cells["ProductName"].Value = ci.Text;
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
                    lblTotal.Text = $"الصافي للدفع: {diff:N2} ج";
                else
                    lblTotal.Text = $"الصافي للمسترجع: {Math.Abs(diff):N2} ج";
            }
            else
            {
                lblTotal.Text = $"إجمالي المرتجع: {totalRet:N2} ج";
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
                EnableHeadersVisualStyles = false
            };
            return dg;
        }

        private void LoadSales()
        {
            if (cboMode != null && cboMode.SelectedIndex != 0) return;
            try
            {
                int? clientID = null;
                if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
                    clientID = ci.ID;

                _salesDt = SaleDAL.GetAll(dtpFrom.Value, dtpTo.Value, clientID, txtSearch.Text.Trim());
                dgSales.DataSource = _salesDt;
            }
            catch (Exception ex)
            {
                AppLogger.Error("خطأ أثناء تحميل فواتير البيع للمرتجع", ex, "FrmReturn.LoadSales");
            }
        }

        private void DgSales_SelectionChanged(object sender, EventArgs e)
        {
            dgItems.Rows.Clear();
            lblTotal.Text = "الإجمالي: 0.00 ج";
            _selectedSaleTotalAmount = 0m;
            _selectedSaleShippingCharge = 0m;
            _selectedSalePrevReturnedAmount = 0m;

            if (dgSales.CurrentRow == null || dgSales.CurrentRow.Cells["SaleID"].Value == null)
                return;

            int saleID = Convert.ToInt32(dgSales.CurrentRow.Cells["SaleID"].Value);
            
            var dtSaleInfo = DbHelper.Query("SELECT ISNULL(TotalAmount,0) AS TotalAmount, ISNULL(ShippingCharge,0) AS ShippingCharge FROM Sales WHERE SaleID = @id", DbHelper.P("@id", saleID));
            if (dtSaleInfo.Rows.Count > 0)
            {
                _selectedSaleTotalAmount = Convert.ToDecimal(dtSaleInfo.Rows[0]["TotalAmount"]);
                _selectedSaleShippingCharge = Convert.ToDecimal(dtSaleInfo.Rows[0]["ShippingCharge"]);
            }

            var dtPrevRetInfo = DbHelper.Query("SELECT ISNULL(SUM(TotalAmount),0) AS PrevReturned FROM SalesReturns WHERE SaleID = @id", DbHelper.P("@id", saleID));
            if (dtPrevRetInfo.Rows.Count > 0)
            {
                _selectedSalePrevReturnedAmount = Convert.ToDecimal(dtPrevRetInfo.Rows[0]["PrevReturned"]);
            }

            DataTable dtItems = SaleDAL.GetItems(saleID);

            foreach (DataRow row in dtItems.Rows)
            {
                int rowIndex = dgItems.Rows.Add();
                var dgRow = dgItems.Rows[rowIndex];

                dgRow.Cells["ProductID"].Value = row["ProductID"];
                dgRow.Cells["ProductName"].Value = row["ProductName"];

                decimal soldQty = dtItems.Columns.Contains("SoldQty") ? Convert.ToDecimal(row["SoldQty"]) : (dtItems.Columns.Contains("Quantity") ? Convert.ToDecimal(row["Quantity"]) : 0m);
                decimal prevRetQty = dtItems.Columns.Contains("PrevReturnedQty") ? Convert.ToDecimal(row["PrevReturnedQty"]) : 0m;
                decimal origUnitPrice = dtItems.Columns.Contains("UnitPrice") ? Convert.ToDecimal(row["UnitPrice"]) : 0m;

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
                dgRow.Cells["OriginalUnitPrice"].Value = origUnitPrice;
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
                else if (comboCell.Items.Count > 0)
                    comboCell.Value = comboCell.Items[0];

                dgRow.Cells["SoldQty"].Value = soldQty.ToString("G29");
                dgRow.Cells["PrevReturnedQty"].Value = prevRetQty.ToString("G29");
                dgRow.Cells["NewReturnedQty"].Value = 0m;
                dgRow.Cells["UnitPrice"].Value = origUnitPrice.ToString("F2");
                dgRow.Cells["TotalPrice"].Value = "0.00";
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
            if (e.RowIndex < 0) return;
            var row = dgItems.Rows[e.RowIndex];
            var colName = dgItems.Columns[e.ColumnIndex].Name;

            if (colName == "NewReturnedQty")
            {
                decimal newQty = 0;
                if (row.Cells["NewReturnedQty"].Value != null)
                    decimal.TryParse(row.Cells["NewReturnedQty"].Value.ToString(), out newQty);
                
                decimal price = Convert.ToDecimal(row.Cells["UnitPrice"].Value);
                decimal rowTotal = newQty * price;
                row.Cells["TotalPrice"].Value = rowTotal.ToString("F2");

                RecalcTotals();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("Returns")) { MessageBox.Show("⛔ ليس لديك صلاحية حفظ مرتجعات المبيعات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int mode = cboMode.SelectedIndex;
            int? warehouseID = (cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? (int?)cw.ID : 1;
            string returnType = cboReturnType.SelectedIndex == 1 ? "Cash" : "Credit";
            int? clientID = (cboClient.SelectedItem is ComboItem cc && cc.ID > 0) ? (int?)cc.ID : null;

            if (mode == 0) // مرتجع فاتورة معينة
            {
                if (dgSales.CurrentRow == null || dgSales.CurrentRow.Cells["SaleID"].Value == null)
                {
                    MessageBox.Show("يرجى اختيار الفاتورة المراد الإرجاع منها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
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
                        returnItems.Add(new SaleItemDTO 
                        { 
                            ProductID = prodID, 
                            ProductName = prodName, 
                            Quantity = newQty, 
                            UnitPrice = price,
                            UnitName = row.Cells["UnitName"].Value?.ToString(),
                            Factor = 1m
                        });
                        totalReturnAmount += (newQty * price);
                    }
                }

                if (returnItems.Count == 0)
                {
                    MessageBox.Show("يرجى إدخال كمية مرتجعة جديدة صالحة لصنف واحد على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    int id = ReturnDAL.SaveReturn(saleID, clientID, totalReturnAmount, txtNotes.Text, returnItems, warehouseID, returnType);
                    if (id > 0) 
                    { 
                        MessageBox.Show("✅ تم حفظ مرتجع البيع بنجاح!", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information); 
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
                        returnItems.Add(new SaleItemDTO { ProductID = prodID, ProductName = prodName, Quantity = newQty, UnitPrice = price });
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
                    int id = ReturnDAL.SaveReturn(0, clientID, totalReturnAmount, txtNotes.Text, returnItems, warehouseID, returnType);
                    if (id > 0)
                    {
                        MessageBox.Show("✅ تم حفظ مرتجع البيع العام بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    if (q > 0) retItems.Add(new SaleItemDTO { ProductID = pid, ProductName = name, Quantity = q, UnitPrice = p });
                }

                var newItems = new List<SaleItemDTO>();
                foreach (DataGridViewRow r in dgExchangeNewItems.Rows)
                {
                    int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
                    string name = r.Cells["ProductName"].Value.ToString();
                    decimal.TryParse(r.Cells["NewQty"].Value?.ToString(), out decimal q);
                    decimal.TryParse(r.Cells["UnitPrice"].Value?.ToString(), out decimal p);
                    if (q > 0) newItems.Add(new SaleItemDTO { ProductID = pid, ProductName = name, Quantity = q, UnitPrice = p });
                }

                if (retItems.Count == 0 || newItems.Count == 0)
                {
                    MessageBox.Show("يجب إضافة صنف مرتجع وصنف بديل جديد على الأقل لعملية الاستبدال!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    bool ok = ReturnDAL.SaveItemExchange(clientID, warehouseID.Value, retItems, newItems, returnType, txtNotes.Text);
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
            if (string.IsNullOrEmpty(code)) return;
            try
            {
                var dt = DbHelper.Query("SELECT SaleID FROM Sales WHERE SaleCode = @code OR CAST(SaleID AS VARCHAR) = @code", DbHelper.P("@code", code));
                if (dt.Rows.Count > 0)
                {
                    int targetSaleID = Convert.ToInt32(dt.Rows[0]["SaleID"]);
                    bool found = false;

                    dgSales.SelectionChanged -= DgSales_SelectionChanged;
                    foreach (DataGridViewRow row in dgSales.Rows)
                    {
                        if (row.Cells["SaleID"].Value != null && Convert.ToInt32(row.Cells["SaleID"].Value) == targetSaleID)
                        {
                            dgSales.CurrentCell = row.Cells[1];
                            found = true;
                            break;
                        }
                    }
                    dgSales.SelectionChanged += DgSales_SelectionChanged;

                    if (found)
                    {
                        DgSales_SelectionChanged(dgSales, EventArgs.Empty);
                        dgItems.Focus();
                    }
                    else
                    {
                        MessageBox.Show("الفاتورة غير موجودة في نطاق التواريخ المحدد في الأعلى.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("عذراً، رقم الفاتورة أو الباركود غير صحيح أو غير مسجل بالنظام.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء البحث:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupSearchableCombo(ComboBox cbo)
        {
            cbo.AutoCompleteMode = AutoCompleteMode.None;
            cbo.TextUpdate += delegate
            {
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
                    string currentText = cbo.Text;
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

                    cbo.Text = currentText;
                    cbo.SelectionStart = selStart;
                    cbo.SelectionLength = 0;
                    cbo.DroppedDown = true;
                    cbo.EndUpdate();
                }
                finally
                {
                    _isFilteringCombo = false;
                }
            };
        }
    }
}

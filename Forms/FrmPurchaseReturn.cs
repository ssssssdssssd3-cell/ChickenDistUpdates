using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة مرتجع مشتريات مطورة لحساب الخصم وشبكة فواتير وأصناف مقسمة</summary>
    public class FrmPurchaseReturn : Form
    {
        private ComboBox cboMode, cboSupplier, cboWarehouse, cboReturnType, cboAllProducts;
        private ComboBox cboDatePreset, cboPurchaseTypeFilter, cboReturnStatusFilter, cboProductFilter;
        private TextBox txtNotes, txtSearch, txtBarcode, txtGenQty, txtGenPrice, txtItemFilter;
        private DataGridView dgPurchases, dgItems;
        private Button btnSave, btnAddGenItem, btnSearch, btnClearFilters, btnProductFilterSearch, btnSupplierSearch;
        private Label lblTotal, lblPurchaseInfo, lblItemsCountInfo, lblTopTitle;
        private DateTimePicker dtpFrom, dtpTo;
        private SplitContainer _mainSplit;
        private FlowLayoutPanel pnlGeneralItemBar, _pnlInvoiceItemFilter;
        private Timer _searchDebounceTimer;
        private bool _isLoadingPurchases = false;
        private bool _isFilteringCombo = false;
        private bool _isApplyingDatePreset = false;
        private int _selectedPurchaseID = 0;
        private int? _selectedSupplierID = null;

        public FrmPurchaseReturn()
        {
            InitUI();
            LoadCombos();
            this.Shown += (s, e) => LoadPurchasesGrid();
        }

        private void FrmPurchaseReturn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
                btnSave.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                if (cboMode.SelectedIndex == 1)
                {
                    OpenQuickSearch();
                    e.Handled = true;
                }
                else
                {
                    btnProductFilterSearch?.PerformClick();
                    e.Handled = true;
                }
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
                            { nextCol = col; break; }
                        }
                        if (nextCol != -1)
                        {
                            dgItems.CurrentCell = dgItems.Rows[curCell.RowIndex].Cells[nextCol];
                            dgItems.BeginEdit(true);
                            return true;
                        }
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitUI()
        {
            this.Text = "مرتجع مشتريات (على فاتورة / عام)";
            this.Size = new Size(1240, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmPurchaseReturn_KeyDown;

            _searchDebounceTimer = new Timer { Interval = 320 };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                if (cboMode != null && cboMode.SelectedIndex == 0)
                    LoadPurchasesGrid();
            };

            // ===== شريط العنوان =====
            var pnlTitle = Theme.MakeTitleBar("↩ مرتجع مشتريات", "إرجاع بضاعة بسعر الشراء الصافي بعد الخصم مع فلاتر بحث مفصلة وتقسيم الشاشة لفواتير وأصناف");
            pnlTitle.Dock = DockStyle.Top;

            // ===== شريط الفلتر الأعلى =====
            var pnlInfo = new FlowLayoutPanel
            {
                Name = "pnlFilter",
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgSearchPanel,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 92),
                Padding = new Padding(8, 6, 8, 6)
            };

            // نوع المرتجع
            var lblMode = new Label { Text = "نوع المرتجع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 0, 0), Font = Theme.FontBold };
            cboMode = new ComboBox
            {
                Width = 200, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            cboMode.Items.Add("🧾 مرتجع على فاتورة شراء معينة");
            cboMode.Items.Add("🌐 مرتجع شراء عام (بدون فاتورة)");
            cboMode.SelectedIndex = 0;
            cboMode.SelectedIndexChanged += (s, e) => ToggleReturnMode();

            // المخزن
            var lblWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            cboWarehouse = new ComboBox
            {
                Width = 135, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            // طريقة التسوية
            var lblRetType = new Label { Text = "طريقة التسوية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            cboReturnType = new ComboBox
            {
                Width = 155, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cboReturnType.Items.Add("📋 آجل (خصم من المورد)");
            cboReturnType.Items.Add("💵 نقدي (استرداد للخزينة)");
            cboReturnType.Items.Add("💳 فيزا / بنكي (استرداد للبنك)");
            cboReturnType.SelectedIndex = 0;

            // اختصارات الفترة الزمنية
            var lblPreset = new Label { Text = "الفترة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            cboDatePreset = new ComboBox
            {
                Width = 110, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            cboDatePreset.Items.AddRange(new object[] {
                "آخر شهر",
                "اليوم",
                "أمس",
                "آخر 7 أيام",
                "هذا الشهر",
                "الشهر الماضي",
                "هذه السنة",
                "كل الفترات"
            });
            cboDatePreset.SelectedIndex = 0;
            cboDatePreset.SelectedIndexChanged += (s, e) => ApplyDatePreset();

            // تواريخ البحث
            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(8, 8, 0, 0), Font = Theme.FontBold };
            dtpFrom = new DateTimePicker { Width = 145, Height = 26, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Today.AddMonths(-1) };
            dtpFrom.ValueChanged += (s, e) => { if (!_isApplyingDatePreset && cboMode.SelectedIndex == 0) LoadPurchasesGrid(); };

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(8, 8, 0, 0), Font = Theme.FontBold };
            dtpTo = new DateTimePicker { Width = 145, Height = 26, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Now };
            dtpTo.ValueChanged += (s, e) => { if (!_isApplyingDatePreset && cboMode.SelectedIndex == 0) LoadPurchasesGrid(); };

            // جهة الشراء
            var lblSupplierLbl = new Label { Text = "جهة الشراء:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            cboSupplier = new ComboBox
            {
                Width = 175, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.None,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            SetupSearchableCombo(cboSupplier);

            btnSupplierSearch = Theme.MakeButton("🔍", Color.FromArgb(40, 100, 140));
            btnSupplierSearch.Size = new Size(32, 26);
            btnSupplierSearch.Margin = new Padding(2, 3, 4, 0);
            var ttSup = new ToolTip();
            ttSup.SetToolTip(btnSupplierSearch, "بحث سريع عن مورد بالاسم أو الكود أو الهاتف");
            btnSupplierSearch.Click += (s, e) => OpenSupplierSearchDialog();

            // نوع فاتورة الشراء
            var lblPurType = new Label { Text = "نوع الفاتورة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            cboPurchaseTypeFilter = new ComboBox
            {
                Width = 105, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            cboPurchaseTypeFilter.Items.Add("الكل");
            cboPurchaseTypeFilter.Items.Add("نقدي");
            cboPurchaseTypeFilter.Items.Add("آجل");
            cboPurchaseTypeFilter.SelectedIndex = 0;
            cboPurchaseTypeFilter.SelectedIndexChanged += (s, e) => { if (cboMode.SelectedIndex == 0) LoadPurchasesGrid(); };

            // حالة المرتجع السابق
            var lblRetStatus = new Label { Text = "حالة المرتجع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            cboReturnStatusFilter = new ComboBox
            {
                Width = 130, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            cboReturnStatusFilter.Items.AddRange(new object[] {
                "الكل",
                "بدون مرتجع سابق",
                "عليها مرتجع سابق"
            });
            cboReturnStatusFilter.SelectedIndex = 0;
            cboReturnStatusFilter.SelectedIndexChanged += (s, e) => { if (cboMode.SelectedIndex == 0) LoadPurchasesGrid(); };

            // فلتر الصنف
            var lblProdFilter = new Label { Text = "بحث بالصنف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            cboProductFilter = new ComboBox
            {
                Width = 185, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.None,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            SetupSearchableCombo(cboProductFilter);
            cboProductFilter.SelectedIndexChanged += (s, e) => { if (cboMode.SelectedIndex == 0) LoadPurchasesGrid(); };

            btnProductFilterSearch = Theme.MakeButton("🔍", Color.FromArgb(20, 80, 150));
            btnProductFilterSearch.Size = new Size(32, 26);
            btnProductFilterSearch.Margin = new Padding(2, 3, 4, 0);
            var ttProd = new ToolTip();
            ttProd.SetToolTip(btnProductFilterSearch, "فتح نافذة البحث المفصل عن صنف لفلترة فواتير الشراء به (F3)");
            btnProductFilterSearch.Click += (s, e) =>
            {
                int? whId = (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? (int?)cw.ID : null;
                using (var dlg = new FrmProductSearch(whId, isPurchaseMode: true, defaultShowZeroStock: true))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedProductID > 0)
                    {
                        if (cboProductFilter.Tag is List<ComboItem> allProds)
                        {
                            cboProductFilter.BeginUpdate();
                            cboProductFilter.Items.Clear();
                            cboProductFilter.Items.AddRange(allProds.ToArray());
                            cboProductFilter.EndUpdate();
                        }
                        for (int i = 0; i < cboProductFilter.Items.Count; i++)
                        {
                            if (cboProductFilter.Items[i] is ComboItem ci && ci.ID == dlg.SelectedProductID)
                            {
                                cboProductFilter.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            };

            // نص البحث الشامل
            var lblSearch = new Label { Text = "بحث شامل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            txtSearch = new TextBox
            {
                Width = 175, Height = 26,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle
            };
            var ttSearch = new ToolTip();
            ttSearch.SetToolTip(txtSearch, "ابحث برقم الفاتورة، رقم فاتورة المورد، اسم/كود المورد، اسم/كود/باركود الصنف، السيريال IMEI، الملاحظات، أو القيمة");
            txtSearch.TextChanged += (s, e) =>
            {
                if (_isLoadingPurchases) return;
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
                    string t = txtSearch.Text.Trim();
                    string clean = PurchaseDAL.SanitizeIMEIString(t);
                    if (AppConfig.BusinessType == "Mobiles" && clean.Length >= 14 && clean.Length <= 16 && clean.All(char.IsDigit))
                    {
                        DoBarcodeSearch(t);
                    }
                    else
                    {
                        LoadPurchasesGrid();
                    }
                }
            };

            btnSearch = Theme.MakeButton("🔍 جلب الفواتير", Theme.Accent);
            btnSearch.Size = new Size(105, 28);
            btnSearch.Margin = new Padding(6, 2, 0, 0);
            btnSearch.Click += (s, e) => LoadPurchasesGrid();

            btnClearFilters = Theme.MakeButton("🔄 مسح الفلاتر", Color.FromArgb(100, 116, 139));
            btnClearFilters.Size = new Size(100, 28);
            btnClearFilters.Margin = new Padding(4, 2, 0, 0);
            btnClearFilters.Click += (s, e) => ResetAllFilters();

            // مسح بالاسكانر / السيريال (IMEI)
            var lblBarcode = new Label { Text = AppConfig.BusinessType == "Mobiles" ? "📷 اسكانر / IMEI:" : "📷 مسح بالاسكانر:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            txtBarcode = new TextBox
            {
                Width = 140, Height = 26,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                RightToLeft = RightToLeft.No, BorderStyle = BorderStyle.FixedSingle
            };
            txtBarcode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DoBarcodeSearch(txtBarcode.Text.Trim());
                }
            };

            pnlInfo.Controls.AddRange(new Control[] {
                lblMode, cboMode,
                lblWh, cboWarehouse,
                lblRetType, cboReturnType,
                lblPreset, cboDatePreset,
                lblFrom, dtpFrom, lblTo, dtpTo,
                lblSupplierLbl, cboSupplier, btnSupplierSearch,
                lblPurType, cboPurchaseTypeFilter,
                lblRetStatus, cboReturnStatusFilter,
                lblProdFilter, cboProductFilter, btnProductFilterSearch,
                lblSearch, txtSearch, btnSearch, btnClearFilters,
                lblBarcode, txtBarcode
            });

            // ===== SplitContainer للتخطيط المقسم (فواتير فوق / أصناف تحت) =====
            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 210,
                BackColor = Theme.BorderColor,
                Padding = new Padding(5)
            };

            // ── الجزء الأعلى: جدول فواتير الشراء ──
            var pnlTop = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };
            lblTopTitle = new Label
            {
                Text = "📋 فواتير الشراء المطابقة للبحث (اختر الفاتورة لعرض أصنافها والخصم بالأسفل):",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 80, 160),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            dgPurchases = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(35, 65, 110),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                },
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 32,
                EnableHeadersVisualStyles = false
            };

            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseID", Visible = false });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseID", Visible = false });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseCode", HeaderText = "رقم الفاتورة", FillWeight = 60 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierInvoiceNo", HeaderText = "فاتورة المورد", FillWeight = 55 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseDate", HeaderText = "التاريخ والوقت", FillWeight = 75 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "جهة الشراء", FillWeight = 85 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierCode", HeaderText = "كود المورد", FillWeight = 45, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "المخزن", FillWeight = 55 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseTypeStr", HeaderText = "نوع الفاتورة", FillWeight = 48 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", HeaderText = "قبل الخصم", FillWeight = 52 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountAmount", HeaderText = "الخصم", FillWeight = 42 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "الصافي النهائي", FillWeight = 60 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnAmount", HeaderText = "المرتجع السابق", FillWeight = 55 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات", FillWeight = 85 });

            dgPurchases.SelectionChanged += DgPurchases_SelectionChanged;
            dgPurchases.CellClick += (s, e) => { if (e.RowIndex >= 0) DgPurchases_SelectionChanged(dgPurchases, EventArgs.Empty); };
            pnlTop.Controls.Add(dgPurchases);
            pnlTop.Controls.Add(lblTopTitle);
            _mainSplit.Panel1.Controls.Add(pnlTop);

            // ── الجزء الأسفل: شريط فلترة أصناف الفاتورة + شريط المرتجع العام + جدول الأصناف ──
            var pnlBottom = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };

            _pnlInvoiceItemFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(8, 4, 8, 4),
                WrapContents = false
            };
            var lblItemFilter = new Label
            {
                Text = "🔎 بحث سريع داخل أصناف الفاتورة المحددة:",
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 58, 138),
                Font = Theme.FontBold,
                Margin = new Padding(4, 4, 4, 0)
            };
            txtItemFilter = new TextBox
            {
                Width = 230,
                Height = 26,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                RightToLeft = RightToLeft.Yes,
                Font = Theme.FontBold,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtItemFilter.TextChanged += (s, e) => FilterInvoiceItems();

            var btnClearItemFilter = Theme.MakeButton("✖", Color.FromArgb(148, 163, 184));
            btnClearItemFilter.Size = new Size(28, 25);
            btnClearItemFilter.Margin = new Padding(3, 1, 8, 0);
            btnClearItemFilter.Click += (s, e) => { txtItemFilter.Text = ""; };

            lblItemsCountInfo = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = Theme.FontBold,
                Margin = new Padding(10, 4, 0, 0)
            };
            _pnlInvoiceItemFilter.Controls.AddRange(new Control[] { lblItemFilter, txtItemFilter, btnClearItemFilter, lblItemsCountInfo });

            pnlGeneralItemBar = new FlowLayoutPanel
            {
                Name = "pnlFilter",
                Dock = DockStyle.Top,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(5, 5, 5, 5),
                Visible = false
            };

            var btnSearchProd = Theme.MakeButton("🔍 بحث عن صنف (F3)", Color.FromArgb(20, 80, 150));
            btnSearchProd.Size = new Size(150, 26);
            btnSearchProd.Margin = new Padding(5, 1, 5, 0);
            btnSearchProd.Click += (s, e) => OpenQuickSearch();

            var lblGenProd = new Label { Text = "الصنف:", AutoSize = true, ForeColor = Color.FromArgb(255, 220, 110), Margin = new Padding(10, 6, 0, 0), Font = Theme.FontBold };
            cboAllProducts = new ComboBox
            {
                Width = 220, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.None,
                BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42)
            };
            SetupSearchableCombo(cboAllProducts);

            var lblGenQty = new Label { Text = "الكمية:", AutoSize = true, ForeColor = Color.FromArgb(255, 220, 110), Margin = new Padding(10, 6, 0, 0), Font = Theme.FontBold };
            txtGenQty = new TextBox { Width = 65, Height = 26, Text = "1", BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle };

            var lblGenPrice = new Label { Text = "سعر الشراء الصافي:", AutoSize = true, ForeColor = Color.FromArgb(255, 220, 110), Margin = new Padding(10, 6, 0, 0), Font = Theme.FontBold };
            txtGenPrice = new TextBox { Width = 75, Height = 26, Text = "0", BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle };

            btnAddGenItem = Theme.MakeButton("➕ إضافة للصنف المرتجع", Theme.Success);
            btnAddGenItem.Size = new Size(160, 26);
            btnAddGenItem.Margin = new Padding(10, 1, 0, 0);
            btnAddGenItem.Click += BtnAddGenItem_Click;

            txtGenQty.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; BtnAddGenItem_Click(s, e); } };
            txtGenPrice.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; BtnAddGenItem_Click(s, e); } };
            cboAllProducts.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; BtnAddGenItem_Click(s, e); } };

            pnlGeneralItemBar.Controls.AddRange(new Control[] {
                btnSearchProd,
                lblGenProd, cboAllProducts,
                lblGenQty, txtGenQty,
                lblGenPrice, txtGenPrice,
                btnAddGenItem
            });

            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 90, 50),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                },
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 34,
                EnableHeadersVisualStyles = false
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Barcode", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", ReadOnly = true, FillWeight = 110 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "UnitName", 
                HeaderText = "الوحدة", 
                ReadOnly = true, 
                FillWeight = 45,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                } 
            });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "IMEI", 
                HeaderText = "السيريال (IMEI)", 
                ReadOnly = false, 
                FillWeight = 65, 
                Visible = AppConfig.BusinessType == "Mobiles",
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter, 
                    ForeColor = Color.FromArgb(14, 165, 233), 
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) 
                } 
            });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasedQty", HeaderText = "الكمية بالفاتورة", ReadOnly = true, FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedQty", HeaderText = "المرتجع السابق", ReadOnly = true, FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "AvailableStock", 
                HeaderText = "الرصيد المتاح بالمخزن", 
                ReadOnly = true, 
                FillWeight = 50,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter, 
                    ForeColor = Color.LightSkyBlue, 
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) 
                } 
            });

            var colNew = new DataGridViewTextBoxColumn
            {
                Name = "NewReturnedQty",
                HeaderText = "المرتجع الجديد (تعديل مباشر)",
                ReadOnly = false,
                FillWeight = 60,
                ValueType = typeof(decimal)
            };
            colNew.DefaultCellStyle.BackColor = Color.FromArgb(40, 60, 45);
            colNew.DefaultCellStyle.ForeColor = Color.LightGreen;
            colNew.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgItems.Columns.Add(colNew);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasedBonusQty", HeaderText = "بونص الفاتورة", ReadOnly = true, FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedBonusQty", HeaderText = "مرتجع بونص سابق", ReadOnly = true, FillWeight = 45 });

            var colNewBonus = new DataGridViewTextBoxColumn
            {
                Name = "NewReturnedBonusQty",
                HeaderText = "مرتجع البونص (تعديل)",
                ReadOnly = false,
                FillWeight = 55,
                ValueType = typeof(decimal)
            };
            colNewBonus.DefaultCellStyle.BackColor = Color.FromArgb(45, 55, 40);
            colNewBonus.DefaultCellStyle.ForeColor = Color.FromArgb(160, 255, 160);
            colNewBonus.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgItems.Columns.Add(colNewBonus);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "GrossUnitPrice", HeaderText = "سعر الشراء", ReadOnly = true, FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountPct", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetUnitPrice", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "إجمالي المرتجع", ReadOnly = true, FillWeight = 60 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Factor", Visible = false });

            dgItems.CellValidating += DgItems_CellValidating;
            dgItems.CellValueChanged += DgItems_CellValueChanged;
            dgItems.DataError += (s, e) => { e.ThrowException = false; };

            pnlBottom.Controls.Add(dgItems);
            pnlBottom.Controls.Add(_pnlInvoiceItemFilter);
            pnlBottom.Controls.Add(pnlGeneralItemBar);
            _mainSplit.Panel2.Controls.Add(pnlBottom);

            // ===== شريط الذيل السفلي =====
            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblNotesLbl = new Label { Text = "ملاحظات المرتجع:", Location = new Point(15, 12), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            txtNotes = new TextBox
            {
                Location = new Point(125, 8),
                Width = 260, Height = 26,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle
            };

            lblPurchaseInfo = new Label
            {
                Location = new Point(400, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(10, 120, 180),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = ""
            };

            lblTotal = new Label
            {
                Text = "إجمالي مرتجع الشراء: 0.00 ج",
                ForeColor = Color.FromArgb(20, 160, 80),
                Dock = DockStyle.Right,
                Width = 320,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            btnSave = Theme.MakeButton("💾 حفظ مرتجع الشراء (F5)", Color.FromArgb(30, 110, 50));
            btnSave.Location = new Point(15, 34);
            btnSave.Size = new Size(220, 28);
            btnSave.Font = Theme.FontBold;
            btnSave.Click += BtnSave_Click;

            pnlFoot.Controls.AddRange(new Control[] { lblNotesLbl, txtNotes, lblPurchaseInfo, lblTotal, btnSave });

            // ===== تجميع الشاشة =====
            this.Controls.Add(_mainSplit);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlTitle);

            _mainSplit.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private void ApplyDatePreset()
        {
            if (cboDatePreset == null) return;
            _isApplyingDatePreset = true;
            try
            {
                DateTime today = DateTime.Today;
                switch (cboDatePreset.SelectedIndex)
                {
                    case 0: // آخر شهر
                        dtpFrom.Value = today.AddMonths(-1);
                        dtpTo.Value = DateTime.Now;
                        break;
                    case 1: // اليوم
                        dtpFrom.Value = today;
                        dtpTo.Value = today.AddDays(1).AddSeconds(-1);
                        break;
                    case 2: // أمس
                        dtpFrom.Value = today.AddDays(-1);
                        dtpTo.Value = today.AddSeconds(-1);
                        break;
                    case 3: // آخر 7 أيام
                        dtpFrom.Value = today.AddDays(-7);
                        dtpTo.Value = DateTime.Now;
                        break;
                    case 4: // هذا الشهر
                        dtpFrom.Value = new DateTime(today.Year, today.Month, 1);
                        dtpTo.Value = DateTime.Now;
                        break;
                    case 5: // الشهر الماضي
                        var firstThisMonth = new DateTime(today.Year, today.Month, 1);
                        dtpFrom.Value = firstThisMonth.AddMonths(-1);
                        dtpTo.Value = firstThisMonth.AddSeconds(-1);
                        break;
                    case 6: // هذه السنة
                        dtpFrom.Value = new DateTime(today.Year, 1, 1);
                        dtpTo.Value = DateTime.Now;
                        break;
                    case 7: // كل الفترات
                        dtpFrom.Value = new DateTime(2020, 1, 1);
                        dtpTo.Value = DateTime.Now;
                        break;
                }
            }
            finally
            {
                _isApplyingDatePreset = false;
            }
            if (cboMode != null && cboMode.SelectedIndex == 0)
                LoadPurchasesGrid();
        }

        private void ResetAllFilters()
        {
            _isLoadingPurchases = true;
            try
            {
                if (cboSupplier != null)
                {
                    if (cboSupplier.Tag is List<ComboItem> allSups)
                    {
                        cboSupplier.BeginUpdate();
                        cboSupplier.Items.Clear();
                        cboSupplier.Items.AddRange(allSups.ToArray());
                        cboSupplier.EndUpdate();
                    }
                    if (cboSupplier.Items.Count > 0) cboSupplier.SelectedIndex = 0;
                }
                if (cboPurchaseTypeFilter != null && cboPurchaseTypeFilter.Items.Count > 0)
                    cboPurchaseTypeFilter.SelectedIndex = 0;
                if (cboReturnStatusFilter != null && cboReturnStatusFilter.Items.Count > 0)
                    cboReturnStatusFilter.SelectedIndex = 0;
                if (cboProductFilter != null)
                {
                    if (cboProductFilter.Tag is List<ComboItem> allProds)
                    {
                        cboProductFilter.BeginUpdate();
                        cboProductFilter.Items.Clear();
                        cboProductFilter.Items.AddRange(allProds.ToArray());
                        cboProductFilter.EndUpdate();
                    }
                    if (cboProductFilter.Items.Count > 0) cboProductFilter.SelectedIndex = 0;
                }
                if (cboWarehouse != null && cboWarehouse.Enabled && cboWarehouse.Items.Count > 0)
                    cboWarehouse.SelectedIndex = 0;
                if (txtSearch != null)
                    txtSearch.Text = "";
                if (txtItemFilter != null)
                    txtItemFilter.Text = "";
                if (cboDatePreset != null)
                {
                    _isApplyingDatePreset = true;
                    cboDatePreset.SelectedIndex = 0;
                    dtpFrom.Value = DateTime.Today.AddMonths(-1);
                    dtpTo.Value = DateTime.Now;
                    _isApplyingDatePreset = false;
                }
            }
            finally
            {
                _isLoadingPurchases = false;
            }
            LoadPurchasesGrid();
        }

        private void FilterInvoiceItems()
        {
            if (dgItems == null) return;
            string q = txtItemFilter != null ? txtItemFilter.Text.Trim() : "";
            int visibleCount = 0;
            int totalCount = dgItems.Rows.Count;

            dgItems.CurrentCell = null;
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (string.IsNullOrEmpty(q))
                {
                    row.Visible = true;
                    visibleCount++;
                }
                else
                {
                    string pName = row.Cells["ProductName"]?.Value?.ToString() ?? "";
                    string pCode = dgItems.Columns.Contains("ProductCode") ? (row.Cells["ProductCode"]?.Value?.ToString() ?? "") : "";
                    string pBar = dgItems.Columns.Contains("Barcode") ? (row.Cells["Barcode"]?.Value?.ToString() ?? "") : "";
                    string pImei = dgItems.Columns.Contains("IMEI") ? (row.Cells["IMEI"]?.Value?.ToString() ?? "") : "";
                    string pId = row.Cells["ProductID"]?.Value?.ToString() ?? "";

                    bool match = pName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                              || pCode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                              || pBar.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                              || pImei.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                              || pId.Contains(q);

                    row.Visible = match;
                    if (match) visibleCount++;
                }
            }

            if (lblItemsCountInfo != null)
            {
                if (totalCount == 0)
                    lblItemsCountInfo.Text = "";
                else if (string.IsNullOrEmpty(q))
                    lblItemsCountInfo.Text = $"إجمالي البنود: {totalCount}";
                else
                    lblItemsCountInfo.Text = $"المطابق: {visibleCount} من {totalCount}";
            }
        }

        private void OpenSupplierSearchDialog()
        {
            using (var dlg = new Form
            {
                Text = "🔍 بحث سريع عن مورد / جهة شراء",
                Size = new Size(640, 480),
                StartPosition = FormStartPosition.CenterParent,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgMain,
                Font = Theme.FontMain,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            })
            {
                var pnlTop = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(10), BackColor = Theme.BgCard };
                var lbl = new Label { Text = "ابحث بالاسم أو الكود أو الهاتف:", AutoSize = true, Location = new Point(12, 16), Font = Theme.FontBold };
                var txt = new TextBox { Location = new Point(210, 12), Width = 400, Height = 28, Font = Theme.FontBold, BorderStyle = BorderStyle.FixedSingle };
                pnlTop.Controls.AddRange(new Control[] { lbl, txt });

                var dg = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = Theme.BgCard,
                    BorderStyle = BorderStyle.None,
                    RowHeadersVisible = false,
                    AllowUserToAddRows = false,
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    ColumnHeadersHeight = 34,
                    EnableHeadersVisualStyles = false
                };
                dg.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(35, 65, 110), ForeColor = Color.White, Font = Theme.FontBold };
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierID", Visible = false });
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierCode", HeaderText = "كود المورد", FillWeight = 45 });
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "اسم المورد", FillWeight = 120 });
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "الهاتف", FillWeight = 70 });

                var dtSups = SupplierDAL.GetAll(true);
                Action<string> populate = (filter) =>
                {
                    dg.Rows.Clear();
                    dg.Rows.Add(0, "-", "-- الكل (جميع الموردين والعملاء) --", "");
                    foreach (DataRow r in dtSups.Rows)
                    {
                        string sName = r["SupplierName"]?.ToString() ?? "";
                        string sCode = r.Table.Columns.Contains("SupplierCode") && r["SupplierCode"] != DBNull.Value ? r["SupplierCode"].ToString() : "";
                        string sPhone = r.Table.Columns.Contains("Phone") && r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "";
                        if (!string.IsNullOrEmpty(filter))
                        {
                            if (sName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                                sCode.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                                sPhone.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                                continue;
                        }
                        dg.Rows.Add(r["SupplierID"], sCode, sName, sPhone);
                    }
                };
                populate("");
                txt.TextChanged += (s, e) => populate(txt.Text.Trim());

                Action selectCurrent = () =>
                {
                    if (dg.CurrentRow == null) return;
                    int supId = Convert.ToInt32(dg.CurrentRow.Cells["SupplierID"].Value);
                    if (cboSupplier.Tag is List<ComboItem> allSups)
                    {
                        cboSupplier.BeginUpdate();
                        cboSupplier.Items.Clear();
                        cboSupplier.Items.AddRange(allSups.ToArray());
                        cboSupplier.EndUpdate();
                    }
                    for (int i = 0; i < cboSupplier.Items.Count; i++)
                    {
                        if (cboSupplier.Items[i] is ComboItem ci && ci.ID == supId)
                        {
                            cboSupplier.SelectedIndex = i;
                            break;
                        }
                    }
                    dlg.DialogResult = DialogResult.OK;
                };

                dg.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) selectCurrent(); };
                txt.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Down && dg.Rows.Count > 0)
                    {
                        dg.Focus();
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Enter && dg.Rows.Count > 0)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        selectCurrent();
                    }
                };
                dg.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter && dg.CurrentRow != null)
                    {
                        e.Handled = true;
                        selectCurrent();
                    }
                };

                dlg.Controls.Add(dg);
                dlg.Controls.Add(pnlTop);
                Theme.ApplyFormRTL(dlg);
                dlg.Shown += (s, e) => txt.Focus();
                dlg.ShowDialog(this);
            }
        }

        private void ToggleReturnMode()
        {
            bool isGeneral = cboMode.SelectedIndex == 1;

            _mainSplit.Panel1Collapsed = isGeneral;
            pnlGeneralItemBar.Visible = isGeneral;
            if (_pnlInvoiceItemFilter != null) _pnlInvoiceItemFilter.Visible = !isGeneral;

            if (cboDatePreset != null) cboDatePreset.Enabled = !isGeneral;
            dtpFrom.Enabled = !isGeneral;
            dtpTo.Enabled = !isGeneral;
            if (cboPurchaseTypeFilter != null) cboPurchaseTypeFilter.Enabled = !isGeneral;
            if (cboReturnStatusFilter != null) cboReturnStatusFilter.Enabled = !isGeneral;
            if (cboProductFilter != null) cboProductFilter.Enabled = !isGeneral;
            if (btnProductFilterSearch != null) btnProductFilterSearch.Enabled = !isGeneral;
            if (txtSearch != null) txtSearch.Enabled = !isGeneral;
            if (btnSearch != null) btnSearch.Enabled = !isGeneral;
            if (btnClearFilters != null) btnClearFilters.Enabled = !isGeneral;

            if (dgItems.Columns.Contains("NetUnitPrice"))
            {
                dgItems.Columns["NetUnitPrice"].ReadOnly = !isGeneral;
                dgItems.Columns["NetUnitPrice"].DefaultCellStyle.BackColor = isGeneral ? Color.FromArgb(40, 60, 45) : Theme.BgCard;
                dgItems.Columns["NetUnitPrice"].DefaultCellStyle.ForeColor = isGeneral ? Color.LightGreen : Theme.TextMain;
                dgItems.Columns["NetUnitPrice"].DefaultCellStyle.Font = isGeneral ? new Font("Segoe UI", 10f, FontStyle.Bold) : Theme.FontMain;
            }

            if (dgItems.Columns.Contains("GrossUnitPrice"))
            {
                dgItems.Columns["GrossUnitPrice"].ReadOnly = !isGeneral;
                dgItems.Columns["GrossUnitPrice"].DefaultCellStyle.BackColor = isGeneral ? Color.FromArgb(40, 60, 45) : Theme.BgCard;
                dgItems.Columns["GrossUnitPrice"].DefaultCellStyle.ForeColor = isGeneral ? Color.LightGreen : Theme.TextMain;
                dgItems.Columns["GrossUnitPrice"].DefaultCellStyle.Font = isGeneral ? new Font("Segoe UI", 10f, FontStyle.Bold) : Theme.FontMain;
            }

            if (dgItems.Columns.Contains("IMEI"))
            {
                dgItems.Columns["IMEI"].Visible = AppConfig.BusinessType == "Mobiles";
                dgItems.Columns["IMEI"].ReadOnly = !isGeneral;
            }

            dgItems.Rows.Clear();
            RecalcTotal();

            if (isGeneral)
            {
                cboSupplier.Enabled = true;
                lblPurchaseInfo.Text = "مرتجع عام دون التقيد ببيانات فاتورة محددة - يمكنك البحث السريع (F3) والتعديل المباشر للأسعار والكميات في الجدول";
            }
            else
            {
                lblPurchaseInfo.Text = "";
                LoadPurchasesGrid();
            }
        }

        private void LoadCombos()
        {
            var dtS = SupplierDAL.GetAll(true);
            cboSupplier.Items.Clear();
            cboSupplier.Items.Add(new ComboItem(0, "-- الكل (الموردين والعملاء) --"));
            foreach (DataRow r in dtS.Rows)
            {
                string sCode = r.Table.Columns.Contains("SupplierCode") && r["SupplierCode"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["SupplierCode"].ToString())
                    ? $" [{r["SupplierCode"]}]" : "";
                cboSupplier.Items.Add(new ComboItem((int)r["SupplierID"], r["SupplierName"].ToString() + sCode));
            }
            cboSupplier.DisplayMember = "Text";
            cboSupplier.Tag = cboSupplier.Items.Cast<ComboItem>().ToList();
            cboSupplier.SelectedIndex = 0;
            cboSupplier.SelectedIndexChanged += (s, e) => LoadPurchasesGrid();

            // المخازن
            var dtWh = Session.GetAllowedWarehouses(true);
            cboWarehouse.Items.Clear();
            cboWarehouse.Items.Add(new ComboItem(0, "-- الكل (جميع المخازن) --"));
            for (int i = 0; i < dtWh.Rows.Count; i++)
            {
                int wid = Convert.ToInt32(dtWh.Rows[i]["WarehouseID"]);
                string wname = dtWh.Rows[i]["WarehouseName"].ToString();
                cboWarehouse.Items.Add(new ComboItem(wid, wname));
            }
            cboWarehouse.DisplayMember = "Text";
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
            cboWarehouse.Enabled = Session.IsAdmin || dtWh.Rows.Count > 1;
            cboWarehouse.SelectedIndexChanged += (s, e) =>
            {
                UpdateAvailableStockColumn();
                if (cboMode != null && cboMode.SelectedIndex == 0)
                    LoadPurchasesGrid();
            };

            // جميع الأصناف لفلتر البحث وللمرتجع العام
            var dtProd = ProductDAL.GetAll(true);
            cboProductFilter.Items.Clear();
            cboProductFilter.Items.Add(new ComboItem(0, "-- الكل (جميع الأصناف) --"));

            cboAllProducts.Items.Clear();
            cboAllProducts.Items.Add(new ComboItem(0, "-- اختر صنف --"));
            foreach (DataRow r in dtProd.Rows)
            {
                int pid = (int)r["ProductID"];
                string pname = r["ProductName"].ToString();
                string pcode = r.Table.Columns.Contains("ProductCode") && r["ProductCode"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["ProductCode"].ToString())
                    ? $" [{r["ProductCode"]}]" : "";

                cboProductFilter.Items.Add(new ComboItem(pid, pname + pcode));

                var ci = new ComboItem(pid, pname + pcode);
                ci.Extra = r["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                cboAllProducts.Items.Add(ci);
            }
            cboProductFilter.DisplayMember = "Text";
            cboProductFilter.Tag = cboProductFilter.Items.Cast<ComboItem>().ToList();
            if (cboProductFilter.Items.Count > 0) cboProductFilter.SelectedIndex = 0;

            cboAllProducts.DisplayMember = "Text";
            cboAllProducts.Tag = cboAllProducts.Items.Cast<ComboItem>().ToList();
            if (cboAllProducts.Items.Count > 0) cboAllProducts.SelectedIndex = 0;
            cboAllProducts.SelectedIndexChanged += (s, e) =>
            {
                if (cboAllProducts.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    txtGenPrice.Text = ci.Extra.ToString("N2");
                }
            };
        }

        private void UpdateAvailableStockColumn()
        {
            if (dgItems == null || !dgItems.Columns.Contains("AvailableStock")) return;
            int whId = (cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? cw.ID : Session.GetDefaultWarehouseID();
            foreach (DataGridViewRow r in dgItems.Rows)
            {
                if (r.Cells["ProductID"].Value != null && int.TryParse(r.Cells["ProductID"].Value.ToString(), out int pid))
                {
                    decimal stock = InventoryDAL.GetProductStock(pid, whId);
                    r.Cells["AvailableStock"].Value = stock.ToString("0.##");
                }
            }
        }

        private void LoadPurchasesGrid()
        {
            if (_isLoadingPurchases || _isFilteringCombo) return;
            if (cboMode.SelectedIndex == 1) return; // General Return

            _isLoadingPurchases = true;
            try
            {
                int? supplierID = null;
                if (cboSupplier.SelectedItem is ComboItem cs && cs.ID > 0)
                    supplierID = cs.ID;

                int? warehouseID = (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? (int?)cw.ID : null;

                int? filterProductID = null;
                if (cboProductFilter != null && cboProductFilter.SelectedItem is ComboItem cp && cp.ID > 0)
                    filterProductID = cp.ID;

                string purchaseTypeFilter = null;
                if (cboPurchaseTypeFilter != null && cboPurchaseTypeFilter.SelectedIndex > 0)
                {
                    purchaseTypeFilter = cboPurchaseTypeFilter.SelectedIndex == 1 ? "Cash" : "Credit";
                }

                string searchStr = txtSearch != null ? txtSearch.Text.Trim() : null;
                if (string.IsNullOrEmpty(searchStr)) searchStr = null;

                var dtP = PurchaseDAL.GetAll(dtpFrom.Value.Date, dtpTo.Value.Date, supplierID, searchStr, warehouseID, purchaseType: purchaseTypeFilter, productID: filterProductID);

                int retStatusIdx = cboReturnStatusFilter != null ? cboReturnStatusFilter.SelectedIndex : 0;

                dgPurchases.SelectionChanged -= DgPurchases_SelectionChanged;
                dgPurchases.Rows.Clear();
                dgItems.Rows.Clear();
                lblTotal.Text = "إجمالي مرتجع الشراء: 0.00 ج";
                lblPurchaseInfo.Text = "";

                foreach (DataRow r in dtP.Rows)
                {
                    decimal prevRetAmt = r.Table.Columns.Contains("ReturnAmount") && r["ReturnAmount"] != DBNull.Value
                        ? Convert.ToDecimal(r["ReturnAmount"]) : 0m;

                    if (retStatusIdx == 1 && prevRetAmt > 0m) continue; // بدون مرتجع سابق
                    if (retStatusIdx == 2 && prevRetAmt <= 0m) continue; // عليها مرتجع سابق

                    int rowIdx = dgPurchases.Rows.Add();
                    var row = dgPurchases.Rows[rowIdx];
                    row.Cells["PurchaseID"].Value        = r["PurchaseID"];
                    if (dgPurchases.Columns.Contains("WarehouseID") && r.Table.Columns.Contains("WarehouseID"))
                        row.Cells["WarehouseID"].Value   = r["WarehouseID"];
                    row.Cells["PurchaseCode"].Value      = r["PurchaseCode"];
                    row.Cells["SupplierInvoiceNo"].Value = r["SupplierInvoiceNo"] != DBNull.Value ? r["SupplierInvoiceNo"].ToString() : "";
                    row.Cells["PurchaseDate"].Value      = Convert.ToDateTime(r["PurchaseDate"]).ToString("dd/MM/yyyy HH:mm");
                    row.Cells["SupplierName"].Value      = r["SupplierName"];
                    row.Cells["SupplierCode"].Value      = r.Table.Columns.Contains("SupplierCode") && r["SupplierCode"] != DBNull.Value ? r["SupplierCode"].ToString() : "";
                    if (dgPurchases.Columns.Contains("WarehouseName"))
                        row.Cells["WarehouseName"].Value = r.Table.Columns.Contains("WarehouseName") && r["WarehouseName"] != DBNull.Value ? r["WarehouseName"].ToString() : "";
                    row.Cells["PurchaseTypeStr"].Value   = r["PurchaseType"].ToString() == "Cash" ? "🟡 نقدي" : "🔵 آجل";
                    row.Cells["SubTotal"].Value          = Convert.ToDecimal(r["SubTotal"]).ToString("N2");
                    row.Cells["DiscountAmount"].Value    = Convert.ToDecimal(r["DiscountAmount"]).ToString("N2");
                    row.Cells["TotalAmount"].Value       = Convert.ToDecimal(r["TotalAmount"]).ToString("N2");
                    if (dgPurchases.Columns.Contains("ReturnAmount"))
                        row.Cells["ReturnAmount"].Value  = prevRetAmt > 0 ? prevRetAmt.ToString("N2") : "-";
                    row.Cells["Notes"].Value             = r["Notes"] != DBNull.Value ? r["Notes"].ToString() : "";

                    if (prevRetAmt > 0)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 235);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(146, 64, 14);
                    }
                }

                if (lblTopTitle != null)
                {
                    lblTopTitle.Text = $"📋 فواتير الشراء المطابقة للبحث ({dgPurchases.Rows.Count} فاتورة) — اختر الفاتورة لعرض أصنافها بالأسفل:";
                }

                dgPurchases.SelectionChanged += DgPurchases_SelectionChanged;
                if (dgPurchases.Rows.Count > 0)
                {
                    dgPurchases.ClearSelection();
                    dgPurchases.Rows[0].Selected = true;
                    if (dgPurchases.Columns.Contains("PurchaseCode"))
                        dgPurchases.CurrentCell = dgPurchases.Rows[0].Cells["PurchaseCode"];
                    DgPurchases_SelectionChanged(dgPurchases, EventArgs.Empty);
                }
                else
                {
                    FilterInvoiceItems();
                }
            }
            finally
            {
                _isLoadingPurchases = false;
            }
        }

        private void DgPurchases_SelectionChanged(object sender, EventArgs e)
        {
            if (cboMode.SelectedIndex == 1) return; // General return

            dgItems.Rows.Clear();
            lblTotal.Text = "إجمالي مرتجع الشراء: 0.00 ج";
            lblPurchaseInfo.Text = "";

            if (dgPurchases.SelectedRows.Count == 0 && dgPurchases.CurrentRow == null)
            {
                _selectedPurchaseID = 0;
                FilterInvoiceItems();
                return;
            }

            var selectedRow = dgPurchases.SelectedRows.Count > 0 ? dgPurchases.SelectedRows[0] : dgPurchases.CurrentRow;
            int purchaseID = Convert.ToInt32(selectedRow.Cells["PurchaseID"].Value);
            _selectedPurchaseID = purchaseID;

            LoadPurchaseItems(purchaseID, selectedRow);
        }

        private void LoadPurchaseItems(int purchaseID, DataGridViewRow purRow)
        {
            dgItems.Rows.Clear();

            // حساب معامل خصم الفاتورة الإجمالي (Header Discount Factor)
            decimal subTotal = Convert.ToDecimal(purRow.Cells["SubTotal"].Value ?? 0);
            decimal headerDisc = Convert.ToDecimal(purRow.Cells["DiscountAmount"].Value ?? 0);
            decimal headerFactor = (subTotal > 0 && headerDisc > 0) ? ((subTotal - headerDisc) / subTotal) : 1.0m;

            string purTypeStr = purRow.Cells["PurchaseTypeStr"].Value?.ToString() ?? "";
            decimal netPurTotal = Convert.ToDecimal(purRow.Cells["TotalAmount"].Value ?? 0);
            string supName = purRow.Cells["SupplierName"].Value?.ToString() ?? "";
            lblPurchaseInfo.Text = $"الفاتورة رقم ({purRow.Cells["PurchaseCode"].Value}) | المورد: {supName} | الصافي المسدد: {netPurTotal:N2} ج | النوع: {purTypeStr}";

            // ضبط طريقة التسوية التلقائية للمرتجع
            if (purTypeStr.Contains("نقدي") || purTypeStr.Contains("Cash"))
                cboReturnType.SelectedIndex = 1; // نقدي
            else if (purTypeStr.Contains("فيزا") || purTypeStr.Contains("بنك") || purTypeStr.Contains("Visa"))
                cboReturnType.SelectedIndex = 2; // فيزا / بنكي
            else
                cboReturnType.SelectedIndex = 0; // آجل

            // حفظ معرف المورد للفاتورة المحددة دون تغيير فلتر الفواتير المعروضة
            var dtPur = DbHelper.Query(
                "SELECT SupplierID, ClientID, PurchaseSource, WarehouseID FROM Purchases WHERE PurchaseID=@pid",
                DbHelper.P("@pid", purchaseID));
            int invoiceWhId = Session.GetDefaultWarehouseID();
            if (dtPur.Rows.Count > 0)
            {
                if (dtPur.Rows[0]["SupplierID"] != DBNull.Value)
                    _selectedSupplierID = Convert.ToInt32(dtPur.Rows[0]["SupplierID"]);
                else
                    _selectedSupplierID = null;

                if (dtPur.Rows[0]["WarehouseID"] != DBNull.Value && Convert.ToInt32(dtPur.Rows[0]["WarehouseID"]) > 0)
                    invoiceWhId = Convert.ToInt32(dtPur.Rows[0]["WarehouseID"]);
            }
            else
            {
                _selectedSupplierID = null;
            }

            // تحميل أصناف الفاتورة مع المرتجع السابق
            var dtItems = PurchaseDAL.GetItems(purchaseID);
            var dtPrevRet = DbHelper.Query(
                @"SELECT pri.ProductID, 
                         ISNULL(SUM(pri.Quantity),0) AS ReturnedQty,
                         ISNULL(SUM(pri.BonusQuantity),0) AS ReturnedBonusQty
                  FROM PurchaseReturnItems pri
                  JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                  WHERE pr.PurchaseID = @pid
                  GROUP BY pri.ProductID",
                DbHelper.P("@pid", purchaseID));

            var prevMap = new Dictionary<int, (decimal ReturnedQty, decimal ReturnedBonusQty)>();
            foreach (DataRow r in dtPrevRet.Rows)
            {
                prevMap[Convert.ToInt32(r["ProductID"])] = (
                    Convert.ToDecimal(r["ReturnedQty"]),
                    Convert.ToDecimal(r["ReturnedBonusQty"])
                );
            }

            foreach (DataRow r in dtItems.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                decimal purQty = Convert.ToDecimal(r["Quantity"]);
                decimal purBonus = r.Table.Columns.Contains("BonusQuantity") && r["BonusQuantity"] != DBNull.Value 
                    ? Convert.ToDecimal(r["BonusQuantity"]) : 0m;

                decimal prevRet = prevMap.ContainsKey(pid) ? prevMap[pid].ReturnedQty : 0m;
                decimal prevRetBonus = prevMap.ContainsKey(pid) ? prevMap[pid].ReturnedBonusQty : 0m;

                decimal remainingQty = purQty - prevRet;
                decimal remainingBonus = purBonus - prevRetBonus;

                if (remainingQty <= 0 && remainingBonus <= 0) continue; // تم إرجاع الفاتورة والبونص بالكامل مسبقاً

                decimal grossUnitPrice = Convert.ToDecimal(r["UnitPrice"]);
                // اعتماد سعر الشراء الفعلي المباشر للفاتورة دون تخفيضه بخصومات إضافية
                decimal netUnitPrice = grossUnitPrice;
                decimal itemDiscPct = r.Table.Columns.Contains("DiscountPct") && r["DiscountPct"] != DBNull.Value 
                    ? Convert.ToDecimal(r["DiscountPct"]) : 0m;

                int rowIdx = dgItems.Rows.Add();
                var row = dgItems.Rows[rowIdx];
                row.Cells["ProductID"].Value           = pid;
                if (dgItems.Columns.Contains("ProductCode") && r.Table.Columns.Contains("ProductCode"))
                    row.Cells["ProductCode"].Value     = r["ProductCode"]?.ToString() ?? "";
                if (dgItems.Columns.Contains("Barcode") && r.Table.Columns.Contains("Barcode"))
                    row.Cells["Barcode"].Value         = r["Barcode"]?.ToString() ?? "";
                row.Cells["ProductName"].Value         = r["ProductName"].ToString();

                string pImei = r.Table.Columns.Contains("IMEI") && r["IMEI"] != DBNull.Value ? r["IMEI"].ToString().Trim() : "";
                if (dgItems.Columns.Contains("IMEI"))
                {
                    row.Cells["IMEI"].Value = pImei;
                    row.Cells["IMEI"].ReadOnly = true;
                    dgItems.Columns["IMEI"].Visible = (AppConfig.BusinessType == "Mobiles");
                }

                row.Cells["PurchasedQty"].Value        = purQty.ToString("N3");
                row.Cells["PrevReturnedQty"].Value     = prevRet.ToString("N3");

                int curWhId = (cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? cw.ID : invoiceWhId;
                decimal currentStock = InventoryDAL.GetProductStock(pid, curWhId);
                row.Cells["AvailableStock"].Value      = currentStock.ToString("0.##");

                row.Cells["NewReturnedQty"].Value      = 0;
                row.Cells["PurchasedBonusQty"].Value   = purBonus.ToString("N3");
                row.Cells["PrevReturnedBonusQty"].Value = prevRetBonus.ToString("N3");
                row.Cells["NewReturnedBonusQty"].Value  = 0;
                row.Cells["GrossUnitPrice"].Value      = grossUnitPrice.ToString("N2");
                row.Cells["DiscountPct"].Value         = itemDiscPct > 0 ? (itemDiscPct.ToString("N2") + "%") : "0%";
                row.Cells["NetUnitPrice"].Value        = netUnitPrice.ToString("N2");
                row.Cells["TotalPrice"].Value          = "0.00";
                row.Cells["UnitName"].Value            = r["UnitName"]?.ToString() ?? "";
                row.Cells["Factor"].Value              = r["Factor"] != DBNull.Value ? r["Factor"] : 1.0m;
            }

            FilterInvoiceItems();

            if (cboProductFilter != null && cboProductFilter.SelectedItem is ComboItem filterProd && filterProd.ID > 0)
            {
                foreach (DataGridViewRow r in dgItems.Rows)
                {
                    if (r.Visible && r.Cells["ProductID"].Value != null && Convert.ToInt32(r.Cells["ProductID"].Value) == filterProd.ID)
                    {
                        dgItems.CurrentCell = r.Cells["NewReturnedQty"];
                        r.Selected = true;
                        return;
                    }
                }
            }

            foreach (DataGridViewRow r in dgItems.Rows)
            {
                if (r.Visible)
                {
                    dgItems.CurrentCell = r.Cells["NewReturnedQty"];
                    break;
                }
            }
        }

        private void OpenQuickSearch()
        {
            int? whId = null;
            if (cboWarehouse.SelectedItem is ComboItem w && w.ID > 0) whId = w.ID;

            using (var dlg = new FrmProductSearch(whId, isPurchaseMode: true, defaultShowZeroStock: true))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedProductID > 0)
                {
                    decimal qty = dlg.SelectedQuantity > 0 ? dlg.SelectedQuantity : 1m;
                    decimal price = dlg.SelectedPurchasePrice > 0 ? dlg.SelectedPurchasePrice : dlg.SelectedPrice;
                    AddGeneralProductByID(dlg.SelectedProductID, qty, price > 0 ? price : (decimal?)null);
                }
            }
        }

        private void AddGeneralProductByID(int productID, decimal defaultQty = 1m, decimal? customPrice = null)
        {
            var dtP = DbHelper.Query(@"
                SELECT ProductID, ProductName, 
                       COALESCE(PurchasePrice, Unit1PurchasePrice, 0) AS PurchasePrice, 
                       COALESCE(Unit, Unit1Name, DefaultPurchaseUnit, '') AS UnitName 
                FROM Products 
                WHERE ProductID=@id", DbHelper.P("@id", productID));
            if (dtP == null || dtP.Rows.Count == 0) return;

            var r = dtP.Rows[0];
            string name = r["ProductName"].ToString();
            string unitName = r["UnitName"]?.ToString() ?? "";
            decimal price = 0m;
            if (customPrice.HasValue && customPrice.Value > 0)
            {
                price = customPrice.Value;
            }
            else if (r["PurchasePrice"] != DBNull.Value)
            {
                price = Convert.ToDecimal(r["PurchasePrice"]);
            }

            // Check if already in dgItems
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["ProductID"].Value != null && Convert.ToInt32(row.Cells["ProductID"].Value) == productID)
                {
                    decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal curQty);
                    decimal newQty = curQty + defaultQty;
                    row.Cells["NewReturnedQty"].Value = newQty;

                    if (price > 0 && (row.Cells["NetUnitPrice"].Value == null || Convert.ToDecimal(row.Cells["NetUnitPrice"].Value) == 0))
                    {
                        row.Cells["GrossUnitPrice"].Value = price.ToString("N2");
                        row.Cells["NetUnitPrice"].Value = price.ToString("N2");
                    }

                    decimal.TryParse(row.Cells["NetUnitPrice"].Value?.ToString(), out decimal curPrice);
                    row.Cells["TotalPrice"].Value = (newQty * curPrice).ToString("N2");
                    RecalcTotal();
                    return;
                }
            }

            int rowIdx = dgItems.Rows.Add();
            var newRow = dgItems.Rows[rowIdx];
            newRow.Cells["ProductID"].Value           = productID;
            newRow.Cells["ProductName"].Value         = name;
            newRow.Cells["PurchasedQty"].Value        = "عام";
            newRow.Cells["PrevReturnedQty"].Value     = "0";

            int curWhId = (cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? cw.ID : Session.GetDefaultWarehouseID();
            decimal currentStock = InventoryDAL.GetProductStock(productID, curWhId);
            newRow.Cells["AvailableStock"].Value      = currentStock.ToString("0.##");

            newRow.Cells["NewReturnedQty"].Value      = defaultQty;
            newRow.Cells["PurchasedBonusQty"].Value   = "0";
            newRow.Cells["PrevReturnedBonusQty"].Value = "0";
            newRow.Cells["NewReturnedBonusQty"].Value  = 0;
            newRow.Cells["GrossUnitPrice"].Value      = price.ToString("N2");
            newRow.Cells["DiscountPct"].Value         = "0%";
            newRow.Cells["NetUnitPrice"].Value        = price.ToString("N2");
            newRow.Cells["TotalPrice"].Value          = (defaultQty * price).ToString("N2");
            newRow.Cells["UnitName"].Value            = unitName;
            newRow.Cells["Factor"].Value              = 1.0m;

            RecalcTotal();

            if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
            {
                dgItems.CurrentCell = newRow.Cells["NewReturnedQty"];
                dgItems.FirstDisplayedScrollingRowIndex = rowIdx;
            }
        }

        private void AddGeneralProductWithIMEI(int productID, string imei)
        {
            foreach (DataGridViewRow r in dgItems.Rows)
            {
                string curImei = r.Cells["IMEI"]?.Value?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(curImei) && (curImei == imei || IsPairedImeiMatch(curImei, imei)))
                {
                    r.Cells["NewReturnedQty"].Value = 1m;
                    dgItems.CurrentCell = r.Cells["NewReturnedQty"];
                    RecalcTotal();
                    return;
                }
            }

            var dtP = DbHelper.Query(@"
                SELECT ProductID, ProductName, 
                       COALESCE(PurchasePrice, Unit1PurchasePrice, 0) AS PurchasePrice, 
                       COALESCE(Unit, Unit1Name, DefaultPurchaseUnit, '') AS UnitName 
                FROM Products 
                WHERE ProductID=@id", DbHelper.P("@id", productID));
            if (dtP == null || dtP.Rows.Count == 0) return;

            var rP = dtP.Rows[0];
            string name = rP["ProductName"].ToString();
            string unitName = rP["UnitName"]?.ToString() ?? "";
            decimal price = rP["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(rP["PurchasePrice"]) : 0m;

            int rowIdx = dgItems.Rows.Add();
            var row = dgItems.Rows[rowIdx];
            row.Cells["ProductID"].Value           = productID;
            row.Cells["ProductName"].Value         = name;
            row.Cells["IMEI"].Value                = imei;
            row.Cells["PurchasedQty"].Value        = "عام";
            row.Cells["PrevReturnedQty"].Value     = "0.000";

            int curWhId = (cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? cw.ID : Session.GetDefaultWarehouseID();
            decimal currentStock = InventoryDAL.GetProductStock(productID, curWhId);
            row.Cells["AvailableStock"].Value      = currentStock.ToString("0.##");

            row.Cells["NewReturnedQty"].Value      = 1m;
            row.Cells["PurchasedBonusQty"].Value   = "0.000";
            row.Cells["PrevReturnedBonusQty"].Value = "0.000";
            row.Cells["NewReturnedBonusQty"].Value  = 0;
            row.Cells["GrossUnitPrice"].Value      = price.ToString("N2");
            row.Cells["DiscountPct"].Value         = "0%";
            row.Cells["NetUnitPrice"].Value        = price.ToString("N2");
            row.Cells["TotalPrice"].Value          = price.ToString("N2");
            row.Cells["UnitName"].Value            = unitName;
            row.Cells["Factor"].Value              = 1.0m;

            dgItems.Columns["IMEI"].Visible = (AppConfig.BusinessType == "Mobiles");
            RecalcTotal();

            if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
            {
                dgItems.CurrentCell = row.Cells["NewReturnedQty"];
                dgItems.FirstDisplayedScrollingRowIndex = rowIdx;
            }
        }

        private static (int PurchaseID, DateTime PurchaseDate, int ProductID, string IMEI)? FindPurchaseByIMEI(string imei)
        {
            string sql = @"
                SELECT TOP 1 p.PurchaseID, p.PurchaseDate, pi.ProductID, LTRIM(RTRIM(pi.IMEI)) AS IMEI
                FROM PurchaseItems pi
                JOIN Purchases p ON pi.PurchaseID = p.PurchaseID
                WHERE LTRIM(RTRIM(pi.IMEI)) = @imei
                ORDER BY p.PurchaseDate DESC, p.PurchaseID DESC";
            var dt = DbHelper.Query(sql, DbHelper.P("@imei", imei));
            if (dt.Rows.Count > 0)
            {
                return (Convert.ToInt32(dt.Rows[0]["PurchaseID"]), Convert.ToDateTime(dt.Rows[0]["PurchaseDate"]), Convert.ToInt32(dt.Rows[0]["ProductID"]), dt.Rows[0]["IMEI"]?.ToString() ?? imei);
            }

            if (imei.Length == 15 && imei.All(char.IsDigit))
            {
                var candidates = PurchaseDAL.GetCandidatePairedIMEIs(imei);
                foreach (var c in candidates)
                {
                    var dtC = DbHelper.Query(sql, DbHelper.P("@imei", c));
                    if (dtC.Rows.Count > 0)
                    {
                        return (Convert.ToInt32(dtC.Rows[0]["PurchaseID"]), Convert.ToDateTime(dtC.Rows[0]["PurchaseDate"]), Convert.ToInt32(dtC.Rows[0]["ProductID"]), dtC.Rows[0]["IMEI"]?.ToString() ?? c);
                    }
                }
            }
            return null;
        }

        private static bool IsPairedImeiMatch(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            if (a.Length == 15 && b.Length == 15)
            {
                var candidates = PurchaseDAL.GetCandidatePairedIMEIs(a);
                return candidates.Contains(b);
            }
            return false;
        }

        private void DoBarcodeSearch(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            code = code.Trim();
            if (code.StartsWith("*") && code.EndsWith("*") && code.Length > 2)
                code = code.Substring(1, code.Length - 2).Trim();

            try
            {
                // 1. فحص هل الكود يمثل سيريال جهاز (IMEI)
                string cleanImei = PurchaseDAL.SanitizeIMEIString(code);
                bool isPotentialImei = AppConfig.BusinessType == "Mobiles" && cleanImei.Length >= 14 && cleanImei.Length <= 16 && cleanImei.All(char.IsDigit);
                if (isPotentialImei)
                {
                    // أ) إذا كنا في وضع فاتورة ومحددة فاتورة حالياً:
                    if (cboMode.SelectedIndex == 0 && dgPurchases.SelectedRows.Count > 0)
                    {
                        bool foundInGrid = false;
                        foreach (DataGridViewRow r in dgItems.Rows)
                        {
                            string rowImei = r.Cells["IMEI"]?.Value?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(rowImei) && (rowImei == cleanImei || IsPairedImeiMatch(rowImei, cleanImei)))
                            {
                                dgItems.Focus();
                                dgItems.CurrentCell = r.Cells["NewReturnedQty"];
                                r.Cells["NewReturnedQty"].Value = 1m;
                                r.Selected = true;
                                foundInGrid = true;
                                RecalcTotal();
                                SoundAlertHelper.PlayBeep();
                                break;
                            }
                        }
                        if (foundInGrid)
                        {
                            if (txtBarcode != null) txtBarcode.Text = "";
                            return;
                        }
                    }

                    // ب) البحث في فواتير الشراء السابقة عن فاتورة الشراء الأصلية للجهاز
                    var purMatch = FindPurchaseByIMEI(cleanImei);
                    if (purMatch != null)
                    {
                        int targetPurchaseID = purMatch.Value.PurchaseID;
                        DateTime purDate = purMatch.Value.PurchaseDate;

                        if (cboMode.SelectedIndex != 0)
                            cboMode.SelectedIndex = 0;

                        if (purDate < dtpFrom.Value) dtpFrom.Value = purDate.Date.AddDays(-1);
                        if (purDate > dtpTo.Value) dtpTo.Value = purDate.Date.AddDays(1);
                        if (txtSearch != null) txtSearch.Text = "";

                        LoadPurchasesGrid();

                        bool foundPur = false;
                        dgPurchases.SelectionChanged -= DgPurchases_SelectionChanged;
                        foreach (DataGridViewRow row in dgPurchases.Rows)
                        {
                            if (row.Cells["PurchaseID"].Value != null && Convert.ToInt32(row.Cells["PurchaseID"].Value) == targetPurchaseID)
                            {
                                dgPurchases.CurrentCell = row.Cells["PurchaseCode"];
                                row.Selected = true;
                                foundPur = true;
                                break;
                            }
                        }
                        dgPurchases.SelectionChanged += DgPurchases_SelectionChanged;

                        if (foundPur)
                        {
                            DgPurchases_SelectionChanged(dgPurchases, EventArgs.Empty);

                            foreach (DataGridViewRow r in dgItems.Rows)
                            {
                                string rowImei = r.Cells["IMEI"]?.Value?.ToString()?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(rowImei) && (rowImei == cleanImei || IsPairedImeiMatch(rowImei, cleanImei)))
                                {
                                    dgItems.Focus();
                                    dgItems.CurrentCell = r.Cells["NewReturnedQty"];
                                    r.Cells["NewReturnedQty"].Value = 1m;
                                    r.Selected = true;
                                    RecalcTotal();
                                    break;
                                }
                            }
                            SoundAlertHelper.PlayBeep();
                        }
                        if (txtBarcode != null) txtBarcode.Text = "";
                        return;
                    }

                    // ج) إذا كنا في وضع مرتجع عام أو جهاز مسجل
                    if (cboMode.SelectedIndex == 1)
                    {
                        var prodMatch = PurchaseDAL.FindProductByAvailableIMEI(cleanImei);
                        int targetPid = 0;
                        string matchedImei = cleanImei;
                        if (prodMatch.HasValue)
                        {
                            targetPid = prodMatch.Value.ProductID;
                            matchedImei = prodMatch.Value.IMEI;
                        }
                        else
                        {
                            var dtPI = DbHelper.Query("SELECT TOP 1 ProductID, IMEI FROM PurchaseItems WHERE LTRIM(RTRIM(IMEI)) = @imei ORDER BY ItemID DESC", DbHelper.P("@imei", cleanImei));
                            if (dtPI.Rows.Count > 0)
                            {
                                targetPid = Convert.ToInt32(dtPI.Rows[0]["ProductID"]);
                                matchedImei = dtPI.Rows[0]["IMEI"]?.ToString()?.Trim() ?? cleanImei;
                            }
                        }

                        if (targetPid > 0)
                        {
                            AddGeneralProductWithIMEI(targetPid, matchedImei);
                            SoundAlertHelper.PlayBeep();
                            if (txtBarcode != null) txtBarcode.Text = "";
                            return;
                        }
                    }

                    if (cboMode.SelectedIndex == 0)
                    {
                        MessageBox.Show($"لم يتم العثور على فاتورة شراء تحتوي على الجهاز ذو السيريال [{cleanImei}].\nيمكنك إرجاعه للمورد عبر وضع (مرتجع عام بدون فاتورة).", "تنبيه السيريال", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (txtBarcode != null) txtBarcode.Text = "";
                        return;
                    }
                }

                // 2. فحص هل الكود يمثل رقم فاتورة شراء
                var dt = DbHelper.Query(@"
                    SELECT p.PurchaseID, p.PurchaseCode, p.PurchaseDate
                    FROM Purchases p
                    WHERE p.PurchaseCode = @code OR CAST(p.PurchaseID AS NVARCHAR(50)) = @code OR p.SupplierInvoiceNo = @code",
                    DbHelper.P("@code", code));

                if (dt.Rows.Count > 0)
                {
                    int targetPurchaseID = Convert.ToInt32(dt.Rows[0]["PurchaseID"]);
                    DateTime purDate = Convert.ToDateTime(dt.Rows[0]["PurchaseDate"]);

                    if (cboMode.SelectedIndex != 0) cboMode.SelectedIndex = 0;
                    if (purDate < dtpFrom.Value) dtpFrom.Value = purDate.Date.AddDays(-1);
                    if (purDate > dtpTo.Value) dtpTo.Value = purDate.Date.AddDays(1);
                    if (txtSearch != null) txtSearch.Text = "";

                    LoadPurchasesGrid();

                    bool found = false;
                    dgPurchases.SelectionChanged -= DgPurchases_SelectionChanged;
                    foreach (DataGridViewRow row in dgPurchases.Rows)
                    {
                        if (row.Cells["PurchaseID"].Value != null && Convert.ToInt32(row.Cells["PurchaseID"].Value) == targetPurchaseID)
                        {
                            dgPurchases.CurrentCell = row.Cells["PurchaseCode"];
                            row.Selected = true;
                            found = true;
                            break;
                        }
                    }
                    dgPurchases.SelectionChanged += DgPurchases_SelectionChanged;

                    if (found)
                    {
                        DgPurchases_SelectionChanged(dgPurchases, EventArgs.Empty);
                        if (dgItems.Rows.Count > 0)
                        {
                            dgItems.Focus();
                            dgItems.CurrentCell = dgItems.Rows[0].Cells["NewReturnedQty"];
                        }
                        SoundAlertHelper.PlayBeep();
                    }
                    if (txtBarcode != null) txtBarcode.Text = "";
                    return;
                }

                // 3. فحص هل الكود يمثل صنفاً (باركود منتج)
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

                    if (cboMode.SelectedIndex == 0 && dgPurchases.SelectedRows.Count > 0)
                    {
                        bool itemFound = false;
                        foreach (DataGridViewRow r in dgItems.Rows)
                        {
                            if (r.Cells["ProductID"].Value != null && Convert.ToInt32(r.Cells["ProductID"].Value) == pid)
                            {
                                dgItems.Focus();
                                dgItems.CurrentCell = r.Cells["NewReturnedQty"];
                                r.Selected = true;
                                itemFound = true;
                                SoundAlertHelper.PlayBeep();
                                break;
                            }
                        }
                        if (!itemFound)
                        {
                            MessageBox.Show($"الصنف [{pname}] غير موجود ضمن بنود الفاتورة المحددة حالياً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        if (txtBarcode != null) txtBarcode.Text = "";
                        return;
                    }
                    else if (cboMode.SelectedIndex == 1)
                    {
                        AddGeneralProductByID(pid, scanQty > 0 ? scanQty : 1m);
                        SoundAlertHelper.PlayBeep();
                        if (txtBarcode != null) txtBarcode.Text = "";
                        return;
                    }
                }

                MessageBox.Show($"عذراً، لم يتم العثور على أي فاتورة شراء أو جهاز أو صنف يطابق الكود [{code}].", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppLogger.Error("خطأ أثناء البحث بالباركود في مرتجع المشتريات", ex, "FrmPurchaseReturn.DoBarcodeSearch");
                MessageBox.Show($"حدث خطأ أثناء البحث:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddGenItem_Click(object sender, EventArgs e)
        {
            if (!(cboAllProducts.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر صنفاً أولاً من القائمة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtGenQty.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("أدخل كمية صالحة أكبر من صفر", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtGenPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("أدخل سعر شراء صالح", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AddGeneralProductByID(ci.ID, qty, price);
            cboAllProducts.SelectedIndex = 0;
            txtGenQty.Text = "1";
            txtGenPrice.Text = "0";
        }

        private void DgItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            string colName = dgItems.Columns[e.ColumnIndex].Name;
            if (colName != "NewReturnedQty" && colName != "NewReturnedBonusQty" && colName != "NetUnitPrice" && colName != "GrossUnitPrice") return;
            if (string.IsNullOrWhiteSpace(e.FormattedValue?.ToString())) return;

            if (!decimal.TryParse(e.FormattedValue.ToString(), out decimal val) || val < 0)
            {
                MessageBox.Show("أدخل قيمة رقمية صالحة أكبر من أو تساوي صفر", "تحقق من الإدخال",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dgItems.Columns[e.ColumnIndex].Name;
            if (colName != "NewReturnedQty" && colName != "NetUnitPrice" && colName != "GrossUnitPrice") return;

            var row = dgItems.Rows[e.RowIndex];
            decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal qty);
            decimal.TryParse(row.Cells["NetUnitPrice"].Value?.ToString(), out decimal netPrice);

            if (colName == "GrossUnitPrice" && cboMode.SelectedIndex == 1)
            {
                decimal.TryParse(row.Cells["GrossUnitPrice"].Value?.ToString(), out decimal grossPrice);
                netPrice = grossPrice;
                row.Cells["NetUnitPrice"].Value = grossPrice.ToString("N2");
            }

            row.Cells["TotalPrice"].Value = (qty * netPrice).ToString("N2");
            RecalcTotal();
        }

        private void RecalcTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["TotalPrice"].Value != null)
                {
                    decimal.TryParse(row.Cells["TotalPrice"].Value.ToString(), out decimal t);
                    total += t;
                }
            }
            lblTotal.Text = "إجمالي مرتجع الشراء: " + total.ToString("N2") + " ج";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("PurchaseReturn"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية حفظ مرتجعات المشتريات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isGeneral = cboMode.SelectedIndex == 1;

            if (!isGeneral && _selectedPurchaseID <= 0)
            {
                MessageBox.Show("يجب اختيار فاتورة الشراء الأصلية من الجدول أعلى أولاً", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var returnItems = new List<PurchaseItemDTO>();
            decimal totalReturnAmount = 0;

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                int prodID  = Convert.ToInt32(row.Cells["ProductID"].Value);
                string name = row.Cells["ProductName"].Value.ToString();
                decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal newQty);
                decimal.TryParse(row.Cells["NewReturnedBonusQty"].Value?.ToString(), out decimal newBonusQty);
                if (newQty <= 0 && newBonusQty <= 0) continue;

                if (!isGeneral)
                {
                    decimal.TryParse(row.Cells["PurchasedQty"].Value?.ToString(), out decimal purQty);
                    decimal.TryParse(row.Cells["PrevReturnedQty"].Value?.ToString(), out decimal prevQty);

                    if (newQty > 0 && (newQty + prevQty > purQty))
                    {
                        MessageBox.Show(
                            $"الكمية المرتجعة للصنف ({name}) تتجاوز الكمية الأصلية بالفاتورة!\n" +
                            $"المشتريات: {purQty:N3} | السابق: {prevQty:N3} | الجديد: {newQty:N3}",
                            "تجاوز الكمية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    decimal.TryParse(row.Cells["PurchasedBonusQty"].Value?.ToString(), out decimal purBonusQty);
                    decimal.TryParse(row.Cells["PrevReturnedBonusQty"].Value?.ToString(), out decimal prevBonusQty);

                    if (newBonusQty > 0 && (newBonusQty + prevBonusQty > purBonusQty))
                    {
                        MessageBox.Show(
                            $"كمية البونص المرتجعة للصنف ({name}) تتجاوز كمية البونص الأصلية بالفاتورة!\n" +
                            $"بونص الفاتورة: {purBonusQty:N3} | مرتجع سابق: {prevBonusQty:N3} | الجديد: {newBonusQty:N3}",
                            "تجاوز كمية البونص", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                decimal.TryParse(row.Cells["NetUnitPrice"].Value?.ToString(), out decimal netPrice);
                string unitName = row.Cells["UnitName"].Value?.ToString() ?? "";
                decimal.TryParse(row.Cells["Factor"].Value?.ToString(), out decimal fac);
                if (fac <= 0) fac = 1.0m;

                returnItems.Add(new PurchaseItemDTO
                {
                    ProductID = prodID,
                    ProductName = name,
                    Quantity = newQty,
                    BonusQuantity = newBonusQty,
                    UnitPrice = netPrice,
                    UnitName = unitName,
                    Factor = fac,
                    IMEI = row.Cells["IMEI"]?.Value?.ToString() ?? ""
                });
                totalReturnAmount += newQty * netPrice;
            }

            if (returnItems.Count == 0)
            {
                MessageBox.Show("يرجى إدخال كمية مرتجعة أو بونص مرتجع صالح (أكبر من صفر) لصنف واحد على الأقل.",
                    "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int purchaseID = isGeneral ? 0 : _selectedPurchaseID;
            int? supplierID = isGeneral 
                ? ((cboSupplier.SelectedItem is ComboItem cs && cs.ID > 0) ? (int?)cs.ID : null)
                : (_selectedSupplierID ?? ((cboSupplier.SelectedItem is ComboItem cs2 && cs2.ID > 0) ? (int?)cs2.ID : null));
            int? warehouseID = (cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? (int?)cw.ID : null;
            if (!warehouseID.HasValue && !isGeneral && dgPurchases.CurrentRow != null && dgPurchases.Columns.Contains("WarehouseID"))
            {
                var whVal = dgPurchases.CurrentRow.Cells["WarehouseID"].Value;
                if (whVal != null && whVal != DBNull.Value && int.TryParse(whVal.ToString(), out int pWhId) && pWhId > 0)
                    warehouseID = pWhId;
            }
            if (!warehouseID.HasValue)
                warehouseID = Session.GetDefaultWarehouseID();
            int effWhId = warehouseID ?? 1;

            // ── 1. التحقق من السيريالات للأجهزة (IMEI) لنشاط الموبايلات ──
            if (AppConfig.BusinessType == "Mobiles" || returnItems.Any(x => !string.IsNullOrWhiteSpace(x.IMEI)))
            {
                foreach (var item in returnItems)
                {
                    if (!string.IsNullOrWhiteSpace(item.IMEI))
                    {
                        string cleanImei = PurchaseDAL.SanitizeIMEIString(item.IMEI);

                        // فحص هل تم بيع الجهاز لعميل
                        if (PurchaseDAL.IsIMEISoldToClient(cleanImei, out string sCode, out string cName, out DateTime sDate))
                        {
                            MessageBox.Show(
                                $"❌ لا يمكن إرجاع الجهاز ذو السيريال [{cleanImei}] ({item.ProductName}) للمورد!\n\n" +
                                $"هذا الجهاز تم بيعه مسبقاً للعميل: [{cName}]\n" +
                                $"بموجب الفاتورة رقم: [{sCode}] بتاريخ [{sDate:yyyy/MM/dd}]\n\n" +
                                $"يجب استرجاع الجهاز أولاً من العميل (مرتجع بيع) ليدخل المخزن قبل إمكانية إرجاعه للمورد.",
                                "الجهاز مباع للعميل", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return;
                        }

                        // فحص هل تم إرجاعه للمورد مسبقاً
                        if (PurchaseDAL.IsIMEIAlreadyReturnedToSupplier(cleanImei, out int retId, out DateTime retDate, out string supName))
                        {
                            MessageBox.Show(
                                $"❌ لا يمكن إرجاع الجهاز ذو السيريال [{cleanImei}] ({item.ProductName}) للمورد!\n\n" +
                                $"هذا الجهاز تم إرجاعه للمورد مسبقاً في مرتجع الشراء رقم [PRT-{retId}] بتاريخ [{retDate:yyyy/MM/dd}].",
                                "الجهاز مرتجع مسبقاً", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return;
                        }
                    }
                }
            }

            // ── 2. التحقق من رصيد الصنف المتوفر بالمخزن (لكل الأنشطة والموبايلات لمنع الرصيد السالب) ──
            var groupedItems = returnItems
                .GroupBy(x => x.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    ProductName = g.First().ProductName,
                    TotalReturningBaseQty = g.Sum(x => (x.Quantity + x.BonusQuantity) * (x.Factor > 0 ? x.Factor : 1.0m))
                })
                .ToList();

            foreach (var grp in groupedItems)
            {
                decimal currentStock = InventoryDAL.GetProductStock(grp.ProductID, effWhId);
                if (grp.TotalReturningBaseQty > currentStock)
                {
                    MessageBox.Show(
                        $"❌ لا يمكن إتمام مرتجع الشراء للصنف [{grp.ProductName}]!\n\n" +
                        $"• الكمية المطلوب إرجاعها للمورد: {grp.TotalReturningBaseQty:0.##}\n" +
                        $"• الرصيد الفعلي المتوفر حالياً بالمخزن: {currentStock:0.##}\n\n" +
                        $"الكمية المطلوب إرجاعها تتجاوز الرصيد المتوفر بالمخزن (تم بيع جزء منها للعملاء مسبقاً).\n" +
                        $"الرجاء تعديل الكمية المرتجعة لتطابق أو تقل عن الرصيد المتاح لمنع تحول رصيد المخزن إلى سالب.",
                        "تجاوز الرصيد المتوفر بالمخزن", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
            }
            
            string returnType = "Credit";
            if (cboReturnType.SelectedIndex == 1 || cboReturnType.Text.Contains("Cash") || cboReturnType.Text.Contains("نقدي"))
                returnType = "Cash";
            else if (cboReturnType.SelectedIndex == 2 || cboReturnType.Text.Contains("Visa") || cboReturnType.Text.Contains("فيزا") || cboReturnType.Text.Contains("بنك"))
                returnType = "Visa";
            else
                returnType = "Credit";

            if (isGeneral && returnType == "Credit" && !supplierID.HasValue)
            {
                MessageBox.Show("يرجى اختيار المورد/العميل أولاً لمرتجع الشراء العام الآجل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? shiftID = Session.CurrentShiftID ?? ShiftDAL.GetActiveShiftID();

            try
            {
                int id = PurchaseReturnDAL.SavePurchaseReturn(purchaseID, supplierID, totalReturnAmount,
                    txtNotes.Text, returnItems, warehouseID, returnType, shiftID);
                if (id > 0)
                {
                    StockCache.Invalidate(warehouseID);
                    FrmQuickAdd.RaiseProductSaved();
                    txtNotes.Text = "";
                    dgItems.Rows.Clear();
                    lblTotal.Text = "إجمالي مرتجع الشراء: 0.00 ج";
                    if (lblPurchaseInfo != null) lblPurchaseInfo.Text = "";

                    if (!isGeneral)
                    {
                        LoadPurchasesGrid();
                    }

                    // عرض خيارات الطباعة (حراري 80mm / A4 / A5)
                    FrmPrintChoiceDialog.PromptAndPrintPurchaseReturn(this, id, "✅ تم حفظ مرتجع الشراء وتحديث المخزن وحساب المورد بنجاح!");
                }
                else
                {
                    MessageBox.Show("فشل حفظ المرتجع", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل حفظ مرتجع المشتريات", ex, "FrmPurchaseReturn.BtnSave_Click");
                MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

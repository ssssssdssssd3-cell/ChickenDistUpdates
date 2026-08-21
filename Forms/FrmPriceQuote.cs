using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة بيان وتسعير البضائع (عرض سعر)
    /// لا تؤثر إطلاقاً في كميات المخزون إلا عند التحويل الفعلي لفاتورة بيع
    /// </summary>
    public class FrmPriceQuote : Form
    {
        private ComboBox cboClient;
        private TextBox txtClientManual;
        private ComboBox cboWarehouse;
        private Button btnTierRetail, btnTierSemi, btnTierWholesale;
        private string _selectedTier = "قطاعي";

        private TextBox txtProductCode;
        private Button btnSearchProduct, btnManualAdd;
        private DataGridView dgItems;

        private Label lblTotalVal, lblNetVal, lblCostSummary;
        private TextBox txtDiscount;
        private TextBox txtNotes;

        private Button btnNew, btnSuspend, btnPendingList, btnConvertToSale, btnPrintPrep, btnPrintQuote;

        private List<SaleItemDTO> _items = new List<SaleItemDTO>();
        private List<ComboItem> _productCache = new List<ComboItem>();
        private int _currentQuoteID = 0;
        private string _quoteCode = "";

        public FrmPriceQuote(int quoteID = 0)
        {
            _currentQuoteID = quoteID;
            InitUI();
            LoadCombos();
            if (_currentQuoteID > 0)
            {
                LoadQuoteForEdit(_currentQuoteID);
            }
            else
            {
                NewQuote(false);
            }

            this.Shown += (s, e) =>
            {
                if (_currentQuoteID == 0)
                {
                    OpenQuickSearch();
                }
            };
        }

        private void InitUI()
        {
            Text = "📋 شاشة بيان تسعير وعرض أسعار (بدون تأثير مخزني)";
            Size = new Size(1040, 700);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;
            KeyPreview = true;
            KeyDown += FrmPriceQuote_KeyDown;

            // ── 1. Header Panel ──
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                Padding = new Padding(12, 8, 12, 8)
            };
            Theme.StyleSearchHeaderPanel(pnlHeader);

            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 4,
                BackColor = Color.Transparent
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            tblHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));

            // Row 0: Client & Warehouse
            var lblClient = MakeLabel("العميل :");
            cboClient = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(2)
            };
            SetupSearchableCombo(cboClient);

            txtClientManual = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(2),
                Font = Theme.FontMain,
                Visible = false
            };

            var lblWH = MakeLabel("المخزن :");
            cboWarehouse = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(2)
            };

            tblHeader.Controls.Add(lblClient, 0, 0);
            var pnlClientWrap = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            pnlClientWrap.Controls.Add(cboClient);
            pnlClientWrap.Controls.Add(txtClientManual);
            tblHeader.Controls.Add(pnlClientWrap, 1, 0);
            tblHeader.Controls.Add(lblWH, 2, 0);
            tblHeader.Controls.Add(cboWarehouse, 3, 0);

            // Row 1: Price Tier & Date
            var lblTier = MakeLabel("فئة السعر :");
            var pnlTiers = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            btnTierRetail = MakeTierButton("قطاعي", true);
            btnTierSemi = MakeTierButton("نصف جملة", false);
            btnTierWholesale = MakeTierButton("جملة", false);

            btnTierRetail.Click += (s, e) => SelectTier("قطاعي");
            btnTierSemi.Click += (s, e) => SelectTier("نصف جملة");
            btnTierWholesale.Click += (s, e) => SelectTier("جملة");

            pnlTiers.Controls.AddRange(new Control[] { btnTierRetail, btnTierSemi, btnTierWholesale });

            var lblDate = MakeLabel("التاريخ :");
            var dtpDate = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Enabled = false,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd  hh:mm tt",
                Value = DateTime.Now,
                Margin = new Padding(2)
            };

            tblHeader.Controls.Add(lblTier, 0, 1);
            tblHeader.Controls.Add(pnlTiers, 1, 1);
            tblHeader.Controls.Add(lblDate, 2, 1);
            tblHeader.Controls.Add(dtpDate, 3, 1);

            // Row 2: Info banner
            var lblBanner = new Label
            {
                Text = "ℹ️ تنبيه: بيان التسعير هذا مجرد عرض أسعار للعميل ولا يخصم أي كميات من المخزون إلا عند تحويله لفاتورة بيع.",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(255, 220, 110),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            tblHeader.Controls.Add(lblBanner, 0, 2);
            tblHeader.SetColumnSpan(lblBanner, 4);

            pnlHeader.Controls.Add(tblHeader);

            // ── 2. Product Entry Bar ──
            var pnlProductBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(6, 4, 6, 4)
            };
            Theme.StyleSearchHeaderPanel(pnlProductBar);

            var tblProductBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 4,
                BackColor = Color.Transparent
            };
            tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85f));
            tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));

            var lblProdTitle = MakeLabel("الصنف :");
            txtProductCode = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(2, 6, 2, 6),
                Font = Theme.FontMain
            };
            txtProductCode.KeyDown += TxtProductCode_KeyDown;

            btnSearchProduct = Theme.MakeButton("🔍 بحث سريع (F3)", 0, 0, 0, 0, Theme.Accent);
            btnSearchProduct.Dock = DockStyle.Fill;
            btnSearchProduct.Margin = new Padding(2);
            btnSearchProduct.Click += (s, e) => OpenQuickSearch();

            btnManualAdd = Theme.MakeButton("➕ إضافة", 0, 0, 0, 0, Theme.Success);
            btnManualAdd.Dock = DockStyle.Fill;
            btnManualAdd.Margin = new Padding(2);
            btnManualAdd.Click += (s, e) => ProcessProductInput(txtProductCode.Text.Trim());

            tblProductBar.Controls.Add(lblProdTitle, 0, 0);
            tblProductBar.Controls.Add(txtProductCode, 1, 0);
            tblProductBar.Controls.Add(btnSearchProduct, 2, 0);
            tblProductBar.Controls.Add(btnManualAdd, 3, 0);

            pnlProductBar.Controls.Add(tblProductBar);

            // ── 3. Bottom Control & Actions Bar ──
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = Theme.BgCard,
                Padding = new Padding(8)
            };

            var tblBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 6
            };
            tblBottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            tblBottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTotalVal = new Label { Text = "الإجمالي: 0.00 ج", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Theme.TextMain, AutoSize = true, Anchor = AnchorStyles.Left };
            txtDiscount = new TextBox { Width = 80, Text = "0", BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            txtDiscount.TextChanged += (s, e) => RecalculateTotals();

            lblNetVal = new Label { Text = "الصافي: 0.00 ج", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Theme.Success, AutoSize = true, Anchor = AnchorStyles.Left };
            lblCostSummary = new Label { Text = "", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(41, 128, 185), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(10, 3, 0, 0), Visible = Session.CanViewCost("PriceQuote") };
            
            var pnlNetBox = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            pnlNetBox.Controls.Add(lblNetVal);
            pnlNetBox.Controls.Add(lblCostSummary);

            txtNotes = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            tblBottom.Controls.Add(lblTotalVal, 0, 0);
            tblBottom.Controls.Add(new Label { Text = "خصم (ج):", AutoSize = true, Anchor = AnchorStyles.Right }, 1, 0);
            tblBottom.Controls.Add(txtDiscount, 2, 0);
            tblBottom.Controls.Add(pnlNetBox, 3, 0);
            tblBottom.Controls.Add(new Label { Text = "ملاحظات:", AutoSize = true, Anchor = AnchorStyles.Right }, 4, 0);
            tblBottom.Controls.Add(txtNotes, 5, 0);

            // Action Buttons Row
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 0),
                RightToLeft = RightToLeft.Yes
            };

            btnNew = Theme.MakeButton("📄 جديد (F1)", 0, 0, 110, 40, Color.FromArgb(55, 65, 81));
            btnNew.Click += (s, e) => NewQuote(true);

            btnSuspend = Theme.MakeButton("📌 تعليق / حفظ (F2)", 0, 0, 160, 40, Theme.Primary);
            btnSuspend.Click += (s, e) => SaveQuote(false);

            btnPendingList = Theme.MakeButton("📋 العروض المعلقة (F4)", 0, 0, 160, 40, Theme.Accent);
            btnPendingList.Click += (s, e) => OpenPendingQuotesList();

            btnConvertToSale = Theme.MakeButton("🔄 تحويل لفاتورة بيع (F5)", 0, 0, 190, 40, Theme.Success);
            btnConvertToSale.Click += (s, e) => ConvertToSaleInvoice();

            btnPrintPrep = Theme.MakeButton("📦 طباعة إذن تحضير", 0, 0, 150, 40, Color.DarkSlateBlue);
            btnPrintPrep.Click += (s, e) => PrintPreparationSlip();

            btnPrintQuote = Theme.MakeButton("🖨️ طباعة عرض أسعار", 0, 0, 150, 40, Color.DarkGreen);
            btnPrintQuote.Click += (s, e) => PrintPriceQuote();

            var btnWhatsApp = Theme.MakeButton("📱 واتساب للعميل", 0, 0, 150, 40, Color.FromArgb(37, 211, 102));
            btnWhatsApp.ForeColor = Color.White;
            btnWhatsApp.Click += (s, e) => SendQuoteWhatsApp();

            pnlActions.Controls.AddRange(new Control[] { btnNew, btnSuspend, btnPendingList, btnConvertToSale, btnPrintPrep, btnPrintQuote, btnWhatsApp });
            tblBottom.Controls.Add(pnlActions, 0, 1);
            tblBottom.SetColumnSpan(pnlActions, 6);
            pnlBottom.Controls.Add(tblBottom);

            // ── 4. Items DataGridView ──
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = true,
                AllowUserToAddRows = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36
            };
            Theme.EnableDoubleBuffer(dgItems);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الكود", FillWeight = 30, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 90, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLocation", HeaderText = "موقع الصنف (الرف)", FillWeight = 45, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 30, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 35 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "السعر", FillWeight = 35 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "الإجمالي", FillWeight = 40, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", HeaderText = "سعر التكلفة", FillWeight = 35, ReadOnly = true, Visible = Session.CanViewCost("PriceQuote") });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostTotal", HeaderText = "إجمالي التكلفة", FillWeight = 40, ReadOnly = true, Visible = Session.CanViewCost("PriceQuote") });

            var btnDel = new DataGridViewButtonColumn
            {
                Name = "BtnDelete",
                HeaderText = "حذف",
                Text = "❌",
                UseColumnTextForButtonValue = true,
                FillWeight = 20
            };
            dgItems.Columns.Add(btnDel);

            dgItems.CellValueChanged += DgItems_CellValueChanged;
            dgItems.CellContentClick += DgItems_CellContentClick;
            dgItems.RowPostPaint += (s, e) =>
            {
                var grid = s as DataGridView;
                var rowIdx = (e.RowIndex + 1).ToString();
                var centerFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                var headerBounds = new Rectangle(e.RowBounds.Right - grid.RowHeadersWidth, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
                e.Graphics.DrawString(rowIdx, this.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
            };

            // ADD CONTROLS IN PROPER WINFORMS DOCK ORDER
            Controls.Add(dgItems);
            Controls.Add(pnlBottom);
            Controls.Add(pnlProductBar);
            Controls.Add(pnlHeader);
        }

        private Label MakeLabel(string txt)
        {
            return new Label
            {
                Text = txt,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextSearchLabel,
                Font = Theme.FontBold,
                Margin = new Padding(2)
            };
        }

        private Button MakeTierButton(string txt, bool isSelected)
        {
            var btn = new Button
            {
                Text = txt,
                Width = 80,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = Theme.FontMain,
                BackColor = isSelected ? Theme.Primary : Color.FromArgb(40, 60, 95),
                ForeColor = Color.White,
                Margin = new Padding(2)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = isSelected ? Color.White : Color.FromArgb(100, 140, 200);
            return btn;
        }

        private void SelectTier(string tier)
        {
            _selectedTier = tier;
            btnTierRetail.BackColor = tier == "قطاعي" ? Theme.Primary : Color.FromArgb(40, 60, 95);
            btnTierRetail.FlatAppearance.BorderColor = tier == "قطاعي" ? Color.White : Color.FromArgb(100, 140, 200);

            btnTierSemi.BackColor = tier == "نصف جملة" ? Theme.Primary : Color.FromArgb(40, 60, 95);
            btnTierSemi.FlatAppearance.BorderColor = tier == "نصف جملة" ? Color.White : Color.FromArgb(100, 140, 200);

            btnTierWholesale.BackColor = tier == "جملة" ? Theme.Primary : Color.FromArgb(40, 60, 95);
            btnTierWholesale.FlatAppearance.BorderColor = tier == "جملة" ? Color.White : Color.FromArgb(100, 140, 200);
        }

        private void SetupSearchableCombo(ComboBox cbo)
        {
            cbo.AutoCompleteSource = AutoCompleteSource.ListItems;
            cbo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        }

        private void LoadCombos()
        {
            // Clients
            try
            {
                cboClient.Items.Clear();
                cboClient.Items.Add(new ComboItem(0, "-- اختر عميل / نقدي --"));
                DataTable dtC = ClientDAL.GetAll(true);
                foreach (DataRow r in dtC.Rows)
                {
                    cboClient.Items.Add(new ComboItem(Convert.ToInt32(r["ClientID"]), r["ClientName"].ToString()));
                }
                cboClient.SelectedIndex = 0;
            }
            catch { }

            // Warehouses
            try
            {
                cboWarehouse.Items.Clear();
                DataTable dtW = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive=1 ORDER BY WarehouseID");
                foreach (DataRow r in dtW.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
                }
                if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
            }
            catch { }

            // Cache products for fast lookup
            try
            {
                DataTable dtP = DbHelper.Query("SELECT ProductID, ProductCode, ProductName, SalePrice, WholesalePrice, SemiWholesalePrice, Unit, ShelfLocation, PartNumber, COALESCE(PurchasePrice, 0) AS PurchasePrice FROM Products WHERE IsActive=1");
                _productCache.Clear();
                foreach (DataRow r in dtP.Rows)
                {
                    decimal price = Convert.ToDecimal(r["SalePrice"]);
                    decimal purchasePrice = Convert.ToDecimal(r["PurchasePrice"]);
                    var ci = new ComboItem(Convert.ToInt32(r["ProductID"]), r["ProductName"].ToString(), r["ProductName"].ToString(), price, 0m, purchasePrice);
                    ci.ProductCode = r["ProductCode"]?.ToString() ?? "";
                    ci.ShelfLocation = r["ShelfLocation"]?.ToString() ?? "";
                    ci.PartNumber = r["PartNumber"]?.ToString() ?? "";
                    ci.BaseUnitName = r["Unit"]?.ToString() ?? "";
                    _productCache.Add(ci);
                }
            }
            catch { }
        }

        private void NewQuote(bool openSearch = false)
        {
            _currentQuoteID = 0;
            _quoteCode = "Q-NEW";
            _items.Clear();
            dgItems.Rows.Clear();
            txtDiscount.Text = "0";
            txtNotes.Text = "";
            txtProductCode.Clear();
            if (cboClient.Items.Count > 0) cboClient.SelectedIndex = 0;
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
            SelectTier("قطاعي");
            RecalculateTotals();
            txtProductCode.Focus();

            if (openSearch && this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)delegate {
                    OpenQuickSearch();
                });
            }
        }

        private void TxtProductCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                string code = txtProductCode.Text.Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    ProcessProductInput(code);
                    txtProductCode.Clear();
                }
            }
        }

        private void OpenQuickSearch()
        {
            int? whId = null;
            if (cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
                whId = ci.ID;

            while (true)
            {
                using (var frm = new FrmProductSearch(whId, isPurchaseMode: false, defaultShowZeroStock: true))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK && frm.SelectedProductID > 0)
                    {
                        decimal qty = frm.SelectedQuantity > 0 ? frm.SelectedQuantity : 1.00m;
                        decimal price = frm.SelectedSalePrice > 0 ? frm.SelectedSalePrice : frm.SelectedPrice;
                        decimal discount = frm.SelectedDiscount;
                        AddProductByID(frm.SelectedProductID, qty, price, discount);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void ProcessProductInput(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            // Search in product cache by Code or PartNumber or Name
            ComboItem match = null;
            foreach (var ci in _productCache)
            {
                if (string.Equals(ci.ProductCode, code, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ci.PartNumber, code, StringComparison.OrdinalIgnoreCase) ||
                    ci.Text.Equals(code, StringComparison.OrdinalIgnoreCase))
                {
                    match = ci;
                    break;
                }
            }

            if (match != null)
            {
                AddProductByID(match.ID, 1.0m, match.Price);
            }
            else
            {
                // DB Fallback search
                DataTable dt = DbHelper.Query("SELECT ProductID, ProductName, SalePrice FROM Products WHERE ProductCode=@c OR PartNumber=@c OR InternationalCode=@c", DbHelper.P("@c", code));
                if (dt.Rows.Count > 0)
                {
                    int pid = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                    decimal price = Convert.ToDecimal(dt.Rows[0]["SalePrice"]);
                    AddProductByID(pid, 1.0m, price);
                }
                else
                {
                    MessageBox.Show("الصنف غير موجود!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            txtProductCode.Clear();
            txtProductCode.Focus();
        }

        private void AddProductByID(int productID, decimal qty, decimal? overridePrice = null, decimal discount = 0m)
        {
            DataRow pRow = ProductDAL.GetByID(productID);
            if (pRow == null) return;

            string name = pRow["ProductName"].ToString();
            string code = pRow["ProductCode"]?.ToString() ?? "";
            string shelfLoc = pRow["ShelfLocation"]?.ToString() ?? "";
            string unit = pRow["Unit"]?.ToString() ?? "";

            decimal price = overridePrice ?? Convert.ToDecimal(pRow["SalePrice"]);
            decimal purchasePrice = pRow.Table.Columns.Contains("PurchasePrice") && pRow["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["PurchasePrice"]) : 0m;
            if (_selectedTier == "نصف جملة" && pRow.Table.Columns.Contains("SemiWholesalePrice") && pRow["SemiWholesalePrice"] != DBNull.Value)
                price = Convert.ToDecimal(pRow["SemiWholesalePrice"]);
            else if (_selectedTier == "جملة" && pRow.Table.Columns.Contains("WholesalePrice") && pRow["WholesalePrice"] != DBNull.Value)
                price = Convert.ToDecimal(pRow["WholesalePrice"]);

            decimal discPct = 0m;
            decimal discAmt = 0m;
            if (discount > 0)
            {
                if (discount <= 100m)
                {
                    discPct = discount;
                    discAmt = Math.Round((qty * price) * discount / 100m, 2);
                }
                else
                {
                    discAmt = discount;
                    discPct = (qty * price) > 0 ? Math.Round(discount / (qty * price) * 100m, 2) : 0m;
                }
            }

            // Check if item already exists in quote list
            SaleItemDTO existing = _items.Find(x => x.ProductID == productID && Math.Abs(x.UnitPrice - price) < 0.005m);
            if (existing != null)
            {
                existing.Quantity += qty;
                if (discPct > 0)
                {
                    existing.DiscountPct = discPct;
                    existing.DiscountAmt = Math.Round((existing.Quantity * existing.UnitPrice) * discPct / 100m, 2);
                }
                else if (discAmt > 0)
                {
                    existing.DiscountAmt = discAmt;
                    existing.DiscountPct = (existing.Quantity * existing.UnitPrice) > 0 ? Math.Round(discAmt / (existing.Quantity * existing.UnitPrice) * 100m, 2) : 0m;
                }
            }
            else
            {
                var dto = new SaleItemDTO
                {
                    ProductID = productID,
                    ProductName = name,
                    ProductCode = code,
                    ShelfLocation = shelfLoc,
                    UnitName = unit,
                    Quantity = qty,
                    UnitPrice = price,
                    PurchasePrice = purchasePrice,
                    Factor = 1.0m,
                    DiscountPct = discPct,
                    DiscountAmt = discAmt
                };
                _items.Add(dto);
            }
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgItems.Rows.Clear();
            foreach (var item in _items)
            {
                decimal total = item.Quantity * item.UnitPrice - item.DiscountAmt;
                decimal costTotal = item.Quantity * item.PurchasePrice;
                dgItems.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.ShelfLocation,
                    item.UnitName,
                    item.Quantity.ToString("F2"),
                    item.UnitPrice.ToString("N2"),
                    total.ToString("N2"),
                    item.PurchasePrice.ToString("N2"),
                    costTotal.ToString("N2")
                );
            }
            RecalculateTotals();
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;

            var row = dgItems.Rows[e.RowIndex];
            var item = _items[e.RowIndex];

            if (dgItems.Columns[e.ColumnIndex].Name == "Quantity")
            {
                if (decimal.TryParse(row.Cells["Quantity"].Value?.ToString(), out decimal q) && q > 0)
                {
                    item.Quantity = q;
                }
            }
            else if (dgItems.Columns[e.ColumnIndex].Name == "UnitPrice")
            {
                if (decimal.TryParse(row.Cells["UnitPrice"].Value?.ToString(), out decimal p) && p >= 0)
                {
                    item.UnitPrice = p;
                }
            }
            RefreshGrid();
        }

        private void DgItems_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "BtnDelete")
            {
                _items.RemoveAt(e.RowIndex);
                RefreshGrid();
            }
        }

        private void RecalculateTotals()
        {
            decimal gross = 0m;
            decimal totalCost = 0m;
            foreach (var item in _items)
            {
                gross += (item.Quantity * item.UnitPrice) - item.DiscountAmt;
                totalCost += (item.Quantity * item.PurchasePrice);
            }
            lblTotalVal.Text = $"الإجمالي: {gross:N2} ج";

            decimal disc = 0m;
            decimal.TryParse(txtDiscount.Text, out disc);
            decimal net = Math.Max(0m, gross - disc);
            lblNetVal.Text = $"الصافي: {net:N2} ج";

            if (lblCostSummary != null && Session.CanViewCost("PriceQuote"))
            {
                decimal profit = net - totalCost;
                lblCostSummary.Text = $"[ التكلفة: {totalCost:N2} ج | الربح التقديري: {profit:N2} ج ]";
            }
        }

        private int? GetSelectedClientID()
        {
            if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0) return ci.ID;
            return null;
        }

        private string GetClientName()
        {
            if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0) return ci.Text;
            if (!string.IsNullOrWhiteSpace(cboClient.Text) && !cboClient.Text.StartsWith("--")) return cboClient.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtClientManual.Text)) return txtClientManual.Text.Trim();
            return "عميل نقدي";
        }

        private int? GetSelectedWarehouseID()
        {
            if (cboWarehouse.SelectedItem is ComboItem w && w.ID > 0) return w.ID;
            return null;
        }

        private bool SaveQuote(bool isSilent = false)
        {
            if (_items.Count == 0)
            {
                if (!isSilent) MessageBox.Show("أضف أصنافاً أولاً لبيان التسعير!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            decimal gross = 0m;
            foreach (var item in _items) gross += (item.Quantity * item.UnitPrice) - item.DiscountAmt;
            decimal disc = 0m;
            decimal.TryParse(txtDiscount.Text, out disc);
            decimal net = Math.Max(0m, gross - disc);

            int savedID = PriceQuoteDAL.SaveQuote(
                GetSelectedClientID(),
                GetClientName(),
                net,
                disc,
                0m,
                txtNotes.Text,
                _items,
                GetSelectedWarehouseID(),
                _selectedTier,
                _currentQuoteID
            );

            if (savedID > 0)
            {
                _currentQuoteID = savedID;
                if (!isSilent)
                {
                    MessageBox.Show("✅ تم تعليق وحفظ بيان التسعير بنجاح (بدون أي تأثير مخزني).", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    NewQuote(true);
                }
                return true;
            }
            return false;
        }

        private void LoadQuoteForEdit(int quoteID)
        {
            DataRow qRow = PriceQuoteDAL.GetQuoteHeader(quoteID);
            if (qRow == null) return;

            _currentQuoteID = quoteID;
            _quoteCode = qRow["QuoteCode"].ToString();

            // Client
            if (qRow["ClientID"] != DBNull.Value)
            {
                int cid = Convert.ToInt32(qRow["ClientID"]);
                for (int i = 0; i < cboClient.Items.Count; i++)
                    if (cboClient.Items[i] is ComboItem ci && ci.ID == cid)
                    { cboClient.SelectedIndex = i; break; }
            }
            else
            {
                string clientNameStr = qRow["ClientName"]?.ToString() ?? "";
                cboClient.Text = clientNameStr;
                txtClientManual.Text = clientNameStr;
            }

            // Warehouse
            if (qRow["WarehouseID"] != DBNull.Value)
            {
                int wid = Convert.ToInt32(qRow["WarehouseID"]);
                for (int i = 0; i < cboWarehouse.Items.Count; i++)
                    if (cboWarehouse.Items[i] is ComboItem w && w.ID == wid)
                    { cboWarehouse.SelectedIndex = i; break; }
            }

            SelectTier(qRow["PriceTier"]?.ToString() ?? "قطاعي");
            txtDiscount.Text = Convert.ToDecimal(qRow["DiscountAmount"]).ToString("G");
            txtNotes.Text = qRow["Notes"]?.ToString() ?? "";

            // Load Items
            DataTable dtItems = PriceQuoteDAL.GetQuoteItems(quoteID);
            _items.Clear();
            foreach (DataRow r in dtItems.Rows)
            {
                decimal purchasePrice = r.Table.Columns.Contains("PurchasePrice") && r["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                _items.Add(new SaleItemDTO
                {
                    ProductID = Convert.ToInt32(r["ProductID"]),
                    ProductName = r["ProductName"].ToString(),
                    ProductCode = r["ProductCode"]?.ToString() ?? "",
                    ShelfLocation = r["ProductShelfLocation"]?.ToString() ?? "",
                    UnitName = r["UnitName"]?.ToString() ?? "",
                    Quantity = Convert.ToDecimal(r["Quantity"]),
                    UnitPrice = Convert.ToDecimal(r["UnitPrice"]),
                    PurchasePrice = purchasePrice,
                    Factor = Convert.ToDecimal(r["Factor"])
                });
            }
            RefreshGrid();
        }

        private void OpenPendingQuotesList()
        {
            using (var dlg = new FrmPriceQuotesList())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedQuoteID > 0)
                {
                    if (dlg.ActionType == "Edit")
                    {
                        LoadQuoteForEdit(dlg.SelectedQuoteID);
                    }
                    else if (dlg.ActionType == "Convert")
                    {
                        LoadQuoteForEdit(dlg.SelectedQuoteID);
                        ConvertToSaleInvoice();
                    }
                }
            }
        }

        private void ConvertToSaleInvoice()
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف لتحويلها إلى فاتورة بيع!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save quote first
            SaveQuote(true);

            // Confirm conversion
            var confirm = MessageBox.Show(
                "هل تريد تحويل بيان التسعير الحالي إلى فاتورة بيع الآن؟\nسوف يتم إدخال الأصناف في شاشة البيع لخصمها من المخزون وتسجيلها كفاتورة بيع رسمية.",
                "تأكيد تحويل البيان إلى فاتورة بيع",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes) return;

            // Open Sale form with quote items
            int? cid = GetSelectedClientID();
            int? wid = GetSelectedWarehouseID();
            string notes = txtNotes.Text;

            var frmSale = new FrmSale();
            frmSale.LoadFromPriceQuote(_currentQuoteID, cid, wid, _selectedTier, _items, notes);
            frmSale.Show();
            this.Close();
        }

        // ── 🖨️ Printing Logic ──

        private void PrintPreparationSlip()
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف في بيان التسعير لطباعة إذن التحضير!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var res = MessageBox.Show("هل تريد طباعة إذن التحضير على طابعة ريسيت (80mm)؟\nاضغط (Yes) للـ Receipt أو (No) للـ A4/A5.", "اختيار نوع الطباعة", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res == DialogResult.Cancel) return;

            bool isReceipt = (res == DialogResult.Yes);

            var pd = new PrintDocument();
            if (isReceipt)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 300, 1000);
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
            }
            else
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                pd.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            }

            string whName  = cboWarehouse.Text;
            string empName = Session.EmpName;
            string companyName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
            string companyPhone = !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "";
            string companyAddress = !string.IsNullOrWhiteSpace(AppConfig.CompanyAddress) ? AppConfig.CompanyAddress : "";
            
            // Check logo image
            Image logoImg = null;
            try
            {
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.jpg");
                if (System.IO.File.Exists(logoPath)) logoImg = Image.FromFile(logoPath);
            }
            catch { }

            int itemIdx = 0;
            int rowNum  = 0;

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                float titleSize  = isReceipt ? 12f : 16f;
                float headerSize = isReceipt ? 9f  : 11f;
                float bodySize   = isReceipt ? 8.5f: 10f;

                var fontCompany = new Font("Arial", isReceipt ? 11f : 14f, FontStyle.Bold);
                var fontTitle  = new Font("Arial", titleSize,  FontStyle.Bold);
                var fontHeader = new Font("Arial", headerSize, FontStyle.Bold);
                var fontBody   = new Font("Arial", bodySize,   FontStyle.Regular);
                var fontBold   = new Font("Arial", bodySize,   FontStyle.Bold);

                var brushDarkBlue = new SolidBrush(Color.FromArgb(20, 60, 120));
                var brushHeaderBg = new SolidBrush(Color.FromArgb(28, 45, 78));
                var brushRowAlt   = new SolidBrush(Color.FromArgb(245, 248, 253));
                var penGrid       = new Pen(Color.FromArgb(170, 185, 205), 1f);
                var penDark       = new Pen(Color.FromArgb(28, 45, 78), 1.5f);

                int y    = e.MarginBounds.Top;
                int left = e.MarginBounds.Left;
                int rght = e.MarginBounds.Right;
                int w    = e.MarginBounds.Width;

                // ── Header: Logo, Company Name & Title ─────────
                if (logoImg != null && !isReceipt)
                {
                    g.DrawImage(logoImg, rght - 70, y, 65, 50);
                }

                SizeF szComp = g.MeasureString(companyName, fontCompany);
                g.DrawString(companyName, fontCompany, brushDarkBlue, left + (w - szComp.Width) / 2, y);
                y += (int)szComp.Height + 2;

                if (!string.IsNullOrWhiteSpace(companyPhone))
                {
                    string phStr = $"تليفون: {companyPhone}" + (!string.IsNullOrWhiteSpace(companyAddress) ? $" | {companyAddress}" : "");
                    SizeF szPh = g.MeasureString(phStr, fontBody);
                    g.DrawString(phStr, fontBody, Brushes.DarkGray, left + (w - szPh.Width) / 2, y);
                    y += (int)szPh.Height + 4;
                }

                string tit = "📋 إذن تحضير وتجميع بضاعة (من المخزن)";
                SizeF szT  = g.MeasureString(tit, fontTitle);
                g.DrawString(tit, fontTitle, Brushes.Black, left + (w - szT.Width) / 2, y);
                y += (int)szT.Height + (isReceipt ? 4 : 6);

                g.DrawLine(penDark, left, y, rght, y);
                y += (isReceipt ? 4 : 8);

                // ── Header Info ─────────────────────────────
                string dateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                if (!isReceipt)
                {
                    g.DrawString($"المخزن المصدر: {whName}", fontHeader, Brushes.Black, rght - g.MeasureString($"المخزن المصدر: {whName}", fontHeader).Width, y);
                    g.DrawString($"التاريخ والوقت: {dateStr}", fontBody, Brushes.Black, left, y);
                    y += 20;

                    g.DrawString($"الموظف المسؤول: {empName}", fontHeader, Brushes.Black, rght - g.MeasureString($"الموظف المسؤول: {empName}", fontHeader).Width, y);
                    g.DrawString($"عدد الأصناف: {_items.Count}", fontBody, Brushes.Black, left, y);
                    y += 22;
                }
                else
                {
                    g.DrawString($"المخزن: {whName}",   fontHeader, Brushes.Black, left, y); y += 18;
                    g.DrawString($"الموظف: {empName}",  fontBody,   Brushes.Black, left, y); y += 18;
                    g.DrawString($"التاريخ: {dateStr}", fontBody,   Brushes.Black, left, y); y += 18;
                }

                g.DrawLine(penGrid, left, y, rght, y);
                y += (isReceipt ? 4 : 8);

                // ── Table Grid Setup ────────────────────────
                int colNumW  = isReceipt ? 18 : (int)(w * 0.05);
                int colCodeW = isReceipt ? 35 : (int)(w * 0.13);
                int colLocW  = isReceipt ? 45 : (int)(w * 0.22);
                int colUnitW = isReceipt ? 30 : (int)(w * 0.12);
                int colQtyW  = isReceipt ? 30 : (int)(w * 0.14);
                int colProdW = w - colNumW - colCodeW - colLocW - colUnitW - colQtyW;
                int rowH     = isReceipt ? 20 : 25;

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };
                var sfRight  = new StringFormat { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };

                // Header Row with Full Borders
                if (!isReceipt)
                {
                    g.FillRectangle(brushHeaderBg, left, y, w, rowH);
                    g.DrawRectangle(penDark, left, y, w, rowH);

                    int curX = rght;
                    
                    // #
                    curX -= colNumW;
                    g.DrawRectangle(penGrid, curX, y, colNumW, rowH);
                    g.DrawString("#", fontHeader, Brushes.White, new RectangleF(curX, y, colNumW, rowH), sfCenter);

                    // Code
                    curX -= colCodeW;
                    g.DrawRectangle(penGrid, curX, y, colCodeW, rowH);
                    g.DrawString("الكود", fontHeader, Brushes.White, new RectangleF(curX, y, colCodeW, rowH), sfCenter);

                    // Product
                    curX -= colProdW;
                    g.DrawRectangle(penGrid, curX, y, colProdW, rowH);
                    g.DrawString("اسم الصنف", fontHeader, Brushes.White, new RectangleF(curX, y, colProdW, rowH), sfCenter);

                    // Qty
                    curX -= colQtyW;
                    g.DrawRectangle(penGrid, curX, y, colQtyW, rowH);
                    g.DrawString("الكمية المطلوبة", fontHeader, Brushes.White, new RectangleF(curX, y, colQtyW, rowH), sfCenter);

                    // Unit
                    curX -= colUnitW;
                    g.DrawRectangle(penGrid, curX, y, colUnitW, rowH);
                    g.DrawString("الوحدة", fontHeader, Brushes.White, new RectangleF(curX, y, colUnitW, rowH), sfCenter);

                    // Shelf Location
                    curX -= colLocW;
                    g.DrawRectangle(penGrid, curX, y, colLocW, rowH);
                    g.DrawString("موقع الرف / التخزين", fontHeader, Brushes.White, new RectangleF(curX, y, colLocW, rowH), sfCenter);

                    y += rowH;
                }
                else
                {
                    g.DrawString("الصنف",  fontHeader, Brushes.Black, rght - colNumW - colProdW, y);
                    g.DrawString("الكمية",  fontHeader, Brushes.Black, rght - colNumW - colProdW - colQtyW, y);
                    g.DrawString("الوحدة",  fontHeader, Brushes.Black, rght - colNumW - colProdW - colQtyW - colUnitW, y);
                    g.DrawString("الرف",   fontHeader, Brushes.Black, rght - colNumW - colProdW - colQtyW - colUnitW - colLocW, y);
                    y += rowH;
                    g.DrawLine(penGrid, left, y, rght, y);
                    y += 4;
                }

                // ── Data Rows with Grid Borders ───────────────
                while (itemIdx < _items.Count)
                {
                    var item  = _items[itemIdx];
                    string loc  = !string.IsNullOrWhiteSpace(item.ShelfLocation) ? item.ShelfLocation : "---";
                    string unit = !string.IsNullOrWhiteSpace(item.UnitName) ? item.UnitName : "";
                    string code = !string.IsNullOrWhiteSpace(item.ProductCode) ? item.ProductCode : "---";
                    string qty  = item.Quantity % 1 == 0 ? item.Quantity.ToString("N0") : item.Quantity.ToString("N2");
                    rowNum++;

                    if (!isReceipt)
                    {
                        if (rowNum % 2 == 0)
                            g.FillRectangle(brushRowAlt, left, y, w, rowH);

                        g.DrawRectangle(penGrid, left, y, w, rowH);

                        int curX = rght;
                        
                        // #
                        curX -= colNumW;
                        g.DrawRectangle(penGrid, curX, y, colNumW, rowH);
                        g.DrawString(rowNum.ToString(), fontBody, Brushes.Black, new RectangleF(curX, y, colNumW, rowH), sfCenter);

                        // Code
                        curX -= colCodeW;
                        g.DrawRectangle(penGrid, curX, y, colCodeW, rowH);
                        g.DrawString(code, fontBody, Brushes.Gray, new RectangleF(curX, y, colCodeW, rowH), sfCenter);

                        // Product
                        curX -= colProdW;
                        g.DrawRectangle(penGrid, curX, y, colProdW, rowH);
                        g.DrawString(item.ProductName, fontBody, Brushes.Black, new RectangleF(curX + 4, y, colProdW - 8, rowH), sfRight);

                        // Qty
                        curX -= colQtyW;
                        g.DrawRectangle(penGrid, curX, y, colQtyW, rowH);
                        g.DrawString(qty, fontBold, Brushes.Black, new RectangleF(curX, y, colQtyW, rowH), sfCenter);

                        // Unit
                        curX -= colUnitW;
                        g.DrawRectangle(penGrid, curX, y, colUnitW, rowH);
                        g.DrawString(unit, fontBody, Brushes.DarkBlue, new RectangleF(curX, y, colUnitW, rowH), sfCenter);

                        // Shelf Location
                        curX -= colLocW;
                        g.DrawRectangle(penGrid, curX, y, colLocW, rowH);
                        g.DrawString(loc, fontBold, brushDarkBlue, new RectangleF(curX, y, colLocW, rowH), sfCenter);
                    }
                    else
                    {
                        int tx = y;
                        g.DrawString(item.ProductName, fontBody,   Brushes.Black,  rght - colNumW - colProdW,                              tx);
                        g.DrawString(qty,              fontBold,   Brushes.Black,  rght - colNumW - colProdW - colQtyW,                    tx);
                        g.DrawString(unit,             fontBody,   brushDarkBlue,  rght - colNumW - colProdW - colQtyW - colUnitW,         tx);
                        g.DrawString(loc,              fontBold,   brushDarkBlue,  rght - colNumW - colProdW - colQtyW - colUnitW - colLocW,tx);
                        g.DrawString(rowNum.ToString(),fontBody,   Brushes.Black,  left,                                                   tx);
                        g.DrawLine(penGrid, left, y + rowH, rght, y + rowH);
                    }

                    y += rowH;
                    itemIdx++;

                    if (y > e.MarginBounds.Bottom - (isReceipt ? 30 : 60))
                    {
                        e.HasMorePages = true;
                        return;
                    }
                }

                // ── Footer ───────────────────────────────────
                y += (isReceipt ? 6 : 14);
                g.DrawLine(penDark, left, y, rght, y);
                y += (isReceipt ? 6 : 12);
                string sig = "توقيع مسؤول التحضير بالمخزن: ..................................";
                g.DrawString(sig, fontHeader, Brushes.Black, rght - g.MeasureString(sig, fontHeader).Width, y);

                brushDarkBlue.Dispose(); brushHeaderBg.Dispose();
                brushRowAlt.Dispose(); penGrid.Dispose(); penDark.Dispose();
                logoImg?.Dispose();
            };

            try { AppConfig.PrintInBackground(pd); }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintPriceQuote()
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف في بيان التسعير للطباعة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var res = MessageBox.Show("هل تريد طباعة عرض الأسعار كفاتورة بيع تقديرية على طابعة ريسيت (80mm)؟\nاضغط (Yes) للـ Receipt أو (No) للـ A4/A5.", "اختيار نوع الطباعة", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res == DialogResult.Cancel) return;

            bool isReceipt = (res == DialogResult.Yes);

            var pd = new PrintDocument();
            if (isReceipt)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 300, 1000);
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
            }
            else
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                pd.DefaultPageSettings.Margins = new Margins(25, 25, 25, 25);
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            }

            string clientName = GetClientName();
            string empName = Session.EmpName;
            string whName = cboWarehouse.Text;
            string companyName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
            string companyPhone = !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "";
            string companyAddress = !string.IsNullOrWhiteSpace(AppConfig.CompanyAddress) ? AppConfig.CompanyAddress : "";

            // Check logo image
            Image logoImg = null;
            try
            {
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.jpg");
                if (System.IO.File.Exists(logoPath)) logoImg = Image.FromFile(logoPath);
            }
            catch { }

            int itemIdx = 0;
            int rowNum = 0;

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                float titleSize  = isReceipt ? 12f : 16f;
                float headerSize = isReceipt ? 9f  : 11f;
                float bodySize   = isReceipt ? 8.5f: 10f;

                var fontCompany = new Font("Arial", isReceipt ? 11f : 14f, FontStyle.Bold);
                var fontTitle   = new Font("Arial", titleSize,  FontStyle.Bold);
                var fontHeader  = new Font("Arial", headerSize, FontStyle.Bold);
                var fontBody    = new Font("Arial", bodySize,   FontStyle.Regular);
                var fontBold    = new Font("Arial", bodySize,   FontStyle.Bold);

                var brushDarkBlue = new SolidBrush(Color.FromArgb(20, 60, 120));
                var brushHeaderBg = new SolidBrush(Color.FromArgb(28, 45, 78));
                var brushRowAlt   = new SolidBrush(Color.FromArgb(245, 248, 253));
                var brushTotBg    = new SolidBrush(Color.FromArgb(220, 245, 225));
                var penGrid       = new Pen(Color.FromArgb(170, 185, 205), 1f);
                var penDark       = new Pen(Color.FromArgb(28, 45, 78), 1.5f);

                int y     = e.MarginBounds.Top;
                int left  = e.MarginBounds.Left;
                int right = e.MarginBounds.Right;
                int width = e.MarginBounds.Width;

                // ── Header: Logo, Company Name & Title ─────────
                if (logoImg != null && !isReceipt)
                {
                    g.DrawImage(logoImg, right - 70, y, 65, 50);
                }

                SizeF szComp = g.MeasureString(companyName, fontCompany);
                g.DrawString(companyName, fontCompany, brushDarkBlue, (pageW(e) - szComp.Width) / 2, y);
                y += (int)szComp.Height + 2;

                if (!string.IsNullOrWhiteSpace(companyPhone))
                {
                    string phStr = $"تليفون: {companyPhone}" + (!string.IsNullOrWhiteSpace(companyAddress) ? $" | {companyAddress}" : "");
                    SizeF szPh = g.MeasureString(phStr, fontBody);
                    g.DrawString(phStr, fontBody, Brushes.DarkGray, (pageW(e) - szPh.Width) / 2, y);
                    y += (int)szPh.Height + 4;
                }

                string title = "🧾 فاتورة بيع تقديرية (عرض أسعار)";
                SizeF szTitle = g.MeasureString(title, fontTitle);
                g.DrawString(title, fontTitle, Brushes.Black, (pageW(e) - szTitle.Width) / 2, y);
                y += (int)szTitle.Height + 6;

                g.DrawLine(penDark, left, y, right, y);
                y += (isReceipt ? 4 : 8);

                // ── Invoice Details Section ──────────────────
                string dateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                string codeStr = !string.IsNullOrWhiteSpace(_quoteCode) ? _quoteCode : "---";

                if (!isReceipt)
                {
                    g.DrawString($"رقم البيان: {codeStr}", fontHeader, Brushes.Black, right - g.MeasureString($"رقم البيان: {codeStr}", fontHeader).Width, y);
                    g.DrawString($"التاريخ والوقت: {dateStr}", fontBody, Brushes.Black, left, y);
                    y += 20;

                    g.DrawString($"العميل: {clientName}", fontHeader, Brushes.Black, right - g.MeasureString($"العميل: {clientName}", fontHeader).Width, y);
                    g.DrawString($"المخزن: {whName}", fontBody, Brushes.Black, left, y);
                    y += 20;

                    g.DrawString($"الموظف: {empName}", fontBody, Brushes.Black, right - g.MeasureString($"الموظف: {empName}", fontBody).Width, y);
                    g.DrawString($"فئة السعر: {_selectedTier}", fontBody, Brushes.Black, left, y);
                    y += 22;
                }
                else
                {
                    g.DrawString($"رقم البيان: {codeStr}", fontHeader, Brushes.Black, right - g.MeasureString($"رقم البيان: {codeStr}", fontHeader).Width, y);
                    g.DrawString($"التاريخ: {dateStr}", fontBody, Brushes.Black, left, y);
                    y += 18;
                    g.DrawString($"العميل: {clientName}", fontHeader, Brushes.Black, right - g.MeasureString($"العميل: {clientName}", fontHeader).Width, y);
                    y += 18;
                    g.DrawString($"المخزن: {whName} | الموظف: {empName}", fontBody, Brushes.Black, left, y);
                    y += 20;
                }

                g.DrawLine(penGrid, left, y, right, y);
                y += (isReceipt ? 4 : 6);

                // ── Table Column Headers & Grid Borders ──────
                int colNumW   = isReceipt ? 16 : (int)(width * 0.05);
                int colCodeW  = isReceipt ? 35 : (int)(width * 0.14);
                int colUnitW  = isReceipt ? 25 : (int)(width * 0.10);
                int colQtyW   = isReceipt ? 25 : (int)(width * 0.10);
                int colPriceW = isReceipt ? 35 : (int)(width * 0.13);
                int colTotW   = isReceipt ? 40 : (int)(width * 0.14);
                int colProdW  = width - colNumW - colCodeW - colUnitW - colQtyW - colPriceW - colTotW;
                int rowH      = isReceipt ? 20 : 25;

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };
                var sfRight  = new StringFormat { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };

                if (!isReceipt)
                {
                    g.FillRectangle(brushHeaderBg, left, y, width, rowH);
                    g.DrawRectangle(penDark, left, y, width, rowH);

                    int curX = right;
                    
                    // #
                    curX -= colNumW;
                    g.DrawRectangle(penGrid, curX, y, colNumW, rowH);
                    g.DrawString("#", fontHeader, Brushes.White, new RectangleF(curX, y, colNumW, rowH), sfCenter);

                    // Code
                    curX -= colCodeW;
                    g.DrawRectangle(penGrid, curX, y, colCodeW, rowH);
                    g.DrawString("الكود", fontHeader, Brushes.White, new RectangleF(curX, y, colCodeW, rowH), sfCenter);

                    // Product
                    curX -= colProdW;
                    g.DrawRectangle(penGrid, curX, y, colProdW, rowH);
                    g.DrawString("اسم الصنف", fontHeader, Brushes.White, new RectangleF(curX, y, colProdW, rowH), sfCenter);

                    // Unit
                    curX -= colUnitW;
                    g.DrawRectangle(penGrid, curX, y, colUnitW, rowH);
                    g.DrawString("الوحدة", fontHeader, Brushes.White, new RectangleF(curX, y, colUnitW, rowH), sfCenter);

                    // Qty
                    curX -= colQtyW;
                    g.DrawRectangle(penGrid, curX, y, colQtyW, rowH);
                    g.DrawString("الكمية", fontHeader, Brushes.White, new RectangleF(curX, y, colQtyW, rowH), sfCenter);

                    // Price
                    curX -= colPriceW;
                    g.DrawRectangle(penGrid, curX, y, colPriceW, rowH);
                    g.DrawString("السعر", fontHeader, Brushes.White, new RectangleF(curX, y, colPriceW, rowH), sfCenter);

                    // Total
                    curX -= colTotW;
                    g.DrawRectangle(penGrid, curX, y, colTotW, rowH);
                    g.DrawString("الإجمالي", fontHeader, Brushes.White, new RectangleF(curX, y, colTotW, rowH), sfCenter);

                    y += rowH;
                }
                else
                {
                    g.DrawString("الصنف",  fontHeader, Brushes.Black, right - colNumW - colProdW, y);
                    g.DrawString("الكمية",  fontHeader, Brushes.Black, right - colNumW - colProdW - colQtyW, y);
                    g.DrawString("السعر",   fontHeader, Brushes.Black, right - colNumW - colProdW - colQtyW - colPriceW, y);
                    g.DrawString("الإجمالي", fontHeader, Brushes.Black, right - colNumW - colProdW - colQtyW - colPriceW - colTotW, y);
                    y += rowH;
                    g.DrawLine(penGrid, left, y, right, y);
                    y += 4;
                }

                // ── Invoice Items Rows with Grid Borders ─────
                decimal gross = 0m;
                while (itemIdx < _items.Count)
                {
                    var item = _items[itemIdx];
                    decimal tot = item.Quantity * item.UnitPrice - item.DiscountAmt;
                    gross += tot;
                    rowNum++;

                    string loc  = !string.IsNullOrWhiteSpace(item.ShelfLocation) ? item.ShelfLocation : "---";
                    string code = !string.IsNullOrWhiteSpace(item.ProductCode) ? item.ProductCode : "---";
                    string unit = !string.IsNullOrWhiteSpace(item.UnitName) ? item.UnitName : "";

                    if (!isReceipt)
                    {
                        if (rowNum % 2 == 0)
                            g.FillRectangle(brushRowAlt, left, y, width, rowH);

                        g.DrawRectangle(penGrid, left, y, width, rowH);

                        int curX = right;

                        // #
                        curX -= colNumW;
                        g.DrawRectangle(penGrid, curX, y, colNumW, rowH);
                        g.DrawString(rowNum.ToString(), fontBody, Brushes.Black, new RectangleF(curX, y, colNumW, rowH), sfCenter);

                        // Code
                        curX -= colCodeW;
                        g.DrawRectangle(penGrid, curX, y, colCodeW, rowH);
                        g.DrawString(code, fontBody, Brushes.Gray, new RectangleF(curX, y, colCodeW, rowH), sfCenter);

                        // Product
                        curX -= colProdW;
                        g.DrawRectangle(penGrid, curX, y, colProdW, rowH);
                        g.DrawString(item.ProductName, fontBody, Brushes.Black, new RectangleF(curX + 4, y, colProdW - 8, rowH), sfRight);

                        // Unit
                        curX -= colUnitW;
                        g.DrawRectangle(penGrid, curX, y, colUnitW, rowH);
                        g.DrawString(unit, fontBody, Brushes.Black, new RectangleF(curX, y, colUnitW, rowH), sfCenter);

                        // Qty
                        curX -= colQtyW;
                        g.DrawRectangle(penGrid, curX, y, colQtyW, rowH);
                        g.DrawString(item.Quantity.ToString("N0"), fontBold, Brushes.Black, new RectangleF(curX, y, colQtyW, rowH), sfCenter);

                        // Price
                        curX -= colPriceW;
                        g.DrawRectangle(penGrid, curX, y, colPriceW, rowH);
                        g.DrawString(item.UnitPrice.ToString("N2"), fontBody, Brushes.Black, new RectangleF(curX, y, colPriceW, rowH), sfCenter);

                        // Total
                        curX -= colTotW;
                        g.DrawRectangle(penGrid, curX, y, colTotW, rowH);
                        g.DrawString(tot.ToString("N2"), fontBold, Brushes.Black, new RectangleF(curX, y, colTotW, rowH), sfCenter);
                    }
                    else
                    {
                        int tx = y;
                        g.DrawString(item.ProductName,             fontBody, Brushes.Black,  right - colNumW - colProdW, tx);
                        g.DrawString(item.Quantity.ToString("N0"), fontBold, Brushes.Black,  right - colNumW - colProdW - colQtyW, tx);
                        g.DrawString(item.UnitPrice.ToString("N2"),fontBody, Brushes.Black,  right - colNumW - colProdW - colQtyW - colPriceW, tx);
                        g.DrawString(tot.ToString("N2"),           fontBold, Brushes.Black,  right - colNumW - colProdW - colQtyW - colPriceW - colTotW, tx);
                        g.DrawLine(penGrid, left, y + rowH, right, y + rowH);
                    }

                    y += rowH;
                    itemIdx++;

                    if (y > e.MarginBounds.Bottom - 60)
                    {
                        e.HasMorePages = true;
                        return;
                    }
                }

                // ── Totals & Summary Block ───────────────────
                y += (isReceipt ? 6 : 10);
                g.DrawLine(penDark, left, y, right, y);
                y += (isReceipt ? 6 : 10);

                decimal disc = 0m;
                decimal.TryParse(txtDiscount.Text, out disc);
                decimal net = Math.Max(0m, gross - disc);

                if (!isReceipt)
                {
                    g.FillRectangle(brushTotBg, left, y, width, 32);
                    g.DrawRectangle(penDark, left, y, width, 32);

                    string totStr = $"إجمالي البضاعة: {gross:N2} ج" + (disc > 0 ? $"   |   الخصم: {disc:N2} ج" : "") + $"   |   الصافي النهائي للمطالبة: {net:N2} ج";
                    g.DrawString(totStr, fontBold, Brushes.DarkGreen, new RectangleF(left + 5, y + 6, width - 10, 20), sfCenter);
                    y += 38;
                }
                else
                {
                    if (disc > 0)
                    {
                        g.DrawString($"إجمالي البضاعة: {gross:N2} ج", fontBody, Brushes.Black, left, y); y += 18;
                        g.DrawString($"الخصم: {disc:N2} ج", fontBody, Brushes.DarkRed, left, y); y += 18;
                    }

                    g.DrawString($"الصافي النهائي للمطالبة: {net:N2} ج", fontBold, Brushes.Black, left, y);
                    y += 24;
                }

                // ── Disclaimer Box ──────────────────────────
                g.DrawLine(penGrid, left, y, right, y);
                y += 6;
                string noteStr = "* ملاحظة هامة: تعتبر هذه الفاتورة بياناً تقديرياً وعرض أسعار استرشادي للعميل، ولا خصم للمخزون إلا بعد الاعتماد وإصدار إذن التحضير.";
                g.DrawString(noteStr, fontBody, Brushes.DarkSlateGray, left, y);

                brushDarkBlue.Dispose(); brushHeaderBg.Dispose();
                brushRowAlt.Dispose(); brushTotBg.Dispose(); penGrid.Dispose(); penDark.Dispose();
                logoImg?.Dispose();
            };

            try
            {
                AppConfig.PrintInBackground(pd);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int pageW(PrintPageEventArgs e)
        {
            return e.PageBounds.Width;
        }

        private void SendQuoteWhatsApp()
        {
            if (_items == null || _items.Count == 0)
            {
                MessageBox.Show("لا يوجد أصناف في عرض الأسعار لإرساله!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string clientName = cboClient.Text.Trim();
                string phone = "";
                if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    object phObj = DbHelper.Scalar("SELECT Phone FROM Clients WHERE ClientID = @id", DbHelper.P("@id", ci.ID));
                    if (phObj != null && phObj != DBNull.Value) phone = phObj.ToString();
                }

                string quoteCode = _quoteCode ?? "";
                string tier = !string.IsNullOrEmpty(_selectedTier) ? _selectedTier : "قطاعي";
                decimal disc = 0m;
                if (txtDiscount != null) decimal.TryParse(txtDiscount.Text, out disc);
                string notes = txtNotes != null ? txtNotes.Text.Trim() : "";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"📋 *عرض سعر مقدم من {AppConfig.CompanyName}*");
                sb.AppendLine($"📅 *التاريخ:* {DateTime.Now:yyyy-MM-dd HH:mm}");
                sb.AppendLine($"👤 *العميل:* {clientName}");
                if (!string.IsNullOrEmpty(quoteCode)) sb.AppendLine($"🔖 *رقم العرض:* #{quoteCode}");
                sb.AppendLine();
                sb.AppendLine("📦 *تفاصيل عرض السعر:*");

                foreach (var item in _items)
                {
                    sb.AppendLine($"- {item.ProductName} ({item.Quantity} {item.UnitName} × {item.UnitPrice:N2}) = {item.TotalPrice:N2} ج");
                }

                sb.AppendLine();
                sb.AppendLine($"💵 *إجمالي عرض السعر:* {lblTotalVal.Text}");
                if (disc > 0) sb.AppendLine($"✂️ *الخصم:* {disc:N2} ج");
                if (disc > 0 && lblNetVal != null) sb.AppendLine($"💰 *الصافي المطلوب:* {lblNetVal.Text}");
                sb.AppendLine("\nنتشرف بخدمتكم دائماً! 🙏");

                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                    this,
                    phone,
                    sb.ToString(),
                    () => ReceiptImageGenerator.GenerateTextCardImage("عرض أسعار", sb.ToString()),
                    "📱 إرسال عرض السعر عبر الواتساب",
                    () => PdfReportHelper.GeneratePriceQuotePdf(clientName, phone, quoteCode, tier, _items, disc, notes),
                    () => ReceiptImageGenerator.GeneratePriceQuoteImages(clientName, phone, quoteCode, tier, _items, disc, notes));
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPriceQuote.SendQuoteWhatsApp", ex);
                MessageBox.Show("فشل إرسال عرض السعر عبر الواتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FrmPriceQuote_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1) { NewQuote(true); e.Handled = true; }
            else if (e.KeyCode == Keys.F2) { SaveQuote(false); e.Handled = true; }
            else if (e.KeyCode == Keys.F4) { OpenPendingQuotesList(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { ConvertToSaleInvoice(); e.Handled = true; }
            else if (e.KeyCode == Keys.F3) { OpenQuickSearch(); e.Handled = true; }
        }
    }
}

using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة فاتورة المشتريات مع دعم المخزن والباركود</summary>
    public class FrmPurchase : Form
    {
        private Button btnTypeCredit, btnTypeCash;
        private string _purchaseType = "Credit";
        private ComboBox cboSupplier, cboProduct, cboWarehouse;
        private DateTimePicker dtpDate;
        private TextBox txtNotes, txtBarcode;
        private DataGridView dgItems;
        private Panel pnlFooter, pnlItems;
        private Label lblTotalVal, lblNetVal, lblDiscType, lblDiscVal, lblNetTitle;
        private TextBox txtInvoiceDiscount;
        private ComboBox cboInvoiceDiscountType;
        private Button btnSave, btnNew, btnPrint;
        private NumericUpDown nudQty, nudPrice;
        private Button btnAddItem;
        private Label lblCashBalance;

        private List<PurchaseItemDTO> _items = new List<PurchaseItemDTO>();
        private int _lastPurchaseID = 0;

        public FrmPurchase()
        {
            InitUI();
            LoadCombos();
            ClearInvoice();
        }

        private Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontMain };
        }

        private void InitUI()
        {
            this.Text = "فاتورة مشتريات - شراء بضاعة";
            this.Size = new Size(1050, 730);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmPurchase_KeyDown;

            // ===== Top Panel (Header) =====
            Panel panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 170,
                Width = 1050,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            // Purchase Type Buttons
            Label lblType = MakeLabel("نوع الفاتورة:", 940, 12);
            btnTypeCredit = Theme.MakeButton("📋 آجل", 830, 8, 100, 30, Theme.Primary);
            btnTypeCash   = Theme.MakeButton("💵 نقدي", 720, 8, 100, 30, Color.FromArgb(60, 60, 60));
            btnTypeCredit.Click += (s, e) => { _purchaseType = "Credit"; ToggleType(); };
            btnTypeCash.Click   += (s, e) => { _purchaseType = "Cash";   ToggleType(); };

            // Supplier
            Label lblSupp = MakeLabel("المورد:", 940, 48);
            cboSupplier = new ComboBox
            {
                Location = new Point(640, 44), Width = 290,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };

            // Date
            Label lblDate = MakeLabel("التاريخ:", 560, 48);
            dtpDate = new DateTimePicker { Location = new Point(410, 44), Width = 140, Format = DateTimePickerFormat.Short };

            // Cash Balance indicator
            lblCashBalance = new Label
            {
                Text = "",
                Location = new Point(410, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 180, 100),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            // Notes
            Label lblNotes = MakeLabel("ملاحظات:", 940, 84);
            txtNotes = new TextBox { Location = new Point(640, 80), Width = 290, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            // ===== ROW 2: Barcode + Warehouse + Product + Qty + Price =====

            // Barcode field (السكنر يكتب هنا وبعدها Enter)
            Label lblBarcode = MakeLabel("باركود / كود:", 940, 122);
            txtBarcode = new TextBox
            {
                Name = "txtBarcode",          // يحتوي "Barcode" → يُعفى من EnterKeyFilter
                Location = new Point(750, 118),
                Width = 180,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;

            // Warehouse selector
            Label lblWarehouse = MakeLabel("المخزن:", 700, 122);
            cboWarehouse = new ComboBox
            {
                Location = new Point(530, 118), Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };

            // Product combo
            Label lblProd = MakeLabel("الصنف:", 490, 84);
            cboProduct = new ComboBox
            {
                Location = new Point(260, 80), Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };

            Label lblQty = MakeLabel("الكمية:", 230, 122);
            nudQty = new NumericUpDown
            {
                Location = new Point(150, 116), Width = 75,
                DecimalPlaces = 3, Minimum = 0.001m, Maximum = 999999, Value = 1,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            Label lblPrice = MakeLabel("السعر:", 120, 122);
            nudPrice = new NumericUpDown
            {
                Location = new Point(35, 116), Width = 80,
                DecimalPlaces = 2, Minimum = 0, Maximum = 999999,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            btnAddItem = Theme.MakeButton("➕ إضافة", 270, 114, 80, 30, Theme.Accent);
            btnAddItem.Click += BtnAddItem_Click;

            // Label hotkeys row 2
            Label lblHint = new Label
            {
                Text = "💡 اسحب الباركود بالسكنر أو اكتب الكود ثم اضغط Enter للإضافة الفورية",
                ForeColor = Color.FromArgb(100, 180, 100),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                Location = new Point(10, 148),
                AutoSize = true
            };

            panel.Controls.AddRange(new Control[]
            {
                lblType, btnTypeCredit, btnTypeCash,
                lblSupp, cboSupplier,
                lblDate, dtpDate, lblCashBalance,
                lblNotes, txtNotes,
                lblProd, cboProduct,
                lblBarcode, txtBarcode,
                lblWarehouse, cboWarehouse,
                lblQty, nudQty,
                lblPrice, nudPrice,
                btnAddItem,
                lblHint
            });

            // ===== Grid Panel =====
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
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID",   Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف",       ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",    HeaderText = "الكمية",      FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",   HeaderText = "سعر الشراء", FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice",  HeaderText = "الإجمالي",   ReadOnly = true, FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewButtonColumn  { Name = "Delete",      HeaderText = "حذف", Text = "❌", UseColumnTextForButtonValue = true, FillWeight = 25 });

            dgItems.CellValueChanged += DgItems_CellValueChanged;
            dgItems.CellClick        += DgItems_CellClick;
            dgItems.CellEndEdit      += (s, e) => RecalcTotals();

            pnlItems.Controls.Add(dgItems);

            // ===== Footer Panel =====
            pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 95, BackColor = Theme.BgCard };

            Label label5 = new Label { Text = "إجمالي الأصناف:", ForeColor = Theme.TextSub, Location = new Point(920, 15), AutoSize = true, Anchor = (AnchorStyles.Top | AnchorStyles.Right) };
            lblTotalVal  = new Label { Text = "0.00 ج", ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Location = new Point(810, 13), AutoSize = true, Anchor = (AnchorStyles.Top | AnchorStyles.Right) };

            lblDiscType = MakeLabel("خصم:", 710, 15);
            cboInvoiceDiscountType = new ComboBox { Location = new Point(635, 12), Width = 65, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            cboInvoiceDiscountType.Items.AddRange(new object[] { "مبلغ", "%" });
            cboInvoiceDiscountType.SelectedIndex = 0;
            cboInvoiceDiscountType.SelectedIndexChanged += (s, e) => RecalcTotals();

            lblDiscVal = MakeLabel("قيمة:", 585, 15);
            txtInvoiceDiscount = new TextBox { Location = new Point(495, 12), Width = 80, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "0" };
            txtInvoiceDiscount.TextChanged += (s, e) => RecalcTotals();

            lblNetTitle = MakeLabel("الصافي:", 400, 15);
            lblNetVal   = new Label { Text = "0.00 ج", ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Segoe UI", 13f, FontStyle.Bold), Location = new Point(280, 11), AutoSize = true };

            btnSave = Theme.MakeButton("💾 حفظ الفاتورة", 780, 50, 130, 32, Theme.Accent);
            Button btnHold = Theme.MakeButton("⏸️ تعليق", 670, 50, 100, 32, Color.FromArgb(200, 140, 50));
            Button btnLoadHold = Theme.MakeButton("📂 معلقات", 560, 50, 100, 32, Color.FromArgb(100, 100, 150));
            Button btnTawreed = Theme.MakeButton("💵 صرف", 450, 50, 100, 32, Theme.Success);
            btnNew = Theme.MakeButton("🆕 جديد", 360, 50, 80, 32, Color.FromArgb(80, 120, 80));
            btnPrint = Theme.MakeButton("🖨️ طباعة الأخيرة", 200, 50, 150, 32, Theme.Primary);
            Button btnWhatsApp = Theme.MakeButton("📲 واتساب", 30, 50, 160, 32, Color.FromArgb(37, 211, 102));

            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnHold.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoadHold.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTawreed.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNew.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnWhatsApp.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            btnSave.Click += BtnSave_Click;
            btnHold.Click += BtnHold_Click;
            btnLoadHold.Click += BtnLoadHold_Click;
            btnTawreed.Click += BtnSarf_Click;
            btnNew.Click += (s, e) => ClearInvoice();
            btnPrint.Click += BtnPrint_Click;
            btnWhatsApp.Click += BtnWhatsApp_Click;

            Label lblHotkeys = new Label
            {
                Text = "الاختصارات: [F2] فاتورة جديدة  |  [F5] حفظ الفاتورة  |  [F9] طباعة  |  [F12] تركيز على الباركود",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(10, 65),
                AutoSize = true,
                Anchor = (AnchorStyles.Bottom | AnchorStyles.Left)
            };

            pnlFooter.Controls.AddRange(new Control[] { label5, lblTotalVal, lblDiscType, cboInvoiceDiscountType, lblDiscVal, txtInvoiceDiscount, lblNetTitle, lblNetVal, btnSave, btnHold, btnLoadHold, btnTawreed, btnNew, btnPrint, btnWhatsApp, lblHotkeys });

            base.Controls.Add(pnlItems);
            base.Controls.Add(pnlFooter);
            base.Controls.Add(panel);
            pnlItems.BringToFront();
            ToggleType();
        }

        // ===== حدث الباركود - Enter يضيف الصنف مباشرة =====
        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                string code = txtBarcode.Text.Trim();
                if (string.IsNullOrEmpty(code)) return;

                DataRow row = WarehouseDAL.GetProductByBarcode(code);
                if (row == null)
                {
                    MessageBox.Show($"❌ لم يُعثر على صنف بالكود: {code}", "باركود غير موجود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtBarcode.SelectAll();
                    return;
                }

                int prodID   = Convert.ToInt32(row["ProductID"]);
                string name  = row["ProductName"].ToString();
                decimal price = row["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["PurchasePrice"]) : 0m;

                // إذا كان الصنف موجود مسبقاً أضف الكمية
                foreach (var item in _items)
                {
                    if (item.ProductID == prodID)
                    {
                        item.Quantity += nudQty.Value;
                        item.TotalPrice = item.Quantity * item.UnitPrice;
                        RefreshGrid();
                        txtBarcode.Clear();
                        txtBarcode.Focus();
                        return;
                    }
                }

                _items.Add(new PurchaseItemDTO
                {
                    ProductID   = prodID,
                    ProductName = name,
                    Quantity    = nudQty.Value,
                    UnitPrice   = price > 0 ? price : nudPrice.Value,
                    TotalPrice  = nudQty.Value * (price > 0 ? price : nudPrice.Value)
                });

                RefreshGrid();
                txtBarcode.Clear();
                txtBarcode.Focus();
            }
        }

        private void FrmPurchase_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.F5)
            {
                if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
            }

            if      (e.KeyCode == Keys.F2)  { ClearInvoice(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5)  { BtnSave_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.F12) { txtBarcode.Focus(); txtBarcode.SelectAll(); e.Handled = true; }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && dgItems.IsCurrentCellInEditMode)
            {
                dgItems.EndEdit();
                cboProduct.Focus();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ToggleType()
        {
            bool isCredit = _purchaseType == "Credit";
            btnTypeCredit.BackColor = isCredit  ? Theme.Primary : Color.FromArgb(60, 60, 60);
            btnTypeCash.BackColor   = !isCredit ? Theme.Accent  : Color.FromArgb(60, 60, 60);

            if (!isCredit)
            {
                var cashResult = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                decimal cashBal = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                lblCashBalance.Text      = $"💰 رصيد الخزنة: {cashBal:N2} ج";
                lblCashBalance.ForeColor = cashBal > 0 ? Color.FromArgb(100, 180, 100) : Color.OrangeRed;
            }
            else
            {
                lblCashBalance.Text = "";
            }
        }

        private void LoadCombos()
        {
            // Suppliers
            DataTable dtSup = SupplierDAL.GetAll(true);
            cboSupplier.Items.Clear();
            cboSupplier.Items.Add(new ComboItem(0, "-- اختر المورد --"));
            foreach (DataRow r in dtSup.Rows)
                cboSupplier.Items.Add(new ComboItem(Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
            cboSupplier.DisplayMember = "Text";
            cboSupplier.SelectedIndex = 0;

            // Products
            DataTable dtProd = ProductDAL.GetAll(true);
            cboProduct.Items.Clear();
            cboProduct.Items.Add(new ComboItem(0, "-- اختر الصنف --"));
            foreach (DataRow r in dtProd.Rows)
            {
                var ci = new ComboItem(Convert.ToInt32(r["ProductID"]), r["ProductName"].ToString());
                ci.Extra = r["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                cboProduct.Items.Add(ci);
            }
            cboProduct.DisplayMember = "Text";
            cboProduct.SelectedIndex = 0;
            cboProduct.SelectedIndexChanged += (s, e) =>
            {
                if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0)
                    nudPrice.Value = ci.Extra;
            };

            // Warehouses
            DataTable dtWh = WarehouseDAL.GetAll(true);
            cboWarehouse.Items.Clear();
            foreach (DataRow r in dtWh.Rows)
                cboWarehouse.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
            cboWarehouse.DisplayMember = "Text";
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (!(cboProduct.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر صنفاً أولاً"); return;
            }

            decimal qty   = nudQty.Value;
            decimal price = nudPrice.Value;
            if (qty <= 0 || price <= 0)
            {
                MessageBox.Show("أدخل كمية وسعر صحيحين"); return;
            }

            foreach (var item in _items)
            {
                if (item.ProductID == ci.ID)
                {
                    item.Quantity  += qty;
                    item.TotalPrice = item.Quantity * item.UnitPrice;
                    RefreshGrid();
                    cboProduct.SelectedIndex = 0;
                    nudQty.Value = 1;
                    cboProduct.Focus();
                    return;
                }
            }

            _items.Add(new PurchaseItemDTO
            {
                ProductID   = ci.ID,
                ProductName = ci.Text,
                Quantity    = qty,
                UnitPrice   = price,
                TotalPrice  = qty * price
            });

            RefreshGrid();
            cboProduct.SelectedIndex = 0;
            nudQty.Value = 1;
            cboProduct.Focus();
        }

        private void RefreshGrid()
        {
            dgItems.CellValueChanged -= DgItems_CellValueChanged;
            dgItems.Rows.Clear();
            foreach (var item in _items)
                dgItems.Rows.Add(item.ProductID, item.ProductName, item.Quantity.ToString("F3"), item.UnitPrice.ToString("F2"), item.TotalPrice.ToString("F2"));
            dgItems.CellValueChanged += DgItems_CellValueChanged;
            RecalcTotals();
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var item = _items[e.RowIndex];
            if (dgItems.Columns[e.ColumnIndex].Name == "Quantity")
            {
                decimal.TryParse(dgItems.Rows[e.RowIndex].Cells["Quantity"].Value?.ToString(), out decimal q);
                item.Quantity   = q;
                item.TotalPrice = q * item.UnitPrice;
                dgItems.Rows[e.RowIndex].Cells["TotalPrice"].Value = item.TotalPrice.ToString("F2");
            }
            else if (dgItems.Columns[e.ColumnIndex].Name == "UnitPrice")
            {
                decimal.TryParse(dgItems.Rows[e.RowIndex].Cells["UnitPrice"].Value?.ToString(), out decimal p);
                item.UnitPrice  = p;
                item.TotalPrice = item.Quantity * p;
                dgItems.Rows[e.RowIndex].Cells["TotalPrice"].Value = item.TotalPrice.ToString("F2");
            }
            RecalcTotals();
        }

        private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "Delete")
            {
                _items.RemoveAt(e.RowIndex);
                RefreshGrid();
            }
        }

        private void RecalcTotals()
        {
            decimal total = 0;
            foreach (var item in _items) total += item.TotalPrice;
            lblTotalVal.Text = total.ToString("N2") + " ج";

            decimal disc = 0;
            decimal.TryParse(txtInvoiceDiscount.Text, out disc);
            decimal discAmount = cboInvoiceDiscountType.SelectedIndex == 1 ? total * disc / 100 : disc;
            decimal net = total - discAmount;
            if (net < 0) net = 0;
            lblNetVal.Text = net.ToString("N2") + " ج";
        }

        private void ClearInvoice()
        {
            _items.Clear();
            RefreshGrid();
            if (cboSupplier.Items.Count  > 0) cboSupplier.SelectedIndex  = 0;
            if (cboProduct.Items.Count   > 0) cboProduct.SelectedIndex   = 0;
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
            txtNotes.Clear();
            txtBarcode.Clear();
            txtInvoiceDiscount.Text = "0";
            nudQty.Value   = 1;
            nudPrice.Value = 0;
            _purchaseType  = "Credit";
            ToggleType();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0) { MessageBox.Show("أضف أصنافاً أولاً"); return; }

            int? supplierID = null;
            if (cboSupplier.SelectedItem is ComboItem ci && ci.ID > 0)
                supplierID = ci.ID;

            if (_purchaseType == "Credit" && !supplierID.HasValue)
            {
                MessageBox.Show("اختر المورد أولاً للفواتير الآجلة"); return;
            }

            int? warehouseID = null;
            if (cboWarehouse.SelectedItem is ComboItem wh && wh.ID > 0)
                warehouseID = wh.ID;

            decimal total = 0;
            foreach (var item in _items) total += item.TotalPrice;

            decimal disc = 0;
            decimal.TryParse(txtInvoiceDiscount.Text, out disc);
            decimal discPct    = cboInvoiceDiscountType.SelectedIndex == 1 ? disc : 0;
            decimal discAmount = cboInvoiceDiscountType.SelectedIndex == 1 ? total * disc / 100 : disc;
            decimal net        = total - discAmount;

            try
            {
                int id = PurchaseDAL.SavePurchase(_purchaseType, supplierID, net, txtNotes.Text, _items, discAmount, discPct, warehouseID);

                if (id > 0)
                {
                    _lastPurchaseID = id;
                    MessageBox.Show($"✅ تم حفظ فاتورة المشتريات بنجاح\nرقم الفاتورة: PUR-{id}", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    PromptBarcodePrintAfterPurchase();
                    ClearInvoice();
                    LoadCombos();
                }
                else
                {
                    MessageBox.Show("❌ فشل حفظ الفاتورة");
                }
            }
            catch (Exception)
            {
                // Transaction rollback already shown by DbHelper
            }
        }

        private void PromptBarcodePrintAfterPurchase()
        {
            var itemsToPrint = new List<PurchaseItemDTO>();
            foreach (var item in _items)
            {
                var dr = ProductDAL.GetByID(item.ProductID);
                if (dr != null)
                {
                    bool hasBarcode = dr["HasBarcode"] == DBNull.Value || Convert.ToBoolean(dr["HasBarcode"]);
                    if (hasBarcode)
                    {
                        itemsToPrint.Add(item);
                    }
                }
            }

            if (itemsToPrint.Count == 0) return;

            if (MessageBox.Show("📦 تم اكتشاف أصناف لها باركود في الفاتورة.\n\nهل تريد طباعة ملصقات باركود لهذه الأصناف الآن؟", 
                "طباعة باركود الأصناف المشتراة", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var item in itemsToPrint)
                {
                    int labelQty = (int)Math.Max(1, Math.Ceiling(item.Quantity));
                    var dr = ProductDAL.GetByID(item.ProductID);
                    if (dr != null)
                    {
                        string codeToPrint = dr["InternationalBarcode"] != DBNull.Value && !string.IsNullOrWhiteSpace(dr["InternationalBarcode"].ToString())
                            ? dr["InternationalBarcode"].ToString()
                            : dr["ProductCode"].ToString();

                        PrintBarcodeLabels(
                            codeToPrint, 
                            dr["ProductName"].ToString(), 
                            Convert.ToDecimal(dr["SalePrice"]), 
                            labelQty
                        );
                    }
                }
            }
        }

        private void PrintBarcodeLabels(string code, string name, decimal price, int qty)
        {
            try
            {
                var pd = new System.Drawing.Printing.PrintDocument();
                if (AppConfig.ThermalPrinterEnabled && !string.IsNullOrEmpty(AppConfig.ThermalPrinterName))
                {
                    pd.PrinterSettings.PrinterName = AppConfig.ThermalPrinterName;
                }
                
                pd.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("BarcodeLabel", 180, 100);
                pd.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(5, 5, 5, 5);

                int printedCount = 0;
                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics;
                    var nameFont = new Font("Arial", 8, FontStyle.Bold);
                    var codeFont = new Font("Arial", 10, FontStyle.Bold);
                    var priceFont = new Font("Arial", 8);

                    int pageW = ev.PageBounds.Width;
                    int pageH = ev.PageBounds.Height;
                    
                    var center = new StringFormat { Alignment = StringAlignment.Center };

                    g.DrawString(AppConfig.CompanyName, priceFont, Brushes.Black, new RectangleF(0, 5, pageW, 14), center);
                    g.DrawString(name, nameFont, Brushes.Black, new RectangleF(0, 20, pageW, 16), center);
                    g.DrawString($"* {code} *", codeFont, Brushes.Black, new RectangleF(0, 40, pageW, 20), center);
                    
                    // Simulated barcode lines
                    int startX = 30;
                    int endX = pageW - 30;
                    int barY = 60;
                    int barH = 15;
                    Pen thinPen = new Pen(Color.Black, 1.5f);
                    Pen thickPen = new Pen(Color.Black, 3f);
                    
                    for (int x = startX; x < endX; x += 4)
                    {
                        if (x % 3 == 0)
                            g.DrawLine(thickPen, x, barY, x, barY + barH);
                        else
                            g.DrawLine(thinPen, x, barY, x, barY + barH);
                    }

                    g.DrawString($"السعر: {price:N2} ج", nameFont, Brushes.Black, new RectangleF(0, 80, pageW, 16), center);

                    printedCount++;
                    ev.HasMorePages = printedCount < qty;
                };

                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشلت طباعة الباركود للصنف '{name}':\n{ex.Message}", "خطأ طباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void BtnHold_Click(object sender, EventArgs e)
        {
            MessageBox.Show("تحت التطوير", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnLoadHold_Click(object sender, EventArgs e)
        {
            MessageBox.Show("تحت التطوير", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSarf_Click(object sender, EventArgs e)
        {
            MessageBox.Show("تحت التطوير", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (_lastPurchaseID > 0)
            {
                MessageBox.Show("سيتم طباعة الفاتورة رقم " + _lastPurchaseID, "طباعة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("لا توجد فاتورة سابقة لطباعتها", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnWhatsApp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("تحت التطوير", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

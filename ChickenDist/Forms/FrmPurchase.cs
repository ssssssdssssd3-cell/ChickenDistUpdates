using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة فاتورة المشتريات الاحترافية</summary>
    public class FrmPurchase : Form
    {
        private Button btnTypeCredit, btnTypeCash;
        private string _purchaseType = "Credit";
        private ComboBox cboSupplier, cboProduct;
        private DateTimePicker dtpDate;
        private TextBox txtNotes;
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
            this.Size = new Size(1050, 700);
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
                Height = 140,
                Width = 950,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            // Purchase Type Buttons
            Label lblType = MakeLabel("نوع الفاتورة:", 940, 12);
            btnTypeCredit = Theme.MakeButton("📋 آجل", 830, 8, 100, 30, Theme.Primary);
            btnTypeCash = Theme.MakeButton("💵 نقدي", 720, 8, 100, 30, Color.FromArgb(60, 60, 60));

            btnTypeCredit.Click += (s, e) => { _purchaseType = "Credit"; ToggleType(); };
            btnTypeCash.Click += (s, e) => { _purchaseType = "Cash"; ToggleType(); };

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

            // Product Selection Row
            Label lblProd = MakeLabel("الصنف:", 560, 84);
            cboProduct = new ComboBox
            {
                Location = new Point(320, 80), Width = 230,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };

            Label lblQty = MakeLabel("الكمية:", 290, 84);
            nudQty = new NumericUpDown { Location = new Point(200, 78), Width = 80, DecimalPlaces = 3, Minimum = 0.001m, Maximum = 999999, Value = 1, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            Label lblPrice = MakeLabel("السعر:", 170, 84);
            nudPrice = new NumericUpDown { Location = new Point(80, 78), Width = 80, DecimalPlaces = 2, Minimum = 0, Maximum = 999999, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            btnAddItem = Theme.MakeButton("➕ إضافة", 5, 76, 70, 30, Theme.Accent);
            btnAddItem.Click += BtnAddItem_Click;

            panel.Controls.AddRange(new Control[] { lblType, btnTypeCredit, btnTypeCash, lblSupp, cboSupplier, lblDate, dtpDate, lblCashBalance, lblNotes, txtNotes, lblProd, cboProduct, lblQty, nudQty, lblPrice, nudPrice, btnAddItem });

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
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر الشراء", FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "الإجمالي", ReadOnly = true, FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "حذف", Text = "❌", UseColumnTextForButtonValue = true, FillWeight = 25 });

            dgItems.CellValueChanged += DgItems_CellValueChanged;
            dgItems.CellClick += DgItems_CellClick;
            dgItems.CellEndEdit += (s, e) => RecalcTotals();

            pnlItems.Controls.Add(dgItems);

            // ===== Footer Panel =====
            pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 95, Width = 1050, BackColor = Theme.BgCard };

            Label label5 = new Label { Text = "إجمالي الأصناف:", ForeColor = Theme.TextSub, Location = new Point(920, 15), AutoSize = true, Anchor = (AnchorStyles.Top | AnchorStyles.Right) };
            lblTotalVal = new Label { Text = "0.00 ج", ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Location = new Point(810, 13), AutoSize = true, Anchor = (AnchorStyles.Top | AnchorStyles.Right) };

            lblDiscType = MakeLabel("خصم:", 710, 15);
            cboInvoiceDiscountType = new ComboBox { Location = new Point(635, 12), Width = 65, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            cboInvoiceDiscountType.Items.AddRange(new object[] { "مبلغ", "%" });
            cboInvoiceDiscountType.SelectedIndex = 0;
            cboInvoiceDiscountType.SelectedIndexChanged += (s, e) => RecalcTotals();

            lblDiscVal = MakeLabel("قيمة:", 585, 15);
            txtInvoiceDiscount = new TextBox { Location = new Point(495, 12), Width = 80, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "0" };
            txtInvoiceDiscount.TextChanged += (s, e) => RecalcTotals();

            lblNetTitle = MakeLabel("الصافي:", 400, 15);
            lblNetVal = new Label { Text = "0.00 ج", ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Segoe UI", 13f, FontStyle.Bold), Location = new Point(280, 11), AutoSize = true };

            btnSave = Theme.MakeButton("💾 حفظ الفاتورة [F5]", Theme.Accent);
            btnSave.Size = new Size(160, 35);
            btnSave.Location = new Point(10, 8);
            btnSave.Click += BtnSave_Click;

            btnNew = Theme.MakeButton("🆕 فاتورة جديدة [F2]", Color.FromArgb(60, 100, 60));
            btnNew.Size = new Size(155, 35);
            btnNew.Location = new Point(175, 8);
            btnNew.Click += (s, e) => ClearInvoice();

            btnPrint = Theme.MakeButton("🖨 طباعة [F9]", Color.FromArgb(80, 80, 80));
            btnPrint.Size = new Size(100, 35);
            btnPrint.Location = new Point(335, 8);

            Label lblHotkeys = new Label
            {
                Text = "الاختصارات: [F2] فاتورة جديدة  |  [F5] حفظ الفاتورة  |  [F12] بحث سريع عن صنف",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(10, 65),
                AutoSize = true,
                Anchor = (AnchorStyles.Bottom | AnchorStyles.Left)
            };

            pnlFooter.Controls.AddRange(new Control[] { label5, lblTotalVal, lblDiscType, cboInvoiceDiscountType, lblDiscVal, txtInvoiceDiscount, lblNetTitle, lblNetVal, btnSave, btnNew, btnPrint, lblHotkeys });

            base.Controls.Add(pnlItems);
            base.Controls.Add(pnlFooter);
            base.Controls.Add(panel);
            pnlItems.BringToFront();
            ToggleType();
            Theme.ApplyFormRTL(this);
        }

        private void FrmPurchase_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.F5)
            {
                if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
            }

            if (e.KeyCode == Keys.F2) { ClearInvoice(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { BtnSave_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.F12) { cboProduct.Focus(); e.Handled = true; }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                if (dgItems.Focused || dgItems.EditingControl != null)
                {
                    var curCell = dgItems.CurrentCell;
                    if (curCell != null)
                    {
                        dgItems.EndEdit();
                        // Find next editable cell in the same row
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
                            // No more editable cells in this row, go to cboProduct
                            cboProduct.Focus();
                            return true;
                        }
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ToggleType()
        {
            bool isCredit = _purchaseType == "Credit";
            btnTypeCredit.BackColor = isCredit ? Theme.Primary : Color.FromArgb(60, 60, 60);
            btnTypeCash.BackColor = !isCredit ? Theme.Accent : Color.FromArgb(60, 60, 60);

            // Show cash balance when Cash type selected
            if (!isCredit)
            {
                var cashResult = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                decimal cashBal = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                lblCashBalance.Text = $"💰 رصيد الخزنة: {cashBal:N2} ج";
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
                {
                    nudPrice.Value = ci.Extra;
                }
            };
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (!(cboProduct.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر صنفاً أولاً"); return;
            }

            decimal qty = nudQty.Value;
            decimal price = nudPrice.Value;
            if (qty <= 0 || price <= 0)
            {
                MessageBox.Show("أدخل كمية وسعر صحيحين"); return;
            }

            // Check if item already exists
            foreach (var item in _items)
            {
                if (item.ProductID == ci.ID)
                {
                    item.Quantity += qty;
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
                ProductID = ci.ID,
                ProductName = ci.Text,
                Quantity = qty,
                UnitPrice = price,
                TotalPrice = qty * price
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
            {
                dgItems.Rows.Add(item.ProductID, item.ProductName, item.Quantity.ToString("F3"), item.UnitPrice.ToString("F2"), item.TotalPrice.ToString("F2"));
            }
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
                item.Quantity = q;
                item.TotalPrice = q * item.UnitPrice;
                dgItems.Rows[e.RowIndex].Cells["TotalPrice"].Value = item.TotalPrice.ToString("F2");
            }
            else if (dgItems.Columns[e.ColumnIndex].Name == "UnitPrice")
            {
                decimal.TryParse(dgItems.Rows[e.RowIndex].Cells["UnitPrice"].Value?.ToString(), out decimal p);
                item.UnitPrice = p;
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
            if (cboSupplier.Items.Count > 0) cboSupplier.SelectedIndex = 0;
            if (cboProduct.Items.Count > 0) cboProduct.SelectedIndex = 0;
            txtNotes.Clear();
            txtInvoiceDiscount.Text = "0";
            nudQty.Value = 1;
            nudPrice.Value = 0;
            _purchaseType = "Credit";
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

            decimal total = 0;
            foreach (var item in _items) total += item.TotalPrice;

            decimal disc = 0;
            decimal.TryParse(txtInvoiceDiscount.Text, out disc);
            decimal discPct = cboInvoiceDiscountType.SelectedIndex == 1 ? disc : 0;
            decimal discAmount = cboInvoiceDiscountType.SelectedIndex == 1 ? total * disc / 100 : disc;
            decimal net = total - discAmount;

            try
            {
                int id = PurchaseDAL.SavePurchase(_purchaseType, supplierID, net, txtNotes.Text, _items, discAmount, discPct);

                if (id > 0)
                {
                    _lastPurchaseID = id;
                    MessageBox.Show($"✅ تم حفظ فاتورة المشتريات بنجاح\nرقم الفاتورة: PUR-{id}", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearInvoice();
                    LoadCombos();
                }
                else
                {
                    MessageBox.Show("❌ فشل حفظ الفاتورة");
                }
            }
            catch (Exception ex)
            {
                // FIX: تسجيل الخطأ وإظهاره للمستخدم بدلاً من ابتلاعه صامتاً
                AppLogger.Error("فشل حفظ فاتورة المشتريات", ex, "FrmPurchase.BtnSave_Click");
                MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

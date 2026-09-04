using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة التحويل المخزني بين المخازن</summary>
    public class FrmWarehouseTransfer : Form
    {
        private ComboBox cboFromWarehouse, cboToWarehouse;
        private NumericUpDown nudQty;
        private Label lblAvailableStock;
        private TextBox txtNotes, txtBarcodeTransfer, txtSelectedProduct;
        private Button btnSearchProduct, btnAddItem, btnSave, btnNew;
        private DataGridView dgItems;
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
            this.Text = "تحويل مخزني بين المخازن";
            this.Size = new Size(1000, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.F5) { BtnSave_Click(null, null); e.Handled = true; }
                else if (e.KeyCode == Keys.F2) { ClearForm(); e.Handled = true; }
                else if (e.KeyCode == Keys.F3) { OpenProductSearch(); e.Handled = true; }
            };

            // ── Header Panel ───────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 145,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 8, 12, 8)
            };

            // Title bar inside header
            var lblTitle = new Label
            {
                Text = "🔄  تحويل المخزون بين المستودعات",
                Font = Theme.FontHeader,
                ForeColor = Theme.Accent,
                Location = new Point(10, 8),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            // Row 1: From warehouse / To warehouse / Notes
            var lblFrom = new Label { Text = "من المستودع:", Location = new Point(935, 42), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            cboFromWarehouse = new ComboBox
            {
                Location = new Point(770, 38),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };
            cboFromWarehouse.SelectedIndexChanged += CboWarehouse_Changed;

            var lblTo = new Label { Text = "إلى المستودع:", Location = new Point(715, 42), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            cboToWarehouse = new ComboBox
            {
                Location = new Point(550, 38),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };

            var lblNotes = new Label { Text = "ملاحظات:", Location = new Point(480, 42), AutoSize = true, ForeColor = Theme.TextMain };
            txtNotes = new TextBox
            {
                Location = new Point(140, 38),
                Width = 335,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                RightToLeft = RightToLeft.Yes
            };

            pnlHeader.Controls.AddRange(new Control[] { lblFrom, cboFromWarehouse, lblTo, cboToWarehouse, lblNotes, txtNotes });

            // Row 2: Barcode scanner field
            var lblBarcode = new Label { Text = "الاسكنر:", Location = new Point(965, 85), AutoSize = true, ForeColor = Theme.TextMain };
            txtBarcodeTransfer = new TextBox
            {
                Name = "txtBarcodeTransfer",
                Location = new Point(855, 82),
                Width = 105,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtBarcodeTransfer.KeyDown += TxtBarcodeTransfer_KeyDown;

            // Row 2: Search button + Selected product textbox
            btnSearchProduct = Theme.MakeButton("🔍 بحث الأصناف [F3]", 695, 78, 155, 34, Theme.Primary);
            btnSearchProduct.Font = new Font(Theme.FontMain.FontFamily, 9.5f, FontStyle.Bold);
            btnSearchProduct.Click += (s, e) => OpenProductSearch();

            txtSelectedProduct = new TextBox
            {
                Location = new Point(395, 82),
                Width = 295,
                ReadOnly = true,
                BackColor = Color.FromArgb(35, 45, 60),
                ForeColor = Color.FromArgb(240, 245, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 9.5f, FontStyle.Bold),
                Text = "اضغط [F3] أو زر البحث لاختيار الصنف...",
                Cursor = Cursors.Hand
            };
            txtSelectedProduct.Click += (s, e) => OpenProductSearch();

            var lblQty = new Label { Text = "الكمية:", Location = new Point(345, 85), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            nudQty = new NumericUpDown
            {
                Location = new Point(255, 82),
                Width = 85,
                DecimalPlaces = 3, Minimum = 0.001m, Maximum = 999999, Value = 1,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold),
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

            lblAvailableStock = new Label
            {
                Text = "متاح: --",
                Location = new Point(145, 85),
                Width = 105,
                ForeColor = Theme.TextSub,
                Font = Theme.FontBold,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnAddItem = Theme.MakeButton("➕ إضافة [Enter]", 10, 78, 130, 34, Theme.Accent);
            btnAddItem.Font = new Font(Theme.FontMain.FontFamily, 9.5f, FontStyle.Bold);
            btnAddItem.Click += BtnAddItem_Click;

            pnlHeader.Controls.AddRange(new Control[] { lblBarcode, txtBarcodeTransfer, btnSearchProduct, txtSelectedProduct, lblQty, nudQty, lblAvailableStock, btnAddItem });

            // ── Items Grid ─────────────────────────────────────────────────
            var pnlItems = new Panel { Dock = DockStyle.Fill };

            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard, ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 150 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvailableStock", HeaderText = "المتاح بالمصدر", FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المحولة", FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "حذف", Text = "❌", UseColumnTextForButtonValue = true, FillWeight = 30 });
            dgItems.CellClick += DgItems_CellClick;
            dgItems.DoubleClick += (s, e) => OpenProductSearch();

            pnlItems.Controls.Add(dgItems);

            // ── Footer ─────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10)
            };

            btnSave = Theme.MakeButton("💾 حفظ التحويل [F5]", 10, 10, 180, 38, Theme.Accent);
            btnSave.Font = new Font(Theme.FontMain.FontFamily, 10.5f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;

            btnNew = Theme.MakeButton("🆕 تحويل جديد [F2]", 200, 10, 160, 38, Color.FromArgb(60, 100, 60));
            btnNew.Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold);
            btnNew.Click += (s, e) => ClearForm();

            var lblCount = new Label
            {
                Name = "lblItemCount",
                Text = "الأصناف المحولة: 0",
                Location = new Point(410, 20),
                AutoSize = true,
                ForeColor = Theme.TextSub,
                Font = Theme.FontBold
            };

            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnNew, lblCount });

            // ── Assemble ───────────────────────────────────────────────────
            this.Controls.Add(pnlItems);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);

            Theme.ApplyFormRTL(this);
        }

        private void LoadWarehouses()
        {
            var dt = WarehouseDAL.GetAll(true);
            cboFromWarehouse.Items.Clear();
            cboToWarehouse.Items.Clear();
            cboFromWarehouse.Items.Add(new ComboItem(0, "-- اختر المخزن --"));
            cboToWarehouse.Items.Add(new ComboItem(0, "-- اختر المخزن --"));
            foreach (DataRow r in dt.Rows)
            {
                var ci = new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString());
                cboFromWarehouse.Items.Add(ci);
                cboToWarehouse.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
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
                if (MessageBox.Show("تنبيه: تغيير مستودع المصدر سيؤدي إلى مسح الأصناف المحددة لإعادة فحص أرصدتها بالمستودع الجديد. هل تريد المتابعة؟", "تغيير المستودع", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
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
                _selectedProductStock = InventoryDAL.GetProductStock(_selectedProductID, warehouseID);

                lblAvailableStock.Text = $"متاح: {_selectedProductStock:G29} ({_selectedProductUnit})";
                lblAvailableStock.ForeColor = _selectedProductStock > 0 ? Color.FromArgb(80, 200, 120) : Color.OrangeRed;

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
                lblAvailableStock.Text = $"متاح: {_selectedProductStock:G29} ({_selectedProductUnit})";
                lblAvailableStock.ForeColor = _selectedProductStock > 0 ? Color.FromArgb(80, 200, 120) : Color.OrangeRed;
            }
            else
            {
                _selectedProductStock = 0m;
                lblAvailableStock.Text = "متاح: --";
                lblAvailableStock.ForeColor = Theme.TextSub;
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
                    MessageBox.Show($"لم يتم العثور على صنف بهذا الكود: {barcode}", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        MessageBox.Show($"❌ الكمية الإجمالية ({newQty:G29}) تتجاوز الرصيد المتاح في المستودع المصدر ({available:G29})!", "عجز في الرصيد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"❌ الكمية المطلوبة ({qty:G29}) تتجاوز الرصيد المتاح في المستودع المصدر ({available:G29})!", "عجز في الرصيد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _items.Add(new TransferItemDTO 
            { 
                ProductID = productID, 
                ProductCode = productCode,
                ProductName = productName, 
                Quantity = qty,
                AvailableStock = available,
                Unit = unit
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
            txtSelectedProduct.Text = "اضغط [F3] أو زر البحث لاختيار الصنف...";
            lblAvailableStock.Text = "متاح: --";
            lblAvailableStock.ForeColor = Theme.TextSub;
            nudQty.Value = 1;
            txtBarcodeTransfer.Focus();
        }

        private void RefreshGrid()
        {
            dgItems.Rows.Clear();
            foreach (var item in _items)
            {
                dgItems.Rows.Add(
                    item.ProductID, 
                    item.ProductCode, 
                    item.ProductName, 
                    item.AvailableStock.ToString("G29"), 
                    item.Quantity.ToString("G29"), 
                    item.Unit ?? "قطعة"
                );
            }

            var lbl = this.Controls.Find("lblItemCount", true);
            if (lbl.Length > 0) ((Label)lbl[0]).Text = $"الأصناف المحولة: {_items.Count}";
        }

        private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgItems.Columns[e.ColumnIndex].Name != "Delete") return;
            _items.RemoveAt(e.RowIndex);
            RefreshGrid();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("WarehouseTransfer")) { MessageBox.Show("⛔ ليس لديك صلاحية حفظ التحويلات المخزنية.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!(cboFromWarehouse.SelectedItem is ComboItem from) || from.ID <= 0)
            { MessageBox.Show("اختر مستودع المصدر", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!(cboToWarehouse.SelectedItem is ComboItem to) || to.ID <= 0)
            { MessageBox.Show("اختر مستودع الهدف", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (from.ID == to.ID)
            { MessageBox.Show("لا يمكن التحويل لنفس المستودع!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (_items.Count == 0)
            { MessageBox.Show("لا توجد أصناف للتحويل، أضف أصنافاً أولاً بالضغط على [بحث الأصناف]!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string msg = $"هل تريد تأكيد التحويل المخزني؟\n\nمن: {from.Text}\nإلى: {to.Text}\nعدد الأصناف المحولة: {_items.Count}";
            if (MessageBox.Show(msg, "تأكيد التحويل المخزني", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                int transferID = TransferDAL.SaveTransfer(from.ID, to.ID, txtNotes.Text.Trim(), _items);
                if (transferID > 0)
                {
                    MessageBox.Show($"✅ تم حفظ التحويل المخزني بنجاح!\nرقم الإذن: TRF-{transferID}", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                }
                else
                {
                    MessageBox.Show("❌ فشل في حفظ التحويل المخزني.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ أثناء حفظ التحويل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            var lbl = this.Controls.Find("lblItemCount", true);
            if (lbl.Length > 0) ((Label)lbl[0]).Text = "الأصناف المحولة: 0";
        }
    }
}

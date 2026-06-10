using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة التحويل المخزني بين المستودعات</summary>
    public class FrmWarehouseTransfer : Form
    {
        private ComboBox cboFromWarehouse, cboToWarehouse, cboProduct;
        private NumericUpDown nudQty;
        private Label lblAvailableStock;
        private TextBox txtNotes, txtBarcodeTransfer;
        private DataGridView dgItems;
        private Button btnAddItem, btnSave, btnNew;
        private List<TransferItemDTO> _items = new List<TransferItemDTO>();

        public FrmWarehouseTransfer()
        {
            InitUI();
            LoadWarehouses();
            LoadProducts();
        }

        private void InitUI()
        {
            this.Text = "تحويل مخزني بين المستودعات";
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
            };

            // ── Header Panel ───────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
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
            var lblFrom = new Label { Text = "من المستودع:", Location = new Point(940, 42), AutoSize = true, ForeColor = Theme.TextMain };
            cboFromWarehouse = new ComboBox
            {
                Location = new Point(780, 38),
                Width = 155,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };
            cboFromWarehouse.SelectedIndexChanged += CboWarehouse_Changed;

            var lblTo = new Label { Text = "إلى المستودع:", Location = new Point(745, 42), AutoSize = true, ForeColor = Theme.TextMain };
            cboToWarehouse = new ComboBox
            {
                Location = new Point(585, 38),
                Width = 155,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };

            var lblNotes = new Label { Text = "ملاحظات:", Location = new Point(555, 42), AutoSize = true, ForeColor = Theme.TextMain };
            txtNotes = new TextBox
            {
                Location = new Point(320, 38),
                Width = 230,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlHeader.Controls.AddRange(new Control[] { lblFrom, cboFromWarehouse, lblTo, cboToWarehouse, lblNotes, txtNotes });

            // Row 2: Barcode scanner field
            var lblBarcode = new Label { Text = "باركود الاسكنر:", Location = new Point(940, 82), AutoSize = true, ForeColor = Theme.TextMain };
            txtBarcodeTransfer = new TextBox
            {
                Name = "txtBarcodeTransfer",
                Location = new Point(740, 78),
                Width = 195,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtBarcodeTransfer.KeyDown += TxtBarcodeTransfer_KeyDown;

            // Row 2: Product selection
            var lblProd = new Label { Text = "الصنف:", Location = new Point(720, 82), AutoSize = true, ForeColor = Theme.TextMain };
            cboProduct = new ComboBox
            {
                Location = new Point(340, 78),
                Width = 375,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };
            cboProduct.SelectedIndexChanged += CboProduct_Changed;

            var lblQty = new Label { Text = "الكمية:", Location = new Point(305, 82), AutoSize = true, ForeColor = Theme.TextMain };
            nudQty = new NumericUpDown
            {
                Location = new Point(200, 78),
                Width = 100,
                DecimalPlaces = 3, Minimum = 0.001m, Maximum = 999999, Value = 1,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            lblAvailableStock = new Label
            {
                Text = "متاح: --",
                Location = new Point(10, 82),
                Width = 185,
                ForeColor = Color.FromArgb(80, 200, 120),
                Font = Theme.FontBold,
                AutoSize = false
            };

            btnAddItem = Theme.MakeButton("➕ إضافة [Enter]", 10, 112, 150, 30, Theme.Accent);
            btnAddItem.Click += BtnAddItem_Click;

            pnlHeader.Controls.AddRange(new Control[] { lblBarcode, txtBarcodeTransfer, lblProd, cboProduct, lblQty, nudQty, lblAvailableStock, btnAddItem });

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
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", FillWeight = 150 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المحولة", FillWeight = 60 });
            dgItems.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "حذف", Text = "❌", UseColumnTextForButtonValue = true, FillWeight = 30 });
            dgItems.CellClick += DgItems_CellClick;

            pnlItems.Controls.Add(dgItems);

            // ── Footer ─────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8)
            };

            btnSave = Theme.MakeButton("💾 حفظ التحويل [F5]", 10, 10, 170, 35, Theme.Accent);
            btnSave.Click += BtnSave_Click;

            btnNew = Theme.MakeButton("🆕 تحويل جديد [F2]", 190, 10, 160, 35, Color.FromArgb(60, 100, 60));
            btnNew.Click += (s, e) => ClearForm();

            var lblCount = new Label
            {
                Name = "lblItemCount",
                Text = "الأصناف: 0",
                Location = new Point(400, 18),
                AutoSize = true,
                ForeColor = Theme.TextSub,
                Font = Theme.FontBold
            };

            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnNew, lblCount });

            // ترتيب صحيح: Bottom ثم Top ثم Fill
            this.Controls.Add(pnlFooter);  // Bottom - يُضاف أولاً
            this.Controls.Add(pnlHeader);  // Top
            this.Controls.Add(pnlItems);   // Fill - يُضاف أخيراً

            Theme.ApplyFormRTL(this);
        }

        private void LoadWarehouses()
        {
            var dt = WarehouseDAL.GetAll(true);
            cboFromWarehouse.Items.Clear();
            cboToWarehouse.Items.Clear();
            cboFromWarehouse.Items.Add(new ComboItem(0, "-- اختر المستودع --"));
            cboToWarehouse.Items.Add(new ComboItem(0, "-- اختر المستودع --"));
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

        private void LoadProducts()
        {
            var dt = ProductDAL.GetAll(true);
            cboProduct.Items.Clear();
            cboProduct.Items.Add(new ComboItem(0, "-- اختر الصنف --"));
            foreach (DataRow r in dt.Rows)
                cboProduct.Items.Add(new ComboItem(Convert.ToInt32(r["ProductID"]), r["ProductName"].ToString()));
            cboProduct.DisplayMember = "Text";
            cboProduct.SelectedIndex = 0;
        }

        private void CboWarehouse_Changed(object sender, EventArgs e)
        {
            UpdateAvailableStock();
        }

        private void CboProduct_Changed(object sender, EventArgs e)
        {
            UpdateAvailableStock();
        }

        private void UpdateAvailableStock()
        {
            if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0 &&
                cboFromWarehouse.SelectedItem is ComboItem wh && wh.ID > 0)
            {
                decimal stock = InventoryDAL.GetProductStock(ci.ID, wh.ID);
                lblAvailableStock.Text = $"متاح: {stock:N3}";
                lblAvailableStock.ForeColor = stock > 0 ? Color.FromArgb(80, 200, 120) : Color.OrangeRed;
            }
            else
            {
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

                // البحث عن الصنف بالباركود أو كود الصنف
                var dt = ProductDAL.FindByCode(barcode);
                if (dt != null && dt.Rows.Count > 0)
                {
                    int pid = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                    // إيجاد الصنف في الكومبو وتحديده
                    foreach (var item in cboProduct.Items)
                    {
                        if (item is ComboItem ci && ci.ID == pid)
                        {
                            cboProduct.SelectedItem = ci;
                            break;
                        }
                    }
                    // إضافة مباشرة بالكمية 1
                    AddItemToGrid(pid, dt.Rows[0]["ProductName"].ToString(), 1);
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
            if (!(cboProduct.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر صنفاً أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            AddItemToGrid(ci.ID, ci.Text, nudQty.Value);
        }

        private void AddItemToGrid(int productID, string productName, decimal qty)
        {
            if (!(cboFromWarehouse.SelectedItem is ComboItem wh) || wh.ID == 0)
            {
                MessageBox.Show("اختر مستودع المصدر أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // تحقق من الرصيد المتاح
            decimal available = InventoryDAL.GetProductStock(productID, wh.ID);

            // دمج إذا كان الصنف موجود مسبقاً
            foreach (var existing in _items)
            {
                if (existing.ProductID == productID)
                {
                    decimal newQty = existing.Quantity + qty;
                    if (newQty > available)
                    {
                        MessageBox.Show($"الكمية الإجمالية ({newQty:N3}) تتجاوز الرصيد المتاح ({available:N3})", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    existing.Quantity = newQty;
                    RefreshGrid();
                    return;
                }
            }

            if (qty > available)
            {
                MessageBox.Show($"الكمية المطلوبة ({qty:N3}) تتجاوز الرصيد المتاح في المستودع المصدر ({available:N3})", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _items.Add(new TransferItemDTO { ProductID = productID, ProductName = productName, Quantity = qty });
            RefreshGrid();
            nudQty.Value = 1;
            cboProduct.SelectedIndex = 0;
        }

        private void RefreshGrid()
        {
            dgItems.Rows.Clear();
            foreach (var item in _items)
                dgItems.Rows.Add(item.ProductID, item.ProductName, item.Quantity.ToString("N3"));

            // Update count label
            var lbl = this.Controls.Find("lblItemCount", true);
            if (lbl.Length > 0) ((Label)lbl[0]).Text = $"الأصناف: {_items.Count}";
        }

        private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgItems.Columns[e.ColumnIndex].Name != "Delete") return;
            _items.RemoveAt(e.RowIndex);
            RefreshGrid();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!(cboFromWarehouse.SelectedItem is ComboItem from) || from.ID == 0)
            { MessageBox.Show("اختر مستودع المصدر", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!(cboToWarehouse.SelectedItem is ComboItem to) || to.ID == 0)
            { MessageBox.Show("اختر مستودع الهدف", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (from.ID == to.ID)
            { MessageBox.Show("لا يمكن التحويل بين نفس المستودع!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (_items.Count == 0)
            { MessageBox.Show("لا يوجد أصناف للتحويل، أضف أصنافاً أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string msg = $"هل تريد تأكيد التحويل المخزني؟\n\nمن: {from.Text}\nإلى: {to.Text}\nعدد الأصناف: {_items.Count}";
            if (MessageBox.Show(msg, "تأكيد التحويل المخزني", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                int transferID = TransferDAL.SaveTransfer(from.ID, to.ID, txtNotes.Text, _items);
                if (transferID > 0)
                {
                    MessageBox.Show($"✅ تم حفظ التحويل المخزني بنجاح!\nرقم التحويل: TRF-{transferID}", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            cboProduct.SelectedIndex = 0;
            nudQty.Value = 1;
            txtNotes.Clear();
            txtBarcodeTransfer.Clear();
            lblAvailableStock.Text = "متاح: --";

            var lbl = this.Controls.Find("lblItemCount", true);
            if (lbl.Length > 0) ((Label)lbl[0]).Text = "الأصناف: 0";
        }
    }
}

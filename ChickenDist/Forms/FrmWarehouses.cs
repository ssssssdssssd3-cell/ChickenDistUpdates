using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// إدارة المخازن + عرض كميات المخزن المُختار بلوحة منسدلة فورية
    /// </summary>
    public class FrmWarehouses : Form
    {
        // ─── بيانات المخازن ───
        private DataGridView dgWarehouses;
        private TextBox txtWarehouseSearch;

        // ─── نموذج التعديل ───
        private TextBox txtName, txtLocation, txtNotes;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete;
        private Label lblFormTitle;
        private int _selectedID = 0;

        // ─── لوحة كميات المخزن ───
        private Panel pnlStock;
        private Label lblStockHeader;
        private TextBox txtProductSearch;
        private CheckBox chkQtyOnly;
        private DataGridView dgStock;
        private Label lblTotals;
        private Button btnRefreshStock;
        private int _currentWarehouseID = 0;
        private string _currentWarehouseName = "";

        public FrmWarehouses()
        {
            InitUI();
            LoadWarehouses();
            ClearDetail();
        }

        // ══════════════════════════════════════════════════════
        //  بناء الواجهة
        // ══════════════════════════════════════════════════════
        private void InitUI()
        {
            Text = "إدارة المخازن وعرض الكميات";
            Size = new Size(1150, 680);
            MinimumSize = new Size(900, 500);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // ══════ الهيكل الرئيسي: عمودان ══════
            var tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6)
            };
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // محتوى المخازن + الكميات
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f)); // نموذج التعديل

            // ══════ العمود الأيسر: قائمة المخازن + لوحة الكميات ══════
            var pnlLeft = new Panel { Dock = DockStyle.Fill };

            // ─── شريط فلتر المخازن ───
            var pnlWFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 8, 8, 4)
            };
            var lblWSearch = new Label { Text = "بحث مخزن:", AutoSize = true, ForeColor = Theme.TextSub, Location = new Point(195, 12) };
            txtWarehouseSearch = new TextBox
            {
                Location = new Point(10, 8), Width = 180,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtWarehouseSearch.TextChanged += (s, e) => LoadWarehouses();
            var lblWTitle = new Label
            {
                Text = "🏭 قائمة المخازن",
                Location = new Point(340, 12), AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.Primary
            };
            pnlWFilter.Controls.AddRange(new Control[] { lblWTitle, lblWSearch, txtWarehouseSearch });

            // ─── جريد المخازن (ارتفاع ثابت 200px) ───
            var pnlWGrid = new Panel { Dock = DockStyle.Top, Height = 200, BackColor = Theme.BgMain };
            dgWarehouses = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard, ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White,
                    Font = Theme.FontMain, Padding = new Padding(2)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary, ForeColor = Color.White, Font = Theme.FontBold
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 28 }
            };
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseID",   Visible = false });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "اسم المخزن",  FillWeight = 100 });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location",      HeaderText = "الموقع",      FillWeight = 100 });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive",      HeaderText = "حالة النشاط", FillWeight = 40  });
            dgWarehouses.SelectionChanged += DgWarehouses_SelectionChanged;
            pnlWGrid.Controls.Add(dgWarehouses);

            // ─── لوحة الكميات (مخفية حتى يُختار مخزن) ───
            pnlStock = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgMain, Visible = false };

            // رأس لوحة الكميات
            var pnlStockHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 50,
                BackColor = Theme.BgCard, Padding = new Padding(8)
            };
            lblStockHeader = new Label
            {
                Text = "📦 كميات المخزن",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Location = new Point(560, 12), AutoSize = true
            };
            var lblProdSearch = new Label
            {
                Text = "بحث صنف:",
                AutoSize = true, ForeColor = Theme.TextSub, Location = new Point(195, 16)
            };
            txtProductSearch = new TextBox
            {
                Location = new Point(10, 12), Width = 180,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtProductSearch.TextChanged += (s, e) => LoadStock();

            btnRefreshStock = new Button
            {
                Text = "🔄 تحديث",
                Location = new Point(220, 12), Size = new Size(80, 26),
                BackColor = Theme.Accent, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = Theme.FontBold, Cursor = Cursors.Hand
            };
            btnRefreshStock.FlatAppearance.BorderSize = 0;
            btnRefreshStock.Click += (s, e) => LoadStock();

            // زر "إظهار الأصناف ناقصة المخزون فقط"
            var btnLowStock = new Button
            {
                Text = "⚠ عجز فقط",
                Location = new Point(308, 12), Size = new Size(85, 26),
                BackColor = Color.FromArgb(150, 40, 40), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = Theme.FontBold, Cursor = Cursors.Hand
            };
            btnLowStock.FlatAppearance.BorderSize = 0;
            btnLowStock.Click += (s, e) => LoadStock(lowStockOnly: true);

            chkQtyOnly = new CheckBox
            {
                Text = "كميات متوفرة فقط",
                Location = new Point(400, 12), Size = new Size(150, 26),
                ForeColor = Theme.TextMain,
                Checked = true,
                Font = Theme.FontMain
            };
            chkQtyOnly.CheckedChanged += (s, e) => LoadStock();

            pnlStockHeader.Controls.AddRange(new Control[] { lblStockHeader, lblProdSearch, txtProductSearch, btnRefreshStock, btnLowStock, chkQtyOnly });

            // جريد الكميات
            dgStock = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard, ForeColor = Theme.TextMain,
                    SelectionBackColor = Color.FromArgb(50, 80, 150),
                    SelectionForeColor = Color.White, Font = Theme.FontMain, Padding = new Padding(2)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 70, 140), ForeColor = Color.White, Font = Theme.FontBold
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 26 }
            };
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الكود",        FillWeight = 45 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartNumber",  HeaderText = "رقم القطعة",  FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف",   FillWeight = 130 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",        HeaderText = "الوحدة",      FillWeight = 35 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty",     HeaderText = "الكمية",      FillWeight = 45 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinStock",    HeaderText = "الحد الأدنى", FillWeight = 45 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice",   HeaderText = "سعر البيع",   FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalValue",  HeaderText = "القيمة",      FillWeight = 65 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLoc",    HeaderText = "مكان الرف",  FillWeight = 50 });
            dgStock.CellFormatting += DgStock_CellFormatting;

            // شريط الإجماليات
            lblTotals = new Label
            {
                Dock = DockStyle.Bottom, Height = 32,
                BackColor = Color.FromArgb(20, 40, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 10, 0)
            };

            pnlStock.Controls.Add(pnlStockHeader);
            pnlStock.Controls.Add(lblTotals);
            pnlStock.Controls.Add(dgStock);

            pnlLeft.Controls.Add(pnlWFilter);
            pnlLeft.Controls.Add(pnlWGrid);
            pnlLeft.Controls.Add(pnlStock);

            // ══════ العمود الأيمن: نموذج التعديل ══════
            var pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(16)
            };

            lblFormTitle = new Label
            {
                Text = "➕ مخزن جديد",
                Dock = DockStyle.Top, Height = 34,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleRight
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 6, 0, 0)
            };

            flow.Controls.Add(FieldLabel("اسم المخزن:"));
            txtName = FieldTextBox(flow, false);

            flow.Controls.Add(FieldLabel("الموقع:"));
            txtLocation = FieldTextBox(flow, false);

            flow.Controls.Add(FieldLabel("ملاحظات:"));
            txtNotes = FieldTextBox(flow, true);

            chkActive = new CheckBox
            {
                Text = "✔ مخزن نشط",
                AutoSize = false, Size = new Size(260, 30),
                Margin = new Padding(0, 8, 0, 10),
                ForeColor = Theme.TextMain, Checked = true, Font = Theme.FontMain
            };
            flow.Controls.Add(chkActive);

            // ─── أزرار الإجراءات ───
            var pnlBtns = new FlowLayoutPanel
            {
                AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 4, 0, 0), Width = 270
            };
            btnNew    = ActionBtn("🆕 جديد",  Color.FromArgb(50, 110, 50));
            btnSave   = ActionBtn("💾 حفظ",   Theme.Accent);
            btnDelete = ActionBtn("🗑️ إيقاف", Color.FromArgb(155, 40, 40));
            btnNew.Click    += (s, e) => ClearDetail();
            btnSave.Click   += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            pnlBtns.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });
            flow.Controls.Add(pnlBtns);

            pnlForm.Controls.Add(flow);
            pnlForm.Controls.Add(lblFormTitle);

            tblMain.Controls.Add(pnlLeft, 0, 0);
            tblMain.Controls.Add(pnlForm, 1, 0);

            this.Controls.Add(tblMain);
        }

        // ══════════════════════════════════════════════════════
        //  تحميل المخازن
        // ══════════════════════════════════════════════════════
        private void LoadWarehouses()
        {
            dgWarehouses.Rows.Clear();
            string search = txtWarehouseSearch?.Text?.Trim() ?? "";
            DataTable dt = WarehouseDAL.GetAll();
            foreach (DataRow r in dt.Rows)
            {
                string name = r["WarehouseName"].ToString();
                if (!string.IsNullOrEmpty(search) &&
                    !name.Contains(search) &&
                    !r["Location"].ToString().Contains(search)) continue;

                bool active = Convert.ToBoolean(r["IsActive"]);
                int ri = dgWarehouses.Rows.Add(
                    r["WarehouseID"],
                    name,
                    r["Location"],
                    active ? "✓ نشط" : "✗ متوقف");

                if (!active)
                    dgWarehouses.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
            }
        }

        // ══════════════════════════════════════════════════════
        //  عند اختيار مخزن → تحميل بياناته + كمياته
        // ══════════════════════════════════════════════════════
        private void DgWarehouses_SelectionChanged(object sender, EventArgs e)
        {
            if (dgWarehouses.SelectedRows.Count == 0) return;

            var selRow = dgWarehouses.SelectedRows[0];
            if (selRow.Cells["WarehouseID"].Value == null) return;

            _selectedID = Convert.ToInt32(selRow.Cells["WarehouseID"].Value);

            // تحميل نموذج التعديل
            DataRow r = WarehouseDAL.GetByID(_selectedID);
            if (r != null)
            {
                txtName.Text     = r["WarehouseName"].ToString();
                txtLocation.Text = r["Location"].ToString();
                txtNotes.Text    = r["Notes"].ToString();
                chkActive.Checked = Convert.ToBoolean(r["IsActive"]);
                lblFormTitle.Text = $"✏ تعديل: {txtName.Text}";
                btnDelete.Enabled = (_selectedID != 1);
            }

            // تحميل لوحة الكميات
            _currentWarehouseID   = _selectedID;
            _currentWarehouseName = selRow.Cells["WarehouseName"].Value?.ToString() ?? "";
            lblStockHeader.Text   = $"📦 كميات المخزن: {_currentWarehouseName}";
            txtProductSearch.Clear();
            pnlStock.Visible = true;
            LoadStock();
        }

        // ══════════════════════════════════════════════════════
        //  تحميل كميات المخزن المُختار
        // ══════════════════════════════════════════════════════
        private void LoadStock(bool lowStockOnly = false)
        {
            if (_currentWarehouseID <= 0) return;

            try
            {
                string search = txtProductSearch?.Text?.Trim() ?? "";
                var dt = InventoryDAL.GetStock(_currentWarehouseID, search);

                dgStock.Rows.Clear();
                decimal totalValue    = 0m;
                int     totalProducts = 0;
                int     lowStockCount = 0;

                foreach (DataRow row in dt.Rows)
                {
                    decimal qty      = Convert.ToDecimal(row["BookQty"]);
                    decimal price    = row["SalePrice"] != DBNull.Value ? Convert.ToDecimal(row["SalePrice"]) : 0m;
                    decimal minStock = row["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(row["MinStockLimit"]) : 0m;
                    decimal val      = qty * price;
                    bool    isLow    = minStock > 0 && qty < minStock;

                    if (lowStockOnly && !isLow) continue;
                    
                    bool qtyOnly = chkQtyOnly != null && chkQtyOnly.Checked;
                    if (qtyOnly && qty <= 0m) continue;

                    int ri = dgStock.Rows.Add(
                        row["ProductCode"],
                        row["PartNumber"],
                        row["ProductName"],
                        row["Unit"],
                        qty.ToString("N2"),
                        minStock > 0 ? minStock.ToString("N2") : "—",
                        price.ToString("N2") + " ج",
                        val.ToString("N2") + " ج",
                        row["ShelfLocation"]);

                    // تلوين الأصناف ناقصة المخزون بالأحمر
                    if (isLow)
                    {
                        dgStock.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(70, 20, 20);
                        dgStock.Rows[ri].DefaultCellStyle.ForeColor = Color.Tomato;
                        lowStockCount++;
                    }

                    totalValue += val;
                    totalProducts++;
                }

                // شريط الإجماليات
                lblTotals.Text =
                    $"  إجمالي الأصناف: {totalProducts}  |" +
                    $"  إجمالي القيمة (بسعر البيع): {totalValue:N2} ج  |" +
                    $"  🔴 أصناف بها عجز: {lowStockCount}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الكميات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // تلوين خلية الكمية باللون الأحمر إذا كانت صفر
        private void DgStock_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgStock.Columns[e.ColumnIndex].Name != "BookQty") return;
            if (e.Value == null) return;

            string val = e.Value.ToString().Replace(" ج", "").Trim();
            if (decimal.TryParse(val, out decimal qty) && qty <= 0)
            {
                e.CellStyle.ForeColor = Color.Tomato;
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
        }

        // ══════════════════════════════════════════════════════
        //  منطق نموذج التعديل
        // ══════════════════════════════════════════════════════
        private void ClearDetail()
        {
            _selectedID = 0;
            txtName.Clear(); txtLocation.Clear(); txtNotes.Clear();
            chkActive.Checked = true;
            btnDelete.Enabled = false;
            lblFormTitle.Text = "➕ مخزن جديد";
            txtName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم المخزن!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int id = WarehouseDAL.Save(_selectedID, txtName.Text.Trim(), txtLocation.Text.Trim(), txtNotes.Text.Trim(), chkActive.Checked);
            if (id > 0)
            {
                MessageBox.Show("✅ تم حفظ بيانات المخزن بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _selectedID = id;
                LoadWarehouses();
                ClearDetail();
                pnlStock.Visible = false;
            }
            else
                MessageBox.Show("❌ فشل حفظ المخزن.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID <= 1) return;
            if (MessageBox.Show("هل تريد إيقاف هذا المخزن؟\nلن تتمكن من استخدامه في العمليات الجديدة.",
                    "تأكيد الإيقاف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                WarehouseDAL.Delete(_selectedID);
                LoadWarehouses();
                ClearDetail();
                pnlStock.Visible = false;
            }
        }

        // ══════════════════════════════════════════════════════
        //  دوال مساعدة للبناء
        // ══════════════════════════════════════════════════════
        private Label FieldLabel(string text) =>
            new Label
            {
                Text = text, AutoSize = false, Size = new Size(260, 22),
                Margin = new Padding(0, 6, 0, 2),
                ForeColor = Theme.TextSub, Font = Theme.FontMain,
                TextAlign = ContentAlignment.MiddleRight
            };

        private TextBox FieldTextBox(FlowLayoutPanel parent, bool multiline)
        {
            var txt = new TextBox
            {
                Width = 260, Multiline = multiline,
                Height = multiline ? 65 : 26,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle, RightToLeft = RightToLeft.Yes
            };
            parent.Controls.Add(txt);
            return txt;
        }

        private Button ActionBtn(string text, Color back)
        {
            var btn = new Button
            {
                Text = text, Size = new Size(82, 32),
                Margin = new Padding(4, 0, 0, 0),
                BackColor = back, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = Theme.FontBold, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}

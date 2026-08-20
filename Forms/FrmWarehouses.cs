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
            Size = new Size(1250, 750);
            MinimumSize = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // ══════ الهيكل الرئيسي: عمودان (الأيسر: البيانات، الأيمن: التعديل) ══════
            var tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8)
            };
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310f));

            // ══════ العمود الأيسر: قائمة المخازن + لوحة الكميات ══════
            var tblLeft = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            tblLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));   // شريط البحث والعنوان
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f));  // جدول المخازن
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // لوحة كميات المخزن

            // ─── صف 0: شريط فلتر المخازن ───
            var pnlWFilter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8)
            };

            var lblWTitle = new Label
            {
                Text = "🏭 قائمة المخازن",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleRight
            };

            var flowWSearch = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            var lblWSearch = new Label 
            { 
                Text = "بحث مخزن:", 
                AutoSize = true, 
                ForeColor = Theme.TextSub, 
                Margin = new Padding(4, 6, 4, 0),
                Font = Theme.FontMain
            };
            
            txtWarehouseSearch = new TextBox
            {
                Width = 200,
                Margin = new Padding(0, 2, 0, 0),
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontMain
            };
            txtWarehouseSearch.TextChanged += (s, e) => LoadWarehouses();

            flowWSearch.Controls.Add(lblWSearch);
            flowWSearch.Controls.Add(txtWarehouseSearch);

            pnlWFilter.Controls.Add(lblWTitle);
            pnlWFilter.Controls.Add(flowWSearch);

            // ─── صف 1: جدول المخازن ───
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
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseID", Visible = false });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "اسم المخزن", FillWeight = 100 });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "الموقع", FillWeight = 100 });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "حالة النشاط", FillWeight = 40 });
            dgWarehouses.SelectionChanged += DgWarehouses_SelectionChanged;

            // ─── صف 2: لوحة الكميات (تظهر عند اختيار مخزن) ───
            pnlStock = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgMain, Visible = false };

            var tblStock = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            tblStock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblStock.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));   // رأس وتنقيب
            tblStock.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // الجدول
            tblStock.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));   // الشريط السفلي

            // رأس لوحة الكميات (منظم بدون أي أبعاد مطلقة متداخلة)
            var pnlStockHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8)
            };

            lblStockHeader = new Label
            {
                Text = "📦 كميات المخزن",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleRight
            };

            var flowStockTools = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            var lblProdSearch = new Label
            {
                Text = "بحث صنف:",
                AutoSize = true, 
                ForeColor = Theme.TextSub, 
                Margin = new Padding(4, 6, 4, 0),
                Font = Theme.FontMain
            };

            txtProductSearch = new TextBox
            {
                Width = 160,
                Margin = new Padding(0, 2, 8, 0),
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontMain
            };
            txtProductSearch.TextChanged += (s, e) => LoadStock();

            btnRefreshStock = new Button
            {
                Text = "🔄 تحديث",
                Size = new Size(80, 28),
                Margin = new Padding(0, 0, 6, 0),
                BackColor = Theme.Accent, 
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, 
                Font = Theme.FontBold, 
                Cursor = Cursors.Hand
            };
            btnRefreshStock.FlatAppearance.BorderSize = 0;
            btnRefreshStock.Click += (s, e) => LoadStock();

            var btnLowStock = new Button
            {
                Text = "⚠ عجز فقط",
                Size = new Size(92, 28),
                Margin = new Padding(0, 0, 6, 0),
                BackColor = Color.FromArgb(170, 45, 45), 
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, 
                Font = Theme.FontBold, 
                Cursor = Cursors.Hand
            };
            btnLowStock.FlatAppearance.BorderSize = 0;
            btnLowStock.Click += (s, e) => LoadStock(lowStockOnly: true);

            chkQtyOnly = new CheckBox
            {
                Text = "كميات متوفرة فقط",
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0),
                ForeColor = Theme.TextMain,
                Checked = true,
                Font = Theme.FontMain
            };
            chkQtyOnly.CheckedChanged += (s, e) => LoadStock();

            flowStockTools.Controls.Add(lblProdSearch);
            flowStockTools.Controls.Add(txtProductSearch);
            flowStockTools.Controls.Add(btnRefreshStock);
            flowStockTools.Controls.Add(btnLowStock);
            flowStockTools.Controls.Add(chkQtyOnly);

            pnlStockHeader.Controls.Add(lblStockHeader);
            pnlStockHeader.Controls.Add(flowStockTools);

            // جدول الكميات
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
                RowTemplate = { Height = 28 },
                ScrollBars = ScrollBars.Both
            };
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الكود", FillWeight = 45 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartNumber", HeaderText = "رقم القطعة", FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 130 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 35 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty", HeaderText = "الكمية", FillWeight = 45 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinStock", HeaderText = "الحد الأدنى", FillWeight = 45 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", HeaderText = "سعر التكلفة", FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCostValue", HeaderText = "قيمة التكلفة", FillWeight = 65 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalSaleValue", HeaderText = "قيمة البيع", FillWeight = 65 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLoc", HeaderText = "مكان الرف", FillWeight = 50 });
            dgStock.CellFormatting += DgStock_CellFormatting;

            // شريط الإجماليات
            lblTotals = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 40, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 12, 0)
            };

            // تجميع لوحة الكميات
            tblStock.Controls.Add(pnlStockHeader, 0, 0);
            tblStock.Controls.Add(dgStock, 0, 1);
            tblStock.Controls.Add(lblTotals, 0, 2);
            pnlStock.Controls.Add(tblStock);

            // تجميع العمود الأيسر
            tblLeft.Controls.Add(pnlWFilter, 0, 0);
            tblLeft.Controls.Add(dgWarehouses, 0, 1);
            tblLeft.Controls.Add(pnlStock, 0, 2);

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
                Dock = DockStyle.Top, 
                Height = 36,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleRight
            };

            var flowForm = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 8, 0, 0)
            };

            flowForm.Controls.Add(FieldLabel("اسم المخزن:"));
            txtName = FieldTextBox(flowForm, false);

            flowForm.Controls.Add(FieldLabel("الموقع:"));
            txtLocation = FieldTextBox(flowForm, false);

            flowForm.Controls.Add(FieldLabel("ملاحظات:"));
            txtNotes = FieldTextBox(flowForm, true);

            chkActive = new CheckBox
            {
                Text = "✔ مخزن نشط",
                AutoSize = false, 
                Size = new Size(270, 30),
                Margin = new Padding(0, 10, 0, 14),
                ForeColor = Theme.TextMain, 
                Checked = true, 
                Font = Theme.FontMain
            };
            flowForm.Controls.Add(chkActive);

            // ─── أزرار الإجراءات ───
            var pnlBtns = new FlowLayoutPanel
            {
                AutoSize = true, 
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 6, 0, 0), 
                Width = 275
            };
            btnNew    = ActionBtn("🆕 جديد",  Color.FromArgb(40, 120, 60));
            btnSave   = ActionBtn("💾 حفظ",   Theme.Accent);
            btnDelete = ActionBtn("⛔ إيقاف", Color.FromArgb(170, 45, 45));
            btnNew.Click    += (s, e) => ClearDetail();
            btnSave.Click   += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            pnlBtns.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });
            flowForm.Controls.Add(pnlBtns);

            pnlForm.Controls.Add(flowForm);
            pnlForm.Controls.Add(lblFormTitle);

            tblMain.Controls.Add(tblLeft, 0, 0);
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
                dgStock.SuspendLayout();
                dgStock.Rows.Clear();

                string search = txtProductSearch?.Text?.Trim() ?? "";
                var dt = InventoryDAL.GetStock(_currentWarehouseID, search, maxRows: 1000);

                decimal totalSaleValue = 0m;
                decimal totalCostValue = 0m;
                int     totalProducts = 0;
                int     lowStockCount = 0;

                foreach (DataRow row in dt.Rows)
                {
                    decimal qty      = Convert.ToDecimal(row["BookQty"]);
                    decimal salePrice = row["SalePrice"] != DBNull.Value ? Convert.ToDecimal(row["SalePrice"]) : 0m;
                    decimal costPrice = row.Table.Columns.Contains("PurchasePrice") && row["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["PurchasePrice"]) : 0m;
                    decimal minStock = row["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(row["MinStockLimit"]) : 0m;
                    decimal saleVal  = qty * salePrice;
                    decimal costVal  = qty * costPrice;
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
                        costPrice.ToString("N2") + " ج",
                        salePrice.ToString("N2") + " ج",
                        costVal.ToString("N2") + " ج",
                        saleVal.ToString("N2") + " ج",
                        row["ShelfLocation"]);

                    // تلوين الأصناف ناقصة المخزون بالأحمر
                    if (isLow)
                    {
                        dgStock.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(70, 20, 20);
                        dgStock.Rows[ri].DefaultCellStyle.ForeColor = Color.Tomato;
                        lowStockCount++;
                    }

                    totalSaleValue += saleVal;
                    totalCostValue += costVal;
                    totalProducts++;
                }

                // شريط الإجماليات
                lblTotals.Text =
                    $"  إجمالي الأصناف: {totalProducts}  |" +
                    $"  التكلفة: {totalCostValue:N2} ج  |" +
                    $"  البيع: {totalSaleValue:N2} ج  |" +
                    $"  🔴 عجز: {lowStockCount}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الكميات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                dgStock.ResumeLayout();
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
            if (_selectedID == 0 && !Session.CanAdd("Warehouses")) { MessageBox.Show("⛔ ليس لديك صلاحية إضافة مخازن.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_selectedID > 0 && !Session.CanEdit("Warehouses")) { MessageBox.Show("⛔ ليس لديك صلاحية تعديل المخازن.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

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
            if (!Session.CanDelete("Warehouses")) { MessageBox.Show("⛔ ليس لديك صلاحية حذف المخازن.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
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

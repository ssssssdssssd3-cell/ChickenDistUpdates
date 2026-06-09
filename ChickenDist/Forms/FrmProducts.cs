using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة إدارة الأصناف المتوافقة مع قطع الغيار</summary>
    public class FrmProducts : Form
    {
        private DataGridView dgProducts;
        private TextBox txtSearch;
        private TextBox txtCode, txtName, txtUnit, txtDescription, txtPartNumber, txtCarModel, txtBrand, txtShelfLocation;
        private ComboBox cboCategory;
        private NumericUpDown nudPrice, nudPurchasePrice, nudMinStockLimit, nudWholesalePrice, nudSemiWholesalePrice;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete, btnCopy, btnQuickAdd;
        private int _selectedID = 0;

        public FrmProducts()
        {
            InitUI();
            LoadCategoriesCombo();
            LoadProducts();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = "إدارة الأصناف";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, FixedPanel = FixedPanel.Panel1 };

            // Left: Grid (Panel2 in RTL)
            dgProducts = new DataGridView
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
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 40 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartNumber", HeaderText = "رقم القطعة", FillWeight = 60 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 110 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 50 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 30 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 40 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "نشط", FillWeight = 25 });
            dgProducts.SelectionChanged += DgProducts_SelectionChanged;

            // لوحة البحث والاستيراد العلوية
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Theme.BgCard, Padding = new Padding(6) };
            
            // حقل البحث
            txtSearch = new TextBox { Dock = DockStyle.Right, Width = 250, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "بحث بالاسم أو الكود...", Font = Theme.FontMain, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "بحث بالاسم أو الكود...") txtSearch.Text = ""; };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "بحث بالاسم أو الكود..."; };
            txtSearch.TextChanged += (s, e) => {
                string searchVal = txtSearch.Text;
                if (searchVal == "بحث بالاسم أو الكود...") searchVal = "";
                LoadProducts(searchVal);
            };

            // زر البحث بالعدسة
            var btnSearch = Theme.MakeButton("🔍", Theme.Primary);
            btnSearch.Dock = DockStyle.Right;
            btnSearch.Width = 45;
            btnSearch.Click += (s, e) => {
                string searchVal = txtSearch.Text == "بحث بالاسم أو الكود..." ? "" : txtSearch.Text;
                LoadProducts(searchVal);
            };
            txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    string searchVal = txtSearch.Text == "بحث بالاسم أو الكود..." ? "" : txtSearch.Text;
                    LoadProducts(searchVal);
                }
            };

            // زر استيراد أصناف من Excel
            var btnImport = Theme.MakeButton("📥 استيراد أصناف من Excel", Color.FromArgb(40, 110, 180));
            btnImport.Dock = DockStyle.Left;
            btnImport.Width = 180;
            btnImport.Click += (s, e) => {
                using (var frm = new FrmImportProducts())
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        LoadProducts();
                        ClearDetail();
                    }
                }
            };

            // زر الإدخال السريع
            btnQuickAdd = Theme.MakeButton("⚡ إدخال سريع للأصناف", Color.FromArgb(230, 126, 34));
            btnQuickAdd.Dock = DockStyle.Left;
            btnQuickAdd.Width = 180;
            btnQuickAdd.Click += (s, e) => {
                using (var frm = new FrmQuickAdd())
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        LoadProducts();
                        ClearDetail();
                    }
                }
            };

            pnlSearch.Controls.Add(btnSearch);
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(btnImport);
            pnlSearch.Controls.Add(btnQuickAdd);

            split.Panel2.Controls.Add(dgProducts);
            split.Panel2.Controls.Add(pnlSearch);
            pnlSearch.BringToFront();

            // Right: Detail (Panel1 in RTL)
            split.Panel1.BackColor = Theme.BgCard;
            split.Panel1.Padding = new Padding(15);
            split.Panel1.AutoScroll = true;

            int y = 20;
            AddField(split.Panel1, "كود الصنف:", ref y, out txtCode);
            txtCode.ReadOnly = false;
            AddField(split.Panel1, "اسم الصنف:", ref y, out txtName);
            AddField(split.Panel1, "رقم القطعة (OEM):", ref y, out txtPartNumber);

            // ComboBox للتصنيف
            split.Panel1.Controls.Add(new Label { Text = "التصنيف:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            cboCategory = new ComboBox { Location = new Point(15, y - 2), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            split.Panel1.Controls.Add(cboCategory);
            y += 38;

            AddField(split.Panel1, "الموديل المتوافق:", ref y, out txtCarModel);
            AddField(split.Panel1, "الماركة:", ref y, out txtBrand);
            AddField(split.Panel1, "موقع الرف:", ref y, out txtShelfLocation);
            AddField(split.Panel1, "الوحدة:", ref y, out txtUnit);

            split.Panel1.Controls.Add(new Label { Text = "سعر الشراء:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            nudPurchasePrice = new NumericUpDown { Location = new Point(15, y - 2), Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(nudPurchasePrice); y += 40;

            split.Panel1.Controls.Add(new Label { Text = "سعر قطاعي:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            nudPrice = new NumericUpDown { Location = new Point(15, y - 2), Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(nudPrice); y += 40;

            split.Panel1.Controls.Add(new Label { Text = "سعر نصف الجملة:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            nudSemiWholesalePrice = new NumericUpDown { Location = new Point(15, y - 2), Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(nudSemiWholesalePrice); y += 40;

            split.Panel1.Controls.Add(new Label { Text = "سعر الجملة:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            nudWholesalePrice = new NumericUpDown { Location = new Point(15, y - 2), Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(nudWholesalePrice); y += 40;

            split.Panel1.Controls.Add(new Label { Text = "حد الطلب:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            nudMinStockLimit = new NumericUpDown { Location = new Point(15, y - 2), Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 3, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(nudMinStockLimit); y += 40;

            AddField(split.Panel1, "الوصف:", ref y, out txtDescription);

            chkActive = new CheckBox { Text = "صنف نشط", Location = new Point(230, y), ForeColor = Theme.TextMain, Checked = true }; y += 40;
            split.Panel1.Controls.Add(chkActive);

            btnNew = Theme.MakeButton("🆕 جديد", 240, y, 90, 32, Color.FromArgb(60, 100, 60));
            btnSave = Theme.MakeButton("💾 حفظ", 140, y, 90, 32, Theme.Accent);
            btnDelete = Theme.MakeButton("🗑 إيقاف", 40, y, 90, 32, Color.FromArgb(140, 40, 40));
            
            btnCopy = Theme.MakeButton("📋 نسخ صنف موجود", 40, y + 40, 290, 32, Color.FromArgb(100, 80, 140));
            btnCopy.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnCopy.Click += BtnCopy_Click;

            split.Panel1.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete, btnCopy });
            this.Controls.Add(split);
            split.SplitterDistance = 350;

            // اختصارات لوحة المفاتيح
            this.KeyPreview = true;
            this.KeyDown += FrmProducts_KeyDown;

            Theme.ApplyFormRTL(this);
        }

        private void AddField(Control parent, string label, ref int y, out TextBox txt)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            txt = new TextBox { Location = new Point(15, y - 2), Width = 180, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(txt);
            y += 38;
        }

        private void LoadCategoriesCombo()
        {
            cboCategory.Items.Clear();
            cboCategory.Items.Add(new ComboItem(0, "-- بدون تصنيف --"));
            DataTable dt = CategoryDAL.GetAll(true);
            foreach (DataRow r in dt.Rows)
            {
                cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
            }
            cboCategory.DisplayMember = "Text";
            cboCategory.SelectedIndex = 0;
        }

        private void LoadProducts(string filterText = "")
        {
            dgProducts.Rows.Clear();
            var dt = ProductDAL.GetAll();
            foreach (DataRow r in dt.Rows)
            {
                if (!string.IsNullOrWhiteSpace(filterText))
                {
                    string code = r["ProductCode"].ToString();
                    string part = r["PartNumber"] != DBNull.Value ? r["PartNumber"].ToString() : "";
                    string name = r["ProductName"].ToString();
                    string category = r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "";

                    bool matches = code.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   part.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   category.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!matches) continue;
                }

                bool active = Convert.ToBoolean(r["IsActive"]);
                var ri = dgProducts.Rows.Add(r["ProductID"], r["ProductCode"], r["PartNumber"], r["ProductName"],
                    r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "---",
                    r["Unit"], Convert.ToDecimal(r["SalePrice"]).ToString("N2"), active ? "✓" : "✗");
                if (!active) dgProducts.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
            }
        }

        private void DgProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgProducts.SelectedRows.Count == 0) return;
            _selectedID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
            var dr = ProductDAL.GetByID(_selectedID);
            if (dr == null) return;
            txtCode.Text = dr["ProductCode"].ToString();
            txtName.Text = dr["ProductName"].ToString();
            txtPartNumber.Text = dr["PartNumber"] != DBNull.Value ? dr["PartNumber"].ToString() : "";
            txtCarModel.Text = dr["CarModel"] != DBNull.Value ? dr["CarModel"].ToString() : "";
            txtBrand.Text = dr["Brand"] != DBNull.Value ? dr["Brand"].ToString() : "";
            txtShelfLocation.Text = dr["ShelfLocation"] != DBNull.Value ? dr["ShelfLocation"].ToString() : "";
            txtUnit.Text = dr["Unit"].ToString();
            nudPurchasePrice.Value = Convert.ToDecimal(dr["PurchasePrice"] == DBNull.Value ? 0 : dr["PurchasePrice"]);
            nudPrice.Value = Convert.ToDecimal(dr["SalePrice"]);
            nudWholesalePrice.Value = Convert.ToDecimal(dr.Table.Columns.Contains("WholesalePrice") && dr["WholesalePrice"] != DBNull.Value ? dr["WholesalePrice"] : 0);
            nudSemiWholesalePrice.Value = Convert.ToDecimal(dr.Table.Columns.Contains("SemiWholesalePrice") && dr["SemiWholesalePrice"] != DBNull.Value ? dr["SemiWholesalePrice"] : 0);
            nudMinStockLimit.Value = Convert.ToDecimal(dr["MinStockLimit"] == DBNull.Value ? 0 : dr["MinStockLimit"]);
            txtDescription.Text = dr["Description"].ToString();
            chkActive.Checked = Convert.ToBoolean(dr["IsActive"]);

            // تحديد التصنيف في الـ ComboBox
            if (dr["CategoryID"] != DBNull.Value)
            {
                int catID = Convert.ToInt32(dr["CategoryID"]);
                for (int i = 0; i < cboCategory.Items.Count; i++)
                {
                    if (cboCategory.Items[i] is ComboItem item && item.ID == catID)
                    {
                        cboCategory.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                cboCategory.SelectedIndex = 0;
            }
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtCode.Text = ProductDAL.GetNextProductCode();
            txtName.Clear(); 
            txtPartNumber.Clear();
            txtCarModel.Clear();
            txtBrand.Clear();
            txtShelfLocation.Clear();
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
            txtUnit.Text = "قطعة";
            nudPurchasePrice.Value = 0;
            nudPrice.Value = 0;
            nudWholesalePrice.Value = 0;
            nudSemiWholesalePrice.Value = 0;
            nudMinStockLimit.Value = 0;
            txtDescription.Clear();
            chkActive.Checked = true;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) 
            { 
                MessageBox.Show("أدخل اسم الصنف", "خطأ في التحقق", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
                txtName.Focus(); 
                return; 
            }
            
            // 1. التحقق من الأسعار السالبة
            if (nudPrice.Value < 0 || nudPurchasePrice.Value < 0 || nudWholesalePrice.Value < 0 || nudSemiWholesalePrice.Value < 0)
            {
                MessageBox.Show("❌ لا يمكن إدخال أسعار سالبة!", "خطأ في التحقق", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = txtName.Text.Trim();
            string code = txtCode.Text.Trim();
            string partNumber = txtPartNumber.Text.Trim();

            // 2. التحقق من تكرار كود الصنف / الباركود
            if (!string.IsNullOrEmpty(code) && ProductDAL.IsCodeExists(code, _selectedID))
            {
                MessageBox.Show($"❌ كود الصنف أو الباركود '{code}' مستخدم بالفعل لصنف آخر!", "تكرار الباركود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCode.Focus();
                txtCode.SelectAll();
                return;
            }

            // 3. التحقق من تكرار رقم القطعة
            if (!string.IsNullOrEmpty(partNumber) && ProductDAL.IsPartNumberExists(partNumber, _selectedID))
            {
                MessageBox.Show($"❌ رقم القطعة '{partNumber}' مستخدم بالفعل لصنف آخر!", "تكرار رقم القطعة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPartNumber.Focus();
                txtPartNumber.SelectAll();
                return;
            }

            // 4. التحقق من تكرار اسم الصنف
            if (ProductDAL.IsNameExists(name, _selectedID))
            {
                MessageBox.Show($"❌ اسم الصنف '{name}' مستخدم بالفعل!", "تكرار اسم الصنف", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                txtName.SelectAll();
                return;
            }

            int? categoryID = null;
            if (cboCategory.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                categoryID = ci.ID;
            }

            int id = ProductDAL.Save(_selectedID, code, name, txtUnit.Text, nudPrice.Value, chkActive.Checked,
                nudPurchasePrice.Value, nudMinStockLimit.Value, txtDescription.Text,
                partNumber, categoryID, txtCarModel.Text.Trim(), txtBrand.Text.Trim(), txtShelfLocation.Text.Trim(),
                nudWholesalePrice.Value, nudSemiWholesalePrice.Value);
            
            if (id > 0) 
            { 
                MessageBox.Show("✅ تم الحفظ", "حفظ الصنف", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                _selectedID = id; 
                LoadProducts(); 
                txtName.Focus(); // التركيز تلقائيًا على حقل اسم الصنف بعد الحفظ
            }
            else 
            {
                MessageBox.Show("❌ فشل الحفظ", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAndNew()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) 
            { 
                MessageBox.Show("أدخل اسم الصنف", "خطأ في التحقق", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
                txtName.Focus(); 
                return; 
            }
            
            if (nudPrice.Value < 0 || nudPurchasePrice.Value < 0 || nudWholesalePrice.Value < 0 || nudSemiWholesalePrice.Value < 0)
            {
                MessageBox.Show("❌ لا يمكن إدخال أسعار سالبة!", "خطأ في التحقق", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = txtName.Text.Trim();
            string code = txtCode.Text.Trim();
            string partNumber = txtPartNumber.Text.Trim();

            if (!string.IsNullOrEmpty(code) && ProductDAL.IsCodeExists(code, _selectedID))
            {
                MessageBox.Show($"❌ كود الصنف أو الباركود '{code}' مستخدم بالفعل لصنف آخر!", "تكرار الباركود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCode.Focus();
                txtCode.SelectAll();
                return;
            }

            if (!string.IsNullOrEmpty(partNumber) && ProductDAL.IsPartNumberExists(partNumber, _selectedID))
            {
                MessageBox.Show($"❌ رقم القطعة '{partNumber}' مستخدم بالفعل لصنف آخر!", "تكرار رقم القطعة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPartNumber.Focus();
                txtPartNumber.SelectAll();
                return;
            }

            if (ProductDAL.IsNameExists(name, _selectedID))
            {
                MessageBox.Show($"❌ اسم الصنف '{name}' مستخدم بالفعل!", "تكرار اسم الصنف", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                txtName.SelectAll();
                return;
            }

            int? categoryID = null;
            if (cboCategory.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                categoryID = ci.ID;
            }

            int id = ProductDAL.Save(_selectedID, code, name, txtUnit.Text, nudPrice.Value, chkActive.Checked,
                nudPurchasePrice.Value, nudMinStockLimit.Value, txtDescription.Text,
                partNumber, categoryID, txtCarModel.Text.Trim(), txtBrand.Text.Trim(), txtShelfLocation.Text.Trim(),
                nudWholesalePrice.Value, nudSemiWholesalePrice.Value);
            
            if (id > 0) 
            { 
                LoadProducts(); 
                ClearDetail(); 
                txtName.Focus(); // التركيز تلقائيًا على حقل اسم الصنف بعد الحفظ
            }
            else 
            {
                MessageBox.Show("❌ فشل الحفظ", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FrmProducts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                BtnSave_Click(this, EventArgs.Empty);
            }
            else if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                SaveAndNew();
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("❌ اختر صنفاً لنسخه أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _selectedID = 0; // تحويله لصنف جديد
            txtCode.Text = ProductDAL.GetNextProductCode(); // توليد كود تلقائي جديد
            txtName.Text = txtName.Text + " - نسخة";
            txtPartNumber.Clear();
            
            txtCode.Focus();
            txtCode.SelectAll();
            MessageBox.Show("📋 تم نسخ بيانات الصنف كمنتج جديد. يرجى إدخال الباركود والاسم الجديد ثم اضغط حفظ.", "نسخ صنف", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;
            if (MessageBox.Show("إيقاف الصنف؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { ProductDAL.Delete(_selectedID); LoadProducts(); ClearDetail(); }
        }
    }
}

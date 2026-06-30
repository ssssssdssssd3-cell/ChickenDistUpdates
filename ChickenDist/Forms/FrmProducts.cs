using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmProducts : Form
    {
        private DataGridView dgProducts;
        private TextBox txtSearch;
        private ComboBox cboSearchCategory;
        private ComboBox cboSearchStatus;
        private ComboBox cboSearchQuick;
        private Button btnNew, btnEdit, btnDelete;
        private int _selectedID = 0;
        private DataTable _dtProducts;
        private bool _isInitializing = true;

        public FrmProducts()
        {
            InitUI();
            LoadProducts();
        }

        private void InitUI()
        {
            this.Text = "إدارة الأصناف";
            this.Size = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Title Bar
            var titleBar = Theme.MakeTitleBar("إدارة الأصناف", "قائمة عرض وبحث الأصناف والأسعار وتعديلها عبر كارت الصنف");
            this.Controls.Add(titleBar);

            // Search Panel (Top)
            var pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 65, 
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            var flpFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(flpFilters);

            flpFilters.Controls.Add(new Label { Text = "🔍 بحث بالاسم/الباركود:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 12, 0, 0), Font = Theme.FontBold });
            txtSearch = new TextBox { Width = 220, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal, Margin = new Padding(5, 10, 0, 0) };
            txtSearch.TextChanged += (s, e) => { if (!_isInitializing) FilterProducts(); };
            flpFilters.Controls.Add(txtSearch);

            flpFilters.Controls.Add(new Label { Text = "📂 التصنيف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 12, 0, 0), Font = Theme.FontBold });
            cboSearchCategory = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontNormal, Margin = new Padding(5, 10, 0, 0) };
            cboSearchCategory.SelectedIndexChanged += (s, e) => { if (!_isInitializing) FilterProducts(); };
            flpFilters.Controls.Add(cboSearchCategory);

            flpFilters.Controls.Add(new Label { Text = "📋 الحالة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 12, 0, 0), Font = Theme.FontBold });
            cboSearchStatus = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontNormal, Margin = new Padding(5, 10, 0, 0) };
            cboSearchStatus.Items.AddRange(new object[] { "الكل", "نشط فقط", "غير نشط" });
            cboSearchStatus.SelectedIndex = 0;
            cboSearchStatus.SelectedIndexChanged += (s, e) => { if (!_isInitializing) FilterProducts(); };
            flpFilters.Controls.Add(cboSearchStatus);

            flpFilters.Controls.Add(new Label { Text = "⭐ بيع سريع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 12, 0, 0), Font = Theme.FontBold });
            cboSearchQuick = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontNormal, Margin = new Padding(5, 10, 0, 0) };
            cboSearchQuick.Items.AddRange(new object[] { "الكل", "سريع فقط", "عادي فقط" });
            cboSearchQuick.SelectedIndex = 0;
            cboSearchQuick.SelectedIndexChanged += (s, e) => { if (!_isInitializing) FilterProducts(); };
            flpFilters.Controls.Add(cboSearchQuick);

            this.Controls.Add(pnlHeader);

            // Footer Panel (Bottom FlowLayoutPanel)
            var pnlFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            btnNew = Theme.MakeButton("➕ إضافة صنف جديد", Theme.Success);
            btnNew.Width = 145;
            btnNew.Click += (s, e) => {
                if (new FrmProductCard(0).ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            };

            btnEdit = Theme.MakeButton("📝 تعديل ومعاينة", Theme.Accent);
            btnEdit.Width = 145;
            btnEdit.Click += BtnEdit_Click;

            btnDelete = Theme.MakeButton("🗑 إيقاف الصنف", Theme.Danger);
            btnDelete.Width = 120;
            btnDelete.Click += BtnDelete_Click;

            var btnQuickAdd = Theme.MakeButton("⚡ إدخال سريع", Color.FromArgb(60, 100, 60));
            btnQuickAdd.Width = 125;
            btnQuickAdd.Click += (s, e) => {
                if (new FrmQuickAdd().ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            };

            var btnImportExcel = Theme.MakeButton("📥 استيراد إكسل", Theme.Primary);
            btnImportExcel.Width = 125;
            btnImportExcel.Click += (s, e) => {
                if (new FrmImportProducts().ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            };

            var btnPrintBarcode = Theme.MakeButton("🏷️ طباعة الباركود", Theme.Primary);
            btnPrintBarcode.Width = 135;
            btnPrintBarcode.Click += BtnPrintBarcode_Click;

            var btnPricePoster = Theme.MakeButton("📢 منشور الأسعار", Color.FromArgb(120, 80, 140));
            btnPricePoster.Width = 135;
            btnPricePoster.Click += (s, e) => new FrmPricePoster().ShowDialog();

            Button btnMatrix = null;
            if (AppConfig.BusinessType == "Clothing")
            {
                btnMatrix = Theme.MakeButton("📦 مصفوفة الملابس", Theme.Primary);
                btnMatrix.Width = 145;
                btnMatrix.Click += (s, e) => {
                    if (new FrmClothingMatrix().ShowDialog() == DialogResult.OK)
                    {
                        LoadProducts();
                    }
                };
            }

            var footCtrls = new System.Collections.Generic.List<Control> { 
                btnNew, 
                btnEdit, 
                btnDelete, 
                btnQuickAdd, 
                btnImportExcel, 
                btnPrintBarcode, 
                btnPricePoster 
            };
            if (btnMatrix != null) footCtrls.Insert(1, btnMatrix);

            pnlFooter.Controls.AddRange(footCtrls.ToArray());
            this.Controls.Add(pnlFooter);

            // Grid (Center)
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
            Theme.EnableDoubleBuffer(dgProducts);
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 40 });
            bool showPartNo = (AppConfig.BusinessType == "SpareParts" || AppConfig.BusinessType == "Mobiles" || AppConfig.BusinessType == "Clothing");
            string partNoHeader = AppConfig.BusinessType == "Mobiles" ? "الرقم التسلسلي (IMEI)"
                                : AppConfig.BusinessType == "Clothing" ? "كود الموديل"
                                : "رقم القطعة";
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartNumber", HeaderText = partNoHeader, Visible = showPartNo, FillWeight = 60 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 110 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 50 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 30 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 40 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "نشط", FillWeight = 25 });
            
            dgProducts.SelectionChanged += DgProducts_SelectionChanged;
            dgProducts.CellDoubleClick += (s, e) => {
                if (dgProducts.SelectedRows.Count > 0)
                {
                    int productID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
                    if (new FrmProductCard(productID).ShowDialog() == DialogResult.OK)
                    {
                        LoadProducts();
                    }
                }
            };

            this.Controls.Add(dgProducts);

            // Send title bar to back so layout docking works correctly
            titleBar.SendToBack();
            pnlHeader.SendToBack();
            pnlFooter.SendToBack();
            dgProducts.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private void LoadProducts()
        {
            _isInitializing = true;
            _dtProducts = ProductDAL.GetAll();
            LoadSearchCategories();
            _isInitializing = false;
            FilterProducts();
        }

        private void LoadSearchCategories()
        {
            cboSearchCategory.Items.Clear();
            cboSearchCategory.Items.Add(new ComboItem(0, "الكل"));
            try
            {
                DataTable dt = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName");
                foreach (DataRow r in dt.Rows)
                {
                    cboSearchCategory.Items.Add(new ComboItem(
                        Convert.ToInt32(r["CategoryID"]), 
                        r["CategoryName"].ToString() 
                    ));
                }
            }
            catch {}
            cboSearchCategory.SelectedIndex = 0;
        }

        private void FilterProducts()
        {
            if (dgProducts == null) return;
            dgProducts.SuspendLayout();
            var oldMode = dgProducts.AutoSizeColumnsMode;
            dgProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgProducts.Rows.Clear();
            string query = txtSearch.Text?.Trim().ToLower() ?? "";
            
            int selectedCatID = 0;
            if (cboSearchCategory.SelectedItem is ComboItem ci)
                selectedCatID = ci.ID;

            string selectedStatus = cboSearchStatus.SelectedItem?.ToString() ?? "الكل";
            string selectedQuick = cboSearchQuick.SelectedItem?.ToString() ?? "الكل";

            foreach (DataRow r in _dtProducts.Rows)
            {
                string name = r["ProductName"]?.ToString() ?? "";
                string code = r["ProductCode"]?.ToString() ?? "";
                string partNum = r["PartNumber"] != DBNull.Value ? r["PartNumber"].ToString() : "";
                string barcode = r.Table.Columns.Contains("InternationalCode") && r["InternationalCode"] != DBNull.Value ? r["InternationalCode"].ToString() : "";
                string u1Barcode = r.Table.Columns.Contains("Unit1Barcode") && r["Unit1Barcode"] != DBNull.Value ? r["Unit1Barcode"].ToString() : "";
                string u2Barcode = r.Table.Columns.Contains("Unit2Barcode") && r["Unit2Barcode"] != DBNull.Value ? r["Unit2Barcode"].ToString() : "";
                
                bool matchesText = string.IsNullOrEmpty(query) || 
                    name.ToLower().Contains(query) || 
                    code.ToLower().Contains(query) || 
                    partNum.ToLower().Contains(query) || 
                    barcode.ToLower().Contains(query) ||
                    u1Barcode.ToLower().Contains(query) ||
                    u2Barcode.ToLower().Contains(query);

                bool matchesCategory = (selectedCatID == 0);
                if (!matchesCategory)
                {
                    int catID = r["CategoryID"] != DBNull.Value ? Convert.ToInt32(r["CategoryID"]) : 0;
                    matchesCategory = (catID == selectedCatID);
                }

                bool active = Convert.ToBoolean(r["IsActive"]);
                bool matchesStatus = (selectedStatus == "الكل") || 
                    (selectedStatus == "نشط فقط" && active) || 
                    (selectedStatus == "غير نشط" && !active);

                bool isQuick = r.Table.Columns.Contains("IsQuickItem") && Convert.ToBoolean(r["IsQuickItem"]);
                bool matchesQuick = (selectedQuick == "الكل") ||
                    (selectedQuick == "سريع فقط" && isQuick) ||
                    (selectedQuick == "عادي فقط" && !isQuick);

                if (matchesText && matchesCategory && matchesStatus && matchesQuick)
                {
                    var ri = dgProducts.Rows.Add(r["ProductID"], r["ProductCode"], r["PartNumber"], r["ProductName"],
                        r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "---",
                        r["Unit"], Convert.ToDecimal(r["SalePrice"]).ToString("N2"), active ? "✓" : "✗");
                    if (!active) dgProducts.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }

            dgProducts.AutoSizeColumnsMode = oldMode;
            dgProducts.ResumeLayout();
        }

        private void DgProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgProducts.SelectedRows.Count == 0)
            {
                _selectedID = 0;
                return;
            }
            _selectedID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً لتعديله.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (new FrmProductCard(_selectedID).ShowDialog() == DialogResult.OK)
            {
                LoadProducts();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً لإيقافه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("هل أنت متأكد من إيقاف هذا الصنف؟", "تأكيد الإيقاف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ProductDAL.Delete(_selectedID);
                LoadProducts();
            }
        }

        private void BtnPrintBarcode_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dr = ProductDAL.GetByID(_selectedID);
            if (dr == null) return;

            string name = dr["ProductName"]?.ToString() ?? "";
            string code = dr["ProductCode"]?.ToString() ?? "";
            string intCode = dr["InternationalCode"] != DBNull.Value ? dr["InternationalCode"].ToString() : "";
            decimal price = Convert.ToDecimal(dr["SalePrice"]);
            string shelfLocation = dr["ShelfLocation"] != DBNull.Value ? dr["ShelfLocation"].ToString() : "";

            using (var dlg = new FrmPrintProductBarcode(_selectedID, name, code, intCode, price, shelfLocation))
            {
                dlg.ShowDialog(this);
            }
        }
    }
}

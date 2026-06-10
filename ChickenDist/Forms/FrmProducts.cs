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
        private TextBox txtCode, txtName, txtUnit, txtDescription, txtPartNumber, txtCarModel, txtBrand, txtShelfLocation;
        private ComboBox cboCategory;
        private NumericUpDown nudPrice, nudPurchasePrice, nudMinStockLimit, nudWholesalePrice, nudSemiWholesalePrice;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete;
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
            split.Panel2.Controls.Add(dgProducts);

            // Right: Detail (Panel1 in RTL)
            split.Panel1.BackColor = Theme.BgCard;
            split.Panel1.Padding = new Padding(15);
            split.Panel1.AutoScroll = true;

            int y = 20;
            AddField(split.Panel1, "كود الصنف:", ref y, out txtCode);
            txtCode.ReadOnly = true;
            AddField(split.Panel1, "اسم الصنف:", ref y, out txtName);
            AddField(split.Panel1, "رقم القطعة (OEM):", ref y, out txtPartNumber);

            // ComboBox للتصنيف مع زر إضافة
            split.Panel1.Controls.Add(new Label { Text = "التصنيف:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            cboCategory = new ComboBox { Location = new Point(50, y - 2), Width = 145, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            var btnAddCat = new Button { Text = "➕", Location = new Point(15, y - 2), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnAddCat.Click += (s, e) => { new FrmCategories().ShowDialog(); LoadCategoriesCombo(); };
            split.Panel1.Controls.AddRange(new Control[] { cboCategory, btnAddCat });
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
            
            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            split.Panel1.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });
            this.Controls.Add(split);
            split.SplitterDistance = 350;

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

        private void LoadProducts()
        {
            dgProducts.Rows.Clear();
            var dt = ProductDAL.GetAll();
            foreach (DataRow r in dt.Rows)
            {
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
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم الصنف"); return; }
            
            int? categoryID = null;
            if (cboCategory.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                categoryID = ci.ID;
            }

            int id = ProductDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtUnit.Text, nudPrice.Value, chkActive.Checked,
                nudPurchasePrice.Value, nudMinStockLimit.Value, txtDescription.Text,
                txtPartNumber.Text.Trim(), categoryID, txtCarModel.Text.Trim(), txtBrand.Text.Trim(), txtShelfLocation.Text.Trim(),
                nudWholesalePrice.Value, nudSemiWholesalePrice.Value);
            
            if (id > 0) { MessageBox.Show("✅ تم الحفظ"); _selectedID = id; LoadProducts(); }
            else MessageBox.Show("❌ فشل الحفظ");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;
            if (MessageBox.Show("إيقاف الصنف؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { ProductDAL.Delete(_selectedID); LoadProducts(); ClearDetail(); }
        }
    }
}

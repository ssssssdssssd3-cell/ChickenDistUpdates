using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmProductCard : Form
    {
        private TextBox txtCode, txtName, txtUnit, txtDescription, txtPartNumber, txtCarModel, txtBrand, txtShelfLocation, txtInternationalCode;
        private ComboBox cboCategory;
        private NumericUpDown nudPrice, nudPurchasePrice, nudMinStockLimit, nudWholesalePrice, nudSemiWholesalePrice;
        private CheckBox chkActive, chkPrintLocalBarcode;
        private Button btnSave, btnCancel;
        private int _selectedID = 0;

        public FrmProductCard(int id = 0)
        {
            _selectedID = id;
            InitUI();
            LoadCategoriesCombo();
            if (_selectedID > 0)
            {
                LoadProductDetails();
            }
            else
            {
                ClearDetail();
            }
        }

        private void InitUI()
        {
            this.Text = _selectedID > 0 ? "تعديل بيانات الصنف" : "إضافة صنف جديد";
            this.Size = new Size(765, 510);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Main Panel for 2-column layout (takes place of tcDetails)
            var pnlDetails = new Panel { Dock = DockStyle.Top, Height = 410, BackColor = Theme.BgCard, Padding = new Padding(15) };
            this.Controls.Add(pnlDetails);

            // Populate Column 1 (Right Column): Basic Info & Description
            // X coordinate starts from right. Left edges are around X=405 for inputs and X=610 for labels.
            int rx = 405; 
            int ry = 20;

            AddField(pnlDetails, "كود الصنف:", rx, ry, out txtCode);
            txtCode.ReadOnly = true;
            ry += 40;

            AddField(pnlDetails, "اسم الصنف:", rx, ry, out txtName);
            ry += 40;

            pnlDetails.Controls.Add(new Label { Text = "التصنيف:", Location = new Point(rx + 215, ry + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            cboCategory = new ComboBox { Location = new Point(rx + 35, ry), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            var btnAddCat = new Button { Text = "➕", Location = new Point(rx, ry), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnAddCat.Click += (s, e) => { new FrmCategories().ShowDialog(); LoadCategoriesCombo(); };
            pnlDetails.Controls.AddRange(new Control[] { cboCategory, btnAddCat });
            ry += 40;

            AddField(pnlDetails, "الماركة:", rx, ry, out txtBrand);
            ry += 40;

            AddField(pnlDetails, "الوحدة:", rx, ry, out txtUnit);
            ry += 40;

            // Checkboxes side by side
            chkActive = new CheckBox { Text = "صنف نشط", Location = new Point(rx + 110, ry), Width = 95, ForeColor = Theme.TextMain, Checked = true, AutoSize = true };
            chkPrintLocalBarcode = new CheckBox { Text = "طباعة باركود محلي", Location = new Point(rx, ry), Width = 110, ForeColor = Theme.TextMain, Checked = true, AutoSize = true };
            pnlDetails.Controls.AddRange(new Control[] { chkActive, chkPrintLocalBarcode });
            ry += 40;

            pnlDetails.Controls.Add(new Label { Text = "الوصف:", Location = new Point(rx + 215, ry + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txtDescription = new TextBox { Location = new Point(rx, ry), Width = 205, Height = 100, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlDetails.Controls.Add(txtDescription);


            // Populate Column 2 (Left Column): Pricing, Stock & Barcodes
            // X coordinate starts from right (or left in flipped RTL view). Inputs around X=30.
            int lx = 30;
            int ly = 20;

            AddNud(pnlDetails, "سعر الشراء:", lx, ly, out nudPurchasePrice, 2);
            ly += 40;

            AddNud(pnlDetails, "سعر قطاعي:", lx, ly, out nudPrice, 2);
            ly += 40;

            AddNud(pnlDetails, "سعر نصف الجملة:", lx, ly, out nudSemiWholesalePrice, 2);
            ly += 40;

            AddNud(pnlDetails, "سعر الجملة:", lx, ly, out nudWholesalePrice, 2);
            ly += 40;

            AddNud(pnlDetails, "حد الطلب:", lx, ly, out nudMinStockLimit, 3);
            ly += 40;

            AddField(pnlDetails, "رقم القطعة (OEM):", lx, ly, out txtPartNumber);
            ly += 40;

            pnlDetails.Controls.Add(new Label { Text = "الباركود الدولي:", Location = new Point(lx + 215, ly + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txtInternationalCode = new TextBox { Location = new Point(lx + 35, ly), Width = 170, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            var btnMultiBarcode = new Button { Text = "➕", Location = new Point(lx, ly), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnMultiBarcode.Click += BtnMultiBarcode_Click;
            pnlDetails.Controls.AddRange(new Control[] { txtInternationalCode, btnMultiBarcode });
            ly += 40;

            AddField(pnlDetails, "موقع الرف:", lx, ly, out txtShelfLocation);
            ly += 40;

            AddField(pnlDetails, "الموديل المتوافق:", lx, ly, out txtCarModel);


            // Footer Panel for Save / Cancel
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Theme.BgCard };
            btnSave = Theme.MakeButton("💾 حفظ", 390, 12, 160, 36, Theme.Accent);
            btnCancel = Theme.MakeButton("❌ إلغاء", 210, 12, 160, 36, Color.FromArgb(100, 110, 120));

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => this.Close();

            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Controls.Add(pnlFooter);

            Theme.ApplyFormRTL(this);
        }

        private void AddField(Control parent, string label, int x, int y, out TextBox txt)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 215, y + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txt = new TextBox { Location = new Point(x, y), Width = 205, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(txt);
        }

        private void AddNud(Control parent, string label, int x, int y, out NumericUpDown nud, int decimals)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 215, y + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            nud = new NumericUpDown { Location = new Point(x, y), Width = 205, Minimum = 0, Maximum = 9999999, DecimalPlaces = decimals, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(nud);
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

        private void LoadProductDetails()
        {
            var dr = ProductDAL.GetByID(_selectedID);
            if (dr == null) return;
            txtCode.Text = dr["ProductCode"].ToString();
            txtName.Text = dr["ProductName"].ToString();
            txtPartNumber.Text = dr["PartNumber"] != DBNull.Value ? dr["PartNumber"].ToString() : "";
            txtInternationalCode.Text = dr.Table.Columns.Contains("InternationalCode") && dr["InternationalCode"] != DBNull.Value ? dr["InternationalCode"].ToString() : "";
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
            chkPrintLocalBarcode.Checked = dr.Table.Columns.Contains("PrintLocalBarcode") && dr["PrintLocalBarcode"] != DBNull.Value ? Convert.ToBoolean(dr["PrintLocalBarcode"]) : true;

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
            txtCode.Text = ProductDAL.GetNextProductCode();
            txtName.Clear(); 
            txtPartNumber.Clear();
            txtInternationalCode.Clear();
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
            chkPrintLocalBarcode.Checked = true;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم الصنف"); return; }
            
            // تحقق من عدم تكرار الأكواد الدولية
            string barcodesInput = txtInternationalCode.Text.Trim();
            if (!string.IsNullOrEmpty(barcodesInput))
            {
                string[] barcodes = barcodesInput.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var bc in barcodes)
                {
                    string owner = ProductDAL.GetOwnerOfInternationalBarcode(bc, _selectedID);
                    if (owner != null)
                    {
                        MessageBox.Show($"تعارض: الكود \"{bc}\" مسجَّل بالفعل لصنف بكود محلي: {owner}", "تعارض كود دولي", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            int? categoryID = null;
            if (cboCategory.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                categoryID = ci.ID;
            }

            int id = ProductDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtUnit.Text, nudPrice.Value, chkActive.Checked,
                nudPurchasePrice.Value, nudMinStockLimit.Value, txtDescription.Text,
                txtPartNumber.Text.Trim(), categoryID, txtCarModel.Text.Trim(), txtBrand.Text.Trim(), txtShelfLocation.Text.Trim(),
                nudWholesalePrice.Value, nudSemiWholesalePrice.Value, txtInternationalCode.Text.Trim(), chkPrintLocalBarcode.Checked);
            
            if (id > 0) 
            { 
                MessageBox.Show("✅ تم الحفظ"); 
                this.DialogResult = DialogResult.OK; 
                this.Close(); 
            }
            else 
            {
                MessageBox.Show("❌ فشل الحفظ");
            }
        }

        private void BtnMultiBarcode_Click(object sender, EventArgs e)
        {
            using (var dlg = new FrmMultiBarcodes(txtInternationalCode.Text, _selectedID))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtInternationalCode.Text = dlg.ResultBarcodes;
                }
            }
        }
    }

    public class FrmMultiBarcodes : Form
    {
        private TextBox[] txtBarcodes = new TextBox[5];
        private Button btnOk, btnCancel;
        private int _productID;
        public string ResultBarcodes { get; private set; }

        public FrmMultiBarcodes(string existingBarcodes, int productID = 0)
        {
            _productID = productID;
            this.Text = "إدارة الأكواد الدولية (الباركود) - بحد أقصى 5";
            this.Size = new Size(380, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            int y = 20;
            string[] parts = (existingBarcodes ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < 5; i++)
            {
                this.Controls.Add(new Label { Text = $"كود دولي {i + 1}:", Location = new Point(250, y + 3), AutoSize = true, ForeColor = Theme.TextMain });
                txtBarcodes[i] = new TextBox
                {
                    Location = new Point(20, y),
                    Width = 220,
                    BackColor = Theme.BgInput,
                    ForeColor = Theme.TextMain,
                    BorderStyle = BorderStyle.FixedSingle
                };
                if (i < parts.Length)
                    txtBarcodes[i].Text = parts[i].Trim();
                this.Controls.Add(txtBarcodes[i]);
                y += 40;
            }

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Theme.BgCard };
            btnOk     = Theme.MakeButton("💾 حفظ",   190, 12, 150, 36, Theme.Accent);
            btnCancel = Theme.MakeButton("❌ إلغاء", 20,  12, 150, 36, Color.FromArgb(100, 110, 120));

            btnOk.Click += (s, e) =>
            {
                var newList = new System.Collections.Generic.List<string>();
                for (int i = 0; i < 5; i++)
                {
                    string barcode = txtBarcodes[i].Text.Trim();
                    if (string.IsNullOrEmpty(barcode)) continue;

                    // تحقق تكرار داخل القائمة نفسها
                    if (newList.Contains(barcode))
                    {
                        MessageBox.Show($"تكرار: الكود \"{barcode}\" مدخل أكثر من مرة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // تحقق وجوده في صنف آخر
                    string owner = ProductDAL.GetOwnerOfInternationalBarcode(barcode, _productID);
                    if (owner != null)
                    {
                        MessageBox.Show($"تعارض: الكود \"{barcode}\" مسجَّل بالفعل لصنف بكود محلي: {owner}", "تعارض كود دولي", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    newList.Add(barcode);
                }
                ResultBarcodes = string.Join(",", newList);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            btnCancel.Click += (s, e) => this.Close();

            pnlFooter.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.Controls.Add(pnlFooter);

            Theme.ApplyFormRTL(this);
        }
    }
}

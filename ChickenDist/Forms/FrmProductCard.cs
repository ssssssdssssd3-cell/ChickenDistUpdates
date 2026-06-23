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
        private TextBox txtCode, txtName, txtDescription, txtPartNumber, txtCarModel, txtBrand, txtShelfLocation, txtInternationalCode;
        private ComboBox cboCategory, cboUnit;
        private Button btnAddUnit;
        private NumericUpDown nudPrice, nudPurchasePrice, nudMinStockLimit, nudWholesalePrice, nudSemiWholesalePrice;
        private CheckBox chkActive, chkPrintLocalBarcode, chkIsService;
        private Button btnSave, btnCancel;
        private int _selectedID = 0;

        // Multi-Unit Controls
        private TextBox txtUnit1Barcode;
        private ComboBox cboUnit1Name, cboUnit2Name;
        private Button btnAddUnit1Name, btnAddUnit2Name;
        private NumericUpDown nudUnit1SalePrice, nudUnit1PurchasePrice;
        private TextBox txtUnit2Barcode;
        private NumericUpDown nudUnit2Factor, nudUnit2SalePrice, nudUnit2PurchasePrice;
        private NumericUpDown nudUnit3Factor;
        private Button btnUnit1MultiBarcode, btnUnit2MultiBarcode;
        private Label lblUnit1Header, lblUnit2Header;

        public FrmProductCard(int id = 0)
        {
            _selectedID = id;
            InitUI();
            LoadCategoriesCombo();
            LoadUnitsCombos();
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
            this.Size = new Size(820, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Tab Control
            var tabCtrl = new TabControl { Dock = DockStyle.Fill };
            this.Controls.Add(tabCtrl);

            // Tab 1: Basic Info
            var tabBasic = new TabPage { Text = "البيانات الأساسية والتسعير الكلي", BackColor = Theme.BgCard, Padding = new Padding(15) };
            tabCtrl.TabPages.Add(tabBasic);

            // Right Column in Basic Info
            int rx = 420;
            int ry = 15;

            AddField(tabBasic, "كود الصنف:", rx, ry, out txtCode);
            txtCode.ReadOnly = true;
            ry += 35;

            AddField(tabBasic, "اسم الصنف:", rx, ry, out txtName);
            ry += 35;

            tabBasic.Controls.Add(new Label { Text = "التصنيف:", Location = new Point(rx + 215, ry + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            cboCategory = new ComboBox { Location = new Point(rx + 35, ry), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            var btnAddCat = new Button { Text = "➕", Location = new Point(rx, ry), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnAddCat.Click += (s, e) => { new FrmCategories().ShowDialog(); LoadCategoriesCombo(); };
            tabBasic.Controls.AddRange(new Control[] { cboCategory, btnAddCat });
            ry += 35;

            AddField(tabBasic, "الماركة:", rx, ry, out txtBrand);
            ry += 35;

            AddUnitComboField(tabBasic, "الوحدة الكبرى:", rx, ry, out cboUnit, out btnAddUnit);
            btnAddUnit.Click += (s, e) => { new FrmUnits().ShowDialog(); LoadUnitsCombos(); };
            ry += 35;

            chkActive = new CheckBox { Text = "صنف نشط", Location = new Point(rx + 35, ry), ForeColor = Theme.TextMain, Checked = true, AutoSize = true };
            tabBasic.Controls.Add(chkActive);
            ry += 28;

            chkPrintLocalBarcode = new CheckBox { Text = "طباعة باركود محلي", Location = new Point(rx + 35, ry), ForeColor = Theme.TextMain, Checked = true, AutoSize = true };
            tabBasic.Controls.Add(chkPrintLocalBarcode);
            ry += 28;

            chkIsService = new CheckBox { Text = "🔧 صنف خدمة (يُباع بالسالب)", Location = new Point(rx + 35, ry), ForeColor = Color.FromArgb(180, 120, 0), Checked = false, AutoSize = true };
            chkIsService.Font = new Font(Theme.FontMain, FontStyle.Bold);
            tabBasic.Controls.Add(chkIsService);
            ry += 30;

            tabBasic.Controls.Add(new Label { Text = "الوصف:", Location = new Point(rx + 215, ry + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txtDescription = new TextBox { Location = new Point(rx, ry), Width = 205, Height = 80, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            tabBasic.Controls.Add(txtDescription);

            // Left Column in Basic Info
            int lx = 20;
            int ly = 15;

            AddNud(tabBasic, "شراء الكبرى:", lx, ly, out nudPurchasePrice, 2);
            ly += 35;

            AddNud(tabBasic, "بيع قطاعي الكبرى:", lx, ly, out nudPrice, 2);
            ly += 35;

            AddNud(tabBasic, "نصف جملة الكبرى:", lx, ly, out nudSemiWholesalePrice, 2);
            ly += 35;

            AddNud(tabBasic, "جملة الكبرى:", lx, ly, out nudWholesalePrice, 2);
            ly += 35;

            AddNud(tabBasic, "حد الطلب (صغرى):", lx, ly, out nudMinStockLimit, 3);
            ly += 35;

            AddField(tabBasic, "رقم OEM:", lx, ly, out txtPartNumber);
            ly += 35;

            tabBasic.Controls.Add(new Label { Text = "الباركود الدولي:", Location = new Point(lx + 215, ly + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txtInternationalCode = new TextBox { Location = new Point(lx + 35, ly), Width = 170, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            var btnMultiBarcode = new Button { Text = "➕", Location = new Point(lx, ly), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnMultiBarcode.Click += BtnMultiBarcode_Click;
            tabBasic.Controls.AddRange(new Control[] { txtInternationalCode, btnMultiBarcode });
            ly += 35;

            AddField(tabBasic, "موقع الرف:", lx, ly, out txtShelfLocation);
            ly += 35;

            AddField(tabBasic, "الموديل المتوافق:", lx, ly, out txtCarModel);


            // Tab 2: Multi-Unit Settings
            var tabUnits = new TabPage { Text = "تعدد الوحدات (العبوات والتجزئة)", BackColor = Theme.BgCard, Padding = new Padding(15) };
            tabCtrl.TabPages.Add(tabUnits);

            int ux = 420;
            int uy = 15;

            // Unit 2 (Middle Unit) Group on the Right
            lblUnit2Header = new Label { Text = "⚙️ خانات الوحدة الوسطى:", Location = new Point(ux, uy), Width = 300, Font = new Font(Theme.FontMain, FontStyle.Bold), ForeColor = Theme.Primary };
            tabUnits.Controls.Add(lblUnit2Header);
            uy += 30;
            AddUnitComboField(tabUnits, "اسم الوحدة الوسطى:", ux, uy, out cboUnit2Name, out btnAddUnit2Name);
            btnAddUnit2Name.Click += (s, e) => { new FrmUnits().ShowDialog(); LoadUnitsCombos(); };
            uy += 35;
            AddNud(tabUnits, "تحتوي على كم صغرى؟:", ux, uy, out nudUnit2Factor, 0);
            uy += 35;
            
            tabUnits.Controls.Add(new Label { Text = "باركود الوسطى:", Location = new Point(ux + 215, uy + 3), Width = 110, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txtUnit2Barcode = new TextBox { Location = new Point(ux + 35, uy), Width = 170, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            btnUnit2MultiBarcode = new Button { Text = "➕", Location = new Point(ux, uy), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnUnit2MultiBarcode.Click += BtnUnit2MultiBarcode_Click;
            tabUnits.Controls.AddRange(new Control[] { txtUnit2Barcode, btnUnit2MultiBarcode });
            
            uy += 35;
            AddNud(tabUnits, "شراء الوسطى:", ux, uy, out nudUnit2PurchasePrice, 2);
            uy += 35;
            AddNud(tabUnits, "بيع قطاعي الوسطى:", ux, uy, out nudUnit2SalePrice, 2);

            // Unit 1 (Smallest Unit) Group on the Left
            int ux2 = 20;
            int uy2 = 15;

            lblUnit1Header = new Label { Text = "⚙️ خانات الوحدة الصغرى (التجزئة/القطعة):", Location = new Point(ux2, uy2), Width = 300, Font = new Font(Theme.FontMain, FontStyle.Bold), ForeColor = Theme.Primary };
            tabUnits.Controls.Add(lblUnit1Header);
            uy2 += 30;
            AddUnitComboField(tabUnits, "اسم الوحدة الصغرى:", ux2, uy2, out cboUnit1Name, out btnAddUnit1Name);
            btnAddUnit1Name.Click += (s, e) => { new FrmUnits().ShowDialog(); LoadUnitsCombos(); };
            uy2 += 35;
            
            tabUnits.Controls.Add(new Label { Text = "باركود الصغرى:", Location = new Point(ux2 + 215, uy2 + 3), Width = 110, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txtUnit1Barcode = new TextBox { Location = new Point(ux2 + 35, uy2), Width = 170, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            btnUnit1MultiBarcode = new Button { Text = "➕", Location = new Point(ux2, uy2), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnUnit1MultiBarcode.Click += BtnUnit1MultiBarcode_Click;
            tabUnits.Controls.AddRange(new Control[] { txtUnit1Barcode, btnUnit1MultiBarcode });
            
            uy2 += 35;
            AddNud(tabUnits, "شراء الصغرى:", ux2, uy2, out nudUnit1PurchasePrice, 2);
            uy2 += 35;
            AddNud(tabUnits, "بيع قطاعي الصغرى:", ux2, uy2, out nudUnit1SalePrice, 2);
            uy2 += 40;

            // Unit 3 conversion to Unit 2
            tabUnits.Controls.Add(new Label { Text = "⚙️ علاقة الوحدة الكبرى بالوسطى:", Location = new Point(ux2, uy2), Width = 300, Font = new Font(Theme.FontMain, FontStyle.Bold), ForeColor = Theme.Primary });
            uy2 += 30;
            AddNud(tabUnits, "الكبرى تحتوي كم وسطى؟:", ux2, uy2, out nudUnit3Factor, 0);
            tabUnits.Controls.Add(new Label { Text = "*(أو تحتوي كم صغرى مباشرة في حال عدم تفعيل الوحدة الوسطى)", Location = new Point(ux2, uy2 + 30), Width = 300, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) });

            // Bind Combo events to update Headers
            cboUnit2Name.SelectedIndexChanged += (s, e) => UpdateUnitHeaders();
            cboUnit2Name.TextChanged += (s, e) => UpdateUnitHeaders();
            cboUnit1Name.SelectedIndexChanged += (s, e) => UpdateUnitHeaders();
            cboUnit1Name.TextChanged += (s, e) => UpdateUnitHeaders();

            // Footer Panel for Save / Cancel
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Theme.BgCard };
            btnSave = Theme.MakeButton("💾 حفظ", 420, 12, 160, 36, Theme.Accent);
            btnCancel = Theme.MakeButton("❌ إلغاء", 240, 12, 160, 36, Color.FromArgb(100, 110, 120));

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => this.Close();

            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Controls.Add(pnlFooter);

            UpdateUnitHeaders();

            Theme.ApplyFormRTL(this);
        }

        private void AddField(Control parent, string label, int x, int y, out TextBox txt)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 215, y + 3), Width = 110, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txt = new TextBox { Location = new Point(x, y), Width = 205, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(txt);
        }

        private void AddUnitComboField(Control parent, string label, int x, int y, out ComboBox cbo, out Button btnAdd)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 215, y + 3), Width = 110, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            cbo = new ComboBox { Location = new Point(x + 35, y), Width = 170, DropDownStyle = ComboBoxStyle.DropDown, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            btnAdd = new Button { Text = "➕", Location = new Point(x, y), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            parent.Controls.AddRange(new Control[] { cbo, btnAdd });
        }

        private void LoadUnitsCombos()
        {
            try
            {
                DataTable dt = UnitDAL.GetAll();
                var units = new System.Collections.Generic.List<string>();
                foreach (DataRow r in dt.Rows)
                {
                    units.Add(r["UnitName"].ToString());
                }

                string valUnit = cboUnit != null ? cboUnit.Text : "";
                if (cboUnit != null)
                {
                    cboUnit.Items.Clear();
                    cboUnit.Items.AddRange(units.ToArray());
                    cboUnit.Text = valUnit;
                }

                string valUnit1 = cboUnit1Name != null ? cboUnit1Name.Text : "";
                if (cboUnit1Name != null)
                {
                    cboUnit1Name.Items.Clear();
                    cboUnit1Name.Items.AddRange(units.ToArray());
                    cboUnit1Name.Text = valUnit1;
                }

                string valUnit2 = cboUnit2Name != null ? cboUnit2Name.Text : "";
                if (cboUnit2Name != null)
                {
                    cboUnit2Name.Items.Clear();
                    cboUnit2Name.Items.AddRange(units.ToArray());
                    cboUnit2Name.Text = valUnit2;
                }
            }
            catch {}
        }

        private void AddNud(Control parent, string label, int x, int y, out NumericUpDown nud, int decimals)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 215, y + 3), Width = 110, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
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
            cboUnit.Text = dr["Unit"].ToString();
            nudPurchasePrice.Value = Convert.ToDecimal(dr["PurchasePrice"] == DBNull.Value ? 0 : dr["PurchasePrice"]);
            nudPrice.Value = Convert.ToDecimal(dr["SalePrice"]);
            nudWholesalePrice.Value = Convert.ToDecimal(dr.Table.Columns.Contains("WholesalePrice") && dr["WholesalePrice"] != DBNull.Value ? dr["WholesalePrice"] : 0);
            nudSemiWholesalePrice.Value = Convert.ToDecimal(dr.Table.Columns.Contains("SemiWholesalePrice") && dr["SemiWholesalePrice"] != DBNull.Value ? dr["SemiWholesalePrice"] : 0);
            nudMinStockLimit.Value = Convert.ToDecimal(dr["MinStockLimit"] == DBNull.Value ? 0 : dr["MinStockLimit"]);
            txtDescription.Text = dr["Description"].ToString();
            chkActive.Checked = Convert.ToBoolean(dr["IsActive"]);
            chkPrintLocalBarcode.Checked = dr.Table.Columns.Contains("PrintLocalBarcode") && dr["PrintLocalBarcode"] != DBNull.Value ? Convert.ToBoolean(dr["PrintLocalBarcode"]) : true;
            chkIsService.Checked = dr.Table.Columns.Contains("IsService") && dr["IsService"] != DBNull.Value ? Convert.ToBoolean(dr["IsService"]) : false;

            // Multi-Unit Details
            cboUnit1Name.Text = dr.Table.Columns.Contains("Unit1Name") && dr["Unit1Name"] != DBNull.Value ? dr["Unit1Name"].ToString() : "";
            txtUnit1Barcode.Text = dr.Table.Columns.Contains("Unit1Barcode") && dr["Unit1Barcode"] != DBNull.Value ? dr["Unit1Barcode"].ToString() : "";
            nudUnit1SalePrice.Value = dr.Table.Columns.Contains("Unit1SalePrice") && dr["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(dr["Unit1SalePrice"]) : 0m;
            nudUnit1PurchasePrice.Value = dr.Table.Columns.Contains("Unit1PurchasePrice") && dr["Unit1PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(dr["Unit1PurchasePrice"]) : 0m;

            cboUnit2Name.Text = dr.Table.Columns.Contains("Unit2Name") && dr["Unit2Name"] != DBNull.Value ? dr["Unit2Name"].ToString() : "";
            txtUnit2Barcode.Text = dr.Table.Columns.Contains("Unit2Barcode") && dr["Unit2Barcode"] != DBNull.Value ? dr["Unit2Barcode"].ToString() : "";
            nudUnit2Factor.Value = dr.Table.Columns.Contains("Unit2Factor") && dr["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(dr["Unit2Factor"]) : 0m;
            nudUnit2SalePrice.Value = dr.Table.Columns.Contains("Unit2SalePrice") && dr["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(dr["Unit2SalePrice"]) : 0m;
            nudUnit2PurchasePrice.Value = dr.Table.Columns.Contains("Unit2PurchasePrice") && dr["Unit2PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(dr["Unit2PurchasePrice"]) : 0m;

            nudUnit3Factor.Value = dr.Table.Columns.Contains("Unit3Factor") && dr["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(dr["Unit3Factor"]) : 0m;

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
            UpdateUnitHeaders();
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
            cboUnit.Text = "كرتونة";
            nudPurchasePrice.Value = 0;
            nudPrice.Value = 0;
            nudWholesalePrice.Value = 0;
            nudSemiWholesalePrice.Value = 0;
            nudMinStockLimit.Value = 0;
            txtDescription.Clear();
            chkActive.Checked = true;
            chkPrintLocalBarcode.Checked = true;
            chkIsService.Checked = false;

            // Multi-Unit default
            cboUnit1Name.Text = "القطعة";
            txtUnit1Barcode.Clear();
            nudUnit1SalePrice.Value = 0;
            nudUnit1PurchasePrice.Value = 0;

            cboUnit2Name.Text = "";
            txtUnit2Barcode.Clear();
            nudUnit2Factor.Value = 0;
            nudUnit2SalePrice.Value = 0;
            nudUnit2PurchasePrice.Value = 0;

            nudUnit3Factor.Value = 0;
            UpdateUnitHeaders();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم الصنف"); return; }

            // فحص تكرار اسم الصنف
            if (ProductDAL.IsNameExists(txtName.Text.Trim(), _selectedID))
            {
                MessageBox.Show($"⚠️ يوجد صنف آخر بنفس الاسم: \"{txtName.Text.Trim()}\"\nيرجى استخدام اسم مختلف أو تعديل الصنف الموجود.",
                    "تكرار اسم الصنف", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            // تحقق الباركودات
            string barcodesInput = txtInternationalCode.Text.Trim();
            string normalisedIntlBarcodes = null;
            if (!string.IsNullOrEmpty(barcodesInput))
            {
                string[] barcodes = barcodesInput.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                normalisedIntlBarcodes = string.Join(",", barcodes);
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

            // فحص باركود الوحدة الصغرى والوسطى
            string u1Barcode = txtUnit1Barcode.Text.Trim();
            string normalisedU1Barcode = null;
            if (!string.IsNullOrEmpty(u1Barcode))
            {
                string[] barcodes = u1Barcode.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                normalisedU1Barcode = string.Join(",", barcodes);
                foreach (var bc in barcodes)
                {
                    string owner = ProductDAL.GetOwnerOfInternationalBarcode(bc, _selectedID);
                    if (owner != null)
                    {
                        MessageBox.Show($"تعارض: باركود الوحدة الصغرى \"{bc}\" مسجَّل لصنف بكود محلي: {owner}", "تعارض باركود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            string u2Barcode = txtUnit2Barcode.Text.Trim();
            string normalisedU2Barcode = null;
            if (!string.IsNullOrEmpty(u2Barcode))
            {
                string[] barcodes = u2Barcode.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                normalisedU2Barcode = string.Join(",", barcodes);
                foreach (var bc in barcodes)
                {
                    string owner = ProductDAL.GetOwnerOfInternationalBarcode(bc, _selectedID);
                    if (owner != null)
                    {
                        MessageBox.Show($"تعارض: باركود الوحدة الوسطى \"{bc}\" مسجَّل لصنف بكود محلي: {owner}", "تعارض باركود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            int? categoryID = null;
            if (cboCategory.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                categoryID = ci.ID;
            }

            // الحفظ في قاعدة البيانات
            int id = ProductDAL.Save(_selectedID, txtCode.Text, txtName.Text, cboUnit.Text.Trim(), nudPrice.Value, chkActive.Checked,
                nudPurchasePrice.Value, nudMinStockLimit.Value, txtDescription.Text,
                txtPartNumber.Text.Trim(), categoryID, txtCarModel.Text.Trim(), txtBrand.Text.Trim(), txtShelfLocation.Text.Trim(),
                nudWholesalePrice.Value, nudSemiWholesalePrice.Value, normalisedIntlBarcodes, chkPrintLocalBarcode.Checked,
                chkIsService.Checked,
                cboUnit1Name.Text.Trim(), normalisedU1Barcode, nudUnit1SalePrice.Value, nudUnit1PurchasePrice.Value,
                cboUnit2Name.Text.Trim(), nudUnit2Factor.Value > 0 ? (decimal?)nudUnit2Factor.Value : null, normalisedU2Barcode, nudUnit2SalePrice.Value, nudUnit2PurchasePrice.Value,
                nudUnit3Factor.Value > 0 ? (decimal?)nudUnit3Factor.Value : null);

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

        private void BtnUnit1MultiBarcode_Click(object sender, EventArgs e)
        {
            using (var dlg = new FrmMultiBarcodes(txtUnit1Barcode.Text, _selectedID))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtUnit1Barcode.Text = dlg.ResultBarcodes;
                }
            }
        }

        private void BtnUnit2MultiBarcode_Click(object sender, EventArgs e)
        {
            using (var dlg = new FrmMultiBarcodes(txtUnit2Barcode.Text, _selectedID))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtUnit2Barcode.Text = dlg.ResultBarcodes;
                }
            }
        }

        private void UpdateUnitHeaders()
        {
            if (lblUnit2Header != null && cboUnit2Name != null)
            {
                string unit2 = string.IsNullOrWhiteSpace(cboUnit2Name.Text) ? "غير محددة" : cboUnit2Name.Text;
                lblUnit2Header.Text = $"⚙️ خانات الوحدة الوسطى ({unit2}):";
            }
            if (lblUnit1Header != null && cboUnit1Name != null)
            {
                string unit1 = string.IsNullOrWhiteSpace(cboUnit1Name.Text) ? "غير محددة" : cboUnit1Name.Text;
                lblUnit1Header.Text = $"⚙️ خانات الوحدة الصغرى ({unit1}):";
            }
        }
    }
}

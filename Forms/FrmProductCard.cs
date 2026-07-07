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
        private TextBox txtUnit2Name, txtUnit2Barcode, txtUnit1Name, txtUnit1Barcode;
        private ComboBox cboCategory;
        private NumericUpDown nudPrice, nudPurchasePrice, nudMinStockLimit, nudWholesalePrice, nudSemiWholesalePrice;
        private NumericUpDown nudUnit3Factor, nudUnit2Factor;
        private CheckBox chkActive, chkPrintLocalBarcode, chkIsService;
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
            this.Size = new Size(765, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Main Panel for 2-column layout (takes place of tcDetails)
            var pnlDetails = new Panel { Dock = DockStyle.Top, Height = 380, BackColor = Theme.BgCard, Padding = new Padding(15) };
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

            AddField(pnlDetails, "الوحدة الكبرى:", rx, ry, out txtUnit);
            ry += 40;

            // Checkboxes on separate rows (independent options)
            chkActive = new CheckBox { Text = "صنف نشط", Location = new Point(rx, ry), ForeColor = Theme.TextMain, Checked = true, AutoSize = true };
            pnlDetails.Controls.Add(chkActive);
            ry += 28;

            chkPrintLocalBarcode = new CheckBox { Text = "طباعة باركود محلي", Location = new Point(rx, ry), ForeColor = Theme.TextMain, Checked = true, AutoSize = true };
            pnlDetails.Controls.Add(chkPrintLocalBarcode);
            ry += 28;

            chkIsService = new CheckBox { Text = "🔧 صنف خدمة (يُباع بالسالب)", Location = new Point(rx, ry), ForeColor = Color.FromArgb(180, 120, 0), Checked = false, AutoSize = true };
            chkIsService.Font = new Font(Theme.FontMain, FontStyle.Bold);
            pnlDetails.Controls.Add(chkIsService);
            ry += 32;

            pnlDetails.Controls.Add(new Label { Text = "الوصف:", Location = new Point(rx + 215, ry + 3), Width = 90, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain });
            txtDescription = new TextBox { Location = new Point(rx, ry), Width = 205, Height = 65, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlDetails.Controls.Add(txtDescription);


            // Populate Column 2 (Left Column): Pricing, Stock & Barcodes
            // X coordinate starts from right (or left in flipped RTL view). Inputs around X=30.
            int lx = 30;
            int ly = 20;

            AddNud(pnlDetails, "سعر شراء الكبرى:", lx, ly, out nudPurchasePrice, 2);
            ly += 40;

            AddNud(pnlDetails, "سعر بيع الكبرى:", lx, ly, out nudPrice, 2);
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


            // ===== ميزة الوحدات المتعددة (إعدادات التجزئة تلقائية الأسعار) =====
            var pnlMultiUnits = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Theme.BgCard, Padding = new Padding(15, 0, 15, 10) };
            this.Controls.Add(pnlMultiUnits);

            var grpUnits = new GroupBox
            {
                Text = "📦 إعدادات الوحدات المتعددة (التجزئة تلقائية الأسعار)",
                Dock = DockStyle.Fill,
                ForeColor = Theme.Accent,
                Font = new Font(Theme.FontMain.FontFamily, 9.5F, FontStyle.Bold)
            };
            pnlMultiUnits.Controls.Add(grpUnits);

            // Row 1: Medium Unit
            grpUnits.Controls.Add(new Label { Text = "الوحدة المتوسطة:", Location = new Point(570, 30), Width = 120, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtUnit2Name = new TextBox { Location = new Point(430, 27), Width = 130, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
            grpUnits.Controls.Add(txtUnit2Name);

            grpUnits.Controls.Add(new Label { Text = "باركود المتوسطة:", Location = new Point(310, 30), Width = 110, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtUnit2Barcode = new TextBox { Location = new Point(170, 27), Width = 130, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
            grpUnits.Controls.Add(txtUnit2Barcode);

            grpUnits.Controls.Add(new Label { Text = "العدد في الكبرى:", Location = new Point(90, 30), Width = 75, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            nudUnit3Factor = new NumericUpDown { Location = new Point(10, 27), Width = 70, Minimum = 1, Maximum = 999999, DecimalPlaces = 0, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain, Value = 1 };
            grpUnits.Controls.Add(nudUnit3Factor);

            // Row 2: Small Unit
            grpUnits.Controls.Add(new Label { Text = "الوحدة الصغرى:", Location = new Point(570, 70), Width = 120, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtUnit1Name = new TextBox { Location = new Point(430, 67), Width = 130, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
            grpUnits.Controls.Add(txtUnit1Name);

            grpUnits.Controls.Add(new Label { Text = "باركود الصغرى:", Location = new Point(310, 70), Width = 110, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtUnit1Barcode = new TextBox { Location = new Point(170, 67), Width = 130, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
            grpUnits.Controls.Add(txtUnit1Barcode);

            grpUnits.Controls.Add(new Label { Text = "العدد بالمتوسطة/الكبرى:", Location = new Point(85, 70), Width = 80, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = new Font(Theme.FontMain.FontFamily, 8.0F) });
            nudUnit2Factor = new NumericUpDown { Location = new Point(10, 67), Width = 70, Minimum = 1, Maximum = 999999, DecimalPlaces = 0, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain, Value = 1 };
            grpUnits.Controls.Add(nudUnit2Factor);

            // Row 3: Guide/Tip Note
            var lblAccTip = new Label
            {
                Text = "💡 تنبيه: إذا لم تستخدم وحدة متوسطة، اكتب معامل تحويل الصغرى في الكبرى مباشرة بخانة (العدد بالمتوسطة/الكبرى) واترك المتوسطة فارغة.",
                Font = new Font(Theme.FontMain.FontFamily, 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(127, 140, 141),
                Location = new Point(10, 110),
                Size = new Size(700, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            grpUnits.Controls.Add(lblAccTip);


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
            chkIsService.Checked = dr.Table.Columns.Contains("IsService") && dr["IsService"] != DBNull.Value ? Convert.ToBoolean(dr["IsService"]) : false;

            // تحميل الوحدات المتعددة وعوامل التحويل
            txtUnit2Name.Text = dr.Table.Columns.Contains("Unit2Name") && dr["Unit2Name"] != DBNull.Value ? dr["Unit2Name"].ToString() : "";
            txtUnit2Barcode.Text = dr.Table.Columns.Contains("Unit2Barcode") && dr["Unit2Barcode"] != DBNull.Value ? dr["Unit2Barcode"].ToString() : "";
            nudUnit3Factor.Value = dr.Table.Columns.Contains("Unit3Factor") && dr["Unit3Factor"] != DBNull.Value && Convert.ToDecimal(dr["Unit3Factor"]) > 0 ? Convert.ToDecimal(dr["Unit3Factor"]) : 1m;

            txtUnit1Name.Text = dr.Table.Columns.Contains("Unit1Name") && dr["Unit1Name"] != DBNull.Value ? dr["Unit1Name"].ToString() : "";
            txtUnit1Barcode.Text = dr.Table.Columns.Contains("Unit1Barcode") && dr["Unit1Barcode"] != DBNull.Value ? dr["Unit1Barcode"].ToString() : "";
            nudUnit2Factor.Value = dr.Table.Columns.Contains("Unit2Factor") && dr["Unit2Factor"] != DBNull.Value && Convert.ToDecimal(dr["Unit2Factor"]) > 0 ? Convert.ToDecimal(dr["Unit2Factor"]) : 1m;

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
            chkIsService.Checked = false;

            txtUnit2Name.Clear();
            txtUnit2Barcode.Clear();
            nudUnit3Factor.Value = 1;
            txtUnit1Name.Clear();
            txtUnit1Barcode.Clear();
            nudUnit2Factor.Value = 1;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم الصنف"); return; }

            // ─── فحص تكرار اسم الصنف ───
            if (ProductDAL.IsNameExists(txtName.Text.Trim(), _selectedID))
            {
                MessageBox.Show($"⚠️ يوجد صنف آخر بنفس الاسم: \"{txtName.Text.Trim()}\"\nيرجى استخدام اسم مختلف أو تعديل الصنف الموجود.",
                    "تكرار اسم الصنف", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            // تحقق من عدم تكرار الأكواد الدولية
            string barcodesInput = txtInternationalCode.Text.Trim();
            if (!string.IsNullOrEmpty(barcodesInput))
            {
                string[] barcodes = barcodesInput.Split(new[] { ',', ';', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var bc in barcodes)
                {
                    string trimmed = bc.Trim();
                    if (seen.Contains(trimmed))
                    {
                        MessageBox.Show($"تنبيه: الكود الدولي \"{trimmed}\" مكرر داخل نفس الصنف!", "تكرار كود دولي", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    seen.Add(trimmed);

                    string owner = ProductDAL.GetOwnerOfInternationalBarcode(trimmed, _selectedID);
                    if (owner != null)
                    {
                        MessageBox.Show($"تعارض: الكود \"{trimmed}\" مسجَّل بالفعل لصنف بكود محلي: {owner}", "تعارض كود دولي", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            int? categoryID = null;
            if (cboCategory.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                categoryID = ci.ID;
            }

            // ─── احتساب قيم التجزئة والوحدات المتعددة تلقائياً ───
            string u1Name = txtUnit1Name.Text.Trim();
            string u1Barcode = txtUnit1Barcode.Text.Trim();
            decimal? u1SalePrice = null;
            decimal? u1PurchasePrice = null;

            string u2Name = txtUnit2Name.Text.Trim();
            string u2Barcode = txtUnit2Barcode.Text.Trim();
            decimal? u2Factor = null;
            decimal? u2SalePrice = null;
            decimal? u2PurchasePrice = null;

            decimal? u3Factor = null;

            // 1. حالة وجود 3 وحدات (كبرى، متوسطة، صغرى)
            if (!string.IsNullOrEmpty(u2Name) && !string.IsNullOrEmpty(u1Name))
            {
                decimal u3f = nudUnit3Factor.Value; // عدد المتوسطة في الكبرى
                if (u3f <= 0) u3f = 1;
                u3Factor = u3f;

                decimal u2f = nudUnit2Factor.Value; // عدد الصغرى في المتوسطة
                if (u2f <= 0) u2f = 1;
                u2Factor = u2f;

                // سعر المتوسطة = سعر الكبرى / عدد المتوسطة في الكبرى
                u2SalePrice = nudPrice.Value / u3f;
                u2PurchasePrice = nudPurchasePrice.Value / u3f;

                // سعر الصغرى = سعر المتوسطة / عدد الصغرى في المتوسطة
                u1SalePrice = u2SalePrice.Value / u2f;
                u1PurchasePrice = u2PurchasePrice.Value / u2f;
            }
            // 2. حالة وجود وحدتين فقط (كبرى وصغرى) - بدون وحدة متوسطة
            else if (string.IsNullOrEmpty(u2Name) && !string.IsNullOrEmpty(u1Name))
            {
                decimal u2f = nudUnit2Factor.Value; // عدد الصغرى في الكبرى مباشرة
                if (u2f <= 0) u2f = 1;
                u2Factor = u2f;

                // سعر الصغرى = سعر الكبرى / المعامل
                u1SalePrice = nudPrice.Value / u2f;
                u1PurchasePrice = nudPurchasePrice.Value / u2f;
            }

            int id = ProductDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtUnit.Text, nudPrice.Value, chkActive.Checked,
                nudPurchasePrice.Value, nudMinStockLimit.Value, txtDescription.Text,
                txtPartNumber.Text.Trim(), categoryID, txtCarModel.Text.Trim(), txtBrand.Text.Trim(), txtShelfLocation.Text.Trim(),
                nudWholesalePrice.Value, nudSemiWholesalePrice.Value, txtInternationalCode.Text.Trim(), chkPrintLocalBarcode.Checked,
                chkIsService.Checked,
                u1Name, u1Barcode, u1SalePrice, u1PurchasePrice,
                u2Name, u2Factor, u2Barcode, u2SalePrice, u2PurchasePrice,
                u3Factor);
            
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
}

using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmClothingMatrix : Form
    {
        private TextBox txtBaseName;
        private TextBox txtModelCode;
        private ComboBox cboCategory;
        private TextBox txtColors; // Commas-separated list of colors (Brand)
        private TextBox txtSizes;  // Commas-separated list of sizes (CarModel)
        private ComboBox cboMaterial; // (ProducerCompany)
        private NumericUpDown nudCost;
        private NumericUpDown nudPrice;
        private NumericUpDown nudWholesalePrice;
        private NumericUpDown nudMinStockLimit;
        private ComboBox cboShelfLocation;
        private Button btnGenerate;
        private Button btnCancel;

        public FrmClothingMatrix()
        {
            InitUI();
            LoadData();
        }

        private void InitUI()
        {
            this.Text = "توليد مصفوفة الملابس والأحذية";
            this.Size = new Size(500, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTitle = Theme.MakeTitleBar("📦 توليد مصفوفة الملابس", "توليد تلقائي لكافة تركيبات الألوان والمقاسات للصنف بباركودات مستقلة");
            this.Controls.Add(pnlTitle);

            int y = 80;

            AddLabel("الاسم الأساسي للموديل (مثال: قميص جينز كاجوال) *:", 20, ref y);
            txtBaseName = new TextBox { Location = new Point(20, y), Width = 440, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtBaseName);
            y += 35;

            AddLabel("كود الموديل الأساسي (Model Code) *:", 20, ref y);
            txtModelCode = new TextBox { Location = new Point(20, y), Width = 440, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtModelCode);
            y += 35;

            // Category & Material side-by-side
            AddLabel("التصنيف:", 20, ref y);
            cboCategory = new ComboBox { Location = new Point(20, y), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontNormal };
            this.Controls.Add(cboCategory);

            var lblMaterial = new Label { Text = "الخامة (Material):", Location = new Point(250, y - 22), Width = 210, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.Add(lblMaterial);
            cboMaterial = new ComboBox { Location = new Point(250, y), Width = 210, DropDownStyle = ComboBoxStyle.DropDown, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontNormal };
            this.Controls.Add(cboMaterial);
            y += 35;

            // Colors
            AddLabel("الألوان المطلوبة (تفصل بفاصلة أو مسافة، مثال: أسود، أزرق، أبيض):", 20, ref y);
            txtColors = new TextBox { Location = new Point(20, y), Width = 440, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            txtColors.Text = "أسود, كحلي, أبيض, رمادي";
            this.Controls.Add(txtColors);
            y += 35;

            // Sizes
            AddLabel("المقاسات المطلوبة (تفصل بفاصلة أو مسافة، مثال: S, M, L, XL):", 20, ref y);
            txtSizes = new TextBox { Location = new Point(20, y), Width = 440, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            txtSizes.Text = "S, M, L, XL, XXL";
            this.Controls.Add(txtSizes);
            y += 35;

            // Prices side-by-side
            AddLabel("سعر الشراء (ج):", 20, ref y);
            nudCost = new NumericUpDown { Location = new Point(20, y), Width = 210, DecimalPlaces = 2, Maximum = 100000, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontNormal };
            this.Controls.Add(nudCost);

            var lblPrice = new Label { Text = "سعر البيع قطاعي (ج):", Location = new Point(250, y - 22), Width = 210, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.Add(lblPrice);
            nudPrice = new NumericUpDown { Location = new Point(250, y), Width = 210, DecimalPlaces = 2, Maximum = 100000, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontNormal };
            this.Controls.Add(nudPrice);
            y += 35;

            // Wholesale & Shelf Location
            AddLabel("سعر الجملة (ج):", 20, ref y);
            nudWholesalePrice = new NumericUpDown { Location = new Point(20, y), Width = 210, DecimalPlaces = 2, Maximum = 100000, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontNormal };
            this.Controls.Add(nudWholesalePrice);

            var lblShelf = new Label { Text = "موقع الرف:", Location = new Point(250, y - 22), Width = 210, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.Add(lblShelf);
            cboShelfLocation = new ComboBox { Location = new Point(250, y), Width = 210, DropDownStyle = ComboBoxStyle.DropDown, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontNormal };
            this.Controls.Add(cboShelfLocation);
            y += 35;

            // MinStockLimit
            AddLabel("حد أدنى المخزون (تنبيه النقص):", 20, ref y);
            nudMinStockLimit = new NumericUpDown { Location = new Point(20, y), Width = 210, DecimalPlaces = 0, Maximum = 1000, Value = 5, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontNormal };
            this.Controls.Add(nudMinStockLimit);
            y += 45;

            // Buttons
            btnGenerate = Theme.MakeButton("🚀 توليد الأصناف والمصفوفة", 250, y, 210, 40, Theme.Success);
            btnGenerate.Click += BtnGenerate_Click;
            this.Controls.Add(btnGenerate);

            btnCancel = Theme.MakeButton("❌ إلغاء", 20, y, 210, 40, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int x, ref int y)
        {
            var lbl = new Label 
            { 
                Text = text, 
                Location = new Point(x, y), 
                Width = 440, 
                Height = 18, 
                ForeColor = Theme.TextMain, 
                Font = Theme.FontBold, 
                TextAlign = ContentAlignment.TopRight 
            };
            this.Controls.Add(lbl);
            y += 22;
        }

        private void LoadData()
        {
            // Load Categories
            cboCategory.Items.Clear();
            cboCategory.Items.Add(new ComboItem(0, "بدون تصنيف"));
            var dtCat = CategoryDAL.GetAll();
            foreach (DataRow r in dtCat.Rows)
            {
                cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
            }
            cboCategory.SelectedIndex = 0;

            // Load Materials (ProducerCompanies)
            cboMaterial.Items.Clear();
            var dtMaterials = DbHelper.Query("SELECT DISTINCT ProducerName FROM ProducerCompanies ORDER BY ProducerName");
            foreach (DataRow r in dtMaterials.Rows)
            {
                cboMaterial.Items.Add(r["ProducerName"].ToString());
            }

            // Load Shelf Locations
            cboShelfLocation.Items.Clear();
            var dtShelf = DbHelper.Query("SELECT DISTINCT ShelfName FROM ShelfLocations ORDER BY ShelfName");
            foreach (DataRow r in dtShelf.Rows)
            {
                cboShelfLocation.Items.Add(r["ShelfName"].ToString());
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBaseName.Text)) { MessageBox.Show("يرجى إدخال الاسم الأساسي للموديل"); return; }
            if (string.IsNullOrWhiteSpace(txtModelCode.Text)) { MessageBox.Show("يرجى إدخال كود الموديل"); return; }

            // Parse colors
            string[] colors = txtColors.Text.Split(new[] { ',', ';', '،', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (colors.Length == 0) colors = new[] { "" };

            // Parse sizes
            string[] sizes = txtSizes.Text.Split(new[] { ',', ';', '،', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (sizes.Length == 0) sizes = new[] { "" };

            int? categoryID = null;
            if (cboCategory.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                categoryID = ci.ID;
            }

            string material = cboMaterial.Text.Trim();
            string shelf = cboShelfLocation.Text.Trim();

            // Save material if new
            if (!string.IsNullOrWhiteSpace(material))
            {
                LookupDAL.Save("ProducerCompanies", "ProducerID", "ProducerCode", "ProducerName", "PRD", 0, material);
            }

            // Save shelf if new
            if (!string.IsNullOrWhiteSpace(shelf))
            {
                LookupDAL.Save("ShelfLocations", "ShelfID", "ShelfCode", "ShelfName", "SHF", 0, shelf);
            }

            int generatedCount = 0;

            try
            {
                foreach (var color in colors)
                {
                    string trimmedColor = color.Trim();
                    if (!string.IsNullOrEmpty(trimmedColor))
                    {
                        // Auto-save Color (Brand)
                        LookupDAL.Save("Brands", "BrandID", "BrandCode", "BrandName", "BRD", 0, trimmedColor);
                    }

                    foreach (var size in sizes)
                    {
                        string trimmedSize = size.Trim();
                        if (!string.IsNullOrEmpty(trimmedSize))
                        {
                            // Auto-save Size (CarModel)
                            LookupDAL.Save("CarModels", "CarModelID", "CarModelCode", "CarModelName", "MDL", 0, trimmedSize);
                        }

                        // Generate unique variant name
                        string variantName = txtBaseName.Text.Trim();
                        if (!string.IsNullOrEmpty(trimmedColor)) variantName += " - " + trimmedColor;
                        if (!string.IsNullOrEmpty(trimmedSize)) variantName += " - " + trimmedSize;

                        // Check if already exists
                        if (ProductDAL.IsNameExists(variantName, 0))
                        {
                            continue; // Skip existing combinations
                        }

                        // Generate unique code
                        string productCode = ProductDAL.GetNextProductCode();

                        // Save product using ProductDAL.Save
                        ProductDAL.Save(
                            id: 0,
                            code: productCode,
                            name: variantName,
                            unit: "قطعة",
                            price: nudPrice.Value,
                            active: true,
                            purchasePrice: nudCost.Value,
                            minStockLimit: nudMinStockLimit.Value,
                            description: "توليد مصفوفة تلقائي",
                            partNumber: txtModelCode.Text.Trim(), // Model Code
                            categoryID: categoryID,
                            carModel: trimmedSize,    // Size
                            brand: trimmedColor,      // Color
                            shelfLocation: shelf,
                            wholesalePrice: nudWholesalePrice.Value,
                            producerCompany: material, // Material
                            printLocalBarcode: true
                        );

                        generatedCount++;
                    }
                }

                MessageBox.Show($"✅ تم بنجاح توليد ({generatedCount}) صنف للمصفوفة بنجاح!", "اكتمل التوليد", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ فشل توليد بعض الأصناف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

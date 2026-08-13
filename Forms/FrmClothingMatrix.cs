using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة توليد مصفوفة الألوان والمقاسات للصنف - خاصة بنشاط الملابس والأحذية
    /// </summary>
    public class FrmClothingMatrix : Form
    {
        private TextBox txtBaseName;
        private TextBox txtModelCode;
        private ComboBox cboCategory;
        private ComboBox cboMaterial;
        private TextBox txtColors;
        private TextBox txtSizes;
        private NumericUpDown nudCost;
        private NumericUpDown nudPrice;
        private NumericUpDown nudWholesalePrice;
        private NumericUpDown nudMinStockLimit;
        private ComboBox cboShelfLocation;
        private Label lblPreviewBadge;
        private Button btnGenerate;
        private Button btnCancel;

        public FrmClothingMatrix()
        {
            if (AppConfig.BusinessType != "Clothing")
            {
                MessageBox.Show("❌ ميزة مصفوفة المقاسات والألوان مخصصة فقط لنشاط محلات ومعارض الملابس والأحذية!", "تنبيه النشاط", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitUI();
            LoadData();
            UpdatePreview();
        }

        private void InitUI()
        {
            this.Text = "👗 توليد مصفوفة الملابس والأحذية";
            this.Size = new Size(820, 690);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = Theme.FontMain;

            // ── Top Header Banner ────────────────────────────────────────────────
            var pnlHeader = Theme.MakeTitleBar("👗 توليد مصفوفة الملابس والأحذية", "توليد تلقائي احترافي لكافة تركيبات الألوان والمقاسات للصنف بباركودات فريدة ومستقلة لكل صنف");
            this.Controls.Add(pnlHeader);

            // ── Main Padded Scroll Container ─────────────────────────────────────
            var pnlContainer = new Panel
            {
                Location = new Point(16, 76),
                Size = new Size(772, 510),
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlContainer);

            int currentY = 4;

            // ── Card 1: 👔 بيانات الموديل الأساسي ─────────────────────────────
            var cardModel = CreateCardPanel("👔 بيانات الموديل الأساسي (Model Info)", ref currentY, 145);
            
            // Row 1: Model Base Name & Model Code
            var lblBaseName = CreateLabel("الاسم الأساسي للموديل (مثال: قميص جينز كاجوال) *:", 16, 32, 360);
            txtBaseName = CreateTextBox("قميص جينز كاجوال", 16, 54, 360);
            cardModel.Controls.Add(lblBaseName);
            cardModel.Controls.Add(txtBaseName);

            var lblModelCode = CreateLabel("كود الموديل الأساسي (Model Code) *:", 392, 32, 340);
            txtModelCode = CreateTextBox("1001", 392, 54, 340);
            cardModel.Controls.Add(lblModelCode);
            cardModel.Controls.Add(txtModelCode);

            // Row 2: Category & Material
            var lblCategory = CreateLabel("التصنيف الرئيسية:", 16, 86, 360);
            cboCategory = new ComboBox
            {
                Location = new Point(16, 108),
                Width = 360,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f)
            };
            cardModel.Controls.Add(lblCategory);
            cardModel.Controls.Add(cboCategory);

            var lblMaterial = CreateLabel("الخامة / نوع القماش (Material):", 392, 86, 340);
            cboMaterial = new ComboBox
            {
                Location = new Point(392, 108),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f)
            };
            cardModel.Controls.Add(lblMaterial);
            cardModel.Controls.Add(cboMaterial);

            pnlContainer.Controls.Add(cardModel);

            // ── Card 2: 🎨 مصفوفة الألوان والمقاسات ─────────────────────────────
            var cardMatrix = CreateCardPanel("🎨 مصفوفة الألوان والمقاسات (Colors & Sizes Matrix)", ref currentY, 185);

            var lblColors = CreateLabel("الألوان المطلوبة (تفصل بفاصلة أو مسافة، مثال: أسود، كحلي، أبيض، رمادي):", 16, 32, 716);
            txtColors = CreateTextBox("أسود, كحلي, أبيض, رمادي", 16, 54, 716);
            txtColors.TextChanged += (s, e) => UpdatePreview();
            cardMatrix.Controls.Add(lblColors);
            cardMatrix.Controls.Add(txtColors);

            var lblSizes = CreateLabel("المقاسات المطلوبة (تفصل بفاصلة أو مسافة، مثال: S, M, L, XL, XXL):", 16, 86, 716);
            txtSizes = CreateTextBox("S, M, L, XL, XXL", 16, 108, 716);
            txtSizes.TextChanged += (s, e) => UpdatePreview();
            cardMatrix.Controls.Add(lblSizes);
            cardMatrix.Controls.Add(txtSizes);

            // Live Preview Badge
            lblPreviewBadge = new Label
            {
                Location = new Point(16, 142),
                Width = 716,
                Height = 32,
                BackColor = Color.FromArgb(236, 253, 245),
                ForeColor = Color.FromArgb(6, 95, 70),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            cardMatrix.Controls.Add(lblPreviewBadge);

            pnlContainer.Controls.Add(cardMatrix);

            // ── Card 3: 💰 الأسعار والتخزين ───────────────────────────────────
            var cardPricing = CreateCardPanel("💰 الأسعار والتخزين (Pricing & Stock Limits)", ref currentY, 145);

            // Row 1: Purchase Price, Sale Price, Wholesale Price
            var lblCost = CreateLabel("سعر الشراء / التكلفة (ج):", 16, 32, 230);
            nudCost = CreateNumericUpDown(0m, 100000m, 2, 16, 54, 230);
            cardPricing.Controls.Add(lblCost);
            cardPricing.Controls.Add(nudCost);

            var lblPrice = CreateLabel("سعر البيع قطاعي (ج):", 260, 32, 230);
            nudPrice = CreateNumericUpDown(0m, 100000m, 2, 260, 54, 230);
            cardPricing.Controls.Add(lblPrice);
            cardPricing.Controls.Add(nudPrice);

            var lblWholesale = CreateLabel("سعر الجملة (ج):", 504, 32, 228);
            nudWholesalePrice = CreateNumericUpDown(0m, 100000m, 2, 504, 54, 228);
            cardPricing.Controls.Add(lblWholesale);
            cardPricing.Controls.Add(nudWholesalePrice);

            // Row 2: Min Stock Limit & Shelf Location
            var lblMinStock = CreateLabel("حد أدنى المخزون (تنبيه النقص):", 16, 86, 360);
            nudMinStockLimit = CreateNumericUpDown(0m, 10000m, 0, 16, 108, 360);
            nudMinStockLimit.Value = 5m;
            cardPricing.Controls.Add(lblMinStock);
            cardPricing.Controls.Add(nudMinStockLimit);

            var lblShelf = CreateLabel("مكان العرض / الرف:", 392, 86, 340);
            cboShelfLocation = new ComboBox
            {
                Location = new Point(392, 108),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f)
            };
            cardPricing.Controls.Add(lblShelf);
            cardPricing.Controls.Add(cboShelfLocation);

            pnlContainer.Controls.Add(cardPricing);

            // ── Bottom Action Bar ────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 10)
            };
            var pnlBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(226, 232, 240) };
            pnlFooter.Controls.Add(pnlBorder);

            btnGenerate = Theme.MakeButton("🚀 توليد الأصناف والمصفوفة", 435, 12, 330, 42, Theme.Success);
            btnGenerate.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnGenerate.Click += BtnGenerate_Click;
            pnlFooter.Controls.Add(btnGenerate);

            btnCancel = Theme.MakeButton("❌ إلغاء", 16, 12, 150, 42, Color.FromArgb(100, 116, 139));
            btnCancel.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnCancel);

            this.Controls.Add(pnlFooter);
        }

        private Panel CreateCardPanel(string title, ref int currentY, int height)
        {
            var pnl = new Panel
            {
                Location = new Point(4, currentY),
                Width = 748,
                Height = height,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblHeader = new Label
            {
                Text = title,
                Location = new Point(0, 0),
                Width = 748,
                Height = 28,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 12, 0)
            };
            var line = new Panel { Location = new Point(0, 28), Width = 748, Height = 1, BackColor = Color.FromArgb(226, 232, 240) };

            pnl.Controls.Add(lblHeader);
            pnl.Controls.Add(line);

            currentY += height + 14;
            return pnl;
        }

        private Label CreateLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Width = width,
                Height = 20,
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private TextBox CreateTextBox(string defaultText, int x, int y, int width)
        {
            return new TextBox
            {
                Text = defaultText,
                Location = new Point(x, y),
                Width = width,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
        }

        private NumericUpDown CreateNumericUpDown(decimal min, decimal max, int decimalPlaces, int x, int y, int width)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimalPlaces,
                Location = new Point(x, y),
                Width = width,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
        }

        private void UpdatePreview()
        {
            if (txtColors == null || txtSizes == null || lblPreviewBadge == null) return;

            string[] colors = txtColors.Text.Split(new[] { ',', ';', '،', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] sizes = txtSizes.Text.Split(new[] { ',', ';', '،', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int colorCount = colors.Length;
            int sizeCount = sizes.Length;
            int total = colorCount * sizeCount;

            if (total == 0)
            {
                lblPreviewBadge.Text = "⚠️ يُرجى إدخال ألوان ومقاسات ليتم حساب تركيبة الأصناف.";
                lblPreviewBadge.BackColor = Color.FromArgb(254, 243, 199);
                lblPreviewBadge.ForeColor = Color.FromArgb(146, 64, 14);
                btnGenerate.Text = "🚀 توليد الأصناف والمصفوفة";
            }
            else
            {
                lblPreviewBadge.Text = $"⚡ سيتم توليد ({total}) صنف جديد للمصفوفة بباركودات مستقلة ({colorCount} ألوان × {sizeCount} مقاسات)";
                lblPreviewBadge.BackColor = Color.FromArgb(236, 253, 245);
                lblPreviewBadge.ForeColor = Color.FromArgb(6, 95, 70);
                btnGenerate.Text = $"🚀 توليد الأصناف والمصفوفة ({total} صنف)";
            }
        }

        private void LoadData()
        {
            try
            {
                // Load Categories
                cboCategory.Items.Clear();
                cboCategory.Items.Add(new ComboItem(0, "-- بدون تصنيف --"));
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
            catch { }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBaseName.Text))
            {
                MessageBox.Show("يرجى إدخال الاسم الأساسي للموديل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBaseName.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtModelCode.Text))
            {
                MessageBox.Show("يرجى إدخال كود الموديل الأساسي!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtModelCode.Focus();
                return;
            }

            // Parse colors & sizes
            string[] colors = txtColors.Text.Split(new[] { ',', ';', '،', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (colors.Length == 0) colors = new[] { "" };

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

                MessageBox.Show($"✅ تم بنجاح توليد ({generatedCount}) صنف جديد للمصفوفة بنجاح!", "اكتمل التوليد", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

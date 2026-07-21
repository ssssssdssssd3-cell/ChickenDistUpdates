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
        private TextBox txtCode, txtName, txtDescription, txtPartNumber, txtInternationalCode;
        private ComboBox txtCarModel, txtBrand, txtProducerCompany, txtShelfLocation;
        private ComboBox cboCategory, cboUnit;
        private Button btnAddUnit, btnAddBrand, btnAddCarModel, btnAddShelfLocation, btnAddProducerCompany;
        private NumericUpDown nudPrice, nudPurchasePrice, nudMinStockLimit, nudWholesalePrice, nudSemiWholesalePrice;
        private CheckBox chkActive, chkPrintLocalBarcode, chkIsService, chkIsQuickItem, chkHasExpiry;
        private NumericUpDown nudDefaultExpiryDays;
        private Button btnSave, btnCancel;
        private int _selectedID = 0;
        private bool _originalHasExpiry = false;

        // Multi-Unit Controls
        private TextBox txtUnit1Barcode;
        private ComboBox cboUnit1Name, cboUnit2Name, cboDefaultSaleUnit;
        private Button btnAddUnit1Name, btnAddUnit2Name;
        private NumericUpDown nudUnit1SalePrice, nudUnit1PurchasePrice;
        private TextBox txtUnit2Barcode;
        private NumericUpDown nudUnit2Factor, nudUnit2SalePrice, nudUnit2PurchasePrice;
        private NumericUpDown nudUnit3Factor;
        private Button btnUnit1MultiBarcode, btnUnit2MultiBarcode;
        private Label lblUnit1Header, lblUnit2Header;
        // flags لتتبع التعديل اليدوي على أسعار البيع الفرعية
        private bool _unit1SaleOverride = false;
        private bool _unit2SaleOverride = false;
        private bool _inRecalc = false; // منع التداخل

        public FrmProductCard(int id = 0)
        {
            _selectedID = id;
            InitUI();
            LoadCategoriesCombo();
            LoadUnitsCombos();
            LoadLookupCombos();
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
            this.Size = new Size(1300, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // GroupBox 1: البيانات الأساسية
            var grpBasic = new GroupBox
            {
                Text = "📝 البيانات الأساسية",
                Location = new Point(20, 15),
                Size = new Size(305, 430),
                ForeColor = Theme.Primary,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };
            this.Controls.Add(grpBasic);

            // GroupBox 2: الأسعار والصلاحية
            var grpPrice = new GroupBox
            {
                Text = "💰 الأسعار والصلاحية",
                Location = new Point(335, 15),
                Size = new Size(305, 430),
                ForeColor = Theme.Primary,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };
            this.Controls.Add(grpPrice);

            // GroupBox 3: تفاصيل أخرى والوسطى
            var grpUnit2 = new GroupBox
            {
                Text = "📦 الوحدة الوسطى والتفاصيل",
                Location = new Point(650, 15),
                Size = new Size(305, 430),
                ForeColor = Theme.Primary,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };
            this.Controls.Add(grpUnit2);

            // GroupBox 4: الوحدة الصغرى والربط
            var grpUnit1 = new GroupBox
            {
                Text = "🔍 الوحدة الصغرى والربط",
                Location = new Point(965, 15),
                Size = new Size(305, 430),
                ForeColor = Theme.Primary,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };
            this.Controls.Add(grpUnit1);

            // --- GroupBox 1 Content ---
            int ry = 25;
            AddField(grpBasic, "كود الصنف:", 10, ry, out txtCode);
            txtCode.ReadOnly = true;
            ry += 35;

            AddField(grpBasic, "اسم الصنف:", 10, ry, out txtName);
            ry += 35;

            grpBasic.Controls.Add(new Label { Text = "التصنيف:", Location = new Point(10 + 195, ry + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            cboCategory = new ComboBox { Location = new Point(10 + 35, ry), Width = 155, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontMain };
            var btnAddCat = new Button { Text = "➕", Location = new Point(10, ry), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnAddCat.Click += (s, e) => { new FrmCategories().ShowDialog(); LoadCategoriesCombo(); };
            grpBasic.Controls.AddRange(new Control[] { cboCategory, btnAddCat });
            ry += 35;

            AddLookupComboField(grpBasic, "الماركة:", 10, ry, out txtBrand, out btnAddBrand);
            btnAddBrand.Click += (s, e) => { new FrmLookupManager("Brands", "BrandID", "BrandCode", "BrandName", "BRD", "الماركات").ShowDialog(); LoadLookupCombos(); };
            ry += 35;

            AddLookupComboField(grpBasic, "الشركة المنتجة:", 10, ry, out txtProducerCompany, out btnAddProducerCompany);
            btnAddProducerCompany.Click += (s, e) => { new FrmLookupManager("ProducerCompanies", "ProducerID", "ProducerCode", "ProducerName", "PRD", "الشركات المنتجة").ShowDialog(); LoadLookupCombos(); };
            ry += 35;

            AddUnitComboField(grpBasic, "الوحدة الكبرى:", 10, ry, out cboUnit, out btnAddUnit);
            btnAddUnit.Click += (s, e) => { new FrmUnits().ShowDialog(); LoadUnitsCombos(); };
            ry += 35;

            chkActive = new CheckBox { Text = "صنف نشط", Location = new Point(20, ry), ForeColor = Theme.TextMain, Checked = true, AutoSize = true, Font = Theme.FontMain };
            grpBasic.Controls.Add(chkActive);
            ry += 28;

            chkPrintLocalBarcode = new CheckBox { Text = "طباعة باركود محلي", Location = new Point(20, ry), ForeColor = Theme.TextMain, Checked = false, AutoSize = true, Font = Theme.FontMain };
            grpBasic.Controls.Add(chkPrintLocalBarcode);
            ry += 28;

            chkIsService = new CheckBox { Text = "🔧 صنف خدمة (يُباع بالسالب)", Location = new Point(20, ry), ForeColor = Color.FromArgb(180, 120, 0), Checked = false, AutoSize = true, Font = new Font(Theme.FontMain, FontStyle.Bold) };
            grpBasic.Controls.Add(chkIsService);
            ry += 30;

            chkIsQuickItem = new CheckBox { Text = "⚡ صنف بيع سريع (يظهر في شاشة البيع السريع)", Location = new Point(20, ry), ForeColor = Color.FromArgb(0, 120, 180), Checked = false, AutoSize = true, Font = new Font(Theme.FontMain, FontStyle.Bold) };
            grpBasic.Controls.Add(chkIsQuickItem);
            ry += 30;

            grpBasic.Controls.Add(new Label { Text = "الوصف:", Location = new Point(10 + 195, ry + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtDescription = new TextBox { Location = new Point(10, ry), Width = 190, Height = 60, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
            grpBasic.Controls.Add(txtDescription);


            // --- GroupBox 2 Content ---
            AddNud(grpPrice, "شراء الكبرى:", 10, 25, out nudPurchasePrice, 2);
            AddNud(grpPrice, "بيع قطاعي الكبرى:", 10, 60, out nudPrice, 2);
            AddNud(grpPrice, "نصف جملة الكبرى:", 10, 95, out nudSemiWholesalePrice, 2);
            AddNud(grpPrice, "جملة الكبرى:", 10, 130, out nudWholesalePrice, 2);
            AddNud(grpPrice, "حد الطلب (صغرى):", 10, 165, out nudMinStockLimit, 3);

            string bizType = AppConfig.BusinessType;

            if (bizType == "SpareParts")
            {
                // 1. Show OEM, International Barcode, Shelf Location, Car Model in grpUnit2 (original parts layout)
                AddField(grpUnit2, "رقم OEM:", 10, 25, out txtPartNumber);
                
                grpUnit2.Controls.Add(new Label { Text = "الباركود الدولي:", Location = new Point(10 + 195, 60 + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
                txtInternationalCode = new TextBox { Location = new Point(10 + 35, 60), Width = 155, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
                var btnMultiBarcode = new Button { Text = "➕", Location = new Point(10, 60), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
                btnMultiBarcode.Click += BtnMultiBarcode_Click;
                grpUnit2.Controls.AddRange(new Control[] { txtInternationalCode, btnMultiBarcode });

                AddLookupComboField(grpUnit2, "موقع الرف:", 10, 95, out txtShelfLocation, out btnAddShelfLocation);
                btnAddShelfLocation.Click += (s, e) => { new FrmLookupManager("ShelfLocations", "ShelfID", "ShelfCode", "ShelfName", "SHF", "أماكن الرفوف").ShowDialog(); LoadLookupCombos(); };
                AddLookupComboField(grpUnit2, "الموديل المتوافق:", 10, 130, out txtCarModel, out btnAddCarModel);
                btnAddCarModel.Click += (s, e) => { new FrmLookupManager("CarModels", "CarModelID", "CarModelCode", "CarModelName", "MDL", "الموديلات").ShowDialog(); LoadLookupCombos(); };

                lblUnit2Header = new Label { Text = "⚙️ خانات الوحدة الوسطى:", Location = new Point(10, 175), Width = 280, Font = new Font(Theme.FontMain, FontStyle.Bold), ForeColor = Theme.Primary };
                grpUnit2.Controls.Add(lblUnit2Header);

                AddUnitComboField(grpUnit2, "اسم الوحدة الوسطى:", 10, 205, out cboUnit2Name, out btnAddUnit2Name);
                btnAddUnit2Name.Click += (s, e) => { new FrmUnits().ShowDialog(); LoadUnitsCombos(); };
                
                AddNud(grpUnit2, "تحتوي على كم صغرى؟:", 10, 240, out nudUnit2Factor, 0);

                grpUnit2.Controls.Add(new Label { Text = "باركود الوسطى:", Location = new Point(10 + 195, 275 + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
                txtUnit2Barcode = new TextBox { Location = new Point(10 + 35, 275), Width = 155, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
                btnUnit2MultiBarcode = new Button { Text = "➕", Location = new Point(10, 275), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
                btnUnit2MultiBarcode.Click += BtnUnit2MultiBarcode_Click;
                grpUnit2.Controls.AddRange(new Control[] { txtUnit2Barcode, btnUnit2MultiBarcode });

                AddNud(grpUnit2, "شراء الوسطى:", 10, 310, out nudUnit2PurchasePrice, 2);
                AddNud(grpUnit2, "بيع قطاعي الوسطى:", 10, 345, out nudUnit2SalePrice, 2);
                // زر إعادة حساب سعر الوسطى
                var btnResetU2a = new Button { Text = "🔄", Location = new Point(200, 348), Width = 26, Height = 20, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 160, 240), ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 7f) };
                btnResetU2a.Click += (s, e) => ResetUnit2SaleOverride();
                grpUnit2.Controls.Add(btnResetU2a);

                // Hide Expiry controls
                chkHasExpiry = new CheckBox { Visible = false };
                nudDefaultExpiryDays = new NumericUpDown { Visible = false };
            }
            else if (bizType == "Mobiles" || bizType == "Clothing")
            {
                // 2. Mobiles or Clothing Layout
                AddField(grpPrice, "الرقم التسلسلي:", 10, 205, out txtPartNumber);
                AddLookupComboField(grpPrice, "المواصفات:", 10, 240, out txtCarModel, out btnAddCarModel);
                btnAddCarModel.Click += (s, e) => { new FrmLookupManager("CarModels", "CarModelID", "CarModelCode", "CarModelName", "MDL", "المواصفات").ShowDialog(); LoadLookupCombos(); };
                AddLookupComboField(grpPrice, "موقع الرف:", 10, 275, out txtShelfLocation, out btnAddShelfLocation);
                btnAddShelfLocation.Click += (s, e) => { new FrmLookupManager("ShelfLocations", "ShelfID", "ShelfCode", "ShelfName", "SHF", "أماكن الرفوف").ShowDialog(); LoadLookupCombos(); };

                grpPrice.Controls.Add(new Label { Text = "الباركود الدولي:", Location = new Point(10 + 195, 310 + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
                txtInternationalCode = new TextBox { Location = new Point(10 + 35, 310), Width = 155, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
                var btnMultiBarcode = new Button { Text = "➕", Location = new Point(10, 310), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
                btnMultiBarcode.Click += BtnMultiBarcode_Click;
                grpPrice.Controls.AddRange(new Control[] { txtInternationalCode, btnMultiBarcode });

                // Reposition grpUnit2 controls to top
                lblUnit2Header = new Label { Text = "⚙️ خانات الوحدة الوسطى:", Location = new Point(10, 25), Width = 280, Font = new Font(Theme.FontMain, FontStyle.Bold), ForeColor = Theme.Primary };
                grpUnit2.Controls.Add(lblUnit2Header);

                AddUnitComboField(grpUnit2, "اسم الوحدة الوسطى:", 10, 55, out cboUnit2Name, out btnAddUnit2Name);
                btnAddUnit2Name.Click += (s, e) => { new FrmUnits().ShowDialog(); LoadUnitsCombos(); };
                
                AddNud(grpUnit2, "تحتوي على كم صغرى؟:", 10, 90, out nudUnit2Factor, 0);

                grpUnit2.Controls.Add(new Label { Text = "باركود الوسطى:", Location = new Point(10 + 195, 125 + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
                txtUnit2Barcode = new TextBox { Location = new Point(10 + 35, 125), Width = 155, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
                btnUnit2MultiBarcode = new Button { Text = "➕", Location = new Point(10, 125), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
                btnUnit2MultiBarcode.Click += BtnUnit2MultiBarcode_Click;
                grpUnit2.Controls.AddRange(new Control[] { txtUnit2Barcode, btnUnit2MultiBarcode });

                AddNud(grpUnit2, "شراء الوسطى:", 10, 160, out nudUnit2PurchasePrice, 2);
                AddNud(grpUnit2, "بيع قطاعي الوسطى:", 10, 195, out nudUnit2SalePrice, 2);
                // زر إعادة حساب سعر الوسطى
                var btnResetU2b = new Button { Text = "🔄", Location = new Point(200, 198), Width = 26, Height = 20, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 160, 240), ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 7f) };
                btnResetU2b.Click += (s, e) => ResetUnit2SaleOverride();
                grpUnit2.Controls.Add(btnResetU2b);

                // Hide Expiry controls
                chkHasExpiry = new CheckBox { Visible = false };
                nudDefaultExpiryDays = new NumericUpDown { Visible = false };
            }
            else
            {
                // 3. Supermarket or General Retail layout
                AddLookupComboField(grpPrice, "موقع الرف:", 10, 275, out txtShelfLocation, out btnAddShelfLocation);
                btnAddShelfLocation.Click += (s, e) => { new FrmLookupManager("ShelfLocations", "ShelfID", "ShelfCode", "ShelfName", "SHF", "أماكن الرفوف").ShowDialog(); LoadLookupCombos(); };

                grpPrice.Controls.Add(new Label { Text = "الباركود الدولي:", Location = new Point(10 + 195, 310 + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
                txtInternationalCode = new TextBox { Location = new Point(10 + 35, 310), Width = 155, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
                var btnMultiBarcode = new Button { Text = "➕", Location = new Point(10, 310), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
                btnMultiBarcode.Click += BtnMultiBarcode_Click;
                grpPrice.Controls.AddRange(new Control[] { txtInternationalCode, btnMultiBarcode });

                // Hidden Fields (OEM, CarModel)
                var pnlHidden = new Panel { Visible = false };
                AddField(pnlHidden, "رقم OEM:", 0, 0, out txtPartNumber);
                AddLookupComboField(pnlHidden, "الموديل المتوافق:", 0, 0, out txtCarModel, out btnAddCarModel);
                btnAddCarModel.Click += (s, e) => { new FrmLookupManager("CarModels", "CarModelID", "CarModelCode", "CarModelName", "MDL", "الموديلات").ShowDialog(); LoadLookupCombos(); };
                this.Controls.Add(pnlHidden);

                // Reposition grpUnit2 controls to top
                lblUnit2Header = new Label { Text = "⚙️ خانات الوحدة الوسطى:", Location = new Point(10, 25), Width = 280, Font = new Font(Theme.FontMain, FontStyle.Bold), ForeColor = Theme.Primary };
                grpUnit2.Controls.Add(lblUnit2Header);

                AddUnitComboField(grpUnit2, "اسم الوحدة الوسطى:", 10, 55, out cboUnit2Name, out btnAddUnit2Name);
                btnAddUnit2Name.Click += (s, e) => { new FrmUnits().ShowDialog(); LoadUnitsCombos(); };
                
                AddNud(grpUnit2, "تحتوي على كم صغرى؟:", 10, 90, out nudUnit2Factor, 0);

                grpUnit2.Controls.Add(new Label { Text = "باركود الوسطى:", Location = new Point(10 + 195, 125 + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
                txtUnit2Barcode = new TextBox { Location = new Point(10 + 35, 125), Width = 155, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
                btnUnit2MultiBarcode = new Button { Text = "➕", Location = new Point(10, 125), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
                btnUnit2MultiBarcode.Click += BtnUnit2MultiBarcode_Click;
                grpUnit2.Controls.AddRange(new Control[] { txtUnit2Barcode, btnUnit2MultiBarcode });

                AddNud(grpUnit2, "شراء الوسطى:", 10, 160, out nudUnit2PurchasePrice, 2);
                AddNud(grpUnit2, "بيع قطاعي الوسطى:", 10, 195, out nudUnit2SalePrice, 2);
                // زر إعادة حساب سعر الوسطى
                var btnResetU2c = new Button { Text = "🔄", Location = new Point(200, 198), Width = 26, Height = 20, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 160, 240), ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 7f) };
                btnResetU2c.Click += (s, e) => ResetUnit2SaleOverride();
                grpUnit2.Controls.Add(btnResetU2c);

                if (bizType == "General")
                {
                    // Hide Expiry controls
                    chkHasExpiry = new CheckBox { Visible = false };
                    nudDefaultExpiryDays = new NumericUpDown { Visible = false };
                }
                else
                {
                    chkHasExpiry = new CheckBox { Text = "له تاريخ صلاحية", Location = new Point(20, 205), ForeColor = Theme.TextMain, Checked = false, AutoSize = true, Font = Theme.FontMain };
                    grpPrice.Controls.Add(chkHasExpiry);

                    AddNud(grpPrice, "أيام الصلاحية الافتراضية:", 10, 235, out nudDefaultExpiryDays, 0);
                    nudDefaultExpiryDays.Maximum = 9999;
                    nudDefaultExpiryDays.Enabled = false;
                    chkHasExpiry.CheckedChanged += (s, e) => nudDefaultExpiryDays.Enabled = chkHasExpiry.Checked;
                }
            }

            // Apply custom labels depending on BusinessType
            if (bizType == "Mobiles")
            {
                SetFieldLabel(grpBasic, txtBrand, "الذاكرة / الرام:");
                SetFieldLabel(grpBasic, txtProducerCompany, "فترة الضمان:");
                SetFieldLabel(grpPrice, txtPartNumber, "الرقم التسلسلي (IMEI):");
                SetFieldLabel(grpPrice, txtCarModel, "اللون / الموديل:");
            }
            else if (bizType == "Clothing")
            {
                SetFieldLabel(grpBasic, txtBrand, "اللون (Color):");
                SetFieldLabel(grpBasic, txtProducerCompany, "الخامة (Material):");
                SetFieldLabel(grpPrice, txtPartNumber, "كود الموديل:");
                SetFieldLabel(grpPrice, txtCarModel, "المقاس (Size):");
            }
            else if (bizType == "SpareParts")
            {
                SetFieldLabel(grpBasic, txtBrand, "الماركة:");
                SetFieldLabel(grpBasic, txtProducerCompany, "الشركة المنتجة:");
                SetFieldLabel(grpUnit2, txtPartNumber, "رقم OEM:");
                SetFieldLabel(grpUnit2, txtCarModel, "الموديل المتوافق:");
            }

            // --- GroupBox 4 Content ---
            lblUnit1Header = new Label { Text = "⚙️ خانات الوحدة الصغرى (تجزئة):", Location = new Point(10, 25), Width = 280, Font = new Font(Theme.FontMain, FontStyle.Bold), ForeColor = Theme.Primary };
            grpUnit1.Controls.Add(lblUnit1Header);

            AddUnitComboField(grpUnit1, "اسم الوحدة الصغرى:", 10, 55, out cboUnit1Name, out btnAddUnit1Name);
            btnAddUnit1Name.Click += (s, e) => { new FrmUnits().ShowDialog(); LoadUnitsCombos(); };

            grpUnit1.Controls.Add(new Label { Text = "باركود الصغرى:", Location = new Point(10 + 195, 90 + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtUnit1Barcode = new TextBox { Location = new Point(10 + 35, 90), Width = 155, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
            btnUnit1MultiBarcode = new Button { Text = "➕", Location = new Point(10, 90), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnUnit1MultiBarcode.Click += BtnUnit1MultiBarcode_Click;
            grpUnit1.Controls.AddRange(new Control[] { txtUnit1Barcode, btnUnit1MultiBarcode });

            AddNud(grpUnit1, "شراء الصغرى:", 10, 125, out nudUnit1PurchasePrice, 2);
            AddNud(grpUnit1, "بيع قطاعي الصغرى:", 10, 160, out nudUnit1SalePrice, 2);
            // زر إعادة حساب سعر الصغرى
            var btnResetU1 = new Button { Text = "🔄", Location = new Point(200, 163), Width = 26, Height = 20, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 160, 240), ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 7f) };
            btnResetU1.Click += (s, e) => ResetUnit1SaleOverride();
            grpUnit1.Controls.Add(btnResetU1);

            var lblRelationHeader = new Label { Text = "⚙️ علاقة الوحدة الكبرى بالوسطى/الصغرى:", Location = new Point(10, 205), Width = 280, Font = new Font(Theme.FontMain, FontStyle.Bold), ForeColor = Theme.Primary };
            grpUnit1.Controls.Add(lblRelationHeader);

            AddNud(grpUnit1, "تحتوي كم وسطى/صغرى؟:", 10, 235, out nudUnit3Factor, 0);
            
            var lblNote = new Label { Text = "*(أو تحتوي كم صغرى مباشرة في حال عدم تفعيل الوحدة الوسطى)", Location = new Point(10, 270), Width = 280, Height = 50, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            grpUnit1.Controls.Add(lblNote);

            var lblDefaultSaleUnit = new Label { Text = "وحدة البيع الافتراضية:", Location = new Point(10 + 155, 325 + 3), Width = 140, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            cboDefaultSaleUnit = new ComboBox { Location = new Point(10, 325), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontMain };
            cboDefaultSaleUnit.Items.AddRange(new string[] { "الكبرى", "الوسطى", "الصغرى" });
            cboDefaultSaleUnit.SelectedIndex = 0; // Default is الكبرى
            grpUnit1.Controls.AddRange(new Control[] { lblDefaultSaleUnit, cboDefaultSaleUnit });

            // أسعار الشراء: للقراءة فقط (محسوبة)
            nudUnit2PurchasePrice.ReadOnly = true;
            nudUnit1PurchasePrice.ReadOnly = true;
            nudUnit2PurchasePrice.BackColor = SystemColors.Control;
            nudUnit1PurchasePrice.BackColor = SystemColors.Control;

            // أسعار البيع الفرعية: قابلة للتعديل، بخلفية مميزة
            nudUnit2SalePrice.ReadOnly = false;
            nudUnit1SalePrice.ReadOnly = false;
            nudUnit2SalePrice.BackColor = Color.FromArgb(255, 255, 220); // أصفر فاتح = قابل للتعديل
            nudUnit1SalePrice.BackColor = Color.FromArgb(255, 255, 220);

            // إضافة tooltip يوضح أن القيمة قابلة للتعديل اليدوي
            var toolTip = new ToolTip();
            toolTip.SetToolTip(nudUnit2SalePrice, "يُحسب تلقائياً من سعر الكبرى - يمكنك تعديله يدوياً");
            toolTip.SetToolTip(nudUnit1SalePrice, "يُحسب تلقائياً من سعر الكبرى - يمكنك تعديله يدوياً");

            // --- Footer Panel for Save / Cancel ---
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Theme.BgCard };
            btnSave = Theme.MakeButton("💾 حفظ", 660, 12, 160, 36, Theme.Accent);
            btnCancel = Theme.MakeButton("❌ إلغاء", 480, 12, 160, 36, Color.FromArgb(100, 110, 120));

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => this.Close();

            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Controls.Add(pnlFooter);

            // Bind Combo events to update Headers
            cboUnit2Name.SelectedIndexChanged += (s, e) => UpdateUnitHeaders();
            cboUnit2Name.TextChanged += (s, e) => UpdateUnitHeaders();
            cboUnit1Name.SelectedIndexChanged += (s, e) => UpdateUnitHeaders();
            cboUnit1Name.TextChanged += (s, e) => UpdateUnitHeaders();

            // كشف تعديل يدوي على أسعار البيع الفرعية
            nudUnit2SalePrice.ValueChanged += (s, e) => { if (!_inRecalc) { _unit2SaleOverride = true; nudUnit2SalePrice.BackColor = Color.FromArgb(200, 255, 200); } };
            nudUnit1SalePrice.ValueChanged += (s, e) => { if (!_inRecalc) { _unit1SaleOverride = true; nudUnit1SalePrice.BackColor = Color.FromArgb(200, 255, 200); } };

            // Bind events for live calculation of prices
            nudPrice.ValueChanged += (s, e) => RecalculateSubUnitPrices();
            nudPurchasePrice.ValueChanged += (s, e) => RecalculateSubUnitPrices();
            nudUnit3Factor.ValueChanged += (s, e) => RecalculateSubUnitPrices();
            nudUnit2Factor.ValueChanged += (s, e) => RecalculateSubUnitPrices();
            cboUnit2Name.SelectedIndexChanged += (s, e) => RecalculateSubUnitPrices();
            cboUnit2Name.TextChanged += (s, e) => RecalculateSubUnitPrices();
            cboUnit1Name.SelectedIndexChanged += (s, e) => RecalculateSubUnitPrices();
            cboUnit1Name.TextChanged += (s, e) => RecalculateSubUnitPrices();

            UpdateUnitHeaders();

            Theme.ApplyFormRTL(this);
        }

        private void RecalculateSubUnitPrices()
        {
            try
            {
                _inRecalc = true;

                decimal largeSale     = nudPrice.Value;
                decimal largePurchase = nudPurchasePrice.Value;

                string unit2 = cboUnit2Name.Text.Trim();
                string unit1 = cboUnit1Name.Text.Trim();

                decimal u3f = nudUnit3Factor.Value; // عدد المتوسطة في الكبرى
                if (u3f <= 0) u3f = 1;

                decimal u2f = nudUnit2Factor.Value; // عدد الصغرى في المتوسطة (أو في الكبرى مباشرة)
                if (u2f <= 0) u2f = 1;

                // ─────────────────────────────────────────────────────────────
                // حساب أسعار الشراء دائماً (للقراءة فقط)
                // حساب أسعار البيع فقط إذا لم يُعدّلها المستخدم يدوياً
                // ─────────────────────────────────────────────────────────────

                // 1. حالة وجود 3 وحدات (كبرى، متوسطة، صغرى)
                if (!string.IsNullOrEmpty(unit2) && !string.IsNullOrEmpty(unit1))
                {
                    decimal calcU2Sale = largeSale / u3f;
                    nudUnit2PurchasePrice.Value = largePurchase / u3f;

                    if (!_unit2SaleOverride)
                        nudUnit2SalePrice.Value = calcU2Sale;

                    decimal calcU1Sale = (!_unit2SaleOverride ? calcU2Sale : nudUnit2SalePrice.Value) / u2f;
                    nudUnit1PurchasePrice.Value = nudUnit2PurchasePrice.Value / u2f;

                    if (!_unit1SaleOverride)
                        nudUnit1SalePrice.Value = calcU1Sale;
                }
                // 2. حالة وجود وحدتين فقط (كبرى وصغرى) - بدون وحدة متوسطة
                else if (string.IsNullOrEmpty(unit2) && !string.IsNullOrEmpty(unit1))
                {
                    nudUnit2SalePrice.Value     = 0;
                    nudUnit2PurchasePrice.Value = 0;

                    nudUnit1PurchasePrice.Value = largePurchase / u2f;

                    if (!_unit1SaleOverride)
                        nudUnit1SalePrice.Value = largeSale / u2f;
                }
                else
                {
                    nudUnit2SalePrice.Value     = 0;
                    nudUnit2PurchasePrice.Value = 0;
                    nudUnit1SalePrice.Value     = 0;
                    nudUnit1PurchasePrice.Value = 0;
                }
            }
            catch { }
            finally { _inRecalc = false; }
        }

        /// <summary>إعادة ضبط تعديل سعر بيع الوسطى للقيمة المحسوبة من الكبرى</summary>
        private void ResetUnit2SaleOverride()
        {
            _unit2SaleOverride = false;
            nudUnit2SalePrice.BackColor = Color.FromArgb(255, 255, 220);
            RecalculateSubUnitPrices();
        }

        /// <summary>إعادة ضبط تعديل سعر بيع الصغرى للقيمة المحسوبة</summary>
        private void ResetUnit1SaleOverride()
        {
            _unit1SaleOverride = false;
            nudUnit1SalePrice.BackColor = Color.FromArgb(255, 255, 220);
            RecalculateSubUnitPrices();
        }

        private void AddField(Control parent, string label, int x, int y, out TextBox txt)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 195, y + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txt = new TextBox { Location = new Point(x, y), Width = 190, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
            parent.Controls.Add(txt);
        }

        private void AddUnitComboField(Control parent, string label, int x, int y, out ComboBox cbo, out Button btnAdd)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 195, y + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            cbo = new ComboBox { Location = new Point(x + 35, y), Width = 155, DropDownStyle = ComboBoxStyle.DropDown, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontMain };
            btnAdd = new Button { Text = "➕", Location = new Point(x, y), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            parent.Controls.AddRange(new Control[] { cbo, btnAdd });
        }

        private void AddLookupComboField(Control parent, string label, int x, int y, out ComboBox cbo, out Button btnAdd)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 195, y + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            cbo = new ComboBox { Location = new Point(x + 35, y), Width = 155, DropDownStyle = ComboBoxStyle.DropDown, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Font = Theme.FontMain };
            btnAdd = new Button { Text = "➕", Location = new Point(x, y), Width = 30, Height = 23, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Cursor = Cursors.Hand };
            parent.Controls.AddRange(new Control[] { cbo, btnAdd });
        }

        private void LoadLookupCombos()
        {
            try
            {
                // Brands
                string brandVal = txtBrand != null ? txtBrand.Text : "";
                if (txtBrand != null)
                {
                    txtBrand.Items.Clear();
                    var dtBrands = LookupDAL.GetAll("Brands", "BrandName");
                    foreach (DataRow r in dtBrands.Rows) txtBrand.Items.Add(r["BrandName"].ToString());
                    txtBrand.Text = brandVal;
                }

                // CarModels
                string modelVal = txtCarModel != null ? txtCarModel.Text : "";
                if (txtCarModel != null)
                {
                    txtCarModel.Items.Clear();
                    var dtModels = LookupDAL.GetAll("CarModels", "CarModelName");
                    foreach (DataRow r in dtModels.Rows) txtCarModel.Items.Add(r["CarModelName"].ToString());
                    txtCarModel.Text = modelVal;
                }

                // ShelfLocations
                string shelfVal = txtShelfLocation != null ? txtShelfLocation.Text : "";
                if (txtShelfLocation != null)
                {
                    txtShelfLocation.Items.Clear();
                    var dtShelves = LookupDAL.GetAll("ShelfLocations", "ShelfName");
                    foreach (DataRow r in dtShelves.Rows) txtShelfLocation.Items.Add(r["ShelfName"].ToString());
                    txtShelfLocation.Text = shelfVal;
                }

                // ProducerCompanies
                string producerVal = txtProducerCompany != null ? txtProducerCompany.Text : "";
                if (txtProducerCompany != null)
                {
                    txtProducerCompany.Items.Clear();
                    var dtProducers = LookupDAL.GetAll("ProducerCompanies", "ProducerName");
                    foreach (DataRow r in dtProducers.Rows) txtProducerCompany.Items.Add(r["ProducerName"].ToString());
                    txtProducerCompany.Text = producerVal;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadLookupCombos failed", ex);
            }
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
            parent.Controls.Add(new Label { Text = label, Location = new Point(x + 195, y + 3), Width = 100, AutoSize = false, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            nud = new NumericUpDown { Location = new Point(x, y), Width = 190, Minimum = 0, Maximum = 9999999, DecimalPlaces = decimals, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
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
            txtProducerCompany.Text = dr.Table.Columns.Contains("ProducerCompany") && dr["ProducerCompany"] != DBNull.Value ? dr["ProducerCompany"].ToString() : "";
            txtShelfLocation.Text = dr["ShelfLocation"] != DBNull.Value ? dr["ShelfLocation"].ToString() : "";
            cboUnit.Text = dr["Unit"].ToString();
            nudPurchasePrice.Value = Convert.ToDecimal(dr["PurchasePrice"] == DBNull.Value ? 0 : dr["PurchasePrice"]);
            nudPrice.Value = Convert.ToDecimal(dr["SalePrice"]);
            nudWholesalePrice.Value = Convert.ToDecimal(dr.Table.Columns.Contains("WholesalePrice") && dr["WholesalePrice"] != DBNull.Value ? dr["WholesalePrice"] : 0);
            nudSemiWholesalePrice.Value = Convert.ToDecimal(dr.Table.Columns.Contains("SemiWholesalePrice") && dr["SemiWholesalePrice"] != DBNull.Value ? dr["SemiWholesalePrice"] : 0);
            nudMinStockLimit.Value = Convert.ToDecimal(dr["MinStockLimit"] == DBNull.Value ? 0 : dr["MinStockLimit"]);
            txtDescription.Text = dr["Description"].ToString();
            chkActive.Checked = Convert.ToBoolean(dr["IsActive"]);
            chkPrintLocalBarcode.Checked = dr.Table.Columns.Contains("PrintLocalBarcode") && dr["PrintLocalBarcode"] != DBNull.Value ? Convert.ToBoolean(dr["PrintLocalBarcode"]) : false;
            chkIsService.Checked = dr.Table.Columns.Contains("IsService") && dr["IsService"] != DBNull.Value ? Convert.ToBoolean(dr["IsService"]) : false;
            chkIsQuickItem.Checked = dr.Table.Columns.Contains("IsQuickItem") && dr["IsQuickItem"] != DBNull.Value ? Convert.ToBoolean(dr["IsQuickItem"]) : false;
            _originalHasExpiry = dr.Table.Columns.Contains("HasExpiry") && dr["HasExpiry"] != DBNull.Value ? Convert.ToBoolean(dr["HasExpiry"]) : false;
            chkHasExpiry.Checked = _originalHasExpiry;
            nudDefaultExpiryDays.Value = dr.Table.Columns.Contains("DefaultExpiryDays") && dr["DefaultExpiryDays"] != DBNull.Value ? Convert.ToDecimal(dr["DefaultExpiryDays"]) : 0m;
            nudDefaultExpiryDays.Enabled = chkHasExpiry.Checked;

            if (chkPrintLocalBarcode.Checked)
            {
                if (int.TryParse(txtCode.Text, out int codeVal))
                {
                    txtCode.Text = codeVal.ToString("D8");
                }
            }

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

            // Default sale unit loading
            string dsu = dr.Table.Columns.Contains("DefaultSaleUnit") && dr["DefaultSaleUnit"] != DBNull.Value ? dr["DefaultSaleUnit"].ToString() : "";
            if (cboDefaultSaleUnit != null)
            {
                if (dsu == "الوسطى") cboDefaultSaleUnit.SelectedIndex = 1;
                else if (dsu == "الصغرى") cboDefaultSaleUnit.SelectedIndex = 2;
                else cboDefaultSaleUnit.SelectedIndex = 0;
            }

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
            
            RecalculateSubUnitPrices();
            UpdateUnitHeaders();
        }

        private void ClearDetail()
        {
            txtCode.Text = ProductDAL.GetNextProductCode();
            txtName.Clear();
            txtPartNumber.Clear();
            txtInternationalCode.Clear();
            txtCarModel.Text = "";
            txtBrand.Text = "";
            txtProducerCompany.Text = "";
            txtShelfLocation.Text = "";
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
            cboUnit.Text = "كرتونة";
            nudPurchasePrice.Value = 0;
            nudPrice.Value = 0;
            nudWholesalePrice.Value = 0;
            nudSemiWholesalePrice.Value = 0;
            nudMinStockLimit.Value = 0;
            txtDescription.Clear();
            chkActive.Checked = true;
            chkPrintLocalBarcode.Checked = false;
            chkIsService.Checked = false;
            chkIsQuickItem.Checked = false;
            chkHasExpiry.Checked = false;
            nudDefaultExpiryDays.Value = 0;
            nudDefaultExpiryDays.Enabled = false;

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
            if (cboDefaultSaleUnit != null) cboDefaultSaleUnit.SelectedIndex = 0;
            
            RecalculateSubUnitPrices();
            UpdateUnitHeaders();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم الصنف"); return; }

            if (chkHasExpiry.Checked && !_originalHasExpiry && _selectedID > 0)
            {
                decimal totalQty = GetProductTotalStock(_selectedID);
                if (totalQty > 0)
                {
                    MessageBox.Show("❌ عجز: هذا الصنف له رصيد حالي في المخازن يبلغ (" + totalQty.ToString("G29") + ").\nيجب تصفير كمية هذا الصنف في تسوية الجرد أولاً قبل تفعيل خيار تاريخ الصلاحية!", "تنبيه الرصيد الحالي", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkHasExpiry.Checked = false;
                    return;
                }
            }

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

            // Auto-save lookup values if new
            if (!string.IsNullOrWhiteSpace(txtBrand.Text.Trim()))
                LookupDAL.Save("Brands", "BrandID", "BrandCode", "BrandName", "BRD", 0, txtBrand.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtCarModel.Text.Trim()))
                LookupDAL.Save("CarModels", "CarModelID", "CarModelCode", "CarModelName", "MDL", 0, txtCarModel.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtShelfLocation.Text.Trim()))
                LookupDAL.Save("ShelfLocations", "ShelfID", "ShelfCode", "ShelfName", "SHF", 0, txtShelfLocation.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtProducerCompany.Text.Trim()))
                LookupDAL.Save("ProducerCompanies", "ProducerID", "ProducerCode", "ProducerName", "PRD", 0, txtProducerCompany.Text.Trim());

            // Reload combos
            LoadLookupCombos();

            // Recalculate prices one last time to be safe before saving
            RecalculateSubUnitPrices();

            string prodCode = txtCode.Text.Trim();
            if (chkPrintLocalBarcode.Checked)
            {
                if (int.TryParse(prodCode, out int codeVal))
                {
                    prodCode = codeVal.ToString("D8");
                }
            }

            // الحفظ في قاعدة البيانات
            int id = ProductDAL.Save(_selectedID, prodCode, txtName.Text, cboUnit.Text.Trim(), nudPrice.Value, chkActive.Checked,
                nudPurchasePrice.Value, nudMinStockLimit.Value, txtDescription.Text,
                txtPartNumber.Text.Trim(), categoryID, txtCarModel.Text.Trim(), txtBrand.Text.Trim(), txtShelfLocation.Text.Trim(),
                nudWholesalePrice.Value, nudSemiWholesalePrice.Value, normalisedIntlBarcodes, chkPrintLocalBarcode.Checked,
                chkIsService.Checked,
                cboUnit1Name.Text.Trim(), normalisedU1Barcode, nudUnit1SalePrice.Value, nudUnit1PurchasePrice.Value,
                cboUnit2Name.Text.Trim(), nudUnit2Factor.Value > 0 ? (decimal?)nudUnit2Factor.Value : null, normalisedU2Barcode, nudUnit2SalePrice.Value, nudUnit2PurchasePrice.Value,
                nudUnit3Factor.Value > 0 ? (decimal?)nudUnit3Factor.Value : null, chkIsQuickItem.Checked, txtProducerCompany.Text.Trim(),
                chkHasExpiry.Checked, chkHasExpiry.Checked ? (int?)nudDefaultExpiryDays.Value : null, cboDefaultSaleUnit.Text);

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

        private decimal GetProductTotalStock(int productId)
        {
            try
            {
                var dt = DbHelper.Query("SELECT ISNULL(SUM(Quantity), 0) FROM ProductStock WHERE ProductID = @pid", DbHelper.P("@pid", productId));
                if (dt.Rows.Count > 0)
                {
                    return Convert.ToDecimal(dt.Rows[0][0]);
                }
            }
            catch { }
            return 0;
        }

        private void SetFieldLabel(Control parent, Control targetCtrl, string newLabelText)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is Label lbl && Math.Abs(lbl.Location.Y - (targetCtrl.Location.Y + 3)) < 5 && lbl.Location.X > targetCtrl.Location.X)
                {
                    lbl.Text = newLabelText;
                    break;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة ذكية للغاية لاستيراد الأصناف من ملف Excel مع إمكانية مطابقة الأعمدة ديناميكياً
    /// </summary>
    public class FrmImportProducts : Form
    {
        private Panel pnlHeader;
        private Label lblTitle, lblDesc;
        private DataGridView dgPreview;
        private ComboBox cboStockPolicy;
        private Button btnBrowse, btnCopyTemplate, btnImport, btnCancel, btnPreview;
        private GroupBox grpMapping;
        private Label lblStats;

        private List<ParsedProductRow> _parsedRows = new List<ParsedProductRow>();
        private Dictionary<string, int> _warehouseMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, ComboBox> _mappings = new Dictionary<string, ComboBox>(StringComparer.OrdinalIgnoreCase);
        private List<string[]> _allLines = null;

        public FrmImportProducts()
        {
            InitializeComponent();
            LoadWarehouses();
        }

        private void InitializeComponent()
        {
            this.Text = "📥 استيراد الأصناف من ملف Excel";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // شريط العنوان
            pnlHeader = Theme.MakeTitleBar("📥 استيراد الأصناف والبيانات الافتتاحية", 
                "استيراد الأصناف والأسعار والأرصدة من ملف Excel (.xlsx) مع مطابقة الأعمدة ديناميكياً وتفادي التكرار.");
            this.Controls.Add(pnlHeader);

            // لوحة التحكم العلوية
            var pnlTopControls = new Panel
            {
                Location = new Point(20, 85),
                Size = new Size(1045, 65),
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            btnBrowse = Theme.MakeButton("📂 اختيار ملف Excel", 835, 12, 190, 36, Theme.Accent);
            btnBrowse.Click += BtnBrowse_Click;
            pnlTopControls.Controls.Add(btnBrowse);

            btnCopyTemplate = Theme.MakeButton("📋 نسخ قالب Excel", 665, 12, 160, 36, Color.FromArgb(70, 80, 95));
            btnCopyTemplate.Click += BtnCopyTemplate_Click;
            pnlTopControls.Controls.Add(btnCopyTemplate);

            var lblPolicy = new Label
            {
                Text = "سياسة أرصدة الأصناف الموجودة مسبقاً:",
                Location = new Point(310, 20),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            pnlTopControls.Controls.Add(lblPolicy);

            cboStockPolicy = new ComboBox
            {
                Location = new Point(15, 16),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboStockPolicy.Items.AddRange(new object[]
            {
                "تجاهل رصيد الاستيراد (الحفاظ على الأرصدة الحالية)",
                "جمع الرصيد المستورد مع الرصيد الحالي للمخزن",
                "استبدال الرصيد الحالي بالرصيد المستورد (تسوية جردية)"
            });
            cboStockPolicy.SelectedIndex = 0;
            pnlTopControls.Controls.Add(cboStockPolicy);

            this.Controls.Add(pnlTopControls);

            // لوحة مطابقة الأعمدة
            grpMapping = new GroupBox
            {
                Text = "⚙️ مطابقة أعمدة ملف Excel / Column Mapping",
                Location = new Point(20, 155),
                Size = new Size(1045, 175),
                ForeColor = Theme.TextMain,
                Enabled = false
            };

            AddMappingField("ProductCode", "كود الصنف:", 0, 0);
            AddMappingField("ProductName", "اسم الصنف (*):", 0, 1);
            AddMappingField("PartNumber", "رقم القطعة (OEM):", 0, 2);
            AddMappingField("CategoryName", "التصنيف / القسم:", 0, 3);
            AddMappingField("Unit", "الوحدة الأساسية:", 0, 4);

            AddMappingField("PurchasePrice", "سعر الشراء:", 1, 0);
            AddMappingField("SalePrice", "سعر البيع قطاعي:", 1, 1);
            AddMappingField("SemiWholesalePrice", "سعر نصف جملة:", 1, 2);
            AddMappingField("WholesalePrice", "سعر الجملة:", 1, 3);
            AddMappingField("MinStockLimit", "حد الطلب الأدنى:", 1, 4);

            AddMappingField("InitialStock", "الرصيد الافتتاحي:", 2, 0);
            AddMappingField("WarehouseName", "المخزن المستهدف:", 2, 1);
            AddMappingField("CarModel", "الموديل المتوافق:", 2, 2);
            AddMappingField("Brand", "الماركة / الشركة:", 2, 3);
            AddMappingField("ShelfLocation", "موقع الرف:", 2, 4);

            this.Controls.Add(grpMapping);

            // زر المعاينة والإحصائيات
            lblStats = new Label
            {
                Text = "يرجى اختيار ملف Excel (.xlsx) للبدء بالمعاينة والمطابقة...",
                Location = new Point(20, 340),
                Size = new Size(830, 24),
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            this.Controls.Add(lblStats);

            btnPreview = Theme.MakeButton("👁️ معاينة ومطابقة البيانات", 865, 335, 200, 32, Theme.Primary);
            btnPreview.Enabled = false;
            btnPreview.Click += BtnPreview_Click;
            this.Controls.Add(btnPreview);

            // جدول المعاينة
            dgPreview = new DataGridView
            {
                Location = new Point(20, 375),
                Size = new Size(1045, 220),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeight = 35,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };

            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColStatus", HeaderText = "الحالة والمطابقة", FillWeight = 95 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCode", HeaderText = "كود الصنف", FillWeight = 75 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPart", HeaderText = "رقم القطعة", FillWeight = 80 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "اسم الصنف", FillWeight = 160 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCategory", HeaderText = "التصنيف", FillWeight = 80 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPurchasePrice", HeaderText = "سعر الشراء", FillWeight = 70 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSalePrice", HeaderText = "سعر البيع", FillWeight = 70 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColImportStock", HeaderText = "الرصيد المستورد", FillWeight = 80 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCurrentStock", HeaderText = "الرصيد الحالي", FillWeight = 80 });
            dgPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColWarehouse", HeaderText = "المخزن المستهدف", FillWeight = 90 });

            this.Controls.Add(dgPreview);

            // الأزرار بالأسفل
            int yBottom = 605;

            btnImport = Theme.MakeButton("📥 بدء الاستيراد", 725, yBottom, 160, 40, Theme.Success);
            btnImport.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnImport.Enabled = false;
            btnImport.Click += BtnImport_Click;
            this.Controls.Add(btnImport);

            btnCancel = Theme.MakeButton("إلغاء ↩", 905, yBottom, 160, 40, Color.FromArgb(70, 80, 95));
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            Theme.ApplyFormRTL(this);
        }

        private void AddMappingField(string key, string arabicLabel, int colIdx, int rowIdx)
        {
            int colX = colIdx == 0 ? 20 : colIdx == 1 ? 360 : 700;
            int y = 25 + rowIdx * 28;

            var lbl = new Label
            {
                Text = arabicLabel,
                Location = new Point(colX + 210, y + 3),
                Size = new Size(110, 22),
                ForeColor = Theme.TextMain,
                TextAlign = ContentAlignment.MiddleRight
            };

            var cbo = new ComboBox
            {
                Location = new Point(colX, y),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };

            grpMapping.Controls.Add(lbl);
            grpMapping.Controls.Add(cbo);
            _mappings[key] = cbo;
        }

        private void LoadWarehouses()
        {
            _warehouseMap.Clear();
            var dt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive=1");
            foreach (DataRow r in dt.Rows)
            {
                _warehouseMap[r["WarehouseName"].ToString()] = (int)r["WarehouseID"];
            }
        }

        private void PopulateMappings(string[] headers)
        {
            foreach (var kv in _mappings)
            {
                ComboBox cbo = kv.Value;
                cbo.Items.Clear();
                cbo.Items.Add("[غير حدد]");

                if (kv.Key == "ProductCode")
                {
                    cbo.Items.Add("[توليد تلقائي / AUTO]");
                }

                foreach (string header in headers)
                {
                    cbo.Items.Add(header);
                }

                cbo.SelectedIndex = 0; // Default to unmapped
            }
        }

        private void AutoMatchColumns(string[] headers)
        {
            var ruleMap = new Dictionary<string, string[]>
            {
                { "ProductCode", new[] { "كود", "code", "barcode", "الباركود", "الصنف كود", "رقم الصنف", "رقم_الصنف", "كود_الصنف" } },
                { "ProductName", new[] { "اسم", "name", "product", "item", "اسم الصنف", "اسم_الصنف", "الاسم" } },
                { "PartNumber", new[] { "قطعة", "part", "oem", "رقم القطعة", "رقم_القطعة", "oem_number", "part_number", "رقم قطعة" } },
                { "CategoryName", new[] { "تصنيف", "فئة", "قسم", "category", "group", "القسم", "الفئة" } },
                { "CarModel", new[] { "موديل", "سيارة", "model", "car", "الموديل", "الموديل المتوافق" } },
                { "Brand", new[] { "ماركة", "شركة", "براند", "brand", "make", "الماركة" } },
                { "ShelfLocation", new[] { "رف", "موقع", "مكان", "shelf", "location", "موقع الرف", "رقم الرف", "الرف" } },
                { "Unit", new[] { "وحدة", "unit", "الوحدة" } },
                { "PurchasePrice", new[] { "شراء", "تكلفة", "cost", "purchase", "سعر الشراء", "سعر_الشراء", "سعر التكلفة" } },
                { "SalePrice", new[] { "قطاعي", "بيع", "سعر", "price", "sale", "سعر البيع", "سعر_البيع", "سعر القطاعي" } },
                { "SemiWholesalePrice", new[] { "نصف جملة", "نصف_جملة", "semi", "semi_wholesale", "سعر نصف جملة" } },
                { "WholesalePrice", new[] { "جملة", "wholesale", "سعر الجملة" } },
                { "MinStockLimit", new[] { "حد الطلب", "الطلب", "min", "limit", "minimum", "حد_الطلب" } },
                { "InitialStock", new[] { "افتتاحي", "رصيد", "كمية", "qty", "quantity", "stock", "initial", "الرصيد الافتتاحي" } },
                { "WarehouseName", new[] { "مخزن", "مستودع", "warehouse", "store", "المخزن" } }
            };

            foreach (var kv in _mappings)
            {
                ComboBox cbo = kv.Value;
                string fieldKey = kv.Key;
                string[] keywords = ruleMap[fieldKey];

                int matchedIndex = -1; // Default to [غير محدد]
                if (fieldKey == "ProductCode") matchedIndex = -2; // Default to [توليد تلقائي]

                for (int i = 0; i < headers.Length; i++)
                {
                    string header = headers[i].Trim().ToLowerInvariant();
                    foreach (string keyword in keywords)
                    {
                        if (header.Contains(keyword.ToLowerInvariant()))
                        {
                            if (fieldKey == "ProductCode")
                                matchedIndex = i + 2;
                            else
                                matchedIndex = i + 1;
                            break;
                        }
                    }
                    if (matchedIndex >= 0 || (fieldKey == "ProductCode" && matchedIndex != -2))
                        break; 
                }

                if (fieldKey == "ProductCode")
                {
                    if (matchedIndex == -2) cbo.SelectedIndex = 1; // [توليد تلقائي]
                    else if (matchedIndex == -1) cbo.SelectedIndex = 0; // [غير حدد]
                    else cbo.SelectedIndex = matchedIndex;
                }
                else
                {
                    if (matchedIndex == -1) cbo.SelectedIndex = 0; // [غير حدد]
                    else cbo.SelectedIndex = matchedIndex;
                }
            }
        }

        private void BtnCopyTemplate_Click(object sender, EventArgs e)
        {
            string template = "كود_الصنف\tاسم_الصنف\tرقم_القطعة\tالتصنيف\tالموديل\tالماركة\tموقع_الرف\tالوحدة\tسعر_الشراء\tسعر_قطاعي\tسعر_نصف_جملة\tسعر_جملة\tحد_الطلب\tالرصيد_الافتتاحي\tالمخزن\r\n" +
                              "A101\tفلتر زيت تويوتا كورولا\t90915-10001\tفلتر\tCorolla 2018\tToyota\tالرف A1\tحبة\t120.00\t180.00\t165.00\t150.00\t5\t50\tالمخزن الرئيسي\r\n" +
                              "B202\tبوجيهات ليزر ان جي كي\tIZFR6K11\tكهرباء\tCivic 2012\tNGK\tالرف B3\tطقم\t450.00\t600.00\t550.00\t500.00\t2\t20\tالمخزن الرئيسي";
            
            try
            {
                Clipboard.SetText(template);
                MessageBox.Show("✅ تم نسخ هيكل قالب Excel إلى الحافظة!\nيمكنك لصقه مباشرة (Paste) داخل ملف Excel وسينقسم إلى أعمدة تلقائياً.",
                    "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information, 
                    MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل النسخ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "اختر ملف الأصناف Excel";
                dlg.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _allLines = XlsxParser.Parse(dlg.FileName);
                        if (_allLines == null || _allLines.Count == 0)
                        {
                            MessageBox.Show("الملف فارغ أو لم نتمكن من قراءته.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        string[] headers = _allLines[0];
                        PopulateMappings(headers);
                        AutoMatchColumns(headers);

                        grpMapping.Enabled = true;
                        btnPreview.Enabled = true;
                        btnImport.Enabled = false;

                        lblStats.Text = $"📂 تم تحميل الملف بنجاح! يحتوي على {headers.Length} عمود و {_allLines.Count - 1} صف. يرجى مراجعة وتأكيد مطابقة الأعمدة بالأسفل.";
                        lblStats.ForeColor = Theme.Success;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل قراءة ملف Excel:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (_allLines == null || _allLines.Count <= 1)
            {
                MessageBox.Show("يرجى اختيار ملف Excel يحتوي على بيانات أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. التحقق من ربط اسم الصنف
            ComboBox cboName = _mappings["ProductName"];
            if (cboName.SelectedIndex <= 0) // [غير حدد]
            {
                MessageBox.Show("❌ يجب تحديد العمود المقابل لـ (اسم الصنف) على الأقل للاستيراد.", "خطأ مطابقة", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2. التحقق من تكرار الأعمدة المربوطة
            var mappedIndices = new Dictionary<int, string>();
            foreach (var kv in _mappings)
            {
                ComboBox cbo = kv.Value;
                string fieldKey = kv.Key;

                int colIdx = -1;
                if (fieldKey == "ProductCode")
                {
                    if (cbo.SelectedIndex == 1) colIdx = -2; // AUTO
                    else if (cbo.SelectedIndex > 1) colIdx = cbo.SelectedIndex - 2;
                }
                else
                {
                    if (cbo.SelectedIndex > 0) colIdx = cbo.SelectedIndex - 1;
                }

                if (colIdx >= 0)
                {
                    if (mappedIndices.TryGetValue(colIdx, out string existingField))
                    {
                        string friendlyExisting = GetFriendlyName(existingField);
                        string friendlyCurrent = GetFriendlyName(fieldKey);
                        MessageBox.Show($"❌ لا يمكن مطابقة نفس العمود بالملف لأكثر من حقل!\nالعمود '{cbo.SelectedItem}' تم ربطه بكل من '{friendlyExisting}' و '{friendlyCurrent}'.", "تضارب مطابقة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    mappedIndices[colIdx] = fieldKey;
                }
            }

            // 3. قراءة البيانات وبناء المعاينة
            try
            {
                _parsedRows.Clear();
                dgPreview.Rows.Clear();

                var codeMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var partMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var nameMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                var dtExist = DbHelper.Query("SELECT ProductID, ProductCode, PartNumber, ProductName FROM Products");
                foreach (DataRow r in dtExist.Rows)
                {
                    string code = r["ProductCode"].ToString().Trim();
                    string part = r["PartNumber"] != DBNull.Value ? r["PartNumber"].ToString().Trim() : "";
                    string name = r["ProductName"].ToString().Trim();

                    if (!string.IsNullOrEmpty(code) && !codeMap.ContainsKey(code))
                        codeMap[code] = (int)r["ProductID"];
                    if (!string.IsNullOrEmpty(part) && !partMap.ContainsKey(part))
                        partMap[part] = (int)r["ProductID"];
                    
                    string normName = Normalize(name);
                    if (!string.IsNullOrEmpty(normName) && !nameMap.ContainsKey(normName))
                        nameMap[normName] = (int)r["ProductID"];
                }

                int newCount = 0;
                int updateCount = 0;

                for (int idx = 1; idx < _allLines.Count; idx++)
                {
                    string[] cols = _allLines[idx];
                    if (cols == null || cols.Length == 0) continue;

                    string GetValue(string key)
                    {
                        ComboBox cbo = _mappings[key];
                        int colIdx = -1;
                        if (key == "ProductCode")
                        {
                            if (cbo.SelectedIndex == 1) colIdx = -2; // AUTO
                            else if (cbo.SelectedIndex > 1) colIdx = cbo.SelectedIndex - 2;
                        }
                        else
                        {
                            if (cbo.SelectedIndex > 0) colIdx = cbo.SelectedIndex - 1;
                        }

                        if (colIdx == -2) return "AUTO";
                        if (colIdx < 0 || colIdx >= cols.Length) return "";
                        return (cols[colIdx] ?? "").Trim();
                    }

                    string name = GetValue("ProductName");
                    if (string.IsNullOrEmpty(name)) continue; 

                    string code = GetValue("ProductCode");
                    string part = GetValue("PartNumber");
                    string category = GetValue("CategoryName");
                    string model = GetValue("CarModel");
                    string brand = GetValue("Brand");
                    string shelf = GetValue("ShelfLocation");
                    string unit = GetValue("Unit");
                    if (string.IsNullOrEmpty(unit)) unit = "قطعة";

                    decimal purchasePrice = 0, salePrice = 0, semiWholesale = 0, wholesale = 0, minStock = 0, initialStock = 0;
                    decimal.TryParse(GetValue("PurchasePrice"), out purchasePrice);
                    decimal.TryParse(GetValue("SalePrice"), out salePrice);
                    decimal.TryParse(GetValue("SemiWholesalePrice"), out semiWholesale);
                    decimal.TryParse(GetValue("WholesalePrice"), out wholesale);
                    decimal.TryParse(GetValue("MinStockLimit"), out minStock);
                    decimal.TryParse(GetValue("InitialStock"), out initialStock);

                    string warehouseName = GetValue("WarehouseName");
                    if (string.IsNullOrEmpty(warehouseName)) warehouseName = "المخزن الرئيسي";

                    int matchedProductID = 0;
                    bool isExisting = false;

                    if (!string.IsNullOrEmpty(code) && code != "AUTO" && codeMap.TryGetValue(code, out int idByCode))
                    {
                        matchedProductID = idByCode;
                        isExisting = true;
                    }
                    else if (!string.IsNullOrEmpty(part) && partMap.TryGetValue(part, out int idByPart))
                    {
                        matchedProductID = idByPart;
                        isExisting = true;
                    }
                    else if (nameMap.TryGetValue(Normalize(name), out int idByName))
                    {
                        matchedProductID = idByName;
                        isExisting = true;
                    }

                    decimal currentStock = 0;
                    if (isExisting)
                    {
                        int? whId = null;
                        if (_warehouseMap.TryGetValue(warehouseName, out int wID)) whId = wID;
                        currentStock = InventoryDAL.GetProductStock(matchedProductID, whId);
                    }

                    var row = new ParsedProductRow
                    {
                        ProductID = matchedProductID,
                        IsExisting = isExisting,
                        ProductCode = string.IsNullOrEmpty(code) ? "AUTO" : code,
                        ProductName = name,
                        PartNumber = part,
                        CategoryName = category,
                        CarModel = model,
                        Brand = brand,
                        ShelfLocation = shelf,
                        Unit = unit,
                        PurchasePrice = purchasePrice,
                        SalePrice = salePrice,
                        SemiWholesalePrice = semiWholesale,
                        WholesalePrice = wholesale,
                        MinStockLimit = minStock,
                        InitialStock = initialStock,
                        WarehouseName = warehouseName,
                        CurrentStock = currentStock
                    };

                    _parsedRows.Add(row);

                    string statusText = isExisting ? "⚠️ تعديل صنف موجود" : "🆕 صنف جديد";
                    int gridIdx = dgPreview.Rows.Add(
                        statusText,
                        row.ProductCode,
                        row.PartNumber,
                        row.ProductName,
                        row.CategoryName,
                        row.PurchasePrice.ToString("N2"),
                        row.SalePrice.ToString("N2"),
                        row.InitialStock.ToString("N2"),
                        isExisting ? row.CurrentStock.ToString("N2") : "0.00",
                        row.WarehouseName
                    );

                    if (isExisting)
                    {
                        dgPreview.Rows[gridIdx].DefaultCellStyle.BackColor = Color.FromArgb(45, 40, 20);
                        dgPreview.Rows[gridIdx].DefaultCellStyle.ForeColor = Color.FromArgb(240, 200, 100);
                        updateCount++;
                    }
                    else
                    {
                        dgPreview.Rows[gridIdx].DefaultCellStyle.BackColor = Color.FromArgb(20, 40, 30);
                        dgPreview.Rows[gridIdx].DefaultCellStyle.ForeColor = Color.FromArgb(120, 230, 150);
                        newCount++;
                    }
                }

                lblStats.Text = $"📊 المعاينة: إجمالي الأصناف: {_parsedRows.Count} | أصناف جديدة: {newCount} | أصناف للتعديل: {updateCount}";
                lblStats.ForeColor = Theme.Accent;
                btnImport.Enabled = _parsedRows.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل معاينة ومطابقة البيانات:\n" + ex.Message, "خطأ المعاينة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            if (_parsedRows == null || _parsedRows.Count == 0) return;

            string confirmMsg = $"هل أنت متأكد من استيراد {_parsedRows.Count} صنف لقاعدة البيانات؟\nسيتم حفظ الأرصدة وإدراجها بشكل آمن.";
            if (MessageBox.Show(confirmMsg, "تأكيد الاستيراد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            this.Cursor = Cursors.WaitCursor;
            btnImport.Enabled = false;

            int success = 0;
            int failed = 0;

            int stockPolicyIndex = cboStockPolicy.SelectedIndex; 

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    int defaultWarehouseID = 1;
                    var defaultWhRes = DbHelper.ScalarTrans(trans, "SELECT TOP 1 WarehouseID FROM Warehouses WHERE IsActive=1 ORDER BY WarehouseID");
                    if (defaultWhRes != null && defaultWhRes != DBNull.Value)
                        defaultWarehouseID = Convert.ToInt32(defaultWhRes);

                    foreach (var row in _parsedRows)
                    {
                        try
                        {
                            int? catID = null;
                            if (!string.IsNullOrWhiteSpace(row.CategoryName))
                            {
                                var catRes = DbHelper.ScalarTrans(trans, "SELECT CategoryID FROM Categories WHERE CategoryName = @n", DbHelper.P("@n", row.CategoryName.Trim()));
                                if (catRes != null && catRes != DBNull.Value)
                                {
                                    catID = Convert.ToInt32(catRes);
                                }
                                else
                                {
                                    catID = DbHelper.ExecuteInsertTrans(trans, "INSERT INTO Categories (CategoryName, IsActive) VALUES (@n, 1)", DbHelper.P("@n", row.CategoryName.Trim()));
                                }
                            }

                            int finalProductID = 0;

                            if (row.IsExisting)
                            {
                                DbHelper.ExecuteTrans(trans,
                                    @"UPDATE Products
                                      SET ProductName=@n, Unit=@u, SalePrice=@sp, PurchasePrice=@pp, MinStockLimit=@msl,
                                          PartNumber=@pn, CategoryID=@cat, CarModel=@cm, Brand=@b, ShelfLocation=@sl, 
                                          WholesalePrice=@wp, SemiWholesalePrice=@swp
                                      WHERE ProductID=@id",
                                    DbHelper.P("@n", row.ProductName),
                                    DbHelper.P("@u", row.Unit),
                                    DbHelper.P("@sp", row.SalePrice),
                                    DbHelper.P("@pp", row.PurchasePrice),
                                    DbHelper.P("@msl", row.MinStockLimit),
                                    DbHelper.P("@pn", row.PartNumber),
                                    catID.HasValue ? DbHelper.P("@cat", catID.Value) : DbHelper.P("@cat", DBNull.Value),
                                    DbHelper.P("@cm", row.CarModel),
                                    DbHelper.P("@b", row.Brand),
                                    DbHelper.P("@sl", row.ShelfLocation),
                                    DbHelper.P("@wp", row.WholesalePrice),
                                    DbHelper.P("@swp", row.SemiWholesalePrice),
                                    DbHelper.P("@id", row.ProductID));

                                finalProductID = row.ProductID;
                            }
                            else
                            {
                                string codeToUse = row.ProductCode;
                                if (codeToUse == "AUTO")
                                {
                                    var nextResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(ProductID), 0) + 1 FROM Products");
                                    codeToUse = nextResult != null ? nextResult.ToString() : "1";
                                }

                                finalProductID = DbHelper.ExecuteInsertTrans(trans,
                                    @"INSERT INTO Products (ProductCode, ProductName, Unit, SalePrice, IsActive, PurchasePrice, MinStockLimit, Description, PartNumber, CategoryID, CarModel, Brand, ShelfLocation, WholesalePrice, SemiWholesalePrice)
                                      VALUES (@c, @n, @u, @sp, 1, @pp, @msl, '', @pn, @cat, @cm, @b, @sl, @wp, @swp)",
                                    DbHelper.P("@c", codeToUse),
                                    DbHelper.P("@n", row.ProductName),
                                    DbHelper.P("@u", row.Unit),
                                    DbHelper.P("@sp", row.SalePrice),
                                    DbHelper.P("@pp", row.PurchasePrice),
                                    DbHelper.P("@msl", row.MinStockLimit),
                                    DbHelper.P("@pn", row.PartNumber),
                                    catID.HasValue ? DbHelper.P("@cat", catID.Value) : DbHelper.P("@cat", DBNull.Value),
                                    DbHelper.P("@cm", row.CarModel),
                                    DbHelper.P("@b", row.Brand),
                                    DbHelper.P("@sl", row.ShelfLocation),
                                    DbHelper.P("@wp", row.WholesalePrice),
                                    DbHelper.P("@swp", row.SemiWholesalePrice));
                            }

                            if (finalProductID > 0)
                            {
                                int warehouseID = defaultWarehouseID;
                                if (!string.IsNullOrWhiteSpace(row.WarehouseName) && _warehouseMap.TryGetValue(row.WarehouseName, out int wID))
                                {
                                    warehouseID = wID;
                                }

                                decimal bookQty = 0;
                                decimal actualQty = 0;
                                bool shouldAdjustStock = false;

                                if (row.IsExisting)
                                {
                                    if (stockPolicyIndex == 1) // جمع الأرصدة
                                    {
                                        bookQty = row.CurrentStock;
                                        actualQty = row.CurrentStock + row.InitialStock;
                                        shouldAdjustStock = row.InitialStock > 0;
                                    }
                                    else if (stockPolicyIndex == 2) // استبدال الأرصدة
                                    {
                                        bookQty = row.CurrentStock;
                                        actualQty = row.InitialStock;
                                        shouldAdjustStock = true; 
                                    }
                                }
                                else
                                {
                                    bookQty = 0;
                                    actualQty = row.InitialStock;
                                    shouldAdjustStock = row.InitialStock > 0;
                                }

                                if (shouldAdjustStock)
                                {
                                    DbHelper.ExecuteTrans(trans,
                                        @"INSERT INTO StockAdjustments (ProductID, WarehouseID, BookQty, ActualQty, Notes, CreatedBy)
                                          VALUES (@pid, @wid, @bq, @aq, @notes, @by)",
                                        DbHelper.P("@pid", finalProductID),
                                        DbHelper.P("@wid", warehouseID),
                                        DbHelper.P("@bq", bookQty),
                                        DbHelper.P("@aq", actualQty),
                                        DbHelper.P("@notes", "رصيد افتتاحي مستورد من ملف Excel"),
                                        DbHelper.P("@by", Session.EmpID));
                                }

                                success++;
                            }
                            else
                            {
                                failed++;
                            }
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            AppLogger.Error($"فشل استيراد صنف: {row.ProductName}", ex, "FrmImportProducts");
                        }
                    }
                });

                this.Cursor = Cursors.Default;
                ChickenDist.Core.ProductCache.Refresh();
                MessageBox.Show($"✅ اكتمل الاستيراد بنجاح!\nالأصناف المستوردة/المحدثة بنجاح: {success}\nالأصناف التي فشلت: {failed}",
                    "تم الاستيراد", MessageBoxButtons.OK, MessageBoxIcon.Information, 
                    MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                btnImport.Enabled = true;
                MessageBox.Show("فشل ترحيل الاستيراد بالكامل (تم التراجع عن التغييرات):\n" + ex.Message, "خطأ ترحيل البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetFriendlyName(string key)
        {
            switch (key)
            {
                case "ProductCode": return "كود الصنف";
                case "ProductName": return "اسم الصنف";
                case "PartNumber": return "رقم القطعة";
                case "CategoryName": return "التصنيف";
                case "CarModel": return "الموديل المتوافق";
                case "Brand": return "الماركة";
                case "ShelfLocation": return "موقع الرف";
                case "Unit": return "الوحدة";
                case "PurchasePrice": return "سعر الشراء";
                case "SalePrice": return "سعر قطاعي";
                case "SemiWholesalePrice": return "سعر نصف جملة";
                case "WholesalePrice": return "سعر الجملة";
                case "MinStockLimit": return "حد الطلب";
                case "InitialStock": return "الرصيد الافتتاحي";
                case "WarehouseName": return "المخزن المستهدف";
                default: return key;
            }
        }

        private static string Normalize(string s)
            => (s ?? "").Trim().ToLowerInvariant()
                        .Replace("ة", "ه").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا");
    }

    public class ParsedProductRow
    {
        public int ProductID { get; set; }
        public bool IsExisting { get; set; }
        public string ProductCode { get; set; }
        public string PartNumber { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string CarModel { get; set; }
        public string Brand { get; set; }
        public string ShelfLocation { get; set; }
        public string Unit { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal SemiWholesalePrice { get; set; }
        public decimal MinStockLimit { get; set; }
        public decimal InitialStock { get; set; }
        public string WarehouseName { get; set; }
        public decimal CurrentStock { get; set; }
    }
}

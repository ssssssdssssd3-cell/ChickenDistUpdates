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
    /// شاشة ذكية للغاية لاستيراد الأصناف من ملف CSV للعملاء الجدد،
    /// مع الحفاظ الكامل على سلامة البيانات وعدم تكرار الأصناف وإنشاء تسوية جردية للأرصدة.
    /// </summary>
    public class FrmImportProducts : Form
    {
        private Panel pnlHeader;
        private Label lblTitle, lblDesc;
        private DataGridView dgPreview;
        private ComboBox cboStockPolicy;
        private Button btnBrowse, btnCopyTemplate, btnImport, btnCancel;
        private Label lblStats;

        private List<ParsedProductRow> _parsedRows = new List<ParsedProductRow>();
        private Dictionary<string, int> _warehouseMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public FrmImportProducts()
        {
            InitializeComponent();
            LoadWarehouses();
        }

        private void InitializeComponent()
        {
            this.Text = "📥 استيراد الأصناف من ملف Excel";
            this.Size = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // شريط العنوان
            pnlHeader = Theme.MakeTitleBar("📥 استيراد الأصناف والبيانات الافتتاحية", 
                "استيراد الأصناف والأسعار والأرصدة من ملف Excel (.xlsx) مع التحقق التلقائي لمنع التكرار وتحديث البيانات بأمان.");
            this.Controls.Add(pnlHeader);

            // لوحة التحكم العلوية
            var pnlTopControls = new Panel
            {
                Location = new Point(20, 80),
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

            // جدول المعاينة
            dgPreview = new DataGridView
            {
                Location = new Point(20, 155),
                Size = new Size(1045, 410),
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

            // الإحصائيات والأزرار بالأسفل
            int yBottom = 575;

            lblStats = new Label
            {
                Text = "يرجى اختيار ملف Excel (.xlsx) للبدء بالمعاينة والمطابقة...",
                Location = new Point(20, yBottom + 12),
                Size = new Size(500, 24),
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            this.Controls.Add(lblStats);

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

        private void LoadWarehouses()
        {
            _warehouseMap.Clear();
            var dt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive=1");
            foreach (DataRow r in dt.Rows)
            {
                _warehouseMap[r["WarehouseName"].ToString()] = (int)r["WarehouseID"];
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
                    ParseProductsExcel(dlg.FileName);
                }
            }
        }

        private void ParseProductsExcel(string filePath)
        {
            try
            {
                _parsedRows.Clear();
                dgPreview.Rows.Clear();

                // قراءة القواميس للمطابقة السريعة ومنع التكرار
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

                // قراءة ملف Excel باستخدام قارئ Excel المدمج
                List<string[]> lines = XlsxParser.Parse(filePath);
                if (lines.Count <= 1)
                {
                    MessageBox.Show("الملف فارغ أو يحتوي على الهيدر فقط.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int newCount = 0;
                int updateCount = 0;

                for (int idx = 1; idx < lines.Count; idx++)
                {
                    string[] cols = lines[idx];
                    if (cols == null || cols.Length < 2) continue; // يجب أن يحتوي على كود واسم على الأقل

                    string code = cols[0].Trim();
                    string name = cols[1].Trim();
                    if (string.IsNullOrEmpty(name)) continue; // تخطي السطور بدون اسم

                    string part = cols.Length > 2 ? cols[2].Trim() : "";
                    string category = cols.Length > 3 ? cols[3].Trim() : "";
                    string model = cols.Length > 4 ? cols[4].Trim() : "";
                    string brand = cols.Length > 5 ? cols[5].Trim() : "";
                    string shelf = cols.Length > 6 ? cols[6].Trim() : "";
                    string unit = cols.Length > 7 && !string.IsNullOrEmpty(cols[7]) ? cols[7].Trim() : "قطعة";

                    decimal purchasePrice = 0, salePrice = 0, semiWholesale = 0, wholesale = 0, minStock = 0, initialStock = 0;
                    if (cols.Length > 8) decimal.TryParse(cols[8].Trim(), out purchasePrice);
                    if (cols.Length > 9) decimal.TryParse(cols[9].Trim(), out salePrice);
                    if (cols.Length > 10) decimal.TryParse(cols[10].Trim(), out semiWholesale);
                    if (cols.Length > 11) decimal.TryParse(cols[11].Trim(), out wholesale);
                    if (cols.Length > 12) decimal.TryParse(cols[12].Trim(), out minStock);
                    if (cols.Length > 13) decimal.TryParse(cols[13].Trim(), out initialStock);

                    string warehouseName = cols.Length > 14 ? cols[14].Trim() : "المخزن الرئيسي";

                    // مطابقة الصنف لتحديد هل هو جديد أم موجود
                    int matchedProductID = 0;
                    bool isExisting = false;

                    if (!string.IsNullOrEmpty(code) && codeMap.TryGetValue(code, out int idByCode))
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

                    // جلب الرصيد الحالي للصنف لو كان موجوداً
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

                    // إضافة للجدول للمعاينة
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

                    // تلوين الصفوف حسب الحالة
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
                MessageBox.Show("فشل قراءة ملف Excel:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            // 0 = تجاهل رصيد المشتريات للأصناف الموجودة
            // 1 = جمع الرصيد المستورد مع الرصيد الحالي
            // 2 = استبدال الرصيد الحالي بالرصيد المستورد

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    // جلب أول مخزن نشط كافتراضي
                    int defaultWarehouseID = 1;
                    var defaultWhRes = DbHelper.ScalarTrans(trans, "SELECT TOP 1 WarehouseID FROM Warehouses WHERE IsActive=1 ORDER BY WarehouseID");
                    if (defaultWhRes != null && defaultWhRes != DBNull.Value)
                        defaultWarehouseID = Convert.ToInt32(defaultWhRes);

                    foreach (var row in _parsedRows)
                    {
                        try
                        {
                            // 1. معالجة وتأمين التصنيف
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

                            // 2. معالجة وحفظ الصنف نفسه
                            if (row.IsExisting)
                            {
                                // تعديل صنف موجود
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
                                // إضافة صنف جديد
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

                            // 3. معالجة وتأمين المخزن والمخزون
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
                                    // للأصناف الموجودة مسبقاً
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
                                        shouldAdjustStock = true; // يتم التعديل حتى لو كان الصفر
                                    }
                                }
                                else
                                {
                                    // للأصناف الجديدة
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
                                        DbHelper.P("@notes", "رصيد افتتاحي مستورد من ملف CSV"),
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

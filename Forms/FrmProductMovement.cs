using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmProductMovement : Form
    {
        private int _productID;
        private string _productName;
        private string _initialUnit;
        private decimal _initialFactor;
        private int? _initialWarehouseID;
        private bool _isInitializing = true;

        private DataGridView dgMovement;
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboTransType, cboUnit, cboWarehouse;
        private Button btnLoad, btnPrint;
        private Label lblTitle, lblSummary;

        public class UnitOption
        {
            public string Name { get; set; }
            public decimal Factor { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() => DisplayText;
        }

        public FrmProductMovement(int productID, string productName, string productUnit)
            : this(productID, productName, productUnit, 1.0m, null)
        {
        }

        public FrmProductMovement(int productID, string productName, string productUnit, decimal currentFactor, int? warehouseID = null)
        {
            _productID = productID;
            _productName = productName;
            _initialUnit = productUnit?.Replace(" 🔽", "")?.Trim() ?? "";
            _initialFactor = currentFactor > 0 ? currentFactor : 1.0m;
            _initialWarehouseID = warehouseID;

            InitUI();
            LoadWarehouses();
            LoadUnitOptions();

            _isInitializing = false;
            LoadMovement();
        }

        private void InitUI()
        {
            this.Text = $"تقرير حركة الصنف - {_productName}";
            this.Size = new Size(1080, 720);
            this.MinimumSize = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== Header Title & Summary =====
            var pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 65, 
                BackColor = Theme.BgCard, 
                Padding = new Padding(15, 8, 15, 8) 
            };
            
            lblTitle = new Label 
            { 
                Text = $"📊 تقرير حركة الصنف: {_productName}", 
                Font = Theme.FontHeader, 
                ForeColor = Theme.Accent,
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblSummary = new Label
            {
                Text = "جاري احتساب أرصدة وحركات الصنف...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 90, 120),
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(lblSummary);
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // ===== Filter Bar (2 Rows) =====
            var pnlFilters = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 84, 
                BackColor = Theme.BgCard, 
                Padding = new Padding(12, 6, 12, 6) 
            };

            // Row 1: التاريخ من - إلى - المخزن - نوع الحركة
            int yRow1 = 8;
            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(1020, yRow1 + 4), Font = Theme.FontBold };
            dtpFrom = new DateTimePicker 
            { 
                Location = new Point(835, yRow1), 
                Width = 180, 
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd   hh:mm tt",
                Value = DateTime.Today.AddDays(-30)
            };
            dtpFrom.ValueChanged += (s, e) => { if (!_isInitializing) LoadMovement(); };

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(795, yRow1 + 4), Font = Theme.FontBold };
            dtpTo = new DateTimePicker 
            { 
                Location = new Point(610, yRow1), 
                Width = 180, 
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd   hh:mm tt",
                Value = DateTime.Now
            };
            dtpTo.ValueChanged += (s, e) => { if (!_isInitializing) LoadMovement(); };

            var lblWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(555, yRow1 + 4), Font = Theme.FontBold };
            cboWarehouse = new ComboBox
            {
                Location = new Point(390, yRow1),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            cboWarehouse.SelectedIndexChanged += (s, e) => { if (!_isInitializing) LoadMovement(); };

            var lblType = new Label { Text = "نوع الحركة:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(310, yRow1 + 4), Font = Theme.FontBold };
            cboTransType = new ComboBox
            {
                Location = new Point(155, yRow1),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            cboTransType.Items.AddRange(new object[] {
                "(جميع الحركات)",
                "مبيعات",
                "مشتريات",
                "مرتجعات مبيعات",
                "مرتجعات مشتريات",
                "تسويات جردية",
                "تحويلات مخزنية",
                "تالف وهالك",
                "أوامر تصنيع"
            });
            cboTransType.SelectedIndex = 0;
            cboTransType.SelectedIndexChanged += (s, e) => { if (!_isInitializing) LoadMovement(); };

            // Row 2: الوحدة المعروضة - أزرار العرض والطباعة
            int yRow2 = 45;
            var lblUnit = new Label { Text = "الوحدة المعروضة:", AutoSize = true, ForeColor = Theme.Accent, Location = new Point(940, yRow2 + 4), Font = Theme.FontBold };
            cboUnit = new ComboBox
            {
                Location = new Point(675, yRow2),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 245, 255),
                ForeColor = Color.FromArgb(10, 45, 90)
            };
            cboUnit.SelectedIndexChanged += (s, e) => { if (!_isInitializing) LoadMovement(); };

            btnLoad = Theme.MakeButton("🔍 تحديث الحركة", Color.FromArgb(40, 110, 50));
            btnLoad.Location = new Point(530, yRow2 - 2);
            btnLoad.Size = new Size(135, 34);
            btnLoad.Click += (s, e) => LoadMovement();

            btnPrint = Theme.MakeButton("🖨 طباعة تقرير الحركة", Theme.Accent);
            btnPrint.Location = new Point(345, yRow2 - 2);
            btnPrint.Size = new Size(175, 34);
            btnPrint.Click += (s, e) => PrintMovement();

            pnlFilters.Controls.AddRange(new Control[] { 
                lblFrom, dtpFrom, lblTo, dtpTo, lblWh, cboWarehouse, lblType, cboTransType,
                lblUnit, cboUnit, btnLoad, btnPrint 
            });
            this.Controls.Add(pnlFilters);

            // ===== Movement Grid =====
            dgMovement = new DataGridView
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
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.75f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ والوقت", FillWeight = 42 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع الحركة", FillWeight = 45 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefCode", HeaderText = "المستند", FillWeight = 36 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "PersonName", HeaderText = "العميل / المورد / المندوب", FillWeight = 55 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "المخزن", FillWeight = 36 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "QtyIn", HeaderText = "وارد (+)", FillWeight = 32 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "QtyOut", HeaderText = "صادر (-)", FillWeight = 32 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد الحالي", FillWeight = 36 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان / ملاحظات", FillWeight = 65 });

            this.Controls.Add(dgMovement);

            // Apply responsive Z-Order docking:
            pnlHeader.BringToFront();
            pnlFilters.BringToFront();
            dgMovement.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private void LoadWarehouses()
        {
            cboWarehouse.Items.Clear();
            cboWarehouse.Items.Add(new ComboItem(0, "🏢 (جميع المخازن)"));
            try
            {
                var dt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseID");
                foreach (DataRow r in dt.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
                }
            }
            catch { }

            int selIdx = 0;
            if (_initialWarehouseID.HasValue && _initialWarehouseID.Value > 0)
            {
                for (int i = 0; i < cboWarehouse.Items.Count; i++)
                {
                    if (cboWarehouse.Items[i] is ComboItem ci && ci.ID == _initialWarehouseID.Value)
                    {
                        selIdx = i;
                        break;
                    }
                }
            }
            cboWarehouse.SelectedIndex = selIdx;
        }

        private void LoadUnitOptions()
        {
            cboUnit.Items.Clear();
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT Unit, Unit1Name, Unit2Name, Unit2Factor, Unit3Factor 
                    FROM Products WHERE ProductID = @pid", DbHelper.P("@pid", _productID));

                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    string baseUnit = r["Unit"] != DBNull.Value ? r["Unit"].ToString().Trim() : "";
                    string unit1 = r.Table.Columns.Contains("Unit1Name") && r["Unit1Name"] != DBNull.Value ? r["Unit1Name"].ToString().Trim() : "";
                    string unit2 = r.Table.Columns.Contains("Unit2Name") && r["Unit2Name"] != DBNull.Value ? r["Unit2Name"].ToString().Trim() : "";

                    decimal u2Factor = r.Table.Columns.Contains("Unit2Factor") && r["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit2Factor"]) : 0m;
                    decimal u3Factor = r.Table.Columns.Contains("Unit3Factor") && r["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit3Factor"]) : 0m;

                    decimal majorFactor = 1m;
                    decimal mediumFactor = 1m;
                    decimal smallFactor = 1.0m;

                    if (!string.IsNullOrWhiteSpace(unit2))
                    {
                        decimal f2 = u2Factor > 0 ? u2Factor : 1m;
                        decimal f3 = u3Factor > 0 ? u3Factor : 1m;
                        mediumFactor = f2;
                        majorFactor = f2 * f3;
                    }
                    else
                    {
                        if (u3Factor > 0) majorFactor = u3Factor;
                        else if (u2Factor > 0) majorFactor = u2Factor;
                        else majorFactor = 1m;
                        mediumFactor = majorFactor;
                    }
                    if (majorFactor <= 0) majorFactor = 1m;
                    if (mediumFactor <= 0) mediumFactor = 1m;

                    string majorName = !string.IsNullOrWhiteSpace(baseUnit) ? baseUnit : "الكبرى";
                    string mediumName = unit2;
                    string smallName = !string.IsNullOrWhiteSpace(unit1) ? unit1 : (!string.IsNullOrWhiteSpace(baseUnit) ? baseUnit : "الصغرى");

                    // 1. الوحدة الكبرى
                    var optMajor = new UnitOption 
                    { 
                        Name = majorName, 
                        Factor = majorFactor, 
                        DisplayText = $"📦 {majorName} (الوحدة الكبرى - معامل: {majorFactor:G29})" 
                    };
                    cboUnit.Items.Add(optMajor);

                    // 2. الوحدة الوسطى (إن وجدت ومختلفة)
                    if (!string.IsNullOrWhiteSpace(mediumName) && !string.Equals(mediumName, majorName, StringComparison.OrdinalIgnoreCase) && mediumFactor > 1m && mediumFactor != majorFactor)
                    {
                        var optMedium = new UnitOption 
                        { 
                            Name = mediumName, 
                            Factor = mediumFactor, 
                            DisplayText = $"⚙️ {mediumName} (الوحدة الوسطى - معامل: {mediumFactor:G29})" 
                        };
                        cboUnit.Items.Add(optMedium);
                    }

                    // 3. الوحدة الصغرى (إن وجدت ومختلفة عن الكبرى)
                    if (!string.IsNullOrWhiteSpace(smallName) && (!string.Equals(smallName, majorName, StringComparison.OrdinalIgnoreCase) || majorFactor > 1m))
                    {
                        var optSmall = new UnitOption 
                        { 
                            Name = smallName, 
                            Factor = smallFactor, 
                            DisplayText = $"🔹 {smallName} (الوحدة الصغرى - تجزئة - معامل: 1)" 
                        };
                        cboUnit.Items.Add(optSmall);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmProductMovement.LoadUnitOptions", ex);
            }

            if (cboUnit.Items.Count == 0)
            {
                string uName = !string.IsNullOrWhiteSpace(_initialUnit) ? _initialUnit : "وحدة";
                cboUnit.Items.Add(new UnitOption { Name = uName, Factor = 1.0m, DisplayText = $"📦 {uName}" });
            }

            // تحديد الوحدة الافتراضية بناءً على ما كان محدداً في شاشة الجرد:
            int selectedIdx = 0;
            if (_initialFactor > 0)
            {
                for (int i = 0; i < cboUnit.Items.Count; i++)
                {
                    if (cboUnit.Items[i] is UnitOption uo)
                    {
                        if (Math.Abs(uo.Factor - _initialFactor) < 0.001m)
                        {
                            selectedIdx = i;
                            break;
                        }
                        if (!string.IsNullOrWhiteSpace(_initialUnit) && string.Equals(uo.Name, _initialUnit, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedIdx = i;
                            break;
                        }
                    }
                }
            }
            cboUnit.SelectedIndex = selectedIdx;
        }

        private static string FormatQty(decimal q)
        {
            if (q == 0) return "";
            decimal abs = Math.Abs(q);
            if (abs % 1 == 0) return q.ToString("N0");
            if ((abs * 10) % 1 == 0) return q.ToString("N1");
            if ((abs * 100) % 1 == 0) return q.ToString("N2");
            return q.ToString("0.###");
        }

        private static string FormatBalance(decimal q)
        {
            if (q == 0) return "0";
            decimal abs = Math.Abs(q);
            if (abs % 1 == 0) return q.ToString("N0");
            if ((abs * 10) % 1 == 0) return q.ToString("N1");
            if ((abs * 100) % 1 == 0) return q.ToString("N2");
            return q.ToString("0.###");
        }

        private void LoadMovement()
        {
            dgMovement.Rows.Clear();

            var selectedUnit = cboUnit?.SelectedItem as UnitOption;
            decimal factor = (selectedUnit != null && selectedUnit.Factor > 0) ? selectedUnit.Factor : 1.0m;
            string unitName = selectedUnit != null ? selectedUnit.Name : (!string.IsNullOrWhiteSpace(_initialUnit) ? _initialUnit : "وحدة");

            int? wid = null;
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                wid = ci.ID;
            }

            // تحديث عناوين الأعمدة والرأس بالوحدة المختارة
            dgMovement.Columns["QtyIn"].HeaderText = $"وارد (+) [{unitName}]";
            dgMovement.Columns["QtyOut"].HeaderText = $"صادر (-) [{unitName}]";
            dgMovement.Columns["Balance"].HeaderText = $"الرصيد [{unitName}]";

            string whTitle = wid.HasValue ? $" - مخزن: {(cboWarehouse.SelectedItem as ComboItem)?.Text}" : " - (جميع المخازن)";
            lblTitle.Text = $"📊 تقرير حركة الصنف: {_productName} ({unitName}){whTitle}";

            DataTable dt = InventoryDAL.GetProductMovement(_productID, wid);

            decimal runningBalance = 0;
            decimal openingBalance = 0;
            decimal totalPeriodIn = 0;
            decimal totalPeriodOut = 0;

            // 1. حساب رصيد أول المدة قبل تاريخ البداية المختار (بالوحدة المحددة)
            foreach (DataRow r in dt.Rows)
            {
                DateTime date = Convert.ToDateTime(r["TransDate"]);
                decimal rawIn = Convert.ToDecimal(r["QtyIn"]);
                decimal rawOut = Convert.ToDecimal(r["QtyOut"]);
                decimal inScaled = rawIn / factor;
                decimal outScaled = rawOut / factor;

                if (date.Date < dtpFrom.Value.Date)
                {
                    openingBalance += inScaled - outScaled;
                }
            }

            runningBalance = openingBalance;

            // 2. إضافة سطر رصيد أول المدة
            dgMovement.Rows.Add(
                dtpFrom.Value.Date.ToString("dd/MM/yyyy"),
                "رصيد أول المدة",
                "---",
                "---",
                wid.HasValue ? (cboWarehouse.SelectedItem as ComboItem)?.Text : "جميع المخازن",
                openingBalance >= 0 ? FormatQty(openingBalance) : "",
                openingBalance < 0 ? FormatQty(Math.Abs(openingBalance)) : "",
                FormatBalance(openingBalance),
                $"الرصيد الدفتري المتراكم قبل تاريخ {dtpFrom.Value:dd/MM/yyyy} ({unitName})"
            );
            dgMovement.Rows[0].DefaultCellStyle.ForeColor = Theme.TextSub;
            dgMovement.Rows[0].DefaultCellStyle.Font = new Font(Theme.FontMain, FontStyle.Italic);

            string selectedType = cboTransType?.SelectedItem?.ToString() ?? "(جميع الحركات)";

            // 3. إضافة الحركات الواقعة داخل الفترة المحددة
            foreach (DataRow r in dt.Rows)
            {
                DateTime date = Convert.ToDateTime(r["TransDate"]);
                if (date.Date >= dtpFrom.Value.Date && date.Date <= dtpTo.Value.Date)
                {
                    string transType = r["TransType"]?.ToString() ?? "";
                    decimal rawIn = Convert.ToDecimal(r["QtyIn"]);
                    decimal rawOut = Convert.ToDecimal(r["QtyOut"]);
                    decimal inScaled = rawIn / factor;
                    decimal outScaled = rawOut / factor;

                    runningBalance += inScaled - outScaled;
                    totalPeriodIn += inScaled;
                    totalPeriodOut += outScaled;

                    // مطابقة نوع الحركة
                    bool match = false;
                    if (selectedType == "(جميع الحركات)") match = true;
                    else if (selectedType == "مبيعات" && (transType.Contains("بيع") || transType.Contains("تحميل حمولة"))) match = true;
                    else if (selectedType == "مشتريات" && transType.Contains("شراء") && !transType.Contains("مرتجع")) match = true;
                    else if (selectedType == "مرتجعات مبيعات" && transType.Contains("مرتجع") && (transType.Contains("مبيعات") || transType.Contains("حمولة"))) match = true;
                    else if (selectedType == "مرتجعات مشتريات" && transType.Contains("مرتجع") && transType.Contains("مشتريات")) match = true;
                    else if (selectedType == "تسويات جردية" && transType.Contains("تسوية")) match = true;
                    else if (selectedType == "تحويلات مخزنية" && transType.Contains("تحويل")) match = true;
                    else if (selectedType == "تالف وهالك" && (transType.Contains("تالف") || transType.Contains("هالك"))) match = true;
                    else if (selectedType == "أوامر تصنيع" && (transType.Contains("تصنيع") || transType.Contains("إنتاج"))) match = true;
                    else if (transType.Equals(selectedType, StringComparison.OrdinalIgnoreCase)) match = true;

                    if (!match) continue;

                    int rowIndex = dgMovement.Rows.Add(
                        date.ToString("dd/MM/yyyy HH:mm"),
                        transType,
                        r["RefCode"],
                        r["PersonName"],
                        r["WarehouseName"],
                        inScaled > 0 ? FormatQty(inScaled) : "",
                        outScaled > 0 ? FormatQty(outScaled) : "",
                        FormatBalance(runningBalance),
                        r["Notes"]
                    );

                    // تلوين قيم الوارد والصادر للوضوح
                    if (inScaled > 0)
                    {
                        var cellIn = dgMovement.Rows[rowIndex].Cells["QtyIn"];
                        cellIn.Style.ForeColor = Color.FromArgb(0, 130, 50);
                        cellIn.Style.Font = new Font(Theme.FontMain, FontStyle.Bold);
                    }
                    if (outScaled > 0)
                    {
                        var cellOut = dgMovement.Rows[rowIndex].Cells["QtyOut"];
                        cellOut.Style.ForeColor = Color.FromArgb(198, 40, 40);
                        cellOut.Style.Font = new Font(Theme.FontMain, FontStyle.Bold);
                    }
                }
            }

            // 4. تحديث شريط الملخص العلوي
            lblSummary.Text = $"📦 رصيد أول المدة: {FormatBalance(openingBalance)}  |  🟢 إجمالي الوارد: {FormatBalance(totalPeriodIn)}  |  🔴 إجمالي الصادر: {FormatBalance(totalPeriodOut)}  |  🎯 الرصيد الختامي: {FormatBalance(runningBalance)} {unitName}";
        }

        private void PrintMovement()
        {
            var selectedUnit = cboUnit?.SelectedItem as UnitOption;
            string unitName = selectedUnit != null ? selectedUnit.Name : (!string.IsNullOrWhiteSpace(_initialUnit) ? _initialUnit : "وحدة");
            string whName = cboWarehouse?.SelectedItem?.ToString() ?? "جميع المخازن";

            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.Landscape = false;
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // A4 portrait
            
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 16, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var normal = new Font("Arial", 9);
                var small = new Font("Arial", 8);
                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };

                int y = 30;
                int pageW = 800;

                // Header
                g.DrawString($"حركة الصنف: {_productName}", boldBig, Brushes.DarkBlue, new RectangleF(20, y, pageW - 40, 30), center); y += 30;
                g.DrawString(AppConfig.CompanyName, bold, Brushes.Black, new RectangleF(20, y, pageW - 40, 22), center); y += 25;
                g.DrawLine(new Pen(Color.DarkBlue, 2), 20, y, pageW - 20, y); y += 10;

                // Meta Info
                string typeFilterText = cboTransType != null && cboTransType.SelectedIndex > 0 ? $"   |   نوع الحركة: {cboTransType.SelectedItem}" : "";
                g.DrawString($"الصنف: {_productName} [{unitName}]   |   المخزن: {whName}", bold, Brushes.Black, 20, y);
                g.DrawString($"الفترة: من {dtpFrom.Value:dd/MM/yyyy} إلى {dtpTo.Value:dd/MM/yyyy}{typeFilterText}", normal, Brushes.Black, new RectangleF(20, y, pageW - 40, 20), right);
                y += 25;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y); y += 10;

                // Table columns: Date, Type, Ref, Party, Wh, In, Out, Balance, Notes
                int[] xCols = { 20, 115, 195, 255, 345, 410, 470, 530, 595 };
                string[] headers = { "التاريخ", "نوع الحركة", "المستند", "الجهة / المندوب", "المخزن", $"وارد ({unitName})", $"صادر ({unitName})", $"الرصيد", "البيان" };
                
                for (int i = 0; i < headers.Length; i++)
                {
                    g.DrawString(headers[i], bold, Brushes.DarkBlue, xCols[i], y);
                }
                y += 22;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y); y += 8;

                // Draw rows
                foreach (DataGridViewRow row in dgMovement.Rows)
                {
                    if (y > 1100)
                    {
                        g.DrawString("يتبع في الصفحة التالية...", small, Brushes.Gray, 20, y);
                        break;
                    }

                    string date = row.Cells["TransDate"].Value?.ToString() ?? "";
                    string type = row.Cells["TransType"].Value?.ToString() ?? "";
                    string refCode = row.Cells["RefCode"].Value?.ToString() ?? "";
                    string party = row.Cells["PersonName"].Value?.ToString() ?? "---";
                    string wh = row.Cells["WarehouseName"].Value?.ToString() ?? "";
                    string qtyIn = row.Cells["QtyIn"].Value?.ToString() ?? "";
                    string qtyOut = row.Cells["QtyOut"].Value?.ToString() ?? "";
                    string balance = row.Cells["Balance"].Value?.ToString() ?? "";
                    string notes = row.Cells["Notes"].Value?.ToString() ?? "";

                    g.DrawString(date, normal, Brushes.Black, xCols[0], y);
                    g.DrawString(type, normal, Brushes.Black, xCols[1], y);
                    g.DrawString(refCode, normal, Brushes.Black, xCols[2], y);
                    
                    if (party.Length > 14) party = party.Substring(0, 12) + "..";
                    g.DrawString(party, normal, Brushes.Black, xCols[3], y);

                    if (wh.Length > 10) wh = wh.Substring(0, 9) + "..";
                    g.DrawString(wh, normal, Brushes.Black, xCols[4], y);
                    
                    g.DrawString(qtyIn, bold, Brushes.DarkGreen, xCols[5], y);
                    g.DrawString(qtyOut, bold, Brushes.DarkRed, xCols[6], y);
                    g.DrawString(balance, bold, Brushes.Black, xCols[7], y);

                    if (notes.Length > 28) notes = notes.Substring(0, 26) + "..";
                    g.DrawString(notes, small, Brushes.DarkSlateGray, xCols[8], y);

                    y += 20;
                }

                y += 10;
                g.DrawLine(new Pen(Color.DarkBlue, 1.5f), 20, y, pageW - 20, y); y += 8;
                g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}   |   الوحدة: {unitName}   |   نظام التوزيع الذكي", small, Brushes.Gray, 20, y);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 900,
                Height = 750,
                Text = $"معاينة حركة صنف - {_productName} ({unitName})"
            };
            preview.ShowDialog();
        }
    }
}

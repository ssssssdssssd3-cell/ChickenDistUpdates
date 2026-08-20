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
    /// <summary>
    /// شاشة استرجاع عمليات وجلسات الجرد المخزني وطباعة باركود الزيادات والتقارير
    /// </summary>
    public class FrmInventorySessions : Form
    {
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboWarehouse;
        private TextBox txtSearch;
        private Button btnSearch, btnPrintOperationBarcodes, btnPrintOperationReport;

        private DataGridView dgSessions;
        private DataGridView dgDetails;
        private Label lblSessionsCount, lblDetailsTitle, lblSessionSummary;

        private string _selectedBatchCode = "";
        private DateTime? _selectedSessionDate = null;
        private string _selectedWarehouseName = "";
        private string _selectedUserName = "";

        public FrmInventorySessions(string initialBatchCode = "")
        {
            _selectedBatchCode = initialBatchCode;
            InitUI();
            LoadWarehouses();
            LoadSessions();
        }

        private void InitUI()
        {
            this.Text = "📋 سجل واسترجاع عمليات الجرد المخزني وطباعة باركود الزيادات";
            this.Size = new Size(1200, 750);
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── 1. شريط العنوان ──
            var pnlTitle = Theme.MakeTitleBar("📋 استرجاع عمليات الجرد المخزني وطباعة الباركود", "يمكنك اختيار أي عملية جرد سابقة برقمها لاستعراض تفاصيلها وطباعة باركود الأصناف التي تمت زيادتها.");

            // ── 2. شريط الفلتر العلوي ──
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            pnlFilter.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(5, 8, 2, 0) });
            dtpFrom = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-60) };
            pnlFilter.Controls.Add(dtpFrom);

            pnlFilter.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(12, 8, 2, 0) });
            dtpTo = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnlFilter.Controls.Add(dtpTo);

            pnlFilter.Controls.Add(new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(12, 8, 2, 0) });
            cboWarehouse = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            pnlFilter.Controls.Add(cboWarehouse);

            pnlFilter.Controls.Add(new Label { Text = "بحث برقم العملية أو الصنف:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(12, 8, 2, 0) });
            txtSearch = new TextBox { Width = 160, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadSessions(); };
            pnlFilter.Controls.Add(txtSearch);

            btnSearch = Theme.MakeButton("🔍 عرض العمليات", Theme.Primary);
            btnSearch.Size = new Size(120, 32);
            btnSearch.Click += (s, e) => LoadSessions();
            pnlFilter.Controls.Add(btnSearch);

            btnPrintOperationBarcodes = Theme.MakeButton("🏷️ طباعة باركود زيادات العملية", Color.FromArgb(39, 174, 96));
            btnPrintOperationBarcodes.Size = new Size(210, 32);
            btnPrintOperationBarcodes.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPrintOperationBarcodes.Click += BtnPrintOperationBarcodes_Click;
            pnlFilter.Controls.Add(btnPrintOperationBarcodes);

            btnPrintOperationReport = Theme.MakeButton("🖨️ طباعة كشف العملية", Theme.Secondary);
            btnPrintOperationReport.Size = new Size(140, 32);
            btnPrintOperationReport.Click += BtnPrintOperationReport_Click;
            pnlFilter.Controls.Add(btnPrintOperationReport);

            // ── 3. SplitContainer: Upper for Sessions list, Lower for Session details ──
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 260,
                BackColor = Color.FromArgb(210, 215, 225),
                Panel1 = { BackColor = Theme.BgMain },
                Panel2 = { BackColor = Theme.BgMain }
            };

            // ── Upper Panel: Master Sessions Grid ──
            var pnlUpperHeader = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Theme.BgCard, Padding = new Padding(10, 6, 10, 0) };
            lblSessionsCount = new Label { Text = "📋 قائمة عمليات وجلسات الجرد المنفذة:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.Accent };
            pnlUpperHeader.Controls.Add(lblSessionsCount);

            dgSessions = MakeGrid();
            dgSessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "BatchCode",      HeaderText = "رقم العملية / الجلسة", FillWeight = 90 });
            dgSessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "OpDate",         HeaderText = "تاريخ ووقت الجرد",    FillWeight = 85 });
            dgSessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName",  HeaderText = "المخزن",             FillWeight = 70 });
            dgSessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy",      HeaderText = "القائم بالجرد",       FillWeight = 65 });
            dgSessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalItems",     HeaderText = "عدد الأصناف",        FillWeight = 45 });
            dgSessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "SurplusItems",   HeaderText = "أصناف زيادة (➕)",    FillWeight = 50 });
            dgSessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShortageItems",  HeaderText = "أصناف عجز (➖)",     FillWeight = 50 });
            dgSessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalSurplusQty",HeaderText = "إجمالي كمية الزيادة", FillWeight = 55 });
            Theme.AdjustGridHeaders(dgSessions);

            dgSessions.SelectionChanged += DgSessions_SelectionChanged;

            split.Panel1.Controls.Add(dgSessions);
            split.Panel1.Controls.Add(pnlUpperHeader);

            // ── Lower Panel: Detail Items Grid ──
            var pnlLowerHeader = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Theme.BgCard, Padding = new Padding(10, 6, 10, 0) };
            lblDetailsTitle = new Label { Text = "🔍 تفاصيل الأصناف والفروق لعملية الجرد المحددة:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.Primary };
            lblSessionSummary = new Label { Text = "", AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 120, 60), Location = new Point(450, 8) };
            pnlLowerHeader.Controls.AddRange(new Control[] { lblDetailsTitle, lblSessionSummary });

            dgDetails = MakeGrid();
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID",     Visible = false });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode",   HeaderText = "الباركود / الكود", FillWeight = 45 });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName",   HeaderText = "اسم الصنف",       FillWeight = 100 });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLocation", HeaderText = "مكان الرف",       FillWeight = 40 });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",          HeaderText = "الوحدة",          FillWeight = 35 });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice",     HeaderText = "سعر البيع",       FillWeight = 40 });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty",       HeaderText = "الرصيد الدفتري",  FillWeight = 45 });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualQty",     HeaderText = "الرصيد الفعلي",   FillWeight = 45 });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffQty",       HeaderText = "الفارق",          FillWeight = 40 });
            dgDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",         HeaderText = "الملاحظات",       FillWeight = 70 });
            Theme.AdjustGridHeaders(dgDetails);

            // Context menu for Details grid
            var cmsDetails = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };
            cmsDetails.Items.Add("🏷️ طباعة استيكر باركود للصنف المحدد", null, (s, e) =>
            {
                if (dgDetails.SelectedRows.Count > 0)
                {
                    var r = dgDetails.SelectedRows[0];
                    int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
                    string name = r.Cells["ProductName"].Value?.ToString() ?? "";
                    string code = r.Cells["ProductCode"].Value?.ToString() ?? "";
                    string shelf = r.Cells["ShelfLocation"].Value?.ToString() ?? "";
                    decimal.TryParse(r.Cells["SalePrice"].Value?.ToString(), out decimal sp);
                    decimal.TryParse(r.Cells["DiffQty"].Value?.ToString()?.Replace("+", ""), out decimal diff);

                    int qty = diff > 0 ? (int)Math.Ceiling(diff) : 1;
                    var item = new BarcodePrintItem { ProductID = pid, ProductName = name, ProductCode = code, Price = sp, PrintQty = qty, ShelfLocation = shelf };
                    new FrmBulkPrintBarcodes(new List<BarcodePrintItem> { item }).ShowDialog(this);
                }
            });
            cmsDetails.Items.Add("🔍 فتح كارت الصنف", null, (s, e) =>
            {
                if (dgDetails.SelectedRows.Count > 0)
                {
                    int pid = Convert.ToInt32(dgDetails.SelectedRows[0].Cells["ProductID"].Value);
                    if (pid > 0) new FrmProductCard(pid).ShowDialog(this);
                }
            });
            dgDetails.ContextMenuStrip = cmsDetails;

            split.Panel2.Controls.Add(dgDetails);
            split.Panel2.Controls.Add(pnlLowerHeader);

            this.Controls.Add(split);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlTitle);
        }

        private DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(220, 225, 230),
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain, Alignment = DataGridViewContentAlignment.MiddleCenter },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(246, 248, 250), ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain, Alignment = DataGridViewContentAlignment.MiddleCenter },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleCenter },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            Theme.EnableDoubleBuffer(g);
            return g;
        }

        private void LoadWarehouses()
        {
            try
            {
                var dt = WarehouseDAL.GetAll(true);
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add(new ComboItem(0, "--- كل المخازن ---"));
                foreach (DataRow r in dt.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
                }
                cboWarehouse.SelectedIndex = 0;
            }
            catch { }
        }

        public void LoadSessions()
        {
            dgSessions.Rows.Clear();
            dgDetails.Rows.Clear();

            int? wid = null;
            if (cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
                wid = ci.ID;

            string filter = "";
            var prms = new List<System.Data.SqlClient.SqlParameter>
            {
                DbHelper.P("@f", dtpFrom.Value.Date),
                DbHelper.P("@t", dtpTo.Value.Date.AddDays(1).AddSeconds(-1))
            };

            if (wid.HasValue)
            {
                filter += " AND sa.WarehouseID = @wid ";
                prms.Add(DbHelper.P("@wid", wid.Value));
            }

            string term = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                filter += " AND (COALESCE(sa.BatchCode, '') LIKE @term OR p.ProductName LIKE @term OR p.ProductCode LIKE @term) ";
                prms.Add(DbHelper.P("@term", "%" + term + "%"));
            }

            // Query groups adjustments into sessions by BatchCode or rounded timestamp
            string sql = $@"
                SELECT 
                    COALESCE(NULLIF(sa.BatchCode, ''), 'ADJ-' + CONVERT(VARCHAR(10), sa.AdjDate, 112) + '-' + REPLACE(CONVERT(VARCHAR(8), sa.AdjDate, 108), ':', '')) AS OpCode,
                    MIN(sa.AdjDate) AS FirstDate,
                    MAX(sa.AdjDate) AS LastDate,
                    w.WarehouseName,
                    COALESCE(e.EmpName, N'المدير العام') AS CreatedBy,
                    COUNT(*) AS TotalItems,
                    SUM(CASE WHEN sa.ActualQty > sa.BookQty THEN 1 ELSE 0 END) AS SurplusItems,
                    SUM(CASE WHEN sa.ActualQty < sa.BookQty THEN 1 ELSE 0 END) AS ShortageItems,
                    SUM(CASE WHEN sa.ActualQty > sa.BookQty THEN (sa.ActualQty - sa.BookQty) ELSE 0 END) AS TotalSurplusQty
                FROM StockAdjustments sa
                JOIN Products p ON sa.ProductID = p.ProductID
                JOIN Warehouses w ON sa.WarehouseID = w.WarehouseID
                LEFT JOIN Employees e ON sa.CreatedBy = e.EmpID
                WHERE sa.AdjDate BETWEEN @f AND @t {filter}
                GROUP BY 
                    COALESCE(NULLIF(sa.BatchCode, ''), 'ADJ-' + CONVERT(VARCHAR(10), sa.AdjDate, 112) + '-' + REPLACE(CONVERT(VARCHAR(8), sa.AdjDate, 108), ':', '')),
                    w.WarehouseName,
                    e.EmpName
                ORDER BY MIN(sa.AdjDate) DESC";

            try
            {
                var dt = DbHelper.Query(sql, prms.ToArray());
                foreach (DataRow r in dt.Rows)
                {
                    string opCode = r["OpCode"].ToString();
                    DateTime dtOp = Convert.ToDateTime(r["FirstDate"]);
                    string wName = r["WarehouseName"].ToString();
                    string user = r["CreatedBy"].ToString();
                    int total = Convert.ToInt32(r["TotalItems"]);
                    int surplus = Convert.ToInt32(r["SurplusItems"]);
                    int shortage = Convert.ToInt32(r["ShortageItems"]);
                    decimal surplusQty = Convert.ToDecimal(r["TotalSurplusQty"]);

                    int ri = dgSessions.Rows.Add(
                        opCode,
                        dtOp.ToString("dd/MM/yyyy HH:mm"),
                        wName,
                        user,
                        total.ToString("N0"),
                        surplus > 0 ? $"+{surplus} صنف" : "0",
                        shortage > 0 ? $"-{shortage} صنف" : "0",
                        surplusQty > 0 ? surplusQty.ToString("N2") : "0.00"
                    );

                    if (surplus > 0) dgSessions.Rows[ri].Cells["SurplusItems"].Style.ForeColor = Color.DarkGreen;
                    if (shortage > 0) dgSessions.Rows[ri].Cells["ShortageItems"].Style.ForeColor = Color.OrangeRed;

                    if (!string.IsNullOrEmpty(_selectedBatchCode) && opCode == _selectedBatchCode)
                    {
                        dgSessions.ClearSelection();
                        dgSessions.Rows[ri].Selected = true;
                    }
                }

                lblSessionsCount.Text = $"📋 عمليات وجلسات الجرد المنفذة: ({dgSessions.Rows.Count:N0} عملية)";

                if (dgSessions.Rows.Count > 0 && dgSessions.SelectedRows.Count == 0)
                {
                    dgSessions.Rows[0].Selected = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل سجل عمليات الجرد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgSessions_SelectionChanged(object sender, EventArgs e)
        {
            if (dgSessions.SelectedRows.Count == 0)
            {
                dgDetails.Rows.Clear();
                lblSessionSummary.Text = "";
                return;
            }

            var r = dgSessions.SelectedRows[0];
            _selectedBatchCode = r.Cells["BatchCode"].Value?.ToString() ?? "";
            _selectedWarehouseName = r.Cells["WarehouseName"].Value?.ToString() ?? "";
            _selectedUserName = r.Cells["CreatedBy"].Value?.ToString() ?? "";

            LoadSessionDetails(_selectedBatchCode);
        }

        private void LoadSessionDetails(string batchCode)
        {
            dgDetails.Rows.Clear();
            if (string.IsNullOrEmpty(batchCode)) return;

            string sql = @"
                SELECT 
                    sa.ProductID,
                    p.ProductCode,
                    p.ProductName,
                    COALESCE(p.ShelfLocation, N'') AS ShelfLocation,
                    COALESCE(sa.UnitName, p.Unit, N'') AS Unit,
                    COALESCE(p.SalePrice, 0) AS SalePrice,
                    sa.BookQty,
                    sa.ActualQty,
                    (sa.ActualQty - sa.BookQty) AS DiffQty,
                    sa.Notes,
                    sa.AdjDate
                FROM StockAdjustments sa
                JOIN Products p ON sa.ProductID = p.ProductID
                WHERE (sa.BatchCode = @code OR ('ADJ-' + CONVERT(VARCHAR(10), sa.AdjDate, 112) + '-' + REPLACE(CONVERT(VARCHAR(8), sa.AdjDate, 108), ':', '')) = @code)
                ORDER BY (sa.ActualQty - sa.BookQty) DESC, p.ProductName ASC";

            try
            {
                var dt = DbHelper.Query(sql, DbHelper.P("@code", batchCode));
                int plusCount = 0;
                int minusCount = 0;
                decimal totalPlusQty = 0;

                foreach (DataRow r in dt.Rows)
                {
                    decimal book = Convert.ToDecimal(r["BookQty"]);
                    decimal actual = Convert.ToDecimal(r["ActualQty"]);
                    decimal diff = Convert.ToDecimal(r["DiffQty"]);
                    decimal price = Convert.ToDecimal(r["SalePrice"]);

                    if (diff > 0) { plusCount++; totalPlusQty += diff; }
                    else if (diff < 0) { minusCount++; }

                    int ri = dgDetails.Rows.Add(
                        r["ProductID"],
                        r["ProductCode"],
                        r["ProductName"],
                        r["ShelfLocation"],
                        r["Unit"],
                        price.ToString("N2"),
                        book.ToString("N3"),
                        actual.ToString("N3"),
                        (diff > 0 ? "+" : "") + diff.ToString("N3"),
                        r["Notes"]
                    );

                    if (diff > 0)
                    {
                        dgDetails.Rows[ri].Cells["DiffQty"].Style.ForeColor = Color.DarkGreen;
                        dgDetails.Rows[ri].Cells["DiffQty"].Style.BackColor = Color.FromArgb(235, 255, 240);
                    }
                    else if (diff < 0)
                    {
                        dgDetails.Rows[ri].Cells["DiffQty"].Style.ForeColor = Color.DarkRed;
                        dgDetails.Rows[ri].Cells["DiffQty"].Style.BackColor = Color.FromArgb(255, 240, 240);
                    }
                }

                lblDetailsTitle.Text = $"🔍 تفاصيل العملية [{batchCode}] - المخزن: {_selectedWarehouseName} (إجمالي {dt.Rows.Count} صنف)";
                lblSessionSummary.Text = $"🏷️ أصناف الزيادة القابلة للطباعة: {plusCount} صنف (إجمالي كمية +{totalPlusQty:N2})";
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل تفاصيل العملية:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrintOperationBarcodes_Click(object sender, EventArgs e)
        {
            if (dgDetails.Rows.Count == 0)
            {
                MessageBox.Show("من فضلك اختر عملية جرد تحتوي على أصناف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var itemsToPrint = new List<BarcodePrintItem>();
            foreach (DataGridViewRow r in dgDetails.Rows)
            {
                if (r.Cells["ProductID"].Value == null) continue;
                string diffStr = r.Cells["DiffQty"].Value?.ToString()?.Replace("+", "").Trim() ?? "0";
                if (decimal.TryParse(diffStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal diff) && diff > 0)
                {
                    int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
                    string code = r.Cells["ProductCode"].Value?.ToString() ?? "";
                    string name = r.Cells["ProductName"].Value?.ToString() ?? "";
                    string shelf = r.Cells["ShelfLocation"].Value?.ToString() ?? "";
                    decimal.TryParse(r.Cells["SalePrice"].Value?.ToString(), out decimal price);

                    int qty = (int)Math.Ceiling(diff);
                    if (qty <= 0) qty = 1;

                    itemsToPrint.Add(new BarcodePrintItem
                    {
                        ProductID = pid,
                        ProductCode = code,
                        ProductName = name,
                        Price = price,
                        PrintQty = qty,
                        ShelfLocation = shelf
                    });
                }
            }

            if (itemsToPrint.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف بها زيادة بالكميات (+فائض) في هذه العملية المحددة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            new FrmBulkPrintBarcodes(itemsToPrint).ShowDialog(this);
        }

        private int _printRowIndex = 0;
        private int _printPageNum = 1;

        private void BtnPrintOperationReport_Click(object sender, EventArgs e)
        {
            if (dgDetails.Rows.Count == 0)
            {
                MessageBox.Show("من فضلك اختر عملية جرد أولاً لمعاينتها وطباعتها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _printRowIndex = 0;
            _printPageNum = 1;

            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);

            pd.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var boldBig = new Font("Arial", 15, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var boldHead = new Font("Arial", 9.5f, FontStyle.Bold);
                var normal = new Font("Arial", 9.5f, FontStyle.Regular);
                var small = new Font("Arial", 8.5f, FontStyle.Regular);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRtlRight = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap };

                int startX = 23;
                int pageW = 780;
                int y = 25;

                // Header
                string company = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
                g.DrawString(company, bold, Brushes.DarkBlue, new RectangleF(startX, y, pageW, 22), sfCenter); y += 24;
                g.DrawString($"تقرير تسوية عملية الجرد المخزني [{_selectedBatchCode}]", boldBig, Brushes.Black, new RectangleF(startX, y, pageW, 28), sfCenter); y += 28;
                g.DrawString($"المستودع: {_selectedWarehouseName}   |   القائم بالجرد: {_selectedUserName}   |   تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", small, Brushes.DarkSlateGray, new RectangleF(startX, y, pageW, 20), sfCenter); y += 22;

                g.DrawLine(new Pen(Color.FromArgb(24, 43, 73), 1.5f), startX, y, startX + pageW, y); y += 8;

                // Table Columns (RTL): الكود (75), اسم الصنف (310), الرف (65), الوحدة (50), الدفتري (70), الفعلي (70), الفارق (70), السعر (70) = 780
                int[] colW = { 75, 310, 65, 50, 70, 70, 70, 70 };
                string[] headers = { "الكود", "اسم الصنف", "الرف", "الوحدة", "الدفتري", "الفعلي", "الفارق", "سعر البيع" };

                int headH = 28;
                int rowH = 25;

                g.FillRectangle(new SolidBrush(Color.FromArgb(24, 43, 73)), startX, y, pageW, headH);
                int curX = startX + pageW;
                for (int i = 0; i < headers.Length; i++)
                {
                    curX -= colW[i];
                    g.DrawString(headers[i], boldHead, Brushes.White, new RectangleF(curX, y, colW[i], headH), sfCenter);
                }
                y += headH;

                int maxY = 1080;
                var penGrid = new Pen(Color.FromArgb(190, 205, 220), 1f);

                while (_printRowIndex < dgDetails.Rows.Count)
                {
                    var r = dgDetails.Rows[_printRowIndex];
                    string code = r.Cells["ProductCode"].Value?.ToString() ?? "";
                    string name = r.Cells["ProductName"].Value?.ToString() ?? "";
                    string shelf = r.Cells["ShelfLocation"].Value?.ToString() ?? "";
                    string unit = r.Cells["Unit"].Value?.ToString() ?? "";
                    string book = r.Cells["BookQty"].Value?.ToString() ?? "0";
                    string actual = r.Cells["ActualQty"].Value?.ToString() ?? "0";
                    string diff = r.Cells["DiffQty"].Value?.ToString() ?? "0";
                    string price = r.Cells["SalePrice"].Value?.ToString() ?? "0";

                    Brush rowBg = (_printRowIndex % 2 == 1) ? new SolidBrush(Color.FromArgb(248, 250, 252)) : Brushes.White;
                    g.FillRectangle(rowBg, startX, y, pageW, rowH);
                    g.DrawRectangle(penGrid, startX, y, pageW, rowH);

                    curX = startX + pageW;

                    // Code
                    curX -= colW[0]; g.DrawString(code, small, Brushes.Black, new RectangleF(curX, y, colW[0], rowH), sfCenter);
                    // Name
                    curX -= colW[1]; g.DrawString(name, normal, Brushes.Black, new RectangleF(curX + 4, y, colW[1] - 8, rowH), sfRtlRight);
                    // Shelf
                    curX -= colW[2]; g.DrawString(shelf, small, Brushes.Black, new RectangleF(curX, y, colW[2], rowH), sfCenter);
                    // Unit
                    curX -= colW[3]; g.DrawString(unit, small, Brushes.Black, new RectangleF(curX, y, colW[3], rowH), sfCenter);
                    // Book
                    curX -= colW[4]; g.DrawString(book, small, Brushes.Black, new RectangleF(curX, y, colW[4], rowH), sfCenter);
                    // Actual
                    curX -= colW[5]; g.DrawString(actual, small, Brushes.Black, new RectangleF(curX, y, colW[5], rowH), sfCenter);
                    // Diff
                    curX -= colW[6];
                    Brush diffB = diff.StartsWith("+") ? Brushes.Green : (diff.StartsWith("-") ? Brushes.Red : Brushes.Black);
                    g.DrawString(diff, boldHead, diffB, new RectangleF(curX, y, colW[6], rowH), sfCenter);
                    // Price
                    curX -= colW[7]; g.DrawString(price, small, Brushes.Black, new RectangleF(curX, y, colW[7], rowH), sfCenter);

                    y += rowH;
                    _printRowIndex++;

                    if (y >= maxY && _printRowIndex < dgDetails.Rows.Count)
                    {
                        g.DrawString($"صفحة {_printPageNum}", small, Brushes.Gray, startX + pageW - 70, ev.PageBounds.Height - 35);
                        _printPageNum++;
                        ev.HasMorePages = true;
                        return;
                    }
                }

                ev.HasMorePages = false;
                y += 12;
                if (y < maxY)
                {
                    g.DrawLine(new Pen(Color.FromArgb(24, 43, 73), 1.5f), startX, y, startX + pageW, y); y += 6;
                    g.DrawString("أمين المخزن: .......................................         المسؤول عن الاعتماد: .......................................", bold, Brushes.Black, new RectangleF(startX, y, pageW, 25), sfRtlRight);
                }

                g.DrawString($"صفحة {_printPageNum}", small, Brushes.Gray, startX + pageW - 70, ev.PageBounds.Height - 35);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 950,
                Height = 800,
                Text = $"معاينة كشف عملية الجرد [{_selectedBatchCode}]"
            };
            preview.ShowDialog();
        }
    }
}

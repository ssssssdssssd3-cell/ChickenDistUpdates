using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmShortageNotebook : Form
    {
        private TabControl tabMain;
        private TabPage tabAutoShortages;
        private TabPage tabManualShortages;

        // Auto Grid controls
        private DataGridView dgAutoShortages;
        private TextBox txtAutoSearch;
        private Label lblAutoCount;

        // Manual Grid controls
        private DataGridView dgManualShortages;
        private TextBox txtManualSearch;
        private ComboBox cboStatusFilter;
        private Label lblManualCount;

        // Buttons
        private Button btnAddManual;
        private Button btnChangeStatus;
        private Button btnPrintNotebook;
        private Button btnCreatePurchaseOrder;
        private Button btnRefresh;

        public FrmShortageNotebook()
        {
            InitializeComponentCustom();
            LoadAllData();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "📓 كشكول النواقص والطلبات الخاصة";
            this.Size = new Size(1180, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlTop = Theme.MakeTitleBar("📓 كشكول النواقص والطلبات الخاصة", "متابعة الأنماط التلقائية للنواقص (الحد الأدنى للمخزون)، وتسجيل طلبات الموظفين والعملاء للأصناف غير المتوفرة.");
            this.Controls.Add(pnlTop);

            var pnlActions = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            btnAddManual = Theme.MakeButton("➕ إضافة طلب/نقص يدوي", 10, 10, 160, 36, Theme.Success);
            btnAddManual.Click += BtnAddManual_Click;
            pnlActions.Controls.Add(btnAddManual);

            btnChangeStatus = Theme.MakeButton("📝 تغيير الحالة", 180, 10, 120, 36, Color.FromArgb(41, 128, 185));
            btnChangeStatus.Click += BtnChangeStatus_Click;
            pnlActions.Controls.Add(btnChangeStatus);

            btnCreatePurchaseOrder = Theme.MakeButton("🛒 تحويل لأمر شراء", 310, 10, 140, 36, Theme.Primary);
            btnCreatePurchaseOrder.Click += BtnCreatePurchaseOrder_Click;
            pnlActions.Controls.Add(btnCreatePurchaseOrder);

            btnPrintNotebook = Theme.MakeButton("🖨️ طباعة الكشكول", 460, 10, 130, 36, Color.FromArgb(142, 68, 173));
            btnPrintNotebook.Click += BtnPrintNotebook_Click;
            pnlActions.Controls.Add(btnPrintNotebook);

            btnRefresh = Theme.MakeButton("🔄 تحديث", 600, 10, 95, 36, Color.FromArgb(70, 80, 95));
            btnRefresh.Click += (s, e) => LoadAllData();
            pnlActions.Controls.Add(btnRefresh);

            this.Controls.Add(pnlActions);

            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };

            // ----------------------------------------------------
            // Tab 1: Auto Shortages (Min Stock Limit)
            // ----------------------------------------------------
            tabAutoShortages = new TabPage("🚨 النواقص التلقائية (تجاوز الحد الأدنى)");
            tabAutoShortages.BackColor = Theme.BgMain;

            var pnlAutoFilter = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Theme.BgCard, Padding = new Padding(8) };
            var lblAutoSearch = new Label { Text = "🔍 بحث:", Location = new Point(10, 12), AutoSize = true, ForeColor = Theme.TextMain };
            txtAutoSearch = new TextBox { Location = new Point(60, 9), Width = 220, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            txtAutoSearch.TextChanged += (s, e) => LoadAutoShortages();
            lblAutoCount = new Label { Text = "إجمالي النواقص: 0 صنف", Location = new Point(300, 12), AutoSize = true, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            pnlAutoFilter.Controls.Add(lblAutoSearch);
            pnlAutoFilter.Controls.Add(txtAutoSearch);
            pnlAutoFilter.Controls.Add(lblAutoCount);
            tabAutoShortages.Controls.Add(pnlAutoFilter);

            dgAutoShortages = CreateStyledGrid();
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 90 });
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 200 });
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "القسم", FillWeight = 110 });
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 75 });
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStock", HeaderText = "الرصيد الحالي", FillWeight = 100 });
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinStockLimit", HeaderText = "الحد الأدنى", FillWeight = 100 });
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "DeficitQty", HeaderText = "الكمية المطلوبة لتغطية النقص", FillWeight = 140 });
            dgAutoShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "StatusAlert", HeaderText = "حالة الرصيد", FillWeight = 100 });

            tabAutoShortages.Controls.Add(dgAutoShortages);
            dgAutoShortages.BringToFront();

            // ----------------------------------------------------
            // Tab 2: Manual Shortages Notebook
            // ----------------------------------------------------
            tabManualShortages = new TabPage("✏️ الكشكول اليدوي (طلبات الأصناف والعملاء)");
            tabManualShortages.BackColor = Theme.BgMain;

            var pnlManualFilter = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Theme.BgCard, Padding = new Padding(8) };
            var lblManualSearch = new Label { Text = "🔍 بحث:", Location = new Point(10, 12), AutoSize = true, ForeColor = Theme.TextMain };
            txtManualSearch = new TextBox { Location = new Point(60, 9), Width = 180, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            txtManualSearch.TextChanged += (s, e) => LoadManualShortages();

            var lblStatus = new Label { Text = "الحالة:", Location = new Point(255, 12), AutoSize = true, ForeColor = Theme.TextMain };
            cboStatusFilter = new ComboBox
            {
                Location = new Point(300, 9),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "جديد", "تم الطلب", "تم التوفير", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => LoadManualShortages();

            lblManualCount = new Label { Text = "إجمالي الطلبات: 0", Location = new Point(440, 12), AutoSize = true, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

            pnlManualFilter.Controls.Add(lblManualSearch);
            pnlManualFilter.Controls.Add(txtManualSearch);
            pnlManualFilter.Controls.Add(lblStatus);
            pnlManualFilter.Controls.Add(cboStatusFilter);
            pnlManualFilter.Controls.Add(lblManualCount);
            tabManualShortages.Controls.Add(pnlManualFilter);

            dgManualShortages = CreateStyledGrid();
            dgManualShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShortageID", Visible = false });
            dgManualShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "تاريخ التسجيل", FillWeight = 110 });
            dgManualShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف / الطلب", FillWeight = 190 });
            dgManualShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "RequestedQty", HeaderText = "الكمية المطلوبة", FillWeight = 85 });
            dgManualShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStock", HeaderText = "الرصيد وقت الطلب", FillWeight = 95 });
            dgManualShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 90 });
            dgManualShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "المصدر", FillWeight = 90 });
            dgManualShortages.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات (اسم العميل/الموديل)", FillWeight = 180 });

            tabManualShortages.Controls.Add(dgManualShortages);
            dgManualShortages.BringToFront();

            tabMain.TabPages.Add(tabAutoShortages);
            tabMain.TabPages.Add(tabManualShortages);
            this.Controls.Add(tabMain);

            pnlTop.SendToBack();
            pnlActions.SendToBack();
            tabMain.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private DataGridView CreateStyledGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
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
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(42, 48, 62) : Color.FromArgb(238, 243, 250),
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeight = 38,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };
            return grid;
        }

        private void LoadAllData()
        {
            LoadAutoShortages();
            LoadManualShortages();
        }

        private void LoadAutoShortages()
        {
            dgAutoShortages.Rows.Clear();
            string q = txtAutoSearch.Text.Trim();

            // حساب رصيد المخزن الفعلي وتصفية الأصناف التي تجاوزت الحد الأدنى
            var dtStock = DbHelper.Query(@"
                SELECT 
                    p.ProductID, p.ProductCode, p.ProductName, c.CategoryName, p.Unit,
                    ISNULL(p.MinStockLimit, 0) AS MinStockLimit,
                    (
                        ISNULL((SELECT SUM(Quantity) FROM Inventory WHERE ProductID = p.ProductID), 0)
                        + ISNULL((SELECT SUM(pi.Quantity * COALESCE(pi.Factor, 1.0)) FROM PurchaseItems pi JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID WHERE pi.ProductID = p.ProductID AND pu.IsPosted = 1), 0)
                        + ISNULL((SELECT SUM(ri.Quantity * COALESCE(ri.Factor, 1.0)) FROM ReturnItems ri WHERE ri.ProductID = p.ProductID), 0)
                        - ISNULL((SELECT SUM(si.Quantity * COALESCE(si.Factor, 1.0)) FROM SaleItems si JOIN Sales s ON si.SaleID = s.SaleID WHERE si.ProductID = p.ProductID AND s.IsPosted IN (0, 1)), 0)
                        - ISNULL((SELECT SUM(pri.Quantity * COALESCE(pri.Factor, 1.0)) FROM PurchaseReturnItems pri WHERE pri.ProductID = p.ProductID), 0)
                        - ISNULL((SELECT SUM(wli.Quantity * COALESCE(wli.Factor, 1.0)) FROM WastageLossItems wli WHERE wli.ProductID = p.ProductID), 0)
                    ) AS CurrentStock
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                WHERE p.IsActive = 1 AND p.MinStockLimit IS NOT NULL AND p.MinStockLimit > 0
                ORDER BY (ISNULL(p.MinStockLimit, 0) - 
                    (
                        ISNULL((SELECT SUM(Quantity) FROM Inventory WHERE ProductID = p.ProductID), 0)
                        + ISNULL((SELECT SUM(pi.Quantity * COALESCE(pi.Factor, 1.0)) FROM PurchaseItems pi JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID WHERE pi.ProductID = p.ProductID AND pu.IsPosted = 1), 0)
                        + ISNULL((SELECT SUM(ri.Quantity * COALESCE(ri.Factor, 1.0)) FROM ReturnItems ri WHERE ri.ProductID = p.ProductID), 0)
                        - ISNULL((SELECT SUM(si.Quantity * COALESCE(si.Factor, 1.0)) FROM SaleItems si JOIN Sales s ON si.SaleID = s.SaleID WHERE si.ProductID = p.ProductID AND s.IsPosted IN (0, 1)), 0)
                        - ISNULL((SELECT SUM(pri.Quantity * COALESCE(pri.Factor, 1.0)) FROM PurchaseReturnItems pri WHERE pri.ProductID = p.ProductID), 0)
                        - ISNULL((SELECT SUM(wli.Quantity * COALESCE(wli.Factor, 1.0)) FROM WastageLossItems wli WHERE wli.ProductID = p.ProductID), 0)
                    )) DESC");

            int count = 0;
            foreach (DataRow r in dtStock.Rows)
            {
                decimal stock = Convert.ToDecimal(r["CurrentStock"]);
                decimal minLimit = Convert.ToDecimal(r["MinStockLimit"]);

                if (stock <= minLimit)
                {
                    string pName = r["ProductName"].ToString();
                    string pCode = r["ProductCode"].ToString();
                    string catName = r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "-";
                    string unit = r["Unit"] != DBNull.Value ? r["Unit"].ToString() : "قطعة";

                    if (!string.IsNullOrWhiteSpace(q) &&
                        !pName.ToLower().Contains(q.ToLower()) &&
                        !pCode.ToLower().Contains(q.ToLower()) &&
                        !catName.ToLower().Contains(q.ToLower()))
                    {
                        continue;
                    }

                    decimal deficit = minLimit - stock;
                    if (deficit < 1) deficit = 1;

                    string statusAlert = stock <= 0 ? "🔴 نفذ بالكامل" : "🟡 تحت الحد الأدنى";

                    int ri = dgAutoShortages.Rows.Add(
                        r["ProductID"],
                        pCode,
                        pName,
                        catName,
                        unit,
                        stock.ToString("N2"),
                        minLimit.ToString("N2"),
                        deficit.ToString("N2"),
                        statusAlert
                    );

                    var row = dgAutoShortages.Rows[ri];
                    if (stock <= 0)
                    {
                        row.Cells["StatusAlert"].Style.ForeColor = Color.FromArgb(231, 76, 60);
                        row.Cells["StatusAlert"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    }
                    else
                    {
                        row.Cells["StatusAlert"].Style.ForeColor = Color.FromArgb(230, 126, 34);
                        row.Cells["StatusAlert"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    }

                    count++;
                }
            }

            lblAutoCount.Text = $"إجمالي النواقص الآلية: {count} صنف";
        }

        private void LoadManualShortages()
        {
            dgManualShortages.Rows.Clear();
            string status = cboStatusFilter.SelectedItem?.ToString() ?? "الكل";
            string q = txtManualSearch.Text.Trim();

            string sql = @"
                SELECT ShortageID, CreatedDate, ProductName, RequestedQty, CurrentStock, Status, Source, Notes
                FROM ShortageNotebook
                WHERE 1=1 ";

            if (status != "الكل")
            {
                sql += " AND Status = @st ";
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                sql += " AND (ProductName LIKE @q OR Notes LIKE @q) ";
            }
            sql += " ORDER BY ShortageID DESC";

            var dt = DbHelper.Query(sql, DbHelper.P("@st", status), DbHelper.P("@q", "%" + q + "%"));

            foreach (DataRow r in dt.Rows)
            {
                string st = r["Status"].ToString();
                DateTime cDate = Convert.ToDateTime(r["CreatedDate"]);

                int ri = dgManualShortages.Rows.Add(
                    r["ShortageID"],
                    cDate.ToString("yyyy/MM/dd HH:mm"),
                    r["ProductName"],
                    Convert.ToDecimal(r["RequestedQty"]).ToString("N2"),
                    Convert.ToDecimal(r["CurrentStock"]).ToString("N2"),
                    st,
                    r["Source"].ToString(),
                    r["Notes"] != DBNull.Value ? r["Notes"].ToString() : "-"
                );

                var row = dgManualShortages.Rows[ri];
                if (st == "جديد")
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(230, 126, 34);
                    row.Cells["Status"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (st == "تم الطلب")
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(41, 128, 185);
                    row.Cells["Status"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (st == "تم التوفير")
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(46, 204, 113);
                    row.Cells["Status"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (st == "ملغي")
                {
                    row.Cells["Status"].Style.ForeColor = Color.Gray;
                }
            }

            lblManualCount.Text = $"إجمالي طلبات الكشكول: {dt.Rows.Count}";
        }

        private void BtnAddManual_Click(object sender, EventArgs e)
        {
            using (var dlg = new FrmAddShortageItem())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    tabMain.SelectedTab = tabManualShortages;
                    LoadManualShortages();
                }
            }
        }

        private void BtnChangeStatus_Click(object sender, EventArgs e)
        {
            if (tabMain.SelectedTab == tabAutoShortages)
            {
                if (dgAutoShortages.SelectedRows.Count == 0)
                {
                    MessageBox.Show("يرجى اختيار صنف من النواقص التلقائية لنقله للكشكول أو متابعته", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string pName = dgAutoShortages.SelectedRows[0].Cells["ProductName"].Value.ToString();
                int pId = Convert.ToInt32(dgAutoShortages.SelectedRows[0].Cells["ProductID"].Value);
                decimal stock = Convert.ToDecimal(dgAutoShortages.SelectedRows[0].Cells["CurrentStock"].Value);
                decimal deficit = Convert.ToDecimal(dgAutoShortages.SelectedRows[0].Cells["DeficitQty"].Value);

                DbHelper.Execute(@"
                    INSERT INTO ShortageNotebook (ProductID, ProductName, CurrentStock, MinStockLimit, RequestedQty, Status, Source, CreatedBy)
                    VALUES (@pid, @pname, @stock, @min, @req, N'تم الطلب', N'آلي (تجاوز الحد الأدنى)', @by)",
                    DbHelper.P("@pid", pId),
                    DbHelper.P("@pname", pName),
                    DbHelper.P("@stock", stock),
                    DbHelper.P("@min", Convert.ToDecimal(dgAutoShortages.SelectedRows[0].Cells["MinStockLimit"].Value)),
                    DbHelper.P("@req", deficit),
                    DbHelper.P("@by", Session.EmpID)
                );

                MessageBox.Show($"تم نقل الصنف [{pName}] إلى كشكول الطلبات وحالته (تم الطلب من المورد).", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabMain.SelectedTab = tabManualShortages;
                LoadManualShortages();
            }
            else
            {
                if (dgManualShortages.SelectedRows.Count == 0)
                {
                    MessageBox.Show("يرجى تحديد طلب من الجدول لتغيير حالته", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int id = Convert.ToInt32(dgManualShortages.SelectedRows[0].Cells["ShortageID"].Value);
                string currentStatus = dgManualShortages.SelectedRows[0].Cells["Status"].Value.ToString();

                using (var dlg = new Form())
                {
                    dlg.Text = "تغيير حالة الطلب";
                    dlg.Size = new Size(340, 220);
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.BackColor = Theme.BgMain;
                    dlg.RightToLeft = RightToLeft.Yes;
                    dlg.RightToLeftLayout = true;
                    dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dlg.MaximizeBox = false;
                    dlg.MinimizeBox = false;

                    var lbl = new Label { Text = "اختر الحالة الجديدة:", Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontMain };
                    dlg.Controls.Add(lbl);

                    var cbo = new ComboBox
                    {
                        Location = new Point(20, 50),
                        Width = 280,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        BackColor = Theme.BgInput,
                        ForeColor = Theme.TextMain,
                        Font = Theme.FontMain,
                        FlatStyle = FlatStyle.Flat
                    };
                    cbo.Items.AddRange(new object[] { "جديد", "تم الطلب", "تم التوفير", "ملغي" });
                    cbo.SelectedItem = currentStatus;
                    dlg.Controls.Add(cbo);

                    var btnSave = Theme.MakeButton("💾 حفظ", 20, 110, 130, 36, Theme.Success);
                    btnSave.Click += (s2, e2) =>
                    {
                        string newSt = cbo.SelectedItem.ToString();
                        DbHelper.Execute("UPDATE ShortageNotebook SET Status=@st WHERE ShortageID=@id", DbHelper.P("@st", newSt), DbHelper.P("@id", id));
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    };
                    dlg.Controls.Add(btnSave);

                    var btnCancel = Theme.MakeButton("❌ إلغاء", 170, 110, 130, 36, Theme.Danger);
                    btnCancel.Click += (s2, e2) => dlg.Close();
                    dlg.Controls.Add(btnCancel);

                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadManualShortages();
                    }
                }
            }
        }

        private void BtnCreatePurchaseOrder_Click(object sender, EventArgs e)
        {
            MessageBox.Show("تم تحضير قائمة النواقص، يمكنك فتح شاشة (المشتريات / فاتورة شراء جديدة) وتوليد الطلبية بناءً عليها.", "أمر الشراء والتوريد", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnPrintNotebook_Click(object sender, EventArgs e)
        {
            try
            {
                using (var pd = new PrintDocument())
                {
                    pd.PrintPage += (s, e2) =>
                    {
                        Graphics g = e2.Graphics;
                        Font fontTitle = new Font("Segoe UI", 14f, FontStyle.Bold);
                        Font fontHeader = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                        Font fontBody = new Font("Segoe UI", 9.5f);
                        Font fontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);

                        float y = 20;
                        float leftMargin = 20;
                        float rightMargin = e2.PageBounds.Width - 20;
                        float contentWidth = rightMargin - leftMargin;

                        StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                        StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };
                        StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near };

                        g.DrawString(AppConfig.CompanyName ?? "المحل التجاري", fontTitle, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 30), sfCenter);
                        y += 30;
                        g.DrawString("📓 كشكول النواقص وطلبات الأصناف", fontHeader, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 25), sfCenter);
                        y += 30;

                        g.DrawString($"التاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}", fontBody, Brushes.Black, rightMargin, y, sfRight);
                        y += 25;

                        g.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
                        y += 10;

                        g.DrawString("الصنف / الطلب", fontHeader, Brushes.Black, rightMargin, y, sfRight);
                        g.DrawString("الكمية المطلوب توفيرها", fontHeader, Brushes.Black, leftMargin + 120, y, sfLeft);
                        g.DrawString("الحالة / الرصيد", fontHeader, Brushes.Black, leftMargin, y, sfLeft);
                        y += 24;

                        g.DrawLine(Pens.Gray, leftMargin, y, rightMargin, y);
                        y += 6;

                        if (tabMain.SelectedTab == tabAutoShortages)
                        {
                            foreach (DataGridViewRow row in dgAutoShortages.Rows)
                            {
                                string name = row.Cells["ProductName"].Value.ToString();
                                string qty = row.Cells["DeficitQty"].Value.ToString();
                                string st = row.Cells["StatusAlert"].Value.ToString();

                                g.DrawString(name, fontBody, Brushes.Black, rightMargin, y, sfRight);
                                g.DrawString(qty, fontBody, Brushes.Black, leftMargin + 120, y, sfLeft);
                                g.DrawString(st, fontBody, Brushes.Black, leftMargin, y, sfLeft);
                                y += 22;
                            }
                        }
                        else
                        {
                            foreach (DataGridViewRow row in dgManualShortages.Rows)
                            {
                                string name = row.Cells["ProductName"].Value.ToString();
                                string qty = row.Cells["RequestedQty"].Value.ToString();
                                string st = row.Cells["Status"].Value.ToString();

                                g.DrawString(name, fontBody, Brushes.Black, rightMargin, y, sfRight);
                                g.DrawString(qty, fontBody, Brushes.Black, leftMargin + 120, y, sfLeft);
                                g.DrawString(st, fontBody, Brushes.Black, leftMargin, y, sfLeft);
                                y += 22;
                            }
                        }

                        g.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
                        y += 15;

                        g.DrawString("توقيع مسؤول المشتريات: ..........................", fontBold, Brushes.Black, rightMargin, y, sfRight);
                    };

                    using (var dlg = new PrintPreviewDialog { Document = pd, Width = 800, Height = 600, StartPosition = FormStartPosition.CenterParent })
                    {
                        dlg.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء طباعة الكشكول:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// نافذة إضافة صنف أو طلب يدوي للكشكول
    /// </summary>
    public class FrmAddShortageItem : Form
    {
        private ComboBox cboProduct;
        private TextBox txtProductName;
        private NumericUpDown nudQty;
        private TextBox txtNotes;
        private CheckBox chkCustomProduct;

        public FrmAddShortageItem()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "➕ إضافة صنف / طلب لكشكول النواقص";
            this.Size = new Size(460, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblTitle = Theme.MakeTitleBar("➕ إضافة صنف / طلب كشكول", "تسجيل طلب صنف غير موجود أو نقص بناءً على طلب موظف أو عميل.");
            this.Controls.Add(lblTitle);

            chkCustomProduct = new CheckBox
            {
                Text = "صنف غير مسجل بالنظام (كتابة اسم يدوي)",
                Location = new Point(25, 70),
                AutoSize = true,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            chkCustomProduct.CheckedChanged += (s, e) =>
            {
                cboProduct.Enabled = !chkCustomProduct.Checked;
                txtProductName.Enabled = chkCustomProduct.Checked;
                if (chkCustomProduct.Checked) txtProductName.Focus();
            };
            this.Controls.Add(chkCustomProduct);

            var lblProduct = new Label { Text = "اختيار صنف مسجل:", Location = new Point(25, 105), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblProduct);

            cboProduct = new ComboBox
            {
                Location = new Point(25, 130),
                Width = 390,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                FlatStyle = FlatStyle.Flat
            };
            cboProduct.Items.Add(new ComboItem(0, "-- اختر صنف من القائمة --"));
            var dtP = DbHelper.Query("SELECT ProductID, ProductName FROM Products WHERE IsActive=1 ORDER BY ProductName");
            foreach (DataRow r in dtP.Rows)
            {
                cboProduct.Items.Add(new ComboItem(Convert.ToInt32(r["ProductID"]), r["ProductName"].ToString()));
            }
            cboProduct.SelectedIndex = 0;
            this.Controls.Add(cboProduct);

            txtProductName = new TextBox
            {
                Location = new Point(25, 130),
                Width = 390,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Enabled = false,
                Visible = true
            };
            this.Controls.Add(txtProductName);
            txtProductName.SendToBack(); // By default cbo is enabled

            var lblQty = new Label { Text = "الكمية المطلوبة:", Location = new Point(25, 170), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblQty);

            nudQty = new NumericUpDown
            {
                Location = new Point(25, 195),
                Width = 140,
                Minimum = 1,
                Maximum = 10000,
                Value = 1,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            this.Controls.Add(nudQty);

            var lblNotes = new Label { Text = "ملاحظات (اسم العميل / التفاصيل):", Location = new Point(25, 235), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblNotes);

            txtNotes = new TextBox
            {
                Location = new Point(25, 260),
                Width = 390,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            this.Controls.Add(txtNotes);

            var btnSave = Theme.MakeButton("💾 حفظ في الكشكول", 25, 305, 180, 36, Theme.Success);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            var btnCancel = Theme.MakeButton("❌ إلغاء", 235, 305, 180, 36, Theme.Danger);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string pName = "";
            int? pId = null;

            if (chkCustomProduct.Checked)
            {
                pName = txtProductName.Text.Trim();
                if (string.IsNullOrWhiteSpace(pName))
                {
                    MessageBox.Show("يرجى كتابة اسم الصنف المطلوب", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    pId = ci.ID;
                    pName = ci.Name;
                }
                else
                {
                    MessageBox.Show("يرجى اختيار صنف من القائمة أو تفعيل كتابة اسم يدوي", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                decimal stock = 0;
                if (pId.HasValue && pId.Value > 0)
                {
                    var res = DbHelper.Scalar("SELECT ISNULL(SUM(Quantity), 0) FROM Inventory WHERE ProductID=@id", DbHelper.P("@id", pId.Value));
                    if (res != null && res != DBNull.Value) stock = Convert.ToDecimal(res);
                }

                DbHelper.Execute(@"
                    INSERT INTO ShortageNotebook (ProductID, ProductName, CurrentStock, RequestedQty, Notes, Status, Source, CreatedBy)
                    VALUES (@pid, @pname, @stock, @req, @notes, N'جديد', N'يدوي (طلب عميل/موظف)', @by)",
                    DbHelper.P("@pid", pId.HasValue ? (object)pId.Value : DBNull.Value),
                    DbHelper.P("@pname", pName),
                    DbHelper.P("@stock", stock),
                    DbHelper.P("@req", nudQty.Value),
                    DbHelper.P("@notes", string.IsNullOrWhiteSpace(txtNotes.Text) ? (object)DBNull.Value : txtNotes.Text.Trim()),
                    DbHelper.P("@by", Session.EmpID)
                );

                MessageBox.Show("تمت إضافة الطلب لكشكول النواقص بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

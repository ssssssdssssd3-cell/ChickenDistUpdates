using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// تقرير وسجل حركات وتعديلات عمليات التصنيع (ثابت ومخصص)
    /// </summary>
    public class FrmProductionReports : Form
    {
        // Filters
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboTypeFilter, cboStatusFilter, cboWarehouseFilter;
        private TextBox txtSearch;
        private Button btnSearch, btnRefresh;

        // Master Grid
        private DataGridView dgOrders;

        // Details Panel (Tabs: Items & History)
        private TabControl tabDetails;
        private DataGridView dgItemsDetail;
        private DataGridView dgHistoryDetail;

        // Summary Bar
        private Label lblTotalOrdersCount;
        private Label lblTotalCostSum;
        private Label lblExtraExpensesSum;

        // Action Buttons
        private Button btnOpenOrder;
        private Button btnPrintReport;
        private Button btnExportExcel;

        public FrmProductionReports()
        {
            InitUI();
            LoadWarehousesFilter();
            ApplyFilters();
        }

        private void InitUI()
        {
            this.Text = "📊 سجل وتقارير حركات التصنيع والتشغيل الشامل";
            this.Size = new Size(1220, 780);
            this.MinimumSize = new Size(1020, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── Top Filters Panel ──
            var pnlFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Theme.BgCard,
                Padding = new Padding(12)
            };
            this.Controls.Add(pnlFilters);

            var lblTitle = new Label
            {
                Text = "📊 سجل حركات التصنيع والتعديلات والتعليق",
                Location = new Point(12, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlFilters.Controls.Add(lblTitle);

            // Row 1 Filters: Dates, Type, Status
            var lblFrom = new Label { Text = "من تاريخ:", Location = new Point(12, 42), AutoSize = true };
            pnlFilters.Controls.Add(lblFrom);

            dtpFrom = new DateTimePicker
            {
                Location = new Point(75, 39),
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30)
            };
            pnlFilters.Controls.Add(dtpFrom);

            var lblTo = new Label { Text = "إلى تاريخ:", Location = new Point(205, 42), AutoSize = true };
            pnlFilters.Controls.Add(lblTo);

            dtpTo = new DateTimePicker
            {
                Location = new Point(265, 39),
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            pnlFilters.Controls.Add(dtpTo);

            var lblType = new Label { Text = "نوع التصنيع:", Location = new Point(395, 42), AutoSize = true };
            pnlFilters.Controls.Add(lblType);

            cboTypeFilter = new ComboBox
            {
                Location = new Point(475, 39),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboTypeFilter.Items.AddRange(new object[] { "الكل", "تصنيع ثابت", "تصنيع مخصص" });
            cboTypeFilter.SelectedIndex = 0;
            pnlFilters.Controls.Add(cboTypeFilter);

            var lblStatus = new Label { Text = "الحالة:", Location = new Point(625, 42), AutoSize = true };
            pnlFilters.Controls.Add(lblStatus);

            cboStatusFilter = new ComboBox
            {
                Location = new Point(670, 39),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "تحت التحضير (معلقة)", "مكتمل ومرحل", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            pnlFilters.Controls.Add(cboStatusFilter);

            var lblWh = new Label { Text = "المخزن:", Location = new Point(830, 42), AutoSize = true };
            pnlFilters.Controls.Add(lblWh);

            cboWarehouseFilter = new ComboBox
            {
                Location = new Point(885, 39),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlFilters.Controls.Add(cboWarehouseFilter);

            // Row 2: Search Box & Buttons
            var lblSearch = new Label { Text = "🔍 بحث برقم الأمر أو المنتج:", Location = new Point(12, 75), AutoSize = true };
            pnlFilters.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location = new Point(180, 72),
                Width = 320,
                Height = 30,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ApplyFilters(); };
            pnlFilters.Controls.Add(txtSearch);

            btnSearch = Theme.MakeButton("بحث", 510, 70, 100, 32, Theme.Primary);
            btnSearch.Click += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(btnSearch);

            btnRefresh = Theme.MakeButton("تحديث الكل", 620, 70, 110, 32, Color.FromArgb(51, 65, 85));
            btnRefresh.Click += (s, e) => { txtSearch.Clear(); ApplyFilters(); };
            pnlFilters.Controls.Add(btnRefresh);

            // ── Splitter / SplitContainer for Master Grid & Detail Tabs ──
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 380,
                BackColor = Theme.BgMain
            };
            this.Controls.Add(split);
            split.BringToFront();

            // ── Master Grid (Orders) ──
            dgOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgOrders.SelectionChanged += (s, e) => LoadSelectedOrderDetails();
            dgOrders.CellDoubleClick += (s, e) => OpenSelectedOrderForm();
            split.Panel1.Controls.Add(dgOrders);

            // ── Detail Tabs (Panel 2) ──
            tabDetails = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontMain
            };
            split.Panel2.Controls.Add(tabDetails);

            // Tab 1: Raw Materials
            var tabItems = new TabPage("📦 المواد الخام المستهلكة في هذا الأمر");
            tabItems.BackColor = Theme.BgCard;
            dgItemsDetail = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            tabItems.Controls.Add(dgItemsDetail);
            tabDetails.TabPages.Add(tabItems);

            // Tab 2: Audit History
            var tabHistory = new TabPage("📑 سجل حركات وتعديل وتعليق الأمر (Audit Trail)");
            tabHistory.BackColor = Theme.BgCard;
            dgHistoryDetail = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            tabHistory.Controls.Add(dgHistoryDetail);
            tabDetails.TabPages.Add(tabHistory);

            // ── Bottom Summary & Action Bar ──
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(12)
            };
            this.Controls.Add(pnlBottom);

            lblTotalOrdersCount = new Label
            {
                Text = "عدد الأوامر: 0",
                Location = new Point(12, 10),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };
            pnlBottom.Controls.Add(lblTotalOrdersCount);

            lblTotalCostSum = new Label
            {
                Text = "إجمالي التكلفة: 0.00 ج.م",
                Location = new Point(160, 10),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(243, 198, 35)
            };
            pnlBottom.Controls.Add(lblTotalCostSum);

            lblExtraExpensesSum = new Label
            {
                Text = "إجمالي مصاريف التشغيل: 0.00 ج.م",
                Location = new Point(410, 10),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.Orange
            };
            pnlBottom.Controls.Add(lblExtraExpensesSum);

            btnOpenOrder = Theme.MakeButton("📂 فتح أمر التصنيع المختار", 680, 12, 180, 38, Theme.Primary);
            btnOpenOrder.Click += (s, e) => OpenSelectedOrderForm();
            pnlBottom.Controls.Add(btnOpenOrder);

            btnPrintReport = Theme.MakeButton("🖨️ طباعة التقرير", 870, 12, 140, 38, Color.FromArgb(40, 120, 180));
            btnPrintReport.Click += (s, e) => PrintReport();
            pnlBottom.Controls.Add(btnPrintReport);

            btnExportExcel = Theme.MakeButton("📊 تصدير Excel", 1020, 12, 140, 38, Color.FromArgb(22, 163, 74));
            btnExportExcel.Click += (s, e) => ExportToCsv();
            pnlBottom.Controls.Add(btnExportExcel);
        }

        private void LoadWarehousesFilter()
        {
            try
            {
                var dt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses ORDER BY WarehouseID ASC");
                var row = dt.NewRow();
                row["WarehouseID"] = 0;
                row["WarehouseName"] = "كل المخازن";
                dt.Rows.InsertAt(row, 0);

                cboWarehouseFilter.DataSource = dt;
                cboWarehouseFilter.DisplayMember = "WarehouseName";
                cboWarehouseFilter.ValueMember = "WarehouseID";
                cboWarehouseFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmProductionReports.LoadWarehousesFilter", ex);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                DateTime from = dtpFrom.Value;
                DateTime to = dtpTo.Value;

                string prodType = cboTypeFilter.SelectedIndex switch
                {
                    1 => "Fixed",
                    2 => "Custom",
                    _ => "All"
                };

                string status = cboStatusFilter.SelectedIndex switch
                {
                    1 => "InPreparation",
                    2 => "Completed",
                    3 => "Cancelled",
                    _ => "All"
                };

                int? wid = null;
                if (cboWarehouseFilter.SelectedValue != null && Convert.ToInt32(cboWarehouseFilter.SelectedValue) > 0)
                    wid = Convert.ToInt32(cboWarehouseFilter.SelectedValue);

                string search = txtSearch.Text.Trim();

                var dt = ProductionDAL.SearchProductionOrders(from, to, prodType, status, null, wid, search);
                dgOrders.DataSource = dt;

                ConfigureMasterGridColumns();
                CalculateSummary(dt);
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmProductionReports.ApplyFilters", ex);
            }
        }

        private void ConfigureMasterGridColumns()
        {
            if (dgOrders.Columns["ProductionID"] != null) dgOrders.Columns["ProductionID"].Visible = false;
            if (dgOrders.Columns["ProductionType"] != null) dgOrders.Columns["ProductionType"].Visible = false;
            if (dgOrders.Columns["Status"] != null) dgOrders.Columns["Status"].Visible = false;

            SetCol(dgOrders, "OrderCode", "كود الأمر", 120);
            SetCol(dgOrders, "ProductionTypeName", "نوع التصنيع", 100);
            SetCol(dgOrders, "FinishedProductCode", "كود المنتج", 100);
            SetCol(dgOrders, "FinishedProductName", "المنتج المصنع", 180);
            SetCol(dgOrders, "ProducedQty", "الكمية", 70);
            SetCol(dgOrders, "UnitName", "الوحدة", 60);
            SetCol(dgOrders, "RawMaterialsCost", "تكلفة الخامات", 100);
            SetCol(dgOrders, "ExtraExpenses", "مصاريف تشغيل", 100);
            SetCol(dgOrders, "TotalCost", "إجمالي التكلفة", 100);
            SetCol(dgOrders, "UnitCost", "تكلفة القطعة", 100);
            SetCol(dgOrders, "StatusName", "الحالة", 110);
            SetCol(dgOrders, "CreatedDate", "تاريخ الإنشاء", 110);
            SetCol(dgOrders, "UpdatedDate", "آخر تعديل", 110);
            SetCol(dgOrders, "CompletedDate", "تاريخ الإتمام", 110);
            SetCol(dgOrders, "WarehouseName", "المخزن", 110);
            SetCol(dgOrders, "CreatedByName", "المستخدم", 100);

            // Row Coloring
            foreach (DataGridViewRow r in dgOrders.Rows)
            {
                string st = r.Cells["StatusName"].Value?.ToString() ?? "";
                if (st.Contains("تحت التحضير"))
                {
                    r.DefaultCellStyle.ForeColor = Color.FromArgb(234, 88, 12);
                    r.DefaultCellStyle.SelectionBackColor = Color.FromArgb(124, 45, 18);
                }
                else if (st.Contains("مكتمل"))
                {
                    r.DefaultCellStyle.ForeColor = Color.FromArgb(22, 163, 74);
                }
                else if (st.Contains("ملغي"))
                {
                    r.DefaultCellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                }
            }
        }

        private static void SetCol(DataGridView dg, string name, string header, int width)
        {
            if (dg.Columns[name] != null)
            {
                dg.Columns[name].HeaderText = header;
                dg.Columns[name].Width = width;
            }
        }

        private void CalculateSummary(DataTable dt)
        {
            if (dt == null) return;
            lblTotalOrdersCount.Text = $"عدد الأوامر: {dt.Rows.Count}";

            decimal totalCost = 0;
            decimal totalExpenses = 0;

            foreach (DataRow r in dt.Rows)
            {
                if (r["Status"]?.ToString() != "Cancelled")
                {
                    totalCost += Convert.ToDecimal(r["TotalCost"] ?? 0);
                    totalExpenses += Convert.ToDecimal(r["ExtraExpenses"] ?? 0);
                }
            }

            lblTotalCostSum.Text = $"إجمالي التكلفة: {totalCost:N2} ج.م";
            lblExtraExpensesSum.Text = $"إجمالي مصاريف التشغيل: {totalExpenses:N2} ج.م";
        }

        private void LoadSelectedOrderDetails()
        {
            if (dgOrders.CurrentRow == null)
            {
                dgItemsDetail.DataSource = null;
                dgHistoryDetail.DataSource = null;
                return;
            }

            int pid = Convert.ToInt32(dgOrders.CurrentRow.Cells["ProductionID"].Value);

            // Load Items
            var dtItems = DbHelper.Query(@"
                SELECT poi.ItemID, p.ProductCode AS RawProductCode, p.ProductName AS RawProductName,
                       poi.Quantity, poi.UnitName, poi.UnitCost, poi.TotalCost, poi.Notes
                FROM ProductionOrderItems poi
                JOIN Products p ON poi.RawProductID = p.ProductID
                WHERE poi.ProductionID = @id",
                DbHelper.P("@id", pid));

            dgItemsDetail.DataSource = dtItems;
            if (dgItemsDetail.Columns["ItemID"] != null) dgItemsDetail.Columns["ItemID"].Visible = false;
            SetCol(dgItemsDetail, "RawProductCode", "كود الخام", 120);
            SetCol(dgItemsDetail, "RawProductName", "اسم المادة الخام المستهلكة", 250);
            SetCol(dgItemsDetail, "Quantity", "الكمية المخصومة", 110);
            SetCol(dgItemsDetail, "UnitName", "الوحدة", 80);
            SetCol(dgItemsDetail, "UnitCost", "سعر التكلفة", 110);
            SetCol(dgItemsDetail, "TotalCost", "إجمالي التكلفة", 120);
            SetCol(dgItemsDetail, "Notes", "ملاحظات", 180);

            // Load History
            var dtHist = ProductionDAL.GetOrderHistory(pid);
            dgHistoryDetail.DataSource = dtHist;
            if (dgHistoryDetail.Columns["HistoryID"] != null) dgHistoryDetail.Columns["HistoryID"].Visible = false;
            if (dgHistoryDetail.Columns["ProductionID"] != null) dgHistoryDetail.Columns["ProductionID"].Visible = false;
            if (dgHistoryDetail.Columns["ActionType"] != null) dgHistoryDetail.Columns["ActionType"].Visible = false;
            SetCol(dgHistoryDetail, "ActionTypeName", "نوع الإجراء", 180);
            SetCol(dgHistoryDetail, "ActionDate", "التاريخ والوقت", 160);
            SetCol(dgHistoryDetail, "ActionBy", "بواسطة", 130);
            SetCol(dgHistoryDetail, "Details", "تفاصيل الإجراء والتعديلات", 400);
        }

        private void OpenSelectedOrderForm()
        {
            if (dgOrders.CurrentRow == null) return;
            int pid = Convert.ToInt32(dgOrders.CurrentRow.Cells["ProductionID"].Value);
            string pType = dgOrders.CurrentRow.Cells["ProductionType"].Value?.ToString();

            if (pType == "Custom")
            {
                var frm = new FrmCustomProduction(pid);
                frm.ShowDialog();
                ApplyFilters();
            }
            else
            {
                var frm = new FrmFixedProduction(pid);
                frm.ShowDialog();
                ApplyFilters();
            }
        }

        private void PrintReport()
        {
            if (dgOrders.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pd = new PrintDocument();
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                float y = 40;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 10f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);

                g.DrawString("تقرير وسجل حركات التصنيع والإنتاج", fontTitle, Brushes.DarkBlue, new PointF(230, y));
                y += 35;
                g.DrawString($"الفترة من: {dtpFrom.Value:yyyy-MM-dd} إلى: {dtpTo.Value:yyyy-MM-dd} | تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}", fontBody, Brushes.Gray, new PointF(40, y));
                y += 30;

                // Table Header
                g.FillRectangle(Brushes.LightGray, 40, y, 740, 24);
                g.DrawRectangle(Pens.Gray, 40, y, 740, 24);
                g.DrawString("كود الأمر", fontHeader, Brushes.Black, 50, y + 3);
                g.DrawString("النوع", fontHeader, Brushes.Black, 150, y + 3);
                g.DrawString("المنتج المصنع", fontHeader, Brushes.Black, 230, y + 3);
                g.DrawString("الكمية", fontHeader, Brushes.Black, 420, y + 3);
                g.DrawString("إجمالي التكلفة", fontHeader, Brushes.Black, 490, y + 3);
                g.DrawString("تكلفة القطعة", fontHeader, Brushes.Black, 580, y + 3);
                g.DrawString("الحالة", fontHeader, Brushes.Black, 670, y + 3);
                y += 24;

                foreach (DataGridViewRow row in dgOrders.Rows)
                {
                    if (y > e.MarginBounds.Bottom) break;

                    g.DrawRectangle(Pens.LightGray, 40, y, 740, 22);
                    g.DrawString(row.Cells["OrderCode"].Value?.ToString() ?? "", fontBody, Brushes.Black, 50, y + 3);
                    g.DrawString(row.Cells["ProductionTypeName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 150, y + 3);
                    g.DrawString(row.Cells["FinishedProductName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 230, y + 3);
                    g.DrawString(row.Cells["ProducedQty"].Value?.ToString() ?? "", fontBody, Brushes.Black, 420, y + 3);
                    g.DrawString(Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0).ToString("N2"), fontBody, Brushes.Black, 490, y + 3);
                    g.DrawString(Convert.ToDecimal(row.Cells["UnitCost"].Value ?? 0).ToString("N2"), fontBody, Brushes.Black, 580, y + 3);
                    g.DrawString(row.Cells["StatusName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 670, y + 3);
                    y += 22;
                }
            };

            using (var ppd = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700 })
            {
                ppd.ShowDialog();
            }
        }

        private void ExportToCsv()
        {
            if (dgOrders.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv", FileName = $"ProductionReport_{DateTime.Now:yyyyMMdd}.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        // Header
                        sb.AppendLine("كود الأمر,نوع التصنيع,كود المنتج,اسم المنتج,الكمية,الوحدة,تكلفة الخامات,مصاريف التشغيل,إجمالي التكلفة,تكلفة الوحدة,الحالة,تاريخ الإنشاء,المخزن,المستخدم");
                        foreach (DataGridViewRow r in dgOrders.Rows)
                        {
                            sb.AppendLine($"\"{r.Cells["OrderCode"].Value}\",\"{r.Cells["ProductionTypeName"].Value}\",\"{r.Cells["FinishedProductCode"].Value}\",\"{r.Cells["FinishedProductName"].Value}\",\"{r.Cells["ProducedQty"].Value}\",\"{r.Cells["UnitName"].Value}\",\"{r.Cells["RawMaterialsCost"].Value}\",\"{r.Cells["ExtraExpenses"].Value}\",\"{r.Cells["TotalCost"].Value}\",\"{r.Cells["UnitCost"].Value}\",\"{r.Cells["StatusName"].Value}\",\"{r.Cells["CreatedDate"].Value}\",\"{r.Cells["WarehouseName"].Value}\",\"{r.Cells["CreatedByName"].Value}\"");
                        }
                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        MessageBox.Show("تم تصدير التقرير بنجاح!", "نجاح التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"فشل التصدير: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}

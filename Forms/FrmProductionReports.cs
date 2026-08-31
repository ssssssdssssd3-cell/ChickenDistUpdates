using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// تقرير وسجل حركات وتعديلات عمليات التصنيع (ثابت ومخصص)
    /// بتصميم احترافي عالي الكثافة (High Information Density) ومنسق بالكامل
    /// </summary>
    public class FrmProductionReports : Form
    {
        // Filters
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboTypeFilter, cboStatusFilter, cboWarehouseFilter;
        private TextBox txtSearch;
        private Button btnSearch, btnRefresh;

        // Quick Preset Buttons
        private Button btnFilterToday, btnFilterThisMonth, btnFilterAllTime;

        // Master Grid & Splitter
        private SplitContainer split;
        private DataGridView dgOrders;

        // Details Panel (Tabs: Items & History)
        private TabControl tabDetails;
        private DataGridView dgItemsDetail;
        private DataGridView dgHistoryDetail;
        private Button btnToggleDetails;
        private bool _detailsVisible = true;

        // KPI Badges Bar
        private Label lblTotalOrdersCount;
        private Label lblPrepOrdersCount;
        private Label lblCompletedOrdersCount;
        private Label lblRawCostSum;
        private Label lblExtraExpensesSum;
        private Label lblTotalCostSum;

        // Action Buttons
        private Button btnOpenOrder;
        private Button btnNewFixed;
        private Button btnNewCustom;
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
            this.Text = "📊 سجل ومتابعة حركات التصنيع والتشغيل الشامل";
            this.Size = new Size(1280, 780);
            this.MinimumSize = new Size(1060, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false; // لمنع تشوه التخطيط ومحاذاة الشاشات في ويندوز
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ══════════════════════════════════════════════════════════════
            // 1. الشريط العلوي للفلاتر المدمجة (Compact Filter Bar - 48px)
            // ══════════════════════════════════════════════════════════════
            var pnlFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 6, 8, 6)
            };
            this.Controls.Add(pnlFilters);

            var flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlFilters.Controls.Add(flowFilters);

            // عنوان الفلترة
            flowFilters.Controls.Add(new Label
            {
                Text = "🔍 تصفية الأوامر:",
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.Accent,
                Margin = new Padding(0, 6, 6, 0)
            });

            // من تاريخ
            flowFilters.Controls.Add(new Label { Text = "من:", AutoSize = true, Margin = new Padding(0, 6, 2, 0), Font = Theme.FontSmall });
            dtpFrom = new DateTimePicker
            {
                Width = 92,
                Height = 26,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30),
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            flowFilters.Controls.Add(dtpFrom);

            // إلى تاريخ
            flowFilters.Controls.Add(new Label { Text = "إلى:", AutoSize = true, Margin = new Padding(0, 6, 2, 0), Font = Theme.FontSmall });
            dtpTo = new DateTimePicker
            {
                Width = 92,
                Height = 26,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            flowFilters.Controls.Add(dtpTo);

            // أزرار الفترات السريعة
            btnFilterToday = MakeSmallButton("اليوم", () => { dtpFrom.Value = DateTime.Today; dtpTo.Value = DateTime.Today; ApplyFilters(); });
            flowFilters.Controls.Add(btnFilterToday);

            btnFilterThisMonth = MakeSmallButton("الشهر", () => { dtpFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); dtpTo.Value = DateTime.Today; ApplyFilters(); });
            flowFilters.Controls.Add(btnFilterThisMonth);

            btnFilterAllTime = MakeSmallButton("الكل", () => { dtpFrom.Value = DateTime.Today.AddYears(-5); dtpTo.Value = DateTime.Today; ApplyFilters(); });
            flowFilters.Controls.Add(btnFilterAllTime);

            // نوع التصنيع
            flowFilters.Controls.Add(new Label { Text = "النوع:", AutoSize = true, Margin = new Padding(4, 6, 2, 0), Font = Theme.FontSmall });
            cboTypeFilter = new ComboBox
            {
                Width = 100,
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            cboTypeFilter.Items.AddRange(new object[] { "كل الأنواع", "تصنيع ثابت (BOM)", "تصنيع مخصص" });
            cboTypeFilter.SelectedIndex = 0;
            cboTypeFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            flowFilters.Controls.Add(cboTypeFilter);

            // الحالة
            flowFilters.Controls.Add(new Label { Text = "الحالة:", AutoSize = true, Margin = new Padding(4, 6, 2, 0), Font = Theme.FontSmall });
            cboStatusFilter = new ComboBox
            {
                Width = 105,
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            cboStatusFilter.Items.AddRange(new object[] { "كل الحالات", "تحت التحضير", "مكتمل ومرحل", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            flowFilters.Controls.Add(cboStatusFilter);

            // المخزن
            flowFilters.Controls.Add(new Label { Text = "المخزن:", AutoSize = true, Margin = new Padding(4, 6, 2, 0), Font = Theme.FontSmall });
            cboWarehouseFilter = new ComboBox
            {
                Width = 105,
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Margin = new Padding(0, 3, 4, 0)
            };
            cboWarehouseFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            flowFilters.Controls.Add(cboWarehouseFilter);

            // حقل البحث الفوري
            txtSearch = new TextBox
            {
                Width = 140,
                Height = 26,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Margin = new Padding(4, 3, 2, 0)
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ApplyFilters(); } };
            flowFilters.Controls.Add(txtSearch);

            btnSearch = Theme.MakeButton("بحث", 0, 0, 60, 27, Theme.Primary);
            btnSearch.Margin = new Padding(0, 2, 3, 0);
            btnSearch.Click += (s, e) => ApplyFilters();
            flowFilters.Controls.Add(btnSearch);

            btnRefresh = Theme.MakeButton("🔄", 0, 0, 36, 27, Color.FromArgb(71, 85, 105));
            btnRefresh.Margin = new Padding(0, 2, 2, 0);
            btnRefresh.Click += (s, e) => { txtSearch.Clear(); ApplyFilters(); };
            flowFilters.Controls.Add(btnRefresh);

            // ══════════════════════════════════════════════════════════════
            // 2. شريط البطاقات الإحصائية والمالية (KPI Dashboard - 46px)
            // ══════════════════════════════════════════════════════════════
            var pnlKPI = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 0, 0, 2)
            };
            this.Controls.Add(pnlKPI);

            var flowKPI = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlKPI.Controls.Add(flowKPI);

            lblTotalOrdersCount = MakeBadge("📋 إجمالي الأوامر: 0", Color.FromArgb(30, 41, 59), Color.FromArgb(226, 232, 240));
            flowKPI.Controls.Add(lblTotalOrdersCount);

            lblPrepOrdersCount = MakeBadge("⏳ تحت التحضير: 0", Color.FromArgb(194, 65, 12), Color.FromArgb(255, 237, 213));
            flowKPI.Controls.Add(lblPrepOrdersCount);

            lblCompletedOrdersCount = MakeBadge("✅ مكتمل ومرحل: 0", Color.FromArgb(21, 128, 61), Color.FromArgb(220, 252, 231));
            flowKPI.Controls.Add(lblCompletedOrdersCount);

            lblRawCostSum = MakeBadge("📦 تكلفة الخامات: 0.00 ج", Color.FromArgb(146, 64, 14), Color.FromArgb(254, 243, 199));
            flowKPI.Controls.Add(lblRawCostSum);

            lblExtraExpensesSum = MakeBadge("⚡ مصاريف تشغيل: 0.00 ج", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 240, 138));
            flowKPI.Controls.Add(lblExtraExpensesSum);

            lblTotalCostSum = MakeBadge("💰 إجمالي التكاليف: 0.00 ج.م", Color.FromArgb(15, 23, 42), Color.FromArgb(186, 230, 253));
            lblTotalCostSum.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            flowKPI.Controls.Add(lblTotalCostSum);

            // ══════════════════════════════════════════════════════════════
            // 3. منطقة البيانات المقسمة: الجدول الرئيسي + تفاصيل الأمر
            // ══════════════════════════════════════════════════════════════
            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(203, 213, 225),
                SplitterWidth = 6,
                Panel1MinSize = 150,
                Panel2MinSize = 80
            };
            this.Controls.Add(split);
            split.BringToFront();

            // ── أ) جدول أوامر التصنيع الرئيسي (Master Grid) ──
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
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 28 },
                GridColor = Color.FromArgb(226, 232, 240)
            };
            dgOrders.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgOrders.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8.75f),
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.Black,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgOrders.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252)
            };
            Theme.EnableDoubleBuffer(dgOrders);

            dgOrders.SelectionChanged += (s, e) => LoadSelectedOrderDetails();
            dgOrders.CellDoubleClick += (s, e) => OpenSelectedOrderForm();
            split.Panel1.Controls.Add(dgOrders);

            // ── ب) تبويبات تفاصيل الأمر المختار (Details Tabs) ──
            var pnlDetailsHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(4, 2, 4, 2)
            };
            
            btnToggleDetails = new Button
            {
                Text = "🔽 إخفاء لوحة التفاصيل",
                Dock = DockStyle.Left,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontSmall,
                ForeColor = Theme.Primary,
                Cursor = Cursors.Hand
            };
            btnToggleDetails.FlatAppearance.BorderSize = 0;
            btnToggleDetails.Click += (s, e) => ToggleDetailsPanel();
            pnlDetailsHeader.Controls.Add(btnToggleDetails);

            var lblDetTitle = new Label
            {
                Text = "📦 تفاصيل ومكونات وسجل الأمر المحدد أعلاه:",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            pnlDetailsHeader.Controls.Add(lblDetTitle);
            split.Panel2.Controls.Add(pnlDetailsHeader);

            tabDetails = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = false
            };
            split.Panel2.Controls.Add(tabDetails);
            tabDetails.BringToFront();

            // Tab 1: Raw Materials
            var tabItems = new TabPage("📦 المواد الخام المستهلكة في هذا الأمر");
            tabItems.BackColor = Color.White;
            dgItemsDetail = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 25 },
                GridColor = Color.FromArgb(226, 232, 240)
            };
            dgItemsDetail.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgItemsDetail.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8.5f),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            Theme.EnableDoubleBuffer(dgItemsDetail);
            tabItems.Controls.Add(dgItemsDetail);
            tabDetails.TabPages.Add(tabItems);

            // Tab 2: Audit History
            var tabHistory = new TabPage("📑 سجل حركات وتعديل الأمر (Audit Trail)");
            tabHistory.BackColor = Color.White;
            dgHistoryDetail = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 25 },
                GridColor = Color.FromArgb(226, 232, 240)
            };
            dgHistoryDetail.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgHistoryDetail.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8.5f),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            Theme.EnableDoubleBuffer(dgHistoryDetail);
            tabHistory.Controls.Add(dgHistoryDetail);
            tabDetails.TabPages.Add(tabHistory);

            // ══════════════════════════════════════════════════════════════
            // 4. الشريط السفلي للعمليات والإجراءات (Action Bar - 44px)
            // ══════════════════════════════════════════════════════════════
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 4, 8, 4)
            };
            this.Controls.Add(pnlBottom);

            var flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlBottom.Controls.Add(flowBottom);

            btnOpenOrder = Theme.MakeButton("📂 فتح أمر التصنيع", 0, 0, 140, 34, Theme.Primary);
            btnOpenOrder.Font = Theme.FontBold;
            btnOpenOrder.Margin = new Padding(0, 1, 8, 0);
            btnOpenOrder.Click += (s, e) => OpenSelectedOrderForm();
            flowBottom.Controls.Add(btnOpenOrder);

            btnNewFixed = Theme.MakeButton("➕ تصنيع معياري (BOM)", 0, 0, 150, 34, Color.FromArgb(16, 185, 129));
            btnNewFixed.Font = Theme.FontBold;
            btnNewFixed.Margin = new Padding(0, 1, 8, 0);
            btnNewFixed.Click += (s, e) =>
            {
                using (var frm = new FrmFixedProduction())
                {
                    frm.ShowDialog();
                    ApplyFilters();
                }
            };
            flowBottom.Controls.Add(btnNewFixed);

            btnNewCustom = Theme.MakeButton("🛠️ تصنيع مخصص", 0, 0, 125, 34, Color.FromArgb(139, 92, 246));
            btnNewCustom.Font = Theme.FontBold;
            btnNewCustom.Margin = new Padding(0, 1, 8, 0);
            btnNewCustom.Click += (s, e) =>
            {
                using (var frm = new FrmCustomProduction())
                {
                    frm.ShowDialog();
                    ApplyFilters();
                }
            };
            flowBottom.Controls.Add(btnNewCustom);

            btnPrintReport = Theme.MakeButton("🖨️ طباعة التقرير", 0, 0, 115, 34, Color.FromArgb(2, 132, 199));
            btnPrintReport.Font = Theme.FontBold;
            btnPrintReport.Margin = new Padding(0, 1, 8, 0);
            btnPrintReport.Click += (s, e) => PrintReport();
            flowBottom.Controls.Add(btnPrintReport);

            btnExportExcel = Theme.MakeButton("📊 تصدير Excel", 0, 0, 115, 34, Color.FromArgb(22, 163, 74));
            btnExportExcel.Font = Theme.FontBold;
            btnExportExcel.Margin = new Padding(0, 1, 0, 0);
            btnExportExcel.Click += (s, e) => ExportToCsv();
            flowBottom.Controls.Add(btnExportExcel);

            this.Resize += (s, e) =>
            {
                if (split != null && split.Height > 250 && _detailsVisible)
                {
                    split.SplitterDistance = (int)(split.Height * 0.65);
                }
            };
        }

        private static Button MakeSmallButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = 46,
                Height = 26,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontSmall,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 3, 0)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private static Label MakeBadge(string text, Color fore, Color back)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = fore,
                BackColor = back,
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 3, 6, 0)
            };
        }

        private void ToggleDetailsPanel()
        {
            _detailsVisible = !_detailsVisible;
            if (_detailsVisible)
            {
                split.Panel2Collapsed = false;
                btnToggleDetails.Text = "🔽 إخفاء لوحة التفاصيل";
                if (split.Height > 250) split.SplitterDistance = (int)(split.Height * 0.65);
            }
            else
            {
                split.Panel2Collapsed = true;
                btnToggleDetails.Text = "🔼 إظهار لوحة التفاصيل";
            }
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
                DateTime from = dtpFrom.Value.Date;
                DateTime to = dtpTo.Value.Date.AddDays(1).AddSeconds(-1);

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

                if (split != null && split.Height > 250 && _detailsVisible)
                {
                    split.SplitterDistance = (int)(split.Height * 0.65);
                }
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

            SetCol(dgOrders, "OrderCode", "كود الأمر", 85, fillWeight: 10, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "CreatedDate", "التاريخ", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "ProductionTypeName", "نوع التصنيع", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "WarehouseName", "المخزن", 80, fillWeight: 8, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "FinishedProductCode", "كود الصنف", 75, fillWeight: 8, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "FinishedProductName", "المنتج المصنع", 160, fillWeight: 22, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "ProducedQty", "الكمية", 55, fillWeight: 7, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "UnitName", "الوحدة", 45, fillWeight: 6, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "RawMaterialsCost", "تكلفة الخامات", 80, fillWeight: 9, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "ExtraExpenses", "مصاريف تشغيل", 80, fillWeight: 9, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "TotalCost", "إجمالي التكلفة", 85, fillWeight: 10, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "UnitCost", "تكلفة القطعة", 80, fillWeight: 9, align: DataGridViewContentAlignment.MiddleRight);
            SetCol(dgOrders, "StatusName", "الحالة", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "CreatedByName", "المستخدم", 75, fillWeight: 8, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "UpdatedDate", "آخر تعديل", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgOrders, "CompletedDate", "تاريخ الإتمام", 85, fillWeight: 9, align: DataGridViewContentAlignment.MiddleCenter);

            // Formatting
            if (dgOrders.Columns["CreatedDate"] != null) dgOrders.Columns["CreatedDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
            if (dgOrders.Columns["RawMaterialsCost"] != null) dgOrders.Columns["RawMaterialsCost"].DefaultCellStyle.Format = "N2";
            if (dgOrders.Columns["ExtraExpenses"] != null) dgOrders.Columns["ExtraExpenses"].DefaultCellStyle.Format = "N2";
            if (dgOrders.Columns["TotalCost"] != null)
            {
                dgOrders.Columns["TotalCost"].DefaultCellStyle.Format = "N2";
                dgOrders.Columns["TotalCost"].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                dgOrders.Columns["TotalCost"].DefaultCellStyle.ForeColor = Color.FromArgb(180, 83, 9);
            }
            if (dgOrders.Columns["UnitCost"] != null)
            {
                dgOrders.Columns["UnitCost"].DefaultCellStyle.Format = "N2";
                dgOrders.Columns["UnitCost"].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                dgOrders.Columns["UnitCost"].DefaultCellStyle.ForeColor = Color.FromArgb(16, 185, 129);
            }
            if (dgOrders.Columns["ProducedQty"] != null)
            {
                dgOrders.Columns["ProducedQty"].DefaultCellStyle.Format = "N2";
                dgOrders.Columns["ProducedQty"].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }

            // Cell formatting for status & type
            foreach (DataGridViewRow r in dgOrders.Rows)
            {
                string st = r.Cells["StatusName"]?.Value?.ToString() ?? "";
                if (st.Contains("تحت التحضير"))
                {
                    r.Cells["StatusName"].Style.ForeColor = Color.FromArgb(194, 65, 12);
                    r.Cells["StatusName"].Style.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
                }
                else if (st.Contains("مكتمل"))
                {
                    r.Cells["StatusName"].Style.ForeColor = Color.FromArgb(21, 128, 61);
                    r.Cells["StatusName"].Style.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
                }
                else if (st.Contains("ملغي"))
                {
                    r.Cells["StatusName"].Style.ForeColor = Color.FromArgb(220, 38, 38);
                }
            }
        }

        private static void SetCol(DataGridView dg, string name, string header, int width, int fillWeight = 10, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            if (dg.Columns[name] != null)
            {
                dg.Columns[name].HeaderText = header;
                dg.Columns[name].MinimumWidth = width;
                dg.Columns[name].FillWeight = fillWeight;
                dg.Columns[name].DefaultCellStyle.Alignment = align;
            }
        }

        private void CalculateSummary(DataTable dt)
        {
            if (dt == null) return;
            int totalCount = dt.Rows.Count;
            int prepCount = 0;
            int completedCount = 0;
            decimal totalCost = 0;
            decimal totalExpenses = 0;
            decimal rawCost = 0;

            foreach (DataRow r in dt.Rows)
            {
                string st = r["Status"]?.ToString();
                if (st == "InPreparation") prepCount++;
                else if (st == "Completed") completedCount++;

                if (st != "Cancelled")
                {
                    totalCost += Convert.ToDecimal(r["TotalCost"] ?? 0);
                    totalExpenses += Convert.ToDecimal(r["ExtraExpenses"] ?? 0);
                    rawCost += Convert.ToDecimal(r["RawMaterialsCost"] ?? 0);
                }
            }

            lblTotalOrdersCount.Text = $"📋 إجمالي الأوامر: {totalCount}";
            lblPrepOrdersCount.Text = $"⏳ تحت التحضير: {prepCount}";
            lblCompletedOrdersCount.Text = $"✅ مكتمل ومرحل: {completedCount}";
            lblRawCostSum.Text = $"📦 تكلفة الخامات: {rawCost:N2} ج";
            lblExtraExpensesSum.Text = $"⚡ مصاريف تشغيل: {totalExpenses:N2} ج";
            lblTotalCostSum.Text = $"💰 إجمالي التكاليف: {totalCost:N2} ج.م";
        }

        private void LoadSelectedOrderDetails()
        {
            if (dgOrders.CurrentRow == null || dgOrders.CurrentRow.Cells["ProductionID"].Value == null)
            {
                dgItemsDetail.DataSource = null;
                dgHistoryDetail.DataSource = null;
                return;
            }

            int pid = Convert.ToInt32(dgOrders.CurrentRow.Cells["ProductionID"].Value);

            // 1. تحميل المواد الخام
            var dtItems = DbHelper.Query(@"
                SELECT p.ProductCode AS RawProductCode, p.ProductName AS RawProductName,
                       poi.Quantity, poi.UnitName, poi.UnitCost, poi.TotalCost, poi.Notes
                FROM ProductionOrderItems poi
                JOIN Products p ON poi.RawProductID = p.ProductID
                WHERE poi.ProductionID = @id",
                DbHelper.P("@id", pid));

            dgItemsDetail.DataSource = dtItems;
            SetCol(dgItemsDetail, "RawProductCode", "كود الخام", 90, 14, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgItemsDetail, "RawProductName", "اسم المادة الخام المستهلكة", 180, 36, DataGridViewContentAlignment.MiddleRight);
            SetCol(dgItemsDetail, "Quantity", "الكمية المخصومة", 90, 12, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgItemsDetail, "UnitName", "الوحدة", 60, 10, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgItemsDetail, "UnitCost", "سعر التكلفة", 85, 12, DataGridViewContentAlignment.MiddleRight);
            SetCol(dgItemsDetail, "TotalCost", "إجمالي التكلفة", 95, 14, DataGridViewContentAlignment.MiddleRight);
            SetCol(dgItemsDetail, "Notes", "ملاحظات", 120, 18, DataGridViewContentAlignment.MiddleRight);

            if (dgItemsDetail.Columns["Quantity"] != null) dgItemsDetail.Columns["Quantity"].DefaultCellStyle.Format = "N3";
            if (dgItemsDetail.Columns["UnitCost"] != null) dgItemsDetail.Columns["UnitCost"].DefaultCellStyle.Format = "N2";
            if (dgItemsDetail.Columns["TotalCost"] != null)
            {
                dgItemsDetail.Columns["TotalCost"].DefaultCellStyle.Format = "N2";
                dgItemsDetail.Columns["TotalCost"].DefaultCellStyle.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
                dgItemsDetail.Columns["TotalCost"].DefaultCellStyle.ForeColor = Color.FromArgb(180, 83, 9);
            }

            // 2. تحميل سجل التعديل والتدقيق
            var dtHist = ProductionDAL.GetOrderHistory(pid);
            dgHistoryDetail.DataSource = dtHist;
            if (dgHistoryDetail.Columns["HistoryID"] != null) dgHistoryDetail.Columns["HistoryID"].Visible = false;
            if (dgHistoryDetail.Columns["ProductionID"] != null) dgHistoryDetail.Columns["ProductionID"].Visible = false;
            if (dgHistoryDetail.Columns["ActionType"] != null) dgHistoryDetail.Columns["ActionType"].Visible = false;

            SetCol(dgHistoryDetail, "ActionTypeName", "نوع الإجراء", 120, 20, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgHistoryDetail, "ActionDate", "التاريخ والوقت", 130, 22, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgHistoryDetail, "ActionBy", "بواسطة", 100, 16, DataGridViewContentAlignment.MiddleCenter);
            SetCol(dgHistoryDetail, "Details", "تفاصيل الإجراء والتعديلات", 250, 42, DataGridViewContentAlignment.MiddleRight);
        }

        private void OpenSelectedOrderForm()
        {
            if (dgOrders.CurrentRow == null || dgOrders.CurrentRow.Cells["ProductionID"].Value == null) return;
            int pid = Convert.ToInt32(dgOrders.CurrentRow.Cells["ProductionID"].Value);
            string pType = dgOrders.CurrentRow.Cells["ProductionType"].Value?.ToString();

            if (pType == "Custom")
            {
                using (var frm = new FrmCustomProduction(pid))
                {
                    frm.ShowDialog();
                    ApplyFilters();
                }
            }
            else
            {
                using (var frm = new FrmFixedProduction(pid))
                {
                    frm.ShowDialog();
                    ApplyFilters();
                }
            }
        }

        private void PrintReport()
        {
            if (dgOrders.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pd = new PrintDocument();
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    float y = 40;
                    var fontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
                    var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    var fontBody = new Font("Segoe UI", 8.5f);
                    var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold);

                    g.DrawString("تقرير وسجل حركات التصنيع والإنتاج الشامل", fontTitle, Brushes.DarkBlue, new PointF(230, y));
                    y += 35;
                    g.DrawString($"الفترة من: {dtpFrom.Value:yyyy-MM-dd} إلى: {dtpTo.Value:yyyy-MM-dd} | تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}", fontBody, Brushes.DarkSlateGray, new PointF(40, y));
                    y += 28;

                    // Table Header
                    g.FillRectangle(Brushes.LightGray, 40, y, 740, 24);
                    g.DrawRectangle(Pens.Gray, 40, y, 740, 24);
                    g.DrawString("كود الأمر", fontHeader, Brushes.Black, 45, y + 4);
                    g.DrawString("النوع", fontHeader, Brushes.Black, 130, y + 4);
                    g.DrawString("المنتج المصنع", fontHeader, Brushes.Black, 210, y + 4);
                    g.DrawString("الكمية", fontHeader, Brushes.Black, 400, y + 4);
                    g.DrawString("تكلفة الخامات", fontHeader, Brushes.Black, 460, y + 4);
                    g.DrawString("إجمالي التكلفة", fontHeader, Brushes.Black, 550, y + 4);
                    g.DrawString("تكلفة القطعة", fontHeader, Brushes.Black, 640, y + 4);
                    g.DrawString("الحالة", fontHeader, Brushes.Black, 715, y + 4);
                    y += 24;

                    decimal sumTotal = 0;
                    foreach (DataGridViewRow row in dgOrders.Rows)
                    {
                        if (y > e.MarginBounds.Bottom - 40)
                        {
                            e.HasMorePages = true;
                            return;
                        }

                        g.DrawRectangle(Pens.LightGray, 40, y, 740, 22);
                        g.DrawString(row.Cells["OrderCode"].Value?.ToString() ?? "", fontBody, Brushes.Black, 45, y + 3);
                        g.DrawString(row.Cells["ProductionTypeName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 130, y + 3);
                        g.DrawString(row.Cells["FinishedProductName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 210, y + 3);
                        g.DrawString($"{row.Cells["ProducedQty"].Value} {row.Cells["UnitName"].Value}", fontBody, Brushes.Black, 400, y + 3);
                        g.DrawString(Convert.ToDecimal(row.Cells["RawMaterialsCost"].Value ?? 0).ToString("N2"), fontBody, Brushes.Black, 460, y + 3);
                        
                        decimal tot = Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0);
                        sumTotal += tot;
                        g.DrawString(tot.ToString("N2"), fontBold, Brushes.DarkBlue, 550, y + 3);
                        g.DrawString(Convert.ToDecimal(row.Cells["UnitCost"].Value ?? 0).ToString("N2"), fontBody, Brushes.Black, 640, y + 3);
                        g.DrawString(row.Cells["StatusName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 715, y + 3);
                        y += 22;
                    }

                    y += 10;
                    g.DrawLine(new Pen(Color.DarkBlue, 2), 40, y, 780, y);
                    y += 8;
                    g.DrawString($"إجمالي تكاليف أوامر التصنيع: {sumTotal:N2} ج.م", fontBold, Brushes.DarkBlue, new PointF(40, y));
                    g.DrawString($"عدد الأوامر: {dgOrders.Rows.Count}", fontBold, Brushes.Black, new PointF(640, y));
                };

                using (var ppd = new PrintPreviewDialog { Document = pd, Width = 950, Height = 700 })
                {
                    ppd.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل إعداد الطباعة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToCsv()
        {
            if (dgOrders.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog { Filter = "Excel CSV (*.csv)|*.csv", FileName = $"ProductionReport_{DateTime.Now:yyyyMMdd_HHmm}.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("كود الأمر,تاريخ الإنشاء,نوع التصنيع,المخزن,كود المنتج,اسم المنتج المصنع,الكمية,الوحدة,تكلفة الخامات,مصاريف التشغيل,إجمالي التكلفة,تكلفة القطعة,الحالة,المستخدم");
                        foreach (DataGridViewRow r in dgOrders.Rows)
                        {
                            sb.AppendLine($"\"{r.Cells["OrderCode"].Value}\",\"{r.Cells["CreatedDate"].Value}\",\"{r.Cells["ProductionTypeName"].Value}\",\"{r.Cells["WarehouseName"].Value}\",\"{r.Cells["FinishedProductCode"].Value}\",\"{r.Cells["FinishedProductName"].Value}\",\"{r.Cells["ProducedQty"].Value}\",\"{r.Cells["UnitName"].Value}\",\"{r.Cells["RawMaterialsCost"].Value}\",\"{r.Cells["ExtraExpenses"].Value}\",\"{r.Cells["TotalCost"].Value}\",\"{r.Cells["UnitCost"].Value}\",\"{r.Cells["StatusName"].Value}\",\"{r.Cells["CreatedByName"].Value}\"");
                        }
                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        MessageBox.Show("✅ تم تصدير تقرير التصنيع بنجاح!", "نجاح التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ فشل التصدير:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}

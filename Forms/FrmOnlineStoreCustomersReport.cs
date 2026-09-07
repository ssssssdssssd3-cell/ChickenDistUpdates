using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// تقرير عملاء المتجر الإلكتروني الشامل (Online Store Customers Report)
    /// يعرض إحصائيات العملاء الأكثر طلباً وإنفاقاً، تاريخ المعاملات،
    /// المراسلة المباشرة عبر واتساب، تسجيل العملاء في الدليل، وطباعة/تصدير التقارير.
    /// </summary>
    public class FrmOnlineStoreCustomersReport : Form
    {
        // KPI Labels
        private Label lblKpiTotalCustomers;
        private Label lblKpiTotalOrders;
        private Label lblKpiCompletedAmount;
        private Label lblKpiRegisteredRatio;

        // Filter Controls
        private TextBox txtSearch;
        private CheckBox chkDateFilter;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private ComboBox cboFilterType;
        private ComboBox cboSortBy;
        private Button btnClearFilter;

        // DataGridView
        private DataGridView dgvCustomers;

        // Selection & Details Bar
        private Panel pnlSelectedCustomer;
        private Label lblSelectedCustName;
        private Label lblSelectedCustPhone;
        private Label lblSelectedCustStats;
        private Button btnWhatsApp;
        private Button btnViewOrders;
        private Button btnRegisterClient;

        // Header Buttons
        private Button btnRefresh;
        private Button btnPrintReport;
        private Button btnExportCsv;
        private Button btnClose;

        // Internal Data
        private DataTable _dtCustomers;
        private DataRow _selectedRow;

        public FrmOnlineStoreCustomersReport()
        {
            InitializeComponent();
            LoadReport();
        }

        private void InitializeComponent()
        {
            this.Text = "👥 تقرير عملاء المتجر الإلكتروني | ProSoft Online Store Customers";
            this.Size = new Size(1180, 720);
            this.MinimumSize = new Size(950, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.Font = new Font("Segoe UI", 9f);
            this.KeyPreview = true;
            this.KeyDown += FrmOnlineStoreCustomersReport_KeyDown;

            // ==========================================
            // 1. الشريط العلوي المدمج (Header Bar - 45px)
            // ==========================================
            var pnlHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10, 4, 10, 4),
                Margin = new Padding(0)
            };
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var pnlTitle = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0)
            };

            var lblTitle = new Label
            {
                Text = "👥 تقرير عملاء المتجر الإلكتروني",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 114, 182),
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0)
            };

            var lblSubtitle = new Label
            {
                Text = "قاعدة بيانات عملاء المتجر، إحصائيات الشراء، المراسلة الفورية والتسجيل المباشر",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 0)
            };

            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(lblSubtitle);

            var pnlHeaderButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };

            btnRefresh = CreateButton("🔄 تحديث (F5)", Color.FromArgb(37, 99, 235));
            btnRefresh.Click += (s, e) => LoadReport();

            btnPrintReport = CreateButton("🖨️ طباعة تقرير (F9)", Color.FromArgb(16, 185, 129));
            btnPrintReport.Click += (s, e) => PrintReport();

            btnExportCsv = CreateButton("📊 تصدير Excel", Color.FromArgb(14, 116, 144));
            btnExportCsv.Click += (s, e) => ExportToCsv();

            btnClose = CreateButton("❌ إغلاق (Esc)", Color.FromArgb(71, 85, 105));
            btnClose.Click += (s, e) => this.Close();

            pnlHeaderButtons.Controls.Add(btnRefresh);
            pnlHeaderButtons.Controls.Add(btnPrintReport);
            pnlHeaderButtons.Controls.Add(btnExportCsv);
            pnlHeaderButtons.Controls.Add(btnClose);

            pnlHeader.Controls.Add(pnlTitle, 0, 0);
            pnlHeader.Controls.Add(pnlHeaderButtons, 1, 0);

            // ==========================================
            // 2. بطاقات المؤشرات (KPIs - 50px)
            // ==========================================
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                RowCount = 1,
                ColumnCount = 4,
                Padding = new Padding(10, 2, 10, 4),
                BackColor = Color.FromArgb(18, 26, 43),
                Margin = new Padding(0)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblKpiTotalCustomers = CreateKpiCard(pnlKpis, 0, "إجمالي عملاء المتجر", "0", Color.FromArgb(56, 189, 248));
            lblKpiTotalOrders = CreateKpiCard(pnlKpis, 1, "إجمالي الطلبات", "0", Color.FromArgb(251, 191, 36));
            lblKpiCompletedAmount = CreateKpiCard(pnlKpis, 2, "المبيعات المكتملة", "0.00 ج.م", Color.FromArgb(52, 211, 153));
            lblKpiRegisteredRatio = CreateKpiCard(pnlKpis, 3, "مسجلين كعملاء بالبرنامج", "0 (0%)", Color.FromArgb(192, 132, 252));

            // ==========================================
            // 3. شريط الفلاتر والبحث (Filter Bar - 42px)
            // ==========================================
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10, 6, 10, 4),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            // البحث
            var lblSearch = new Label
            {
                Text = "🔍 بحث:",
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0)
            };
            txtSearch = new TextBox
            {
                Width = 180,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 3, 10, 0)
            };
            txtSearch.TextChanged += (s, e) => LoadReport();

            // تصنيف العملاء
            var lblFilterType = new Label
            {
                Text = "التصنيف:",
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0)
            };
            cboFilterType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Width = 130,
                Margin = new Padding(0, 3, 10, 0)
            };
            cboFilterType.Items.AddRange(new object[] { "الكل", "مسجلين بالمنظومة", "غير مسجلين", "أكثر من طلب", "لديهم طلبات مكتملة" });
            cboFilterType.SelectedIndex = 0;
            cboFilterType.SelectedIndexChanged += (s, e) => LoadReport();

            // ترتيب حسب
            var lblSort = new Label
            {
                Text = "ترتيب حسب:",
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0)
            };
            cboSortBy = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Width = 140,
                Margin = new Padding(0, 3, 10, 0)
            };
            cboSortBy.Items.AddRange(new object[] { "تاريخ آخر طلب", "الأعلى إنفاقاً", "الأكثر طلباً", "أبجدياً بالاسم" });
            cboSortBy.SelectedIndex = 0;
            cboSortBy.SelectedIndexChanged += (s, e) => LoadReport();

            // فلترة التاريخ
            chkDateFilter = new CheckBox
            {
                Text = "فترة محددة",
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0)
            };

            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 95,
                Value = DateTime.Today.AddMonths(-1),
                Enabled = false,
                Margin = new Padding(0, 3, 4, 0)
            };

            var lblTo = new Label
            {
                Text = "إلى",
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0)
            };

            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 95,
                Value = DateTime.Today,
                Enabled = false,
                Margin = new Padding(0, 3, 8, 0)
            };

            chkDateFilter.CheckedChanged += (s, e) =>
            {
                dtpFrom.Enabled = chkDateFilter.Checked;
                dtpTo.Enabled = chkDateFilter.Checked;
                LoadReport();
            };
            dtpFrom.ValueChanged += (s, e) => { if (chkDateFilter.Checked) LoadReport(); };
            dtpTo.ValueChanged += (s, e) => { if (chkDateFilter.Checked) LoadReport(); };

            btnClearFilter = new Button
            {
                Text = "مسح الفلاتر",
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Height = 26,
                Width = 80,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 3, 0, 0)
            };
            btnClearFilter.FlatAppearance.BorderSize = 0;
            btnClearFilter.Click += (s, e) =>
            {
                txtSearch.Clear();
                chkDateFilter.Checked = false;
                cboFilterType.SelectedIndex = 0;
                cboSortBy.SelectedIndex = 0;
                LoadReport();
            };

            pnlFilter.Controls.Add(lblSearch);
            pnlFilter.Controls.Add(txtSearch);
            pnlFilter.Controls.Add(lblFilterType);
            pnlFilter.Controls.Add(cboFilterType);
            pnlFilter.Controls.Add(lblSort);
            pnlFilter.Controls.Add(cboSortBy);
            pnlFilter.Controls.Add(chkDateFilter);
            pnlFilter.Controls.Add(dtpFrom);
            pnlFilter.Controls.Add(lblTo);
            pnlFilter.Controls.Add(dtpTo);
            pnlFilter.Controls.Add(btnClearFilter);

            // ==========================================
            // 4. جدول العملاء الرئيسي (DataGridView)
            // ==========================================
            dgvCustomers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9f)
            };
            dgvCustomers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvCustomers.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(56, 189, 248);
            dgvCustomers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvCustomers.ColumnHeadersHeight = 32;
            dgvCustomers.RowTemplate.Height = 30;
            dgvCustomers.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvCustomers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 235);
            dgvCustomers.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvCustomers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(20, 29, 47);

            dgvCustomers.SelectionChanged += DgvCustomers_SelectionChanged;
            dgvCustomers.CellDoubleClick += DgvCustomers_CellDoubleClick;
            dgvCustomers.CellFormatting += DgvCustomers_CellFormatting;

            // ==========================================
            // 5. شريط الإجراءات والعميل المحدد (Bottom Action Bar - 60px)
            // ==========================================
            pnlSelectedCustomer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(24, 33, 53),
                Padding = new Padding(12, 8, 12, 8)
            };

            var tblBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            tblBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tblBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            var pnlCustomerInfo = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var pnlNamePhone = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0)
            };

            lblSelectedCustName = new Label
            {
                Text = "العميل: (حدد عميلاً من الجدول)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            lblSelectedCustPhone = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                Padding = new Padding(12, 1, 0, 0)
            };

            pnlNamePhone.Controls.Add(lblSelectedCustName);
            pnlNamePhone.Controls.Add(lblSelectedCustPhone);

            lblSelectedCustStats = new Label
            {
                Text = "اضغط نقراً مزدوجاً على أي عميل لعرض سجل كافة طلباته بالمتجر",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Padding = new Padding(0, 2, 0, 0)
            };

            pnlCustomerInfo.Controls.Add(pnlNamePhone);
            pnlCustomerInfo.Controls.Add(lblSelectedCustStats);

            var pnlCustButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0)
            };

            btnWhatsApp = CreateActionButton("💬 واتساب", Color.FromArgb(34, 197, 94));
            btnWhatsApp.Click += (s, e) => OpenCustomerWhatsApp();

            btnViewOrders = CreateActionButton("📜 طلبات العميل", Color.FromArgb(59, 130, 246));
            btnViewOrders.Click += (s, e) => ViewCustomerOrders();

            btnRegisterClient = CreateActionButton("➕ تسجيل بالمنظومة", Color.FromArgb(236, 72, 153));
            btnRegisterClient.Click += (s, e) => RegisterCurrentCustomerAsClient();

            pnlCustButtons.Controls.Add(btnWhatsApp);
            pnlCustButtons.Controls.Add(btnViewOrders);
            pnlCustButtons.Controls.Add(btnRegisterClient);

            tblBottom.Controls.Add(pnlCustomerInfo, 0, 0);
            tblBottom.Controls.Add(pnlCustButtons, 1, 0);

            pnlSelectedCustomer.Controls.Add(tblBottom);

            // تجميع عناصر الفورم
            this.Controls.Add(dgvCustomers);
            this.Controls.Add(pnlSelectedCustomer);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlKpis);
            this.Controls.Add(pnlHeader);
        }

        private Label CreateKpiCard(TableLayoutPanel parent, int col, string title, string val, Color valColor)
        {
            var pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59),
                Margin = new Padding(3, 2, 3, 2),
                Padding = new Padding(6, 4, 6, 4)
            };

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 16,
                TextAlign = ContentAlignment.TopRight
            };

            var lblV = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = valColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomRight
            };

            pnlCard.Controls.Add(lblV);
            pnlCard.Controls.Add(lblT);
            parent.Controls.Add(pnlCard, col, 0);

            return lblV;
        }

        private Button CreateButton(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Height = 32,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 0, 3, 0),
                Padding = new Padding(8, 2, 8, 2)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateActionButton(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 34,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 0, 3, 0),
                Padding = new Padding(10, 4, 10, 4)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ==========================================
        // تحميل البيانات وتحديث الواجهة
        // ==========================================
        public void LoadReport()
        {
            try
            {
                DateTime? from = chkDateFilter.Checked ? dtpFrom.Value.Date : (DateTime?)null;
                DateTime? to = chkDateFilter.Checked ? dtpTo.Value.Date : (DateTime?)null;
                string term = txtSearch?.Text?.Trim();
                string filterType = cboFilterType?.SelectedItem?.ToString() ?? "الكل";
                
                string sortBy = "LastOrderDate";
                if (cboSortBy != null)
                {
                    switch (cboSortBy.SelectedIndex)
                    {
                        case 1: sortBy = "TotalSpent"; break;
                        case 2: sortBy = "TotalOrders"; break;
                        case 3: sortBy = "Name"; break;
                        default: sortBy = "LastOrderDate"; break;
                    }
                }

                _dtCustomers = OnlineOrdersDAL.GetStoreCustomersReport(from, to, term, filterType, sortBy);

                BuildGridColumns();
                dgvCustomers.DataSource = _dtCustomers;
                UpdateKpis();
                UpdateSelectedCustomerCard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء جلب تقرير العملاء:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildGridColumns()
        {
            if (dgvCustomers.Columns.Count > 0) return;

            dgvCustomers.AutoGenerateColumns = false;
            dgvCustomers.Columns.Clear();

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColCustomerName",
                DataPropertyName = "CustomerName",
                HeaderText = "اسم العميل",
                FillWeight = 16
            });

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColCustomerPhone",
                DataPropertyName = "CustomerPhone",
                HeaderText = "رقم الموبايل",
                FillWeight = 13
            });

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColCustomerAddress",
                DataPropertyName = "CustomerAddress",
                HeaderText = "العنوان",
                FillWeight = 20
            });

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColTotalOrdersCount",
                DataPropertyName = "TotalOrdersCount",
                HeaderText = "إجمالي الطلبات",
                FillWeight = 9,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColCompletedOrdersCount",
                DataPropertyName = "CompletedOrdersCount",
                HeaderText = "المكتملة",
                FillWeight = 8,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(52, 211, 153) }
            });

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColTotalAmountSpent",
                DataPropertyName = "TotalAmountSpent",
                HeaderText = "إجمالي الإنفاق",
                FillWeight = 11,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }
            });

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColCompletedAmountSpent",
                DataPropertyName = "CompletedAmountSpent",
                HeaderText = "المنفق المكتمل",
                FillWeight = 11,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2", ForeColor = Color.FromArgb(52, 211, 153) }
            });

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColLastOrderDate",
                DataPropertyName = "LastOrderDate",
                HeaderText = "آخر طلب",
                FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "yyyy/MM/dd HH:mm" }
            });

            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColRegistrationStatus",
                HeaderText = "حالة التسجيل",
                FillWeight = 14,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
        }

        private void DgvCustomers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvCustomers.Rows.Count) return;

            var drv = dgvCustomers.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (drv == null) return;

            string colName = dgvCustomers.Columns[e.ColumnIndex].Name;

            if (colName == "ColRegistrationStatus")
            {
                bool isRegistered = drv["IsRegisteredClient"] != DBNull.Value && Convert.ToInt32(drv["IsRegisteredClient"]) == 1;
                if (isRegistered)
                {
                    string code = drv["ClientCode"]?.ToString();
                    e.Value = $"✅ مسجل (كود: {code})";
                    e.CellStyle.ForeColor = Color.FromArgb(52, 211, 153);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                }
                else
                {
                    e.Value = "⚠️ غير مسجل";
                    e.CellStyle.ForeColor = Color.FromArgb(251, 191, 36);
                }
                e.FormattingApplied = true;
            }
        }

        private void UpdateKpis()
        {
            if (_dtCustomers == null || _dtCustomers.Rows.Count == 0)
            {
                lblKpiTotalCustomers.Text = "0";
                lblKpiTotalOrders.Text = "0";
                lblKpiCompletedAmount.Text = "0.00 ج.م";
                lblKpiRegisteredRatio.Text = "0 (0%)";
                return;
            }

            int totalCusts = _dtCustomers.Rows.Count;
            long totalOrders = 0;
            decimal totalCompletedAmt = 0m;
            int registeredCount = 0;

            foreach (DataRow r in _dtCustomers.Rows)
            {
                if (r["TotalOrdersCount"] != DBNull.Value)
                    totalOrders += Convert.ToInt64(r["TotalOrdersCount"]);

                if (r["CompletedAmountSpent"] != DBNull.Value)
                    totalCompletedAmt += Convert.ToDecimal(r["CompletedAmountSpent"]);

                if (r["IsRegisteredClient"] != DBNull.Value && Convert.ToInt32(r["IsRegisteredClient"]) == 1)
                    registeredCount++;
            }

            int percent = totalCusts > 0 ? (int)Math.Round((double)registeredCount * 100.0 / totalCusts) : 0;

            lblKpiTotalCustomers.Text = totalCusts.ToString("N0");
            lblKpiTotalOrders.Text = totalOrders.ToString("N0");
            lblKpiCompletedAmount.Text = $"{totalCompletedAmt:N2} ج.م";
            lblKpiRegisteredRatio.Text = $"{registeredCount} ({percent}%)";
        }

        private void DgvCustomers_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSelectedCustomerCard();
        }

        private void UpdateSelectedCustomerCard()
        {
            if (dgvCustomers.SelectedRows.Count > 0 && dgvCustomers.SelectedRows[0].DataBoundItem is DataRowView drv)
            {
                _selectedRow = drv.Row;
                string name = _selectedRow["CustomerName"]?.ToString();
                string phone = _selectedRow["CustomerPhone"]?.ToString();
                string addr = _selectedRow["CustomerAddress"]?.ToString();
                int orders = _selectedRow["TotalOrdersCount"] != DBNull.Value ? Convert.ToInt32(_selectedRow["TotalOrdersCount"]) : 0;
                decimal spent = _selectedRow["TotalAmountSpent"] != DBNull.Value ? Convert.ToDecimal(_selectedRow["TotalAmountSpent"]) : 0m;
                bool isReg = _selectedRow["IsRegisteredClient"] != DBNull.Value && Convert.ToInt32(_selectedRow["IsRegisteredClient"]) == 1;
                string code = _selectedRow["ClientCode"]?.ToString();

                lblSelectedCustName.Text = $"العميل: {name}";
                lblSelectedCustPhone.Text = $"📱 {phone}";
                lblSelectedCustStats.Text = $"إجمالي الطلبات: {orders} | الإنفاق: {spent:N2} ج.م | العنوان: {(string.IsNullOrEmpty(addr) ? "غير محدد" : addr)} | الحالة: {(isReg ? $"مسجل بالمنظومة (كود: {code})" : "غير مسجل بالدليل")}";

                btnWhatsApp.Enabled = !string.IsNullOrWhiteSpace(phone);
                btnViewOrders.Enabled = true;
                btnRegisterClient.Enabled = !isReg;
                btnRegisterClient.Text = isReg ? "✅ مسجل بالفعل" : "➕ تسجيل بالمنظومة";
            }
            else
            {
                _selectedRow = null;
                lblSelectedCustName.Text = "العميل: (حدد عميلاً من الجدول)";
                lblSelectedCustPhone.Text = "";
                lblSelectedCustStats.Text = "اضغط نقراً مزدوجاً على أي عميل لعرض سجل كافة طلباته بالمتجر";
                btnWhatsApp.Enabled = false;
                btnViewOrders.Enabled = false;
                btnRegisterClient.Enabled = false;
                btnRegisterClient.Text = "➕ تسجيل بالمنظومة";
            }
        }

        private void DgvCustomers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                ViewCustomerOrders();
            }
        }

        // ==========================================
        // الإجراءات (Actions)
        // ==========================================
        private void OpenCustomerWhatsApp()
        {
            if (_selectedRow == null) return;
            string phone = _selectedRow["CustomerPhone"]?.ToString();
            if (string.IsNullOrWhiteSpace(phone)) return;

            string clean = phone.Replace(" ", "").Replace("-", "").Replace("+", "");
            if (clean.StartsWith("01")) clean = "2" + clean;
            else if (clean.StartsWith("1") && clean.Length == 10) clean = "20" + clean;

            string name = _selectedRow["CustomerName"]?.ToString() ?? "عميلنا العزيز";
            string text = Uri.EscapeDataString($"مرحباً بك أستاذ {name}، تواصل معك بخصوص طلبك من المتجر الإلكتروني.");
            string url = $"https://wa.me/{clean}?text={text}";

            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر فتح تطبيق واتساب:\n{ex.Message}", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ViewCustomerOrders()
        {
            if (_selectedRow == null) return;
            string phone = _selectedRow["CustomerPhone"]?.ToString();
            string name = _selectedRow["CustomerName"]?.ToString();

            using (var dlg = new FrmCustomerOrdersHistoryDialog(phone, name))
            {
                dlg.ShowDialog(this);
            }
        }

        private void RegisterCurrentCustomerAsClient()
        {
            if (_selectedRow == null) return;

            string name = _selectedRow["CustomerName"]?.ToString();
            string phone = _selectedRow["CustomerPhone"]?.ToString();
            string addr = _selectedRow["CustomerAddress"]?.ToString();

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("لا يمكن تسجيل العميل بدون رقم هاتف صحيح!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"هل ترغب في تسجيل العميل:\n\n👤 الاسم: {name}\n📱 الهاتف: {phone}\n📍 العنوان: {addr}\n\nفي جدول العملاء الرسمي لتسهيل البيع الآجل ومتابعة كشف الحساب؟",
                "تأكيد تسجيل عميل",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            int newId = OnlineOrdersDAL.RegisterStoreCustomerAsClient(name, phone, addr, out string err);
            if (newId > 0)
            {
                MessageBox.Show($"✅ تم تسجيل العميل بنجاح في قاعدة البيانات!\nمعرف العميل الداخلي: {newId}", "تم التسجيل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadReport();
            }
            else
            {
                MessageBox.Show($"تعذر تسجيل العميل:\n{err}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // تصدير CSV / Excel
        // ==========================================
        private void ExportToCsv()
        {
            if (_dtCustomers == null || _dtCustomers.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات متاحة للتصدير!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "ملف CSV (*.csv)|*.csv|ملف نصي (*.txt)|*.txt";
                sfd.FileName = $"عملاء_المتجر_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        // رأس الملف مع UTF-8 BOM
                        sb.AppendLine("م,اسم العميل,رقم الموبايل,العنوان,إجمالي الطلبات,الطلبات المكتملة,إجمالي الإنفاق,المنفق المكتمل,تاريخ أول طلب,تاريخ آخر طلب,حالة التسجيل,كود العميل");

                        int seq = 1;
                        foreach (DataRow r in _dtCustomers.Rows)
                        {
                            string name = EscapeCsv(r["CustomerName"]?.ToString());
                            string phone = EscapeCsv(r["CustomerPhone"]?.ToString());
                            string addr = EscapeCsv(r["CustomerAddress"]?.ToString());
                            string orders = r["TotalOrdersCount"]?.ToString();
                            string compOrders = r["CompletedOrdersCount"]?.ToString();
                            string spent = r["TotalAmountSpent"] != DBNull.Value ? Convert.ToDecimal(r["TotalAmountSpent"]).ToString("F2") : "0.00";
                            string compSpent = r["CompletedAmountSpent"] != DBNull.Value ? Convert.ToDecimal(r["CompletedAmountSpent"]).ToString("F2") : "0.00";
                            string firstO = r["FirstOrderDate"] != DBNull.Value ? Convert.ToDateTime(r["FirstOrderDate"]).ToString("yyyy/MM/dd") : "";
                            string lastO = r["LastOrderDate"] != DBNull.Value ? Convert.ToDateTime(r["LastOrderDate"]).ToString("yyyy/MM/dd HH:mm") : "";
                            bool isReg = r["IsRegisteredClient"] != DBNull.Value && Convert.ToInt32(r["IsRegisteredClient"]) == 1;
                            string regStr = isReg ? "مسجل بالمنظومة" : "غير مسجل";
                            string code = r["ClientCode"]?.ToString();

                            sb.AppendLine($"{seq},{name},{phone},{addr},{orders},{compOrders},{spent},{compSpent},{firstO},{lastO},{regStr},{code}");
                            seq++;
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("تم تصدير تقرير عملاء المتجر بنجاح ✅", "تم التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ أثناء التصدير:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        // ==========================================
        // طباعة التقرير A4 (Print Document)
        // ==========================================
        private int _printRowIndex = 0;
        private int _printPageNum = 0;

        private void PrintReport()
        {
            if (_dtCustomers == null || _dtCustomers.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true;
            pd.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);

            _printRowIndex = 0;
            _printPageNum = 0;

            pd.PrintPage += (s, e) =>
            {
                _printPageNum++;
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int left = e.MarginBounds.Left;
                int right = e.MarginBounds.Right;
                int top = e.MarginBounds.Top;
                int width = e.MarginBounds.Width;
                int y = top;

                // الترويسة
                using (var fTitle = new Font("Segoe UI", 14f, FontStyle.Bold))
                using (var fSub = new Font("Segoe UI", 9f))
                using (var fBold = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var fCell = new Font("Segoe UI", 8.5f))
                {
                    string coName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة برو سوفت";
                    g.DrawString(coName, fTitle, Brushes.Black, right, y, new StringFormat { Alignment = StringAlignment.Far });
                    y += 24;
                    g.DrawString($"تقرير عملاء المتجر الإلكتروني - صفحة {_printPageNum}", fSub, Brushes.Gray, right, y, new StringFormat { Alignment = StringAlignment.Far });
                    g.DrawString($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", fSub, Brushes.Gray, left, y);
                    y += 20;

                    // خط فاصل
                    g.DrawLine(Pens.LightGray, left, y, right, y);
                    y += 8;

                    // في الصفحة الأولى نطبع شريط المؤشرات
                    if (_printPageNum == 1)
                    {
                        string kpiText = $"إجمالي العملاء: {_dtCustomers.Rows.Count} | إجمالي الطلبات: {lblKpiTotalOrders.Text} | المبيعات المكتملة: {lblKpiCompletedAmount.Text} | المسجلين بالمنظومة: {lblKpiRegisteredRatio.Text}";
                        g.FillRectangle(Brushes.AliceBlue, left, y, width, 24);
                        g.DrawRectangle(Pens.CornflowerBlue, left, y, width, 24);
                        g.DrawString(kpiText, fBold, Brushes.DarkBlue, right - 8, y + 4, new StringFormat { Alignment = StringAlignment.Far });
                        y += 32;
                    }

                    // رأس الجدول
                    int colSeq = 30;
                    int colName = 160;
                    int colPhone = 100;
                    int colOrders = 60;
                    int colComp = 60;
                    int colSpent = 90;
                    int colDate = 100;
                    int colStatus = 110;
                    int colAddr = width - (colSeq + colName + colPhone + colOrders + colComp + colSpent + colDate + colStatus);

                    int hY = y;
                    g.FillRectangle(Brushes.LightSteelBlue, left, hY, width, 24);
                    g.DrawRectangle(Pens.SteelBlue, left, hY, width, 24);

                    int curX = right;
                    DrawHeaderCell(g, "م", ref curX, colSeq, fBold, hY);
                    DrawHeaderCell(g, "اسم العميل", ref curX, colName, fBold, hY);
                    DrawHeaderCell(g, "الموبايل", ref curX, colPhone, fBold, hY);
                    DrawHeaderCell(g, "الطلبات", ref curX, colOrders, fBold, hY);
                    DrawHeaderCell(g, "المكتمل", ref curX, colComp, fBold, hY);
                    DrawHeaderCell(g, "الإنفاق (ج.م)", ref curX, colSpent, fBold, hY);
                    DrawHeaderCell(g, "آخر طلب", ref curX, colDate, fBold, hY);
                    DrawHeaderCell(g, "الحالة", ref curX, colStatus, fBold, hY);
                    DrawHeaderCell(g, "العنوان", ref curX, colAddr, fBold, hY);

                    y += 24;

                    // صفوف البيانات
                    while (_printRowIndex < _dtCustomers.Rows.Count)
                    {
                        if (y + 22 > e.MarginBounds.Bottom)
                        {
                            e.HasMorePages = true;
                            return;
                        }

                        DataRow r = _dtCustomers.Rows[_printRowIndex];
                        int rSeq = _printRowIndex + 1;
                        string rName = r["CustomerName"]?.ToString();
                        string rPhone = r["CustomerPhone"]?.ToString();
                        string rOrders = r["TotalOrdersCount"]?.ToString();
                        string rComp = r["CompletedOrdersCount"]?.ToString();
                        decimal rSpent = r["TotalAmountSpent"] != DBNull.Value ? Convert.ToDecimal(r["TotalAmountSpent"]) : 0m;
                        string rDate = r["LastOrderDate"] != DBNull.Value ? Convert.ToDateTime(r["LastOrderDate"]).ToString("yyyy/MM/dd") : "";
                        bool isReg = r["IsRegisteredClient"] != DBNull.Value && Convert.ToInt32(r["IsRegisteredClient"]) == 1;
                        string rStat = isReg ? $"مسجل ({r["ClientCode"]})" : "غير مسجل";
                        string rAddr = r["CustomerAddress"]?.ToString();

                        if (_printRowIndex % 2 == 1)
                            g.FillRectangle(Brushes.WhiteSmoke, left, y, width, 20);

                        g.DrawRectangle(Pens.Gainsboro, left, y, width, 20);

                        curX = right;
                        DrawDataCell(g, rSeq.ToString(), ref curX, colSeq, fCell, y, StringAlignment.Center);
                        DrawDataCell(g, rName, ref curX, colName, fCell, y, StringAlignment.Far);
                        DrawDataCell(g, rPhone, ref curX, colPhone, fCell, y, StringAlignment.Center);
                        DrawDataCell(g, rOrders, ref curX, colOrders, fCell, y, StringAlignment.Center);
                        DrawDataCell(g, rComp, ref curX, colComp, fCell, y, StringAlignment.Center);
                        DrawDataCell(g, rSpent.ToString("N1"), ref curX, colSpent, fCell, y, StringAlignment.Far);
                        DrawDataCell(g, rDate, ref curX, colDate, fCell, y, StringAlignment.Center);
                        DrawDataCell(g, rStat, ref curX, colStatus, fCell, y, StringAlignment.Center);
                        DrawDataCell(g, rAddr, ref curX, colAddr, fCell, y, StringAlignment.Far);

                        y += 20;
                        _printRowIndex++;
                    }

                    e.HasMorePages = false;
                }
            };

            using (var ppd = new PrintPreviewDialog { Document = pd, Width = 980, Height = 650 })
            {
                ppd.ShowDialog(this);
            }
        }

        private void DrawHeaderCell(Graphics g, string text, ref int x, int w, Font f, int y)
        {
            x -= w;
            var rect = new RectangleF(x, y + 3, w, 20);
            g.DrawString(text, f, Brushes.Black, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }

        private void DrawDataCell(Graphics g, string text, ref int x, int w, Font f, int y, StringAlignment align)
        {
            x -= w;
            var rect = new RectangleF(x + 2, y + 2, w - 4, 18);
            g.DrawString(text ?? "", f, Brushes.Black, rect, new StringFormat { Alignment = align, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
        }

        private void FrmOnlineStoreCustomersReport_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                LoadReport();
            }
            else if (e.KeyCode == Keys.F9)
            {
                e.Handled = true;
                PrintReport();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }
    }

    /// <summary>
    /// نافذة عرض سجل كافة طلبات عميل محدد بالمتجر الإلكتروني
    /// </summary>
    public class FrmCustomerOrdersHistoryDialog : Form
    {
        private DataGridView dgvOrders;
        private DataGridView dgvItems;
        private Label lblHeaderInfo;
        private string _phone;
        private string _custName;

        public FrmCustomerOrdersHistoryDialog(string phone, string custName)
        {
            _phone = phone;
            _custName = custName;
            InitializeComponent();
            LoadOrders();
        }

        private void InitializeComponent()
        {
            this.Text = $"📜 سجل طلبات العميل | {_custName} ({_phone})";
            this.Size = new Size(880, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.Font = new Font("Segoe UI", 9f);
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };

            // Header
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12, 8, 12, 8)
            };

            lblHeaderInfo = new Label
            {
                Text = $"👤 سجل طلبات العميل: {_custName} | الهاتف: {_phone}",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlTop.Controls.Add(lblHeaderInfo);

            // Split Container (Top: Orders, Bottom: Items)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 280,
                SplitterWidth = 5,
                BackColor = Color.FromArgb(51, 65, 85)
            };

            // Top: Orders Grid
            dgvOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };
            dgvOrders.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvOrders.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(56, 189, 248);
            dgvOrders.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvOrders.DefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 235);
            dgvOrders.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(20, 29, 47);
            dgvOrders.SelectionChanged += (s, e) => LoadSelectedOrderItems();

            split.Panel1.Controls.Add(dgvOrders);

            // Bottom: Order Items
            var pnlItemsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 26, 43),
                Padding = new Padding(8)
            };

            var lblItemsTitle = new Label
            {
                Text = "📦 محتويات وأصناف الطلب المحدد:",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 191, 36)
            };

            dgvItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(18, 26, 43),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };
            dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
            dgvItems.DefaultCellStyle.BackColor = Color.FromArgb(18, 26, 43);
            dgvItems.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 58, 138);

            pnlItemsContainer.Controls.Add(dgvItems);
            pnlItemsContainer.Controls.Add(lblItemsTitle);
            split.Panel2.Controls.Add(pnlItemsContainer);

            // Bottom bar
            var pnlClose = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(8)
            };

            var btnDlgClose = new Button
            {
                Text = "إغلاق النافذة",
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Left,
                Width = 120,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDlgClose.FlatAppearance.BorderSize = 0;
            btnDlgClose.Click += (s, e) => this.Close();
            pnlClose.Controls.Add(btnDlgClose);

            this.Controls.Add(split);
            this.Controls.Add(pnlClose);
            this.Controls.Add(pnlTop);
        }

        private void LoadOrders()
        {
            var dt = OnlineOrdersDAL.GetCustomerOrders(_phone);
            dgvOrders.AutoGenerateColumns = false;
            dgvOrders.Columns.Clear();

            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "OnlineOrderID", DataPropertyName = "OnlineOrderID", Visible = false });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderNumber", DataPropertyName = "OrderNumber", HeaderText = "رقم الطلب", FillWeight = 15 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderDate", DataPropertyName = "OrderDate", HeaderText = "تاريخ الطلب", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd HH:mm" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemsCount", DataPropertyName = "ItemsCount", HeaderText = "عدد الأصناف", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", DataPropertyName = "SubTotal", HeaderText = "قيمة الأصناف", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "DeliveryCharge", DataPropertyName = "DeliveryCharge", HeaderText = "التوصيل", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", DataPropertyName = "TotalAmount", HeaderText = "الإجمالي", FillWeight = 16, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2", ForeColor = Color.FromArgb(52, 211, 153) } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", DataPropertyName = "Status", HeaderText = "الحالة", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            dgvOrders.DataSource = dt;
        }

        private void LoadSelectedOrderItems()
        {
            if (dgvOrders.SelectedRows.Count == 0)
            {
                dgvItems.DataSource = null;
                return;
            }

            var drv = dgvOrders.SelectedRows[0].DataBoundItem as DataRowView;
            if (drv == null) return;

            int oid = Convert.ToInt32(drv["OnlineOrderID"]);
            var dtItems = OnlineOrdersDAL.GetOrderItems(oid, false);

            dgvItems.AutoGenerateColumns = false;
            dgvItems.Columns.Clear();
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "اسم الصنف", FillWeight = 40 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitName", HeaderText = "الوحدة", FillWeight = 15 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "الكمية", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice", HeaderText = "السعر", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalPrice", HeaderText = "الإجمالي", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2", ForeColor = Color.FromArgb(52, 211, 153) } });

            dgvItems.DataSource = dtItems;
        }
    }
}

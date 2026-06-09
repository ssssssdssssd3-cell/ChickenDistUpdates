using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmMain : Form
    {
        private Panel pnlTopBar, pnlContent;
        private FlowLayoutPanel pnlNavBar;
        private Label lblUserInfo, lblCompany;
        private Form _currentChild;

        public FrmMain()
        {
            InitializeComponent();
            NavigateTo(new FrmDashboard());
        }

        private void InitializeComponent()
        {
            this.Text = AppConfig.CompanyName + " - النظام الرئيسي | الإصدار: " + UpdateManager.CurrentVersion;
            this.Size = new Size(1280, 780);
            this.MinimumSize = new Size(1024, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Theme.BgLight;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Icon extract failed: " + ex.Message); }

            // ===== TopBar =====
            this.pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Theme.Primary };
            this.lblCompany = new Label
            {
                Text = "🐣  " + AppConfig.CompanyName,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.lblUserInfo = new Label
            {
                Text = $"👤 {Session.EmpName}  |  {Session.Role}",
                Font = Theme.FontSmall,
                ForeColor = Color.FromArgb(200, 230, 255),
                AutoSize = false,
                Width = 250,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(10, 0, 0, 0)
            };
            var btnLogoutTop = new Button
            {
                Text = "خروج ↩",
                Width = 100,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Danger,
                ForeColor = Color.White,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnLogoutTop.FlatAppearance.BorderSize = 0;
            btnLogoutTop.Click += (s, e) => { if (MessageBox.Show("هل تريد تسجيل الخروج؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { Session.Clear(); this.Close(); } };
            this.pnlTopBar.Controls.AddRange(new Control[] { lblCompany, lblUserInfo, btnLogoutTop });

            // ===== NavBar (previously Sidebar) =====
            pnlNavBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.Sidebar,
                Padding = new Padding(10, 5, 10, 5)
            };
            BuildNavBar();

            // ===== Content =====
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgLight,
                Padding = new Padding(0)
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlNavBar);
            this.Controls.Add(this.pnlTopBar);
            
            this.pnlTopBar.SendToBack(); // Docks first (takes top edge)
            pnlNavBar.SendToBack();      // Docks second (takes space below top edge)
            pnlContent.BringToFront();   // Docks last (fills remaining space)
            
            this.Resize += (s, e) => UpdateLayout();
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            pnlTopBar.Dock = DockStyle.Top;
            pnlNavBar.Dock = DockStyle.Top; 
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(0);
        }

        public void UpdateCompanyName(string newName)
        {
            lblCompany.Text = $"🐣  {newName}";
            this.Text = $"{newName} - النظام الرئيسي";
        }

        private void BuildNavBar()
        {
            pnlNavBar.Controls.Clear();

            // كل مجموعة: (icon, label, screen, action, groupColor)
            // groupColor = null يعني الشفافية الافتراضية
            var groups = new (string icon, string label, string screen, Action action, Color btnColor)[]
            {
                // ── الرئيسية ──────────────────────────────────────────
                ("🏠", "الرئيسية",        "",               () => NavigateTo(new FrmDashboard()),      Color.FromArgb(55, 65, 81)),

                // ── البضاعة والمخزون ──────────────────────────────────
                ("📦", "الأصناف",          "Products",       () => NavigateTo(new FrmProducts()),       Color.FromArgb(17, 94, 89)),
                // ("🏷️", "التصنيفات",        "Products",       () => NavigateTo(new FrmCategories()),     Color.FromArgb(17, 94, 89)),
                ("🏢", "المخازن",          "Inventory",      () => NavigateTo(new FrmWarehouses()),     Color.FromArgb(17, 94, 89)),
                ("⚖️", "جرد المخزن",      "Inventory",      () => NavigateTo(new FrmInventory()),      Color.FromArgb(17, 94, 89)),
                ("🔄", "تحويل مخزني",      "Inventory",      () => NavigateTo(new FrmWarehouseTransfer()), Color.FromArgb(17, 94, 89)),
                ("📋", "سجل التحويلات",    "Inventory",      () => NavigateTo(new FrmWarehouseTransfersList()), Color.FromArgb(17, 94, 89)),

                // ── العملاء والموردين ──────────────────────────────────
                ("👥", "العملاء",          "Clients",        () => NavigateTo(new FrmClients()),        Color.FromArgb(30, 64, 175)),
                ("🤝", "الموردين",         "Suppliers",      () => NavigateTo(new FrmSuppliers()),      Color.FromArgb(30, 64, 175)),
                ("🚗", "المركبات",          "Vehicles",       () => NavigateTo(new FrmVehicles()),       Color.FromArgb(30, 64, 175)),

                // ── المبيعات (مجموعة متكاملة) ─────────────────────────
                ("🛒", "فاتورة بيع",       "Sales",          () => NavigateTo(new FrmSale()),           Color.FromArgb(5, 122, 85)),
                ("↩", "مرتجع بيع",        "Returns",        () => NavigateTo(new FrmReturn()),         Color.FromArgb(5, 122, 85)),
                ("📋", "سجل المبيعات",     "Sales",          () => NavigateTo(new FrmSalesList()),      Color.FromArgb(5, 122, 85)),
                ("📑", "سجل التعديلات",    "Sales",          () => NavigateTo(new FrmSalesAuditList()), Color.FromArgb(5, 122, 85)),

                // ── المشتريات (مجموعة متكاملة) ───────────────────────
                ("📥", "فاتورة شراء",      "Purchases",      () => NavigateTo(new FrmPurchase()),       Color.FromArgb(120, 53, 15)),
                ("↩", "مرتجع شراء",       "Purchases",      () => NavigateTo(new FrmPurchaseReturn()), Color.FromArgb(120, 53, 15)),
                ("📋", "سجل المشتريات",    "Purchases",      () => NavigateTo(new FrmPurchasesList()),  Color.FromArgb(120, 53, 15)),

                // ── المناديب ──────────────────────────────────────────
                ("🚚", "حمولة مندوب",     "DriverHandover", () => NavigateTo(new FrmDriverHandover()), Color.FromArgb(109, 40, 217)),
                ("📡", "بوابة المندوب",    "DriverSales",    () => NavigateTo(new FrmDriverPortal()),    Color.FromArgb(109, 40, 217)),
                ("📥", "استيراد CSV",     "ImportPreview",  () => OpenImportPreviewDialog(),          Color.FromArgb(109, 40, 217)),
                ("🖥️", "مراقبة المناديب", "DriverHandover", () => NavigateTo(new FrmDriversMonitor()), Color.FromArgb(109, 40, 217)),
                ("📋", "عهدة المناديب",   "DriverHandover", () => NavigateTo(new FrmDriverCustody()),  Color.FromArgb(109, 40, 217)),
                ("🏆", "أداء المناديب",   "DriverHandover", () => NavigateTo(new FrmDriverLeaderboard()), Color.FromArgb(80, 30, 190)),

                // ── المالية ───────────────────────────────────────────
                ("💰", "الخزنة",           "CashBox",        () => NavigateTo(new FrmCashBox()),        Color.FromArgb(159, 18, 57)),
                ("📊", "التقارير",         "Reports",        () => NavigateTo(new FrmReports()),        Color.FromArgb(159, 18, 57)),
                ("📑", "تقفيل يومية",      "Reports",        () => NavigateTo(new FrmDailyClosing()),   Color.FromArgb(159, 18, 57)),

                // ── الإدارة ───────────────────────────────────────────
                ("👔", "الموظفين",         "Employees",      () => NavigateTo(new FrmEmployees()),      Color.FromArgb(55, 65, 81)),
                ("💰", "حسابات الموظفين",  "Employees",      () => NavigateTo(new FrmEmployeeTransactions()), Color.FromArgb(55, 65, 81)),
                ("⚙️", "الإعدادات",       "",               () => new FrmSettings().ShowDialog(),      Color.FromArgb(55, 65, 81)),
                ("🔄", "تحديث البرنامج",   "",               () => UpdateManager.CheckForUpdates(true), Color.FromArgb(55, 65, 81)),
            };

            var buttonsList = new System.Collections.Generic.List<Button>();

            foreach (var item in groups)
            {
                if (!string.IsNullOrEmpty(item.screen) && !Session.CanAccess(item.screen)) continue;

                var btn = new Button
                {
                    Name      = item.label,
                    Text      = $"{item.icon}\n{item.label}",
                    Size      = new Size(105, 52),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = item.btnColor,
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin    = new Padding(3, 4, 3, 4),
                    Cursor    = Cursors.Hand,
                    ImageAlign = ContentAlignment.TopCenter
                };
                btn.FlatAppearance.BorderSize          = 0;
                btn.FlatAppearance.BorderColor          = Color.FromArgb(80, 255, 255, 255);
                btn.FlatAppearance.MouseOverBackColor   = ControlPaint.Light(item.btnColor, 0.3f);
                btn.FlatAppearance.MouseDownBackColor   = ControlPaint.Dark(item.btnColor, 0.1f);
                
                var act = item.action;
                btn.Click += (s, e) => act();

                // ── تفعيل السحب والإفلات (Drag & Drop) لإعادة ترتيب الأيقونات ──
                Point dragStartPoint = Point.Empty;

                btn.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        dragStartPoint = e.Location;
                    }
                };

                btn.MouseMove += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left && dragStartPoint != Point.Empty)
                    {
                        int dragWidth = SystemInformation.DragSize.Width;
                        int dragHeight = SystemInformation.DragSize.Height;
                        Rectangle dragRect = new Rectangle(
                            dragStartPoint.X - dragWidth / 2,
                            dragStartPoint.Y - dragHeight / 2,
                            dragWidth,
                            dragHeight);
                        if (!dragRect.Contains(e.Location))
                        {
                            btn.DoDragDrop(btn, DragDropEffects.Move);
                            dragStartPoint = Point.Empty;
                        }
                    }
                };

                btn.MouseUp += (s, e) =>
                {
                    dragStartPoint = Point.Empty;
                };

                btn.AllowDrop = true;

                btn.DragEnter += (s, e) =>
                {
                    if (e.Data.GetDataPresent(typeof(Button)))
                    {
                        e.Effect = DragDropEffects.Move;
                    }
                };

                btn.DragOver += (s, e) =>
                {
                    if (e.Data.GetDataPresent(typeof(Button)))
                    {
                        e.Effect = DragDropEffects.Move;
                    }
                };

                btn.DragDrop += (s, e) =>
                {
                    if (e.Data.GetData(typeof(Button)) is Button dragged && dragged != btn)
                    {
                        int targetIndex = pnlNavBar.Controls.GetChildIndex(btn);
                        pnlNavBar.Controls.SetChildIndex(dragged, targetIndex);
                        SaveNavBarOrder();
                    }
                };

                buttonsList.Add(btn);
            }

            // تحميل الترتيب المخصص للأيقونات من Settings.ini
            try
            {
                string savedOrder = LicenseManager.ReadIniValue("NavBarOrder", "Order", "");
                if (!string.IsNullOrWhiteSpace(savedOrder))
                {
                    var orderedNames = savedOrder.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var orderedList = new System.Collections.Generic.List<Button>();

                    foreach (var name in orderedNames)
                    {
                        var btn = buttonsList.Find(b => b.Name == name.Trim());
                        if (btn != null)
                        {
                            orderedList.Add(btn);
                            buttonsList.Remove(btn);
                        }
                    }
                    orderedList.AddRange(buttonsList); // إضافة أي أزرار متبقية أو جديدة
                    buttonsList = orderedList;
                }
            }
            catch { }

            // إضافة الأزرار إلى الشريط
            foreach (var btn in buttonsList)
            {
                pnlNavBar.Controls.Add(btn);
            }
        }

        private void SaveNavBarOrder()
        {
            try
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (Control ctrl in pnlNavBar.Controls)
                {
                    if (ctrl is Button btn && !string.IsNullOrEmpty(btn.Name))
                    {
                        names.Add(btn.Name);
                    }
                }
                LicenseManager.WriteIniValue("NavBarOrder", "Order", string.Join(",", names));
            }
            catch { }
        }

        public void NavigateTo(Form form)
        {
            if (_currentChild != null && !_currentChild.IsDisposed)
                _currentChild.Close();

            _currentChild = form;
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.FormBorderStyle = FormBorderStyle.None;
            form.RightToLeft = RightToLeft.Yes;

            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(form);
            form.Show();
            form.BringToFront();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // نسخ احتياطي صامت عند الخروج لمرة واحدة في اليوم
                var lastBackup = BackupManager.LastBackupTime;
                if (lastBackup == null || lastBackup.Value.Date < DateTime.Today)
                {
                    BackupManager.DoBackup(silent: true);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("AutoBackup on exit failed", ex, "FrmMain");
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // إيقاف خادم المندوب بأمان عند إغلاق البرنامج
            Core.DriverPortalServer.Stop();
            base.OnFormClosed(e);
        }

        private void OpenImportPreviewDialog()
        {
            try
            {
                using (var openDlg = new OpenFileDialog())
                {
                    openDlg.Title = "اختر ملف CSV للمندوب";
                    openDlg.Filter = "CSV Files|*.csv|All Files|*.*";
                    if (openDlg.ShowDialog() == DialogResult.OK)
                    {
                        using (var driverDlg = new Form())
                        {
                            driverDlg.Text = "اختر المندوب";
                            driverDlg.Size = new Size(350, 180);
                            driverDlg.StartPosition = FormStartPosition.CenterParent;
                            driverDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                            driverDlg.MaximizeBox = false;
                            driverDlg.MinimizeBox = false;
                            driverDlg.RightToLeft = RightToLeft.Yes;
                            driverDlg.BackColor = Theme.BgMain;

                            var lbl = new Label { Text = "اختر المندوب للاستيراد:", Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.TextMain };
                            var cbo = new ComboBox { Location = new Point(20, 45), Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

                            var dt = EmployeeDAL.GetDrivers();
                            foreach (DataRow r in dt.Rows)
                                cbo.Items.Add(new ComboItem((int)r["EmpID"], r["EmpName"].ToString()));
                            cbo.DisplayMember = "Text";
                            if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;

                            var btnOk = Theme.MakeButton("📥 بدء الاستيراد", 180, 90, 130, 32, Theme.Accent);
                            btnOk.Click += (senderDlg, eDlg) => {
                                if (cbo.SelectedItem is ComboItem ci)
                                {
                                    driverDlg.DialogResult = DialogResult.OK;
                                    driverDlg.Close();

                                    var preview = new FrmImportPreview(openDlg.FileName, DateTime.Today, ci.ID, ci.Text);
                                    NavigateTo(preview);
                                }
                            };

                            driverDlg.Controls.AddRange(new Control[] { lbl, cbo, btnOk });
                            driverDlg.ShowDialog(this);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل بدء الاستيراد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ===== Dashboard =====
    public class FrmDashboard : Form
    {
        public FrmDashboard()
        {
            this.BackColor = Theme.BgMain;
            this.RightToLeft = RightToLeft.Yes;
            BuildUI();
        }

        private void BuildUI()
        {
            this.Controls.Clear();

            // Main Table Layout (full screen)
            var mainTbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain
            };
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f)); // Header
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f)); // Cards
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Dynamic content split

            // 1. Header
            var pnlTitle = Theme.MakeTitleBar("لوحة التحكم والمؤشرات اليومية", $"مرحباً {Session.EmpName} 👋  |  تاريخ اليوم: {DateTime.Today:dd/MM/yyyy}");
            pnlTitle.BackColor = Theme.BgCard;
            mainTbl.Controls.Add(pnlTitle, 0, 0);

            // 2. Cards (FlowLayout)
            var pnlCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Theme.BgMain,
                Padding = new Padding(10, 5, 10, 5)
            };

            try
            {
                decimal cashBal = AccountDAL.GetCashBalance();
                var salesDt = ReportDAL.SalesByDay(DateTime.Today, DateTime.Today);
                decimal todaySales = salesDt.Rows.Count > 0 ? Convert.ToDecimal(salesDt.Rows[0]["Total"]) : 0;
                var openLoads = DriverDAL.GetOpenLoads();

                pnlCards.Controls.Add(MakeCard("💰 رصيد الخزنة الحالي", cashBal.ToString("N2") + " ج", Theme.Success));
                pnlCards.Controls.Add(MakeCard("🛒 مبيعات اليوم", todaySales.ToString("N2") + " ج", Theme.Accent));
                pnlCards.Controls.Add(MakeCard("🚚 حمولات مفتوحة حالياً", openLoads.Rows.Count + " حمولة", Color.FromArgb(52, 152, 219)));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Load dashboard cards failed: " + ex.Message); }
            mainTbl.Controls.Add(pnlCards, 0, 1);

            // 3. Lower Split (Quick Actions & Recent Invoices)
            var lowerTbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain
            };
            lowerTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f)); // Column 0: Quick Actions (35%)
            lowerTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f)); // Column 1: Recent Invoices (65%)
            lowerTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // 3a. Left Column: Quick Actions
            var pnlActions = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15),
                Margin = new Padding(15, 0, 8, 15)
            };
            var lblActTitle = new Label { Text = "⚡ إجراءات سريعة", Font = Theme.FontHeader, ForeColor = Theme.Accent, Location = new Point(15, 15), AutoSize = true };
            pnlActions.Controls.Add(lblActTitle);

            int btnY = 55;
            AddQuickButton(pnlActions, "🛒 فاتورة مبيعات جديدة", ref btnY, () => NavigateMain(new FrmSale()), Theme.Accent);
            AddQuickButton(pnlActions, "🚗 المركبات والتحميل", ref btnY, () => NavigateMain(new FrmVehicles()), Color.FromArgb(55, 135, 195));
            AddQuickButton(pnlActions, "🚚 تسليم حمولة مندوب", ref btnY, () => NavigateMain(new FrmDriverHandover()), Theme.Primary);
            AddQuickButton(pnlActions, "💰 تحصيل نقدي للخزنة", ref btnY, () => NavigateMain(new FrmCashBox()), Theme.Success);
            AddQuickButton(pnlActions, "📦 جرد المخزن والأصناف", ref btnY, () => NavigateMain(new FrmInventory()), Color.FromArgb(120, 120, 80));
            AddQuickButton(pnlActions, "👥 كشف حساب العملاء", ref btnY, () => NavigateMain(new FrmClients()), Color.FromArgb(100, 100, 150));
            AddQuickButton(pnlActions, "📊 التقارير والإحصائيات", ref btnY, () => NavigateMain(new FrmReports()), Color.FromArgb(150, 100, 100));

            lowerTbl.Controls.Add(pnlActions, 0, 0);

            // 3b. Right Column: Recent Sales Invoices
            var pnlRecent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15),
                Margin = new Padding(8, 0, 15, 15)
            };
            var lblRecTitle = new Label { Text = "📋 أحدث مبيعات وحمولات اليوم", Font = Theme.FontHeader, ForeColor = Theme.TextMain, Location = new Point(15, 15), AutoSize = true };
            pnlRecent.Controls.Add(lblRecTitle);

            var dgRecent = new DataGridView
            {
                Location = new Point(15, 55),
                Size = new Size(540, 360),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", HeaderText = "كود الفاتورة", FillWeight = 40 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleType", HeaderText = "النوع", FillWeight = 30 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "العميل / المندوب", FillWeight = 50 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "المبلغ الإجمالي", FillWeight = 35 });

            try
            {
                var dtSales = SaleDAL.GetAll(DateTime.Today, DateTime.Today);
                int limit = 0;
                foreach (DataRow r in dtSales.Rows)
                {
                    if (limit++ >= 8) break; // Display only last 8 sales
                    string clientOrDriver = r["SaleType"].ToString() == "DriverLoad" ? r["DriverName"].ToString() : r["ClientName"].ToString();
                    string typeArabic = r["SaleType"].ToString() == "Cash" ? "نقدي" : r["SaleType"].ToString() == "Credit" ? "آجل" : "تحميل مندوب";
                    dgRecent.Rows.Add(r["SaleCode"], typeArabic, clientOrDriver, Convert.ToDecimal(r["TotalAmount"]).ToString("N2") + " ج");
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Load recent sales failed: " + ex.Message); }

            pnlRecent.Controls.Add(dgRecent);
            lowerTbl.Controls.Add(pnlRecent, 1, 0);

            mainTbl.Controls.Add(lowerTbl, 0, 2);
            this.Controls.Add(mainTbl);
        }

        private Panel MakeCard(string title, string value, Color color)
        {
            var card = new Panel
            {
                Size = new Size(240, 110),
                BackColor = Theme.BgCard,
                Margin = new Padding(10),
                Cursor = Cursors.Hand
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.FillRectangle(new SolidBrush(color), 0, 0, 6, 110);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = Theme.FontBold,
                ForeColor = Theme.TextSub,
                Location = new Point(15, 18),
                AutoSize = true
            };
            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(15, 45),
                AutoSize = true
            };
            card.Controls.AddRange(new Control[] { lblTitle, lblValue });
            return card;
        }

        private void AddQuickButton(Panel p, string text, ref int y, Action onClick, Color hoverColor)
        {
            var btn = Theme.MakeButton(text, 15, y, 220, 42, Theme.Primary);
            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            btn.Click += (s, e) => onClick();
            btn.TextAlign = ContentAlignment.MiddleRight;
            btn.Padding = new Padding(0, 0, 10, 0);
            p.Controls.Add(btn);
            y += 50;
        }

        private void NavigateMain(Form form)
        {
            if (this.ParentForm is FrmMain main)
            {
                main.NavigateTo(form);
            }
        }
    }
}



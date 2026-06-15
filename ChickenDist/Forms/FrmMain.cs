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

            // ── تعريف المجموعات مع قوائمها المنسدلة ──
            // كل مجموعة: (ايكون، عنوان، لون، قائمة العناصر[(عنوان، screen، action)])
            var groups = new (string icon, string label, Color color, (string text, string screen, Action action)[] items)[]
            {
                ("🏠", "الرئيسية", Color.FromArgb(55, 65, 81), new[] {
                    ("🏠 الرئيسية", "", (Action)(() => NavigateTo(new FrmDashboard())))
                }),

                ("🛒", "المبيعات", Color.FromArgb(5, 122, 85), new[] {
                    ("🛒 فاتورة بيع",    "Sales",      (Action)(() => NavigateTo(new FrmSale()))),
                    ("↩ مرتجع بيع",     "Returns",    (Action)(() => NavigateTo(new FrmReturn()))),
                    ("💳 عقود التقسيط", "Installments", (Action)(() => NavigateTo(new FrmInstallments()))),
                    ("📋 سجل المبيعات", "Sales",      (Action)(() => NavigateTo(new FrmSalesList()))),
                    ("📑 سجل التعديلات","SalesAudit", (Action)(() => NavigateTo(new FrmSalesAuditList()))),
                    ("📡 بوابة المحاسب",  "Sales",      (Action)(() => NavigateTo(new FrmAccountantPortal()))),
                }),

                ("📥", "المشتريات", Color.FromArgb(120, 53, 15), new[] {
                    ("📥 فاتورة شراء",    "Purchases",      (Action)(() => NavigateTo(new FrmPurchase()))),
                    ("↩ مرتجع شراء",     "PurchaseReturn", (Action)(() => NavigateTo(new FrmPurchaseReturn()))),
                    ("📋 سجل المشتريات", "Purchases",      (Action)(() => NavigateTo(new FrmPurchasesList()))),
                }),

                ("📦", "المخازن", Color.FromArgb(17, 94, 89), new[] {
                    ("📦 الأصناف",          "Products",          (Action)(() => NavigateTo(new FrmProducts()))),
                    ("📥 استيراد الأصناف",   "Products",          (Action)(() => NavigateTo(new FrmImportProducts()))),
                    ("🏢 المخازن",          "Warehouses",        (Action)(() => NavigateTo(new FrmWarehouses()))),
                    ("⚖️ جرد المخزن",      "Inventory",         (Action)(() => NavigateTo(new FrmInventory()))),
                    ("🗑️ الهوالك والتالف",  "Inventory",         (Action)(() => NavigateTo(new FrmWastage()))),
                    ("🔄 تحويل مخزني",     "WarehouseTransfers",(Action)(() => NavigateTo(new FrmWarehouseTransfer()))),
                    ("📋 سجل التحويلات",   "WarehouseTransfers",(Action)(() => NavigateTo(new FrmWarehouseTransfersList()))),
                    ("📊 سجل تغير الأسعار", "Products",          (Action)(() => NavigateTo(new FrmPriceChanges()))),
                    ("🏷️ طباعة الباركود (مجمع)", "Products",     (Action)(() => NavigateTo(new FrmBulkPrintBarcodes()))),
                }),

                ("👥", "العملاء", Color.FromArgb(30, 64, 175), new[] {
                    ("👥 العملاء",   "Clients",   (Action)(() => NavigateTo(new FrmClients()))),
                    ("🤝 الموردين",  "Suppliers", (Action)(() => NavigateTo(new FrmSuppliers()))),
                    ("🚗 المركبات",  "Vehicles",  (Action)(() => NavigateTo(new FrmVehicles()))),
                }),

                ("🚚", "المناديب", Color.FromArgb(109, 40, 217), new[] {
                    ("🚚 حمولة مندوب",      "DriverHandover", (Action)(() => NavigateTo(new FrmDriverHandover()))),
                    ("📡 بوابة المندوب",    "DriverSales",    (Action)(() => NavigateTo(new FrmDriverPortal()))),
                    ("☁️ استيراد من السحاب", "ImportPreview",  (Action)(() => OpenCloudImportDialog())),
                    ("🖥️ مراقبة المناديب", "DriversMonitor", (Action)(() => NavigateTo(new FrmDriversMonitor()))),
                    ("📋 عهدة المناديب",   "DriverHandover", (Action)(() => NavigateTo(new FrmDriverCustody()))),
                    ("🏆 أداء المناديب",   "DriverHandover", (Action)(() => NavigateTo(new FrmDriverLeaderboard()))),
                }),

                ("💰", "المالية", Color.FromArgb(159, 18, 57), new[] {
                    ("💰 الخزنة",       "CashBox",      (Action)(() => NavigateTo(new FrmCashBox()))),
                    ("📊 التقارير",     "Reports",      (Action)(() => NavigateTo(new FrmReports()))),
                    ("📑 تقفيل يومية", "DailyClosing", (Action)(() => NavigateTo(new FrmDailyClosing()))),
                }),

                ("⚙️", "الإدارة", Color.FromArgb(55, 65, 81), new[] {
                    ("👔 الموظفين",          "Employees",            (Action)(() => NavigateTo(new FrmEmployees()))),
                    ("💰 حسابات الموظفين",  "EmployeeTransactions", (Action)(() => NavigateTo(new FrmEmployeeTransactions()))),
                    ("⚙️ الإعدادات",        "Settings",             (Action)(() => new FrmSettings().ShowDialog())),
                    ("🔄 تحديث البرنامج",   "",                     (Action)(() => UpdateManager.CheckForUpdates(true))),
                }),
            };

            foreach (var group in groups)
            {
                // تحقق إذا المجموعة كلها ليس لها صلاحية → تخطَّ
                bool hasAnyAccess = false;
                foreach (var item in group.items)
                {
                    if (string.IsNullOrEmpty(item.screen) || Session.CanAccess(item.screen))
                    { hasAnyAccess = true; break; }
                }
                if (!hasAnyAccess) continue;

                // بناء القائمة المنسدلة
                var menu = new ContextMenuStrip();
                menu.BackColor  = Color.FromArgb(30, 35, 45);
                menu.ForeColor  = Color.White;
                menu.Font       = new Font("Segoe UI", 10f);
                menu.ShowImageMargin = false;
                menu.Renderer   = new ToolStripProfessionalRenderer(new DarkMenuColorTable());

                foreach (var item in group.items)
                {
                    if (!string.IsNullOrEmpty(item.screen) && !Session.CanAccess(item.screen)) continue;
                    var menuItem = new ToolStripMenuItem(item.text)
                    {
                        ForeColor = Color.White,
                        BackColor = Color.FromArgb(30, 35, 45),
                        Padding   = new Padding(8, 6, 8, 6)
                    };
                    var act = item.action;
                    menuItem.Click += (s, e) => act();
                    menu.Items.Add(menuItem);
                }

                // الزر الرئيسي للمجموعة
                var btn = new Button
                {
                    Name      = group.label,
                    Text      = $"{group.icon}\n{group.label} ▾",
                    Size      = new Size(108, 54),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = group.color,
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin    = new Padding(3, 4, 3, 4),
                    Cursor    = Cursors.Hand,
                };
                btn.FlatAppearance.BorderSize        = 0;
                btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(group.color, 0.3f);
                btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(group.color, 0.1f);

                // زر الرئيسية يتنقل مباشرة بدون قائمة
                if (group.label == "الرئيسية")
                {
                    btn.Text = "🏠\nالرئيسية";
                    btn.Click += (s, e) => NavigateTo(new FrmDashboard());
                }
                else
                {
                    btn.Click += (s, e) =>
                    {
                        menu.Show(btn, new System.Drawing.Point(0, btn.Height));
                    };
                }

                pnlNavBar.Controls.Add(btn);
            }
        }

        // جدول ألوان القائمة الداكنة
        private class DarkMenuColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected         => Color.FromArgb(55, 65, 90);
            public override Color MenuItemBorder           => Color.FromArgb(70, 80, 100);
            public override Color MenuBorder               => Color.FromArgb(50, 55, 70);
            public override Color ToolStripDropDownBackground => Color.FromArgb(30, 35, 45);
            public override Color ImageMarginGradientBegin => Color.FromArgb(30, 35, 45);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 35, 45);
            public override Color ImageMarginGradientEnd   => Color.FromArgb(30, 35, 45);
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // إيقاف خادم المندوب بأمان عند إغلاق البرنامج
            Core.DriverPortalServer.Stop();
            base.OnFormClosed(e);
        }

        private void OpenCloudImportDialog()
        {
            try
            {
                string code = "";
                if (ShowInputDialog("☁️ استيراد من السحاب", "أدخل رمز الاستيراد المكون من 5 حروف أو أكثر:", ref code))
                {
                    code = code.Trim();
                    if (string.IsNullOrEmpty(code)) return;

                    string tempFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", $"temp_import_{code}.csv");
                    string scratchDir = System.IO.Path.GetDirectoryName(tempFile);
                    if (!System.IO.Directory.Exists(scratchDir)) System.IO.Directory.CreateDirectory(scratchDir);

                    string csvContent = "";
                    using (var wc = new System.Net.WebClient())
                    {
                        wc.Encoding = System.Text.Encoding.UTF8;
                        wc.Headers[System.Net.HttpRequestHeader.UserAgent] = "ChickenDistApp";
                        
                        string downloadUrl = $"https://api.pastes.dev/raw/{code}";
                        try
                        {
                            csvContent = wc.DownloadString(downloadUrl);
                        }
                        catch
                        {
                            csvContent = wc.DownloadString($"https://api.pastes.dev/{code}");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(csvContent) || csvContent.Contains("{\"error\""))
                    {
                        throw new Exception("الرمز غير صحيح، أو انتهت صلاحيته.");
                    }

                    string decryptedCsv = SecurityHelper.Decrypt(csvContent);

                    if (string.IsNullOrWhiteSpace(decryptedCsv) || (!decryptedCsv.Contains("رقم_الفاتورة") && !decryptedCsv.Contains("رقم_الطلب")))
                    {
                        throw new Exception("المستند المحمل ليس كشف مبيعات أو طلبات صالح.");
                    }

                    System.IO.File.WriteAllText(tempFile, decryptedCsv, System.Text.Encoding.UTF8);

                    using (var driverDlg = new Form())
                    {
                        driverDlg.Text = "اختر المستخدم للاستيراد";
                        driverDlg.Size = new Size(350, 180);
                        driverDlg.StartPosition = FormStartPosition.CenterParent;
                        driverDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                        driverDlg.MaximizeBox = false;
                        driverDlg.MinimizeBox = false;
                        driverDlg.RightToLeft = RightToLeft.Yes;
                        driverDlg.BackColor = Theme.BgMain;

                        var lbl = new Label { Text = "اختر المندوب/المحاسب المنسوب له هذا الاستيراد:", Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.TextMain };
                        var cbo = new ComboBox { Location = new Point(20, 45), Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

                        // إضافة خيار افتراضي للمحاسب
                        cbo.Items.Add(new ComboItem(-99, "المحاسب (حجوزات/طلبات)"));

                        var dt = EmployeeDAL.GetDrivers();
                        foreach (DataRow r in dt.Rows)
                            cbo.Items.Add(new ComboItem((int)r["EmpID"], r["EmpName"].ToString()));
                        cbo.DisplayMember = "Text";
                        cbo.SelectedIndex = 0;

                        var btnOk = Theme.MakeButton("📥 بدء الاستيراد", 180, 90, 130, 32, Theme.Accent);
                        btnOk.Click += (senderDlg, eDlg) => {
                            if (cbo.SelectedItem is ComboItem ci)
                            {
                                driverDlg.DialogResult = DialogResult.OK;
                                driverDlg.Close();

                                var preview = new FrmImportPreview(tempFile, DateTime.Today, ci.ID, ci.Text);
                                preview.FormClosed += (s, ev) => {
                                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                                };
                                NavigateTo(preview);
                            }
                        };

                        driverDlg.Controls.AddRange(new Control[] { lbl, cbo, btnOk });
                        driverDlg.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ أثناء تحميل البيانات من السحاب:\n" + ex.Message, "خطأ الاستيراد السحابي", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool ShowInputDialog(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "موافق";
            buttonCancel.Text = "إلغاء";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;
            
            form.RightToLeft = RightToLeft.Yes;
            form.RightToLeftLayout = true;
            form.Font = Theme.FontMain;
            form.BackColor = Theme.BgMain;
            label.ForeColor = Theme.TextMain;
            textBox.BackColor = Theme.BgInput;
            textBox.ForeColor = Theme.TextMain;
            buttonOk.BackColor = Theme.Accent;
            buttonOk.ForeColor = Color.White;
            buttonCancel.BackColor = Theme.BgCard;
            buttonCancel.ForeColor = Theme.TextMain;

            if (form.ShowDialog() == DialogResult.OK)
            {
                value = textBox.Text;
                return true;
            }
            return false;
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
                int belowMinCount = InventoryDAL.GetBelowMinStockCount();

                pnlCards.Controls.Add(MakeCard("💰 رصيد الخزنة الحالي", cashBal.ToString("N2") + " ج", Theme.Success));
                pnlCards.Controls.Add(MakeCard("🛒 مبيعات اليوم", todaySales.ToString("N2") + " ج", Theme.Accent));
                pnlCards.Controls.Add(MakeCard("🚚 حمولات مفتوحة حالياً", openLoads.Rows.Count + " حمولة", Color.FromArgb(52, 152, 219)));

                var cardBelowMin = MakeCard("🔴 أصناف تحت حد الطلب", belowMinCount + " صنف", Theme.Danger);
                cardBelowMin.Click += (s, e) => NavigateMain(new FrmInventory(true));
                foreach (Control child in cardBelowMin.Controls)
                {
                    child.Click += (s, e) => NavigateMain(new FrmInventory(true));
                    child.Cursor = Cursors.Hand;
                }
                pnlCards.Controls.Add(cardBelowMin);
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
            if (Session.CanAccess("Sales")) AddQuickButton(pnlActions, "🛒 فاتورة مبيعات جديدة", ref btnY, () => NavigateMain(new FrmSale()), Theme.Accent);
            if (Session.CanAccess("Vehicles")) AddQuickButton(pnlActions, "🚗 المركبات والتحميل", ref btnY, () => NavigateMain(new FrmVehicles()), Color.FromArgb(55, 135, 195));
            if (Session.CanAccess("DriverHandover")) AddQuickButton(pnlActions, "🚚 تسليم حمولة مندوب", ref btnY, () => NavigateMain(new FrmDriverHandover()), Theme.Primary);
            if (Session.CanAccess("CashBox")) AddQuickButton(pnlActions, "💰 تحصيل نقدي للخزنة", ref btnY, () => NavigateMain(new FrmCashBox()), Theme.Success);
            if (Session.CanAccess("Inventory")) AddQuickButton(pnlActions, "📦 جرد المخزن والأصناف", ref btnY, () => NavigateMain(new FrmInventory()), Color.FromArgb(120, 120, 80));
            if (Session.CanAccess("Clients")) AddQuickButton(pnlActions, "👥 كشف حساب العملاء", ref btnY, () => NavigateMain(new FrmClients()), Color.FromArgb(100, 100, 150));
            if (Session.CanAccess("Reports")) AddQuickButton(pnlActions, "📊 التقارير والإحصائيات", ref btnY, () => NavigateMain(new FrmReports()), Color.FromArgb(150, 100, 100));

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



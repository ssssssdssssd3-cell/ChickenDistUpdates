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
        private Label lblUserInfo, lblCompany, lblTitle;
        private Form _currentChild;
        private Button _activeGroupBtn;
        private Timer tmrPeriodicBackup;

        public FrmMain()
        {
            InitializeComponent();
            NavigateTo(new FrmDashboard());
            InitializePeriodicBackup();
        }

        private void InitializeComponent()
        {
            this.Text = AppConfig.CompanyName + " - النظام الرئيسي | الإصدار: " + UpdateManager.CurrentVersion;
            this.Size = new Size(1280, 780);
            this.MinimumSize = new Size(1024, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Theme.BgMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Icon extract failed: " + ex.Message); }

            // ===== pnlTopBar =====
            this.pnlTopBar = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 65, 
                BackColor = Theme.BgHeader 
            };

            this.lblCompany = new Label
            {
                Text = "🐣 " + AppConfig.CompanyName,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Width = 250,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var pnlProfile = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                Padding = new Padding(10, 16, 10, 16),
                BackColor = Color.Transparent
            };

            var btnLogoutTop = new Button
            {
                Text = "خروج ↩",
                Width = 85,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Danger,
                ForeColor = Color.White,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnLogoutTop.FlatAppearance.BorderSize = 0;
            btnLogoutTop.Click += (s, e) => 
            { 
                if (MessageBox.Show("هل تريد تسجيل الخروج؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) 
                { 
                    Session.Clear(); 
                    this.Close(); 
                } 
            };

            this.lblUserInfo = new Label
            {
                Text = $"👤 {Session.EmpName}  |  💼 {Session.Role}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 190, 210),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            pnlProfile.Controls.Add(lblUserInfo);
            pnlProfile.Controls.Add(btnLogoutTop);

            this.lblTitle = new Label
            {
                Text = "لوحة التحكم الرئيسية",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.pnlTopBar.Controls.Add(this.lblCompany);
            this.pnlTopBar.Controls.Add(pnlProfile);
            this.pnlTopBar.Controls.Add(this.lblTitle);

            this.lblCompany.SendToBack();
            pnlProfile.SendToBack();
            this.lblTitle.BringToFront();

            // ===== pnlNavBar =====
            this.pnlNavBar = new FlowLayoutPanel
            {
                Name = "pnlNavBar",
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgNavBar,
                Padding = new Padding(10, 5, 10, 5),
                AllowDrop = true
            };

            this.pnlNavBar.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(typeof(Button)))
                {
                    e.Effect = DragDropEffects.Move;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };

            this.pnlNavBar.DragOver += (s, e) =>
            {
                e.Effect = DragDropEffects.Move;
                Point clientPoint = this.pnlNavBar.PointToClient(new Point(e.X, e.Y));
                Control dragControl = (Control)e.Data.GetData(typeof(Button));
                if (dragControl != null)
                {
                    Control targetControl = this.pnlNavBar.GetChildAtPoint(clientPoint);
                    if (targetControl != null && targetControl != dragControl)
                    {
                        int targetIndex = this.pnlNavBar.Controls.GetChildIndex(targetControl);
                        this.pnlNavBar.Controls.SetChildIndex(dragControl, targetIndex);
                    }
                }
            };

            // ===== Content Panel =====
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgMain,
                Padding = new Padding(0)
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlNavBar);
            this.Controls.Add(pnlTopBar);

            pnlTopBar.SendToBack(); // Docks first at the top
            pnlNavBar.SendToBack();  // Docks second below top bar
            pnlContent.BringToFront(); // Fills the rest

            this.Resize += (s, e) => UpdateLayout();
            UpdateLayout();
            
            // Build the menu
            BuildNavBar(pnlNavBar);
        }

        private void UpdateLayout()
        {
            // Do not override docking built in InitializeComponent
        }

        public void UpdateCompanyName(string newName)
        {
            lblCompany.Text = $"🐣  {newName}";
            this.Text = $"{newName} - النظام الرئيسي";
        }

        private void HighlightActiveGroup(string className)
        {
            string targetGroup = "";
            switch (className)
            {
                case "FrmDashboard":
                    targetGroup = "الرئيسية";
                    break;
                case "FrmSale":
                case "FrmReturn":
                case "FrmInstallments":
                case "FrmSalesList":
                case "FrmSalesAuditList":
                case "FrmAccountantPortal":
                    targetGroup = "المبيعات";
                    break;
                case "FrmPurchase":
                case "FrmPurchaseReturn":
                case "FrmPurchasesList":
                    targetGroup = "المشتريات";
                    break;
                case "FrmProducts":
                case "FrmCategories":
                case "FrmUnits":
                case "FrmImportProducts":
                case "FrmWarehouses":
                case "FrmInventory":
                case "FrmWastage":
                case "FrmWarehouseTransfer":
                case "FrmWarehouseTransfersList":
                case "FrmPriceChanges":
                case "FrmBulkPrintBarcodes":
                    targetGroup = "المخازن";
                    break;
                case "FrmClients":
                case "FrmInactiveClients":
                case "FrmVehicles":
                    targetGroup = "العملاء";
                    break;
                case "FrmSuppliers":
                case "FrmSupplierStatement":
                case "FrmSupplierPayment":
                case "FrmSupplierAdjustment":
                    targetGroup = "الموردين";
                    break;
                case "FrmDriverHandover":
                case "FrmDriverPortal":
                case "FrmImportPreview":
                case "FrmDriversMonitor":
                case "FrmDriverCustody":
                case "FrmDriverLeaderboard":
                    targetGroup = "المناديب";
                    break;
                case "FrmCashBox":
                case "FrmDailyClosing":
                    targetGroup = "المالية";
                    break;
                case "FrmReports":
                    if (_currentChild is FrmReports rptForm)
                    {
                        if (rptForm.TargetModule == "Sales") targetGroup = "المبيعات";
                        else if (rptForm.TargetModule == "Purchases") targetGroup = "المشتريات";
                        else if (rptForm.TargetModule == "Stores") targetGroup = "المخازن";
                        else if (rptForm.TargetModule == "Clients") targetGroup = "العملاء";
                        else if (rptForm.TargetModule == "Drivers") targetGroup = "المناديب";
                        else if (rptForm.TargetModule == "Financials") targetGroup = "المالية";
                        else targetGroup = "الإدارة";
                    }
                    else
                    {
                        targetGroup = "المالية";
                    }
                    break;
                case "FrmEmployees":
                case "FrmEmployeeTransactions":
                    targetGroup = "الإدارة";
                    break;
            }

            if (_activeGroupBtn != null)
            {
                if (_activeGroupBtn.Tag is Color originalColor)
                {
                    _activeGroupBtn.BackColor = originalColor;
                }
                else
                {
                    _activeGroupBtn.BackColor = Color.Transparent;
                }
                _activeGroupBtn.ForeColor = Color.White;
                _activeGroupBtn.FlatAppearance.BorderSize = 0;
            }

            // Find button in pnlNavBar
            if (pnlNavBar != null)
            {
                foreach (Control ctrl in pnlNavBar.Controls)
                {
                    if (ctrl is Button btn && btn.Name == targetGroup)
                    {
                        _activeGroupBtn = btn;
                        btn.FlatAppearance.BorderSize = 2;
                        btn.FlatAppearance.BorderColor = Theme.Accent; // Gold highlight
                        break;
                    }
                }
            }
        }

        private bool UserCanAccess(string screenList)
        {
            if (string.IsNullOrEmpty(screenList)) return true;
            string[] screens = screenList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var scr in screens)
            {
                if (!Session.CanAccess(scr.Trim())) return false;
            }
            return true;
        }

        private void BuildNavBar(FlowLayoutPanel pnlNavBar)
        {
            pnlNavBar.Controls.Clear();

            var groups = new System.Collections.Generic.List<(string icon, string label, Color color, (string text, string screen, Action action)[] items)>
            {
                ("🏠", "الرئيسية", Color.FromArgb(55, 65, 81), new[] {
                    ("🏠 الرئيسية", "", (Action)(() => NavigateTo(new FrmDashboard())))
                }),

                ("🛒", "المبيعات", Color.FromArgb(5, 122, 85), new[] {
                    ("🛒 نقطة البيع POS", "POS",       (Action)(() => { var f = new FrmPOS(); f.ShowDialog(); })),
                    ("🛒 فاتورة بيع",    "Sales",      (Action)(() => NavigateTo(new FrmSale()))),
                    ("↩ مرتجع بيع",     "Returns",    (Action)(() => NavigateTo(new FrmReturn()))),
                    ("💳 عقود التقسيط", "Installments", (Action)(() => NavigateTo(new FrmInstallments()))),
                    ("📋 سجل المبيعات", "SalesList",   (Action)(() => NavigateTo(new FrmSalesList()))),
                    ("📑 سجل التعديلات","SalesAudit", (Action)(() => NavigateTo(new FrmSalesAuditList()))),
                    ("📡 بوابة المحاسب",  "AccountantPortal", (Action)(() => NavigateTo(new FrmAccountantPortal()))),
                    ("📊 تقارير المبيعات", "Reports,Sales",   (Action)(() => NavigateTo(new FrmReports("Sales")))),
                }),

                ("📥", "المشتريات", Color.FromArgb(120, 53, 15), new[] {
                    ("📥 فاتورة شراء",    "Purchases",      (Action)(() => NavigateTo(new FrmPurchase()))),
                    ("↩ مرتجع شراء",     "PurchaseReturn", (Action)(() => NavigateTo(new FrmPurchaseReturn()))),
                    ("📋 سجل المشتريات", "PurchasesList",  (Action)(() => NavigateTo(new FrmPurchasesList()))),
                    ("📊 تقارير المشتريات", "Reports,Purchases",      (Action)(() => NavigateTo(new FrmReports("Purchases")))),
                }),

                ("📦", "المخازن", Color.FromArgb(17, 94, 89), new[] {
                    ("📦 الأصناف",          "Products",          (Action)(() => NavigateTo(new FrmProducts()))),
                    ("🏢 التصنيفات والأقسام", "Categories",        (Action)(() => NavigateTo(new FrmCategories()))),
                    ("📏 إدارة الوحدات",      "Units",             (Action)(() => NavigateTo(new FrmUnits()))),
                    ("📥 استيراد الأصناف",   "ImportProducts",    (Action)(() => NavigateTo(new FrmImportProducts()))),
                    ("🏢 المخازن",          "Warehouses",        (Action)(() => NavigateTo(new FrmWarehouses()))),
                    ("⚖️ جرد المخزن",      "Inventory",         (Action)(() => NavigateTo(new FrmInventory()))),
                    ("🗑️ الهوالك والتالف",  "Wastage",           (Action)(() => NavigateTo(new FrmWastage()))),
                    ("🔄 تحويل مخزني",     "WarehouseTransfer", (Action)(() => NavigateTo(new FrmWarehouseTransfer()))),
                    ("📋 سجل التحويلات",   "WarehouseTransfersList",(Action)(() => NavigateTo(new FrmWarehouseTransfersList()))),
                    ("📊 سجل تغير الأسعار", "PriceChanges",      (Action)(() => NavigateTo(new FrmPriceChanges()))),
                    ("🏷️ طباعة الباركود (مجمع)", "BulkPrintBarcodes", (Action)(() => NavigateTo(new FrmBulkPrintBarcodes()))),
                    ("📊 تقارير المخازن",   "Reports,Products",           (Action)(() => NavigateTo(new FrmReports("Stores")))),
                }),

                ("👥", "العملاء", Color.FromArgb(30, 64, 175), new[] {
                    ("👥 العملاء",   "Clients",   (Action)(() => NavigateTo(new FrmClients()))),
                    ("📢 العملاء الرواكد", "InactiveClients", (Action)(() => NavigateTo(new FrmInactiveClients()))),
                    ("🚗 المركبات",  "Vehicles",  (Action)(() => NavigateTo(new FrmVehicles()))),
                    ("📊 تقارير العملاء", "Reports,Clients",   (Action)(() => NavigateTo(new FrmReports("Clients")))),
                }),

                ("🤝", "الموردين", Color.FromArgb(194, 120, 3), new[] {
                    ("🤝 إدارة الموردين", "Suppliers", (Action)(() => NavigateTo(new FrmSuppliers()))),
                    ("📊 كشف حساب مورد", "SupplierStatement", (Action)(() => OpenSupplierStatementSelector())),
                    ("💸 صرف نقدي لمورد", "SupplierPayment", (Action)(() => OpenSupplierPaymentSelector())),
                    ("⚖️ تسوية أرصدة الموردين", "SupplierAdjustment", (Action)(() => OpenSupplierAdjustmentSelector())),
                }),

                ("🚚", "المناديب", Color.FromArgb(109, 40, 217), new[] {
                    ("🚚 حمولة مندوب",      "DriverHandover", (Action)(() => NavigateTo(new FrmDriverHandover()))),
                    ("📡 بوابة المندوب",    "DriverSales",    (Action)(() => NavigateTo(new FrmDriverPortal()))),
                    ("☁️ استيراد من السحاب", "ImportPreview",  (Action)(() => OpenCloudImportDialog())),
                    ("🖥️ مراقبة المناديب", "DriversMonitor", (Action)(() => NavigateTo(new FrmDriversMonitor()))),
                    ("📋 عهدة المناديب",   "DriverCustody",  (Action)(() => NavigateTo(new FrmDriverCustody()))),
                    ("🏆 أداء المناديب",   "DriverLeaderboard", (Action)(() => NavigateTo(new FrmDriverLeaderboard()))),
                    ("📊 تقارير المناديب", "Reports,DriverHandover",         (Action)(() => NavigateTo(new FrmReports("Drivers")))),
                }),

                ("💰", "المالية", Color.FromArgb(159, 18, 57), new[] {
                    ("💰 الخزنة",       "CashBox",      (Action)(() => NavigateTo(new FrmCashBox()))),
                    ("🔄 إدارة الوردية", "ShiftClose",  (Action)(() => { var f = new FrmShiftClose(); f.ShowDialog(); })),
                    ("📊 التقارير المالية", "Reports",   (Action)(() => NavigateTo(new FrmReports("Financials")))),
                    ("📑 تقفيل يومية", "DailyClosing", (Action)(() => NavigateTo(new FrmDailyClosing()))),
                }),

                ("⚙️", "الإدارة", Color.FromArgb(55, 65, 81), new[] {
                    ("👔 الموظفين",          "Employees",            (Action)(() => NavigateTo(new FrmEmployees()))),
                    ("💰 حسابات الموظفين",  "EmployeeTransactions", (Action)(() => NavigateTo(new FrmEmployeeTransactions()))),
                    ("⚙️ الإعدادات",        "Settings",             (Action)(() => new FrmSettings().ShowDialog())),
                    ("🤖 إدارة بوت الواتساب", "BotManager",           (Action)(() => new FrmBotManager().ShowDialog())),
                    ("📊 التقارير الشاملة", "Reports",              (Action)(() => NavigateTo(new FrmReports(null)))),
                    ("🔄 تحديث البرنامج",   "",                     (Action)(() => UpdateManager.CheckForUpdates(true))),
                }),
            };

            if (AppConfig.BusinessType == "Mobiles")
            {
                groups.Insert(groups.Count - 1, ("🔧", "الصيانة", Color.FromArgb(13, 148, 136), new[] {
                    ("🔧 تذاكر الصيانة", "", (Action)(() => NavigateTo(new FrmMaintenance()))),
                }));
            }

            foreach (var group in groups)
            {
                // Check permissions
                bool hasAnyAccess = false;
                foreach (var item in group.items)
                {
                    if (UserCanAccess(item.screen))
                    { 
                        hasAnyAccess = true; 
                        break; 
                    }
                }
                if (!hasAnyAccess) continue;

                // Build context menu dropdown
                var menu = new ContextMenuStrip();
                menu.BackColor  = Theme.BgCard;
                menu.ForeColor  = Theme.TextMain;
                menu.Font       = new Font("Segoe UI", 9.5f);
                menu.ShowImageMargin = false;
                menu.Renderer   = new ToolStripProfessionalRenderer(new MenuColorTable());

                foreach (var item in group.items)
                {
                    if (!UserCanAccess(item.screen)) continue;
                    
                    var menuItem = new ToolStripMenuItem(item.text)
                    {
                        ForeColor = Theme.TextMain,
                        BackColor = Theme.BgCard,
                        Padding   = new Padding(8, 6, 8, 6)
                    };
                    var act = item.action;
                    menuItem.Click += (s, e) => act();
                    menu.Items.Add(menuItem);
                }

                // Main navigation button
                var btn = new Button
                {
                    Name      = group.label,
                    Text      = group.label == "الرئيسية" ? "🏠\nالرئيسية" : $"{group.icon}\n{group.label} ▾",
                    Size      = new Size(108, 54),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = group.color,
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin    = new Padding(3, 4, 3, 4),
                    Cursor    = Cursors.Hand,
                    Tag       = group.color, // Store original color for highlighting
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(group.color, 0.3f);
                btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(group.color, 0.1f);

                if (group.label == "الرئيسية")
                {
                    btn.Click += (s, e) => NavigateTo(new FrmDashboard());
                }
                else
                {
                    btn.Click += (s, e) =>
                    {
                        menu.Show(btn, new System.Drawing.Point(0, btn.Height));
                    };
                }

                // Drag and Drop support
                Point dragStart = Point.Empty;
                btn.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        dragStart = e.Location;
                    }
                };
                btn.MouseMove += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left && dragStart != Point.Empty)
                    {
                        int dx = e.X - dragStart.X;
                        int dy = e.Y - dragStart.Y;
                        if (Math.Sqrt(dx * dx + dy * dy) > 4)
                        {
                            btn.DoDragDrop(btn, DragDropEffects.Move);
                            dragStart = Point.Empty;
                        }
                    }
                };
                btn.MouseUp += (s, e) =>
                {
                    dragStart = Point.Empty;
                };

                pnlNavBar.Controls.Add(btn);
            }

            // Add Technical Support Bot button
            var btnBot = new Button
            {
                Text = "🤖\nمساعد الدعم",
                Size      = new Size(108, 54),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(147, 51, 234), // Purple
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin    = new Padding(3, 4, 3, 4),
                Cursor    = Cursors.Hand
            };
            btnBot.FlatAppearance.BorderSize = 0;
            btnBot.FlatAppearance.MouseOverBackColor = ControlPaint.Light(btnBot.BackColor, 0.3f);
            btnBot.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(btnBot.BackColor, 0.1f);
            btnBot.Click += (s, e) => new FrmSupportBot().ShowDialog();
            pnlNavBar.Controls.Add(btnBot);
        }

        private class MenuColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected         => Theme.BgLight;
            public override Color MenuItemBorder           => Theme.BorderColor;
            public override Color MenuBorder               => Theme.BorderColor;
            public override Color ToolStripDropDownBackground => Theme.BgCard;
            public override Color ImageMarginGradientBegin => Theme.BgCard;
            public override Color ImageMarginGradientMiddle => Theme.BgCard;
            public override Color ImageMarginGradientEnd   => Theme.BgCard;
        }

        public void NavigateTo(Form form)
        {
            if (_currentChild != null && !_currentChild.IsDisposed)
            {
                _currentChild.Close();
                if (!_currentChild.IsDisposed)
                {
                    // The child form cancelled closing (e.g. user chose DialogResult.No on dirty invoice)
                    return;
                }
            }

            _currentChild = form;
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.FormBorderStyle = FormBorderStyle.None;
            form.RightToLeft = RightToLeft.Yes;

            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(form);
            form.Show();
            form.BringToFront();

            if (lblTitle != null)
            {
                lblTitle.Text = form.Text;
            }

            HighlightActiveGroup(form.GetType().Name);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // إيقاف خادم المندوب بأمان عند إغلاق البرنامج
            Core.DriverPortalServer.Stop();
            // النسخ الاحتياطي التلقائي عند الخروج
            BackupManager.AutoBackupOnExit();
            base.OnFormClosed(e);
        }

        private void OpenSupplierStatementSelector()
        {
            try
            {
                DataTable dt = SupplierDAL.GetAll(true);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("لا يوجد موردين مسجلين حالياً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var dlg = new Form())
                {
                    dlg.Text = "📊 اختر المورد لعرض كشف الحساب";
                    dlg.Size = new Size(350, 180);
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                    dlg.RightToLeft = RightToLeft.Yes;
                    dlg.BackColor = Theme.BgMain;
                    dlg.Font = Theme.FontMain;

                    var lbl = new Label { Text = "اختر المورد:", Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.TextMain };
                    var cbo = new ComboBox { Location = new Point(20, 45), Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

                    foreach (DataRow r in dt.Rows)
                        cbo.Items.Add(new ComboItem((int)r["SupplierID"], r["SupplierName"].ToString()));
                    cbo.DisplayMember = "Text";
                    cbo.SelectedIndex = 0;

                    var btnOk = Theme.MakeButton("🔍 عرض الكشف", 180, 90, 130, 32, Theme.Accent);
                    btnOk.Click += (senderDlg, eDlg) => {
                        if (cbo.SelectedItem is ComboItem ci)
                        {
                            dlg.DialogResult = DialogResult.OK;
                            dlg.Close();
                            new FrmSupplierStatement(ci.ID, ci.Text).ShowDialog(this);
                        }
                    };

                    dlg.Controls.AddRange(new Control[] { lbl, cbo, btnOk });
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ أثناء فتح شاشة كشف حساب المورد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSupplierPaymentSelector()
        {
            try
            {
                DataTable dt = SupplierDAL.GetAll(true);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("لا يوجد موردين مسجلين حالياً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var dlg = new Form())
                {
                    dlg.Text = "💸 صرف نقدي للمورد";
                    dlg.Size = new Size(350, 180);
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                    dlg.RightToLeft = RightToLeft.Yes;
                    dlg.BackColor = Theme.BgMain;
                    dlg.Font = Theme.FontMain;

                    var lbl = new Label { Text = "اختر المورد:", Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.TextMain };
                    var cbo = new ComboBox { Location = new Point(20, 45), Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

                    foreach (DataRow r in dt.Rows)
                        cbo.Items.Add(new ComboItem((int)r["SupplierID"], r["SupplierName"].ToString()));
                    cbo.DisplayMember = "Text";
                    cbo.SelectedIndex = 0;

                    var btnOk = Theme.MakeButton("💸 صرف نقدي", 180, 90, 130, 32, Theme.Accent);
                    btnOk.Click += (senderDlg, eDlg) => {
                        if (cbo.SelectedItem is ComboItem ci)
                        {
                            dlg.DialogResult = DialogResult.OK;
                            dlg.Close();
                            ShowSupplierPaymentDialog(ci.ID, ci.Text);
                        }
                    };

                    dlg.Controls.AddRange(new Control[] { lbl, cbo, btnOk });
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSupplierPaymentDialog(int supplierID, string supplierName)
        {
            try
            {
                decimal currentBalance = 0;
                DataTable dt = SupplierDAL.GetAll();
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["SupplierID"]) == supplierID)
                    {
                        currentBalance = Convert.ToDecimal(r["Balance"]);
                        break;
                    }
                }

                var dlg = new Form
                {
                    Text = "💸 صرف نقدي للمورد - " + supplierName,
                    Size = new Size(420, 300),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false, MinimizeBox = false,
                    RightToLeft = RightToLeft.Yes,
                    RightToLeftLayout = true,
                    BackColor = Theme.BgMain,
                    Font = Theme.FontMain
                };

                int dy = 18;
                dlg.Controls.Add(new Label
                {
                    Text = "المورد: " + supplierName,
                    Location = new Point(10, dy), Width = 380,
                    ForeColor = Theme.TextMain,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                }); dy += 30;

                dlg.Controls.Add(new Label
                {
                    Text = $"الرصيد الحالي: {currentBalance:N2} ج",
                    Location = new Point(10, dy), Width = 380,
                    ForeColor = Theme.Accent,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                }); dy += 36;

                dlg.Controls.Add(new Label { Text = "المبلغ المصروف (ج):", Location = new Point(200, dy + 5), Width = 180, ForeColor = Theme.TextMain });
                var nudAmt = new NumericUpDown
                {
                    Location = new Point(10, dy), Width = 185,
                    Minimum = 0.01m, Maximum = 9999999, DecimalPlaces = 2,
                    BackColor = Theme.BgInput, ForeColor = Theme.TextMain
                };
                dlg.Controls.Add(nudAmt); dy += 40;

                dlg.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(200, dy + 5), Width = 180, ForeColor = Theme.TextMain });
                var txtNote = new TextBox
                {
                    Location = new Point(10, dy), Width = 185,
                    BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                    Text = "سداد جزء من المديونية"
                };
                dlg.Controls.Add(txtNote); dy += 40;

                var btnOk     = Theme.MakeButton("✅ تأكيد الصرف", 210, dy, 175, 38, Color.FromArgb(140, 80, 0));
                var btnCancel = Theme.MakeButton("❌ إلغاء",        10,  dy, 120, 38, Color.FromArgb(100, 40, 40));
                btnOk.Font    = new Font("Segoe UI", 10, FontStyle.Bold);

                btnOk.Click += (s2, e2) =>
                {
                    if (nudAmt.Value <= 0) { MessageBox.Show("أدخل مبلغاً أكبر من صفر."); return; }
                    try
                    {
                        string code = SupplierDAL.AddSupplierPayment(supplierID, nudAmt.Value, txtNote.Text.Trim());
                        MessageBox.Show(
                            $"✅ تم الصرف بنجاح!\n\nكود القيد: {code}\nالمبلغ: {nudAmt.Value:N2} ج\nالمورد: {supplierName}",
                            "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch { }
                };
                btnCancel.Click += (s2, e2) => dlg.Close();

                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSupplierAdjustmentSelector()
        {
            try
            {
                DataTable dt = SupplierDAL.GetAll(true);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("لا يوجد موردين مسجلين حالياً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var dlg = new Form())
                {
                    dlg.Text = "⚖️ تسوية أرصدة الموردين";
                    dlg.Size = new Size(350, 180);
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                    dlg.RightToLeft = RightToLeft.Yes;
                    dlg.BackColor = Theme.BgMain;
                    dlg.Font = Theme.FontMain;

                    var lbl = new Label { Text = "اختر المورد:", Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.TextMain };
                    var cbo = new ComboBox { Location = new Point(20, 45), Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

                    foreach (DataRow r in dt.Rows)
                        cbo.Items.Add(new ComboItem((int)r["SupplierID"], r["SupplierName"].ToString()));
                    cbo.DisplayMember = "Text";
                    cbo.SelectedIndex = 0;

                    var btnOk = Theme.MakeButton("⚖️ تسوية الرصيد", 180, 90, 130, 32, Theme.Accent);
                    btnOk.Click += (senderDlg, eDlg) => {
                        if (cbo.SelectedItem is ComboItem ci)
                        {
                            dlg.DialogResult = DialogResult.OK;
                            dlg.Close();
                            new FrmAdjustment(ci.ID, ci.Text, false).ShowDialog(this);
                        }
                    };

                    dlg.Controls.AddRange(new Control[] { lbl, cbo, btnOk });
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void InitializePeriodicBackup()
        {
            try
            {
                int intervalHours = AppConfig.BackupIntervalHours;
                if (intervalHours > 0)
                {
                    // Run immediate check in a separate task/thread to keep startup fast
                    System.Threading.Tasks.Task.Run(() => CheckAndRunPeriodicBackup(true));

                    tmrPeriodicBackup = new Timer();
                    tmrPeriodicBackup.Interval = 5 * 60 * 1000; // Check every 5 minutes
                    tmrPeriodicBackup.Tick += (s, e) => CheckAndRunPeriodicBackup(false);
                    tmrPeriodicBackup.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Init periodic backup failed: " + ex.Message);
            }
        }

        private void CheckAndRunPeriodicBackup(bool isStartup)
        {
            try
            {
                int intervalHours = AppConfig.BackupIntervalHours;
                if (intervalHours <= 0) return;

                string folder = BackupManager.BackupFolder;
                if (string.IsNullOrWhiteSpace(folder) || !System.IO.Directory.Exists(folder))
                {
                    if (isStartup)
                    {
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            MessageBox.Show(
                                "⚠️ تنبيه: النسخ الاحتياطي الدوري مفعل ولكن مجلد النسخ الاحتياطي غير موجود أو غير صالح.\nيرجى تحديد مسار مجلد صحيح من شاشة الإعدادات.",
                                "تنبيه النسخ الاحتياطي",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning,
                                MessageBoxDefaultButton.Button1,
                                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                        }));
                    }
                    return;
                }

                var last = BackupManager.LastBackupTime;
                if (last == null || (DateTime.Now - last.Value).TotalHours >= intervalHours)
                {
                    // Run backup
                    bool success = BackupManager.DoBackup(silent: true);
                    if (success && isStartup)
                    {
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            MessageBox.Show(
                                "✅ تم عمل نسخة احتياطية دورية تلقائية لقاعدة البيانات بنجاح عند تشغيل النظام.",
                                "النسخ الاحتياطي التلقائي",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information,
                                MessageBoxDefaultButton.Button1,
                                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CheckAndRunPeriodicBackup failed", ex, "PeriodicBackup");
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
                if (Session.CanAccess("DashTreasury"))
                {
                    decimal cashBal = AccountDAL.GetCashBalance();
                    pnlCards.Controls.Add(MakeCard("💰 رصيد الخزنة الحالي", cashBal.ToString("N2") + " ج", Theme.Success));
                }

                if (Session.CanAccess("DashSales"))
                {
                    var salesDt = ReportDAL.SalesByDay(DateTime.Today, DateTime.Today);
                    decimal todaySales = salesDt.Rows.Count > 0 ? Convert.ToDecimal(salesDt.Rows[0]["Total"]) : 0;
                    pnlCards.Controls.Add(MakeCard("🛒 مبيعات اليوم", todaySales.ToString("N2") + " ج", Theme.Accent));
                }

                if (Session.CanAccess("DashLoads"))
                {
                    var openLoads = DriverDAL.GetOpenLoads();
                    pnlCards.Controls.Add(MakeCard("🚚 حمولات مفتوحة حالياً", openLoads.Rows.Count + " حمولة", Color.FromArgb(52, 152, 219)));
                }

                if (Session.CanAccess("DashBelowMin"))
                {
                    int belowMinCount = InventoryDAL.GetBelowMinStockCount();
                    var cardBelowMin = MakeCard("🔴 أصناف تحت حد الطلب", belowMinCount + " صنف", Theme.Danger);
                    cardBelowMin.Click += (s, e) => NavigateMain(new FrmInventory(true));
                    foreach (Control child in cardBelowMin.Controls)
                    {
                        child.Click += (s, e) => NavigateMain(new FrmInventory(true));
                        child.Cursor = Cursors.Hand;
                    }
                    pnlCards.Controls.Add(cardBelowMin);
                }
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
            AddQuickButton(pnlActions, "🤖 مساعد الدعم الفني", ref btnY, () => new FrmSupportBot().ShowDialog(), Color.FromArgb(160, 80, 180));

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



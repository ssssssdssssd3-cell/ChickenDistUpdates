using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmMain : Form
    {
        private Panel pnlTopBar, pnlContent;
        private FlowLayoutPanel pnlNavBar;
        private Panel pnlTabBar;
        private Button _btnOpenPages;
        private ToolStripDropDown _pnlDropdown;
        private FlowLayoutPanel pnlHeaderRight;
        private Label lblUserInfo, lblCompany, lblTitle;
        private Form _currentChild;
        private Button _activeGroupBtn;
        private Timer tmrPeriodicBackup;
        private System.Collections.Generic.List<(Form form, Button tab)> _openTabs
            = new System.Collections.Generic.List<(Form, Button)>();

        public FrmMain()
        {
            InitializeComponent();
            NavigateTo(new FrmDashboard());
            InitializePeriodicBackup();
            try { ChickenDist.Services.CloudSyncService.StartAutoBackgroundSync(); } catch {}
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

            Image rawLogo = Theme.GetCompanyLogo();
            this.lblCompany = new Label
            {
                Text = AppConfig.CompanyName,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 198, 35), // Sleek Golden color
                Width = 260,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                ImageAlign = ContentAlignment.MiddleLeft,
                Image = rawLogo != null ? new Bitmap(rawLogo, new Size(40, 40)) : null,
                Padding = new Padding(10, 0, 10, 0)
            };

            var pnlProfile = new Panel
            {
                Dock = DockStyle.Left,
                Width = 420,
                Padding = new Padding(10, 16, 10, 16),
                BackColor = Color.Transparent
            };


            var btnHelpTop = new Button
            {
                Text = "🤖 الدعم الفني",
                Width = 110,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(160, 80, 180),
                ForeColor = Color.White,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnHelpTop.FlatAppearance.BorderSize = 0;
            btnHelpTop.Click += (s, e) => new FrmSupportBot().ShowDialog();

            var btnMobileSync = new Button
            {
                Text = "📱 ربط الموبايل",
                Width = 120,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 140, 220),
                ForeColor = Color.White,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 0)
            };
            btnMobileSync.FlatAppearance.BorderSize = 0;
            btnMobileSync.Click += (s, e) => NavigateTo(new FrmCloudSync());

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
            pnlProfile.Controls.Add(btnMobileSync);
            pnlProfile.Controls.Add(btnHelpTop);

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
                FlowDirection = FlowDirection.RightToLeft, // RTL: أزرار تبدأ من اليمين
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                RightToLeft = RightToLeft.No, // منع عكس النص داخل الأزرار - فقط الترتيب من اليمين
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

            // ===== Tab Bar — Full Screen Navigation Header =====
            pnlTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(28, 28, 35),
                Padding = new Padding(6, 5, 6, 5)
            };

            // Left panel for close button
            var pnlHeaderLeft = new Panel
            {
                Dock = DockStyle.Left, // Close button on visual Left
                Width = 160,
                BackColor = Color.Transparent
            };
            pnlTabBar.Controls.Add(pnlHeaderLeft);

            // Close button to go back to home screen
            var btnCloseCurrent = new Button
            {
                Text = "✕ إغلاق والعودة للرئيسية",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnCloseCurrent.FlatAppearance.BorderSize = 0;
            btnCloseCurrent.Click += (s, e) => {
                if (_currentChild != null && !(_currentChild is FrmDashboard))
                {
                    var entry = _openTabs.Find(t => t.form == _currentChild);
                    CloseTab(_currentChild, entry.tab);
                }
            };
            pnlHeaderLeft.Controls.Add(btnCloseCurrent);

            // Right panel for Open screens dropdown and Quick-access icons
            pnlHeaderRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight, // LeftToRight under RTL starts 1st added control (الرئيسية) on visual Far Right
                WrapContents = false,
                AutoScroll = false,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = Padding.Empty
            };
            pnlTabBar.Controls.Add(pnlHeaderRight);

            // Open Pages Button
            _btnOpenPages = new Button
            {
                Text = "📑 الشاشات المفتوحة (0) ▾",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 58),
                ForeColor = Color.FromArgb(210, 210, 225),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(160, 32),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(4, 0, 4, 0)
            };
            _btnOpenPages.FlatAppearance.BorderSize = 1;
            _btnOpenPages.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 90);
            _btnOpenPages.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 78);
            _btnOpenPages.Click += BtnOpenPages_Click;

            // Build navigation icons inside the top bar
            BuildTopNavBar(pnlHeaderRight);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTabBar);
            this.Controls.Add(pnlNavBar);
            this.pnlTopBar.Visible = false;
            this.Controls.Add(pnlTopBar);

            pnlTopBar.SendToBack();
            pnlNavBar.SendToBack();
            pnlTabBar.SendToBack();
            pnlContent.BringToFront();

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
                case "FrmReservations":
                case "FrmClearanceOffers":
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
                case "FrmMinStockEdit":
                case "FrmShortageNotebook":
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
                case "FrmMaintenance":
                case "FrmMaintenanceCard":
                    targetGroup = "الصيانة";
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

                ("🛒", "المبيعات", Color.FromArgb(5, 122, 85), AppConfig.IsRestaurant ? new[] {
                    ("🛒 نقطة البيع POS", "POS",       (Action)(() => { var f = new FrmPOS(); f.ShowDialog(); })),
                    ("🛒 فاتورة بيع",    "Sales",      (Action)(() => NavigateTo(new FrmSale()))),
                    ("📋 بيان تسعير / عرض سعر", "Sales", (Action)(() => NavigateTo(new FrmPriceQuote()))),
                    ("📋 حجوزات العملاء", "Reservations", (Action)(() => NavigateTo(new FrmReservations()))),
                    ("🏷️ الأوكازيون والعروض", "ClearanceOffers", (Action)(() => NavigateTo(new FrmClearanceOffers()))),
                    ("↩ مرتجع بيع",     "Returns",    (Action)(() => NavigateTo(new FrmReturn()))),
                    ("💳 عقود التقسيط", "Installments", (Action)(() => NavigateTo(new FrmInstallments()))),
                    ("📋 سجل المبيعات", "SalesList",   (Action)(() => NavigateTo(new FrmSalesList()))),
                    ("📑 سجل التعديلات","SalesAudit", (Action)(() => NavigateTo(new FrmSalesAuditList()))),
                    ("📡 بوابة المحاسب",  "AccountantPortal", (Action)(() => NavigateTo(new FrmAccountantPortal()))),
                    ("📊 تقارير المبيعات", "Reports,Sales",   (Action)(() => NavigateTo(new FrmReports("Sales")))),
                } : new[] {
                    ("🛒 نقطة البيع POS", "POS",       (Action)(() => { var f = new FrmPOS(); f.ShowDialog(); })),
                    ("🛒 فاتورة بيع",    "Sales",      (Action)(() => NavigateTo(new FrmSale()))),
                    ("📋 بيان تسعير / عرض سعر", "Sales", (Action)(() => NavigateTo(new FrmPriceQuote()))),
                    ("📋 حجوزات العملاء", "Reservations", (Action)(() => NavigateTo(new FrmReservations()))),
                    ("🏷️ الأوكازيون والعروض", "ClearanceOffers", (Action)(() => NavigateTo(new FrmClearanceOffers()))),
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
                    ("📥 استيراد الأصناف",   "ImportProducts",    (Action)(() => { if (FrmProducts.PromptImportPassword(this)) NavigateTo(new FrmImportProducts()); })),
                    ("🏢 المخازن",          "Warehouses",        (Action)(() => NavigateTo(new FrmWarehouses()))),
                    ("⚖️ جرد وتعديل الأسعار",      "Inventory",         (Action)(() => NavigateTo(new FrmInventory()))),
                    ("🎯 تعديل حد طلب الأصناف", "MinStockEdit",     (Action)(() => NavigateTo(new FrmMinStockEdit()))),
                    ("📓 كشكول النواقص والطلبات", "ShortageNotebook", (Action)(() => NavigateTo(new FrmShortageNotebook()))),
                    ("📊 تقرير فروق وعجز الجرد الشامل", "InventoryVarianceReport", (Action)(() => new FrmInventoryVarianceReport().ShowDialog())),
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
                    ("🤝 إدارة الموردين",        "Suppliers",          (Action)(() => NavigateTo(new FrmSuppliers()))),
                    ("📊 كشف حساب مورد",     "SupplierStatement",  (Action)(() => OpenSupplierStatementSelector())),
                    ("💸 صرف نقدي لمورد",     "SupplierPayment",    (Action)(() => OpenSupplierPaymentSelector())),
                    ("⚖️ تسوية أرصدة الموردين", "SupplierAdjustment", (Action)(() => OpenSupplierAdjustmentSelector())),
                    ("📊 تقارير الموردين",      "Reports,Suppliers",  (Action)(() => NavigateTo(new FrmReports("Suppliers")))),
                }),                ("🚚", "المناديب", Color.FromArgb(109, 40, 217), new[] {
                    ("🚚 حمولة مندوب",      "DriverHandover", (Action)(() => NavigateTo(new FrmDriverHandover()))),
                    ("📡 بوابة المندوب",    "DriverSales",    (Action)(() => NavigateTo(new FrmDriverPortal()))),
                    ("☁️ استيراد من السحاب", "ImportPreview",  (Action)(() => OpenCloudImportDialog())),
                    ("🖥️ مراقبة المناديب", "DriversMonitor", (Action)(() => NavigateTo(new FrmDriversMonitor()))),
                    ("📋 عهدة المناديب",   "DriverCustody",  (Action)(() => NavigateTo(new FrmDriverCustody()))),
                    ("🏆 أداء المناديب",   "DriverLeaderboard", (Action)(() => NavigateTo(new FrmDriverLeaderboard()))),
                    ("📊 تقارير المناديب", "Reports,DriverHandover",         (Action)(() => NavigateTo(new FrmReports("Drivers")))),
                }),

                ("💰", "المالية", Color.FromArgb(159, 18, 57), new[] {
                    ("💰 الخزنة والمصروفات", "CashBox", (Action)(() => NavigateTo(new FrmCashBox()))),
                    ("💵 النقدية والأرصدة الفعلية المتاحة", "CashBox", (Action)(() => NavigateTo(new FrmActualBalances()))),
                    ("🔄 إدارة الوردية", "ShiftClose",  (Action)(() => { var f = new FrmShiftClose(); f.ShowDialog(); })),
                    ("📊 سجل وتقارير الورديات", "Reports,ShiftClose", (Action)(() => NavigateTo(new FrmReports("ShiftsHistory")))),
                    ("📊 الموقف المالي للمكان", "Reports", (Action)(() => NavigateTo(new FrmFinancialPosition()))),
                    ("📈 قائمة الدخل والتقارير المالية", "Reports,Financials", (Action)(() => NavigateTo(new FrmReports("Financials")))),
                    ("📑 تقفيل يومية", "DailyClosing", (Action)(() => NavigateTo(new FrmDailyClosing()))),
                }),

                ("⚙️", "الإدارة", Color.FromArgb(55, 65, 81), new[] {
                    ("👔 الموظفين",          "Employees",            (Action)(() => NavigateTo(new FrmEmployees()))),
                    ("💰 حسابات الموظفين",  "EmployeeTransactions", (Action)(() => NavigateTo(new FrmEmployeeTransactions()))),
                    ("⚙️ الإعدادات",        "Settings",             (Action)(() => new FrmSettings().ShowDialog())),
                    ("🔑 تفعيل الترخيص (سيريال العميل)", "",        (Action)(() => new FrmActivation("").ShowDialog())),
                    ("🤖 إدارة بوت الواتساب", "BotManager",           (Action)(() => new FrmBotManager().ShowDialog())),
                    ("🔄 تحديث البرنامج",   "",                     (Action)(() => UpdateManager.CheckForUpdates(true))),
                }),
            };

            if (AppConfig.BusinessType == "Mobiles" || AppConfig.BusinessType == "SpareParts" || AppConfig.BusinessType == "CarService" || AppConfig.BusinessType == "MaintenanceCenter" || AppConfig.BusinessType == "Maintenance")
            {
                groups.Insert(groups.Count - 1, ("🔧", "الصيانة", Color.FromArgb(13, 148, 136), new[] {
                    ("🔧 تذاكر وشاشة الصيانة", "Maintenance", (Action)(() => NavigateTo(new FrmMaintenance()))),
                    ("📋 كرت صيانة جديد",       "Maintenance", (Action)(() => NavigateTo(new FrmMaintenanceCard(0)))),
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

                // Build context menu dropdown with group-specific color theme
                var menu = CreateCategoryMenu(group.icon, group.label, group.color, group.items);

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
        }

        private void BuildTopNavBar(FlowLayoutPanel pnlHeaderRight)
        {
            pnlHeaderRight.Controls.Clear();

            var groups = new System.Collections.Generic.List<(string icon, string label, Color color, (string text, string screen, Action action)[] items)>
            {
                ("🏠", "الرئيسية", Color.FromArgb(55, 65, 81), new[] {
                    ("🏠 الرئيسية", "", (Action)(() => NavigateTo(new FrmDashboard())))
                }),

                ("🛒", "المبيعات", Color.FromArgb(5, 122, 85), new[] {
                    ("🛒 نقطة البيع POS", "POS",       (Action)(() => { var f = new FrmPOS(); f.ShowDialog(); })),
                    ("🛒 فاتورة بيع",    "Sales",      (Action)(() => NavigateTo(new FrmSale()))),
                    ("📋 بيان تسعير / عرض سعر", "Sales", (Action)(() => NavigateTo(new FrmPriceQuote()))),
                    ("📋 حجوزات العملاء", "Reservations", (Action)(() => NavigateTo(new FrmReservations()))),
                    ("🏷️ الأوكازيون والعروض", "ClearanceOffers", (Action)(() => NavigateTo(new FrmClearanceOffers()))),
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
                    ("📥 استيراد الأصناف",   "ImportProducts",    (Action)(() => { if (FrmProducts.PromptImportPassword(this)) NavigateTo(new FrmImportProducts()); })),
                    ("🏢 المخازن",          "Warehouses",        (Action)(() => NavigateTo(new FrmWarehouses()))),
                    ("⚖️ جرد وتعديل الأسعار",      "Inventory",         (Action)(() => NavigateTo(new FrmInventory()))),
                    ("🎯 تعديل حد طلب الأصناف", "MinStockEdit",     (Action)(() => NavigateTo(new FrmMinStockEdit()))),
                    ("📓 كشكول النواقص والطلبات", "ShortageNotebook", (Action)(() => NavigateTo(new FrmShortageNotebook()))),
                    ("📊 تقرير فروق وعجز الجرد الشامل", "InventoryVarianceReport", (Action)(() => new FrmInventoryVarianceReport().ShowDialog())),
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
                    ("🤝 إدارة الموردين",        "Suppliers",          (Action)(() => NavigateTo(new FrmSuppliers()))),
                    ("📊 كشف حساب مورد",     "SupplierStatement",  (Action)(() => OpenSupplierStatementSelector())),
                    ("💸 صرف نقدي لمورد",     "SupplierPayment",    (Action)(() => OpenSupplierPaymentSelector())),
                    ("⚖️ تسوية أرصدة الموردين", "SupplierAdjustment", (Action)(() => OpenSupplierAdjustmentSelector())),
                    ("📊 تقارير الموردين",      "Reports,Suppliers",  (Action)(() => NavigateTo(new FrmReports("Suppliers")))),
                }),                ("🚚", "المناديب", Color.FromArgb(109, 40, 217), new[] {
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
                    ("📊 سجل وتقارير الورديات", "Reports,ShiftClose", (Action)(() => NavigateTo(new FrmReports("ShiftsHistory")))),
                    ("📊 الموقف المالي للمكان", "Reports", (Action)(() => NavigateTo(new FrmFinancialPosition()))),
                    ("📈 قائمة الدخل والتقارير المالية", "Reports,Financials", (Action)(() => NavigateTo(new FrmReports("Financials")))),
                    ("📑 تقفيل يومية", "DailyClosing", (Action)(() => NavigateTo(new FrmDailyClosing()))),
                }),

                ("⚙️", "الإدارة", Color.FromArgb(55, 65, 81), new[] {
                    ("👔 الموظفين",          "Employees",            (Action)(() => NavigateTo(new FrmEmployees()))),
                    ("💰 حسابات الموظفين",  "EmployeeTransactions", (Action)(() => NavigateTo(new FrmEmployeeTransactions()))),
                    ("⚙️ الإعدادات",        "Settings",             (Action)(() => new FrmSettings().ShowDialog())),
                    ("🤖 إدارة بوت الواتساب", "BotManager",           (Action)(() => new FrmBotManager().ShowDialog())),
                    ("🔄 تحديث البرنامج",   "",                     (Action)(() => UpdateManager.CheckForUpdates(true))),
                }),
            };

            if (AppConfig.BusinessType == "Mobiles")
            {
                groups.Insert(groups.Count - 1, ("🔧", "الصيانة", Color.FromArgb(13, 148, 136), new[] {
                    ("🔧 تذاكر الصيانة", "Maintenance", (Action)(() => NavigateTo(new FrmMaintenance()))),
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

                // Build context menu dropdown with group-specific color theme
                var menu = CreateCategoryMenu(group.icon, group.label, group.color, group.items);

                // Add small top navigation button
                var btn = new Button
                {
                    Text      = $"{group.icon} {group.label}",
                    Height    = 32,
                    Width     = 95,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 58),
                    ForeColor = Color.FromArgb(220, 220, 235),
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor    = Cursors.Hand,
                    Margin    = new Padding(3, 0, 3, 0)
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 90);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 78);

                if (group.label == "الرئيسية")
                {
                    btn.Click += (s, e) => NavigateTo(new FrmDashboard());
                }
                else
                {
                    btn.Click += (s, e) => menu.Show(btn, new Point(0, btn.Height));
                }

                pnlHeaderRight.Controls.Add(btn);
            }

            pnlHeaderRight.Controls.Add(_btnOpenPages);
        }

        private ContextMenuStrip CreateCategoryMenu(string icon, string label, Color groupColor, (string text, string screen, Action action)[] items)
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(24, 28, 38);
            menu.ForeColor = Color.White;
            menu.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            menu.ShowImageMargin = false;
            menu.RightToLeft = RightToLeft.Yes;
            menu.Renderer = new CategoryToolStripRenderer(groupColor);

            // Colored Category Header Banner
            var headerItem = new ToolStripMenuItem($"{icon}  قائمة {label}")
            {
                Enabled = false,
                BackColor = groupColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Padding = new Padding(10, 8, 10, 8)
            };
            menu.Items.Add(headerItem);

            foreach (var item in items)
            {
                if (!UserCanAccess(item.screen)) continue;

                var menuItem = new ToolStripMenuItem(item.text)
                {
                    ForeColor = Color.FromArgb(241, 245, 249),
                    BackColor = Color.FromArgb(28, 33, 44),
                    Font = new Font("Segoe UI", 9.8f, FontStyle.Bold),
                    Padding = new Padding(12, 8, 12, 8)
                };
                var act = item.action;
                menuItem.Click += (s, e) => act();
                menu.Items.Add(menuItem);
            }

            return menu;
        }

        private class CustomCategoryMenuColorTable : ProfessionalColorTable
        {
            private readonly Color _categoryColor;

            public CustomCategoryMenuColorTable(Color categoryColor)
            {
                _categoryColor = categoryColor;
            }

            public override Color MenuItemSelected => _categoryColor;
            public override Color MenuItemSelectedGradientBegin => _categoryColor;
            public override Color MenuItemSelectedGradientEnd => _categoryColor;
            public override Color MenuItemPressedGradientBegin => ControlPaint.Dark(_categoryColor, 0.15f);
            public override Color MenuItemPressedGradientEnd => ControlPaint.Dark(_categoryColor, 0.15f);
            public override Color MenuItemBorder => _categoryColor;
            public override Color MenuBorder => _categoryColor;
            public override Color ToolStripDropDownBackground => Color.FromArgb(28, 33, 44);
            public override Color ImageMarginGradientBegin => Color.FromArgb(28, 33, 44);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(28, 33, 44);
            public override Color ImageMarginGradientEnd => Color.FromArgb(28, 33, 44);
        }

        private class CategoryToolStripRenderer : ToolStripProfessionalRenderer
        {
            private readonly Color _categoryColor;

            public CategoryToolStripRenderer(Color categoryColor) : base(new CustomCategoryMenuColorTable(categoryColor))
            {
                _categoryColor = categoryColor;
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Enabled)
                {
                    Rectangle headerRect = new Rectangle(Point.Empty, e.Item.Size);
                    using (var brush = new SolidBrush(_categoryColor))
                    {
                        e.Graphics.FillRectangle(brush, headerRect);
                    }
                    e.Item.ForeColor = Color.White;
                    return;
                }

                if (e.Item.Selected)
                {
                    Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
                    using (var brush = new SolidBrush(_categoryColor))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                    e.Item.ForeColor = Color.White;
                }
                else
                {
                    Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
                    using (var brush = new SolidBrush(Color.FromArgb(28, 33, 44)))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                    e.Item.ForeColor = Color.FromArgb(241, 245, 249);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                Rectangle rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                using (var pen = new Pen(_categoryColor, 2))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        // ─── Dropdown list of Open Screens (Max 6 + More...) ───────────────
        private void BtnOpenPages_Click(object sender, EventArgs e)
        {
            if (_pnlDropdown == null)
            {
                _pnlDropdown = new ToolStripDropDown
                {
                    BackColor = Color.FromArgb(30, 30, 40),
                    Padding = Padding.Empty,
                    Margin = Padding.Empty,
                    DropShadowEnabled = true
                };
            }
            else
            {
                _pnlDropdown.Close();
            }

            _pnlDropdown.Items.Clear();
            int rowH = 38;

            if (_openTabs.Count == 0)
            {
                var lbl = new Label 
                { 
                    Text = "لا توجد شاشات مفتوحة", 
                    ForeColor = Color.Gray, 
                    Font = new Font("Segoe UI", 9f), 
                    AutoSize = false, 
                    Width = 256, 
                    Height = 34, 
                    TextAlign = ContentAlignment.MiddleCenter 
                };
                var host = new ToolStripControlHost(lbl) { Padding = Padding.Empty, Margin = Padding.Empty };
                _pnlDropdown.Items.Add(host);
                _pnlDropdown.Show(_btnOpenPages, new Point(0, _btnOpenPages.Height));
                return;
            }

            int countToShow = Math.Min(_openTabs.Count, 6);
            
            // Container flow layout panel to host inside ToolStripDropDown
            var container = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = 260,
                Height = ((countToShow + (_openTabs.Count > 6 ? 1 : 0)) * rowH) + 8,
                Padding = new Padding(1, 4, 1, 4),
                Margin = Padding.Empty,
                BackColor = Color.FromArgb(30, 30, 40)
            };

            for (int i = 0; i < countToShow; i++)
            {
                var entry = _openTabs[i];
                var form = entry.form;
                var tab = entry.tab;
                bool isActive = form == _currentChild;

                var row = new Panel
                {
                    Width = 256,
                    Height = rowH - 2,
                    BackColor = isActive ? Color.FromArgb(5, 110, 75) : Color.FromArgb(40, 40, 52),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 1, 0, 1)
                };

                var lblName = new Label
                {
                    Text = TrimTabTitle(form.Text),
                    ForeColor = isActive ? Color.White : Color.FromArgb(210, 210, 225),
                    Font = new Font("Segoe UI", 9.5f, isActive ? FontStyle.Bold : FontStyle.Regular),
                    Location = new Point(4, 0),
                    Width = 210,
                    Height = rowH - 2,
                    TextAlign = ContentAlignment.MiddleRight,
                    Cursor = Cursors.Hand
                };

                var btnClose = new Button
                {
                    Text = "✕",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(180, 80, 80),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Size = new Size(30, rowH - 6),
                    Location = new Point(220, 2),
                    Cursor = Cursors.Hand
                };
                btnClose.FlatAppearance.BorderSize = 0;
                btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 50, 50);

                var capturedForm = form;
                var capturedTab = tab;

                lblName.Click += (snd, ev) => { _pnlDropdown.Close(); SwitchToTab(capturedForm, capturedTab); };
                row.Click += (snd, ev) => { _pnlDropdown.Close(); SwitchToTab(capturedForm, capturedTab); };
                btnClose.Click += (snd, ev) => { _pnlDropdown.Close(); CloseTab(capturedForm, capturedTab); };

                row.Controls.Add(lblName);
                row.Controls.Add(btnClose);
                container.Controls.Add(row);
            }

            if (_openTabs.Count > 6)
            {
                var rowMore = new Panel
                {
                    Width = 256,
                    Height = rowH - 2,
                    BackColor = Color.FromArgb(45, 45, 58),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 1, 0, 1)
                };

                var lblMore = new Label
                {
                    Text = "🔍 المزيد من الشاشات المفتوحة...",
                    ForeColor = Color.FromArgb(220, 220, 240),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Location = new Point(4, 0),
                    Width = 248,
                    Height = rowH - 2,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand
                };

                lblMore.Click += (snd, ev) => { _pnlDropdown.Close(); ShowMoreScreensDialog(); };
                rowMore.Click += (snd, ev) => { _pnlDropdown.Close(); ShowMoreScreensDialog(); };

                rowMore.Controls.Add(lblMore);
                container.Controls.Add(rowMore);
            }

            var containerHost = new ToolStripControlHost(container) { Padding = Padding.Empty, Margin = Padding.Empty };
            _pnlDropdown.Items.Add(containerHost);
            _pnlDropdown.Show(_btnOpenPages, new Point(0, _btnOpenPages.Height));
        }

        private void ShowMoreScreensDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "كل الشاشات المفتوحة للتنقل";
                dlg.Size = new Size(350, 450);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain;
                dlg.Font = Theme.FontMain;

                var lbl = new Label
                {
                    Text = "اختر الشاشة التي ترغب في الانتقال إليها:",
                    Location = new Point(12, 12),
                    Size = new Size(310, 20),
                    ForeColor = Theme.TextMain
                };

                var lst = new ListBox
                {
                    Location = new Point(12, 40),
                    Size = new Size(310, 280),
                    BackColor = Theme.BgInput,
                    ForeColor = Theme.TextMain,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 10f)
                };

                foreach (var entry in _openTabs)
                {
                    lst.Items.Add(entry.form.Text);
                }

                var btnGo = Theme.MakeButton("✔ انتقال", 12, 340, 140, 35, Theme.Accent);
                var btnCloseTab = Theme.MakeButton("❌ إغلاق الشاشة", 182, 340, 140, 35, Color.FromArgb(180, 50, 50));
                btnCloseTab.ForeColor = Color.White;

                btnGo.Click += (s, e) =>
                {
                    if (lst.SelectedIndex >= 0)
                    {
                        var entry = _openTabs[lst.SelectedIndex];
                        SwitchToTab(entry.form, entry.tab);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                btnCloseTab.Click += (s, e) =>
                {
                    if (lst.SelectedIndex >= 0)
                    {
                        var entry = _openTabs[lst.SelectedIndex];
                        CloseTab(entry.form, entry.tab);
                        
                        lst.Items.Clear();
                        foreach (var ent in _openTabs)
                        {
                            lst.Items.Add(ent.form.Text);
                        }
                        if (_openTabs.Count == 0) dlg.Close();
                    }
                };

                lst.DoubleClick += (s, e) =>
                {
                    if (lst.SelectedIndex >= 0)
                    {
                        var entry = _openTabs[lst.SelectedIndex];
                        SwitchToTab(entry.form, entry.tab);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                dlg.Controls.AddRange(new Control[] { lbl, lst, btnGo, btnCloseTab });
                dlg.ShowDialog();
            }
        }

        // ─── Tab management ────────────────────────────────────────────────
        private Button _AddTab(Form form)
        {
            if (form is FrmDashboard) return null;

            // Check if this form type is already open → switch to it
            foreach (var entry in _openTabs)
            {
                if (!entry.form.IsDisposed && entry.form.GetType() == form.GetType())
                {
                    SwitchToTab(entry.form, entry.tab);
                    return entry.tab;
                }
            }

            // Create dummy tag button (not shown directly, used as key)
            var tab = new Button { Tag = form };
            _openTabs.Add((form, tab));
            _UpdateDropdownButtonText();
            return tab;
        }

        private string TrimTabTitle(string title)
        {
            if (title == null) return "";
            return title.Length > 18 ? title.Substring(0, 16) + ".." : title;
        }

        private void SwitchToTab(Form form, Button tab)
        {
            if (form == null || form.IsDisposed) return;

            _currentChild = form;

            // Hide all children, show the selected one
            foreach (Control c in pnlContent.Controls)
                c.Visible = false;

            if (!pnlContent.Controls.Contains(form))
            {
                form.TopLevel = false;
                form.Dock = DockStyle.Fill;
                form.FormBorderStyle = FormBorderStyle.None;
                form.RightToLeft = RightToLeft.Yes;
                pnlContent.Controls.Add(form);
            }
            form.Visible = true;
            form.BringToFront();

            // Update title
            if (lblTitle != null)
            {
                lblTitle.Text = form is FrmDashboard ? "" : "الشاشة الحالية: " + form.Text;
            }

            HighlightActiveGroup(form.GetType().Name);

            // Layout adjustments to open child forms in full-screen mode:
            if (form is FrmDashboard)
            {
                pnlNavBar.Visible = true;
                pnlTopBar.Visible = true;
                pnlTabBar.Visible = false;
            }
            else
            {
                pnlNavBar.Visible = false;
                pnlTopBar.Visible = false;
                pnlTabBar.Visible = true;
            }

            _UpdateDropdownButtonText();
        }

        private void CloseTab(Form form, Button tab)
        {
            // Remove from list
            _openTabs.RemoveAll(t => t.form == form);

            if (!form.IsDisposed)
            {
                pnlContent.Controls.Remove(form);
                form.Dispose();
            }

            _UpdateDropdownButtonText();

            // Switch to last tab if any
            if (_openTabs.Count > 0)
            {
                var last = _openTabs[_openTabs.Count - 1];
                SwitchToTab(last.form, last.tab);
            }
            else
            {
                _currentChild = null;
                NavigateTo(new FrmDashboard());
            }
        }

        private void _UpdateDropdownButtonText()
        {
            if (_btnOpenPages != null)
            {
                _btnOpenPages.Text = $"📑 الشاشات المفتوحة ({_openTabs.Count}) ▾";
            }
        }

        private void _RefreshTabStyles(Button activeTab) { } // kept for compatibility

        public void NavigateTo(Form form)
        {
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.FormBorderStyle = FormBorderStyle.None;
            form.RightToLeft = RightToLeft.Yes;

            var tab = _AddTab(form);
            SwitchToTab(form, tab);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Only ask when the user clicked X (not programmatic close)
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show(
                    "هل تريد الخروج من البرنامج؟",
                    "تأكيد الخروج",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
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
            Theme.EnableDoubleBuffer(this);
            BuildUI();
        }

        private void BuildUI()
        {
            this.Controls.Clear();

            // Main full screen container panel (Matches exact logo background #0A0A0A so logo blends 100% seamlessly across full screen)
            var pnlShowcase = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 10, 10), // Rich midnight matching logo file background
                Padding = new Padding(20)
            };

            // Top Right Quick Details Button
            var btnQuickDetails = new Button
            {
                Text = "📊 التفاصيل والإحصائيات السريعة",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(220, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Location = new Point(pnlShowcase.Width - 240, 12)
            };
            btnQuickDetails.FlatAppearance.BorderSize = 0;
            btnQuickDetails.Click += (s, e) =>
            {
                var frmDetails = new FrmQuickDetails((frm) => NavigateMain(frm));
                frmDetails.ShowDialog(this);
            };

            pnlShowcase.Controls.Add(btnQuickDetails);

            // Full-Width Hero Logo (Spans wide across full screen width!)
            Image logoLarge = Theme.GetCompanyLogo();
            if (logoLarge == null) logoLarge = Theme.CreateDefaultLogoBitmap(450);

            var pbMainLogo = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = logoLarge,
                BackColor = Color.Transparent
            };

            var lblMainCompany = new Label
            {
                Text = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "نظام المحترفين المالي لإدارة المبيعات والتوزيع" : AppConfig.CompanyName,
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 198, 35), // Rich Gold
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Height = 48
            };

            var lblMainTagline = new Label
            {
                Text = "برنامج متكامل لإدارة المبيعات، المخازن، الحسابات والشاحنات 🚀  |  الإصدار الرسمي v2.0.119",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 195, 215), // Soft Silver
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Height = 30
            };

            // Bottom session badges
            var flowBadges = new FlowLayoutPanel
            {
                Height = 40,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(5, 0, 5, 0)
            };

            Action<string, string> addBadge = (iconText, bgHex) =>
            {
                var lblBadge = new Label
                {
                    Text = iconText,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = ColorTranslator.FromHtml(bgHex),
                    AutoSize = true,
                    Padding = new Padding(12, 6, 12, 6),
                    Margin = new Padding(4)
                };
                flowBadges.Controls.Add(lblBadge);
            };

            addBadge($"👤 المستخدم: {Session.EmpName}", "#2563EB");
            addBadge($"🏢 النشاط: {(string.IsNullOrWhiteSpace(AppConfig.BusinessType) ? "عام" : AppConfig.BusinessType)}", "#059669");
            addBadge($"📅 اليوم: {DateTime.Today:dd MMMM yyyy}", "#4B5563");
            addBadge($"🟢 حالة النظام: متصل وتعمل قاعدة البيانات بنجاح", "#047857");

            // Dynamic layout calculations: Logo spans wide across full screen width!
            pnlShowcase.SizeChanged += (s, e) =>
            {
                int w = pnlShowcase.Width;
                int h = pnlShowcase.Height;

                btnQuickDetails.Location = new Point(w - 240, 12);

                int logoW = Math.Max(300, w - 40); // Spans full width of the main screen
                int logoH = Math.Min(h - 170, 430);
                if (logoH < 180) logoH = 180;

                pbMainLogo.Size = new Size(logoW, logoH);
                pbMainLogo.Location = new Point(20, Math.Max(15, (h - logoH - 125) / 2));

                lblMainCompany.Size = new Size(w - 20, 48);
                lblMainCompany.Location = new Point(10, pbMainLogo.Bottom + 4);

                lblMainTagline.Size = new Size(w - 20, 30);
                lblMainTagline.Location = new Point(10, lblMainCompany.Bottom + 2);

                flowBadges.Location = new Point((w - flowBadges.Width) / 2, lblMainTagline.Bottom + 6);
            };

            pnlShowcase.Controls.Add(pbMainLogo);
            pnlShowcase.Controls.Add(lblMainCompany);
            pnlShowcase.Controls.Add(lblMainTagline);
            pnlShowcase.Controls.Add(flowBadges);

            this.Controls.Add(pnlShowcase);
        }

        private void AddCompactHeaderTile(FlowLayoutPanel p, string emoji, string title, Action onClick, Color color)
        {
            var btn = new Button
            {
                Text = $"{emoji} {title}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 35,
                AutoSize = true,
                Padding = new Padding(10, 3, 10, 3),
                Margin = new Padding(3, 2, 3, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 45, 65),
                ForeColor = Color.FromArgb(235, 235, 245),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(60, color);

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = color;
                btn.ForeColor = Color.White;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = Color.FromArgb(35, 45, 65);
                btn.ForeColor = Color.FromArgb(235, 235, 245);
            };

            btn.Click += (s, e) => onClick?.Invoke();
            p.Controls.Add(btn);
        }

        private void AddLargeActionTile(FlowLayoutPanel p, string emoji, string title, string subTitle, Action onClick, Color color)
        {
            var tile = new Panel
            {
                Size = new Size(200, 125),
                BackColor = Theme.BgCard,
                Margin = new Padding(12),
                Cursor = Cursors.Hand
            };

            var lblEmoji = new Label
            {
                Text = emoji,
                Font = new Font("Segoe UI Emoji", 24f),
                Size = new Size(200, 50),
                Location = new Point(0, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Size = new Size(200, 26),
                Location = new Point(0, 62),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextMain,
                BackColor = Color.Transparent
            };

            var lblSub = new Label
            {
                Text = subTitle,
                Font = new Font("Segoe UI", 8.5f),
                Size = new Size(200, 24),
                Location = new Point(0, 88),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            tile.Controls.Add(lblEmoji);
            tile.Controls.Add(lblTitle);
            tile.Controls.Add(lblSub);

            tile.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Theme.BorderColor, 1.5f))
                {
                    g.DrawRectangle(pen, 0, 0, tile.Width - 1, tile.Height - 1);
                }
                using (var brush = new SolidBrush(color))
                {
                    g.FillRectangle(brush, 0, tile.Height - 5, tile.Width, 5);
                }
            };

            Action applyHover = () =>
            {
                tile.BackColor = Color.FromArgb(20, color);
                lblTitle.ForeColor = color;
            };
            Action applyLeave = () =>
            {
                tile.BackColor = Theme.BgCard;
                lblTitle.ForeColor = Theme.TextMain;
            };

            tile.MouseEnter += (s, e) => applyHover();
            tile.MouseLeave += (s, e) => applyLeave();

            foreach (Control ctrl in tile.Controls)
            {
                ctrl.Cursor = Cursors.Hand;
                ctrl.MouseEnter += (s, e) => applyHover();
                ctrl.MouseLeave += (s, e) => applyLeave();
                ctrl.Click += (s, e) => onClick?.Invoke();
            }
            tile.Click += (s, e) => onClick?.Invoke();

            p.Controls.Add(tile);
        }

        private Panel MakeCard(string title, string value, Color color)
        {
            var card = new Panel
            {
                Size = new Size(240, 110),
                BackColor = Theme.BgCard,
                Margin = new Padding(12),
                Cursor = Cursors.Hand
            };

            card.MouseEnter += (s, e) => { card.BackColor = Color.FromArgb(10, color); };
            card.MouseLeave += (s, e) => { card.BackColor = Theme.BgCard; };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                Location = new Point(15, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(15, 48),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            card.Controls.AddRange(new Control[] { lblTitle, lblValue });

            foreach (Control ctrl in card.Controls)
            {
                ctrl.Cursor = Cursors.Hand;
                ctrl.MouseEnter += (s, e) => { card.BackColor = Color.FromArgb(10, color); };
                ctrl.MouseLeave += (s, e) => { card.BackColor = Theme.BgCard; };
            }

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                using (var brush = new SolidBrush(color))
                {
                    g.FillRectangle(brush, 0, 0, card.Width, 4);
                }
                
                using (var pen = new Pen(Theme.BorderColor, 1.5f))
                {
                    g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            
            return card;
        }

        private void AddActionTile(FlowLayoutPanel p, string emoji, string title, Action onClick, Color color)
        {
            var tile = new Panel
            {
                Size = new Size(110, 100),
                BackColor = Theme.BgCard,
                Margin = new Padding(8),
                Cursor = Cursors.Hand
            };
            
            var lblEmoji = new Label
            {
                Text = emoji,
                Font = new Font("Segoe UI Emoji", 20f),
                Size = new Size(110, 45),
                Location = new Point(0, 12),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Size = new Size(110, 35),
                Location = new Point(0, 57),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextMain,
                BackColor = Color.Transparent
            };
            
            tile.Controls.Add(lblEmoji);
            tile.Controls.Add(lblTitle);
            
            tile.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Theme.BorderColor, 1.5f))
                {
                    g.DrawRectangle(pen, 0, 0, tile.Width - 1, tile.Height - 1);
                }
                using (var brush = new SolidBrush(color))
                {
                    g.FillRectangle(brush, 0, tile.Height - 5, tile.Width, 5);
                }
            };
            
            tile.MouseEnter += (s, e) =>
            {
                tile.BackColor = Color.FromArgb(15, color);
                lblTitle.ForeColor = color;
            };
            tile.MouseLeave += (s, e) =>
            {
                tile.BackColor = Theme.BgCard;
                lblTitle.ForeColor = Theme.TextMain;
            };
            
            foreach (Control ctrl in tile.Controls)
            {
                ctrl.Cursor = Cursors.Hand;
                ctrl.MouseEnter += (s, e) =>
                {
                    tile.BackColor = Color.FromArgb(15, color);
                    lblTitle.ForeColor = color;
                };
                ctrl.MouseLeave += (s, e) =>
                {
                    tile.BackColor = Theme.BgCard;
                    lblTitle.ForeColor = Theme.TextMain;
                };
                ctrl.Click += (s, e) => onClick();
            }
            
            tile.Click += (s, e) => onClick();
            p.Controls.Add(tile);
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



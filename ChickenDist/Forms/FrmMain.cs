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
            this.Text = AppConfig.CompanyName + " - Ø§Ù„Ù†Ø¸Ø§Ù… Ø§Ù„Ø±Ø¦ÙŠØ³ÙŠ";
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
                Text = "ðŸ£  " + AppConfig.CompanyName,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.lblUserInfo = new Label
            {
                Text = $"ðŸ‘¤ {Session.EmpName}  |  {Session.Role}",
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
                Text = "Ø®Ø±ÙˆØ¬ â†©",
                Width = 100,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Danger,
                ForeColor = Color.White,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnLogoutTop.FlatAppearance.BorderSize = 0;
            btnLogoutTop.Click += (s, e) => { if (MessageBox.Show("Ù‡Ù„ ØªØ±ÙŠØ¯ ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø®Ø±ÙˆØ¬ØŸ", "ØªØ£ÙƒÙŠØ¯", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { Session.Clear(); this.Close(); } };
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
            lblCompany.Text = $"ðŸ£  {newName}";
            this.Text = $"{newName} - Ø§Ù„Ù†Ø¸Ø§Ù… Ø§Ù„Ø±Ø¦ÙŠØ³ÙŠ";
        }

        private void BuildNavBar()
        {
            pnlNavBar.Controls.Clear();
            
            var items = new (string icon, string label, string screen, Action action)[]
            {
                ("ðŸ ", "Ø§Ù„Ø±Ø¦ÙŠØ³ÙŠØ©",          "",               () => NavigateTo(new FrmDashboard())),
                ("ðŸ“¦", "Ø§Ù„Ø£ØµÙ†Ø§Ù",            "Products",       () => NavigateTo(new FrmProducts())),
                ("âš–ï¸", "Ø¬Ø±Ø¯ Ø§Ù„Ù…Ø®Ø²Ù†",        "Products",       () => NavigateTo(new FrmInventory())),
                ("ðŸ‘¥", "Ø§Ù„Ø¹Ù…Ù„Ø§Ø¡",            "Clients",        () => NavigateTo(new FrmClients())),
                ("ðŸš—", "Ø§Ù„Ù…Ø±ÙƒØ¨Ø§Øª",            "Vehicles",       () => NavigateTo(new FrmVehicles())),
                ("ðŸ¤", "Ø§Ù„Ù…ÙˆØ±Ø¯ÙŠÙ†",           "Suppliers",      () => NavigateTo(new FrmSuppliers())),
                ("ðŸ›’", "ÙØ§ØªÙˆØ±Ø© Ù…Ø¨ÙŠØ¹Ø§Øª",      "Sales",          () => NavigateTo(new FrmSale())),
                ("ðŸ“‹", "Ø³Ø¬Ù„ Ø§Ù„Ù…Ø¨ÙŠØ¹Ø§Øª",       "Sales",          () => NavigateTo(new FrmSalesList())),
                ("â†©", "Ù…Ø±ØªØ¬Ø¹ Ø¨ÙŠØ¹",          "Returns",        () => NavigateTo(new FrmReturn())),
                ("ðŸ“¥", "ÙØ§ØªÙˆØ±Ø© Ù…Ø´ØªØ±ÙŠØ§Øª",     "Purchases",      () => NavigateTo(new FrmPurchase())),
                ("ðŸšš", "Ø­Ù…ÙˆÙ„Ø© Ù…Ù†Ø¯ÙˆØ¨",       "DriverHandover", () => NavigateTo(new FrmDriverHandover())),
                ("ðŸ–¥ï¸", "Ù…Ø±Ø§Ù‚Ø¨Ø© Ø§Ù„Ù…Ù†Ø§Ø¯ÙŠØ¨",    "DriverHandover", () => NavigateTo(new FrmDriversMonitor())),
                ("ðŸ“‹", "Ø¹Ù‡Ø¯Ø© Ø§Ù„Ù…Ù†Ø§Ø¯ÙŠØ¨",      "DriverHandover", () => NavigateTo(new FrmDriverCustody())),
                ("ðŸ’°", "Ø§Ù„Ø®Ø²Ù†Ø©",             "CashBox",        () => NavigateTo(new FrmCashBox())),
                ("ðŸ“Š", "Ø§Ù„ØªÙ‚Ø§Ø±ÙŠØ±",           "Reports",        () => NavigateTo(new FrmReports())),
                ("ðŸ“‘", "ØªÙ‚ÙÙŠÙ„ ÙŠÙˆÙ…ÙŠØ©",        "Reports",        () => NavigateTo(new FrmDailyClosing())),
                ("ðŸ‘”", "Ø§Ù„Ù…ÙˆØ¸ÙÙŠÙ†",           "Employees",      () => NavigateTo(new FrmEmployees())),
                ("âš™ï¸", "Ø§Ù„Ø¥Ø¹Ø¯Ø§Ø¯Ø§Øª",         "",               () => new FrmSettings().ShowDialog()),
                ("ðŸ”„", "ØªØ­Ø¯ÙŠØ« Ø§Ù„Ø¨Ø±Ù†Ø§Ù…Ø¬",     "",               () => UpdateManager.CheckForUpdates(true)),
            };

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.screen) && !Session.CanAccess(item.screen)) continue;

                var btn = new Button
                {
                    Text = $"{item.icon} {item.label}",
                    Size = new Size(130, 45),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.White,
                    Font = Theme.FontArabic,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(5),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 255, 255, 255);
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, Theme.Accent.R, Theme.Accent.G, Theme.Accent.B);
                var act = item.action;
                btn.Click += (s, e) => act();
                pnlNavBar.Controls.Add(btn);
            }
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
            var pnlTitle = Theme.MakeTitleBar("Ù„ÙˆØ­Ø© Ø§Ù„ØªØ­ÙƒÙ… ÙˆØ§Ù„Ù…Ø¤Ø´Ø±Ø§Øª Ø§Ù„ÙŠÙˆÙ…ÙŠØ©", $"Ù…Ø±Ø­Ø¨Ø§Ù‹ {Session.EmpName} ðŸ‘‹  |  ØªØ§Ø±ÙŠØ® Ø§Ù„ÙŠÙˆÙ…: {DateTime.Today:dd/MM/yyyy}");
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

                pnlCards.Controls.Add(MakeCard("ðŸ’° Ø±ØµÙŠØ¯ Ø§Ù„Ø®Ø²Ù†Ø© Ø§Ù„Ø­Ø§Ù„ÙŠ", cashBal.ToString("N2") + " Ø¬", Theme.Success));
                pnlCards.Controls.Add(MakeCard("ðŸ›’ Ù…Ø¨ÙŠØ¹Ø§Øª Ø§Ù„ÙŠÙˆÙ…", todaySales.ToString("N2") + " Ø¬", Theme.Accent));
                pnlCards.Controls.Add(MakeCard("ðŸšš Ø­Ù…ÙˆÙ„Ø§Øª Ù…ÙØªÙˆØ­Ø© Ø­Ø§Ù„ÙŠØ§Ù‹", openLoads.Rows.Count + " Ø­Ù…ÙˆÙ„Ø©", Color.FromArgb(52, 152, 219)));
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
            var lblActTitle = new Label { Text = "⚡ الوصول السريع والعمليات", Font = Theme.FontHeader, ForeColor = Theme.Accent, Location = new Point(15, 15), AutoSize = true };
            pnlActions.Controls.Add(lblActTitle);

            int btnY = 55;
            AddQuickButton(pnlActions, "🛒 فاتورة مبيعات جديدة", ref btnY, () => NavigateMain(new FrmSale()), Theme.Accent);
            AddQuickButton(pnlActions, "🚗 إدارة المركبات", ref btnY, () => NavigateMain(new FrmVehicles()), Color.FromArgb(55, 135, 195));
            AddQuickButton(pnlActions, "🚚 تقفيل حمولة مندوب", ref btnY, () => NavigateMain(new FrmDriverHandover()), Theme.Primary);
            AddQuickButton(pnlActions, "💰 تسجيل حركة خزنة ومصروف", ref btnY, () => NavigateMain(new FrmCashBox()), Theme.Success);
            AddQuickButton(pnlActions, "⚖️ جرد كميات وتعديل المخزن", ref btnY, () => NavigateMain(new FrmInventory()), Color.FromArgb(120, 120, 80));
            AddQuickButton(pnlActions, "👥 إدارة وبيانات العملاء", ref btnY, () => NavigateMain(new FrmClients()), Color.FromArgb(100, 100, 150));
            AddQuickButton(pnlActions, "📊 عرض التقارير والإحصائيات", ref btnY, () => NavigateMain(new FrmReports()), Color.FromArgb(150, 100, 100));

            lowerTbl.Controls.Add(pnlActions, 0, 0);

            // 3b. Right Column: Recent Sales Invoices
            var pnlRecent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15),
                Margin = new Padding(8, 0, 15, 15)
            };
            var lblRecTitle = new Label { Text = "ðŸ“‹ Ø£Ø­Ø¯Ø« Ù…Ø¨ÙŠØ¹Ø§Øª ÙˆØ­Ù…ÙˆÙ„Ø§Øª Ø§Ù„ÙŠÙˆÙ…", Font = Theme.FontHeader, ForeColor = Theme.TextMain, Location = new Point(15, 15), AutoSize = true };
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
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", HeaderText = "ÙƒÙˆØ¯ Ø§Ù„ÙØ§ØªÙˆØ±Ø©", FillWeight = 40 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleType", HeaderText = "Ø§Ù„Ù†ÙˆØ¹", FillWeight = 30 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "Ø§Ù„Ø¹Ù…ÙŠÙ„ / Ø§Ù„Ù…Ù†Ø¯ÙˆØ¨", FillWeight = 50 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "Ø§Ù„Ù…Ø¨Ù„Øº Ø§Ù„Ø¥Ø¬Ù…Ø§Ù„ÙŠ", FillWeight = 35 });

            try
            {
                var dtSales = SaleDAL.GetAll(DateTime.Today, DateTime.Today);
                int limit = 0;
                foreach (DataRow r in dtSales.Rows)
                {
                    if (limit++ >= 8) break; // Display only last 8 sales
                    string clientOrDriver = r["SaleType"].ToString() == "DriverLoad" ? r["DriverName"].ToString() : r["ClientName"].ToString();
                    string typeArabic = r["SaleType"].ToString() == "Cash" ? "Ù†Ù‚Ø¯ÙŠ" : r["SaleType"].ToString() == "Credit" ? "Ø¢Ø¬Ù„" : "ØªØ­Ù…ÙŠÙ„ Ù…Ù†Ø¯ÙˆØ¨";
                    dgRecent.Rows.Add(r["SaleCode"], typeArabic, clientOrDriver, Convert.ToDecimal(r["TotalAmount"]).ToString("N2") + " Ø¬");
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



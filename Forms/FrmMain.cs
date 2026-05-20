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
            this.Text = "شركة توزيع الكتاكيت - النظام الرئيسي";
            this.Size = new Size(1280, 780);
            this.MinimumSize = new Size(1024, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Theme.BgLight;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // ===== TopBar =====
            this.pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Theme.Primary };
            this.lblCompany = new Label
            {
                Text = "🐣  شركة توزيع الكتاكيت",
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

        private void BuildNavBar()
        {
            pnlNavBar.Controls.Clear();
            
            var items = new (string icon, string label, string screen, Action action)[]
            {
                ("🏠", "الرئيسية",          "",               () => NavigateTo(new FrmDashboard())),
                ("📦", "الأصناف",            "Products",       () => NavigateTo(new FrmProducts())),
                ("⚖️", "جرد المخزن",        "Products",       () => NavigateTo(new FrmInventory())),
                ("👥", "العملاء",            "Clients",        () => NavigateTo(new FrmClients())),
                ("🛒", "فاتورة مبيعات",      "Sales",          () => NavigateTo(new FrmSale())),
                ("📋", "سجل المبيعات",       "Sales",          () => NavigateTo(new FrmSalesList())),
                ("↩", "مرتجع بيع",          "Returns",        () => NavigateTo(new FrmReturn())),
                ("🚚", "حمولة مندوب",       "DriverHandover", () => NavigateTo(new FrmDriverHandover())),
                ("🖥️", "مراقبة المناديب",    "DriverHandover", () => NavigateTo(new FrmDriversMonitor())),
                ("📋", "عهدة المناديب",      "DriverHandover", () => NavigateTo(new FrmDriverCustody())),
                ("💰", "الخزنة",             "CashBox",        () => NavigateTo(new FrmCashBox())),
                ("📊", "التقارير",           "Reports",        () => NavigateTo(new FrmReports())),
                ("📑", "تقفيل يومية",        "Reports",        () => NavigateTo(new FrmDailyClosing())),
                ("👔", "الموظفين",           "Employees",      () => NavigateTo(new FrmEmployees())),
                ("🔄", "تحديث البرنامج",     "",               () => UpdateManager.CheckForUpdates(true)),
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
            catch { }
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
            catch { }

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

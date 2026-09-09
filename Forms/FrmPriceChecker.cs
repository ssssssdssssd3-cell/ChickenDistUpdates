using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// كشك الاستعلام الذكي عن أسعار الأصناف والخدمة الذاتية
    /// </summary>
    public class FrmPriceChecker : Form
    {
        // ── Controls ──────────────────────────────────────────────────
        // Header
        private Panel pnlHeader;
        private PictureBox picLogo;
        private Label lblCompany;
        private Label lblHeaderSubtitle;
        private Label lblClock;
        private Button btnToggleFullscreen;
        private Button btnClose;

        // Footer
        private Panel pnlFooter;
        private Label lblCustomerGreeting;
        private Panel pnlCountdownWrap;
        private Label lblCountdownText;
        private ProgressBar prgResetCountdown;
        private Button btnScanAnother;

        // Content & Containers
        private Panel pnlContent;
        private Panel pnlStandby;
        private Panel pnlProductDetails;

        // Standby UI (منطقة اسكان الصنف)
        private Panel pnlStandbyCard;
        private Panel pnlScannerZone;
        private Label lblBarcodeArt;
        private Label lblScannerLaser;
        private Label lblScanPrompt;
        private Label lblStandbySubtitle;
        private Panel pnlSearchBox;
        private TextBox txtManualSearch;
        private Button btnManualSearch;

        // Product Details UI (التركيز على الصنف والسعر في منتصف الشاشة)
        private Panel pnlHeroCard;
        private Label lblActiveScannerHint;
        private FlowLayoutPanel pnlBadges;
        private Label lblBarcodeBadge;
        private Label lblCategoryBadge;
        private Label lblShelfLocation;
        private Label lblStockStatus;
        private Label lblProductName;
        private Label lblEnglishName;
        private Panel pnlPriceHero;
        private Label lblPriceMain;
        private Label lblPriceUnit;
        private Label lblOriginalPrice;
        private Label lblDiscountBadge;
        private FlowLayoutPanel flowUnits;
        private Panel pnlAlternativesSection;
        private Label lblAlternativesTitle;
        private FlowLayoutPanel flowAlternatives;

        // Scanner Input
        private TextBox txtBarcodeScan;

        // Timers & State
        private System.Windows.Forms.Timer _clockTimer;
        private System.Windows.Forms.Timer _resetCountdownTimer;
        private System.Windows.Forms.Timer _laserPulseTimer;
        private int _countdownSeconds = 12;
        private bool _isFullscreen = false;
        private bool _laserPulseState = false;
        private System.Text.StringBuilder _scannerBuffer = new System.Text.StringBuilder();
        private DateTime _lastScannerChar = DateTime.MinValue;

        public FrmPriceChecker(bool startFullscreen = false)
        {
            _isFullscreen = startFullscreen;
            InitializeComponent();
            SetupTimers();
            SwitchToStandby();
        }

        private void InitializeComponent()
        {
            this.Text = "🏷️ كشك الاستعلام الذكي وفحص الأسعار";
            this.Size = new Size(1150, 760);
            this.MinimumSize = new Size(950, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 12, 38);   // Deep Royal Background
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.KeyPreview = true;

            if (_isFullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }

            this.KeyDown += FrmPriceChecker_KeyDown;

            // ══════════════════════════════════════════════════════════════
            // 1. Top Header: اللوجو واسم المكان بشكل منظم واحترافي
            // ══════════════════════════════════════════════════════════════
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(36, 18, 75),   // Rich Royal Purple
                Padding = new Padding(20, 10, 20, 10)
            };

            pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(126, 58, 242), 2))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            // Logo
            picLogo = new PictureBox
            {
                Size = new Size(64, 64),
                Location = new Point(15, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            try
            {
                picLogo.Image = Theme.GetCompanyLogo();
            }
            catch { }

            // Company Name
            string compName = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "البرنامج الذكي" : AppConfig.CompanyName;
            lblCompany = new Label
            {
                Text = compName,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),   // Bright Gold
                AutoSize = true,
                Location = new Point(90, 14)
            };

            // Header Subtitle
            lblHeaderSubtitle = new Label
            {
                Text = "كشك فحص الأسعار والخدمة الذاتية | Self-Service Price Checker",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(216, 180, 254), // Light Lilac
                AutoSize = true,
                Location = new Point(92, 46)
            };

            // Digital Clock
            lblClock = new Label
            {
                Text = DateTime.Now.ToString("hh:mm:ss tt  |  yyyy/MM/dd"),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(233, 213, 255),
                AutoSize = true
            };

            // Fullscreen Button
            btnToggleFullscreen = new Button
            {
                Text = _isFullscreen ? "🗗 نافذة" : "🗖 ملء الشاشة",
                Size = new Size(115, 38),
                BackColor = Color.FromArgb(67, 34, 128),
                ForeColor = Color.FromArgb(243, 232, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnToggleFullscreen.FlatAppearance.BorderSize = 0;
            btnToggleFullscreen.Click += (s, e) => ToggleFullscreen();

            // Close Button
            btnClose = new Button
            {
                Text = "❌ إغلاق",
                Size = new Size(90, 38),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(picLogo);
            pnlHeader.Controls.Add(lblCompany);
            pnlHeader.Controls.Add(lblHeaderSubtitle);
            pnlHeader.Controls.Add(lblClock);
            pnlHeader.Controls.Add(btnToggleFullscreen);
            pnlHeader.Controls.Add(btnClose);

            pnlHeader.Resize += (s, e) => LayoutHeaderControls();
            LayoutHeaderControls();

            // ══════════════════════════════════════════════════════════════
            // 2. Bottom Footer: جملة حلوة مميزة للعميل مع شريط العد
            // ══════════════════════════════════════════════════════════════
            pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                BackColor = Color.FromArgb(26, 14, 56),   // Dark Violet
                Padding = new Padding(15, 8, 15, 8)
            };

            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(109, 40, 217), 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
                }
            };

            // Customer Warm Greeting Message (جملة حلوة مميزة للعميل)
            lblCustomerGreeting = new Label
            {
                Text = "✨ نسعد دائماً بزيارتكم الكريمة.. نتشرف بخدمتكم ونتمنى لكم تجربة تسوق ممتعة ويوم سعيد 🌟",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(254, 240, 138),  // Glowing Warm Gold
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };

            // Countdown & Reset Controls
            pnlCountdownWrap = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = Color.Transparent,
                Visible = false
            };

            lblCountdownText = new Label
            {
                Text = "⏳ العودة للاستقبال بعد 12 ثانية...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(216, 180, 254),
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleRight
            };

            prgResetCountdown = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 12,
                Maximum = 100,
                Value = 100
            };

            pnlCountdownWrap.Controls.Add(prgResetCountdown);
            pnlCountdownWrap.Controls.Add(lblCountdownText);

            btnScanAnother = new Button
            {
                Text = "🔍 فحص صنف آخر",
                Dock = DockStyle.Right,
                Width = 155,
                BackColor = Color.FromArgb(234, 179, 8),   // Bright Gold
                ForeColor = Color.FromArgb(20, 14, 45),    // Dark text
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnScanAnother.FlatAppearance.BorderSize = 0;
            btnScanAnother.Click += (s, e) => SwitchToStandby();

            pnlFooter.Controls.Add(lblCustomerGreeting);
            pnlFooter.Controls.Add(btnScanAnother);
            pnlFooter.Controls.Add(pnlCountdownWrap);

            // ══════════════════════════════════════════════════════════════
            // 3. Center Content Container
            // ══════════════════════════════════════════════════════════════
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 12, 38),  // Deep Indigo
                Padding = new Padding(12)
            };

            // Docking hierarchy
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);

            pnlHeader.SendToBack();
            pnlFooter.SendToBack();
            pnlContent.BringToFront();

            // Hidden Barcode Scanner Input
            txtBarcodeScan = new TextBox
            {
                Location = new Point(-200, -200),
                Size = new Size(100, 20)
            };
            txtBarcodeScan.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtBarcodeScan.Text))
                {
                    string code = txtBarcodeScan.Text.Trim();
                    txtBarcodeScan.Text = "";
                    ProcessBarcodeSearch(code);
                }
            };
            this.Controls.Add(txtBarcodeScan);

            BuildStandbyUI();
            BuildProductDetailsUI();
        }

        private void LayoutHeaderControls()
        {
            if (pnlHeader == null || btnClose == null) return;

            // In RTL layout (RightToLeftLayout = true), WinForms mirrors X:
            btnClose.Location = new Point(pnlHeader.Width - 105, 23);
            btnToggleFullscreen.Location = new Point(pnlHeader.Width - 230, 23);
            lblClock.Location = new Point(pnlHeader.Width - 475, 30);
        }

        // ══════════════════════════════════════════════════════════════
        // 4. Standby Mode (منطقة اسكان الصنف بشكل جذاب وواضح)
        // ══════════════════════════════════════════════════════════════
        private void BuildStandbyUI()
        {
            pnlStandby = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            pnlStandbyCard = new Panel
            {
                Size = new Size(820, 480),
                BackColor = Color.FromArgb(36, 20, 76),   // Deep Royal Purple Card
                Padding = new Padding(22)
            };

            pnlStandbyCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(147, 51, 234), 2))
                {
                    var rect = pnlStandbyCard.ClientRectangle;
                    rect.Width -= 2;
                    rect.Height -= 2;
                    g.DrawRectangle(pen, rect);
                }
            };

            // Badge Top
            var lblKioskBadge = new Label
            {
                Text = "⚡ الاستعلام الفوري والمباشر عن الأسعار والخدمة الذاتية",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                BackColor = Color.FromArgb(60, 30, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 34
            };

            // ── Scanner Zone (مكان اسكان الصنف) ──
            pnlScannerZone = new Panel
            {
                Dock = DockStyle.Top,
                Height = 240,
                BackColor = Color.FromArgb(24, 14, 52),
                Margin = new Padding(0, 10, 0, 10),
                Padding = new Padding(12)
            };

            pnlScannerZone.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color glowColor = _laserPulseState ? Color.FromArgb(239, 68, 68) : Color.FromArgb(168, 85, 247);
                using (var pen = new Pen(glowColor, 2) { DashStyle = DashStyle.Dash })
                {
                    var r = pnlScannerZone.ClientRectangle;
                    r.Inflate(-8, -8);
                    g.DrawRectangle(pen, r);
                }
            };

            // Barcode ASCII Art Icon
            lblBarcodeArt = new Label
            {
                Text = "║▌║█║▌│║▌║▌█║ ▌│║▌║▌║",
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = Color.FromArgb(216, 180, 254),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 44
            };

            // Laser Beam Line
            lblScannerLaser = new Label
            {
                Text = "━━━━━━━ 🔴 وجّه شعاع القارئ هنا 🔴 ━━━━━━━",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(239, 68, 68),  // Laser Red
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 26
            };

            // Scan Prompt
            lblScanPrompt = new Label
            {
                Text = "وجّه باركود الصنف هنا نحو شعاع القارئ",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),   // Bright Gold
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50
            };

            // Subtitle
            lblStandbySubtitle = new Label
            {
                Text = "مرر باركود أي سلعة وسيظهر الاسم والسعر والتفاصيل فوراً في منتصف الشاشة",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(233, 213, 255),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 32
            };

            pnlScannerZone.Controls.Add(lblStandbySubtitle);
            pnlScannerZone.Controls.Add(lblScanPrompt);
            pnlScannerZone.Controls.Add(lblScannerLaser);
            pnlScannerZone.Controls.Add(lblBarcodeArt);

            // ── Manual Search Area (للبحث باللمس أو بدون باركود) ──
            pnlSearchBox = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 105,
                BackColor = Color.FromArgb(26, 15, 58),
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblSearchTitle = new Label
            {
                Text = "أو أدخل كود الصنف / الاسم يدوياً باللمس:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(216, 180, 254),
                Dock = DockStyle.Top,
                Height = 24
            };

            var pnlInputRow = new Panel { Dock = DockStyle.Bottom, Height = 44 };

            txtManualSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                BackColor = Color.FromArgb(55, 32, 110),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtManualSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtManualSearch.Text))
                {
                    ProcessBarcodeSearch(txtManualSearch.Text.Trim());
                }
            };

            btnManualSearch = new Button
            {
                Text = "🔍 استعلام",
                Dock = DockStyle.Left,
                Width = 125,
                BackColor = Color.FromArgb(234, 179, 8),    // Gold
                ForeColor = Color.FromArgb(20, 14, 45),     // Dark text
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnManualSearch.FlatAppearance.BorderSize = 0;
            btnManualSearch.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtManualSearch.Text))
                {
                    ProcessBarcodeSearch(txtManualSearch.Text.Trim());
                }
            };

            pnlInputRow.Controls.Add(txtManualSearch);
            pnlInputRow.Controls.Add(btnManualSearch);

            pnlSearchBox.Controls.Add(pnlInputRow);
            pnlSearchBox.Controls.Add(lblSearchTitle);

            pnlStandbyCard.Controls.Add(pnlSearchBox);
            pnlStandbyCard.Controls.Add(pnlScannerZone);
            pnlStandbyCard.Controls.Add(lblKioskBadge);

            pnlStandby.Controls.Add(pnlStandbyCard);
            pnlStandby.Resize += (s, e) => CenterCard(pnlStandby, pnlStandbyCard);
            CenterCard(pnlStandby, pnlStandbyCard);

            pnlContent.Controls.Add(pnlStandby);
        }

        // ══════════════════════════════════════════════════════════════
        // 5. Product Scanned View: التركيز التام على الصنف والسعر
        // ══════════════════════════════════════════════════════════════
        private void BuildProductDetailsUI()
        {
            pnlProductDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Visible = false,
                AutoScroll = true
            };

            // Main Scanned Item Card (Centered Hero)
            pnlHeroCard = new Panel
            {
                Size = new Size(900, 520),
                BackColor = Color.FromArgb(36, 20, 76),   // Rich Violet
                Padding = new Padding(20, 15, 20, 15)
            };

            pnlHeroCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(234, 179, 8), 2)) // Gold Border
                {
                    var r = pnlHeroCard.ClientRectangle;
                    r.Width -= 2;
                    r.Height -= 2;
                    g.DrawRectangle(pen, r);
                }
            };

            // Active Scanner Status Indicator (مكان اسكان الصنف حتى أثناء عرض الصنف)
            lblActiveScannerHint = new Label
            {
                Text = "⚡ قارئ الباركود نشط الآن: يمكنك تمرير باركود أي صنف آخر فوراً للاستعلام في أي وقت",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),  // Gold
                BackColor = Color.FromArgb(50, 25, 105),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 28
            };

            // Badges Row
            pnlBadges = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 35,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 3, 0, 0)
            };

            lblBarcodeBadge = CreateBadge("🏷️ كود: ----", Color.FromArgb(24, 14, 52), Color.FromArgb(216, 180, 254));
            lblCategoryBadge = CreateBadge("📂 القسم: ----", Color.FromArgb(24, 14, 52), Color.FromArgb(250, 204, 21));
            lblShelfLocation = CreateBadge("📍 مكان الرف: ----", Color.FromArgb(79, 45, 143), Color.FromArgb(253, 224, 71));
            lblStockStatus = CreateBadge("🟢 متوفر بالفرع", Color.FromArgb(20, 83, 45), Color.FromArgb(134, 239, 172));

            pnlBadges.Controls.Add(lblBarcodeBadge);
            pnlBadges.Controls.Add(lblCategoryBadge);
            pnlBadges.Controls.Add(lblShelfLocation);
            pnlBadges.Controls.Add(lblStockStatus);

            // Product Name (اسم الصنف في منتصف الشاشة بخط كبير وواضح)
            lblProductName = new Label
            {
                Text = "اسم الصنف",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 58,
                AutoEllipsis = true
            };

            // English / Sub Name
            lblEnglishName = new Label
            {
                Text = "English Name",
                Font = new Font("Segoe UI", 11f, FontStyle.Italic),
                ForeColor = Color.FromArgb(216, 180, 254),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 22,
                AutoEllipsis = true
            };

            // ── Price Hero Showcase (السعر بخط ضخم ومميز في المنتصف) ──
            pnlPriceHero = new Panel
            {
                Dock = DockStyle.Top,
                Height = 152,
                BackColor = Color.FromArgb(25, 14, 55),
                Margin = new Padding(0, 6, 0, 6),
                Padding = new Padding(12, 4, 12, 4)
            };

            pnlPriceHero.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(126, 58, 242), 1.5f))
                {
                    var r = pnlPriceHero.ClientRectangle;
                    r.Inflate(-2, -2);
                    e.Graphics.DrawRectangle(pen, r);
                }
            };

            lblPriceMain = new Label
            {
                Text = "0.00 ج.م",
                Font = new Font("Segoe UI", 50f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),   // Bright Glowing Gold
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 74
            };

            lblPriceUnit = new Label
            {
                Text = "لكل (قطعة)",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(233, 213, 255),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 25
            };

            var pnlOfferRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0)
            };

            lblOriginalPrice = new Label
            {
                Text = "0.00 ج",
                Font = new Font("Segoe UI", 13f, FontStyle.Strikeout),
                ForeColor = Color.FromArgb(248, 113, 113),  // Soft Red Strikeout
                AutoSize = true,
                Margin = new Padding(10, 3, 10, 0),
                Visible = false
            };

            lblDiscountBadge = new Label
            {
                Text = "🔥 وفر 0.00 ج (0% خصم)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(185, 28, 28),     // Crimson Red
                Padding = new Padding(8, 3, 8, 3),
                AutoSize = true,
                Margin = new Padding(10, 0, 10, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            pnlOfferRow.Controls.Add(lblDiscountBadge);
            pnlOfferRow.Controls.Add(lblOriginalPrice);

            pnlPriceHero.Controls.Add(pnlOfferRow);
            pnlPriceHero.Controls.Add(lblPriceUnit);
            pnlPriceHero.Controls.Add(lblPriceMain);

            // Units Row (لو الصنف ليه كرتونة أو دستة أو قطاعي)
            flowUnits = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                AutoScroll = true,
                Padding = new Padding(0, 3, 0, 0)
            };

            // ── Alternatives Section (بدائل اقتصادية سريعة ومدمجة) ──
            pnlAlternativesSection = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 4, 0, 0)
            };

            lblAlternativesTitle = new Label
            {
                Text = "✨ بدائل متوفرة وخيارات اقتصادية في نفس القسم:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Dock = DockStyle.Top,
                Height = 22
            };

            flowAlternatives = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoScroll = true,
                Padding = new Padding(0, 2, 0, 0)
            };

            pnlAlternativesSection.Controls.Add(flowAlternatives);
            pnlAlternativesSection.Controls.Add(lblAlternativesTitle);

            // Assemble Hero Card
            pnlHeroCard.Controls.Add(pnlAlternativesSection);
            pnlHeroCard.Controls.Add(flowUnits);
            pnlHeroCard.Controls.Add(pnlPriceHero);
            pnlHeroCard.Controls.Add(lblEnglishName);
            pnlHeroCard.Controls.Add(lblProductName);
            pnlHeroCard.Controls.Add(pnlBadges);
            pnlHeroCard.Controls.Add(lblActiveScannerHint);

            pnlProductDetails.Controls.Add(pnlHeroCard);
            pnlProductDetails.Resize += (s, e) => CenterCard(pnlProductDetails, pnlHeroCard);
            CenterCard(pnlProductDetails, pnlHeroCard);

            pnlContent.Controls.Add(pnlProductDetails);
        }

        private void CenterCard(Panel container, Panel card)
        {
            if (container == null || card == null) return;
            int x = Math.Max(10, (container.ClientSize.Width - card.Width) / 2);
            int y = Math.Max(10, (container.ClientSize.Height - card.Height) / 2);
            card.Location = new Point(x, y);
        }

        private Label CreateBadge(string text, Color backColor, Color foreColor)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = foreColor,
                BackColor = backColor,
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private void SetupTimers()
        {
            // Clock Timer (1 sec)
            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) =>
            {
                lblClock.Text = DateTime.Now.ToString("hh:mm:ss tt  |  yyyy/MM/dd");
            };
            _clockTimer.Start();

            // Laser Pulse Effect (600ms)
            _laserPulseTimer = new System.Windows.Forms.Timer { Interval = 600 };
            _laserPulseTimer.Tick += (s, e) =>
            {
                _laserPulseState = !_laserPulseState;
                if (pnlStandby.Visible)
                {
                    lblScannerLaser.ForeColor = _laserPulseState ? Color.FromArgb(239, 68, 68) : Color.FromArgb(250, 204, 21);
                    pnlScannerZone.Invalidate();
                }
            };
            _laserPulseTimer.Start();

            // Reset Countdown Timer (1 sec)
            _resetCountdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _resetCountdownTimer.Tick += (s, e) =>
            {
                _countdownSeconds--;
                if (_countdownSeconds <= 0)
                {
                    _resetCountdownTimer.Stop();
                    SwitchToStandby();
                }
                else
                {
                    lblCountdownText.Text = $"⏳ العودة للاستقبال بعد {_countdownSeconds} ثانية...";
                    prgResetCountdown.Value = Math.Max(0, Math.Min(100, (_countdownSeconds * 100) / 12));
                }
            };
        }

        private void SwitchToStandby()
        {
            _resetCountdownTimer.Stop();
            pnlProductDetails.Visible = false;
            pnlStandby.Visible = true;
            txtManualSearch.Text = "";
            txtBarcodeScan.Text = "";

            // Footer in Standby Mode
            string compName = string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? "البرنامج الذكي" : AppConfig.CompanyName;
            lblCustomerGreeting.Text = $"✨ أهلاً بكم في {compName}.. نسعد دائماً بخدمتكم ونتمنى لكم تجربة تسوق ممتعة 🌟";
            pnlCountdownWrap.Visible = false;
            btnScanAnother.Visible = false;

            this.ActiveControl = txtBarcodeScan;
            txtBarcodeScan.Focus();
        }

        private void ProcessBarcodeSearch(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            try
            {
                var dr = ProductDAL.GetByBarcodeOrScaleCode(code, out decimal parsedWeight);
                if (dr == null)
                {
                    // Fallback to code or name search
                    var dtAll = DbHelper.Query(@"
                        SELECT TOP 1 p.*, c.CategoryName 
                        FROM Products p 
                        LEFT JOIN Categories c ON p.CategoryID = c.CategoryID 
                        WHERE p.IsActive = 1 AND (p.ProductName LIKE @q OR p.ProductCode = @code)
                        ORDER BY p.ProductName",
                        DbHelper.P("@q", "%" + code + "%"),
                        DbHelper.P("@code", code));

                    if (dtAll.Rows.Count > 0)
                    {
                        dr = dtAll.Rows[0];
                    }
                }

                if (dr != null)
                {
                    DisplayProduct(dr);
                }
                else
                {
                    MessageBox.Show($"❌ لم يتم العثور على صنف مطابق للباركود:\n({code})", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SwitchToStandby();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPriceChecker.ProcessBarcodeSearch", ex);
                MessageBox.Show("حدث خطأ أثناء فحص الصنف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SwitchToStandby();
            }
        }

        private void DisplayProduct(DataRow dr)
        {
            int productID = Convert.ToInt32(dr["ProductID"]);
            string prodCode = dr["ProductCode"]?.ToString() ?? "";
            string prodName = dr["ProductName"]?.ToString() ?? "";
            string engName = dr.Table.Columns.Contains("EnglishName") ? dr["EnglishName"]?.ToString() : "";
            string catName = dr.Table.Columns.Contains("CategoryName") ? dr["CategoryName"]?.ToString() : "";
            int catID = dr.Table.Columns.Contains("CategoryID") && dr["CategoryID"] != DBNull.Value ? Convert.ToInt32(dr["CategoryID"]) : 0;
            string shelfLoc = dr.Table.Columns.Contains("ShelfLocation") ? dr["ShelfLocation"]?.ToString() : "";
            decimal salePrice = Convert.ToDecimal(dr["SalePrice"]);
            string unit = dr["Unit"]?.ToString() ?? "قطعة";

            // Stock Check
            decimal totalStock = 0m;
            try
            {
                object stockObj = DbHelper.Scalar("SELECT SUM(Quantity) FROM ProductStock WHERE ProductID = @pid", DbHelper.P("@pid", productID));
                if (stockObj != null && stockObj != DBNull.Value)
                {
                    totalStock = Convert.ToDecimal(stockObj);
                }
            }
            catch { }

            // Populate Badges
            lblBarcodeBadge.Text = $"🏷️ كود: {prodCode}";
            lblCategoryBadge.Text = string.IsNullOrWhiteSpace(catName) ? "📂 القسم: عام" : $"📂 {catName}";
            lblShelfLocation.Text = string.IsNullOrWhiteSpace(shelfLoc) ? "📍 مكان الرف: غير محدد" : $"📍 مكان الرف: {shelfLoc}";

            if (totalStock > 5)
            {
                lblStockStatus.Text = "🟢 متوفر بالفرع";
                lblStockStatus.BackColor = Color.FromArgb(6, 78, 59);
                lblStockStatus.ForeColor = Color.FromArgb(74, 222, 128);
            }
            else if (totalStock > 0)
            {
                lblStockStatus.Text = $"🟡 كمية محدودة ({totalStock:N0} {unit})";
                lblStockStatus.BackColor = Color.FromArgb(113, 63, 18);
                lblStockStatus.ForeColor = Color.FromArgb(253, 224, 71);
            }
            else
            {
                lblStockStatus.Text = "🔴 غير متوفر حالياً بالمخزن";
                lblStockStatus.BackColor = Color.FromArgb(136, 19, 55);
                lblStockStatus.ForeColor = Color.FromArgb(251, 113, 133);
            }

            // Populate Names (بخط واضح ومركزي)
            lblProductName.Text = prodName;
            lblEnglishName.Text = string.IsNullOrWhiteSpace(engName) ? "" : engName;
            lblEnglishName.Visible = !string.IsNullOrWhiteSpace(engName);

            // Populate Price & Offers (في منتصف الشاشة بخط ضخم)
            bool isOffer = dr.Table.Columns.Contains("IsOffer") && dr["IsOffer"] != DBNull.Value && Convert.ToBoolean(dr["IsOffer"]);
            decimal origPrice = dr.Table.Columns.Contains("OriginalPrice") && dr["OriginalPrice"] != DBNull.Value ? Convert.ToDecimal(dr["OriginalPrice"]) : 0m;

            lblPriceMain.Text = $"{salePrice:N2} ج.م";
            lblPriceUnit.Text = $"لكل ({unit})";

            if (isOffer && origPrice > salePrice)
            {
                decimal saved = origPrice - salePrice;
                decimal pct = (saved / origPrice) * 100m;
                lblOriginalPrice.Text = $"السعر قبل الخصم: {origPrice:N2} ج.م";
                lblOriginalPrice.Visible = true;
                lblDiscountBadge.Text = $"🔥 وفر {saved:N2} ج.م ({pct:N0}% خصم)";
                lblDiscountBadge.Visible = true;
            }
            else
            {
                lblOriginalPrice.Visible = false;
                lblDiscountBadge.Visible = false;
            }

            // Populate Multi-Units
            flowUnits.Controls.Clear();
            flowUnits.Controls.Add(CreateUnitCard(unit, salePrice, 1));

            if (dr.Table.Columns.Contains("Unit1Name") && !string.IsNullOrWhiteSpace(dr["Unit1Name"]?.ToString()))
            {
                string u1 = dr["Unit1Name"].ToString();
                decimal p1 = dr["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(dr["Unit1SalePrice"]) : 0m;
                if (p1 > 0) flowUnits.Controls.Add(CreateUnitCard(u1, p1, 1));
            }
            if (dr.Table.Columns.Contains("Unit2Name") && !string.IsNullOrWhiteSpace(dr["Unit2Name"]?.ToString()))
            {
                string u2 = dr["Unit2Name"].ToString();
                decimal p2 = dr["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(dr["Unit2SalePrice"]) : 0m;
                decimal f2 = dr["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(dr["Unit2Factor"]) : 1m;
                if (p2 > 0) flowUnits.Controls.Add(CreateUnitCard(u2, p2, f2));
            }

            // Populate Alternatives
            LoadAlternatives(productID, catID, salePrice, totalStock <= 0);

            // Switch to Product View
            pnlStandby.Visible = false;
            pnlProductDetails.Visible = true;

            // Footer in Product View (جملة مميزة + عداد العودة)
            lblCustomerGreeting.Text = "✨ شكراً لثقتكم الغالية.. نتشرف بخدمتكم دائماً ونتمنى لكم تجربة تسوق ممتعة 🌟";
            pnlCountdownWrap.Visible = true;
            btnScanAnother.Visible = true;

            // Start Countdown (12s)
            _countdownSeconds = 12;
            prgResetCountdown.Value = 100;
            lblCountdownText.Text = "⏳ العودة للاستقبال بعد 12 ثانية...";
            _resetCountdownTimer.Start();

            this.ActiveControl = txtBarcodeScan;
            txtBarcodeScan.Focus();
        }

        private Control CreateUnitCard(string unitName, decimal price, decimal factor)
        {
            var pnl = new Panel
            {
                Size = new Size(165, 40),
                BackColor = Color.FromArgb(24, 14, 52),
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(8, 2, 8, 2)
            };

            var lblU = new Label
            {
                Text = unitName,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(216, 180, 254),
                Dock = DockStyle.Top,
                Height = 16
            };

            var lblP = new Label
            {
                Text = $"{price:N2} ج.م",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Dock = DockStyle.Bottom,
                Height = 18
            };

            pnl.Controls.Add(lblP);
            pnl.Controls.Add(lblU);
            return pnl;
        }

        private void LoadAlternatives(int productID, int categoryID, decimal currentPrice, bool isOutOfStock)
        {
            flowAlternatives.Controls.Clear();

            if (isOutOfStock)
            {
                lblAlternativesTitle.Text = "⚠️ هذا الصنف غير متوفر حالياً! إليك أفضل البدائل المتاحة فوراً بالفرع:";
                lblAlternativesTitle.ForeColor = Color.FromArgb(252, 165, 165);
            }
            else
            {
                lblAlternativesTitle.Text = "✨ بدائل متوفرة وخيارات اقتصادية أوفر في نفس القسم:";
                lblAlternativesTitle.ForeColor = Color.FromArgb(250, 204, 21);
            }

            try
            {
                string sql = @"
                    SELECT TOP 4 
                        p.ProductID, p.ProductCode, p.ProductName, p.EnglishName, p.Unit, p.SalePrice, 
                        p.ShelfLocation, p.IsOffer, p.OriginalPrice, c.CategoryName,
                        ISNULL((SELECT SUM(Quantity) FROM ProductStock WHERE ProductID = p.ProductID), 0) AS TotalStock
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive = 1 
                      AND p.ProductID <> @pid
                      AND (@catId <= 0 OR p.CategoryID = @catId)
                    ORDER BY 
                        CASE WHEN (SELECT SUM(Quantity) FROM ProductStock WHERE ProductID = p.ProductID) > 0 THEN 0 ELSE 1 END ASC,
                        CASE WHEN p.SalePrice < @price AND p.SalePrice > 0 THEN 0 ELSE 1 END ASC,
                        p.SalePrice ASC,
                        p.ProductName ASC";

                var dt = DbHelper.Query(sql,
                    DbHelper.P("@pid", productID),
                    DbHelper.P("@catId", categoryID),
                    DbHelper.P("@price", currentPrice));

                if (dt.Rows.Count == 0)
                {
                    var lblNone = new Label
                    {
                        Text = "لا توجد بدائل مسجلة أخرى في هذا القسم حالياً.",
                        Font = new Font("Segoe UI", 9.5f),
                        ForeColor = Color.FromArgb(216, 180, 254),
                        AutoSize = true,
                        Padding = new Padding(6)
                    };
                    flowAlternatives.Controls.Add(lblNone);
                    return;
                }

                foreach (DataRow r in dt.Rows)
                {
                    int altID = Convert.ToInt32(r["ProductID"]);
                    string altName = r["ProductName"]?.ToString() ?? "";
                    decimal altPrice = Convert.ToDecimal(r["SalePrice"]);
                    string altShelf = r["ShelfLocation"]?.ToString() ?? "";
                    decimal altStock = Convert.ToDecimal(r["TotalStock"]);
                    decimal diff = currentPrice - altPrice;

                    flowAlternatives.Controls.Add(CreateAlternativeCard(altID, altName, altPrice, altShelf, altStock, diff));
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPriceChecker.LoadAlternatives", ex);
            }
        }

        private Control CreateAlternativeCard(int id, string name, decimal price, string shelf, decimal stock, decimal diff)
        {
            var card = new Panel
            {
                Size = new Size(195, 76),
                BackColor = Color.FromArgb(48, 26, 96),
                Margin = new Padding(0, 0, 10, 4),
                Padding = new Padding(6),
                Cursor = Cursors.Hand
            };

            EventHandler onClick = (s, e) =>
            {
                var dr = ProductDAL.GetByID(id);
                if (dr != null) DisplayProduct(dr);
            };

            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(70, 38, 140);
            card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(48, 26, 96);
            card.Click += onClick;

            var lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 22,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            lblName.Click += onClick;

            var lblP = new Label
            {
                Text = $"{price:N2} ج.م",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Dock = DockStyle.Top,
                Height = 20,
                Cursor = Cursors.Hand
            };
            lblP.Click += onClick;

            string stockText = stock > 0 ? "متوفر 🟢" : "غير متوفر 🔴";
            if (diff > 0.01m) stockText += $" | أوفر {diff:N1} ج";

            var lblLoc = new Label
            {
                Text = stockText,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(216, 180, 254),
                Dock = DockStyle.Bottom,
                Height = 16,
                Cursor = Cursors.Hand
            };
            lblLoc.Click += onClick;

            card.Controls.Add(lblLoc);
            card.Controls.Add(lblP);
            card.Controls.Add(lblName);

            return card;
        }

        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;
            if (_isFullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                btnToggleFullscreen.Text = "🗗 نافذة";
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.Size = new Size(1150, 760);
                this.CenterToScreen();
                btnToggleFullscreen.Text = "🗖 ملء الشاشة";
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && _scannerBuffer.Length > 0)
            {
                string scanned = _scannerBuffer.ToString().Trim();
                _scannerBuffer.Clear();
                if (!string.IsNullOrEmpty(scanned))
                {
                    ProcessBarcodeSearch(scanned);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar))
            {
                double ms = (DateTime.Now - _lastScannerChar).TotalMilliseconds;
                _lastScannerChar = DateTime.Now;

                if (ms > 150 && !(this.ActiveControl == txtManualSearch))
                {
                    _scannerBuffer.Clear();
                }

                if (!(this.ActiveControl == txtManualSearch))
                {
                    _scannerBuffer.Append(e.KeyChar);
                }
            }
            base.OnKeyPress(e);
        }

        private void FrmPriceChecker_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (pnlProductDetails.Visible)
                {
                    SwitchToStandby();
                    e.Handled = true;
                }
                else
                {
                    this.Close();
                }
            }
        }
    }
}

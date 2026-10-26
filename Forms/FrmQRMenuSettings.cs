using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.Services;

namespace ChickenDist.Forms
{
    public class FrmQRMenuSettings : Form
    {
        // Branding Controls
        private TextBox txtMenuTitle;
        private TextBox txtMenuSubtitle;
        private TextBox txtWiFiSSID;
        private TextBox txtWiFiPass;
        private PictureBox picLogo;
        private PictureBox picBanner;
        private Button btnChooseLogo;
        private Button btnClearLogo;
        private Button btnChooseBanner;
        private Button btnClearBanner;
        private RadioButton rbMenuOnly;
        private RadioButton rbTableOrders;

        // Table Standee Controls
        private NumericUpDown nudTableNumber;
        private TextBox txtCustomTableName;
        private TextBox txtGeneratedUrl;
        private PictureBox picStandeePreview;
        private Button btnCopyUrl;
        private Button btnOpenBrowser;
        private Button btnPrintSingleCard;
        private Button btnPrintBatchCards;
        private Button btnSaveCardImage;
        private NumericUpDown nudBatchFrom;
        private NumericUpDown nudBatchTo;

        // Footer & Sync
        private Button btnSaveAndSync;
        private Button btnClose;
        private Label lblSyncStatus;

        // Printing helper
        private int _currentPrintingTableIndex = 0;
        private List<string> _batchTableNames = new List<string>();

        public FrmQRMenuSettings()
        {
            InitializeComponent();
            LoadCurrentSettings();
            UpdateStandeePreview();
        }

        private void InitializeComponent()
        {
            this.Text = "إعدادات منيو المطاعم وباركود الطاولات (Digital QR Table Menu)";
            this.Size = new Size(1060, 750);
            this.MinimumSize = new Size(980, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblHeaderTitle = new Label
            {
                Text = "قائمة الطعام الرقمية وباركود الطاولات (Digital QR Table Menu)",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 158, 11),
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblHeaderSub = new Label
            {
                Text = "خصص هوية مطعمك (اللوجو، البانر، الواي فاي)، واستخرج كروت وستاندات الطاولات الاحترافية بباركود المنيو المتزامن مع الأسعار لحظياً.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 22
            };

            pnlHeader.Controls.Add(lblHeaderSub);
            pnlHeader.Controls.Add(lblHeaderTitle);
            this.Controls.Add(pnlHeader);

            // 2. Footer Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(24, 33, 53),
                Padding = new Padding(15, 10, 15, 10)
            };

            btnSaveAndSync = new Button
            {
                Text = "حفظ الإعدادات ومزامنة المنيو مع السحابة فوراً",
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveAndSync.FlatAppearance.BorderSize = 0;
            btnSaveAndSync.Click += async (s, e) => await SaveAndSyncSettingsAsync();

            lblSyncStatus = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153)
            };

            btnClose = new Button
            {
                Text = "إغلاق",
                Dock = DockStyle.Right,
                Width = 100,
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(lblSyncStatus);
            pnlFooter.Controls.Add(btnSaveAndSync);
            pnlFooter.Controls.Add(btnClose);
            this.Controls.Add(pnlFooter);

            // 3. Main TabControl
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Point(16, 6)
            };

            var tabBranding = new TabPage("هوية ومظهر المنيو (Branding)")
            {
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15)
            };

            var tabStandees = new TabPage("باركود وطباعة ستاندات الطاولات (Table QR)")
            {
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15)
            };

            BuildBrandingTab(tabBranding);
            BuildStandeesTab(tabStandees);

            tabControl.TabPages.Add(tabBranding);
            tabControl.TabPages.Add(tabStandees);
            this.Controls.Add(tabControl);
            pnlHeader.SendToBack();
        }

        private void BuildBrandingTab(TabPage tab)
        {
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            int y = 10;

            // الاسم والعنوان
            var lblT = new Label { Text = "اسم المطعم / المنشأة في المنيو:", Location = new Point(20, y), AutoSize = true, ForeColor = Color.FromArgb(226, 232, 240) };
            pnlMain.Controls.Add(lblT);
            txtMenuTitle = new TextBox { Location = new Point(20, y + 25), Width = 450, Font = new Font("Segoe UI", 10.5f), BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            pnlMain.Controls.Add(txtMenuTitle);

            var lblSub = new Label { Text = "الوصف الترحيبي / السلوجان:", Location = new Point(500, y), AutoSize = true, ForeColor = Color.FromArgb(226, 232, 240) };
            pnlMain.Controls.Add(lblSub);
            txtMenuSubtitle = new TextBox { Location = new Point(500, y + 25), Width = 450, Font = new Font("Segoe UI", 10.5f), BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            pnlMain.Controls.Add(txtMenuSubtitle);

            y += 75;

            // بيانات الواي فاي
            var grpWifi = new GroupBox
            {
                Text = "بيانات شبكة الواي فاي للزبائن (تظهر تلقائياً على كارت الطاولة لمسح المنيو بسهولة)",
                Location = new Point(20, y),
                Size = new Size(930, 85),
                ForeColor = Color.FromArgb(96, 165, 250),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var lblSsid = new Label { Text = "اسم الشبكة (Wi-Fi SSID):", Location = new Point(20, 28), AutoSize = true, ForeColor = Color.White };
            txtWiFiSSID = new TextBox { Location = new Point(190, 25), Width = 250, Font = new Font("Segoe UI", 10f), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            var lblPass = new Label { Text = "كلمة المرور (Password):", Location = new Point(480, 28), AutoSize = true, ForeColor = Color.White };
            txtWiFiPass = new TextBox { Location = new Point(640, 25), Width = 250, Font = new Font("Segoe UI", 10f), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            grpWifi.Controls.Add(lblSsid);
            grpWifi.Controls.Add(txtWiFiSSID);
            grpWifi.Controls.Add(lblPass);
            grpWifi.Controls.Add(txtWiFiPass);
            pnlMain.Controls.Add(grpWifi);

            y += 100;

            // نمط العرض
            var grpMode = new GroupBox
            {
                Text = "نمط عرض المنيو عند مسح الباركود",
                Location = new Point(20, y),
                Size = new Size(930, 75),
                ForeColor = Color.FromArgb(245, 158, 11),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            rbMenuOnly = new RadioButton
            {
                Text = "منيو رقمي فقط (عرض قائمة الوجبات والمشروبات والأسعار - بدون سلة أو خيارات توصيل)",
                Location = new Point(20, 28),
                AutoSize = true,
                ForeColor = Color.White,
                Checked = true
            };

            rbTableOrders = new RadioButton
            {
                Text = "منيو تفاعلي مع إمكانية إرسال الطلب من الطاولة مباشرة إلى الكاشير",
                Location = new Point(520, 28),
                AutoSize = true,
                ForeColor = Color.White
            };

            grpMode.Controls.Add(rbMenuOnly);
            grpMode.Controls.Add(rbTableOrders);
            pnlMain.Controls.Add(grpMode);

            y += 90;

            // اللوجو والبانر
            var grpMedia = new GroupBox
            {
                Text = "صور وهوية المطعم (اللوجو وبانر الغلاف الترحيبي)",
                Location = new Point(20, y),
                Size = new Size(930, 230),
                ForeColor = Color.FromArgb(52, 211, 153),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            // لوجو المطعم
            var lblLogoTitle = new Label { Text = "لوجو المطعم (أيقونة المنيو):", Location = new Point(20, 28), AutoSize = true, ForeColor = Color.White };
            picLogo = new PictureBox
            {
                Location = new Point(20, 52),
                Size = new Size(130, 130),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            btnChooseLogo = new Button
            {
                Text = "اختيار لوجو...",
                Location = new Point(20, 188),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChooseLogo.FlatAppearance.BorderSize = 0;
            btnChooseLogo.Click += (s, e) => ChooseLogoImage();

            btnClearLogo = new Button
            {
                Text = "إزالة",
                Location = new Point(105, 188),
                Size = new Size(45, 28),
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearLogo.FlatAppearance.BorderSize = 0;
            btnClearLogo.Click += (s, e) =>
            {
                picLogo.Image = null;
                AppConfig.Menu_LogoPath = "";
                UpdateStandeePreview();
            };

            // بانر الغلاف
            var lblBannerTitle = new Label { Text = "بانر الغلاف الترحيبي (صورة عريضة أعلى المنيو):", Location = new Point(220, 28), AutoSize = true, ForeColor = Color.White };
            picBanner = new PictureBox
            {
                Location = new Point(220, 52),
                Size = new Size(680, 130),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            btnChooseBanner = new Button
            {
                Text = "رفع بانر غلاف جذاب...",
                Location = new Point(220, 188),
                Size = new Size(150, 28),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChooseBanner.FlatAppearance.BorderSize = 0;
            btnChooseBanner.Click += (s, e) => ChooseBannerImage();

            btnClearBanner = new Button
            {
                Text = "إزالة البانر",
                Location = new Point(380, 188),
                Size = new Size(90, 28),
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearBanner.FlatAppearance.BorderSize = 0;
            btnClearBanner.Click += (s, e) =>
            {
                picBanner.Image = null;
                AppConfig.Menu_BannerPath = "";
            };

            grpMedia.Controls.Add(lblLogoTitle);
            grpMedia.Controls.Add(picLogo);
            grpMedia.Controls.Add(btnChooseLogo);
            grpMedia.Controls.Add(btnClearLogo);
            grpMedia.Controls.Add(lblBannerTitle);
            grpMedia.Controls.Add(picBanner);
            grpMedia.Controls.Add(btnChooseBanner);
            grpMedia.Controls.Add(btnClearBanner);
            pnlMain.Controls.Add(grpMedia);

            tab.Controls.Add(pnlMain);
        }

        private void BuildStandeesTab(TabPage tab)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 460,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            // اليمين: خيارات الطاولة وتوليد الباركود
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15),
                AutoScroll = true
            };

            int y = 10;

            var lblTNum = new Label { Text = "رقم الطاولة الحالية:", Location = new Point(15, y), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            pnlLeft.Controls.Add(lblTNum);
            nudTableNumber = new NumericUpDown
            {
                Location = new Point(150, y - 3),
                Width = 80,
                Minimum = 1,
                Maximum = 500,
                Value = 1,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(245, 158, 11)
            };
            nudTableNumber.ValueChanged += (s, e) => UpdateStandeePreview();
            pnlLeft.Controls.Add(nudTableNumber);

            var lblCustT = new Label { Text = "أو اسم مخصص (مثل VIP 1):", Location = new Point(245, y), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184) };
            pnlLeft.Controls.Add(lblCustT);
            txtCustomTableName = new TextBox
            {
                Location = new Point(15, y + 35),
                Width = 410,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = ""
            };
            txtCustomTableName.TextChanged += (s, e) => UpdateStandeePreview();
            pnlLeft.Controls.Add(txtCustomTableName);

            y += 75;

            var lblUrl = new Label { Text = "رابط المنيو الرقمي للطاولة:", Location = new Point(15, y), AutoSize = true, ForeColor = Color.FromArgb(96, 165, 250) };
            pnlLeft.Controls.Add(lblUrl);
            txtGeneratedUrl = new TextBox
            {
                Location = new Point(15, y + 24),
                Width = 410,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.FromArgb(24, 33, 53),
                ForeColor = Color.FromArgb(148, 163, 184),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlLeft.Controls.Add(txtGeneratedUrl);

            y += 60;

            btnCopyUrl = new Button
            {
                Text = "نسخ الرابط",
                Location = new Point(15, y),
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopyUrl.FlatAppearance.BorderSize = 0;
            btnCopyUrl.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtGeneratedUrl.Text))
                {
                    Clipboard.SetText(txtGeneratedUrl.Text);
                    MessageBox.Show("تم نسخ رابط الطاولة بنجاح!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            pnlLeft.Controls.Add(btnCopyUrl);

            btnOpenBrowser = new Button
            {
                Text = "تجربة المنيو بالمتصفح",
                Location = new Point(155, y),
                Size = new Size(180, 32),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOpenBrowser.FlatAppearance.BorderSize = 0;
            btnOpenBrowser.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtGeneratedUrl.Text))
                {
                    try { System.Diagnostics.Process.Start(txtGeneratedUrl.Text); } catch { }
                }
            };
            pnlLeft.Controls.Add(btnOpenBrowser);

            y += 50;

            // كرت إجراءات الطباعة
            var grpPrint = new GroupBox
            {
                Text = "طباعة كروت وستاندات الطاولات (Acrylic Stand / Tent Card)",
                Location = new Point(15, y),
                Size = new Size(410, 240),
                ForeColor = Color.FromArgb(245, 158, 11),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            btnPrintSingleCard = new Button
            {
                Text = "طباعة كارت هذه الطاولة الحالية",
                Location = new Point(15, 30),
                Size = new Size(380, 38),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPrintSingleCard.FlatAppearance.BorderSize = 0;
            btnPrintSingleCard.Click += (s, e) => PrintSingleStandee();
            grpPrint.Controls.Add(btnPrintSingleCard);

            btnSaveCardImage = new Button
            {
                Text = "حفظ الكارت كصورة عالية الدقة (PNG)",
                Location = new Point(15, 76),
                Size = new Size(380, 34),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveCardImage.FlatAppearance.BorderSize = 0;
            btnSaveCardImage.Click += (s, e) => SaveStandeeAsImage();
            grpPrint.Controls.Add(btnSaveCardImage);

            // طباعة دفعة
            var lblBatch = new Label { Text = "طباعة دفعة طاولات متتالية:", Location = new Point(15, 125), AutoSize = true, ForeColor = Color.White };
            grpPrint.Controls.Add(lblBatch);

            var lblFrom = new Label { Text = "من طاولة:", Location = new Point(15, 153), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184) };
            grpPrint.Controls.Add(lblFrom);
            nudBatchFrom = new NumericUpDown { Location = new Point(80, 150), Width = 60, Minimum = 1, Maximum = 500, Value = 1, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White };
            grpPrint.Controls.Add(nudBatchFrom);

            var lblTo = new Label { Text = "إلى طاولة:", Location = new Point(160, 153), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184) };
            grpPrint.Controls.Add(lblTo);
            nudBatchTo = new NumericUpDown { Location = new Point(225, 150), Width = 60, Minimum = 1, Maximum = 500, Value = 10, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White };
            grpPrint.Controls.Add(nudBatchTo);

            btnPrintBatchCards = new Button
            {
                Text = "طباعة المجموعة كاملة",
                Location = new Point(15, 188),
                Size = new Size(380, 36),
                BackColor = Color.FromArgb(139, 92, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPrintBatchCards.FlatAppearance.BorderSize = 0;
            btnPrintBatchCards.Click += (s, e) => PrintBatchStandees();
            grpPrint.Controls.Add(btnPrintBatchCards);

            pnlLeft.Controls.Add(grpPrint);
            split.Panel1.Controls.Add(pnlLeft);

            // اليسار: معاينة الكارت الحقيقي
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 15, 30),
                Padding = new Padding(20)
            };

            var lblPrevTitle = new Label
            {
                Text = "معاينة كارت الطاولة الحي (Standard Acrylic Standee Preview):",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 158, 11)
            };
            pnlRight.Controls.Add(lblPrevTitle);

            picStandeePreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(10, 15, 30)
            };
            pnlRight.Controls.Add(picStandeePreview);

            split.Panel2.Controls.Add(pnlRight);
            tab.Controls.Add(split);
        }

        private void LoadCurrentSettings()
        {
            txtMenuTitle.Text = AppConfig.Menu_Title;
            if (string.IsNullOrEmpty(txtMenuTitle.Text)) txtMenuTitle.Text = AppConfig.CompanyName;
            txtMenuSubtitle.Text = AppConfig.Menu_Subtitle;
            txtWiFiSSID.Text = AppConfig.Menu_WiFiSSID;
            txtWiFiPass.Text = AppConfig.Menu_WiFiPassword;

            if (AppConfig.Menu_IsMenuOnly) rbMenuOnly.Checked = true;
            else rbTableOrders.Checked = true;

            // Load logo
            string logoPath = AppConfig.Menu_LogoPath;
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try { picLogo.Image = Image.FromFile(logoPath); } catch { }
            }
            else
            {
                var compLogo = Theme.GetCompanyLogo();
                if (compLogo != null) picLogo.Image = compLogo;
            }

            // Load banner
            string bannerPath = AppConfig.Menu_BannerPath;
            if (!string.IsNullOrEmpty(bannerPath) && File.Exists(bannerPath))
            {
                try { picBanner.Image = Image.FromFile(bannerPath); } catch { }
            }
        }

        private string GetEffectiveTableString()
        {
            string custom = txtCustomTableName?.Text?.Trim();
            if (!string.IsNullOrEmpty(custom)) return custom;
            return nudTableNumber != null ? nudTableNumber.Value.ToString() : "1";
        }

        private string GenerateTableMenuUrl(string tableStr)
        {
            string projId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
            if (string.IsNullOrEmpty(projId)) projId = "checkin-192ab";

            string mode = rbMenuOnly != null && rbMenuOnly.Checked ? "menu" : "table";
            return $"https://{projId}.web.app/store.html?mode={mode}&table={Uri.EscapeDataString(tableStr)}";
        }

        private void UpdateStandeePreview()
        {
            string tableStr = GetEffectiveTableString();
            string url = GenerateTableMenuUrl(tableStr);
            if (txtGeneratedUrl != null) txtGeneratedUrl.Text = url;

            Bitmap standeeBmp = GenerateStandeeBitmap(tableStr, url, 600, 850);
            if (picStandeePreview != null)
            {
                var old = picStandeePreview.Image;
                picStandeePreview.Image = standeeBmp;
                if (old != null) old.Dispose();
            }
        }

        /// <summary>
        /// توليد صورة كارت الطاولة / الستاند الأكريليك بدقة وتصميم فندقي راقي
        /// </summary>
        private Bitmap GenerateStandeeBitmap(string tableStr, string url, int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 1. الخلفية الفاخرة (Dark Navy Luxury Gradient)
                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, width, height),
                    Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), 90f))
                {
                    g.FillRectangle(brush, 0, 0, width, height);
                }

                // 2. إطار داخلي ذهبي أنيق
                using (var penGold = new Pen(Color.FromArgb(217, 119, 6), 3f))
                {
                    g.DrawRectangle(penGold, 18, 18, width - 36, height - 36);
                }
                using (var penGoldThin = new Pen(Color.FromArgb(245, 158, 11), 1f))
                {
                    g.DrawRectangle(penGoldThin, 24, 24, width - 48, height - 48);
                }

                int y = 45;

                // 3. اللوجو
                Image logo = picLogo?.Image;
                if (logo != null)
                {
                    int logoSize = 80;
                    int logoX = (width - logoSize) / 2;
                    g.DrawImage(logo, new Rectangle(logoX, y, logoSize, logoSize));
                    y += logoSize + 12;
                }
                else
                {
                    y += 10;
                }

                // 4. اسم المطعم
                string title = !string.IsNullOrEmpty(txtMenuTitle?.Text) ? txtMenuTitle.Text : AppConfig.CompanyName;
                using (var fTitle = new Font("Segoe UI", 18f, FontStyle.Bold))
                using (var bTitle = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(title, fTitle, bTitle, new RectangleF(30, y, width - 60, 34), sf);
                }
                y += 38;

                // 5. العنوان الفرعي / نوع القائمة
                using (var fSub = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (var bGold = new SolidBrush(Color.FromArgb(245, 158, 11)))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("قائمة الطعام والأسعار الرقمية | Digital Menu", fSub, bGold, new RectangleF(30, y, width - 60, 24), sf);
                }
                y += 30;

                // 6. شارة رقم الطاولة (Table Badge)
                string tableLabel = $"طاولة: {tableStr}";
                int badgeW = 220;
                int badgeH = 40;
                int badgeX = (width - badgeW) / 2;
                using (var brushBadge = new SolidBrush(Color.FromArgb(245, 158, 11)))
                {
                    g.FillRectangle(brushBadge, badgeX, y, badgeW, badgeH);
                }
                using (var fBadge = new Font("Segoe UI", 13f, FontStyle.Bold))
                using (var bBadgeText = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(tableLabel, fBadge, bBadgeText, new RectangleF(badgeX, y, badgeW, badgeH), sf);
                }
                y += badgeH + 20;

                // 7. الباركود عالي الدقة QR Code
                int qrBoxSize = 250;
                int qrX = (width - qrBoxSize) / 2;

                // لوحة بيضاء خلف الباركود لسهولة المسح بالكاميرا
                using (var bWhite = new SolidBrush(Color.White))
                {
                    g.FillRectangle(bWhite, qrX - 8, y - 8, qrBoxSize + 16, qrBoxSize + 16);
                }

                Bitmap qrBmp = QrCodeHelper.Generate(url, 8);
                if (qrBmp != null)
                {
                    g.DrawImage(qrBmp, new Rectangle(qrX, y, qrBoxSize, qrBoxSize));
                }
                y += qrBoxSize + 25;

                // 8. إرشادات المسح
                using (var fScan = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                using (var bScan = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("امسح الكود عبر كاميرا هاتفك لتصفح المنيو", fScan, bScan, new RectangleF(30, y, width - 60, 26), sf);
                }
                y += 30;

                // 9. بطاقة الواي فاي (Wi-Fi Credentials)
                string ssid = txtWiFiSSID?.Text?.Trim();
                string pass = txtWiFiPass?.Text?.Trim();
                if (!string.IsNullOrEmpty(ssid))
                {
                    int wifiBoxW = width - 80;
                    int wifiBoxH = 46;
                    int wifiBoxX = 40;
                    using (var bWifiBg = new SolidBrush(Color.FromArgb(30, 41, 59)))
                    using (var pWifiBorder = new Pen(Color.FromArgb(59, 130, 246), 1.5f))
                    {
                        g.FillRectangle(bWifiBg, wifiBoxX, y, wifiBoxW, wifiBoxH);
                        g.DrawRectangle(pWifiBorder, wifiBoxX, y, wifiBoxW, wifiBoxH);
                    }

                    string wifiText = $"واي فاي: {ssid}   |   كلمة السر: {(string.IsNullOrEmpty(pass) ? "مفتوحة" : pass)}";
                    using (var fWifi = new Font("Segoe UI", 10f, FontStyle.Bold))
                    using (var bWifi = new SolidBrush(Color.FromArgb(147, 197, 253)))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(wifiText, fWifi, bWifi, new RectangleF(wifiBoxX, y, wifiBoxW, wifiBoxH), sf);
                    }
                    y += wifiBoxH + 15;
                }

                // 10. تذييل الترحيب
                using (var fFoot = new Font("Segoe UI", 9.5f, FontStyle.Italic))
                using (var bFoot = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("نتمنى لكم أطيب الأوقات ووجبة شهية", fFoot, bFoot, new RectangleF(30, height - 55, width - 60, 25), sf);
                }
            }
            return bmp;
        }

        private void ChooseLogoImage()
        {
            using (var ofd = new OpenFileDialog
            {
                Title = "اختر صورة لوجو المطعم",
                Filter = "صور اللوجو (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|جميع الملفات (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        AppConfig.Menu_LogoPath = ofd.FileName;
                        picLogo.Image = Image.FromFile(ofd.FileName);
                        UpdateStandeePreview();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("خطأ في قراءة الصورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void ChooseBannerImage()
        {
            using (var ofd = new OpenFileDialog
            {
                Title = "اختر صورة بانر الغلاف الترحيبي",
                Filter = "صور البانر (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|جميع الملفات (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        AppConfig.Menu_BannerPath = ofd.FileName;
                        picBanner.Image = Image.FromFile(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("خطأ في قراءة الصورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void PrintSingleStandee()
        {
            try
            {
                string tableStr = GetEffectiveTableString();
                string url = GenerateTableMenuUrl(tableStr);

                var pd = new PrintDocument();
                pd.DocumentName = $"Standee_Table_{tableStr}";
                pd.PrintPage += (s, e) =>
                {
                    using (Bitmap bmp = GenerateStandeeBitmap(tableStr, url, 600, 850))
                    {
                        // تكييف مقاس الكارت ليملأ الصفحة بمحاذاة ممتازة
                        int pw = e.MarginBounds.Width;
                        int ph = e.MarginBounds.Height;
                        e.Graphics.DrawImage(bmp, e.MarginBounds.Left, e.MarginBounds.Top, pw, ph);
                    }
                };

                using (var ppd = new PrintPreviewDialog { Document = pd, Width = 800, Height = 650 })
                {
                    ppd.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintBatchStandees()
        {
            int from = (int)nudBatchFrom.Value;
            int to = (int)nudBatchTo.Value;

            if (from > to)
            {
                MessageBox.Show("رقم البداية أكبر من رقم النهاية!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _batchTableNames.Clear();
            for (int i = from; i <= to; i++)
            {
                _batchTableNames.Add(i.ToString());
            }

            _currentPrintingTableIndex = 0;

            try
            {
                var pd = new PrintDocument();
                pd.DocumentName = $"Standees_Tables_{from}_to_{to}";
                pd.PrintPage += (s, e) =>
                {
                    if (_currentPrintingTableIndex < _batchTableNames.Count)
                    {
                        string currentTable = _batchTableNames[_currentPrintingTableIndex];
                        string url = GenerateTableMenuUrl(currentTable);

                        using (Bitmap bmp = GenerateStandeeBitmap(currentTable, url, 600, 850))
                        {
                            int pw = e.MarginBounds.Width;
                            int ph = e.MarginBounds.Height;
                            e.Graphics.DrawImage(bmp, e.MarginBounds.Left, e.MarginBounds.Top, pw, ph);
                        }

                        _currentPrintingTableIndex++;
                        e.HasMorePages = (_currentPrintingTableIndex < _batchTableNames.Count);
                    }
                    else
                    {
                        e.HasMorePages = false;
                    }
                };

                using (var ppd = new PrintPreviewDialog { Document = pd, Width = 800, Height = 650 })
                {
                    ppd.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء طباعة المجموعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveStandeeAsImage()
        {
            string tableStr = GetEffectiveTableString();
            string url = GenerateTableMenuUrl(tableStr);

            using (var sfd = new SaveFileDialog
            {
                Title = "حفظ كارت الطاولة كصورة",
                Filter = "PNG Image (*.png)|*.png",
                FileName = $"Table_{tableStr}_Menu_Standee.png"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (Bitmap bmp = GenerateStandeeBitmap(tableStr, url, 1200, 1700))
                        {
                            bmp.Save(sfd.FileName, ImageFormat.Png);
                        }
                        MessageBox.Show("تم حفظ كارت الطاولة بدقة فائقة بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("خطأ في حفظ الصورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async Task SaveAndSyncSettingsAsync()
        {
            btnSaveAndSync.Enabled = false;
            lblSyncStatus.ForeColor = Color.FromArgb(56, 189, 248);
            lblSyncStatus.Text = "جاري حفظ الإعدادات ورفع المنيو الرقمي للسحابة...";

            try
            {
                // حفظ في AppConfig
                AppConfig.Menu_Title = txtMenuTitle.Text.Trim();
                AppConfig.Menu_Subtitle = txtMenuSubtitle.Text.Trim();
                AppConfig.Menu_WiFiSSID = txtWiFiSSID.Text.Trim();
                AppConfig.Menu_WiFiPassword = txtWiFiPass.Text.Trim();
                AppConfig.Menu_IsMenuOnly = rbMenuOnly.Checked;
                AppConfig.Menu_AllowTableOrders = rbTableOrders.Checked;

                // تشغيل المزامنة إلى Firebase فوراً
                string projId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
                bool syncOk = await CloudSyncService.SyncStoreCatalogToFirebaseAsync(projId);

                if (syncOk)
                {
                    lblSyncStatus.ForeColor = Color.FromArgb(52, 211, 153);
                    lblSyncStatus.Text = "تم حفظ وتحديث المنيو السحابي بنجاح!";
                    MessageBox.Show("تم حفظ إعدادات المنيو ورفع الكتالوج بجميع الأسعار الحالية فوراً إلى السحابة بنجاح!", "نجاح المزامنة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblSyncStatus.ForeColor = Color.FromArgb(244, 63, 94);
                    lblSyncStatus.Text = "تم الحفظ محلياً وتعذر رفع السحابة (تأكد من الإنترنت)";
                }
            }
            catch (Exception ex)
            {
                lblSyncStatus.ForeColor = Color.FromArgb(244, 63, 94);
                lblSyncStatus.Text = "خطأ: " + ex.Message;
                MessageBox.Show("حدث خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSaveAndSync.Enabled = true;
            }
        }
    }
}

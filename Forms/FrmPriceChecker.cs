using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPriceChecker : Form
    {
        // UI Controls
        private Panel pnlHeader;
        private Label lblCompany;
        private Label lblClock;
        private Button btnToggleFullscreen;
        private Button btnClose;

        // Container Panels
        private Panel pnlContent;
        private Panel pnlStandby;
        private Panel pnlProductDetails;

        // Standby UI
        private TextBox txtBarcodeScan;
        private Label lblScanPrompt;
        private Label lblStandbySubtitle;
        private TextBox txtManualSearch;
        private Button btnManualSearch;

        // Product Details UI
        private Label lblBarcodeBadge;
        private Label lblCategoryBadge;
        private Label lblProductName;
        private Label lblEnglishName;
        private Label lblPriceMain;
        private Label lblOriginalPrice;
        private Label lblDiscountBadge;
        private Label lblStockStatus;
        private Label lblShelfLocation;
        private FlowLayoutPanel flowUnits;
        private FlowLayoutPanel flowAlternatives;
        private Label lblAlternativesTitle;
        private ProgressBar prgResetCountdown;
        private Label lblCountdownText;
        private Button btnScanAnother;

        // Timers
        private System.Windows.Forms.Timer _clockTimer;
        private System.Windows.Forms.Timer _resetCountdownTimer;
        private int _countdownSeconds = 10;
        private bool _isFullscreen = false;

        public FrmPriceChecker(bool startFullscreen = false)
        {
            _isFullscreen = startFullscreen;
            InitializeComponent();
            SetupTimers();
            SwitchToStandby();
        }

        private void InitializeComponent()
        {
            this.Text = "🏷️ كشك فحص الأسعار والبدائل الذكية";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 14, 45);   // Deep Royal Indigo
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

            // ── 1. Top Header ──────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(49, 27, 102),   // Deep Purple
                Padding = new Padding(15, 10, 15, 10)
            };

            lblCompany = new Label
            {
                Text = $"🏷️ {AppConfig.CompanyName}  |  كشك فحص الأسعار والاستعلام الذكي",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),   // Gold
                AutoSize = true,
                Location = new Point(15, 18)
            };

            lblClock = new Label
            {
                Text = DateTime.Now.ToString("hh:mm:ss tt  |  yyyy/MM/dd"),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(216, 180, 254),   // Light Purple
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 420, 22)
            };

            btnToggleFullscreen = new Button
            {
                Text = _isFullscreen ? "🗗 نافذة" : "🗖 ملء الشاشة",
                Size = new Size(105, 36),
                BackColor = Color.FromArgb(79, 45, 143),   // Mid Purple
                ForeColor = Color.FromArgb(233, 213, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 210, 16),
                Cursor = Cursors.Hand
            };
            btnToggleFullscreen.FlatAppearance.BorderSize = 0;
            btnToggleFullscreen.Click += (s, e) => ToggleFullscreen();

            btnClose = new Button
            {
                Text = "❌ إغلاق",
                Size = new Size(85, 36),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 95, 16),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(lblCompany);
            pnlHeader.Controls.Add(lblClock);
            pnlHeader.Controls.Add(btnToggleFullscreen);
            pnlHeader.Controls.Add(btnClose);
            this.Controls.Add(pnlHeader);

            // ── 2. Content Container ──────────────────────────────────
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 14, 45),  // Deep Indigo
                Padding = new Padding(25)
            };
            this.Controls.Add(pnlContent);

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

        private void BuildStandbyUI()
        {
            pnlStandby = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var pnlCenter = new Panel
            {
                Size = new Size(720, 480),
                BackColor = Color.FromArgb(45, 28, 95),   // Rich Violet Card
                Anchor = AnchorStyles.None,
                Padding = new Padding(30)
            };
            pnlCenter.Location = new Point((pnlStandby.Width - pnlCenter.Width) / 2, (pnlStandby.Height - pnlCenter.Height) / 2);
            pnlStandby.Resize += (s, e) =>
            {
                pnlCenter.Location = new Point(Math.Max(10, (pnlStandby.Width - pnlCenter.Width) / 2), Math.Max(10, (pnlStandby.Height - pnlCenter.Height) / 2));
            };

            var lblIcon = new Label
            {
                Text = "🏷️ 📱 🔍",
                Font = new Font("Segoe UI", 36f),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 80
            };

            lblScanPrompt = new Label
            {
                Text = "مرحباً بكم! يرجى تمرير باركود الصنف أمام القارئ",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),   // Gold
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 45
            };

            lblStandbySubtitle = new Label
            {
                Text = "لعرض السعر الحالي، العروض والخصومات، أماكن الرفوف، والبدائل الأوفر المتاحة",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(216, 180, 254),  // Light Purple
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 35
            };

            var pnlSearchBox = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = Color.FromArgb(30, 18, 65),   // Darker Indigo
                Padding = new Padding(20, 15, 20, 15)
            };

            var lblSearchTitle = new Label
            {
                Text = "أو أدخل كود الصنف / الاسم يدوياً باللمس:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(233, 213, 255),  // Soft Lilac
                Dock = DockStyle.Top,
                Height = 25
            };

            txtManualSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                BackColor = Color.FromArgb(65, 40, 130),   // Mid Purple Input
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
                Text = "🔍 بحث",
                Dock = DockStyle.Left,
                Width = 110,
                BackColor = Color.FromArgb(234, 179, 8),    // Bright Gold
                ForeColor = Color.FromArgb(20, 14, 45),     // Dark text on gold
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

            var pnlInputRow = new Panel { Dock = DockStyle.Bottom, Height = 45 };
            pnlInputRow.Controls.Add(txtManualSearch);
            pnlInputRow.Controls.Add(btnManualSearch);

            pnlSearchBox.Controls.Add(pnlInputRow);
            pnlSearchBox.Controls.Add(lblSearchTitle);

            pnlCenter.Controls.Add(pnlSearchBox);
            pnlCenter.Controls.Add(lblStandbySubtitle);
            pnlCenter.Controls.Add(lblScanPrompt);
            pnlCenter.Controls.Add(lblIcon);

            pnlStandby.Controls.Add(pnlCenter);
            pnlContent.Controls.Add(pnlStandby);
        }

        private void BuildProductDetailsUI()
        {
            pnlProductDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Visible = false,
                AutoScroll = true
            };

            // Main Product Card Panel
            var pnlMainCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 290,
                BackColor = Color.FromArgb(45, 28, 95),    // Rich Violet Card
                Padding = new Padding(20)
            };

            // Badges Row
            var pnlBadges = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 35,
                FlowDirection = FlowDirection.RightToLeft
            };

            lblBarcodeBadge = CreateBadge("باركود: ----", Color.FromArgb(30, 18, 65), Color.FromArgb(216, 180, 254));
            lblCategoryBadge = CreateBadge("القسم: ----", Color.FromArgb(30, 18, 65), Color.FromArgb(250, 204, 21));
            lblShelfLocation = CreateBadge("📍 الرف: ----", Color.FromArgb(79, 45, 143), Color.FromArgb(253, 224, 71));
            lblStockStatus = CreateBadge("🟢 متوفر", Color.FromArgb(20, 83, 45), Color.FromArgb(134, 239, 172));

            pnlBadges.Controls.Add(lblBarcodeBadge);
            pnlBadges.Controls.Add(lblCategoryBadge);
            pnlBadges.Controls.Add(lblShelfLocation);
            pnlBadges.Controls.Add(lblStockStatus);

            // Names
            lblProductName = new Label
            {
                Text = "اسم الصنف",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 45,
                AutoEllipsis = true
            };

            lblEnglishName = new Label
            {
                Text = "English Name",
                Font = new Font("Segoe UI", 11f, FontStyle.Italic),
                ForeColor = Color.FromArgb(216, 180, 254),   // Light Purple
                Dock = DockStyle.Top,
                Height = 25,
                AutoEllipsis = true
            };

            // Price Row
            var pnlPriceRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(0, 5, 0, 5)
            };

            lblPriceMain = new Label
            {
                Text = "0.00 ج",
                Font = new Font("Segoe UI", 28f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),    // Gold Price
                AutoSize = true,
                Dock = DockStyle.Right
            };

            lblOriginalPrice = new Label
            {
                Text = "0.00 ج",
                Font = new Font("Segoe UI", 16f, FontStyle.Strikeout),
                ForeColor = Color.FromArgb(167, 139, 250),   // Muted Violet
                AutoSize = true,
                Dock = DockStyle.Right,
                Padding = new Padding(15, 12, 0, 0),
                Visible = false
            };

            lblDiscountBadge = new Label
            {
                Text = "🔥 وفر 0.00 ج (0%)",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(190, 18, 60),     // Crimson Red
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true,
                Dock = DockStyle.Right,
                Margin = new Padding(15, 10, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            pnlPriceRow.Controls.Add(lblDiscountBadge);
            pnlPriceRow.Controls.Add(lblOriginalPrice);
            pnlPriceRow.Controls.Add(lblPriceMain);

            // Units Row
            flowUnits = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoScroll = true
            };

            pnlMainCard.Controls.Add(flowUnits);
            pnlMainCard.Controls.Add(pnlPriceRow);
            pnlMainCard.Controls.Add(lblEnglishName);
            pnlMainCard.Controls.Add(lblProductName);
            pnlMainCard.Controls.Add(pnlBadges);

            // ── Alternatives Section ──────────────────────────────────
            var pnlAlternativesSection = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 15, 0, 0)
            };

            lblAlternativesTitle = new Label
            {
                Text = "✨ بدائل متوفرة وخيارات أوفر في نفس القسم:",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),    // Gold
                Dock = DockStyle.Top,
                Height = 32
            };

            flowAlternatives = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoScroll = true,
                Padding = new Padding(0, 5, 0, 0)
            };

            pnlAlternativesSection.Controls.Add(flowAlternatives);
            pnlAlternativesSection.Controls.Add(lblAlternativesTitle);

            // ── Bottom Countdown & Action Bar ──────────────────────────
            var pnlBottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.FromArgb(49, 27, 102),     // Deep Purple
                Padding = new Padding(15, 10, 15, 10)
            };

            btnScanAnother = new Button
            {
                Text = "🔍 فحص صنف آخر",
                Dock = DockStyle.Right,
                Width = 170,
                BackColor = Color.FromArgb(234, 179, 8),    // Gold
                ForeColor = Color.FromArgb(20, 14, 45),     // Dark text
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnScanAnother.FlatAppearance.BorderSize = 0;
            btnScanAnother.Click += (s, e) => SwitchToStandby();

            lblCountdownText = new Label
            {
                Text = "⏳ ستعود الشاشة تلقائياً لوضع الاستقبال بعد 10 ثوانٍ...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(216, 180, 254),  // Light Purple
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(0, 12, 0, 0)
            };

            prgResetCountdown = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Maximum = 100,
                Value = 100,
                Height = 10,
                Margin = new Padding(20, 18, 20, 18)
            };

            var pnlProg = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 12) };
            pnlProg.Controls.Add(prgResetCountdown);

            pnlBottomBar.Controls.Add(pnlProg);
            pnlBottomBar.Controls.Add(lblCountdownText);
            pnlBottomBar.Controls.Add(btnScanAnother);

            pnlProductDetails.Controls.Add(pnlAlternativesSection);
            pnlProductDetails.Controls.Add(pnlMainCard);
            pnlProductDetails.Controls.Add(pnlBottomBar);

            pnlContent.Controls.Add(pnlProductDetails);
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
            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) =>
            {
                lblClock.Text = DateTime.Now.ToString("hh:mm:ss tt  |  yyyy/MM/dd");
            };
            _clockTimer.Start();

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
                    lblCountdownText.Text = $"⏳ ستعود الشاشة تلقائياً بعد {_countdownSeconds} ثوانٍ...";
                    prgResetCountdown.Value = Math.Max(0, Math.Min(100, _countdownSeconds * 10));
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
                    // Fallback to name search
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
                    MessageBox.Show($"❌ لم يتم العثور على صنف مطابق للباركود أو الكود:\n({code})", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            catch {}

            // Populate Badges
            lblBarcodeBadge.Text = $"🏷️ كود: {prodCode}";
            lblCategoryBadge.Text = string.IsNullOrWhiteSpace(catName) ? "القسم: عام" : $"📂 {catName}";
            lblShelfLocation.Text = string.IsNullOrWhiteSpace(shelfLoc) ? "📍 الرف: غير محدد" : $"📍 مكان الرف: {shelfLoc}";

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

            // Populate Names
            lblProductName.Text = prodName;
            lblEnglishName.Text = string.IsNullOrWhiteSpace(engName) ? "" : engName;
            lblEnglishName.Visible = !string.IsNullOrWhiteSpace(engName);

            // Populate Price & Offers
            bool isOffer = dr.Table.Columns.Contains("IsOffer") && dr["IsOffer"] != DBNull.Value && Convert.ToBoolean(dr["IsOffer"]);
            decimal origPrice = dr.Table.Columns.Contains("OriginalPrice") && dr["OriginalPrice"] != DBNull.Value ? Convert.ToDecimal(dr["OriginalPrice"]) : 0m;

            if (isOffer && origPrice > salePrice)
            {
                decimal saved = origPrice - salePrice;
                decimal pct = (saved / origPrice) * 100m;
                lblPriceMain.Text = $"{salePrice:N2} ج";
                lblOriginalPrice.Text = $"{origPrice:N2} ج";
                lblOriginalPrice.Visible = true;
                lblDiscountBadge.Text = $"🔥 وفر {saved:N2} ج ({pct:N0}%)";
                lblDiscountBadge.Visible = true;
            }
            else
            {
                lblPriceMain.Text = $"{salePrice:N2} ج / {unit}";
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

            // Start Countdown
            _countdownSeconds = 12;
            prgResetCountdown.Value = 100;
            lblCountdownText.Text = "⏳ ستعود الشاشة تلقائياً بعد 12 ثانية...";
            _resetCountdownTimer.Start();

            this.ActiveControl = txtBarcodeScan;
            txtBarcodeScan.Focus();
        }

        private Control CreateUnitCard(string unitName, decimal price, decimal factor)
        {
            var pnl = new Panel
            {
                Size = new Size(160, 48),
                BackColor = Color.FromArgb(30, 18, 65),    // Dark Indigo
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(8, 4, 8, 4)
            };

            var lblU = new Label
            {
                Text = unitName,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(216, 180, 254),  // Light Purple
                Dock = DockStyle.Top,
                Height = 18
            };

            var lblP = new Label
            {
                Text = $"{price:N2} ج",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),   // Gold
                Dock = DockStyle.Bottom,
                Height = 22
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
                lblAlternativesTitle.ForeColor = Color.FromArgb(252, 165, 165);  // Soft Red
            }
            else
            {
                lblAlternativesTitle.Text = "✨ بدائل متوفرة وخيارات اقتصادية أوفر في نفس القسم:";
                lblAlternativesTitle.ForeColor = Color.FromArgb(250, 204, 21);   // Gold
            }

            try
            {
                string sql = @"
                    SELECT TOP 8 
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
                        Text = "لا توجد بدائل مسجلة في نفس القسم حالياً.",
                        Font = new Font("Segoe UI", 10.5f),
                        ForeColor = Color.FromArgb(216, 180, 254),
                        AutoSize = true,
                        Padding = new Padding(10)
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
                Size = new Size(245, 140),
                BackColor = Color.FromArgb(55, 35, 110),    // Mid Violet Card
                Margin = new Padding(0, 0, 12, 12),
                Padding = new Padding(12),
                Cursor = Cursors.Hand
            };

            EventHandler onClick = (s, e) =>
            {
                var dr = ProductDAL.GetByID(id);
                if (dr != null) DisplayProduct(dr);
            };

            // Highlight on hover
            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(79, 50, 150);
            card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(55, 35, 110);
            card.Click += onClick;

            var lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 42,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            lblName.Click += onClick;

            var lblP = new Label
            {
                Text = $"{price:N2} ج",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),   // Gold
                Dock = DockStyle.Top,
                Height = 26,
                Cursor = Cursors.Hand
            };
            lblP.Click += onClick;

            var pnlSub = new Panel { Dock = DockStyle.Fill };

            if (diff > 0.01m)
            {
                var lblDiff = new Label
                {
                    Text = $"🟢 أوفر بـ {diff:N2} ج",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(134, 239, 172),  // Bright Green
                    Dock = DockStyle.Top,
                    Height = 20
                };
                pnlSub.Controls.Add(lblDiff);
            }

            string stockText = stock > 0 ? "متوفر بالفرع 🟢" : "غير متوفر 🔴";
            string shelfInfo = string.IsNullOrWhiteSpace(shelf) ? stockText : $"📍 رف: {shelf} | {stockText}";

            var lblLoc = new Label
            {
                Text = shelfInfo,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(216, 180, 254),  // Light Purple
                Dock = DockStyle.Bottom,
                Height = 20
            };
            pnlSub.Controls.Add(lblLoc);

            card.Controls.Add(pnlSub);
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
                this.Size = new Size(1100, 720);
                this.CenterToScreen();
                btnToggleFullscreen.Text = "🗖 ملء الشاشة";
            }
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

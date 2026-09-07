using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;
using ChickenDist.Services;
using QRCoder;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة التحكم الشاملة في المتجر الإلكتروني للعملاء (Online Web Store Controls)
    /// تتيح تحديد فئة السعر (قطاعي/نصف جملة/جملة)، إظهار أو إخفاء رصيد المخزون،
    /// إظهار أو إخفاء أسعار البيع، وتحديد الأقسام المسموح بعرضها في المتجر.
    /// </summary>
    public class FrmOnlineStoreSettings : Form
    {
        private CheckBox chkStoreActive;
        private RadioButton rbRetail;
        private RadioButton rbSemiWholesale;
        private RadioButton rbWholesale;
        private CheckBox chkShowPrices;
        private CheckBox chkShowStockQty;
        private CheckedListBox clbCategories;
        private TextBox txtSearchCategory;
        private NumericUpDown nudMinimumOrder;
        private TextBox txtAnnouncement;
        private TextBox txtNotificationWhatsApp;
        private TextBox txtStoreUrl;
        private PictureBox picQR;
        private Button btnCopyUrl;
        private Button btnOpenStore;
        private Button btnSelectAllCats;
        private Button btnDeselectAllCats;
        private Button btnSaveAndSync;
        private Button btnClose;
        private Label lblSyncStatus;

        private DataTable _dtCategories;

        public FrmOnlineStoreSettings()
        {
            InitializeComponent();
            LoadCurrentSettings();
            GenerateQrCode();
        }

        private void InitializeComponent()
        {
            this.Text = "⚙️ لوحة التحكم في المتجر الإلكتروني للعملاء (Online Store)";
            this.Size = new Size(950, 750);
            this.MinimumSize = new Size(880, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.Font = new Font("Segoe UI", 9.5f);

            // 1. الشريط العلوي (Header)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblTitle = new Label
            {
                Text = "🌐 إعدادات وتحكم المتجر الإلكتروني للعملاء (Web Store)",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };

            var lblSubtitle = new Label
            {
                Text = "التحكم في فئات الأسعار، ظهور رصيد المخزن، خصوصية الأسعار، وتحديد الأقسام المعروضة للعملاء",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);

            // 2. الشريط السفلي للأزرار (Footer)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(16, 10, 16, 10)
            };

            btnSaveAndSync = new Button
            {
                Text = "💾 حفظ الإعدادات ومزامنة المتجر الآن",
                Size = new Size(250, 38),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnSaveAndSync.FlatAppearance.BorderSize = 0;
            btnSaveAndSync.Click += async (s, e) => await SaveSettingsAndSyncAsync();

            btnClose = new Button
            {
                Text = "إغلاق",
                Size = new Size(100, 38),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            lblSyncStatus = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(52, 211, 153),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlFooter.Controls.Add(btnSaveAndSync);
            pnlFooter.Controls.Add(lblSyncStatus);
            pnlFooter.Controls.Add(btnClose);

            // 3. المحتوى الرئيسي (Main Layout: 2 Columns)
            var tableMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(15, 23, 42)
            };
            tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f)); // اليمين: الإعدادات العامة والأسعار
            tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f)); // اليسار: الأقسام و QR Code

            // ===== العمود الأيمن =====
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(6),
                BackColor = Color.FromArgb(15, 23, 42)
            };

            // أ. تفعيل المتجر
            var pnlStoreActive = new Panel
            {
                Location = new Point(10, 6),
                Size = new Size(470, 40),
                BackColor = Color.FromArgb(24, 33, 53),
                Padding = new Padding(10, 8, 10, 8)
            };

            chkStoreActive = new CheckBox
            {
                Text = "تفعيل المتجر الإلكتروني واستقبال طلبات العملاء أونلاين",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                BackColor = Color.FromArgb(24, 33, 53),
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            pnlStoreActive.Controls.Add(chkStoreActive);
            pnlRight.Controls.Add(pnlStoreActive);

            // ب. فئة السعر المعروض للعملاء (Price Tier)
            var grpPriceTier = new GroupBox
            {
                Text = "🏷️ فئة السعر المعروض للعملاء في المتجر",
                Location = new Point(10, 52),
                Size = new Size(470, 120),
                ForeColor = Color.FromArgb(96, 165, 250),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(10)
            };

            rbRetail = new RadioButton
            {
                Text = "🔘 سعر القطاعي (سعر البيع العادي للمستهلك)",
                Location = new Point(15, 25),
                Size = new Size(440, 26),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Checked = true,
                Cursor = Cursors.Hand
            };

            rbSemiWholesale = new RadioButton
            {
                Text = "🔘 سعر نصف الجملة (للتجار الصغار ومحلات التجزئة)",
                Location = new Point(15, 55),
                Size = new Size(440, 26),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            rbWholesale = new RadioButton
            {
                Text = "🔘 سعر الجملة (أسعار كبار العملاء والتوزيع)",
                Location = new Point(15, 85),
                Size = new Size(440, 26),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            grpPriceTier.Controls.Add(rbRetail);
            grpPriceTier.Controls.Add(rbSemiWholesale);
            grpPriceTier.Controls.Add(rbWholesale);
            pnlRight.Controls.Add(grpPriceTier);

            // ج. خيارات الشفافية والعرض (Visibility Controls)
            var grpVisibility = new GroupBox
            {
                Text = "👁️ خيارات إظهار الأسعار ورصيد المخزون للعملاء",
                Location = new Point(10, 180),
                Size = new Size(470, 135),
                ForeColor = Color.FromArgb(96, 165, 250),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(10)
            };

            chkShowPrices = new CheckBox
            {
                Text = "عرض أسعار البيع للعملاء في الموقع\n(إذا أُلغي الخيار، يظهر 'السعر عند الطلب 📞')",
                Location = new Point(15, 25),
                Size = new Size(440, 44),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            chkShowStockQty = new CheckBox
            {
                Text = "إظهار رصيد الأصناف والكميات المتاحة في المخزن للعملاء\n(إذا أُلغي الخيار، يظهر 'متوفر للطلب ✅' دون كشف رصيد المخزن الحقيقي)",
                Location = new Point(15, 75),
                Size = new Size(440, 48),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            grpVisibility.Controls.Add(chkShowPrices);
            grpVisibility.Controls.Add(chkShowStockQty);
            pnlRight.Controls.Add(grpVisibility);

            // د. تفاصيل إضافية ورقم الواتساب والحد الأدنى
            var grpExtra = new GroupBox
            {
                Text = "📢 تفاصيل المتجر والتواصل",
                Location = new Point(10, 325),
                Size = new Size(470, 265),
                ForeColor = Color.FromArgb(96, 165, 250),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(10)
            };

            var lblMin = new Label
            {
                Text = "الحد الأدنى لقيمة الطلب (ج.م):",
                Location = new Point(290, 26),
                Size = new Size(170, 22),
                ForeColor = Color.FromArgb(241, 245, 249),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            nudMinimumOrder = new NumericUpDown
            {
                Location = new Point(15, 24),
                Size = new Size(270, 26),
                Maximum = 100000,
                DecimalPlaces = 2,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(52, 211, 153),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            var lblWa = new Label
            {
                Text = "رقم هاتف وواتساب المتجر للتواصل:",
                Location = new Point(240, 62),
                Size = new Size(220, 22),
                ForeColor = Color.FromArgb(241, 245, 249),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            txtNotificationWhatsApp = new TextBox
            {
                Location = new Point(15, 60),
                Size = new Size(220, 26),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblAnnounce = new Label
            {
                Text = "نص الإعلان الترويجي ورسالة الترحيب في أعلى الموقع:",
                Location = new Point(15, 98),
                Size = new Size(440, 22),
                ForeColor = Color.FromArgb(241, 245, 249),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            txtAnnouncement = new TextBox
            {
                Location = new Point(15, 122),
                Size = new Size(440, 52),
                Multiline = true,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblUrlTitle = new Label
            {
                Text = "رابط المتجر المباشر:",
                Location = new Point(340, 185),
                Size = new Size(115, 22),
                ForeColor = Color.FromArgb(241, 245, 249),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            txtStoreUrl = new TextBox
            {
                Location = new Point(15, 212),
                Size = new Size(260, 27),
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };

            btnCopyUrl = new Button
            {
                Text = "📋 نسخ",
                Location = new Point(282, 210),
                Size = new Size(78, 30),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopyUrl.FlatAppearance.BorderSize = 0;
            btnCopyUrl.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtStoreUrl.Text))
                {
                    Clipboard.SetText(txtStoreUrl.Text);
                    MessageBox.Show("تم نسخ رابط المتجر للحافظة بنجاح ✅", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnOpenStore = new Button
            {
                Text = "🌐 فتح",
                Location = new Point(366, 210),
                Size = new Size(88, 30),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOpenStore.FlatAppearance.BorderSize = 0;
            btnOpenStore.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtStoreUrl.Text))
                {
                    try { System.Diagnostics.Process.Start(txtStoreUrl.Text); } catch { }
                }
            };

            grpExtra.Controls.Add(lblMin);
            grpExtra.Controls.Add(nudMinimumOrder);
            grpExtra.Controls.Add(lblWa);
            grpExtra.Controls.Add(txtNotificationWhatsApp);
            grpExtra.Controls.Add(lblAnnounce);
            grpExtra.Controls.Add(txtAnnouncement);
            grpExtra.Controls.Add(lblUrlTitle);
            grpExtra.Controls.Add(txtStoreUrl);
            grpExtra.Controls.Add(btnCopyUrl);
            grpExtra.Controls.Add(btnOpenStore);
            pnlRight.Controls.Add(grpExtra);

            // ===== العمود الأيسر =====
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
                BackColor = Color.FromArgb(15, 23, 42)
            };

            // أ. تحديد الأقسام المسموح بظهورها في المتجر (Category Filter)
            var grpCategories = new GroupBox
            {
                Text = "📂 الأقسام المعروضة في المتجر (تحديد قسم معين أو استبعاده)",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(96, 165, 250),
                BackColor = Color.FromArgb(24, 33, 53),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(10)
            };

            var pnlCatToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(0, 0, 0, 6),
                BackColor = Color.FromArgb(24, 33, 53)
            };

            txtSearchCategory = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearchCategory.TextChanged += (s, e) => FilterCategoriesList();

            btnSelectAllCats = new Button
            {
                Text = "تحديد الكل",
                Dock = DockStyle.Left,
                Width = 85,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelectAllCats.FlatAppearance.BorderSize = 0;
            btnSelectAllCats.Click += (s, e) => SetAllCategoriesChecked(true);

            btnDeselectAllCats = new Button
            {
                Text = "إلغاء الكل",
                Dock = DockStyle.Left,
                Width = 85,
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeselectAllCats.FlatAppearance.BorderSize = 0;
            btnDeselectAllCats.Click += (s, e) => SetAllCategoriesChecked(false);

            pnlCatToolbar.Controls.Add(txtSearchCategory);
            pnlCatToolbar.Controls.Add(btnSelectAllCats);
            pnlCatToolbar.Controls.Add(btnDeselectAllCats);

            clbCategories = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                CheckOnClick = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            var pnlQR = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 160,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(18, 26, 43)
            };

            picQR = new PictureBox
            {
                Dock = DockStyle.Left,
                Width = 150,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            var lblQrInfo = new Label
            {
                Text = "📱 رمز الاستجابة السريع (QR Code)\nامسح الرمز بكاميرا الهاتف لفتح المتجر مباشرة، أو التقط لقطة شاشة لطباعته في المحل.",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(226, 232, 240),
                BackColor = Color.FromArgb(18, 26, 43),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlQR.Controls.Add(lblQrInfo);
            pnlQR.Controls.Add(picQR);

            grpCategories.Controls.Add(clbCategories);
            grpCategories.Controls.Add(pnlCatToolbar);
            grpCategories.Controls.Add(pnlQR);

            pnlLeft.Controls.Add(grpCategories);

            tableMain.Controls.Add(pnlRight, 0, 0);
            tableMain.Controls.Add(pnlLeft, 1, 0);

            this.Controls.Add(tableMain);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        private void LoadCurrentSettings()
        {
            chkStoreActive.Checked = AppConfig.Store_IsActive;

            string tier = AppConfig.Store_PriceTier;
            if (tier == "Wholesale") rbWholesale.Checked = true;
            else if (tier == "SemiWholesale") rbSemiWholesale.Checked = true;
            else rbRetail.Checked = true;

            chkShowPrices.Checked = AppConfig.Store_ShowPrices;
            chkShowStockQty.Checked = AppConfig.Store_ShowStockQty;

            nudMinimumOrder.Value = Math.Max(0, AppConfig.Store_MinimumOrder);
            txtAnnouncement.Text = AppConfig.Store_Announcement ?? "";
            string storePhone = AppConfig.Store_OrderNotificationWhatsApp;
            if (string.IsNullOrEmpty(storePhone)) storePhone = AppConfig.CompanyPhone;
            txtNotificationWhatsApp.Text = storePhone ?? "";

            string projectId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
            if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";
            txtStoreUrl.Text = $"https://{projectId}.web.app/store.html";

            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                _dtCategories = CategoryDAL.GetAllForStore();
                FilterCategoriesList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل قائمة الأقسام: " + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void FilterCategoriesList()
        {
            clbCategories.Items.Clear();
            if (_dtCategories == null) return;

            string search = txtSearchCategory.Text.Trim().ToLower();

            foreach (DataRow row in _dtCategories.Rows)
            {
                string catName = row["CategoryName"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(search) && !catName.ToLower().Contains(search))
                    continue;

                int pCount = row["ProductsCount"] != DBNull.Value ? Convert.ToInt32(row["ProductsCount"]) : 0;
                bool isShown = row["ShowInOnlineStore"] != DBNull.Value && Convert.ToBoolean(row["ShowInOnlineStore"]);
                int catId = Convert.ToInt32(row["CategoryID"]);

                var item = new CategoryCheckItem
                {
                    CategoryID = catId,
                    CategoryName = catName,
                    ProductsCount = pCount,
                    IsShown = isShown
                };

                clbCategories.Items.Add(item, isShown);
            }
        }

        private void SetAllCategoriesChecked(bool check)
        {
            for (int i = 0; i < clbCategories.Items.Count; i++)
            {
                clbCategories.SetItemChecked(i, check);
            }
        }

        private void GenerateQrCode()
        {
            try
            {
                string url = txtStoreUrl.Text.Trim();
                if (string.IsNullOrEmpty(url)) return;

                using (var gen = new QRCodeGenerator())
                {
                    var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
                    using (var qr = new QRCode(data))
                    {
                        var bmp = qr.GetGraphic(5, Color.Black, Color.White, true);
                        picQR.Image = bmp;
                    }
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task SaveSettingsAndSyncAsync()
        {
            btnSaveAndSync.Enabled = false;
            lblSyncStatus.ForeColor = Color.FromArgb(56, 189, 248);
            lblSyncStatus.Text = "⏳ جاري حفظ الإعدادات ومزامنة المتجر مع السحابة...";

            try
            {
                // 1. حفظ الإعدادات في AppConfig
                AppConfig.Store_IsActive = chkStoreActive.Checked;

                if (rbWholesale.Checked) AppConfig.Store_PriceTier = "Wholesale";
                else if (rbSemiWholesale.Checked) AppConfig.Store_PriceTier = "SemiWholesale";
                else AppConfig.Store_PriceTier = "Retail";

                AppConfig.Store_ShowPrices = chkShowPrices.Checked;
                AppConfig.Store_ShowStockQty = chkShowStockQty.Checked;
                AppConfig.Store_MinimumOrder = nudMinimumOrder.Value;
                AppConfig.Store_Announcement = txtAnnouncement.Text.Trim();
                string enteredPhone = txtNotificationWhatsApp.Text.Trim();
                AppConfig.Store_OrderNotificationWhatsApp = enteredPhone;
                if (!string.IsNullOrEmpty(enteredPhone) && string.IsNullOrEmpty(AppConfig.CompanyPhone1))
                {
                    AppConfig.CompanyPhone1 = enteredPhone;
                }

                // 2. حفظ رؤية الأقسام في قاعدة البيانات
                for (int i = 0; i < clbCategories.Items.Count; i++)
                {
                    if (clbCategories.Items[i] is CategoryCheckItem item)
                    {
                        bool isChecked = clbCategories.GetItemChecked(i);
                        CategoryDAL.SetStoreVisibility(item.CategoryID, isChecked);
                    }
                }

                // 3. مزامنة الكتالوج والإعدادات إلى Firebase فوراً
                bool syncOk = await CloudSyncService.SyncStoreCatalogToFirebaseAsync();

                if (syncOk)
                {
                    lblSyncStatus.ForeColor = Color.FromArgb(52, 211, 153);
                    lblSyncStatus.Text = "✅ تم حفظ الإعدادات ومزامنة المتجر مع السحابة بنجاح!";
                    MessageBox.Show("تم حفظ إعدادات المتجر الإلكتروني ومزامنة الكتالوج مع السحابة بنجاح 🔥", "نجاح المزامنة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblSyncStatus.ForeColor = Color.FromArgb(244, 63, 94);
                    lblSyncStatus.Text = "⚠️ تم حفظ الإعدادات محلياً، لكن تعذر الاتصال بـ Firebase";
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

        private class CategoryCheckItem
        {
            public int CategoryID { get; set; }
            public string CategoryName { get; set; }
            public int ProductsCount { get; set; }
            public bool IsShown { get; set; }

            public override string ToString()
            {
                return $"{CategoryName} ({ProductsCount} صنف)";
            }
        }
    }
}

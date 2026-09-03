using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.Services;

namespace ChickenDist.Forms
{
    /// <summary>
    /// الشاشة الموحدة الشاملة لتطبيق المالك (Web App) والتزامن اللحظي مع Firebase وإدارة نشر التطبيق (Deploy)
    /// </summary>
    public class FrmCloudSync : Form
    {
        private TextBox txtFirebaseProjectId;
        private Label lblLiveWebUrl;
        private CheckBox chkAutoSync;
        private ComboBox cboInterval;
        private Label lblSalesTotal, lblCashSales, lblCashboxBalance, lblLowStock, lblSyncStatus, lblLastSyncTime;
        private Label lblClientDebts, lblSupplierDebts, lblTodayNetProfit;
        private Button btnSyncNow, btnDeployFirebase, btnInstallTools, btnLoginFirebase, btnEnsureFiles, btnCopyUrl, btnOpenMobileApp, btnSave;
        private TextBox txtDeployLog;

        public FrmCloudSync()
        {
            CloudSyncService.EnsureMobileAppFiles();
            InitUI();
            LoadSettings();
            RefreshLiveStats();
        }

        private void InitUI()
        {
            this.Text = "📱 تطبيق المالك وخدمات السحاب (Firebase Realtime Cloud)";
            this.Size = new Size(1100, 820);
            this.MinimumSize = new Size(980, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== 1. Header Title =====
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitleText = new Label
            {
                Text = "📱 تطبيق المالك وخدمات السحاب (Firebase Realtime Cloud)",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };

            var lblSubText = new Label
            {
                Text = "متابعة وإدارة مزامنة بيانات وتقارير المحل لحظياً مع تطبيق المالك (Web App) ونشره على Firebase Hosting لكل عميل",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlTitle.Controls.Add(lblSubText);
            pnlTitle.Controls.Add(lblTitleText);

            // ===== 2. KPI Cards Panel (Compact & Clean) =====
            var pnlKPI = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 125,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(8, 4, 8, 4),
                BackColor = Color.FromArgb(18, 26, 43)
            };
            pnlKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblSalesTotal = MakeKPICard("مبيعات اليوم الكلية", "0.00 ج", Color.FromArgb(40, 140, 220), out Panel p1);
            lblTodayNetProfit = MakeKPICard("صافي الأرباح اليومية", "0.00 ج", Color.FromArgb(16, 185, 129), out Panel p2);
            lblCashboxBalance = MakeKPICard("رصيد الخزنة الحالي", "0.00 ج", Color.FromArgb(6, 182, 212), out Panel p3);
            lblLowStock = MakeKPICard("أصناف النواقص", "0 صنف", Color.FromArgb(244, 63, 94), out Panel p4);

            lblCashSales = MakeKPICard("مبيعات كاش / آجل", "0.00 | 0.00", Color.FromArgb(50, 160, 200), out Panel p5);
            lblClientDebts = MakeKPICard("ديون العملاء", "0.00 ج", Color.FromArgb(168, 85, 247), out Panel p6);
            lblSupplierDebts = MakeKPICard("مستحقات الموردين", "0.00 ج", Color.FromArgb(245, 158, 11), out Panel p7);
            lblSyncStatus = MakeKPICard("حالة المزامنة", "جاهز", Color.FromArgb(14, 165, 233), out Panel p8);

            pnlKPI.Controls.Add(p1, 0, 0); pnlKPI.Controls.Add(p2, 1, 0); pnlKPI.Controls.Add(p3, 2, 0); pnlKPI.Controls.Add(p4, 3, 0);
            pnlKPI.Controls.Add(p5, 0, 1); pnlKPI.Controls.Add(p6, 1, 1); pnlKPI.Controls.Add(p7, 2, 1); pnlKPI.Controls.Add(p8, 3, 1);

            // ===== 3. Action Buttons Panel (Dock = Top with 2 Organized Rows) =====
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 94,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 6, 10, 6),
                BackColor = Color.FromArgb(15, 23, 42)
            };

            // الصف الأول: عمليات المزامنة والنشر
            btnSyncNow = Theme.MakeButton("🔥 مزامنة ورفع البيانات لحظياً", Color.FromArgb(16, 185, 129));
            btnSyncNow.Size = new Size(220, 36);
            btnSyncNow.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSyncNow.Click += BtnSyncNow_Click;

            btnDeployFirebase = Theme.MakeButton("🚀 نشر تطبيق المالك (Deploy)", Color.FromArgb(234, 88, 12));
            btnDeployFirebase.Size = new Size(210, 36);
            btnDeployFirebase.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnDeployFirebase.Click += BtnDeployFirebase_Click;

            btnOpenMobileApp = Theme.MakeButton("📱 فتح تطبيق المالك", Color.FromArgb(147, 51, 234));
            btnOpenMobileApp.Size = new Size(160, 36);
            btnOpenMobileApp.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnOpenMobileApp.Click += BtnOpenMobileApp_Click;

            btnCopyUrl = Theme.MakeButton("📋 نسخ الرابط", Color.FromArgb(2, 132, 199));
            btnCopyUrl.Size = new Size(110, 36);
            btnCopyUrl.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnCopyUrl.Click += (s, e) =>
            {
                string u = lblLiveWebUrl.Text.Trim();
                if (!string.IsNullOrEmpty(u))
                {
                    Clipboard.SetText(u);
                    MessageBox.Show("✅ تم نسخ رابط تطبيق المالك إلى الحافظة بنجاح:\n" + u, "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnSave = Theme.MakeButton("💾 حفظ الإعدادات", Color.FromArgb(71, 85, 105));
            btnSave.Size = new Size(120, 36);
            btnSave.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;

            // الصف الثاني: أدوات التثبيت وتسجيل الدخول وإنشاء الملفات
            btnInstallTools = Theme.MakeButton("📦 تثبيت أداة Firebase Tools", Color.FromArgb(79, 70, 229));
            btnInstallTools.Size = new Size(220, 36);
            btnInstallTools.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnInstallTools.Click += BtnInstallFirebaseTools_Click;

            btnLoginFirebase = Theme.MakeButton("🔑 تسجيل دخول فيربيز (Firebase Login)", Color.FromArgb(13, 148, 136));
            btnLoginFirebase.Size = new Size(270, 36);
            btnLoginFirebase.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnLoginFirebase.Click += BtnLoginFirebase_Click;

            btnEnsureFiles = Theme.MakeButton("🔄 تهيئة ملفات firebase.json", Color.FromArgb(37, 99, 235));
            btnEnsureFiles.Size = new Size(210, 36);
            btnEnsureFiles.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnEnsureFiles.Click += BtnEnsureFiles_Click;

            pnlActions.Controls.AddRange(new Control[] {
                btnSyncNow, btnDeployFirebase, btnOpenMobileApp, btnCopyUrl, btnSave,
                btnInstallTools, btnLoginFirebase, btnEnsureFiles
            });

            // ===== 4. Middle Settings & Deploy Panel (Dock = Fill) =====
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                AutoScroll = true
            };

            var grpSettings = new GroupBox
            {
                Text = "⚙️ إعدادات حساب Firebase الخاص بالعميل والتزامن اللحظي",
                Dock = DockStyle.Top,
                Height = 230,
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Padding = new Padding(10)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(4)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var lblProject = new Label { Text = "معرّف مشروع Firebase للعميل (Project ID):", AutoSize = true, ForeColor = Color.White, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtFirebaseProjectId = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, RightToLeft = RightToLeft.No, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
            txtFirebaseProjectId.TextChanged += (s, e) => UpdateWebUrlLabel();

            var lblUrlTitle = new Label { Text = "رابط تطبيق المالك (Web App URL):", AutoSize = true, ForeColor = Color.White, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            lblLiveWebUrl = new Label { Text = "https://checkin-192ab.web.app", AutoSize = true, ForeColor = Color.FromArgb(56, 189, 248), Font = new Font("Segoe UI", 10.5f, FontStyle.Bold | FontStyle.Underline), Anchor = AnchorStyles.Right, Cursor = Cursors.Hand };
            lblLiveWebUrl.Click += (s, e) => BtnOpenMobileApp_Click(s, e);

            var lblAuto = new Label { Text = "المزامنة اللحظية المستمرة:", AutoSize = true, ForeColor = Color.White, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            chkAutoSync = new CheckBox { Text = "تفعيل الرفع اللحظي التلقائي مع كل فاتورة بيع أو شراء أو حركة نقدية وتقفيل وردية", AutoSize = true, ForeColor = Color.FromArgb(226, 232, 240), Checked = true, Font = new Font("Segoe UI", 9f, FontStyle.Regular) };

            var lblInt = new Label { Text = "معدل التحديث والـ Last Sync:", AutoSize = true, ForeColor = Color.White, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            
            var pnlIntRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            cboInterval = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f) };
            cboInterval.Items.AddRange(new object[] { "كل 15 ثانية (فوري)", "كل 30 ثانية", "كل دقيقة", "كل 5 دقائق" });
            cboInterval.SelectedIndex = 0;

            lblLastSyncTime = new Label { Text = "🕒 آخر تحديث: لم تتم بعد", AutoSize = true, ForeColor = Color.FromArgb(52, 211, 153), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Padding = new Padding(10, 4, 0, 0) };
            pnlIntRow.Controls.Add(cboInterval);
            pnlIntRow.Controls.Add(lblLastSyncTime);

            var lblCredTitle = new Label { Text = "بيانات دخول تطبيق المالك الموحدة:", AutoSize = true, ForeColor = Color.White, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            var pnlCredRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            var lblCredBadge = new Label 
            { 
                Text = "🔑 اسم المستخدم: pro    |    كلمة المرور: pro@2026", 
                AutoSize = true, 
                ForeColor = Color.FromArgb(250, 204, 21), 
                BackColor = Color.FromArgb(30, 41, 59),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4)
            };
            var btnCopyCred = Theme.MakeButton("📋 نسخ الرابط والبيانات للعميل", Color.FromArgb(16, 185, 129));
            btnCopyCred.Size = new Size(200, 32);
            btnCopyCred.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnCopyCred.Click += (s, e) =>
            {
                string u = lblLiveWebUrl.Text.Trim();
                string txt = $"📱 تطبيق المالك ProSoft Cloud:\nالرابط: {u}\nاسم المستخدم: pro\nكلمة المرور: pro@2026";
                Clipboard.SetText(txt);
                MessageBox.Show("✅ تم نسخ رابط وبيانات دخول تطبيق المالك بنجاح لإرسالها للعميل:\n\n" + txt, "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlCredRow.Controls.Add(lblCredBadge);
            pnlCredRow.Controls.Add(btnCopyCred);

            tbl.Controls.Add(lblProject, 0, 0); tbl.Controls.Add(txtFirebaseProjectId, 1, 0);
            tbl.Controls.Add(lblUrlTitle, 0, 1); tbl.Controls.Add(lblLiveWebUrl, 1, 1);
            tbl.Controls.Add(lblAuto, 0, 2); tbl.Controls.Add(chkAutoSync, 1, 2);
            tbl.Controls.Add(lblInt, 0, 3); tbl.Controls.Add(pnlIntRow, 1, 3);
            tbl.Controls.Add(lblCredTitle, 0, 4); tbl.Controls.Add(pnlCredRow, 1, 4);

            grpSettings.Controls.Add(tbl);

            // Group: Deploy & Log Box
            var grpDeploy = new GroupBox
            {
                Text = "🚀 سجل نشر وتهيئة تطبيق المالك على Firebase (Deploy Log)",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(245, 158, 11),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Padding = new Padding(8)
            };

            txtDeployLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(10, 14, 23),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Consolas", 9f),
                Text = "جاهز لنشر تطبيق المالك على حساب Firebase... انقر على زر النشر أعلاه."
            };
            grpDeploy.Controls.Add(txtDeployLog);

            pnlMain.Controls.Add(grpDeploy);
            pnlMain.Controls.Add(new Panel { Height = 8, Dock = DockStyle.Top });
            pnlMain.Controls.Add(grpSettings);

            // Controls docking order:
            // 1. Fill control first in Controls.Add so it gets sandwiched between Tops and Bottoms
            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlKPI);
            this.Controls.Add(pnlTitle);

            Theme.ApplyFormRTL(this);
        }

        private void UpdateWebUrlLabel()
        {
            string pId = txtFirebaseProjectId.Text.Trim();
            if (string.IsNullOrEmpty(pId)) pId = "checkin-192ab";
            lblLiveWebUrl.Text = $"https://{pId}.web.app";
        }

        private Label MakeKPICard(string title, string defaultVal, Color accentColor, out Panel card)
        {
            card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = Color.FromArgb(24, 32, 48),
                BorderStyle = BorderStyle.FixedSingle
            };

            var pnlBar = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = accentColor };
            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };

            var lblV = new Label
            {
                Text = defaultVal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
            };

            card.Controls.Add(lblV);
            card.Controls.Add(lblT);
            card.Controls.Add(pnlBar);

            return lblV;
        }

        private void LoadSettings()
        {
            try
            {
                string projectId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
                if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";
                txtFirebaseProjectId.Text = projectId;
                UpdateWebUrlLabel();

                DataTable dt = DbHelper.Query("SELECT TOP 1 AutoSyncEnabled, SyncIntervalMinutes, LastSyncDate, LastSyncStatus FROM CloudSyncSettings WHERE SettingID = 1");
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    chkAutoSync.Checked = r["AutoSyncEnabled"] != DBNull.Value && Convert.ToBoolean(r["AutoSyncEnabled"]);

                    int interval = r["SyncIntervalMinutes"] != DBNull.Value ? Convert.ToInt32(r["SyncIntervalMinutes"]) : 1;
                    cboInterval.SelectedIndex = interval <= 1 ? 0 : interval <= 2 ? 1 : interval <= 5 ? 2 : 3;

                    if (r["LastSyncDate"] != DBNull.Value)
                        lblLastSyncTime.Text = "🕒 تاريخ وساعة آخر تحديث ومزامنة: " + Convert.ToDateTime(r["LastSyncDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                    else
                        lblLastSyncTime.Text = "🕒 تاريخ وساعة آخر تحديث ومزامنة: لم تتم بعد";
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل تحميل إعدادات التزامن", ex, "FrmCloudSync.LoadSettings");
            }
        }

        private void RefreshLiveStats()
        {
            var stats = CloudSyncService.GetLiveStats();
            lblSalesTotal.Text = stats.TodaySalesTotal.ToString("N2") + " ج";
            lblCashSales.Text = $"{stats.TodayCashSales:N0} | {stats.TodayCreditSales:N0} ج";
            lblCashboxBalance.Text = stats.CashboxBalance.ToString("N2") + " ج";
            lblLowStock.Text = stats.LowStockCount + " صنف";
            lblSyncStatus.Text = string.IsNullOrEmpty(stats.LastSyncStatus) ? "جاهز" : stats.LastSyncStatus;

            object profitObj = DbHelper.Scalar(
                @"SELECT ISNULL(SUM(si.TotalPrice - (si.Quantity * ISNULL(p.PurchasePrice, 0))), 0)
                  FROM SaleItems si
                  JOIN Sales s ON si.SaleID = s.SaleID
                  JOIN Products p ON si.ProductID = p.ProductID
                  WHERE CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
            decimal todayProfit = profitObj != null && profitObj != DBNull.Value ? Convert.ToDecimal(profitObj) : 0m;
            lblTodayNetProfit.Text = todayProfit.ToString("N2") + " ج";

            object cDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(Balance),0) FROM Clients WHERE Balance > 0");
            decimal cDebts = cDebtsObj != null && cDebtsObj != DBNull.Value ? Convert.ToDecimal(cDebtsObj) : 0m;
            lblClientDebts.Text = cDebts.ToString("N2") + " ج";

            object sDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(Balance),0) FROM Suppliers WHERE Balance > 0");
            decimal sDebts = sDebtsObj != null && sDebtsObj != DBNull.Value ? Convert.ToDecimal(sDebtsObj) : 0m;
            lblSupplierDebts.Text = sDebts.ToString("N2") + " ج";
        }

        private async void BtnSyncNow_Click(object sender, EventArgs e)
        {
            btnSyncNow.Enabled = false;
            btnSyncNow.Text = "⏳ جاري الرفع والمزامنة...";

            try
            {
                string projectId = txtFirebaseProjectId.Text.Trim();
                if (string.IsNullOrEmpty(projectId)) projectId = "mahmoud-68b74";

                AppConfig.Set("FirebaseProjectId", projectId);
                bool ok = await CloudSyncService.PushLiveStatsToFirebaseAsync(projectId);

                if (ok)
                {
                    lblLastSyncTime.Text = "🕒 تاريخ وساعة آخر تحديث ومزامنة: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    lblSyncStatus.Text = "متصل بنجاح 🔥";
                    MessageBox.Show($"✅ تم رفع ومزامنة كافة بيانات وتقارير المحل لحظياً بنجاح إلى Firebase للمشروع:\n({projectId})\n\nيمكن للمالك فتح التطبيق الآن ومتابعة البيانات مباشرة!", "نجاح المزامنة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("⚠️ لم تكتمل المزامنة. يرجى التأكد من اتصال الإنترنت وصحة معرّف المشروع (Project ID).", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                RefreshLiveStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ أثناء المزامنة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSyncNow.Enabled = true;
                btnSyncNow.Text = "🔥 مزامنة ورفع كافة البيانات لحظياً الآن";
            }
        }

        private async void BtnDeployFirebase_Click(object sender, EventArgs e)
        {
            string projectId = txtFirebaseProjectId.Text.Trim();
            if (string.IsNullOrEmpty(projectId))
            {
                MessageBox.Show("يرجى إدخال معرّف مشروع Firebase للعميل أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dr = MessageBox.Show($"هل تريد نشر وتحديث استضافة تطبيق المالك (Dedicated Hosting) على مشروع فيربيز الخاص بالعميل:\n\n🔥 {projectId}\n\n💡 ملاحظة: تطبيق المالك يعمل بالفعل دون حاجة للنشر عبر الرابط:\nhttps://checkin-192ab.web.app/?p={projectId}", "تأكيد النشر", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            btnDeployFirebase.Enabled = false;
            btnDeployFirebase.Text = "⏳ جاري التحديث والنشر...";
            txtDeployLog.Text = $"=== بدء عملية تحديث ونشر تطبيق المالك (Deploy) للمشروع: {projectId} ===\r\n";

            try
            {
                // 1. تحديث وتهيئة كافة ملفات الموبايل (firebase.json, index.html, sw.js)
                txtDeployLog.AppendText("[1/3] تجهيز وتحديث ملفات التطبيق (HTML / Service Worker / Config)...\r\n");
                CloudSyncService.EnsureMobileAppFiles();

                string mobileAppDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MobileApp");
                if (!Directory.Exists(mobileAppDir))
                {
                    string devPath = @"D:\قطع غيار وتوزيع\قطع غيار وتوزيع\ChickenDistUpdates-main\ChickenDistUpdates-main\MobileApp";
                    if (Directory.Exists(devPath)) mobileAppDir = devPath;
                }

                if (!Directory.Exists(mobileAppDir))
                {
                    txtDeployLog.AppendText($"❌ مجلد ملفات الموبايل MobileApp غير موجود: {mobileAppDir}\r\n");
                    return;
                }

                // ── البحث عن firebase CLI ──
                string firebaseCmd = FindFirebaseCLI();
                if (string.IsNullOrEmpty(firebaseCmd))
                {
                    txtDeployLog.AppendText("❌ لم يتم العثور على Firebase CLI!\r\n");
                    txtDeployLog.AppendText("يرجى تثبيت Node.js ثم تشغيل:\r\n");
                    txtDeployLog.AppendText("   npm install -g firebase-tools\r\n");
                    txtDeployLog.AppendText("ثم تسجيل الدخول عبر:\r\n");
                    txtDeployLog.AppendText("   firebase login\r\n");
                    MessageBox.Show(
                        "❌ Firebase CLI غير موجود!\r\n\r\nخطوات الإعداد:\r\n1. ثبّت Node.js من: https://nodejs.org\r\n2. افتح CMD وشغّل: npm install -g firebase-tools\r\n3. ثم: firebase login\r\n4. بعدها اضغط نشر مرة ثانية",
                        "مطلوب إعداد Firebase CLI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                txtDeployLog.AppendText($"[1/2] المسار: {mobileAppDir}\r\n");
                txtDeployLog.AppendText($"[2/2] تنفيذ: {firebaseCmd} deploy --only hosting --project {projectId}...\r\n");

                // بناء PATH محسّن يشمل Node.js و npm global
                string extraPaths = string.Join(";", new[]
                {
                    @"C:\Program Files\nodejs",
                    @"C:\Program Files (x86)\nodejs",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm")
                });
                string fullPath = extraPaths + ";" + Environment.GetEnvironmentVariable("PATH");

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{firebaseCmd}\" deploy --only hosting --project {projectId} --non-interactive",
                    WorkingDirectory = mobileAppDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                psi.EnvironmentVariables["PATH"] = fullPath;

                using (var proc = new Process { StartInfo = psi })
                {
                    proc.OutputDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                            this.Invoke((Action)(() => txtDeployLog.AppendText(ev.Data + "\r\n")));
                    };
                    proc.ErrorDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                            this.Invoke((Action)(() => txtDeployLog.AppendText("[LOG] " + ev.Data + "\r\n")));
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    await Task.Run(() => proc.WaitForExit());

                    if (proc.ExitCode == 0)
                    {
                        // رفع ومزامنة البيانات اللحظية فوراً بعد النشر
                        txtDeployLog.AppendText($"[3/3] جاري رفع ومزامنة بيانات السيكول اللحظية وتقارير التقفيلات...\r\n");
                        await CloudSyncService.PushLiveStatsToFirebaseAsync(projectId);

                        string appUrl = $"https://{projectId}.web.app";
                        txtDeployLog.AppendText($"\r\n==========================================\r\n");
                        txtDeployLog.AppendText($"✅ تم نشر وتحديث تطبيق المالك ومزامنة البيانات بنجاح!\r\n");
                        txtDeployLog.AppendText($"🔗 رابط تطبيق المالك: {appUrl}\r\n");
                        txtDeployLog.AppendText($"==========================================\r\n");
                        Clipboard.SetText(appUrl);
                        MessageBox.Show(
                            $"✅ تم تحديث ونشر تطبيق المالك ومزامنة البيانات بنجاح!\r\n\r\n🔗 رابط التطبيق:\r\n{appUrl}\r\n\r\n(تم نسخ الرابط تلقائياً للحافظة)",
                            "نجاح النشر والتحديث", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        txtDeployLog.AppendText($"\r\n❌ انتهت عملية النشر بكود: {proc.ExitCode}\r\n");
                        txtDeployLog.AppendText("\r\nالأسباب المحتملة:\r\n");
                        txtDeployLog.AppendText("• المشروع غير موجود في Firebase Console (يجب إنشاؤه أولاً)\r\n");
                        txtDeployLog.AppendText("• لم يتم تسجيل الدخول (شغّل: firebase login)\r\n");
                        txtDeployLog.AppendText("• الحساب ليس لديه صلاحية على هذا المشروع\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                txtDeployLog.AppendText($"\r\n❌ استثناء أثناء النشر: {ex.Message}\r\n");
                MessageBox.Show("خطأ أثناء النشر: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDeployFirebase.Enabled = true;
                btnDeployFirebase.Text = "🚀 نشر تطبيق المالك (Deploy)";
            }
        }

        private async void BtnInstallFirebaseTools_Click(object sender, EventArgs e)
        {
            btnInstallTools.Enabled = false;
            btnInstallTools.Text = "⏳ جاري التثبيت...";
            txtDeployLog.Text = "=== بدء تثبيت أداة Firebase Tools (npm install -g firebase-tools) ===\r\n";
            txtDeployLog.AppendText("يرجى الانتظار، جاري تحميل وتثبيت حزمة الأدوات عبر Node.js...\r\n");

            try
            {
                CloudSyncService.EnsureMobileAppFiles();

                string npmCmd = FindNpmCLI();
                if (string.IsNullOrEmpty(npmCmd))
                {
                    txtDeployLog.AppendText("❌ لم يتم العثور على Node.js / npm على هذا الجهاز!\r\n");
                    txtDeployLog.AppendText("يرجى تثبيت Node.js أولاً من الموقع الرسمي: https://nodejs.org\r\n");
                    var res = MessageBox.Show(
                        "لم يتم العثور على Node.js على هذا الجهاز!\n\nهل ترغب في فتح موقع Node.js الرسمي لتحميله وتثبيته الآن؟",
                        "Node.js غير مثبت", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (res == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo { FileName = "https://nodejs.org/en/download", UseShellExecute = true });
                    }
                    return;
                }

                txtDeployLog.AppendText($"[1/2] تم العثور على npm: {npmCmd}\r\n");
                txtDeployLog.AppendText("[2/2] جاري تنفيذ: npm install -g firebase-tools ...\r\n");

                string extraPaths = string.Join(";", new[]
                {
                    @"C:\Program Files\nodejs",
                    @"C:\Program Files (x86)\nodejs",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm")
                });
                string fullPath = extraPaths + ";" + Environment.GetEnvironmentVariable("PATH");

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm install -g firebase-tools",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                psi.EnvironmentVariables["PATH"] = fullPath;

                using (var proc = new Process { StartInfo = psi })
                {
                    proc.OutputDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                            this.Invoke((Action)(() => txtDeployLog.AppendText(ev.Data + "\r\n")));
                    };
                    proc.ErrorDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                            this.Invoke((Action)(() => txtDeployLog.AppendText("[LOG] " + ev.Data + "\r\n")));
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    await Task.Run(() => proc.WaitForExit());

                    if (proc.ExitCode == 0)
                    {
                        txtDeployLog.AppendText("\r\n==========================================\r\n");
                        txtDeployLog.AppendText("✅ تم تثبيت أداة Firebase Tools بنجاح!\r\n");
                        txtDeployLog.AppendText("الخطوة التالية: اضغط على زر '🔑 تسجيل دخول فيربيز (Firebase Login)' لربط حساب Google.\r\n");
                        txtDeployLog.AppendText("==========================================\r\n");
                        MessageBox.Show("✅ تم تثبيت أداة Firebase Tools بنجاح على الجهاز!\n\nالخطوة التالية: اضغط على زر 'تسجيل دخول فيربيز' لربط الحساب.", "نجاح التثبيت", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        txtDeployLog.AppendText($"\r\n⚠️ انتهى التثبيت بكود: {proc.ExitCode}\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                txtDeployLog.AppendText($"\r\n❌ خطأ أثناء تثبيت Firebase Tools: {ex.Message}\r\n");
                MessageBox.Show("خطأ أثناء التثبيت: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnInstallTools.Enabled = true;
                btnInstallTools.Text = "📦 تثبيت أداة Firebase Tools";
            }
        }

        private void BtnLoginFirebase_Click(object sender, EventArgs e)
        {
            try
            {
                CloudSyncService.EnsureMobileAppFiles();

                string firebaseCmd = FindFirebaseCLI();
                if (string.IsNullOrEmpty(firebaseCmd))
                {
                    var res = MessageBox.Show(
                        "أداة Firebase CLI غير مثبتة بعد على هذا الجهاز!\n\nهل ترغب في تثبيتها أولاً عبر زر 'تثبيت أداة Firebase Tools'؟",
                        "Firebase Tools غير مثبت", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (res == DialogResult.Yes)
                    {
                        BtnInstallFirebaseTools_Click(sender, e);
                    }
                    return;
                }

                txtDeployLog.AppendText("=== جاري فتح نافذة تسجيل الدخول في Firebase (Google Login) ===\r\n");
                txtDeployLog.AppendText("سيتم فتح نافذة الأوامر والمتصفح لتسجيل الدخول بحساب Google...\r\n");

                string mobileAppDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MobileApp");
                if (!Directory.Exists(mobileAppDir)) Directory.CreateDirectory(mobileAppDir);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"\"{firebaseCmd}\" login\"",
                    WorkingDirectory = mobileAppDir,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Process.Start(psi);
                txtDeployLog.AppendText("✅ تم فتح نافذة تسجيل الدخول. يرجى إتمام التسجيل في المتصفح والعودة للبرنامج.\r\n");
            }
            catch (Exception ex)
            {
                txtDeployLog.AppendText($"❌ خطأ أثناء فتح نافذة تسجيل الدخول: {ex.Message}\r\n");
                MessageBox.Show("خطأ أثناء تسجيل الدخول: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEnsureFiles_Click(object sender, EventArgs e)
        {
            try
            {
                CloudSyncService.EnsureMobileAppFiles();
                string mobileDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MobileApp");
                txtDeployLog.AppendText("✅ تم توليد وتحديث ملفات firebase.json و index.html في المجلد:\r\n" + mobileDir + "\r\n");
                MessageBox.Show($"✅ تم التأكد وإنشاء ملفات:\n- MobileApp/firebase.json\n- MobileApp/index.html\n- firebase.json\n\nبنجاح داخل مسار البرنامج!", "تهيئة الملفات", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تهيئة الملفات: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>يبحث عن npm CLI في كل المسارات الممكنة</summary>
        private static string FindNpmCLI()
        {
            string[] candidatePaths = new[]
            {
                @"C:\Program Files\nodejs\npm.cmd",
                @"C:\Program Files (x86)\nodejs\npm.cmd",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\npm.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm\npm.cmd")
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path)) return path;
            }

            try
            {
                var psi = new ProcessStartInfo("where.exe", "npm")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    if (!string.IsNullOrEmpty(output))
                    {
                        string firstLine = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        if (File.Exists(firstLine)) return firstLine;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>يبحث عن firebase CLI في كل المسارات الممكنة</summary>
        private static string FindFirebaseCLI()
        {
            string[] candidatePaths = new[]
            {
                // npm global عند المستخدم الحالي
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\firebase.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm\firebase.cmd"),
                // npm global system
                @"C:\Program Files\nodejs\node_modules\firebase-tools\bin\firebase.cmd",
                @"C:\Users\" + Environment.UserName + @"\AppData\Roaming\npm\firebase.cmd",
                // npm global بدون .cmd
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\firebase"),
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path)) return path;
            }

            // جرّب where.exe
            try
            {
                var psi = new ProcessStartInfo("where.exe", "firebase")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    if (!string.IsNullOrEmpty(output))
                    {
                        string firstLine = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        if (File.Exists(firstLine)) return firstLine;
                    }
                }
            }
            catch { }

            return null; // لم يتم إيجاده
        }

        private void BtnOpenMobileApp_Click(object sender, EventArgs e)
        {
            try
            {
                string projectId = txtFirebaseProjectId.Text.Trim();
                if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";

                // بعد Deploy يفتح رابط العميل الخاص به مباشرة
                string url = $"https://{projectId}.web.app";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر فتح الرابط في المتصفح: " + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string projectId = txtFirebaseProjectId.Text.Trim();
                if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";

                AppConfig.Set("FirebaseProjectId", projectId);

                int interval = cboInterval.SelectedIndex == 0 ? 1 : cboInterval.SelectedIndex == 1 ? 2 : cboInterval.SelectedIndex == 2 ? 5 : 15;
                DbHelper.Execute(@"
                    UPDATE CloudSyncSettings 
                    SET AutoSyncEnabled = @auto, SyncIntervalMinutes = @int 
                    WHERE SettingID = 1",
                    DbHelper.P("@auto", chkAutoSync.Checked),
                    DbHelper.P("@int", interval));

                MessageBox.Show("✅ تم حفظ إعدادات تطبيق المالك وربط Firebase بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ حدث خطأ أثناء الحفظ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

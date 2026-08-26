using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.Services;

namespace ChickenDist.Forms
{
    /// <summary>شاشة إعدادات وتزامن تطبيق الموبايل مع تقارير تفصيلية وتاريخ التحديث</summary>
    public class FrmCloudSync : Form
    {
        private TextBox txtFirebaseProjectId;
        private CheckBox chkAutoSync;
        private ComboBox cboInterval;
        private Label lblSalesTotal, lblCashSales, lblCreditSales, lblCashboxBalance, lblLowStock, lblSyncStatus, lblLastSyncTime;
        private Label lblInvoiceCount, lblCashIn, lblCashOut, lblStockVal, lblClientDebts, lblSupplierDebts;
        private Button btnSyncNow, btnSave, btnOpenMobileApp;

        public FrmCloudSync()
        {
            InitUI();
            LoadSettings();
            RefreshLiveStats();
        }

        private void InitUI()
        {
            this.Text = "📱 ربط وتزامن تطبيق الموبايل للمالك (Firebase)";
            this.Size = new Size(1000, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== 1. Header Title =====
            var pnlTitle = Theme.MakeTitleBar("📱 ربط وتزامن تطبيق الموبايل للمالك (Firebase Cloud)", "متابعة تفاصيل المبيعات والخزنة والديون والأرباح مباشرة عبر فيربيز لحظة بلحظة");
            pnlTitle.Dock = DockStyle.Top;

            // ===== 2. Detailed KPI & Report Cards Panel =====
            var pnlKPI = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 220,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(10),
                BackColor = Theme.BgCard
            };
            pnlKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblSalesTotal = MakeKPICard("مبيعات اليوم الكلية", "0.00 ج", Color.FromArgb(40, 140, 220), out Panel p1);
            lblCashboxBalance = MakeKPICard("رصيد الخزنة الحالي", "0.00 ج", Color.FromArgb(40, 180, 100), out Panel p2);
            lblLowStock = MakeKPICard("أصناف النواقص", "0 صنف", Color.FromArgb(230, 120, 40), out Panel p3);
            lblSyncStatus = MakeKPICard("حالة التزامن", "جاهز", Color.FromArgb(160, 90, 220), out Panel p4);

            lblCashSales = MakeKPICard("مبيعات كاش / آجل", "0.00 | 0.00", Color.FromArgb(50, 160, 200), out Panel p5);
            lblCashIn = MakeKPICard("مقبوضات / مصروفات", "0.00 | 0.00", Color.FromArgb(60, 170, 120), out Panel p6);
            lblClientDebts = MakeKPICard("ديون العملاء", "0.00 ج", Color.FromArgb(210, 100, 180), out Panel p7);
            lblSupplierDebts = MakeKPICard("مستحقات الموردين", "0.00 ج", Color.FromArgb(220, 90, 90), out Panel p8);

            pnlKPI.Controls.Add(p1, 0, 0); pnlKPI.Controls.Add(p2, 1, 0); pnlKPI.Controls.Add(p3, 2, 0); pnlKPI.Controls.Add(p4, 3, 0);
            pnlKPI.Controls.Add(p5, 0, 1); pnlKPI.Controls.Add(p6, 1, 1); pnlKPI.Controls.Add(p7, 2, 1); pnlKPI.Controls.Add(p8, 3, 1);

            // ===== 3. Settings Form Group =====
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            var grpSettings = new GroupBox
            {
                Text = "🔥 إعدادات الربط المباشر مع Firebase",
                Dock = DockStyle.Top,
                Height = 220,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var lblProject = new Label { Text = "معرّف مشروع فيربيز (Project ID):", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left };
            txtFirebaseProjectId = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.No, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };

            var lblAuto = new Label { Text = "المزامنة اللحظية:", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left };
            chkAutoSync = new CheckBox { Text = "تفعيل المزامنة التلقائية اللحظية مع كل حركة بيع وتقفيل شيفت", AutoSize = true, ForeColor = Theme.TextMain, Checked = true };

            var lblInt = new Label { Text = "معدل التحديث:", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left };
            cboInterval = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            cboInterval.Items.AddRange(new object[] { "كل 30 ثانية", "كل دقيقة", "كل 5 دقائق", "كل 15 دقيقة" });
            cboInterval.SelectedIndex = 0;

            lblLastSyncTime = new Label { Text = "🕒 تاريخ وساعة آخر تحديث ومزامنة: لم تتم بعد", AutoSize = true, ForeColor = Color.FromArgb(70, 200, 240), Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };

            tbl.Controls.Add(lblProject, 0, 0); tbl.Controls.Add(txtFirebaseProjectId, 1, 0);
            tbl.Controls.Add(lblAuto, 0, 1); tbl.Controls.Add(chkAutoSync, 1, 1);
            tbl.Controls.Add(lblInt, 0, 2); tbl.Controls.Add(cboInterval, 1, 2);
            tbl.Controls.Add(lblLastSyncTime, 1, 3);

            grpSettings.Controls.Add(tbl);

            // ===== 4. Action Buttons =====
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10)
            };

            btnSyncNow = Theme.MakeButton("🔥 مزامنة وتحديث بيانات المالك على Firebase الآن", Theme.Accent);
            btnSyncNow.Size = new Size(300, 40);
            btnSyncNow.Click += BtnSyncNow_Click;

            btnOpenMobileApp = Theme.MakeButton("📱 فتح تطبيق الموبايل للمالك (Web App)", Color.FromArgb(160, 80, 220));
            btnOpenMobileApp.Size = new Size(260, 40);
            btnOpenMobileApp.Click += BtnOpenMobileApp_Click;

            btnSave = Theme.MakeButton("💾 حفظ الإعدادات", Theme.Success);
            btnSave.Size = new Size(160, 40);
            btnSave.Click += BtnSave_Click;

            pnlActions.Controls.AddRange(new Control[] { btnSyncNow, btnOpenMobileApp, btnSave });

            pnlMain.Controls.Add(grpSettings);
            pnlMain.Controls.Add(pnlActions);

            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlKPI);
            this.Controls.Add(pnlTitle);

            Theme.ApplyFormRTL(this);
        }

        private Label MakeKPICard(string title, string defaultVal, Color accentColor, out Panel card)
        {
            card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = Color.FromArgb(32, 40, 54),
                BorderStyle = BorderStyle.FixedSingle
            };

            var pnlBar = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = accentColor };
            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
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
                string projectId = AppConfig.Get("FirebaseProjectId", "mahmoud-68b74");
                if (string.IsNullOrEmpty(projectId)) projectId = "mahmoud-68b74";
                txtFirebaseProjectId.Text = projectId;

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

            // تفاصيل الديون والمقبوضات
            object cDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(Balance),0) FROM Clients WHERE Balance > 0");
            decimal cDebts = cDebtsObj != null && cDebtsObj != DBNull.Value ? Convert.ToDecimal(cDebtsObj) : 0m;
            lblClientDebts.Text = cDebts.ToString("N2") + " ج";

            object sDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(Balance),0) FROM Suppliers WHERE Balance > 0");
            decimal sDebts = sDebtsObj != null && sDebtsObj != DBNull.Value ? Convert.ToDecimal(sDebtsObj) : 0m;
            lblSupplierDebts.Text = sDebts.ToString("N2") + " ج";

            object inObj = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn),0) FROM CashBox WHERE CAST(TransDate AS DATE) = CAST(GETDATE() AS DATE)");
            decimal inAmt = inObj != null && inObj != DBNull.Value ? Convert.ToDecimal(inObj) : 0m;

            object outObj = DbHelper.Scalar("SELECT ISNULL(SUM(AmountOut),0) FROM CashBox WHERE CAST(TransDate AS DATE) = CAST(GETDATE() AS DATE)");
            decimal outAmt = outObj != null && outObj != DBNull.Value ? Convert.ToDecimal(outObj) : 0m;

            lblCashIn.Text = $"{inAmt:N0} | {outAmt:N0} ج";
        }

        private async void BtnSyncNow_Click(object sender, EventArgs e)
        {
            btnSyncNow.Enabled = false;
            lblSyncStatus.Text = "جاري المزامنة مع Firebase...";
            try
            {
                string projectId = txtFirebaseProjectId.Text.Trim();
                if (string.IsNullOrEmpty(projectId)) projectId = "mahmoud-68b74";

                bool ok = await CloudSyncService.PushLiveStatsToFirebaseAsync(projectId);
                string msg = ok 
                    ? $"✅ تم تحديث ورفع بيانات المالك الحية بنجاح إلى Firebase 🔥 ({projectId})" 
                    : "❌ تعذر الاتصال بـ Firebase، يرجى التأكد من اتصال الإنترنت وصحة معرّف المشروع";

                lblSyncStatus.Text = ok ? "متصل 🔥" : "فشل الاتصال";
                lblLastSyncTime.Text = "🕒 تاريخ وساعة آخر تحديث ومزامنة: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                RefreshLiveStats();
                MessageBox.Show(msg, "نتيجة المزامنة مع Firebase", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            finally
            {
                btnSyncNow.Enabled = true;
            }
        }

        private void BtnOpenMobileApp_Click(object sender, EventArgs e)
        {
            try
            {
                string projectId = txtFirebaseProjectId.Text.Trim();
                if (string.IsNullOrEmpty(projectId)) projectId = "mahmoud-68b74";

                string url = $"https://{projectId}.web.app";
                string localMobilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MobileApp", "index.html");

                if (System.IO.File.Exists(localMobilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = localMobilePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر فتح التطبيق: " + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string projectId = txtFirebaseProjectId.Text.Trim();
                if (string.IsNullOrEmpty(projectId)) projectId = "mahmoud-68b74";

                AppConfig.Set("FirebaseProjectId", projectId);

                int interval = cboInterval.SelectedIndex == 0 ? 1 : cboInterval.SelectedIndex == 1 ? 2 : cboInterval.SelectedIndex == 2 ? 5 : 15;
                DbHelper.Execute(@"
                    UPDATE CloudSyncSettings 
                    SET AutoSyncEnabled = @auto, SyncIntervalMinutes = @int 
                    WHERE SettingID = 1",
                    DbHelper.P("@auto", chkAutoSync.Checked),
                    DbHelper.P("@int", interval));

                MessageBox.Show("✅ تم حفظ إعدادات ربط الموبايل مع Firebase بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ حدث خطأ أثناء الحفظ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

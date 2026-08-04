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
        private TextBox txtApiUrl, txtSecretKey;
        private CheckBox chkAutoSync;
        private ComboBox cboInterval;
        private Label lblSalesTotal, lblCashSales, lblCreditSales, lblCashboxBalance, lblLowStock, lblSyncStatus, lblLastSyncTime;
        private Label lblInvoiceCount, lblCashIn, lblCashOut, lblStockVal, lblClientDebts, lblSupplierDebts;
        private Button btnSyncNow, btnSave, btnGeneratePairing;

        public FrmCloudSync()
        {
            InitUI();
            LoadSettings();
            RefreshLiveStats();
        }

        private void InitUI()
        {
            this.Text = "📱 ربط وتزامن تطبيق الموبايل مع التقارير التفصيلية";
            this.Size = new Size(1000, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== 1. Header Title =====
            var pnlTitle = Theme.MakeTitleBar("📱 ربط وتزامن تطبيق الموبايل للمالك", "متابعة تفاصيل التقارير وتاريخ آخر تحديث ومزامنة لحظة بلحظة");
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
                Text = "⚙️ إعدادات الاتصال وتاريخ التحديث",
                Dock = DockStyle.Top,
                Height = 250,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(10)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var lblApi = new Label { Text = "رابط سيرفر الموبايل (API):", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left };
            txtApiUrl = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.No };

            var lblKey = new Label { Text = "مفتاح الربط للمالك (Secret):", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left };
            txtSecretKey = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.No };

            var lblAuto = new Label { Text = "المزامنة التلقائية:", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left };
            chkAutoSync = new CheckBox { Text = "تفعيل المزامنة التلقائية مع كل عملية بيع وتقفيل شيفت", AutoSize = true, ForeColor = Theme.TextMain, Checked = true };

            var lblInt = new Label { Text = "معدل التكرار:", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left };
            cboInterval = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            cboInterval.Items.AddRange(new object[] { "كل دقيقة", "كل 5 دقائق", "كل 15 دقيقة", "كل ساعة" });
            cboInterval.SelectedIndex = 1;

            lblLastSyncTime = new Label { Text = "🕒 تاريخ وساعة آخر تحديث ومزامنة: لم تتم بعد", AutoSize = true, ForeColor = Color.FromArgb(70, 200, 240), Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };

            tbl.Controls.Add(lblApi, 0, 0); tbl.Controls.Add(txtApiUrl, 1, 0);
            tbl.Controls.Add(lblKey, 0, 1); tbl.Controls.Add(txtSecretKey, 1, 1);
            tbl.Controls.Add(lblAuto, 0, 2); tbl.Controls.Add(chkAutoSync, 1, 2);
            tbl.Controls.Add(lblInt, 0, 3); tbl.Controls.Add(cboInterval, 1, 3);
            tbl.Controls.Add(lblLastSyncTime, 1, 4);

            grpSettings.Controls.Add(tbl);

            // ===== 4. Action Buttons =====
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10)
            };

            btnSyncNow = Theme.MakeButton("⚡ مزامنة وتحديث التقارير السحابية الآن", Theme.Accent);
            btnSyncNow.Size = new Size(240, 38);
            btnSyncNow.Click += BtnSyncNow_Click;

            btnGeneratePairing = Theme.MakeButton("🔑 توليد ونسخ سيريال العميل للموبايل", Color.FromArgb(160, 80, 220));
            btnGeneratePairing.Size = new Size(240, 38);
            btnGeneratePairing.Click += BtnGeneratePairing_Click;

            btnSave = Theme.MakeButton("💾 حفظ الإعدادات", Theme.Success);
            btnSave.Size = new Size(160, 38);
            btnSave.Click += BtnSave_Click;

            pnlActions.Controls.AddRange(new Control[] { btnSyncNow, btnGeneratePairing, btnSave });

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
                DataTable dt = DbHelper.Query("SELECT TOP 1 ApiUrl, OwnerSecretKey, AutoSyncEnabled, SyncIntervalMinutes, LastSyncDate, LastSyncStatus FROM CloudSyncSettings WHERE SettingID = 1");
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    txtApiUrl.Text = r["ApiUrl"]?.ToString() ?? "https://api.chickendist.com/v1";
                    
                    string key = r["OwnerSecretKey"]?.ToString();
                    if (string.IsNullOrEmpty(key) || key == "OWNER-SECRET-KEY")
                    {
                        key = CloudSyncService.GetPermanentClientSerial();
                    }
                    txtSecretKey.Text = key;

                    chkAutoSync.Checked = r["AutoSyncEnabled"] != DBNull.Value && Convert.ToBoolean(r["AutoSyncEnabled"]);

                    int interval = r["SyncIntervalMinutes"] != DBNull.Value ? Convert.ToInt32(r["SyncIntervalMinutes"]) : 5;
                    cboInterval.SelectedIndex = interval <= 1 ? 0 : interval <= 5 ? 1 : interval <= 15 ? 2 : 3;

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
            lblSyncStatus.Text = "جاري المزامنة...";
            try
            {
                var result = await CloudSyncService.SyncNowAsync();
                lblSyncStatus.Text = result.message;
                lblLastSyncTime.Text = "🕒 تاريخ وساعة آخر تحديث ومزامنة: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                RefreshLiveStats();
                MessageBox.Show(result.message, "نتيجة المزامنة والتحديث", MessageBoxButtons.OK, result.success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            finally
            {
                btnSyncNow.Enabled = true;
            }
        }

        private void BtnGeneratePairing_Click(object sender, EventArgs e)
        {
            try
            {
                btnGeneratePairing.Enabled = false;
                btnGeneratePairing.Text = "⏳ جاري التوليد...";

                string cloudCode = DriverPortalServer.UploadToCloud();
                if (!string.IsNullOrEmpty(cloudCode))
                {
                    Clipboard.SetText(cloudCode);
                    MessageBox.Show(
                        $"🔑 كود الربط السحابي (السيريال) الخاص بك هو:\n\n" +
                        $"👉   {cloudCode}   👈\n\n" +
                        $"✅ تم نسخ الكود بنجاح إلى الحافظة!\n\n" +
                        $"📋 خطوات الاستخدام:\n" +
                        $"1. افتح تطبيق الموبايل على أي جهاز.\n" +
                        $"2. اضغط على زر '🔑 كود الربط' بأعلى شاشة الموبايل.\n" +
                        $"3. الصق الكود واضغط '⚡ ربط وجلب البيانات الفعليه'.",
                        "كود الربط بالموبايل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء إنشاء كود الربط السحابي: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGeneratePairing.Enabled = true;
                btnGeneratePairing.Text = "🔑 توليد كود الربط (السيريال)";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                int interval = cboInterval.SelectedIndex == 0 ? 1 : cboInterval.SelectedIndex == 1 ? 5 : cboInterval.SelectedIndex == 2 ? 15 : 60;
                DbHelper.Execute(@"
                    UPDATE CloudSyncSettings 
                    SET ApiUrl = @url, OwnerSecretKey = @key, AutoSyncEnabled = @auto, SyncIntervalMinutes = @int 
                    WHERE SettingID = 1",
                    DbHelper.P("@url", txtApiUrl.Text.Trim()),
                    DbHelper.P("@key", txtSecretKey.Text.Trim()),
                    DbHelper.P("@auto", chkAutoSync.Checked),
                    DbHelper.P("@int", interval));

                MessageBox.Show("✅ تم حفظ إعدادات ربط الموبايل بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ حدث خطأ أثناء الحفظ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

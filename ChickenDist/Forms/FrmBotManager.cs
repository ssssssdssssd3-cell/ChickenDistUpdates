using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmBotManager : Form
    {
        private Button btnToggle;
        private Button btnPushPrices;
        private Button btnClearSession;
        private PictureBox pbQrCode;
        private Label lblStatus;
        private Label lblLastSync;
        private Label lblQrCountdown;
        private ListBox lbLogs;
        private Timer tmrStatus;
        private Timer tmrCountdown;
        private int _qrSecondsLeft = 0;
        private HttpClient _httpClient;
        private Process _nodeProcess;
        private TextBox txtAccUrl;
        private Label lblPairingCodeResult;

        public FrmBotManager()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            StartLocalNodeServer();
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة بوت الواتساب واللوحة السحابية";
            this.Size = new Size(720, 670);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.FromArgb(245, 246, 250);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;

            // Layout setup
            TableLayoutPanel mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(15) };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // QR / Logs (takes remaining space)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Buttons
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Sync text
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F)); // Accountant App Link
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F)); // Pairing Code Control

            // Header/Status Layout container to support refresh and settings buttons
            TableLayoutPanel statusContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            statusContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            statusContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            statusContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23f));

            lblStatus = new Label 
            { 
                Text = "الحالة: جاري فحص الاتصال بالخادم...", 
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), 
                ForeColor = Color.DarkGray, 
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusContainer.Controls.Add(lblStatus, 0, 0);

            var btnCheckStatus = Theme.MakeButton("🔄 تحديث الحالة", Theme.Primary);
            btnCheckStatus.Size = new Size(130, 36);
            btnCheckStatus.Click += async (s, e) => {
                btnCheckStatus.Enabled = false;
                btnCheckStatus.Text = "⏳ جاري الفحص...";
                await CheckBotStatusAsync();
                btnCheckStatus.Text = "🔄 تحديث الحالة";
                btnCheckStatus.Enabled = true;
            };
            statusContainer.Controls.Add(btnCheckStatus, 1, 0);

            var btnCloudSettings = Theme.MakeButton("⚙️ الإعدادات السحابية", Theme.Accent);
            btnCloudSettings.Size = new Size(140, 36);
            btnCloudSettings.Click += BtnCloudSettings_Click;
            statusContainer.Controls.Add(btnCloudSettings, 2, 0);

            mainLayout.Controls.Add(statusContainer, 0, 0);
            mainLayout.SetColumnSpan(statusContainer, 2);

            // PictureBox for QR Code
            pbQrCode = new PictureBox 
            { 
                Size = new Size(280, 280), 
                SizeMode = PictureBoxSizeMode.Zoom, 
                BorderStyle = BorderStyle.FixedSingle, 
                BackColor = Color.White,
                Anchor = AnchorStyles.None 
            };
            mainLayout.Controls.Add(pbQrCode, 0, 1);

            // QR countdown label
            lblQrCountdown = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 126, 34),
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            mainLayout.Controls.Add(lblQrCountdown, 0, 1);

            // ListBox for Logs
            lbLogs = new ListBox 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(45, 52, 54),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9.5F),
                BorderStyle = BorderStyle.None
            };
            mainLayout.Controls.Add(lbLogs, 1, 1);

            // Button Control Panel
            FlowLayoutPanel btnPanelLeft = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            btnToggle = new Button 
            { 
                Text = "تشغيل البوت", 
                BackColor = Color.FromArgb(9, 132, 227), 
                ForeColor = Color.White, 
                Size = new Size(180, 42), 
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnToggle.FlatAppearance.BorderSize = 0;
            btnPanelLeft.Controls.Add(btnToggle);

            // Clear Session button
            btnClearSession = new Button
            {
                Text = "🗑️ مسح الجلسة",
                BackColor = Color.FromArgb(180, 60, 60),
                ForeColor = Color.White,
                Size = new Size(130, 42),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClearSession.FlatAppearance.BorderSize = 0;
            btnClearSession.Click += BtnClearSession_Click;
            btnPanelLeft.Controls.Add(btnClearSession);
            mainLayout.Controls.Add(btnPanelLeft, 0, 2);

            FlowLayoutPanel btnPanelRight = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            btnPushPrices = new Button 
            { 
                Text = "تحديث الأسعار وإرسالها للبوت 🔄", 
                BackColor = Color.FromArgb(46, 204, 113), 
                ForeColor = Color.White, 
                Size = new Size(240, 42), 
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPushPrices.FlatAppearance.BorderSize = 0;
            btnPanelRight.Controls.Add(btnPushPrices);
            mainLayout.Controls.Add(btnPanelRight, 1, 2);

            // Sync Label
            lblLastSync = new Label 
            { 
                Text = "آخر تحديث للأسعار: لم يتم تحديث البوت في هذه الجلسة.", 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true, 
                Anchor = AnchorStyles.Left 
            };
            mainLayout.Controls.Add(lblLastSync, 0, 3);
            mainLayout.SetColumnSpan(lblLastSync, 2);

            // Accountant Link Panel (Row 4)
            Panel pnlAccountant = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(230, 240, 250),
                Padding = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            Label lblAccTitle = new Label
            {
                Text = "📱 تطبيق المحاسب لاستقبال طلبات البوت (على الموبايل أو المتصفح):",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            pnlAccountant.Controls.Add(lblAccTitle);

            // Use permanent cloud URL instead of local IP address
            string accUrl = "https://checkin-192ab.web.app/admin.html";

            txtAccUrl = new TextBox
            {
                Text = accUrl,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(41, 128, 185),
                Font = new Font("Courier New", 10.5F, FontStyle.Bold),
                Location = new Point(230, 32),
                Width = 400,
                RightToLeft = RightToLeft.No
            };
            pnlAccountant.Controls.Add(txtAccUrl);

            Button btnCopyAccUrl = new Button
            {
                Text = "نسخ الرابط",
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 28),
                Location = new Point(120, 31),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCopyAccUrl.FlatAppearance.BorderSize = 0;
            btnCopyAccUrl.Click += (s, e) => {
                Clipboard.SetText(txtAccUrl.Text);
                MessageBox.Show("✅ تم نسخ رابط تطبيق المحاسب إلى الحافظة!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlAccountant.Controls.Add(btnCopyAccUrl);

            Button btnOpenAccUrl = new Button
            {
                Text = "فتح اللوحة",
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 28),
                Location = new Point(10, 31),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnOpenAccUrl.FlatAppearance.BorderSize = 0;
            btnOpenAccUrl.Click += (s, e) => {
                try { Process.Start(txtAccUrl.Text); } catch {}
            };
            pnlAccountant.Controls.Add(btnOpenAccUrl);

            Label lblAccTip = new Label
            {
                Text = "💡 افتح هذا الرابط السحابي من موبايل المحاسب في أي مكان لاستقبال وتأكيد طلبات الواتساب مباشرة.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(127, 140, 141),
                AutoSize = true,
                Location = new Point(10, 65)
            };
            pnlAccountant.Controls.Add(lblAccTip);

            mainLayout.Controls.Add(pnlAccountant, 0, 4);
            mainLayout.SetColumnSpan(pnlAccountant, 2);

            // WhatsApp Pairing Code Panel (Row 5)
            Panel pnlPairing = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(240, 248, 240), // Light green tint
                Padding = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            Label lblPairingTitle = new Label
            {
                Text = "🔗 ربط البوت بكود هاتف (بدون QR كود):",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            pnlPairing.Controls.Add(lblPairingTitle);

            TextBox txtPairPhone = new TextBox
            {
                Text = AppConfig.CompanyPhone1.Replace("+", "").Replace(" ", ""),
                Font = new Font("Segoe UI", 10.5F),
                Location = new Point(480, 32),
                Width = 160,
                RightToLeft = RightToLeft.No
            };
            pnlPairing.Controls.Add(txtPairPhone);

            Label lblPhoneHint = new Label
            {
                Text = "رقم الهاتف (بكود الدولة بدون +):",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.Gray,
                Location = new Point(480, 10),
                Width = 160
            };
            pnlPairing.Controls.Add(lblPhoneHint);

            Button btnRequestPairCode = new Button
            {
                Text = "طلب كود الربط",
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 28),
                Location = new Point(340, 31),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnRequestPairCode.FlatAppearance.BorderSize = 0;
            btnRequestPairCode.Click += async (s, e) => {
                string phone = txtPairPhone.Text.Trim();
                if (string.IsNullOrEmpty(phone) || !System.Text.RegularExpressions.Regex.IsMatch(phone, "^[0-9]+$"))
                {
                    MessageBox.Show("❌ يرجى إدخال رقم هاتف صحيح يحتوي على أرقام فقط وكود الدولة (مثال لمصر: 201012345678)", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                btnRequestPairCode.Enabled = false;
                await SendStartBotWithPairingPhoneAsync(phone);
                btnRequestPairCode.Enabled = true;
            };
            pnlPairing.Controls.Add(btnRequestPairCode);

            lblPairingCodeResult = new Label
            {
                Text = "كود الربط: سيظهر هنا بعد طلب الكود من الهاتف...",
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 34)
            };
            pnlPairing.Controls.Add(lblPairingCodeResult);

            mainLayout.Controls.Add(pnlPairing, 0, 5);
            mainLayout.SetColumnSpan(pnlPairing, 2);

            this.Controls.Add(mainLayout);

            // Hook Events
            btnToggle.Click += BtnToggle_Click;
            btnPushPrices.Click += BtnPushPrices_Click;

            // Timer setup to poll cloud status (every 10 seconds)
            tmrStatus = new Timer { Interval = 10000 };
            tmrStatus.Tick += TmrStatus_Tick;
            tmrStatus.Start();

            // Countdown timer for QR code validity
            tmrCountdown = new Timer { Interval = 1000 };
            tmrCountdown.Tick += (s, e) =>
            {
                if (_qrSecondsLeft > 0)
                {
                    _qrSecondsLeft--;
                    lblQrCountdown.Text = $"⏱ صالحية الكود: {_qrSecondsLeft} ثانية";
                    lblQrCountdown.ForeColor = _qrSecondsLeft < 15
                        ? Color.FromArgb(220, 50, 50)
                        : Color.FromArgb(230, 126, 34);
                }
                else
                {
                    lblQrCountdown.Text = "⚠️ انتهت صلاحية QR — جاري تحديثه...";
                    tmrCountdown.Stop();
                }
            };

            LogMessage("تم فتح شاشة إدارة البوت الميدانية.");
            LogMessage("تطبيق المبيعات يعمل الآن بنظام الربط السحابي المباشر ☁️");
        }

        private void LogMessage(string msg)
        {
            lbLogs.Items.Insert(0, $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}");
        }

        private async void TmrStatus_Tick(object sender, EventArgs e)
        {
            await CheckBotStatusAsync();
        }

        private async Task CheckBotStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://firestore.googleapis.com/v1/projects/{AppConfig.FirebaseProjectId}/databases/(default)/documents/metadata/status");
                if (response.IsSuccessStatusCode)
                {
                    string expectedAccUrl = $"{AppConfig.FirebaseWebUrl}/admin.html";
                    if (txtAccUrl != null && txtAccUrl.Text != expectedAccUrl)
                    {
                        txtAccUrl.Text = expectedAccUrl;
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    
                    // Simple string parsing to read the status field
                    string statusVal = "";
                    int statusKeyIdx = json.IndexOf("\"status\"");
                    if (statusKeyIdx != -1)
                    {
                        int valStart = json.IndexOf("\"stringValue\":", statusKeyIdx);
                        if (valStart != -1)
                        {
                            int quoteStart = json.IndexOf("\"", valStart + 14);
                            if (quoteStart != -1)
                            {
                                int quoteEnd = json.IndexOf("\"", quoteStart + 1);
                                if (quoteEnd != -1)
                                {
                                    statusVal = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                                }
                            }
                        }
                    }

                    if (statusVal == "Online")
                    {
                        lblStatus.Text = "الحالة: متصل بالواتساب ومتاح للعملاء (سحابي) ✅";
                        lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                        btnToggle.Text = "البوت متصل بالكامل";
                        btnToggle.BackColor = Color.FromArgb(46, 204, 113);
                        btnToggle.Enabled = false;
                        pbQrCode.Image = null;
                        pbQrCode.BackColor = Color.FromArgb(240, 240, 240);
                        lblQrCountdown.Visible = false;
                        tmrCountdown.Stop();
                        _qrSecondsLeft = 0;
                        tmrStatus.Interval = 20000; // Poll less frequently when online

                        if (lblPairingCodeResult != null)
                        {
                            lblPairingCodeResult.Text = "✅ تم ربط الحساب بالهاتف بنجاح!";
                            lblPairingCodeResult.ForeColor = Color.FromArgb(39, 174, 96);
                        }
                    }
                    else if (statusVal == "PairingCode_Ready")
                    {
                        lblStatus.Text = "الحالة: تم توليد كود الربط بنجاح! 🔑";
                        lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                        btnToggle.Text = "في انتظار الربط بالهاتف...";
                        btnToggle.BackColor = Color.FromArgb(52, 152, 219);
                        btnToggle.Enabled = false;
                        pbQrCode.Image = null;
                        tmrStatus.Interval = 3000; // Poll fast while waiting for linking

                        // Parse pairingCode field
                        string pairCode = "";
                        int pairCodeKeyIdx = json.IndexOf("\"pairingCode\"");
                        if (pairCodeKeyIdx != -1)
                        {
                            int valStart = json.IndexOf("\"stringValue\":", pairCodeKeyIdx);
                            if (valStart != -1)
                            {
                                int quoteStart = json.IndexOf("\"", valStart + 14);
                                if (quoteStart != -1)
                                {
                                    int quoteEnd = json.IndexOf("\"", quoteStart + 1);
                                    if (quoteEnd != -1)
                                    {
                                        pairCode = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                                    }
                                }
                            }
                        }

                        if (lblPairingCodeResult != null)
                        {
                            if (!string.IsNullOrEmpty(pairCode))
                            {
                                lblPairingCodeResult.Text = $"كود الربط الخاص بك: {pairCode}";
                                lblPairingCodeResult.ForeColor = Color.FromArgb(39, 174, 96);
                                lblPairingCodeResult.Font = new Font("Consolas", 14F, FontStyle.Bold);
                            }
                            else
                            {
                                lblPairingCodeResult.Text = "كود الربط: جاري توليد الكود...";
                                lblPairingCodeResult.ForeColor = Color.Orange;
                            }
                        }
                    }
                    else if (statusVal == "QR_Ready")
                    {
                        lblStatus.Text = "الحالة: يرجى مسح رمز الدخول بالهاتف 📲\n(السبب: لم يتم ربط الحساب بالهاتف)";
                        lblStatus.ForeColor = Color.FromArgb(230, 126, 34);
                        btnToggle.Text = "إعادة تشغيل الجلسة";
                        btnToggle.BackColor = Color.FromArgb(230, 126, 34);
                        btnToggle.Enabled = true;
                        LoadQrCodeFromStatusJson(json);
                        tmrStatus.Interval = 3000;

                        if (!tmrCountdown.Enabled || _qrSecondsLeft <= 0)
                        {
                            _qrSecondsLeft = 55;
                            lblQrCountdown.Text = $"⏱ صالحية الكود: 55 ثانية";
                            lblQrCountdown.Visible = true;
                            tmrCountdown.Start();
                        }

                        if (lblPairingCodeResult != null)
                        {
                            lblPairingCodeResult.Text = "كود الربط: سيظهر هنا بعد طلب الكود من الهاتف...";
                            lblPairingCodeResult.ForeColor = Color.FromArgb(44, 62, 80);
                            lblPairingCodeResult.Font = new Font("Consolas", 12F, FontStyle.Bold);
                        }
                    }
                    else if (statusVal == "Connecting")
                    {
                        lblStatus.Text = "الحالة: جاري تحضير المتصفح الخلفي... ⏳\n(السبب: جاري مزامنة وربط الجلسة)";
                        lblStatus.ForeColor = Color.FromArgb(52, 152, 219);
                        btnToggle.Text = "جاري التحضير...";
                        btnToggle.BackColor = Color.FromArgb(52, 152, 219);
                        btnToggle.Enabled = false;
                        pbQrCode.Image = null;
                        tmrStatus.Interval = 5000;
                    }
                    else
                    {
                        lblStatus.Text = "الحالة: البوت متوقف حالياً ❌\n(السبب: البوت مغلق من السحابة - اضغط ربط)";
                        lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
                        btnToggle.Text = "ربط وتفعيل البوت";
                        btnToggle.BackColor = Color.FromArgb(9, 132, 227);
                        btnToggle.Enabled = true;
                        pbQrCode.Image = null;
                        tmrStatus.Interval = 10000;

                        if (lblPairingCodeResult != null)
                        {
                            lblPairingCodeResult.Text = "كود الربط: سيظهر هنا بعد طلب الكود من الهاتف...";
                            lblPairingCodeResult.ForeColor = Color.FromArgb(44, 62, 80);
                            lblPairingCodeResult.Font = new Font("Consolas", 12F, FontStyle.Bold);
                        }
                    }
                }
                else
                {
                    lblStatus.Text = "الحالة: لا يمكن جلب حالة البوت السحابية ⚠️\n(السبب: خطأ خادم سحابي HTTP)";
                    lblStatus.ForeColor = Color.Red;
                    pbQrCode.Image = null;
                    tmrStatus.Interval = 15000;
                }
            }
            catch
            {
                lblStatus.Text = "الحالة: فشل الاتصال بالسحابة ⚠️";
                lblStatus.ForeColor = Color.Red;
                pbQrCode.Image = null;
                tmrStatus.Interval = 15000;
            }
        }

        private async Task SendStartBotWithPairingPhoneAsync(string phone)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    string json = "{" +
                                  "\"fields\": {" +
                                  "\"type\": {\"stringValue\": \"start_bot\"}," +
                                  "\"pairingPhone\": {\"stringValue\": \"" + phone + "\"}," +
                                  "\"status\": {\"stringValue\": \"pending\"}," +
                                  "\"time\": {\"stringValue\": \"" + DateTime.UtcNow.ToString("o") + "\"}" +
                                  "}" +
                                  "}";
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"https://firestore.googleapis.com/v1/projects/{AppConfig.FirebaseProjectId}/databases/(default)/documents/commands", content);
                    if (response.IsSuccessStatusCode)
                    {
                        LogMessage($"📬 تم إرسال طلب توليد كود الربط للرقم {phone} بنجاح!");
                        lblStatus.Text = "الحالة: جاري إرسال الطلب للسحابة... ⏳";
                        lblStatus.ForeColor = Color.FromArgb(52, 152, 219);
                        if (lblPairingCodeResult != null)
                        {
                            lblPairingCodeResult.Text = "كود الربط: جاري الاتصال بالسيرفر وتوليد الكود...";
                            lblPairingCodeResult.ForeColor = Color.Orange;
                        }
                    }
                    else
                    {
                        string res = await response.Content.ReadAsStringAsync();
                        LogMessage($"❌ فشل إرسال الطلب: {res}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ خطأ أثناء إرسال طلب الربط: {ex.Message}");
            }
        }

        private void BtnCloudSettings_Click(object sender, EventArgs e)
        {
            // Custom Password Dialog
            using (Form pwdForm = new Form())
            {
                pwdForm.Text = "أمان الإدارة";
                pwdForm.Size = new Size(350, 160);
                pwdForm.StartPosition = FormStartPosition.CenterParent;
                pwdForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                pwdForm.MaximizeBox = false;
                pwdForm.MinimizeBox = false;
                pwdForm.RightToLeft = RightToLeft.Yes;
                pwdForm.RightToLeftLayout = true;
                pwdForm.BackColor = Color.FromArgb(245, 246, 250);

                Label lblPrompt = new Label { Text = "أدخل كلمة مرور الإدارة لفتح الإعدادات السحابية:", Location = new Point(15, 15), Size = new Size(300, 20), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                TextBox txtPassword = new TextBox { Location = new Point(15, 40), Size = new Size(300, 25), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 10.5F) };
                Button btnOk = new Button { Text = "دخول", Location = new Point(220, 80), Size = new Size(90, 30), DialogResult = DialogResult.OK, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "إلغاء", Location = new Point(120, 80), Size = new Size(90, 30), DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(149, 165, 166), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnOk.FlatAppearance.BorderSize = 0;
                btnCancel.FlatAppearance.BorderSize = 0;

                pwdForm.Controls.Add(lblPrompt);
                pwdForm.Controls.Add(txtPassword);
                pwdForm.Controls.Add(btnOk);
                pwdForm.Controls.Add(btnCancel);
                pwdForm.AcceptButton = btnOk;

                if (pwdForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (txtPassword.Text == "Tamim")
                    {
                        OpenFirebaseSettingsDialog();
                    }
                    else
                    {
                        MessageBox.Show("❌ كلمة المرور غير صحيحة!", "خطأ في الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenFirebaseSettingsDialog()
        {
            using (Form configForm = new Form())
            {
                configForm.Text = "إعدادات الاتصال السحابي (Firebase)";
                configForm.Size = new Size(500, 380);
                configForm.StartPosition = FormStartPosition.CenterParent;
                configForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                configForm.MaximizeBox = false;
                configForm.MinimizeBox = false;
                configForm.RightToLeft = RightToLeft.Yes;
                configForm.RightToLeftLayout = true;
                configForm.BackColor = Color.FromArgb(245, 246, 250);

                int top = 15;
                
                Label lblApiKey = new Label { Text = "Firebase API Key:", Location = new Point(15, top), Size = new Size(450, 20), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                TextBox txtApiKey = new TextBox { Text = AppConfig.FirebaseApiKey, Location = new Point(15, top + 20), Size = new Size(450, 25), RightToLeft = RightToLeft.No, Font = new Font("Segoe UI", 10F) };
                
                top += 55;
                Label lblProjId = new Label { Text = "Project ID:", Location = new Point(15, top), Size = new Size(450, 20), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                TextBox txtProjId = new TextBox { Text = AppConfig.FirebaseProjectId, Location = new Point(15, top + 20), Size = new Size(450, 25), RightToLeft = RightToLeft.No, Font = new Font("Segoe UI", 10F) };

                top += 55;
                Label lblBucket = new Label { Text = "Storage Bucket:", Location = new Point(15, top), Size = new Size(450, 20), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                TextBox txtBucket = new TextBox { Text = AppConfig.FirebaseStorageBucket, Location = new Point(15, top + 20), Size = new Size(450, 25), RightToLeft = RightToLeft.No, Font = new Font("Segoe UI", 10F) };

                top += 55;
                Label lblWebUrl = new Label { Text = "Hosting Web URL (الرابط الرئيسي للويب دون admin.html):", Location = new Point(15, top), Size = new Size(450, 20), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                TextBox txtWebUrl = new TextBox { Text = AppConfig.FirebaseWebUrl, Location = new Point(15, top + 20), Size = new Size(450, 25), RightToLeft = RightToLeft.No, Font = new Font("Segoe UI", 10F) };

                top += 60;
                Button btnSave = new Button { Text = "💾 حفظ الإعدادات", Location = new Point(345, top), Size = new Size(120, 32), DialogResult = DialogResult.OK, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                Button btnCancel = new Button { Text = "إلغاء", Location = new Point(215, top), Size = new Size(120, 32), DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(149, 165, 166), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F) };
                btnSave.FlatAppearance.BorderSize = 0;
                btnCancel.FlatAppearance.BorderSize = 0;

                configForm.Controls.Add(lblApiKey);
                configForm.Controls.Add(txtApiKey);
                configForm.Controls.Add(lblProjId);
                configForm.Controls.Add(txtProjId);
                configForm.Controls.Add(lblBucket);
                configForm.Controls.Add(txtBucket);
                configForm.Controls.Add(lblWebUrl);
                configForm.Controls.Add(txtWebUrl);
                configForm.Controls.Add(btnSave);
                configForm.Controls.Add(btnCancel);
                configForm.AcceptButton = btnSave;

                if (configForm.ShowDialog(this) == DialogResult.OK)
                {
                    AppConfig.FirebaseApiKey = txtApiKey.Text.Trim();
                    AppConfig.FirebaseProjectId = txtProjId.Text.Trim();
                    AppConfig.FirebaseStorageBucket = txtBucket.Text.Trim();
                    AppConfig.FirebaseWebUrl = txtWebUrl.Text.Trim();
                    
                    MessageBox.Show("✅ تم حفظ الإعدادات بنجاح! يرجى إعادة تشغيل البوت لتطبيق التغييرات الجديدة.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LogMessage("🔄 تم تحديث إعدادات Firebase السحابية.");
                    
                    // Force refresh status URLs
                    CheckBotStatusAsync().ConfigureAwait(false);
                }
            }
        }

        private void LoadQrCodeFromStatusJson(string json)
        {
            try
            {
                // Firestore REST API escapes forward slashes as \/ in JSON strings
                // Normalize before searching to handle both escaped and unescaped variants
                string normalizedJson = json.Replace("\\/", "/");

                int qrKeyIdx = normalizedJson.IndexOf("\"qr\"");
                if (qrKeyIdx == -1) return;

                int valStart = normalizedJson.IndexOf("\"stringValue\":", qrKeyIdx);
                if (valStart == -1) return;

                // Find the data URI prefix (PNG or jpeg fallback)
                int startIdx = normalizedJson.IndexOf("data:image/png;base64,", valStart);
                if (startIdx == -1)
                    startIdx = normalizedJson.IndexOf("data:image/jpeg;base64,", valStart);
                if (startIdx == -1) return;

                int commaIdx = normalizedJson.IndexOf(",", startIdx);
                if (commaIdx == -1) return;

                int base64Start = commaIdx + 1;
                int quoteEnd = normalizedJson.IndexOf("\"", base64Start);
                if (quoteEnd == -1) return;

                string base64 = normalizedJson.Substring(base64Start, quoteEnd - base64Start).Trim();
                // Remove any whitespace/newlines that may appear in large base64 blocks
                base64 = base64.Replace(" ", "").Replace("\n", "").Replace("\r", "");

                byte[] bytes = Convert.FromBase64String(base64);
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    var img = Image.FromStream(ms);
                    pbQrCode.Image = img;
                    pbQrCode.BackColor = Color.White;
                }
                LogMessage("✅ تم تحميل رمز QR — امسح الآن بهاتفك");
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ خطأ تحميل QR: {ex.Message}");
            }
        }

        private async void BtnClearSession_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "هل تريد مسح جلسة الواتساب وإعادة تشغيل البوت؟\nسيحتاج لمسح QR جديد.",
                "تأكيد",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                btnClearSession.Enabled = false;
                btnClearSession.Text = "⏳ جاري...";
                LogMessage("جاري إرسال أمر مسح الجلسة وإعادة التشغيل للسحابة...");

                string isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string json = "{\"fields\": {" +
                    "\"type\": {\"stringValue\": \"clear_session\"}," +
                    "\"status\": {\"stringValue\": \"pending\"}," +
                    "\"time\": {\"stringValue\": \"" + isoNow + "\"}" +
                    "}}";

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(
                    "https://firestore.googleapis.com/v1/projects/checkin-192ab/databases/(default)/documents/commands",
                    content);

                if (response.IsSuccessStatusCode)
                    LogMessage("✅ تم إرسال أمر مسح الجلسة — انتظر ظهور QR جديد خلال 30 ثانية...");
                else
                    LogMessage($"❌ فشل إرسال الأمر: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                LogMessage($"خطأ: {ex.Message}");
            }
            finally
            {
                btnClearSession.Text = "🗑️ مسح الجلسة";
                btnClearSession.Enabled = true;
            }
        }

        private async void BtnToggle_Click(object sender, EventArgs e)
        {
            try
            {
                bool isRunning = lblStatus.Text.Contains("متصل بالواتساب") || lblStatus.Text.Contains("يرجى") || lblStatus.Text.Contains("تحضير");
                string actionCmd = isRunning ? "stop_bot" : "start_bot";
                
                LogMessage(isRunning ? "جاري إرسال أمر إيقاف البوت للسحابة..." : "جاري إرسال أمر تشغيل البوت للسحابة...");
                
                string isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string json = "{" +
                              "\"fields\": {" +
                              "\"type\": {\"stringValue\": \"" + actionCmd + "\"}," +
                              "\"status\": {\"stringValue\": \"pending\"}," +
                              "\"time\": {\"stringValue\": \"" + isoNow + "\"}" +
                              "}" +
                              "}";
                              
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://firestore.googleapis.com/v1/projects/checkin-192ab/databases/(default)/documents/commands", content);
                
                if (response.IsSuccessStatusCode)
                {
                    LogMessage(isRunning ? "تم إرسال أمر الإيقاف للسحاب بنجاح." : "تم إرسال أمر تشغيل البوت للسحاب بنجاح.");
                }
                else
                {
                    string errBody = await response.Content.ReadAsStringAsync();
                    LogMessage($"❌ فشل إرسال الأمر السحابي: {response.StatusCode} - {errBody}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"خطأ: {ex.Message}");
            }
        }

        private async void BtnPushPrices_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage("جاري قراءة الأصناف النشطة والأسعار الحالية...");
                
                // Get active products from local database
                DataTable dt = DbHelper.Query("SELECT ProductID, ProductName, SalePrice AS Price FROM Products WHERE IsActive = 1");
                if (dt == null || dt.Rows.Count == 0)
                {
                    LogMessage("تنبيه: لا يوجد أصناف نشطة بقاعدة البيانات لإرسالها!");
                    return;
                }

                // Build properly-escaped JSON array for products in Firestore REST format
                var items = new System.Collections.Generic.List<string>(dt.Rows.Count);
                foreach (DataRow row in dt.Rows)
                {
                    string name = EscapeJsonString(row["ProductName"].ToString());
                    decimal price = Convert.ToDecimal(row["Price"]);
                    items.Add("{" +
                              "\"mapValue\": {" +
                              "\"fields\": {" +
                              "\"ProductID\": {\"integerValue\": \"" + row["ProductID"] + "\"}," +
                              "\"ProductName\": {\"stringValue\": \"" + name + "\"}," +
                              "\"Price\": {\"doubleValue\": " + price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "}" +
                              "}" +
                              "}" +
                              "}");
                }
                string pricesListJson = "[" + string.Join(",", items) + "]";

                // Get active clients from local database to sync with bot
                DataTable dtClients = DbHelper.Query("SELECT ClientID, ClientName, Phone FROM Clients WHERE IsActive = 1");
                string clientsListJson = "[]";
                if (dtClients != null && dtClients.Rows.Count > 0)
                {
                    var clientItems = new System.Collections.Generic.List<string>(dtClients.Rows.Count);
                    foreach (DataRow row in dtClients.Rows)
                    {
                        string clientName = EscapeJsonString(row["ClientName"].ToString());
                        string clientPhone = EscapeJsonString(row["Phone"].ToString());
                        clientItems.Add("{" +
                                        "\"mapValue\": {" +
                                        "\"fields\": {" +
                                        "\"ClientID\": {\"integerValue\": \"" + row["ClientID"] + "\"}," +
                                        "\"ClientName\": {\"stringValue\": \"" + clientName + "\"}," +
                                        "\"Phone\": {\"stringValue\": \"" + clientPhone + "\"}" +
                                        "}" +
                                        "}" +
                                        "}");
                    }
                    clientsListJson = "[" + string.Join(",", clientItems) + "]";
                }

                string isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // Construct full Firestore REST document bodies
                string pricesBody = "{" +
                                    "\"fields\": {" +
                                    "\"updatedTime\": {\"stringValue\": \"" + isoNow + "\"}," +
                                    "\"list\": {\"arrayValue\": {\"values\": " + pricesListJson + "}}" +
                                    "}" +
                                    "}";

                string clientsBody = "{" +
                                     "\"fields\": {" +
                                     "\"updatedTime\": {\"stringValue\": \"" + isoNow + "\"}," +
                                     "\"list\": {\"arrayValue\": {\"values\": " + clientsListJson + "}}" +
                                     "}" +
                                     "}";

                LogMessage($"جاري إرسال {dt.Rows.Count} صنف و {dtClients?.Rows.Count ?? 0} عميل للسحابة مباشرة...");

                var pricesContent = new StringContent(pricesBody, Encoding.UTF8, "application/json");
                var clientsContent = new StringContent(clientsBody, Encoding.UTF8, "application/json");

                var pricesRequest = new HttpRequestMessage(new HttpMethod("PATCH"), "https://firestore.googleapis.com/v1/projects/checkin-192ab/databases/(default)/documents/metadata/prices?updateMask.fieldPaths=list&updateMask.fieldPaths=updatedTime") { Content = pricesContent };
                var clientsRequest = new HttpRequestMessage(new HttpMethod("PATCH"), "https://firestore.googleapis.com/v1/projects/checkin-192ab/databases/(default)/documents/metadata/clients?updateMask.fieldPaths=list&updateMask.fieldPaths=updatedTime") { Content = clientsContent };

                var pricesResponse = await _httpClient.SendAsync(pricesRequest);
                var clientsResponse = await _httpClient.SendAsync(clientsRequest);

                if (pricesResponse.IsSuccessStatusCode && clientsResponse.IsSuccessStatusCode)
                {
                    LogMessage("✅ تم تحديث الأسعار والعملاء بالسحابة بنجاح!");
                    lblLastSync.Text = $"آخر تحديث للأسعار والعملاء: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                }
                else
                {
                    string body = "";
                    if (!pricesResponse.IsSuccessStatusCode)
                    {
                        try { body = await pricesResponse.Content.ReadAsStringAsync(); } catch { }
                        if (body.Length > 120) body = body.Substring(0, 120) + "...";
                        LogMessage($"❌ فشل تحديث الأسعار بالسحاب — HTTP {(int)pricesResponse.StatusCode}: {body}");
                    }
                    if (!clientsResponse.IsSuccessStatusCode)
                    {
                        try { body = await clientsResponse.Content.ReadAsStringAsync(); } catch { }
                        if (body.Length > 120) body = body.Substring(0, 120) + "...";
                        LogMessage($"❌ فشل تحديث العملاء بالسحاب — HTTP {(int)clientsResponse.StatusCode}: {body}");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                LogMessage("❌ انتهت مهلة الطلب — قد يكون الاتصال بالسحابة ضعيفاً.");
            }
            catch (Exception ex)
            {
                LogMessage($"خطأ أثناء المزامنة السحابية: {ex.Message}");
            }
        }

        /// <summary>
        /// يحوّل النص لصيغة JSON آمنة مع تجاوز جميع الأحرف الخاصة بشكل صحيح.
        /// </summary>
        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b");  break;
                    case '\f': sb.Append("\\f");  break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u" + ((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }


        private void StartLocalNodeServer()
        {
            try
            {
                var checkTask = _httpClient.GetAsync("http://localhost:5000/api/status");
                checkTask.Wait(1000);
                if (checkTask.IsCompleted && checkTask.Result.IsSuccessStatusCode)
                {
                    LogMessage("خادم البوت المحلي نشط ويعمل بالفعل.");
                    return;
                }
            }
            catch
            {
                // Port 5000 is not responding
            }

            try
            {
                string botDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot");
                if (!Directory.Exists(botDir) || !File.Exists(Path.Combine(botDir, "index.js")))
                {
                    string current = AppDomain.CurrentDomain.BaseDirectory;
                    for (int i = 0; i < 5; i++)
                    {
                        var parentInfo = Directory.GetParent(current);
                        if (parentInfo == null) break;
                        current = parentInfo.FullName;
                        string tempBotDir = Path.Combine(current, "bot");
                        if (Directory.Exists(tempBotDir) && File.Exists(Path.Combine(tempBotDir, "index.js")))
                        {
                            botDir = tempBotDir;
                            break;
                        }
                    }
                }
                
                string indexPath = Path.Combine(botDir, "index.js");
                if (File.Exists(indexPath))
                {
                    // Generate firebase_config.json dynamically based on current AppConfig values
                    string configJson = "{\n" +
                                        "  \"apiKey\": \"" + AppConfig.FirebaseApiKey + "\",\n" +
                                        "  \"authDomain\": \"" + AppConfig.FirebaseProjectId + ".firebaseapp.com\",\n" +
                                        "  \"projectId\": \"" + AppConfig.FirebaseProjectId + "\",\n" +
                                        "  \"storageBucket\": \"" + AppConfig.FirebaseStorageBucket + "\",\n" +
                                        "  \"messagingSenderId\": \"818712709979\",\n" +
                                        "  \"appId\": \"1:818712709979:web:ce0c913f02a43cec6a687e\",\n" +
                                        "  \"measurementId\": \"G-6YV1QPB7M6\"\n" +
                                        "}";
                    try
                    {
                        File.WriteAllText(Path.Combine(botDir, "firebase_config.json"), configJson, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"⚠️ فشل كتابة ملف إعدادات البوت السحابية: {ex.Message}");
                    }

                    LogMessage("جاري تشغيل خادم البوت المحلي...");
                    _nodeProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "node.exe",
                            Arguments = $"\"{indexPath}\"",
                            WorkingDirectory = botDir,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    
                    _nodeProcess.OutputDataReceived += (s, ev) => {
                        if (!string.IsNullOrEmpty(ev.Data))
                        {
                            try
                            {
                                this.BeginInvoke((Action)(() => LogMessage("[Server] " + ev.Data)));
                            }
                            catch {}
                        }
                    };
                    
                    _nodeProcess.ErrorDataReceived += (s, ev) => {
                        if (!string.IsNullOrEmpty(ev.Data))
                        {
                            try
                            {
                                this.BeginInvoke((Action)(() => LogMessage("[Error] " + ev.Data)));
                            }
                            catch {}
                        }
                    };

                    _nodeProcess.Start();
                    _nodeProcess.BeginOutputReadLine();
                    _nodeProcess.BeginErrorReadLine();
                }
                else
                {
                    LogMessage("⚠️ لم يتم العثور على ملفات خادم البوت المحلي!");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ فشل تشغيل خادم البوت تلقائياً: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Node server runs 24/7 as requested, so we keep the process running.
            // If the user wants to terminate it entirely when application exits, 
            // they can stop it manually, or we can terminate standard child process here:
            /*
            try
            {
                if (_nodeProcess != null && !_nodeProcess.HasExited)
                {
                    _nodeProcess.Kill();
                }
            }
            catch {}
            */
            base.OnFormClosed(e);
        }
    }
}

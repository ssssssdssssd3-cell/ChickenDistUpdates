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
        private Button btnFirebaseSettings;
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
        private TextBox txtClientUrl;

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
            this.Size = new Size(720, 680);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.FromArgb(245, 246, 250);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;

            // Layout setup
            TableLayoutPanel mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(15) };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // QR / Logs (takes remaining space)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Buttons
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Sync text
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 175F)); // Accountant and Client App Links

            // Header/Status Layout container to support refresh button
            TableLayoutPanel statusContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            statusContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            statusContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

            lblStatus = new Label 
            { 
                Text = "الحالة: جاري فحص الاتصال بالخادم...", 
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), 
                ForeColor = Color.DarkGray, 
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusContainer.Controls.Add(lblStatus, 0, 0);

            // Right-side buttons panel (refresh + firebase settings)
            FlowLayoutPanel statusBtnsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                WrapContents = false
            };

            var btnCheckStatus = Theme.MakeButton("🔄 تحديث", Theme.Primary);
            btnCheckStatus.Size = new Size(100, 36);
            btnCheckStatus.Click += async (s, e) => {
                btnCheckStatus.Enabled = false;
                btnCheckStatus.Text = "⏳...";
                await CheckBotStatusAsync();
                btnCheckStatus.Text = "🔄 تحديث";
                btnCheckStatus.Enabled = true;
            };
            statusBtnsPanel.Controls.Add(btnCheckStatus);

            btnFirebaseSettings = new Button
            {
                Text = "⚙️ إعدادات Firebase",
                BackColor = Color.FromArgb(255, 165, 0),
                ForeColor = Color.White,
                Size = new Size(155, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnFirebaseSettings.FlatAppearance.BorderSize = 0;
            btnFirebaseSettings.Click += BtnFirebaseSettings_Click;
            statusBtnsPanel.Controls.Add(btnFirebaseSettings);

            statusContainer.Controls.Add(statusBtnsPanel, 1, 0);

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

            // Accountant and Client Links Panel (Row 4)
            Panel pnlAccountant = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(230, 240, 250),
                Padding = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // ─── 1. Accountant App Link ───
            Label lblAccTitle = new Label
            {
                Text = "📱 رابط تطبيق المحاسب (لاستقبال وتأكيد الطلبات سحابياً):",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            pnlAccountant.Controls.Add(lblAccTitle);

            string projectId = GetFirebaseProjectId();
            string accUrl = $"https://{projectId}.web.app/admin.html";

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

            // ─── 2. Client Web Site Link ───
            Label lblClientTitle = new Label
            {
                Text = "🛒 رابط موقع طلبات العملاء (الويب سايت لنشره للزبائن):",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 68)
            };
            pnlAccountant.Controls.Add(lblClientTitle);

            string clientUrl = $"https://{projectId}.web.app";

            txtClientUrl = new TextBox
            {
                Text = clientUrl,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(39, 174, 96),
                Font = new Font("Courier New", 10.5F, FontStyle.Bold),
                Location = new Point(230, 92),
                Width = 400,
                RightToLeft = RightToLeft.No
            };
            pnlAccountant.Controls.Add(txtClientUrl);

            Button btnCopyClientUrl = new Button
            {
                Text = "نسخ الرابط",
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 28),
                Location = new Point(120, 91),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCopyClientUrl.FlatAppearance.BorderSize = 0;
            btnCopyClientUrl.Click += (s, e) => {
                Clipboard.SetText(txtClientUrl.Text);
                MessageBox.Show("✅ تم نسخ رابط موقع طلبات العملاء إلى الحافظة!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlAccountant.Controls.Add(btnCopyClientUrl);

            Button btnOpenClientUrl = new Button
            {
                Text = "فتح الموقع",
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 28),
                Location = new Point(10, 91),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnOpenClientUrl.FlatAppearance.BorderSize = 0;
            btnOpenClientUrl.Click += (s, e) => {
                try { Process.Start(txtClientUrl.Text); } catch {}
            };
            pnlAccountant.Controls.Add(btnOpenClientUrl);

            // ─── 3. Deploy Button ───
            Button btnDeployHosting = new Button
            {
                Text = "⚡ رفع وتفعيل المنيو سحابياً",
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(210, 34),
                Location = new Point(10, 130),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnDeployHosting.FlatAppearance.BorderSize = 0;
            btnDeployHosting.Click += (s, e) =>
            {
                try
                {
                    string botDir = GetBotDirectory();
                    string batPath = Path.Combine(botDir, "deploy_hosting.bat");
                    
                    string batContent = @"@echo off
chcp 65001 > nul
echo ===================================================
echo   ⚡ Cloud Menu Deployer - ChickenDist ⚡
echo ===================================================
echo.
echo [1/3] Checking Node.js and local packages...
cd /d ""%~dp0""
call npm install
echo.
echo [2/3] Checking Firebase login status...
echo (If a browser window opens, please login with your Google account)
call npx firebase login
echo.
echo [3/3] Deploying client menu and accountant portal to Firebase Hosting...
call npx firebase deploy --only hosting
echo.
echo ===================================================
echo   ✅ Done! Your website is now live online!
echo ===================================================
pause
";
                    // Write with UTF-8 without BOM to avoid cmd parse errors (∩╗┐@echo off)
                    var utf8WithoutBom = new System.Text.UTF8Encoding(false);
                    File.WriteAllText(batPath, batContent, utf8WithoutBom);

                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{batPath}\"",
                        WorkingDirectory = botDir,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تشغيل الرفع التلقائي: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            pnlAccountant.Controls.Add(btnDeployHosting);

            // ─── 4. Tip Label ───
            Label lblAccTip = new Label
            {
                Text = "💡 النصيحة: انشر رابط الويب سايت الأخضر لعملائك ليطلبوا منه، وافتح الأزرق في موبايل المحاسب.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(127, 140, 141),
                AutoSize = true,
                Location = new Point(230, 138)
            };
            pnlAccountant.Controls.Add(lblAccTip);

            mainLayout.Controls.Add(pnlAccountant, 0, 4);
            mainLayout.SetColumnSpan(pnlAccountant, 2);

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
                string projectId = GetFirebaseProjectId();
                var response = await _httpClient.GetAsync($"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/metadata/status");
                if (response.IsSuccessStatusCode)
                {
                    string targetAccUrl = $"https://{projectId}.web.app/admin.html";
                    if (txtAccUrl != null && txtAccUrl.Text != targetAccUrl)
                    {
                        txtAccUrl.Text = targetAccUrl;
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
                    }
                    else if (statusVal == "PairingCode_Ready")
                    {
                        string code = "";
                        var mCode = System.Text.RegularExpressions.Regex.Match(json, @"""pairingCode""\s*:\s*\{\s*""stringValue""\s*:\s*""([^""]+)""");
                        if (mCode.Success)
                        {
                            code = mCode.Groups[1].Value;
                        }

                        lblStatus.Text = $"الحالة: كود الاقتران جاهز ({code}) 📲\n(أدخل الكود في تطبيق الواتساب بالهاتف للربط)";
                        lblStatus.ForeColor = Color.FromArgb(155, 89, 182);
                        btnToggle.Text = "إعادة تشغيل الجلسة";
                        btnToggle.BackColor = Color.FromArgb(155, 89, 182);
                        btnToggle.Enabled = true;

                        DisplayPairingCode(code);

                        tmrStatus.Interval = 3000;
                        lblQrCountdown.Visible = false;
                        tmrCountdown.Stop();
                    }
                    else if (statusVal == "QR_Ready")
                    {
                        lblStatus.Text = "الحالة: يرجى مسح رمز الدخول بالهاتف 📲\n(السبب: لم يتم ربط الحساب بالهاتف)";
                        lblStatus.ForeColor = Color.FromArgb(230, 126, 34);
                        btnToggle.Text = "إعادة تشغيل الجلسة";
                        btnToggle.BackColor = Color.FromArgb(230, 126, 34);
                        btnToggle.Enabled = true;
                        LoadQrCodeFromStatusJson(json);
                        // Poll every 3 seconds while waiting for QR scan
                        tmrStatus.Interval = 3000;
                        // Reset countdown to 55 seconds (QR valid ~60s, we start from 55 to be safe)
                        if (!tmrCountdown.Enabled || _qrSecondsLeft <= 0)
                        {
                            _qrSecondsLeft = 55;
                            lblQrCountdown.Text = $"⏱ صالحية الكود: 55 ثانية";
                            lblQrCountdown.Visible = true;
                            tmrCountdown.Start();
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
                        tmrStatus.Interval = 5000; // Poll faster when connecting
                    }
                    else
                    {
                        lblStatus.Text = "الحالة: البوت متوقف حالياً ❌\n(السبب: البوت مغلق من السحابة - اضغط ربط)";
                        lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
                        btnToggle.Text = "ربط وتفعيل البوت";
                        btnToggle.BackColor = Color.FromArgb(9, 132, 227);
                        btnToggle.Enabled = true;
                        pbQrCode.Image = null;
                        tmrStatus.Interval = 10000; // Default offline polling
                    }
                }
                else
                {
                    lblStatus.Text = "الحالة: لا يمكن جلب حالة البوت السحابية ⚠️\n(السبب: خطأ خادم سحابي HTTP)";
                    lblStatus.ForeColor = Color.Red;
                    pbQrCode.Image = null;
                    tmrStatus.Interval = 15000; // Cool down on error
                }
            }
            catch
            {
                lblStatus.Text = "الحالة: فشل الاتصال بالسحابة ⚠️";
                lblStatus.ForeColor = Color.Red;
                pbQrCode.Image = null;
                tmrStatus.Interval = 15000; // Cool down on connection error
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

        private void DisplayPairingCode(string code)
        {
            try
            {
                Bitmap bmp = new Bitmap(pbQrCode.Width, pbQrCode.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(245, 246, 250)); // matches theme background
                    
                    // Draw a nice rounded box for the code
                    using (Pen borderPen = new Pen(Color.FromArgb(155, 89, 182), 3))
                    {
                        g.DrawRectangle(borderPen, 15, 40, pbQrCode.Width - 30, pbQrCode.Height - 80);
                    }

                    // Draw text labels
                    using (Font titleFont = new Font("Segoe UI", 11F, FontStyle.Bold))
                    using (Font codeFont = new Font("Consolas", 28F, FontStyle.Bold))
                    using (Font descFont = new Font("Segoe UI", 8.5F, FontStyle.Regular))
                    using (Brush textBrush = new SolidBrush(Color.FromArgb(44, 62, 80)))
                    using (Brush codeBrush = new SolidBrush(Color.FromArgb(155, 89, 182)))
                    {
                        string titleText = "كود ربط الواتساب السريع";
                        string descText1 = "أدخل هذا الكود في هاتف العميل";
                        string descText2 = "من خيار (ربط برقم الهاتف)";

                        // Measure and draw title
                        SizeF titleSize = g.MeasureString(titleText, titleFont);
                        g.DrawString(titleText, titleFont, textBrush, (pbQrCode.Width - titleSize.Width) / 2, 60);

                        // Measure and draw code
                        string formattedCode = code; 
                        if (code.Length == 8)
                        {
                            formattedCode = code.Substring(0, 4) + " - " + code.Substring(4);
                        }
                        SizeF codeSize = g.MeasureString(formattedCode, codeFont);
                        g.DrawString(formattedCode, codeFont, codeBrush, (pbQrCode.Width - codeSize.Width) / 2, 105);

                        // Measure and draw descriptions
                        SizeF descSize1 = g.MeasureString(descText1, descFont);
                        g.DrawString(descText1, descFont, textBrush, (pbQrCode.Width - descSize1.Width) / 2, 175);
                        
                        SizeF descSize2 = g.MeasureString(descText2, descFont);
                        g.DrawString(descText2, descFont, textBrush, (pbQrCode.Width - descSize2.Width) / 2, 195);
                    }
                }
                pbQrCode.Image = bmp;
            }
            catch (Exception ex)
            {
                LogMessage($"خطأ في رسم كود الاقتران: {ex.Message}");
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

                string projectId = GetFirebaseProjectId();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(
                    $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/commands",
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
                bool isRunning = lblStatus.Text.Contains("متصل بالواتساب") || lblStatus.Text.Contains("يرجى") || lblStatus.Text.Contains("تحضير") || lblStatus.Text.Contains("كود");
                string actionCmd = isRunning ? "stop_bot" : "start_bot";
                string pairingPhone = null;

                if (actionCmd == "start_bot")
                {
                    // Show a dialog to choose between QR and Phone Pairing Code
                    using (Form inputDlg = new Form())
                    {
                        inputDlg.Text = "خيار ربط البوت بالواتساب";
                        inputDlg.Size = new Size(400, 240);
                        inputDlg.StartPosition = FormStartPosition.CenterParent;
                        inputDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                        inputDlg.MaximizeBox = false;
                        inputDlg.MinimizeBox = false;
                        inputDlg.RightToLeft = RightToLeft.Yes;
                        inputDlg.RightToLeftLayout = true;
                        inputDlg.BackColor = Color.FromArgb(245, 246, 250);

                        Label lblInfo = new Label
                        {
                            Text = "اختر طريقة ربط واتساب:\n- لمسح الـ QR: اترك الحقل فارغاً واضغط موافق.\n- للربط برقم الهاتف (Pairing Code): اكتب الرقم واضغط موافق.",
                            Location = new Point(15, 15),
                            Size = new Size(360, 60),
                            Font = new Font("Segoe UI", 9.5F),
                            ForeColor = Color.FromArgb(44, 62, 80)
                        };
                        inputDlg.Controls.Add(lblInfo);

                        Label lblPhone = new Label
                        {
                            Text = "رقم الموبايل (مثال: 01012345678):",
                            Location = new Point(15, 85),
                            Size = new Size(360, 20),
                            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                            ForeColor = Color.FromArgb(44, 62, 80)
                        };
                        inputDlg.Controls.Add(lblPhone);

                        TextBox txtPhone = new TextBox
                        {
                            Location = new Point(15, 110),
                            Size = new Size(360, 28),
                            Font = new Font("Segoe UI", 10.5F)
                        };
                        inputDlg.Controls.Add(txtPhone);

                        Button btnOk = new Button
                        {
                            Text = "موافق 👍",
                            DialogResult = DialogResult.OK,
                            Location = new Point(265, 155),
                            Size = new Size(110, 34),
                            BackColor = Color.FromArgb(46, 204, 113),
                            ForeColor = Color.White,
                            FlatStyle = FlatStyle.Flat,
                            Cursor = Cursors.Hand,
                            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
                        };
                        btnOk.FlatAppearance.BorderSize = 0;
                        inputDlg.Controls.Add(btnOk);

                        Button btnCancel = new Button
                        {
                            Text = "إلغاء",
                            DialogResult = DialogResult.Cancel,
                            Location = new Point(145, 155),
                            Size = new Size(110, 34),
                            BackColor = Color.FromArgb(180, 180, 180),
                            ForeColor = Color.White,
                            FlatStyle = FlatStyle.Flat,
                            Cursor = Cursors.Hand,
                            Font = new Font("Segoe UI", 9.5F)
                        };
                        btnCancel.FlatAppearance.BorderSize = 0;
                        inputDlg.Controls.Add(btnCancel);

                        inputDlg.AcceptButton = btnOk;
                        inputDlg.CancelButton = btnCancel;

                        if (inputDlg.ShowDialog(this) == DialogResult.OK)
                        {
                            string phoneStr = txtPhone.Text.Trim();
                            if (!string.IsNullOrEmpty(phoneStr))
                            {
                                pairingPhone = phoneStr;
                            }
                        }
                        else
                        {
                            return; // Cancel clicked, do nothing
                        }
                    }
                }
                
                LogMessage(isRunning ? "جاري إرسال أمر إيقاف البوت للسحابة..." : "جاري إرسال أمر تشغيل البوت للسحابة...");
                
                string isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string json;
                if (!string.IsNullOrEmpty(pairingPhone))
                {
                    json = "{" +
                           "\"fields\": {" +
                           "\"type\": {\"stringValue\": \"" + actionCmd + "\"}," +
                           "\"status\": {\"stringValue\": \"pending\"}," +
                           "\"pairingPhone\": {\"stringValue\": \"" + EscapeJsonString(pairingPhone) + "\"}," +
                           "\"time\": {\"stringValue\": \"" + isoNow + "\"}" +
                           "}" +
                           "}";
                }
                else
                {
                    json = "{" +
                           "\"fields\": {" +
                           "\"type\": {\"stringValue\": \"" + actionCmd + "\"}," +
                           "\"status\": {\"stringValue\": \"pending\"}," +
                           "\"time\": {\"stringValue\": \"" + isoNow + "\"}" +
                           "}" +
                           "}";
                }
                               
                string projectId = GetFirebaseProjectId();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/commands", content);
                
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

                string projectId = GetFirebaseProjectId();
                var pricesRequest = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/metadata/prices?updateMask.fieldPaths=list&updateMask.fieldPaths=updatedTime") { Content = pricesContent };
                var clientsRequest = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/metadata/clients?updateMask.fieldPaths=list&updateMask.fieldPaths=updatedTime") { Content = clientsContent };

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

        // ─────────────────────────────────────────────────────────────────────────
        // Firebase Settings Dialog
        // ─────────────────────────────────────────────────────────────────────────
        private void BtnFirebaseSettings_Click(object sender, EventArgs e)
        {
            // Build a clean, professional dialog
            Form dlg = new Form
            {
                Text = "⚙️ إعدادات Firebase",
                Size = new Size(520, 320),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(245, 246, 250),
                Font = new Font("Segoe UI", 10F),
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };

            // Title label
            Label lblTitle = new Label
            {
                Text = "إعدادات ربط Firebase",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(16, 14)
            };
            dlg.Controls.Add(lblTitle);

            // Description
            Label lblDesc = new Label
            {
                Text = "أدخل معرّف مشروع Firebase الخاص بك (Project ID).\nمثال: my-project-12345",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                AutoSize = true,
                Location = new Point(16, 46)
            };
            dlg.Controls.Add(lblDesc);

            // Project ID label
            Label lblProjectId = new Label
            {
                Text = "🔑 Firebase Project ID:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(16, 88)
            };
            dlg.Controls.Add(lblProjectId);

            // Project ID TextBox
            TextBox txtProjectId = new TextBox
            {
                Text = GetFirebaseProjectId(),
                Font = new Font("Courier New", 11F, FontStyle.Bold),
                Location = new Point(16, 112),
                Width = 470,
                Height = 32,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(41, 128, 185),
                RightToLeft = RightToLeft.No,
                BorderStyle = BorderStyle.FixedSingle
            };
            dlg.Controls.Add(txtProjectId);

            // Status label for test result
            Label lblTestResult = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(16, 152)
            };
            dlg.Controls.Add(lblTestResult);

            // Buttons panel
            FlowLayoutPanel btnPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(16, 220),
                Size = new Size(470, 48),
                BackColor = Color.Transparent,
                WrapContents = false
            };

            // Save button
            Button btnSave = new Button
            {
                Text = "💾 حفظ",
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Size = new Size(110, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s2, e2) =>
            {
                string pid = txtProjectId.Text.Trim();
                if (string.IsNullOrEmpty(pid))
                {
                    MessageBox.Show("يرجى إدخال Project ID!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Save to firebase_config.json
                try
                {
                    string botDir = GetBotDirectory();
                    if (!Directory.Exists(botDir)) Directory.CreateDirectory(botDir);

                    string configJson;
                    string actualPid = pid;

                    if (pid.StartsWith("{") || pid.Contains("apiKey"))
                    {
                        configJson = pid;
                        var match = System.Text.RegularExpressions.Regex.Match(pid, @"""projectId""\s*:\s*""([^""]+)""");
                        if (match.Success)
                        {
                            actualPid = match.Groups[1].Value;
                        }
                    }
                    else
                    {
                        configJson = "{\n  \"projectId\": \"" + pid + "\"\n}";
                    }

                    string configPath = Path.Combine(botDir, "firebase_config.json");
                    File.WriteAllText(configPath, configJson, Encoding.UTF8);

                    // Write .firebaserc to automatically link the local Firebase CLI project
                    try
                    {
                        string rcPath = Path.Combine(botDir, ".firebaserc");
                        string rcJson = "{\n  \"projects\": {\n    \"default\": \"" + actualPid + "\"\n  }\n}";
                        File.WriteAllText(rcPath, rcJson, Encoding.UTF8);
                    }
                    catch { }

                    // Kill existing node process to force server restart and load the new config
                    LogMessage("⏳ جاري إيقاف خادم البوت القديم...");
                    try
                    {
                        foreach (var proc in System.Diagnostics.Process.GetProcessesByName("node"))
                        {
                            try { proc.Kill(); } catch { }
                        }
                        // Also kill the tracked process if any
                        if (_nodeProcess != null && !_nodeProcess.HasExited)
                        {
                            try { _nodeProcess.Kill(); } catch { }
                        }
                        _nodeProcess = null;
                    }
                    catch { }

                    // Wait for node to fully die before restarting (blocking OK here since we're in a click handler)
                    System.Threading.Thread.Sleep(2500);

                    // Update URLs in main form
                    string newAccUrl = $"https://{actualPid}.web.app/admin.html";
                    string newClientUrl = $"https://{actualPid}.web.app";
                    if (txtAccUrl != null) txtAccUrl.Text = newAccUrl;
                    if (txtClientUrl != null) txtClientUrl.Text = newClientUrl;

                    LogMessage($"✅ تم حفظ إعدادات Firebase للمشروع: {actualPid}");

                    // Restart local node server to load new configurations (force=true to bypass port check)
                    StartLocalNodeServer(forceRestart: true);

                    MessageBox.Show(
                        $"✅ تم الحفظ بنجاح!\n\nProject ID: {actualPid}\nتم تحديث الروابط وإعادة تشغيل خادم البوت بالقيم الجديدة.",
                        "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في الحفظ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnPanel.Controls.Add(btnSave);

            // Test connection button
            Button btnTest = new Button
            {
                Text = "🔗 اختبار الاتصال",
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Size = new Size(150, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += async (s2, e2) =>
            {
                string pid = txtProjectId.Text.Trim();
                if (string.IsNullOrEmpty(pid))
                {
                    lblTestResult.Text = "⚠️ أدخل Project ID أولاً!";
                    lblTestResult.ForeColor = Color.Orange;
                    return;
                }
                btnTest.Enabled = false;
                btnTest.Text = "⏳ جاري الاختبار...";
                lblTestResult.Text = "جاري الاتصال بـ Firebase...";
                lblTestResult.ForeColor = Color.Gray;
                try
                {
                    var testClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                    var resp = await testClient.GetAsync(
                        $"https://firestore.googleapis.com/v1/projects/{pid}/databases/(default)/documents/metadata/status");
                    if (resp.IsSuccessStatusCode)
                    {
                        lblTestResult.Text = $"✅ الاتصال ناجح! المشروع '{pid}' متصل بـ Firestore.";
                        lblTestResult.ForeColor = Color.FromArgb(46, 204, 113);
                    }
                    else if ((int)resp.StatusCode == 403)
                    {
                        lblTestResult.Text = $"✅ المشروع موجود، لكن Firestore مقيّد (يحتاج auth). Project ID صحيح!";
                        lblTestResult.ForeColor = Color.FromArgb(230, 126, 34);
                    }
                    else if ((int)resp.StatusCode == 404)
                    {
                        // 404 means the project and database exist, but the specific status document is not created yet by the node bot.
                        lblTestResult.Text = $"✅ الاتصال ناجح! المشروع '{pid}' موجود وقاعدة البيانات متصلة. (اضغط حفظ وشغّل البوت).";
                        lblTestResult.ForeColor = Color.FromArgb(46, 204, 113);
                    }
                    else
                    {
                        lblTestResult.Text = $"⚠️ استجابة غير متوقعة: HTTP {(int)resp.StatusCode}";
                        lblTestResult.ForeColor = Color.Orange;
                    }
                }
                catch (TaskCanceledException)
                {
                    lblTestResult.Text = "❌ انتهت مهلة الاتصال — تحقق من الإنترنت.";
                    lblTestResult.ForeColor = Color.Red;
                }
                catch (Exception ex)
                {
                    lblTestResult.Text = $"❌ خطأ: {ex.Message}";
                    lblTestResult.ForeColor = Color.Red;
                }
                finally
                {
                    btnTest.Text = "🔗 اختبار الاتصال";
                    btnTest.Enabled = true;
                }
            };
            btnPanel.Controls.Add(btnTest);

            // Cancel button
            Button btnCancel = new Button
            {
                Text = "إلغاء",
                BackColor = Color.FromArgb(180, 180, 180),
                ForeColor = Color.White,
                Size = new Size(90, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s2, e2) => dlg.Close();
            btnPanel.Controls.Add(btnCancel);

            dlg.Controls.Add(btnPanel);
            dlg.ShowDialog(this);
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


        private void StartLocalNodeServer(bool forceRestart = false)
        {
            if (!forceRestart)
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
                    // Port 5000 is not responding — proceed to start
                }
            }

            try
            {
                string botDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot");
                if (!Directory.Exists(botDir))
                {
                    string parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.FullName;
                    if (parent != null)
                    {
                        botDir = Path.Combine(parent, "bot");
                    }
                }
                
                string indexPath = Path.Combine(botDir, "index.js");
                if (File.Exists(indexPath))
                {
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

        private string GetBotDirectory()
        {
            string botDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot");
            if (!Directory.Exists(botDir))
            {
                string parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.FullName;
                if (parent != null)
                {
                    botDir = Path.Combine(parent, "bot");
                }
            }
            return botDir;
        }

        private string GetFirebaseProjectId()
        {
            try
            {
                string path = Path.Combine(GetBotDirectory(), "firebase_config.json");
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    var match = System.Text.RegularExpressions.Regex.Match(content, @"""projectId""\s*:\s*""([^""]+)""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            catch { }
            return "checkin-192ab";
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

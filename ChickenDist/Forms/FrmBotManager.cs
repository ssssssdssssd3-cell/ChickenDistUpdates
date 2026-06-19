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
        private PictureBox pbQrCode;
        private Label lblStatus;
        private Label lblLastSync;
        private ListBox lbLogs;
        private Timer tmrStatus;
        private HttpClient _httpClient;
        private Process _nodeProcess;
        private TextBox txtAccUrl;

        public FrmBotManager()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة بوت الواتساب واللوحة السحابية";
            this.Size = new Size(700, 570);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.FromArgb(245, 246, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Layout setup
            TableLayoutPanel mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(15) };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // QR / Logs (takes remaining space)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Buttons
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Sync text
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F)); // Accountant App Link

            // Header/Status Label
            lblStatus = new Label 
            { 
                Text = "الحالة: جاري فحص الاتصال بالخادم...", 
                Font = new Font("Segoe UI", 13F, FontStyle.Bold), 
                ForeColor = Color.DarkGray, 
                AutoSize = true, 
                Anchor = AnchorStyles.Left | AnchorStyles.Right 
            };
            mainLayout.Controls.Add(lblStatus, 0, 0);
            mainLayout.SetColumnSpan(lblStatus, 2);

            // PictureBox for QR Code
            pbQrCode = new PictureBox 
            { 
                Size = new Size(240, 240), 
                SizeMode = PictureBoxSizeMode.Zoom, 
                BorderStyle = BorderStyle.FixedSingle, 
                BackColor = Color.White,
                Anchor = AnchorStyles.None 
            };
            mainLayout.Controls.Add(pbQrCode, 0, 1);

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

            // Fetch local IP for link
            string accUrl = "http://localhost:5000/";
            try
            {
                var localIPs = DriverPortalServer.GetLocalIPs();
                if (localIPs != null && localIPs.Count > 0)
                {
                    string chosenIP = localIPs[0];
                    foreach (var ip in localIPs)
                    {
                        if (ip.StartsWith("192.168.") || ip.StartsWith("10."))
                        {
                            chosenIP = ip;
                            break;
                        }
                    }
                    accUrl = $"http://{chosenIP}:5000/";
                }
            }
            catch {}

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
                Text = "💡 افتح هذا الرابط من موبايل المحاسب (بشرط الاتصال بنفس شبكة الـ Wi-Fi) لاستقبال وتأكيد طلبات الواتساب مباشرة.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(127, 140, 141),
                AutoSize = true,
                Location = new Point(10, 65)
            };
            pnlAccountant.Controls.Add(lblAccTip);

            mainLayout.Controls.Add(pnlAccountant, 0, 4);
            mainLayout.SetColumnSpan(pnlAccountant, 2);

            this.Controls.Add(mainLayout);

            // Hook Events
            btnToggle.Click += BtnToggle_Click;
            btnPushPrices.Click += BtnPushPrices_Click;

            // Timer setup to poll local server status
            tmrStatus = new Timer { Interval = 2000 };
            tmrStatus.Tick += TmrStatus_Tick;
            tmrStatus.Start();

            LogMessage("تم فتح شاشة إدارة البوت الميدانية.");
            CheckAndStartNodeServer();
        }

        private void LogMessage(string msg)
        {
            lbLogs.Items.Insert(0, $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}");
        }

        // Checks if Node server is running on port 5000, if not, spawns it
        private async void CheckAndStartNodeServer()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5000/api/status");
                if (response.IsSuccessStatusCode)
                {
                    LogMessage("خادم البوت المحلي نشط بالفعل ويعمل بالخلفية.");
                    return;
                }
            }
            catch
            {
                LogMessage("خادم البوت متوقف. محاولة بدء تشغيل خادم Node.js...");
                StartNodeProcess();
            }
        }

        private void StartNodeProcess()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string botDir = Path.Combine(baseDir, "bot");

                if (!Directory.Exists(botDir) || !File.Exists(Path.Combine(botDir, "index.js")))
                {
                    // Search upwards (e.g. for dev/nested environment or repo root next to release folder)
                    string current = baseDir;
                    for (int i = 0; i < 4; i++)
                    {
                        current = Path.GetDirectoryName(current);
                        if (string.IsNullOrEmpty(current)) break;

                        string testPath = Path.Combine(current, "bot");
                        if (Directory.Exists(testPath) && File.Exists(Path.Combine(testPath, "index.js")))
                        {
                            botDir = testPath;
                            break;
                        }

                        string repoPath = Path.Combine(current, "ChickenDistUpdates-main", "ChickenDistUpdates-main", "bot");
                        if (Directory.Exists(repoPath) && File.Exists(Path.Combine(repoPath, "index.js")))
                        {
                            botDir = repoPath;
                            break;
                        }

                        string repoPath1 = Path.Combine(current, "ChickenDistUpdates-main", "bot");
                        if (Directory.Exists(repoPath1) && File.Exists(Path.Combine(repoPath1, "index.js")))
                        {
                            botDir = repoPath1;
                            break;
                        }
                    }
                }

                if (!Directory.Exists(botDir) || !File.Exists(Path.Combine(botDir, "index.js")))
                {
                    LogMessage("خطأ: لم يتم العثور على مجلد البوت أو ملف index.js!");
                    return;
                }

                _nodeProcess = new Process();
                _nodeProcess.StartInfo.FileName = "node";
                _nodeProcess.StartInfo.Arguments = "index.js";
                _nodeProcess.StartInfo.WorkingDirectory = botDir;
                _nodeProcess.StartInfo.CreateNoWindow = true;
                _nodeProcess.StartInfo.UseShellExecute = false;
                _nodeProcess.StartInfo.RedirectStandardOutput = true;
                _nodeProcess.StartInfo.RedirectStandardError = true;

                _nodeProcess.OutputDataReceived += (s, ev) => {
                    if (!string.IsNullOrEmpty(ev.Data)) 
                        this.BeginInvoke(new Action(() => LogMessage($"[Server]: {ev.Data}")));
                };
                
                _nodeProcess.ErrorDataReceived += (s, ev) => {
                    if (!string.IsNullOrEmpty(ev.Data)) 
                        this.BeginInvoke(new Action(() => LogMessage($"[Error]: {ev.Data}")));
                };

                _nodeProcess.Start();
                _nodeProcess.BeginOutputReadLine();
                _nodeProcess.BeginErrorReadLine();

                LogMessage("تم إطلاق عملية خادم Node.js بنجاح.");
            }
            catch (Exception ex)
            {
                LogMessage($"فشل تشغيل عملية Node: {ex.Message}");
            }
        }

        private async void TmrStatus_Tick(object sender, EventArgs e)
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5000/api/status");
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        string tunnelFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot", "tunnel_url.txt");
                        if (File.Exists(tunnelFilePath))
                        {
                            string publicUrl = File.ReadAllText(tunnelFilePath).Trim();
                            if (!string.IsNullOrEmpty(publicUrl) && txtAccUrl != null && txtAccUrl.Text != publicUrl)
                            {
                                txtAccUrl.Text = publicUrl;
                            }
                        }
                    }
                    catch {}

                    string json = await response.Content.ReadAsStringAsync();
                    if (json.Contains("\"Online\""))
                    {
                        lblStatus.Text = "الحالة: متصل بالواتساب ومتاح للعملاء ✅";
                        lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                        btnToggle.Text = "إيقاف اتصال الواتساب";
                        btnToggle.BackColor = Color.FromArgb(231, 76, 60);
                        pbQrCode.Image = null;
                        pbQrCode.BackColor = Color.FromArgb(240, 240, 240);
                    }
                    else if (json.Contains("\"QR_Ready\""))
                    {
                        lblStatus.Text = "الحالة: يرجى مسح رمز الدخول بالهاتف 📲";
                        lblStatus.ForeColor = Color.FromArgb(230, 126, 34);
                        btnToggle.Text = "إلغاء الاتصال";
                        btnToggle.BackColor = Color.FromArgb(231, 76, 60);
                        LoadQrCodeImage();
                    }
                    else if (json.Contains("\"Connecting\""))
                    {
                        lblStatus.Text = "الحالة: جاري تحضير المتصفح الخلفي... ⏳";
                        lblStatus.ForeColor = Color.FromArgb(52, 152, 219);
                        btnToggle.Text = "إلغاء الاتصال";
                        btnToggle.BackColor = Color.FromArgb(231, 76, 60);
                        pbQrCode.Image = null;
                    }
                    else
                    {
                        lblStatus.Text = "الحالة: البوت متوقف حالياً ❌";
                        lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
                        btnToggle.Text = "ربط وتفعيل البوت";
                        btnToggle.BackColor = Color.FromArgb(9, 132, 227);
                        pbQrCode.Image = null;
                    }
                }
            }
            catch
            {
                lblStatus.Text = "الحالة: خادم البوت غير متصل ⚠️";
                lblStatus.ForeColor = Color.Red;
                btnToggle.Text = "تشغيل البوت";
                btnToggle.BackColor = Color.FromArgb(9, 132, 227);
                pbQrCode.Image = null;
            }
        }

        private async void LoadQrCodeImage()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5000/api/qr");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    int startIdx = json.IndexOf("data:image/png;base64,");
                    if (startIdx != -1)
                    {
                        string base64 = json.Substring(startIdx).Split('"')[0].Replace("data:image/png;base64,", "");
                        byte[] bytes = Convert.FromBase64String(base64);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            pbQrCode.Image = Image.FromStream(ms);
                        }
                    }
                }
            }
            catch {}
        }

        private async void BtnToggle_Click(object sender, EventArgs e)
        {
            try
            {
                bool isRunning = (lblStatus.Text.Contains("متصل") && !lblStatus.Text.Contains("غير متصل")) || lblStatus.Text.Contains("يرجى") || lblStatus.Text.Contains("تحضير");
                string action = isRunning ? "stop" : "start";
                
                LogMessage(isRunning ? "جاري إيقاف جلسة الواتساب..." : "جاري تشغيل جلسة الواتساب...");
                
                var content = new StringContent($"{{\"action\":\"{action}\"}}", Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync("http://localhost:5000/api/control", content);
                if (res.IsSuccessStatusCode)
                {
                    LogMessage(isRunning ? "تم إرسال أمر الإيقاف بنجاح." : "تم تشغيل المتصفح الخلفي للربط.");
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
                // ── 1. Verify server is up before attempting ──────────────────────
                try
                {
                    var statusResp = await _httpClient.GetAsync("http://localhost:5000/api/status");
                    if (!statusResp.IsSuccessStatusCode)
                    {
                        LogMessage("❌ خادم البوت لا يستجيب — شغّل البوت أولاً.");
                        return;
                    }
                }
                catch
                {
                    LogMessage("❌ تعذّر الاتصال بالخادم على المنفذ 5000 — شغّل البوت أولاً.");
                    return;
                }

                LogMessage("جاري قراءة الأصناف النشطة والأسعار الحالية...");
                
                // Get active products from local database
                DataTable dt = DbHelper.Query("SELECT ProductID, ProductName, SalePrice AS Price FROM Products WHERE IsActive = 1");
                if (dt == null || dt.Rows.Count == 0)
                {
                    LogMessage("تنبيه: لا يوجد أصناف نشطة بقاعدة البيانات لإرسالها!");
                    return;
                }

                // Build properly-escaped JSON array for products
                var items = new System.Collections.Generic.List<string>(dt.Rows.Count);
                foreach (DataRow row in dt.Rows)
                {
                    string name = EscapeJsonString(row["ProductName"].ToString());
                    decimal price = Convert.ToDecimal(row["Price"]);
                    items.Add("{\"ProductID\":" + row["ProductID"] +
                              ",\"ProductName\":\"" + name +
                              "\",\"Price\":" + price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "}");
                }
                string jsonBody = "[" + string.Join(",", items) + "]";

                // Get active clients from local database to sync with bot
                DataTable dtClients = DbHelper.Query("SELECT ClientID, ClientName, Phone FROM Clients WHERE IsActive = 1");
                string clientJson = "[]";
                if (dtClients != null && dtClients.Rows.Count > 0)
                {
                    var clientItems = new System.Collections.Generic.List<string>(dtClients.Rows.Count);
                    foreach (DataRow row in dtClients.Rows)
                    {
                        string clientName = EscapeJsonString(row["ClientName"].ToString());
                        string clientPhone = EscapeJsonString(row["Phone"].ToString());
                        clientItems.Add("{\"ClientID\":" + row["ClientID"] +
                                       ",\"ClientName\":\"" + clientName +
                                       "\",\"Phone\":\"" + clientPhone + "\"}");
                    }
                    clientJson = "[" + string.Join(",", clientItems) + "]";
                }

                LogMessage($"جاري إرسال {dt.Rows.Count} صنف و {dtClients?.Rows.Count ?? 0} عميل للبوت...");
                
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("http://localhost:5000/api/prices", content);

                var clientContent = new StringContent(clientJson, Encoding.UTF8, "application/json");
                var clientResponse = await _httpClient.PostAsync("http://localhost:5000/api/clients", clientContent);
                
                if (response.IsSuccessStatusCode && clientResponse.IsSuccessStatusCode)
                {
                    LogMessage("✅ تم تحديث الأسعار والعملاء بالبوت وحفظها محلياً بنجاح.");
                    lblLastSync.Text = $"آخر تحديث للأسعار والعملاء: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                }
                else
                {
                    string body = "";
                    if (!response.IsSuccessStatusCode)
                    {
                        try { body = await response.Content.ReadAsStringAsync(); } catch { }
                        if (body.Length > 120) body = body.Substring(0, 120) + "...";
                        LogMessage($"❌ فشل تحديث الأسعار — الخادم أعاد HTTP {(int)response.StatusCode}: {body}");
                    }
                    if (!clientResponse.IsSuccessStatusCode)
                    {
                        try { body = await clientResponse.Content.ReadAsStringAsync(); } catch { }
                        if (body.Length > 120) body = body.Substring(0, 120) + "...";
                        LogMessage($"❌ فشل تحديث العملاء — الخادم أعاد HTTP {(int)clientResponse.StatusCode}: {body}");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                LogMessage("❌ انتهت مهلة الطلب — قد يكون الخادم مشغولاً أو بطيئاً.");
            }
            catch (Exception ex)
            {
                LogMessage($"خطأ أثناء المزامنة: {ex.Message}");
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

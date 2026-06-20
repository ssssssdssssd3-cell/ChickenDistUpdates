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
            LogMessage("تم فتح شاشة إدارة البوت الميدانية (سحابي).");
            LogMessage("تطبيق المبيعات يعمل الآن بنظام الربط السحابي المباشر ☁️");
        }

        private void LogMessage(string msg)
        {
            lbLogs.Items.Insert(0, $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}");
        }

        private async void TmrStatus_Tick(object sender, EventArgs e)
        {
            try
            {
                var response = await _httpClient.GetAsync("https://firestore.googleapis.com/v1/projects/checkin-192ab/databases/(default)/documents/metadata/status");
                if (response.IsSuccessStatusCode)
                {
                    if (txtAccUrl != null && txtAccUrl.Text != "https://checkin-192ab.web.app")
                    {
                        txtAccUrl.Text = "https://checkin-192ab.web.app";
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
                    }
                    else if (statusVal == "QR_Ready")
                    {
                        lblStatus.Text = "الحالة: يرجى مسح رمز الدخول بالهاتف 📲";
                        lblStatus.ForeColor = Color.FromArgb(230, 126, 34);
                        btnToggle.Text = "إعادة تشغيل الجلسة";
                        btnToggle.BackColor = Color.FromArgb(230, 126, 34);
                        btnToggle.Enabled = true;
                        LoadQrCodeFromStatusJson(json);
                    }
                    else if (statusVal == "Connecting")
                    {
                        lblStatus.Text = "الحالة: جاري تحضير المتصفح الخلفي... ⏳";
                        lblStatus.ForeColor = Color.FromArgb(52, 152, 219);
                        btnToggle.Text = "جاري التحضير...";
                        btnToggle.BackColor = Color.FromArgb(52, 152, 219);
                        btnToggle.Enabled = false;
                        pbQrCode.Image = null;
                    }
                    else
                    {
                        lblStatus.Text = "الحالة: البوت متوقف حالياً ❌";
                        lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
                        btnToggle.Text = "ربط وتفعيل البوت";
                        btnToggle.BackColor = Color.FromArgb(9, 132, 227);
                        btnToggle.Enabled = true;
                        pbQrCode.Image = null;
                    }
                }
                else
                {
                    lblStatus.Text = "الحالة: لا يمكن جلب حالة البوت السحابية ⚠️";
                    lblStatus.ForeColor = Color.Red;
                    pbQrCode.Image = null;
                }
            }
            catch
            {
                lblStatus.Text = "الحالة: فشل الاتصال بالسحابة ⚠️";
                lblStatus.ForeColor = Color.Red;
                pbQrCode.Image = null;
            }
        }

        private void LoadQrCodeFromStatusJson(string json)
        {
            try
            {
                int qrKeyIdx = json.IndexOf("\"qr\"");
                if (qrKeyIdx != -1)
                {
                    int valStart = json.IndexOf("\"stringValue\":", qrKeyIdx);
                    if (valStart != -1)
                    {
                        int startIdx = json.IndexOf("data:image/png;base64,", valStart);
                        if (startIdx != -1)
                        {
                            int base64Start = startIdx + "data:image/png;base64,".Length;
                            int quoteEnd = json.IndexOf("\"", base64Start);
                            if (quoteEnd != -1)
                            {
                                string base64 = json.Substring(base64Start, quoteEnd - base64Start);
                                byte[] bytes = Convert.FromBase64String(base64);
                                using (MemoryStream ms = new MemoryStream(bytes))
                                {
                                    pbQrCode.Image = Image.FromStream(ms);
                                }
                            }
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

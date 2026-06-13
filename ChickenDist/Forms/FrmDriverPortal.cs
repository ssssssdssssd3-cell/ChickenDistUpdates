using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChickenDist.Core;
using QRCoder;

namespace ChickenDist.Forms
{
    public class FrmDriverPortal : Form
    {
        // ── الخادم المحلي ──────────────────────────────────────────────
        private Button   btnStartServer, btnStopServer;
        private Label    lblServerStatus, lblLocalUrl;
        private PictureBox picQR;
        private ComboBox cboIP;
        private NumericUpDown nudPort;
        private RichTextBox rtbLog;
        private CheckBox chkAutoStart;

        // ── السحابة ──────────────────────────────────────────────────
        private Button btnUploadCloud;
        private Label  lblCloudCode, lblCloudExpiry;
        private Button btnCopyCode;
        private ComboBox cboUploadDriver;

        // ── تبويبات ──────────────────────────────────────────────────
        private TabControl tabs;

        public FrmDriverPortal()
        {
            this.Text             = "📡 بوابة مزامنة المندوب";
            this.Size             = new Size(720, 600);
            this.StartPosition    = FormStartPosition.CenterParent;
            this.BackColor        = Theme.BgMain;
            this.Font             = Theme.FontMain;
            this.RightToLeft      = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.FormClosing      += FrmDriverPortal_FormClosing;

            BuildUI();
            LoadIPs();
            RefreshServerStatus();

            // ربط حدث وصول طلبات الموبايل بالسجل
            DriverPortalServer.OnRequestReceived += msg =>
            {
                if (rtbLog.IsHandleCreated)
                    rtbLog.Invoke((Action)(() =>
                    {
                        rtbLog.AppendText(msg + "\n");
                        rtbLog.ScrollToCaret();
                    }));
            };

            // تشغيل تلقائي إذا كان الخيار مفعلاً
            if (AppConfig.DriverPortalAutoStart && !DriverPortalServer.IsRunning)
                StartServer();
        }

        // ===================================================================
        // بناء الواجهة
        // ===================================================================
        private void BuildUI()
        {
            // شريط العنوان
            var pnlTop = Theme.MakeTitleBar("📡 بوابة مزامنة المندوب",
                "اختر طريقة إرسال البيانات للمندوب — سحابي (رمز 5 حروف) أو Wi-Fi محلي");
            this.Controls.Add(pnlTop);

            tabs = new TabControl
            {
                Location  = new Point(10, 65),
                Size      = new Size(688, 490),
                Font      = new Font("Segoe UI", 10f),
                RightToLeft = RightToLeft.Yes,
            };
            tabs.TabPages.Add(BuildCloudTab());
            tabs.TabPages.Add(BuildLocalTab());
            this.Controls.Add(tabs);
        }

        // ──────────────────────────────────────────────────────────────────
        // تبويب 1: المزامنة السحابية
        // ──────────────────────────────────────────────────────────────────
        private TabPage BuildCloudTab()
        {
            var page = new TabPage("🌐  مزامنة سحابية (الإنترنت)");
            page.BackColor = Theme.BgMain;
            page.RightToLeft = RightToLeft.Yes;

            int y = 18;

            // ── شرح ──────────────────────────────────────────────────
            var lblInfo = new Label
            {
                Text = "📋 الطريقة:\n" +
                       "① اضغط «رفع وتوليد الرمز» ← سيظهر رمز مكون من 5 حروف\n" +
                       "② أرسل الرمز للمندوب على واتساب\n" +
                       "③ المندوب يفتح تطبيق البيع → يكتب الرمز → يضغط «سحب» — خلاص!\n" +
                       "⏰ الرمز صالح 24 ساعة ثم يُحذف تلقائياً",
                Location  = new Point(14, y),
                Size      = new Size(645, 95),
                ForeColor = Theme.TextSub,
                Font      = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(30, 60, 90)
            };
            page.Controls.Add(lblInfo);
            y += 108;

            // ── اختيار المندوب ─────────────────────────────────────────
            var lblSelectDriver = new Label
            {
                Text      = "👤 المندوب المستهدف:",
                Location  = new Point(14, y + 4),
                Size      = new Size(160, 25),
                ForeColor = Theme.TextMain,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            page.Controls.Add(lblSelectDriver);

            cboUploadDriver = new ComboBox
            {
                Location      = new Point(180, y),
                Width         = 479,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = Theme.BgInput,
                ForeColor     = Theme.TextMain,
                Font          = new Font("Segoe UI", 11f)
            };
            cboUploadDriver.Items.Add(new ComboItem { ID = 0, Name = "-- جميع المناديب --" });
            try
            {
                var dt = DbHelper.Query("SELECT EmpID, EmpName FROM Employees WHERE IsDriver=1 AND IsActive=1 ORDER BY EmpName");
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    cboUploadDriver.Items.Add(new ComboItem { ID = Convert.ToInt32(r["EmpID"]), Name = r["EmpName"].ToString() });
                }
            }
            catch { }
            cboUploadDriver.SelectedIndex = 0;
            page.Controls.Add(cboUploadDriver);
            y += 38;

            // ── زر الرفع ─────────────────────────────────────────────
            btnUploadCloud = Theme.MakeButton("☁️  رفع وتوليد رمز المزامنة", Color.FromArgb(14, 122, 200));
            btnUploadCloud.Location = new Point(14, y);
            btnUploadCloud.Size     = new Size(645, 46);
            btnUploadCloud.Font     = new Font("Segoe UI", 14f, FontStyle.Bold);
            btnUploadCloud.Click   += BtnUploadCloud_Click;
            page.Controls.Add(btnUploadCloud);
            y += 58;

            // ── عرض الرمز ────────────────────────────────────────────
            var pnlCode = new Panel
            {
                Location  = new Point(14, y),
                Size      = new Size(645, 110),
                BackColor = Color.FromArgb(15, 35, 55),
                BorderStyle = BorderStyle.FixedSingle
            };
            page.Controls.Add(pnlCode);

            lblCloudCode = new Label
            {
                Text      = "——",
                Font      = new Font("Courier New", 38f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 210, 130),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill,
                Cursor    = Cursors.Hand
            };
            lblCloudCode.Click += (s, e) => CopyCode();
            pnlCode.Controls.Add(lblCloudCode);
            y += 118;

            lblCloudExpiry = new Label
            {
                Text      = "أرسل الرمز للمندوب عبر واتساب وهو يكتبه في التطبيق",
                Location  = new Point(14, y),
                Size      = new Size(645, 22),
                ForeColor = Theme.TextSub,
                Font      = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            page.Controls.Add(lblCloudExpiry);
            y += 28;

            // ── زر نسخ ───────────────────────────────────────────────
            btnCopyCode = Theme.MakeButton("📋  نسخ الرمز", Color.FromArgb(37, 170, 90));
            btnCopyCode.Location = new Point(14, y);
            btnCopyCode.Size     = new Size(310, 38);
            btnCopyCode.Click   += (s, e) => CopyCode();
            btnCopyCode.Enabled  = false;
            page.Controls.Add(btnCopyCode);

            var btnWhatsApp = Theme.MakeButton("📲  فتح واتساب ويب", Color.FromArgb(37, 211, 102));
            btnWhatsApp.Location = new Point(340, y);
            btnWhatsApp.Size     = new Size(319, 38);
            btnWhatsApp.Click   += (s, e) =>
            {
                if (!string.IsNullOrEmpty(DriverPortalServer.LastCloudCode))
                {
                    string msg = $"🔑 رمز مزامنة بيانات اليوم: {DriverPortalServer.LastCloudCode}\n\n" +
                                 $"رابط صفحة البيع للمندوب:\n" +
                                 $"https://raw.githack.com/ssssssdssssd3-cell/ChickenDistUpdates/main/ChickenDist/Forms/driver_sales.html\n\n" +
                                 $"افتح الرابط ← سحب من الإنترنت ← اكتب الرمز\n" +
                                 $"⏰ صالح حتى {DriverPortalServer.CloudCodeExpiry:hh:mm tt}";
                    Clipboard.SetText(msg);
                    MessageBox.Show("✅ تم نسخ رسالة واتساب الجاهزة!\nافتح واتساب ويب وألصق النص.",
                        "جاهز", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start("https://web.whatsapp.com");
                }
            };
            page.Controls.Add(btnWhatsApp);

            y += 48;

            // ── رابط صفحة المندوب ──────────────────────────────────────
            var lblDriverLinkTitle = new Label
            {
                Text      = "🔗 رابط صفحة المندوب (افتح منها واكتب الرمز):",
                Location  = new Point(14, y),
                Size      = new Size(645, 20),
                ForeColor = Theme.TextSub,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            page.Controls.Add(lblDriverLinkTitle);
            y += 24;

            var txtDriverLink = new TextBox
            {
                Text      = "https://raw.githack.com/ssssssdssssd3-cell/ChickenDistUpdates/main/ChickenDist/Forms/driver_sales.html",
                Location  = new Point(14, y),
                Size      = new Size(500, 28),
                ReadOnly  = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font      = new Font("Segoe UI", 9f),
                RightToLeft = RightToLeft.No
            };
            page.Controls.Add(txtDriverLink);

            var btnCopyLink = Theme.MakeButton("📋 نسخ الرابط", Color.FromArgb(70, 80, 95));
            btnCopyLink.Location = new Point(524, y - 1);
            btnCopyLink.Size     = new Size(135, 28);
            btnCopyLink.Font     = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnCopyLink.Click   += (s, e) =>
            {
                Clipboard.SetText("https://raw.githack.com/ssssssdssssd3-cell/ChickenDistUpdates/main/ChickenDist/Forms/driver_sales.html");
                MessageBox.Show("✅ تم نسخ رابط صفحة المندوب إلى الحافظة!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            page.Controls.Add(btnCopyLink);

            return page;
        }

        // ──────────────────────────────────────────────────────────────────
        // تبويب 2: المزامنة المحلية (Wi-Fi)
        // ──────────────────────────────────────────────────────────────────
        private TabPage BuildLocalTab()
        {
            var page = new TabPage("📶  مزامنة Wi-Fi محلي (نفس الشبكة)");
            page.BackColor = Theme.BgMain;
            page.RightToLeft = RightToLeft.Yes;

            int y = 14;

            // ── الـ IP والـ Port ──────────────────────────────────────
            var lblIP = new Label { Text = "كرت الشبكة / عنوان IP:", Location = new Point(14, y), AutoSize = true, ForeColor = Theme.TextSub };
            page.Controls.Add(lblIP);
            y += 22;

            cboIP = new ComboBox
            {
                Location      = new Point(14, y),
                Width         = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = Theme.BgInput,
                ForeColor     = Theme.TextMain,
                Font          = new Font("Courier New", 11f)
            };
            cboIP.SelectedIndexChanged += (s, e) => RefreshQR();
            page.Controls.Add(cboIP);

            var lblPort = new Label { Text = "المنفذ (Port):", Location = new Point(308, y - 22), AutoSize = true, ForeColor = Theme.TextSub };
            page.Controls.Add(lblPort);

            nudPort = new NumericUpDown
            {
                Location = new Point(308, y),
                Width    = 100,
                Minimum  = 1024,
                Maximum  = 65535,
                Value    = AppConfig.DriverPortalPort,
                BackColor= Theme.BgInput,
                ForeColor= Theme.TextMain,
                Font     = new Font("Courier New", 11f),
                RightToLeft = RightToLeft.No
            };
            page.Controls.Add(nudPort);

            chkAutoStart = new CheckBox
            {
                Text     = "تشغيل الخادم تلقائياً مع فتح البرنامج",
                Location = new Point(14, y + 35),
                AutoSize = true,
                ForeColor= Theme.TextMain,
                Checked  = AppConfig.DriverPortalAutoStart
            };
            page.Controls.Add(chkAutoStart);
            y += 72;

            // ── أزرار التشغيل ─────────────────────────────────────────
            btnStartServer = Theme.MakeButton("▶ تشغيل الخادم", Color.FromArgb(14, 122, 55));
            btnStartServer.Location = new Point(14, y);
            btnStartServer.Size     = new Size(190, 36);
            btnStartServer.Click   += (s, e) => StartServer();
            page.Controls.Add(btnStartServer);

            btnStopServer = Theme.MakeButton("⏹ إيقاف الخادم", Color.FromArgb(160, 30, 30));
            btnStopServer.Location = new Point(210, y);
            btnStopServer.Size     = new Size(190, 36);
            btnStopServer.Click   += (s, e) => StopServer();
            page.Controls.Add(btnStopServer);

            lblServerStatus = new Label
            {
                Location  = new Point(415, y + 8),
                Size      = new Size(250, 24),
                ForeColor = Color.Gray,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            page.Controls.Add(lblServerStatus);
            y += 46;

            // ── عنوان الرابط ─────────────────────────────────────────
            lblLocalUrl = new Label
            {
                Text      = "— الخادم متوقف —",
                Location  = new Point(14, y),
                Size      = new Size(640, 24),
                ForeColor = Theme.TextSub,
                Font      = new Font("Courier New", 11f)
            };
            page.Controls.Add(lblLocalUrl);
            y += 32;

            // ── صورة QR ──────────────────────────────────────────────
            var lblQRTitle = new Label
            {
                Text      = "📱 كود QR — المندوب يمسحه بكاميرا الجوال لفتح الصفحة مباشرة:",
                Location  = new Point(14, y),
                Size      = new Size(640, 22),
                ForeColor = Theme.TextSub,
                Font      = new Font("Segoe UI", 9.5f)
            };
            page.Controls.Add(lblQRTitle);
            y += 26;

            picQR = new PictureBox
            {
                Location    = new Point(14, y),
                Size        = new Size(200, 200),
                SizeMode    = PictureBoxSizeMode.Zoom,
                BackColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            page.Controls.Add(picQR);

            // ── سجل العمليات ─────────────────────────────────────────
            var lblLog = new Label
            {
                Text     = "📋 سجل الاتصالات:",
                Location = new Point(228, y),
                AutoSize = true,
                ForeColor= Theme.TextSub
            };
            page.Controls.Add(lblLog);

            rtbLog = new RichTextBox
            {
                Location   = new Point(228, y + 22),
                Size       = new Size(436, 178),
                BackColor  = Color.FromArgb(15, 20, 30),
                ForeColor  = Color.FromArgb(0, 210, 130),
                Font       = new Font("Courier New", 9f),
                ReadOnly   = true,
                BorderStyle= BorderStyle.None,
                RightToLeft= RightToLeft.No
            };
            page.Controls.Add(rtbLog);

            return page;
        }

        // ===================================================================
        // منطق التشغيل
        // ===================================================================

        private void LoadIPs()
        {
            cboIP.Items.Clear();
            foreach (var ip in DriverPortalServer.GetLocalIPs())
                cboIP.Items.Add(ip);
            if (cboIP.Items.Count > 0) cboIP.SelectedIndex = 0;
        }

        private void StartServer()
        {
            try
            {
                int port = (int)nudPort.Value;
                AppConfig.DriverPortalPort     = port;
                AppConfig.DriverPortalAutoStart = chkAutoStart.Checked;
                DriverPortalServer.Start(port);
                RefreshServerStatus();
                RefreshQR();
                rtbLog?.AppendText($"[{DateTime.Now:HH:mm:ss}] ✅ الخادم يعمل على المنفذ {port}\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ تشغيل الخادم", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopServer()
        {
            DriverPortalServer.Stop();
            RefreshServerStatus();
            picQR.Image = null;
            rtbLog?.AppendText($"[{DateTime.Now:HH:mm:ss}] ⏹ الخادم متوقف\n");
        }

        private void RefreshServerStatus()
        {
            if (btnStartServer == null) return;
            bool running = DriverPortalServer.IsRunning;
            btnStartServer.Enabled  = !running;
            btnStopServer.Enabled   =  running;
            lblServerStatus.Text    = running ? "🟢 الخادم نشط" : "🔴 الخادم متوقف";
            lblServerStatus.ForeColor = running ? Color.FromArgb(0, 210, 100) : Color.FromArgb(200, 60, 60);
        }

        private void RefreshQR()
        {
            if (!DriverPortalServer.IsRunning) return;
            string ip  = cboIP.SelectedItem?.ToString() ?? "127.0.0.1";
            string url = $"http://{ip}:{DriverPortalServer.Port}/";
            lblLocalUrl.Text = url;

            try
            {
                using (var gen = new QRCodeGenerator())
                {
                    var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
                    using (var qr = new QRCode(data))
                    {
                        var bmp = qr.GetGraphic(6, Color.Black, Color.White, true);
                        picQR.Image = bmp;
                    }
                }
            }
            catch
            {
                // إذا فشل QR — اعرض الرابط بخط كبير
                picQR.Image = null;
            }
        }

        private async void BtnUploadCloud_Click(object sender, EventArgs e)
        {
            btnUploadCloud.Enabled = false;
            btnUploadCloud.Text    = "⏳ جاري الرفع...";
            lblCloudCode.Text      = "⏳";
            lblCloudExpiry.Text    = "جارٍ رفع البيانات للإنترنت — انتظر...";

            int? targetDriverID = null;
            if (cboUploadDriver.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                targetDriverID = ci.ID;
            }

            try
            {
                string code = await Task.Run(() => DriverPortalServer.UploadToCloud(targetDriverID));
                lblCloudCode.Text   = code;
                lblCloudExpiry.Text = $"⏰ الرمز صالح حتى {DriverPortalServer.CloudCodeExpiry:hh:mm tt} — أرسله للمندوب على واتساب";
                btnCopyCode.Enabled = true;
            }
            catch (Exception ex)
            {
                lblCloudCode.Text   = "❌ فشل";
                lblCloudExpiry.Text = "تأكد من اتصال الإنترنت وحاول مرة أخرى";
                MessageBox.Show("فشل رفع البيانات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnUploadCloud.Enabled = true;
                btnUploadCloud.Text    = "☁️  رفع وتوليد رمز المزامنة";
            }
        }

        private void CopyCode()
        {
            if (!string.IsNullOrWhiteSpace(DriverPortalServer.LastCloudCode) &&
                DriverPortalServer.LastCloudCode != "——")
            {
                Clipboard.SetText(DriverPortalServer.LastCloudCode);
                MessageBox.Show($"✅ تم نسخ الرمز: {DriverPortalServer.LastCloudCode}",
                    "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void FrmDriverPortal_FormClosing(object sender, FormClosingEventArgs e)
        {
            AppConfig.DriverPortalAutoStart = chkAutoStart.Checked;
        }

        private class ComboItem
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }
    }
}

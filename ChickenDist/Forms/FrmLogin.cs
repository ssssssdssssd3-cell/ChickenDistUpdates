using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmLogin : Form
    {
        private TextBox txtUser, txtPass;
        private Button btnLogin;
        private Label lblError;
        private ProgressBar pbUpdate;
        private Label lblUpdateStatus;

        public FrmLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "تسجيل الدخول - " + AppConfig.CompanyName + " | الإصدار: " + UpdateManager.CurrentVersion;
            this.Size = new Size(480, 610);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Theme.Primary;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Icon extract failed: " + ex.Message); }

            // Logo/Header panel
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Theme.Primary };

            // \u2500\u2500 \u0645\u062d\u0627\u0648\u0644\u0629 \u062a\u062d\u0645\u064a\u0644 \u0644\u0648\u062c\u0648 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062c \u0623\u0648\u0644\u0627\u064b \u0645\u0646 \u0645\u0633\u0627\u0631 \u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
            Control logoControl;
            string logoPath = AppConfig.ShopLogoPath;
            bool hasLogo = !string.IsNullOrEmpty(logoPath) && System.IO.File.Exists(logoPath);
            if (hasLogo)
            {
                try
                {
                    var pb = new PictureBox
                    {
                        Size = new Size(480, 100),
                        Top = 5,
                        Left = 0,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.Transparent
                    };
                    pb.Image = Image.FromFile(logoPath);
                    logoControl = pb;
                }
                catch
                {
                    hasLogo = false;
                    logoControl = new Label
                    {
                        Text = "\ud83d\ude9a",
                        Font = new Font("Segoe UI Emoji", 50f),
                        ForeColor = Theme.Accent,
                        AutoSize = false,
                        Size = new Size(480, 90),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Top = 10
                    };
                }
            }
            else
            {
                logoControl = new Label
                {
                    Text = "\ud83d\ude9a",
                    Font = new Font("Segoe UI Emoji", 50f),
                    ForeColor = Theme.Accent,
                    AutoSize = false,
                    Size = new Size(480, 90),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Top = 10
                };
            }

            var lblTitle = new Label
            {
                Text = AppConfig.CompanyName,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(480, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 100
            };
            var lblSub = new Label
            {
                Text = "\u0646\u0638\u0627\u0645 \u0627\u0644\u0645\u0628\u064a\u0639\u0627\u062a \u0648\u0627\u0644\u062a\u0648\u0632\u064a\u0639",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(180, 220, 255),
                AutoSize = false,
                Size = new Size(480, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 144
            };
            pnlTop.Controls.AddRange(new Control[] { logoControl, lblTitle, lblSub });


            // White card panel
            var pnlCard = new Panel
            {
                BackColor = Color.White,
                Size = new Size(360, 300),
                Location = new Point(60, 200),
                Padding = new Padding(30)
            };
            pnlCard.Paint += (s, e) => {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            };

            int y = 30;

            var lblUserLbl = new Label { Text = "اسم المستخدم", Font = Theme.FontBold, ForeColor = Theme.TextDark, Location = new Point(20, y), AutoSize = false, Width = 320, Height = 22, TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
            y += 25;
            txtUser = new TextBox { Location = new Point(20, y), Width = 320, Height = 36, Font = Theme.FontNormal, BorderStyle = BorderStyle.FixedSingle, RightToLeft = RightToLeft.Yes };
            y += 50;

            var lblPassLbl = new Label { Text = "كلمة المرور", Font = Theme.FontBold, ForeColor = Theme.TextDark, Location = new Point(20, y), AutoSize = false, Width = 320, Height = 22, TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
            y += 25;
            txtPass = new TextBox { Location = new Point(20, y), Width = 320, Height = 36, Font = Theme.FontNormal, BorderStyle = BorderStyle.FixedSingle, PasswordChar = '*', RightToLeft = RightToLeft.Yes };
            y += 55;

            lblError = new Label { Location = new Point(20, y), Width = 320, Height = 22, Font = Theme.FontSmall, ForeColor = Theme.Danger, TextAlign = ContentAlignment.MiddleCenter };
            y += 28;

            btnLogin = new Button
            {
                Text = "تسجيل الدخول",
                Location = new Point(20, y),
                Width = 320,
                Height = 44,
                Font = Theme.FontBold,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;
            txtPass.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(null, null); };

            pnlCard.Controls.AddRange(new Control[] { lblUserLbl, txtUser, lblPassLbl, txtPass, lblError, btnLogin });

            var lblFooter = new Label
            {
                Text = "© 2025 - " + AppConfig.CompanyName,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(150, 180, 210),
                AutoSize = false,
                Size = new Size(480, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 520)
            };

            // ── شريط تقدم التحديث ─────────────────────────────────────
            lblUpdateStatus = new Label
            {
                Text = $"⟳  جاري فحص التحديثات...   |   الإصدار الحالي: v{UpdateManager.CurrentVersion}",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(160, 210, 255),
                AutoSize = false,
                Size = new Size(480, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 548)
            };

            pbUpdate = new ProgressBar
            {
                Location = new Point(0, 569),
                Size = new Size(480, 7),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 25,
                ForeColor = Color.FromArgb(100, 180, 255)
            };

            this.Controls.AddRange(new Control[] { pnlTop, pnlCard, lblFooter, lblUpdateStatus, pbUpdate });

            this.Shown += FrmLogin_Shown;
        }

        // ── فحص التحديثات في الخلفية عند فتح الشاشة ───────────────────
        private void FrmLogin_Shown(object sender, EventArgs e)
        {
            var bw = new BackgroundWorker();
            bw.DoWork += (s, ev) =>
            {
                try
                {
                    System.Threading.Thread.Sleep(600);
                    System.Net.ServicePointManager.SecurityProtocol =
                        System.Net.SecurityProtocolType.Tls12 |
                        System.Net.SecurityProtocolType.Tls11 |
                        System.Net.SecurityProtocolType.Tls |
                        (System.Net.SecurityProtocolType)12288;

                    using (var client = new System.Net.WebClient())
                    {
                        client.Encoding = System.Text.Encoding.UTF8;
                        client.Headers.Add("User-Agent", "ChickenDist/" + UpdateManager.CurrentVersion);
                        string cacheBusted = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/update.txt"
                                           + "?t=" + DateTime.Now.Ticks;
                        string raw = client.DownloadString(cacheBusted).TrimStart('\uFEFF');
                        string remoteVer = "";
                        foreach (var line in raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            int idx = line.IndexOf('=');
                            if (idx > 0 && line.Substring(0, idx).Trim().ToLower() == "version")
                            {
                                remoteVer = line.Substring(idx + 1).Trim();
                                break;
                            }
                        }
                        ev.Result = remoteVer;
                    }
                }
                catch { ev.Result = ""; }
            };

            bw.RunWorkerCompleted += (s, ev) =>
            {
                if (this.IsDisposed) return;
                pbUpdate.Style = ProgressBarStyle.Continuous;
                pbUpdate.Value = 100;

                string remoteVer = ev.Result as string ?? "";
                if (!string.IsNullOrEmpty(remoteVer))
                {
                    try
                    {
                        var local  = new Version(UpdateManager.CurrentVersion);
                        var remote = new Version(remoteVer);
                        if (remote > local)
                        {
                            lblUpdateStatus.Text = $"🔄  تحديث جديد متاح: v{remoteVer}  — سيظهر بعد تسجيل الدخول";
                            lblUpdateStatus.ForeColor = Color.FromArgb(255, 220, 80);
                            pbUpdate.ForeColor = Color.FromArgb(255, 200, 50);
                            return;
                        }
                    }
                    catch { }
                }
                lblUpdateStatus.Text = $"✅  أحدث إصدار مثبت: v{UpdateManager.CurrentVersion}";
                lblUpdateStatus.ForeColor = Color.FromArgb(100, 230, 150);
            };

            bw.RunWorkerAsync();
        }

        private int _failedAttempts = 0;
        private const int MaxAttempts = 5;
        private System.Windows.Forms.Timer _lockTimer;
        private int _lockSecondsLeft = 0;

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (!btnLogin.Enabled) return;

            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
            {
                lblError.Text = "يرجى إدخال اسم المستخدم وكلمة المرور";
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "جاري الدخول...";

            var row = EmployeeDAL.Login(txtUser.Text.Trim(), txtPass.Text);
            if (row == null)
            {
                _failedAttempts++;
                if (_failedAttempts >= MaxAttempts)
                {
                    // قفل مؤقت 30 ثانية بعد 5 محاولات فاشلة
                    _lockSecondsLeft = 30;
                    lblError.Text = $"تم تجاوز عدد المحاولات. انتظر {_lockSecondsLeft} ثانية.";
                    _lockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                    _lockTimer.Tick += (ts, te) =>
                    {
                        _lockSecondsLeft--;
                        if (_lockSecondsLeft <= 0)
                        {
                            _lockTimer.Stop();
                            _failedAttempts = 0;
                            btnLogin.Enabled = true;
                            btnLogin.Text = "تسجيل الدخول";
                            lblError.Text = "";
                        }
                        else
                        {
                            lblError.Text = $"تم تجاوز عدد المحاولات. انتظر {_lockSecondsLeft} ثانية.";
                        }
                    };
                    _lockTimer.Start();
                }
                else
                {
                    lblError.Text = $"اسم المستخدم أو كلمة المرور غير صحيحة ({_failedAttempts}/{MaxAttempts})";
                    btnLogin.Enabled = true;
                    btnLogin.Text = "تسجيل الدخول";
                }
                return;
            }

            _failedAttempts = 0;
            Session.EmpID = (int)row["EmpID"];
            Session.EmpName = row["EmpName"].ToString();
            Session.UserName = row["UserName"].ToString();
            Session.Role = row["Role"].ToString();
            // FIX: استخدام Convert.ToBoolean بدلاً من cast مباشر — يتجنب InvalidCastException
            // عند قيم NULL أو أنواع غير متوقعة في بيانات قديمة
            Session.IsDriver = row["IsDriver"] != DBNull.Value && Convert.ToBoolean(row["IsDriver"]);

            Session.DefaultSafeID = row.Table.Columns.Contains("DefaultSafeID") && row["DefaultSafeID"] != DBNull.Value ? (int?)Convert.ToInt32(row["DefaultSafeID"]) : null;
            Session.AllowedSafeIDs = row.Table.Columns.Contains("AllowedSafeIDs") && row["AllowedSafeIDs"] != DBNull.Value ? row["AllowedSafeIDs"].ToString() : "";
            Session.CanSellCash = !row.Table.Columns.Contains("CanSellCash") || row["CanSellCash"] == DBNull.Value || Convert.ToBoolean(row["CanSellCash"]);
            Session.CanSellCredit = !row.Table.Columns.Contains("CanSellCredit") || row["CanSellCredit"] == DBNull.Value || Convert.ToBoolean(row["CanSellCredit"]);
            Session.CanSellDriverLoad = !row.Table.Columns.Contains("CanSellDriverLoad") || row["CanSellDriverLoad"] == DBNull.Value || Convert.ToBoolean(row["CanSellDriverLoad"]);
            Session.CanSellInstallment = !row.Table.Columns.Contains("CanSellInstallment") || row["CanSellInstallment"] == DBNull.Value || Convert.ToBoolean(row["CanSellInstallment"]);

            Session.LoadPermissions(Session.EmpID);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

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
            this.BackColor = Color.FromArgb(15, 22, 35); // Sleek modern dark container
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Icon extract failed: " + ex.Message); }

            // Logo/Header panel (Matches exact logo background #0A0A0A so square box vanishes seamlessly!)
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Color.FromArgb(10, 10, 10), Padding = new Padding(10, 5, 10, 5) };
            
            PictureBox pbLogo = null;
            Label lblLogo = null;
            Image logoImg = null;

            if (!string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(AppConfig.ShopLogoPath);
                    using (var ms = new System.IO.MemoryStream(bytes))
                    {
                        logoImg = Image.FromStream(ms);
                    }
                }
                catch { }
            }

            if (logoImg == null)
            {
                logoImg = Theme.GetCompanyLogo();
            }

            if (logoImg != null)
            {
                pbLogo = new PictureBox
                {
                    Image = logoImg,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(440, 115), // Spans full width of the login window seamlessly
                    Location = new Point(20, 5),
                    BackColor = Color.Transparent
                };
            }
            else
            {
                lblLogo = new Label
                {
                    Text = "🚚",
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
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 198, 35), // Gold
                AutoSize = false,
                Size = new Size(480, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 122
            };
            var lblSub = new Label
            {
                Text = "نظام المبيعات والتوزيع المالي المتكامل",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(180, 200, 225),
                AutoSize = false,
                Size = new Size(480, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 152
            };

            if (pbLogo != null)
                pnlTop.Controls.AddRange(new Control[] { pbLogo, lblTitle, lblSub });
            else
                pnlTop.Controls.AddRange(new Control[] { lblLogo, lblTitle, lblSub });

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
            
            txtUser.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    txtPass.Focus();
                }
            };
            
            txtPass.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnLogin_Click(null, null);
                }
            };

            pnlCard.Controls.AddRange(new Control[] { lblUserLbl, txtUser, lblPassLbl, txtPass, lblError, btnLogin });

            var lblFooter = new Label
            {
                Text = "© 2025 - " + AppConfig.CompanyName,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(150, 180, 210),
                AutoSize = false,
                Size = new Size(480, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 510)
            };

            // ── شريط عرض الإصدار الحالي ─────────────────────────────────────
            lblUpdateStatus = new Label
            {
                Text = $"🎯 إصدار البرنامج: v{UpdateManager.CurrentVersion} (انقر للفحص أو التحديث)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 211, 252),
                AutoSize = false,
                Size = new Size(480, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 532),
                Cursor = Cursors.Hand
            };
            lblUpdateStatus.MouseEnter += (s, e) => { lblUpdateStatus.ForeColor = Color.FromArgb(56, 189, 248); };
            lblUpdateStatus.MouseLeave += (s, e) => { lblUpdateStatus.ForeColor = Color.FromArgb(125, 211, 252); };
            lblUpdateStatus.Click += delegate
            {
                if (!DbHelper.CheckAndEnforceVersion(UpdateManager.CurrentVersion))
                {
                    return;
                }
                UpdateManager.CheckForUpdates(showNoUpdateMsg: true);
            };

            var btnUpdateFromMain = new Button
            {
                Text = "🔄 فحص وتحديث الإصدار من السيرفر الرئيسي",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(260, 28),
                Location = new Point(110, 556),
                Cursor = Cursors.Hand
            };
            btnUpdateFromMain.FlatAppearance.BorderSize = 0;
            btnUpdateFromMain.Click += delegate
            {
                if (!DbHelper.CheckAndEnforceVersion(UpdateManager.CurrentVersion))
                {
                    return;
                }
                UpdateManager.CheckForUpdates(showNoUpdateMsg: true);
            };

            pbUpdate = new ProgressBar
            {
                Location = new Point(0, 586),
                Size = new Size(480, 4),
                Visible = false
            };

            this.Controls.AddRange(new Control[] { pnlTop, pnlCard, lblFooter, lblUpdateStatus, btnUpdateFromMain, pbUpdate });
        }

        private int _failedAttempts = 0;
        private const int MaxAttempts = 5;
        private System.Windows.Forms.Timer _lockTimer;
        private int _lockSecondsLeft = 0;

        private void BtnLogin_Click(object sender, EventArgs e)
        {

            if (!btnLogin.Enabled) return;

            if (!DbHelper.CheckAndEnforceVersion(UpdateManager.CurrentVersion))
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "تسجيل الدخول";
                return;
            }

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
            Session.CanSellVisa = !row.Table.Columns.Contains("CanSellVisa") || row["CanSellVisa"] == DBNull.Value || Convert.ToBoolean(row["CanSellVisa"]);
            Session.CanSellDriverLoad = !row.Table.Columns.Contains("CanSellDriverLoad") || row["CanSellDriverLoad"] == DBNull.Value || Convert.ToBoolean(row["CanSellDriverLoad"]);
            Session.CanSellInstallment = !row.Table.Columns.Contains("CanSellInstallment") || row["CanSellInstallment"] == DBNull.Value || Convert.ToBoolean(row["CanSellInstallment"]);

            Session.LoadPermissions(Session.EmpID);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

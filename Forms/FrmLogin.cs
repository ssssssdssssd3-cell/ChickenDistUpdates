using System;
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

        public FrmLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "تسجيل الدخول - " + AppConfig.CompanyName;
            // تصغير ارتفاع Login ليناسب 1366x768
            int loginH = ScreenHelper.IsSmallScreen ? 500 : 580;
            this.Size = new Size(480, loginH);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.BackColor = Theme.Primary;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // Logo/Header panel
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Theme.Primary };
            var lblLogo = new Label
            {
                Text = "🚚",
                Font = new Font("Segoe UI Emoji", 50f),
                ForeColor = Theme.Accent,
                AutoSize = false,
                Size = new Size(480, 90),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 10
            };
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
                Text = "نظام المبيعات والتوزيع",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(180, 220, 255),
                AutoSize = false,
                Size = new Size(480, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 144
            };
            pnlTop.Controls.AddRange(new Control[] { lblLogo, lblTitle, lblSub });

            // White card panel
            var pnlCard = new Panel
            {
                BackColor = Color.White,
                Size = new Size(360, 300),
                Location = new Point(60, 200),
                Padding = new Padding(30)
            };
            // Round corners effect
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

            this.Controls.AddRange(new Control[] { pnlTop, pnlCard, lblFooter });
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
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
                lblError.Text = "اسم المستخدم أو كلمة المرور غير صحيحة";
                btnLogin.Enabled = true;
                btnLogin.Text = "تسجيل الدخول";
                return;
            }

            Session.EmpID = (int)row["EmpID"];
            Session.EmpName = row["EmpName"].ToString();
            Session.UserName = row["UserName"].ToString();
            Session.Role = row["Role"].ToString();
            Session.IsDriver = (bool)row["IsDriver"];
            Session.LoadPermissions(Session.EmpID);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

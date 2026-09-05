using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة بيانات المؤسسة والفرع والهوية التجارية
    /// </summary>
    public class FrmCompanySettings : Form
    {
        private TextBox txtCompanyName;
        private TextBox txtCompanyPhone1;
        private TextBox txtCompanyPhone2;
        private TextBox txtCompanyAddress;
        private TextBox txtShopLogoPath;
        private PictureBox picLogoPreview;
        private CheckBox chkPrintShopLogo;
        private ComboBox cboBusinessType;
        private Button btnUnlockBizType;
        private ComboBox cboAppTheme;

        public FrmCompanySettings()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "🏢 بيانات المؤسسة والفرع";
            this.Size = new Size(620, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("🏢 بيانات المؤسسة والفرع", "تعديل اسم الشركة، أرقام الهواتف، العنوان، الشعار، ونوع النشاط التجاري");
            this.Controls.Add(pnlTop);

            var pnlBody = new Panel
            {
                Location = new Point(15, 75),
                Size = new Size(575, 515),
                AutoScroll = true,
                BackColor = Theme.BgMain
            };
            this.Controls.Add(pnlBody);

            int y = 10;

            // ── اسم الشركة ──────────────────────────────────────
            AddLabel(pnlBody, "اسم الشركة / المؤسسة (يظهر في أعلى الفواتير والتقارير):", 15, y);
            y += 24;
            txtCompanyName = new TextBox
            {
                Location = new Point(15, y),
                Width = 530,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
            };
            txtCompanyName.Text = AppConfig.CompanyName;
            pnlBody.Controls.Add(txtCompanyName);
            y += 38;

            // ── أرقام الهواتف ───────────────────────────────────
            AddLabel(pnlBody, "هاتف المؤسسة 1:", 15, y);
            AddLabel(pnlBody, "هاتف المؤسسة 2:", 290, y);
            y += 24;

            txtCompanyPhone1 = new TextBox
            {
                Location = new Point(15, y),
                Width = 255,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f)
            };
            txtCompanyPhone1.Text = AppConfig.CompanyPhone1;
            pnlBody.Controls.Add(txtCompanyPhone1);

            txtCompanyPhone2 = new TextBox
            {
                Location = new Point(290, y),
                Width = 255,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f)
            };
            txtCompanyPhone2.Text = AppConfig.CompanyPhone2;
            pnlBody.Controls.Add(txtCompanyPhone2);
            y += 38;

            // ── عنوان الشركة ──────────────────────────────────────
            AddLabel(pnlBody, "عنوان المؤسسة / المقر:", 15, y);
            y += 24;
            txtCompanyAddress = new TextBox
            {
                Location = new Point(15, y),
                Width = 530,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f)
            };
            txtCompanyAddress.Text = AppConfig.CompanyAddress;
            pnlBody.Controls.Add(txtCompanyAddress);
            y += 40;

            // ── شعار المؤسسة ──────────────────────────────────────
            AddLabel(pnlBody, "شعار المؤسسة / المحل (Logo):", 15, y);
            y += 24;

            txtShopLogoPath = new TextBox
            {
                Location = new Point(15, y),
                Width = 330,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtShopLogoPath.Text = AppConfig.ShopLogoPath;
            txtShopLogoPath.TextChanged += (s, e) => UpdateLogoPreview();
            pnlBody.Controls.Add(txtShopLogoPath);

            var btnBrowseLogo = Theme.MakeButton("📂 تصفح الشعار", 355, y - 2, 105, 30, Color.FromArgb(55, 65, 81));
            btnBrowseLogo.Font = new Font("Segoe UI", 9f);
            btnBrowseLogo.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*";
                    dlg.Title = "اختر شعار المؤسسة";
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        txtShopLogoPath.Text = dlg.FileName;
                    }
                }
            };
            pnlBody.Controls.Add(btnBrowseLogo);

            var btnClearLogo = Theme.MakeButton("❌ مسح", 468, y - 2, 75, 30, Color.FromArgb(120, 50, 50));
            btnClearLogo.Font = new Font("Segoe UI", 9f);
            btnClearLogo.Click += (s, e) => { txtShopLogoPath.Text = ""; };
            pnlBody.Controls.Add(btnClearLogo);
            y += 36;

            // صورة المعاينة المصغرة للشعار
            picLogoPreview = new PictureBox
            {
                Location = new Point(15, y),
                Size = new Size(110, 75),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Theme.BgCard
            };
            pnlBody.Controls.Add(picLogoPreview);

            chkPrintShopLogo = new CheckBox
            {
                Text = "طباعة شعار المؤسسة في أعلى الفواتير والريسيت",
                Location = new Point(140, y + 20),
                Size = new Size(400, 24),
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f),
                Checked = AppConfig.PrintShopLogo
            };
            pnlBody.Controls.Add(chkPrintShopLogo);
            UpdateLogoPreview();
            y += 88;

            // ── نوع النشاط التجاري ───────────────────────────────────
            AddLabel(pnlBody, "نوع النشاط التجاري (لتخصيص الخانات والحقول المناسبة):", 15, y);
            y += 24;

            cboBusinessType = new ComboBox
            {
                Location = new Point(15, y),
                Width = 380,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f),
                Enabled = false
            };
            cboBusinessType.Items.AddRange(new object[]
            {
                "سوبر ماركت (المواد الغذائية والصلاحية)",
                "قطع غيار سيارات (أرقام قطع OEM والموديلات)",
                "موبايلات وأجهزة ذكية (IMEI، اللون، شاشة صيانة)",
                "ملابس وأحذية (المقاس، اللون، الخامة)",
                "نشاط تجاري عام / تجزئة (عام بدون خانات مخصصة)",
                "غيار زيت وصيانة سيارات (أرقام لوحات، فحص، كروت صيانة)",
                "مطعم وكافيه (طاولات، تحضير مطبخ، تيك أواي، توصيل)",
                "مصانع وإنتاج (أوامر تشغيل، خطوط إنتاج، خامات وتكاليف)"
            });
            cboBusinessType.SelectedItem = AppConfig.BusinessType switch
            {
                "SpareParts" => "قطع غيار سيارات (أرقام قطع OEM والموديلات)",
                "Mobiles" => "موبايلات وأجهزة ذكية (IMEI، اللون، شاشة صيانة)",
                "Clothing" => "ملابس وأحذية (المقاس، اللون، الخامة)",
                "CarService" => "غيار زيت وصيانة سيارات (أرقام لوحات، فحص، كروت صيانة)",
                "General" => "نشاط تجاري عام / تجزئة (عام بدون خانات مخصصة)",
                "Restaurant" => "مطعم وكافيه (طاولات، تحضير مطبخ، تيك أواي، توصيل)",
                "Factories" or "Manufacturing" => "مصانع وإنتاج (أوامر تشغيل، خطوط إنتاج، خامات وتكاليف)",
                _ => "سوبر ماركت (المواد الغذائية والصلاحية)"
            };
            pnlBody.Controls.Add(cboBusinessType);

            btnUnlockBizType = Theme.MakeButton("🔒 تعديل النشاط", 405, y - 2, 140, 32, Theme.Accent);
            btnUnlockBizType.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnUnlockBizType.Click += (s, e) => UnlockBusinessType();
            pnlBody.Controls.Add(btnUnlockBizType);
            y += 40;

            // ── طابع ألوان البرنامج ──────────────────────────────────
            AddLabel(pnlBody, "طابع ألوان البرنامج (الثيم المفضل):", 15, y);
            y += 24;

            cboAppTheme = new ComboBox
            {
                Location = new Point(15, y),
                Width = 530,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboAppTheme.Items.AddRange(new object[]
            {
                "داكن هادئ مريح (Dark Theme)",
                "رمادي ناعم هادئ (Slate Theme)",
                "فاتح مريح للعين (Light Theme)"
            });
            cboAppTheme.SelectedItem = AppConfig.AppTheme switch
            {
                "Light" => "فاتح مريح للعين (Light Theme)",
                "Slate" => "رمادي ناعم هادئ (Slate Theme)",
                _ => "داكن هادئ مريح (Dark Theme)"
            };
            pnlBody.Controls.Add(cboAppTheme);
            y += 45;

            // ── شريط الأزرار السفلي ─────────────────────────────────
            var pnlFooter = new Panel
            {
                Location = new Point(0, 595),
                Size = new Size(620, 55),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlFooter);

            var btnSave = Theme.MakeButton("💾 حفظ بيانات المؤسسة", 15, 8, 200, 38, Theme.Primary);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;
            pnlFooter.Controls.Add(btnSave);

            var btnCancel = Theme.MakeButton("إغلاق", 225, 8, 100, 38, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnCancel);

            Theme.ApplyFormRTL(this);
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            parent.Controls.Add(lbl);
        }

        private void UpdateLogoPreview()
        {
            try
            {
                if (picLogoPreview.Image != null)
                {
                    var oldImg = picLogoPreview.Image;
                    picLogoPreview.Image = null;
                    oldImg.Dispose();
                }

                string p = txtShopLogoPath.Text.Trim();
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read))
                    {
                        picLogoPreview.Image = Image.FromStream(fs);
                    }
                }
            }
            catch { }
        }

        private void UnlockBusinessType()
        {
            using (var passForm = new Form())
            {
                passForm.Text = "كلمة المرور مطلوبة";
                passForm.Size = new Size(340, 160);
                passForm.StartPosition = FormStartPosition.CenterParent;
                passForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                passForm.MaximizeBox = false;
                passForm.MinimizeBox = false;
                passForm.RightToLeft = RightToLeft.Yes;
                passForm.RightToLeftLayout = true;
                passForm.BackColor = Theme.BgMain;
                passForm.Font = Theme.FontMain;

                var lbl = new Label
                {
                    Text = "أدخل كلمة المرور لتعديل نوع النشاط:",
                    Dock = DockStyle.Top,
                    Height = 32,
                    TextAlign = ContentAlignment.MiddleRight,
                    Padding = new Padding(8, 5, 8, 0),
                    ForeColor = Theme.TextMain
                };
                var txt = new TextBox
                {
                    Dock = DockStyle.Top,
                    PasswordChar = '*',
                    Height = 28,
                    Font = new Font("Segoe UI", 11f),
                    RightToLeft = RightToLeft.Yes,
                    BackColor = Theme.BgInput,
                    ForeColor = Theme.TextMain
                };
                var btnOk = Theme.MakeButton("موافق", 20, 95, 120, 32, Theme.Primary);
                btnOk.Click += (s, e) => passForm.DialogResult = DialogResult.OK;

                passForm.Controls.Add(btnOk);
                passForm.Controls.Add(txt);
                passForm.Controls.Add(lbl);
                passForm.AcceptButton = btnOk;

                if (passForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (txt.Text == "Pro@soft2026")
                    {
                        cboBusinessType.Enabled = true;
                        btnUnlockBizType.Text = "🔓 مفتوح للتعديل";
                        btnUnlockBizType.BackColor = Theme.Success;
                        btnUnlockBizType.Enabled = false;
                    }
                    else
                    {
                        MessageBox.Show("كلمة المرور غير صحيحة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCompanyName.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم المؤسسة أو الشركة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCompanyName.Focus();
                return;
            }

            AppConfig.CompanyName = txtCompanyName.Text.Trim();
            AppConfig.CompanyPhone1 = txtCompanyPhone1.Text.Trim();
            AppConfig.CompanyPhone2 = txtCompanyPhone2.Text.Trim();
            AppConfig.CompanyAddress = txtCompanyAddress.Text.Trim();
            AppConfig.ShopLogoPath = txtShopLogoPath.Text.Trim();
            AppConfig.PrintShopLogo = chkPrintShopLogo.Checked;

            if (cboBusinessType.Enabled)
            {
                AppConfig.BusinessType = cboBusinessType.SelectedIndex switch
                {
                    1 => "SpareParts",
                    2 => "Mobiles",
                    3 => "Clothing",
                    4 => "General",
                    5 => "CarService",
                    6 => "Restaurant",
                    7 => "Factories",
                    _ => "Supermarket"
                };
            }

            AppConfig.AppTheme = cboAppTheme.SelectedIndex switch
            {
                1 => "Slate",
                2 => "Light",
                _ => "Dark"
            };

            MessageBox.Show(
                "✅ تم حفظ بيانات المؤسسة بنجاح!\n(ملاحظة: في حال تغيير الثيم، يُرجى إعادة تشغيل البرنامج لتطبيقه بالكامل).",
                "تم الحفظ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            if (Application.OpenForms["FrmMain"] is FrmMain main)
                main.UpdateCompanyName(AppConfig.CompanyName);

            this.Close();
        }
    }
}

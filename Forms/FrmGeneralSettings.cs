using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة خيارات النظام والتشغيل، نقاط الولاء، الورديات، والترخيص
    /// </summary>
    public class FrmGeneralSettings : Form
    {
        private CheckBox chkEnableCrates;
        private CheckBox chkLoyaltyEnabled;
        private TextBox txtLoyaltyRate;
        private TextBox txtRedemptionRate;
        private CheckBox chkShiftRequired;
        private CheckBox chkAllowSellExpired;
        private ComboBox cboWhatsAppInvoiceTemplate;

        public FrmGeneralSettings()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "⚙️ خيارات النظام والتشغيل";
            this.Size = new Size(650, 710);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("⚙️ خيارات النظام والتشغيل", "نقاط الولاء للعملاء، سياسات البيع والورديات، الفوارغ، قوالب الواتساب، وبيانات الترخيص");
            this.Controls.Add(pnlTop);

            var pnlBody = new Panel
            {
                Location = new Point(15, 75),
                Size = new Size(605, 545),
                AutoScroll = true,
                BackColor = Theme.BgMain
            };
            this.Controls.Add(pnlBody);

            int y = 10;

            // ── 1. سياسات التشغيل والبيع ─────────────────────────
            var lblSalesPolicy = new Label
            {
                Text = "🛒 سياسات البيع والكاشير والمخزون:",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlBody.Controls.Add(lblSalesPolicy);
            y += 28;

            chkShiftRequired = new CheckBox
            {
                Text = "الوردية إجبارية قبل تسجيل أي مبيعات في الكاشير (POS)",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ShiftRequired
            };
            pnlBody.Controls.Add(chkShiftRequired);
            y += 30;

            chkAllowSellExpired = new CheckBox
            {
                Text = "السماح ببيع الأصناف منتهية الصلاحية (مع إظهار تنبيه للكاشير)",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.AllowSellExpired
            };
            pnlBody.Controls.Add(chkAllowSellExpired);
            y += 30;

            chkEnableCrates = new CheckBox
            {
                Text = "تفعيل نظام تتبع الفوارغ والوزن الفارغ للعملاء (الأقفاص / الصناديق)",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.EnableCratesTracking
            };
            pnlBody.Controls.Add(chkEnableCrates);
            y += 40;

            var sep1 = new Panel { Location = new Point(15, y), Size = new Size(560, 2), BackColor = Theme.BorderColor };
            pnlBody.Controls.Add(sep1);
            y += 15;

            // ── 2. نظام نقاط الولاء ─────────────────────────────
            var lblLoyaltyTitle = new Label
            {
                Text = "🎁 نظام نقاط الولاء والمكافآت للعملاء:",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlBody.Controls.Add(lblLoyaltyTitle);
            y += 28;

            chkLoyaltyEnabled = new CheckBox
            {
                Text = "تفعيل احتساب نقاط الولاء تلقائياً للعملاء المسجلين عند الشراء",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.LoyaltyEnabled
            };
            pnlBody.Controls.Add(chkLoyaltyEnabled);
            y += 32;

            AddLabel(pnlBody, "كل كم جنيه مشتريات = 1 نقطة:", 15, y);
            AddLabel(pnlBody, "قيمة النقطة الواحدة عند الخصم والاسترداد:", 290, y);
            y += 24;

            txtLoyaltyRate = new TextBox
            {
                Location = new Point(15, y),
                Width = 255,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Text = AppConfig.LoyaltyPointsPerCurrency.ToString()
            };
            pnlBody.Controls.Add(txtLoyaltyRate);

            txtRedemptionRate = new TextBox
            {
                Location = new Point(290, y),
                Width = 255,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Text = AppConfig.LoyaltyRedemptionRate.ToString()
            };
            pnlBody.Controls.Add(txtRedemptionRate);
            y += 42;

            var sep2 = new Panel { Location = new Point(15, y), Size = new Size(560, 2), BackColor = Theme.BorderColor };
            pnlBody.Controls.Add(sep2);
            y += 15;

            // ── 3. قوالب إرسال الفواتير عبر الواتساب ───────────────
            var lblWhatsAppTitle = new Label
            {
                Text = "💬 قوالب إرسال كروت الفواتير عبر الواتساب للعملاء:",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlBody.Controls.Add(lblWhatsAppTitle);
            y += 28;

            AddLabel(pnlBody, "النموذج الافتراضي المعتمد لمشاركة الفاتورة بالواتس:", 15, y);
            y += 24;

            cboWhatsAppInvoiceTemplate = new ComboBox
            {
                Location = new Point(15, y),
                Width = 380,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboWhatsAppInvoiceTemplate.Items.AddRange(new object[]
            {
                "🖼️ كارت الفاتورة الكلاسيكي الملكي (Royal Navy Card)",
                "🖼️ كارت الفاتورة المودرن الفحمي (Modern Charcoal Card)",
                "🖼️ كارت الفاتورة الشبكي التجاري (Commercial Grid Card)",
                "🖼️ كارت الفاتورة الزمردي الأنيق (Emerald Green Card)",
                "🖼️ كارت الفاتورة الذهبي للشركات (Corporate Gold Card)",
                "🖼️ كارت فاتورة الطارق هوم (Al Tarek Home Grid Card)",
                "💬 النموذج التفصيلي الشامل (رسالة نصية تفصيلية)",
                "💬 النموذج السريع الموجز (رسالة نصية سريعة)",
                "💬 نموذج كشف الحساب والمالية (رسالة نصية مالية)",
                "💬 نموذج الطارق المعتمد (رسالة نصية الطارق)"
            });
            cboWhatsAppInvoiceTemplate.SelectedIndex = AppConfig.WhatsAppInvoiceTemplate switch
            {
                "ImageCardModern" => 1,
                "ImageCardCommercial" => 2,
                "ImageCardEmerald" => 3,
                "ImageCardGold" => 4,
                "ImageCardAlTarek" or "AlTarek" or "AlTarekGrid" or "AlTarekHome" => 5,
                "Detailed" => 6,
                "Summary" => 7,
                "Financial" => 8,
                "AlTarekText" => 9,
                _ => 0
            };
            pnlBody.Controls.Add(cboWhatsAppInvoiceTemplate);

            var btnPreviewWhatsApp = Theme.MakeButton("👁️ معاينة القالب", 405, y - 2, 140, 32, Color.FromArgb(37, 211, 102));
            btnPreviewWhatsApp.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPreviewWhatsApp.Click += (s, e) => PreviewWhatsAppTemplate();
            pnlBody.Controls.Add(btnPreviewWhatsApp);
            y += 45;

            var sep3 = new Panel { Location = new Point(15, y), Size = new Size(560, 2), BackColor = Theme.BorderColor };
            pnlBody.Controls.Add(sep3);
            y += 15;

            // ── 4. معلومات ترخيص البرنامج ─────────────────────────
            var lblLicTitle = new Label
            {
                Text = "🔑 معلومات ترخيص البرنامج وتفعيل الجهاز:",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlBody.Controls.Add(lblLicTitle);
            y += 28;

            string machineId = LicenseManager.GetCurrentMachineId();
            string hddSerial = LicenseManager.GetCurrentHddSerial();
            string expiryTxt = LicenseManager.IsActivated
                ? (LicenseManager.ExpiryDate == DateTime.MaxValue
                    ? "✅ ترخيص دائم ومفعل"
                    : $"✅ مفعل وصالح حتى: {LicenseManager.ExpiryDate:yyyy-MM-dd}")
                : "⛔ غير مفعل";

            var lblLicInfo = new Label
            {
                Text = $"الحالة: {expiryTxt}\n" +
                       $"اسم الجهاز: {LicenseManager.DeviceName}\n" +
                       $"معرف المعالج (Machine ID): {machineId}\n" +
                       $"سيريال الهارد (HDD Serial):   {hddSerial}",
                Location = new Point(15, y),
                AutoSize = false,
                Width = 380,
                Height = 75,
                Font = new Font("Consolas", 9.5f),
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Theme.BgInput,
                Padding = new Padding(6)
            };
            pnlBody.Controls.Add(lblLicInfo);

            var btnCopyIds = Theme.MakeButton("📋 نسخ المعرفات", 405, y, 140, 34, Color.FromArgb(55, 65, 81));
            btnCopyIds.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnCopyIds.Click += (s, e) =>
            {
                string info = $"Machine ID: {machineId}\nHDD Serial: {hddSerial}";
                Clipboard.SetText(info);
                MessageBox.Show("✅ تم نسخ معرفات الجهاز للحافظة بنجاح!\nأرسلها للدعم الفني لتوليد كود التفعيل.", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlBody.Controls.Add(btnCopyIds);

            var btnActivateLic = Theme.MakeButton("🔑 تفعيل الترخيص", 405, y + 40, 140, 34, Theme.Success);
            btnActivateLic.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnActivateLic.Click += (s, e) =>
            {
                using (var dlg = new FrmActivation(""))
                {
                    dlg.ShowDialog(this);
                }
            };
            pnlBody.Controls.Add(btnActivateLic);

            // ── شريط الأزرار السفلي ─────────────────────────────────
            var pnlFooter = new Panel
            {
                Location = new Point(0, 625),
                Size = new Size(650, 55),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlFooter);

            var btnSave = Theme.MakeButton("💾 حفظ خيارات النظام", 15, 8, 200, 38, Theme.Primary);
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

        private void PreviewWhatsAppTemplate()
        {
            string tplKey = cboWhatsAppInvoiceTemplate.SelectedIndex switch
            {
                1 => "ImageCardModern",
                2 => "ImageCardCommercial",
                3 => "ImageCardEmerald",
                4 => "ImageCardGold",
                5 => "ImageCardAlTarek",
                6 => "Detailed",
                7 => "Summary",
                8 => "Financial",
                9 => "AlTarekText",
                _ => "ImageCardNavy"
            };

            using (var dlg = new FrmWhatsAppPreviewDialog(tplKey))
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            AppConfig.EnableCratesTracking = chkEnableCrates.Checked;
            AppConfig.ShiftRequired = chkShiftRequired.Checked;
            AppConfig.AllowSellExpired = chkAllowSellExpired.Checked;

            AppConfig.LoyaltyEnabled = chkLoyaltyEnabled.Checked;
            if (decimal.TryParse(txtLoyaltyRate.Text, out decimal lr)) AppConfig.LoyaltyPointsPerCurrency = lr;
            if (decimal.TryParse(txtRedemptionRate.Text, out decimal rr)) AppConfig.LoyaltyRedemptionRate = rr;

            AppConfig.WhatsAppInvoiceTemplate = cboWhatsAppInvoiceTemplate.SelectedIndex switch
            {
                1 => "ImageCardModern",
                2 => "ImageCardCommercial",
                3 => "ImageCardEmerald",
                4 => "ImageCardGold",
                5 => "ImageCardAlTarek",
                6 => "Detailed",
                7 => "Summary",
                8 => "Financial",
                9 => "AlTarekText",
                _ => "ImageCardNavy"
            };

            MessageBox.Show("✅ تم حفظ خيارات النظام والتشغيل بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}

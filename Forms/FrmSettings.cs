using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// لوحة تحكم الإعدادات الشاملة (Settings Dashboard Hub)
    /// تتيح الوصول السريع لجميع أقسام الإعدادات المقسمة بسهولة ووضوح
    /// </summary>
    public class FrmSettings : Form
    {
        public FrmSettings() : this("") { }

        public FrmSettings(string initialSection = "")
        {
            InitializeComponentCustom();

            if (!string.IsNullOrEmpty(initialSection))
            {
                this.Shown += (s, e) =>
                {
                    OpenSection(initialSection);
                };
            }
        }

        private void InitializeComponentCustom()
        {
            this.Text = "⚙️ لوحة تحكم الإعدادات العامة";
            this.Size = new Size(820, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("⚙️ لوحة تحكم الإعدادات العامة", "اختر القسم المطلوب لضبط وتخصيص إعدادات المؤسسة، الطابعات، النسخ الاحتياطي، الموازين، أو خيارات النظام");
            this.Controls.Add(pnlTop);

            var pnlBody = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(765, 465),
                AutoScroll = true,
                BackColor = Theme.BgMain
            };
            this.Controls.Add(pnlBody);

            // إضافة كروت الأقسام الرئيسية
            int cardW = 365;
            int cardH = 135;

            // 1. كارت بيانات المؤسسة والفرع (صف 1 - يمين)
            var cardCompany = CreateSettingsCard(
                "🏢 بيانات المؤسسة والفرع",
                "تعديل اسم الشركة، أرقام الهواتف، العنوان، شعار المحل، نوع النشاط التجاري، وثيم ألوان البرنامج.",
                Theme.Primary,
                () => OpenSection("company"),
                20, 15, cardW, cardH
            );
            pnlBody.Controls.Add(cardCompany);

            // 2. كارت إعدادات الطابعات والفواتير (صف 1 - شمال)
            var cardPrinters = CreateSettingsCard(
                "🖨️ إعدادات الطابعات ونماذج الفواتير",
                "تخصيص طابعة الريسيت وA4 والباركود، مقاسات الفواتير والتقارير، قوالب الفواتير، محاذاة A5، وأذونات التحضير.",
                Color.FromArgb(14, 165, 233), // Sky Blue
                () => OpenSection("printers"),
                395, 15, cardW, cardH
            );
            pnlBody.Controls.Add(cardPrinters);

            // 3. كارت النسخ الاحتياطي والأرشفة (صف 2 - يمين)
            var cardBackup = CreateSettingsCard(
                "💾 النسخ الاحتياطي والأرشفة",
                "إدارة مسار النسخ الاحتياطية، النسخ التلقائي عند الخروج، الجدولة الدورية، المجلد السحابي، والواتساب.",
                Color.FromArgb(16, 185, 129), // Emerald
                () => OpenSection("backup"),
                20, 165, cardW, cardH
            );
            pnlBody.Controls.Add(cardBackup);

            // 4. كارت إعدادات الموازين والأجهزة (صف 2 - شمال)
            var cardScales = CreateSettingsCard(
                "⚖️ إعدادات الموازين والأجهزة",
                "إعداد واختبار الميزان الإلكتروني اللحظي للكاشير (COM Port) وضبط بادئة وأطوال ميزان الباركود الملصق.",
                Color.FromArgb(245, 158, 11), // Amber
                () => OpenSection("scales"),
                395, 165, cardW, cardH
            );
            pnlBody.Controls.Add(cardScales);

            // 5. كارت خيارات النظام والتشغيل (صف 3 - يمين)
            var cardGeneral = CreateSettingsCard(
                "⚙️ خيارات النظام والتشغيل",
                "إدارة نقاط الولاء، إلزامية الورديات، تتبع الفوارغ، بيع منتهي الصلاحية، قوالب الواتساب، وبيانات الترخيص.",
                Color.FromArgb(139, 92, 246), // Purple
                () => OpenSection("general"),
                20, 315, cardW, cardH
            );
            pnlBody.Controls.Add(cardGeneral);

            // 6. كارت تفعيل الترخيص (صف 3 - شمال)
            var cardLicense = CreateSettingsCard(
                "🔑 ترخيص وتفعيل البرنامج",
                "عرض معرفات الجهاز (Machine ID و HDD Serial)، نسخ بيانات التفعيل، وإدخال سيريال ترخيص العميل.",
                Color.FromArgb(225, 29, 72), // Rose
                () => {
                    using (var dlg = new FrmActivation(""))
                    {
                        dlg.ShowDialog(this);
                    }
                },
                395, 315, cardW, cardH
            );
            pnlBody.Controls.Add(cardLicense);

            // ── شريط الأزرار السفلي ─────────────────────────────────
            var pnlFooter = new Panel
            {
                Location = new Point(0, 550),
                Size = new Size(820, 55),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlFooter);

            var lblFooterTip = new Label
            {
                Text = "💡 يمكنك أيضاً الوصول المباشر لأي قسم من هذه الإعدادات من قائمة «الإدارة» بالشاشة الرئيسية.",
                Location = new Point(20, 16),
                AutoSize = true,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f)
            };
            pnlFooter.Controls.Add(lblFooterTip);

            var btnClose = Theme.MakeButton("إغلاق", 690, 8, 100, 38, Color.FromArgb(100, 110, 120));
            btnClose.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnClose);

            Theme.ApplyFormRTL(this);
        }

        private Panel CreateSettingsCard(string title, string desc, Color accentColor, Action onClick, int x, int y, int w, int h)
        {
            var pnlCard = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };

            // شريط جانبي ملون للبطاقة
            var pnlStripe = new Panel
            {
                Location = new Point(w - 6, 0),
                Size = new Size(6, h),
                BackColor = accentColor
            };
            pnlCard.Controls.Add(pnlStripe);

            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(15, 12),
                Size = new Size(w - 35, 24),
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Cursor = Cursors.Hand
            };
            pnlCard.Controls.Add(lblTitle);

            var lblDesc = new Label
            {
                Text = desc,
                Location = new Point(15, 40),
                Size = new Size(w - 35, 45),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSub,
                Cursor = Cursors.Hand
            };
            pnlCard.Controls.Add(lblDesc);

            var btnOpen = Theme.MakeButton("فتح الإعدادات ⬅", 15, 92, 130, 30, accentColor);
            btnOpen.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnOpen.Click += (s, e) => onClick();
            pnlCard.Controls.Add(btnOpen);

            // تفعيل النقر على كامل البطاقة
            pnlCard.Click += (s, e) => onClick();
            lblTitle.Click += (s, e) => onClick();
            lblDesc.Click += (s, e) => onClick();

            return pnlCard;
        }

        public void OpenSection(string section)
        {
            switch (section.ToLowerInvariant())
            {
                case "company":
                case "organization":
                    using (var frm = new FrmCompanySettings()) frm.ShowDialog(this);
                    break;
                case "printers":
                case "printing":
                    using (var frm = new FrmPrinterSettings()) frm.ShowDialog(this);
                    break;
                case "backup":
                    using (var frm = new FrmBackupSettings()) frm.ShowDialog(this);
                    break;
                case "scales":
                case "devices":
                    using (var frm = new FrmScaleSettings()) frm.ShowDialog(this);
                    break;
                case "general":
                case "system":
                    using (var frm = new FrmGeneralSettings()) frm.ShowDialog(this);
                    break;
                case "license":
                    using (var dlg = new FrmActivation("")) dlg.ShowDialog(this);
                    break;
            }
        }
    }
}

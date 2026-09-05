using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة النسخ الاحتياطي والأرشفة والمزامنة السحابية وبوت الواتساب
    /// </summary>
    public class FrmBackupSettings : Form
    {
        private TextBox txtBackupFolder;
        private CheckBox chkBackupOnExit;
        private ComboBox cboBackupInterval;
        private TextBox txtLocalCloudPath;
        private TextBox txtWhatsAppPhone;
        private Label lblLastBackup;
        private Panel pnlStatusCard;

        public FrmBackupSettings()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "💾 النسخ الاحتياطي والأرشفة";
            this.Size = new Size(680, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("💾 النسخ الاحتياطي والأرشفة", "إدارة مسار حفظ النسخ الاحتياطية، الجدولة الدورية التلقائية، والمزامنة السحابية والواتساب");
            this.Controls.Add(pnlTop);

            var pnlBody = new Panel
            {
                Location = new Point(15, 75),
                Size = new Size(635, 515),
                AutoScroll = true,
                BackColor = Theme.BgMain
            };
            this.Controls.Add(pnlBody);

            int y = 10;

            // ── كارت حالة آخر نسخة احتياطية ─────────────────────────
            pnlStatusCard = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(590, 65),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlBody.Controls.Add(pnlStatusCard);

            lblLastBackup = new Label
            {
                Location = new Point(15, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            pnlStatusCard.Controls.Add(lblLastBackup);

            var lblBackupTip = new Label
            {
                Text = "يُنصح دائماً بإجراء نسخة احتياطية دورية لحفظ بيانات المبيعات والحسابات بأمان تام.",
                Location = new Point(15, 38),
                AutoSize = true,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9f)
            };
            pnlStatusCard.Controls.Add(lblBackupTip);
            RefreshLastBackupLabel();
            y += 80;

            // ── مجلد حفظ النسخ الاحتياطية ───────────────────────────
            AddLabel(pnlBody, "مجلد حفظ النسخ الاحتياطية الافتراضي على القرص الصلب:", 15, y);
            y += 24;

            txtBackupFolder = new TextBox
            {
                Location = new Point(15, y),
                Width = 460,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f),
                Text = BackupManager.BackupFolder
            };
            pnlBody.Controls.Add(txtBackupFolder);

            var btnBrowse = Theme.MakeButton("📂 تصفح", 485, y - 2, 120, 32, Color.FromArgb(55, 65, 81));
            btnBrowse.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnBrowse.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "اختر مجلد حفظ النسخ الاحتياطية";
                    dlg.SelectedPath = txtBackupFolder.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        txtBackupFolder.Text = dlg.SelectedPath;
                }
            };
            pnlBody.Controls.Add(btnBrowse);
            y += 38;

            // ── خيارات النسخ التلقائي ──────────────────────────────
            chkBackupOnExit = new CheckBox
            {
                Text = "عمل نسخة احتياطية مضغوطة تلقائياً عند إغلاق البرنامج",
                Location = new Point(15, y),
                Size = new Size(500, 24),
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f),
                Checked = AppConfig.BackupOnExit
            };
            pnlBody.Controls.Add(chkBackupOnExit);
            y += 32;

            AddLabel(pnlBody, "النسخ الاحتياطي الدوري التلقائي (أثناء عمل البرنامج):", 15, y);
            y += 24;

            cboBackupInterval = new ComboBox
            {
                Location = new Point(15, y),
                Width = 460,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboBackupInterval.Items.Add(new ComboItem(0, "🛑 إيقاف النسخ الاحتياطي الدوري"));
            cboBackupInterval.Items.Add(new ComboItem(2, "⏱️ كل ساعتين (موصى به أثناء العمل)"));
            cboBackupInterval.Items.Add(new ComboItem(6, "⏱️ كل 6 ساعات"));
            cboBackupInterval.Items.Add(new ComboItem(12, "⏱️ كل 12 ساعة"));
            cboBackupInterval.Items.Add(new ComboItem(24, "⏱️ كل 24 ساعة (يومياً)"));

            cboBackupInterval.DisplayMember = "Text";
            cboBackupInterval.ValueMember = "ID";
            cboBackupInterval.SelectedIndex = 0;

            int currentInterval = AppConfig.BackupIntervalHours;
            for (int i = 0; i < cboBackupInterval.Items.Count; i++)
            {
                if (cboBackupInterval.Items[i] is ComboItem ci && ci.ID == currentInterval)
                {
                    cboBackupInterval.SelectedIndex = i;
                    break;
                }
            }
            pnlBody.Controls.Add(cboBackupInterval);
            y += 42;

            // ── مسار مجلد المزامنة السحابية المحلي ─────────────────
            AddLabel(pnlBody, "مسار مجلد سحابي محلي (Google Drive / Dropbox / OneDrive):", 15, y);
            y += 24;

            txtLocalCloudPath = new TextBox
            {
                Location = new Point(15, y),
                Width = 350,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Text = AppConfig.BackupLocalPath
            };
            pnlBody.Controls.Add(txtLocalCloudPath);

            var btnBrowseCloud = Theme.MakeButton("📂 تصفح", 375, y - 2, 85, 30, Color.FromArgb(55, 65, 81));
            btnBrowseCloud.Font = new Font("Segoe UI", 9f);
            btnBrowseCloud.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "اختر مجلد المزامنة السحابية المحلي";
                    dlg.SelectedPath = txtLocalCloudPath.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        txtLocalCloudPath.Text = dlg.SelectedPath;
                }
            };
            pnlBody.Controls.Add(btnBrowseCloud);

            var btnAutoDetectGDrive = Theme.MakeButton("☁️ كشف جوجل درايف", 470, y - 2, 135, 30, Color.FromArgb(47, 54, 64));
            btnAutoDetectGDrive.Font = new Font("Segoe UI", 9f);
            btnAutoDetectGDrive.Click += (s, e) =>
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string path1 = Path.Combine(userProfile, "Google Drive\\My Drive");
                string path2 = Path.Combine(userProfile, "Google Drive");
                if (Directory.Exists(path1))
                {
                    txtLocalCloudPath.Text = path1;
                    MessageBox.Show("تم العثور على مجلد Google Drive وتعيينه بنجاح!", "كشف تلقائي", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (Directory.Exists(path2))
                {
                    txtLocalCloudPath.Text = path2;
                    MessageBox.Show("تم العثور على مجلد Google Drive وتعيينه بنجاح!", "كشف تلقائي", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على مجلد Google Drive الافتراضي تلقائياً. يرجى تحديده يدوياً باستخدام زر التصفح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            pnlBody.Controls.Add(btnAutoDetectGDrive);
            y += 42;

            // ── إعدادات الواتساب للنسخ الاحتياطي ───────────────────
            AddLabel(pnlBody, "رقم هاتف استلام النسخ الاحتياطية عبر الواتساب (WhatsApp Backup Phone):", 15, y);
            y += 24;

            txtWhatsAppPhone = new TextBox
            {
                Location = new Point(15, y),
                Width = 430,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f),
                Text = AppConfig.WhatsAppBackupPhone
            };
            pnlBody.Controls.Add(txtWhatsAppPhone);

            var btnTestWhatsApp = Theme.MakeButton("📤 اختبار الرفع بالواتس", 455, y - 2, 150, 32, Color.FromArgb(80, 100, 60));
            btnTestWhatsApp.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnTestWhatsApp.Click += (s, e) => TestWhatsAppUpload();
            pnlBody.Controls.Add(btnTestWhatsApp);
            y += 48;

            var sep = new Panel { Location = new Point(15, y), Size = new Size(590, 2), BackColor = Theme.BorderColor };
            pnlBody.Controls.Add(sep);
            y += 15;

            // ── أزرار العمليات الفورية ──────────────────────────────
            var btnBackupNow = Theme.MakeButton("💾 عمل نسخة احتياطية الآن فوراً", 15, y, 285, 42, Theme.Success);
            btnBackupNow.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnBackupNow.Click += (s, e) =>
            {
                SaveBackupFolder();
                bool ok = BackupManager.DoBackup(silent: false);
                if (ok) RefreshLastBackupLabel();
            };
            pnlBody.Controls.Add(btnBackupNow);

            var btnOpenFolder = Theme.MakeButton("📂 استعراض وفتح مجلد الباكب", 310, y, 285, 42, Color.FromArgb(55, 65, 81));
            btnOpenFolder.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnOpenFolder.Click += (s, e) =>
            {
                SaveBackupFolder();
                BackupManager.OpenBackupFolder();
            };
            pnlBody.Controls.Add(btnOpenFolder);

            // ── شريط الأزرار السفلي ─────────────────────────────────
            var pnlFooter = new Panel
            {
                Location = new Point(0, 595),
                Size = new Size(680, 55),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlFooter);

            var btnSave = Theme.MakeButton("💾 حفظ إعدادات النسخ الاحتياطي", 15, 8, 230, 38, Theme.Primary);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;
            pnlFooter.Controls.Add(btnSave);

            var btnCancel = Theme.MakeButton("إغلاق", 255, 8, 100, 38, Color.FromArgb(100, 110, 120));
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

        private void SaveBackupFolder()
        {
            string folder = txtBackupFolder.Text.Trim();
            if (!string.IsNullOrWhiteSpace(folder))
                BackupManager.BackupFolder = folder;
        }

        private void RefreshLastBackupLabel()
        {
            var last = BackupManager.LastBackupTime;
            string lastStr = last.HasValue
                ? last.Value.ToString("dd/MM/yyyy hh:mm tt")
                : "لم يتم عمل نسخة احتياطية بعد";
            bool isOverdue = BackupManager.IsBackupOverdue();
            string overdueStr = isOverdue ? " ⚠️ متأخر!" : " ✅ حديث";

            lblLastBackup.Text = $"آخر نسخة احتياطية: {lastStr}{overdueStr}";
            lblLastBackup.ForeColor = isOverdue
                ? Color.FromArgb(239, 68, 68)
                : Color.FromArgb(34, 197, 94);
        }

        private void TestWhatsAppUpload()
        {
            if (string.IsNullOrWhiteSpace(txtWhatsAppPhone.Text))
            {
                MessageBox.Show("الرجاء إدخال رقم الهاتف أولاً للاختبار.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtWhatsAppPhone.Focus();
                return;
            }

            AppConfig.WhatsAppBackupPhone = txtWhatsAppPhone.Text.Trim();
            MessageBox.Show("جاري إنشاء نسخة احتياطية واختبار رفعها بالواتساب، يرجى الانتظار...", "جاري الاختبار", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Task.Run(() =>
            {
                bool ok = BackupManager.DoBackup(silent: true);
                this.Invoke(new Action(() =>
                {
                    if (ok)
                    {
                        MessageBox.Show("✅ تم إرسال النسخة التجريبية بالواتساب بنجاح! يرجى التحقق من هاتفك.", "نجاح الاختبار", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshLastBackupLabel();
                    }
                    else
                    {
                        MessageBox.Show("❌ فشل اختبار الرفع بالواتساب. تأكد من تشغيل البوت والاتصال بالشبكة.", "خطأ بالاختبار", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }));
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveBackupFolder();
            AppConfig.BackupOnExit = chkBackupOnExit.Checked;

            if (cboBackupInterval.SelectedItem is ComboItem ciInterval)
            {
                AppConfig.BackupIntervalHours = ciInterval.ID;
            }

            AppConfig.BackupLocalPath = txtLocalCloudPath.Text.Trim();
            AppConfig.WhatsAppBackupPhone = txtWhatsAppPhone.Text.Trim();

            MessageBox.Show("✅ تم حفظ إعدادات النسخ الاحتياطي بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}

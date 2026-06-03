using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSettings : Form
    {
        private TextBox txtCompanyName;
        private ComboBox cboReceiptPrintMode;
        private TextBox txtBackupFolder;
        private Label lblLastBackup;

        public FrmSettings()
        {
            this.Text = "إعدادات النظام";
            this.Size = new Size(560, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("⚙️ إعدادات النظام", "تعديل بيانات الشركة والنسخ الاحتياطي والإعدادات الأساسية");
            this.Controls.Add(pnlTop);

            int y = 80;

            // ── اسم الشركة ──────────────────────────────────────
            AddLabel("اسم الشركة / المؤسسة:", 20, ref y, 0);
            txtCompanyName = new TextBox
            {
                Location = new Point(20, y),
                Width = 500,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 12f)
            };
            txtCompanyName.Text = AppConfig.CompanyName;
            this.Controls.Add(txtCompanyName);
            y += 40;

            // ── نمط الطباعة ──────────────────────────────────────
            AddLabel("نمط طباعة الفاتورة:", 20, ref y, 15);
            cboReceiptPrintMode = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboReceiptPrintMode.Items.AddRange(new object[]
            {
                "مفصل - يظهر الرصيد السابق والحالي والمدفوع",
                "مختصر - يظهر المجموع النهائي فقط"
            });
            cboReceiptPrintMode.SelectedIndex = AppConfig.ReceiptPrintMode == "Compact" ? 1 : 0;
            this.Controls.Add(cboReceiptPrintMode);
            y += 40;

            // ── فاصل ──────────────────────────────────────────────
            var sep = new Panel
            {
                Location = new Point(20, y + 10),
                Size = new Size(500, 2),
                BackColor = Theme.BorderColor
            };
            this.Controls.Add(sep);
            y += 25;

            // ── النسخ الاحتياطي ──────────────────────────────────
            var lblBackupTitle = new Label
            {
                Text = "💾 إعدادات النسخ الاحتياطي",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            this.Controls.Add(lblBackupTitle);
            y += 30;

            // مجلد الباكب
            AddLabel("مجلد حفظ النسخ الاحتياطية:", 20, ref y, 0);
            txtBackupFolder = new TextBox
            {
                Location = new Point(20, y),
                Width = 380,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            txtBackupFolder.Text = BackupManager.BackupFolder;
            this.Controls.Add(txtBackupFolder);

            var btnBrowse = Theme.MakeButton("📂 تصفح", 410, y - 1, 110, 28, Color.FromArgb(55, 65, 81));
            btnBrowse.Font = new Font("Segoe UI", 9f);
            btnBrowse.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "اختر مجلد حفظ النسخ الاحتياطية";
                    dlg.SelectedPath = txtBackupFolder.Text;
                    if (dlg.ShowDialog() == DialogResult.OK)
                        txtBackupFolder.Text = dlg.SelectedPath;
                }
            };
            this.Controls.Add(btnBrowse);
            y += 38;

            // آخر نسخة احتياطية
            var last = BackupManager.LastBackupTime;
            string lastStr = last.HasValue
                ? last.Value.ToString("dd/MM/yyyy hh:mm tt")
                : "لم يتم عمل نسخة احتياطية بعد";
            string overdueStr = BackupManager.IsBackupOverdue() ? " ⚠️ متأخر!" : " ✅ حديث";

            lblLastBackup = new Label
            {
                Text = $"آخر نسخة احتياطية: {lastStr}{overdueStr}",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = BackupManager.IsBackupOverdue() ? Color.FromArgb(220, 80, 80) : Color.FromArgb(80, 200, 120),
                Font = new Font("Segoe UI", 9.5f)
            };
            this.Controls.Add(lblLastBackup);
            y += 30;

            // أزرار النسخ الاحتياطي
            var btnBackupNow = Theme.MakeButton("💾 نسخ احتياطي الآن", 20, y, 185, 38, Theme.Success);
            btnBackupNow.Click += (s, e) =>
            {
                // حفظ المسار أولاً
                SaveBackupFolder();
                bool ok = BackupManager.DoBackup(silent: false);
                if (ok) RefreshLastBackupLabel();
            };
            this.Controls.Add(btnBackupNow);

            var btnOpenFolder = Theme.MakeButton("📂 فتح مجلد الباكب", 215, y, 180, 38, Color.FromArgb(55, 65, 81));
            btnOpenFolder.Click += (s, e) =>
            {
                SaveBackupFolder();
                BackupManager.OpenBackupFolder();
            };
            this.Controls.Add(btnOpenFolder);
            y += 55;

            // ── زر الحفظ الرئيسي ──────────────────────────────────
            var btnSave = Theme.MakeButton("💾 حفظ الإعدادات", 20, y, 180, 44, Theme.Accent);
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCompanyName.Text))
                {
                    MessageBox.Show("أدخل اسم الشركة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AppConfig.CompanyName = txtCompanyName.Text.Trim();
                AppConfig.ReceiptPrintMode = cboReceiptPrintMode.SelectedIndex == 1 ? "Compact" : "Detailed";
                SaveBackupFolder();

                MessageBox.Show(
                    "✅ تم حفظ الإعدادات بنجاح!\nقد تحتاج لإعادة فتح بعض الشاشات ليتم تحديث الاسم.",
                    "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                if (Application.OpenForms["FrmMain"] is FrmMain main)
                    main.UpdateCompanyName(AppConfig.CompanyName);
            };
            this.Controls.Add(btnSave);
        }

        private void AddLabel(string text, int x, ref int y, int extraTop)
        {
            y += extraTop;
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Theme.TextMain
            };
            this.Controls.Add(lbl);
            y += 22;
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
            string overdueStr = BackupManager.IsBackupOverdue() ? " ⚠️ متأخر!" : " ✅ حديث";
            lblLastBackup.Text = $"آخر نسخة احتياطية: {lastStr}{overdueStr}";
            lblLastBackup.ForeColor = BackupManager.IsBackupOverdue()
                ? Color.FromArgb(220, 80, 80)
                : Color.FromArgb(80, 200, 120);
        }
    }
}

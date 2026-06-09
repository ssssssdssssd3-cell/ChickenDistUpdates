using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSettings : Form
    {
        private TextBox txtCompanyName;
        private ComboBox cboReceiptPrintMode;
        private ComboBox cboReceiptPrinter;
        private ComboBox cboA4Printer;
        private ComboBox cboInvoiceFormat;
        private TextBox txtBackupFolder;
        private Label lblLastBackup;

        private CheckBox chkScaleEnabled;
        private ComboBox cboScalePort;
        private ComboBox cboScaleBaud;
        private Button btnTestScale;
        private Label lblTestWeightResult;
        private Timer scaleTestTimer;

        public FrmSettings()
        {
            this.Text = "إعدادات النظام";
            this.Size = new Size(560, 780);
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

            // ── طابعة الريسيت الافتراضية ──────────────────────────
            AddLabel("طابعة الريسيت الافتراضية:", 20, ref y, 15);
            cboReceiptPrinter = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboReceiptPrinter.Items.Add("(طابعة النظام الافتراضية)");
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cboReceiptPrinter.Items.Add(printer);
                }
            }
            catch { }
            cboReceiptPrinter.SelectedItem = string.IsNullOrEmpty(AppConfig.ReceiptPrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.ReceiptPrinterName;
            if (cboReceiptPrinter.SelectedIndex == -1 && cboReceiptPrinter.Items.Count > 0)
                cboReceiptPrinter.SelectedIndex = 0;
            this.Controls.Add(cboReceiptPrinter);
            y += 40;

            // ── طابعة A4 الافتراضية ──────────────────────────────
            AddLabel("طابعة A4 / التقارير الافتراضية:", 20, ref y, 15);
            cboA4Printer = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboA4Printer.Items.Add("(طابعة النظام الافتراضية)");
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cboA4Printer.Items.Add(printer);
                }
            }
            catch { }
            cboA4Printer.SelectedItem = string.IsNullOrEmpty(AppConfig.A4PrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.A4PrinterName;
            if (cboA4Printer.SelectedIndex == -1 && cboA4Printer.Items.Count > 0)
                cboA4Printer.SelectedIndex = 0;
            this.Controls.Add(cboA4Printer);
            y += 40;

            // ── الحجم الافتراضي لطباعة الفاتورة ───────────────────
            AddLabel("حجم طباعة الفاتورة الافتراضي:", 20, ref y, 15);
            cboInvoiceFormat = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboInvoiceFormat.Items.AddRange(new object[]
            {
                "ريسيت حراري (Receipt 80mm)",
                "ورق عادي (A4/A5)"
            });
            cboInvoiceFormat.SelectedIndex = AppConfig.DefaultInvoiceFormat == "Receipt" ? 0 : 1;
            this.Controls.Add(cboInvoiceFormat);
            y += 40;

            // ── إعدادات الميزان ───────────────────────────────
            AddLabel("🔌 إعدادات الميزان الإلكتروني:", 20, ref y, 15);
            
            chkScaleEnabled = new CheckBox
            {
                Text = "تفعيل قراءة الميزان الإلكتروني",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ScaleEnabled
            };
            this.Controls.Add(chkScaleEnabled);
            y += 30;

            AddLabel("منفذ الاتصال (COM Port):", 20, ref y, 5);
            cboScalePort = new ComboBox
            {
                Location = new Point(20, y),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            try
            {
                cboScalePort.Items.Clear();
                foreach (string p in System.IO.Ports.SerialPort.GetPortNames())
                    cboScalePort.Items.Add(p);
                
                if (cboScalePort.Items.Contains(AppConfig.ScaleComPort))
                    cboScalePort.SelectedItem = AppConfig.ScaleComPort;
                else if (cboScalePort.Items.Count > 0)
                    cboScalePort.SelectedIndex = 0;
            }
            catch { }
            this.Controls.Add(cboScalePort);

            var lblBaud = new Label
            {
                Text = "سرعة النقل (Baud Rate):",
                Location = new Point(280, y - 22),
                AutoSize = true,
                ForeColor = Theme.TextMain
            };
            this.Controls.Add(lblBaud);

            cboScaleBaud = new ComboBox
            {
                Location = new Point(280, y),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboScaleBaud.Items.AddRange(new object[] { "2400", "4800", "9600", "19200", "38400", "115200" });
            cboScaleBaud.SelectedItem = AppConfig.ScaleBaudRate.ToString();
            this.Controls.Add(cboScaleBaud);
            y += 40;

            btnTestScale = Theme.MakeButton("🔌 اختبار قراءة الوزن", 20, y, 180, 32, Theme.Primary);
            btnTestScale.Font = new Font("Segoe UI", 9f);
            btnTestScale.Click += BtnTestScale_Click;
            this.Controls.Add(btnTestScale);

            lblTestWeightResult = new Label
            {
                Text = "الوزن الحالي: ---",
                Location = new Point(215, y + 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            this.Controls.Add(lblTestWeightResult);
            y += 45;

            // Timer to show live weight during test
            scaleTestTimer = new Timer { Interval = 200 };
            scaleTestTimer.Tick += ScaleTestTimer_Tick;

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
                AppConfig.ReceiptPrinterName = cboReceiptPrinter.SelectedIndex <= 0 ? "" : cboReceiptPrinter.SelectedItem.ToString();
                AppConfig.A4PrinterName = cboA4Printer.SelectedIndex <= 0 ? "" : cboA4Printer.SelectedItem.ToString();
                AppConfig.DefaultInvoiceFormat = cboInvoiceFormat.SelectedIndex == 0 ? "Receipt" : "A4";
                
                AppConfig.ScaleEnabled = chkScaleEnabled.Checked;
                if (cboScalePort.SelectedItem != null)
                    AppConfig.ScaleComPort = cboScalePort.SelectedItem.ToString();
                if (cboScaleBaud.SelectedItem != null)
                    AppConfig.ScaleBaudRate = int.Parse(cboScaleBaud.SelectedItem.ToString());

                if (chkScaleEnabled.Checked)
                {
                    ScaleService.Instance.Disconnect();
                    ScaleService.Instance.Connect(AppConfig.ScaleComPort, AppConfig.ScaleBaudRate);
                }
                else
                {
                    ScaleService.Instance.Disconnect();
                }

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

            // ── فاصل ──────────────────────────────────────────────
            y += 55;
            var sep2 = new Panel
            {
                Location  = new Point(20, y),
                Size      = new Size(500, 2),
                BackColor = Theme.BorderColor
            };
            this.Controls.Add(sep2);
            y += 12;

            // ── معلومات الترخيص ──────────────────────────────────
            var lblLicTitle = new Label
            {
                Text      = "🔑 معلومات ترخيص البرنامج",
                Location  = new Point(20, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            this.Controls.Add(lblLicTitle);
            y += 30;

            string machineId = LicenseManager.GetCurrentMachineId();
            string hddSerial = LicenseManager.GetCurrentHddSerial();

            string expiryTxt = LicenseManager.IsActivated
                ? (LicenseManager.ExpiryDate == DateTime.MaxValue
                    ? "✅ ترخيص دائم"
                    : $"✅ صالح حتى: {LicenseManager.ExpiryDate:yyyy-MM-dd}")
                : "⛔ غير مفعّل";

            var lblLicInfo = new Label
            {
                Text      = $"الحالة: {expiryTxt}\n" +
                            $"الجهاز: {LicenseManager.DeviceName}\n" +
                            $"Machine ID: {machineId}\n" +
                            $"HDD Serial:   {hddSerial}",
                Location  = new Point(20, y),
                AutoSize  = false,
                Width     = 390,
                Height    = 75,
                Font      = new Font("Consolas", 9.5f),
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Theme.BgInput,
                Padding   = new Padding(5)
            };
            this.Controls.Add(lblLicInfo);

            var btnCopyIds = Theme.MakeButton("📋 نسخ المعرّفات", 420, y, 110, 38, Color.FromArgb(55, 65, 81));
            btnCopyIds.Font = new Font("Segoe UI", 9f);
            btnCopyIds.Click += (s, e) =>
            {
                string info = $"Machine ID: {machineId}\nHDD Serial: {hddSerial}";
                System.Windows.Forms.Clipboard.SetText(info);
                MessageBox.Show("✅ تم نسخ معرّفات الجهاز!\nأرسلها للمطور للحصول على ملف التفعيل.",
                    "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
            };
            this.Controls.Add(btnCopyIds);
            y += 85;

            // ── فاصل ──────────────────────────────────────────────
            y += 10;
            var sep3 = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(500, 2),
                BackColor = Theme.BorderColor
            };
            this.Controls.Add(sep3);
            y += 15;

            // ── أدوات الصيانة والدعم الفني ──────────────────────────
            var lblSupportTitle = new Label
            {
                Text = "🛠️ أدوات الصيانة والدعم الفني",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            this.Controls.Add(lblSupportTitle);
            y += 35;

            var btnDefender = Theme.MakeButton("🛡️ استثناء جدار الحماية", 20, y, 240, 36, Color.FromArgb(100, 40, 150));
            btnDefender.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnDefender.Click += (s, e) =>
            {
                try
                {
                    string appFolder = AppDomain.CurrentDomain.BaseDirectory;
                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"Add-MpPreference -ExclusionPath '{appFolder}'\"",
                        Verb = "runas", // طلب صلاحيات المسؤول UAC
                        UseShellExecute = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    };
                    System.Diagnostics.Process.Start(psi);
                    MessageBox.Show("✅ تم إرسال طلب استثناء المجلد لجدار الحماية بنجاح.\nيرجى تأكيد نافذة طلب الصلاحيات (UAC) التي ستظهر.", 
                        "استثناء الحماية", MessageBoxButtons.OK, MessageBoxIcon.Information, 
                        MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("فشل إضافة استثناء الحماية:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnDefender);

            var btnOpenLog = Theme.MakeButton("📋 عرض سجل الأخطاء", 270, y, 250, 36, Color.FromArgb(70, 80, 95));
            btnOpenLog.Font = new Font("Segoe UI", 9f);
            btnOpenLog.Click += (s, e) =>
            {
                try
                {
                    string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                    if (!File.Exists(logFile))
                    {
                        File.WriteAllText(logFile, $"--- سجل جديد تم إنشاؤه في {DateTime.Now} ---{Environment.NewLine}");
                    }
                    System.Diagnostics.Process.Start("notepad.exe", logFile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("فشل فتح ملف السجل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnOpenLog);
            y += 45;

            var btnExportLog = Theme.MakeButton("📂 تحديد موقع ملف السجل لإرساله", 20, y, 500, 36, Color.FromArgb(55, 65, 81));
            btnExportLog.Font = new Font("Segoe UI", 9f);
            btnExportLog.Click += (s, e) =>
            {
                try
                {
                    string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                    if (!File.Exists(logFile))
                    {
                        File.WriteAllText(logFile, $"--- سجل جديد تم إنشاؤه في {DateTime.Now} ---{Environment.NewLine}");
                    }
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{logFile}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("فشل تحديد موقع الملف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnExportLog);
            y += 55;

            this.Height = y + 60;
        }

        private void BtnTestScale_Click(object sender, EventArgs e)
        {
            if (scaleTestTimer.Enabled)
            {
                scaleTestTimer.Stop();
                ScaleService.Instance.Disconnect();
                btnTestScale.Text = "🔌 اختبار قراءة الوزن";
                btnTestScale.BackColor = Theme.Primary;
                lblTestWeightResult.Text = "الوزن الحالي: ---";
            }
            else
            {
                if (cboScalePort.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار منفذ COM أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string port = cboScalePort.SelectedItem.ToString();
                int baud = int.Parse(cboScaleBaud.SelectedItem.ToString());

                lblTestWeightResult.Text = "جاري الاتصال...";
                btnTestScale.Text = "⏹️ إيقاف الاختبار";
                btnTestScale.BackColor = Theme.Danger;

                if (ScaleService.Instance.Connect(port, baud))
                {
                    scaleTestTimer.Start();
                }
                else
                {
                    lblTestWeightResult.Text = "فشل الاتصال!";
                    btnTestScale.Text = "🔌 اختبار قراءة الوزن";
                    btnTestScale.BackColor = Theme.Primary;
                }
            }
        }

        private void ScaleTestTimer_Tick(object sender, EventArgs e)
        {
            if (ScaleService.Instance.IsConnected)
            {
                decimal w = ScaleService.Instance.CurrentWeight;
                bool stable = ScaleService.Instance.IsStable;
                lblTestWeightResult.Text = $"الوزن الحالي: {w:F3} كجم {(stable ? "نشط" : "غير مستقر")}";
            }
            else
            {
                lblTestWeightResult.Text = "انقطع الاتصال!";
                scaleTestTimer.Stop();
                btnTestScale.Text = "🔌 اختبار قراءة الوزن";
                btnTestScale.BackColor = Theme.Primary;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (scaleTestTimer != null && scaleTestTimer.Enabled)
            {
                scaleTestTimer.Stop();
                ScaleService.Instance.Disconnect();
            }
            base.OnFormClosing(e);
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

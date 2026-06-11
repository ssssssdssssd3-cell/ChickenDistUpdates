using System;
using System.Drawing;
using System.Drawing.Printing;
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
        private ComboBox cboReceiptTemplate;
        private ComboBox cboA4Template;
        private ComboBox cboBarcodeTemplate;
        private ComboBox cboBarcodeEncoding;
        private TextBox txtBackupFolder;
        private Label lblLastBackup;

        // Scale & Barcode controls
        private CheckBox chkScaleEnabled;
        private ComboBox cboScalePort;
        private ComboBox cboScaleBaud;
        private Label lblTestWeightResult;
        private TextBox txtBarcodePrefix;
        private NumericUpDown nudCodeLen;
        private NumericUpDown nudWeightLen;
        private NumericUpDown nudDiv;

        public FrmSettings()
        {
            this.Text = "إعدادات النظام";
            this.Size = new Size(560, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.AutoScroll = true;

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

            // ── قالب طباعة الريسيت ───────────────────
            AddLabel("قالب طباعة الريسيت الحراري (Receipt):", 20, ref y, 10);
            cboReceiptTemplate = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboReceiptTemplate.Items.AddRange(new object[]
            {
                "القياسي (Standard)",
                "العصري (Modern)",
                "المبسط السريع (Compact)",
                "الفواتير الاحترافية (Elegant)"
            });
            cboReceiptTemplate.SelectedItem = AppConfig.ReceiptTemplate == "Modern" ? "العصري (Modern)"
                                            : AppConfig.ReceiptTemplate == "Compact" ? "المبسط السريع (Compact)"
                                            : AppConfig.ReceiptTemplate == "Elegant" ? "الفواتير الاحترافية (Elegant)"
                                            : "القياسي (Standard)";
            if (cboReceiptTemplate.SelectedIndex == -1) cboReceiptTemplate.SelectedIndex = 0;
            this.Controls.Add(cboReceiptTemplate);
            y += 40;

            // ── قالب طباعة A4 ───────────────────
            AddLabel("قالب طباعة ورق A4/A5:", 20, ref y, 10);
            cboA4Template = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboA4Template.Items.AddRange(new object[]
            {
                "الكلاسيكي الأزرق (Classic Blue)",
                "التصميم الحديث (Modern Dark)",
                "الفاتورة الرسمية (Official Invoice)",
                "الشبكة المبسطة (Simple Grid)"
            });
            cboA4Template.SelectedItem = AppConfig.A4Template == "Modern" ? "التصميم الحديث (Modern Dark)"
                                       : AppConfig.A4Template == "Official" ? "الفاتورة الرسمية (Official Invoice)"
                                       : AppConfig.A4Template == "Simple" ? "الشبكة المبسطة (Simple Grid)"
                                       : "الكلاسيكي الأزرق (Classic Blue)";
            if (cboA4Template.SelectedIndex == -1) cboA4Template.SelectedIndex = 0;
            this.Controls.Add(cboA4Template);
            y += 40;

            // ── قالب الباركود الافتراضي ───────────────────
            AddLabel("قالب ملصق الباركود الافتراضي (Sticker):", 20, ref y, 10);
            cboBarcodeTemplate = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboBarcodeTemplate.Items.AddRange(new object[]
            {
                "الافتراضي (اسم صنف + سعر + باركود)",
                "سعر بارز (سعر كبير + باركود)",
                "ملصق صغير (سعر وباركود فقط)",
                "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)"
            });
            cboBarcodeTemplate.SelectedItem = AppConfig.BarcodeTemplate == "PriceHeavy" ? "سعر بارز (سعر كبير + باركود)"
                                            : AppConfig.BarcodeTemplate == "Small" ? "ملصق صغير (سعر وباركود فقط)"
                                            : AppConfig.BarcodeTemplate == "Shelf" ? "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)"
                                            : "الافتراضي (اسم صنف + سعر + باركود)";
            if (cboBarcodeTemplate.SelectedIndex == -1) cboBarcodeTemplate.SelectedIndex = 0;
            this.Controls.Add(cboBarcodeTemplate);
            y += 40;

            // ── ترميز الباركود ───────────────────
            AddLabel("نوع تشفير الباركود المطبوع:", 20, ref y, 10);
            cboBarcodeEncoding = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboBarcodeEncoding.Items.AddRange(new object[]
            {
                "Code 128 (موصى به - ثنائي ومدمج وسريع القراءة)",
                "Code 39 (أحادي عريض)"
            });
            cboBarcodeEncoding.SelectedIndex = AppConfig.BarcodeEncoding == "Code39" ? 1 : 0;
            this.Controls.Add(cboBarcodeEncoding);
            y += 40;

            // ── فاصل ──────────────────────────────────────────────
            y += 25;
            var sepSettings = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(500, 2),
                BackColor = Theme.BorderColor
            };
            this.Controls.Add(sepSettings);
            y += 15;

            // ── إعدادات الميزان الإلكتروني ─────────────────────────
            var lblScaleTitle = new Label
            {
                Text = "⚖️ إعدادات الميزان الإلكتروني (COM Port)",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            this.Controls.Add(lblScaleTitle);
            y += 30;

            chkScaleEnabled = new CheckBox
            {
                Text = "تفعيل الميزان الإلكتروني",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ScaleEnabled
            };
            this.Controls.Add(chkScaleEnabled);
            y += 30;

            AddLabel("منفذ الاتصال (COM Port):", 20, ref y, 0);
            cboScalePort = new ComboBox { Location = new Point(20, y), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            foreach (string p in System.IO.Ports.SerialPort.GetPortNames()) cboScalePort.Items.Add(p);
            if (cboScalePort.Items.Contains(AppConfig.ScaleComPort)) cboScalePort.SelectedItem = AppConfig.ScaleComPort;
            else if (cboScalePort.Items.Count > 0) cboScalePort.SelectedIndex = 0;
            this.Controls.Add(cboScalePort);

            var lblBaud = new Label { Text = "سرعة النقل (Baud Rate):", Location = new Point(270, y - 22), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblBaud);
            cboScaleBaud = new ComboBox { Location = new Point(270, y), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            cboScaleBaud.Items.AddRange(new object[] { "2400", "4800", "9600", "19200", "38400", "115200" });
            cboScaleBaud.SelectedItem = AppConfig.ScaleBaudRate.ToString();
            this.Controls.Add(cboScaleBaud);
            y += 40;

            var btnTestScale = Theme.MakeButton("⚖️ اختبار الميزان", 20, y, 150, 35, Theme.Primary);
            lblTestWeightResult = new Label { Location = new Point(180, y + 8), AutoSize = true, ForeColor = Theme.Success, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            this.Controls.Add(btnTestScale);
            this.Controls.Add(lblTestWeightResult);
            btnTestScale.Click += (s, e) =>
            {
                if (ScaleService.Instance.IsConnected)
                {
                    ScaleService.Instance.Disconnect();
                    btnTestScale.Text = "⚖️ اختبار الميزان";
                    btnTestScale.BackColor = Theme.Primary;
                    lblTestWeightResult.Text = "";
                }
                else
                {
                    if (cboScalePort.SelectedItem == null) return;
                    btnTestScale.Text = "🛑 إيقاف الاختبار";
                    btnTestScale.BackColor = Theme.Danger;
                    lblTestWeightResult.Text = "جاري الاتصال...";
                    if (ScaleService.Instance.Connect(cboScalePort.SelectedItem.ToString(), int.Parse(cboScaleBaud.SelectedItem.ToString())))
                    {
                        ScaleService.Instance.WeightChanged += (w, stable) =>
                        {
                            this.Invoke(new Action(() => lblTestWeightResult.Text = $"الوزن: {w} {(stable ? "(مستقر)" : "")}"));
                        };
                    }
                    else
                    {
                        lblTestWeightResult.Text = "خطأ في الاتصال";
                        btnTestScale.Text = "⚖️ اختبار الميزان";
                        btnTestScale.BackColor = Theme.Primary;
                    }
                }
            };
            y += 50;

            // ── إعدادات ميزان الباركود ─────────────────────────────
            var lblBarcodeScaleTitle = new Label
            {
                Text = "🏷️ إعدادات ميزان الباركود (الاستيكرات)",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            this.Controls.Add(lblBarcodeScaleTitle);
            y += 30;

            AddLabel("بداية باركود الميزان (Prefix):", 20, ref y, 0);
            txtBarcodePrefix = new TextBox { Location = new Point(20, y), Width = 230, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = AppConfig.BarcodeScalePrefix };
            this.Controls.Add(txtBarcodePrefix);

            var lblCodeLen = new Label { Text = "طول كود الصنف:", Location = new Point(270, y - 22), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblCodeLen);
            nudCodeLen = new NumericUpDown { Location = new Point(270, y), Width = 110, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Minimum = 1, Maximum = 10, Value = AppConfig.BarcodeScaleItemCodeLength };
            this.Controls.Add(nudCodeLen);

            var lblWeightLen = new Label { Text = "طول الوزن:", Location = new Point(390, y - 22), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblWeightLen);
            nudWeightLen = new NumericUpDown { Location = new Point(390, y), Width = 110, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Minimum = 1, Maximum = 10, Value = AppConfig.BarcodeScaleWeightLength };
            this.Controls.Add(nudWeightLen);
            y += 40;

            AddLabel("عامل القسمة للوزن (مثال 1000 للجرام):", 20, ref y, 0);
            nudDiv = new NumericUpDown { Location = new Point(20, y), Width = 230, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Minimum = 1, Maximum = 10000, Value = AppConfig.BarcodeScaleDivideBy };
            this.Controls.Add(nudDiv);
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
                AppConfig.ReceiptPrinterName = cboReceiptPrinter.SelectedIndex <= 0 ? "" : cboReceiptPrinter.SelectedItem.ToString();
                AppConfig.A4PrinterName = cboA4Printer.SelectedIndex <= 0 ? "" : cboA4Printer.SelectedItem.ToString();
                AppConfig.DefaultInvoiceFormat = cboInvoiceFormat.SelectedIndex == 0 ? "Receipt" : "A4";

                // Save Templates Settings
                AppConfig.ReceiptTemplate = cboReceiptTemplate.SelectedIndex == 1 ? "Modern"
                                          : cboReceiptTemplate.SelectedIndex == 2 ? "Compact"
                                          : cboReceiptTemplate.SelectedIndex == 3 ? "Elegant"
                                          : "Standard";
                AppConfig.A4Template = cboA4Template.SelectedIndex == 1 ? "Modern"
                                     : cboA4Template.SelectedIndex == 2 ? "Official"
                                     : cboA4Template.SelectedIndex == 3 ? "Simple"
                                     : "Classic";
                AppConfig.BarcodeTemplate = cboBarcodeTemplate.SelectedIndex == 1 ? "PriceHeavy"
                                          : cboBarcodeTemplate.SelectedIndex == 2 ? "Small"
                                          : cboBarcodeTemplate.SelectedIndex == 3 ? "Shelf"
                                          : "Standard";
                AppConfig.BarcodeEncoding = cboBarcodeEncoding.SelectedIndex == 1 ? "Code39" : "Code128";

                SaveBackupFolder();

                // Save Scale Settings
                AppConfig.ScaleEnabled = chkScaleEnabled.Checked;
                if (cboScalePort.SelectedItem != null) AppConfig.ScaleComPort = cboScalePort.SelectedItem.ToString();
                if (cboScaleBaud.SelectedItem != null) AppConfig.ScaleBaudRate = int.Parse(cboScaleBaud.SelectedItem.ToString());

                // Save Barcode Settings
                AppConfig.BarcodeScalePrefix = txtBarcodePrefix.Text.Trim();
                AppConfig.BarcodeScaleItemCodeLength = (int)nudCodeLen.Value;
                AppConfig.BarcodeScaleWeightLength = (int)nudWeightLen.Value;
                AppConfig.BarcodeScaleDivideBy = nudDiv.Value;

                // Reconnect if enabled
                if (ScaleService.Instance.IsConnected) ScaleService.Instance.Disconnect();
                if (AppConfig.ScaleEnabled)
                    ScaleService.Instance.Connect(AppConfig.ScaleComPort, AppConfig.ScaleBaudRate);


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

            this.Height = y + 60;
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

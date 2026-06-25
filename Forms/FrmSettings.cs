using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmSettings : Form
    {
        private TextBox txtCompanyName;
        private TextBox txtCompanyPhone1;
        private TextBox txtCompanyPhone2;
        private TextBox txtShopLogoPath;
        private CheckBox chkPrintShopLogo;
        private ComboBox cboReceiptPrintMode;
        private ComboBox cboReceiptPrinter;
        private ComboBox cboA4Printer;
        private ComboBox cboBarcodePrinter;
        private ComboBox cboInvoiceFormat;
        private ComboBox cboReceiptTemplate;
        private ComboBox cboA4Template;
        private ComboBox cboBarcodeTemplate;
        private ComboBox cboBarcodeEncoding;
        private ComboBox cboBarcodeStickerSize;
        private CheckBox chkReceiptShowDiscount;
        private CheckBox chkReceiptShowClientInfo;
        private TextBox txtBackupFolder;
        private Label lblLastBackup;
        private CheckBox chkBackupOnExit;
        private TextBox txtWhatsAppPhone;
        private CheckBox chkEnableCrates;
        private TextBox txtLocalCloudPath;
        private ComboBox cboAppTheme;

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
            this.Size = new Size(560, 850);
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

            // ── أرقام الهواتف ───────────────────────────────────
            AddLabel("هاتف الشركة 1:", 20, ref y, 0);
            txtCompanyPhone1 = new TextBox
            {
                Location = new Point(20, y),
                Width = 240,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11f)
            };
            txtCompanyPhone1.Text = AppConfig.CompanyPhone1;
            this.Controls.Add(txtCompanyPhone1);

            var lblPhone2 = new Label
            {
                Text = "هاتف الشركة 2:",
                Location = new Point(280, y - 22),
                Width = 110,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            this.Controls.Add(lblPhone2);

            txtCompanyPhone2 = new TextBox
            {
                Location = new Point(280, y),
                Width = 240,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11f)
            };
            txtCompanyPhone2.Text = AppConfig.CompanyPhone2;
            this.Controls.Add(txtCompanyPhone2);
            y += 40;

            // ── شعار الشركة ──────────────────────────────────────
            AddLabel("شعار المؤسسة / المحل (يظهر بالطباعة):", 20, ref y, 15);
            txtShopLogoPath = new TextBox
            {
                Location = new Point(20, y),
                Width = 380,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            txtShopLogoPath.Text = AppConfig.ShopLogoPath;
            this.Controls.Add(txtShopLogoPath);

            var btnBrowseLogo = Theme.MakeButton("📂 تصفح الشعار", 410, y - 1, 110, 28, Color.FromArgb(55, 65, 81));
            btnBrowseLogo.Font = new Font("Segoe UI", 9f);
            btnBrowseLogo.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*";
                    dlg.Title = "اختر شعار المؤسسة";
                    if (dlg.ShowDialog() == DialogResult.OK)
                        txtShopLogoPath.Text = dlg.FileName;
                }
            };
            this.Controls.Add(btnBrowseLogo);
            y += 38;

            chkPrintShopLogo = new CheckBox
            {
                Text = "طباعة شعار المؤسسة في أعلى الفواتير والريسيت",
                Location = new Point(20, y),
                Size = new Size(400, 22),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.PrintShopLogo
            };
            this.Controls.Add(chkPrintShopLogo);
            y += 35;

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

            // ── طابعة الباركود الافتراضية ──────────────────────────
            AddLabel("طابعة ملصقات الباركود الافتراضية (Stickers):", 20, ref y, 15);
            cboBarcodePrinter = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboBarcodePrinter.Items.Add("(طابعة النظام الافتراضية)");
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cboBarcodePrinter.Items.Add(printer);
                }
            }
            catch { }
            cboBarcodePrinter.SelectedItem = string.IsNullOrEmpty(AppConfig.BarcodePrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.BarcodePrinterName;
            if (cboBarcodePrinter.SelectedIndex == -1 && cboBarcodePrinter.Items.Count > 0)
                cboBarcodePrinter.SelectedIndex = 0;
            this.Controls.Add(cboBarcodePrinter);
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
                "الفواتير الاحترافية (Elegant)",
                "قالب ميني ماركت (MiniMarket)"
            });
            cboReceiptTemplate.SelectedItem = AppConfig.ReceiptTemplate == "Modern" ? "العصري (Modern)"
                                            : AppConfig.ReceiptTemplate == "Compact" ? "المبسط السريع (Compact)"
                                            : AppConfig.ReceiptTemplate == "Elegant" ? "الفواتير الاحترافية (Elegant)"
                                            : AppConfig.ReceiptTemplate == "MiniMarket" ? "قالب ميني ماركت (MiniMarket)"
                                            : "القياسي (Standard)";
            if (cboReceiptTemplate.SelectedIndex == -1) cboReceiptTemplate.SelectedIndex = 0;
            this.Controls.Add(cboReceiptTemplate);
            y += 40;

            // ── خيارات محتوى الريسيت ─────────────────────────────────
            var lblReceiptOptions = new Label
            {
                Text = "📋 خيارات محتوى الريسيت الحراري:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            this.Controls.Add(lblReceiptOptions);
            y += 28;

            chkReceiptShowDiscount = new CheckBox
            {
                Text = "إظهار عمود الخصم في جدول الأصناف",
                Location = new Point(20, y),
                Size = new Size(400, 22),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ReceiptShowDiscount
            };
            this.Controls.Add(chkReceiptShowDiscount);
            y += 28;

            chkReceiptShowClientInfo = new CheckBox
            {
                Text = "إظهار بيانات العميل (اسم + هاتف + عنوان) في رأس الريسيت",
                Location = new Point(20, y),
                Size = new Size(460, 22),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ReceiptShowClientInfo
            };
            this.Controls.Add(chkReceiptShowClientInfo);
            y += 32;

            // زر معاينة الريسيت
            var btnPreviewReceipt = Theme.MakeButton("🖨️ معاينة نموذج الريسيت", 20, y, 220, 36, Theme.Primary);
            btnPreviewReceipt.Click += (s, e) =>
            {
                // حفظ الإعدادات الحالية أولاً قبل المعاينة
                AppConfig.ReceiptTemplate = cboReceiptTemplate.SelectedIndex == 1 ? "Modern"
                                          : cboReceiptTemplate.SelectedIndex == 2 ? "Compact"
                                          : cboReceiptTemplate.SelectedIndex == 3 ? "Elegant"
                                          : cboReceiptTemplate.SelectedIndex == 4 ? "MiniMarket"
                                          : "Standard";
                AppConfig.ReceiptShowDiscount = chkReceiptShowDiscount.Checked;
                AppConfig.ReceiptShowClientInfo = chkReceiptShowClientInfo.Checked;
                AppConfig.PrintShopLogo = chkPrintShopLogo.Checked;
                AppConfig.ShopLogoPath = txtShopLogoPath.Text.Trim();
                AppConfig.CompanyName = string.IsNullOrWhiteSpace(txtCompanyName.Text) ? AppConfig.CompanyName : txtCompanyName.Text.Trim();

                // بحث عن أي فاتورة موجودة للمعاينة
                var dtPreview = DbHelper.Query("SELECT TOP 1 SaleID FROM Sales WHERE IsPosted=1 ORDER BY SaleID DESC");
                if (dtPreview.Rows.Count > 0)
                {
                    int previewSaleID = Convert.ToInt32(dtPreview.Rows[0]["SaleID"]);
                    new FrmPrintSale(previewSaleID, "Receipt", showPreview: true);
                }
                else
                {
                    MessageBox.Show("لا توجد فواتير محفوظة للمعاينة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                }
            };
            this.Controls.Add(btnPreviewReceipt);
            y += 50;

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

            // ── مقاس ملصق الباركود ──────────────────────────────
            AddLabel("مقاس ملصق الباركود (Sticker Size):", 20, ref y, 15);
            cboBarcodeStickerSize = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f)
            };
            cboBarcodeStickerSize.Items.AddRange(new object[]
            {
                "50x30 مم (كامل)",
                "38x26 مم (صغير)"
            });
            cboBarcodeStickerSize.SelectedIndex = AppConfig.BarcodeStickerSize == "38x26" ? 1 : 0;
            this.Controls.Add(cboBarcodeStickerSize);
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

            // خيار النسخ الاحتياطي عند الخروج
            chkBackupOnExit = new CheckBox
            {
                Text = "عمل نسخة احتياطية تلقائياً عند إغلاق البرنامج",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = AppConfig.BackupOnExit
            };
            this.Controls.Add(chkBackupOnExit);
            y += 30;

            // خيار تفعيل تتبع الفوارغ والوزن الفارغ
            chkEnableCrates = new CheckBox
            {
                Text = "تفعيل نظام تتبع الفوارغ والوزن الفارغ للعملاء",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = AppConfig.EnableCratesTracking
            };
            this.Controls.Add(chkEnableCrates);
            y += 30;

            // مسار مجلد سحابي محلي
            AddLabel("مسار مجلد سحابي محلي (مثل Google Drive / Dropbox):", 20, ref y, 0);
            txtLocalCloudPath = new TextBox
            {
                Location = new Point(20, y),
                Width = 380,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            txtLocalCloudPath.Text = AppConfig.BackupLocalPath;
            this.Controls.Add(txtLocalCloudPath);

            var btnBrowseCloud = Theme.MakeButton("📂 تصفح السحابي", 410, y - 1, 110, 28, Color.FromArgb(55, 65, 81));
            btnBrowseCloud.Font = new Font("Segoe UI", 9f);
            btnBrowseCloud.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "اختر مجلد المزامنة السحابية المحلي";
                    dlg.SelectedPath = txtLocalCloudPath.Text;
                    if (dlg.ShowDialog() == DialogResult.OK)
                        txtLocalCloudPath.Text = dlg.SelectedPath;
                }
            };
            this.Controls.Add(btnBrowseCloud);
            y += 38;

            var btnAutoDetectGDrive = Theme.MakeButton("☁️ كشف تلقائي لجوجل درايف", 20, y, 230, 28, Color.FromArgb(47, 54, 64));
            btnAutoDetectGDrive.Font = new Font("Segoe UI", 9f);
            btnAutoDetectGDrive.Click += (s, e) =>
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string path1 = System.IO.Path.Combine(userProfile, "Google Drive\\My Drive");
                string path2 = System.IO.Path.Combine(userProfile, "Google Drive");
                if (System.IO.Directory.Exists(path1)) {
                    txtLocalCloudPath.Text = path1;
                    MessageBox.Show("تم العثور على مجلد Google Drive وتعيينه بنجاح!", "كشف تلقائي", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } else if (System.IO.Directory.Exists(path2)) {
                    txtLocalCloudPath.Text = path2;
                    MessageBox.Show("تم العثور على مجلد Google Drive وتعيينه بنجاح!", "كشف تلقائي", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } else {
                    MessageBox.Show("لم يتم العثور على مجلد Google Drive الافتراضي تلقائياً. يرجى تحديده يدوياً باستخدام زر التصفح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            this.Controls.Add(btnAutoDetectGDrive);
            y += 38;

            // إعدادات الواتساب
            AddLabel("رقم الهاتف للنسخ الاحتياطي بالواتساب (WhatsApp Backup Phone):", 20, ref y, 0);
            txtWhatsAppPhone = new TextBox
            {
                Location = new Point(20, y),
                Width = 330,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            txtWhatsAppPhone.Text = AppConfig.WhatsAppBackupPhone;
            this.Controls.Add(txtWhatsAppPhone);

            var btnTestWhatsApp = Theme.MakeButton("📤 اختبار الرفع بالواتس", 360, y - 1, 160, 28, Color.FromArgb(80, 100, 60));
            btnTestWhatsApp.Font = new Font("Segoe UI", 9f);
            btnTestWhatsApp.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtWhatsAppPhone.Text))
                {
                    MessageBox.Show("الرجاء إدخال رقم الهاتف أولاً للاختبار.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AppConfig.WhatsAppBackupPhone = txtWhatsAppPhone.Text.Trim();

                MessageBox.Show("جاري إنشاء نسخة احتياقيه واختبار رفعها بالواتساب، يرجى الانتظار...", "جاري الاختبار", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                System.Threading.Tasks.Task.Run(() => {
                    bool ok = BackupManager.DoBackup(silent: true);
                    this.Invoke(new Action(() => {
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
            };
            this.Controls.Add(btnTestWhatsApp);
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
            // ── طابع ألوان البرنامج (Theme Selection) ──────────────────────────────────
            AddLabel("طابع ألوان البرنامج (الثيم المفضل):", 20, ref y, 15);
            cboAppTheme = new ComboBox
            {
                Location = new Point(20, y),
                Width = 500,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 11f)
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
                _       => "داكن هادئ مريح (Dark Theme)"
            };
            this.Controls.Add(cboAppTheme);
            y += 45;

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
                AppConfig.CompanyPhone1 = txtCompanyPhone1.Text.Trim();
                AppConfig.CompanyPhone2 = txtCompanyPhone2.Text.Trim();
                AppConfig.ShopLogoPath = txtShopLogoPath.Text.Trim();
                AppConfig.PrintShopLogo = chkPrintShopLogo.Checked;
                AppConfig.ReceiptPrintMode = cboReceiptPrintMode.SelectedIndex == 1 ? "Compact" : "Detailed";
                AppConfig.ReceiptPrinterName = cboReceiptPrinter.SelectedIndex <= 0 ? "" : cboReceiptPrinter.SelectedItem.ToString();
                AppConfig.A4PrinterName = cboA4Printer.SelectedIndex <= 0 ? "" : cboA4Printer.SelectedItem.ToString();
                AppConfig.BarcodePrinterName = cboBarcodePrinter.SelectedIndex <= 0 ? "" : cboBarcodePrinter.SelectedItem.ToString();
                AppConfig.BarcodeStickerSize = cboBarcodeStickerSize.SelectedIndex == 1 ? "38x26" : "50x30";
                AppConfig.DefaultInvoiceFormat = cboInvoiceFormat.SelectedIndex == 0 ? "Receipt" : "A4";

                // Save Templates Settings
                AppConfig.ReceiptTemplate = cboReceiptTemplate.SelectedIndex == 1 ? "Modern"
                                          : cboReceiptTemplate.SelectedIndex == 2 ? "Compact"
                                          : cboReceiptTemplate.SelectedIndex == 3 ? "Elegant"
                                          : cboReceiptTemplate.SelectedIndex == 4 ? "MiniMarket"
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

                // حفظ إعدادات الواتساب وتفعيل الفوارغ والباكب عند الإغلاق والمسار السحابي
                AppConfig.WhatsAppBackupPhone = txtWhatsAppPhone.Text.Trim();
                AppConfig.EnableCratesTracking = chkEnableCrates.Checked;
                AppConfig.BackupOnExit = chkBackupOnExit.Checked;
                AppConfig.BackupLocalPath = txtLocalCloudPath.Text.Trim();

                // Save Theme
                AppConfig.AppTheme = cboAppTheme.SelectedIndex switch
                {
                    1 => "Slate",
                    2 => "Light",
                    _ => "Dark"
                };
 
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
                    "✅ تم حفظ الإعدادات بنجاح!\nيرجى إعادة تشغيل البرنامج لتطبيق المظهر المختار بالكامل.",
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

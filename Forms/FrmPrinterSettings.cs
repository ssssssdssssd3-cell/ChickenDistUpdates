using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة إعدادات الطابعات ونماذج الفواتير والباركود وأذونات التحضير
    /// </summary>
    public class FrmPrinterSettings : Form
    {
        // ── Tab 1 Controls: الطابعات وسلوك الطباعة ──────────────────
        private ComboBox cboReceiptPrinter;
        private ComboBox cboA4Printer;
        private ComboBox cboBarcodePrinter;
        private ComboBox cboInvoiceFormat;
        private ComboBox cboReportFormat;
        private ComboBox cboPrintBehavior;
        private ComboBox cboReceiptPrintMode;
        private ComboBox cboPOSReceiptMode;

        // ── Tab 2 Controls: نماذج الفواتير والريسيت ──────────────────
        private ComboBox cboReceiptTemplate;
        private CheckBox chkReceiptShowDiscount;
        private CheckBox chkReceiptShowClientInfo;
        private TextBox txtReceiptHeader;
        private TextBox txtReceiptFooter;
        private ComboBox cboA4Template;
        private ComboBox cboA5Shift;
        private NumericUpDown numA5ShiftCm;

        // ── Tab 3 Controls: أذونات التحضير والباركود ──────────────────
        private ComboBox cboPrepTemplate;
        private ComboBox cboPrepPaperSize;
        private ComboBox cboPrepA5Shift;
        private NumericUpDown numPrepA5ShiftCm;
        private ComboBox cboBarcodeTemplate;
        private ComboBox cboBarcodeEncoding;
        private ComboBox cboBarcodeStickerSize;

        public FrmPrinterSettings()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "🖨️ إعدادات الطابعات ونماذج الفواتير";
            this.Size = new Size(760, 710);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("🖨️ إعدادات الطابعات ونماذج الفواتير", "تخصيص الطابعات الافتراضية، مقاسات الفواتير والتقارير، القوالب، وملصقات الباركود");
            this.Controls.Add(pnlTop);

            var tabControl = new TabControl
            {
                Location = new Point(15, 75),
                Size = new Size(715, 560),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            this.Controls.Add(tabControl);

            // ── Tab 1: الطابعات وسلوك الطباعة ────────────────────────
            var tabPrinters = new TabPage("🖨️ الطابعات وسلوك الطباعة")
            {
                BackColor = Theme.BgMain,
                AutoScroll = true
            };
            tabControl.TabPages.Add(tabPrinters);
            BuildTabPrinters(tabPrinters);

            // ── Tab 2: قوالب الفواتير والريسيت A4 / A5 ─────────────────
            var tabTemplates = new TabPage("📄 قوالب ونماذج الفواتير")
            {
                BackColor = Theme.BgMain,
                AutoScroll = true
            };
            tabControl.TabPages.Add(tabTemplates);
            BuildTabTemplates(tabTemplates);

            // ── Tab 3: أذونات التحضير والباركود ──────────────────────
            var tabPrepAndBarcode = new TabPage("🏷️ أذونات التحضير والباركود")
            {
                BackColor = Theme.BgMain,
                AutoScroll = true
            };
            tabControl.TabPages.Add(tabPrepAndBarcode);
            BuildTabPrepAndBarcode(tabPrepAndBarcode);

            // ── شريط الأزرار السفلي ─────────────────────────────────
            var pnlFooter = new Panel
            {
                Location = new Point(0, 642),
                Size = new Size(760, 55),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlFooter);

            var btnSave = Theme.MakeButton("💾 حفظ إعدادات الطباعة", 15, 8, 220, 38, Theme.Primary);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;
            pnlFooter.Controls.Add(btnSave);

            var btnCancel = Theme.MakeButton("إغلاق", 245, 8, 100, 38, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnCancel);

            Theme.ApplyFormRTL(this);
        }

        private void BuildTabPrinters(TabPage page)
        {
            int y = 15;

            // 1. طابعة الريسيت الافتراضية
            AddLabel(page, "طابعة الريسيت الافتراضية (Receipt 80mm):", 20, y);
            y += 24;
            cboReceiptPrinter = MakePrintersCombo(20, y, 660);
            cboReceiptPrinter.SelectedItem = string.IsNullOrEmpty(AppConfig.ReceiptPrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.ReceiptPrinterName;
            if (cboReceiptPrinter.SelectedIndex == -1 && cboReceiptPrinter.Items.Count > 0) cboReceiptPrinter.SelectedIndex = 0;
            page.Controls.Add(cboReceiptPrinter);
            y += 38;

            // 2. طابعة A4 الافتراضية
            AddLabel(page, "طابعة A4 / A5 والتقارير الافتراضية:", 20, y);
            y += 24;
            cboA4Printer = MakePrintersCombo(20, y, 660);
            cboA4Printer.SelectedItem = string.IsNullOrEmpty(AppConfig.A4PrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.A4PrinterName;
            if (cboA4Printer.SelectedIndex == -1 && cboA4Printer.Items.Count > 0) cboA4Printer.SelectedIndex = 0;
            page.Controls.Add(cboA4Printer);
            y += 38;

            // 3. طابعة الباركود الافتراضية
            AddLabel(page, "طابعة ملصقات الباركود الافتراضية (Stickers):", 20, y);
            y += 24;
            cboBarcodePrinter = MakePrintersCombo(20, y, 660);
            cboBarcodePrinter.SelectedItem = string.IsNullOrEmpty(AppConfig.BarcodePrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.BarcodePrinterName;
            if (cboBarcodePrinter.SelectedIndex == -1 && cboBarcodePrinter.Items.Count > 0) cboBarcodePrinter.SelectedIndex = 0;
            page.Controls.Add(cboBarcodePrinter);
            y += 42;

            // 4. مقاس الفاتورة الافتراضي ومقاس التقارير
            AddLabel(page, "حجم طباعة الفاتورة الافتراضي:", 20, y);
            AddLabel(page, "حجم طباعة التقارير الافتراضي:", 360, y);
            y += 24;

            cboInvoiceFormat = new ComboBox
            {
                Location = new Point(20, y),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboInvoiceFormat.Items.AddRange(new object[]
            {
                "ريسيت حراري (Receipt 80mm)",
                "ورق A4 كامل (A4 Sheet)",
                "ورق A5 نصف صفحة (A5 Sheet)"
            });
            cboInvoiceFormat.SelectedIndex = AppConfig.DefaultInvoiceFormat == "Receipt" ? 0 : (AppConfig.DefaultInvoiceFormat == "A5" ? 2 : 1);
            page.Controls.Add(cboInvoiceFormat);

            cboReportFormat = new ComboBox
            {
                Location = new Point(360, y),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboReportFormat.Items.AddRange(new object[]
            {
                "ورق A4 كامل (A4 Sheet - الافتراضي والموصى به)",
                "ورق A5 نصف صفحة (A5 Sheet)",
                "ريسيت حراري (Receipt 80mm)"
            });
            cboReportFormat.SelectedIndex = AppConfig.DefaultReportFormat == "A5" ? 1 : (AppConfig.DefaultReportFormat == "Receipt" ? 2 : 0);
            page.Controls.Add(cboReportFormat);
            y += 42;

            // 5. سلوك وسؤال الطباعة عند الحفظ
            AddLabel(page, "سلوك وسؤال الطباعة عند حفظ الفاتورة (بيع / بيان تسعير / POS):", 20, y);
            y += 24;
            cboPrintBehavior = new ComboBox
            {
                Location = new Point(20, y),
                Width = 660,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboPrintBehavior.Items.AddRange(new object[]
            {
                "سؤال واختيار نوع ومقاس الطباعة في كل مرة (نافذة الحوار)",
                "طباعة مباشرة وفورية بالمقاس الافتراضي فور الحفظ (بدون سؤال)",
                "عدم الطباعة تلقائياً (الطباعة يدوياً عند الحاجة)"
            });
            cboPrintBehavior.SelectedIndex = AppConfig.PrintBehaviorOnSave == "Direct" ? 1
                                           : (AppConfig.PrintBehaviorOnSave == "None" ? 2 : 0);
            page.Controls.Add(cboPrintBehavior);
            y += 42;

            // 6. نمط طباعة الفاتورة وطباعة رسيت البيع بعد الدفع
            AddLabel(page, "نمط طباعة الفاتورة:", 20, y);
            AddLabel(page, "طباعة رسيت البيع بعد الدفع (POS):", 360, y);
            y += 24;

            cboReceiptPrintMode = new ComboBox
            {
                Location = new Point(20, y),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboReceiptPrintMode.Items.AddRange(new object[]
            {
                "مفصل - يظهر الرصيد السابق والحالي والمدفوع",
                "مختصر - يظهر المجموع النهائي فقط"
            });
            cboReceiptPrintMode.SelectedIndex = AppConfig.ReceiptPrintMode == "Compact" ? 1 : 0;
            page.Controls.Add(cboReceiptPrintMode);

            cboPOSReceiptMode = new ComboBox
            {
                Location = new Point(360, y),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboPOSReceiptMode.Items.AddRange(new object[]
            {
                "طباعة تلقائية دائماً",
                "اسألني كل مرة",
                "لا تطبع رسيت — بون المطبخ فقط"
            });
            cboPOSReceiptMode.SelectedIndex = AppConfig.POSReceiptMode == "Never" ? 2 : AppConfig.POSReceiptMode == "Ask" ? 1 : 0;
            page.Controls.Add(cboPOSReceiptMode);
        }

        private void BuildTabTemplates(TabPage page)
        {
            int y = 15;

            // 1. قالب طباعة الريسيت الحراري
            AddLabel(page, "قالب طباعة الريسيت الحراري (Receipt):", 20, y);
            y += 24;
            cboReceiptTemplate = new ComboBox
            {
                Location = new Point(20, y),
                Width = 470,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboReceiptTemplate.Items.AddRange(new object[]
            {
                "القياسي (Standard)",
                "العصري (Modern)",
                "المبسط السريع (Compact)",
                "الفواتير الاحترافية (Elegant)",
                "قالب ميني ماركت (MiniMarket)",
                "النموذج الشبكي (GridReceipt)",
                "النموذج الزخرفي (FancyReceipt)",
                "قالب كافيه ومطاعم (RestaurantReceipt)",
                "قالب صيدليات وأغذية (PharmacyReceipt)"
            });
            cboReceiptTemplate.SelectedItem = AppConfig.ReceiptTemplate switch
            {
                "Modern" => "العصري (Modern)",
                "Compact" => "المبسط السريع (Compact)",
                "Elegant" => "الفواتير الاحترافية (Elegant)",
                "MiniMarket" => "قالب ميني ماركت (MiniMarket)",
                "GridReceipt" => "النموذج الشبكي (GridReceipt)",
                "FancyReceipt" => "النموذج الزخرفي (FancyReceipt)",
                "RestaurantReceipt" => "قالب كافيه ومطاعم (RestaurantReceipt)",
                "PharmacyReceipt" => "قالب صيدليات وأغذية (PharmacyReceipt)",
                _ => "القياسي (Standard)"
            };
            if (cboReceiptTemplate.SelectedIndex == -1) cboReceiptTemplate.SelectedIndex = 0;
            page.Controls.Add(cboReceiptTemplate);

            var btnPreviewReceipt = Theme.MakeButton("🖨️ معاينة الريسيت", 500, y - 2, 180, 32, Theme.Primary);
            btnPreviewReceipt.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPreviewReceipt.Click += (s, e) => PreviewReceipt();
            page.Controls.Add(btnPreviewReceipt);
            y += 38;

            // خيارات محتوى الريسيت
            chkReceiptShowDiscount = new CheckBox
            {
                Text = "إظهار عمود الخصم في جدول الأصناف بالريسيت",
                Location = new Point(20, y),
                Size = new Size(320, 22),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ReceiptShowDiscount
            };
            page.Controls.Add(chkReceiptShowDiscount);

            chkReceiptShowClientInfo = new CheckBox
            {
                Text = "إظهار بيانات العميل (اسم + هاتف + عنوان) في رأس الريسيت",
                Location = new Point(350, y),
                Size = new Size(330, 22),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ReceiptShowClientInfo
            };
            page.Controls.Add(chkReceiptShowClientInfo);
            y += 30;

            // مقدمة وتذييل الريسيت
            AddLabel(page, "ترويسة / مقدمة فاتورة الريسيت (نص بأعلى الفاتورة):", 20, y);
            AddLabel(page, "تذييل / خاتمة الريسيت (سياسة الاستبدال أو رسالة شكر):", 360, y);
            y += 24;

            txtReceiptHeader = new TextBox
            {
                Location = new Point(20, y),
                Width = 320,
                Height = 46,
                Multiline = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Text = AppConfig.ReceiptHeaderNote
            };
            page.Controls.Add(txtReceiptHeader);

            txtReceiptFooter = new TextBox
            {
                Location = new Point(360, y),
                Width = 320,
                Height = 46,
                Multiline = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Text = AppConfig.ReceiptFooterNote
            };
            page.Controls.Add(txtReceiptFooter);
            y += 56;

            var sep = new Panel { Location = new Point(20, y), Size = new Size(660, 2), BackColor = Theme.BorderColor };
            page.Controls.Add(sep);
            y += 12;

            // 2. قالب طباعة ورق A4 / A5
            AddLabel(page, "قالب طباعة ورق A4 / A5:", 20, y);
            y += 24;

            cboA4Template = new ComboBox
            {
                Location = new Point(20, y),
                Width = 380,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboA4Template.Items.AddRange(new object[]
            {
                "نموذج بيان الأسعار والشبكة التجارية (Commercial Grid A4)",
                "نموذج بيان الأسعار (بدون عمود الخصم) (Commercial Grid - No Discount)",
                "نموذج الطارق هوم (Al Tarek Home)",
                "نموذج الطارق هوم (بدون عمود الخصم) (Al Tarek Home - No Discount)",
                "الكلاسيكي الأزرق (Classic Blue)",
                "التصميم الحديث (Modern Dark)",
                "الفاتورة الرسمية (Official Invoice)",
                "الشبكة المبسطة (Simple Grid)",
                "نموذج قطع الغيار (SparePartsGrid)",
                "نموذج السوبرماركت (SupermarketA4)",
                "فاتورة كلاسيكية فاخرة (ElegantClassic)",
                "فاتورة شركات حديثة (CorporateModern)"
            });
            cboA4Template.SelectedItem = AppConfig.A4Template switch
            {
                "CommercialGrid" or "AlRahmaGrid" => "نموذج بيان الأسعار والشبكة التجارية (Commercial Grid A4)",
                "CommercialGridNoDiscount" => "نموذج بيان الأسعار (بدون عمود الخصم) (Commercial Grid - No Discount)",
                "AlTarekGrid" or "AlTarekHome" => "نموذج الطارق هوم (Al Tarek Home)",
                "AlTarekNoDiscount" or "AlTarekHomeNoDiscount" => "نموذج الطارق هوم (بدون عمود الخصم) (Al Tarek Home - No Discount)",
                "Classic" => "الكلاسيكي الأزرق (Classic Blue)",
                "Modern" => "التصميم الحديث (Modern Dark)",
                "Official" => "الفاتورة الرسمية (Official Invoice)",
                "Simple" => "الشبكة المبسطة (Simple Grid)",
                "SparePartsGrid" => "نموذج قطع الغيار (SparePartsGrid)",
                "SupermarketA4" => "نموذج السوبرماركت (SupermarketA4)",
                "ElegantClassic" => "فاتورة كلاسيكية فاخرة (ElegantClassic)",
                "CorporateModern" => "فاتورة شركات حديثة (CorporateModern)",
                _ => "نموذج الطارق هوم (Al Tarek Home)"
            };
            if (cboA4Template.SelectedIndex == -1) cboA4Template.SelectedIndex = 2;
            page.Controls.Add(cboA4Template);

            var btnPreviewA4 = Theme.MakeButton("📄 معاينة A4", 410, y - 2, 130, 32, Color.FromArgb(40, 120, 180));
            btnPreviewA4.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPreviewA4.Click += (s, e) => PreviewA4();
            page.Controls.Add(btnPreviewA4);

            var btnPreviewA5 = Theme.MakeButton("📑 معاينة A5", 550, y - 2, 130, 32, Color.FromArgb(46, 125, 50));
            btnPreviewA5.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPreviewA5.Click += (s, e) => PreviewA5();
            page.Controls.Add(btnPreviewA5);
            y += 42;

            // 3. مقاس ومحاذاة ورق A5
            AddLabel(page, "📐 مقاس ومحاذاة ورق A5 (إزاحة درج الطابعة / دفاتر الفواتير):", 20, y);
            y += 24;

            cboA5Shift = new ComboBox
            {
                Location = new Point(20, y),
                Width = 510,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboA5Shift.Items.AddRange(new object[]
            {
                "مقاس 1: إزاحة 3.0 سم (118 نقطة - الموصى به والمضبوط حالياً)",
                "مقاس 2: إزاحة 2.8 سم (110 نقطة - سنة إضافية للشمال)",
                "مقاس 3: إزاحة 3.2 سم (126 نقطة)",
                "مقاس 4: إزاحة 3.5 سم (138 نقطة)",
                "مقاس 5: إزاحة 2.5 سم (98 نقطة - دفاتر الفواتير القياسية)",
                "مقاس 6: إزاحة 4.0 سم (157 نقطة)",
                "مقاس 7: إزاحة 4.5 سم (175 نقطة)",
                "مقاس 8: إزاحة 2.0 سم (78 نقطة)",
                "مقاس 9: إزاحة 1.0 سم (39 نقطة)",
                "مقاس 10: بدون إزاحة 0 سم (توسيط قياسي / طابعة A5 متخصصة)",
                "مقاس مخصص يدوي (بالسنتيمتر)"
            });

            numA5ShiftCm = new NumericUpDown
            {
                Location = new Point(540, y),
                Width = 140,
                Height = 30,
                DecimalPlaces = 1,
                Increment = 0.2m,
                Minimum = 0.0m,
                Maximum = 15.0m,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };

            decimal curCm = Math.Round((decimal)(AppConfig.A5ShiftRight * 0.0254), 1);
            if (curCm < 0m) curCm = 0m;
            if (curCm > 15m) curCm = 15m;
            numA5ShiftCm.Value = curCm;

            if (Math.Abs(curCm - 3.0m) <= 0.1m) cboA5Shift.SelectedIndex = 0;
            else if (Math.Abs(curCm - 2.8m) <= 0.1m) cboA5Shift.SelectedIndex = 1;
            else if (Math.Abs(curCm - 3.2m) <= 0.1m) cboA5Shift.SelectedIndex = 2;
            else if (Math.Abs(curCm - 3.5m) <= 0.1m) cboA5Shift.SelectedIndex = 3;
            else if (Math.Abs(curCm - 2.5m) <= 0.1m) cboA5Shift.SelectedIndex = 4;
            else if (Math.Abs(curCm - 4.0m) <= 0.1m) cboA5Shift.SelectedIndex = 5;
            else if (Math.Abs(curCm - 4.5m) <= 0.1m) cboA5Shift.SelectedIndex = 6;
            else if (Math.Abs(curCm - 2.0m) <= 0.1m) cboA5Shift.SelectedIndex = 7;
            else if (Math.Abs(curCm - 1.0m) <= 0.1m) cboA5Shift.SelectedIndex = 8;
            else if (curCm == 0m) cboA5Shift.SelectedIndex = 9;
            else cboA5Shift.SelectedIndex = 10;

            cboA5Shift.SelectedIndexChanged += (s, e) =>
            {
                switch (cboA5Shift.SelectedIndex)
                {
                    case 0: numA5ShiftCm.Value = 3.0m; break;
                    case 1: numA5ShiftCm.Value = 2.8m; break;
                    case 2: numA5ShiftCm.Value = 3.2m; break;
                    case 3: numA5ShiftCm.Value = 3.5m; break;
                    case 4: numA5ShiftCm.Value = 2.5m; break;
                    case 5: numA5ShiftCm.Value = 4.0m; break;
                    case 6: numA5ShiftCm.Value = 4.5m; break;
                    case 7: numA5ShiftCm.Value = 2.0m; break;
                    case 8: numA5ShiftCm.Value = 1.0m; break;
                    case 9: numA5ShiftCm.Value = 0.0m; break;
                }
            };

            page.Controls.Add(cboA5Shift);
            page.Controls.Add(numA5ShiftCm);
        }

        private void BuildTabPrepAndBarcode(TabPage page)
        {
            int y = 15;

            // 1. إذن التحضير وصرف البضاعة بالمخزن
            var lblPrepTitle = new Label
            {
                Text = "📦 نماذج إذن التحضير وصرف البضاعة بالمخزن:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            page.Controls.Add(lblPrepTitle);
            y += 26;

            AddLabel(page, "نموذج إذن التحضير وصرف البضاعة:", 20, y);
            AddLabel(page, "مقاس ورق إذن التحضير الافتراضي:", 360, y);
            y += 24;

            cboPrepTemplate = new ComboBox
            {
                Location = new Point(20, y),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboPrepTemplate.Items.AddRange(new object[]
            {
                "نموذج إذن صرف بضاعة مع خانة الملاحظات (Disbursement Slip)",
                "النموذج القياسي مع موقع الرف والتخزين (Standard Grid)",
                "النموذج العصري الحديث (Modern Dark Preparation)"
            });
            cboPrepTemplate.SelectedItem = AppConfig.PreparationSlipTemplate == "Standard" ? "النموذج القياسي مع موقع الرف والتخزين (Standard Grid)"
                                         : AppConfig.PreparationSlipTemplate == "Modern" ? "النموذج العصري الحديث (Modern Dark Preparation)"
                                         : "نموذج إذن صرف بضاعة مع خانة الملاحظات (Disbursement Slip)";
            if (cboPrepTemplate.SelectedIndex == -1) cboPrepTemplate.SelectedIndex = 0;
            page.Controls.Add(cboPrepTemplate);

            cboPrepPaperSize = new ComboBox
            {
                Location = new Point(360, y),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboPrepPaperSize.Items.AddRange(new object[]
            {
                "ورق كامل A4 (210 x 297 mm)",
                "نصف صفحة A5 (148 x 210 mm)",
                "بون حراري Receipt (80 mm)"
            });
            cboPrepPaperSize.SelectedItem = AppConfig.PreparationPaperSize == "A5" ? "نصف صفحة A5 (148 x 210 mm)"
                                          : AppConfig.PreparationPaperSize == "Receipt" ? "بون حراري Receipt (80 mm)"
                                          : "ورق كامل A4 (210 x 297 mm)";
            if (cboPrepPaperSize.SelectedIndex == -1) cboPrepPaperSize.SelectedIndex = 0;
            page.Controls.Add(cboPrepPaperSize);
            y += 40;

            // محاذاة إذن التحضير A5
            AddLabel(page, "محاذاة إذن التحضير A5 (إزاحة درج الطابعة):", 20, y);
            y += 24;

            cboPrepA5Shift = new ComboBox
            {
                Location = new Point(20, y),
                Width = 360,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f)
            };
            cboPrepA5Shift.Items.AddRange(new object[]
            {
                "مطابق لمقاس ومحاذاة الفاتورة A5 تلقائياً (موصى به)",
                "مقاس 1: إزاحة 3.0 سم",
                "مقاس 2: إزاحة 2.8 سم",
                "مقاس 3: إزاحة 3.2 سم",
                "مقاس 4: إزاحة 3.5 سم",
                "مقاس 5: إزاحة 2.5 سم",
                "مقاس 6: إزاحة 4.0 سم",
                "مقاس 7: إزاحة 4.5 سم",
                "مقاس 8: إزاحة 2.0 سم",
                "مقاس 9: إزاحة 1.0 سم",
                "بدون إزاحة 0 سم"
            });

            numPrepA5ShiftCm = new NumericUpDown
            {
                Location = new Point(390, y),
                Width = 110,
                DecimalPlaces = 1,
                Increment = 0.2m,
                Minimum = 0.0m,
                Maximum = 15.0m,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            decimal prepCurCm = Math.Round((decimal)(AppConfig.GetPreparationA5Shift() * 0.0254), 1);
            numPrepA5ShiftCm.Value = prepCurCm >= 0 && prepCurCm <= 15 ? prepCurCm : 3.0m;

            if (!AppConfig.PreparationA5UseCustomShift)
            {
                cboPrepA5Shift.SelectedIndex = 0;
                numPrepA5ShiftCm.Enabled = false;
            }
            else
            {
                numPrepA5ShiftCm.Enabled = true;
                cboPrepA5Shift.SelectedIndex = 1;
            }

            cboPrepA5Shift.SelectedIndexChanged += (s, e) =>
            {
                numPrepA5ShiftCm.Enabled = (cboPrepA5Shift.SelectedIndex != 0);
            };

            page.Controls.Add(cboPrepA5Shift);
            page.Controls.Add(numPrepA5ShiftCm);

            var btnPreviewPrepA5 = Theme.MakeButton("📋 معاينة إذن التحضير A5", 510, y - 2, 170, 30, Color.FromArgb(34, 153, 84));
            btnPreviewPrepA5.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnPreviewPrepA5.Click += (s, e) => PreviewPrepSlip("A5");
            page.Controls.Add(btnPreviewPrepA5);
            y += 48;

            var sep2 = new Panel { Location = new Point(20, y), Size = new Size(660, 2), BackColor = Theme.BorderColor };
            page.Controls.Add(sep2);
            y += 12;

            // 2. إعدادات ملصقات الباركود
            var lblBarcodeTitle = new Label
            {
                Text = "🏷️ إعدادات ملصقات واستيكرات الباركود (Barcode Stickers):",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            page.Controls.Add(lblBarcodeTitle);
            y += 26;

            AddLabel(page, "قالب ملصق الباركود الافتراضي:", 20, y);
            AddLabel(page, "مقاس ملصق الباركود (Sticker Size):", 360, y);
            y += 24;

            cboBarcodeTemplate = new ComboBox
            {
                Location = new Point(20, y),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboBarcodeTemplate.Items.AddRange(new object[]
            {
                "الافتراضي (اسم صنف + سعر + باركود)",
                "سعر بارز (سعر كبير + باركود)",
                "ملصق صغير (سعر وباركود فقط)",
                "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)",
                "اسم صنف كبير + باركود (بدون سعر)"
            });
            cboBarcodeTemplate.SelectedItem = AppConfig.BarcodeTemplate switch
            {
                "PriceHeavy" => "سعر بارز (سعر كبير + باركود)",
                "Small" => "ملصق صغير (سعر وباركود فقط)",
                "Shelf" => "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)",
                "NoPrice" or "NoPriceBigName" => "اسم صنف كبير + باركود (بدون سعر)",
                _ => "الافتراضي (اسم صنف + سعر + باركود)"
            };
            if (cboBarcodeTemplate.SelectedIndex == -1) cboBarcodeTemplate.SelectedIndex = 0;
            page.Controls.Add(cboBarcodeTemplate);

            cboBarcodeStickerSize = new ComboBox
            {
                Location = new Point(360, y),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboBarcodeStickerSize.Items.AddRange(new object[]
            {
                "50x30 مم (عمود واحد - كامل)",
                "38x26 مم (عمود واحد - صغير)",
                "38x26 مم (عمودين - صغير مزدوج)",
                "50x25 مم (عمودين - مزدوج ملابس)"
            });
            cboBarcodeStickerSize.SelectedIndex = AppConfig.BarcodeStickerSize switch
            {
                "50x25_double" => 3,
                "38x26_double" => 2,
                "38x26" => 1,
                _ => 0
            };
            page.Controls.Add(cboBarcodeStickerSize);
            y += 42;

            // تشفير الباركود
            AddLabel(page, "نوع تشفير وترميز الباركود المطبوع:", 20, y);
            y += 24;

            cboBarcodeEncoding = new ComboBox
            {
                Location = new Point(20, y),
                Width = 660,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            var encList = BarcodeEngine.GetAvailableEncodings();
            foreach (var enc in encList)
            {
                cboBarcodeEncoding.Items.Add(enc.Item2);
            }
            int defEncIdx = encList.FindIndex(x => x.Item1 == AppConfig.BarcodeEncoding);
            cboBarcodeEncoding.SelectedIndex = defEncIdx >= 0 ? defEncIdx : 0;
            page.Controls.Add(cboBarcodeEncoding);
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

        private ComboBox MakePrintersCombo(int x, int y, int width)
        {
            var cbo = new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cbo.Items.Add("(طابعة النظام الافتراضية)");
            try
            {
                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    cbo.Items.Add(p);
                }
            }
            catch { }
            return cbo;
        }

        private void PreviewReceipt()
        {
            ApplyCurrentToAppConfig();
            var dtPreview = DbHelper.Query("SELECT TOP 1 SaleID FROM Sales WHERE IsPosted=1 ORDER BY SaleID DESC");
            if (dtPreview != null && dtPreview.Rows.Count > 0)
            {
                int previewSaleID = Convert.ToInt32(dtPreview.Rows[0]["SaleID"]);
                new FrmPrintSale(previewSaleID, "Receipt", true);
            }
            else
            {
                MessageBox.Show("لا توجد فواتير محفوظة للمعاينة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PreviewA4()
        {
            ApplyCurrentToAppConfig();
            var dtPreview = DbHelper.Query("SELECT TOP 1 SaleID FROM Sales WHERE IsPosted=1 ORDER BY SaleID DESC");
            if (dtPreview != null && dtPreview.Rows.Count > 0)
            {
                int previewSaleID = Convert.ToInt32(dtPreview.Rows[0]["SaleID"]);
                new FrmPrintSale(previewSaleID, "A4", true);
            }
            else
            {
                MessageBox.Show("لا توجد فواتير مبيعات مسجلة حالياً للمعاينة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PreviewA5()
        {
            ApplyCurrentToAppConfig();
            var dtPreview = DbHelper.Query("SELECT TOP 1 SaleID FROM Sales WHERE IsPosted=1 ORDER BY SaleID DESC");
            if (dtPreview != null && dtPreview.Rows.Count > 0)
            {
                int previewSaleID = Convert.ToInt32(dtPreview.Rows[0]["SaleID"]);
                new FrmPrintSale(previewSaleID, "A5", true);
            }
            else
            {
                MessageBox.Show("لا توجد فواتير مبيعات مسجلة حالياً للمعاينة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PreviewPrepSlip(string size)
        {
            ApplyCurrentToAppConfig();
            var dtPreview = DbHelper.Query("SELECT TOP 1 SaleID FROM Sales WHERE IsPosted=1 ORDER BY SaleID DESC");
            if (dtPreview != null && dtPreview.Rows.Count > 0)
            {
                int previewSaleID = Convert.ToInt32(dtPreview.Rows[0]["SaleID"]);
                FrmPrintSale.PrintPreparationSlip(previewSaleID, size, true);
            }
            else
            {
                MessageBox.Show("لا توجد فواتير مبيعات مسجلة حالياً لمعاينة إذن التحضير.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ApplyCurrentToAppConfig()
        {
            AppConfig.ReceiptPrinterName = cboReceiptPrinter.SelectedIndex <= 0 ? "" : cboReceiptPrinter.SelectedItem.ToString();
            AppConfig.A4PrinterName = cboA4Printer.SelectedIndex <= 0 ? "" : cboA4Printer.SelectedItem.ToString();
            AppConfig.BarcodePrinterName = cboBarcodePrinter.SelectedIndex <= 0 ? "" : cboBarcodePrinter.SelectedItem.ToString();
            AppConfig.DefaultInvoiceFormat = cboInvoiceFormat.SelectedIndex == 0 ? "Receipt" : (cboInvoiceFormat.SelectedIndex == 2 ? "A5" : "A4");
            AppConfig.DefaultReportFormat = cboReportFormat.SelectedIndex == 1 ? "A5" : (cboReportFormat.SelectedIndex == 2 ? "Receipt" : "A4");
            AppConfig.PrintBehaviorOnSave = cboPrintBehavior.SelectedIndex == 1 ? "Direct" : (cboPrintBehavior.SelectedIndex == 2 ? "None" : "Prompt");
            AppConfig.ReceiptPrintMode = cboReceiptPrintMode.SelectedIndex == 1 ? "Compact" : "Detailed";
            AppConfig.POSReceiptMode = cboPOSReceiptMode.SelectedIndex == 2 ? "Never" : cboPOSReceiptMode.SelectedIndex == 1 ? "Ask" : "Always";

            AppConfig.ReceiptTemplate = cboReceiptTemplate.SelectedIndex switch
            {
                1 => "Modern",
                2 => "Compact",
                3 => "Elegant",
                4 => "MiniMarket",
                5 => "GridReceipt",
                6 => "FancyReceipt",
                7 => "RestaurantReceipt",
                8 => "PharmacyReceipt",
                _ => "Standard"
            };
            AppConfig.ReceiptShowDiscount = chkReceiptShowDiscount.Checked;
            AppConfig.ReceiptShowClientInfo = chkReceiptShowClientInfo.Checked;
            AppConfig.ReceiptHeaderNote = txtReceiptHeader.Text.Trim();
            AppConfig.ReceiptFooterNote = txtReceiptFooter.Text.Trim();

            AppConfig.A4Template = cboA4Template.SelectedIndex switch
            {
                0 => "CommercialGrid",
                1 => "CommercialGridNoDiscount",
                2 => "AlTarekGrid",
                3 => "AlTarekNoDiscount",
                4 => "Classic",
                5 => "Modern",
                6 => "Official",
                7 => "Simple",
                8 => "SparePartsGrid",
                9 => "SupermarketA4",
                10 => "ElegantClassic",
                11 => "CorporateModern",
                _ => "AlTarekGrid"
            };

            if (numA5ShiftCm != null)
            {
                int pts = (int)Math.Round(((double)numA5ShiftCm.Value / 2.54) * 100);
                AppConfig.A5ShiftRight = pts;
            }

            if (cboPrepTemplate != null)
            {
                AppConfig.PreparationSlipTemplate = cboPrepTemplate.SelectedIndex switch
                {
                    1 => "Standard",
                    2 => "Modern",
                    _ => "Disbursement"
                };
            }
            if (cboPrepPaperSize != null)
            {
                AppConfig.PreparationPaperSize = cboPrepPaperSize.SelectedIndex switch
                {
                    1 => "A5",
                    2 => "Receipt",
                    _ => "A4"
                };
            }
            if (cboPrepA5Shift != null && numPrepA5ShiftCm != null)
            {
                if (cboPrepA5Shift.SelectedIndex == 0)
                {
                    AppConfig.PreparationA5UseCustomShift = false;
                }
                else
                {
                    AppConfig.PreparationA5UseCustomShift = true;
                    int pts = (int)Math.Round(((double)numPrepA5ShiftCm.Value / 2.54) * 100);
                    AppConfig.PreparationA5ShiftRight = pts;
                }
            }

            AppConfig.BarcodeTemplate = cboBarcodeTemplate.SelectedIndex switch
            {
                1 => "PriceHeavy",
                2 => "Small",
                3 => "Shelf",
                4 => "NoPrice",
                _ => "Standard"
            };

            if (cboBarcodeEncoding.SelectedIndex >= 0)
            {
                var encList = BarcodeEngine.GetAvailableEncodings();
                if (cboBarcodeEncoding.SelectedIndex < encList.Count)
                {
                    AppConfig.BarcodeEncoding = encList[cboBarcodeEncoding.SelectedIndex].Item1;
                }
            }

            AppConfig.BarcodeStickerSize = cboBarcodeStickerSize.SelectedIndex switch
            {
                3 => "50x25_double",
                2 => "38x26_double",
                1 => "38x26",
                _ => "50x30"
            };
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            ApplyCurrentToAppConfig();
            MessageBox.Show("✅ تم حفظ إعدادات الطابعات ونماذج الفواتير بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة إعدادات الطابعات ونماذج الفواتير والباركود وأذونات التحضير
    /// بتصميم بطاقات عصري ومنظم ومريح للعين
    /// </summary>
    public class FrmPrinterSettings : Form
    {
        // ── Tab 1 Controls: الطابعات وسلوك الطباعة ──────────────────
        private ComboBox cboReceiptPrinter;
        private NumericUpDown numReceiptCopies;       // عدد نسخ طباعة ريسيت البيع
        private ComboBox cboA4Printer;
        private ComboBox cboBarcodePrinter;
        private ComboBox cboKitchenPrinter;           // طابعة المطبخ والتحضير
        private ComboBox cboInvoiceFormat;
        private ComboBox cboReportFormat;
        private ComboBox cboVoucherFormat;
        private ComboBox cboPrintBehavior;
        private ComboBox cboReceiptPrintMode;
        private ComboBox cboPOSReceiptMode;
        private CheckBox chkKitchenAutoPrint;         // طباعة بون تلقائية عند البيع
        private CheckBox chkKitchenPrintBlendRecipe;  // طباعة التوليفة داخل البون

        // ── Tab 2 Controls: نماذج الفواتير والريسيت ──────────────────
        private ComboBox cboReceiptTemplate;
        private CheckBox chkReceiptShowDiscount;
        private CheckBox chkReceiptShowClientInfo;
        private TextBox txtReceiptHeader;
        private TextBox txtReceiptFooter;
        private ComboBox cboA4Template;
        private ComboBox cboA5Shift;
        private NumericUpDown numA5ShiftCm;
        private NumericUpDown numA4ShiftCm;

        // ── Tab 3 Controls: أذونات التحضير والباركود ──────────────────
        private ComboBox cboPrepTemplate;
        private ComboBox cboPrepPaperSize;
        private NumericUpDown numPrepCopies;          // عدد نسخ طباعة إذن التحضير
        private ComboBox cboPrepA5Shift;
        private NumericUpDown numPrepA5ShiftCm;
        private ComboBox cboBarcodeTemplate;
        private ComboBox cboBarcodeEncoding;
        private ComboBox cboBarcodeStickerSize;
        private CheckBox chkBarcodePrintCategory;
        private CheckBox chkBarcodePrintPrice;

        public FrmPrinterSettings()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "إعدادات الطابعات ونماذج الفواتير";
            this.Size = new Size(860, 770);
            this.MinimumSize = new Size(840, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Font = Theme.FontMain;

            // ── 1. Top Header Bar (Modern, Calm Navy Header) ──
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20, 10, 20, 10)
            };
            var lblTitle = new Label
            {
                Text = "إعدادات الطابعات ونماذج الفواتير والباركود",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 8)
            };
            var lblSub = new Label
            {
                Text = "تخصيص الطابعات الافتراضية، مقاسات الفواتير والتقارير، قوالب الطباعة، وهوامش ورق A4/A5 وملصقات الباركود",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Location = new Point(20, 34)
            };
            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblSub);
            this.Controls.Add(pnlTop);

            // ── 2. Bottom Footer Bar (Docked Bottom with Prominent Save Button) ──
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
                }
            };

            var btnSave = new Button
            {
                Text = "حفظ إعدادات الطباعة",
                Size = new Size(230, 40),
                Location = new Point(20, 10),
                BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            pnlFooter.Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "إغلاق النافذة",
                Size = new Size(130, 40),
                Location = new Point(265, 10),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnCancel);

            this.Controls.Add(pnlFooter);

            // ── 3. Main TabControl (Clean Tabs Without Broken Emojis) ──
            var pnlTabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 12, 15, 12),
                BackColor = Color.FromArgb(243, 244, 246)
            };

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ItemSize = new Size(180, 36),
                SizeMode = TabSizeMode.Fixed
            };

            // ── Tab 1: الطابعات وسلوك الطباعة ────────────────────────
            var tabPrinters = new TabPage("الطابعات وسلوك الطباعة")
            {
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true,
                Padding = new Padding(12)
            };
            tabControl.TabPages.Add(tabPrinters);
            BuildTabPrinters(tabPrinters);

            // ── Tab 2: قوالب ونماذج الفواتير ─────────────────
            var tabTemplates = new TabPage("قوالب ونماذج الفواتير")
            {
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true,
                Padding = new Padding(12)
            };
            tabControl.TabPages.Add(tabTemplates);
            BuildTabTemplates(tabTemplates);

            // ── Tab 3: أذونات التحضير والباركود ──────────────────────
            var tabPrepAndBarcode = new TabPage("أذونات التحضير والباركود")
            {
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true,
                Padding = new Padding(12)
            };
            tabControl.TabPages.Add(tabPrepAndBarcode);
            BuildTabPrepAndBarcode(tabPrepAndBarcode);

            pnlTabContainer.Controls.Add(tabControl);
            this.Controls.Add(pnlTabContainer);

            // Ensure proper docking order so Top Banner and Footer Bar are docked first, and Fill claims the middle area
            pnlTop.SendToBack();
            pnlFooter.SendToBack();
            pnlTabContainer.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        /// <summary>
        /// ينشئ كارت أبيض أنيق مع شريط جانبي ملون وعنوان بارز لإراحة العين وفصل الإعدادات
        /// </summary>
        private Panel CreateSectionCard(string title, Color accentColor, int width, int height)
        {
            var card = new Panel
            {
                Size = new Size(width, height),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 14)
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var penBorder = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                {
                    g.DrawRectangle(penBorder, 0, 0, card.Width - 1, card.Height - 1);
                }
                // شريط ملون بالطرف الأيمن في نظام الـ RTL
                using (var brushAccent = new SolidBrush(accentColor))
                {
                    g.FillRectangle(brushAccent, card.Width - 5, 0, 5, card.Height);
                }
            };

            var lblHeader = new Label
            {
                Text = title,
                Location = new Point(15, 8),
                Size = new Size(width - 35, 24),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.MiddleRight
            };
            card.Controls.Add(lblHeader);

            var div = new Panel
            {
                Location = new Point(12, 34),
                Size = new Size(width - 24, 1),
                BackColor = Color.FromArgb(241, 245, 249)
            };
            card.Controls.Add(div);

            return card;
        }

        private void BuildTabPrinters(TabPage page)
        {
            int cardW = 745;
            int curY = 12;

            // =========================================================================
            // ── البطاقة 1: أجهزة الطابعات الافتراضية (Hardware Devices) ──
            // =========================================================================
            var cardPrinters = CreateSectionCard("أجهزة الطابعات الافتراضية المتصلة بالجهاز", Color.FromArgb(37, 99, 235), cardW, 255);
            cardPrinters.Location = new Point(15, curY);

            // 1. طابعة الريسيت الحراري + عدد النسخ
            AddLabel(cardPrinters, "طابعة الريسيت الحراري الافتراضية (Receipt 80mm):", 340, 44);
            cboReceiptPrinter = MakePrintersCombo(340, 68, 385);
            cboReceiptPrinter.SelectedItem = string.IsNullOrEmpty(AppConfig.ReceiptPrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.ReceiptPrinterName;
            if (cboReceiptPrinter.SelectedIndex == -1 && cboReceiptPrinter.Items.Count > 0) cboReceiptPrinter.SelectedIndex = 0;
            cardPrinters.Controls.Add(cboReceiptPrinter);

            AddLabel(cardPrinters, "عدد نسخ طباعة ريسيت البيع تلقائياً:", 20, 44);
            numReceiptCopies = new NumericUpDown
            {
                Location = new Point(175, 68),
                Width = 90,
                Minimum = 1,
                Maximum = 10,
                Value = AppConfig.ReceiptCopies,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
            };
            cardPrinters.Controls.Add(numReceiptCopies);

            var lblCopiesUnit = new Label
            {
                Text = "نسخة (لكل بيع)",
                Location = new Point(60, 72),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cardPrinters.Controls.Add(lblCopiesUnit);

            // 2. طابعة ورق A4 / A5 والتقارير
            AddLabel(cardPrinters, "طابعة ورق A4 / A5 والتقارير الافتراضية:", 20, 110);
            cboA4Printer = MakePrintersCombo(20, 134, 705);
            cboA4Printer.SelectedItem = string.IsNullOrEmpty(AppConfig.A4PrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.A4PrinterName;
            if (cboA4Printer.SelectedIndex == -1 && cboA4Printer.Items.Count > 0) cboA4Printer.SelectedIndex = 0;
            cardPrinters.Controls.Add(cboA4Printer);

            // 3. طابعة ملصقات الباركود
            AddLabel(cardPrinters, "طابعة ملصقات واستيكرات الباركود الافتراضية (Barcode Stickers):", 20, 176);
            cboBarcodePrinter = MakePrintersCombo(20, 200, 705);
            cboBarcodePrinter.SelectedItem = string.IsNullOrEmpty(AppConfig.BarcodePrinterName) ? "(طابعة النظام الافتراضية)" : AppConfig.BarcodePrinterName;
            if (cboBarcodePrinter.SelectedIndex == -1 && cboBarcodePrinter.Items.Count > 0) cboBarcodePrinter.SelectedIndex = 0;
            cardPrinters.Controls.Add(cboBarcodePrinter);

            page.Controls.Add(cardPrinters);
            curY += 270;

            // =========================================================================
            // ── البطاقة 2: طابعة المطبخ وبون التحضير (Kitchen / Prep) ──
            // =========================================================================
            var cardKitchen = CreateSectionCard("طابعة وبون التحضير (Kitchen / Prep Printer) — للمطاعم والكافيهات والتجهيز", Color.FromArgb(217, 119, 6), cardW, 165);
            cardKitchen.Location = new Point(15, curY);

            AddLabel(cardKitchen, "طابعة بون المطبخ والتحضير (أو اختيار نفس طابعة الريسيت تلقائياً):", 20, 44);
            cboKitchenPrinter = new ComboBox
            {
                Location = new Point(20, 68),
                Width = 705,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboKitchenPrinter.Items.Add("(نفس طابعة الريسيت — Fallback تلقائي)");
            try { foreach (string p in PrinterSettings.InstalledPrinters) cboKitchenPrinter.Items.Add(p); } catch { }
            if (!string.IsNullOrEmpty(AppConfig.KitchenPrinterName))
                cboKitchenPrinter.SelectedItem = AppConfig.KitchenPrinterName;
            if (cboKitchenPrinter.SelectedIndex < 0) cboKitchenPrinter.SelectedIndex = 0;
            cardKitchen.Controls.Add(cboKitchenPrinter);

            chkKitchenAutoPrint = new CheckBox
            {
                Text = "طباعة بون التحضير تلقائياً فور حفظ عملية البيع في شاشة الكاشير (POS)",
                Location = new Point(320, 110),
                Size = new Size(425, 24),
                Checked = AppConfig.KitchenAutoPrint,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cardKitchen.Controls.Add(chkKitchenAutoPrint);

            chkKitchenPrintBlendRecipe = new CheckBox
            {
                Text = "طباعة تفاصيل وتوليفة الصنف (المكونات والمقادير) داخل بون التحضير",
                Location = new Point(20, 110),
                Size = new Size(300, 24),
                Checked = AppConfig.KitchenPrintBlendRecipe,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cardKitchen.Controls.Add(chkKitchenPrintBlendRecipe);

            page.Controls.Add(cardKitchen);
            curY += 180;

            // =========================================================================
            // ── البطاقة 3: مقاسات وأحجام الطباعة الافتراضية ──
            // =========================================================================
            var cardSizes = CreateSectionCard("مقاسات وأحجام الطباعة الافتراضية للفواتير والتقارير والسندات", Color.FromArgb(5, 150, 105), cardW, 185);
            cardSizes.Location = new Point(15, curY);

            // Row 1: Invoice Format + Report Format
            AddLabel(cardSizes, "حجم طباعة الفاتورة الافتراضي:", 380, 44);
            cboInvoiceFormat = new ComboBox
            {
                Location = new Point(380, 68),
                Width = 345,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboInvoiceFormat.Items.AddRange(new object[]
            {
                "ريسيت حراري (Receipt 80mm)",
                "ورق A4 كامل (A4 Sheet)",
                "ورق A5 نصف صفحة (A5 Sheet)"
            });
            cboInvoiceFormat.SelectedIndex = AppConfig.DefaultInvoiceFormat == "Receipt" ? 0 : (AppConfig.DefaultInvoiceFormat == "A5" ? 2 : 1);
            cardSizes.Controls.Add(cboInvoiceFormat);

            AddLabel(cardSizes, "حجم طباعة التقارير الافتراضي:", 20, 44);
            cboReportFormat = new ComboBox
            {
                Location = new Point(20, 68),
                Width = 345,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboReportFormat.Items.AddRange(new object[]
            {
                "ورق A4 كامل (A4 Sheet - الافتراضي والموصى به)",
                "ورق A5 نصف صفحة (A5 Sheet)",
                "ريسيت حراري (Receipt 80mm)"
            });
            cboReportFormat.SelectedIndex = AppConfig.DefaultReportFormat == "A5" ? 1 : (AppConfig.DefaultReportFormat == "Receipt" ? 2 : 0);
            cardSizes.Controls.Add(cboReportFormat);

            // Row 2: Voucher Format
            AddLabel(cardSizes, "مقاس طباعة سندات الصرف والقبض الافتراضي (سندات الخزينة والقبض والدفع):", 20, 110);
            cboVoucherFormat = new ComboBox
            {
                Location = new Point(20, 134),
                Width = 705,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboVoucherFormat.Items.AddRange(new object[]
            {
                "ريسيت حراري (Receipt 80mm / 58mm) - طابعة الريسيت الافتراضية",
                "ورق كامل A4 (210 x 297 mm)",
                "نصف صفحة A5 (148 x 210 mm)",
                "سؤال واختيار المقاس عند كل طباعة (Choice Dialog)"
            });
            cboVoucherFormat.SelectedIndex = AppConfig.VoucherPaperSize == "A4" ? 1 
                                           : (AppConfig.VoucherPaperSize == "A5" ? 2 
                                           : (AppConfig.VoucherPaperSize == "Ask" ? 3 : 0));
            cardSizes.Controls.Add(cboVoucherFormat);

            page.Controls.Add(cardSizes);
            curY += 200;

            // =========================================================================
            // ── البطاقة 4: سلوك الطباعة وطباعة الكاشير POS ──
            // =========================================================================
            var cardBehavior = CreateSectionCard("سلوك الطباعة التلقائية وأنماط الحفظ وشاشات الكاشير (POS)", Color.FromArgb(99, 102, 241), cardW, 185);
            cardBehavior.Location = new Point(15, curY);

            // Row 1: Print Behavior on save
            AddLabel(cardBehavior, "سلوك وسؤال الطباعة عند حفظ الفاتورة (بيع / تسعير / POS):", 20, 44);
            cboPrintBehavior = new ComboBox
            {
                Location = new Point(20, 68),
                Width = 705,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboPrintBehavior.Items.AddRange(new object[]
            {
                "سؤال واختيار نوع ومقاس الطباعة في كل مرة (نافذة الحوار)",
                "طباعة مباشرة وفورية بالمقاس الافتراضي فور الحفظ (بدون سؤال)",
                "عدم الطباعة تلقائياً (الطباعة يدوياً عند الحاجة)"
            });
            cboPrintBehavior.SelectedIndex = AppConfig.PrintBehaviorOnSave == "Direct" ? 1
                                           : (AppConfig.PrintBehaviorOnSave == "None" ? 2 : 0);
            cardBehavior.Controls.Add(cboPrintBehavior);

            // Row 2: Receipt Mode + POS Mode
            AddLabel(cardBehavior, "نمط طباعة الفاتورة:", 380, 110);
            cboReceiptPrintMode = new ComboBox
            {
                Location = new Point(380, 134),
                Width = 345,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboReceiptPrintMode.Items.AddRange(new object[]
            {
                "مفصل - يظهر الرصيد السابق والحالي والمدفوع",
                "مختصر - يظهر المجموع النهائي فقط"
            });
            cboReceiptPrintMode.SelectedIndex = AppConfig.ReceiptPrintMode == "Compact" ? 1 : 0;
            cardBehavior.Controls.Add(cboReceiptPrintMode);

            AddLabel(cardBehavior, "طباعة رسيت البيع بعد الدفع (POS):", 20, 110);
            cboPOSReceiptMode = new ComboBox
            {
                Location = new Point(20, 134),
                Width = 345,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboPOSReceiptMode.Items.AddRange(new object[]
            {
                "طباعة تلقائية دائماً (رسيت العميل)",
                "اسألني كل مرة (نافذة الاختيار عند الحفظ)",
                "بون المطبخ / التحضير فقط (بدون رسيت عميل)",
                "عدم الطباعة نهائياً بعد الحفظ (لا رسيت ولا مطبخ)"
            });
            cboPOSReceiptMode.SelectedIndex = AppConfig.POSReceiptMode switch
            {
                "None" => 3,
                "Never" or "KitchenOnly" => 2,
                "Ask" => 1,
                _ => 0
            };
            cardBehavior.Controls.Add(cboPOSReceiptMode);

            page.Controls.Add(cardBehavior);
            curY += 205;

            // Extra space at bottom to ensure zero clipping
            var pnlSpacer = new Panel { Location = new Point(15, curY), Size = new Size(cardW, 20), BackColor = Color.Transparent };
            page.Controls.Add(pnlSpacer);
        }

        private void BuildTabTemplates(TabPage page)
        {
            int cardW = 745;
            int curY = 12;

            // =========================================================================
            // ── البطاقة 1: قوالب وإعدادات الريسيت الحراري (Receipt 80mm) ──
            // =========================================================================
            var cardReceipt = CreateSectionCard("قوالب ومظهر فاتورة الريسيت الحراري (Receipt 80mm)", Color.FromArgb(37, 99, 235), cardW, 240);
            cardReceipt.Location = new Point(15, curY);

            AddLabel(cardReceipt, "قالب طباعة الريسيت الحراري الافتراضي:", 180, 44);
            cboReceiptTemplate = new ComboBox
            {
                Location = new Point(180, 68),
                Width = 565,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboReceiptTemplate.Items.AddRange(new object[]
            {
                "النموذج الشبكي المبوب (أعمدة سوداء بارزة - زي الصورة)",
                "النموذج الشبكي المخطط (سطور سوداء عريضة ومتبادلة)",
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
                "DarkGridReceipt" => "النموذج الشبكي المبوب (أعمدة سوداء بارزة - زي الصورة)",
                "DarkRowsReceipt" => "النموذج الشبكي المخطط (سطور سوداء عريضة ومتبادلة)",
                "Modern" => "العصري (Modern)",
                "Compact" => "المبسط السريع (Compact)",
                "Elegant" => "الفواتير الاحترافية (Elegant)",
                "MiniMarket" => "قالب ميني ماركت (MiniMarket)",
                "GridReceipt" => "النموذج الشبكي (GridReceipt)",
                "FancyReceipt" => "النموذج الزخرفي (FancyReceipt)",
                "RestaurantReceipt" => "قالب كافيه ومطاعم (RestaurantReceipt)",
                "PharmacyReceipt" => "قالب صيدليات وأغذية (PharmacyReceipt)",
                _ => "النموذج الشبكي المبوب (أعمدة سوداء بارزة - زي الصورة)"
            };
            if (cboReceiptTemplate.SelectedIndex == -1) cboReceiptTemplate.SelectedIndex = 0;
            cardReceipt.Controls.Add(cboReceiptTemplate);

            var btnPreviewReceipt = new Button
            {
                Text = "معاينة الريسيت",
                Location = new Point(20, 66),
                Size = new Size(150, 32),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPreviewReceipt.FlatAppearance.BorderSize = 0;
            btnPreviewReceipt.Click += (s, e) => PreviewReceipt();
            cardReceipt.Controls.Add(btnPreviewReceipt);

            // خيارات الريسيت
            chkReceiptShowDiscount = new CheckBox
            {
                Text = "إظهار عمود الخصم في جدول الأصناف بالريسيت",
                Location = new Point(380, 110),
                Size = new Size(355, 22),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Checked = AppConfig.ReceiptShowDiscount
            };
            cardReceipt.Controls.Add(chkReceiptShowDiscount);

            chkReceiptShowClientInfo = new CheckBox
            {
                Text = "إظهار بيانات العميل (اسم + هاتف + عنوان) في رأس الريسيت",
                Location = new Point(20, 110),
                Size = new Size(355, 22),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Checked = AppConfig.ReceiptShowClientInfo
            };
            cardReceipt.Controls.Add(chkReceiptShowClientInfo);

            // مقدمة وخاتمة الفاتورة
            AddLabel(cardReceipt, "ترويسة ومقدمة الفاتورة (نص بأعلى الريسيت):", 380, 142);
            txtReceiptHeader = new TextBox
            {
                Location = new Point(380, 166),
                Width = 345,
                Height = 54,
                Multiline = true,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Text = AppConfig.ReceiptHeaderNote
            };
            cardReceipt.Controls.Add(txtReceiptHeader);

            AddLabel(cardReceipt, "تذييل وخاتمة الفاتورة (سياسة الاستبدال أو رسالة شكر):", 20, 142);
            txtReceiptFooter = new TextBox
            {
                Location = new Point(20, 166),
                Width = 345,
                Height = 54,
                Multiline = true,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Text = AppConfig.ReceiptFooterNote
            };
            cardReceipt.Controls.Add(txtReceiptFooter);

            page.Controls.Add(cardReceipt);
            curY += 255;

            // =========================================================================
            // ── البطاقة 2: قوالب ومحاذاة ورق A4 و A5 ──
            // =========================================================================
            var cardA4 = CreateSectionCard("قوالب الفواتير الكاملة A4 ومحاذاة دفاتر A5 بالسنتيمتر", Color.FromArgb(5, 150, 105), cardW, 260);
            cardA4.Location = new Point(15, curY);

            AddLabel(cardA4, "قالب طباعة ورق A4 / A5 الافتراضي:", 280, 44);
            cboA4Template = new ComboBox
            {
                Location = new Point(280, 68),
                Width = 465,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
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
            cardA4.Controls.Add(cboA4Template);

            var btnPreviewA4 = new Button
            {
                Text = "معاينة A4",
                Location = new Point(150, 66),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPreviewA4.FlatAppearance.BorderSize = 0;
            btnPreviewA4.Click += (s, e) => PreviewA4();
            cardA4.Controls.Add(btnPreviewA4);

            var btnPreviewA5 = new Button
            {
                Text = "معاينة A5",
                Location = new Point(20, 66),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPreviewA5.FlatAppearance.BorderSize = 0;
            btnPreviewA5.Click += (s, e) => PreviewA5();
            cardA4.Controls.Add(btnPreviewA5);

            // A5 Shift
            AddLabel(cardA4, "مقاس ومحاذاة ورق A5 (إزاحة درج الطابعة ودفاتر الفواتير المطبوعة):", 20, 110);
            cboA5Shift = new ComboBox
            {
                Location = new Point(165, 134),
                Width = 580,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
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
                Location = new Point(20, 134),
                Width = 135,
                DecimalPlaces = 1,
                Increment = 0.2m,
                Minimum = 0.0m,
                Maximum = 15.0m,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
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
            cardA4.Controls.Add(cboA5Shift);
            cardA4.Controls.Add(numA5ShiftCm);

            // A4 Shift
            AddLabel(cardA4, "إزاحة محتوى فاتورة A4 لليمين بالسنتيمتر (لمعالجة هوامش الطابعة):", 20, 180);
            numA4ShiftCm = new NumericUpDown
            {
                Location = new Point(610, 206),
                Width = 135,
                DecimalPlaces = 1,
                Increment = 0.5m,
                Minimum = -5.0m,
                Maximum = 15.0m,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
            };
            decimal curA4Cm = Math.Round((decimal)(AppConfig.A4ShiftRight * 0.0254), 1);
            if (curA4Cm < -5m) curA4Cm = 0m;
            if (curA4Cm > 15m) curA4Cm = 0m;
            numA4ShiftCm.Value = curA4Cm;
            cardA4.Controls.Add(numA4ShiftCm);

            var lblA4Hint = new Label
            {
                Text = "اضبط الإزاحة لتوسيط جدول الفاتورة تماماً على ورقة A4 (الافتراضي 0 أو بين 1.0 و 3.0 سم)",
                Location = new Point(20, 210),
                Size = new Size(580, 20),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };
            cardA4.Controls.Add(lblA4Hint);

            page.Controls.Add(cardA4);
            curY += 275;

            var pnlSpacer = new Panel { Location = new Point(15, curY), Size = new Size(cardW, 20), BackColor = Color.Transparent };
            page.Controls.Add(pnlSpacer);
        }

        private void BuildTabPrepAndBarcode(TabPage page)
        {
            int cardW = 745;
            int curY = 12;

            // =========================================================================
            // ── البطاقة 1: نماذج إذن التحضير وصرف البضاعة بالمخزن ──
            // =========================================================================
            var cardPrep = CreateSectionCard("نماذج إذن التحضير وصرف البضاعة بالمخزن (Disbursement Slip)", Color.FromArgb(217, 119, 6), cardW, 200);
            cardPrep.Location = new Point(15, curY);

            // Row 1: Template + Paper Size
            AddLabel(cardPrep, "نموذج إذن التحضير وصرف البضاعة:", 380, 44);
            cboPrepTemplate = new ComboBox
            {
                Location = new Point(380, 68),
                Width = 345,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboPrepTemplate.Items.AddRange(new object[]
            {
                "إذن صرف بضاعة مع خانة الملاحظات (Disbursement)",
                "النموذج القياسي مع موقع الرف (Standard Grid)",
                "النموذج العصري الحديث (Modern Dark)"
            });
            cboPrepTemplate.SelectedIndex = AppConfig.PreparationSlipTemplate == "Standard" ? 1
                                          : AppConfig.PreparationSlipTemplate == "Modern" ? 2
                                          : 0;
            if (cboPrepTemplate.SelectedIndex == -1) cboPrepTemplate.SelectedIndex = 0;
            cardPrep.Controls.Add(cboPrepTemplate);

            AddLabel(cardPrep, "مقاس ورق إذن التحضير الافتراضي:", 20, 44);
            cboPrepPaperSize = new ComboBox
            {
                Location = new Point(20, 68),
                Width = 345,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
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
            cardPrep.Controls.Add(cboPrepPaperSize);

            // Row 2: Copies + A5 Shift + Preview
            AddLabel(cardPrep, "عدد نسخ الطباعة:", 580, 110);
            numPrepCopies = new NumericUpDown
            {
                Location = new Point(580, 134),
                Width = 165,
                Minimum = 1,
                Maximum = 10,
                Value = AppConfig.PreparationSlipCopies,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
            };
            cardPrep.Controls.Add(numPrepCopies);

            AddLabel(cardPrep, "محاذاة إذن التحضير A5 وإزاحة الدرج:", 190, 110);
            cboPrepA5Shift = new ComboBox
            {
                Location = new Point(310, 134),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 9.5f)
            };
            cboPrepA5Shift.Items.AddRange(new object[]
            {
                "مطابق لمحاذاة الفاتورة A5 تلقائياً (توصية)",
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
            cardPrep.Controls.Add(cboPrepA5Shift);

            numPrepA5ShiftCm = new NumericUpDown
            {
                Location = new Point(190, 134),
                Width = 110,
                DecimalPlaces = 1,
                Increment = 0.2m,
                Minimum = 0.0m,
                Maximum = 15.0m,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = HorizontalAlignment.Center
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
            cardPrep.Controls.Add(numPrepA5ShiftCm);

            var btnPreviewPrepA5 = new Button
            {
                Text = "معاينة إذن التحضير",
                Location = new Point(20, 132),
                Size = new Size(160, 32),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPreviewPrepA5.FlatAppearance.BorderSize = 0;
            btnPreviewPrepA5.Click += (s, e) => PreviewPrepSlip("A5");
            cardPrep.Controls.Add(btnPreviewPrepA5);

            page.Controls.Add(cardPrep);
            curY += 215;

            // =========================================================================
            // ── البطاقة 2: إعدادات ملصقات واستيكرات الباركود ──
            // =========================================================================
            var cardBarcode = CreateSectionCard("إعدادات ملصقات واستيكرات الباركود (Barcode Stickers)", Color.FromArgb(13, 148, 136), cardW, 230);
            cardBarcode.Location = new Point(15, curY);

            // Row 1: Barcode Template + Sticker Size
            AddLabel(cardBarcode, "قالب ملصق الباركود الافتراضي:", 380, 44);
            cboBarcodeTemplate = new ComboBox
            {
                Location = new Point(380, 68),
                Width = 345,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            cboBarcodeTemplate.Items.AddRange(new object[]
            {
                "الافتراضي (اسم صنف + سعر + باركود)",
                "ملصق أجهزة وسيريالات الموبايل (اسم المكان كبير + IMEI)",
                "كود الصنف علي العرض",
                "سعر بارز (سعر كبير + باركود)",
                "ملصق صغير (سعر وباركود فقط)",
                "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)",
                "اسم صنف كبير + باركود (بدون سعر)"
            });
            cboBarcodeTemplate.SelectedItem = AppConfig.BarcodeTemplate switch
            {
                "CodeOnSide" => "كود الصنف علي العرض",
                "MobileIMEI" => "ملصق أجهزة وسيريالات الموبايل (اسم المكان كبير + IMEI)",
                "PriceHeavy" => "سعر بارز (سعر كبير + باركود)",
                "Small" => "ملصق صغير (سعر وباركود فقط)",
                "Shelf" => "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)",
                "NoPrice" or "NoPriceBigName" => "اسم صنف كبير + باركود (بدون سعر)",
                _ => (AppConfig.BusinessType == "Mobiles" ? "ملصق أجهزة وسيريالات الموبايل (اسم المكان كبير + IMEI)" : "الافتراضي (اسم صنف + سعر + باركود)")
            };
            if (cboBarcodeTemplate.SelectedIndex == -1) cboBarcodeTemplate.SelectedIndex = 0;
            cardBarcode.Controls.Add(cboBarcodeTemplate);

            AddLabel(cardBarcode, "مقاس ملصق الباركود (Sticker Size):", 20, 44);
            cboBarcodeStickerSize = new ComboBox
            {
                Location = new Point(20, 68),
                Width = 345,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
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
            cardBarcode.Controls.Add(cboBarcodeStickerSize);

            // Row 2: Encoding
            AddLabel(cardBarcode, "نوع تشفير وترميز الباركود المطبوع (Encoding):", 20, 110);
            cboBarcodeEncoding = new ComboBox
            {
                Location = new Point(20, 134),
                Width = 705,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
            };
            var encList = BarcodeEngine.GetAvailableEncodings();
            foreach (var enc in encList)
            {
                cboBarcodeEncoding.Items.Add(enc.Item2);
            }
            int defEncIdx = encList.FindIndex(x => x.Item1 == AppConfig.BarcodeEncoding);
            cboBarcodeEncoding.SelectedIndex = defEncIdx >= 0 ? defEncIdx : 0;
            cardBarcode.Controls.Add(cboBarcodeEncoding);

            // Row 3: Checkboxes
            chkBarcodePrintPrice = new CheckBox
            {
                Text = "طباعة سعر البيع على ملصقات واستيكرات الباركود بشكل دائم",
                Location = new Point(380, 180),
                Size = new Size(355, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Checked = AppConfig.BarcodePrintPrice
            };
            cardBarcode.Controls.Add(chkBarcodePrintPrice);

            chkBarcodePrintCategory = new CheckBox
            {
                Text = "طباعة اسم التصنيف بدلاً من اسم الصنف على ملصق الباركود",
                Location = new Point(20, 180),
                Size = new Size(355, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Checked = AppConfig.BarcodePrintCategoryInsteadOfName
            };
            cardBarcode.Controls.Add(chkBarcodePrintCategory);

            page.Controls.Add(cardBarcode);
            curY += 245;

            var pnlSpacer = new Panel { Location = new Point(15, curY), Size = new Size(cardW, 20), BackColor = Color.Transparent };
            page.Controls.Add(pnlSpacer);
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(51, 65, 85),
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
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10f)
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
            if (numReceiptCopies != null)
                AppConfig.ReceiptCopies = (int)numReceiptCopies.Value;
            AppConfig.A4PrinterName = cboA4Printer.SelectedIndex <= 0 ? "" : cboA4Printer.SelectedItem.ToString();
            AppConfig.BarcodePrinterName = cboBarcodePrinter.SelectedIndex <= 0 ? "" : cboBarcodePrinter.SelectedItem.ToString();

            // إعدادات طابعة المطبخ والتحضير
            if (cboKitchenPrinter != null)
            {
                var kitchenSelected = cboKitchenPrinter.SelectedItem?.ToString() ?? "";
                AppConfig.KitchenPrinterName = (cboKitchenPrinter.SelectedIndex <= 0
                    || kitchenSelected.StartsWith("(نفس طابعة")) ? "" : kitchenSelected;
            }
            if (chkKitchenAutoPrint != null)
                AppConfig.KitchenAutoPrint = chkKitchenAutoPrint.Checked;
            if (chkKitchenPrintBlendRecipe != null)
                AppConfig.KitchenPrintBlendRecipe = chkKitchenPrintBlendRecipe.Checked;
            AppConfig.DefaultInvoiceFormat = cboInvoiceFormat.SelectedIndex == 0 ? "Receipt" : (cboInvoiceFormat.SelectedIndex == 2 ? "A5" : "A4");
            AppConfig.DefaultReportFormat = cboReportFormat.SelectedIndex == 1 ? "A5" : (cboReportFormat.SelectedIndex == 2 ? "Receipt" : "A4");
            if (cboVoucherFormat != null)
            {
                AppConfig.VoucherPaperSize = cboVoucherFormat.SelectedIndex switch
                {
                    1 => "A4",
                    2 => "A5",
                    3 => "Ask",
                    _ => "Receipt"
                };
            }
            AppConfig.PrintBehaviorOnSave = cboPrintBehavior.SelectedIndex == 1 ? "Direct" : (cboPrintBehavior.SelectedIndex == 2 ? "None" : "Prompt");
            AppConfig.ReceiptPrintMode = cboReceiptPrintMode.SelectedIndex == 1 ? "Compact" : "Detailed";
            AppConfig.POSReceiptMode = cboPOSReceiptMode.SelectedIndex switch
            {
                3 => "None",
                2 => "Never",
                1 => "Ask",
                _ => "Always"
            };

            AppConfig.ReceiptTemplate = cboReceiptTemplate.SelectedItem?.ToString() switch
            {
                "النموذج الشبكي المبوب (أعمدة سوداء بارزة - زي الصورة)" => "DarkGridReceipt",
                "النموذج الشبكي المخطط (سطور سوداء عريضة ومتبادلة)" => "DarkRowsReceipt",
                "العصري (Modern)" => "Modern",
                "المبسط السريع (Compact)" => "Compact",
                "الفواتير الاحترافية (Elegant)" => "Elegant",
                "قالب ميني ماركت (MiniMarket)" => "MiniMarket",
                "النموذج الشبكي (GridReceipt)" => "GridReceipt",
                "النموذج الزخرفي (FancyReceipt)" => "FancyReceipt",
                "قالب كافيه ومطاعم (RestaurantReceipt)" => "RestaurantReceipt",
                "قالب صيدليات وأغذية (PharmacyReceipt)" => "PharmacyReceipt",
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

            if (numA4ShiftCm != null)
            {
                int pts4 = (int)Math.Round(((double)numA4ShiftCm.Value / 2.54) * 100);
                AppConfig.A4ShiftRight = pts4;
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
            if (numPrepCopies != null)
            {
                AppConfig.PreparationSlipCopies = (int)numPrepCopies.Value;
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

            string bTmpl = cboBarcodeTemplate.SelectedItem?.ToString() ?? "";
            AppConfig.BarcodeTemplate = (bTmpl.Contains("كود الصنف علي العرض") || bTmpl.Contains("كود الصنف على العرض") || bTmpl.Contains("CodeOnSide")) ? "CodeOnSide"
                                      : (bTmpl.Contains("MobileIMEI") || bTmpl.Contains("الموبايل") || bTmpl.Contains("IMEI")) ? "MobileIMEI"
                                      : bTmpl.Contains("سعر بارز") ? "PriceHeavy"
                                      : bTmpl.Contains("ملصق صغير") ? "Small"
                                      : bTmpl.Contains("ملصق الرف") ? "Shelf"
                                      : bTmpl.Contains("بدون سعر") ? "NoPrice"
                                      : "Standard";

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

            if (chkBarcodePrintCategory != null)
                AppConfig.BarcodePrintCategoryInsteadOfName = chkBarcodePrintCategory.Checked;

            if (chkBarcodePrintPrice != null)
                AppConfig.BarcodePrintPrice = chkBarcodePrintPrice.Checked;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            ApplyCurrentToAppConfig();
            MessageBox.Show("تم حفظ إعدادات الطابعات ونماذج الفواتير بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmFinancialPosition : Form
    {
        // ── عناصر واجهة المستخدم الرئيسية ──
        private TabControl tabMain;
        private TabPage tabDashboard;
        private TabPage tabIncomeStatement;
        private TabPage tabBalanceSheet;
        private TabPage tabAdjustments;

        // ── كروت التبويب الأول (الملخص والمؤشرات) ──
        private Label lblTotalCash;
        private Label lblInventoryPurchase;
        private Label lblClientReceivables;
        private Label lblSupplierPayables;
        private Label lblWorkingCapital;
        private Label lblNetProfitDashboard;

        // المؤشرات المالية
        private Label lblCurrentRatio;
        private Label lblQuickRatio;
        private Label lblInvTurnover;
        private Label lblClientTurnover;
        private Label lblSupplierTurnover;

        // جداول التبويب الأول
        private DataGridView dgSafes;
        private DataGridView dgTopClients;
        private DataGridView dgTopSuppliers;

        // ── تبويب قائمة الدخل ──
        private DateTimePicker dtpPLFrom;
        private DateTimePicker dtpPLTo;
        private Button btnReloadPL;
        private DataGridView dgPL;

        // ── تبويب الميزانية العمومية ──
        private DateTimePicker dtpBSDate;
        private Button btnReloadBS;
        private DataGridView dgBS;

        // ── تبويب التسويات الدفترية ──
        private DataGridView dgAdjustments;
        private Button btnSaveAdjustments;

        // قاموس الحسابات الدفترية
        private Dictionary<string, decimal> adjustments = new Dictionary<string, decimal>();

        private struct AdjustmentAccount
        {
            public string Key;
            public string Name;
            public string Group;

            public AdjustmentAccount(string key, string name, string group)
            {
                Key = key;
                Name = name;
                Group = group;
            }
        }

        private readonly List<AdjustmentAccount> adjustmentDefinitions = new List<AdjustmentAccount>
        {
            // الأصول غير المتداولة
            new AdjustmentAccount("Land", "الأراضي", "أصول غير متداولة"),
            new AdjustmentAccount("Buildings", "المباني", "أصول غير متداولة"),
            new AdjustmentAccount("Machinery", "الآلات والمعدات", "أصول غير متداولة"),
            new AdjustmentAccount("Vehicles", "السيارات", "أصول غير متداولة"),
            new AdjustmentAccount("Furniture", "الأثاث والتجهيزات", "أصول غير متداولة"),
            new AdjustmentAccount("Computers", "أجهزة الحاسب", "أصول غير متداولة"),
            new AdjustmentAccount("Investments", "الاستثمارات طويلة الأجل", "أصول غير متداولة"),
            new AdjustmentAccount("Intangibles", "الأصول غير الملموسة", "أصول غير متداولة"),
            new AdjustmentAccount("AccumulatedDepreciation", "مجمع الإهلاك (-)", "أصول غير متداولة"),

            // الأصول المتداولة الدفترية
            new AdjustmentAccount("NotesReceivable", "أوراق القبض", "أصول متداولة"),
            new AdjustmentAccount("PrepaidExpenses", "المصروفات المقدمة", "أصول متداولة"),
            new AdjustmentAccount("AccruedRevenues", "الإيرادات المستحقة", "أصول متداولة"),
            new AdjustmentAccount("CustodiesAdvances", "العهد والسلف", "أصول متداولة"),

            // الخصوم المتداولة الدفترية
            new AdjustmentAccount("NotesPayable", "أوراق الدفع", "خصوم متداولة"),
            new AdjustmentAccount("ShortTermLoans", "القروض قصيرة الأجل", "خصوم متداولة"),
            new AdjustmentAccount("AccruedTax", "الضرائب المستحقة", "خصوم متداولة"),
            new AdjustmentAccount("AccruedInsurance", "التأمينات المستحقة", "خصوم متداولة"),
            new AdjustmentAccount("AccruedExpenses", "المصروفات المستحقة", "خصوم متداولة"),
            new AdjustmentAccount("DeferredRevenues", "الإيرادات المقدمة", "خصوم متداولة"),

            // الخصوم طويلة الأجل
            new AdjustmentAccount("LongTermLoans", "القروض طويلة الأجل", "خصوم طويلة الأجل"),
            new AdjustmentAccount("LongTermLiabilities", "الالتزامات طويلة الأجل", "خصوم طويلة الأجل"),

            // حقوق الملكية
            new AdjustmentAccount("Capital", "رأس المال الافتتاحي", "حقوق ملكية"),
            new AdjustmentAccount("LegalReserve", "الاحتياطي القانوني", "حقوق ملكية"),
            new AdjustmentAccount("GeneralReserve", "الاحتياطي العام", "حقوق ملكية"),
            new AdjustmentAccount("RetainedEarnings", "الأرباح المحتجزة", "حقوق ملكية"),
            new AdjustmentAccount("Drawings", "المسحوبات الشخصية (-)", "حقوق ملكية"),

            // إيرادات ومصروفات أخرى (قائمة الدخل)
            new AdjustmentAccount("GainOnAssetSale", "أرباح بيع أصول", "إيرادات أخرى"),
            new AdjustmentAccount("InterestEarned", "فوائد دائنة", "إيرادات أخرى"),
            new AdjustmentAccount("FXGain", "أرباح فروق العملة", "إيرادات أخرى"),
            new AdjustmentAccount("OtherRevenues", "إيرادات أخرى متنوعة", "إيرادات أخرى"),
            new AdjustmentAccount("InterestPaid", "فوائد مدينة", "مصروفات أخرى"),
            new AdjustmentAccount("FXLoss", "خسائر فروق العملة", "مصروفات أخرى"),
            new AdjustmentAccount("LossOnAssetSale", "خسائر بيع أصول", "مصروفات أخرى"),
            new AdjustmentAccount("FinesPenalties", "غرامات وجزاءات", "مصروفات أخرى"),
            new AdjustmentAccount("OtherExpenses", "مصروفات أخرى متنوعة", "مصروفات أخرى"),
            new AdjustmentAccount("IncomeTax", "ضريبة الدخل المستقطعة", "ضرائب")
        };

        public FrmFinancialPosition()
        {
            InitUI();
            LoadAdjustments();
            LoadDashboardData();
            LoadPLData();
            LoadBSData();
        }

        private void InitUI()
        {
            this.Text = "📊 موديول المحاسبة والموقف المالي المتكامل";
            this.Size = new Size(1250, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── العنوان الرئيسي ──
            var pnlTitle = Theme.MakeTitleBar("📊 لوحة الحسابات العامة والقوائم المالية", "النظام المحاسبي الموحد: الموقف المالي، قائمة الدخل، الميزانية العمومية، والتسويات الدفترية");
            pnlTitle.Dock = DockStyle.Top;

            var btnPrintPosition = Theme.MakeButton("🖨️ طباعة الموقف المالي للمكان", 15, 10, 230, 36, Theme.Primary);
            btnPrintPosition.Click += BtnPrintFinancialPosition_Click;
            pnlTitle.Controls.Add(btnPrintPosition);
            btnPrintPosition.BringToFront();

            this.Controls.Add(pnlTitle);

            // ── TabControl الرئيسي ──
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontBold,
                Padding = new Point(12, 6)
            };
            this.Controls.Add(tabMain);
            tabMain.BringToFront();

            // 1. تبويب لوحة الملخص
            tabDashboard = new TabPage("📊 ملخص الموقف والمؤشرات") { BackColor = Theme.BgMain };
            InitDashboardTab();
            tabMain.TabPages.Add(tabDashboard);

            // 2. تبويب قائمة الدخل
            tabIncomeStatement = new TabPage("📉 قائمة الدخل والربحية") { BackColor = Theme.BgMain };
            InitPLTab();
            tabMain.TabPages.Add(tabIncomeStatement);

            // 3. تبويب الميزانية العمومية
            tabBalanceSheet = new TabPage("⚖️ الميزانية العمومية") { BackColor = Theme.BgMain };
            InitBSTab();
            tabMain.TabPages.Add(tabBalanceSheet);

            // 4. تبويب التسويات الدفترية
            tabAdjustments = new TabPage("⚙️ ضبط الحسابات الدفترية") { BackColor = Theme.BgMain };
            InitAdjustmentsTab();
            tabMain.TabPages.Add(tabAdjustments);

            // ربط حدث تغيير التبويب لإعادة التحميل
            tabMain.SelectedIndexChanged += (s, e) =>
            {
                if (tabMain.SelectedTab == tabDashboard) LoadDashboardData();
                else if (tabMain.SelectedTab == tabIncomeStatement) LoadPLData();
                else if (tabMain.SelectedTab == tabBalanceSheet) LoadBSData();
                else if (tabMain.SelectedTab == tabAdjustments) LoadAdjustments();
            };

            Theme.ApplyFormRTL(this);
        }

        #region تهيئة تبويبات واجهة المستخدم

        private void InitDashboardTab()
        {
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f)); // الكروت الإحصائية الرئيسية
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));  // نسب السيولة والدوران
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // التفاصيل والجداول
            tabDashboard.Controls.Add(mainLayout);

            // ── صف 1: الكروت الإحصائية الرئيسية ──
            TableLayoutPanel pnlCards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1 };
            for (int i = 0; i < 6; i++) pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
            mainLayout.Controls.Add(pnlCards, 0, 0);

            pnlCards.Controls.Add(CreateMiniCard("💵 النقدية بالخزائن", "0.00 ج", Theme.Primary, out lblTotalCash), 0, 0);
            pnlCards.Controls.Add(CreateMiniCard("📦 قيمة المخزون بالشراء", "0.00 ج", Theme.Accent, out lblInventoryPurchase), 1, 0);
            pnlCards.Controls.Add(CreateMiniCard("👥 مديونيات العملاء طرفنا", "0.00 ج", Theme.Success, out lblClientReceivables), 2, 0);
            pnlCards.Controls.Add(CreateMiniCard("🏢 مطلوبات الموردين منا", "0.00 ج", Theme.Danger, out lblSupplierPayables), 3, 0);
            pnlCards.Controls.Add(CreateMiniCard("⚖️ رأس المال العامل", "0.00 ج", Color.FromArgb(23, 162, 184), out lblWorkingCapital), 4, 0);
            pnlCards.Controls.Add(CreateMiniCard("📈 صافي أرباح الفترة", "0.00 ج", Color.FromArgb(111, 66, 193), out lblNetProfitDashboard), 5, 0);

            // ── صف 2: المؤشرات المالية ونسب السيولة ──
            var grpRatios = new GroupBox
            {
                Text = "⚡ مؤشرات السيولة ومعدلات الدوران",
                Dock = DockStyle.Fill,
                ForeColor = Theme.Accent,
                Font = Theme.FontBold
            };
            mainLayout.Controls.Add(grpRatios, 0, 1);

            TableLayoutPanel pnlRatios = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(3)
            };
            for (int i = 0; i < 5; i++) pnlRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            grpRatios.Controls.Add(pnlRatios);

            pnlRatios.Controls.Add(CreateRatioLabel("نسبة التداول:", out lblCurrentRatio), 0, 0);
            pnlRatios.Controls.Add(CreateRatioLabel("نسبة السيولة السريعة:", out lblQuickRatio), 1, 0);
            pnlRatios.Controls.Add(CreateRatioLabel("دوران المخزون (مرة):", out lblInvTurnover), 2, 0);
            pnlRatios.Controls.Add(CreateRatioLabel("دوران العملاء (مرة):", out lblClientTurnover), 3, 0);
            pnlRatios.Controls.Add(CreateRatioLabel("دوران الموردين (مرة):", out lblSupplierTurnover), 4, 0);

            // ── صف 3: لوحة التفاصيل والجداول ──
            TableLayoutPanel pnlDetails = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 0)
            };
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f)); // الخزائن
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f)); // العملاء
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f)); // الموردين
            mainLayout.Controls.Add(pnlDetails, 0, 2);

            pnlDetails.Controls.Add(BuildDetailSection("🏦 أرصدة الخزائن والبنوك الحالية", out dgSafes, new[] {
                ("SafeName", "اسم الحساب/الخزنة", 120),
                ("SafeType", "النوع", 80),
                ("Balance", "الرصيد الحالي", 90)
            }), 0, 0);

            pnlDetails.Controls.Add(BuildDetailSection("👥 كبار العملاء (مدينون)", out dgTopClients, new[] {
                ("ClientName", "اسم العميل", 120),
                ("Phone", "الهاتف", 90),
                ("Balance", "المبلغ المستحق", 90)
            }), 1, 0);

            pnlDetails.Controls.Add(BuildDetailSection("🏢 كبار الموردين (دائنون)", out dgTopSuppliers, new[] {
                ("SupplierName", "اسم المورد", 120),
                ("Phone", "الهاتف", 90),
                ("Balance", "المطلوب سداده", 90)
            }), 2, 0);
        }

        private void InitPLTab()
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tabIncomeStatement.Controls.Add(layout);

            // لوحة الفلاتر
            Panel pnlFilter = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(8) };
            pnlFilter.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlFilter);
            layout.Controls.Add(pnlFilter, 0, 0);

            Label lblFrom = new Label { Text = "من تاريخ:", AutoSize = true, Location = new Point(1020, 16), ForeColor = Theme.TextMain };
            dtpPLFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Width = 180, Location = new Point(830, 12), BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            dtpPLFrom.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);

            Label lblTo = new Label { Text = "إلى تاريخ:", AutoSize = true, Location = new Point(760, 16), ForeColor = Theme.TextMain };
            dtpPLTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Width = 180, Location = new Point(570, 12), BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            dtpPLTo.Value = DateTime.Now;

            btnReloadPL = Theme.MakeButton("🔄 تحديث قائمة الدخل", Theme.Primary);
            btnReloadPL.Location = new Point(480, 10);
            btnReloadPL.Width = 170;
            btnReloadPL.Height = 30;
            btnReloadPL.Click += (s, e) => LoadPLData();

            pnlFilter.Controls.AddRange(new Control[] { lblFrom, dtpPLFrom, lblTo, dtpPLTo, btnReloadPL });

            // جدول العرض
            dgPL = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36,
                EnableHeadersVisualStyles = false
            };
            dgPL.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontNormal };
            dgPL.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = Theme.FontBold, Alignment = DataGridViewContentAlignment.MiddleCenter };

            dgPL.Columns.Add("AccountName", "الحساب المحاسبي / البيان");
            dgPL.Columns.Add("Debit", "مدين / مصروف (-)");
            dgPL.Columns.Add("Credit", "دائن / إيراد (+)");
            dgPL.Columns.Add("NetValue", "الرصيد الصافي");

            dgPL.Columns[1].DefaultCellStyle.ForeColor = Theme.Danger;
            dgPL.Columns[2].DefaultCellStyle.ForeColor = Theme.Success;

            layout.Controls.Add(dgPL, 0, 1);
        }

        private void InitBSTab()
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tabBalanceSheet.Controls.Add(layout);

            // لوحة الفلاتر
            Panel pnlFilter = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(8) };
            pnlFilter.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlFilter);
            layout.Controls.Add(pnlFilter, 0, 0);

            Label lblBSDate = new Label { Text = "الميزانية حتى تاريخ:", AutoSize = true, Location = new Point(1000, 16), ForeColor = Theme.TextMain };
            dtpBSDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 140, Location = new Point(840, 12), BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            dtpBSDate.Value = DateTime.Now;

            btnReloadBS = Theme.MakeButton("🔄 تحديث الميزانية العمومية", Theme.Primary);
            btnReloadBS.Location = new Point(640, 10);
            btnReloadBS.Width = 180;
            btnReloadBS.Height = 30;
            btnReloadBS.Click += (s, e) => LoadBSData();

            pnlFilter.Controls.AddRange(new Control[] { lblBSDate, dtpBSDate, btnReloadBS });

            // جدول الميزانية (شكل T-Account المحاسبي الشهير)
            dgBS = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36,
                EnableHeadersVisualStyles = false
            };
            dgBS.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontNormal };
            dgBS.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = Theme.FontBold, Alignment = DataGridViewContentAlignment.MiddleCenter };

            dgBS.Columns.Add("AssetAccount", "الأصول (الجانب المدين)");
            dgBS.Columns.Add("AssetValue", "القيمة (ج)");
            dgBS.Columns.Add("LiabilityAccount", "الخصوم وحقوق الملكية (الجانب الدائن)");
            dgBS.Columns.Add("LiabilityValue", "القيمة (ج)");

            dgBS.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgBS.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            layout.Controls.Add(dgBS, 0, 1);
        }

        private void InitAdjustmentsTab()
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // توجيه
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // جدول الإدخال
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // زر الحفظ
            tabAdjustments.Controls.Add(layout);

            var lblNotice = new Label
            {
                Text = "💡 قم بإدخال الأرصدة الافتتاحية الدفترية للأصول الثابتة، رأس المال، القروض والتسويات والضرائب لتتزن ميزانيتك وقائمة الدخل بشكل احترافي.",
                Dock = DockStyle.Fill,
                ForeColor = Theme.TextSub,
                Font = Theme.FontNormal,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(lblNotice, 0, 0);

            dgAdjustments = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36,
                EnableHeadersVisualStyles = false
            };
            dgAdjustments.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontNormal };
            dgAdjustments.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = Theme.FontBold, Alignment = DataGridViewContentAlignment.MiddleCenter };

            dgAdjustments.Columns.Add("AccountKey", "كود الحساب");
            dgAdjustments.Columns.Add("AccountGroup", "تصنيف الحساب");
            dgAdjustments.Columns.Add("AccountName", "اسم الحساب الدفتري");
            dgAdjustments.Columns.Add("AccountValue", "الرصيد الدفتري (ج.م)");

            dgAdjustments.Columns[0].ReadOnly = true;
            dgAdjustments.Columns[0].Visible = false; // كود الحساب مخفي
            dgAdjustments.Columns[1].ReadOnly = true;
            dgAdjustments.Columns[2].ReadOnly = true;

            // السماح بالتعديل فقط على العمود الثالث
            dgAdjustments.Columns[3].ReadOnly = false;
            dgAdjustments.Columns[3].DefaultCellStyle.BackColor = Theme.BgInput;

            layout.Controls.Add(dgAdjustments, 0, 1);

            btnSaveAdjustments = Theme.MakeButton("💾 حفظ الأرصدة والتسويات الدفترية", Theme.Success);
            btnSaveAdjustments.Dock = DockStyle.Fill;
            btnSaveAdjustments.Click += (s, e) => SaveAdjustments();
            layout.Controls.Add(btnSaveAdjustments, 0, 2);
        }

        #endregion

        #region كتل المساعد لتصميم الواجهات

        private Panel CreateMiniCard(string title, string value, Color color, out Label valLabel)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(4), Padding = new Padding(8) };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnl.Controls.Add(lblTitle);

            valLabel = new Label
            {
                Text = value,
                Dock = DockStyle.Fill,
                ForeColor = color,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnl.Controls.Add(valLabel);

            return pnl;
        }

        private Panel CreateRatioLabel(string ratioTitle, out Label valLabel)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Margin = new Padding(2) };

            var lblTitle = new Label
            {
                Text = ratioTitle,
                Dock = DockStyle.Right,
                Width = 130,
                ForeColor = Theme.TextMain,
                Font = Theme.FontNormal,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnl.Controls.Add(lblTitle);

            valLabel = new Label
            {
                Text = "0.00",
                Dock = DockStyle.Fill,
                ForeColor = Theme.Success,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnl.Controls.Add(valLabel);

            return pnl;
        }

        private Panel BuildDetailSection(string title, out DataGridView dg, (string name, string headerText, int width)[] columns)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(4), Padding = new Padding(8) };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            var lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 28, ForeColor = Theme.Accent, Font = Theme.FontBold, TextAlign = ContentAlignment.MiddleLeft };
            pnl.Controls.Add(lblTitle);

            dg = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dg.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontNormal };
            dg.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = Theme.FontBold, Alignment = DataGridViewContentAlignment.MiddleCenter };
            dg.EnableHeadersVisualStyles = false;

            foreach (var col in columns)
            {
                dg.Columns.Add(new DataGridViewTextBoxColumn { Name = col.name, HeaderText = col.headerText, FillWeight = col.width });
            }

            pnl.Controls.Add(dg);
            lblTitle.BringToFront();

            return pnl;
        }

        #endregion

        #region منطق معالجة البيانات والداتا الحية والتسويات

        private void LoadAdjustments()
        {
            try
            {
                adjustments.Clear();
                // جلب القيم من الـ DB
                var dt = DbHelper.Query("SELECT AccountKey, AccountValue FROM AccountingAdjustments");
                foreach (DataRow r in dt.Rows)
                {
                    adjustments[r["AccountKey"].ToString()] = Convert.ToDecimal(r["AccountValue"]);
                }

                // ملء الجدول
                dgAdjustments.Rows.Clear();
                foreach (var def in adjustmentDefinitions)
                {
                    decimal val = 0m;
                    adjustments.TryGetValue(def.Key, out val);
                    dgAdjustments.Rows.Add(def.Key, def.Group, def.Name, val.ToString("N2"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل تحميل الأرصدة الدفترية: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAdjustments()
        {
            try
            {
                dgAdjustments.EndEdit();
                foreach (DataGridViewRow row in dgAdjustments.Rows)
                {
                    if (row.Cells["AccountKey"].Value == null) continue;
                    string key = row.Cells["AccountKey"].Value.ToString();
                    decimal val = 0m;
                    if (row.Cells["AccountValue"].Value != null)
                    {
                        string sVal = row.Cells["AccountValue"].Value.ToString().Replace("ج.م", "").Replace("ج", "").Trim();
                        decimal.TryParse(sVal, out val);
                    }

                    DbHelper.Execute("UPDATE AccountingAdjustments SET AccountValue = @val WHERE AccountKey = @key",
                        DbHelper.P("@val", val), DbHelper.P("@key", key));
                }

                MessageBox.Show("✅ تم حفظ الأرصدة الدفترية والتسويات بنجاح وتحديث كافة التقارير!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAdjustments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل حفظ التعديلات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private decimal GetAdj(string key)
        {
            decimal val = 0m;
            adjustments.TryGetValue(key, out val);
            return val;
        }

        private void LoadDashboardData()
        {
            try
            {
                // 1. النقدية الكلية
                object cashObj = DbHelper.Scalar("SELECT ISNULL(SUM(Balance), 0) FROM vw_SafeAccountBalances");
                decimal liveCash = cashObj != null ? Convert.ToDecimal(cashObj) : 0m;
                lblTotalCash.Text = $"{liveCash:N2} ج";

                // 2. المخزون
                object purObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(v.CurrentQty * (p.PurchasePrice / COALESCE(NULLIF(p.Unit2Factor * p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), 1.0))), 0) 
                    FROM vw_CurrentStockByWarehouse v 
                    JOIN Products p ON v.ProductID = p.ProductID");
                decimal liveInventory = purObj != null ? Convert.ToDecimal(purObj) : 0m;
                lblInventoryPurchase.Text = $"{liveInventory:N2} ج";

                // 3. العملاء
                object clientObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(CurrentBalance), 0) FROM (
                        SELECT c.OpeningBalance + 
                               ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                               ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) AS CurrentBalance
                        FROM Clients c
                    ) t WHERE CurrentBalance > 0");
                decimal liveClients = clientObj != null ? Convert.ToDecimal(clientObj) : 0m;
                lblClientReceivables.Text = $"{liveClients:N2} ج";

                // 4. الموردون
                object supplierObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(Balance), 0) FROM (
                        SELECT s.OpeningBalance + 
                               ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                               ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) AS Balance
                        FROM Suppliers s
                    ) t WHERE Balance > 0");
                decimal liveSuppliers = supplierObj != null ? Convert.ToDecimal(supplierObj) : 0m;
                lblSupplierPayables.Text = $"{liveSuppliers:N2} ج";

                // 5. رأس المال العامل = الأصول المتداولة - الخصوم المتداولة
                decimal currentAssets = liveCash + liveClients + liveInventory + GetAdj("NotesReceivable") + GetAdj("PrepaidExpenses") + GetAdj("AccruedRevenues") + GetAdj("CustodiesAdvances");
                decimal currentLiabilities = liveSuppliers + GetAdj("NotesPayable") + GetAdj("ShortTermLoans") + GetAdj("AccruedTax") + GetAdj("AccruedInsurance") + GetAdj("AccruedExpenses") + GetAdj("DeferredRevenues");
                decimal workingCapital = currentAssets - currentLiabilities;
                lblWorkingCapital.Text = $"{workingCapital:N2} ج";

                // 6. صافي الأرباح (اليوم/الفترة الحالية)
                DataTable dtPL = GetCalculatedPL(DateTime.Today, DateTime.Now);
                decimal netProfit = 0m;
                if (dtPL.Rows.Count > 0)
                {
                    netProfit = Convert.ToDecimal(dtPL.Rows[dtPL.Rows.Count - 1]["NetValue"]);
                }
                lblNetProfitDashboard.Text = $"{netProfit:N2} ج";
                lblNetProfitDashboard.ForeColor = netProfit >= 0 ? Theme.Success : Theme.Danger;

                // ── حساب المؤشرات والنسب المالية ──
                // نسبة التداول
                decimal currentRatio = currentLiabilities > 0 ? (currentAssets / currentLiabilities) : 0m;
                lblCurrentRatio.Text = currentRatio.ToString("N2");

                // نسبة السيولة السريعة
                decimal quickRatio = currentLiabilities > 0 ? ((currentAssets - liveInventory) / currentLiabilities) : 0m;
                lblQuickRatio.Text = quickRatio.ToString("N2");

                // دوران المخزون (COGS السنوي المقدر / المخزون الحالي)
                DataTable dtYearPL = GetCalculatedPL(new DateTime(DateTime.Now.Year, 1, 1), DateTime.Now);
                decimal yearCOGS = 0m;
                if (dtYearPL.Rows.Count > 0)
                {
                    foreach (DataRow r in dtYearPL.Rows)
                    {
                        if (r["AccountName"].ToString().Contains("صافي تكلفة المبيعات"))
                        {
                            yearCOGS = Convert.ToDecimal(r["NetValue"]);
                            break;
                        }
                    }
                }
                decimal invTurnover = liveInventory > 0 ? (yearCOGS / liveInventory) : 0m;
                lblInvTurnover.Text = invTurnover.ToString("N2");

                // دوران العملاء (المبيعات السنوية / متوسط الذمم)
                decimal yearSales = 0m;
                if (dtYearPL.Rows.Count > 0)
                {
                    foreach (DataRow r in dtYearPL.Rows)
                    {
                        if (r["AccountName"].ToString().Contains("صافي المبيعات"))
                        {
                            yearSales = Convert.ToDecimal(r["NetValue"]);
                            break;
                        }
                    }
                }
                decimal clientTurnover = liveClients > 0 ? (yearSales / liveClients) : 0m;
                lblClientTurnover.Text = clientTurnover.ToString("N2");

                // دوران الموردين
                decimal supplierTurnover = liveSuppliers > 0 ? (yearCOGS / liveSuppliers) : 0m;
                lblSupplierTurnover.Text = supplierTurnover.ToString("N2");

                // ── شحن الجداول التفصيلية ──
                var dtSafes = DbHelper.Query(@"
                    SELECT 
                        sa.AccountName AS SafeName,
                        CASE sa.AccountType 
                            WHEN 'Cash' THEN N'خزينة نقدية' 
                            WHEN 'Bank' THEN N'حساب بنكي' 
                            ELSE N'شبكة/فيزا' END AS SafeType,
                        sa.Balance
                    FROM vw_SafeAccountBalances sa
                    ORDER BY sa.AccountName");
                dgSafes.Rows.Clear();
                foreach (DataRow r in dtSafes.Rows)
                {
                    dgSafes.Rows.Add(r["SafeName"], r["SafeType"], Convert.ToDecimal(r["Balance"]).ToString("N2") + " ج");
                }

                var dtClients = DbHelper.Query(@"
                    SELECT TOP 10
                        c.ClientName,
                        c.Phone,
                        (c.OpeningBalance + 
                         ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                         ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0)) AS Balance
                    FROM Clients c
                    WHERE (c.OpeningBalance + 
                           ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                           ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0)) > 0
                    ORDER BY Balance DESC");
                dgTopClients.Rows.Clear();
                foreach (DataRow r in dtClients.Rows)
                {
                    dgTopClients.Rows.Add(r["ClientName"], r["Phone"], Convert.ToDecimal(r["Balance"]).ToString("N2") + " ج");
                }

                var dtSuppliers = DbHelper.Query(@"
                    SELECT TOP 10
                        s.SupplierName,
                        s.Phone,
                        (s.OpeningBalance + 
                         ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                         ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0)) AS Balance
                    FROM Suppliers s
                    WHERE (s.OpeningBalance + 
                           ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                           ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0)) > 0
                    ORDER BY Balance DESC");
                dgTopSuppliers.Rows.Clear();
                foreach (DataRow r in dtSuppliers.Rows)
                {
                    dgTopSuppliers.Rows.Add(r["SupplierName"], r["Phone"], Convert.ToDecimal(r["Balance"]).ToString("N2") + " ج");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل تحميل بيانات الموقف المالي: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable GetCalculatedPL(DateTime f, DateTime t)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("AccountName");
            dt.Columns.Add("Debit", typeof(decimal));
            dt.Columns.Add("Credit", typeof(decimal));
            dt.Columns.Add("NetValue", typeof(decimal));

            // المبيعات والمردودات والخصومات
            object grossSalesObj = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount), 0) FROM Sales WHERE (COL_LENGTH('Sales', 'IsPosted') IS NULL OR ISNULL(IsPosted, 1) = 1) AND CAST(SaleDate AS DATE) BETWEEN @f AND @t", DbHelper.P("@f", f.Date), DbHelper.P("@t", t.Date));
            decimal grossSales = grossSalesObj != null ? Convert.ToDecimal(grossSalesObj) : 0m;

            object returnsObj = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount), 0) FROM SalesReturns WHERE CAST(ReturnDate AS DATE) BETWEEN @f AND @t", DbHelper.P("@f", f.Date), DbHelper.P("@t", t.Date));
            decimal returns = returnsObj != null ? Convert.ToDecimal(returnsObj) : 0m;

            object discountsObj = DbHelper.Scalar("SELECT ISNULL(SUM(DiscountAmount), 0) FROM Sales WHERE (COL_LENGTH('Sales', 'IsPosted') IS NULL OR ISNULL(IsPosted, 1) = 1) AND CAST(SaleDate AS DATE) BETWEEN @f AND @t", DbHelper.P("@f", f.Date), DbHelper.P("@t", t.Date));
            decimal discounts = discountsObj != null ? Convert.ToDecimal(discountsObj) : 0m;

            decimal netSales = grossSales - returns - discounts;

            // تكلفة المبيعات والمردودات
            object grossCOGSObj = DbHelper.Scalar(@"
                SELECT ISNULL(SUM(si.Quantity * ISNULL(si.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))), 0) 
                FROM SaleItems si 
                JOIN Sales s ON si.SaleID = s.SaleID 
                JOIN Products p ON si.ProductID = p.ProductID 
                WHERE (COL_LENGTH('Sales', 'IsPosted') IS NULL OR ISNULL(s.IsPosted, 1) = 1) AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t", DbHelper.P("@f", f.Date), DbHelper.P("@t", t.Date));
            decimal grossCOGS = grossCOGSObj != null ? Convert.ToDecimal(grossCOGSObj) : 0m;

            object returnsCOGSObj = DbHelper.Scalar(@"
                SELECT ISNULL(SUM(ri.Quantity * ISNULL(ri.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))), 0) 
                FROM ReturnItems ri 
                JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                JOIN Products p ON ri.ProductID = p.ProductID 
                WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t", DbHelper.P("@f", f.Date), DbHelper.P("@t", t.Date));
            decimal returnsCOGS = returnsCOGSObj != null ? Convert.ToDecimal(returnsCOGSObj) : 0m;

            decimal netCOGS = grossCOGS - returnsCOGS;
            decimal grossProfit = netSales - netCOGS;

            // إيرادات تشغيلية أخرى
            object otherOpRevObj = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn), 0) FROM CashBox WHERE TransType = 'OtherIncome' AND CAST(TransDate AS DATE) BETWEEN @f AND @t", DbHelper.P("@f", f.Date), DbHelper.P("@t", t.Date));
            decimal otherOpRevenues = otherOpRevObj != null ? Convert.ToDecimal(otherOpRevObj) : 0m;

            // إدراج صفوف قائمة الدخل التشغيلية
            dt.Rows.Add("المبيعات الكلية", 0, grossSales, grossSales);
            dt.Rows.Add("مردودات المبيعات (-)", returns, 0, -returns);
            dt.Rows.Add("خصومات المبيعات (-)", discounts, 0, -discounts);
            dt.Rows.Add("👉 صافي المبيعات", 0, netSales, netSales);
            dt.Rows.Add("تكلفة المبيعات (COGS)", grossCOGS, 0, -grossCOGS);
            dt.Rows.Add("تكلفة المردودات المسترجعة (+)", 0, returnsCOGS, returnsCOGS);
            dt.Rows.Add("👉 صافي تكلفة البضاعة المباعة", netCOGS, 0, -netCOGS);
            dt.Rows.Add("📊 مجمل الربح (الخسارة) التجاري", 0, grossProfit, grossProfit);
            dt.Rows.Add("إيرادات تشغيلية أخرى", 0, otherOpRevenues, otherOpRevenues);

            var dtExps = DbHelper.Query(@"
                SELECT ISNULL(e.ExpenseType, N'مصروفات متنوعة') AS TypeName, SUM(e.Amount) AS Total
                FROM Expenses e
                WHERE CAST(e.ExpenseDate AS DATE) BETWEEN @f AND @t
                GROUP BY e.ExpenseType", DbHelper.P("@f", f.Date), DbHelper.P("@t", t.Date));

            decimal totalOperatingExpenses = 0m;
            foreach (DataRow r in dtExps.Rows)
            {
                string expName = r["TypeName"].ToString();
                decimal amt = Convert.ToDecimal(r["Total"]);
                dt.Rows.Add($"   🔻 مصروف تشغيلي: {expName}", amt, 0, -amt);
                totalOperatingExpenses += amt;
            }

            decimal operatingProfit = grossProfit + otherOpRevenues - totalOperatingExpenses;
            dt.Rows.Add("⚖️ الربح (الخسارة) التشغيلي", 0, operatingProfit, operatingProfit);

            // الإيرادات الأخرى (غير تشغيلية) من التسويات
            decimal gainAssetSale = GetAdj("GainOnAssetSale");
            decimal interestEarned = GetAdj("InterestEarned");
            decimal fxGain = GetAdj("FXGain");
            decimal otherRevenues = GetAdj("OtherRevenues");
            decimal totalOtherRevenues = gainAssetSale + interestEarned + fxGain + otherRevenues;

            dt.Rows.Add("   ➕ أرباح بيع أصول", 0, gainAssetSale, gainAssetSale);
            dt.Rows.Add("   ➕ فوائد دائنة محصلة", 0, interestEarned, interestEarned);
            dt.Rows.Add("   ➕ أرباح فروق العملة", 0, fxGain, fxGain);
            dt.Rows.Add("   ➕ إيرادات غير تشغيلية أخرى", 0, otherRevenues, otherRevenues);

            // المصروفات الأخرى (غير تشغيلية) من التسويات
            decimal interestPaid = GetAdj("InterestPaid");
            decimal fxLoss = GetAdj("FXLoss");
            decimal lossAssetSale = GetAdj("LossOnAssetSale");
            decimal finesPenalties = GetAdj("FinesPenalties");
            decimal otherExpenses = GetAdj("OtherExpenses");
            decimal totalOtherExpenses = interestPaid + fxLoss + lossAssetSale + finesPenalties + otherExpenses;

            dt.Rows.Add("   ➖ فوائد مدينة مدفوعة", interestPaid, 0, -interestPaid);
            dt.Rows.Add("   ➖ خسائر فروق العملة", fxLoss, 0, -fxLoss);
            dt.Rows.Add("   ➖ خسائر بيع أصول", lossAssetSale, 0, -lossAssetSale);
            dt.Rows.Add("   ➖ غرامات وجزاءات حكومية", finesPenalties, 0, -finesPenalties);
            dt.Rows.Add("   ➖ مصروفات غير تشغيلية أخرى", otherExpenses, 0, -otherExpenses);

            // صافي الربح قبل الضريبة
            decimal profitBeforeTax = operatingProfit + totalOtherRevenues - totalOtherExpenses;
            dt.Rows.Add("⚖️ صافي الربح (الخسارة) قبل الضريبة", 0, profitBeforeTax, profitBeforeTax);

            // ضريبة الدخل
            decimal incomeTax = GetAdj("IncomeTax");
            dt.Rows.Add("   ➖ ضريبة الدخل المستقطعة", incomeTax, 0, -incomeTax);

            // صافي الربح النهائي بعد الضريبة
            decimal netProfitAfter = profitBeforeTax - incomeTax;
            dt.Rows.Add("🏆 صافي الربح (الخسارة) النهائي بعد الضريبة", 0, netProfitAfter, netProfitAfter);

            return dt;
        }

        private void LoadPLData()
        {
            try
            {
                DataTable dt = GetCalculatedPL(dtpPLFrom.Value, dtpPLTo.Value);
                dgPL.Rows.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    string name = r["AccountName"].ToString();
                    decimal deb = Convert.ToDecimal(r["Debit"]);
                    decimal cred = Convert.ToDecimal(r["Credit"]);
                    decimal net = Convert.ToDecimal(r["NetValue"]);

                    int idx = dgPL.Rows.Add(
                        name,
                        deb == 0 ? "" : deb.ToString("N2") + " ج",
                        cred == 0 ? "" : cred.ToString("N2") + " ج",
                        net.ToString("N2") + " ج"
                    );

                    // تنسيق صفوف التوتال
                    if (name.StartsWith("👉") || name.StartsWith("⚖️") || name.StartsWith("📊") || name.StartsWith("🏆"))
                    {
                        dgPL.Rows[idx].DefaultCellStyle.Font = new Font(Theme.FontBold.FontFamily, 9.5f, FontStyle.Bold);
                        if (name.StartsWith("🏆"))
                        {
                            dgPL.Rows[idx].DefaultCellStyle.BackColor = Theme.Primary;
                            dgPL.Rows[idx].DefaultCellStyle.ForeColor = Color.White;
                        }
                        else
                        {
                            dgPL.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(235, 243, 250);
                            dgPL.Rows[idx].DefaultCellStyle.ForeColor = Theme.Primary;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل تحميل قائمة الدخل: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadBSData()
        {
            try
            {
                DateTime toDate = dtpBSDate.Value;

                // 1. حساب الحسابات التشغيلية الحية
                object cashObj = DbHelper.Scalar("SELECT ISNULL(SUM(Balance), 0) FROM vw_SafeAccountBalances");
                decimal liveCash = cashObj != null ? Convert.ToDecimal(cashObj) : 0m;

                object purObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(v.CurrentQty * (p.PurchasePrice / COALESCE(NULLIF(p.Unit2Factor * p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), 1.0))), 0) 
                    FROM vw_CurrentStockByWarehouse v 
                    JOIN Products p ON v.ProductID = p.ProductID");
                decimal liveInventory = purObj != null ? Convert.ToDecimal(purObj) : 0m;

                object clientObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(CurrentBalance), 0) FROM (
                        SELECT c.OpeningBalance + 
                               ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                               ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) AS CurrentBalance
                        FROM Clients c
                    ) t WHERE CurrentBalance > 0");
                decimal liveClients = clientObj != null ? Convert.ToDecimal(clientObj) : 0m;

                object supplierObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(Balance), 0) FROM (
                        SELECT s.OpeningBalance + 
                               ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                               ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) AS Balance
                        FROM Suppliers s
                    ) t WHERE Balance > 0");
                decimal liveSuppliers = supplierObj != null ? Convert.ToDecimal(supplierObj) : 0m;

                // حساب صافي ربح الفترة الحالية
                DataTable dtPL = GetCalculatedPL(new DateTime(toDate.Year, 1, 1), toDate);
                decimal netPeriodProfit = 0m;
                if (dtPL.Rows.Count > 0)
                {
                    netPeriodProfit = Convert.ToDecimal(dtPL.Rows[dtPL.Rows.Count - 1]["NetValue"]);
                }

                // بناء كتل الأصول
                List<KeyValuePair<string, decimal>> assets = new List<KeyValuePair<string, decimal>>
                {
                    new KeyValuePair<string, decimal>("🟢 الأصول المتداولة", 0m),
                    new KeyValuePair<string, decimal>("   النقدي بالخزائن والبنوك", liveCash),
                    new KeyValuePair<string, decimal>("   أرصدة العملاء (الذمم)", liveClients),
                    new KeyValuePair<string, decimal>("   أوراق القبض", GetAdj("NotesReceivable")),
                    new KeyValuePair<string, decimal>("   مخزون بضاعة آخر المدة", liveInventory),
                    new KeyValuePair<string, decimal>("   المصروفات المقدمة", GetAdj("PrepaidExpenses")),
                    new KeyValuePair<string, decimal>("   الإيرادات المستحقة", GetAdj("AccruedRevenues")),
                    new KeyValuePair<string, decimal>("   العهد والسلف", GetAdj("CustodiesAdvances"))
                };
                decimal totalCurrentAssets = liveCash + liveClients + GetAdj("NotesReceivable") + liveInventory + GetAdj("PrepaidExpenses") + GetAdj("AccruedRevenues") + GetAdj("CustodiesAdvances");
                assets.Add(new KeyValuePair<string, decimal>("👉 إجمالي الأصول المتداولة", totalCurrentAssets));

                assets.Add(new KeyValuePair<string, decimal>("🟢 الأصول غير المتداولة (الثابتة)", 0m));
                assets.Add(new KeyValuePair<string, decimal>("   الأراضي", GetAdj("Land")));
                assets.Add(new KeyValuePair<string, decimal>("   المباني والإنشاءات", GetAdj("Buildings")));
                assets.Add(new KeyValuePair<string, decimal>("   الآلات والمعدات", GetAdj("Machinery")));
                assets.Add(new KeyValuePair<string, decimal>("   السيارات ووسائل النقل", GetAdj("Vehicles")));
                assets.Add(new KeyValuePair<string, decimal>("   الأثاث والتجهيزات", GetAdj("Furniture")));
                assets.Add(new KeyValuePair<string, decimal>("   أجهزة الحاسب الآلي والشبكات", GetAdj("Computers")));
                assets.Add(new KeyValuePair<string, decimal>("   الاستثمارات طويلة الأجل", GetAdj("Investments")));
                assets.Add(new KeyValuePair<string, decimal>("   الأصول غير الملموسة", GetAdj("Intangibles")));
                assets.Add(new KeyValuePair<string, decimal>("   مجمع الإهلاك للاستقطاع (-)", GetAdj("AccumulatedDepreciation")));

                decimal totalFixedAssets = GetAdj("Land") + GetAdj("Buildings") + GetAdj("Machinery") + GetAdj("Vehicles") + GetAdj("Furniture") + GetAdj("Computers") + GetAdj("Investments") + GetAdj("Intangibles") - GetAdj("AccumulatedDepreciation");
                assets.Add(new KeyValuePair<string, decimal>("👉 إجمالي الأصول غير المتداولة", totalFixedAssets));

                decimal totalAssets = totalCurrentAssets + totalFixedAssets;
                assets.Add(new KeyValuePair<string, decimal>("🏆 إجمالي الأصول الكلية", totalAssets));

                // بناء كتل الخصوم وحقوق الملكية
                List<KeyValuePair<string, decimal>> liabilities = new List<KeyValuePair<string, decimal>>
                {
                    new KeyValuePair<string, decimal>("🔴 الخصوم المتداولة", 0m),
                    new KeyValuePair<string, decimal>("   أرصدة الموردين (الحسابات الدائنة)", liveSuppliers),
                    new KeyValuePair<string, decimal>("   أوراق الدفع", GetAdj("NotesPayable")),
                    new KeyValuePair<string, decimal>("   القروض قصيرة الأجل", GetAdj("ShortTermLoans")),
                    new KeyValuePair<string, decimal>("   الضرائب والجمارك المستحقة", GetAdj("AccruedTax")),
                    new KeyValuePair<string, decimal>("   التأمينات والالتزامات المستحقة", GetAdj("AccruedInsurance")),
                    new KeyValuePair<string, decimal>("   المصروفات المستحقة", GetAdj("AccruedExpenses")),
                    new KeyValuePair<string, decimal>("   الإيرادات المقدمة (المؤجلة)", GetAdj("DeferredRevenues"))
                };
                decimal totalCurrentLiabilities = liveSuppliers + GetAdj("NotesPayable") + GetAdj("ShortTermLoans") + GetAdj("AccruedTax") + GetAdj("AccruedInsurance") + GetAdj("AccruedExpenses") + GetAdj("DeferredRevenues");
                liabilities.Add(new KeyValuePair<string, decimal>("👉 إجمالي الخصوم المتداولة", totalCurrentLiabilities));

                liabilities.Add(new KeyValuePair<string, decimal>("🔴 الخصوم غير المتداولة (طويلة الأجل)", 0m));
                liabilities.Add(new KeyValuePair<string, decimal>("   قروض بنكية طويلة الأجل", GetAdj("LongTermLoans")));
                liabilities.Add(new KeyValuePair<string, decimal>("   التزامات طويلة الأجل أخرى", GetAdj("LongTermLiabilities")));
                decimal totalLongTermLiabilities = GetAdj("LongTermLoans") + GetAdj("LongTermLiabilities");
                liabilities.Add(new KeyValuePair<string, decimal>("👉 إجمالي الخصوم غير المتداولة", totalLongTermLiabilities));

                liabilities.Add(new KeyValuePair<string, decimal>("🔵 حقوق الملكية (رأس المال والاحتياطيات)", 0m));
                liabilities.Add(new KeyValuePair<string, decimal>("   رأس المال الافتتاحي للشركاء", GetAdj("Capital")));
                liabilities.Add(new KeyValuePair<string, decimal>("   الاحتياطي القانوني", GetAdj("LegalReserve")));
                liabilities.Add(new KeyValuePair<string, decimal>("   الاحتياطي العام للمكان", GetAdj("GeneralReserve")));
                liabilities.Add(new KeyValuePair<string, decimal>("   الأرباح المحتجزة / المدورة", GetAdj("RetainedEarnings")));
                liabilities.Add(new KeyValuePair<string, decimal>("   صافي أرباح (خسائر) الفترة الحالية", netPeriodProfit));
                liabilities.Add(new KeyValuePair<string, decimal>("   المسحوبات الشخصية للشركاء (-)", GetAdj("Drawings")));

                decimal totalEquity = GetAdj("Capital") + GetAdj("LegalReserve") + GetAdj("GeneralReserve") + GetAdj("RetainedEarnings") + netPeriodProfit - GetAdj("Drawings");
                liabilities.Add(new KeyValuePair<string, decimal>("👉 إجمالي حقوق الملكية الكلية", totalEquity));

                decimal totalLiabilitiesAndEquity = totalCurrentLiabilities + totalLongTermLiabilities + totalEquity;
                liabilities.Add(new KeyValuePair<string, decimal>("🏆 إجمالي الخصوم وحقوق الملكية", totalLiabilitiesAndEquity));

                // تعبئة البيانات جنباً إلى جنب في الـ DataGridView بالتزامن
                dgBS.Rows.Clear();
                int maxRows = Math.Max(assets.Count, liabilities.Count);
                for (int i = 0; i < maxRows; i++)
                {
                    string assetAcc = "";
                    string assetVal = "";
                    string liabAcc = "";
                    string liabVal = "";

                    if (i < assets.Count)
                    {
                        assetAcc = assets[i].Key;
                        assetVal = assets[i].Value == 0m && (assets[i].Key.StartsWith("🟢") || assets[i].Key.Contains("الثابتة")) ? "" : assets[i].Value.ToString("N2") + " ج";
                    }

                    if (i < liabilities.Count)
                    {
                        liabAcc = liabilities[i].Key;
                        liabVal = liabilities[i].Value == 0m && (liabilities[i].Key.StartsWith("🔴") || liabilities[i].Key.Contains("حقوق الملكية")) ? "" : liabilities[i].Value.ToString("N2") + " ج";
                    }

                    int idx = dgBS.Rows.Add(assetAcc, assetVal, liabAcc, liabVal);

                    // تنسيق صفوف التوتال
                    if (assetAcc.StartsWith("🟢") || assetAcc.StartsWith("👉") || assetAcc.StartsWith("🏆") ||
                        liabAcc.StartsWith("🔴") || liabAcc.StartsWith("🔵") || liabAcc.StartsWith("👉") || liabAcc.StartsWith("🏆"))
                    {
                        dgBS.Rows[idx].DefaultCellStyle.Font = new Font(Theme.FontBold.FontFamily, 9.5f, FontStyle.Bold);
                        if (assetAcc.StartsWith("🏆") || liabAcc.StartsWith("🏆"))
                        {
                            dgBS.Rows[idx].DefaultCellStyle.BackColor = Theme.Primary;
                            dgBS.Rows[idx].DefaultCellStyle.ForeColor = Color.White;
                        }
                        else
                        {
                            dgBS.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(235, 243, 250);
                            dgBS.Rows[idx].DefaultCellStyle.ForeColor = Theme.Primary;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل تحميل الميزانية العمومية: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrintFinancialPosition_Click(object sender, EventArgs e)
        {
            try
            {
                // ضمان تحديث كافة الأرقام الحية قبل الطباعة
                LoadDashboardData();

                var pd = new System.Drawing.Printing.PrintDocument();
                pd.PrintController = new System.Drawing.Printing.StandardPrintController();
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    var titleFont        = new Font("Arial", 15, FontStyle.Bold);
                    var subTitleFont     = new Font("Arial", 9.5f, FontStyle.Bold);
                    var sectionFont      = new Font("Arial", 11, FontStyle.Bold);
                    var cardTitleFont    = new Font("Arial", 9, FontStyle.Bold);
                    var cardValFont      = new Font("Arial", 10.5f, FontStyle.Bold);
                    var headerFont       = new Font("Arial", 9.5f, FontStyle.Bold);
                    var dataFont         = new Font("Arial", 9f, FontStyle.Regular);
                    var boldDataFont     = new Font("Arial", 9f, FontStyle.Bold);

                    var borderPen       = new Pen(Color.FromArgb(15, 45, 90), 1.5f);
                    var gridPen         = new Pen(Color.FromArgb(200, 210, 225), 1f);

                    int y = 25;
                    int leftMargin = 20;
                    int rightMargin = 805;
                    int tableWidth = rightMargin - leftMargin;

                    var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    var sfRight  = new StringFormat { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                    // ── 1. Title Header Block ──
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 244, 252)), leftMargin, y, tableWidth, 55);
                    g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 55);

                    string shopName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة والتجارة العامة";
                    g.DrawString($"تقرير الموقف والمركز المالي الشامل للمكان - {shopName}", titleFont, Brushes.DarkBlue, new RectangleF(leftMargin, y + 4, tableWidth, 26), sfCenter);
                    g.DrawString($"تحليل السيولة النقدية والمخزونية والحقوق والالتزامات | تاريخ التقرير: {DateTime.Now:yyyy/MM/dd  hh:mm tt}", subTitleFont, Brushes.DimGray, new RectangleF(leftMargin, y + 32, tableWidth, 18), sfCenter);
                    y += 68;

                    // ── 2. Key Financial Cards Grid (2 rows of 3 columns) ──
                    g.DrawString("📌 أولاً: ملخص السيولة والموجودات والمطلوبات النقدية والمخزونية", sectionFont, Brushes.DarkBlue, new RectangleF(leftMargin, y, tableWidth, 22), sfRight);
                    y += 26;

                    int cardWidth = (tableWidth - 10) / 3;
                    int cardHeight = 44;

                    string[,] cards = {
                        { "💵 إجمالي السيولة بالخزن", lblTotalCash.Text, "0" },
                        { "📦 قيمة المخزون الحالي (شراء)", lblInventoryPurchase.Text, "1" },
                        { "👥 مديونيات ومستحقات العملاء", lblClientReceivables.Text, "2" },
                        { "🏢 مطلوبات وديون الموردين", lblSupplierPayables.Text, "3" },
                        { "⚖️ رأس المال العامل الصافي", lblWorkingCapital.Text, "4" },
                        { "📈 صافي أرباح الفترة الحالية", lblNetProfitDashboard.Text, "5" }
                    };

                    for (int i = 0; i < 6; i++)
                    {
                        int row = i / 3;
                        int col = i % 3;
                        int cx = leftMargin + (2 - col) * (cardWidth + 5);
                        int cy = y + row * (cardHeight + 6);

                        Color bgCol = (i == 0 || i == 4) ? Color.FromArgb(235, 245, 255) :
                                      (i == 1 || i == 5) ? Color.FromArgb(235, 250, 240) :
                                      (i == 2) ? Color.FromArgb(240, 248, 255) : Color.FromArgb(255, 240, 240);

                        g.FillRectangle(new SolidBrush(bgCol), cx, cy, cardWidth, cardHeight);
                        g.DrawRectangle(Pens.SteelBlue, cx, cy, cardWidth, cardHeight);

                        g.DrawString(cards[i, 0], cardTitleFont, Brushes.DarkSlateGray, new RectangleF(cx, cy + 3, cardWidth, 16), sfCenter);
                        g.DrawString(cards[i, 1], cardValFont, Brushes.DarkBlue, new RectangleF(cx, cy + 20, cardWidth, 20), sfCenter);
                    }
                    y += (cardHeight * 2) + 18;

                    // ── 3. Ratios & Performance Indicators ──
                    g.DrawString("⚡ ثانياً: مؤشرات الكفاءة والسيولة المالية للمكان", sectionFont, Brushes.DarkBlue, new RectangleF(leftMargin, y, tableWidth, 22), sfRight);
                    y += 24;

                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 249, 252)), leftMargin, y, tableWidth, 26);
                    g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 26);

                    int ratioColW = tableWidth / 5;
                    string[] ratioNames = { "نسبة التداول", "نسبة السيولة السريعة", "دوران المخزون", "دوران العملاء", "دوران الموردين" };
                    string[] ratioVals  = { lblCurrentRatio.Text, lblQuickRatio.Text, lblInvTurnover.Text, lblClientTurnover.Text, lblSupplierTurnover.Text };

                    for (int r = 0; r < 5; r++)
                    {
                        int rx = leftMargin + (4 - r) * ratioColW;
                        g.DrawString($"{ratioNames[r]}: {ratioVals[r]}", subTitleFont, Brushes.DarkSlateGray, new RectangleF(rx, y, ratioColW, 26), sfCenter);
                        if (r > 0) g.DrawLine(gridPen, rx, y, rx, y + 26);
                    }
                    y += 34;

                    // ── 4. Safe Accounts Liquidity Table ──
                    g.DrawString("🏛️ ثالثاً: بيان تفصيلي بأرصدة الخزن والمحفظة النقدية والمصرفية", sectionFont, Brushes.DarkBlue, new RectangleF(leftMargin, y, tableWidth, 22), sfRight);
                    y += 24;

                    int[] safeCols = { leftMargin, leftMargin + 250, leftMargin + 450, rightMargin };
                    string[] safeHeaders = { "اسم الخزينة / الحساب", "نوع الحساب", "الرصيد الحقيقي المتاح" };

                    g.FillRectangle(new SolidBrush(Color.FromArgb(15, 45, 90)), leftMargin, y, tableWidth, 24);
                    g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 24);

                    for (int k = 0; k < safeHeaders.Length; k++)
                    {
                        float cx = safeCols[k];
                        float cw = safeCols[k + 1] - safeCols[k];
                        g.DrawString(safeHeaders[k], headerFont, Brushes.White, new RectangleF(cx, y, cw, 24), sfCenter);
                        if (k > 0) g.DrawLine(Pens.White, safeCols[k], y, safeCols[k], y + 24);
                    }
                    y += 24;

                    foreach (DataGridViewRow row in dgSafes.Rows)
                    {
                        if (row.IsNewRow) continue;
                        string sName = row.Cells[0].Value?.ToString() ?? "";
                        string sType = row.Cells[1].Value?.ToString() ?? "";
                        string sBal  = row.Cells[2].Value?.ToString() ?? "";

                        g.FillRectangle(new SolidBrush(Color.FromArgb(250, 252, 255)), leftMargin, y, tableWidth, 22);
                        g.DrawRectangle(gridPen, leftMargin, y, tableWidth, 22);

                        g.DrawString(sName, boldDataFont, Brushes.DarkSlateGray, new RectangleF(safeCols[0] + 8, y, safeCols[1] - safeCols[0] - 16, 22), sfRight);
                        g.DrawString(sType, dataFont, Brushes.Black, new RectangleF(safeCols[1], y, safeCols[2] - safeCols[1], 22), sfCenter);
                        g.DrawString(sBal,  boldDataFont, Brushes.DarkGreen, new RectangleF(safeCols[2], y, safeCols[3] - safeCols[2], 22), sfCenter);

                        y += 22;
                    }
                    y += 18;

                    // ── 5. Top Clients & Suppliers Tables (Side by Side) ──
                    int halfWidth = (tableWidth - 15) / 2;

                    g.DrawString("👥 كبار العملاء المدينين (حقوق المكان)", sectionFont, Brushes.DarkBlue, new RectangleF(rightMargin - halfWidth, y, halfWidth, 20), sfRight);
                    g.DrawString("🏢 كبار الموردين الدائنين (التزامات المكان)", sectionFont, Brushes.DarkBlue, new RectangleF(leftMargin, y, halfWidth, 20), sfRight);
                    y += 22;

                    // Table headers
                    g.FillRectangle(new SolidBrush(Color.FromArgb(25, 80, 140)), rightMargin - halfWidth, y, halfWidth, 22);
                    g.DrawRectangle(borderPen, rightMargin - halfWidth, y, halfWidth, 22);
                    g.DrawString("اسم العميل", headerFont, Brushes.White, new RectangleF(rightMargin - halfWidth + 120, y, halfWidth - 120, 22), sfCenter);
                    g.DrawString("الرصيد المدين", headerFont, Brushes.White, new RectangleF(rightMargin - halfWidth, y, 120, 22), sfCenter);

                    g.FillRectangle(new SolidBrush(Color.FromArgb(140, 40, 25)), leftMargin, y, halfWidth, 22);
                    g.DrawRectangle(borderPen, leftMargin, y, halfWidth, 22);
                    g.DrawString("اسم المورد", headerFont, Brushes.White, new RectangleF(leftMargin + 120, y, halfWidth - 120, 22), sfCenter);
                    g.DrawString("الرصيد الدائن", headerFont, Brushes.White, new RectangleF(leftMargin, y, 120, 22), sfCenter);
                    y += 22;

                    int maxRows = Math.Max(dgTopClients.Rows.Count, dgTopSuppliers.Rows.Count);
                    maxRows = Math.Min(maxRows, 7); // Show top 7

                    for (int r = 0; r < maxRows; r++)
                    {
                        // Client Row
                        if (r < dgTopClients.Rows.Count && !dgTopClients.Rows[r].IsNewRow)
                        {
                            string cName = dgTopClients.Rows[r].Cells[0].Value?.ToString() ?? "";
                            string cBal  = dgTopClients.Rows[r].Cells[2].Value?.ToString() ?? "";
                            g.FillRectangle(new SolidBrush(Color.FromArgb(248, 252, 255)), rightMargin - halfWidth, y, halfWidth, 20);
                            g.DrawRectangle(gridPen, rightMargin - halfWidth, y, halfWidth, 20);
                            g.DrawString(cName, dataFont, Brushes.Black, new RectangleF(rightMargin - halfWidth + 120, y, halfWidth - 130, 20), sfRight);
                            g.DrawString(cBal, boldDataFont, Brushes.DarkBlue, new RectangleF(rightMargin - halfWidth, y, 120, 20), sfCenter);
                        }

                        // Supplier Row
                        if (r < dgTopSuppliers.Rows.Count && !dgTopSuppliers.Rows[r].IsNewRow)
                        {
                            string sName = dgTopSuppliers.Rows[r].Cells[0].Value?.ToString() ?? "";
                            string sBal  = dgTopSuppliers.Rows[r].Cells[2].Value?.ToString() ?? "";
                            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 248, 248)), leftMargin, y, halfWidth, 20);
                            g.DrawRectangle(gridPen, leftMargin, y, halfWidth, 20);
                            g.DrawString(sName, dataFont, Brushes.Black, new RectangleF(leftMargin + 120, y, halfWidth - 130, 20), sfRight);
                            g.DrawString(sBal, boldDataFont, Brushes.DarkRed, new RectangleF(leftMargin, y, 120, 20), sfCenter);
                        }
                        y += 20;
                    }

                    // ── 6. Footer Signatures Block ──
                    y = ev.PageBounds.Height - 75;
                    g.DrawLine(borderPen, leftMargin, y, rightMargin, y);
                    y += 8;

                    g.DrawString("التوقيع والاعتماد المحاسبي: .......................................", subTitleFont, Brushes.DarkSlateGray, new RectangleF(rightMargin - 350, y, 350, 22), sfRight);
                    g.DrawString("اعتماد إدارة المؤسسة: .......................................", subTitleFont, Brushes.DarkSlateGray, new RectangleF(leftMargin, y, 350, 22), sfRight);
                };

                var preview = new PrintPreviewDialog { Document = pd, Width = 1000, Height = 750 };
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل إعداد أو طباعة تقرير الموقف المالي: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Linq;
using ChickenDist.Core;
using ChickenDist.DAL;
using System.Collections.Generic;

namespace ChickenDist.Forms
{
	public class FrmReports : Form
	{
		private TabControl tabReports;

		private DateTimePicker dtpFrom;

		private DateTimePicker dtpTo;

		private ComboBox cboDatePresets;

		private ComboBox cboWarehouse;

		private ComboBox cboPayType;

		private ComboBox cboEmployee;

		private Button btnLoad;

		private Button btnPrint;

		private Button btnWhatsAppReport;

		private Button btnExportExcel;
		private Button btnExportPdf;

		private TextBox txtSearchClient;

		private DataTable _currentDt;
		private string _targetModule = null;
		private int _preFilteredID = 0;
		private string _defaultTabTag = null;

		private static readonly Dictionary<string, Color> ReportTabColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
		{
			// Sales
			{ "DailySalesSummary", Color.FromArgb(16, 185, 129) },     // Emerald Green
			{ "SalesByPeriod", Color.FromArgb(14, 165, 233) },         // Sky Blue
			{ "DetailedSales", Color.FromArgb(99, 102, 241) },         // Indigo
			{ "DetailedSaleItems", Color.FromArgb(20, 184, 166) },     // Teal
			{ "SalesByProduct", Color.FromArgb(139, 92, 246) },        // Violet
			{ "SalesByCategory", Color.FromArgb(217, 119, 6) },        // Amber
			{ "SalesByClient", Color.FromArgb(2, 132, 199) },          // Deep Sky
			{ "SalesByUser", Color.FromArgb(225, 29, 72) },            // Rose
			{ "SalesByPaymentMethod", Color.FromArgb(79, 70, 229) },   // Royal Indigo
			{ "SalesDiscounts", Color.FromArgb(234, 88, 12) },         // Orange
			{ "DetailedReturns", Color.FromArgb(220, 38, 38) },        // Red/Crimson
			{ "SalesProfitability", Color.FromArgb(22, 163, 74) },     // Green
			{ "StagnantProducts", Color.FromArgb(147, 51, 234) },      // Purple

			// Purchases
			{ "DailyPurchasesSummary", Color.FromArgb(13, 148, 136) }, // Teal
			{ "PurchasesByPeriod", Color.FromArgb(37, 99, 235) },      // Blue
			{ "DetailedPurchases", Color.FromArgb(67, 56, 202) },      // Dark Indigo
			{ "DetailedPurchaseItems", Color.FromArgb(8, 145, 178) },  // Cyan
			{ "PurchasesBySupplier", Color.FromArgb(124, 58, 237) },   // Purple
			{ "PurchasesByProduct", Color.FromArgb(180, 83, 9) },      // Warm Amber
			{ "PurchasesByCategory", Color.FromArgb(194, 65, 12) },    // Dark Orange
			{ "DetailedPurchaseReturns", Color.FromArgb(185, 28, 28) },// Dark Red
			{ "SupplierPayments", Color.FromArgb(15, 118, 110) },      // Dark Teal
			{ "PurchasePricesTracking", Color.FromArgb(30, 64, 175) }, // Navy Blue
			{ "CreditPurchases", Color.FromArgb(190, 24, 93) },        // Pink-Red

			// Financials & Shifts
			{ "DailyClosing", Color.FromArgb(180, 83, 9) },            // Amber
			{ "ShiftsHistory", Color.FromArgb(5, 150, 105) },          // Green
			{ "ShiftVsCalendarComparison", Color.FromArgb(79, 70, 229)},// Purple
			{ "IncomeStatementAndProfitability", Color.FromArgb(16, 185, 129) }, // Emerald
			{ "FinancialSummary", Color.FromArgb(14, 165, 233) },

			// Clients & Drivers
			{ "SalesByDriver", Color.FromArgb(217, 119, 6) },          // Amber
			{ "ClientBalances", Color.FromArgb(37, 99, 235) },         // Blue
			{ "DebtAging", Color.FromArgb(225, 29, 72) },              // Rose
			{ "ClientProductSales", Color.FromArgb(13, 148, 136) },    // Teal
			{ "Handovers", Color.FromArgb(109, 40, 217) },             // Deep Violet

			// Stores & Inventory
			{ "ProductQtyDetail", Color.FromArgb(14, 165, 233) },      // Sky Blue
			{ "WastageLoss", Color.FromArgb(220, 38, 38) },            // Red
			{ "DetailedInventoryValuation", Color.FromArgb(22, 163, 74) }, // Green
			{ "SupplierItemActivity", Color.FromArgb(124, 58, 237) },  // Violet
			{ "ExpiryReport", Color.FromArgb(234, 88, 12) },           // Orange
			{ "InventoryVariance", Color.FromArgb(219, 39, 119) }      // Magenta
		};

		private static readonly Color[] FallbackTabColors = new Color[]
		{
			Color.FromArgb(14, 165, 233),
			Color.FromArgb(16, 185, 129),
			Color.FromArgb(245, 158, 11),
			Color.FromArgb(139, 92, 246),
			Color.FromArgb(244, 63, 94),
			Color.FromArgb(20, 184, 166),
			Color.FromArgb(234, 88, 12),
			Color.FromArgb(99, 102, 241)
		};

		public string TargetModule => _targetModule;
		public string DefaultTabTag => _defaultTabTag;

		private Label lblReportHeaderTitle;
		private Label lblReportHeaderDesc;

		private static readonly Dictionary<string, (string title, string desc)> ReportDescriptions = new Dictionary<string, (string title, string desc)>(StringComparer.OrdinalIgnoreCase)
		{
			// Sales
			{ "DailySalesSummary", ("📅 تقرير المبيعات اليومية", "عرض ملخص إجمالي لمبيعات اليوم الحالي أو يوم محدد، ومجموع المبيعات النقدية والآجلة والفيزا، مع إجمالي الخصومات وصافي دخل اليومية.") },
			{ "SalesByPeriod", ("📈 تقرير المبيعات خلال فترة", "مقارنة وتحليل حركة المبيعات وتطورها على مدار فترة زمنية محددة (أيام / أسابيع / شهور) مع الرسوم البيانية والإحصائيات.") },
			{ "DetailedSales", ("🧾 سجل فواتير المبيعات", "استعراض ومراجعة كافة فواتير المبيعات الصادرة مع تفاصيل كل فاتورة وإمكانية إعادة الطباعة (A4 / ريسيت) أو الإرسال واتساب للعميل.") },
			{ "DetailedSaleItems", ("📦 تفاصيل سطور وأصناف المبيعات", "حصر تفصيلي لكل صنف تم بيعه داخل الفواتير مع الكميات المباعة وسعر كل حركة ونسبة الخصم والإجمالي.") },
			{ "SalesByProduct", ("📊 مبيعات الأصناف والربحية", "معرفة الأصناف الأكثر مبيعاً والأعلى تحقيقاً للأرباح، وحساب هامش ربح كل صنف ونسبته من إجمالي المبيعات.") },
			{ "SalesByCategory", ("🏢 مبيعات المجموعات والأقسام", "تحليل المبيعات حسب التصنيفات والأقسام لمعرفة أي الأقسام الأكثر رواجاً ونشاطاً في المبيعات.") },
			{ "SalesByClient", ("👥 مبيعات العملاء والمسدد", "كشف مبيعات وسحوبات كل عميل على حدة مع إجمالي المبالغ المسددة والمتبقية كديون ومعدل تكرار الشراء.") },
			{ "SalesByUser", ("👔 مبيعات المستخدمين والكاشير", "تقييم إنتاجية ومبيعات كل مستخدم أو كاشير أو بائع، ومتابعة حركات البيع الصادرة من كل موظف.") },
			{ "SalesByPaymentMethod", ("💳 طرق الدفع والتحصيل", "تفصيل وتوزيع المبيعات حسب طريقة التحصيل (نقدي / فيزا وبطاقات / آجل / دفع مختلط) لمطابقة النقدية والخزائن.") },
			{ "SalesDiscounts", ("🏷️ الخصومات والتخفيضات", "حصر شامل لجميع الخصومات والتخفيضات الممنوحة على الفواتير أو الأصناف لمعرفة التكلفة الإجمالية للخصومات.") },
			{ "DetailedReturns", ("🔄 مرتجعات المبيعات", "متابعة وتحليل فواتير وأصناف مرتجع المبيعات وأسباب الإرجاع وطريقة رد المبالغ للعملاء.") },
			{ "SalesProfitability", ("💰 أرباح وهامش المبيعات", "حساب صافي الأرباح المحققة بعد خصم تكلفة الشراء والخصومات، وتحديد نسبة الربحية الدقيقة للمبيعات.") },
			{ "StagnantProducts", ("💤 الأصناف الراكدة (مشتراة ولم تُباع)", "كشف الأصناف المخزنة التي لم تسجل أي حركة بيع خلال الفترة المحددة للمساعدة في التصفية وتنشيط المبيعات.") },

			// Purchases
			{ "DailyPurchasesSummary", ("📅 تقرير المشتريات اليومية", "ملخص مشتريات اليوم والتوريدات النقدية والآجلة وتكلفة البضاعة المشتراة.") },
			{ "PurchasesByPeriod", ("📈 تقرير المشتريات خلال فترة", "تحليل ومتابعة المشتريات وتطور تكاليف التوريد خلال فترة محددة.") },
			{ "DetailedPurchases", ("🧾 سجل فواتير المشتريات", "استعراض سجل فواتير الشراء وأرقامها والموردين ومراجعة الأسعار وتفاصيل الفاتورة.") },
			{ "DetailedPurchaseItems", ("📦 تفاصيل سطور وأصناف المشتريات", "حصر الأصناف المشتراة مع الكميات الواردة وأسعار الشراء وتكاليف التوريد.") },
			{ "PurchasesBySupplier", ("🤝 مشتريات الموردين والمسدد", "حجم التعاملات والمشتريات لكل مورد مع إجمالي المسدد والمتبقي في الحساب.") },
			{ "PurchasesByProduct", ("📊 مشتريات الأصناف ومتوسط التكلفة", "متابعة كميات شراء كل صنف ومتوسط تكلفة الشراء عبر التوريدات المختلفة.") },
			{ "PurchasesByCategory", ("🏢 مشتريات الأقسام والتصنيفات", "توزيع تكاليف المشتريات حسب الأقسام والتصنيفات المخزنية.") },
			{ "DetailedPurchaseReturns", ("🔄 مرتجعات المشتريات", "حصر البضاعة المسترجعة للموردين واسترداد قيمتها نقداً أو خصماً من الرصيد.") },
			{ "SupplierPayments", ("💵 المدفوعات للموردين والتسويات", "سجل سندات الصرف والتحويلات المالية المسددة للموردين لتسوية الأرصدة.") },
			{ "PurchasePricesTracking", ("📈 أسعار الشراء وتغير الأسعار", "مراقبة تقلبات وتغيرات أسعار شراء الأصناف عبر الزمن لتفادي ارتفاع التكاليف.") },
			{ "CreditPurchases", ("⏳ المشتريات الآجلة والمديونيات", "حصر المشتريات الآجلة ومتابعة مواعيد استحقاق السداد للموردين.") },

			// Financials & Shifts
			{ "DailyClosing", ("📑 تقرير التقفيل اليومي", "مراجعة واعتماد إقفال اليومية ومطابقة النقدية الفعلية مع مبيعات البرنامج.") },
			{ "ShiftsHistory", ("📊 سجل وتقارير الورديات", "استعراض تفاصيل الورديات المغلقة ومبيعات كل كاشير والعجز أو الزيادة في الدرج.") },
			{ "ShiftVsCalendarComparison", ("⚖️ مقارنة الورديات بالأيام التقويمية", "مطابقة مبيعات الورديات مع التاريخ الفعلي لليوم لمنع أي تداخل بين الأيام.") },
			{ "IncomeStatementAndProfitability", ("📊 قائمة الدخل والربحية", "قائمة الدخل الشاملة: المبيعات - تكلفة المبيعات - المصروفات = صافي الربح.") },
			{ "FinancialSummary", ("📊 الملخص المالي العام", "نظرة عامة وشاملة على الموقف المالي وحركة الخزائن والديون والأرباح.") },

			// Clients & Drivers
			{ "SalesByDriver", ("🚚 مبيعات المناديب والسيارات", "حجم مبيعات وتحصيلات كل مندوب وسيارة توزيع وعمولاتهم.") },
			{ "ClientBalances", ("👥 أرصدة ومديونيات العملاء", "كشف كامل بأرصدة وديون جميع العملاء وإمكانية إرسال كشوف الحساب عبر واتساب.") },
			{ "DebtAging", ("⏳ أعمار الديون", "تصنيف مديونيات العملاء حسب الفترة الزمنية (أقل من 30 يوم، 60 يوم، 90+ يوم) لمتابعة التحصيل.") },
			{ "ClientProductSales", ("📊 مسحوبات العملاء من الأصناف", "تحليل الأصناف والكميات التي يسحبها كل عميل بانتظام.") },
			{ "Handovers", ("📋 تسليم وتصفية المناديب", "متابعة حركات تسليم العهد والبضائع المحملة للمناديب وتصفيتها.") },

			// Stores & Inventory
			{ "ProductQtyDetail", ("📦 تفاصيل أرصدة المخازن", "استعراض أرصدة وكميات الأصناف داخل كل مخزن مع مواقع الأرفف وحد الطلب.") },
			{ "WastageLoss", ("🗑️ الهوالك والتالف", "سجل الأصناف التالفة والهالكة وأسباب التلف وتكلفتها الإجمالية.") },
			{ "DetailedInventoryValuation", ("💰 تقييم بضاعة المخزن", "حساب القيمة المالية الإجمالية للمخزون بسعر التكلفة وسعر البيع المتوقع.") },
			{ "SupplierItemActivity", ("📊 حركة أصناف الموردين", "متابعة حركة الأصناف الخاصة بكل مورد وتوريداتها ومبيعاتها.") },
			{ "ExpiryReport", ("⏳ تواريخ الصلاحية", "كشف الأصناف القريبة من انتهاء الصلاحية لتصريفها قبل التلف.") },
			{ "InventoryVariance", ("⚖️ عجز وفروق الجرد", "مقارنة الرصيد الفعلي بعد الجرد مع الرصيد الدفتري وحساب الفروق.") }
		};

		public FrmReports(string targetModule = null, int preFilteredID = 0, string defaultTabTag = null)
		{
			_targetModule = targetModule;
			_preFilteredID = preFilteredID;
			_defaultTabTag = defaultTabTag;
			InitUI();
		}

		private Panel MakeFilterPanel(string labelText, Control inputCtrl, int inputWidth = 140, Color? labelColor = null)
		{
			inputCtrl.Width = inputWidth;
			inputCtrl.Height = 26;
			inputCtrl.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

			var lbl = new Label
			{
				Text = labelText,
				AutoSize = true,
				ForeColor = labelColor ?? Color.FromArgb(255, 220, 110), // Gold/Amber for readability
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				RightToLeft = RightToLeft.Yes
			};

			int lblW = lbl.PreferredWidth;
			int totalW = lblW + inputWidth + 16;

			var pnl = new Panel
			{
				Size = new Size(totalW, 36),
				BackColor = Color.FromArgb(30, 41, 59),
				Margin = new Padding(3, 2, 4, 2),
				Padding = new Padding(4),
				RightToLeft = RightToLeft.Yes
			};
			pnl.Paint += (s, e) =>
			{
				using (var pen = new Pen(Color.FromArgb(51, 65, 85), 1.2f))
				{
					e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
				}
			};

			lbl.Location = new Point(totalW - lblW - 6, 8);
			inputCtrl.Location = new Point(4, 5);

			pnl.Controls.Add(lbl);
			pnl.Controls.Add(inputCtrl);
			return pnl;
		}

		private void SetupDatePresets()
		{
			cboDatePresets.Items.Clear();
			cboDatePresets.Items.AddRange(new object[]
			{
				"📅 مخصص",
				"⚡ اليوم",
				"⚡ أمس",
				"⚡ هذا الأسبوع",
				"⚡ هذا الشهر",
				"⚡ الشهر السابق",
				"⚡ العام الحالي",
				"⚡ كل الفترات"
			});
			cboDatePresets.SelectedIndex = 0;

			cboDatePresets.SelectedIndexChanged += (s, e) =>
			{
				if (cboDatePresets.SelectedIndex <= 0) return;

				DateTime now = DateTime.Now;
				DateTime today = DateTime.Today;

				switch (cboDatePresets.SelectedIndex)
				{
					case 1: // اليوم
						dtpFrom.Value = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);
						dtpTo.Value = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59);
						break;
					case 2: // أمس
						var yday = today.AddDays(-1);
						dtpFrom.Value = new DateTime(yday.Year, yday.Month, yday.Day, 0, 0, 0);
						dtpTo.Value = new DateTime(yday.Year, yday.Month, yday.Day, 23, 59, 59);
						break;
					case 3: // هذا الأسبوع
						int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Saturday) % 7;
						var weekStart = today.AddDays(-diff);
						dtpFrom.Value = new DateTime(weekStart.Year, weekStart.Month, weekStart.Day, 0, 0, 0);
						dtpTo.Value = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59);
						break;
					case 4: // هذا الشهر
						dtpFrom.Value = new DateTime(today.Year, today.Month, 1, 0, 0, 0);
						dtpTo.Value = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59);
						break;
					case 5: // الشهر السابق
						var lastMonth = today.AddMonths(-1);
						var lmStart = new DateTime(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0);
						var lmEnd = new DateTime(today.Year, today.Month, 1, 0, 0, 0).AddSeconds(-1);
						dtpFrom.Value = lmStart;
						dtpTo.Value = lmEnd;
						break;
					case 6: // العام الحالي
						dtpFrom.Value = new DateTime(today.Year, 1, 1, 0, 0, 0);
						dtpTo.Value = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59);
						break;
					case 7: // كل الفترات
						dtpFrom.Value = new DateTime(2020, 1, 1, 0, 0, 0);
						dtpTo.Value = new DateTime(2035, 12, 31, 23, 59, 59);
						break;
				}
			};
		}

		private void LoadPaymentTypes()
		{
			cboPayType.Items.Clear();
			cboPayType.Items.AddRange(new object[]
			{
				"كل طرق الدفع",
				"نقدي (Cash)",
				"آجل (Credit)",
				"فيزا / شبكة (Visa)",
				"تقسيط شرعي"
			});
			cboPayType.SelectedIndex = 0;
			cboPayType.SelectedIndexChanged += (s, e) => ApplyAllFilters();
		}

		private void LoadEmployees()
		{
			try
			{
				DataTable dt = DbHelper.Query("SELECT EmployeeID, Name FROM Employees WHERE IsActive=1 ORDER BY Name");
				cboEmployee.Items.Clear();
				cboEmployee.Items.Add(new ComboItem(0, "كل المناديب والمستخدمين"));
				if (dt != null)
				{
					foreach (DataRow r in dt.Rows)
					{
						cboEmployee.Items.Add(new ComboItem(Convert.ToInt32(r["EmployeeID"]), r["Name"].ToString()));
					}
				}
				cboEmployee.DisplayMember = "Text";
				cboEmployee.SelectedIndex = 0;
				cboEmployee.SelectedIndexChanged += (s, e) => ApplyAllFilters();
			}
			catch { }
		}

		private void InitUI()
		{
			Text = "التقارير التفصيلية المتقدمة";
			base.Size = new Size(1160, 720);
			base.StartPosition = FormStartPosition.CenterScreen;
			RightToLeft = RightToLeft.Yes;
			BackColor = Theme.BgMain;
			Font = Theme.FontMain;

			dtpFrom = new DateTimePicker
			{
				Width = 135,
				Format = DateTimePickerFormat.Custom,
				CustomFormat = "yyyy/MM/dd",
				Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0),
				Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
				Margin = new Padding(0)
			};
			dtpFrom.ValueChanged += (s, e) => LoadCurrentTab();

			dtpTo = new DateTimePicker
			{
				Width = 135,
				Format = DateTimePickerFormat.Custom,
				CustomFormat = "yyyy/MM/dd",
				Value = DateTime.Now,
				Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
				Margin = new Padding(0)
			};
			dtpTo.ValueChanged += (s, e) => LoadCurrentTab();

			cboDatePresets = new ComboBox
			{
				Width = 120,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Color.White,
				ForeColor = Color.FromArgb(30, 41, 59),
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
				Margin = new Padding(0)
			};
			SetupDatePresets();

			cboWarehouse = new ComboBox
			{
				Width = 125,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Color.White,
				ForeColor = Color.FromArgb(30, 41, 59),
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
				Margin = new Padding(0)
			};
			LoadWarehouses();

			cboPayType = new ComboBox
			{
				Width = 115,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Color.White,
				ForeColor = Color.FromArgb(30, 41, 59),
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
				Margin = new Padding(0)
			};
			LoadPaymentTypes();

			cboEmployee = new ComboBox
			{
				Width = 135,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Color.White,
				ForeColor = Color.FromArgb(30, 41, 59),
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
				Margin = new Padding(0)
			};
			LoadEmployees();

			txtSearchClient = new TextBox
			{
				Width = 190,
				BackColor = Color.White,
				ForeColor = Color.FromArgb(30, 41, 59),
				BorderStyle = BorderStyle.FixedSingle,
				Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
				Margin = new Padding(0)
			};
			txtSearchClient.TextChanged += (s, e) => ApplyAllFilters();

			btnLoad = Theme.MakeButton("🔄 تحديث التقرير", Color.FromArgb(245, 158, 11));
			btnLoad.Size = new Size(130, 36);
			btnLoad.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
			btnLoad.Margin = new Padding(3, 2, 3, 2);
			btnLoad.Click += delegate { LoadCurrentTab(); };

			btnPrint = Theme.MakeButton("🖨️ طباعة", Color.FromArgb(37, 99, 235));
			btnPrint.Size = new Size(100, 36);
			btnPrint.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
			btnPrint.Margin = new Padding(3, 2, 3, 2);
			btnPrint.Click += BtnPrint_Click;

			btnExportPdf = Theme.MakeButton("📄 PDF", Color.FromArgb(220, 38, 38));
			btnExportPdf.Size = new Size(95, 36);
			btnExportPdf.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
			btnExportPdf.Margin = new Padding(3, 2, 3, 2);
			btnExportPdf.Click += BtnExportPdf_Click;

			btnExportExcel = Theme.MakeButton("📥 إكسيل", Color.FromArgb(16, 185, 129));
			btnExportExcel.Size = new Size(100, 36);
			btnExportExcel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
			btnExportExcel.Margin = new Padding(3, 2, 3, 2);
			btnExportExcel.Click += BtnExportExcel_Click;

			btnWhatsAppReport = Theme.MakeButton("📲 واتساب", Color.FromArgb(37, 211, 102));
			btnWhatsAppReport.Size = new Size(110, 36);
			btnWhatsAppReport.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
			btnWhatsAppReport.Margin = new Padding(3, 2, 3, 2);
			btnWhatsAppReport.Click += BtnWhatsAppReport_Click;
			btnWhatsAppReport.Visible = false;

			// ── 1. بانر علوي فخم (Header Banner) يعرض اسم التقرير وأزرار العمليات ──
			var pnlReportBanner = new Panel
			{
				Dock = DockStyle.Top,
				Height = 58,
				BackColor = Color.FromArgb(15, 23, 42),
				Padding = new Padding(12, 6, 12, 6),
				RightToLeft = RightToLeft.Yes
			};

			var pnlBannerActions = new FlowLayoutPanel
			{
				Dock = DockStyle.Left,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BackColor = Color.Transparent,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				Padding = new Padding(0, 4, 0, 4),
				Margin = new Padding(0),
				RightToLeft = RightToLeft.Yes
			};
			pnlBannerActions.Controls.Add(btnLoad);
			pnlBannerActions.Controls.Add(btnPrint);
			pnlBannerActions.Controls.Add(btnExportPdf);
			pnlBannerActions.Controls.Add(btnExportExcel);
			pnlBannerActions.Controls.Add(btnWhatsAppReport);

			var pnlBannerTitles = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent,
				Padding = new Padding(10, 2, 10, 2)
			};

			lblReportHeaderTitle = new Label
			{
				Dock = DockStyle.Top,
				Height = 26,
				ForeColor = Color.FromArgb(241, 196, 15), // Gold
				Font = new Font("Segoe UI", 12f, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleRight,
				Text = "📊 تقارير المبيعات الشاملة"
			};

			lblReportHeaderDesc = new Label
			{
				Dock = DockStyle.Fill,
				ForeColor = Color.FromArgb(203, 213, 225), // Light Slate
				Font = new Font("Segoe UI", 9f, FontStyle.Regular),
				TextAlign = ContentAlignment.MiddleRight,
				Text = "💡 استخدام التقرير: استعراض ومتابعة حركة المبيعات والأرباح وفواتير العملاء مع إمكانية التصدير والطباعة والبحث السريع."
			};

			pnlBannerTitles.Controls.Add(lblReportHeaderDesc);
			pnlBannerTitles.Controls.Add(lblReportHeaderTitle);

			pnlReportBanner.Controls.Add(pnlBannerTitles);
			pnlReportBanner.Controls.Add(pnlBannerActions);

			// ── 2. شريط أدوات الفلترة والبحث الموحد (Unified Filter & Search Bar) ──
			var pnlFiltersBar = new Panel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BackColor = Color.FromArgb(24, 38, 62),
				Padding = new Padding(10, 6, 10, 6),
				RightToLeft = RightToLeft.Yes
			};

			var flowFilters = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BackColor = Color.Transparent,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = true,
				Margin = new Padding(0),
				Padding = new Padding(0),
				RightToLeft = RightToLeft.Yes
			};

			flowFilters.Controls.Add(MakeFilterPanel("📅 من تاريخ:", dtpFrom, 135));
			flowFilters.Controls.Add(MakeFilterPanel("📅 إلى تاريخ:", dtpTo, 135));
			flowFilters.Controls.Add(MakeFilterPanel("⚡ الفترة:", cboDatePresets, 120));
			flowFilters.Controls.Add(MakeFilterPanel("🏢 المخزن:", cboWarehouse, 125));
			flowFilters.Controls.Add(MakeFilterPanel("💳 طريقة الدفع:", cboPayType, 115));
			flowFilters.Controls.Add(MakeFilterPanel("👔 الكاشير/المندوب:", cboEmployee, 135));
			flowFilters.Controls.Add(MakeFilterPanel("🔍 بحث في النتائج:", txtSearchClient, 190, Color.FromArgb(255, 220, 110)));

			pnlFiltersBar.Controls.Add(flowFilters);

			var pnlTopContainer = new Panel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BackColor = Color.FromArgb(15, 23, 42),
				Padding = new Padding(0),
				RightToLeft = RightToLeft.Yes
			};

			pnlTopContainer.Controls.Add(pnlFiltersBar);
			pnlTopContainer.Controls.Add(pnlReportBanner);
			base.Controls.Add(pnlTopContainer);

			tabReports = new TabControl
			{
				Dock = DockStyle.Fill,
				Font = Theme.FontMain,
				DrawMode = TabDrawMode.OwnerDrawFixed,
				ItemSize = new Size(0, 36),
				Padding = new Point(14, 6)
			};
			tabReports.DrawItem += TabReports_DrawItem;
			var allReports = new List<(string name, string tag)>
			{
				// ══════════════════════════════════════════════════════════════
				// تقارير المبيعات الشاملة
				// ══════════════════════════════════════════════════════════════
				("📅 تقرير المبيعات اليومية", "DailySalesSummary"),
				("📈 تقرير المبيعات خلال فترة", "SalesByPeriod"),
				("🧾 سجل فواتير المبيعات", "DetailedSales"),
				("📦 تفاصيل سطور وأصناف المبيعات", "DetailedSaleItems"),
				("📊 مبيعات الأصناف والربحية", "SalesByProduct"),
				("🏢 مبيعات المجموعات والأقسام", "SalesByCategory"),
				("👥 مبيعات العملاء والمسدد", "SalesByClient"),
				("👔 مبيعات المستخدمين والكاشير", "SalesByUser"),
				("💳 طرق الدفع والتحصيل", "SalesByPaymentMethod"),
				("🏷️ الخصومات والتخفيضات", "SalesDiscounts"),
				("🔄 مرتجعات المبيعات", "DetailedReturns"),
				("💰 أرباح وهامش المبيعات", "SalesProfitability"),
				("💤 الأصناف الراكدة (مشتراة ولم تُباع)", "StagnantProducts"),

				// ══════════════════════════════════════════════════════════════
				// تقارير المشتريات الشاملة
				// ══════════════════════════════════════════════════════════════
				("📅 تقرير المشتريات اليومية", "DailyPurchasesSummary"),
				("📈 تقرير المشتريات خلال فترة", "PurchasesByPeriod"),
				("🧾 سجل فواتير المشتريات", "DetailedPurchases"),
				("📦 تفاصيل سطور وأصناف المشتريات", "DetailedPurchaseItems"),
				("🤝 مشتريات الموردين والمسدد", "PurchasesBySupplier"),
				("📊 مشتريات الأصناف ومتوسط التكلفة", "PurchasesByProduct"),
				("🏢 مشتريات الأقسام والتصنيفات", "PurchasesByCategory"),
				("🔄 مرتجعات المشتريات", "DetailedPurchaseReturns"),
				("💵 المدفوعات للموردين والتسويات", "SupplierPayments"),
				("📈 أسعار الشراء وتغير الأسعار", "PurchasePricesTracking"),
				("⏳ المشتريات الآجلة والمديونيات", "CreditPurchases"),

				// ══════════════════════════════════════════════════════════════
				// تقارير الحسابات والمالية والتقفيل
				// ══════════════════════════════════════════════════════════════
				("📑 تقرير التقفيل اليومي", "DailyClosing"),
				("📊 سجل وتقارير الورديات", "ShiftsHistory"),
				("⚖️ مقارنة الورديات بالأيام التقويمية", "ShiftVsCalendarComparison"),
				("📊 قائمة الدخل والربحية", "IncomeStatementAndProfitability"),

				// ══════════════════════════════════════════════════════════════
				// تقارير العملاء والمناديب
				// ══════════════════════════════════════════════════════════════
				("🚚 مبيعات المناديب", "SalesByDriver"),
				("⚖️ أرصدة وبيانات العملاء", "ClientBalances"),
				("⏳ أعمار الديون (الديون الراكدة)", "DebtAging"),
				("📑 مبيعات عميل تفصيلي", "ClientProductSales"),
				("📋 سجل تقفيل المناديب", "Handovers"),

				// ══════════════════════════════════════════════════════════════
				// تقارير المخازن والجرد
				// ══════════════════════════════════════════════════════════════
				("📊 كميات الأصناف التفصيلي", "ProductQtyDetail"),
				("🚨 تقرير الهالك والتالف", "WastageLoss"),
				("📦 تقييم المخزن التفصيلي", "DetailedInventoryValuation"),
				("📊 حركة أصناف الموردين", "SupplierItemActivity"),
				("⚠️ تقرير انتهاء الصلاحية", "ExpiryReport"),
				("📊 تقرير فروق الجرد والعجز", "InventoryVariance")
			};

			var filteredReports = new List<(string name, string tag)>();
			foreach (var report in allReports)
			{
				bool keep = false;
				if (string.IsNullOrEmpty(_targetModule))
				{
					keep = true;
				}
				else if (_targetModule == "Sales")
				{
					keep = (report.tag == "DailySalesSummary" || report.tag == "SalesByPeriod" || report.tag == "DetailedSales" || report.tag == "DetailedSaleItems" || report.tag == "SalesByProduct" || report.tag == "SalesByCategory" || report.tag == "SalesByClient" || report.tag == "SalesByUser" || report.tag == "SalesByPaymentMethod" || report.tag == "SalesDiscounts" || report.tag == "DetailedReturns" || report.tag == "SalesProfitability" || report.tag == "StagnantProducts");
				}
				else if (_targetModule == "Purchases")
				{
					keep = (report.tag == "DailyPurchasesSummary" || report.tag == "PurchasesByPeriod" || report.tag == "DetailedPurchases" || report.tag == "DetailedPurchaseItems" || report.tag == "PurchasesBySupplier" || report.tag == "PurchasesByProduct" || report.tag == "PurchasesByCategory" || report.tag == "DetailedPurchaseReturns" || report.tag == "SupplierPayments" || report.tag == "PurchasePricesTracking" || report.tag == "CreditPurchases" || report.tag == "StagnantProducts");
				}
				else if (_targetModule == "Stores")
				{
					keep = (report.tag == "ProductQtyDetail" || report.tag == "WastageLoss" || report.tag == "DetailedInventoryValuation" || report.tag == "SupplierItemActivity" || report.tag == "ExpiryReport" || report.tag == "InventoryVariance" || report.tag == "PurchasesByProduct" || report.tag == "StagnantProducts");
				}
				else if (_targetModule == "Clients")
				{
					keep = (report.tag == "SalesByClient" || report.tag == "ClientBalances" || report.tag == "ClientProductSales" || report.tag == "DebtAging");
				}
				else if (_targetModule == "Suppliers")
				{
					keep = (report.tag == "PurchasesBySupplier" || report.tag == "SupplierPayments" || report.tag == "SupplierItemActivity" || report.tag == "CreditPurchases" || report.tag == "PurchasePricesTracking");
				}
				else if (_targetModule == "Drivers")
				{
					keep = (report.tag == "SalesByDriver" || report.tag == "Handovers");
				}
				else if (_targetModule == "Financials")
				{
					keep = (report.tag == "DailyClosing" || report.tag == "FinancialSummary" || report.tag == "IncomeStatementAndProfitability" || report.tag == "DailySalesSummary" || report.tag == "SalesProfitability");
				}
				else if (_targetModule == "Shifts" || _targetModule == "ShiftsHistory")
				{
					keep = (report.tag == "ShiftsHistory" || report.tag == "ShiftVsCalendarComparison");
				}

				if (keep && !Session.CanAccess(GetReportPermissionKey(report.tag)))
				{
					keep = false;
				}

				if (keep && !Session.CanViewCost("Reports"))
				{
					if (report.tag == "SalesProfitability" || report.tag == "IncomeStatementAndProfitability" || report.tag == "DetailedInventoryValuation")
					{
						keep = false;
					}
				}

				if (keep)
				{
					filteredReports.Add(report);
				}
			}

			foreach (var tuple in filteredReports)
			{
				string item = tuple.name;
				string item2 = tuple.tag;
				TabPage tabPage = new TabPage(item)
				{
					Tag = item2,
					BackColor = Theme.BgMain
				};
				if (item2 == "DetailedSales")
				{
					TableLayoutPanel layout = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 1,
						RowCount = 2,
						RightToLeft = RightToLeft.Yes
					};
					layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
					layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

					DataGridView dgDetailedSales = new DataGridView
					{
						Name = "dgDetailedSales",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
					};
					ApplyGridZebraStyle(dgDetailedSales);
					layout.Controls.Add(dgDetailedSales, 0, 0);

					TableLayoutPanel tblBottom = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 2,
						RowCount = 1,
						RightToLeft = RightToLeft.Yes,
						Margin = new Padding(0, 4, 0, 0)
					};
					tblBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
					tblBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
					tblBottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

					FlowLayoutPanel pnlActionButtons = new FlowLayoutPanel
					{
						Dock = DockStyle.Fill,
						FlowDirection = FlowDirection.TopDown,
						BackColor = Theme.BgCard,
						Padding = new Padding(10, 8, 10, 8),
						WrapContents = false,
						AutoScroll = true
					};

					var btnPrintReceipt = Theme.MakeButton("🧾 طباعة ريسيت حراري", Theme.Primary);
					btnPrintReceipt.Size = new Size(195, 34);
					btnPrintReceipt.Margin = new Padding(0, 0, 0, 8);

					var btnPrintA4 = Theme.MakeButton("📄 طباعة فاتورة (A4/A5)", Color.FromArgb(40, 120, 180));
					btnPrintA4.Size = new Size(195, 34);
					btnPrintA4.Margin = new Padding(0, 0, 0, 8);

					var btnSendWhatsApp = Theme.MakeButton("📱 إرسال واتساب للعميل", Color.FromArgb(37, 211, 102));
					btnSendWhatsApp.Size = new Size(195, 34);
					btnSendWhatsApp.ForeColor = Color.White;
					btnSendWhatsApp.Margin = new Padding(0, 0, 0, 12);

					Label lblItemsHeader = new Label
					{
						Text = "📦 الأصناف المسحوبة بالفاتورة:",
						Size = new Size(195, 40),
						ForeColor = Theme.TextSub,
						Font = Theme.FontBold,
						TextAlign = ContentAlignment.TopRight
					};

					pnlActionButtons.Controls.AddRange(new Control[] { btnPrintReceipt, btnPrintA4, btnSendWhatsApp, lblItemsHeader });

					DataGridView dgDetailedSaleItems = new DataGridView
					{
						Name = "dgDetailedSaleItems",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
					};
					ApplyGridZebraStyle(dgDetailedSaleItems);

					dgDetailedSaleItems.Columns.Add("ProductName", "اسم الصنف");
					dgDetailedSaleItems.Columns.Add("Quantity", "الكمية");
					dgDetailedSaleItems.Columns.Add("UnitPrice", "سعر الوحدة");
					dgDetailedSaleItems.Columns.Add("Discount", "الخصم");
					dgDetailedSaleItems.Columns.Add("TotalPrice", "الإجمالي");

					tblBottom.Controls.Add(pnlActionButtons, 0, 0);
					tblBottom.Controls.Add(dgDetailedSaleItems, 1, 0);
					layout.Controls.Add(tblBottom, 0, 1);

					dgDetailedSales.SelectionChanged += (s, e) =>
					{
						dgDetailedSaleItems.Rows.Clear();
						if (dgDetailedSales.SelectedRows.Count == 0) return;
						int saleID = 0;
						if (dgDetailedSales.Columns.Contains("SaleID") && dgDetailedSales.SelectedRows[0].Cells["SaleID"].Value != null)
						{
							int.TryParse(dgDetailedSales.SelectedRows[0].Cells["SaleID"].Value.ToString(), out saleID);
						}
						if (saleID > 0)
						{
							DataTable items = SaleDAL.GetItems(saleID);
							foreach (DataRow r in items.Rows)
							{
								string pName = r["ProductName"]?.ToString() ?? "";
								string qty = Convert.ToDecimal(r["Quantity"]).ToString("N2");
								string price = Convert.ToDecimal(r["UnitPrice"]).ToString("N2") + " ج";
								string disc = r.Table.Columns.Contains("DiscountAmt") && r["DiscountAmt"] != DBNull.Value && Convert.ToDecimal(r["DiscountAmt"]) > 0 ? Convert.ToDecimal(r["DiscountAmt"]).ToString("N2") : "-";
								string total = Convert.ToDecimal(r["TotalPrice"]).ToString("N2") + " ج";
								dgDetailedSaleItems.Rows.Add(pName, qty, price, disc, total);
							}
						}
					};

					btnPrintReceipt.Click += (s, e) =>
					{
						if (dgDetailedSales.SelectedRows.Count == 0 || !dgDetailedSales.Columns.Contains("SaleID")) return;
						if (int.TryParse(dgDetailedSales.SelectedRows[0].Cells["SaleID"].Value?.ToString(), out int sid) && sid > 0)
						{
							new FrmPrintSale(sid, "Receipt", showPreview: false);
						}
						else
						{
							MessageBox.Show("من فضلك اختر الفاتورة المراد طباعتها أولاً من الجدول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
					};

					btnPrintA4.Click += (s, e) =>
					{
						if (dgDetailedSales.SelectedRows.Count == 0 || !dgDetailedSales.Columns.Contains("SaleID")) return;
						if (int.TryParse(dgDetailedSales.SelectedRows[0].Cells["SaleID"].Value?.ToString(), out int sid) && sid > 0)
						{
							new FrmPrintSale(sid, "A4", showPreview: true);
						}
						else
						{
							MessageBox.Show("من فضلك اختر الفاتورة المراد طباعتها أولاً من الجدول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
					};

					btnSendWhatsApp.Click += (s, e) =>
					{
						if (dgDetailedSales.SelectedRows.Count == 0 || !dgDetailedSales.Columns.Contains("SaleID")) return;
						if (int.TryParse(dgDetailedSales.SelectedRows[0].Cells["SaleID"].Value?.ToString(), out int sid) && sid > 0)
						{
							FrmSale.SendSaleInvoiceWhatsApp(sid, this);
						}
						else
						{
							MessageBox.Show("من فضلك اختر الفاتورة المراد إرسالها أولاً من الجدول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
					};

					tabPage.Controls.Add(layout);
					tabReports.TabPages.Add(tabPage);
					continue;
				}

				if (item2 == "ClientProductSales")
				{
					TableLayoutPanel layout = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 1,
						RowCount = 3,
						RightToLeft = RightToLeft.Yes
					};
					layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
					layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
					layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

					FlowLayoutPanel pnlFilters = new FlowLayoutPanel
					{
						Dock = DockStyle.Fill,
						BackColor = Theme.BgCard,
						FlowDirection = FlowDirection.RightToLeft,
						WrapContents = false,
						Padding = new Padding(5)
					};

					Label lblClient = new Label { Text = "العميل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 0, 0), Font = Theme.FontBold };
					ComboBox cboClient = new ComboBox { Name = "cboFilterClient", Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 0, 0) };
					
					Label lblProduct = new Label { Text = "الصنف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
					ComboBox cboProduct = new ComboBox { Name = "cboFilterProduct", Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 0, 0) };

					Label lblType = new Label { Text = "نوع البيع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
					ComboBox cboSaleType = new ComboBox { Name = "cboFilterSaleType", Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 0, 0) };

					cboClient.Items.Add(new ComboItem(0, "كل العملاء"));
					try
					{
						DataTable dtClients = ClientDAL.GetAll(activeOnly: true);
						foreach (DataRow r in dtClients.Rows)
						{
							cboClient.Items.Add(new ComboItem(Convert.ToInt32(r["ClientID"]), r["ClientName"].ToString()));
						}
					}
					catch { }
					cboClient.DisplayMember = "Text";
					cboClient.SelectedIndex = 0;

					cboProduct.Items.Add(new ComboItem(0, "كل الأصناف"));
					try
					{
						DataTable dtProducts = ProductDAL.GetAll(activeOnly: true);
						foreach (DataRow r in dtProducts.Rows)
						{
							cboProduct.Items.Add(new ComboItem(Convert.ToInt32(r["ProductID"]), r["ProductName"].ToString()));
						}
					}
					catch { }
					cboProduct.DisplayMember = "Text";
					cboProduct.SelectedIndex = 0;

					cboSaleType.Items.Add(new ComboItem(0, "كل المبيعات"));
					cboSaleType.Items.Add(new ComboItem(1, "نقدي"));
					cboSaleType.Items.Add(new ComboItem(2, "آجل"));
					cboSaleType.Items.Add(new ComboItem(3, "تقسيط شرعي"));
					cboSaleType.Items.Add(new ComboItem(4, "حملة مندوب"));
					cboSaleType.DisplayMember = "Text";
					cboSaleType.SelectedIndex = 0;

					if (_preFilteredID > 0)
					{
						foreach (ComboItem itemObj in cboClient.Items)
						{
							if (itemObj.ID == _preFilteredID)
							{
								cboClient.SelectedItem = itemObj;
								break;
							}
						}
					}

					cboClient.SelectedIndexChanged += (s, e) => LoadCurrentTab();
					cboProduct.SelectedIndexChanged += (s, e) => LoadCurrentTab();
					cboSaleType.SelectedIndexChanged += (s, e) => LoadCurrentTab();

					pnlFilters.Controls.AddRange(new Control[] { lblClient, cboClient, lblProduct, cboProduct, lblType, cboSaleType });
					layout.Controls.Add(pnlFilters, 0, 0);

					DataGridView dgClientSales = new DataGridView
					{
						Name = "dgClientSales",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
					};
					ApplyGridZebraStyle(dgClientSales);
					layout.Controls.Add(dgClientSales, 0, 1);

					TableLayoutPanel pnlKPIs = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 2,
						RowCount = 1,
						BackColor = Theme.BgCard,
						Padding = new Padding(10, 5, 10, 5)
					};
					pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
					pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

					Label lblTotalSales = new Label
					{
						Name = "lblTotalSales",
						Text = "إجمالي مبيعات الفترة: 0.00 ج.م",
						Dock = DockStyle.Fill,
						ForeColor = Theme.Accent,
						Font = new Font("Segoe UI", 12f, FontStyle.Bold),
						TextAlign = ContentAlignment.MiddleLeft
					};
					Label lblTotalQty = new Label
					{
						Name = "lblTotalQty",
						Text = "إجمالي كمية البيع: 0.00",
						Dock = DockStyle.Fill,
						ForeColor = Theme.TextMain,
						Font = new Font("Segoe UI", 12f, FontStyle.Bold),
						TextAlign = ContentAlignment.MiddleRight
					};
					pnlKPIs.Controls.Add(lblTotalSales, 0, 0);
					pnlKPIs.Controls.Add(lblTotalQty, 1, 0);
					layout.Controls.Add(pnlKPIs, 0, 2);

					tabPage.Controls.Add(layout);
					tabReports.TabPages.Add(tabPage);
					continue;
				}

				if (item2 == "SupplierItemActivity")
				{
					TableLayoutPanel layout = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 1,
						RowCount = 2,
						RightToLeft = RightToLeft.Yes
					};
					layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
					layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

					FlowLayoutPanel pnlFilters = new FlowLayoutPanel
					{
						Dock = DockStyle.Fill,
						BackColor = Theme.BgCard,
						FlowDirection = FlowDirection.RightToLeft,
						WrapContents = false,
						Padding = new Padding(5)
					};

					Label lblSupplier = new Label { Text = "المورد:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 0, 0), Font = Theme.FontBold };
					ComboBox cboSupplier = new ComboBox { Name = "cboFilterSupplier", Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 0, 0) };
					
					Label lblCompany = new Label { Text = "الشركة المنتجة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
					ComboBox cboCompany = new ComboBox { Name = "cboFilterCompany", Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 0, 0) };

					Label lblSearch = new Label { Text = "بحث بالاسم/الكود:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
					TextBox txtSearch = new TextBox { Name = "txtFilterSearch", Width = 150, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5, 4, 0, 0) };

					cboSupplier.Items.Add(new ComboItem(0, "كل الموردين"));
					try
					{
						DataTable dtSuppliers = SupplierDAL.GetAll(activeOnly: true);
						foreach (DataRow r in dtSuppliers.Rows)
						{
							cboSupplier.Items.Add(new ComboItem(Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
						}
					}
					catch { }
					cboSupplier.DisplayMember = "Text";
					cboSupplier.SelectedIndex = 0;

					cboCompany.Items.Add(new ComboItem(0, "كل الشركات"));
					try
					{
						DataTable dtComp = DbHelper.Query("SELECT DISTINCT ProducerCompany FROM Products WHERE IsActive = 1 AND ProducerCompany IS NOT NULL AND ProducerCompany != '' ORDER BY ProducerCompany");
						foreach (DataRow r in dtComp.Rows)
						{
							string compName = r["ProducerCompany"].ToString();
							cboCompany.Items.Add(new ComboItem(cboCompany.Items.Count, compName));
						}
					}
					catch { }
					cboCompany.DisplayMember = "Text";
					cboCompany.SelectedIndex = 0;

					if (_preFilteredID > 0)
					{
						foreach (ComboItem itemObj in cboSupplier.Items)
						{
							if (itemObj.ID == _preFilteredID)
							{
								cboSupplier.SelectedItem = itemObj;
								break;
							}
						}
					}

					cboSupplier.SelectedIndexChanged += (s, e) => LoadCurrentTab();
					cboCompany.SelectedIndexChanged += (s, e) => LoadCurrentTab();
					txtSearch.TextChanged += (s, e) => LoadCurrentTab();

					pnlFilters.Controls.AddRange(new Control[] { lblSupplier, cboSupplier, lblCompany, cboCompany, lblSearch, txtSearch });
					layout.Controls.Add(pnlFilters, 0, 0);

					DataGridView dgSupplierActivity = new DataGridView
					{
						Name = "dgSupplierActivity",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
					};
					ApplyGridZebraStyle(dgSupplierActivity);
					layout.Controls.Add(dgSupplierActivity, 0, 1);

					tabPage.Controls.Add(layout);
					tabReports.TabPages.Add(tabPage);
					continue;
				}

				if (item2 == "IncomeStatementAndProfitability")
				{
					TableLayoutPanel splitPanel = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 2,
						RowCount = 1,
						RightToLeft = RightToLeft.Yes
					};
					splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
					splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));

					GroupBox grpPL = new GroupBox
					{
						Text = "📊 قائمة الدخل وقيمة الربحية",
						Dock = DockStyle.Fill,
						Font = Theme.FontBold,
						ForeColor = Theme.TextMain,
						RightToLeft = RightToLeft.Yes
					};
					DataGridView dgPL = new DataGridView
					{
						Name = "dgIncomeStatement",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
					};
					ApplyGridZebraStyle(dgPL);
					grpPL.Controls.Add(dgPL);
					splitPanel.Controls.Add(grpPL, 0, 0);

					TabControl subTab = new TabControl
					{
						Name = "subTabProfitability",
						Dock = DockStyle.Fill,
						Font = Theme.FontMain,
						RightToLeft = RightToLeft.Yes
					};
					
					TabPage subTabProduct = new TabPage("📦 ربحية الأصناف")
					{
						BackColor = Theme.BgMain
					};
					DataGridView dgProdProfit = new DataGridView
					{
						Name = "dgProductProfit",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
					};
					ApplyGridZebraStyle(dgProdProfit);
					subTabProduct.Controls.Add(dgProdProfit);
					subTab.TabPages.Add(subTabProduct);

					TabPage subTabClient = new TabPage("👥 ربحية العملاء")
					{
						BackColor = Theme.BgMain
					};
					DataGridView dgCliProfit = new DataGridView
					{
						Name = "dgClientProfit",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
					};
					ApplyGridZebraStyle(dgCliProfit);
					subTabClient.Controls.Add(dgCliProfit);
					subTab.TabPages.Add(subTabClient);

					Theme.StyleTabControl(subTab);
					splitPanel.Controls.Add(subTab, 1, 0);
					tabPage.Controls.Add(splitPanel);
					tabReports.TabPages.Add(tabPage);
					continue;
				}

				if (item2 == "DebtAging")
				{
					TableLayoutPanel layout = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 1,
						RowCount = 2,
						RightToLeft = RightToLeft.Yes
					};
					layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
					layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

					FlowLayoutPanel pnlFilters = new FlowLayoutPanel
					{
						Dock = DockStyle.Fill,
						BackColor = Theme.BgCard,
						FlowDirection = FlowDirection.RightToLeft,
						WrapContents = false,
						Padding = new Padding(5)
					};

					Label lblDriver = new Label { Text = "المندوب:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 0, 0), Font = Theme.FontBold };
					ComboBox cboDriver = new ComboBox { Name = "cboFilterDriver", Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 0, 0) };
					
					Label lblMinBalance = new Label { Text = "رصيد أكبر من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
					NumericUpDown nudMinBalance = new NumericUpDown { Name = "nudFilterMinBalance", Width = 80, Minimum = 0, Maximum = 9999999, DecimalPlaces = 0, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5, 4, 0, 0) };

					Label lblOverdueDays = new Label { Text = "تأخير السداد:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
					ComboBox cboOverdueDays = new ComboBox { Name = "cboFilterOverdueDays", Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 0, 0) };

					Label lblSearch = new Label { Text = "بحث بالعميل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
					TextBox txtSearch = new TextBox { Name = "txtFilterSearch", Width = 140, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5, 4, 0, 0) };

					cboDriver.Items.Add(new ComboItem(0, "كل المناديب"));
					try
					{
						DataTable dtDrivers = EmployeeDAL.GetDrivers();
						foreach (DataRow r in dtDrivers.Rows)
						{
							cboDriver.Items.Add(new ComboItem(Convert.ToInt32(r["EmpID"]), r["EmpName"].ToString()));
						}
					}
					catch { }
					cboDriver.DisplayMember = "Text";
					cboDriver.SelectedIndex = 0;

					cboOverdueDays.Items.Add(new ComboItem(0, "كل الديون"));
					cboOverdueDays.Items.Add(new ComboItem(30, "أكثر من 30 يوم"));
					cboOverdueDays.Items.Add(new ComboItem(60, "أكثر من 60 يوم"));
					cboOverdueDays.Items.Add(new ComboItem(90, "أكثر من 90 يوم"));
					cboOverdueDays.Items.Add(new ComboItem(120, "أكثر من 120 يوم"));
					cboOverdueDays.DisplayMember = "Text";
					cboOverdueDays.SelectedIndex = 0;

					cboDriver.SelectedIndexChanged += (s, e) => LoadCurrentTab();
					nudMinBalance.ValueChanged += (s, e) => LoadCurrentTab();
					cboOverdueDays.SelectedIndexChanged += (s, e) => LoadCurrentTab();
					txtSearch.TextChanged += (s, e) => LoadCurrentTab();

					pnlFilters.Controls.AddRange(new Control[] { lblDriver, cboDriver, lblMinBalance, nudMinBalance, lblOverdueDays, cboOverdueDays, lblSearch, txtSearch });
					layout.Controls.Add(pnlFilters, 0, 0);

					DataGridView dgDebtAging = new DataGridView
					{
						Name = "dgDebtAging",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
					};
					ApplyGridZebraStyle(dgDebtAging);
					layout.Controls.Add(dgDebtAging, 0, 1);

					tabPage.Controls.Add(layout);
					tabReports.TabPages.Add(tabPage);
					continue;
				}

				if (item2 == "StagnantProducts")
				{
					TableLayoutPanel layout = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 1,
						RowCount = 3,
						RightToLeft = RightToLeft.Yes
					};
					layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
					layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
					layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));

					FlowLayoutPanel pnlFilters = new FlowLayoutPanel
					{
						Dock = DockStyle.Fill,
						BackColor = Theme.BgCard,
						FlowDirection = FlowDirection.RightToLeft,
						WrapContents = false,
						Padding = new Padding(6, 6, 6, 4)
					};

					Label lblMode = new Label { Text = "🔍 نوع التقرير:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(4, 7, 0, 0), Font = Theme.FontBold };
					ComboBox cboMode = new ComboBox { Name = "cboFilterStagnantMode", Width = 235, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 4, 0, 0) };
					cboMode.Items.Add(new ComboItem(0, "🛒 تم شراؤه ولم يُباع نهائياً (مبيعات = 0)"));
					cboMode.Items.Add(new ComboItem(1, "⏳ لم يُباع خلال الفترة (رصيد بالمخزن)"));
					cboMode.Items.Add(new ComboItem(2, "💤 لم يُباع إطلاقاً (كل الوقت)"));
					cboMode.Items.Add(new ComboItem(3, "📉 بطيء الحركة (مبيعات ضعيفة <= 3)"));
					cboMode.DisplayMember = "Text";
					cboMode.SelectedIndex = 0;
					cboMode.SelectedIndexChanged += (s, e) => LoadCurrentTab();

					Label lblCategory = new Label { Text = "التصنيف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 7, 0, 0), Font = Theme.FontBold };
					ComboBox cboCat = new ComboBox { Name = "cboFilterStagnantCategory", Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 4, 0, 0) };
					cboCat.Items.Add(new ComboItem(0, "جميع التصنيفات"));
					try
					{
						var dtCats = CategoryDAL.GetAll(true);
						foreach (DataRow r in dtCats.Rows)
							cboCat.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
					}
					catch { }
					cboCat.DisplayMember = "Text";
					cboCat.SelectedIndex = 0;
					cboCat.SelectedIndexChanged += (s, e) => LoadCurrentTab();

					Label lblBrand = new Label { Text = "الشركة/الماركة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 7, 0, 0), Font = Theme.FontBold };
					ComboBox cboBrand = new ComboBox { Name = "cboFilterStagnantBrand", Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 4, 0, 0) };
					cboBrand.Items.Add("جميع الشركات والماركات");
					try
					{
						var dtB = LookupDAL.GetAll("Brands", "BrandName");
						if (dtB != null)
						{
							foreach (DataRow r in dtB.Rows)
							{
								string b = r["BrandName"]?.ToString()?.Trim();
								if (!string.IsNullOrEmpty(b) && !cboBrand.Items.Contains(b)) cboBrand.Items.Add(b);
							}
						}
						var dtP = LookupDAL.GetAll("ProducerCompanies", "ProducerName");
						if (dtP != null)
						{
							foreach (DataRow r in dtP.Rows)
							{
								string p = r["ProducerName"]?.ToString()?.Trim();
								if (!string.IsNullOrEmpty(p) && !cboBrand.Items.Contains(p)) cboBrand.Items.Add(p);
							}
						}
					}
					catch { }
					cboBrand.SelectedIndex = 0;
					cboBrand.SelectedIndexChanged += (s, e) => LoadCurrentTab();

					Label lblDays = new Label { Text = "أيام الركود:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 7, 0, 0), Font = Theme.FontBold };
					ComboBox cboDays = new ComboBox { Name = "cboFilterStagnantDays", Width = 135, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 4, 0, 0) };
					cboDays.Items.Add(new ComboItem(0, "كل الفترات"));
					cboDays.Items.Add(new ComboItem(30, "أكثر من شهر (30 يوم)"));
					cboDays.Items.Add(new ComboItem(60, "أكثر من شهرين (60 يوم)"));
					cboDays.Items.Add(new ComboItem(90, "أكثر من 3 شهور (90 يوم)"));
					cboDays.Items.Add(new ComboItem(180, "أكثر من 6 شهور (180 يوم)"));
					cboDays.Items.Add(new ComboItem(365, "أكثر من سنة (365 يوم)"));
					cboDays.DisplayMember = "Text";
					cboDays.SelectedIndex = 0;
					cboDays.SelectedIndexChanged += (s, e) => LoadCurrentTab();

					pnlFilters.Controls.AddRange(new Control[] { lblMode, cboMode, lblCategory, cboCat, lblBrand, cboBrand, lblDays, cboDays });
					layout.Controls.Add(pnlFilters, 0, 0);

					DataGridView dgStagnant = new DataGridView
					{
						Name = "dgStagnantProducts",
						Dock = DockStyle.Fill,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
					};
					ApplyGridZebraStyle(dgStagnant);
					layout.Controls.Add(dgStagnant, 0, 1);

					Panel pnlSummary = new Panel
					{
						Name = "pnlStagnantSummary",
						Dock = DockStyle.Fill,
						BackColor = Theme.BgCard,
						Padding = new Padding(10, 4, 10, 4)
					};
					Label lblStagnantSummary = new Label
					{
						Name = "lblStagnantSummary",
						Dock = DockStyle.Fill,
						ForeColor = Theme.Accent,
						Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
						TextAlign = ContentAlignment.MiddleLeft,
						Text = "📊 إجمالي الأصناف الراكدة: 0 | إجمالي كميات الرصيد: 0.00 | إجمالي قيمة البضاعة الراكدة بالتكلفة: 0.00 ج.م"
					};
					pnlSummary.Controls.Add(lblStagnantSummary);
					layout.Controls.Add(pnlSummary, 0, 2);

					tabPage.Controls.Add(layout);
					tabReports.TabPages.Add(tabPage);
					continue;
				}

				DataGridView value = new DataGridView
				{
					Dock = DockStyle.Fill,
					AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
				};
				ApplyGridZebraStyle(value);
				tabPage.Controls.Add(value);
				tabReports.TabPages.Add(tabPage);
			}
			tabReports.SelectedIndexChanged += delegate
			{
				if (txtSearchClient != null)
				{
					txtSearchClient.Text = "";
				}
				LoadCurrentTab();
			};
			base.Controls.Add(tabReports);
			tabReports.BringToFront();

			if (!string.IsNullOrEmpty(_defaultTabTag))
			{
				foreach (TabPage tp in tabReports.TabPages)
				{
					if (tp.Tag?.ToString() == _defaultTabTag)
					{
						tabReports.SelectedTab = tp;
						break;
					}
				}
			}

			LoadCurrentTab();
		}

		private void LoadCurrentTab()
		{
			if (tabReports.SelectedTab == null)
			{
				return;
			}
			string text = tabReports.SelectedTab.Tag?.ToString();

			// تحديث بانر عنوان ووصف التقرير
			if (!string.IsNullOrEmpty(text) && ReportDescriptions.TryGetValue(text, out var rptInfo))
			{
				if (lblReportHeaderTitle != null) lblReportHeaderTitle.Text = rptInfo.title;
				if (lblReportHeaderDesc != null) lblReportHeaderDesc.Text = "💡 استخدام التقرير: " + rptInfo.desc;
				this.Text = rptInfo.title;
			}
			else
			{
				if (lblReportHeaderTitle != null) lblReportHeaderTitle.Text = tabReports.SelectedTab.Text;
				if (lblReportHeaderDesc != null) lblReportHeaderDesc.Text = "💡 استعراض بيانات التقرير المختار مع إمكانية الفلترة والتصدير والطباعة.";
				this.Text = tabReports.SelectedTab.Text;
			}

			if (btnWhatsAppReport != null)
			{
				btnWhatsAppReport.Visible = (text == "ClientBalances");
			}
			int? warehouseID = null;
			if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem wh && wh.ID > 0)
			{
				warehouseID = wh.ID;
			}
			if (text == "IncomeStatementAndProfitability")
			{
				LoadIncomeStatementAndProfitability(warehouseID);
				return;
			}
			DataGridView dataGridView = GetActiveGrid();
			if (dataGridView != null)
			{
				dataGridView.Columns.Clear();
				dataGridView.Rows.Clear();
				switch (text)
				{
				case "ShiftsHistory":
					_currentDt = ShiftDAL.GetShiftsReport(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[]
					{
						("ShiftID", "رقم الوردية"),
						("SafeName", "الدرج / الخزنة"),
						("OpenedByName", "فتح بواسطة"),
						("OpenTime", "وقت الفتح"),
						("ClosedByName", "إغلاق بواسطة"),
						("CloseTime", "وقت الإغلاق"),
						("OpeningCash", "افتتاحي الدرج"),
						("CashSales", "مبيعات كاش الوردية"),
						("TotalSales", "إجمالي مبيعات الوردية"),
						("CalendarSales", "مبيعات اليوم التقويمي 📅"),
						("ExpectedCash", "النقدية المتوقعة"),
						("ActualCash", "النقدية الفعلية"),
						("Difference", "العجز / الزيادة"),
						("StatusArabic", "حالة الوردية"),
						("Notes", "الملاحظات")
					}, dataGridView);
					break;
				case "ShiftVsCalendarComparison":
					_currentDt = ShiftDAL.GetShiftVsCalendarComparison(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[]
					{
						("ShiftID", "رقم الوردية"),
						("StatusArabic", "حالة الوردية"),
						("OpenTime", "تاريخ ووقت فتح الوردية"),
						("CloseTime", "تاريخ ووقت إغلاق الوردية"),
						("ShiftSales", "إجمالي مبيعات فترة الوردية (الشيفت)"),
						("CalendarSales", "إجمالي مبيعات اليوم التقويمي (من 12 ص لـ 12 م) 📅"),
						("Difference", "الفارق الحسابي"),
						("Explanation", "توضيح الفارق بين التقريرين")
					}, dataGridView);
					break;
				case "DetailedSales":
					_currentDt = SaleDAL.GetAll(dtpFrom.Value, dtpTo.Value, warehouseID);
					var targetDgSales = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgDetailedSales") ?? dataGridView;
					if (targetDgSales != null) dataGridView = targetDgSales;
					SetupGrid(new(string, string)[10]
					{
						("SaleCode", "رقم الفاتورة"),
						("SaleDate", "التاريخ والوقت"),
						("SaleType", "النوع"),
						("ClientName", "العميل"),
						("DriverName", "المندوب"),
						("TotalAmount", "قيمة الفاتورة"),
						("TotalCost", "التكلفة"),
						("NetProfit", "الربح"),
						("Notes", "الملاحظات"),
						("SaleID", "معرف الفاتورة")
					}, dataGridView);
					if (dataGridView.Columns["SaleID"] != null) dataGridView.Columns["SaleID"].Visible = false;
					break;
				case "DetailedReturns":
					_currentDt = ReturnDAL.GetAll(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[]
					{
						("ReturnDate", "التاريخ والوقت"),
						("SaleCode", "الفاتورة الأصلية"),
						("ClientCode", "كود العميل"),
						("ClientName", "العميل"),
						("ItemsCount", "عدد الأصناف"),
						("PaymentType", "طريقة الدفع / السداد"),
						("TotalAmount", "قيمة المرتجع"),
						("Notes", "البيان / الملاحظات"),
						("ReturnID", "معرف المرتجع")
					}, dataGridView);
					if (dataGridView.Columns["ReturnID"] != null) dataGridView.Columns["ReturnID"].Visible = false;
					break;
				case "DetailedPurchaseReturns":
					_currentDt = PurchaseReturnDAL.GetAll(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[6]
					{
						("ReturnDate", "التاريخ والوقت"),
						("PurchaseCode", "الفاتورة الأصلية"),
						("SupplierName", "المورد / العميل"),
						("TotalAmount", "قيمة المرتجع"),
						("Notes", "البيان / الملاحظات"),
						("ReturnID", "معرف المرتجع")
					}, dataGridView);
					if (dataGridView.Columns["ReturnID"] != null) dataGridView.Columns["ReturnID"].Visible = false;
					break;
				case "DailySalesSummary":
					_currentDt = ReportDAL.GetDailySalesSummary(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[]
					{
						("SaleDay", "اليوم / التاريخ"),
						("InvoiceCount", "عدد الفواتير"),
						("GrossSales", "إجمالي المبيعات"),
						("TotalDiscounts", "الخصومات"),
						("TotalSales", "الصافي بعد الخصم"),
						("TotalReturns", "المرتجعات"),
						("NetSales", "صافي المبيعات النهائي"),
						("TotalCost", "تكلفة المبيعات"),
						("GrossProfit", "مجمل الربح"),
						("ProfitMarginPct", "هامش الربح %")
					}, dataGridView);
					break;
				case "SalesByPeriod":
					_currentDt = ReportDAL.GetSalesByPeriod(dtpFrom.Value, dtpTo.Value, "Daily", warehouseID);
					SetupGrid(new(string, string)[]
					{
						("PeriodName", "الفترة الزمنية"),
						("InvoiceCount", "عدد الفواتير"),
						("CashSales", "مبيعات نقدي"),
						("VisaSales", "مبيعات فيزا"),
						("CreditSales", "مبيعات آجل"),
						("TotalDiscounts", "الخصومات"),
						("TotalSales", "إجمالي المبيعات"),
						("TotalCost", "التكلفة"),
						("NetProfit", "صافي الربح"),
						("ProfitMarginPct", "هامش الربح %")
					}, dataGridView);
					break;
				case "DetailedSaleItems":
					_currentDt = ReportDAL.GetDetailedSaleItems(dtpFrom.Value, dtpTo.Value, warehouseID, keyword: txtSearchClient != null ? txtSearchClient.Text.Trim() : null);
					SetupGrid(new(string, string)[]
					{
						("SaleDate", "التاريخ والوقت"),
						("SaleCode", "رقم الفاتورة"),
						("ClientName", "العميل"),
						("ProductCode", "كود الصنف"),
						("ProductName", "اسم الصنف"),
						("CategoryName", "القسم / المجموعة"),
						("Quantity", "الكمية"),
						("UnitName", "الوحدة"),
						("UnitPrice", "سعر البيع"),
						("DiscountAmt", "الخصم"),
						("TotalPrice", "الإجمالي"),
						("ItemCost", "التكلفة"),
						("ItemProfit", "الربح"),
						("SaleTypeArabic", "طريقة الدفع"),
						("CreatedByName", "المستخدم / الكاشير"),
						("WarehouseName", "المخزن")
					}, dataGridView);
					break;
				case "SalesByCategory":
					_currentDt = ReportDAL.GetSalesByCategory(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[]
					{
						("CategoryName", "المجموعة / القسم"),
						("DistinctProductsCount", "عدد الأصناف المباعة"),
						("TotalQtySold", "إجمالي الكميات المباعة"),
						("TotalDiscounts", "إجمالي الخصومات"),
						("TotalSalesAmount", "إجمالي المبيعات"),
						("TotalCost", "التكلفة"),
						("NetProfit", "الربح"),
						("ProfitMarginPct", "هامش الربح %")
					}, dataGridView);
					break;
				case "SalesByUser":
					_currentDt = ReportDAL.GetSalesByUser(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[]
					{
						("EmployeeName", "المستخدم / الكاشير"),
						("InvoiceCount", "عدد الفواتير"),
						("CashSales", "مبيعات نقدي"),
						("VisaSales", "مبيعات فيزا"),
						("CreditSales", "مبيعات آجل"),
						("TotalDiscounts", "الخصومات"),
						("TotalSales", "إجمالي المبيعات"),
						("TotalReturns", "المرتجعات"),
						("NetSales", "صافي المبيعات")
					}, dataGridView);
					break;
				case "SalesByPaymentMethod":
					_currentDt = ReportDAL.GetSalesByPaymentMethod(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[]
					{
						("PaymentMethodName", "طريقة الدفع / الحساب"),
						("InvoiceCount", "عدد العمليات / الفواتير"),
						("CashAmount", "المحصل نقدي (كاش)"),
						("VisaAmount", "المحصل إلكتروني (فيزا)"),
						("CreditAmount", "المتبقي آجل"),
						("TotalAmount", "إجمالي المبالغ")
					}, dataGridView);
					break;
				case "SalesDiscounts":
					_currentDt = ReportDAL.GetSalesDiscounts(dtpFrom.Value, dtpTo.Value, warehouseID, keyword: txtSearchClient != null ? txtSearchClient.Text.Trim() : null);
					SetupGrid(new(string, string)[]
					{
						("SaleDate", "التاريخ والوقت"),
						("SaleCode", "رقم الفاتورة"),
						("ClientName", "العميل"),
						("CreatedByName", "المستخدم"),
						("TotalBeforeDiscount", "الإجمالي قبل الخصم"),
						("DiscountAmount", "قيمة الخصم"),
						("DiscountPct", "نسبة الخصم %"),
						("TotalAfterDiscount", "الصافي بعد الخصم"),
						("Notes", "الملاحظات / سبب الخصم")
					}, dataGridView);
					break;
				case "SalesProfitability":
					_currentDt = ReportDAL.GetSalesProfitability(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[]
					{
						("SaleDay", "اليوم / التاريخ"),
						("GrossSales", "إجمالي المبيعات"),
						("TotalDiscounts", "الخصومات"),
						("TotalReturns", "المرتجعات"),
						("NetSales", "صافي المبيعات"),
						("TotalCost", "تكلفة المبيعات"),
						("ReturnsCost", "تكلفة المرتجعات"),
						("NetCost", "صافي التكلفة"),
						("NetProfit", "صافي الأرباح"),
						("MarginPct", "هامش الربح %")
					}, dataGridView);
					break;

				case "StagnantProducts":
				{
					string mode = "PurchasedNeverSold";
					var cboM = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterStagnantMode");
					if (cboM != null)
					{
						int sel = cboM.SelectedIndex;
						mode = sel == 1 ? "NoSalesInPeriodWithStock" : sel == 2 ? "ZeroSalesAllTime" : sel == 3 ? "SlowMoving" : "PurchasedNeverSold";
					}

					int? catId = null;
					var cboC = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterStagnantCategory");
					if (cboC?.SelectedItem is ComboItem ciC && ciC.ID > 0)
					{
						catId = ciC.ID;
					}

					string brand = null;
					var cboB = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterStagnantBrand");
					if (cboB?.SelectedItem != null && cboB.SelectedIndex > 0)
					{
						brand = cboB.SelectedItem.ToString();
					}

					int minDays = 0;
					var cboD = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterStagnantDays");
					if (cboD?.SelectedItem is ComboItem ciD)
					{
						minDays = ciD.ID;
					}

					string kw = txtSearchClient != null ? txtSearchClient.Text.Trim() : null;

					_currentDt = ReportDAL.GetStagnantProducts(dtpFrom.Value, dtpTo.Value, warehouseID, catId, brand, mode, minDays, kw);

					var dg = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgStagnantProducts") ?? dataGridView;
					SetupGrid(new(string, string)[]
					{
						("ProductCode", "كود الصنف"),
						("PartNumber", "رقم القطعة"),
						("ProductName", "اسم الصنف"),
						("CategoryName", "القسم / التصنيف"),
						("Brand", "الشركة / الماركة"),
						("ShelfLocation", "الرف"),
						("Unit", "الوحدة"),
						("PurchasePrice", "سعر الشراء"),
						("SalePrice", "سعر البيع"),
						("TotalPurchasedQty", "إجمالي المشتريات"),
						("LastPurchaseDate", "تاريخ آخر شراء"),
						("TotalSoldQty", "الكمية المباعة"),
						("CurrentStock", "الرصيد الحالي"),
						("StagnantStockValue", "قيمة الركود بالتكلفة"),
						("StagnantDays", "أيام الركود")
					}, dg);

					var lblSummary = FindControlByName<Label>(tabReports.SelectedTab, "lblStagnantSummary");
					if (lblSummary != null && _currentDt != null)
					{
						int count = _currentDt.Rows.Count;
						decimal totalQty = 0;
						decimal totalVal = 0;
						foreach (DataRow r in _currentDt.Rows)
						{
							if (r["CurrentStock"] != DBNull.Value) totalQty += Convert.ToDecimal(r["CurrentStock"]);
							if (r["StagnantStockValue"] != DBNull.Value) totalVal += Convert.ToDecimal(r["StagnantStockValue"]);
						}
						lblSummary.Text = $"📊 إجمالي الأصناف الراكدة: {count:N0} صنف | إجمالي كميات الرصيد: {totalQty:N2} | إجمالي قيمة البضاعة الراكدة بالتكلفة: {totalVal:N2} ج.م";
					}
					break;
				}

				case "DailyPurchasesSummary":
					_currentDt = PurchaseDAL.GetDailyPurchasesSummary(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[]
					{
						("PurchaseDay", "اليوم / التاريخ"),
						("InvoiceCount", "عدد فواتير الشراء"),
						("GrossPurchases", "إجمالي المشتريات"),
						("TotalDiscounts", "الخصومات"),
						("TotalTax", "الضريبة"),
						("TotalShipping", "الشحن"),
						("TotalPurchases", "الصافي بعد الخصم"),
						("TotalReturns", "المرتجعات"),
						("NetPurchases", "صافي المشتريات النهائي")
					}, dataGridView);
					break;
				case "PurchasesByPeriod":
					_currentDt = PurchaseDAL.GetPurchasesByPeriod(dtpFrom.Value, dtpTo.Value, "Daily");
					SetupGrid(new(string, string)[]
					{
						("PeriodName", "الفترة الزمنية"),
						("InvoiceCount", "عدد فواتير الشراء"),
						("CashPurchases", "مشتريات نقدي"),
						("CreditPurchases", "مشتريات آجل"),
						("TotalDiscounts", "الخصومات"),
						("TotalTax", "الضريبة"),
						("TotalPurchases", "إجمالي المشتريات")
					}, dataGridView);
					break;
				case "DetailedPurchaseItems":
					_currentDt = PurchaseDAL.GetDetailedPurchaseItems(dtpFrom.Value, dtpTo.Value, keyword: txtSearchClient != null ? txtSearchClient.Text.Trim() : null);
					SetupGrid(new(string, string)[]
					{
						("PurchaseDate", "التاريخ والوقت"),
						("PurchaseCode", "رقم الفاتورة"),
						("SupplierInvoiceNo", "رقم فاتورة المورد"),
						("SupplierName", "المورد"),
						("ProductCode", "كود الصنف"),
						("ProductName", "اسم الصنف"),
						("CategoryName", "القسم / التصنيف"),
						("Quantity", "الكمية"),
						("UnitName", "الوحدة"),
						("UnitPrice", "سعر الشراء"),
						("DiscountAmt", "الخصم"),
						("TotalPrice", "الإجمالي"),
						("PurchaseTypeArabic", "نوع الشراء"),
						("CreatedByName", "الموظف"),
						("WarehouseName", "المخزن")
					}, dataGridView);
					break;
				case "PurchasesByCategory":
					_currentDt = PurchaseDAL.GetPurchasesByCategory(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[]
					{
						("CategoryName", "القسم / التصنيف"),
						("DistinctProductsCount", "عدد الأصناف المشتراة"),
						("TotalQtyPurchased", "إجمالي الكميات"),
						("TotalDiscounts", "الخصومات"),
						("TotalPurchasesAmount", "إجمالي قيمة المشتريات"),
						("InvoicesCount", "عدد الفواتير")
					}, dataGridView);
					break;
				case "SupplierPayments":
					_currentDt = PurchaseDAL.GetSupplierPaymentsReport(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[]
					{
						("SupplierName", "المورد"),
						("Phone", "الهاتف"),
						("TotalPurchases", "إجمالي المشتريات بالفترة"),
						("TotalPaid", "إجمالي المدفوعات بالفترة"),
						("CurrentBalance", "الرصيد / المستحق الحالي"),
						("LastPaymentAmount", "قيمة آخر دفعة"),
						("LastPaymentDate", "تاريخ آخر دفعة")
					}, dataGridView);
					break;
				case "PurchasePricesTracking":
					_currentDt = PurchaseDAL.GetPurchasePricesTracking(dtpFrom.Value, dtpTo.Value, keyword: txtSearchClient != null ? txtSearchClient.Text.Trim() : null);
					SetupGrid(new(string, string)[]
					{
						("ProductCode", "كود الصنف"),
						("ProductName", "اسم الصنف"),
						("SupplierName", "المورد"),
						("LastPrice", "آخر سعر شراء"),
						("PreviousPrice", "السعر السابق"),
						("ChangePercentage", "نسبة التغير %"),
						("LastPurchaseDate", "تاريخ آخر شراء")
					}, dataGridView);
					break;
				case "CreditPurchases":
					_currentDt = PurchaseDAL.GetCreditPurchasesReport(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[]
					{
						("PurchaseDate", "تاريخ الفاتورة"),
						("PurchaseCode", "رقم الفاتورة"),
						("SupplierInvoiceNo", "فاتورة المورد"),
						("SupplierName", "المورد"),
						("Phone", "الهاتف"),
						("TotalInvoiceAmount", "قيمة الفاتورة"),
						("PaidAmount", "المدفوع"),
						("RemainingAmount", "المتبقي من الفاتورة"),
						("SupplierTotalBalance", "إجمالي رصيد المورد"),
						("Notes", "الملاحظات")
					}, dataGridView);
					break;

				case "DetailedPurchases":
					_currentDt = ReportDAL.GetDetailedPurchases(dtpFrom.Value, dtpTo.Value, warehouseID, keyword: txtSearchClient != null ? txtSearchClient.Text.Trim() : null);
					SetupGrid(new(string, string)[]
					{
						("PurchaseDate", "التاريخ والوقت"),
						("PurchaseCode", "رقم الفاتورة"),
						("SupplierInvoiceNo", "رقم فاتورة المورد"),
						("PartyName", "جهة الشراء (المورد / العميل)"),
						("PurchaseSourceText", "نوع الجهة"),
						("PurchaseType", "نوع الفاتورة"),
						("Subtotal", "قبل الخصم"),
						("DiscountAmount", "الخصم"),
						("TaxAmount", "الضريبة"),
						("ShippingCost", "الشحن"),
						("TotalAmount", "الصافي النهائي"),
						("WarehouseName", "المخزن"),
						("CreatedByName", "الموظف"),
						("Notes", "الملاحظات")
					}, dataGridView);
					break;
				case "PurchasesByProduct":
					_currentDt = PurchaseDAL.GetPurchasesByProduct(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[]
					{
						("ProductCode", "كود الصنف"),
						("ProductName", "اسم الصنف"),
						("CategoryName", "التصنيف"),
						("TotalQtyPurchased", "إجمالي الكمية المشتراة"),
						("Unit", "الوحدة"),
						("AvgPurchasePrice", "متوسط سعر الشراء"),
						("LastPurchasePrice", "آخر سعر شراء"),
						("MinPurchasePrice", "أقل سعر"),
						("MaxPurchasePrice", "أعلى سعر"),
						("TotalCost", "إجمالي التكلفة / القيمة")
					}, dataGridView);
					break;
				case "PurchasesBySupplier":
					_currentDt = PurchaseDAL.GetPurchasesBySupplier(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[]
					{
						("SupplierName", "المورد"),
						("Phone", "الهاتف"),
						("InvoiceCount", "عدد الفواتير"),
						("TotalPurchases", "إجمالي المشتريات"),
						("TotalReturns", "المرتجعات"),
						("NetPurchases", "صافي المشتريات"),
						("TotalPaid", "المسدد للمورد"),
						("CurrentBalance", "الرصيد الحالي المستحق")
					}, dataGridView);
					break;
				case "SalesByDay":
					_currentDt = ReportDAL.SalesByDay(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[8]
					{
						("SaleDay", "اليوم"),
						("Count", "عدد الفواتير"),
						("CashTotal", "مبيعات نقدي"),
						("CreditTotal", "مبيعات آجل"),
						("LoadTotal", "حمولات مناديب"),
						("Total", "إجمالي اليوم"),
						("TotalCost", "إجمالي التكلفة"),
						("NetProfit", "صافي الربح")
					}, dataGridView);
					break;
				case "SalesByDriver":
					_currentDt = ReportDAL.SalesByDriver(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[7]
					{
						("DriverName", "المندوب"),
						("Count", "عدد فواتيره"),
						("CashTotal", "مبيعات نقدي"),
						("CreditTotal", "مبيعات آجل"),
						("Total", "إجمالي المبيعات"),
						("TotalCost", "التكلفة"),
						("NetProfit", "الربح")
					}, dataGridView);
					break;
				case "SalesByClient":
					_currentDt = ReportDAL.SalesByClient(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[11]
					{
						("ClientName", "العميل"),
						("Phone", "الهاتف"),
						("Count", "فواتير الشراء"),
						("CashTotal", "شراء نقدي"),
						("CreditTotal", "شراء آجل"),
						("ReturnsTotal", "إجمالي مرتجعاته"),
						("PaidTotal", "إجمالي مسدداته"),
						("Total", "إجمالي الشراء"),
						("CurrentBalance", "المديونية الحالية"),
						("TotalCost", "التكلفة"),
						("NetProfit", "صافي الربح")
					}, dataGridView);
					break;
				case "SalesByProduct":
					_currentDt = ReportDAL.SalesByProduct(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[13]
					{
						("ProductName", "الصنف"),
						("Unit", "الوحدة"),
						("CurrentStock", "الرصيد الفعلي"),
						("AvgPrice", "متوسط سعر البيع"),
						("TotalQty", "الكمية المباعة"),
						("TotalAmount", "إجمالي المبيعات"),
						("ReturnedQty", "الكمية المرتجعة"),
						("ReturnedAmount", "إجمالي المرتجعات"),
						("NetQty", "صافي الكمية"),
						("NetAmount", "صافي المبيعات"),
						("TotalCost", "التكلفة"),
						("NetProfit", "الربح"),
						("ProfitMargin", "نسبة الربح %")
					}, dataGridView);
					break;
				case "DetailedInventoryValuation":
					_currentDt = ReportDAL.GetInventoryValuation(warehouseID);
					SetupGrid(new(string, string)[9]
					{
						("ProductCode", "كود الصنف"),
						("ProductName", "اسم الصنف"),
						("Unit", "الوحدة"),
						("PurchasePrice", "سعر التكلفة"),
						("SalePrice", "سعر البيع"),
						("CurrentStock", "الرصيد الحالي"),
						("StockValue", "قيمة المخزن بالتكلفة"),
						("StockSaleValue", "قيمة المخزن بسعر البيع"),
						("ExpectedProfit", "الأرباح المتوقعة")
					}, dataGridView);
					break;
				case "ProductQtyDetail":
					_currentDt = ReportDAL.GetProductQtyDetail(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[14]
					{
						("ProductCode",    "كود الصنف"),
						("ProductName",    "اسم الصنف"),
						("Unit",           "الوحدة"),
						("CurrentStock",   "الرصيد الفعلي"),
						("SalePrice",      "سعر البيع"),
						("LastAdjQty",     "رصيد آخر تسوية"),
						("SoldQty",        "إجمالي المبيع"),
						("CashQty",        "نقدي"),
						("CreditQty",      "آجل"),
						("DriverLoadQty",  "حمولات مناديب"),
						("ReturnedQty",    "مرتجع مبيعات"),
						("DriverReturnQty","مرتجع مناديب"),
						("NetSoldQty",     "صافي المبيع"),
						("TotalSalesAmt",  "إجمالي قيمة المبيع")
					}, dataGridView);
					break;
				case "Handovers":
					_currentDt = DriverDAL.GetHandovers(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[9]
					{
						("HandoverDate", "التاريخ والوقت"),
						("DriverName", "المندوب"),
						("TotalLoaded", "المحمل"),
						("TotalReturned", "المرتجع"),
						("TotalDead", "النافق"),
						("TotalExtra", "الزيادة"),
						("TotalDeficit", "العجز"),
						("Notes", "ملاحظات التقفيل"),
						("CreatedBy", "المستلم")
					}, dataGridView);
					break;
				case "WastageLoss":
					_currentDt = ReportDAL.WastageLossReport(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[8]
					{
						("TransDate", "التاريخ والوقت"),
						("SourceType", "مصدر الهالك"),
						("ProductName", "الصنف"),
						("Quantity", "الكمية التالفة"),
						("UnitPrice", "سعر الوحدة"),
						("TotalCost", "التكلفة الإجمالية"),
						("ResponsibleParty", "المسؤول / المندوب"),
						("Notes", "البيان / ملاحظات")
					}, dataGridView);
					break;
				case "ExpiryReport":
					_currentDt = DbHelper.Query(@"
						SELECT
							p.ProductCode AS ProductCode,
							p.ProductName AS ProductName,
							pb.BatchNumber AS BatchNumber,
							p.Unit AS Unit,
							pb.Quantity AS Quantity,
							w.WarehouseName AS WarehouseName,
							pb.ExpiryDate AS ExpiryDate,
							CASE
								WHEN pb.ExpiryDate < CAST(GETDATE() AS DATE) THEN N'منتهي'
								WHEN pb.ExpiryDate < DATEADD(DAY, 30, CAST(GETDATE() AS DATE)) THEN N'قريب الانتهاء'
								ELSE N'سليم'
							END AS ExpiryStatus
						FROM ProductBatches pb
						JOIN Products p ON pb.ProductID = p.ProductID
						JOIN Warehouses w ON pb.WarehouseID = w.WarehouseID
						WHERE pb.Quantity > 0 AND pb.ExpiryDate IS NOT NULL
						ORDER BY pb.ExpiryDate ASC");
					SetupGrid(new(string, string)[]
					{
						("ProductCode", "كود الصنف"),
						("ProductName", "اسم الصنف"),
						("BatchNumber", "رقم الدفعة"),
						("Unit", "الوحدة"),
						("Quantity", "الكمية"),
						("WarehouseName", "المخزن"),
						("ExpiryDate", "تاريخ الانتهاء"),
						("ExpiryStatus", "الحالة")
					}, dataGridView);
					foreach (DataGridViewRow dgRow in dataGridView.Rows)
					{
						if (dgRow.IsNewRow) continue;
						string status = dgRow.Cells["ExpiryStatus"]?.Value?.ToString() ?? "";
						if (status == "منتهي")
							dgRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 202, 202);
						else if (status == "قريب الانتهاء")
							dgRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 235, 200);
						else
							dgRow.DefaultCellStyle.BackColor = Color.FromArgb(198, 246, 213);
					}
					break;
				case "InventoryVariance":
					_currentDt = InventoryDAL.GetVarianceReport(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new (string, string)[]
					{
						("AdjDate", "التاريخ والوقت"),
						("WarehouseName", "المخزن"),
						("ProductCode", "كود الصنف"),
						("ProductName", "اسم الصنف"),
						("Unit", "الوحدة"),
						("BookQty", "الرصيد الدفتري"),
						("ActualQty", "الرصيد الفعلي"),
						("DiffQty", "الفارق"),
						("PurchasePrice", "سعر الشراء"),
						("SalePrice", "سعر البيع"),
						("ShortageCostLoss", "خسارة العجز (تكلفة)"),
						("SurplusCostGain", "زيادة التكلفة (فائض)"),
						("CreatedBy", "المسؤول"),
						("Notes", "ملاحظات")
					}, dataGridView);
					break;
				case "ClientBalances":
					_currentDt = ReportDAL.ClientsReport();
					SetupGrid(new(string, string)[10]
					{
						("ClientCode", "كود العميل"),
						("ClientName", "اسم العميل"),
						("Phone", "هاتف أساسي"),
						("Phone2", "هاتف إضافي"),
						("Address", "العنوان"),
						("DriverName", "المندوب الافتراضي"),
						("MaxCreditLimit", "حد المديونية"),
						("OpeningBalance", "رصيد افتتاحي"),
						("Balance", "المديونية الحالية"),
						("Notes", "ملاحظات")
					}, dataGridView);
					break;
				case "DebtAging":
					{
						int? filterDriverID = null;
						decimal filterMinBalance = 0;
						int filterMinDays = 0;

						var cboDriver = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterDriver");
						if (cboDriver != null && cboDriver.SelectedItem is ComboItem drItem && drItem.ID > 0)
						{
							filterDriverID = drItem.ID;
						}

						var nudMinBalance = FindControlByName<NumericUpDown>(tabReports.SelectedTab, "nudFilterMinBalance");
						if (nudMinBalance != null)
						{
							filterMinBalance = nudMinBalance.Value;
						}

						var cboOverdueDays = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterOverdueDays");
						if (cboOverdueDays != null && cboOverdueDays.SelectedItem is ComboItem daysItem && daysItem.ID > 0)
						{
							filterMinDays = daysItem.ID;
						}

						string filterSearch = "";
						var txtSearch = FindControlByName<TextBox>(tabReports.SelectedTab, "txtFilterSearch");
						if (txtSearch != null)
						{
							filterSearch = txtSearch.Text.Trim();
						}

						_currentDt = ReportDAL.DebtAgingReport(dtpFrom.Value, dtpTo.Value, filterDriverID, filterMinBalance, filterMinDays, filterSearch);
						SetupGrid(new(string, string)[9]
						{
							("ClientCode", "كود العميل"),
							("ClientName", "اسم العميل"),
							("Phone", "رقم الهاتف"),
							("Balance", "المديونية الحالية"),
							("LastInvoiceDate", "تاريخ آخر فاتورة"),
							("LastInvoiceAmount", "قيمة آخر فاتورة"),
							("LastPaymentDate", "تاريخ آخر توريد"),
							("LastPaymentAmount", "قيمة آخر توريد"),
							("DaysSinceLastPayment", "أيام منذ آخر توريد")
						}, dataGridView);
					}
					break;
				case "FinancialSummary":
					_currentDt = ReportDAL.GetFinancialSummary(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[2]
					{
						("Indicator", "المؤشر المالي والتشغيلي"),
						("Val", "القيمة المالية للنشاط")
					}, dataGridView);
					break;
				case "DailyClosing":
					_currentDt = new DataTable();
					LoadDailyClosingReport(dataGridView, warehouseID);
					break;
				case "ClientProductSales":
					{
						int? clientID = null;
						int? productID = null;
						string saleType = "الكل";

						var cboC = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterClient");
						var cboP = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterProduct");
						var cboT = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterSaleType");

						if (cboC != null && cboC.SelectedItem is ComboItem cli && cli.ID > 0)
						{
							clientID = cli.ID;
						}
						if (cboP != null && cboP.SelectedItem is ComboItem prod && prod.ID > 0)
						{
							productID = prod.ID;
						}
						if (cboT != null && cboT.SelectedItem is ComboItem itemType)
						{
							if (itemType.ID == 1) saleType = "Cash";
							else if (itemType.ID == 2) saleType = "Credit";
							else if (itemType.ID == 3) saleType = "Installment";
							else if (itemType.ID == 4) saleType = "DriverLoad";
						}

						_currentDt = ReportDAL.GetClientProductSalesReport(dtpFrom.Value, dtpTo.Value, clientID, productID, saleType, warehouseID);
						
						SetupGrid(new(string, string)[]
						{
							("رقم الفاتورة", "رقم الفاتورة"),
							("تاريخ الفاتورة", "التاريخ والوقت"),
							("العميل", "العميل"),
							("الصنف", "الصنف"),
							("الكمية", "الكمية"),
							("سعر الوحدة", "سعر الوحدة"),
							("الصافي", "الصافي"),
							("نوع البيع", "نوع البيع")
						}, dataGridView);

						decimal totalSales = 0;
						decimal totalQty = 0;
						foreach (DataRow r in _currentDt.Rows)
						{
							totalSales += Convert.ToDecimal(r["الصافي"]);
							totalQty += Convert.ToDecimal(r["الكمية"]);
						}

						var lblSales = FindControlByName<Label>(tabReports.SelectedTab, "lblTotalSales");
						var lblQty = FindControlByName<Label>(tabReports.SelectedTab, "lblTotalQty");
						if (lblSales != null) lblSales.Text = $"إجمالي مبيعات الفترة: {totalSales:N2} ج.م";
						if (lblQty != null) lblQty.Text = $"إجمالي كمية البيع: {totalQty:N2}";
					}
					break;
				case "SupplierItemActivity":
					{
						int? supplierID = null;
						string producerCompany = "الكل";
						string search = "";

						var cboS = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterSupplier");
						var cboC = FindControlByName<ComboBox>(tabReports.SelectedTab, "cboFilterCompany");
						var txtS = FindControlByName<TextBox>(tabReports.SelectedTab, "txtFilterSearch");

						if (cboS != null && cboS.SelectedItem is ComboItem sup && sup.ID > 0)
						{
							supplierID = sup.ID;
						}
						if (cboC != null && cboC.SelectedItem is ComboItem comp && comp.ID > 0)
						{
							producerCompany = comp.Text;
						}
						if (txtS != null)
						{
							search = txtS.Text.Trim();
						}

						_currentDt = ReportDAL.GetSupplierItemActivityReport(dtpFrom.Value, dtpTo.Value, supplierID, producerCompany, search);

						bool hasCompany = false;
						if (_currentDt != null)
						{
							foreach (DataRow r in _currentDt.Rows)
							{
								if (r.Table.Columns.Contains("الشركة المنتجة") && !string.IsNullOrWhiteSpace(r["الشركة المنتجة"]?.ToString()))
								{
									hasCompany = true;
									break;
								}
							}
						}

						var colList = new List<(string, string)>
						{
							("الصنف", "الصنف")
						};
						if (hasCompany)
						{
							colList.Add(("الشركة المنتجة", "الشركة المنتجة"));
						}
						colList.AddRange(new (string, string)[]
						{
							("المخزون الحالي", "المخزون الحالي"),
							("الكمية المباعة", "الكمية المباعة"),
							("قيمة المبيعات", "قيمة المبيعات"),
							("الكمية المشتراة", "الكمية المشتراة"),
							("قيمة المشتريات", "قيمة المشتريات"),
							("الحالة", "الحالة")
						});

						SetupGrid(colList.ToArray(), dataGridView);
					}
					break;
				}
				FillGrid(dataGridView);
				ApplyAllFilters();
			}
		}

		private void LoadIncomeStatementAndProfitability(int? warehouseID)
		{
			TabPage tab = tabReports.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Tag?.ToString() == "IncomeStatementAndProfitability");
			if (tab == null) return;

			DataGridView dgPL = FindControlByName<DataGridView>(tab, "dgIncomeStatement");
			DataGridView dgProd = FindControlByName<DataGridView>(tab, "dgProductProfit");
			DataGridView dgCli = FindControlByName<DataGridView>(tab, "dgClientProfit");

			if (dgPL == null || dgProd == null || dgCli == null) return;

			_currentDt = new DataTable();

			// 1. Income Statement (P&L)
			dgPL.Columns.Clear();
			dgPL.Rows.Clear();
			dgPL.Columns.Add("Item", "إسم الحساب");
			dgPL.Columns.Add("Value", "القيمة");

			try
			{
				DataTable dt = ReportDAL.GetIncomeStatement(dtpFrom.Value, dtpTo.Value, warehouseID);
				if (dt.Rows.Count > 0)
				{
					DataRow r = dt.Rows[0];
					decimal grossSales = Convert.ToDecimal(r["GrossSales"]);
					decimal returns = Convert.ToDecimal(r["SalesReturns"]);
					decimal salesAfterReturns = grossSales - returns;

					// جلب خصومات المبيعات للفترة
					object discountsObj = DbHelper.Scalar(
						@"SELECT ISNULL(SUM(DiscountAmount), 0) 
						  FROM Sales 
						  WHERE (COL_LENGTH('Sales', 'IsPosted') IS NULL OR ISNULL(IsPosted, 1) = 1) 
							AND CAST(SaleDate AS DATE) BETWEEN @f AND @t 
							AND (@wid IS NULL OR WarehouseID = @wid)", 
						DbHelper.P("@f", dtpFrom.Value.Date), 
						DbHelper.P("@t", dtpTo.Value.Date), 
						DbHelper.P("@wid", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
					decimal discounts = discountsObj != null ? Convert.ToDecimal(discountsObj) : 0m;

					decimal netSales = salesAfterReturns - discounts;

					decimal grossCOGS = Convert.ToDecimal(r["GrossCOGS"]);
					decimal returnsCOGS = Convert.ToDecimal(r["ReturnsCOGS"]);
					decimal netCOGS = grossCOGS - returnsCOGS;

					decimal grossProfit = netSales - netCOGS;

					// إضافة البنود بالترتيب الدقيق المطلوب
					AddPlRow(dgPL, "قيمة المبيعات الافتراضية", grossSales, false);
					AddPlRow(dgPL, "قيمة المرتجعات", returns, true);
					AddPlRow(dgPL, "قيمة المبيعات بعد المرتجع", salesAfterReturns, false, Color.FromArgb(45, 65, 90));
					AddPlRow(dgPL, "خصم البيع", discounts, true);
					AddPlRow(dgPL, "قيمة صافي المبيعات", netSales, false, Color.FromArgb(30, 45, 60));
					AddPlRow(dgPL, "تكلفة المبيعات", netCOGS, true);
					AddPlRow(dgPL, "الربح بعد التكلفة", grossProfit, grossProfit < 0, Color.FromArgb(50, 40, 70));

					// تفصيل المصروفات التشغيلية
					DataTable dtExps = DbHelper.Query(@"
						SELECT ISNULL(e.ExpenseType, N'مصروفات متنوعة') AS TypeName, SUM(e.Amount) AS Total
						FROM Expenses e
						WHERE CAST(e.ExpenseDate AS DATE) BETWEEN @f AND @t
						GROUP BY e.ExpenseType
						ORDER BY SUM(e.Amount) DESC", 
						DbHelper.P("@f", dtpFrom.Value.Date), 
						DbHelper.P("@t", dtpTo.Value.Date));

					decimal totalExpenses = 0m;
					foreach (DataRow re in dtExps.Rows)
					{
						string expName = re["TypeName"].ToString();
						decimal amt = Convert.ToDecimal(re["Total"]);
						AddPlRow(dgPL, expName, amt, true);
						totalExpenses += amt;
					}

					decimal whWastage = Convert.ToDecimal(r["WarehouseWastage"]);
					decimal drWastage = Convert.ToDecimal(r["DriverWastage"]);

					if (whWastage > 0)
					{
						AddPlRow(dgPL, "هالك وتالف المستودعات", whWastage, true);
						totalExpenses += whWastage;
					}
					if (drWastage > 0)
					{
						AddPlRow(dgPL, "هالك وتالف المناديب", drWastage, true);
						totalExpenses += drWastage;
					}

					decimal netProfit = grossProfit - totalExpenses;

					AddPlRow(dgPL, "إجمالي المصروفات", totalExpenses, true, Color.FromArgb(70, 45, 45));
					AddPlRow(dgPL, "صافي الربح", netProfit, netProfit < 0, Color.FromArgb(30, 60, 30));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("حدث خطأ أثناء تحميل قائمة الدخل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			// 2. Product Profitability
			dgProd.Columns.Clear();
			dgProd.Rows.Clear();
			SetupGrid(new(string, string)[]
			{
				("ProductName", "الصنف"),
				("NetQty", "الكمية الصافية"),
				("NetAmount", "صافي المبيعات"),
				("TotalCost", "صافي التكلفة"),
				("NetProfit", "صافي الربح"),
				("ProfitMargin", "نسبة الربح %")
			}, dgProd);

			try
			{
				DataTable dtProd = ReportDAL.SalesByProduct(dtpFrom.Value, dtpTo.Value, warehouseID);
				decimal totalNetAmt = 0, totalCost = 0, totalProfit = 0;
				foreach (DataRow row in dtProd.Rows)
				{
					decimal netAmt = Convert.ToDecimal(row["NetAmount"]);
					decimal cost = Convert.ToDecimal(row["TotalCost"]);
					decimal profit = Convert.ToDecimal(row["NetProfit"]);
					decimal margin = netAmt != 0 ? (profit / netAmt) * 100 : 0;

					totalNetAmt += netAmt;
					totalCost += cost;
					totalProfit += profit;

					dgProd.Rows.Add(
						row["ProductName"],
						Convert.ToDecimal(row["NetQty"]).ToString("N2"),
						netAmt.ToString("N2"),
						cost.ToString("N2"),
						profit.ToString("N2"),
						margin.ToString("N1") + " %"
					);
				}
				if (dgProd.Rows.Count > 0)
				{
					int idx = dgProd.Rows.Add();
					dgProd.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(30, 60, 30);
					dgProd.Rows[idx].DefaultCellStyle.ForeColor = Color.LightGreen;
					dgProd.Rows[idx].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
					dgProd.Rows[idx].Cells[0].Value = "الإجمالي";
					dgProd.Rows[idx].Cells[2].Value = totalNetAmt.ToString("N2");
					dgProd.Rows[idx].Cells[3].Value = totalCost.ToString("N2");
					dgProd.Rows[idx].Cells[4].Value = totalProfit.ToString("N2");
					dgProd.Rows[idx].Cells[5].Value = (totalNetAmt != 0 ? (totalProfit / totalNetAmt) * 100 : 0).ToString("N1") + " %";
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("حدث خطأ أثناء تحميل ربحية الأصناف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			// 3. Client Profitability
			dgCli.Columns.Clear();
			dgCli.Rows.Clear();
			SetupGrid(new(string, string)[]
			{
				("ClientName", "العميل"),
				("NetAmount", "صافي المبيعات"),
				("TotalCost", "صافي التكلفة"),
				("NetProfit", "صافي الربح"),
				("ProfitMargin", "نسبة الربح %")
			}, dgCli);

			try
			{
				DataTable dtCli = ReportDAL.SalesByClient(dtpFrom.Value, dtpTo.Value, warehouseID);
				decimal totalCliNet = 0, totalCliCost = 0, totalCliProfit = 0;
				foreach (DataRow row in dtCli.Rows)
				{
					decimal grossSales = Convert.ToDecimal(row["Total"]);
					decimal returns = Convert.ToDecimal(row["ReturnsTotal"]);
					decimal netAmt = grossSales - returns;
					decimal cost = Convert.ToDecimal(row["TotalCost"]);
					decimal profit = Convert.ToDecimal(row["NetProfit"]);
					decimal margin = netAmt != 0 ? (profit / netAmt) * 100 : 0;

					totalCliNet += netAmt;
					totalCliCost += cost;
					totalCliProfit += profit;

					dgCli.Rows.Add(
						row["ClientName"],
						netAmt.ToString("N2"),
						cost.ToString("N2"),
						profit.ToString("N2"),
						margin.ToString("N1") + " %"
					);
				}
				if (dgCli.Rows.Count > 0)
				{
					int idx = dgCli.Rows.Add();
					dgCli.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(30, 60, 30);
					dgCli.Rows[idx].DefaultCellStyle.ForeColor = Color.LightGreen;
					dgCli.Rows[idx].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
					dgCli.Rows[idx].Cells[0].Value = "الإجمالي";
					dgCli.Rows[idx].Cells[1].Value = totalCliNet.ToString("N2");
					dgCli.Rows[idx].Cells[2].Value = totalCliCost.ToString("N2");
					dgCli.Rows[idx].Cells[3].Value = totalCliProfit.ToString("N2");
					dgCli.Rows[idx].Cells[4].Value = (totalCliNet != 0 ? (totalCliProfit / totalCliNet) * 100 : 0).ToString("N1") + " %";
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("حدث خطأ أثناء تحميل ربحية العملاء:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			ApplyAllFilters();
		}

		private void AddPlRow(DataGridView dg, string name, decimal val, bool isNegative, Color? customBg = null)
		{
			int index = dg.Rows.Add(name, (isNegative && val != 0 ? "-" : "") + val.ToString("N2"));
			if (customBg.HasValue)
			{
				dg.Rows[index].DefaultCellStyle.BackColor = customBg.Value;
				dg.Rows[index].DefaultCellStyle.ForeColor = Color.White;
				dg.Rows[index].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
			}
			if (isNegative && val != 0)
			{
				dg.Rows[index].Cells[1].Style.ForeColor = Color.OrangeRed;
				dg.Rows[index].Cells[1].Style.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
			}
			else
			{
				dg.Rows[index].Cells[1].Style.ForeColor = Color.LightGreen;
				dg.Rows[index].Cells[1].Style.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
			}
		}

		private T FindControlByName<T>(Control parent, string name) where T : Control
		{
			if ((string.IsNullOrEmpty(name) || parent.Name == name) && parent is T typed)
			{
				return typed;
			}
			foreach (Control child in parent.Controls)
			{
				T val = FindControlByName<T>(child, name);
				if (val != null)
				{
					return val;
				}
			}
			return null;
		}

		private DataGridView GetActiveGrid()
		{
			if (tabReports.SelectedTab == null) return null;
			
			string text = tabReports.SelectedTab.Tag?.ToString();
			if (text == "IncomeStatementAndProfitability")
			{
				var subTab = FindControlByName<TabControl>(tabReports.SelectedTab, "subTabProfitability");
				if (subTab != null)
				{
					var dgProd = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgProductProfit");
					var dgCli = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgClientProfit");
					var dgPL = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgIncomeStatement");

					if (dgProd != null && (dgProd.ContainsFocus || dgProd.Focused)) return dgProd;
					if (dgCli != null && (dgCli.ContainsFocus || dgCli.Focused)) return dgCli;
					if (dgPL != null && (dgPL.ContainsFocus || dgPL.Focused)) return dgPL;

					if (subTab.ContainsFocus)
					{
						return (subTab.SelectedIndex == 0) ? dgProd : dgCli;
					}
					return dgPL;
				}
				return FindControlByName<DataGridView>(tabReports.SelectedTab, "dgIncomeStatement");
			}
			if (text == "DetailedSales")
			{
				return FindControlByName<DataGridView>(tabReports.SelectedTab, "dgDetailedSales");
			}
			if (text == "ClientProductSales")
			{
				return FindControlByName<DataGridView>(tabReports.SelectedTab, "dgClientSales");
			}
			if (text == "SupplierItemActivity")
			{
				return FindControlByName<DataGridView>(tabReports.SelectedTab, "dgSupplierActivity");
			}
			if (text == "DebtAging")
			{
				return FindControlByName<DataGridView>(tabReports.SelectedTab, "dgDebtAging");
			}
			if (text == "StagnantProducts")
			{
				return FindControlByName<DataGridView>(tabReports.SelectedTab, "dgStagnantProducts");
			}
			
			return FindControlByName<DataGridView>(tabReports.SelectedTab, "") ?? tabReports.SelectedTab.Controls.OfType<DataGridView>().FirstOrDefault();
		}

		private void ApplyGridZebraStyle(DataGridView dg)
		{
			if (dg == null) return;
			dg.EnableDoubleBuffering();
			dg.RowTemplate.Height = 28;
			dg.RowHeadersVisible = false;
			dg.AllowUserToAddRows = false;
			dg.ReadOnly = true;
			dg.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			dg.RightToLeft = RightToLeft.Yes;
			dg.BorderStyle = BorderStyle.None;

			bool isDark = (AppConfig.AppTheme == "Dark");
			Color row1 = isDark ? Color.FromArgb(30, 41, 59) : Color.White;
			Color row2 = isDark ? Color.FromArgb(24, 32, 47) : Color.FromArgb(248, 250, 252);
			Color textCol = isDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(30, 41, 59);
			Color selBg = Color.FromArgb(37, 99, 235);
			Color gridLine = isDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);

			dg.GridColor = gridLine;
			dg.BackgroundColor = isDark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(245, 247, 250);

			dg.DefaultCellStyle = new DataGridViewCellStyle
			{
				BackColor = row1,
				ForeColor = textCol,
				SelectionBackColor = selBg,
				SelectionForeColor = Color.White,
				Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
				Alignment = DataGridViewContentAlignment.MiddleLeft
			};

			dg.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
			{
				BackColor = row2,
				ForeColor = textCol,
				SelectionBackColor = selBg,
				SelectionForeColor = Color.White,
				Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
				Alignment = DataGridViewContentAlignment.MiddleLeft
			};

			dg.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
			{
				BackColor = Color.FromArgb(30, 41, 59),
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 10f, FontStyle.Bold),
				Alignment = DataGridViewContentAlignment.MiddleCenter
			};
			dg.ColumnHeadersHeight = 34;
			dg.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			dg.EnableHeadersVisualStyles = false;
		}

		private void SetupGrid((string field, string header)[] cols, DataGridView dg)
		{
			if (dg == null) return;
			ApplyGridZebraStyle(dg);
			dg.Columns.Clear();
			if (cols.Length > 6)
			{
				dg.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
			}
			else
			{
				dg.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			}

			bool canSeeCost = Session.CanViewCost("Reports");
			for (int i = 0; i < cols.Length; i++)
			{
				var (name, headerText) = cols[i];
				bool isNameCol = (name == "الصنف" || name == "ProductName" || name == "اسم الصنف" || name == "البيان" || headerText == "الصنف" || headerText == "اسم الصنف");
				bool isCostCol = (name == "PurchasePrice" || name == "AvgPurchasePrice" || name == "LastPurchasePrice" || name == "MinPurchasePrice" || name == "MaxPurchasePrice" ||
				                  name == "TotalCost" || name == "NetProfit" || name == "ProfitMargin" || name == "MarginPct" ||
				                  name == "StagnantStockValue" || name == "StockValue" || name == "ExpectedProfit" ||
				                  name == "ShortageCostLoss" || name == "SurplusCostGain" ||
				                  headerText.Contains("سعر الشراء") || headerText.Contains("سعر التكلفة") || headerText.Contains("التكلفة") ||
				                  headerText.Contains("الربح") || headerText.Contains("الأرباح") || headerText.Contains("هامش"));

				var col = new DataGridViewTextBoxColumn
				{
					Name = name,
					HeaderText = headerText,
					FillWeight = isNameCol ? 350f : 100f,
					Visible = !isCostCol || canSeeCost
				};
				if (isNameCol)
				{
					col.MinimumWidth = 280;
				}
				else if (name == "Notes" || name == "Address")
				{
					col.MinimumWidth = 150;
				}
				dg.Columns.Add(col);
			}
		}

		private void FillGrid(DataGridView dg)
		{
			if (_currentDt == null)
			{
				return;
			}
			dg.Rows.Clear();
			string text = tabReports.SelectedTab?.Tag?.ToString();
			if (text == "FinancialSummary")
			{
				if (_currentDt.Rows.Count != 0)
				{
					DataRow dataRow = _currentDt.Rows[0];
					decimal num = Convert.ToDecimal(dataRow["TotalSales"]);
					decimal val = Convert.ToDecimal(dataRow["CashSales"]);
					decimal val2 = Convert.ToDecimal(dataRow["CreditSales"]);
					decimal val3 = Convert.ToDecimal(dataRow["DriverLoadsSales"]);
					decimal num2 = Convert.ToDecimal(dataRow["TotalReturns"]);
					decimal val4 = Convert.ToDecimal(dataRow["ClientPayments"]);
					decimal num3 = Convert.ToDecimal(dataRow["TotalExpenses"]);
					decimal num4 = Convert.ToDecimal(dataRow["CashInflow"]);
					decimal num5 = Convert.ToDecimal(dataRow["CashOutflow"]);
					decimal num6 = num - num2;
					decimal num7 = num6 - num3;
					decimal num8 = num4 - num5;
					AddIndicatorRow(dg, "\ud83d\udcca إجمالي قيمة المبيعات خلال الفترة (شامل)", num, isNegative: false);
					AddIndicatorRow(dg, "\ud83d\udcb5 المبيعات النقدية المباشرة", val, isNegative: false);
					AddIndicatorRow(dg, "\ud83d\udcb3 المبيعات الآجلة للعملاء", val2, isNegative: false);
					AddIndicatorRow(dg, "\ud83d\ude9a مبيعات حمولات المناديب الصادرة", val3, isNegative: false);
					AddIndicatorRow(dg, "\ud83d\udd04 إجمالي قيمة مرتجعات البيع", num2, isNegative: true);
					AddIndicatorRow(dg, "\ud83d\udcc8 صافي المبيعات (المبيعات - المرتجعات)", num6, isNegative: false, Color.FromArgb(30, 45, 60));
					AddIndicatorRow(dg, "\ud83d\udce5 إجمالي تحصيلات ومسددات العملاء الآجل", val4, isNegative: false);
					AddIndicatorRow(dg, "\ud83d\udcb8 إجمالي المصروفات العامة والتشغيلية", num3, isNegative: true);
					AddIndicatorRow(dg, "\ud83d\udcb0 إجمالي مقبوضات الخزينة (وارد)", num4, isNegative: false);
					AddIndicatorRow(dg, "\ud83d\udce4 إجمالي مدفوعات الخزينة (صادر)", num5, isNegative: true);
					AddIndicatorRow(dg, "⚖\ufe0f صافي التدفق النقدي بالخزينة (وارد - صادر)", num8, num8 < 0m, Color.FromArgb(45, 45, 30));
					AddIndicatorRow(dg, "\ud83c\udfc6 صافي الأرباح التشغيلية التقريبية (الصافي - المصاريف)", num7, num7 < 0m, Color.FromArgb(30, 60, 30));
				}
				return;
			}
			decimal[] array = new decimal[dg.Columns.Count];
			bool flag = false;
			foreach (DataRow row in _currentDt.Rows)
			{
				object[] array2 = new object[dg.Columns.Count];
				for (int i = 0; i < dg.Columns.Count; i++)
				{
					string name = dg.Columns[i].Name;
					if (_currentDt.Columns.Contains(name))
					{
						object obj = row[name];
						if (obj is decimal num9)
						{
							array2[i] = num9.ToString("N2");
							array[i] += num9;
							flag = true;
						}
						else if (obj is double dblVal)
						{
							array2[i] = dblVal.ToString("N2");
							array[i] += (decimal)dblVal;
							flag = true;
						}
						else if (obj is float fltVal)
						{
							array2[i] = fltVal.ToString("N2");
							array[i] += (decimal)fltVal;
							flag = true;
						}
						else if (obj is int num10)
						{
							array2[i] = num10.ToString();
							array[i] += (decimal)num10;
							flag = true;
						}
						else if (obj is DateTime dateTime)
						{
							array2[i] = dateTime.ToString("dd/MM/yyyy HH:mm");
						}
						else
						{
							if (name == "SaleType")
							{
								string typ = obj.ToString();
								array2[i] = typ == "Cash" ? "نقدي" : typ == "Credit" ? "آجل" : typ == "DriverLoad" ? "تحميل مندوب" : typ;
							}
							else
							{
								array2[i] = obj;
							}
						}
					}
					else if (name == "ProfitMargin")
					{
						decimal netAmt = _currentDt.Columns.Contains("NetAmount") ? Convert.ToDecimal(row["NetAmount"]) : 0;
						decimal profit = _currentDt.Columns.Contains("NetProfit") ? Convert.ToDecimal(row["NetProfit"]) : 0;
						decimal margin = netAmt != 0 ? (profit / netAmt) * 100 : 0;
						array2[i] = margin.ToString("N1") + " %";
					}
				}
				dg.Rows.Add(array2);
			}
			if (text == "SupplierItemActivity")
			{
				for (int r = 0; r < dg.Rows.Count; r++)
				{
					var row = dg.Rows[r];
					if (row.Cells["الحالة"] != null && row.Cells["الحالة"].Value != null)
					{
						string status = row.Cells["الحالة"].Value.ToString();
						if (status == "نشط")
						{
							row.DefaultCellStyle.BackColor = Color.FromArgb(230, 245, 230);
							row.DefaultCellStyle.ForeColor = Color.DarkGreen;
						}
						else if (status == "راكد")
						{
							row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
							row.DefaultCellStyle.ForeColor = Color.DarkRed;
						}
					}
				}
			}

			if (!flag || dg.Rows.Count <= 0)
			{
				return;
			}
			int index = dg.Rows.Add();
			dg.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
			dg.Rows[index].DefaultCellStyle.ForeColor = Color.FromArgb(245, 158, 11);
			dg.Rows[index].DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 41, 59);
			dg.Rows[index].DefaultCellStyle.SelectionForeColor = Color.FromArgb(245, 158, 11);
			dg.Rows[index].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
			dg.Rows[index].Cells[0].Value = "الإجمالي الكلي";
			for (int j = 1; j < dg.Columns.Count; j++)
			{
				string name2 = dg.Columns[j].Name;
				if (name2 == "ProfitMargin")
				{
					int netAmtIdx = -1;
					int netProfitIdx = -1;
					for (int k = 0; k < dg.Columns.Count; k++)
					{
						if (dg.Columns[k].Name == "NetAmount" || dg.Columns[k].Name == "Total" || dg.Columns[k].Name == "TotalAmount") netAmtIdx = k;
						if (dg.Columns[k].Name == "NetProfit") netProfitIdx = k;
					}
					if (netAmtIdx >= 0 && netProfitIdx >= 0)
					{
						decimal totAmt = array[netAmtIdx];
						decimal totProfit = array[netProfitIdx];
						decimal totMargin = totAmt != 0 ? (totProfit / totAmt) * 100 : 0;
						dg.Rows[index].Cells[j].Value = totMargin.ToString("N1") + " %";
					}
					continue;
				}
				switch (name2)
				{
				default:
					if (!(name2 == "MaxCreditLimit"))
					{
						continue;
					}
					break;
				case "Total":
				case "TotalAmount":
				case "Count":
				case "CashTotal":
				case "CreditTotal":
				case "LoadTotal":
				case "ReturnsTotal":
				case "PaidTotal":
				case "CurrentBalance":
				case "TotalQty":
				case "ReturnedQty":
				case "ReturnedAmount":
				case "NetQty":
				case "NetAmount":
				case "SoldQty":
				case "CashQty":
				case "CreditQty":
				case "DriverLoadQty":
				case "DriverReturnQty":
				case "NetSoldQty":
				case "LastAdjQty":
				case "TotalSalesAmt":
				case "CurrentStock":
				case "SalePrice":
				case "TotalLoaded":
				case "TotalReturned":
				case "TotalDead":
				case "TotalExtra":
				case "TotalDeficit":
				case "Balance":
				case "OpeningBalance":
				case "TotalCost":
				case "NetProfit":
				case "StockValue":
				case "StockSaleValue":
				case "ExpectedProfit":
				case "الكمية":
				case "الصافي":
				case "المخزون الحالي":
				case "الكمية المباعة":
				case "قيمة المبيعات":
				case "الكمية المشتراة":
				case "قيمة المشتريات":
				case "TotalPurchasedQty":
				case "TotalSoldQty":
				case "StagnantStockValue":
					break;
				}
				string text2 = ((name2 == "Count" || name2 == "المخزون الحالي" || name2 == "الكمية المباعة" || name2 == "الكمية المشتراة" || name2 == "الكمية" || name2 == "TotalPurchasedQty" || name2 == "TotalSoldQty") ? "N0" : "N2");
				dg.Rows[index].Cells[j].Value = array[j].ToString(text2);
			}
		}

		private void AddIndicatorRow(DataGridView dg, string name, decimal val, bool isNegative, Color? customBg = null)
		{
			int index = dg.Rows.Add(name, val.ToString("N2") + " ج.م");
			if (customBg.HasValue)
			{
				dg.Rows[index].DefaultCellStyle.BackColor = customBg.Value;
				dg.Rows[index].DefaultCellStyle.ForeColor = Color.White;
				dg.Rows[index].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
			}
			if (isNegative)
			{
				dg.Rows[index].Cells[1].Style.ForeColor = Color.OrangeRed;
				dg.Rows[index].Cells[1].Style.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
			}
			else
			{
				dg.Rows[index].Cells[1].Style.ForeColor = Color.LightGreen;
				dg.Rows[index].Cells[1].Style.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
			}
		}

		private void BtnPrint_Click(object sender, EventArgs e)
		{
			DataGridView dg = GetActiveGrid();
			if (_currentDt == null || dg == null || dg.Rows.Count == 0)
			{
				MessageBox.Show("لا توجد بيانات متاحة للطباعة حالياً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (tabReports.SelectedTab?.Tag?.ToString() == "DailyClosing")
			{
				PrintDailyClosing(dg);
				return;
			}
			PrintDocument printDocument = new PrintDocument();
			printDocument.PrintController = new StandardPrintController();
			AppConfig.SetPrinter(printDocument, AppConfig.A4PrinterName);
			if (dg.Columns.Count > 5)
			{
				printDocument.DefaultPageSettings.Landscape = true;
			}

			int pageRow = 0;
			int pageNum = 1;

			printDocument.PrintPage += delegate(object s, PrintPageEventArgs ev)
			{
				Graphics g = ev.Graphics;
				Font fComp  = new Font("Arial", 13f, FontStyle.Bold);
				Font fTitle = new Font("Arial", 15f, FontStyle.Bold);
				Font fHead  = new Font("Arial", 9.5f, FontStyle.Bold);
				Font fCell  = new Font("Arial", 8.5f, FontStyle.Regular);
				Font fCellB = new Font("Arial", 8.5f, FontStyle.Bold);
				Font fFoot  = new Font("Arial", 8f, FontStyle.Regular);

				var brushHeaderBg = new SolidBrush(Color.FromArgb(28, 45, 78));
				var brushRowAlt   = new SolidBrush(Color.FromArgb(245, 248, 253));
				var brushTotBg    = new SolidBrush(Color.FromArgb(220, 245, 225));
				var penGrid       = new Pen(Color.FromArgb(170, 185, 205), 1f);
				var penDark       = new Pen(Color.FromArgb(28, 45, 78), 1.5f);

				int pageW = printDocument.DefaultPageSettings.Landscape ? 1040 : 775;
				int startX = 25;
				int y = 25;

				// 1. Company & Report Title Header
				string companyName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
				SizeF szComp = g.MeasureString(companyName, fComp);
				g.DrawString(companyName, fComp, Brushes.DarkBlue, startX + (pageW - szComp.Width) / 2f, y);
				y += (int)szComp.Height + 3;

				string titleText = tabReports.SelectedTab?.Text ?? "تقرير";
				titleText = System.Text.RegularExpressions.Regex.Replace(titleText, @"\p{Cs}|\p{So}|\p{Sk}|\p{Cn}", "").Trim();
				SizeF szTitle = g.MeasureString(titleText, fTitle);
				g.DrawString(titleText, fTitle, Brushes.Black, startX + (pageW - szTitle.Width) / 2f, y);
				y += (int)szTitle.Height + 5;

				string dateInfo = (tabReports.SelectedTab?.Tag?.ToString() == "ClientBalances")
					? $"تاريخ التقرير: {DateTime.Now:dd/MM/yyyy HH:mm}"
					: $"من تاريخ: {dtpFrom.Value:dd/MM/yyyy}   إلى تاريخ: {dtpTo.Value:dd/MM/yyyy}";
				SizeF szDate = g.MeasureString(dateInfo, fFoot);
				g.DrawString(dateInfo, fFoot, Brushes.DarkGray, startX + (pageW - szDate.Width) / 2f, y);
				y += (int)szDate.Height + 6;

				g.DrawLine(penDark, startX, y, startX + pageW, y);
				y += 10;

				// Compute visible columns and intelligent widths
				var visCols = new List<DataGridViewColumn>();
				for (int k = 0; k < dg.Columns.Count; k++)
				{
					if (dg.Columns[k].Visible)
					{
						visCols.Add(dg.Columns[k]);
					}
				}

				int[] colWidths = new int[visCols.Count];
				if (visCols.Count > 0)
				{
					int nameColIndex = -1;
					int otherColsTotalW = 0;

					for (int i = 0; i < visCols.Count; i++)
					{
						string hText = visCols[i].HeaderText ?? "";
						string cName = visCols[i].Name ?? "";
						bool isNameCol = (hText == "الصنف" || hText == "اسم الصنف" || hText == "البيان" || cName == "ProductName" || cName == "ItemName" || cName == "Description");
						if (isNameCol && nameColIndex == -1)
						{
							nameColIndex = i;
							continue;
						}

						int stdW = 90;
						if (hText.Contains("الحالة") || hText.Contains("الكود") || hText.Contains("الوحدة")) stdW = 75;
						else if (hText.Contains("التاريخ") || hText.Contains("الرقم") || hText.Contains("السند")) stdW = 95;
						else if (hText.Contains("الكمية") || hText.Contains("المخزون")) stdW = 85;
						else if (hText.Contains("القيمة") || hText.Contains("المبيعات") || hText.Contains("المشتريات") || hText.Contains("الإجمالي") || hText.Contains("الرصيد")) stdW = 100;
						else if (hText.Contains("الشركة") || hText.Contains("المورد") || hText.Contains("العميل")) stdW = 130;

						if (!printDocument.DefaultPageSettings.Landscape)
						{
							stdW = (int)(stdW * 0.85f);
						}

						colWidths[i] = stdW;
						otherColsTotalW += stdW;
					}

					if (nameColIndex >= 0)
					{
						int remainW = pageW - otherColsTotalW;
						colWidths[nameColIndex] = Math.Max(remainW, 260);
					}
					else
					{
						int totalW = 0;
						for (int i = 0; i < visCols.Count; i++) totalW += visCols[i].Width;
						if (totalW <= 0) totalW = 1;
						int assigned = 0;
						for (int i = 0; i < visCols.Count; i++)
						{
							colWidths[i] = (visCols[i].Width * pageW) / totalW;
							if (colWidths[i] < 40) colWidths[i] = 40;
							assigned += colWidths[i];
						}
						if (assigned != pageW) colWidths[colWidths.Length - 1] += (pageW - assigned);
					}

					int totalAssigned = 0;
					for (int i = 0; i < colWidths.Length; i++) totalAssigned += colWidths[i];
					if (totalAssigned != pageW && colWidths.Length > 0)
					{
						if (nameColIndex >= 0 && colWidths[nameColIndex] + (pageW - totalAssigned) >= 150)
						{
							colWidths[nameColIndex] += (pageW - totalAssigned);
						}
						else
						{
							colWidths[0] += (pageW - totalAssigned);
						}
					}
				}

				int headH = 28;
				int rowH  = 25;

				// 2. Draw Table Header Row (RTL: right to left)
				int curX = startX + pageW;
				g.FillRectangle(brushHeaderBg, startX, y, pageW, headH);
				g.DrawRectangle(penDark, startX, y, pageW, headH);

				for (int i = 0; i < visCols.Count; i++)
				{
					int cw = colWidths[i];
					curX -= cw;
					var rect = new RectangleF(curX, y, cw, headH);
					g.DrawRectangle(penGrid, curX, y, cw, headH);

					var sf = new StringFormat
					{
						Alignment = StringAlignment.Center,
						LineAlignment = StringAlignment.Center,
						Trimming = StringTrimming.EllipsisCharacter,
						FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft
					};
					g.DrawString(visCols[i].HeaderText, fHead, Brushes.White, rect, sf);
				}
				y += headH;

				// 3. Draw Table Data Rows (RTL Grid with borders & alternating colors)
				while (pageRow < dg.Rows.Count)
				{
					DataGridViewRow dgRow = dg.Rows[pageRow];
					if (dgRow.IsNewRow) { pageRow++; continue; }

					string cell0Val = dgRow.Cells[0].Value?.ToString() ?? "";
					bool isTotalRow = cell0Val.Contains("الإجمالي") || cell0Val.Contains("المجموع");
					Font rowFont = isTotalRow ? fCellB : fCell;

					Brush bgBrush = isTotalRow ? brushTotBg 
								  : (pageRow % 2 == 1 ? brushRowAlt : Brushes.White);
					Brush textBrush = isTotalRow ? Brushes.DarkGreen : Brushes.Black;

					g.FillRectangle(bgBrush, startX, y, pageW, rowH);
					g.DrawRectangle(penGrid, startX, y, pageW, rowH);

					curX = startX + pageW;
					for (int j = 0; j < visCols.Count; j++)
					{
						int cw = colWidths[j];
						curX -= cw;
						var rect = new RectangleF(curX + 3, y + 1, cw - 6, rowH - 2);
						g.DrawRectangle(penGrid, curX, y, cw, rowH);

						string val = dgRow.Cells[visCols[j].Index].Value?.ToString() ?? "";
						string hText = visCols[j].HeaderText ?? "";
						bool isTextNameCol = (hText == "الصنف" || hText == "اسم الصنف" || hText == "البيان" || hText == "اسم العميل" || hText == "المورد" || hText == "ملاحظات" || hText == "الشركة المنتجة");
						
						var sf = new StringFormat
						{
							Alignment = isTextNameCol ? StringAlignment.Near : StringAlignment.Center,
							LineAlignment = StringAlignment.Center,
							Trimming = StringTrimming.EllipsisCharacter,
							FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft
						};

						g.DrawString(val, rowFont, textBrush, rect, sf);
					}

					y += rowH;
					pageRow++;

					if (y > ev.PageBounds.Height - 65)
					{
						g.DrawString($"صفحة {pageNum}", fFoot, Brushes.Gray, startX + pageW - 60, ev.PageBounds.Height - 35);
						g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", fFoot, Brushes.Gray, startX, ev.PageBounds.Height - 35);
						pageNum++;
						ev.HasMorePages = true;
						return;
					}
				}

				g.DrawLine(penGrid, startX, y + 5, startX + pageW, y + 5);
				g.DrawString($"صفحة {pageNum}", fFoot, Brushes.Gray, startX + pageW - 60, ev.PageBounds.Height - 35);
				g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", fFoot, Brushes.Gray, startX, ev.PageBounds.Height - 35);

				pageRow = 0;
			};

			PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
			printPreviewDialog.Document = printDocument;
			printPreviewDialog.Width = 1150;
			printPreviewDialog.Height = 800;
			printPreviewDialog.ShowDialog();
		}

		private void LoadDailyClosingReport(DataGridView dg, int? warehouseID)
		{
			try
			{
				DateTime date = dtpFrom.Value.Date;

				var products = ProductDAL.GetAll(activeOnly: true);
				int productCount = products.Rows.Count;

				var dtQty = ReportDAL.GetDailyClientProductSales(date, warehouseID);
				var dtTotals = ReportDAL.GetDailyClientTotals(date, warehouseID);

				var qtyMap = new Dictionary<int, Dictionary<int, decimal>>();
				foreach (DataRow r in dtQty.Rows)
				{
					int cid = Convert.ToInt32(r["ClientID"]);
					int pid = Convert.ToInt32(r["ProductID"]);
					decimal q = Convert.ToDecimal(r["TotalQty"]);
					if (!qtyMap.ContainsKey(cid)) qtyMap[cid] = new Dictionary<int, decimal>();
					qtyMap[cid][pid] = q;
				}

				var totMap = new Dictionary<int, (string name, decimal inv, decimal pay, decimal bal)>();
				var clientOrder = new List<int>();
				foreach (DataRow r in dtTotals.Rows)
				{
					int cid = Convert.ToInt32(r["ClientID"]);
					totMap[cid] = (
						r["ClientName"].ToString(),
						Convert.ToDecimal(r["TotalInvoice"]),
						Convert.ToDecimal(r["LastPayment"]),
						Convert.ToDecimal(r["Balance"])
					);
					if (!clientOrder.Contains(cid)) clientOrder.Add(cid);
				}
				foreach (int cid in qtyMap.Keys)
					if (!clientOrder.Contains(cid)) clientOrder.Add(cid);

				dg.Columns.Clear();
				dg.Rows.Clear();

				dg.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = "ClientName",
					HeaderText = "اسم العميل",
					MinimumWidth = 140,
					DefaultCellStyle = new DataGridViewCellStyle
					{
						Alignment = DataGridViewContentAlignment.MiddleRight,
						Font = new Font("Segoe UI", 10f, FontStyle.Bold)
					}
				});

				foreach (DataRow pr in products.Rows)
				{
					dg.Columns.Add(new DataGridViewTextBoxColumn
					{
						Name = "P_" + pr["ProductID"],
						HeaderText = pr["ProductName"].ToString(),
						MinimumWidth = 68,
						Tag = pr
					});
				}

				dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalInvoice", HeaderText = "إجمالي الفاتورة", MinimumWidth = 100,
					DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.Accent, Alignment = DataGridViewContentAlignment.MiddleCenter } });
				dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastPayment", HeaderText = "آخر توريد", MinimumWidth = 100,
					DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.Success, Alignment = DataGridViewContentAlignment.MiddleCenter } });
				dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "المديونية", MinimumWidth = 100,
					DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(231, 76, 60), Alignment = DataGridViewContentAlignment.MiddleCenter } });

				int totalCols = dg.Columns.Count;

				var priceVals = new object[totalCols];
				priceVals[0] = "السعر";
				for (int i = 0; i < productCount; i++)
				{
					decimal price = Convert.ToDecimal(products.Rows[i]["SalePrice"]);
					priceVals[i + 1] = price > 0 ? price.ToString("N2") : "-";
				}
				int priceRowIdx = dg.Rows.Add(priceVals);
				StyleDailySpecialRow(dg.Rows[priceRowIdx], Color.FromArgb(26, 43, 90), Color.FromArgb(243, 156, 18), new Font("Segoe UI", 9.5f, FontStyle.Bold));

				decimal grandInvoice = 0m, grandPayment = 0m, grandBalance = 0m;
				bool alternate = false;

				foreach (int cid in clientOrder)
				{
					var row = new object[totalCols];
					string clientName = totMap.ContainsKey(cid) ? totMap[cid].name : "عميل";
					row[0] = clientName;

					for (int i = 0; i < productCount; i++)
					{
						int pid = Convert.ToInt32(products.Rows[i]["ProductID"]);
						decimal qty = 0;
						if (qtyMap.ContainsKey(cid) && qtyMap[cid].ContainsKey(pid))
							qty = qtyMap[cid][pid];
						row[i + 1] = qty != 0 ? qty.ToString("N0") : "";
					}

					decimal inv = 0, pay = 0, bal = 0;
					if (totMap.ContainsKey(cid))
						(_, inv, pay, bal) = totMap[cid];

					row[productCount + 1] = inv.ToString("N2");
					row[productCount + 2] = pay.ToString("N2");
					row[productCount + 3] = bal.ToString("N2");

					grandInvoice += inv;
					grandPayment += pay;
					grandBalance += bal;

					int ri = dg.Rows.Add(row);
					dg.Rows[ri].DefaultCellStyle.BackColor = alternate ? Color.FromArgb(40, 48, 65) : Theme.BgCard;
					alternate = !alternate;
				}

				var totVals = new object[totalCols];
				totVals[0] = "الإجمالي الكلي";
				for (int i = 1; i <= productCount; i++) totVals[i] = "";
				totVals[productCount + 1] = grandInvoice.ToString("N2");
				totVals[productCount + 2] = grandPayment.ToString("N2");
				totVals[productCount + 3] = grandBalance.ToString("N2");

				int totRowIdx = dg.Rows.Add(totVals);
				StyleDailySpecialRow(dg.Rows[totRowIdx], Color.FromArgb(30, 41, 59), Color.FromArgb(245, 158, 11), new Font("Segoe UI", 10.5f, FontStyle.Bold));
			}
			catch (Exception ex)
			{
				MessageBox.Show("حدث خطأ أثناء تحميل تقرير التقفيل اليومي:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static void StyleDailySpecialRow(DataGridViewRow row, Color bg, Color fg, Font font)
		{
			row.DefaultCellStyle.BackColor = bg;
			row.DefaultCellStyle.ForeColor = fg;
			row.DefaultCellStyle.Font = font;
			row.DefaultCellStyle.SelectionBackColor = bg;
			row.DefaultCellStyle.SelectionForeColor = fg;
		}

		private static string GetInstalledPdfPrinter()
		{
			foreach (string p in PrinterSettings.InstalledPrinters)
			{
				if (p.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0)
					return p;
			}
			return null;
		}

		private void PrintDailyClosing(DataGridView dg, string pdfFilePath = null)
		{
			var pd = new PrintDocument();
			pd.PrintController = new StandardPrintController();
			if (!string.IsNullOrEmpty(pdfFilePath))
			{
				string pdfPrinter = GetInstalledPdfPrinter();
				if (!string.IsNullOrEmpty(pdfPrinter))
				{
					pd.PrinterSettings.PrinterName = pdfPrinter;
					pd.PrinterSettings.PrintToFile = true;
					pd.PrinterSettings.PrintFileName = pdfFilePath;
				}
			}
			else
			{
				AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
			}
			pd.DefaultPageSettings.Landscape = true;
			pd.DefaultPageSettings.Margins = new Margins(30, 30, 40, 40);

			int pageRow = 0;
			pd.PrintPage += (s, ev) =>
			{
				var g = ev.Graphics;

				var fTitle = new Font("Arial", 13f, FontStyle.Bold);
				var fSub   = new Font("Arial", 7.5f);
				var fHead  = new Font("Arial", 7.5f, FontStyle.Bold);
				var fCell  = new Font("Arial", 7f);
				var fTotal = new Font("Arial", 8f, FontStyle.Bold);

				int mL = 20;
				int mR = 1070;
				int mT = 40;
				int mB = 780;
				int pgW = mR - mL;

				int y = mT;

				if (pageRow == 0)
				{
					string title = $"تقرير التقفيل اليومي  –  {dtpFrom.Value:dd/MM/yyyy}";
					SizeF tsz = g.MeasureString(title, fTitle);
					g.DrawString(title, fTitle, Brushes.DarkBlue,
						mL + (pgW - tsz.Width) / 2f, y);
					y += (int)tsz.Height + 4;

					string sub = $"إجمالي فواتير اليوم: {dg.Rows.Count - 2}  |  عدد الأصناف: {dg.Columns.Count - 4}";
					SizeF ssz = g.MeasureString(sub, fSub);
					g.DrawString(sub, fSub, Brushes.DimGray,
						mL + (pgW - ssz.Width) / 2f, y);
					y += (int)ssz.Height + 4;

					g.DrawLine(new Pen(Color.DarkBlue, 1.5f), mL, y, mR, y);
					y += 6;
				}

				int visColCount = dg.Columns.GetColumnCount(DataGridViewElementStates.Visible);
				int[] widths = ComputeDailyPrintWidths(pgW, visColCount, dg);

				int totalW = 0;
				foreach (int w in widths) totalW += w;
				if (totalW != pgW && widths.Length > 0 && widths[widths.Length - 1] + (pgW - totalW) > 0)
					widths[widths.Length - 1] += pgW - totalW;

				const int HEAD_H = 22;
				const int ROW_H  = 18;

				{
					int cx = mR;
					foreach (DataGridViewColumn col in dg.Columns)
					{
						if (!col.Visible) continue;
						int idx = GetDailyVisColIndex(col, dg);
						int cw  = widths[idx];
						cx -= cw;
						var rect = new RectangleF(cx, y, cw, HEAD_H);
						g.FillRectangle(new SolidBrush(Color.FromArgb(26, 43, 90)), rect);
						var sf = new StringFormat
						{
							Alignment     = StringAlignment.Center,
							LineAlignment = StringAlignment.Center,
							Trimming      = StringTrimming.EllipsisCharacter,
							FormatFlags   = StringFormatFlags.DirectionRightToLeft
						};
						g.DrawString(col.HeaderText, fHead, Brushes.White, rect, sf);
					}
					y += HEAD_H + 2;
				}

				while (pageRow < dg.Rows.Count)
				{
					var dgRow  = dg.Rows[pageRow];
					bool isPrice = pageRow == 0;
					bool isTotal = dgRow.Cells[0].Value?.ToString() == "الإجمالي الكلي";

					var rowFont  = isTotal || isPrice ? fTotal : fCell;
					var rowBg    = isPrice  ? Color.FromArgb(220, 230, 245)
								 : isTotal  ? Color.FromArgb(220, 245, 220)
								 : (pageRow % 2 == 0) ? Color.White : Color.FromArgb(245, 245, 250);
					var rowFg    = isTotal ? Color.DarkGreen : Color.Black;

					int cx = mR;
					foreach (DataGridViewColumn col in dg.Columns)
					{
						if (!col.Visible) continue;
						int idx = GetDailyVisColIndex(col, dg);
						int cw  = widths[idx];
						string v = dgRow.Cells[col.Name].Value?.ToString() ?? "";
						cx -= cw;
						var rect = new RectangleF(cx, y, cw, ROW_H);
						g.FillRectangle(new SolidBrush(rowBg), rect);
						var sf = new StringFormat
						{
							Alignment     = StringAlignment.Center,
							LineAlignment = StringAlignment.Center,
							Trimming      = StringTrimming.EllipsisCharacter,
							FormatFlags   = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft
						};
						g.DrawString(v, rowFont, new SolidBrush(rowFg), rect, sf);
					}

					g.DrawLine(Pens.LightGray, mL, y + ROW_H, mR, y + ROW_H);
					y += ROW_H;
					pageRow++;

					if (y > mB - ROW_H * 2)
					{
						ev.HasMorePages = true;
						return;
					}
				}

				g.DrawLine(new Pen(Color.Gray, 1f), mL, y + 6, mR, y + 6);
				g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}",
					fCell, Brushes.Gray, mL, y + 10);

				pageRow = 0;
			};

			var preview = new PrintPreviewDialog
			{
				Document = pd,
				Width    = 1150,
				Height   = 820
			};
			preview.ShowDialog();
		}

		private int[] ComputeDailyPrintWidths(int pgW, int colCount, DataGridView dg)
		{
			int clientW = (int)(pgW * 0.15);
			int extraW  = (int)(pgW * 0.09);
			int reservedW = clientW + extraW * 3;
			int prodCount = colCount - 4;
			int prodW = prodCount > 0 ? (pgW - reservedW) / prodCount : 60;
			
			if (prodW < 15) prodW = 15;

			var ws = new int[colCount];
			int vi = 0;
			foreach (DataGridViewColumn col in dg.Columns)
			{
				if (!col.Visible) continue;
				if      (col.Name == "ClientName")    ws[vi] = clientW;
				else if (col.Name == "TotalInvoice" ||
				         col.Name == "LastPayment"   ||
				         col.Name == "Balance")       ws[vi] = extraW;
				else                                  ws[vi] = prodW;
				vi++;
			}
			return ws;
		}

		private int GetDailyVisColIndex(DataGridViewColumn col, DataGridView dg)
		{
			int idx = 0;
			foreach (DataGridViewColumn c in dg.Columns)
			{
				if (!c.Visible) continue;
				if (c.Name == col.Name) return idx;
				idx++;
			}
			return 0;
		}

		private void BtnWhatsAppReport_Click(object sender, EventArgs e)
		{
			DataGridView dataGridView = tabReports.SelectedTab?.Controls.OfType<DataGridView>().FirstOrDefault();
			if (dataGridView == null || dataGridView.Rows.Count == 0)
			{
				MessageBox.Show("لا توجد بيانات للإرسال.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			var sb = new System.Text.StringBuilder();
			sb.AppendLine("📋 *تقرير أرصدة وبيانات العملاء*");
			sb.AppendLine($"🏢 {AppConfig.CompanyName}");
			sb.AppendLine($"📅 التاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}");
			sb.AppendLine("──────────────────────");

			decimal grandTotalBalance = 0;
			foreach (DataGridViewRow row in dataGridView.Rows)
			{
				if (row.Cells["ClientName"].Value == null) continue;
				string clientName = row.Cells["ClientName"].Value.ToString();
				string clientCode = row.Cells["ClientCode"].Value?.ToString() ?? "";
				string phoneNum = row.Cells["Phone"].Value?.ToString() ?? "";
				decimal balance = 0;
				if (row.Cells["Balance"].Value != null && row.Cells["Balance"].Value != DBNull.Value)
				{
					balance = Convert.ToDecimal(row.Cells["Balance"].Value);
				}

				grandTotalBalance += balance;

				sb.AppendLine($"• {clientName} (كود: {clientCode})");
				sb.AppendLine($"  الهاتف: {phoneNum} | المديونية: {balance:N2} ج.م");
			}

			sb.AppendLine("──────────────────────");
			sb.AppendLine($"📊 إجمالي مديونيات العملاء: {grandTotalBalance:N2} ج.م");
			sb.AppendLine("──────────────────────");

			WhatsAppSender.ShowWhatsAppSendOptionsDialog(
				this,
				"",
				sb.ToString(),
				() => ReceiptImageGenerator.GenerateTextCardImage("تقرير أرصدة العملاء", sb.ToString()),
				"📱 إرسال تقرير أرصدة العملاء عبر الواتساب");
		}

		private static void SendWhatsApp(string phone, string message)
		{
			try
			{
				string clean = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
				if (clean.StartsWith("0")) clean = "20" + clean.Substring(1);
				
				string encoded = "";
				if (message.Length > 600 || Uri.EscapeDataString(message).Length > 1800)
				{
					Clipboard.SetText(message);
					MessageBox.Show(
						"⚠️ نظراً لأن التقرير طويل جداً، تم نسخه بالكامل إلى الحافظة (Clipboard) تلقائياً.\n" +
						"يرجى الضغط على لصق (Ctrl + V) داخل محادثة الواتساب التي ستفتح الآن لإرساله.",
						"تم نسخ التقرير", MessageBoxButtons.OK, MessageBoxIcon.Information,
						MessageBoxDefaultButton.Button1,
						MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
						
					encoded = Uri.EscapeDataString("📋 تقرير أرصدة العملاء (تم نسخ التفاصيل للحافظة، يرجى اللصق وإرسال)");
				}
				else
				{
					encoded = Uri.EscapeDataString(message);
				}
				
				// 1. Try to open the WhatsApp Desktop App protocol
				string appUrl = $"whatsapp://send?phone={clean}&text={encoded}";
				try
				{
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(appUrl) { UseShellExecute = true });
					return;
				}
				catch { }

				// 2. Try to open wa.me link directly via shell
				string waUrl = $"https://wa.me/{clean}?text={encoded}";
				try
				{
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(waUrl) { UseShellExecute = true });
					return;
				}
				catch { }

				// 3. Fallback: Launch via explorer.exe (highly robust in Windows)
				try
				{
					System.Diagnostics.Process.Start("explorer.exe", $"\"{waUrl}\"");
					return;
				}
				catch { }

				// 4. Try WhatsApp Web as a last resort via explorer.exe
				string webUrl = $"https://web.whatsapp.com/send?phone={clean}&text={encoded}";
				System.Diagnostics.Process.Start("explorer.exe", $"\"{webUrl}\"");
			}
			catch (Exception ex)
			{
				MessageBox.Show("تعذر فتح واتساب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LoadWarehouses()
		{
			try
			{
				DataTable dt = WarehouseDAL.GetAll(activeOnly: true);
				cboWarehouse.Items.Clear();
				cboWarehouse.Items.Add(new ComboItem(0, "كل المخازن"));
				foreach (DataRow r in dt.Rows)
				{
					cboWarehouse.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
				}
				cboWarehouse.DisplayMember = "Text";
				cboWarehouse.SelectedIndex = 0;
				
				cboWarehouse.SelectedIndexChanged += delegate
				{
					LoadCurrentTab();
				};
			}
			catch (Exception ex)
			{
				MessageBox.Show("حدث خطأ أثناء تحميل المخازن:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnExportPdf_Click(object sender, EventArgs e)
		{
			DataGridView dataGridView = GetActiveGrid();
			if (dataGridView == null || dataGridView.Rows.Count == 0)
			{
				MessageBox.Show("لا توجد بيانات لتصديرها إلى PDF.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
					MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
				return;
			}

			string tabText = tabReports.SelectedTab?.Text ?? "تقرير";
			string cleanName = System.Text.RegularExpressions.Regex.Replace(tabText, @"[^\w\s\-\u0600-\u06FF]", "").Trim();
			string defaultFileName = $"{cleanName}_{DateTime.Now:yyyy_MM_dd}.pdf";

			ExportToPdf(dataGridView, defaultFileName);
		}

		private void ExportToPdf(DataGridView dg, string defaultFileName)
		{
			using (var dlg = new SaveFileDialog())
			{
				dlg.Title = "تصدير التقرير إلى PDF";
				dlg.FileName = defaultFileName;
				dlg.Filter = "ملفات PDF (*.pdf)|*.pdf|كل الملفات (*.*)|*.*";
				dlg.DefaultExt = "pdf";
				if (dlg.ShowDialog(this) != DialogResult.OK) return;

				string filePath = dlg.FileName;

				try
				{
					if (tabReports.SelectedTab?.Tag?.ToString() == "DailyClosing")
					{
						PrintDailyClosing(dg, filePath);
					}
					else
					{
						string pdfPrinter = GetInstalledPdfPrinter();
						PrintDocument printDocument = new PrintDocument();
						printDocument.PrintController = new StandardPrintController();
						if (!string.IsNullOrEmpty(pdfPrinter))
						{
							printDocument.PrinterSettings.PrinterName = pdfPrinter;
							printDocument.PrinterSettings.PrintToFile = true;
							printDocument.PrinterSettings.PrintFileName = filePath;
						}

						if (dg.Columns.Count > 5)
						{
							printDocument.DefaultPageSettings.Landscape = true;
						}

						int pageRow = 0;
						int pageNum = 1;

						printDocument.PrintPage += delegate(object s, PrintPageEventArgs ev)
						{
							Graphics g = ev.Graphics;
							Font fComp  = new Font("Arial", 13f, FontStyle.Bold);
							Font fTitle = new Font("Arial", 15f, FontStyle.Bold);
							Font fHead  = new Font("Arial", 9.5f, FontStyle.Bold);
							Font fCell  = new Font("Arial", 8.5f, FontStyle.Regular);
							Font fCellB = new Font("Arial", 8.5f, FontStyle.Bold);
							Font fFoot  = new Font("Arial", 8f, FontStyle.Regular);

							var brushHeaderBg = new SolidBrush(Color.FromArgb(28, 45, 78));
							var brushRowAlt   = new SolidBrush(Color.FromArgb(245, 248, 253));
							var brushTotBg    = new SolidBrush(Color.FromArgb(220, 245, 225));
							var penGrid       = new Pen(Color.FromArgb(170, 185, 205), 1f);
							var penDark       = new Pen(Color.FromArgb(28, 45, 78), 1.5f);

							int pageW = printDocument.DefaultPageSettings.Landscape ? 1040 : 775;
							int startX = 25;
							int y = 25;

							// 1. Header
							string companyName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
							SizeF szComp = g.MeasureString(companyName, fComp);
							g.DrawString(companyName, fComp, Brushes.DarkBlue, startX + (pageW - szComp.Width) / 2f, y);
							y += (int)szComp.Height + 3;

							string titleText = tabReports.SelectedTab?.Text ?? "تقرير";
							titleText = System.Text.RegularExpressions.Regex.Replace(titleText, @"\p{Cs}|\p{So}|\p{Sk}|\p{Cn}", "").Trim();
							SizeF szTitle = g.MeasureString(titleText, fTitle);
							g.DrawString(titleText, fTitle, Brushes.Black, startX + (pageW - szTitle.Width) / 2f, y);
							y += (int)szTitle.Height + 5;

							string dateInfo = (tabReports.SelectedTab?.Tag?.ToString() == "ClientBalances")
								? $"تاريخ التقرير: {DateTime.Now:dd/MM/yyyy HH:mm}"
								: $"من تاريخ: {dtpFrom.Value:dd/MM/yyyy}   إلى تاريخ: {dtpTo.Value:dd/MM/yyyy}";
							SizeF szDate = g.MeasureString(dateInfo, fFoot);
							g.DrawString(dateInfo, fFoot, Brushes.DarkGray, startX + (pageW - szDate.Width) / 2f, y);
							y += (int)szDate.Height + 6;

							g.DrawLine(penDark, startX, y, startX + pageW, y);
							y += 10;

							// Visible Columns
							var visCols = new List<DataGridViewColumn>();
							for (int k = 0; k < dg.Columns.Count; k++)
							{
								if (dg.Columns[k].Visible) visCols.Add(dg.Columns[k]);
							}

							int[] colWidths = new int[visCols.Count];
							if (visCols.Count > 0)
							{
								int totalW = 0;
								for (int i = 0; i < visCols.Count; i++) totalW += visCols[i].Width;
								if (totalW <= 0) totalW = 1;
								int assigned = 0;
								for (int i = 0; i < visCols.Count; i++)
								{
									colWidths[i] = (visCols[i].Width * pageW) / totalW;
									if (colWidths[i] < 40) colWidths[i] = 40;
									assigned += colWidths[i];
								}
								if (assigned != pageW) colWidths[colWidths.Length - 1] += (pageW - assigned);
							}

							int headH = 28;
							int rowH  = 25;

							// Table Header Row
							int curX = startX + pageW;
							g.FillRectangle(brushHeaderBg, startX, y, pageW, headH);
							g.DrawRectangle(penDark, startX, y, pageW, headH);

							for (int i = 0; i < visCols.Count; i++)
							{
								int cw = colWidths[i];
								curX -= cw;
								var rect = new RectangleF(curX, y, cw, headH);
								g.DrawRectangle(penGrid, curX, y, cw, headH);

								var sf = new StringFormat
								{
									Alignment = StringAlignment.Center,
									LineAlignment = StringAlignment.Center,
									Trimming = StringTrimming.EllipsisCharacter,
									FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft
								};
								g.DrawString(visCols[i].HeaderText, fHead, Brushes.White, rect, sf);
							}
							y += headH;

							// Data Rows
							int maxY = printDocument.DefaultPageSettings.Landscape ? 700 : 980;
							while (pageRow < dg.Rows.Count)
							{
								if (y + rowH > maxY)
								{
									ev.HasMorePages = true;
									pageNum++;
									return;
								}

								var row = dg.Rows[pageRow];
								bool isSummary = (pageRow == dg.Rows.Count - 1 && row.Cells[0].Value?.ToString()?.Contains("الإجمالي") == true);
								Brush rowBg = isSummary ? brushTotBg : ((pageRow % 2 == 1) ? brushRowAlt : Brushes.White);
								Font cellFont = isSummary ? fCellB : fCell;

								g.FillRectangle(rowBg, startX, y, pageW, rowH);
								g.DrawRectangle(penGrid, startX, y, pageW, rowH);

								int rx = startX + pageW;
								for (int i = 0; i < visCols.Count; i++)
								{
									int cw = colWidths[i];
									rx -= cw;
									var cellRect = new RectangleF(rx, y, cw, rowH);
									g.DrawRectangle(penGrid, rx, y, cw, rowH);

									string cVal = row.Cells[visCols[i].Index].Value?.ToString() ?? "";
									var sf = new StringFormat
									{
										Alignment = StringAlignment.Center,
										LineAlignment = StringAlignment.Center,
										Trimming = StringTrimming.EllipsisCharacter,
										FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft
									};
									g.DrawString(cVal, cellFont, Brushes.Black, cellRect, sf);
								}

								y += rowH;
								pageRow++;
							}

							// Footer
							string foot = $"صفحة {pageNum}  -  طُبع بواسطة نظام برو سوفت  {DateTime.Now:yyyy/MM/dd HH:mm}";
							SizeF szF = g.MeasureString(foot, fFoot);
							g.DrawString(foot, fFoot, Brushes.Gray, startX + (pageW - szF.Width) / 2f, maxY + 5);

							ev.HasMorePages = false;
						};

						printDocument.Print();
					}

					var result = MessageBox.Show(
						"✅ تم تصدير التقرير إلى PDF بنجاح!\n\nهل تريد فتح الملف الآن؟",
						"تم التصدير بنجاح", MessageBoxButtons.YesNo, MessageBoxIcon.Information,
						MessageBoxDefaultButton.Button1,
						MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

					if (result == DialogResult.Yes)
					{
						System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
					}
				}
				catch (Exception ex)
				{
					AppLogger.Error("FrmReports.ExportToPdf", ex);
					MessageBox.Show("❌ فشل تصدير ملف PDF:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void BtnExportExcel_Click(object sender, EventArgs e)
		{
			DataGridView dataGridView = GetActiveGrid();
			if (dataGridView == null || dataGridView.Rows.Count == 0)
			{
				MessageBox.Show("لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
					MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
				return;
			}

			string tabText = tabReports.SelectedTab?.Text ?? "تقرير";
			string cleanName = System.Text.RegularExpressions.Regex.Replace(tabText, @"[^\w\s\-\u0600-\u06FF]", "").Trim();
			string defaultFileName = $"{cleanName}_{DateTime.Now:yyyy_MM_dd}.xls";

			ExportToExcel(dataGridView, defaultFileName);
		}

		private void ExportToExcel(DataGridView dgv, string defaultFileName)
		{
			using (var dlg = new SaveFileDialog())
			{
				dlg.Title = "تصدير التقرير إلى Excel";
				dlg.FileName = defaultFileName;
				dlg.Filter = "Excel Files (*.xls)|*.xls|All Files (*.*)|*.*";
				dlg.DefaultExt = "xls";
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					try
					{
						ExportDataGridViewToXls(dgv, dlg.FileName,
							tabReports.SelectedTab?.Text ?? "تقرير",
							AppConfig.CompanyName ?? "التقرير");

						var result = MessageBox.Show(
							"✅ تم تصدير التقرير بنجاح!\nهل تريد فتح الملف الآن؟",
							"تم التصدير", MessageBoxButtons.YesNo, MessageBoxIcon.Information,
							MessageBoxDefaultButton.Button1,
							MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

						if (result == DialogResult.Yes)
							System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
					}
					catch (Exception ex)
					{
						MessageBox.Show("❌ فشل تصدير الملف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		/// <summary>
		/// Exports a DataGridView to a properly-formatted Excel SpreadsheetML (.xls) file.
		/// Handles Arabic text correctly, no external library required.
		/// </summary>
		public static void ExportDataGridViewToXls(DataGridView dgv, string filePath, string sheetTitle = "", string companyName = "")
		{
			var xml = new System.Text.StringBuilder();
			xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			xml.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
			xml.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
			xml.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
			xml.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">");

			// ── Styles ──────────────────────────────────────────────────────
			xml.AppendLine("<Styles>");

			// Default
			xml.AppendLine("<Style ss:ID=\"Default\"><Alignment ss:Horizontal=\"Right\" ss:ReadingOrder=\"RightToLeft\"/></Style>");

			// Title row
			xml.AppendLine("<Style ss:ID=\"Title\">");
			xml.AppendLine(" <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\"/>");
			xml.AppendLine(" <Font ss:Bold=\"1\" ss:Size=\"14\" ss:Color=\"#1E3A5F\"/>");
			xml.AppendLine(" <Interior ss:Color=\"#D6E4F0\" ss:Pattern=\"Solid\"/>");
			xml.AppendLine(" <Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"2\" ss:Color=\"#1E3A5F\"/></Borders>");
			xml.AppendLine("</Style>");

			// Header row
			xml.AppendLine("<Style ss:ID=\"Header\">");
			xml.AppendLine(" <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\" ss:WrapText=\"1\"/>");
			xml.AppendLine(" <Font ss:Bold=\"1\" ss:Size=\"11\" ss:Color=\"#FFFFFF\"/>");
			xml.AppendLine(" <Interior ss:Color=\"#1E5799\" ss:Pattern=\"Solid\"/>");
			xml.AppendLine(" <Borders>");
			xml.AppendLine("  <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#AAAAAA\"/>");
			xml.AppendLine("  <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#AAAAAA\"/>");
			xml.AppendLine("  <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#AAAAAA\"/>");
			xml.AppendLine(" </Borders>");
			xml.AppendLine("</Style>");

			// Even row
			xml.AppendLine("<Style ss:ID=\"Even\">");
			xml.AppendLine(" <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\"/>");
			xml.AppendLine(" <Interior ss:Color=\"#F4F8FF\" ss:Pattern=\"Solid\"/>");
			xml.AppendLine(" <Borders>");
			xml.AppendLine("  <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine("  <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine("  <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine(" </Borders>");
			xml.AppendLine("</Style>");

			// Odd row
			xml.AppendLine("<Style ss:ID=\"Odd\">");
			xml.AppendLine(" <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\"/>");
			xml.AppendLine(" <Interior ss:Color=\"#FFFFFF\" ss:Pattern=\"Solid\"/>");
			xml.AppendLine(" <Borders>");
			xml.AppendLine("  <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine("  <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine("  <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine(" </Borders>");
			xml.AppendLine("</Style>");

			// Total/Footer row
			xml.AppendLine("<Style ss:ID=\"Total\">");
			xml.AppendLine(" <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\"/>");
			xml.AppendLine(" <Font ss:Bold=\"1\" ss:Size=\"11\" ss:Color=\"#1E3A5F\"/>");
			xml.AppendLine(" <Interior ss:Color=\"#FFF3CD\" ss:Pattern=\"Solid\"/>");
			xml.AppendLine(" <Borders>");
			xml.AppendLine("  <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"2\" ss:Color=\"#1E3A5F\"/>");
			xml.AppendLine("  <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#AAAAAA\"/>");
			xml.AppendLine("  <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#AAAAAA\"/>");
			xml.AppendLine("  <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#AAAAAA\"/>");
			xml.AppendLine(" </Borders>");
			xml.AppendLine("</Style>");

			// Number cell
			xml.AppendLine("<Style ss:ID=\"Num\">");
			xml.AppendLine(" <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\"/>");
			xml.AppendLine(" <Interior ss:Color=\"#FFFFFF\" ss:Pattern=\"Solid\"/>");
			xml.AppendLine(" <Borders>");
			xml.AppendLine("  <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine("  <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine("  <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D0D8E8\"/>");
			xml.AppendLine(" </Borders>");
			xml.AppendLine("</Style>");

			xml.AppendLine("</Styles>");

			// ── Worksheet ───────────────────────────────────────────────────
			string safeTitle = string.IsNullOrWhiteSpace(sheetTitle) ? "تقرير" : sheetTitle;
			string safeTitleAttr = safeTitle.Replace("\"", "&quot;").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
			// Sheet name must be ≤31 chars
			string sheetName = safeTitleAttr.Length > 31 ? safeTitleAttr.Substring(0, 31) : safeTitleAttr;
			xml.AppendLine($"<Worksheet ss:Name=\"{sheetName}\">");

			// Collect visible columns
			var visibleCols = new List<DataGridViewColumn>();
			foreach (DataGridViewColumn col in dgv.Columns)
				if (col.Visible) visibleCols.Add(col);

			int colCount = visibleCols.Count;

			xml.AppendLine("<Table ss:DefaultRowHeight=\"20\">");

			// Column widths (approximate)
			foreach (var col in visibleCols)
			{
				int w = Math.Max(col.Width, 60);
				// Convert pixel width to points (roughly 0.75)
				double pts = w * 0.75;
				xml.AppendLine($"<Column ss:Width=\"{pts:0.0}\"/>");
			}

			// ── Row 1: Company / Title ──────────────────────────────────────
			xml.AppendLine("<Row ss:Height=\"28\">");
			string titleText = (!string.IsNullOrWhiteSpace(companyName) ? companyName + " - " : "") + safeTitle + " | " + DateTime.Now.ToString("yyyy/MM/dd");
			xml.AppendLine($"<Cell ss:MergeAcross=\"{colCount - 1}\" ss:StyleID=\"Title\"><Data ss:Type=\"String\">{EscapeXml(titleText)}</Data></Cell>");
			xml.AppendLine("</Row>");

			// ── Row 2: Headers ──────────────────────────────────────────────
			xml.AppendLine("<Row ss:Height=\"26\">");
			foreach (var col in visibleCols)
			{
				string hdr = EscapeXml(col.HeaderText ?? "");
				xml.AppendLine($"<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">{hdr}</Data></Cell>");
			}
			xml.AppendLine("</Row>");

			// ── Data rows ───────────────────────────────────────────────────
			int rowIndex = 0;
			foreach (DataGridViewRow row in dgv.Rows)
			{
				if (row.IsNewRow) continue;

				// Detect total/summary row
				bool isTotalRow = false;
				if (visibleCols.Count > 0)
				{
					var firstCell = row.Cells[visibleCols[0].Index]?.Value?.ToString() ?? "";
					if (firstCell.Contains("إجمالي") || firstCell.Contains("المجموع") || firstCell.Contains("الكلي"))
						isTotalRow = true;
				}

				string rowStyle = isTotalRow ? "Total" : (rowIndex % 2 == 0 ? "Even" : "Odd");
				xml.AppendLine("<Row ss:Height=\"20\">");

				foreach (var col in visibleCols)
				{
					var rawVal = row.Cells[col.Index].Value;
					string valStr = rawVal?.ToString() ?? "";

					// Determine data type
					bool isNumeric = false;
					double numVal = 0;
					if (rawVal != null && !(rawVal is string) && double.TryParse(valStr, System.Globalization.NumberStyles.Any,
						System.Globalization.CultureInfo.InvariantCulture, out numVal))
					{
						isNumeric = true;
					}
					else if (!string.IsNullOrWhiteSpace(valStr))
					{
						// Try parsing Arabic number strings (e.g. "1,234.56" or "1234.56")
						string cleaned = valStr.Replace(",", "");
						if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any,
							System.Globalization.CultureInfo.InvariantCulture, out numVal) &&
							!valStr.Any(c => char.IsLetter(c) && c < 128) && // no ASCII letters
							!valStr.StartsWith("0") && cleaned.Length > 0)
						{
							isNumeric = true;
						}
					}

					if (isNumeric)
					{
						string numStyle = isTotalRow ? "Total" : "Num";
						xml.AppendLine($"<Cell ss:StyleID=\"{numStyle}\"><Data ss:Type=\"Number\">{numVal.ToString(System.Globalization.CultureInfo.InvariantCulture)}</Data></Cell>");
					}
					else
					{
						xml.AppendLine($"<Cell ss:StyleID=\"{rowStyle}\"><Data ss:Type=\"String\">{EscapeXml(valStr)}</Data></Cell>");
					}
				}

				xml.AppendLine("</Row>");
				rowIndex++;
			}

			xml.AppendLine("</Table>");

			// Worksheet options - RTL, freeze header rows, auto-filter
			xml.AppendLine("<WorksheetOptions xmlns=\"urn:schemas-microsoft-com:office:excel\">");
			xml.AppendLine(" <DisplayRightToLeft/>");
			xml.AppendLine(" <FreezePanes/>");
			xml.AppendLine(" <SplitHorizontal>2</SplitHorizontal>");
			xml.AppendLine(" <TopRowBottomPane>2</TopRowBottomPane>");
			xml.AppendLine(" <ActivePane>2</ActivePane>");
			xml.AppendLine("</WorksheetOptions>");

			// AutoFilter
			xml.AppendLine($"<AutoFilter x:Range=\"R2C1:R2C{colCount}\" xmlns=\"urn:schemas-microsoft-com:office:excel\"/>");

			xml.AppendLine("</Worksheet>");
			xml.AppendLine("</Workbook>");

			// Write with UTF-8 BOM so Excel recognizes encoding
			System.IO.File.WriteAllText(filePath, xml.ToString(), new System.Text.UTF8Encoding(true));
		}

		private static string EscapeXml(string s)
		{
			if (string.IsNullOrEmpty(s)) return "";
			return s.Replace("&", "&amp;")
			        .Replace("<", "&lt;")
			        .Replace(">", "&gt;")
			        .Replace("\"", "&quot;")
			        .Replace("'", "&apos;");
		}


		private void ApplyAllFilters()
		{
			string query = txtSearchClient != null ? txtSearchClient.Text.Trim() : "";
			string payType = (cboPayType != null && cboPayType.SelectedIndex > 0) ? cboPayType.SelectedItem?.ToString() : "";
			string empName = (cboEmployee != null && cboEmployee.SelectedIndex > 0 && cboEmployee.SelectedItem is ComboItem ci) ? ci.Text : "";

			string tabTag = tabReports.SelectedTab?.Tag?.ToString();
			if (tabTag == "IncomeStatementAndProfitability")
			{
				var dgPL = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgIncomeStatement");
				var dgProd = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgProductProfit");
				var dgCli = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgClientProfit");

				if (dgPL != null) FilterGrid(dgPL, query, payType, empName);
				if (dgProd != null) FilterGrid(dgProd, query, payType, empName);
				if (dgCli != null) FilterGrid(dgCli, query, payType, empName);
			}
			else
			{
				DataGridView dataGridView = FindDataGridView(tabReports.SelectedTab);
				if (dataGridView != null)
				{
					FilterGrid(dataGridView, query, payType, empName);
				}
			}
		}

		private void FilterGrid(DataGridView dg, string query, string payType = "", string empName = "")
		{
			if (dg == null) return;

			dg.SuspendLayout();
			try
			{
				bool hasQuery = !string.IsNullOrWhiteSpace(query);
				bool hasPayType = !string.IsNullOrWhiteSpace(payType) && !payType.StartsWith("كل ");
				bool hasEmp = !string.IsNullOrWhiteSpace(empName) && !empName.StartsWith("كل ");

				for (int i = 0; i < dg.Rows.Count; i++)
				{
					DataGridViewRow row = dg.Rows[i];
					if (row.IsNewRow) continue;

					if (row.Cells.Count > 0 && (row.Cells[0].Value?.ToString() == "الإجمالي الكلي" || row.Cells[0].Value?.ToString() == "الإجمالي"))
					{
						row.Visible = true;
						continue;
					}

					bool matchQuery = !hasQuery;
					bool matchPay = !hasPayType;
					bool matchEmp = !hasEmp;

					if (hasQuery)
					{
						foreach (DataGridViewCell cell in row.Cells)
						{
							string val = cell.Value?.ToString();
							if (val != null && val.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
							{
								matchQuery = true;
								break;
							}
						}
					}

					if (hasPayType)
					{
						string pKeyword = payType.Contains("نقدي") ? "نقدي" : payType.Contains("آجل") ? "آجل" : payType.Contains("فيزا") ? "فيزا" : payType.Contains("تقسيط") ? "تقسيط" : payType;
						foreach (DataGridViewCell cell in row.Cells)
						{
							string val = cell.Value?.ToString();
							if (val != null && val.IndexOf(pKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
							{
								matchPay = true;
								break;
							}
						}
					}

					if (hasEmp)
					{
						foreach (DataGridViewCell cell in row.Cells)
						{
							string val = cell.Value?.ToString();
							if (val != null && val.IndexOf(empName, StringComparison.OrdinalIgnoreCase) >= 0)
							{
								matchEmp = true;
								break;
							}
						}
					}

					row.Visible = (matchQuery && matchPay && matchEmp);
				}
			}
			catch { }
			finally
			{
				dg.ResumeLayout();
			}
		}

		private void TabReports_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index < 0 || e.Index >= tabReports.TabPages.Count) return;

			var tp = tabReports.TabPages[e.Index];
			bool isSelected = (e.Index == tabReports.SelectedIndex);

			Graphics g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			Rectangle tabRect = tabReports.GetTabRect(e.Index);

			// Background & Text Colors
			Color bg;
			Color borderColor;
			Color textCol;

			if (isSelected)
			{
				bg = Color.FromArgb(30, 41, 59); // Slate Navy Card
				borderColor = Color.FromArgb(59, 130, 246); // Royal Blue Accent
				textCol = Color.White;
			}
			else
			{
				bg = Color.FromArgb(241, 245, 249); // Calm Soft Slate
				borderColor = Color.FromArgb(203, 213, 225);
				textCol = Color.FromArgb(71, 85, 105);
			}

			// Fill tab background
			using (var br = new SolidBrush(bg))
			{
				g.FillRectangle(br, tabRect);
			}

			// Border
			using (var p = new Pen(borderColor, 1f))
			{
				g.DrawRectangle(p, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
			}

			if (isSelected)
			{
				// Top Accent Line
				using (var pAccent = new Pen(Color.FromArgb(59, 130, 246), 3.5f))
				{
					g.DrawLine(pAccent, tabRect.X + 1, tabRect.Y + 2, tabRect.Right - 1, tabRect.Y + 2);
				}
			}

			// Text
			string text = tp.Text;
			using (var font = new Font("Segoe UI", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Bold))
			{
				var sf = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center,
					FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox
				};

				var textRect = new RectangleF(tabRect.X + 4, tabRect.Y, tabRect.Width - 8, tabRect.Height);
				using (var brText = new SolidBrush(textCol))
				{
					g.DrawString(text, font, brText, textRect, sf);
				}
			}
		}

		private DataGridView FindDataGridView(Control parent)
		{
			if (parent == null) return null;
			if (parent is DataGridView dg) return dg;
			foreach (Control child in parent.Controls)
			{
				var found = FindDataGridView(child);
				if (found != null) return found;
			}
			return null;
		}

		private static string GetReportPermissionKey(string tag)
		{
			switch (tag)
			{
				case "DailySalesSummary": return "RepDailySales";
				case "SalesByPeriod": return "RepSalesByPeriod";
				case "DetailedSales": return "RepDetailedSales";
				case "DetailedSaleItems": return "RepDetailedSaleItems";
				case "SalesByProduct": return "RepSalesByProduct";
				case "SalesByCategory": return "RepSalesByCategory";
				case "SalesByClient": return "RepSalesByClient";
				case "SalesByUser": return "RepSalesByUser";
				case "SalesByPaymentMethod": return "RepSalesByPayment";
				case "SalesDiscounts": return "RepSalesDiscounts";
				case "DetailedReturns": return "RepDetailedReturns";
				case "SalesProfitability": return "RepSalesProfit";
				case "StagnantProducts": return "RepStagnantProducts";
				case "DailyPurchasesSummary": return "RepDailyPurchases";
				case "PurchasesByPeriod": return "RepPurchasesByPeriod";
				case "DetailedPurchases": return "RepDetailedPurchases";
				case "DetailedPurchaseItems": return "RepDetailedPurchaseItems";
				case "PurchasesBySupplier": return "RepPurchasesBySupplier";
				case "PurchasesByProduct": return "RepPurchasesByProduct";
				case "PurchasesByCategory": return "RepPurchasesByCategory";
				case "DetailedPurchaseReturns": return "RepPurchaseReturns";
				case "SupplierPayments": return "RepSupplierPayments";
				case "PurchasePricesTracking": return "RepPurchasePrices";
				case "CreditPurchases": return "RepCreditPurchases";
				case "ProductQtyDetail": return "RepProductQtyDetail";
				case "WastageLoss": return "RepWastageLoss";
				case "DetailedInventoryValuation": return "RepInventoryValuation";
				case "SupplierItemActivity": return "RepSupplierItemActivity";
				case "ExpiryReport": return "RepExpiryReport";
				case "InventoryVariance": return "RepInventoryVariance";
				case "ClientBalances": return "RepClientBalances";
				case "DebtAging": return "RepDebtAging";
				case "ClientProductSales": return "RepClientProductSales";
				case "SalesByDriver": return "RepSalesByDriver";
				case "Handovers": return "RepHandovers";
				case "DailyClosing": return "RepDailyClosing";
				case "IncomeStatementAndProfitability": return "RepIncomeStatement";
				case "FinancialSummary": return "RepFinancialSummary";
				case "ShiftsHistory": return "ShiftsHistory";
				case "ShiftVsCalendarComparison": return "RepShiftComparison";
				default: return "Reports";
			}
		}
	}
}

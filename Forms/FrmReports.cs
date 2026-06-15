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

		private Button btnLoad;

		private Button btnPrint;

		private Button btnWhatsAppReport;

		private Button btnExportExcel;

		private TextBox txtSearchClient;

		private Label lblSearchClient;

		private DataTable _currentDt;

		public FrmReports()
		{
			InitUI();
		}

		private void InitUI()
		{
			Text = "التقارير التفصيلية المتقدمة";
			base.Size = new Size(1100, 700);
			base.StartPosition = FormStartPosition.CenterScreen;
			RightToLeft = RightToLeft.Yes;
			BackColor = Theme.BgMain;
			Font = Theme.FontMain;
			FlowLayoutPanel panel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 55,
				BackColor = Theme.BgCard,
				Padding = new Padding(10),
				FlowDirection = FlowDirection.RightToLeft,
				WrapContents = false
			};
			Label label = new Label
			{
				Text = "من:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Font = Theme.FontBold,
				Margin = new Padding(10, 8, 0, 0)
			};
			dtpFrom = new DateTimePicker
			{
				Width = 130,
				Format = DateTimePickerFormat.Short,
				Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
				Margin = new Padding(5, 4, 0, 0)
			};
			Label label2 = new Label
			{
				Text = "إلى:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Font = Theme.FontBold,
				Margin = new Padding(20, 8, 0, 0)
			};
			dtpTo = new DateTimePicker
			{
				Width = 130,
				Format = DateTimePickerFormat.Short,
				Margin = new Padding(5, 4, 0, 0)
			};
			btnLoad = Theme.MakeButton("🔄 تحديث التقرير", Theme.Accent);
			btnLoad.Size = new Size(130, 32);
			btnLoad.Margin = new Padding(30, 0, 0, 0);
			btnLoad.Click += delegate
			{
				LoadCurrentTab();
			};
			btnPrint = Theme.MakeButton("🖨️ طباعة الصفحة الحالية", Theme.Primary);
			btnPrint.Size = new Size(160, 32);
			btnPrint.Margin = new Padding(10, 0, 0, 0);
			btnPrint.Click += BtnPrint_Click;

			btnWhatsAppReport = Theme.MakeButton("📲 إرسال الكشف واتساب", Color.FromArgb(37, 211, 102));
			btnWhatsAppReport.Size = new Size(180, 32);
			btnWhatsAppReport.Margin = new Padding(10, 0, 0, 0);
			btnWhatsAppReport.Click += BtnWhatsAppReport_Click;
			btnWhatsAppReport.Visible = false;

			btnExportExcel = Theme.MakeButton("📥 تصدير إكسيل", Color.FromArgb(0, 102, 204));
			btnExportExcel.Size = new Size(130, 32);
			btnExportExcel.Margin = new Padding(10, 0, 0, 0);
			btnExportExcel.Click += BtnExportExcel_Click;

			lblSearchClient = new Label
			{
				Text = "بحث باسم العميل:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Font = Theme.FontBold,
				Margin = new Padding(20, 8, 0, 0)
			};
			txtSearchClient = new TextBox
			{
				Width = 150,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				BorderStyle = BorderStyle.FixedSingle,
				Margin = new Padding(5, 4, 0, 0)
			};
			txtSearchClient.TextChanged += (s, e) =>
			{
				DataGridView dataGridView = tabReports.SelectedTab?.Controls.OfType<DataGridView>().FirstOrDefault();
				if (dataGridView != null)
				{
					FilterGrid(dataGridView, txtSearchClient.Text.Trim());
				}
			};

			panel.Controls.AddRange(new Control[] { dtpFrom, label, dtpTo, label2, txtSearchClient, lblSearchClient, btnLoad, btnPrint, btnWhatsAppReport, btnExportExcel });
			base.Controls.Add(panel);
			tabReports = new TabControl
			{
				Dock = DockStyle.Fill,
				Font = Theme.FontMain
			};
			var tabsList = new System.Collections.Generic.List<(string, string)>
			{
				("📑 تقرير التقفيل اليومي", "DailyClosing"),
				("🧾 سجل فواتير المبيعات", "DetailedSales"),
				("🔄 سجل مرتجعات المبيعات", "DetailedReturns"),
				("🗓 مبيعات يومية تفصيلية", "SalesByDay"),
				("🚚 مبيعات المناديب", "SalesByDriver"),
				("👥 مبيعات العملاء الشاملة", "SalesByClient"),
				("⚖️ أرصدة وبيانات العملاء", "ClientBalances"),
				("📦 مبيعات الأصناف والصافي", "SalesByProduct"),
				("📊 كميات الأصناف التفصيلي", "ProductQtyDetail"),
				("📋 سجل تقفيل المناديب", "Handovers"),
				("🚨 تقرير الهالك والتالف", "WastageLoss")
			};
			if (Session.CanShowCostProfit("Reports"))
			{
				tabsList.Add(("📈 الملخص المالي والتشغيلي", "FinancialSummary"));
			}

			foreach (var tabInfo in tabsList)
			{
				string item = tabInfo.Item1;
				string item2 = tabInfo.Item2;
				TabPage tabPage = new TabPage(item)
				{
					Tag = item2,
					BackColor = Theme.BgMain
				};
				DataGridView value = new DataGridView
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
					DefaultCellStyle = new DataGridViewCellStyle
					{
						BackColor = Theme.BgCard,
						ForeColor = Theme.TextMain,
						SelectionBackColor = Theme.Primary,
						SelectionForeColor = Color.White,
						Font = Theme.FontMain
					},
					ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
					{
						BackColor = Theme.Primary,
						ForeColor = Color.White,
						Font = new Font("Segoe UI", 10f, FontStyle.Bold),
						Alignment = DataGridViewContentAlignment.MiddleCenter
					},
					ColumnHeadersHeight = 36,
					ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
					EnableHeadersVisualStyles = false
				};
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
			LoadCurrentTab();
		}

		private void LoadCurrentTab()
		{
			if (tabReports.SelectedTab == null)
			{
				return;
			}
			if (btnWhatsAppReport != null)
			{
				string text = tabReports.SelectedTab.Tag?.ToString();
				btnWhatsAppReport.Visible = (text == "ClientBalances");
			}
			DataGridView dataGridView = tabReports.SelectedTab.Controls.OfType<DataGridView>().FirstOrDefault();
			if (dataGridView != null)
			{
				string text = tabReports.SelectedTab.Tag?.ToString();
				dataGridView.Columns.Clear();
				dataGridView.Rows.Clear();
				switch (text)
				{
				case "DetailedSales":
					_currentDt = SaleDAL.GetAll(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[8]
					{
						("SaleCode", "رقم الفاتورة"),
						("SaleDate", "التاريخ والوقت"),
						("SaleType", "النوع"),
						("ClientName", "العميل"),
						("DriverName", "المندوب"),
						("TotalAmount", "قيمة الفاتورة"),
						("Notes", "الملاحظات"),
						("SaleID", "معرف الفاتورة")
					}, dataGridView);
					if (dataGridView.Columns["SaleID"] != null) dataGridView.Columns["SaleID"].Visible = false;
					break;
				case "DetailedReturns":
					_currentDt = ReturnDAL.GetAll(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[6]
					{
						("ReturnDate", "التاريخ والوقت"),
						("SaleCode", "الفاتورة الأصلية"),
						("ClientName", "العميل"),
						("TotalAmount", "قيمة المرتجع"),
						("Notes", "البيان / الملاحظات"),
						("ReturnID", "معرف المرتجع")
					}, dataGridView);
					if (dataGridView.Columns["ReturnID"] != null) dataGridView.Columns["ReturnID"].Visible = false;
					break;
				case "SalesByDay":
					_currentDt = ReportDAL.SalesByDay(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[6]
					{
						("SaleDay", "اليوم"),
						("Count", "عدد الفواتير"),
						("CashTotal", "مبيعات نقدي"),
						("CreditTotal", "مبيعات آجل"),
						("LoadTotal", "حمولات مناديب"),
						("Total", "إجمالي اليوم")
					}, dataGridView);
					break;
				case "SalesByDriver":
					_currentDt = ReportDAL.SalesByDriver(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[5]
					{
						("DriverName", "المندوب"),
						("Count", "عدد فواتيره"),
						("CashTotal", "مبيعات نقدي"),
						("CreditTotal", "مبيعات آجل"),
						("Total", "إجمالي المبيعات")
					}, dataGridView);
					break;
				case "SalesByClient":
					_currentDt = ReportDAL.SalesByClient(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[9]
					{
						("ClientName", "العميل"),
						("Phone", "الهاتف"),
						("Count", "فواتير الشراء"),
						("CashTotal", "شراء نقدي"),
						("CreditTotal", "شراء آجل"),
						("ReturnsTotal", "إجمالي مرتجعاته"),
						("PaidTotal", "إجمالي مسدداته"),
						("Total", "إجمالي الشراء"),
						("CurrentBalance", "المديونية الحالية")
					}, dataGridView);
					break;
				case "SalesByProduct":
					_currentDt = ReportDAL.SalesByProduct(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[9]
					{
						("ProductName", "الصنف"),
						("Unit", "الوحدة"),
						("AvgPrice", "متوسط سعر البيع"),
						("TotalQty", "الكمية المباعة"),
						("TotalAmount", "إجمالي المبيعات"),
						("ReturnedQty", "الكمية المرتجعة"),
						("ReturnedAmount", "إجمالي المرتجعات"),
						("NetQty", "صافي الكمية"),
						("NetAmount", "صافي المبيعات")
					}, dataGridView);
					break;
				case "ProductQtyDetail":
					_currentDt = ReportDAL.GetProductQtyDetail(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[12]
					{
						("ProductCode",    "كود الصنف"),
						("ProductName",    "اسم الصنف"),
						("Unit",           "الوحدة"),
						("SalePrice",      "سعر البيع"),
						("LastAdjQty",     "رصيد آخر تسوية"),
						("SoldQty",        "إجمالي المبيع"),
						("CashQty",        "نقدي"),
						("CreditQty",      "آجل"),
						("DriverLoadQty",  "حمولات مناديب"),
						("ReturnedQty",    "مرتجع مبيعات"),
						("DriverReturnQty","مرتجع مناديب"),
						("NetSoldQty",     "صافي المبيع"),
						// TotalSalesAmt و CurrentStock في عمودين مخفيَّين بعد ذلك
					}, dataGridView);
					// نضيف عمودين إضافيين: إجمالي القيمة والرصيد الحالي
					dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalSalesAmt", HeaderText = "إجمالي قيمة المبيع" });
					dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStock",  HeaderText = "الرصيد الحالي" });
					break;
				case "Handovers":
					_currentDt = DriverDAL.GetHandovers(dtpFrom.Value, dtpTo.Value);
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
					_currentDt = ReportDAL.WastageLossReport(dtpFrom.Value, dtpTo.Value);
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
				case "FinancialSummary":
					_currentDt = ReportDAL.GetFinancialSummary(dtpFrom.Value, dtpTo.Value);
					SetupGrid(new(string, string)[2]
					{
						("Indicator", "المؤشر المالي والتشغيلي"),
						("Val", "القيمة المالية للنشاط")
					}, dataGridView);
					break;
				case "DailyClosing":
					_currentDt = new DataTable();
					LoadDailyClosingReport(dataGridView);
					break;
				}
				FillGrid(dataGridView);
			}
		}

		private void SetupGrid((string field, string header)[] cols, DataGridView dg)
		{
			if (cols.Length > 6)
			{
				dg.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
			}
			else
			{
				dg.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			}

			for (int i = 0; i < cols.Length; i++)
			{
				var (name, headerText) = cols[i];
				var col = new DataGridViewTextBoxColumn
				{
					Name = name,
					HeaderText = headerText
				};
				if (name == "Notes" || name == "Address")
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
				}
				dg.Rows.Add(array2);
			}
			if (!flag || dg.Rows.Count <= 0)
			{
				return;
			}
			int index = dg.Rows.Add();
			dg.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(30, 60, 30);
			dg.Rows[index].DefaultCellStyle.ForeColor = Color.LightGreen;
			dg.Rows[index].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
			dg.Rows[index].Cells[0].Value = "الإجمالي الكلي";
			for (int j = 1; j < dg.Columns.Count; j++)
			{
				string name2 = dg.Columns[j].Name;
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
					break;
				}
				string text2 = ((name2 == "Count") ? "N0" : "N2");
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
			DataGridView dg = tabReports.SelectedTab?.Controls.OfType<DataGridView>().FirstOrDefault();
			if (_currentDt == null || dg == null || dg.Rows.Count == 0)
			{
				MessageBox.Show("لا توجد بيانات متاحة للطباعة حاليا\u064b.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (tabReports.SelectedTab?.Tag?.ToString() == "DailyClosing")
			{
				PrintDailyClosing(dg);
				return;
			}
			PrintDocument printDocument = new PrintDocument();
			if (dg.Columns.Count > 5)
			{
				printDocument.DefaultPageSettings.Landscape = true;
			}
			int pageRow = 0;
			printDocument.PrintPage += delegate(object s, PrintPageEventArgs ev)
			{
				Graphics graphics = ev.Graphics;
				Font font = new Font("Arial", 14f, FontStyle.Bold);
				Font font2 = new Font("Arial", 9f, FontStyle.Bold);
				Font font3 = new Font("Arial", 8f);
				int num = 20;

				int totalGridWidth = 0;
				for (int k = 0; k < dg.Columns.Count; k++)
				{
					if (dg.Columns[k].Visible)
						totalGridWidth += dg.Columns[k].Width;
				}
				if (totalGridWidth == 0) totalGridWidth = 1;

				int printWidth = printDocument.DefaultPageSettings.Landscape ? 1050 : 780;
				int titleX = printDocument.DefaultPageSettings.Landscape ? 450 : 320;

				graphics.DrawString(tabReports.SelectedTab.Text, font, Brushes.DarkBlue, titleX, num);
				num += 30;
				if (tabReports.SelectedTab.Tag?.ToString() == "ClientBalances")
				{
					graphics.DrawString($"تاريخ التقرير: {DateTime.Now:dd/MM/yyyy HH:mm}", font3, Brushes.Black, titleX, num);
					num += 25;
				}
				else
				{
					graphics.DrawString($"من تاريخ: {dtpFrom.Value:dd/MM/yyyy}  إلى تاريخ: {dtpTo.Value:dd/MM/yyyy}", font3, Brushes.Black, titleX - 20, num);
					num += 25;
				}
				graphics.DrawLine(Pens.DarkBlue, 20, num, printWidth + 20, num);
				num += 10;

				int currentX = 20;
				for (int i = 0; i < dg.Columns.Count; i++)
				{
					if (!dg.Columns[i].Visible) continue;
					int colWidth = (dg.Columns[i].Width * printWidth) / totalGridWidth;
					graphics.DrawString(dg.Columns[i].HeaderText, font2, Brushes.DarkBlue, currentX, num);
					currentX += colWidth;
				}
				num += 22;
				graphics.DrawLine(Pens.Gray, 20, num, printWidth + 20, num);
				num += 8;

				while (pageRow < dg.Rows.Count)
				{
					DataGridViewRow dataGridViewRow = dg.Rows[pageRow];
					bool flag = dataGridViewRow.Cells[0].Value?.ToString() == "الإجمالي الكلي" || dataGridViewRow.Cells[0].Value?.ToString() == "الإجمالي";
					Font font4 = (flag ? new Font("Arial", 8.5f, FontStyle.Bold) : font3);
					Brush brush = (flag ? Brushes.DarkGreen : Brushes.Black);

					currentX = 20;
					for (int j = 0; j < dg.Columns.Count; j++)
					{
						if (!dg.Columns[j].Visible) continue;
						int colWidth = (dg.Columns[j].Width * printWidth) / totalGridWidth;
						
						var rect = new RectangleF(currentX, num, colWidth - 5, 18);
						var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
						graphics.DrawString(dataGridViewRow.Cells[j].Value?.ToString() ?? "", font4, brush, rect, sf);
						
						currentX += colWidth;
					}
					num += 18;
					pageRow++;
					if (num > ev.PageBounds.Height - 45)
					{
						ev.HasMorePages = true;
						return;
					}
				}
				pageRow = 0;
			};
			PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
			printPreviewDialog.Document = printDocument;
			printPreviewDialog.Width = 950;
			printPreviewDialog.Height = 750;
			printPreviewDialog.ShowDialog();
		}

		private void LoadDailyClosingReport(DataGridView dg)
		{
			try
			{
				DateTime date = dtpFrom.Value.Date;

				var products = ProductDAL.GetAll(activeOnly: true);
				int productCount = products.Rows.Count;

				var dtQty = ReportDAL.GetDailyClientProductSales(date);
				var dtTotals = ReportDAL.GetDailyClientTotals(date);

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
						row[i + 1] = qty > 0 ? qty.ToString("N0") : "";
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
				StyleDailySpecialRow(dg.Rows[totRowIdx], Color.FromArgb(30, 60, 30), Color.LightGreen, new Font("Segoe UI", 10.5f, FontStyle.Bold));
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

		private void PrintDailyClosing(DataGridView dg)
		{
			var pd = new PrintDocument();
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

				// استخدام أبعاد ثابتة لصفحة A4 بالعرض (Landscape)
				int mL = 20;
				int mR = 1070;
				int mT = 40;
				int mB = 780;
				int pgW = mR - mL;   // 1050

				int y = mT;

				// ─── العنوان (الصفحة الأولى فقط) ───
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

				// ─── حساب عرض كل عمود بدقة ───
				int visColCount = dg.Columns.GetColumnCount(DataGridViewElementStates.Visible);
				int[] widths = ComputeDailyPrintWidths(pgW, visColCount, dg);

				// التحقق من أن مجموع العروض يساوي pgW بالضبط
				int totalW = 0;
				foreach (int w in widths) totalW += w;
				if (totalW != pgW && widths.Length > 0 && widths[widths.Length - 1] + (pgW - totalW) > 0)
					widths[widths.Length - 1] += pgW - totalW; // تصحيح الفرق في آخر عمود

				const int HEAD_H = 22;
				const int ROW_H  = 18;

				// ─── رؤوس الأعمدة (تُرسم في كل صفحة) ───
				{
					int cx = mR;   // RTL: نبدأ من اليمين
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

				// ─── صفوف البيانات ───
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

				// ─── تذييل الصفحة ───
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
			// توزيع عرض الصفحة الكامل على الأعمدة بدقة
			int clientW = (int)(pgW * 0.15);
			int extraW  = (int)(pgW * 0.09);
			int reservedW = clientW + extraW * 3;
			int prodCount = colCount - 4;
			int prodW = prodCount > 0 ? (pgW - reservedW) / prodCount : 60;
			
			// لضمان عدم حدوث عرض سالب
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

			var dlg = new Form
			{
				Width = 420, Height = 190,
				Text = "إرسال واتساب - أرصدة العملاء",
				StartPosition = FormStartPosition.CenterParent,
				RightToLeft = RightToLeft.Yes,
				RightToLeftLayout = true,
				BackColor = Theme.BgCard,
				Font = Theme.FontMain
			};
			var lbl = new Label { Text = "📱 أدخل رقم الواتساب (مثال: 01012345678):", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(10, 15) };
			var txt = new TextBox { Location = new Point(10, 42), Width = 380, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle };
			var btnSend = Theme.MakeButton("✅ إرسال", 230, 90, 150, 36, Color.FromArgb(37, 211, 102));
			var btnCancel = Theme.MakeButton("❌ إلغاء", 60, 90, 150, 36, Color.FromArgb(180, 60, 60));
			btnSend.Click   += (s2, e2) => { dlg.DialogResult = DialogResult.OK;     dlg.Close(); };
			btnCancel.Click += (s2, e2) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
			dlg.Controls.AddRange(new Control[] { lbl, txt, btnSend, btnCancel });

			if (dlg.ShowDialog() != DialogResult.OK) return;
			string phone = txt.Text.Trim();
			if (string.IsNullOrWhiteSpace(phone)) return;

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

			SendWhatsApp(phone, sb.ToString());
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

		private void BtnExportExcel_Click(object sender, EventArgs e)
		{
			DataGridView dataGridView = tabReports.SelectedTab?.Controls.OfType<DataGridView>().FirstOrDefault();
			if (dataGridView == null || dataGridView.Rows.Count == 0)
			{
				MessageBox.Show("لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			string tabText = tabReports.SelectedTab?.Text ?? "تقرير";
			string cleanName = System.Text.RegularExpressions.Regex.Replace(tabText, @"[^\w\s\-\u0600-\u06FF]", "").Trim();
			string defaultFileName = $"{cleanName}_{DateTime.Now:yyyy_MM_dd}.csv";

			ExportToExcel(dataGridView, defaultFileName);
		}

		private void ExportToExcel(DataGridView dgv, string defaultFileName)
		{
			using (var dlg = new SaveFileDialog())
			{
				dlg.Title = "تصدير التقرير إلى Excel";
				dlg.FileName = defaultFileName;
				dlg.Filter = "Excel CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					try
					{
						var sb = new System.Text.StringBuilder();

						// 1. Headers
						var headers = new List<string>();
						foreach (DataGridViewColumn col in dgv.Columns)
						{
							if (col.Visible)
							{
								headers.Add($"\"{col.HeaderText?.Replace("\"", "\"\"")}\"");
							}
						}
						sb.AppendLine(string.Join(",", headers));

						// 2. Rows
						foreach (DataGridViewRow row in dgv.Rows)
						{
							if (row.IsNewRow) continue;
							var cells = new List<string>();
							foreach (DataGridViewColumn col in dgv.Columns)
							{
								if (col.Visible)
								{
									var val = row.Cells[col.Index].Value?.ToString() ?? "";
									cells.Add($"\"{val.Replace("\"", "\"\"")}\"");
								}
							}
							sb.AppendLine(string.Join(",", cells));
						}

						// Save with UTF-8 BOM encoding so Excel displays Arabic correctly
						System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);

						MessageBox.Show("✅ تم تصدير التقرير بنجاح!\nيمكنك الآن فتح الملف مباشرة باستخدام برنامج Excel أو إرساله.", 
							"تم التصدير بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information,
							MessageBoxDefaultButton.Button1,
							MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
					}
					catch (Exception ex)
					{
						MessageBox.Show("❌ فشل تصدير الملف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void FilterGrid(DataGridView dg, string query)
		{
			if (dg == null) return;

			dg.SuspendLayout();
			try
			{
				bool hasQuery = !string.IsNullOrWhiteSpace(query);
				for (int i = 0; i < dg.Rows.Count; i++)
				{
					DataGridViewRow row = dg.Rows[i];
					if (row.IsNewRow) continue;

					if (row.Cells[0].Value?.ToString() == "الإجمالي الكلي")
					{
						row.Visible = true;
						continue;
					}

					if (!hasQuery)
					{
						row.Visible = true;
						continue;
					}

					bool match = false;
					foreach (DataGridViewCell cell in row.Cells)
					{
						string val = cell.Value?.ToString();
						if (val != null && val.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							match = true;
							break;
						}
					}
					row.Visible = match;
				}
			}
			catch { }
			finally
			{
				dg.ResumeLayout();
			}
		}
	}
}

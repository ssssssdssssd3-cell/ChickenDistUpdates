using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Linq;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
	public class FrmReports : Form
	{
		private TabControl tabReports;

		private DateTimePicker dtpFrom;

		private DateTimePicker dtpTo;

		private Button btnLoad;

		private Button btnPrint;

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
			panel.Controls.AddRange(new Control[6] { label, dtpFrom, label2, dtpTo, btnLoad, btnPrint });
			base.Controls.Add(panel);
			tabReports = new TabControl
			{
				Dock = DockStyle.Fill,
				Font = Theme.FontMain
			};
			(string, string)[] array = new(string, string)[10]
			{
				("🧾 سجل فواتير المبيعات", "DetailedSales"),
				("🔄 سجل مرتجعات المبيعات", "DetailedReturns"),
				("🗓 مبيعات يومية تفصيلية", "SalesByDay"),
				("🚚 مبيعات المناديب", "SalesByDriver"),
				("👥 مبيعات العملاء الشاملة", "SalesByClient"),
				("⚖️ أرصدة وبيانات العملاء", "ClientBalances"),
				("📦 مبيعات الأصناف والصافي", "SalesByProduct"),
				("📊 كميات الأصناف التفصيلي", "ProductQtyDetail"),
				("📋 سجل تقفيل المناديب", "Handovers"),
				("📈 الملخص المالي والتشغيلي", "FinancialSummary")
			};
			(string, string)[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				(string, string) tuple = array2[i];
				string item = tuple.Item1;
				string item2 = tuple.Item2;
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
	}
}

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

		private ComboBox cboWarehouse;

		private Button btnLoad;

		private Button btnPrint;

		private Button btnWhatsAppReport;

		private Button btnExportExcel;

		private TextBox txtSearchClient;

		private Label lblSearchClient;

		private DataTable _currentDt;
		private string _targetModule = null;
		private int _preFilteredID = 0;

		public string TargetModule => _targetModule;

		public FrmReports(string targetModule = null, int preFilteredID = 0)
		{
			_targetModule = targetModule;
			_preFilteredID = preFilteredID;
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
			Label lblWh = new Label
			{
				Text = "المخزن:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Font = Theme.FontBold,
				Margin = new Padding(20, 8, 0, 0)
			};
			cboWarehouse = new ComboBox
			{
				Width = 150,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				Margin = new Padding(5, 4, 0, 0)
			};
			LoadWarehouses();
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
				string tabTag = tabReports.SelectedTab?.Tag?.ToString();
				if (tabTag == "IncomeStatementAndProfitability")
				{
					var dgPL = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgIncomeStatement");
					var dgProd = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgProductProfit");
					var dgCli = FindControlByName<DataGridView>(tabReports.SelectedTab, "dgClientProfit");

					if (dgPL != null) FilterGrid(dgPL, txtSearchClient.Text.Trim());
					if (dgProd != null) FilterGrid(dgProd, txtSearchClient.Text.Trim());
					if (dgCli != null) FilterGrid(dgCli, txtSearchClient.Text.Trim());
				}
				else
				{
					DataGridView dataGridView = FindDataGridView(tabReports.SelectedTab);
					if (dataGridView != null)
					{
						FilterGrid(dataGridView, txtSearchClient.Text.Trim());
					}
				}
			};

			panel.Controls.AddRange(new Control[] { label, dtpFrom, label2, dtpTo, lblWh, cboWarehouse, lblSearchClient, txtSearchClient, btnLoad, btnPrint, btnWhatsAppReport, btnExportExcel });
			base.Controls.Add(panel);
			tabReports = new TabControl
			{
				Dock = DockStyle.Fill,
				Font = Theme.FontMain
			};
			var allReports = new List<(string name, string tag)>
			{
				("📑 تقرير التقفيل اليومي", "DailyClosing"),
				("🧾 سجل فواتير المبيعات", "DetailedSales"),
				("🔄 سجل مرتجعات المبيعات", "DetailedReturns"),
				("🔄 سجل مرتجعات المشتريات", "DetailedPurchaseReturns"),
				("🗓 مبيعات يومية تفصيلية", "SalesByDay"),
				("🚚 مبيعات المناديب", "SalesByDriver"),
				("👥 مبيعات العملاء الشاملة", "SalesByClient"),
				("⚖️ أرصدة وبيانات العملاء", "ClientBalances"),
				("⏳ أعمار الديون (الديون الراكدة)", "DebtAging"),
				("📦 مبيعات الأصناف والصافي", "SalesByProduct"),
				("📊 كميات الأصناف التفصيلي", "ProductQtyDetail"),
				("📋 سجل تقفيل المناديب", "Handovers"),
				("🚨 تقرير الهالك والتالف", "WastageLoss"),
				("📈 الملخص المالي والتشغيلي", "FinancialSummary"),
				("📦 تقييم المخزن التفصيلي", "DetailedInventoryValuation"),
				("📊 قائمة الدخل والربحية", "IncomeStatementAndProfitability"),
				("📑 مبيعات عميل تفصيلي", "ClientProductSales"),
				("📊 حركة أصناف الموردين", "SupplierItemActivity"),
				("⚠️ تقرير انتهاء الصلاحية", "ExpiryReport")
			};

			var filteredReports = new List<(string name, string tag)>();
			if (string.IsNullOrEmpty(_targetModule))
			{
				filteredReports = allReports;
			}
			else
			{
				foreach (var report in allReports)
				{
					bool keep = false;
					if (_targetModule == "Sales")
					{
						keep = (report.tag == "DetailedSales" || report.tag == "DetailedReturns" || report.tag == "SalesByDay" || report.tag == "SalesByProduct");
					}
					else if (_targetModule == "Purchases")
					{
						keep = (report.tag == "DetailedPurchaseReturns");
					}
					else if (_targetModule == "Stores")
					{
						keep = (report.tag == "ProductQtyDetail" || report.tag == "WastageLoss" || report.tag == "DetailedInventoryValuation" || report.tag == "SupplierItemActivity" || report.tag == "ExpiryReport");
					}
					else if (_targetModule == "Clients")
					{
						keep = (report.tag == "SalesByClient" || report.tag == "ClientBalances" || report.tag == "ClientProductSales" || report.tag == "DebtAging");
					}
					else if (_targetModule == "Suppliers")
					{
						keep = (report.tag == "SupplierItemActivity");
					}
					else if (_targetModule == "Drivers")
					{
						keep = (report.tag == "SalesByDriver" || report.tag == "Handovers");
					}
					else if (_targetModule == "Financials")
					{
						keep = (report.tag == "DailyClosing" || report.tag == "FinancialSummary" || report.tag == "IncomeStatementAndProfitability");
					}

					if (keep)
					{
						filteredReports.Add(report);
					}
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
						BackgroundColor = Theme.BgCard,
						BorderStyle = BorderStyle.None,
						RowHeadersVisible = false,
						AllowUserToAddRows = false,
						ReadOnly = true,
						SelectionMode = DataGridViewSelectionMode.FullRowSelect,
						RightToLeft = RightToLeft.Yes,
						GridColor = Theme.BorderColor,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
						DefaultCellStyle = dgPL.DefaultCellStyle,
						ColumnHeadersDefaultCellStyle = dgPL.ColumnHeadersDefaultCellStyle,
						ColumnHeadersHeight = 36,
						ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
						EnableHeadersVisualStyles = false
					};
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
						BackgroundColor = Theme.BgCard,
						BorderStyle = BorderStyle.None,
						RowHeadersVisible = false,
						AllowUserToAddRows = false,
						ReadOnly = true,
						SelectionMode = DataGridViewSelectionMode.FullRowSelect,
						RightToLeft = RightToLeft.Yes,
						GridColor = Theme.BorderColor,
						AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
						DefaultCellStyle = dgPL.DefaultCellStyle,
						ColumnHeadersDefaultCellStyle = dgPL.ColumnHeadersDefaultCellStyle,
						ColumnHeadersHeight = 36,
						ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
						EnableHeadersVisualStyles = false
					};
					subTabClient.Controls.Add(dgCliProfit);
					subTab.TabPages.Add(subTabClient);

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
					layout.Controls.Add(dgDebtAging, 0, 1);

					tabPage.Controls.Add(layout);
					tabReports.TabPages.Add(tabPage);
					continue;
				}

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
			string text = tabReports.SelectedTab.Tag?.ToString();
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
				case "DetailedSales":
					_currentDt = SaleDAL.GetAll(dtpFrom.Value, dtpTo.Value, warehouseID);
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
				case "DetailedPurchaseReturns":
					_currentDt = PurchaseReturnDAL.GetAll(dtpFrom.Value, dtpTo.Value, warehouseID);
					SetupGrid(new(string, string)[6]
					{
						("ReturnDate", "التاريخ والوقت"),
						("PurchaseCode", "الفاتورة الأصلية"),
						("SupplierName", "المورد"),
						("TotalAmount", "قيمة المرتجع"),
						("Notes", "البيان / الملاحظات"),
						("ReturnID", "معرف المرتجع")
					}, dataGridView);
					if (dataGridView.Columns["ReturnID"] != null) dataGridView.Columns["ReturnID"].Visible = false;
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
					SetupGrid(new(string, string)[12]
					{
						("ProductName", "الصنف"),
						("Unit", "الوحدة"),
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
					}, dataGridView);
					dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalSalesAmt", HeaderText = "إجمالي قيمة المبيع" });
					dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStock",  HeaderText = "الرصيد الحالي" });
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

						SetupGrid(new(string, string)[]
						{
							("الصنف", "الصنف"),
							("الشركة المنتجة", "الشركة المنتجة"),
							("المخزون الحالي", "المخزون الحالي"),
							("الكمية المباعة", "الكمية المباعة"),
							("قيمة المبيعات", "قيمة المبيعات"),
							("الكمية المشتراة", "الكمية المشتراة"),
							("قيمة المشتريات", "قيمة المشتريات"),
							("الحالة", "الحالة")
						}, dataGridView);
					}
					break;
				}
				FillGrid(dataGridView);
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
			dgPL.Columns.Add("Item", "البند التشغيلي");
			dgPL.Columns.Add("Value", "القيمة (ج.م)");

			try
			{
				DataTable dt = ReportDAL.GetIncomeStatement(dtpFrom.Value, dtpTo.Value, warehouseID);
				if (dt.Rows.Count > 0)
				{
					DataRow r = dt.Rows[0];
					decimal grossSales = Convert.ToDecimal(r["GrossSales"]);
					decimal returns = Convert.ToDecimal(r["SalesReturns"]);
					decimal netSales = grossSales - returns;

					decimal grossCOGS = Convert.ToDecimal(r["GrossCOGS"]);
					decimal returnsCOGS = Convert.ToDecimal(r["ReturnsCOGS"]);
					decimal netCOGS = grossCOGS - returnsCOGS;

					decimal grossProfit = netSales - netCOGS;

					decimal expenses = Convert.ToDecimal(r["GeneralExpenses"]);
					decimal whWastage = Convert.ToDecimal(r["WarehouseWastage"]);
					decimal drWastage = Convert.ToDecimal(r["DriverWastage"]);

					decimal netProfit = grossProfit - expenses - whWastage - drWastage;

					AddPlRow(dgPL, "🟢 إجمالي المبيعات", grossSales, false);
					AddPlRow(dgPL, "🔴 مرتجعات المبيعات (-)", returns, true);
					AddPlRow(dgPL, "⚖️ صافي الإيرادات", netSales, false, Color.FromArgb(30, 45, 60));
					AddPlRow(dgPL, "🔴 تكلفة البضاعة المباعة (-)", grossCOGS, true);
					AddPlRow(dgPL, "🟢 تكلفة مرتجع المبيعات (+)", returnsCOGS, false);
					AddPlRow(dgPL, "⚖️ صافي تكلفة المبيعات", netCOGS, true, Color.FromArgb(45, 45, 30));
					AddPlRow(dgPL, "💰 مجمل الأرباح التشغيلية", grossProfit, grossProfit < 0, Color.FromArgb(30, 30, 50));
					AddPlRow(dgPL, "🔴 المصاريف التشغيلية والعمومية (-)", expenses, true);
					AddPlRow(dgPL, "🔴 هالك وتالف المستودعات (-)", whWastage, true);
					AddPlRow(dgPL, "🔴 هالك وتالف المناديب (-)", drWastage, true);
					AddPlRow(dgPL, "🏆 صافي الربح / الخسارة النهائي", netProfit, netProfit < 0, Color.FromArgb(30, 60, 30));
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
			if (text == "ClientProductSales")
			{
				return FindControlByName<DataGridView>(tabReports.SelectedTab, "dgClientSales");
			}
			if (text == "SupplierItemActivity")
			{
				return FindControlByName<DataGridView>(tabReports.SelectedTab, "dgSupplierActivity");
			}
			
			return tabReports.SelectedTab.Controls.OfType<DataGridView>().FirstOrDefault();
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
			dg.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(30, 60, 30);
			dg.Rows[index].DefaultCellStyle.ForeColor = Color.LightGreen;
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
					break;
				}
				string text2 = ((name2 == "Count" || name2 == "المخزون الحالي" || name2 == "الكمية المباعة" || name2 == "الكمية المشتراة" || name2 == "الكمية") ? "N0" : "N2");
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
			AppConfig.SetPrinter(printDocument, AppConfig.A4PrinterName);
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
			AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
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

		private void BtnExportExcel_Click(object sender, EventArgs e)
		{
			DataGridView dataGridView = GetActiveGrid();
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
	}
}

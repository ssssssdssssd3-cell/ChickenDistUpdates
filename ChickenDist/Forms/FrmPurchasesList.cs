using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
	public class FrmPurchasesList : Form
	{
		private DataGridView dgPurchases;
		private DataGridView dgItems;
		private DateTimePicker dtpFrom;
		private DateTimePicker dtpTo;
		private TextBox txtSearch;
		private ComboBox cboTypeFilter;
		private ComboBox cboSupplierFilter;
		private TextBox txtProductSearch;
		private Button btnLoad;
		private Button btnNewPurchase;
		private Label lblTotalSummary;
		private Label lblReturnSummary;
		private Label lblNetSummary;
		private Label lblCashSummary;
		private Label lblCreditSummary;
		private DataTable _allPurchasesDt;

		public FrmPurchasesList()
		{
			InitUI();
			LoadPurchases();
		}

		private void InitUI()
		{
			Text = "سجل المشتريات";
			base.Size = new Size(1150, 720);
			base.StartPosition = FormStartPosition.CenterScreen;
			RightToLeft = RightToLeft.Yes;
			RightToLeftLayout = true;
			BackColor = Theme.BgMain;
			Font = Theme.FontMain;

			FlowLayoutPanel filterPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 50,
				FlowDirection = FlowDirection.LeftToRight,
				BackColor = Theme.BgCard,
				Padding = new Padding(10, 10, 10, 10),
				WrapContents = false
			};

			Label lblFrom = new Label
			{
				Text = "من:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Margin = new Padding(10, 5, 0, 0)
			};
			dtpFrom = new DateTimePicker
			{
				Width = 110,
				Height = 26,
				Format = DateTimePickerFormat.Short,
				Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
			};
			filterPanel.Controls.AddRange(new Control[2] { lblFrom, dtpFrom });

			Label lblTo = new Label
			{
				Text = "إلى:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Margin = new Padding(15, 5, 0, 0)
			};
			dtpTo = new DateTimePicker
			{
				Width = 110,
				Height = 26,
				Format = DateTimePickerFormat.Short
			};
			filterPanel.Controls.AddRange(new Control[2] { lblTo, dtpTo });

			Label lblType = new Label
			{
				Text = "النوع:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Margin = new Padding(15, 5, 0, 0)
			};
			cboTypeFilter = new ComboBox
			{
				Width = 110,
				Height = 26,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes
			};
			cboTypeFilter.Items.AddRange(new object[3] { "الكل", "نقدي (Cash)", "آجل (Credit)" });
			cboTypeFilter.SelectedIndex = 0;
			cboTypeFilter.SelectedIndexChanged += delegate { FilterData(); };
			filterPanel.Controls.AddRange(new Control[2] { lblType, cboTypeFilter });

			Label lblSupplier = new Label
			{
				Text = "المورد:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Margin = new Padding(15, 5, 0, 0)
			};
			cboSupplierFilter = new ComboBox
			{
				Width = 130,
				Height = 26,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes
			};
			cboSupplierFilter.Items.Add(new ComboItem(0, "الكل"));
			foreach (DataRow row in SupplierDAL.GetAll(true).Rows)
			{
				cboSupplierFilter.Items.Add(new ComboItem((int)row["SupplierID"], row["SupplierName"].ToString()));
			}
			cboSupplierFilter.DisplayMember = "Text";
			cboSupplierFilter.SelectedIndex = 0;
			cboSupplierFilter.SelectedIndexChanged += delegate
			{
				LoadPurchases();
			};
			filterPanel.Controls.AddRange(new Control[2] { lblSupplier, cboSupplierFilter });

			Label lblProduct = new Label
			{
				Text = "بحث صنف:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Margin = new Padding(15, 5, 0, 0)
			};
			txtProductSearch = new TextBox
			{
				Width = 120,
				Height = 26,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes
			};
			txtProductSearch.KeyDown += (s, e) =>
			{
				if (e.KeyCode == Keys.Enter)
				{
					LoadPurchases();
					e.Handled = true;
					e.SuppressKeyPress = true;
				}
			};
			filterPanel.Controls.AddRange(new Control[2] { lblProduct, txtProductSearch });

			Label lblSearch = new Label
			{
				Text = "بحث:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Margin = new Padding(15, 5, 0, 0)
			};
			txtSearch = new TextBox
			{
				Width = 140,
				Height = 26,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes
			};
			txtSearch.TextChanged += delegate
			{
				FilterData();
			};
			filterPanel.Controls.AddRange(new Control[2] { lblSearch, txtSearch });

			btnLoad = Theme.MakeButton("🔄 عرض", Theme.Accent);
			btnLoad.Size = new Size(80, 28);
			btnLoad.Margin = new Padding(15, 0, 0, 0);
			btnLoad.Click += delegate
			{
				LoadPurchases();
			};
			filterPanel.Controls.Add(btnLoad);

			btnNewPurchase = Theme.MakeButton("➕ فاتورة شراء جديدة", Color.FromArgb(40, 150, 80));
			btnNewPurchase.Size = new Size(150, 28);
			btnNewPurchase.Margin = new Padding(10, 0, 0, 0);
			btnNewPurchase.Click += delegate
			{
				FrmPurchase frm = new FrmPurchase();
				frm.ShowDialog();
				LoadPurchases();
			};
			filterPanel.Controls.Add(btnNewPurchase);

			Panel masterPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 280,
				Padding = new Padding(10, 0, 10, 10)
			};
			dgPurchases = MakeGrid();
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "PurchaseID",
				Visible = false
			});
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "PurchaseCode",
				HeaderText = "رقم الفاتورة",
				FillWeight = 60f
			});
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "PurchaseDate",
				HeaderText = "التاريخ والوقت",
				FillWeight = 80f
			});
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "PurchaseType",
				HeaderText = "نوع الفاتورة",
				FillWeight = 50f
			});
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "SupplierName",
				HeaderText = "المورد",
				FillWeight = 120f
			});
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "TotalAmount",
				HeaderText = "قيمة الفاتورة",
				FillWeight = 60f
			});
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ReturnAmount",
				HeaderText = "المرتجع ↩",
				FillWeight = 55f,
				DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(231, 76, 60), Alignment = DataGridViewContentAlignment.MiddleCenter }
			});
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "NetAmount",
				HeaderText = "الصافي ✔",
				FillWeight = 55f,
				DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
			});
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Notes",
				HeaderText = "الملاحظات",
				FillWeight = 130f
			});
			dgPurchases.SelectionChanged += DgPurchases_SelectionChanged;
			masterPanel.Controls.Add(dgPurchases);

			Panel detailPanel = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(10, 0, 10, 10)
			};

			FlowLayoutPanel detailLeftPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Left,
				Width = 220,
				FlowDirection = FlowDirection.TopDown,
				BackColor = Theme.BgCard,
				Padding = new Padding(15),
				WrapContents = false
			};
			Label lblDetailsTitle = new Label
			{
				Text = "تفاصيل الأصناف بالفاتورة المحددة",
				Size = new Size(190, 60),
				ForeColor = Theme.TextSub,
				Font = Theme.FontBold,
				TextAlign = ContentAlignment.TopCenter,
				Margin = new Padding(0)
			};
			detailLeftPanel.Controls.Add(lblDetailsTitle);

			dgItems = MakeGrid();
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ProductName",
				HeaderText = "الصنف",
				FillWeight = 130f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Quantity",
				HeaderText = "الكمية",
				FillWeight = 50f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "UnitPrice",
				HeaderText = "سعر الوحدة",
				FillWeight = 50f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Discount",
				HeaderText = "الخصم",
				FillWeight = 50f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "TotalPrice",
				HeaderText = "الإجمالي",
				FillWeight = 60f
			});

			detailPanel.Controls.Add(detailLeftPanel);
			detailPanel.Controls.Add(dgItems);

			TableLayoutPanel summaryTbl = new TableLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 70,
				ColumnCount = 5,
				RowCount = 1,
				RightToLeft = RightToLeft.Yes,
				BackColor = Theme.BgCard,
				Padding = new Padding(10, 5, 10, 5)
			};
			summaryTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
			summaryTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
			summaryTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
			summaryTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
			summaryTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
			summaryTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

			lblTotalSummary  = AddDashboardCard(summaryTbl, "إجمالي الفواتير:",        "0.00 ج", Theme.Accent,                         0);
			lblReturnSummary = AddDashboardCard(summaryTbl, "إجمالي المرتجعات: ↩",   "0.00 ج", Color.FromArgb(231, 76, 60),          1);
			lblNetSummary    = AddDashboardCard(summaryTbl, "الصافي بعد المرتجع: ✔", "0.00 ج", Color.FromArgb(46, 204, 113),         2);
			lblCashSummary   = AddDashboardCard(summaryTbl, "المشتريات النقدية:",      "0.00 ج", Theme.Success,                        3);
			lblCreditSummary = AddDashboardCard(summaryTbl, "المشتريات الآجلة:",      "0.00 ج", Color.FromArgb(52, 152, 219),         4);

			base.Controls.Add(detailPanel);
			base.Controls.Add(masterPanel);
			base.Controls.Add(filterPanel);
			base.Controls.Add(summaryTbl);
		}

		private Label AddDashboardCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIndex)
		{
			Panel cardPanel = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = Theme.BgCard,
				Padding = new Padding(5)
			};
			Label lblTitle = new Label
			{
				Text = title,
				Dock = DockStyle.Top,
				Height = 18,
				Font = new Font("Segoe UI", 9f),
				ForeColor = Theme.TextSub,
				TextAlign = ContentAlignment.TopRight
			};
			Label lblVal = new Label
			{
				Text = val,
				Dock = DockStyle.Fill,
				Font = new Font("Segoe UI", 13f, FontStyle.Bold),
				ForeColor = valColor,
				TextAlign = ContentAlignment.BottomRight
			};
			cardPanel.Controls.Add(lblVal);
			cardPanel.Controls.Add(lblTitle);
			parent.Controls.Add(cardPanel, colIndex, 0);
			return lblVal;
		}

		private DataGridView MakeGrid()
		{
			return new DataGridView
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
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				DefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Theme.BgCard,
					ForeColor = Theme.TextMain,
					SelectionBackColor = Theme.Primary,
					SelectionForeColor = Color.White,
					Font = Theme.FontMain
				},
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
				ColumnHeadersHeight = 40,
				ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Theme.Primary,
					ForeColor = Color.White,
					Font = new Font("Segoe UI", 10f, FontStyle.Bold),
					Alignment = DataGridViewContentAlignment.MiddleCenter
				},
				EnableHeadersVisualStyles = false
			};
		}

		private void LoadPurchases()
		{
			dgPurchases.Rows.Clear();
			dgItems.Rows.Clear();
			int? supplierID = null;
			if (cboSupplierFilter != null && cboSupplierFilter.SelectedItem is ComboItem ci && ci.ID > 0)
			{
				supplierID = ci.ID;
			}
			string productSearch = (txtProductSearch != null && !string.IsNullOrWhiteSpace(txtProductSearch.Text)) ? txtProductSearch.Text.Trim() : null;
			_allPurchasesDt = PurchaseDAL.GetAll(dtpFrom.Value, dtpTo.Value, supplierID, productSearch);
			FilterData();
		}

		private void FilterData()
		{
			dgPurchases.Rows.Clear();
			dgItems.Rows.Clear();
			if (_allPurchasesDt == null || _allPurchasesDt.Rows.Count == 0)
			{
				UpdateSummary(0m, 0m, 0m, 0m);
				return;
			}

			string query = txtSearch.Text.Trim().ToLower();
			string typeFilter = cboTypeFilter.SelectedItem?.ToString() ?? "الكل";

			decimal total = 0m;
			decimal ret   = 0m;
			decimal cash = 0m;
			decimal credit = 0m;

			foreach (DataRow row in _allPurchasesDt.Rows)
			{
				string code = row["PurchaseCode"].ToString().ToLower();
				string supplier = row["SupplierName"].ToString();
				string pType = row["PurchaseType"].ToString();
				string notes = row["Notes"].ToString().ToLower();

				// Filter by type
				if (typeFilter != "الكل")
				{
					if (typeFilter.Contains("نقدي") && pType != "Cash") continue;
					if (typeFilter.Contains("آجل") && pType != "Credit") continue;
				}

				// Filter by search text
				if (!string.IsNullOrEmpty(query))
				{
					if (!code.Contains(query) && !supplier.ToLower().Contains(query) && !notes.Contains(query))
						continue;
				}

				decimal amount    = Convert.ToDecimal(row["TotalAmount"]);
				decimal returnAmt = row.Table.Columns.Contains("ReturnAmount") && row["ReturnAmount"] != DBNull.Value
				                    ? Convert.ToDecimal(row["ReturnAmount"]) : 0m;
				decimal netAmt    = amount - returnAmt;

				total += amount;
				ret   += returnAmt;

				if (pType == "Cash") cash += amount;
				else if (pType == "Credit") credit += amount;

				string displayType = (pType == "Credit") ? "آجل" : "نقدي";
				string retStr = returnAmt > 0 ? returnAmt.ToString("N2") + " ج" : "-";
				dgPurchases.Rows.Add(
					row["PurchaseID"],
					row["PurchaseCode"],
					Convert.ToDateTime(row["PurchaseDate"]).ToString("dd/MM/yyyy HH:mm"),
					displayType,
					supplier,
					amount.ToString("N2") + " ج",
					retStr,
					netAmt.ToString("N2") + " ج",
					row["Notes"]
				);
			}

			UpdateSummary(total, ret, cash, credit);
		}

		private void UpdateSummary(decimal total, decimal ret, decimal cash, decimal credit)
		{
			lblTotalSummary.Text  = total.ToString("N2") + " ج";
			lblReturnSummary.Text = ret.ToString("N2")   + " ج";
			lblNetSummary.Text    = (total - ret).ToString("N2") + " ج";
			lblCashSummary.Text   = cash.ToString("N2")  + " ج";
			lblCreditSummary.Text = credit.ToString("N2")+ " ج";
		}

		private void DgPurchases_SelectionChanged(object sender, EventArgs e)
		{
			dgItems.Rows.Clear();
			if (dgPurchases.SelectedRows.Count == 0) return;

			int purchaseID = Convert.ToInt32(dgPurchases.SelectedRows[0].Cells["PurchaseID"].Value);
			DataTable items = PurchaseDAL.GetItems(purchaseID);

			foreach (DataRow row in items.Rows)
			{
				decimal itemDiscPct = 0;
				decimal itemDiscAmt = 0;
				if (row.Table.Columns.Contains("DiscountPct") && row["DiscountPct"] != DBNull.Value)
				{
					itemDiscPct = Convert.ToDecimal(row["DiscountPct"]);
				}
				if (row.Table.Columns.Contains("DiscountAmt") && row["DiscountAmt"] != DBNull.Value)
				{
					itemDiscAmt = Convert.ToDecimal(row["DiscountAmt"]);
				}

				string discText = "-";
				if (itemDiscPct > 0)
				{
					discText = $"{itemDiscPct:0.##}%";
				}
				else if (itemDiscAmt > 0)
				{
					discText = itemDiscAmt.ToString("N2");
				}

				dgItems.Rows.Add(
					row["ProductName"],
					Convert.ToDecimal(row["Quantity"]).ToString("N2"),
					Convert.ToDecimal(row["UnitPrice"]).ToString("N2"),
					discText,
					Convert.ToDecimal(row["TotalPrice"]).ToString("N2")
				);
			}
		}
	}
}

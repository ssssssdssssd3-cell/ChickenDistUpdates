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
			base.Size = new Size(1200, 750);
			base.StartPosition = FormStartPosition.CenterScreen;
			RightToLeft = RightToLeft.Yes;
			RightToLeftLayout = true;
			BackColor = Theme.BgMain;
			Font = Theme.FontMain;

			// ─── شريط الفلتر (ثابت أعلى) ───
			FlowLayoutPanel filterPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				FlowDirection = FlowDirection.LeftToRight,
				BackColor = Theme.BgCard,
				Padding = new Padding(8, 8, 8, 8),
				WrapContents = true
			};

			Label lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
			dtpFrom = new DateTimePicker { Width = 110, Height = 26, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
			filterPanel.Controls.AddRange(new Control[] { lblFrom, dtpFrom });

			Label lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
			dtpTo = new DateTimePicker { Width = 110, Height = 26, Format = DateTimePickerFormat.Short };
			filterPanel.Controls.AddRange(new Control[] { lblTo, dtpTo });

			Label lblType = new Label { Text = "النوع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
			cboTypeFilter = new ComboBox { Width = 110, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
			cboTypeFilter.Items.AddRange(new object[] { "الكل", "نقدي (Cash)", "آجل (Credit)" });
			cboTypeFilter.SelectedIndex = 0;
			cboTypeFilter.SelectedIndexChanged += delegate { FilterData(); };
			filterPanel.Controls.AddRange(new Control[] { lblType, cboTypeFilter });

			Label lblSupplier = new Label { Text = "المورد:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
			cboSupplierFilter = new ComboBox { Width = 130, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
			cboSupplierFilter.Items.Add(new ComboItem(0, "الكل"));
			foreach (DataRow row in SupplierDAL.GetAll(true).Rows)
				cboSupplierFilter.Items.Add(new ComboItem((int)row["SupplierID"], row["SupplierName"].ToString()));
			cboSupplierFilter.DisplayMember = "Text";
			cboSupplierFilter.SelectedIndex = 0;
			cboSupplierFilter.SelectedIndexChanged += delegate { LoadPurchases(); };
			filterPanel.Controls.AddRange(new Control[] { lblSupplier, cboSupplierFilter });

			Label lblProduct = new Label { Text = "بحث صنف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
			txtProductSearch = new TextBox { Width = 120, Height = 26, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
			txtProductSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadPurchases(); e.Handled = true; e.SuppressKeyPress = true; } };
			filterPanel.Controls.AddRange(new Control[] { lblProduct, txtProductSearch });

			Label lblSearch = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
			txtSearch = new TextBox { Width = 140, Height = 26, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
			txtSearch.TextChanged += delegate { FilterData(); };
			filterPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch });

			btnLoad = Theme.MakeButton("🔄 عرض", Theme.Accent);
			btnLoad.Size = new Size(80, 28);
			btnLoad.Margin = new Padding(15, 0, 0, 0);
			btnLoad.Click += delegate { LoadPurchases(); };
			filterPanel.Controls.Add(btnLoad);

			var btnPrint = Theme.MakeButton("🖨️ طباعة الفاتورة", Color.FromArgb(40, 100, 180));
			btnPrint.Size = new Size(130, 28);
			btnPrint.Margin = new Padding(6, 0, 0, 0);
			btnPrint.Click += BtnPrintPurchase_Click;
			filterPanel.Controls.Add(btnPrint);

			btnNewPurchase = Theme.MakeButton("➕ فاتورة شراء جديدة", Color.FromArgb(40, 150, 80));
			btnNewPurchase.Size = new Size(150, 28);
			btnNewPurchase.Margin = new Padding(10, 0, 0, 0);
			btnNewPurchase.Click += delegate { new FrmPurchase().ShowDialog(); LoadPurchases(); };
			filterPanel.Controls.Add(btnNewPurchase);

			// ─── شريط الملخص (ثابت أسفل) ───
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
			for (int i = 0; i < 5; i++) summaryTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
			summaryTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			lblTotalSummary  = AddDashboardCard(summaryTbl, "إجمالي الفواتير:",        "0.00 ج", Theme.Accent,                  0);
			lblReturnSummary = AddDashboardCard(summaryTbl, "إجمالي المرتجعات: ↩",   "0.00 ج", Color.FromArgb(231, 76, 60),   1);
			lblNetSummary    = AddDashboardCard(summaryTbl, "الصافي بعد المرتجع: ✔", "0.00 ج", Color.FromArgb(46, 204, 113),  2);
			lblCashSummary   = AddDashboardCard(summaryTbl, "المشتريات النقدية:",      "0.00 ج", Theme.Success,                 3);
			lblCreditSummary = AddDashboardCard(summaryTbl, "المشتريات الآجلة:",      "0.00 ج", Color.FromArgb(52, 152, 219),  4);

			// ─── منطقة المحتوى: صفان بنسب مرنة ───
			var tblContent = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 2
			};
			tblContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			tblContent.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));  // جريد الفواتير
			tblContent.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));  // تفاصيل الأصناف

			// الصف 0: جريد الفواتير
			dgPurchases = MakeGrid();
			dgPurchases.Margin = new Padding(10, 6, 10, 4);
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseID",        Visible = false });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseCode",      HeaderText = "رقم الفاتورة",   FillWeight = 55f });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierInvoiceNo", HeaderText = "رقم فاتورة المورد", FillWeight = 75f });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseDate",      HeaderText = "التاريخ والوقت", FillWeight = 80f });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseType",      HeaderText = "نوع الفاتورة",   FillWeight = 45f });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName",      HeaderText = "المورد",         FillWeight = 110f });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount",       HeaderText = "قيمة الفاتورة", FillWeight = 60f });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnAmount",      HeaderText = "المرتجع ↩",      FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(231, 76, 60), Alignment = DataGridViewContentAlignment.MiddleCenter } });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetAmount",         HeaderText = "الصافي ✔",       FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Segoe UI", 9f, FontStyle.Bold) } });
			dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",             HeaderText = "الملاحظات",      FillWeight = 110f });
			dgPurchases.SelectionChanged += DgPurchases_SelectionChanged;

			// الصف 1: تفاصيل الأصناف
			var tblDetail = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Margin = new Padding(10, 4, 10, 6)
			};
			tblDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
			tblDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			tblDetail.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

			var pnlDetailLabel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(10) };
			Label lblDetailsTitle = new Label
			{
				Text = "تفاصيل الأصناف بالفاتورة المحددة",
				Dock = DockStyle.Top,
				Height = 60,
				ForeColor = Theme.TextSub,
				Font = Theme.FontBold,
				TextAlign = ContentAlignment.TopCenter
			};
			pnlDetailLabel.Controls.Add(lblDetailsTitle);

			dgItems = MakeGrid();
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف",      FillWeight = 130f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",    HeaderText = "الكمية",     FillWeight = 50f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",   HeaderText = "سعر الوحدة", FillWeight = 50f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Discount",    HeaderText = "الخصم",      FillWeight = 50f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice",  HeaderText = "الإجمالي",   FillWeight = 60f });

			tblDetail.Controls.Add(pnlDetailLabel, 0, 0);
			tblDetail.Controls.Add(dgItems, 1, 0);

			tblContent.Controls.Add(dgPurchases, 0, 0);
			tblContent.Controls.Add(tblDetail, 0, 1);

			base.Controls.Clear();
			base.Controls.Add(tblContent);
			base.Controls.Add(summaryTbl);
			base.Controls.Add(filterPanel);

			filterPanel.SendToBack();
			summaryTbl.SendToBack();
			tblContent.BringToFront();
		}

		private Label AddDashboardCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIndex)
		{
			Panel cardPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(5) };
			Label lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 9f), ForeColor = Theme.TextSub, TextAlign = ContentAlignment.TopRight };
			Label lblVal = new Label { Text = val, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = valColor, TextAlign = ContentAlignment.BottomRight };
			cardPanel.Controls.Add(lblVal);
			cardPanel.Controls.Add(lblTitle);
			parent.Controls.Add(cardPanel, colIndex, 0);
			return lblVal;
		}

		private DataGridView MakeGrid()
		{
			var grid = new DataGridView
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
				ColumnHeadersVisible = true,
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
				ColumnHeadersHeight = 36,
				ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Color.FromArgb(30, 41, 59),
					ForeColor = Color.White,
					Font = new Font("Segoe UI", 10f, FontStyle.Bold),
					Alignment = DataGridViewContentAlignment.MiddleCenter
				},
				EnableHeadersVisualStyles = false
			};

			grid.CellPainting += (s, e) =>
			{
				if (e.RowIndex == -1 && e.ColumnIndex >= 0)
				{
					e.PaintBackground(e.CellBounds, true);
					using (var b = new System.Drawing.Drawing2D.LinearGradientBrush(e.CellBounds, Color.FromArgb(41, 60, 88), Color.FromArgb(24, 38, 60), 90f))
					{
						e.Graphics.FillRectangle(b, e.CellBounds);
					}
					using (var pen = new Pen(Color.FromArgb(70, 90, 120)))
					{
						e.Graphics.DrawRectangle(pen, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
					}
					string headerText = grid.Columns[e.ColumnIndex].HeaderText;
					TextRenderer.DrawText(e.Graphics, headerText, new Font("Segoe UI", 10f, FontStyle.Bold),
						e.CellBounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
					e.Handled = true;
				}
			};

			return grid;
		}

		private void LoadPurchases()
		{
			dgPurchases.Rows.Clear();
			dgItems.Rows.Clear();
			int? supplierID = null;
			if (cboSupplierFilter != null && cboSupplierFilter.SelectedItem is ComboItem ci && ci.ID > 0)
				supplierID = ci.ID;
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
			decimal total = 0m, ret = 0m, cash = 0m, credit = 0m;

			foreach (DataRow row in _allPurchasesDt.Rows)
			{
				string code = row["PurchaseCode"].ToString().ToLower();
				string supplierInvNo = row.Table.Columns.Contains("SupplierInvoiceNo") ? row["SupplierInvoiceNo"].ToString().ToLower() : "";
				string supplier = row["SupplierName"].ToString();
				string pType = row["PurchaseType"].ToString();
				string notes = row["Notes"].ToString().ToLower();

				if (typeFilter != "الكل")
				{
					if (typeFilter.Contains("نقدي") && pType != "Cash") continue;
					if (typeFilter.Contains("آجل") && pType != "Credit") continue;
				}

				if (!string.IsNullOrEmpty(query))
				{
					if (!code.Contains(query) && !supplierInvNo.Contains(query) && !supplier.ToLower().Contains(query) && !notes.Contains(query))
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
					row.Table.Columns.Contains("SupplierInvoiceNo") ? row["SupplierInvoiceNo"].ToString() : "",
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
			lblTotalSummary.Text  = total.ToString("N2")         + " ج";
			lblReturnSummary.Text = ret.ToString("N2")           + " ج";
			lblNetSummary.Text    = (total - ret).ToString("N2") + " ج";
			lblCashSummary.Text   = cash.ToString("N2")          + " ج";
			lblCreditSummary.Text = credit.ToString("N2")        + " ج";
		}

		private void DgPurchases_SelectionChanged(object sender, EventArgs e)
		{
			dgItems.Rows.Clear();
			if (dgPurchases.SelectedRows.Count == 0) return;

			int purchaseID = Convert.ToInt32(dgPurchases.SelectedRows[0].Cells["PurchaseID"].Value);
			DataTable items = PurchaseDAL.GetItems(purchaseID);

			foreach (DataRow row in items.Rows)
			{
				decimal itemDiscPct = 0, itemDiscAmt = 0;
				if (row.Table.Columns.Contains("DiscountPct") && row["DiscountPct"] != DBNull.Value)
					itemDiscPct = Convert.ToDecimal(row["DiscountPct"]);
				if (row.Table.Columns.Contains("DiscountAmt") && row["DiscountAmt"] != DBNull.Value)
					itemDiscAmt = Convert.ToDecimal(row["DiscountAmt"]);

				string discText = "-";
				if (itemDiscPct > 0) discText = $"{itemDiscPct:0.##}%";
				else if (itemDiscAmt > 0) discText = itemDiscAmt.ToString("N2");

				dgItems.Rows.Add(
					row["ProductName"],
					Convert.ToDecimal(row["Quantity"]).ToString("N2"),
					Convert.ToDecimal(row["UnitPrice"]).ToString("N2"),
					discText,
					Convert.ToDecimal(row["TotalPrice"]).ToString("N2")
				);
			}
		}
		private void BtnPrintPurchase_Click(object sender, EventArgs e)
		{
			if (dgPurchases.SelectedRows.Count == 0)
			{
				MessageBox.Show("من فضلك اختر الفاتورة المراد طباعتها أولاً من الجدول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			int purchaseID = Convert.ToInt32(dgPurchases.SelectedRows[0].Cells["PurchaseID"].Value);
			string purchaseCode = dgPurchases.SelectedRows[0].Cells["PurchaseCode"].Value?.ToString() ?? "";

			var menu = new ContextMenuStrip();

			var itemReceipt = new ToolStripMenuItem("🧾 طباعة باركود (Receipt)");
			itemReceipt.Click += (s2, e2) => new FrmPrintPurchase(purchaseID, "Receipt");

			var itemA4 = new ToolStripMenuItem("📄 طباعة فاتورة A4");
			itemA4.Click += (s2, e2) => new FrmPrintPurchase(purchaseID, "A4");

			menu.Items.Add(itemReceipt);
			menu.Items.Add(itemA4);

			if (sender is Control ctrl)
				menu.Show(ctrl, new System.Drawing.Point(0, ctrl.Height));
		}
	}
}

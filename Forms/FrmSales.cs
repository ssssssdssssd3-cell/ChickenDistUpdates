using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
	public class FrmSalesList : Form
	{
		private DataGridView dgSales;

		private DataGridView dgItems;

		private DateTimePicker dtpFrom;

		private DateTimePicker dtpTo;

		private TextBox txtSearch;

		private ComboBox cboTypeFilter;

		private Button btnLoad;

		private Button btnPrint;
		private Button btnEdit;
		private Button btnDelete;
		private Button btnCopy;
		private Button btnNewSale;

		private Label lblTotalSummary;
		private Label lblReturnSummary;
		private Label lblNetSummary;
		private Label lblCashSummary;
		private Label lblCreditSummary;
		private Label lblDriverSummary;
		private Label lblShippingSummary;
		private CheckBox chkOnlyShipping;
		private ComboBox cboClientFilter;
		private ComboBox cboProductFilter;
		private DataTable _allSalesDt;

		public FrmSalesList()
		{
			InitUI();
			LoadSales();
		}

		private void InitUI()
		{
			Text = "سجل المبيعات";
			base.Size = new Size(1366, 768);
			base.MinimumSize = new Size(1024, 600);
			base.StartPosition = FormStartPosition.CenterScreen;
			this.WindowState = FormWindowState.Maximized;
			this.AutoScaleMode = AutoScaleMode.Dpi;
			this.AutoScaleDimensions = new SizeF(96F, 96F);
			RightToLeft = RightToLeft.Yes;
			RightToLeftLayout = true;
			BackColor = Theme.BgMain;
			Font = Theme.FontMain;

			// ─── شريط أدوات البحث — RTL ───
			// في FlowDirection.RightToLeft: العنصر المضاف أولاً يظهر أقصى اليمين
			// لذلك نضيف كل مجموعة (حقل + تسمية) في Panel مستقل لضمان ظهور التسمية يمين الحقل
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				FlowDirection = FlowDirection.RightToLeft,
				BackColor = Theme.BgCard,
				Padding = new Padding(6, 6, 6, 6),
				WrapContents = true
			};

			// دالة مساعدة لإنشاء حاوية (label + control + optional button) بشكل صحيح
			Panel MakeFilterPanel(string labelText, Control inputCtrl, int inputWidth = 115, Button extraBtn = null)
			{
				inputCtrl.Width = inputWidth;
				inputCtrl.Height = 26;
				var lbl = new Label
				{
					Text = labelText,
					AutoSize = true,
					ForeColor = Theme.TextMain,
					Margin = new Padding(0, 5, 0, 0),
					Font = Theme.FontMain
				};
				var pnl = new Panel
				{
					Height = 36,
					AutoSize = true,
					AutoSizeMode = AutoSizeMode.GrowAndShrink,
					BackColor = Color.Transparent,
					Margin = new Padding(4, 0, 0, 0),
					Padding = new Padding(0)
				};

				int extraW = extraBtn != null ? extraBtn.Width + 2 : 0;
				int lblW = TextRenderer.MeasureText(labelText, Theme.FontMain).Width + 4;
				int totalW = inputWidth + extraW + lblW + 6;

				lbl.Location = new Point(inputWidth + extraW + 4, 5);
				inputCtrl.Location = new Point(extraW, 4);

				if (extraBtn != null)
				{
					extraBtn.Location = new Point(0, 4);
					pnl.Controls.Add(extraBtn);
				}

				pnl.Width = totalW;
				pnl.Controls.Add(inputCtrl);
				pnl.Controls.Add(lbl);
				return pnl;
			}

			// من التاريخ
			dtpFrom = new DateTimePicker
			{
				Format = DateTimePickerFormat.Short,
				Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
			};
			flowLayoutPanel.Controls.Add(MakeFilterPanel("من:", dtpFrom, 110));

			// إلى التاريخ
			dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short };
			flowLayoutPanel.Controls.Add(MakeFilterPanel("إلى:", dtpTo, 110));

			// نوع الفاتورة
			cboTypeFilter = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes
			};
			cboTypeFilter.Items.AddRange(new object[5] { "الكل", "نقدي (Cash)", "آجل (Credit)", "تقسيط شرعي", "تحميل مندوب" });
			cboTypeFilter.SelectedIndex = 0;
			cboTypeFilter.SelectedIndexChanged += delegate { FilterData(); };
			flowLayoutPanel.Controls.Add(MakeFilterPanel("نوع الفاتورة:", cboTypeFilter, 115));

			// العميل + زر البحث المباشر
			cboClientFilter = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDown,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes,
				AutoCompleteMode = AutoCompleteMode.SuggestAppend,
				AutoCompleteSource = AutoCompleteSource.ListItems
			};
			cboClientFilter.Items.Add(new ComboItem(0, "الكل"));
			foreach (DataRow row in ClientDAL.GetAll(true).Rows)
				cboClientFilter.Items.Add(new ComboItem((int)row["ClientID"], row["ClientName"].ToString()));
			cboClientFilter.DisplayMember = "Text";
			cboClientFilter.SelectedIndex = 0;
			cboClientFilter.SelectedIndexChanged += delegate { LoadSales(); };
			cboClientFilter.KeyDown += (s, e) =>
			{
				if (e.KeyCode == Keys.Enter) { LoadSales(); e.Handled = true; e.SuppressKeyPress = true; }
			};

			Button btnClientSearchDlg = new Button
			{
				Text = "🔍",
				Width = 32,
				Height = 26,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(70, 80, 95),
				ForeColor = Color.White,
				Cursor = Cursors.Hand
			};
			btnClientSearchDlg.FlatAppearance.BorderSize = 0;
			btnClientSearchDlg.Click += (s, e) =>
			{
				using (var frm = new FrmClientSearch())
				{
					if (frm.ShowDialog() == DialogResult.OK && frm.SelectedClientID > 0)
					{
						int cid = frm.SelectedClientID;
						for (int i = 0; i < cboClientFilter.Items.Count; i++)
						{
							if (cboClientFilter.Items[i] is ComboItem ci && ci.ID == cid)
							{
								cboClientFilter.SelectedIndex = i;
								break;
							}
						}
						LoadSales();
					}
				}
			};
			flowLayoutPanel.Controls.Add(MakeFilterPanel("اسم العميل:", cboClientFilter, 140, btnClientSearchDlg));

			// ─── قائمة منسدلة للصنف + زر البحث الشامل ───
			cboProductFilter = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDown,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes,
				AutoCompleteMode = AutoCompleteMode.SuggestAppend,
				AutoCompleteSource = AutoCompleteSource.ListItems
			};
			cboProductFilter.Items.Add(new ComboItem(0, "الكل"));
			foreach (DataRow row in ProductDAL.GetAll(true).Rows)
				cboProductFilter.Items.Add(new ComboItem((int)row["ProductID"], row["ProductName"].ToString()));
			cboProductFilter.DisplayMember = "Text";
			cboProductFilter.SelectedIndex = 0;
			cboProductFilter.SelectedIndexChanged += delegate { LoadSales(); };
			cboProductFilter.KeyDown += (s, e) =>
			{
				if (e.KeyCode == Keys.Enter) { LoadSales(); e.Handled = true; e.SuppressKeyPress = true; }
			};

			Button btnProductSearchDlg = new Button
			{
				Text = "🔍",
				Width = 32,
				Height = 26,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(70, 80, 95),
				ForeColor = Color.White,
				Cursor = Cursors.Hand
			};
			btnProductSearchDlg.FlatAppearance.BorderSize = 0;
			btnProductSearchDlg.Click += (s, e) =>
			{
				using (var frm = new FrmProductSearch())
				{
					if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
					{
						int pid = frm.SelectedProductID;
						for (int i = 0; i < cboProductFilter.Items.Count; i++)
						{
							if (cboProductFilter.Items[i] is ComboItem ci && ci.ID == pid)
							{
								cboProductFilter.SelectedIndex = i;
								break;
							}
						}
						LoadSales();
					}
				}
			};
			flowLayoutPanel.Controls.Add(MakeFilterPanel("اسم الصنف:", cboProductFilter, 140, btnProductSearchDlg));

			// بحث سريع (فلترة محلية)
			txtSearch = new TextBox
			{
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes
			};
			txtSearch.TextChanged += delegate { FilterData(); };
			flowLayoutPanel.Controls.Add(MakeFilterPanel("🔍 بحث سريع:", txtSearch, 110));

			chkOnlyShipping = new CheckBox
			{
				Text = "بها شحن فقط",
				ForeColor = Theme.TextMain,
				AutoSize = true,
				Margin = new Padding(4, 8, 4, 0),
				Font = Theme.FontMain
			};
			chkOnlyShipping.CheckedChanged += delegate { FilterData(); };
			flowLayoutPanel.Controls.Add(chkOnlyShipping);

			// زر عرض
			btnLoad = Theme.MakeButton("🔄 عرض", Theme.Accent);
			btnLoad.Size = new Size(80, 34);
			btnLoad.Margin = new Padding(4, 0, 0, 0);
			btnLoad.Click += delegate { LoadSales(); };
			flowLayoutPanel.Controls.Add(btnLoad);

			// زر فاتورة جديدة
			btnNewSale = Theme.MakeButton("➕ فاتورة جديدة", Color.FromArgb(40, 150, 80));
			btnNewSale.Size = new Size(130, 34);
			btnNewSale.Margin = new Padding(4, 0, 4, 0);
			btnNewSale.Click += delegate
			{
				FrmSale frmSale = new FrmSale();
				frmSale.ShowDialog();
				LoadSales();
			};
			flowLayoutPanel.Controls.Add(btnNewSale);
			// ─── منطقة المحتوى: صفان بنسب مرنة ───
			TableLayoutPanel tblContent = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 2,
				RightToLeft = RightToLeft.Yes
			};
			tblContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			tblContent.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));  // جريد الفواتير (dgSales)
			tblContent.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));  // تفاصيل الأصناف والتحكم (tblDetail)

			dgSales = MakeGrid();
			dgSales.Margin = new Padding(10, 6, 10, 4);
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "SaleID",
				Visible = false
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "SaleCode",
				HeaderText = "رقم الفاتورة",
				FillWeight = 50f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "SaleDate",
				HeaderText = "التاريخ والوقت",
				FillWeight = 70f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "SaleType",
				HeaderText = "نوع الفاتورة",
				FillWeight = 45f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ClientName",
				HeaderText = "العميل / المندوب",
				FillWeight = 110f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "TotalAmount",
				HeaderText = "قيمة الفاتورة",
				FillWeight = 55f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ShippingCharge",
				HeaderText = "خدمة شحن",
				FillWeight = 50f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ReturnAmount",
				HeaderText = "المرتجع ↩",
				FillWeight = 50f,
				DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(231, 76, 60), Alignment = DataGridViewContentAlignment.MiddleCenter }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "NetAmount",
				HeaderText = "الصافي ✔",
				FillWeight = 50f,
				DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "CreatedByName",
				HeaderText = "القائم بالحركة",
				FillWeight = 65f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Notes",
				HeaderText = "الملاحظات",
				FillWeight = 100f
			});
			dgSales.SelectionChanged += DgSales_SelectionChanged;

			// الصف 1: تفاصيل الأصناف والتحكم
			TableLayoutPanel tblDetail = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Margin = new Padding(10, 4, 10, 6),
				RightToLeft = RightToLeft.Yes
			};
			tblDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f)); // لوحة الأزرار
			tblDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));   // جريد تفاصيل الفاتورة
			tblDetail.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

			FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.TopDown,
				BackColor = Theme.BgCard,
				Padding = new Padding(15, 4, 15, 15),
				WrapContents = false,
				AutoScroll = true
			};

			btnPrint = Theme.MakeButton("🖨️ طباعة الفاتورة", Theme.Primary);
			btnPrint.Size = new Size(190, 36);
			btnPrint.Margin = new Padding(0, 0, 0, 8);
			btnPrint.Click += BtnPrint_Click;
			flowLayoutPanel2.Controls.Add(btnPrint);

			btnEdit = Theme.MakeButton("📝 تعديل الفاتورة", Theme.Accent);
			btnEdit.Size = new Size(190, 36);
			btnEdit.Margin = new Padding(0, 0, 0, 8);
			btnEdit.Click += BtnEdit_Click;
			flowLayoutPanel2.Controls.Add(btnEdit);

			btnDelete = Theme.MakeButton("🗑️ إلغاء وحذف الفاتورة", Theme.Danger);
			btnDelete.Size = new Size(190, 36);
			btnDelete.Margin = new Padding(0, 0, 0, 8);
			btnDelete.Click += BtnDelete_Click;
			flowLayoutPanel2.Controls.Add(btnDelete);

			btnCopy = Theme.MakeButton("📄 نسخ الفاتورة", Color.FromArgb(40, 120, 180));
			btnCopy.Size = new Size(190, 36);
			btnCopy.Margin = new Padding(0, 0, 0, 15);
			btnCopy.Click += BtnCopy_Click;
			flowLayoutPanel2.Controls.Add(btnCopy);

			Label value = new Label
			{
				Text = "تفاصيل الأصناف بالفاتورة المحددة",
				Size = new Size(190, 60),
				ForeColor = Theme.TextSub,
				Font = Theme.FontBold,
				TextAlign = ContentAlignment.TopCenter,
				Margin = new Padding(0)
			};
			flowLayoutPanel2.Controls.Add(value);

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

			tblDetail.Controls.Add(flowLayoutPanel2, 0, 0);
			tblDetail.Controls.Add(dgItems, 1, 0);

			tblContent.Controls.Add(dgSales, 0, 0);
			tblContent.Controls.Add(tblDetail, 0, 1);

			TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 70,
				ColumnCount = 7,
				RowCount = 1,
				RightToLeft = RightToLeft.Yes,
				BackColor = Theme.BgCard,
				Padding = new Padding(10, 5, 10, 5),
				Visible = Session.CanViewSalesTotals("SalesList")
			};
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.28f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.28f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.28f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.28f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.28f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.28f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.28f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			lblTotalSummary  = AddDashboardCard(tableLayoutPanel, "إجمالي الفواتير:",        "0.00 ج", Theme.Accent,                  0);
			lblReturnSummary = AddDashboardCard(tableLayoutPanel, "إجمالي المرتجعات: ↩",   "0.00 ج", Color.FromArgb(231, 76, 60),   1);
			lblNetSummary    = AddDashboardCard(tableLayoutPanel, "الصافي بعد المرتجع: ✔", "0.00 ج", Color.FromArgb(46, 204, 113),  2);
			lblCashSummary   = AddDashboardCard(tableLayoutPanel, "المبيعات النقدية:",       "0.00 ج", Theme.Success,                 3);
			lblCreditSummary = AddDashboardCard(tableLayoutPanel, "المبيعات الآجلة:",       "0.00 ج", Color.FromArgb(52, 152, 219),  4);
			lblDriverSummary = AddDashboardCard(tableLayoutPanel, "حمولات المناديب:",        "0.00 ج", Color.FromArgb(155, 89, 182), 5);
			lblShippingSummary = AddDashboardCard(tableLayoutPanel, "إجمالي الشحن:",         "0.00 ج", Color.FromArgb(243, 156, 18), 6);

			// ترتيب صحيح للرسو والـ Z-Order (DockStyle.Fill يجب أن يكون بأسفل Z-order حتى لا يغطي DockStyle.Bottom)
			base.Controls.Add(tblContent);
			base.Controls.Add(tableLayoutPanel);
			base.Controls.Add(flowLayoutPanel);

			tblContent.SendToBack();
			tableLayoutPanel.BringToFront();
			flowLayoutPanel.BringToFront();

			Theme.ApplyFormRTL(this);
		}

		private Label AddDashboardCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIndex)
		{
			Panel panel = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = Theme.BgCard,
				Padding = new Padding(5)
			};
			Label value = new Label
			{
				Text = title,
				Dock = DockStyle.Top,
				Height = 18,
				Font = new Font("Segoe UI", 9f),
				ForeColor = Theme.TextSub,
				TextAlign = ContentAlignment.TopRight
			};
			Label label = new Label
			{
				Text = val,
				Dock = DockStyle.Fill,
				Font = new Font("Segoe UI", 13f, FontStyle.Bold),
				ForeColor = valColor,
				TextAlign = ContentAlignment.BottomRight
			};
			panel.Controls.Add(label);
			panel.Controls.Add(value);
			parent.Controls.Add(panel, colIndex, 0);
			return label;
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

		private void LoadSales()
		{
			dgSales.Rows.Clear();
			dgItems.Rows.Clear();
			int? clientID = null;
			if (cboClientFilter != null)
			{
				if (cboClientFilter.SelectedItem is ComboItem ci && ci.ID > 0)
				{
					clientID = ci.ID;
				}
				else if (!string.IsNullOrWhiteSpace(cboClientFilter.Text) && cboClientFilter.Text.Trim() != "الكل")
				{
					string searchText = cboClientFilter.Text.Trim();
					foreach (var item in cboClientFilter.Items)
					{
						if (item is ComboItem ci2 && ci2.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							clientID = ci2.ID;
							cboClientFilter.SelectedItem = ci2;
							break;
						}
					}
				}
			}
			string productSearch = null;
			if (cboProductFilter != null)
			{
				if (cboProductFilter.SelectedItem is ComboItem pci && pci.ID > 0)
				{
					productSearch = pci.Text;
				}
				else if (!string.IsNullOrWhiteSpace(cboProductFilter.Text) && cboProductFilter.Text.Trim() != "الكل")
				{
					productSearch = cboProductFilter.Text.Trim();
				}
			}
			_allSalesDt = SaleDAL.GetAll(dtpFrom.Value, dtpTo.Value, clientID, productSearch);
			FilterData();
		}

		private void FilterData()
		{
			dgSales.Rows.Clear();
			dgItems.Rows.Clear();
			if (_allSalesDt == null || _allSalesDt.Rows.Count == 0)
			{
				UpdateSummary(0m, 0m, 0m, 0m, 0m, 0m);
				return;
			}
			string value = txtSearch.Text.Trim().ToLower();
			string text = cboTypeFilter.SelectedItem?.ToString() ?? "الكل";
			decimal tot    = 0m;
			decimal ret    = 0m;
			decimal cash   = 0m;
			decimal credit = 0m;
			decimal driver = 0m;
			decimal shipping = 0m;

			// تعطيل AutoSize أثناء التحميل لتسريع عرض البيانات الكثيرة
			dgSales.SuspendLayout();
			var oldMode = dgSales.AutoSizeColumnsMode;
			dgSales.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

			foreach (DataRow row in _allSalesDt.Rows)
			{
				string text2 = row["SaleCode"].ToString().ToLower();
				string text3 = row["ClientName"].ToString();
				string text4 = ((row.Table.Columns.Contains("DriverName") && row["DriverName"] != DBNull.Value) ? row["DriverName"].ToString() : "---");
				string text5 = row["SaleType"].ToString();
				string text6 = row["Notes"].ToString().ToLower();
				string text7 = text3;
				if (text5 == "DriverLoad" && text4 != "---")
					text7 = text4;

				decimal shippingAmt = row.Table.Columns.Contains("ShippingCharge") && row["ShippingCharge"] != DBNull.Value
				                    ? Convert.ToDecimal(row["ShippingCharge"]) : 0m;

				if (chkOnlyShipping != null && chkOnlyShipping.Checked && shippingAmt <= 0)
					continue;

				if ((!(text != "الكل") || ((!text.Contains("نقدي") || !(text5 != "Cash")) && (!text.Contains("آجل") || !(text5 != "Credit")) && (!text.Contains("تقسيط") || !(text5 != "Installment")) && (!text.Contains("تحميل") || !(text5 != "DriverLoad")))) && (string.IsNullOrEmpty(value) || text2.Contains(value) || text7.ToLower().Contains(value) || text6.Contains(value)))
				{
					decimal num = Convert.ToDecimal(row["TotalAmount"]);
					decimal returnAmt = row.Table.Columns.Contains("ReturnAmount") && row["ReturnAmount"] != DBNull.Value
					                    ? Convert.ToDecimal(row["ReturnAmount"]) : 0m;
					decimal netAmt = num - returnAmt;

					tot += num;
					ret += returnAmt;
					shipping += shippingAmt;
					switch (text5)
					{
						case "Cash":        cash   += num; break;
						case "Credit":      
						case "Installment": credit += num; break;
						case "DriverLoad":  driver += num; break;
					}
					string text8 = (text5 == "Credit") ? "آجل" : (text5 == "Cash") ? "نقدي" : (text5 == "Installment") ? "تقسيط شرعي" : "تحميل مندوب";
					string retStr = returnAmt > 0 ? returnAmt.ToString("N2") + " ج" : "-";
					dgSales.Rows.Add(
						row["SaleID"], row["SaleCode"],
						Convert.ToDateTime(row["SaleDate"]).ToString("dd/MM/yyyy HH:mm"),
						text8, text7,
						num.ToString("N2") + " ج",
						shippingAmt > 0 ? shippingAmt.ToString("N2") + " ج" : "-",
						retStr,
						netAmt.ToString("N2") + " ج",
						row.Table.Columns.Contains("CreatedByName") ? row["CreatedByName"].ToString() : "---",
						row["Notes"]);
				}
			}

			// إعادة تفعيل AutoSize بعد اكتمال التحميل
			dgSales.AutoSizeColumnsMode = oldMode;
			dgSales.ResumeLayout();

			UpdateSummary(tot, ret, cash, credit, driver, shipping);
		}

		private void UpdateSummary(decimal tot, decimal ret, decimal cash, decimal credit, decimal driver, decimal shipping)
		{
			lblTotalSummary.Text  = tot.ToString("N2")         + " ج";
			lblReturnSummary.Text = ret.ToString("N2")         + " ج";
			lblNetSummary.Text    = (tot - ret).ToString("N2") + " ج";
			lblCashSummary.Text   = cash.ToString("N2")        + " ج";
			lblCreditSummary.Text = credit.ToString("N2")      + " ج";
			lblDriverSummary.Text = driver.ToString("N2")      + " ج";
			if (lblShippingSummary != null)
			{
				lblShippingSummary.Text = shipping.ToString("N2") + " ج";
			}
		}

		private void DgSales_SelectionChanged(object sender, EventArgs e)
		{
			dgItems.Rows.Clear();
			if (dgSales.SelectedRows.Count == 0)
			{
				return;
			}
			int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);
			DataTable items = SaleDAL.GetItems(saleID);
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

		private void BtnPrint_Click(object sender, EventArgs e)
		{
			if (dgSales.SelectedRows.Count == 0)
			{
				MessageBox.Show("من فضلك اختر الفاتورة المراد طباعتها أولاً من الجدول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);

			var menu = new ContextMenuStrip();
			
			var itemPrintReceipt = new ToolStripMenuItem("🖨️ طباعة ريسيت حراري (Receipt) - مباشر");
			itemPrintReceipt.Click += (s2, e2) => new FrmPrintSale(saleID, "Receipt", showPreview: false);

			var itemPreviewReceipt = new ToolStripMenuItem("🔍 معاينة ريسيت حراري (Receipt)");
			itemPreviewReceipt.Click += (s2, e2) => new FrmPrintSale(saleID, "Receipt", showPreview: true);

			var itemPrintA4 = new ToolStripMenuItem("📄 طباعة فاتورة ورق (A4/A5) - مباشر");
			itemPrintA4.Click += (s2, e2) => new FrmPrintSale(saleID, "A4", showPreview: false);

			var itemPreviewA4 = new ToolStripMenuItem("🔍 معاينة فاتورة ورق (A4/A5)");
			itemPreviewA4.Click += (s2, e2) => new FrmPrintSale(saleID, "A4", showPreview: true);

			menu.Items.Add(itemPrintReceipt);
			menu.Items.Add(itemPreviewReceipt);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(itemPrintA4);
			menu.Items.Add(itemPreviewA4);

			if (sender is Control ctrl)
			{
				menu.Show(ctrl, new Point(0, ctrl.Height));
			}
			else
			{
				menu.Show(Cursor.Position);
			}
		}

		private void BtnDelete_Click(object sender, EventArgs e)
		{
			if (!Session.CanDeleteSalesInvoice())
			{
				MessageBox.Show("عذراً، ليس لديك صلاحية حذف وإلغاء فواتير المبيعات.", "غير مصرح", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return;
			}

			if (dgSales.SelectedRows.Count == 0)
			{
				MessageBox.Show("من فضلك اختر الفاتورة المراد حذفها أولا\u064b.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);
			string text = dgSales.SelectedRows[0].Cells["SaleCode"].Value.ToString();
			if (!SaleDAL.CanDeleteSale(saleID, out var reason))
			{
				MessageBox.Show(reason, "فشل إلغاء الفاتورة", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			DialogResult dialogResult = MessageBox.Show("هل أنت متأكد من رغبتك في حذف وإلغاء الفاتورة رقم [" + text + "] نهائيا\u064b؟\n\n⚠\ufe0f سيتم عكس جميع الحركات المالية المرتبطة بالفاتورة تلقائيا\u064b (حساب العميل أو الخزنة أو عهدة المندوب).", "تأكيد إلغاء الفاتورة", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			if (dialogResult == DialogResult.Yes)
			{
				if (SaleDAL.DeleteSale(saleID))
				{
					MessageBox.Show("✅ تم إلغاء وحذف الفاتورة وعكس جميع الحركات المالية المرتبطة بها بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					LoadSales();
				}
				else
				{
					MessageBox.Show("❌ فشل عملية الحذف، يرجى مراجعة اتصال قاعدة البيانات.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		private void BtnEdit_Click(object sender, EventArgs e)
		{
			if (dgSales.SelectedRows.Count == 0)
			{
				MessageBox.Show("من فضلك اختر الفاتورة المراد تعديلها أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			if (!Session.CanEditSalesInvoice())
			{
				MessageBox.Show("عذراً، ليس لديك صلاحية تعديل فواتير المبيعات.", "غير مصرح", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return;
			}

			int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);

			if (!SaleDAL.CanEditSale(saleID, out string reason))
			{
				MessageBox.Show(reason, "لا يمكن التعديل", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			FrmSale frmSale = new FrmSale(saleID, isCopyMode: false);
			frmSale.ShowDialog();
			LoadSales();
		}

		private void BtnCopy_Click(object sender, EventArgs e)
		{
			if (!Session.CanCopySalesInvoice())
			{
				MessageBox.Show("عذراً، ليس لديك صلاحية نسخ فواتير المبيعات.", "غير مصرح", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return;
			}

			if (dgSales.SelectedRows.Count == 0)
			{
				MessageBox.Show("من فضلك اختر الفاتورة المراد نسخها أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);

			FrmSale frmSale = new FrmSale(saleID, isCopyMode: true);
			frmSale.ShowDialog();
			LoadSales();
		}
	}
}

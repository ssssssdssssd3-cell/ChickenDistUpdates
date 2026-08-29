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

		private Label lblTotalBeforeDiscountSummary;
		private Label lblDiscountSummary;
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
		private ComboBox cboUserFilter;
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
					RightToLeft = RightToLeft.No,
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

			// من التاريخ والوقت (بالساعة والدقيقة)
			dtpFrom = new DateTimePicker
			{
				Format = DateTimePickerFormat.Custom,
				CustomFormat = "yyyy-MM-dd   hh:mm tt",
				Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0)
			};
			dtpFrom.ValueChanged += delegate { LoadSales(); };
			flowLayoutPanel.Controls.Add(MakeFilterPanel("من:", dtpFrom, 190));

			// إلى التاريخ والوقت (بالساعة والدقيقة)
			dtpTo = new DateTimePicker
			{
				Format = DateTimePickerFormat.Custom,
				CustomFormat = "yyyy-MM-dd   hh:mm tt",
				Value = DateTime.Now
			};
			dtpTo.ValueChanged += delegate { LoadSales(); };
			flowLayoutPanel.Controls.Add(MakeFilterPanel("إلى:", dtpTo, 190));

			// نوع الفاتورة
			cboTypeFilter = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes
			};
			cboTypeFilter.Items.AddRange(new object[] { "الكل", "نقدي (Cash)", "آجل (Credit)", "فيزا (Visa)", "مختلط (Mixed)", "تقسيط شرعي", "تحميل مندوب" });
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
				AutoCompleteSource = AutoCompleteSource.ListItems,
				AutoCompleteMode = AutoCompleteMode.SuggestAppend
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
				AutoCompleteSource = AutoCompleteSource.ListItems,
				AutoCompleteMode = AutoCompleteMode.SuggestAppend
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

			// ─── فلترة الموظف / القائم بالحركة ───
			cboUserFilter = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes
			};
			cboUserFilter.Items.Add(new ComboItem(0, "الكل"));
			try
			{
				foreach (DataRow row in EmployeeDAL.GetAll().Rows)
				{
					cboUserFilter.Items.Add(new ComboItem(Convert.ToInt32(row["EmpID"]), row["EmpName"].ToString()));
				}
			}
			catch { }
			cboUserFilter.DisplayMember = "Text";
			cboUserFilter.SelectedIndex = 0;
			cboUserFilter.SelectedIndexChanged += delegate { LoadSales(); };
			flowLayoutPanel.Controls.Add(MakeFilterPanel("الموظف:", cboUserFilter, 130));

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

			// زر تقرير الأصناف الراكدة
			var btnStagnant = Theme.MakeButton("💤 الأصناف الراكدة", Color.FromArgb(140, 60, 160));
			btnStagnant.Size = new Size(150, 34);
			btnStagnant.Margin = new Padding(4, 0, 4, 0);
			btnStagnant.Click += delegate
			{
				new FrmReports("Sales", 0, "StagnantProducts").ShowDialog();
			};
			flowLayoutPanel.Controls.Add(btnStagnant);
			// ─── منطقة المحتوى: صفان بنسب مرنة (الفواتير أعلاه والأصناف أسفله تحت بعض) ───
			TableLayoutPanel tblContent = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 2,
				RightToLeft = RightToLeft.Yes
			};
			tblContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			tblContent.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));  // جريد الفواتير (dgSales)
			tblContent.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));  // شريط التحكم وتفاصيل الأصناف (tblDetail)

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
				FillWeight = 40f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "SaleDate",
				HeaderText = "التاريخ والوقت",
				FillWeight = 55f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "SaleType",
				HeaderText = "نوع الفاتورة",
				FillWeight = 42f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ClientCode",
				HeaderText = "كود العميل",
				FillWeight = 38f,
				DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ClientName",
				HeaderText = "العميل / المندوب",
				FillWeight = 85f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ItemsCount",
				HeaderText = "عدد الأصناف",
				FillWeight = 38f,
				DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "TotalBeforeDiscount",
				HeaderText = "قبل الخصم",
				FillWeight = 45f,
				DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "DiscountAmount",
				HeaderText = "الخصم ✂",
				FillWeight = 40f,
				DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(249, 115, 22), Alignment = DataGridViewContentAlignment.MiddleRight }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "TotalAmount",
				HeaderText = "بعد الخصم (قبل المرتجع)",
				FillWeight = 52f,
				DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ShippingCharge",
				HeaderText = "خدمة شحن",
				FillWeight = 38f,
				DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ReturnAmount",
				HeaderText = "المرتجع ↩",
				FillWeight = 40f,
				DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(231, 76, 60), Alignment = DataGridViewContentAlignment.MiddleCenter }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "NetAmount",
				HeaderText = "الصافي النهائي ✔",
				FillWeight = 48f,
				DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleRight }
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "CreatedByName",
				HeaderText = "القائم بالحركة",
				FillWeight = 50f
			});
			dgSales.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Notes",
				HeaderText = "الملاحظات",
				FillWeight = 70f
			});
			dgSales.SelectionChanged += DgSales_SelectionChanged;
			dgSales.CellFormatting += (s, e) =>
			{
				if (e.RowIndex >= 0 && dgSales.Columns[e.ColumnIndex].Name == "ClientCode" && e.Value != null)
				{
					string cCode = e.Value.ToString();
					if (cCode == "0" || string.IsNullOrEmpty(cCode))
					{
						e.CellStyle.ForeColor = Color.FromArgb(160, 160, 160);
						e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
					}
					else
					{
						e.CellStyle.ForeColor = Color.FromArgb(16, 185, 129);
						e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
					}
				}
			};
			SetupSalesGridContextMenu();

			// الصف 1: تفاصيل الأصناف والتحكم (تحت بعض بكامل العرض)
			TableLayoutPanel tblDetail = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 2,
				Margin = new Padding(10, 2, 10, 6),
				RightToLeft = RightToLeft.Yes
			};
			tblDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			tblDetail.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // شريط الأزرار الأفقي
			tblDetail.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // جريد تفاصيل الفاتورة

			FlowLayoutPanel pnlDetailBar = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft,
				BackColor = Theme.BgSearchPanel,
				Padding = new Padding(8, 4, 8, 4),
				Margin = new Padding(0),
				WrapContents = false
			};

			Label lblDetailTitle = new Label
			{
				Text = "📋 تفاصيل الأصناف بالفاتورة المحددة:",
				AutoSize = true,
				ForeColor = Theme.TextSearchLabel,
				Font = Theme.FontBold,
				Margin = new Padding(6, 7, 15, 0)
			};
			pnlDetailBar.Controls.Add(lblDetailTitle);

			btnPrint = Theme.MakeButton("🖨️ طباعة الفاتورة", Theme.Primary);
			btnPrint.Size = new Size(150, 30);
			btnPrint.Margin = new Padding(0, 0, 10, 0);
			btnPrint.Click += BtnPrint_Click;
			pnlDetailBar.Controls.Add(btnPrint);

			var btnWhatsApp = Theme.MakeButton("📱 إعادة إرسال واتساب", Color.FromArgb(37, 211, 102));
			btnWhatsApp.Size = new Size(165, 30);
			btnWhatsApp.ForeColor = Color.White;
			btnWhatsApp.Margin = new Padding(0, 0, 10, 0);
			btnWhatsApp.Click += (s, e) =>
			{
				if (dgSales.SelectedRows.Count == 0 || !dgSales.Columns.Contains("SaleID"))
				{
					MessageBox.Show("من فضلك اختر الفاتورة المراد إرسالها أولاً من الجدول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);
				FrmSale.SendSaleInvoiceWhatsApp(saleID, this);
			};
			pnlDetailBar.Controls.Add(btnWhatsApp);

			btnEdit = Theme.MakeButton("📝 تعديل الفاتورة", Theme.Accent);
			btnEdit.Size = new Size(140, 30);
			btnEdit.Margin = new Padding(0, 0, 10, 0);
			btnEdit.Click += BtnEdit_Click;
			pnlDetailBar.Controls.Add(btnEdit);

			btnCopy = Theme.MakeButton("📄 نسخ الفاتورة", Color.FromArgb(40, 120, 180));
			btnCopy.Size = new Size(140, 30);
			btnCopy.Margin = new Padding(0, 0, 10, 0);
			btnCopy.Click += BtnCopy_Click;
			pnlDetailBar.Controls.Add(btnCopy);

			dgItems = MakeGrid();
			dgItems.Margin = new Padding(0, 2, 0, 0);
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ProductName",
				HeaderText = "الصنف",
				FillWeight = 120f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Quantity",
				HeaderText = "الكمية",
				FillWeight = 45f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "UnitPrice",
				HeaderText = "سعر الوحدة",
				FillWeight = 50f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "LastClientPrice",
				HeaderText = "آخر سعر للعميل 🏷️",
				FillWeight = 55f,
				DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(230, 126, 34), Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "IMEI",
				HeaderText = "السيريال",
				FillWeight = 55f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Discount",
				HeaderText = "الخصم",
				FillWeight = 45f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "TotalPrice",
				HeaderText = "الإجمالي",
				FillWeight = 55f
			});

			tblDetail.Controls.Add(pnlDetailBar, 0, 0);
			tblDetail.Controls.Add(dgItems, 0, 1);

			tblContent.Controls.Add(dgSales, 0, 0);
			tblContent.Controls.Add(tblDetail, 0, 1);

			TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 70,
				ColumnCount = 8,
				RowCount = 1,
				RightToLeft = RightToLeft.Yes,
				BackColor = Theme.BgCard,
				Padding = new Padding(6, 4, 6, 4),
				Visible = Session.CanViewSalesTotals("SalesList")
			};
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			lblTotalBeforeDiscountSummary = AddDashboardCard(tableLayoutPanel, "إجمالي قبل الخصم:", "0.00 ج", Color.FromArgb(160, 175, 200), 0);
			lblDiscountSummary             = AddDashboardCard(tableLayoutPanel, "إجمالي الخصومات: ✂", "0.00 ج", Color.FromArgb(249, 115, 22), 1);
			lblTotalSummary               = AddDashboardCard(tableLayoutPanel, "بعد الخصم (قبل المرتجع):", "0.00 ج", Theme.Accent, 2);
			lblReturnSummary              = AddDashboardCard(tableLayoutPanel, "إجمالي المرتجعات: ↩", "0.00 ج", Color.FromArgb(231, 76, 60), 3);
			lblNetSummary                 = AddDashboardCard(tableLayoutPanel, "الصافي النهائي: ✔", "0.00 ج", Color.FromArgb(46, 204, 113), 4);
			lblCashSummary                = AddDashboardCard(tableLayoutPanel, "المبيعات النقدية:", "0.00 ج", Theme.Success, 5);
			lblCreditSummary              = AddDashboardCard(tableLayoutPanel, "المبيعات الآجلة:", "0.00 ج", Color.FromArgb(52, 152, 219), 6);
			lblShippingSummary            = AddDashboardCard(tableLayoutPanel, "إجمالي الشحن:", "0.00 ج", Color.FromArgb(243, 156, 18), 7);

			// ترتيب صحيح للرسو والـ Z-Order (DockStyle.Fill يجب أن يكون في مقدمة Z-order حتى يحسب التخطيط بعد الفلتر وشريط الإجمالي)
			base.Controls.Clear();
			base.Controls.Add(tblContent);
			base.Controls.Add(tableLayoutPanel);
			base.Controls.Add(flowLayoutPanel);

			flowLayoutPanel.SendToBack();
			tableLayoutPanel.SendToBack();
			tblContent.BringToFront();

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
			int? empID = null;
			if (cboUserFilter != null && cboUserFilter.SelectedItem is ComboItem uci && uci.ID > 0)
			{
				empID = uci.ID;
			}
			_allSalesDt = SaleDAL.GetAll(dtpFrom.Value, dtpTo.Value, clientID, productSearch, null, null, empID);
			FilterData();
		}

		private void FilterData()
		{
			dgSales.Rows.Clear();
			dgItems.Rows.Clear();
			if (_allSalesDt == null || _allSalesDt.Rows.Count == 0)
			{
				UpdateSummary(0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
				return;
			}
			string value = txtSearch.Text.Trim().ToLower();
			string text = cboTypeFilter.SelectedItem?.ToString() ?? "الكل";
			decimal totBeforeDisc = 0m;
			decimal totDisc       = 0m;
			decimal totAfterDisc  = 0m;
			decimal ret           = 0m;
			decimal cash          = 0m;
			decimal credit        = 0m;
			decimal driver        = 0m;
			decimal shipping      = 0m;

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

				string clientCode = (row.Table.Columns.Contains("ClientCode") && row["ClientCode"] != DBNull.Value) ? row["ClientCode"].ToString() : "0";
				int itemsCount = (row.Table.Columns.Contains("ItemsCount") && row["ItemsCount"] != DBNull.Value) ? Convert.ToInt32(row["ItemsCount"]) : 0;

				decimal shippingAmt = row.Table.Columns.Contains("ShippingCharge") && row["ShippingCharge"] != DBNull.Value
				                    ? Convert.ToDecimal(row["ShippingCharge"]) : 0m;

				if (chkOnlyShipping != null && chkOnlyShipping.Checked && shippingAmt <= 0)
					continue;

				bool typeMatch = (text == "الكل") ||
					(text.Contains("نقدي") && text5 == "Cash") ||
					(text.Contains("آجل") && text5 == "Credit") ||
					(text.Contains("فيزا") && text5 == "Visa") ||
					(text.Contains("مختلط") && text5 == "Mixed") ||
					(text.Contains("تقسيط") && text5 == "Installment") ||
					(text.Contains("تحميل") && text5 == "DriverLoad");

				if (typeMatch && (string.IsNullOrEmpty(value) || text2.Contains(value) || clientCode.ToLower().Contains(value) || text7.ToLower().Contains(value) || text6.Contains(value)))
				{
					decimal discAmt = row.Table.Columns.Contains("DiscountAmount") && row["DiscountAmount"] != DBNull.Value
					                ? Convert.ToDecimal(row["DiscountAmount"]) : 0m;
					decimal num = Convert.ToDecimal(row["TotalAmount"]); // بعد الخصم قبل المرتجع
					decimal beforeDiscAmt = row.Table.Columns.Contains("TotalBeforeDiscount") && row["TotalBeforeDiscount"] != DBNull.Value
					                      ? Convert.ToDecimal(row["TotalBeforeDiscount"])
					                      : num + discAmt;
					decimal returnAmt = row.Table.Columns.Contains("ReturnAmount") && row["ReturnAmount"] != DBNull.Value
					                    ? Convert.ToDecimal(row["ReturnAmount"]) : 0m;
					decimal netAmt = num + shippingAmt - returnAmt; // الصافي النهائي

					totBeforeDisc += beforeDiscAmt;
					totDisc += discAmt;
					totAfterDisc += num;
					ret += returnAmt;
					shipping += shippingAmt;

					switch (text5)
					{
						case "Cash":        cash   += num; break;
						case "Credit":      
						case "Installment": credit += num; break;
						case "DriverLoad":  driver += num; break;
					}
					string text8 = (text5 == "Credit") ? "آجل" :
					               (text5 == "Cash") ? "نقدي" :
					               (text5 == "Visa") ? "فيزا / شبكة" :
					               (text5 == "Mixed") ? "مختلط (كاش + فيزا)" :
					               (text5 == "Installment") ? "تقسيط شرعي" : "تحميل مندوب";
					string beforeDiscStr = beforeDiscAmt.ToString("N2") + " ج";
					string discStr = discAmt > 0 ? discAmt.ToString("N2") + " ج" : "-";
					string afterDiscStr = num.ToString("N2") + " ج";
					string shippingStr = shippingAmt > 0 ? shippingAmt.ToString("N2") + " ج" : "-";
					string retStr = returnAmt > 0 ? returnAmt.ToString("N2") + " ج" : "-";
					string netStr = netAmt.ToString("N2") + " ج";

					int addedIdx = dgSales.Rows.Add(
						row["SaleID"], 
						row["SaleCode"],
						Convert.ToDateTime(row["SaleDate"]).ToString("dd/MM/yyyy HH:mm"),
						text8, 
						clientCode,
						text7,
						itemsCount,
						beforeDiscStr,
						discStr,
						afterDiscStr,
						shippingStr,
						retStr,
						netStr,
						row.Table.Columns.Contains("CreatedByName") ? row["CreatedByName"].ToString() : "---",
						row["Notes"]);

					var addedRow = dgSales.Rows[addedIdx];
					if (clientCode == "0" || string.IsNullOrEmpty(clientCode))
					{
						addedRow.Cells["ClientCode"].Style.ForeColor = Color.FromArgb(160, 160, 160);
						addedRow.Cells["ClientCode"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
					}
					else
					{
						addedRow.Cells["ClientCode"].Style.ForeColor = Color.FromArgb(16, 185, 129);
						addedRow.Cells["ClientCode"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
					}

					if (returnAmt > 0)
					{
						addedRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 240, 138); // اصفر واضح ومميز للفواتير المرتجعة
						addedRow.DefaultCellStyle.ForeColor = Color.Black;
						addedRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(234, 179, 8);
						addedRow.DefaultCellStyle.SelectionForeColor = Color.Black;
					}
				}
			}

			// إعادة تفعيل AutoSize بعد اكتمال التحميل
			dgSales.AutoSizeColumnsMode = oldMode;
			dgSales.ResumeLayout();

			UpdateSummary(totBeforeDisc, totDisc, totAfterDisc, ret, cash, credit, driver, shipping);
		}

		private void UpdateSummary(decimal totBeforeDisc, decimal totDisc, decimal totAfterDisc, decimal ret, decimal cash, decimal credit, decimal driver, decimal shipping)
		{
			if (lblTotalBeforeDiscountSummary != null)
				lblTotalBeforeDiscountSummary.Text = totBeforeDisc.ToString("N2") + " ج";
			if (lblDiscountSummary != null)
				lblDiscountSummary.Text = totDisc.ToString("N2") + " ج";
			lblTotalSummary.Text  = totAfterDisc.ToString("N2") + " ج";
			lblReturnSummary.Text = ret.ToString("N2")         + " ج";
			lblNetSummary.Text    = (totAfterDisc + shipping - ret).ToString("N2") + " ج";
			if (lblCashSummary != null) lblCashSummary.Text = cash.ToString("N2") + " ج";
			if (lblCreditSummary != null) lblCreditSummary.Text = credit.ToString("N2") + " ج";
			if (lblDriverSummary != null) lblDriverSummary.Text = driver.ToString("N2") + " ج";
			if (lblShippingSummary != null) lblShippingSummary.Text = shipping.ToString("N2") + " ج";
		}

		private void DgSales_SelectionChanged(object sender, EventArgs e)
		{
			dgItems.Rows.Clear();
			if (dgSales.SelectedRows.Count == 0)
			{
				return;
			}
			int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);
			object cliObj = DbHelper.Scalar("SELECT ClientID FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleID));
			int clientID = (cliObj != null && cliObj != DBNull.Value) ? Convert.ToInt32(cliObj) : 0;

			DataTable items = SaleDAL.GetItems(saleID);
			foreach (DataRow row in items.Rows)
			{
				int pid = row.Table.Columns.Contains("ProductID") && row["ProductID"] != DBNull.Value ? Convert.ToInt32(row["ProductID"]) : 0;
				decimal? lastPrice = (clientID > 0 && pid > 0) ? SaleDAL.GetLastPriceForClient(pid, clientID) : null;
				string lastPriceStr = lastPrice.HasValue ? lastPrice.Value.ToString("N2") + " ج" : "-";

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

				string imeiVal = row.Table.Columns.Contains("IMEI") && row["IMEI"] != DBNull.Value ? row["IMEI"].ToString() : "-";
				if (string.IsNullOrWhiteSpace(imeiVal)) imeiVal = "-";

				int addedItemIdx = dgItems.Rows.Add(
					row["ProductName"], 
					Convert.ToDecimal(row["Quantity"]).ToString("N2"), 
					Convert.ToDecimal(row["UnitPrice"]).ToString("N2") + " ج", 
					lastPriceStr,
					imeiVal,
					discText,
					Convert.ToDecimal(row["TotalPrice"]).ToString("N2") + " ج"
				);

				if (row.Table.Columns.Contains("PrevReturnedQty") && row["PrevReturnedQty"] != DBNull.Value && Convert.ToDecimal(row["PrevReturnedQty"]) > 0)
				{
					var addedItemRow = dgItems.Rows[addedItemIdx];
					addedItemRow.DefaultCellStyle.BackColor = Color.FromArgb(70, 80, 95); // رصاصي غامق مميز للأصناف المسترجعة
					addedItemRow.DefaultCellStyle.ForeColor = Color.White;
					addedItemRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 55, 70);
					addedItemRow.DefaultCellStyle.SelectionForeColor = Color.Yellow;
				}
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

			var itemPrintA4 = new ToolStripMenuItem("📄 طباعة فاتورة ورق (A4 كامل) - مباشر");
			itemPrintA4.Click += (s2, e2) => new FrmPrintSale(saleID, "A4", showPreview: false);

			var itemPreviewA4 = new ToolStripMenuItem("🔍 معاينة فاتورة ورق (A4 كامل)");
			itemPreviewA4.Click += (s2, e2) => new FrmPrintSale(saleID, "A4", showPreview: true);

			var itemPrintA5 = new ToolStripMenuItem("📑 طباعة فاتورة ورق (A5 نصف صفحة) - مباشر");
			itemPrintA5.Click += (s2, e2) => new FrmPrintSale(saleID, "A5", showPreview: false);

			var itemPreviewA5 = new ToolStripMenuItem("🔍 معاينة فاتورة ورق (A5 نصف صفحة)");
			itemPreviewA5.Click += (s2, e2) => new FrmPrintSale(saleID, "A5", showPreview: true);

			menu.Items.Add(itemPrintReceipt);
			menu.Items.Add(itemPreviewReceipt);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(itemPrintA4);
			menu.Items.Add(itemPreviewA4);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(itemPrintA5);
			menu.Items.Add(itemPreviewA5);

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
			MessageBox.Show(
				"⛔ نعتذر! غير مسموح بحذف الفواتير الصادرة نهائياً من قاعدة البيانات بعد اعتمادها وإغلاقها للحفاظ على السلامة المالية والرقابة المحاسبية.\n\n" +
				"💡 لتصحيح أي فاتورة، يرجى استخدام (شاشة مرتجع المبيعات) لعمل مرتجع كلي أو جزئي أو إصدار قيد تسوية.",
				"حظر حذف الفواتير المحاسبي",
				MessageBoxButtons.OK,
				MessageBoxIcon.Warning);
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

		private void SetupSalesGridContextMenu()
		{
			var ctx = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };

			var miPrint = new ToolStripMenuItem("🖨️ طباعة الفاتورة", null, (s, e) => BtnPrint_Click(s, e));
			var miWhatsApp = new ToolStripMenuItem("📱 إرسال الفاتورة عبر واتساب", null, (s, e) =>
			{
				if (dgSales.SelectedRows.Count > 0 && dgSales.Columns.Contains("SaleID"))
				{
					int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);
					FrmSale.SendSaleInvoiceWhatsApp(saleID, this);
				}
			});
			var miEdit = new ToolStripMenuItem("📝 تعديل الفاتورة", null, (s, e) => BtnEdit_Click(s, e));
			var miCopy = new ToolStripMenuItem("📄 نسخ الفاتورة", null, (s, e) => BtnCopy_Click(s, e));
			var miReturn = new ToolStripMenuItem("↩️ فتح شاشة المرتجعات", null, (s, e) =>
			{
				new FrmReturn().ShowDialog(this);
			});
			var miStatement = new ToolStripMenuItem("👤 كشف حساب العميل", null, (s, e) =>
			{
				if (dgSales.SelectedRows.Count > 0 && dgSales.Columns.Contains("SaleID"))
				{
					int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);
					string cname = dgSales.SelectedRows[0].Cells["ClientName"].Value?.ToString() ?? "";
					object cidObj = DbHelper.Scalar("SELECT ClientID FROM Sales WHERE SaleID = @id", DbHelper.P("@id", saleID));
					if (cidObj != null && int.TryParse(cidObj.ToString(), out int cid) && cid > 0)
					{
						new FrmClientStatement(cid, cname, 0).ShowDialog(this);
					}
					else
					{
						MessageBox.Show("هذه الفاتورة نقدية وليست لعميل مسجل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
			});
			var miAudit = new ToolStripMenuItem("🔒 سجل تعديلات وعمليات الفواتير", null, (s, e) =>
			{
				new FrmSalesAuditList().ShowDialog(this);
			});
			var miCopyCode = new ToolStripMenuItem("📋 نسخ رقم الفاتورة", null, (s, e) =>
			{
				if (dgSales.SelectedRows.Count > 0 && dgSales.Columns.Contains("SaleCode"))
				{
					string code = dgSales.SelectedRows[0].Cells["SaleCode"].Value?.ToString() ?? "";
					if (!string.IsNullOrEmpty(code))
					{
						Clipboard.SetText(code);
					}
				}
			});

			ctx.Items.AddRange(new ToolStripItem[] {
				miPrint,
				miWhatsApp,
				miEdit,
				miCopy,
				miReturn,
				new ToolStripSeparator(),
				miStatement,
				miAudit,
				new ToolStripSeparator(),
				miCopyCode
			});

			dgSales.ContextMenuStrip = ctx;
			dgSales.MouseDown += (s, e) =>
			{
				if (e.Button == MouseButtons.Right)
				{
					var hit = dgSales.HitTest(e.X, e.Y);
					if (hit.RowIndex >= 0)
					{
						dgSales.ClearSelection();
						dgSales.Rows[hit.RowIndex].Selected = true;
						dgSales.CurrentCell = dgSales.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
					}
				}
			};
		}
	}
}

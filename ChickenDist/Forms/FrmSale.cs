using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
	public class FrmSale : Form
	{
		private Panel pnlHeader;

		private Panel pnlItems;

		private Panel pnlFooter;

		private Label lblTitle;

		private Button btnTypeCredit;

		private Button btnTypeCash;

		private Button btnTypeDriverLoad;

		private string _invoiceType = "Credit";

		private Label lblClient;

		private Label lblDriver;

		private Label lblDate;

		private Label lblNotes;

		private ComboBox cboClient;

		private ComboBox cboDriver;

		private DateTimePicker dtpDate;

		private TextBox txtNotes;

		private Button btnAddItem;

		private Button btnSave;

		private Button btnNew;

		private Button btnPrint;

		private Button btnWhatsApp;

		private Button btnSearchProduct;

		private DataGridView dgItems;

		private Label lblTotalVal;

		private ComboBox cboInvoiceDiscountType;

		private TextBox txtInvoiceDiscount;

		private Label lblNetVal;
		private Label lblCostTitle;
		private Label lblCostVal;
		private Label lblProfitTitle;
		private Label lblProfitVal;

		private ComboBox cboProduct;

		private NumericUpDown nudQty;

		private TextBox txtPrice;

		private List<SaleItemDTO> _items = new List<SaleItemDTO>();
		// FIX: cache أرصدة المخزون لتفادي رحلة DB لكل صنف عند الاختيار
		private Dictionary<int, decimal> _stockCache = new Dictionary<int, decimal>();

		private int _lastSaleID = 0;
        private bool _isDirty = false;
        private int _editSaleID = 0;
        private bool _isCopyMode = false;
        private DateTime _loadedLastModified;
		private Button btnTierRetail;
		private Button btnTierSemi;
		private Button btnTierWholesale;
		private string _selectedTier = "قطاعي";
		private ComboBox cboWarehouse;
		private Button btnCustomizeCols; // زر تخصيص الأعمدة
		private Label lblLiveWeight;
		private decimal? _scaleBarcodeWeight = null;

		public FrmSale() : this(0, false)
		{
		}

		public FrmSale(int saleID, bool isCopyMode = false)
		{
			_editSaleID = isCopyMode ? 0 : saleID;
			_isCopyMode = isCopyMode;
			InitUI();
			LoadCombos();
			if (saleID > 0)
			{
				LoadInvoiceForEdit(saleID);
			}
		}

		private void InitUI()
		{
			Text = "شاشة المبيعات";
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
            KeyPreview = true;
            this.KeyDown += FrmSale_KeyDown;
            this.FormClosing += FrmSale_FormClosing;
			int hdrH = ScreenHelper.IsSmallScreen ? 155 : 185;
			Panel panel = new Panel
			{
				Dock = DockStyle.Top,
				Height = hdrH,
				BackColor = Theme.BgCard,
				Padding = new Padding(12, 8, 12, 8)
			};
			var tbl = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				RowCount = 4,
				ColumnCount = 6,
				BackColor = Color.Transparent,
				CellBorderStyle = TableLayoutPanelCellBorderStyle.None
			};
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // col0: label
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));  // col1: control
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // col2: label
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));  // col3: control
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // col4: label
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));  // col5: control / buttons

			int rowH = ScreenHelper.IsSmallScreen ? 36 : 42;
			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));

			// Row 0: Client & Statement, Date, Type Buttons
			lblClient = MakeLabel("العميل :", 0, 0);
			lblClient.Dock = DockStyle.Fill;
			lblClient.TextAlign = ContentAlignment.MiddleRight;
			lblClient.Margin = new Padding(2);

			var pnlClient = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
			cboClient = new ComboBox
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDown,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 6, 2, 6)
			};
			SetupSearchableCombo(cboClient);

			Button btnClientStatement = new Button
			{
				Text = "📋 كشف",
				Width = 65,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = Theme.Primary,
				ForeColor = Color.White,
				Cursor = Cursors.Hand,
				Dock = DockStyle.Left,
				Margin = new Padding(2, 6, 2, 6)
			};
			btnClientStatement.FlatAppearance.BorderSize = 0;
			btnClientStatement.Click += (s, e) => {
				if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0) {
					new FrmClientStatement(ci.ID, ci.Text).ShowDialog();
				} else {
					MessageBox.Show("الرجاء اختيار عميل أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			};
			pnlClient.Controls.Add(cboClient);
			pnlClient.Controls.Add(btnClientStatement);

			lblDate = MakeLabel("التاريخ :", 0, 0);
			lblDate.Dock = DockStyle.Fill;
			lblDate.TextAlign = ContentAlignment.MiddleRight;
			lblDate.Margin = new Padding(2);

			dtpDate = new DateTimePicker
			{
				Dock = DockStyle.Fill,
				Format = DateTimePickerFormat.Short,
				RightToLeft = RightToLeft.Yes,
				RightToLeftLayout = true,
				Margin = new Padding(2, 6, 2, 6)
			};

			Label label = MakeLabel("نوع الفاتورة :", 0, 0);
			label.Dock = DockStyle.Fill;
			label.TextAlign = ContentAlignment.MiddleRight;
			label.Margin = new Padding(2);

			btnTypeCredit = new Button
			{
				Text = "آجل",
				Width = 65,
				Height = 28,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Margin = new Padding(2)
			};
			btnTypeCredit.FlatAppearance.BorderSize = 0;
			btnTypeCredit.Click += delegate
			{
				SetInvoiceType("Credit");
			};

			btnTypeCash = new Button
			{
				Text = "نقدي",
				Width = 65,
				Height = 28,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Margin = new Padding(2)
			};
			btnTypeCash.FlatAppearance.BorderSize = 0;
			btnTypeCash.Click += delegate
			{
				SetInvoiceType("Cash");
			};

			btnTypeDriverLoad = new Button
			{
				Text = "تحميل مندوب",
				Width = 90,
				Height = 28,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Margin = new Padding(2)
			};
			btnTypeDriverLoad.FlatAppearance.BorderSize = 0;
			btnTypeDriverLoad.Click += delegate
			{
				SetInvoiceType("DriverLoad");
			};

			var pnlTypeBtns = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.RightToLeft,
				BackColor = Color.Transparent,
				Dock = DockStyle.Fill,
				WrapContents = false,
				Margin = new Padding(0, 4, 0, 0)
			};
			pnlTypeBtns.Controls.Add(btnTypeDriverLoad);
			pnlTypeBtns.Controls.Add(btnTypeCash);
			pnlTypeBtns.Controls.Add(btnTypeCredit);

			// Row 1: Driver, Product & Search (spanning 3 columns)
			lblDriver = MakeLabel("المندوب :", 0, 0);
			lblDriver.Dock = DockStyle.Fill;
			lblDriver.TextAlign = ContentAlignment.MiddleRight;
			lblDriver.Margin = new Padding(2);

			cboDriver = new ComboBox
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDown,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 6, 2, 6)
			};
			SetupSearchableCombo(cboDriver);

			Label label2 = MakeLabel("الصنف :", 0, 0);
			label2.Dock = DockStyle.Fill;
			label2.TextAlign = ContentAlignment.MiddleRight;
			label2.Margin = new Padding(2);

			var pnlProduct = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
			cboProduct = new ComboBox
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDown,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 6, 2, 6)
			};
			SetupSearchableCombo(cboProduct);

			btnSearchProduct = new Button
			{
				Text = "🔍",
				Width = 35,
				Height = 28,
				BackColor = Theme.Accent,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Dock = DockStyle.Left,
				Margin = new Padding(2, 6, 2, 6)
			};
			btnSearchProduct.FlatAppearance.BorderSize = 0;
			btnSearchProduct.Click += BtnSearchProduct_Click;

			pnlProduct.Controls.Add(cboProduct);
			pnlProduct.Controls.Add(btnSearchProduct);

			// Background initialization to prevent NullReferenceException:
			nudQty = new NumericUpDown { Value = 1m };
			txtPrice = new TextBox();
			btnAddItem = new Button();

			// Row 2: Notes, Warehouse
			lblNotes = MakeLabel("ملاحظات :", 0, 0);
			lblNotes.Dock = DockStyle.Fill;
			lblNotes.TextAlign = ContentAlignment.MiddleRight;
			lblNotes.Margin = new Padding(2);

			txtNotes = new TextBox
			{
				Dock = DockStyle.Fill,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				BorderStyle = BorderStyle.FixedSingle,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 6, 2, 6)
			};

			var lblWarehouse = MakeLabel("المخزن :", 0, 0);
			lblWarehouse.Dock = DockStyle.Fill;
			lblWarehouse.TextAlign = ContentAlignment.MiddleRight;
			lblWarehouse.Margin = new Padding(2);

			cboWarehouse = new ComboBox
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 6, 2, 6)
			};

			lblLiveWeight = new Label
			{
				Name = "lblLiveWeight",
				Text = "⚖️ الميزان: 0.000 كجم (غير متصل)",
				Dock = DockStyle.Fill,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				ForeColor = Theme.Accent,
				TextAlign = ContentAlignment.MiddleLeft,
				Visible = AppConfig.ScaleEnabled
			};

			// Row 3: Price Tiers (spanning 5 columns)
			Color clrRetailOn    = Color.FromArgb(30,  100, 200);
			Color clrSemiOn      = Color.FromArgb(130,  50, 180);
			Color clrWholesaleOn = Color.FromArgb(200,  90,   0);
			Color clrOff         = Theme.BgInput;

			var lblTierRow = MakeLabel("فئة السعر :", 0, 0);
			lblTierRow.Dock = DockStyle.Fill;
			lblTierRow.TextAlign = ContentAlignment.MiddleRight;
			lblTierRow.ForeColor = Theme.TextSub;
			lblTierRow.Margin = new Padding(2);

			btnTierRetail = new Button
			{
				Text = "🔵 قطاعي",
				Width = 95,
				Height = 28,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = clrRetailOn,
				ForeColor = Color.White,
				Cursor = Cursors.Hand,
				Margin = new Padding(2)
			};
			btnTierRetail.FlatAppearance.BorderSize = 0;
			btnTierRetail.Click += (s, e) => ApplyTierChange("قطاعي");

			btnTierSemi = new Button
			{
				Text = "🟣 نصف جملة",
				Width = 105,
				Height = 28,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = clrOff,
				ForeColor = Theme.TextMain,
				Cursor = Cursors.Hand,
				Margin = new Padding(2)
			};
			btnTierSemi.FlatAppearance.BorderSize = 0;
			btnTierSemi.Click += (s, e) => ApplyTierChange("نصف جملة");

			btnTierWholesale = new Button
			{
				Text = "🟠 جملة",
				Width = 105,
				Height = 28,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = clrOff,
				ForeColor = Theme.TextMain,
				Cursor = Cursors.Hand,
				Margin = new Padding(2)
			};
			btnTierWholesale.FlatAppearance.BorderSize = 0;
			btnTierWholesale.Click += (s, e) => ApplyTierChange("جملة");

			var pnlTierBtns = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.RightToLeft,
				BackColor = Color.Transparent,
				Dock = DockStyle.Fill,
				WrapContents = false,
				Margin = new Padding(0, 4, 0, 0)
			};
			pnlTierBtns.Controls.Add(btnTierWholesale);
			pnlTierBtns.Controls.Add(btnTierSemi);
			pnlTierBtns.Controls.Add(btnTierRetail);

			// Add to TableLayoutPanel
			tbl.Controls.Add(lblClient, 0, 0);
			tbl.Controls.Add(pnlClient, 1, 0);
			tbl.Controls.Add(lblDate, 2, 0);
			tbl.Controls.Add(dtpDate, 3, 0);
			tbl.Controls.Add(label, 4, 0);
			tbl.Controls.Add(pnlTypeBtns, 5, 0);

			tbl.Controls.Add(lblDriver, 0, 1);
			tbl.Controls.Add(cboDriver, 1, 1);
			tbl.Controls.Add(label2, 2, 1);
			tbl.Controls.Add(pnlProduct, 3, 1);
			tbl.SetColumnSpan(pnlProduct, 3);

			tbl.Controls.Add(lblNotes, 0, 2);
			tbl.Controls.Add(txtNotes, 1, 2);
			tbl.Controls.Add(lblWarehouse, 2, 2);
			tbl.Controls.Add(cboWarehouse, 3, 2);
			tbl.Controls.Add(lblLiveWeight, 4, 2);
			tbl.SetColumnSpan(lblLiveWeight, 2);

			tbl.Controls.Add(lblTierRow, 0, 3);
			tbl.Controls.Add(pnlTierBtns, 1, 3);
			tbl.SetColumnSpan(pnlTierBtns, 5);

			panel.Controls.Add(tbl);
			pnlItems = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(5)
			};
			dgItems = new DataGridView
			{
				Dock = DockStyle.Fill,
				BackgroundColor = Theme.BgCard,
				BorderStyle = BorderStyle.None,
				RowHeadersVisible = false,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				ReadOnly = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				RightToLeft = RightToLeft.Yes,
				GridColor = Theme.BorderColor,
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
					Font = new Font("Segoe UI", 10f, FontStyle.Bold)
				},
				EnableHeadersVisualStyles = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
			};
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ProductName",
				HeaderText = "الصنف",
				ReadOnly = true
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "PriceTier",
				HeaderText = "فئة السعر",
				ReadOnly = true,
				FillWeight = 30f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "PartNumber",
				HeaderText = "رقم القطعة",
				ReadOnly = true,
				FillWeight = 40f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "CarModel",
				HeaderText = "الموديل",
				ReadOnly = true,
				FillWeight = 40f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Brand",
				HeaderText = "الماركة",
				ReadOnly = true,
				FillWeight = 40f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ShelfLocation",
				HeaderText = "الرف",
				ReadOnly = true,
				FillWeight = 30f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "StockQty",
				HeaderText = "الرصيد الفعلي",
				ReadOnly = true,
				FillWeight = 40f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Quantity",
				HeaderText = "الكمية",
				ReadOnly = false, // Always editable for speed
				FillWeight = 40f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "UnitPrice",
				HeaderText = "السعر",
				ReadOnly = !Session.CanEditPrice(), // Only editable if user has permission
				FillWeight = 40f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "DiscountPct",
				HeaderText = "خصم %",
				ReadOnly = false,
				FillWeight = 30f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "DiscountAmt",
				HeaderText = "قيمة خصم",
				ReadOnly = false,
				FillWeight = 35f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "TotalPrice",
				HeaderText = "الإجمالي",
				ReadOnly = true,
				FillWeight = 50f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "PurchasePrice",
				HeaderText = "سعر التكلفة",
				ReadOnly = true,
				FillWeight = 40f,
				Visible = Session.CanViewCost("Sales")
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "CostTotal",
				HeaderText = "إجمالي التكلفة",
				ReadOnly = true,
				FillWeight = 50f,
				Visible = Session.CanViewCost("Sales")
			});
			DataGridViewButtonColumn dataGridViewColumn = new DataGridViewButtonColumn
			{
				Name = "Delete",
				HeaderText = "",
				Text = "\ud83d\uddd1",
				UseColumnTextForButtonValue = true,
				FillWeight = 20f
			};
			dgItems.Columns.Add(dataGridViewColumn);
			dgItems.CellClick += DgItems_CellClick;
			dgItems.CellEndEdit += DgItems_CellEndEdit;
            dgItems.RowsAdded += (s, e) => _isDirty = true;
            dgItems.RowsRemoved += (s, e) => _isDirty = true;
			pnlItems.Controls.Add(dgItems);

			// ── زر تخصيص الأعمدة ⚙️ (يظهر في زاوية الجدول) ─────────────────────
			btnCustomizeCols = new Button
			{
				Text      = "⚙️ الأعمدة",
				Size      = new Size(90, 26),
				Anchor    = AnchorStyles.Top | AnchorStyles.Left,
				Location  = new Point(5, 5),
				BackColor = Color.FromArgb(55, 65, 81),
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
				Cursor    = Cursors.Hand
			};
			btnCustomizeCols.FlatAppearance.BorderSize = 0;
			btnCustomizeCols.Click += (s, e) => ShowColumnCustomizer();
			pnlItems.Controls.Add(btnCustomizeCols);
			btnCustomizeCols.BringToFront();

			// تحميل إعدادات الأعمدة المحفوظة
			LoadColumnSettings();
			pnlFooter = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 105,
				BackColor = Theme.BgCard,
				Padding = new Padding(8, 6, 8, 6)
			};

			// ── قسم الإجماليات (يمين) ─────────────────────────────────────────
			var pnlTotals = new Panel
			{
				Width = 650,
				Dock = DockStyle.Right,
				BackColor = Color.Transparent,
				Padding = new Padding(0)
			};

			var tblTotals = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				RowCount = 2,
				ColumnCount = 6,
				BackColor = Color.Transparent,
				CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
				Padding = new Padding(4)
			};
			tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); // col0: label
			tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // col1: value
			tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));  // col2: label
			tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));  // col3: control / value
			tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // col4: label
			tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // col5: control / value
			tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
			tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

			// صف 0: إجمالي الأصناف + خصم الفاتورة
			var lblItemsTotalLbl = new Label
			{
				Text = "إجمالي الأصناف:",
				ForeColor = Theme.TextSub,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleRight,
				Margin = new Padding(2)
			};
			lblTotalVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.TextMain,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(2)
			};

			var lblDiscType = new Label
			{
				Text = "نوع الخصم:",
				ForeColor = Theme.TextSub,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleRight,
				Margin = new Padding(2)
			};
			cboInvoiceDiscountType = new ComboBox
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 4, 2, 4)
			};
			cboInvoiceDiscountType.Items.AddRange(new object[] { "قيمة", "نسبة %" });
			cboInvoiceDiscountType.SelectedIndex = 0;
			cboInvoiceDiscountType.SelectedIndexChanged += (s, e) => CalculateNet();

			var lblDiscVal = new Label
			{
				Text = "خصم الفاتورة:",
				ForeColor = Theme.TextSub,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleRight,
				Margin = new Padding(2)
			};
			txtInvoiceDiscount = new TextBox
			{
				Dock = DockStyle.Fill,
				Text = "0",
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				BorderStyle = BorderStyle.FixedSingle,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 4, 2, 4)
			};
			txtInvoiceDiscount.TextChanged += (s, e) => CalculateNet();

			// صف 1: التكلفة + الربح + صافي الفاتورة
			lblCostTitle = new Label
			{
				Text = "إجمالي التكلفة:",
				ForeColor = Theme.TextSub,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleRight,
				Margin = new Padding(2),
				Visible = Session.CanViewCost("Sales")
			};
			lblCostVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.TextMain,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(2),
				Visible = Session.CanViewCost("Sales")
			};

			lblProfitTitle = new Label
			{
				Text = "صافي الربح:",
				ForeColor = Theme.TextSub,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleRight,
				Margin = new Padding(2),
				Visible = Session.CanViewCost("Sales")
			};
			lblProfitVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.Success,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(2),
				Visible = Session.CanViewCost("Sales")
			};

			var lblNetTitle = new Label
			{
				Text = "صافي الفاتورة:",
				ForeColor = Theme.TextSub,
				Font = new Font("Segoe UI", 10f, FontStyle.Bold),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleRight,
				Margin = new Padding(2)
			};
			lblNetVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.Accent,
				Font = new Font("Segoe UI", 14f, FontStyle.Bold),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(2)
			};

			// إضافة عناصر الإجماليات
			tblTotals.Controls.Add(lblItemsTotalLbl, 0, 0);
			tblTotals.Controls.Add(lblTotalVal, 1, 0);
			tblTotals.Controls.Add(lblDiscType, 2, 0);
			tblTotals.Controls.Add(cboInvoiceDiscountType, 3, 0);
			tblTotals.Controls.Add(lblDiscVal, 4, 0);
			tblTotals.Controls.Add(txtInvoiceDiscount, 5, 0);

			tblTotals.Controls.Add(lblCostTitle, 0, 1);
			tblTotals.Controls.Add(lblCostVal, 1, 1);
			tblTotals.Controls.Add(lblProfitTitle, 2, 1);
			tblTotals.Controls.Add(lblProfitVal, 3, 1);
			tblTotals.Controls.Add(lblNetTitle, 4, 1);
			tblTotals.Controls.Add(lblNetVal, 5, 1);

			pnlTotals.Controls.Add(tblTotals);

			// ── قسم الأزرار (يسار) ────────────────────────────────────────────
			var pnlBtnArea = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent,
				Padding = new Padding(4, 4, 4, 4)
			};

			btnSave = Theme.MakeButton("💾 حفظ الفاتورة", 0, 0, 120, 32, Theme.Accent);
			Button btnHold = Theme.MakeButton("⏸️ تعليق", 0, 0, 90, 32, Color.FromArgb(200, 140, 50));
			Button btnLoadHold = Theme.MakeButton("📂 معلقات", 0, 0, 95, 32, Color.FromArgb(100, 100, 150));
			Button button = Theme.MakeButton("💵 توريد", 0, 0, 90, 32, Theme.Success);
			btnNew = Theme.MakeButton("🆕 جديد", 0, 0, 75, 32, Color.FromArgb(80, 120, 80));
			btnPrint = Theme.MakeButton("🖨️ طباعة الأخيرة", 0, 0, 125, 32, Theme.Primary);
			btnWhatsApp = Theme.MakeButton("📲 واتساب", 0, 0, 100, 32, Color.FromArgb(37, 211, 102));

			btnSave.Click += BtnSave_Click;
			btnHold.Click += BtnHold_Click;
			btnLoadHold.Click += BtnLoadHold_Click;
			button.Click += BtnTawreed_Click;
			btnNew.Click += delegate
			{
				ResetForm();
			};
			btnPrint.Click += BtnPrint_Click;
			btnWhatsApp.Click += BtnWhatsApp_Click;

			var flowBtns = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.RightToLeft,
				Dock = DockStyle.Top,
				Height = 38,
				BackColor = Color.Transparent,
				WrapContents = false,
				Padding = new Padding(0),
				RightToLeft = RightToLeft.Yes
			};
			foreach (var b in new[] { btnWhatsApp, btnPrint, btnNew, button, btnLoadHold, btnHold, btnSave })
			{
				b.Margin = new Padding(0, 0, 5, 0);
				flowBtns.Controls.Add(b);
			}

			var lblHotkeys = new Label
			{
				Text = "الاختصارات: [F2] جديدة | [F5] حفظ | [F9] طباعة | [F12] بحث سريع صنف | [F8] ميزان",
				ForeColor = Theme.TextSub,
				Font = new Font("Segoe UI", 8f, FontStyle.Bold),
				Dock = DockStyle.Bottom,
				Height = 18,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(0, 4, 0, 0)
			};

			pnlBtnArea.Controls.Add(lblHotkeys);
			pnlBtnArea.Controls.Add(flowBtns);

			// تجميع الذيل
			pnlFooter.Controls.Add(pnlBtnArea);
			pnlFooter.Controls.Add(pnlTotals);

			base.Controls.Add(pnlItems);
			base.Controls.Add(pnlFooter);
			base.Controls.Add(panel);
			pnlFooter.BringToFront();
			panel.BringToFront();
			pnlItems.SendToBack();
			ToggleType();
			InitializeScale();
			Theme.ApplyFormRTL(this);
		}

		private void FrmSale_KeyDown(object sender, KeyEventArgs e)
		{
            // Force grid to commit any pending edits before processing hotkeys
            if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.F5 || e.KeyCode == Keys.F9)
            {
                if (dgItems.IsCurrentCellInEditMode)
                {
                    dgItems.EndEdit();
                }
            }

			if (e.KeyCode == Keys.F2) { btnNew.PerformClick(); e.Handled = true; }
			else if (e.KeyCode == Keys.F5) { btnSave.PerformClick(); e.Handled = true; }
			else if (e.KeyCode == Keys.F9) { btnPrint.PerformClick(); e.Handled = true; }
			else if (e.KeyCode == Keys.F12) { cboProduct.Focus(); e.Handled = true; }
			else if (e.KeyCode == Keys.F8) { ReadWeightToSelectedRow(); e.Handled = true; }
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Enter)
			{
				if (dgItems.Focused || dgItems.EditingControl != null)
				{
					var curCell = dgItems.CurrentCell;
					if (curCell != null)
					{
						dgItems.EndEdit();
						// Find next editable cell in the same row
						int nextCol = -1;
						for (int col = curCell.ColumnIndex + 1; col < dgItems.ColumnCount; col++)
						{
							if (!dgItems.Columns[col].ReadOnly && dgItems.Columns[col].Visible)
							{
								nextCol = col;
								break;
							}
						}

						if (nextCol != -1)
						{
							dgItems.CurrentCell = dgItems.Rows[curCell.RowIndex].Cells[nextCol];
							dgItems.BeginEdit(true);
							return true;
						}
						else
						{
							// No more editable cells in this row, go to cboProduct
							cboProduct.Focus();
							return true;
						}
					}
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private Label MakeLabel(string text, int x, int y)
		{
			return new Label
			{
				Text = text,
				Location = new Point(x, y),
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Font = Theme.FontMain
			};
		}

		private void SetupSearchableCombo(ComboBox cbo)
		{
			cbo.AutoCompleteMode = AutoCompleteMode.None;
			cbo.TextUpdate += delegate
			{
				if (cbo.Tag == null)
				{
					List<ComboItem> list = new List<ComboItem>();
					foreach (ComboItem item in cbo.Items)
					{
						list.Add(item);
					}
					cbo.Tag = list;
				}
				List<ComboItem> list2 = (List<ComboItem>)cbo.Tag;
				string text = cbo.Text;

				// ── اعتراض باركود الميزان المطبوع (13 رقم يبدأ بـ 2) ────────────────
				if (cbo == cboProduct && text.Length == 13 && text.StartsWith("2") && decimal.TryParse(text, out _))
				{
					string productCode = text.Substring(2, 5); // كود الصنف (5 أرقام)
					string weightStr = text.Substring(7, 5);     // الوزن بالجرام (5 أرقام)
					
					if (decimal.TryParse(weightStr, out decimal grams))
					{
						decimal parsedWeight = grams / 1000m; // تحويل لكيلوجرام
						int numericCode = int.TryParse(productCode, out int num) ? num : -1;
						ComboItem matchedItem = null;
						foreach (ComboItem item2 in list2)
						{
							if (item2.ID > 0 && (item2.Barcode == productCode || (numericCode != -1 && item2.Barcode == numericCode.ToString())))
							{
								if (item2.PriceTier == _selectedTier)
								{
									matchedItem = item2;
									break;
								}
								if (matchedItem == null || item2.PriceTier == "قطاعي")
								{
									matchedItem = item2;
								}
							}
						}

						if (matchedItem != null)
						{
							_scaleBarcodeWeight = parsedWeight;
							cbo.SelectedItem = matchedItem;
							return;
						}
					}
				}

				cbo.BeginUpdate();
				cbo.Items.Clear();
				if (string.IsNullOrWhiteSpace(text))
				{
					ComboBox.ObjectCollection items = cbo.Items;
					object[] items2 = list2.ToArray();
					items.AddRange(items2);
				}
				else
				{
					foreach (ComboItem item2 in list2)
					{
						bool matches = false;
						if (item2.ID == 0)
						{
							matches = true;
						}
						else if (item2.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							matches = true;
						}
						else if (cbo == cboProduct)
						{
							// البحث الموسع للأصناف ليشمل رقم القطعة والماركة والموديل والباركود الأصلي
							if ((item2.Barcode != null && item2.Barcode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							    (item2.PartNumber != null && item2.PartNumber.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							    (item2.Brand != null && item2.Brand.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							    (item2.CarModel != null && item2.CarModel.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
							{
								matches = true;
							}
						}

						if (matches)
						{
							cbo.Items.Add(item2);
						}
					}
				}
				cbo.EndUpdate();
				cbo.SelectionStart = text.Length;
				cbo.SelectionLength = 0;
				cbo.DroppedDown = true;
			};
		}

		private void LoadCombos()
		{
			// FIX: تحميل كل أرصدة المخزون مرة واحدة بدلاً من رحلة DB لكل صنف
			_stockCache.Clear();
			var stockTable = InventoryDAL.GetStock();
			foreach (DataRow sRow in stockTable.Rows)
				_stockCache[(int)sRow["ProductID"]] = sRow["BookQty"] == DBNull.Value ? 0m : Convert.ToDecimal(sRow["BookQty"]);
			DataTable all = ClientDAL.GetAll(activeOnly: true);
			cboClient.Items.Clear();
			cboClient.Items.Add(new ComboItem(0, "-- اختر عميل --"));
			foreach (DataRow row in all.Rows)
			{
				cboClient.Items.Add(new ComboItem((int)row["ClientID"], row["ClientName"].ToString()));
			}
			cboClient.DisplayMember = "Text";
			cboClient.SelectedIndex = 0;
			cboClient.SelectedIndexChanged += delegate
			{
				if (cboClient.SelectedItem is ComboItem comboItem2 && comboItem2.ID > 0)
				{
					DataRow byID = ClientDAL.GetByID(comboItem2.ID);
					if (byID != null && byID["DriverID"] != DBNull.Value)
					{
						int driverID = Convert.ToInt32(byID["DriverID"]);
						SelectDriverByID(driverID);
					}
					else
					{
						cboDriver.SelectedIndex = 0;
					}
                    // تطبيق فئة السعر الافتراضية للعميل
                    if (byID != null && byID["DefaultPriceTier"] != DBNull.Value)
                    {
                        string clientTier = byID["DefaultPriceTier"].ToString();
                        if (clientTier != _selectedTier)
                            SetTierButtons(clientTier); // تحديث التصميم فقط بدون سؤال
                    }
                    EvaluateClientFinancials(comboItem2.ID);
				}
                else
                {
                    this.BackColor = Theme.BgMain;
                    pnlItems.Enabled = true;
                    btnSave.Enabled = true;
                }
			};
			DataTable drivers = EmployeeDAL.GetDrivers();
			cboDriver.Items.Clear();
			cboDriver.Items.Add(new ComboItem(0, "-- اختر مندوب --"));
			foreach (DataRow row2 in drivers.Rows)
			{
				cboDriver.Items.Add(new ComboItem((int)row2["EmpID"], row2["EmpName"].ToString()));
			}
			cboDriver.DisplayMember = "Text";
			cboDriver.SelectedIndex = 0;
			DataTable all2 = ProductDAL.GetAll(activeOnly: true);
			cboProduct.Items.Clear();
			cboProduct.Items.Add(new ComboItem(0, "-- اختر صنف --"));
			foreach (DataRow row3 in all2.Rows)
			{
				int pid = (int)row3["ProductID"];
				string name = row3["ProductName"].ToString();
				decimal retailPrice = row3["SalePrice"] != DBNull.Value ? Convert.ToDecimal(row3["SalePrice"]) : 0m;
				decimal pendingPrice = row3["PendingSalePrice"] != DBNull.Value ? Convert.ToDecimal(row3["PendingSalePrice"]) : 0m;
				decimal purchasePrice = row3["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row3["PurchasePrice"]) : 0m;
				decimal wholesalePrice = row3["WholesalePrice"] != DBNull.Value ? Convert.ToDecimal(row3["WholesalePrice"]) : 0m;
				decimal semiWholesalePrice = row3["SemiWholesalePrice"] != DBNull.Value ? Convert.ToDecimal(row3["SemiWholesalePrice"]) : 0m;
				decimal minStockLimit = row3["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(row3["MinStockLimit"]) : 0m;
				decimal stock = _stockCache.TryGetValue(pid, out var st) ? st : 0m;

				bool hasWholesale = wholesalePrice > 0 && wholesalePrice != retailPrice;
				bool hasSemi = semiWholesalePrice > 0 && semiWholesalePrice != retailPrice && semiWholesalePrice != wholesalePrice;

				// 1. Retail Price Item
				string retailText;
				if (hasWholesale || hasSemi)
				{
					retailText = pendingPrice > 0 
						? $"{name} - قطاعي (المتاح: {stock:N2} | سعر: {retailPrice:N2} | المعلق: {pendingPrice:N2})"
						: $"{name} - قطاعي (المتاح: {stock:N2} | سعر: {retailPrice:N2})";
				}
				else
				{
					retailText = pendingPrice > 0 
						? $"{name} (المتاح: {stock:N2} | السعر: {retailPrice:N2} | المعلق: {pendingPrice:N2})"
						: $"{name} (المتاح: {stock:N2} | السعر: {retailPrice:N2})";
				}

				var retailItem = new ComboItem(pid, name, retailText, retailPrice, minStockLimit, purchasePrice)
				{
					PriceTier = "قطاعي",
					PartNumber = row3["PartNumber"]?.ToString() ?? "",
					CarModel = row3["CarModel"]?.ToString() ?? "",
					Brand = row3["Brand"]?.ToString() ?? "",
					ShelfLocation = row3["ShelfLocation"]?.ToString() ?? "",
					Barcode = row3["ProductCode"]?.ToString() ?? ""
				};
				cboProduct.Items.Add(retailItem);

				// 2. Wholesale Price Item (if different)
				if (hasWholesale)
				{
					string wholesaleText = $"{name} - جملة (المتاح: {stock:N2} | سعر: {wholesalePrice:N2})";
					var wholesaleItem = new ComboItem(pid, name, wholesaleText, wholesalePrice, minStockLimit, purchasePrice)
					{
						PriceTier = "جملة",
						PartNumber = row3["PartNumber"]?.ToString() ?? "",
						CarModel = row3["CarModel"]?.ToString() ?? "",
						Brand = row3["Brand"]?.ToString() ?? "",
						ShelfLocation = row3["ShelfLocation"]?.ToString() ?? "",
						Barcode = row3["ProductCode"]?.ToString() ?? ""
					};
					cboProduct.Items.Add(wholesaleItem);
				}

				// 3. Semi-Wholesale Price Item (if different)
				if (hasSemi)
				{
					string semiText = $"{name} - نصف جملة (المتاح: {stock:N2} | سعر: {semiWholesalePrice:N2})";
					var semiItem = new ComboItem(pid, name, semiText, semiWholesalePrice, minStockLimit, purchasePrice)
					{
						PriceTier = "نصف جملة",
						PartNumber = row3["PartNumber"]?.ToString() ?? "",
						CarModel = row3["CarModel"]?.ToString() ?? "",
						Brand = row3["Brand"]?.ToString() ?? "",
						ShelfLocation = row3["ShelfLocation"]?.ToString() ?? "",
						Barcode = row3["ProductCode"]?.ToString() ?? ""
					};
					cboProduct.Items.Add(semiItem);
				}
			}
			cboProduct.DisplayMember = "Text";
			cboProduct.SelectedIndex = 0;
			cboProduct.SelectedIndexChanged += delegate
			{
				if (cboProduct.SelectedItem is ComboItem comboItem && comboItem.ID > 0)
				{
					// Check if product with same price tier is already in the list
					foreach (SaleItemDTO item in _items)
					{
						if (item.ProductID == comboItem.ID && item.PriceTier == comboItem.PriceTier)
						{
							MessageBox.Show("الصنف بنفس فئة السعر موجود مسبقاً بالفاتورة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							cboProduct.SelectedIndex = 0;
							return;
						}
					}

					// Verify stock availability
					// FIX: استخدام cache بدلاً من رحلة DB لكل صنف
					decimal stock = _stockCache.TryGetValue(comboItem.ID, out var cached1) ? cached1 : 0m;
					if (stock <= 0)
					{
						MessageBox.Show($"❌ عجز: الصنف '{comboItem.Name}' ليس لديه رصيد كافٍ في المخزن حالياً (الرصيد الحالي: 0)!", "رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						cboProduct.SelectedIndex = 0;
						return;
					}

					// تحديد الكمية بناء على الوزن اللحظي أو وزن باركود الميزان المطبوع
					decimal initialQty = 1.00m;
					if (_scaleBarcodeWeight.HasValue)
					{
						initialQty = _scaleBarcodeWeight.Value;
						_scaleBarcodeWeight = null; // إعادة تعيين
					}
					else if (AppConfig.ScaleEnabled && ScaleService.Instance.IsConnected && ScaleService.Instance.CurrentWeight > 0m)
					{
						initialQty = ScaleService.Instance.CurrentWeight;
					}

					// التحقق من الرصيد الكافي للوزن المقروء
					if (initialQty > stock)
					{
						MessageBox.Show($"❌ خطأ: الكمية المطلوبة ({initialQty:N2}) أكبر من الكمية المتاحة في المخزن حالياً ({stock:N2})!", "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						cboProduct.SelectedIndex = 0;
						return;
					}

					// Add to items list with default Quantity
					_items.Add(new SaleItemDTO
					{
						ProductID = comboItem.ID,
						ProductName = comboItem.Name,
						Quantity = initialQty,
						UnitPrice = comboItem.Price,
						StockQty = stock,
						MinStockLimit = comboItem.MinStockLimit,
						PurchasePrice = comboItem.PurchasePrice,
						PartNumber = comboItem.PartNumber,
						CarModel = comboItem.CarModel,
						Brand = comboItem.Brand,
						ShelfLocation = comboItem.ShelfLocation,
						PriceTier = comboItem.PriceTier
					});

					RefreshGrid();

					// Focus on the newly added row's Quantity cell
					int rowIndex = _items.Count - 1;
					if (rowIndex >= 0)
					{
						dgItems.Focus();
						dgItems.ClearSelection();
						dgItems.CurrentCell = dgItems.Rows[rowIndex].Cells["Quantity"];
						dgItems.BeginEdit(true);
					}

					// Clear combobox selection quietly
					cboProduct.SelectedIndex = 0;
				}
			};
			dtpDate.Value = DateTime.Today;
			SetInvoiceType("Credit");

			// تحميل المخازن
			try
			{
				var whDt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive=1 ORDER BY WarehouseID");
				cboWarehouse.Items.Clear();
				cboWarehouse.DisplayMember = "Text";
				cboWarehouse.ValueMember = "Value";
				foreach (DataRow whRow in whDt.Rows)
				{
					cboWarehouse.Items.Add(new ComboItem(
						Convert.ToInt32(whRow["WarehouseID"]),
						whRow["WarehouseName"].ToString()
					));
				}
				cboWarehouse.DisplayMember = "Text";
				if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
			}
			catch { /* لو مافيش مخازن نكمل بدون خطأ */ }
		}

		/// <summary>
		/// يُطبِّق فئة السعر على جميع البنود الموجودة في الجدول عند تغيير الفئة.
		/// </summary>
		/// <summary>
		/// يُطبِّق فئة السعر المختارة: يُحدِّث الأزرار ويسأل عن تحديث الأصناف إن وُجدت.
		/// </summary>
		private void ApplyTierChange(string newTier)
		{
			SetTierButtons(newTier);
			if (_items.Count == 0) return;

			// جلب الأسعار دفعةً واحدة
			var sb = new System.Text.StringBuilder();
			foreach (var it in _items) sb.Append(it.ProductID + ",");
			string ids = sb.ToString().TrimEnd(',');
			if (string.IsNullOrEmpty(ids)) return;

			var dtPrices = DbHelper.Query(
				$"SELECT ProductID, SalePrice, SemiWholesalePrice, WholesalePrice FROM Products WHERE ProductID IN ({ids})");

			var priceMap = new System.Collections.Generic.Dictionary<int, decimal>();
			foreach (DataRow r in dtPrices.Rows)
			{
				int pid = Convert.ToInt32(r["ProductID"]);
				decimal price = newTier == "جملة"
					? (r["WholesalePrice"] != DBNull.Value ? Convert.ToDecimal(r["WholesalePrice"]) : Convert.ToDecimal(r["SalePrice"]))
					: newTier == "نصف جملة"
						? (r["SemiWholesalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SemiWholesalePrice"]) : Convert.ToDecimal(r["SalePrice"]))
						: Convert.ToDecimal(r["SalePrice"]);
				priceMap[pid] = price;
			}

			if (MessageBox.Show(
				$"هل تريد تحديث أسعار جميع الأصناف وفق فئة \"{newTier}\"؟",
				"تغيير فئة السعر", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				foreach (var item in _items)
				{
					if (priceMap.TryGetValue(item.ProductID, out decimal np) && np > 0)
					{
						item.UnitPrice = np;
						item.PriceTier = newTier;
						decimal gross = item.Quantity * item.UnitPrice;
						item.DiscountAmt = Math.Round(gross * item.DiscountPct / 100m, 2);
					}
				}
				RefreshGrid();
			}
		}

		/// <summary>
		/// يُحدِّث مظهر أزرار فئة السعر ليُبرز المحدود منها.
		/// </summary>
		private void SetTierButtons(string tier)
		{
			_selectedTier = tier;
			if (btnTierRetail == null) return;

			Color clrRetailOn    = Color.FromArgb(30,  100, 200);
			Color clrSemiOn      = Color.FromArgb(130,  50, 180);
			Color clrWholesaleOn = Color.FromArgb(200,  90,   0);
			Color clrOff         = Theme.BgInput;

			btnTierRetail.BackColor    = tier == "قطاعي"    ? clrRetailOn    : clrOff;
			btnTierRetail.ForeColor    = tier == "قطاعي"    ? Color.White    : Theme.TextMain;
			btnTierSemi.BackColor      = tier == "نصف جملة" ? clrSemiOn      : clrOff;
			btnTierSemi.ForeColor      = tier == "نصف جملة" ? Color.White    : Theme.TextMain;
			btnTierWholesale.BackColor = tier == "جملة"     ? clrWholesaleOn : clrOff;
			btnTierWholesale.ForeColor = tier == "جملة"     ? Color.White    : Theme.TextMain;
		}

		private void SetInvoiceType(string type)
		{
			_invoiceType = type;
			btnTypeCredit.BackColor = ((_invoiceType == "Credit") ? Theme.Accent : Theme.BgInput);
			btnTypeCredit.ForeColor = ((_invoiceType == "Credit") ? Color.White : Theme.TextMain);
			btnTypeCash.BackColor = ((_invoiceType == "Cash") ? Theme.Accent : Theme.BgInput);
			btnTypeCash.ForeColor = ((_invoiceType == "Cash") ? Color.White : Theme.TextMain);
			btnTypeDriverLoad.BackColor = ((_invoiceType == "DriverLoad") ? Theme.Accent : Theme.BgInput);
			btnTypeDriverLoad.ForeColor = ((_invoiceType == "DriverLoad") ? Color.White : Theme.TextMain);
			ToggleType();
		}

		private void ToggleType()
		{
			bool flag = _invoiceType == "Credit";
			bool flag2 = _invoiceType == "DriverLoad";
			bool flag3 = _invoiceType == "Cash";
			cboClient.Enabled = flag || flag3;
			cboDriver.Enabled = true;

            if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                EvaluateClientFinancials(ci.ID);
            }
            else
            {
                this.BackColor = Theme.BgMain;
                pnlItems.Enabled = true;
                btnSave.Enabled = true;
            }
		}

        private void EvaluateClientFinancials(int clientID)
        {
            var status = ClientDAL.GetFinancialStatus(clientID);
            
            bool limitExceeded = status.MaxCreditLimit > 0 && status.Balance >= status.MaxCreditLimit;
            bool oldDebtExists = status.OldDebt30 > 0;

            if ((limitExceeded || oldDebtExists) && _invoiceType == "Credit")
            {
                this.BackColor = Color.FromArgb(255, 200, 200); // Light Red
                pnlItems.Enabled = false; 
                btnSave.Enabled = false;
                
                string msg = "⚠️ تحذير مالي ⚠️\n\n";
                if (limitExceeded) msg += $"- تجاوز العميل الحد الائتماني ({status.MaxCreditLimit:N2} ج). رصيده: {status.Balance:N2} ج.\n";
                if (oldDebtExists) msg += $"- ديون متأخرة (تجاوزت 30 يوم) بقيمة {status.OldDebt30:N2} ج لم تسدد.\n";
                
                MessageBox.Show(msg + "\nالبيع الآجل موقوف لهذا العميل حتى يتم السداد.", "إيقاف البيع", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else
            {
                this.BackColor = Theme.BgMain;
                pnlItems.Enabled = true;
                btnSave.Enabled = true;
            }
        }

		private void BtnSearchProduct_Click(object sender, EventArgs e)
		{
			using FrmProductSearch frmProductSearch = new FrmProductSearch();
			if (frmProductSearch.ShowDialog() == DialogResult.OK)
			{
				SelectProductByID(frmProductSearch.SelectedProductID);
			}
		}

		private void SelectProductByID(int prodID)
		{
			int matchedIndex = -1;
			for (int i = 0; i < cboProduct.Items.Count; i++)
			{
				if (cboProduct.Items[i] is ComboItem comboItem && comboItem.ID == prodID)
				{
					if (comboItem.PriceTier == _selectedTier)
					{
						matchedIndex = i;
						break;
					}
					if (matchedIndex == -1 || comboItem.PriceTier == "قطاعي")
					{
						matchedIndex = i;
					}
				}
			}
			if (matchedIndex != -1)
			{
				cboProduct.SelectedIndex = matchedIndex;
			}
		}

		private void SelectDriverByID(int driverID)
		{
			for (int i = 0; i < cboDriver.Items.Count; i++)
			{
				if (cboDriver.Items[i] is ComboItem comboItem && comboItem.ID == driverID)
				{
					cboDriver.SelectedIndex = i;
					break;
				}
			}
		}

		private void BtnAddItem_Click(object sender, EventArgs e)
		{
			if (!(cboProduct.SelectedItem is ComboItem comboItem) || comboItem.ID == 0)
			{
				MessageBox.Show("اختر الصنف أولا\u064b");
				return;
			}
			if (!decimal.TryParse(txtPrice.Text, out var result) || result <= 0m)
			{
				MessageBox.Show("أدخل سعرا\u064b صحيحا\u064b");
				return;
			}
			decimal value = nudQty.Value;
			// FIX: استخدام cache بدلاً من رحلة DB
			decimal productStock = _stockCache.TryGetValue(comboItem.ID, out var cached2) ? cached2 : 0m;
			if (value > productStock)
			{
				MessageBox.Show($"❌ خطأ: الكمية المطلوبة ({value:N2}) أكبر من الكمية المتاحة في المخزن حاليا\u064b ({productStock:N2})!", "تنبيه - رصيد غير كاف\u064d", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			foreach (SaleItemDTO item in _items)
			{
				if (item.ProductID == comboItem.ID && item.PriceTier == comboItem.PriceTier)
				{
					MessageBox.Show("الصنف بنفس فئة السعر موجود مسبقاً بالفاتورة");
					return;
				}
			}
			_items.Add(new SaleItemDTO
			{
				ProductID = comboItem.ID,
				ProductName = comboItem.Name,
				Quantity = value,
				UnitPrice = result,
				StockQty = productStock,
				MinStockLimit = comboItem.MinStockLimit,
				PurchasePrice = comboItem.PurchasePrice,
				PartNumber = comboItem.PartNumber,
				CarModel = comboItem.CarModel,
				Brand = comboItem.Brand,
				ShelfLocation = comboItem.ShelfLocation,
				PriceTier = comboItem.PriceTier
			});
			RefreshGrid();
		}

		private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "Delete")
			{
				_items.RemoveAt(e.RowIndex);
				RefreshGrid();
			}
		}

		private void DgItems_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.RowIndex >= _items.Count)
			{
				return;
			}
			DataGridViewRow dataGridViewRow = dgItems.Rows[e.RowIndex];
			SaleItemDTO saleItemDTO = _items[e.RowIndex];
			if (dgItems.Columns[e.ColumnIndex].Name == "Quantity")
			{
				if (decimal.TryParse(dataGridViewRow.Cells["Quantity"].Value?.ToString(), out var result) && result > 0m)
				{
					// استخدام cache — يتم تحديثه عند فتح الشاشة
					decimal productStock = _stockCache.TryGetValue(saleItemDTO.ProductID, out var cached3) ? cached3 : 0m;
					if (result > productStock)
					{
						MessageBox.Show($"❌ خطأ: الكمية المطلوبة ({result:N2}) أكبر من الكمية المتاحة في المخزن حالياً ({productStock:N2})!", "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dataGridViewRow.Cells["Quantity"].Value = saleItemDTO.Quantity.ToString("F2");
						return;
					}
					saleItemDTO.Quantity = result;
					// Recalculate discount amount based on percentage
					decimal gross = saleItemDTO.Quantity * saleItemDTO.UnitPrice;
					saleItemDTO.DiscountAmt = Math.Round(gross * saleItemDTO.DiscountPct / 100m, 2);
				}
				else
				{
					MessageBox.Show("من فضلك أدخل كمية صحيحة أكبر من الصفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					dataGridViewRow.Cells["Quantity"].Value = saleItemDTO.Quantity.ToString("F2");
				}
			}
			else if (dgItems.Columns[e.ColumnIndex].Name == "UnitPrice")
			{
				// FIX: تغيير >= 0 إلى > 0 لمنع حفظ الفاتورة بسعر صفر
				if (decimal.TryParse(dataGridViewRow.Cells["UnitPrice"].Value?.ToString(), out var result2) && result2 > 0m)
				{
					saleItemDTO.UnitPrice = result2;
					// Recalculate discount amount based on percentage
					decimal gross = saleItemDTO.Quantity * saleItemDTO.UnitPrice;
					saleItemDTO.DiscountAmt = Math.Round(gross * saleItemDTO.DiscountPct / 100m, 2);
				}
				else
				{
					MessageBox.Show("من فضلك أدخل سعر صحيح أكبر من الصفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					dataGridViewRow.Cells["UnitPrice"].Value = saleItemDTO.UnitPrice.ToString("F2");
				}
			}
			else if (dgItems.Columns[e.ColumnIndex].Name == "DiscountPct")
			{
				if (decimal.TryParse(dataGridViewRow.Cells["DiscountPct"].Value?.ToString(), out var resultPct) && resultPct >= 0m && resultPct <= 100m)
				{
					saleItemDTO.DiscountPct = resultPct;
					decimal gross = saleItemDTO.Quantity * saleItemDTO.UnitPrice;
					saleItemDTO.DiscountAmt = Math.Round(gross * resultPct / 100m, 2);
				}
				else
				{
					MessageBox.Show("من فضلك أدخل نسبة خصم صحيحة بين 0 و 100.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					dataGridViewRow.Cells["DiscountPct"].Value = saleItemDTO.DiscountPct.ToString("F2");
				}
			}
			else if (dgItems.Columns[e.ColumnIndex].Name == "DiscountAmt")
			{
				if (decimal.TryParse(dataGridViewRow.Cells["DiscountAmt"].Value?.ToString(), out var resultAmt) && resultAmt >= 0m)
				{
					decimal gross = saleItemDTO.Quantity * saleItemDTO.UnitPrice;
					if (resultAmt > gross)
					{
						MessageBox.Show("قيمة الخصم لا يمكن أن تكون أكبر من إجمالي سعر الصنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dataGridViewRow.Cells["DiscountAmt"].Value = saleItemDTO.DiscountAmt.ToString("F2");
						return;
					}
					saleItemDTO.DiscountAmt = resultAmt;
					if (gross > 0m)
					{
						saleItemDTO.DiscountPct = Math.Round((resultAmt / gross) * 100m, 2);
					}
					else
					{
						saleItemDTO.DiscountPct = 0m;
					}
				}
				else
				{
					MessageBox.Show("من فضلك أدخل قيمة خصم صحيحة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					dataGridViewRow.Cells["DiscountAmt"].Value = saleItemDTO.DiscountAmt.ToString("F2");
				}
			}

			dataGridViewRow.Cells["DiscountPct"].Value = saleItemDTO.DiscountPct.ToString("F2");
			dataGridViewRow.Cells["DiscountAmt"].Value = saleItemDTO.DiscountAmt.ToString("F2");
			dataGridViewRow.Cells["TotalPrice"].Value = saleItemDTO.TotalPrice.ToString("F2");
			CalculateNet();
		}

		private void RefreshGrid()
		{
			dgItems.Rows.Clear();
			foreach (SaleItemDTO item in _items)
			{
				decimal costTotal = item.PurchasePrice * item.Quantity;
				int rIndex = dgItems.Rows.Add(
					item.ProductName,
					item.PriceTier,
					item.PartNumber,
					item.CarModel,
					item.Brand,
					item.ShelfLocation,
					item.StockQty.ToString("F2"),
					item.Quantity.ToString("F2"),
					item.UnitPrice.ToString("F2"),
					item.DiscountPct.ToString("F2"),
					item.DiscountAmt.ToString("F2"),
					item.TotalPrice.ToString("F2"),
					item.PurchasePrice.ToString("F2"),
					costTotal.ToString("F2")
				);
                
                var cell = dgItems.Rows[rIndex].Cells["StockQty"];
                if (item.MinStockLimit > 0)
                {
                    if (item.StockQty <= item.MinStockLimit / 2m)
                    {
                        cell.Style.BackColor = Color.FromArgb(255, 100, 100); // Red
                        cell.Style.ForeColor = Color.White;
                    }
                    else if (item.StockQty <= item.MinStockLimit)
                    {
                        cell.Style.BackColor = Color.FromArgb(255, 165, 0); // Orange
                        cell.Style.ForeColor = Color.White;
                    }
                }
			}
			CalculateNet();
		}

		private void CalculateNet()
		{
			decimal gross = 0m;
			decimal totalCost = 0m;
			foreach (SaleItemDTO item in _items)
			{
				gross += item.TotalPrice;
				totalCost += item.PurchasePrice * item.Quantity;
			}
			lblTotalVal.Text = gross.ToString("N2") + " ج";

			decimal discount = 0m;
			decimal discountPct = 0m;
			decimal discountAmt = 0m;
			if (txtInvoiceDiscount != null && decimal.TryParse(txtInvoiceDiscount.Text, out discount) && discount > 0)
			{
				if (cboInvoiceDiscountType.SelectedIndex == 1) // نسبة %
				{
					discountPct = discount;
					discountAmt = Math.Round(gross * discountPct / 100m, 2);
				}
				else // قيمة
				{
					discountAmt = discount;
					if (gross > 0)
					{
						discountPct = Math.Round((discountAmt / gross) * 100m, 2);
					}
				}
			}

			decimal net = Math.Max(0m, gross - discountAmt);
			if (lblNetVal != null)
			{
				lblNetVal.Text = net.ToString("N2") + " ج";
			}

			// Cost & Profit (only if user has CanViewCost permission)
			if (lblCostVal != null && Session.CanViewCost("Sales"))
			{
				decimal profit = net - totalCost;
				lblCostVal.Text = totalCost.ToString("N2") + " ج";
				lblProfitVal.Text = profit.ToString("N2") + " ج";
				lblProfitVal.ForeColor = profit >= 0 ? Theme.Success : Color.FromArgb(220, 60, 60);
			}
            _isDirty = true;
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			SaveInvoiceLogic(isDraft: false);
		}

		private void BtnHold_Click(object sender, EventArgs e)
		{
			SaveInvoiceLogic(isDraft: true);
		}

		/// <summary>
		/// يحمّل فاتورة موجودة لغرض التعديل أو النسخ.
		/// </summary>
		private void LoadInvoiceForEdit(int saleID)
		{
			var dtSale = DbHelper.Query(
				@"SELECT s.SaleType, s.SaleDate, s.ClientID, s.DriverID, s.Notes,
				         COALESCE(s.DiscountAmount,0) AS DiscountAmount,
				         COALESCE(s.DiscountPct,0)    AS DiscountPct,
				         COALESCE(s.PriceTier,'قطاعي') AS PriceTier,
				         s.LastModifiedDate
				  FROM Sales s WHERE s.SaleID=@id",
				DbHelper.P("@id", saleID));

			if (dtSale.Rows.Count == 0)
			{
				MessageBox.Show("لم يتم العثور على الفاتورة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var row = dtSale.Rows[0];

			// Concurrency Token
			_loadedLastModified = row["LastModifiedDate"] != DBNull.Value ? Convert.ToDateTime(row["LastModifiedDate"]) : Convert.ToDateTime(row["SaleDate"]);

			// نوع الفاتورة
			string typeStr = row["SaleType"].ToString();
			SetInvoiceType(typeStr);

			// التاريخ
			dtpDate.Value = _isCopyMode ? DateTime.Today : Convert.ToDateTime(row["SaleDate"]);

			// العميل
			if (row["ClientID"] != DBNull.Value)
			{
				int cid = Convert.ToInt32(row["ClientID"]);
				for (int i = 0; i < cboClient.Items.Count; i++)
					if (cboClient.Items[i] is ComboItem ci && ci.ID == cid)
						{ cboClient.SelectedIndex = i; break; }
			}

			// المندوب
			if (row["DriverID"] != DBNull.Value)
			{
				int did = Convert.ToInt32(row["DriverID"]);
				for (int i = 0; i < cboDriver.Items.Count; i++)
					if (cboDriver.Items[i] is ComboItem ci2 && ci2.ID == did)
						{ cboDriver.SelectedIndex = i; break; }
			}

			// ملاحظات
			txtNotes.Text = row["Notes"].ToString();

			// الخصم
			decimal discPct = Convert.ToDecimal(row["DiscountPct"]);
			decimal discAmt = Convert.ToDecimal(row["DiscountAmount"]);
			if (discPct > 0)
			{
				cboInvoiceDiscountType.SelectedIndex = 1;
				txtInvoiceDiscount.Text = discPct.ToString("G29");
			}
			else
			{
				cboInvoiceDiscountType.SelectedIndex = 0;
				txtInvoiceDiscount.Text = discAmt.ToString("G29");
			}

			// فئة السعر
			string tier = row["PriceTier"].ToString();
			// تعيين فئة السعر أثناء تحميل الفاتورة (بدون سؤال)
			SetTierButtons(!string.IsNullOrEmpty(tier) ? tier : "قطاعي");

			// البنود
			var dtItems = SaleDAL.GetItems(saleID);
			_items.Clear();
			foreach (DataRow iRow in dtItems.Rows)
			{
				int pid = Convert.ToInt32(iRow["ProductID"]);
				decimal qty = Convert.ToDecimal(iRow["Quantity"]);
				// نضيف الكمية للـ cache في وضع التعديل (وليس النسخ) لكي يعتبرها رصيداً متاحاً في الجريد أثناء التعديل
				if (!_isCopyMode)
				{
					if (_stockCache.ContainsKey(pid))
						_stockCache[pid] += qty;
					else
						_stockCache[pid] = qty;
				}

				decimal stock = _stockCache.TryGetValue(pid, out var st) ? st : 0m;
				_items.Add(new SaleItemDTO
				{
					ProductID   = pid,
					ProductName = iRow["ProductName"].ToString(),
					Quantity    = qty,
					UnitPrice   = Convert.ToDecimal(iRow["UnitPrice"]),
					DiscountPct = Convert.ToDecimal(iRow["DiscountPct"]),
					DiscountAmt = Convert.ToDecimal(iRow["DiscountAmt"]),
					PriceTier   = iRow["PriceTier"].ToString(),
					StockQty    = stock,
					PurchasePrice = iRow["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(iRow["PurchasePrice"]) : 0m,
					PartNumber  = iRow["PartNumber"]?.ToString() ?? "",
					CarModel    = iRow["CarModel"]?.ToString() ?? "",
					Brand       = iRow["Brand"]?.ToString() ?? "",
					ShelfLocation = iRow["ShelfLocation"]?.ToString() ?? ""
				});
			}
			RefreshGrid();

			// عنوان النافذة
			if (_isCopyMode)
				Text = "نسخة من الفاتورة";
			else
				Text = $"تعديل الفاتورة رقم {saleID}";

			_isDirty = false;
		}


		private void SaveInvoiceLogic(bool isDraft)
		{
			if (_items.Count == 0)
			{
				MessageBox.Show("أضف أصناف أولاً");
				return;
			}

			// ─── التحقق من صلاحية تعديل الفاتورة ───
			if (_editSaleID > 0)
			{
				if (!Session.CanEditSalesInvoice())
				{
					MessageBox.Show("❌ ليس لديك صلاحية تعديل الفواتير.\nراجع مسؤول النظام.",
						"صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					return;
				}
				if (!SaleDAL.CanEditSale(_editSaleID, out string editReason))
				{
					MessageBox.Show($"❌ لا يمكن تعديل الفاتورة:\n{editReason}",
						"تعديل مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					return;
				}
			}

			// ─── التحقق من المخزون ───
			foreach (SaleItemDTO item in _items)
			{
				decimal productStock = InventoryDAL.GetProductStock(item.ProductID, GetSelectedWarehouseID());
				decimal quantityToCheck = item.Quantity;

				if (_editSaleID > 0)
				{
					// في حال التعديل، نقوم بالتحقق من الفارق فقط
					var oldQtyObj = DbHelper.Scalar("SELECT Quantity FROM SaleItems WHERE SaleID=@sid AND ProductID=@pid",
						DbHelper.P("@sid", _editSaleID), DbHelper.P("@pid", item.ProductID));
					decimal oldQty = oldQtyObj != null ? Convert.ToDecimal(oldQtyObj) : 0m;
					
					quantityToCheck = item.Quantity - oldQty;
				}

				if (quantityToCheck > 0 && quantityToCheck > productStock)
				{
					MessageBox.Show($"❌ خطأ: الصنف '{item.ProductName}' لا يوجد منه رصيد كافٍ في المخزن حالياً لتغطية الزيادة المطلوبة.\nالزيادة المطلوبة: {quantityToCheck:N2}\nالكمية المتاحة بالمخزن: {productStock:N2}",
						"عجز في الرصيد", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}

			int saleType = _invoiceType == "Credit" ? 0 : _invoiceType == "DriverLoad" ? 1 : 2;
			int? clientID = null;
			int? driverID = null;
			if (_invoiceType == "Credit" || _invoiceType == "Cash")
			{
				if (!(cboClient.SelectedItem is ComboItem comboItem) || comboItem.ID == 0)
				{
					MessageBox.Show("اختر العميل");
					return;
				}
				clientID = comboItem.ID;
				if (cboDriver.SelectedItem is ComboItem comboItem2 && comboItem2.ID > 0)
					driverID = comboItem2.ID;
			}
			else if (_invoiceType == "DriverLoad")
			{
				if (!(cboDriver.SelectedItem is ComboItem comboItem3) || comboItem3.ID == 0)
				{
					MessageBox.Show("اختر المندوب");
					return;
				}
				driverID = comboItem3.ID;
			}

			decimal gross = 0m;
			foreach (SaleItemDTO item2 in _items) gross += item2.TotalPrice;

			decimal discountAmount = 0m;
			decimal discountPct = 0m;
			if (txtInvoiceDiscount != null && decimal.TryParse(txtInvoiceDiscount.Text, out decimal discount) && discount > 0)
			{
				if (cboInvoiceDiscountType.SelectedIndex == 1) // نسبة %
				{
					discountPct = discount;
					discountAmount = Math.Round(gross * discountPct / 100m, 2);
				}
				else // قيمة
				{
					discountAmount = discount;
					if (gross > 0) discountPct = Math.Round((discountAmount / gross) * 100m, 2);
				}
			}
			decimal net = Math.Max(0m, gross - discountAmount);
			string priceTier = _selectedTier ?? "قطاعي";

			// ─── إشعار الدفع النقدي ───
			if (!isDraft && _invoiceType == "Cash" && _editSaleID == 0)
			{
				using (var frm = new FrmQuickPayment(net))
					if (frm.ShowDialog() != DialogResult.OK) return;
			}

			// ─── التحقق من حد الائتمان ───
			if (!isDraft && _invoiceType == "Credit" && clientID.HasValue)
			{
				DataRow byID = ClientDAL.GetByID(clientID.Value);
				if (byID != null)
				{
					decimal maxCredit = Convert.ToDecimal(byID["MaxCreditLimit"] == DBNull.Value ? 0 : byID["MaxCreditLimit"]);
					if (maxCredit > 0m)
					{
						decimal clientBalance = ClientDAL.GetClientBalance(clientID.Value);
						decimal valueToCompare = clientBalance + net;

						if (_editSaleID > 0)
						{
							// في وضع التعديل، نطرح قيمة الفاتورة القديمة أولاً
							var oldTotalObj = DbHelper.Scalar("SELECT TotalAmount FROM Sales WHERE SaleID=@id", DbHelper.P("@id", _editSaleID));
							decimal oldTotal = oldTotalObj != null ? Convert.ToDecimal(oldTotalObj) : 0m;
							valueToCompare = clientBalance - oldTotal + net;
						}

						if (valueToCompare > maxCredit)
						{
							MessageBox.Show($"❌ الرصيد المتوقع بعد الحفظ ({valueToCompare:N2} ج) يتجاوز الحد الأقصى للائتمان المسموح به لهذا العميل ({maxCredit:N2} ج)!\n\nيرجى تحصيل دفعة نقدية أولاً.",
								"تجاوز حد المديونية", MessageBoxButtons.OK, MessageBoxIcon.Hand);
							return;
						}
					}
				}
			}

			// ─── الحفظ أو التعديل ───
			if (_editSaleID > 0)
			{
				// وضع التعديل
				try
				{
					bool updated = SaleDAL.UpdateSale(_editSaleID, saleType, clientID, driverID,
						net, txtNotes.Text, _items, discountAmount, discountPct,
						isDraft: false, warehouseID: GetSelectedWarehouseID(), priceTier: priceTier,
						loadedLastModified: _loadedLastModified);
					if (updated)
					{
						_isDirty = false;
						DialogResult pr = MessageBox.Show(
							$"✅ تم تعديل الفاتورة رقم [{_editSaleID}] بنجاح!\n\nهل تريد طباعة الفاتورة المعدّلة؟",
							"تعديل ناجح", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (pr == DialogResult.Yes) new FrmPrintSale(_editSaleID);
						this.Close();
					}
					else
					{
						MessageBox.Show("❌ فشل التعديل، راجع الاتصال بقاعدة البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("CONCURRENCY_ERROR"))
					{
						MessageBox.Show(ex.Message.Replace("CONCURRENCY_ERROR: ", ""), "خطأ تعديل متزامن", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					else
					{
						MessageBox.Show("❌ حدث خطأ أثناء التعديل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
			else
			{
				// وضع الإنشاء الجديد (أو نسخ)
				int num3 = SaleDAL.SaveSale(saleType, clientID, driverID, net,
					txtNotes.Text, _items, discountAmount, discountPct, isDraft,
					warehouseID: GetSelectedWarehouseID(), priceTier: priceTier);
				if (num3 > 0)
				{
					_lastSaleID = num3;
					_isDirty = false;
					if (isDraft)
					{
						MessageBox.Show($"✅ تم تعليق الفاتورة بنجاح.\nيمكنك استدعاؤها لاحقاً من زر 📂 معلقات.",
							"تعليق", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						DialogResult printResult = MessageBox.Show(
							$"✅ تم حفظ الفاتورة بنجاح رقم [{num3}]!\n\nهل تريد طباعة الفاتورة الآن؟",
							"نجاح الحفظ والطباعة", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (printResult == DialogResult.Yes) new FrmPrintSale(num3);
					}
					if (!_isCopyMode) ResetForm();
					else this.Close();
				}
				else
				{
					MessageBox.Show("❌ فشل الحفظ، راجع الاتصال بقاعدة البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		private void BtnLoadHold_Click(object sender, EventArgs e)
		{
			DataTable dt = SaleDAL.GetDraftSales();
			if (dt.Rows.Count == 0)
			{
				MessageBox.Show("لا توجد فواتير معلقة حالياً.", "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			var dlg = new Form
			{
				Width = 800, Height = 450,
				Text = "📂 الفواتير المعلقة",
				StartPosition = FormStartPosition.CenterParent,
				RightToLeft = RightToLeft.Yes,
				RightToLeftLayout = true,
				BackColor = Theme.BgCard,
				Font = Theme.FontMain
			};

			var dgDrafts = new DataGridView
			{
				Dock = DockStyle.Fill,
				DataSource = dt,
				BackgroundColor = Theme.BgCard,
				RowHeadersVisible = false,
				ReadOnly = true,
				AllowUserToAddRows = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				DefaultCellStyle = new DataGridViewCellStyle { Font = Theme.FontMain, BackColor = Theme.BgCard, ForeColor = Theme.TextMain },
				ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { Font = Theme.FontBold, BackColor = Theme.Primary, ForeColor = Color.White },
				EnableHeadersVisualStyles = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
			};

			dlg.Load += (sl, el) =>
			{
				if (dgDrafts.Columns.Contains("SaleID")) dgDrafts.Columns["SaleID"].Visible = false;
				if (dgDrafts.Columns.Contains("ClientID")) dgDrafts.Columns["ClientID"].Visible = false;
				if (dgDrafts.Columns.Contains("DriverID")) dgDrafts.Columns["DriverID"].Visible = false;
				if (dgDrafts.Columns.Contains("SaleType")) dgDrafts.Columns["SaleType"].Visible = false;
				if (dgDrafts.Columns.Contains("DiscountAmount")) dgDrafts.Columns["DiscountAmount"].Visible = false;
				if (dgDrafts.Columns.Contains("DiscountPct")) dgDrafts.Columns["DiscountPct"].Visible = false;
				if (dgDrafts.Columns.Contains("SaleCode")) dgDrafts.Columns["SaleCode"].HeaderText = "كود الفاتورة";
				if (dgDrafts.Columns.Contains("SaleDate")) dgDrafts.Columns["SaleDate"].HeaderText = "التاريخ";
				if (dgDrafts.Columns.Contains("ClientName")) dgDrafts.Columns["ClientName"].HeaderText = "العميل";
				if (dgDrafts.Columns.Contains("DriverName")) dgDrafts.Columns["DriverName"].HeaderText = "المندوب";
				if (dgDrafts.Columns.Contains("TotalAmount")) dgDrafts.Columns["TotalAmount"].HeaderText = "الإجمالي";
				if (dgDrafts.Columns.Contains("Notes")) dgDrafts.Columns["Notes"].HeaderText = "ملاحظات";
			};

			var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 45, Width = 800, BackColor = Theme.BgCard, Padding = new Padding(5) };

			var btnLoad = Theme.MakeButton("✅ استدعاء الفاتورة", 0, 5, 180, 35, Theme.Success);
			btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnLoad.Click += (s2, e2) =>
			{
				if (dgDrafts.SelectedRows.Count == 0) return;
				var row = (DataRowView)dgDrafts.SelectedRows[0].DataBoundItem;

				if (_isDirty && _items.Count > 0)
				{
					if (MessageBox.Show("توجد فاتورة حالية قيد التسجيل، سيتم مسحها لتحميل الفاتورة المعلقة.\nهل أنت متأكد؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
						return;
				}

				int saleID = Convert.ToInt32(row["SaleID"]);
				ResetForm();

				string typeStr = row["SaleType"].ToString();
				SetInvoiceType(typeStr);

				if (row["ClientID"] != DBNull.Value)
				{
					int cid = Convert.ToInt32(row["ClientID"]);
					for (int i = 0; i < cboClient.Items.Count; i++)
					{
						if (cboClient.Items[i] is ComboItem ci && ci.ID == cid)
						{
							cboClient.SelectedIndex = i;
							break;
						}
					}
				}
				if (row["DriverID"] != DBNull.Value)
				{
					int did = Convert.ToInt32(row["DriverID"]);
					for (int i = 0; i < cboDriver.Items.Count; i++)
					{
						if (cboDriver.Items[i] is ComboItem ci && ci.ID == did)
						{
							cboDriver.SelectedIndex = i;
							break;
						}
					}
				}

				dtpDate.Value = Convert.ToDateTime(row["SaleDate"]);
				txtNotes.Text = row["Notes"].ToString();

				decimal discAmt = Convert.ToDecimal(row["DiscountAmount"]);
				decimal discPctVal = Convert.ToDecimal(row["DiscountPct"]);
				if (discPctVal > 0)
				{
					cboInvoiceDiscountType.SelectedIndex = 1;
					txtInvoiceDiscount.Text = discPctVal.ToString("G29");
				}
				else
				{
					cboInvoiceDiscountType.SelectedIndex = 0;
					txtInvoiceDiscount.Text = discAmt.ToString("G29");
				}

				var itemsDt = SaleDAL.GetItems(saleID);
				_items.Clear();
				foreach (DataRow iRow in itemsDt.Rows)
				{
					_items.Add(new SaleItemDTO
					{
						ProductID = Convert.ToInt32(iRow["ProductID"]),
						ProductName = iRow["ProductName"].ToString(),
						Quantity = Convert.ToDecimal(iRow["Quantity"]),
						UnitPrice = Convert.ToDecimal(iRow["UnitPrice"]),
						DiscountPct = Convert.ToDecimal(iRow["DiscountPct"]),
						DiscountAmt = Convert.ToDecimal(iRow["DiscountAmt"]),
						PartNumber = iRow["PartNumber"]?.ToString() ?? "",
						CarModel = iRow["CarModel"]?.ToString() ?? "",
						Brand = iRow["Brand"]?.ToString() ?? "",
						ShelfLocation = iRow["ShelfLocation"]?.ToString() ?? "",
						PurchasePrice = iRow["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(iRow["PurchasePrice"]) : 0m
					});
				}
				RefreshGrid();

				// Delete draft from DB
				SaleDAL.DeleteDraftSale(saleID);
				_isDirty = true;

				dlg.DialogResult = DialogResult.OK;
				dlg.Close();
			};

			var btnDeleteDraft = Theme.MakeButton("❌ حذف المسودة", 190, 5, 150, 35, Color.FromArgb(180, 60, 60));
			btnDeleteDraft.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnDeleteDraft.Click += (s2, e2) =>
			{
				if (dgDrafts.SelectedRows.Count == 0) return;
				var row = (DataRowView)dgDrafts.SelectedRows[0].DataBoundItem;
				if (MessageBox.Show("هل أنت متأكد من حذف هذه الفاتورة المعلقة نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
				{
					int saleID = Convert.ToInt32(row["SaleID"]);
					SaleDAL.DeleteDraftSale(saleID);
					dgDrafts.DataSource = SaleDAL.GetDraftSales();
					if (((DataTable)dgDrafts.DataSource).Rows.Count == 0)
					{
						dlg.Close();
					}
				}
			};

			pnlBottom.Controls.Add(btnLoad);
			pnlBottom.Controls.Add(btnDeleteDraft);
			dlg.Controls.Add(dgDrafts);
			dlg.Controls.Add(pnlBottom);
			dlg.ShowDialog();
		}

		private void FrmSale_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (AppConfig.ScaleEnabled)
			{
				ScaleService.Instance.WeightChanged -= Scale_WeightChanged;
				ScaleService.Instance.ErrorOccurred -= Scale_ErrorOccurred;
				ScaleService.Instance.Disconnect();
			}

			if (_isDirty && _items.Count > 0)
			{
				var res = MessageBox.Show("هناك تغييرات لم يتم حفظها في الفاتورة الحالية.\nهل تريد الخروج بدون حفظ؟", "تنبيه", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (res == DialogResult.No)
				{
					e.Cancel = true;
				}
			}
		}

		private void BtnPrint_Click(object sender, EventArgs e)
		{
			int printID = _lastSaleID;
			if (printID == 0)
			{
				var lastObj = DbHelper.Scalar("SELECT COALESCE(MAX(SaleID), 0) FROM Sales");
				if (lastObj != null)
				{
					printID = Convert.ToInt32(lastObj);
				}
			}

			if (printID == 0)
			{
				MessageBox.Show("لا توجد فواتير مسجلة لطباعتها!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var menu = new ContextMenuStrip();
			var itemReceipt = new ToolStripMenuItem("🧾 طباعة ريسيت حراري (Receipt)");
			itemReceipt.Click += (s2, e2) => new FrmPrintSale(printID, "Receipt");
            
			var itemA4 = new ToolStripMenuItem("📄 طباعة فاتورة ورق (A4/A5)");
			itemA4.Click += (s2, e2) => new FrmPrintSale(printID, "A4");

			menu.Items.Add(itemReceipt);
			menu.Items.Add(itemA4);

			if (sender is Control ctrl)
			{
				menu.Show(ctrl, new Point(0, ctrl.Height));
			}
			else
			{
				menu.Show(Cursor.Position);
			}
		}

		private void BtnTawreed_Click(object sender, EventArgs e)
		{
			if (!(cboClient.SelectedItem is ComboItem comboItem) || comboItem.ID == 0)
			{
				MessageBox.Show("❌ خطأ: يجب اختيار عميل مسجل أولا\u064b لتسجيل عملية التوريد لحسابه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			Form frm = new Form
			{
				Width = 400,
				Height = 250,
				Text = "توريد نقدية",
				StartPosition = FormStartPosition.CenterParent,
				RightToLeft = RightToLeft.Yes,
				RightToLeftLayout = true,
				BackColor = Theme.BgCard,
				Font = Theme.FontMain
			};
			Label label = new Label
			{
				Left = 20,
				Top = 20,
				Text = "المبلغ المورد:",
				AutoSize = true,
				ForeColor = Theme.TextMain
			};
			TextBox textBox = new TextBox
			{
				Left = 20,
				Top = 45,
				Width = 340,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain
			};
			Label label2 = new Label
			{
				Left = 20,
				Top = 80,
				Text = "ملاحظات:",
				AutoSize = true,
				ForeColor = Theme.TextMain
			};
			TextBox textBox2 = new TextBox
			{
				Left = 20,
				Top = 105,
				Width = 340,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain
			};
			Button button = Theme.MakeButton("✅ حفظ", 120, 150, 100, 35, Theme.Accent);
			button.Click += delegate
			{
				frm.DialogResult = DialogResult.OK;
				frm.Close();
			};
			frm.Controls.AddRange(new Control[5] { label, textBox, label2, textBox2, button });
			if (frm.ShowDialog() == DialogResult.OK && decimal.TryParse(textBox.Text, out var result) && result > 0m)
			{
				AccountDAL.SaveCashReceipt(comboItem.ID, result, dtpDate.Value, textBox2.Text);
				MessageBox.Show("✅ تم تسجيل التوريد في الخزنة بنجاح!", "تم", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}

		private void BtnWhatsApp_Click(object sender, EventArgs e)
		{
			int saleID = _lastSaleID;
			if (saleID == 0)
			{
				var lastObj = DbHelper.Scalar("SELECT COALESCE(MAX(SaleID), 0) FROM Sales");
				if (lastObj != null) saleID = Convert.ToInt32(lastObj);
			}
			if (saleID == 0)
			{
				MessageBox.Show("لا توجد فاتورة محفوظة لإرسالها!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			// جلب بيانات الفاتورة
			var dt = DbHelper.Query(@"
				SELECT s.SaleCode, s.SaleDate, s.SaleType, s.TotalAmount,
				       COALESCE(s.DiscountAmount, 0) AS DiscountAmount,
				       s.ClientID,
				       COALESCE(c.ClientName, N'---') AS ClientName,
				       COALESCE(c.Phone, '') AS ClientPhone
				FROM Sales s
				LEFT JOIN Clients c ON s.ClientID = c.ClientID
				WHERE s.SaleID = @id", DbHelper.P("@id", saleID));

			if (dt.Rows.Count == 0) { MessageBox.Show("لم يتم العثور على الفاتورة!"); return; }
			var saleRow = dt.Rows[0];
			string phone = saleRow["ClientPhone"].ToString().Trim();

			if (string.IsNullOrWhiteSpace(phone))
			{
				MessageBox.Show("العميل ليس لديه رقم هاتف مسجل!\nيرجى إضافة رقم الهاتف من شاشة إدارة العملاء.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			// جلب أصناف الفاتورة
			var items = SaleDAL.GetItems(saleID);

			// جلب البيانات المالية للعميل
			decimal prevBalance = 0m;
			decimal currentBalance = 0m;
			decimal lastPaymentAmt = 0m;
			DateTime lastPaymentDate = DateTime.MinValue;

			if (saleRow["ClientID"] != DBNull.Value)
			{
				int clientID = Convert.ToInt32(saleRow["ClientID"]);
				DateTime saleDate = Convert.ToDateTime(saleRow["SaleDate"]);

				// الرصيد السابق قبل هذه الفاتورة
				prevBalance = ClientDAL.GetPreviousBalance(clientID, saleDate);

				// الرصيد الحالي بعد الفاتورة
				currentBalance = ClientDAL.GetClientBalance(clientID);

				// آخر توريد (دفعة)
				var lastPayDt = DbHelper.Query(@"
					SELECT TOP 1 Credit, TransDate 
					FROM ClientTransactions 
					WHERE ClientID=@id AND TransType='Payment' AND Credit > 0
					ORDER BY TransDate DESC, TransID DESC",
					DbHelper.P("@id", clientID));
				if (lastPayDt.Rows.Count > 0)
				{
					lastPaymentAmt  = Convert.ToDecimal(lastPayDt.Rows[0]["Credit"]);
					lastPaymentDate = Convert.ToDateTime(lastPayDt.Rows[0]["TransDate"]);
				}
			}

			// بناء نص الرسالة
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("🧾 *فاتورة مبيعات*");
			sb.AppendLine($"🏢 {AppConfig.CompanyName}");
			sb.AppendLine("──────────────────────");
			sb.AppendLine($"📌 رقم الفاتورة: {saleRow["SaleCode"]}");
			sb.AppendLine($"📅 التاريخ: {Convert.ToDateTime(saleRow["SaleDate"]):dd/MM/yyyy}");
			sb.AppendLine($"👤 العميل: {saleRow["ClientName"]}");
			string typeLabel = saleRow["SaleType"].ToString() == "Credit" ? "آجل" : saleRow["SaleType"].ToString() == "Cash" ? "نقدي" : "تحميل مندوب";
			sb.AppendLine($"🏷️ النوع: {typeLabel}");
			sb.AppendLine("──────────────────────");

			if (items != null)
			{
				foreach (DataRow r in items.Rows)
				{
					string name  = r["ProductName"].ToString();
					decimal qty   = Convert.ToDecimal(r["Quantity"]);
					decimal price = Convert.ToDecimal(r["UnitPrice"]);
					decimal tot   = Convert.ToDecimal(r["TotalPrice"]);
					sb.AppendLine($"• {name}: {qty:N0} × {price:N2} = {tot:N2} ج");
				}
			}

			sb.AppendLine("──────────────────────");
			decimal discAmt = Convert.ToDecimal(saleRow["DiscountAmount"]);
			if (discAmt > 0)
				sb.AppendLine($"💸 الخصم: {discAmt:N2} ج");
			sb.AppendLine($"💰 *صافي الفاتورة: {Convert.ToDecimal(saleRow["TotalAmount"]):N2} ج.م*");
			sb.AppendLine("──────────────────────");

			// المعلومات المالية
			if (saleRow["ClientID"] != DBNull.Value)
			{
				sb.AppendLine("📊 *الوضع المالي للحساب:*");
				sb.AppendLine($"📋 الرصيد السابق:    {prevBalance:N2} ج");
				sb.AppendLine($"🛒 الفاتورة الحالية:  {Convert.ToDecimal(saleRow["TotalAmount"]):N2} ج");
				sb.AppendLine($"📈 *إجمالي المديونية: {currentBalance:N2} ج*");
				if (lastPaymentAmt > 0)
					sb.AppendLine($"✅ آخر توريد: {lastPaymentAmt:N2} ج  ({lastPaymentDate:dd/MM/yyyy})");
				else
					sb.AppendLine("✅ آخر توريد: لا يوجد");
				sb.AppendLine("──────────────────────");
			}

			sb.AppendLine("شكراً لتعاملكم معنا 🙏");

			SendWhatsApp(phone, sb.ToString());
		}

		private static void SendWhatsApp(string phone, string message)
		{
			try
			{
				string clean = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
				if (clean.StartsWith("0")) clean = "20" + clean.Substring(1);
				string encoded = Uri.EscapeDataString(message);
				string url = $"https://wa.me/{clean}?text={encoded}";
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				MessageBox.Show("تعذر فتح واتساب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ResetForm()
		{
			_items.Clear();
			dgItems.Rows.Clear();
			lblTotalVal.Text = "0.00 ج";
			if (txtInvoiceDiscount != null) txtInvoiceDiscount.Text = "0";
			if (cboInvoiceDiscountType != null) cboInvoiceDiscountType.SelectedIndex = 0;
			if (lblNetVal != null) lblNetVal.Text = "0.00 ج";
			txtNotes.Clear();
			txtPrice.Clear();
			nudQty.Value = 1m;
			if (cboClient.Items.Count > 0) cboClient.SelectedIndex = 0;
			if (cboDriver.Items.Count > 0) cboDriver.SelectedIndex = 0;
			if (cboProduct.Items.Count > 0) cboProduct.SelectedIndex = 0;
			SetTierButtons("قطاعي");
			dtpDate.Value = DateTime.Today;
			SetInvoiceType("Credit");
			Text = "شاشة المبيعات";
			_editSaleID = 0;
			_isCopyMode = false;
			_isDirty = false;
		}

		private int? GetSelectedWarehouseID()
		{
			if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem wh && wh.ID > 0)
				return wh.ID;
			return null;
		}

        // ══════════════════════════════════════════════════════════════════════
        // ── تخصيص أعمدة الجدول ────────────────────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>يفتح نافذة تخصيص الأعمدة (إظهار/إخفاء + ترتيب)</summary>
        private void ShowColumnCustomizer()
        {
            var dlg = new Form
            {
                Text            = "⚙️ تخصيص أعمدة الفاتورة",
                Size            = new Size(360, 480),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                RightToLeft     = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor       = Color.FromArgb(30, 30, 45),
                Font            = new Font("Segoe UI", 10f)
            };

            var lblHint = new Label
            {
                Text      = "✅ تفعيل/إيقاف الأعمدة  |  ▲▼ لتغيير الترتيب",
                Dock      = DockStyle.Top,
                Height    = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(150, 200, 255),
                Font      = new Font("Segoe UI", 9f)
            };

            var clb = new CheckedListBox
            {
                Dock            = DockStyle.Fill,
                CheckOnClick    = true,
                BackColor       = Color.FromArgb(40, 42, 58),
                ForeColor       = Color.White,
                BorderStyle     = BorderStyle.None,
                Font            = new Font("Segoe UI", 10f),
                RightToLeft     = RightToLeft.Yes
            };

            // ملء القائمة بالأعمدة (ما عدا عمود الحذف)
            foreach (DataGridViewColumn col in dgItems.Columns)
            {
                if (col.Name == "Delete") continue;
                clb.Items.Add(new ColEntry(col.Name, col.HeaderText), col.Visible);
            }

            // أزرار ▲▼
            var btnUp   = new Button { Text = "▲ أعلى",   Width = 90, Height = 30, BackColor = Color.FromArgb(55,65,81), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnDown = new Button { Text = "▼ أسفل",   Width = 90, Height = 30, BackColor = Color.FromArgb(55,65,81), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnUp.FlatAppearance.BorderSize = btnDown.FlatAppearance.BorderSize = 0;

            btnUp.Click += (s, e) =>
            {
                int i = clb.SelectedIndex;
                if (i <= 0) return;
                var item    = clb.Items[i];
                bool chk    = clb.GetItemChecked(i);
                clb.Items.RemoveAt(i);
                clb.Items.Insert(i - 1, item);
                clb.SetItemChecked(i - 1, chk);
                clb.SelectedIndex = i - 1;
            };
            btnDown.Click += (s, e) =>
            {
                int i = clb.SelectedIndex;
                if (i < 0 || i >= clb.Items.Count - 1) return;
                var item    = clb.Items[i];
                bool chk    = clb.GetItemChecked(i);
                clb.Items.RemoveAt(i);
                clb.Items.Insert(i + 1, item);
                clb.SetItemChecked(i + 1, chk);
                clb.SelectedIndex = i + 1;
            };

            var btnOk     = new Button { Text = "✅ حفظ",   Width = 100, Height = 32, BackColor = Color.FromArgb(46,204,113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "❌ إلغاء", Width = 80,  Height = 32, BackColor = Color.FromArgb(200,50,50),  ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnOk.FlatAppearance.BorderSize = btnCancel.FlatAppearance.BorderSize = 0;

            var pnlArrows = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 40,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = Color.Transparent,
                Padding       = new Padding(5, 5, 5, 0)
            };
            pnlArrows.Controls.AddRange(new Control[] { btnDown, btnUp });

            var pnlFooter = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 44,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = Color.Transparent,
                Padding       = new Padding(5, 5, 5, 0)
            };
            pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnOk });

            dlg.Controls.Add(clb);
            dlg.Controls.Add(pnlArrows);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(lblHint);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // تطبيق الترتيب والإظهار على الجدول
                int displayIndex = 0;
                var hiddenNames  = new System.Collections.Generic.List<string>();
                var orderedNames = new System.Collections.Generic.List<string>();

                for (int i = 0; i < clb.Items.Count; i++)
                {
                    if (!(clb.Items[i] is ColEntry ce)) continue;
                    orderedNames.Add(ce.ColName);
                    bool visible = clb.GetItemChecked(i);
                    if (!visible) hiddenNames.Add(ce.ColName);

                    if (dgItems.Columns.Contains(ce.ColName))
                    {
                        dgItems.Columns[ce.ColName].Visible      = visible;
                        dgItems.Columns[ce.ColName].DisplayIndex = displayIndex++;
                    }
                }
                // عمود الحذف دائماً في الآخر
                if (dgItems.Columns.Contains("Delete"))
                    dgItems.Columns["Delete"].DisplayIndex = dgItems.ColumnCount - 1;

                SaveColumnSettings(orderedNames, hiddenNames);
            }
        }

        /// <summary>يحفظ ترتيب الأعمدة وما هو مخفي في Settings.ini</summary>
        private void SaveColumnSettings(
            System.Collections.Generic.List<string> ordered,
            System.Collections.Generic.List<string> hidden)
        {
            try
            {
                Core.LicenseManager.WriteIniValue("SaleGridColumns", "Order",  string.Join(",", ordered));
                Core.LicenseManager.WriteIniValue("SaleGridColumns", "Hidden", string.Join(",", hidden));
            }
            catch { }
        }

        /// <summary>يحمّل ترتيب الأعمدة من Settings.ini عند بداية التشغيل</summary>
        private void LoadColumnSettings()
        {
            try
            {
                string orderVal  = Core.LicenseManager.ReadIniValue("SaleGridColumns", "Order",  "");
                string hiddenVal = Core.LicenseManager.ReadIniValue("SaleGridColumns", "Hidden", "");

                if (string.IsNullOrWhiteSpace(orderVal)) return;

                var ordered = new System.Collections.Generic.List<string>(
                    orderVal.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));
                var hidden  = new System.Collections.Generic.List<string>(
                    string.IsNullOrEmpty(hiddenVal) ? new string[0]
                    : hiddenVal.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));

                int displayIndex = 0;
                foreach (string colName in ordered)
                {
                    if (!dgItems.Columns.Contains(colName)) continue;
                    dgItems.Columns[colName].Visible      = !hidden.Contains(colName);
                    dgItems.Columns[colName].DisplayIndex = displayIndex++;
                }
                if (dgItems.Columns.Contains("Delete"))
                    dgItems.Columns["Delete"].DisplayIndex = dgItems.ColumnCount - 1;
            }
            catch { }
        }

        // مساعد: تمثيل عمود في القائمة
        private class ColEntry
        {
            public string ColName    { get; }
            public string HeaderText { get; }
            public ColEntry(string n, string h) { ColName = n; HeaderText = h; }
            public override string ToString() => HeaderText;
        }

		private void InitializeScale()
		{
			if (AppConfig.ScaleEnabled)
			{
				ScaleService.Instance.WeightChanged += Scale_WeightChanged;
				ScaleService.Instance.ErrorOccurred += Scale_ErrorOccurred;
				
				lblLiveWeight.Text = "⚖️ الميزان: جاري الاتصال...";
				lblLiveWeight.ForeColor = Theme.TextSub;
				
				System.Threading.Tasks.Task.Run(() => {
					ScaleService.Instance.Connect(AppConfig.ScaleComPort, AppConfig.ScaleBaudRate);
				});
			}
		}

		private void Scale_WeightChanged(decimal weight, bool isStable)
		{
			if (this.IsDisposed) return;
			
			this.BeginInvoke((Action)delegate {
				lblLiveWeight.Text = $"⚖️ الميزان: {weight:F3} كجم {(isStable ? "🟢" : "🟡")}";
				lblLiveWeight.ForeColor = isStable ? Color.FromArgb(46, 204, 113) : Theme.Accent;
			});
		}

		private void Scale_ErrorOccurred(string error)
		{
			if (this.IsDisposed) return;
			
			this.BeginInvoke((Action)delegate {
				lblLiveWeight.Text = "⚖️ الميزان: 🔴 خطأ في الاتصال!";
				lblLiveWeight.ForeColor = Theme.Danger;
			});
		}

		private void ReadWeightToSelectedRow()
		{
			if (!AppConfig.ScaleEnabled)
			{
				MessageBox.Show("الميزان غير مفعّل في الإعدادات.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			if (!ScaleService.Instance.IsConnected)
			{
				MessageBox.Show("الميزان غير متصل حالياً! تأكد من التوصيل والإعدادات.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			
			decimal weight = ScaleService.Instance.CurrentWeight;
			if (weight <= 0m)
			{
				MessageBox.Show("الوزن المقروء صفر أو سالب!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (dgItems.CurrentRow != null && dgItems.CurrentRow.Index >= 0 && dgItems.CurrentRow.Index < _items.Count)
			{
				int rowIndex = dgItems.CurrentRow.Index;
				SaleItemDTO saleItemDTO = _items[rowIndex];
				
				decimal productStock = _stockCache.TryGetValue(saleItemDTO.ProductID, out var sVal) ? sVal : 0m;
				if (weight > productStock)
				{
					MessageBox.Show($"❌ خطأ: الوزن المقروء ({weight:N2}) أكبر من الكمية المتاحة في المخزن حالياً ({productStock:N2})!", "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}

				saleItemDTO.Quantity = weight;
				decimal gross = saleItemDTO.Quantity * saleItemDTO.UnitPrice;
				saleItemDTO.DiscountAmt = Math.Round(gross * saleItemDTO.DiscountPct / 100m, 2);

				dgItems.Rows[rowIndex].Cells["Quantity"].Value = saleItemDTO.Quantity.ToString("F2");
				dgItems.Rows[rowIndex].Cells["DiscountPct"].Value = saleItemDTO.DiscountPct.ToString("F2");
				dgItems.Rows[rowIndex].Cells["DiscountAmt"].Value = saleItemDTO.DiscountAmt.ToString("F2");
				dgItems.Rows[rowIndex].Cells["TotalPrice"].Value = saleItemDTO.TotalPrice.ToString("F2");

				CalculateNet();
			}
			else
			{
				MessageBox.Show("يرجى اختيار صنف من الجدول أولاً لقراءة الوزن إليه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
	}
	internal class ComboItem
	{
		public int ID { get; }

		public string Name { get; }

		public string Text { get; }
        
		public decimal Extra { get; set; }

		public decimal Price { get; }
        
		public decimal MinStockLimit { get; }

		public decimal PurchasePrice { get; }

		public string PartNumber { get; set; } = "";
		public string CarModel { get; set; } = "";
		public string Brand { get; set; } = "";
		public string ShelfLocation { get; set; } = "";
		public string Barcode { get; set; } = "";
		public string PriceTier { get; set; } = "قطاعي";

		public ComboItem(int id, string text, decimal price = 0m, decimal minStockLimit = 0m, decimal purchasePrice = 0m)
		{
			ID = id;
			Name = text;
			Text = text;
			Price = price;
			MinStockLimit = minStockLimit;
			PurchasePrice = purchasePrice;
		}

		public ComboItem(int id, string name, string text, decimal price, decimal minStockLimit = 0m, decimal purchasePrice = 0m)
		{
			ID = id;
			Name = name;
			Text = text;
			Price = price;
			MinStockLimit = minStockLimit;
			PurchasePrice = purchasePrice;
		}

		public override string ToString()
		{
			return Text;
		}
	}
}


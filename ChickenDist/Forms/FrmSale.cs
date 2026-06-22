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

		private Button btnTypeInstallment;

		private string _invoiceType = "Credit";

		private Label lblClient;

		private Label lblDriver;

		private Label lblDate;

		private Label lblNotes;

		private ComboBox cboClient;
		private Label lblClientBalance;

		private ComboBox cboDriver;

		private DateTimePicker dtpDate;

		private TextBox txtNotes;

		private Button btnAddItem;

		private Button btnSave;

		private Button btnNew;

		private Button btnPrint;

		private Button btnPreview;

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
		private decimal? _pendingBarcodeWeight = null;
		private decimal? _pendingScaleWeight = null;
		// FIX: cache أرصدة المخزون لتفادي رحلة DB لكل صنف عند الاختيار
		private Dictionary<int, decimal> _stockCache = new Dictionary<int, decimal>();

		private int _lastSaleID = 0;
        private bool _isDirty = false;
        private int _editSaleID = 0;
        private bool _isCopyMode = false;
        private bool _isScanningBarcode = false;
        private DateTime _loadedLastModified;
        // ── Auto-barcode detection ─────────────────────────────────────────────
        private System.Windows.Forms.Timer _barcodeTimer;
        private DateTime _lastKeyTime = DateTime.MinValue;
        private const int BARCODE_INTERVAL_MS = 50;
        private const int BARCODE_MIN_LENGTH = 4;
		private Button btnTierRetail;
		private Button btnTierSemi;
		private Button btnTierWholesale;
		private string _selectedTier = "قطاعي";
		private ComboBox cboWarehouse;
		private ComboBox cboSafeAccount;
		private Label lblSafeAccount;
		private Button btnCustomizeCols; // زر تخصيص الأعمدة
		private int _pendingRowIdx = -1; // سطر إدخال الكود المعلق
		private Label lblCratesOut;
		private NumericUpDown nudCratesOut;
		private Label lblCratesIn;
		private NumericUpDown nudCratesIn;
		private Label lblClientCratesBalance;

		public FrmSale() : this(0, false)
		{
		}

		public FrmSale(int saleID, bool isCopyMode = false)
		{
			_editSaleID = isCopyMode ? 0 : saleID;
			_isCopyMode = isCopyMode;
			InitUI();
			LoadCombos();
			ApplyInvoiceTypePermissions();
			if (saleID > 0)
			{
				LoadInvoiceForEdit(saleID);
			}
			
			// Scale Service Hook
			if (AppConfig.ScaleEnabled)
			{
				ScaleService.Instance.WeightChanged += ScaleService_WeightChanged;
			}
			this.Load += (s, e) => { this.ActiveControl = cboProduct; cboProduct.Focus(); };
		}

		private void ScaleService_WeightChanged(decimal weight, bool isStable)
		{
			if (isStable)
			{
				_pendingScaleWeight = weight;
			}
		}

		private void InitUI()
		{
			Text = "شاشة المبيعات";
			base.Size = new Size(1020, 680);
			base.StartPosition = FormStartPosition.CenterScreen;
			RightToLeft = RightToLeft.Yes;
			RightToLeftLayout = true;
			BackColor = Theme.BgMain;
			Font = Theme.FontMain;
            KeyPreview = true;
            this.KeyDown += FrmSale_KeyDown;
            this.FormClosing += FrmSale_FormClosing;
            // ── تهيئة Timer الباركود التلقائي ──────────────────────────────
            _barcodeTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _barcodeTimer.Tick += BarcodeTimer_Tick;
			Panel panel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 225,
				Width = 1020,
				BackColor = Theme.BgCard,
				Padding = new Padding(12, 8, 12, 8)
			};
			var tbl = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				RowCount = 5,
				ColumnCount = 6,
				BackColor = Color.Transparent,
				CellBorderStyle = TableLayoutPanelCellBorderStyle.None
			};
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // col0: label
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));  // col1: control
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // col2: label
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));  // col3: control
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // col4: label
			tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));  // col5: control / buttons

			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
			tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

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
				DropDownWidth = 250,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 6, 2, 6)
			};
			SetupSearchableCombo(cboClient);

			lblClientBalance = new Label
			{
				Text = "رصيد: 0.00 ج",
				Width = 115,
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				ForeColor = Theme.Accent,
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Left,
				Margin = new Padding(2, 6, 2, 6)
			};

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
			pnlClient.Controls.Add(lblClientBalance);
			pnlClient.Controls.Add(btnClientStatement);

			btnClientStatement.SendToBack();
			lblClientBalance.SendToBack();

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

			btnTypeInstallment = new Button
			{
				Text = "تقسيط شرعي",
				Width = 95,
				Height = 28,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Margin = new Padding(2)
			};
			btnTypeInstallment.FlatAppearance.BorderSize = 0;
			btnTypeInstallment.Click += delegate
			{
				SetInvoiceType("Installment");
			};

			var pnlTypeBtns = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.RightToLeft,
				BackColor = Color.Transparent,
				Dock = DockStyle.Fill,
				WrapContents = false,
				Margin = new Padding(0, 4, 0, 0)
			};
			pnlTypeBtns.Controls.Add(btnTypeInstallment);
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
			cboProduct.KeyDown += CboProduct_KeyDown;
			cboProduct.KeyPress += CboProduct_KeyPress_BarcodeDetect; // اكتشاف الباركود التلقائي

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

			var btnManualAdd = new Button
			{
				Text = "➕",
				Width = 35,
				Height = 28,
				BackColor = Color.FromArgb(40, 167, 69),
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Dock = DockStyle.Left,
				Margin = new Padding(2, 6, 2, 6)
			};
			btnManualAdd.FlatAppearance.BorderSize = 0;
			btnManualAdd.Click += BtnManualAdd_Click;

			pnlProduct.Controls.Add(cboProduct);
			pnlProduct.Controls.Add(btnSearchProduct);
			pnlProduct.Controls.Add(btnManualAdd);

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

			lblSafeAccount = MakeLabel("حساب الدفع :", 0, 0);
			lblSafeAccount.Dock = DockStyle.Fill;
			lblSafeAccount.TextAlign = ContentAlignment.MiddleRight;
			lblSafeAccount.Margin = new Padding(2);

			cboSafeAccount = new ComboBox
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 6, 2, 6)
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
			tbl.Controls.Add(lblSafeAccount, 4, 2);
			tbl.Controls.Add(cboSafeAccount, 5, 2);

			tbl.Controls.Add(lblTierRow, 0, 3);
			tbl.Controls.Add(pnlTierBtns, 1, 3);
			tbl.SetColumnSpan(pnlTierBtns, 5);

			lblCratesOut = MakeLabel("أقفاص صادرة :", 0, 0);
			lblCratesOut.Dock = DockStyle.Fill;
			lblCratesOut.TextAlign = ContentAlignment.MiddleRight;
			lblCratesOut.Margin = new Padding(2);

			nudCratesOut = new NumericUpDown
			{
				Dock = DockStyle.Fill,
				Minimum = 0,
				Maximum = 9999,
				DecimalPlaces = 0,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				Margin = new Padding(2, 6, 2, 6)
			};

			lblCratesIn = MakeLabel("أقفاص واردة :", 0, 0);
			lblCratesIn.Dock = DockStyle.Fill;
			lblCratesIn.TextAlign = ContentAlignment.MiddleRight;
			lblCratesIn.Margin = new Padding(2);

			nudCratesIn = new NumericUpDown
			{
				Dock = DockStyle.Fill,
				Minimum = 0,
				Maximum = 9999,
				DecimalPlaces = 0,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				Margin = new Padding(2, 6, 2, 6)
			};

			lblClientCratesBalance = new Label
			{
				Text = "أقفاص العميل: 0 قفص",
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				ForeColor = Theme.Accent,
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				Margin = new Padding(2, 6, 2, 6)
			};

			tbl.Controls.Add(lblCratesOut, 0, 4);
			tbl.Controls.Add(nudCratesOut, 1, 4);
			tbl.Controls.Add(lblCratesIn, 2, 4);
			tbl.Controls.Add(nudCratesIn, 3, 4);
			tbl.Controls.Add(lblClientCratesBalance, 4, 4);
			tbl.SetColumnSpan(lblClientCratesBalance, 2);

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
			// عمود إدخال الكود (أول عمود - قابل للكتابة مباشرة)
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name       = "CodeEntry",
				HeaderText = "كود الصنف",
				ReadOnly   = false,
				FillWeight = 55f
			});
			dgItems.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "ProductName",
				HeaderText = "الصنف",
				ReadOnly = true
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
			dgItems.Columns.Add(new DataGridViewComboBoxColumn
			{
				Name = "UnitName",
				HeaderText = "الوحدة",
				ReadOnly = false,
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
			dgItems.RowsAdded   += (s, e) => _isDirty = true;
			dgItems.RowsRemoved += (s, e) => _isDirty = true;
			// سهم لأسفل في آخر سطر → يفتح سطر إدخال كود جديد | Insert = نفس الشيء
			dgItems.KeyDown += (s, ke) =>
			{
				if (ke.KeyCode == Keys.Down && dgItems.CurrentCell != null)
				{
					int lastReal = _items.Count - 1;
					if (dgItems.CurrentCell.RowIndex >= lastReal && _pendingRowIdx < 0)
					{
						ke.Handled = true;
						AddNewCodeRow();
					}
				}
				else if (ke.KeyCode == Keys.Insert)
				{
					ke.Handled = true;
					AddNewCodeRow();
				}
			};
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
				Height = 110,
				Width = 950,
				BackColor = Theme.BgCard
			};
			Label label5 = new Label
			{
				Text = "إجمالي الأصناف:",
				ForeColor = Theme.TextSub,
				Location = new Point(830, 15),
				AutoSize = true,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			lblTotalVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.TextMain,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				Location = new Point(740, 13),
				AutoSize = true,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			Label lblDiscType = new Label
			{
				Text = "نوع الخصم:",
				ForeColor = Theme.TextSub,
				Location = new Point(660, 15),
				AutoSize = true,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			cboInvoiceDiscountType = new ComboBox
			{
				Location = new Point(570, 11),
				Width = 80,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			cboInvoiceDiscountType.Items.AddRange(new object[] { "قيمة", "نسبة %" });
			cboInvoiceDiscountType.SelectedIndex = 0;
			cboInvoiceDiscountType.SelectedIndexChanged += (s, e) => CalculateNet();

			Label lblDiscVal = new Label
			{
				Text = "خصم الفاتورة:",
				ForeColor = Theme.TextSub,
				Location = new Point(480, 15),
				AutoSize = true,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			txtInvoiceDiscount = new TextBox
			{
				Location = new Point(390, 11),
				Width = 80,
				Text = "0",
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				BorderStyle = BorderStyle.FixedSingle,
				RightToLeft = RightToLeft.Yes,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			txtInvoiceDiscount.TextChanged += (s, e) => CalculateNet();

			Label lblNetTitle = new Label
			{
				Text = "صافي الفاتورة:",
				ForeColor = Theme.TextSub,
				Location = new Point(280, 15),
				AutoSize = true,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			lblNetVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.Accent,
				Font = new Font("Segoe UI", 14f, FontStyle.Bold),
				Location = new Point(160, 10),
				AutoSize = true,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			lblCostTitle = new Label
			{
				Text = "إجمالي التكلفة:",
				ForeColor = Theme.TextSub,
				Location = new Point(830, 42),
				AutoSize = true,
				Visible = Session.CanViewCost("Sales"),
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			lblCostVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.TextMain,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				Location = new Point(740, 40),
				AutoSize = true,
				Visible = Session.CanViewCost("Sales"),
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			lblProfitTitle = new Label
			{
				Text = "صافي الربح:",
				ForeColor = Theme.TextSub,
				Location = new Point(660, 42),
				AutoSize = true,
				Visible = Session.CanViewCost("Sales"),
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			lblProfitVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.Success,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				Location = new Point(570, 40),
				AutoSize = true,
				Visible = Session.CanViewCost("Sales"),
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			btnSave = Theme.MakeButton("💾 حفظ الفاتورة", 0, 0, 130, 32, Theme.Accent);
            Button btnHold = Theme.MakeButton("⏸️ تعليق", 0, 0, 100, 32, Color.FromArgb(200, 140, 50));
			Button btnLoadHold = Theme.MakeButton("📂 معلقات", 0, 0, 100, 32, Color.FromArgb(100, 100, 150));
			Button button = Theme.MakeButton("💵 توريد", 0, 0, 100, 32, Theme.Success);
			btnNew = Theme.MakeButton("🆕 جديد", 0, 0, 80, 32, Color.FromArgb(80, 120, 80));
			btnPrint = Theme.MakeButton("🖨️ طباعة الأخيرة", 0, 0, 140, 32, Theme.Primary);
			btnPreview = Theme.MakeButton("🔍 معاينة الأخيرة", 0, 0, 130, 32, Color.FromArgb(70, 80, 90));
			btnWhatsApp = Theme.MakeButton("📲 واتساب", 0, 0, 130, 32, Color.FromArgb(37, 211, 102));
			btnSave.Anchor = AnchorStyles.None;
            btnHold.Anchor = AnchorStyles.None;
            btnLoadHold.Anchor = AnchorStyles.None;
			button.Anchor = AnchorStyles.None;
			btnNew.Anchor = AnchorStyles.None;
			btnPrint.Anchor = AnchorStyles.None;
			btnPreview.Anchor = AnchorStyles.None;
			btnWhatsApp.Anchor = AnchorStyles.None;
			btnSave.Click += BtnSave_Click;
            btnHold.Click += BtnHold_Click;
            btnLoadHold.Click += BtnLoadHold_Click;
			button.Click += BtnTawreed_Click;
			btnNew.Click += delegate
			{
				ResetForm();
			};
			btnPrint.Click += BtnPrint_Click;
			btnPreview.Click += BtnPreview_Click;
			btnWhatsApp.Click += BtnWhatsApp_Click;
            Label lblHotkeys = new Label
            {
                Text = "الاختصارات: [F2] فاتورة جديدة  |  [F5] حفظ الفاتورة  |  [F9] طباعة  |  [F12] بحث سريع عن صنف",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(15, 70),
                AutoSize = true,
                Anchor = (AnchorStyles.Bottom | AnchorStyles.Left)
            };

            var pnlFooterButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Height = 42,
                Padding = new Padding(15, 5, 15, 5),
                BackColor = Color.Transparent,
                RightToLeft = RightToLeft.Yes,
                WrapContents = false,
                AutoSize = false
            };
            btnWhatsApp.Margin = new Padding(5, 5, 5, 5);
            btnPrint.Margin = new Padding(5, 5, 5, 5);
            btnPreview.Margin = new Padding(5, 5, 5, 5);
            btnNew.Margin = new Padding(5, 5, 5, 5);
            button.Margin = new Padding(5, 5, 5, 5);
            btnLoadHold.Margin = new Padding(5, 5, 5, 5);
            btnHold.Margin = new Padding(5, 5, 5, 5);
            btnSave.Margin = new Padding(5, 5, 5, 5);
            pnlFooterButtons.Controls.AddRange(new Control[] { btnWhatsApp, btnPrint, btnPreview, btnNew, button, btnLoadHold, btnHold, btnSave });

			pnlFooter.Controls.AddRange(new Control[] { label5, lblTotalVal, lblDiscType, cboInvoiceDiscountType, lblDiscVal, txtInvoiceDiscount, lblNetTitle, lblNetVal, lblCostTitle, lblCostVal, lblProfitTitle, lblProfitVal, pnlFooterButtons, lblHotkeys });
			base.Controls.Add(pnlItems);
			base.Controls.Add(pnlFooter);
			base.Controls.Add(panel);
            pnlItems.BringToFront();
			ToggleType();
			Theme.ApplyFormRTL(this);
			if (!AppConfig.EnableCratesTracking)
			{
				tbl.RowStyles[4] = new RowStyle(SizeType.Absolute, 0f);
				panel.Height = 183;
				lblCratesOut.Visible = false;
				nudCratesOut.Visible = false;
				lblCratesIn.Visible = false;
				nudCratesIn.Visible = false;
				lblClientCratesBalance.Visible = false;
			}
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

			if      (e.KeyCode == Keys.F2)  { btnNew.PerformClick(); e.Handled = true; }
			else if (e.KeyCode == Keys.F5)  { btnSave.PerformClick(); e.Handled = true; }
			else if (e.KeyCode == Keys.F9)  { btnPrint.PerformClick(); e.Handled = true; }
			else if (e.KeyCode == Keys.F12) { cboProduct.Focus(); e.Handled = true; }
			else if (e.KeyCode == Keys.F3)  { btnSearchProduct.PerformClick(); e.Handled = true; } // F3 = فتح شاشة البحث
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Insert)
			{
				AddNewCodeRow();
				return true;
			}
			if (keyData == Keys.Enter)
			{
				if (dgItems.Focused || dgItems.EditingControl != null)
				{
					var curCell = dgItems.CurrentCell;
					if (curCell != null && curCell.RowIndex >= 0 && curCell.RowIndex < dgItems.Rows.Count)
					{
						if (curCell.RowIndex >= _items.Count)
						{
							dgItems.EndEdit();
							return true;
						}
						int productID = _items[curCell.RowIndex].ProductID;

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

						dgItems.EndEdit();

						if (nextCol != -1)
						{
							string nextColName = dgItems.Columns[nextCol].Name;
							this.BeginInvoke((MethodInvoker)delegate
							{
								int targetRowIndex = -1;
								for (int i = 0; i < _items.Count; i++)
								{
									if (_items[i].ProductID == productID)
									{
										targetRowIndex = i;
										break;
									}
								}
								if (targetRowIndex >= 0 && targetRowIndex < dgItems.Rows.Count)
								{
									dgItems.Focus();
									dgItems.ClearSelection();
									dgItems.CurrentCell = dgItems.Rows[targetRowIndex].Cells[nextColName];
									dgItems.BeginEdit(true);
								}
								else
								{
									cboProduct.Focus();
								}
							});
							return true;
						}
						else
						{
							this.BeginInvoke((MethodInvoker)delegate
							{
								cboProduct.Focus();
							});
							return true;
						}
					}
					else
					{
						dgItems.EndEdit();
						this.BeginInvoke((MethodInvoker)delegate
						{
							cboProduct.Focus();
						});
						return true;
					}
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private bool MatchBarcode(string barcodes, string scanText)
		{
			if (string.IsNullOrEmpty(barcodes)) return false;
			var parts = barcodes.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in parts)
			{
				if (string.Equals(part.Trim(), scanText, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		// ── اكتشاف الباركود التلقائي ───────────────────────────────────────
		private void CboProduct_KeyPress_BarcodeDetect(object sender, KeyPressEventArgs e)
		{
			var now = DateTime.Now;
			var interval = (now - _lastKeyTime).TotalMilliseconds;
			_lastKeyTime = now;
			_barcodeTimer.Stop();
			if (interval <= BARCODE_INTERVAL_MS || interval == (DateTime.Now - DateTime.MinValue).TotalMilliseconds)
				_barcodeTimer.Start();
		}

		private void BarcodeTimer_Tick(object sender, EventArgs e)
		{
			_barcodeTimer.Stop();
			string text = cboProduct.Text?.Trim();
			if (string.IsNullOrWhiteSpace(text) || text.Length < BARCODE_MIN_LENGTH) return;

			var res = BarcodeParser.Parse(text);

			List<ComboItem> allItems = cboProduct.Tag as List<ComboItem>;
			if (allItems == null)
			{
				allItems = new List<ComboItem>();
				foreach (var item in cboProduct.Items)
					if (item is ComboItem ci) allItems.Add(ci);
			}

			ComboItem foundItem = null;

			if (res.IsScaleBarcode)
			{
				_pendingBarcodeWeight = res.WeightOrPrice;
				foreach (var ci in allItems)
				{
					if (ci.ID > 0 && (ci.ID.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode || ci.PartNumber == res.ItemCode))
					{
						foundItem = ci;
						break;
					}
				}
				if (foundItem == null) { _pendingBarcodeWeight = null; return; }
			}
			else
			{
				foreach (var ci in allItems)
				{
					if (ci.ID > 0 &&
						(string.Equals(ci.ProductCode, text, StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(ci.PartNumber, text, StringComparison.OrdinalIgnoreCase) ||
						 MatchBarcode(ci.InternationalCode, text)))
					{
						foundItem = ci;
						break;
					}
				}
			}

			if (foundItem != null)
			{
				decimal qtyToAdd = _pendingBarcodeWeight ?? (_pendingScaleWeight ?? 1.00m);
				_pendingBarcodeWeight = null;
				_pendingScaleWeight = null;

				_isScanningBarcode = true;
				try
				{
					AddOrUpdateProduct(foundItem.ID, qtyToAdd);
					cboProduct.Text = "";
					cboProduct.Items.Clear();
					cboProduct.Items.AddRange(allItems.ToArray());
					cboProduct.SelectedIndex = 0;
					cboProduct.Focus();
				}
				finally
				{
					_isScanningBarcode = false;
				}
			}
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

		private void CboProduct_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(cboProduct.Text))
			{
				var res = BarcodeParser.Parse(cboProduct.Text);
				
				// Get unfiltered product list
				List<ComboItem> allItems = cboProduct.Tag as List<ComboItem>;
				if (allItems == null)
				{
					allItems = new List<ComboItem>();
					foreach (var item in cboProduct.Items)
					{
						if (item is ComboItem ci) allItems.Add(ci);
					}
				}

				ComboItem foundItem = null;

				if (res.IsScaleBarcode)
				{
					_pendingBarcodeWeight = res.WeightOrPrice;
					
					// Search for item by scale code in unfiltered list
					foreach (var ci in allItems)
					{
						if (ci.ID > 0 && (ci.ID.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode || ci.PartNumber == res.ItemCode))
						{
							foundItem = ci;
							break;
						}
					}
					if (foundItem == null)
					{
						MessageBox.Show("لم يتم العثور على الصنف الخاص بباركود الميزان!");
						_pendingBarcodeWeight = null;
						return;
					}
				}
				else
				{
					string scanText = cboProduct.Text.Trim();
					foreach (var ci in allItems)
					{
						if (ci.ID > 0 && 
							(string.Equals(ci.ProductCode, scanText, StringComparison.OrdinalIgnoreCase) || 
							 string.Equals(ci.PartNumber, scanText, StringComparison.OrdinalIgnoreCase) || 
							 MatchBarcode(ci.InternationalCode, scanText)))
						{
							foundItem = ci;
							break;
						}
					}
				}

				if (foundItem != null)
				{
					e.Handled = true;
					e.SuppressKeyPress = true;

					decimal qtyToAdd = _pendingBarcodeWeight ?? (_pendingScaleWeight ?? 1.00m);
					_pendingBarcodeWeight = null;
					_pendingScaleWeight = null;

					_isScanningBarcode = true;
					try
					{
						AddOrUpdateProduct(foundItem.ID, qtyToAdd);
						
						cboProduct.Text = "";
						cboProduct.Items.Clear();
						cboProduct.Items.AddRange(allItems.ToArray());
						cboProduct.SelectedIndex = 0;
						cboProduct.Focus();
					}
					finally
					{
						_isScanningBarcode = false;
					}
					return;
				}
			}
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
						if (item2.ID == 0 || 
							item2.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
							(item2.ProductCode != null && item2.ProductCode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							(item2.PartNumber != null && item2.PartNumber.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							(item2.InternationalCode != null && item2.InternationalCode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
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
                    UpdateClientBalanceLabel(comboItem2.ID);
				}
                else
                {
                    this.BackColor = Theme.BgMain;
                    pnlItems.Enabled = true;
                    btnSave.Enabled = true;
                    if (lblClientBalance != null)
                    {
                        lblClientBalance.Text = "رصيد: 0.00 ج";
                        lblClientBalance.ForeColor = Theme.Accent;
                    }
                    if (lblClientCratesBalance != null)
                    {
                        lblClientCratesBalance.Text = "أقفاص العميل: 0 قفص";
                    }
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
				string name = row3["ProductName"].ToString();
				decimal price = (decimal)row3["SalePrice"];
				decimal pendingPrice = row3["PendingSalePrice"] != DBNull.Value ? Convert.ToDecimal(row3["PendingSalePrice"]) : 0m;
				decimal pendingQtyThreshold = row3["PendingQtyThreshold"] != DBNull.Value ? Convert.ToDecimal(row3["PendingQtyThreshold"]) : 0m;
				decimal purchasePrice = row3["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row3["PurchasePrice"]) : 0m;

				// ── بيانات الوحدات المتعددة (مشتركة بين كل فروع الـ if/else) ──
				string unit1Name   = row3.Table.Columns.Contains("Unit1Name")   && row3["Unit1Name"]   != DBNull.Value ? row3["Unit1Name"].ToString()   : null;
				decimal unit1SP    = row3.Table.Columns.Contains("Unit1SalePrice") && row3["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(row3["Unit1SalePrice"]) : 0m;
				decimal unit1PP    = row3.Table.Columns.Contains("Unit1PurchasePrice") && row3["Unit1PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row3["Unit1PurchasePrice"]) : 0m;
				string unit2Name   = row3.Table.Columns.Contains("Unit2Name")   && row3["Unit2Name"]   != DBNull.Value ? row3["Unit2Name"].ToString()   : null;
				decimal unit2Factor = row3.Table.Columns.Contains("Unit2Factor") && row3["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(row3["Unit2Factor"]) : 1m;
				decimal unit2SP    = row3.Table.Columns.Contains("Unit2SalePrice") && row3["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(row3["Unit2SalePrice"]) : 0m;
				decimal unit2PP    = row3.Table.Columns.Contains("Unit2PurchasePrice") && row3["Unit2PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row3["Unit2PurchasePrice"]) : 0m;
				decimal unit3Factor = row3.Table.Columns.Contains("Unit3Factor") && row3["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(row3["Unit3Factor"]) : 1m;
				string baseUnit    = row3.Table.Columns.Contains("Unit")         && row3["Unit"]         != DBNull.Value ? row3["Unit"].ToString()         : "";

				if (pendingPrice > 0m && pendingQtyThreshold > 0m)
				{
					// إضافة السعر الحالي كخيار مستقل
					var itemOld = new ComboItem(
						(int)row3["ProductID"], 
						name,
						$"{name} (سعر: {price:N2})",
						price, 
						row3["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(row3["MinStockLimit"]) : 0m,
						purchasePrice
					);
					itemOld.PendingSalePrice = 0m;
					itemOld.PendingQtyThreshold = 0m;
					itemOld.PartNumber = row3["PartNumber"]?.ToString() ?? "";
					itemOld.CarModel = row3["CarModel"]?.ToString() ?? "";
					itemOld.Brand = row3["Brand"]?.ToString() ?? "";
					itemOld.ShelfLocation = row3["ShelfLocation"]?.ToString() ?? "";
					itemOld.ProductCode = row3["ProductCode"]?.ToString() ?? "";
					itemOld.InternationalCode = row3["InternationalCode"]?.ToString() ?? "";
					itemOld.IsService = row3.Table.Columns.Contains("IsService") && row3["IsService"] != DBNull.Value && Convert.ToBoolean(row3["IsService"]);
					// وحدات متعددة
					itemOld.BaseUnitName = baseUnit;
					itemOld.Unit1Name = unit1Name; itemOld.Unit1SalePrice = unit1SP; itemOld.Unit1PurchasePrice = unit1PP; itemOld.Unit1Factor = 1m;
					itemOld.Unit2Name = unit2Name; itemOld.Unit2Factor = unit2Factor; itemOld.Unit2SalePrice = unit2SP; itemOld.Unit2PurchasePrice = unit2PP;
					itemOld.Unit3Factor = unit3Factor;
					cboProduct.Items.Add(itemOld);

					// إضافة السعر المعلق كخيار مستقل
					var itemPending = new ComboItem(
						(int)row3["ProductID"], 
						name,
						$"{name} (معلق: {pendingPrice:N2})",
						pendingPrice, 
						row3["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(row3["MinStockLimit"]) : 0m,
						purchasePrice
					);
					itemPending.PendingSalePrice = 0m;
					itemPending.PendingQtyThreshold = 0m;
					itemPending.PartNumber = row3["PartNumber"]?.ToString() ?? "";
					itemPending.CarModel = row3["CarModel"]?.ToString() ?? "";
					itemPending.Brand = row3["Brand"]?.ToString() ?? "";
					itemPending.ShelfLocation = row3["ShelfLocation"]?.ToString() ?? "";
					itemPending.ProductCode = row3["ProductCode"]?.ToString() ?? "";
					itemPending.InternationalCode = row3["InternationalCode"]?.ToString() ?? "";
					itemPending.IsService = row3.Table.Columns.Contains("IsService") && row3["IsService"] != DBNull.Value && Convert.ToBoolean(row3["IsService"]);
					// وحدات متعددة
					itemPending.BaseUnitName = baseUnit;
					itemPending.Unit1Name = unit1Name; itemPending.Unit1SalePrice = unit1SP; itemPending.Unit1PurchasePrice = unit1PP; itemPending.Unit1Factor = 1m;
					itemPending.Unit2Name = unit2Name; itemPending.Unit2Factor = unit2Factor; itemPending.Unit2SalePrice = unit2SP; itemPending.Unit2PurchasePrice = unit2PP;
					itemPending.Unit3Factor = unit3Factor;
					cboProduct.Items.Add(itemPending);
				}
				else
				{
					var comboItem = new ComboItem(
						(int)row3["ProductID"], 
						name,
						$"{name} ({price:N2})",
						price, 
						row3["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(row3["MinStockLimit"]) : 0m,
						purchasePrice
					);
					comboItem.PendingSalePrice = pendingPrice;
					comboItem.PendingQtyThreshold = pendingQtyThreshold;
					comboItem.PartNumber = row3["PartNumber"]?.ToString() ?? "";
					comboItem.CarModel = row3["CarModel"]?.ToString() ?? "";
					comboItem.Brand = row3["Brand"]?.ToString() ?? "";
					comboItem.ShelfLocation = row3["ShelfLocation"]?.ToString() ?? "";
					comboItem.ProductCode = row3["ProductCode"]?.ToString() ?? "";
					comboItem.InternationalCode = row3["InternationalCode"]?.ToString() ?? "";
					comboItem.IsService = row3.Table.Columns.Contains("IsService") && row3["IsService"] != DBNull.Value && Convert.ToBoolean(row3["IsService"]);
					// وحدات متعددة
					comboItem.BaseUnitName = baseUnit;
					comboItem.Unit1Name = unit1Name; comboItem.Unit1SalePrice = unit1SP; comboItem.Unit1PurchasePrice = unit1PP; comboItem.Unit1Factor = 1m;
					comboItem.Unit2Name = unit2Name; comboItem.Unit2Factor = unit2Factor; comboItem.Unit2SalePrice = unit2SP; comboItem.Unit2PurchasePrice = unit2PP;
					comboItem.Unit3Factor = unit3Factor;
					cboProduct.Items.Add(comboItem);
				}
			}
			cboProduct.DisplayMember = "Text";
			cboProduct.SelectedIndex = 0;
			cboProduct.SelectedIndexChanged += delegate
			{
				if (_isScanningBarcode) return;
				if (cboProduct.SelectedItem is ComboItem comboItem && comboItem.ID > 0)
				{
					decimal qtyToAdd = _pendingBarcodeWeight ?? (_pendingScaleWeight ?? 1.00m);
					_pendingBarcodeWeight = null;
					_pendingScaleWeight = null;

					AddOrUpdateProduct(comboItem.ID, qtyToAdd, comboItem.Price);

					int rowIndex = -1;
					for (int i = _items.Count - 1; i >= 0; i--)
					{
						if (_items[i].ProductID == comboItem.ID && Math.Abs(_items[i].UnitPrice - comboItem.Price) < 0.005m)
						{
							rowIndex = i;
							break;
						}
					}
					if (rowIndex >= 0)
					{
						dgItems.Focus();
						dgItems.ClearSelection();
						dgItems.CurrentCell = dgItems.Rows[rowIndex].Cells["Quantity"];
						dgItems.BeginEdit(true);
					}

					cboProduct.SelectedIndex = 0;
				}
			};
			dtpDate.Value = DateTime.Today;
			SetInvoiceType(GetDefaultAllowedInvoiceType());

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

			// تحميل الحسابات والخزائن
			try
			{
				DataTable safes = AccountDAL.GetActiveSafeAccounts();
				cboSafeAccount.Items.Clear();

				// Get allowed safes from Session
				System.Collections.Generic.HashSet<int> allowedSafes = null;
				if (Session.Role != "Admin")
				{
					allowedSafes = new System.Collections.Generic.HashSet<int>();
					if (!string.IsNullOrEmpty(Session.AllowedSafeIDs))
					{
						foreach (var part in Session.AllowedSafeIDs.Split(','))
						{
							if (int.TryParse(part, out int id))
								allowedSafes.Add(id);
						}
					}
				}

				int selectedIdx = -1;
				int defaultSafeID = Session.DefaultSafeID ?? 0;

				foreach (DataRow row in safes.Rows)
				{
					int accID = Convert.ToInt32(row["AccountID"]);
					if (allowedSafes != null && !allowedSafes.Contains(accID))
					{
						continue; // Filter out if not allowed
					}

					var comboItem = new ComboItem(accID, row["AccountName"].ToString());
					int addedIdx = cboSafeAccount.Items.Add(comboItem);

					if (accID == defaultSafeID)
					{
						selectedIdx = addedIdx;
					}
				}
				cboSafeAccount.DisplayMember = "Text";
				if (cboSafeAccount.Items.Count > 0)
				{
					cboSafeAccount.SelectedIndex = selectedIdx >= 0 ? selectedIdx : 0;
				}
			}
			catch { }
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
			btnTypeInstallment.BackColor = ((_invoiceType == "Installment") ? Theme.Accent : Theme.BgInput);
			btnTypeInstallment.ForeColor = ((_invoiceType == "Installment") ? Color.White : Theme.TextMain);
			ToggleType();
		}

		private string GetDefaultAllowedInvoiceType()
		{
			if (Session.Role == "Admin") return "Credit";
			if (Session.CanSellCredit) return "Credit";
			if (Session.CanSellCash) return "Cash";
			if (Session.CanSellDriverLoad) return "DriverLoad";
			if (Session.CanSellInstallment) return "Installment";
			return "Credit"; // Fallback
		}

		private void ApplyInvoiceTypePermissions()
		{
			if (Session.Role == "Admin") return;

			btnTypeCash.Visible = Session.CanSellCash;
			btnTypeCredit.Visible = Session.CanSellCredit;
			btnTypeDriverLoad.Visible = Session.CanSellDriverLoad;
			btnTypeInstallment.Visible = Session.CanSellInstallment;
		}

		private void ToggleType()
		{
			bool flag = _invoiceType == "Credit" || _invoiceType == "Installment";
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

            if ((limitExceeded || oldDebtExists) && (_invoiceType == "Credit" || _invoiceType == "Installment"))
            {
                this.BackColor = Color.FromArgb(255, 200, 200); // Light Red
                pnlItems.Enabled = false; 
                btnSave.Enabled = false;
                
                string msg = "⚠️ تحذير مالي ⚠️\n\n";
                if (limitExceeded) msg += $"- تجاوز العميل الحد الائتماني ({status.MaxCreditLimit:N2} ج). رصيده: {status.Balance:N2} ج.\n";
                if (oldDebtExists) msg += $"- ديون متأخرة (تجاوزت 30 يوم) بقيمة {status.OldDebt30:N2} ج لم تسدد.\n";
                
                MessageBox.Show(msg + "\nالبيع الآجل والتقسيط موقوف لهذا العميل حتى يتم السداد.", "إيقاف البيع", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else
            {
                this.BackColor = Theme.BgMain;
                pnlItems.Enabled = true;
                btnSave.Enabled = true;
            }
        }

        private void UpdateClientBalanceLabel(int clientID)
        {
            var status = ClientDAL.GetFinancialStatus(clientID);
            lblClientBalance.Text = "رصيد: " + status.Balance.ToString("N2") + " ج";
            if (status.Balance > 0)
            {
                lblClientBalance.ForeColor = Color.FromArgb(255, 110, 110); // Bright light red
            }
            else if (status.Balance < 0)
            {
                lblClientBalance.ForeColor = Color.FromArgb(100, 220, 100); // Bright light green
            }
            else
            {
                lblClientBalance.ForeColor = Theme.Accent;
            }

            int cratesBal = ClientDAL.GetClientCratesBalance(clientID);
            if (lblClientCratesBalance != null)
            {
                lblClientCratesBalance.Text = "أقفاص العميل: " + cratesBal + " قفص";
            }
        }

		private void BtnSearchProduct_Click(object sender, EventArgs e)
		{
			int? warehouseID = null;
			if (cboWarehouse.SelectedItem is ComboItem wci) warehouseID = wci.ID;

			using FrmProductSearch frmProductSearch = new FrmProductSearch(warehouseID);
			if (frmProductSearch.ShowDialog() == DialogResult.OK)
			{
				SelectProductByID(frmProductSearch.SelectedProductID, frmProductSearch.SelectedPrice);
			}
		}

		private void BtnManualAdd_Click(object sender, EventArgs e)
		{
			AddNewCodeRow();
		}

		/// <summary>يضيف سطراً فارغاً في الجدول ويضع الكيرسور على عمود كود الصنف مباشرة</summary>
		private void AddNewCodeRow()
		{
			// إزالة سطر الكود المعلق السابق إذا كان فارغاً
			if (_pendingRowIdx >= 0 && _pendingRowIdx < dgItems.Rows.Count)
			{
				var prevCell = dgItems.Rows[_pendingRowIdx].Cells["CodeEntry"];
				if (prevCell.Value == null || string.IsNullOrEmpty(prevCell.Value.ToString()))
					dgItems.Rows.RemoveAt(_pendingRowIdx);
			}

			// إضافة سطر فارغ جديد
			_pendingRowIdx = dgItems.Rows.Add();
			// تلوين السطر الجديد لتمييزه
			dgItems.Rows[_pendingRowIdx].DefaultCellStyle.BackColor = Color.FromArgb(30, 120, 190, 80);

			// الانتقال لخلية الكود في السطر الجديد
			try
			{
				dgItems.ClearSelection();
				dgItems.CurrentCell = dgItems.Rows[_pendingRowIdx].Cells["CodeEntry"];
				dgItems.BeginEdit(true);
				dgItems.FirstDisplayedScrollingRowIndex = _pendingRowIdx;
			}
			catch { }
		}

		private void SelectProductByID(int prodID, decimal price)
		{
			for (int i = 0; i < cboProduct.Items.Count; i++)
			{
				if (cboProduct.Items[i] is ComboItem comboItem && comboItem.ID == prodID && Math.Abs(comboItem.Price - price) < 0.005m)
				{
					cboProduct.SelectedIndex = i;
					break;
				}
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

			AddOrUpdateProduct(comboItem.ID, value, result);
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
			// معالجة خلية كود الصنف (السطر المعلق)
			if (e.ColumnIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "CodeEntry")
			{
				string code = dgItems.Rows[e.RowIndex].Cells["CodeEntry"].Value?.ToString()?.Trim() ?? "";
				int rowIdx  = e.RowIndex;
				this.BeginInvoke((MethodInvoker)delegate
				{
					if (string.IsNullOrEmpty(code))
					{
						// كود فارغ → حذف السطر المعلق
						if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
							dgItems.Rows.RemoveAt(rowIdx);
						_pendingRowIdx = -1;
						return;
					}
					var dt = ProductDAL.FindByCode(code);
					if (dt.Rows.Count > 0)
					{
						int productID = Convert.ToInt32(dt.Rows[0]["ProductID"]);
						// حذف السطر المعلق ثم إضافة الصنف الحقيقي
						if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
							dgItems.Rows.RemoveAt(rowIdx);
						_pendingRowIdx = -1;
						AddOrUpdateProduct(productID, 1.00m);
						// فتح سطر جديد للإدخال التالي
						AddNewCodeRow();
					}
					else
					{
						MessageBox.Show("❌ لم يتم العثور على صنف بالكود: " + code, "خطأ في الكود", MessageBoxButtons.OK, MessageBoxIcon.Error);
						// إعادة التركيز على خلية الكود
						if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
						{
							dgItems.CurrentCell = dgItems.Rows[rowIdx].Cells["CodeEntry"];
							dgItems.BeginEdit(true);
						}
					}
				});
				return;
			}

			// ─── معالجة تغيير الوحدة ────────────────────────────────────────────
			if (e.ColumnIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "UnitName")
			{
				if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
				string newUnit = dgItems.Rows[e.RowIndex].Cells["UnitName"].Value?.ToString() ?? "";
				this.BeginInvoke((MethodInvoker)delegate
				{
					if (e.RowIndex >= 0 && e.RowIndex < _items.Count)
						HandleUnitChange(dgItems.Rows[e.RowIndex], _items[e.RowIndex], newUnit);
				});
				return;
			}

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
					// ─── الأصناف الخدمية تتجاوز فحص المخزون ───
					if (!saleItemDTO.IsService && result > productStock)
					{
						MessageBox.Show($"❌ خطأ: الكمية المطلوبة ({result:N2}) أكبر من الكمية المتاحة في المخزن حالياً ({productStock:N2})!", "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dataGridViewRow.Cells["Quantity"].Value = saleItemDTO.Quantity.ToString("F2");
						return;
					}

					decimal delta = result - saleItemDTO.Quantity;
					decimal? manualPrice = saleItemDTO.UnitPrice;
					AddOrUpdateProduct(saleItemDTO.ProductID, delta, manualPrice, true);
					return;
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
				if (decimal.TryParse(dataGridViewRow.Cells[e.ColumnIndex].Value?.ToString(), out var resultPct) && resultPct >= 0m && resultPct <= 100m)
				{
					saleItemDTO.DiscountPct = resultPct;
					decimal gross = saleItemDTO.Quantity * saleItemDTO.UnitPrice;
					saleItemDTO.DiscountAmt = Math.Round(gross * resultPct / 100m, 2);
				}
				else
				{
					MessageBox.Show("من فضلك أدخل نسبة خصم صحيحة بين 0 و 100.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					dataGridViewRow.Cells[e.ColumnIndex].Value = saleItemDTO.DiscountPct.ToString("F2");
				}
			}
			else if (dgItems.Columns[e.ColumnIndex].Name == "DiscountAmt")
			{
				if (decimal.TryParse(dataGridViewRow.Cells[e.ColumnIndex].Value?.ToString(), out var resultAmt) && resultAmt >= 0m)
				{
					decimal gross = saleItemDTO.Quantity * saleItemDTO.UnitPrice;
					if (resultAmt > gross)
					{
						MessageBox.Show("قيمة الخصم لا يمكن أن تكون أكبر من إجمالي سعر الصنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dataGridViewRow.Cells[e.ColumnIndex].Value = saleItemDTO.DiscountAmt.ToString("F2");
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
					dataGridViewRow.Cells[e.ColumnIndex].Value = saleItemDTO.DiscountAmt.ToString("F2");
				}
			}

			if (dataGridViewRow.DataGridView == null) return;

			if (dgItems.Columns.Contains("DiscountPct"))
				dataGridViewRow.Cells["DiscountPct"].Value = saleItemDTO.DiscountPct.ToString("F2");
			if (dgItems.Columns.Contains("DiscountAmt"))
				dataGridViewRow.Cells["DiscountAmt"].Value = saleItemDTO.DiscountAmt.ToString("F2");
			if (dgItems.Columns.Contains("TotalPrice"))
				dataGridViewRow.Cells["TotalPrice"].Value = saleItemDTO.TotalPrice.ToString("F2");
			CalculateNet();
		}

		/// <summary>معالجة تغيير الوحدة في عمود UnitName — يُحدِّث Factor وسعر البيع وسعر الشراء</summary>
		private void HandleUnitChange(DataGridViewRow row, SaleItemDTO dto, string newUnit)
		{
			if (string.IsNullOrEmpty(newUnit)) return;
			ComboItem prod = GetProductComboItem(dto.ProductID);
			if (prod == null) return;

			dto.UnitName = newUnit;

			if (!string.IsNullOrEmpty(prod.Unit2Name) && newUnit == prod.Unit2Name)
			{
				// 1. الوحدة الوسطى
				dto.Factor = prod.Unit2Factor > 0 ? prod.Unit2Factor : 1m;
				if (prod.Unit2SalePrice > 0) dto.UnitPrice = prod.Unit2SalePrice;
				if (prod.Unit2PurchasePrice > 0) dto.PurchasePrice = prod.Unit2PurchasePrice;
			}
			else if (!string.IsNullOrEmpty(prod.Unit1Name) && newUnit == prod.Unit1Name)
			{
				// 2. الوحدة الصغرى (التجزئة)
				dto.Factor = 1m;
				if (prod.Unit1SalePrice > 0) dto.UnitPrice = prod.Unit1SalePrice;
				else dto.UnitPrice = prod.Price;
				if (prod.Unit1PurchasePrice > 0) dto.PurchasePrice = prod.Unit1PurchasePrice;
				else dto.PurchasePrice = prod.PurchasePrice;
			}
			else if (!string.IsNullOrEmpty(prod.BaseUnitName) && newUnit == prod.BaseUnitName)
			{
				// 3. الوحدة الكبرى (الأساسية)
				dto.Factor = (prod.Unit3Factor > 0 ? prod.Unit3Factor : 1m) * (prod.Unit2Factor > 0 ? prod.Unit2Factor : 1m);
				dto.UnitPrice = prod.Price;
				dto.PurchasePrice = prod.PurchasePrice;
			}
			else
			{
				// احتياطي
				dto.Factor = 1m;
				dto.UnitPrice = prod.Price;
				dto.PurchasePrice = prod.PurchasePrice;
			}

			// تحديث الجدول
			row.Cells["UnitPrice"].Value = dto.UnitPrice.ToString("F2");
			row.Cells["TotalPrice"].Value = dto.TotalPrice.ToString("F2");
			if (dgItems.Columns.Contains("PurchasePrice"))
				row.Cells["PurchasePrice"].Value = dto.PurchasePrice.ToString("F2");
			CalculateNet();
		}

		private void RefreshGrid()
		{
			_pendingRowIdx = -1; // إعادة تعيين السطر المعلق عند تحديث الجدول
			dgItems.Rows.Clear();
			foreach (SaleItemDTO item in _items)
			{
				decimal costTotal = item.PurchasePrice * item.Quantity;
				int rIndex = dgItems.Rows.Add(
					item.ProductCode, // CodeEntry - عرض الكود المحلي للصنف
					item.ProductName,
					item.PartNumber,
					item.CarModel,
					item.Brand,
					item.ShelfLocation,
					item.StockQty.ToString("F2"),
					null,              // UnitName - سيُعيَّن بالكود أدناه
					item.Quantity.ToString("F2"),
					item.UnitPrice.ToString("F2"),
					item.DiscountPct.ToString("F2"),
					item.DiscountAmt.ToString("F2"),
					item.TotalPrice.ToString("F2"),
					item.PurchasePrice.ToString("F2"),
					costTotal.ToString("F2")
				);
				// عمود الكود للسطور المضافة للقراءة فقط (ليس للتعديل)
				dgItems.Rows[rIndex].Cells["CodeEntry"].ReadOnly = true;

				// ─── تهيئة ComboBox الوحدة ──────────────────────────────────────────
				if (dgItems.Columns.Contains("UnitName") && dgItems.Columns["UnitName"] is DataGridViewComboBoxColumn unitCol)
				{
					var unitCell = (DataGridViewComboBoxCell)dgItems.Rows[rIndex].Cells["UnitName"];
					var unitList = new System.Collections.ArrayList();

					ComboItem prod = GetProductComboItem(item.ProductID);
					if (prod != null)
					{
						// 1. الوحدة الكبرى (الأساسية)
						if (!string.IsNullOrEmpty(prod.BaseUnitName))
						{
							unitList.Add(prod.BaseUnitName);
						}
						else
						{
							unitList.Add("وحدة");
						}

						// 2. الوحدة الوسطى (إن وُجدت)
						if (!string.IsNullOrEmpty(prod.Unit2Name))
						{
							unitList.Add(prod.Unit2Name);
						}

						// 3. الوحدة الصغرى (إن وُجدت وليست مكررة مع الكبرى)
						if (!string.IsNullOrEmpty(prod.Unit1Name) && prod.Unit1Name != prod.BaseUnitName)
						{
							unitList.Add(prod.Unit1Name);
						}
					}
					else
					{
						unitList.Add(!string.IsNullOrEmpty(item.UnitName) ? item.UnitName : "وحدة");
					}

					unitCell.DataSource = unitList;
					// تعيين القيمة المحفوظة (أو الافتراضية)
					string savedUnit = item.UnitName;
					if (!string.IsNullOrEmpty(savedUnit) && unitList.Contains(savedUnit))
						unitCell.Value = savedUnit;
					else if (unitList.Count > 0)
						unitCell.Value = unitList[0];
				}

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

		private void AddOrUpdateProduct(int productID, decimal qtyToAdd, decimal? manualPrice = null, bool deferRefresh = false)
		{
			ComboItem product = null;
			foreach (var item in cboProduct.Items)
			{
				if (item is ComboItem ci && ci.ID == productID)
				{
					product = ci;
					break;
				}
			}
			if (product == null && cboProduct.Tag is List<ComboItem> allItems)
			{
				foreach (var ci in allItems)
				{
					if (ci.ID == productID)
					{
						product = ci;
						break;
					}
				}
			}
			if (product == null) return;

			decimal stock = _stockCache.TryGetValue(productID, out var cached) ? cached : 0m;
			if (stock <= 0)
			{
				MessageBox.Show($"❌ عجز: الصنف '{product.Name}' ليس لديه رصيد كافٍ في المخزن حالياً (الرصيد الحالي: 0)!", "رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				if (deferRefresh) this.BeginInvoke((MethodInvoker)delegate { RefreshGrid(); });
				else RefreshGrid();
				return;
			}

			if (manualPrice.HasValue)
			{
				decimal targetPrice = manualPrice.Value;
				SaleItemDTO existingRow = null;
				foreach (var item in _items)
				{
					if (item.ProductID == productID && Math.Abs(item.UnitPrice - targetPrice) < 0.005m)
					{
						existingRow = item;
						break;
					}
				}

				decimal newQty = qtyToAdd;
				if (existingRow != null)
				{
					newQty = existingRow.Quantity + qtyToAdd;
				}

				decimal totalProductQtyInInvoice = qtyToAdd;
				foreach (var item in _items)
				{
					if (item.ProductID == productID)
					{
						totalProductQtyInInvoice += item.Quantity;
					}
				}
				if (existingRow != null)
				{
					totalProductQtyInInvoice -= existingRow.Quantity;
				}

				if (totalProductQtyInInvoice > stock)
				{
					MessageBox.Show($"❌ خطأ: إجمالي الكمية المطلوبة ({totalProductQtyInInvoice:N2}) أكبر من الكمية المتاحة في المخزن حالياً ({stock:N2})!", "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					if (deferRefresh) this.BeginInvoke((MethodInvoker)delegate { RefreshGrid(); });
					else RefreshGrid();
					return;
				}

				if (existingRow != null)
				{
					if (newQty <= 0)
					{
						_items.Remove(existingRow);
					}
					else
					{
						existingRow.Quantity = newQty;
					}
				}
				else
				{
					if (newQty > 0)
					{
						_items.Add(CreateSaleItemDTO(product, newQty, targetPrice, stock));
					}
				}
			}
			else
			{
				decimal existingQty = 0m;
				List<SaleItemDTO> existingRows = new List<SaleItemDTO>();
				foreach (var item in _items)
				{
					if (item.ProductID == productID)
					{
						existingQty += item.Quantity;
						existingRows.Add(item);
					}
				}

				decimal totalQty = existingQty + qtyToAdd;

				if (totalQty > stock)
				{
					MessageBox.Show($"❌ خطأ: الكمية المطلوبة ({totalQty:N2}) أكبر من الكمية المتاحة في المخزن حالياً ({stock:N2})!", "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					RefreshGrid();
					return;
				}

				decimal oldPrice = product.Price;
				decimal newPrice = product.PendingSalePrice;
				decimal threshold = product.PendingQtyThreshold;

				bool hasPendingPrice = newPrice > 0 && threshold > 0;

				foreach (var row in existingRows)
				{
					_items.Remove(row);
				}

				if (hasPendingPrice)
				{
					decimal oldQtyAvailable = Math.Max(0m, Math.Min(stock, threshold));
					decimal qtyOld = Math.Min(totalQty, oldQtyAvailable);
					decimal qtyNew = Math.Max(0m, totalQty - oldQtyAvailable);

					if (qtyOld > 0)
					{
						_items.Add(CreateSaleItemDTO(product, qtyOld, oldPrice, stock));
					}
					if (qtyNew > 0)
					{
						_items.Add(CreateSaleItemDTO(product, qtyNew, newPrice, stock));
					}
				}
				else
				{
					if (totalQty > 0)
					{
						_items.Add(CreateSaleItemDTO(product, totalQty, oldPrice, stock));
					}
				}
			}

			if (deferRefresh) this.BeginInvoke((MethodInvoker)delegate { RefreshGrid(); });
			else RefreshGrid();
		}

		private SaleItemDTO CreateSaleItemDTO(ComboItem product, decimal qty, decimal price, decimal stock)
		{
			// الوحدة الافتراضية = الوحدة الكبرى (Unit1Name إن وجدت وإلا Unit الأساسية)
			string defaultUnit = !string.IsNullOrEmpty(product.Unit1Name) ? product.Unit1Name
				               : !string.IsNullOrEmpty(product.BaseUnitName) ? product.BaseUnitName
				               : null;
			decimal defaultFactor = 1m; // الوحدة الكبرى دائما factor = 1 (هي الوحدة الأساسية للمخزون)

			return new SaleItemDTO
			{
				ProductID = product.ID,
				ProductName = product.Name,
				Quantity = qty,
				UnitPrice = price,
				StockQty = stock,
				MinStockLimit = product.MinStockLimit,
				PurchasePrice = product.PurchasePrice,
				PartNumber = product.PartNumber,
				CarModel = product.CarModel,
				Brand = product.Brand,
				ShelfLocation = product.ShelfLocation,
				ProductCode = product.ProductCode,
				IsService = product.IsService,
				UnitName = defaultUnit,
				Factor = defaultFactor
			};
		}

		/// <summary>
		/// يجلب بيانات الوحدات المتعددة للصنف من ComboItem (أو يستعلم إذا لم يكن في الـ cache)
		/// </summary>
		private ComboItem GetProductComboItem(int productID)
		{
			// بحث في العناصر المرئية أولاً
			foreach (var obj in cboProduct.Items)
				if (obj is ComboItem ci && ci.ID == productID) return ci;
			// بحث في cache الـ Tag
			if (cboProduct.Tag is List<ComboItem> all)
				foreach (var ci in all)
					if (ci.ID == productID) return ci;
			return null;
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
				         COALESCE(s.CratesOut, 0) AS CratesOut,
				         COALESCE(s.CratesIn, 0) AS CratesIn,
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

			// الأقفاص
			nudCratesOut.Value = row["CratesOut"] != DBNull.Value ? Convert.ToInt32(row["CratesOut"]) : 0;
			nudCratesIn.Value = row["CratesIn"] != DBNull.Value ? Convert.ToInt32(row["CratesIn"]) : 0;

			// الخصم
			decimal discPct = row.Table.Columns.Contains("DiscountPct") && row["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(row["DiscountPct"]) : 0m;
			decimal discAmt = row.Table.Columns.Contains("DiscountAmount") && row["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmount"]) : 0m;
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
					DiscountPct = iRow.Table.Columns.Contains("DiscountPct") && iRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(iRow["DiscountPct"]) : 0m,
					DiscountAmt = iRow.Table.Columns.Contains("DiscountAmt") && iRow["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(iRow["DiscountAmt"]) : 0m,
					PriceTier   = iRow["PriceTier"].ToString(),
					StockQty    = stock,
					PurchasePrice = iRow["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(iRow["PurchasePrice"]) : 0m,
					PartNumber  = iRow["PartNumber"]?.ToString() ?? "",
					CarModel    = iRow["CarModel"]?.ToString() ?? "",
					Brand       = iRow["Brand"]?.ToString() ?? "",
					ShelfLocation = iRow["ShelfLocation"]?.ToString() ?? "",
					UnitName    = iRow.Table.Columns.Contains("UnitName") && iRow["UnitName"] != DBNull.Value ? iRow["UnitName"].ToString() : null,
					Factor      = iRow.Table.Columns.Contains("Factor")   && iRow["Factor"]   != DBNull.Value ? Convert.ToDecimal(iRow["Factor"]) : 1m
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

			if (_editSaleID > 0 && _invoiceType == "Installment")
			{
				MessageBox.Show("❌ لا يمكن تعديل فواتير التقسيط من شاشة المبيعات مباشرة. يرجى تعديلها أو إدارتها من شاشة عقود التقسيط.", "تعديل غير مسموح", MessageBoxButtons.OK, MessageBoxIcon.Stop);
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

			int saleType = _invoiceType == "Credit" ? 0 : _invoiceType == "DriverLoad" ? 1 : _invoiceType == "Installment" ? 3 : 2;
			int? clientID = null;
			int? driverID = null;
			if (_invoiceType == "Credit" || _invoiceType == "Cash" || _invoiceType == "Installment")
			{
				if (!(cboClient.SelectedItem is ComboItem comboItem) || comboItem.ID == 0)
				{
					if (_invoiceType == "Cash")
					{
						clientID = null;
					}
					else
					{
						MessageBox.Show("اختر العميل");
						return;
					}
				}
				else
				{
					clientID = comboItem.ID;
				}
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
				if (cboInvoiceDiscountType != null && cboInvoiceDiscountType.SelectedIndex == 0) // %
				{
					discountPct = discount;
					discountAmount = Math.Round(gross * (discount / 100m), 2);
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
			decimal paidAmount = net;
			if (!isDraft && _invoiceType == "Cash")
			{
				decimal? defaultPaid = null;
				if (_editSaleID > 0)
				{
					var existingPaidObj = DbHelper.Scalar("SELECT CashPaid FROM Sales WHERE SaleID=@id", DbHelper.P("@id", _editSaleID));
					if (existingPaidObj != null && existingPaidObj != DBNull.Value)
					{
						defaultPaid = Convert.ToDecimal(existingPaidObj);
					}
				}
				using (var frm = new FrmQuickPayment(net, clientID.HasValue, defaultPaid))
				{
					if (frm.ShowDialog() != DialogResult.OK) return;
					paidAmount = frm.PaidAmount;
				}
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
					int? safeAccountID = null;
					if (cboSafeAccount.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
					{
						safeAccountID = safeItem.ID;
					}
					bool updated = SaleDAL.UpdateSale(_editSaleID, saleType, clientID, driverID,
						net, txtNotes.Text, _items, discountAmount, discountPct,
						isDraft: false, warehouseID: GetSelectedWarehouseID(), priceTier: priceTier,
						loadedLastModified: _loadedLastModified, safeAccountID: safeAccountID, cashPaid: paidAmount,
						cratesOut: (int)nudCratesOut.Value, cratesIn: (int)nudCratesIn.Value);
					if (updated)
					{
						_isDirty = false;
						DialogResult pr = MessageBox.Show(
							$"✅ تم تعديل الفاتورة رقم [{_editSaleID}] بنجاح!\n\nهل تريد طباعة الفاتورة المعدّلة؟",
							"تعديل ناجح", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (pr == DialogResult.Yes) new FrmPrintSale(_editSaleID, showPreview: false);
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
				decimal downPayment = 0m;
				int installmentCount = 1;
				string installmentPeriod = "Monthly";
				DateTime? startDate = null;
				List<InstallmentScheduleDTO> schedule = null;

				if (_invoiceType == "Installment" && !isDraft)
				{
					using (var frmConfig = new FrmConfigureInstallment(net))
					{
						if (frmConfig.ShowDialog() != DialogResult.OK)
						{
							return;
						}
						net = frmConfig.InstallmentPrice;
						downPayment = frmConfig.DownPayment;
						installmentCount = frmConfig.InstallmentCount;
						installmentPeriod = frmConfig.InstallmentPeriod;
						startDate = frmConfig.StartDate;
						schedule = frmConfig.Schedule;
					}
				}

				int? safeAccountID = null;
				if (cboSafeAccount.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
				{
					safeAccountID = safeItem.ID;
				}
				int num3 = SaleDAL.SaveSale(saleType, clientID, driverID, net,
					txtNotes.Text, _items, discountAmount, discountPct, isDraft,
					warehouseID: GetSelectedWarehouseID(), priceTier: priceTier,
					downPayment: downPayment, installmentCount: installmentCount,
					installmentPeriod: installmentPeriod, startDate: startDate,
					schedule: schedule, safeAccountID: safeAccountID, cashPaid: paidAmount,
					cratesOut: (int)nudCratesOut.Value, cratesIn: (int)nudCratesIn.Value);
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
						if (printResult == DialogResult.Yes) new FrmPrintSale(num3, showPreview: false);
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

				decimal discAmt = row.Row.Table.Columns.Contains("DiscountAmount") && row["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmount"]) : 0m;
				decimal discPctVal = row.Row.Table.Columns.Contains("DiscountPct") && row["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(row["DiscountPct"]) : 0m;
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
						DiscountPct = iRow.Table.Columns.Contains("DiscountPct") && iRow["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(iRow["DiscountPct"]) : 0m,
						DiscountAmt = iRow.Table.Columns.Contains("DiscountAmt") && iRow["DiscountAmt"] != DBNull.Value ? Convert.ToDecimal(iRow["DiscountAmt"]) : 0m,
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
				ScaleService.Instance.WeightChanged -= ScaleService_WeightChanged;
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
			int printID = 0;
			if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
			{
				var clientLastObj = DbHelper.Scalar("SELECT TOP 1 SaleID FROM Sales WHERE ClientID = @cid ORDER BY SaleDate DESC, SaleID DESC", DbHelper.P("@cid", ci.ID));
				if (clientLastObj != null && clientLastObj != DBNull.Value)
				{
					printID = Convert.ToInt32(clientLastObj);
				}
			}

			if (printID == 0)
			{
				printID = _lastSaleID;
			}

			if (printID == 0)
			{
				var lastObj = DbHelper.Scalar("SELECT COALESCE(MAX(SaleID), 0) FROM Sales");
				if (lastObj != null && lastObj != DBNull.Value)
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
			itemReceipt.Click += (s2, e2) => new FrmPrintSale(printID, "Receipt", showPreview: false);
            
			var itemA4 = new ToolStripMenuItem("📄 طباعة فاتورة ورق (A4/A5)");
			itemA4.Click += (s2, e2) => new FrmPrintSale(printID, "A4", showPreview: false);

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

		private void BtnPreview_Click(object sender, EventArgs e)
		{
			int printID = 0;
			if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
			{
				var clientLastObj = DbHelper.Scalar("SELECT TOP 1 SaleID FROM Sales WHERE ClientID = @cid ORDER BY SaleDate DESC, SaleID DESC", DbHelper.P("@cid", ci.ID));
				if (clientLastObj != null && clientLastObj != DBNull.Value)
				{
					printID = Convert.ToInt32(clientLastObj);
				}
			}

			if (printID == 0)
			{
				printID = _lastSaleID;
			}

			if (printID == 0)
			{
				var lastObj = DbHelper.Scalar("SELECT COALESCE(MAX(SaleID), 0) FROM Sales");
				if (lastObj != null && lastObj != DBNull.Value)
				{
					printID = Convert.ToInt32(lastObj);
				}
			}

			if (printID == 0)
			{
				MessageBox.Show("لا توجد فواتير مسجلة لمعاينتها!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var menu = new ContextMenuStrip();
			var itemReceipt = new ToolStripMenuItem("🧾 معاينة ريسيت حراري (Receipt)");
			itemReceipt.Click += (s2, e2) => new FrmPrintSale(printID, "Receipt", showPreview: true);
            
			var itemA4 = new ToolStripMenuItem("📄 معاينة فاتورة ورق (A4/A5)");
			itemA4.Click += (s2, e2) => new FrmPrintSale(printID, "A4", showPreview: true);

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
				MessageBox.Show("❌ خطأ: يجب اختيار عميل مسجل أولاً لتسجيل عملية التوريد لحسابه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			Form frm = new Form
			{
				Width = 400,
				Height = 310,
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
			Label label3 = new Label
			{
				Left = 20,
				Top = 140,
				Text = "حساب التوريد:",
				AutoSize = true,
				ForeColor = Theme.TextMain
			};
			ComboBox cboSafe = new ComboBox
			{
				Left = 20,
				Top = 165,
				Width = 340,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat
			};
			// Load safes
			try
			{
				DataTable safes = AccountDAL.GetActiveSafeAccounts();
				cboSafe.Items.Clear();

				// Get allowed safes from Session
				System.Collections.Generic.HashSet<int> allowedSafes = null;
				if (Session.Role != "Admin")
				{
					allowedSafes = new System.Collections.Generic.HashSet<int>();
					if (!string.IsNullOrEmpty(Session.AllowedSafeIDs))
					{
						foreach (var part in Session.AllowedSafeIDs.Split(','))
						{
							if (int.TryParse(part, out int id))
								allowedSafes.Add(id);
						}
					}
				}

				foreach (DataRow row in safes.Rows)
				{
					int accID = Convert.ToInt32(row["AccountID"]);
					if (allowedSafes != null && !allowedSafes.Contains(accID))
					{
						continue; // Filter out if not allowed
					}

					cboSafe.Items.Add(new ComboItem(
						accID,
						row["AccountName"].ToString()
					));
				}
				cboSafe.DisplayMember = "Text";
				if (cboSafe.Items.Count > 0) cboSafe.SelectedIndex = 0;
			}
			catch { }

			Button button = Theme.MakeButton("✅ حفظ", 120, 215, 100, 35, Theme.Accent);
			button.Click += delegate
			{
				frm.DialogResult = DialogResult.OK;
				frm.Close();
			};
			frm.Controls.AddRange(new Control[7] { label, textBox, label2, textBox2, label3, cboSafe, button });
			if (frm.ShowDialog() == DialogResult.OK && decimal.TryParse(textBox.Text, out var result) && result > 0m)
			{
				int? targetSafeID = null;
				if (cboSafe.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
				{
					targetSafeID = safeItem.ID;
				}
				AccountDAL.SaveCashReceipt(comboItem.ID, result, dtpDate.Value, textBox2.Text, targetSafeID);
				UpdateClientBalanceLabel(comboItem.ID);
				MessageBox.Show("✅ تم تسجيل التوريد في الخزنة بنجاح!", "تم", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}

		private void BtnWhatsApp_Click(object sender, EventArgs e)
		{
			int saleID = 0;
			if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
			{
				var clientLastObj = DbHelper.Scalar("SELECT TOP 1 SaleID FROM Sales WHERE ClientID = @cid ORDER BY SaleDate DESC, SaleID DESC", DbHelper.P("@cid", ci.ID));
				if (clientLastObj != null && clientLastObj != DBNull.Value)
				{
					saleID = Convert.ToInt32(clientLastObj);
				}
			}

			if (saleID == 0)
			{
				saleID = _lastSaleID;
			}

			if (saleID == 0)
			{
				var lastObj = DbHelper.Scalar("SELECT COALESCE(MAX(SaleID), 0) FROM Sales");
				if (lastObj != null && lastObj != DBNull.Value) saleID = Convert.ToInt32(lastObj);
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
				       s.CashPaid,
				       COALESCE(s.CratesOut, 0) AS CratesOut,
				       COALESCE(s.CratesIn, 0) AS CratesIn,
				       COALESCE(c.ClientName, N'عميل نقدي') AS ClientName,
				       COALESCE(c.Phone, '') AS ClientPhone
				FROM Sales s
				LEFT JOIN Clients c ON s.ClientID = c.ClientID
				WHERE s.SaleID = @id", DbHelper.P("@id", saleID));

			if (dt.Rows.Count == 0) { MessageBox.Show("لم يتم العثور على الفاتورة!"); return; }
			var saleRow = dt.Rows[0];
			string phone = saleRow["ClientPhone"].ToString().Trim();

			if (string.IsNullOrWhiteSpace(phone))
			{
				using (var frmInput = new Form())
				{
					frmInput.Text = "إدخال رقم الهاتف";
					frmInput.Size = new Size(350, 150);
					frmInput.StartPosition = FormStartPosition.CenterParent;
					frmInput.FormBorderStyle = FormBorderStyle.FixedDialog;
					frmInput.MaximizeBox = false;
					frmInput.MinimizeBox = false;
					frmInput.RightToLeft = RightToLeft.Yes;
					frmInput.RightToLeftLayout = true;
					frmInput.BackColor = Theme.BgMain;
					frmInput.Font = Theme.FontMain;

					var lbl = new Label { Text = "أدخل رقم هاتف العميل للإرسال:", Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.TextMain };
					var txt = new TextBox { Location = new Point(20, 45), Width = 290, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
					var btnOk = Theme.MakeButton("✅ موافق", 190, 80, 100, 30, Theme.Success);
					btnOk.Click += (s, ev) => { phone = txt.Text.Trim(); frmInput.DialogResult = DialogResult.OK; frmInput.Close(); };
					
					frmInput.Controls.AddRange(new Control[] { lbl, txt, btnOk });
					frmInput.AcceptButton = btnOk;
					if (frmInput.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(phone))
					{
						return;
					}
				}
			}

			// جلب أصناف الفاتورة
			var items = SaleDAL.GetItems(saleID);

			// جلب البيانات المالية للعميل
			decimal prevBalance = 0m;
			decimal lastPaymentAmt = 0m;
			DateTime lastPaymentDate = DateTime.MinValue;
			decimal todayPayments = 0m;
			decimal todayReturns = 0m;
			decimal actualCurrentBalance = 0m; // الرصيد الفعلي الحالي من قاعدة البيانات

			if (saleRow["ClientID"] != DBNull.Value)
			{
				int clientID = Convert.ToInt32(saleRow["ClientID"]);
				DateTime saleDate = Convert.ToDateTime(saleRow["SaleDate"]);

				// الرصيد السابق قبل هذه الفاتورة
				prevBalance = ClientDAL.GetPreviousBalanceBeforeSale(clientID, saleID);

				// الرصيد الفعلي الحالي (يشمل كل الحركات بما فيها التوريدات)
				actualCurrentBalance = ClientDAL.GetClientBalance(clientID);

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

				// مجموع المدفوعات والمرتجع في تاريخ الفاتورة
				int saleTransID = 0;
				var dtTrans = DbHelper.Query(@"
					SELECT TOP 1 TransID 
					FROM ClientTransactions 
					WHERE ClientID = @cid AND TransType = 'Sale' AND RefID = @sid 
					ORDER BY TransID DESC",
					DbHelper.P("@cid", clientID), DbHelper.P("@sid", saleID));
				if (dtTrans.Rows.Count > 0)
				{
					saleTransID = Convert.ToInt32(dtTrans.Rows[0]["TransID"]);
				}

				var todayPayDt = DbHelper.Query(@"
					SELECT 
						COALESCE(SUM(CASE WHEN TransType = 'Payment' THEN Credit ELSE 0 END), 0) AS TotalPayment,
						COALESCE(SUM(CASE WHEN TransType = 'Return' THEN Credit ELSE 0 END), 0) AS TotalReturn
					FROM ClientTransactions
					WHERE ClientID=@id 
					  AND CAST(TransDate AS DATE) = CAST(@saleDate AS DATE)
					  AND TransID >= @saleTransID
					  AND NOT (RefID = @sid AND TransType = 'Payment')",
					DbHelper.P("@id", clientID), 
					DbHelper.P("@saleDate", saleDate),
					DbHelper.P("@saleTransID", saleTransID),
					DbHelper.P("@sid", saleID));
				if (todayPayDt.Rows.Count > 0)
				{
					todayPayments = Convert.ToDecimal(todayPayDt.Rows[0]["TotalPayment"]);
					todayReturns = Convert.ToDecimal(todayPayDt.Rows[0]["TotalReturn"]);
				}
			}

			// عرض خيارات الإرسال
			string choice = "";
			using (var frm = new Form())
			{
				frm.Text = "إرسال الفاتورة عبر واتساب";
				frm.Size = new Size(380, 160);
				frm.StartPosition = FormStartPosition.CenterParent;
				frm.FormBorderStyle = FormBorderStyle.FixedDialog;
				frm.MaximizeBox = false;
				frm.MinimizeBox = false;
				frm.RightToLeft = RightToLeft.Yes;
				frm.RightToLeftLayout = true;
				frm.BackColor = Theme.BgMain;
				frm.Font = Theme.FontMain;

				var lbl = new Label { Text = "اختر طريقة إرسال الفاتورة للعميل:", Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.TextMain };
				frm.Controls.Add(lbl);

				var btnText = Theme.MakeButton("📋 إرسال كرسالة نصية", 20, 60, 150, 40, Theme.Primary);
				var btnImage = Theme.MakeButton("🖼️ إرسال كصورة (تصميم)", 190, 60, 150, 40, Theme.Success);

				btnText.Click += (s, ev) => { choice = "text"; frm.Close(); };
				btnImage.Click += (s, ev) => { choice = "image"; frm.Close(); };

				frm.Controls.Add(btnText);
				frm.Controls.Add(btnImage);

				frm.ShowDialog(this);
			}

			if (choice == "text")
			{
				// بناء نص الرسالة بالتنسيق المطلوب من المستخدم
				var sb = new System.Text.StringBuilder();
				sb.AppendLine($"📋 فاتورة مبيعات رقم {saleRow["SaleCode"]}");
				sb.AppendLine($"🏢 {AppConfig.CompanyName}");
				sb.AppendLine($"👤 العميل: {saleRow["ClientName"]}");
				sb.AppendLine($"📅 التاريخ: {Convert.ToDateTime(saleRow["SaleDate"]):dd/MM/yyyy}");
				string typeLabel = saleRow["SaleType"].ToString() == "Credit" ? "آجل" : saleRow["SaleType"].ToString() == "Cash" ? "نقدي" : "تحميل مندوب";
				sb.AppendLine($"💳 نوع البيع: {typeLabel}");
				sb.AppendLine("━━━━━━━━━━━━━━━━");

				if (items != null)
				{
					foreach (DataRow r in items.Rows)
					{
						string name  = r["ProductName"].ToString();
						decimal qty   = Convert.ToDecimal(r["Quantity"]);
						decimal price = Convert.ToDecimal(r["UnitPrice"]);
						decimal tot   = Convert.ToDecimal(r["TotalPrice"]);
						sb.AppendLine($"🐥 {name}");
						sb.AppendLine($"▪ الكمية : {qty:0.##}");
						sb.AppendLine($"▪ السعر : {price:N2} ج");
						sb.AppendLine($"▪ الإجمالي : {tot:N2} ج");
						sb.AppendLine("━━━━━━━━━━━━━━━━");
					}
				}

				decimal totalAmount = Convert.ToDecimal(saleRow["TotalAmount"]);
				sb.AppendLine("💰 صافي الفاتورة");
				sb.AppendLine($"{totalAmount:N2} ج.م");
				sb.AppendLine("━━━━━━━━━━━━━━━━");

				if (AppConfig.EnableCratesTracking)
				{
					int cratesOutValMsg = saleRow["CratesOut"] != DBNull.Value ? Convert.ToInt32(saleRow["CratesOut"]) : 0;
					int cratesInValMsg = saleRow["CratesIn"] != DBNull.Value ? Convert.ToInt32(saleRow["CratesIn"]) : 0;
					if (cratesOutValMsg > 0 || cratesInValMsg > 0)
					{
						sb.AppendLine("📦 حركة الأقفاص");
						if (cratesOutValMsg > 0) sb.AppendLine($"▪ أقفاص صادرة : {cratesOutValMsg} قفص");
						if (cratesInValMsg > 0) sb.AppendLine($"▪ أقفاص واردة : {cratesInValMsg} قفص");
						sb.AppendLine("━━━━━━━━━━━━━━━━");
					}
				}

				bool isCredit = saleRow["SaleType"].ToString() == "Credit";
				decimal cashPaid = saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(saleRow["CashPaid"]) : totalAmount;
				decimal remainingFromInvoice = isCredit ? totalAmount : (totalAmount - cashPaid);

				if (saleRow["SaleType"].ToString() == "Cash")
				{
					sb.AppendLine($"💵 المدفوع نقداً : {cashPaid:N2} ج.م");
					if (remainingFromInvoice > 0)
					{
						sb.AppendLine($"⚠️ المتبقي (آجل) : {remainingFromInvoice:N2} ج.م");
						sb.AppendLine("📝 (سيتم إضافة المتبقي على حساب العميل)");
					}
					else if (remainingFromInvoice < 0)
					{
						sb.AppendLine($"➕ الزيادة : {-remainingFromInvoice:N2} ج.م");
						sb.AppendLine("📝 (سيتم خصم الزيادة من حساب العميل)");
					}
					else
					{
						sb.AppendLine("✅ (تم سداد الفاتورة بالكامل)");
					}
					sb.AppendLine("━━━━━━━━━━━━━━━━");
				}

				if (saleRow["ClientID"] != DBNull.Value)
				{
					int clientIDVal = Convert.ToInt32(saleRow["ClientID"]);
					decimal totalDue = prevBalance + (isCredit ? totalAmount : remainingFromInvoice);
					// استخدام الرصيد الفعلي من قاعدة البيانات بدلاً من الحساب اليدوي
					// لضمان احتساب التوريدات التي تمت بعد تاريخ الفاتورة
					decimal currentDue = actualCurrentBalance;

					sb.AppendLine("📊 الوضع المالي");
					sb.AppendLine($"الرصيد السابق : {prevBalance:N2} ج.م");
					if (isCredit)
					{
						sb.AppendLine($"+ الفاتورة الحالية : {totalAmount:N2} ج.م");
						sb.AppendLine($"= إجمالي المستحق : {totalDue:N2} ج.م");
					}
					else
					{
						if (remainingFromInvoice > 0)
						{
							sb.AppendLine($"+ متبقي الفاتورة الحالية : {remainingFromInvoice:N2} ج.م");
							sb.AppendLine($"= إجمالي المستحق : {totalDue:N2} ج.م");
						}
						else if (remainingFromInvoice < 0)
						{
							sb.AppendLine($"- زيادة الفاتورة الحالية : {-remainingFromInvoice:N2} ج.م");
							sb.AppendLine($"= إجمالي المستحق : {totalDue:N2} ج.م");
						}
					}
					sb.AppendLine($"- مسدد اليوم : {todayPayments:N2} ج.م");
					if (todayReturns > 0)
					{
						sb.AppendLine($"- مرتجع اليوم : {todayReturns:N2} ج.م");
					}
					if (lastPaymentAmt > 0)
					{
						sb.AppendLine($"📝 آخر توريد سابق : {lastPaymentAmt:N2} ج.م ({lastPaymentDate:dd/MM/yyyy})");
					}
					else
					{
						sb.AppendLine("📝 آخر توريد سابق : لا يوجد");
					}
					int currentCratesDueMsg = ClientDAL.GetClientCratesBalance(clientIDVal);
					sb.AppendLine($"أقفاص العميل الحالية : {currentCratesDueMsg} قفص");
					sb.AppendLine("━━━━━━━━━━━━━━━━");
					sb.AppendLine($"{currentDue:N2} ج.م");
					sb.AppendLine("🔴 الرصيد الحالي المستحق");
					sb.AppendLine("━━━━━━━━━━━━━━━━");
				}

				sb.AppendLine("🙏 شكراً لتعاملكم معنا");

				SendWhatsApp(phone, sb.ToString());
			}
			else if (choice == "image")
			{
				try
				{
					using (Bitmap bmp = DrawInvoiceImage(saleRow, items, prevBalance, lastPaymentAmt, lastPaymentDate, todayPayments, todayReturns, actualCurrentBalance))
					{
						Clipboard.SetImage(bmp);
					}

					MessageBox.Show("✅ تم تصميم الفاتورة ونسخ الصورة للحافظة بنجاح!\nسيتم فتح واتساب العميل الآن، فقط اضغط Ctrl+V في مربع الكتابة للصق وإرسال الصورة.",
						"تم النسخ للحافظة", MessageBoxButtons.OK, MessageBoxIcon.Information,
						MessageBoxDefaultButton.Button1,
						MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

					// فتح محادثة الواتساب
					string clean = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
					if (clean.StartsWith("0")) clean = "20" + clean.Substring(1);
					
					string appUrl = $"whatsapp://send?phone={clean}";
					try
					{
						System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(appUrl) { UseShellExecute = true });
					}
					catch
					{
						string waUrl = $"https://wa.me/{clean}";
						try
						{
							System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(waUrl) { UseShellExecute = true });
						}
						catch
						{
							try
							{
								System.Diagnostics.Process.Start("explorer.exe", $"\"{waUrl}\"");
							}
							catch
							{
								string webUrl = $"https://web.whatsapp.com/send?phone={clean}";
								System.Diagnostics.Process.Start("explorer.exe", $"\"{webUrl}\"");
							}
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("فشل تصميم الفاتورة أو نسخها للحافظة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
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
						
					encoded = Uri.EscapeDataString("📋 تفاصيل فاتورة المبيعات (تم نسخ التفاصيل للحافظة، يرجى اللصق وإرسال)");
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

		private Bitmap DrawInvoiceImage(DataRow saleRow, DataTable items, decimal prevBalance, decimal lastPaymentAmt, DateTime lastPaymentDate, decimal todayPayments, decimal todayReturns, decimal actualCurrentBalance = 0m)
		{
			int itemCount = items != null ? items.Rows.Count : 0;
			bool showFinancial = saleRow["ClientID"] != DBNull.Value;
			decimal netVal = Convert.ToDecimal(saleRow["TotalAmount"]);

			// حساب الارتفاع المطلوب ديناميكياً
			int headerH = 110;
			int metaH = 80;
			int tableHeaderH = 35;
			int rowH = 30;
			int netH = 40;
			
			int financialLines = 0;
			if (showFinancial)
			{
				financialLines = 2 + 1; // "الوضع المالي للحساب" header + "الرصيد السابق" + "الرصيد الحالي المستحق"
				bool isCredit = saleRow["SaleType"].ToString() == "Credit";
				decimal cashPaid = saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(saleRow["CashPaid"]) : netVal;
				decimal remainingFromInvoice = isCredit ? netVal : (netVal - cashPaid);

				if (isCredit)
				{
					financialLines += 2; // "الفاتورة الحالية", "إجمالي المستحق"
				}
				else
				{
					financialLines += 1; // "المدفوع نقداً"
					if (remainingFromInvoice != 0)
					{
						financialLines += 2; // "متبقي الفاتورة"/"زيادة الفاتورة", "إجمالي المستحق"
					}
				}
				financialLines += 1; // "مسدد اليوم"
				if (todayReturns > 0) financialLines += 1; // "مرتجع اليوم"

				if (AppConfig.EnableCratesTracking)
				{
					int cratesOutVal = saleRow.Table.Columns.Contains("CratesOut") && saleRow["CratesOut"] != DBNull.Value ? Convert.ToInt32(saleRow["CratesOut"]) : 0;
					int cratesInVal = saleRow.Table.Columns.Contains("CratesIn") && saleRow["CratesIn"] != DBNull.Value ? Convert.ToInt32(saleRow["CratesIn"]) : 0;
					if (cratesOutVal > 0) financialLines += 1;
					if (cratesInVal > 0) financialLines += 1;
					financialLines += 1; // "رصيد الأقفاص المستحق"
				}
			}
			int financialH = showFinancial ? (30 + financialLines * 28 + 25) : 0;
			int footerH = 55;
			
			int totalH = headerH + metaH + tableHeaderH + (itemCount * rowH) + netH + 15 + financialH + footerH + 50;
			int w = 600;

			var bmp = new Bitmap(w, totalH);
			using (var g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.White);
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

				var cNavy = Color.FromArgb(0, 51, 153);
				using (var pNavyThick = new Pen(cNavy, 3))
				using (var pNavyThin = new Pen(cNavy, 1))
				using (var bNavy = new SolidBrush(cNavy))
				using (var bRed = new SolidBrush(Color.FromArgb(200, 30, 30)))
				{
					// رسم الحدود
					g.DrawRectangle(pNavyThick, 4, 4, w - 8, totalH - 8);
					g.DrawRectangle(pNavyThin, 9, 9, w - 18, totalH - 18);

					float y = 20;

					// الخطوط
					var fTitle = new Font("Arial", 20f, FontStyle.Bold);
					var fComp = new Font("Arial", 14f, FontStyle.Bold);
					var fBold = new Font("Arial", 9.5f, FontStyle.Bold);
					var fNormal = new Font("Arial", 9f);

					var center = new StringFormat { Alignment = StringAlignment.Center };
					var rtlNear = new StringFormat { Alignment = StringAlignment.Near, FormatFlags = StringFormatFlags.DirectionRightToLeft };
					var rtlCenter = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

					g.DrawString("فاتورة مبيعات", fTitle, bNavy, new RectangleF(0, y, w, 32), center);
					y += 35;

					g.DrawString(AppConfig.CompanyName, fComp, bNavy, new RectangleF(0, y, w, 28), center);
					
					// رسم دجاجتين كشعار
					DrawChickenSilhouette(g, 35, y - 25, 40);
					DrawChickenSilhouette(g, w - 75, y - 25, 40);
					y += 40;

					// مربع البيانات الفوقية
					g.DrawRectangle(pNavyThin, 20, y, w - 40, 75);
					g.DrawLine(pNavyThin, w / 2, y, w / 2, y + 75);

					float boxY = y + 10;
					// اليمين
					g.DrawString($"رقم الفاتورة:  {saleRow["SaleCode"]}", fBold, Brushes.Black, new RectangleF(w / 2 + 10, boxY, w / 2 - 30, 22), rtlNear);
					g.DrawString($"التاريخ:  {Convert.ToDateTime(saleRow["SaleDate"]):dd/MM/yyyy}", fNormal, Brushes.Black, new RectangleF(w / 2 + 10, boxY + 26, w / 2 - 30, 22), rtlNear);

					// اليسار
					g.DrawString($"العميل:  {saleRow["ClientName"]}", fBold, Brushes.Black, new RectangleF(25, boxY, w / 2 - 35, 22), rtlNear);
					string typeLabel = saleRow["SaleType"].ToString() == "Credit" ? "آجل" : saleRow["SaleType"].ToString() == "Cash" ? "نقدي" : "تحميل مندوب";
					g.DrawString($"النوع:  {typeLabel}", fNormal, Brushes.Black, new RectangleF(25, boxY + 26, w / 2 - 35, 22), rtlNear);
					
					y += 90;

					// ترويسة جدول الأصناف
					g.FillRectangle(bNavy, 20, y, w - 40, tableHeaderH);
					
					g.DrawString("النوع", fBold, Brushes.White, new RectangleF(400, y + 8, 180, tableHeaderH), rtlCenter);
					g.DrawString("الكمية", fBold, Brushes.White, new RectangleF(290, y + 8, 110, tableHeaderH), rtlCenter);
					g.DrawString("السعر", fBold, Brushes.White, new RectangleF(180, y + 8, 110, tableHeaderH), rtlCenter);
					g.DrawString("الإجمالي", fBold, Brushes.White, new RectangleF(20, y + 8, 160, tableHeaderH), rtlCenter);
					
					y += tableHeaderH;

					// سطور الأصناف
					if (items != null)
					{
						foreach (DataRow r in items.Rows)
						{
							g.DrawRectangle(pNavyThin, 20, y, w - 40, rowH);
							g.DrawLine(pNavyThin, 400, y, 400, y + rowH);
							g.DrawLine(pNavyThin, 290, y, 290, y + rowH);
							g.DrawLine(pNavyThin, 180, y, 180, y + rowH);

							string prodName = r["ProductName"].ToString();
							decimal qty = Convert.ToDecimal(r["Quantity"]);
							decimal price = Convert.ToDecimal(r["UnitPrice"]);
							decimal tot = Convert.ToDecimal(r["TotalPrice"]);

							g.DrawString(prodName, fBold, Brushes.Black, new RectangleF(405, y + 6, 170, rowH), rtlNear);
							g.DrawString(qty.ToString("0.##"), fNormal, Brushes.Black, new RectangleF(290, y + 6, 110, rowH), rtlCenter);
							g.DrawString(price.ToString("N2"), fNormal, Brushes.Black, new RectangleF(180, y + 6, 110, rowH), rtlCenter);
							g.DrawString(tot.ToString("N2"), fBold, Brushes.Black, new RectangleF(20, y + 6, 155, rowH), rtlCenter);

							y += rowH;
						}
					}

					// إجمالي الفاتورة
					g.FillRectangle(bNavy, 320, y, 260, netH);
					g.DrawString("صافي الفاتورة", fBold, Brushes.White, new RectangleF(320, y + 10, 260, netH), rtlCenter);
					
					g.DrawRectangle(pNavyThin, 20, y, 300, netH);
					g.DrawString($"{netVal:N2} ج.م", fTitle, bNavy, new RectangleF(20, y + 2, 290, netH), rtlCenter);

					y += netH + 20;

					// الوضع المالي للحساب
					if (showFinancial)
					{
						bool isCredit = saleRow["SaleType"].ToString() == "Credit";
						decimal cashPaid = saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(saleRow["CashPaid"]) : netVal;
						decimal remainingFromInvoice = isCredit ? netVal : (netVal - cashPaid);

						decimal totalDue = prevBalance + (isCredit ? netVal : remainingFromInvoice);
						// استخدام الرصيد الفعلي من قاعدة البيانات لضمان احتساب التوريدات
						decimal currentDue = actualCurrentBalance;

						g.FillRectangle(bNavy, 20, y, w - 40, 30);
						g.DrawString("الوضع المالي للحساب", fBold, Brushes.White, new RectangleF(20, y + 6, w - 40, 30), rtlCenter);
						y += 30;

						var labelsList = new System.Collections.Generic.List<string> { "الرصيد السابق" };
						var valsList = new System.Collections.Generic.List<string> { $"{prevBalance:N2} ج.م" };

						if (isCredit)
						{
							labelsList.Add("الفاتورة الحالية");
							valsList.Add($"{netVal:N2} ج.م");

							labelsList.Add("إجمالي المستحق");
							valsList.Add($"{totalDue:N2} ج.م");
						}
						else
						{
							labelsList.Add("المدفوع نقداً");
							valsList.Add($"{cashPaid:N2} ج.م");

							if (remainingFromInvoice > 0)
							{
								labelsList.Add("متبقي الفاتورة");
								valsList.Add($"{remainingFromInvoice:N2} ج.م");
								
								labelsList.Add("إجمالي المستحق");
								valsList.Add($"{totalDue:N2} ج.م");
							}
							else if (remainingFromInvoice < 0)
							{
								labelsList.Add("زيادة الفاتورة");
								valsList.Add($"{-remainingFromInvoice:N2} ج.م");
								
								labelsList.Add("إجمالي المستحق");
								valsList.Add($"{totalDue:N2} ج.م");
							}
						}

						labelsList.Add("مسدد اليوم");
						valsList.Add($"{todayPayments:N2} ج.م");

						if (todayReturns > 0)
						{
							labelsList.Add("مرتجع اليوم");
							valsList.Add($"{todayReturns:N2} ج.م");
						}

						if (AppConfig.EnableCratesTracking)
						{
							int cratesOutVal = saleRow.Table.Columns.Contains("CratesOut") && saleRow["CratesOut"] != DBNull.Value ? Convert.ToInt32(saleRow["CratesOut"]) : 0;
							int cratesInVal = saleRow.Table.Columns.Contains("CratesIn") && saleRow["CratesIn"] != DBNull.Value ? Convert.ToInt32(saleRow["CratesIn"]) : 0;
							if (cratesOutVal > 0)
							{
								labelsList.Add("أقفاص صادرة بالفاتورة");
								valsList.Add($"{cratesOutVal} قفص");
							}
							if (cratesInVal > 0)
							{
								labelsList.Add("أقفاص واردة بالفاتورة");
								valsList.Add($"{cratesInVal} قفص");
							}

							int currentCratesDue = ClientDAL.GetClientCratesBalance(Convert.ToInt32(saleRow["ClientID"]));
							labelsList.Add("رصيد الأقفاص المستحق");
							valsList.Add($"{currentCratesDue} قفص");
						}

						labelsList.Add("الرصيد الحالي المستحق");
						valsList.Add($"{currentDue:N2} ج.م");

						string[] labels = labelsList.ToArray();
						string[] vals = valsList.ToArray();

						for (int i = 0; i < labels.Length; i++)
						{
							g.DrawRectangle(pNavyThin, 20, y, w - 40, 28);
							g.DrawLine(pNavyThin, w / 2, y, w / 2, y + 28);

							bool isLast = (i == labels.Length - 1);
							var brushVal = isLast ? bRed : Brushes.Black;
							var fontLabel = isLast ? fBold : fNormal;
							var fontVal = isLast ? fTitle : fBold;

							g.DrawString(labels[i], fontLabel, isLast ? bRed : bNavy, new RectangleF(w / 2 + 10, y + 5, w / 2 - 30, 22), rtlNear);
							g.DrawString(vals[i], isLast ? fBold : fNormal, brushVal, new RectangleF(25, y + 5, w / 2 - 35, 22), rtlNear);

							y += 28;
						}

						// إضافة سطر إعلامي بآخر توريد سابق تحت الجدول
						string lastPayText = "";
						if (lastPaymentAmt > 0)
						{
							lastPayText = $"* آخر توريد سابق للعميل: {lastPaymentAmt:N2} ج.م بتاريخ {lastPaymentDate:dd/MM/yyyy}";
						}
						else
						{
							lastPayText = "* آخر توريد سابق للعميل: لا يوجد";
						}
						g.DrawString(lastPayText, fNormal, Brushes.Gray, new RectangleF(20, y + 5, w - 40, 22), rtlNear);

						y += 28 + 15;
					}

					// التذييل
					g.DrawRectangle(pNavyThin, 20, y, w - 40, footerH);
					g.DrawString("شكراً لتعاملكم معنا", fComp, bNavy, new RectangleF(20, y + 14, w - 40, footerH), rtlCenter);
					
					DrawChickenSilhouette(g, 100, y + 10, 25);
					DrawChickenSilhouette(g, w - 125, y + 10, 25);

					// الدعاية للبرنامج
					var fPromo = new Font("Arial", 8f, FontStyle.Regular);
					using (var bGray = new SolidBrush(Color.FromArgb(120, 120, 120)))
					{
						g.DrawString("✨ تم إصدار هذه الفاتورة بواسطة Pro System لإدارة المبيعات والتوزيع. للاشتراك: 01016517586", fPromo, bGray, new RectangleF(20, y + footerH + 10, w - 40, 20), rtlCenter);
					}
					fPromo.Dispose();

					fTitle.Dispose();
					fComp.Dispose();
					fBold.Dispose();
					fNormal.Dispose();
					center.Dispose();
					rtlNear.Dispose();
					rtlCenter.Dispose();
				}
			}
			return bmp;
		}

		private void DrawChickenSilhouette(Graphics g, float x, float y, float size)
		{
			using (var brush = new SolidBrush(Color.FromArgb(0, 51, 153)))
			{
				// Body (oval)
				g.FillEllipse(brush, x, y + size * 0.3f, size, size * 0.7f);
				// Head (circle)
				g.FillEllipse(brush, x + size * 0.4f, y, size * 0.5f, size * 0.5f);
				
				// Beak (triangle facing right)
				var beakPoints = new PointF[] {
					new PointF(x + size * 1.02f, y + size * 0.25f),
					new PointF(x + size * 0.88f, y + size * 0.18f),
					new PointF(x + size * 0.88f, y + size * 0.32f)
				};
				g.FillPolygon(brush, beakPoints);
				
				// Tail (triangle facing left)
				var tailPoints = new PointF[] {
					new PointF(x, y + size * 0.4f),
					new PointF(x - size * 0.2f, y + size * 0.1f),
					new PointF(x + size * 0.2f, y + size * 0.5f)
				};
				g.FillPolygon(brush, tailPoints);
				
				// Legs
				using (var pen = new Pen(Color.FromArgb(0, 51, 153), size * 0.08f))
				{
					g.DrawLine(pen, x + size * 0.4f, y + size * 0.9f, x + size * 0.35f, y + size * 1.2f);
					g.DrawLine(pen, x + size * 0.6f, y + size * 0.9f, x + size * 0.65f, y + size * 1.2f);
				}
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
			if (nudCratesOut != null) nudCratesOut.Value = 0;
			if (nudCratesIn != null) nudCratesIn.Value = 0;
			if (cboClient.Items.Count > 0) cboClient.SelectedIndex = 0;
			if (cboDriver.Items.Count > 0) cboDriver.SelectedIndex = 0;
			if (cboProduct.Items.Count > 0) cboProduct.SelectedIndex = 0;
			SetTierButtons("قطاعي");
			dtpDate.Value = DateTime.Today;
			SetInvoiceType(GetDefaultAllowedInvoiceType());
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

                // تأمين: أي أعمدة موجودة في الجدول برمجياً وغير مسجلة في الإعدادات (ترقية جديدة)، نقوم بإضافتها في النهاية
                foreach (System.Windows.Forms.DataGridViewColumn col in dgItems.Columns)
                {
                    if (col.Name == "Delete") continue;
                    if (!ordered.Contains(col.Name))
                    {
                        ordered.Add(col.Name);
                    }
                }

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
		public decimal PendingSalePrice { get; set; } = 0m;
		public decimal PendingQtyThreshold { get; set; } = 0m;
		public string ProductCode { get; set; } = "";
		public string InternationalCode { get; set; } = "";
		/// <summary>صنف خدمة — يُباع بالسالب دون فحص المخزون</summary>
		public bool IsService { get; set; } = false;

		// ─── بيانات الوحدات المتعددة ───────────────────────────────────────────
		/// <summary>اسم الوحدة الأساسية (Unit) — الوحدة الكبرى المستخدمة عند الإضافة</summary>
		public string BaseUnitName { get; set; } = "";
		/// <summary>اسم الوحدة1 (مثل كرتونة)</summary>
		public string Unit1Name { get; set; } = null;
		/// <summary>سعر بيع الوحدة1</summary>
		public decimal Unit1SalePrice { get; set; } = 0m;
		/// <summary>سعر شراء الوحدة1</summary>
		public decimal Unit1PurchasePrice { get; set; } = 0m;
		/// <summary>عامل تحويل الوحدة1 (عدد الوحدات الأساسية في الوحدة1)</summary>
		public decimal Unit1Factor { get; set; } = 1m;
		/// <summary>اسم الوحدة2 (مثل علبة)</summary>
		public string Unit2Name { get; set; } = null;
		/// <summary>عامل تحويل الوحدة2</summary>
		public decimal Unit2Factor { get; set; } = 1m;
		/// <summary>سعر بيع الوحدة2</summary>
		public decimal Unit2SalePrice { get; set; } = 0m;
		/// <summary>سعر شراء الوحدة2</summary>
		public decimal Unit2PurchasePrice { get; set; } = 0m;
		/// <summary>عامل الوحدة3 (الوحدة الأكبر مثل كرتون كبير)</summary>
		public decimal Unit3Factor { get; set; } = 1m;
		public string Unit1Barcode { get; set; } = "";
		public string Unit2Barcode { get; set; } = "";

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


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

		private Button btnTypeVisa;

		private string _invoiceType = "Cash";

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
		private Label lblItemCountTitle;
		private Label lblItemCountVal;

		private ComboBox cboProduct;
		private TextBox txtProductCode;


		private NumericUpDown nudQty;

		private TextBox txtPrice;

		private List<SaleItemDTO> _items = new List<SaleItemDTO>();
		private decimal? _pendingBarcodeWeight = null;
		private decimal? _pendingScaleWeight = null;
		// كاش الأصناف المستقل (بدلاً من cboProduct.Tag)
		private List<ComboItem> _productCache = new List<ComboItem>();
		// FIX: cache أرصدة المخزون لتفادي رحلة DB لكل صنف عند الاختيار
		private Dictionary<int, decimal> _stockCache = new Dictionary<int, decimal>();

		private int _lastSaleID = 0;
        private bool _isDirty = false;
        private int _editSaleID = 0;
        private int _loadedQuoteID = 0; // معرف عرض الأسعار المحول
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
		private bool _searchSessionActive = false; // جلسة البحث السريع
		private Label lblCratesOut;
		private NumericUpDown nudCratesOut;
		private Label lblCratesIn;
		private NumericUpDown nudCratesIn;
		private Label lblClientCratesBalance;
		private Label lblShippingChargeTitle;
		private NumericUpDown nudShippingCharge;
		private Panel pnlQuickItems;
		private FlowLayoutPanel flowQuickItems;
		private Label lblShiftSummaryBar;

		public FrmSale() : this(0, false)
		{
		}

		public FrmSale(int saleID, bool isCopyMode = false)
		{
			_editSaleID = isCopyMode ? 0 : saleID;
			_isCopyMode = isCopyMode;
			InitUI();
			LoadCombos();
			LoadQuickItems();
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
			this.Load += (s, e) =>
			{
				if (txtProductCode != null && txtProductCode.Visible && txtProductCode.Enabled)
				{
					this.ActiveControl = txtProductCode;
					txtProductCode.Focus();
				}
			};
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
			base.Size = new Size(1024, 700);
			base.StartPosition = FormStartPosition.CenterScreen;
			RightToLeft = RightToLeft.Yes;
			RightToLeftLayout = true;
			BackColor = Theme.BgMain;
			Font = Theme.FontMain;
			KeyPreview = true;
			this.KeyDown += FrmSale_KeyDown;
			this.FormClosing += FrmSale_FormClosing;
			
			_barcodeTimer = new System.Windows.Forms.Timer { Interval = 100 };
			_barcodeTimer.Tick += BarcodeTimer_Tick;

			// ── 1. رأس الصفحة (Header Panel) ──────────────────────────────────
			pnlHeader = new Panel
			{
				Dock = DockStyle.Top,
				Height = AppConfig.EnableCratesTracking ? 144 : 114,
				Width = 1024,
				BackColor = Theme.BgCard,
				Padding = new Padding(10, 6, 10, 6)
			};

			var tblHeaderMain = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				RowCount = 1,
				ColumnCount = 2,
				BackColor = Color.Transparent,
				Padding = new Padding(0)
			};
			tblHeaderMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f)); // Right: Invoice details
			tblHeaderMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f)); // Left: Invoice options (Type & Tier)
			tblHeaderMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

			var tblDetails = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				RowCount = AppConfig.EnableCratesTracking ? 4 : 3,
				ColumnCount = 4,
				BackColor = Color.Transparent,
				Padding = new Padding(0)
			};
			tblDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));  // Col 0: Label
			tblDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));   // Col 1: Control (pnlClient)
			tblDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));  // Col 2: Label
			tblDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));   // Col 3: Control (Date/Warehouse)

			tblDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
			tblDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
			tblDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
			if (AppConfig.EnableCratesTracking)
			{
				tblDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
			}

			// Row 0: Client & Date
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
				Margin = new Padding(2)
			};
			SetupSearchableCombo(cboClient);

			lblClientBalance = new Label
			{
				Text = "رصيد: 0.00 ج",
				Width = 95,
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				ForeColor = Theme.Accent,
				TextAlign = ContentAlignment.MiddleRight,
				Dock = DockStyle.Left,
				Margin = new Padding(2)
			};

			Button btnClientStatement = new Button
			{
				Text = "📋 كشف",
				Width = 50,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = Theme.Primary,
				ForeColor = Color.White,
				Cursor = Cursors.Hand,
				Dock = DockStyle.Left,
				Margin = new Padding(2)
			};
			btnClientStatement.FlatAppearance.BorderSize = 0;
			btnClientStatement.Click += (s, e) => {
				if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0) {
					new FrmClientStatement(ci.ID, ci.Text).ShowDialog();
				} else {
					MessageBox.Show("الرجاء اختيار عميل أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			};

			Button btnClientSearch = new Button
			{
				Text = "🔍",
				Width = 40,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = Theme.Accent,
				ForeColor = Color.White,
				Cursor = Cursors.Hand,
				Dock = DockStyle.Left,
				Margin = new Padding(2)
			};
			btnClientSearch.FlatAppearance.BorderSize = 0;
			btnClientSearch.Click += (s, e) =>
			{
				using (var frm = new FrmClientSearch())
				{
					if (frm.ShowDialog() == DialogResult.OK)
					{
						int cid = frm.SelectedClientID;
						if (cid > 0)
						{
							var allClients = cboClient.Tag as List<ComboItem>;
							if (allClients != null)
							{
								cboClient.BeginUpdate();
								cboClient.Items.Clear();
								cboClient.Items.AddRange(allClients.ToArray());
								cboClient.EndUpdate();
							}
							for (int i = 0; i < cboClient.Items.Count; i++)
							{
								if (cboClient.Items[i] is ComboItem ci && ci.ID == cid)
								{
									cboClient.SelectedIndex = i;
									break;
								}
							}
						}
					}
				}
			};

			Button btnClientAdd = new Button
			{
				Text = "➕",
				Width = 30,
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				BackColor = Theme.Success,
				ForeColor = Color.White,
				Cursor = Cursors.Hand,
				Dock = DockStyle.Left,
				Margin = new Padding(2)
			};
			btnClientAdd.FlatAppearance.BorderSize = 0;
			btnClientAdd.Click += (s, e) =>
			{
				new FrmClients().ShowDialog();
				// Reload clients
				DataTable all = ClientDAL.GetAll(activeOnly: true);
				cboClient.BeginUpdate();
				cboClient.Items.Clear();
				List<ComboItem> clientItems = new List<ComboItem>();
				clientItems.Add(new ComboItem(0, "-- اختر عميل --"));
				foreach (DataRow row in all.Rows)
				{
					var item = new ComboItem((int)row["ClientID"], row["ClientName"].ToString());
					item.ClientCode = row["ClientCode"] != DBNull.Value ? row["ClientCode"].ToString().Trim() : "";
					item.Phone = row["Phone"] != DBNull.Value ? row["Phone"].ToString().Trim() : "";
					item.Phone2 = row["Phone2"] != DBNull.Value ? row["Phone2"].ToString().Trim() : "";
					clientItems.Add(item);
				}
				cboClient.Items.AddRange(clientItems.ToArray());
				cboClient.Tag = clientItems;
				cboClient.EndUpdate();

				// Try to select the latest client
				object latestIdObj = DbHelper.Scalar("SELECT TOP 1 ClientID FROM Clients ORDER BY ClientID DESC");
				if (latestIdObj != null && int.TryParse(latestIdObj.ToString(), out int latestId) && latestId > 0)
				{
					for (int i = 0; i < cboClient.Items.Count; i++)
					{
						if (cboClient.Items[i] is ComboItem ci && ci.ID == latestId)
						{
							cboClient.SelectedIndex = i;
							break;
						}
					}
				}
			};

			pnlClient.Controls.Add(cboClient);
			pnlClient.Controls.Add(lblClientBalance);
			pnlClient.Controls.Add(btnClientSearch);
			pnlClient.Controls.Add(btnClientStatement);
			pnlClient.Controls.Add(btnClientAdd);
			cboClient.SendToBack();

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
				Margin = new Padding(2),
				Enabled = Session.CanAccess("EditInvoiceDate")
			};

			tblDetails.Controls.Add(lblClient, 0, 0);
			tblDetails.Controls.Add(pnlClient, 1, 0);
			tblDetails.Controls.Add(lblDate, 2, 0);
			tblDetails.Controls.Add(dtpDate, 3, 0);

			// Row 1: Driver & Warehouse
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
				Margin = new Padding(2)
			};
			SetupSearchableCombo(cboDriver);

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
				Margin = new Padding(2)
			};

			tblDetails.Controls.Add(lblDriver, 0, 1);
			tblDetails.Controls.Add(cboDriver, 1, 1);
			lblDriver.Visible = Session.CanSelectDriver;
			cboDriver.Visible = Session.CanSelectDriver;
			tblDetails.Controls.Add(lblWarehouse, 2, 1);
			tblDetails.Controls.Add(cboWarehouse, 3, 1);

			// Row 2: Safe Account & Notes
			lblSafeAccount = MakeLabel("الخزينة :", 0, 0);
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
				Margin = new Padding(2)
			};

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
				Margin = new Padding(2)
			};

			tblDetails.Controls.Add(lblSafeAccount, 0, 2);
			tblDetails.Controls.Add(cboSafeAccount, 1, 2);
			tblDetails.Controls.Add(lblNotes, 2, 2);
			tblDetails.Controls.Add(txtNotes, 3, 2);

			// Row 3: Crates Tracking (only if enabled)
			lblCratesOut = MakeLabel("فوارغ صادرة :", 0, 0);
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
				Margin = new Padding(2, 4, 2, 4)
			};

			lblCratesIn = MakeLabel("فوارغ واردة :", 0, 0);
			lblCratesIn.Dock = DockStyle.Fill;
			lblCratesIn.TextAlign = ContentAlignment.MiddleRight;
			lblCratesIn.Margin = new Padding(2);

			nudCratesIn = new NumericUpDown
			{
				Minimum = 0,
				Maximum = 9999,
				DecimalPlaces = 0,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				Width = 80,
				Dock = DockStyle.Right,
				Margin = new Padding(2, 4, 2, 4)
			};

			lblClientCratesBalance = new Label
			{
				Text = "فوارغ العميل: 0 فارغ",
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				ForeColor = Theme.Accent,
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				Margin = new Padding(6, 4, 2, 4)
			};

			var pnlCratesInBalance = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
			pnlCratesInBalance.Controls.Add(lblClientCratesBalance);
			pnlCratesInBalance.Controls.Add(nudCratesIn);
			lblClientCratesBalance.SendToBack();

			if (AppConfig.EnableCratesTracking)
			{
				tblDetails.Controls.Add(lblCratesOut, 0, 3);
				tblDetails.Controls.Add(nudCratesOut, 1, 3);
				tblDetails.Controls.Add(lblCratesIn, 2, 3);
				tblDetails.Controls.Add(pnlCratesInBalance, 3, 3);
			}
			else
			{
				lblCratesOut.Visible = false;
				nudCratesOut.Visible = false;
				lblCratesIn.Visible = false;
				nudCratesIn.Visible = false;
				lblClientCratesBalance.Visible = false;
			}

			// Right side options (Type & Tier Grouping)
			var pnlOptions = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.TopDown,
				BackColor = Color.Transparent,
				Padding = new Padding(4, 0, 4, 0),
				WrapContents = false
			};

			// Group 1: Invoice Type Card
			var pnlTypeGroup = new Panel
			{
				Width = 430,
				Height = 50,
				BackColor = Color.FromArgb(30, 41, 59),
				Padding = new Padding(6, 2, 6, 2),
				Margin = new Padding(0, 0, 0, 4)
			};
			var lblTypeHeader = new Label
			{
				Text = "💳 نوع الدفع / الفاتورة :",
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				ForeColor = Color.FromArgb(226, 232, 240),
				Dock = DockStyle.Top,
				Height = 16,
				TextAlign = ContentAlignment.TopRight
			};
			pnlTypeGroup.Controls.Add(lblTypeHeader);

			var pnlTypeButtonsFlow = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft,
				BackColor = Color.Transparent,
				WrapContents = false,
				Margin = new Padding(0)
			};
			
			btnTypeCash = new Button { Text = "💵 نقدي", Width = 74, Height = 26, Font = Theme.FontBold, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(2) };
			btnTypeCash.FlatAppearance.BorderSize = 0;
			btnTypeCash.Click += delegate {
				if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
				{
					DataRow clientRow = ClientDAL.GetByID(ci.ID);
					if (clientRow != null && clientRow.Table.Columns.Contains("DefaultPaymentType") && clientRow["DefaultPaymentType"] != DBNull.Value)
					{
						string ptype = clientRow["DefaultPaymentType"].ToString();
						if (string.Equals(ptype, "Credit", StringComparison.OrdinalIgnoreCase) || ptype == "آجل")
						{
							MessageBox.Show("⚠️ هذا العميل محدَّد في كارت العميل لـ (آجل فقط)، لا يمكن البيع له نقداً!", "طريقة الدفع غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							return;
						}
					}
				}
				SetInvoiceType("Cash");
			};

			btnTypeCredit = new Button { Text = "⏳ آجل", Width = 74, Height = 26, Font = Theme.FontBold, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(2) };
			btnTypeCredit.FlatAppearance.BorderSize = 0;
			btnTypeCredit.Click += delegate {
				if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
				{
					DataRow clientRow = ClientDAL.GetByID(ci.ID);
					if (clientRow != null && clientRow.Table.Columns.Contains("DefaultPaymentType") && clientRow["DefaultPaymentType"] != DBNull.Value)
					{
						string ptype = clientRow["DefaultPaymentType"].ToString();
						if (string.Equals(ptype, "Cash", StringComparison.OrdinalIgnoreCase) || ptype == "كاش")
						{
							MessageBox.Show("⚠️ هذا العميل محدَّد في كارت العميل لـ (كاش فقط)، لا يمكن البيع له بالأجل!", "طريقة الدفع غير مسموحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							return;
						}
					}
				}
				SetInvoiceType("Credit");
			};

			btnTypeVisa = new Button { Text = "💳 فيزا", Width = 74, Height = 26, Font = Theme.FontBold, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(2) };
			btnTypeVisa.FlatAppearance.BorderSize = 0;
			btnTypeVisa.Click += delegate { SetInvoiceType("Visa"); };

			btnTypeInstallment = new Button { Text = "📅 تقسيط", Width = 74, Height = 26, Font = Theme.FontBold, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(2) };
			btnTypeInstallment.FlatAppearance.BorderSize = 0;
			btnTypeInstallment.Click += delegate { SetInvoiceType("Installment"); };

			btnTypeDriverLoad = new Button { Text = "🚚 تحميل", Width = 74, Height = 26, Font = Theme.FontBold, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(2) };
			btnTypeDriverLoad.FlatAppearance.BorderSize = 0;
			btnTypeDriverLoad.Click += delegate { SetInvoiceType("DriverLoad"); };

			pnlTypeButtonsFlow.Controls.Add(btnTypeCash);
			pnlTypeButtonsFlow.Controls.Add(btnTypeCredit);
			pnlTypeButtonsFlow.Controls.Add(btnTypeVisa);
			pnlTypeButtonsFlow.Controls.Add(btnTypeInstallment);
			pnlTypeButtonsFlow.Controls.Add(btnTypeDriverLoad);
			pnlTypeGroup.Controls.Add(pnlTypeButtonsFlow);
			pnlTypeButtonsFlow.BringToFront();

			// Group 2: Price Tiers Card
			var pnlTierGroup = new Panel
			{
				Width = 430,
				Height = 48,
				BackColor = Color.FromArgb(30, 41, 59),
				Padding = new Padding(6, 2, 6, 2),
				Margin = new Padding(0, 0, 0, 4)
			};
			var lblTierHeader = new Label
			{
				Text = "🏷️ فئة السعر :",
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				ForeColor = Color.FromArgb(226, 232, 240),
				Dock = DockStyle.Top,
				Height = 16,
				TextAlign = ContentAlignment.TopRight
			};
			pnlTierGroup.Controls.Add(lblTierHeader);

			var pnlTierButtonsFlow = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft,
				BackColor = Color.Transparent,
				WrapContents = false,
				Margin = new Padding(0)
			};

			btnTierRetail = new Button { Text = "🔵 قطاعي", Width = 110, Height = 26, Font = Theme.FontBold, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(2) };
			btnTierRetail.FlatAppearance.BorderSize = 0;
			btnTierRetail.Click += (s, e) => ApplyTierChange("قطاعي");

			btnTierSemi = new Button { Text = "🟣 نصف جملة", Width = 110, Height = 26, Font = Theme.FontBold, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(2) };
			btnTierSemi.FlatAppearance.BorderSize = 0;
			btnTierSemi.Click += (s, e) => ApplyTierChange("نصف جملة");

			btnTierWholesale = new Button { Text = "🟠 جملة", Width = 110, Height = 26, Font = Theme.FontBold, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(2) };
			btnTierWholesale.FlatAppearance.BorderSize = 0;
			btnTierWholesale.Click += (s, e) => ApplyTierChange("جملة");

			pnlTierButtonsFlow.Controls.Add(btnTierRetail);
			pnlTierButtonsFlow.Controls.Add(btnTierSemi);
			pnlTierButtonsFlow.Controls.Add(btnTierWholesale);
			pnlTierGroup.Controls.Add(pnlTierButtonsFlow);
			pnlTierButtonsFlow.BringToFront();

			// Group 3: Shift Status Card
			var pnlShiftGroup = new Panel
			{
				Width = 320,
				Height = 44,
				BackColor = Color.FromArgb(43, 50, 70),
				Padding = new Padding(6, 2, 6, 2),
				Margin = new Padding(0, 0, 0, 4)
			};
			var lblShiftTitleHeader = new Label
			{
				Text = "الوردية والدرج المفتوح :",
				Font = Theme.FontSmall,
				ForeColor = Theme.TextSub,
				Dock = DockStyle.Top,
				Height = 15,
				TextAlign = ContentAlignment.TopRight
			};
			pnlShiftGroup.Controls.Add(lblShiftTitleHeader);

			lblShiftSummaryBar = new Label
			{
				Text = "🔄 جاري التحميل...",
				Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
				ForeColor = Color.FromArgb(74, 222, 128),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleCenter,
				Cursor = Cursors.Hand
			};
			lblShiftSummaryBar.Click += (s, e) =>
			{
				if (Session.CurrentShiftID == null)
				{
					using (var dlg = new FrmOpenShift())
					{
						if (dlg.ShowDialog(this) == DialogResult.OK)
						{
							UpdateShiftSummaryLabel();
						}
					}
				}
				else
				{
					new FrmShiftClose().ShowDialog(this);
					UpdateShiftSummaryLabel();
				}
			};
			pnlShiftGroup.Controls.Add(lblShiftSummaryBar);
			lblShiftSummaryBar.BringToFront();

			pnlOptions.Controls.Add(pnlTypeGroup);
			pnlOptions.Controls.Add(pnlTierGroup);
			pnlOptions.Controls.Add(pnlShiftGroup);

			tblHeaderMain.Controls.Add(tblDetails, 0, 0);
			tblHeaderMain.Controls.Add(pnlOptions, 1, 0);
			pnlHeader.Controls.Add(tblHeaderMain);

			// ── 2. شريط اختيار وإدخال الأصناف (Product Entry Bar) ───────────────
			var pnlProductBar = new Panel
			{
				Dock = DockStyle.Top,
				Height = 44,
				BackColor = Theme.BgCard,
				Padding = new Padding(6, 4, 6, 4)
			};

			var tblProductBar = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				RowCount = 1,
				ColumnCount = 6,
				BackColor = Color.Transparent,
				Padding = new Padding(0)
			};
			tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85f));   // Col 0: Label
			tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // Col 1: txtProductCode (يملأ المساحة)
			tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0f));   // Col 2: مخفي (كان cboProduct)
			tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));  // Col 3: btnSearchProduct
			tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));   // Col 4: btnManualAdd
			tblProductBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));  // Col 5: btnCustomizeCols

			var lblProductTitle = MakeLabel("الصنف (F12) :", 0, 0);
			lblProductTitle.Dock = DockStyle.Fill;
			lblProductTitle.TextAlign = ContentAlignment.MiddleRight;
			lblProductTitle.Margin = new Padding(2);

			txtProductCode = new TextBox
			{
				Dock = DockStyle.Fill,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				BorderStyle = BorderStyle.FixedSingle,
				RightToLeft = RightToLeft.Yes,
				Margin = new Padding(2, 6, 2, 6),
				Font = Theme.FontMain
			};
			txtProductCode.KeyDown += (s, e) =>
			{
				if (e.KeyCode == Keys.Down)
				{
					e.Handled = true;
					e.SuppressKeyPress = true;
					AddNewCodeRow();
					return;
				}
				if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
				{
					string scanText = txtProductCode.Text.Trim();
					if (string.IsNullOrEmpty(scanText))
					{
						cboProduct.SelectedIndex = 0;
						return;
					}

					DataRow pRow = ProductDAL.GetByBarcodeOrScaleCode(scanText, out decimal weight);
					if (pRow != null)
					{
						int pid = Convert.ToInt32(pRow["ProductID"]);
						e.Handled = true;
						e.SuppressKeyPress = true;

						_isScanningBarcode = true;
						try
						{
							AddOrUpdateProduct(pid, weight > 0 ? weight : 1.00m, scannedBarcode: scanText);
							
							for (int i = 0; i < cboProduct.Items.Count; i++)
							{
								if (cboProduct.Items[i] is ComboItem ci && ci.ID == pid)
								{
									cboProduct.SelectedIndex = i;
									break;
								}
							}

							txtProductCode.Clear();
							FocusQtyCellInGrid(pid);
						}
						finally
						{
							_isScanningBarcode = false;
						}
					}
					else
					{
						MessageBox.Show($"لم يتم العثور على الصنف برقم الباركود أو كود الميزان ({scanText})!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			};

			// cboProduct: نُبقي على الـ ComboBox مخفياً فقط كحاوية للكاش (لا يظهر في الواجهة)
			cboProduct = new ComboBox { Visible = false, Width = 0 };
			// لا نضيف أي Event handlers للـ cboProduct بعد الآن


			btnSearchProduct = new Button
			{
				Text = "🔍 بحث سريع (F3)",
				Dock = DockStyle.Fill,
				BackColor = Theme.Accent,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Font = Theme.FontBold,
				Margin = new Padding(4, 2, 4, 2)
			};
			btnSearchProduct.FlatAppearance.BorderSize = 0;
			btnSearchProduct.Click += BtnSearchProduct_Click;

			var btnManualAdd = new Button
			{
				Text = "➕ إضافة",
				Dock = DockStyle.Fill,
				BackColor = Theme.Success,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Font = Theme.FontBold,
				Margin = new Padding(4, 2, 4, 2)
			};
			btnManualAdd.FlatAppearance.BorderSize = 0;
			btnManualAdd.Click += BtnManualAdd_Click;

			btnCustomizeCols = new Button
			{
				Text      = "⚙️ الأعمدة",
				Dock = DockStyle.Fill,
				BackColor = Color.FromArgb(55, 65, 81),
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Font      = Theme.FontBold,
				Cursor    = Cursors.Hand,
				Margin = new Padding(4, 2, 4, 2)
			};
			btnCustomizeCols.FlatAppearance.BorderSize = 0;
			btnCustomizeCols.Click += (s, e) => ShowColumnCustomizer();

			bool canOrder = Session.CanOrderColumns("Sales");
			btnCustomizeCols.Visible = canOrder;
			if (!canOrder)
			{
				tblProductBar.ColumnStyles[5].Width = 0f;
			}

			tblProductBar.Controls.Add(lblProductTitle, 0, 0);
			tblProductBar.Controls.Add(txtProductCode, 1, 0);
			// Col 2 مخفي (لا نضيف cboProduct للواجهة)
			tblProductBar.Controls.Add(btnSearchProduct, 3, 0);
			tblProductBar.Controls.Add(btnManualAdd, 4, 0);
			tblProductBar.Controls.Add(btnCustomizeCols, 5, 0);

			pnlProductBar.Controls.Add(tblProductBar);

			// Background initialization to prevent NullReferenceException:
			nudQty = new NumericUpDown { Value = 1m };
			txtPrice = new TextBox();
			btnAddItem = new Button();

			// ── 3. جدول الأصناف (Items Panel & Grid) ──────────────────────────
			pnlItems = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(5)
			};

			pnlQuickItems = new Panel
			{
				Dock = DockStyle.Left,
				Width = 210,
				BackColor = Theme.BgCard,
				Padding = new Padding(5),
				BorderStyle = BorderStyle.FixedSingle,
				Visible = Session.CanViewQuickItems("Sales")
			};

			var lblQuickTitle = new Label
			{
				Text = "⭐ أصناف سريعة",
				Dock = DockStyle.Top,
				Height = 30,
				Font = new Font(Theme.FontMain.FontFamily, 11f, FontStyle.Bold),
				ForeColor = Theme.Accent,
				TextAlign = ContentAlignment.MiddleCenter
			};

			flowQuickItems = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				FlowDirection = FlowDirection.LeftToRight,
				RightToLeft = RightToLeft.Yes,
				Padding = new Padding(2)
			};
			pnlQuickItems.Controls.Add(flowQuickItems);
			pnlQuickItems.Controls.Add(lblQuickTitle);

			dgItems = new DataGridView
			{
				Dock = DockStyle.Fill,
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				RowHeadersVisible = false,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				ReadOnly = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				RightToLeft = RightToLeft.Yes,
				GridColor = Color.FromArgb(210, 210, 215),
				CellBorderStyle = DataGridViewCellBorderStyle.Single,
				ScrollBars = ScrollBars.Both,
				EnableHeadersVisualStyles = false,
				DefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Color.White,
					ForeColor = Theme.TextMain,
					SelectionBackColor = Theme.Primary,
					SelectionForeColor = Color.White,
					Font = Theme.FontMain
				},
				AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Color.FromArgb(240, 242, 245),
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
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
			};
			
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodeEntry", HeaderText = "كود الصنف", ReadOnly = false, FillWeight = 55f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", ReadOnly = true });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductSize", HeaderText = "المقاس", ReadOnly = true, FillWeight = 35f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "اللون", ReadOnly = true, FillWeight = 35f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartNumber", HeaderText = "رقم القطعة", ReadOnly = true, FillWeight = 40f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CarModel", HeaderText = "الموديل", ReadOnly = true, FillWeight = 40f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Brand", HeaderText = "الماركة", ReadOnly = true, FillWeight = 40f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLocation", HeaderText = "مكان العرض", ReadOnly = true, FillWeight = 30f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQty", HeaderText = "الرصيد الفعلي", ReadOnly = true, FillWeight = 40f });
			dgItems.Columns.Add(new DataGridViewComboBoxColumn { Name = "UnitName", HeaderText = "الوحدة", ReadOnly = false, FillWeight = 40f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", ReadOnly = false, FillWeight = 40f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "السعر", ReadOnly = !Session.CanEditPrice(), FillWeight = 40f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastClientPrice", HeaderText = "آخر سعر للعميل 🏷️", ReadOnly = true, FillWeight = 40f, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(230, 126, 34), Font = new Font("Segoe UI", 9f, FontStyle.Bold) } });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountPct", HeaderText = "خصم %", ReadOnly = false, FillWeight = 30f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountAmt", HeaderText = "قيمة خصم", ReadOnly = false, FillWeight = 35f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "الإجمالي", ReadOnly = true, FillWeight = 50f });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate", HeaderText = "الصلاحية", ReadOnly = true, FillWeight = 45f, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "IMEI", HeaderText = "السيريال", ReadOnly = false, FillWeight = 55f, Visible = true });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", HeaderText = "سعر التكلفة", ReadOnly = true, FillWeight = 40f, Visible = Session.CanViewCost("Sales") });
			dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostTotal", HeaderText = "إجمالي التكلفة", ReadOnly = true, FillWeight = 50f, Visible = Session.CanViewCost("Sales") });
			
			DataGridViewButtonColumn delCol = new DataGridViewButtonColumn
			{
				Name = "Delete",
				HeaderText = "",
				Text = "\ud83d\uddd1",
				UseColumnTextForButtonValue = true,
				FillWeight = 20f
			};
			dgItems.Columns.Add(delCol);
			Theme.AdjustGridHeaders(dgItems);

			dgItems.CellDoubleClick += (s, e) =>
			{
				if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
				{
					string colName = dgItems.Columns[e.ColumnIndex].Name;
					if (colName == "Quantity" || colName == "UnitPrice" || colName == "DiscountPct" || colName == "DiscountAmt" || colName == "UnitName" || colName == "IMEI")
					{
						return; // السماح بتعديل الخانات التفاعلية مباشرة
					}
				}
				BtnSearchProduct_Click(s, e);
			};

			dgItems.DoubleClick += (s, e) =>
			{
				if (dgItems.SelectedCells.Count == 0 || (dgItems.CurrentCell != null && dgItems.CurrentCell.ReadOnly))
				{
					BtnSearchProduct_Click(s, e);
				}
			};

			foreach (DataGridViewColumn col in dgItems.Columns)
			{
				col.MinimumWidth = 95;
			}
			if (dgItems.Columns.Contains("ProductName"))
			{
				dgItems.Columns["ProductName"].MinimumWidth = 160;
			}
			
			dgItems.AllowUserToOrderColumns = Session.CanOrderColumns("Sales");
			Session.LoadColumnOrder(dgItems, "Sales");

			dgItems.CellClick += DgItems_CellClick;
			dgItems.CellEndEdit += DgItems_CellEndEdit;
			dgItems.EditingControlShowing += DgItems_EditingControlShowing;
			dgItems.RowsAdded   += (s, e) => _isDirty = true;
			dgItems.RowsRemoved += (s, e) => _isDirty = true;
			
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
			pnlItems.Controls.Add(pnlQuickItems);
			pnlItems.Controls.Add(pnlProductBar);
			LoadColumnSettings();

			// ── 4. تذييل الصفحة (Footer Panel & Summary) ─────────────────────
			pnlFooter = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 84,
				Width = 1024,
				BackColor = Theme.BgCard
			};

			var pnlSummaryFlow = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 34,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				BackColor = Color.Transparent,
				Padding = new Padding(5, 2, 5, 2),
				RightToLeft = RightToLeft.Yes,
				AutoScroll = false
			};

			// 1. إجمالي الأصناف
			Label lblTotalTitle = MakeLabel("إجمالي الأصناف:", 0, 0);
			lblTotalTitle.AutoSize = true;
			lblTotalTitle.ForeColor = Theme.TextSub;
			lblTotalTitle.Margin = new Padding(2, 6, 0, 0);

			lblTotalVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.TextMain,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				AutoSize = true,
				Margin = new Padding(2, 5, 12, 0)
			};

			pnlSummaryFlow.Controls.Add(lblTotalTitle);
			pnlSummaryFlow.Controls.Add(lblTotalVal);

			// 2. الخصم
			Label lblDiscType = MakeLabel("الخصم:", 0, 0);
			lblDiscType.AutoSize = true;
			lblDiscType.ForeColor = Theme.TextSub;
			lblDiscType.Margin = new Padding(2, 6, 0, 0);

			cboInvoiceDiscountType = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Width = 65,
				Margin = new Padding(2, 3, 2, 0)
			};
			cboInvoiceDiscountType.Items.AddRange(new object[] { "قيمة", "نسبة %" });
			cboInvoiceDiscountType.SelectedIndex = 0;
			cboInvoiceDiscountType.SelectedIndexChanged += (s, e) => CalculateNet();

			txtInvoiceDiscount = new TextBox
			{
				Text = "0",
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				BorderStyle = BorderStyle.FixedSingle,
				RightToLeft = RightToLeft.Yes,
				Width = 65,
				Margin = new Padding(2, 3, 12, 0)
			};
			txtInvoiceDiscount.TextChanged += (s, e) => CalculateNet();

			pnlSummaryFlow.Controls.Add(lblDiscType);
			pnlSummaryFlow.Controls.Add(cboInvoiceDiscountType);
			pnlSummaryFlow.Controls.Add(txtInvoiceDiscount);

			// 3. شحن / تحميل
			lblShippingChargeTitle = MakeLabel("شحن:", 0, 0);
			lblShippingChargeTitle.AutoSize = true;
			lblShippingChargeTitle.ForeColor = Theme.TextSub;
			lblShippingChargeTitle.Margin = new Padding(2, 6, 0, 0);

			nudShippingCharge = new NumericUpDown
			{
				Minimum = 0,
				Maximum = 1000000,
				DecimalPlaces = 2,
				Value = 0,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				BorderStyle = BorderStyle.FixedSingle,
				RightToLeft = RightToLeft.Yes,
				Width = 70,
				Margin = new Padding(2, 3, 12, 0)
			};
			nudShippingCharge.ValueChanged += (s, e) => CalculateNet();

			pnlSummaryFlow.Controls.Add(lblShippingChargeTitle);
			pnlSummaryFlow.Controls.Add(nudShippingCharge);

			// 4. التكلفة والربح (إن وجدت الصلاحية)
			if (Session.CanViewCost("Sales"))
			{
				lblCostTitle = MakeLabel("التكلفة:", 0, 0);
				lblCostTitle.AutoSize = true;
				lblCostTitle.ForeColor = Theme.TextSub;
				lblCostTitle.Margin = new Padding(2, 6, 0, 0);

				lblCostVal = new Label
				{
					Text = "0.00 ج",
					ForeColor = Theme.TextMain,
					Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
					AutoSize = true,
					Margin = new Padding(2, 5, 10, 0)
				};

				lblProfitTitle = MakeLabel("الربح:", 0, 0);
				lblProfitTitle.AutoSize = true;
				lblProfitTitle.ForeColor = Theme.TextSub;
				lblProfitTitle.Margin = new Padding(2, 6, 0, 0);

				lblProfitVal = new Label
				{
					Text = "0.00 ج",
					ForeColor = Theme.Success,
					Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
					AutoSize = true,
					Margin = new Padding(2, 5, 12, 0)
				};

				pnlSummaryFlow.Controls.Add(lblCostTitle);
				pnlSummaryFlow.Controls.Add(lblCostVal);
				pnlSummaryFlow.Controls.Add(lblProfitTitle);
				pnlSummaryFlow.Controls.Add(lblProfitVal);
			}

			// 5. عدد الأصناف
			lblItemCountTitle = MakeLabel("الأصناف:", 0, 0);
			lblItemCountTitle.AutoSize = true;
			lblItemCountTitle.ForeColor = Theme.TextSub;
			lblItemCountTitle.Margin = new Padding(2, 6, 0, 0);

			lblItemCountVal = MakeLabel("0", 0, 0);
			lblItemCountVal.AutoSize = true;
			lblItemCountVal.ForeColor = Theme.Accent;
			lblItemCountVal.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
			lblItemCountVal.Margin = new Padding(2, 5, 15, 0);

			pnlSummaryFlow.Controls.Add(lblItemCountTitle);
			pnlSummaryFlow.Controls.Add(lblItemCountVal);

			// 6. صافي الفاتورة
			Label lblNetTitle = MakeLabel("صافي الفاتورة:", 0, 0);
			lblNetTitle.AutoSize = true;
			lblNetTitle.ForeColor = Theme.TextSub;
			lblNetTitle.Margin = new Padding(2, 6, 0, 0);

			lblNetVal = new Label
			{
				Text = "0.00 ج",
				ForeColor = Theme.Accent,
				Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
				AutoSize = true,
				Margin = new Padding(2, 3, 0, 0)
			};

			pnlSummaryFlow.Controls.Add(lblNetTitle);
			pnlSummaryFlow.Controls.Add(lblNetVal);

			// Footer buttons (RTL flow)
			btnSave = Theme.MakeButton("💾 حفظ الفاتورة (F5)", 0, 0, 180, 28, Theme.Accent);
			btnSave.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

			Button btnHold = Theme.MakeButton("⏸️ تعليق", 0, 0, 90, 26, Color.FromArgb(200, 140, 50));
			Button btnLoadHold = Theme.MakeButton("📂 معلقات", 0, 0, 90, 26, Color.FromArgb(100, 100, 150));
			Button btnTawreed = Theme.MakeButton("💵 توريد", 0, 0, 80, 26, Theme.Success);
			btnNew = Theme.MakeButton("🆕 جديد", 0, 0, 75, 26, Color.FromArgb(80, 120, 80));
			btnPrint = Theme.MakeButton("🖨️ طباعة", 0, 0, 90, 26, Theme.Primary);
			btnPreview = Theme.MakeButton("🔍 معاينة", 0, 0, 90, 26, Color.FromArgb(70, 80, 90));
			btnPrint.Visible = false;
			btnPreview.Visible = false;

			btnWhatsApp = Theme.MakeButton("📲 واتساب", 0, 0, 90, 26, Color.FromArgb(37, 211, 102));
			Button btnPrepSlip = Theme.MakeButton("📋 إذن تحضير (F9)", 0, 0, 130, 26, Color.FromArgb(41, 128, 185));

			btnSave.Anchor = AnchorStyles.None;
			btnHold.Anchor = AnchorStyles.None;
			btnLoadHold.Anchor = AnchorStyles.None;
			btnTawreed.Anchor = AnchorStyles.None;
			btnNew.Anchor = AnchorStyles.None;
			btnPrint.Anchor = AnchorStyles.None;
			btnPreview.Anchor = AnchorStyles.None;
			btnWhatsApp.Anchor = AnchorStyles.None;
			btnPrepSlip.Anchor = AnchorStyles.None;

			btnSave.Click += BtnSave_Click;
			btnHold.Click += BtnHold_Click;
			btnLoadHold.Click += BtnLoadHold_Click;
			btnTawreed.Click += BtnTawreed_Click;
			btnNew.Click += delegate { ResetForm(); };
			btnPrint.Click += BtnPrint_Click;
			btnPreview.Click += BtnPreview_Click;
			btnWhatsApp.Click += BtnWhatsApp_Click;
			btnPrepSlip.Click += (s, e) => PrintPreparationSlip();

			var pnlFooterButtons = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.RightToLeft,
				Dock = DockStyle.Bottom,
				Height = 34,
				Padding = new Padding(10, 3, 10, 3),
				BackColor = Color.Transparent,
				RightToLeft = RightToLeft.Yes,
				WrapContents = false,
				AutoSize = false
			};
			btnWhatsApp.Margin = new Padding(2);
			btnPrepSlip.Margin = new Padding(2);
			btnNew.Margin = new Padding(2);
			btnTawreed.Margin = new Padding(2);
			btnLoadHold.Margin = new Padding(2);
			btnHold.Margin = new Padding(2);
			btnSave.Margin = new Padding(2);
			pnlFooterButtons.Controls.AddRange(new Control[] { btnWhatsApp, btnPrepSlip, btnNew, btnTawreed, btnLoadHold, btnHold, btnSave });

			// Status bar for Hotkeys
			var pnlStatus = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 18,
				BackColor = Theme.BgMain,
				Padding = new Padding(10, 1, 10, 1)
			};
			var lblHotkeys = new Label
			{
				Text = "الاختصارات: [F2] جديدة | [F5] حفظ | [F9] إذن تحضير | [F12] تركيز الصنف | [F3] بحث سريع | [Ctrl+1/2/3] تغيير الوحدة",
				ForeColor = Theme.TextSub,
				Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleRight,
				RightToLeft = RightToLeft.Yes
			};
			pnlStatus.Controls.Add(lblHotkeys);

			pnlFooter.Controls.Add(pnlSummaryFlow);
			pnlFooter.Controls.Add(pnlFooterButtons);
			pnlFooter.Controls.Add(pnlStatus);

			base.Controls.Add(pnlItems);
			base.Controls.Add(pnlFooter);
			base.Controls.Add(pnlHeader);
			
			pnlItems.BringToFront();
			ToggleType();
			Theme.ApplyFormRTL(this);
			ApplyInputStyles(this);
		}

		private void ApplyInputStyles(Control parent)
		{
			foreach (Control c in parent.Controls)
			{
				if (c is TextBox || c is ComboBox || c is DateTimePicker || c is NumericUpDown)
				{
					c.BackColor = Theme.BgInput;
					c.ForeColor = Theme.TextInput;
				}
				else if (c.HasChildren)
				{
					ApplyInputStyles(c);
				}
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

			// ─── اختصارات تغيير الوحدات بالكيبورد (Ctrl + 1/2/3) ───
			if (e.Control && (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1 || e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2 || e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3))
			{
				if (dgItems.CurrentRow != null && dgItems.CurrentRow.Index >= 0 && dgItems.CurrentRow.Index < _items.Count)
				{
					int rowIndex = dgItems.CurrentRow.Index;
					var dto = _items[rowIndex];
					ComboItem prod = GetProductComboItem(dto.ProductID);
					if (prod != null)
					{
						string targetUnit = null;
						if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
						{
							targetUnit = prod.BaseUnitName; // الوحدة الكبرى
						}
						else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
						{
							targetUnit = prod.Unit2Name; // الوحدة المتوسطة
						}
						else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
						{
							targetUnit = prod.Unit1Name; // الوحدة الصغرى
						}

						if (!string.IsNullOrEmpty(targetUnit))
						{
							if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
							
							if (dgItems.Rows[rowIndex].Cells["UnitName"] is DataGridViewComboBoxCell cell)
							{
								if (cell.Items.Contains(targetUnit))
								{
									cell.Value = targetUnit;
									HandleUnitChange(dgItems.Rows[rowIndex], dto, targetUnit);
									e.Handled = true;
								}
								else
								{
									MessageBox.Show($"⚠️ الوحدة '{targetUnit}' غير متوفرة لهذا الصنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
								}
							}
						}
					}
				}
			}

			if      (e.KeyCode == Keys.F2)  { btnNew.PerformClick(); e.Handled = true; }
			else if (e.KeyCode == Keys.F5)  { btnSave.PerformClick(); e.Handled = true; }
			else if (e.KeyCode == Keys.F9)  { PrintPreparationSlip(); e.Handled = true; }
			else if (e.KeyCode == Keys.F12) { txtProductCode.Focus(); txtProductCode.SelectAll(); e.Handled = true; }
			else if (e.KeyCode == Keys.F3)  { btnSearchProduct.PerformClick(); e.Handled = true; } // F3 = فتح شاشة البحث
			else if (e.Control && e.KeyCode == Keys.D) { RawPrinterHelper.OpenCashDrawer(); e.Handled = true; }
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Insert || keyData == Keys.Down)
			{
				if (keyData == Keys.Down)
				{
					if (dgItems != null && (dgItems.IsCurrentCellInEditMode || dgItems.EditingControl != null))
					{
						return base.ProcessCmdKey(ref msg, keyData);
					}
				}

				AddNewCodeRow();
				return true;
			}
			// إذا كانت قائمة المنتجات مفتوحة (لا ينطبق بعد الآن - cboProduct مخفي)
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
									txtProductCode?.Focus();
								}
							});
							return true;
						}
						else
						{
							this.BeginInvoke((MethodInvoker)delegate
							{
								txtProductCode?.Focus();
							});
							return true;
						}
					}
					else
					{
						dgItems.EndEdit();
						this.BeginInvoke((MethodInvoker)delegate
						{
							txtProductCode?.Focus();
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
					if (ci.ID > 0 && (
						ci.ID.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode || 
						ci.PartNumber == res.ItemCode ||
						(int.TryParse(ci.ProductCode, out int pCodeVal) && pCodeVal.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode)
					))
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
					AddOrUpdateProduct(foundItem.ID, qtyToAdd, scannedBarcode: text);
					cboProduct.Text = "";
					cboProduct.BeginUpdate();
					cboProduct.Items.Clear();
					cboProduct.Items.AddRange(allItems.ToArray());
					cboProduct.SelectedIndex = 0;
					cboProduct.EndUpdate();
					txtProductCode?.Focus();
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

				string scanText = cboProduct.Text.Trim();
				ComboItem foundItem = null;

				if (res.IsScaleBarcode)
				{
					_pendingBarcodeWeight = res.WeightOrPrice;
					
					// 1) First check for exact ScalePLU match
					foreach (var ci in allItems)
					{
						if (ci.ID > 0 && !string.IsNullOrWhiteSpace(ci.ScalePLU) && (
							ci.ScalePLU == res.ItemCode || 
							ci.ScalePLU == res.TrimmedItemCode || 
							(int.TryParse(ci.ScalePLU, out int pluVal) && pluVal.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode)
						))
						{
							foundItem = ci;
							break;
						}
					}
					// 2) Fallback to ProductCode/ID if no ScalePLU matched
					if (foundItem == null)
					{
						foreach (var ci in allItems)
						{
							if (ci.ID > 0 && (
								ci.ID.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode || 
								ci.PartNumber == res.ItemCode ||
								(int.TryParse(ci.ProductCode, out int pCodeVal) && pCodeVal.ToString().PadLeft(AppConfig.BarcodeScaleItemCodeLength, '0') == res.ItemCode)
							))
							{
								foundItem = ci;
								break;
							}
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
						AddOrUpdateProduct(foundItem.ID, qtyToAdd, scannedBarcode: scanText);
						
						cboProduct.Text = "";
						cboProduct.BeginUpdate();
						cboProduct.Items.Clear();
						cboProduct.Items.AddRange(allItems.ToArray());
						cboProduct.SelectedIndex = 0;
						cboProduct.EndUpdate();
						txtProductCode?.Focus();
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
					cbo.Items.AddRange(list2.ToArray());
				}
				else
				{
					List<ComboItem> filtered = new List<ComboItem>();
					if (list2.Count > 0 && list2[0].ID == 0)
					{
						filtered.Add(list2[0]);
					}
					int count = 0;
					foreach (ComboItem item2 in list2)
					{
						if (item2.ID == 0) continue;
						if (item2.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
							(item2.ProductCode != null && item2.ProductCode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							(item2.PartNumber != null && item2.PartNumber.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							(item2.InternationalCode != null && item2.InternationalCode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							(item2.ClientCode != null && item2.ClientCode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							(item2.Phone != null && item2.Phone.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
							(item2.Phone2 != null && item2.Phone2.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
						{
							filtered.Add(item2);
							count++;
							if (count >= 100)
								break;
						}
					}
					cbo.Items.AddRange(filtered.ToArray());
				}
				cbo.EndUpdate();
				cbo.SelectionStart = text.Length;
				cbo.SelectionLength = 0;
				// لا تفتح القائمة إذا كانت الكتابة سريعة جداً (سكانر باركود)
				var timeSinceLastKey = (DateTime.Now - _lastKeyTime).TotalMilliseconds;
				bool isBarcodeScan = timeSinceLastKey <= BARCODE_INTERVAL_MS;
				if (!cbo.DroppedDown && !isBarcodeScan)
				{
					cbo.DroppedDown = true;
					Cursor.Current = Cursors.Default;
				}
			};
		}

		private void LoadCombos()
		{
			cboClient.Tag = null;
			cboProduct.Tag = null;
			_productCache.Clear();
			cboDriver.Tag = null;

			// FIX: تحميل كل أرصدة المخزون مرة واحدة بدلاً من رحلة DB لكل صنف
			_stockCache.Clear();
			var stockTable = InventoryDAL.GetStock();
			foreach (DataRow sRow in stockTable.Rows)
				_stockCache[(int)sRow["ProductID"]] = sRow["BookQty"] == DBNull.Value ? 0m : Convert.ToDecimal(sRow["BookQty"]);
			
			DataTable all = ClientCache.GetActive();
			cboClient.BeginUpdate();
			cboClient.Items.Clear();
			List<ComboItem> clientItems = new List<ComboItem>();
			clientItems.Add(new ComboItem(0, "-- اختر عميل --"));
			foreach (DataRow row in all.Rows)
			{
				var item = new ComboItem((int)row["ClientID"], row["ClientName"].ToString());
				item.ClientCode = row["ClientCode"] != DBNull.Value ? row["ClientCode"].ToString().Trim() : "";
				item.Phone = row["Phone"] != DBNull.Value ? row["Phone"].ToString().Trim() : "";
				item.Phone2 = row["Phone2"] != DBNull.Value ? row["Phone2"].ToString().Trim() : "";
				clientItems.Add(item);
			}
			cboClient.Items.AddRange(clientItems.ToArray());
			cboClient.DisplayMember = "Text";
			cboClient.Tag = clientItems;
			cboClient.SelectedIndex = 0;
			cboClient.EndUpdate();
			
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
                    if (byID != null && byID["DefaultPriceTier"] != DBNull.Value && !string.IsNullOrEmpty(byID["DefaultPriceTier"].ToString()))
                    {
                        string clientTier = byID["DefaultPriceTier"].ToString();
                        if (clientTier != _selectedTier)
                            SetTierButtons(clientTier); // تحديث التصميم فقط بدون سؤال
                    }
                    else
                    {
                        if (_selectedTier != "قطاعي")
                            SetTierButtons("قطاعي");
                    }

                    // تطبيق طريقة الدفع الافتراضية للعميل (كاش أو آجل)
                    if (byID != null && byID.Table.Columns.Contains("DefaultPaymentType") && byID["DefaultPaymentType"] != DBNull.Value)
                    {
                        string ptype = byID["DefaultPaymentType"].ToString();
                        if (string.Equals(ptype, "Cash", StringComparison.OrdinalIgnoreCase) || ptype == "كاش")
                        {
                            SetInvoiceType("Cash");
                        }
                        else if (string.Equals(ptype, "Credit", StringComparison.OrdinalIgnoreCase) || ptype == "آجل")
                        {
                            SetInvoiceType("Credit");
                        }
                    }

                    EvaluateClientFinancials(comboItem2.ID);
                    UpdateClientBalanceLabel(comboItem2.ID);
				}
                else
                {
                    if (_selectedTier != "قطاعي")
                        SetTierButtons("قطاعي");
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
                        lblClientCratesBalance.Text = "فوارغ العميل: 0 فارغ";
                    }
                }
			};
			
			DataTable drivers = EmployeeDAL.GetDrivers();
			cboDriver.BeginUpdate();
			cboDriver.Items.Clear();
			List<ComboItem> driverItems = new List<ComboItem>();
			driverItems.Add(new ComboItem(0, "-- اختر مندوب --"));
			foreach (DataRow row2 in drivers.Rows)
			{
				driverItems.Add(new ComboItem((int)row2["EmpID"], row2["EmpName"].ToString()));
			}
			cboDriver.Items.AddRange(driverItems.ToArray());
			cboDriver.DisplayMember = "Text";
			cboDriver.Tag = driverItems;
			cboDriver.SelectedIndex = 0;
			cboDriver.EndUpdate();
			
			DataTable all2 = ProductCache.GetActive();
			cboProduct.BeginUpdate();
			cboProduct.Items.Clear();
			List<ComboItem> productItems = new List<ComboItem>();
			productItems.Add(new ComboItem(0, "-- اختر صنف --"));
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
					itemOld.PartNumber = row3["PartNumber"]?.ToString().Trim() ?? "";
					itemOld.CarModel = row3["CarModel"]?.ToString().Trim() ?? "";
					itemOld.Brand = row3["Brand"]?.ToString().Trim() ?? "";
					itemOld.ShelfLocation = row3["ShelfLocation"]?.ToString().Trim() ?? "";
					itemOld.ProductCode = row3["ProductCode"]?.ToString().Trim() ?? "";
					itemOld.InternationalCode = row3["InternationalCode"]?.ToString().Trim() ?? "";
					itemOld.ScalePLU = row3.Table.Columns.Contains("ScalePLU") && row3["ScalePLU"] != DBNull.Value ? row3["ScalePLU"].ToString().Trim() : "";
					itemOld.IsService = row3.Table.Columns.Contains("IsService") && row3["IsService"] != DBNull.Value && Convert.ToBoolean(row3["IsService"]);
					itemOld.HasExpiry = row3.Table.Columns.Contains("HasExpiry") && row3["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row3["HasExpiry"]);
					itemOld.DefaultExpiryDays = row3.Table.Columns.Contains("DefaultExpiryDays") && row3["DefaultExpiryDays"] != DBNull.Value ? Convert.ToInt32(row3["DefaultExpiryDays"]) : (int?)null;
					itemOld.DefaultSaleUnit = row3.Table.Columns.Contains("DefaultSaleUnit") && row3["DefaultSaleUnit"] != DBNull.Value ? row3["DefaultSaleUnit"].ToString() : "";
					// وحدات متعددة
					itemOld.BaseUnitName = baseUnit;
					itemOld.Unit1Name = unit1Name; itemOld.Unit1SalePrice = unit1SP; itemOld.Unit1PurchasePrice = unit1PP; itemOld.Unit1Factor = 1m;
					itemOld.Unit2Name = unit2Name; itemOld.Unit2Factor = unit2Factor; itemOld.Unit2SalePrice = unit2SP; itemOld.Unit2PurchasePrice = unit2PP;
					itemOld.Unit3Factor = unit3Factor;
					productItems.Add(itemOld);

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
					itemPending.PartNumber = row3["PartNumber"]?.ToString().Trim() ?? "";
					itemPending.CarModel = row3["CarModel"]?.ToString().Trim() ?? "";
					itemPending.Brand = row3["Brand"]?.ToString().Trim() ?? "";
					itemPending.ShelfLocation = row3["ShelfLocation"]?.ToString().Trim() ?? "";
					itemPending.ProductCode = row3["ProductCode"]?.ToString().Trim() ?? "";
					itemPending.InternationalCode = row3["InternationalCode"]?.ToString().Trim() ?? "";
					itemPending.ScalePLU = row3.Table.Columns.Contains("ScalePLU") && row3["ScalePLU"] != DBNull.Value ? row3["ScalePLU"].ToString().Trim() : "";
					itemPending.IsService = row3.Table.Columns.Contains("IsService") && row3["IsService"] != DBNull.Value && Convert.ToBoolean(row3["IsService"]);
					itemPending.HasExpiry = row3.Table.Columns.Contains("HasExpiry") && row3["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row3["HasExpiry"]);
					itemPending.DefaultExpiryDays = row3.Table.Columns.Contains("DefaultExpiryDays") && row3["DefaultExpiryDays"] != DBNull.Value ? Convert.ToInt32(row3["DefaultExpiryDays"]) : (int?)null;
					itemPending.DefaultSaleUnit = row3.Table.Columns.Contains("DefaultSaleUnit") && row3["DefaultSaleUnit"] != DBNull.Value ? row3["DefaultSaleUnit"].ToString() : "";
					// وحدات متعددة
					itemPending.BaseUnitName = baseUnit;
					itemPending.Unit1Name = unit1Name; itemPending.Unit1SalePrice = unit1SP; itemPending.Unit1PurchasePrice = unit1PP; itemPending.Unit1Factor = 1m;
					itemPending.Unit2Name = unit2Name; itemPending.Unit2Factor = unit2Factor; itemPending.Unit2SalePrice = unit2SP; itemPending.Unit2PurchasePrice = unit2PP;
					itemPending.Unit3Factor = unit3Factor;
					productItems.Add(itemPending);
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
					comboItem.ProductSize = row3.Table.Columns.Contains("ProductSize") && row3["ProductSize"] != DBNull.Value ? row3["ProductSize"].ToString() : "";
					comboItem.Color = row3.Table.Columns.Contains("Color") && row3["Color"] != DBNull.Value ? row3["Color"].ToString() : "";
					comboItem.ShelfLocation = row3["ShelfLocation"]?.ToString() ?? "";
					comboItem.ProductCode = row3["ProductCode"]?.ToString() ?? "";
					comboItem.InternationalCode = row3["InternationalCode"]?.ToString() ?? "";
					comboItem.ScalePLU = row3.Table.Columns.Contains("ScalePLU") && row3["ScalePLU"] != DBNull.Value ? row3["ScalePLU"].ToString().Trim() : "";
					comboItem.IsService = row3.Table.Columns.Contains("IsService") && row3["IsService"] != DBNull.Value && Convert.ToBoolean(row3["IsService"]);
					comboItem.HasExpiry = row3.Table.Columns.Contains("HasExpiry") && row3["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row3["HasExpiry"]);
					comboItem.DefaultExpiryDays = row3.Table.Columns.Contains("DefaultExpiryDays") && row3["DefaultExpiryDays"] != DBNull.Value ? Convert.ToInt32(row3["DefaultExpiryDays"]) : (int?)null;
					comboItem.DefaultSaleUnit = row3.Table.Columns.Contains("DefaultSaleUnit") && row3["DefaultSaleUnit"] != DBNull.Value ? row3["DefaultSaleUnit"].ToString() : "";
					// وحدات متعددة
					comboItem.BaseUnitName = baseUnit;
					comboItem.Unit1Name = unit1Name; comboItem.Unit1SalePrice = unit1SP; comboItem.Unit1PurchasePrice = unit1PP; comboItem.Unit1Factor = 1m;
					comboItem.Unit2Name = unit2Name; comboItem.Unit2Factor = unit2Factor; comboItem.Unit2SalePrice = unit2SP; comboItem.Unit2PurchasePrice = unit2PP;
					comboItem.Unit3Factor = unit3Factor;
					productItems.Add(comboItem);
				}
			}
			_productCache = productItems;
			// نحدّث cboProduct أيضاً للتوافق مع الكود القديم
			cboProduct.BeginUpdate();
			cboProduct.Items.Clear();
			cboProduct.Items.AddRange(productItems.ToArray());
			cboProduct.DisplayMember = "Text";
			cboProduct.Tag = productItems;
			cboProduct.SelectedIndex = 0;
			cboProduct.EndUpdate();
			// لا نضيف SelectedIndexChanged - cboProduct مخفي
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
				cboWarehouse.SelectedIndexChanged += (s, e) =>
				{
					foreach (var item in _items)
					{
						item.StockQty = InventoryDAL.GetProductStock(item.ProductID, GetSelectedWarehouseID());
					}
					RefreshGrid();
				};
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
				int defaultSafeID = Session.DefaultSafeID ?? Session.GetDefaultSafeID();

				foreach (DataRow row in safes.Rows)
				{
					int accID = Convert.ToInt32(row["AccountID"]);
					if (allowedSafes != null && !allowedSafes.Contains(accID))
					{
						continue; // Filter out if not allowed
					}

					string safeName = row["AccountName"].ToString().Replace(" / الدرج", "").Replace("/ الدرج", "").Replace("/الدرج", "").Replace(" / درج", "").Trim();
					var comboItem = new ComboItem(accID, safeName);
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
			UpdateShiftSummaryLabel();
		}

		private void UpdateShiftSummaryLabel()
		{
			if (lblShiftSummaryBar == null) return;
			try
			{
				DbHelper.EnsureShiftSchema();
				DataTable dt;
				try
				{
					dt = DbHelper.Query(
						@"SELECT TOP 1 s.ShiftID, s.OpenTime, s.OpeningCash, s.SafeAccountID, e.EmpName, sa.AccountName AS SafeName
						  FROM Shifts s
						  JOIN Employees e ON s.OpenedBy = e.EmpID
						  LEFT JOIN SafeAccounts sa ON s.SafeAccountID = sa.AccountID
						  WHERE s.Status = 'Open' ORDER BY s.OpenTime DESC");
				}
				catch
				{
					dt = DbHelper.Query(
						@"SELECT TOP 1 s.ShiftID, s.OpenTime, s.OpeningCash, NULL AS SafeAccountID, e.EmpName, NULL AS SafeName
						  FROM Shifts s
						  JOIN Employees e ON s.OpenedBy = e.EmpID
						  WHERE s.Status = 'Open' ORDER BY s.OpenTime DESC");
				}

				if (dt.Rows.Count > 0)
				{
					DataRow r = dt.Rows[0];
					int shiftId = Convert.ToInt32(r["ShiftID"]);
					Session.CurrentShiftID = shiftId;
					DateTime openTime = Convert.ToDateTime(r["OpenTime"]);
					decimal openingCash = Convert.ToDecimal(r["OpeningCash"]);
					string emp = r["EmpName"].ToString();
					string safe = r["SafeName"] != DBNull.Value ? r["SafeName"].ToString() : "درج الكاشير";

					if (r.Table.Columns.Contains("SafeAccountID") && r["SafeAccountID"] != DBNull.Value)
					{
						int shiftSafeID = Convert.ToInt32(r["SafeAccountID"]);
						for (int i = 0; i < cboSafeAccount.Items.Count; i++)
						{
							if (cboSafeAccount.Items[i] is ComboItem ci && ci.ID == shiftSafeID)
							{
								if (cboSafeAccount.SelectedIndex != i) cboSafeAccount.SelectedIndex = i;
								break;
							}
						}
					}

					lblShiftSummaryBar.Text = $"🟢 وردية #{shiftId} | 👤 {emp} | 💵 فتح: {openingCash:N0}ج | 🏦 {safe}";
					lblShiftSummaryBar.ForeColor = Color.FromArgb(74, 222, 128);
				}
				else
				{
					Session.CurrentShiftID = null;
					lblShiftSummaryBar.Text = "🔴 لا توجد وردية مفتوحة (اضغط هنا لفتح وردية)";
					lblShiftSummaryBar.ForeColor = Color.FromArgb(248, 113, 113);
				}
			}
			catch { }
		}

		private void LoadQuickItems()
		{
			flowQuickItems.Controls.Clear();
			try
			{
				DataTable dt = ProductDAL.GetQuickItems();
				foreach (DataRow row in dt.Rows)
				{
					int id = Convert.ToInt32(row["ProductID"]);
					string name = row["ProductName"].ToString();
					decimal price = Convert.ToDecimal(row["SalePrice"]);
					bool isService = Convert.ToBoolean(row["IsService"]);

					Button btn = new Button
					{
						Width = 90,
						Height = 55,
						FlatStyle = FlatStyle.Flat,
						BackColor = isService ? Color.FromArgb(45, 55, 72) : Theme.Primary,
						ForeColor = Color.White,
						Font = new Font(Theme.FontMain.FontFamily, 8.5f, FontStyle.Bold),
						Cursor = Cursors.Hand,
						Text = $"{name}\n{price:N2} ج",
						Margin = new Padding(3),
						Tag = id
					};
					btn.FlatAppearance.BorderSize = 0;
					btn.Click += (s, e) =>
					{
						AddOrUpdateProduct(id, 1.00m);
					};
					flowQuickItems.Controls.Add(btn);
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

			Color clrRetailOn    = Color.FromArgb(0, 136, 255);
			Color clrSemiOn      = Color.FromArgb(155, 38, 224);
			Color clrWholesaleOn = Theme.Accent;
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
			Color inactiveBg = Color.FromArgb(51, 65, 85);
			Color inactiveFg = Color.FromArgb(203, 213, 225);

			if (btnTypeCash != null)
			{
				btnTypeCash.BackColor = ((_invoiceType == "Cash") ? Color.FromArgb(16, 185, 129) : inactiveBg);
				btnTypeCash.ForeColor = ((_invoiceType == "Cash") ? Color.White : inactiveFg);
			}
			if (btnTypeCredit != null)
			{
				btnTypeCredit.BackColor = ((_invoiceType == "Credit") ? Color.FromArgb(37, 99, 235) : inactiveBg);
				btnTypeCredit.ForeColor = ((_invoiceType == "Credit") ? Color.White : inactiveFg);
			}
			if (btnTypeVisa != null)
			{
				btnTypeVisa.BackColor = ((_invoiceType == "Visa") ? Color.FromArgb(142, 68, 173) : inactiveBg);
				btnTypeVisa.ForeColor = ((_invoiceType == "Visa") ? Color.White : inactiveFg);
			}
			if (btnTypeInstallment != null)
			{
				btnTypeInstallment.BackColor = ((_invoiceType == "Installment") ? Color.FromArgb(14, 165, 233) : inactiveBg);
				btnTypeInstallment.ForeColor = ((_invoiceType == "Installment") ? Color.White : inactiveFg);
			}
			if (btnTypeDriverLoad != null)
			{
				btnTypeDriverLoad.BackColor = ((_invoiceType == "DriverLoad") ? Color.FromArgb(217, 119, 6) : inactiveBg);
				btnTypeDriverLoad.ForeColor = ((_invoiceType == "DriverLoad") ? Color.White : inactiveFg);
			}
			ToggleType();
		}

		private string GetDefaultAllowedInvoiceType()
		{
			if (Session.IsAdmin) return "Cash";
			if (Session.CanSellCash) return "Cash";
			if (Session.CanSellVisa) return "Visa";
			if (Session.CanSellCredit) return "Credit";
			if (Session.CanSellDriverLoad) return "DriverLoad";
			if (Session.CanSellInstallment) return "Installment";
			return "Cash"; // Fallback
		}

		private void ApplyInvoiceTypePermissions()
		{
			if (Session.IsAdmin)
			{
				if (nudShippingCharge != null) nudShippingCharge.Enabled = true;
				return;
			}

			btnTypeCash.Visible = Session.CanSellCash;
			btnTypeCredit.Visible = Session.CanSellCredit;
			if (btnTypeVisa != null) btnTypeVisa.Visible = Session.IsAdmin || Session.CanSellVisa;
			btnTypeDriverLoad.Visible = Session.CanSellDriverLoad;
			btnTypeInstallment.Visible = Session.CanSellInstallment;

			if (nudShippingCharge != null)
			{
				nudShippingCharge.Enabled = Session.CanEditShippingCharge;
			}
		}

		private void ToggleType()
		{
			bool flag = _invoiceType == "Credit" || _invoiceType == "Installment";
			bool flag2 = _invoiceType == "DriverLoad";
			bool flag3 = _invoiceType == "Cash" || _invoiceType == "Visa";
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
                lblClientCratesBalance.Text = "فوارغ العميل: " + cratesBal + " فارغ";
            }
        }

		private void BtnSearchProduct_Click(object sender, EventArgs e)
		{
			int? warehouseID = null;
			if (cboWarehouse.SelectedItem is ComboItem wci) warehouseID = wci.ID;

			try
			{
				int? saleClientID = (cboClient != null && cboClient.SelectedItem is ComboItem ciClient && ciClient.ID > 0) ? ciClient.ID : (int?)null;
				_searchSessionActive = true;
				while (true)
				{
					using FrmProductSearch frmProductSearch = new FrmProductSearch(warehouseID, isPurchaseMode: false, defaultShowZeroStock: false, clientID: saleClientID);
					frmProductSearch.ShowDialog();

					if (frmProductSearch.DialogResult == DialogResult.OK)
					{
						decimal qty = frmProductSearch.SelectedQuantity > 0 ? frmProductSearch.SelectedQuantity : 1.00m;
						decimal price = frmProductSearch.SelectedSalePrice > 0 ? frmProductSearch.SelectedSalePrice : frmProductSearch.SelectedPrice;
						AddOrUpdateProduct(frmProductSearch.SelectedProductID, qty, price, false, frmProductSearch.SelectedUnitName);
						FocusQtyCellInGrid(frmProductSearch.SelectedProductID);
						if (frmProductSearch.SelectedBatchID.HasValue)
						{
							if (frmProductSearch.SelectedExpiryDate.HasValue && frmProductSearch.SelectedExpiryDate.Value < DateTime.Today && !AppConfig.AllowSellExpired)
							{
								MessageBox.Show("❌ عجز: هذا الصنف منتهي الصلاحية ولا يسمح النظام ببيعه حسب الإعدادات الحالية!", "تنبيه الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Error);
								var lastItem = _items.FindLast(i => i.ProductID == frmProductSearch.SelectedProductID);
								if (lastItem != null)
								{
									_items.Remove(lastItem);
									RefreshGrid();
								}
							}
							else
							{
								var lastItem2 = _items.FindLast(i => i.ProductID == frmProductSearch.SelectedProductID);
								if (lastItem2 != null)
								{
									lastItem2.BatchID = frmProductSearch.SelectedBatchID;
									lastItem2.ExpiryDate = frmProductSearch.SelectedExpiryDate;
									RefreshGrid();
								}
							}
						}
						// فتح الشاشة مرة أخرى لاختيار صنف تاني
						continue;
					}
					else
					{
						// المستخدم ضغط إلغاء → نخرج من الحلقة
						break;
					}
				}
			}
			catch { }
			finally
			{
				_searchSessionActive = false;
				// إرجاع الفوكس للكومبو أو الجدول
				this.BeginInvoke((MethodInvoker)delegate { txtProductCode.Clear(); txtProductCode.Focus(); });
			}
		}

		private void FocusQtyCellInGrid(int productID)
		{
			this.BeginInvoke((MethodInvoker)delegate
			{
				try
				{
					for (int i = 0; i < dgItems.Rows.Count; i++)
					{
						var row = dgItems.Rows[i];
						if (row.Tag is SaleItemDTO dto && dto.ProductID == productID)
						{
							dgItems.Focus();
							if (dgItems.Columns.Contains("Quantity"))
							{
								dgItems.CurrentCell = row.Cells["Quantity"];
								dgItems.BeginEdit(true);
							}
							break;
						}
					}
				}
				catch { }
			});
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
			else if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "UnitName")
			{
				dgItems.CurrentCell = dgItems.Rows[e.RowIndex].Cells[e.ColumnIndex];
				dgItems.BeginEdit(true);
			}
		}

		private void DgItems_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
		{
			if (dgItems.CurrentCell != null && dgItems.CurrentCell.OwningColumn.Name == "UnitName")
			{
				if (e.Control is ComboBox comboBox)
				{
					comboBox.DroppedDown = true;
				}
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
						int matchedUnit = Convert.ToInt32(dt.Rows[0]["MatchedUnit"]);
						decimal price = 0m;
						string unitName = "";
						if (matchedUnit == 1)
						{
							price = dt.Rows[0]["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["Unit1SalePrice"]) : 0m;
							unitName = dt.Rows[0]["Unit1Name"]?.ToString();
						}
						else if (matchedUnit == 2)
						{
							price = dt.Rows[0]["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["Unit2SalePrice"]) : 0m;
							unitName = dt.Rows[0]["Unit2Name"]?.ToString();
						}
						else
						{
							price = Convert.ToDecimal(dt.Rows[0]["SalePrice"]);
							unitName = dt.Rows[0]["Unit"]?.ToString();
						}
						if (price <= 0) price = Convert.ToDecimal(dt.Rows[0]["SalePrice"]);
						if (string.IsNullOrEmpty(unitName)) unitName = dt.Rows[0]["Unit"]?.ToString();

						// حذف السطر المعلق ثم إضافة الصنف الحقيقي
						if (rowIdx >= 0 && rowIdx < dgItems.Rows.Count)
							dgItems.Rows.RemoveAt(rowIdx);
						_pendingRowIdx = -1;
						decimal itemQty = dt.Rows[0].Table.Columns.Contains("ParsedWeight") && dt.Rows[0]["ParsedWeight"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["ParsedWeight"]) : 1.00m;
						AddOrUpdateProduct(productID, itemQty, price > 0 ? price : (decimal?)null, false, unitName, scannedBarcode: code);
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
					if (!CheckSaleItemStock(saleItemDTO, result, out string err))
					{
						MessageBox.Show(err, "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dataGridViewRow.Cells["Quantity"].Value = saleItemDTO.Quantity.ToString("F2");
						return;
					}

					decimal delta = result - saleItemDTO.Quantity;
					decimal? manualPrice = saleItemDTO.UnitPrice;
					AddOrUpdateProduct(saleItemDTO.ProductID, delta, manualPrice, true, saleItemDTO.UnitName);
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
			else if (dgItems.Columns[e.ColumnIndex].Name == "IMEI")
			{
				saleItemDTO.IMEI = dataGridViewRow.Cells["IMEI"].Value?.ToString() ?? "";
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
			int clientID = (cboClient != null && cboClient.SelectedItem is ComboItem ci) ? ci.ID : 0;
			foreach (SaleItemDTO item in _items)
			{
				decimal costTotal = item.PurchasePrice * item.Quantity;
				decimal? lastPrice = (clientID > 0) ? SaleDAL.GetLastPriceForClient(item.ProductID, clientID) : null;
				string lastPriceStr = lastPrice.HasValue ? lastPrice.Value.ToString("N2") : "-";

				int rIndex = dgItems.Rows.Add(
					item.ProductCode, // CodeEntry - عرض الكود المحلي للصنف
					item.ProductName,
					item.ProductSize,
					item.Color,
					item.PartNumber,
					item.CarModel,
					item.Brand,
					item.ShelfLocation,
					item.StockQty.ToString("F2"),
					null,              // UnitName - سيُعيَّن بالكود أدناه
					item.Quantity.ToString("F2"),
					item.UnitPrice.ToString("F2"),
					lastPriceStr,
					item.DiscountPct.ToString("F2"),
					item.DiscountAmt.ToString("F2"),
					item.TotalPrice.ToString("F2"),
					item.ExpiryDate?.ToString("yyyy-MM-dd") ?? "",
					item.IMEI,
					item.PurchasePrice.ToString("F2"),
					costTotal.ToString("F2")
				);
				// عمود الكود للسطور المضافة للقراءة فقط (ليس للتعديل)
				dgItems.Rows[rIndex].Cells["CodeEntry"].ReadOnly = true;

				// ─── تهيئة ComboBox السيريال المتاح ─────────────────────────────────────
				if (dgItems.Columns.Contains("IMEI"))
				{
					var availableSerials = PurchaseDAL.GetAvailableSerialsForProduct(item.ProductID);
					if (availableSerials != null && availableSerials.Count > 0)
					{
						var comboCell = new DataGridViewComboBoxCell();
						comboCell.Items.Add("");
						foreach (var s in availableSerials)
						{
							comboCell.Items.Add(s);
						}
						dgItems.Rows[rIndex].Cells["IMEI"] = comboCell;
						if (!string.IsNullOrEmpty(item.IMEI) && comboCell.Items.Contains(item.IMEI))
						{
							comboCell.Value = item.IMEI;
						}
						else if (comboCell.Items.Count > 1)
						{
							comboCell.Value = comboCell.Items[1];
							item.IMEI = comboCell.Value.ToString();
						}
					}
				}

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

		private void AddOrUpdateProduct(int productID, decimal qtyToAdd, decimal? manualPrice = null, bool deferRefresh = false, string unitName = null, string scannedBarcode = null)
		{
			ComboItem product = null;
			// البحث في _productCache أولاً
			foreach (var ci in _productCache)
			{
				if (ci.ID == productID) { product = ci; break; }
			}
			// Fallback: بحث في cboProduct.Items (للتوافق)
			if (product == null)
				foreach (var item in cboProduct.Items)
				{
					if (item is ComboItem ci && ci.ID == productID) { product = ci; break; }
				}
			// Fallback: إذا لم يكن الصنف في الكومبو، نحمله مباشرة من قاعدة البيانات
			if (product == null)
			{
				try
				{
					var pRow = ProductDAL.GetByID(productID);
					if (pRow != null)
					{
						string name = pRow["ProductName"].ToString();
						decimal price = pRow["SalePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["SalePrice"]) : 0m;
						decimal purchasePrice = pRow["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["PurchasePrice"]) : 0m;
						decimal minStock = pRow["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(pRow["MinStockLimit"]) : 0m;

						product = new ComboItem(productID, name, $"{name} ({price:N2})", price, minStock, purchasePrice);
						product.ProductCode = pRow["ProductCode"]?.ToString() ?? "";
						product.InternationalCode = pRow["InternationalCode"]?.ToString() ?? "";
						product.ScalePLU = pRow.Table.Columns.Contains("ScalePLU") && pRow["ScalePLU"] != DBNull.Value ? pRow["ScalePLU"].ToString().Trim() : "";
						product.IsService = pRow.Table.Columns.Contains("IsService") && pRow["IsService"] != DBNull.Value && Convert.ToBoolean(pRow["IsService"]);
						product.HasExpiry = pRow.Table.Columns.Contains("HasExpiry") && pRow["HasExpiry"] != DBNull.Value && Convert.ToBoolean(pRow["HasExpiry"]);
						product.DefaultExpiryDays = pRow.Table.Columns.Contains("DefaultExpiryDays") && pRow["DefaultExpiryDays"] != DBNull.Value ? Convert.ToInt32(pRow["DefaultExpiryDays"]) : (int?)null;
						product.DefaultSaleUnit = pRow.Table.Columns.Contains("DefaultSaleUnit") && pRow["DefaultSaleUnit"] != DBNull.Value ? pRow["DefaultSaleUnit"].ToString() : "";
						product.BaseUnitName = pRow.Table.Columns.Contains("Unit") && pRow["Unit"] != DBNull.Value ? pRow["Unit"].ToString() : "";
						product.Unit1Name = pRow.Table.Columns.Contains("Unit1Name") && pRow["Unit1Name"] != DBNull.Value ? pRow["Unit1Name"].ToString() : null;
						product.Unit1SalePrice = pRow.Table.Columns.Contains("Unit1SalePrice") && pRow["Unit1SalePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["Unit1SalePrice"]) : 0m;
						product.Unit1PurchasePrice = pRow.Table.Columns.Contains("Unit1PurchasePrice") && pRow["Unit1PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["Unit1PurchasePrice"]) : 0m;
						product.Unit1Factor = 1m;
						product.Unit2Name = pRow.Table.Columns.Contains("Unit2Name") && pRow["Unit2Name"] != DBNull.Value ? pRow["Unit2Name"].ToString() : null;
						product.Unit2Factor = pRow.Table.Columns.Contains("Unit2Factor") && pRow["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(pRow["Unit2Factor"]) : 1m;
						product.Unit2SalePrice = pRow.Table.Columns.Contains("Unit2SalePrice") && pRow["Unit2SalePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["Unit2SalePrice"]) : 0m;
						product.Unit2PurchasePrice = pRow.Table.Columns.Contains("Unit2PurchasePrice") && pRow["Unit2PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(pRow["Unit2PurchasePrice"]) : 0m;
						product.Unit3Factor = pRow.Table.Columns.Contains("Unit3Factor") && pRow["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(pRow["Unit3Factor"]) : 1m;
						product.PartNumber = pRow["PartNumber"]?.ToString() ?? "";
						product.ShelfLocation = pRow["ShelfLocation"]?.ToString() ?? "";

						// أضف الصنف للقائمة لتجنب التحميل مرة أخرى
						cboProduct.Items.Add(product);
						if (cboProduct.Tag is List<ComboItem> tagList)
							tagList.Add(product);
					}
				}
				catch (Exception ex)
				{
					AppLogger.Error("AddOrUpdateProduct fallback load", ex);
				}
			}
			if (product == null) return;

			decimal stock = InventoryDAL.GetProductStock(productID, GetSelectedWarehouseID());
			// التحقق من IsService مباشرة من DB لضمان دقة القيمة
			bool isServiceDB = product.IsService;
			if (!isServiceDB)
			{
				var isServiceVal = DbHelper.Scalar("SELECT IsService FROM Products WHERE ProductID=@pid", DbHelper.P("@pid", productID));
				isServiceDB = isServiceVal != null && isServiceVal != DBNull.Value && Convert.ToBoolean(isServiceVal);
			}
			if (stock <= 0 && !isServiceDB)
			{
				MessageBox.Show($"❌ عجز: الصنف '{product.Name}' ليس لديه رصيد كافٍ في المخزن حالياً (الرصيد الحالي: 0)!", "رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				if (deferRefresh) this.BeginInvoke((MethodInvoker)delegate { RefreshGrid(); });
				else RefreshGrid();
				return;
			}

			int? batchID = null;
			DateTime? expiryDate = null;
			if (product.HasExpiry)
			{
				int whId = 1;
				if (cboWarehouse.SelectedItem is ComboItem wci && wci.ID > 0) whId = wci.ID;

				var batches = DbHelper.Query(@"
					SELECT BatchID, ExpiryDate, Quantity 
					FROM ProductBatches 
					WHERE ProductID = @pid AND WarehouseID = @wid AND Quantity > 0 
					ORDER BY ExpiryDate ASC, BatchID ASC",
					DbHelper.P("@pid", product.ID), DbHelper.P("@wid", whId));
				
				if (batches.Rows.Count > 0)
				{
					var oldestBatch = batches.Rows[0];
					int oldestBatchID = Convert.ToInt32(oldestBatch["BatchID"]);
					DateTime? oldestExpiry = oldestBatch["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(oldestBatch["ExpiryDate"]) : (DateTime?)null;

					bool isInternational = false;
					if (!string.IsNullOrEmpty(scannedBarcode) && product.InternationalCode != null)
					{
						isInternational = MatchBarcode(product.InternationalCode, scannedBarcode);
					}

					if (isInternational)
					{
						batchID = oldestBatchID;
						expiryDate = oldestExpiry;
					}
					else
					{
						if (oldestExpiry.HasValue)
						{
							var res = MessageBox.Show("يوجد تاريخ أقرب سينتهي، هل تريد بيعه أولاً؟", "تنبيه تاريخ الصلاحية", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
							if (res == DialogResult.Yes)
							{
								batchID = oldestBatchID;
								expiryDate = oldestExpiry;
							}
						}
					}
				}
			}

			if (expiryDate.HasValue && expiryDate.Value < DateTime.Today && !AppConfig.AllowSellExpired)
			{
				MessageBox.Show("❌ عجز: هذا الصنف منتهي الصلاحية ولا يسمح النظام ببيعه حسب الإعدادات الحالية!", "تنبيه الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Error);
				if (deferRefresh) this.BeginInvoke((MethodInvoker)delegate { RefreshGrid(); });
				else RefreshGrid();
				return;
			}

			if (product.HasExpiry)
			{
				decimal targetPrice = manualPrice ?? product.Price;
				SaleItemDTO existingRow = null;
				foreach (var item in _items)
				{
					if (item.ProductID == productID && 
						item.UnitPrice == targetPrice &&
						item.BatchID == batchID && 
						(unitName == null || string.Equals(item.UnitName, unitName, StringComparison.OrdinalIgnoreCase)))
					{
						existingRow = item;
						break;
					}
				}
				decimal newQty = (existingRow != null ? existingRow.Quantity : 0m) + qtyToAdd;

				var tempItem = existingRow ?? CreateSaleItemDTO(product, qtyToAdd, targetPrice, stock, unitName, batchID, expiryDate);
				if (qtyToAdd > 0 && !CheckSaleItemStock(tempItem, newQty, out string err))
				{
					MessageBox.Show(err, "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
						if (manualPrice.HasValue) existingRow.UnitPrice = manualPrice.Value;
					}
				}
				else
				{
					if (qtyToAdd > 0)
					{
						_items.Add(tempItem);
					}
				}
			}
			else
			{
				decimal targetPrice = manualPrice ?? product.Price;
				SaleItemDTO existingRow = null;
				foreach (var item in _items)
				{
					if (item.ProductID == productID && 
						item.UnitPrice == targetPrice &&
						(unitName == null || string.Equals(item.UnitName, unitName, StringComparison.OrdinalIgnoreCase)))
					{
						existingRow = item;
						break;
					}
				}
				decimal newQty = (existingRow != null ? existingRow.Quantity : 0m) + qtyToAdd;

				var tempItem = existingRow ?? CreateSaleItemDTO(product, qtyToAdd, targetPrice, stock, unitName, batchID, expiryDate);
				if (qtyToAdd > 0 && !CheckSaleItemStock(tempItem, newQty, out string err))
				{
					MessageBox.Show(err, "تنبيه - رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					if (deferRefresh) this.BeginInvoke((MethodInvoker)delegate { RefreshGrid(); });
					else RefreshGrid();
					return;
				}

				decimal oldPrice = product.Price;
				decimal newPrice = product.PendingSalePrice;
				decimal threshold = product.PendingQtyThreshold;
				bool hasPendingPrice = newPrice > 0 && threshold > 0;

				if (hasPendingPrice)
				{
					List<SaleItemDTO> existingRows = new List<SaleItemDTO>();
					foreach (var item in _items)
					{
						if (item.ProductID == productID)
							existingRows.Add(item);
					}
					foreach (var row in existingRows)
					{
						_items.Remove(row);
					}

					decimal totalQty = (existingRow != null ? existingRow.Quantity : 0m) + qtyToAdd;
					decimal oldQtyAvailable = Math.Max(0m, Math.Min(stock, threshold));
					decimal qtyOld = Math.Min(totalQty, oldQtyAvailable);
					decimal qtyNew = Math.Max(0m, totalQty - oldQtyAvailable);

					if (qtyOld > 0)
					{
						_items.Add(CreateSaleItemDTO(product, qtyOld, oldPrice, stock, unitName, batchID, expiryDate));
					}
					if (qtyNew > 0)
					{
						_items.Add(CreateSaleItemDTO(product, qtyNew, newPrice, stock, unitName, batchID, expiryDate));
					}
				}
				else
				{
					if (existingRow != null)
					{
						if (newQty <= 0)
						{
							_items.Remove(existingRow);
						}
						else
						{
							existingRow.Quantity = newQty;
							if (manualPrice.HasValue) existingRow.UnitPrice = manualPrice.Value;
						}
					}
					else
					{
						if (qtyToAdd > 0)
						{
							_items.Add(tempItem);
						}
					}
				}
			}

			if (deferRefresh) this.BeginInvoke((MethodInvoker)delegate { RefreshGrid(); });
			else RefreshGrid();
		}

		private bool CheckSaleItemStock(SaleItemDTO item, decimal newQty, out string err)
		{
			err = "";
			bool isSrv = item.IsService;
			if (!isSrv)
			{
				var isSrvVal = DbHelper.Scalar("SELECT IsService FROM Products WHERE ProductID=@pid", DbHelper.P("@pid", item.ProductID));
				isSrv = isSrvVal != null && isSrvVal != DBNull.Value && Convert.ToBoolean(isSrvVal);
			}
			if (isSrv) return true;

			decimal reqQtyInFactor = newQty * item.Factor;
			
			int warehouseID = 1;
			if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem wci && wci.ID > 0)
			{
				warehouseID = wci.ID;
			}

			if (item.BatchID.HasValue)
			{
				var qtyVal = DbHelper.Scalar("SELECT Quantity FROM ProductBatches WHERE BatchID=@bid", DbHelper.P("@bid", item.BatchID.Value));
				decimal dbQty = qtyVal != null && qtyVal != DBNull.Value ? Convert.ToDecimal(qtyVal) : 0m;

				// If editing an existing invoice, add the original quantity of this item
				if (_editSaleID > 0)
				{
					var origQtyVal = DbHelper.Scalar("SELECT Quantity * Factor FROM SaleItems WHERE SaleID=@sid AND ProductID=@pid AND BatchID=@bid",
						DbHelper.P("@sid", _editSaleID), DbHelper.P("@pid", item.ProductID), DbHelper.P("@bid", item.BatchID.Value));
					if (origQtyVal != null && origQtyVal != DBNull.Value)
					{
						dbQty += Convert.ToDecimal(origQtyVal);
					}
				}

				if (reqQtyInFactor > dbQty)
				{
					err = $"❌ عجز: الكمية المطلوبة ({reqQtyInFactor:N2}) أكبر من الكمية المتاحة في تشغيلية الصلاحية المحددة ({dbQty:N2})!";
					return false;
				}
			}
			else
			{
				decimal dbQty = InventoryDAL.GetProductStock(item.ProductID, warehouseID);

				// If editing an existing invoice, add the original quantity
				if (_editSaleID > 0)
				{
					var origQtyVal = DbHelper.Scalar("SELECT Quantity * Factor FROM SaleItems WHERE SaleID=@sid AND ProductID=@pid AND (BatchID IS NULL OR BatchID = 0)",
						DbHelper.P("@sid", _editSaleID), DbHelper.P("@pid", item.ProductID));
					if (origQtyVal != null && origQtyVal != DBNull.Value)
					{
						dbQty += Convert.ToDecimal(origQtyVal);
					}
				}

				if (reqQtyInFactor > dbQty)
				{
					err = $"❌ عجز: الكمية المطلوبة ({reqQtyInFactor:N2}) أكبر من الكمية المتاحة في المخزن حالياً ({dbQty:N2})!";
					return false;
				}
			}
			return true;
		}

		private SaleItemDTO CreateSaleItemDTO(ComboItem product, decimal qty, decimal price, decimal stock, string unitName = null, int? batchID = null, DateTime? expiryDate = null)
		{
			string selectedUnit = unitName;
			decimal factor = 1m;

			if (string.IsNullOrEmpty(selectedUnit))
			{
				string defUnit = product.DefaultSaleUnit;
				if (string.IsNullOrEmpty(defUnit)) defUnit = "الكبرى";

				if (defUnit == "الوسطى" && !string.IsNullOrEmpty(product.Unit2Name))
				{
					selectedUnit = product.Unit2Name;
				}
				else if (defUnit == "الصغرى" && !string.IsNullOrEmpty(product.Unit1Name))
				{
					selectedUnit = product.Unit1Name;
				}
				else // "الكبرى" or default
				{
					selectedUnit = !string.IsNullOrEmpty(product.BaseUnitName) ? product.BaseUnitName : product.Unit1Name;
				}
			}

			decimal purchasePrice = product.PurchasePrice;

			if (!string.IsNullOrEmpty(selectedUnit))
			{
				if (!string.IsNullOrEmpty(product.Unit2Name) && selectedUnit == product.Unit2Name)
				{
					factor = product.Unit2Factor > 0 ? product.Unit2Factor : 1m;
					if (product.Unit2PurchasePrice > 0) purchasePrice = product.Unit2PurchasePrice;
				}
				else if (!string.IsNullOrEmpty(product.Unit1Name) && selectedUnit == product.Unit1Name)
				{
					factor = 1m;
					if (product.Unit1PurchasePrice > 0) purchasePrice = product.Unit1PurchasePrice;
				}
				else if (!string.IsNullOrEmpty(product.BaseUnitName) && selectedUnit == product.BaseUnitName)
				{
					factor = (product.Unit3Factor > 0 ? product.Unit3Factor : 1m) * (product.Unit2Factor > 0 ? product.Unit2Factor : 1m);
				}
			}

			return new SaleItemDTO
			{
				ProductID = product.ID,
				ProductName = product.Name,
				Quantity = qty,
				UnitPrice = price,
				StockQty = stock,
				MinStockLimit = product.MinStockLimit,
				PurchasePrice = purchasePrice,
				PartNumber = product.PartNumber,
				CarModel = product.CarModel,
				Brand = product.Brand,
				ProductSize = product.ProductSize,
				Color = product.Color,
				ShelfLocation = product.ShelfLocation,
				ProductCode = product.ProductCode,
				IsService = product.IsService,
				UnitName = selectedUnit,
				Factor = factor,
				BatchID = batchID,
				ExpiryDate = expiryDate
			};
		}

		/// <summary>
		/// يجلب بيانات الوحدات المتعددة للصنف من ComboItem (أو يستعلم إذا لم يكن في الـ cache)
		/// </summary>
		private ComboItem GetProductComboItem(int productID)
		{
			// بحث في _productCache
			foreach (var ci in _productCache)
				if (ci.ID == productID) return ci;
			// بحث في cboProduct.Items كـ fallback
			foreach (var obj in cboProduct.Items)
				if (obj is ComboItem ci2 && ci2.ID == productID) return ci2;
			if (cboProduct.Tag is List<ComboItem> all)
				foreach (var ci3 in all)
					if (ci3.ID == productID) return ci3;
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

			decimal shippingVal = nudShippingCharge != null ? nudShippingCharge.Value : 0m;
			decimal net = Math.Max(0m, gross - discountAmt) + shippingVal;
			if (lblNetVal != null)
			{
				lblNetVal.Text = net.ToString("N2") + " ج";
			}

			if (lblItemCountVal != null)
			{
				lblItemCountVal.Text = _items.Count.ToString();
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
				         COALESCE(s.ShippingCharge, 0.0) AS ShippingCharge,
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

			// الشحن
			if (nudShippingCharge != null)
			{
				nudShippingCharge.Value = row.Table.Columns.Contains("ShippingCharge") && row["ShippingCharge"] != DBNull.Value
					? Convert.ToDecimal(row["ShippingCharge"])
					: 0m;
			}

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
					Factor      = iRow.Table.Columns.Contains("Factor")   && iRow["Factor"]   != DBNull.Value ? Convert.ToDecimal(iRow["Factor"]) : 1m,
					IMEI        = iRow.Table.Columns.Contains("IMEI")     && iRow["IMEI"]     != DBNull.Value ? iRow["IMEI"].ToString() : ""
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

		public void LoadFromPriceQuote(int quoteID, int? clientID, int? warehouseID, string priceTier, List<SaleItemDTO> quoteItems, string notes)
		{
			_loadedQuoteID = quoteID;
			if (clientID.HasValue && clientID.Value > 0)
			{
				for (int i = 0; i < cboClient.Items.Count; i++)
					if (cboClient.Items[i] is ComboItem ci && ci.ID == clientID.Value)
					{ cboClient.SelectedIndex = i; break; }
			}
			if (warehouseID.HasValue && warehouseID.Value > 0)
			{
				for (int i = 0; i < cboWarehouse.Items.Count; i++)
					if (cboWarehouse.Items[i] is ComboItem w && w.ID == warehouseID.Value)
					{ cboWarehouse.SelectedIndex = i; break; }
			}
			if (!string.IsNullOrEmpty(priceTier))
			{
				SetTierButtons(priceTier);
			}
			if (!string.IsNullOrEmpty(notes))
			{
				txtNotes.Text = notes;
			}

			_items.Clear();
			foreach (var item in quoteItems)
			{
				_items.Add(new SaleItemDTO
				{
					ProductID = item.ProductID,
					ProductName = item.ProductName,
					ProductCode = item.ProductCode,
					ShelfLocation = item.ShelfLocation,
					UnitName = item.UnitName,
					Quantity = item.Quantity,
					UnitPrice = item.UnitPrice,
					DiscountAmt = item.DiscountAmt,
					DiscountPct = item.DiscountPct,
					Factor = item.Factor
				});
			}
			RefreshGrid();
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
				bool isSrv = item.IsService;
				if (!isSrv)
				{
					var isSrvVal = DbHelper.Scalar("SELECT IsService FROM Products WHERE ProductID=@pid", DbHelper.P("@pid", item.ProductID));
					isSrv = isSrvVal != null && isSrvVal != DBNull.Value && Convert.ToBoolean(isSrvVal);
				}
				if (isSrv) continue; // الأصناف الخدمية لا تخضع لفحص الرصيد وتباع بالسالب

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

				decimal quantityToCheckBase = quantityToCheck * item.Factor;

				if (quantityToCheckBase > 0 && quantityToCheckBase > productStock)
				{
					decimal availableInSelectedUnit = productStock / item.Factor;
					MessageBox.Show($"❌ خطأ: الصنف '{item.ProductName}' لا يوجد منه رصيد كافٍ في المخزن حالياً لتغطية الزيادة المطلوبة.\nالزيادة المطلوبة: {quantityToCheck:N2} {item.UnitName}\nالكمية المتاحة بالمخزن: {availableInSelectedUnit:N2} {item.UnitName}",
						"عجز في الرصيد", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}

			int saleType = _invoiceType == "Credit" ? 0 : _invoiceType == "DriverLoad" ? 1 : _invoiceType == "Installment" ? 3 : _invoiceType == "Visa" ? 4 : 2;
			int? clientID = null;
			int? driverID = null;
			if (_invoiceType == "Credit" || _invoiceType == "Cash" || _invoiceType == "Installment" || _invoiceType == "Visa")
			{
				if (!(cboClient.SelectedItem is ComboItem comboItem) || comboItem.ID == 0)
				{
					if (_invoiceType == "Cash" || _invoiceType == "Visa")
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
			// ─── إضافة الشحن إلى الإجمالي ───
			decimal shippingAtSave = nudShippingCharge != null ? nudShippingCharge.Value : 0m;
			net += shippingAtSave;
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

					int? visaAccountID = null;
					string visaAccountName = "";
					if (_invoiceType == "Visa" && !isDraft)
					{
						if (!FrmSelectVisaAccount.SelectVisaAccount(this, net, safeAccountID, out int vId, out string vName))
						{
							return;
						}
						visaAccountID = vId;
						visaAccountName = vName;
						safeAccountID = vId;
					}

					bool updated = SaleDAL.UpdateSale(_editSaleID, saleType, clientID, driverID,
						net, txtNotes.Text, _items, discountAmount, discountPct,
						isDraft: false, warehouseID: GetSelectedWarehouseID(), priceTier: priceTier,
						loadedLastModified: _loadedLastModified, safeAccountID: safeAccountID, cashPaid: paidAmount,
						cratesOut: (int)nudCratesOut.Value, cratesIn: (int)nudCratesIn.Value, shippingCharge: shippingAtSave,
						visaAccountID: visaAccountID, visaPaid: (_invoiceType == "Visa" ? net : (decimal?)null));
					if (updated)
					{
						_isDirty = false;
						DialogResult pr = MessageBox.Show(
							$"✅ تم تعديل الفاتورة رقم [{_editSaleID}] بنجاح!\n\nهل تريد طباعة الفاتورة المعدّلة؟",
							"تعديل ناجح", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (pr == DialogResult.Yes) new FrmPrintSale(_editSaleID, showPreview: false);

						try
						{
							List<int> soldPids = _items != null ? _items.ConvertAll(x => x.ProductID) : new List<int>();
							var zeroItems = ShortageDAL.ProcessStockChangesAfterSale(soldPids);
							if (zeroItems.Count > 0)
							{
								ShortageDAL.PromptZeroStockDialog(this, zeroItems);
							}
						}
						catch { }

						ResetForm();
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
					using (var frmConfig = new FrmConfigureInstallment(net, paidAmount))
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

				int? visaAccountID = null;
				string visaAccountName = "";
				if (_invoiceType == "Visa" && !isDraft)
				{
					if (!FrmSelectVisaAccount.SelectVisaAccount(this, net, safeAccountID, out int vId, out string vName))
					{
						return;
					}
					visaAccountID = vId;
					visaAccountName = vName;
					safeAccountID = vId;
				}

				int num3 = SaleDAL.SaveSale(saleType, clientID, driverID, net,
					txtNotes.Text, _items, discountAmount, discountPct, isDraft,
					warehouseID: GetSelectedWarehouseID(), priceTier: priceTier,
					downPayment: downPayment, installmentCount: installmentCount,
					installmentPeriod: installmentPeriod, startDate: startDate,
					schedule: schedule, safeAccountID: safeAccountID, cashPaid: paidAmount,
					cratesOut: (int)nudCratesOut.Value, cratesIn: (int)nudCratesIn.Value, shippingCharge: shippingAtSave,
					visaAccountID: visaAccountID, visaPaid: (_invoiceType == "Visa" ? net : (decimal?)null));
				if (num3 > 0)
				{
					_lastSaleID = num3;
					_isDirty = false;
					if (_loadedQuoteID > 0)
					{
						try { PriceQuoteDAL.MarkAsConverted(_loadedQuoteID, num3); } catch { }
						_loadedQuoteID = 0;
					}
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

						try
						{
							List<int> soldPids = _items != null ? _items.ConvertAll(x => x.ProductID) : new List<int>();
							var zeroItems = ShortageDAL.ProcessStockChangesAfterSale(soldPids);
							if (zeroItems.Count > 0)
							{
								ShortageDAL.PromptZeroStockDialog(this, zeroItems);
							}
						}
						catch { }
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
			if (Session.CanOrderColumns("Sales"))
			{
				Session.SaveColumnOrder(dgItems, "Sales");
			}

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

			var itemPrep = new ToolStripMenuItem("📋 طباعة إذن التحضير والتجميع (F9)");
			itemPrep.Click += (s2, e2) => PrintPreparationSlip();

			menu.Items.Add(itemReceipt);
			menu.Items.Add(itemA4);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(itemPrep);

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

			var itemPrep = new ToolStripMenuItem("📋 معاينة إذن التحضير والتجميع (F9)");
			itemPrep.Click += (s2, e2) => PrintPreparationSlip();

			menu.Items.Add(itemReceipt);
			menu.Items.Add(itemA4);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(itemPrep);

			if (sender is Control ctrl)
			{
				menu.Show(ctrl, new Point(0, ctrl.Height));
			}
			else
			{
				menu.Show(Cursor.Position);
			}
		}

		/// <summary>
		/// طباعة إذن تحضير وتجميع بضاعة من المخزن للأصناف الموجودة في الفاتورة الحالية
		/// </summary>
		public void PrintPreparationSlip()
		{
			if (_items == null || _items.Count == 0)
			{
				MessageBox.Show("لا توجد أصناف في الفاتورة لطباعة إذن التحضير!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var res = MessageBox.Show("هل تريد طباعة إذن التحضير على طابعة ريسيت حراري (80mm)؟\nاضغط (Yes) للـ Receipt أو (No) للـ A4/A5.", "اختيار نوع طباعة إذن التحضير", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (res == DialogResult.Cancel) return;

			bool isReceipt = (res == DialogResult.Yes);

			var pd = new System.Drawing.Printing.PrintDocument();
			if (isReceipt)
			{
				pd.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Receipt", 300, 1000);
				pd.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10);
				AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
			}
			else
			{
				pd.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1169);
				pd.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(25, 25, 25, 25);
				AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
			}

			string whName = cboWarehouse != null && cboWarehouse.SelectedItem != null ? cboWarehouse.Text : "المخزن الرئيسي";
			string clientName = (cboClient != null && cboClient.SelectedItem is ComboItem ci && ci.ID > 0) ? ci.Text : (cboClient?.Text?.Trim() ?? "عميل نقدي");
			if (string.IsNullOrEmpty(clientName) || clientName.StartsWith("--")) clientName = "عميل نقدي";
			string empName = Session.EmpName;
			string companyName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
			string companyPhone = !string.IsNullOrWhiteSpace(AppConfig.CompanyPhone) ? AppConfig.CompanyPhone : "";
			string companyAddress = !string.IsNullOrWhiteSpace(AppConfig.CompanyAddress) ? AppConfig.CompanyAddress : "";
			string invoiceCode = _editSaleID > 0 ? $"فاتورة رقم {_editSaleID}" : "فاتورة مبيعات جديدة";
			string saleTypeStr = FormatInvoiceTypeArabic(_invoiceType);

			Image logoImg = null;
			try
			{
				string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
				if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.jpg");
				if (System.IO.File.Exists(logoPath)) logoImg = Image.FromFile(logoPath);
			}
			catch { }

			int itemIdx = 0;
			int rowNum = 0;
			decimal totalQty = 0m;

			pd.BeginPrint += (s, e) =>
			{
				itemIdx = 0;
				rowNum = 0;
				totalQty = 0m;
			};

			pd.PrintPage += (s, e) =>
			{
				var g = e.Graphics;
				float titleSize  = isReceipt ? 12f : 16f;
				float headerSize = isReceipt ? 9f  : 11f;
				float bodySize   = isReceipt ? 8.5f: 10f;

				using var fontCompany = new Font("Arial", isReceipt ? 11f : 14f, FontStyle.Bold);
				using var fontTitle   = new Font("Arial", titleSize,  FontStyle.Bold);
				using var fontHeader  = new Font("Arial", headerSize, FontStyle.Bold);
				using var fontBody    = new Font("Arial", bodySize,   FontStyle.Regular);
				using var fontBold    = new Font("Arial", bodySize,   FontStyle.Bold);

				using var brushDarkBlue = new SolidBrush(Color.FromArgb(20, 60, 120));
				using var brushHeaderBg = new SolidBrush(Color.FromArgb(28, 45, 78));
				using var brushRowAlt   = new SolidBrush(Color.FromArgb(245, 248, 253));
				using var brushTotBg    = new SolidBrush(Color.FromArgb(235, 245, 255));
				using var penGrid       = new Pen(Color.FromArgb(170, 185, 205), 1f);
				using var penDark       = new Pen(Color.FromArgb(28, 45, 78), 1.5f);

				int y     = e.MarginBounds.Top;
				int left  = e.MarginBounds.Left;
				int right = e.MarginBounds.Right;
				int width = e.MarginBounds.Width;

				// ── 1. ترويسة الصفحة الأولى ──
				if (itemIdx == 0)
				{
					if (logoImg != null && !isReceipt)
					{
						g.DrawImage(logoImg, right - 70, y, 65, 50);
					}

					SizeF szComp = g.MeasureString(companyName, fontCompany);
					g.DrawString(companyName, fontCompany, brushDarkBlue, left + (width - szComp.Width) / 2, y);
					y += (int)szComp.Height + 2;

					if (!string.IsNullOrWhiteSpace(companyPhone))
					{
						string phStr = $"تليفون: {companyPhone}" + (!string.IsNullOrWhiteSpace(companyAddress) ? $" | {companyAddress}" : "");
						SizeF szPh = g.MeasureString(phStr, fontBody);
						g.DrawString(phStr, fontBody, Brushes.DarkGray, left + (width - szPh.Width) / 2, y);
						y += (int)szPh.Height + 4;
					}

					string tit = "📋 إذن تحضير وتجميع بضاعة (من المخزن)";
					SizeF szT  = g.MeasureString(tit, fontTitle);
					g.DrawString(tit, fontTitle, Brushes.Black, left + (width - szT.Width) / 2, y);
					y += (int)szT.Height + (isReceipt ? 4 : 6);

					g.DrawLine(penDark, left, y, right, y);
					y += (isReceipt ? 4 : 8);

					string dateStr = dtpDate.Value.ToString("dd/MM/yyyy HH:mm");
					if (!isReceipt)
					{
						g.DrawString($"المخزن المصدر: {whName}", fontHeader, Brushes.Black, right - g.MeasureString($"المخزن المصدر: {whName}", fontHeader).Width, y);
						g.DrawString($"التاريخ والوقت: {dateStr}", fontBody, Brushes.Black, left, y);
						y += 20;

						g.DrawString($"العميل: {clientName}", fontHeader, Brushes.Black, right - g.MeasureString($"العميل: {clientName}", fontHeader).Width, y);
						g.DrawString($"المرجع / الفاتورة: {invoiceCode} ({saleTypeStr})", fontBody, Brushes.Black, left, y);
						y += 20;

						g.DrawString($"الموظف المسؤول: {empName}", fontBody, Brushes.Black, right - g.MeasureString($"الموظف المسؤول: {empName}", fontBody).Width, y);
						g.DrawString($"عدد الأصناف: {_items.Count}", fontBody, Brushes.Black, left, y);
						y += 22;

						if (!string.IsNullOrWhiteSpace(txtNotes.Text))
						{
							g.DrawString($"ملاحظات: {txtNotes.Text.Trim()}", fontBody, Brushes.DarkRed, right - g.MeasureString($"ملاحظات: {txtNotes.Text.Trim()}", fontBody).Width, y);
							y += 20;
						}
					}
					else
					{
						g.DrawString($"المخزن: {whName}",   fontHeader, Brushes.Black, left, y); y += 18;
						g.DrawString($"العميل: {clientName}", fontHeader, Brushes.Black, left, y); y += 18;
						g.DrawString($"المرجع: {invoiceCode} | الموظف: {empName}", fontBody, Brushes.Black, left, y); y += 18;
						g.DrawString($"التاريخ: {dateStr}", fontBody,   Brushes.Black, left, y); y += 18;
						if (!string.IsNullOrWhiteSpace(txtNotes.Text))
						{
							g.DrawString($"ملاحظة: {txtNotes.Text.Trim()}", fontBody, Brushes.DarkRed, left, y); y += 18;
						}
					}

					g.DrawLine(penGrid, left, y, right, y);
					y += (isReceipt ? 4 : 8);
				}

				// ── 2. إعداد أبعاد أعمدة الجدول الشبكي ──
				int colNumW  = isReceipt ? 18 : (int)(width * 0.05);
				int colCodeW = isReceipt ? 35 : (int)(width * 0.13);
				int colLocW  = isReceipt ? 45 : (int)(width * 0.20);
				int colUnitW = isReceipt ? 30 : (int)(width * 0.11);
				int colQtyW  = isReceipt ? 30 : (int)(width * 0.13);
				int colProdW = width - colNumW - colCodeW - colLocW - colUnitW - colQtyW;
				int rowH     = isReceipt ? 22 : 26;

				var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };
				var sfRight  = new StringFormat { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };

				// رأس الجدول
				if (!isReceipt)
				{
					g.FillRectangle(brushHeaderBg, left, y, width, rowH);
					g.DrawRectangle(penDark, left, y, width, rowH);

					int curX = right;

					// #
					curX -= colNumW;
					g.DrawRectangle(penGrid, curX, y, colNumW, rowH);
					g.DrawString("#", fontHeader, Brushes.White, new RectangleF(curX, y, colNumW, rowH), sfCenter);

					// Code
					curX -= colCodeW;
					g.DrawRectangle(penGrid, curX, y, colCodeW, rowH);
					g.DrawString("الكود", fontHeader, Brushes.White, new RectangleF(curX, y, colCodeW, rowH), sfCenter);

					// Product
					curX -= colProdW;
					g.DrawRectangle(penGrid, curX, y, colProdW, rowH);
					g.DrawString("اسم الصنف", fontHeader, Brushes.White, new RectangleF(curX, y, colProdW, rowH), sfCenter);

					// Qty
					curX -= colQtyW;
					g.DrawRectangle(penGrid, curX, y, colQtyW, rowH);
					g.DrawString("الكمية المطلوبة", fontHeader, Brushes.White, new RectangleF(curX, y, colQtyW, rowH), sfCenter);

					// Unit
					curX -= colUnitW;
					g.DrawRectangle(penGrid, curX, y, colUnitW, rowH);
					g.DrawString("الوحدة", fontHeader, Brushes.White, new RectangleF(curX, y, colUnitW, rowH), sfCenter);

					// Shelf Location
					curX -= colLocW;
					g.DrawRectangle(penGrid, curX, y, colLocW, rowH);
					g.DrawString("مكان التخزين / الرف", fontHeader, Brushes.White, new RectangleF(curX, y, colLocW, rowH), sfCenter);

					y += rowH;
				}
				else
				{
					g.DrawString("الصنف",  fontHeader, Brushes.Black, right - colNumW - colProdW, y);
					g.DrawString("الكمية",  fontHeader, Brushes.Black, right - colNumW - colProdW - colQtyW, y);
					g.DrawString("الوحدة",  fontHeader, Brushes.Black, right - colNumW - colProdW - colQtyW - colUnitW, y);
					g.DrawString("الرف",   fontHeader, Brushes.Black, right - colNumW - colProdW - colQtyW - colUnitW - colLocW, y);
					y += rowH;
					g.DrawLine(penGrid, left, y, right, y);
					y += 4;
				}

				// ── 3. سطور أصناف التحضير ──
				while (itemIdx < _items.Count)
				{
					var item  = _items[itemIdx];
					string loc = !string.IsNullOrWhiteSpace(item.ShelfLocation) ? item.ShelfLocation : "";
					if (string.IsNullOrWhiteSpace(loc) && item.ProductID > 0)
					{
						var ciLoc = GetProductComboItem(item.ProductID);
						if (ciLoc != null && !string.IsNullOrWhiteSpace(ciLoc.ShelfLocation)) loc = ciLoc.ShelfLocation;
					}
					if (string.IsNullOrWhiteSpace(loc)) loc = "---";

					string unit = !string.IsNullOrWhiteSpace(item.UnitName) ? item.UnitName : "قطعة";
					string code = !string.IsNullOrWhiteSpace(item.ProductCode) ? item.ProductCode : (!string.IsNullOrWhiteSpace(item.PartNumber) ? item.PartNumber : item.ProductID.ToString());
					string qty  = item.Quantity % 1 == 0 ? item.Quantity.ToString("N0") : item.Quantity.ToString("N2");
					totalQty += item.Quantity;
					rowNum++;

					if (!isReceipt)
					{
						if (rowNum % 2 == 0)
							g.FillRectangle(brushRowAlt, left, y, width, rowH);

						g.DrawRectangle(penGrid, left, y, width, rowH);

						int curX = right;

						// #
						curX -= colNumW;
						g.DrawRectangle(penGrid, curX, y, colNumW, rowH);
						g.DrawString(rowNum.ToString(), fontBody, Brushes.Black, new RectangleF(curX, y, colNumW, rowH), sfCenter);

						// Code
						curX -= colCodeW;
						g.DrawRectangle(penGrid, curX, y, colCodeW, rowH);
						g.DrawString(code, fontBody, Brushes.Gray, new RectangleF(curX, y, colCodeW, rowH), sfCenter);

						// Product
						curX -= colProdW;
						g.DrawRectangle(penGrid, curX, y, colProdW, rowH);
						g.DrawString(item.ProductName, fontBody, Brushes.Black, new RectangleF(curX + 4, y, colProdW - 8, rowH), sfRight);

						// Qty
						curX -= colQtyW;
						g.DrawRectangle(penGrid, curX, y, colQtyW, rowH);
						g.DrawString(qty, fontBold, Brushes.Black, new RectangleF(curX, y, colQtyW, rowH), sfCenter);

						// Unit
						curX -= colUnitW;
						g.DrawRectangle(penGrid, curX, y, colUnitW, rowH);
						g.DrawString(unit, fontBody, Brushes.DarkBlue, new RectangleF(curX, y, colUnitW, rowH), sfCenter);

						// Shelf Location
						curX -= colLocW;
						g.DrawRectangle(penGrid, curX, y, colLocW, rowH);
						g.DrawString(loc, fontBold, brushDarkBlue, new RectangleF(curX, y, colLocW, rowH), sfCenter);
					}
					else
					{
						int tx = y;
						g.DrawString(item.ProductName, fontBody,   Brushes.Black,  right - colNumW - colProdW,                              tx);
						g.DrawString(qty,              fontBold,   Brushes.Black,  right - colNumW - colProdW - colQtyW,                    tx);
						g.DrawString(unit,             fontBody,   brushDarkBlue,  right - colNumW - colProdW - colQtyW - colUnitW,         tx);
						g.DrawString(loc,              fontBold,   brushDarkBlue,  right - colNumW - colProdW - colQtyW - colUnitW - colLocW,tx);
						g.DrawString(rowNum.ToString(),fontBody,   Brushes.Black,  left,                                                   tx);
						g.DrawLine(penGrid, left, y + rowH, right, y + rowH);
					}

					y += rowH;
					itemIdx++;

					if (y > e.MarginBounds.Bottom - (isReceipt ? 40 : 70) && itemIdx < _items.Count)
					{
						e.HasMorePages = true;
						return;
					}
				}

				// ── 4. الإجماليات والتوقيعات ──
				if (!isReceipt)
				{
					g.FillRectangle(brushTotBg, left, y, width, rowH);
					g.DrawRectangle(penDark, left, y, width, rowH);

					string totStr = $"إجمالي الأصناف: {_items.Count} صنف  |  إجمالي كميات التحضير: {(totalQty % 1 == 0 ? totalQty.ToString("N0") : totalQty.ToString("N2"))}";
					g.DrawString(totStr, fontHeader, Brushes.Black, new RectangleF(left, y, width, rowH), sfCenter);
					y += rowH + 15;
				}
				else
				{
					y += 6;
					g.DrawLine(penDark, left, y, right, y);
					y += 6;
					string totStr = $"إجمالي الأصناف: {_items.Count}  |  إجمالي الكميات: {(totalQty % 1 == 0 ? totalQty.ToString("N0") : totalQty.ToString("N2"))}";
					g.DrawString(totStr, fontHeader, Brushes.Black, left, y);
					y += 18;
				}

				// توقيعات المسؤول والمستلم
				y += (isReceipt ? 6 : 14);
				g.DrawLine(penDark, left, y, right, y);
				y += (isReceipt ? 6 : 12);
				string sig1 = "مسؤول التحضير بالمخزن: ..................................";
				string sig2 = "توقيع المستلم / السائق: ..................................";
				if (!isReceipt)
				{
					g.DrawString(sig1, fontHeader, Brushes.Black, right - g.MeasureString(sig1, fontHeader).Width, y);
					g.DrawString(sig2, fontHeader, Brushes.Black, left, y);
				}
				else
				{
					g.DrawString(sig1, fontHeader, Brushes.Black, left, y);
				}

				logoImg?.Dispose();
				e.HasMorePages = false;
			};

			try
			{
				pd.Print();
			}
			catch (Exception ex)
			{
				AppLogger.Error("FrmSale.PrintPreparationSlip", ex);
				MessageBox.Show("خطأ في طباعة إذن التحضير: " + ex.Message, "خطأ في الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private string FormatInvoiceTypeArabic(string type)
		{
			switch (type)
			{
				case "Credit": return "آجل";
				case "Cash": return "نقدي";
				case "Visa": return "فيزا";
				case "DriverLoad": return "تحميل مندوب";
				case "Installment": return "تقسيط";
				default: return type ?? "نقدي";
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
				if (cboSafe.Items.Count > 0)
				{
					int defaultSafeID = Session.DefaultSafeID ?? 0;
					int selectedIdx = -1;
					if (defaultSafeID > 0)
					{
						for (int i = 0; i < cboSafe.Items.Count; i++)
						{
							if (cboSafe.Items[i] is ComboItem ci && ci.ID == defaultSafeID)
							{
								selectedIdx = i;
								break;
							}
						}
					}
					if (selectedIdx >= 0)
					{
						cboSafe.SelectedIndex = selectedIdx;
					}
					else
					{
						int fallbackIdx = 0;
						for (int i = 0; i < cboSafe.Items.Count; i++)
						{
							if (cboSafe.Items[i] is ComboItem ci && ci.Text.Contains("درج تلقائي"))
							{
								fallbackIdx = i;
								break;
							}
						}
						cboSafe.SelectedIndex = fallbackIdx;
					}
				}
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

			// استدعاء شاشة اختيار نموذج الفاتورة والمعاينة التفاعلية
			ShowWhatsAppTemplateModal(phone, saleRow, items, prevBalance, lastPaymentAmt, lastPaymentDate, todayPayments, todayReturns, actualCurrentBalance, null);
		}

		private static string BuildWhatsAppTextDetailed(DataRow saleRow, DataTable items, decimal prevBalance, decimal lastPaymentAmt, DateTime lastPaymentDate, decimal todayPayments, decimal todayReturns, decimal actualCurrentBalance)
		{
			var sb = new System.Text.StringBuilder();
			string shopName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة والتجارة العامة";
			sb.AppendLine($"📋 *فاتورة مبيعات رقم #{saleRow["SaleCode"]}*");
			sb.AppendLine($"🏢 *{shopName}*");
			sb.AppendLine($"👤 العميل: {saleRow["ClientName"]}");
			sb.AppendLine($"📅 التاريخ: {Convert.ToDateTime(saleRow["SaleDate"]):dd/MM/yyyy hh:mm tt}");
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
					sb.AppendLine($"▪ السعر : {price:N2} ج.م");
					sb.AppendLine($"▪ الإجمالي : {tot:N2} ج.م");
					sb.AppendLine("━━━━━━━━━━━━━━━━");
				}
			}

			decimal totalAmount = Convert.ToDecimal(saleRow["TotalAmount"]);
			sb.AppendLine($"💰 *صافي الفاتورة: {totalAmount:N2} ج.م*");
			sb.AppendLine("━━━━━━━━━━━━━━━━");

			if (AppConfig.EnableCratesTracking)
			{
				int cratesOutValMsg = saleRow.Table.Columns.Contains("CratesOut") && saleRow["CratesOut"] != DBNull.Value ? Convert.ToInt32(saleRow["CratesOut"]) : 0;
				int cratesInValMsg = saleRow.Table.Columns.Contains("CratesIn") && saleRow["CratesIn"] != DBNull.Value ? Convert.ToInt32(saleRow["CratesIn"]) : 0;
				if (cratesOutValMsg > 0 || cratesInValMsg > 0)
				{
					sb.AppendLine("📦 *حركة الفوارغ*");
					if (cratesOutValMsg > 0) sb.AppendLine($"▪ فوارغ صادرة : {cratesOutValMsg} فارغ");
					if (cratesInValMsg > 0) sb.AppendLine($"▪ فوارغ واردة : {cratesInValMsg} فارغ");
					sb.AppendLine("━━━━━━━━━━━━━━━━");
				}
			}

			bool isCredit = saleRow["SaleType"].ToString() == "Credit";
			decimal cashPaid = saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(saleRow["CashPaid"]) : totalAmount;
			decimal remainingFromInvoice = isCredit ? totalAmount : (totalAmount - cashPaid);

			if (saleRow["ClientID"] != DBNull.Value)
			{
				int clientIDVal = Convert.ToInt32(saleRow["ClientID"]);
				decimal totalDue = prevBalance + (isCredit ? totalAmount : remainingFromInvoice);
				decimal currentDue = actualCurrentBalance;

				sb.AppendLine("📊 *الوضع المالي للحساب*");
				sb.AppendLine($"▪ الرصيد السابق : {prevBalance:N2} ج.م");
				if (isCredit)
				{
					sb.AppendLine($"▪ الفاتورة الحالية : {totalAmount:N2} ج.م");
					sb.AppendLine($"▪ إجمالي المستحق : {totalDue:N2} ج.م");
				}
				else
				{
					if (remainingFromInvoice > 0)
					{
						sb.AppendLine($"▪ متبقي الفاتورة الحالية : {remainingFromInvoice:N2} ج.م");
						sb.AppendLine($"▪ إجمالي المستحق : {totalDue:N2} ج.م");
					}
					else if (remainingFromInvoice < 0)
					{
						sb.AppendLine($"▪ زيادة الفاتورة الحالية : {-remainingFromInvoice:N2} ج.م");
						sb.AppendLine($"▪ إجمالي المستحق : {totalDue:N2} ج.م");
					}
				}
				sb.AppendLine($"▪ مسدد اليوم : {todayPayments:N2} ج.م");
				if (todayReturns > 0)
				{
					sb.AppendLine($"▪ مرتجع اليوم : {todayReturns:N2} ج.م");
				}
				if (lastPaymentAmt > 0)
				{
					sb.AppendLine($"📝 آخر توريد سابق : {lastPaymentAmt:N2} ج.م ({lastPaymentDate:dd/MM/yyyy})");
				}
				int currentCratesDueMsg = ClientDAL.GetClientCratesBalance(clientIDVal);
				sb.AppendLine($"▪ فوارغ العميل الحالية : {currentCratesDueMsg} فارغ");
				sb.AppendLine("━━━━━━━━━━━━━━━━");
				sb.AppendLine($"🔴 *الرصيد الحالي المستحق: {currentDue:N2} ج.م*");
				sb.AppendLine("━━━━━━━━━━━━━━━━");
			}

			sb.AppendLine("🙏 شكراً لتعاملكم معنا ✨");
			return sb.ToString();
		}

		private static string BuildWhatsAppTextSummary(DataRow saleRow, DataTable items, decimal actualCurrentBalance)
		{
			var sb = new System.Text.StringBuilder();
			string shopName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة والتجارة العامة";
			sb.AppendLine($"🧾 *فاتورة مبيعات مختصرة* #{saleRow["SaleCode"]}");
			sb.AppendLine($"🏢 *{shopName}*");
			sb.AppendLine($"👤 العميل: {saleRow["ClientName"]}");
			sb.AppendLine($"📅 التاريخ: {Convert.ToDateTime(saleRow["SaleDate"]):dd/MM/yyyy}");
			string typeLabel = saleRow["SaleType"].ToString() == "Credit" ? "آجل" : "نقدي";
			sb.AppendLine($"💳 نوع البيع: {typeLabel}");
			sb.AppendLine("--------------------------------");

			if (items != null)
			{
				decimal totalQty = 0;
				foreach (DataRow r in items.Rows)
				{
					totalQty += Convert.ToDecimal(r["Quantity"]);
				}
				sb.AppendLine($"📦 عدد الأصناف: {items.Rows.Count} | إجمالي الكمية: {totalQty:0.##}");
			}

			decimal totalAmount = Convert.ToDecimal(saleRow["TotalAmount"]);
			sb.AppendLine($"💰 *إجمالي الفاتورة: {totalAmount:N2} ج.م*");

			if (saleRow["ClientID"] != DBNull.Value)
			{
				sb.AppendLine("--------------------------------");
				sb.AppendLine($"🔴 *الرصيد النهائي المستحق: {actualCurrentBalance:N2} ج.م*");
			}

			sb.AppendLine("🙏 شكراً لتعاملكم معنا ✨");
			return sb.ToString();
		}

		private static string BuildWhatsAppTextFinancial(DataRow saleRow, DataTable items, decimal prevBalance, decimal actualCurrentBalance)
		{
			var sb = new System.Text.StringBuilder();
			string shopName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة والتجارة العامة";
			sb.AppendLine($"💳 *إشعار فاتورة وكشف حساب عميل*");
			sb.AppendLine($"🏢 *{shopName}*");
			sb.AppendLine($"👤 العميل: {saleRow["ClientName"]}");
			sb.AppendLine($"📅 التاريخ: {Convert.ToDateTime(saleRow["SaleDate"]):dd/MM/yyyy}");
			sb.AppendLine("--------------------------------");
			sb.AppendLine($"🏷️ الفاتورة رقم: #{saleRow["SaleCode"]}");

			decimal totalAmount = Convert.ToDecimal(saleRow["TotalAmount"]);
			decimal cashPaid = saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(saleRow["CashPaid"]) : (saleRow["SaleType"].ToString() == "Cash" ? totalAmount : 0m);

			sb.AppendLine($"💰 قيمة الفاتورة الحالية: {totalAmount:N2} ج.م");
			sb.AppendLine($"💵 المسدد نقداً: {cashPaid:N2} ج.م");
			sb.AppendLine($"📜 الرصيد السابق قبل الفاتورة: {prevBalance:N2} ج.م");
			sb.AppendLine("--------------------------------");
			sb.AppendLine($"✨ *صافي رصيد الحساب المالي المستحق: {actualCurrentBalance:N2} ج.م*");

			if (saleRow["ClientID"] != DBNull.Value)
			{
				int clientIDVal = Convert.ToInt32(saleRow["ClientID"]);
				int cratesDue = ClientDAL.GetClientCratesBalance(clientIDVal);
				if (cratesDue != 0)
				{
					sb.AppendLine($"📦 رصيد الفوارغ المستحق: {cratesDue} فارغ");
				}
			}

			if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone))
			{
				sb.AppendLine($"📱 للتواصل والاستفسار: {AppConfig.CompanyPhone}");
			}
			return sb.ToString();
		}

		private static void ShowWhatsAppTemplateModal(string phone, DataRow saleRow, DataTable items, decimal prevBalance, decimal lastPaymentAmt, DateTime lastPaymentDate, decimal todayPayments, decimal todayReturns, decimal actualCurrentBalance, Form parent)
		{
			var dlg = new Form
			{
				Text = "📱 معاينة وإرسال فاتورة مبيعات عبر واتساب",
				Size = new Size(680, 700),
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false,
				BackColor = Theme.BgCard,
				Font = Theme.FontMain,
				RightToLeft = RightToLeft.Yes,
				RightToLeftLayout = true
			};

			var pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Theme.BgSearchPanel, Padding = new Padding(15, 10, 15, 10) };
			var lblTpl = new Label { Text = "اختر نموذج رسالة الفاتورة:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(15, 15) };

			var cboTpl = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 420,
				Location = new Point(180, 12),
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat
			};
			cboTpl.Items.AddRange(new object[]
			{
				"🖼️ كارت الفاتورة الكلاسيكي الملكي (Royal Navy Card)",
				"🖼️ كارت الفاتورة المودرن الفحمي (Modern Charcoal Card)",
				"🖼️ كارت الفاتورة الشبكي التجاري (Commercial Grid Card)",
				"🖼️ كارت الفاتورة الزمردي الأنيق (Emerald Green Card)",
				"🖼️ كارت الفاتورة الذهبي للشركات (Corporate Gold Card)",
				"💬 النموذج التفصيلي الشامل (رسالة نصية تفصيلية)",
				"💬 النموذج السريع الموجز (رسالة نصية سريعة)",
				"💬 نموذج كشف الحساب والمالية (رسالة نصية مالية)"
			});

			string savedTpl = AppConfig.WhatsAppInvoiceTemplate;
			cboTpl.SelectedIndex = savedTpl switch
			{
				"ImageCardModern" => 1,
				"ImageCardCommercial" => 2,
				"ImageCardEmerald" => 3,
				"ImageCardGold" => 4,
				"Detailed" => 5,
				"Summary" => 6,
				"Financial" => 7,
				_ => 0
			};

			pnlTop.Controls.Add(lblTpl);
			pnlTop.Controls.Add(cboTpl);

			var pnlCenter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

			var txtTextPreview = new RichTextBox
			{
				Dock = DockStyle.Fill,
				BackColor = Color.FromArgb(15, 23, 42),
				ForeColor = Color.FromArgb(241, 245, 249),
				Font = new Font("Segoe UI", 10.5f),
				BorderStyle = BorderStyle.FixedSingle,
				RightToLeft = RightToLeft.Yes
			};

			var picImagePreview = new PictureBox
			{
				Dock = DockStyle.Fill,
				SizeMode = PictureBoxSizeMode.Zoom,
				BackColor = Color.FromArgb(15, 23, 42),
				BorderStyle = BorderStyle.FixedSingle,
				Visible = false
			};

			pnlCenter.Controls.Add(txtTextPreview);
			pnlCenter.Controls.Add(picImagePreview);

			Bitmap cachedBmp = null;

			Action updatePreview = () =>
			{
				int idx = cboTpl.SelectedIndex;
				if (idx < 5)
				{
					txtTextPreview.Visible = false;
					picImagePreview.Visible = true;
					string tplKey = idx switch
					{
						1 => "ImageCardModern",
						2 => "ImageCardCommercial",
						3 => "ImageCardEmerald",
						4 => "ImageCardGold",
						_ => "ImageCardNavy"
					};
					cachedBmp = ReceiptImageGenerator.GenerateSaleReceiptImage(saleRow, items, prevBalance, lastPaymentAmt, lastPaymentDate, todayPayments, todayReturns, actualCurrentBalance, tplKey);
					picImagePreview.Image = cachedBmp;
				}
				else
				{
					picImagePreview.Visible = false;
					txtTextPreview.Visible = true;
					string textContent = idx switch
					{
						6 => BuildWhatsAppTextSummary(saleRow, items, actualCurrentBalance),
						7 => BuildWhatsAppTextFinancial(saleRow, items, prevBalance, actualCurrentBalance),
						_ => BuildWhatsAppTextDetailed(saleRow, items, prevBalance, lastPaymentAmt, lastPaymentDate, todayPayments, todayReturns, actualCurrentBalance)
					};
					txtTextPreview.Text = textContent;
				}
			};

			cboTpl.SelectedIndexChanged += (s, e) => { cachedBmp = null; updatePreview(); };
			updatePreview();

			var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Theme.BgSearchPanel, Padding = new Padding(15, 10, 15, 10) };

			var btnSendText = Theme.MakeButton("💬 إرسال واتساب (نص)", Color.FromArgb(37, 211, 102));
			btnSendText.Size = new Size(185, 42);
			btnSendText.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
			btnSendText.Dock = DockStyle.Left;

			var btnSendImage = Theme.MakeButton("🖼️ إرسال واتساب (صورة)", Color.FromArgb(18, 140, 126));
			btnSendImage.Size = new Size(185, 42);
			btnSendImage.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
			btnSendImage.Dock = DockStyle.Left;
			btnSendImage.Margin = new Padding(8, 0, 0, 0);

			var btnSaveDefault = Theme.MakeButton("⚙️ حفظ كافتراضي", Color.FromArgb(70, 80, 100));
			btnSaveDefault.Size = new Size(130, 42);
			btnSaveDefault.Dock = DockStyle.Left;
			btnSaveDefault.Margin = new Padding(8, 0, 0, 0);

			var btnCancel = Theme.MakeButton("إلغاء", Color.FromArgb(100, 100, 110));
			btnCancel.Size = new Size(80, 42);
			btnCancel.Dock = DockStyle.Right;
			btnCancel.Click += (s, e) => dlg.Close();

			btnSaveDefault.Click += (s, e) =>
			{
				string tplKey = cboTpl.SelectedIndex switch
				{
					1 => "ImageCardModern",
					2 => "ImageCardCommercial",
					3 => "ImageCardEmerald",
					4 => "ImageCardGold",
					5 => "Detailed",
					6 => "Summary",
					7 => "Financial",
					_ => "ImageCardNavy"
				};
				AppConfig.WhatsAppInvoiceTemplate = tplKey;
				MessageBox.Show("✅ تم حفظ النموذج المختار كنموذج افتراضي لفواتير الواتساب!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
			};

			btnSendText.Click += (s, e) =>
			{
				int idx = cboTpl.SelectedIndex;
				string messageToSend = idx switch
				{
					6 => BuildWhatsAppTextSummary(saleRow, items, actualCurrentBalance),
					7 => BuildWhatsAppTextFinancial(saleRow, items, prevBalance, actualCurrentBalance),
					_ => BuildWhatsAppTextDetailed(saleRow, items, prevBalance, lastPaymentAmt, lastPaymentDate, todayPayments, todayReturns, actualCurrentBalance)
				};
				SendWhatsApp(phone, messageToSend);
				dlg.Close();
			};

			btnSendImage.Click += (s, e) =>
			{
				try
				{
					int idx = cboTpl.SelectedIndex;
					string tplKey = idx switch
					{
						1 => "ImageCardModern",
						2 => "ImageCardCommercial",
						3 => "ImageCardEmerald",
						4 => "ImageCardGold",
						_ => "ImageCardNavy"
					};
					if (cachedBmp == null)
					{
						cachedBmp = ReceiptImageGenerator.GenerateSaleReceiptImage(saleRow, items, prevBalance, lastPaymentAmt, lastPaymentDate, todayPayments, todayReturns, actualCurrentBalance, tplKey);
					}
					if (cachedBmp != null)
					{
						Clipboard.SetImage(cachedBmp);
					}
					MessageBox.Show("✅ تم تصميم كارت الفاتورة ونسخ الصورة للحافظة بنجاح!\nسيتم فتح واتساب العميل الآن، فقط اضغط Ctrl+V في مربع الكتابة للصق وإرسال الصورة.",
						"تم النسخ للحافظة", MessageBoxButtons.OK, MessageBoxIcon.Information);

					WhatsAppSender.OpenWhatsAppChat(phone);
					dlg.Close();
				}
				catch (Exception ex)
				{
					MessageBox.Show("فشل نسخ صورة الفاتورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			};

			pnlFooter.Controls.Add(btnSendText);
			pnlFooter.Controls.Add(btnSendImage);
			pnlFooter.Controls.Add(btnSaveDefault);
			pnlFooter.Controls.Add(btnCancel);

			dlg.Controls.Add(pnlCenter);
			dlg.Controls.Add(pnlTop);
			dlg.Controls.Add(pnlFooter);

			Theme.ApplyFormRTL(dlg);
			dlg.ShowDialog(parent);
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

		public static void SendSaleInvoiceWhatsApp(int saleID, Form parent = null)
		{
			try
			{
				DataTable dtSale = DbHelper.Query("SELECT s.*, c.ClientName, c.Phone AS ClientPhone, c.Phone2 AS ClientPhone2 FROM Sales s LEFT JOIN Clients c ON s.ClientID = c.ClientID WHERE s.SaleID=@id", DbHelper.P("@id", saleID));
				if (dtSale == null || dtSale.Rows.Count == 0)
				{
					MessageBox.Show("لم يتم العثور على الفاتورة المحددة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				DataRow sRow = dtSale.Rows[0];

				int clientID = sRow["ClientID"] != DBNull.Value ? Convert.ToInt32(sRow["ClientID"]) : 0;
				string clientPhone = sRow.Table.Columns.Contains("ClientPhone") && sRow["ClientPhone"] != DBNull.Value ? sRow["ClientPhone"].ToString() : "";
				if (string.IsNullOrWhiteSpace(clientPhone) && sRow.Table.Columns.Contains("ClientPhone2") && sRow["ClientPhone2"] != DBNull.Value)
					clientPhone = sRow["ClientPhone2"].ToString();
				string clientName = sRow.Table.Columns.Contains("ClientName") && sRow["ClientName"] != DBNull.Value ? sRow["ClientName"].ToString() : "";

				if (clientID > 0)
				{
					DataRow cRow = ClientDAL.GetByID(clientID);
					if (cRow != null)
					{
						clientPhone = cRow["Phone"] != DBNull.Value ? cRow["Phone"].ToString() : "";
						if (string.IsNullOrWhiteSpace(clientPhone) && cRow.Table.Columns.Contains("Phone2") && cRow["Phone2"] != DBNull.Value)
							clientPhone = cRow["Phone2"].ToString();
					}
				}

				if (string.IsNullOrWhiteSpace(clientPhone))
				{
					using (var inputDlg = new Form())
					{
						inputDlg.Text = "📱 أدخل رقم الواتساب للعميل";
						inputDlg.Size = new Size(380, 160);
						inputDlg.StartPosition = FormStartPosition.CenterParent;
						inputDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
						inputDlg.MaximizeBox = false; inputDlg.MinimizeBox = false;
						inputDlg.RightToLeft = RightToLeft.Yes;
						inputDlg.BackColor = Theme.BgMain;
						inputDlg.Font = Theme.FontMain;

						var lbl = new Label { Text = $"رقم موبايل/واتساب العميل ({clientName}):", Location = new Point(15, 15), AutoSize = true, ForeColor = Theme.TextMain };
						var txt = new TextBox { Location = new Point(15, 40), Width = 330, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
						var btn = Theme.MakeButton("إرسال الآن 📱", 200, 75, 145, 30, Theme.Success);
						btn.Click += (s, e) => { inputDlg.DialogResult = DialogResult.OK; inputDlg.Close(); };

						inputDlg.Controls.AddRange(new Control[] { lbl, txt, btn });
						if (inputDlg.ShowDialog(parent) == DialogResult.OK)
						{
							clientPhone = txt.Text.Trim();
						}
					}
				}

				if (string.IsNullOrWhiteSpace(clientPhone))
				{
					MessageBox.Show("لم يتم إدخال رقم واتساب إرسال الفاتورة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				string shopName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة والتجارة العامة";
				string saleCode = sRow["SaleCode"]?.ToString() ?? "";
				string saleDate = Convert.ToDateTime(sRow["SaleDate"]).ToString("yyyy/MM/dd hh:mm tt");
				decimal totalAmount = Convert.ToDecimal(sRow["TotalAmount"]);

				DataTable items = SaleDAL.GetItems(saleID);
				var sb = new System.Text.StringBuilder();
				sb.AppendLine($"🧾 *فاتورة مبيعات - {shopName}*");
				sb.AppendLine($"رقم الفاتورة: #{saleCode}");
				sb.AppendLine($"التاريخ: {saleDate}");
				sb.AppendLine($"العميل: {clientName}");
				sb.AppendLine("━━━━━━━━━━━━━━━━");
				sb.AppendLine("📦 *الأصناف والمسحوبات:*");

				foreach (DataRow item in items.Rows)
				{
					string pName = item["ProductName"]?.ToString() ?? "";
					decimal qty = Convert.ToDecimal(item["Quantity"]);
					decimal price = Convert.ToDecimal(item["UnitPrice"]);
					decimal total = Convert.ToDecimal(item["TotalPrice"]);
					sb.AppendLine($"• {pName} × {qty:0.##} = {total:N2} ج.م");
				}

				sb.AppendLine("━━━━━━━━━━━━━━━━");
				sb.AppendLine($"💰 *إجمالي الفاتورة:* {totalAmount:N2} ج.م");

				if (clientID > 0)
				{
					decimal clientBalance = ClientDAL.GetBalance(clientID);
					sb.AppendLine($"⚖️ *رصيد الحساب الحالي:* {clientBalance:N2} ج.م");
				}
				sb.AppendLine("🙏 شكراً لتعاملكم معنا!");

				WhatsAppSender.ShowWhatsAppSendOptionsDialog(
					parent,
					clientPhone,
					sb.ToString(),
					() => ReceiptImageGenerator.GenerateSaleReceiptImage(saleID),
					"📱 إرسال فاتورة المبيعات عبر الواتساب");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ فشل إرسال الفاتورة عبر الواتساب: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static Bitmap DrawInvoiceImage(DataRow saleRow, DataTable items, decimal prevBalance, decimal lastPaymentAmt, DateTime lastPaymentDate, decimal todayPayments, decimal todayReturns, decimal actualCurrentBalance = 0m)
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
					financialLines += 1; // "رصيد الفوارغ المستحق"
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
					
					// رسم سلتين مشتريات كشعار
					DrawShoppingCartSilhouette(g, 35, y - 25, 40);
					DrawShoppingCartSilhouette(g, w - 75, y - 25, 40);
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
								labelsList.Add("فوارغ صادرة بالفاتورة");
								valsList.Add($"{cratesOutVal} فارغ");
							}
							if (cratesInVal > 0)
							{
								labelsList.Add("فوارغ واردة بالفاتورة");
								valsList.Add($"{cratesInVal} فارغ");
							}

							int currentCratesDue = ClientDAL.GetClientCratesBalance(Convert.ToInt32(saleRow["ClientID"]));
							labelsList.Add("رصيد الفوارغ المستحق");
							valsList.Add($"{currentCratesDue} فارغ");
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
					
					DrawShoppingCartSilhouette(g, 100, y + 10, 25);
					DrawShoppingCartSilhouette(g, w - 125, y + 10, 25);

					// الدعاية للبرنامج
					var fPromo = new Font("Arial", 10f, FontStyle.Bold);
					using (var bPromo = new SolidBrush(Color.FromArgb(0, 80, 220)))
					{
						g.DrawString("✨ تم إصدار هذه الفاتورة بواسطة Pro System لإدارة المبيعات والتوزيع. للاشتراك: 01016517586", fPromo, bPromo, new RectangleF(20, y + footerH + 10, w - 40, 20), rtlCenter);
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

		private static void DrawShoppingCartSilhouette(Graphics g, float x, float y, float size)
		{
			using (var brush = new SolidBrush(Color.FromArgb(0, 51, 153)))
			using (var pen = new Pen(Color.FromArgb(0, 51, 153), size * 0.08f))
			{
				pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
				pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
				pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

				// Handle and frame line
				PointF pHandle = new PointF(x, y + size * 0.15f);
				PointF pTopBack = new PointF(x + size * 0.22f, y + size * 0.15f);
				PointF pBottomBack = new PointF(x + size * 0.30f, y + size * 0.68f);
				PointF pBottomFront = new PointF(x + size * 0.85f, y + size * 0.68f);

				// Draw frame & handle line
				g.DrawLine(pen, pHandle, pTopBack);
				g.DrawLine(pen, pTopBack, pBottomBack);
				g.DrawLine(pen, pBottomBack, pBottomFront);

				// Cart Basket (Solid Polygon)
				PointF[] basket = new PointF[]
				{
					pTopBack,
					new PointF(x + size * 0.95f, y + size * 0.22f),
					new PointF(x + size * 0.82f, y + size * 0.62f),
					new PointF(x + size * 0.30f, y + size * 0.62f)
				};
				g.FillPolygon(brush, basket);

				// Cart Wheels
				float wheelRadius = size * 0.11f;
				g.FillEllipse(brush, x + size * 0.35f - wheelRadius, y + size * 0.78f, wheelRadius * 2, wheelRadius * 2);
				g.FillEllipse(brush, x + size * 0.75f - wheelRadius, y + size * 0.78f, wheelRadius * 2, wheelRadius * 2);
			}
		}

		private void ResetForm()
		{
			_items.Clear();
			dgItems.Rows.Clear();
			lblTotalVal.Text = "0.00 ج";
			if (txtInvoiceDiscount != null) txtInvoiceDiscount.Text = "0";
			if (nudShippingCharge != null) nudShippingCharge.Value = 0;
			if (cboInvoiceDiscountType != null) cboInvoiceDiscountType.SelectedIndex = 0;
			if (lblNetVal != null) lblNetVal.Text = "0.00 ج";
			txtNotes.Clear();
			txtPrice.Clear();
			nudQty.Value = 1m;
			if (nudCratesOut != null) nudCratesOut.Value = 0;
			if (nudCratesIn != null) nudCratesIn.Value = 0;
			SetTierButtons("قطاعي");
			dtpDate.Value = DateTime.Today;
			SetInvoiceType(GetDefaultAllowedInvoiceType());
			Text = "شاشة المبيعات";
			_editSaleID = 0;
			_isCopyMode = false;
			_isDirty = false;

			// إعادة تحميل الكومبو لإعادة تعيين الفلترة والبحث ومنح تجربة سريعة بين الفواتير
			LoadCombos();

			this.BeginInvoke((MethodInvoker)delegate
			{
				if (txtProductCode != null)
				{
					this.ActiveControl = txtProductCode;
					txtProductCode.Focus();
				}
			});
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

            // ملء القائمة بالأعمدة (ما عدا عمود الحذف والأعمدة المعطلة لنوع النشاط)
            bool isClothingMode = AppConfig.BusinessType == "Clothing";
            foreach (DataGridViewColumn col in dgItems.Columns)
            {
                if (col.Name == "Delete") continue;
                if (isClothingMode && (col.Name == "CarModel" || col.Name == "Brand")) continue;
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

		public string ClientCode { get; set; } = "";
		public string Phone { get; set; } = "";
		public string Phone2 { get; set; } = "";

		public string PartNumber { get; set; } = "";
		public string CarModel { get; set; } = "";
		public string Brand { get; set; } = "";
		public string ProductSize { get; set; } = "";
		public string Color { get; set; } = "";
		public string ShelfLocation { get; set; } = "";
		public decimal PendingSalePrice { get; set; } = 0m;
		public decimal PendingQtyThreshold { get; set; } = 0m;
		public string ProductCode { get; set; } = "";
		public string InternationalCode { get; set; } = "";
		public string ScalePLU { get; set; } = "";
		/// <summary>صنف خدمة — يُباع بالسالب دون فحص المخزون</summary>
		public bool IsService { get; set; } = false;
		public bool HasExpiry { get; set; } = false;
		public int? DefaultExpiryDays { get; set; } = null;
		public string DefaultSaleUnit { get; set; } = "";

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


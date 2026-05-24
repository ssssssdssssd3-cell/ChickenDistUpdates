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

		private ComboBox cboProduct;

		private NumericUpDown nudQty;

		private TextBox txtPrice;

		private List<SaleItemDTO> _items = new List<SaleItemDTO>();

		private int _lastSaleID = 0;
        private bool _isDirty = false;

		public FrmSale()
		{
			InitUI();
			LoadCombos();
		}

		private void InitUI()
		{
			Text = "شاشة المبيعات";
			base.Size = new Size(950, 680);
			base.StartPosition = FormStartPosition.CenterScreen;
			RightToLeft = RightToLeft.Yes;
			RightToLeftLayout = true;
			BackColor = Theme.BgMain;
			Font = Theme.FontMain;
            this.FormClosing += FrmSale_FormClosing;
			Panel panel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 120,
				Width = 950,
				BackColor = Theme.BgCard,
				Padding = new Padding(10)
			};
			Label label = MakeLabel("نوع الفاتورة :", 850, 12);
			label.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnTypeCredit = new Button
			{
				Text = "آجل",
				Location = new Point(765, 8),
				Size = new Size(80, 28),
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			btnTypeCredit.FlatAppearance.BorderSize = 0;
			btnTypeCredit.Click += delegate
			{
				SetInvoiceType("Credit");
			};
			btnTypeCash = new Button
			{
				Text = "نقدي",
				Location = new Point(680, 8),
				Size = new Size(80, 28),
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			btnTypeCash.FlatAppearance.BorderSize = 0;
			btnTypeCash.Click += delegate
			{
				SetInvoiceType("Cash");
			};
			btnTypeDriverLoad = new Button
			{
				Text = "تحميل مندوب",
				Location = new Point(570, 8),
				Size = new Size(105, 28),
				Font = Theme.FontBold,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			btnTypeDriverLoad.FlatAppearance.BorderSize = 0;
			btnTypeDriverLoad.Click += delegate
			{
				SetInvoiceType("DriverLoad");
			};
			lblDate = MakeLabel("التاريخ :", 530, 12);
			lblDate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			dtpDate = new DateTimePicker
			{
				Location = new Point(400, 8),
				Width = 120,
				Format = DateTimePickerFormat.Short,
				RightToLeft = RightToLeft.Yes,
				RightToLeftLayout = true,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			lblClient = MakeLabel("العميل :", 330, 12);
			lblClient.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			cboClient = new ComboBox
			{
				Location = new Point(110, 8),
				Width = 210,
				DropDownStyle = ComboBoxStyle.DropDown,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			SetupSearchableCombo(cboClient);
			lblDriver = MakeLabel("المندوب :", 840, 48);
			lblDriver.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			cboDriver = new ComboBox
			{
				Location = new Point(630, 44),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDown,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			SetupSearchableCombo(cboDriver);
			Label label2 = MakeLabel("الصنف :", 560, 48);
			label2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			cboProduct = new ComboBox
			{
				Location = new Point(50, 44),
				Width = 500,
				DropDownStyle = ComboBoxStyle.DropDown,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				FlatStyle = FlatStyle.Flat,
				RightToLeft = RightToLeft.Yes,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			SetupSearchableCombo(cboProduct);
			btnSearchProduct = new Button
			{
				Text = "🔍",
				Location = new Point(10, 43),
				Size = new Size(35, 24),
				BackColor = Theme.Accent,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			btnSearchProduct.FlatAppearance.BorderSize = 0;
			btnSearchProduct.Click += BtnSearchProduct_Click;

			// Background initialization to prevent NullReferenceException:
			nudQty = new NumericUpDown { Value = 1m };
			txtPrice = new TextBox();
			btnAddItem = new Button();

			lblNotes = MakeLabel("ملاحظات :", 840, 84);
			lblNotes.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			txtNotes = new TextBox
			{
				Location = new Point(110, 80),
				Width = 720,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				BorderStyle = BorderStyle.FixedSingle,
				RightToLeft = RightToLeft.Yes,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			panel.Controls.AddRange(new Control[]
			{
				label, btnTypeCredit, btnTypeCash, btnTypeDriverLoad, lblDate, dtpDate, lblClient, cboClient, lblDriver, cboDriver,
				label2, cboProduct, btnSearchProduct, lblNotes, txtNotes
			});
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
			DataGridViewButtonColumn dataGridViewColumn = new DataGridViewButtonColumn
			{
				Name = "Delete",
				HeaderText = "",
				Text = "\ud83d\uddd1",
				UseColumnTextForButtonValue = true,
				FillWeight = 20f
			};
			dgItems.Columns.Add(dataGridViewColumn);
			dgItems.Columns.Add(dataGridViewColumn);
			dgItems.CellClick += DgItems_CellClick;
			dgItems.CellEndEdit += DgItems_CellEndEdit;
            dgItems.RowsAdded += (s, e) => _isDirty = true;
            dgItems.RowsRemoved += (s, e) => _isDirty = true;
			pnlItems.Controls.Add(dgItems);
			pnlFooter = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 95,
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
			btnSave = Theme.MakeButton("💾 حفظ الفاتورة", 780, 50, 130, 32, Theme.Accent);
            Button btnHold = Theme.MakeButton("⏸️ تعليق", 670, 50, 100, 32, Color.FromArgb(200, 140, 50));
			Button btnLoadHold = Theme.MakeButton("📂 معلقات", 560, 50, 100, 32, Color.FromArgb(100, 100, 150));
			Button button = Theme.MakeButton("💵 توريد", 450, 50, 100, 32, Theme.Success);
			btnNew = Theme.MakeButton("🆕 جديد", 360, 50, 80, 32, Color.FromArgb(80, 120, 80));
			btnPrint = Theme.MakeButton("🖨️ طباعة الأخيرة", 200, 50, 150, 32, Theme.Primary);
			btnWhatsApp = Theme.MakeButton("📲 واتساب", 30, 50, 160, 32, Color.FromArgb(37, 211, 102));
			btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnHold.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoadHold.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnNew.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnPrint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnWhatsApp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
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
			pnlFooter.Controls.AddRange(new Control[] { label5, lblTotalVal, lblDiscType, cboInvoiceDiscountType, lblDiscVal, txtInvoiceDiscount, lblNetTitle, lblNetVal, btnSave, btnHold, btnLoadHold, button, btnNew, btnPrint, btnWhatsApp });
			base.Controls.Add(pnlItems);
			base.Controls.Add(pnlFooter);
			base.Controls.Add(panel);
            pnlItems.BringToFront();
			ToggleType();
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
						if (item2.ID == 0 || item2.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
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
				cboProduct.Items.Add(new ComboItem((int)row3["ProductID"], row3["ProductName"].ToString(), (decimal)row3["SalePrice"]));
			}
			cboProduct.DisplayMember = "Text";
			cboProduct.SelectedIndex = 0;
			cboProduct.SelectedIndexChanged += delegate
			{
				if (cboProduct.SelectedItem is ComboItem comboItem && comboItem.ID > 0)
				{
					// Check if product is already in the list
					foreach (SaleItemDTO item in _items)
					{
						if (item.ProductID == comboItem.ID)
						{
							MessageBox.Show("الصنف موجود مسبقاً بالفاتورة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							cboProduct.SelectedIndex = 0;
							return;
						}
					}

					// Verify stock availability
					decimal stock = InventoryDAL.GetProductStock(comboItem.ID);
					if (stock <= 0)
					{
						MessageBox.Show($"❌ عجز: الصنف '{comboItem.Text}' ليس لديه رصيد كافٍ في المخزن حالياً (الرصيد الحالي: 0)!", "رصيد غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						cboProduct.SelectedIndex = 0;
						return;
					}

					// Add to items list with default Quantity = 1
					_items.Add(new SaleItemDTO
					{
						ProductID = comboItem.ID,
						ProductName = comboItem.Text,
						Quantity = 1.00m,
						UnitPrice = comboItem.Price,
						StockQty = stock
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
			for (int i = 0; i < cboProduct.Items.Count; i++)
			{
				if (cboProduct.Items[i] is ComboItem comboItem && comboItem.ID == prodID)
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
			decimal productStock = InventoryDAL.GetProductStock(comboItem.ID);
			if (value > productStock)
			{
				MessageBox.Show($"❌ خطأ: الكمية المطلوبة ({value:N2}) أكبر من الكمية المتاحة في المخزن حاليا\u064b ({productStock:N2})!", "تنبيه - رصيد غير كاف\u064d", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			foreach (SaleItemDTO item in _items)
			{
				if (item.ProductID == comboItem.ID)
				{
					MessageBox.Show("الصنف موجود مسبقا\u064b");
					return;
				}
			}
			_items.Add(new SaleItemDTO
			{
				ProductID = comboItem.ID,
				ProductName = comboItem.Text,
				Quantity = value,
				UnitPrice = result,
				StockQty = productStock
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
					decimal productStock = InventoryDAL.GetProductStock(saleItemDTO.ProductID);
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
				if (decimal.TryParse(dataGridViewRow.Cells["UnitPrice"].Value?.ToString(), out var result2) && result2 >= 0m)
				{
					saleItemDTO.UnitPrice = result2;
					// Recalculate discount amount based on percentage
					decimal gross = saleItemDTO.Quantity * saleItemDTO.UnitPrice;
					saleItemDTO.DiscountAmt = Math.Round(gross * saleItemDTO.DiscountPct / 100m, 2);
				}
				else
				{
					MessageBox.Show("من فضلك أدخل سعر صحيح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
				dgItems.Rows.Add(
					item.ProductName,
					item.StockQty.ToString("F2"),
					item.Quantity.ToString("F2"),
					item.UnitPrice.ToString("F2"),
					item.DiscountPct.ToString("F2"),
					item.DiscountAmt.ToString("F2"),
					item.TotalPrice.ToString("F2")
				);
			}
			CalculateNet();
		}

		private void CalculateNet()
		{
			decimal gross = 0m;
			foreach (SaleItemDTO item in _items)
			{
				gross += item.TotalPrice;
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

		private void SaveInvoiceLogic(bool isDraft)
		{
			if (_items.Count == 0)
			{
				MessageBox.Show("أضف أصناف أولاً");
				return;
			}
			foreach (SaleItemDTO item in _items)
			{
				decimal productStock = InventoryDAL.GetProductStock(item.ProductID);
				if (item.Quantity > productStock)
				{
					MessageBox.Show($"❌ خطأ: الصنف '{item.ProductName}' لا يوجد منه رصيد كافٍ في المخزن حالياً لحفظ الفاتورة.\nالكمية المطلوبة: {item.Quantity:N2}\nالكمية المتاحة: {productStock:N2}", "عجز في الرصيد", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
			int saleType = ((!(_invoiceType == "Credit")) ? ((_invoiceType == "DriverLoad") ? 1 : 2) : 0);
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
				{
					driverID = comboItem2.ID;
				}
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
			foreach (SaleItemDTO item2 in _items)
			{
				gross += item2.TotalPrice;
			}
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
					if (gross > 0)
					{
						discountPct = Math.Round((discountAmount / gross) * 100m, 2);
					}
				}
			}
			decimal net = Math.Max(0m, gross - discountAmount);

			if (!isDraft && _invoiceType == "Credit" && clientID.HasValue)
			{
				DataRow byID = ClientDAL.GetByID(clientID.Value);
				if (byID != null)
				{
					decimal num2 = Convert.ToDecimal((byID["MaxCreditLimit"] == DBNull.Value) ? ((object)0) : byID["MaxCreditLimit"]);
					if (num2 > 0m)
					{
						decimal clientBalance = ClientDAL.GetClientBalance(clientID.Value);
						if (clientBalance + net > num2)
						{
							MessageBox.Show($"❌ خطأ: الرصيد الحالي للعميل ({clientBalance:N2} ج) بالإضافة إلى قيمة الفاتورة الحالية ({net:N2} ج) يساوي ({clientBalance + net:N2} ج)، وهو ما يتجاوز حد المديونية الأقصى المسموح به لهذا العميل ({num2:N2} ج)!\n\nيرجى تحصيل دفعة من العميل أولاً لحفظ الفاتورة.", "تنبيه - تجاوز حد المديونية الأقصى", MessageBoxButtons.OK, MessageBoxIcon.Hand);
							return;
						}
					}
				}
			}
			int num3 = SaleDAL.SaveSale(saleType, clientID, driverID, net, txtNotes.Text, _items, discountAmount, discountPct, isDraft);
			if (num3 > 0)
			{
				_lastSaleID = num3;
				_isDirty = false;
				if (isDraft)
				{
					MessageBox.Show($"✅ تم تعليق الفاتورة بنجاح.\nيمكنك استدعاؤها لاحقاً من زر 📂 معلقات.", "تعليق", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					DialogResult printResult = MessageBox.Show($"✅ تم حفظ الفاتورة بنجاح رقم [{num3}]!\n\nهل تريد طباعة الفاتورة الآن؟", "نجاح الحفظ والطباعة", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (printResult == DialogResult.Yes)
					{
						new FrmPrintSale(num3);
					}
				}
				ResetForm();
			}
			else
			{
				MessageBox.Show("❌ فشل الحفظ، راجع الاتصال بقاعدة البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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

			var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Theme.BgCard, Padding = new Padding(5) };

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
						DiscountAmt = Convert.ToDecimal(iRow["DiscountAmt"])
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
			}
			else
			{
				new FrmPrintSale(printID);
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
			if (txtInvoiceDiscount != null)
			{
				txtInvoiceDiscount.Text = "0";
			}
			if (cboInvoiceDiscountType != null)
			{
				cboInvoiceDiscountType.SelectedIndex = 0;
			}
			if (lblNetVal != null)
			{
				lblNetVal.Text = "0.00 ج";
			}
			txtNotes.Clear();
			txtPrice.Clear();
			nudQty.Value = 1m;
			if (cboClient.Items.Count > 0)
			{
				cboClient.SelectedIndex = 0;
			}
			if (cboDriver.Items.Count > 0)
			{
				cboDriver.SelectedIndex = 0;
			}
			if (cboProduct.Items.Count > 0)
			{
				cboProduct.SelectedIndex = 0;
			}
			dtpDate.Value = DateTime.Today;
			SetInvoiceType("Credit");
            _isDirty = false;
		}
	}
	internal class ComboItem
	{
		public int ID { get; }

		public string Text { get; }

		public decimal Price { get; }

		public ComboItem(int id, string text, decimal price = 0m)
		{
			ID = id;
			Text = text;
			Price = price;
		}

		public override string ToString()
		{
			return Text;
		}
	}
}


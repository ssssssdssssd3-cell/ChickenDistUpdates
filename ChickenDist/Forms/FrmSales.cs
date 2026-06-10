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
		private ComboBox cboClientFilter;
		private TextBox txtProductSearch;
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
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = ScreenHelper.IsSmallScreen ? 90 : 50,
				FlowDirection = FlowDirection.RightToLeft,
				BackColor = Theme.BgCard,
				Padding = new Padding(10),
				WrapContents = true
			};
			Label label = new Label
			{
				Text = "من:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Location = new Point(10, 16)
			};
			dtpFrom = new DateTimePicker
			{
				Width = 110,
				Height = 26,
				Format = DateTimePickerFormat.Short,
				Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
				Location = new Point(35, 12)
			};
			flowLayoutPanel.Controls.AddRange(new Control[2] { label, dtpFrom });
			Label label2 = new Label
			{
				Text = "إلى:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Location = new Point(155, 16)
			};
			dtpTo = new DateTimePicker
			{
				Width = 110,
				Height = 26,
				Format = DateTimePickerFormat.Short,
				Location = new Point(185, 12)
			};
			flowLayoutPanel.Controls.AddRange(new Control[2] { label2, dtpTo });
			Label label3 = new Label
			{
				Text = "النوع:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Location = new Point(305, 16)
			};
			cboTypeFilter = new ComboBox
			{
				Width = 110,
				Height = 26,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes,
				Location = new Point(345, 12)
			};
			cboTypeFilter.Items.AddRange(new object[4] { "الكل", "نقدي (Cash)", "آجل (Credit)", "تحميل مندوب" });
			cboTypeFilter.SelectedIndex = 0;
			flowLayoutPanel.Controls.AddRange(new Control[2] { label3, cboTypeFilter });

			Label labelClient = new Label
			{
				Text = "العميل:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Location = new Point(465, 16)
			};
			cboClientFilter = new ComboBox
			{
				Width = 130,
				Height = 26,
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes,
				Location = new Point(515, 12)
			};
			cboClientFilter.Items.Add(new ComboItem(0, "الكل"));
			foreach (DataRow row in ClientDAL.GetAll(true).Rows)
			{
				cboClientFilter.Items.Add(new ComboItem((int)row["ClientID"], row["ClientName"].ToString()));
			}
			cboClientFilter.DisplayMember = "Text";
			cboClientFilter.SelectedIndex = 0;
			cboClientFilter.SelectedIndexChanged += delegate
			{
				LoadSales();
			};
			flowLayoutPanel.Controls.AddRange(new Control[2] { labelClient, cboClientFilter });

			Label labelProduct = new Label
			{
				Text = "بحث صنف:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Location = new Point(655, 16)
			};
			txtProductSearch = new TextBox
			{
				Width = 120,
				Height = 26,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes,
				Location = new Point(725, 12)
			};
			txtProductSearch.KeyDown += (s, e) =>
			{
				if (e.KeyCode == Keys.Enter)
				{
					LoadSales();
					e.Handled = true;
					e.SuppressKeyPress = true;
				}
			};
			flowLayoutPanel.Controls.AddRange(new Control[2] { labelProduct, txtProductSearch });
			Label label4 = new Label
			{
				Text = "بحث:",
				AutoSize = true,
				ForeColor = Theme.TextMain,
				Location = new Point(855, 16)
			};
			txtSearch = new TextBox
			{
				Width = 120,
				Height = 26,
				BackColor = Theme.BgInput,
				ForeColor = Theme.TextMain,
				RightToLeft = RightToLeft.Yes,
				Location = new Point(895, 12)
			};
			txtSearch.TextChanged += delegate
			{
				FilterData();
			};
			flowLayoutPanel.Controls.AddRange(new Control[2] { label4, txtSearch });
			btnLoad = Theme.MakeButton("🔄 عرض", Theme.Accent);
			btnLoad.Size = new Size(80, 26);
			btnLoad.Location = new Point(1025, 12);
			btnLoad.Click += delegate
			{
				LoadSales();
			};
			flowLayoutPanel.Controls.Add(btnLoad);
			btnNewSale = Theme.MakeButton("➕ فاتورة جديدة", Color.FromArgb(40, 150, 80));
			btnNewSale.Size = new Size(120, 26);
			btnNewSale.Location = new Point(1115, 12);
			btnNewSale.Click += delegate
			{
				FrmSale frmSale = new FrmSale();
				frmSale.ShowDialog();
				LoadSales();
			};
			flowLayoutPanel.Controls.Add(btnNewSale);
			Panel panel = new Panel
			{
				Dock = DockStyle.Top,
				Height = ScreenHelper.IsSmallScreen ? 240 : 280,
				Padding = new Padding(10, 0, 10, 10)
			};
			dgSales = MakeGrid();
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
			panel.Controls.Add(dgSales);
			Panel panel2 = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(10, 0, 10, 10)
			};
			FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
			{
				Dock = DockStyle.Left,
				Width = 220,
				FlowDirection = FlowDirection.TopDown,
				BackColor = Theme.BgCard,
				Padding = new Padding(15),
				WrapContents = false
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
			panel2.Controls.Add(flowLayoutPanel2);
			panel2.Controls.Add(dgItems);
			TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 70,
				ColumnCount = 6,
				RowCount = 1,
				RightToLeft = RightToLeft.Yes,
				BackColor = Theme.BgCard,
				Padding = new Padding(10, 5, 10, 5)
			};
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			lblTotalSummary  = AddDashboardCard(tableLayoutPanel, "إجمالي الفواتير:",        "0.00 ج", Theme.Accent,                  0);
			lblReturnSummary = AddDashboardCard(tableLayoutPanel, "إجمالي المرتجعات: ↩",   "0.00 ج", Color.FromArgb(231, 76, 60),   1);
			lblNetSummary    = AddDashboardCard(tableLayoutPanel, "الصافي بعد المرتجع: ✔", "0.00 ج", Color.FromArgb(46, 204, 113),  2);
			lblCashSummary   = AddDashboardCard(tableLayoutPanel, "المبيعات النقدية:",       "0.00 ج", Theme.Success,                 3);
			lblCreditSummary = AddDashboardCard(tableLayoutPanel, "المبيعات الآجلة:",       "0.00 ج", Color.FromArgb(52, 152, 219),  4);
			lblDriverSummary = AddDashboardCard(tableLayoutPanel, "حمولات المناديب:",        "0.00 ج", Color.FromArgb(155, 89, 182), 5);
			// ترتيب صحيح: Bottom ثم Top ثم Fill
			base.Controls.Add(tableLayoutPanel);  // Bottom - يُضاف أولاً
			base.Controls.Add(flowLayoutPanel);   // Top (filter bar)
			base.Controls.Add(panel);             // Top (master grid)
			base.Controls.Add(panel2);            // Fill - يُضاف أخيراً

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

		private void LoadSales()
		{
			dgSales.Rows.Clear();
			dgItems.Rows.Clear();
			int? clientID = null;
			if (cboClientFilter != null && cboClientFilter.SelectedItem is ComboItem ci && ci.ID > 0)
			{
				clientID = ci.ID;
			}
			string productSearch = (txtProductSearch != null && !string.IsNullOrWhiteSpace(txtProductSearch.Text)) ? txtProductSearch.Text.Trim() : null;
			_allSalesDt = SaleDAL.GetAll(dtpFrom.Value, dtpTo.Value, clientID, productSearch);
			FilterData();
		}

		private void FilterData()
		{
			dgSales.Rows.Clear();
			dgItems.Rows.Clear();
			if (_allSalesDt == null || _allSalesDt.Rows.Count == 0)
			{
				UpdateSummary(0m, 0m, 0m, 0m, 0m);
				return;
			}
			string value = txtSearch.Text.Trim().ToLower();
			string text = cboTypeFilter.SelectedItem?.ToString() ?? "الكل";
			decimal tot    = 0m;
			decimal ret    = 0m;
			decimal cash   = 0m;
			decimal credit = 0m;
			decimal driver = 0m;
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

				if ((!(text != "الكل") || ((!text.Contains("نقدي") || !(text5 != "Cash")) && (!text.Contains("آجل") || !(text5 != "Credit")) && (!text.Contains("تحميل") || !(text5 != "DriverLoad")))) && (string.IsNullOrEmpty(value) || text2.Contains(value) || text7.ToLower().Contains(value) || text6.Contains(value)))
				{
					decimal num = Convert.ToDecimal(row["TotalAmount"]);
					decimal returnAmt = row.Table.Columns.Contains("ReturnAmount") && row["ReturnAmount"] != DBNull.Value
					                    ? Convert.ToDecimal(row["ReturnAmount"]) : 0m;
					decimal netAmt = num - returnAmt;

					tot += num;
					ret += returnAmt;
					switch (text5)
					{
						case "Cash":       cash   += num; break;
						case "Credit":     credit += num; break;
						case "DriverLoad": driver += num; break;
					}
					string text8 = (text5 == "Credit") ? "آجل" : (text5 == "Cash") ? "نقدي" : "تحميل مندوب";
					string retStr = returnAmt > 0 ? returnAmt.ToString("N2") + " ج" : "-";
					dgSales.Rows.Add(
						row["SaleID"], row["SaleCode"],
						Convert.ToDateTime(row["SaleDate"]).ToString("dd/MM/yyyy HH:mm"),
						text8, text7,
						num.ToString("N2") + " ج",
						retStr,
						netAmt.ToString("N2") + " ج",
						row.Table.Columns.Contains("CreatedByName") ? row["CreatedByName"].ToString() : "---",
						row["Notes"]);
				}
			}
			UpdateSummary(tot, ret, cash, credit, driver);
		}

		private void UpdateSummary(decimal tot, decimal ret, decimal cash, decimal credit, decimal driver)
		{
			lblTotalSummary.Text  = tot.ToString("N2")         + " ج";
			lblReturnSummary.Text = ret.ToString("N2")         + " ج";
			lblNetSummary.Text    = (tot - ret).ToString("N2") + " ج";
			lblCashSummary.Text   = cash.ToString("N2")        + " ج";
			lblCreditSummary.Text = credit.ToString("N2")      + " ج";
			lblDriverSummary.Text = driver.ToString("N2")      + " ج";
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
				MessageBox.Show("من فضلك اختر الفاتورة المراد طباعتها أولا\u064b من الجدول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			int saleID = Convert.ToInt32(dgSales.SelectedRows[0].Cells["SaleID"].Value);

			var menu = new ContextMenuStrip();
			var itemReceipt = new ToolStripMenuItem("🧾 طباعة ريسيت حراري (Receipt)");
			itemReceipt.Click += (s2, e2) => new FrmPrintSale(saleID, "Receipt");
            
			var itemA4 = new ToolStripMenuItem("📄 طباعة فاتورة ورق (A4/A5)");
			itemA4.Click += (s2, e2) => new FrmPrintSale(saleID, "A4");

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

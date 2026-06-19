using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة مرتجع مشتريات — مطابقة للمنطق المحاسبي الصحيح</summary>
    public class FrmPurchaseReturn : Form
    {
        private ComboBox cboPurchase, cboSupplier;
        private TextBox txtNotes, txtProductSearch;
        private DataGridView dgItems;
        private Button btnSave;
        private Label lblTotal;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnSearch;
        private Label lblPurchaseInfo;

        public FrmPurchaseReturn()
        {
            InitUI();
            LoadCombos();
        }

        private void FrmPurchaseReturn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
                btnSave.PerformClick();
                e.Handled = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                if (dgItems.Focused || dgItems.EditingControl != null)
                {
                    dgItems.EndEdit();
                    var curCell = dgItems.CurrentCell;
                    if (curCell != null && curCell.RowIndex >= 0 && curCell.RowIndex < dgItems.Rows.Count)
                    {
                        int nextCol = -1;
                        for (int col = curCell.ColumnIndex + 1; col < dgItems.ColumnCount; col++)
                        {
                            if (!dgItems.Columns[col].ReadOnly && dgItems.Columns[col].Visible)
                            { nextCol = col; break; }
                        }
                        if (nextCol != -1)
                        {
                            dgItems.CurrentCell = dgItems.Rows[curCell.RowIndex].Cells[nextCol];
                            dgItems.BeginEdit(true);
                            return true;
                        }
                        else
                        {
                            cboPurchase.Focus();
                            return true;
                        }
                    }
                    else
                    {
                        cboPurchase.Focus();
                        return true;
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitUI()
        {
            this.Text = "مرتجع مشتريات";
            this.Size = new Size(1050, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmPurchaseReturn_KeyDown;

            // ===== شريط العنوان =====
            var pnlTitle = Theme.MakeTitleBar("↩ مرتجع مشتريات", "إرجاع بضاعة للمورد مع تسوية الحساب تلقائياً");
            pnlTitle.Dock = DockStyle.Top;

            // ===== شريط الفلتر والبيانات =====
            var pnlInfo = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 115,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 12, 10, 10),
                WrapContents = true
            };

            // تواريخ البحث
            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 0, 0), Font = Theme.FontBold };
            dtpFrom = new DateTimePicker { Width = 120, Height = 26, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            dtpTo = new DateTimePicker { Width = 120, Height = 26, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            btnSearch = Theme.MakeButton("🔍 جلب الفواتير", Theme.Accent);
            btnSearch.Size = new Size(130, 28);
            btnSearch.Margin = new Padding(10, 0, 0, 0);
            btnSearch.Click += (s, e) => LoadCombos();

            // فاتورة الشراء
            var lblPur = new Label { Text = "فاتورة الشراء الأصلية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            cboPurchase = new ComboBox
            {
                Width = 270, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            cboPurchase.SelectedIndexChanged += CboPurchase_SelectedIndexChanged;

            // معلومات الفاتورة المختارة
            lblPurchaseInfo = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 200, 140),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(10, 8, 0, 0),
                Text = ""
            };

            // المورد
            var lblSup = new Label { Text = "المورد:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            cboSupplier = new ComboBox
            {
                Width = 200, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            // بحث صنف
            var lblProduct = new Label { Text = "بحث صنف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            txtProductSearch = new TextBox 
            { 
                Width = 150, 
                Height = 26, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                RightToLeft = RightToLeft.Yes, 
                BorderStyle = BorderStyle.FixedSingle 
            };
            txtProductSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    LoadPurchasesCombo();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            // ملاحظات
            var lblNotes = new Label { Text = "ملاحظات:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            txtNotes = new TextBox { Width = 220, Height = 26, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle };

            pnlInfo.Controls.AddRange(new Control[] {
                lblFrom, dtpFrom, lblTo, dtpTo, btnSearch,
                lblPur, cboPurchase, lblPurchaseInfo,
                lblSup, cboSupplier,
                lblProduct, txtProductSearch,
                lblNotes, txtNotes
            });

            // ===== الجدول =====
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 100, 60),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID",       Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName",     HeaderText = "الصنف",                     ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasedQty",    HeaderText = "الكمية الأصلية بالفاتورة",  ReadOnly = true, FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedQty", HeaderText = "المرتجع السابق",            ReadOnly = true, FillWeight = 40 });

            var colNew = new DataGridViewTextBoxColumn
            {
                Name = "NewReturnedQty",
                HeaderText = "المرتجع الجديد (تعديل مباشر)",
                ReadOnly = false,
                FillWeight = 55,
                ValueType = typeof(decimal)
            };
            colNew.DefaultCellStyle.BackColor = Color.FromArgb(40, 55, 40);
            colNew.DefaultCellStyle.ForeColor = Color.LightGreen;
            colNew.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgItems.Columns.Add(colNew);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",  HeaderText = "سعر الشراء الأصلي",  ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "إجمالي المرتجع",     ReadOnly = true, FillWeight = 50 });

            dgItems.CellValidating  += DgItems_CellValidating;
            dgItems.CellValueChanged += DgItems_CellValueChanged;

            pnlGrid.Controls.Add(dgItems);

            // ===== الذيل =====
            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            lblTotal = new Label
            {
                Text = "الإجمالي: 0.00 ج",
                ForeColor = Color.FromArgb(80, 200, 120),
                Dock = DockStyle.Right,
                Width = 260,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            btnSave = Theme.MakeButton("💾 حفظ مرتجع الشراء", Color.FromArgb(50, 110, 60));
            btnSave.Dock = DockStyle.Left;
            btnSave.Width = 200;
            btnSave.Font = Theme.FontBold;
            btnSave.Click += BtnSave_Click;

            var lblHotkeys = new Label
            {
                Text = "الاختصارات: [F5] حفظ المرتجع",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(10, 20),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            pnlFoot.Controls.AddRange(new Control[] { lblTotal, btnSave, lblHotkeys });

            // ===== تجميع =====
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlTitle);

            pnlGrid.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private void LoadPurchasesCombo()
        {
            cboPurchase.SelectedIndexChanged -= CboPurchase_SelectedIndexChanged;

            int? supplierID = null;
            if (cboSupplier.SelectedItem is ComboItem cs && cs.ID > 0)
                supplierID = cs.ID;

            string prodSearch = txtProductSearch != null ? txtProductSearch.Text.Trim() : null;
            if (string.IsNullOrEmpty(prodSearch)) prodSearch = null;

            var dtP = PurchaseDAL.GetAll(dtpFrom.Value.Date, dtpTo.Value.Date, supplierID, prodSearch);
            cboPurchase.Items.Clear();
            cboPurchase.Items.Add(new ComboItem(0, "-- اختر فاتورة الشراء الأصلية --"));
            foreach (DataRow r in dtP.Rows)
                cboPurchase.Items.Add(new ComboItem((int)r["PurchaseID"],
                    $"{r["PurchaseCode"]} | {r["SupplierName"]} | {Convert.ToDecimal(r["TotalAmount"]):N2} ج"));
            cboPurchase.DisplayMember = "Text";

            cboPurchase.SelectedIndexChanged += CboPurchase_SelectedIndexChanged;
            if (cboPurchase.Items.Count > 0)
                cboPurchase.SelectedIndex = 0;
        }

        private void CboSupplier_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboPurchase.SelectedIndex <= 0)
            {
                LoadPurchasesCombo();
            }
        }

        private void LoadCombos()
        {
            cboSupplier.SelectedIndexChanged -= CboSupplier_SelectedIndexChanged;

            var dtS = SupplierDAL.GetAll(true);
            cboSupplier.Items.Clear();
            cboSupplier.Items.Add(new ComboItem(0, "-- اختر مورد --"));
            foreach (DataRow r in dtS.Rows)
                cboSupplier.Items.Add(new ComboItem((int)r["SupplierID"], r["SupplierName"].ToString()));
            cboSupplier.DisplayMember = "Text";
            cboSupplier.SelectedIndex = 0;

            cboSupplier.SelectedIndexChanged += CboSupplier_SelectedIndexChanged;

            LoadPurchasesCombo();
        }

        private void CboPurchase_SelectedIndexChanged(object sender, EventArgs e)
        {
            dgItems.Rows.Clear();
            lblTotal.Text = "الإجمالي: 0.00 ج";
            lblPurchaseInfo.Text = "";

            if (!(cboPurchase.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                cboSupplier.SelectedIndexChanged -= CboSupplier_SelectedIndexChanged;
                cboSupplier.Enabled = true;
                if (cboSupplier.Items.Count > 0)
                    cboSupplier.SelectedIndex = 0;
                cboSupplier.SelectedIndexChanged += CboSupplier_SelectedIndexChanged;
                return;
            }

            int purchaseID = ci.ID;

            // تلقائياً اختر المورد من الفاتورة
            var dtPur = DbHelper.Query(
                "SELECT SupplierID, PurchaseType FROM Purchases WHERE PurchaseID=@pid",
                DbHelper.P("@pid", purchaseID));
            if (dtPur.Rows.Count > 0)
            {
                string purType = dtPur.Rows[0]["PurchaseType"].ToString();
                lblPurchaseInfo.Text = $"نوع الفاتورة: {(purType == "Cash" ? "🟡 نقدي" : "🔵 آجل")}";

                if (dtPur.Rows[0]["SupplierID"] != DBNull.Value)
                {
                    int sid = Convert.ToInt32(dtPur.Rows[0]["SupplierID"]);
                    cboSupplier.SelectedIndexChanged -= CboSupplier_SelectedIndexChanged;
                    for (int i = 0; i < cboSupplier.Items.Count; i++)
                        if (cboSupplier.Items[i] is ComboItem item && item.ID == sid)
                        { cboSupplier.SelectedIndex = i; break; }
                    cboSupplier.SelectedIndexChanged += CboSupplier_SelectedIndexChanged;
                    cboSupplier.Enabled = false; // Lock supplier selection
                }
                else
                {
                    cboSupplier.SelectedIndexChanged -= CboSupplier_SelectedIndexChanged;
                    if (cboSupplier.Items.Count > 0)
                        cboSupplier.SelectedIndex = 0;
                    cboSupplier.SelectedIndexChanged += CboSupplier_SelectedIndexChanged;
                    cboSupplier.Enabled = true;
                }
            }

            // تحميل أصناف الفاتورة مع المرتجع السابق
            var dtItems = PurchaseDAL.GetItems(purchaseID);
            var dtPrevRet = DbHelper.Query(
                @"SELECT pri.ProductID, ISNULL(SUM(pri.Quantity),0) AS ReturnedQty
                  FROM PurchaseReturnItems pri
                  JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                  WHERE pr.PurchaseID = @pid
                  GROUP BY pri.ProductID",
                DbHelper.P("@pid", purchaseID));

            var prevMap = new System.Collections.Generic.Dictionary<int, decimal>();
            foreach (DataRow r in dtPrevRet.Rows)
                prevMap[Convert.ToInt32(r["ProductID"])] = Convert.ToDecimal(r["ReturnedQty"]);

            foreach (DataRow r in dtItems.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                decimal purQty = Convert.ToDecimal(r["Quantity"]);
                decimal prevRet = prevMap.ContainsKey(pid) ? prevMap[pid] : 0m;
                decimal remaining = purQty - prevRet;

                if (remaining <= 0) continue; // تم إرجاع الكل مسبقاً

                int rowIdx = dgItems.Rows.Add();
                var row = dgItems.Rows[rowIdx];
                row.Cells["ProductID"].Value       = pid;
                row.Cells["ProductName"].Value     = r["ProductName"].ToString();
                row.Cells["PurchasedQty"].Value    = purQty.ToString("N3");
                row.Cells["PrevReturnedQty"].Value = prevRet.ToString("N3");
                row.Cells["NewReturnedQty"].Value  = 0;
                row.Cells["UnitPrice"].Value       = Convert.ToDecimal(r["UnitPrice"]).ToString("N2");
                row.Cells["TotalPrice"].Value      = "0.00";
            }

            if (dgItems.Rows.Count > 0)
                dgItems.CurrentCell = dgItems.Rows[0].Cells["NewReturnedQty"];
        }

        private void DgItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dgItems.Columns[e.ColumnIndex].Name != "NewReturnedQty") return;
            if (e.FormattedValue?.ToString() == "") return;
            if (!decimal.TryParse(e.FormattedValue.ToString(), out decimal val) || val < 0)
            {
                MessageBox.Show("أدخل كمية صالحة (رقم موجب أو صفر)", "تحقق من الإدخال",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgItems.Columns[e.ColumnIndex].Name != "NewReturnedQty") return;
            var row = dgItems.Rows[e.RowIndex];
            decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal qty);
            decimal.TryParse(row.Cells["UnitPrice"].Value?.ToString(), out decimal price);
            row.Cells["TotalPrice"].Value = (qty * price).ToString("N2");
            RecalcTotal();
        }

        private void RecalcTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["TotalPrice"].Value != null)
                    decimal.TryParse(row.Cells["TotalPrice"].Value.ToString(), out decimal rowTotal);
                decimal.TryParse(row.Cells["TotalPrice"].Value?.ToString(), out decimal t);
                total += t;
            }
            lblTotal.Text = "الإجمالي: " + total.ToString("N2") + " ج";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!(cboPurchase.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("يجب اختيار فاتورة الشراء الأصلية أولاً", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var returnItems = new List<PurchaseItemDTO>();
            decimal totalReturnAmount = 0;

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                int prodID   = Convert.ToInt32(row.Cells["ProductID"].Value);
                string name  = row.Cells["ProductName"].Value.ToString();
                decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal newQty);
                if (newQty <= 0) continue;

                decimal.TryParse(row.Cells["PurchasedQty"].Value?.ToString(), out decimal purQty);
                decimal.TryParse(row.Cells["PrevReturnedQty"].Value?.ToString(), out decimal prevQty);

                if (newQty + prevQty > purQty)
                {
                    MessageBox.Show(
                        $"الكمية المرتجعة للصنف ({name}) تتجاوز الكمية الأصلية!\n" +
                        $"المشتريات: {purQty:N3} | السابق: {prevQty:N3} | الجديد: {newQty:N3}",
                        "تجاوز الكمية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                decimal.TryParse(row.Cells["UnitPrice"].Value?.ToString(), out decimal price);
                returnItems.Add(new PurchaseItemDTO
                {
                    ProductID = prodID, ProductName = name,
                    Quantity = newQty, UnitPrice = price
                });
                totalReturnAmount += newQty * price;
            }

            if (returnItems.Count == 0)
            {
                MessageBox.Show("يرجى إدخال كمية مرتجعة صالحة (أكبر من صفر) لصنف واحد على الأقل.",
                    "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int purchaseID = ci.ID;
            int? supplierID = (cboSupplier.SelectedItem is ComboItem cs && cs.ID > 0) ? (int?)cs.ID : null;

            try
            {
                int id = PurchaseReturnDAL.SavePurchaseReturn(purchaseID, supplierID, totalReturnAmount,
                    txtNotes.Text, returnItems);
                if (id > 0)
                {
                    MessageBox.Show("✅ تم حفظ مرتجع الشراء بنجاح!\nتم تحديث حساب المورد والخزنة تلقائياً.",
                        "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtNotes.Text = "";
                    cboPurchase.SelectedIndex = 0;
                    dgItems.Rows.Clear();
                    lblTotal.Text = "الإجمالي: 0.00 ج";
                    lblPurchaseInfo.Text = "";
                }
                else
                {
                    MessageBox.Show("فشل حفظ المرتجع", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل حفظ مرتجع المشتريات", ex, "FrmPurchaseReturn.BtnSave_Click");
                MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

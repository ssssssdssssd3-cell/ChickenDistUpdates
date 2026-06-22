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
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasedQty",    HeaderText = "الكمية الأصلية",            ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedQty", HeaderText = "المرتجع السابق",            ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewComboBoxColumn { Name = "UnitName", HeaderText = "الوحدة", ReadOnly = false, FillWeight = 40 });

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

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",  HeaderText = "سعر المرتجع",        ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "إجمالي المرتجع",     ReadOnly = true, FillWeight = 50 });

            // Hidden helper columns
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "OriginalFactor", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "OriginalUnitPrice", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasedQtyInSmallest", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedQtyInSmallest", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseUnitName", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit1Name", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit1PurchasePrice", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Name", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Factor", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2PurchasePrice", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit3Factor", Visible = false });

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
            dgItems.CellValueChanged -= DgItems_CellValueChanged;
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
                dgItems.CellValueChanged += DgItems_CellValueChanged;
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
            var dtItems = DbHelper.Query(
                @"SELECT pi.ProductID, pr.ProductCode, pr.ProductName, pi.Quantity, pi.UnitPrice, pi.TotalPrice,
                         pi.UnitName, COALESCE(pi.Factor, 1.0) AS Factor,
                         pr.Unit AS BaseUnitName,
                         pr.Unit1Name,
                         pr.Unit1PurchasePrice,
                         pr.Unit2Name,
                         pr.Unit2Factor,
                         pr.Unit2PurchasePrice,
                         pr.Unit3Factor
                  FROM PurchaseItems pi
                  JOIN Products pr ON pi.ProductID = pr.ProductID
                  WHERE pi.PurchaseID = @id",
                DbHelper.P("@id", purchaseID));

            var dtPrevRet = DbHelper.Query(
                @"SELECT pri.ProductID, ISNULL(SUM(pri.Quantity * COALESCE(pri.Factor, 1.0)),0) AS ReturnedQtyInSmallest
                  FROM PurchaseReturnItems pri
                  JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                  WHERE pr.PurchaseID = @pid
                  GROUP BY pri.ProductID",
                DbHelper.P("@pid", purchaseID));

            var prevMap = new System.Collections.Generic.Dictionary<int, decimal>();
            foreach (DataRow r in dtPrevRet.Rows)
                prevMap[Convert.ToInt32(r["ProductID"])] = Convert.ToDecimal(r["ReturnedQtyInSmallest"]);

            foreach (DataRow r in dtItems.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                decimal purQty = Convert.ToDecimal(r["Quantity"]);
                decimal origFactor = Convert.ToDecimal(r["Factor"]);
                decimal purQtyInSmallest = purQty * origFactor;

                decimal prevRetInSmallest = prevMap.ContainsKey(pid) ? prevMap[pid] : 0m;
                decimal remainingInSmallest = purQtyInSmallest - prevRetInSmallest;

                if (remainingInSmallest <= 0) continue; // تم إرجاع الكل مسبقاً

                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                decimal prevQty = prevRetInSmallest / (origFactor > 0 ? origFactor : 1m);

                string baseUnit = r["BaseUnitName"]?.ToString() ?? "";
                string u1Name = r["Unit1Name"] != DBNull.Value ? r["Unit1Name"].ToString() : null;
                decimal u1Price = r["Unit1PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit1PurchasePrice"]) : 0m;
                string u2Name = r["Unit2Name"] != DBNull.Value ? r["Unit2Name"].ToString() : null;
                decimal u2Factor = r["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit2Factor"]) : 1m;
                decimal u2Price = r["Unit2PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["Unit2PurchasePrice"]) : 0m;
                decimal u3Factor = r["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit3Factor"]) : 1m;

                int rowIdx = dgItems.Rows.Add();
                var row = dgItems.Rows[rowIdx];
                row.Cells["ProductID"].Value       = pid;
                row.Cells["ProductName"].Value     = r["ProductName"].ToString();
                row.Cells["PurchasedQty"].Value    = purQty.ToString("N3");
                row.Cells["PrevReturnedQty"].Value = prevQty.ToString("N3");
                row.Cells["NewReturnedQty"].Value  = "0";
                row.Cells["UnitPrice"].Value       = price.ToString("N2");
                row.Cells["TotalPrice"].Value      = "0.00";
                
                // Hidden columns
                row.Cells["OriginalFactor"].Value = origFactor;
                row.Cells["OriginalUnitPrice"].Value = price;
                row.Cells["PurchasedQtyInSmallest"].Value = purQtyInSmallest;
                row.Cells["PrevReturnedQtyInSmallest"].Value = prevRetInSmallest;
                row.Cells["BaseUnitName"].Value = baseUnit;
                row.Cells["Unit1Name"].Value = u1Name;
                row.Cells["Unit1PurchasePrice"].Value = u1Price;
                row.Cells["Unit2Name"].Value = u2Name;
                row.Cells["Unit2Factor"].Value = u2Factor;
                row.Cells["Unit2PurchasePrice"].Value = u2Price;
                row.Cells["Unit3Factor"].Value = u3Factor;

                // Populate UnitName combobox cell
                if (dgItems.Columns["UnitName"] is DataGridViewComboBoxColumn unitCol)
                {
                    var unitCell = (DataGridViewComboBoxCell)row.Cells["UnitName"];
                    var unitList = new System.Collections.ArrayList();

                    if (!string.IsNullOrEmpty(baseUnit)) unitList.Add(baseUnit);
                    else unitList.Add("وحدة");

                    if (!string.IsNullOrEmpty(u2Name)) unitList.Add(u2Name);
                    if (!string.IsNullOrEmpty(u1Name) && u1Name != baseUnit) unitList.Add(u1Name);

                    unitCell.DataSource = unitList;

                    string purUnitName = r["UnitName"]?.ToString();
                    if (!string.IsNullOrEmpty(purUnitName) && unitList.Contains(purUnitName))
                        unitCell.Value = purUnitName;
                    else if (unitList.Count > 0)
                        unitCell.Value = unitList[0];
                }
            }

            if (dgItems.Rows.Count > 0)
                dgItems.CurrentCell = dgItems.Rows[0].Cells["NewReturnedQty"];

            dgItems.CellValueChanged += DgItems_CellValueChanged;
        }

        private void DgItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dgItems.Columns[e.ColumnIndex].Name != "NewReturnedQty") return;
            string valStr = e.FormattedValue?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(valStr)) return;
            if (!decimal.TryParse(valStr, out decimal newQty) || newQty < 0)
            {
                MessageBox.Show("أدخل كمية صالحة (رقم موجب أو صفر)", "تحقق من الإدخال",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            var row = dgItems.Rows[e.RowIndex];
            string selectedUnit = row.Cells["UnitName"].Value?.ToString();
            string baseUnit = row.Cells["BaseUnitName"].Value?.ToString() ?? "";
            string u1Name = row.Cells["Unit1Name"].Value?.ToString();
            string u2Name = row.Cells["Unit2Name"].Value?.ToString();

            decimal selectedFactor = 1m;
            if (!string.IsNullOrEmpty(u2Name) && selectedUnit == u2Name)
            {
                decimal u2Factor = Convert.ToDecimal(row.Cells["Unit2Factor"].Value);
                selectedFactor = u2Factor > 0 ? u2Factor : 1m;
            }
            else if (!string.IsNullOrEmpty(u1Name) && selectedUnit == u1Name)
            {
                selectedFactor = 1m;
            }
            else if (!string.IsNullOrEmpty(baseUnit) && selectedUnit == baseUnit)
            {
                decimal u2Factor = Convert.ToDecimal(row.Cells["Unit2Factor"].Value);
                decimal u3Factor = Convert.ToDecimal(row.Cells["Unit3Factor"].Value);
                selectedFactor = (u3Factor > 0 ? u3Factor : 1m) * (u2Factor > 0 ? u2Factor : 1m);
            }

            decimal purQtyInSmallest = Convert.ToDecimal(row.Cells["PurchasedQtyInSmallest"].Value);
            decimal prevQtyInSmallest = Convert.ToDecimal(row.Cells["PrevReturnedQtyInSmallest"].Value);
            decimal newQtyInSmallest = newQty * selectedFactor;

            if (newQtyInSmallest + prevQtyInSmallest > purQtyInSmallest)
            {
                decimal maxAllowedInSmallest = purQtyInSmallest - prevQtyInSmallest;
                decimal maxAllowedInSelected = maxAllowedInSmallest / selectedFactor;
                MessageBox.Show($"الكمية المرتجعة الجديدة ({newQty} {selectedUnit}) مع المرتجع السابق لا يمكن أن تتجاوز الكمية الأصلية بالفاتورة.\n\nالحد الأقصى المسموح به حالياً للمرتجع الجديد هو: {maxAllowedInSelected:N3} {selectedUnit}", "تجاوز الكمية المتاحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgItems.Rows[e.RowIndex];
            var colName = dgItems.Columns[e.ColumnIndex].Name;

            if (colName == "UnitName")
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (e.RowIndex >= 0 && e.RowIndex < dgItems.Rows.Count)
                    {
                        var curRow = dgItems.Rows[e.RowIndex];
                        HandleUnitChange(curRow);
                    }
                });
                return;
            }

            if (colName == "NewReturnedQty")
            {
                decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal qty);
                decimal.TryParse(row.Cells["UnitPrice"].Value?.ToString(), out decimal price);
                row.Cells["TotalPrice"].Value = (qty * price).ToString("N2");
                RecalcTotal();
            }
        }

        private void HandleUnitChange(DataGridViewRow row)
        {
            string selectedUnit = row.Cells["UnitName"].Value?.ToString();
            if (string.IsNullOrEmpty(selectedUnit)) return;

            string baseUnit = row.Cells["BaseUnitName"].Value?.ToString() ?? "";
            string u1Name = row.Cells["Unit1Name"].Value?.ToString();
            string u2Name = row.Cells["Unit2Name"].Value?.ToString();

            decimal origUnitPrice = Convert.ToDecimal(row.Cells["OriginalUnitPrice"].Value);
            decimal origFactor = Convert.ToDecimal(row.Cells["OriginalFactor"].Value);
            if (origFactor <= 0) origFactor = 1m;

            decimal selectedFactor = 1m;
            if (!string.IsNullOrEmpty(u2Name) && selectedUnit == u2Name)
            {
                decimal u2Factor = Convert.ToDecimal(row.Cells["Unit2Factor"].Value);
                selectedFactor = u2Factor > 0 ? u2Factor : 1m;
            }
            else if (!string.IsNullOrEmpty(u1Name) && selectedUnit == u1Name)
            {
                selectedFactor = 1m;
            }
            else if (!string.IsNullOrEmpty(baseUnit) && selectedUnit == baseUnit)
            {
                decimal u2Factor = Convert.ToDecimal(row.Cells["Unit2Factor"].Value);
                decimal u3Factor = Convert.ToDecimal(row.Cells["Unit3Factor"].Value);
                selectedFactor = (u3Factor > 0 ? u3Factor : 1m) * (u2Factor > 0 ? u2Factor : 1m);
            }

            decimal returnedUnitPrice = origUnitPrice * (selectedFactor / origFactor);
            row.Cells["UnitPrice"].Value = returnedUnitPrice.ToString("F2");

            decimal newQty = 0;
            if (row.Cells["NewReturnedQty"].Value != null)
            {
                decimal.TryParse(row.Cells["NewReturnedQty"].Value.ToString(), out newQty);
            }
            row.Cells["TotalPrice"].Value = (newQty * returnedUnitPrice).ToString("F2");
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

                string selectedUnit = row.Cells["UnitName"].Value?.ToString();
                string baseUnit = row.Cells["BaseUnitName"].Value?.ToString() ?? "";
                string u1Name = row.Cells["Unit1Name"].Value?.ToString();
                string u2Name = row.Cells["Unit2Name"].Value?.ToString();

                decimal selectedFactor = 1m;
                if (!string.IsNullOrEmpty(u2Name) && selectedUnit == u2Name)
                {
                    decimal u2Factor = Convert.ToDecimal(row.Cells["Unit2Factor"].Value);
                    selectedFactor = u2Factor > 0 ? u2Factor : 1m;
                }
                else if (!string.IsNullOrEmpty(u1Name) && selectedUnit == u1Name)
                {
                    selectedFactor = 1m;
                }
                else if (!string.IsNullOrEmpty(baseUnit) && selectedUnit == baseUnit)
                {
                    decimal u2Factor = Convert.ToDecimal(row.Cells["Unit2Factor"].Value);
                    decimal u3Factor = Convert.ToDecimal(row.Cells["Unit3Factor"].Value);
                    selectedFactor = (u3Factor > 0 ? u3Factor : 1m) * (u2Factor > 0 ? u2Factor : 1m);
                }

                decimal purQtyInSmallest = Convert.ToDecimal(row.Cells["PurchasedQtyInSmallest"].Value);
                decimal prevQtyInSmallest = Convert.ToDecimal(row.Cells["PrevReturnedQtyInSmallest"].Value);
                decimal newQtyInSmallest = newQty * selectedFactor;

                if (newQtyInSmallest + prevQtyInSmallest > purQtyInSmallest)
                {
                    MessageBox.Show(
                        $"الكمية المرتجعة للصنف ({name}) تتجاوز الكمية الأصلية!",
                        "تجاوز الكمية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                decimal.TryParse(row.Cells["UnitPrice"].Value?.ToString(), out decimal price);
                returnItems.Add(new PurchaseItemDTO
                {
                    ProductID = prodID, ProductName = name,
                    Quantity = newQty, UnitPrice = price,
                    UnitName = selectedUnit,
                    Factor = selectedFactor
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

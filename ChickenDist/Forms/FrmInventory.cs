using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmInventory : Form
    {
        private TabControl tabMain;
        private TabPage tabStock, tabLogs;

        // Tab Stock
        private DataGridView dgStock;
        private TextBox txtSearch;
        private ComboBox cboWarehouse;
        private Button btnSearch, btnMovement, btnPrintStock, btnAddExpiryRow;
        private CheckBox chkBelowMin, chkHideZeroStock, chkExpiryOnly;
        private ComboBox cboCategory;

        // Adjustment Form Controls
        private Label lblSelectedProduct, lblBookQtyVal, lblDiffVal;
        private NumericUpDown nudActualQty;
        private ComboBox cboAdjUnit;
        private TextBox txtNotes;
        private Button btnSaveAdj, btnClearAdj;
        private int _selectedProductID = 0;
        private decimal _selectedBookQty = 0;
        private string _selectedProductName = "";
        private string _selectedProductUnit = "";
        private bool _isSelecting = false;
        private decimal _lastSelectedFactor = 1m;
        private bool _selectedHasExpiry = false;
        private int? _selectedDefaultExpiryDays = null;
        private Button btnExpiryBatches;
        // حفظ الأرصدة الفعلية المدخلة عبر إعادات التحميل
        private readonly System.Collections.Generic.Dictionary<int, decimal> _enteredActualQty
            = new System.Collections.Generic.Dictionary<int, decimal>();

        // Tab Logs
        private DataGridView dgLogs;
        private DateTimePicker dtpFrom, dtpTo;
        private TextBox txtSearchLog;
        private ComboBox cboLogWarehouse;
        private Button btnLoadLogs, btnPrintLogs;

        public FrmInventory(bool belowMinOnly = false)
        {
            InitUI();
            LoadWarehouses();
            LoadLogWarehouses();
            if (belowMinOnly && chkBelowMin != null)
            {
                chkBelowMin.Checked = true;
            }
            LoadStock();
            LoadLogs();
        }

        private void InitUI()
        {
            this.Text = "جرد ومراقبة المخزن";
            this.Size = new Size(1050, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            tabMain = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontMain };
            tabStock = new TabPage("📦 الجرد الفعلي وتسوية الكميات") { BackColor = Theme.BgMain };
            tabLogs = new TabPage("📜 سجل تسويات الجرد") { BackColor = Theme.BgMain };
            tabMain.TabPages.AddRange(new[] { tabStock, tabLogs });
            this.Controls.Add(tabMain);

            BuildStockTab();
            BuildLogsTab();

            Theme.ApplyFormRTL(this);
        }

        private void BuildStockTab()
        {
            // Panel Left: Grid and filters (Dock Fill)
            var pnlLeft = new Panel { Dock = DockStyle.Fill };

            var pnlF = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true
            };

            var lblWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(5, 8, 5, 0) };
            cboWarehouse = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 5, 0) };
            cboWarehouse.SelectedIndexChanged += (s, e) => LoadStock();

            var lblCat = new Label { Text = "التصنيف:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(15, 8, 5, 0) };
            cboCategory = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(5, 4, 5, 0) };
            try
            {
                cboCategory.Items.Add(new ComboItem(0, "(كل التصنيفات)"));
                var dtCat = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories WHERE IsActive = 1 ORDER BY CategoryName");
                foreach (DataRow r in dtCat.Rows)
                {
                    cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                }
                cboCategory.SelectedIndex = 0;
            }
            catch { }
            cboCategory.SelectedIndexChanged += (s, e) => LoadStock();

            var lblSch = new Label { Text = "بحث صنف:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(15, 8, 5, 0) };
            txtSearch = new TextBox { Width = 110, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Margin = new Padding(5, 4, 5, 0) };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadStock(); };

            btnSearch = Theme.MakeButton("🔍 بحث", Color.FromArgb(60, 100, 60));
            btnSearch.Size = new Size(70, 26);
            btnSearch.Margin = new Padding(5, 2, 5, 0);
            btnSearch.Click += (s, e) => LoadStock();

            chkBelowMin = new CheckBox
            {
                Text = "⚠️ حد الطلب",
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(15, 6, 5, 0),
                RightToLeft = RightToLeft.Yes
            };
            chkBelowMin.CheckedChanged += (s, e) => LoadStock();

            chkHideZeroStock = new CheckBox
            {
                Text = "🚫 بدون رصيد صفري",
                ForeColor = Color.LightSkyBlue,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(15, 6, 5, 0),
                RightToLeft = RightToLeft.Yes,
                Checked = false
            };
            chkHideZeroStock.CheckedChanged += (s, e) => LoadStock();

            chkExpiryOnly = new CheckBox
            {
                Text = "📅 صلاحية فقط",
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(15, 6, 5, 0),
                RightToLeft = RightToLeft.Yes,
                Checked = false
            };
            chkExpiryOnly.CheckedChanged += (s, e) => LoadStock();

            btnMovement = Theme.MakeButton("📊 كشف حركة الصنف", Theme.Primary);
            btnMovement.Size = new Size(130, 26);
            btnMovement.Margin = new Padding(5, 2, 5, 0);
            btnMovement.Click += BtnMovement_Click;

            btnPrintStock = Theme.MakeButton("🖨 طباعة الجرد", Theme.Accent);
            btnPrintStock.Size = new Size(110, 26);
            btnPrintStock.Margin = new Padding(5, 2, 5, 0);
            btnPrintStock.Click += (s, e) => PrintStocktakeReport();

            btnAddExpiryRow = Theme.MakeButton("➕ إضافة صلاحية جديدة", Color.FromArgb(40, 120, 60));
            btnAddExpiryRow.Size = new Size(150, 26);
            btnAddExpiryRow.Margin = new Padding(5, 2, 5, 0);
            btnAddExpiryRow.Click += BtnAddExpiryRow_Click;
            btnAddExpiryRow.Enabled = false;

            pnlF.Controls.AddRange(new Control[] { lblWh, cboWarehouse, lblCat, cboCategory, lblSch, txtSearch, btnSearch, chkBelowMin, chkHideZeroStock, chkExpiryOnly, btnMovement, btnPrintStock, btnAddExpiryRow });


            // ── شبكة الجرد ─────────────────────────────────
            dgStock = MakeGrid();
            dgStock.ReadOnly = false;
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID",  Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BatchID",     Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", ReadOnly = true,  FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف",  ReadOnly = true,  FillWeight = 85 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate",  HeaderText = "تاريخ الصلاحية", ReadOnly = false, FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",        HeaderText = "الوحدة",    ReadOnly = true,  FillWeight = 38 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice",   HeaderText = "سعر البيع", ReadOnly = true,  FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty",     HeaderText = "الرصيد الدفتري", ReadOnly = true,  FillWeight = 55 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualQty",   HeaderText = "الرصيد الفعلي", ReadOnly = false, FillWeight = 55, DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(255, 255, 225), ForeColor = Color.FromArgb(80, 50, 0) } });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffQty",     HeaderText = "الفارق",       ReadOnly = true,  FillWeight = 48 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",       HeaderText = "ملاحظات",      ReadOnly = false, FillWeight = 70 });

            // أعمدة مخفية للوحدات
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseUnit",         Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit1Name",        Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Name",        Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit2Factor",      Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit3Factor",      Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "HasExpiry",        Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "DefaultExpiryDays", Visible = false });

            dgStock.CellEndEdit += DgStock_CellEndEdit;
            dgStock.CellDoubleClick += (s, e) => { if (e.ColumnIndex >= 0 && dgStock.Columns[e.ColumnIndex].Name != "ActualQty" && dgStock.Columns[e.ColumnIndex].Name != "Notes" && dgStock.Columns[e.ColumnIndex].Name != "ExpiryDate") BtnMovement_Click(s, e); };

            // ── زر الحفظ الشامل ─────────────────────────────
            btnSaveAdj = Theme.MakeButton("💾 حفظ كل التسويات", Theme.Accent);
            btnSaveAdj.Size = new Size(145, 26);
            btnSaveAdj.Margin = new Padding(5, 2, 5, 0);
            btnSaveAdj.Click += BtnSaveAdj_Click;

            btnClearAdj = Theme.MakeButton("❌ إلغاء التغييرات", Color.FromArgb(140, 40, 40));
            btnClearAdj.Size = new Size(130, 26);
            btnClearAdj.Margin = new Padding(5, 2, 5, 0);
            btnClearAdj.Click += (s, e) => ClearAdjustmentForm();

            pnlF.Controls.Add(btnSaveAdj);
            pnlF.Controls.Add(btnClearAdj);

            pnlLeft.Controls.Add(dgStock); // Fill
            pnlLeft.Controls.Add(pnlF);    // Top

            tabStock.Controls.Add(pnlLeft);
        }

        private void BuildLogsTab()
        {
            var pnlTop = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Top, 
                Height = 55, 
                BackColor = Theme.BgCard, 
                Padding = new Padding(10, 10, 10, 10),
                FlowDirection = FlowDirection.RightToLeft
            };
            
            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 5, 0) };
            dtpFrom = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            
            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 5, 0) };
            dtpTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var lblLogWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 5, 0) };
            cboLogWarehouse = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            cboLogWarehouse.SelectedIndexChanged += (s, e) => LoadLogs();

            var lblSearchLog = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 5, 0) };
            txtSearchLog = new TextBox { Width = 140 };
            txtSearchLog.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadLogs(); };

            btnLoadLogs = Theme.MakeButton("🔍 عرض السجل", Color.FromArgb(60, 100, 60));
            btnLoadLogs.Size = new Size(110, 30);
            btnLoadLogs.Click += (s, e) => LoadLogs();

            btnPrintLogs = Theme.MakeButton("🖨 طباعة السجل", Theme.Accent);
            btnPrintLogs.Size = new Size(110, 30);
            btnPrintLogs.Click += (s, e) => PrintAdjustmentsLog();

            pnlTop.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblLogWh, cboLogWarehouse, lblSearchLog, txtSearchLog, btnLoadLogs, btnPrintLogs });
            tabLogs.Controls.Add(pnlTop);

            dgLogs = MakeGrid();
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "AdjDate", HeaderText = "التاريخ والوقت", FillWeight = 50 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الكود", FillWeight = 30 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 80 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 30 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty", HeaderText = "الرصيد الدفتري", FillWeight = 40 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualQty", HeaderText = "الرصيد الفعلي", FillWeight = 40 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffQty", HeaderText = "الفارق", FillWeight = 35 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات التسوية", FillWeight = 70 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy", HeaderText = "بواسطة", FillWeight = 50 });

            tabLogs.Controls.Add(dgLogs);
            
            pnlTop.BringToFront();
            dgLogs.BringToFront();
        }

        private void LoadStock()
        {
            dgStock.Rows.Clear();
            int? wid = null;
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
                wid = ci.ID;

            bool hideZero = chkHideZeroStock != null && chkHideZeroStock.Checked;
            bool expOnly  = chkExpiryOnly  != null && chkExpiryOnly.Checked;
            int? catId = null;
            if (cboCategory != null && cboCategory.SelectedItem is ComboItem catCi && catCi.ID > 0)
                catId = catCi.ID;

            var dt  = InventoryDAL.GetStock(wid, txtSearch.Text, chkBelowMin != null && chkBelowMin.Checked, hideZero, expOnly, catId);
            var inv = System.Globalization.CultureInfo.InvariantCulture;

            foreach (DataRow r in dt.Rows)
            {
                decimal bookQty = Convert.ToDecimal(r["BookQty"]);
                int pid = Convert.ToInt32(r["ProductID"]);

                string actualVal = "";
                string diffVal   = "";
                if (_enteredActualQty.TryGetValue(pid, out decimal savedActual))
                {
                    actualVal = savedActual.ToString("N3");
                    decimal diff = savedActual - bookQty;
                    diffVal = (diff > 0 ? "+" : "") + diff.ToString("N3");
                }

                string displayUnit = r["Unit1Name"] != DBNull.Value && !string.IsNullOrEmpty(r["Unit1Name"].ToString())
                    ? r["Unit1Name"].ToString()
                    : r["Unit"].ToString();

                string expiryVal = "";
                if (dt.Columns.Contains("ExpiryDate") && r["ExpiryDate"] != DBNull.Value)
                {
                    DateTime expDt = Convert.ToDateTime(r["ExpiryDate"]);
                    expiryVal = expDt.ToString("yyyy-MM-dd");
                }
                object batchIdVal = dt.Columns.Contains("BatchID") && r["BatchID"] != DBNull.Value
                    ? r["BatchID"]
                    : (object)DBNull.Value;

                int ri = dgStock.Rows.Add(
                    r["ProductID"],
                    batchIdVal,
                    r["ProductCode"],
                    r["ProductName"],
                    expiryVal,
                    displayUnit,
                    Convert.ToDecimal(r["SalePrice"]).ToString("N2"),
                    bookQty.ToString("N3"),
                    actualVal,
                    diffVal,
                    "" // Notes
                );

                dgStock.Rows[ri].Cells["BaseUnit"].Value          = r["Unit"];
                dgStock.Rows[ri].Cells["Unit1Name"].Value         = r["Unit1Name"];
                dgStock.Rows[ri].Cells["Unit2Name"].Value         = r["Unit2Name"];
                dgStock.Rows[ri].Cells["Unit2Factor"].Value       = r["Unit2Factor"];
                dgStock.Rows[ri].Cells["Unit3Factor"].Value       = r["Unit3Factor"];
                dgStock.Rows[ri].Cells["HasExpiry"].Value         = r["HasExpiry"];
                dgStock.Rows[ri].Cells["DefaultExpiryDays"].Value = r["DefaultExpiryDays"];

                if (!string.IsNullOrEmpty(diffVal))
                {
                    decimal diff2 = savedActual - bookQty;
                    dgStock.Rows[ri].Cells["DiffQty"].Style.ForeColor = diff2 > 0 ? Color.DarkGreen : Color.OrangeRed;
                }
            }
            ClearAdjustmentForm();
        }

        private void LoadLogs()
        {
            dgLogs.Rows.Clear();
            int? wid = null;
            if (cboLogWarehouse != null && cboLogWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                wid = ci.ID;
            }
            var dt = InventoryDAL.GetAdjustments(dtpFrom.Value, dtpTo.Value, wid, txtSearchLog.Text);
            foreach (DataRow r in dt.Rows)
            {
                decimal diff = Convert.ToDecimal(r["DiffQty"]);
                int ri = dgLogs.Rows.Add(
                    Convert.ToDateTime(r["AdjDate"]).ToString("dd/MM/yyyy HH:mm"),
                    r["ProductCode"],
                    r["ProductName"],
                    r["Unit"],
                    Convert.ToDecimal(r["BookQty"]).ToString("N3"),
                    Convert.ToDecimal(r["ActualQty"]).ToString("N3"),
                    (diff > 0 ? "+" : "") + diff.ToString("N3"),
                    r["Notes"],
                    r["CreatedBy"]
                );

                if (diff > 0)
                    dgLogs.Rows[ri].Cells["DiffQty"].Style.ForeColor = Color.LightGreen;
                else if (diff < 0)
                    dgLogs.Rows[ri].Cells["DiffQty"].Style.ForeColor = Color.OrangeRed;
            }
        }

        private void DgStock_SelectionChanged(object sender, EventArgs e)
        {
            if (dgStock.SelectedRows.Count == 0) return;
            var r = dgStock.SelectedRows[0];
            _isSelecting = true;
            try
            {
                _selectedProductID   = Convert.ToInt32(r.Cells["ProductID"].Value);
                _selectedProductName = r.Cells["ProductName"].Value?.ToString();
                _selectedProductUnit = r.Cells["Unit"].Value?.ToString();
                _selectedHasExpiry   = r.Cells["HasExpiry"].Value != DBNull.Value && Convert.ToBoolean(r.Cells["HasExpiry"].Value);
                _selectedDefaultExpiryDays = r.Cells["DefaultExpiryDays"].Value != DBNull.Value ? Convert.ToInt32(r.Cells["DefaultExpiryDays"].Value) : (int?)null;
                _selectedBookQty = decimal.TryParse(r.Cells["BookQty"].Value?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal bq) ? bq : 0;
                _lastSelectedFactor = 1m;
                // عناصر الشريط الجانبي (إن وُجدت)
                if (lblSelectedProduct != null) lblSelectedProduct.Text = _selectedProductName;
                if (btnExpiryBatches   != null) btnExpiryBatches.Enabled = _selectedHasExpiry && _selectedProductID > 0;
                if (btnAddExpiryRow    != null) btnAddExpiryRow.Enabled = _selectedHasExpiry && _selectedProductID > 0;
            }
            finally { _isSelecting = false; }
        }

        private void NudActualQty_ValueChanged(object sender, EventArgs e)
        {
            if (_isSelecting || nudActualQty == null) return;
            if (dgStock.SelectedRows.Count > 0)
            {
                var r = dgStock.SelectedRows[0];
                decimal factor = 1m;
                if (cboAdjUnit?.SelectedItem is UnitItem ui) factor = ui.Factor;
                decimal actualInSmallest = nudActualQty.Value * factor;
                r.Cells["ActualQty"].Value = actualInSmallest.ToString("N3");
                decimal diff = actualInSmallest - _selectedBookQty;
                r.Cells["DiffQty"].Value = (diff > 0 ? "+" : "") + diff.ToString("N3");
                r.Cells["DiffQty"].Style.ForeColor = diff > 0 ? Color.DarkGreen : (diff < 0 ? Color.OrangeRed : Theme.TextMain);
                if (_selectedProductID > 0) _enteredActualQty[_selectedProductID] = actualInSmallest;
            }
            UpdateDifference();
        }

        private void DgStock_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgStock.Rows[e.RowIndex];
            if (dgStock.Columns[e.ColumnIndex].Name == "ActualQty")
            {
                var inv       = System.Globalization.CultureInfo.InvariantCulture;
                var numStyles = System.Globalization.NumberStyles.Any;
                decimal.TryParse(row.Cells["BookQty"].Value?.ToString(), numStyles, inv, out decimal bookQty);
                string cellText = row.Cells["ActualQty"].Value?.ToString();
                int rowPid = Convert.ToInt32(row.Cells["ProductID"].Value);

                if (!string.IsNullOrWhiteSpace(cellText) &&
                    decimal.TryParse(cellText, numStyles, inv, out decimal actualQty))
                {
                    decimal diff = actualQty - bookQty;
                    row.Cells["DiffQty"].Value = (diff > 0 ? "+" : "") + diff.ToString("N3");
                    row.Cells["DiffQty"].Style.ForeColor = diff > 0 ? Color.DarkGreen : (diff < 0 ? Color.OrangeRed : Theme.TextMain);
                    if (rowPid > 0) _enteredActualQty[rowPid] = actualQty;
                }
                else
                {
                    row.Cells["ActualQty"].Value = "";
                    row.Cells["DiffQty"].Value   = "";
                    row.Cells["DiffQty"].Style.ForeColor = Theme.TextMain;
                    if (rowPid > 0) _enteredActualQty.Remove(rowPid);
                }
            }
            else if (dgStock.Columns[e.ColumnIndex].Name == "ExpiryDate")
            {
                var cell = row.Cells["ExpiryDate"];
                string val = cell.Value?.ToString();
                var parsed = DbHelper.ParseExpiryInput(val);
                if (parsed.HasValue)
                {
                    cell.Value = parsed.Value.ToString("yyyy-MM-dd");
                }
                else if (string.IsNullOrWhiteSpace(val))
                {
                    cell.Value = "";
                }
                else
                {
                    MessageBox.Show("تاريخ غير صالح. يرجى إدخال التاريخ بالصيغة الصحيحة (شهر وسنة مثل 0326 أو yyyy-MM-dd).", "تاريخ غير صالح", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cell.Value = "";
                }
            }
        }

        private void CboAdjUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isSelecting || cboAdjUnit == null || nudActualQty == null) return;
            if (cboAdjUnit.SelectedItem is UnitItem ui)
            {
                decimal newFactor = ui.Factor;
                _isSelecting = true;
                try
                {
                    decimal actualInSmallest = nudActualQty.Value * _lastSelectedFactor;
                    nudActualQty.Value = actualInSmallest / (newFactor > 0 ? newFactor : 1m);
                }
                catch { }
                finally { _isSelecting = false; }
                _lastSelectedFactor = newFactor;
                UpdateDifference();
            }
        }

        private void UpdateDifference()
        {
            if (_selectedProductID == 0) return;
            decimal factor   = 1m;
            string unitName  = _selectedProductUnit ?? "";
            if (cboAdjUnit?.SelectedItem is UnitItem ui) { factor = ui.Factor; unitName = ui.Name; }
            decimal bookInUnit = _selectedBookQty / (factor > 0 ? factor : 1m);
            if (lblBookQtyVal != null) lblBookQtyVal.Text = bookInUnit.ToString("N3") + " " + unitName;
            if (nudActualQty != null && lblDiffVal != null)
            {
                decimal diff = nudActualQty.Value - bookInUnit;
                lblDiffVal.Text = (diff > 0 ? "+" : "") + diff.ToString("N3") + " " + unitName;
                lblDiffVal.ForeColor = diff > 0 ? Color.DarkGreen : (diff < 0 ? Color.OrangeRed : Theme.TextMain);
            }
        }

        private void ClearAdjustmentForm()
        {
            _isSelecting = true;
            try
            {
                _selectedProductID   = 0;
                _selectedBookQty     = 0;
                _selectedProductName = "";
                _selectedProductUnit = "";
                _lastSelectedFactor  = 1m;
                // عناصر الشريط الجانبي أُزيلت — نتحقق من الـ null قبل الاستخدام
                if (lblSelectedProduct != null) lblSelectedProduct.Text = "اختر صنفاً...";
                if (lblBookQtyVal      != null) lblBookQtyVal.Text      = "0.00";
                if (nudActualQty       != null) nudActualQty.Value      = 0;
                if (lblDiffVal         != null) { lblDiffVal.Text = "0.00"; lblDiffVal.ForeColor = Theme.TextMain; }
                if (txtNotes           != null) txtNotes.Clear();
                if (cboAdjUnit         != null) cboAdjUnit.DataSource   = null;
            }
            finally
            {
                _isSelecting = false;
            }
        }


        private void BtnSaveAdj_Click(object sender, EventArgs e)
        {
            int? selectedWid = null;
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                selectedWid = ci.ID;
            }

            if (!selectedWid.HasValue)
            {
                MessageBox.Show("من فضلك اختر مستودعاً محدداً أولاً لإجراء التسوية الجردية فيه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int wid = selectedWid.Value;

            // 1. تجميع كافة الأصناف التي أدخل المستخدم رصيدها الفعلي (غير فارغة)
            var modifiedRows = new System.Collections.Generic.List<DataGridViewRow>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var numStyles = System.Globalization.NumberStyles.Any;
            foreach (DataGridViewRow row in dgStock.Rows)
            {
                if (row.Cells["ProductID"].Value == null) continue;
                string cellVal = row.Cells["ActualQty"].Value?.ToString();
                // فقط الأصناف التي أدخل المستخدم لها رصيداً فعلياً
                if (!string.IsNullOrWhiteSpace(cellVal) &&
                    decimal.TryParse(cellVal, numStyles, inv, out decimal actualQty))
                {
                    modifiedRows.Add(row);
                }
            }

            if (modifiedRows.Count > 0)
            {
                string msg = "هل أنت متأكد من حفظ تسوية كميات الأصناف التالية وتعديل أرصدتها في المخزن؟\n\n";
                int count = 0;
                foreach (var row in modifiedRows)
                {
                    string name = row.Cells["ProductName"].Value?.ToString();
                    decimal.TryParse(row.Cells["BookQty"].Value?.ToString(), numStyles, inv, out decimal book);
                    decimal.TryParse(row.Cells["ActualQty"].Value?.ToString(), numStyles, inv, out decimal actual);
                    decimal diff = actual - book;
                    if (count < 10)
                        msg += $"• {name}: الدفتري ({book:N3}) ➔ الفعلي ({actual:N3}) [الفارق: {(diff > 0 ? "+" : "")}{diff:N3}]\n";
                    count++;
                }
                if (count > 10)
                    msg += $"\n... وعدد {count - 10} أصناف أخرى.";

                if (MessageBox.Show(msg, "تأكيد تسوية الكميات الجردية", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    int savedCount = 0;
                    try
                    {
                        DbHelper.RunInTransaction((con, trans) =>
                        {
                            foreach (var row in modifiedRows)
                            {
                                int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                                decimal.TryParse(row.Cells["BookQty"].Value?.ToString(), numStyles, inv, out decimal book);
                                decimal.TryParse(row.Cells["ActualQty"].Value?.ToString(), numStyles, inv, out decimal actual);
                                string displayUnit = row.Cells["Unit"].Value?.ToString();
                                string rowNotes    = row.Cells["Notes"].Value?.ToString() ?? "";
                                bool hasExpiry     = row.Cells["HasExpiry"].Value != DBNull.Value && Convert.ToBoolean(row.Cells["HasExpiry"].Value);

                                if (hasExpiry)
                                {
                                    object batchIdVal = row.Cells["BatchID"].Value;
                                    string expStr = row.Cells["ExpiryDate"].Value?.ToString();
                                    DateTime? exp = null;
                                    if (!string.IsNullOrWhiteSpace(expStr) && DateTime.TryParse(expStr, out DateTime parsedExp))
                                        exp = parsedExp;

                                    if (batchIdVal != null && batchIdVal != DBNull.Value)
                                    {
                                        int bid = Convert.ToInt32(batchIdVal);
                                        DbHelper.ExecuteTrans(trans,
                                            "UPDATE ProductBatches SET ExpiryDate=@exp, Quantity=@qty WHERE BatchID=@bid",
                                            DbHelper.P("@exp", exp.HasValue ? (object)exp.Value : DBNull.Value),
                                            DbHelper.P("@qty", actual),
                                            DbHelper.P("@bid", bid));
                                    }
                                    else
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "INSERT INTO ProductBatches (ProductID, WarehouseID, Quantity, ExpiryDate) VALUES (@pid, @wid, @qty, @exp)",
                                            DbHelper.P("@pid", pid),
                                            DbHelper.P("@wid", wid),
                                            DbHelper.P("@qty", actual),
                                            DbHelper.P("@exp", exp.HasValue ? (object)exp.Value : DBNull.Value));
                                    }

                                    // Sync ProductStock
                                    DbHelper.ExecuteTrans(trans,
                                        @"IF EXISTS (SELECT 1 FROM ProductStock WHERE ProductID=@pid AND WarehouseID=@wid)
                                            UPDATE ProductStock SET Quantity = (SELECT COALESCE(SUM(Quantity), 0) FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid) WHERE ProductID=@pid AND WarehouseID=@wid
                                          ELSE
                                            INSERT INTO ProductStock (ProductID, WarehouseID, Quantity) VALUES (@pid, @wid, (SELECT COALESCE(SUM(Quantity), 0) FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid))",
                                        DbHelper.P("@pid", pid),
                                        DbHelper.P("@wid", wid));

                                    // Log to StockAdjustments
                                    string expText = exp.HasValue ? exp.Value.ToString("yyyy-MM-dd") : "بدون";
                                    string logNotes = $"[تسوية صلاحية: {expText}] " + rowNotes;
                                    DbHelper.ExecuteTrans(trans,
                                        @"INSERT INTO StockAdjustments (ProductID, WarehouseID, BookQty, ActualQty, Notes, CreatedBy, UnitName, Factor)
                                          VALUES (@pid, @wid, @bq, @aq, @notes, @by, @un, 1.0)",
                                        DbHelper.P("@pid", pid),
                                        DbHelper.P("@wid", wid),
                                        DbHelper.P("@bq", book),
                                        DbHelper.P("@aq", actual),
                                        DbHelper.P("@notes", logNotes),
                                        DbHelper.P("@by", Session.EmpID),
                                        DbHelper.P("@un", displayUnit));
                                }
                                else
                                {
                                    // Normal adjustment
                                    DbHelper.ExecuteTrans(trans,
                                        @"INSERT INTO StockAdjustments (ProductID, WarehouseID, BookQty, ActualQty, Notes, CreatedBy, UnitName, Factor)
                                          VALUES (@pid, @wid, @bq, @aq, @notes, @by, @un, 1.0)",
                                        DbHelper.P("@pid", pid),
                                        DbHelper.P("@wid", wid),
                                        DbHelper.P("@bq", book),
                                        DbHelper.P("@aq", actual),
                                        DbHelper.P("@notes", rowNotes),
                                        DbHelper.P("@by", Session.EmpID),
                                        DbHelper.P("@un", displayUnit));
                                }

                                _enteredActualQty.Remove(pid);
                                savedCount++;
                            }
                        });

                        if (savedCount > 0)
                        {
                            MessageBox.Show($"✅ تم حفظ وتطبيق التسوية الجردية لعدد ({savedCount}) أصناف بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadStock();
                            LoadLogs();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ فشل حفظ التعديلات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("لم يتم إدخال أي رصيد فعلي في الجدول بعد.\nاكتب الرصيد الفعلي في عمود «الرصيد الفعلي» لأي صنف ثم اضغط حفظ.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        private class UnitItem
        {
            public string Name { get; set; }
            public decimal Factor { get; set; }
            public override string ToString() => $"{Name} (×{Factor:N0})";
        }

        private void BtnMovement_Click(object sender, EventArgs e)
        {
            if (dgStock.SelectedRows.Count == 0)
            {
                MessageBox.Show("من فضلك اختر صنفاً أولاً من الجدول لمشاهدة حركته.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var r = dgStock.SelectedRows[0];
            int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
            string name = r.Cells["ProductName"].Value?.ToString();
            string unit = r.Cells["Unit"].Value?.ToString();

            var frm = new FrmProductMovement(pid, name, unit);
            frm.ShowDialog();
        }

        private void PrintStocktakeReport()
        {
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 16, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var normal = new Font("Arial", 9);
                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };

                int y = 30;
                int pageW = 800;

                // Title
                g.DrawString("ورقة عمل الجرد المخزني الفعلي", boldBig, Brushes.DarkBlue, new RectangleF(20, y, pageW - 40, 30), center); y += 35;
                g.DrawString("اطبع هذه الورقة لتدوين الرصيد الفعلي يدوياً من داخل المستودع", normal, Brushes.Black, new RectangleF(20, y, pageW - 40, 20), center); y += 25;
                g.DrawLine(new Pen(Color.DarkBlue, 2), 20, y, pageW - 20, y); y += 15;

                g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", normal, Brushes.Black, 20, y);
                y += 25;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y); y += 10;

                // Columns: Code, Name, Unit, Book Qty, Actual Qty (Blank line for writing)
                int[] xCols = { 20, 150, 420, 520, 670 };
                string[] headers = { "الكود", "اسم الصنف", "الوحدة", "الرصيد الدفتري", "الرصيد الفعلي (يدوي)" };

                for (int i = 0; i < headers.Length; i++)
                    g.DrawString(headers[i], bold, Brushes.DarkBlue, xCols[i], y);
                y += 22;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y); y += 8;

                foreach (DataGridViewRow row in dgStock.Rows)
                {
                    string code = row.Cells["ProductCode"].Value?.ToString();
                    string name = row.Cells["ProductName"].Value?.ToString();
                    string unit = row.Cells["Unit"].Value?.ToString();
                    string bookQty = row.Cells["BookQty"].Value?.ToString();

                    g.DrawString(code, normal, Brushes.Black, xCols[0], y);
                    g.DrawString(name, normal, Brushes.Black, xCols[1], y);
                    g.DrawString(unit, normal, Brushes.Black, xCols[2], y);
                    g.DrawString(bookQty, bold, Brushes.Black, xCols[3], y);
                    
                    // Draw a dashed line or underline for manual writing
                    g.DrawLine(new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot }, xCols[4], y + 14, xCols[4] + 110, y + 14);

                    y += 28;
                }

                y += 20;
                g.DrawLine(new Pen(Color.DarkBlue, 1.5f), 20, y, pageW - 20, y); y += 8;
                g.DrawString("المسؤول عن الجرد: .......................................         التوقيع: .......................................", bold, Brushes.Black, 20, y);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 850,
                Height = 750,
                Text = "طباعة ورقة الجرد المخزني"
            };
            preview.ShowDialog();
        }

        private void PrintAdjustmentsLog()
        {
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 16, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var normal = new Font("Arial", 9);
                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };

                int y = 30;
                int pageW = 800;

                // Title
                g.DrawString("تقرير سجل تسويات فروقات الجرد", boldBig, Brushes.DarkBlue, new RectangleF(20, y, pageW - 40, 30), center); y += 30;
                g.DrawString($"الفترة: من {dtpFrom.Value:dd/MM/yyyy} إلى {dtpTo.Value:dd/MM/yyyy}", normal, Brushes.Black, new RectangleF(20, y, pageW - 40, 20), center); y += 25;
                g.DrawLine(new Pen(Color.DarkBlue, 2), 20, y, pageW - 20, y); y += 15;

                g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", normal, Brushes.Black, 20, y);
                y += 25;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y); y += 10;

                // Columns: Date, Name, Book, Actual, Diff, Done By
                int[] xCols = { 20, 150, 360, 460, 560, 660 };
                string[] headers = { "التاريخ والوقت", "اسم الصنف", "الدفتري", "الفعلي", "الفارق", "المسؤول" };

                for (int i = 0; i < headers.Length; i++)
                    g.DrawString(headers[i], bold, Brushes.DarkBlue, xCols[i], y);
                y += 22;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y); y += 8;

                foreach (DataGridViewRow row in dgLogs.Rows)
                {
                    string date = row.Cells["AdjDate"].Value?.ToString();
                    string name = row.Cells["ProductName"].Value?.ToString();
                    string book = row.Cells["BookQty"].Value?.ToString();
                    string actual = row.Cells["ActualQty"].Value?.ToString();
                    string diff = row.Cells["DiffQty"].Value?.ToString();
                    string user = row.Cells["CreatedBy"].Value?.ToString();

                    g.DrawString(date, normal, Brushes.Black, xCols[0], y);
                    g.DrawString(name, normal, Brushes.Black, xCols[1], y);
                    g.DrawString(book, normal, Brushes.Black, xCols[2], y);
                    g.DrawString(actual, normal, Brushes.Black, xCols[3], y);
                    g.DrawString(diff, bold, diff.StartsWith("+") ? Brushes.Green : Brushes.Red, xCols[4], y);
                    g.DrawString(user, normal, Brushes.Black, xCols[5], y);

                    y += 22;
                }

                y += 15;
                g.DrawLine(new Pen(Color.DarkBlue, 1.5f), 20, y, pageW - 20, y);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 850,
                Height = 750,
                Text = "طباعة سجل التسويات الجردية"
            };
            preview.ShowDialog();
        }

        private void LoadWarehouses()
        {
            try
            {
                var dt = WarehouseDAL.GetAll(true);
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add(new ComboItem(0, "--- كل المخازن ---"));
                foreach (DataRow r in dt.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
                }
                cboWarehouse.DisplayMember = "Text";
                cboWarehouse.ValueMember = "ID";
                if (cboWarehouse.Items.Count > 0)
                {
                    cboWarehouse.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل قائمة المخازن:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLogWarehouses()
        {
            try
            {
                var dt = WarehouseDAL.GetAll(true);
                cboLogWarehouse.Items.Clear();
                cboLogWarehouse.Items.Add(new ComboItem(0, "--- كل المخازن ---"));
                foreach (DataRow r in dt.Rows)
                {
                    cboLogWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
                }
                cboLogWarehouse.DisplayMember = "Text";
                cboLogWarehouse.ValueMember = "ID";
                if (cboLogWarehouse.Items.Count > 0)
                {
                    cboLogWarehouse.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل قائمة المخازن في السجل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataGridView MakeGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(210, 210, 215),
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(240, 242, 245), ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
        }

        private void BtnExpiryBatches_Click(object sender, EventArgs e)
        {
            int? selectedWid = null;
            if (cboWarehouse != null && cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                selectedWid = ci.ID;
            }

            if (!selectedWid.HasValue || _selectedProductID <= 0) return;

            using (var frm = new FrmAdjustExpiryBatches(_selectedProductID, _selectedProductName, selectedWid.Value))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadStock();
                }
            }
        }

        private void BtnAddExpiryRow_Click(object sender, EventArgs e)
        {
            if (dgStock.SelectedRows.Count == 0) return;
            var r = dgStock.SelectedRows[0];
            
            int pid = Convert.ToInt32(r.Cells["ProductID"].Value);
            string code = r.Cells["ProductCode"].Value?.ToString();
            string name = r.Cells["ProductName"].Value?.ToString();
            string unit = r.Cells["Unit"].Value?.ToString();
            string baseUnit = r.Cells["BaseUnit"].Value?.ToString();
            string unit1 = r.Cells["Unit1Name"].Value?.ToString();
            string unit2 = r.Cells["Unit2Name"].Value?.ToString();
            decimal u2f = r.Cells["Unit2Factor"].Value != DBNull.Value ? Convert.ToDecimal(r.Cells["Unit2Factor"].Value) : 0m;
            decimal u3f = r.Cells["Unit3Factor"].Value != DBNull.Value ? Convert.ToDecimal(r.Cells["Unit3Factor"].Value) : 0m;
            bool hasExp = r.Cells["HasExpiry"].Value != DBNull.Value && Convert.ToBoolean(r.Cells["HasExpiry"].Value);
            int? defDays = r.Cells["DefaultExpiryDays"].Value != DBNull.Value ? Convert.ToInt32(r.Cells["DefaultExpiryDays"].Value) : (int?)null;
            string price = r.Cells["SalePrice"].Value?.ToString();

            // Clean the name of "(صلاحية إضافية)" if we are adding from an already modified row
            if (name.Contains(" (صلاحية إضافية)"))
            {
                name = name.Replace(" (صلاحية إضافية)", "");
            }

            int ri = dgStock.Rows.Add();
            var newRow = dgStock.Rows[ri];

            newRow.Cells["ProductID"].Value = pid;
            newRow.Cells["BatchID"].Value = DBNull.Value;
            newRow.Cells["ProductCode"].Value = code;
            newRow.Cells["ProductName"].Value = name + " (صلاحية إضافية)";
            newRow.Cells["ExpiryDate"].Value = "";
            newRow.Cells["Unit"].Value = unit;
            newRow.Cells["SalePrice"].Value = price;
            newRow.Cells["BookQty"].Value = "0.000";
            newRow.Cells["ActualQty"].Value = "";
            newRow.Cells["DiffQty"].Value = "";
            newRow.Cells["Notes"].Value = "";

            newRow.Cells["BaseUnit"].Value = baseUnit;
            newRow.Cells["Unit1Name"].Value = unit1;
            newRow.Cells["Unit2Name"].Value = unit2;
            newRow.Cells["Unit2Factor"].Value = u2f;
            newRow.Cells["Unit3Factor"].Value = u3f;
            newRow.Cells["HasExpiry"].Value = hasExp;
            newRow.Cells["DefaultExpiryDays"].Value = defDays;

            dgStock.ClearSelection();
            newRow.Selected = true;
            dgStock.CurrentCell = newRow.Cells["ExpiryDate"];
            dgStock.BeginEdit(true);
        }
    }


    public class FrmAdjustExpiryBatches : Form
    {
        private int _productID;
        private int _warehouseID;
        private DataGridView dgBatches;
        private Button btnSave, btnCancel, btnAddBatch;
        private DateTimePicker dtpNewExpiry;
        private NumericUpDown nudNewQty;

        public FrmAdjustExpiryBatches(int productID, string productName, int warehouseID)
        {
            _productID = productID;
            _warehouseID = warehouseID;

            this.Text = $"تعديل أرصدة صلاحية الصنف: {productName}";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10) };
            var lblInfo = new Label { Text = $"صلاحيات المخزن الحالي لمجموعة الدفعات الخاصة بالصنف", Dock = DockStyle.Fill, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlTop.Controls.Add(lblInfo);

            dgBatches = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgBatches.Columns.Add(new DataGridViewTextBoxColumn { Name = "BatchID", HeaderText = "رقم الدفعة", ReadOnly = true, FillWeight = 25 });
            dgBatches.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate", HeaderText = "تاريخ الصلاحية (yyyy-MM-dd)", ReadOnly = false, FillWeight = 50 });
            dgBatches.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", ReadOnly = false, FillWeight = 35 });

            dgBatches.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgBatches.Columns[e.ColumnIndex].Name == "ExpiryDate")
                {
                    var cell = dgBatches.Rows[e.RowIndex].Cells["ExpiryDate"];
                    string val = cell.Value?.ToString();
                    var parsed = DbHelper.ParseExpiryInput(val);
                    if (parsed.HasValue)
                    {
                        cell.Value = parsed.Value.ToString("yyyy-MM-dd");
                    }
                    else if (string.IsNullOrWhiteSpace(val))
                    {
                        cell.Value = "";
                    }
                    else
                    {
                        MessageBox.Show("تاريخ غير صالح. يرجى إدخال التاريخ بالصيغة الصحيحة (شهر وسنة مثل 0326 أو yyyy-MM-dd).", "تاريخ غير صالح", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cell.Value = "";
                    }
                }
            };

            var pnlAdd = new GroupBox
            {
                Text = "إضافة دفعة جديدة",
                Dock = DockStyle.Bottom,
                Height = 85,
                ForeColor = Theme.Accent,
                Padding = new Padding(10)
            };
            var lblDate = new Label { Text = "تاريخ الانتهاء:", Location = new Point(370, 25), Size = new Size(90, 20), ForeColor = Theme.TextMain };
            dtpNewExpiry = new DateTimePicker { Location = new Point(230, 22), Width = 130, Format = DateTimePickerFormat.Short };
            var lblQty = new Label { Text = "الكمية:", Location = new Point(160, 25), Size = new Size(50, 20), ForeColor = Theme.TextMain };
            nudNewQty = new NumericUpDown { Location = new Point(80, 22), Width = 80, DecimalPlaces = 3, Maximum = 999999, Minimum = 0 };
            btnAddBatch = Theme.MakeButton("➕ إضافة", Color.FromArgb(40, 120, 60));
            btnAddBatch.Location = new Point(10, 20);
            btnAddBatch.Size = new Size(60, 28);
            btnAddBatch.Click += BtnAddBatch_Click;

            pnlAdd.Controls.AddRange(new Control[] { lblDate, dtpNewExpiry, lblQty, nudNewQty, btnAddBatch });

            var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };
            btnSave = Theme.MakeButton("💾 حفظ التعديلات", Theme.Accent);
            btnSave.Location = new Point(10, 10);
            btnSave.Size = new Size(130, 30);
            btnSave.Click += BtnSave_Click;
            btnCancel = Theme.MakeButton("❌ إلغاء", Color.FromArgb(120, 40, 40));
            btnCancel.Location = new Point(150, 10);
            btnCancel.Size = new Size(90, 30);
            btnCancel.Click += (s, e) => this.Close();
            pnlActions.Controls.AddRange(new Control[] { btnSave, btnCancel });

            this.Controls.Add(dgBatches);
            this.Controls.Add(pnlAdd);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlActions);

            LoadBatches();
        }

        private void LoadBatches()
        {
            dgBatches.Rows.Clear();
            var dt = DbHelper.Query(@"
                SELECT BatchID, ExpiryDate, Quantity 
                FROM ProductBatches 
                WHERE ProductID = @pid AND WarehouseID = @wid 
                ORDER BY ExpiryDate ASC",
                DbHelper.P("@pid", _productID), DbHelper.P("@wid", _warehouseID));
            foreach (DataRow r in dt.Rows)
            {
                DateTime? exp = r["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(r["ExpiryDate"]) : (DateTime?)null;
                dgBatches.Rows.Add(
                    r["BatchID"],
                    exp.HasValue ? exp.Value.ToString("yyyy-MM-dd") : "",
                    Convert.ToDecimal(r["Quantity"]).ToString("N3")
                );
            }
        }

        private void BtnAddBatch_Click(object sender, EventArgs e)
        {
            if (nudNewQty.Value <= 0)
            {
                MessageBox.Show("من فضلك أدخل كمية أكبر من الصفر للدفعة الجديدة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            dgBatches.Rows.Add("", dtpNewExpiry.Value.ToString("yyyy-MM-dd"), nudNewQty.Value.ToString("N3"));
            nudNewQty.Value = 0;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgBatches.Rows)
            {
                string expStr = row.Cells["ExpiryDate"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(expStr) || !DateTime.TryParse(expStr, out _))
                {
                    MessageBox.Show("❌ خطأ: يجب تسجيل تاريخ صلاحية صحيح لكل الدفعات المعروضة قبل الحفظ!", "تاريخ صلاحية مفقود", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    foreach (DataGridViewRow row in dgBatches.Rows)
                    {
                        string batchIdStr = row.Cells["BatchID"].Value?.ToString();
                        string expStr = row.Cells["ExpiryDate"].Value?.ToString();
                        string qtyStr = row.Cells["Quantity"].Value?.ToString();

                        DateTime? exp = null;
                        if (!string.IsNullOrWhiteSpace(expStr) && DateTime.TryParse(expStr, out DateTime parsedExp))
                            exp = parsedExp;

                        decimal.TryParse(qtyStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal qty);

                        if (!string.IsNullOrEmpty(batchIdStr))
                        {
                            int bid = Convert.ToInt32(batchIdStr);
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE ProductBatches SET ExpiryDate=@exp, Quantity=@qty WHERE BatchID=@bid",
                                DbHelper.P("@exp", exp.HasValue ? (object)exp.Value : DBNull.Value),
                                DbHelper.P("@qty", qty),
                                DbHelper.P("@bid", bid));
                        }
                        else
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ProductBatches (ProductID, WarehouseID, Quantity, ExpiryDate) VALUES (@pid, @wid, @qty, @exp)",
                                DbHelper.P("@pid", _productID),
                                DbHelper.P("@wid", _warehouseID),
                                DbHelper.P("@qty", qty),
                                DbHelper.P("@exp", exp.HasValue ? (object)exp.Value : DBNull.Value));
                        }
                    }

                    // Sync ProductStock
                    DbHelper.ExecuteTrans(trans,
                        @"IF EXISTS (SELECT 1 FROM ProductStock WHERE ProductID=@pid AND WarehouseID=@wid)
                            UPDATE ProductStock SET Quantity = (SELECT COALESCE(SUM(Quantity), 0) FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid) WHERE ProductID=@pid AND WarehouseID=@wid
                          ELSE
                            INSERT INTO ProductStock (ProductID, WarehouseID, Quantity) VALUES (@pid, @wid, (SELECT COALESCE(SUM(Quantity), 0) FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid))",
                        DbHelper.P("@pid", _productID),
                        DbHelper.P("@wid", _warehouseID));
                });

                MessageBox.Show("✅ تم حفظ تواريخ الصلاحية وتحديث أرصدة الدفعات بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل حفظ التعديلات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

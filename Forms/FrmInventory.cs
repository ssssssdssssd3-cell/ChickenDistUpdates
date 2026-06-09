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
        private Button btnSearch, btnMovement, btnPrintStock;

        // Adjustment Form Controls
        private Label lblSelectedProduct, lblBookQtyVal, lblDiffVal;
        private NumericUpDown nudActualQty;
        private TextBox txtNotes;
        private Button btnSaveAdj, btnClearAdj;
        private int _selectedProductID = 0;
        private decimal _selectedBookQty = 0;
        private string _selectedProductName = "";
        private string _selectedProductUnit = "";
        private bool _isSelecting = false;

        // Tab Logs
        private DataGridView dgLogs;
        private DateTimePicker dtpFrom, dtpTo;
        private TextBox txtSearchLog;
        private Button btnLoadLogs, btnPrintLogs;

        public FrmInventory()
        {
            InitUI();
            LoadStock();
            LoadLogs();
        }

        private void InitUI()
        {
            this.Text = "جرد ومراقبة المخزن";
            this.Size = new Size(1366, 768);
            this.MinimumSize = new Size(1024, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
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

            var pnlF = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Theme.BgCard, Padding = new Padding(8) };
            
            pnlF.Controls.Add(new Label { Text = "بحث عن صنف:", Location = new Point(600, 15), AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Top | AnchorStyles.Right });
            
            txtSearch = new TextBox { Location = new Point(400, 11), Width = 190, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadStock(); };
            pnlF.Controls.Add(txtSearch);

            btnSearch = Theme.MakeButton("🔍 بحث", Color.FromArgb(60, 100, 60));
            btnSearch.Location = new Point(310, 8);
            btnSearch.Size = new Size(80, 32);
            btnSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSearch.Click += (s, e) => LoadStock();
            pnlF.Controls.Add(btnSearch);

            btnMovement = Theme.MakeButton("📊 كشف حركة الصنف", Theme.Primary);
            btnMovement.Location = new Point(130, 8);
            btnMovement.Size = new Size(170, 32);
            btnMovement.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMovement.Click += BtnMovement_Click;
            pnlF.Controls.Add(btnMovement);

            btnPrintStock = Theme.MakeButton("🖨 طباعة ورقة الجرد", Theme.Accent);
            btnPrintStock.Location = new Point(10, 8);
            btnPrintStock.Size = new Size(110, 32);
            btnPrintStock.Click += (s, e) => PrintStocktakeReport();
            pnlF.Controls.Add(btnPrintStock);

            dgStock = MakeGrid();
            dgStock.ReadOnly = false;
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", ReadOnly = true, FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", ReadOnly = true, FillWeight = 90 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", ReadOnly = true, FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", ReadOnly = true, FillWeight = 40 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty", HeaderText = "الرصيد الدفتري الحالي", ReadOnly = true, FillWeight = 60 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualQty", HeaderText = "الرصيد الفعلي", ReadOnly = false, FillWeight = 60 });
            dgStock.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffQty", HeaderText = "الفارق الجردي", ReadOnly = true, FillWeight = 60 });
            
            dgStock.SelectionChanged += DgStock_SelectionChanged;
            dgStock.CellEndEdit += DgStock_CellEndEdit;
            dgStock.CellDoubleClick += (s, e) => {
                if (e.ColumnIndex >= 0 && dgStock.Columns[e.ColumnIndex].Name != "ActualQty")
                    BtnMovement_Click(s, e);
            };

            pnlLeft.Controls.Add(dgStock); // Fill
            pnlLeft.Controls.Add(pnlF);    // Top

            // Panel Right: Adjustment Form (Dock Left, Width 340)
            var pnlDetails = new Panel { Dock = DockStyle.Left, Width = 340, BackColor = Theme.BgCard, Padding = new Padding(15) };
            
            var lblSectionTitle = new Label 
            { 
                Text = "⚡ تسوية كميات الصنف", 
                Font = Theme.FontHeader, 
                ForeColor = Theme.Accent, 
                AutoSize = true, 
                Location = new Point(160, 15) 
            };
            pnlDetails.Controls.Add(lblSectionTitle);

            var tblFields = new TableLayoutPanel
            {
                Location = new Point(10, 55),
                Size = new Size(310, 240),
                ColumnCount = 2,
                RowCount = 5,
                RightToLeft = RightToLeft.Yes
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            for (int i = 0; i < 5; i++)
                tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));

            // Row 0: Selected Product
            var lblProd = new Label { Text = "الصنف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) };
            lblSelectedProduct = new Label { Text = "اختر صنفاً...", Font = Theme.FontBold, ForeColor = Theme.Accent, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            tblFields.Controls.Add(lblProd, 0, 0);
            tblFields.Controls.Add(lblSelectedProduct, 1, 0);

            // Row 1: Book Qty
            var lblBook = new Label { Text = "الرصيد الدفتري:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) };
            lblBookQtyVal = new Label { Text = "0.00", Font = Theme.FontBold, ForeColor = Theme.TextMain, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            tblFields.Controls.Add(lblBook, 0, 1);
            tblFields.Controls.Add(lblBookQtyVal, 1, 1);

            // Row 2: Actual Qty
            var lblActual = new Label { Text = "الرصيد الفعلي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) };
            nudActualQty = new NumericUpDown 
            { 
                Dock = DockStyle.Fill, 
                Minimum = -999999, 
                Maximum = 999999, 
                DecimalPlaces = 3, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                Margin = new Padding(0, 5, 0, 5)
            };
            nudActualQty.ValueChanged += NudActualQty_ValueChanged;
            tblFields.Controls.Add(lblActual, 0, 2);
            tblFields.Controls.Add(nudActualQty, 1, 2);

            // Row 3: Difference
            var lblDiff = new Label { Text = "الفارق الجردي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) };
            lblDiffVal = new Label { Text = "0.00", Font = Theme.FontBold, ForeColor = Theme.TextMain, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            tblFields.Controls.Add(lblDiff, 0, 3);
            tblFields.Controls.Add(lblDiffVal, 1, 3);

            // Row 4: Notes
            var lblNotes = new Label { Text = "ملاحظات:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) };
            txtNotes = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                Margin = new Padding(0, 5, 0, 5),
                BorderStyle = BorderStyle.FixedSingle
            };
            tblFields.Controls.Add(lblNotes, 0, 4);
            tblFields.Controls.Add(txtNotes, 1, 4);

            pnlDetails.Controls.Add(tblFields);

            // Action Buttons
            btnSaveAdj = Theme.MakeButton("💾 تسوية وحفظ الرصيد", Theme.Accent);
            btnSaveAdj.Location = new Point(170, 310);
            btnSaveAdj.Size = new Size(150, 35);
            btnSaveAdj.Click += BtnSaveAdj_Click;

            btnClearAdj = Theme.MakeButton("🆕 إلغاء", Color.FromArgb(140, 40, 40));
            btnClearAdj.Location = new Point(20, 310);
            btnClearAdj.Size = new Size(140, 35);
            btnClearAdj.Click += (s, e) => ClearAdjustmentForm();

            pnlDetails.Controls.AddRange(new Control[] { btnSaveAdj, btnClearAdj });

            tabStock.Controls.Add(pnlLeft);
            tabStock.Controls.Add(pnlDetails);
        }

        private void BuildLogsTab()
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(10) };
            
            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(940, 18) };
            dtpFrom = new DateTimePicker { Location = new Point(800, 14), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            
            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(750, 18) };
            dtpTo = new DateTimePicker { Location = new Point(610, 14), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var lblSearchLog = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(560, 18) };
            txtSearchLog = new TextBox { Location = new Point(380, 14), Width = 170 };
            txtSearchLog.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadLogs(); };

            btnLoadLogs = Theme.MakeButton("🔍 عرض السجل", Color.FromArgb(60, 100, 60));
            btnLoadLogs.Location = new Point(250, 11);
            btnLoadLogs.Size = new Size(120, 32);
            btnLoadLogs.Click += (s, e) => LoadLogs();

            btnPrintLogs = Theme.MakeButton("🖨 طباعة سجل التسويات", Theme.Accent);
            btnPrintLogs.Location = new Point(20, 11);
            btnPrintLogs.Size = new Size(160, 32);
            btnPrintLogs.Click += (s, e) => PrintAdjustmentsLog();

            pnlTop.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblSearchLog, txtSearchLog, btnLoadLogs, btnPrintLogs });
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
            var dt = InventoryDAL.GetStock(txtSearch.Text);
            foreach (DataRow r in dt.Rows)
            {
                decimal bookQty = Convert.ToDecimal(r["BookQty"]);
                dgStock.Rows.Add(
                    r["ProductID"],
                    r["ProductCode"],
                    r["ProductName"],
                    r["Unit"],
                    Convert.ToDecimal(r["SalePrice"]).ToString("N2"),
                    bookQty.ToString("N3"),
                    "",   // ActualQty يبدأ فارغاً — المستخدم يُدخله يدوياً فقط للأصناف التي يجردها
                    ""    // DiffQty يبدأ فارغاً
                );
            }
            ClearAdjustmentForm();
        }

        private void LoadLogs()
        {
            dgLogs.Rows.Clear();
            var dt = InventoryDAL.GetAdjustments(dtpFrom.Value, dtpTo.Value, txtSearchLog.Text);
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
                _selectedProductID = Convert.ToInt32(r.Cells["ProductID"].Value);
                _selectedProductName = r.Cells["ProductName"].Value?.ToString();
                _selectedProductUnit = r.Cells["Unit"].Value?.ToString();
                _selectedBookQty = decimal.TryParse(r.Cells["BookQty"].Value?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal bq) ? bq : 0;

                lblSelectedProduct.Text = _selectedProductName;
                lblBookQtyVal.Text = _selectedBookQty.ToString("N3") + " " + _selectedProductUnit;

                // إذا المستخدم سبق وأدخل رصيداً فعلياً لهذا الصنف، نعرضه — وإلا نعرض الرصيد الدفتري كقيمة مقترحة
                string cellVal = r.Cells["ActualQty"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(cellVal) &&
                    decimal.TryParse(cellVal, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal gridActual))
                {
                    nudActualQty.Value = gridActual;
                }
                else
                {
                    nudActualQty.Value = _selectedBookQty;
                }
                UpdateDifference();
            }
            finally
            {
                _isSelecting = false;
            }
        }

        private void NudActualQty_ValueChanged(object sender, EventArgs e)
        {
            // تجاهل الحدث أثناء تحديد الصف لمنع الكتابة التلقائية في الخلية
            if (_isSelecting) return;

            UpdateDifference();

            if (dgStock.SelectedRows.Count > 0)
            {
                var r = dgStock.SelectedRows[0];
                r.Cells["ActualQty"].Value = nudActualQty.Value.ToString("N3");
                decimal diff = nudActualQty.Value - _selectedBookQty;
                r.Cells["DiffQty"].Value = (diff > 0 ? "+" : "") + diff.ToString("N3");

                if (diff > 0)
                    r.Cells["DiffQty"].Style.ForeColor = Color.LightGreen;
                else if (diff < 0)
                    r.Cells["DiffQty"].Style.ForeColor = Color.OrangeRed;
                else
                    r.Cells["DiffQty"].Style.ForeColor = Theme.TextMain;
            }
        }

        private void DgStock_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgStock.Rows[e.RowIndex];
            if (dgStock.Columns[e.ColumnIndex].Name == "ActualQty")
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var numStyles = System.Globalization.NumberStyles.Any;
                decimal.TryParse(row.Cells["BookQty"].Value?.ToString(), numStyles, inv, out decimal bookQty);
                string cellText = row.Cells["ActualQty"].Value?.ToString();

                if (!string.IsNullOrWhiteSpace(cellText) &&
                    decimal.TryParse(cellText, numStyles, inv, out decimal actualQty))
                {
                    decimal diff = actualQty - bookQty;
                    row.Cells["DiffQty"].Value = (diff > 0 ? "+" : "") + diff.ToString("N3");

                    if (diff > 0)
                        row.Cells["DiffQty"].Style.ForeColor = Color.LightGreen;
                    else if (diff < 0)
                        row.Cells["DiffQty"].Style.ForeColor = Color.OrangeRed;
                    else
                        row.Cells["DiffQty"].Style.ForeColor = Theme.TextMain;

                    if (Convert.ToInt32(row.Cells["ProductID"].Value) == _selectedProductID)
                    {
                        _isSelecting = true;
                        nudActualQty.Value = actualQty;
                        _isSelecting = false;
                        UpdateDifference();
                    }
                }
                else
                {
                    // المستخدم حذف القيمة — نعيدها فارغة (لم يُجرد هذا الصنف)
                    row.Cells["ActualQty"].Value = "";
                    row.Cells["DiffQty"].Value = "";
                    row.Cells["DiffQty"].Style.ForeColor = Theme.TextMain;

                    if (Convert.ToInt32(row.Cells["ProductID"].Value) == _selectedProductID)
                    {
                        _isSelecting = true;
                        nudActualQty.Value = bookQty;
                        _isSelecting = false;
                        UpdateDifference();
                    }
                }
            }
        }

        private void UpdateDifference()
        {
            decimal diff = nudActualQty.Value - _selectedBookQty;
            lblDiffVal.Text = (diff > 0 ? "+" : "") + diff.ToString("N3") + " " + _selectedProductUnit;

            if (diff == 0)
            {
                lblDiffVal.ForeColor = Theme.TextMain;
            }
            else if (diff > 0)
            {
                lblDiffVal.ForeColor = Color.LightGreen;
            }
            else
            {
                lblDiffVal.ForeColor = Color.OrangeRed;
            }
        }

        private void ClearAdjustmentForm()
        {
            _selectedProductID = 0;
            _selectedBookQty = 0;
            _selectedProductName = "";
            _selectedProductUnit = "";
            lblSelectedProduct.Text = "اختر صنفاً...";
            lblBookQtyVal.Text = "0.00";
            nudActualQty.Value = 0;
            lblDiffVal.Text = "0.00";
            lblDiffVal.ForeColor = Theme.TextMain;
            txtNotes.Clear();
        }

        private void BtnSaveAdj_Click(object sender, EventArgs e)
        {
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
                // عرض قائمة الأصناف المعدلة للتأكيد
                string msg = "هل أنت متأكد من حفظ تسوية كميات الأصناف التالية وتعديل أرصدتها في المخزن؟\n\n";
                int count = 0;
                foreach (var row in modifiedRows)
                {
                    string name = row.Cells["ProductName"].Value?.ToString();
                    decimal.TryParse(row.Cells["BookQty"].Value?.ToString(), numStyles, inv, out decimal book);
                    decimal.TryParse(row.Cells["ActualQty"].Value?.ToString(), numStyles, inv, out decimal actual);
                    decimal diff = actual - book;
                    
                    if (count < 10)
                    {
                        msg += $"• {name}: الدفتري ({book:N3}) ➔ الفعلي ({actual:N3}) [الفارق: {(diff > 0 ? "+" : "")}{diff:N3}]\n";
                    }
                    count++;
                }

                if (count > 10)
                {
                    msg += $"\n... وعدد {count - 10} أصناف أخرى.";
                }

                if (!string.IsNullOrEmpty(txtNotes.Text))
                {
                    msg += $"\n\nملاحظات التسوية: {txtNotes.Text}";
                }

                if (MessageBox.Show(msg, "تأكيد تسوية الكميات الجردية", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    int savedCount = 0;
                    foreach (var row in modifiedRows)
                    {
                        int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                        decimal.TryParse(row.Cells["BookQty"].Value?.ToString(), numStyles, inv, out decimal book);
                        decimal.TryParse(row.Cells["ActualQty"].Value?.ToString(), numStyles, inv, out decimal actual);
                        
                        int id = InventoryDAL.SaveAdjustment(pid, book, actual, txtNotes.Text);
                        if (id > 0) savedCount++;
                    }

                    if (savedCount > 0)
                    {
                        MessageBox.Show($"✅ تم حفظ وتطبيق التسوية الجردية لعدد ({savedCount}) أصناف وتعديل كميات المخزن بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadStock();
                        LoadLogs();
                    }
                    else
                    {
                        MessageBox.Show("❌ حدث خطأ أثناء محاولة حفظ تسوية الجرد.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                // في حال لم يتم تعديل أي صنف في الجدول، نتحقق من الصنف المحدد في القائمة الجانبية لحفظه (حتى لو كان الفارق صفراً كخط أساس)
                if (_selectedProductID == 0)
                {
                    MessageBox.Show("لم يتم تعديل أي كمية فعلية في الجدول. من فضلك اختر صنفاً أولاً أو قم بتعديل رصيده الفعلي.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal diff = nudActualQty.Value - _selectedBookQty;
                string msg = $"هل أنت متأكد من حفظ تسوية كمية هذا الصنف كخط أساس جردي؟\n\nالصنف: {_selectedProductName}\nالرصيد الدفتري الحالي: {_selectedBookQty:N3}\nالرصيد الفعلي المُدخل: {nudActualQty.Value:N3}\nالفارق الجردي: {(diff > 0 ? "+" : "")}{diff:N3}";
                
                if (MessageBox.Show(msg, "تأكيد التسوية الجردية", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    int id = InventoryDAL.SaveAdjustment(_selectedProductID, _selectedBookQty, nudActualQty.Value, txtNotes.Text);
                    if (id > 0)
                    {
                        MessageBox.Show("✅ تم حفظ وتطبيق التسوية الجردية وتعديل كمية المخزن بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadStock();
                        LoadLogs();
                    }
                    else
                    {
                        MessageBox.Show("❌ حدث خطأ أثناء محاولة حفظ تسوية الجرد.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
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
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
        }
    }
}

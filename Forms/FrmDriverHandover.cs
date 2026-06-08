using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة تقفيل حمولة المندوب</summary>
    public class FrmDriverHandover : Form
    {
        private Panel pnlHeader;
        private Label lblTitle;
        private ComboBox cboDriver, cboLoad;
        private Label lblDriver, lblLoad;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnSearch;
        private DataGridView dgItems;
        private Button btnLoadItems, btnSave;
        private TextBox txtNotes, txtCashCollected;
        private ComboBox cboDeadTreatment;
        private Label lblTotLoad, lblTotRet, lblTotDead, lblTotExtra, lblTotDef, lblExpCash;

        private int _loadID = 0;
        private int _driverID = 0;
        private List<HandoverItemDTO> _items = new List<HandoverItemDTO>();

        public FrmDriverHandover()
        {
            InitUI();
            LoadDrivers();
        }

        private void InitUI()
        {
            this.Text = "تقفيل حمولة مندوب";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== 1. Selection panel (FlowLayoutPanel for responsive layout) =====
            var pnlSel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10),
                WrapContents = true
            };

            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            dtpFrom = new DateTimePicker { Width = 120, Height = 26, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            
            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            dtpTo = new DateTimePicker { Width = 120, Height = 26, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            btnSearch = Theme.MakeButton("🔍 بحث حمولات", Theme.Accent);
            btnSearch.Size = new Size(130, 28);
            btnSearch.Margin = new Padding(10, 0, 0, 0);
            btnSearch.Click += (s, e) => CboDriver_SelectedIndexChanged(null, null);

            lblDriver = new Label { Text = "المندوب :", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            cboDriver = new ComboBox 
            { 
                Width = 220, 
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown, 
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboDriver.SelectedIndexChanged += CboDriver_SelectedIndexChanged;

            lblLoad = new Label { Text = "الحمولة :", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 5, 0, 0) };
            cboLoad = new ComboBox 
            { 
                Width = 280, 
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown, 
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboLoad.SelectedIndexChanged += CboLoad_SelectedIndexChanged;

            btnLoadItems = Theme.MakeButton("📋 تحميل البيانات", Theme.Accent);
            btnLoadItems.Size = new Size(140, 28);
            btnLoadItems.Margin = new Padding(20, 0, 0, 0);
            btnLoadItems.Click += BtnLoadItems_Click;

            pnlSel.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnSearch, lblDriver, cboDriver, lblLoad, cboLoad, btnLoadItems });


            // ===== 2. Grid Panel (No AutoScroll conflict) =====
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 10) };
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                EditMode = DataGridViewEditMode.EditOnEnter,
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
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر الوحدة", ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "LoadedQty", HeaderText = "المحمل", ReadOnly = true, FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldQty", HeaderText = "المبيعات", FillWeight = 50, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.Blue, BackColor = Color.FromArgb(245, 245, 255) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnedQty", HeaderText = "المرتجع", FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DeadQty", HeaderText = "النافق", FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExtraQty", HeaderText = "الزيادة", ReadOnly = true, FillWeight = 50, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.Green, BackColor = Color.FromArgb(235, 255, 235) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DeficitQty", HeaderText = "العجز", ReadOnly = true, FillWeight = 50, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.Red, BackColor = Color.FromArgb(255, 235, 235) } });

            dgItems.CellValueChanged += DgItems_CellValueChanged;
            dgItems.CurrentCellDirtyStateChanged += (s, e) => { if (dgItems.IsCurrentCellDirty) dgItems.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            pnlGrid.Controls.Add(dgItems);


            // ===== 3. Summary + Footer Panel =====
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 5, 10, 5)
            };

            // Network style table for 6 metrics
            var pnlSummaryTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 55,
                ColumnCount = 6,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgCard
            };
            for(int i=0; i<6; i++)
                pnlSummaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
            pnlSummaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTotLoad = AddHandoverSummaryCard(pnlSummaryTable, "إجمالي المحمل:", "0.00", Color.White, 0);
            lblTotRet = AddHandoverSummaryCard(pnlSummaryTable, "المرتجع:", "0.00", Color.LightGreen, 1);
            lblTotDead = AddHandoverSummaryCard(pnlSummaryTable, "النافق:", "0.00", Color.OrangeRed, 2);
            lblExpCash = AddHandoverSummaryCard(pnlSummaryTable, "المتوقع نقداً:", "0.00", Color.LightSkyBlue, 3);
            lblTotExtra = AddHandoverSummaryCard(pnlSummaryTable, "الزيادة (كمية):", "0.00", Color.Yellow, 4);
            lblTotDef = AddHandoverSummaryCard(pnlSummaryTable, "العجز (كمية):", "0.00", Color.Red, 5);

            pnlFooter.Controls.Add(pnlSummaryTable);

            // Flow row for Actions & Cash Collection
            var pnlActionsRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(0, 10, 0, 5)
            };

            var lblCashL = new Label { Text = "المبلغ المحصل نقداً:", AutoSize = true, ForeColor = Theme.Primary, Font = new Font("Segoe UI", 12, FontStyle.Bold), Margin = new Padding(0, 5, 5, 0) };
            txtCashCollected = new TextBox { Width = 130, Height = 32, Font = new Font("Segoe UI", 14, FontStyle.Bold), BackColor = Color.LightYellow, ForeColor = Color.DarkGreen, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle, Text = "0.00" };

            var lblDeadT = new Label { Text = "معالجة النافق:", AutoSize = true, ForeColor = Theme.TextSub, Margin = new Padding(20, 8, 5, 0) };
            cboDeadTreatment = new ComboBox
            {
                Width = 180,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f)
            };
            cboDeadTreatment.Items.AddRange(new object[] {
                "تحميل الخزنة (مصروف عام)",
                "سلفة على المندوب",
                "خصم من مستحقات المندوب"
            });
            cboDeadTreatment.SelectedIndex = 0;

            var lblNotesL = new Label { Text = "ملاحظات التقفيل:", AutoSize = true, ForeColor = Theme.TextSub, Margin = new Padding(20, 8, 5, 0) };
            txtNotes = new TextBox { Width = 200, Height = 32, Font = new Font("Segoe UI", 11), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle };

            btnSave = Theme.MakeButton("💾 حفظ التقفيل", Theme.Accent);
            btnSave.Size = new Size(140, 32);
            btnSave.Margin = new Padding(20, 0, 0, 0);
            btnSave.Click += BtnSave_Click;

            pnlActionsRow.Controls.AddRange(new Control[] { lblCashL, txtCashCollected, lblDeadT, cboDeadTreatment, lblNotesL, txtNotes, btnSave });
            pnlFooter.Controls.Add(pnlActionsRow);


            // ===== 4. Add to form in correct Z-order docking hierarchy =====
            this.Controls.Add(pnlGrid);   // Dock = Fill (added last, fills middle)
            this.Controls.Add(pnlFooter); // Dock = Bottom
            this.Controls.Add(pnlSel);    // Dock = Top

            Theme.ApplyFormRTL(this);
        }

        private Label AddHandoverSummaryCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIndex)
        {
            var pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(2)
            };

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 16,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.TopRight
            };

            var lblVal = new Label
            {
                Text = val,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.BottomRight
            };

            pnlCard.Controls.Add(lblVal);
            pnlCard.Controls.Add(lblTitle);
            parent.Controls.Add(pnlCard, colIndex, 0);

            return lblVal;
        }

        private void LoadDrivers()
        {
            var dt = EmployeeDAL.GetDrivers();
            cboDriver.Items.Clear();
            cboDriver.Items.Add(new ComboItem(0, "-- اختر مندوب --"));
            foreach (DataRow r in dt.Rows)
                cboDriver.Items.Add(new ComboItem((int)r["EmpID"], r["EmpName"].ToString()));
            cboDriver.DisplayMember = "Text";
            cboDriver.SelectedIndex = 0;
        }

        private void CboDriver_SelectedIndexChanged(object sender, EventArgs e)
        {
            cboLoad.Items.Clear();
            if (!(cboDriver.SelectedItem is ComboItem ci) || ci.ID == 0) return;
            _driverID = ci.ID;

            var dt = DriverDAL.GetOpenLoads(ci.ID, dtpFrom.Value.Date, dtpTo.Value.Date);
            cboLoad.Items.Add(new ComboItem(0, "-- اختر حمولة --"));
            foreach (DataRow r in dt.Rows)
                cboLoad.Items.Add(new ComboItem((int)r["LoadID"],
                    $"{r["LoadDate"]:dd/MM/yyyy}  |  {r["SaleCode"]}  |  {r["TotalAmount"]:N0} ج"));
            cboLoad.DisplayMember = "Text";
            cboLoad.SelectedIndex = 0;
        }

        private void CboLoad_SelectedIndexChanged(object sender, EventArgs e)
        {
            _loadID = (cboLoad.SelectedItem is ComboItem ci) ? ci.ID : 0;
        }

        private void BtnLoadItems_Click(object sender, EventArgs e)
        {
            if (_loadID == 0) { MessageBox.Show("اختر الحمولة أولاً"); return; }

            var dt = DriverDAL.GetLoadItems(_loadID);
            _items.Clear();
            dgItems.Rows.Clear();

            foreach (DataRow r in dt.Rows)
            {
                var item = new HandoverItemDTO
                {
                    ProductID = (int)r["ProductID"],
                    ProductName = r["ProductName"].ToString(),
                    LoadedQty = Convert.ToDecimal(r["LoadedQty"]),
                    SoldQty = Convert.ToDecimal(r["SoldQty"]),
                    UnitPrice = Convert.ToDecimal(r["UnitPrice"])
                };
                
                // حساب تلقائي في حالة عدم وجود مبيعات مسجلة مسبقاً (تسهيلاً على المحاسب)
                if (item.SoldQty == 0)
                {
                    item.SoldQty = item.LoadedQty; // افتراض أن الكل بيع إلى أن يتم تعديل المرتجع
                }
                
                _items.Add(item);
                var row = dgItems.Rows.Add(
                    item.ProductName,
                    item.UnitPrice.ToString("N2"),
                    item.LoadedQty.ToString("F2"),
                    item.SoldQty.ToString("F2"),
                    "0",
                    "0",
                    item.ExtraQty.ToString("F2"),
                    item.DeficitQty.ToString("F2")
                );
            }
            UpdateTotals();
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var item = _items[e.RowIndex];
            var row = dgItems.Rows[e.RowIndex];

            // اسم العمود الذي تم تعديله
            string colName = dgItems.Columns[e.ColumnIndex].Name;

            if (colName == "ReturnedQty" || colName == "DeadQty")
            {
                TryParse(row, "ReturnedQty", v => item.ReturnedQty = v);
                TryParse(row, "DeadQty", v => item.DeadQty = v);
                
                // تعديل المبيعات تلقائياً بناءً على المرتجع والنافق
                decimal newSold = item.LoadedQty - item.ReturnedQty - item.DeadQty;
                if (newSold < 0) newSold = 0;
                item.SoldQty = newSold;
                row.Cells["SoldQty"].Value = item.SoldQty.ToString("F2");
            }
            else if (colName == "SoldQty")
            {
                TryParse(row, "SoldQty", v => item.SoldQty = v);
            }

            row.Cells["ExtraQty"].Value = item.ExtraQty.ToString("F2");
            row.Cells["DeficitQty"].Value = item.DeficitQty.ToString("F2");
            UpdateTotals();
        }

        private void TryParse(DataGridViewRow row, string col, Action<decimal> setter)
        {
            if (decimal.TryParse(row.Cells[col].Value?.ToString(), out decimal v))
            {
                if (v < 0) v = 0;
                setter(v);
            }
        }

        private void UpdateTotals()
        {
            decimal tl = 0, tr = 0, td = 0, te = 0, tdf = 0, tCash = 0;
            foreach (var it in _items) 
            { 
                tl += it.LoadedQty; 
                tr += it.ReturnedQty; 
                td += it.DeadQty; 
                te += it.ExtraQty; 
                tdf += it.DeficitQty; 
                tCash += (it.SoldQty * it.UnitPrice);
            }
            lblTotLoad.Text = tl.ToString("N2");
            lblTotRet.Text = tr.ToString("N2");
            lblTotDead.Text = td.ToString("N2");
            lblTotExtra.Text = te.ToString("N2");
            lblTotDef.Text = tdf.ToString("N2");
            lblExpCash.Text = tCash.ToString("N2");
            
            // تحديث الحقل النصي الخاص بالمحصل إذا لم يقم المحاسب بإدخال قيمة يدوية
            if (!txtCashCollected.Focused)
                txtCashCollected.Text = tCash.ToString("N2");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_loadID == 0 || _items.Count == 0) { MessageBox.Show("لا توجد بيانات للحفظ"); return; }
            if (!decimal.TryParse(txtCashCollected.Text, out decimal cashCollected) || cashCollected < 0)
            {
                MessageBox.Show("يرجى إدخال المبلغ المحصل بشكل صحيح");
                return;
            }

            // التحقق من صحة البيانات والالتزام بالكمية المحملة
            foreach (var item in _items)
            {
                // لا يمكن للنافق أن يتخطى المحمل
                if (item.DeadQty > item.LoadedQty)
                {
                    MessageBox.Show($"❌ خطأ: لا يمكن للنافق ({item.DeadQty:F2}) أن يزيد عن الكمية المحملة ({item.LoadedQty:F2}) للصنف: {item.ProductName}", 
                        "خطأ في البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // حالة الزيادة (إذا كان مجموع المبيعات والمرتجع يتخطى المحمل)
                if (item.ExtraQty > 0)
                {
                    var res = MessageBox.Show($"⚠️ تنبيه: يوجد زيادة بقيمة ({item.ExtraQty:F2}) في صنف ({item.ProductName}).\nهل هذه الزيادة صحيحة؟", 
                        "تأكيد الزيادة", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    
                    if (res != DialogResult.Yes)
                    {
                        MessageBox.Show("❌ تم إلغاء الحفظ.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }
            }

            if (MessageBox.Show("هل تريد تقفيل الحمولة وإغلاقها نهائياً؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            int deadTreatment = cboDeadTreatment.SelectedIndex;
            int hvID = DriverDAL.SaveHandover(_loadID, _driverID, _items, txtNotes.Text, cashCollected, deadTreatment);
            if (hvID > 0)
            {
                MessageBox.Show($"✅ تم تقفيل الحمولة بنجاح!\nتم تسجيل مبلغ {cashCollected:N2} ج في الخزينة كمبيعات نقدية.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CboDriver_SelectedIndexChanged(null, null);
                dgItems.Rows.Clear();
                _items.Clear();
                _loadID = 0;
                txtNotes.Clear();
                txtCashCollected.Text = "0.00";
                cboDeadTreatment.SelectedIndex = 0;
            }
            else MessageBox.Show("❌ فشل الحفظ", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

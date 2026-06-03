using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
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
        private Button btnLoadItems, btnSave, btnWhatsApp;
        private TextBox txtNotes, txtCashCollected;
        private Label lblTotLoad, lblTotRet, lblTotDead, lblTotExtra, lblTotDef, lblExpCash;
        private Label lblDeficitValue; // بطاقة القيمة المالية للعجز الكلي

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

            // 7 بطاقات: المحمل، المرتجع، النافق، المتوقع، الزيادة، العجز كمية، قيمة العجز المالي
            pnlSummaryTable.ColumnCount = 7;
            pnlSummaryTable.ColumnStyles.Clear();
            for (int i = 0; i < 7; i++)
                pnlSummaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7f));

            lblTotLoad  = AddHandoverSummaryCard(pnlSummaryTable, "إجمالي المحمل:",    "0.00", Color.White,        0);
            lblTotRet   = AddHandoverSummaryCard(pnlSummaryTable, "المرتجع:",           "0.00", Color.LightGreen,    1);
            lblTotDead  = AddHandoverSummaryCard(pnlSummaryTable, "النافق:",             "0.00", Color.OrangeRed,     2);
            lblExpCash  = AddHandoverSummaryCard(pnlSummaryTable, "المتوقع نقداً:",     "0.00", Color.LightSkyBlue,  3);
            lblTotExtra = AddHandoverSummaryCard(pnlSummaryTable, "الزيادة (كمية):",    "0.00", Color.Yellow,        4);
            lblTotDef   = AddHandoverSummaryCard(pnlSummaryTable, "العجز (كمية):",      "0.00", Color.Red,           5);
            lblDeficitValue = AddHandoverSummaryCard(pnlSummaryTable, "💰 قيمة العجز:", "0.00 ج", Color.FromArgb(255, 80, 80), 6);

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
            txtCashCollected = new TextBox { Width = 150, Height = 32, Font = new Font("Segoe UI", 14, FontStyle.Bold), BackColor = Color.LightYellow, ForeColor = Color.DarkGreen, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle, Text = "0.00" };

            var lblNotesL = new Label { Text = "ملاحظات التقفيل:", AutoSize = true, ForeColor = Theme.TextSub, Margin = new Padding(20, 10, 5, 0) };
            txtNotes = new TextBox { Width = 260, Height = 32, Font = new Font("Segoe UI", 11), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle };

            btnSave = Theme.MakeButton("💾 حفظ التقفيل", Theme.Accent);
            btnSave.Size = new Size(140, 32);
            btnSave.Margin = new Padding(20, 0, 0, 0);
            btnSave.Click += BtnSave_Click;

            // زر تصدير كشف التحصيل لواتساب
            btnWhatsApp = Theme.MakeButton("📲 كشف واتساب", Color.FromArgb(37, 211, 102));
            btnWhatsApp.Size = new Size(140, 32);
            btnWhatsApp.Margin = new Padding(15, 0, 0, 0);
            btnWhatsApp.Click += BtnWhatsApp_Click;

            pnlActionsRow.Controls.AddRange(new Control[] { lblCashL, txtCashCollected, lblNotesL, txtNotes, btnSave, btnWhatsApp });
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
            decimal tl = 0, tr = 0, td = 0, te = 0, tdf = 0, tCash = 0, tDeficitVal = 0;
            foreach (var it in _items)
            {
                tl  += it.LoadedQty;
                tr  += it.ReturnedQty;
                td  += it.DeadQty;
                te  += it.ExtraQty;
                tdf += it.DeficitQty;
                tDeficitVal += it.DeficitValue; // القيمة المالية للعجز
                tCash += (it.SoldQty * it.UnitPrice);
            }
            lblTotLoad.Text  = tl.ToString("N2");
            lblTotRet.Text   = tr.ToString("N2");
            lblTotDead.Text  = td.ToString("N2");
            lblTotExtra.Text = te.ToString("N2");
            lblTotDef.Text   = tdf.ToString("N2");
            lblExpCash.Text  = tCash.ToString("N2");

            // عرض القيمة المالية للعجز باللون الأحمر
            lblDeficitValue.Text = tDeficitVal > 0
                ? $"{tDeficitVal:N2} ج"
                : "0.00 ج";
            lblDeficitValue.ForeColor = tDeficitVal > 0
                ? Color.FromArgb(255, 60, 60)
                : Color.FromArgb(100, 200, 100);

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

            foreach (var item in _items)
            {
                if (item.DeadQty > item.LoadedQty)
                {
                    MessageBox.Show($"❌ خطأ: لا يمكن للنافق ({item.DeadQty:F2}) أن يزيد عن الكمية المحملة ({item.LoadedQty:F2}) للصنف: {item.ProductName}",
                        "خطأ في البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

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

            int hvID = DriverDAL.SaveHandover(_loadID, _driverID, _items, txtNotes.Text, cashCollected);
            if (hvID <= 0)
            {
                MessageBox.Show("❌ فشل الحفظ", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ===== معالجة العجز المالي الذكي =====
            decimal totalDeficitValue = 0;
            foreach (var it in _items)
                totalDeficitValue += it.DeficitValue;

            if (totalDeficitValue > 0.01m)
            {
                ShowDeficitSettlementDialog(totalDeficitValue, _loadID);
            }

            MessageBox.Show(
                $"✅ تم تقفيل الحمولة بنجاح!\nتم تسجيل مبلغ {cashCollected:N2} ج في الخزينة.",
                "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);

            CboDriver_SelectedIndexChanged(null, null);
            dgItems.Rows.Clear();
            _items.Clear();
            _loadID = 0;
            txtNotes.Clear();
            txtCashCollected.Text = "0.00";
        }

        /// <summary>نافذة تسوية العجز المالي — 3 خيارات للمحاسب</summary>
        private void ShowDeficitSettlementDialog(decimal deficitValue, int loadID)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "💰 تسوية العجز المالي";
                dlg.Size = new Size(500, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Theme.BgMain;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;

                var pnl = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    Padding = new Padding(20),
                    AutoSize = false
                };

                var lblTitle = new Label
                {
                    Text = $"⚠️ يوجد عجز مالي بقيمة: {deficitValue:N2} ج",
                    Font = new Font("Segoe UI", 13, FontStyle.Bold),
                    ForeColor = Color.FromArgb(255, 80, 80),
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 10)
                };

                var lblSub = new Label
                {
                    Text = "(النافق يُحسب على المندوب — اختر طريقة تسوية العجز:)",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Theme.TextSub,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 15)
                };

                var btnAdvance = Theme.MakeButton("📋 سلفة / مديونية على المندوب", Theme.Primary);
                btnAdvance.Size = new Size(420, 44);
                btnAdvance.Margin = new Padding(0, 0, 0, 8);
                btnAdvance.Click += (s, ev) =>
                {
                    DriverDAL.SettleDeficit(_driverID, loadID, deficitValue, "Advance",
                        $"عجز حمولة #{loadID} — قيمة {deficitValue:N2} ج");
                    MessageBox.Show("✅ تم تسجيل العجز كسلفة على المندوب.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                };

                var btnCompany = Theme.MakeButton("🏢 تحميل على الشركة (مصروف تشغيلي)", Color.FromArgb(100, 100, 200));
                btnCompany.Size = new Size(420, 44);
                btnCompany.Margin = new Padding(0, 0, 0, 8);
                btnCompany.Click += (s, ev) =>
                {
                    DriverDAL.SettleDeficit(_driverID, loadID, deficitValue, "CompanyExpense",
                        $"عجز تشغيلي — حمولة #{loadID} — مندوب #{_driverID} — {deficitValue:N2} ج");
                    MessageBox.Show("✅ تم تحميل العجز على الشركة كمصروف تشغيلي.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                };

                var btnDeduct = Theme.MakeButton("✂️ خصم من مستحقات المندوب", Color.FromArgb(200, 100, 0));
                btnDeduct.Size = new Size(420, 44);
                btnDeduct.Margin = new Padding(0, 0, 0, 8);
                btnDeduct.Click += (s, ev) =>
                {
                    DriverDAL.SettleDeficit(_driverID, loadID, deficitValue, "Deduction",
                        $"خصم عجز حمولة #{loadID} — {deficitValue:N2} ج");
                    MessageBox.Show("✅ تم تسجيل الخصم من مستحقات المندوب.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                };

                var btnSkip = new Button
                {
                    Text = "تجاهل الآن",
                    Size = new Size(120, 32),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Theme.TextSub,
                    BackColor = Theme.BgCard
                };
                btnSkip.Click += (s, ev) => dlg.Close();

                pnl.Controls.AddRange(new Control[] { lblTitle, lblSub, btnAdvance, btnCompany, btnDeduct, btnSkip });
                dlg.Controls.Add(pnl);
                dlg.ShowDialog(this);
            }
        }

        /// <summary>تصدير كشف التحصيل اليومي جاهزاً للإرسال عبر واتساب</summary>
        private void BtnWhatsApp_Click(object sender, EventArgs e)
        {
            if (!(_driverID > 0))
            {
                MessageBox.Show("اختر المندوب أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dt = DriverDAL.GetDriverCollectionList(_driverID, DateTime.Today);
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("لا يوجد عملاء بديون لهذا المندوب اليوم.", "كشف التحصيل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string driverName = cboDriver.Text.Trim();
            var sb = new StringBuilder();
            sb.AppendLine($"📋 كشف تحصيل يوم {DateTime.Today:dd/MM/yyyy}");
            sb.AppendLine($"المندوب: {driverName}");
            sb.AppendLine(new string('─', 30));

            decimal totalBalance = 0;
            int idx = 1;
            foreach (DataRow r in dt.Rows)
            {
                decimal bal   = Convert.ToDecimal(r["Balance"]);
                string  phone = r["Phone"].ToString();
                string  name  = r["ClientName"].ToString();
                totalBalance += bal;

                sb.AppendLine($"{idx}. {name}");
                sb.AppendLine($"   📞 {phone}");
                sb.AppendLine($"   💰 الدين: {bal:N2} ج");
                sb.AppendLine();
                idx++;
            }

            sb.AppendLine(new string('─', 30));
            sb.AppendLine($"إجمالي المطلوب تحصيله: {totalBalance:N2} ج");

            // نسخ النص للحافظة
            Clipboard.SetText(sb.ToString());

            MessageBox.Show(
                $"✅ تم نسخ كشف التحصيل ({dt.Rows.Count} عملاء — إجمالي {totalBalance:N2} ج)\n\n" +
                "الكشف موجود في الحافظة (Clipboard)، افتح واتساب ويب أو أي تطبيق وألصق النص مباشرةً.",
                "كشف واتساب جاهز", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

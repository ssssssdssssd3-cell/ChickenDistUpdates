using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة الخزنة والمصروفات</summary>
    public class FrmCashBox : Form
    {
        private TabControl tabMain;
        private TabPage tabCash, tabExpenses;

        // Cash tab
        private DataGridView dgCash;
        private DateTimePicker dtpCashFrom, dtpCashTo;
        private Button btnLoadCash;
        private Label lblCashBalance, lblCashIn, lblCashOut;

        // Expenses tab
        private DataGridView dgExpenses;
        private DateTimePicker dtpExpFrom, dtpExpTo;
        private Button btnLoadExp, btnNewExp, btnSaveExp, btnDelExp;
        private ComboBox cboExpType;
        private ComboBox cboExpSupplier;
        private ComboBox cboExpVehicleType;
        private ComboBox cboExpVehicle;
        private TextBox txtExpNotes;
        private NumericUpDown nudExpAmount;
        private DateTimePicker dtpExpDate;
        private int _selectedExpID = 0;
        private int _selectedSupplierForExpense = 0;
        private string _selectedSupplierNameForExpense;
        private DataTable _vehiclesForExpenseFilter;

        public FrmCashBox()
        {
            InitUI();
            LoadCashBox();
            LoadExpenses();
        }

        public FrmCashBox(int supplierID, string supplierName) : this()
        {
            _selectedSupplierForExpense = supplierID;
            _selectedSupplierNameForExpense = supplierName;
            tabMain.SelectedTab = tabExpenses;
            SelectSupplierInExpenseCombo(supplierID);
            cboExpType.Focus();
        }

        private void InitUI()
        {
            this.Text = "الخزنة والمصروفات";
            this.Size = new Size(1000, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // header handled by main form's top bar

            tabMain = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontMain };
            tabCash = new TabPage("حركات الخزنة") { BackColor = Theme.BgMain };
            tabExpenses = new TabPage("المصروفات") { BackColor = Theme.BgMain };
            tabMain.TabPages.AddRange(new[] { tabCash, tabExpenses });
            this.Controls.Add(tabMain);

            BuildCashTab();
            BuildExpensesTab();
            LoadSuppliersToCombo();
            LoadVehicleFilters();

            Theme.ApplyFormRTL(this);
        }

        private void BuildCashTab()
        {
            var pnlF = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Top, 
                Height = 55, 
                BackColor = Theme.BgCard, 
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            
            pnlF.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            dtpCashFrom = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), Margin = new Padding(10, 4, 0, 0) };
            pnlF.Controls.Add(dtpCashFrom);
            
            pnlF.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0) });
            dtpCashTo = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Margin = new Padding(10, 4, 0, 0) };
            pnlF.Controls.Add(dtpCashTo);
            
            btnLoadCash = Theme.MakeButton("عرض", Theme.Accent);
            btnLoadCash.Size = new Size(80, 32);
            btnLoadCash.Margin = new Padding(30, 0, 0, 0);
            btnLoadCash.Click += (s, e) => LoadCashBox();
            pnlF.Controls.Add(btnLoadCash);

            var btnDeposit = Theme.MakeButton("➕ توريد نقدية", Color.FromArgb(40, 130, 80));
            btnDeposit.Size = new Size(130, 32);
            btnDeposit.Margin = new Padding(20, 0, 0, 0);
            btnDeposit.Click += BtnDeposit_Click;
            pnlF.Controls.Add(btnDeposit);

            var btnWithdraw = Theme.MakeButton("➖ سحب نقدية", Color.FromArgb(170, 70, 70));
            btnWithdraw.Size = new Size(130, 32);
            btnWithdraw.Margin = new Padding(10, 0, 0, 0);
            btnWithdraw.Click += BtnWithdraw_Click;
            pnlF.Controls.Add(btnWithdraw);

            var btnReconcile = Theme.MakeButton("⚖️ تسوية الخزنة", Color.FromArgb(120, 90, 40));
            btnReconcile.Size = new Size(130, 32);
            btnReconcile.Margin = new Padding(10, 0, 0, 0);
            btnReconcile.Click += BtnReconcile_Click;
            pnlF.Controls.Add(btnReconcile);

            dgCash = MakeGrid();
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ", FillWeight = 50 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "النوع" });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "AmountIn", HeaderText = "وارد", FillWeight = 45 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "AmountOut", HeaderText = "صادر", FillWeight = 45 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "صافي", FillWeight = 45 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان" });

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGrid.Controls.Add(dgCash);

            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(8) };
            lblCashBalance = new Label { Text = "رصيد الخزنة: ---", ForeColor = Theme.Accent, Location = new Point(10, 15), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            lblCashIn = new Label { Text = "إجمالي وارد: 0", ForeColor = Color.LightGreen, Location = new Point(250, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblCashOut = new Label { Text = "إجمالي صادر: 0", ForeColor = Color.OrangeRed, Location = new Point(450, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pnlFoot.Controls.AddRange(new Control[] { lblCashBalance, lblCashIn, lblCashOut });

            tabCash.Controls.Add(pnlGrid); // Fill
            tabCash.Controls.Add(pnlF);    // Top
            tabCash.Controls.Add(pnlFoot); // Bottom
        }

        private void BuildExpensesTab()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f)); // Column 0 (Right): Details
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f)); // Column 1 (Left): Grid
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Left: Grid Panel (Column 1)
            var pnlList = new Panel { Dock = DockStyle.Fill };
            
            var pnlF = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Top, 
                Height = 55, 
                BackColor = Theme.BgCard, 
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            
            pnlF.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            dtpExpFrom = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), Margin = new Padding(10, 4, 0, 0) };
            pnlF.Controls.Add(dtpExpFrom);
            
            pnlF.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0) });
            dtpExpTo = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Margin = new Padding(10, 4, 0, 0) };
            pnlF.Controls.Add(dtpExpTo);

            pnlF.Controls.Add(new Label { Text = "نوع العربية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0) });
            cboExpVehicleType = new ComboBox
            {
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(10, 4, 0, 0)
            };
            cboExpVehicleType.SelectedIndexChanged += (s, e) => UpdateVehicleFilter();
            pnlF.Controls.Add(cboExpVehicleType);

            pnlF.Controls.Add(new Label { Text = "اسم العربية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0) });
            cboExpVehicle = new ComboBox
            {
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(10, 4, 0, 0)
            };
            pnlF.Controls.Add(cboExpVehicle);
            
            btnLoadExp = Theme.MakeButton("عرض", Theme.Accent);
            btnLoadExp.Size = new Size(80, 32);
            btnLoadExp.Margin = new Padding(30, 0, 0, 0);
            btnLoadExp.Click += (s, e) => LoadExpenses();
            pnlF.Controls.Add(btnLoadExp);

            dgExpenses = MakeGrid();
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpenseID", Visible = false });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "VehicleID", Visible = false });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpenseDate", HeaderText = "التاريخ", FillWeight = 35 });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpenseType", HeaderText = "النوع", FillWeight = 25 });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Supplier", HeaderText = "المورد", FillWeight = 25 });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vehicle", HeaderText = "العربة / المركبة", FillWeight = 25 });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "المبلغ", FillWeight = 20 });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان" });
            dgExpenses.SelectionChanged += DgExpenses_SelectionChanged;

            var pnlGridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGridContainer.Controls.Add(dgExpenses);

            pnlList.Controls.Add(pnlGridContainer);
            pnlList.Controls.Add(pnlF);

            // Right: Details Panel (Column 0)
            var pnlDetails = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(15) };

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 320,
                ColumnCount = 2,
                RowCount = 6,
                RightToLeft = RightToLeft.Yes,
                Padding = new Padding(5)
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f)); // Label column
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f)); // Input column
            for (int i=0; i<6; i++) tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

            // Row 0: Date
            var lblDate = new Label { Text = "التاريخ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            dtpExpDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 5, 0, 5) };
            tblFields.Controls.Add(lblDate, 0, 0);
            tblFields.Controls.Add(dtpExpDate, 1, 0);

            // Row 1: Type
            var lblType = new Label { Text = "نوع المصروف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cboExpType = new ComboBox 
            { 
                Dock = DockStyle.Fill, 
                DropDownStyle = ComboBoxStyle.DropDown, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                Margin = new Padding(0, 5, 0, 5)
            };
            cboExpType.Items.AddRange(new object[] { "رواتب", "وقود", "صيانة", "مصروف إداري", "مواد تغليف", "نقل", "أخرى" });
            tblFields.Controls.Add(lblType, 0, 1);
            tblFields.Controls.Add(cboExpType, 1, 1);

            // Row 3: Supplier (optional)
            var lblSupplier = new Label { Text = "المورد (اختياري):", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cboExpSupplier = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Margin = new Padding(0,5,0,5) };
            tblFields.Controls.Add(lblSupplier, 0, 3);
            tblFields.Controls.Add(cboExpSupplier, 1, 3);

            // Row 4: Vehicle (optional)
            var lblVehicle = new Label { Text = "العربية / المركبة (اختياري):", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cboExpVehicle = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Margin = new Padding(0,5,0,5) };
            tblFields.Controls.Add(lblVehicle, 0, 4);
            tblFields.Controls.Add(cboExpVehicle, 1, 4);

            // Row 2: Amount
            var lblAmountVal = new Label { Text = "المبلغ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            nudExpAmount = new NumericUpDown 
            { 
                Dock = DockStyle.Fill, 
                Minimum = 0, 
                Maximum = 9999999, 
                DecimalPlaces = 2, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                Margin = new Padding(0, 5, 0, 5)
            };
            tblFields.Controls.Add(lblAmountVal, 0, 2);
            tblFields.Controls.Add(nudExpAmount, 1, 2);

            // Row 5: Notes
            var lblNotesVal = new Label { Text = "البيان:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            txtExpNotes = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                Margin = new Padding(0, 5, 0, 5),
                BorderStyle = BorderStyle.FixedSingle
            };
            tblFields.Controls.Add(lblNotesVal, 0, 5);
            tblFields.Controls.Add(txtExpNotes, 1, 5);

            pnlDetails.Controls.Add(tblFields);

            // Bottom Actions (Absolute positioning inside pnlDetails)
            btnNewExp = Theme.MakeButton("🆕 جديد", 20, 330, 85, 38, Color.FromArgb(60, 100, 60));
            btnNewExp.Click += (s, e) => ClearExp();

            btnSaveExp = Theme.MakeButton("💾 حفظ المصروف", 115, 330, 115, 38, Theme.Accent);
            btnSaveExp.Click += BtnSaveExp_Click;

            btnDelExp = Theme.MakeButton("🗑 حذف", 240, 330, 80, 38, Color.FromArgb(140, 40, 40));
            btnDelExp.Click += BtnDelExp_Click;

            pnlDetails.Controls.AddRange(new Control[] { btnNewExp, btnSaveExp, btnDelExp });
            
            btnNewExp.BringToFront();
            btnSaveExp.BringToFront();
            btnDelExp.BringToFront();

            tbl.Controls.Add(pnlDetails, 0, 0); // Right
            tbl.Controls.Add(pnlList, 1, 0);    // Left

            tabExpenses.Controls.Add(tbl);
        }

        private void AddDetailLabel(Control parent, string text, int y)
        {
            parent.Controls.Add(new Label { Text = text, Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
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

        private void LoadCashBox()
        {
            dgCash.Rows.Clear();
            var dt = AccountDAL.GetCashBox(dtpCashFrom.Value, dtpCashTo.Value);
            decimal totIn = 0, totOut = 0;
            foreach (DataRow r in dt.Rows)
            {
                decimal inAmt = Convert.ToDecimal(r["AmountIn"]);
                decimal outAmt = Convert.ToDecimal(r["AmountOut"]);
                decimal net = inAmt - outAmt;
                
                string transType = r["TransType"].ToString();
                string transTypeArabic = transType switch
                {
                    "Deposit" => "توريد نقدية",
                    "Withdraw" => "سحب نقدية",
                    "SaleIncome" => "بيع نقدي",
                    "ClientPayment" => "تحصيل من عميل",
                    "Expense" => "مصروفات",
                    _ => transType
                };

                var ri = dgCash.Rows.Add(
                    Convert.ToDateTime(r["TransDate"]).ToString("dd/MM/yyyy HH:mm"),
                    transTypeArabic, inAmt > 0 ? inAmt.ToString("N2") : "",
                    outAmt > 0 ? outAmt.ToString("N2") : "",
                    net.ToString("N2"), r["Notes"]);
                if (outAmt > 0) dgCash.Rows[ri].DefaultCellStyle.ForeColor = Color.OrangeRed;
                totIn += inAmt; totOut += outAmt;
            }
            lblCashIn.Text = "إجمالي وارد: " + totIn.ToString("N2") + " ج";
            lblCashOut.Text = "إجمالي صادر: " + totOut.ToString("N2") + " ج";
            lblCashBalance.Text = "رصيد الخزنة: " + AccountDAL.GetCashBalance().ToString("N2") + " ج";
        }

        private void BtnDeposit_Click(object sender, EventArgs e)
        {
            ShowCashActionDialog("توريد نقدية للخزنة", "Deposit");
        }

        private void BtnWithdraw_Click(object sender, EventArgs e)
        {
            ShowCashActionDialog("سحب نقدية من الخزنة", "Withdraw");
        }

        private void BtnReconcile_Click(object sender, EventArgs e)
        {
            decimal currentBalance = AccountDAL.GetCashBalance();
            ShowCashActionDialog($"تسوية رصيد الخزنة (الرصيد الدفتري الحالي: {currentBalance:N2} ج)", "Reconcile");
        }

        private void ShowCashActionDialog(string title, string type)
        {
            decimal currentBalance = AccountDAL.GetCashBalance();
            var dlg = new Form
            {
                Text = title,
                Size = new Size(400, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };

            var lblAmt = new Label { 
                Text = type == "Reconcile" ? "الرصيد الفعلي الحالي في الصندوق:" : "المبلغ:", 
                Location = new Point(30, 20), 
                AutoSize = true, 
                ForeColor = Theme.TextMain 
            };
            var nudAmt = new NumericUpDown 
            { 
                Location = new Point(30, 45), 
                Width = 320, 
                Height = 30, 
                Minimum = 0.00m, 
                Maximum = 9999999, 
                DecimalPlaces = 2,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Value = type == "Reconcile" ? currentBalance : 0.00m
            };

            var lblNotes = new Label { Text = "البيان / السبب:", Location = new Point(30, 90), AutoSize = true, ForeColor = Theme.TextMain };
            var txtNotes = new TextBox 
            { 
                Location = new Point(30, 115), 
                Width = 320, 
                Height = 30,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnSave = Theme.MakeButton("💾 حفظ الحركة", Theme.Accent);
            btnSave.Location = new Point(30, 170);
            btnSave.Size = new Size(320, 38);
            btnSave.Click += (s, ev) =>
            {
                if (type != "Reconcile" && nudAmt.Value <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ أكبر من الصفر");
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                {
                    MessageBox.Show("يرجى كتابة بيان أو سبب الحركة");
                    return;
                }

                decimal amount = nudAmt.Value;
                string notes = txtNotes.Text.Trim();

                if (type == "Deposit")
                {
                    DbHelper.Execute("INSERT INTO CashBox(TransType, AmountIn, Notes, CreatedBy) VALUES('Deposit', @amt, @n, @by)",
                        DbHelper.P("@amt", amount), DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));
                }
                else if (type == "Withdraw")
                {
                    if (amount > currentBalance)
                    {
                        MessageBox.Show($"رصيد الخزنة الحالي ({currentBalance:N2} ج) لا يكفي لسحب مبلغ ({amount:N2} ج)!");
                        return;
                    }

                    DbHelper.Execute("INSERT INTO CashBox(TransType, AmountOut, Notes, CreatedBy) VALUES('Withdraw', @amt, @n, @by)",
                        DbHelper.P("@amt", amount), DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));
                }
                else if (type == "Reconcile")
                {
                    decimal diff = amount - currentBalance;
                    if (diff == 0)
                    {
                        MessageBox.Show("الرصيد الفعلي مطابق تماماً للرصيد الدفتري الحالي. لا توجد فروقات للتسوية!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (diff > 0)
                    {
                        DbHelper.Execute("INSERT INTO CashBox(TransType, AmountIn, Notes, CreatedBy) VALUES('Deposit', @amt, @n, @by)",
                            DbHelper.P("@amt", diff), DbHelper.P("@n", "تسوية خزنة (زيادة) | " + notes), DbHelper.P("@by", Session.EmpID));
                    }
                    else
                    {
                        DbHelper.Execute("INSERT INTO CashBox(TransType, AmountOut, Notes, CreatedBy) VALUES('Withdraw', @amt, @n, @by)",
                            DbHelper.P("@amt", Math.Abs(diff)), DbHelper.P("@n", "تسوية خزنة (عجز) | " + notes), DbHelper.P("@by", Session.EmpID));
                    }
                }

                MessageBox.Show("✅ تم تسجيل الحركة المالية بنجاح!");
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.AddRange(new Control[] { lblAmt, nudAmt, lblNotes, txtNotes, btnSave });
            Theme.ApplyRTL(dlg.Controls);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadCashBox();
            }
        }

        private void LoadExpenses()
        {
            dgExpenses.Rows.Clear();
            int? selectedVehicleID = null;
            string selectedVehicleType = null;
            if (cboExpVehicle.SelectedValue != null && int.TryParse(cboExpVehicle.SelectedValue.ToString(), out int vid) && vid > 0)
                selectedVehicleID = vid;
            else if (cboExpVehicleType.SelectedIndex > 0)
                selectedVehicleType = cboExpVehicleType.SelectedItem?.ToString();

            var dt = AccountDAL.GetExpenses(dtpExpFrom.Value, dtpExpTo.Value, selectedVehicleID, selectedVehicleType);
            foreach (DataRow r in dt.Rows)
            {
                var vehicleLabel = r["VehicleType"] != DBNull.Value && r["VehicleName"] != DBNull.Value
                    ? $"{r["VehicleType"]} - {r["VehicleName"]}"
                    : r["VehicleName"] != DBNull.Value ? r["VehicleName"].ToString() : "";

                dgExpenses.Rows.Add(r["ExpenseID"],
                    r["VehicleID"],
                    Convert.ToDateTime(r["ExpenseDate"]).ToString("dd/MM/yyyy"),
                    r["ExpenseType"], r["SupplierName"], vehicleLabel,
                    Convert.ToDecimal(r["Amount"]).ToString("N2"), r["Notes"]);
            }
        }

        private void DgExpenses_SelectionChanged(object sender, EventArgs e)
        {
            if (dgExpenses.SelectedRows.Count == 0) return;
            var row = dgExpenses.SelectedRows[0];
            _selectedExpID = Convert.ToInt32(row.Cells["ExpenseID"].Value);
            if (DateTime.TryParse(row.Cells["ExpenseDate"].Value?.ToString(), out DateTime d)) dtpExpDate.Value = d;
            cboExpType.Text = row.Cells["ExpenseType"].Value?.ToString();
            if (decimal.TryParse(row.Cells["Amount"].Value?.ToString(), out decimal amt)) nudExpAmount.Value = amt;
            txtExpNotes.Text = row.Cells["Notes"].Value?.ToString();
            // select supplier if present
            var supName = row.Cells["Supplier"].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(supName) && cboExpSupplier.Items.Count > 0)
                cboExpSupplier.SelectedIndex = cboExpSupplier.FindStringExact(supName);
            else cboExpSupplier.SelectedIndex = -1;

            // select vehicle if present
            if (cboExpVehicle.Items.Count > 0)
            {
                int vehicleID = 0;
                if (row.Cells["VehicleID"].Value != null && row.Cells["VehicleID"].Value != DBNull.Value)
                {
                    int.TryParse(row.Cells["VehicleID"].Value.ToString(), out vehicleID);
                }
                if (vehicleID > 0)
                    cboExpVehicle.SelectedValue = vehicleID;
                else
                    cboExpVehicle.SelectedIndex = 0;
            }
        }

        private void ClearExp()
        {
            _selectedExpID = 0;
            dtpExpDate.Value = DateTime.Today;
            cboExpType.Text = "";
            nudExpAmount.Value = 0;
            txtExpNotes.Clear();
            if (cboExpVehicle.Items.Count > 0) cboExpVehicle.SelectedIndex = 0;
        }

        private void BtnSaveExp_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cboExpType.Text)) { MessageBox.Show("اختر نوع المصروف"); return; }
            if (nudExpAmount.Value <= 0) { MessageBox.Show("أدخل مبلغاً أكبر من صفر"); return; }
            int? supplierID = null;
            if (cboExpSupplier.SelectedItem != null && cboExpSupplier.SelectedValue != null && int.TryParse(cboExpSupplier.SelectedValue.ToString(), out int sid) && sid > 0)
                supplierID = sid;
            int? vehicleID = null;
            if (cboExpVehicle.SelectedItem != null && cboExpVehicle.SelectedValue != null && int.TryParse(cboExpVehicle.SelectedValue.ToString(), out int vid) && vid > 0)
                vehicleID = vid;

            try
            {
                int id = AccountDAL.SaveExpense(_selectedExpID, dtpExpDate.Value, cboExpType.Text, nudExpAmount.Value, txtExpNotes.Text, supplierID, vehicleID);
                if (id > 0) { MessageBox.Show("✅ تم الحفظ"); _selectedExpID = id; LoadExpenses(); LoadCashBox(); }
                else MessageBox.Show("❌ فشل الحفظ");
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل حفظ المصروف", ex, "FrmCashBox.BtnSaveExp_Click");
                MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelExp_Click(object sender, EventArgs e)
        {
            if (_selectedExpID == 0) return;
            if (MessageBox.Show("حذف المصروف؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { AccountDAL.DeleteExpense(_selectedExpID); ClearExp(); LoadExpenses(); LoadCashBox(); }
        }

        private void LoadSuppliersToCombo()
        {
            try
            {
                var dt = SupplierDAL.GetAll();
                var emptyRow = dt.NewRow();
                emptyRow["SupplierID"] = DBNull.Value;
                emptyRow["SupplierName"] = "--- لا يوجد ---";
                dt.Rows.InsertAt(emptyRow, 0);
                cboExpSupplier.DataSource = dt;
                cboExpSupplier.DisplayMember = "SupplierName";
                cboExpSupplier.ValueMember = "SupplierID";
                if (_selectedSupplierForExpense > 0)
                {
                    SelectSupplierInExpenseCombo(_selectedSupplierForExpense);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load suppliers failed: " + ex.Message);
            }
        }

        private void LoadVehicleFilters()
        {
            try
            {
                _vehiclesForExpenseFilter = VehicleDAL.GetAll(true);
                cboExpVehicleType.Items.Clear();
                cboExpVehicleType.Items.Add("-- الكل --");
                var types = new System.Collections.Generic.List<string>();
                foreach (DataRow row in _vehiclesForExpenseFilter.Rows)
                {
                    var type = row["VehicleType"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(type) && !types.Contains(type))
                        types.Add(type);
                }
                types.Sort();
                foreach (var type in types)
                    cboExpVehicleType.Items.Add(type);
                cboExpVehicleType.SelectedIndex = 0;

                var dt = _vehiclesForExpenseFilter.Copy();
                var emptyRow = dt.NewRow();
                emptyRow["VehicleID"] = DBNull.Value;
                emptyRow["VehicleType"] = DBNull.Value;
                emptyRow["VehicleName"] = "--- الكل ---";
                dt.Rows.InsertAt(emptyRow, 0);
                cboExpVehicle.DataSource = dt;
                cboExpVehicle.DisplayMember = "VehicleName";
                cboExpVehicle.ValueMember = "VehicleID";
                cboExpVehicle.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load vehicle filters failed: " + ex.Message);
            }
        }

        private void UpdateVehicleFilter()
        {
            if (_vehiclesForExpenseFilter == null) return;
            string selectedType = cboExpVehicleType.SelectedItem?.ToString();
            var dt = _vehiclesForExpenseFilter.Clone();
            foreach (DataRow row in _vehiclesForExpenseFilter.Rows)
            {
                if (string.IsNullOrWhiteSpace(selectedType) || selectedType == "-- الكل --" || row["VehicleType"]?.ToString() == selectedType)
                    dt.ImportRow(row);
            }
            var emptyRow = dt.NewRow();
            emptyRow["VehicleID"] = DBNull.Value;
            emptyRow["VehicleType"] = DBNull.Value;
            emptyRow["VehicleName"] = "--- الكل ---";
            dt.Rows.InsertAt(emptyRow, 0);
            cboExpVehicle.DataSource = dt;
            cboExpVehicle.SelectedIndex = 0;
        }

        private void SelectSupplierInExpenseCombo(int supplierID)
        {
            if (supplierID <= 0 || cboExpSupplier.Items.Count == 0) return;
            for (int i = 0; i < cboExpSupplier.Items.Count; i++)
            {
                if (cboExpSupplier.Items[i] is DataRowView drv && drv["SupplierID"] != DBNull.Value)
                {
                    if (Convert.ToInt32(drv["SupplierID"]) == supplierID)
                    {
                        cboExpSupplier.SelectedIndex = i;
                        return;
                    }
                }
            }
        }
    }
}

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
        private ComboBox cboSafeFilter;
        private ComboBox cboTransTypeFilter;
        private Button btnLoadCash;
        private Label lblCashBalance, lblCashIn, lblCashOut;

        // Expenses tab
        private DataGridView dgExpenses;
        private DateTimePicker dtpExpFrom, dtpExpTo;
        private Button btnLoadExp, btnNewExp, btnSaveExp, btnDelExp;
        private ComboBox cboExpType;
        private ComboBox cboExpVehicleType;
        private ComboBox cboExpVehicle;
        private ComboBox cboExpVehicleFilter;
        private ComboBox cboExpSafeAccount;
        private TextBox txtExpNotes;
        private TextBox txtExpAmount;
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
            tabMain.SelectedTab = tabCash;
        }

        private void InitUI()
        {
            this.Text = "الخزنة والمصروفات التشغيلية";
            this.Size = new Size(1020, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            tabMain = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontMain };
            tabCash = new TabPage("حركات الخزنة") { BackColor = Theme.BgMain };
            tabExpenses = new TabPage("المصروفات التشغيلية") { BackColor = Theme.BgMain };
            tabMain.TabPages.AddRange(new[] { tabCash, tabExpenses });
            Theme.StyleTabControl(tabMain);
            this.Controls.Add(tabMain);

            BuildCashTab();
            BuildExpensesTab();
            
            LoadSafesCombos();
            LoadExpenseTypes();
            LoadVehicleFilters();

            Theme.ApplyFormRTL(this);
        }

        private void BuildCashTab()
        {
            var pnlF = new FlowLayoutPanel 
            { 
                Name = "pnlF",
                Dock = DockStyle.Top, 
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgSearchPanel, 
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true
            };
            
            pnlF.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            dtpCashFrom = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0), Margin = new Padding(10, 4, 0, 0) };
            dtpCashFrom.ValueChanged += (s, e) => LoadCashBox();
            pnlF.Controls.Add(dtpCashFrom);
            
            pnlF.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0) });
            dtpCashTo = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Now, Margin = new Padding(10, 4, 0, 0) };
            dtpCashTo.ValueChanged += (s, e) => LoadCashBox();
            pnlF.Controls.Add(dtpCashTo);

            pnlF.Controls.Add(new Label { Text = "الحساب:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0) });
            cboSafeFilter = new ComboBox
            {
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(10, 4, 0, 0)
            };
            cboSafeFilter.SelectedIndexChanged += (s, e) => LoadCashBox();
            pnlF.Controls.Add(cboSafeFilter);

            pnlF.Controls.Add(new Label { Text = "نوع الحركة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0) });
            cboTransTypeFilter = new ComboBox
            {
                Width = 115,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(10, 4, 0, 0)
            };
            cboTransTypeFilter.Items.AddRange(new object[] { "الكل", "وارد (توريد)", "صادر (صرف)" });
            cboTransTypeFilter.SelectedIndex = 0;
            cboTransTypeFilter.SelectedIndexChanged += (s, e) => LoadCashBox();
            pnlF.Controls.Add(cboTransTypeFilter);
            
            btnLoadCash = Theme.MakeButton("عرض", Theme.Accent);
            btnLoadCash.Size = new Size(70, 32);
            btnLoadCash.Margin = new Padding(15, 0, 0, 0);
            btnLoadCash.Click += (s, e) => LoadCashBox();
            pnlF.Controls.Add(btnLoadCash);

            var btnDeposit = Theme.MakeButton("➕ توريد نقدي", Color.FromArgb(40, 130, 80));
            btnDeposit.Size = new Size(110, 32);
            btnDeposit.Margin = new Padding(10, 0, 0, 0);
            btnDeposit.Click += BtnDeposit_Click;
            pnlF.Controls.Add(btnDeposit);

            var btnWithdraw = Theme.MakeButton("➖ صرف نقدي", Color.FromArgb(170, 70, 70));
            btnWithdraw.Size = new Size(110, 32);
            btnWithdraw.Margin = new Padding(10, 0, 0, 0);
            btnWithdraw.Click += BtnWithdraw_Click;
            pnlF.Controls.Add(btnWithdraw);

            var btnReconcile = Theme.MakeButton("⚖️ تسوية", Color.FromArgb(120, 90, 40));
            btnReconcile.Size = new Size(95, 32);
            btnReconcile.Margin = new Padding(10, 0, 0, 0);
            btnReconcile.Click += BtnReconcile_Click;
            pnlF.Controls.Add(btnReconcile);

            var btnManageAccounts = Theme.MakeButton("💳 الحسابات", Color.FromArgb(70, 70, 150));
            btnManageAccounts.Size = new Size(95, 32);
            btnManageAccounts.Margin = new Padding(10, 0, 0, 0);
            btnManageAccounts.Visible = Session.IsAdmin || (Session.CanAccess("SafeAccounts") && (Session.CanAdd("SafeAccounts") || Session.CanEdit("SafeAccounts")));
            btnManageAccounts.Click += (s, e) =>
            {
                if (!Session.IsAdmin && !Session.CanAccess("SafeAccounts"))
                {
                    MessageBox.Show("⛔ غير مصرح لك بالدخول على إدارة الحسابات والخزائن.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                new FrmSafeAccounts().ShowDialog();
                LoadSafesCombos();
                LoadCashBox();
            };
            pnlF.Controls.Add(btnManageAccounts);

            var btnTransfer = Theme.MakeButton("🔄 تحويل", Color.FromArgb(100, 70, 150));
            btnTransfer.Size = new Size(95, 32);
            btnTransfer.Margin = new Padding(10, 0, 0, 0);
            btnTransfer.Click += BtnTransfer_Click;
            pnlF.Controls.Add(btnTransfer);

            var btnOpenVouchers = Theme.MakeButton("📄 شاشة السندات المستقلة", Color.FromArgb(40, 120, 180));
            btnOpenVouchers.Size = new Size(160, 32);
            btnOpenVouchers.Margin = new Padding(10, 0, 0, 0);
            btnOpenVouchers.Click += (s, e) =>
            {
                new FrmReceiptVoucher().ShowDialog();
                LoadCashBox();
            };
            pnlF.Controls.Add(btnOpenVouchers);

            var btnPrintVoucher = Theme.MakeButton("🖨️ طباعة كشف الحركة", Color.FromArgb(16, 185, 129));
            btnPrintVoucher.Size = new Size(130, 32);
            btnPrintVoucher.Margin = new Padding(10, 0, 0, 0);
            btnPrintVoucher.Click += (s, e) =>
            {
                int? accId = null;
                if (cboSafeFilter != null && cboSafeFilter.SelectedItem is ComboItem item && item.ID > 0)
                {
                    accId = item.ID;
                }
                var dtTrans = AccountDAL.GetCashBox(dtpCashFrom.Value, dtpCashTo.Value, accId);
                string safeName = cboSafeFilter != null ? cboSafeFilter.Text : "الخزينة الرئيسية";
                new FrmPrintPayment(dtTrans, safeName, 0, "AlTarekVoucher", true);
            };
            pnlF.Controls.Add(btnPrintVoucher);

            dgCash = MakeGrid();
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ", FillWeight = 50 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "النوع" });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "AmountIn", HeaderText = "وارد", FillWeight = 45 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "AmountOut", HeaderText = "صادر", FillWeight = 45 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "صافي", FillWeight = 45 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "RunningBalance", HeaderText = "الرصيد بعد الحركة", FillWeight = 55 });
            dgCash.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان" });

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGrid.Controls.Add(dgCash);

            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(8) };
            lblCashBalance = new Label { Text = "رصيد الخزنة: ---", ForeColor = Theme.Accent, Location = new Point(10, 15), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            lblCashIn = new Label { Text = "إجمالي وارد: 0", ForeColor = Color.LightGreen, Location = new Point(280, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblCashOut = new Label { Text = "إجمالي صادر: 0", ForeColor = Color.OrangeRed, Location = new Point(480, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
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
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgCard, 
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true
            };
            
            pnlF.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            dtpExpFrom = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0), Margin = new Padding(10, 4, 0, 0) };
            dtpExpFrom.ValueChanged += (s, e) => LoadExpenses();
            pnlF.Controls.Add(dtpExpFrom);
            
            pnlF.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0) });
            dtpExpTo = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Now, Margin = new Padding(10, 4, 0, 0) };
            dtpExpTo.ValueChanged += (s, e) => LoadExpenses();
            pnlF.Controls.Add(dtpExpTo);

            pnlF.Controls.Add(new Label { Text = "نوع العربية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0) });
            cboExpVehicleType = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(10, 4, 0, 0)
            };
            cboExpVehicleType.SelectedIndexChanged += (s, e) => UpdateVehicleFilter();
            pnlF.Controls.Add(cboExpVehicleType);

            pnlF.Controls.Add(new Label { Text = "اسم العربية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0) });
            cboExpVehicleFilter = new ComboBox
            {
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(10, 4, 0, 0)
            };
            pnlF.Controls.Add(cboExpVehicleFilter);
            
            btnLoadExp = Theme.MakeButton("عرض", Theme.Accent);
            btnLoadExp.Size = new Size(70, 32);
            btnLoadExp.Margin = new Padding(20, 0, 0, 0);
            btnLoadExp.Click += (s, e) => LoadExpenses();
            pnlF.Controls.Add(btnLoadExp);

            dgExpenses = MakeGrid();
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpenseID", Visible = false });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "VehicleID", Visible = false });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "SafeAccountID", Visible = false });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpenseDate", HeaderText = "التاريخ", FillWeight = 35 });
            dgExpenses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpenseType", HeaderText = "النوع", FillWeight = 25 });
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
            for (int i = 0; i < 6; i++) tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

            // Row 0: Date
            var lblDate = new Label { Text = "التاريخ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            dtpExpDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 5, 0, 5) };
            tblFields.Controls.Add(lblDate, 0, 0);
            tblFields.Controls.Add(dtpExpDate, 1, 0);

            // Row 1: Type
            var lblType = new Label { Text = "نوع المصروف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            
            var pnlTypeContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            pnlTypeContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlTypeContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35f));
            
            cboExpType = new ComboBox 
            { 
                Dock = DockStyle.Fill, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 5)
            };
            
            var btnAddExpType = Theme.MakeButton("➕", 0, 0, 30, 32, Color.FromArgb(70, 70, 70));
            btnAddExpType.Dock = DockStyle.Fill;
            btnAddExpType.Margin = new Padding(3, 4, 0, 4);
            btnAddExpType.Click += (s, e) => 
            {
                new FrmLookupManager("ExpenseTypes", "ExpenseTypeID", "ExpenseTypeCode", "ExpenseTypeName", "EXP", "بنود المصروفات").ShowDialog();
                LoadExpenseTypes();
            };
            
            pnlTypeContainer.Controls.Add(cboExpType, 0, 0);
            pnlTypeContainer.Controls.Add(btnAddExpType, 1, 0);
            
            tblFields.Controls.Add(lblType, 0, 1);
            tblFields.Controls.Add(pnlTypeContainer, 1, 1);

            // Row 2: Amount
            var lblAmountVal = new Label { Text = "المبلغ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            txtExpAmount = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Theme.BgInput, 
                ForeColor = Color.FromArgb(250, 204, 21),
                Margin = new Padding(0, 5, 0, 5),
                TextAlign = HorizontalAlignment.Center
            };
            txtExpAmount.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                    e.Handled = true;
                if (e.KeyChar == '.' && (s as TextBox).Text.IndexOf('.') > -1)
                    e.Handled = true;
            };
            txtExpAmount.Enter += (s, e) => txtExpAmount.SelectAll();
            tblFields.Controls.Add(lblAmountVal, 0, 2);
            tblFields.Controls.Add(txtExpAmount, 1, 2);

            // Row 3: Vehicle (optional)
            var lblVehicle = new Label { Text = "العربية / المركبة (اختياري):", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cboExpVehicle = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Margin = new Padding(0, 5, 0, 5) };
            tblFields.Controls.Add(lblVehicle, 0, 3);
            tblFields.Controls.Add(cboExpVehicle, 1, 3);

            // Row 4: Notes
            var lblNotesVal = new Label { Text = "البيان:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            txtExpNotes = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                Margin = new Padding(0, 5, 0, 5),
                BorderStyle = BorderStyle.FixedSingle
            };
            tblFields.Controls.Add(lblNotesVal, 0, 4);
            tblFields.Controls.Add(txtExpNotes, 1, 4);

            // Row 5: Source Safe Account for Expense
            var lblExpSafe = new Label { Text = "حساب الدفع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cboExpSafeAccount = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 5)
            };
            tblFields.Controls.Add(lblExpSafe, 0, 5);
            tblFields.Controls.Add(cboExpSafeAccount, 1, 5);

            pnlDetails.Controls.Add(tblFields);

            // Bottom Actions (Absolute positioning inside pnlDetails)
            btnNewExp = Theme.MakeButton("🆕 جديد", 20, 390, 85, 38, Color.FromArgb(60, 100, 60));
            btnNewExp.Click += (s, e) => ClearExp();

            btnSaveExp = Theme.MakeButton("💾 حفظ المصروف", 115, 390, 115, 38, Theme.Accent);
            btnSaveExp.Click += BtnSaveExp_Click;

            btnDelExp = Theme.MakeButton("🗑 حذف", 240, 390, 80, 38, Color.FromArgb(140, 40, 40));
            btnDelExp.Click += BtnDelExp_Click;

            pnlDetails.Controls.AddRange(new Control[] { btnNewExp, btnSaveExp, btnDelExp });
            
            btnNewExp.BringToFront();
            btnSaveExp.BringToFront();
            btnDelExp.BringToFront();

            tbl.Controls.Add(pnlDetails, 0, 0); // Right
            tbl.Controls.Add(pnlList, 1, 0);    // Left

            tbl.BringToFront();
            tabExpenses.Controls.Add(tbl);
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

        private void LoadSafesCombos()
        {
            try
            {
                int selectedFilterID = 0;
                if (cboSafeFilter != null && cboSafeFilter.SelectedItem is ComboItem fItem) selectedFilterID = fItem.ID;

                DataTable safes = AccountDAL.GetAllowedSafeAccounts();
                
                // For Cash Filter Combo
                if (cboSafeFilter != null)
                {
                    cboSafeFilter.Items.Clear();
                    if (Session.IsAdmin || (Session.CanChangeSafe("CashBox") && safes.Rows.Count > 1))
                    {
                        cboSafeFilter.Items.Add(new ComboItem(0, "-- الكل --"));
                    }
                }

                // For Expense Entry Combo
                int selectedExpAccID = 0;
                if (cboExpSafeAccount != null && cboExpSafeAccount.SelectedItem is ComboItem eItem) selectedExpAccID = eItem.ID;
                if (cboExpSafeAccount != null) cboExpSafeAccount.Items.Clear();

                foreach (DataRow row in safes.Rows)
                {
                    int id = Convert.ToInt32(row["AccountID"]);
                    string name = row["AccountName"].ToString();
                    ComboItem item1 = new ComboItem(id, name);
                    ComboItem item2 = new ComboItem(id, name);

                    if (cboSafeFilter != null) cboSafeFilter.Items.Add(item1);
                    if (cboExpSafeAccount != null) cboExpSafeAccount.Items.Add(item2);
                }

                int defaultSafeID = Session.GetPrimaryAllowedSafeID();

                if (cboSafeFilter != null)
                {
                    cboSafeFilter.DisplayMember = "Text";
                    int selectIdx = 0;
                    for (int i = 0; i < cboSafeFilter.Items.Count; i++)
                    {
                        if (cboSafeFilter.Items[i] is ComboItem item && (item.ID == selectedFilterID || (selectedFilterID == 0 && item.ID == defaultSafeID)))
                        {
                            selectIdx = i;
                            break;
                        }
                    }
                    if (cboSafeFilter.Items.Count > 0) cboSafeFilter.SelectedIndex = selectIdx;

                    if (!Session.IsAdmin && (!Session.CanChangeSafe("CashBox") || safes.Rows.Count <= 1))
                    {
                        cboSafeFilter.Enabled = false;
                    }
                }

                if (cboExpSafeAccount != null)
                {
                    cboExpSafeAccount.DisplayMember = "Text";
                    int selectExpIdx = 0;
                    for (int i = 0; i < cboExpSafeAccount.Items.Count; i++)
                    {
                        if (cboExpSafeAccount.Items[i] is ComboItem item && (item.ID == selectedExpAccID || (selectedExpAccID == 0 && item.ID == defaultSafeID)))
                        {
                            selectExpIdx = i;
                            break;
                        }
                    }
                    if (cboExpSafeAccount.Items.Count > 0) cboExpSafeAccount.SelectedIndex = selectExpIdx;

                    if (!Session.IsAdmin && (!Session.CanChangeSafe("CashBox") || safes.Rows.Count <= 1))
                    {
                        cboExpSafeAccount.Enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load safes combos failed: " + ex.Message);
            }
        }

        private void LoadCashBox()
        {
            dgCash.Rows.Clear();
            int? selectedAccountID = null;
            if (cboSafeFilter != null && cboSafeFilter.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
            {
                selectedAccountID = safeItem.ID;
            }

            if (!Session.IsAdmin)
            {
                var allowed = Session.GetAllowedSafeIDSet();
                if (selectedAccountID == null || selectedAccountID == 0 || (allowed != null && !allowed.Contains(selectedAccountID.Value)))
                {
                    selectedAccountID = Session.GetPrimaryAllowedSafeID();
                }
            }

            var dt = AccountDAL.GetCashBox(dtpCashFrom.Value, dtpCashTo.Value, selectedAccountID);
            bool canViewBalance = Session.CanViewBalance("CashBox");

            // ── حساب الرصيد الافتتاحي المسبق قبل تاريخ البداية لحساب الرصيد التراكمي بعد كل حركة ──
            decimal runningBalance = 0;
            if (selectedAccountID.HasValue && selectedAccountID.Value > 0)
            {
                var openingObj = DbHelper.Scalar(
                    "SELECT OpeningBalance FROM SafeAccounts WHERE AccountID = @accId",
                    DbHelper.P("@accId", selectedAccountID.Value));
                runningBalance = openingObj != DBNull.Value && openingObj != null ? Convert.ToDecimal(openingObj) : 0m;

                var prevTransObj = DbHelper.Scalar(
                    "SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox WHERE AccountID = @accId AND TransDate < @fromDate",
                    DbHelper.P("@accId", selectedAccountID.Value),
                    DbHelper.P("@fromDate", dtpCashFrom.Value.Date));
                runningBalance += prevTransObj != DBNull.Value && prevTransObj != null ? Convert.ToDecimal(prevTransObj) : 0m;
            }
            else
            {
                var openingObj = DbHelper.Scalar("SELECT SUM(OpeningBalance) FROM SafeAccounts");
                runningBalance = openingObj != DBNull.Value && openingObj != null ? Convert.ToDecimal(openingObj) : 0m;

                var prevTransObj = DbHelper.Scalar(
                    "SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox WHERE TransDate < @fromDate",
                    DbHelper.P("@fromDate", dtpCashFrom.Value.Date));
                runningBalance += prevTransObj != DBNull.Value && prevTransObj != null ? Convert.ToDecimal(prevTransObj) : 0m;
            }

            decimal totIn = 0, totOut = 0;
            foreach (DataRow r in dt.Rows)
            {
                decimal inAmt = Convert.ToDecimal(r["AmountIn"]);
                decimal outAmt = Convert.ToDecimal(r["AmountOut"]);

                runningBalance += (inAmt - outAmt);

                if (cboTransTypeFilter != null)
                {
                    if (cboTransTypeFilter.SelectedIndex == 1 && inAmt == 0) continue; // وارد (توريد) فقط
                    if (cboTransTypeFilter.SelectedIndex == 2 && outAmt == 0) continue; // صادر (صرف) فقط
                }

                decimal net = inAmt - outAmt;
                
                string notes = r["Notes"].ToString();
                string transType = r["TransType"].ToString();
                string transTypeArabic = transType switch
                {
                    "Deposit" => "توريد نقدي",
                    "Withdraw" => "صرف نقدي",
                    "SaleIncome" => "بيع نقدي",
                    "ClientPayment" => "تحصيل من عميل",
                    "ReservationDeposit" => "عربون حجز صنف",
                    "Expense" => "مصروفات",
                    "Transfer" => notes.Contains("تقفيل وردية") ? "تحويل تقفيل وردية" : "تحويل بين الحسابات",
                    "ShiftCloseOut" => "إغلاق وردية (تحويل صادر)",
                    "ShiftCloseIn" => "إغلاق وردية (استلام وارد)",
                    "ShiftClose" => "تقفيل وردية",
                    "ShiftOpen" => "فتح وردية جديدة",
                    "ShiftDeficit" => "سند تسوية عجز وردية",
                    "ShiftSurplus" => "سند تسوية زيادة وردية",
                    _ => transType
                };

                string accName = r.Table.Columns.Contains("AccountName") && r["AccountName"] != DBNull.Value ? $" [{r["AccountName"]}]" : "";
                string runningBalanceText = canViewBalance ? runningBalance.ToString("N2") : "*** 🔒";

                var ri = dgCash.Rows.Add(
                    Convert.ToDateTime(r["TransDate"]).ToString("dd/MM/yyyy HH:mm"),
                    transTypeArabic, 
                    inAmt > 0 ? inAmt.ToString("N2") : "",
                    outAmt > 0 ? outAmt.ToString("N2") : "",
                    net.ToString("N2"), 
                    runningBalanceText,
                    notes + accName);

                if (outAmt > 0) dgCash.Rows[ri].DefaultCellStyle.ForeColor = Color.FromArgb(185, 25, 25);
                else if (inAmt > 0) dgCash.Rows[ri].DefaultCellStyle.ForeColor = Color.FromArgb(15, 125, 45);
                totIn += inAmt; totOut += outAmt;
            }
            if (canViewBalance)
            {
                lblCashIn.Text = "إجمالي وارد: " + totIn.ToString("N2") + " ج";
                lblCashOut.Text = "إجمالي صادر: " + totOut.ToString("N2") + " ج";
                string balanceLabel = selectedAccountID == null ? "رصيد كافة الحسابات: " : "رصيد الحساب المختار: ";
                lblCashBalance.Text = balanceLabel + AccountDAL.GetCashBalance(selectedAccountID, dtpCashTo.Value).ToString("N2") + " ج";
            }
            else
            {
                lblCashIn.Text = "إجمالي وارد: *** 🔒";
                lblCashOut.Text = "إجمالي صادر: *** 🔒";
                lblCashBalance.Text = "رصيد الخزنة/الدرج: *** 🔒 (محجوب)";
            }
        }

        private void BtnDeposit_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("CashBox")) { MessageBox.Show("⛔ ليس لديك صلاحية التوريد النقدي.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            ShowCashActionDialog("توريد نقدي للحساب", "Deposit");
        }

        private void BtnWithdraw_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("CashBox")) { MessageBox.Show("⛔ ليس لديك صلاحية الصرف النقدي.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            ShowCashActionDialog("صرف نقدي من الحساب", "Withdraw");
        }

        private void BtnReconcile_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("CashBox")) { MessageBox.Show("⛔ ليس لديك صلاحية إجراء التسوية.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int? selectedAccountID = null;
            if (cboSafeFilter != null && cboSafeFilter.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
            {
                selectedAccountID = safeItem.ID;
            }
            decimal currentBalance = AccountDAL.GetCashBalance(selectedAccountID);
            ShowCashActionDialog($"تسوية رصيد الحساب (الرصيد الدفتري الحالي: {currentBalance:N2} ج)", "Reconcile");
        }

        private void BtnTransfer_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("CashBox")) { MessageBox.Show("⛔ ليس لديك صلاحية تحويل النقدية بين الحسابات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!Session.IsAdmin && !Session.CanChangeSafe("CashBox"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية تحويل النقدية بين الخزن والأدراج.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dlg = new Form
            {
                Text = "🔄 تحويل نقدية بين الحسابات والخزن",
                Size = new Size(420, 470),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };

            // بطاقة معاينة الأرصدة
            var pnlBalPreview = new Panel
            {
                Location = new Point(25, 12),
                Size = new Size(355, 80),
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(8)
            };
            var lblSrcBalTitle = new Label { Text = "المصدر بعد التحويل:", Location = new Point(210, 10), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var lblSrcBalVal = new Label { Text = "0.00 ج", Location = new Point(10, 8), Width = 195, ForeColor = Color.FromArgb(248, 113, 113), Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            var lblDestBalTitle = new Label { Text = "المستهدف بعد التحويل:", Location = new Point(200, 45), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var lblDestBalVal = new Label { Text = "0.00 ج", Location = new Point(10, 43), Width = 185, ForeColor = Color.FromArgb(74, 222, 128), Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            pnlBalPreview.Controls.AddRange(new Control[] { lblSrcBalTitle, lblSrcBalVal, lblDestBalTitle, lblDestBalVal });
            dlg.Controls.Add(pnlBalPreview);

            int currentY = 100;
            var lblSource = new Label { Text = "الحساب المصدر (من):", Location = new Point(25, currentY), AutoSize = true, ForeColor = Theme.TextMain };
            var cboSource = new ComboBox
            {
                Location = new Point(25, currentY + 22),
                Width = 355,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };

            currentY += 55;
            var lblDest = new Label { Text = "الحساب المستهدف (إلى):", Location = new Point(25, currentY), AutoSize = true, ForeColor = Theme.TextMain };
            var cboDest = new ComboBox
            {
                Location = new Point(25, currentY + 22),
                Width = 355,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };

            // Load safes
            try
            {
                DataTable srcSafes = AccountDAL.GetAllowedSafeAccounts();
                DataTable allSafes = AccountDAL.GetActiveSafeAccounts();
                foreach (DataRow row in srcSafes.Rows)
                {
                    ComboItem item1 = new ComboItem(Convert.ToInt32(row["AccountID"]), row["AccountName"].ToString());
                    cboSource.Items.Add(item1);
                }
                foreach (DataRow row in allSafes.Rows)
                {
                    ComboItem item2 = new ComboItem(Convert.ToInt32(row["AccountID"]), row["AccountName"].ToString());
                    cboDest.Items.Add(item2);
                }
                cboSource.DisplayMember = "Text";
                cboDest.DisplayMember = "Text";
                if (cboSource.Items.Count > 0) cboSource.SelectedIndex = 0;
                if (cboDest.Items.Count > 1) cboDest.SelectedIndex = 1;
                else if (cboDest.Items.Count > 0) cboDest.SelectedIndex = 0;

                if (!Session.IsAdmin && (!Session.CanChangeSafe("CashBox") || srcSafes.Rows.Count <= 1))
                {
                    cboSource.Enabled = false;
                }
            }
            catch { }

            currentY += 55;
            var lblAmt = new Label { Text = "المبلغ المراد تحويله (ج):", Location = new Point(25, currentY), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            var txtAmount = new TextBox
            {
                Location = new Point(25, currentY + 22),
                Width = 355,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Color.FromArgb(250, 204, 21),
                TextAlign = HorizontalAlignment.Center
            };
            txtAmount.KeyPress += (s, e2) =>
            {
                if (!char.IsControl(e2.KeyChar) && !char.IsDigit(e2.KeyChar) && e2.KeyChar != '.')
                    e2.Handled = true;
                if (e2.KeyChar == '.' && (s as TextBox).Text.IndexOf('.') > -1)
                    e2.Handled = true;
            };
            txtAmount.Enter += (s, e2) => txtAmount.SelectAll();

            void UpdateTransferPreview()
            {
                decimal amt = 0m;
                decimal.TryParse(txtAmount.Text.Trim(), out amt);
                if (cboSource.SelectedItem is ComboItem sItem && cboDest.SelectedItem is ComboItem dItem)
                {
                    decimal sBal = AccountDAL.GetCashBalance(sItem.ID);
                    decimal dBal = AccountDAL.GetCashBalance(dItem.ID);
                    lblSrcBalVal.Text = $"{sBal - amt:N2} ج  (الحالي: {sBal:N2} ج)";
                    lblDestBalVal.Text = $"{dBal + amt:N2} ج  (الحالي: {dBal:N2} ج)";
                }
            }

            cboSource.SelectedIndexChanged += (s, ev) => UpdateTransferPreview();
            cboDest.SelectedIndexChanged += (s, ev) => UpdateTransferPreview();
            txtAmount.TextChanged += (s, ev) => UpdateTransferPreview();

            currentY += 55;
            var lblNotes = new Label { Text = "ملاحظات:", Location = new Point(25, currentY), AutoSize = true, ForeColor = Theme.TextMain };
            var txtNotes = new TextBox
            {
                Location = new Point(25, currentY + 22),
                Width = 355,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                Text = "تحويل نقدية بين الخزن"
            };

            currentY += 60;
            var btnSave = Theme.MakeButton("✅ إتمـام التحويل", Theme.Accent);
            btnSave.Location = new Point(25, currentY);
            btnSave.Size = new Size(355, 38);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);

            btnSave.Click += (s, ev) =>
            {
                if (!(cboSource.SelectedItem is ComboItem srcItem) || !(cboDest.SelectedItem is ComboItem destItem))
                {
                    MessageBox.Show("يرجى اختيار الحسابات");
                    return;
                }
                if (srcItem.ID == destItem.ID)
                {
                    MessageBox.Show("لا يمكن التحويل لنفس الحساب المختار كأصل!");
                    return;
                }
                if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amt) || amt <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر");
                    txtAmount.Focus();
                    return;
                }

                try
                {
                    AccountDAL.TransferFunds(srcItem.ID, destItem.ID, amt, txtNotes.Text.Trim());
                    MessageBox.Show("✅ تم التحويل بنجاح!");
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل التحويل: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { lblSource, cboSource, lblDest, cboDest, lblAmt, txtAmount, lblNotes, txtNotes, btnSave });
            Theme.ApplyRTL(dlg.Controls);

            dlg.Shown += (s, ev) => { UpdateTransferPreview(); txtAmount.Focus(); };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadCashBox();
            }
        }

        private void ShowCashActionDialog(string title, string type)
        {
            var dlg = new Form
            {
                Text = title,
                Size = new Size(420, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };

            // بطاقة معاينة الأرصدة
            var pnlBalPreview = new Panel
            {
                Location = new Point(25, 12),
                Size = new Size(355, 75),
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(8)
            };
            var lblCurTitle = new Label { Text = "الرصيد الحالي للحساب:", Location = new Point(215, 10), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var lblCurVal = new Label { Text = "0.00 ج", Location = new Point(10, 8), Width = 200, ForeColor = Color.White, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            var lblNewTitle = new Label { Text = "الرصيد بعد الحركة:", Location = new Point(230, 42), AutoSize = true, ForeColor = Color.FromArgb(250, 204, 21), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            var lblNewVal = new Label { Text = "0.00 ج", Location = new Point(10, 39), Width = 215, ForeColor = Color.FromArgb(250, 204, 21), Font = new Font("Segoe UI", 12f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            pnlBalPreview.Controls.AddRange(new Control[] { lblCurTitle, lblCurVal, lblNewTitle, lblNewVal });
            dlg.Controls.Add(pnlBalPreview);

            int currentY = 95;
            var cboSafe = new ComboBox
            {
                Width = 355,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };

            // Load safes
            try
            {
                DataTable safes = AccountDAL.GetAllowedSafeAccounts();
                foreach (DataRow row in safes.Rows)
                {
                    cboSafe.Items.Add(new ComboItem(
                        Convert.ToInt32(row["AccountID"]),
                        row["AccountName"].ToString()
                    ));
                }
                cboSafe.DisplayMember = "Text";
                
                // Pre-select current filter or default safe
                int preselectedID = Session.GetPrimaryAllowedSafeID();
                if (cboSafeFilter != null && cboSafeFilter.SelectedItem is ComboItem filterItem && filterItem.ID > 0)
                {
                    preselectedID = filterItem.ID;
                }
                
                cboSafe.SelectedIndex = 0;
                for (int i = 0; i < cboSafe.Items.Count; i++)
                {
                    if (cboSafe.Items[i] is ComboItem item && item.ID == preselectedID)
                    {
                        cboSafe.SelectedIndex = i;
                        break;
                    }
                }

                if (!Session.IsAdmin && (!Session.CanChangeSafe("CashBox") || safes.Rows.Count <= 1))
                {
                    cboSafe.Enabled = false;
                }
            }
            catch { }

            var txtAmount = new TextBox 
            { 
                Width = 355, 
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Color.FromArgb(250, 204, 21),
                TextAlign = HorizontalAlignment.Center
            };
            txtAmount.KeyPress += (s, e2) =>
            {
                if (!char.IsControl(e2.KeyChar) && !char.IsDigit(e2.KeyChar) && e2.KeyChar != '.')
                    e2.Handled = true;
                if (e2.KeyChar == '.' && (s as TextBox).Text.IndexOf('.') > -1)
                    e2.Handled = true;
            };
            txtAmount.Enter += (s, e2) => txtAmount.SelectAll();

            void UpdateActionPreview()
            {
                if (cboSafe.SelectedItem is ComboItem safeItem)
                {
                    decimal cur = AccountDAL.GetCashBalance(safeItem.ID);
                    lblCurVal.Text = cur.ToString("N2") + " ج";

                    decimal amt = 0m;
                    decimal.TryParse(txtAmount.Text.Trim(), out amt);

                    decimal newBal = cur;
                    if (type == "Deposit") newBal = cur + amt;
                    else if (type == "Withdraw") newBal = cur - amt;
                    else if (type == "Reconcile") newBal = amt;

                    lblNewVal.Text = newBal.ToString("N2") + " ج";
                    lblNewVal.ForeColor = newBal < 0 ? Color.FromArgb(248, 113, 113) : (newBal > 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(250, 204, 21));
                }
            }

            // Set initial balance for Reconcile
            if (type == "Reconcile" && cboSafe.SelectedItem is ComboItem initialSafe)
            {
                txtAmount.Text = AccountDAL.GetCashBalance(initialSafe.ID).ToString("N2");
            }

            cboSafe.SelectedIndexChanged += (s, ev) =>
            {
                if (type == "Reconcile" && cboSafe.SelectedItem is ComboItem selectedSafe)
                {
                    txtAmount.Text = AccountDAL.GetCashBalance(selectedSafe.ID).ToString("N2");
                }
                UpdateActionPreview();
            };
            txtAmount.TextChanged += (s, ev) => UpdateActionPreview();

            var txtNotes = new TextBox 
            { 
                Width = 355, 
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };

            var btnSave = Theme.MakeButton("💾 حفظ الحركة المالية", Theme.Accent);
            btnSave.Size = new Size(355, 38);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);

            // Lay out controls dynamically
            var lblSafe = new Label { Text = "الحساب المالي المعني:", Location = new Point(25, currentY), AutoSize = true, ForeColor = Theme.TextMain };
            cboSafe.Location = new Point(25, currentY + 22);
            dlg.Controls.Add(lblSafe);
            dlg.Controls.Add(cboSafe);
            
            currentY += 55;
            
            Label lblClassification = null;
            ComboBox cboClassification = null;
            if (type == "Deposit" || type == "Withdraw")
            {
                lblClassification = new Label { Text = "التصنيف المحاسبي للمقابلة:", Location = new Point(25, currentY), AutoSize = true, ForeColor = Theme.TextMain };
                cboClassification = new ComboBox
                {
                    Location = new Point(25, currentY + 22),
                    Width = 355,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Theme.BgInput,
                    ForeColor = Theme.TextMain,
                    FlatStyle = FlatStyle.Flat
                };
                
                if (type == "Deposit")
                {
                    cboClassification.Items.Add(new ComboItem(-1, "تسوية نقدية / رصيد افتتاحي (Reconciliation)"));
                    cboClassification.Items.Add(new ComboItem(1, "تمويل زيادة رأس المال (Capital)"));
                    cboClassification.Items.Add(new ComboItem(2, "قروض مستلمة (ShortTermLoans)"));
                    cboClassification.Items.Add(new ComboItem(3, "إيرادات أخرى متنوعة (OtherRevenues)"));
                }
                else // Withdraw
                {
                    cboClassification.Items.Add(new ComboItem(-1, "تسوية نقدية / عجز جرد (Reconciliation)"));
                    cboClassification.Items.Add(new ComboItem(1, "مسحوبات شخصية للشركاء (Drawings)"));
                    cboClassification.Items.Add(new ComboItem(2, "عهود وسلف الموظفين (CustodiesAdvances)"));
                    cboClassification.Items.Add(new ComboItem(3, "سداد قروض مستحقة (ShortTermLoans)"));
                }
                cboClassification.SelectedIndex = 0;
                
                dlg.Controls.Add(lblClassification);
                dlg.Controls.Add(cboClassification);
                
                currentY += 55;
            }
            
            var lblAmt = new Label { 
                Text = type == "Reconcile" ? "الرصيد الفعلي الحالي في الحساب:" : "المبلغ (ج):", 
                Location = new Point(25, currentY), 
                AutoSize = true, 
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            txtAmount.Location = new Point(25, currentY + 22);
            dlg.Controls.Add(lblAmt);
            dlg.Controls.Add(txtAmount);
            
            currentY += 55;
            
            var lblNotes = new Label { Text = "البيان المحاسبي التفصيلي (إجباري):", Location = new Point(25, currentY), AutoSize = true, ForeColor = Theme.TextMain };
            txtNotes.Location = new Point(25, currentY + 22);
            dlg.Controls.Add(lblNotes);
            dlg.Controls.Add(txtNotes);
            
            currentY += 60;
            
            btnSave.Location = new Point(25, currentY);
            btnSave.Size = new Size(355, 38);
            dlg.Controls.Add(btnSave);
            
            dlg.Size = new Size(420, currentY + 90);

            btnSave.Click += (s, ev) =>
            {
                if (!(cboSafe.SelectedItem is ComboItem safeItem))
                {
                    MessageBox.Show("يرجى اختيار الحساب");
                    return;
                }
                if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || (type != "Reconcile" && amount <= 0))
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر");
                    txtAmount.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                {
                    MessageBox.Show("يرجى كتابة بيان محاسبي أو سبب تفصيلي للحركة");
                    txtNotes.Focus();
                    return;
                }

                string notes = txtNotes.Text.Trim();
                int targetSafeID = safeItem.ID;
                decimal currentBalance = AccountDAL.GetCashBalance(targetSafeID);

                // Get selected classification key
                string classificationName = "";
                string classificationKey = "";
                if (cboClassification != null && cboClassification.SelectedItem is ComboItem classItem)
                {
                    classificationName = classItem.Text;
                    if (type == "Deposit")
                    {
                        if (classItem.ID == 1) classificationKey = "Capital";
                        else if (classItem.ID == 2) classificationKey = "ShortTermLoans";
                        else if (classItem.ID == 3) classificationKey = "OtherRevenues";
                    }
                    else if (type == "Withdraw")
                    {
                        if (classItem.ID == 1) classificationKey = "Drawings";
                        else if (classItem.ID == 2) classificationKey = "CustodiesAdvances";
                        else if (classItem.ID == 3) classificationKey = "ShortTermLoans";
                    }
                }

                // Format Notes to include classification
                string formattedNotes = notes;
                if (!string.IsNullOrEmpty(classificationName))
                {
                    int parenIdx = classificationName.IndexOf(" (");
                    string cleanClassName = parenIdx > 0 ? classificationName.Substring(0, parenIdx) : classificationName;
                    formattedNotes = $"[{cleanClassName}] {notes}";
                }

                int? currentShiftID = Session.CurrentShiftID > 0 ? (int?)Session.CurrentShiftID : null;

                if (type == "Deposit")
                {
                    DbHelper.Execute("INSERT INTO CashBox(TransDate, TransType, AmountIn, Notes, CreatedBy, AccountID, ShiftID) VALUES(GETDATE(), 'Deposit', @amt, @n, @by, @accId, @sid)",
                        DbHelper.P("@amt", amount), DbHelper.P("@n", formattedNotes), DbHelper.P("@by", Session.EmpID), DbHelper.P("@accId", targetSafeID), DbHelper.P("@sid", currentShiftID));

                    if (!string.IsNullOrEmpty(classificationKey))
                    {
                        DbHelper.Execute("UPDATE AccountingAdjustments SET AccountValue = AccountValue + @amt WHERE AccountKey = @key",
                            DbHelper.P("@amt", amount), DbHelper.P("@key", classificationKey));
                    }
                }
                else if (type == "Withdraw")
                {
                    if (amount > currentBalance)
                    {
                        MessageBox.Show($"⛔ غير مسموح بالصرف على المكشوف أو تحويل الحساب لرصيد سالب!\nرصيد الحساب المختار ({currentBalance:N2} ج) لا يكفي لصرف مبلغ ({amount:N2} ج).", "رصيد غير كافٍ بالخزنة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DbHelper.RunInTransaction((con, trans) =>
                    {
                        AccountDAL.EnsureSufficientCashTrans(trans, targetSafeID, amount, "صرف نقدي من الحساب");

                        DbHelper.ExecuteTrans(trans, "INSERT INTO CashBox(TransDate, TransType, AmountOut, Notes, CreatedBy, AccountID, ShiftID) VALUES(GETDATE(), 'Withdraw', @amt, @n, @by, @accId, @sid)",
                            DbHelper.P("@amt", amount), DbHelper.P("@n", formattedNotes), DbHelper.P("@by", Session.EmpID), DbHelper.P("@accId", targetSafeID), DbHelper.P("@sid", currentShiftID));

                        if (!string.IsNullOrEmpty(classificationKey))
                        {
                            decimal adjustAmt = amount;
                            if (classificationKey == "ShortTermLoans")
                            {
                                adjustAmt = -amount; // Repaying loan reduces the liability
                            }
                            DbHelper.ExecuteTrans(trans, "UPDATE AccountingAdjustments SET AccountValue = AccountValue + @amt WHERE AccountKey = @key",
                                DbHelper.P("@amt", adjustAmt), DbHelper.P("@key", classificationKey));
                        }
                    });
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
                        DbHelper.Execute("INSERT INTO CashBox(TransDate, TransType, AmountIn, Notes, CreatedBy, AccountID, ShiftID) VALUES(GETDATE(), 'Deposit', @amt, @n, @by, @accId, @sid)",
                            DbHelper.P("@amt", diff), DbHelper.P("@n", "تسوية حساب (زيادة) | " + notes), DbHelper.P("@by", Session.EmpID), DbHelper.P("@accId", targetSafeID), DbHelper.P("@sid", currentShiftID));
                    }
                    else
                    {
                        DbHelper.Execute("INSERT INTO CashBox(TransDate, TransType, AmountOut, Notes, CreatedBy, AccountID, ShiftID) VALUES(GETDATE(), 'Withdraw', @amt, @n, @by, @accId, @sid)",
                            DbHelper.P("@amt", Math.Abs(diff)), DbHelper.P("@n", "تسوية حساب (عجز) | " + notes), DbHelper.P("@by", Session.EmpID), DbHelper.P("@accId", targetSafeID), DbHelper.P("@sid", currentShiftID));
                    }
                }

                MessageBox.Show("✅ تم تسجيل الحركة المالية بنجاح وتحديث الحسابات المقابلة!");
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            Theme.ApplyRTL(dlg.Controls);

            dlg.Shown += (s, ev) => { UpdateActionPreview(); txtAmount.Focus(); };

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
            if (cboExpVehicleFilter.SelectedValue != null && int.TryParse(cboExpVehicleFilter.SelectedValue.ToString(), out int vid) && vid > 0)
                selectedVehicleID = vid;
            else if (cboExpVehicleType.SelectedIndex > 0)
                selectedVehicleType = cboExpVehicleType.SelectedItem?.ToString();

            var dt = AccountDAL.GetExpenses(dtpExpFrom.Value, dtpExpTo.Value, selectedVehicleID, selectedVehicleType);
            foreach (DataRow r in dt.Rows)
            {
                if (!Session.IsAdmin)
                {
                    int safeAccID = r["SafeAccountID"] != DBNull.Value ? Convert.ToInt32(r["SafeAccountID"]) : 1;
                    if (!Session.IsSafeAllowed(safeAccID)) continue;
                }

                var vehicleLabel = r["VehicleType"] != DBNull.Value && r["VehicleName"] != DBNull.Value
                    ? $"{r["VehicleType"]} - {r["VehicleName"]}"
                    : r["VehicleName"] != DBNull.Value ? r["VehicleName"].ToString() : "";

                dgExpenses.Rows.Add(
                    r["ExpenseID"],
                    r["VehicleID"],
                    r["SafeAccountID"],
                    Convert.ToDateTime(r["ExpenseDate"]).ToString("dd/MM/yyyy"),
                    r["ExpenseType"], vehicleLabel,
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
            if (decimal.TryParse(row.Cells["Amount"].Value?.ToString(), out decimal amt)) txtExpAmount.Text = amt.ToString("N2");
            txtExpNotes.Text = row.Cells["Notes"].Value?.ToString();
            
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

            // select safe account if present
            if (cboExpSafeAccount != null && cboExpSafeAccount.Items.Count > 0)
            {
                int safeAccountID = Session.GetPrimaryAllowedSafeID();
                if (row.Cells["SafeAccountID"].Value != null && row.Cells["SafeAccountID"].Value != DBNull.Value)
                {
                    int.TryParse(row.Cells["SafeAccountID"].Value.ToString(), out safeAccountID);
                }

                cboExpSafeAccount.SelectedIndex = 0;
                for (int i = 0; i < cboExpSafeAccount.Items.Count; i++)
                {
                    if (cboExpSafeAccount.Items[i] is ComboItem item && item.ID == safeAccountID)
                    {
                        cboExpSafeAccount.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void ClearExp()
        {
            _selectedExpID = 0;
            dtpExpDate.Value = DateTime.Today;
            cboExpType.Text = "";
            txtExpAmount.Text = "";
            txtExpNotes.Clear();
            if (cboExpVehicle.Items.Count > 0) cboExpVehicle.SelectedIndex = 0;
            if (cboExpSafeAccount != null && cboExpSafeAccount.Items.Count > 0)
            {
                int defSafeId = Session.GetPrimaryAllowedSafeID();
                int targetIdx = 0;
                for (int i = 0; i < cboExpSafeAccount.Items.Count; i++)
                {
                    if (cboExpSafeAccount.Items[i] is ComboItem item && item.ID == defSafeId)
                    {
                        targetIdx = i;
                        break;
                    }
                }
                cboExpSafeAccount.SelectedIndex = targetIdx;
            }
        }

        private void BtnSaveExp_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("CashBox")) { MessageBox.Show("⛔ ليس لديك صلاحية حفظ المصروفات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(cboExpType.Text)) { MessageBox.Show("اختر نوع المصروف"); return; }
            if (!decimal.TryParse(txtExpAmount.Text.Trim(), out decimal expAmt) || expAmt <= 0) { MessageBox.Show("أدخل مبلغاً أكبر من صفر"); return; }
            int? supplierID = null;
            int? vehicleID = null;
            if (cboExpVehicle.SelectedItem != null && cboExpVehicle.SelectedValue != null && int.TryParse(cboExpVehicle.SelectedValue.ToString(), out int vid) && vid > 0)
                vehicleID = vid;
            int? safeAccountID = null;
            if (cboExpSafeAccount.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
                safeAccountID = safeItem.ID;

            if (!Session.IsAdmin)
            {
                if (!safeAccountID.HasValue || !Session.IsSafeAllowed(safeAccountID.Value))
                    safeAccountID = Session.GetPrimaryAllowedSafeID();
            }

            int targetSafeID = safeAccountID ?? Session.GetPrimaryAllowedSafeID();
            decimal currentBal = AccountDAL.GetCashBalance(targetSafeID);
            if (expAmt > currentBal && _selectedExpID == 0)
            {
                MessageBox.Show($"⛔ غير مسموح بالصرف على المكشوف أو تحويل الحساب لرصيد سالب!\nرصيد الحساب المالي المختار هو ({currentBal:N2} ج) فقط، بينما مبلغ المصروف المطلوب تسجيله هو ({expAmt:N2} ج).\nيرجى توريد نقدية أولاً أو اختيار خزنة بها رصيد كافٍ.", "رصيد غير كافٍ بالخزنة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int id = AccountDAL.SaveExpense(_selectedExpID, dtpExpDate.Value, cboExpType.Text, expAmt, txtExpNotes.Text, supplierID, vehicleID, safeAccountID);
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
            if (!Session.CanDelete("CashBox")) { MessageBox.Show("⛔ ليس لديك صلاحية حذف المصروفات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("حذف المصروف؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { AccountDAL.DeleteExpense(_selectedExpID); ClearExp(); LoadExpenses(); LoadCashBox(); }
        }

        private void LoadExpenseTypes()
        {
            cboExpType.Items.Clear();
            try
            {
                DataTable dt = DbHelper.Query("SELECT ExpenseTypeName FROM ExpenseTypes ORDER BY ExpenseTypeID");
                foreach (DataRow r in dt.Rows)
                {
                    cboExpType.Items.Add(r["ExpenseTypeName"].ToString());
                }
                if (cboExpType.Items.Count > 0) cboExpType.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load expense types failed: " + ex.Message);
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

                // Load filter combo with "--- الكل ---"
                var dtFilter = _vehiclesForExpenseFilter.Copy();
                var emptyFilterRow = dtFilter.NewRow();
                emptyFilterRow["VehicleID"] = DBNull.Value;
                emptyFilterRow["VehicleType"] = DBNull.Value;
                emptyFilterRow["VehicleName"] = "--- الكل ---";
                dtFilter.Rows.InsertAt(emptyFilterRow, 0);
                cboExpVehicleFilter.DataSource = dtFilter;
                cboExpVehicleFilter.DisplayMember = "VehicleName";
                cboExpVehicleFilter.ValueMember = "VehicleID";
                cboExpVehicleFilter.SelectedIndex = 0;

                // Load details combo with "--- لا يوجد ---"
                var dtDetails = _vehiclesForExpenseFilter.Copy();
                var emptyDetailsRow = dtDetails.NewRow();
                emptyDetailsRow["VehicleID"] = DBNull.Value;
                emptyDetailsRow["VehicleType"] = DBNull.Value;
                emptyDetailsRow["VehicleName"] = "--- لا يوجد ---";
                dtDetails.Rows.InsertAt(emptyDetailsRow, 0);
                cboExpVehicle.DataSource = dtDetails;
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
            cboExpVehicleFilter.DataSource = dt;
            cboExpVehicleFilter.SelectedIndex = 0;
        }
    }
}

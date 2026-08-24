using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة مسيرات الرواتب الشهرية والبدلات والمكافآت والخصومات
    /// </summary>
    public class FrmEmployeePayroll : Form
    {
        private TabControl tabControl;

        // ══════ تبويب 1: مسير الرواتب الشهري ══════
        private DateTimePicker dtpMonth;
        private DataGridView dgPayroll;
        private Button btnGeneratePayroll, btnPaySelectedSalary, btnPrintPayrollSheet, btnPrintPayslip;
        private Label lblTotalNetSalaries, lblTotalPaidSalaries, lblEmployeesCount;

        // ══════ تبويب 2: البدلات والمكافآت والخصومات ══════
        private ComboBox cboItemEmp, cboItemType;
        private DateTimePicker dtpItemDate;
        private TextBox txtItemAmount, txtItemReason;
        private CheckBox chkAffectCash;
        private Button btnSaveItem, btnDeleteItem;
        private DataGridView dgItems;
        private DateTimePicker dtpItemFilterFrom, dtpItemFilterTo;
        private ComboBox cboItemFilterEmp, cboItemFilterType;
        private Button btnFilterItems;

        public FrmEmployeePayroll()
        {
            InitUI();
            LoadEmployeesDropdowns();
            LoadPayrollSheet();
            LoadSalaryItems();
        }

        private void InitUI()
        {
            this.Text = "📊 مسيرات الرواتب والبدلات والمكافآت والخصومات";
            this.Size = new Size(1220, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // الشريط العلوي
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(19, 78, 74),
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblTitle = new Label
            {
                Text = "📊 إدارة الرواتب الشهرية ومفردات المرتب والبدلات والخصومات",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 12)
            };
            pnlTop.Controls.Add(lblTitle);

            // التبويبات
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Padding = new Point(16, 8)
            };

            var tabPayroll = new TabPage("📑 مسير الرواتب الشهرية والاحتساب التلقائي");
            var tabItems = new TabPage("🎁 إدارة البدلات والمكافآت والخصومات");

            BuildPayrollTab(tabPayroll);
            BuildItemsTab(tabItems);

            tabControl.TabPages.Add(tabPayroll);
            tabControl.TabPages.Add(tabItems);

            this.Controls.Add(tabControl);
            this.Controls.Add(pnlTop);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // التبويب الأول: مسير الرواتب الشهري
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildPayrollTab(TabPage tab)
        {
            tab.BackColor = Theme.BgMain;

            var pnlToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            var lblMonth = new Label { Text = "📅 شهر الرواتب:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 7, 0, 0), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            dtpMonth = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM",
                Value = DateTime.Today,
                Width = 110,
                Font = new Font("Segoe UI", 10f)
            };
            dtpMonth.ValueChanged += (s, e) => LoadPayrollSheet();

            btnGeneratePayroll = Theme.MakeButton("⚡ توليد واحتساب مسير الرواتب تلقائياً", 0, 0, 260, 36, Theme.Primary);
            btnGeneratePayroll.Click += BtnGeneratePayroll_Click;

            btnPaySelectedSalary = Theme.MakeButton("💵 صرف واعتماد راتب المحدد", 0, 0, 200, 36, Theme.Success);
            btnPaySelectedSalary.Click += BtnPaySelectedSalary_Click;

            btnPrintPayrollSheet = Theme.MakeButton("🖨️ طباعة كشف المسير", 0, 0, 150, 36, Theme.Secondary);
            btnPrintPayrollSheet.Click += BtnPrintPayrollSheet_Click;

            btnPrintPayslip = Theme.MakeButton("📄 طباعة مفردات المرتب", 0, 0, 170, 36, Color.FromArgb(79, 70, 229));
            btnPrintPayslip.Click += BtnPrintPayslip_Click;

            pnlToolbar.Controls.AddRange(new Control[] { lblMonth, dtpMonth, btnGeneratePayroll, btnPaySelectedSalary, btnPrintPayrollSheet, btnPrintPayslip });

            // شريط الإحصائيات أسفل الجدول
            var pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(15, 10, 15, 10),
                RightToLeft = RightToLeft.Yes
            };

            lblEmployeesCount = new Label { Text = "👥 عدد الموظفين: 0", AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Theme.TextMain, Margin = new Padding(10, 0, 30, 0) };
            lblTotalNetSalaries = new Label { Text = "💰 إجمالي صافي الرواتب المستحقة: 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.DarkBlue, Margin = new Padding(10, 0, 30, 0) };
            lblTotalPaidSalaries = new Label { Text = "✅ المصروف منها: 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.DarkGreen, Margin = new Padding(10, 0, 30, 0) };

            pnlSummary.Controls.AddRange(new Control[] { lblEmployeesCount, lblTotalNetSalaries, lblTotalPaidSalaries });

            // جدول مسير الرواتب
            dgPayroll = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(19, 78, 74), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 }
            };

            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "PayrollID", Visible = false });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmpID", Visible = false });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmpName", HeaderText = "الموظف", FillWeight = 110 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "JobTitle", HeaderText = "الوظيفة", FillWeight = 70 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "BasicSalary", HeaderText = "الأساسي", FillWeight = 60 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAllowances", HeaderText = "البدلات (+)", FillWeight = 55 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalBonuses", HeaderText = "مكافآت (+)", FillWeight = 55 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCommissions", HeaderText = "عمولات (+)", FillWeight = 55 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "OvertimeAmount", HeaderText = "إضافي (+)", FillWeight = 50 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalDeductions", HeaderText = "خصومات (-)", FillWeight = 55 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "AbsenceDeductions", HeaderText = "غياب (-)", FillWeight = 50 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "AdvancesDeductions", HeaderText = "سلف (-)", FillWeight = 50 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetSalary", HeaderText = "صافي الراتب", FillWeight = 75 });
            dgPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 60 });

            tab.Controls.Add(dgPayroll);
            tab.Controls.Add(pnlSummary);
            tab.Controls.Add(pnlToolbar);
        }

        private void LoadPayrollSheet()
        {
            string monthYear = dtpMonth.Value.ToString("yyyy-MM");
            var dt = EmployeeHRDAL.GetMonthlyPayrollSummary(monthYear);
            dgPayroll.Rows.Clear();

            decimal totalNet = 0m;
            decimal totalPaid = 0m;

            foreach (DataRow r in dt.Rows)
            {
                decimal basic = Convert.ToDecimal(r["BasicSalary"]);
                decimal al = Convert.ToDecimal(r["TotalAllowances"]);
                decimal bn = Convert.ToDecimal(r["TotalBonuses"]);
                decimal cm = Convert.ToDecimal(r["TotalCommissions"]);
                decimal ot = Convert.ToDecimal(r["OvertimeAmount"]);
                decimal ded = Convert.ToDecimal(r["TotalDeductions"]);
                decimal abs = Convert.ToDecimal(r["AbsenceDeductions"]);
                decimal adv = Convert.ToDecimal(r["AdvancesDeductions"]);
                decimal net = Convert.ToDecimal(r["NetSalary"]);
                bool isPaid = Convert.ToBoolean(r["IsPaid"]);

                totalNet += net;
                if (isPaid) totalPaid += net;

                int idx = dgPayroll.Rows.Add(
                    r["PayrollID"],
                    r["EmpID"],
                    r["EmpName"],
                    r["JobTitle"] != DBNull.Value ? r["JobTitle"].ToString() : r["Role"].ToString(),
                    basic.ToString("N2"),
                    al > 0 ? al.ToString("N2") : "—",
                    bn > 0 ? bn.ToString("N2") : "—",
                    cm > 0 ? cm.ToString("N2") : "—",
                    ot > 0 ? ot.ToString("N2") : "—",
                    ded > 0 ? ded.ToString("N2") : "—",
                    abs > 0 ? abs.ToString("N2") : "—",
                    adv > 0 ? adv.ToString("N2") : "—",
                    net.ToString("N2") + " ج",
                    isPaid ? "✅ تم الصرف" : "⏳ غير معتمد"
                );

                var row = dgPayroll.Rows[idx];
                row.Cells["NetSalary"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                row.Cells["NetSalary"].Style.ForeColor = Color.DarkBlue;

                if (isPaid)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244);
                    row.Cells["Status"].Style.ForeColor = Color.DarkGreen;
                }
                else
                {
                    row.Cells["Status"].Style.ForeColor = Color.DarkOrange;
                }
            }

            lblEmployeesCount.Text = $"👥 عدد الموظفين: {dgPayroll.Rows.Count:N0}";
            lblTotalNetSalaries.Text = $"💰 إجمالي صافي الرواتب المستحقة: {totalNet:N2} ج";
            lblTotalPaidSalaries.Text = $"✅ المصروف منها: {totalPaid:N2} ج";
        }

        private void BtnGeneratePayroll_Click(object sender, EventArgs e)
        {
            string mYear = dtpMonth.Value.ToString("yyyy-MM");
            if (MessageBox.Show($"هل تريد توليد واحتساب مسير رواتب شهر [{mYear}] تلقائياً من واقع (الرواتب الأساسية + البدلات + المكافآت + العمولات + ساعات الحضور والغياب والسلف)؟", "تأكيد احتساب الرواتب", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    EmployeeHRDAL.GenerateMonthlyPayroll(mYear);
                    MessageBox.Show($"✅ تم احتساب وتوليد مسير رواتب شهر [{mYear}] بنجاح.", "تم الاحتساب", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadPayrollSheet();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء احتساب الرواتب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnPaySelectedSalary_Click(object sender, EventArgs e)
        {
            if (dgPayroll.SelectedRows.Count == 0) return;
            var r = dgPayroll.SelectedRows[0];
            int pId = Convert.ToInt32(r.Cells["PayrollID"].Value);
            string empName = r.Cells["EmpName"].Value?.ToString() ?? "";
            string netStr = r.Cells["NetSalary"].Value?.ToString() ?? "0";
            string st = r.Cells["Status"].Value?.ToString() ?? "";

            if (pId <= 0)
            {
                MessageBox.Show("يرجى الضغط على زر [⚡ توليد واحتساب مسير الرواتب تلقائياً] أولاً لحفظ مسير الراتب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (st.Contains("تم الصرف"))
            {
                MessageBox.Show("تم صرف هذا الراتب مسبقاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string mYear = dtpMonth.Value.ToString("yyyy-MM");
            if (MessageBox.Show($"هل تريد اعتماد وصرف صافي راتب شهر [{mYear}] للموظف [{empName}] بقيمة ({netStr}) من الخزينة الرئيسية؟", "تأكيد صرف الراتب", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    int safeId = Session.GetDefaultSafeID();
                    if (EmployeeHRDAL.PaySalary(pId, safeId, $"صرف راتب شهر {mYear}"))
                    {
                        MessageBox.Show($"✅ تم صرف راتب الموظف [{empName}] بنجاح والتأثير على الخزينة وحساب الموظف.", "تم الصرف", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadPayrollSheet();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء صرف الراتب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // التبويب الثاني: إدارة البدلات والمكافآت والخصومات
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildItemsTab(TabPage tab)
        {
            tab.BackColor = Theme.BgMain;

            // لوحة الإدخال العلوية
            var pnlInput = new Panel
            {
                Dock = DockStyle.Top,
                Height = 115,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblEmp = new Label { Text = "👤 الموظف:", Location = new Point(1020, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cboItemEmp = new ComboBox { Location = new Point(810, 12), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };

            var lblType = new Label { Text = "نوع البند:", Location = new Point(740, 15), AutoSize = true };
            cboItemType = new ComboBox { Location = new Point(570, 12), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cboItemType.Items.AddRange(new object[] { "مكافأة", "بدل", "خصم / جزاء", "سلفة", "إضافي" });
            cboItemType.SelectedIndex = 0;

            var lblDate = new Label { Text = "التاريخ:", Location = new Point(500, 15), AutoSize = true };
            dtpItemDate = new DateTimePicker { Location = new Point(350, 12), Width = 140, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5f) };

            var lblAmt = new Label { Text = "المبلغ (ج.م):", Location = new Point(270, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtItemAmount = new TextBox { Location = new Point(140, 12), Width = 120, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Text = "0" };

            var lblReason = new Label { Text = "السبب / البيان:", Location = new Point(1020, 60), AutoSize = true };
            txtItemReason = new TextBox { Location = new Point(480, 58), Width = 530, Font = new Font("Segoe UI", 9.5f) };

            chkAffectCash = new CheckBox
            {
                Text = "صرف نقدي فوري من الخزينة وحساب الموظف (للسلف والمكافآت)",
                Location = new Point(120, 58),
                AutoSize = true,
                Checked = true,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            btnSaveItem = Theme.MakeButton("💾 إضافة البند", 15, 30, 100, 42, Theme.Success);
            btnSaveItem.Click += BtnSaveItem_Click;

            pnlInput.Controls.AddRange(new Control[] {
                lblEmp, cboItemEmp, lblType, cboItemType, lblDate, dtpItemDate, lblAmt, txtItemAmount,
                lblReason, txtItemReason, chkAffectCash, btnSaveItem
            });

            // شريط فلترة الجدول
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(10, 6, 10, 6),
                RightToLeft = RightToLeft.Yes
            };

            var lfFrom = new Label { Text = "من:", AutoSize = true, Margin = new Padding(5, 6, 0, 0) };
            dtpItemFilterFrom = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };

            var lfTo = new Label { Text = "إلى:", AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
            dtpItemFilterTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var lfEmp = new Label { Text = "الموظف:", AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
            cboItemFilterEmp = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };

            var lfType = new Label { Text = "النوع:", AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
            cboItemFilterType = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cboItemFilterType.Items.AddRange(new object[] { "الكل", "مكافأة", "بدل", "خصم / جزاء", "سلفة", "إضافي" });
            cboItemFilterType.SelectedIndex = 0;

            btnFilterItems = Theme.MakeButton("🔍 بحث", 0, 0, 90, 30, Theme.Primary);
            btnFilterItems.Click += (s, e) => LoadSalaryItems();

            btnDeleteItem = Theme.MakeButton("🗑️ حذف البند المحدد", 0, 0, 150, 30, Color.FromArgb(185, 28, 28));
            btnDeleteItem.Click += BtnDeleteItem_Click;

            pnlFilter.Controls.AddRange(new Control[] { lfFrom, dtpItemFilterFrom, lfTo, dtpItemFilterTo, lfEmp, cboItemFilterEmp, lfType, cboItemFilterType, btnFilterItems, btnDeleteItem });

            // جدول البنود
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 }
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemDate", HeaderText = "التاريخ", FillWeight = 60 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmpName", HeaderText = "اسم الموظف", FillWeight = 110 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemType", HeaderText = "نوع البند", FillWeight = 70 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "المبلغ", FillWeight = 65 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reason", HeaderText = "السبب / البيان", FillWeight = 160 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsSettled", HeaderText = "حالة التسوية بالمسير", FillWeight = 80 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "المسجل", FillWeight = 70 });

            tab.Controls.Add(dgItems);
            tab.Controls.Add(pnlFilter);
            tab.Controls.Add(pnlInput);
        }

        private void LoadSalaryItems()
        {
            int empId = (cboItemFilterEmp.SelectedItem is ComboItem ci) ? ci.ID : 0;
            string type = cboItemFilterType.SelectedItem?.ToString() ?? "الكل";

            var dt = EmployeeHRDAL.GetSalaryItems(empId, dtpItemFilterFrom.Value, dtpItemFilterTo.Value, type);
            dgItems.Rows.Clear();

            foreach (DataRow r in dt.Rows)
            {
                decimal amt = Convert.ToDecimal(r["Amount"]);
                bool settled = Convert.ToBoolean(r["IsSettled"]);
                string iType = r["ItemType"].ToString();

                int idx = dgItems.Rows.Add(
                    r["ItemID"],
                    Convert.ToDateTime(r["ItemDate"]).ToString("yyyy-MM-dd"),
                    r["EmpName"],
                    iType,
                    amt.ToString("N2") + " ج",
                    r["Reason"],
                    settled ? "✅ تمت التسوية بالراتب" : "⏳ جاري (غير مسوى)",
                    r["CreatedByName"]
                );

                var row = dgItems.Rows[idx];
                if (iType == "خصم / جزاء") row.Cells["Amount"].Style.ForeColor = Color.DarkRed;
                else if (iType == "مكافأة" || iType == "إضافي") row.Cells["Amount"].Style.ForeColor = Color.DarkGreen;
                else if (iType == "سلفة") row.Cells["Amount"].Style.ForeColor = Color.DarkOrange;
            }
        }

        private void BtnSaveItem_Click(object sender, EventArgs e)
        {
            if (cboItemEmp.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                if (decimal.TryParse(txtItemAmount.Text.Trim(), out decimal amt) && amt > 0)
                {
                    string type = cboItemType.SelectedItem.ToString();
                    string reason = txtItemReason.Text.Trim();
                    string pMonth = dtpItemDate.Value.ToString("yyyy-MM");

                    int id = EmployeeHRDAL.SaveSalaryItem(ci.ID, dtpItemDate.Value, type, amt, reason, pMonth, chkAffectCash.Checked);
                    if (id > 0)
                    {
                        MessageBox.Show("✅ تم إضافة البند بنجاح.", "تمت الإضافة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtItemAmount.Text = "0";
                        txtItemReason.Clear();
                        LoadSalaryItems();
                    }
                }
                else
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("يرجى اختيار الموظف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDeleteItem_Click(object sender, EventArgs e)
        {
            if (dgItems.SelectedRows.Count == 0) return;
            int itemId = Convert.ToInt32(dgItems.SelectedRows[0].Cells["ItemID"].Value);
            string emp = dgItems.SelectedRows[0].Cells["EmpName"].Value?.ToString() ?? "";
            string amt = dgItems.SelectedRows[0].Cells["Amount"].Value?.ToString() ?? "";

            if (MessageBox.Show($"هل تريد حذف هذا البند ({amt}) للموظف [{emp}]؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (EmployeeHRDAL.DeleteSalaryItem(itemId))
                {
                    LoadSalaryItems();
                }
            }
        }

        private void LoadEmployeesDropdowns()
        {
            var dt = EmployeeDAL.GetAll();

            cboItemEmp.Items.Clear();
            cboItemFilterEmp.Items.Clear();

            cboItemEmp.Items.Add(new ComboItem(0, "-- اختر الموظف --"));
            cboItemFilterEmp.Items.Add(new ComboItem(0, "-- كل الموظفين --"));

            foreach (DataRow r in dt.Rows)
            {
                int id = (int)r["EmpID"];
                string name = r["EmpName"].ToString();
                cboItemEmp.Items.Add(new ComboItem(id, name));
                cboItemFilterEmp.Items.Add(new ComboItem(id, name));
            }

            cboItemEmp.DisplayMember = "Text";
            cboItemFilterEmp.DisplayMember = "Text";

            cboItemEmp.SelectedIndex = 0;
            cboItemFilterEmp.SelectedIndex = 0;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // الطباعة والتقارير
        // ═══════════════════════════════════════════════════════════════════════════
        private void BtnPrintPayrollSheet_Click(object sender, EventArgs e)
        {
            if (dgPayroll.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string mYear = dtpMonth.Value.ToString("yyyy-MM");
            PrintDocument doc = new PrintDocument();
            doc.DefaultPageSettings.Landscape = true;
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 35;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 8.5f);

                g.DrawString($"كشف مسير رواتب الموظفين - شهر [{mYear}]", fontTitle, Brushes.Black, new PointF(ev.PageBounds.Width / 2 - 160, y));
                y += 35;
                g.DrawString($"{lblTotalNetSalaries.Text}   |   {lblTotalPaidSalaries.Text}", fontBody, Brushes.DarkBlue, new PointF(ev.PageBounds.Width / 2 - 170, y));
                y += 30;

                float[] colWidths = { 140, 90, 70, 60, 60, 60, 60, 60, 60, 60, 90, 80 };
                string[] headers = { "الموظف", "الوظيفة", "الأساسي", "بدلات", "مكافآت", "عمولات", "إضافي", "خصومات", "غياب", "سلف", "الصافي", "الحالة" };

                float x = 30;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightGray, x, y, colWidths[i], 26);
                    g.DrawRectangle(Pens.Gray, x, y, colWidths[i], 26);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 3, y + 4);
                    x += colWidths[i];
                }
                y += 26;

                foreach (DataGridViewRow r in dgPayroll.Rows)
                {
                    if (y > ev.PageBounds.Height - 60) break;
                    x = 30;
                    string[] vals = {
                        r.Cells["EmpName"].Value?.ToString() ?? "",
                        r.Cells["JobTitle"].Value?.ToString() ?? "",
                        r.Cells["BasicSalary"].Value?.ToString() ?? "",
                        r.Cells["TotalAllowances"].Value?.ToString() ?? "",
                        r.Cells["TotalBonuses"].Value?.ToString() ?? "",
                        r.Cells["TotalCommissions"].Value?.ToString() ?? "",
                        r.Cells["OvertimeAmount"].Value?.ToString() ?? "",
                        r.Cells["TotalDeductions"].Value?.ToString() ?? "",
                        r.Cells["AbsenceDeductions"].Value?.ToString() ?? "",
                        r.Cells["AdvancesDeductions"].Value?.ToString() ?? "",
                        r.Cells["NetSalary"].Value?.ToString() ?? "",
                        r.Cells["Status"].Value?.ToString() ?? ""
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 24);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 3, y + 4);
                        x += colWidths[i];
                    }
                    y += 24;
                }
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 950, Height = 650 })
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnPrintPayslip_Click(object sender, EventArgs e)
        {
            if (dgPayroll.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار الموظف أولاً من الجدول لطباعة قسيمة الراتب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var r = dgPayroll.SelectedRows[0];
            string empName = r.Cells["EmpName"].Value?.ToString() ?? "";
            string jobTitle = r.Cells["JobTitle"].Value?.ToString() ?? "";
            string basic = r.Cells["BasicSalary"].Value?.ToString() ?? "0";
            string allowances = r.Cells["TotalAllowances"].Value?.ToString() ?? "0";
            string bonuses = r.Cells["TotalBonuses"].Value?.ToString() ?? "0";
            string commissions = r.Cells["TotalCommissions"].Value?.ToString() ?? "0";
            string overtime = r.Cells["OvertimeAmount"].Value?.ToString() ?? "0";
            string deductions = r.Cells["TotalDeductions"].Value?.ToString() ?? "0";
            string absence = r.Cells["AbsenceDeductions"].Value?.ToString() ?? "0";
            string advances = r.Cells["AdvancesDeductions"].Value?.ToString() ?? "0";
            string netSalary = r.Cells["NetSalary"].Value?.ToString() ?? "0";
            string mYear = dtpMonth.Value.ToString("yyyy-MM");

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 50;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontSub = new Font("Segoe UI", 12f, FontStyle.Bold);
                var fontItem = new Font("Segoe UI", 10f);
                var fontBold = new Font("Segoe UI", 10.5f, FontStyle.Bold);

                // إطار خارجي
                g.DrawRectangle(Pens.DarkBlue, 40, 30, ev.PageBounds.Width - 80, 520);

                g.DrawString("قسيمة راتب ومفردات مرتب (Payslip)", fontTitle, Brushes.DarkBlue, new PointF(ev.PageBounds.Width / 2 - 130, y));
                y += 40;
                g.DrawString($"شهر: {mYear}   |   تاريخ الإصدار: {DateTime.Now:yyyy/MM/dd}", fontItem, Brushes.Gray, new PointF(ev.PageBounds.Width / 2 - 110, y));
                y += 35;

                // بيانات الموظف
                g.FillRectangle(Brushes.LightCyan, 50, y, ev.PageBounds.Width - 100, 35);
                g.DrawRectangle(Pens.CadetBlue, 50, y, ev.PageBounds.Width - 100, 35);
                g.DrawString($"اسم الموظف: {empName}       |       الوظيفة: {jobTitle}", fontBold, Brushes.Black, 60, y + 8);
                y += 50;

                // جدول الاستحقاقات والاستقطاعات
                float midX = ev.PageBounds.Width / 2;

                g.DrawString("🟢 الاستحقاقات والأرباح (+)", fontSub, Brushes.DarkGreen, 60, y);
                g.DrawString("🔴 الاستقطاعات والخصومات (-)", fontSub, Brushes.DarkRed, midX + 20, y);
                y += 30;

                string[] earnings = {
                    $"الراتب الأساسي: {basic} ج",
                    $"البدلات الشهرية: {allowances} ج",
                    $"المكافآت والحوافز: {bonuses} ج",
                    $"عمولات المبيعات: {commissions} ج",
                    $"أجر الساعات الإضافية: {overtime} ج"
                };

                string[] deductionsArr = {
                    $"الخصومات والجزاءات: {deductions} ج",
                    $"خصم الغياب والتأخير: {absence} ج",
                    $"السلف والمسحوبات: {advances} ج"
                };

                float curY = y;
                foreach (var eStr in earnings)
                {
                    g.DrawString("• " + eStr, fontItem, Brushes.Black, 60, curY);
                    curY += 25;
                }

                float curY2 = y;
                foreach (var dStr in deductionsArr)
                {
                    g.DrawString("• " + dStr, fontItem, Brushes.Black, midX + 20, curY2);
                    curY2 += 25;
                }

                y = Math.Max(curY, curY2) + 20;

                // الصافي الإجمالي
                g.FillRectangle(Brushes.LightYellow, 50, y, ev.PageBounds.Width - 100, 45);
                g.DrawRectangle(Pens.Orange, 50, y, ev.PageBounds.Width - 100, 45);
                g.DrawString($"💵 صافي الراتب المستحق للصرف: {netSalary}", new Font("Segoe UI", 13f, FontStyle.Bold), Brushes.DarkBlue, 60, y + 10);
                y += 70;

                // التوقيعات
                g.DrawString("توقيع المحاسب / الإدارة: ..........................", fontItem, Brushes.Black, 60, y);
                g.DrawString("توقيع الموظف بالاستلام: ..........................", fontItem, Brushes.Black, midX + 20, y);
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 850, Height = 650 })
            {
                dlg.ShowDialog(this);
            }
        }
    }
}

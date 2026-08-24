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
    /// شاشة حسابات الموظفين والمناديب — كشف حساب وسلف ومستحقات ورواتب
    /// </summary>
    public class FrmEmployeeTransactions : Form
    {
        private ComboBox cboEmployee;
        private Label lblBalanceVal, lblBalanceTitle;
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboTypeFilter;
        private DataGridView dgTrans;

        // إضافة حركة جديدة
        private ComboBox cboNewType;
        private TextBox txtNewAmount, txtNewNotes;
        private DateTimePicker dtpNewDate;
        private CheckBox chkAffectCash;
        private Button btnSaveTrans, btnPrint;

        // بطاقات إحصائيات علوية
        private Label lblTotalDebit, lblTotalCredit, lblTransCount;

        private int _selectedEmpID = 0;
        private string _selectedEmpName = "";

        public FrmEmployeeTransactions()
        {
            InitUI();
            LoadEmployees();
        }

        private void InitUI()
        {
            this.Text = "💰 كشف وحسابات الموظفين والمناديب";
            this.Size = new Size(1220, 740);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // الشريط العلوي مع أزرار الانتقال السريع للشاشات المرتبطة
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "💰 حسابات الموظفين والمناديب (كشف حساب تفصيلي + سلف ومستحقات)",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 14)
            };

            var btnGoAttendance = Theme.MakeButton("🕒 الحضور والانصراف", 0, 0, 150, 36, Color.FromArgb(30, 41, 59));
            btnGoAttendance.Click += (s, e) => new FrmEmployeeAttendance().ShowDialog(this);

            var btnGoPayroll = Theme.MakeButton("📊 مسيرات الرواتب", 0, 0, 140, 36, Color.FromArgb(19, 78, 74));
            btnGoPayroll.Click += (s, e) => new FrmEmployeePayroll().ShowDialog(this);

            var btnGoCommissions = Theme.MakeButton("💼 عمولات المبيعات", 0, 0, 150, 36, Color.FromArgb(14, 116, 144));
            btnGoCommissions.Click += (s, e) => new FrmEmployeeCommissions().ShowDialog(this);

            var flowShortcuts = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 2, 0, 0)
            };
            flowShortcuts.Controls.AddRange(new Control[] { btnGoAttendance, btnGoPayroll, btnGoCommissions });

            pnlTop.Controls.Add(flowShortcuts);
            pnlTop.Controls.Add(lblTitle);

            // ══════ لوحة الإدخال والبطاقة الجانبية (يمين الشاشة - 330px) ══════
            var pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 340,
                BackColor = Theme.BgCard,
                Padding = new Padding(14),
                AutoScroll = true
            };

            var lblSelectEmp = new Label { Text = "👤 اختر الموظف أو المندوب:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 12) };
            cboEmployee = new ComboBox
            {
                Location = new Point(15, 36),
                Width = 295,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            cboEmployee.SelectedIndexChanged += CboEmployee_SelectedIndexChanged;

            // بطاقة الرصيد
            var pnlBalanceCard = new Panel
            {
                Location = new Point(15, 78),
                Width = 295,
                Height = 85,
                BackColor = Theme.BgMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlBalanceCard.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Theme.Primary), 0, 0, 6, 85);
            };

            lblBalanceTitle = new Label
            {
                Text = "صافي حساب الموظف:",
                Location = new Point(12, 10),
                Width = 270,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSub
            };
            lblBalanceVal = new Label
            {
                Text = "0.00 ج",
                Location = new Point(12, 38),
                Width = 270,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Theme.Success,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBalanceCard.Controls.AddRange(new Control[] { lblBalanceTitle, lblBalanceVal });

            // نموذج إضافة حركة جديدة
            var lblNewTransTitle = new Label
            {
                Text = "⚡ تسجيل حركة مالية جديدة",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Location = new Point(15, 178),
                AutoSize = true
            };

            var lblNewType = new Label { Text = "نوع الحركة:", Location = new Point(15, 210), AutoSize = true, ForeColor = Theme.TextSub };
            cboNewType = new ComboBox
            {
                Location = new Point(15, 230),
                Width = 295,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboNewType.Items.Add("سلفة نقدية (صرف من الخزينة)");
            cboNewType.Items.Add("استحقاق راتب (قيد مستحقات)");
            cboNewType.Items.Add("صرف نقدية / دفعة راتب");
            cboNewType.Items.Add("تحصيل نقدي من موظف (سداد سلفة)");
            cboNewType.Items.Add("خصم / عجز (تسوية مديونية)");
            cboNewType.Items.Add("مكافأة / حافز (استحقاق)");
            cboNewType.SelectedIndex = 0;

            var lblNewDate = new Label { Text = "التاريخ:", Location = new Point(15, 268), AutoSize = true, ForeColor = Theme.TextSub };
            dtpNewDate = new DateTimePicker
            {
                Location = new Point(15, 288),
                Width = 295,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Font = new Font("Segoe UI", 10f)
            };

            var lblNewAmount = new Label { Text = "المبلغ (ج.م):", Location = new Point(15, 326), AutoSize = true, ForeColor = Theme.TextSub, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtNewAmount = new TextBox
            {
                Location = new Point(15, 348),
                Width = 295,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Color.DarkBlue,
                TextAlign = HorizontalAlignment.Center,
                Text = "0"
            };

            var lblNewNotes = new Label { Text = "ملاحظات وتفاصيل الحركة:", Location = new Point(15, 388), AutoSize = true, ForeColor = Theme.TextSub };
            txtNewNotes = new TextBox
            {
                Location = new Point(15, 408),
                Width = 295,
                Multiline = true,
                Height = 60,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f)
            };

            chkAffectCash = new CheckBox
            {
                Text = "التأثير على الخزينة النقدية فوراً",
                Location = new Point(15, 478),
                Width = 295,
                Checked = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Primary
            };

            btnSaveTrans = Theme.MakeButton("💾 حفظ وتأثير المالية", 15, 510, 295, 38, Theme.Accent);
            btnSaveTrans.Click += BtnSaveTrans_Click;

            pnlRight.Controls.AddRange(new Control[] {
                lblSelectEmp, cboEmployee,
                pnlBalanceCard,
                lblNewTransTitle,
                lblNewType, cboNewType,
                lblNewDate, dtpNewDate,
                lblNewAmount, txtNewAmount,
                lblNewNotes, txtNewNotes,
                chkAffectCash,
                btnSaveTrans
            });

            // ══════ لوحة الجدول والفلترة (اليسار - Fill) ══════
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            // شريط الفلاتر
            var pnlFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 6, 0, 0) };
            dtpFrom = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3) };
            dtpFrom.ValueChanged += (s, e) => LoadTransactions();

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 6, 0, 0) };
            dtpTo = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpTo.ValueChanged += (s, e) => LoadTransactions();

            var lblType = new Label { Text = "نوع الحركة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 6, 0, 0) };
            cboTypeFilter = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            cboTypeFilter.Items.AddRange(new object[] { "الكل", "سلفة", "استحقاق راتب", "صرف نقدية", "تحصيل سداد", "خصم / عجز" });
            cboTypeFilter.SelectedIndex = 0;
            cboTypeFilter.SelectedIndexChanged += (s, e) => LoadTransactions();

            btnPrint = Theme.MakeButton("🖨️ طباعة كشف حساب", 0, 0, 160, 32, Theme.Primary);
            btnPrint.Click += BtnPrint_Click;

            pnlFilters.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblType, cboTypeFilter, btnPrint });

            // شريط الإحصائيات أسفل الجدول
            var pnlBottomStats = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(15, 8, 15, 8),
                RightToLeft = RightToLeft.Yes
            };

            lblTransCount = new Label { Text = "📊 عدد الحركات: 0", AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain, Margin = new Padding(10, 0, 30, 0) };
            lblTotalDebit = new Label { Text = "🔴 إجمالي المدين (عليه/سلف): 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.DarkRed, Margin = new Padding(10, 0, 30, 0) };
            lblTotalCredit = new Label { Text = "🟢 إجمالي الدائن (له/رواتب ومكافآت): 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.DarkGreen, Margin = new Padding(10, 0, 30, 0) };

            pnlBottomStats.Controls.AddRange(new Control[] { lblTransCount, lblTotalDebit, lblTotalCredit });

            // جدول الحركات
            dgTrans = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                GridColor = Theme.BorderColor,
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
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 }
            };

            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransID", Visible = false });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ والوقت", FillWeight = 60 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع الحركة", FillWeight = 60 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "مدين (عليه/سلف)", FillWeight = 50 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "دائن (له/مستحقات)", FillWeight = 50 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "RunningBalance", HeaderText = "الرصيد التراكمي", FillWeight = 55 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات والتفاصيل", FillWeight = 120 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "المسجل", FillWeight = 50 });

            // عمود الحذف
            DataGridViewButtonColumn btnDelCol = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "",
                Text = "🗑",
                UseColumnTextForButtonValue = true,
                FillWeight = 22f
            };
            dgTrans.Columns.Add(btnDelCol);
            dgTrans.CellClick += DgTrans_CellClick;

            pnlLeft.Controls.Add(dgTrans);
            pnlLeft.Controls.Add(pnlBottomStats);
            pnlLeft.Controls.Add(pnlFilters);

            this.Controls.Add(pnlLeft);
            this.Controls.Add(pnlRight);
            this.Controls.Add(pnlTop);
        }

        private void LoadEmployees()
        {
            var dt = EmployeeDAL.GetAll();
            cboEmployee.Items.Clear();
            cboEmployee.Items.Add(new ComboItem(0, "-- اختر الموظف / المندوب --"));
            foreach (DataRow r in dt.Rows)
            {
                cboEmployee.Items.Add(new ComboItem((int)r["EmpID"], r["EmpName"].ToString()));
            }
            cboEmployee.DisplayMember = "Text";
            cboEmployee.SelectedIndex = 0;
        }

        private void CboEmployee_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboEmployee.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                _selectedEmpID = ci.ID;
                _selectedEmpName = ci.Name;
                LoadEmployeeBalance();
                LoadTransactions();
            }
            else
            {
                _selectedEmpID = 0;
                _selectedEmpName = "";
                lblBalanceVal.Text = "0.00 ج";
                lblTransCount.Text = "📊 عدد الحركات: 0";
                lblTotalDebit.Text = "🔴 إجمالي المدين: 0.00 ج";
                lblTotalCredit.Text = "🟢 إجمالي الدائن: 0.00 ج";
                dgTrans.Rows.Clear();
            }
        }

        private void LoadEmployeeBalance()
        {
            if (_selectedEmpID <= 0) return;
            decimal bal = EmployeeDAL.GetBalance(_selectedEmpID);
            lblBalanceVal.Text = $"{bal:N2} ج";

            if (bal > 0)
            {
                lblBalanceVal.ForeColor = Color.DarkRed;
                lblBalanceTitle.Text = "صافي المديونية / السلفة على الموظف (مدين):";
            }
            else if (bal < 0)
            {
                lblBalanceVal.ForeColor = Color.DarkGreen;
                lblBalanceTitle.Text = "مستحقات الموظف غير المصروفة (دائن):";
                lblBalanceVal.Text = $"{Math.Abs(bal):N2} ج";
            }
            else
            {
                lblBalanceVal.ForeColor = Theme.TextMain;
                lblBalanceTitle.Text = "صافي حساب الموظف:";
            }
        }

        private void LoadTransactions()
        {
            if (_selectedEmpID <= 0) return;

            string filterType = "All";
            if (cboTypeFilter.SelectedIndex == 1) filterType = "Advance";
            else if (cboTypeFilter.SelectedIndex == 2) filterType = "Salary";
            else if (cboTypeFilter.SelectedIndex == 3) filterType = "SalaryPayment";
            else if (cboTypeFilter.SelectedIndex == 4) filterType = "Repayment";
            else if (cboTypeFilter.SelectedIndex == 5) filterType = "Deduction";

            var dt = EmployeeDAL.GetTransactions(_selectedEmpID, dtpFrom.Value, dtpTo.Value, filterType);
            dgTrans.Rows.Clear();

            decimal totalDebit = 0m;
            decimal totalCredit = 0m;
            decimal runningBalance = 0m;

            // ترتيب زمني لحساب الرصيد التراكمي
            var rows = dt.Select("", "TransDate ASC, TransID ASC");

            foreach (DataRow r in rows)
            {
                string rawType = r["TransType"].ToString();
                string typeArabic = rawType;
                if (rawType == "Advance") typeArabic = "سلفة نقدية";
                else if (rawType == "Salary") typeArabic = "استحقاق راتب";
                else if (rawType == "SalaryPayment") typeArabic = "صرف نقدية/راتب";
                else if (rawType == "Repayment") typeArabic = "تحصيل سداد";
                else if (rawType == "Deduction") typeArabic = "خصم / عجز";
                else if (rawType == "Commission") typeArabic = "عمولة مبيعات";
                else if (rawType == "Bonus") typeArabic = "مكافأة / حافز";

                decimal debit = Convert.ToDecimal(r["Debit"]);
                decimal credit = Convert.ToDecimal(r["Credit"]);

                totalDebit += debit;
                totalCredit += credit;
                runningBalance += (debit - credit);

                dgTrans.Rows.Insert(0,
                    r["TransID"],
                    Convert.ToDateTime(r["TransDate"]).ToString("yyyy/MM/dd hh:mm tt"),
                    typeArabic,
                    debit > 0 ? debit.ToString("N2") : "—",
                    credit > 0 ? credit.ToString("N2") : "—",
                    runningBalance.ToString("N2") + " ج",
                    r["Notes"].ToString(),
                    r["CreatedByName"].ToString()
                );

                var row = dgTrans.Rows[0];
                if (debit > 0) row.Cells["Debit"].Style.ForeColor = Color.DarkRed;
                if (credit > 0) row.Cells["Credit"].Style.ForeColor = Color.DarkGreen;
            }

            lblTransCount.Text = $"📊 عدد الحركات: {dgTrans.Rows.Count:N0}";
            lblTotalDebit.Text = $"🔴 إجمالي المدين: {totalDebit:N2} ج";
            lblTotalCredit.Text = $"🟢 إجمالي الدائن: {totalCredit:N2} ج";
        }

        private static readonly string[] _transTypeKeys = { "Advance", "Salary", "SalaryPayment", "Repayment", "Deduction", "Bonus" };
        private static readonly string[] _transTypeArabic = { "سلفة نقدية", "استحقاق راتب", "صرف نقدية/راتب", "تحصيل سداد", "خصم / عجز", "مكافأة / حافز" };

        private void BtnSaveTrans_Click(object sender, EventArgs e)
        {
            if (_selectedEmpID <= 0) { MessageBox.Show("يرجى اختيار الموظف أولاً."); return; }
            if (cboNewType.SelectedIndex < 0) return;
            if (!decimal.TryParse(txtNewAmount.Text.Trim(), out decimal amt) || amt <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من صفر.");
                return;
            }

            string transType = _transTypeKeys[cboNewType.SelectedIndex];
            string transTypeAr = _transTypeArabic[cboNewType.SelectedIndex];

            decimal debit = 0;
            decimal credit = 0;
            bool affectCash = chkAffectCash.Checked;

            if (transType == "Advance" || transType == "SalaryPayment")
            {
                debit = amt;
            }
            else if (transType == "Salary" || transType == "Repayment" || transType == "Bonus")
            {
                credit = amt;
            }
            else if (transType == "Deduction")
            {
                credit = amt;
            }

            try
            {
                string note = txtNewNotes.Text.Trim();
                if (string.IsNullOrEmpty(note))
                {
                    note = transTypeAr + " للموظف " + _selectedEmpName;
                }

                int id = EmployeeDAL.SaveTransaction(_selectedEmpID, dtpNewDate.Value, transType, debit, credit, note, affectCash);
                if (id > 0)
                {
                    MessageBox.Show("✅ تم تسجيل الحركة بنجاح وتأثير الخزينة.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtNewAmount.Text = "0";
                    txtNewNotes.Clear();
                    LoadEmployeeBalance();
                    LoadTransactions();
                }
                else MessageBox.Show("❌ فشل تسجيل الحركة.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء حفظ الحركة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgTrans_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgTrans.Columns[e.ColumnIndex].Name == "Delete")
            {
                int transId = Convert.ToInt32(dgTrans.Rows[e.RowIndex].Cells["TransID"].Value);
                string dtStr = dgTrans.Rows[e.RowIndex].Cells["TransDate"].Value?.ToString() ?? "";
                string type = dgTrans.Rows[e.RowIndex].Cells["TransType"].Value?.ToString() ?? "";

                if (MessageBox.Show($"هل تريد بالتأكيد حذف هذه الحركة ({type}) المسجلة بتاريخ {dtStr}؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (EmployeeDAL.DeleteTransaction(transId))
                    {
                        MessageBox.Show("✅ تم حذف الحركة بنجاح.");
                        LoadEmployeeBalance();
                        LoadTransactions();
                    }
                }
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (_selectedEmpID <= 0 || dgTrans.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد حركات لطباعة كشف الحساب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 40;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 10f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);
                var fontBold = new Font("Segoe UI", 10f, FontStyle.Bold);

                g.DrawString($"كشف حساب الموظف / المندوب: {_selectedEmpName}", fontTitle, Brushes.Black, new PointF(ev.PageBounds.Width / 2 - 160, y));
                y += 35;
                g.DrawString($"الفترة من: {dtpFrom.Value:yyyy/MM/dd} إلى: {dtpTo.Value:yyyy/MM/dd}   |   {lblBalanceTitle.Text} {lblBalanceVal.Text}", fontBody, Brushes.DarkBlue, new PointF(ev.PageBounds.Width / 2 - 180, y));
                y += 35;

                float[] colWidths = { 130, 90, 80, 80, 90, 200 };
                string[] headers = { "التاريخ والوقت", "نوع الحركة", "مدين (عليه)", "دائن (له)", "الرصيد", "الملاحظات" };

                float x = 40;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightGray, x, y, colWidths[i], 26);
                    g.DrawRectangle(Pens.Gray, x, y, colWidths[i], 26);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 4, y + 4);
                    x += colWidths[i];
                }
                y += 26;

                foreach (DataGridViewRow r in dgTrans.Rows)
                {
                    if (y > ev.PageBounds.Height - 60) break;
                    x = 40;
                    string[] vals = {
                        r.Cells["TransDate"].Value?.ToString() ?? "",
                        r.Cells["TransType"].Value?.ToString() ?? "",
                        r.Cells["Debit"].Value?.ToString() ?? "",
                        r.Cells["Credit"].Value?.ToString() ?? "",
                        r.Cells["RunningBalance"].Value?.ToString() ?? "",
                        r.Cells["Notes"].Value?.ToString() ?? ""
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
    }
}

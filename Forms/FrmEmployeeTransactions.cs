using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة حسابات الموظفين والمناديب — رواتب وسلف ومستحقات</summary>
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
        private Button btnSaveTrans, btnPrint;

        private int _selectedEmpID = 0;
        private string _selectedEmpName = "";

        public FrmEmployeeTransactions()
        {
            InitUI();
            LoadEmployees();
        }

        private void InitUI()
        {
            this.Text = "حسابات الموظفين والمناديب";
            this.Size = new Size(1150, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ══════ الهيكل الرئيسي: جدول عمودين ══════
            var tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6)
            };
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // اليسار: جدول الحركات (توسيع تلقائي)
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310f)); // اليمين: الاختيار + البطاقة + إضافة حركة (عرض ثابت)

            // ══════ العمود الأيمن: الاختيار والبطاقات والادخال ══════
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(12),
                AutoScroll = true
            };

            var lblSelectEmp = new Label { Text = "👤 اختر الموظف أو المندوب:", AutoSize = true, ForeColor = Theme.TextSub, Margin = new Padding(0, 0, 0, 5) };
            cboEmployee = new ComboBox
            {
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            cboEmployee.SelectedIndexChanged += CboEmployee_SelectedIndexChanged;

            // بطاقة الرصيد
            var pnlBalanceCard = new Panel
            {
                Width = 260,
                Height = 85,
                BackColor = Theme.BgMain,
                Margin = new Padding(0, 15, 0, 15),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlBalanceCard.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Theme.Primary), 0, 0, 6, 85);
            };

            lblBalanceTitle = new Label
            {
                Text = "صافي المديونية / السلفة على الموظف:",
                Location = new Point(12, 10),
                Width = 230,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSub
            };
            lblBalanceVal = new Label
            {
                Text = "0.00 ج",
                Location = new Point(12, 38),
                Width = 230,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Theme.Success,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBalanceCard.Controls.AddRange(new Control[] { lblBalanceTitle, lblBalanceVal });

            // نموذج إضافة حركة جديدة
            var lblNewTransTitle = new Label
            {
                Text = "⚡ تسجيل حركة مالية جديدة",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Margin = new Padding(0, 15, 0, 8)
            };

            var lblNewType = new Label { Text = "نوع الحركة:", AutoSize = true, ForeColor = Theme.TextSub };
            cboNewType = new ComboBox
            {
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboNewType.Items.Add("سلفة نقدية (من الخزينة)");
            cboNewType.Items.Add("استحقاق راتب (قيد مستحقات)");
            cboNewType.Items.Add("صرف نقدية / دفعة راتب");
            cboNewType.Items.Add("تحصيل نقدي من موظف (سداد سلفة)");
            cboNewType.Items.Add("خصم / عجز (تسوية مديونية)");
            cboNewType.SelectedIndex = 0;

            var lblNewDate = new Label { Text = "التاريخ:", AutoSize = true, ForeColor = Theme.TextSub, Margin = new Padding(0, 10, 0, 2) };
            dtpNewDate = new DateTimePicker
            {
                Width = 260,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };

            var lblNewAmount = new Label { Text = "المبلغ (ج.م):", AutoSize = true, ForeColor = Theme.TextSub, Margin = new Padding(0, 10, 0, 2) };
            txtNewAmount = new TextBox
            {
                Width = 260,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblNewNotes = new Label { Text = "ملاحظات وتفاصيل الحركة:", AutoSize = true, ForeColor = Theme.TextSub, Margin = new Padding(0, 10, 0, 2) };
            txtNewNotes = new TextBox
            {
                Width = 260,
                Multiline = true,
                Height = 65,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnSaveTrans = Theme.MakeButton("💾 حفظ وتأثير المالية", Theme.Accent);
            btnSaveTrans.Size = new Size(260, 36);
            btnSaveTrans.Click += BtnSaveTrans_Click;

            // ترتيب عناصر العمود الأيمن عمودياً
            var flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(5),
                AutoScroll = true
            };
            flowRight.Controls.AddRange(new Control[] {
                lblSelectEmp, cboEmployee,
                pnlBalanceCard,
                lblNewTransTitle,
                lblNewType, cboNewType,
                lblNewDate, dtpNewDate,
                lblNewAmount, txtNewAmount,
                lblNewNotes, txtNewNotes,
                new Panel { Height = 10 },
                btnSaveTrans
            });
            pnlRight.Controls.Add(flowRight);

            // ══════ العمود الأيسر: شريط الفلترة وجدول الحركات ══════
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };

            // شريط الفلاتر
            var pnlFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                FlowDirection = FlowDirection.RightToLeft
            };

            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 5, 0, 0) };
            dtpFrom = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Today.AddMonths(-1) };
            dtpFrom.ValueChanged += (s, e) => LoadTransactions();

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            dtpTo = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Now };
            dtpTo.ValueChanged += (s, e) => LoadTransactions();

            var lblType = new Label { Text = "النوع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            cboTypeFilter = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            cboTypeFilter.Items.AddRange(new object[] { "الكل", "سلفة", "استحقاق راتب", "صرف نقدية", "تحصيل سداد", "خصم / عجز" });
            cboTypeFilter.SelectedIndex = 0;
            cboTypeFilter.SelectedIndexChanged += (s, e) => LoadTransactions();

            btnPrint = Theme.MakeButton("🖨️ طباعة كشف حساب", Theme.Primary);
            btnPrint.Size = new Size(160, 26);
            btnPrint.Margin = new Padding(30, 0, 0, 0);
            btnPrint.Click += BtnPrint_Click;

            pnlFilters.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblType, cboTypeFilter, btnPrint });

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
                RowTemplate = { Height = 28 }
            };

            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransID", Visible = false });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ", FillWeight = 50 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع الحركة", FillWeight = 60 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "مدين (عليه)", FillWeight = 40 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "دائن (له)", FillWeight = 40 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات والتفاصيل", FillWeight = 130 });
            dgTrans.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "بواسطة", FillWeight = 50 });

            // عمود الحذف
            DataGridViewButtonColumn btnDelCol = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "",
                Text = "🗑",
                UseColumnTextForButtonValue = true,
                FillWeight = 20f
            };
            dgTrans.Columns.Add(btnDelCol);
            dgTrans.CellClick += DgTrans_CellClick;

            pnlLeft.Controls.Add(dgTrans);
            pnlLeft.Controls.Add(pnlFilters);

            // تجميع المكونات
            tblMain.Controls.Add(pnlLeft, 0, 0);  // اليسار
            tblMain.Controls.Add(pnlRight, 1, 0); // اليمين
            this.Controls.Add(tblMain);

            Theme.ApplyFormRTL(this);
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
                lblBalanceVal.ForeColor = Color.Tomato;
                lblBalanceTitle.Text = "صافي المديونية / السلفة على الموظف (مدين):";
            }
            else if (bal < 0)
            {
                lblBalanceVal.ForeColor = Color.LightGreen;
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

            foreach (DataRow r in dt.Rows)
            {
                string rawType = r["TransType"].ToString();
                string typeArabic = rawType;
                if (rawType == "Advance") typeArabic = "سلفة نقدية";
                else if (rawType == "Salary") typeArabic = "استحقاق راتب";
                else if (rawType == "SalaryPayment") typeArabic = "صرف نقدية/راتب";
                else if (rawType == "Repayment") typeArabic = "تحصيل سداد";
                else if (rawType == "Deduction") typeArabic = "خصم / عجز";
                else if (rawType == "DeficitCharge") typeArabic = "مديونية عجز حمولة";

                decimal debit = Convert.ToDecimal(r["Debit"]);
                decimal credit = Convert.ToDecimal(r["Credit"]);

                dgTrans.Rows.Add(
                    r["TransID"],
                    Convert.ToDateTime(r["TransDate"]).ToString("dd/MM/yyyy HH:mm"),
                    typeArabic,
                    debit > 0 ? debit.ToString("N2") : "—",
                    credit > 0 ? credit.ToString("N2") : "—",
                    r["Notes"].ToString(),
                    r["CreatedByName"].ToString()
                );
            }
        }

        // Map combobox index → internal type key
        private static readonly string[] _transTypeKeys = { "Advance", "Salary", "SalaryPayment", "Repayment", "Deduction" };
        private static readonly string[] _transTypeArabic = { "سلفة نقدية", "استحقاق راتب", "صرف نقدية/راتب", "تحصيل سداد", "خصم / عجز" };

        private void BtnSaveTrans_Click(object sender, EventArgs e)
        {
            if (_selectedEmpID <= 0) { MessageBox.Show("يرجى اختيار الموظف أولاً."); return; }
            if (cboNewType.SelectedIndex < 0) return;
            if (!decimal.TryParse(txtNewAmount.Text, out decimal amt) || amt <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من صفر.");
                return;
            }

            string transType = _transTypeKeys[cboNewType.SelectedIndex];
            string transTypeAr = _transTypeArabic[cboNewType.SelectedIndex];

            decimal debit = 0;
            decimal credit = 0;
            bool affectCash = false;

            if (transType == "Advance")
            {
                debit = amt;
                affectCash = true;
            }
            else if (transType == "Salary")
            {
                credit = amt;
                affectCash = false;
            }
            else if (transType == "SalaryPayment")
            {
                debit = amt;
                affectCash = true;
            }
            else if (transType == "Repayment")
            {
                credit = amt;
                affectCash = true;
            }
            else if (transType == "Deduction")
            {
                credit = amt; // Deduction reduces driver debt balance, which is Debit - Credit, so Credit increases to reduce balance
                affectCash = false;
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
                    MessageBox.Show("✅ تم تسجيل الحركة بنجاح وتأثير الخزينة إذا تطلب ذلك.");
                    txtNewAmount.Clear();
                    txtNewNotes.Clear();
                    LoadEmployeeBalance();
                    LoadTransactions();
                }
                else MessageBox.Show("❌ فشل تسجيل الحركة.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ أثناء حفظ الحركة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgTrans_Click(object sender, EventArgs e)
        {
        }

        private void DgTrans_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgTrans.Columns[e.ColumnIndex].Name == "Delete")
            {
                if (MessageBox.Show("هل أنت متأكد من حذف هذه الحركة؟\nسيتم عكس أثرها وحذف القيد المالي المقابل من الخزينة أيضاً.", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        int transID = Convert.ToInt32(dgTrans.Rows[e.RowIndex].Cells["TransID"].Value);
                        EmployeeDAL.DeleteTransaction(transID);
                        MessageBox.Show("✅ تم حذف الحركة بنجاح وتحديث الحسابات والخزينة.");
                        LoadEmployeeBalance();
                        LoadTransactions();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ خطأ أثناء حذف الحركة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // =====================================================================
        // منطق طباعة كشف حساب موظف
        // =====================================================================
        private int _printRowIndex = 0;

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (_selectedEmpID <= 0) { MessageBox.Show("اختر موظفاً أولاً"); return; }
            if (dgTrans.Rows.Count == 0) { MessageBox.Show("لا توجد حركات لطباعتها"); return; }

            _printRowIndex = 0;

            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
            pd.PrintPage += Pd_PrintPage;

            var ppd = new PrintPreviewDialog
            {
                Document = pd,
                Width = 1100,
                Height = 750,
                WindowState = FormWindowState.Maximized,
                RightToLeft = RightToLeft.Yes
            };
            ppd.ShowDialog(this);
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            var g = ev.Graphics;
            int margin = 40;
            int printW = ev.PageBounds.Width - (margin * 2);
            int y = margin;

            var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
            var fSub = new Font("Segoe UI", 10f, FontStyle.Bold);
            var fHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var fData = new Font("Segoe UI", 9f);
            var fTotal = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            // ─── الصفحة الأولى ───
            if (_printRowIndex == 0)
            {
                // شعار ورأس الكشف
                g.DrawString("🐣 " + AppConfig.CompanyName, fSub, Brushes.DimGray, margin, y);
                y += 20;
                g.DrawString("كشف حساب موظف / مندوب تفصيلي", fTitle, Brushes.MidnightBlue, margin, y);
                y += 35;

                g.DrawLine(new Pen(Color.MidnightBlue, 2f), margin, y, printW + margin, y);
                y += 10;

                // بيانات الموظف
                g.DrawString($"الاسم: {_selectedEmpName}", fSub, Brushes.Black, margin, y);
                g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", fSub, Brushes.Black, margin + 450, y);
                y += 20;
                g.DrawString($"الفترة: من {dtpFrom.Value:dd/MM/yyyy} إلى {dtpTo.Value:dd/MM/yyyy}", fSub, Brushes.Black, margin, y);
                g.DrawString($"الرصيد الحالي: {EmployeeDAL.GetBalance(_selectedEmpID):N2} ج.م", fSub, Brushes.DarkRed, margin + 450, y);
                y += 25;

                g.DrawLine(Pens.Gray, margin, y, printW + margin, y);
                y += 10;

                // جدول الحركات
                int x = margin;
                string[] headers = { "التاريخ", "النوع", "مدين (عليه)", "دائن (له)", "ملاحظات" };
                int[] widths = { 110, 100, 80, 80, 400 };

                for (int i = 0; i < headers.Length; i++)
                {
                    g.DrawString(headers[i], fHeader, Brushes.MidnightBlue, x, y);
                    x += widths[i];
                }
                y += 22;
                g.DrawLine(Pens.DarkBlue, margin, y, printW + margin, y);
                y += 8;
            }

            int[] colW = { 110, 100, 80, 80, 400 };

            // رسم الصفوف
            while (_printRowIndex < dgTrans.Rows.Count)
            {
                var row = dgTrans.Rows[dgTrans.Rows.Count - 1 - _printRowIndex]; // ترتيب تصاعدي تاريخياً للطباعة
                int x = margin;

                string dateStr = row.Cells["TransDate"].Value?.ToString() ?? "";
                string typeStr = row.Cells["TransType"].Value?.ToString() ?? "";
                string debitStr = row.Cells["Debit"].Value?.ToString() ?? "";
                string creditStr = row.Cells["Credit"].Value?.ToString() ?? "";
                string noteStr = row.Cells["Notes"].Value?.ToString() ?? "";

                // رسم حقول الصف
                g.DrawString(dateStr, fData, Brushes.Black, x, y); x += colW[0];
                g.DrawString(typeStr, fData, Brushes.Black, x, y); x += colW[1];
                g.DrawString(debitStr, fData, Brushes.Black, x, y); x += colW[2];
                g.DrawString(creditStr, fData, Brushes.Black, x, y); x += colW[3];

                // التفاف الملاحظات
                var rectNote = new RectangleF(x, y, colW[4], 32);
                g.DrawString(noteStr, fData, Brushes.Black, rectNote, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });

                y += 20;
                _printRowIndex++;

                // التحقق من نهاية الصفحة
                if (y > ev.PageBounds.Height - margin - 40)
                {
                    ev.HasMorePages = true;
                    return;
                }
            }

            // رسم الإجمالي في نهاية الكشف
            y += 10;
            g.DrawLine(new Pen(Color.MidnightBlue, 1.5f), margin, y, printW + margin, y);
            y += 8;

            decimal totalDebit = 0, totalCredit = 0;
            foreach (DataGridViewRow r in dgTrans.Rows)
            {
                if (decimal.TryParse(r.Cells["Debit"].Value?.ToString()?.Replace(" ج", ""), out decimal d)) totalDebit += d;
                if (decimal.TryParse(r.Cells["Credit"].Value?.ToString()?.Replace(" ج", ""), out decimal c)) totalCredit += c;
            }

            g.DrawString("الإجماليات الكلية للفترة:", fTotal, Brushes.MidnightBlue, margin, y);
            g.DrawString($"إجمالي المدين: {totalDebit:N2} ج", fTotal, Brushes.DarkRed, margin + 200, y);
            g.DrawString($"إجمالي الدائن: {totalCredit:N2} ج", fTotal, Brushes.DarkGreen, margin + 400, y);
            
            _printRowIndex = 0; // تصفير للمرة القادمة
            ev.HasMorePages = false;
        }
    }
}

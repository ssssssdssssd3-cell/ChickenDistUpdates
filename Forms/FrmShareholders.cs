using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// مديول إدارة الشركاء والمساهمين ورأس المال وتوزيع وصرف الأرباح
    /// </summary>
    public class FrmShareholders : Form
    {
        private Label lblTotalCapVal, lblPartnerCountVal, lblTotalDrawingsVal, lblTotalDividendsVal;
        private TabControl tabMain;
        private TabPage tabPartners, tabStatement, tabDividends;

        // Partners Tab Controls
        private DataGridView dgPartners;
        private Button btnAddPartner, btnEditPartner, btnDepositCap, btnPartnerDrawing, btnPartnerStatement, btnDeletePartner, btnPrintPartners;

        // Statement Tab Controls
        private ComboBox cboStatementPartner;
        private DateTimePicker dtpFrom, dtpTo;
        private DataGridView dgStatement;
        private Label lblStmtBalance, lblStmtDebit, lblStmtCredit;
        private Button btnRefreshStmt, btnAddTrans, btnPrintStmt;

        // Dividends Tab Controls
        private DateTimePicker dtpDivFrom, dtpDivTo;
        private TextBox txtNetProfit, txtRetainedPct, txtDistributedAmt, txtDivNotes;
        private Button btnCalculateProfit, btnPreviewDividends, btnPostDividends, btnDisburseDividends, btnPrintDivReport;
        private DataGridView dgDivPreview;
        private List<DividendDistributionLineDTO> _currentDivList = new List<DividendDistributionLineDTO>();

        public FrmShareholders()
        {
            InitUI();
            RefreshDashboard();
            LoadPartnersGrid();
            LoadStatementPartnersCombo();
        }

        private void InitUI()
        {
            this.Text = "🤝 إدارة الشركاء والمساهمين وحسابات رأس المال وتوزيع الأرباح";
            this.Size = new Size(1180, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. Dashboard Metrics Panel
            var pnlMetrics = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 68,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Theme.BgMain,
                Padding = new Padding(10, 6, 10, 6)
            };

            pnlMetrics.Controls.Add(MakeMetricCard("💰 إجمالي رأس المال", out lblTotalCapVal, Theme.Primary));
            pnlMetrics.Controls.Add(MakeMetricCard("👥 عدد الشركاء النشطين", out lblPartnerCountVal, Color.FromArgb(52, 152, 219)));
            pnlMetrics.Controls.Add(MakeMetricCard("💸 إجمالي المسحوبات الشخصية", out lblTotalDrawingsVal, Theme.Danger));
            pnlMetrics.Controls.Add(MakeMetricCard("📈 إجمالي الأرباح الموزعة", out lblTotalDividendsVal, Theme.Success));

            // 2. TabControl
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontMain
            };

            tabPartners = new TabPage("👥 دليل الشركاء ورؤوس الأموال") { BackColor = Theme.BgMain };
            tabStatement = new TabPage("📊 كشف حساب وحركات الشريك") { BackColor = Theme.BgMain };
            tabDividends = new TabPage("💰 محرك احتساب وتوزيع وصرف الأرباح") { BackColor = Theme.BgMain };

            BuildPartnersTab(tabPartners);
            BuildStatementTab(tabStatement);
            BuildDividendsTab(tabDividends);

            tabMain.TabPages.AddRange(new TabPage[] { tabPartners, tabStatement, tabDividends });

            this.Controls.Add(tabMain);
            this.Controls.Add(pnlMetrics);
        }

        private Panel MakeMetricCard(string title, out Label valLabel, Color barColor)
        {
            var pnl = new Panel
            {
                Width = 265,
                Height = 56,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 10, 0)
            };

            var bar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 5,
                BackColor = barColor
            };

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                Location = new Point(5, 5),
                AutoSize = true
            };

            valLabel = new Label
            {
                Text = "0.00 ج",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Location = new Point(5, 26),
                AutoSize = true
            };

            pnl.Controls.AddRange(new Control[] { bar, lblT, valLabel });
            return pnl;
        }

        private void RefreshDashboard()
        {
            var metrics = ShareholdersDAL.GetSummaryMetrics();
            lblTotalCapVal.Text = $"{metrics.totalCapital:N2} ج";
            lblPartnerCountVal.Text = $"{metrics.partnerCount} شريك";
            lblTotalDrawingsVal.Text = $"{metrics.totalDrawings:N2} ج";
            lblTotalDividendsVal.Text = $"{metrics.totalDistributed:N2} ج";
        }

        // ══════════════════════════════════════════════════
        // تبويب 1: دليل الشركاء ورؤوس الأموال
        // ══════════════════════════════════════════════════
        private void BuildPartnersTab(TabPage tab)
        {
            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10, 7, 10, 7),
                WrapContents = false,
                RightToLeft = RightToLeft.Yes
            };

            btnAddPartner = Theme.MakeButton("➕ إضافة شريك جديد", 0, 0, 150, 32, Theme.Primary);
            btnAddPartner.Click += (s, e) => OpenPartnerEditor(0);
            pnlTop.Controls.Add(btnAddPartner);

            btnPrintPartners = Theme.MakeButton("🖨️ طباعة دليل الشركاء", 0, 0, 150, 32, Theme.Secondary);
            btnPrintPartners.Click += BtnPrintPartners_Click;
            pnlTop.Controls.Add(btnPrintPartners);

            // أزرار العمليات السريعة السفلية
            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 7, 10, 7),
                RightToLeft = RightToLeft.Yes
            };

            btnEditPartner = Theme.MakeButton("✏️ تعديل البيانات", 0, 0, 120, 32, Theme.Accent);
            btnEditPartner.Click += (s, e) => { if (GetSelectedPartnerID() > 0) OpenPartnerEditor(GetSelectedPartnerID()); };

            btnDepositCap = Theme.MakeButton("💰 إيداع رأس مال", 0, 0, 130, 32, Theme.Success);
            btnDepositCap.Click += (s, e) => OpenQuickTransDialog("CapitalDeposit");

            btnPartnerDrawing = Theme.MakeButton("💸 مسحوبات شخصية", 0, 0, 135, 32, Theme.Danger);
            btnPartnerDrawing.Click += (s, e) => OpenQuickTransDialog("PersonalDrawing");

            btnPartnerStatement = Theme.MakeButton("📊 كشف الحساب", 0, 0, 120, 32, Color.FromArgb(70, 70, 70));
            btnPartnerStatement.Click += (s, e) =>
            {
                int pid = GetSelectedPartnerID();
                if (pid > 0)
                {
                    SelectPartnerInStatement(pid);
                    tabMain.SelectedIndex = 1;
                }
            };

            btnDeletePartner = Theme.MakeButton("🗑️ حذف", 0, 0, 85, 32, Color.FromArgb(153, 27, 27));
            btnDeletePartner.Click += BtnDeletePartner_Click;

            pnlBottom.Controls.AddRange(new Control[] { btnEditPartner, btnDepositCap, btnPartnerDrawing, btnPartnerStatement, btnDeletePartner });

            dgPartners = new DataGridView
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
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgPartners.DoubleClick += (s, e) => { if (GetSelectedPartnerID() > 0) OpenPartnerEditor(GetSelectedPartnerID()); };

            dgPartners.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartnerID", Visible = false });
            dgPartners.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartnerCode", HeaderText = "كود الشريك", FillWeight = 40 });
            dgPartners.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartnerName", HeaderText = "اسم الشريك / المساهم", FillWeight = 90 });
            dgPartners.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "الهاتف", FillWeight = 50 });
            dgPartners.Columns.Add(new DataGridViewTextBoxColumn { Name = "SharePercentage", HeaderText = "نسبة المساهمة %", FillWeight = 45 });
            dgPartners.Columns.Add(new DataGridViewTextBoxColumn { Name = "CapitalContribution", HeaderText = "رأس المال (ج)", FillWeight = 50 });
            dgPartners.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentBalance", HeaderText = "الرصيد الجاري الحالي (ج)", FillWeight = 55 });
            dgPartners.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "الحالة", FillWeight = 30 });

            tab.Controls.Add(dgPartners);
            tab.Controls.Add(pnlBottom);
            tab.Controls.Add(pnlTop);
        }

        private int GetSelectedPartnerID()
        {
            if (dgPartners.SelectedRows.Count > 0)
            {
                return Convert.ToInt32(dgPartners.SelectedRows[0].Cells["PartnerID"].Value);
            }
            return 0;
        }

        private void LoadPartnersGrid()
        {
            DataTable dt = ShareholdersDAL.GetAllPartners();
            dgPartners.Rows.Clear();

            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["PartnerID"]);
                string code = r["PartnerCode"].ToString();
                string name = r["PartnerName"].ToString();
                string phone = r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "";
                decimal pct = Convert.ToDecimal(r["SharePercentage"]);
                decimal cap = Convert.ToDecimal(r["CapitalContribution"]);
                decimal bal = Convert.ToDecimal(r["CurrentBalance"]);
                bool act = Convert.ToBoolean(r["IsActive"]);

                int rowIndex = dgPartners.Rows.Add(id, code, name, phone, $"{pct:F2}%", cap.ToString("N2"), bal.ToString("N2"), act ? "نشط" : "متوقف");

                if (bal > 0) dgPartners.Rows[rowIndex].Cells["CurrentBalance"].Style.ForeColor = Color.DarkGreen;
                else if (bal < 0) dgPartners.Rows[rowIndex].Cells["CurrentBalance"].Style.ForeColor = Color.Firebrick;
            }

            RefreshDashboard();
        }

        private void OpenPartnerEditor(int partnerID)
        {
            using (var dlg = new Form())
            {
                dlg.Text = partnerID > 0 ? "✏️ تعديل بيانات الشريك" : "➕ إضافة شريك مساهم جديد";
                dlg.Size = new Size(480, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes; dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain; dlg.Font = Theme.FontMain;

                int y = 15;
                dlg.Controls.Add(new Label { Text = "كود الشريك:", Location = new Point(360, y), AutoSize = true });
                var txtCode = new TextBox { Location = new Point(20, y + 20), Width = 420, Text = ShareholdersDAL.GeneratePartnerCode() };
                dlg.Controls.Add(txtCode);
                y += 55;

                dlg.Controls.Add(new Label { Text = "اسم الشريك / المساهم (*):", Location = new Point(270, y), AutoSize = true, Font = Theme.FontBold });
                var txtName = new TextBox { Location = new Point(20, y + 20), Width = 420, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
                dlg.Controls.Add(txtName);
                y += 55;

                dlg.Controls.Add(new Label { Text = "رقم الهاتف:", Location = new Point(360, y), AutoSize = true });
                var txtPhone = new TextBox { Location = new Point(20, y + 20), Width = 420 };
                dlg.Controls.Add(txtPhone);
                y += 55;

                dlg.Controls.Add(new Label { Text = "نسبة المساهمة في رأس المال (%):", Location = new Point(220, y), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.Primary });
                var nudPct = new NumericUpDown { Location = new Point(20, y + 20), Width = 420, Minimum = 0, Maximum = 100, DecimalPlaces = 3, Value = 10m, Font = new Font("Segoe UI", 11f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(nudPct);
                y += 55;

                dlg.Controls.Add(new Label { Text = "رأس المال المدفوع / المكتتب به (ج):", Location = new Point(210, y), AutoSize = true });
                var txtCap = new TextBox { Location = new Point(20, y + 20), Width = 420, Font = new Font("Segoe UI", 11f), Text = "0.00", TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(txtCap);
                y += 55;

                dlg.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(370, y), AutoSize = true });
                var txtNotes = new TextBox { Location = new Point(20, y + 20), Width = 420, Height = 40, Multiline = true };
                dlg.Controls.Add(txtNotes);
                y += 65;

                if (partnerID > 0)
                {
                    var row = ShareholdersDAL.GetPartnerByID(partnerID);
                    if (row != null)
                    {
                        txtCode.Text = row["PartnerCode"].ToString();
                        txtName.Text = row["PartnerName"].ToString();
                        txtPhone.Text = row["Phone"] != DBNull.Value ? row["Phone"].ToString() : "";
                        nudPct.Value = Convert.ToDecimal(row["SharePercentage"]);
                        txtCap.Text = Convert.ToDecimal(row["CapitalContribution"]).ToString("F2");
                        txtNotes.Text = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : "";
                    }
                }

                var btnSave = Theme.MakeButton("💾 حفظ بيانات الشريك", 230, y, 210, 38, Theme.Success);
                var btnCancel = Theme.MakeButton("إلغاء", 20, y, 100, 38, Color.FromArgb(100, 116, 139));
                btnCancel.Click += (s, e) => dlg.Close();

                btnSave.Click += (s, e) =>
                {
                    try
                    {
                        decimal.TryParse(txtCap.Text.Trim(), out decimal cap);
                        ShareholdersDAL.SavePartner(partnerID, txtCode.Text.Trim(), txtName.Text.Trim(), txtPhone.Text.Trim(),
                            "", nudPct.Value, cap, true, txtNotes.Text.Trim(), Session.EmpID, Session.GetDefaultSafeID());

                        MessageBox.Show("✅ تم حفظ بيانات الشريك بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                dlg.Controls.AddRange(new Control[] { btnSave, btnCancel });

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadPartnersGrid();
                    LoadStatementPartnersCombo();
                }
            }
        }

        private void OpenQuickTransDialog(string transType)
        {
            int pid = GetSelectedPartnerID();
            if (pid <= 0) return;

            var r = ShareholdersDAL.GetPartnerByID(pid);
            if (r == null) return;

            string title = transType == "CapitalDeposit" ? "💰 إيداع رأس مال للشريك" : "💸 تسجيل مسحوبات شخصية للشريك";

            using (var dlg = new Form())
            {
                dlg.Text = $"{title} [{r["PartnerName"]}]";
                dlg.Size = new Size(420, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes; dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain; dlg.Font = Theme.FontMain;

                dlg.Controls.Add(new Label { Text = "المبلغ (ج.م):", Location = new Point(300, 20), AutoSize = true, Font = Theme.FontBold });
                var txtAmt = new TextBox { Location = new Point(20, 42), Width = 360, Font = new Font("Segoe UI", 12f, FontStyle.Bold), Text = "0.00", TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(txtAmt);

                dlg.Controls.Add(new Label { Text = transType == "CapitalDeposit" ? "الخزينة المودع بها:" : "الخزينة المصروف منها:", Location = new Point(230, 80), AutoSize = true });
                var cboSafe = new ComboBox { Location = new Point(20, 102), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
                var dtSafes = DbHelper.Query("SELECT AccountID, AccountName FROM Accounts WHERE IsActive = 1");
                foreach (DataRow sr in dtSafes.Rows) cboSafe.Items.Add(new ComboItem(Convert.ToInt32(sr["AccountID"]), sr["AccountName"].ToString()));
                cboSafe.DisplayMember = "Text";
                if (cboSafe.Items.Count > 0) cboSafe.SelectedIndex = 0;
                dlg.Controls.Add(cboSafe);

                dlg.Controls.Add(new Label { Text = "البيان والملاحظات:", Location = new Point(270, 140), AutoSize = true });
                var txtNotes = new TextBox { Location = new Point(20, 162), Width = 360, Height = 45, Multiline = true };
                dlg.Controls.Add(txtNotes);

                var btnSave = Theme.MakeButton("✅ تنفيذ وتأثير الخزينة", 180, 225, 200, 36, Theme.Success);
                btnSave.Click += (s, ev) =>
                {
                    decimal.TryParse(txtAmt.Text.Trim(), out decimal amt);
                    int safeID = (cboSafe.SelectedItem is ComboItem ci) ? ci.ID : 1;

                    decimal dr = transType == "PersonalDrawing" ? amt : 0;
                    decimal cr = transType == "CapitalDeposit" ? amt : 0;

                    ShareholdersDAL.AddPartnerTransaction(pid, transType, dr, cr, txtNotes.Text.Trim(), safeID, null, Session.EmpID);
                    MessageBox.Show("✅ تم تسجيل الحركة بنجاح.", "تمت العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                dlg.Controls.Add(btnSave);

                if (dlg.ShowDialog(this) == DialogResult.OK) LoadPartnersGrid();
            }
        }

        private void BtnDeletePartner_Click(object sender, EventArgs e)
        {
            int pid = GetSelectedPartnerID();
            if (pid <= 0) return;

            if (MessageBox.Show("هل أنت متأكد من حذف هذا الشريك وسجل حركاته؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ShareholdersDAL.DeletePartner(pid);
                LoadPartnersGrid();
                LoadStatementPartnersCombo();
            }
        }

        // ══════════════════════════════════════════════════
        // تبويب 2: كشف حساب وحركات الشريك
        // ══════════════════════════════════════════════════
        private void BuildStatementTab(TabPage tab)
        {
            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10, 7, 10, 7),
                WrapContents = false,
                RightToLeft = RightToLeft.Yes
            };

            pnlTop.Controls.Add(new Label { Text = "👤 اختر الشريك:", AutoSize = true, Margin = new Padding(3, 7, 0, 0), Font = Theme.FontBold });
            cboStatementPartner = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 4, 10, 0) };
            cboStatementPartner.SelectedIndexChanged += (s, e) => LoadStatementGrid();
            pnlTop.Controls.Add(cboStatementPartner);

            pnlTop.Controls.Add(new Label { Text = "من:", AutoSize = true, Margin = new Padding(3, 7, 0, 0) });
            dtpFrom = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3), Margin = new Padding(3, 4, 10, 0) };
            pnlTop.Controls.Add(dtpFrom);

            pnlTop.Controls.Add(new Label { Text = "إلى:", AutoSize = true, Margin = new Padding(3, 7, 0, 0) });
            dtpTo = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(3, 4, 10, 0) };
            pnlTop.Controls.Add(dtpTo);

            btnRefreshStmt = Theme.MakeButton("🔄 عرض", 0, 0, 90, 30, Theme.Primary);
            btnRefreshStmt.Click += (s, e) => LoadStatementGrid();
            pnlTop.Controls.Add(btnRefreshStmt);

            btnAddTrans = Theme.MakeButton("➕ تسجيل حركة", 0, 0, 130, 30, Theme.Accent);
            btnAddTrans.Click += (s, e) => OpenQuickTransDialog("CapitalDeposit");
            pnlTop.Controls.Add(btnAddTrans);

            btnPrintStmt = Theme.MakeButton("🖨️ طباعة كشف الحساب", 0, 0, 160, 30, Theme.Secondary);
            btnPrintStmt.Click += BtnPrintStmt_Click;
            pnlTop.Controls.Add(btnPrintStmt);

            // لوحة الملخص السفلي
            var pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(10, 10, 10, 10),
                RightToLeft = RightToLeft.Yes
            };

            lblStmtCredit = new Label { Text = "إجمالي الدائن (له): 0.00 ج", AutoSize = true, Font = Theme.FontBold, ForeColor = Color.DarkGreen, Margin = new Padding(5, 0, 20, 0) };
            lblStmtDebit = new Label { Text = "إجمالي المدين (عليه): 0.00 ج", AutoSize = true, Font = Theme.FontBold, ForeColor = Color.Firebrick, Margin = new Padding(5, 0, 20, 0) };
            lblStmtBalance = new Label { Text = "الرصيد النهائي: 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.DarkBlue, Margin = new Padding(5, 0, 20, 0) };

            pnlSummary.Controls.AddRange(new Control[] { lblStmtCredit, lblStmtDebit, lblStmtBalance });

            dgStatement = new DataGridView
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
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ والوقت", FillWeight = 50 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع الحركة", FillWeight = 45 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "له / دائن (استحقاق/إيداع)", FillWeight = 50 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "عليه / مدين (مسحوبات/صرف)", FillWeight = 50 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "RunningBalance", HeaderText = "الرصيد التراكمي", FillWeight = 50 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "SafeName", HeaderText = "الخزينة", FillWeight = 40 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان والتفاصيل", FillWeight = 110 });

            tab.Controls.Add(dgStatement);
            tab.Controls.Add(pnlSummary);
            tab.Controls.Add(pnlTop);
        }

        private void LoadStatementPartnersCombo()
        {
            var dt = ShareholdersDAL.GetAllPartners();
            cboStatementPartner.Items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                cboStatementPartner.Items.Add(new ComboItem(Convert.ToInt32(r["PartnerID"]), r["PartnerName"].ToString()));
            }
            cboStatementPartner.DisplayMember = "Text";
            if (cboStatementPartner.Items.Count > 0) cboStatementPartner.SelectedIndex = 0;
        }

        private void SelectPartnerInStatement(int partnerID)
        {
            for (int i = 0; i < cboStatementPartner.Items.Count; i++)
            {
                if (cboStatementPartner.Items[i] is ComboItem ci && ci.ID == partnerID)
                {
                    cboStatementPartner.SelectedIndex = i;
                    break;
                }
            }
        }

        private void LoadStatementGrid()
        {
            if (!(cboStatementPartner.SelectedItem is ComboItem ci) || ci.ID <= 0) return;

            DataTable dt = ShareholdersDAL.GetPartnerStatement(ci.ID, dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1).AddSeconds(-1));
            dgStatement.Rows.Clear();

            decimal totCr = 0m, totDr = 0m;

            foreach (DataRow r in dt.Rows)
            {
                DateTime dtT = Convert.ToDateTime(r["TransDate"]);
                string type = r["TransType"].ToString();
                decimal cr = Convert.ToDecimal(r["Credit"]);
                decimal dr = Convert.ToDecimal(r["Debit"]);
                decimal run = Convert.ToDecimal(r["RunningBalance"]);
                string safe = r["SafeName"] != DBNull.Value ? r["SafeName"].ToString() : "—";
                string notes = r["Notes"] != DBNull.Value ? r["Notes"].ToString() : "";

                totCr += cr; totDr += dr;

                string typeAr = type;
                if (type == "CapitalDeposit") typeAr = "إيداع رأس مال";
                else if (type == "ProfitShare") typeAr = "استحقاق نصيب أرباح";
                else if (type == "PersonalDrawing") typeAr = "مسحوبات شخصية";
                else if (type == "DividendPayout") typeAr = "صرف أرباح نقدية";

                int rowIdx = dgStatement.Rows.Add(dtT.ToString("yyyy/MM/dd hh:mm tt"), typeAr,
                    cr > 0 ? cr.ToString("N2") : "—",
                    dr > 0 ? dr.ToString("N2") : "—",
                    run.ToString("N2"), safe, notes);

                if (cr > 0) dgStatement.Rows[rowIdx].Cells["Credit"].Style.ForeColor = Color.DarkGreen;
                if (dr > 0) dgStatement.Rows[rowIdx].Cells["Debit"].Style.ForeColor = Color.Firebrick;
            }

            decimal finalBal = totCr - totDr;
            lblStmtCredit.Text = $"إجمالي الدائن (له): {totCr:N2} ج";
            lblStmtDebit.Text = $"إجمالي المدين (عليه): {totDr:N2} ج";
            lblStmtBalance.Text = $"الرصيد النهائي: {finalBal:N2} ج";
        }

        // ══════════════════════════════════════════════════
        // تبويب 3: محرك احتساب وتوزيع وصرف الأرباح
        // ══════════════════════════════════════════════════
        private void BuildDividendsTab(TabPage tab)
        {
            var pnlRight = new Panel { Dock = DockStyle.Right, Width = 380, BackColor = Theme.BgCard, Padding = new Padding(15), AutoScroll = true };
            int y = 10;

            pnlRight.Controls.Add(new Label { Text = "📅 تحديد فترة احتساب الأرباح:", Location = new Point(190, y), AutoSize = true, Font = Theme.FontBold });
            y += 24;

            pnlRight.Controls.Add(new Label { Text = "من:", Location = new Point(340, y), AutoSize = true });
            dtpDivFrom = new DateTimePicker { Location = new Point(200, y), Width = 135, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, 1, 1) };
            pnlRight.Controls.Add(new Label { Text = "إلى:", Location = new Point(155, y), AutoSize = true });
            dtpDivTo = new DateTimePicker { Location = new Point(15, y), Width = 135, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnlRight.Controls.AddRange(new Control[] { dtpDivFrom, dtpDivTo });
            y += 38;

            btnCalculateProfit = Theme.MakeButton("⚡ جلب صافي أرباح المنشأة المحققة", 15, y, 350, 36, Theme.Primary);
            btnCalculateProfit.Click += (s, e) =>
            {
                decimal profit = ShareholdersDAL.CalculateBusinessNetProfit(dtpDivFrom.Value.Date, dtpDivTo.Value.Date.AddDays(1).AddSeconds(-1));
                txtNetProfit.Text = profit.ToString("F2");
                RecalculateDistributed();
            };
            pnlRight.Controls.Add(btnCalculateProfit);
            y += 48;

            pnlRight.Controls.Add(new Label { Text = "صافي أرباح المنشأة الإجمالي (ج):", Location = new Point(170, y), AutoSize = true });
            txtNetProfit = new TextBox { Location = new Point(15, y + 20), Width = 350, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = "0.00", TextAlign = HorizontalAlignment.Center };
            txtNetProfit.TextChanged += (s, e) => RecalculateDistributed();
            pnlRight.Controls.Add(txtNetProfit);
            y += 55;

            pnlRight.Controls.Add(new Label { Text = "نسبة الاحتجاز للتطوير والتوسعات (%):", Location = new Point(140, y), AutoSize = true });
            txtRetainedPct = new TextBox { Location = new Point(15, y + 20), Width = 350, Text = "0.0", TextAlign = HorizontalAlignment.Center };
            txtRetainedPct.TextChanged += (s, e) => RecalculateDistributed();
            pnlRight.Controls.Add(txtRetainedPct);
            y += 55;

            pnlRight.Controls.Add(new Label { Text = "💰 المبلغ الصافي الموزع على الشركاء (ج):", Location = new Point(120, y), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.Success });
            txtDistributedAmt = new TextBox { Location = new Point(15, y + 20), Width = 350, Font = new Font("Segoe UI", 12f, FontStyle.Bold), Text = "0.00", ForeColor = Color.DarkGreen, TextAlign = HorizontalAlignment.Center };
            pnlRight.Controls.Add(txtDistributedAmt);
            y += 58;

            pnlRight.Controls.Add(new Label { Text = "ملاحظات وبيان جلسة التوزيع:", Location = new Point(190, y), AutoSize = true });
            txtDivNotes = new TextBox { Location = new Point(15, y + 20), Width = 350, Height = 45, Multiline = true };
            pnlRight.Controls.Add(txtDivNotes);
            y += 72;

            btnPreviewDividends = Theme.MakeButton("⚡ احتساب نصيب كل شريك", 15, y, 350, 36, Color.FromArgb(79, 70, 229));
            btnPreviewDividends.Click += (s, e) => RunDividendsPreview();
            pnlRight.Controls.Add(btnPreviewDividends);

            // أزرار العمليات السفلية لجدول التوزيع
            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 7, 10, 7),
                RightToLeft = RightToLeft.Yes
            };

            btnPostDividends = Theme.MakeButton("💾 اعتماد وتوزيع الأرباح على حسابات الشركاء", 0, 0, 290, 32, Theme.Success);
            btnPostDividends.Click += BtnPostDividends_Click;

            btnDisburseDividends = Theme.MakeButton("💵 صرف أرباح نقدي للشريك من الخزينة", 0, 0, 240, 32, Theme.Primary);
            btnDisburseDividends.Click += BtnDisburseDividends_Click;

            btnPrintDivReport = Theme.MakeButton("🖨️ طباعة محضر التوزيع", 0, 0, 160, 32, Theme.Secondary);
            btnPrintDivReport.Click += BtnPrintDivReport_Click;

            pnlBottom.Controls.AddRange(new Control[] { btnPostDividends, btnDisburseDividends, btnPrintDivReport });

            dgDivPreview = new DataGridView
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
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgDivPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartnerID", Visible = false });
            dgDivPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartnerName", HeaderText = "اسم الشريك / المساهم", FillWeight = 90 });
            dgDivPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "SharePercentage", HeaderText = "نسبة الحصة %", FillWeight = 45 });
            dgDivPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "CalculatedProfit", HeaderText = "نصيب الأرباح المحتسب (ج)", FillWeight = 60 });
            dgDivPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "حالة الترحيل والسداد", FillWeight = 60 });

            tab.Controls.Add(dgDivPreview);
            tab.Controls.Add(pnlBottom);
            tab.Controls.Add(pnlRight);
        }

        private void RecalculateDistributed()
        {
            decimal.TryParse(txtNetProfit.Text.Trim(), out decimal net);
            decimal.TryParse(txtRetainedPct.Text.Trim(), out decimal ret);

            decimal retainedAmt = net * (ret / 100m);
            decimal dist = Math.Max(0m, net - retainedAmt);
            txtDistributedAmt.Text = dist.ToString("F2");
        }

        private void RunDividendsPreview()
        {
            decimal.TryParse(txtDistributedAmt.Text.Trim(), out decimal dist);
            if (dist <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ توزيع أرباح أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _currentDivList = ShareholdersDAL.PreviewDividendsDistribution(dist);
            dgDivPreview.Rows.Clear();

            foreach (var item in _currentDivList)
            {
                int r = dgDivPreview.Rows.Add(
                    item.PartnerID,
                    item.PartnerName,
                    $"{item.SharePercentage:F2}%",
                    item.CalculatedProfit.ToString("N2"),
                    "جاهز للترحيل"
                );
                dgDivPreview.Rows[r].Cells["CalculatedProfit"].Style.ForeColor = Color.DarkGreen;
                dgDivPreview.Rows[r].Cells["CalculatedProfit"].Style.Font = new Font(Theme.FontMain, FontStyle.Bold);
            }
        }

        private void BtnPostDividends_Click(object sender, EventArgs e)
        {
            if (_currentDivList.Count == 0) RunDividendsPreview();

            decimal.TryParse(txtNetProfit.Text.Trim(), out decimal net);
            decimal.TryParse(txtRetainedPct.Text.Trim(), out decimal ret);
            decimal.TryParse(txtDistributedAmt.Text.Trim(), out decimal dist);

            string msg = $"هل ترغب في اعتماد وترحيل توزيع الأرباح بمبلغ إجمالي [{dist:N2} ج] وإضافتها كأرصدة دائنة في كشوف حسابات جميع الشركاء؟";
            if (MessageBox.Show(msg, "تأكيد اعتماد التوزيع", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    int distID = ShareholdersDAL.PostDividendsDistribution(
                        dtpDivFrom.Value.Date, dtpDivTo.Value.Date, net, ret, dist, _currentDivList, txtDivNotes.Text.Trim(), Session.EmpID);

                    MessageBox.Show($"✅ تم اعتماد وترحيل توزيع الأرباح بنجاح برقم جلسة [{distID}].", "تم الاعتماد", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadPartnersGrid();
                    LoadStatementGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDisburseDividends_Click(object sender, EventArgs e)
        {
            if (dgDivPreview.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار الشريك من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pid = Convert.ToInt32(dgDivPreview.SelectedRows[0].Cells["PartnerID"].Value);
            string name = dgDivPreview.SelectedRows[0].Cells["PartnerName"].Value.ToString();
            decimal.TryParse(dgDivPreview.SelectedRows[0].Cells["CalculatedProfit"].Value.ToString(), out decimal profit);

            using (var dlg = new Form())
            {
                dlg.Text = $"💵 صرف أرباح نقدية للشريك [{name}]";
                dlg.Size = new Size(420, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes; dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain; dlg.Font = Theme.FontMain;

                dlg.Controls.Add(new Label { Text = "المبلغ المنصرف نقداً (ج.م):", Location = new Point(230, 20), AutoSize = true, Font = Theme.FontBold });
                var txtAmt = new TextBox { Location = new Point(20, 42), Width = 360, Font = new Font("Segoe UI", 12f, FontStyle.Bold), Text = profit.ToString("F2"), TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(txtAmt);

                dlg.Controls.Add(new Label { Text = "الخزينة المنصرف منها:", Location = new Point(260, 80), AutoSize = true });
                var cboSafe = new ComboBox { Location = new Point(20, 102), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
                var dtSafes = DbHelper.Query("SELECT AccountID, AccountName FROM Accounts WHERE IsActive = 1");
                foreach (DataRow sr in dtSafes.Rows) cboSafe.Items.Add(new ComboItem(Convert.ToInt32(sr["AccountID"]), sr["AccountName"].ToString()));
                cboSafe.DisplayMember = "Text";
                if (cboSafe.Items.Count > 0) cboSafe.SelectedIndex = 0;
                dlg.Controls.Add(cboSafe);

                dlg.Controls.Add(new Label { Text = "ملاحظات وبيان الصرف:", Location = new Point(260, 140), AutoSize = true });
                var txtNotes = new TextBox { Location = new Point(20, 162), Width = 360, Height = 45, Multiline = true, Text = $"صرف أرباح نقدية عن فترة {dtpDivFrom.Value:yyyy/MM/dd} إلى {dtpDivTo.Value:yyyy/MM/dd}" };
                dlg.Controls.Add(txtNotes);

                var btnSave = Theme.MakeButton("✅ صرف النقدية وإصدار السند", 170, 225, 210, 36, Theme.Success);
                btnSave.Click += (s, ev) =>
                {
                    decimal.TryParse(txtAmt.Text.Trim(), out decimal amt);
                    int safeID = (cboSafe.SelectedItem is ComboItem ci) ? ci.ID : 1;

                    ShareholdersDAL.DisbursePartnerDividends(0, pid, amt, safeID, txtNotes.Text.Trim(), Session.EmpID);
                    MessageBox.Show("✅ تم صرف الأرباح نقدياً والتأثير على الخزينة وحساب الشريك بنجاح.", "تم الصرف", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                dlg.Controls.Add(btnSave);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadPartnersGrid();
                    LoadStatementGrid();
                }
            }
        }

        // ══════════════════════════════════════════════════
        // تقارير الطباعة
        // ══════════════════════════════════════════════════
        private void BtnPrintPartners_Click(object sender, EventArgs e)
        {
            var dt = ShareholdersDAL.GetAllPartners();
            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 30;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);

                g.DrawString("سجل ودليل المساهمين وحصص رأس المال", fontTitle, Brushes.DarkSlateBlue, new PointF(ev.PageBounds.Width / 2 - 180, y));
                y += 35;
                g.DrawString($"تاريخ التقرير: {DateTime.Now:yyyy/MM/dd hh:mm tt}", fontBody, Brushes.Gray, new PointF(ev.PageBounds.Width / 2 - 100, y));
                y += 35;

                float[] colWidths = { 70, 180, 100, 80, 110, 110 };
                string[] headers = { "الكود", "اسم الشريك / المساهم", "الهاتف", "نسبة الحصة", "رأس المال", "الرصيد الجاري" };

                float x = 30;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightSteelBlue, x, y, colWidths[i], 24);
                    g.DrawRectangle(Pens.SlateGray, x, y, colWidths[i], 24);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 4, y + 3);
                    x += colWidths[i];
                }
                y += 24;

                decimal totCap = 0m, totBal = 0m;
                foreach (DataRow r in dt.Rows)
                {
                    if (y > ev.PageBounds.Height - 80) break;
                    x = 30;
                    decimal cap = Convert.ToDecimal(r["CapitalContribution"]);
                    decimal bal = Convert.ToDecimal(r["CurrentBalance"]);
                    totCap += cap; totBal += bal;

                    string[] vals = {
                        r["PartnerCode"].ToString(),
                        r["PartnerName"].ToString(),
                        r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "",
                        $"{Convert.ToDecimal(r["SharePercentage"]):F2}%",
                        $"{cap:N2} ج",
                        $"{bal:N2} ج"
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 20);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 4, y + 2);
                        x += colWidths[i];
                    }
                    y += 20;
                }

                y += 10;
                g.DrawString($"إجمالي رأس المال: {totCap:N2} ج  |  إجمالي الأرصدة الجارية للشركاء: {totBal:N2} ج", fontHeader, Brushes.DarkBlue, 30, y);
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 700 })
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnPrintStmt_Click(object sender, EventArgs e)
        {
            if (!(cboStatementPartner.SelectedItem is ComboItem ci) || ci.ID <= 0) return;

            var rPartner = ShareholdersDAL.GetPartnerByID(ci.ID);
            var dt = ShareholdersDAL.GetPartnerStatement(ci.ID, dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1).AddSeconds(-1));

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 30;
                var fontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);

                g.DrawString($"كشف حساب الشريك / المساهم [{ci.Text}]", fontTitle, Brushes.DarkSlateBlue, new PointF(ev.PageBounds.Width / 2 - 160, y));
                y += 30;
                g.DrawString($"الفترة من {dtpFrom.Value:yyyy/MM/dd} إلى {dtpTo.Value:yyyy/MM/dd}   |   الحصة: {rPartner["SharePercentage"]}%", fontBody, Brushes.Gray, new PointF(ev.PageBounds.Width / 2 - 140, y));
                y += 30;

                float[] colWidths = { 110, 110, 80, 80, 80, 200 };
                string[] headers = { "التاريخ", "نوع الحركة", "له (دائن)", "عليه (مدين)", "الرصيد", "البيان" };

                float x = 30;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightSteelBlue, x, y, colWidths[i], 24);
                    g.DrawRectangle(Pens.SlateGray, x, y, colWidths[i], 24);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 4, y + 3);
                    x += colWidths[i];
                }
                y += 24;

                foreach (DataRow r in dt.Rows)
                {
                    if (y > ev.PageBounds.Height - 80) break;
                    x = 30;
                    string type = r["TransType"].ToString();
                    decimal cr = Convert.ToDecimal(r["Credit"]);
                    decimal dr = Convert.ToDecimal(r["Debit"]);
                    decimal run = Convert.ToDecimal(r["RunningBalance"]);

                    string[] vals = {
                        Convert.ToDateTime(r["TransDate"]).ToString("yyyy/MM/dd"),
                        type,
                        cr > 0 ? $"{cr:N0}" : "—",
                        dr > 0 ? $"{dr:N0}" : "—",
                        $"{run:N0}",
                        r["Notes"] != DBNull.Value ? r["Notes"].ToString() : ""
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 20);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 4, y + 2);
                        x += colWidths[i];
                    }
                    y += 20;
                }
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 700 })
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnPrintDivReport_Click(object sender, EventArgs e)
        {
            if (_currentDivList.Count == 0) RunDividendsPreview();

            decimal.TryParse(txtDistributedAmt.Text.Trim(), out decimal dist);

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 30;
                var fontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);

                g.DrawString("محضر وكشف توزيع الأرباح على الشركاء", fontTitle, Brushes.DarkSlateBlue, new PointF(ev.PageBounds.Width / 2 - 160, y));
                y += 30;
                g.DrawString($"عن الفترة من {dtpDivFrom.Value:yyyy/MM/dd} إلى {dtpDivTo.Value:yyyy/MM/dd}   |   إجمالي الأرباح الموزعة: {dist:N2} ج", fontBody, Brushes.Black, new PointF(ev.PageBounds.Width / 2 - 180, y));
                y += 35;

                float[] colWidths = { 60, 200, 110, 140, 140 };
                string[] headers = { "م", "اسم الشريك / المساهم", "نسبة الحصة %", "نصيب الأرباح (ج)", "توقيع الاستلام" };

                float x = 30;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightSteelBlue, x, y, colWidths[i], 24);
                    g.DrawRectangle(Pens.SlateGray, x, y, colWidths[i], 24);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 4, y + 3);
                    x += colWidths[i];
                }
                y += 24;

                int seq = 1;
                foreach (var item in _currentDivList)
                {
                    if (y > ev.PageBounds.Height - 80) break;
                    x = 30;
                    string[] vals = {
                        (seq++).ToString(),
                        item.PartnerName,
                        $"{item.SharePercentage:F2}%",
                        $"{item.CalculatedProfit:N2} ج",
                        "..............."
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 22);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 4, y + 3);
                        x += colWidths[i];
                    }
                    y += 22;
                }

                y += 35;
                g.DrawString("اعتماد الإدارة المالية: .........................", fontHeader, Brushes.Black, 50, y);
                g.DrawString("توقيع المدير العام: .........................", fontHeader, Brushes.Black, 420, y);
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 700 })
            {
                dlg.ShowDialog(this);
            }
        }
    }
}

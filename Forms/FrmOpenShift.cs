using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة فتح وردية جديدة — تسجيل الرصيد الافتتاحي وتحديد الخزينة
    /// </summary>
    public class FrmOpenShift : Form
    {
        public int CreatedShiftID { get; private set; } = 0;

        private Label lblEmployee;
        private Label lblOpenTime;
        private TextBox txtOpeningCash;
        private ComboBox cboSafeAccount;
        private TextBox txtNotes;
        private Button btnStart;
        private Button btnCancel;

        public FrmOpenShift()
        {
            InitUI();
            LoadSafeAccounts();
        }

        private void InitUI()
        {
            this.Text = "🔓 فتح وردية جديدة - الخزينة والكاشير";
            this.Size = new Size(530, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.Primary,
                Padding = new Padding(15)
            };
            Label lblTitle = new Label
            {
                Text = "⚡ بدء وفتح وردية عمل جديدة",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 28
            };
            Label lblSub = new Label
            {
                Text = "قم بتسجيل النقدية الافتتاحية واختيار حساب الخزينة لبدء الوردية",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(220, 235, 252),
                Dock = DockStyle.Top,
                Height = 20
            };
            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Container Box
            TableLayoutPanel tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(20, 15, 20, 15),
                RightToLeft = RightToLeft.Yes
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 5; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

            // 1. الموظف / الكاشير
            Label lblEmpTitle = new Label { Text = "👤 الموظف / الكاشير:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            lblEmployee = new Label { Text = Session.EmpName, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Theme.Accent };
            tbl.Controls.Add(lblEmpTitle, 0, 0);
            tbl.Controls.Add(lblEmployee, 1, 0);

            // 2. تاريخ ووقت الفتح
            Label lblTimeTitle = new Label { Text = "📅 تاريخ ووقت الفتح:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            lblOpenTime = new Label { Text = DateTime.Now.ToString("yyyy-MM-dd   hh:mm tt"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Theme.TextMain };
            tbl.Controls.Add(lblTimeTitle, 0, 1);
            tbl.Controls.Add(lblOpenTime, 1, 1);

            // 3. الرصيد الافتتاحي (Opening Cash)
            Label lblCashTitle = new Label { Text = "💵 الرصيد الافتتاحي (ج):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            txtOpeningCash = new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 32,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Text = "0.00",
                TextAlign = HorizontalAlignment.Center,
                ReadOnly = false,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(16, 185, 129),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtOpeningCash.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                {
                    e.Handled = true;
                }
                if (e.KeyChar == '.' && (s as TextBox).Text.IndexOf('.') > -1)
                {
                    e.Handled = true;
                }
            };
            this.Shown += (s, e) => { txtOpeningCash?.Focus(); txtOpeningCash?.SelectAll(); };
            tbl.Controls.Add(lblCashTitle, 0, 2);
            tbl.Controls.Add(txtOpeningCash, 1, 2);

            // 4. الخزينة المرتبطة
            Label lblSafeTitle = new Label { Text = "🏦 الخزينة المرتبطة:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = Theme.FontBold, ForeColor = Theme.TextMain };
            cboSafeAccount = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                RightToLeft = RightToLeft.Yes
            };
            tbl.Controls.Add(lblSafeTitle, 0, 3);
            tbl.Controls.Add(cboSafeAccount, 1, 3);

            // 5. ملاحظات
            Label lblNotesTitle = new Label { Text = "📝 ملاحظات البدء:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = Theme.FontMain, ForeColor = Theme.TextMain };
            txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 28,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            tbl.Controls.Add(lblNotesTitle, 0, 4);
            tbl.Controls.Add(txtNotes, 1, 4);

            this.Controls.Add(tbl);

            // Footer Panel
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            btnStart = Theme.MakeButton("✅ فتح الوردية الآن", Theme.Success, new Point(0, 0), new Size(170, 40));
            btnCancel = Theme.MakeButton("❌ إلغاء", Color.FromArgb(100, 110, 125), new Point(0, 0), new Size(110, 40));

            btnStart.Click += BtnStart_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            FlowLayoutPanel flowFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };
            btnStart.Margin = new Padding(6, 0, 0, 0);
            btnCancel.Margin = new Padding(6, 0, 0, 0);
            flowFooter.Controls.Add(btnStart);
            flowFooter.Controls.Add(btnCancel);

            pnlFooter.Controls.Add(flowFooter);
            this.Controls.Add(pnlFooter);
        }

        private void LoadSafeAccounts()
        {
            try
            {
                DataTable safes = AccountDAL.GetActiveSafeAccounts();
                cboSafeAccount.Items.Clear();
                int selectedIdx = -1;
                int defaultSafeID = Session.DefaultSafeID ?? Session.GetDefaultSafeID();

                for (int i = 0; i < safes.Rows.Count; i++)
                {
                    DataRow r = safes.Rows[i];
                    int id = Convert.ToInt32(r["AccountID"]);
                    string name = r["AccountName"].ToString().Replace(" / الدرج", "").Replace("/ الدرج", "").Replace("/الدرج", "").Replace(" / درج", "").Trim();
                    var item = new ComboItem(id, name);
                    int added = cboSafeAccount.Items.Add(item);
                    if (id == defaultSafeID)
                    {
                        selectedIdx = added;
                    }
                }
                cboSafeAccount.DisplayMember = "Text";
                cboSafeAccount.SelectedIndexChanged += (s, e) => UpdateOpeningCashFromPreviousShift();
                if (cboSafeAccount.Items.Count > 0)
                {
                    cboSafeAccount.SelectedIndex = selectedIdx >= 0 ? selectedIdx : 0;
                    UpdateOpeningCashFromPreviousShift();
                }
            }
            catch { }
        }

        private void UpdateOpeningCashFromPreviousShift()
        {
            if (!(cboSafeAccount.SelectedItem is ComboItem safeItem) || safeItem.ID <= 0) return;
            try
            {
                // اعتماد الرصيد الفعلي الحالي المتاح في الخزينة المختارة مباشرة
                decimal actualSafeBalance = AccountDAL.GetCashBalance(safeItem.ID);

                // الرصيد الافتتاحي للخزنة لا يمكن أن يكون سالباً نهائياً
                if (actualSafeBalance < 0m) actualSafeBalance = 0m;
                txtOpeningCash.Text = actualSafeBalance.ToString("N2");
            }
            catch { }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtOpeningCash.Text.Trim(), out decimal openingCash) || openingCash < 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ الرصيد الافتتاحي بشكل صحيح (يجب أن يكون صفراً أو موجباً ولا يمكن أن يكون سالباً).", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtOpeningCash.Focus();
                return;
            }

            int safeAccountID = 0;
            if (cboSafeAccount.SelectedItem is ComboItem safeItem)
            {
                safeAccountID = safeItem.ID;
            }
            if (safeAccountID <= 0)
            {
                safeAccountID = Session.DefaultSafeID ?? Session.GetDefaultSafeID();
            }

            try
            {
                DbHelper.EnsureShiftSchema();
                decimal currentSafeBalance = AccountDAL.GetCashBalance(safeAccountID);
                if (currentSafeBalance < 0) currentSafeBalance = 0;

                int shiftID = DbHelper.ExecuteInsert(
                    @"INSERT INTO Shifts (ShiftDate, OpenTime, OpenedBy, OpeningCash, SafeAccountID, Status, Notes)
                      VALUES (CAST(GETDATE() AS DATE), GETDATE(), @emp, @cash, @safe, 'Open', @notes)",
                    DbHelper.P("@emp", Session.EmpID),
                    DbHelper.P("@cash", openingCash),
                    DbHelper.P("@safe", safeAccountID > 0 ? (object)safeAccountID : DBNull.Value),
                    DbHelper.P("@notes", txtNotes.Text.Trim()));

                if (shiftID > 0)
                {
                    Session.CurrentShiftID = shiftID;
                    if (safeAccountID > 0) Session.DefaultSafeID = safeAccountID;

                    // مزامنة وربط رصيد الخزينة بالوردية بدقة 100%
                    decimal diff = openingCash - currentSafeBalance;
                    if (diff > 0)
                    {
                        // تسوية زيادة لتطابق الخزينة النقدية الفعلية الافتتاحية
                        DbHelper.Execute(
                            @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy, RefID)
                              VALUES (GETDATE(), 'ShiftOpen', @amt, 0, @acc, @notes, @uid, @ref)",
                            DbHelper.P("@amt", diff),
                            DbHelper.P("@acc", safeAccountID),
                            DbHelper.P("@notes", $"تسوية رصيد الخزينة عند فتح الوردية #{shiftID} (رصيد افتتاحي: {openingCash:N2} ج)"),
                            DbHelper.P("@uid", Session.EmpID),
                            DbHelper.P("@ref", shiftID));
                    }
                    else if (diff < 0)
                    {
                        // تسوية تخفيض لتطابق الخزينة النقدية الفعلية الافتتاحية
                        DbHelper.Execute(
                            @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy, RefID)
                              VALUES (GETDATE(), 'ShiftOpen', 0, @amt, @acc, @notes, @uid, @ref)",
                            DbHelper.P("@amt", Math.Abs(diff)),
                            DbHelper.P("@acc", safeAccountID),
                            DbHelper.P("@notes", $"تسوية رصيد الخزينة عند فتح الوردية #{shiftID} (رصيد افتتاحي: {openingCash:N2} ج)"),
                            DbHelper.P("@uid", Session.EmpID),
                            DbHelper.P("@ref", shiftID));
                    }
                    else
                    {
                        // الرصيد مطابق - تسجيل حركة إثبات فتح الوردية
                        DbHelper.Execute(
                            @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy, RefID)
                              VALUES (GETDATE(), 'ShiftOpen', 0, 0, @acc, @notes, @uid, @ref)",
                            DbHelper.P("@acc", safeAccountID),
                            DbHelper.P("@notes", $"فتح وردية جديدة #{shiftID} - رصيد الخزينة المعتمد: {openingCash:N2} ج (الموظف: {Session.EmpName})"),
                            DbHelper.P("@uid", Session.EmpID),
                            DbHelper.P("@ref", shiftID));
                    }

                    CreatedShiftID = shiftID;
                    MessageBox.Show($"تم فتح الوردية رقم #{shiftID} بنجاح واعتماد رصيد الخزينة {openingCash:N2} ج.", "تمت العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmOpenShift.BtnStart_Click", ex);
                MessageBox.Show($"حدث خطأ أثناء فتح الوردية: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

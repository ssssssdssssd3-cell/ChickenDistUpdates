using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSafeAccounts : Form
    {
        private DataGridView dgAccounts;
        private TextBox txtName, txtNumber;
        private ComboBox cboType;
        private NumericUpDown nudOpening;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete;
        private int _selectedAccountID = 0;

        public FrmSafeAccounts()
        {
            if (!Session.IsAdmin && !Session.CanAccess("SafeAccounts"))
            {
                MessageBox.Show("⛔ غير مصرح لك بالدخول على إدارة الحسابات والخزائن.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }
            InitUI();
            LoadAccounts();
        }

        private void InitUI()
        {
            this.Text = "إدارة الحسابات والخزائن والفيزا";
            this.Size = new Size(900, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f)); // Column 0 (Right): Inputs
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f)); // Column 1 (Left): Grid
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Left: Grid (Column 1)
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            dgAccounts = new DataGridView
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
            dgAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountID", Visible = false });
            dgAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "اسم الحساب / الخزنة", FillWeight = 45 });
            dgAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountType", HeaderText = "النوع", FillWeight = 25 });
            dgAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountNumber", HeaderText = "رقم الحساب", FillWeight = 35 });
            dgAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "OpeningBalance", HeaderText = "الرصيد الافتتاحي", FillWeight = 30 });
            dgAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد الحالي", FillWeight = 35 });
            dgAccounts.SelectionChanged += DgAccounts_SelectionChanged;
            pnlGrid.Controls.Add(dgAccounts);

            // Right: Inputs (Column 0)
            var pnlFields = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(15) };
            
            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 260,
                ColumnCount = 2,
                RowCount = 5,
                RightToLeft = RightToLeft.Yes
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            for (int i = 0; i < 5; i++) tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

            // Row 0: Name
            tblFields.Controls.Add(new Label { Text = "اسم الحساب :", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right }, 0, 0);
            txtName = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 8, 0, 8) };
            tblFields.Controls.Add(txtName, 1, 0);

            // Row 1: Type
            tblFields.Controls.Add(new Label { Text = "نوع الحساب :", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right }, 0, 1);
            cboType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 8, 0, 8)
            };
            cboType.Items.AddRange(new object[] { "Cash", "Bank", "Visa" });
            cboType.SelectedIndex = 0;
            tblFields.Controls.Add(cboType, 1, 1);

            // Row 2: Account Number
            tblFields.Controls.Add(new Label { Text = "رقم الحساب :", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right }, 0, 2);
            txtNumber = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 8, 0, 8) };
            tblFields.Controls.Add(txtNumber, 1, 2);

            // Row 3: Opening Balance
            tblFields.Controls.Add(new Label { Text = "رصيد افتتاحي :", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right }, 0, 3);
            nudOpening = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = -9999999,
                Maximum = 9999999,
                DecimalPlaces = 2,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(0, 8, 0, 8)
            };
            tblFields.Controls.Add(nudOpening, 1, 3);

            // Row 4: Is Active
            chkActive = new CheckBox
            {
                Text = "الحساب نشط ويقبل العمليات",
                ForeColor = Theme.TextMain,
                Checked = true,
                Margin = new Padding(0, 10, 0, 0),
                Dock = DockStyle.Fill
            };
            tblFields.Controls.Add(chkActive, 1, 4);

            pnlFields.Controls.Add(tblFields);

            // Buttons
            bool canAdd = Session.IsAdmin || Session.CanAdd("SafeAccounts");
            bool canEdit = Session.IsAdmin || Session.CanEdit("SafeAccounts");
            bool canDelete = Session.IsAdmin || Session.CanDelete("SafeAccounts");

            btnNew = Theme.MakeButton("🆕 جديد", 20, 280, 80, 38, Color.FromArgb(60, 100, 60));
            btnNew.Visible = canAdd;
            btnNew.Click += (s, e) => ClearFields();

            btnSave = Theme.MakeButton("💾 حفظ الحساب", 110, 280, 110, 38, Theme.Accent);
            btnSave.Visible = canAdd || canEdit;
            btnSave.Click += BtnSave_Click;

            btnDelete = Theme.MakeButton("🗑 حذف", 230, 280, 80, 38, Color.FromArgb(140, 40, 40));
            btnDelete.Visible = canDelete;
            btnDelete.Click += BtnDelete_Click;

            pnlFields.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });

            if (!canAdd && !canEdit)
            {
                txtName.ReadOnly = true;
                txtNumber.ReadOnly = true;
                cboType.Enabled = false;
                nudOpening.Enabled = false;
                chkActive.Enabled = false;
            }

            tbl.Controls.Add(pnlFields, 0, 0); // Right
            tbl.Controls.Add(pnlGrid, 1, 0);   // Left
            this.Controls.Add(tbl);

            Theme.ApplyFormRTL(this);
        }

        private void LoadAccounts()
        {
            dgAccounts.Rows.Clear();
            DataTable dt = DbHelper.Query(@"
                SELECT sa.AccountID, sa.AccountName, sa.AccountType, sa.AccountNumber, sa.OpeningBalance,
                       (sa.OpeningBalance + ISNULL((SELECT SUM(cb.AmountIn) - SUM(cb.AmountOut) FROM CashBox cb WHERE cb.AccountID = sa.AccountID), 0)) AS Balance,
                       sa.IsActive
                FROM SafeAccounts sa
                ORDER BY sa.AccountID");

            foreach (DataRow r in dt.Rows)
            {
                string typeAr = r["AccountType"].ToString() switch
                {
                    "Cash" => "خزينة نقدية",
                    "Bank" => "حساب بنكي",
                    "Visa" => "فيزا",
                    _ => r["AccountType"].ToString()
                };

                string activeIndicator = Convert.ToBoolean(r["IsActive"]) ? "" : " (معطل)";
                var rowIdx = dgAccounts.Rows.Add(
                    r["AccountID"],
                    r["AccountName"].ToString() + activeIndicator,
                    typeAr,
                    r["AccountNumber"] != DBNull.Value ? r["AccountNumber"].ToString() : "",
                    Convert.ToDecimal(r["OpeningBalance"]).ToString("N2"),
                    Convert.ToDecimal(r["Balance"]).ToString("N2")
                );

                if (!Convert.ToBoolean(r["IsActive"]))
                {
                    dgAccounts.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
                }
            }
        }

        private void DgAccounts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgAccounts.SelectedRows.Count == 0) return;
            var row = dgAccounts.SelectedRows[0];
            _selectedAccountID = Convert.ToInt32(row.Cells["AccountID"].Value);

            // Fetch record details
            DataTable dt = DbHelper.Query("SELECT AccountName, AccountType, AccountNumber, OpeningBalance, IsActive FROM SafeAccounts WHERE AccountID=@id", DbHelper.P("@id", _selectedAccountID));
            if (dt.Rows.Count > 0)
            {
                var r = dt.Rows[0];
                txtName.Text = r["AccountName"].ToString();
                cboType.Text = r["AccountType"].ToString();
                txtNumber.Text = r["AccountNumber"] != DBNull.Value ? r["AccountNumber"].ToString() : "";
                nudOpening.Value = Convert.ToDecimal(r["OpeningBalance"]);
                chkActive.Checked = Convert.ToBoolean(r["IsActive"]);

                bool canEdit = Session.IsAdmin || Session.CanEdit("SafeAccounts");
                bool canDelete = Session.IsAdmin || Session.CanDelete("SafeAccounts");

                txtName.ReadOnly = !canEdit;
                txtNumber.ReadOnly = !canEdit;
                nudOpening.Enabled = canEdit;

                // If it is the default Cash Safe (ID=1), prevent editing its type or disabling it
                if (_selectedAccountID == 1)
                {
                    cboType.Enabled = false;
                    chkActive.Enabled = false;
                    btnDelete.Enabled = false;
                }
                else
                {
                    cboType.Enabled = canEdit;
                    chkActive.Enabled = canEdit;
                    btnDelete.Enabled = canDelete;
                }
            }
        }

        private void ClearFields()
        {
            if (!Session.IsAdmin && !Session.CanAdd("SafeAccounts"))
            {
                MessageBox.Show("⛔ غير مصرح لك بإضافة خزائن أو حسابات جديدة.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedAccountID = 0;
            txtName.Clear();
            txtName.ReadOnly = false;
            cboType.SelectedIndex = 0;
            cboType.Enabled = true;
            txtNumber.Clear();
            txtNumber.ReadOnly = false;
            nudOpening.Value = 0;
            nudOpening.Enabled = true;
            chkActive.Checked = true;
            chkActive.Enabled = true;
            btnDelete.Enabled = false;
            txtName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_selectedAccountID == 0)
            {
                if (!Session.IsAdmin && !Session.CanAdd("SafeAccounts"))
                {
                    MessageBox.Show("⛔ غير مصرح لك بإضافة خزينة أو حساب جديد.", "رفض العملية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                if (!Session.IsAdmin && !Session.CanEdit("SafeAccounts"))
                {
                    MessageBox.Show("⛔ غير مصرح لك بتعديل بيانات الخزينة أو الحساب.", "رفض العملية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string name = txtName.Text.Trim();
            string type = cboType.Text;
            string number = string.IsNullOrWhiteSpace(txtNumber.Text) ? null : txtNumber.Text.Trim();
            decimal opening = nudOpening.Value;
            bool active = chkActive.Checked;

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("يرجى إدخال اسم الحساب / الخزنة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_selectedAccountID == 0)
                {
                    // Verify name uniqueness
                    var exists = DbHelper.Scalar("SELECT COUNT(*) FROM SafeAccounts WHERE AccountName=@n", DbHelper.P("@n", name));
                    if (exists != null && Convert.ToInt32(exists) > 0)
                    {
                        MessageBox.Show("اسم الحساب مسجل بالفعل! يرجى اختيار اسم فريد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DbHelper.Execute(
                        @"INSERT INTO SafeAccounts(AccountName, AccountType, AccountNumber, OpeningBalance, IsActive)
                          VALUES(@n, @t, @num, @op, @act)",
                        DbHelper.P("@n", name),
                        DbHelper.P("@t", type),
                        DbHelper.P("@num", number),
                        DbHelper.P("@op", opening),
                        DbHelper.P("@act", active));

                    MessageBox.Show("✅ تم إضافة الحساب بنجاح!");
                }
                else
                {
                    // Verify name uniqueness for others
                    var exists = DbHelper.Scalar("SELECT COUNT(*) FROM SafeAccounts WHERE AccountName=@n AND AccountID<>@id", DbHelper.P("@n", name), DbHelper.P("@id", _selectedAccountID));
                    if (exists != null && Convert.ToInt32(exists) > 0)
                    {
                        MessageBox.Show("اسم الحساب مسجل لحساب آخر! يرجى اختيار اسم فريد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DbHelper.Execute(
                        @"UPDATE SafeAccounts
                          SET AccountName=@n, AccountType=@t, AccountNumber=@num, OpeningBalance=@op, IsActive=@act
                          WHERE AccountID=@id",
                        DbHelper.P("@n", name),
                        DbHelper.P("@t", type),
                        DbHelper.P("@num", number),
                        DbHelper.P("@op", opening),
                        DbHelper.P("@act", active),
                        DbHelper.P("@id", _selectedAccountID));

                    MessageBox.Show("✅ تم تحديث بيانات الحساب بنجاح!");
                }

                ClearFields();
                LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ فشل الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedAccountID == 0) return;
            if (!Session.IsAdmin && !Session.CanDelete("SafeAccounts"))
            {
                MessageBox.Show("⛔ غير مصرح لك بحذف الخزائن أو الحسابات.", "رفض العملية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedAccountID == 1)
            {
                MessageBox.Show("❌ خطأ: يمنع تماماً حذف الخزينة الرئيسية للنظام.", "حظر عملية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show("⚠️ هل أنت متأكد من حذف هذا الحساب نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    // Check if account has transactions in CashBox
                    var hasTrans = DbHelper.Scalar("SELECT COUNT(*) FROM CashBox WHERE AccountID=@id", DbHelper.P("@id", _selectedAccountID));
                    if (hasTrans != null && Convert.ToInt32(hasTrans) > 0)
                    {
                        MessageBox.Show("❌ لا يمكن حذف هذا الحساب لوجود حركات مالية مسجلة عليه.\n\nيرجى إلغاء تفعيله (جعله غير نشط) بدلاً من الحذف لتجنب الأخطاء المحاسبية.", "حظر عملية الحذف", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        return;
                    }

                    // Check if account has transactions in Expenses
                    var hasExp = DbHelper.Scalar("SELECT COUNT(*) FROM Expenses WHERE SafeAccountID=@id", DbHelper.P("@id", _selectedAccountID));
                    if (hasExp != null && Convert.ToInt32(hasExp) > 0)
                    {
                        MessageBox.Show("❌ لا يمكن حذف هذا الحساب لوجود مصروفات مسجلة عليه.\n\nيرجى إلغاء تفعيله (جعله غير نشط) بدلاً من الحذف لتجنب الأخطاء المحاسبية.", "حظر عملية الحذف", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        return;
                    }

                    DbHelper.Execute("DELETE FROM SafeAccounts WHERE AccountID=@id", DbHelper.P("@id", _selectedAccountID));
                    MessageBox.Show("✅ تم حذف الحساب بنجاح.");
                    ClearFields();
                    LoadAccounts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل الحذف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

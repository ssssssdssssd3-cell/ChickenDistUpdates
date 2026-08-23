using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة منبثقة سريعة لاختيار ماكينة أو حساب الفيزا / البنك عند إتمام عملية البيع
    /// </summary>
    public class FrmSelectVisaAccount : Form
    {
        public int SelectedAccountID { get; private set; } = 0;
        public string SelectedAccountName { get; private set; } = "";

        private decimal _amount;
        private int? _preselectedID;
        private ComboBox cboAccounts;
        private FlowLayoutPanel flowAccounts;
        private Button btnOk;
        private Button btnCancel;
        private Button btnAddNew;

        public FrmSelectVisaAccount(decimal amount, int? preselectedID = null)
        {
            _amount = amount;
            _preselectedID = preselectedID;
            InitUI();
            LoadAccounts();
        }

        private void InitUI()
        {
            this.Text = "💳 اختيار ماكينة / حساب الفيزا";
            this.Size = new Size(500, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.KeyPreview = true;

            // Top Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(41, 25, 75), // Visa Purple Theme
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "💳 تحصيل السداد عبر الفيزا / الدفع الإلكتروني",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleRight
            };

            var lblAmount = new Label
            {
                Text = $"المبلغ المطلوب تحصيله: {_amount:N2} ج",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 196, 15), // Gold Accent
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };

            var lblSub = new Label
            {
                Text = "اضغط على رقم الماكينة أو انقر على الزر المطلوب لإتمام الحركة فوراً",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(215, 205, 235),
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblAmount);
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Bottom Actions Bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnOk = Theme.MakeButton("✅ تأكيد الحساب (Enter)", 270, 8, 195, 38, Color.FromArgb(39, 174, 96));
            btnOk.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnOk.Click += (s, e) => ConfirmSelection();

            btnCancel = Theme.MakeButton("❌ إلغاء (Esc)", 15, 8, 120, 38, Theme.Danger);
            btnCancel.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlBottom.Controls.Add(btnOk);
            pnlBottom.Controls.Add(btnCancel);
            this.Controls.Add(pnlBottom);

            // Center Panel with Flow & Dropdown
            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 8, 15, 8),
                BackColor = Theme.BgMain
            };

            var lblSelectPrompt = new Label
            {
                Text = "⚡ اختر الماكينة / الحساب المطلوب:",
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleRight
            };

            flowAccounts = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(2),
                Margin = new Padding(0),
                RightToLeft = RightToLeft.Yes
            };

            // Combo selection row
            var pnlComboRow = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.Transparent
            };

            cboAccounts = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                RightToLeft = RightToLeft.Yes
            };
            cboAccounts.SelectedIndexChanged += CboAccounts_SelectedIndexChanged;

            btnAddNew = new Button
            {
                Text = "➕ ماكينة جديدة",
                Dock = DockStyle.Left,
                Width = 115,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0),
                Visible = Session.IsAdmin || (Session.CanAccess("SafeAccounts") && Session.CanAdd("SafeAccounts"))
            };
            btnAddNew.FlatAppearance.BorderSize = 0;
            btnAddNew.Click += (s, e) =>
            {
                if (!Session.IsAdmin && !Session.CanAdd("SafeAccounts"))
                {
                    MessageBox.Show("⛔ غير مصرح لك بإضافة ماكينات فيزا أو خزائن جديدة.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using (var frm = new FrmSafeAccounts())
                {
                    frm.ShowDialog(this);
                }
                LoadAccounts();
            };

            pnlComboRow.Controls.Add(cboAccounts);
            pnlComboRow.Controls.Add(btnAddNew);

            pnlCenter.Controls.Add(flowAccounts);
            pnlCenter.Controls.Add(pnlComboRow);
            pnlCenter.Controls.Add(lblSelectPrompt);

            this.Controls.Add(pnlCenter);
            pnlCenter.BringToFront();

            this.KeyDown += FrmSelectVisaAccount_KeyDown;
        }

        private void LoadAccounts()
        {
            flowAccounts.Controls.Clear();
            cboAccounts.Items.Clear();

            DataTable dt = AccountDAL.GetActiveVisaAccounts();
            if (dt.Rows.Count == 0)
            {
                DbHelper.Execute(@"
                    IF NOT EXISTS (SELECT 1 FROM SafeAccounts WHERE AccountType = 'Visa')
                    BEGIN
                        INSERT INTO SafeAccounts (AccountName, AccountType, AccountNumber, OpeningBalance, IsActive)
                        VALUES (N'ماكينة فيزا 1', N'Visa', N'VISA-01', 0, 1);
                    END");
                dt = AccountDAL.GetActiveVisaAccounts();
            }

            int index = 1;
            int preselectIdx = -1;

            foreach (DataRow r in dt.Rows)
            {
                int accId = Convert.ToInt32(r["AccountID"]);
                string accName = r["AccountName"].ToString();
                string accNum = r["AccountNumber"] != DBNull.Value ? r["AccountNumber"].ToString() : "";

                var item = new ComboItem(accId, accName);
                int cIdx = cboAccounts.Items.Add(item);

                if (_preselectedID.HasValue && accId == _preselectedID.Value)
                {
                    preselectIdx = cIdx;
                }

                int shortcutKey = index <= 9 ? index : 0;
                string shortcutPrefix = shortcutKey > 0 ? $"[{shortcutKey}] " : "• ";
                string displayNum = !string.IsNullOrWhiteSpace(accNum) ? $" ({accNum})" : "";

                var btnCard = new Button
                {
                    Text = $"{shortcutPrefix}💳 {accName}{displayNum}",
                    Height = 42,
                    Width = 430,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(53, 44, 78),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleRight,
                    Padding = new Padding(12, 0, 12, 0),
                    Cursor = Cursors.Hand,
                    Tag = accId,
                    Margin = new Padding(0, 2, 0, 4)
                };
                btnCard.FlatAppearance.BorderSize = 1;
                btnCard.FlatAppearance.BorderColor = Color.FromArgb(142, 68, 173);

                btnCard.MouseEnter += (s, e) => {
                    var b = (Button)s;
                    b.BackColor = Color.FromArgb(142, 68, 173);
                };
                btnCard.MouseLeave += (s, e) => {
                    var b = (Button)s;
                    b.BackColor = (SelectedAccountID == (int)b.Tag) ? Color.FromArgb(142, 68, 173) : Color.FromArgb(53, 44, 78);
                };

                btnCard.Click += (s, e) =>
                {
                    var b = (Button)s;
                    int id = (int)b.Tag;
                    SelectAndConfirm(id, accName);
                };

                flowAccounts.Controls.Add(btnCard);
                index++;
            }

            cboAccounts.DisplayMember = "Text";
            if (cboAccounts.Items.Count > 0)
            {
                cboAccounts.SelectedIndex = preselectIdx >= 0 ? preselectIdx : 0;
            }
        }

        private void CboAccounts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboAccounts.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                SelectedAccountID = ci.ID;
                SelectedAccountName = ci.Text;

                foreach (Control c in flowAccounts.Controls)
                {
                    if (c is Button b && b.Tag is int bId)
                    {
                        if (bId == ci.ID)
                        {
                            b.BackColor = Color.FromArgb(142, 68, 173);
                            b.FlatAppearance.BorderSize = 2;
                        }
                        else
                        {
                            b.BackColor = Color.FromArgb(53, 44, 78);
                            b.FlatAppearance.BorderSize = 1;
                        }
                    }
                }
            }
        }

        private void SelectAndConfirm(int accId, string accName)
        {
            SelectedAccountID = accId;
            SelectedAccountName = accName;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ConfirmSelection()
        {
            if (cboAccounts.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                SelectedAccountID = ci.ID;
                SelectedAccountName = ci.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("يرجى اختيار ماكينة / حساب الفيزا أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void FrmSelectVisaAccount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
                return;
            }

            // Numeric shortcuts 1-9 for quick selection
            int numPressed = -1;
            if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
            {
                numPressed = e.KeyCode - Keys.D1 + 1;
            }
            else if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9)
            {
                numPressed = e.KeyCode - Keys.NumPad1 + 1;
            }

            if (numPressed > 0 && numPressed <= flowAccounts.Controls.Count)
            {
                var btn = flowAccounts.Controls[numPressed - 1] as Button;
                if (btn != null && btn.Tag is int id)
                {
                    SelectAndConfirm(id, btn.Text);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// دالة سريعة لاختيار حساب الفيزا إن وُجد أكثر من حساب، وإرجاع المختار مباشرة.
        /// </summary>
        public static bool SelectVisaAccount(IWin32Window owner, decimal amount, int? preselectedID, out int chosenID, out string chosenName)
        {
            try
            {
                var dt = AccountDAL.GetActiveVisaAccounts();
                if (dt.Rows.Count == 0)
                {
                    chosenID = 0;
                    chosenName = "";
                    return true;
                }

                // لو في حساب فيزا واحد فقط مسجل في النظام، نأخذه تلقائياً فوراً دون تعطيل المستخدم
                if (dt.Rows.Count == 1)
                {
                    chosenID = Convert.ToInt32(dt.Rows[0]["AccountID"]);
                    chosenName = dt.Rows[0]["AccountName"].ToString();
                    return true;
                }

                // لو الحساب المحدد مسبقاً هو حساب فيزا ساري، نأخذه مباشرة
                if (preselectedID.HasValue && preselectedID.Value > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        if (Convert.ToInt32(r["AccountID"]) == preselectedID.Value)
                        {
                            chosenID = preselectedID.Value;
                            chosenName = r["AccountName"].ToString();
                            return true;
                        }
                    }
                }

                using (var frm = new FrmSelectVisaAccount(amount, preselectedID))
                {
                    if (frm.ShowDialog(owner) == DialogResult.OK && frm.SelectedAccountID > 0)
                    {
                        chosenID = frm.SelectedAccountID;
                        chosenName = frm.SelectedAccountName;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmSelectVisaAccount.SelectVisaAccount", ex);
            }

            chosenID = 0;
            chosenName = "";
            return false;
        }
    }
}

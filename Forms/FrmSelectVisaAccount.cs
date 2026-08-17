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
            this.Size = new Size(500, 440);
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
                Height = 85,
                BackColor = Color.FromArgb(41, 25, 75), // Visa Purple Theme
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblTitle = new Label
            {
                Text = "💳 تحصيل السداد عبر الفيزا / الدفع الإلكتروني",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 26
            };

            var lblAmount = new Label
            {
                Text = $"المبلغ المطلوب تحصيله: {_amount:N2} ج",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 196, 15), // Gold Accent
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblSub = new Label
            {
                Text = "اضغط على رقم الماكينة أو انقر على الزر المطلوب لإتمام الحركة فوراً",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(215, 205, 235),
                Dock = DockStyle.Top,
                Height = 18
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblAmount);
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Center Panel with Flow & Dropdown
            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblSelectPrompt = new Label
            {
                Text = "⚡ اختر الماكينة / الحساب المطلوب:",
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 24
            };
            pnlCenter.Controls.Add(lblSelectPrompt);

            flowAccounts = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 5)
            };
            pnlCenter.Controls.Add(flowAccounts);

            // Combo selection row
            var pnlComboRow = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                Padding = new Padding(0, 5, 0, 5)
            };

            cboAccounts = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
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
                Margin = new Padding(5, 0, 0, 0)
            };
            btnAddNew.FlatAppearance.BorderSize = 0;
            btnAddNew.Click += (s, e) =>
            {
                using (var frm = new FrmSafeAccounts())
                {
                    frm.ShowDialog(this);
                }
                LoadAccounts();
            };

            pnlComboRow.Controls.Add(cboAccounts);
            pnlComboRow.Controls.Add(btnAddNew);
            pnlCenter.Controls.Add(pnlComboRow);

            this.Controls.Add(pnlCenter);

            // Bottom Actions Bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
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

            this.KeyDown += FrmSelectVisaAccount_KeyDown;
        }

        private void LoadAccounts()
        {
            flowAccounts.Controls.Clear();
            cboAccounts.Items.Clear();

            DataTable dt = AccountDAL.GetActiveVisaAccounts();
            if (dt.Rows.Count == 0)
            {
                // Auto seed default Visa if none
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
                string accType = r["AccountType"].ToString();

                var item = new ComboItem(accId, accName);
                int cIdx = cboAccounts.Items.Add(item);

                if (_preselectedID.HasValue && accId == _preselectedID.Value)
                {
                    preselectIdx = cIdx;
                }

                // Create Quick Card Button
                int shortcutKey = index <= 9 ? index : 0;
                string shortcutPrefix = shortcutKey > 0 ? $"[{shortcutKey}] " : "• ";
                string displayNum = !string.IsNullOrWhiteSpace(accNum) ? $" ({accNum})" : "";

                var btnCard = new Button
                {
                    Text = $"{shortcutPrefix}💳 {accName}{displayNum}",
                    Height = 44,
                    Width = 440,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(53, 44, 78),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleRight,
                    Padding = new Padding(12, 0, 12, 0),
                    Cursor = Cursors.Hand,
                    Tag = accId,
                    Margin = new Padding(0, 3, 0, 3)
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

            // Numeric keys 1..9 for instant fast selection
            int numKey = -1;
            if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) numKey = (e.KeyCode - Keys.D1);
            else if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) numKey = (e.KeyCode - Keys.NumPad1);

            if (numKey >= 0 && numKey < cboAccounts.Items.Count)
            {
                cboAccounts.SelectedIndex = numKey;
                if (cboAccounts.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    SelectAndConfirm(ci.ID, ci.Text);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// استدعاء سريع لنافذة اختيار حساب الفيزا
        /// </summary>
        public static bool SelectVisaAccount(IWin32Window owner, decimal amount, int? preselectedID, out int chosenID, out string chosenName)
        {
            try
            {
                var dt = AccountDAL.GetActiveVisaAccounts();
                if (dt.Rows.Count == 1 && (!preselectedID.HasValue || preselectedID.Value <= 0 || preselectedID.Value == Convert.ToInt32(dt.Rows[0]["AccountID"])))
                {
                    // لو في حساب فيزا واحد فقط مسجل في النظام، نأخذه تلقائياً مع إمكانية التأكيد
                    chosenID = Convert.ToInt32(dt.Rows[0]["AccountID"]);
                    chosenName = dt.Rows[0]["AccountName"].ToString();
                    return true;
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

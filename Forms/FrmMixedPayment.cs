using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة السداد المختلط (جزء نقدي + جزء فيزا / إلكتروني)
    /// </summary>
    public class FrmMixedPayment : Form
    {
        private readonly decimal _totalAmount;
        private readonly bool _hasClient;
        private readonly int? _defaultSafeID;

        private TextBox txtCashPaid;
        private TextBox txtVisaPaid;
        private ComboBox cboVisaAccount;
        private ComboBox cboSafeAccount;
        private Label lblTotal;
        private Label lblTotalPaid;
        private Label lblRemaining;
        private Button btnOk;
        private Button btnCancel;
        private bool _isUpdatingText = false;

        public decimal CashPaid { get; private set; }
        public decimal VisaPaid { get; private set; }
        public int VisaAccountID { get; private set; }
        public string VisaAccountName { get; private set; } = "";
        public int SafeAccountID { get; private set; }

        public FrmMixedPayment(decimal totalAmount, bool hasClient, int? defaultSafeID = null)
        {
            _totalAmount = Math.Max(0m, totalAmount);
            _hasClient = hasClient;
            _defaultSafeID = defaultSafeID;

            InitUI();
            LoadAccounts();
            RecalculateTotals();
        }

        private void InitUI()
        {
            this.Text = "💳💵 سداد مختلط (نقدي + فيزا)";
            this.Size = new Size(460, 410);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;

            // Header Banner
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "💳💵 توزيع السداد (جزء نقدي + جزء فيزا)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleRight
            };

            lblTotal = new Label
            {
                Text = $"إجمالي الفاتورة المطلوب: {_totalAmount:N2} ج",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 196, 15), // Gold Accent
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlHeader.Controls.Add(lblTotal);
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Body Panel
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 10),
                BackColor = Theme.BgMain
            };

            int curY = 15;

            // 1. Cash Amount
            var lblCash = new Label
            {
                Text = "💵 المبلغ المدفوع نقداً (كاش):",
                Location = new Point(240, curY + 4),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };
            txtCashPaid = new TextBox
            {
                Location = new Point(20, curY),
                Width = 200,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Color.FromArgb(16, 185, 129),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center,
                Text = "0.00"
            };
            txtCashPaid.TextChanged += (s, e) =>
            {
                if (_isUpdatingText) return;
                _isUpdatingText = true;
                if (decimal.TryParse(txtCashPaid.Text.Trim(), out decimal cp))
                {
                    decimal rem = Math.Max(0m, _totalAmount - cp);
                    txtVisaPaid.Text = rem.ToString("F2");
                }
                _isUpdatingText = false;
                RecalculateTotals();
            };
            pnlBody.Controls.AddRange(new Control[] { lblCash, txtCashPaid });

            curY += 45;

            // 2. Safe Account (Cash Drawer)
            var lblSafe = new Label
            {
                Text = "🏦 خزينة الاستلام (الدرج):",
                Location = new Point(240, curY + 4),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextSub
            };
            cboSafeAccount = new ComboBox
            {
                Location = new Point(20, curY),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlBody.Controls.AddRange(new Control[] { lblSafe, cboSafeAccount });

            curY += 45;

            // 3. Visa Amount
            var lblVisa = new Label
            {
                Text = "💳 المبلغ المدفوع فيزا (إلكتروني):",
                Location = new Point(240, curY + 4),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };
            txtVisaPaid = new TextBox
            {
                Location = new Point(20, curY),
                Width = 200,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Color.FromArgb(142, 68, 173),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center,
                Text = _totalAmount.ToString("F2")
            };
            txtVisaPaid.TextChanged += (s, e) =>
            {
                if (_isUpdatingText) return;
                RecalculateTotals();
            };
            pnlBody.Controls.AddRange(new Control[] { lblVisa, txtVisaPaid });

            curY += 45;

            // 4. Visa Machine / Bank Account
            var lblVisaAcc = new Label
            {
                Text = "💳 ماكينة / حساب الفيزا:",
                Location = new Point(240, curY + 4),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextSub
            };
            cboVisaAccount = new ComboBox
            {
                Location = new Point(20, curY),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlBody.Controls.AddRange(new Control[] { lblVisaAcc, cboVisaAccount });

            curY += 50;

            // Summary Status Bar
            var pnlSummary = new Panel
            {
                Location = new Point(20, curY),
                Size = new Size(405, 45),
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(8)
            };

            lblTotalPaid = new Label
            {
                Text = "المسدد: 0.00 ج",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                Dock = DockStyle.Left,
                Width = 190,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblRemaining = new Label
            {
                Text = "المتبقي: 0.00 ج",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 113, 113),
                Dock = DockStyle.Right,
                Width = 190,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlSummary.Controls.Add(lblTotalPaid);
            pnlSummary.Controls.Add(lblRemaining);
            pnlBody.Controls.Add(pnlSummary);

            this.Controls.Add(pnlBody);

            // Bottom Buttons Bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            btnOk = Theme.MakeButton("✔️ تأكيد الحفظ (Enter)", 230, 10, 195, 35, Theme.Success);
            btnOk.Click += (s, e) => ConfirmPayment();

            btnCancel = Theme.MakeButton("❌ إلغاء", 20, 10, 100, 35, Theme.Danger);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlBottom.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.Controls.Add(pnlBottom);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void LoadAccounts()
        {
            // Load Cash Safes
            try
            {
                var dtSafes = DbHelper.Query("SELECT AccountID, AccountName FROM SafeAccounts WHERE AccountType != 'Visa' AND IsActive = 1 ORDER BY AccountName");
                cboSafeAccount.Items.Clear();
                int selectSafeIdx = 0;
                for (int i = 0; i < dtSafes.Rows.Count; i++)
                {
                    int id = Convert.ToInt32(dtSafes.Rows[i]["AccountID"]);
                    string name = dtSafes.Rows[i]["AccountName"].ToString();
                    cboSafeAccount.Items.Add(new ComboItem(id, name));
                    if (_defaultSafeID.HasValue && id == _defaultSafeID.Value)
                    {
                        selectSafeIdx = i;
                    }
                }
                if (cboSafeAccount.Items.Count > 0) cboSafeAccount.SelectedIndex = selectSafeIdx;
            }
            catch { }

            // Load Visa Accounts
            try
            {
                var dtVisa = DbHelper.Query("SELECT AccountID, AccountName FROM SafeAccounts WHERE AccountType = 'Visa' AND IsActive = 1 ORDER BY AccountName");
                cboVisaAccount.Items.Clear();
                for (int i = 0; i < dtVisa.Rows.Count; i++)
                {
                    int id = Convert.ToInt32(dtVisa.Rows[i]["AccountID"]);
                    string name = dtVisa.Rows[i]["AccountName"].ToString();
                    cboVisaAccount.Items.Add(new ComboItem(id, name));
                }
                if (cboVisaAccount.Items.Count > 0) cboVisaAccount.SelectedIndex = 0;
            }
            catch { }
        }

        private void RecalculateTotals()
        {
            decimal cp = 0m, vp = 0m;
            decimal.TryParse(txtCashPaid.Text.Trim(), out cp);
            decimal.TryParse(txtVisaPaid.Text.Trim(), out vp);

            decimal totalPaid = cp + vp;
            decimal rem = _totalAmount - totalPaid;

            lblTotalPaid.Text = $"إجمالي المسدد: {totalPaid:N2} ج";

            if (rem <= 0.001m && rem >= -0.001m)
            {
                lblRemaining.Text = "✅ المسدد مساوي للمطلوب";
                lblRemaining.ForeColor = Color.FromArgb(74, 222, 128);
            }
            else if (rem > 0)
            {
                lblRemaining.Text = $"متبقي على العميل: {rem:N2} ج";
                lblRemaining.ForeColor = Color.FromArgb(251, 191, 36); // Amber
            }
            else
            {
                lblRemaining.Text = $"فائض مدفوع: {Math.Abs(rem):N2} ج";
                lblRemaining.ForeColor = Color.FromArgb(96, 165, 250); // Blue
            }
        }

        private void ConfirmPayment()
        {
            decimal cp = 0m, vp = 0m;
            decimal.TryParse(txtCashPaid.Text.Trim(), out cp);
            decimal.TryParse(txtVisaPaid.Text.Trim(), out vp);

            if (cp < 0 || vp < 0)
            {
                MessageBox.Show("يرجى إدخال مبالغ صحيحة وموجبة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal totalPaid = cp + vp;

            if (!_hasClient && totalPaid < _totalAmount - 0.001m)
            {
                MessageBox.Show("عذراً، يجب سداد كامل قيمة الفاتورة للعميل النقدي غير المسجل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (vp > 0)
            {
                if (!(cboVisaAccount.SelectedItem is ComboItem vi) || vi.ID <= 0)
                {
                    MessageBox.Show("يرجى اختيار ماكينة / حساب الفيزا لسداد الجزء الإلكتروني.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboVisaAccount.Focus();
                    return;
                }
                VisaAccountID = vi.ID;
                VisaAccountName = vi.Name;
            }

            if (cboSafeAccount.SelectedItem is ComboItem si && si.ID > 0)
            {
                SafeAccountID = si.ID;
            }
            else
            {
                SafeAccountID = _defaultSafeID ?? 1;
            }

            CashPaid = cp;
            VisaPaid = vp;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtCashPaid.Focus();
            txtCashPaid.SelectAll();
        }
    }
}

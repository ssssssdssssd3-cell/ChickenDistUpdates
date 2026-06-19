using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmQuickPayment : Form
    {
        private decimal _totalAmount;
        private bool _hasClient;
        private decimal _defaultPaid;
        private TextBox txtPaid;
        private Label lblChange;
        
        public decimal PaidAmount
        {
            get
            {
                if (decimal.TryParse(txtPaid.Text, out decimal val))
                    return val;
                return 0;
            }
        }

        public FrmQuickPayment(decimal totalAmount, bool hasClient, decimal? defaultPaid = null)
        {
            _totalAmount = totalAmount;
            _hasClient = hasClient;
            _defaultPaid = defaultPaid ?? totalAmount;
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "شاشة الدفع السريع";
            this.Size = new Size(350, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            Label lblTotalTitle = new Label { Text = "المطلوب دفعه:", Location = new Point(20, 20), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextSub };
            Label lblTotal = new Label { Text = _totalAmount.ToString("N2") + " ج", Location = new Point(150, 15), AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Theme.Accent };

            Label lblPaidTitle = new Label { Text = "المدفوع من العميل:", Location = new Point(20, 70), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextSub };
            txtPaid = new TextBox
            {
                Location = new Point(150, 65),
                Width = 120,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Text = _defaultPaid.ToString("F2"),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtPaid.TextChanged += (s, e) => CalculateChange();
            txtPaid.KeyPress += (s, e) =>
            {
                char decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.') && (e.KeyChar != ','))
                {
                    e.Handled = true;
                }
                if (e.KeyChar == '.' || e.KeyChar == ',')
                {
                    e.KeyChar = decimalSeparator;
                    if (txtPaid.Text.IndexOf(decimalSeparator) > -1)
                    {
                        e.Handled = true;
                    }
                }
            };

            Label lblChangeTitle = new Label { Text = "الباقي للعميل:", Location = new Point(20, 120), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextSub };
            lblChange = new Label { Text = "0.00 ج", Location = new Point(150, 115), AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Theme.Success };

            Button btnOK = Theme.MakeButton("✔️ تأكيد الحفظ", 150, 160, 120, 35, Theme.Success);
            btnOK.Click += (s, e) => 
            {
                if (!decimal.TryParse(txtPaid.Text, out decimal paidValue) || paidValue < 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ دفع صحيح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPaid.Focus();
                    return;
                }

                if (!_hasClient && paidValue != _totalAmount)
                {
                    MessageBox.Show("عذراً، يجب دفع كامل قيمة الفاتورة للعميل غير المسجل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_hasClient)
                {
                    if (paidValue < _totalAmount)
                    {
                        decimal diff = _totalAmount - paidValue;
                        MessageBox.Show($"⚠️ سيتم إضافة المتبقي بقيمة {diff:N2} ج.م على حساب العميل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (paidValue > _totalAmount)
                    {
                        decimal diff = paidValue - _totalAmount;
                        MessageBox.Show($"➕ سيتم خصم الزيادة بقيمة {diff:N2} ج.م من حساب العميل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                this.DialogResult = DialogResult.OK; 
                this.Close(); 
            };

            Button btnCancel = Theme.MakeButton("❌ إلغاء", 20, 160, 100, 35, Theme.Danger);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { lblTotalTitle, lblTotal, lblPaidTitle, txtPaid, lblChangeTitle, lblChange, btnOK, btnCancel });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void CalculateChange()
        {
            if (decimal.TryParse(txtPaid.Text, out decimal paidVal))
            {
                decimal change = paidVal - _totalAmount;
                if (change < 0) change = 0;
                lblChange.Text = change.ToString("N2") + " ج";
            }
            else
            {
                lblChange.Text = "0.00 ج";
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtPaid.Focus();
            txtPaid.SelectAll();
        }
    }
}

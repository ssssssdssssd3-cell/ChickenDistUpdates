using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmQuickPayment : Form
    {
        private decimal _totalAmount;
        private NumericUpDown nudPaid;
        private Label lblChange;
        
        public FrmQuickPayment(decimal totalAmount)
        {
            _totalAmount = totalAmount;
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
            nudPaid = new NumericUpDown
            {
                Location = new Point(150, 65),
                Width = 120,
                Minimum = 0,
                Maximum = 9999999,
                DecimalPlaces = 2,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Value = _totalAmount, // Default to total amount
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            nudPaid.ValueChanged += (s, e) => CalculateChange();
            nudPaid.KeyUp += (s, e) => CalculateChange();

            Label lblChangeTitle = new Label { Text = "الباقي للعميل:", Location = new Point(20, 120), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextSub };
            lblChange = new Label { Text = "0.00 ج", Location = new Point(150, 115), AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Theme.Success };

            Button btnOK = Theme.MakeButton("✔️ تأكيد الحفظ", 150, 160, 120, 35, Theme.Success);
            btnOK.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

            Button btnCancel = Theme.MakeButton("❌ إلغاء", 20, 160, 100, 35, Theme.Danger);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { lblTotalTitle, lblTotal, lblPaidTitle, nudPaid, lblChangeTitle, lblChange, btnOK, btnCancel });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void CalculateChange()
        {
            decimal change = nudPaid.Value - _totalAmount;
            if (change < 0) change = 0;
            lblChange.Text = change.ToString("N2") + " ج";
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            nudPaid.Focus();
            nudPaid.Select(0, nudPaid.Value.ToString().Length + 10);
        }
    }
}

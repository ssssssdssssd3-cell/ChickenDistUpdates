using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmQuickPayment : Form
    {
        private readonly decimal _totalAmount;
        private readonly bool _hasClient;
        private readonly decimal _defaultPaid;

        private Label lblTotal;
        private TextBox txtPaid;
        private Label lblChangeTitle;
        private Label lblChange;
        private Label lblStatusHint;
        private CheckBox chkKeepExcess;
        private Button btnOK;
        private Button btnCancel;

        public decimal PaidAmount { get; private set; }
        public decimal CustomerTendered { get; private set; }
        public decimal CustomerChange { get; private set; }

        public FrmQuickPayment(decimal totalAmount, bool hasClient, decimal? defaultPaid = null)
        {
            _totalAmount = Math.Max(0m, Math.Round(totalAmount, 2));
            _hasClient = hasClient;
            _defaultPaid = defaultPaid.HasValue ? Math.Max(0m, Math.Round(defaultPaid.Value, 2)) : _totalAmount;
            PaidAmount = _totalAmount;
            CustomerTendered = _defaultPaid;
            CustomerChange = Math.Max(0m, _defaultPaid - _totalAmount);

            InitUI();
        }

        private void InitUI()
        {
            this.Text = "سداد الفاتورة - المدفوع والمتبقي";
            this.Size = new Size(470, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            // خلفية داكنة ثابتة لضمان وضوح كل النصوص بغض النظر عن الثيم الحالي
            this.BackColor = Color.FromArgb(24, 30, 42);
            this.Font = Theme.FontMain;

            // ── 1. بطاقة إجمالي الفاتورة (المطلوب سداده) ──
            Panel pnlTotalCard = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(415, 60),
                BackColor = Color.FromArgb(15, 20, 30)
            };

            Label lblTotalTitle = new Label
            {
                Text = "مطلوب سداده (قيمة الفاتورة):",
                Location = new Point(190, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.Transparent
            };

            lblTotal = new Label
            {
                Text = _totalAmount.ToString("N2") + " ج",
                Location = new Point(15, 12),
                Size = new Size(170, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                BackColor = Color.Transparent
            };

            pnlTotalCard.Controls.Add(lblTotalTitle);
            pnlTotalCard.Controls.Add(lblTotal);

            // ── 2. خانة دفع العميل (المبلغ المستلم نقدياً) ──
            Label lblPaidTitle = new Label
            {
                Text = "دفع العميل (المبلغ المستلم نقداً):",
                Location = new Point(20, 88),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            txtPaid = new TextBox
            {
                Location = new Point(20, 118),
                Size = new Size(415, 44),
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                Text = _defaultPaid.ToString("F2"),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42), // نص داكن واضح على خلفية بيضاء
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center,
                RightToLeft = RightToLeft.No
            };
            txtPaid.TextChanged += (s, e) => CalculateChange();
            txtPaid.KeyPress += (s, e) =>
            {
                char decSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
                {
                    e.Handled = true;
                }
                if (e.KeyChar == '.' || e.KeyChar == ',')
                {
                    e.KeyChar = decSep;
                    if (txtPaid.Text.IndexOf(decSep) > -1)
                    {
                        e.Handled = true;
                    }
                }
            };

            // ── 3. بطاقة الباقي للعميل (أو المتبقي) ──
            Panel pnlChangeCard = new Panel
            {
                Location = new Point(20, 178),
                Size = new Size(415, 84),
                BackColor = Color.FromArgb(15, 20, 30)
            };

            lblChangeTitle = new Label
            {
                Text = "الباقي للعميل (فكة مسترجعة):",
                Location = new Point(15, 6),
                Size = new Size(385, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.Transparent
            };

            lblChange = new Label
            {
                Text = "0.00 ج",
                Location = new Point(10, 28),
                Size = new Size(395, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                BackColor = Color.Transparent
            };

            lblStatusHint = new Label
            {
                Text = "",
                Location = new Point(10, 62),
                Size = new Size(395, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.Transparent
            };

            pnlChangeCard.Controls.Add(lblChangeTitle);
            pnlChangeCard.Controls.Add(lblChange);
            pnlChangeCard.Controls.Add(lblStatusHint);

            // ── 4. خيار العميل المسجل ──
            chkKeepExcess = new CheckBox
            {
                Location = new Point(25, 270),
                Size = new Size(405, 24),
                AutoSize = true,
                Text = "إضافة الزيادة لحساب العميل كرصيد دائن بدل إرجاعها كاش",
                ForeColor = Color.FromArgb(226, 232, 240),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Visible = _hasClient
            };
            chkKeepExcess.CheckedChanged += (s, e) => CalculateChange();

            // ── 5. أزرار الحفظ والإلغاء (حفظ كبير وواضح) ──
            btnOK = Theme.MakeButton("تأكيد وحفظ (Enter)", 175, 300, 260, 58, Color.FromArgb(5, 150, 105));
            btnOK.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            btnOK.ForeColor = Color.White;
            btnOK.Click += BtnOK_Click;

            btnCancel = Theme.MakeButton("إلغاء (Esc)", 20, 300, 140, 58, Color.FromArgb(220, 38, 38));
            btnCancel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                pnlTotalCard,
                lblPaidTitle,
                txtPaid,
                pnlChangeCard,
                chkKeepExcess,
                btnOK,
                btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void CalculateChange()
        {
            if (decimal.TryParse(txtPaid.Text, out decimal paidVal))
            {
                decimal diff = paidVal - _totalAmount;
                if (diff >= 0)
                {
                    lblChangeTitle.Text = "الباقي للعميل (فكة مسترجعة):";
                    lblChangeTitle.ForeColor = Color.FromArgb(203, 213, 225);
                    lblChange.Text = diff.ToString("N2") + " ج";
                    lblChange.ForeColor = Color.FromArgb(52, 211, 153);

                    if (diff > 0)
                    {
                        if (_hasClient && chkKeepExcess != null && chkKeepExcess.Checked)
                        {
                            lblStatusHint.Text = $"ستضاف الزيادة ({diff:N2} ج) كرصيد دائن بحساب العميل";
                            lblStatusHint.ForeColor = Color.FromArgb(56, 189, 248);
                        }
                        else
                        {
                            lblStatusHint.Text = $"يرجى تسليم العميل الباقي: {diff:N2} ج";
                            lblStatusHint.ForeColor = Color.FromArgb(52, 211, 153);
                        }
                    }
                    else
                    {
                        lblStatusHint.Text = "المبلغ المدفوع مضبوط بالتمام";
                        lblStatusHint.ForeColor = Color.FromArgb(52, 211, 153);
                    }
                }
                else
                {
                    decimal remainingDue = _totalAmount - paidVal;
                    lblChangeTitle.Text = "المتبقي على الفاتورة:";
                    lblChangeTitle.ForeColor = Color.FromArgb(252, 165, 165);
                    lblChange.Text = remainingDue.ToString("N2") + " ج";
                    lblChange.ForeColor = Color.FromArgb(248, 113, 113);

                    if (_hasClient)
                    {
                        lblStatusHint.Text = $"سيتم ترحيل {remainingDue:N2} ج كدين آجل على حساب العميل";
                        lblStatusHint.ForeColor = Color.FromArgb(251, 191, 36);
                    }
                    else
                    {
                        lblStatusHint.Text = $"المبلغ غير كافٍ! ينقص {remainingDue:N2} ج";
                        lblStatusHint.ForeColor = Color.FromArgb(248, 113, 113);
                    }
                }
            }
            else
            {
                lblChange.Text = "0.00 ج";
                lblStatusHint.Text = "";
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtPaid.Text, out decimal paidValue) || paidValue < 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ دفع صحيح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPaid.Focus();
                txtPaid.SelectAll();
                return;
            }

            if (!_hasClient)
            {
                // ── عميل نقدي كاش غير مسجل ──
                if (paidValue < _totalAmount)
                {
                    decimal diff = _totalAmount - paidValue;
                    MessageBox.Show($"عذراً: المبلغ المستلم من العميل ({paidValue:N2} ج) أقل من إجمالي الفاتورة ({_totalAmount:N2} ج) بمقدار ({diff:N2} ج)!\n\nالعميل النقدي يجب أن يسدد كامل قيمة الفاتورة.",
                        "المبلغ المستلم غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPaid.Focus();
                    txtPaid.SelectAll();
                    return;
                }

                // يسدد كامل الفاتورة، والباقي فكة مسترجعة للعميل
                PaidAmount = _totalAmount;
                CustomerTendered = paidValue;
                CustomerChange = paidValue - _totalAmount;
            }
            else
            {
                // ── عميل مسجل له حساب ──
                CustomerTendered = paidValue;
                if (paidValue < _totalAmount)
                {
                    PaidAmount = paidValue;
                    CustomerChange = 0m;
                }
                else if (paidValue > _totalAmount)
                {
                    if (chkKeepExcess != null && chkKeepExcess.Checked)
                    {
                        PaidAmount = paidValue;
                        CustomerChange = 0m;
                    }
                    else
                    {
                        PaidAmount = _totalAmount;
                        CustomerChange = paidValue - _totalAmount;
                    }
                }
                else
                {
                    PaidAmount = _totalAmount;
                    CustomerChange = 0m;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtPaid.Focus();
            txtPaid.SelectAll();
            CalculateChange();
        }
    }
}

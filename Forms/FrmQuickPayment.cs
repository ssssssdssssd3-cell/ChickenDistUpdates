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
            this.Text = "💳 سداد الفاتورة - المدفوع والمتبقي";
            this.Size = new Size(470, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── 1. بطاقة إجمالي الفاتورة (المطلوب سداده) ──
            Panel pnlTotalCard = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(415, 60),
                BackColor = Color.FromArgb(24, 30, 42),
                Padding = new Padding(12, 8, 12, 8)
            };

            Label lblTotalTitle = new Label
            {
                Text = "المطلوب سداده (قيمة الفاتورة):",
                Location = new Point(220, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            lblTotal = new Label
            {
                Text = _totalAmount.ToString("N2") + " ج",
                Location = new Point(15, 12),
                Size = new Size(200, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21) // Glowing Amber/Gold
            };

            pnlTotalCard.Controls.Add(lblTotalTitle);
            pnlTotalCard.Controls.Add(lblTotal);

            // ── 2. خانة دفع العميل (المبلغ المستلم نقدياً) ──
            Label lblPaidTitle = new Label
            {
                Text = "دفع العميل (المبلغ المستلم نقدياً 💵):",
                Location = new Point(20, 85),
                AutoSize = true,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249)
            };

            txtPaid = new TextBox
            {
                Location = new Point(20, 114),
                Size = new Size(415, 42),
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                Text = _defaultPaid.ToString("F2"),
                BackColor = Theme.BgInput,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
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

            // ── 3. أزرار الفئات النقدية السريعة ──
            FlowLayoutPanel pnlDenominations = new FlowLayoutPanel
            {
                Location = new Point(20, 165),
                Size = new Size(415, 34),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0)
            };

            Button btnExact = MakeQuickButton("المبلغ بالضبط", 100, Color.FromArgb(30, 58, 138));
            btnExact.Click += (s, e) => { SetPaidAmount(_totalAmount); };

            Button btn50 = MakeQuickButton("50 ج", 70, Color.FromArgb(31, 41, 55));
            btn50.Click += (s, e) => { SetPaidAmount(50m); };

            Button btn100 = MakeQuickButton("100 ج", 70, Color.FromArgb(31, 41, 55));
            btn100.Click += (s, e) => { SetPaidAmount(100m); };

            Button btn200 = MakeQuickButton("200 ج", 70, Color.FromArgb(31, 41, 55));
            btn200.Click += (s, e) => { SetPaidAmount(200m); };

            Button btn500 = MakeQuickButton("500 ج", 70, Color.FromArgb(31, 41, 55));
            btn500.Click += (s, e) => { SetPaidAmount(500m); };

            pnlDenominations.Controls.AddRange(new Control[] { btnExact, btn50, btn100, btn200, btn500 });

            // صف إضافي للزيادات السريعة
            FlowLayoutPanel pnlIncrements = new FlowLayoutPanel
            {
                Location = new Point(20, 203),
                Size = new Size(415, 32),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0)
            };

            Button btnAdd10 = MakeQuickButton("+10 ج", 70, Color.FromArgb(30, 41, 59));
            btnAdd10.Click += (s, e) => { AddToPaidAmount(10m); };

            Button btnAdd20 = MakeQuickButton("+20 ج", 70, Color.FromArgb(30, 41, 59));
            btnAdd20.Click += (s, e) => { AddToPaidAmount(20m); };

            Button btnAdd50 = MakeQuickButton("+50 ج", 70, Color.FromArgb(30, 41, 59));
            btnAdd50.Click += (s, e) => { AddToPaidAmount(50m); };

            Button btnAdd100 = MakeQuickButton("+100 ج", 75, Color.FromArgb(30, 41, 59));
            btnAdd100.Click += (s, e) => { AddToPaidAmount(100m); };

            Button btnAdd200 = MakeQuickButton("+200 ج", 75, Color.FromArgb(30, 41, 59));
            btnAdd200.Click += (s, e) => { AddToPaidAmount(200m); };

            pnlIncrements.Controls.AddRange(new Control[] { btnAdd10, btnAdd20, btnAdd50, btnAdd100, btnAdd200 });

            // ── 4. بطاقة الباقي للعميل (أو المتبقي) ──
            Panel pnlChangeCard = new Panel
            {
                Location = new Point(20, 245),
                Size = new Size(415, 78),
                BackColor = Color.FromArgb(16, 24, 39),
                Padding = new Padding(12, 6, 12, 6)
            };

            lblChangeTitle = new Label
            {
                Text = "الباقي للعميل (فكة مسترجعة 🪙):",
                Location = new Point(15, 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            lblChange = new Label
            {
                Text = "0.00 ج",
                Location = new Point(10, 26),
                Size = new Size(395, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153) // Vibrant Emerald
            };

            lblStatusHint = new Label
            {
                Text = "المبلغ مطابق بالتمام",
                Location = new Point(10, 56),
                Size = new Size(395, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            pnlChangeCard.Controls.Add(lblChangeTitle);
            pnlChangeCard.Controls.Add(lblChange);
            pnlChangeCard.Controls.Add(lblStatusHint);

            // ── 5. خيار العميل المسجل ──
            chkKeepExcess = new CheckBox
            {
                Location = new Point(25, 330),
                Size = new Size(405, 24),
                AutoSize = true,
                Text = "إضافة الزيادة لحساب العميل كرصيد دائن بدل إرجاعها كاش",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Visible = _hasClient
            };
            chkKeepExcess.CheckedChanged += (s, e) => CalculateChange();

            // ── 6. أزرار الحفظ والإلغاء ──
            btnOK = Theme.MakeButton("💾 تأكيد وسداد (Enter)", 235, 370, 200, 42, Color.FromArgb(5, 150, 105));
            btnOK.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            btnOK.Click += BtnOK_Click;

            btnCancel = Theme.MakeButton("❌ إلغاء (Esc)", 20, 370, 120, 42, Color.FromArgb(220, 38, 38));
            btnCancel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                pnlTotalCard,
                lblPaidTitle,
                txtPaid,
                pnlDenominations,
                pnlIncrements,
                pnlChangeCard,
                chkKeepExcess,
                btnOK,
                btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private Button MakeQuickButton(string text, int width, Color backColor)
        {
            Button btn = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetPaidAmount(decimal amount)
        {
            txtPaid.Text = amount.ToString("F2");
            txtPaid.Focus();
            txtPaid.SelectAll();
        }

        private void AddToPaidAmount(decimal add)
        {
            decimal current = 0m;
            decimal.TryParse(txtPaid.Text, out current);
            txtPaid.Text = (current + add).ToString("F2");
            txtPaid.Focus();
            txtPaid.SelectAll();
        }

        private void CalculateChange()
        {
            if (decimal.TryParse(txtPaid.Text, out decimal paidVal))
            {
                decimal diff = paidVal - _totalAmount;
                if (diff >= 0)
                {
                    lblChangeTitle.Text = "الباقي للعميل (فكة مسترجعة 🪙):";
                    lblChangeTitle.ForeColor = Color.FromArgb(148, 163, 184);
                    lblChange.Text = diff.ToString("N2") + " ج";
                    lblChange.ForeColor = Color.FromArgb(52, 211, 153); // Emerald
                    
                    if (diff > 0)
                    {
                        if (_hasClient && chkKeepExcess != null && chkKeepExcess.Checked)
                        {
                            lblStatusHint.Text = $"➕ ستضاف الزيادة ({diff:N2} ج) كرصيد دائن بحساب العميل";
                            lblStatusHint.ForeColor = Color.FromArgb(56, 189, 248); // Sky blue
                        }
                        else
                        {
                            lblStatusHint.Text = $"✅ يتبقى للعميل {diff:N2} ج (يرجى تسليم الباقي)";
                            lblStatusHint.ForeColor = Color.FromArgb(52, 211, 153);
                        }
                    }
                    else
                    {
                        lblStatusHint.Text = "✅ المبلغ المدفوع مضبوط بالتمام";
                        lblStatusHint.ForeColor = Color.FromArgb(52, 211, 153);
                    }
                }
                else
                {
                    decimal remainingDue = _totalAmount - paidVal;
                    lblChangeTitle.Text = "المتبقي على الفاتورة (عجز):";
                    lblChangeTitle.ForeColor = Color.FromArgb(248, 113, 113);
                    lblChange.Text = remainingDue.ToString("N2") + " ج";
                    lblChange.ForeColor = Color.FromArgb(239, 68, 68); // Red
                    
                    if (_hasClient)
                    {
                        lblStatusHint.Text = $"⚠️ سيتم ترحيل {remainingDue:N2} ج كدين آجل على حساب العميل";
                        lblStatusHint.ForeColor = Color.FromArgb(251, 191, 36); // Amber
                    }
                    else
                    {
                        lblStatusHint.Text = $"❌ المبلغ غير كافٍ! ينقص {remainingDue:N2} ج لسداد الفاتورة";
                        lblStatusHint.ForeColor = Color.FromArgb(239, 68, 68);
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
                    MessageBox.Show($"⚠️ عذراً: المبلغ المستلم من العميل ({paidValue:N2} ج) أقل من إجمالي الفاتورة ({_totalAmount:N2} ج) بمقدار ({diff:N2} ج)!\n\nالعميل النقدي يجب أن يسدد كامل قيمة الفاتورة.",
                        "المبلغ المستلم غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPaid.Focus();
                    txtPaid.SelectAll();
                    return;
                }

                // دفع العميل مساوٍ أو أكبر من الفاتورة:
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

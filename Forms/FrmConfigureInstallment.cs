using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmConfigureInstallment : Form
    {
        private readonly decimal _cashPrice;
        private bool _isUpdating = false;

        // Expose values for FrmSale to read
        public decimal InstallmentPrice { get; private set; }
        public decimal DownPayment { get; private set; }
        public decimal ProfitRate { get; private set; }
        public decimal ProfitAmount { get; private set; }
        public decimal FinancedAmount { get; private set; }
        public int InstallmentCount { get; private set; }
        public string InstallmentPeriod { get; private set; }
        public DateTime StartDate { get; private set; }
        public List<InstallmentScheduleDTO> Schedule { get; private set; }

        private TextBox txtCashPrice;
        private TextBox txtDownPayment;
        private TextBox txtRemainingPrincipal;
        private TextBox txtProfitPct;
        private TextBox txtProfitAmount;
        private TextBox txtFinancedAmount;
        private TextBox txtInstallmentPrice;

        private NumericUpDown nudInstallmentCount;
        private ComboBox cboPeriod;
        private DateTimePicker dtpStartDate;
        private Label lblSummary;
        private DataGridView dgSchedule;
        private Button btnRecalc;
        private Button btnConfirm;
        private Button btnCancel;

        public FrmConfigureInstallment(decimal cashPrice, decimal initialDownPayment = 0m)
        {
            _cashPrice = Math.Max(0m, cashPrice);
            InitUI(initialDownPayment);
            CalculateFromDownPaymentOrRate(isRateChanged: true);
        }

        private void InitUI(decimal initialDownPayment)
        {
            this.Text = "💳 إعداد عقد وتقسيط الفاتورة";
            this.Size = new Size(700, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("💳 إعداد عقد التقسيط وحساب الأرباح", "يتم احتساب الفائدة والأقساط على صافي المبلغ المتبقي بعد خصم الدفعة المقدمة");
            this.Controls.Add(pnlTop);

            int y = 75;

            // ── السطر الأول: سعر الفاتورة النقدي والدفعة المقدمة ───────────────────────
            AddLabel("سعر الفاتورة النقدي (ج):", 25, y);
            txtCashPrice = new TextBox
            {
                Location = new Point(190, y - 3),
                Width = 130,
                Text = _cashPrice.ToString("F2"),
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 243, 246),
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            this.Controls.Add(txtCashPrice);

            AddLabel("الدفعة المقدمة / المدفوع (ج):", 345, y);
            txtDownPayment = new TextBox
            {
                Location = new Point(530, y - 3),
                Width = 130,
                Text = initialDownPayment > 0 ? initialDownPayment.ToString("F2") : "0.00",
                BackColor = Theme.BgInput,
                ForeColor = Color.FromArgb(16, 185, 129), // Emerald green
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            txtDownPayment.TextChanged += (s, e) => { if (!_isUpdating) CalculateFromDownPaymentOrRate(isRateChanged: false); };
            this.Controls.Add(txtDownPayment);

            y += 38;

            // ── السطر الثاني: أصل المبلغ المتبقي ونسبة الفائدة ───────────────────────
            AddLabel("المتبقي بعد خصم المدفوع:", 25, y);
            txtRemainingPrincipal = new TextBox
            {
                Location = new Point(190, y - 3),
                Width = 130,
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 243, 246),
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            this.Controls.Add(txtRemainingPrincipal);

            AddLabel("نسبة الفائدة / الربح (%):", 345, y);
            txtProfitPct = new TextBox
            {
                Location = new Point(530, y - 3),
                Width = 130,
                Text = "10", // Default 10% profit on remaining balance
                BackColor = Theme.BgInput,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            txtProfitPct.TextChanged += (s, e) => { if (!_isUpdating) CalculateFromProfitPct(); };
            this.Controls.Add(txtProfitPct);

            y += 38;

            // ── السطر الثالث: مبلغ الفائدة والمبلغ المتبقي للتقسيط ───────────────────
            AddLabel("مبلغ الفائدة المضاف (ج):", 25, y);
            txtProfitAmount = new TextBox
            {
                Location = new Point(190, y - 3),
                Width = 130,
                BackColor = Theme.BgInput,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            txtProfitAmount.TextChanged += (s, e) => { if (!_isUpdating) CalculateFromProfitAmount(); };
            this.Controls.Add(txtProfitAmount);

            AddLabel("صافي المتبقي للتقسيط (الممول):", 345, y);
            txtFinancedAmount = new TextBox
            {
                Location = new Point(530, y - 3),
                Width = 130,
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 243, 246),
                ForeColor = Color.FromArgb(30, 64, 175), // Deep Blue
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            this.Controls.Add(txtFinancedAmount);

            y += 38;

            // ── السطر الرابع: إجمالي سعر الفاتورة بالتقسيط وعدد الأقساط ─────────────
            AddLabel("إجمالي الفاتورة بالتقسيط (ج):", 25, y);
            txtInstallmentPrice = new TextBox
            {
                Location = new Point(190, y - 3),
                Width = 130,
                BackColor = Theme.BgInput,
                ForeColor = Color.FromArgb(194, 65, 12), // Deep Orange / Brown
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 10.5f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            txtInstallmentPrice.TextChanged += (s, e) => { if (!_isUpdating) CalculateFromInstallmentPrice(); };
            this.Controls.Add(txtInstallmentPrice);

            AddLabel("عدد الأقساط:", 345, y);
            nudInstallmentCount = new NumericUpDown
            {
                Location = new Point(530, y - 3),
                Width = 130,
                Minimum = 1,
                Maximum = 120,
                Value = 6,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                TextAlign = HorizontalAlignment.Center,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold)
            };
            nudInstallmentCount.ValueChanged += (s, e) => CalculateSchedule();
            this.Controls.Add(nudInstallmentCount);

            y += 38;

            // ── السطر الخامس: دورية السداد وتاريخ أول قسط ───────────────────────────
            AddLabel("دورية السداد:", 25, y);
            cboPeriod = new ComboBox
            {
                Location = new Point(190, y - 3),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(Theme.FontMain.FontFamily, 9.5f)
            };
            cboPeriod.Items.AddRange(new object[] { "شهري", "أسبوعي", "نصف شهري" });
            cboPeriod.SelectedIndex = 0;
            cboPeriod.SelectedIndexChanged += (s, e) => CalculateSchedule();
            this.Controls.Add(cboPeriod);

            AddLabel("تاريخ استحقاق أول قسط:", 345, y);
            dtpStartDate = new DateTimePicker
            {
                Location = new Point(530, y - 3),
                Width = 130,
                Format = DateTimePickerFormat.Short,
                RightToLeftLayout = true,
                Font = new Font(Theme.FontMain.FontFamily, 9.5f)
            };
            dtpStartDate.Value = DateTime.Today.AddMonths(1);
            dtpStartDate.ValueChanged += (s, e) => CalculateSchedule();
            this.Controls.Add(dtpStartDate);

            y += 36;

            // ── بطاقة ملخص الأقساط ────────────────────────────────────────────────
            lblSummary = new Label
            {
                Location = new Point(25, y),
                Size = new Size(635, 34),
                BackColor = Color.FromArgb(238, 242, 255), // Indigo tint
                ForeColor = Color.FromArgb(49, 46, 129),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Theme.FontMain.FontFamily, 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "قيمة القسط: 0.00 ج | صافي المتبقي للتقسيط: 0.00 ج"
            };
            this.Controls.Add(lblSummary);

            y += 42;

            // ── جدول معاينة الأقساط ───────────────────────────────────────────────
            dgSchedule = new DataGridView
            {
                Location = new Point(25, y),
                Size = new Size(635, 200),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, Alignment = DataGridViewContentAlignment.MiddleCenter },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleCenter },
                EnableHeadersVisualStyles = false
            };
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "رقم القسط", FillWeight = 25 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "DueDate", HeaderText = "تاريخ الاستحقاق", FillWeight = 45 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "قيمة القسط (ج)", FillWeight = 40 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 30 });
            this.Controls.Add(dgSchedule);

            y += 210;

            // ── الأزرار السفلية ───────────────────────────────────────────────────
            btnConfirm = Theme.MakeButton("💾 تأكيد وحفظ العقد", 470, y, 190, 38, Theme.Success);
            btnConfirm.Click += BtnConfirm_Click;

            btnRecalc = Theme.MakeButton("🔄 إعادة جدولة الأقساط", 260, y, 190, 38, Theme.Primary);
            btnRecalc.Click += (s, e) => CalculateSchedule();

            btnCancel = Theme.MakeButton("❌ إلغاء", 25, y, 110, 38, Theme.Danger);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { btnConfirm, btnRecalc, btnCancel });
        }

        private void AddLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            this.Controls.Add(lbl);
        }

        /// <summary>
        /// الحساب الأساسي: يحسب الفائدة والأقساط على أصل المبلغ المتبقي بعد خصم المدفوع/المقدم
        /// </summary>
        private void CalculateFromDownPaymentOrRate(bool isRateChanged)
        {
            _isUpdating = true;
            try
            {
                decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
                if (downPayment < 0) downPayment = 0;

                decimal remainingPrincipal = Math.Max(0, _cashPrice - downPayment);
                txtRemainingPrincipal.Text = remainingPrincipal.ToString("F2");

                decimal.TryParse(txtProfitPct.Text, out decimal profitPct);
                if (profitPct < 0) profitPct = 0;

                decimal profitAmount = Math.Round(remainingPrincipal * (profitPct / 100m), 2);
                decimal financedAmount = remainingPrincipal + profitAmount;
                decimal installmentPrice = downPayment + financedAmount;

                txtProfitAmount.Text = profitAmount.ToString("F2");
                txtFinancedAmount.Text = financedAmount.ToString("F2");
                txtInstallmentPrice.Text = installmentPrice.ToString("F2");
            }
            finally
            {
                _isUpdating = false;
            }

            CalculateSchedule();
        }

        private void CalculateFromProfitPct()
        {
            _isUpdating = true;
            try
            {
                decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
                decimal remainingPrincipal = Math.Max(0, _cashPrice - downPayment);
                txtRemainingPrincipal.Text = remainingPrincipal.ToString("F2");

                decimal.TryParse(txtProfitPct.Text, out decimal profitPct);
                if (profitPct < 0) profitPct = 0;

                decimal profitAmount = Math.Round(remainingPrincipal * (profitPct / 100m), 2);
                decimal financedAmount = remainingPrincipal + profitAmount;
                decimal installmentPrice = downPayment + financedAmount;

                txtProfitAmount.Text = profitAmount.ToString("F2");
                txtFinancedAmount.Text = financedAmount.ToString("F2");
                txtInstallmentPrice.Text = installmentPrice.ToString("F2");
            }
            finally
            {
                _isUpdating = false;
            }

            CalculateSchedule();
        }

        private void CalculateFromProfitAmount()
        {
            _isUpdating = true;
            try
            {
                decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
                decimal remainingPrincipal = Math.Max(0, _cashPrice - downPayment);
                txtRemainingPrincipal.Text = remainingPrincipal.ToString("F2");

                decimal.TryParse(txtProfitAmount.Text, out decimal profitAmount);
                if (profitAmount < 0) profitAmount = 0;

                decimal profitPct = remainingPrincipal > 0 ? Math.Round((profitAmount / remainingPrincipal) * 100m, 2) : 0m;
                decimal financedAmount = remainingPrincipal + profitAmount;
                decimal installmentPrice = downPayment + financedAmount;

                txtProfitPct.Text = profitPct.ToString("0.##");
                txtFinancedAmount.Text = financedAmount.ToString("F2");
                txtInstallmentPrice.Text = installmentPrice.ToString("F2");
            }
            finally
            {
                _isUpdating = false;
            }

            CalculateSchedule();
        }

        private void CalculateFromInstallmentPrice()
        {
            _isUpdating = true;
            try
            {
                decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
                decimal remainingPrincipal = Math.Max(0, _cashPrice - downPayment);
                txtRemainingPrincipal.Text = remainingPrincipal.ToString("F2");

                decimal.TryParse(txtInstallmentPrice.Text, out decimal instPrice);
                decimal financedAmount = Math.Max(0, instPrice - downPayment);
                decimal profitAmount = Math.Max(0, financedAmount - remainingPrincipal);
                decimal profitPct = remainingPrincipal > 0 ? Math.Round((profitAmount / remainingPrincipal) * 100m, 2) : 0m;

                txtProfitPct.Text = profitPct.ToString("0.##");
                txtProfitAmount.Text = profitAmount.ToString("F2");
                txtFinancedAmount.Text = financedAmount.ToString("F2");
            }
            finally
            {
                _isUpdating = false;
            }

            CalculateSchedule();
        }

        private void CalculateSchedule()
        {
            dgSchedule.Rows.Clear();
            Schedule = new List<InstallmentScheduleDTO>();

            decimal.TryParse(txtFinancedAmount.Text, out decimal financed);
            decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
            decimal.TryParse(txtProfitAmount.Text, out decimal profitAmount);

            if (financed <= 0)
            {
                lblSummary.Text = "⚠️ لا يوجد مبلغ متبقي للتقسيط (تم سداد الفاتورة بالكامل مقدماً)";
                return;
            }

            int count = (int)nudInstallmentCount.Value;
            if (count <= 0) count = 1;

            decimal baseValue = Math.Round(financed / count, 2);
            decimal totalAllocated = baseValue * count;
            decimal diff = financed - totalAllocated;

            lblSummary.Text = $"📊 قيمة القسط: {baseValue:N2} ج ({cboPeriod.Text}) | إجمالي الأقساط: {financed:N2} ج (يشمل {profitAmount:N2} ج أرباح) | المقدم: {downPayment:N2} ج";

            DateTime currentDueDate = dtpStartDate.Value;
            string period = cboPeriod.Text;

            for (int i = 1; i <= count; i++)
            {
                decimal amount = baseValue;
                if (i == count)
                {
                    amount += diff; // ضبط كسور التقريب بالقسط الأخير
                }

                Schedule.Add(new InstallmentScheduleDTO
                {
                    InstallmentNo = i,
                    DueDate = currentDueDate,
                    Amount = amount
                });

                dgSchedule.Rows.Add(i, currentDueDate.ToString("yyyy-MM-dd"), amount.ToString("N2") + " ج", "مستحق");

                // Calculate next due date
                if (period == "أسبوعي")
                {
                    currentDueDate = currentDueDate.AddDays(7);
                }
                else if (period == "نصف شهري")
                {
                    currentDueDate = currentDueDate.AddDays(15);
                }
                else // شهري
                {
                    currentDueDate = currentDueDate.AddMonths(1);
                }
            }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtInstallmentPrice.Text, out decimal instPrice) || instPrice <= 0)
            {
                MessageBox.Show("من فضلك أدخل سعر بيع تقسيط صحيح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
            if (downPayment >= instPrice)
            {
                MessageBox.Show("لا يمكن أن تكون الدفعة المقدمة مساوية أو أكبر من إجمالي سعر التقسيط.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (instPrice < _cashPrice)
            {
                MessageBox.Show("تحذير: سعر البيع بالتقسيط أقل من السعر النقدي المعتاد للفاتورة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            CalculateSchedule();

            if (Schedule == null || Schedule.Count == 0)
            {
                MessageBox.Show("فشل حساب جدول الأقساط.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal.TryParse(txtProfitPct.Text, out decimal profitRate);
            decimal.TryParse(txtProfitAmount.Text, out decimal profitAmt);
            decimal.TryParse(txtFinancedAmount.Text, out decimal financedAmt);

            // Populate public properties for FrmSale to read
            InstallmentPrice = instPrice;
            DownPayment = downPayment;
            ProfitRate = profitRate;
            ProfitAmount = profitAmt;
            FinancedAmount = financedAmt;
            InstallmentCount = Schedule.Count;
            StartDate = dtpStartDate.Value;

            string periodEn = "Monthly";
            if (cboPeriod.Text == "أسبوعي") periodEn = "Weekly";
            else if (cboPeriod.Text == "نصف شهري") periodEn = "BiWeekly";
            InstallmentPeriod = periodEn;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

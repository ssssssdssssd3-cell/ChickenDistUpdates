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
        private decimal _cashPrice;
        
        // Expose values for FrmSale to read
        public decimal InstallmentPrice { get; private set; }
        public decimal DownPayment { get; private set; }
        public int InstallmentCount { get; private set; }
        public string InstallmentPeriod { get; private set; }
        public DateTime StartDate { get; private set; }
        public List<InstallmentScheduleDTO> Schedule { get; private set; }

        private TextBox txtCashPrice;
        private TextBox txtInstallmentPrice;
        private TextBox txtDownPayment;
        private TextBox txtFinancedAmount;
        private NumericUpDown nudInstallmentCount;
        private ComboBox cboPeriod;
        private DateTimePicker dtpStartDate;
        private DataGridView dgSchedule;
        private Button btnCalculate;
        private Button btnConfirm;
        private Button btnCancel;

        public FrmConfigureInstallment(decimal cashPrice)
        {
            _cashPrice = cashPrice;
            InitUI();
            CalculateSchedule();
        }

        private void InitUI()
        {
            this.Text = "إعداد عقد التقسيط الشرعي";
            this.Size = new Size(580, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            // Labels and inputs layout
            int y = 20;

            AddLabel("سعر الفاتورة النقدي (ج):", 20, y);
            txtCashPrice = new TextBox
            {
                Location = new Point(180, y - 3),
                Width = 120,
                Text = _cashPrice.ToString("F2"),
                ReadOnly = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            this.Controls.Add(txtCashPrice);

            AddLabel("سعر البيع بالتقسيط (ج):", 310, y);
            txtInstallmentPrice = new TextBox
            {
                Location = new Point(440, y - 3),
                Width = 110,
                Text = Math.Round(_cashPrice * 1.10m, 2).ToString("F2"), // Default 10% markup
                BackColor = Theme.BgInput,
                ForeColor = Theme.Accent,
                Font = new Font(Theme.FontMain.FontFamily, 10f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtInstallmentPrice.TextChanged += (s, e) => UpdateFinancedAmount();
            this.Controls.Add(txtInstallmentPrice);

            y += 40;

            AddLabel("الدفعة المقدمة (ج):", 20, y);
            txtDownPayment = new TextBox
            {
                Location = new Point(180, y - 3),
                Width = 120,
                Text = "0.00",
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtDownPayment.TextChanged += (s, e) => UpdateFinancedAmount();
            this.Controls.Add(txtDownPayment);

            AddLabel("المبلغ المتبقي للتقسيط:", 310, y);
            txtFinancedAmount = new TextBox
            {
                Location = new Point(440, y - 3),
                Width = 110,
                ReadOnly = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            this.Controls.Add(txtFinancedAmount);

            y += 40;

            AddLabel("عدد الأقساط:", 20, y);
            nudInstallmentCount = new NumericUpDown
            {
                Location = new Point(180, y - 3),
                Width = 120,
                Minimum = 1,
                Maximum = 60,
                Value = 6,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                TextAlign = HorizontalAlignment.Center
            };
            this.Controls.Add(nudInstallmentCount);

            AddLabel("دورية السداد:", 310, y);
            cboPeriod = new ComboBox
            {
                Location = new Point(440, y - 3),
                Width = 110,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboPeriod.Items.AddRange(new object[] { "شهري", "أسبوعي", "نصف شهري" });
            cboPeriod.SelectedIndex = 0;
            this.Controls.Add(cboPeriod);

            y += 40;

            AddLabel("تاريخ أول قسط:", 20, y);
            dtpStartDate = new DateTimePicker
            {
                Location = new Point(180, y - 3),
                Width = 120,
                Format = DateTimePickerFormat.Short,
                RightToLeftLayout = true
            };
            dtpStartDate.Value = DateTime.Today.AddMonths(1);
            this.Controls.Add(dtpStartDate);

            btnCalculate = Theme.MakeButton("🔄 حساب الجدولة", 400, y - 5, 150, 28, Theme.Primary);
            btnCalculate.Click += (s, e) => CalculateSchedule();
            this.Controls.Add(btnCalculate);

            y += 45;

            // Grid preview
            dgSchedule = new DataGridView
            {
                Location = new Point(20, y),
                Size = new Size(530, 240),
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
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "رقم القسط", FillWeight = 30 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "DueDate", HeaderText = "تاريخ الاستحقاق", FillWeight = 50 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "قيمة القسط (ج)", FillWeight = 40 });
            this.Controls.Add(dgSchedule);

            y += 255;

            // Confirm / Cancel
            btnConfirm = Theme.MakeButton("💾 تأكيد وحفظ العقد", 370, y, 180, 34, Theme.Success);
            btnConfirm.Click += BtnConfirm_Click;
            btnCancel = Theme.MakeButton("❌ إلغاء", 260, y, 100, 34, Theme.Danger);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            
            this.Controls.AddRange(new Control[] { btnConfirm, btnCancel });

            UpdateFinancedAmount();
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

        private void UpdateFinancedAmount()
        {
            decimal.TryParse(txtInstallmentPrice.Text, out decimal instPrice);
            decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
            txtFinancedAmount.Text = (instPrice - downPayment).ToString("F2");
        }

        private void CalculateSchedule()
        {
            dgSchedule.Rows.Clear();
            Schedule = new List<InstallmentScheduleDTO>();

            if (!decimal.TryParse(txtInstallmentPrice.Text, out decimal instPrice) || instPrice <= 0) return;
            decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
            decimal financed = instPrice - downPayment;

            if (financed <= 0) return;

            int count = (int)nudInstallmentCount.Value;
            decimal baseValue = Math.Round(financed / count, 2);
            decimal totalAllocated = baseValue * count;
            decimal diff = financed - totalAllocated;

            DateTime currentDueDate = dtpStartDate.Value;
            string period = cboPeriod.Text;

            for (int i = 1; i <= count; i++)
            {
                decimal amount = baseValue;
                if (i == count)
                {
                    amount += diff; // Adjust rounding on the last installment
                }

                Schedule.Add(new InstallmentScheduleDTO
                {
                    InstallmentNo = i,
                    DueDate = currentDueDate,
                    Amount = amount
                });

                dgSchedule.Rows.Add(i, currentDueDate.ToString("yyyy-MM-dd"), amount.ToString("F2") + " ج");

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

            if (instPrice < _cashPrice)
            {
                MessageBox.Show("تحذير شرعي: سعر البيع بالتقسيط لا يجب أن يكون أقل من السعر النقدي المعتاد للفاتورة إلا في حال التنازل الطوعي.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            decimal.TryParse(txtDownPayment.Text, out decimal downPayment);
            if (downPayment >= instPrice)
            {
                MessageBox.Show("لا يمكن أن تكون الدفعة المقدمة مساوية أو أكبر من إجمالي سعر التقسيط.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CalculateSchedule();

            if (Schedule == null || Schedule.Count == 0)
            {
                MessageBox.Show("فشل حساب جدول الأقساط.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Populate public properties for FrmSale to read
            InstallmentPrice = instPrice;
            DownPayment = downPayment;
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

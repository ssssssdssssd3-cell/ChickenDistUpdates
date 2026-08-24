using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة تقسيط وجدولة مديونية عميل سابقة وقائمة
    /// </summary>
    public class FrmScheduleClientDebt : Form
    {
        private int _initialClientID = 0;
        private ComboBox cboClient;
        private Label lblCurrentBalance;
        private TextBox txtDebtAmount, txtDownPayment, txtProfitPct, txtProfitAmount, txtFinancedAmount, txtNotes;
        private NumericUpDown nudInstallmentCount;
        private ComboBox cboPeriod, cboSafeAccount;
        private DateTimePicker dtpStartDate;
        private Button btnGenerateSchedule, btnSaveContract, btnPrintContract, btnCancel;
        private DataGridView dgSchedule;
        private Label lblSummaryContract, lblSummaryInstallment, lblSummaryEndDate;

        private bool _isUpdating = false;
        private decimal _clientCurrentBalance = 0m;
        private List<InstallmentScheduleDTO> _generatedSchedule = new List<InstallmentScheduleDTO>();

        public FrmScheduleClientDebt(int clientID = 0)
        {
            _initialClientID = clientID;
            InitUI();
            LoadSafes();
            LoadClients();

            if (_initialClientID > 0)
            {
                SelectClientByID(_initialClientID);
            }
        }

        private void InitUI()
        {
            this.Text = "💳 تقسيط وجدولة مديونية عميل";
            this.Size = new Size(1020, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // شريط العنوان العلوي
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblTitle = new Label
            {
                Text = "💳 تقسيط وجدولة مديونية عميل قائمة (تحويل الرصيد الآجل إلى أقساط مجدولة)",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 13)
            };
            pnlTop.Controls.Add(lblTitle);

            // تقسيم الشاشة: لوحة الإعدادات يمين (460px)، جدول الأقساط يسار (Fill)
            var pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 460,
                BackColor = Theme.BgCard,
                Padding = new Padding(15),
                AutoScroll = true
            };

            int y = 15;

            // 1. اختيار العميل
            pnlRight.Controls.Add(new Label { Text = "👤 اختر العميل:", Location = new Point(330, y), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain });
            cboClient = new ComboBox
            {
                Location = new Point(20, y + 24),
                Width = 410,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            cboClient.SelectedIndexChanged += CboClient_SelectedIndexChanged;
            pnlRight.Controls.Add(cboClient);
            y += 62;

            // بطاقة رصيد المديونية الحالي
            var pnlBal = new Panel
            {
                Location = new Point(20, y),
                Width = 410,
                Height = 60,
                BackColor = Color.FromArgb(254, 242, 242),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblBalTitle = new Label { Text = "المديونية الحالية على العميل:", Location = new Point(230, 8), AutoSize = true, ForeColor = Color.FromArgb(153, 27, 27), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            lblCurrentBalance = new Label { Text = "0.00 ج", Location = new Point(15, 26), AutoSize = true, ForeColor = Color.FromArgb(185, 28, 28), Font = new Font("Segoe UI", 14f, FontStyle.Bold) };
            pnlBal.Controls.AddRange(new Control[] { lblBalTitle, lblCurrentBalance });
            pnlRight.Controls.Add(pnlBal);
            y += 72;

            // 2. المبالغ والتقسيط
            pnlRight.Controls.Add(new Label { Text = "أصل المديونية المقسطة (ج):", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            pnlRight.Controls.Add(new Label { Text = "الدفعة المقدمة المسددة (ج):", Location = new Point(40, y), AutoSize = true, ForeColor = Theme.TextMain });
            y += 22;

            txtDebtAmount = new TextBox { Location = new Point(230, y), Width = 200, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = "0.00", TextAlign = HorizontalAlignment.Center };
            txtDebtAmount.TextChanged += (s, e) => { if (!_isUpdating) RecalculateFinanced(); };

            txtDownPayment = new TextBox { Location = new Point(20, y), Width = 195, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = "0.00", TextAlign = HorizontalAlignment.Center, ForeColor = Color.DarkGreen };
            txtDownPayment.TextChanged += (s, e) => { if (!_isUpdating) RecalculateFinanced(); };

            pnlRight.Controls.AddRange(new Control[] { txtDebtAmount, txtDownPayment });
            y += 42;

            // 3. أرباح التقسيط
            pnlRight.Controls.Add(new Label { Text = "نسبة الفائدة / الربح (%):", Location = new Point(265, y), AutoSize = true, ForeColor = Theme.TextSub });
            pnlRight.Controls.Add(new Label { Text = "مبلغ الربح الإضافي (ج):", Location = new Point(55, y), AutoSize = true, ForeColor = Theme.TextSub });
            y += 22;

            txtProfitPct = new TextBox { Location = new Point(230, y), Width = 200, Font = new Font("Segoe UI", 10.5f), Text = "0.0", TextAlign = HorizontalAlignment.Center };
            txtProfitPct.TextChanged += (s, e) => { if (!_isUpdating) CalculateProfitFromPct(); };

            txtProfitAmount = new TextBox { Location = new Point(20, y), Width = 195, Font = new Font("Segoe UI", 10.5f), Text = "0.00", TextAlign = HorizontalAlignment.Center };
            txtProfitAmount.TextChanged += (s, e) => { if (!_isUpdating) CalculateProfitFromAmt(); };

            pnlRight.Controls.AddRange(new Control[] { txtProfitPct, txtProfitAmount });
            y += 42;

            // 4. صافي المبلغ الممول (المقسط)
            pnlRight.Controls.Add(new Label { Text = "💰 صافي المبلغ الإجمالي للأقساط:", Location = new Point(210, y), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.Primary });
            y += 24;
            txtFinancedAmount = new TextBox
            {
                Location = new Point(20, y),
                Width = 410,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Text = "0.00",
                ReadOnly = true,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.DarkBlue,
                TextAlign = HorizontalAlignment.Center
            };
            pnlRight.Controls.Add(txtFinancedAmount);
            y += 46;

            // 5. عدد الأقساط والنظام وتاريخ البداية
            pnlRight.Controls.Add(new Label { Text = "عدد الأقساط:", Location = new Point(330, y), AutoSize = true, ForeColor = Theme.TextMain });
            pnlRight.Controls.Add(new Label { Text = "فترة التكرار:", Location = new Point(195, y), AutoSize = true, ForeColor = Theme.TextMain });
            pnlRight.Controls.Add(new Label { Text = "أول قسط:", Location = new Point(50, y), AutoSize = true, ForeColor = Theme.TextMain });
            y += 22;

            nudInstallmentCount = new NumericUpDown
            {
                Location = new Point(310, y),
                Width = 120,
                Minimum = 1,
                Maximum = 120,
                Value = 6,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };

            cboPeriod = new ComboBox
            {
                Location = new Point(170, y),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f)
            };
            cboPeriod.Items.AddRange(new object[] { "شهري", "نصف شهري", "أسبوعي", "ربع سنوي", "سنوي" });
            cboPeriod.SelectedIndex = 0;

            dtpStartDate = new DateTimePicker
            {
                Location = new Point(20, y),
                Width = 140,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddMonths(1),
                Font = new Font("Segoe UI", 10f)
            };

            pnlRight.Controls.AddRange(new Control[] { nudInstallmentCount, cboPeriod, dtpStartDate });
            y += 44;

            // 6. الخزينة النقدية (لتوريد المقدم)
            pnlRight.Controls.Add(new Label { Text = "خزينة استلام المقدم (إن وُجد):", Location = new Point(230, y), AutoSize = true, ForeColor = Theme.TextSub });
            y += 22;
            cboSafeAccount = new ComboBox
            {
                Location = new Point(20, y),
                Width = 410,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            pnlRight.Controls.Add(cboSafeAccount);
            y += 38;

            // 7. الملاحظات
            pnlRight.Controls.Add(new Label { Text = "ملاحظات العقد والإقرار:", Location = new Point(260, y), AutoSize = true, ForeColor = Theme.TextSub });
            y += 20;
            txtNotes = new TextBox
            {
                Location = new Point(20, y),
                Width = 410,
                Height = 45,
                Multiline = true,
                Font = new Font("Segoe UI", 9f)
            };
            pnlRight.Controls.Add(txtNotes);
            y += 55;

            // زر توليد الجدول
            btnGenerateSchedule = Theme.MakeButton("⚡ احتساب وتوليد جدول الأقساط", 20, y, 410, 38, Theme.Primary);
            btnGenerateSchedule.Click += (s, e) => GenerateSchedule();
            pnlRight.Controls.Add(btnGenerateSchedule);

            // ══════ لوحة جدول الأقساط والأزرار (اليسار) ══════
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            // بطاقة ملخص العقد السفلية
            var pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(10, 10, 10, 10),
                RightToLeft = RightToLeft.Yes
            };

            lblSummaryContract = new Label { Text = "📋 إجمالي العقد: 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain, Margin = new Padding(5, 0, 18, 0) };
            lblSummaryInstallment = new Label { Text = "💵 قيمة القسط: 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.DarkGreen, Margin = new Padding(5, 0, 18, 0) };
            lblSummaryEndDate = new Label { Text = "📅 تاريخ الانتهاء: —", AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.DarkBlue, Margin = new Padding(5, 0, 18, 0) };

            pnlSummary.Controls.AddRange(new Control[] { lblSummaryContract, lblSummaryInstallment, lblSummaryEndDate });

            // شريط أزرار الحفظ والطباعة السفلي
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            btnSaveContract = Theme.MakeButton("💾 اعتماد وإنشاء عقد التقسيط", 0, 0, 220, 36, Theme.Success);
            btnSaveContract.Click += BtnSaveContract_Click;

            btnPrintContract = Theme.MakeButton("🖨️ طباعة إقرار وعقد التقسيط", 0, 0, 190, 36, Theme.Secondary);
            btnPrintContract.Click += BtnPrintContract_Click;

            btnCancel = Theme.MakeButton("إلغاء", 0, 0, 90, 36, Color.FromArgb(100, 116, 139));
            btnCancel.Click += (s, e) => this.Close();

            pnlActions.Controls.AddRange(new Control[] { btnSaveContract, btnPrintContract, btnCancel });

            // جدول الأقساط
            dgSchedule = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 }
            };

            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "InstallmentNo", HeaderText = "رقم القسط", FillWeight = 40, ReadOnly = true });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "DueDate", HeaderText = "تاريخ الاستحقاق", FillWeight = 70 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "مبلغ القسط (ج)", FillWeight = 60 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان / الملاحظات", FillWeight = 90 });

            pnlLeft.Controls.Add(dgSchedule);
            pnlLeft.Controls.Add(pnlSummary);
            pnlLeft.Controls.Add(pnlActions);

            this.Controls.Add(pnlLeft);
            this.Controls.Add(pnlRight);
            this.Controls.Add(pnlTop);
        }

        private void LoadClients()
        {
            var dt = ClientDAL.GetAll();
            cboClient.Items.Clear();
            cboClient.Items.Add(new ComboItem(0, "-- اختر العميل من القائمة --"));
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["ClientID"]);
                string name = r["ClientName"].ToString();
                string phone = r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "";
                string display = string.IsNullOrWhiteSpace(phone) ? name : $"{name} ({phone})";
                cboClient.Items.Add(new ComboItem(id, display));
            }
            cboClient.DisplayMember = "Text";
            cboClient.SelectedIndex = 0;
        }

        private void SelectClientByID(int clientID)
        {
            for (int i = 0; i < cboClient.Items.Count; i++)
            {
                if (cboClient.Items[i] is ComboItem ci && ci.ID == clientID)
                {
                    cboClient.SelectedIndex = i;
                    break;
                }
            }
        }

        private void LoadSafes()
        {
            var dt = DbHelper.Query("SELECT AccountID, AccountName FROM Accounts WHERE IsActive = 1");
            cboSafeAccount.Items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                cboSafeAccount.Items.Add(new ComboItem(Convert.ToInt32(r["AccountID"]), r["AccountName"].ToString()));
            }
            cboSafeAccount.DisplayMember = "Text";
            if (cboSafeAccount.Items.Count > 0) cboSafeAccount.SelectedIndex = 0;
        }

        private void CboClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                _clientCurrentBalance = ClientDAL.GetBalance(ci.ID);
                lblCurrentBalance.Text = $"{_clientCurrentBalance:N2} ج";

                if (_clientCurrentBalance > 0)
                {
                    txtDebtAmount.Text = _clientCurrentBalance.ToString("F2");
                }
                else
                {
                    txtDebtAmount.Text = "0.00";
                    MessageBox.Show("تنبيه: هذا العميل ليس عليه مديونية حالية (الرصيد 0 أو دائن). يمكنك كتابة مبلغ يدوي للتقسيط إذا رغبت.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                RecalculateFinanced();
            }
            else
            {
                lblCurrentBalance.Text = "0.00 ج";
                txtDebtAmount.Text = "0.00";
                RecalculateFinanced();
            }
        }

        private void RecalculateFinanced()
        {
            decimal.TryParse(txtDebtAmount.Text.Trim(), out decimal debt);
            decimal.TryParse(txtDownPayment.Text.Trim(), out decimal down);
            decimal.TryParse(txtProfitAmount.Text.Trim(), out decimal profit);

            decimal financed = Math.Max(0m, (debt - down) + profit);
            _isUpdating = true;
            txtFinancedAmount.Text = financed.ToString("F2");
            _isUpdating = false;
        }

        private void CalculateProfitFromPct()
        {
            decimal.TryParse(txtDebtAmount.Text.Trim(), out decimal debt);
            decimal.TryParse(txtDownPayment.Text.Trim(), out decimal down);
            decimal.TryParse(txtProfitPct.Text.Trim(), out decimal pct);

            decimal principal = Math.Max(0m, debt - down);
            decimal profit = principal * (pct / 100m);

            _isUpdating = true;
            txtProfitAmount.Text = profit.ToString("F2");
            RecalculateFinanced();
            _isUpdating = false;
        }

        private void CalculateProfitFromAmt()
        {
            decimal.TryParse(txtDebtAmount.Text.Trim(), out decimal debt);
            decimal.TryParse(txtDownPayment.Text.Trim(), out decimal down);
            decimal.TryParse(txtProfitAmount.Text.Trim(), out decimal profit);

            decimal principal = Math.Max(0m, debt - down);
            decimal pct = principal > 0 ? (profit / principal) * 100m : 0m;

            _isUpdating = true;
            txtProfitPct.Text = pct.ToString("F1");
            RecalculateFinanced();
            _isUpdating = false;
        }

        private void GenerateSchedule()
        {
            decimal.TryParse(txtDebtAmount.Text.Trim(), out decimal debt);
            decimal.TryParse(txtDownPayment.Text.Trim(), out decimal down);
            decimal.TryParse(txtProfitAmount.Text.Trim(), out decimal profit);
            decimal.TryParse(txtFinancedAmount.Text.Trim(), out decimal financed);

            int count = (int)nudInstallmentCount.Value;
            if (count <= 0) count = 1;

            if (financed <= 0)
            {
                MessageBox.Show("يرجى التأكد من أن المبلغ المقسط أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal rawMonthly = Math.Round(financed / count, 2);
            _generatedSchedule.Clear();
            dgSchedule.Rows.Clear();

            DateTime currentDate = dtpStartDate.Value.Date;
            decimal totalAssigned = 0m;

            for (int i = 1; i <= count; i++)
            {
                decimal instAmt = rawMonthly;
                if (i == count)
                {
                    // تسوية أي كسور في القسط الأخير
                    instAmt = financed - totalAssigned;
                }
                totalAssigned += instAmt;

                var dto = new InstallmentScheduleDTO
                {
                    InstallmentNo = i,
                    DueDate = currentDate,
                    Amount = instAmt,
                    PaidAmount = 0,
                    RemainingAmount = instAmt,
                    Status = "Pending"
                };
                _generatedSchedule.Add(dto);

                dgSchedule.Rows.Add(i, currentDate.ToString("yyyy-MM-dd"), instAmt.ToString("N2"), $"قسط {i} من {count}");

                // حساب تاريخ القسط التالي
                if (cboPeriod.SelectedIndex == 0) // شهري
                    currentDate = currentDate.AddMonths(1);
                else if (cboPeriod.SelectedIndex == 1) // نصف شهري
                    currentDate = currentDate.AddDays(15);
                else if (cboPeriod.SelectedIndex == 2) // أسبوعي
                    currentDate = currentDate.AddDays(7);
                else if (cboPeriod.SelectedIndex == 3) // ربع سنوي
                    currentDate = currentDate.AddMonths(3);
                else if (cboPeriod.SelectedIndex == 4) // سنوي
                    currentDate = currentDate.AddYears(1);
            }

            lblSummaryContract.Text = $"📋 إجمالي العقد: {financed + down:N2} ج";
            lblSummaryInstallment.Text = $"💵 قيمة القسط: {rawMonthly:N2} ج";
            lblSummaryEndDate.Text = $"📅 تاريخ الانتهاء: {_generatedSchedule[count - 1].DueDate:yyyy/MM/dd}";
        }

        private void BtnSaveContract_Click(object sender, EventArgs e)
        {
            if (!(cboClient.SelectedItem is ComboItem ci) || ci.ID <= 0)
            {
                MessageBox.Show("يرجى اختيار العميل أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_generatedSchedule.Count == 0)
            {
                GenerateSchedule();
            }

            decimal.TryParse(txtDebtAmount.Text.Trim(), out decimal debt);
            decimal.TryParse(txtDownPayment.Text.Trim(), out decimal down);
            decimal.TryParse(txtProfitAmount.Text.Trim(), out decimal profit);
            decimal.TryParse(txtFinancedAmount.Text.Trim(), out decimal financed);
            int count = (int)nudInstallmentCount.Value;
            decimal instValue = count > 0 ? financed / count : 0;

            int safeId = (cboSafeAccount.SelectedItem is ComboItem si) ? si.ID : Session.GetDefaultSafeID();

            // قراءة المبالغ المعدلة من الجدول
            for (int i = 0; i < dgSchedule.Rows.Count; i++)
            {
                if (i < _generatedSchedule.Count)
                {
                    if (decimal.TryParse(dgSchedule.Rows[i].Cells["Amount"].Value?.ToString(), out decimal editedAmt))
                    {
                        _generatedSchedule[i].Amount = editedAmt;
                        _generatedSchedule[i].RemainingAmount = editedAmt;
                    }
                    if (DateTime.TryParse(dgSchedule.Rows[i].Cells["DueDate"].Value?.ToString(), out DateTime editedDate))
                    {
                        _generatedSchedule[i].DueDate = editedDate;
                    }
                }
            }

            string clientName = ci.Name;
            string confirmMsg = $"هل تريد اعتماد عقد تقسيط مديونية للعميل [{clientName}]؟\n\n" +
                                $"• أصل المديونية: {debt:N2} ج\n" +
                                $"• المقدم المسدد: {down:N2} ج\n" +
                                $"• أرباح التقسيط المضافة: {profit:N2} ج\n" +
                                $"• إجمالي الأقساط ({count} قسط): {financed:N2} ج\n" +
                                $"• أول قسط: {dtpStartDate.Value:yyyy/MM/dd}";

            if (MessageBox.Show(confirmMsg, "تأكيد اعتماد عقد التقسيط", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    int contractID = InstallmentDAL.CreateDebtInstallmentContract(
                        ci.ID, debt, down, profit, count, instValue, dtpStartDate.Value, _generatedSchedule, txtNotes.Text.Trim(), safeId);

                    if (contractID > 0)
                    {
                        MessageBox.Show($"✅ تم اعتماد وإنشاء عقد تقسيط المديونية بنجاح برقم [{contractID}].\nتم تسجيل جدول الأقساط وقيد الحسابات والتأثير على الخزينة.", "تم الاعتماد", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        if (MessageBox.Show("هل ترغب في طباعة عقد التقسيط وإقرار السداد الآن؟", "طباعة العقد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            PrintContractDocument(ci.Name, debt, down, profit, financed, count);
                        }

                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء اعتماد العقد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnPrintContract_Click(object sender, EventArgs e)
        {
            if (!(cboClient.SelectedItem is ComboItem ci) || ci.ID <= 0)
            {
                MessageBox.Show("يرجى اختيار العميل وتوليد الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_generatedSchedule.Count == 0)
            {
                GenerateSchedule();
            }

            decimal.TryParse(txtDebtAmount.Text.Trim(), out decimal debt);
            decimal.TryParse(txtDownPayment.Text.Trim(), out decimal down);
            decimal.TryParse(txtProfitAmount.Text.Trim(), out decimal profit);
            decimal.TryParse(txtFinancedAmount.Text.Trim(), out decimal financed);
            int count = (int)nudInstallmentCount.Value;

            PrintContractDocument(ci.Name, debt, down, profit, financed, count);
        }

        private void PrintContractDocument(string clientName, decimal debt, decimal down, decimal profit, decimal financed, int count)
        {
            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 35;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontSub = new Font("Segoe UI", 11f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9.5f);
                var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);

                // إطار خارجي
                g.DrawRectangle(Pens.DarkSlateBlue, 30, 20, ev.PageBounds.Width - 60, ev.PageBounds.Height - 40);

                // عنوان
                g.DrawString("عقد وإقرار جدولة وتقسيط مديونية", fontTitle, Brushes.DarkSlateBlue, new PointF(ev.PageBounds.Width / 2 - 170, y));
                y += 35;
                g.DrawString($"تاريخ التحرير: {DateTime.Now:yyyy/MM/dd}   |   رقم العقد: {InstallmentDAL.GenerateDebtContractCode()}", fontBody, Brushes.Gray, new PointF(ev.PageBounds.Width / 2 - 140, y));
                y += 35;

                // بيانات الطرفين والاتفاق
                g.FillRectangle(Brushes.GhostWhite, 40, y, ev.PageBounds.Width - 80, 75);
                g.DrawRectangle(Pens.LightSlateGray, 40, y, ev.PageBounds.Width - 80, 75);

                g.DrawString($"الطرف الأول (الدائن): إدارة المنشأة", fontSub, Brushes.Black, 50, y + 8);
                g.DrawString($"الطرف الثاني (المدين): {clientName}", fontSub, Brushes.DarkBlue, 420, y + 8);
                y += 32;

                g.DrawString($"أصل المديونية: {debt:N2} ج  |  الدفعة المقدمة: {down:N2} ج  |  أرباح وجدولة: {profit:N2} ج  |  إجمالي المبلغ المقسط: {financed:N2} ج", fontHeader, Brushes.DarkGreen, 50, y + 8);
                y += 52;

                // جدول الأقساط
                g.DrawString("📅 جدول استحقاق ومواعيد سداد الأقساط:", fontSub, Brushes.Black, 40, y);
                y += 24;

                float[] colWidths = { 60, 140, 110, 160, 110 };
                string[] headers = { "رقم", "تاريخ الاستحقاق", "مبلغ القسط", "حالة القسط", "توقيع السداد" };

                float x = 40;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightSteelBlue, x, y, colWidths[i], 24);
                    g.DrawRectangle(Pens.SlateGray, x, y, colWidths[i], 24);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 5, y + 3);
                    x += colWidths[i];
                }
                y += 24;

                foreach (var sch in _generatedSchedule)
                {
                    if (y > ev.PageBounds.Height - 160) break;
                    x = 40;
                    string[] vals = {
                        sch.InstallmentNo.ToString(),
                        sch.DueDate.ToString("yyyy/MM/dd"),
                        $"{sch.Amount:N2} ج",
                        "مستحق السداد",
                        "..............."
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 22);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 4, y + 3);
                        x += colWidths[i];
                    }
                    y += 22;
                }

                y += 25;
                // الإقرار والتوقيعات
                g.DrawString("إقرار المدين: أقر أنا الموقع أدناه بصحة المديونية الموضحة أعلاه والتزامي التام بسداد الأقساط بمواعيدها المحددة.", fontBody, Brushes.Black, 40, y);
                y += 35;

                g.DrawString("توقيع الطرف الأول (الدائن): .........................", fontSub, Brushes.Black, 50, y);
                g.DrawString("توقيع الطرف الثاني (المدين): .........................", fontSub, Brushes.Black, 420, y);
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 850, Height = 700 })
            {
                dlg.ShowDialog(this);
            }
        }
    }
}

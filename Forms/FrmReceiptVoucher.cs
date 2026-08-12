using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة مستقلة لإصدار وتتبع وإدارة سندات الصرف والتوريد النقدي (Receipt & Payment Vouchers)
    /// </summary>
    public class FrmReceiptVoucher : Form
    {
        private FlowLayoutPanel pnlFilter;
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboVoucherTypeFilter;
        private ComboBox cboSafeFilter;
        private TextBox txtSearch;
        private Button btnLoad;

        private TableLayoutPanel pnlKPIs;
        private Label lblTotalIn, lblTotalOut, lblNetBalance, lblVoucherCount;

        private DataGridView dgVouchers;

        public FrmReceiptVoucher()
        {
            InitUI();
            LoadVouchers();
        }

        private void InitUI()
        {
            this.Text = "إصدار وإدارة سندات الصرف والتوريد النقدي";
            this.Size = new Size(1150, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── Top Filter Panel ──
            pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true
            };

            pnlFilter.Controls.Add(new Label { Text = "من تاريخ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 0, 0) });
            dtpFrom = new DateTimePicker
            {
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd",
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                Margin = new Padding(5, 4, 0, 0)
            };
            dtpFrom.ValueChanged += (s, e) => LoadVouchers();
            pnlFilter.Controls.Add(dtpFrom);

            pnlFilter.Controls.Add(new Label { Text = "إلى تاريخ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            dtpTo = new DateTimePicker
            {
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd",
                Value = DateTime.Now,
                Margin = new Padding(5, 4, 0, 0)
            };
            dtpTo.ValueChanged += (s, e) => LoadVouchers();
            pnlFilter.Controls.Add(dtpTo);

            pnlFilter.Controls.Add(new Label { Text = "نوع السند:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            cboVoucherTypeFilter = new ComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 4, 0, 0)
            };
            cboVoucherTypeFilter.Items.AddRange(new object[] { "الكل", "🟢 سند توريد (قبض)", "🔴 سند صرف (دفوعات)", "🔄 تحويل نقدية" });
            cboVoucherTypeFilter.SelectedIndex = 0;
            cboVoucherTypeFilter.SelectedIndexChanged += (s, e) => LoadVouchers();
            pnlFilter.Controls.Add(cboVoucherTypeFilter);

            pnlFilter.Controls.Add(new Label { Text = "الحساب / الخزنة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            cboSafeFilter = new ComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 4, 0, 0)
            };
            LoadSafesFilterCombo();
            cboSafeFilter.SelectedIndexChanged += (s, e) => LoadVouchers();
            pnlFilter.Controls.Add(cboSafeFilter);

            pnlFilter.Controls.Add(new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            txtSearch = new TextBox
            {
                Width = 140,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5, 4, 0, 0)
            };
            txtSearch.TextChanged += (s, e) => FilterGridLocally();
            pnlFilter.Controls.Add(txtSearch);

            btnLoad = Theme.MakeButton("🔄 تحديث", Theme.Accent);
            btnLoad.Size = new Size(80, 32);
            btnLoad.Margin = new Padding(10, 0, 0, 0);
            btnLoad.Click += (s, e) => LoadVouchers();
            pnlFilter.Controls.Add(btnLoad);

            // ── Primary Actions Bar ──
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true
            };

            var btnNewDeposit = Theme.MakeButton("🟢 + سند توريد نقدي (قبض)", Color.FromArgb(40, 140, 80));
            btnNewDeposit.Size = new Size(190, 38);
            btnNewDeposit.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnNewDeposit.Margin = new Padding(5, 0, 5, 0);
            btnNewDeposit.Click += (s, e) => ShowIssueVoucherModal(true);
            pnlActions.Controls.Add(btnNewDeposit);

            var btnNewWithdraw = Theme.MakeButton("🔴 - سند صرف نقدي (دفع)", Color.FromArgb(190, 60, 60));
            btnNewWithdraw.Size = new Size(180, 38);
            btnNewWithdraw.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnNewWithdraw.Margin = new Padding(5, 0, 5, 0);
            btnNewWithdraw.Click += (s, e) => ShowIssueVoucherModal(false);
            pnlActions.Controls.Add(btnNewWithdraw);

            var btnNewTransfer = Theme.MakeButton("🔄 تحويل نقدية", Color.FromArgb(100, 70, 160));
            btnNewTransfer.Size = new Size(130, 38);
            btnNewTransfer.Font = Theme.FontBold;
            btnNewTransfer.Margin = new Padding(5, 0, 5, 0);
            btnNewTransfer.Click += BtnTransfer_Click;
            pnlActions.Controls.Add(btnNewTransfer);

            var btnPrintSelected = Theme.MakeButton("🖨️ طباعة السند المحدد", Color.FromArgb(16, 185, 129));
            btnPrintSelected.Size = new Size(160, 38);
            btnPrintSelected.Font = Theme.FontBold;
            btnPrintSelected.Margin = new Padding(15, 0, 5, 0);
            btnPrintSelected.Click += BtnPrintSelected_Click;
            pnlActions.Controls.Add(btnPrintSelected);

            var btnDeleteSelected = Theme.MakeButton("🗑️ حذف السند", Color.FromArgb(120, 40, 40));
            btnDeleteSelected.Size = new Size(110, 38);
            btnDeleteSelected.Font = Theme.FontBold;
            btnDeleteSelected.Margin = new Padding(5, 0, 5, 0);
            btnDeleteSelected.Click += BtnDeleteSelected_Click;
            pnlActions.Controls.Add(btnDeleteSelected);

            // ── KPI Summary Bar ──
            pnlKPIs = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                ColumnCount = 4,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgCard,
                Padding = new Padding(5, 5, 5, 5)
            };
            for (int i = 0; i < 4; i++) pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblTotalIn = new Label { Text = "إجمالي المقبوضات: 0.00 ج", ForeColor = Color.LightGreen, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            lblTotalOut = new Label { Text = "إجمالي المدفوعات: 0.00 ج", ForeColor = Color.OrangeRed, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            lblNetBalance = new Label { Text = "صافي الحركة: 0.00 ج", ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            lblVoucherCount = new Label { Text = "عدد السندات: 0", ForeColor = Color.FromArgb(180, 200, 230), Font = new Font("Segoe UI", 10f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

            pnlKPIs.Controls.Add(lblTotalIn, 0, 0);
            pnlKPIs.Controls.Add(lblTotalOut, 1, 0);
            pnlKPIs.Controls.Add(lblNetBalance, 2, 0);
            pnlKPIs.Controls.Add(lblVoucherCount, 3, 0);

            // ── DataGridView Container ──
            dgVouchers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransID", HeaderText = "رقم السند", FillWeight = 25 });
            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ والوقت", FillWeight = 40 });
            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع السند", FillWeight = 35 });
            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "AmountIn", HeaderText = "وارد (قبض)", FillWeight = 30 });
            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "AmountOut", HeaderText = "صادر (صرف)", FillWeight = 30 });
            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "الحساب / الخزنة", FillWeight = 40 });
            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان والتفاصيل المحاسبية", FillWeight = 90 });
            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "محرر السند", FillWeight = 35 });

            dgVouchers.DoubleClick += (s, e) => BtnPrintSelected_Click(null, null);

            var pnlGridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGridContainer.Controls.Add(dgVouchers);

            this.Controls.Add(pnlGridContainer);
            this.Controls.Add(pnlKPIs);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlFilter);

            Theme.ApplyFormRTL(this);
        }

        private void LoadSafesFilterCombo()
        {
            try
            {
                cboSafeFilter.Items.Clear();
                cboSafeFilter.Items.Add(new ComboItem(0, "-- كل الحسابات --"));

                DataTable safes = AccountDAL.GetActiveSafeAccounts();
                foreach (DataRow row in safes.Rows)
                {
                    cboSafeFilter.Items.Add(new ComboItem(
                        Convert.ToInt32(row["AccountID"]),
                        row["AccountName"].ToString()
                    ));
                }
                cboSafeFilter.DisplayMember = "Text";
                cboSafeFilter.SelectedIndex = 0;

                if (!Session.CanChangeSafe("CashBox"))
                {
                    cboSafeFilter.Enabled = false;
                }
            }
            catch { }
        }

        private DataTable _dtLoadedVouchers;

        private void LoadVouchers()
        {
            try
            {
                int? accId = null;
                if (cboSafeFilter.SelectedItem is ComboItem item && item.ID > 0)
                {
                    accId = item.ID;
                }

                _dtLoadedVouchers = AccountDAL.GetCashBox(dtpFrom.Value, dtpTo.Value, accId);
                FilterGridLocally();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل السندات: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterGridLocally()
        {
            if (_dtLoadedVouchers == null) return;

            dgVouchers.Rows.Clear();

            int voucherTypeIdx = cboVoucherTypeFilter.SelectedIndex; // 0=All, 1=Deposit, 2=Withdraw, 3=Transfer
            string search = txtSearch.Text.Trim().ToLower();

            decimal sumIn = 0, sumOut = 0;
            int count = 0;

            foreach (DataRow r in _dtLoadedVouchers.Rows)
            {
                int transID = Convert.ToInt32(r["CashID"]);
                string rawType = r["TransType"].ToString();
                decimal inAmt = Convert.ToDecimal(r["AmountIn"]);
                decimal outAmt = Convert.ToDecimal(r["AmountOut"]);
                string notes = r["Notes"].ToString();
                string accName = r.Table.Columns.Contains("AccountName") && r["AccountName"] != DBNull.Value ? r["AccountName"].ToString() : "الخزينة الرئيسية";

                // Voucher Type Filter
                if (voucherTypeIdx == 1 && (inAmt == 0 || rawType == "Transfer")) continue;
                if (voucherTypeIdx == 2 && (outAmt == 0 || rawType == "Transfer")) continue;
                if (voucherTypeIdx == 3 && rawType != "Transfer" && !rawType.Contains("ShiftClose")) continue;

                // Search filter
                if (!string.IsNullOrEmpty(search))
                {
                    string searchableText = $"{transID} {rawType} {notes} {accName}".ToLower();
                    if (!searchableText.Contains(search)) continue;
                }

                string typeArabic = rawType switch
                {
                    "Deposit" => "🟢 سند توريد",
                    "Withdraw" => "🔴 سند صرف",
                    "SaleIncome" => "🛒 بيع نقدي",
                    "ClientPayment" => "🟢 تحصيل عميل",
                    "SupplierPayment" => "🔴 صرف للمورد",
                    "Expense" => "🔴 مصروفات",
                    "Transfer" => "🔄 تحويل نقدية",
                    _ => rawType
                };

                int ri = dgVouchers.Rows.Add(
                    transID,
                    Convert.ToDateTime(r["TransDate"]).ToString("yyyy/MM/dd HH:mm"),
                    typeArabic,
                    inAmt > 0 ? inAmt.ToString("N2") : "",
                    outAmt > 0 ? outAmt.ToString("N2") : "",
                    accName,
                    notes,
                    r.Table.Columns.Contains("CreatedByName") ? r["CreatedByName"].ToString() : "المشرف"
                );

                if (outAmt > 0) dgVouchers.Rows[ri].DefaultCellStyle.ForeColor = Color.OrangeRed;
                else if (inAmt > 0) dgVouchers.Rows[ri].DefaultCellStyle.ForeColor = Color.LightGreen;

                sumIn += inAmt;
                sumOut += outAmt;
                count++;
            }

            lblTotalIn.Text = "إجمالي المقبوضات: " + sumIn.ToString("N2") + " ج";
            lblTotalOut.Text = "إجمالي المدفوعات: " + sumOut.ToString("N2") + " ج";
            lblNetBalance.Text = "صافي الحركة: " + (sumIn - sumOut).ToString("N2") + " ج";
            lblVoucherCount.Text = "عدد السندات: " + count;
        }

        private void ShowIssueVoucherModal(bool isDeposit)
        {
            if (!Session.CanAdd("CashBox"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية إصدار سندات نقدية.", "تنفيذي", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dlg = new Form
            {
                Text = isDeposit ? "🟢 إصدار سند توريد نقدي (قبض)" : "🔴 إصدار سند صرف نقدي (دفع)",
                Size = new Size(540, 560),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = isDeposit ? Color.FromArgb(30, 90, 50) : Color.FromArgb(140, 45, 45)
            };
            var lblTitle = new Label
            {
                Text = isDeposit ? "🟢 نموذج إصدار سند توريد نقدي (سند قبض)" : "🔴 نموذج إصدار سند صرف نقدي",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblTitle);

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 15),
                ColumnCount = 2,
                RowCount = 7,
                RightToLeft = RightToLeft.Yes
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            for (int i = 0; i < 7; i++) tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));

            // Row 0: Date
            tblFields.Controls.Add(new Label { Text = "تاريخ السند:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0) }, 0, 0);
            var dtpVoucherDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Now, Margin = new Padding(0, 8, 0, 8) };
            tblFields.Controls.Add(dtpVoucherDate, 1, 0);

            // Row 1: Safe Account
            tblFields.Controls.Add(new Label { Text = "الحساب / الخزنة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0) }, 0, 1);
            var cboSafe = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 8, 0, 8) };
            try
            {
                DataTable safes = AccountDAL.GetActiveSafeAccounts();
                foreach (DataRow row in safes.Rows)
                {
                    cboSafe.Items.Add(new ComboItem(Convert.ToInt32(row["AccountID"]), row["AccountName"].ToString()));
                }
                cboSafe.DisplayMember = "Text";
                cboSafe.SelectedIndex = 0;
            }
            catch { }
            tblFields.Controls.Add(cboSafe, 1, 1);

            // Row 2: Amount
            tblFields.Controls.Add(new Label { Text = "المبلغ النقدي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0) }, 0, 2);
            var nudAmount = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 999999999m,
                DecimalPlaces = 2,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = isDeposit ? Color.LightGreen : Color.OrangeRed,
                Margin = new Padding(0, 8, 0, 8)
            };
            tblFields.Controls.Add(nudAmount, 1, 2);

            // Row 3: Beneficiary Type
            tblFields.Controls.Add(new Label { Text = "جهة التعامل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0) }, 0, 3);
            var cboPartyType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 8, 0, 8)
            };
            cboPartyType.Items.AddRange(new object[] { "تعامل عام / جهة خارجية", "عميل (تحصيل / دفع)", "مورد (سداد / استرداد)", "مصروف تشغيلي" });
            cboPartyType.SelectedIndex = 0;
            tblFields.Controls.Add(cboPartyType, 1, 3);

            // Row 4: Classification
            tblFields.Controls.Add(new Label { Text = "التصنيف المحاسبي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0) }, 0, 4);
            var cboClassification = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 8, 0, 8)
            };
            if (isDeposit)
            {
                cboClassification.Items.Add(new ComboItem(-1, "تحصيل / تسوية نقدية عامة"));
                cboClassification.Items.Add(new ComboItem(1, "تمويل زيادة رأس المال (Capital)"));
                cboClassification.Items.Add(new ComboItem(2, "قروض مستلمة (ShortTermLoans)"));
                cboClassification.Items.Add(new ComboItem(3, "إيرادات أخرى متنوعة (OtherRevenues)"));
            }
            else
            {
                cboClassification.Items.Add(new ComboItem(-1, "سداد / تسوية نقدية عامة"));
                cboClassification.Items.Add(new ComboItem(1, "مسحوبات شخصية للشركاء (Drawings)"));
                cboClassification.Items.Add(new ComboItem(2, "عهود وسلف الموظفين (Custodies)"));
                cboClassification.Items.Add(new ComboItem(3, "سداد قروض مستحقة (ShortTermLoans)"));
            }
            cboClassification.SelectedIndex = 0;
            tblFields.Controls.Add(cboClassification, 1, 4);

            // Row 5: Notes
            tblFields.Controls.Add(new Label { Text = "البيان والسبب التفصيلي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 12, 0, 0) }, 0, 5);
            var txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 8, 0, 8)
            };
            tblFields.Controls.Add(txtNotes, 1, 5);

            // Row 6: Print Checkbox
            var chkPrint = new CheckBox
            {
                Text = "🖨️ معاينة وطباعة السند فور الحفظ",
                Checked = true,
                AutoSize = true,
                ForeColor = Theme.Accent,
                Font = Theme.FontBold,
                Dock = DockStyle.Fill
            };
            tblFields.Controls.Add(chkPrint, 1, 6);

            // Bottom Save Buttons
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgSearchPanel, Padding = new Padding(15, 8, 15, 8) };
            var btnSave = Theme.MakeButton(isDeposit ? "💾 إصدار سند التوريد" : "💾 إصدار سند الصرف", isDeposit ? Color.FromArgb(40, 130, 70) : Color.FromArgb(170, 50, 50));
            btnSave.Size = new Size(200, 38);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSave.Dock = DockStyle.Left;

            var btnCancel = Theme.MakeButton("إلغاء", Color.FromArgb(80, 80, 90));
            btnCancel.Size = new Size(90, 38);
            btnCancel.Dock = DockStyle.Right;
            btnCancel.Click += (s, e) => dlg.Close();

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);

            btnSave.Click += (s, e) =>
            {
                if (nudAmount.Value <= 0)
                {
                    MessageBox.Show("⚠️ يرجى إدخال مبلغ أكبر من الصفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                {
                    MessageBox.Show("⚠️ يرجى كتابة البيان والسبب المحاسبي للسند.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!(cboSafe.SelectedItem is ComboItem safeItem))
                {
                    MessageBox.Show("⚠️ يرجى اختيار الحساب المالي / الخزنة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    int safeID = safeItem.ID;
                    decimal amount = nudAmount.Value;
                    string userNotes = txtNotes.Text.Trim();
                    string partyType = cboPartyType.Text;
                    string classText = cboClassification.Text;
                    string formattedNotes = $"[{partyType}] {userNotes}";

                    int newTransID = 0;

                    if (isDeposit)
                    {
                        newTransID = DbHelper.ExecuteInsert(@"
                            INSERT INTO CashBox(TransDate, TransType, AmountIn, Notes, CreatedBy, AccountID)
                            VALUES(@d, 'Deposit', @amt, @n, @by, @accId)",
                            DbHelper.P("@d", dtpVoucherDate.Value),
                            DbHelper.P("@amt", amount),
                            DbHelper.P("@n", formattedNotes),
                            DbHelper.P("@by", Session.EmpID),
                            DbHelper.P("@accId", safeID));
                    }
                    else
                    {
                        decimal currentBalance = AccountDAL.GetCashBalance(safeID);
                        if (amount > currentBalance)
                        {
                            var confirm = MessageBox.Show($"⚠️ رصيد الخزنة المقتطعة ({currentBalance:N2} ج) أقل من مبلغ الصرف المطلوب ({amount:N2} ج)!\nهل تريد متابعة الصرف واستكمال السند رغماً عن العجز الدفتري؟", "تأكيد تجاوز الرصيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (confirm != DialogResult.Yes) return;
                        }

                        newTransID = DbHelper.ExecuteInsert(@"
                            INSERT INTO CashBox(TransDate, TransType, AmountOut, Notes, CreatedBy, AccountID)
                            VALUES(@d, 'Withdraw', @amt, @n, @by, @accId)",
                            DbHelper.P("@d", dtpVoucherDate.Value),
                            DbHelper.P("@amt", amount),
                            DbHelper.P("@n", formattedNotes),
                            DbHelper.P("@by", Session.EmpID),
                            DbHelper.P("@accId", safeID));
                    }

                    MessageBox.Show($"✅ تم إصدار السند بنجاح! رقم السند: #{newTransID}", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();

                    LoadVouchers();

                    if (chkPrint.Checked && newTransID > 0)
                    {
                        new FrmPrintPayment(newTransID, "AlTarekVoucher", true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل حفظ السند: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.Add(tblFields);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlHeader);

            Theme.ApplyFormRTL(dlg);
            dlg.ShowDialog();
        }

        private void BtnTransfer_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("CashBox"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية التحويل بين الحسابات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            new FrmCashBox().ShowDialog();
            LoadVouchers();
        }

        private void BtnPrintSelected_Click(object sender, EventArgs e)
        {
            if (dgVouchers.CurrentRow == null)
            {
                MessageBox.Show("يرجى تحديد السند من القائمة أولاً للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int transID = Convert.ToInt32(dgVouchers.CurrentRow.Cells["TransID"].Value);
            new FrmPrintPayment(transID, "AlTarekVoucher", true);
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (!Session.CanDelete("CashBox"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية حذف السندات المالي.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (dgVouchers.CurrentRow == null)
            {
                MessageBox.Show("يرجى تحديد السند المراد حذفه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int transID = Convert.ToInt32(dgVouchers.CurrentRow.Cells["TransID"].Value);
            string notes = dgVouchers.CurrentRow.Cells["Notes"].Value.ToString();

            if (MessageBox.Show($"هل أنت أيد أنك تريد حذف السند رقم #{transID}؟\nالبيان: {notes}", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    DbHelper.Execute("DELETE FROM CashBox WHERE CashID = @id", DbHelper.P("@id", transID));
                    MessageBox.Show("✅ تم حذف السند بنجاح.", "تم الحذف", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadVouchers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل حذف السند: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

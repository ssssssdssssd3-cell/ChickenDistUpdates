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
    /// مع إظهار وتتبع رصيد العميل أو المورد في السند
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
                Width = 190,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd hh:mm tt",
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0),
                Margin = new Padding(5, 4, 0, 0)
            };
            dtpFrom.ValueChanged += (s, e) => LoadVouchers();
            pnlFilter.Controls.Add(dtpFrom);

            pnlFilter.Controls.Add(new Label { Text = "إلى تاريخ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0) });
            dtpTo = new DateTimePicker
            {
                Width = 190,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd hh:mm tt",
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

            var btnWhatsApp = Theme.MakeButton("📱 إرسال واتساب للعميل", Color.FromArgb(37, 211, 102));
            btnWhatsApp.Size = new Size(170, 38);
            btnWhatsApp.Font = Theme.FontBold;
            btnWhatsApp.ForeColor = Color.White;
            btnWhatsApp.Margin = new Padding(5, 0, 5, 0);
            btnWhatsApp.Click += BtnWhatsApp_Click;
            pnlActions.Controls.Add(btnWhatsApp);

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

            lblTotalIn = new Label { Text = "إجمالي المقبوضات: 0.00 ج", ForeColor = Color.FromArgb(20, 160, 60), Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            lblTotalOut = new Label { Text = "إجمالي المدفوعات: 0.00 ج", ForeColor = Color.FromArgb(220, 40, 40), Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            lblNetBalance = new Label { Text = "صافي الحركة: 0.00 ج", ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            lblVoucherCount = new Label { Text = "عدد السندات: 0", ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

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
            dgVouchers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان والتفاصيل المحاسبية (شاملاً رصيد المتعامل)", FillWeight = 110 });
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
                DataTable safes = AccountDAL.GetAllowedSafeAccounts();

                bool canSwitch = Session.IsAdmin || (Session.CanChangeSafe("ReceiptVoucher") && safes.Rows.Count > 1);

                if (canSwitch)
                {
                    cboSafeFilter.Items.Add(new ComboItem(0, "-- كل الحسابات --"));
                }

                foreach (DataRow row in safes.Rows)
                {
                    cboSafeFilter.Items.Add(new ComboItem(
                        Convert.ToInt32(row["AccountID"]),
                        row["AccountName"].ToString()
                    ));
                }
                cboSafeFilter.DisplayMember = "Text";

                int defSafeId = Session.GetPrimaryAllowedSafeID();
                int selectIdx = 0;
                for (int i = 0; i < cboSafeFilter.Items.Count; i++)
                {
                    if (cboSafeFilter.Items[i] is ComboItem item && item.ID == defSafeId)
                    {
                        selectIdx = i;
                        break;
                    }
                }
                if (cboSafeFilter.Items.Count > 0) cboSafeFilter.SelectedIndex = selectIdx;

                if (!canSwitch)
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

                if (!Session.IsAdmin)
                {
                    bool canSwitch = Session.CanChangeSafe("ReceiptVoucher");
                    var allowed = Session.GetAllowedSafeIDSet();
                    if (!canSwitch || accId == null || accId == 0 || (allowed != null && !allowed.Contains(accId.Value)))
                    {
                        accId = Session.GetPrimaryAllowedSafeID();
                    }
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

                if (outAmt > 0) dgVouchers.Rows[ri].DefaultCellStyle.ForeColor = Color.FromArgb(185, 25, 25);
                else if (inAmt > 0) dgVouchers.Rows[ri].DefaultCellStyle.ForeColor = Color.FromArgb(15, 125, 45);

                sumIn += inAmt;
                sumOut += outAmt;
                count++;
            }

            lblTotalIn.Text = "إجمالي المقبوضات: " + sumIn.ToString("N2") + " ج";
            lblTotalOut.Text = "إجمالي المدفوعات: " + sumOut.ToString("N2") + " ج";
            lblNetBalance.Text = "صافي الحركة: " + (sumIn - sumOut).ToString("N2") + " ج";
            lblVoucherCount.Text = "عدد السندات: " + count;
        }

        private void ShowIssueVoucherModal(bool isDeposit, int defaultPartyType = 0)
        {
            if (!Session.CanAdd("ReceiptVoucher") && !Session.CanAdd("CashBox") && !Session.CanAccess("ReceiptVoucher") && !Session.CanAccess("CashBox"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية إصدار سندات نقدية.", "تنفيذي", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dlg = new Form
            {
                Text = isDeposit ? "🟢 إصدار سند توريد نقدي (قبض)" : "🔴 إصدار سند صرف نقدي (دفع) / تحويل",
                Size = new Size(620, 720),
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
                Text = isDeposit ? "🟢 نموذج إصدار سند توريد نقدي (قبض)" : "🔴 نموذج إصدار سند صرف نقدي / تحويل بين الأدراج",
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
                RowCount = 9,
                RightToLeft = RightToLeft.Yes
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));

            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // 0: Date
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // 1: Source Safe
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // 2: PartyType
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // 3: Dynamic Selector
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 75f)); // 4: Multi-Balance Badge
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // 5: Amount
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // 6: Classification
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // 7: Notes
            tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // 8: Print Checkbox

            // Row 0: Date
            tblFields.Controls.Add(new Label { Text = "تاريخ السند:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) }, 0, 0);
            var dtpVoucherDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Now, Margin = new Padding(0, 4, 0, 4) };
            tblFields.Controls.Add(dtpVoucherDate, 1, 0);

            // Row 1: Source Safe Account / Drawer
            tblFields.Controls.Add(new Label { Text = "الدرج / الخزنة المصدر:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) }, 0, 1);
            var cboSafe = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 4, 0, 4) };
            try
            {
                DataTable safes = AccountDAL.GetAllowedSafeAccounts();
                foreach (DataRow row in safes.Rows)
                {
                    cboSafe.Items.Add(new ComboItem(Convert.ToInt32(row["AccountID"]), row["AccountName"].ToString()));
                }
                cboSafe.DisplayMember = "Text";
                
                int defSafeId = Session.GetPrimaryAllowedSafeID();
                int selectIdx = 0;
                for (int i = 0; i < cboSafe.Items.Count; i++)
                {
                    if (cboSafe.Items[i] is ComboItem item && item.ID == defSafeId)
                    {
                        selectIdx = i;
                        break;
                    }
                }
                if (cboSafe.Items.Count > 0) cboSafe.SelectedIndex = selectIdx;

                if (!Session.IsAdmin && (!Session.CanChangeSafe("ReceiptVoucher") || safes.Rows.Count <= 1))
                {
                    cboSafe.Enabled = false;
                }
            }
            catch { }
            tblFields.Controls.Add(cboSafe, 1, 1);

            // Row 2: Party Type (عميل | مورد | مصروفات تشغيل | تحويل بين الأدراج | تعامل عام)
            tblFields.Controls.Add(new Label { Text = "جهة التعامل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) }, 0, 2);
            var cboPartyType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 4, 0, 4)
            };
            cboPartyType.Items.AddRange(new object[] {
                "عميل (تحصيل / دفع)",
                "مورد (سداد للمورد / استرداد)",
                "مصروفات تشغيل (Operational Expenses)",
                "تحويل نقدية بين الأدراج / الخزن (Transfer)",
                "تعامل عام / جهة خارجية"
            });
            cboPartyType.SelectedIndex = Math.Min(Math.Max(0, defaultPartyType), 4);
            tblFields.Controls.Add(cboPartyType, 1, 2);

            // Row 3: Dynamic Controls Container
            var lblPartyNameLbl = new Label { Text = "اختر العميل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) };
            tblFields.Controls.Add(lblPartyNameLbl, 0, 3);

            var pnlPartyContainer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };

            var txtGeneralPartyName = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Visible = false };

            // ── Client Search (TextBox + Search Button) ─────────────────────────
            int selectedClientID = 0;
            var pnlClientSearch = new Panel { Dock = DockStyle.Fill, Visible = true };
            var txtClientName = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Text = "-- اضغط 🔍 للبحث عن عميل --",
                Cursor = Cursors.Hand,
                Font = Theme.FontBold
            };
            var btnClientSearchPick = new Button
            {
                Text = "🔍",
                Width = 38,
                Dock = DockStyle.Left,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = Theme.FontBold
            };
            btnClientSearchPick.FlatAppearance.BorderSize = 0;
            pnlClientSearch.Controls.Add(txtClientName);
            pnlClientSearch.Controls.Add(btnClientSearchPick);
            txtClientName.Click += (s, e) => btnClientSearchPick.PerformClick();

            // ── Supplier Search (TextBox + Search Button) ────────────────────────
            int selectedSupplierID = 0;
            var pnlSupplierSearch = new Panel { Dock = DockStyle.Fill, Visible = false };
            var txtSupplierName = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Text = "-- اضغط 🔍 للبحث عن مورد --",
                Cursor = Cursors.Hand,
                Font = Theme.FontBold
            };
            var btnSupplierSearchPick = new Button
            {
                Text = "🔍",
                Width = 38,
                Dock = DockStyle.Left,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = Theme.FontBold
            };
            btnSupplierSearchPick.FlatAppearance.BorderSize = 0;
            pnlSupplierSearch.Controls.Add(txtSupplierName);
            pnlSupplierSearch.Controls.Add(btnSupplierSearchPick);
            txtSupplierName.Click += (s, e) => btnSupplierSearchPick.PerformClick();

            var cboExpenseType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Visible = false };
            var cboTargetSafe = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat, Visible = false };

            // Load Expense Categories
            cboExpenseType.Items.AddRange(new object[] {
                "إيجارات وشواغر",
                "كهرباء ومياه وغاز",
                "صيانة وتجهيزات",
                "نقل وانتقالات ومصاريف شحن",
                "أدوات كتابية ومطبوعات",
                "صيانة وإصلاح سيارات",
                "إكراميات ونثريات عامة",
                "مصروفات تسويق وإعلانات"
            });
            if (cboExpenseType.Items.Count > 0) cboExpenseType.SelectedIndex = 0;

            // Load Target Safes
            Action reloadTargetSafes = () =>
            {
                cboTargetSafe.Items.Clear();
                try
                {
                    int currentSrcID = (cboSafe.SelectedItem is ComboItem si) ? si.ID : 0;
                    DataTable safes = AccountDAL.GetActiveSafeAccounts();
                    foreach (DataRow row in safes.Rows)
                    {
                        int id = Convert.ToInt32(row["AccountID"]);
                        if (id != currentSrcID)
                        {
                            cboTargetSafe.Items.Add(new ComboItem(id, row["AccountName"].ToString()));
                        }
                    }
                    if (cboTargetSafe.Items.Count > 0) cboTargetSafe.SelectedIndex = 0;
                }
                catch { }
            };
            reloadTargetSafes();

            // (Wire-up for client/supplier search buttons is done below, after updatePartyBalance is declared)

            pnlPartyContainer.Controls.Add(txtGeneralPartyName);
            pnlPartyContainer.Controls.Add(pnlClientSearch);
            pnlPartyContainer.Controls.Add(pnlSupplierSearch);
            pnlPartyContainer.Controls.Add(cboExpenseType);
            pnlPartyContainer.Controls.Add(cboTargetSafe);
            tblFields.Controls.Add(pnlPartyContainer, 1, 3);

            // Row 4: Multi-Balance Interactive Badge Banner
            tblFields.Controls.Add(new Label { Text = "الأرصدة الحالية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 15, 0, 0) }, 0, 4);

            var pnlBalanceBadge = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6),
                Margin = new Padding(0, 2, 0, 2)
            };
            var lblPartyBalance = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.Gold,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "يرجى تحديد جهة التعامل لعرض تفاصيل الرصيد"
            };
            pnlBalanceBadge.Controls.Add(lblPartyBalance);
            tblFields.Controls.Add(pnlBalanceBadge, 1, 4);

            // Row 5: Amount
            tblFields.Controls.Add(new Label { Text = "المبلغ النقدي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) }, 0, 5);
            var nudAmount = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 999999999m,
                DecimalPlaces = 2,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = isDeposit ? Color.LightGreen : Color.OrangeRed,
                Margin = new Padding(0, 4, 0, 4)
            };
            tblFields.Controls.Add(nudAmount, 1, 5);

            // Row 6: Classification
            tblFields.Controls.Add(new Label { Text = "التصنيف المحاسبي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) }, 0, 6);
            var cboClassification = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 4, 0, 4)
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
            tblFields.Controls.Add(cboClassification, 1, 6);

            // Row 7: Notes
            tblFields.Controls.Add(new Label { Text = "البيان والسبب التفصيلي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(0, 10, 0, 0) }, 0, 7);
            var txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 4, 0, 4)
            };
            tblFields.Controls.Add(txtNotes, 1, 7);

            // ── Multi-Balance Updating Action ──
            Action updatePartyBalance = () =>
            {
                try
                {
                    decimal amt = nudAmount.Value;
                    int srcSafeID = (cboSafe.SelectedItem is ComboItem sItem) ? sItem.ID : 1;
                    string srcSafeName = (cboSafe.SelectedItem is ComboItem sItem2) ? sItem2.Text : "الدرج الرئيسي";
                    decimal srcBal = AccountDAL.GetCashBalance(srcSafeID);

                    int pIdx = cboPartyType.SelectedIndex;

                    if (pIdx == 0 && selectedClientID > 0)
                    {
                        decimal curBal = ClientDAL.GetFinancialStatus(selectedClientID).Balance;
                        string curState = curBal > 0 ? "عليه (مدين)" : (curBal < 0 ? "له (دائن)" : "خالص (0.00 ج)");
                        decimal afterBal = isDeposit ? (curBal - amt) : (curBal + amt);
                        string afterState = afterBal > 0 ? "عليه (مدين)" : (afterBal < 0 ? "له (دائن)" : "خالص (0.00 ج)");

                        lblPartyBalance.Text = $"💰 رصيد الخزنة المصدر ({srcSafeName}): {srcBal:N2} ج\n👤 رصيد العميل الحالي: {Math.Abs(curBal):N2} ج ({curState}) ➔ المتوقع: {Math.Abs(afterBal):N2} ج ({afterState})";
                        lblPartyBalance.ForeColor = Color.Gold;
                    }
                    else if (pIdx == 1 && selectedSupplierID > 0)
                    {
                        decimal curBal = SupplierDAL.GetBalance(selectedSupplierID);
                        string curState = curBal > 0 ? "له للمورد (دائن)" : (curBal < 0 ? "عليه للمورد (مدين)" : "خالص (0.00 ج)");
                        decimal afterBal = isDeposit ? (curBal + amt) : (curBal - amt);
                        string afterState = afterBal > 0 ? "له للمورد (دائن)" : (afterBal < 0 ? "عليه للمورد (مدين)" : "خالص (0.00 ج)");

                        lblPartyBalance.Text = $"💰 رصيد الخزنة المصدر ({srcSafeName}): {srcBal:N2} ج\n🏬 رصيد المورد الحالي: {Math.Abs(curBal):N2} ج ({curState}) ➔ المتوقع: {Math.Abs(afterBal):N2} ج ({afterState})";
                        lblPartyBalance.ForeColor = Color.Cyan;
                    }
                    else if (pIdx == 2)
                    {
                        string expName = cboExpenseType.Text;
                        lblPartyBalance.Text = $"💰 رصيد الخزنة المصدر ({srcSafeName}): {srcBal:N2} ج\n📋 بند المصروف التشغيلي: {expName}";
                        lblPartyBalance.ForeColor = Color.Orange;
                    }
                    else if (pIdx == 3)
                    {
                        int targetSafeID = (cboTargetSafe.SelectedItem is ComboItem tItem) ? tItem.ID : 0;
                        string targetName = (cboTargetSafe.SelectedItem is ComboItem tItem2) ? tItem2.Text : "الدرج المستلم";
                        decimal targetBal = AccountDAL.GetCashBalance(targetSafeID);

                        decimal srcAfter = srcBal - amt;
                        decimal targetAfter = targetBal + amt;

                        lblPartyBalance.Text = $"📤 الدرج المصدر ({srcSafeName}): {srcBal:N2} ج (المتوقع: {srcAfter:N2} ج)\n📥 الدرج المستلم ({targetName}): {targetBal:N2} ج (المتوقع: {targetAfter:N2} ج)";
                        lblPartyBalance.ForeColor = Color.LightGreen;
                    }
                    else
                    {
                        lblPartyBalance.Text = $"💰 رصيد الخزنة المصدر ({srcSafeName}): {srcBal:N2} ج\nيرجى تحديد جهة التعامل لعرض تفاصيل الرصيد";
                        lblPartyBalance.ForeColor = Color.White;
                    }
                }
                catch
                {
                    lblPartyBalance.Text = "---";
                }
            };

            cboSafe.SelectedIndexChanged += (s, e) =>
            {
                reloadTargetSafes();
                updatePartyBalance();
            };

            // Wire client search button (after updatePartyBalance is declared)
            btnClientSearchPick.Click += (s, e) =>
            {
                using (var frm = new FrmClientSearch())
                {
                    if (frm.ShowDialog() == DialogResult.OK && frm.SelectedClientID > 0)
                    {
                        selectedClientID = frm.SelectedClientID;
                        var dt = DbHelper.Query("SELECT ClientName FROM Clients WHERE ClientID=@id", DbHelper.P("@id", selectedClientID));
                        txtClientName.Text = dt.Rows.Count > 0 ? dt.Rows[0]["ClientName"].ToString() : "---";
                        txtClientName.ForeColor = Theme.TextMain;
                        updatePartyBalance();
                    }
                }
            };

            // Wire supplier search button (after updatePartyBalance is declared)
            btnSupplierSearchPick.Click += (s, e) =>
            {
                using (var frm = new FrmSupplierSearch())
                {
                    if (frm.ShowDialog() == DialogResult.OK && frm.SelectedSupplierID > 0)
                    {
                        selectedSupplierID = frm.SelectedSupplierID;
                        var dt = DbHelper.Query("SELECT SupplierName FROM Suppliers WHERE SupplierID=@id", DbHelper.P("@id", selectedSupplierID));
                        txtSupplierName.Text = dt.Rows.Count > 0 ? dt.Rows[0]["SupplierName"].ToString() : "---";
                        txtSupplierName.ForeColor = Theme.TextMain;
                        updatePartyBalance();
                    }
                }
            };

            cboPartyType.SelectedIndexChanged += (s, e) =>
            {
                int idx = cboPartyType.SelectedIndex;
                pnlClientSearch.Visible   = (idx == 0);
                pnlSupplierSearch.Visible = (idx == 1);
                cboExpenseType.Visible    = (idx == 2);
                cboTargetSafe.Visible     = (idx == 3);
                txtGeneralPartyName.Visible = (idx == 4);

                if (idx == 0) lblPartyNameLbl.Text = "اختر العميل:";
                else if (idx == 1) lblPartyNameLbl.Text = "اختر المورد:";
                else if (idx == 2) lblPartyNameLbl.Text = "بند المصروف:";
                else if (idx == 3) lblPartyNameLbl.Text = "الدرج المستلم:";
                else lblPartyNameLbl.Text = "اسم الجهة:";

                updatePartyBalance();
            };

            cboTargetSafe.SelectedIndexChanged += (s, e) => updatePartyBalance();
            cboExpenseType.TextChanged += (s, e) => updatePartyBalance();
            nudAmount.ValueChanged += (s, e) => updatePartyBalance();

            // Initial view state trigger
            cboPartyType.SelectedIndex = Math.Min(Math.Max(0, defaultPartyType), 4);
            updatePartyBalance();

            // Bottom Save Buttons
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgSearchPanel, Padding = new Padding(15, 8, 15, 8) };
            var btnSave = Theme.MakeButton(isDeposit ? "💾 إصدار سند التوريد" : "💾 إصدار سند الصرف / التحويل", isDeposit ? Color.FromArgb(40, 130, 70) : Color.FromArgb(170, 50, 50));
            btnSave.Size = new Size(220, 38);
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
                if (!(cboSafe.SelectedItem is ComboItem safeItem))
                {
                    MessageBox.Show("⚠️ يرجى اختيار الحساب المالي / الخزنة المصدر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Validate party selection
                int partyIdx0 = cboPartyType.SelectedIndex;
                if (partyIdx0 == 0 && selectedClientID <= 0)
                {
                    MessageBox.Show("⚠️ يرجى البحث عن عميل واختياره أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (partyIdx0 == 1 && selectedSupplierID <= 0)
                {
                    MessageBox.Show("⚠️ يرجى البحث عن مورد واختياره أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    int safeID = safeItem.ID;
                    decimal amount = nudAmount.Value;
                    string userNotes = txtNotes.Text.Trim();
                    int partyIdx = cboPartyType.SelectedIndex;

                    // تحقق مسبق من رصيد الخزنة/الدرج لكافة سندات الصرف
                    if (!isDeposit)
                    {
                        decimal safeBalance = AccountDAL.GetCashBalance(safeID);
                        if (safeBalance < amount)
                        {
                            MessageBox.Show($"⛔ غير مسموح بالصرف على المكشوف أو تحويل الحساب لرصيد سالب!\nالرصيد المتاح حالياً في [{safeItem.Text}] هو ({safeBalance:N2} ج) فقط، بينما مبلغ السند المطلوب صرفه هو ({amount:N2} ج).\nيرجى توريد نقدية أولاً أو اختيار خزنة أخرى بها رصيد كافٍ.", "رصيد غير كافٍ بالخزنة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    int newTransID = 0;

                    // 0: Client
                    if (partyIdx == 0 && selectedClientID > 0)
                    {
                        string clientName = txtClientName.Text;
                        decimal curBal = ClientDAL.GetFinancialStatus(selectedClientID).Balance;
                        string curState = curBal > 0 ? "عليه" : (curBal < 0 ? "له" : "خالص");

                        if (isDeposit)
                        {
                            ClientDAL.AddPayment(selectedClientID, amount, $"{userNotes} (رصيد العميل قبل: {Math.Abs(curBal):N2} ج {curState})", safeID);
                            newTransID = Convert.ToInt32(DbHelper.Scalar("SELECT TOP 1 CashID FROM CashBox ORDER BY CashID DESC"));
                        }
                        else
                        {
                            DbHelper.RunInTransaction((con, trans) =>
                            {
                                AccountDAL.EnsureSufficientCashTrans(trans, safeID, amount, "صرف نقدية لعميل");

                                DbHelper.ExecuteTrans(trans,
                                    "INSERT INTO ClientTransactions(ClientID,TransType,Debit,Notes,CreatedBy) VALUES(@id,'Withdraw',@amt,@n,@by)",
                                    DbHelper.P("@id", selectedClientID), DbHelper.P("@amt", amount),
                                    DbHelper.P("@n", "سند صرف - " + userNotes), DbHelper.P("@by", Session.EmpID));

                                DbHelper.ExecuteTrans(trans,
                                    "INSERT INTO CashBox(TransDate,TransType,AmountOut,Notes,CreatedBy,AccountID) VALUES(GETDATE(),'Withdraw',@amt,@n,@by,@accId)",
                                    DbHelper.P("@amt", amount),
                                    DbHelper.P("@n", $"[عميل: {clientName}] {userNotes} (رصيد العميل قبل: {Math.Abs(curBal):N2} ج {curState})"),
                                    DbHelper.P("@by", Session.EmpID),
                                    DbHelper.P("@accId", safeID));
                            });
                            newTransID = Convert.ToInt32(DbHelper.Scalar("SELECT TOP 1 CashID FROM CashBox ORDER BY CashID DESC"));
                        }
                    }
                    // 1: Supplier
                    else if (partyIdx == 1 && selectedSupplierID > 0)
                    {
                        string supplierName = txtSupplierName.Text;
                        decimal curBal = SupplierDAL.GetBalance(selectedSupplierID);
                        string curState = curBal > 0 ? "له للمورد" : (curBal < 0 ? "عليه للمورد" : "خالص");

                        if (!isDeposit)
                        {
                            SupplierDAL.AddSupplierPayment(selectedSupplierID, amount, $"{userNotes} (رصيد المورد قبل: {Math.Abs(curBal):N2} ج {curState})", safeAccountID: safeID);
                            newTransID = Convert.ToInt32(DbHelper.Scalar("SELECT TOP 1 CashID FROM CashBox ORDER BY CashID DESC"));
                        }
                        else
                        {
                            DbHelper.RunInTransaction((con, trans) =>
                            {
                                DbHelper.ExecuteTrans(trans,
                                    "INSERT INTO SupplierTransactions(SupplierID,TransType,Credit,Notes,CreatedBy) VALUES(@id,'Refund',@amt,@n,@by)",
                                    DbHelper.P("@id", selectedSupplierID), DbHelper.P("@amt", amount),
                                    DbHelper.P("@n", "سند توريد - " + userNotes), DbHelper.P("@by", Session.EmpID));

                                DbHelper.ExecuteTrans(trans,
                                    "INSERT INTO CashBox(TransDate,TransType,AmountIn,Notes,CreatedBy,AccountID) VALUES(GETDATE(),'Deposit',@amt,@n,@by,@accId)",
                                    DbHelper.P("@amt", amount),
                                    DbHelper.P("@n", $"[مورد: {supplierName}] {userNotes} (رصيد المورد قبل: {Math.Abs(curBal):N2} ج {curState})"),
                                    DbHelper.P("@by", Session.EmpID),
                                    DbHelper.P("@accId", safeID));
                            });
                            newTransID = Convert.ToInt32(DbHelper.Scalar("SELECT TOP 1 CashID FROM CashBox ORDER BY CashID DESC"));
                        }
                    }
                    // 2: Operational Expenses
                    else if (partyIdx == 2)
                    {
                        string expTypeStr = cboExpenseType.Text.Trim();
                        if (string.IsNullOrWhiteSpace(expTypeStr)) expTypeStr = "مصروفات تشغيل عامة";
                        string fullExpNotes = string.IsNullOrWhiteSpace(userNotes) ? expTypeStr : $"{expTypeStr} - {userNotes}";

                        newTransID = AccountDAL.SaveExpense(0, dtpVoucherDate.Value, expTypeStr, amount, fullExpNotes, safeAccountID: safeID);
                    }
                    // 3: Drawer Transfer
                    else if (partyIdx == 3)
                    {
                        if (!(cboTargetSafe.SelectedItem is ComboItem targetItem))
                        {
                            MessageBox.Show("⚠️ يرجى اختيار الدرج / الخزنة المستلمة المحول إليها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        AccountDAL.TransferFunds(safeID, targetItem.ID, amount, userNotes);
                        newTransID = Convert.ToInt32(DbHelper.Scalar("SELECT TOP 1 CashID FROM CashBox ORDER BY CashID DESC"));
                    }
                    // 4: General Party
                    else
                    {
                        string beneficiary = string.IsNullOrWhiteSpace(txtGeneralPartyName.Text) ? "تعامل عام" : txtGeneralPartyName.Text.Trim();
                        string formattedNotes = $"[{beneficiary}] {userNotes}";

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
                            DbHelper.RunInTransaction((con, trans) =>
                            {
                                AccountDAL.EnsureSufficientCashTrans(trans, safeID, amount, "صرف نقدية لجهة عامة");

                                newTransID = DbHelper.ExecuteInsertTrans(trans, @"
                                    INSERT INTO CashBox(TransDate, TransType, AmountOut, Notes, CreatedBy, AccountID)
                                    VALUES(@d, 'Withdraw', @amt, @n, @by, @accId)",
                                    DbHelper.P("@d", dtpVoucherDate.Value),
                                    DbHelper.P("@amt", amount),
                                    DbHelper.P("@n", formattedNotes),
                                    DbHelper.P("@by", Session.EmpID),
                                    DbHelper.P("@accId", safeID));
                            });
                        }
                    }

                    // ── Close dialog first ──────────────────────────────────────
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadVouchers();

                    MessageBox.Show($"✅ تم إصدار السند بنجاح! رقم السند: #{newTransID}", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // ── سؤال الطباعة ────────────────────────────────────────────
                    if (newTransID > 0)
                    {
                        var printResult = MessageBox.Show(
                            $"🖨️ هل تريد طباعة السند رقم #{newTransID} الآن؟",
                            "طباعة السند",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);

                        if (printResult == DialogResult.Yes)
                        {
                            new FrmPrintPayment(newTransID, "AlTarekVoucher", true);
                        }

                        // ── سؤال الإرسال عبر الواتساب ───────────────────────────
                        var waResult = MessageBox.Show(
                            $"📱 هل تريد إرسال السند رقم #{newTransID} عبر الواتساب؟",
                            "إرسال الواتساب",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);

                        if (waResult == DialogResult.Yes)
                        {
                            try
                            {
                                // Get phone based on party type
                                string phone = "";
                                if (partyIdx == 0 && selectedClientID > 0)
                                {
                                    var cRow = DbHelper.Query("SELECT Phone FROM Clients WHERE ClientID=@id", DbHelper.P("@id", selectedClientID));
                                    if (cRow.Rows.Count > 0) phone = cRow.Rows[0]["Phone"]?.ToString() ?? "";
                                }
                                else if (partyIdx == 1 && selectedSupplierID > 0)
                                {
                                    var sRow = DbHelper.Query("SELECT Phone FROM Suppliers WHERE SupplierID=@id", DbHelper.P("@id", selectedSupplierID));
                                    if (sRow.Rows.Count > 0) phone = sRow.Rows[0]["Phone"]?.ToString() ?? "";
                                }

                                string partyName = partyIdx == 0 ? txtClientName.Text
                                                 : partyIdx == 1 ? txtSupplierName.Text
                                                 : partyIdx == 4 ? txtGeneralPartyName.Text
                                                 : "جهة تعامل";

                                string voucherLabel = isDeposit ? "سند توريد (قبض)" : "سند صرف (دفع)";
                                string msg = $"📄 *{voucherLabel} رقم: #{newTransID}*\n"
                                           + $"📅 *التاريخ:* {DateTime.Now:yyyy-MM-dd HH:mm}\n"
                                           + $"👤 *الجهة:* {partyName}\n"
                                           + $"💵 *المبلغ:* {amount:N2} ج\n"
                                           + (!string.IsNullOrWhiteSpace(userNotes) ? $"📝 *البيان:* {userNotes}\n" : "")
                                           + $"\nشكراً لتعاملكم معنا! 🙏";

                                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                                    this,
                                    phone,
                                    msg,
                                    () => ReceiptImageGenerator.GenerateVoucherReceiptImage(newTransID),
                                    "📱 إرسال السند عبر الواتساب");
                            }
                            catch (Exception waEx)
                            {
                                AppLogger.Error("FrmReceiptVoucher.SaveVoucher.WhatsApp", waEx);
                                MessageBox.Show("فشل تجهيز الواتساب: " + waEx.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
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
            if (!Session.CanAdd("ReceiptVoucher") && !Session.CanAdd("CashBox") && !Session.CanAccess("ReceiptVoucher") && !Session.CanAccess("CashBox"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية التحويل بين الحسابات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowIssueVoucherModal(isDeposit: false, defaultPartyType: 3);
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

        private void BtnWhatsApp_Click(object sender, EventArgs e)
        {
            if (dgVouchers.CurrentRow == null)
            {
                MessageBox.Show("يرجى تحديد السند من القائمة أولاً للإرسال عبر الواتساب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int voucherID = Convert.ToInt32(dgVouchers.CurrentRow.Cells["TransID"].Value);
                var dt = DbHelper.Query(@"
                    SELECT v.VoucherCode, v.VoucherDate, v.VoucherType, v.Amount, v.Notes,
                           c.ClientName, c.Phone
                    FROM ReceiptVouchers v
                    LEFT JOIN Clients c ON v.ClientID = c.ClientID
                    WHERE v.VoucherID = @id", DbHelper.P("@id", voucherID));

                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];
                string phone = r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "";
                string code = r["VoucherCode"].ToString();
                decimal amount = Convert.ToDecimal(r["Amount"]);
                string party = r["ClientName"] != DBNull.Value ? r["ClientName"].ToString() : "عميل عام";
                string notes = r["Notes"] != DBNull.Value ? r["Notes"].ToString() : "";

                string msg = $"📄 *إيصال نقدية رقم: #{code}*\n" +
                             $"📅 *التاريخ:* {Convert.ToDateTime(r["VoucherDate"]):yyyy-MM-dd HH:mm}\n" +
                             $"👤 *العميل:* {party}\n" +
                             $"💵 *المبلغ:* {amount:N2} ج\n" +
                             (!string.IsNullOrWhiteSpace(notes) ? $"📝 *الملاحظات:* {notes}\n" : "") +
                             $"\nشكراً لتعاملكم معنا! 🙏";

                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                    this,
                    phone,
                    msg,
                    () => ReceiptImageGenerator.GenerateVoucherReceiptImage(voucherID),
                    "📱 إرسال السند عبر الواتساب");
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmReceiptVoucher.BtnWhatsApp_Click", ex);
                MessageBox.Show("فشل تجهيز الواتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "⛔ نعتذر! غير مسموح بحذف أو إلغاء السندات المالية بعد إصدارها وإغلاقها محاسبياً للحفاظ على الرقابة المالية وسلامة القيود والدفاتر.\n\n" +
                "💡 لتصحيح أي قيد أو تسوية مبلغ، يرجى إصدار (سند صرف) أو (سند توريد) جديد بقيمة التسوية بدلاً من الحذف المباشر.",
                "حظر حذف السندات المالي المحاسبي",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}

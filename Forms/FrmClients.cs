using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة إدارة العملاء</summary>
    public class FrmClients : Form
    {
        private Panel pnlHeader;
        private DataGridView dgClients;
        private TextBox txtSearch, txtCode, txtName, txtPhone, txtPhone2, txtAddress, txtNotes;
        private NumericUpDown nudOpening, nudCreditLimit, nudOpeningCrates;
        private ComboBox cmbDriver, cmbPriceTier, cmbPaymentType;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete, btnStatement, btnSearch, btnPayment, btnAdjustment, btnSalesReport, btnItemizedStatement;
        private Label lblBalance, lblSummary;
        private int _selectedID = 0;

        public FrmClients()
        {
            InitUI();
            LoadDrivers();
            LoadClients();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = "إدارة العملاء والمديونيات";
            this.Size = new Size(1150, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350f)); // Column 0 (Right): Customer Form Card (350px)
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // Column 1 (Left): Grid Table
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Left: Grid panel
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = Theme.BgMain };
            
            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            lblSummary = new Label
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 196, 15),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 8, 0, 0),
                Text = "👥 إجمالي العملاء: --  |  💰 إجمالي المديونيات: -- ج"
            };

            var lblSearch = new Label
            {
                Text = "🔍 بحث العملاء:",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Padding = new Padding(0, 8, 6, 0)
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Right,
                Width = 280,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Text = "بحث بالاسم أو الهاتف أو الكود...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "بحث بالاسم أو الهاتف أو الكود...") txtSearch.Text = ""; };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "بحث بالاسم أو الهاتف أو الكود..."; };
            txtSearch.TextChanged += (s, e) => {
                string searchVal = txtSearch.Text;
                if (searchVal == "بحث بالاسم أو الهاتف أو الكود...") searchVal = "";
                LoadClients(searchVal);
            };

            btnSearch = Theme.MakeButton("🔍", Color.FromArgb(37, 99, 235));
            btnSearch.Dock = DockStyle.Right;
            btnSearch.Width = 45;
            btnSearch.Click += (s, e) => {
                string searchVal = txtSearch.Text == "بحث بالاسم أو الهاتف أو الكود..." ? "" : txtSearch.Text;
                LoadClients(searchVal);
            };
            txtSearch.KeyDown += (s, e) => { 
                if (e.KeyCode == Keys.Enter) {
                    string searchVal = txtSearch.Text == "بحث بالاسم أو الهاتف أو الكود..." ? "" : txtSearch.Text;
                    LoadClients(searchVal);
                }
            };

            var btnFixDuplicates = Theme.MakeButton("🔍 الأكواد المكررة", Color.FromArgb(180, 83, 9));
            btnFixDuplicates.Dock = DockStyle.Left;
            btnFixDuplicates.Width = 145;
            btnFixDuplicates.Click += (s, e) => FixDuplicateClientCodes();

            pnlSearch.Controls.Add(lblSummary);
            pnlSearch.Controls.Add(btnFixDuplicates);
            pnlSearch.Controls.Add(btnSearch);
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(lblSearch);

            dgClients = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(226, 232, 240),
                RowTemplate = { Height = 34 },
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(15, 23, 42),
                    SelectionBackColor = Color.FromArgb(37, 99, 235),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(248, 250, 252),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    SelectionBackColor = Color.FromArgb(37, 99, 235),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(15, 23, 42),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientID", Visible = false });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientCode", HeaderText = "الكود", FillWeight = 30 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "اسم العميل", FillWeight = 110 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "الهاتف", FillWeight = 60 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد المالي", FillWeight = 55 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CratesBalance", HeaderText = "رصيد الفوارغ", FillWeight = 40 });
            if (!AppConfig.EnableCratesTracking)
            {
                dgClients.Columns["CratesBalance"].Visible = false;
            }
            dgClients.SelectionChanged += DgClients_SelectionChanged;
            SetupClientsContextMenu();
            
            pnlGrid.Controls.Add(dgClients);
            pnlGrid.Controls.Add(pnlSearch);

            // Right: Detail panel (Customer Form Card)
            var pnlDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 38, 62),
                Padding = new Padding(12),
                AutoScroll = true
            };

            int y = 10;
            var lblDetailsTitle = new Label
            {
                Text = "📋 بطاقة وتفاصيل العميل",
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(241, 196, 15),
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
            };
            pnlDetails.Controls.Add(lblDetailsTitle);
            y += 32;

            pnlDetails.Controls.Add(MakeField("كود العميل:", ref y, out txtCode));
            txtCode.ReadOnly = true;
            txtCode.TabStop = false;
            txtCode.BackColor = Color.FromArgb(241, 245, 249);
            txtCode.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            pnlDetails.Controls.Add(MakeField("اسم العميل:", ref y, out txtName));
            txtName.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

            pnlDetails.Controls.Add(MakeField("الهاتف:", ref y, out txtPhone));
            pnlDetails.Controls.Add(MakeField("هاتف إضافي:", ref y, out txtPhone2));
            pnlDetails.Controls.Add(MakeField("العنوان:", ref y, out txtAddress));

            var lblLimit = new Label { Text = "حد المديونية:", Location = new Point(220, y + 4), AutoSize = true, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnlDetails.Controls.Add(lblLimit);
            nudCreditLimit = new NumericUpDown { Location = new Point(10, y), Width = 205, Height = 28, Minimum = 0, Maximum = 9999999, DecimalPlaces = 2, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            pnlDetails.Controls.Add(nudCreditLimit); y += 34;

            var lblOp = new Label { Text = "رصيد افتتاحي:", Location = new Point(220, y + 4), AutoSize = true, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnlDetails.Controls.Add(lblOp);
            nudOpening = new NumericUpDown { Location = new Point(10, y), Width = 205, Height = 28, Minimum = -999999, Maximum = 9999999, DecimalPlaces = 2, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            pnlDetails.Controls.Add(nudOpening); y += 34;

            var lblOpCrates = new Label { Text = "رصيد الفوارغ الأولي:", Location = new Point(220, y + 4), AutoSize = true, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnlDetails.Controls.Add(lblOpCrates);
            nudOpeningCrates = new NumericUpDown { Location = new Point(10, y), Width = 205, Height = 28, Minimum = -999999, Maximum = 9999999, DecimalPlaces = 0, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            pnlDetails.Controls.Add(nudOpeningCrates);
            if (!AppConfig.EnableCratesTracking)
            {
                lblOpCrates.Visible = false;
                nudOpeningCrates.Visible = false;
            }
            else
            {
                y += 34;
            }

            var lblDriver = new Label { Text = "المندوب الافتراضي:", Location = new Point(220, y + 4), AutoSize = true, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnlDetails.Controls.Add(lblDriver);
            cmbDriver = new ComboBox { Location = new Point(10, y), Width = 205, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            pnlDetails.Controls.Add(cmbDriver); y += 34;

            var lblPriceTier = new Label { Text = "فئة السعر:", Location = new Point(220, y + 4), AutoSize = true, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnlDetails.Controls.Add(lblPriceTier);
            cmbPriceTier = new ComboBox { Location = new Point(10, y), Width = 205, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cmbPriceTier.Items.AddRange(new object[] { "قطاعي", "نصف جملة", "جملة" });
            cmbPriceTier.SelectedIndex = 0;
            pnlDetails.Controls.Add(cmbPriceTier); y += 34;

            var lblPaymentType = new Label { Text = "طريقة الدفع:", Location = new Point(220, y + 4), AutoSize = true, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnlDetails.Controls.Add(lblPaymentType);
            cmbPaymentType = new ComboBox { Location = new Point(10, y), Width = 205, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cmbPaymentType.Items.AddRange(new object[] { "الكل (كاش / آجل)", "كاش فقط (نقدي)", "آجل فقط (مديونية)" });
            cmbPaymentType.SelectedIndex = 0;
            pnlDetails.Controls.Add(cmbPaymentType); y += 34;

            pnlDetails.Controls.Add(MakeField("ملاحظات:", ref y, out txtNotes));

            chkActive = new CheckBox { Text = "العميل نشط ويتعامل حالياً", Location = new Point(10, y), Width = 285, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Checked = true, RightToLeft = RightToLeft.Yes }; y += 32;
            pnlDetails.Controls.Add(chkActive);

            lblBalance = new Label
            {
                Text = "الرصيد الحالي: ---",
                Location = new Point(10, y),
                Width = 305,
                Height = 38,
                AutoSize = false,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(241, 196, 15),
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlDetails.Controls.Add(lblBalance);
            y += 46;

            btnSave = Theme.MakeButton("💾 حفظ التعديل", 215, y, 100, 36, Color.FromArgb(245, 158, 11));
            btnNew = Theme.MakeButton("🆕 عميل جديد", 115, y, 95, 36, Color.FromArgb(16, 185, 129));
            btnDelete = Theme.MakeButton("🗑 إيقاف", 10, y, 100, 36, Color.FromArgb(239, 68, 68)); y += 42;

            btnPayment = Theme.MakeButton("💵 تحصيل نقدية", 215, y, 100, 34, Color.FromArgb(5, 150, 105));
            btnAdjustment = Theme.MakeButton("⚖️ تسوية رصيد", 115, y, 95, 34, Color.FromArgb(71, 85, 105));
            btnStatement = Theme.MakeButton("📄 كشف حساب", 10, y, 100, 34, Color.FromArgb(37, 99, 235)); y += 40;

            btnItemizedStatement = Theme.MakeButton("📦 كشف الأصناف", 165, y, 150, 34, Color.FromArgb(2, 132, 199));
            btnSalesReport = Theme.MakeButton("📊 تقرير المبيعات", 10, y, 150, 34, Color.FromArgb(217, 119, 6)); y += 42;

            var btnWhatsApp = Theme.MakeButton("📲 إرسال كشف الحساب واتساب", 10, y, 305, 36, Color.FromArgb(37, 211, 102));
            btnWhatsApp.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnWhatsApp.ForeColor = Color.White;

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnStatement.Click += BtnStatement_Click;
            btnPayment.Click += BtnPayment_Click;
            btnAdjustment.Click += BtnAdjustment_Click;
            btnSalesReport.Click += BtnSalesReport_Click;
            btnItemizedStatement.Click += (s, e) =>
            {
                if (_selectedID == 0) { MessageBox.Show("اختر عميلاً من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                new FrmClientStatement(_selectedID, txtName.Text, initialTab: 1).ShowDialog();
            };

            btnWhatsApp.Click += (s, e) =>
            {
                if (_selectedID == 0) { MessageBox.Show("اختر عميلاً من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                string phone = txtPhone.Text.Trim();
                string name = txtName.Text.Trim();
                decimal bal = 0m;
                try
                {
                    object balObj = DbHelper.Scalar("SELECT Balance FROM Clients WHERE ClientID = @id", DbHelper.P("@id", _selectedID));
                    if (balObj != null && balObj != DBNull.Value) bal = Convert.ToDecimal(balObj);
                }
                catch { }

                string msg = $"📊 *كشف حساب مالي - {AppConfig.CompanyName}*\n" +
                             $"👤 *العميل:* {name}\n" +
                             $"📅 *التاريخ:* {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                             $"💵 *الرصيد المتبقي/المديونية:* {bal:N2} ج\n" +
                             $"\nنتمنى لكم دوام التوفيق والنجاح! 🙏";

                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                    this,
                    phone,
                    msg,
                    () => ReceiptImageGenerator.GenerateClientStatementImage(_selectedID, name, bal),
                    "📱 إرسال كشف حساب العميل عبر الواتساب");
            };

            pnlDetails.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete, btnStatement, btnPayment, btnAdjustment, btnSalesReport, btnItemizedStatement, btnWhatsApp });

            tbl.Controls.Add(pnlDetails, 0, 0); // Column 0 (Right): Details
            tbl.Controls.Add(pnlGrid, 1, 0);    // Column 1 (Left): Grid
            this.Controls.Add(tbl);
            Theme.ApplyFormRTL(this);
        }

        private Panel MakeField(string label, ref int y, out TextBox txt)
        {
            var p = new Panel { Location = new Point(5, y), Width = 320, Height = 32 };
            p.Controls.Add(new Label { Text = label, Location = new Point(215, 6), AutoSize = true, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            txt = new TextBox { Location = new Point(5, 2), Width = 205, Height = 26, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle };
            p.Controls.Add(txt);
            y += 34;
            return p;
        }

        private void LoadClients(string search = "")
        {
            dgClients.Rows.Clear();
            if (search == "بحث بالاسم أو الهاتف أو الكود...") search = "";

            DataTable dt = string.IsNullOrWhiteSpace(search)
                ? ClientDAL.GetAll()
                : ClientDAL.Search(search);

            // تعطيل AutoSize أثناء التحميل لتسريع عرض العملاء
            dgClients.SuspendLayout();
            var oldMode = dgClients.AutoSizeColumnsMode;
            dgClients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            decimal totalDebt = 0;
            foreach (DataRow r in dt.Rows)
            {
                decimal bal = Convert.ToDecimal(r["Balance"]);
                totalDebt += bal;
                int cratesBal = Convert.ToInt32(r["CratesBalance"]);
                int rowIndex = dgClients.Rows.Add(r["ClientID"], r["ClientCode"], r["ClientName"], r["Phone"], bal.ToString("N2") + " ج", cratesBal.ToString() + " فارغ");
                var row = dgClients.Rows[rowIndex];

                // تنسيق الألوان بشكل مريح ومميز لعمود الرصيد فقط
                if (bal > 0)
                {
                    row.Cells["Balance"].Style.ForeColor = Color.FromArgb(220, 38, 38); // Red
                    row.Cells["Balance"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (bal < 0)
                {
                    row.Cells["Balance"].Style.ForeColor = Color.FromArgb(37, 99, 235); // Blue
                    row.Cells["Balance"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else
                {
                    row.Cells["Balance"].Style.ForeColor = Color.FromArgb(22, 163, 74); // Green
                    row.Cells["Balance"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }

                if (cratesBal > 0)
                {
                    row.Cells["CratesBalance"].Style.ForeColor = Color.FromArgb(217, 119, 6);
                }
                else
                {
                    row.Cells["CratesBalance"].Style.ForeColor = Color.FromArgb(148, 163, 184);
                }
            }

            if (lblSummary != null)
            {
                lblSummary.Text = string.Format("👥 إجمالي العملاء: {0:N0}  |  💰 إجمالي المديونيات: {1:N2} ج", dt.Rows.Count, totalDebt);
            }

            dgClients.AutoSizeColumnsMode = oldMode;
            dgClients.ResumeLayout();
        }

        private void DgClients_SelectionChanged(object sender, EventArgs e)
        {
            if (dgClients.SelectedRows.Count == 0) return;
            var row = dgClients.SelectedRows[0];
            _selectedID = Convert.ToInt32(row.Cells["ClientID"].Value);
            var dr = ClientDAL.GetByID(_selectedID);
            if (dr == null) return;
            txtCode.Text = dr["ClientCode"].ToString();
            txtName.Text = dr["ClientName"].ToString();
            txtPhone.Text = dr["Phone"].ToString();
            txtPhone2.Text = dr["Phone2"].ToString();
            txtAddress.Text = dr["Address"].ToString();
            nudCreditLimit.Value = Convert.ToDecimal(dr["MaxCreditLimit"] == DBNull.Value ? 0 : dr["MaxCreditLimit"]);
            nudOpening.Value = Convert.ToDecimal(dr["OpeningBalance"]);
            nudOpeningCrates.Value = Convert.ToInt32(dr["OpeningCrates"] == DBNull.Value ? 0 : dr["OpeningCrates"]);
            chkActive.Checked = Convert.ToBoolean(dr["IsActive"]);
            if (dr["DriverID"] == DBNull.Value)
                cmbDriver.SelectedIndex = 0;
            else
                cmbDriver.SelectedValue = dr["DriverID"];
            txtNotes.Text = dr["Notes"].ToString();
            cmbPriceTier.Text = dr.Table.Columns.Contains("DefaultPriceTier") && dr["DefaultPriceTier"] != DBNull.Value
                ? dr["DefaultPriceTier"].ToString()
                : "قطاعي";

            string ptype = dr.Table.Columns.Contains("DefaultPaymentType") && dr["DefaultPaymentType"] != DBNull.Value
                ? dr["DefaultPaymentType"].ToString()
                : "Any";
            SetPaymentTypeCombo(ptype);

            if (AppConfig.EnableCratesTracking)
                lblBalance.Text = "الرصيد المالي: " + row.Cells["Balance"].Value + " | الفوارغ: " + row.Cells["CratesBalance"].Value;
            else
                lblBalance.Text = "الرصيد المالي: " + row.Cells["Balance"].Value;
        }

        private string GetSelectedPaymentType()
        {
            if (cmbPaymentType == null || cmbPaymentType.SelectedIndex < 0) return "Any";
            switch (cmbPaymentType.SelectedIndex)
            {
                case 1: return "Cash";
                case 2: return "Credit";
                default: return "Any";
            }
        }

        private void SetPaymentTypeCombo(string ptype)
        {
            if (cmbPaymentType == null) return;
            if (string.Equals(ptype, "Cash", StringComparison.OrdinalIgnoreCase) || ptype == "كاش")
                cmbPaymentType.SelectedIndex = 1;
            else if (string.Equals(ptype, "Credit", StringComparison.OrdinalIgnoreCase) || ptype == "آجل")
                cmbPaymentType.SelectedIndex = 2;
            else
                cmbPaymentType.SelectedIndex = 0;
        }

        private void LoadDrivers()
        {
            try
            {
                var dt = EmployeeDAL.GetDrivers();
                var dr = dt.NewRow();
                dr["EmpID"] = DBNull.Value;
                dr["EmpName"] = "--- بدون مندوب افتراضي ---";
                dt.Rows.InsertAt(dr, 0);

                cmbDriver.DataSource = dt;
                cmbDriver.DisplayMember = "EmpName";
                cmbDriver.ValueMember = "EmpID";
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل قائمة المندوبين:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtCode.Text = ClientDAL.GetNextClientCode();
            txtName.Clear(); txtPhone.Clear(); txtPhone2.Clear(); txtAddress.Clear();
            nudCreditLimit.Value = 0;
            nudOpening.Value = 0;
            nudOpeningCrates.Value = 0;
            chkActive.Checked = true;
            if (cmbDriver.Items.Count > 0) cmbDriver.SelectedIndex = 0;
            if (cmbPriceTier.Items.Count > 0) cmbPriceTier.SelectedIndex = 0;
            if (cmbPaymentType != null && cmbPaymentType.Items.Count > 0) cmbPaymentType.SelectedIndex = 0;
            txtNotes.Clear();
            lblBalance.Text = "الرصيد الحالي: ---";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0 && !Session.CanAdd("Clients"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية إضافة عملاء جُدد!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedID > 0 && !Session.CanEdit("Clients"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية تعديل بيانات العملاء!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم العميل"); return; }

            // ─── فحص تكرار الاسم ───
            if (ClientDAL.IsDuplicateName(txtName.Text.Trim(), _selectedID))
            {
                MessageBox.Show($"⚠️ يوجد عميل آخر بنفس الاسم: \"{txtName.Text.Trim()}\"\nيرجى استخدام اسم مختلف أو البحث عن العميل الموجود.",
                    "تكرار اسم العميل", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            // ─── فحص تكرار رقم الهاتف ───
            if (!string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                if (ClientDAL.IsDuplicatePhone(txtPhone.Text.Trim(), _selectedID))
                {
                    MessageBox.Show($"⚠️ رقم الهاتف \"{txtPhone.Text.Trim()}\" مسجَّل لعميل آخر بالفعل.\nيرجى التحقق من الرقم أو البحث عن العميل الموجود.",
                        "تكرار رقم الهاتف", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPhone.Focus();
                    return;
                }
            }

            // ─── فحص كود العميل وتوليده إن كان فارغاً ومنع التكرار ───
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                txtCode.Text = ClientDAL.GetNextClientCode();
            }
            if (ClientDAL.IsDuplicateCode(txtCode.Text.Trim(), _selectedID))
            {
                string suggestedCode = ClientDAL.GetNextClientCode();
                var dr = MessageBox.Show(
                    $"⚠️ كود العميل \"{txtCode.Text.Trim()}\" مسجَّل لعميل آخر بالفعل!\n\nهل تريد استخدام الكود المقترح تلقائياً: ({suggestedCode})؟",
                    "تكرار كود العميل", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    txtCode.Text = suggestedCode;
                }
                else
                {
                    txtCode.Focus();
                    return;
                }
            }

            int? driverID = null;
            if (cmbDriver.SelectedValue != null && cmbDriver.SelectedValue != DBNull.Value)
                driverID = Convert.ToInt32(cmbDriver.SelectedValue);

            int id = ClientDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtPhone.Text, txtPhone2.Text,
                txtAddress.Text, nudOpening.Value, chkActive.Checked, driverID, nudCreditLimit.Value, txtNotes.Text, cmbPriceTier.Text, (int)nudOpeningCrates.Value, GetSelectedPaymentType());
            if (id > 0) { ClientCache.Refresh(); MessageBox.Show("✅ تم الحفظ"); _selectedID = id; LoadClients(); }
            else MessageBox.Show("❌ فشل الحفظ");
        }

        private void FixDuplicateClientCodes()
        {
            try
            {
                DataTable dtDups = ClientDuplicateDAL.GetDuplicateClientsReport();
                if (dtDups == null || dtDups.Rows.Count == 0)
                {
                    MessageBox.Show("✅ ممتاز: لا توجد أي أكواد عملاء مكررة في قاعدة البيانات، جميع الأكواد فريدة 100%!",
                        "فحص أكواد العملاء", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int dupCount = dtDups.Rows.Count;
                int withTrans = 0;
                if (dtDups.Columns.Contains("HasTransactions"))
                {
                    foreach (DataRow dr in dtDups.Rows)
                    {
                        if (Convert.ToInt32(dr["HasTransactions"]) == 1) withTrans++;
                    }
                }
                int withoutTrans = dupCount - withTrans;

                var res = MessageBox.Show(
                    $"⚠️ تم اكتشاف ({dupCount}) سجل عملاء يشتركون في أكواد مكررة داخل قاعدة البيانات!\n\n" +
                    $"• عملاء لهم حركات وفواتير مسجلة (محميون من التعديل): {withTrans}\n" +
                    $"• عملاء ليس لهم أي حركات على البرنامج (سيتم تعديل أكوادهم فقط): {withoutTrans}\n\n" +
                    "📌 شرط الأمان المعتمد:\n" +
                    "سيتم تعديل كود العميل فقط للعملاء الذين ليس لهم أي حركات على البرنامج، والاحتفاظ التام بكود العميل الذي له حركات مسجلة ولن يتم المساس به إطلاقاً.\n\n" +
                    "هل ترغب في البدء في تصحيح الأكواد الآن؟",
                    "معالجة أكواد العملاء المكررة", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    var (totalFixed, fixLog) = ClientDuplicateDAL.AutoFixDuplicateClientCodes(onlyModifyZeroTransactions: true);
                    ClientCache.Refresh();
                    LoadClients();
                    MessageBox.Show(
                        $"✅ تم بنجاح معالجة وتصحيح ({totalFixed}) عميل من الذين ليس لهم أي حركات!\n\n" +
                        "تمت حماية جميع العملاء ذوي الحركات المحاسبية وبقيت أكوادهم الأصلية كما هي تماماً.",
                        "اكتمال المعالجة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشلت عملية فحص أو معالجة الأكواد المكررة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!Session.CanDelete("Clients"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية حذف وإيقاف العملاء!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedID == 0) return;
            if (MessageBox.Show("إيقاف تفعيل العميل؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                int? driverID = null;
                if (cmbDriver.SelectedValue != null && cmbDriver.SelectedValue != DBNull.Value)
                    driverID = Convert.ToInt32(cmbDriver.SelectedValue);

                ClientDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtPhone.Text, txtPhone2.Text,
                    txtAddress.Text, nudOpening.Value, false, driverID, nudCreditLimit.Value, txtNotes.Text, cmbPriceTier.Text, (int)nudOpeningCrates.Value, GetSelectedPaymentType());
                LoadClients();
                ClearDetail();
            }
        }

        private void BtnStatement_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) { MessageBox.Show("اختر عميلاً أولاً"); return; }
            new FrmClientStatement(_selectedID, txtName.Text).ShowDialog();
        }

        private void BtnPayment_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) { MessageBox.Show("اختر عميلاً أولاً من القائمة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            
            decimal currentBal = 0m;
            try { currentBal = ClientDAL.GetClientBalance(_selectedID); } catch { }

            var dlg = new Form
            {
                Width = 420,
                Height = 370,
                Text = "💵 تحصيل نقدية وسند قبض من العميل",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain
            };

            int y = 15;

            // بطاقة معاينة الأرصدة
            var pnlBalPreview = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(375, 75),
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(10, 8, 10, 8)
            };

            var lblCurTitle = new Label
            {
                Text = "الرصيد الحالي:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(245, 10),
                AutoSize = true
            };
            var lblCurBalVal = new Label
            {
                Text = currentBal.ToString("N2") + " ج",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = currentBal < 0 ? Color.FromArgb(248, 113, 113) : (currentBal > 0 ? Color.FromArgb(74, 222, 128) : Color.White),
                Location = new Point(10, 8),
                Width = 230,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblNewTitle = new Label
            {
                Text = "الرصيد بعد التحصيل:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Location = new Point(230, 42),
                AutoSize = true
            };
            var lblNewBalVal = new Label
            {
                Text = currentBal.ToString("N2") + " ج",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Location = new Point(10, 38),
                Width = 215,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlBalPreview.Controls.AddRange(new Control[] { lblCurTitle, lblCurBalVal, lblNewTitle, lblNewBalVal });
            dlg.Controls.Add(pnlBalPreview);
            y += 85;

            // اسم العميل
            var lblClient = new Label
            {
                Text = $"👤 العميل: {txtName.Text}",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Location = new Point(15, y),
                AutoSize = true
            };
            dlg.Controls.Add(lblClient);
            y += 30;

            // المبلغ (يدوي بدون أسهم)
            var lblAmtTitle = new Label
            {
                Text = "المبلغ المحصل (ج):",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Location = new Point(15, y + 6),
                AutoSize = true
            };
            var txtAmount = new TextBox
            {
                Location = new Point(150, y),
                Width = 240,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Color.FromArgb(250, 204, 21),
                TextAlign = HorizontalAlignment.Center
            };
            txtAmount.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                    e.Handled = true;
                if (e.KeyChar == '.' && (s as TextBox).Text.IndexOf('.') > -1)
                    e.Handled = true;
            };
            txtAmount.TextChanged += (s, e) =>
            {
                decimal amt = 0m;
                decimal.TryParse(txtAmount.Text.Trim(), out amt);
                decimal newBal = currentBal - amt;
                lblNewBalVal.Text = newBal.ToString("N2") + " ج";
                lblNewBalVal.ForeColor = newBal < 0 ? Color.FromArgb(248, 113, 113) : (newBal > 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(250, 204, 21));
            };
            txtAmount.Enter += (s, e) => txtAmount.SelectAll();
            dlg.Controls.AddRange(new Control[] { lblAmtTitle, txtAmount });
            y += 45;

            // ملاحظات
            var lblNoteTitle = new Label
            {
                Text = "ملاحظات / البيان:",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextMain,
                Location = new Point(15, y + 4),
                AutoSize = true
            };
            var txtNotes = new TextBox
            {
                Location = new Point(150, y),
                Width = 240,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f),
                RightToLeft = RightToLeft.Yes,
                Text = "سداد نقدية من العميل"
            };
            dlg.Controls.AddRange(new Control[] { lblNoteTitle, txtNotes });
            y += 50;

            // أزرار
            var btnSave = Theme.MakeButton("✅ حفظ وإصدار سند", 215, y, 175, 38, Theme.Success);
            var btnCancel = Theme.MakeButton("❌ إلغاء", 115, y, 90, 38, Theme.Danger);
            btnSave.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

            btnSave.Click += (s2, e2) =>
            {
                if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amt) || amt <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ محصل صحيح أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAmount.Focus();
                    return;
                }
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            btnCancel.Click += (s2, e2) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
            dlg.Controls.AddRange(new Control[] { btnSave, btnCancel });

            dlg.Shown += (s, e) => txtAmount.Focus();

            if (dlg.ShowDialog(this) == DialogResult.OK && decimal.TryParse(txtAmount.Text.Trim(), out decimal paidAmt) && paidAmt > 0)
            {
                ClientDAL.AddPayment(_selectedID, paidAmt, txtNotes.Text.Trim());
                new FrmPrintClientPayment(_selectedID, paidAmt, txtNotes.Text.Trim(), null, txtName.Text);
                LoadClients();
            }
        }


        private void BtnAdjustment_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) { MessageBox.Show("اختر عميلاً أولاً"); return; }
            var frm = new FrmAdjustment(_selectedID, txtName.Text, true);
            if (frm.ShowDialog() == DialogResult.OK)
                LoadClients();
        }

        private void BtnSalesReport_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) { MessageBox.Show("اختر عميلاً من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            new FrmReports("Clients", _selectedID).ShowDialog();
        }

        private void SetupClientsContextMenu()
        {
            var ctx = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };

            var miStatement = new ToolStripMenuItem("📑 كشف حساب تفصيلي", null, (s, e) =>
            {
                if (dgClients.SelectedRows.Count > 0 && dgClients.Columns.Contains("ClientID"))
                {
                    int cid = Convert.ToInt32(dgClients.SelectedRows[0].Cells["ClientID"].Value);
                    string cname = dgClients.SelectedRows[0].Cells["ClientName"].Value?.ToString() ?? "";
                    new FrmClientStatement(cid, cname, 0).ShowDialog(this);
                }
            });

            var miItemized = new ToolStripMenuItem("📊 كشف حساب بالأصناف والكميات", null, (s, e) =>
            {
                if (dgClients.SelectedRows.Count > 0 && dgClients.Columns.Contains("ClientID"))
                {
                    int cid = Convert.ToInt32(dgClients.SelectedRows[0].Cells["ClientID"].Value);
                    string cname = dgClients.SelectedRows[0].Cells["ClientName"].Value?.ToString() ?? "";
                    new FrmClientStatement(cid, cname, 1).ShowDialog(this);
                }
            });

            var miPayment = new ToolStripMenuItem("💵 سند قبض سريع", null, (s, e) =>
            {
                if (dgClients.SelectedRows.Count > 0 && dgClients.Columns.Contains("ClientID"))
                {
                    BtnPayment_Click(s, e);
                }
            });

            var miWhatsApp = new ToolStripMenuItem("📱 مراسلة واتساب / تذكير بالمديونية", null, (s, e) =>
            {
                if (dgClients.SelectedRows.Count > 0 && dgClients.Columns.Contains("ClientID"))
                {
                    string phone = dgClients.SelectedRows[0].Cells["Phone"].Value?.ToString() ?? "";
                    string name = dgClients.SelectedRows[0].Cells["ClientName"].Value?.ToString() ?? "";
                    string bal = dgClients.SelectedRows[0].Cells["Balance"].Value?.ToString() ?? "0";
                    if (!string.IsNullOrEmpty(phone))
                    {
                        string msg = Uri.EscapeDataString($"مرحباً {name}، نود إحاطتكم بأن رصيد حسابكم الحالي طرفنا هو: {bal} ج.");
                        string cleanPhone = phone.Replace(" ", "").Replace("-", "");
                        if (cleanPhone.StartsWith("01")) cleanPhone = "2" + cleanPhone;
                        try { System.Diagnostics.Process.Start($"https://wa.me/{cleanPhone}?text={msg}"); } catch { }
                    }
                    else
                    {
                        MessageBox.Show("لا يوجد رقم هاتف مسجل لهذا العميل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            });

            var miCopyPhone = new ToolStripMenuItem("📋 نسخ رقم الهاتف", null, (s, e) =>
            {
                if (dgClients.SelectedRows.Count > 0 && dgClients.Columns.Contains("Phone"))
                {
                    string phone = dgClients.SelectedRows[0].Cells["Phone"].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(phone))
                    {
                        Clipboard.SetText(phone);
                    }
                }
            });

            ctx.Items.AddRange(new ToolStripItem[] {
                miStatement,
                miItemized,
                miPayment,
                miWhatsApp,
                new ToolStripSeparator(),
                miCopyPhone
            });

            dgClients.ContextMenuStrip = ctx;
            dgClients.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = dgClients.HitTest(e.X, e.Y);
                    if (hit.RowIndex >= 0)
                    {
                        dgClients.ClearSelection();
                        dgClients.Rows[hit.RowIndex].Selected = true;
                        dgClients.CurrentCell = dgClients.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
                    }
                }
            };
        }
    }
}

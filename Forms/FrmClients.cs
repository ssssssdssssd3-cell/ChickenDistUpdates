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
        private ComboBox cmbDriver, cmbPriceTier;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete, btnStatement, btnSearch, btnPayment, btnAdjustment, btnSalesReport;
        private Label lblBalance;
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
            this.Text = "إدارة العملاء";
            this.Size = new Size(1100, 680);
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
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f)); // Column 0 (Right): Details (35%)
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f)); // Column 1 (Left): Grid (65%)
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Left: Grid panel
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Theme.BgCard, Padding = new Padding(6) };
            txtSearch = new TextBox { Dock = DockStyle.Right, Width = 250, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "بحث بالاسم أو الهاتف...", Font = Theme.FontMain, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "بحث بالاسم أو الهاتف...") txtSearch.Text = ""; };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "بحث بالاسم أو الهاتف..."; };
            txtSearch.TextChanged += (s, e) => {
                string searchVal = txtSearch.Text;
                if (searchVal == "بحث بالاسم أو الهاتف...") searchVal = "";
                LoadClients(searchVal);
            };

            btnSearch = Theme.MakeButton("🔍", Theme.Primary);
            btnSearch.Dock = DockStyle.Right;
            btnSearch.Width = 45;
            btnSearch.Click += (s, e) => {
                string searchVal = txtSearch.Text == "بحث بالاسم أو الهاتف..." ? "" : txtSearch.Text;
                LoadClients(searchVal);
            };
            txtSearch.KeyDown += (s, e) => { 
                if (e.KeyCode == Keys.Enter) {
                    string searchVal = txtSearch.Text == "بحث بالاسم أو الهاتف..." ? "" : txtSearch.Text;
                    LoadClients(searchVal);
                }
            };

            pnlSearch.Controls.Add(btnSearch);
            pnlSearch.Controls.Add(txtSearch);

            dgClients = new DataGridView
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
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientID", Visible = false });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientCode", HeaderText = "الكود", FillWeight = 30 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "اسم العميل" });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "الهاتف", FillWeight = 60 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد", FillWeight = 50 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CratesBalance", HeaderText = "رصيد الفوارغ", FillWeight = 40 });
            if (!AppConfig.EnableCratesTracking)
            {
                dgClients.Columns["CratesBalance"].Visible = false;
            }
            dgClients.SelectionChanged += DgClients_SelectionChanged;
            
            pnlGrid.Controls.Add(dgClients);
            pnlGrid.Controls.Add(pnlSearch);

            // Right: Detail panel
            var pnlDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(12),
                AutoScroll = true
            };

            int y = 10;
            pnlDetails.Controls.Add(MakeField("كود العميل:", ref y, out txtCode));
            txtCode.ReadOnly = true;
            txtCode.TabStop = false;
            pnlDetails.Controls.Add(MakeField("اسم العميل:", ref y, out txtName));
            pnlDetails.Controls.Add(MakeField("الهاتف:", ref y, out txtPhone));
            pnlDetails.Controls.Add(MakeField("هاتف إضافي:", ref y, out txtPhone2));
            pnlDetails.Controls.Add(MakeField("العنوان:", ref y, out txtAddress));

            var lblLimit = new Label { Text = "حد المديونية:", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(lblLimit);
            nudCreditLimit = new NumericUpDown { Location = new Point(10, y - 2), Width = 185, Minimum = 0, Maximum = 9999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(nudCreditLimit); y += 36;

            var lblOp = new Label { Text = "رصيد افتتاحي:", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(lblOp);
            nudOpening = new NumericUpDown { Location = new Point(10, y - 2), Width = 185, Minimum = -999999, Maximum = 9999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(nudOpening); y += 36;

            var lblOpCrates = new Label { Text = "رصيد الفوارغ الأولي:", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(lblOpCrates);
            nudOpeningCrates = new NumericUpDown { Location = new Point(10, y - 2), Width = 185, Minimum = -999999, Maximum = 9999999, DecimalPlaces = 0, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(nudOpeningCrates);
            if (!AppConfig.EnableCratesTracking)
            {
                lblOpCrates.Visible = false;
                nudOpeningCrates.Visible = false;
            }
            else
            {
                y += 36;
            }

            var lblDriver = new Label { Text = "المندوب الافتراضي:", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(lblDriver);
            cmbDriver = new ComboBox { Location = new Point(10, y - 2), Width = 185, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            pnlDetails.Controls.Add(cmbDriver); y += 36;

            var lblPriceTier = new Label { Text = "فئة السعر الافتراضية:", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(lblPriceTier);
            cmbPriceTier = new ComboBox { Location = new Point(10, y - 2), Width = 185, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            cmbPriceTier.Items.AddRange(new object[] { "قطاعي", "نصف جملة", "جملة" });
            cmbPriceTier.SelectedIndex = 0;
            pnlDetails.Controls.Add(cmbPriceTier); y += 36;

            pnlDetails.Controls.Add(MakeField("ملاحظات:", ref y, out txtNotes));

            chkActive = new CheckBox { Text = "نشط", Location = new Point(110, y), Width = 185, ForeColor = Theme.TextMain, Checked = true, RightToLeft = RightToLeft.Yes }; y += 36;
            pnlDetails.Controls.Add(chkActive);

            lblBalance = new Label { Text = "الرصيد الحالي: ---", Location = new Point(10, y), Width = 285, AutoSize = false, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 11, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight }; y += 40;
            pnlDetails.Controls.Add(lblBalance);

            btnSave = Theme.MakeButton("💾 حفظ", 210, y, 90, 32, Theme.Accent);
            btnNew = Theme.MakeButton("🆕 جديد", 110, y, 90, 32, Theme.Success);
            btnDelete = Theme.MakeButton("🗑 إيقاف", 10, y, 90, 32, Theme.Danger); y += 44;
            btnPayment = Theme.MakeButton("💵 تحصيل", 205, y, 95, 32, Theme.Success);
            btnAdjustment = Theme.MakeButton("⚖️ تسوية", 110, y, 90, 32, Theme.Secondary);
            btnStatement = Theme.MakeButton("📄 كشف", 10, y, 95, 32, Theme.Primary); y += 44;
            btnSalesReport = Theme.MakeButton("📊 تقرير المبيعات", 10, y, 290, 32, Theme.Accent); y += 44;
 
            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnStatement.Click += BtnStatement_Click;
            btnPayment.Click += BtnPayment_Click;
            btnAdjustment.Click += BtnAdjustment_Click;
            btnSalesReport.Click += BtnSalesReport_Click;
 
            pnlDetails.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete, btnStatement, btnPayment, btnAdjustment, btnSalesReport });
            
            tbl.Controls.Add(pnlDetails, 0, 0); // Column 0 (Right): Details
            tbl.Controls.Add(pnlGrid, 1, 0);    // Column 1 (Left): Grid
            this.Controls.Add(tbl);
            Theme.ApplyFormRTL(this);
        }

        private Panel MakeField(string label, ref int y, out TextBox txt)
        {
            var p = new Panel { Location = new Point(5, y), Width = 310, Height = 32 };
            p.Controls.Add(new Label { Text = label, Location = new Point(200, 5), AutoSize = true, ForeColor = Theme.TextMain });
            txt = new TextBox { Location = new Point(10, 1), Width = 185, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            p.Controls.Add(txt);
            y += 38;
            return p;
        }

        private void LoadClients(string search = "")
        {
            dgClients.Rows.Clear();
            if (search == "بحث بالاسم أو الهاتف...") search = "";

            DataTable dt = string.IsNullOrWhiteSpace(search)
                ? ClientDAL.GetAll()
                : ClientDAL.Search(search);

            // تعطيل AutoSize أثناء التحميل لتسريع عرض العملاء
            dgClients.SuspendLayout();
            var oldMode = dgClients.AutoSizeColumnsMode;
            dgClients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            foreach (DataRow r in dt.Rows)
            {
                decimal bal = Convert.ToDecimal(r["Balance"]);
                int cratesBal = Convert.ToInt32(r["CratesBalance"]);
                var row = dgClients.Rows.Add(r["ClientID"], r["ClientCode"], r["ClientName"], r["Phone"], bal.ToString("N2") + " ج", cratesBal.ToString() + " فارغ");
                if (bal > 0) dgClients.Rows[row].DefaultCellStyle.ForeColor = System.Drawing.Color.OrangeRed;
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
            if (AppConfig.EnableCratesTracking)
                lblBalance.Text = "الرصيد المالي: " + row.Cells["Balance"].Value + " | الفوارغ: " + row.Cells["CratesBalance"].Value;
            else
                lblBalance.Text = "الرصيد المالي: " + row.Cells["Balance"].Value;
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

            int? driverID = null;
            if (cmbDriver.SelectedValue != null && cmbDriver.SelectedValue != DBNull.Value)
                driverID = Convert.ToInt32(cmbDriver.SelectedValue);

            int id = ClientDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtPhone.Text, txtPhone2.Text,
                txtAddress.Text, nudOpening.Value, chkActive.Checked, driverID, nudCreditLimit.Value, txtNotes.Text, cmbPriceTier.Text, (int)nudOpeningCrates.Value);
            if (id > 0) { ClientCache.Refresh(); MessageBox.Show("✅ تم الحفظ"); _selectedID = id; LoadClients(); }
            else MessageBox.Show("❌ فشل الحفظ");
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
                    txtAddress.Text, nudOpening.Value, false, driverID, nudCreditLimit.Value, txtNotes.Text, cmbPriceTier.Text, (int)nudOpeningCrates.Value);
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
            if (_selectedID == 0) { MessageBox.Show("اختر عميلاً أولاً"); return; }
            var frm = new FrmPayment(_selectedID, txtName.Text);
            if (frm.ShowDialog() == DialogResult.OK)
                LoadClients();
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
    }
}

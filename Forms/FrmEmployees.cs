using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Collections.Generic;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة إدارة الموظفين والصلاحيات</summary>
    public class FrmEmployees : Form
    {
        private DataGridView dgEmployees;
        private TextBox txtName, txtUsername, txtPassword, txtPhone;
        private TextBox txtJobTitle, txtSalary, txtDailyHours, txtCommissionRate, txtTarget, txtNationalID;
        private ComboBox cboRole;
        private CheckBox chkDriver, chkActive;
        private Button btnNew, btnSave, btnDelete, btnPerms;
        private int _selectedID = 0;

        private ComboBox cboDefaultSafe;
        private CheckedListBox clbAllowedSafes;
        private CheckBox chkCanSellCash, chkCanSellCredit, chkCanSellVisa, chkCanSellDriverLoad, chkCanSellInstallment, chkCanEditShippingCharge, chkCanSelectDriver;

        public FrmEmployees()
        {
            if (!Session.CanAccess("Employees"))
            {
                this.Load += (s, e) =>
                {
                    MessageBox.Show("غير مصرح لك بالوصول");
                    this.Close();
                };
                return;
            }
            InitUI();
            LoadEmployees();
        }

        private void InitUI()
        {
            this.Text = "إدارة الموظفين والصلاحيات";
            this.Size = new Size(1000, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // header handled by main form's top bar


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
            dgEmployees = new DataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgEmployees.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmpID", Visible = false });
            dgEmployees.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmpName", HeaderText = "الاسم" });
            dgEmployees.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserName", HeaderText = "اسم المستخدم", FillWeight = 60 });
            dgEmployees.Columns.Add(new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "الدور", FillWeight = 50 });
            dgEmployees.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "الهاتف", FillWeight = 55 });
            dgEmployees.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "نشط", FillWeight = 25 });
            dgEmployees.SelectionChanged += DgEmployees_SelectionChanged;

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            pnlGrid.Controls.Add(dgEmployees);

            // Right: Detail panel
            var pnlDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15),
                AutoScroll = true
            };

            int y = 20;
            AddField(pnlDetails, "الاسم:", ref y, out txtName);
            AddField(pnlDetails, "اسم المستخدم:", ref y, out txtUsername);
            AddField(pnlDetails, "كلمة المرور:", ref y, out txtPassword);
            AddField(pnlDetails, "الهاتف:", ref y, out txtPhone);
            AddField(pnlDetails, "المسمى الوظيفي:", ref y, out txtJobTitle);
            AddField(pnlDetails, "الراتب الأساسي (ج.م):", ref y, out txtSalary);
            AddField(pnlDetails, "ساعات العمل اليومية:", ref y, out txtDailyHours);
            AddField(pnlDetails, "نسبة عمولة المبيعات (%):", ref y, out txtCommissionRate);
            AddField(pnlDetails, "تارجت المبيعات (ج.م):", ref y, out txtTarget);
            AddField(pnlDetails, "الرقم القومي:", ref y, out txtNationalID);

            pnlDetails.Controls.Add(new Label { Text = "الدور:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            cboRole = new ComboBox { Location = new Point(15, y - 2), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput };
            cboRole.Items.AddRange(new object[] { "Admin", "Supervisor", "Driver", "Accountant", "User" });
            cboRole.SelectedIndex = 4;
            pnlDetails.Controls.Add(cboRole); y += 38;

            chkDriver = new CheckBox { Text = "مندوب توزيع", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain }; y += 32;
            chkActive = new CheckBox { Text = "موظف نشط", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true }; y += 40;
            pnlDetails.Controls.AddRange(new Control[] { chkDriver, chkActive });

            // Default Safe
            pnlDetails.Controls.Add(new Label { Text = "الخزينة الافتراضية:", Location = new Point(220, y), AutoSize = true, ForeColor = Theme.TextMain });
            cboDefaultSafe = new ComboBox { Location = new Point(15, y - 2), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(cboDefaultSafe); y += 38;

            // Allowed Safes CheckedListBox
            pnlDetails.Controls.Add(new Label { Text = "الخزائن المسموحة:", Location = new Point(220, y), AutoSize = true, ForeColor = Theme.TextMain });
            clbAllowedSafes = new CheckedListBox { Location = new Point(15, y), Width = 180, Height = 95, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlDetails.Controls.Add(clbAllowedSafes); y += 105;

            // Selling permissions checkboxes
            pnlDetails.Controls.Add(new Label { Text = "طرق البيع المسموحة:", Location = new Point(200, y), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextMain });
            y += 24;
            chkCanSellCash = new CheckBox { Text = "بيع نقدي (كاش)", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            chkCanSellCredit = new CheckBox { Text = "بيع آجل", Location = new Point(50, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            y += 28;
            chkCanSellVisa = new CheckBox { Text = "بيع فيزا / شبكة", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            chkCanSellInstallment = new CheckBox { Text = "تقسيط شرعي", Location = new Point(50, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            y += 28;
            chkCanSellDriverLoad = new CheckBox { Text = "تحميل مندوب", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            chkCanSelectDriver = new CheckBox { Text = "اختيار/ظهور المندوب", Location = new Point(20, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            y += 28;
            chkCanEditShippingCharge = new CheckBox { Text = "إضافة/تعديل خدمة الشحن", Location = new Point(160, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            pnlDetails.Controls.AddRange(new Control[] { chkCanSellCash, chkCanSellCredit, chkCanSellVisa, chkCanSellInstallment, chkCanSellDriverLoad, chkCanSelectDriver, chkCanEditShippingCharge });
            y += 35;

            btnNew = Theme.MakeButton("🆕 جديد", 240, y, 90, 32, Color.FromArgb(60, 100, 60));
            btnSave = Theme.MakeButton("💾 حفظ", 140, y, 90, 32, Theme.Accent);
            btnDelete = Theme.MakeButton("🗑 إيقاف", 40, y, 90, 32, Color.FromArgb(140, 40, 40)); y += 44;
            btnPerms = Theme.MakeButton("🔐 الصلاحيات", 180, y, 150, 32, Theme.Primary);

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnPerms.Click += BtnPerms_Click;

            pnlDetails.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete, btnPerms });

            tbl.Controls.Add(pnlDetails, 0, 0); // Column 0 (Right): Details
            tbl.Controls.Add(pnlGrid, 1, 0);    // Column 1 (Left): Grid
            this.Controls.Add(tbl);

            LoadSafesList();
            Theme.ApplyFormRTL(this);
        }

        private void AddField(Control parent, string label, ref int y, out TextBox txt)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            txt = new TextBox { Location = new Point(15, y - 2), Width = 180, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            parent.Controls.Add(txt);
            y += 38;
        }

        private void LoadEmployees()
        {
            dgEmployees.Rows.Clear();
            var dt = EmployeeDAL.GetAll();
            foreach (DataRow r in dt.Rows)
            {
                bool active = Convert.ToBoolean(r["IsActive"]);
                var ri = dgEmployees.Rows.Add(r["EmpID"], r["EmpName"], r["UserName"], r["Role"], r["Phone"], active ? "✓" : "✗");
                if (!active) dgEmployees.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
            }
        }

        private void DgEmployees_SelectionChanged(object sender, EventArgs e)
        {
            if (dgEmployees.SelectedRows.Count == 0) return;
            _selectedID = Convert.ToInt32(dgEmployees.SelectedRows[0].Cells["EmpID"].Value);
            var dr = EmployeeDAL.GetByID(_selectedID);
            if (dr == null) return;
            txtName.Text = dr["EmpName"].ToString();
            txtUsername.Text = dr["UserName"].ToString();
            // تفريغ حقل كلمة المرور للأمان؛ يمكن كتابة كلمة مرور جديدة أو تركها فارغة للحفاظ على الحالية
            txtPassword.Clear();
            txtPhone.Text = dr["Phone"].ToString();
            txtJobTitle.Text = dr.Table.Columns.Contains("JobTitle") && dr["JobTitle"] != DBNull.Value ? dr["JobTitle"].ToString() : "";
            txtSalary.Text = dr.Table.Columns.Contains("Salary") && dr["Salary"] != DBNull.Value ? Convert.ToDecimal(dr["Salary"]).ToString("N2") : "0.00";
            txtDailyHours.Text = dr.Table.Columns.Contains("DailyWorkHours") && dr["DailyWorkHours"] != DBNull.Value ? Convert.ToDecimal(dr["DailyWorkHours"]).ToString("N1") : "8.0";
            txtCommissionRate.Text = dr.Table.Columns.Contains("SalesCommissionRate") && dr["SalesCommissionRate"] != DBNull.Value ? Convert.ToDecimal(dr["SalesCommissionRate"]).ToString("N1") : "0.0";
            txtTarget.Text = dr.Table.Columns.Contains("TargetAmount") && dr["TargetAmount"] != DBNull.Value ? Convert.ToDecimal(dr["TargetAmount"]).ToString("N2") : "0.00";
            txtNationalID.Text = dr.Table.Columns.Contains("NationalID") && dr["NationalID"] != DBNull.Value ? dr["NationalID"].ToString() : "";
            cboRole.Text = dr["Role"].ToString();
            chkDriver.Checked = Convert.ToBoolean(dr["IsDriver"]);
            chkActive.Checked = Convert.ToBoolean(dr["IsActive"]);

            // Default Safe
            int defaultSafeId = dr["DefaultSafeID"] != DBNull.Value ? Convert.ToInt32(dr["DefaultSafeID"]) : 0;
            cboDefaultSafe.SelectedIndex = 0; // Default none
            for (int i = 0; i < cboDefaultSafe.Items.Count; i++)
            {
                if (cboDefaultSafe.Items[i] is ComboItem item && item.ID == defaultSafeId)
                {
                    cboDefaultSafe.SelectedIndex = i;
                    break;
                }
            }

            // Allowed Safes
            string allowedSafesStr = dr["AllowedSafeIDs"] != DBNull.Value ? dr["AllowedSafeIDs"].ToString() : "";
            var allowedIds = new System.Collections.Generic.HashSet<string>(allowedSafesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            for (int i = 0; i < clbAllowedSafes.Items.Count; i++)
            {
                if (clbAllowedSafes.Items[i] is ComboItem item)
                {
                    bool shouldCheck = allowedIds.Contains(item.ID.ToString());
                    clbAllowedSafes.SetItemChecked(i, shouldCheck);
                }
            }

            // Selling Permissions Checkboxes
            chkCanSellCash.Checked = dr["CanSellCash"] == DBNull.Value || Convert.ToBoolean(dr["CanSellCash"]);
            chkCanSellCredit.Checked = dr["CanSellCredit"] == DBNull.Value || Convert.ToBoolean(dr["CanSellCredit"]);
            chkCanSellVisa.Checked = !dr.Table.Columns.Contains("CanSellVisa") || dr["CanSellVisa"] == DBNull.Value || Convert.ToBoolean(dr["CanSellVisa"]);
            chkCanSellDriverLoad.Checked = dr["CanSellDriverLoad"] == DBNull.Value || Convert.ToBoolean(dr["CanSellDriverLoad"]);
            chkCanSellInstallment.Checked = dr["CanSellInstallment"] == DBNull.Value || Convert.ToBoolean(dr["CanSellInstallment"]);
            chkCanEditShippingCharge.Checked = dr.Table.Columns.Contains("CanEditShippingCharge") && (dr["CanEditShippingCharge"] == DBNull.Value || Convert.ToBoolean(dr["CanEditShippingCharge"]));
            chkCanSelectDriver.Checked = !dr.Table.Columns.Contains("CanSelectDriver") || dr["CanSelectDriver"] == DBNull.Value || Convert.ToBoolean(dr["CanSelectDriver"]);
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtName.Clear(); txtUsername.Clear(); txtPassword.Clear(); txtPhone.Clear();
            txtJobTitle.Clear(); txtSalary.Text = "0.00"; txtDailyHours.Text = "8.0";
            txtCommissionRate.Text = "0.0"; txtTarget.Text = "0.00"; txtNationalID.Clear();
            cboRole.SelectedIndex = 4;
            chkDriver.Checked = false; chkActive.Checked = true;

            if (cboDefaultSafe.Items.Count > 0) cboDefaultSafe.SelectedIndex = 0;
            for (int i = 0; i < clbAllowedSafes.Items.Count; i++)
            {
                clbAllowedSafes.SetItemChecked(i, false);
            }
            chkCanSellCash.Checked = true;
            chkCanSellCredit.Checked = true;
            chkCanSellVisa.Checked = true;
            chkCanSellDriverLoad.Checked = true;
            chkCanSellInstallment.Checked = true;
            chkCanEditShippingCharge.Checked = true;
            chkCanSelectDriver.Checked = true;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم الموظف"); return; }
            if (string.IsNullOrWhiteSpace(txtUsername.Text)) { MessageBox.Show("أدخل اسم المستخدم"); return; }
            if (_selectedID == 0 && string.IsNullOrWhiteSpace(txtPassword.Text)) { MessageBox.Show("أدخل كلمة المرور"); return; }

            int? defaultSafeID = null;
            if (cboDefaultSafe.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
            {
                defaultSafeID = safeItem.ID;
            }

            var allowedList = new System.Collections.Generic.List<string>();
            for (int i = 0; i < clbAllowedSafes.CheckedItems.Count; i++)
            {
                if (clbAllowedSafes.CheckedItems[i] is ComboItem item)
                {
                    allowedList.Add(item.ID.ToString());
                }
            }
            string allowedSafeIDs = string.Join(",", allowedList);

            decimal.TryParse(txtSalary.Text.Trim(), out decimal sal);
            decimal.TryParse(txtDailyHours.Text.Trim(), out decimal dwh);
            if (dwh <= 0) dwh = 8;
            decimal.TryParse(txtCommissionRate.Text.Trim(), out decimal crate);
            decimal.TryParse(txtTarget.Text.Trim(), out decimal target);
            decimal hourlyRate = (sal > 0 && dwh > 0) ? (sal / 30m / dwh) : 0m;
            string jobTitle = txtJobTitle.Text.Trim();
            string nationalID = txtNationalID.Text.Trim();

            try
            {
                int id = EmployeeDAL.Save(_selectedID, txtName.Text, txtUsername.Text,
                    txtPassword.Text, cboRole.Text, txtPhone.Text, chkDriver.Checked, chkActive.Checked,
                    defaultSafeID, allowedSafeIDs, chkCanSellCash.Checked, chkCanSellCredit.Checked,
                    chkCanSellDriverLoad.Checked, chkCanSellInstallment.Checked, chkCanEditShippingCharge.Checked,
                    chkCanSelectDriver.Checked, chkCanSellVisa.Checked,
                    sal, dwh, hourlyRate, crate, target, jobTitle, null, nationalID);
                if (id > 0) { MessageBox.Show("✅ تم الحفظ"); _selectedID = id; LoadEmployees(); }
                else MessageBox.Show("❌ فشل الحفظ");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ فشل الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;
            if (MessageBox.Show("إيقاف الموظف؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { EmployeeDAL.Delete(_selectedID); LoadEmployees(); ClearDetail(); }
        }

        private void LoadSafesList()
        {
            try
            {
                DataTable safes = AccountDAL.GetActiveSafeAccounts();
                
                // For cboDefaultSafe
                cboDefaultSafe.Items.Clear();
                cboDefaultSafe.Items.Add(new ComboItem(0, "--- بدون خزينة افتراضية ---"));
                
                clbAllowedSafes.Items.Clear();
                
foreach (DataRow r in safes.Rows)
                {
                    int id = Convert.ToInt32(r["AccountID"]);
                    string name = r["AccountName"].ToString();
                    var item = new ComboItem(id, name);
                    cboDefaultSafe.Items.Add(item);
                    clbAllowedSafes.Items.Add(item);
                }
                cboDefaultSafe.DisplayMember = "Text";
                cboDefaultSafe.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadSafesList failed", ex);
            }
        }

        private void BtnPerms_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) { MessageBox.Show("اختر موظفاً أولاً"); return; }
            new FrmPermissions(_selectedID, txtName.Text).ShowDialog();
        }
    }

    /// <summary>شاشة إدارة وضبط الصلاحيات الشاملة — هيكل تفصيلي شاشة بشاشة مع شروحات واضحة</summary>
    public class FrmPermissions : Form
    {
        private readonly int _empID;
        private readonly string _empName;

        public class ScreenDef
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public string Icon { get; set; }
            public string Description { get; set; }

            // Flags of which sub-permissions are applicable to this screen
            public bool HasAdd { get; set; }
            public bool HasEdit { get; set; }
            public bool HasDelete { get; set; }
            public bool HasEditPrice { get; set; }
            public bool HasEditSalesInvoice { get; set; } // تعديل/إرجاع فواتير الغير
            public bool HasDeleteSalesInvoice { get; set; }
            public bool HasCopySalesInvoice { get; set; }
            public bool HasViewCost { get; set; }
            public bool HasViewBalance { get; set; }
            public bool HasViewDetails { get; set; }
            public bool HasChangeSafe { get; set; }
            public bool HasViewSalesTotals { get; set; }
            public bool HasViewQuickItems { get; set; }
            public bool HasOrderColumns { get; set; }

            // Custom labels & hints if needed
            public string AddLabel { get; set; } = "➕ حفظ وإصدار العمليات الجديدة";
            public string AddHint { get; set; } = "يتيح للموظف إتمام وحفظ العمليات والفواتير الجديدة بنجاح في قاعدة البيانات.";

            public string EditLabel { get; set; } = "✏️ تعديل العمليات والبيانات السابقة";
            public string EditHint { get; set; } = "السماح للموظف بفتح وتعديل الفواتير أو الحركات المسجلة مسبقاً وتغيير بنودها.";

            public string DeleteLabel { get; set; } = "🗑️ حذف وإلغاء العمليات والسجلات";
            public string DeleteHint { get; set; } = "السماح للموظف بحذف الفواتير أو السجلات نهائياً من قاعدة البيانات.";

            public string EditPriceLabel { get; set; } = "🏷️ تعديل الأسعار والخصومات يدوياً";
            public string EditPriceHint { get; set; } = "السماح بتغيير سعر بيع/شراء الوحدة أو نسبة الخصم يدوياً أثناء تحرير الفاتورة.";

            public string OtherSalesLabel { get; set; } = "📝 تعديل / إرجاع فواتير الموظفين الآخرين";
            public string OtherSalesHint { get; set; } = "عند التفعيل: يستطيع الموظف تعديل أو إرجاع فواتير أي موظف أو مندوب آخر.\nعند الإلغاء: يقتصر على فواتيره ومبيعاته الشخصية فقط.";
        }

        public class ScreenPermState
        {
            public bool CanAccess { get; set; }
            public bool CanAdd { get; set; } = true;
            public bool CanEdit { get; set; } = true;
            public bool CanDelete { get; set; }
            public bool CanEditPrice { get; set; }
            public bool CanEditSalesInvoice { get; set; }
            public bool CanDeleteSalesInvoice { get; set; }
            public bool CanCopySalesInvoice { get; set; } = true;
            public bool CanViewCost { get; set; }
            public bool CanViewBalance { get; set; }
            public bool CanViewDetails { get; set; } = true;
            public bool CanChangeSafe { get; set; }
            public bool CanViewSalesTotals { get; set; } = true;
            public bool CanViewQuickItems { get; set; } = true;
            public bool CanOrderColumns { get; set; }
        }

        private static readonly List<ScreenDef> AllScreens = new List<ScreenDef>
        {
            // ── 🛒 1. المبيعات والعملاء ─────────────────────────────────────
            new ScreenDef {
                Key = "Sales", Name = "فاتورة المبيعات الرئيسية", Category = "🛒 المبيعات والعملاء", Icon = "🛒",
                Description = "شاشة إصدار فواتير البيع للعملاء وإدارة الحسابات النقدية والآجلة.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasEditPrice = true, HasEditSalesInvoice = true,
                HasDeleteSalesInvoice = true, HasCopySalesInvoice = true, HasViewCost = true, HasChangeSafe = true,
                HasViewQuickItems = true, HasOrderColumns = true,
                AddLabel = "➕ إصدار وحفظ فواتير بيع جديدة",
                AddHint = "السماح للموظف بحفظ فواتير البيع وإتمام عملية البيع.",
                EditLabel = "✏️ تعديل فواتير المبيعات السابقة",
                EditHint = "السماح بفتح وتعديل فواتير بيع سابقة.",
                OtherSalesLabel = "📝 تعديل فواتير الموظفين الآخرين",
                OtherSalesHint = "إذا أُلغيت، يقتصر الموظف على تعديل فواتيره الشخصية فقط."
            },
            new ScreenDef {
                Key = "POS", Name = "شاشة الكاشير السريع (POS)", Category = "🛒 المبيعات والعملاء", Icon = "⚡",
                Description = "شاشة البيع السريعة المخصصة لنقاط البيع وطباعة الريسيت الحراري.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasEditPrice = true, HasEditSalesInvoice = true,
                HasDeleteSalesInvoice = true, HasCopySalesInvoice = true, HasViewCost = true, HasChangeSafe = true,
                HasViewQuickItems = true, HasOrderColumns = true,
                AddLabel = "➕ حفظ وإتمام فواتير الكاشير",
                AddHint = "السماح للموظف بإنهاء وحفظ فواتير نقاط البيع السريعة."
            },
            new ScreenDef {
                Key = "Returns", Name = "مرتجع المبيعات واستبدال الأصناف", Category = "🛒 المبيعات والعملاء", Icon = "↩️",
                Description = "شاشة إرجاع بضاعة وفواتير مبيعات للعملاء وإعادة الأصناف للمخزن.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasEditPrice = true, HasEditSalesInvoice = true,
                HasCopySalesInvoice = true, HasChangeSafe = true,
                AddLabel = "➕ حفظ وإتمام عمليات المرتجع",
                AddHint = "السماح للموظف بحفظ وإتمام مرتجعات المبيعات (يجب تفعيلها ليتمكن من حفظ المرتجع).",
                EditLabel = "✏️ تعديل فواتير المرتجع السابقة",
                EditHint = "السماح بتعديل مرتجعات تم حفظها مسبقاً.",
                OtherSalesLabel = "📝 إرجاع فواتير الموظفين والمناديب الآخرين",
                OtherSalesHint = "عند التفعيل: يستطيع إرجاع أي فاتورة لأي موظف.\nعند الإلغاء: يقتصر على إرجاع فواتيره ومبيعاته الشخصية فقط."
            },
            new ScreenDef {
                Key = "PriceQuote", Name = "بيان تسعير وعروض الأسعار", Category = "🛒 المبيعات والعملاء", Icon = "📋",
                Description = "شاشة تحرير عروض وبيانات أسعار للعملاء دون التأثير على رصيد المخزن.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasEditPrice = true, HasCopySalesInvoice = true, HasViewCost = true,
                AddLabel = "➕ إنشاء وحفظ بيان تسعير جديد",
                AddHint = "السماح للموظف بحفظ عروض وبيانات الأسعار للعملاء."
            },
            new ScreenDef {
                Key = "SalesList", Name = "سجل الفواتير والمبيعات السابقة", Category = "🛒 المبيعات والعملاء", Icon = "📋",
                Description = "استعراض والبحث في كافة فواتير المبيعات الصادرة وإعادة طباعتها وتصديرها.",
                HasEdit = true, HasDelete = true, HasEditSalesInvoice = true, HasDeleteSalesInvoice = true,
                HasCopySalesInvoice = true, HasViewCost = true, HasViewSalesTotals = true,
                OtherSalesLabel = "📝 تعديل فواتير الغير من السجل",
                OtherSalesHint = "السماح بفتح وتعديل فواتير مسجلة بأسماء موظفين آخرين من السجل."
            },
            new ScreenDef {
                Key = "Clients", Name = "إدارة بيانات العملاء", Category = "🛒 المبيعات والعملاء", Icon = "👥",
                Description = "إضافة وتعديل بيانات العملاء وتحديد حدود الائتمان والأسعار الخاصة.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasViewBalance = true,
                AddLabel = "➕ إضافة عميل جديد",
                EditLabel = "✏️ تعديل بيانات العملاء",
                DeleteLabel = "🗑️ حذف عميل"
            },
            new ScreenDef {
                Key = "ClientStatement", Name = "كشف حساب العميل التفصيلي", Category = "🛒 المبيعات والعملاء", Icon = "📄",
                Description = "استعراض حركات وفواتير ومسددات ومتبقي حساب أي عميل خلال فترة.",
                HasViewBalance = true
            },
            new ScreenDef {
                Key = "Installments", Name = "عقود التقسيط وجدولة المديونيات", Category = "🛒 المبيعات والعملاء", Icon = "💳",
                Description = "إدارة عقود التقسيط للعملاء وجدولة الأقساط الشهرية ومتابعة التحصيل.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasViewBalance = true
            },
            new ScreenDef {
                Key = "PriceChecker", Name = "كشك فحص الأسعار والبدائل الذكية", Category = "🛒 المبيعات والعملاء", Icon = "🏷️",
                Description = "شاشة استعلام فوري عن سعر الصنف والبدائل المتوفرة بدون فتح فواتير."
            },
            new ScreenDef {
                Key = "ProductSearch", Name = "شاشة بحث الأصناف السريعة", Category = "🛒 المبيعات والعملاء", Icon = "🔍",
                Description = "نافذة البحث السريع عن الأصعار والكميات المتاحة في المخازن.",
                HasEditPrice = true, HasViewCost = true
            },

            // ── 📥 2. المشتريات والموردين ─────────────────────────────────
            new ScreenDef {
                Key = "Purchases", Name = "فاتورة المشتريات (وارد بضاعة)", Category = "📥 المشتريات والموردين", Icon = "📥",
                Description = "شاشة إدخال فواتير الشراء من الموردين وزيادة رصيد المخزن وتكلفة الأصناف.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasEditPrice = true, HasCopySalesInvoice = true,
                HasChangeSafe = true, HasOrderColumns = true,
                AddLabel = "➕ حفظ وإصدار فواتير شراء جديدة",
                AddHint = "السماح للموظف بحفظ فواتير الشراء من الموردين.",
                EditLabel = "✏️ تعديل فواتير الشراء السابقة",
                DeleteLabel = "🗑️ حذف فاتورة شراء"
            },
            new ScreenDef {
                Key = "PurchaseReturn", Name = "مرتجع المشتريات للموردين", Category = "📥 المشتريات والموردين", Icon = "↩️",
                Description = "شاشة إرجاع بضاعة لمورد وخصم قيمتها من حسابه وخصم الرصيد من المخزن.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasEditPrice = true, HasChangeSafe = true,
                AddLabel = "➕ حفظ وإتمام مرتجع مشتريات",
                AddHint = "السماح للموظف بحفظ وإتمام مرتجع الشراء للمورد."
            },
            new ScreenDef {
                Key = "PurchasesList", Name = "سجل فواتير المشتريات", Category = "📥 المشتريات والموردين", Icon = "📋",
                Description = "استعراض سجل فواتير الشراء والبحث فيها ومراجعة بنودها.",
                HasEdit = true, HasDelete = true, HasCopySalesInvoice = true, HasViewSalesTotals = true
            },
            new ScreenDef {
                Key = "Suppliers", Name = "إدارة بيانات الموردين", Category = "📥 المشتريات والموردين", Icon = "🏢",
                Description = "إضافة وتعديل بيانات الموردين وجهات التوريد وحساباتهم.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasViewBalance = true,
                AddLabel = "➕ إضافة مورد جديد",
                EditLabel = "✏️ تعديل بيانات المورد",
                DeleteLabel = "🗑️ حذف مورد"
            },
            new ScreenDef {
                Key = "SupplierStatement", Name = "كشف حساب المورد التفصيلي", Category = "📥 المشتريات والموردين", Icon = "📄",
                Description = "استعراض حركات الشراء والمدفوعات والمتبقي للمورد.",
                HasViewBalance = true
            },
            new ScreenDef {
                Key = "SupplierPayment", Name = "سند صرف نقدي لمورد", Category = "📥 المشتريات والموردين", Icon = "💵",
                Description = "تسجيل دفعات نقدية أو شيكات للموردين وخصمها من الخزنة.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasChangeSafe = true, HasViewBalance = true
            },
            new ScreenDef {
                Key = "SupplierAdjustment", Name = "تسوية أرصدة الموردين", Category = "📥 المشتريات والموردين", Icon = "⚖️",
                Description = "تسوية الفروق الحسابية والخصومات المكتسبة مع الموردين.",
                HasAdd = true, HasEdit = true, HasDelete = true
            },

            // ── 📦 3. المخازن والأصناف ───────────────────────────────────
            new ScreenDef {
                Key = "Products", Name = "إدارة وتعديل الأصناف والأسعار", Category = "📦 المخازن والأصناف", Icon = "🏷️",
                Description = "إضافة وتعديل الأصناف وتحديد أسعار البيع والشراء والباركود وحد الطلب.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasEditPrice = true, HasViewCost = true, HasOrderColumns = true,
                AddLabel = "➕ إضافة صنف جديد",
                AddHint = "السماح للموظف بإنشاء أصناف جديدة في النظام.",
                EditLabel = "✏️ تعديل بيانات وأسعار الأصناف",
                DeleteLabel = "🗑️ حذف صنف من النظام",
                EditPriceLabel = "🏷️ تعديل أسعار البيع والشراء للأصناف"
            },
            new ScreenDef {
                Key = "ProductCard", Name = "كارت الصنف والمواصفات", Category = "📦 المخازن والأصناف", Icon = "💳",
                Description = "استعراض كارت الصنف ومواصفاته الفنية وأسعاره ومخزونه.",
                HasAdd = true, HasEdit = true, HasViewCost = true
            },
            new ScreenDef {
                Key = "Categories", Name = "التصنيفات والمجموعات", Category = "📦 المخازن والأصناف", Icon = "🗂️",
                Description = "إدارة أقسام وتصنيفات ومجموعات الأصناف الرئيسية والفرعية.",
                HasAdd = true, HasEdit = true, HasDelete = true
            },
            new ScreenDef {
                Key = "Units", Name = "إدارة الوحدات ومعاملات التحويل", Category = "📦 المخازن والأصناف", Icon = "📏",
                Description = "تعريف وحدات القياس (قطعة، دستة، كرتونة) ومعاملات التحويل.",
                HasAdd = true, HasEdit = true, HasDelete = true
            },
            new ScreenDef {
                Key = "Warehouses", Name = "إدارة المخازن والمستودعات", Category = "📦 المخازن والأصناف", Icon = "🏭",
                Description = "إضافة وتعريف المخازن والفروع والمستودعات.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasViewCost = true
            },
            new ScreenDef {
                Key = "Inventory", Name = "جرد وتعديل رصيد المخزن", Category = "📦 المخازن والأصناف", Icon = "📦",
                Description = "تسجيل أرصدة الجرد الفعلي للمخازن وتسوية فروق الكميات.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasViewCost = true
            },
            new ScreenDef {
                Key = "ShortageNotebook", Name = "كشكول النواقص والطلبات", Category = "📦 المخازن والأصناف", Icon = "📓",
                Description = "حصر الأصناف الناقصة تحت حد الطلب وتجهيز طلبيات التوريد.",
                HasAdd = true, HasEdit = true, HasViewCost = true
            },
            new ScreenDef {
                Key = "Wastage", Name = "تسجيل الهوالك والتالف", Category = "📦 المخازن والأصناف", Icon = "⚠️",
                Description = "تسجيل بضاعة تالفة أو منتهية الصلاحية وخصمها من المخزن.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasViewCost = true
            },
            new ScreenDef {
                Key = "WarehouseTransfer", Name = "التحويل المخزني بين الفروع", Category = "📦 المخازن والأصناف", Icon = "🔄",
                Description = "تحويل كميات أصناف من مخزن إلى مخزن آخر داخل المنشأة.",
                HasAdd = true, HasEdit = true, HasDelete = true
            },
            new ScreenDef {
                Key = "BulkPrintBarcodes", Name = "طباعة الباركود (مجمع وفردي)", Category = "📦 المخازن والأصناف", Icon = "🏷️",
                Description = "تصميم وطباعة ملصقات الباركود على طابعات الاستيكر المختلفة."
            },

            // ── 💰 4. المالية والخزائن والورديات ──────────────────────────
            new ScreenDef {
                Key = "CashBox", Name = "الخزنة والمصروفات والإيرادات", Category = "💰 المالية والخزائن والورديات", Icon = "💰",
                Description = "تسجيل حركات النقدية والمصروفات اليومية والإيرادات المتنوعة.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasChangeSafe = true, HasViewBalance = true,
                AddLabel = "➕ تسجيل حركة قبض أو صرف جديدة",
                EditLabel = "✏️ تعديل حركة نقدية سابقة",
                DeleteLabel = "🗑️ حذف حركة نقدية"
            },
            new ScreenDef {
                Key = "ReceiptVoucher", Name = "سندات القبض والصرف الرسمية", Category = "💰 المالية والخزائن والورديات", Icon = "📄",
                Description = "إصدار وطباعة سندات القبض والدفع المالية للعملاء والموردين.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasChangeSafe = true, HasViewBalance = true
            },
            new ScreenDef {
                Key = "DailyAccounts", Name = "الحسابات والمالية اليومية الشاملة", Category = "💰 المالية والخزائن والورديات", Icon = "📊",
                Description = "شاشة المتابعة المالية اليومية الشاملة لكافة حركات النقدية والأدراج.",
                HasAdd = true, HasEdit = true, HasChangeSafe = true, HasViewBalance = true, HasViewDetails = true
            },
            new ScreenDef {
                Key = "SafeAccounts", Name = "إدارة الخزن والحسابات البنكية", Category = "💰 المالية والخزائن والورديات", Icon = "🏛️",
                Description = "تعريف وتعديل الخزن الفرعية وحسابات البنوك ومحافظ فودافون كاش.",
                HasAdd = true, HasEdit = true, HasDelete = true, HasChangeSafe = true, HasViewBalance = true
            },
            new ScreenDef {
                Key = "ActualBalances", Name = "مطابقة الأرصدة الفعلية للنقدية", Category = "💰 المالية والخزائن والورديات", Icon = "💵",
                Description = "مطابقة النقدية الفعلية في الدرج مع الرصيد الدفتري المسجل.",
                HasViewBalance = true, HasViewDetails = true
            },
            new ScreenDef {
                Key = "DailyClosing", Name = "تقفيل اليومية ومراجعة المبيعات", Category = "💰 المالية والخزائن والورديات", Icon = "🔒",
                Description = "تقفيل اليومية وحساب إجمالي الإيرادات والمصروفات وترحيل الأرصدة.",
                HasViewBalance = true, HasViewDetails = true, HasViewSalesTotals = true
            },
            new ScreenDef {
                Key = "ShiftClose", Name = "إغلاق وتقفيل وردية الكاشير", Category = "💰 المالية والخزائن والورديات", Icon = "🔄",
                Description = "إنهاء شيفت الكاشير واستلام عهدة الدرج النقدية وترحيلها للخزنة.",
                HasViewBalance = true, HasViewDetails = true
            },
            new ScreenDef {
                Key = "FinancialPosition", Name = "الموقف المالي وقائمة المركز المالي", Category = "💰 المالية والخزائن والورديات", Icon = "📊",
                Description = "استعراض الأصول والخصوم ورأس المال وحقوق الشركاء والديون.",
                HasViewBalance = true, HasViewCost = true
            },

            // ── 📊 5. التقارير التفصيلية الشاملة ─────────────────────────
            new ScreenDef {
                Key = "Reports", Name = "بوابة التقارير والإحصائيات الشاملة", Category = "📊 التقارير التفصيلية الشاملة", Icon = "📊",
                Description = "استعراض والاطلاع على كافة تقارير المنشأة ومؤشرات الأداء.",
                HasViewCost = true, HasViewBalance = true, HasViewDetails = true
            },
            new ScreenDef {
                Key = "Financials", Name = "قائمة الدخل والأرباح والميزانية", Category = "📊 التقارير التفصيلية الشاملة", Icon = "📈",
                Description = "استعراض الأرباح التشغيلية وصافي ربح الفواتير وقائمة الدخل التفصيلية.",
                HasViewCost = true, HasViewBalance = true, HasViewDetails = true
            },
            new ScreenDef {
                Key = "RepDailySales", Name = "تقرير المبيعات اليومية التفصيلي", Category = "📊 التقارير التفصيلية الشاملة", Icon = "📅",
                Description = "تقرير فواتير ومبيعات اليوم مع تصنيف الكاش والآجل والفيزا."
            },
            new ScreenDef {
                Key = "RepSalesByProduct", Name = "تقرير مبيعات وأرباح الأصناف", Category = "📊 التقارير التفصيلية الشاملة", Icon = "📦",
                Description = "تقرير يوضح أكثر الأصناف مبيعاً وربحية خلال فترة زمنية.",
                HasViewCost = true
            },
            new ScreenDef {
                Key = "RepClientBalances", Name = "تقرير مديونيات وأرصدة العملاء", Category = "📊 التقارير التفصيلية الشاملة", Icon = "⚖️",
                Description = "استعراض كشف مديونيات كافة العملاء ومتابعة التحصيلات.",
                HasViewBalance = true
            },
            new ScreenDef {
                Key = "RepStores", Name = "تقرير أرصدة وتقييم المخزون", Category = "📊 التقارير التفصيلية الشاملة", Icon = "📦",
                Description = "استعراض كميات المخازن وقيمتها بسعر التكلفة وسعر البيع.",
                HasViewCost = true
            },

            // ── 🚚 6. المناديب والصيانة والإدارة ─────────────────────────
            new ScreenDef {
                Key = "DriverHandover", Name = "تسليم وتحميل بضاعة المندوب", Category = "🚚 المناديب والصيانة والإدارة", Icon = "📦",
                Description = "تسجيل حمولة بضاعة السيارة للمندوب وتتبع عهدة البضاعة.",
                HasAdd = true, HasEdit = true, HasDelete = true
            },
            new ScreenDef {
                Key = "DriverCustody", Name = "عهدة المناديب والتحصيلات المالية", Category = "🚚 المناديب والصيانة والإدارة", Icon = "💼",
                Description = "متابعة النقدية والتحصيلات المسلمة من المندوب إلى الخزينة.",
                HasAdd = true, HasEdit = true, HasViewBalance = true
            },
            new ScreenDef {
                Key = "Maintenance", Name = "تذاكر الصيانة وإدارة الأجهزة", Category = "🚚 المناديب والصيانة والإدارة", Icon = "🔧",
                Description = "إدارة كروت وتذاكر صيانة الأجهزة والسيارات ومتابعة مراحل التصليح.",
                HasAdd = true, HasEdit = true, HasDelete = true
            },
            new ScreenDef {
                Key = "Employees", Name = "إدارة الموظفين وتعديل الصلاحيات", Category = "🚚 المناديب والصيانة والإدارة", Icon = "👨‍💼",
                Description = "إضافة المستخدمين وتعيين كلمات المرور وضبط الصلاحيات.",
                HasAdd = true, HasEdit = true, HasDelete = true
            },
            new ScreenDef {
                Key = "Settings", Name = "الإعدادات العامة للنظام والطابعات", Category = "🚚 المناديب والصيانة والإدارة", Icon = "⚙️",
                Description = "تعديل اسم المنشأة واللوجو وإعدادات الطابعات والنسخ الاحتياطي.",
                HasEdit = true
            },
            new ScreenDef {
                Key = "CloudSync", Name = "ربط الموبايل والتزامن السحابي", Category = "🚚 المناديب والصيانة والإدارة", Icon = "☁️",
                Description = "إعدادات مزامنة بيانات المالك مع Firebase وتطبيق الموبايل.",
                HasEdit = true
            }
        };

        private readonly Dictionary<string, ScreenPermState> _permStates = new Dictionary<string, ScreenPermState>(StringComparer.OrdinalIgnoreCase);
        private ScreenDef _selectedScreen;

        // UI Controls
        private TextBox txtSearch;
        private FlowLayoutPanel pnlCategories;
        private DataGridView dgScreens;
        private Panel pnlDetailCard;
        private Label lblSelectedTitle, lblSelectedDesc;
        private CheckBox chkMasterAccess;
        private FlowLayoutPanel flowSubPerms;
        private Label lblStatsBadge, lblFooterStats;
        private string _activeCategoryFilter = "الكل";

        public FrmPermissions(int empID, string empName)
        {
            _empID = empID;
            _empName = empName;
            InitializeComponentCustom();
            LoadEmployeePermissions();
        }

        private void InitializeComponentCustom()
        {
            this.Text = $"🔐 ضبط صلاحيات الموظف: {_empName}";
            this.Size = new Size(1280, 820);
            this.MinimumSize = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== 1. Top Header Banner =====
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblTitle = new Label
            {
                Text = $"🔐 صلاحيات الموظف: {_empName}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            var lblSub = new Label
            {
                Text = "اختر أي شاشة من القائمة اليمنى لعرض وضبط كافة صلاحياتها الفرعية بالتفصيل مع شروحات واضحة ومعاني صريحة.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(170, 190, 215),
                AutoSize = true,
                Location = new Point(20, 42)
            };
            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // Preset Roles Bar (Top-Left inside Header)
            var flowPresets = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 620,
                Height = 65,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5, 12, 5, 5),
                BackColor = Color.Transparent
            };

            flowPresets.Controls.Add(MakePresetBtn("👑 مدير كامل", Color.FromArgb(192, 57, 43), () => ApplyPreset("Admin")));
            flowPresets.Controls.Add(MakePresetBtn("🛒 كاشير / بيع", Color.FromArgb(41, 128, 185), () => ApplyPreset("Cashier")));
            flowPresets.Controls.Add(MakePresetBtn("📥 مشتريات", Color.FromArgb(142, 68, 173), () => ApplyPreset("Purchases")));
            flowPresets.Controls.Add(MakePresetBtn("📦 أمين مخزن", Color.FromArgb(39, 174, 96), () => ApplyPreset("Inventory")));
            flowPresets.Controls.Add(MakePresetBtn("💰 محاسب مالي", Color.FromArgb(211, 84, 0), () => ApplyPreset("Accountant")));
            flowPresets.Controls.Add(MakePresetBtn("🧹 تفريغ الكل", Color.FromArgb(108, 122, 137), () => ApplyPreset("Clear")));

            pnlTop.Controls.Add(flowPresets);

            // ===== 2. Main Workspace Split (Right: Screen Navigation List, Left: Detailed Workspace) =====
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 420,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(15, 20, 30),
                RightToLeft = RightToLeft.Yes
            };

            // ── Right Panel: Categories & Screen List ─────────────────────
            var pnlRight = split.Panel1;
            pnlRight.BackColor = Theme.BgCard;
            pnlRight.Padding = new Padding(10);

            // Search Box
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 0, 0, 8) };
            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => FilterScreensList();

            var lblSearch = new Label
            {
                Text = "🔍 بحث سريع عن شاشة:",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                Padding = new Padding(0, 4, 8, 0)
            };
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(lblSearch);

            // Category Chips Bar
            pnlCategories = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 68,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 2, 0, 4),
                BackColor = Color.Transparent
            };
            BuildCategoryChips();

            // DataGridView of Screens List
            dgScreens = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                GridColor = Color.FromArgb(40, 48, 65),
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 42 },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Color.FromArgb(37, 99, 235),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(25, 33, 48),
                    ForeColor = Color.FromArgb(220, 230, 245),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };

            var colCheck = new DataGridViewCheckBoxColumn { Name = "colAccess", HeaderText = "👁️ الدخول", Width = 70 };
            var colName = new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "اسم الشاشة / القسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            var colBadge = new DataGridViewTextBoxColumn { Name = "colBadge", HeaderText = "الصلاحيات", Width = 95 };
            colBadge.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colBadge.DefaultCellStyle.ForeColor = Color.FromArgb(70, 200, 240);

            dgScreens.Columns.AddRange(new DataGridViewColumn[] { colCheck, colName, colBadge });
            dgScreens.SelectionChanged += DgScreens_SelectionChanged;
            dgScreens.CellValueChanged += DgScreens_CellValueChanged;
            dgScreens.CurrentCellDirtyStateChanged += (s, e) => { if (dgScreens.IsCurrentCellDirty) dgScreens.CommitEdit(DataGridViewDataErrorContexts.Commit); };

            pnlRight.Controls.Add(dgScreens);
            pnlRight.Controls.Add(pnlCategories);
            pnlRight.Controls.Add(pnlSearch);

            // ── Left Panel: Detailed Permissions Card Workspace ──────────
            var pnlLeft = split.Panel2;
            pnlLeft.BackColor = Color.FromArgb(16, 22, 34);
            pnlLeft.Padding = new Padding(15);

            pnlDetailCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(20),
                AutoScroll = true
            };

            // Selected Screen Header inside Left Card
            var pnlDetailHeader = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.FromArgb(25, 33, 48), Padding = new Padding(15, 12, 15, 12) };

            lblSelectedTitle = new Label
            {
                Text = "🛒 فاتورة المبيعات الرئيسية",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 198, 35),
                AutoSize = true,
                Location = new Point(15, 10)
            };

            lblSelectedDesc = new Label
            {
                Text = "شاشة إصدار فواتير البيع للعملاء وإدارة الحسابات النقدية والآجلة.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(180, 195, 220),
                AutoSize = true,
                Location = new Point(15, 42)
            };

            lblStatsBadge = new Label
            {
                Text = "مفعلة بالكامل 🟢",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 204, 113),
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(10, 15, 0, 0)
            };

            pnlDetailHeader.Controls.AddRange(new Control[] { lblSelectedTitle, lblSelectedDesc, lblStatsBadge });

            // Master Access Switch Bar
            var pnlMaster = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(32, 42, 60),
                Padding = new Padding(15, 12, 15, 12),
                Margin = new Padding(0, 10, 0, 10)
            };

            chkMasterAccess = new CheckBox
            {
                Text = "👁️ السماح للموظف بفتح واستخدام هذه الشاشة في البرنامج",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Right,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            chkMasterAccess.CheckedChanged += ChkMasterAccess_CheckedChanged;

            var btnSelectAllCurrent = MakeSmallBtn("✔️ تحديد كافة الصلاحيات", Color.FromArgb(5, 150, 105), () => ToggleCurrentScreenSubPerms(true));
            var btnClearAllCurrent = MakeSmallBtn("❌ إلغاء الصلاحيات الفرعية", Color.FromArgb(140, 60, 70), () => ToggleCurrentScreenSubPerms(false));
            btnSelectAllCurrent.Location = new Point(230, 15);
            btnClearAllCurrent.Location = new Point(45, 15);

            pnlMaster.Controls.AddRange(new Control[] { chkMasterAccess, btnSelectAllCurrent, btnClearAllCurrent });

            // Sub-Permissions Scrollable Container
            flowSubPerms = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(5, 15, 5, 15),
                BackColor = Color.Transparent
            };

            pnlDetailCard.Controls.Add(flowSubPerms);
            pnlDetailCard.Controls.Add(pnlMaster);
            pnlDetailCard.Controls.Add(pnlDetailHeader);

            pnlLeft.Controls.Add(pnlDetailCard);

            // ===== 3. Bottom Footer Actions Bar =====
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(20, 12, 20, 12)
            };

            lblFooterStats = new Label
            {
                Text = "📊 إجمالي الشاشات المسموحة: 0 / 0 | 🛡️ الصلاحيات الفرعية المفعلة: 0",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 200, 240),
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            };

            var btnSavePerms = Theme.MakeButton("💾 حفظ وتطبيق الصلاحيات فوراً [F5]", Color.FromArgb(16, 185, 129));
            btnSavePerms.Size = new Size(270, 42);
            btnSavePerms.Location = new Point(20, 10);
            btnSavePerms.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSavePerms.Click += BtnSave_Click;

            var btnClose = Theme.MakeButton("❌ إلغاء وخروج", Color.FromArgb(100, 110, 125));
            btnClose.Size = new Size(130, 42);
            btnClose.Location = new Point(300, 10);
            btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.AddRange(new Control[] { btnSavePerms, btnClose, lblFooterStats });

            this.Controls.Add(split);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlTop);

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F5) { BtnSave_Click(s, e); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) this.Close();
            };

            Theme.ApplyFormRTL(this);
        }

        private void BuildCategoryChips()
        {
            pnlCategories.Controls.Clear();
            string[] cats = { "الكل", "🛒 المبيعات", "📥 المشتريات", "📦 المخازن", "💰 المالية", "📊 التقارير", "🚚 الإدارة" };

            foreach (var cat in cats)
            {
                var btn = new Button
                {
                    Text = cat,
                    Height = 28,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = (_activeCategoryFilter == cat) ? Color.FromArgb(37, 99, 235) : Color.FromArgb(35, 42, 58),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(2, 2, 2, 2)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) =>
                {
                    _activeCategoryFilter = cat;
                    BuildCategoryChips();
                    FilterScreensList();
                };
                pnlCategories.Controls.Add(btn);
            }
        }

        private void LoadEmployeePermissions()
        {
            try
            {
                var dt = EmployeeDAL.GetPermissions(_empID);
                var dict = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in dt.Rows)
                {
                    string sName = r["ScreenName"]?.ToString();
                    if (!string.IsNullOrEmpty(sName)) dict[sName] = r;
                }

                foreach (var def in AllScreens)
                {
                    var st = new ScreenPermState();
                    if (dict.TryGetValue(def.Key, out DataRow r))
                    {
                        st.CanAccess = ToBool(r["CanAccess"]);
                        st.CanAdd = ToBool(r["CanAdd"]);
                        st.CanEdit = ToBool(r["CanEdit"]);
                        st.CanDelete = ToBool(r["CanDelete"]);
                        st.CanEditPrice = ToBool(r["CanEditPrice"]);
                        st.CanEditSalesInvoice = ToBool(r["CanEditSalesInvoice"]);
                        st.CanDeleteSalesInvoice = ToBool(r["CanDeleteSalesInvoice"]);
                        st.CanCopySalesInvoice = ToBool(r["CanCopySalesInvoice"]);
                        st.CanViewCost = ToBool(r["CanViewCost"]);
                        st.CanOrderColumns = ToBool(r["CanOrderColumns"]);
                        st.CanViewDetails = ToBool(r["CanViewDetails"]);
                        st.CanViewBalance = ToBool(r["CanViewBalance"]);
                        st.CanChangeSafe = ToBool(r["CanChangeSafe"]);
                        st.CanViewSalesTotals = ToBool(r["CanViewSalesTotals"]);
                        st.CanViewQuickItems = ToBool(r["CanViewQuickItems"]);
                    }
                    else
                    {
                        st.CanAccess = false;
                        st.CanAdd = true;
                        st.CanEdit = true;
                        st.CanCopySalesInvoice = true;
                        st.CanViewDetails = true;
                        st.CanViewSalesTotals = true;
                        st.CanViewQuickItems = true;
                    }
                    _permStates[def.Key] = st;
                }

                FilterScreensList();

                if (dgScreens.Rows.Count > 0)
                {
                    dgScreens.Rows[0].Selected = true;
                    SelectScreen(AllScreens[0]);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadEmployeePermissions", ex);
            }
        }

        private void FilterScreensList()
        {
            dgScreens.Rows.Clear();
            string q = txtSearch.Text.Trim().ToLowerInvariant();

            foreach (var def in AllScreens)
            {
                if (_activeCategoryFilter != "الكل" && !def.Category.Contains(_activeCategoryFilter.Replace("🛒", "").Replace("📥", "").Replace("📦", "").Replace("💰", "").Replace("📊", "").Replace("🚚", "").Trim()))
                    continue;

                if (!string.IsNullOrEmpty(q) && !def.Name.ToLowerInvariant().Contains(q) && !def.Key.ToLowerInvariant().Contains(q) && !def.Category.ToLowerInvariant().Contains(q))
                    continue;

                var st = _permStates.ContainsKey(def.Key) ? _permStates[def.Key] : new ScreenPermState();
                int totalSub = CountAvailableSubPerms(def);
                int activeSub = CountActiveSubPerms(def, st);

                int ri = dgScreens.Rows.Add(st.CanAccess, $"{def.Icon} {def.Name}", st.CanAccess ? $"{activeSub}/{totalSub} مفعلة" : "❌ مقفلة");
                dgScreens.Rows[ri].Tag = def;
            }
            UpdateGlobalCounters();
        }

        private int CountAvailableSubPerms(ScreenDef def)
        {
            int cnt = 0;
            if (def.HasAdd) cnt++;
            if (def.HasEdit) cnt++;
            if (def.HasDelete) cnt++;
            if (def.HasEditPrice) cnt++;
            if (def.HasEditSalesInvoice) cnt++;
            if (def.HasDeleteSalesInvoice) cnt++;
            if (def.HasCopySalesInvoice) cnt++;
            if (def.HasViewCost) cnt++;
            if (def.HasViewBalance) cnt++;
            if (def.HasViewDetails) cnt++;
            if (def.HasChangeSafe) cnt++;
            if (def.HasViewSalesTotals) cnt++;
            if (def.HasViewQuickItems) cnt++;
            if (def.HasOrderColumns) cnt++;
            return cnt;
        }

        private int CountActiveSubPerms(ScreenDef def, ScreenPermState st)
        {
            int cnt = 0;
            if (def.HasAdd && st.CanAdd) cnt++;
            if (def.HasEdit && st.CanEdit) cnt++;
            if (def.HasDelete && st.CanDelete) cnt++;
            if (def.HasEditPrice && st.CanEditPrice) cnt++;
            if (def.HasEditSalesInvoice && st.CanEditSalesInvoice) cnt++;
            if (def.HasDeleteSalesInvoice && st.CanDeleteSalesInvoice) cnt++;
            if (def.HasCopySalesInvoice && st.CanCopySalesInvoice) cnt++;
            if (def.HasViewCost && st.CanViewCost) cnt++;
            if (def.HasViewBalance && st.CanViewBalance) cnt++;
            if (def.HasViewDetails && st.CanViewDetails) cnt++;
            if (def.HasChangeSafe && st.CanChangeSafe) cnt++;
            if (def.HasViewSalesTotals && st.CanViewSalesTotals) cnt++;
            if (def.HasViewQuickItems && st.CanViewQuickItems) cnt++;
            if (def.HasOrderColumns && st.CanOrderColumns) cnt++;
            return cnt;
        }

        private void DgScreens_SelectionChanged(object sender, EventArgs e)
        {
            if (dgScreens.SelectedRows.Count > 0 && dgScreens.SelectedRows[0].Tag is ScreenDef def)
            {
                SelectScreen(def);
            }
        }

        private void DgScreens_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                var row = dgScreens.Rows[e.RowIndex];
                if (row.Tag is ScreenDef def)
                {
                    bool access = ToBool(row.Cells[0].Value);
                    if (_permStates.ContainsKey(def.Key))
                    {
                        _permStates[def.Key].CanAccess = access;
                        if (access)
                        {
                            _permStates[def.Key].CanAdd = true;
                            _permStates[def.Key].CanEdit = true;
                            _permStates[def.Key].CanCopySalesInvoice = true;
                        }
                    }

                    if (_selectedScreen == def)
                    {
                        SelectScreen(def);
                    }
                    UpdateScreenRowBadge(row, def);
                    UpdateGlobalCounters();
                }
            }
        }

        private void SelectScreen(ScreenDef def)
        {
            _selectedScreen = def;
            var st = _permStates.ContainsKey(def.Key) ? _permStates[def.Key] : new ScreenPermState();

            lblSelectedTitle.Text = $"{def.Icon}  {def.Name}";
            lblSelectedDesc.Text = def.Description;

            chkMasterAccess.CheckedChanged -= ChkMasterAccess_CheckedChanged;
            chkMasterAccess.Checked = st.CanAccess;
            chkMasterAccess.CheckedChanged += ChkMasterAccess_CheckedChanged;

            int total = CountAvailableSubPerms(def);
            int active = CountActiveSubPerms(def, st);
            lblStatsBadge.Text = st.CanAccess ? $"مفعلة ({active} من {total} صلاحيات) 🟢" : "الشاشة مقفلة ⛔";
            lblStatsBadge.ForeColor = st.CanAccess ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60);

            BuildSubPermissionsCards(def, st);
        }

        private void ChkMasterAccess_CheckedChanged(object sender, EventArgs e)
        {
            if (_selectedScreen == null) return;
            bool access = chkMasterAccess.Checked;
            if (_permStates.ContainsKey(_selectedScreen.Key))
            {
                _permStates[_selectedScreen.Key].CanAccess = access;
                if (access)
                {
                    _permStates[_selectedScreen.Key].CanAdd = true;
                    _permStates[_selectedScreen.Key].CanEdit = true;
                    _permStates[_selectedScreen.Key].CanCopySalesInvoice = true;
                }
            }

            foreach (DataGridViewRow r in dgScreens.Rows)
            {
                if (r.Tag == _selectedScreen)
                {
                    r.Cells[0].Value = access;
                    UpdateScreenRowBadge(r, _selectedScreen);
                    break;
                }
            }

            SelectScreen(_selectedScreen);
            UpdateGlobalCounters();
        }

        private void BuildSubPermissionsCards(ScreenDef def, ScreenPermState st)
        {
            flowSubPerms.SuspendLayout();
            flowSubPerms.Controls.Clear();

            bool isEnabled = st.CanAccess;

            if (!isEnabled)
            {
                var pnlDisabled = new Panel
                {
                    Width = 720,
                    Height = 90,
                    BackColor = Color.FromArgb(30, 25, 30),
                    Padding = new Padding(20),
                    Margin = new Padding(5, 10, 5, 10)
                };
                var lblDis = new Label
                {
                    Text = "⛔ هذه الشاشة مقفلة حالياً لهذا الموظف!\nقم بتفعيل مفتاح (السماح بالدخول للشاشة) أعلاه لتمكين وضبط صلاحياتها الفرعية.",
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(240, 120, 120),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlDisabled.Controls.Add(lblDis);
                flowSubPerms.Controls.Add(pnlDisabled);
                flowSubPerms.ResumeLayout();
                return;
            }

            // 1. CanAdd
            if (def.HasAdd)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard(def.AddLabel, def.AddHint, st.CanAdd, v => { st.CanAdd = v; OnSubPermChanged(def); }));
            }

            // 2. CanEdit
            if (def.HasEdit)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard(def.EditLabel, def.EditHint, st.CanEdit, v => { st.CanEdit = v; OnSubPermChanged(def); }));
            }

            // 3. CanEditSalesInvoice (فواتير الغير)
            if (def.HasEditSalesInvoice)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard(def.OtherSalesLabel, def.OtherSalesHint, st.CanEditSalesInvoice, v => { st.CanEditSalesInvoice = v; OnSubPermChanged(def); }, Color.FromArgb(243, 156, 18)));
            }

            // 4. CanEditPrice
            if (def.HasEditPrice)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard(def.EditPriceLabel, def.EditPriceHint, st.CanEditPrice, v => { st.CanEditPrice = v; OnSubPermChanged(def); }));
            }

            // 5. CanDelete
            if (def.HasDelete)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard(def.DeleteLabel, def.DeleteHint, st.CanDelete, v => { st.CanDelete = v; OnSubPermChanged(def); }, Color.FromArgb(231, 76, 60)));
            }

            // 6. CanDeleteSalesInvoice
            if (def.HasDeleteSalesInvoice)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("❌ حذف وإلغاء فواتير المبيعات الصادرة", "السماح للموظف بحذف فواتير البيع بالكامل وشطبها من السجل.", st.CanDeleteSalesInvoice, v => { st.CanDeleteSalesInvoice = v; OnSubPermChanged(def); }, Color.FromArgb(231, 76, 60)));
            }

            // 7. CanCopySalesInvoice
            if (def.HasCopySalesInvoice)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("📋 نسخ وإعادة طباعة الفواتير السابقة", "السماح بإعادة طباعة الإيصالات أو نسخ بنود الفاتورة لإنشاء فاتورة جديدة.", st.CanCopySalesInvoice, v => { st.CanCopySalesInvoice = v; OnSubPermChanged(def); }));
            }

            // 8. CanViewCost
            if (def.HasViewCost)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("💲 الاطلاع على سعر التكلفة والربح", "إظهار سعر الشراء والتكلفة وأرباح الفاتورة وهوامش الأصناف للموظف.", st.CanViewCost, v => { st.CanViewCost = v; OnSubPermChanged(def); }, Color.FromArgb(155, 89, 182)));
            }

            // 9. CanViewBalance
            if (def.HasViewBalance)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("💰 الاطلاع على الرصيد الفعلي وكشف الحساب", "كشف الرصيد الفعلي المتوفر في الدرج أو كشف مديونيات وحسابات العملاء والموردين.", st.CanViewBalance, v => { st.CanViewBalance = v; OnSubPermChanged(def); }));
            }

            // 10. CanViewDetails (التقفيل)
            if (def.HasViewDetails)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("📄 الاطلاع على تفاصيل التقفيل والشيفت", "كشف المبالغ النقدية المتوقعة ومبيعات الكاش والفيزا عند إغلاق الوردية.", st.CanViewDetails, v => { st.CanViewDetails = v; OnSubPermChanged(def); }));
            }

            // 11. CanChangeSafe
            if (def.HasChangeSafe)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("🔄 تغيير الخزينة / الحساب المالي", "السماح باختيار خزينة أو درج مالي آخر بخلاف الدرج الافتراضي المحدد للموظف.", st.CanChangeSafe, v => { st.CanChangeSafe = v; OnSubPermChanged(def); }));
            }

            // 12. CanViewSalesTotals
            if (def.HasViewSalesTotals)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("📊 عرض شريط الإجماليات والأرباح في السجل", "إظهار شريط الإجماليات المالية السفلية في سجل الفواتير والمبيعات.", st.CanViewSalesTotals, v => { st.CanViewSalesTotals = v; OnSubPermChanged(def); }));
            }

            // 13. CanViewQuickItems
            if (def.HasViewQuickItems)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("⚡ إظهار أزرار الأصناف السريعة", "عرض شبكة أزرار الأصناف الأكثر مبيعاً في شاشة الكاشير POS.", st.CanViewQuickItems, v => { st.CanViewQuickItems = v; OnSubPermChanged(def); }));
            }

            // 14. CanOrderColumns
            if (def.HasOrderColumns)
            {
                flowSubPerms.Controls.Add(MakeSubPermCard("↕️ ترتيب وتخصيص أعمدة الجدول", "السماح للموظف بسحب وتغيير ترتيب وعرض الأعمدة في الجداول.", st.CanOrderColumns, v => { st.CanOrderColumns = v; OnSubPermChanged(def); }));
            }

            flowSubPerms.ResumeLayout();
        }

        private Panel MakeSubPermCard(string title, string hint, bool isChecked, Action<bool> onChange, Color? accentColor = null)
        {
            var pnl = new Panel
            {
                Width = 720,
                Height = 72,
                BackColor = isChecked ? Color.FromArgb(28, 38, 56) : Color.FromArgb(22, 28, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15, 8, 15, 8),
                Margin = new Padding(4, 5, 4, 5),
                Cursor = Cursors.Hand
            };

            var chk = new CheckBox
            {
                Text = title,
                Checked = isChecked,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = isChecked ? (accentColor ?? Color.FromArgb(70, 200, 240)) : Color.FromArgb(200, 210, 225),
                Dock = DockStyle.Top,
                Height = 28,
                Cursor = Cursors.Hand
            };

            var lblHint = new Label
            {
                Text = hint,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = isChecked ? Color.FromArgb(170, 190, 215) : Color.FromArgb(130, 140, 155),
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };

            EventHandler toggleHandler = (s, e) =>
            {
                if (s != chk) chk.Checked = !chk.Checked;
            };

            chk.CheckedChanged += (s, e) =>
            {
                bool val = chk.Checked;
                pnl.BackColor = val ? Color.FromArgb(28, 38, 56) : Color.FromArgb(22, 28, 40);
                chk.ForeColor = val ? (accentColor ?? Color.FromArgb(70, 200, 240)) : Color.FromArgb(200, 210, 225);
                lblHint.ForeColor = val ? Color.FromArgb(170, 190, 215) : Color.FromArgb(130, 140, 155);
                onChange(val);
            };

            pnl.Click += toggleHandler;
            lblHint.Click += toggleHandler;

            pnl.Controls.Add(lblHint);
            pnl.Controls.Add(chk);
            return pnl;
        }

        private void OnSubPermChanged(ScreenDef def)
        {
            var st = _permStates[def.Key];
            int total = CountAvailableSubPerms(def);
            int active = CountActiveSubPerms(def, st);
            lblStatsBadge.Text = $"مفعلة ({active} من {total} صلاحيات) 🟢";

            foreach (DataGridViewRow r in dgScreens.Rows)
            {
                if (r.Tag == def)
                {
                    UpdateScreenRowBadge(r, def);
                    break;
                }
            }
            UpdateGlobalCounters();
        }

        private void ToggleCurrentScreenSubPerms(bool enable)
        {
            if (_selectedScreen == null) return;
            var st = _permStates[_selectedScreen.Key];
            if (!st.CanAccess && enable)
            {
                st.CanAccess = true;
                chkMasterAccess.Checked = true;
            }

            st.CanAdd = enable;
            st.CanEdit = enable;
            st.CanDelete = enable;
            st.CanEditPrice = enable;
            st.CanEditSalesInvoice = enable;
            st.CanDeleteSalesInvoice = enable;
            st.CanCopySalesInvoice = enable;
            st.CanViewCost = enable;
            st.CanViewBalance = enable;
            st.CanViewDetails = enable;
            st.CanChangeSafe = enable;
            st.CanViewSalesTotals = enable;
            st.CanViewQuickItems = enable;
            st.CanOrderColumns = enable;

            SelectScreen(_selectedScreen);
            foreach (DataGridViewRow r in dgScreens.Rows)
            {
                if (r.Tag == _selectedScreen)
                {
                    UpdateScreenRowBadge(r, _selectedScreen);
                    break;
                }
            }
            UpdateGlobalCounters();
        }

        private void UpdateScreenRowBadge(DataGridViewRow row, ScreenDef def)
        {
            var st = _permStates[def.Key];
            int total = CountAvailableSubPerms(def);
            int active = CountActiveSubPerms(def, st);
            row.Cells[2].Value = st.CanAccess ? $"{active}/{total} مفعلة" : "❌ مقفلة";
        }

        private void UpdateGlobalCounters()
        {
            int allowed = 0, total = AllScreens.Count, subActive = 0;
            foreach (var def in AllScreens)
            {
                if (_permStates.TryGetValue(def.Key, out ScreenPermState st))
                {
                    if (st.CanAccess)
                    {
                        allowed++;
                        subActive += CountActiveSubPerms(def, st);
                    }
                }
            }
            lblFooterStats.Text = $"📊 إجمالي الشاشات المسموحة: {allowed} من {total} | 🛡️ الصلاحيات الفرعية المفعلة: {subActive}";
        }

        private void ApplyPreset(string role)
        {
            var salesScreens = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sales", "POS", "PriceQuote", "Returns", "PriceChecker", "ProductSearch", "Clients", "ClientStatement", "Installments", "SalesList" };
            var purchaseScreens = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Purchases", "PurchaseReturn", "PurchasesList", "Suppliers", "SupplierStatement", "SupplierPayment", "SupplierAdjustment" };
            var inventoryScreens = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Products", "ProductCard", "Categories", "Units", "Warehouses", "Inventory", "ShortageNotebook", "Wastage", "WarehouseTransfer", "BulkPrintBarcodes", "RepStores" };
            var accountantScreens = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CashBox", "ReceiptVoucher", "DailyAccounts", "SafeAccounts", "ActualBalances", "DailyClosing", "ShiftClose", "FinancialPosition", "Reports", "Financials", "RepDailySales", "RepSalesByProduct", "RepClientBalances", "RepStores" };

            foreach (var def in AllScreens)
            {
                var st = _permStates[def.Key];
                bool enable = false;

                if (role == "Admin") enable = true;
                else if (role == "Cashier" && salesScreens.Contains(def.Key)) enable = true;
                else if (role == "Purchases" && purchaseScreens.Contains(def.Key)) enable = true;
                else if (role == "Inventory" && inventoryScreens.Contains(def.Key)) enable = true;
                else if (role == "Accountant" && (accountantScreens.Contains(def.Key) || salesScreens.Contains(def.Key))) enable = true;

                st.CanAccess = enable;
                st.CanAdd = enable;
                st.CanEdit = enable;
                st.CanCopySalesInvoice = enable;
                st.CanViewDetails = enable;
                st.CanViewSalesTotals = enable;
                st.CanViewQuickItems = enable;

                if (role == "Admin")
                {
                    st.CanDelete = true;
                    st.CanEditPrice = true;
                    st.CanEditSalesInvoice = true;
                    st.CanDeleteSalesInvoice = true;
                    st.CanViewCost = true;
                    st.CanViewBalance = true;
                    st.CanChangeSafe = true;
                    st.CanOrderColumns = true;
                }
                else
                {
                    st.CanDelete = false;
                    st.CanDeleteSalesInvoice = false;
                    st.CanViewCost = (role == "Accountant");
                    st.CanEditPrice = (role == "Accountant" || role == "Purchases");
                    st.CanEditSalesInvoice = (role == "Accountant");
                }
            }

            FilterScreensList();
            if (_selectedScreen != null) SelectScreen(_selectedScreen);
        }

        private Button MakePresetBtn(string text, Color bg, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(110, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 0, 3, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private Button MakeSmallBtn(string text, Color bg, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(175, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private static bool ToBool(object val)
        {
            if (val == null) return false;
            if (val is bool b) return b;
            var s = val.ToString().Trim().ToLowerInvariant();
            return s == "true" || s == "1" || s == "yes";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var def in AllScreens)
                {
                    if (_permStates.TryGetValue(def.Key, out ScreenPermState st))
                    {
                        EmployeeDAL.SavePermissions(_empID, def.Key,
                            st.CanAccess, st.CanAdd, st.CanEdit, st.CanDelete,
                            st.CanEditPrice, st.CanEditSalesInvoice, st.CanDeleteSalesInvoice,
                            st.CanCopySalesInvoice, st.CanViewCost, st.CanOrderColumns,
                            st.CanViewDetails, st.CanViewBalance, st.CanChangeSafe,
                            st.CanViewSalesTotals, st.CanViewQuickItems);
                    }
                }

                MessageBox.Show($"✅ تم حفظ وتطبيق صلاحيات الموظف ({_empName}) بنجاح!", "حفظ الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ حدث خطأ أثناء حفظ الصلاحيات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

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

    /// <summary>شاشة الصلاحيات الاحترافية المحدثة — 5 تبويبات وجميع الشاشات</summary>
    public class FrmPermissions : Form
    {
        private readonly int _empID;
        private readonly string _empName;
        private TabControl tcPerms;
        private DataGridView dgSales;
        private DataGridView dgPurchases;
        private DataGridView dgInventory;
        private DataGridView dgFinance;
        private DataGridView dgReports;
        private DataGridView dgAdmin;
        private TextBox txtSearch;
        private Label lblCounter;
        private Button btnSave;

        private struct ScreenInfo
        {
            public string Key;
            public string Name;
            public int TabIndex; // 0: Sales & Clients, 1: Purchases & Suppliers, 2: Inventory & Products, 3: Finance & Safes, 4: Detailed Reports, 5: Drivers, Maintenance & Admin
            public ScreenInfo(string key, string name, int tabIndex)
            {
                Key = key;
                Name = name;
                TabIndex = tabIndex;
            }
        }

        private static readonly ScreenInfo[] ScreensList = {
            // ── Tab 0: Sales & Clients (المبيعات والعملاء) ──────────────────────────
            new ScreenInfo("Sales", "🛒 شاشة المبيعات الرئيسية (فاتورة بيع)", 0),
            new ScreenInfo("POS", "⚡ نقطة البيع السريعة POS", 0),
            new ScreenInfo("PriceQuote", "📋 بيان تسعير وعروض الأسعار", 0),
            new ScreenInfo("ProductSearch", "🔍 شاشة بحث الأصناف وتعديل الأسعار", 0),
            new ScreenInfo("Returns", "↩️ مرتجع المبيعات", 0),
            new ScreenInfo("Installments", "📜 عقود التقسيط الشرعي", 0),
            new ScreenInfo("Reservations", "📋 حجوزات العملاء", 0),
            new ScreenInfo("ClearanceOffers", "🏷️ الأوكازيون والعروض", 0),
            new ScreenInfo("SalesList", "📋 سجل الفواتير والمبيعات", 0),
            new ScreenInfo("SalesAudit", "🔍 سجل تعديلات وحذف الفواتير", 0),
            new ScreenInfo("AccountantPortal", "🌐 بوابة المحاسبة الميدانية", 0),
            new ScreenInfo("Clients", "👥 إدارة وتعديل بيانات العملاء", 0),
            new ScreenInfo("ClientStatement", "📄 كشف حساب العميل التفصيلي", 0),
            new ScreenInfo("InactiveClients", "💤 تنشيط العملاء الراكدين", 0),
            new ScreenInfo("Vehicles", "🚚 إدارة المركبات والسيارات", 0),
            new ScreenInfo("DashSales", "🏠 لوحة التحكم: مبيعات اليوم", 0),

            // ── Tab 1: Purchases & Suppliers (المشتريات والموردين) ─────────────────
            new ScreenInfo("Purchases", "📥 شاشة المشتريات الرئيسية (فاتورة شراء)", 1),
            new ScreenInfo("PurchaseReturn", "↩️ مرتجع المشتريات", 1),
            new ScreenInfo("PurchasesList", "📋 سجل الفواتير والمشتريات", 1),
            new ScreenInfo("Suppliers", "🏢 إدارة وتعديل الموردين", 1),
            new ScreenInfo("SupplierStatement", "📄 كشف حساب المورد التفصيلي", 1),
            new ScreenInfo("SupplierPayment", "💵 صرف نقدي لمورد", 1),
            new ScreenInfo("SupplierAdjustment", "⚖️ تسوية أرصدة الموردين", 1),

            // ── Tab 2: Inventory & Products (المخازن والأصناف) ─────────────────────
            new ScreenInfo("Products", "🏷️ إدارة وتعديل الأصناف والأسعار", 2),
            new ScreenInfo("ProductCard", "💳 كارت الصنف والمواصفات", 2),
            new ScreenInfo("Categories", "🗂️ التصنيفات والأقسام", 2),
            new ScreenInfo("Units", "📏 إدارة وحدات قياس الأصناف", 2),
            new ScreenInfo("ImportProducts", "📊 استيراد الأصناف من إكسيل", 2),
            new ScreenInfo("Warehouses", "🏭 إدارة المخازن والمستودعات", 2),
            new ScreenInfo("Inventory", "📦 جرد وتعديل رصيد المخزن", 2),
            new ScreenInfo("MinStockEdit", "🎯 تعديل حد طلب الأصناف والنواقص", 2),
            new ScreenInfo("ShortageNotebook", "📓 كشكول النواقص والطلبات الخاصة", 2),
            new ScreenInfo("InventoryVarianceReport", "📊 تقرير فروق وعجز الجرد الشامل", 2),
            new ScreenInfo("Wastage", "⚠️ تسجيل الهوالك والتالف", 2),
            new ScreenInfo("WarehouseTransfer", "🔄 تحويل مخزني صادر", 2),
            new ScreenInfo("WarehouseTransfersList", "📋 سجل التحويلات المخزنية", 2),
            new ScreenInfo("PriceChanges", "📉 سجل تغير وحركات الأسعار", 2),
            new ScreenInfo("PricePoster", "📋 لستة الأصناف (منشور الأسعار)", 2),
            new ScreenInfo("ProductMovement", "📊 تقرير وحركة الصنف التفصيلي", 2),
            new ScreenInfo("BulkPrintBarcodes", "🏷️ طباعة الباركود (مجمع)", 2),
            new ScreenInfo("ClothingMatrix", "👔 مصفوفة مقاسات وألوان الملابس", 2),
            new ScreenInfo("ModelLookup", "🔍 دليل الموديلات والأجهزة", 2),
            new ScreenInfo("MultiBarcodes", "🔢 إدارة الباركودات المتعددة", 2),
            new ScreenInfo("DashBelowMin", "🏠 لوحة التحكم: الأصناف تحت حد الطلب", 2),

            // ── Tab 3: Finance, Safes & Shifts (المالية والخزائن والورديات) ────────
            new ScreenInfo("CashBox", "💰 الخزنة والمصروفات والوارد", 3),
            new ScreenInfo("SafeAccounts", "🏛️ إدارة حسابات الخزائن الفرعية", 3),
            new ScreenInfo("ActualBalances", "💵 مطابقة الأرصدة الفعلية للنقدية", 3),
            new ScreenInfo("DailyAccounts", "📊 الحسابات والمالية اليومية الشاملة", 3),
            new ScreenInfo("ReceiptVoucher", "📄 إصدار سندات القبض والدفع", 3),
            new ScreenInfo("FinancialPosition", "📊 الموقف المالي الشامل للمكان", 3),
            new ScreenInfo("DailyClosing", "🔒 تقفيل يومية المبيعات", 3),
            new ScreenInfo("ShiftClose", "🔄 شاشة إدارة وإغلاق الوردية", 3),
            new ScreenInfo("ShiftsHistory", "📊 تقرير وسجل الورديات السابقة", 3),
            new ScreenInfo("DashTreasury", "🏠 لوحة التحكم: رصيد الخزنة الحالي", 3),

            // ── Tab 4: Detailed Reports (التقارير التفصيلية الشاملة) ───────────────
            new ScreenInfo("Reports", "📊 التقارير والإحصائيات العامة (كل التقارير)", 4),
            new ScreenInfo("RepDailySales", "📅 تقرير المبيعات اليومية", 4),
            new ScreenInfo("RepSalesByPeriod", "📈 تقرير المبيعات خلال فترة", 4),
            new ScreenInfo("RepDetailedSales", "🧾 سجل فواتير المبيعات التفصيلي", 4),
            new ScreenInfo("RepDetailedSaleItems", "📦 تفاصيل سطور وأصناف المبيعات", 4),
            new ScreenInfo("RepSalesByProduct", "📊 تقرير مبيعات الأصناف والربحية", 4),
            new ScreenInfo("RepSalesByCategory", "🏢 تقرير مبيعات المجموعات والأقسام", 4),
            new ScreenInfo("RepSalesByClient", "👥 تقرير مبيعات العملاء والمسدد", 4),
            new ScreenInfo("RepSalesByUser", "👔 تقرير مبيعات المستخدمين والكاشير", 4),
            new ScreenInfo("RepSalesByPayment", "💳 تقرير طرق الدفع والتحصيل", 4),
            new ScreenInfo("RepSalesDiscounts", "🏷️ تقرير الخصومات والتخفيضات", 4),
            new ScreenInfo("RepDetailedReturns", "🔄 تقرير مرتجعات المبيعات", 4),
            new ScreenInfo("RepSalesProfit", "💰 تقرير أرباح وهامش المبيعات", 4),
            new ScreenInfo("RepStagnantProducts", "💤 تقرير الأصناف الراكدة", 4),
            new ScreenInfo("RepPurchases", "📊 تقارير وإحصائيات المشتريات الشاملة", 4),
            new ScreenInfo("RepDailyPurchases", "📅 تقرير المشتريات اليومية", 4),
            new ScreenInfo("RepPurchasesByPeriod", "📈 تقرير المشتريات خلال فترة", 4),
            new ScreenInfo("RepDetailedPurchases", "🧾 سجل فواتير المشتريات التفصيلي", 4),
            new ScreenInfo("RepDetailedPurchaseItems", "📦 تفاصيل سطور وأصناف المشتريات", 4),
            new ScreenInfo("RepPurchasesBySupplier", "🤝 مشتريات الموردين والمسدد", 4),
            new ScreenInfo("RepPurchasesByProduct", "📊 مشتريات الأصناف ومتوسط التكلفة", 4),
            new ScreenInfo("RepPurchasesByCategory", "🏢 مشتريات الأقسام والتصنيفات", 4),
            new ScreenInfo("RepPurchaseReturns", "🔄 مرتجعات المشتريات التفصيلي", 4),
            new ScreenInfo("RepSupplierPayments", "💵 المدفوعات للموردين والتسويات", 4),
            new ScreenInfo("RepPurchasePrices", "📈 أسعار الشراء وتتبع التغيرات", 4),
            new ScreenInfo("RepCreditPurchases", "⏳ المشتريات الآجلة والمديونيات", 4),
            new ScreenInfo("RepStores", "📊 تقارير المخازن وحركة الأرصدة الشاملة", 4),
            new ScreenInfo("RepProductQtyDetail", "📊 تقرير كميات الأصناف التفصيلي", 4),
            new ScreenInfo("RepWastageLoss", "🚨 تقرير الهالك والتالف", 4),
            new ScreenInfo("RepInventoryValuation", "📦 تقييم المخزن التفصيلي بالتكلفة", 4),
            new ScreenInfo("RepSupplierItemActivity", "📊 حركة أصناف الموردين", 4),
            new ScreenInfo("RepExpiryReport", "⚠️ تقرير انتهاء الصلاحية", 4),
            new ScreenInfo("RepInventoryVariance", "📊 تقرير فروق الجرد والعجز", 4),
            new ScreenInfo("RepClients", "👥 تقارير العملاء الشاملة", 4),
            new ScreenInfo("RepClientBalances", "⚖️ أرصدة وبيانات العملاء", 4),
            new ScreenInfo("RepDebtAging", "⏳ أعمار الديون والديون الراكدة", 4),
            new ScreenInfo("RepClientProductSales", "📑 مبيعات عميل تفصيلي", 4),
            new ScreenInfo("RepSuppliers", "🤝 تقارير الموردين الشاملة", 4),
            new ScreenInfo("RepDrivers", "🚚 تقارير المناديب والتوزيع الشاملة", 4),
            new ScreenInfo("RepSalesByDriver", "🚚 مبيعات المناديب التفصيلية", 4),
            new ScreenInfo("RepHandovers", "📋 سجل تقفيل المناديب والحمولات", 4),
            new ScreenInfo("Financials", "📈 التقارير المالية وقائمة الدخل والأرباح", 4),
            new ScreenInfo("RepDailyClosing", "📑 تقرير التقفيل اليومي", 4),
            new ScreenInfo("RepIncomeStatement", "📊 قائمة الدخل والربحية", 4),
            new ScreenInfo("RepFinancialSummary", "📈 ملخص الحسابات والمالية", 4),
            new ScreenInfo("RepShiftComparison", "⚖️ مقارنة الورديات بالأيام التقويمية", 4),

            // ── Tab 5: Drivers, Maintenance & Administration (المناديب والصيانة والإدارة) ──
            new ScreenInfo("DriverHandover", "📦 تسليم وحمولة المندوب", 5),
            new ScreenInfo("DriverPortal", "📱 بوابة المندوب الميداني", 5),
            new ScreenInfo("DriverSales", "📱 مبيعات المندوب الميداني", 5),
            new ScreenInfo("ImportPreview", "📥 استيراد مبيعات المناديب من السحاب", 5),
            new ScreenInfo("DriversMonitor", "📡 شاشة مراقبة السائقين", 5),
            new ScreenInfo("DriverCustody", "💼 عهدة المناديب المالية", 5),
            new ScreenInfo("DriverLeaderboard", "🏆 أداء وتقييم المناديب", 5),
            new ScreenInfo("DashLoads", "🏠 لوحة التحكم: الحمولات المفتوحة", 5),
            new ScreenInfo("Maintenance", "🔧 تذاكر الصيانة وإدارة الأجهزة", 5),
            new ScreenInfo("Employees", "👨‍💼 إدارة الموظفين والرواتب", 5),
            new ScreenInfo("EmployeeTransactions", "💳 حسابات وحركات الموظفين", 5),
            new ScreenInfo("Settings", "⚙️ إعدادات النظام العامة", 5),
            new ScreenInfo("BotManager", "🤖 إدارة بوت الواتساب التلقائي", 5),
            new ScreenInfo("CloudSync", "☁️ التزامن السحابي والفرعي", 5),
            new ScreenInfo("LookupManager", "📚 إدارة الجداول المرجعية", 5),
            new ScreenInfo("EditInvoiceDate", "🔒 تغيير تاريخ فاتورة المبيعات/المشتريات", 5)
        };

        public FrmPermissions(int empID, string empName)
        {
            _empID = empID;
            _empName = empName;
            InitializeComponentCustom();
            LoadPermissions();
            UpdateLiveCounter();
        }

        private void InitializeComponentCustom()
        {
            this.Text = $"🔐 صلاحيات الموظف: {_empName}";
            this.Size = new Size(1180, 780);
            this.MinimumSize = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(24, 32, 47), Padding = new Padding(15, 10, 15, 10) };
            var lblTitleHeader = new Label { Text = $"🔐 صلاحيات الموظف: {_empName}", Font = new Font("Segoe UI", 12.5f, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(15, 8) };
            var lblSubHeader = new Label { Text = "حدد العمليات والصلاحيات والتقارير التفصيلية المسموح لهذا الموظف بإجرائها والاطلاع عليها في كافة شاشات وأقسام البرنامج.", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(180, 195, 215), AutoSize = true, Location = new Point(15, 36) };
            pnlTop.Controls.Add(lblTitleHeader); pnlTop.Controls.Add(lblSubHeader);

            var pnlControlBar = new Panel { Dock = DockStyle.Top, Height = 85, BackColor = Theme.BgCard, Padding = new Padding(10, 8, 10, 8) };
            txtSearch = new TextBox { Location = new Point(740, 7), Width = 210, Font = new Font("Segoe UI", 10f), BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtSearch.TextChanged += (s, e) => ApplySearchFilter();
            
            var btnRoleAdmin = CreatePresetButton("👑 مدير كامل", Color.FromArgb(192, 57, 43), (s, e) => ApplyRolePreset("Admin")); btnRoleAdmin.Location = new Point(830, 43);
            var btnRoleSales = CreatePresetButton("🛒 كاشير / مبيعات", Color.FromArgb(41, 128, 185), (s, e) => ApplyRolePreset("Sales")); btnRoleSales.Location = new Point(695, 43);
            var btnRolePurchases = CreatePresetButton("📥 مسؤول مشتريات", Color.FromArgb(142, 68, 173), (s, e) => ApplyRolePreset("Purchases")); btnRolePurchases.Location = new Point(555, 43);
            var btnRoleInventory = CreatePresetButton("📦 أمين مخزن", Color.FromArgb(39, 174, 96), (s, e) => ApplyRolePreset("Inventory")); btnRoleInventory.Location = new Point(435, 43);
            var btnRoleAccountant = CreatePresetButton("💰 محاسب مالي", Color.FromArgb(211, 84, 0), (s, e) => ApplyRolePreset("Accountant")); btnRoleAccountant.Location = new Point(310, 43);
            var btnClearAll = CreatePresetButton("🧹 تفريغ الكل", Color.FromArgb(127, 140, 141), (s, e) => ToggleAllPermissions(false)); btnClearAll.Location = new Point(195, 43);
            
            pnlControlBar.Controls.AddRange(new Control[] { new Label { Text = "🔍 تصفية وبحث فوري:", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.TextSub, AutoSize = true, Location = new Point(960, 10) }, txtSearch, new Label { Text = "🛡️ الأدوار والقوالب السريعة:", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.TextSub, AutoSize = true, Location = new Point(960, 48) }, btnRoleAdmin, btnRoleSales, btnRolePurchases, btnRoleInventory, btnRoleAccountant, btnClearAll });

            tcPerms = new TabControl { Dock = DockStyle.Fill, RightToLeftLayout = false, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Point(12, 6) };
            tcPerms.TabPages.Add(BuildTabPage("🛒 المبيعات والعملاء", out dgSales, 0));
            tcPerms.TabPages.Add(BuildTabPage("📥 المشتريات والموردين", out dgPurchases, 1));
            tcPerms.TabPages.Add(BuildTabPage("📦 المخازن والأصناف", out dgInventory, 2));
            tcPerms.TabPages.Add(BuildTabPage("💰 المالية والخزائن والورديات", out dgFinance, 3));
            tcPerms.TabPages.Add(BuildTabPage("📊 التقارير التفصيلية الشاملة", out dgReports, 4));
            tcPerms.TabPages.Add(BuildTabPage("🚚 المناديب والصيانة والإدارة", out dgAdmin, 5));

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(24, 32, 47), Padding = new Padding(15, 8, 15, 8) };
            lblCounter = new Label { Text = "📊 الصلاحيات المفعلة: 0 / 0", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(46, 204, 113), AutoSize = true };
            btnSave = Theme.MakeButton("💾 حفظ الصلاحيات [F5]", 15, 10, 215, 40, Theme.Accent);
            btnSave.Click += BtnSave_Click;
            var btnCancel = Theme.MakeButton("❌ إلغاء", 240, 10, 110, 40, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();
            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnCancel, lblCounter });

            this.Controls.Add(tcPerms); this.Controls.Add(pnlFooter); this.Controls.Add(pnlControlBar); this.Controls.Add(pnlTop);
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F5) { BtnSave_Click(s, e); e.Handled = true; } else if (e.KeyCode == Keys.Escape) this.Close(); };
            Theme.ApplyFormRTL(this);
        }

        private TabPage BuildTabPage(string title, out DataGridView grid, int tabIndex)
        {
            var tp = new TabPage(title) { BackColor = Theme.BgCard, Padding = new Padding(6) };
            var pnlTabHeader = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.BgMain, Padding = new Padding(5) };
            var btnSelectTab = new Button { Text = "✔️ تحديد الكل", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Size = new Size(100, 26), Location = new Point(5, 4), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(52, 73, 94), ForeColor = Color.White };
            btnSelectTab.Click += (s, e) => ToggleTabPermissions(tabIndex, true);
            var btnClearTab = new Button { Text = "❌ إلغاء", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Size = new Size(100, 26), Location = new Point(110, 4), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White };
            btnClearTab.Click += (s, e) => ToggleTabPermissions(tabIndex, false);
            pnlTabHeader.Controls.AddRange(new Control[] { btnSelectTab, btnClearTab });
            grid = CreatePermissionsGrid();
            tp.Controls.Add(grid); tp.Controls.Add(pnlTabHeader);
            return tp;
        }

        private DataGridView CreatePermissionsGrid()
        {
            var dg = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Theme.BgCard, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, RightToLeft = RightToLeft.Yes, GridColor = Theme.BorderColor, EnableHeadersVisualStyles = false };
            dg.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleCenter };
            dg.ColumnHeadersHeight = 42;
            dg.Columns.AddRange(new DataGridViewTextBoxColumn { Name = "Screen", Visible = false }, new DataGridViewTextBoxColumn { Name = "ScreenName", HeaderText = "اسم الشاشة / الوظيفة", ReadOnly = true, Width = 260 }, new DataGridViewCheckBoxColumn { Name = "CanAccess", HeaderText = "👁️ دخول", Width = 85 }, new DataGridViewCheckBoxColumn { Name = "CanAdd", HeaderText = "➕ إضافة", Width = 85 }, new DataGridViewCheckBoxColumn { Name = "CanEdit", HeaderText = "✏️ تعديل", Width = 85 }, new DataGridViewCheckBoxColumn { Name = "CanDelete", HeaderText = "🗑️ حذف", Width = 80 }, new DataGridViewCheckBoxColumn { Name = "CanEditPrice", HeaderText = "🏷️ السعر", Width = 85 }, new DataGridViewCheckBoxColumn { Name = "CanEditSalesInvoice", HeaderText = "📝 تعديل فاتورة", Width = 120 }, new DataGridViewCheckBoxColumn { Name = "CanDeleteSalesInvoice", HeaderText = "❌ حذف فاتورة", Width = 120 }, new DataGridViewCheckBoxColumn { Name = "CanCopySalesInvoice", HeaderText = "📋 نسخ/طباعة", Width = 120 }, new DataGridViewCheckBoxColumn { Name = "CanViewCost", HeaderText = "💲 التكلفة", Width = 90 }, new DataGridViewCheckBoxColumn { Name = "CanOrderColumns", HeaderText = "↕️ ترتيب", Width = 85 }, new DataGridViewCheckBoxColumn { Name = "CanViewDetails", HeaderText = "📄 التقفيل", Width = 90 }, new DataGridViewCheckBoxColumn { Name = "CanViewBalance", HeaderText = "💰 الرصيد", Width = 90 }, new DataGridViewCheckBoxColumn { Name = "CanChangeSafe", HeaderText = "🔄 تغيير خزنة", Width = 115 }, new DataGridViewCheckBoxColumn { Name = "CanViewSalesTotals", HeaderText = "📊 إجماليات السجل", Width = 135 }, new DataGridViewCheckBoxColumn { Name = "CanViewQuickItems", HeaderText = "⚡ أصناف سريعة", Width = 115 });
            dg.CellValueChanged += (s, e) => UpdateLiveCounter();
            dg.CurrentCellDirtyStateChanged += (s, e) => { if (dg.IsCurrentCellDirty) dg.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            return dg;
        }

        private Button CreatePresetButton(string text, Color bg, EventHandler onClick) { var btn = new Button { Text = text, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Size = new Size(125, 30), FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand }; btn.FlatAppearance.BorderSize = 0; btn.Click += onClick; return btn; }

        private void ToggleAllPermissions(bool check) { foreach (var grid in new[] { dgSales, dgPurchases, dgInventory, dgFinance, dgReports, dgAdmin }) { if (grid == null) continue; foreach (DataGridViewRow row in grid.Rows) { for (int i = 2; i < grid.Columns.Count; i++) if (row.Cells[i] is DataGridViewCheckBoxCell cell && !row.Cells[i].ReadOnly) cell.Value = check; } } UpdateLiveCounter(); }
        
        private void ToggleTabPermissions(int tabIndex, bool check) { DataGridView grid = tabIndex == 0 ? dgSales : tabIndex == 1 ? dgPurchases : tabIndex == 2 ? dgInventory : tabIndex == 3 ? dgFinance : tabIndex == 4 ? dgReports : dgAdmin; if (grid != null) { foreach (DataGridViewRow row in grid.Rows) { foreach (DataGridViewCell cell in row.Cells) if (cell.ColumnIndex >= 2 && cell is DataGridViewCheckBoxCell && !cell.ReadOnly) cell.Value = check; } UpdateLiveCounter(); } }

        private void ApplyRolePreset(string role)
        {
            ToggleAllPermissions(false);
            var salesKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sales", "POS", "PriceQuote", "ProductSearch", "Returns", "Installments", "Reservations", "ClearanceOffers", "SalesList", "SalesAudit", "AccountantPortal", "Clients", "ClientStatement", "InactiveClients", "Vehicles", "DashSales", "RepDailySales", "RepSalesByPeriod", "RepDetailedSales", "RepDetailedSaleItems", "RepSalesByProduct", "RepSalesByCategory", "RepSalesByClient", "RepSalesByUser", "RepSalesByPayment", "RepSalesDiscounts", "RepDetailedReturns", "RepStagnantProducts" };
            var purchaseKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Purchases", "PurchaseReturn", "PurchasesList", "Suppliers", "SupplierStatement", "SupplierPayment", "SupplierAdjustment", "RepPurchases", "RepDailyPurchases", "RepPurchasesByPeriod", "RepDetailedPurchases", "RepDetailedPurchaseItems", "RepPurchasesBySupplier", "RepPurchasesByProduct", "RepPurchasesByCategory", "RepPurchaseReturns", "RepSupplierPayments", "RepPurchasePrices", "RepCreditPurchases" };
            var inventoryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Products", "ProductCard", "Categories", "Units", "ImportProducts", "Warehouses", "Inventory", "MinStockEdit", "ShortageNotebook", "InventoryVarianceReport", "Wastage", "WarehouseTransfer", "WarehouseTransfersList", "PriceChanges", "PricePoster", "ProductMovement", "BulkPrintBarcodes", "ClothingMatrix", "ModelLookup", "MultiBarcodes", "DashBelowMin", "RepStores", "RepProductQtyDetail", "RepWastageLoss", "RepInventoryValuation", "RepSupplierItemActivity", "RepExpiryReport", "RepInventoryVariance" };
            var accountantKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CashBox", "SafeAccounts", "ActualBalances", "DailyAccounts", "ReceiptVoucher", "FinancialPosition", "Reports", "Financials", "DailyClosing", "ShiftClose", "ShiftsHistory", "EmployeeTransactions", "DriverCustody", "SupplierStatement", "ClientStatement", "DashTreasury", "SalesAudit", "RepDailySales", "RepSalesByPeriod", "RepDetailedSales", "RepDetailedSaleItems", "RepSalesByProduct", "RepSalesByCategory", "RepSalesByClient", "RepSalesByUser", "RepSalesByPayment", "RepSalesDiscounts", "RepDetailedReturns", "RepSalesProfit", "RepStagnantProducts", "RepPurchases", "RepDailyPurchases", "RepPurchasesByPeriod", "RepDetailedPurchases", "RepDetailedPurchaseItems", "RepPurchasesBySupplier", "RepPurchasesByProduct", "RepPurchasesByCategory", "RepPurchaseReturns", "RepSupplierPayments", "RepPurchasePrices", "RepCreditPurchases", "RepStores", "RepProductQtyDetail", "RepWastageLoss", "RepInventoryValuation", "RepSupplierItemActivity", "RepExpiryReport", "RepInventoryVariance", "RepClients", "RepClientBalances", "RepDebtAging", "RepClientProductSales", "RepSuppliers", "RepDrivers", "RepSalesByDriver", "RepHandovers", "RepDailyClosing", "RepIncomeStatement", "RepFinancialSummary", "RepShiftComparison" };
            foreach (var grid in new[] { dgSales, dgPurchases, dgInventory, dgFinance, dgReports, dgAdmin })
            {
                if (grid == null) continue;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    string screen = row.Cells["Screen"].Value?.ToString();
                    bool isAdminRole = string.Equals(role?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase);
                    bool enable = isAdminRole || (role == "Sales" && salesKeys.Contains(screen)) || (role == "Purchases" && purchaseKeys.Contains(screen)) || (role == "Inventory" && inventoryKeys.Contains(screen)) || (role == "Accountant" && accountantKeys.Contains(screen));
                    if (enable) { if (row.Cells["CanAccess"] is DataGridViewCheckBoxCell cAcc) cAcc.Value = true; if (isAdminRole) for (int i = 3; i < grid.Columns.Count; i++) if (row.Cells[i] is DataGridViewCheckBoxCell cOpt && !row.Cells[i].ReadOnly) cOpt.Value = true; }
                }
            }
            UpdateLiveCounter();
        }

        private void ApplySearchFilter() 
        { 
            string q = txtSearch.Text.Trim(); 
            foreach (var grid in new[] { dgSales, dgPurchases, dgInventory, dgFinance, dgReports, dgAdmin }) 
                if (grid != null) 
                    foreach (DataGridViewRow r in grid.Rows) 
                        r.Visible = string.IsNullOrEmpty(q) || r.Cells["ScreenName"].Value.ToString().Contains(q) || r.Cells["Screen"].Value.ToString().Contains(q); 
        }

        private void UpdateLiveCounter()
        {
            int total = 0, access = 0, special = 0;
            foreach (var grid in new[] { dgSales, dgPurchases, dgInventory, dgFinance, dgReports, dgAdmin })
            {
                if (grid == null) continue;
                foreach (DataGridViewRow r in grid.Rows)
                {
                    total++;
                    if (ToBool(r.Cells["CanAccess"].Value)) access++;
                    for (int c = 3; c < grid.Columns.Count; c++) if (r.Cells[c] is DataGridViewCheckBoxCell && ToBool(r.Cells[c].Value)) special++;
                }
            }
            lblCounter.Text = $"📊 الشاشات المسموحة: {access} / {total} | 🛡️ صلاحيات خاصة: {special}";
        }

        private void LoadPermissions()
        {
            try
            {
                var dt = EmployeeDAL.GetPermissions(_empID);
                foreach (var grid in new[] { dgSales, dgPurchases, dgInventory, dgFinance, dgReports, dgAdmin }) grid.Rows.Clear();

                foreach (var screen in ScreensList)
                {
                    try
                    {
                        bool access = false, canAdd = true, canEdit = true, canDelete = true;
                        bool editPrice = false, editInvoice = false, deleteInvoice = false, copyInvoice = false, viewCost = false, orderColumns = false;
                        bool viewDetails = true, viewBalance = true, changeSafe = true, viewSalesTotals = true, viewQuickItems = true;

                        foreach (DataRow r in dt.Rows)
                        {
                            if (string.Equals(r["ScreenName"].ToString(), screen.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                access = Convert.ToBoolean(r["CanAccess"]);
                                canAdd = r.Table.Columns.Contains("CanAdd") && r["CanAdd"] != DBNull.Value ? Convert.ToBoolean(r["CanAdd"]) : true;
                                canEdit = r.Table.Columns.Contains("CanEdit") && r["CanEdit"] != DBNull.Value ? Convert.ToBoolean(r["CanEdit"]) : true;
                                canDelete = r.Table.Columns.Contains("CanDelete") && r["CanDelete"] != DBNull.Value ? Convert.ToBoolean(r["CanDelete"]) : true;
                                editPrice = Convert.ToBoolean(r["CanEditPrice"]);
                                editInvoice = r.Table.Columns.Contains("CanEditSalesInvoice") && r["CanEditSalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanEditSalesInvoice"]);
                                deleteInvoice = r.Table.Columns.Contains("CanDeleteSalesInvoice") && r["CanDeleteSalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanDeleteSalesInvoice"]);
                                copyInvoice = r.Table.Columns.Contains("CanCopySalesInvoice") && r["CanCopySalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanCopySalesInvoice"]);
                                viewCost = r.Table.Columns.Contains("CanViewCost") && r["CanViewCost"] != DBNull.Value && Convert.ToBoolean(r["CanViewCost"]);
                                orderColumns = r.Table.Columns.Contains("CanOrderColumns") && r["CanOrderColumns"] != DBNull.Value && Convert.ToBoolean(r["CanOrderColumns"]);
                                viewDetails = r.Table.Columns.Contains("CanViewDetails") && r["CanViewDetails"] != DBNull.Value ? Convert.ToBoolean(r["CanViewDetails"]) : true;
                                viewBalance = r.Table.Columns.Contains("CanViewBalance") && r["CanViewBalance"] != DBNull.Value ? Convert.ToBoolean(r["CanViewBalance"]) : true;
                                changeSafe = r.Table.Columns.Contains("CanChangeSafe") && r["CanChangeSafe"] != DBNull.Value ? Convert.ToBoolean(r["CanChangeSafe"]) : true;
                                viewSalesTotals = r.Table.Columns.Contains("CanViewSalesTotals") && r["CanViewSalesTotals"] != DBNull.Value ? Convert.ToBoolean(r["CanViewSalesTotals"]) : true;
                                viewQuickItems = r.Table.Columns.Contains("CanViewQuickItems") && r["CanViewQuickItems"] != DBNull.Value ? Convert.ToBoolean(r["CanViewQuickItems"]) : true;
                                break;
                            }
                        }

                        DataGridView targetGrid = null;
                        if (screen.TabIndex == 0) targetGrid = dgSales;
                        else if (screen.TabIndex == 1) targetGrid = dgPurchases;
                        else if (screen.TabIndex == 2) targetGrid = dgInventory;
                        else if (screen.TabIndex == 3) targetGrid = dgFinance;
                        else if (screen.TabIndex == 4) targetGrid = dgReports;
                        else if (screen.TabIndex == 5) targetGrid = dgAdmin;

                        if (targetGrid != null)
                        {
                            int ri = targetGrid.Rows.Add(
                                screen.Key, 
                                screen.Name, 
                                access, 
                                canAdd, 
                                canEdit, 
                                canDelete, 
                                editPrice, 
                                editInvoice, 
                                deleteInvoice, 
                                copyInvoice, 
                                viewCost, 
                                orderColumns, 
                                viewDetails, 
                                viewBalance, 
                                changeSafe, 
                                viewSalesTotals, 
                                viewQuickItems
                            );
                            
                            var key = screen.Key;

                            // Disable Non-applicable columns per screen
                            bool isEntityScreen = string.Equals(key, "Products", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "ProductCard", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Clients", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Suppliers", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Warehouses", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Categories", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Units", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Vehicles", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Employees", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Installments", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Maintenance", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "SafeAccounts", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "PriceQuote", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Reservations", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "ClearanceOffers", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "LookupManager", StringComparison.OrdinalIgnoreCase);

                            if (!isEntityScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 3); // CanAdd
                                DisableGridCell(targetGrid.Rows[ri], 4); // CanEdit
                                DisableGridCell(targetGrid.Rows[ri], 5); // CanDelete
                            }

                            bool isPriceEditableScreen = string.Equals(key, "Sales", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(key, "POS", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(key, "Purchases", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(key, "ProductSearch", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(key, "Products", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(key, "PriceQuote", StringComparison.OrdinalIgnoreCase);
                            if (!isPriceEditableScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 6); // CanEditPrice
                            }

                            bool isInvoiceOpsScreen = string.Equals(key, "Sales", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "POS", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "Purchases", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "SalesList", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "PurchasesList", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "Returns", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "PurchaseReturn", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "PriceQuote", StringComparison.OrdinalIgnoreCase);
                            if (!isInvoiceOpsScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 7); // EditInvoice
                                DisableGridCell(targetGrid.Rows[ri], 8); // DeleteInvoice
                                DisableGridCell(targetGrid.Rows[ri], 9); // CopyInvoice
                            }

                            bool isCostViewableScreen = string.Equals(key, "Sales", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "POS", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "PriceQuote", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "Products", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "ProductCard", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "SalesList", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "PurchasesList", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "Reports", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "Financials", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "Inventory", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "ProductMovement", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "RepSalesProfit", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "RepIncomeStatement", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "RepInventoryValuation", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "RepPurchasesByProduct", StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(key, "RepFinancials", StringComparison.OrdinalIgnoreCase);
                            if (!isCostViewableScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 10); // ViewCost
                            }

                            bool isOrderColumnsApplicable = string.Equals(key, "Sales", StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(key, "Purchases", StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(key, "POS", StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(key, "Products", StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(key, "SalesList", StringComparison.OrdinalIgnoreCase);
                            if (!isOrderColumnsApplicable)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 11); // OrderColumns
                            }

                            bool isDetailsScreen = string.Equals(key, "DailyClosing", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "ShiftClose", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "ShiftsHistory", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "DailyAccounts", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "ActualBalances", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Reports", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Financials", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "POS", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "Sales", StringComparison.OrdinalIgnoreCase);
                            if (!isDetailsScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 12); // ViewDetails
                            }

                            bool isBalanceScreen = string.Equals(key, "CashBox", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "SafeAccounts", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "ActualBalances", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "DailyAccounts", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "DashTreasury", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "DailyClosing", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "ShiftClose", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(key, "ShiftsHistory", StringComparison.OrdinalIgnoreCase);
                            if (!isBalanceScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 13); // ViewBalance
                            }

                            bool isSafeChangeScreen = string.Equals(key, "Sales", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "POS", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "Purchases", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "CashBox", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "SafeAccounts", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "DailyAccounts", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "ReceiptVoucher", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "SupplierPayment", StringComparison.OrdinalIgnoreCase);
                            if (!isSafeChangeScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 14); // ChangeSafe
                            }

                            bool isSalesListScreen = string.Equals(key, "SalesList", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "PurchasesList", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "DailyClosing", StringComparison.OrdinalIgnoreCase);
                            if (!isSalesListScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 15); // CanViewSalesTotals
                            }

                            bool isQuickItemsScreen = string.Equals(key, "Sales", StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(key, "POS", StringComparison.OrdinalIgnoreCase);
                            if (!isQuickItemsScreen)
                            {
                                DisableGridCell(targetGrid.Rows[ri], 16); // CanViewQuickItems
                            }
                        }
                    }
                    catch (Exception exScreen) { AppLogger.Error("LoadPermissions screen=" + screen.Key, exScreen); }
                }

                UpdateLiveCounter();
            }
            catch (Exception ex) { AppLogger.Error("LoadPermissions", ex); }
        }

        private static void DisableGridCell(DataGridViewRow row, int colIdx)
        {
            row.Cells[colIdx] = new DataGridViewTextBoxCell { Value = "—" };
            row.Cells[colIdx].ReadOnly = true;
            row.Cells[colIdx].Style.BackColor = Color.FromArgb(245, 246, 248);
            row.Cells[colIdx].Style.ForeColor = Color.FromArgb(170, 175, 185);
            row.Cells[colIdx].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
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
            var grids = new[] { dgSales, dgPurchases, dgInventory, dgFinance, dgReports, dgAdmin };
            foreach (var grid in grids)
            {
                if (grid == null) continue;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    string screen = row.Cells["Screen"].Value?.ToString();
                    if (string.IsNullOrEmpty(screen)) continue;

                    bool access       = ToBool(row.Cells["CanAccess"].Value);
                    bool add          = ToBool(row.Cells["CanAdd"].Value);
                    bool edit         = ToBool(row.Cells["CanEdit"].Value);
                    bool delete       = ToBool(row.Cells["CanDelete"].Value);
                    bool editP        = ToBool(row.Cells["CanEditPrice"].Value);
                    bool editI        = ToBool(row.Cells["CanEditSalesInvoice"].Value);
                    bool deleteI      = ToBool(row.Cells["CanDeleteSalesInvoice"].Value);
                    bool copyI        = ToBool(row.Cells["CanCopySalesInvoice"].Value);
                    bool viewC        = ToBool(row.Cells["CanViewCost"].Value);
                    bool orderC       = ToBool(row.Cells["CanOrderColumns"].Value);
                    bool viewDetails  = ToBool(row.Cells["CanViewDetails"].Value);
                    bool viewBalance  = ToBool(row.Cells["CanViewBalance"].Value);
                    bool changeSafe   = ToBool(row.Cells["CanChangeSafe"].Value);
                    bool viewSalesTotals = ToBool(row.Cells["CanViewSalesTotals"].Value);
                    bool viewQuickItems = ToBool(row.Cells["CanViewQuickItems"].Value);

                    EmployeeDAL.SavePermissions(_empID, screen, access, add, edit, delete, editP, editI, deleteI, copyI, viewC, orderC, viewDetails, viewBalance, changeSafe, viewSalesTotals, viewQuickItems);
                }
            }
            MessageBox.Show($"✅ تم حفظ صلاحيات الموظف ({_empName}) بنجاح!", "حفظ الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private static bool rowCheckColContains(string colName, DataColumnCollection cols)
        {
            return cols.Contains(colName);
        }
    }

}

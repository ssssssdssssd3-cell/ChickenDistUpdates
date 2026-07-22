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
        private ComboBox cboRole;
        private CheckBox chkDriver, chkActive;
        private Button btnNew, btnSave, btnDelete, btnPerms;
        private int _selectedID = 0;

        private ComboBox cboDefaultSafe;
        private CheckedListBox clbAllowedSafes;
        private CheckBox chkCanSellCash, chkCanSellCredit, chkCanSellDriverLoad, chkCanSellInstallment, chkCanEditShippingCharge, chkCanSelectDriver;

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
            // كلمة المرور ظاهرة للأدمن (لاسترجاعها عند النسيان)
            // txtPassword.PasswordChar = '●';  // تم إلغاء الإخفاء بناءً على طلب المدير
            AddField(pnlDetails, "الهاتف:", ref y, out txtPhone);

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
            chkCanSellDriverLoad = new CheckBox { Text = "تحميل مندوب", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            chkCanSellInstallment = new CheckBox { Text = "تقسيط شرعي", Location = new Point(50, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            y += 28;
            chkCanEditShippingCharge = new CheckBox { Text = "إضافة/تعديل خدمة الشحن", Location = new Point(180, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            chkCanSelectDriver = new CheckBox { Text = "اختيار/ظهور المندوب", Location = new Point(20, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true };
            pnlDetails.Controls.AddRange(new Control[] { chkCanSellCash, chkCanSellCredit, chkCanSellDriverLoad, chkCanSellInstallment, chkCanEditShippingCharge, chkCanSelectDriver });
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
            chkCanSellDriverLoad.Checked = dr["CanSellDriverLoad"] == DBNull.Value || Convert.ToBoolean(dr["CanSellDriverLoad"]);
            chkCanSellInstallment.Checked = dr["CanSellInstallment"] == DBNull.Value || Convert.ToBoolean(dr["CanSellInstallment"]);
            chkCanEditShippingCharge.Checked = dr.Table.Columns.Contains("CanEditShippingCharge") && (dr["CanEditShippingCharge"] == DBNull.Value || Convert.ToBoolean(dr["CanEditShippingCharge"]));
            chkCanSelectDriver.Checked = !dr.Table.Columns.Contains("CanSelectDriver") || dr["CanSelectDriver"] == DBNull.Value || Convert.ToBoolean(dr["CanSelectDriver"]);
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtName.Clear(); txtUsername.Clear(); txtPassword.Clear(); txtPhone.Clear();
            cboRole.SelectedIndex = 4;
            chkDriver.Checked = false; chkActive.Checked = true;

            if (cboDefaultSafe.Items.Count > 0) cboDefaultSafe.SelectedIndex = 0;
            for (int i = 0; i < clbAllowedSafes.Items.Count; i++)
            {
                clbAllowedSafes.SetItemChecked(i, false);
            }
            chkCanSellCash.Checked = true;
            chkCanSellCredit.Checked = true;
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

            try
            {
                int id = EmployeeDAL.Save(_selectedID, txtName.Text, txtUsername.Text,
                    txtPassword.Text, cboRole.Text, txtPhone.Text, chkDriver.Checked, chkActive.Checked,
                    defaultSafeID, allowedSafeIDs, chkCanSellCash.Checked, chkCanSellCredit.Checked,
                    chkCanSellDriverLoad.Checked, chkCanSellInstallment.Checked, chkCanEditShippingCharge.Checked,
                    chkCanSelectDriver.Checked);
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

    /// <summary>شاشة الصلاحيات الاحترافية المحدثة — قوالب جاهزة وبحث فوري وعداد مباشر</summary>
    public class FrmPermissions : Form
    {
        private readonly int _empID;
        private readonly string _empName;
        private TabControl tcPerms;
        private DataGridView dgSales;
        private DataGridView dgPurchases;
        private DataGridView dgInventory;
        private DataGridView dgAdmin;
        private TextBox txtSearch;
        private Label lblCounter;
        private Button btnSave;

        private struct ScreenInfo
        {
            public string Key;
            public string Name;
            public int TabIndex; // 0: Sales & Clients, 1: Purchases & Suppliers, 2: Inventory & Products, 3: Finance & Administration
            public ScreenInfo(string key, string name, int tabIndex)
            {
                Key = key;
                Name = name;
                TabIndex = tabIndex;
            }
        }

        private static readonly ScreenInfo[] ScreensList = {
            // Tab 0: Sales & Clients
            new ScreenInfo("Sales", "🛒 شاشة المبيعات الرئيسية", 0),
            new ScreenInfo("POS", "⚡ نقطة البيع السريعة POS", 0),
            new ScreenInfo("Returns", "↩️ مرتجع المبيعات", 0),
            new ScreenInfo("Installments", "📜 عقود التقسيط الشرعي", 0),
            new ScreenInfo("SalesList", "📋 سجل الفواتير والمبيعات", 0),
            new ScreenInfo("SalesAudit", "🔍 سجل تعديلات وحذف الفواتير", 0),
            new ScreenInfo("AccountantPortal", "🌐 بوابة المحاسبة الميدانية", 0),
            new ScreenInfo("Clients", "👥 إدارة وتعديل العملاء", 0),
            new ScreenInfo("InactiveClients", "💤 تنشيط العملاء الراكدين", 0),
            new ScreenInfo("Vehicles", "🚚 إدارة المركبات والسيارات", 0),
            new ScreenInfo("Maintenance", "🔧 تذاكر الصيانة وإدارة الأجهزة", 0),

            // Tab 1: Purchases & Suppliers
            new ScreenInfo("Purchases", "📥 شاشة المشتريات الرئيسية", 1),
            new ScreenInfo("PurchaseReturn", "↩️ مرتجع المشتريات", 1),
            new ScreenInfo("PurchasesList", "📋 سجل الفواتير والمشتريات", 1),
            new ScreenInfo("Suppliers", "🏢 إدارة وتعديل الموردين", 1),
            new ScreenInfo("SupplierStatement", "📄 كشف حساب المورد", 1),
            new ScreenInfo("SupplierPayment", "💵 صرف نقدي لمورد", 1),
            new ScreenInfo("SupplierAdjustment", "⚖️ تسوية أرصدة الموردين", 1),

            // Tab 2: Inventory & Products
            new ScreenInfo("Products", "🏷️ إدارة وتعديل الأصناف والأسعار", 2),
            new ScreenInfo("Categories", "🗂️ التصنيفات والأقسام", 2),
            new ScreenInfo("ImportProducts", "📊 استيراد الأصناف من إكسيل", 2),
            new ScreenInfo("Warehouses", "🏭 إدارة المخازن والمستودعات", 2),
            new ScreenInfo("Inventory", "📦 جرد وتعديل رصيد المخزن", 2),
            new ScreenInfo("Wastage", "⚠️ تسجيل الهوالك والتالف", 2),
            new ScreenInfo("WarehouseTransfer", "🔄 تحويل مخزني صادر", 2),
            new ScreenInfo("WarehouseTransfersList", "📋 سجل التحويلات المخزنية", 2),
            new ScreenInfo("PriceChanges", "📉 سجل تغير وحركات الأسعار", 2),
            new ScreenInfo("BulkPrintBarcodes", "🏷️ طباعة الباركود (مجمع)", 2),

            // Tab 3: Finance, Drivers & Administration
            new ScreenInfo("CashBox", "💰 حركات الخزينة والصندوق", 3),
            new ScreenInfo("Reports", "📊 التقارير والإحصائيات المالية", 3),
            new ScreenInfo("DailyClosing", "🔒 تقفيل يومية المبيعات", 3),
            new ScreenInfo("ShiftClose", "🔄 شاشة إدارة وإغلاق الوردية", 3),
            new ScreenInfo("Employees", "👨‍💼 إدارة الموظفين والرواتب", 3),
            new ScreenInfo("EmployeeTransactions", "💳 حسابات وحركات الموظفين", 3),
            new ScreenInfo("DriverHandover", "📦 تسليم وحمولة المندوب", 3),
            new ScreenInfo("DriverPortal", "📱 بوابة المندوب الميداني", 3),
            new ScreenInfo("ImportPreview", "📥 استيراد مبيعات المناديب", 3),
            new ScreenInfo("DriversMonitor", "📡 شاشة مراقبة السائقين", 3),
            new ScreenInfo("DashTreasury", "🏠 لوحة التحكم: رصيد الخزنة الحالي", 3),
            new ScreenInfo("DashSales", "🏠 لوحة التحكم: مبيعات اليوم", 3),
            new ScreenInfo("DashLoads", "🏠 لوحة التحكم: الحمولات المفتوحة", 3),
            new ScreenInfo("DashBelowMin", "🏠 لوحة التحكم: الأصناف تحت حد الطلب", 3),
            new ScreenInfo("DriverCustody", "💼 عهدة المناديب المالية", 3),
            new ScreenInfo("DriverLeaderboard", "🏆 أداء وتقييم المناديب", 3),
            new ScreenInfo("Settings", "⚙️ إعدادات النظام العامة", 3),
            new ScreenInfo("BotManager", "🤖 إدارة بوت الواتساب التلقائي", 3),
            new ScreenInfo("EditInvoiceDate", "🔒 تغيير تاريخ فاتورة المبيعات/المشتريات", 3)
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
            this.Size = new Size(1020, 720);
            this.MinimumSize = new Size(920, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. Header Panel
            var pnlTop = Theme.MakeTitleBar($"🔐 صلاحيات الموظف: {_empName}", "حدد الصلاحيات والوظائف المسموح بهذا المستخدم الوصول إليها وإدارتها.");
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 60;
            this.Controls.Add(pnlTop);

            // 2. Presets & Search Control Bar Panel
            var pnlControlBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8)
            };

            // Search Box
            var lblSearch = new Label
            {
                Text = "🔍 تصفية وبحث فوري:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                AutoSize = true,
                Location = new Point(840, 10)
            };
            txtSearch = new TextBox
            {
                Location = new Point(620, 7),
                Width = 210,
                Font = new Font("Segoe UI", 10f),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            txtSearch.TextChanged += (s, e) => ApplySearchFilter();

            // Preset Roles Title
            var lblPresets = new Label
            {
                Text = "🛡️ الأدوار والقوالب السريعة:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                AutoSize = true,
                Location = new Point(840, 48)
            };

            // Preset Buttons
            var btnRoleAdmin = CreatePresetButton("👑 مدير كامل", Color.FromArgb(192, 57, 43), (s, e) => ApplyRolePreset("Admin"));
            btnRoleAdmin.Location = new Point(710, 43);

            var btnRoleSales = CreatePresetButton("🛒 كاشير / مبيعات", Color.FromArgb(41, 128, 185), (s, e) => ApplyRolePreset("Sales"));
            btnRoleSales.Location = new Point(575, 43);

            var btnRolePurchases = CreatePresetButton("📥 مسؤول مشتريات", Color.FromArgb(142, 68, 173), (s, e) => ApplyRolePreset("Purchases"));
            btnRolePurchases.Location = new Point(435, 43);

            var btnRoleInventory = CreatePresetButton("📦 أمين مخزن", Color.FromArgb(39, 174, 96), (s, e) => ApplyRolePreset("Inventory"));
            btnRoleInventory.Location = new Point(315, 43);

            var btnRoleAccountant = CreatePresetButton("💰 محاسب مالي", Color.FromArgb(211, 84, 0), (s, e) => ApplyRolePreset("Accountant"));
            btnRoleAccountant.Location = new Point(190, 43);

            var btnClearAll = CreatePresetButton("🧹 تفريغ الكل", Color.FromArgb(127, 140, 141), (s, e) => ToggleAllPermissions(false));
            btnClearAll.Location = new Point(75, 43);

            pnlControlBar.Controls.Add(lblSearch);
            pnlControlBar.Controls.Add(txtSearch);
            pnlControlBar.Controls.Add(lblPresets);
            pnlControlBar.Controls.Add(btnRoleAdmin);
            pnlControlBar.Controls.Add(btnRoleSales);
            pnlControlBar.Controls.Add(btnRolePurchases);
            pnlControlBar.Controls.Add(btnRoleInventory);
            pnlControlBar.Controls.Add(btnRoleAccountant);
            pnlControlBar.Controls.Add(btnClearAll);

            this.Controls.Add(pnlControlBar);

            // 3. Tab Control
            tcPerms = new TabControl
            {
                Dock = DockStyle.Fill,
                RightToLeftLayout = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Point(12, 6)
            };

            var tpSales = BuildTabPage("🛒 المبيعات والعملاء", out dgSales, 0);
            var tpPurchases = BuildTabPage("📥 المشتريات والموردين", out dgPurchases, 1);
            var tpInventory = BuildTabPage("📦 المخازن والأصناف", out dgInventory, 2);
            var tpAdmin = BuildTabPage("⚙️ المالية والمناديب والإدارة", out dgAdmin, 3);

            tcPerms.TabPages.Add(tpSales);
            tcPerms.TabPages.Add(tpPurchases);
            tcPerms.TabPages.Add(tpInventory);
            tcPerms.TabPages.Add(tpAdmin);

            this.Controls.Add(tcPerms);
            tcPerms.BringToFront();

            // 4. Bottom Footer Bar Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            lblCounter = new Label
            {
                Text = "📊 الصلاحيات المفعلة: 0 / 0",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 204, 113),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            btnSave = Theme.MakeButton("💾 حفظ الصلاحيات [F5]", 0, 0, 180, 40, Theme.Accent);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSave.Dock = DockStyle.Left;
            btnSave.Click += BtnSave_Click;

            var btnCancel = Theme.MakeButton("إلغاء", 0, 0, 100, 40, Color.FromArgb(100, 110, 120));
            btnCancel.Dock = DockStyle.Left;
            btnCancel.Click += (s, e) => this.Close();

            var pnlLeftButtons = new Panel { Dock = DockStyle.Left, Width = 300, BackColor = Color.Transparent };
            pnlLeftButtons.Controls.Add(btnSave);
            pnlLeftButtons.Controls.Add(btnCancel);
            btnSave.Location = new Point(0, 0);
            btnCancel.Location = new Point(190, 0);

            pnlFooter.Controls.Add(lblCounter);
            pnlFooter.Controls.Add(pnlLeftButtons);

            this.Controls.Add(pnlFooter);
            pnlFooter.SendToBack();

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F5) { BtnSave_Click(s, e); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) { this.Close(); }
            };

            Theme.ApplyFormRTL(this);
        }

        private TabPage BuildTabPage(string title, out DataGridView grid, int tabIndex)
        {
            var tp = new TabPage(title) { BackColor = Theme.BgCard, Padding = new Padding(6) };

            // Quick per-tab header panel
            var pnlTabHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Theme.BgMain,
                Padding = new Padding(5)
            };

            var btnSelectTab = new Button
            {
                Text = "✔️ تحديد كل الشاشات بهذا التبويب",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Size = new Size(190, 26),
                Location = new Point(5, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White
            };
            btnSelectTab.FlatAppearance.BorderSize = 0;
            btnSelectTab.Click += (s, e) => ToggleTabPermissions(tabIndex, true);

            var btnClearTab = new Button
            {
                Text = "❌ إلغاء هذا التبويب",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Size = new Size(130, 26),
                Location = new Point(200, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(127, 140, 141),
                ForeColor = Color.White
            };
            btnClearTab.FlatAppearance.BorderSize = 0;
            btnClearTab.Click += (s, e) => ToggleTabPermissions(tabIndex, false);

            var lblHint = new Label
            {
                Text = "💡 اضغط على عنوان العمود لتحديد/إلغاء العمود بالكامل",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Theme.TextSub,
                AutoSize = true,
                Location = new Point(500, 8)
            };

            pnlTabHeader.Controls.Add(btnSelectTab);
            pnlTabHeader.Controls.Add(btnClearTab);
            pnlTabHeader.Controls.Add(lblHint);

            grid = CreatePermissionsGrid();

            tp.Controls.Add(grid);
            tp.Controls.Add(pnlTabHeader);

            grid.BringToFront();
            return tp;
        }

        private Button CreatePresetButton(string text, Color bg, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(125, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private DataGridView CreatePermissionsGrid()
        {
            var dg = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, SelectionBackColor = Color.FromArgb(41, 128, 185), SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleCenter },
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 32 }
            };

            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "Screen", Visible = false });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScreenName", HeaderText = "اسم الشاشة / الوظيفة", ReadOnly = true, FillWeight = 75 });
            dg.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanAccess", HeaderText = "👁️ دخول ورؤية", FillWeight = 25 });
            dg.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanEditPrice", HeaderText = "🏷️ تعديل السعر", FillWeight = 25 });
            dg.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanEditSalesInvoice", HeaderText = "✏️ تعديل الفاتورة", FillWeight = 25 });
            dg.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanDeleteSalesInvoice", HeaderText = "🗑️ حذف الفاتورة", FillWeight = 25 });
            dg.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanCopySalesInvoice", HeaderText = "📋 نسخ الفاتورة", FillWeight = 25 });
            dg.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanViewCost", HeaderText = "💲 رؤية التكلفة", FillWeight = 25 });
            dg.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanOrderColumns", HeaderText = "↕️ ترتيب الأعمدة", FillWeight = 25 });
            dg.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Live updates for counter
            dg.CellValueChanged += (s, e) => UpdateLiveCounter();
            dg.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dg.IsCurrentCellDirty)
                {
                    dg.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            // Toggle column on header click
            dg.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.ColumnIndex >= 2)
                {
                    ToggleColumn(dg, e.ColumnIndex);
                }
            };

            return dg;
        }

        private void ToggleColumn(DataGridView dg, int colIndex)
        {
            bool anyUnchecked = false;
            foreach (DataGridViewRow r in dg.Rows)
            {
                if (r.Visible && r.Cells[colIndex] is DataGridViewCheckBoxCell cell && !r.Cells[colIndex].ReadOnly)
                {
                    if (!ToBool(cell.Value))
                    {
                        anyUnchecked = true;
                        break;
                    }
                }
            }

            foreach (DataGridViewRow r in dg.Rows)
            {
                if (r.Visible && r.Cells[colIndex] is DataGridViewCheckBoxCell cell && !r.Cells[colIndex].ReadOnly)
                {
                    cell.Value = anyUnchecked;
                }
            }
            UpdateLiveCounter();
        }

        private void ToggleAllPermissions(bool check)
        {
            var grids = new[] { dgSales, dgPurchases, dgInventory, dgAdmin };
            foreach (var grid in grids)
            {
                if (grid == null) continue;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    for (int i = 2; i < grid.Columns.Count; i++)
                    {
                        if (row.Cells[i] is DataGridViewCheckBoxCell cell && !row.Cells[i].ReadOnly)
                        {
                            cell.Value = check;
                        }
                    }
                }
            }
            UpdateLiveCounter();
        }

        private void ToggleTabPermissions(int tabIndex, bool check)
        {
            DataGridView grid = null;
            if (tabIndex == 0) grid = dgSales;
            else if (tabIndex == 1) grid = dgPurchases;
            else if (tabIndex == 2) grid = dgInventory;
            else if (tabIndex == 3) grid = dgAdmin;

            if (grid != null)
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    for (int i = 2; i < grid.Columns.Count; i++)
                    {
                        if (row.Cells[i] is DataGridViewCheckBoxCell cell && !row.Cells[i].ReadOnly)
                        {
                            cell.Value = check;
                        }
                    }
                }
            }
            UpdateLiveCounter();
        }

        private void ApplyRolePreset(string role)
        {
            ToggleAllPermissions(false);

            var salesKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sales", "POS", "Returns", "Installments", "SalesList", "Clients", "Vehicles", "Maintenance", "DashSales" };
            var purchaseKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Purchases", "PurchaseReturn", "PurchasesList", "Suppliers", "SupplierStatement", "SupplierPayment", "SupplierAdjustment" };
            var inventoryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Products", "Categories", "Warehouses", "Inventory", "Wastage", "WarehouseTransfer", "WarehouseTransfersList", "BulkPrintBarcodes", "DashBelowMin" };
            var accountantKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CashBox", "Reports", "DailyClosing", "ShiftClose", "EmployeeTransactions", "DriverCustody", "SupplierStatement", "DashTreasury", "SalesAudit" };

            var grids = new[] { dgSales, dgPurchases, dgInventory, dgAdmin };
            foreach (var grid in grids)
            {
                if (grid == null) continue;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    string screen = row.Cells["Screen"].Value?.ToString();
                    if (string.IsNullOrEmpty(screen)) continue;

                    bool enable = false;
                    if (role == "Admin") enable = true;
                    else if (role == "Sales" && salesKeys.Contains(screen)) enable = true;
                    else if (role == "Purchases" && purchaseKeys.Contains(screen)) enable = true;
                    else if (role == "Inventory" && inventoryKeys.Contains(screen)) enable = true;
                    else if (role == "Accountant" && accountantKeys.Contains(screen)) enable = true;

                    if (enable)
                    {
                        if (row.Cells["CanAccess"] is DataGridViewCheckBoxCell cAcc) cAcc.Value = true;
                        if (role == "Admin")
                        {
                            for (int colIdx = 3; colIdx < grid.Columns.Count; colIdx++)
                            {
                                if (row.Cells[colIdx] is DataGridViewCheckBoxCell cOpt && !row.Cells[colIdx].ReadOnly)
                                {
                                    cOpt.Value = true;
                                }
                            }
                        }
                    }
                }
            }
            UpdateLiveCounter();
        }

        private void ApplySearchFilter()
        {
            string query = txtSearch.Text.Trim();
            var grids = new[] { dgSales, dgPurchases, dgInventory, dgAdmin };

            foreach (var grid in grids)
            {
                if (grid == null) continue;
                grid.CurrentCell = null;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    string name = row.Cells["ScreenName"].Value?.ToString() ?? "";
                    string key = row.Cells["Screen"].Value?.ToString() ?? "";
                    bool match = string.IsNullOrEmpty(query) ||
                                 name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 key.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                    row.Visible = match;
                }
            }
        }

        private void UpdateLiveCounter()
        {
            int totalScreens = 0;
            int accessedScreens = 0;
            int specialPerms = 0;

            var grids = new[] { dgSales, dgPurchases, dgInventory, dgAdmin };
            foreach (var grid in grids)
            {
                if (grid == null) continue;
                foreach (DataGridViewRow r in grid.Rows)
                {
                    totalScreens++;
                    if (ToBool(r.Cells["CanAccess"].Value))
                    {
                        accessedScreens++;
                    }
                    for (int c = 3; c < grid.Columns.Count; c++)
                    {
                        if (r.Cells[c] is DataGridViewCheckBoxCell && ToBool(r.Cells[c].Value))
                        {
                            specialPerms++;
                        }
                    }
                }
            }

            if (lblCounter != null)
            {
                lblCounter.Text = $"📊 الشاشات المسموحة: {accessedScreens} من أصل {totalScreens} | 🛡️ صلاحيات خاصة مفعلة: {specialPerms}";
            }
        }

        private void LoadPermissions()
        {
            var dt = EmployeeDAL.GetPermissions(_empID);

            dgSales.Rows.Clear();
            dgPurchases.Rows.Clear();
            dgInventory.Rows.Clear();
            dgAdmin.Rows.Clear();

            foreach (var screen in ScreensList)
            {
                bool access = false, editPrice = false, editInvoice = false, deleteInvoice = false, copyInvoice = false, viewCost = false, orderColumns = false;
                foreach (DataRow r in dt.Rows)
                {
                    if (string.Equals(r["ScreenName"].ToString(), screen.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        access = Convert.ToBoolean(r["CanAccess"]);
                        editPrice = Convert.ToBoolean(r["CanEditPrice"]);
                        editInvoice = r.Table.Columns.Contains("CanEditSalesInvoice") && r["CanEditSalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanEditSalesInvoice"]);
                        deleteInvoice = r.Table.Columns.Contains("CanDeleteSalesInvoice") && r["CanDeleteSalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanDeleteSalesInvoice"]);
                        copyInvoice = r.Table.Columns.Contains("CanCopySalesInvoice") && r["CanCopySalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanCopySalesInvoice"]);
                        viewCost = r.Table.Columns.Contains("CanViewCost") && r["CanViewCost"] != DBNull.Value && Convert.ToBoolean(r["CanViewCost"]);
                        orderColumns = r.Table.Columns.Contains("CanOrderColumns") && r["CanOrderColumns"] != DBNull.Value && Convert.ToBoolean(r["CanOrderColumns"]);
                        break;
                    }
                }

                DataGridView targetGrid = null;
                if (screen.TabIndex == 0) targetGrid = dgSales;
                else if (screen.TabIndex == 1) targetGrid = dgPurchases;
                else if (screen.TabIndex == 2) targetGrid = dgInventory;
                else if (screen.TabIndex == 3) targetGrid = dgAdmin;

                if (targetGrid != null)
                {
                    int ri = targetGrid.Rows.Add(screen.Key, screen.Name, access, editPrice, editInvoice, deleteInvoice, copyInvoice, viewCost, orderColumns);
                    
                    bool isSalesScreen = string.Equals(screen.Key, "Sales", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(screen.Key, "POS", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(screen.Key, "SalesList", StringComparison.OrdinalIgnoreCase);

                    bool isPurchasesScreen = string.Equals(screen.Key, "Purchases", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(screen.Key, "PurchasesList", StringComparison.OrdinalIgnoreCase);

                    if (!isSalesScreen && !isPurchasesScreen)
                    {
                        for (int colIdx = 3; colIdx <= 7; colIdx++)
                        {
                            DisableGridCell(targetGrid.Rows[ri], colIdx);
                        }
                    }
                    else if (isPurchasesScreen)
                    {
                        DisableGridCell(targetGrid.Rows[ri], 3); // EditPrice not for purchases
                        DisableGridCell(targetGrid.Rows[ri], 7); // ViewCost not for purchases
                    }
                    else if (string.Equals(screen.Key, "POS", StringComparison.OrdinalIgnoreCase))
                    {
                        for (int colIdx = 4; colIdx <= 7; colIdx++)
                        {
                            DisableGridCell(targetGrid.Rows[ri], colIdx);
                        }
                    }

                    // Format CanOrderColumns (colIdx = 8)
                    bool isOrderColumnsApplicable = string.Equals(screen.Key, "Sales", StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(screen.Key, "Purchases", StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(screen.Key, "POS", StringComparison.OrdinalIgnoreCase);

                    if (!isOrderColumnsApplicable)
                    {
                        DisableGridCell(targetGrid.Rows[ri], 8);
                    }
                }
            }

            UpdateLiveCounter();
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
            var grids = new[] { dgSales, dgPurchases, dgInventory, dgAdmin };
            foreach (var grid in grids)
            {
                if (grid == null) continue;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    string screen = row.Cells["Screen"].Value?.ToString();
                    if (string.IsNullOrEmpty(screen)) continue;

                    bool access  = ToBool(row.Cells["CanAccess"].Value);
                    bool editP   = ToBool(row.Cells["CanEditPrice"].Value);
                    bool editI   = ToBool(row.Cells["CanEditSalesInvoice"].Value);
                    bool deleteI = ToBool(row.Cells["CanDeleteSalesInvoice"].Value);
                    bool copyI   = ToBool(row.Cells["CanCopySalesInvoice"].Value);
                    bool viewC   = ToBool(row.Cells["CanViewCost"].Value);
                    bool orderC  = ToBool(row.Cells["CanOrderColumns"].Value);

                    EmployeeDAL.SavePermissions(_empID, screen, access, editP, editI, deleteI, copyI, viewC, orderC);
                }
            }
            MessageBox.Show($"✅ تم حفظ صلاحيات الموظف ({_empName}) بنجاح!", "حفظ الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }

}

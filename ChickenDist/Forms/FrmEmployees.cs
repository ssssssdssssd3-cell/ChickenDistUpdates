using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
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

        public FrmEmployees()
        {
            if (Session.Role != "Admin") { MessageBox.Show("غير مصرح لك بالوصول"); this.Close(); return; }
            InitUI();
            LoadEmployees();
        }

        private void InitUI()
        {
            this.Text = "إدارة الموظفين والصلاحيات";
            this.Size = new Size(1366, 768);
            this.MinimumSize = new Size(1024, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
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
            txtPassword.PasswordChar = '●';
            AddField(pnlDetails, "الهاتف:", ref y, out txtPhone);

            pnlDetails.Controls.Add(new Label { Text = "الدور:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            cboRole = new ComboBox { Location = new Point(15, y - 2), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput };
            cboRole.Items.AddRange(new object[] { "Admin", "Supervisor", "Driver", "Accountant", "User" });
            cboRole.SelectedIndex = 4;
            pnlDetails.Controls.Add(cboRole); y += 38;

            chkDriver = new CheckBox { Text = "مندوب توزيع", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain }; y += 32;
            chkActive = new CheckBox { Text = "موظف نشط", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true }; y += 40;
            pnlDetails.Controls.AddRange(new Control[] { chkDriver, chkActive });

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
            txtPassword.Clear();
            txtPhone.Text = dr["Phone"].ToString();
            cboRole.Text = dr["Role"].ToString();
            chkDriver.Checked = Convert.ToBoolean(dr["IsDriver"]);
            chkActive.Checked = Convert.ToBoolean(dr["IsActive"]);
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtName.Clear(); txtUsername.Clear(); txtPassword.Clear(); txtPhone.Clear();
            cboRole.SelectedIndex = 4;
            chkDriver.Checked = false; chkActive.Checked = true;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم الموظف"); return; }
            if (string.IsNullOrWhiteSpace(txtUsername.Text)) { MessageBox.Show("أدخل اسم المستخدم"); return; }
            if (_selectedID == 0 && string.IsNullOrWhiteSpace(txtPassword.Text)) { MessageBox.Show("أدخل كلمة المرور"); return; }

            int id = EmployeeDAL.Save(_selectedID, txtName.Text, txtUsername.Text,
                txtPassword.Text, cboRole.Text, txtPhone.Text, chkDriver.Checked, chkActive.Checked);
            if (id > 0) { MessageBox.Show("✅ تم الحفظ"); _selectedID = id; LoadEmployees(); }
            else MessageBox.Show("❌ فشل الحفظ");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;
            if (MessageBox.Show("إيقاف الموظف؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { EmployeeDAL.Delete(_selectedID); LoadEmployees(); ClearDetail(); }
        }

        private void BtnPerms_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) { MessageBox.Show("اختر موظفاً أولاً"); return; }
            new FrmPermissions(_selectedID, txtName.Text).ShowDialog();
        }
    }

    /// <summary>شاشة الصلاحيات</summary>
    public class FrmPermissions : Form
    {
        private int _empID;
        private DataGridView dgPerms;
        private Button btnSave;

        private static readonly string[] Screens = {
            "Sales", "DriverHandover", "DriverSales", "ImportPreview", "Clients", "CashBox",
            "Products", "Returns", "Reports", "Employees", "Suppliers", "Purchases", "Vehicles", "Inventory"
        };
        private static readonly string[] ScreenNames = {
            "المبيعات", "تقفيل الحمولة", "بيع المندوب الميداني", "استيراد مبيعات CSV",
            "العملاء", "الخزنة", "الأصناف", "المرتجع", "التقارير", "الموظفين", "الموردين", "المشتريات", "المركبات", "جرد المخزن"
        };

        public FrmPermissions(int empID, string empName)
        {
            _empID = empID;
            this.Text = "صلاحيات: " + empName;
            this.Size = new Size(780, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.RightToLeft = RightToLeft.Yes;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            dgPerms = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(745, 340),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgPerms.Columns.Add(new DataGridViewTextBoxColumn { Name = "Screen", Visible = false });
            dgPerms.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScreenName", HeaderText = "الشاشة", ReadOnly = true, FillWeight = 60 });
            dgPerms.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanAccess", HeaderText = "وصول", FillWeight = 30 });
            dgPerms.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanEditPrice", HeaderText = "تعديل سعر", FillWeight = 30 });
            dgPerms.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanEditSalesInvoice", HeaderText = "تعديل فاتورة", FillWeight = 30 });
            dgPerms.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanDeleteSalesInvoice", HeaderText = "حذف فاتورة", FillWeight = 30 });
            dgPerms.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanCopySalesInvoice", HeaderText = "نسخ فاتورة", FillWeight = 30 });
            dgPerms.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanViewCost", HeaderText = "عرض التكلفة", FillWeight = 30 });
            dgPerms.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.Controls.Add(dgPerms);

            btnSave = Theme.MakeButton("💾 حفظ الصلاحيات", 300, 360, 160, 34, Theme.Accent);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            LoadPermissions();
        }

        private void LoadPermissions()
        {
            var dt = EmployeeDAL.GetPermissions(_empID);
            dgPerms.Rows.Clear();
            for (int i = 0; i < Screens.Length; i++)
            {
                bool access = false, editPrice = false, editInvoice = false, deleteInvoice = false, copyInvoice = false, viewCost = false;
                foreach (DataRow r in dt.Rows)
                    if (r["ScreenName"].ToString() == Screens[i])
                    {
                        access = Convert.ToBoolean(r["CanAccess"]);
                        editPrice = Convert.ToBoolean(r["CanEditPrice"]);
                        editInvoice = r.Table.Columns.Contains("CanEditSalesInvoice") && r["CanEditSalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanEditSalesInvoice"]);
                        deleteInvoice = r.Table.Columns.Contains("CanDeleteSalesInvoice") && r["CanDeleteSalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanDeleteSalesInvoice"]);
                        copyInvoice = r.Table.Columns.Contains("CanCopySalesInvoice") && r["CanCopySalesInvoice"] != DBNull.Value && Convert.ToBoolean(r["CanCopySalesInvoice"]);
                        viewCost = r.Table.Columns.Contains("CanViewCost") && r["CanViewCost"] != DBNull.Value && Convert.ToBoolean(r["CanViewCost"]);
                        break;
                    }
                dgPerms.Rows.Add(Screens[i], ScreenNames[i], access, editPrice, editInvoice, deleteInvoice, copyInvoice, viewCost);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgPerms.Rows)
            {
                string screen = row.Cells["Screen"].Value?.ToString();
                bool access = Convert.ToBoolean(row.Cells["CanAccess"].Value);
                bool editP = Convert.ToBoolean(row.Cells["CanEditPrice"].Value);
                bool editI = Convert.ToBoolean(row.Cells["CanEditSalesInvoice"].Value);
                bool deleteI = Convert.ToBoolean(row.Cells["CanDeleteSalesInvoice"].Value);
                bool copyI = Convert.ToBoolean(row.Cells["CanCopySalesInvoice"].Value);
                bool viewC = Convert.ToBoolean(row.Cells["CanViewCost"].Value);
                EmployeeDAL.SavePermissions(_empID, screen, access, editP, editI, deleteI, copyI, viewC);
            }
            MessageBox.Show("✅ تم حفظ الصلاحيات");
            this.Close();
        }
    }
}

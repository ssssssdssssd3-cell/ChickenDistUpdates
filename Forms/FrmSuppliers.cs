using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة إدارة الموردين</summary>
    public class FrmSuppliers : Form
    {
        private DataGridView dgSuppliers;
        private TextBox txtSearch, txtCode, txtName, txtPhone, txtAddress;
        private ComboBox cboSearchFilter;
        private NumericUpDown nudOpening;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete, btnStatement, btnItemMovementReport;
        private Label lblBalance, lblSummary;
        private int _selectedID = 0;

        public FrmSuppliers()
        {
            InitUI();
            LoadSuppliers();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = "إدارة الموردين والأرصدة المستحقة";
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
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350f)); // Column 0 (Right): Supplier Form Card
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // Column 1 (Left): Grid Table
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Grid panel
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
                Text = "👥 إجمالي الموردين: --  |  💰 إجمالي المستحقات: -- ج"
            };

            var lblSearch = new Label
            {
                Text = "🔍 بحث الموردين:",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Padding = new Padding(0, 8, 6, 0)
            };

            cboSearchFilter = new ComboBox
            {
                Dock = DockStyle.Right,
                Width = 135,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboSearchFilter.Items.AddRange(new object[] { "🔍 الكل (شامل)", "🏢 بالاسم", "📞 برقم الهاتف", "🔢 بالكود" });
            cboSearchFilter.SelectedIndex = 0;
            cboSearchFilter.SelectedIndexChanged += (s, e) =>
            {
                if (IsPlaceholderText(txtSearch.Text))
                {
                    txtSearch.Text = GetCurrentPlaceholder();
                }
                string searchVal = IsPlaceholderText(txtSearch.Text) ? "" : txtSearch.Text.Trim();
                LoadSuppliers(searchVal);
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Right,
                Width = 260,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Text = "بحث بالاسم أو الهاتف أو الكود...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.Enter += (s, e) => { if (IsPlaceholderText(txtSearch.Text)) txtSearch.Text = ""; };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = GetCurrentPlaceholder(); };
            txtSearch.TextChanged += (s, e) =>
            {
                string searchVal = IsPlaceholderText(txtSearch.Text) ? "" : txtSearch.Text.Trim();
                LoadSuppliers(searchVal);
            };

            var btnSearchIcon = Theme.MakeButton("🔍", Color.FromArgb(37, 99, 235));
            btnSearchIcon.Dock = DockStyle.Right;
            btnSearchIcon.Width = 45;
            btnSearchIcon.Click += (s, e) =>
            {
                string searchVal = IsPlaceholderText(txtSearch.Text) ? "" : txtSearch.Text.Trim();
                LoadSuppliers(searchVal);
            };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    string searchVal = IsPlaceholderText(txtSearch.Text) ? "" : txtSearch.Text.Trim();
                    LoadSuppliers(searchVal);
                }
            };

            pnlSearch.Controls.Add(lblSummary);
            pnlSearch.Controls.Add(btnSearchIcon);
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(cboSearchFilter);
            pnlSearch.Controls.Add(lblSearch);

            dgSuppliers = new DataGridView
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
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierID", Visible = false });
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierCode", HeaderText = "الكود", FillWeight = 30 });
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "اسم المورد", FillWeight = 110 });
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "الهاتف", FillWeight = 60 });
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد المستحق", FillWeight = 55 });
            dgSuppliers.SelectionChanged += DgSuppliers_SelectionChanged;
            SetupSuppliersContextMenu();

            pnlGrid.Controls.Add(dgSuppliers);
            pnlGrid.Controls.Add(pnlSearch);

            // Detail panel (Supplier Form Card)
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
                Text = "📋 بطاقة وتفاصيل المورد",
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(241, 196, 15),
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
            };
            pnlDetails.Controls.Add(lblDetailsTitle);
            y += 34;

            pnlDetails.Controls.Add(MakeField("كود المورد:", ref y, out txtCode));
            txtCode.ReadOnly = true;
            txtCode.TabStop = false;
            txtCode.BackColor = Color.FromArgb(241, 245, 249);
            txtCode.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            pnlDetails.Controls.Add(MakeField("اسم المورد:", ref y, out txtName));
            txtName.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

            pnlDetails.Controls.Add(MakeField("الهاتف:", ref y, out txtPhone));
            pnlDetails.Controls.Add(MakeField("العنوان:", ref y, out txtAddress));

            var lblOp = new Label { Text = "رصيد افتتاحي:", Location = new Point(215, y + 4), AutoSize = true, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnlDetails.Controls.Add(lblOp);
            nudOpening = new NumericUpDown { Location = new Point(5, y), Width = 205, Height = 28, Minimum = -999999, Maximum = 9999999, DecimalPlaces = 2, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            pnlDetails.Controls.Add(nudOpening); y += 34;

            chkActive = new CheckBox { Text = "المورد نشط ونتعامل معه حالياً", Location = new Point(10, y), Width = 285, ForeColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Checked = true, RightToLeft = RightToLeft.Yes }; y += 32;
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

            btnSave = Theme.MakeButton("➕ حفظ المورد", 215, y, 100, 36, Color.FromArgb(16, 185, 129));
            btnNew = Theme.MakeButton("➕ إضافة", 115, y, 95, 36, Color.FromArgb(37, 99, 235));
            btnDelete = Theme.MakeButton("🗑 إيقاف", 10, y, 100, 36, Color.FromArgb(239, 68, 68));
            y += 42;

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            var btnExpense = Theme.MakeButton("💸 صرف دفعة", 215, y, 100, 34, Color.FromArgb(5, 150, 105));
            btnExpense.Click += BtnExpense_Click;

            var btnAdjustment = Theme.MakeButton("⚖️ تسوية رصيد", 115, y, 95, 34, Color.FromArgb(71, 85, 105));
            btnAdjustment.Click += BtnAdjustment_Click;

            btnStatement = Theme.MakeButton("📋 كشف حساب", 10, y, 100, 34, Color.FromArgb(37, 99, 235));
            btnStatement.Click += (s, e) =>
            {
                if (_selectedID == 0) { MessageBox.Show("اختر مورداً من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                new FrmSupplierStatement(_selectedID, txtName.Text).ShowDialog();
            };
            y += 40;

            btnItemMovementReport = Theme.MakeButton("📊 حركة الأصناف الموردة", 165, y, 150, 34, Color.FromArgb(2, 132, 199));
            btnItemMovementReport.Click += BtnItemMovementReport_Click;

            var btnWhatsApp = Theme.MakeButton("📲 إرسال كشف الحساب واتساب", 10, y, 150, 34, Color.FromArgb(37, 211, 102));
            btnWhatsApp.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnWhatsApp.ForeColor = Color.White;
            btnWhatsApp.Click += (s, e) =>
            {
                if (_selectedID == 0) { MessageBox.Show("اختر مورداً من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                string phone = txtPhone.Text.Trim();
                string name = txtName.Text.Trim();
                decimal bal = 0m;
                try
                {
                    object balObj = DbHelper.Scalar("SELECT Balance FROM Suppliers WHERE SupplierID = @id", DbHelper.P("@id", _selectedID));
                    if (balObj != null && balObj != DBNull.Value) bal = Convert.ToDecimal(balObj);
                }
                catch { }

                string msg = $"📊 *كشف حساب مورد - {AppConfig.CompanyName}*\n" +
                             $"👤 *المورد:* {name}\n" +
                             $"📅 *التاريخ:* {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                             $"💵 *الرصيد المستحق للمورد:* {bal:N2} ج\n" +
                             $"\nشاكرين ومقدرين حسن تعاونكم معنا! 🙏";

                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                    this,
                    phone,
                    msg,
                    () => ReceiptImageGenerator.GenerateTextCardImage("كشف حساب مورد", msg),
                    "📱 إرسال كشف حساب المورد عبر الواتساب");
            };

            pnlDetails.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete, btnExpense, btnAdjustment, btnStatement, btnItemMovementReport, btnWhatsApp });

            tbl.Controls.Add(pnlDetails, 0, 0);
            tbl.Controls.Add(pnlGrid, 1, 0);
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

        private string GetCurrentPlaceholder()
        {
            if (cboSearchFilter == null) return "بحث بالاسم أو الهاتف أو الكود...";
            switch (cboSearchFilter.SelectedIndex)
            {
                case 1: return "اكتب اسم المورد للبحث...";
                case 2: return "اكتب رقم الهاتف للبحث...";
                case 3: return "اكتب كود المورد للبحث...";
                default: return "بحث بالاسم أو الهاتف أو الكود...";
            }
        }

        private bool IsPlaceholderText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ||
                   text == "بحث بالاسم أو الهاتف أو الكود..." ||
                   text == "اكتب اسم المورد للبحث..." ||
                   text == "اكتب رقم الهاتف للبحث..." ||
                   text == "اكتب كود المورد للبحث...";
        }

        private void LoadSuppliers(string search = "")
        {
            dgSuppliers.Rows.Clear();
            if (IsPlaceholderText(search)) search = "";

            DataTable dt = SupplierDAL.GetAll();

            dgSuppliers.SuspendLayout();
            var oldMode = dgSuppliers.AutoSizeColumnsMode;
            dgSuppliers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            int filterType = cboSearchFilter != null ? cboSearchFilter.SelectedIndex : 0;
            decimal totalDue = 0;
            int count = 0;
            foreach (DataRow r in dt.Rows)
            {
                string code = r["SupplierCode"]?.ToString() ?? "";
                string name = r["SupplierName"]?.ToString() ?? "";
                string phone = r["Phone"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(search))
                {
                    bool match = false;
                    switch (filterType)
                    {
                        case 1: // بالاسم
                            match = name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        case 2: // برقم الهاتف
                            match = phone.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        case 3: // بالكود
                            match = code.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        default: // الكل
                            match = name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    phone.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    code.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                    }
                    if (!match) continue;
                }

                decimal bal = Convert.ToDecimal(r["Balance"]);
                totalDue += bal;
                count++;
                int rowIndex = dgSuppliers.Rows.Add(r["SupplierID"], code, name, phone, bal.ToString("N2") + " ج");
                var row = dgSuppliers.Rows[rowIndex];

                // تلوين مريح لعمود الرصيد فقط
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
            }

            if (lblSummary != null)
            {
                lblSummary.Text = string.Format("👥 إجمالي الموردين: {0:N0}  |  💰 إجمالي المستحقات: {1:N2} ج", count, totalDue);
            }

            dgSuppliers.AutoSizeColumnsMode = oldMode;
            dgSuppliers.ResumeLayout();
        }

        private void DgSuppliers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgSuppliers.SelectedRows.Count == 0) return;
            var row = dgSuppliers.SelectedRows[0];
            _selectedID = Convert.ToInt32(row.Cells["SupplierID"].Value);

            DataTable dt = SupplierDAL.GetAll();
            DataRow dr = null;
            foreach (DataRow r in dt.Rows)
                if (Convert.ToInt32(r["SupplierID"]) == _selectedID) { dr = r; break; }
            if (dr == null) return;

            txtCode.Text = dr["SupplierCode"].ToString();
            txtName.Text = dr["SupplierName"].ToString();
            txtPhone.Text = dr["Phone"].ToString();
            txtAddress.Text = dr["Address"].ToString();
            nudOpening.Value = Convert.ToDecimal(dr["OpeningBalance"]);
            chkActive.Checked = Convert.ToBoolean(dr["IsActive"]);
            lblBalance.Text = "الرصيد: " + row.Cells["Balance"].Value;

            if (btnSave != null)
            {
                btnSave.Text = "💾 حفظ التعديل";
                btnSave.BackColor = Color.FromArgb(245, 158, 11);
            }
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtCode.Text = SupplierDAL.GetNextSupplierCode();
            txtName.Clear(); txtPhone.Clear(); txtAddress.Clear();
            nudOpening.Value = 0; chkActive.Checked = true;
            lblBalance.Text = "الرصيد الحالي: ---";

            if (btnSave != null)
            {
                btnSave.Text = "➕ حفظ المورد";
                btnSave.BackColor = Color.FromArgb(16, 185, 129);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0 && !Session.CanAdd("Suppliers"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية إضافة موردين جُدد!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedID > 0 && !Session.CanEdit("Suppliers"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية تعديل بيانات الموردين!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم المورد"); return; }

            // ─── فحص تكرار الاسم المتقدم بالكامل وبالاسم الثنائي ───
            if (SupplierDAL.CheckDuplicateName(txtName.Text.Trim(), _selectedID, out string dupName, out string dupCode, out string dupReason))
            {
                MessageBox.Show(
                    $"⚠️ لا يمكن الحفظ لمنع التكرار والتضارب المحاسبي:\n\n" +
                    $"يوجد مورد مسجل مسبقاً ({dupReason}):\n" +
                    $"• الاسم المسجل: \"{dupName}\"\n" +
                    $"• كود المورد: {dupCode}\n\n" +
                    $"💡 للتسجيل: يرجى كتابة الاسم ثلاثياً أو إضافة تمييز لاسم المورد الجديد.",
                    "تكرار اسم المورد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            // ─── فحص تكرار رقم الهاتف ───
            if (!string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                if (SupplierDAL.IsDuplicatePhone(txtPhone.Text.Trim(), _selectedID))
                {
                    MessageBox.Show($"⚠️ رقم الهاتف \"{txtPhone.Text.Trim()}\" مسجَّل لمورد آخر بالفعل.\nيرجى التحقق من الرقم أو البحث عن المورد الموجود.",
                        "تكرار رقم الهاتف", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPhone.Focus();
                    return;
                }
            }

            int id = SupplierDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtPhone.Text,
                txtAddress.Text, nudOpening.Value, chkActive.Checked);
            if (id > 0) { SupplierCache.Refresh(); MessageBox.Show("✅ تم الحفظ"); _selectedID = id; LoadSuppliers(); }
            else MessageBox.Show("❌ فشل الحفظ");
        }


        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!Session.CanDelete("Suppliers"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية حذف وإيقاف الموردين!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedID == 0) return;
            if (MessageBox.Show("إيقاف تفعيل المورد؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SupplierDAL.Delete(_selectedID);
                SupplierCache.Refresh();
                LoadSuppliers();
                ClearDetail();
            }
        }

        private void BtnExpense_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("اختر مورداً أولاً من القائمة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string supplierName = txtName.Text;
            decimal currentBal = 0m;
            try { currentBal = SupplierDAL.GetBalance(_selectedID); } catch { }

            // نافذة الصرف
            var dlg = new Form
            {
                Text = "💸 صرف نقدي وسداد دفعة للمورد - " + supplierName,
                Size = new Size(420, 380),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain
            };

            int dy = 15;

            // بطاقة معاينة الأرصدة
            var pnlBalPreview = new Panel
            {
                Location = new Point(15, dy),
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
                ForeColor = currentBal > 0 ? Color.FromArgb(248, 113, 113) : (currentBal < 0 ? Color.FromArgb(74, 222, 128) : Color.White),
                Location = new Point(10, 8),
                Width = 230,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblNewTitle = new Label
            {
                Text = "الرصيد بعد السداد:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Location = new Point(240, 42),
                AutoSize = true
            };
            var lblNewBalVal = new Label
            {
                Text = currentBal.ToString("N2") + " ج",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Location = new Point(10, 38),
                Width = 225,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlBalPreview.Controls.AddRange(new Control[] { lblCurTitle, lblCurBalVal, lblNewTitle, lblNewBalVal });
            dlg.Controls.Add(pnlBalPreview);
            dy += 85;

            // اسم المورد
            dlg.Controls.Add(new Label
            {
                Text = "🏢 المورد: " + supplierName,
                Location = new Point(15, dy),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            });
            dy += 30;

            // المبلغ (يدوي بدون أسهم)
            dlg.Controls.Add(new Label { 
                Text = "المبلغ المصروف (ج):", 
                Location = new Point(15, dy + 6), 
                AutoSize = true, 
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            });
            var txtAmount = new TextBox
            {
                Location = new Point(150, dy),
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
                lblNewBalVal.ForeColor = newBal > 0 ? Color.FromArgb(248, 113, 113) : (newBal < 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(250, 204, 21));
            };
            txtAmount.Enter += (s, e) => txtAmount.SelectAll();
            dlg.Controls.Add(txtAmount);
            dy += 45;

            // ملاحظات
            dlg.Controls.Add(new Label { 
                Text = "ملاحظات / البيان:", 
                Location = new Point(15, dy + 4), 
                AutoSize = true, 
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f)
            });
            var txtNote = new TextBox
            {
                Location = new Point(150, dy),
                Width = 240,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f),
                RightToLeft = RightToLeft.Yes,
                Text = "سداد جزء من مستحقات المورد"
            };
            dlg.Controls.Add(txtNote);
            dy += 50;

            var btnOk = Theme.MakeButton("✅ تأكيد الصرف", 215, dy, 175, 38, Color.FromArgb(180, 83, 9));
            var btnCancel = Theme.MakeButton("❌ إلغاء", 115, dy, 90, 38, Color.FromArgb(90, 90, 90));
            btnOk.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

            btnOk.Click += (s2, e2) =>
            {
                if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amt) || amt <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAmount.Focus();
                    return;
                }
                try
                {
                    string code = SupplierDAL.AddSupplierPayment(_selectedID, amt, txtNote.Text.Trim());
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadSuppliers();

                    // Open print & WhatsApp options dialog for supplier payment
                    new FrmPrintSupplierPayment(_selectedID, amt, txtNote.Text.Trim(), supplierName: supplierName).ShowOptionsDialog(this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("فشل تسجيل الصرف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (s2, e2) => dlg.Close();

            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);

            dlg.Shown += (s, e) => txtAmount.Focus();
            dlg.ShowDialog(this);
        }

        private void BtnAdjustment_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("اختر مورداً أولاً من القائمة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var frm = new FrmAdjustment(_selectedID, txtName.Text, false);
            if (frm.ShowDialog() == DialogResult.OK)
                LoadSuppliers();
        }

        private void BtnItemMovementReport_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("اختر مورداً من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            new FrmReports("Suppliers", _selectedID).ShowDialog();
        }

        private void SetupSuppliersContextMenu()
        {
            var ctx = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };

            var miStatement = new ToolStripMenuItem("📑 كشف حساب تفصيلي", null, (s, e) =>
            {
                if (dgSuppliers.SelectedRows.Count > 0 && dgSuppliers.Columns.Contains("SupplierID"))
                {
                    int sid = Convert.ToInt32(dgSuppliers.SelectedRows[0].Cells["SupplierID"].Value);
                    string sname = dgSuppliers.SelectedRows[0].Cells["SupplierName"].Value?.ToString() ?? "";
                    new FrmSupplierStatement(sid, sname).ShowDialog(this);
                }
            });

            var miMovement = new ToolStripMenuItem("📊 تقرير حركة أصناف المورد", null, (s, e) =>
            {
                if (dgSuppliers.SelectedRows.Count > 0 && dgSuppliers.Columns.Contains("SupplierID"))
                {
                    BtnItemMovementReport_Click(s, e);
                }
            });

            var miPayment = new ToolStripMenuItem("💵 سند صرف سريع", null, (s, e) =>
            {
                if (dgSuppliers.SelectedRows.Count > 0 && dgSuppliers.Columns.Contains("SupplierID"))
                {
                    BtnExpense_Click(s, e);
                }
            });

            var miWhatsApp = new ToolStripMenuItem("📱 مراسلة واتساب للمورد", null, (s, e) =>
            {
                if (dgSuppliers.SelectedRows.Count > 0 && dgSuppliers.Columns.Contains("SupplierID"))
                {
                    string phone = dgSuppliers.SelectedRows[0].Cells["Phone"].Value?.ToString() ?? "";
                    string name = dgSuppliers.SelectedRows[0].Cells["SupplierName"].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(phone))
                    {
                        string msg = Uri.EscapeDataString($"مرحباً {name}، نود الاستفسار بشأن الحساب والطلبيات.");
                        string cleanPhone = phone.Replace(" ", "").Replace("-", "");
                        if (cleanPhone.StartsWith("01")) cleanPhone = "2" + cleanPhone;
                        try { System.Diagnostics.Process.Start($"https://wa.me/{cleanPhone}?text={msg}"); } catch { }
                    }
                    else
                    {
                        MessageBox.Show("لا يوجد رقم هاتف مسجل لهذا المورد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            });

            var miCopyPhone = new ToolStripMenuItem("📋 نسخ رقم الهاتف", null, (s, e) =>
            {
                if (dgSuppliers.SelectedRows.Count > 0 && dgSuppliers.Columns.Contains("Phone"))
                {
                    string phone = dgSuppliers.SelectedRows[0].Cells["Phone"].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(phone))
                    {
                        Clipboard.SetText(phone);
                    }
                }
            });

            ctx.Items.AddRange(new ToolStripItem[] {
                miStatement,
                miMovement,
                miPayment,
                miWhatsApp,
                new ToolStripSeparator(),
                miCopyPhone
            });

            dgSuppliers.ContextMenuStrip = ctx;
            dgSuppliers.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = dgSuppliers.HitTest(e.X, e.Y);
                    if (hit.RowIndex >= 0)
                    {
                        dgSuppliers.ClearSelection();
                        dgSuppliers.Rows[hit.RowIndex].Selected = true;
                        dgSuppliers.CurrentCell = dgSuppliers.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
                    }
                }
            };
        }
    }
}

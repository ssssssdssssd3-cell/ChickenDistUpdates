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
        private NumericUpDown nudOpening;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete, btnStatement, btnItemMovementReport;
        private Label lblBalance;
        private int _selectedID = 0;

        public FrmSuppliers()
        {
            InitUI();
            LoadSuppliers();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = "إدارة الموردين";
            this.Size = new Size(1050, 620);
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
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Grid panel
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.BgSearchPanel, Padding = new Padding(8) };
            Theme.StyleSearchHeaderPanel(pnlSearch);
            var lblSearch = new Label { Text = "🔍 بحث الموردين:", Dock = DockStyle.Right, AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextSearchLabel, Margin = new Padding(0, 6, 8, 0) };
            txtSearch = new TextBox { Dock = DockStyle.Right, Width = 260, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), Text = "بحث بالاسم أو الهاتف...", Font = new Font("Segoe UI", 10F, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "بحث بالاسم أو الهاتف...") txtSearch.Text = ""; };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "بحث بالاسم أو الهاتف..."; };
            txtSearch.TextChanged += (s, e) =>
            {
                string searchVal = txtSearch.Text;
                if (searchVal == "بحث بالاسم أو الهاتف...") searchVal = "";
                LoadSuppliers(searchVal);
            };
            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(txtSearch);

            dgSuppliers = new DataGridView
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
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierID", Visible = false });
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierCode", HeaderText = "الكود", FillWeight = 30 });
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "اسم المورد" });
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "الهاتف", FillWeight = 60 });
            dgSuppliers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد", FillWeight = 50 });
            dgSuppliers.SelectionChanged += DgSuppliers_SelectionChanged;
            SetupSuppliersContextMenu();

            pnlGrid.Controls.Add(dgSuppliers);
            pnlGrid.Controls.Add(pnlSearch);

            // Detail panel
            var pnlDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(12),
                AutoScroll = true
            };

            int y = 10;
            pnlDetails.Controls.Add(MakeField("كود المورد:", ref y, out txtCode));
            txtCode.ReadOnly = true;
            txtCode.TabStop = false;
            pnlDetails.Controls.Add(MakeField("اسم المورد:", ref y, out txtName));
            pnlDetails.Controls.Add(MakeField("الهاتف:", ref y, out txtPhone));
            pnlDetails.Controls.Add(MakeField("العنوان:", ref y, out txtAddress));

            var lblOp = new Label { Text = "رصيد افتتاحي:", Location = new Point(200, y), AutoSize = true, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(lblOp);
            nudOpening = new NumericUpDown { Location = new Point(10, y - 2), Width = 185, Minimum = -999999, Maximum = 9999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            pnlDetails.Controls.Add(nudOpening); y += 36;

            chkActive = new CheckBox { Text = "نشط", Location = new Point(110, y), Width = 185, ForeColor = Theme.TextMain, Checked = true, RightToLeft = RightToLeft.Yes }; y += 36;
            pnlDetails.Controls.Add(chkActive);

            lblBalance = new Label { Text = "الرصيد الحالي: ---", Location = new Point(10, y), Width = 285, AutoSize = false, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 11, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight }; y += 40;
            pnlDetails.Controls.Add(lblBalance);

            btnSave = Theme.MakeButton("💾 حفظ", 210, y, 90, 32, Theme.Accent);
            btnNew = Theme.MakeButton("🆕 جديد", 110, y, 90, 32, Theme.Success);
            btnDelete = Theme.MakeButton("🗑 إيقاف", 10, y, 90, 32, Theme.Danger);

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            var btnExpense = Theme.MakeButton("💸 صرف", 205, y + 40, 95, 32, Theme.Primary);
            btnExpense.Click += BtnExpense_Click;

            var btnAdjustment = Theme.MakeButton("⚖️ تسوية", 110, y + 40, 90, 32, Theme.Secondary);
            btnAdjustment.Click += BtnAdjustment_Click;

            btnStatement = Theme.MakeButton("📋 كشف", 10, y + 40, 95, 32, Theme.Accent);
            btnStatement.Click += (s, e) =>
            {
                if (_selectedID == 0) { MessageBox.Show("اختر مورداً من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                new FrmSupplierStatement(_selectedID, txtName.Text).ShowDialog();
            };

            btnItemMovementReport = Theme.MakeButton("📊 حركة الأصناف", 160, y + 80, 140, 32, Color.FromArgb(70, 130, 180));
            btnItemMovementReport.Click += BtnItemMovementReport_Click;

            var btnWhatsApp = Theme.MakeButton("📱 واتساب المورد", 10, y + 80, 140, 32, Color.FromArgb(37, 211, 102));
            btnWhatsApp.Font = Theme.FontBold;
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
            var p = new Panel { Location = new Point(5, y), Width = 310, Height = 32 };
            p.Controls.Add(new Label { Text = label, Location = new Point(200, 5), AutoSize = true, ForeColor = Theme.TextMain });
            txt = new TextBox { Location = new Point(10, 1), Width = 185, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            p.Controls.Add(txt);
            y += 38;
            return p;
        }

        private void LoadSuppliers(string search = "")
        {
            dgSuppliers.Rows.Clear();
            if (search == "بحث بالاسم أو الهاتف...") search = "";

            DataTable dt = SupplierDAL.GetAll();

            foreach (DataRow r in dt.Rows)
            {
                string name = r["SupplierName"].ToString();
                string phone = r["Phone"].ToString();
                if (!string.IsNullOrEmpty(search) && !name.Contains(search) && !phone.Contains(search))
                    continue;

                decimal bal = Convert.ToDecimal(r["Balance"]);
                var row = dgSuppliers.Rows.Add(r["SupplierID"], r["SupplierCode"], name, phone, bal.ToString("N2") + " ج");
                if (bal > 0) dgSuppliers.Rows[row].DefaultCellStyle.ForeColor = Color.OrangeRed;
            }
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
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtCode.Text = SupplierDAL.GetNextSupplierCode();
            txtName.Clear(); txtPhone.Clear(); txtAddress.Clear();
            nudOpening.Value = 0; chkActive.Checked = true;
            lblBalance.Text = "الرصيد الحالي: ---";
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

            // ─── فحص تكرار الاسم ───
            if (SupplierDAL.IsDuplicateName(txtName.Text.Trim(), _selectedID))
            {
                MessageBox.Show($"⚠️ يوجد مورد آخر بنفس الاسم: \"{txtName.Text.Trim()}\"\nيرجى استخدام اسم مختلف أو البحث عن المورد الموجود.",
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
            string balText = lblBalance.Text;

            // نافذة الصرف
            var dlg = new Form
            {
                Text = "💸 صرف نقدي للمورد - " + supplierName,
                Size = new Size(420, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgMain,
                Font = Theme.FontMain
            };

            int dy = 18;
            dlg.Controls.Add(new Label
            {
                Text = "المورد: " + supplierName,
                Location = new Point(10, dy), Width = 380,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            }); dy += 30;

            dlg.Controls.Add(new Label
            {
                Text = balText,
                Location = new Point(10, dy), Width = 380,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            }); dy += 36;

            dlg.Controls.Add(new Label { Text = "المبلغ المصروف (ج):", Location = new Point(200, dy + 5), Width = 180, ForeColor = Theme.TextMain });
            var nudAmt = new NumericUpDown
            {
                Location = new Point(10, dy), Width = 185,
                Minimum = 0.01m, Maximum = 9999999, DecimalPlaces = 2,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            dlg.Controls.Add(nudAmt); dy += 40;

            dlg.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(200, dy + 5), Width = 180, ForeColor = Theme.TextMain });
            var txtNote = new TextBox
            {
                Location = new Point(10, dy), Width = 185,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                Text = "سداد جزء من المديونية"
            };
            dlg.Controls.Add(txtNote); dy += 40;

            var btnOk     = Theme.MakeButton("✅ تأكيد الصرف", 210, dy, 175, 38, Color.FromArgb(140, 80, 0));
            var btnCancel = Theme.MakeButton("❌ إلغاء",        10,  dy, 120, 38, Color.FromArgb(100, 40, 40));
            btnOk.Font    = new Font("Segoe UI", 10, FontStyle.Bold);

            btnOk.Click += (s2, e2) =>
            {
                if (nudAmt.Value <= 0) { MessageBox.Show("أدخل مبلغاً أكبر من صفر."); return; }
                try
                {
                    string code = SupplierDAL.AddSupplierPayment(_selectedID, nudAmt.Value, txtNote.Text.Trim());
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadSuppliers();

                    // Open print & WhatsApp options dialog for supplier payment
                    new FrmPrintSupplierPayment(_selectedID, nudAmt.Value, txtNote.Text.Trim(), supplierName: supplierName).ShowOptionsDialog(this);
                }
                catch { }
            };
            btnCancel.Click += (s2, e2) => dlg.Close();

            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);
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

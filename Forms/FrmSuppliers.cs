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
        private Button btnNew, btnSave, btnDelete, btnPaySupplier;
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
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Theme.BgCard, Padding = new Padding(6) };
            txtSearch = new TextBox { Dock = DockStyle.Right, Width = 250, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "بحث بالاسم أو الهاتف...", Font = Theme.FontMain, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "بحث بالاسم أو الهاتف...") txtSearch.Text = ""; };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "بحث بالاسم أو الهاتف..."; };
            txtSearch.TextChanged += (s, e) =>
            {
                string searchVal = txtSearch.Text;
                if (searchVal == "بحث بالاسم أو الهاتف...") searchVal = "";
                LoadSuppliers(searchVal);
            };
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
            btnNew = Theme.MakeButton("🆕 جديد", 110, y, 90, 32, Color.FromArgb(60, 100, 60));
            btnDelete = Theme.MakeButton("🗑 إيقاف", 10, y, 90, 32, Color.FromArgb(140, 40, 40));

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            pnlDetails.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });

            // زر صرف للمورد
            y += 40;
            btnPaySupplier = Theme.MakeButton("💸 صرف للمورد", 10, y, 290, 38, Color.FromArgb(180, 100, 0));
            btnPaySupplier.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnPaySupplier.Click += BtnPaySupplier_Click;
            pnlDetails.Controls.Add(btnPaySupplier);

            tbl.Controls.Add(pnlDetails, 0, 0);
            tbl.Controls.Add(pnlGrid, 1, 0);
            this.Controls.Add(tbl);
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
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم المورد"); return; }
            int id = SupplierDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtPhone.Text,
                txtAddress.Text, nudOpening.Value, chkActive.Checked);
            if (id > 0) { MessageBox.Show("✅ تم الحفظ"); _selectedID = id; LoadSuppliers(); }
            else MessageBox.Show("❌ فشل الحفظ");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;
            if (MessageBox.Show("إيقاف تفعيل المورد؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SupplierDAL.Delete(_selectedID);
                LoadSuppliers();
                ClearDetail();
            }
        }

        private void BtnPaySupplier_Click(object sender, EventArgs e)
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
                Text = "صرف نقدي للمورد - " + supplierName,
                Size = new Size(400, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgMain
            };

            int dy = 15;
            dlg.Controls.Add(new Label { Text = "المورد: " + supplierName, Location = new Point(10, dy), Width = 360, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 10, FontStyle.Bold) }); dy += 28;
            dlg.Controls.Add(new Label { Text = balText, Location = new Point(10, dy), Width = 360, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10, FontStyle.Bold) }); dy += 32;

            dlg.Controls.Add(new Label { Text = "المبلغ المصروف (ج):", Location = new Point(200, dy + 4), Width = 170, ForeColor = Theme.TextMain });
            var nudAmt = new NumericUpDown { Location = new Point(10, dy), Width = 185, Minimum = 0.01m, Maximum = 9999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            dlg.Controls.Add(nudAmt); dy += 38;

            dlg.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(200, dy + 4), Width = 170, ForeColor = Theme.TextMain });
            var txtNote = new TextBox { Location = new Point(10, dy), Width = 185, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "سداد جزء من المديونية" };
            dlg.Controls.Add(txtNote); dy += 38;

            var btnOk = Theme.MakeButton("✅ تأكيد الصرف", 200, dy, 170, 36, Color.FromArgb(180, 100, 0));
            var btnCancel = Theme.MakeButton("❌ إلغاء", 10, dy, 120, 36, Color.FromArgb(100, 40, 40));

            btnOk.Click += (s2, e2) =>
            {
                if (nudAmt.Value <= 0) { MessageBox.Show("أدخل مبلغاً أكبر من صفر."); return; }
                try
                {
                    string code = SupplierDAL.AddSupplierPayment(_selectedID, nudAmt.Value, txtNote.Text.Trim());
                    MessageBox.Show(
                        $"✅ تم الصرف بنجاح!\n\nكود القيد: {code}\nالمبلغ: {nudAmt.Value:N2} ج\nالمورد: {supplierName}",
                        "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadSuppliers();
                }
                catch { } // الخطأ بيتعرض تلقائياً من RunInTransaction
            };
            btnCancel.Click += (s2, e2) => dlg.Close();

            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);
            dlg.ShowDialog(this);
        }
    }
}

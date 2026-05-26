using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmVehicles : Form
    {
        private DataGridView dgVehicles;
        private TextBox txtVehicleType;
        private TextBox txtVehicleName;
        private TextBox txtLicensePlate;
        private TextBox txtNotes;
        private CheckBox chkActive;
        private Button btnNewVehicle;
        private Button btnSaveVehicle;
        private Button btnDeleteVehicle;
        private int _selectedVehicleID = 0;

        public FrmVehicles()
        {
            this.Text = "إدارة المركبات والعربيات";
            this.Size = new Size(920, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("🚗 المركبات", "سجل المركبات وأنواع العربيات للمصروفات والمهام اللوجستية");
            this.Controls.Add(pnlTop);

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10),
                BackColor = Theme.BgMain
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));

            // Details Panel using TableLayoutPanel to avoid overlap
            var pnlDetails = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };

            var tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 6,
                RightToLeft = RightToLeft.Yes,
                Padding = new Padding(10),
                Height = 410
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f)); // Label
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // Input
            for (int i = 0; i < 6; i++) tblFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));

            // Row 0: نوع العربية
            tblFields.Controls.Add(new Label { Text = "نوع العربية:", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0,10,5,0) }, 0, 0);
            txtVehicleType = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0,6,0,6) };
            tblFields.Controls.Add(txtVehicleType, 1, 0);

            // Row 1: اسم المركبة
            tblFields.Controls.Add(new Label { Text = "اسم المركبة:", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0,10,5,0) }, 0, 1);
            txtVehicleName = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0,6,0,6) };
            tblFields.Controls.Add(txtVehicleName, 1, 1);

            // Row 2: رقم اللوحة
            tblFields.Controls.Add(new Label { Text = "رقم اللوحة:", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0,10,5,0) }, 0, 2);
            txtLicensePlate = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0,6,0,6) };
            tblFields.Controls.Add(txtLicensePlate, 1, 2);

            // Row 3: ملاحظات
            tblFields.Controls.Add(new Label { Text = "ملاحظات:", AutoSize = true, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0,10,5,0) }, 0, 3);
            txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0,6,0,6) };
            tblFields.RowStyles[3] = new RowStyle(SizeType.Absolute, 100f);
            tblFields.Height += 45;
            tblFields.Controls.Add(txtNotes, 1, 3);

            // Row 4: نشط
            chkActive = new CheckBox { Text = "نشط", AutoSize = true, ForeColor = Theme.TextMain, Checked = true, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0,10,5,0) };
            tblFields.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 4);
            tblFields.Controls.Add(chkActive, 1, 4);

            pnlDetails.Controls.Add(tblFields);

            // Buttons panel at bottom
            var pnlBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Theme.BgCard,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 8, 10, 0),
                WrapContents = false
            };
            btnNewVehicle = Theme.MakeButton("🆕 جديد", Color.FromArgb(80, 120, 80));
            btnNewVehicle.Size = new Size(100, 38);
            btnNewVehicle.Margin = new Padding(5, 0, 0, 0);
            btnNewVehicle.Click += (s, e) => ClearVehicle();
            btnSaveVehicle = Theme.MakeButton("💾 حفظ المركبة", Theme.Accent);
            btnSaveVehicle.Size = new Size(150, 38);
            btnSaveVehicle.Margin = new Padding(5, 0, 0, 0);
            btnSaveVehicle.Click += BtnSaveVehicle_Click;
            btnDeleteVehicle = Theme.MakeButton("🗑 حذف", Color.FromArgb(140, 40, 40));
            btnDeleteVehicle.Size = new Size(90, 38);
            btnDeleteVehicle.Margin = new Padding(5, 0, 0, 0);
            btnDeleteVehicle.Click += BtnDeleteVehicle_Click;
            pnlBtns.Controls.AddRange(new Control[] { btnDeleteVehicle, btnSaveVehicle, btnNewVehicle });

            pnlDetails.Controls.Add(pnlBtns);
            tblFields.BringToFront();
            pnlBtns.SendToBack();

            dgVehicles = new DataGridView
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
            dgVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "VehicleID", Visible = false });
            dgVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "VehicleType", HeaderText = "نوع العربية" });
            dgVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "VehicleName", HeaderText = "اسم المركبة" });
            dgVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "LicensePlate", HeaderText = "رقم اللوحة" });
            dgVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "نشط", FillWeight = 25 });
            dgVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات" });
            dgVehicles.SelectionChanged += DgVehicles_SelectionChanged;

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 0) };
            pnlGrid.Controls.Add(dgVehicles);

            tbl.Controls.Add(pnlDetails, 0, 0);
            tbl.Controls.Add(pnlGrid, 1, 0);

            this.Controls.Add(tbl);
            Theme.ApplyFormRTL(this);
            LoadVehicles();
        }

        private void LoadVehicles()
        {
            dgVehicles.Rows.Clear();
            foreach (DataRow row in VehicleDAL.GetAll().Rows)
            {
                var index = dgVehicles.Rows.Add(row["VehicleID"], row["VehicleType"], row["VehicleName"], row["LicensePlate"], Convert.ToBoolean(row["IsActive"]) ? "نعم" : "لا", row["Notes"]);
                dgVehicles.Rows[index].Tag = row["VehicleID"];
            }
            ClearVehicle();
        }

        private void DgVehicles_SelectionChanged(object sender, EventArgs e)
        {
            if (dgVehicles.SelectedRows.Count == 0) return;
            var row = dgVehicles.SelectedRows[0];
            _selectedVehicleID = Convert.ToInt32(row.Cells["VehicleID"].Value);
            txtVehicleType.Text = row.Cells["VehicleType"].Value?.ToString();
            txtVehicleName.Text = row.Cells["VehicleName"].Value?.ToString();
            txtLicensePlate.Text = row.Cells["LicensePlate"].Value?.ToString();
            txtNotes.Text = row.Cells["Notes"].Value?.ToString();
            chkActive.Checked = row.Cells["IsActive"].Value?.ToString() == "نعم";
        }

        private void BtnSaveVehicle_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtVehicleType.Text)) { MessageBox.Show("أدخل نوع العربية أو المركبة"); return; }
            if (string.IsNullOrWhiteSpace(txtVehicleName.Text)) { MessageBox.Show("أدخل اسم المركبة"); return; }
            int id = VehicleDAL.Save(_selectedVehicleID, txtVehicleType.Text.Trim(), txtVehicleName.Text.Trim(), txtLicensePlate.Text.Trim(), txtNotes.Text.Trim(), chkActive.Checked);
            if (id > 0)
            {
                MessageBox.Show("✅ تم حفظ بيانات المركبة بنجاح!");
                LoadVehicles();
            }
            else
            {
                MessageBox.Show("❌ فشل حفظ بيانات المركبة");
            }
        }

        private void BtnDeleteVehicle_Click(object sender, EventArgs e)
        {
            if (_selectedVehicleID == 0) return;
            if (MessageBox.Show("حذف المركبة؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                VehicleDAL.Delete(_selectedVehicleID);
                LoadVehicles();
            }
        }

        private void ClearVehicle()
        {
            _selectedVehicleID = 0;
            txtVehicleType.Clear();
            txtVehicleName.Clear();
            txtLicensePlate.Clear();
            txtNotes.Clear();
            chkActive.Checked = true;
            dgVehicles.ClearSelection();
        }
    }
}

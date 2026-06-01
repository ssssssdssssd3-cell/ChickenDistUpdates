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

            // Details Panel with absolute positioning and auto-scroll to avoid layout bugs
            var pnlDetails = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Theme.BgCard,
                Padding = new Padding(15),
                AutoScroll = true
            };

            int y = 25;

            // Row 0: نوع العربية
            pnlDetails.Controls.Add(new Label { Text = "نوع العربية:", Location = new Point(230, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtVehicleType = new TextBox { Location = new Point(20, y - 3), Width = 200, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlDetails.Controls.Add(txtVehicleType);
            y += 45;

            // Row 1: اسم المركبة
            pnlDetails.Controls.Add(new Label { Text = "اسم المركبة:", Location = new Point(230, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtVehicleName = new TextBox { Location = new Point(20, y - 3), Width = 200, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlDetails.Controls.Add(txtVehicleName);
            y += 45;

            // Row 2: رقم اللوحة
            pnlDetails.Controls.Add(new Label { Text = "رقم اللوحة:", Location = new Point(230, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtLicensePlate = new TextBox { Location = new Point(20, y - 3), Width = 200, Height = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlDetails.Controls.Add(txtLicensePlate);
            y += 45;

            // Row 3: ملاحظات
            pnlDetails.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(230, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtNotes = new TextBox { Location = new Point(20, y - 3), Width = 200, Height = 80, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlDetails.Controls.Add(txtNotes);
            y += 95;

            // Row 4: نشط
            chkActive = new CheckBox { Text = "المركبة نشطة", Location = new Point(110, y), AutoSize = true, ForeColor = Theme.TextMain, Checked = true, Font = Theme.FontBold };
            pnlDetails.Controls.Add(chkActive);
            y += 45;

            // Buttons: New, Save, Delete
            btnNewVehicle = Theme.MakeButton("🆕 جديد", 240, y, 80, 35, Color.FromArgb(60, 100, 60));
            btnSaveVehicle = Theme.MakeButton("💾 حفظ المركبة", 100, y, 130, 35, Theme.Accent);
            btnDeleteVehicle = Theme.MakeButton("🗑 حذف", 15, y, 75, 35, Color.FromArgb(140, 40, 40));

            btnNewVehicle.Click += (s, e) => ClearVehicle();
            btnSaveVehicle.Click += BtnSaveVehicle_Click;
            btnDeleteVehicle.Click += BtnDeleteVehicle_Click;

            pnlDetails.Controls.AddRange(new Control[] { btnNewVehicle, btnSaveVehicle, btnDeleteVehicle });

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
            pnlTop.BringToFront();
            tbl.SendToBack();
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

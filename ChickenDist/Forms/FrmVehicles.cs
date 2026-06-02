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

            // ====== Title Bar ======
            var pnlTop = Theme.MakeTitleBar("🚗 المركبات", "سجل المركبات وأنواع العربيات للمصروفات والمهام اللوجستية");
            pnlTop.Dock = DockStyle.Top;

            // ====== Main Split: Left = details, Right = grid ======
            var splitMain = new TableLayoutPanel
            {
                Dock      = DockStyle.Fill,
                ColumnCount = 2,
                RowCount   = 1,
                Padding    = new Padding(8),
                BackColor  = Theme.BgMain
            };
            splitMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340f)); // عمود التفاصيل
            splitMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // عمود الجدول

            // ====== Details Panel (TableLayoutPanel بدلاً من absolute) ======
            var pnlDetails = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding   = new Padding(12)
            };

            // نبني TableLayoutPanel للحقول بداخل Panel العادي
            var formTbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                AutoSize    = true,
                ColumnCount = 2,
                RowCount    = 6,
                BackColor   = Theme.BgCard,
                Padding     = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            formTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));   // عمود الليبل
            formTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));   // عمود الإنبوت

            // دالة مساعدة لإضافة صف Label + Control
            Action<string, Control, int> addRow = (labelText, ctrl, rowIdx) =>
            {
                var lbl = new Label
                {
                    Text      = labelText,
                    AutoSize  = false,
                    Dock      = DockStyle.Fill,
                    ForeColor = Theme.TextMain,
                    Font      = Theme.FontBold,
                    TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                    Padding   = new Padding(0, 0, 6, 0)
                };
                ctrl.Dock = DockStyle.Fill;
                formTbl.Controls.Add(lbl,  0, rowIdx);
                formTbl.Controls.Add(ctrl, 1, rowIdx);
                formTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            };

            txtVehicleType   = new TextBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Height = 28 };
            txtVehicleName   = new TextBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Height = 28 };
            txtLicensePlate  = new TextBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Height = 28 };
            txtNotes         = new TextBox { BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Multiline = true, Height = 70 };
            chkActive        = new CheckBox { Text = "نشطة ✅", ForeColor = Theme.TextMain, Font = Theme.FontBold, Checked = true, Dock = DockStyle.Fill };

            formTbl.RowStyles.Clear();
            addRow("نوع العربية:", txtVehicleType,  0);
            addRow("اسم المركبة:", txtVehicleName,  1);
            addRow("رقم اللوحة:", txtLicensePlate, 2);

            // ملاحظات (صف أطول)
            var lblNotes = new Label { Text = "ملاحظات:", AutoSize = false, Dock = DockStyle.Fill, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = System.Drawing.ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 6, 0) };
            txtNotes.Dock = DockStyle.Fill;
            formTbl.Controls.Add(lblNotes, 0, 3);
            formTbl.Controls.Add(txtNotes, 1, 3);
            formTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));

            // صف المربع نشطة
            var lblActive = new Label { Text = "الحالة:", AutoSize = false, Dock = DockStyle.Fill, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = System.Drawing.ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 6, 0) };
            formTbl.Controls.Add(lblActive, 0, 4);
            formTbl.Controls.Add(chkActive, 1, 4);
            formTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            formTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ====== أزرار الحفظ ======
            var pnlBtns = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize      = true,
                BackColor     = Theme.BgCard,
                Padding       = new Padding(0, 10, 0, 0)
            };

            btnNewVehicle    = new Button { Text = "🆕 جديد",         Width = 90,  Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60,100,60),   ForeColor = Color.White, Font = Theme.FontBold, Cursor = Cursors.Hand, Margin = new Padding(0,0,6,0) };
            btnSaveVehicle   = new Button { Text = "💾 حفظ المركبة",   Width = 140, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent,                ForeColor = Color.White, Font = Theme.FontBold, Cursor = Cursors.Hand, Margin = new Padding(0,0,6,0) };
            btnDeleteVehicle = new Button { Text = "🗑 حذف",           Width = 80,  Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(140,40,40),   ForeColor = Color.White, Font = Theme.FontBold, Cursor = Cursors.Hand };
            foreach (var b in new[] { btnNewVehicle, btnSaveVehicle, btnDeleteVehicle })
                b.FlatAppearance.BorderSize = 0;

            btnNewVehicle.Click    += (s, e) => ClearVehicle();
            btnSaveVehicle.Click   += BtnSaveVehicle_Click;
            btnDeleteVehicle.Click += BtnDeleteVehicle_Click;

            pnlBtns.Controls.AddRange(new Control[] { btnNewVehicle, btnSaveVehicle, btnDeleteVehicle });

            formTbl.Dock = DockStyle.Fill;
            pnlDetails.Controls.Add(formTbl);
            pnlDetails.Controls.Add(pnlBtns);

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

            var pnlGrid = new Panel { Dock = DockStyle.Fill };
            pnlGrid.Controls.Add(dgVehicles);

            splitMain.Controls.Add(pnlDetails, 0, 0);
            splitMain.Controls.Add(pnlGrid,    1, 0);

            this.Controls.Add(splitMain);
            this.Controls.Add(pnlTop);
            pnlTop.BringToFront();
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

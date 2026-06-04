using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmWarehouses : Form
    {
        private DataGridView dgWarehouses;
        private TextBox txtName, txtLocation, txtNotes;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete;
        private int _selectedID = 0;

        public FrmWarehouses()
        {
            InitUI();
            LoadWarehouses();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = "إدارة المخازن";
            this.Size = new Size(850, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                SplitterDistance = 320
            };

            // Right Panel: Form Fields (Panel1 in C# RTL is layout-right)
            split.Panel1.BackColor = Theme.BgCard;
            split.Panel1.Padding = new Padding(15);

            int y = 20;
            AddLabel("اسم المخزن:", split.Panel1, y);
            txtName = AddTextBox(split.Panel1, ref y);

            AddLabel("الموقع:", split.Panel1, y);
            txtLocation = AddTextBox(split.Panel1, ref y);

            AddLabel("ملاحظات:", split.Panel1, y);
            txtNotes = AddTextBox(split.Panel1, ref y, true);
            txtNotes.Height = 70;
            y += 40;

            chkActive = new CheckBox
            {
                Text = "مخزن نشط",
                Location = new Point(190, y),
                Size = new Size(100, 24),
                ForeColor = Theme.TextMain,
                Checked = true
            };
            split.Panel1.Controls.Add(chkActive);
            y += 40;

            btnNew = Theme.MakeButton("🆕 جديد", 210, y, 80, 32, Color.FromArgb(60, 100, 60));
            btnSave = Theme.MakeButton("💾 حفظ", 115, y, 85, 32, Theme.Accent);
            btnDelete = Theme.MakeButton("🗑️ إيقاف", 20, y, 85, 32, Color.FromArgb(140, 40, 40));

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            split.Panel1.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });

            // Left Panel: Grid View (Panel2)
            dgWarehouses = new DataGridView
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
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseID", Visible = false });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "اسم المخزن", FillWeight = 100 });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "الموقع", FillWeight = 100 });
            dgWarehouses.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "حالة النشاط", FillWeight = 40 });
            dgWarehouses.SelectionChanged += DgWarehouses_SelectionChanged;

            split.Panel2.Controls.Add(dgWarehouses);

            this.Controls.Add(split);
            Theme.ApplyFormRTL(this);
        }

        private void AddLabel(string text, Panel panel, int y)
        {
            panel.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(190, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            });
        }

        private TextBox AddTextBox(Panel panel, ref int y, bool multiline = false)
        {
            var txt = new TextBox
            {
                Location = new Point(20, y - 2),
                Width = 160,
                Multiline = multiline,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(txt);
            y += multiline ? 45 : 36;
            return txt;
        }

        private void LoadWarehouses()
        {
            dgWarehouses.Rows.Clear();
            DataTable dt = WarehouseDAL.GetAll();
            foreach (DataRow r in dt.Rows)
            {
                bool active = Convert.ToBoolean(r["IsActive"]);
                int ri = dgWarehouses.Rows.Add(r["WarehouseID"], r["WarehouseName"], r["Location"], active ? "✓ نشط" : "✗ متوقف");
                if (!active)
                {
                    dgWarehouses.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
        }

        private void DgWarehouses_SelectionChanged(object sender, EventArgs e)
        {
            if (dgWarehouses.SelectedRows.Count == 0) return;
            _selectedID = Convert.ToInt32(dgWarehouses.SelectedRows[0].Cells["WarehouseID"].Value);
            DataRow r = WarehouseDAL.GetByID(_selectedID);
            if (r == null) return;

            txtName.Text = r["WarehouseName"].ToString();
            txtLocation.Text = r["Location"].ToString();
            txtNotes.Text = r["Notes"].ToString();
            chkActive.Checked = Convert.ToBoolean(r["IsActive"]);

            // منع إيقاف المخزن الرئيسي
            btnDelete.Enabled = (_selectedID != 1);
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtName.Clear();
            txtLocation.Clear();
            txtNotes.Clear();
            chkActive.Checked = true;
            btnDelete.Enabled = false;
            txtName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم المخزن!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = WarehouseDAL.Save(_selectedID, txtName.Text.Trim(), txtLocation.Text.Trim(), txtNotes.Text.Trim(), chkActive.Checked);
            if (id > 0)
            {
                MessageBox.Show("✅ تم حفظ بيانات المخزن بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _selectedID = id;
                LoadWarehouses();
                ClearDetail();
            }
            else
            {
                MessageBox.Show("❌ فشل حفظ المخزن.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0 || _selectedID == 1) return;

            if (MessageBox.Show("هل تريد بالتأكيد إيقاف هذا المخزن؟\nلن تتمكن من استخدامه في العمليات الجديدة.", "تأكيد الإيقاف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                WarehouseDAL.Delete(_selectedID);
                LoadWarehouses();
                ClearDetail();
            }
        }
    }
}

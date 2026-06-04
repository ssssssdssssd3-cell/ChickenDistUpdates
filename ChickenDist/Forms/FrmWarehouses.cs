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
            this.Size = new Size(900, 520);
            this.MinimumSize = new Size(750, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // TableLayoutPanel: عمودان - الأيمن: الجريد | الأيسر: بيانات المخزن
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f)); // جريد
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f)); // فورم

            // ─── الجريد ───
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

            tbl.Controls.Add(dgWarehouses, 0, 0);

            // ─── لوحة بيانات المخزن ───
            var pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(16)
            };

            // عنوان اللوحة
            var lblTitle = new Label
            {
                Text = "بيانات المخزن",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlForm.Controls.Add(lblTitle);

            // حاوية الحقول بـ FlowLayout لتجنب التداخل
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 4, 0, 0)
            };

            flow.Controls.Add(MakeFieldLabel("اسم المخزن:"));
            txtName = MakeFieldTextBox(flow, false);

            flow.Controls.Add(MakeFieldLabel("الموقع:"));
            txtLocation = MakeFieldTextBox(flow, false);

            flow.Controls.Add(MakeFieldLabel("ملاحظات:"));
            txtNotes = MakeFieldTextBox(flow, true);

            chkActive = new CheckBox
            {
                Text = "✔ مخزن نشط",
                AutoSize = false,
                Size = new Size(240, 28),
                Margin = new Padding(0, 8, 0, 8),
                ForeColor = Theme.TextMain,
                Checked = true,
                Font = Theme.FontMain
            };
            flow.Controls.Add(chkActive);

            // أزرار الإجراءات
            var pnlBtns = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 6, 0, 0),
                Width = 250
            };
            btnNew    = MakeActionBtn("🆕 جديد",  Color.FromArgb(55, 110, 55));
            btnSave   = MakeActionBtn("💾 حفظ",   Theme.Accent);
            btnDelete = MakeActionBtn("🗑️ إيقاف", Color.FromArgb(160, 40, 40));
            btnNew.Click    += (s, e) => ClearDetail();
            btnSave.Click   += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            pnlBtns.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });
            flow.Controls.Add(pnlBtns);

            pnlForm.Controls.Add(flow);
            tbl.Controls.Add(pnlForm, 1, 0);

            this.Controls.Add(tbl);
        }

        private Label MakeFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(240, 22),
                Margin = new Padding(0, 6, 0, 2),
                ForeColor = Theme.TextSub,
                Font = Theme.FontMain,
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private TextBox MakeFieldTextBox(FlowLayoutPanel parent, bool multiline)
        {
            var txt = new TextBox
            {
                Width = 240,
                Multiline = multiline,
                Height = multiline ? 65 : 26,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                RightToLeft = RightToLeft.Yes
            };
            parent.Controls.Add(txt);
            return txt;
        }

        private Button MakeActionBtn(string text, Color back)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(80, 32),
                Margin = new Padding(4, 0, 0, 0),
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
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

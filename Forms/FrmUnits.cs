using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmUnits : Form
    {
        private DataGridView dgUnits;
        private TextBox txtName;
        private Button btnNew, btnSave, btnDelete;
        private int _selectedID = 0;

        public FrmUnits()
        {
            InitUI();
            LoadUnits();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = "إدارة الوحدات الثابتة";
            this.Size = new Size(600, 380);
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
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f)); // العمود الأيمن: المدخلات
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // العمود الأيسر: الجدول
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Details Panel (Inputs)
            var pnlDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15)
            };

            int y = 30;
            pnlDetails.Controls.Add(new Label
            {
                Text = "اسم الوحدة:",
                Location = new Point(140, y),
                Width = 100,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            });
            txtName = new TextBox
            {
                Location = new Point(15, y - 2),
                Width = 120,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlDetails.Controls.Add(txtName);
            y += 60;

            btnNew = Theme.MakeButton("🆕 جديد", 175, y, 70, 32, Color.FromArgb(60, 100, 60));
            btnSave = Theme.MakeButton("💾 حفظ", 95, y, 75, 32, Theme.Accent);
            btnDelete = Theme.MakeButton("🗑️ حذف", 15, y, 75, 32, Color.FromArgb(140, 40, 40));

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            pnlDetails.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });

            // Left Panel: Grid
            dgUnits = new DataGridView
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
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitID", Visible = false });
            dgUnits.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "اسم الوحدة", FillWeight = 100 });
            dgUnits.SelectionChanged += DgUnits_SelectionChanged;

            tbl.Controls.Add(pnlDetails, 0, 0); // العمود 0 (اليمين بسبب RTL)
            tbl.Controls.Add(dgUnits, 1, 0); // العمود 1 (اليسار بسبب RTL)

            this.Controls.Add(tbl);
            Theme.ApplyFormRTL(this);
            this.RightToLeftLayout = false;
        }

        private void LoadUnits()
        {
            dgUnits.Rows.Clear();
            DataTable dt = UnitDAL.GetAll();
            foreach (DataRow r in dt.Rows)
            {
                dgUnits.Rows.Add(r["UnitID"], r["UnitName"]);
            }
        }

        private void DgUnits_SelectionChanged(object sender, EventArgs e)
        {
            if (dgUnits.SelectedRows.Count == 0) return;
            _selectedID = Convert.ToInt32(dgUnits.SelectedRows[0].Cells["UnitID"].Value);
            txtName.Text = dgUnits.SelectedRows[0].Cells["UnitName"].Value.ToString();
            btnDelete.Enabled = true;
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtName.Clear();
            btnDelete.Enabled = false;
            txtName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0 && !Session.CanAdd("Units")) { MessageBox.Show("⛔ ليس لديك صلاحية إضافة وحدات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_selectedID > 0 && !Session.CanEdit("Units")) { MessageBox.Show("⛔ ليس لديك صلاحية تعديل الوحدات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الوحدة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = UnitDAL.Save(_selectedID, txtName.Text.Trim());
            if (id > 0)
            {
                MessageBox.Show("✅ تم الحفظ بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _selectedID = id;
                LoadUnits();
                ClearDetail();
            }
            else
            {
                MessageBox.Show("❌ فشل الحفظ. اسم الوحدة قد يكون مكرراً.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;
            if (!Session.CanDelete("Units")) { MessageBox.Show("⛔ ليس لديك صلاحية حذف الوحدات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (MessageBox.Show("هل تريد بالتأكيد حذف هذه الوحدة؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    UnitDAL.Delete(_selectedID);
                    LoadUnits();
                    ClearDetail();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل الحذف. قد تكون الوحدة مستخدمة في بعض الأصناف.\n" + ex.Message, "خطأ الحذف", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

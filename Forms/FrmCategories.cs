using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmCategories : Form
    {
        private DataGridView dgCategories;
        private TextBox txtName;
        private CheckBox chkActive;
        private Button btnNew, btnSave, btnDelete;
        private int _selectedID = 0;

        public FrmCategories()
        {
            InitUI();
            LoadCategories();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = "إدارة التصنيفات / الأقسام";
            this.Size = new Size(700, 420);
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
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f)); // العمود الأيمن: المدخلات
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
                Text = "اسم التصنيف:",
                Location = new Point(160, y),
                Width = 110,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            });
            txtName = new TextBox
            {
                Location = new Point(15, y - 2),
                Width = 135,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlDetails.Controls.Add(txtName);
            y += 45;

            chkActive = new CheckBox
            {
                Text = "نشط",
                Location = new Point(160, y),
                ForeColor = Theme.TextMain,
                Checked = true
            };
            pnlDetails.Controls.Add(chkActive);
            y += 50;

            btnNew = Theme.MakeButton("🆕 جديد", 175, y, 70, 32, Color.FromArgb(60, 100, 60));
            btnSave = Theme.MakeButton("💾 حفظ", 95, y, 75, 32, Theme.Accent);
            btnDelete = Theme.MakeButton("🗑️ إيقاف", 15, y, 75, 32, Color.FromArgb(140, 40, 40));

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            pnlDetails.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });

            // Left Panel: Grid (Panel2)
            dgCategories = new DataGridView
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
            dgCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryID", Visible = false });
            dgCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "اسم القسم / التصنيف", FillWeight = 100 });
            dgCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "الحالة", FillWeight = 40 });
            dgCategories.SelectionChanged += DgCategories_SelectionChanged;

            tbl.Controls.Add(pnlDetails, 0, 0); // العمود 0 (اليمين بسبب RTL)
            tbl.Controls.Add(dgCategories, 1, 0); // العمود 1 (اليسار بسبب RTL)

            this.Controls.Add(tbl);
            Theme.ApplyFormRTL(this);
            this.RightToLeftLayout = false; // تعطيل الانعكاس لتجنب قص اللوحة اليمنى عند التضمين كابن
        }

        private void LoadCategories()
        {
            dgCategories.Rows.Clear();
            DataTable dt = CategoryDAL.GetAll();
            foreach (DataRow r in dt.Rows)
            {
                bool active = Convert.ToBoolean(r["IsActive"]);
                int ri = dgCategories.Rows.Add(r["CategoryID"], r["CategoryName"], active ? "✓ نشط" : "✗ متوقف");
                if (!active)
                {
                    dgCategories.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
        }

        private void DgCategories_SelectionChanged(object sender, EventArgs e)
        {
            if (dgCategories.SelectedRows.Count == 0) return;
            _selectedID = Convert.ToInt32(dgCategories.SelectedRows[0].Cells["CategoryID"].Value);
            DataRow r = CategoryDAL.GetByID(_selectedID);
            if (r == null) return;

            txtName.Text = r["CategoryName"].ToString();
            chkActive.Checked = Convert.ToBoolean(r["IsActive"]);
            btnDelete.Enabled = true;
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtName.Clear();
            chkActive.Checked = true;
            btnDelete.Enabled = false;
            txtName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم التصنيف!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = CategoryDAL.Save(_selectedID, txtName.Text.Trim(), chkActive.Checked);
            if (id > 0)
            {
                MessageBox.Show("✅ تم الحفظ بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _selectedID = id;
                LoadCategories();
                ClearDetail();
            }
            else
            {
                MessageBox.Show("❌ فشل الحفظ.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;

            if (MessageBox.Show("هل تريد بالتأكيد إيقاف هذا القسم؟", "تأكيد الإيقاف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                CategoryDAL.Delete(_selectedID);
                LoadCategories();
                ClearDetail();
            }
        }
    }
}

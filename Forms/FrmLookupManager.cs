using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmLookupManager : Form
    {
        private string _tableName;
        private string _idCol;
        private string _codeCol;
        private string _nameCol;
        private string _prefix;
        private string _title;

        private DataGridView dgItems;
        private TextBox txtName;
        private TextBox txtCode;
        private Button btnNew, btnSave, btnDelete;
        private int _selectedID = 0;

        public FrmLookupManager() : this("Categories", "CategoryID", "CategoryCode", "CategoryName", "CAT-", "الأقسام والتصنيفات")
        {
        }

        public FrmLookupManager(string tableName, string idCol, string codeCol, string nameCol, string prefix, string title)
        {
            _tableName = tableName;
            _idCol = idCol;
            _codeCol = codeCol;
            _nameCol = nameCol;
            _prefix = prefix;
            _title = title;

            InitUI();
            LoadData();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = $"إدارة {_title}";
            this.Size = new Size(650, 420);
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
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f)); 
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var pnlDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15)
            };

            int y = 20;
            pnlDetails.Controls.Add(new Label
            {
                Text = "الكود:",
                Location = new Point(160, y),
                Width = 100,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            });
            txtCode = new TextBox
            {
                Location = new Point(15, y - 2),
                Width = 140,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Text = "تلقائي"
            };
            pnlDetails.Controls.Add(txtCode);
            y += 50;

            pnlDetails.Controls.Add(new Label
            {
                Text = $"الاسم:",
                Location = new Point(160, y),
                Width = 100,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            });
            txtName = new TextBox
            {
                Location = new Point(15, y - 2),
                Width = 140,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlDetails.Controls.Add(txtName);
            y += 70;

            btnNew = Theme.MakeButton("🆕 جديد", 195, y, 70, 32, Color.FromArgb(60, 100, 60));
            btnSave = Theme.MakeButton("💾 حفظ", 105, y, 80, 32, Theme.Accent);
            btnDelete = Theme.MakeButton("🗑️ حذف", 15, y, 80, 32, Color.FromArgb(140, 40, 40));

            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            pnlDetails.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });
            tbl.Controls.Add(pnlDetails, 0, 0);

            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font(Theme.FontMain, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            Theme.EnableDoubleBuffer(dgItems);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "الكود", FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "الاسم", FillWeight = 100 });
            dgItems.SelectionChanged += DgItems_SelectionChanged;

            tbl.Controls.Add(dgItems, 1, 0);
            this.Controls.Add(tbl);

            Theme.ApplyFormRTL(this);
        }

        private void LoadData()
        {
            dgItems.Rows.Clear();
            DataTable dt = LookupDAL.GetAll(_tableName, _nameCol);
            foreach (DataRow r in dt.Rows)
            {
                dgItems.Rows.Add(r[_idCol], r[_codeCol], r[_nameCol]);
            }
        }

        private void DgItems_SelectionChanged(object sender, EventArgs e)
        {
            if (dgItems.SelectedRows.Count == 0) return;
            _selectedID = Convert.ToInt32(dgItems.SelectedRows[0].Cells["ID"].Value);
            txtCode.Text = dgItems.SelectedRows[0].Cells["Code"].Value?.ToString() ?? "";
            txtName.Text = dgItems.SelectedRows[0].Cells["Name"].Value?.ToString() ?? "";
            btnDelete.Enabled = true;
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtCode.Text = "تلقائي";
            txtName.Clear();
            btnDelete.Enabled = false;
            txtName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("يرجى إدخال الاسم!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = LookupDAL.Save(_tableName, _idCol, _codeCol, _nameCol, _prefix, _selectedID, txtName.Text.Trim());
            if (id > 0)
            {
                MessageBox.Show("✅ تم الحفظ بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _selectedID = id;
                LoadData();
                ClearDetail();
            }
            else
            {
                MessageBox.Show("❌ فشل الحفظ. قد يكون الاسم مكرراً.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;

            if (MessageBox.Show("هل تريد بالتأكيد حذف هذا العنصر؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    LookupDAL.Delete(_tableName, _idCol, _selectedID);
                    LoadData();
                    ClearDetail();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل الحذف. قد يكون العنصر مستخدماً في النظام.\n" + ex.Message, "خطأ الحذف", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

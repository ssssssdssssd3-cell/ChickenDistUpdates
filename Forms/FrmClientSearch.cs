using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة البحث المتقدم والسهل عن عميل</summary>
    public class FrmClientSearch : Form
    {
        private TextBox txtSearch;
        private ComboBox cboSearchType;
        private DataGridView dgClients;
        private Button btnSelect, btnCancel;
        private DataTable _dtClients;
        private DataView _dvClients;

        public int SelectedClientID { get; private set; } = 0;
        public string SelectedClientName { get; private set; } = "";
        public string SelectedClientPhone { get; private set; } = "";

        public FrmClientSearch()
        {
            InitUI();
            LoadClients();
        }

        private void InitUI()
        {
            this.Text = "🔍 بحث عن عميل";
            this.Size = new Size(860, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            // لون مميز لشاشة البحث - بترولي/كحلي فاخر يختلف عن الشاشات العادية
            this.BackColor = Color.FromArgb(15, 30, 50);
            this.Font = Theme.FontMain;

            // Search Header Panel
            var pnlSearch = new Panel
            {
                Name = "pnlSearch",
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgSearchPanel,
                RightToLeft = RightToLeft.No,
                Padding = new Padding(12)
            };

            var lblSearch = new Label
            {
                Text = "🔍 نوع البحث:",
                Location = new Point(690, 22),
                Width = 130,
                ForeColor = Color.FromArgb(255, 220, 110),
                TextAlign = ContentAlignment.MiddleRight,
                Font = Theme.FontBold
            };

            cboSearchType = new ComboBox
            {
                Location = new Point(510, 18),
                Width = 175,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                RightToLeft = RightToLeft.Yes
            };
            cboSearchType.Items.AddRange(new object[] { "🔍 الكل (شامل)", "👤 بالاسم", "📞 برقم الهاتف", "🔢 بالكود" });
            cboSearchType.SelectedIndex = 0;
            cboSearchType.SelectedIndexChanged += (s, e) => ApplyFilter();

            txtSearch = new TextBox
            {
                Location = new Point(20, 18),
                Width = 475,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 12f),
                RightToLeft = RightToLeft.Yes,
                TextAlign = HorizontalAlignment.Left
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            txtSearch.KeyDown += TxtSearch_KeyDown;

            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(cboSearchType);
            pnlSearch.Controls.Add(txtSearch);
            Theme.StyleSearchPanel(pnlSearch);

            // DataGridView Panel
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 0, 12, 0),
                BackColor = Color.FromArgb(15, 30, 50)
            };

            dgClients = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.FromArgb(245, 247, 250),
                ForeColor = Color.FromArgb(20, 25, 35),
                GridColor = Color.FromArgb(220, 225, 235),
                BorderStyle = BorderStyle.None,
                RowTemplate = { Height = 32 },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(20, 25, 35),
                    SelectionBackColor = Color.FromArgb(0, 120, 215),
                    SelectionForeColor = Color.White
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(240, 243, 248),
                    ForeColor = Color.FromArgb(20, 25, 35),
                    SelectionBackColor = Color.FromArgb(0, 120, 215),
                    SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(24, 43, 73),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            dgClients.CellDoubleClick += (s, e) => SelectClient();
            dgClients.KeyDown += DgClients_KeyDown;
            pnlGrid.Controls.Add(dgClients);

            // Footer Panel (Buttons)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(10, 22, 38)
            };

            btnCancel = Theme.MakeButton("❌ إلغاء", 20, 14, 100, 34, Color.FromArgb(140, 40, 40));
            btnCancel.Click += (s, e) => this.Close();

            btnSelect = Theme.MakeButton("✅ اختيار", 130, 14, 120, 34, Theme.Accent);
            btnSelect.Click += (s, e) => SelectClient();

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSelect);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlSearch);
            this.Controls.Add(pnlFooter);

            txtSearch.Select();
        }

        private void LoadClients()
        {
            try
            {
                _dtClients = ClientDAL.GetAll(activeOnly: true);
                _dvClients = new DataView(_dtClients);
                dgClients.DataSource = _dvClients;

                // Hide all columns by default
                foreach (DataGridViewColumn col in dgClients.Columns)
                {
                    col.Visible = false;
                }

                // Explicitly show only the required columns with correct display order
                if (dgClients.Columns.Contains("ClientCode"))
                {
                    var col = dgClients.Columns["ClientCode"];
                    col.Visible = true;
                    col.HeaderText = "كود العميل";
                    col.Width = 90;
                    col.DisplayIndex = 0;
                }
                if (dgClients.Columns.Contains("ClientName"))
                {
                    var col = dgClients.Columns["ClientName"];
                    col.Visible = true;
                    col.HeaderText = "اسم العميل / المؤسسة";
                    col.Width = 230;
                    col.DisplayIndex = 1;
                    col.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }
                if (dgClients.Columns.Contains("Phone"))
                {
                    var col = dgClients.Columns["Phone"];
                    col.Visible = true;
                    col.HeaderText = "رقم الهاتف";
                    col.Width = 120;
                    col.DisplayIndex = 2;
                }
                if (dgClients.Columns.Contains("Phone2"))
                {
                    var col = dgClients.Columns["Phone2"];
                    col.Visible = true;
                    col.HeaderText = "الهاتف 2";
                    col.Width = 110;
                    col.DisplayIndex = 3;
                }
                if (dgClients.Columns.Contains("Address"))
                {
                    var col = dgClients.Columns["Address"];
                    col.Visible = true;
                    col.HeaderText = "العنوان";
                    col.Width = 160;
                    col.DisplayIndex = 4;
                }
                if (dgClients.Columns.Contains("Balance"))
                {
                    var col = dgClients.Columns["Balance"];
                    col.Visible = true;
                    col.HeaderText = "الرصيد الحالي (ج)";
                    col.Width = 120;
                    col.DisplayIndex = 5;
                    col.DefaultCellStyle.Format = "N2";
                    col.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }

                // Grid coloring & styling - نطبق رأس الجدول فقط بدون كتابة فوق ألوان السطور المتبادلة
                Theme.StyleGridHeader(dgClients);
                // نُعيد تطبيق ألوان السطور المتبادلة لأن StyleGridHeader قد يؤثر عليها
                dgClients.DefaultCellStyle.BackColor = Color.FromArgb(22, 42, 65);
                dgClients.DefaultCellStyle.ForeColor = Color.FromArgb(220, 235, 255);
                dgClients.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 150, 200);
                dgClients.DefaultCellStyle.SelectionForeColor = Color.White;
                dgClients.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(14, 28, 46);
                dgClients.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(220, 235, 255);
                dgClients.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 150, 200);
                dgClients.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل العملاء: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_dvClients == null) return;
            string txt = txtSearch.Text.Trim().Replace("'", "''");
            if (string.IsNullOrEmpty(txt))
            {
                _dvClients.RowFilter = "";
            }
            else
            {
                int filterType = cboSearchType != null ? cboSearchType.SelectedIndex : 0;
                switch (filterType)
                {
                    case 1: // بالاسم
                        _dvClients.RowFilter = string.Format("ClientName LIKE '%{0}%'", txt);
                        break;
                    case 2: // برقم الهاتف
                        _dvClients.RowFilter = string.Format("Phone LIKE '%{0}%' OR Phone2 LIKE '%{0}%'", txt);
                        break;
                    case 3: // بالكود
                        _dvClients.RowFilter = string.Format("ClientCode LIKE '%{0}%'", txt);
                        break;
                    default: // الكل (شامل)
                        _dvClients.RowFilter = string.Format(
                            "ClientName LIKE '%{0}%' OR Phone LIKE '%{0}%' OR Phone2 LIKE '%{0}%' OR ClientCode LIKE '%{0}%' OR Address LIKE '%{0}%'",
                            txt
                        );
                        break;
                }
            }
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && dgClients.Rows.Count > 0)
            {
                dgClients.Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                SelectClient();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void DgClients_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SelectClient();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void SelectClient()
        {
            if (dgClients.CurrentRow != null)
            {
                var row = ((DataRowView)dgClients.CurrentRow.DataBoundItem).Row;
                SelectedClientID = Convert.ToInt32(row["ClientID"]);
                SelectedClientName = row["ClientName"]?.ToString() ?? "";
                SelectedClientPhone = row["Phone"]?.ToString() ?? "";
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("الرجاء اختيار عميل أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

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
        private DataGridView dgClients;
        private Button btnSelect, btnCancel;
        private DataTable _dtClients;
        private DataView _dvClients;

        public int SelectedClientID { get; private set; } = 0;

        public FrmClientSearch()
        {
            InitUI();
            LoadClients();
        }

        private void InitUI()
        {
            this.Text = "🔍 بحث عن عميل";
            this.Size = new Size(700, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Search Header Panel
            var pnlSearch = new Panel
            {
                Name = "pnlSearch",
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(12)
            };

            var lblSearch = new Label
            {
                Text = "ابحث بالاسم أو الهاتف أو الكود:",
                Location = new Point(480, 22),
                Width = 180,
                ForeColor = Color.FromArgb(255, 220, 110),
                TextAlign = ContentAlignment.MiddleRight,
                Font = Theme.FontBold
            };

            txtSearch = new TextBox
            {
                Location = new Point(20, 18),
                Width = 450,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 12f)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            txtSearch.KeyDown += TxtSearch_KeyDown;

            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(txtSearch);
            Theme.StyleSearchPanel(pnlSearch);

            // DataGridView Panel
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 0, 12, 0)
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
                BackgroundColor = Theme.BgCard,
                ForeColor = Theme.TextMain,
                GridColor = Color.FromArgb(230, 230, 235),
                BorderStyle = BorderStyle.None,
                RowTemplate = { Height = 28 }
            };
            dgClients.CellDoubleClick += (s, e) => SelectClient();
            dgClients.KeyDown += DgClients_KeyDown;
            pnlGrid.Controls.Add(dgClients);

            // Footer Panel (Buttons)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(12)
            };

            btnCancel = Theme.MakeButton("❌ إلغاء", 20, 14, 90, 32, Color.FromArgb(140, 40, 40));
            btnCancel.Click += (s, e) => this.Close();

            btnSelect = Theme.MakeButton("✅ اختيار", 120, 14, 100, 32, Theme.Accent);
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

                // Setup Grid Columns
                if (dgClients.Columns.Contains("ClientID")) dgClients.Columns["ClientID"].Visible = false;
                if (dgClients.Columns.Contains("IsActive")) dgClients.Columns["IsActive"].Visible = false;
                if (dgClients.Columns.Contains("DriverID")) dgClients.Columns["DriverID"].Visible = false;
                if (dgClients.Columns.Contains("MaxCreditLimit")) dgClients.Columns["MaxCreditLimit"].Visible = false;
                if (dgClients.Columns.Contains("Notes")) dgClients.Columns["Notes"].Visible = false;
                if (dgClients.Columns.Contains("DefaultPriceTier")) dgClients.Columns["DefaultPriceTier"].Visible = false;
                if (dgClients.Columns.Contains("OpeningBalance")) dgClients.Columns["OpeningBalance"].Visible = false;
                if (dgClients.Columns.Contains("OpeningCrates")) dgClients.Columns["OpeningCrates"].Visible = false;
                if (dgClients.Columns.Contains("CratesBalance")) dgClients.Columns["CratesBalance"].Visible = false;

                if (dgClients.Columns.Contains("ClientCode"))
                {
                    dgClients.Columns["ClientCode"].HeaderText = "كود العميل";
                    dgClients.Columns["ClientCode"].Width = 90;
                }
                if (dgClients.Columns.Contains("ClientName"))
                {
                    dgClients.Columns["ClientName"].HeaderText = "اسم العميل";
                    dgClients.Columns["ClientName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
                if (dgClients.Columns.Contains("Phone"))
                {
                    dgClients.Columns["Phone"].HeaderText = "رقم الهاتف";
                    dgClients.Columns["Phone"].Width = 110;
                }
                if (dgClients.Columns.Contains("Phone2"))
                {
                    dgClients.Columns["Phone2"].HeaderText = "الهاتف 2";
                    dgClients.Columns["Phone2"].Width = 110;
                }
                if (dgClients.Columns.Contains("Address"))
                {
                    dgClients.Columns["Address"].HeaderText = "العنوان";
                    dgClients.Columns["Address"].Width = 150;
                }
                if (dgClients.Columns.Contains("Balance"))
                {
                    dgClients.Columns["Balance"].HeaderText = "الرصيد الحالي";
                    dgClients.Columns["Balance"].Width = 100;
                    dgClients.Columns["Balance"].DefaultCellStyle.Format = "N2";
                    dgClients.Columns["Balance"].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }

                // Grid coloring & styling
                dgClients.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
                Theme.StyleGrid(dgClients);
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
                _dvClients.RowFilter = string.Format(
                    "ClientName LIKE '%{0}%' OR Phone LIKE '%{0}%' OR Phone2 LIKE '%{0}%' OR ClientCode LIKE '%{0}%' OR Address LIKE '%{0}%'",
                    txt
                );
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

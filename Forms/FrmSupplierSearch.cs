using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة البحث المتقدم عن مورد</summary>
    public class FrmSupplierSearch : Form
    {
        private TextBox txtSearch;
        private DataGridView dgSuppliers;
        private Button btnSelect, btnCancel;
        private DataTable _dtSuppliers;
        private DataView _dvSuppliers;

        public int SelectedSupplierID { get; private set; } = 0;

        public FrmSupplierSearch()
        {
            InitUI();
            LoadSuppliers();
        }

        private void InitUI()
        {
            this.Text = "بحث عن مورد";
            this.Size = new Size(820, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Theme.BgSearchPanel, RightToLeft = RightToLeft.No, Padding = new Padding(12) };
            var lblSearch = new Label { Text = "ابحث بالاسم او الهاتف او الكود:", Location = new Point(590, 22), Width = 210, ForeColor = Color.FromArgb(255, 220, 110), TextAlign = ContentAlignment.MiddleRight, Font = Theme.FontBold };
            txtSearch = new TextBox { Location = new Point(20, 18), Width = 560, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12f), RightToLeft = RightToLeft.Yes, TextAlign = HorizontalAlignment.Left };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            txtSearch.KeyDown += TxtSearch_KeyDown;
            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(txtSearch);
            Theme.StyleSearchPanel(pnlSearch);

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 0) };
            dgSuppliers = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false, BackgroundColor = Theme.BgCard, ForeColor = Theme.TextMain, GridColor = Color.FromArgb(230, 230, 235), BorderStyle = BorderStyle.None, RowTemplate = { Height = 32 } };
            dgSuppliers.CellDoubleClick += (s, e) => SelectSupplier();
            dgSuppliers.KeyDown += DgSuppliers_KeyDown;
            pnlGrid.Controls.Add(dgSuppliers);

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(12) };
            btnCancel = Theme.MakeButton("الغاء", 20, 14, 100, 34, Color.FromArgb(140, 40, 40));
            btnCancel.Click += (s, e) => this.Close();
            btnSelect = Theme.MakeButton("اختيار", 130, 14, 120, 34, Theme.Accent);
            btnSelect.Click += (s, e) => SelectSupplier();
            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSelect);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlSearch);
            this.Controls.Add(pnlFooter);
            txtSearch.Select();
        }

        private void LoadSuppliers()
        {
            try
            {
                _dtSuppliers = SupplierDAL.GetAll(activeOnly: true);
                _dvSuppliers = new DataView(_dtSuppliers);
                dgSuppliers.DataSource = _dvSuppliers;
                foreach (DataGridViewColumn col in dgSuppliers.Columns) col.Visible = false;
                ShowCol("SupplierCode", "كود المورد", 80, 0);
                ShowCol("SupplierName", "اسم المورد", 230, 1, true);
                ShowCol("Phone", "رقم الهاتف", 120, 2);
                ShowCol("Address", "العنوان", 160, 3);
                ShowCol("Balance", "الرصيد المستحق (ج)", 130, 4, true, "N2");
                dgSuppliers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
                Theme.StyleGrid(dgSuppliers);
            }
            catch (Exception ex) { MessageBox.Show("خطا: " + ex.Message); }
        }

        private void ShowCol(string name, string header, int width, int di, bool bold = false, string fmt = null)
        {
            if (!dgSuppliers.Columns.Contains(name)) return;
            var c = dgSuppliers.Columns[name];
            c.Visible = true; c.HeaderText = header; c.Width = width; c.DisplayIndex = di;
            if (bold) c.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            if (fmt != null) c.DefaultCellStyle.Format = fmt;
        }

        private void ApplyFilter()
        {
            if (_dvSuppliers == null) return;
            string txt = txtSearch.Text.Trim().Replace("'", "''");
            _dvSuppliers.RowFilter = string.IsNullOrEmpty(txt) ? "" : string.Format("SupplierName LIKE '%{0}%' OR Phone LIKE '%{0}%' OR SupplierCode LIKE '%{0}%' OR Address LIKE '%{0}%'", txt);
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && dgSuppliers.Rows.Count > 0) { dgSuppliers.Focus(); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter) { SelectSupplier(); e.Handled = true; e.SuppressKeyPress = true; }
        }

        private void DgSuppliers_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { SelectSupplier(); e.Handled = true; e.SuppressKeyPress = true; }
        }

        private void SelectSupplier()
        {
            if (dgSuppliers.CurrentRow != null)
            {
                var row = ((DataRowView)dgSuppliers.CurrentRow.DataBoundItem).Row;
                SelectedSupplierID = Convert.ToInt32(row["SupplierID"]);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else { MessageBox.Show("الرجاء اختيار مورد اولا", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}

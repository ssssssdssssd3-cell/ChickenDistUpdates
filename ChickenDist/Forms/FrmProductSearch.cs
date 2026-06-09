using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة بحث متقدم عن الأصناف</summary>
    public class FrmProductSearch : Form
    {
        private TextBox txtSearch;
        private DataGridView dgProducts;
        private Button btnSelect, btnCancel;
        private DataTable _dtProducts;
        private DataView _dvProducts;

        public int SelectedProductID { get; private set; } = 0;

        public FrmProductSearch()
        {
            InitUI();
            LoadProducts();
        }

        private void InitUI()
        {
            this.Text = "🔍 بحث عن صنف";
            this.Size = new Size(720, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            // Top panel (Search)
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.BgCard, Padding = new Padding(12) };
            var lblSearch = new Label { Text = "ابحث بالاسم أو الكود :", Location = new Point(440, 20), AutoSize = true, ForeColor = Theme.TextMain };
            txtSearch = new TextBox { Location = new Point(20, 16), Width = 400, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11) };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            txtSearch.KeyDown += TxtSearch_KeyDown;
            pnlSearch.Controls.AddRange(new Control[] { lblSearch, txtSearch });

            // Grid Panel
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            dgProducts = new DataGridView
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
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 25 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 55 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 18 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 25 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQty", HeaderText = "الرصيد الفعلي", FillWeight = 27 });
            
            dgProducts.DoubleClick += DgProducts_DoubleClick;
            dgProducts.KeyDown += DgProducts_KeyDown;
            pnlGrid.Controls.Add(dgProducts);

            // Bottom panel (Actions)
            var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(10) };
            btnSelect = Theme.MakeButton("✅ اختيار", 470, 10, 100, 32, Theme.Accent);
            btnCancel = Theme.MakeButton("❌ إلغاء", 360, 10, 100, 32, Color.FromArgb(120, 40, 40));
            
            btnSelect.Click += BtnSelect_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            pnlActions.Controls.AddRange(new Control[] { btnSelect, btnCancel });

            // Add in docking Z-order
            this.Controls.Add(pnlGrid);      // Fill
            this.Controls.Add(pnlActions);   // Bottom
            this.Controls.Add(pnlSearch);    // Top

            Theme.ApplyFormRTL(this);
        }

        private void LoadProducts()
        {
            _dtProducts = ProductDAL.GetAll(true);
            _dvProducts = new DataView(_dtProducts);
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgProducts.Rows.Clear();
            foreach (DataRowView drv in _dvProducts)
            {
                var row = drv.Row;
                int pid = Convert.ToInt32(row["ProductID"]);
                decimal stock = InventoryDAL.GetProductStock(pid);
                int rowIdx = dgProducts.Rows.Add(row["ProductID"], row["ProductCode"], row["ProductName"], row["Unit"],
                    Convert.ToDecimal(row["SalePrice"]).ToString("F2"), stock.ToString("F2"));
                // تلوين الرصيد: أحمر إذا صفر، برتقالي إذا أقل من 10، أخضر إذا طبيعي
                var cell = dgProducts.Rows[rowIdx].Cells["StockQty"];
                if (stock <= 0)
                    cell.Style.ForeColor = System.Drawing.Color.FromArgb(220, 70, 70);
                else if (stock < 10)
                    cell.Style.ForeColor = System.Drawing.Color.FromArgb(220, 150, 40);
                else
                    cell.Style.ForeColor = System.Drawing.Color.FromArgb(60, 190, 100);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string term = txtSearch.Text.Trim().Replace("'", "''");
            if (string.IsNullOrEmpty(term))
            {
                _dvProducts.RowFilter = "";
            }
            else
            {
                _dvProducts.RowFilter = $"ProductName LIKE '%{term}%' OR ProductCode LIKE '%{term}%'";
            }
            RefreshGrid();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (dgProducts.Rows.Count > 0)
                {
                    dgProducts.Focus();
                    dgProducts.CurrentCell = dgProducts.Rows[0].Cells[1];
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (dgProducts.Rows.Count > 0)
                {
                    SelectAndClose();
                    e.Handled = true;
                }
            }
        }

        private void DgProducts_DoubleClick(object sender, EventArgs e)
        {
            SelectAndClose();
        }

        private void DgProducts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SelectAndClose();
                e.Handled = true;
            }
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            SelectAndClose();
        }

        private void SelectAndClose()
        {
            if (dgProducts.SelectedRows.Count == 0) return;
            SelectedProductID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

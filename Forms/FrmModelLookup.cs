using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة فحص وتتبع ألوان ومقاسات الموديل والكميات المتاحة بالمخزن والستير</summary>
    public class FrmModelLookup : Form
    {
        private TextBox txtSearch;
        private DataGridView dgVariants;
        private Button btnSelect;
        private Button btnClose;

        public int SelectedProductID { get; private set; } = 0;
        public string SelectedProductName { get; private set; } = "";
        public decimal SelectedPrice { get; private set; } = 0m;

        public FrmModelLookup(string initialSearch = "")
        {
            InitializeComponentCustom(initialSearch);
        }

        private void InitializeComponentCustom(string initialSearch)
        {
            this.Text = "👗 فحص ألوان ومقاسات الموديل (الرصيد المتاح)";
            this.Size = new Size(780, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlTop = Theme.MakeTitleBar("👗 فحص ألوان ومقاسات الموديل", "ادخل اسم الموديل أو امسح الباركود للتحقق من كافة الألوان والمقاسات المتاحة ورصيدها بالمخزن.");
            this.Controls.Add(pnlTop);

            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            var lbl = new Label { Text = "🔍 اسم الموديل / الباركود:", AutoSize = true, Location = new Point(610, 16), ForeColor = Theme.TextMain, Font = Theme.FontBold };
            txtSearch = new TextBox
            {
                Location = new Point(230, 12),
                Width = 370,
                Font = new Font("Segoe UI", 11f),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Text = initialSearch
            };
            txtSearch.TextChanged += (s, e) => SearchVariants();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SearchVariants(); e.Handled = true; } };

            var btnSearch = Theme.MakeButton("بحث", 130, 10, 90, 32, Theme.Primary);
            btnSearch.Click += (s, e) => SearchVariants();

            pnlSearch.Controls.Add(lbl);
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(btnSearch);
            this.Controls.Add(pnlSearch);

            dgVariants = new DataGridView
            {
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeight = 35,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };
            dgVariants.Dock = DockStyle.Fill;
            dgVariants.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgVariants.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الموديل / القطعة", FillWeight = 130 });
            dgVariants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Brand", HeaderText = "اللون / الماركة", FillWeight = 85 });
            dgVariants.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductSize", HeaderText = "المقاس", FillWeight = 75 });
            dgVariants.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الباركود / الكود", FillWeight = 90 });
            dgVariants.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "السعر", FillWeight = 65 });
            dgVariants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المتاحة", FillWeight = 75 });
            dgVariants.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLocation", HeaderText = "مكان العرض", FillWeight = 75 });

            dgVariants.DoubleClick += (s, e) => SelectAndClose();

            this.Controls.Add(dgVariants);

            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnSelect = Theme.MakeButton("✔️ اختيار الصنف المحدد للبيع", 15, 10, 220, 36, Theme.Success);
            btnSelect.Click += (s, e) => SelectAndClose();

            btnClose = Theme.MakeButton("إغلاق", 245, 10, 100, 36, Color.FromArgb(100, 110, 120));
            btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(btnSelect);
            pnlFooter.Controls.Add(btnClose);
            this.Controls.Add(pnlFooter);

            pnlTop.SendToBack();
            pnlSearch.SendToBack();
            pnlFooter.SendToBack();
            dgVariants.BringToFront();

            if (!string.IsNullOrWhiteSpace(initialSearch))
            {
                SearchVariants();
            }
        }

        private void SearchVariants()
        {
            dgVariants.Rows.Clear();
            string q = txtSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(q)) return;

            string querySql = @"
                SELECT ProductID, ProductName, COALESCE(Brand, '') AS Brand, COALESCE(ProductSize, '') AS ProductSize, ProductCode, SalePrice, COALESCE(ShelfLocation, '') AS ShelfLocation
                FROM Products
                WHERE IsActive = 1 
                  AND (ProductName LIKE @q OR ProductCode LIKE @q OR InternationalCode LIKE @q OR Brand LIKE @q OR ProductSize LIKE @q OR ShelfLocation LIKE @q)
                ORDER BY ProductName, ProductCode";

            var dt = DbHelper.Query(querySql, DbHelper.P("@q", "%" + q + "%"));
            foreach (DataRow r in dt.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                decimal qty = InventoryDAL.GetProductStock(pid);
                int ri = dgVariants.Rows.Add(
                    r["ProductID"],
                    r["ProductName"],
                    r["Brand"],
                    r["ProductSize"],
                    r["ProductCode"],
                    Convert.ToDecimal(r["SalePrice"]).ToString("N2") + " ج",
                    qty.ToString("N2"),
                    r["ShelfLocation"]
                );

                if (qty <= 0)
                {
                    dgVariants.Rows[ri].DefaultCellStyle.ForeColor = Color.Red;
                }
                else
                {
                    dgVariants.Rows[ri].DefaultCellStyle.ForeColor = Theme.TextMain;
                }
            }
        }

        private void SelectAndClose()
        {
            if (dgVariants.SelectedRows.Count == 0) return;
            var row = dgVariants.SelectedRows[0];
            SelectedProductID = Convert.ToInt32(row.Cells["ProductID"].Value);
            SelectedProductName = row.Cells["ProductName"].Value.ToString();
            string priceStr = row.Cells["SalePrice"].Value.ToString().Replace("ج", "").Trim();
            if (decimal.TryParse(priceStr, out decimal p)) SelectedPrice = p;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

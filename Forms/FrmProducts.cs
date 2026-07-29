using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmProducts : Form
    {
        private DataGridView dgProducts;
        private TextBox txtSearch;
        private ComboBox cboCategory, cboStatus;
        private Label lblItemCount, lblSearch, lblCat, lblStatus;
        private Button btnNew, btnEdit, btnDelete;
        private int _selectedID = 0;
        private DataTable _dtProducts;

        public FrmProducts()
        {
            InitUI();
            LoadProducts();
        }

        private void InitUI()
        {
            this.Text = "إدارة الأصناف";
            this.Size = new Size(1150, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Title Bar
            var titleBar = Theme.MakeTitleBar("إدارة الأصناف", "قائمة عرض وبحث الأصناف والأسعار وتعديلها عبر كارت الصنف");
            this.Controls.Add(titleBar);

            // Search Panel (Top) - Right-aligned controls for RTL
            var pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 58, 
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8)
            };

            lblSearch = new Label 
            { 
                Text = "🔍 بحث سريع:", 
                AutoSize = true, 
                ForeColor = Theme.TextMain, 
                Font = Theme.FontBold
            };
            
            txtSearch = new TextBox 
            { 
                Width = 230, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                BorderStyle = BorderStyle.FixedSingle, 
                Font = Theme.FontNormal
            };
            txtSearch.TextChanged += (s, e) => FilterProducts();

            lblCat = new Label 
            { 
                Text = "📂 التصنيف:", 
                AutoSize = true, 
                ForeColor = Theme.TextMain, 
                Font = Theme.FontBold
            };

            cboCategory = new ComboBox
            {
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontNormal
            };
            LoadCategoriesCombo();

            lblStatus = new Label 
            { 
                Text = "⚡ الحالة:", 
                AutoSize = true, 
                ForeColor = Theme.TextMain, 
                Font = Theme.FontBold
            };

            cboStatus = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontNormal
            };
            cboStatus.Items.AddRange(new object[] { "جميع الأصناف", "النشطة فقط", "المعطلة فقط" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => FilterProducts();

            lblItemCount = new Label
            {
                Text = "عدد الأصناف: 0",
                AutoSize = true,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblCat, cboCategory, lblStatus, cboStatus, lblItemCount });

            Action layoutHeader = () =>
            {
                if (pnlHeader.ClientSize.Width <= 0) return;
                int currentX = pnlHeader.ClientSize.Width - 15;

                lblSearch.Location = new Point(currentX - lblSearch.PreferredWidth, 18);
                currentX -= (lblSearch.PreferredWidth + 6);

                txtSearch.Location = new Point(currentX - txtSearch.Width, 14);
                currentX -= (txtSearch.Width + 22);

                lblCat.Location = new Point(currentX - lblCat.PreferredWidth, 18);
                currentX -= (lblCat.PreferredWidth + 6);

                cboCategory.Location = new Point(currentX - cboCategory.Width, 14);
                currentX -= (cboCategory.Width + 22);

                lblStatus.Location = new Point(currentX - lblStatus.PreferredWidth, 18);
                currentX -= (lblStatus.PreferredWidth + 6);

                cboStatus.Location = new Point(currentX - cboStatus.Width, 14);

                lblItemCount.Location = new Point(15, 18);
            };

            pnlHeader.Resize += (s, e) => layoutHeader();
            pnlHeader.HandleCreated += (s, e) => layoutHeader();
            this.Controls.Add(pnlHeader);

            // Footer Panel (Bottom FlowLayoutPanel)
            var pnlFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            btnNew = Theme.MakeButton("➕ إضافة صنف جديد", Theme.Success);
            btnNew.Width = 145;
            btnNew.Click += (s, e) => {
                if (!Session.CanAdd("Products"))
                {
                    MessageBox.Show("❌ عفوًا: لا تملك صلاحية إضافة أصناف جديدة!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (new FrmProductCard(0).ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            };

            btnEdit = Theme.MakeButton("📝 تعديل ومعاينة", Theme.Accent);
            btnEdit.Width = 145;
            btnEdit.Click += BtnEdit_Click;

            btnDelete = Theme.MakeButton("🗑 إيقاف الصنف", Theme.Danger);
            btnDelete.Width = 120;
            btnDelete.Click += BtnDelete_Click;

            var btnQuickAdd = Theme.MakeButton("⚡ إدخال سريع", Color.FromArgb(60, 100, 60));
            btnQuickAdd.Width = 125;
            btnQuickAdd.Click += (s, e) => {
                if (new FrmQuickAdd().ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            };

            var btnImportExcel = Theme.MakeButton("📥 استيراد إكسل", Theme.Primary);
            btnImportExcel.Width = 125;
            btnImportExcel.Click += (s, e) => {
                if (!PromptImportPassword(this)) return;
                if (new FrmImportProducts().ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            };

            var btnPrintBarcode = Theme.MakeButton("🏷️ طباعة الباركود", Theme.Primary);
            btnPrintBarcode.Width = 135;
            btnPrintBarcode.Click += BtnPrintBarcode_Click;

            var btnPricePoster = Theme.MakeButton("📢 منشور الأسعار", Color.FromArgb(120, 80, 140));
            btnPricePoster.Width = 135;
            btnPricePoster.Click += (s, e) => new FrmPricePoster().ShowDialog();

            pnlFooter.Controls.AddRange(new Control[] { 
                btnNew, 
                btnEdit, 
                btnDelete, 
                btnQuickAdd, 
                btnImportExcel, 
                btnPrintBarcode, 
                btnPricePoster 
            });
            this.Controls.Add(pnlFooter);

            // Grid (Center)
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
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 30 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartNumber", HeaderText = "رقم القطعة", FillWeight = 30 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 320 }); // زيادة مساحة اسم الصنف لتأخذ معظم عرض الجدول
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 40 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 25 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 30 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "نشط", FillWeight = 18 });
            
            Theme.AdjustGridHeaders(dgProducts);

            dgProducts.SelectionChanged += DgProducts_SelectionChanged;
            dgProducts.CellDoubleClick += (s, e) => {
                if (dgProducts.SelectedRows.Count > 0)
                {
                    int productID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
                    if (new FrmProductCard(productID).ShowDialog() == DialogResult.OK)
                    {
                        LoadProducts();
                    }
                }
            };

            this.Controls.Add(dgProducts);

            // Send title bar to back so layout docking works correctly
            titleBar.SendToBack();
            pnlHeader.SendToBack();
            pnlFooter.SendToBack();
            dgProducts.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private void LoadCategoriesCombo()
        {
            cboCategory.Items.Clear();
            cboCategory.Items.Add(new ComboItem(0, "جميع التصنيفات"));
            try
            {
                DataTable dtCat = CategoryDAL.GetAll(true);
                foreach (DataRow r in dtCat.Rows)
                {
                    cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                }
            }
            catch { }
            cboCategory.SelectedIndex = 0;
            cboCategory.SelectedIndexChanged += (s, e) => FilterProducts();
        }

        private void LoadProducts()
        {
            _dtProducts = ProductDAL.GetAll();
            FilterProducts();
        }

        private void FilterProducts()
        {
            if (_dtProducts == null) return;

            dgProducts.Rows.Clear();
            string query = txtSearch.Text?.Trim().ToLower() ?? "";

            int selectedCatID = 0;
            if (cboCategory.SelectedItem is ComboItem ciCat)
            {
                selectedCatID = ciCat.ID;
            }

            int selectedStatus = cboStatus.SelectedIndex; // 0 = الكل, 1 = النشطة, 2 = المعطلة

            int count = 0;

            dgProducts.SuspendLayout();
            try
            {
                foreach (DataRow r in _dtProducts.Rows)
                {
                    bool active = Convert.ToBoolean(r["IsActive"]);

                    if (selectedStatus == 1 && !active) continue;
                    if (selectedStatus == 2 && active) continue;

                    if (selectedCatID > 0)
                    {
                        int catId = r["CategoryID"] != DBNull.Value ? Convert.ToInt32(r["CategoryID"]) : 0;
                        if (catId != selectedCatID) continue;
                    }

                    string name = r["ProductName"]?.ToString() ?? "";
                    string code = r["ProductCode"]?.ToString() ?? "";
                    string partNum = r["PartNumber"] != DBNull.Value ? r["PartNumber"].ToString() : "";
                    string barcode = r.Table.Columns.Contains("InternationalCode") && r["InternationalCode"] != DBNull.Value ? r["InternationalCode"].ToString() : "";
                    string brand = r.Table.Columns.Contains("Brand") && r["Brand"] != DBNull.Value ? r["Brand"].ToString() : "";
                    string model = r.Table.Columns.Contains("CarModel") && r["CarModel"] != DBNull.Value ? r["CarModel"].ToString() : "";

                    if (string.IsNullOrEmpty(query) || 
                        name.ToLower().Contains(query) || 
                        code.ToLower().Contains(query) || 
                        partNum.ToLower().Contains(query) || 
                        barcode.ToLower().Contains(query) ||
                        brand.ToLower().Contains(query) ||
                        model.ToLower().Contains(query))
                    {
                        count++;
                        var ri = dgProducts.Rows.Add(r["ProductID"], r["ProductCode"], r["PartNumber"], r["ProductName"],
                            r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "---",
                            r["Unit"], Convert.ToDecimal(r["SalePrice"]).ToString("N2"), active ? "✓" : "✗");
                        if (!active) dgProducts.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
                    }
                }
            }
            finally
            {
                dgProducts.ResumeLayout();
            }

            if (lblItemCount != null)
            {
                lblItemCount.Text = $"عدد الأصناف: {count}";
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            FilterProducts();
        }

        private void DgProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgProducts.SelectedRows.Count == 0)
            {
                _selectedID = 0;
                return;
            }
            _selectedID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (!Session.CanEdit("Products"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية تعديل كارت الصنف!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً لتعديله.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (new FrmProductCard(_selectedID).ShowDialog() == DialogResult.OK)
            {
                LoadProducts();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!Session.CanDelete("Products"))
            {
                MessageBox.Show("❌ عفوًا: لا تملك صلاحية حذف وإيقاف الأصناف!", "صلاحية مرفوضة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً لإيقافه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("هل أنت متأكد من إيقاف هذا الصنف؟", "تأكيد الإيقاف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ProductDAL.Delete(_selectedID);
                LoadProducts();
            }
        }

        private void BtnPrintBarcode_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("يرجى اختيار صنف أولاً للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dr = ProductDAL.GetByID(_selectedID);
            if (dr == null) return;

            string name = dr["ProductName"]?.ToString() ?? "";
            string code = dr["ProductCode"]?.ToString() ?? "";
            string intCode = dr["InternationalCode"] != DBNull.Value ? dr["InternationalCode"].ToString() : "";
            decimal price = Convert.ToDecimal(dr["SalePrice"]);
            string shelfLocation = dr["ShelfLocation"] != DBNull.Value ? dr["ShelfLocation"].ToString() : "";

            using (var dlg = new FrmPrintProductBarcode(_selectedID, name, code, intCode, price, shelfLocation))
            {
                dlg.ShowDialog(this);
            }
        }

        internal static bool PromptImportPassword(Form owner)
        {
            using (var passForm = new Form())
            {
                passForm.Text = "كلمة المرور مطلوبة";
                passForm.Size = new Size(340, 155);
                passForm.StartPosition = FormStartPosition.CenterParent;
                passForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                passForm.MaximizeBox = false;
                passForm.MinimizeBox = false;
                passForm.RightToLeft = RightToLeft.Yes;
                passForm.RightToLeftLayout = true;
                var lbl = new Label { Text = "أدخل كلمة المرور للاستيراد:", Dock = DockStyle.Top, Height = 30, TextAlign = System.Drawing.ContentAlignment.MiddleRight, Padding = new Padding(8, 5, 8, 0) };
                var txt = new TextBox { Dock = DockStyle.Top, PasswordChar = '*', Height = 28, Font = new Font("Segoe UI", 11f), RightToLeft = RightToLeft.Yes };
                var btnOk = new Button { Text = "موافق", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 36 };
                passForm.Controls.Add(btnOk);
                passForm.Controls.Add(txt);
                passForm.Controls.Add(lbl);
                passForm.AcceptButton = btnOk;
                if (passForm.ShowDialog(owner) == DialogResult.OK)
                {
                    if (txt.Text == "Pro@soft2026")
                        return true;
                    MessageBox.Show("كلمة المرور غير صحيحة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }
    }
}

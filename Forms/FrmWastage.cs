using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmWastage : Form
    {
        private ComboBox cboWarehouse, cboDriver, cboProduct;
        private DateTimePicker dtpDate;
        private TextBox txtNotes;
        private DataGridView dgItems;
        private Button btnSave, btnAddItem, btnSearchProduct;
        private Label lblTotal;

        private DataTable dtProducts;

        public FrmWastage()
        {
            InitUI();
            LoadWarehouseCombo();
            LoadDriverCombo();
            LoadProductsData();
        }

        private void InitUI()
        {
            this.Text = "تسجيل وتسوية الهوالك والتالف 🗑️";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmWastage_KeyDown;

            // Flow panel for top info
            var pnlInfo = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 12, 10, 10),
                RightToLeft = RightToLeft.Yes
            };

            var lblDate = new Label { Text = "التاريخ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            dtpDate = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Now };

            var lblWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            cboWarehouse = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            var lblDriver = new Label { Text = "المندوب المسؤول (اختياري):", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            cboDriver = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            var lblNotes = new Label { Text = "ملاحظات:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            txtNotes = new TextBox { Width = 200, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };

            // Row 2 of inputs: Product selection
            var lblProd = new Label { Text = "الصنف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 15, 0, 0), Font = Theme.FontBold };
            cboProduct = new ComboBox
            {
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(0, 10, 0, 0)
            };

            btnSearchProduct = Theme.MakeButton("🔍 بحث بالاسم/الكود (F3)", Theme.Accent);
            btnSearchProduct.Width = 180;
            btnSearchProduct.Height = 28;
            btnSearchProduct.Margin = new Padding(10, 10, 0, 0);
            btnSearchProduct.Click += BtnSearchProduct_Click;

            btnAddItem = Theme.MakeButton("➕ إضافة صنف", Theme.Primary);
            btnAddItem.Width = 120;
            btnAddItem.Height = 28;
            btnAddItem.Margin = new Padding(10, 10, 0, 0);
            btnAddItem.Click += BtnAddItem_Click;

            pnlInfo.Controls.AddRange(new Control[] { 
                lblDate, dtpDate, lblWh, cboWarehouse, lblDriver, cboDriver, lblNotes, txtNotes,
                lblProd, cboProduct, btnSearchProduct, btnAddItem 
            });

            // Grid Panel
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Header panel for Grid
            var pnlGridHeader = new Panel { Dock = DockStyle.Top, Height = 40 };
            var lblGridTitle = new Label { Text = "📦 بنود الأصناف التالفة:", Font = Theme.FontHeader, ForeColor = Theme.TextMain, AutoSize = true, Location = new Point(5, 10) };
            pnlGridHeader.Controls.Add(lblGridTitle);

            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(140, 50, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) }
            };

            // Columns
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف التالف", ReadOnly = true, FillWeight = 150 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "الكمية التالفة", FillWeight = 50, ValueType = typeof(decimal) });
            bool canSeeCost = Session.CanViewCost("Wastage");
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostPrice", HeaderText = "تكلفة الوحدة", ReadOnly = true, FillWeight = 50, Visible = canSeeCost });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "إجمالي التكلفة", ReadOnly = true, FillWeight = 60, Visible = canSeeCost });
            
            var colDelete = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "حذف",
                Text = "❌",
                UseColumnTextForButtonValue = true,
                FillWeight = 30
            };
            dgItems.Columns.Add(colDelete);

            dgItems.CellClick += DgItems_CellClick;
            dgItems.CellValueChanged += DgItems_CellValueChanged;
            dgItems.CellValidating += DgItems_CellValidating;
            dgItems.DataError += (s, e) => e.ThrowException = false; // Prevent annoying dialogs

            pnlGrid.Controls.Add(dgItems);
            pnlGrid.Controls.Add(pnlGridHeader);

            // Footer Panel
            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Theme.BgCard, Padding = new Padding(15, 10, 15, 10) };
            
            lblTotal = new Label
            {
                Text = "إجمالي التكلفة: 0.00 ج",
                ForeColor = Theme.Danger,
                Dock = DockStyle.Right,
                Width = 300,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Visible = canSeeCost
            };

            btnSave = Theme.MakeButton("💾 حفظ مستند التالف", Color.FromArgb(160, 50, 50));
            btnSave.Dock = DockStyle.Left;
            btnSave.Width = 200;
            btnSave.Font = Theme.FontBold;
            btnSave.Click += BtnSave_Click;

            pnlFoot.Controls.AddRange(new Control[] { lblTotal, btnSave });

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlInfo);
            pnlGrid.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private void FrmWastage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3)
            {
                BtnSearchProduct_Click(null, null);
                e.Handled = true;
            }
        }

        private void LoadWarehouseCombo()
        {
            try
            {
                var dt = WarehouseDAL.GetAll(true);
                cboWarehouse.Items.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
                }
                cboWarehouse.DisplayMember = "Text";
                if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل المخازن: " + ex.Message);
            }
        }

        private void LoadDriverCombo()
        {
            try
            {
                var dt = DbHelper.Query("SELECT EmpID, EmpName FROM Employees WHERE IsActive = 1 ORDER BY EmpName");
                cboDriver.Items.Clear();
                cboDriver.Items.Add(new ComboItem(0, "-- لا يوجد مندوب مسؤول --"));
                foreach (DataRow r in dt.Rows)
                {
                    cboDriver.Items.Add(new ComboItem((int)r["EmpID"], r["EmpName"].ToString()));
                }
                cboDriver.DisplayMember = "Text";
                cboDriver.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الموظفين: " + ex.Message);
            }
        }

        private void LoadProductsData()
        {
            try
            {
                dtProducts = DbHelper.Query("SELECT ProductID, ProductName, PurchasePrice, ISNULL(Unit, N'وحدة') AS Unit FROM Products WHERE IsActive = 1 ORDER BY ProductName");
                cboProduct.Items.Clear();
                cboProduct.Items.Add(new ComboItem(0, "-- اختر الصنف --"));
                foreach (DataRow r in dtProducts.Rows)
                {
                    cboProduct.Items.Add(new ComboItem((int)r["ProductID"], r["ProductName"].ToString()));
                }
                cboProduct.DisplayMember = "Text";
                cboProduct.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الأصناف: " + ex.Message);
            }
        }

        private void BtnSearchProduct_Click(object sender, EventArgs e)
        {
            int? warehouseID = null;
            if (cboWarehouse.SelectedItem is ComboItem w && w.ID > 0)
                warehouseID = w.ID;

            using (var frm = new FrmProductSearch(warehouseID))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    for (int i = 0; i < cboProduct.Items.Count; i++)
                    {
                        if (cboProduct.Items[i] is ComboItem item && item.ID == frm.SelectedProductID)
                        {
                            cboProduct.SelectedIndex = i;
                            break;
                        }
                    }
                    BtnAddItem_Click(null, null);
                }
            }
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (!(cboProduct.SelectedItem is ComboItem selectedItem) || selectedItem.ID <= 0)
            {
                MessageBox.Show("يرجى اختيار الصنف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (DataGridViewRow r in dgItems.Rows)
            {
                if (r.Cells["ProductID"].Value != null && Convert.ToInt32(r.Cells["ProductID"].Value) == selectedItem.ID)
                {
                    dgItems.CurrentCell = r.Cells["Qty"];
                    dgItems.BeginEdit(true);
                    return;
                }
            }

            var rows = dtProducts.Select("ProductID = " + selectedItem.ID);
            if (rows.Length > 0)
            {
                string unit = rows[0]["Unit"].ToString();
                decimal cost = Convert.ToDecimal(rows[0]["PurchasePrice"]);

                int ri = dgItems.Rows.Add(
                    selectedItem.ID,
                    selectedItem.Text,
                    unit,
                    1.000m,
                    cost.ToString("N2"),
                    cost.ToString("N2")
                );

                dgItems.CurrentCell = dgItems.Rows[ri].Cells["Qty"];
                dgItems.BeginEdit(true);
                CalculateTotal();
            }
        }

        private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "Delete")
            {
                dgItems.Rows.RemoveAt(e.RowIndex);
                CalculateTotal();
            }
        }

        private void DgItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dgItems.Columns[e.ColumnIndex].Name == "Qty")
            {
                string val = e.FormattedValue.ToString().Trim();
                if (string.IsNullOrEmpty(val)) return;
                if (!decimal.TryParse(val, out decimal q) || q <= 0)
                {
                    MessageBox.Show("يرجى إدخال كمية تالفة أكبر من الصفر.", "خطأ مدخلات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgItems.Rows[e.RowIndex];
                if (dgItems.Columns[e.ColumnIndex].Name == "Qty")
                {
                    decimal qty = 0;
                    if (row.Cells["Qty"].Value != null)
                        decimal.TryParse(row.Cells["Qty"].Value.ToString(), out qty);
                    
                    decimal cost = 0;
                    if (row.Cells["CostPrice"].Value != null)
                        decimal.TryParse(row.Cells["CostPrice"].Value.ToString(), out cost);

                    row.Cells["TotalCost"].Value = (qty * cost).ToString("N2");
                }
                CalculateTotal();
            }
        }

        private void CalculateTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["TotalCost"].Value != null)
                {
                    decimal.TryParse(row.Cells["TotalCost"].Value.ToString(), out decimal val);
                    total += val;
                }
            }
            lblTotal.Text = "إجمالي التكلفة: " + total.ToString("N2") + " ج";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("Wastage")) { MessageBox.Show("⛔ ليس لديك صلاحية تسجيل التالف والهالك.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (dgItems.Rows.Count == 0)
            {
                MessageBox.Show("يرجى إضافة صنف واحد تالف على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();

            var items = new List<(int pid, decimal qty, decimal cost)>();
            decimal totalCost = 0;

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                int pid = 0;
                if (row.Cells["ProductID"].Value != null)
                    pid = Convert.ToInt32(row.Cells["ProductID"].Value);

                if (pid <= 0)
                {
                    MessageBox.Show("يرجى اختيار الصنف بشكل صحيح في جميع الأسطر.", "خطأ مدخلات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal qty = 0;
                if (row.Cells["Qty"].Value != null)
                    decimal.TryParse(row.Cells["Qty"].Value.ToString(), out qty);

                if (qty <= 0)
                {
                    MessageBox.Show("يرجى تحديد كمية صالحة أكبر من الصفر لجميع الأصناف.", "خطأ مدخلات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal cost = 0;
                if (row.Cells["CostPrice"].Value != null)
                    decimal.TryParse(row.Cells["CostPrice"].Value.ToString(), out cost);

                items.Add((pid, qty, cost));
                totalCost += (qty * cost);
            }

            int? wid = null;
            if (cboWarehouse.SelectedItem is ComboItem w && w.ID > 0)
                wid = w.ID;

            if (!wid.HasValue)
            {
                MessageBox.Show("يرجى اختيار المخزن أولاً.", "خطأ مدخلات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? driverID = null;
            if (cboDriver.SelectedItem is ComboItem d && d.ID > 0)
                driverID = d.ID;

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var cmd = new System.Data.SqlClient.SqlCommand(@"
                            INSERT INTO WastageLoss (WastageDate, WarehouseID, ResponsibleDriverID, TotalCost, Notes, CreatedBy)
                            OUTPUT INSERTED.WastageID
                            VALUES (@dt, @wid, @driver, @tot, @notes, @by)", conn, trans);
                        cmd.Parameters.AddWithValue("@dt", dtpDate.Value);
                        cmd.Parameters.AddWithValue("@wid", wid.Value);
                        cmd.Parameters.AddWithValue("@driver", (object)driverID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@tot", totalCost);
                        cmd.Parameters.AddWithValue("@notes", txtNotes.Text);
                        cmd.Parameters.AddWithValue("@by", Session.EmpID > 0 ? (object)Session.EmpID : DBNull.Value);

                        int wastageID = (int)cmd.ExecuteScalar();

                        foreach (var it in items)
                        {
                            var cmdItem = new System.Data.SqlClient.SqlCommand(@"
                                INSERT INTO WastageLossItems (WastageID, ProductID, Quantity, CostPrice, TotalCost)
                                VALUES (@wid, @pid, @qty, @cost, @tot)", conn, trans);
                            cmdItem.Parameters.AddWithValue("@wid", wastageID);
                            cmdItem.Parameters.AddWithValue("@pid", it.pid);
                            cmdItem.Parameters.AddWithValue("@qty", it.qty);
                            cmdItem.Parameters.AddWithValue("@cost", it.cost);
                            cmdItem.Parameters.AddWithValue("@tot", it.qty * it.cost);
                            cmdItem.ExecuteNonQuery();
                        }

                        trans.Commit();
                        MessageBox.Show("✅ تم حفظ مستند التالف وتعديل كميات المخزن بنجاح.", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        CloseOrNavigateBack();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show($"❌ فشل حفظ المستند:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void CloseOrNavigateBack()
        {
            if (this.ParentForm is FrmMain mainForm)
            {
                mainForm.NavigateTo(new FrmDashboard());
            }
            else
            {
                this.Close();
            }
        }
    }
}

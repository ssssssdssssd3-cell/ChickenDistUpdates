using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة مرتجع مبيعات متطورة</summary>
    public class FrmReturn : Form
    {
        private ComboBox cboSale, cboClient;
        private TextBox txtNotes;
        private DataGridView dgItems;
        private Button btnSave;
        private Label lblTotal;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnSearch;

        public FrmReturn()
        {
            InitUI();
            LoadCombos();
        }

        private void FrmReturn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
                btnSave.PerformClick();
                e.Handled = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && dgItems.IsCurrentCellInEditMode)
            {
                dgItems.EndEdit();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitUI()
        {
            this.Text = "مرتجع مبيعات - إدخال مباشر";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmReturn_KeyDown;

            // ===== 1. Filter bar (FlowLayoutPanel) =====
            var pnlInfo = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 12, 10, 10),
                WrapContents = true
            };

            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            dtpFrom = new DateTimePicker { Width = 120, Height = 26, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            
            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            dtpTo = new DateTimePicker { Width = 120, Height = 26, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            btnSearch = Theme.MakeButton("🔍 جلب الفواتير", Theme.Accent);
            btnSearch.Size = new Size(130, 28);
            btnSearch.Margin = new Padding(10, 0, 0, 0);
            btnSearch.Click += (s, e) => LoadCombos();

            var lblSale = new Label { Text = "الفاتورة الأصلية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            cboSale = new ComboBox 
            { 
                Width = 250, 
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown, 
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain 
            };
            cboSale.SelectedIndexChanged += CboSale_SelectedIndexChanged;

            var lblClient = new Label { Text = "العميل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            cboClient = new ComboBox 
            { 
                Width = 200, 
                Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown, 
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain 
            };

            var lblNotes = new Label { Text = "ملاحظات المرتجع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(20, 8, 0, 0), Font = Theme.FontBold };
            txtNotes = new TextBox { Width = 220, Height = 26, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle };

            pnlInfo.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnSearch, lblSale, cboSale, lblClient, cboClient, lblNotes, txtNotes });

            // ===== 2. Grid panel =====
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 10, 10, 10) };
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = false, // Enable editing on cells
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(140, 40, 40), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldQty", HeaderText = "الكمية الأصلية بالفاتورة", ReadOnly = true, FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedQty", HeaderText = "المرتجع السابق", ReadOnly = true, FillWeight = 40 });
            
            var colNew = new DataGridViewTextBoxColumn 
            { 
                Name = "NewReturnedQty", 
                HeaderText = "المرتجع الجديد (تعديل مباشر)", 
                ReadOnly = false, 
                FillWeight = 50,
                ValueType = typeof(decimal)
            };
            colNew.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 60);
            colNew.DefaultCellStyle.ForeColor = Color.Yellow;
            colNew.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgItems.Columns.Add(colNew);
            
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "السعر الأصلي", ReadOnly = true, FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "إجمالي المرتجع", ReadOnly = true, FillWeight = 50 });

            dgItems.CellValidating += DgItems_CellValidating;
            dgItems.CellValueChanged += DgItems_CellValueChanged;

            pnlGrid.Controls.Add(dgItems);

            // ===== 3. Footer panel =====
            var pnlFoot = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 60, 
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };
            
            lblTotal = new Label 
            { 
                Text = "الإجمالي: 0.00 ج", 
                ForeColor = Theme.Accent, 
                Dock = DockStyle.Right,
                Width = 250,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            btnSave = Theme.MakeButton("💾 حفظ مرتجع البيع", Color.FromArgb(160, 50, 50));
            btnSave.Dock = DockStyle.Left;
            btnSave.Width = 180;
            btnSave.Font = Theme.FontBold;
            btnSave.Click += BtnSave_Click;
            
            Label lblHotkeys = new Label
            {
                Text = "الاختصارات: [F5] حفظ المرتجع",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(10, 20),
                AutoSize = true,
                Anchor = (AnchorStyles.Bottom | AnchorStyles.Left)
            };

            pnlFoot.Controls.AddRange(new Control[] { lblTotal, btnSave, lblHotkeys });

            // ===== 4. Add controls =====
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlInfo);
            pnlGrid.BringToFront();
        }

        private void LoadCombos()
        {
            cboSale.SelectedIndexChanged -= CboSale_SelectedIndexChanged;

            var dtS = SaleDAL.GetAll(dtpFrom.Value.Date, dtpTo.Value.Date);
            cboSale.Items.Clear();
            cboSale.Items.Add(new ComboItem(0, "-- اختر الفاتورة الأصلية لمرتجعاتها --"));
            foreach (DataRow r in dtS.Rows)
                cboSale.Items.Add(new ComboItem((int)r["SaleID"], $"{r["SaleCode"]} | {r["ClientName"]}"));
            cboSale.DisplayMember = "Text";

            var dtC = ClientDAL.GetAll(true);
            cboClient.Items.Clear();
            cboClient.Items.Add(new ComboItem(0, "-- اختر عميل --"));
            foreach (DataRow r in dtC.Rows)
                cboClient.Items.Add(new ComboItem((int)r["ClientID"], r["ClientName"].ToString()));
            cboClient.DisplayMember = "Text";

            cboSale.SelectedIndexChanged += CboSale_SelectedIndexChanged;
            
            if (cboSale.Items.Count > 0)
                cboSale.SelectedIndex = 0;
            if (cboClient.Items.Count > 0)
                cboClient.SelectedIndex = 0;
        }

        private void CboSale_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Temporarily detach events to avoid triggering calculations while loading rows
            dgItems.CellValueChanged -= DgItems_CellValueChanged;
            dgItems.Rows.Clear();
            lblTotal.Text = "الإجمالي: 0.00 ج";

            if (cboSale.SelectedItem is ComboItem cs && cs.ID > 0)
            {
                // 1. Get Client ID of this sale
                var dtSale = DbHelper.Query("SELECT ClientID FROM Sales WHERE SaleID = @id", DbHelper.P("@id", cs.ID));
                if (dtSale.Rows.Count > 0 && dtSale.Rows[0]["ClientID"] != DBNull.Value)
                {
                    int clientID = Convert.ToInt32(dtSale.Rows[0]["ClientID"]);
                    foreach (ComboItem item in cboClient.Items)
                    {
                        if (item.ID == clientID)
                        {
                            cboClient.SelectedItem = item;
                            break;
                        }
                    }
                    cboClient.Enabled = false; // Lock client selection
                }
                else
                {
                    if (cboClient.Items.Count > 0)
                        cboClient.SelectedIndex = 0;
                    cboClient.Enabled = true;
                }

                // 2. Fetch sale items and their previously returned quantities using dynamic subquery
                DataTable dtItems = DbHelper.Query(@"
                    SELECT 
                        si.ProductID, 
                        p.ProductName, 
                        si.Quantity AS SoldQty, 
                        si.UnitPrice,
                        COALESCE((
                            SELECT SUM(ri.Quantity)
                            FROM ReturnItems ri
                            JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                            WHERE sr.SaleID = si.SaleID AND ri.ProductID = si.ProductID
                        ), 0) AS PrevReturnedQty
                    FROM SaleItems si
                    JOIN Products p ON si.ProductID = p.ProductID
                    WHERE si.SaleID = @sid", 
                    DbHelper.P("@sid", cs.ID));

                foreach (DataRow r in dtItems.Rows)
                {
                    int prodID = Convert.ToInt32(r["ProductID"]);
                    string name = r["ProductName"].ToString();
                    decimal soldQty = Convert.ToDecimal(r["SoldQty"]);
                    decimal price = Convert.ToDecimal(r["UnitPrice"]);
                    decimal prevQty = Convert.ToDecimal(r["PrevReturnedQty"]);

                    dgItems.Rows.Add(prodID, name, soldQty.ToString("F2"), prevQty.ToString("F2"), "0.00", price.ToString("F2"), "0.00");
                }
            }
            else
            {
                cboClient.Enabled = true;
                if (cboClient.Items.Count > 0)
                    cboClient.SelectedIndex = 0;
            }

            dgItems.CellValueChanged += DgItems_CellValueChanged;
            CalculateOverallTotal();
        }

        private void DgItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dgItems.Columns[e.ColumnIndex].Name == "NewReturnedQty")
            {
                string valStr = e.FormattedValue.ToString().Trim();
                if (string.IsNullOrEmpty(valStr)) return;
                
                if (!decimal.TryParse(valStr, out decimal newQty) || newQty < 0)
                {
                    MessageBox.Show("الرجاء إدخال كمية مرتجعة صالحة (أكبر من أو تساوي الصفر).", "خطأ مدخلات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                var row = dgItems.Rows[e.RowIndex];
                decimal soldQty = Convert.ToDecimal(row.Cells["SoldQty"].Value);
                decimal prevQty = Convert.ToDecimal(row.Cells["PrevReturnedQty"].Value);

                if (newQty + prevQty > soldQty)
                {
                    MessageBox.Show($"الكمية المرتجعة الجديدة ({newQty}) مع المرتجع السابق ({prevQty}) لا يمكن أن تتجاوز الكمية الأصلية بالفاتورة ({soldQty}).\n\nالحد الأقصى المسموح به حالياً للمرتجع الجديد هو: {soldQty - prevQty}", "تجاوز الكمية المتاحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "NewReturnedQty")
            {
                var row = dgItems.Rows[e.RowIndex];
                decimal newQty = 0;
                if (row.Cells["NewReturnedQty"].Value != null)
                {
                    decimal.TryParse(row.Cells["NewReturnedQty"].Value.ToString(), out newQty);
                }
                
                decimal price = Convert.ToDecimal(row.Cells["UnitPrice"].Value);
                decimal rowTotal = newQty * price;
                row.Cells["TotalPrice"].Value = rowTotal.ToString("F2");

                CalculateOverallTotal();
            }
        }

        private void CalculateOverallTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["TotalPrice"].Value != null)
                {
                    decimal.TryParse(row.Cells["TotalPrice"].Value.ToString(), out decimal rowTotal);
                    total += rowTotal;
                }
            }
            lblTotal.Text = "الإجمالي: " + total.ToString("N2") + " ج";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!(cboSale.SelectedItem is ComboItem cs) || cs.ID == 0) 
            { 
                MessageBox.Show("يجب اختيار الفاتورة الأصلية أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
                return; 
            }

            var returnItems = new List<SaleItemDTO>();
            decimal totalReturnAmount = 0;

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                int prodID = Convert.ToInt32(row.Cells["ProductID"].Value);
                string prodName = row.Cells["ProductName"].Value.ToString();
                
                decimal newQty = 0;
                if (row.Cells["NewReturnedQty"].Value != null)
                {
                    decimal.TryParse(row.Cells["NewReturnedQty"].Value.ToString(), out newQty);
                }

                if (newQty > 0)
                {
                    decimal soldQty = Convert.ToDecimal(row.Cells["SoldQty"].Value);
                    decimal prevQty = Convert.ToDecimal(row.Cells["PrevReturnedQty"].Value);
                    
                    if (newQty + prevQty > soldQty)
                    {
                        MessageBox.Show($"عذراً، الكمية المرتجعة الجديدة مع السابقة للصنف ({prodName}) تتجاوز الكمية الأصلية بالفاتورة!\nالكمية الأصلية: {soldQty}\nالمرتجع السابق: {prevQty}\nالمرتجع الجديد: {newQty}", "تجاوز الكمية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    decimal price = Convert.ToDecimal(row.Cells["UnitPrice"].Value);
                    returnItems.Add(new SaleItemDTO 
                    { 
                        ProductID = prodID, 
                        ProductName = prodName, 
                        Quantity = newQty, 
                        UnitPrice = price 
                    });
                    totalReturnAmount += (newQty * price);
                }
            }

            if (returnItems.Count == 0)
            {
                MessageBox.Show("يرجى إدخال كمية مرتجعة جديدة صالحة (أكبر من الصفر) لصنف واحد على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int saleID = cs.ID;
            int? clientID = (cboClient.SelectedItem is ComboItem cc && cc.ID > 0) ? (int?)cc.ID : null;

            int id = ReturnDAL.SaveReturn(saleID, clientID, totalReturnAmount, txtNotes.Text, returnItems);
            if (id > 0) 
            { 
                MessageBox.Show("تم حفظ مرتجع البيع بنجاح!", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                txtNotes.Text = "";
                cboSale.SelectedIndex = 0; // Trigger reload
            }
            else 
            {
                MessageBox.Show("فشل حفظ المرتجع", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;
using System.Linq;

namespace ChickenDist.Forms
{
    /// <summary>شاشة مرتجع مبيعات متطورة بتصميم رئيسي-تفصيلي</summary>
    public class FrmReturn : Form
    {
        private DataGridView dgSales, dgItems;
        private TextBox txtSearch, txtInvoiceBarcode, txtNotes;
        private ComboBox cboClient;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnSearch, btnSave;
        private Label lblTotal;
        private DataTable _salesDt;

        public FrmReturn()
        {
            InitUI();
            LoadClients();
            LoadSales();
        }

        private void LoadClients()
        {
            cboClient.SelectedIndexChanged -= CboClient_SelectedIndexChanged;
            cboClient.Items.Clear();
            cboClient.Items.Add(new ComboItem(0, "-- الكل --"));
            try
            {
                var dtC = ClientDAL.GetAll(true);
                foreach (DataRow r in dtC.Rows)
                {
                    cboClient.Items.Add(new ComboItem(Convert.ToInt32(r["ClientID"]), r["ClientName"].ToString()));
                }
            }
            catch { }
            cboClient.DisplayMember = "Text";
            cboClient.SelectedIndexChanged += CboClient_SelectedIndexChanged;
            if (cboClient.Items.Count > 0)
                cboClient.SelectedIndex = 0;
        }

        private void CboClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSales();
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
            if (keyData == Keys.Enter)
            {
                if (dgItems.Focused || dgItems.EditingControl != null)
                {
                    dgItems.EndEdit();
                    var curCell = dgItems.CurrentCell;
                    if (curCell != null && curCell.RowIndex >= 0 && curCell.RowIndex < dgItems.Rows.Count)
                    {
                        // البحث عن الخلية التالية القابلة للتعديل في نفس السطر
                        int nextCol = -1;
                        for (int col = curCell.ColumnIndex + 1; col < dgItems.ColumnCount; col++)
                        {
                            if (!dgItems.Columns[col].ReadOnly && dgItems.Columns[col].Visible)
                            {
                                nextCol = col;
                                break;
                            }
                        }

                        if (nextCol != -1)
                        {
                            dgItems.CurrentCell = dgItems.Rows[curCell.RowIndex].Cells[nextCol];
                            dgItems.BeginEdit(true);
                            return true;
                        }
                        else
                        {
                            txtNotes.Focus();
                            return true;
                        }
                    }
                    else
                    {
                        txtNotes.Focus();
                        return true;
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitUI()
        {
            this.Text = "مرتجع مبيعات - تحديد الفاتورة والارتجاع";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmReturn_KeyDown;

            // ===== 1. Top Filter panel =====
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10),
                WrapContents = false
            };

            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            dtpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            dtpFrom.ValueChanged += (s, e) => LoadSales();

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            dtpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpTo.ValueChanged += (s, e) => LoadSales();

            var lblClient = new Label { Text = "العميل:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            cboClient = new ComboBox 
            { 
                Width = 180, 
                DropDownStyle = ComboBoxStyle.DropDown, 
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain 
            };
            cboClient.SelectedIndexChanged += (s, e) => LoadSales();

            var lblSearch = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            txtSearch = new TextBox { Width = 150, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
            txtSearch.TextChanged += (s, e) => LoadSales();

            var lblBarcode = new Label { Text = "باركود الفاتورة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            txtInvoiceBarcode = new TextBox { Width = 130, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.No };
            txtInvoiceBarcode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DoBarcodeSearch(txtInvoiceBarcode.Text.Trim());
                }
            };

            btnSearch = Theme.MakeButton("🔍 تحديث الفواتير", Theme.Accent);
            btnSearch.Size = new Size(120, 28);
            btnSearch.Margin = new Padding(20, 0, 0, 0);
            btnSearch.Click += (s, e) => LoadSales();

            pnlFilter.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblClient, cboClient, lblSearch, txtSearch, lblBarcode, txtInvoiceBarcode, btnSearch });

            // ===== 2. SplitContainer =====
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 260
            };
            split.Panel1.Padding = new Padding(10, 5, 10, 5);
            split.Panel2.Padding = new Padding(10, 5, 10, 5);

            // Top Grid: Sales Invoices
            dgSales = MakeGrid();
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleID", Visible = false });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", HeaderText = "رقم الفاتورة", FillWeight = 50f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleDate", HeaderText = "التاريخ والوقت", FillWeight = 70f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleType", HeaderText = "نوع الفاتورة", FillWeight = 45f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "العميل", FillWeight = 110f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "صافي القيمة", FillWeight = 55f });
            dgSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات", FillWeight = 120f });
            split.Panel1.Controls.Add(dgSales);

            // Bottom Grid: Selected Sale Items
            dgItems = MakeGrid();
            dgItems.ReadOnly = false; // تفعيل التعديل على الخلايا
            dgItems.SelectionMode = DataGridViewSelectionMode.CellSelect;
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
            dgItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(140, 40, 40); // اللون الأحمر الغامق لتمييز بنود المرتجع
            split.Panel2.Controls.Add(dgItems);

            // ===== 3. Footer panel =====
            var pnlFoot = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 60, 
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblNotes = new Label { Text = "ملاحظات المرتجع:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(15, 20), Anchor = AnchorStyles.Left };
            txtNotes = new TextBox { Width = 300, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle, Location = new Point(125, 16), Anchor = AnchorStyles.Left };
            
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
            btnSave.Width = 180;
            btnSave.Height = 36;
            btnSave.Location = new Point(450, 12);
            btnSave.Anchor = AnchorStyles.None;
            btnSave.Font = Theme.FontBold;
            btnSave.Click += BtnSave_Click;
            
            Label lblHotkeys = new Label
            {
                Text = "الاختصارات: [F5] حفظ المرتجع",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(650, 20),
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            pnlFoot.Controls.AddRange(new Control[] { lblNotes, txtNotes, lblTotal, btnSave, lblHotkeys });

            // ===== 4. Add controls =====
            this.Controls.Add(split);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlFilter);
            split.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private DataGridView MakeGrid()
        {
            var dg = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(40, 50, 70), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };
            return dg;
        }

        private void LoadSales()
        {
            dgSales.SelectionChanged -= DgSales_SelectionChanged;
            dgSales.Rows.Clear();
            dgItems.Rows.Clear();
            lblTotal.Text = "الإجمالي: 0.00 ج";

            string search = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(search)) search = null;

            int? selectedClientID = null;
            if (cboClient != null && cboClient.SelectedItem is ComboItem cs && cs.ID > 0)
            {
                selectedClientID = cs.ID;
            }

            var dtS = SaleDAL.GetAll(dtpFrom.Value.Date, dtpTo.Value.Date, selectedClientID, null);
            _salesDt = dtS;

            foreach (DataRow r in dtS.Rows)
            {
                int saleID = Convert.ToInt32(r["SaleID"]);
                string saleCode = r["SaleCode"].ToString();
                string dateStr = Convert.ToDateTime(r["SaleDate"]).ToString("yyyy/MM/dd HH:mm");
                string type = r["SaleType"].ToString() == "Credit" ? "آجل" : (r["SaleType"].ToString() == "Installment" ? "تقسيط" : "نقدي");
                string clientName = r["ClientName"].ToString();
                decimal total = Convert.ToDecimal(r["TotalAmount"]);
                string notes = r["Notes"].ToString();

                if (search != null)
                {
                    bool match = saleCode.Contains(search) || clientName.Contains(search) || notes.Contains(search);
                    if (!match) continue;
                }

                dgSales.Rows.Add(saleID, saleCode, dateStr, type, clientName, total.ToString("N2"), notes);
            }

            dgSales.SelectionChanged += DgSales_SelectionChanged;
            if (dgSales.Rows.Count > 0)
            {
                dgSales.CurrentCell = dgSales.Rows[0].Cells[1];
            }
        }

        private void DgSales_SelectionChanged(object sender, EventArgs e)
        {
            dgItems.CellValueChanged -= DgItems_CellValueChanged;
            dgItems.Rows.Clear();
            lblTotal.Text = "الإجمالي: 0.00 ج";

            if (dgSales.CurrentRow != null && dgSales.CurrentRow.Cells["SaleID"].Value != null)
            {
                int saleID = Convert.ToInt32(dgSales.CurrentRow.Cells["SaleID"].Value);

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
                    DbHelper.P("@sid", saleID));

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
            if (dgSales.CurrentRow == null || dgSales.CurrentRow.Cells["SaleID"].Value == null) 
            { 
                MessageBox.Show("يجب اختيار الفاتورة الأصلية أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
                return; 
            }

            int saleID = Convert.ToInt32(dgSales.CurrentRow.Cells["SaleID"].Value);

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

            int? clientID = null;
            var dtSale = DbHelper.Query("SELECT ClientID FROM Sales WHERE SaleID = @id", DbHelper.P("@id", saleID));
            if (dtSale.Rows.Count > 0 && dtSale.Rows[0]["ClientID"] != DBNull.Value)
            {
                clientID = Convert.ToInt32(dtSale.Rows[0]["ClientID"]);
            }

            try
            {
                int id = ReturnDAL.SaveReturn(saleID, clientID, totalReturnAmount, txtNotes.Text, returnItems);
                if (id > 0) 
                { 
                    MessageBox.Show("تم حفظ مرتجع البيع بنجاح!", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                    txtNotes.Text = "";
                    
                    LoadSales();
                    
                    foreach (DataGridViewRow row in dgSales.Rows)
                    {
                        if (row.Cells["SaleID"].Value != null && Convert.ToInt32(row.Cells["SaleID"].Value) == saleID)
                        {
                            dgSales.CurrentCell = row.Cells[1];
                            break;
                        }
                    }
                    DgSales_SelectionChanged(dgSales, EventArgs.Empty);
                }
                else 
                {
                    MessageBox.Show("فشل حفظ المرتجع", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل حفظ مرتجع المبيعات", ex, "FrmReturn.BtnSave_Click");
                MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DoBarcodeSearch(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            try
            {
                var dt = DbHelper.Query("SELECT SaleID FROM Sales WHERE SaleCode = @code OR CAST(SaleID AS VARCHAR) = @code", DbHelper.P("@code", code));
                if (dt.Rows.Count > 0)
                {
                    int targetSaleID = Convert.ToInt32(dt.Rows[0]["SaleID"]);
                    bool found = false;

                    dgSales.SelectionChanged -= DgSales_SelectionChanged;
                    foreach (DataGridViewRow row in dgSales.Rows)
                    {
                        if (row.Cells["SaleID"].Value != null && Convert.ToInt32(row.Cells["SaleID"].Value) == targetSaleID)
                        {
                            dgSales.CurrentCell = row.Cells[1];
                            found = true;
                            break;
                        }
                    }
                    dgSales.SelectionChanged += DgSales_SelectionChanged;

                    if (found)
                    {
                        DgSales_SelectionChanged(dgSales, EventArgs.Empty);
                        dgItems.Focus();
                    }
                    else
                    {
                        MessageBox.Show("الفاتورة غير موجودة في نطاق التواريخ المحدد في الأعلى.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("عذراً، رقم الفاتورة أو الباركود غير صحيح أو غير مسجل بالنظام.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء البحث:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

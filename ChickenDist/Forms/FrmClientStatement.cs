using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>كشف حساب العميل التفصيلي</summary>
    public class FrmClientStatement : Form
    {
        private int _clientID;
        private string _clientName;
        private DataGridView dgStatement;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnLoad, btnPrint;
        private Label lblDebit, lblCredit, lblBalance;
        private DataTable _dt;
        private decimal _totalSales = 0;
        private decimal _totalReturns = 0;
        private decimal _totalPayments = 0;
        private decimal _runBalance = 0;

        public FrmClientStatement(int clientID, string clientName)
        {
            _clientID = clientID;
            _clientName = clientName;
            InitUI();
            LoadStatement();
        }

        private void InitUI()
        {
            this.Text = "كشف حساب - " + _clientName;
            this.Size = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 48, Width = 800, BackColor = Theme.BgCard, Padding = new Padding(8) };
            pnlFilter.Controls.Add(new Label { Text = "من:", Location = new Point(730, 14), AutoSize = true, ForeColor = Theme.TextMain });
            dtpFrom = new DateTimePicker { Location = new Point(590, 10), Width = 130, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            pnlFilter.Controls.Add(dtpFrom);
            pnlFilter.Controls.Add(new Label { Text = "إلى:", Location = new Point(540, 14), AutoSize = true, ForeColor = Theme.TextMain });
            dtpTo = new DateTimePicker { Location = new Point(400, 10), Width = 130, Format = DateTimePickerFormat.Short };
            pnlFilter.Controls.Add(dtpTo);
            btnLoad = Theme.MakeButton("عرض", 300, 10, 80, 30, Theme.Accent);
            btnLoad.Click += (s, e) => LoadStatement();
            btnPrint = Theme.MakeButton("🖨 طباعة", 200, 10, 90, 30, Theme.Primary);
            btnPrint.Click += BtnPrint_Click;
            pnlFilter.Controls.AddRange(new Control[] { dtpFrom, dtpTo, btnLoad, btnPrint });
            this.Controls.Add(pnlFilter);

            dgStatement = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ والوقت", FillWeight = 55 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "النوع", FillWeight = 40 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "مدين", FillWeight = 40 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "دائن", FillWeight = 40 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد الجاري", FillWeight = 55 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "القائم بالعمل", FillWeight = 50 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "تفاصيل الأصناف والبيان المالي للحساب", FillWeight = 170 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransTypeRaw", Visible = false });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefID", Visible = false });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseNotes", Visible = false });

            var btnCol = new DataGridViewButtonColumn
            {
                Name = "BtnView",
                HeaderText = "عرض",
                Text = "👁️",
                UseColumnTextForButtonValue = true,
                FillWeight = 25
            };
            dgStatement.Columns.Add(btnCol);

            dgStatement.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgStatement.Columns[e.ColumnIndex].Name == "BtnView")
                {
                    var row = dgStatement.Rows[e.RowIndex];
                    if (row.Cells["BtnView"] is DataGridViewButtonCell)
                    {
                        string typeRaw = row.Cells["TransTypeRaw"].Value?.ToString();
                        int refID = row.Cells["RefID"].Value != null ? Convert.ToInt32(row.Cells["RefID"].Value) : 0;

                        if ((typeRaw == "Sale" || typeRaw == "Return") && refID > 0)
                        {
                            var frm = new FrmStatementItemsInfo(typeRaw, refID);
                            frm.ShowDialog();
                        }
                    }
                }
            };

            this.Controls.Add(dgStatement);

            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 46, Width = 800, BackColor = Theme.BgCard, Padding = new Padding(8) };
            lblBalance = new Label { Text = "الصافي: 0", ForeColor = Theme.Accent, Location = new Point(680, 12), AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            lblCredit = new Label { Text = "إجمالي مرتجع: 0 | إجمالي توريد: 0", ForeColor = Color.LightGreen, Location = new Point(250, 12), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblDebit = new Label { Text = "إجمالي مديونية: 0", ForeColor = Color.OrangeRed, Location = new Point(20, 12), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pnlFoot.Controls.AddRange(new Control[] { lblDebit, lblCredit, lblBalance });
            this.Controls.Add(pnlFoot);
            dgStatement.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private void LoadStatement()
        {
            _dt = ClientDAL.GetStatement(_clientID, dtpFrom.Value, dtpTo.Value);
            dgStatement.Rows.Clear();
            decimal prevBalance = ClientDAL.GetPreviousBalance(_clientID, dtpFrom.Value);
            _runBalance = prevBalance;
            _totalSales = 0;
            _totalReturns = 0;
            _totalPayments = 0;

            if (prevBalance != 0)
            {
                dgStatement.Rows.Add("", "رصيد افتتاحي سابق", "", "", prevBalance.ToString("N2") + " ج", "---", "رصيد ما قبل " + dtpFrom.Value.ToString("dd/MM/yyyy"), "", 0, "");
            }

            foreach (DataRow r in _dt.Rows)
            {
                decimal deb = Convert.ToDecimal(r["Debit"]);
                decimal cred = Convert.ToDecimal(r["Credit"]);
                _runBalance = _runBalance + deb - cred;

                string typeStr = r["TransType"].ToString();
                int refID = r["RefID"] != DBNull.Value ? Convert.ToInt32(r["RefID"]) : 0;
                string baseNotes = r["Notes"].ToString();
                string detailedNotes = baseNotes;

                if (typeStr == "Sale" && refID > 0)
                {
                    _totalSales += deb;
                    // جلب تفاصيل أصناف الفاتورة
                    var dtItems = DbHelper.Query(@"
                        SELECT p.ProductName, si.Quantity, p.Unit
                        FROM SaleItems si
                        JOIN Products p ON si.ProductID = p.ProductID
                        WHERE si.SaleID = @id", DbHelper.P("@id", refID));
                    
                    if (dtItems.Rows.Count > 0)
                    {
                        var itemsList = new List<string>();
                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            itemsList.Add($"{itemRow["ProductName"]} ({Convert.ToDecimal(itemRow["Quantity"]):N0} {itemRow["Unit"]})");
                        }
                        detailedNotes += " [" + string.Join("، ", itemsList) + "]";
                    }
                }
                else if (typeStr == "Return" && refID > 0)
                {
                    _totalReturns += cred;
                    // جلب تفاصيل أصناف المرتجع
                    var dtItems = DbHelper.Query(@"
                        SELECT p.ProductName, ri.Quantity, p.Unit
                        FROM ReturnItems ri
                        JOIN Products p ON ri.ProductID = p.ProductID
                        WHERE ri.ReturnID = @id", DbHelper.P("@id", refID));
                    
                    if (dtItems.Rows.Count > 0)
                    {
                        var itemsList = new List<string>();
                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            itemsList.Add($"{itemRow["ProductName"]} ({Convert.ToDecimal(itemRow["Quantity"]):N0} {itemRow["Unit"]})");
                        }
                        detailedNotes += " [" + string.Join("، ", itemsList) + "]";
                    }
                }
                else if (typeStr == "Payment")
                {
                    _totalPayments += cred;
                }
                else if (typeStr == "Opening")
                {
                    _totalSales += deb;
                }

                string createdBy = r.Table.Columns.Contains("CreatedByName") && r["CreatedByName"] != DBNull.Value ? r["CreatedByName"].ToString() : "---";
                var rowIdx = dgStatement.Rows.Add(
                    Convert.ToDateTime(r["TransDate"]).ToString("dd/MM/yyyy HH:mm"),
                    TransTypeName(typeStr),
                    deb > 0 ? deb.ToString("N2") : "",
                    cred > 0 ? cred.ToString("N2") : "",
                    _runBalance.ToString("N2") + " ج",
                    createdBy,
                    detailedNotes,
                    typeStr,
                    refID,
                    baseNotes);

                if ((typeStr != "Sale" && typeStr != "Return") || refID <= 0)
                {
                    dgStatement.Rows[rowIdx].Cells["BtnView"] = new DataGridViewTextBoxCell { Value = "" };
                }

                if (typeStr == "Return")
                {
                    dgStatement.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.OrangeRed;
                }
                else if (typeStr == "Payment")
                {
                    dgStatement.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.LightGreen;
                }
            }

            lblDebit.Text = $"إجمالي مديونية: {_totalSales:N2} ج";
            lblCredit.Text = $"إجمالي مرتجع: {_totalReturns:N2} ج  |  إجمالي توريد: {_totalPayments:N2} ج";
            lblBalance.Text = $"الصافي: {_runBalance:N2} ج";
        }

        private string TransTypeName(string t)
        {
            switch (t)
            {
                case "Sale": return "فاتورة بيع";
                case "Return": return "مرتجع";
                case "Payment": return "تحصيل";
                case "Opening": return "رصيد افتتاحي";
                case "Discount": return "تسوية خصم";
                case "Addition": return "تسوية إضافة";
                default: return t;
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            int currentRowIndex = 0;
            
            pd.BeginPrint += (s, ev) =>
            {
                currentRowIndex = 0;
            };
            
            pd.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                var titleFont = new Font("Arial", 14, FontStyle.Bold);
                var headerFont = new Font("Arial", 9, FontStyle.Bold);
                var dataFont = new Font("Arial", 8.5f);
                var itemFont = new Font("Arial", 8f, FontStyle.Italic);
                int y = 20;
                
                g.DrawString($"كشف حساب العميل: {_clientName}", titleFont, Brushes.DarkBlue, 300, y); y += 30;
                g.DrawString($"من: {dtpFrom.Value:dd/MM/yyyy}  إلى: {dtpTo.Value:dd/MM/yyyy}", dataFont, Brushes.Black, 300, y); y += 25;
                g.DrawLine(Pens.DarkBlue, 20, y, 800, y); y += 5;
                
                int[] cols = { 20, 130, 210, 290, 370, 460 };
                string[] headers = { "التاريخ والوقت", "النوع", "مدين", "دائن", "الرصيد الجاري", "البيان التفصيلي" };
                for (int i = 0; i < headers.Length; i++)
                    g.DrawString(headers[i], headerFont, Brushes.DarkBlue, cols[i], y);
                y += 20;
                g.DrawLine(Pens.Gray, 20, y, 800, y); y += 5;
                
                ev.HasMorePages = false;
                while (currentRowIndex < dgStatement.Rows.Count)
                {
                    var row = dgStatement.Rows[currentRowIndex];
                    
                    string typeRaw = row.Cells["TransTypeRaw"].Value?.ToString();
                    int refID = row.Cells["RefID"].Value != null ? Convert.ToInt32(row.Cells["RefID"].Value) : 0;
                    
                    int itemsCount = 0;
                    DataTable dtItems = null;
                    if ((typeRaw == "Sale" || typeRaw == "Return") && refID > 0)
                    {
                        if (typeRaw == "Sale")
                        {
                            dtItems = DbHelper.Query(@"
                                SELECT p.ProductName, si.Quantity, p.Unit, si.UnitPrice, (si.Quantity * si.UnitPrice) AS Total
                                FROM SaleItems si
                                JOIN Products p ON si.ProductID = p.ProductID
                                WHERE si.SaleID = @id", DbHelper.P("@id", refID));
                        }
                        else if (typeRaw == "Return")
                        {
                            dtItems = DbHelper.Query(@"
                                SELECT p.ProductName, ri.Quantity, p.Unit, ri.UnitPrice, (ri.Quantity * ri.UnitPrice) AS Total
                                FROM ReturnItems ri
                                JOIN Products p ON ri.ProductID = p.ProductID
                                WHERE ri.ReturnID = @id", DbHelper.P("@id", refID));
                        }
                        
                        if (dtItems != null)
                        {
                            itemsCount = dtItems.Rows.Count;
                        }
                    }
                    
                    int neededHeight = 18 + (itemsCount * 15);
                    if (y + neededHeight > ev.PageBounds.Height - 100)
                    {
                        ev.HasMorePages = true;
                        return;
                    }
                    
                    g.DrawString(row.Cells["TransDate"].Value?.ToString() ?? "", dataFont, Brushes.Black, cols[0], y);
                    g.DrawString(row.Cells["TransType"].Value?.ToString() ?? "", dataFont, Brushes.Black, cols[1], y);
                    g.DrawString(row.Cells["Debit"].Value?.ToString() ?? "", dataFont, Brushes.Black, cols[2], y);
                    g.DrawString(row.Cells["Credit"].Value?.ToString() ?? "", dataFont, Brushes.Black, cols[3], y);
                    g.DrawString(row.Cells["Balance"].Value?.ToString() ?? "", dataFont, Brushes.Black, cols[4], y);
                    string printNotes = row.Cells["BaseNotes"].Value?.ToString() ?? "";
                    string createdByStr = row.Cells["CreatedByName"].Value?.ToString();
                    if (!string.IsNullOrEmpty(createdByStr) && createdByStr != "---")
                    {
                        printNotes += $" (بواسطة: {createdByStr})";
                    }
                    g.DrawString(printNotes, dataFont, Brushes.Black, cols[5], y);
                    y += 18;
                    
                    if (dtItems != null && itemsCount > 0)
                    {
                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            string bullet = typeRaw == "Sale" ? "🔸" : "🔹";
                            Brush brush = typeRaw == "Sale" ? Brushes.DimGray : Brushes.Brown;
                            string itemText = $"  {bullet} {itemRow["ProductName"]} - الكمية: {Convert.ToDecimal(itemRow["Quantity"]):N0} {itemRow["Unit"]} | السعر: {Convert.ToDecimal(itemRow["UnitPrice"]):N2} ج | الإجمالي: {Convert.ToDecimal(itemRow["Total"]):N2} ج";
                            
                            g.DrawString(itemText, itemFont, brush, cols[1] + 10, y);
                            y += 15;
                        }
                    }
                    
                    currentRowIndex++;
                }
                
                y += 15;
                if (y + 100 > ev.PageBounds.Height)
                {
                    ev.HasMorePages = true;
                    return;
                }
                
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 244, 248)), 20, y, 780, 50);
                g.DrawRectangle(new Pen(Color.FromArgb(200, 214, 228), 1.5f), 20, y, 780, 50);
                
                var labelFont = new Font("Arial", 8.5f, FontStyle.Regular);
                var valueFont = new Font("Arial", 11.5f, FontStyle.Bold);
                
                g.DrawString("إجمالي المديونية", labelFont, Brushes.DarkRed, 30, y + 6);
                g.DrawString($"{_totalSales:N2} ج", valueFont, Brushes.DarkRed, 30, y + 24);
                
                g.DrawLine(new Pen(Color.FromArgb(200, 214, 228), 1f), 215, y + 5, 215, y + 45);
                
                g.DrawString("إجمالي المرتجعات", labelFont, Brushes.Brown, 225, y + 6);
                g.DrawString($"{_totalReturns:N2} ج", valueFont, Brushes.Brown, 225, y + 24);
                
                g.DrawLine(new Pen(Color.FromArgb(200, 214, 228), 1f), 410, y + 5, 410, y + 45);
                
                g.DrawString("إجمالي التحصيل", labelFont, Brushes.DarkGreen, 420, y + 6);
                g.DrawString($"{_totalPayments:N2} ج", valueFont, Brushes.DarkGreen, 420, y + 24);
                
                g.DrawLine(new Pen(Color.FromArgb(200, 214, 228), 1f), 605, y + 5, 605, y + 45);
                
                g.DrawString("الصافي النهائي", labelFont, Brushes.DarkBlue, 615, y + 6);
                g.DrawString($"{_runBalance:N2} ج", valueFont, Brushes.DarkBlue, 615, y + 24);
            };
            var dlg = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700 };
            dlg.ShowDialog();
        }
    }

    /// <summary>شاشة تحصيل من عميل</summary>
    public class FrmPayment : Form
    {
        private int _clientID;
        private TextBox txtAmount, txtNotes;
        private ComboBox cboSafe;
        private Button btnOk, btnCancel;

        public FrmPayment(int clientID, string clientName)
        {
            _clientID = clientID;
            this.Text = "تحصيل من: " + clientName;
            this.Size = new Size(360, 270);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.RightToLeft = RightToLeft.Yes;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            int y = 20;
            this.Controls.Add(new Label { Text = "المبلغ المحصل (ج):", Location = new Point(170, y), AutoSize = true, ForeColor = Theme.TextMain });
            txtAmount = new TextBox { Location = new Point(20, y - 2), Width = 140, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
            this.Controls.Add(txtAmount); y += 40;

            this.Controls.Add(new Label { Text = "حساب التحصيل:", Location = new Point(170, y), AutoSize = true, ForeColor = Theme.TextMain });
            cboSafe = new ComboBox
            {
                Location = new Point(20, y - 2),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            // تحميل الحسابات النشطة
            try
            {
                DataTable safes = AccountDAL.GetActiveSafeAccounts();
                foreach (DataRow row in safes.Rows)
                {
                    cboSafe.Items.Add(new ComboItem(
                        Convert.ToInt32(row["AccountID"]),
                        row["AccountName"].ToString()
                    ));
                }
                 cboSafe.DisplayMember = "Text";
                 if (cboSafe.Items.Count > 0)
                 {
                     int defaultSafeID = Session.DefaultSafeID ?? 0;
                     int selectedIdx = -1;
                     if (defaultSafeID > 0)
                     {
                         for (int i = 0; i < cboSafe.Items.Count; i++)
                         {
                             if (cboSafe.Items[i] is ComboItem ci && ci.ID == defaultSafeID)
                             {
                                 selectedIdx = i;
                                 break;
                             }
                         }
                     }
                     if (selectedIdx >= 0)
                     {
                         cboSafe.SelectedIndex = selectedIdx;
                     }
                     else
                     {
                         int fallbackIdx = 0;
                         for (int i = 0; i < cboSafe.Items.Count; i++)
                         {
                             if (cboSafe.Items[i] is ComboItem ci && ci.Text.Contains("درج تلقائي"))
                             {
                                 fallbackIdx = i;
                                 break;
                             }
                         }
                         cboSafe.SelectedIndex = fallbackIdx;
                     }
                 }
            }
            catch { }
            this.Controls.Add(cboSafe); y += 40;

            this.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(170, y), AutoSize = true, ForeColor = Theme.TextMain });
            txtNotes = new TextBox { Location = new Point(20, y - 2), Width = 140, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes };
            this.Controls.Add(txtNotes); y += 50;

            btnOk = Theme.MakeButton("✅ تأكيد", 170, y, 110, 32, Theme.Accent);
            btnCancel = Theme.MakeButton("إلغاء", 50, y, 90, 32, Color.FromArgb(90, 90, 90));
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Text, out decimal amt) || amt <= 0) { MessageBox.Show("أدخل مبلغاً صحيحاً"); return; }
            int? targetSafeID = null;
            if (cboSafe.SelectedItem is ComboItem safeItem && safeItem.ID > 0)
            {
                targetSafeID = safeItem.ID;
            }
            ClientDAL.AddPayment(_clientID, amt, txtNotes.Text, targetSafeID);
            MessageBox.Show("✅ تم تسجيل التحصيل");
            this.DialogResult = DialogResult.OK;
        }
    }

    /// <summary>شاشة تفاصيل أصناف العملية المحددة</summary>
    public class FrmStatementItemsInfo : Form
    {
        private DataGridView dgItems;
        private Label lblTitle, lblTotal;
        private Button btnClose;

        public FrmStatementItemsInfo(string transType, int refID)
        {
            string titleText = transType == "Sale" ? "تفاصيل أصناف الفاتورة" : "تفاصيل أصناف المرتجع";
            this.Text = titleText;
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            lblTitle = new Label
            {
                Text = titleText,
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف" });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 35 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "السعر", FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "الإجمالي", FillWeight = 50 });
            this.Controls.Add(dgItems);

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(10) };
            
            lblTotal = new Label
            {
                Text = "إجمالي العملية: 0.00 ج",
                Dock = DockStyle.Right,
                Width = 250,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlBottom.Controls.Add(lblTotal);

            btnClose = Theme.MakeButton("إغلاق", 10, 10, 100, 32, Color.FromArgb(90, 90, 90));
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(pnlBottom);

            lblTitle.BringToFront();
            pnlBottom.BringToFront();
            dgItems.BringToFront();

            LoadItems(transType, refID);
        }

        private void LoadItems(string transType, int refID)
        {
            dgItems.Rows.Clear();
            DataTable dt = null;

            if (transType == "Sale")
            {
                dt = DbHelper.Query(@"
                    SELECT p.ProductName, si.Quantity, p.Unit, si.UnitPrice, (si.Quantity * si.UnitPrice) AS Total
                    FROM SaleItems si
                    JOIN Products p ON si.ProductID = p.ProductID
                    WHERE si.SaleID = @id", DbHelper.P("@id", refID));
            }
            else if (transType == "Return")
            {
                dt = DbHelper.Query(@"
                    SELECT p.ProductName, ri.Quantity, p.Unit, ri.UnitPrice, (ri.Quantity * ri.UnitPrice) AS Total
                    FROM ReturnItems ri
                    JOIN Products p ON ri.ProductID = p.ProductID
                    WHERE ri.ReturnID = @id", DbHelper.P("@id", refID));
            }

            if (dt == null) return;

            decimal totalSum = 0;
            foreach (DataRow r in dt.Rows)
            {
                decimal qty = Convert.ToDecimal(r["Quantity"]);
                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                decimal tot = Convert.ToDecimal(r["Total"]);
                totalSum += tot;

                dgItems.Rows.Add(r["ProductName"], qty.ToString("N0"), r["Unit"], price.ToString("N2"), tot.ToString("N2") + " ج");
            }

            lblTotal.Text = $"إجمالي العملية: {totalSum:N2} ج";
        }
    }
}

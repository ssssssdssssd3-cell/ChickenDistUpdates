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
        private decimal _totalClientPurchases = 0;  // إجمالي فواتير الشراء من العميل
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
            pnlFilter.Controls.Add(new Label { Text = "من:", Location = new Point(745, 14), AutoSize = true, ForeColor = Theme.TextMain });
            dtpFrom = new DateTimePicker { Location = new Point(545, 10), Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0) };
            dtpFrom.ValueChanged += (s, e) => LoadStatement();
            pnlFilter.Controls.Add(dtpFrom);
            pnlFilter.Controls.Add(new Label { Text = "إلى:", Location = new Point(505, 14), AutoSize = true, ForeColor = Theme.TextMain });
            dtpTo = new DateTimePicker { Location = new Point(305, 10), Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Now };
            dtpTo.ValueChanged += (s, e) => LoadStatement();
            pnlFilter.Controls.Add(dtpTo);
            btnLoad = Theme.MakeButton("عرض", 260, 10, 75, 30, Theme.Accent);
            btnLoad.Click += (s, e) => LoadStatement();
            btnPrint = Theme.MakeButton("🖨 طباعة", 165, 10, 85, 30, Theme.Primary);
            btnPrint.Click += BtnPrint_Click;
            var btnCollect = Theme.MakeButton("💵 تحصيل نقدية", 10, 10, 145, 30, Theme.Success);
            btnCollect.Click += (s, e) =>
            {
                using (var dlg = new FrmPayment(_clientID, _clientName))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadStatement();
                    }
                }
            };
            pnlFilter.Controls.AddRange(new Control[] { dtpFrom, dtpTo, btnLoad, btnPrint, btnCollect });
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

            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 46, Width = 800, BackColor = Theme.BgCard, Padding = new Padding(8) };
            lblBalance = new Label { Text = "الصافي: 0", ForeColor = Color.FromArgb(10, 60, 140), Location = new Point(680, 12), AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            lblCredit = new Label { Text = "إجمالي مرتجع: 0 | إجمالي توريد: 0", ForeColor = Color.FromArgb(15, 120, 50), Location = new Point(250, 12), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblDebit = new Label { Text = "إجمالي مديونية: 0", ForeColor = Color.FromArgb(180, 20, 20), Location = new Point(20, 12), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pnlFoot.Controls.AddRange(new Control[] { lblDebit, lblCredit, lblBalance });

            this.Controls.Clear();
            this.Controls.Add(dgStatement);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlFilter);

            pnlFilter.SendToBack();
            pnlFoot.SendToBack();
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
            _totalClientPurchases = 0;

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
                else if (typeStr == "ClientPurchase" && refID > 0)
                {
                    // شراء من عميل آجل - يزيد رصيده (دائن)
                    _totalClientPurchases += cred;
                    // جلب تفاصيل أصناف فاتورة الشراء
                    var dtItems = DbHelper.Query(@"
                        SELECT p.ProductName, pi2.Quantity, pi2.UnitName
                        FROM PurchaseItems pi2
                        JOIN Products p ON pi2.ProductID = p.ProductID
                        WHERE pi2.PurchaseID = @id", DbHelper.P("@id", refID));
                    if (dtItems.Rows.Count > 0)
                    {
                        var itemsList = new System.Collections.Generic.List<string>();
                        foreach (DataRow itemRow in dtItems.Rows)
                            itemsList.Add($"{itemRow["ProductName"]} ({Convert.ToDecimal(itemRow["Quantity"]):N0} {itemRow["UnitName"]})");
                        detailedNotes += " [" + string.Join("، ", itemsList) + "]";
                    }
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

                // تلوين احترافي وواضح لصفوف حركة الحساب بخلفيات متباينة ونصوص دقيقة
                var rowStyle = dgStatement.Rows[rowIdx].DefaultCellStyle;
                if (typeStr == "Sale")
                {
                    rowStyle.BackColor = Color.FromArgb(240, 244, 255);
                    rowStyle.ForeColor = Color.FromArgb(10, 50, 130);
                }
                else if (typeStr == "Payment")
                {
                    rowStyle.BackColor = Color.FromArgb(235, 250, 240);
                    rowStyle.ForeColor = Color.FromArgb(15, 120, 50);
                }
                else if (typeStr == "Return")
                {
                    rowStyle.BackColor = Color.FromArgb(255, 240, 240);
                    rowStyle.ForeColor = Color.FromArgb(180, 20, 20);
                }
                else if (typeStr == "ClientPurchase")
                {
                    rowStyle.BackColor = Color.FromArgb(245, 238, 255);
                    rowStyle.ForeColor = Color.FromArgb(90, 20, 140);
                }
                else
                {
                    rowStyle.BackColor = Color.FromArgb(250, 250, 250);
                    rowStyle.ForeColor = Color.FromArgb(30, 40, 50);
                }
            }

            lblDebit.Text = $"إجمالي مديونية: {_totalSales:N2} ج";
            lblCredit.Text = $"إجمالي مرتجع: {_totalReturns:N2} ج  |  إجمالي توريد: {_totalPayments:N2} ج" +
                             (_totalClientPurchases > 0 ? $"  |  شراء من عميل: {_totalClientPurchases:N2} ج" : "");
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
                case "ClientPurchase": return "📦 شراء من عميل";
                default: return t;
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            int currentRowIndex = 0;
            int pageNumber = 0;

            pd.BeginPrint += (s, ev) =>
            {
                currentRowIndex = 0;
                pageNumber = 0;
            };

            pd.PrintPage += (s, ev) =>
            {
                pageNumber++;
                var g = ev.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var titleFont  = new Font("Arial", 14, FontStyle.Bold);
                var subTitleFont = new Font("Arial", 9, FontStyle.Bold);
                var headerFont = new Font("Arial", 9, FontStyle.Bold);
                var dataFont   = new Font("Arial", 8.5f, FontStyle.Regular);
                var boldDataFont = new Font("Arial", 8.5f, FontStyle.Bold);
                var itemFont   = new Font("Arial", 8f, FontStyle.Regular);
                var itemHeaderFont = new Font("Arial", 8f, FontStyle.Bold);

                var headerBgBrush = new SolidBrush(Color.FromArgb(15, 45, 90));
                var gridPen = new Pen(Color.FromArgb(180, 190, 205), 1f);
                var borderPen = new Pen(Color.FromArgb(15, 45, 90), 1.5f);
                var subGridPen = new Pen(Color.FromArgb(200, 210, 225), 1f);

                int y = 25;
                int leftMargin = 20;
                int rightMargin = 805;
                int tableWidth = rightMargin - leftMargin;

                // ── رأس الصفحة (Header Title Block) ──
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 244, 250)), leftMargin, y, tableWidth, 45);
                g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 45);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRight  = new StringFormat { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                var sfLeft   = new StringFormat { Alignment = StringAlignment.Near,   LineAlignment = StringAlignment.Center };

                g.DrawString($"كشف حساب العميل التفصيلي: {_clientName}", titleFont, Brushes.DarkBlue, new RectangleF(leftMargin, y + 4, tableWidth, 22), sfCenter);
                g.DrawString($"الفترة من: {dtpFrom.Value:dd/MM/yyyy}  إلى: {dtpTo.Value:dd/MM/yyyy}   |   تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", subTitleFont, Brushes.DimGray, new RectangleF(leftMargin, y + 25, tableWidth, 16), sfCenter);
                y += 55;

                // ── إعداد مواضع الأعمدة والترويسة ──
                // X offsets for vertical lines: 20, 135, 220, 295, 370, 465, 805
                int[] xCols = { 20, 135, 220, 295, 370, 465, 805 };
                string[] headers = { "التاريخ والوقت", "النوع", "مدين", "دائن", "الرصيد الجاري", "البيان التفصيلي والأصناف" };

                int headerY = y;
                g.FillRectangle(headerBgBrush, leftMargin, y, tableWidth, 26);
                g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 26);

                for (int i = 0; i < headers.Length; i++)
                {
                    float cx = xCols[i];
                    float cw = xCols[i + 1] - xCols[i];
                    g.DrawString(headers[i], headerFont, Brushes.White, new RectangleF(cx, y, cw, 26), sfCenter);
                    if (i > 0)
                        g.DrawLine(Pens.White, xCols[i], y, xCols[i], y + 26);
                }
                y += 26;

                ev.HasMorePages = false;
                while (currentRowIndex < dgStatement.Rows.Count)
                {
                    var row = dgStatement.Rows[currentRowIndex];
                    string typeRaw = row.Cells["TransTypeRaw"].Value?.ToString();
                    int refID = row.Cells["RefID"].Value != null ? Convert.ToInt32(row.Cells["RefID"].Value) : 0;

                    DataTable dtItems = null;
                    if ((typeRaw == "Sale" || typeRaw == "Return" || typeRaw == "ClientPurchase") && refID > 0)
                    {
                        if (typeRaw == "Sale")
                        {
                            dtItems = DbHelper.Query(@"
                                SELECT p.ProductName, si.Quantity, ISNULL(si.UnitName, p.Unit) AS Unit, si.UnitPrice, (si.Quantity * si.UnitPrice) AS Total
                                FROM SaleItems si
                                JOIN Products p ON si.ProductID = p.ProductID
                                WHERE si.SaleID = @id", DbHelper.P("@id", refID));
                        }
                        else if (typeRaw == "Return")
                        {
                            dtItems = DbHelper.Query(@"
                                SELECT p.ProductName, ri.Quantity, ISNULL(ri.UnitName, p.Unit) AS Unit, ri.UnitPrice, (ri.Quantity * ri.UnitPrice) AS Total
                                FROM ReturnItems ri
                                JOIN Products p ON ri.ProductID = p.ProductID
                                WHERE ri.ReturnID = @id", DbHelper.P("@id", refID));
                        }
                        else if (typeRaw == "ClientPurchase")
                        {
                            dtItems = DbHelper.Query(@"
                                SELECT p.ProductName, pi2.Quantity, ISNULL(pi2.UnitName, p.Unit) AS Unit, pi2.UnitPrice, (pi2.Quantity * pi2.UnitPrice) AS Total
                                FROM PurchaseItems pi2
                                JOIN Products p ON pi2.ProductID = p.ProductID
                                WHERE pi2.PurchaseID = @id", DbHelper.P("@id", refID));
                        }
                    }

                    int itemsCount = dtItems != null ? dtItems.Rows.Count : 0;
                    int rowHeight = 22 + (itemsCount > 0 ? (18 + itemsCount * 17) : 0);

                    if (y + rowHeight > ev.PageBounds.Height - 90)
                    {
                        ev.HasMorePages = true;
                        return;
                    }

                    int rowStartY = y;

                    // تظليل خفيف جداً للصفوف التبادلية
                    if (currentRowIndex % 2 == 1 && itemsCount == 0)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 254)), leftMargin, y, tableWidth, 22);
                    }

                    // كتابة القيم في الأعمدة الرئيسية
                    string dateStr = row.Cells["TransDate"].Value?.ToString() ?? "";
                    string typeStr = row.Cells["TransType"].Value?.ToString() ?? "";
                    string debStr  = row.Cells["Debit"].Value?.ToString() ?? "";
                    string credStr = row.Cells["Credit"].Value?.ToString() ?? "";
                    string balStr  = row.Cells["Balance"].Value?.ToString() ?? "";
                    
                    string baseNotes = row.Cells["BaseNotes"].Value?.ToString() ?? "";
                    string createdBy = row.Cells["CreatedByName"].Value?.ToString();
                    if (!string.IsNullOrEmpty(createdBy) && createdBy != "---")
                        baseNotes += $" (بواسطة: {createdBy})";

                    g.DrawString(dateStr, dataFont, Brushes.Black, new RectangleF(xCols[0], y, xCols[1] - xCols[0], 22), sfCenter);
                    g.DrawString(typeStr, boldDataFont, Brushes.DarkSlateGray, new RectangleF(xCols[1], y, xCols[2] - xCols[1], 22), sfCenter);
                    g.DrawString(debStr,  boldDataFont, Brushes.DarkRed, new RectangleF(xCols[2], y, xCols[3] - xCols[2], 22), sfCenter);
                    g.DrawString(credStr, boldDataFont, Brushes.DarkGreen, new RectangleF(xCols[3], y, xCols[4] - xCols[3], 22), sfCenter);
                    g.DrawString(balStr,  boldDataFont, Brushes.DarkBlue, new RectangleF(xCols[4], y, xCols[5] - xCols[4], 22), sfCenter);
                    g.DrawString(baseNotes, dataFont, Brushes.Black, new RectangleF(xCols[5] + 5, y + 2, xCols[6] - xCols[5] - 10, 20), sfRight);

                    y += 22;

                    // ── جدول فرعي تفصيلي للأصناف عند وجود فاتورة ──
                    if (itemsCount > 0 && dtItems != null)
                    {
                        int subLeft = xCols[1] + 5;
                        int subWidth = xCols[6] - xCols[1] - 10;
                        int subHeaderY = y;

                        // خلفية الجدول الفرعي للأصناف
                        g.FillRectangle(new SolidBrush(Color.FromArgb(242, 246, 252)), subLeft, y, subWidth, 18 + itemsCount * 17);
                        g.DrawRectangle(subGridPen, subLeft, y, subWidth, 18 + itemsCount * 17);

                        // أعمدة الجدول الفرعي: [اسم الصنف (50%)] [الكمية والوحدة (20%)] [سعر الوحدة (15%)] [الإجمالي (15%)]
                        float subW0 = subWidth * 0.45f;
                        float subW1 = subWidth * 0.20f;
                        float subW2 = subWidth * 0.17f;
                        float subW3 = subWidth * 0.18f;

                        float sx0 = subLeft;
                        float sx1 = sx0 + subW0;
                        float sx2 = sx1 + subW1;
                        float sx3 = sx2 + subW2;

                        // ترويسة الجدول الفرعي للأصناف
                        g.FillRectangle(new SolidBrush(Color.FromArgb(215, 225, 240)), subLeft, y, subWidth, 18);
                        g.DrawLine(subGridPen, subLeft, y + 18, subLeft + subWidth, y + 18);

                        g.DrawString("بيان الصنف", itemHeaderFont, Brushes.DarkBlue, new RectangleF(sx0, y, subW0, 18), sfCenter);
                        g.DrawString("الكمية والوحدة", itemHeaderFont, Brushes.DarkBlue, new RectangleF(sx1, y, subW1, 18), sfCenter);
                        g.DrawString("سعر الوحدة", itemHeaderFont, Brushes.DarkBlue, new RectangleF(sx2, y, subW2, 18), sfCenter);
                        g.DrawString("الإجمالي", itemHeaderFont, Brushes.DarkBlue, new RectangleF(sx3, y, subW3, 18), sfCenter);
                        y += 18;

                        foreach (DataRow ir in dtItems.Rows)
                        {
                            string pName = ir["ProductName"].ToString();
                            decimal pQty = Convert.ToDecimal(ir["Quantity"]);
                            string pUnit = ir["Unit"]?.ToString() ?? "";
                            decimal pPrice = Convert.ToDecimal(ir["UnitPrice"]);
                            decimal pTotal = Convert.ToDecimal(ir["Total"]);

                            g.DrawString(pName, itemFont, Brushes.Black, new RectangleF(sx0 + 4, y, subW0 - 8, 17), sfRight);
                            g.DrawString($"{pQty:N0} {pUnit}", itemFont, Brushes.DarkSlateGray, new RectangleF(sx1, y, subW1, 17), sfCenter);
                            g.DrawString($"{pPrice:N2} ج", itemFont, Brushes.DarkSlateGray, new RectangleF(sx2, y, subW2, 17), sfCenter);
                            g.DrawString($"{pTotal:N2} ج", itemFont, Brushes.DarkBlue, new RectangleF(sx3, y, subW3, 17), sfCenter);

                            y += 17;
                            g.DrawLine(subGridPen, subLeft, y, subLeft + subWidth, y);
                        }
                    }

                    // ── رسم شبكة الفواصل الرأسية والأفقية للصف الرئيسي ──
                    g.DrawLine(gridPen, leftMargin, y, rightMargin, y);
                    for (int i = 0; i < xCols.Length; i++)
                    {
                        g.DrawLine(gridPen, xCols[i], rowStartY, xCols[i], y);
                    }

                    currentRowIndex++;
                }

                // رسم الإطار الخارجي الكامل للجدول
                g.DrawRectangle(borderPen, leftMargin, headerY, tableWidth, y - headerY);

                // ── صندوق الإجماليات والملخص في ذيل الصفحة ──
                y += 12;
                if (y + 55 <= ev.PageBounds.Height)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(242, 246, 252)), leftMargin, y, tableWidth, 48);
                    g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 48);

                    float boxW = tableWidth / 4f;
                    var labelFont = new Font("Arial", 8.5f, FontStyle.Regular);
                    var valueFont = new Font("Arial", 11.5f, FontStyle.Bold);

                    // 1. المديونية
                    g.DrawString("إجمالي المديونية", labelFont, Brushes.DarkRed, new RectangleF(leftMargin, y + 4, boxW, 16), sfCenter);
                    g.DrawString($"{_totalSales:N2} ج", valueFont, Brushes.DarkRed, new RectangleF(leftMargin, y + 22, boxW, 22), sfCenter);
                    g.DrawLine(gridPen, leftMargin + boxW, y, leftMargin + boxW, y + 48);

                    // 2. المرتجعات
                    g.DrawString("إجمالي المرتجعات", labelFont, Brushes.Brown, new RectangleF(leftMargin + boxW, y + 4, boxW, 16), sfCenter);
                    g.DrawString($"{_totalReturns:N2} ج", valueFont, Brushes.Brown, new RectangleF(leftMargin + boxW, y + 22, boxW, 22), sfCenter);
                    g.DrawLine(gridPen, leftMargin + boxW * 2, y, leftMargin + boxW * 2, y + 48);

                    // 3. التحصيل
                    g.DrawString("إجمالي التحصيل", labelFont, Brushes.DarkGreen, new RectangleF(leftMargin + boxW * 2, y + 4, boxW, 16), sfCenter);
                    g.DrawString($"{_totalPayments:N2} ج", valueFont, Brushes.DarkGreen, new RectangleF(leftMargin + boxW * 2, y + 22, boxW, 22), sfCenter);
                    g.DrawLine(gridPen, leftMargin + boxW * 3, y, leftMargin + boxW * 3, y + 48);

                    // 4. الصافي
                    g.DrawString("الصافي النهائي", labelFont, Brushes.DarkBlue, new RectangleF(leftMargin + boxW * 3, y + 4, boxW, 16), sfCenter);
                    g.DrawString($"{_runBalance:N2} ج", valueFont, Brushes.DarkBlue, new RectangleF(leftMargin + boxW * 3, y + 22, boxW, 22), sfCenter);
                }
            };

            var dlg = new PrintPreviewDialog { Document = pd, Width = 950, Height = 720 };
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
                if (cboSafe.Items.Count > 0) cboSafe.SelectedIndex = 0;
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
            this.DialogResult = DialogResult.OK;
            this.Close();

            // Open print & WhatsApp options dialog
            new FrmPrintClientPayment(_clientID, amt, txtNotes.Text, targetSafeID).ShowOptionsDialog();
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

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>كشف حساب المورد التفصيلي</summary>
    public class FrmSupplierStatement : Form
    {
        private int _supplierID;
        private string _supplierName;
        private ComboBox cmbSupplierSelector;
        private bool _isLoadingCombo = false;
        private DataGridView dgStatement;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnLoad, btnPrint;
        private Label lblPurchases, lblPayments, lblBalance;
        private DataTable _dt;
        private decimal _totalPurchases = 0;
        private decimal _totalPayments  = 0;
        private decimal _runBalance     = 0;

        public FrmSupplierStatement() : this(0, "") { }

        public FrmSupplierStatement(int supplierID, string supplierName)
        {
            _supplierID   = supplierID;
            _supplierName = supplierName;
            InitUI();
            LoadSuppliersCombo();
            LoadStatement();
        }

        private void InitUI()
        {
            this.Text = "كشف حساب المورد - " + (!string.IsNullOrEmpty(_supplierName) ? _supplierName : "اختر المورد");
            this.Size = new Size(1050, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== Filter bar =====
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(8, 6, 8, 6),
                WrapContents = false
            };

            pnlFilter.Controls.Add(new Label { Text = "🤝 المورد:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 6, 0, 0) });
            cmbSupplierSelector = new ComboBox
            {
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(2, 2, 0, 0)
            };
            cmbSupplierSelector.SelectedIndexChanged += CmbSupplierSelector_SelectedIndexChanged;
            pnlFilter.Controls.Add(cmbSupplierSelector);

            pnlFilter.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(12, 6, 0, 0) });
            dtpFrom = new DateTimePicker
            {
                Width = 180,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd   hh:mm tt",
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0),
                Margin = new Padding(2, 2, 0, 0)
            };
            dtpFrom.ValueChanged += (s, e) => LoadStatement();
            pnlFilter.Controls.Add(dtpFrom);

            pnlFilter.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(10, 6, 0, 0) });
            dtpTo = new DateTimePicker
            {
                Width = 180,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd   hh:mm tt",
                Value = DateTime.Now,
                Margin = new Padding(2, 2, 0, 0)
            };
            dtpTo.ValueChanged += (s, e) => LoadStatement();
            pnlFilter.Controls.Add(dtpTo);

            btnLoad = Theme.MakeButton("🔄 عرض", Theme.Accent);
            btnLoad.Size = new Size(90, 30);
            btnLoad.Margin = new Padding(10, 0, 0, 0);
            btnLoad.Click += (s, e) => LoadStatement();
            pnlFilter.Controls.Add(btnLoad);

            btnPrint = Theme.MakeButton("🖨️ طباعة", Theme.Primary);
            btnPrint.Size = new Size(100, 30);
            btnPrint.Margin = new Padding(8, 0, 0, 0);
            btnPrint.Click += BtnPrint_Click;
            pnlFilter.Controls.Add(btnPrint);

            var btnWhatsApp = Theme.MakeButton("📱 إرسال واتساب", Color.FromArgb(37, 211, 102));
            btnWhatsApp.Size = new Size(130, 30);
            btnWhatsApp.Font = Theme.FontBold;
            btnWhatsApp.ForeColor = Color.White;
            btnWhatsApp.Margin = new Padding(8, 0, 0, 0);
            btnWhatsApp.Click += (s, e) =>
            {
                if (dgStatement == null || dgStatement.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد حركات مالية لعرضها وإرسالها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                string phone = "";
                try
                {
                    object ph = DbHelper.Scalar("SELECT Phone FROM Suppliers WHERE SupplierID = @id", DbHelper.P("@id", _supplierID));
                    if (ph != null && ph != DBNull.Value) phone = ph.ToString();
                }
                catch { }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"📋 *كشف حساب مورد تفصيلي*");
                sb.AppendLine($"🏢 {AppConfig.CompanyName}");
                sb.AppendLine($"👤 المورد: {_supplierName}");
                sb.AppendLine($"📅 الفترة: من {dtpFrom.Value:yyyy/MM/dd} إلى {dtpTo.Value:yyyy/MM/dd}");
                sb.AppendLine("──────────────────────");
                sb.AppendLine("📝 *تفاصيل الحركات والمعاملات:*");

                int lineCount = 0;
                foreach (DataGridViewRow dgr in dgStatement.Rows)
                {
                    if (dgr.IsNewRow) continue;
                    string dtStr   = dgStatement.Columns.Contains("TransDate") ? (dgr.Cells["TransDate"]?.Value?.ToString() ?? "") : (dgr.Cells.Count > 0 ? dgr.Cells[0].Value?.ToString() ?? "" : "");
                    string typeStr = dgStatement.Columns.Contains("TransType") ? (dgr.Cells["TransType"]?.Value?.ToString() ?? "") : (dgr.Cells.Count > 1 ? dgr.Cells[1].Value?.ToString() ?? "" : "");
                    string debit   = dgStatement.Columns.Contains("Debit") ? (dgr.Cells["Debit"]?.Value?.ToString() ?? "") : (dgStatement.Columns.Contains("Paid") ? dgr.Cells["Paid"]?.Value?.ToString() ?? "" : "");
                    string credit  = dgStatement.Columns.Contains("Credit") ? (dgr.Cells["Credit"]?.Value?.ToString() ?? "") : (dgStatement.Columns.Contains("Purchases") ? dgr.Cells["Purchases"]?.Value?.ToString() ?? "" : "");
                    string bal     = dgStatement.Columns.Contains("Balance") ? (dgr.Cells["Balance"]?.Value?.ToString() ?? "") : "";
                    string details = dgStatement.Columns.Contains("Notes") ? (dgr.Cells["Notes"]?.Value?.ToString() ?? "") : (dgStatement.Columns.Contains("Details") ? (dgr.Cells["Details"]?.Value?.ToString() ?? "") : "");

                    string amountStr = "";
                    if (decimal.TryParse(debit, out decimal d) && d > 0) amountStr = $"🟢 مسدد: {d:N2} ج";
                    else if (decimal.TryParse(credit, out decimal c) && c > 0) amountStr = $"🔴 مشتريات: {c:N2} ج";

                    sb.AppendLine($"• {dtStr} | {typeStr}" + (!string.IsNullOrWhiteSpace(details) ? $" ({details})" : ""));
                    if (!string.IsNullOrWhiteSpace(amountStr)) sb.AppendLine($"   {amountStr} | 💰 الرصيد: {bal} ج");
                    else sb.AppendLine($"   💰 الرصيد: {bal} ج");

                    lineCount++;
                    if (lineCount >= 40 && dgStatement.Rows.Count > 45)
                    {
                        sb.AppendLine($"... ومتبقي {dgStatement.Rows.Count - 40} حركة أخرى (راجع كارت الصورة أو ملف الـ PDF المرفق)");
                        break;
                    }
                }

                sb.AppendLine("──────────────────────");
                sb.AppendLine($"📥 إجمالي المشتريات: {_totalPurchases:N2} ج");
                sb.AppendLine($"📤 إجمالي المسدد: {_totalPayments:N2} ج");
                sb.AppendLine("──────────────────────");
                string balStatus = _runBalance > 0 ? "رصيد مستحق للمورد" : (_runBalance < 0 ? "رصيد دائن لصالحنا" : "الحساب خالص ومطابق تماماً");
                sb.AppendLine($"💰 *صافي الرصيد: {Math.Abs(_runBalance):N2} ج ({balStatus})*");
                sb.AppendLine("──────────────────────");
                sb.AppendLine("مع تحيات إدارة الحسابات 🙏");

                WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                    this,
                    phone,
                    sb.ToString(),
                    () => ReceiptImageGenerator.GenerateTextCardImage("كشف حساب مورد", sb.ToString()),
                    "📱 إرسال كشف حساب المورد عبر الواتساب",
                    () => PdfReportHelper.GenerateSupplierStatementPdf(_supplierName, phone, dtpFrom.Value, dtpTo.Value, dgStatement, _totalPurchases, _totalPayments, _runBalance),
                    () => ReceiptImageGenerator.GenerateDetailedSupplierStatementImages(_supplierName, phone, dtpFrom.Value, dtpTo.Value, dgStatement, _totalPurchases, _totalPayments, _runBalance));
            };
            pnlFilter.Controls.Add(btnWhatsApp);

            var btnPay = Theme.MakeButton("💸 سداد/صرف نقدية", Color.FromArgb(140, 80, 0));
            btnPay.Size = new Size(140, 30);
            btnPay.Margin = new Padding(8, 0, 0, 0);
            btnPay.Click += (s, e) => OpenSupplierPaymentDialog();
            pnlFilter.Controls.Add(btnPay);

            this.Controls.Add(pnlFilter);

            // ===== DataGridView =====
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
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    Font = Theme.FontMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false
            };

            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate",  HeaderText = "التاريخ والوقت",    FillWeight = 55 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType",  HeaderText = "النوع",             FillWeight = 45 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit",     HeaderText = "مدين (علينا)",      FillWeight = 45 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit",      HeaderText = "دائن (سددنا)",      FillWeight = 45 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance",    HeaderText = "الرصيد الجاري",    FillWeight = 55 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",      HeaderText = "البيان",            FillWeight = 150 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransTypeRaw", Visible = false });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefID",        Visible = false });

            this.Controls.Add(dgStatement);

            // ===== Footer totals =====
            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Theme.BgCard,
                Padding = new Padding(8)
            };

            lblPurchases = new Label
            {
                Text = "إجمالي المشتريات: 0.00 ج",
                ForeColor = Color.OrangeRed,
                Location = new Point(680, 13),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            lblPayments = new Label
            {
                Text = "إجمالي المدفوعات: 0.00 ج",
                ForeColor = Color.LightGreen,
                Location = new Point(390, 13),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            lblBalance = new Label
            {
                Text = "صافي المديونية: 0.00 ج",
                ForeColor = Theme.Accent,
                Location = new Point(10, 13),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            pnlFoot.Controls.AddRange(new Control[] { lblPurchases, lblPayments, lblBalance });
            
            this.Controls.Clear();
            this.Controls.Add(dgStatement);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlFilter);

            pnlFilter.SendToBack();
            pnlFoot.SendToBack();
            dgStatement.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private void LoadSuppliersCombo()
        {
            try
            {
                _isLoadingCombo = true;
                DataTable dt = SupplierDAL.GetAll();
                if (dt != null)
                {
                    if (!dt.Columns.Contains("SupplierDisplayInfo"))
                    {
                        dt.Columns.Add("SupplierDisplayInfo", typeof(string));
                        foreach (DataRow r in dt.Rows)
                        {
                            string code = r.Table.Columns.Contains("SupplierCode") ? r["SupplierCode"].ToString() : "";
                            string phone = r.Table.Columns.Contains("Phone") && r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "";
                            r["SupplierDisplayInfo"] = string.IsNullOrEmpty(phone) ? $"{r["SupplierName"]} (كود: {code})" : $"{r["SupplierName"]}  |  📱 {phone}  |  (كود: {code})";
                        }
                    }
                    cmbSupplierSelector.DataSource = dt;
                    cmbSupplierSelector.DisplayMember = "SupplierDisplayInfo";
                    cmbSupplierSelector.ValueMember = "SupplierID";
                    cmbSupplierSelector.AutoCompleteSource = AutoCompleteSource.ListItems;
                    cmbSupplierSelector.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                }

                if (_supplierID > 0)
                {
                    cmbSupplierSelector.SelectedValue = _supplierID;
                }
                else if (dt != null && dt.Rows.Count > 0)
                {
                    _supplierID = Convert.ToInt32(dt.Rows[0]["SupplierID"]);
                    _supplierName = dt.Rows[0]["SupplierName"].ToString();
                    this.Text = "كشف حساب المورد - " + _supplierName;
                    cmbSupplierSelector.SelectedValue = _supplierID;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadSuppliersCombo failed", ex);
            }
            finally
            {
                _isLoadingCombo = false;
            }
        }

        private void CmbSupplierSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoadingCombo) return;
            if (cmbSupplierSelector.SelectedValue != null && cmbSupplierSelector.SelectedValue != DBNull.Value)
            {
                if (int.TryParse(cmbSupplierSelector.SelectedValue.ToString(), out int sid) && sid > 0)
                {
                    if (sid != _supplierID)
                    {
                        _supplierID = sid;
                        _supplierName = cmbSupplierSelector.Text;
                        this.Text = "كشف حساب المورد - " + _supplierName;
                        LoadStatement();
                    }
                }
            }
        }

        private void LoadStatement()
        {
            _dt = SupplierDAL.GetStatement(_supplierID, dtpFrom.Value, dtpTo.Value);
            dgStatement.Rows.Clear();
            _totalPurchases = 0;
            _totalPayments  = 0;

            // الرصيد السابق قبل بداية الفترة
            decimal prevBalance = SupplierDAL.GetPreviousBalance(_supplierID, dtpFrom.Value);
            _runBalance = prevBalance;

            if (prevBalance != 0)
            {
                string balLabel = prevBalance > 0 ? "مديونية سابقة" : "رصيد دائن سابق";
                dgStatement.Rows.Add(
                    "", balLabel,
                    prevBalance > 0 ? prevBalance.ToString("N2") : "",
                    prevBalance < 0 ? Math.Abs(prevBalance).ToString("N2") : "",
                    prevBalance.ToString("N2") + " ج",
                    "رصيد ما قبل " + dtpFrom.Value.ToString("dd/MM/yyyy"),
                    "Opening", 0);
                dgStatement.Rows[dgStatement.Rows.Count - 1].DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 100);
            }

            foreach (DataRow r in _dt.Rows)
            {
                // Credit = مشتريات = علينا (يزيد المديونية)
                // Debit  = مدفوعات = سددنا (يقلل المديونية)
                decimal cred = Convert.ToDecimal(r["Credit"]);
                decimal deb  = Convert.ToDecimal(r["Debit"]);
                _runBalance  = _runBalance + cred - deb;

                string typeStr = r["TransType"].ToString();
                int refID = r["RefID"] != DBNull.Value ? Convert.ToInt32(r["RefID"]) : 0;
                string notes = r["Notes"].ToString();

                if (typeStr == "Purchase") _totalPurchases += cred;
                else if (typeStr == "Payment") _totalPayments += deb;

                int rowIdx = dgStatement.Rows.Add(
                    Convert.ToDateTime(r["TransDate"]).ToString("dd/MM/yyyy HH:mm"),
                    TransTypeName(typeStr),
                    cred > 0 ? cred.ToString("N2") : "",
                    deb  > 0 ? deb.ToString("N2")  : "",
                    _runBalance.ToString("N2") + " ج",
                    notes,
                    typeStr,
                    refID);

                var rowStyle = dgStatement.Rows[rowIdx].DefaultCellStyle;
                if (typeStr == "Purchase")
                {
                    rowStyle.BackColor = Color.FromArgb(255, 242, 242);
                    rowStyle.ForeColor = Color.FromArgb(180, 30, 30);
                }
                else if (typeStr == "Payment")
                {
                    rowStyle.BackColor = Color.FromArgb(235, 250, 240);
                    rowStyle.ForeColor = Color.FromArgb(15, 120, 50);
                }
                else
                {
                    rowStyle.BackColor = Color.FromArgb(250, 250, 250);
                    rowStyle.ForeColor = Color.FromArgb(30, 40, 50);
                }
            }

            lblPurchases.Text = $"إجمالي المشتريات: {_totalPurchases:N2} ج";
            lblPayments.Text  = $"إجمالي المدفوعات: {_totalPayments:N2} ج";
            lblPurchases.ForeColor = Color.FromArgb(180, 30, 30);
            lblPayments.ForeColor  = Color.FromArgb(15, 120, 50);
            lblBalance.Text   = _runBalance >= 0
                ? $"صافي المديونية للمورد: {_runBalance:N2} ج"
                : $"رصيد دائن (المورد مدين لنا): {Math.Abs(_runBalance):N2} ج";
            lblBalance.ForeColor = _runBalance >= 0 ? Color.FromArgb(180, 30, 30) : Color.FromArgb(15, 120, 50);
        }

        private string TransTypeName(string t)
        {
            switch (t)
            {
                case "Purchase": return "فاتورة مشتريات";
                case "Payment":  return "دفعة للمورد";
                case "Opening":  return "رصيد افتتاحي";
                case "Discount": return "تسوية خصم";
                case "Addition": return "تسوية إضافة";
                default: return t;
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
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

                var titleFont    = new Font("Arial", 14, FontStyle.Bold);
                var subTitleFont = new Font("Arial", 9, FontStyle.Bold);
                var headerFont   = new Font("Arial", 9, FontStyle.Bold);
                var dataFont     = new Font("Arial", 8.5f, FontStyle.Regular);
                var boldDataFont = new Font("Arial", 8.5f, FontStyle.Bold);
                var itemFont     = new Font("Arial", 8f, FontStyle.Regular);
                var itemHeaderFont = new Font("Arial", 8f, FontStyle.Bold);

                var headerBgBrush = new SolidBrush(Color.FromArgb(15, 45, 90));
                var gridPen   = new Pen(Color.FromArgb(180, 190, 205), 1f);
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

                g.DrawString($"كشف حساب المورد التفصيلي: {_supplierName}", titleFont, Brushes.DarkBlue, new RectangleF(leftMargin, y + 4, tableWidth, 22), sfCenter);
                g.DrawString($"الفترة من: {dtpFrom.Value:dd/MM/yyyy}  إلى: {dtpTo.Value:dd/MM/yyyy}   |   تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", subTitleFont, Brushes.DimGray, new RectangleF(leftMargin, y + 25, tableWidth, 16), sfCenter);
                y += 55;

                // ── إعداد مواضع الأعمدة والترويسة ──
                // X offsets for vertical lines: 20, 135, 220, 295, 370, 465, 805
                int[] xCols = { 20, 135, 220, 295, 370, 465, 805 };
                string[] headers = { "التاريخ والوقت", "النوع", "مدين (سددنا)", "دائن (علينا)", "الرصيد الجاري", "البيان التفصيلي والأصناف" };

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
                    string typeRaw = row.Cells["TransTypeRaw"]?.Value?.ToString() ?? "";
                    int refID = row.Cells["RefID"]?.Value != null ? Convert.ToInt32(row.Cells["RefID"].Value) : 0;

                    DataTable dtItems = null;
                    if (typeRaw == "Purchase" && refID > 0)
                    {
                        dtItems = DbHelper.Query(@"
                            SELECT p.ProductName, pi2.Quantity, ISNULL(pi2.UnitName, p.Unit) AS Unit, pi2.UnitPrice, (pi2.Quantity * pi2.UnitPrice) AS Total
                            FROM PurchaseItems pi2
                            JOIN Products p ON pi2.ProductID = p.ProductID
                            WHERE pi2.PurchaseID = @id", DbHelper.P("@id", refID));
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
                    string notes   = row.Cells["Notes"].Value?.ToString() ?? "";

                    g.DrawString(dateStr, dataFont, Brushes.Black, new RectangleF(xCols[0], y, xCols[1] - xCols[0], 22), sfCenter);
                    g.DrawString(typeStr, boldDataFont, Brushes.DarkSlateGray, new RectangleF(xCols[1], y, xCols[2] - xCols[1], 22), sfCenter);
                    g.DrawString(debStr,  boldDataFont, Brushes.DarkGreen, new RectangleF(xCols[2], y, xCols[3] - xCols[2], 22), sfCenter);
                    g.DrawString(credStr, boldDataFont, Brushes.DarkRed, new RectangleF(xCols[3], y, xCols[4] - xCols[3], 22), sfCenter);
                    g.DrawString(balStr,  boldDataFont, Brushes.DarkBlue, new RectangleF(xCols[4], y, xCols[5] - xCols[4], 22), sfCenter);
                    g.DrawString(notes,   dataFont, Brushes.Black, new RectangleF(xCols[5] + 5, y + 2, xCols[6] - xCols[5] - 10, 20), sfRight);

                    y += 22;

                    // ── جدول فرعي تفصيلي للأصناف عند وجود فاتورة مشتريات ──
                    if (itemsCount > 0 && dtItems != null)
                    {
                        int subLeft = xCols[1] + 5;
                        int subWidth = xCols[6] - xCols[1] - 10;

                        // خلفية الجدول الفرعي للأصناف
                        g.FillRectangle(new SolidBrush(Color.FromArgb(242, 246, 252)), subLeft, y, subWidth, 18 + itemsCount * 17);
                        g.DrawRectangle(subGridPen, subLeft, y, subWidth, 18 + itemsCount * 17);

                        // أعمدة الجدول الفرعي: [اسم الصنف (45%)] [الكمية والوحدة (20%)] [سعر الوحدة (17%)] [الإجمالي (18%)]
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

                    float boxW = tableWidth / 3f;
                    var labelFont = new Font("Arial", 8.5f, FontStyle.Regular);
                    var valueFont = new Font("Arial", 11.5f, FontStyle.Bold);

                    // 1. المشتريات
                    g.DrawString("إجمالي المشتريات", labelFont, Brushes.DarkRed, new RectangleF(leftMargin, y + 4, boxW, 16), sfCenter);
                    g.DrawString($"{_totalPurchases:N2} ج", valueFont, Brushes.DarkRed, new RectangleF(leftMargin, y + 22, boxW, 22), sfCenter);
                    g.DrawLine(gridPen, leftMargin + boxW, y, leftMargin + boxW, y + 48);

                    // 2. المدفوعات
                    g.DrawString("إجمالي المدفوعات", labelFont, Brushes.DarkGreen, new RectangleF(leftMargin + boxW, y + 4, boxW, 16), sfCenter);
                    g.DrawString($"{_totalPayments:N2} ج", valueFont, Brushes.DarkGreen, new RectangleF(leftMargin + boxW, y + 22, boxW, 22), sfCenter);
                    g.DrawLine(gridPen, leftMargin + boxW * 2, y, leftMargin + boxW * 2, y + 48);

                    // 3. صافي المديونية
                    g.DrawString("صافي المديونية للمورد", labelFont, Brushes.DarkBlue, new RectangleF(leftMargin + boxW * 2, y + 4, boxW, 16), sfCenter);
                    g.DrawString($"{_runBalance:N2} ج", valueFont, Brushes.DarkBlue, new RectangleF(leftMargin + boxW * 2, y + 22, boxW, 22), sfCenter);
                }
            };

            var dlg = new PrintPreviewDialog { Document = pd, Width = 950, Height = 720 };
            dlg.ShowDialog();
        }

        private void OpenSupplierPaymentDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "💸 صرف نقدية للمورد - " + _supplierName;
                dlg.Size = new Size(420, 220);
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgCard;
                dlg.Font = Theme.FontMain;

                int dy = 20;
                dlg.Controls.Add(new Label { Text = "المبلغ المَصروف:", Location = new Point(270, dy + 3), Width = 110, ForeColor = Theme.TextMain, Font = Theme.FontBold });
                var nudAmt = new NumericUpDown
                {
                    Location = new Point(20, dy), Width = 240,
                    Maximum = 9999999, Minimum = 0, DecimalPlaces = 2,
                    BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 11, FontStyle.Bold)
                };
                dlg.Controls.Add(nudAmt); dy += 45;

                dlg.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(270, dy + 3), Width = 110, ForeColor = Theme.TextMain });
                var txtNote = new TextBox
                {
                    Location = new Point(20, dy), Width = 240,
                    BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                    Text = "سداد جزء من حساب المورد"
                };
                dlg.Controls.Add(txtNote); dy += 50;

                var btnOk = Theme.MakeButton("✅ تأكيد الصرف", 210, dy, 180, 38, Color.FromArgb(140, 80, 0));
                var btnCancel = Theme.MakeButton("❌ إلغاء", 20, dy, 160, 38, Color.DarkSlateGray);

                btnOk.Click += (s2, e2) =>
                {
                    if (nudAmt.Value <= 0) { MessageBox.Show("أدخل مبلغاً أكبر من صفر."); return; }
                    try
                    {
                        SupplierDAL.AddSupplierPayment(_supplierID, nudAmt.Value, txtNote.Text.Trim());
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadStatement();

                        new FrmPrintSupplierPayment(_supplierID, nudAmt.Value, txtNote.Text.Trim(), supplierName: _supplierName).ShowOptionsDialog(this);
                    }
                    catch { }
                };
                btnCancel.Click += (s2, e2) => dlg.Close();

                dlg.Controls.AddRange(new Control[] { btnOk, btnCancel });
                dlg.ShowDialog(this);
            }
        }
    }
}


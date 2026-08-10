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
        private DataGridView dgStatement;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnLoad, btnPrint;
        private Label lblPurchases, lblPayments, lblBalance;
        private DataTable _dt;
        private decimal _totalPurchases = 0;
        private decimal _totalPayments  = 0;
        private decimal _runBalance     = 0;

        public FrmSupplierStatement(int supplierID, string supplierName)
        {
            _supplierID   = supplierID;
            _supplierName = supplierName;
            InitUI();
            LoadStatement();
        }

        private void InitUI()
        {
            this.Text = "كشف حساب المورد - " + _supplierName;
            this.Size = new Size(980, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== Filter bar =====
            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.BgCard,
                Padding = new Padding(8)
            };

            pnlFilter.Controls.Add(new Label { Text = "من:", Location = new Point(745, 15), AutoSize = true, ForeColor = Theme.TextMain });
            dtpFrom = new DateTimePicker
            {
                Location = new Point(550, 11),
                Width = 190,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd   hh:mm tt",
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0)
            };
            dtpFrom.ValueChanged += (s, e) => LoadStatement();
            pnlFilter.Controls.Add(dtpFrom);

            pnlFilter.Controls.Add(new Label { Text = "إلى:", Location = new Point(510, 15), AutoSize = true, ForeColor = Theme.TextMain });
            dtpTo = new DateTimePicker
            {
                Location = new Point(315, 11),
                Width = 190,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd   hh:mm tt",
                Value = DateTime.Now
            };
            dtpTo.ValueChanged += (s, e) => LoadStatement();
            pnlFilter.Controls.Add(dtpTo);

            btnLoad = Theme.MakeButton("🔍 عرض", 260, 10, 75, 30, Theme.Accent);
            btnLoad.Click += (s, e) => LoadStatement();
            pnlFilter.Controls.Add(btnLoad);

            btnPrint = Theme.MakeButton("🖨 طباعة", 165, 10, 85, 30, Theme.Primary);
            btnPrint.Click += BtnPrint_Click;
            pnlFilter.Controls.Add(btnPrint);

            var btnPay = Theme.MakeButton("💸 سداد/صرف نقدية", 10, 10, 145, 30, Color.FromArgb(140, 80, 0));
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

                Color rowColor = Theme.TextMain;
                if (typeStr == "Purchase") rowColor = Color.OrangeRed;
                else if (typeStr == "Payment") rowColor = Color.LightGreen;

                int rowIdx = dgStatement.Rows.Add(
                    Convert.ToDateTime(r["TransDate"]).ToString("dd/MM/yyyy HH:mm"),
                    TransTypeName(typeStr),
                    cred > 0 ? cred.ToString("N2") : "",
                    deb  > 0 ? deb.ToString("N2")  : "",
                    _runBalance.ToString("N2") + " ج",
                    notes,
                    typeStr,
                    refID);

                dgStatement.Rows[rowIdx].DefaultCellStyle.ForeColor = rowColor;
            }

            lblPurchases.Text = $"إجمالي المشتريات: {_totalPurchases:N2} ج";
            lblPayments.Text  = $"إجمالي المدفوعات: {_totalPayments:N2} ج";
            lblBalance.Text   = _runBalance >= 0
                ? $"صافي المديونية للمورد: {_runBalance:N2} ج"
                : $"رصيد دائن (المورد مدين لنا): {Math.Abs(_runBalance):N2} ج";
            lblBalance.ForeColor = _runBalance >= 0 ? Color.OrangeRed : Color.LightGreen;
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
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            int currentRowIndex = 0;

            pd.BeginPrint += (s, ev) => { currentRowIndex = 0; };

            pd.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                var titleFont  = new Font("Arial", 13, FontStyle.Bold);
                var headerFont = new Font("Arial", 9,  FontStyle.Bold);
                var dataFont   = new Font("Arial", 8.5f);
                int y = 20;

                g.DrawString($"كشف حساب المورد: {_supplierName}", titleFont, Brushes.DarkBlue, 280, y); y += 28;
                g.DrawString($"من: {dtpFrom.Value:dd/MM/yyyy}  إلى: {dtpTo.Value:dd/MM/yyyy}", dataFont, Brushes.Black, 300, y); y += 22;
                g.DrawLine(Pens.DarkBlue, 20, y, 800, y); y += 5;

                int[] cols = { 20, 130, 215, 295, 380, 470 };
                string[] hdrs = { "التاريخ والوقت", "النوع", "مدين (علينا)", "دائن (سددنا)", "الرصيد الجاري", "البيان" };
                for (int i = 0; i < hdrs.Length; i++)
                    g.DrawString(hdrs[i], headerFont, Brushes.DarkBlue, cols[i], y);
                y += 20;
                g.DrawLine(Pens.Gray, 20, y, 800, y); y += 5;

                ev.HasMorePages = false;
                while (currentRowIndex < dgStatement.Rows.Count)
                {
                    if (y + 18 > ev.PageBounds.Height - 80) { ev.HasMorePages = true; return; }
                    var row = dgStatement.Rows[currentRowIndex];
                    g.DrawString(row.Cells["TransDate"].Value?.ToString() ?? "", dataFont, Brushes.Black, cols[0], y);
                    g.DrawString(row.Cells["TransType"].Value?.ToString() ?? "", dataFont, Brushes.Black, cols[1], y);
                    g.DrawString(row.Cells["Credit"].Value?.ToString()    ?? "", dataFont, Brushes.Black, cols[2], y);
                    g.DrawString(row.Cells["Debit"].Value?.ToString()     ?? "", dataFont, Brushes.Black, cols[3], y);
                    g.DrawString(row.Cells["Balance"].Value?.ToString()   ?? "", dataFont, Brushes.Black, cols[4], y);
                    g.DrawString(row.Cells["Notes"].Value?.ToString()     ?? "", dataFont, Brushes.Black, cols[5], y);
                    y += 18;
                    currentRowIndex++;
                }

                // Footer summary
                y += 10;
                if (y + 50 < ev.PageBounds.Height)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 244, 248)), 20, y, 780, 48);
                    g.DrawRectangle(new Pen(Color.FromArgb(200, 214, 228), 1.5f), 20, y, 780, 48);

                    var lbf = new Font("Arial", 8.5f);
                    var vbf = new Font("Arial", 11,  FontStyle.Bold);
                    g.DrawString("إجمالي المشتريات",  lbf, Brushes.DarkRed,   30,  y + 5);
                    g.DrawString($"{_totalPurchases:N2} ج", vbf, Brushes.DarkRed, 30, y + 22);
                    g.DrawLine(new Pen(Color.LightGray, 1f), 215, y + 4, 215, y + 44);
                    g.DrawString("إجمالي المدفوعات", lbf, Brushes.DarkGreen, 225, y + 5);
                    g.DrawString($"{_totalPayments:N2} ج",  vbf, Brushes.DarkGreen, 225, y + 22);
                    g.DrawLine(new Pen(Color.LightGray, 1f), 410, y + 4, 410, y + 44);
                    g.DrawString("صافي المديونية",   lbf, Brushes.DarkBlue,  420, y + 5);
                    g.DrawString($"{_runBalance:N2} ج",     vbf, Brushes.DarkBlue,  420, y + 22);
                }
            };

            new PrintPreviewDialog { Document = pd, Width = 950, Height = 720 }.ShowDialog();
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


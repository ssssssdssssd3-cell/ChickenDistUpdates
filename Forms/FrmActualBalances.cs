using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة النقدية والسيولة الفعلية المتاحة بكافة الحسابات والأدراج والفيزا</summary>
    public class FrmActualBalances : Form
    {
        private DataGridView _dg;
        private Label _lblTotalCash, _lblTotalBank, _lblTotalVisa, _lblTotalLiquidity;
        private DateTimePicker _dtpAsOf;
        private Button _btnRefresh, _btnPrint, _btnWhatsApp, _btnManageAccounts;

        public FrmActualBalances()
        {
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            Text            = "النقدية والسيولة الفعلية المتاحة";
            BackColor       = Theme.BgMain;
            RightToLeft     = RightToLeft.Yes;
            RightToLeftLayout = true;
            Font            = Theme.FontMain;
            Size            = new Size(1100, 720);
            StartPosition   = FormStartPosition.CenterScreen;

            // ── Title Bar ──────────────────────────────────────────────────────
            var titleBar = Theme.MakeTitleBar(
                "💵 النقدية والسيولة الفعلية المتاحة",
                "الأرصدة الحقيقية المتاحة في كافة الخزائن والأدراج والفيزا والبنوك بعد التقفيل وكافة الحركات");
            Controls.Add(titleBar);

            // ── Top Toolbar ────────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = Theme.BgCard,
                Padding   = new Padding(10, 8, 10, 8)
            };

            var lblDate = new Label
            {
                Text      = "حتى تاريخ:",
                Font      = Theme.FontBold,
                ForeColor = Theme.TextMain,
                AutoSize  = true,
                Dock      = DockStyle.Right,
                Margin    = new Padding(0, 4, 12, 0)
            };

            _dtpAsOf = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today,
                Width  = 130,
                Dock   = DockStyle.Right
            };
            _dtpAsOf.RightToLeftLayout = true;
            _dtpAsOf.ValueChanged += (s, e) => LoadData();

            _btnRefresh = Theme.MakeButton("🔄 تحديث", Theme.Accent);
            _btnRefresh.Size   = new Size(110, 34);
            _btnRefresh.Dock   = DockStyle.Right;
            _btnRefresh.Click += (s, e) => LoadData();

            _btnPrint = Theme.MakeButton("🖨️ طباعة التقرير", Theme.Primary);
            _btnPrint.Size   = new Size(130, 34);
            _btnPrint.Dock   = DockStyle.Right;
            _btnPrint.Click += BtnPrint_Click;

            _btnWhatsApp = Theme.MakeButton("📲 إرسال للواتساب", Theme.Accent);
            _btnWhatsApp.Size   = new Size(140, 34);
            _btnWhatsApp.BackColor = Color.FromArgb(37, 211, 102);
            _btnWhatsApp.Dock   = DockStyle.Right;
            _btnWhatsApp.Click += BtnWhatsApp_Click;

            _btnManageAccounts = Theme.MakeButton("💳 إدارة الخزائن والحسابات", Color.FromArgb(70, 70, 150));
            _btnManageAccounts.Size = new Size(180, 34);
            _btnManageAccounts.Dock = DockStyle.Left;
            _btnManageAccounts.Visible = Session.IsAdmin || Session.CanAccess("SafeAccounts");
            _btnManageAccounts.Click += (s, e) =>
            {
                new FrmSafeAccounts().ShowDialog(this);
                LoadData();
            };

            toolbar.Controls.AddRange(new Control[] { lblDate, _dtpAsOf, _btnRefresh, _btnPrint, _btnWhatsApp, _btnManageAccounts });
            Controls.Add(toolbar);

            // ── KPI Summary Cards Panel ────────────────────────────────────────
            var pnlKpis = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                Height      = 90,
                ColumnCount = 4,
                RowCount    = 1,
                Padding     = new Padding(10, 8, 10, 8),
                BackColor   = Theme.BgMain
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _lblTotalCash      = MakeKpiCard(pnlKpis, 0, "💵 النقدية والأدراج", "0.00 ج", Color.FromArgb(46, 204, 113));
            _lblTotalBank      = MakeKpiCard(pnlKpis, 1, "🏦 الحسابات البنكية", "0.00 ج", Color.FromArgb(52, 152, 219));
            _lblTotalVisa      = MakeKpiCard(pnlKpis, 2, "💳 ماكينات الفيزا/الشبكة", "0.00 ج", Color.FromArgb(155, 89, 182));
            _lblTotalLiquidity = MakeKpiCard(pnlKpis, 3, "💎 إجمالي السيولة المتاحة", "0.00 ج", Color.FromArgb(241, 196, 15));

            Controls.Add(pnlKpis);

            // ── DataGridView ──────────────────────────────────────────────────
            _dg = new DataGridView
            {
                Dock                          = DockStyle.Fill,
                BackgroundColor               = Theme.BgCard,
                BorderStyle                   = BorderStyle.None,
                RowHeadersVisible             = false,
                AllowUserToAddRows            = false,
                AllowUserToOrderColumns       = true,
                ReadOnly                      = true,
                SelectionMode                 = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft                   = RightToLeft.Yes,
                GridColor                     = Theme.BorderColor,
                AutoSizeColumnsMode           = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight           = 42,
                ColumnHeadersHeightSizeMode   = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles     = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Theme.BgCard,
                    ForeColor          = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font               = Theme.FontMain,
                    Alignment          = DataGridViewContentAlignment.MiddleCenter
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor  = Theme.Primary,
                    ForeColor  = Color.White,
                    Font       = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    Alignment  = DataGridViewContentAlignment.MiddleCenter
                }
            };

            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountID", HeaderText = "ID", Visible = false });
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "اسم الخزينة / الحساب", FillWeight = 160 });
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountType", HeaderText = "نوع الحساب", FillWeight = 100 });
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountNumber", HeaderText = "رقم الحساب/الماكينة", FillWeight = 110 });
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "OpeningBalance", HeaderText = "الرصيد الافتتاحي", FillWeight = 100 });
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalIn", HeaderText = "إجمالي الوارد (+)", FillWeight = 100 });
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalOut", HeaderText = "إجمالي الصادر (-)", FillWeight = 100 });
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualBalance", HeaderText = "الرصيد الفعلي المتاح", FillWeight = 140 });
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastTransDate", HeaderText = "آخر حركة", FillWeight = 110 });

            _dg.CellDoubleClick += (s, e) => OpenSelectedAccountCashBox();

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGrid.Controls.Add(_dg);
            Controls.Add(pnlGrid);

            // ── Footer Hint ──────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 36,
                BackColor = Theme.BgCard
            };
            var lblHint = new Label
            {
                Text      = "💡 انقر مرتين على أي حساب لعرض كشف الحركة التفصيلي الخاص به في حركة الخزنة.",
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Theme.Accent,
                AutoSize  = true,
                Location  = new Point(15, 8)
            };
            pnlFooter.Controls.Add(lblHint);
            Controls.Add(pnlFooter);

            // Ensure Z-Order
            titleBar.BringToFront();
            toolbar.BringToFront();
            pnlKpis.BringToFront();
            pnlGrid.BringToFront();
            pnlFooter.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private Label MakeKpiCard(TableLayoutPanel parent, int col, string title, string defaultVal, Color accentColor)
        {
            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin    = new Padding(4),
                Padding   = new Padding(10, 6, 10, 6)
            };

            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(accentColor, 3f))
                    e.Graphics.DrawLine(pen, 0, 0, card.Width, 0);
            };

            var lblTitle = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock      = DockStyle.Top,
                Height = 22
            };

            var lblVal = new Label
            {
                Text      = defaultVal,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = accentColor,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(lblVal);
            card.Controls.Add(lblTitle);
            parent.Controls.Add(card, col, 0);
            return lblVal;
        }

        private void LoadData()
        {
            try
            {
                _dg.Rows.Clear();
                DateTime asOfDate = _dtpAsOf.Value.Date.AddDays(1).AddSeconds(-1);

                string sql = @"
                    SELECT 
                        sa.AccountID,
                        sa.AccountName,
                        sa.AccountType,
                        sa.AccountNumber,
                        sa.OpeningBalance,
                        ISNULL(SUM(CASE WHEN cb.TransDate <= @toDate THEN cb.AmountIn ELSE 0 END), 0) AS TotalIn,
                        ISNULL(SUM(CASE WHEN cb.TransDate <= @toDate THEN cb.AmountOut ELSE 0 END), 0) AS TotalOut,
                        MAX(CASE WHEN cb.TransDate <= @toDate THEN cb.TransDate ELSE NULL END) AS LastTransDate
                    FROM SafeAccounts sa
                    LEFT JOIN CashBox cb ON (sa.AccountID = cb.AccountID OR (sa.AccountID = 1 AND (cb.AccountID IS NULL OR cb.AccountID <= 0)))
                    WHERE sa.IsActive = 1
                    GROUP BY sa.AccountID, sa.AccountName, sa.AccountType, sa.AccountNumber, sa.OpeningBalance
                    ORDER BY sa.AccountID ASC";

                var dt = DbHelper.Query(sql, DbHelper.P("@toDate", asOfDate));

                decimal totalCash = 0, totalBank = 0, totalVisa = 0;
                bool canViewBalance = Session.CanViewBalance("CashBox");

                foreach (DataRow r in dt.Rows)
                {
                    int accID = Convert.ToInt32(r["AccountID"]);
                    if (!Session.IsSafeAllowed(accID)) continue;

                    string name = r["AccountName"].ToString().Replace(" / الدرج", "").Replace("/ الدرج", "").Replace("/الدرج", "").Replace(" / درج", "").Trim();
                    string type = r["AccountType"]?.ToString() ?? "Cash";
                    string num = r["AccountNumber"] != DBNull.Value ? r["AccountNumber"].ToString() : "-";
                    decimal opening = Convert.ToDecimal(r["OpeningBalance"]);
                    decimal totIn = Convert.ToDecimal(r["TotalIn"]);
                    decimal totOut = Convert.ToDecimal(r["TotalOut"]);
                    decimal actualBalance = opening + totIn - totOut;

                    string lastDateStr = r["LastTransDate"] != DBNull.Value 
                        ? Convert.ToDateTime(r["LastTransDate"]).ToString("dd/MM/yyyy HH:mm") 
                        : "لا توجد حركات";

                    string typeArabic = type switch
                    {
                        "Cash" => "💵 خزنة نقدية",
                        "Bank" => "🏦 حساب بنكي",
                        "Visa" => "💳 ماكينة فيزا / شبكة",
                        _ => "💵 خزنة نقدية"
                    };

                    if (type == "Bank") totalBank += actualBalance;
                    else if (type == "Visa") totalVisa += actualBalance;
                    else totalCash += actualBalance;

                    string actualText = canViewBalance ? actualBalance.ToString("N2") + " ج" : "*** 🔒";
                    string openText   = canViewBalance ? opening.ToString("N2") : "*** 🔒";
                    string inText     = canViewBalance ? totIn.ToString("N2") : "*** 🔒";
                    string outText    = canViewBalance ? totOut.ToString("N2") : "*** 🔒";

                    int ri = _dg.Rows.Add(
                        accID,
                        name,
                        typeArabic,
                        num,
                        openText,
                        inText,
                        outText,
                        actualText,
                        lastDateStr
                    );

                    var row = _dg.Rows[ri];
                    row.Cells["ActualBalance"].Style.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                    if (actualBalance < 0)
                        row.Cells["ActualBalance"].Style.ForeColor = Color.OrangeRed;
                    else if (actualBalance > 0)
                        row.Cells["ActualBalance"].Style.ForeColor = Theme.Success;
                }

                decimal totalLiquidity = totalCash + totalBank + totalVisa;

                if (canViewBalance)
                {
                    _lblTotalCash.Text      = totalCash.ToString("N2") + " ج";
                    _lblTotalBank.Text      = totalBank.ToString("N2") + " ج";
                    _lblTotalVisa.Text      = totalVisa.ToString("N2") + " ج";
                    _lblTotalLiquidity.Text = totalLiquidity.ToString("N2") + " ج";
                }
                else
                {
                    _lblTotalCash.Text      = "*** 🔒 (محجوب)";
                    _lblTotalBank.Text      = "*** 🔒 (محجوب)";
                    _lblTotalVisa.Text      = "*** 🔒 (محجوب)";
                    _lblTotalLiquidity.Text = "*** 🔒 (محجوب)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل بيان النقدية المتاحة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSelectedAccountCashBox()
        {
            if (_dg.CurrentRow != null && _dg.CurrentRow.Cells["AccountID"].Value != null)
            {
                int accID = Convert.ToInt32(_dg.CurrentRow.Cells["AccountID"].Value);
                var f = new FrmCashBox();
                f.ShowDialog(this);
                LoadData();
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            try
            {
                var pd = new PrintDocument();
                pd.PrintController = new StandardPrintController();
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
                pd.PrintPage += (s, pe) =>
                {
                    Graphics g = pe.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Font fComp  = new Font("Arial", 12, FontStyle.Bold);
                    Font fTitle = new Font("Arial", 15, FontStyle.Bold);
                    Font fSub   = new Font("Arial", 9, FontStyle.Regular);
                    Font fHead  = new Font("Arial", 9.5f, FontStyle.Bold);
                    Font fBody  = new Font("Arial", 9, FontStyle.Regular);
                    Font fBold  = new Font("Arial", 9.5f, FontStyle.Bold);

                    var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    var sfRtlRight = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap };

                    int startX = 25;
                    int pageW = 770;
                    float y = 25;

                    string compName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "شركة قطع غيار وتوزيع";
                    g.DrawString(compName, fComp, Brushes.DarkBlue, new RectangleF(startX, y, pageW, 20), sfCenter); y += 22;
                    g.DrawString("تقرير النقدية والأرصدة الفعلية المتاحة", fTitle, Brushes.Black, new RectangleF(startX, y, pageW, 28), sfCenter); y += 28;
                    g.DrawString($"حتى تاريخ: {_dtpAsOf.Value:dd/MM/yyyy}   |   تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", fSub, Brushes.DarkSlateGray, new RectangleF(startX, y, pageW, 18), sfCenter); y += 22;

                    g.DrawLine(new Pen(Color.FromArgb(24, 43, 73), 1.5f), startX, y, startX + pageW, y); y += 10;

                    // Table Columns (RTL): الحساب/الخزنة (230), النوع (100), الافتتاحي (110), الوارد (110), الصادر (110), الرصيد الفعلي (110) = 770
                    int[] colWidths = { 230, 100, 110, 110, 110, 110 };
                    string[] headers = { "الحساب / الخزنة", "النوع", "الافتتاحي", "الوارد", "الصادر", "الرصيد الفعلي" };

                    int headH = 28;
                    int rowH = 25;

                    var brushHeaderBg = new SolidBrush(Color.FromArgb(24, 43, 73));
                    var brushRowAlt = new SolidBrush(Color.FromArgb(248, 250, 252));
                    var penGrid = new Pen(Color.FromArgb(170, 185, 205), 1f);
                    var penDark = new Pen(Color.FromArgb(24, 43, 73), 1.5f);

                    g.FillRectangle(brushHeaderBg, startX, y, pageW, headH);
                    g.DrawRectangle(penDark, startX, y, pageW, headH);

                    float curX = startX + pageW;
                    for (int i = 0; i < headers.Length; i++)
                    {
                        curX -= colWidths[i];
                        var hRect = new RectangleF(curX, y, colWidths[i], headH);
                        g.DrawRectangle(penGrid, curX, y, colWidths[i], headH);
                        g.DrawString(headers[i], fHead, Brushes.White, hRect, sfCenter);
                    }
                    y += headH;

                    int rIdx = 0;
                    foreach (DataGridViewRow row in _dg.Rows)
                    {
                        if (row.IsNewRow) continue;
                        string name   = row.Cells["AccountName"].Value?.ToString() ?? "";
                        string type   = row.Cells["AccountType"].Value?.ToString() ?? "";
                        string open   = row.Cells["OpeningBalance"].Value?.ToString() ?? "0";
                        string inAmt  = row.Cells["TotalIn"].Value?.ToString() ?? "0";
                        string outAmt = row.Cells["TotalOut"].Value?.ToString() ?? "0";
                        string bal    = row.Cells["ActualBalance"].Value?.ToString() ?? "0";

                        Brush bgBrush = (rIdx % 2 == 1) ? brushRowAlt : Brushes.White;
                        g.FillRectangle(bgBrush, startX, y, pageW, rowH);
                        g.DrawRectangle(penGrid, startX, y, pageW, rowH);

                        curX = startX + pageW;

                        // Col 0: Name (Right aligned)
                        curX -= colWidths[0];
                        g.DrawRectangle(penGrid, curX, y, colWidths[0], rowH);
                        g.DrawString(name, fBody, Brushes.Black, new RectangleF(curX + 5, y, colWidths[0] - 10, rowH), sfRtlRight);

                        // Col 1: Type
                        curX -= colWidths[1];
                        g.DrawRectangle(penGrid, curX, y, colWidths[1], rowH);
                        g.DrawString(type, fBody, Brushes.Black, new RectangleF(curX, y, colWidths[1], rowH), sfCenter);

                        // Col 2: Open
                        curX -= colWidths[2];
                        g.DrawRectangle(penGrid, curX, y, colWidths[2], rowH);
                        g.DrawString(open, fBody, Brushes.Black, new RectangleF(curX, y, colWidths[2], rowH), sfCenter);

                        // Col 3: In
                        curX -= colWidths[3];
                        g.DrawRectangle(penGrid, curX, y, colWidths[3], rowH);
                        g.DrawString(inAmt, fBody, Brushes.DarkGreen, new RectangleF(curX, y, colWidths[3], rowH), sfCenter);

                        // Col 4: Out
                        curX -= colWidths[4];
                        g.DrawRectangle(penGrid, curX, y, colWidths[4], rowH);
                        g.DrawString(outAmt, fBody, Brushes.DarkRed, new RectangleF(curX, y, colWidths[4], rowH), sfCenter);

                        // Col 5: Actual Balance
                        curX -= colWidths[5];
                        g.DrawRectangle(penGrid, curX, y, colWidths[5], rowH);
                        g.DrawString(bal, fBold, Brushes.DarkBlue, new RectangleF(curX, y, colWidths[5], rowH), sfCenter);

                        y += rowH;
                        rIdx++;
                    }

                    y += 12;
                    g.DrawLine(new Pen(Color.FromArgb(24, 43, 73), 1.5f), startX, y, startX + pageW, y); y += 8;
                    g.DrawString($"💎 إجمالي السيولة الكلية المتاحة: {_lblTotalLiquidity.Text}", fTitle, Brushes.DarkGreen, new RectangleF(startX, y, pageW, 28), sfRtlRight);
                };

                using (var dlg = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700, Text = "طباعة تقرير النقدية والسيولة" })
                {
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ عند طباعة تقرير النقدية: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnWhatsApp_Click(object sender, EventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("💵 *تقرير النقدية والسيولة الفعلية المتاحة*");
                sb.AppendLine($"📅 حتى تاريخ: {_dtpAsOf.Value:dd/MM/yyyy}");
                sb.AppendLine("------------------------------------");

                foreach (DataGridViewRow row in _dg.Rows)
                {
                    string name = row.Cells["AccountName"].Value?.ToString();
                    string bal  = row.Cells["ActualBalance"].Value?.ToString();
                    sb.AppendLine($"🔹 {name}: *{bal}*");
                }

                sb.AppendLine("------------------------------------");
                sb.AppendLine($"💎 *إجمالي السيولة الكلية:* {_lblTotalLiquidity.Text}");

                Clipboard.SetText(sb.ToString());
                MessageBox.Show("✅ تم نسخ ملخص النقدية الفعليه إلى الحافظة بنجاح!\nيمكنك لصق التقرير في الواتساب فوراً.", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }
    }
}

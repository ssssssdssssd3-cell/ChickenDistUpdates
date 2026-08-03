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
            _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "اسم الخزنة / الدرج / الحساب", FillWeight = 160 });
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
                Height    = 22
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
                    LEFT JOIN CashBox cb ON sa.AccountID = cb.AccountID
                    WHERE sa.IsActive = 1
                    GROUP BY sa.AccountID, sa.AccountName, sa.AccountType, sa.AccountNumber, sa.OpeningBalance
                    ORDER BY sa.AccountID ASC";

                var dt = DbHelper.Query(sql, DbHelper.P("@toDate", asOfDate));

                decimal totalCash = 0, totalBank = 0, totalVisa = 0;
                bool canViewBalance = Session.CanViewBalance("CashBox");

                foreach (DataRow r in dt.Rows)
                {
                    int accID = Convert.ToInt32(r["AccountID"]);
                    string name = r["AccountName"].ToString();
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
                        "Cash" => "💵 خزنة نقدية / درج",
                        "Bank" => "🏦 حساب بنكي",
                        "Visa" => "💳 ماكينة فيزا / شبكة",
                        _ => "💰 حساب مالي"
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
                pd.PrintPage += (s, pe) =>
                {
                    Graphics g = pe.Graphics;
                    Font fTitle = new Font("Segoe UI", 14, FontStyle.Bold);
                    Font fSub   = new Font("Segoe UI", 10, FontStyle.Bold);
                    Font fBody  = new Font("Segoe UI", 9);
                    Brush b     = Brushes.Black;

                    float y = 40;
                    g.DrawString("💵 تقرير النقدية والأرصدة الفعلية المتاحة", fTitle, b, new PointF(200, y));
                    y += 30;
                    g.DrawString($"حتى تاريخ: {_dtpAsOf.Value:dd/MM/yyyy}  |  تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", fSub, b, new PointF(180, y));
                    y += 40;

                    g.DrawString("الحساب / الخزنة                     النوع             الافتتاحي           الوارد            الصادر           الرصيد الفعلي", fSub, b, new PointF(40, y));
                    y += 25;
                    g.DrawLine(Pens.Black, 40, y, 750, y);
                    y += 10;

                    foreach (DataGridViewRow row in _dg.Rows)
                    {
                        string name   = row.Cells["AccountName"].Value?.ToString() ?? "";
                        string type   = row.Cells["AccountType"].Value?.ToString() ?? "";
                        string open   = row.Cells["OpeningBalance"].Value?.ToString() ?? "";
                        string inAmt  = row.Cells["TotalIn"].Value?.ToString() ?? "";
                        string outAmt = row.Cells["TotalOut"].Value?.ToString() ?? "";
                        string bal    = row.Cells["ActualBalance"].Value?.ToString() ?? "";

                        if (name.Length > 22) name = name.Substring(0, 22);

                        g.DrawString($"{name,-24} {type,-12} {open,10} {inAmt,10} {outAmt,10} {bal,12}", fBody, b, new PointF(40, y));
                        y += 22;
                    }

                    y += 15;
                    g.DrawLine(Pens.Black, 40, y, 750, y);
                    y += 15;
                    g.DrawString($"إجمالي السيولة الكلية المتاحة: {_lblTotalLiquidity.Text}", fTitle, Brushes.DarkGreen, new PointF(40, y));
                };

                using (var dlg = new PrintPreviewDialog { Document = pd, Width = 800, Height = 600 })
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

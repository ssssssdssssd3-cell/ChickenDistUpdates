using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة التفاصيل والإحصائيات السريعة للنظام (تحميل خفيف وسريع)</summary>
    public class FrmQuickDetails : Form
    {
        private Panel pnlHeader;
        private FlowLayoutPanel pnlCards;
        private DataGridView dgRecent;
        private Panel pnlChart;
        private Button btnRefresh, btnClose;
        private Action<Form> _navigateAction;

        private decimal[] _chartValues = new decimal[7];
        private string[] _chartDays = new string[7];

        public FrmQuickDetails(Action<Form> navigateAction = null)
        {
            _navigateAction = navigateAction;
            InitUI();
            this.Shown += async (s, e) => await LoadDataAsync();
        }

        private void InitUI()
        {
            this.Text = "📊 التفاصيل والإحصائيات السريعة للنظام";
            this.Size = new Size(950, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. Header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };
            var lblTitle = new Label
            {
                Text = "📊 التفاصيل والإحصائيات السريعة والتحليلات",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            btnRefresh = Theme.MakeButton("🔄 تحديث البيانات", Theme.Primary, new Point(780, 12), new Size(135, 34));
            btnRefresh.Click += async (s, e) => await LoadDataAsync();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnRefresh);

            // 2. Cards (FlowLayout)
            pnlCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 115,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Theme.BgMain,
                Padding = new Padding(10, 5, 10, 5)
            };

            // 3. Main Split (Table Layout: Left Recent Sales, Right Chart)
            var tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain,
                Padding = new Padding(10)
            };
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // 3a. Recent Invoices DataGridView
            var pnlRecentGroup = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(10), Margin = new Padding(5) };
            var lblRecentTitle = new Label { Text = "📋 آخر 10 فواتير مبيعات محررة اليوم", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain };
            
            dgRecent = new DataGridView
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
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", HeaderText = "كود الفاتورة", FillWeight = 35 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleType", HeaderText = "النوع", FillWeight = 30 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "العميل / المندوب", FillWeight = 55 });
            dgRecent.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "المبلغ الإجمالي", FillWeight = 40 });

            pnlRecentGroup.Controls.Add(dgRecent);
            pnlRecentGroup.Controls.Add(lblRecentTitle);
            tblMain.Controls.Add(pnlRecentGroup, 0, 0);

            // 3b. Chart Panel
            var pnlChartGroup = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(10), Margin = new Padding(5) };
            pnlChart = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };
            pnlChart.Paint += PnlChart_Paint;
            pnlChartGroup.Controls.Add(pnlChart);
            tblMain.Controls.Add(pnlChartGroup, 1, 0);

            // 4. Footer Panel
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Theme.BgCard };
            btnClose = Theme.MakeButton("إغلاق", Color.FromArgb(108, 117, 125), new Point(15, 8), new Size(100, 34));
            btnClose.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnClose);

            this.Controls.Add(tblMain);
            this.Controls.Add(pnlCards);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        private async Task LoadDataAsync()
        {
            btnRefresh.Enabled = false;
            pnlCards.Controls.Clear();
            pnlCards.Controls.Add(new Label { Text = "جاري تحميل البيانات الإحصائية...", Font = Theme.FontMain, ForeColor = Theme.TextGray, AutoSize = true, Margin = new Padding(15) });

            try
            {
                decimal cashBal = 0, todaySales = 0;
                int openLoadsCount = 0, belowMinCount = 0;
                DataTable dtSales = null, salesDtChart = null;

                await Task.Run(() =>
                {
                    try { if (Session.CanAccess("DashTreasury")) cashBal = AccountDAL.GetCashBalance(); } catch { }
                    try
                    {
                        if (Session.CanAccess("DashSales"))
                        {
                            var dtToday = ReportDAL.SalesByDay(DateTime.Today, DateTime.Today);
                            todaySales = dtToday.Rows.Count > 0 ? Convert.ToDecimal(dtToday.Rows[0]["Total"]) : 0;
                        }
                    }
                    catch { }
                    try { if (Session.CanAccess("DashLoads")) openLoadsCount = DriverDAL.GetOpenLoads().Rows.Count; } catch { }
                    try { if (Session.CanAccess("DashBelowMin")) belowMinCount = InventoryDAL.GetBelowMinStockCount(); } catch { }
                    try { dtSales = SaleDAL.GetAll(DateTime.Today, DateTime.Today); } catch { }
                    try { salesDtChart = ReportDAL.SalesByDay(DateTime.Today.AddDays(-6), DateTime.Today); } catch { }
                });

                pnlCards.Controls.Clear();

                if (Session.CanAccess("DashTreasury"))
                    pnlCards.Controls.Add(MakeCard("💰 رصيد الخزنة الحالي", cashBal.ToString("N2") + " ج", Theme.Success));

                if (Session.CanAccess("DashSales"))
                    pnlCards.Controls.Add(MakeCard("🛒 مبيعات اليوم", todaySales.ToString("N2") + " ج", Theme.Accent));

                if (Session.CanAccess("DashLoads"))
                    pnlCards.Controls.Add(MakeCard("🚚 حمولات مفتوحة حالياً", openLoadsCount + " حمولة", Color.FromArgb(52, 152, 219)));

                if (Session.CanAccess("DashBelowMin"))
                {
                    var cardBelowMin = MakeCard("🔴 أصناف تحت حد الطلب", belowMinCount + " صنف", Theme.Danger);
                    if (_navigateAction != null)
                    {
                        cardBelowMin.Click += (s, e) => { this.Close(); _navigateAction(new FrmInventory(true)); };
                        foreach (Control child in cardBelowMin.Controls)
                        {
                            child.Click += (s, e) => { this.Close(); _navigateAction(new FrmInventory(true)); };
                            child.Cursor = Cursors.Hand;
                        }
                    }
                    pnlCards.Controls.Add(cardBelowMin);
                }

                // Populate Recent Invoices
                dgRecent.Rows.Clear();
                if (dtSales != null)
                {
                    int limit = 0;
                    foreach (DataRow r in dtSales.Rows)
                    {
                        if (limit++ >= 10) break;
                        string clientOrDriver = r["SaleType"].ToString() == "DriverLoad" ? r["DriverName"].ToString() : r["ClientName"].ToString();
                        string typeArabic = r["SaleType"].ToString() == "Cash" ? "نقدي" : r["SaleType"].ToString() == "Credit" ? "آجل" : "تحميل مندوب";
                        dgRecent.Rows.Add(r["SaleCode"], typeArabic, clientOrDriver, Convert.ToDecimal(r["TotalAmount"]).ToString("N2") + " ج");
                    }
                }

                // Populate Chart Data
                var dayNames = new[] { "الأحد", "الأثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };
                for (int i = 0; i < 7; i++)
                {
                    DateTime d = DateTime.Today.AddDays(-6 + i);
                    _chartDays[i] = dayNames[(int)d.DayOfWeek];
                    decimal val = 0;
                    if (salesDtChart != null)
                    {
                        foreach (DataRow row in salesDtChart.Rows)
                        {
                            if (Convert.ToDateTime(row["SaleDay"]).Date == d.Date)
                            {
                                val = Convert.ToDecimal(row["Total"]);
                                break;
                            }
                        }
                    }
                    _chartValues[i] = val;
                }
                pnlChart.Invalidate();
            }
            catch { }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private Panel MakeCard(string title, string value, Color color)
        {
            var card = new Panel { Size = new Size(210, 95), BackColor = Theme.BgCard, Margin = new Padding(8), Cursor = Cursors.Hand };
            var lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.TextSub, Location = new Point(12, 14), AutoSize = true, BackColor = Color.Transparent };
            var lblValue = new Label { Text = value, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = color, Location = new Point(12, 42), AutoSize = true, BackColor = Color.Transparent };

            card.Controls.AddRange(new Control[] { lblTitle, lblValue });
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(color)) g.FillRectangle(brush, 0, 0, card.Width, 4);
                using (var pen = new Pen(Theme.BorderColor, 1.5f)) g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            return card;
        }

        private void PnlChart_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padLeft = 40, padRight = 20, padTop = 35, padBottom = 25;
            int w = pnlChart.Width, h = pnlChart.Height;
            if (w < 100 || h < 50) return;

            int chartW = w - padLeft - padRight;
            int chartH = h - padTop - padBottom;

            g.DrawString("📈 مؤشر حركة المبيعات لآخر 7 أيام", new Font("Segoe UI", 9.5f, FontStyle.Bold), new SolidBrush(Theme.TextMain), 15, 8);

            decimal maxVal = 1000;
            foreach (var val in _chartValues) if (val > maxVal) maxVal = val;
            float maxF = (float)maxVal * 1.15f;

            using (var gridPen = new Pen(Color.FromArgb(40, Theme.BorderColor), 1f))
            using (var textBrush = new SolidBrush(Theme.TextSub))
            using (var linePen = new Pen(Theme.BorderColor, 1.5f))
            {
                for (int j = 0; j <= 3; j++)
                {
                    float yVal = padTop + (chartH / 3f) * j;
                    g.DrawLine(gridPen, padLeft, yVal, w - padRight, yVal);
                    decimal gridVal = (decimal)(maxF - (maxF / 3f) * j);
                    g.DrawString(gridVal.ToString("N0"), new Font("Segoe UI", 7.5f), textBrush, 5, yVal - 7);
                }
                g.DrawLine(linePen, padLeft, h - padBottom, w - padRight, h - padBottom);
            }

            float colWidth = chartW / 7f;
            float barWidth = colWidth * 0.45f;

            for (int i = 0; i < 7; i++)
            {
                float barX = padLeft + (colWidth * i) + (colWidth - barWidth) / 2f;
                float valRatio = maxF > 0 ? (float)_chartValues[i] / maxF : 0f;
                float barHeight = chartH * valRatio;
                float barY = h - padBottom - barHeight;

                if (barHeight > 0)
                {
                    var rect = new RectangleF(barX, barY, barWidth, barHeight);
                    using (var brush = new LinearGradientBrush(new PointF(barX, barY), new PointF(barX, barY + barHeight), Color.FromArgb(243, 198, 35), Theme.Primary))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    using (var valBrush = new SolidBrush(Theme.TextMain))
                    {
                        g.DrawString(_chartValues[i].ToString("N0"), new Font("Segoe UI", 7.5f, FontStyle.Bold), valBrush, barX - 5, barY - 15);
                    }
                }
                using (var labelBrush = new SolidBrush(Theme.TextMain))
                {
                    g.DrawString(_chartDays[i] ?? "", new Font("Segoe UI", 7.5f), labelBrush, barX - 5, h - padBottom + 5);
                }
            }
        }
    }
}

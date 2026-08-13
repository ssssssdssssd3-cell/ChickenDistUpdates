using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>لوحة مقارنة وترتيب أداء المناديب — بمخططات بيانية GDI+ بدون مكتبات خارجية</summary>
    public class FrmDriverLeaderboard : Form
    {
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnLoad;
        private Panel pnlCards;
        private Panel pnlChart;

        private DataTable _data;

        // ألوان الميداليات
        private static readonly Color Gold   = Color.FromArgb(255, 215, 0);
        private static readonly Color Silver = Color.FromArgb(192, 192, 192);
        private static readonly Color Bronze = Color.FromArgb(205, 127, 50);

        public FrmDriverLeaderboard()
        {
            InitUI();
            LoadData();
        }

        private void InitUI()
        {
            Text = "🏆 لوحة أداء ومنافسة المناديب";
            Size = new Size(1300, 780);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // ===== شريط العنوان =====
            var pnlTitle = Theme.MakeTitleBar(
                "🏆 لوحة أداء المناديب",
                "مقارنة شاملة للمبيعات والتحصيل والعجز لكل مندوب — اختر الفترة الزمنية");
            pnlTitle.Dock = DockStyle.Top;

            // ===== شريط الفلاتر =====
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 55,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8)
            };

            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 7, 0, 0), Font = Theme.FontBold };
            dtpFrom = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Today.AddDays(-30) };

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 7, 0, 0), Font = Theme.FontBold };
            dtpTo = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd hh:mm tt", Value = DateTime.Now };

            btnLoad = Theme.MakeButton("🔄 تحديث", Theme.Accent);
            btnLoad.Size = new Size(110, 34);
            btnLoad.Margin = new Padding(10, 2, 0, 0);
            btnLoad.Click += (s, e) => LoadData();

            pnlFilter.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnLoad });

            // ===== منطقة البطاقات (ترتيب المناديب) =====
            pnlCards = new Panel
            {
                Dock = DockStyle.Top,
                Height = 260,
                BackColor = Theme.BgMain,
                AutoScroll = false,
                Padding = new Padding(10)
            };

            // ===== منطقة المخططات البيانية (GDI+) =====
            pnlChart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15)
            };
            pnlChart.Paint += PnlChart_Paint;

            Controls.Add(pnlChart);
            Controls.Add(pnlCards);
            Controls.Add(pnlFilter);
            Controls.Add(pnlTitle);

            Theme.ApplyFormRTL(this);
        }

        private void LoadData()
        {
            _data = DriverDAL.GetLeaderboard(dtpFrom.Value.Date, dtpTo.Value.Date);
            BuildCards();
            pnlChart.Invalidate();
        }

        private void BuildCards()
        {
            pnlCards.Controls.Clear();
            if (_data == null || _data.Rows.Count == 0)
            {
                pnlCards.Controls.Add(new Label
                {
                    Text = "لا توجد بيانات للمناديب في هذه الفترة.",
                    ForeColor = Theme.TextSub,
                    AutoSize = true,
                    Location = new Point(20, 20)
                });
                return;
            }

            int x = 10;
            int rank = 1;

            foreach (DataRow row in _data.Rows)
            {
                Color medalColor = rank == 1 ? Gold : rank == 2 ? Silver : rank == 3 ? Bronze : Theme.BgCard;
                string medal = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : $"#{rank}";

                var card = BuildDriverCard(row, rank, medal, medalColor);
                card.Location = new Point(x, 10);
                pnlCards.Controls.Add(card);
                x += card.Width + 12;
                rank++;
            }
        }

        private Panel BuildDriverCard(DataRow row, int rank, string medal, Color medalColor)
        {
            string name         = row["DriverName"].ToString();
            decimal totalSales  = Convert.ToDecimal(row["TotalSales"]);
            decimal cashSales   = Convert.ToDecimal(row["CashSales"]);
            decimal totalDead   = Convert.ToDecimal(row["TotalDead"]);
            decimal totalDeficit = Convert.ToDecimal(row["TotalDeficit"]);
            int     handovers   = Convert.ToInt32(row["HandoverCount"]);
            decimal debt        = Convert.ToDecimal(row["DebtBalance"]);

            decimal cashPct = totalSales > 0 ? (cashSales / totalSales * 100m) : 0;

            var card = new Panel
            {
                Width = 190,
                Height = 240,
                BackColor = Theme.BgCard,
                Padding = new Padding(12)
            };

            // حد ملون حسب الترتيب
            card.Paint += (s, ev) =>
            {
                var g = ev.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // إطار بلون الميدالية
                using (var pen = new Pen(medalColor, 3))
                {
                    using (var path = GetRoundedRect(new Rectangle(1, 1, card.Width - 2, card.Height - 2), 10))
                        g.DrawPath(pen, path);
                }

                // شريط الميدالية العلوي
                using (var brush = new SolidBrush(Color.FromArgb(220, medalColor)))
                using (var path = GetRoundedRect(new Rectangle(0, 0, card.Width, 36), 10))
                    g.FillPath(brush, path);
            };

            int yPos = 40;

            var lblMedal = new Label
            {
                Text = medal,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = rank <= 3 ? Color.FromArgb(50, 30, 0) : Theme.TextMain,
                AutoSize = true,
                Location = new Point(10, 6)
            };

            var lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                AutoSize = false,
                Width = card.Width - 24,
                Height = 36,
                Location = new Point(12, yPos),
                TextAlign = ContentAlignment.MiddleRight
            };
            yPos += 40;

            var lblSales = MakeCardRow(card.Width, yPos, "📦 المبيعات:", $"{totalSales:N0} ج", Color.LightSkyBlue);
            yPos += 26;

            var lblCash = MakeCardRow(card.Width, yPos, "💵 نقدي:", $"{cashSales:N0} ج ({cashPct:F0}%)", Color.LightGreen);
            yPos += 26;

            var lblLoads = MakeCardRow(card.Width, yPos, "📋 حمولات:", $"{handovers}", Color.White);
            yPos += 26;

            // النافق والعجز — ملوّن بالأحمر إذا > 0
            var lblDead = MakeCardRow(card.Width, yPos, "☠️ النافق:", $"{totalDead:F1}", totalDead > 0 ? Color.OrangeRed : Color.Gray);
            yPos += 26;

            var lblDeficit = MakeCardRow(card.Width, yPos, "⚠️ العجز:", $"{totalDeficit:F1}", totalDeficit > 0 ? Color.FromArgb(255, 80, 80) : Color.Gray);
            yPos += 26;

            if (debt > 0)
            {
                var lblDebt = MakeCardRow(card.Width, yPos, "🔴 المديونية:", $"{debt:N2} ج", Color.FromArgb(255, 60, 60));
                card.Controls.Add(lblDebt);
            }

            card.Controls.AddRange(new Control[] { lblMedal, lblName, lblSales, lblCash, lblLoads, lblDead, lblDeficit });
            return card;
        }

        private Label MakeCardRow(int cardWidth, int y, string title, string value, Color valueColor)
        {
            var pnl = new Label
            {
                AutoSize = false,
                Width = cardWidth - 24,
                Height = 22,
                Location = new Point(12, y),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = valueColor,
                Font = new Font("Segoe UI", 9f)
            };
            pnl.Text = $"{title}  {value}";
            return pnl;
        }

        // ===== رسم المخططات البيانية =====
        private void PnlChart_Paint(object sender, PaintEventArgs e)
        {
            if (_data == null || _data.Rows.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = pnlChart.ClientSize.Width;
            int h = pnlChart.ClientSize.Height;
            int pad = 50;
            int chartW = (w - pad * 2);
            int chartH = h - 100;

            // === مخطط إجمالي المبيعات ===
            DrawBarChart(g,
                _data,
                "TotalSales",
                "📊 إجمالي المبيعات (ج)",
                new Rectangle(pad, 30, chartW / 3 - 15, chartH),
                Color.FromArgb(70, 130, 220),
                Color.FromArgb(130, 180, 255));

            // === مخطط النقدية المحصلة ===
            DrawBarChart(g,
                _data,
                "CashSales",
                "💵 التحصيل النقدي (ج)",
                new Rectangle(pad + chartW / 3 + 10, 30, chartW / 3 - 15, chartH),
                Color.FromArgb(46, 160, 80),
                Color.FromArgb(100, 220, 130));

            // === مخطط النافق+العجز (أقل = أفضل) ===
            DrawDeficitChart(g,
                _data,
                new Rectangle(pad + 2 * (chartW / 3) + 20, 30, chartW / 3 - 15, chartH));
        }

        private void DrawBarChart(Graphics g, DataTable dt, string colName, string title,
            Rectangle area, Color barColorDark, Color barColorLight)
        {
            // خلفية
            using (var bgBrush = new SolidBrush(Color.FromArgb(30, 40, 60)))
                g.FillRectangle(bgBrush, area);

            // العنوان
            using (var fTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var bTitle = new SolidBrush(Color.White))
            {
                var titleRect = new RectangleF(area.X, area.Y - 26, area.Width, 24);
                g.DrawString(title, fTitle, bTitle, titleRect, new StringFormat { Alignment = StringAlignment.Center });
            }

            if (dt.Rows.Count == 0) return;

            // حساب الأعلى قيمة
            decimal maxVal = 0;
            foreach (DataRow row in dt.Rows)
            {
                decimal val = Convert.ToDecimal(row[colName]);
                if (val > maxVal) maxVal = val;
            }
            if (maxVal == 0) return;

            int barCount  = dt.Rows.Count;
            int barPad    = 10;
            int totalPad  = barPad * (barCount + 1);
            int barWidth  = (area.Width - totalPad) / barCount;
            int chartInnerH = area.Height - 50;
            int rank = 0;

            foreach (DataRow row in dt.Rows)
            {
                decimal val  = Convert.ToDecimal(row[colName]);
                string  name = row["DriverName"].ToString();
                if (name.Length > 10) name = name.Substring(0, 10) + "…";

                int barH = (int)(val / maxVal * chartInnerH);
                int bx   = area.X + barPad + rank * (barWidth + barPad);
                int by   = area.Y + area.Height - barH - 28;

                Color top = rank == 0 ? Gold : rank == 1 ? Silver : rank == 2 ? Bronze : barColorLight;

                using (var brush = new LinearGradientBrush(
                    new Point(bx, by), new Point(bx, by + barH),
                    top, barColorDark))
                {
                    using (var path = GetRoundedRect(new Rectangle(bx, by, barWidth, barH), 5))
                        g.FillPath(brush, path);
                }

                // القيمة فوق الشريط
                using (var fVal = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (var bVal = new SolidBrush(Color.White))
                {
                    var valStr = val >= 1000 ? $"{val / 1000m:F1}k" : val.ToString("F0");
                    var valRect = new RectangleF(bx, by - 18, barWidth, 18);
                    g.DrawString(valStr, fVal, bVal, valRect, new StringFormat { Alignment = StringAlignment.Center });
                }

                // اسم المندوب أسفل الشريط
                using (var fName = new Font("Segoe UI", 7f))
                using (var bName = new SolidBrush(Color.LightGray))
                {
                    var nameRect = new RectangleF(bx, area.Y + area.Height - 24, barWidth, 22);
                    g.DrawString(name, fName, bName, nameRect, new StringFormat { Alignment = StringAlignment.Center });
                }

                rank++;
            }
        }

        private void DrawDeficitChart(Graphics g, DataTable dt, Rectangle area)
        {
            // خلفية
            using (var bgBrush = new SolidBrush(Color.FromArgb(50, 20, 20)))
                g.FillRectangle(bgBrush, area);

            // العنوان
            using (var fTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var bTitle = new SolidBrush(Color.FromArgb(255, 120, 120)))
            {
                var titleRect = new RectangleF(area.X, area.Y - 26, area.Width, 24);
                g.DrawString("⚠️ النافق + العجز (أقل = أفضل)", fTitle, bTitle, titleRect, new StringFormat { Alignment = StringAlignment.Center });
            }

            if (dt.Rows.Count == 0) return;

            decimal maxVal = 0;
            foreach (DataRow row in dt.Rows)
            {
                decimal val = Convert.ToDecimal(row["TotalDead"]) + Convert.ToDecimal(row["TotalDeficit"]);
                if (val > maxVal) maxVal = val;
            }

            int barCount   = dt.Rows.Count;
            int barPad     = 10;
            int barWidth   = (area.Width - barPad * (barCount + 1)) / barCount;
            int chartInnerH = area.Height - 50;

            // ترتيب البيانات تصاعدياً (أقل نافق = أفضل)
            var rows = new List<DataRow>();
            foreach (DataRow r in dt.Rows) rows.Add(r);
            rows.Sort((a, b) =>
            {
                decimal va = Convert.ToDecimal(a["TotalDead"]) + Convert.ToDecimal(a["TotalDeficit"]);
                decimal vb = Convert.ToDecimal(b["TotalDead"]) + Convert.ToDecimal(b["TotalDeficit"]);
                return va.CompareTo(vb);
            });

            int rank = 0;
            foreach (DataRow row in rows)
            {
                decimal val  = Convert.ToDecimal(row["TotalDead"]) + Convert.ToDecimal(row["TotalDeficit"]);
                string  name = row["DriverName"].ToString();
                if (name.Length > 10) name = name.Substring(0, 10) + "…";

                int barH = maxVal > 0 ? (int)(val / maxVal * chartInnerH) : 0;
                if (barH < 4) barH = 4;

                int bx = area.X + barPad + rank * (barWidth + barPad);
                int by = area.Y + area.Height - barH - 28;

                // الأقل نافقاً باللون الأخضر (أفضل)، الأعلى باللأحمر
                Color barColor = rank == 0
                    ? Color.FromArgb(46, 200, 80)
                    : Color.FromArgb(200, 60 + rank * 30, 40);

                using (var brush = new LinearGradientBrush(
                    new Point(bx, by), new Point(bx, by + barH),
                    barColor, Color.FromArgb(30, barColor.R / 2, barColor.G / 2)))
                {
                    using (var path = GetRoundedRect(new Rectangle(bx, by, barWidth, barH), 5))
                        g.FillPath(brush, path);
                }

                // القيمة فوق الشريط
                using (var fVal = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (var bVal = new SolidBrush(Color.White))
                {
                    var valRect = new RectangleF(bx, by - 18, barWidth, 18);
                    g.DrawString(val.ToString("F1"), fVal, bVal, valRect, new StringFormat { Alignment = StringAlignment.Center });
                }

                // اسم المندوب
                using (var fName = new Font("Segoe UI", 7f))
                using (var bName = new SolidBrush(Color.LightGray))
                {
                    var nameRect = new RectangleF(bx, area.Y + area.Height - 24, barWidth, 22);
                    g.DrawString(name, fName, bName, nameRect, new StringFormat { Alignment = StringAlignment.Center });
                }

                rank++;
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

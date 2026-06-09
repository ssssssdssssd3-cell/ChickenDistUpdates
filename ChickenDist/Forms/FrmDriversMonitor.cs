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
    /// <summary>لوحة مراقبة حركة المناديب اللحظية في السوق</summary>
    public class FrmDriversMonitor : Form
    {
        private FlowLayoutPanel flpDrivers;
        private Timer refreshTimer;

        public FrmDriversMonitor()
        {
            InitUI();
            LoadMonitorData();

            // مؤقت لتحديث البيانات تلقائياً كل 15 ثانية لجعل الشاشة حية ولحظية
            refreshTimer = new Timer { Interval = 15000 };
            refreshTimer.Tick += (s, e) => LoadMonitorData();
            refreshTimer.Start();

            this.FormClosed += (s, e) => refreshTimer.Stop();
        }

        private void InitUI()
        {
            this.Text = "لوحة المراقبة اللحظية للمناديب";
            this.Size = new Size(1366, 768);
            this.MinimumSize = new Size(1024, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var mainTbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                RightToLeft = RightToLeft.Yes
            };
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f)); // العنوان
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // المحتوى

            // شريط العنوان مع لمسات عصرية
            var pnlTitle = Theme.MakeTitleBar("لوحة المراقبة اللحظية للمناديب", "متابعة حركة وحمولات المناديب النشطين في السوق، الكميات المباعة، والمبالغ المحصلة لحظة بلحظة");
            
            var btnRefresh = Theme.MakeButton("🔄 تحديث تلقائي (15ث)", Theme.Success);
            btnRefresh.Size = new Size(150, 32);
            btnRefresh.Location = new Point(20, 20);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnRefresh.Click += (s, e) => LoadMonitorData();
            pnlTitle.Controls.Add(btnRefresh);

            mainTbl.Controls.Add(pnlTitle, 0, 0);

            // حاوية بطاقات المناديب المرنة
            flpDrivers = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(20),
                BackColor = Theme.BgMain
            };
            mainTbl.Controls.Add(flpDrivers, 0, 1);

            this.Controls.Add(mainTbl);
            Theme.ApplyFormRTL(this);
        }

        private void LoadMonitorData()
        {
            // الحفاظ على موضع التمرير الحالي وتفادي الوميض
            int scrollVal = flpDrivers.VerticalScroll.Value;
            flpDrivers.Controls.Clear();

            // جلب بيانات الحمولات المفتوحة للمناديب مع المبيعات والتحصيلات النشطة من بداية الحمولة
            string sql = @"
                SELECT dl.LoadID, dl.LoadDate, e.EmpID, e.EmpName AS DriverName,
                       s.SaleCode, s.TotalAmount AS LoadedValue,
                        ISNULL((
                            SELECT SUM(s2.TotalAmount)
                            FROM Sales s2
                            WHERE s2.DriverID = dl.DriverID
                              AND s2.SaleType IN ('Cash', 'Credit')
                              AND CAST(s2.SaleDate AS DATE) >= CAST(dl.LoadDate AS DATE)
                        ), 0) AS ActiveSalesValue,
                        ISNULL((
                            SELECT SUM(s2.TotalAmount)
                            FROM Sales s2
                            WHERE s2.DriverID = dl.DriverID
                              AND s2.SaleType = 'Cash'
                              AND CAST(s2.SaleDate AS DATE) >= CAST(dl.LoadDate AS DATE)
                        ), 0) AS ActiveCashCollected
                FROM DriverLoads dl
                JOIN Employees e ON dl.DriverID = e.EmpID
                JOIN Sales s ON dl.SaleID = s.SaleID
                WHERE dl.IsClosed=0
                ORDER BY dl.LoadDate DESC";

            DataTable dt = DbHelper.Query(sql);

            if (dt == null || dt.Rows.Count == 0)
            {
                var lblNoData = new Label
                {
                    Text = "📭 لا توجد حمولات نشطة أو مناديب في السوق حالياً.",
                    Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = Theme.TextSub,
                    AutoSize = true,
                    Margin = new Padding(50)
                };
                flpDrivers.Controls.Add(lblNoData);
                return;
            }

            foreach (DataRow r in dt.Rows)
            {
                int loadID = Convert.ToInt32(r["LoadID"]);
                string driverName = r["DriverName"].ToString();
                DateTime loadDate = Convert.ToDateTime(r["LoadDate"]);
                decimal loadedVal = Convert.ToDecimal(r["LoadedValue"]);
                decimal salesVal = Convert.ToDecimal(r["ActiveSalesValue"]);
                decimal cashColl = Convert.ToDecimal(r["ActiveCashCollected"]);

                var card = CreateDriverCard(loadID, driverName, loadDate, loadedVal, salesVal, cashColl);
                flpDrivers.Controls.Add(card);
            }

            // استعادة موضع التمرير
            try { flpDrivers.VerticalScroll.Value = scrollVal; } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Restore scroll failed: " + ex.Message); }
        }

        private Panel CreateDriverCard(int loadID, string driverName, DateTime loadDate, decimal loadedVal, decimal salesVal, decimal cashColl)
        {
            var card = new Panel
            {
                Size = new Size(350, 380),
                BackColor = Theme.BgCard,
                Margin = new Padding(12),
                Padding = new Padding(12)
            };

            // رسم حواف زجاجية وتدرج علوي رائع بصرياً (Gradients & Premium Details)
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // إطار خارجي ناعم ومتناسق
                using (var pen = new Pen(Theme.BorderColor, 1.5f))
                {
                    g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }

                // شريط تدرج لوني علوي فخم (من الأزرق المودرن إلى الأخضر التفاعلي)
                using (var brush = new LinearGradientBrush(new Point(0, 0), new Point(card.Width, 0), Color.FromArgb(41, 128, 185), Color.FromArgb(46, 204, 113)))
                {
                    g.FillRectangle(brush, 1, 1, card.Width - 2, 6);
                }
            };

            // اسم المندوب بخط عريض ذهبي
            var lblDriver = new Label
            {
                Text = "🚚 " + driverName,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Location = new Point(15, 15),
                AutoSize = true
            };

            // تاريخ التحميل
            var lblDate = new Label
            {
                Text = "وقت التحميل: " + loadDate.ToString("dd/MM/yyyy HH:mm"),
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                Location = new Point(15, 42),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblDriver, lblDate });

            // لوحة الإحصائيات (Stats Grid) بخلفية زجاجية شبه شفافة وممتازة
            var statsTbl = new TableLayoutPanel
            {
                Location = new Point(12, 68),
                Size = new Size(326, 75),
                ColumnCount = 3,
                RowCount = 2,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.FromArgb(10, 255, 255, 255) // تأثير زجاجي شبه شفاف
            };
            statsTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            statsTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));

            // العناوين
            statsTbl.Controls.Add(new Label { Text = "القيمة المحملة", Font = Theme.FontSmall, ForeColor = Theme.TextSub, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
            statsTbl.Controls.Add(new Label { Text = "قيمة المبيعات", Font = Theme.FontSmall, ForeColor = Theme.TextSub, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 0);
            statsTbl.Controls.Add(new Label { Text = "التحصيل النقدي", Font = Theme.FontSmall, ForeColor = Theme.TextSub, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 2, 0);

            // القيم المباشرة والملونة
            statsTbl.Controls.Add(new Label { Text = loadedVal.ToString("N0") + " ج", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.TextMain, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 0, 1);
            statsTbl.Controls.Add(new Label { Text = salesVal.ToString("N0") + " ج", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.Success, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 1);
            statsTbl.Controls.Add(new Label { Text = cashColl.ToString("N0") + " ج", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.Accent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 2, 1);

            card.Controls.Add(statsTbl);

            // جلب أصناف حمولة المندوب وتفاصيل بيعها
            DataTable itemsDt = DriverDAL.GetLoadItems(loadID);
            decimal totalQtyLoaded = 0;
            decimal totalQtySold = 0;

            var dgItems = new DataGridView
            {
                Location = new Point(12, 152),
                Size = new Size(326, 150),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                ScrollBars = ScrollBars.Vertical,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = new Font("Segoe UI", 8.5f) },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Loaded", HeaderText = "حمل", FillWeight = 20 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sold", HeaderText = "باع", FillWeight = 20 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rem", HeaderText = "باقي", FillWeight = 20 });

            foreach (DataRow ir in itemsDt.Rows)
            {
                string pName = ir["ProductName"].ToString();
                decimal loaded = Convert.ToDecimal(ir["LoadedQty"]);
                decimal sold = Convert.ToDecimal(ir["SoldQty"]);
                decimal rem = loaded - sold;

                totalQtyLoaded += loaded;
                totalQtySold += sold;

                dgItems.Rows.Add(pName, loaded.ToString("N0"), sold.ToString("N0"), rem.ToString("N0"));
            }

            card.Controls.Add(dgItems);

            // حساب النسبة الإجمالية للإنجاز والبيع
            decimal completionRate = totalQtyLoaded > 0 ? (totalQtySold / totalQtyLoaded) * 100 : 0;
            if (completionRate > 100) completionRate = 100;

            // شريط إنجاز مخصص وجميل جداً (GDI+ Custom Rounded Progress Bar Container)
            var pnlProgressContainer = new Panel
            {
                Location = new Point(12, 312),
                Size = new Size(326, 50),
                BackColor = Color.Transparent
            };

            var lblProgTitle = new Label
            {
                Text = "نسبة توزيع بضاعة الحمولة:",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                Location = new Point(5, 2),
                AutoSize = true
            };

            var lblProgPct = new Label
            {
                Text = completionRate.ToString("F1") + "%",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Success,
                Location = new Point(275, 2),
                AutoSize = true
            };

            // رسم شريط التقدم الفعلي المنحني (Custom Rounded ProgressBar)
            var pnlProgressBar = new Panel
            {
                Location = new Point(5, 24),
                Size = new Size(316, 16),
                BackColor = Color.FromArgb(48, 55, 72)
            };
            pnlProgressBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // رسم خلفية دائرية الحواف
                using (var pathBg = GetRoundedRectPath(new Rectangle(0, 0, pnlProgressBar.Width - 1, pnlProgressBar.Height - 1), 6))
                {
                    g.FillPath(new SolidBrush(Color.FromArgb(48, 55, 72)), pathBg);
                }

                // رسم تعبئة التقدم بتدرج لوني ممتاز أخضر
                int fillWidth = (int)((completionRate / 100m) * pnlProgressBar.Width);
                if (fillWidth > 6)
                {
                    using (var pathFill = GetRoundedRectPath(new Rectangle(0, 0, fillWidth - 1, pnlProgressBar.Height - 1), 6))
                    using (var brush = new LinearGradientBrush(new Point(0, 0), new Point(fillWidth, 0), Color.FromArgb(46, 204, 113), Color.FromArgb(39, 174, 96)))
                    {
                        g.FillPath(brush, pathFill);
                    }
                }
            };

            pnlProgressContainer.Controls.AddRange(new Control[] { lblProgTitle, lblProgPct, pnlProgressBar });
            card.Controls.Add(pnlProgressContainer);

            return card;
        }

        // دالة مساعدة لرسم مستطيل منحني الزوايا لجمالية فائقة
        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

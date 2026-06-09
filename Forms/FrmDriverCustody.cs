using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة عهدة المناديب الحالية — تعرض ملخص كل المناديب ذوي الحمولات المفتوحة</summary>
    public class FrmDriverCustody : Form
    {
        private DataGridView dgCustody;
        private Label lblLastUpdate;

        public FrmDriverCustody()
        {
            InitUI();
            LoadData();
        }

        private void InitUI()
        {
            Text = "عهدة المناديب الحالية";
            Size = new Size(1366, 768);
            MinimumSize = new Size(1024, 600);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // ===== شريط العنوان =====
            var pnlTitle = Theme.MakeTitleBar(
                "عهدة المناديب الحالية",
                "ملخص شامل للحمولات المفتوحة غير المقفلة — الكميات والقيم والمحصل والمتبقي بعهدة كل مندوب");

            var btnRefresh = Theme.MakeButton("🔄 تحديث", Theme.Accent);
            btnRefresh.Size = new Size(110, 30);
            btnRefresh.Location = new Point(20, 20);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnRefresh.Click += (s, e) => LoadData();
            pnlTitle.Controls.Add(btnRefresh);

            var btnPrint = Theme.MakeButton("🖨️ طباعة", Theme.Primary);
            btnPrint.Size = new Size(110, 30);
            btnPrint.Location = new Point(140, 20);
            btnPrint.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnPrint.Click += BtnPrint_Click;
            pnlTitle.Controls.Add(btnPrint);

            // ===== شريط أسفل الجدول — ملخص الإجماليات =====
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 6, 10, 6)
            };
            lblLastUpdate = new Label
            {
                AutoSize = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Left
            };
            pnlFooter.Controls.Add(lblLastUpdate);

            // ===== الجدول الرئيسي =====
            dgCustody = new DataGridView
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
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain,
                    Padding = new Padding(4, 2, 4, 2)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 30 }
            };

            // تعريف الأعمدة
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "DriverName",    HeaderText = "المندوب",           FillWeight = 15 });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "LoadCode",      HeaderText = "كود الحمولة",       FillWeight = 10 });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "LoadDate",      HeaderText = "تاريخ التحميل",     FillWeight = 12 });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalLoadedQty",HeaderText = "كمية المحمل",       FillWeight = 9,  DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "LoadedValue",   HeaderText = "قيمة الحمولة",      FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldQty",       HeaderText = "كمية المبيع",       FillWeight = 9,  DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.LightGreen, Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldValue",     HeaderText = "قيمة المبيع",       FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.LightGreen, Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "CashCollected", HeaderText = "محصل نقدي",         FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.Cyan,       Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreditSold",    HeaderText = "آجل غير محصل",      FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.Orange,     Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnedQty",   HeaderText = "مرتجع كميات",       FillWeight = 8,  DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgCustody.Columns.Add(new DataGridViewTextBoxColumn { Name = "RemainingQty",  HeaderText = "المتبقي بعهدته",    FillWeight = 9,  DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.OrangeRed, Alignment = DataGridViewContentAlignment.MiddleCenter } });

            // تلوين صفوف بديلة
            dgCustody.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < dgCustody.Rows.Count)
                {
                    var row = dgCustody.Rows[e.RowIndex];
                    if (row.Tag?.ToString() == "total")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(30, 60, 30);
                        row.DefaultCellStyle.ForeColor = Color.LightGreen;
                        row.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    }
                    else if (e.RowIndex % 2 == 1)
                        row.DefaultCellStyle.BackColor = Color.FromArgb(30, 35, 48);
                }
            };

            // إضافة إلى النموذج بالترتيب الصحيح
            Controls.Add(dgCustody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlTitle);

            Theme.ApplyFormRTL(this);
        }

        private void LoadData()
        {
            dgCustody.Rows.Clear();
            DataTable dt = DriverDAL.GetDriversCustodySummary();

            if (dt == null || dt.Rows.Count == 0)
            {
                lblLastUpdate.Text = $"لا توجد حمولات مفتوحة حالياً  |  آخر تحديث: {DateTime.Now:HH:mm:ss}";
                return;
            }

            // مجاميع للإجمالي الكلي
            decimal sumLoadedQty = 0, sumLoadedVal = 0, sumSoldQty = 0, sumSoldVal = 0;
            decimal sumCash = 0, sumCredit = 0, sumReturn = 0, sumRemaining = 0;

            foreach (DataRow r in dt.Rows)
            {
                decimal loadedQty  = Convert.ToDecimal(r["TotalLoadedQty"]);
                decimal loadedVal  = Convert.ToDecimal(r["LoadedValue"]);
                decimal soldQty    = Convert.ToDecimal(r["SoldQty"]);
                decimal soldVal    = Convert.ToDecimal(r["SoldValue"]);
                decimal cash       = Convert.ToDecimal(r["CashCollected"]);
                decimal credit     = Convert.ToDecimal(r["CreditSold"]);
                decimal retQty     = Convert.ToDecimal(r["ReturnedQty"]);
                decimal remaining  = Convert.ToDecimal(r["RemainingQty"]);

                sumLoadedQty += loadedQty;
                sumLoadedVal += loadedVal;
                sumSoldQty   += soldQty;
                sumSoldVal   += soldVal;
                sumCash      += cash;
                sumCredit    += credit;
                sumReturn    += retQty;
                sumRemaining += remaining;

                string loadDate = Convert.ToDateTime(r["LoadDate"]).ToString("dd/MM/yyyy HH:mm");

                int idx = dgCustody.Rows.Add(
                    r["DriverName"],
                    r["LoadCode"],
                    loadDate,
                    loadedQty.ToString("N2"),
                    loadedVal.ToString("N2"),
                    soldQty.ToString("N2"),
                    soldVal.ToString("N2"),
                    cash.ToString("N2"),
                    credit.ToString("N2"),
                    retQty.ToString("N2"),
                    remaining.ToString("N2")
                );

                // تلوين خلية المتبقي: إذا كان المتبقي أكبر من الصفر يُلوَّن بالأحمر
                if (remaining > 0)
                    dgCustody.Rows[idx].Cells["RemainingQty"].Style.ForeColor = Color.OrangeRed;
                else if (remaining < 0)
                    dgCustody.Rows[idx].Cells["RemainingQty"].Style.ForeColor = Color.Yellow; // زيادة
                else
                    dgCustody.Rows[idx].Cells["RemainingQty"].Style.ForeColor = Color.LightGreen; // مسوَّى
            }

            // صف الإجمالي الكلي
            int totIdx = dgCustody.Rows.Add(
                "📊 الإجمالي الكلي",
                "---",
                "---",
                sumLoadedQty.ToString("N2"),
                sumLoadedVal.ToString("N2"),
                sumSoldQty.ToString("N2"),
                sumSoldVal.ToString("N2"),
                sumCash.ToString("N2"),
                sumCredit.ToString("N2"),
                sumReturn.ToString("N2"),
                sumRemaining.ToString("N2")
            );
            dgCustody.Rows[totIdx].Tag = "total";

            lblLastUpdate.Text = $"إجمالي {dt.Rows.Count} حمولة مفتوحة  |  آخر تحديث: {DateTime.Now:HH:mm:ss}";
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (dgCustody.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pd = new System.Drawing.Printing.PrintDocument();
            pd.DefaultPageSettings.Landscape = true;
            int pageRow = 0;

            pd.PrintPage += (s2, ev) =>
            {
                var g = ev.Graphics;
                var fTitle   = new Font("Arial", 13f, FontStyle.Bold);
                var fHeader  = new Font("Arial", 8.5f, FontStyle.Bold);
                var fData    = new Font("Arial", 8f);
                var fTotal   = new Font("Arial", 8.5f, FontStyle.Bold);
                int y = 20, printW = 1050;

                if (pageRow == 0)
                {
                    g.DrawString("عهدة المناديب الحالية", fTitle, Brushes.DarkBlue, 420, y);
                    y += 25;
                    g.DrawString($"تاريخ التقرير: {DateTime.Now:dd/MM/yyyy HH:mm}", fData, Brushes.Black, 420, y);
                    y += 20;
                    g.DrawLine(Pens.DarkBlue, 20, y, printW + 20, y);
                    y += 8;

                    // رسم رؤوس الأعمدة
                    string[] headers = { "المندوب", "كود الحمولة", "تاريخ التحميل", "كمية محمل", "قيمة حمولة", "كمية مبيع", "قيمة مبيع", "نقدي محصل", "آجل", "مرتجع", "المتبقي" };
                    int x = 20;
                    int[] widths = { 120, 90, 100, 80, 90, 80, 90, 90, 90, 70, 80 };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        g.DrawString(headers[i], fHeader, Brushes.DarkBlue, x, y);
                        x += widths[i];
                    }
                    y += 20;
                    g.DrawLine(Pens.Gray, 20, y, printW + 20, y);
                    y += 6;
                }

                int[] colW = { 120, 90, 100, 80, 90, 80, 90, 90, 90, 70, 80 };

                while (pageRow < dgCustody.Rows.Count)
                {
                    var row = dgCustody.Rows[pageRow];
                    bool isTotal = row.Tag?.ToString() == "total";
                    var f = isTotal ? fTotal : fData;
                    var br = isTotal ? Brushes.DarkGreen : Brushes.Black;
                    int x = 20;
                    for (int c = 0; c < dgCustody.Columns.Count; c++)
                    {
                        var rect = new RectangleF(x, y, colW[c] - 4, 16);
                        var sf = new System.Drawing.StringFormat { Trimming = System.Drawing.StringTrimming.EllipsisCharacter, FormatFlags = System.Drawing.StringFormatFlags.NoWrap };
                        g.DrawString(row.Cells[c].Value?.ToString() ?? "", f, br, rect, sf);
                        x += colW[c];
                    }
                    y += 18;
                    pageRow++;
                    if (y > ev.PageBounds.Height - 40) { ev.HasMorePages = true; return; }
                }
                pageRow = 0;
            };

            var ppd = new System.Windows.Forms.PrintPreviewDialog { Document = pd };
            ScreenHelper.SafePrintPreview(ppd, 1100, 750);
            ppd.ShowDialog();
        }
    }
}

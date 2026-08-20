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
    public class FrmInventoryVarianceReport : Form
    {
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private ComboBox cboWarehouse;
        private ComboBox cboFilterType;
        private TextBox txtSearch;
        private Button btnLoad;
        private Button btnPrint;
        private Button btnExport;

        private Label lblTotalShortageQty;
        private Label lblTotalShortageCost;
        private Label lblTotalSurplusQty;
        private Label lblTotalSurplusCost;
        private Label lblNetCostDiff;
        private Label lblTotalShortageSale;

        private DataGridView dgGrid;
        private DataTable _dtData;

        public FrmInventoryVarianceReport()
        {
            InitUI();
        }

        private void InitUI()
        {
            Text = "📊 تقرير فروق وتسويات الجرد والعجز والزيادة والتقييم المالي";
            Size = new Size(1250, 750);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // 1. Top Filter Panel
            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            pnlTop.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(5, 10, 0, 0) });
            dtpFrom = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            pnlTop.Controls.Add(dtpFrom);

            pnlTop.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(15, 10, 0, 0) });
            dtpTo = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnlTop.Controls.Add(dtpTo);

            pnlTop.Controls.Add(new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(15, 10, 0, 0) });
            cboWarehouse = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlTop.Controls.Add(cboWarehouse);

            pnlTop.Controls.Add(new Label { Text = "التصفية:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(15, 10, 0, 0) });
            cboFilterType = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFilterType.Items.AddRange(new object[] { "--- كل الفروق ---", "🔻 عجز فقط (خسارة)", "🔺 زيادة فقط (فائض)" });
            cboFilterType.SelectedIndex = 0;
            pnlTop.Controls.Add(cboFilterType);

            pnlTop.Controls.Add(new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(15, 10, 0, 0) });
            txtSearch = new TextBox { Width = 140 };
            pnlTop.Controls.Add(txtSearch);

            btnLoad = Theme.MakeButton("🔍 عرض التقرير", Theme.Primary);
            btnLoad.Size = new Size(110, 32);
            btnLoad.Click += (s, e) => LoadReportData();
            pnlTop.Controls.Add(btnLoad);

            btnPrint = Theme.MakeButton("🖨️ طباعة A4", Theme.Secondary);
            btnPrint.Size = new Size(110, 32);
            btnPrint.Click += BtnPrint_Click;
            pnlTop.Controls.Add(btnPrint);

            btnExport = Theme.MakeButton("📊 إكسيل", Color.FromArgb(40, 120, 60));
            btnExport.Size = new Size(90, 32);
            btnExport.Click += BtnExport_Click;
            pnlTop.Controls.Add(btnExport);

            var btnPrintSurplusBarcodes = Theme.MakeButton("🏷️ طباعة باركود الزيادات", Color.FromArgb(39, 174, 96));
            btnPrintSurplusBarcodes.Size = new Size(160, 32);
            btnPrintSurplusBarcodes.Click += BtnPrintSurplusBarcodes_Click;
            pnlTop.Controls.Add(btnPrintSurplusBarcodes);

            var btnSessions = Theme.MakeButton("📋 استرجاع عمليات الجرد", Color.FromArgb(70, 40, 130));
            btnSessions.Size = new Size(160, 32);
            btnSessions.Click += (s, e) => new FrmInventorySessions().ShowDialog(this);
            pnlTop.Controls.Add(btnSessions);

            // 2. Metrics Cards Panel
            var pnlCards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10, 5, 10, 5),
                BackColor = Theme.BgMain
            };
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblTotalShortageCost = new Label();
            lblTotalShortageQty = new Label();
            var card1 = CreateCard("🔴 إجمالي العجز (خسارة التكلفة)", lblTotalShortageCost, lblTotalShortageQty, Color.FromArgb(140, 30, 30), Color.FromArgb(255, 235, 235));

            lblTotalSurplusCost = new Label();
            lblTotalSurplusQty = new Label();
            var card2 = CreateCard("🟢 إجمالي الزيادة (بالتكلفة)", lblTotalSurplusCost, lblTotalSurplusQty, Color.FromArgb(25, 110, 45), Color.FromArgb(230, 250, 235));

            lblNetCostDiff = new Label();
            var lblNetDesc = new Label { Text = "الفرق بين الزيادة والعجز", Font = new Font("Arial", 8f), ForeColor = Color.DarkGray };
            var card3 = CreateCard("⚖️ صافي الفارق المالي", lblNetCostDiff, lblNetDesc, Color.FromArgb(20, 80, 140), Color.FromArgb(230, 240, 255));

            lblTotalShortageSale = new Label();
            var lblSaleDesc = new Label { Text = "القيمة البيعية المفقودة", Font = new Font("Arial", 8f), ForeColor = Color.DarkGray };
            var card4 = CreateCard("🏷️ خسارة العجز (بسعر البيع)", lblTotalShortageSale, lblSaleDesc, Color.FromArgb(110, 40, 120), Color.FromArgb(245, 235, 255));

            pnlCards.Controls.Add(card1, 0, 0);
            pnlCards.Controls.Add(card2, 1, 0);
            pnlCards.Controls.Add(card3, 2, 0);
            pnlCards.Controls.Add(card4, 3, 0);

            // 3. DataGrid
            dgGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };
            Theme.ApplyGridTheme(dgGrid);

            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AdjDate",       HeaderText = "التاريخ والوقت",  Width = 130 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "المخزن",         Width = 110 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode",   HeaderText = "كود الصنف",      Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName",   HeaderText = "اسم الصنف",      Width = 180 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",          HeaderText = "الوحدة",         Width = 70 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookQty",       HeaderText = "الدفتري",        Width = 75 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualQty",     HeaderText = "الفعلي",         Width = 75 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffQty",       HeaderText = "فارق الكمية",    Width = 85 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffType",      HeaderText = "نوع الفارق",     Width = 85 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", HeaderText = "سعر الشراء",    Width = 85 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice",     HeaderText = "سعر البيع",      Width = 85 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShortageLoss",   HeaderText = "خسارة العجز (ج)", Width = 110 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SurplusGain",    HeaderText = "زيادة التكلفة (ج)", Width = 110 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy",     HeaderText = "المسؤول",        Width = 90 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",         HeaderText = "ملاحظات",        Width = 150 });

            Controls.Add(dgGrid);
            Controls.Add(pnlCards);
            Controls.Add(pnlTop);

            LoadWarehouses();
            LoadReportData();
        }

        private Panel CreateCard(string title, Label lblMainVal, Label lblSubVal, Color titleColor, Color bgColor)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgColor,
                Padding = new Padding(8),
                Margin = new Padding(4)
            };
            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, titleColor, ButtonBorderStyle.Solid);
            };

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Arial", 9.5f, FontStyle.Bold),
                ForeColor = titleColor,
                RightToLeft = RightToLeft.Yes
            };

            lblMainVal.Dock = DockStyle.Top;
            lblMainVal.Height = 26;
            lblMainVal.Font = new Font("Arial", 12f, FontStyle.Bold);
            lblMainVal.ForeColor = titleColor;
            lblMainVal.Text = "0.00 ج";
            lblMainVal.RightToLeft = RightToLeft.Yes;

            lblSubVal.Dock = DockStyle.Fill;
            lblSubVal.RightToLeft = RightToLeft.Yes;

            card.Controls.Add(lblSubVal);
            card.Controls.Add(lblMainVal);
            card.Controls.Add(lblTitle);
            return card;
        }

        private void LoadWarehouses()
        {
            try
            {
                var dt = WarehouseDAL.GetAll(true);
                cboWarehouse.Items.Clear();
                cboWarehouse.Items.Add(new ComboItem(0, "--- كل المخازن ---"));
                foreach (DataRow r in dt.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
                }
                cboWarehouse.DisplayMember = "Text";
                cboWarehouse.ValueMember = "ID";
                cboWarehouse.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadReportData()
        {
            try
            {
                int? wid = null;
                if (cboWarehouse.SelectedItem is ComboItem ci && ci.ID > 0)
                    wid = ci.ID;

                string filterType = "ALL";
                if (cboFilterType.SelectedIndex == 1) filterType = "SHORTAGE";
                else if (cboFilterType.SelectedIndex == 2) filterType = "SURPLUS";

                _dtData = InventoryDAL.GetVarianceReport(dtpFrom.Value, dtpTo.Value, wid, filterType, txtSearch.Text.Trim());
                dgGrid.Rows.Clear();

                decimal totalShortageCost = 0;
                decimal totalShortageSale = 0;
                decimal totalShortageQty = 0;

                decimal totalSurplusCost = 0;
                decimal totalSurplusQty = 0;

                foreach (DataRow r in _dtData.Rows)
                {
                    decimal diff = Convert.ToDecimal(r["DiffQty"]);
                    decimal purchasePrice = Convert.ToDecimal(r["PurchasePrice"]);
                    decimal salePrice = Convert.ToDecimal(r["SalePrice"]);

                    decimal shortageLoss = Convert.ToDecimal(r["ShortageCostLoss"]);
                    decimal shortageSaleLoss = Convert.ToDecimal(r["ShortageSaleLoss"]);
                    decimal surplusGain = Convert.ToDecimal(r["SurplusCostGain"]);

                    string typeStr = "مطابق";
                    if (diff < 0)
                    {
                        typeStr = "🔻 عجز";
                        totalShortageQty += Math.Abs(diff);
                        totalShortageCost += shortageLoss;
                        totalShortageSale += shortageSaleLoss;
                    }
                    else if (diff > 0)
                    {
                        typeStr = "🔺 زيادة";
                        totalSurplusQty += diff;
                        totalSurplusCost += surplusGain;
                    }

                    int ri = dgGrid.Rows.Add(
                        Convert.ToDateTime(r["AdjDate"]).ToString("dd/MM/yyyy HH:mm"),
                        r["WarehouseName"],
                        r["ProductCode"],
                        r["ProductName"],
                        r["Unit"],
                        Convert.ToDecimal(r["BookQty"]).ToString("N3"),
                        Convert.ToDecimal(r["ActualQty"]).ToString("N3"),
                        (diff > 0 ? "+" : "") + diff.ToString("N3"),
                        typeStr,
                        purchasePrice.ToString("N2"),
                        salePrice.ToString("N2"),
                        shortageLoss > 0 ? shortageLoss.ToString("N2") : "0.00",
                        surplusGain > 0 ? surplusGain.ToString("N2") : "0.00",
                        r["CreatedBy"],
                        r["Notes"]
                    );

                    if (diff < 0)
                    {
                        dgGrid.Rows[ri].Cells["DiffQty"].Style.ForeColor = Color.OrangeRed;
                        dgGrid.Rows[ri].Cells["DiffType"].Style.ForeColor = Color.DarkRed;
                        dgGrid.Rows[ri].Cells["ShortageLoss"].Style.ForeColor = Color.DarkRed;
                        dgGrid.Rows[ri].Cells["ShortageLoss"].Style.Font = Theme.FontBold;
                    }
                    else if (diff > 0)
                    {
                        dgGrid.Rows[ri].Cells["DiffQty"].Style.ForeColor = Color.DarkGreen;
                        dgGrid.Rows[ri].Cells["DiffType"].Style.ForeColor = Color.DarkGreen;
                        dgGrid.Rows[ri].Cells["SurplusGain"].Style.ForeColor = Color.DarkGreen;
                        dgGrid.Rows[ri].Cells["SurplusGain"].Style.Font = Theme.FontBold;
                    }
                }

                // Update Cards
                lblTotalShortageCost.Text = $"{totalShortageCost:N2} ج";
                lblTotalShortageQty.Text = $"كمية العجز: {totalShortageQty:N3}";

                lblTotalSurplusCost.Text = $"{totalSurplusCost:N2} ج";
                lblTotalSurplusQty.Text = $"كمية الزيادة: {totalSurplusQty:N3}";

                decimal netCost = totalSurplusCost - totalShortageCost;
                if (netCost < 0)
                {
                    lblNetCostDiff.Text = $"- {Math.Abs(netCost):N2} ج (عجز صافي)";
                    lblNetCostDiff.ForeColor = Color.OrangeRed;
                }
                else if (netCost > 0)
                {
                    lblNetCostDiff.Text = $"+ {netCost:N2} ج (زيادة صافية)";
                    lblNetCostDiff.ForeColor = Color.DarkGreen;
                }
                else
                {
                    lblNetCostDiff.Text = "0.00 ج (متكافئ)";
                    lblNetCostDiff.ForeColor = Color.DarkBlue;
                }

                lblTotalShortageSale.Text = $"{totalShortageSale:N2} ج";
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل بيانات تقرير فروق الجرد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (dgGrid == null || dgGrid.Rows.Count == 0)
            {
                Theme.ShowMsg("لا توجد بيانات للتصدير.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "ملف CSV (*.csv)|*.csv";
                dlg.FileName = $"تقرير_فروق_وتسويات_الجرد_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();

                        var headers = new List<string>();
                        foreach (DataGridViewColumn col in dgGrid.Columns)
                        {
                            if (col.Visible)
                                headers.Add($"\"{col.HeaderText.Replace("\"", "\"\"")}\"");
                        }
                        sb.AppendLine(string.Join(",", headers));

                        foreach (DataGridViewRow row in dgGrid.Rows)
                        {
                            if (row.IsNewRow) continue;
                            var cells = new List<string>();
                            foreach (DataGridViewColumn col in dgGrid.Columns)
                            {
                                if (col.Visible)
                                {
                                    string val = row.Cells[col.Index].Value?.ToString() ?? "";
                                    cells.Add($"\"{val.Replace("\"", "\"\"")}\"");
                                }
                            }
                            sb.AppendLine(string.Join(",", cells));
                        }

                        System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        Theme.ShowMsg("✅ تم تصدير التقرير بنجاح!\nيمكنك الآن فتح الملف باستخدام برنامج Excel.", "تم التصدير بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Theme.ShowMsg("❌ فشل تصدير الملف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private int _printRowIndex = 0;

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (dgGrid.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لطباعتها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _printRowIndex = 0;
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);

            pd.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                var boldTitle = new Font("Arial", 16, FontStyle.Bold);
                var boldHeader = new Font("Arial", 10, FontStyle.Bold);
                var normalFont = new Font("Arial", 9);
                var smallBold = new Font("Arial", 9, FontStyle.Bold);
                var center = new StringFormat { Alignment = StringAlignment.Center };

                int y = 30;
                int pageW = 800;

                // Title Header
                g.DrawString("تقرير فروق وتسويات الجرد والعجز والزيادة والتقييم المالي", boldTitle, Brushes.DarkBlue, new RectangleF(20, y, pageW - 40, 30), center);
                y += 30;
                g.DrawString($"الفترة: من {dtpFrom.Value:dd/MM/yyyy} إلى {dtpTo.Value:dd/MM/yyyy}  |  المخزن: {cboWarehouse.Text}", normalFont, Brushes.Black, new RectangleF(20, y, pageW - 40, 20), center);
                y += 22;
                g.DrawLine(new Pen(Color.DarkBlue, 2), 20, y, pageW - 20, y);
                y += 12;

                // Metrics summary line on page 1
                if (_printRowIndex == 0)
                {
                    string shortageStr = $"{lblTotalShortageCost.Text} ({lblTotalShortageQty.Text})".Replace("🔴 ", "");
                    string surplusStr = $"{lblTotalSurplusCost.Text}".Replace("🟢 ", "");
                    string netStr = $"{lblNetCostDiff.Text}".Replace("⚖️ ", "");

                    string summaryLine = $"إجمالي العجز: {shortageStr}   |   إجمالي الزيادة: {surplusStr}   |   الصافي: {netStr}";
                    g.DrawString(summaryLine, smallBold, Brushes.Black, new RectangleF(20, y, pageW - 40, 20), center);
                    y += 22;
                    g.DrawLine(Pens.Gray, 20, y, pageW - 20, y);
                    y += 10;
                }

                // Table Header
                // Total printable width = 760 (from x=20 to x=780)
                int[] xCols = { 20, 95, 175, 240, 455, 510, 565, 620, 680 };
                int[] wCols = { 75, 80,  65,  210,  50,  50,  50,  55, 100 };
                string[] headers = { "التاريخ", "المخزن", "كود الصنف", "اسم الصنف", "الدفتري", "الفعلي", "الفارق", "نوع الحركة", "خسارة العجز / الزيادة" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var rectHeader = new RectangleF(xCols[i], y, wCols[i], 20);
                    var sfHeader = new StringFormat { Alignment = i >= 4 ? StringAlignment.Center : StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
                    g.DrawString(headers[i], boldHeader, Brushes.DarkBlue, rectHeader, sfHeader);
                }
                y += 22;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y);
                y += 8;

                int maxY = 1080;

                while (_printRowIndex < dgGrid.Rows.Count)
                {
                    var r = dgGrid.Rows[_printRowIndex];
                    string date = r.Cells["AdjDate"].Value?.ToString();
                    if (date != null && date.Length >= 10) date = date.Substring(0, 10);
                    string wh = r.Cells["WarehouseName"].Value?.ToString() ?? "";
                    string code = r.Cells["ProductCode"].Value?.ToString() ?? "";
                    string name = r.Cells["ProductName"].Value?.ToString() ?? "";
                    string book = r.Cells["BookQty"].Value?.ToString() ?? "0";
                    string actual = r.Cells["ActualQty"].Value?.ToString() ?? "0";
                    string diff = r.Cells["DiffQty"].Value?.ToString() ?? "0";
                    string type = r.Cells["DiffType"].Value?.ToString() ?? "";
                    string lossGain = "";

                    decimal.TryParse(r.Cells["ShortageLoss"].Value?.ToString(), out decimal sl);
                    decimal.TryParse(r.Cells["SurplusGain"].Value?.ToString(), out decimal sg);
                    if (sl > 0) lossGain = $"-{sl:N2} ج";
                    else if (sg > 0) lossGain = $"+{sg:N2} ج";
                    else lossGain = "0.00";

                    var sfNear = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                    var sfCenter = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

                    g.DrawString(date, normalFont, Brushes.Black, new RectangleF(xCols[0], y, wCols[0], 20), sfNear);
                    g.DrawString(wh, normalFont, Brushes.Black, new RectangleF(xCols[1], y, wCols[1], 20), sfNear);
                    g.DrawString(code, normalFont, Brushes.Black, new RectangleF(xCols[2], y, wCols[2], 20), sfNear);
                    g.DrawString(name, normalFont, Brushes.Black, new RectangleF(xCols[3], y, wCols[3], 20), sfNear);
                    g.DrawString(book, normalFont, Brushes.Black, new RectangleF(xCols[4], y, wCols[4], 20), sfCenter);
                    g.DrawString(actual, normalFont, Brushes.Black, new RectangleF(xCols[5], y, wCols[5], 20), sfCenter);
                    g.DrawString(diff, smallBold, diff.StartsWith("-") ? Brushes.Red : (diff.StartsWith("+") ? Brushes.Green : Brushes.Black), new RectangleF(xCols[6], y, wCols[6], 20), sfCenter);
                    g.DrawString(type, normalFont, Brushes.Black, new RectangleF(xCols[7], y, wCols[7], 20), sfCenter);
                    g.DrawString(lossGain, smallBold, sl > 0 ? Brushes.Red : (sg > 0 ? Brushes.Green : Brushes.Black), new RectangleF(xCols[8], y, wCols[8], 20), sfCenter);

                    y += 22;
                    _printRowIndex++;

                    if (y >= maxY && _printRowIndex < dgGrid.Rows.Count)
                    {
                        ev.HasMorePages = true;
                        return;
                    }
                }

                ev.HasMorePages = false;
                y += 15;
                if (y < maxY)
                {
                    g.DrawLine(new Pen(Color.DarkBlue, 1.5f), 20, y, pageW - 20, y);
                    y += 10;
                    g.DrawString("اعتماد مسئول الجرد: .......................................         التوقيع: .......................................", smallBold, Brushes.Black, 20, y);
                }
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 950,
                Height = 800,
                Text = "معاينة طباعة تقرير فروق وتسويات الجرد (A4)"
            };
            preview.ShowDialog();
        }

        private void BtnPrintSurplusBarcodes_Click(object sender, EventArgs e)
        {
            if (_dtData == null || _dtData.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات معروضة في التقرير حالياً. اضغط عرض التقرير أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var itemsToPrint = new List<BarcodePrintItem>();
            foreach (DataRow r in _dtData.Rows)
            {
                decimal diff = Convert.ToDecimal(r["DiffQty"]);
                if (diff > 0)
                {
                    int pid = Convert.ToInt32(r["ProductID"]);
                    string name = r["ProductName"].ToString();
                    string code = r["ProductCode"].ToString();
                    string shelf = r["ShelfLocation"] != DBNull.Value ? r["ShelfLocation"].ToString() : "";
                    decimal salePrice = Convert.ToDecimal(r["SalePrice"]);

                    int qty = (int)Math.Ceiling(diff);
                    if (qty <= 0) qty = 1;

                    itemsToPrint.Add(new BarcodePrintItem
                    {
                        ProductID = pid,
                        ProductName = name,
                        ProductCode = code,
                        Price = salePrice,
                        PrintQty = qty,
                        ShelfLocation = shelf
                    });
                }
            }

            if (itemsToPrint.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف بها زيادة بالكميات (+فائض) في نتائج التقرير الحالي.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            new FrmBulkPrintBarcodes(itemsToPrint).ShowDialog(this);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmDailyClosing : Form
    {
        // ── Controls ────────────────────────────────────────────────────────────
        private DateTimePicker _dtpDate;
        private DataGridView   _dg;
        private Label          _lblTotalInvoice, _lblTotalPayment, _lblTotalBalance;
        private Label          _lblProductCountInfo;
        private Panel          _pnlSummary;

        // ── State ────────────────────────────────────────────────────────────────
        private DataTable _products;          // active products
        private int       _productCount;      // number of product columns
        private decimal   _grandInvoice, _grandPayment, _grandBalance;

        public FrmDailyClosing()
        {
            BuildUI();
            LoadReport();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  UI Construction
        // ════════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            Text            = "تقرير التقفيل اليومي";
            BackColor       = Theme.BgMain;
            RightToLeft     = RightToLeft.Yes;
            Font            = Theme.FontMain;

            // ── Title bar ─────────────────────────────────────────────────────
            var titleBar = Theme.MakeTitleBar(
                "📋 تقرير التقفيل اليومي",
                $"إجمالي المبيعات  |  آخر توريد  |  مديونية العملاء");
            Controls.Add(titleBar);

            // ── Top toolbar ───────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = Theme.BgCard,
                Padding   = new Padding(10, 8, 10, 8)
            };

            var lblDate = new Label
            {
                Text      = "تاريخ اليوم:",
                Font      = Theme.FontBold,
                ForeColor = Theme.TextMain,
                AutoSize  = true,
                Dock      = DockStyle.Right,
                Margin    = new Padding(0, 4, 12, 0)
            };

            _dtpDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today,
                Width  = 130,
                Dock   = DockStyle.Right
            };
            _dtpDate.RightToLeftLayout = true;
            _dtpDate.ValueChanged += (s, e) => LoadReport();

            var btnLoad = Theme.MakeButton("🔄 تحديث", Theme.Accent);
            btnLoad.Size   = new Size(110, 34);
            btnLoad.Dock   = DockStyle.Right;
            btnLoad.Click += (s, e) => LoadReport();

            var btnPrint = Theme.MakeButton("🖨️ طباعة", Theme.Primary);
            btnPrint.Size   = new Size(110, 34);
            btnPrint.Dock   = DockStyle.Right;
            btnPrint.Click += BtnPrint_Click;

            var btnWhatsApp = Theme.MakeButton("📲 واتساب التقفيل", Theme.Accent);
            btnWhatsApp.Size   = new Size(140, 34);
            btnWhatsApp.BackColor = Color.FromArgb(37, 211, 102);
            btnWhatsApp.Dock   = DockStyle.Right;
            btnWhatsApp.Click += BtnWhatsAppClosing_Click;

            _lblProductCountInfo = new Label
            {
                Text = "📦 الأصناف المباعة: 0",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(10, 8, 10, 0)
            };

            toolbar.Controls.AddRange(new Control[] { lblDate, _dtpDate, btnLoad, btnPrint, btnWhatsApp, _lblProductCountInfo });
            Controls.Add(toolbar);

            // ── Summary footer ────────────────────────────────────────────────
            _pnlSummary = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 70,
                BackColor = Theme.BgCard
            };
            _pnlSummary.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Accent, 2f))
                    e.Graphics.DrawLine(pen, 0, 0, _pnlSummary.Width, 0);
            };

            _lblTotalInvoice = MakeSummaryLabel("إجمالي فواتير البيع: --", Theme.Accent);
            _lblTotalPayment = MakeSummaryLabel("إجمالي التوريد: --",     Theme.Success);
            _lblTotalBalance = MakeSummaryLabel("إجمالي أرصدة عملاء اليوم: --",   Color.FromArgb(231, 76, 60));

            var summaryFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents  = false,
                Padding       = new Padding(10, 12, 10, 0)
            };
            summaryFlow.Controls.AddRange(new Control[]
                { _lblTotalInvoice, _lblTotalPayment, _lblTotalBalance });
            _pnlSummary.Controls.Add(summaryFlow);
            Controls.Add(_pnlSummary);

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
                SelectionMode                 = DataGridViewSelectionMode.CellSelect,
                RightToLeft                   = RightToLeft.Yes,
                GridColor                     = Theme.BorderColor,
                AutoSizeColumnsMode           = DataGridViewAutoSizeColumnsMode.AllCells,
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
                    Font       = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Alignment  = DataGridViewContentAlignment.MiddleCenter
                }
            };
            Controls.Add(_dg);

            // ensure Z-order: title → toolbar → grid → footer
            titleBar.BringToFront();
            toolbar.BringToFront();
            _dg.BringToFront();
            _pnlSummary.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private Label MakeSummaryLabel(string text, Color color)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = color,
                AutoSize  = true,
                Margin    = new Padding(30, 0, 10, 0)
            };
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Data Loading & Pivot
        // ════════════════════════════════════════════════════════════════════════
        private void LoadReport()
        {
            try
            {
                DateTime date = _dtpDate.Value.Date;

                // 1. Quantities: client × product
                var dtQty    = ReportDAL.GetDailyClientProductSales(date);

                // 2. Totals: invoice, last payment, balance per client
                var dtTotals = ReportDAL.GetDailyClientTotals(date);

                // 3. Extract unique product IDs that had sales on this date
                var soldProductIDs = new HashSet<int>();
                foreach (DataRow r in dtQty.Rows)
                {
                    if (Convert.ToDecimal(r["TotalQty"]) != 0)
                        soldProductIDs.Add(Convert.ToInt32(r["ProductID"]));
                }

                // Fetch products: ONLY products that were sold on this date
                var allProducts = ProductDAL.GetAll(activeOnly: false);
                var pageProductRows = allProducts.Rows.Cast<DataRow>()
                    .Where(r => soldProductIDs.Contains(Convert.ToInt32(r["ProductID"])))
                    .OrderBy(r => r["ProductName"].ToString())
                    .ToList();

                _productCount = pageProductRows.Count;
                _lblProductCountInfo.Text = _productCount > 0 
                    ? $"📦 الأصناف المباعة اليوم: {_productCount} صنف"
                    : "📦 لا توجد مبيعات أصناف اليوم";

                // ── Build lookup: clientID → { productID → qty }
                var qtyMap = new Dictionary<int, Dictionary<int, decimal>>();
                foreach (DataRow r in dtQty.Rows)
                {
                    int cid = Convert.ToInt32(r["ClientID"]);
                    int pid = Convert.ToInt32(r["ProductID"]);
                    decimal q = Convert.ToDecimal(r["TotalQty"]);
                    if (!qtyMap.ContainsKey(cid)) qtyMap[cid] = new Dictionary<int, decimal>();
                    qtyMap[cid][pid] = q;
                }

                // ── Build lookup: clientID → (name, invoice, lastPayment, balance)
                var totMap = new Dictionary<int, (string name, decimal inv, decimal pay, decimal bal)>();
                var clientOrder = new List<int>();
                foreach (DataRow r in dtTotals.Rows)
                {
                    int cid = Convert.ToInt32(r["ClientID"]);
                    totMap[cid] = (
                        r["ClientName"].ToString(),
                        Convert.ToDecimal(r["TotalInvoice"]),
                        Convert.ToDecimal(r["LastPayment"]),
                        Convert.ToDecimal(r["Balance"])
                    );
                    if (!clientOrder.Contains(cid)) clientOrder.Add(cid);
                }
                // clients that appear in qty but not in totals (edge case)
                foreach (int cid in qtyMap.Keys)
                    if (!clientOrder.Contains(cid)) clientOrder.Add(cid);

                // ── Rebuild grid ───────────────────────────────────────────────
                _dg.Columns.Clear();
                _dg.Rows.Clear();

                // Column: client name (fixed, wide)
                _dg.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name         = "ClientName",
                    HeaderText   = "اسم العميل",
                    MinimumWidth = 140,
                    FillWeight   = 1,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                        ForeColor = Color.White
                    }
                });

                // Columns: for products in current page (max 20)
                foreach (DataRow pr in pageProductRows)
                {
                    _dg.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name         = "P_" + pr["ProductID"],
                        HeaderText   = pr["ProductName"].ToString(),
                        MinimumWidth = 68,
                        FillWeight   = 1,
                        Tag          = pr
                    });
                }

                // Extra columns
                _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalInvoice", HeaderText = "إجمالي الفاتورة", MinimumWidth = 100, FillWeight = 1,
                    DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.Accent, Alignment = DataGridViewContentAlignment.MiddleCenter } });
                _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastPayment",  HeaderText = "آخر توريد",       MinimumWidth = 100, FillWeight = 1,
                    DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.Success, Alignment = DataGridViewContentAlignment.MiddleCenter } });
                _dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance",      HeaderText = "المديونية",        MinimumWidth = 100, FillWeight = 1,
                    DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(231, 76, 60), Alignment = DataGridViewContentAlignment.MiddleCenter } });

                int totalCols = _dg.Columns.Count;

                // ── Price row ──────────────────────────────────────────────────
                var priceVals = new object[totalCols];
                priceVals[0] = "السعر";
                for (int i = 0; i < _productCount; i++)
                {
                    decimal price = Convert.ToDecimal(pageProductRows[i]["SalePrice"]);
                    priceVals[i + 1] = price > 0 ? price.ToString("N2") : "-";
                }
                priceVals[_productCount + 1] = "";
                priceVals[_productCount + 2] = "";
                priceVals[_productCount + 3] = "";

                int priceRowIdx = _dg.Rows.Add(priceVals);
                StyleSpecialRow(_dg.Rows[priceRowIdx],
                    Color.FromArgb(26, 43, 90),
                    Color.FromArgb(243, 156, 18),
                    new Font("Segoe UI", 9.5f, FontStyle.Bold));

                // ── Client rows ────────────────────────────────────────────────
                _grandInvoice = _grandPayment = _grandBalance = 0m;
                bool alternate = false;

                foreach (int cid in clientOrder)
                {
                    var row = new object[totalCols];

                    string clientName = totMap.ContainsKey(cid) ? totMap[cid].name : "عميل";
                    row[0] = clientName;

                    for (int i = 0; i < _productCount; i++)
                    {
                        int pid = Convert.ToInt32(pageProductRows[i]["ProductID"]);
                        decimal qty = 0;
                        if (qtyMap.ContainsKey(cid) && qtyMap[cid].ContainsKey(pid))
                            qty = qtyMap[cid][pid];
                        row[i + 1] = qty != 0 ? qty.ToString("N0") : "";
                    }

                    decimal inv = 0, pay = 0, bal = 0;
                    if (totMap.ContainsKey(cid))
                        (_, inv, pay, bal) = totMap[cid];

                    row[_productCount + 1] = inv.ToString("N2");
                    row[_productCount + 2] = pay.ToString("N2");
                    row[_productCount + 3] = bal.ToString("N2");

                    _grandInvoice += inv;
                    _grandPayment += pay;
                    _grandBalance += bal;

                    int ri = _dg.Rows.Add(row);
                    _dg.Rows[ri].DefaultCellStyle.BackColor =
                        alternate ? Color.FromArgb(40, 48, 65) : Theme.BgCard;
                    alternate = !alternate;
                }

                // ── Totals row ─────────────────────────────────────────────────
                var totVals = new object[totalCols];
                totVals[0] = "الإجمالي الكلي";
                for (int i = 1; i <= _productCount; i++) totVals[i] = "";
                totVals[_productCount + 1] = _grandInvoice.ToString("N2");
                totVals[_productCount + 2] = _grandPayment.ToString("N2");
                totVals[_productCount + 3] = _grandBalance.ToString("N2");

                int totRowIdx = _dg.Rows.Add(totVals);
                StyleSpecialRow(_dg.Rows[totRowIdx],
                    Color.FromArgb(30, 60, 30),
                    Color.LightGreen,
                    new Font("Segoe UI", 10.5f, FontStyle.Bold));

                // ── Summary labels ─────────────────────────────────────────────
                _lblTotalInvoice.Text = $"إجمالي فواتير البيع: {_grandInvoice:N2} ج.م";
                _lblTotalPayment.Text = $"إجمالي آخر توريد: {_grandPayment:N2} ج.م";
                _lblTotalBalance.Text = $"إجمالي أرصدة عملاء اليوم: {_grandBalance:N2} ج.م";
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل التقرير:\n" + ex.Message,
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void StyleSpecialRow(DataGridViewRow row, Color bg, Color fg, Font font)
        {
            row.DefaultCellStyle.BackColor = bg;
            row.DefaultCellStyle.ForeColor = fg;
            row.DefaultCellStyle.Font      = font;
            row.DefaultCellStyle.SelectionBackColor = bg;
            row.DefaultCellStyle.SelectionForeColor = fg;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Printing
        // ════════════════════════════════════════════════════════════════════════
        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (_dg.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var pd  = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.Landscape = true;
            pd.DefaultPageSettings.Margins   = new Margins(20, 20, 30, 30);

            int pageRow = 0;
            pd.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                // No PageUnit setting - allow Default GraphicUnit.Display (1/100 inch) to automatically match ev.PageBounds

                var fTitle  = new Font("Arial", 13f, FontStyle.Bold);
                var fHead   = new Font("Arial", 7.5f, FontStyle.Bold);
                var fCell   = new Font("Arial", 7f);
                var fTotal  = new Font("Arial", 8f, FontStyle.Bold);

                int pgW   = (int)ev.PageBounds.Width - 40;
                int y     = 20;

                // ─ Title
                string title = $"{AppConfig.CompanyName}  -  تقرير التقفيل اليومي  –  {_dtpDate.Value:dd/MM/yyyy}";
                var sfTitle = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                var tsz = g.MeasureString(title, fTitle);
                g.DrawString(title, fTitle, Brushes.DarkBlue, new RectangleF(20, y, pgW, tsz.Height), sfTitle);
                y += (int)tsz.Height + 4;

                string sub = $"إجمالي فواتير البيع: {_grandInvoice:N2}   |   إجمالي التوريد: {_grandPayment:N2}   |   إجمالي أرصدة عملاء اليوم: {_grandBalance:N2}   ج.م";
                var sfSub = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                var ssz = g.MeasureString(sub, fHead);
                g.DrawString(sub, fHead, Brushes.DarkGray, new RectangleF(20, y, pgW, ssz.Height), sfSub);
                y += (int)ssz.Height + 6;

                g.DrawLine(new Pen(Color.DarkBlue, 1.5f), 20, y, pgW + 20, y);
                y += 6;

                var orderedCols = _dg.Columns.Cast<DataGridViewColumn>()
                                    .Where(c => c.Visible)
                                    .OrderBy(c => c.DisplayIndex)
                                    .ToList();

                // ─ Compute column widths proportionally
                int visColCount = orderedCols.Count;
                int[] widths = ComputePrintWidths(pgW, visColCount, orderedCols);

                // ─ Header row (only on first page)
                if (pageRow == 0)
                {
                    int cx = pgW + 20;
                    for (int i = 0; i < orderedCols.Count; i++)
                    {
                        var col = orderedCols[i];
                        int cw     = widths[i];
                        cx -= cw;
                        var rect   = new RectangleF(cx, y, cw - 2, 22);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(26, 43, 75)), rect);
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                        g.DrawString(col.HeaderText, fHead, Brushes.White, rect, sf);
                    }
                    y += 24;
                }

                // ─ Data rows
                while (pageRow < _dg.Rows.Count)
                {
                    var dgRow  = _dg.Rows[pageRow];
                    bool isPrice = pageRow == 0;
                    bool isTotal = dgRow.Cells[0].Value?.ToString() == "الإجمالي الكلي";

                    var rowFont   = isTotal || isPrice ? fTotal : fCell;
                    var rowBgClr  = isPrice  ? Color.FromArgb(220, 230, 245)
                                  : isTotal  ? Color.FromArgb(220, 245, 220)
                                  : (pageRow % 2 == 0) ? Color.White : Color.FromArgb(248, 248, 252);
                    var rowFgClr  = isTotal ? Color.DarkGreen : Color.Black;

                    int cx = pgW + 20;
                    for (int i = 0; i < orderedCols.Count; i++)
                    {
                        var col = orderedCols[i];
                        int cw   = widths[i];
                        string v = dgRow.Cells[col.Name].Value?.ToString() ?? "";
                        cx -= cw;
                        var rect = new RectangleF(cx, y, cw - 2, 18);
                        g.FillRectangle(new SolidBrush(rowBgClr), rect);
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };
                        g.DrawString(v, rowFont, new SolidBrush(rowFgClr), rect, sf);
                    }

                    // row separator
                    g.DrawLine(Pens.LightGray, 20, y + 18, pgW + 20, y + 18);
                    y += 19;
                    pageRow++;

                    if (y > ev.PageBounds.Height - 50)
                    {
                        ev.HasMorePages = true;
                        return;
                    }
                }

                // ─ Page footer
                g.DrawLine(new Pen(Color.Gray, 1f), 20, y + 4, pgW + 20, y + 4);
                y += 8;
                g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}", fCell, Brushes.Gray, 20, y);

                pageRow = 0; // reset for next print call
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width    = 1100,
                Height   = 800
            };
            preview.ShowDialog();
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private int[] ComputePrintWidths(int pgW, int colCount, System.Collections.Generic.List<DataGridViewColumn> orderedCols)
        {
            // client name = 12%, total/pay/bal cols = 8% each, product cols share the rest
            int clientW  = (int)(pgW * 0.12);
            int extraW   = (int)(pgW * 0.08);  // per extra col (3 extra cols)
            int usedW    = clientW + extraW * 3;
            int prodW    = colCount > 4 ? (pgW - usedW) / (_productCount == 0 ? 1 : _productCount) : 60;
            if (prodW < 15) prodW = 15;

            var ws = new int[colCount];
            int vi = 0;
            foreach (var col in orderedCols)
            {
                if (col.Name == "ClientName")         ws[vi] = clientW;
                else if (col.Name == "TotalInvoice" ||
                         col.Name == "LastPayment"  ||
                         col.Name == "Balance")       ws[vi] = extraW;
                else                                  ws[vi] = prodW;
                vi++;
            }
            return ws;
        }
        private void BtnWhatsAppClosing_Click(object sender, EventArgs e)
        {
            if (_dg.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للإرسال.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // مربع حوار لإدخال رقم الهاتف
            var dlg = new Form
            {
                Width = 420, Height = 190,
                Text = "إرسال واتساب - التقفيل اليومي",
                StartPosition = FormStartPosition.CenterParent,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain
            };
            var lbl = new Label { Text = "📱 أدخل رقم الواتساب (مثال: 01012345678):", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(10, 15) };
            var txt = new TextBox { Location = new Point(10, 42), Width = 380, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle };
            var btnSend = Theme.MakeButton("✅ إرسال", 230, 90, 150, 36, Color.FromArgb(37, 211, 102));
            var btnCancel = Theme.MakeButton("❌ إلغاء", 60, 90, 150, 36, Color.FromArgb(180, 60, 60));
            btnSend.Click   += (s2, e2) => { dlg.DialogResult = DialogResult.OK;     dlg.Close(); };
            btnCancel.Click += (s2, e2) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
            dlg.Controls.AddRange(new Control[] { lbl, txt, btnSend, btnCancel });

            if (dlg.ShowDialog() != DialogResult.OK) return;
            string phone = txt.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone)) return;

            // بناء نص رسالة التقفيل
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📋 *تقرير التقفيل اليومي*");
            sb.AppendLine($"🏢 {AppConfig.CompanyName}");
            sb.AppendLine($"📅 التاريخ: {_dtpDate.Value:dd/MM/yyyy}");
            sb.AppendLine("──────────────────────");
            sb.AppendLine($"💰 إجمالي فواتير البيع: {_grandInvoice:N2} ج.م");
            sb.AppendLine($"✅ إجمالي التوريد: {_grandPayment:N2} ج.م");
            sb.AppendLine($"📊 إجمالي المديونية: {_grandBalance:N2} ج.م");
            sb.AppendLine("──────────────────────");

            // تفاصيل كل عميل (تخطي صف السعر وصف الإجمالي)
            foreach (DataGridViewRow row in _dg.Rows)
            {
                string clientName = row.Cells["ClientName"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(clientName)) continue;
                if (clientName == "السعر" || clientName == "الإجمالي الكلي") continue;

                string inv = row.Cells["TotalInvoice"].Value?.ToString() ?? "0";
                string pay = row.Cells["LastPayment"].Value?.ToString() ?? "0";
                string bal = row.Cells["Balance"].Value?.ToString() ?? "0";
                sb.AppendLine($"• {clientName}");
                sb.AppendLine($"  فاتورة: {inv} | توريد: {pay} | مديونية: {bal} ج.م");
            }

            sb.AppendLine("──────────────────────");

            SendWhatsApp(phone, sb.ToString());
        }

        private static void SendWhatsApp(string phone, string message)
        {
            try
            {
                string clean = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
                if (clean.StartsWith("0")) clean = "20" + clean.Substring(1);
                
                string encoded = "";
                if (message.Length > 600 || Uri.EscapeDataString(message).Length > 1800)
                {
                    Clipboard.SetText(message);
                    MessageBox.Show(
                        "⚠️ نظراً لأن التقرير طويل جداً، تم نسخه بالكامل إلى الحافظة (Clipboard) تلقائياً.\n" +
                        "يرجى الضغط على لصق (Ctrl + V) داخل محادثة الواتساب التي ستفتح الآن لإرساله.",
                        "تم نسخ التقرير", MessageBoxButtons.OK, MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                        
                    encoded = Uri.EscapeDataString("📋 تقرير الإغلاق اليومي (تم نسخ التفاصيل للحافظة، يرجى اللصق وإرسال)");
                }
                else
                {
                    encoded = Uri.EscapeDataString(message);
                }
                
                // 1. Try to open the WhatsApp Desktop App protocol
                string appUrl = $"whatsapp://send?phone={clean}&text={encoded}";
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(appUrl) { UseShellExecute = true });
                    return;
                }
                catch { }

                // 2. Try to open wa.me link directly via shell
                string waUrl = $"https://wa.me/{clean}?text={encoded}";
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(waUrl) { UseShellExecute = true });
                    return;
                }
                catch { }

                // 3. Fallback: Launch via explorer.exe (highly robust in Windows)
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{waUrl}\"");
                    return;
                }
                catch { }

                // 4. Try WhatsApp Web as a last resort via explorer.exe
                string webUrl = $"https://web.whatsapp.com/send?phone={clean}&text={encoded}";
                System.Diagnostics.Process.Start("explorer.exe", $"\"{webUrl}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر فتح واتساب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

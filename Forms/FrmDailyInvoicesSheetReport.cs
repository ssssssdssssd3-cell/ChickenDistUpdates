using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmDailyInvoicesSheetReport : Form
    {
        // ── Filter Controls ──────────────────────────────────────────────
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private ComboBox cboWarehouse;
        private ComboBox cboClient;
        private TextBox txtSearch;
        private Button btnLoad;
        private Button btnPrint;
        private Button btnPreview;
        private Button btnExportPdf;

        // ── Summary Labels ──────────────────────────────────────────────
        private Label lblTotalInvoicesCount;
        private Label lblTotalItemsCount;
        private Label lblTotalSalesSum;
        private Label lblTotalReturnsSum;
        private Label lblTotalNetSum;

        // ── Data & State ────────────────────────────────────────────────
        private List<DailyInvoiceReportItem> _allInvoices = new List<DailyInvoiceReportItem>();
        private List<DailyInvoiceReportItem> _filteredInvoices = new List<DailyInvoiceReportItem>();
        private DataGridView dgPreviewSummary;

        // ── Printing State ──────────────────────────────────────────────
        private int _printInvoiceIndex = 0;
        private int _printLineIndex = 0;
        private int _printPageNum = 1;

        public FrmDailyInvoicesSheetReport(DateTime? initialDate = null)
        {
            InitializeComponent();
            if (initialDate.HasValue)
            {
                dtpFrom.Value = initialDate.Value.Date;
                dtpTo.Value = initialDate.Value.Date.AddDays(1).AddSeconds(-1);
            }
            LoadWarehouses();
            LoadClients();
            LoadReportData();
        }

        private void InitializeComponent()
        {
            this.Text = "📑 تقرير فواتير البيع التفصيلي اليومي (أصناف وفواتير)";
            this.Size = new Size(1250, 750);
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── Title Bar ───────────────────────────────────────────────
            var titleBar = Theme.MakeTitleBar(
                "📑 تقرير فواتير البيع التفصيلي اليومي",
                "استخراج وطباعة فواتير البيع مع كافة الأصناف والخصومات والمرتجعات على هيئة شيت مطبوع A4");
            this.Controls.Add(titleBar);

            // ── Top Filters Panel ───────────────────────────────────────
            var pnlFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 8, 8, 8)
            };

            var flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true
            };

            // Date From
            dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 115, Value = DateTime.Today };
            flowFilters.Controls.Add(MakeFilterBox("📅 من تاريخ:", dtpFrom, 115));

            // Date To
            dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 115, Value = DateTime.Today };
            flowFilters.Controls.Add(MakeFilterBox("📅 إلى تاريخ:", dtpTo, 115));

            // Warehouse
            cboWarehouse = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Font = Theme.FontMain };
            flowFilters.Controls.Add(MakeFilterBox("🏢 المخزن:", cboWarehouse, 130));

            // Client
            cboClient = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Font = Theme.FontMain };
            flowFilters.Controls.Add(MakeFilterBox("👤 العميل:", cboClient, 140));

            // Search
            txtSearch = new TextBox { Width = 140, Font = Theme.FontBold, BackColor = Color.White, ForeColor = Color.FromArgb(15, 23, 42) };
            txtSearch.TextChanged += (s, e) => ApplySearchFilter();
            flowFilters.Controls.Add(MakeFilterBox("🔍 تصفية سريعة:", txtSearch, 140));

            // Buttons
            btnLoad = Theme.MakeButton("🔄 عرض الفواتير", Color.FromArgb(245, 158, 11));
            btnLoad.Size = new Size(115, 36);
            btnLoad.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnLoad.Click += (s, e) => LoadReportData();
            flowFilters.Controls.Add(btnLoad);

            btnPreview = Theme.MakeButton("🔍 معاينة A4", Color.FromArgb(14, 165, 233));
            btnPreview.Size = new Size(110, 36);
            btnPreview.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPreview.Click += (s, e) => ShowPrintPreview();
            flowFilters.Controls.Add(btnPreview);

            btnPrint = Theme.MakeButton("🖨️ طباعة فورية", Color.FromArgb(37, 99, 235));
            btnPrint.Size = new Size(115, 36);
            btnPrint.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPrint.Click += (s, e) => DirectPrintReport();
            flowFilters.Controls.Add(btnPrint);

            btnExportPdf = Theme.MakeButton("📄 تصدير PDF", Color.FromArgb(220, 38, 38));
            btnExportPdf.Size = new Size(110, 36);
            btnExportPdf.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnExportPdf.Click += (s, e) => ExportToPdf();
            flowFilters.Controls.Add(btnExportPdf);

            pnlFilters.Controls.Add(flowFilters);
            this.Controls.Add(pnlFilters);

            // ── Bottom Summary Panel ────────────────────────────────────
            var pnlSummary = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10, 8, 10, 8)
            };

            var flowSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            lblTotalInvoicesCount = MakeSummaryBadge("🧾 عدد الفواتير: 0", Color.FromArgb(59, 130, 246));
            lblTotalItemsCount = MakeSummaryBadge("📦 إجمالي الأصناف: 0", Color.FromArgb(168, 85, 247));
            lblTotalSalesSum = MakeSummaryBadge("💰 إجمالي المبيعات: 0.00 ج", Color.FromArgb(16, 185, 129));
            lblTotalReturnsSum = MakeSummaryBadge("↩️ إجمالي المرتجعات: 0.00 ج", Color.FromArgb(239, 68, 68));
            lblTotalNetSum = MakeSummaryBadge("✔ الصافي النهائي: 0.00 ج", Color.FromArgb(245, 158, 11));

            flowSummary.Controls.AddRange(new Control[] {
                lblTotalInvoicesCount,
                lblTotalItemsCount,
                lblTotalSalesSum,
                lblTotalReturnsSum,
                lblTotalNetSum
            });
            pnlSummary.Controls.Add(flowSummary);
            this.Controls.Add(pnlSummary);

            // ── DataGridView Grid Preview ───────────────────────────────
            dgPreviewSummary = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Color.FromArgb(41, 60, 88),
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };

            SetupSummaryGridColumns();
            this.Controls.Add(dgPreviewSummary);

            // Set layout orders
            dgPreviewSummary.BringToFront();
        }

        private Panel MakeFilterBox(string labelText, Control control, int width)
        {
            var pnl = new Panel { Width = width + 10, Height = 48, Margin = new Padding(0, 0, 8, 0) };
            var lbl = new Label
            {
                Text = labelText,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            control.Dock = DockStyle.Bottom;
            pnl.Controls.Add(control);
            pnl.Controls.Add(lbl);
            return pnl;
        }

        private Label MakeSummaryBadge(string text, Color bg)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                BackColor = bg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(0, 0, 10, 0)
            };
        }

        private void SetupSummaryGridColumns()
        {
            dgPreviewSummary.Columns.Clear();
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", HeaderText = "رقم الفاتورة", FillWeight = 40f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleDate", HeaderText = "التاريخ والوقت", FillWeight = 60f });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleType", HeaderText = "نوع الفاتورة", FillWeight = 45f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientCode", HeaderText = "كود العميل", FillWeight = 35f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "اسم العميل", FillWeight = 85f });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemsCount", HeaderText = "ع.الأصناف", FillWeight = 35f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseAmount", HeaderText = "ق.الأساسي", FillWeight = 50f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "Discount", HeaderText = "الخصم", FillWeight = 40f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(249, 115, 22) } });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "Returns", HeaderText = "المرتجع", FillWeight = 40f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.FromArgb(239, 68, 68) } });
            dgPreviewSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetAmount", HeaderText = "الصافي النهائي", FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) } });
        }

        private void LoadWarehouses()
        {
            cboWarehouse.Items.Clear();
            cboWarehouse.Items.Add(new ComboItem(0, "كل المخازن"));
            try
            {
                var dt = WarehouseDAL.GetAll();
                foreach (DataRow r in dt.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
                }
            }
            catch { }
            cboWarehouse.SelectedIndex = 0;
        }

        private void LoadClients()
        {
            cboClient.Items.Clear();
            cboClient.Items.Add(new ComboItem(0, "كل العملاء"));
            try
            {
                var dt = ClientDAL.GetAll();
                foreach (DataRow r in dt.Rows)
                {
                    cboClient.Items.Add(new ComboItem(Convert.ToInt32(r["ClientID"]), r["ClientName"].ToString()));
                }
            }
            catch { }
            cboClient.SelectedIndex = 0;
        }

        public void LoadReportData()
        {
            DateTime f = dtpFrom.Value.Date;
            DateTime t = dtpTo.Value.Date.AddDays(1).AddSeconds(-1);
            int? wid = (cboWarehouse.SelectedItem is ComboItem wci && wci.ID > 0) ? (int?)wci.ID : null;
            int? cid = (cboClient.SelectedItem is ComboItem cci && cci.ID > 0) ? (int?)cci.ID : null;

            _allInvoices = LoadInvoicesFromDb(f, t, wid, cid);
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            string q = txtSearch.Text?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(q))
            {
                _filteredInvoices = new List<DailyInvoiceReportItem>(_allInvoices);
            }
            else
            {
                _filteredInvoices = _allInvoices.Where(inv =>
                    inv.SaleCode.ToLower().Contains(q) ||
                    inv.ClientName.ToLower().Contains(q) ||
                    inv.ClientCode.ToLower().Contains(q) ||
                    inv.Lines.Any(l => l.ProductCode.ToLower().Contains(q) || l.ProductName.ToLower().Contains(q))
                ).ToList();
            }

            // Populate Grid
            dgPreviewSummary.Rows.Clear();
            decimal totalSales = 0, totalReturns = 0, totalNet = 0;
            int totalItems = 0;

            foreach (var inv in _filteredInvoices)
            {
                totalSales += inv.BaseAmount;
                totalReturns += inv.ReturnAmount;
                totalNet += inv.NetAmount;
                totalItems += inv.ItemsCount;

                dgPreviewSummary.Rows.Add(
                    inv.SaleCode,
                    inv.SaleDate.ToString("dd/MM/yyyy hh:mm tt"),
                    inv.SaleType,
                    inv.ClientCode,
                    inv.ClientName,
                    inv.ItemsCount,
                    inv.BaseAmount.ToString("N2"),
                    inv.DiscountAmount.ToString("N2"),
                    inv.ReturnAmount.ToString("N2"),
                    inv.NetAmount.ToString("N2")
                );
            }

            lblTotalInvoicesCount.Text = $"🧾 عدد الفواتير: {_filteredInvoices.Count}";
            lblTotalItemsCount.Text = $"📦 إجمالي الأصناف: {totalItems}";
            lblTotalSalesSum.Text = $"💰 إجمالي المبيعات: {totalSales:N2} ج";
            lblTotalReturnsSum.Text = $"↩️ إجمالي المرتجعات: {totalReturns:N2} ج";
            lblTotalNetSum.Text = $"✔ الصافي النهائي: {totalNet:N2} ج";
        }

        public static List<DailyInvoiceReportItem> LoadInvoicesFromDb(DateTime fromDate, DateTime toDate, int? warehouseID = null, int? clientID = null)
        {
            var result = new List<DailyInvoiceReportItem>();
            try
            {
                DataTable dtSales = SaleDAL.GetAll(fromDate, toDate, clientID, null, warehouseID);
                if (dtSales == null || dtSales.Rows.Count == 0) return result;

                // Sort chronological or as entered
                var sortedRows = dtSales.Rows.Cast<DataRow>().OrderBy(r => Convert.ToDateTime(r["SaleDate"])).ToList();
                var saleIDs = sortedRows.Select(r => Convert.ToInt32(r["SaleID"])).ToList();

                string idList = string.Join(",", saleIDs);
                DataTable dtItems = DbHelper.Query($@"
                    SELECT si.SaleID, si.ProductID,
                           COALESCE(p.ProductCode, p.PartNumber, CAST(p.ProductID AS NVARCHAR(50))) AS ProductCode,
                           p.ProductName,
                           COALESCE(si.UnitName, p.Unit, N'قطعة') AS UnitName,
                           si.Quantity,
                           si.UnitPrice,
                           si.TotalPrice,
                           COALESCE(si.DiscountAmt, 0) AS DiscountAmt,
                           COALESCE(si.DiscountPct, 0) AS DiscountPct,
                           ISNULL(ret.PrevReturnedQty, 0) AS PrevReturnedQty
                    FROM SaleItems si
                    JOIN Products p ON si.ProductID = p.ProductID
                    LEFT JOIN (
                        SELECT sr.SaleID, ri.ProductID, SUM(ri.Quantity) AS PrevReturnedQty
                        FROM SalesReturns sr
                        JOIN ReturnItems ri ON sr.ReturnID = ri.ReturnID
                        WHERE sr.SaleID IN ({idList})
                        GROUP BY sr.SaleID, ri.ProductID
                    ) ret ON ret.SaleID = si.SaleID AND ret.ProductID = si.ProductID
                    WHERE si.SaleID IN ({idList})
                    ORDER BY si.SaleID, si.ItemID");

                var itemsGrouped = dtItems.Rows.Cast<DataRow>()
                    .GroupBy(r => Convert.ToInt32(r["SaleID"]))
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (DataRow sRow in sortedRows)
                {
                    int sid = Convert.ToInt32(sRow["SaleID"]);
                    string sType = sRow["SaleType"]?.ToString() ?? "Cash";
                    string typeAr = sType == "Cash" ? "كاش" : sType == "Credit" ? "آجل" : sType == "Visa" ? "فيزا" : sType == "Installment" ? "تقسيط" : sType == "DriverLoad" ? "تحميل مندوب" : sType;

                    var item = new DailyInvoiceReportItem
                    {
                        SaleID = sid,
                        SaleCode = sRow["SaleCode"].ToString(),
                        SaleDate = Convert.ToDateTime(sRow["SaleDate"]),
                        SaleType = typeAr,
                        PriceTier = sRow.Table.Columns.Contains("PriceTier") && sRow["PriceTier"] != DBNull.Value ? sRow["PriceTier"].ToString() : "قطاعي",
                        ClientCode = sRow.Table.Columns.Contains("ClientCode") && sRow["ClientCode"] != DBNull.Value ? sRow["ClientCode"].ToString() : "0",
                        ClientName = sRow["ClientName"].ToString(),
                        ItemsCount = sRow.Table.Columns.Contains("ItemsCount") ? Convert.ToInt32(sRow["ItemsCount"]) : 0,
                        BaseAmount = Convert.ToDecimal(sRow["TotalBeforeDiscount"]),
                        DiscountPct = Convert.ToDecimal(sRow["DiscountPct"]),
                        DiscountAmount = Convert.ToDecimal(sRow["DiscountAmount"]),
                        ExtraAmount = sRow.Table.Columns.Contains("ShippingCharge") ? Convert.ToDecimal(sRow["ShippingCharge"]) : 0m,
                        ReturnAmount = sRow.Table.Columns.Contains("ReturnAmount") ? Convert.ToDecimal(sRow["ReturnAmount"]) : 0m,
                        NetAmount = Convert.ToDecimal(sRow["TotalAmount"])
                    };

                    if (itemsGrouped.ContainsKey(sid))
                    {
                        foreach (var iRow in itemsGrouped[sid])
                        {
                            decimal qty = Convert.ToDecimal(iRow["Quantity"]);
                            decimal uPrice = Convert.ToDecimal(iRow["UnitPrice"]);
                            decimal retQty = Convert.ToDecimal(iRow["PrevReturnedQty"]);
                            string uName = iRow["UnitName"].ToString();

                            item.Lines.Add(new DailyInvoiceLineItem
                            {
                                ProductCode = iRow["ProductCode"].ToString(),
                                ProductName = iRow["ProductName"].ToString(),
                                SaleUnit = uName,
                                SaleQty = qty,
                                ReturnUnit = retQty > 0 ? uName : "-",
                                ReturnQty = retQty,
                                UnitPrice = uPrice,
                                TotalSale = Math.Round(qty * uPrice, 2),
                                TotalReturn = Math.Round(retQty * uPrice, 2)
                            });
                        }
                    }
                    result.Add(item);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmDailyInvoicesSheetReport.LoadInvoicesFromDb", ex);
            }
            return result;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  PRINTING ENGINE - MATCHING EXACT PDF LAYOUT
        // ════════════════════════════════════════════════════════════════════════

        public PrintDocument CreatePrintDocument()
        {
            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            pd.DefaultPageSettings.Landscape = false; // A4 Portrait
            pd.DefaultPageSettings.Margins = new Margins(25, 25, 30, 30);

            _printInvoiceIndex = 0;
            _printLineIndex = 0;
            _printPageNum = 1;

            pd.BeginPrint += (s, ev) =>
            {
                _printInvoiceIndex = 0;
                _printLineIndex = 0;
                _printPageNum = 1;
            };

            pd.PrintPage += (s, ev) =>
            {
                RenderReportPage(ev);
            };

            return pd;
        }

        private void RenderReportPage(PrintPageEventArgs ev)
        {
            Graphics g = ev.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int marginL = 25;
            int marginR = ev.PageBounds.Width - 25;
            int pageW = marginR - marginL; // ~777 points
            int pageBottom = ev.PageBounds.Height - 35;
            int y = 30;

            // Fonts
            using (var fTitle = new Font("Arial", 16f, FontStyle.Bold))
            using (var fComp = new Font("Arial", 11.5f, FontStyle.Bold))
            using (var fHead = new Font("Arial", 8.5f, FontStyle.Bold))
            using (var fCell = new Font("Arial", 8f, FontStyle.Regular))
            using (var fCellB = new Font("Arial", 8.5f, FontStyle.Bold))
            using (var fSec = new Font("Arial", 9f, FontStyle.Bold))
            using (var fFoot = new Font("Arial", 8f, FontStyle.Regular))
            using (var penDark = new Pen(Color.FromArgb(100, 116, 139), 1f))
            using (var penLight = new Pen(Color.FromArgb(203, 213, 225), 1f))
            using (var penBorder = new Pen(Color.Black, 1.2f))
            using (var brushHeaderBg = new SolidBrush(Color.FromArgb(226, 232, 240)))
            using (var brushItemsHeadBg = new SolidBrush(Color.FromArgb(238, 242, 246)))
            using (var brushRed = new SolidBrush(Color.FromArgb(185, 28, 28)))
            {
                // 1. Page Header (on every page)
                string docTitle = "فواتير البيع";
                SizeF szTitle = g.MeasureString(docTitle, fTitle);
                g.DrawString(docTitle, fTitle, Brushes.Black, marginL + (pageW - szTitle.Width) / 2f, y);
                y += (int)szTitle.Height + 2;

                string compName = AppConfig.CompanyName ?? "شركه الرحمه جروب";
                SizeF szComp = g.MeasureString(compName, fComp);
                g.DrawString(compName, fComp, Brushes.Black, marginL + (pageW - szComp.Width) / 2f, y);
                y += (int)szComp.Height + 4;

                // Dotted line separator
                using (var penDot = new Pen(Color.FromArgb(160, 174, 192), 1f) { DashStyle = DashStyle.Dot })
                {
                    g.DrawLine(penDot, marginL + 50, y, marginR - 50, y);
                }
                y += 10;

                // StringFormat helpers
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                var sfRtlRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };
                var sfRtlCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionRightToLeft };

                // 2. Render Invoices Loop
                while (_printInvoiceIndex < _filteredInvoices.Count)
                {
                    var inv = _filteredInvoices[_printInvoiceIndex];

                    // Check if we need a new page for starting a new invoice
                    // Minimum space needed: Invoice header (24) + Financial Table (42) + Items Header (34) + 1 item row (18) = 118 points
                    if (_printLineIndex == 0 && (y + 118 > pageBottom - 25))
                    {
                        DrawPageFooter(g, marginL, marginR, pageW, pageBottom, _printPageNum, fFoot);
                        _printPageNum++;
                        ev.HasMorePages = true;
                        return;
                    }

                    // A. Render Invoice Top Strip & Financial Table only once at start of invoice
                    if (_printLineIndex == 0)
                    {
                        int stripH = 24;

                        // Left Pill: [ قطاعى ]
                        int pillW = 85;
                        var pillRect = new Rectangle(marginL + 2, y, pillW, stripH);
                        DrawRoundedRectangle(g, penBorder, pillRect, 6);
                        g.DrawString(inv.PriceTier ?? "قطاعى", fCellB, Brushes.Black, pillRect, sfCenter);

                        // Center Box: [ بتاريخ  dd/MM/yyyy hh:mm tt ]
                        int dateBoxW = 280;
                        int dateBoxX = marginL + pillW + 15;
                        var dateBoxRect = new Rectangle(dateBoxX, y, dateBoxW, stripH);
                        g.DrawRectangle(penBorder, dateBoxRect);
                        g.DrawString($"{inv.SaleDate:dd/MM/yyyy   hh:mmtt}         بتاريخ", fCellB, Brushes.Black, dateBoxRect, sfCenter);

                        // Right Box: [ فاتورة بيع رقم  9971 ]
                        int invBoxW = pageW - (pillW + dateBoxW + 30);
                        int invBoxX = dateBoxX + dateBoxW + 15;
                        var invBoxRect = new Rectangle(invBoxX, y, invBoxW, stripH);
                        g.DrawRectangle(penBorder, invBoxRect);
                        g.DrawString($"{inv.SaleCode}          فاتورة بيع رقم", fCellB, Brushes.Black, invBoxRect, sfCenter);

                        y += stripH + 4;

                        // B. Financial Summary Table
                        // Columns widths from Right to Left:
                        // [ك.عميل:40][اسم العميل:170][النوع:45][ع.أصناف:45][ق.الاساسى:65][خصم%:45][خصم.ق:45][خ.أصناف:45][خ.عميل%:45][م.اضافية:45][ق.المرتجعات:65][ق.الصافى:67]
                        int[] finCols = new int[] { 40, 170, 45, 45, 65, 45, 45, 45, 45, 45, 65, 67 };
                        string[] finHeaders = new string[] { "ك.عميل", "اسم العميل", "النوع", "ع.أصناف", "ق.الاساسى", "خصم%", "خصم.ق", "خ.أصناف", "خ.عميل%", "م.اضافية", "ق.المرتجعات", "ق.الصافى" };

                        int finHeadH = 20;
                        int finDataH = 20;

                        // Draw Financial Table Header
                        int curX = marginR;
                        for (int c = 0; c < finCols.Length; c++)
                        {
                            int cw = finCols[c];
                            curX -= cw;
                            var cellRect = new Rectangle(curX, y, cw, finHeadH);
                            g.FillRectangle(brushHeaderBg, cellRect);
                            g.DrawRectangle(penDark, cellRect);
                            g.DrawString(finHeaders[c], fHead, Brushes.Black, cellRect, sfCenter);
                        }
                        y += finHeadH;

                        // Draw Financial Table Data Row
                        string[] finValues = new string[] {
                            inv.ClientCode,
                            inv.ClientName,
                            inv.SaleType,
                            inv.ItemsCount.ToString(),
                            inv.BaseAmount.ToString("0.00"),
                            inv.DiscountPct > 0 ? inv.DiscountPct.ToString("0.00") : "0.00",
                            inv.DiscountAmount > 0 ? inv.DiscountAmount.ToString("0.00") : "0.00",
                            "0.00",
                            "0.00",
                            inv.ExtraAmount > 0 ? inv.ExtraAmount.ToString("0.00") : "0.00",
                            inv.ReturnAmount > 0 ? inv.ReturnAmount.ToString("0.00") : "0.00",
                            inv.NetAmount.ToString("0.00")
                        };

                        curX = marginR;
                        for (int c = 0; c < finCols.Length; c++)
                        {
                            int cw = finCols[c];
                            curX -= cw;
                            var cellRect = new Rectangle(curX, y, cw, finDataH);
                            g.DrawRectangle(penDark, cellRect);
                            g.DrawString(finValues[c], fCell, Brushes.Black, cellRect, sfCenter);
                        }
                        y += finDataH + 3;

                        // C. Section Header: أصناف الفاتورة
                        var secRect = new Rectangle(marginR - 120, y, 120, 14);
                        g.DrawString("أصناف الفاتورة", fSec, brushRed, secRect, sfRtlRight);
                        y += 15;
                    }
                    else
                    {
                        // Continuation Subheader for multi-page invoices
                        var contRect = new Rectangle(marginR - 250, y, 250, 14);
                        g.DrawString($"تابع أصناف فاتورة رقم ({inv.SaleCode}) - {inv.ClientName}", fSec, brushRed, contRect, sfRtlRight);
                        y += 15;
                    }

                    // D. Items Table Header
                    // Columns from Right to Left:
                    // [ك.الصنف:45][اسم الصنف:260][وحدة بيع:50][ك.البيع:45][وحدة مرتجع:55][ك.المرتجع:45][س.البيع:55][اجمالى البيع:70][اجمالى المرتجع:72]
                    int[] itemCols = new int[] { 45, 260, 50, 45, 55, 45, 55, 70, 72 };
                    string[] itemHeaders = new string[] { "ك.الصنف", "اسم الصنف", "وحدة بيع", "ك.البيع", "وحدة مرتجع", "ك.المرتجع", "س.البيع", "اجمالى البيع", "اجمالى المرتجع" };
                    int itemHeadH = 18;
                    int itemRowH = 17;

                    int curItemX = marginR;
                    for (int c = 0; c < itemCols.Length; c++)
                    {
                        int cw = itemCols[c];
                        curItemX -= cw;
                        var cellRect = new Rectangle(curItemX, y, cw, itemHeadH);
                        g.FillRectangle(brushItemsHeadBg, cellRect);
                        g.DrawRectangle(penDark, cellRect);
                        g.DrawString(itemHeaders[c], fHead, Brushes.Black, cellRect, sfCenter);
                    }
                    y += itemHeadH;

                    // E. Render Item Lines
                    while (_printLineIndex < inv.Lines.Count)
                    {
                        if (y + itemRowH > pageBottom - 25)
                        {
                            DrawPageFooter(g, marginL, marginR, pageW, pageBottom, _printPageNum, fFoot);
                            _printPageNum++;
                            ev.HasMorePages = true;
                            return;
                        }

                        var line = inv.Lines[_printLineIndex];
                        string[] lineValues = new string[] {
                            line.ProductCode,
                            line.ProductName,
                            line.SaleUnit,
                            line.SaleQty.ToString("0.00"),
                            line.ReturnUnit,
                            line.ReturnQty.ToString("0.00"),
                            line.UnitPrice.ToString("0.00"),
                            line.TotalSale.ToString("0.00"),
                            line.TotalReturn.ToString("0.00")
                        };

                        curItemX = marginR;
                        for (int c = 0; c < itemCols.Length; c++)
                        {
                            int cw = itemCols[c];
                            curItemX -= cw;
                            var cellRect = new Rectangle(curItemX, y, cw, itemRowH);
                            g.DrawRectangle(penLight, cellRect);

                            // Product Name right-aligned with padding, others centered
                            if (c == 1)
                            {
                                var textRect = new Rectangle(cellRect.X + 2, cellRect.Y, cellRect.Width - 4, cellRect.Height);
                                g.DrawString(lineValues[c], fCell, Brushes.Black, textRect, sfRtlRight);
                            }
                            else
                            {
                                g.DrawString(lineValues[c], fCell, Brushes.Black, cellRect, sfCenter);
                            }
                        }
                        y += itemRowH;
                        _printLineIndex++;
                    }

                    // Invoice Completed: Reset line index and advance invoice
                    _printLineIndex = 0;
                    _printInvoiceIndex++;

                    // Add bottom spacing or separator between invoices
                    y += 10;
                    using (var penInvSep = new Pen(Color.FromArgb(22, 163, 74), 2f)) // Green separator bar as in PDF!
                    {
                        g.DrawLine(penInvSep, marginL, y, marginR, y);
                    }
                    y += 8;
                }

                // All Invoices finished!
                DrawPageFooter(g, marginL, marginR, pageW, pageBottom, _printPageNum, fFoot);
                ev.HasMorePages = false;
            }
        }

        private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle bounds, int radius)
        {
            using (var path = new GraphicsPath())
            {
                int d = radius * 2;
                path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
                path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
                path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }
        }

        private static void DrawPageFooter(Graphics g, int marginL, int marginR, int pageW, int pageBottom, int pageNum, Font fFoot)
        {
            int footY = pageBottom - 18;
            g.DrawLine(Pens.Black, marginL, footY, marginR, footY);
            footY += 4;

            // Left: توقيت الطباعة
            g.DrawString($"توقيت الطباعة  {DateTime.Now:dd/MM/yyyy}", fFoot, Brushes.Black, marginL, footY);

            // Center: Copyright @ e-Stock / Modern Soft Company
            string copyText = "Copyright @ e-Stock\nModern Soft Company";
            var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(copyText, new Font("Arial", 7f, FontStyle.Italic), Brushes.Black, marginL + pageW / 2f, footY - 1, sfCenter);

            // Right: رقم الصفحة X
            var sfRight = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString($"رقم الصفحة {pageNum}", fFoot, Brushes.Black, marginR, footY, sfRight);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  ACTIONS: PREVIEW, PRINT, PDF, EXCEL
        // ════════════════════════════════════════════════════════════════════════

        public void ShowPrintPreview()
        {
            if (_filteredInvoices.Count == 0)
            {
                MessageBox.Show("لا توجد فواتير لعرضها في الفترة المحددة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pd = CreatePrintDocument();
            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 1150,
                Height = 850,
                StartPosition = FormStartPosition.CenterScreen
            };
            preview.ShowDialog(this);
        }

        public void DirectPrintReport()
        {
            if (_filteredInvoices.Count == 0)
            {
                MessageBox.Show("لا توجد فواتير للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pd = CreatePrintDocument();
            pd.Print();
        }

        public void ExportToPdf()
        {
            if (_filteredInvoices.Count == 0)
            {
                MessageBox.Show("لا توجد فواتير لتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "تصدير تقرير فواتير اليومية إلى PDF";
                sfd.Filter = "ملفات PDF (*.pdf)|*.pdf";
                sfd.FileName = $"فواتير_البيع_{dtpFrom.Value:yyyy_MM_dd}.pdf";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var pd = CreatePrintDocument();
                        string pdfPrinter = null;
                        foreach (string p in PrinterSettings.InstalledPrinters)
                        {
                            if (p.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                pdfPrinter = p;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(pdfPrinter))
                        {
                            pd.PrinterSettings.PrinterName = pdfPrinter;
                            pd.PrinterSettings.PrintToFile = true;
                            pd.PrinterSettings.PrintFileName = sfd.FileName;
                            pd.Print();

                            var res = MessageBox.Show("✅ تم تصدير ملف PDF بنجاح!\n\nهل تريد فتح الملف الآن؟", "تم التصدير", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (res == DialogResult.Yes)
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                            }
                        }
                        else
                        {
                            MessageBox.Show("لم يتم العثور على طابعة Microsoft Print to PDF بالجهاز.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("حدث خطأ أثناء التصدير:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    public class DailyInvoiceReportItem
    {
        public int SaleID { get; set; }
        public string SaleCode { get; set; }
        public DateTime SaleDate { get; set; }
        public string SaleType { get; set; }
        public string PriceTier { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int ItemsCount { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal DiscountPct { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ExtraAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetAmount { get; set; }
        public List<DailyInvoiceLineItem> Lines { get; set; } = new List<DailyInvoiceLineItem>();
    }

    public class DailyInvoiceLineItem
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string SaleUnit { get; set; }
        public decimal SaleQty { get; set; }
        public string ReturnUnit { get; set; }
        public decimal ReturnQty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalSale { get; set; }
        public decimal TotalReturn { get; set; }
    }
}

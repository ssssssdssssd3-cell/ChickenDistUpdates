using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة مراجعة واستيراد مبيعات المندوب من ملف CSV
    /// يتيح للمحاسب مراجعة الفواتير وتصحيح أي أسماء قبل الحفظ الرسمي
    /// </summary>
    public class FrmImportPreview : Form
    {
        // ===== Controls =====
        private Panel pnlHeader;
        private Label lblTitle, lblFileName, lblStats;
        private DataGridView dgInvoices;   // قائمة الفواتير (مجمّعة)
        private DataGridView dgItems;      // بنود الفاتورة المحددة
        private Panel pnlBottom;
        private Button btnImportAll, btnClose;
        private Label lblInvCount, lblTotalCash, lblTotalCredit, lblTotalAll;
        private ComboBox cboDriverFilter;
        private Label lblDriverLbl;

        // ===== Data =====
        private readonly DateTime _importDate;
        private readonly int _driverID;         // من الـ ComboBox في FrmDriverHandover
        private readonly string _driverName;

        // قاموس للمطابقة السريعة
        private Dictionary<string, int> _clientMap;    // اسم صغير → ClientID
        private Dictionary<string, int> _productMap;   // اسم صغير → ProductID
        private Dictionary<int, decimal> _productPrice; // ProductID → SalePrice

        // قائمة الفواتير المستخلصة من CSV (مجمّعة)
        private List<DraftInvoice> _invoices = new List<DraftInvoice>();

        // ===== DTO =====
        private class DraftInvoice
        {
            public long CsvInvoiceID { get; set; }     // رقم الفاتورة الأصلي من CSV
            public string ClientNameCsv { get; set; }  // الاسم كما جاء من CSV
            public int ClientID { get; set; }           // 0 = غير متطابق
            public string ClientNameResolved { get; set; }
            public string PaymentType { get; set; }     // Cash / Credit
            public DateTime SaleDate { get; set; }
            public string Notes { get; set; }
            public List<DraftItem> Items { get; set; } = new List<DraftItem>();
            public decimal Total => Items.Sum(i => i.Qty * i.UnitPrice);
            public bool HasError => ClientID == 0 || Items.Any(i => i.ProductID == 0);
            public bool IsImported { get; set; } = false;
        }

        private class DraftItem
        {
            public string ProductNameCsv { get; set; }
            public int ProductID { get; set; }
            public string ProductNameResolved { get; set; }
            public decimal Qty { get; set; }
            public decimal UnitPrice { get; set; }
        }

        // ===== Constructor =====
        public FrmImportPreview(string csvPath, DateTime importDate, int driverID, string driverName)
        {
            _importDate = importDate;
            _driverID   = driverID;
            _driverName = driverName;

            BuildLookups();
            ParseCsv(csvPath);
            InitUI();
            PopulateGrid();
        }

        // =====================================================================
        // 1. بناء قاموس المطابقة
        // =====================================================================
        private void BuildLookups()
        {
            _clientMap    = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _productMap   = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _productPrice = new Dictionary<int, decimal>();

            var clients = DbHelper.Query("SELECT ClientID, ClientName FROM Clients WHERE IsActive=1");
            foreach (DataRow r in clients.Rows)
            {
                string key = Normalize(r["ClientName"].ToString());
                if (!_clientMap.ContainsKey(key))
                    _clientMap[key] = (int)r["ClientID"];
            }

            var products = DbHelper.Query("SELECT ProductID, ProductName, SalePrice FROM Products WHERE IsActive=1");
            foreach (DataRow r in products.Rows)
            {
                string key = Normalize(r["ProductName"].ToString());
                int pid = (int)r["ProductID"];
                if (!_productMap.ContainsKey(key))
                    _productMap[key] = pid;
                _productPrice[pid] = Convert.ToDecimal(r["SalePrice"]);
            }
        }

        private static string Normalize(string s)
            => (s ?? "").Trim().ToLowerInvariant()
                        .Replace("ة", "ه").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا");

        // =====================================================================
        // 2. تحليل CSV
        // =====================================================================
        private void ParseCsv(string path)
        {
            // حقول CSV:
            // رقم_الفاتورة,تاريخ,وقت,اسم_العميل,هاتف_العميل,اسم_المندوب,
            // كود_الصنف,اسم_الصنف,الكمية,سعر_الوحدة,الإجمالي,نوع_الدفع,ملاحظات
            const int COL_INV = 0, COL_DATE = 1, COL_TIME = 2, COL_CLIENT = 3;
            const int COL_PROD_ID = 6, COL_PROD_NAME = 7, COL_QTY = 8;
            const int COL_PRICE = 9, COL_PAYTYPE = 11, COL_NOTES = 12;

            var invDict = new Dictionary<long, DraftInvoice>();

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            bool firstLine = true;
            foreach (string rawLine in lines)
            {
                if (firstLine) { firstLine = false; continue; } // تخطي الهيدر
                string line = rawLine.TrimStart('\uFEFF'); // BOM
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] cols = SplitCsvLine(line);
                if (cols.Length < 12) continue;

                if (!long.TryParse(cols[COL_INV], out long invID)) continue;

                // الفاتورة
                if (!invDict.TryGetValue(invID, out DraftInvoice inv))
                {
                    string clientName = cols[COL_CLIENT].Trim();
                    int clientID = ResolveClient(clientName);
                    string clientResolved = clientID > 0
                        ? GetClientName(clientID)
                        : clientName;

                    DateTime saleDate = _importDate;
                    if (DateTime.TryParse(cols[COL_DATE], out DateTime d)) saleDate = d;

                    inv = new DraftInvoice
                    {
                        CsvInvoiceID      = invID,
                        ClientNameCsv     = clientName,
                        ClientID          = clientID,
                        ClientNameResolved= clientResolved,
                        PaymentType       = cols[COL_PAYTYPE].Trim() == "Cash" ? "Cash" : "Credit",
                        SaleDate          = saleDate,
                        Notes             = cols.Length > COL_NOTES ? cols[COL_NOTES].Trim() : ""
                    };
                    invDict[invID] = inv;
                }

                // البند
                string prodName = cols[COL_PROD_NAME].Trim();
                int prodID = 0;
                if (int.TryParse(cols[COL_PROD_ID], out int pidFromCsv) && pidFromCsv > 0
                    && _productPrice.ContainsKey(pidFromCsv))
                    prodID = pidFromCsv;
                else
                    prodID = ResolveProduct(prodName);

                decimal qty = 0, price = 0;
                decimal.TryParse(cols[COL_QTY],   System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out qty);
                decimal.TryParse(cols[COL_PRICE], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out price);

                inv.Items.Add(new DraftItem
                {
                    ProductNameCsv      = prodName,
                    ProductID           = prodID,
                    ProductNameResolved = prodID > 0 ? GetProductName(prodID) : prodName,
                    Qty                 = qty,
                    UnitPrice           = price > 0 ? price : (prodID > 0 ? _productPrice[prodID] : 0)
                });
            }

            _invoices = invDict.Values.ToList();
        }

        private int ResolveClient(string name)
        {
            string key = Normalize(name);
            return _clientMap.TryGetValue(key, out int id) ? id : 0;
        }
        private int ResolveProduct(string name)
        {
            string key = Normalize(name);
            return _productMap.TryGetValue(key, out int id) ? id : 0;
        }
        private string GetClientName(int id)
        {
            var r = DbHelper.Query("SELECT ClientName FROM Clients WHERE ClientID=@id", DbHelper.P("@id", id));
            return r.Rows.Count > 0 ? r.Rows[0]["ClientName"].ToString() : "";
        }
        private string GetProductName(int id)
        {
            var r = DbHelper.Query("SELECT ProductName FROM Products WHERE ProductID=@id", DbHelper.P("@id", id));
            return r.Rows.Count > 0 ? r.Rows[0]["ProductName"].ToString() : "";
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQ = false;
            var cur = new StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') { inQ = !inQ; }
                else if (c == ',' && !inQ) { result.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(c);
            }
            result.Add(cur.ToString());
            return result.ToArray();
        }

        // =====================================================================
        // 3. بناء الواجهة
        // =====================================================================
        private void InitUI()
        {
            this.Text = "مراجعة واستيراد مبيعات المندوب";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== Header =====
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 90,
                BackColor = Theme.BgCard,
                Padding = new Padding(14, 10, 14, 10)
            };
            lblTitle = new Label
            {
                Text = "📥 مراجعة مبيعات المندوب قبل الترحيل",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                AutoSize = true,
                Location = new Point(14, 10)
            };
            lblFileName = new Label
            {
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSub,
                AutoSize = true,
                Location = new Point(14, 40)
            };
            lblStats = new Label
            {
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(14, 62)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblFileName, lblStats });

            // ===== Top invoices grid =====
            var pnlTop = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 10, 4) };

            dgInvoices = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 280,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                EditMode = DataGridViewEditMode.EditOnEnter,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard, ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // أعمدة جدول الفواتير
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColIdx",      HeaderText = "#",         ReadOnly = true, FillWeight = 25 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColStatus",   HeaderText = "الحالة",    ReadOnly = true, FillWeight = 35 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColClient",   HeaderText = "العميل (CSV)",  ReadOnly = true, FillWeight = 90 });

            // عمود اختيار العميل عند عدم التطابق
            var cboClientCol = new DataGridViewComboBoxColumn
            {
                Name = "ColClientFixed", HeaderText = "العميل المطابق",
                FillWeight = 120, DisplayStyleForCurrentCellOnly = true
            };
            dgInvoices.Columns.Add(cboClientCol);

            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDate",     HeaderText = "تاريخ البيع", ReadOnly = true, FillWeight = 60 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPayType",  HeaderText = "الدفع",     ReadOnly = true, FillWeight = 40 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColItems",    HeaderText = "عدد الأصناف", ReadOnly = true, FillWeight = 45 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTotal",    HeaderText = "الإجمالي",  ReadOnly = true, FillWeight = 60 });
            dgInvoices.Columns.Add(new DataGridViewCheckBoxColumn { Name = "ColImport",  HeaderText = "استيراد",   FillWeight = 40 });

            dgInvoices.SelectionChanged += DgInvoices_SelectionChanged;
            dgInvoices.CellValueChanged += DgInvoices_CellValueChanged;
            dgInvoices.CurrentCellDirtyStateChanged += (s, e) =>
            { if (dgInvoices.IsCurrentCellDirty) dgInvoices.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            dgInvoices.DataError += (s, e) => e.Cancel = true;

            // ===== Label separator =====
            var lblItemsTitle = new Label
            {
                Text = "📦 بنود الفاتورة المحددة:",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 4, 4, 0)
            };

            // ===== Bottom items grid =====
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(24, 28, 42),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(24, 28, 42), ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 50, 80), ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "IColStatus",  HeaderText = "حالة",    FillWeight = 30 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "IColProdCsv", HeaderText = "اسم الصنف (CSV)", FillWeight = 120 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "IColProdRes", HeaderText = "الصنف المطابق",   FillWeight = 120 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "IColQty",     HeaderText = "الكمية",   FillWeight = 50 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "IColPrice",   HeaderText = "السعر",    FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "IColTotal",   HeaderText = "الإجمالي", FillWeight = 60 });

            pnlTop.Controls.Add(dgItems);
            pnlTop.Controls.Add(lblItemsTitle);
            pnlTop.Controls.Add(dgInvoices);

            // ===== Footer =====
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Theme.BgCard,
                Padding = new Padding(14, 10, 14, 10)
            };

            var statsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes
            };

            btnImportAll = Theme.MakeButton("✅ ترحيل الفواتير المختارة", Theme.Accent);
            btnImportAll.Size = new Size(200, 40);
            btnImportAll.Margin = new Padding(0, 8, 20, 0);
            btnImportAll.Click += BtnImportAll_Click;

            btnClose = Theme.MakeButton("❌ إغلاق", Color.FromArgb(80, 80, 100));
            btnClose.Size = new Size(120, 40);
            btnClose.Margin = new Padding(0, 8, 10, 0);
            btnClose.Click += (s, e) => this.Close();

            lblInvCount   = MakeStatLabel("الفواتير:", "0");
            lblTotalCash   = MakeStatLabel("نقدي:", "0.00");
            lblTotalCredit = MakeStatLabel("آجل:", "0.00");
            lblTotalAll    = MakeStatLabel("الإجمالي:", "0.00");

            statsFlow.Controls.AddRange(new Control[] {
                btnImportAll, btnClose,
                lblInvCount, lblTotalCash, lblTotalCredit, lblTotalAll });
            pnlBottom.Controls.Add(statsFlow);

            // ===== Assemble =====
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);
            Theme.ApplyFormRTL(this);
        }

        private Label MakeStatLabel(string title, string val)
        {
            return new Label
            {
                Text = $"{title} {val}",
                AutoSize = false,
                Width = 130,
                Height = 40,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(4, 8, 4, 0),
                BackColor = Theme.BgMain,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        // =====================================================================
        // 4. ملء الجدول
        // =====================================================================
        private void PopulateGrid()
        {
            // ملء ComboBox العملاء في العمود
            var cboCol = (DataGridViewComboBoxColumn)dgInvoices.Columns["ColClientFixed"];
            cboCol.Items.Clear();
            cboCol.Items.Add("-- غير محدد --");
            var allClients = DbHelper.Query("SELECT ClientID, ClientName FROM Clients WHERE IsActive=1 ORDER BY ClientName");
            foreach (DataRow r in allClients.Rows)
                cboCol.Items.Add($"{r["ClientID"]}|{r["ClientName"]}");
            cboCol.ValueMember = null;
            cboCol.DisplayMember = null;

            dgInvoices.Rows.Clear();
            int idx = 1;
            foreach (var inv in _invoices)
            {
                string status = inv.HasError ? "⚠️ يحتاج مراجعة" : "✅ جاهز";
                string clientFixed = inv.ClientID > 0
                    ? $"{inv.ClientID}|{inv.ClientNameResolved}"
                    : "-- غير محدد --";

                int rowIdx = dgInvoices.Rows.Add(
                    idx++,
                    status,
                    inv.ClientNameCsv,
                    clientFixed,
                    inv.SaleDate.ToString("dd/MM/yyyy"),
                    inv.PaymentType == "Cash" ? "💵 نقدي" : "📋 آجل",
                    inv.Items.Count,
                    inv.Total.ToString("N2") + " ج",
                    !inv.HasError   // تحديد تلقائي للجاهزين
                );

                // تلوين الصف حسب الحالة
                var row = dgInvoices.Rows[rowIdx];
                if (inv.IsImported)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(30, 80, 40);
                    row.DefaultCellStyle.ForeColor = Color.LightGreen;
                }
                else if (inv.HasError)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(80, 30, 30);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 180, 180);
                }
            }

            UpdateStats();

            // عنوان
            lblFileName.Text = $"المندوب: {_driverName}  |  تاريخ الاستيراد: {_importDate:dd/MM/yyyy}";
            lblStats.Text = $"إجمالي الفواتير: {_invoices.Count}  |  تحتاج مراجعة: {_invoices.Count(i => i.HasError)}  |  جاهزة للترحيل: {_invoices.Count(i => !i.HasError)}";
        }

        // =====================================================================
        // 5. أحداث الجدول
        // =====================================================================
        private void DgInvoices_SelectionChanged(object sender, EventArgs e)
        {
            dgItems.Rows.Clear();
            if (dgInvoices.SelectedRows.Count == 0) return;
            int rowIdx = dgInvoices.SelectedRows[0].Index;
            if (rowIdx < 0 || rowIdx >= _invoices.Count) return;

            var inv = _invoices[rowIdx];
            foreach (var item in inv.Items)
            {
                string itemStatus = item.ProductID > 0 ? "✅" : "⚠️";
                dgItems.Rows.Add(
                    itemStatus,
                    item.ProductNameCsv,
                    item.ProductID > 0 ? item.ProductNameResolved : "❌ غير موجود",
                    item.Qty.ToString("F2"),
                    item.UnitPrice.ToString("N2"),
                    (item.Qty * item.UnitPrice).ToString("N2") + " ج"
                );

                if (item.ProductID == 0)
                    dgItems.Rows[dgItems.RowCount - 1].DefaultCellStyle.BackColor = Color.FromArgb(80, 30, 30);
            }
        }

        private void DgInvoices_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _invoices.Count) return;
            var inv = _invoices[e.RowIndex];

            // تغيير العميل
            if (dgInvoices.Columns[e.ColumnIndex].Name == "ColClientFixed")
            {
                var val = dgInvoices.Rows[e.RowIndex].Cells["ColClientFixed"].Value?.ToString() ?? "";
                if (val.Contains("|"))
                {
                    var parts = val.Split('|');
                    if (int.TryParse(parts[0], out int cid) && cid > 0)
                    {
                        inv.ClientID = cid;
                        inv.ClientNameResolved = parts[1];
                    }
                }
                else
                {
                    inv.ClientID = 0;
                }
                // إعادة تلوين الصف
                RefreshRowStyle(e.RowIndex, inv);
                UpdateStats();
            }
        }

        private void RefreshRowStyle(int rowIdx, DraftInvoice inv)
        {
            var row = dgInvoices.Rows[rowIdx];
            row.Cells["ColStatus"].Value = inv.HasError ? "⚠️ يحتاج مراجعة" : "✅ جاهز";
            if (inv.IsImported)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(30, 80, 40);
                row.DefaultCellStyle.ForeColor = Color.LightGreen;
            }
            else if (inv.HasError)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(80, 30, 30);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 180, 180);
            }
            else
            {
                row.DefaultCellStyle.BackColor = Theme.BgCard;
                row.DefaultCellStyle.ForeColor = Theme.TextMain;
            }
        }

        private void UpdateStats()
        {
            var selected = GetSelectedInvoices();
            decimal cash   = selected.Where(i => i.PaymentType == "Cash")  .Sum(i => i.Total);
            decimal credit = selected.Where(i => i.PaymentType != "Cash")  .Sum(i => i.Total);
            lblInvCount.Text    = $"الفواتير: {selected.Count}";
            lblTotalCash.Text   = $"نقدي: {cash:N2} ج";
            lblTotalCredit.Text = $"آجل: {credit:N2} ج";
            lblTotalAll.Text    = $"الإجمالي: {cash + credit:N2} ج";
        }

        private List<DraftInvoice> GetSelectedInvoices()
        {
            var result = new List<DraftInvoice>();
            for (int i = 0; i < dgInvoices.RowCount; i++)
            {
                var chk = dgInvoices.Rows[i].Cells["ColImport"].Value;
                if (chk is bool b && b && i < _invoices.Count && !_invoices[i].IsImported)
                    result.Add(_invoices[i]);
            }
            return result;
        }

        // =====================================================================
        // 6. ترحيل الفواتير
        // =====================================================================
        private void BtnImportAll_Click(object sender, EventArgs e)
        {
            var toImport = GetSelectedInvoices();
            if (toImport.Count == 0)
            {
                MessageBox.Show("لم تختر أي فاتورة للترحيل.\nتأكد من وضع علامة ✓ في عمود \"استيراد\".",
                    "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var withErrors = toImport.Where(i => i.HasError).ToList();
            if (withErrors.Count > 0)
            {
                var res = MessageBox.Show(
                    $"⚠️ يوجد {withErrors.Count} فاتورة تحتوي على أخطاء (عميل أو صنف غير محدد).\n" +
                    "هل تريد ترحيل الفواتير الصحيحة فقط وتجاهل الأخطاء؟",
                    "فواتير بأخطاء", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res != DialogResult.Yes) return;
                toImport = toImport.Where(i => !i.HasError).ToList();
            }

            if (toImport.Count == 0)
            {
                MessageBox.Show("لا توجد فواتير صحيحة للترحيل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                $"هل تريد ترحيل {toImport.Count} فاتورة بإجمالي {toImport.Sum(i => i.Total):N2} ج؟\n" +
                "هذا الإجراء لا يمكن التراجع عنه.",
                "تأكيد الترحيل", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int success = 0, fail = 0;
            this.Cursor = Cursors.WaitCursor;
            btnImportAll.Enabled = false;

            foreach (var inv in toImport)
            {
                try
                {
                    var items = inv.Items.Where(it => it.ProductID > 0).Select(it => new SaleItemDTO
                    {
                        ProductID = it.ProductID,
                        Quantity  = it.Qty,
                        UnitPrice = it.UnitPrice
                    }).ToList();

                    int saleID = DriverDAL.ImportDriverSaleRow(
                        inv.ClientID, _driverID, inv.PaymentType,
                        inv.SaleDate, inv.Notes, items);

                    if (saleID > 0)
                    {
                        inv.IsImported = true;
                        success++;
                    }
                    else fail++;
                }
                catch (Exception ex)
                {
                    fail++;
                    AppLogger.Error("خطأ في ترحيل فاتورة مندوب: " + ex.Message, ex, "FrmImportPreview");
                }
            }

            this.Cursor = Cursors.Default;
            btnImportAll.Enabled = true;

            // تحديث الجدول
            PopulateGrid();

            MessageBox.Show(
                $"✅ تم ترحيل {success} فاتورة بنجاح!\n" +
                (fail > 0 ? $"❌ فشل ترحيل {fail} فاتورة (راجع السجلات)." : ""),
                "نتيجة الترحيل", MessageBoxButtons.OK,
                success > 0 && fail == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }
}

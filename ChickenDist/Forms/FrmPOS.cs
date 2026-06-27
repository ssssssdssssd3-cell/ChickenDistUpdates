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
    /// <summary>
    /// شاشة نقطة البيع السريعة — مصممة للسوبر ماركت والكاشير
    /// </summary>
    public class FrmPOS : Form
    {
        // ── عناصر الواجهة ─────────────────────────────────────
        private TextBox txtBarcode;
        private DataGridView dgItems;
        private Label lblTotal, lblPaid, lblChange, lblItemCount, lblClientName, lblClientPoints;
        private Label _lPaid;
        private Button _btnPrint;
        private TextBox txtPaid;
        private Button btnPay, btnNew, btnCancel, btnSearchProduct;
        private ComboBox cboClient;
        private Panel pnlClient;
        private FlowLayoutPanel flowQuickItems;
        private Panel pnlTotals, pnlQuick, pnlTop;
        private CheckBox chkRedeemPoints;

        // ── البيانات ──────────────────────────────────────────
        private List<POSItem> _items = new List<POSItem>();
        private int _lastSaleID = 0;
        private Dictionary<int, decimal> _stockCache = new Dictionary<int, decimal>();

        // Barcode auto-detection
        private System.Windows.Forms.Timer _barcodeTimer;
        private string _barcodeBuffer = "";
        private DateTime _lastKeyTime = DateTime.MinValue;
        private const int BARCODE_INTERVAL_MS = 50;
        private const int BARCODE_MIN_LENGTH = 4;

        public FrmPOS()
        {
            InitUI();
            LoadQuickItems();
            LoadClients();
            LoadStockCache();
            this.Load += (s, e) => { this.ActiveControl = txtBarcode; txtBarcode.Focus(); };
        }

        private void InitUI()
        {
            this.Text = "🛒 نقطة البيع السريعة - POS";
            this.Size = new Size(1100, 750);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.KeyPreview = true;
            this.KeyDown += FrmPOS_KeyDown;

            // ── الشريط العلوي ─────────────────────────────────
            pnlTop = new Panel { Dock = DockStyle.Top, Height = 75, BackColor = Theme.BgHeader };
            var lblTitle = new Label { Text = "🛒 نقطة البيع السريعة", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Theme.Accent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            
            txtBarcode = new TextBox
            {
                Location = new Point(20, 35), Size = new Size(300, 32),
                Font = new Font("Segoe UI", 14f), BackColor = Theme.BgInput, ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;

            btnSearchProduct = Theme.MakeButton("🔍", Theme.Primary, new Point(325, 35), new Size(40, 32));
            btnSearchProduct.Click += (s, e) => OpenProductSearch();

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(txtBarcode);
            pnlTop.Controls.Add(btnSearchProduct);
            txtBarcode.BringToFront();
            btnSearchProduct.BringToFront();
            this.Controls.Add(pnlTop);

            // ── جدول الأصناف (يسار) ──────────────────────────
            dgItems = new DataGridView
            {
                Location = new Point(10, 85), Size = new Size(640, 400),
                BackgroundColor = Color.White, ForeColor = Theme.TextMain,
                AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) },
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Accent, SelectionForeColor = Color.White },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(240, 242, 245), ForeColor = Theme.TextMain, SelectionBackColor = Theme.Accent, SelectionForeColor = Color.White },
                GridColor = Color.FromArgb(210, 210, 215), BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.Single
            };
            dgItems.Columns.Add("Code", "الكود");
            dgItems.Columns.Add("Name", "الصنف");
            dgItems.Columns.Add("Qty", "الكمية");
            dgItems.Columns.Add("Price", "السعر");
            dgItems.Columns.Add("Total", "الإجمالي");
            dgItems.Columns["Code"].Width = 80;
            dgItems.Columns["Qty"].Width = 60;
            dgItems.Columns["Price"].Width = 80;
            dgItems.Columns["Total"].Width = 90;
            dgItems.CellEndEdit += DgItems_CellEndEdit;
            dgItems.KeyDown += DgItems_KeyDown;
            this.Controls.Add(dgItems);

            // ── لوحة العميل ───────────────────────────────────
            pnlClient = new Panel { Location = new Point(660, 85), Size = new Size(420, 55), BackColor = Theme.BgCard };
            var lClient = new Label { Text = "العميل:", Location = new Point(5, 5), Size = new Size(60, 25), ForeColor = Theme.TextMain, Font = Theme.FontMain };
            cboClient = new ComboBox { Location = new Point(70, 3), Size = new Size(200, 28), DropDownStyle = ComboBoxStyle.DropDown, Font = Theme.FontMain, BackColor = Theme.BgInput };
            cboClient.SelectedIndexChanged += CboClient_Changed;
            lblClientPoints = new Label { Text = "", Location = new Point(280, 5), Size = new Size(130, 25), ForeColor = Theme.Accent, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            chkRedeemPoints = new CheckBox { Text = "استرداد نقاط", Location = new Point(280, 28), Size = new Size(120, 22), ForeColor = Theme.TextMain, Font = Theme.FontMain, Checked = false };
            pnlClient.Controls.Add(lClient);
            pnlClient.Controls.Add(cboClient);
            pnlClient.Controls.Add(lblClientPoints);
            pnlClient.Controls.Add(chkRedeemPoints);
            this.Controls.Add(pnlClient);

            // ── لوحة الأصناف السريعة (يمين) ──────────────────
            pnlQuick = new Panel { Location = new Point(660, 150), Size = new Size(420, 335), BackColor = Theme.BgCard, Padding = new Padding(4) };
            pnlQuick.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlQuick);
            var lQuick = new Label { Text = "⚡ أصناف سريعة", Dock = DockStyle.Top, Height = 28, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            flowQuickItems = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent, FlowDirection = FlowDirection.RightToLeft, RightToLeft = RightToLeft.Yes };
            pnlQuick.Controls.Add(flowQuickItems);
            pnlQuick.Controls.Add(lQuick);
            this.Controls.Add(pnlQuick);

            // ── لوحة الإجماليات ───────────────────────────────
            pnlTotals = new Panel { Location = new Point(10, 495), Size = new Size(1070, 200), BackColor = Theme.BgCard };
            pnlTotals.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlTotals);

            // ترتيب RTL: الإجمالي (يمين) → المدفوع (وسط) → الباقي (يسار)
            lblTotal     = new Label { Text = "الإجمالي: 0.00 ج",  Location = new Point(700, 45), Size = new Size(340, 40), ForeColor = Theme.Success, Font = new Font("Segoe UI", 20f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
            lblItemCount = new Label { Text = "عدد الأصناف: 0",    Location = new Point(700, 10), Size = new Size(340, 30), ForeColor = Theme.TextSub,  Font = new Font("Segoe UI", 11f),              TextAlign = ContentAlignment.MiddleRight };

            _lPaid = new Label { Text = "المدفوع:", Location = new Point(370, 50), Size = new Size(80, 28), ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 12f) };
            txtPaid = new TextBox { Location = new Point(255, 46), Size = new Size(110, 34), Font = new Font("Segoe UI", 16f, FontStyle.Bold), BackColor = Theme.BgInput, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle, Text = "0", TextAlign = HorizontalAlignment.Center };
            txtPaid.TextChanged += (s, e) => RecalcChange();

            lblChange = new Label { Text = "الباقي: 0.00 ج", Location = new Point(20, 45), Size = new Size(230, 40), ForeColor = Theme.Accent, Font = new Font("Segoe UI", 20f, FontStyle.Bold) };

            // ── أزرار الأسفل ──────────────────────────────────
            btnPay = Theme.MakeButton("💰 إتمام البيع (F5)", Theme.Success, new Point(20, 130), new Size(250, 55));
            btnPay.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            btnPay.Click += BtnPay_Click;

            btnNew = Theme.MakeButton("🔄 فاتورة جديدة (F2)", Color.FromArgb(60, 70, 85), new Point(280, 130), new Size(210, 55));
            btnNew.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnNew.Click += (s, e) => NewInvoice();

            btnCancel = Theme.MakeButton("❌ إلغاء (Esc)", Theme.Danger, new Point(500, 130), new Size(170, 55));
            btnCancel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnCancel.Click += (s, e) => { if (_items.Count > 0 && MessageBox.Show("إلغاء الفاتورة؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes) NewInvoice(); };

            _btnPrint = Theme.MakeButton("🖨️ طباعة آخر (F6)", Theme.Primary, new Point(680, 130), new Size(170, 55));
            _btnPrint.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            _btnPrint.Click += (s, e) => { if (_lastSaleID > 0) PrintReceipt(_lastSaleID); };

            pnlTotals.Controls.Add(lblItemCount);
            pnlTotals.Controls.Add(lblTotal);
            pnlTotals.Controls.Add(_lPaid);
            pnlTotals.Controls.Add(txtPaid);
            pnlTotals.Controls.Add(lblChange);
            pnlTotals.Controls.Add(btnPay);
            pnlTotals.Controls.Add(btnNew);
            pnlTotals.Controls.Add(btnCancel);
            pnlTotals.Controls.Add(_btnPrint);
            this.Controls.Add(pnlTotals);

            this.Resize += (s, e) => LayoutPanels();
            LayoutPanels();
        }

        private void LayoutPanels()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            int rightW = Math.Max(300, (int)(w * 0.38));
            int leftW = w - rightW - 30;

            dgItems.Size = new Size(leftW, h - 290);
            pnlQuick.Location = new Point(leftW + 20, 150);
            pnlQuick.Size = new Size(rightW, h - 360);
            pnlTotals.Location = new Point(10, h - 210);
            pnlTotals.Size = new Size(w - 20, 200);

            if (pnlClient != null) { pnlClient.Location = new Point(leftW + 20, 85); pnlClient.Size = new Size(rightW, 55); }

            // ── توزيع ديناميكي لعناصر لوحة الإجماليات ──────────
            int totW = pnlTotals.Width;
            // الإجمالي: أقصى اليمين
            lblTotal.Location     = new Point(totW - 360, 45);
            lblTotal.Size         = new Size(340, 40);
            lblItemCount.Location = new Point(totW - 360, 10);
            lblItemCount.Size     = new Size(340, 28);
            // المدفوع: الوسط
            int midX = totW / 2;
            if (_lPaid   != null) _lPaid.Location   = new Point(midX + 10, 50);
            if (txtPaid  != null) txtPaid.Location  = new Point(midX - 105, 46);
            // الباقي: أقصى اليسار
            lblChange.Location = new Point(20, 45);
            lblChange.Size     = new Size(Math.Max(100, midX - 130), 40);
            // الأزرار: توزيع من اليمين لليسار
            if (_btnPrint != null) { _btnPrint.Location = new Point(totW - 195, 130); _btnPrint.Size = new Size(175, 55); }
            if (btnCancel != null) { btnCancel.Location = new Point(totW - 380, 130); btnCancel.Size = new Size(175, 55); }
            if (btnNew    != null) { btnNew.Location    = new Point(totW - 600, 130); btnNew.Size    = new Size(210, 55); }
            if (btnPay    != null) { btnPay.Location    = new Point(20, 130);          btnPay.Size    = new Size(Math.Max(150, totW - 640), 55); }
        }

        // ── اختصارات لوحة المفاتيح ───────────────────────────
        private void FrmPOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) { NewInvoice(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { BtnPay_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.F6) { if (_lastSaleID > 0) PrintReceipt(_lastSaleID); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape && _items.Count == 0) { this.Close(); e.Handled = true; }
        }

        // ── مسح الباركود ──────────────────────────────────────
        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string code = txtBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    AddProductByCode(code);
                    txtBarcode.Clear();
                }
            }
        }

        private void AddProductByCode(string code)
        {
            // بحث بالباركود أو الكود
            var dt = DbHelper.Query(@"
                SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice,
                       p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice,
                       p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2Factor,
                       p.InternationalCode, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                FROM Products p
                WHERE p.IsActive = 1 AND (p.ProductCode = @c OR p.InternationalCode = @c OR p.Unit1Barcode = @c OR p.Unit2Barcode = @c)",
                DbHelper.P("@c", code));

            if (dt.Rows.Count == 0)
            {
                // Handle barcode-weight (e.g., prefix 20)
                if (code.Length >= 8 && code.StartsWith(AppConfig.BarcodeScalePrefix))
                {
                    int codeLen = AppConfig.BarcodeScaleItemCodeLength;
                    int weightLen = AppConfig.BarcodeScaleWeightLength;
                    string itemCode = code.Substring(AppConfig.BarcodeScalePrefix.Length, codeLen);
                    string weightStr = code.Substring(AppConfig.BarcodeScalePrefix.Length + codeLen, weightLen);
                    decimal weight = 0;
                    if (decimal.TryParse(weightStr, out weight)) weight /= AppConfig.BarcodeScaleDivideBy;

                    dt = DbHelper.Query(@"
                        SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                               p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, 
                               p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2Factor,
                               p.InternationalCode, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                        FROM Products p 
                        WHERE p.IsActive = 1 AND p.ProductCode = @c", DbHelper.P("@c", itemCode));
                    if (dt.Rows.Count > 0 && weight > 0)
                    {
                        var row2 = dt.Rows[0];
                        int pid2 = Convert.ToInt32(row2["ProductID"]);
                        int? bid2 = null;
                        DateTime? exp2 = null;
                        bool isInt2 = (row2["InternationalCode"] != DBNull.Value && code == row2["InternationalCode"].ToString());
                        if (row2["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row2["HasExpiry"]))
                        {
                            var batches = DbHelper.Query("SELECT BatchID, ExpiryDate FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=1 AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", pid2));
                            if (batches.Rows.Count > 0)
                            {
                                int oldestId = Convert.ToInt32(batches.Rows[0]["BatchID"]);
                                DateTime? oldestExp = batches.Rows[0]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[0]["ExpiryDate"]) : (DateTime?)null;
                                if (isInt2)
                                {
                                    bid2 = oldestId; exp2 = oldestExp;
                                }
                                else if (oldestExp.HasValue)
                                {
                                    if (MessageBox.Show("يوجد تاريخ أقرب سينتهي، هل تريد بيعه أولاً؟", "تنبيه تاريخ الصلاحية", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                                    {
                                        bid2 = oldestId; exp2 = oldestExp;
                                    }
                                }
                            }
                        }
                        AddItemFromRow(row2, weight, null, 1m, 0, bid2, exp2);
                        return;
                    }
                }

                MessageBox.Show("لم يتم العثور على صنف بهذا الكود.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dt.Rows[0];
            int productID = Convert.ToInt32(row["ProductID"]);
            // Check if barcode matches a sub-unit
            string unitName = null;
            decimal factor = 1m;
            decimal price = Convert.ToDecimal(row["SalePrice"]);
            if (row["Unit1Barcode"] != DBNull.Value && code == row["Unit1Barcode"].ToString())
            {
                unitName = row["Unit1Name"]?.ToString();
                if (row["Unit1SalePrice"] != DBNull.Value) price = Convert.ToDecimal(row["Unit1SalePrice"]);
            }
            else if (row["Unit2Barcode"] != DBNull.Value && code == row["Unit2Barcode"].ToString())
            {
                unitName = row["Unit2Name"]?.ToString();
                if (row["Unit2SalePrice"] != DBNull.Value) price = Convert.ToDecimal(row["Unit2SalePrice"]);
                if (row["Unit2Factor"] != DBNull.Value) factor = Convert.ToDecimal(row["Unit2Factor"]);
            }

            int? batchID = null;
            DateTime? expiryDate = null;
            bool isInternational = (row["InternationalCode"] != DBNull.Value && code == row["InternationalCode"].ToString());
            if (row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]))
            {
                var batches = DbHelper.Query("SELECT BatchID, ExpiryDate FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=1 AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", productID));
                if (batches.Rows.Count > 0)
                {
                    int oldestId = Convert.ToInt32(batches.Rows[0]["BatchID"]);
                    DateTime? oldestExp = batches.Rows[0]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[0]["ExpiryDate"]) : (DateTime?)null;
                    if (isInternational)
                    {
                        batchID = oldestId; expiryDate = oldestExp;
                    }
                    else if (oldestExp.HasValue)
                    {
                        if (MessageBox.Show("يوجد تاريخ أقرب سينتهي، هل تريد بيعه أولاً؟", "تنبيه تاريخ الصلاحية", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            batchID = oldestId; expiryDate = oldestExp;
                        }
                    }
                }
            }

            AddItemFromRow(row, 1, unitName, factor, price, batchID, expiryDate);
        }

        private void AddItemFromRow(DataRow row, decimal qty, string unitName, decimal factor, decimal overridePrice = 0, int? batchID = null, DateTime? expiryDate = null)
        {
            if (expiryDate.HasValue && expiryDate.Value < DateTime.Today && !AppConfig.AllowSellExpired)
            {
                MessageBox.Show("❌ عجز: هذا الصنف منتهي الصلاحية ولا يسمح النظام ببيعه حسب الإعدادات الحالية!", "تنبيه الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int productID = Convert.ToInt32(row["ProductID"]);
            string code = row["ProductCode"]?.ToString() ?? "";
            string name = row["ProductName"]?.ToString() ?? "";
            decimal price = overridePrice > 0 ? overridePrice : Convert.ToDecimal(row["SalePrice"]);
            decimal cost = row["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["PurchasePrice"]) : 0;

            // Check if item already in list (same product + unit)
            var existing = _items.Find(i => i.ProductID == productID && i.UnitName == unitName);
            if (existing != null)
            {
                existing.Qty += qty;
                existing.Total = existing.Qty * existing.Price;
                RefreshGrid();
                return;
            }

            _items.Add(new POSItem
            {
                ProductID = productID,
                Code = code,
                Name = name,
                Unit = row["Unit"]?.ToString() ?? "",
                UnitName = unitName,
                Factor = factor,
                Qty = qty,
                Price = price,
                Cost = cost,
                Total = qty * price,
                HasExpiry = row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]),
                DefaultExpiryDays = row["DefaultExpiryDays"] != DBNull.Value ? Convert.ToInt32(row["DefaultExpiryDays"]) : (int?)null,
                BatchID = batchID,
                ExpiryDate = expiryDate
            });
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgItems.Rows.Clear();
            decimal total = 0;
            foreach (var item in _items)
            {
                dgItems.Rows.Add(item.Code, item.Name + (string.IsNullOrEmpty(item.UnitName) ? "" : $" ({item.UnitName})"), item.Qty.ToString("G"), item.Price.ToString("N2"), item.Total.ToString("N2"));
                total += item.Total;
            }
            lblTotal.Text = $"الإجمالي: {total:N2} ج";
            lblItemCount.Text = $"عدد الأصناف: {_items.Count}   |   عدد القطع: {_items.ConvertAll(i => i.Qty).FindAll(q => q > 0).Count}";
            txtPaid.Text = total.ToString("N2");
            RecalcChange();
        }

        private void RecalcChange()
        {
            decimal total = 0;
            foreach (var item in _items) total += item.Total;

            // Loyalty redemption
            decimal loyaltyDiscount = 0;
            if (chkRedeemPoints.Checked && AppConfig.LoyaltyEnabled && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", ci.ID));
                decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                loyaltyDiscount = Math.Min(points * AppConfig.LoyaltyRedemptionRate, total);
                total -= loyaltyDiscount;
            }

            if (!decimal.TryParse(txtPaid.Text.Replace(",", ""), out decimal paid)) paid = 0;
            decimal change = paid - total;
            lblChange.Text = $"الباقي: {change:N2} ج";
            lblChange.ForeColor = change >= 0 ? Theme.Accent : Theme.Danger;
        }

        // ── تعديل الكمية من الجدول ────────────────────────────
        private void DgItems_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2 && e.RowIndex >= 0 && e.RowIndex < _items.Count) // Qty column
            {
                if (decimal.TryParse(dgItems.Rows[e.RowIndex].Cells[2].Value?.ToString(), out decimal newQty) && newQty > 0)
                {
                    _items[e.RowIndex].Qty = newQty;
                    _items[e.RowIndex].Total = newQty * _items[e.RowIndex].Price;
                    RefreshGrid();
                }
            }
        }

        private void DgItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dgItems.CurrentRow != null)
            {
                int idx = dgItems.CurrentRow.Index;
                if (idx >= 0 && idx < _items.Count)
                {
                    _items.RemoveAt(idx);
                    RefreshGrid();
                }
                e.Handled = true;
            }
        }

        // ── إتمام البيع ──────────────────────────────────────
        private void BtnPay_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0) { MessageBox.Show("لا يوجد أصناف في الفاتورة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            decimal total = 0;
            foreach (var item in _items) total += item.Total;

            int clientID = 0;
            if (cboClient.SelectedItem is ComboItem ci) clientID = ci.ID;

            // Loyalty
            decimal loyaltyDiscount = 0;
            decimal pointsToRedeem = 0;
            if (chkRedeemPoints.Checked && AppConfig.LoyaltyEnabled && clientID > 0)
            {
                var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", clientID));
                decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                loyaltyDiscount = Math.Min(points * AppConfig.LoyaltyRedemptionRate, total);
                pointsToRedeem = loyaltyDiscount / AppConfig.LoyaltyRedemptionRate;
                total -= loyaltyDiscount;
            }

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    var nextSaleResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                    string saleCode = nextSaleResult != null ? nextSaleResult.ToString() : "1";
                    int warehouseID = 1;
                    int saleID = DbHelper.ExecuteInsertTrans(trans,
                        @"INSERT INTO Sales (SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,DiscountAmount,DiscountPct,NetAmount,Notes,CreatedBy,IsPosted,WarehouseID,PriceTier,ShiftID,CashPaid,ShippingCharge)
                          VALUES (@sc,GETDATE(),'Cash',@cid,NULL,@tot,@disc,0,@net,'POS',@emp,1,@wid,N'قطاعي',@sid,@paid,0)",
                        DbHelper.P("@sc", saleCode), DbHelper.P("@cid", clientID > 0 ? (object)clientID : DBNull.Value),
                        DbHelper.P("@tot", total + loyaltyDiscount), DbHelper.P("@disc", loyaltyDiscount), DbHelper.P("@net", total),
                        DbHelper.P("@emp", Session.EmpID), DbHelper.P("@wid", warehouseID),
                        DbHelper.P("@sid", Session.CurrentShiftID.HasValue ? (object)Session.CurrentShiftID.Value : DBNull.Value),
                        DbHelper.P("@paid", decimal.TryParse(txtPaid.Text.Replace(",",""), out decimal pd) ? pd : total));

                    if (saleID <= 0) throw new Exception("فشل حفظ الفاتورة.");

                    // 2. Save items + update stock
                    foreach (var item in _items)
                    {
                        DbHelper.ExecuteInsertTrans(trans,
                            @"INSERT INTO SaleItems (SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier,UnitName,Factor,ExpiryDate,BatchID)
                              VALUES (@sid,@pid,@qty,@up,@tp,0,0,N'قطاعي',@un,@f,@exp,@bid)",
                            DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                            DbHelper.P("@qty", item.Qty), DbHelper.P("@up", item.Price), DbHelper.P("@tp", item.Total),
                            DbHelper.P("@un", (object)item.UnitName ?? DBNull.Value),
                            DbHelper.P("@f", item.Factor),
                            DbHelper.P("@exp", item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value : DBNull.Value),
                            DbHelper.P("@bid", item.BatchID.HasValue ? (object)item.BatchID.Value : DBNull.Value));

                        // Deduct from ProductBatches table
                        if (item.BatchID.HasValue)
                        {
                            decimal baseQty = item.Qty * item.Factor;
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                DbHelper.P("@q", baseQty), DbHelper.P("@bid", item.BatchID.Value));
                        }
                        else
                        {
                            var hasExpObj = DbHelper.ScalarTrans(trans, "SELECT HasExpiry FROM Products WHERE ProductID = @pid", DbHelper.P("@pid", item.ProductID));
                            if (hasExpObj != null && hasExpObj != DBNull.Value && Convert.ToBoolean(hasExpObj))
                            {
                                decimal remainingQty = item.Qty * item.Factor;
                                var batchesDt = DbHelper.QueryTrans(trans, 
                                    "SELECT BatchID, Quantity FROM ProductBatches WHERE ProductID = @pid AND WarehouseID = @wid AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC",
                                    DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", warehouseID));
                                foreach (DataRow bRow in batchesDt.Rows)
                                {
                                    int bId = Convert.ToInt32(bRow["BatchID"]);
                                    decimal bQty = Convert.ToDecimal(bRow["Quantity"]);
                                    decimal toDeduct = Math.Min(remainingQty, bQty);
                                    if (toDeduct > 0)
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                            DbHelper.P("@q", toDeduct), DbHelper.P("@bid", bId));
                                        remainingQty -= toDeduct;
                                        if (remainingQty <= 0) break;
                                    }
                                }
                                if (remainingQty > 0)
                                {
                                    var oldestBatchId = DbHelper.ScalarTrans(trans, "SELECT TOP 1 BatchID FROM ProductBatches WHERE ProductID = @pid AND WarehouseID = @wid ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", warehouseID));
                                    if (oldestBatchId != null && oldestBatchId != DBNull.Value)
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                            DbHelper.P("@q", remainingQty), DbHelper.P("@bid", oldestBatchId));
                                    }
                                    else
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "INSERT INTO ProductBatches (ProductID, WarehouseID, Quantity, ExpiryDate) VALUES (@pid, @wid, -@q, @exp)",
                                            DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", warehouseID), DbHelper.P("@q", remainingQty), DbHelper.P("@exp", DateTime.Today.AddDays(30)));
                                    }
                                }
                            }
                        }

                        // Update stock
                        decimal baseQty2 = item.Qty * item.Factor;
                        DbHelper.ExecuteTrans(trans,
                            @"IF EXISTS (SELECT 1 FROM ProductStock WHERE ProductID=@pid AND WarehouseID=@wid)
                              UPDATE ProductStock SET Quantity = Quantity - @q, LastUpdated=GETDATE() WHERE ProductID=@pid AND WarehouseID=@wid
                              ELSE INSERT INTO ProductStock (ProductID,WarehouseID,Quantity) VALUES (@pid,@wid,-@q)",
                            DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", warehouseID), DbHelper.P("@q", baseQty2));
                    }

                    // 3. CashBox entry
                    DbHelper.ExecuteInsertTrans(trans,
                        "INSERT INTO CashBox (TransDate,TransType,Description,AmountIn,AmountOut,RefID,CreatedBy,AccountID) VALUES (GETDATE(),'Sale',@desc,@amt,0,@ref,@emp,1)",
                        DbHelper.P("@desc", $"فاتورة POS #{saleCode}"), DbHelper.P("@amt", total),
                        DbHelper.P("@ref", saleID), DbHelper.P("@emp", Session.EmpID));

                    // 4. Loyalty points
                    if (AppConfig.LoyaltyEnabled && clientID > 0)
                    {
                        // Earn points
                        decimal earnedPoints = Math.Floor((total + loyaltyDiscount) / AppConfig.LoyaltyPointsPerCurrency);
                        if (earnedPoints > 0)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE Clients SET LoyaltyPoints = ISNULL(LoyaltyPoints,0) + @p, TotalPointsEarned = ISNULL(TotalPointsEarned,0) + @p WHERE ClientID=@cid",
                                DbHelper.P("@p", earnedPoints), DbHelper.P("@cid", clientID));
                            DbHelper.ExecuteInsertTrans(trans,
                                "INSERT INTO LoyaltyTransactions (ClientID,TransType,Points,RefSaleID,Notes,CreatedBy) VALUES (@cid,'Earn',@p,@sid,@n,@emp)",
                                DbHelper.P("@cid", clientID), DbHelper.P("@p", earnedPoints),
                                DbHelper.P("@sid", saleID), DbHelper.P("@n", $"كسب {earnedPoints:N0} نقطة من فاتورة POS"),
                                DbHelper.P("@emp", Session.EmpID));
                        }

                        // Redeem points
                        if (pointsToRedeem > 0)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE Clients SET LoyaltyPoints = ISNULL(LoyaltyPoints,0) - @p WHERE ClientID=@cid",
                                DbHelper.P("@p", pointsToRedeem), DbHelper.P("@cid", clientID));
                            DbHelper.ExecuteInsertTrans(trans,
                                "INSERT INTO LoyaltyTransactions (ClientID,TransType,Points,RefSaleID,Notes,CreatedBy) VALUES (@cid,'Redeem',@p,@sid,@n,@emp)",
                                DbHelper.P("@cid", clientID), DbHelper.P("@p", pointsToRedeem),
                                DbHelper.P("@sid", saleID), DbHelper.P("@n", $"استرداد {pointsToRedeem:N0} نقطة = خصم {loyaltyDiscount:N2} ج"),
                                DbHelper.P("@emp", Session.EmpID));
                        }
                    }

                    _lastSaleID = saleID;
                });

                // Auto-print
                PrintReceipt(_lastSaleID);
                NewInvoice();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPOS.BtnPay_Click", ex);
            }
        }

        private void NewInvoice()
        {
            _items.Clear();
            RefreshGrid();
            txtPaid.Text = "0";
            txtBarcode.Clear();
            chkRedeemPoints.Checked = false;
            txtBarcode.Focus();
        }

        // ── طباعة الإيصال ─────────────────────────────────────
        private void PrintReceipt(int saleID)
        {
            try { new FrmPrintSale(saleID, "Receipt", true); }
            catch (Exception ex) { AppLogger.Error("FrmPOS.PrintReceipt", ex); }
        }

        // ── بحث أصناف ────────────────────────────────────────
        private void OpenProductSearch()
        {
            try
            {
                var frm = new FrmProductSearch();
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    var dt = DbHelper.Query(@"
                        SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                               p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, 
                               p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2Factor,
                               COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                        FROM Products p 
                        WHERE p.ProductID = @id", DbHelper.P("@id", frm.SelectedProductID));
                    if (dt.Rows.Count > 0)
                    {
                        var row = dt.Rows[0];
                        decimal factor = 1m;
                        if (!string.IsNullOrEmpty(frm.SelectedUnitName))
                        {
                            if (row["Unit2Name"] != DBNull.Value && frm.SelectedUnitName == row["Unit2Name"].ToString())
                            {
                                if (row["Unit2Factor"] != DBNull.Value) factor = Convert.ToDecimal(row["Unit2Factor"]);
                            }
                            else if (row["Unit1Name"] != DBNull.Value && frm.SelectedUnitName == row["Unit1Name"].ToString())
                            {
                                factor = 1m;
                            }
                        }
                        AddItemFromRow(row, 1, frm.SelectedUnitName, factor, frm.SelectedPrice, frm.SelectedBatchID, frm.SelectedExpiryDate);
                    }
                }
            }
            catch { }
        }

        // ── Quick Items ──────────────────────────────────────
        private void LoadQuickItems()
        {
            flowQuickItems.Controls.Clear();
            var dt = DbHelper.Query("SELECT ProductID, ProductCode, ProductName, SalePrice FROM Products WHERE IsActive=1 AND IsQuickItem=1 ORDER BY ProductName");
            foreach (DataRow row in dt.Rows)
            {
                int pid = Convert.ToInt32(row["ProductID"]);
                string name = row["ProductName"].ToString();
                decimal price = Convert.ToDecimal(row["SalePrice"]);
                var btn = new Button
                {
                    Text = $"{name}\n{price:N2}",
                    Size = new Size(125, 55), FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Primary, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand, Margin = new Padding(3),
                    Tag = pid
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) =>
                {
                    var dtP = DbHelper.Query("SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2Factor, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays FROM Products p WHERE p.ProductID=@id", DbHelper.P("@id", (int)((Button)s).Tag));
                    if (dtP.Rows.Count > 0) AddItemFromRow(dtP.Rows[0], 1, null, 1m);
                };
                flowQuickItems.Controls.Add(btn);
            }
        }

        private void LoadClients()
        {
            cboClient.Items.Clear();
            cboClient.Items.Add(new ComboItem(0, "-- بدون عميل --"));
            var dt = DbHelper.Query("SELECT ClientID, ClientName FROM Clients WHERE IsActive=1 ORDER BY ClientName");
            foreach (DataRow row in dt.Rows) cboClient.Items.Add(new ComboItem(Convert.ToInt32(row["ClientID"]), row["ClientName"].ToString()));
            cboClient.SelectedIndex = 0;
        }

        private void CboClient_Changed(object sender, EventArgs e)
        {
            if (AppConfig.LoyaltyEnabled && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", ci.ID));
                decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                lblClientPoints.Text = $"🎁 {points:N0} نقطة";
            }
            else { lblClientPoints.Text = ""; }
        }

        private void LoadStockCache()
        {
            try
            {
                _stockCache.Clear();
                var dt = DbHelper.Query("SELECT ProductID, SUM(Quantity) AS TotalQty FROM ProductStock GROUP BY ProductID");
                foreach (DataRow row in dt.Rows) _stockCache[Convert.ToInt32(row["ProductID"])] = Convert.ToDecimal(row["TotalQty"]);
            }
            catch { }
        }

        private class POSItem
        {
            public int ProductID; public string Code, Name, Unit, UnitName;
            public decimal Qty, Price, Cost, Total, Factor;
            public bool HasExpiry;
            public int? DefaultExpiryDays;
            public DateTime? ExpiryDate;
            public int? BatchID;
        }

        public class ComboItem
        {
            public int ID; public string Text;
            public ComboItem(int id, string text) { ID = id; Text = text; }
            public override string ToString() => Text;
        }
    }
}
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
        private Button _btnPrint, _btnWhatsApp, btnOpenDrawer;
        private TextBox txtPaid;
        private Button btnPay, btnNew, btnCancel, btnSearchProduct, btnCustomizeCols;
        private ComboBox cboClient;
        private Panel pnlClient;
        private FlowLayoutPanel flowQuickItems;
        private Panel pnlTotals, pnlQuick, pnlTop;
        private CheckBox chkRedeemPoints;

        // ── عناصر واجهة المطعم ─────────────────────────────────
        private FlowLayoutPanel flowCategories;
        private Panel pnlOrderType;
        private RadioButton rbDineIn, rbTakeaway, rbDelivery;
        private Label lblTableNum;
        private TextBox txtTableNum;
        private ComboBox cboDeliveryDriver;
        private Button btnSuspend, btnRecall;
        private int _loadedDraftSaleID = 0;

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
            LoadCategories();
            LoadDeliveryDrivers();
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
            this.WindowState = FormWindowState.Maximized;

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

            btnCustomizeCols = new Button
            {
                Text      = "⚙️ الأعمدة",
                Size      = new Size(95, 32),
                Location  = new Point(375, 35),
                BackColor = Color.FromArgb(55, 65, 81),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnCustomizeCols.FlatAppearance.BorderSize = 0;
            btnCustomizeCols.Click += (s, e) => ShowColumnCustomizer();
            btnCustomizeCols.Visible = Session.CanOrderColumns("POS");
            pnlTop.Controls.Add(btnCustomizeCols);
            btnCustomizeCols.BringToFront();

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
            dgItems.Columns.Add("Code", AppConfig.BusinessType switch
            {
                "Mobiles"   => "كود الموديل",
                "Clothing"  => "كود الموديل",
                "SpareParts" => "رقم القطعة",
                _           => "الكود"
            });
            dgItems.Columns.Add("Name", AppConfig.BusinessType switch
            {
                "Mobiles"   => "الجهاز / الصنف",
                "Clothing"  => "القطعة / الصنف",
                _           => "الصنف"
            });
            dgItems.Columns.Add("Qty", "الكمية");
            dgItems.Columns.Add("Price", "السعر");
            dgItems.Columns.Add("Discount", "الخصم");
            dgItems.Columns.Add("Total", "الإجمالي");
            if (AppConfig.IsRestaurant)
            {
                dgItems.Columns.Add("KitchenNotes", "📝 ملاحظات المطبخ");
                dgItems.Columns["KitchenNotes"].ReadOnly = false;
                dgItems.Columns["KitchenNotes"].Width = 130;
            }
            
            dgItems.Columns["Code"].ReadOnly = true;
            dgItems.Columns["Name"].ReadOnly = true;
            dgItems.Columns["Qty"].ReadOnly = false;
            dgItems.Columns["Price"].ReadOnly = false;
            dgItems.Columns["Discount"].ReadOnly = false;
            dgItems.Columns["Total"].ReadOnly = true;

            dgItems.Columns["Code"].Width = 60;
            dgItems.Columns["Name"].Width = 240;
            dgItems.Columns["Name"].MinimumWidth = 180;
            dgItems.Columns["Name"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgItems.Columns["Qty"].Width = 50;
            dgItems.Columns["Price"].Width = 70;
            dgItems.Columns["Discount"].Width = 55;
            dgItems.Columns["Total"].Width = 80;
            dgItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            dgItems.AllowUserToOrderColumns = Session.CanOrderColumns("POS");
            Session.LoadColumnOrder(dgItems, "POS");
            LoadColumnSettings();

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
            chkRedeemPoints.CheckedChanged += (s, e) => RefreshGrid();
            pnlClient.Controls.Add(lClient);
            pnlClient.Controls.Add(cboClient);
            pnlClient.Controls.Add(lblClientPoints);
            pnlClient.Controls.Add(chkRedeemPoints);
            this.Controls.Add(pnlClient);

            // ── لوحة نوع الطلب (مطاعم فقط) ───────────────────
            if (AppConfig.IsRestaurant)
            {
                pnlOrderType = new Panel
                {
                    BackColor = Color.FromArgb(30, 30, 46),
                    BorderStyle = BorderStyle.None,
                    Padding = new Padding(6)
                };

                rbDineIn = new RadioButton   { Text = "🍽️ صالة",    Checked = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true };
                rbTakeaway = new RadioButton { Text = "🛍️ تيك أواي", ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true };
                rbDelivery = new RadioButton { Text = "🛵 توصيل",   ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true };

                lblTableNum = new Label { Text = "رقم الطاولة:", ForeColor = Color.White, Font = new Font("Segoe UI", 9f), AutoSize = true };
                txtTableNum = new TextBox { Width = 60, Font = new Font("Segoe UI", 10f, FontStyle.Bold), BackColor = Theme.BgInput, ForeColor = Color.Black };

                var lblDriverRest = new Label { Text = "الطيار:", ForeColor = Color.White, Font = new Font("Segoe UI", 9f), AutoSize = true };
                cboDeliveryDriver = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f), BackColor = Theme.BgInput };
                cboDeliveryDriver.Visible = false;
                lblDriverRest.Visible = false;

                var toggleVisibility = new Action(() => {
                    bool isDineIn = rbDineIn.Checked;
                    bool isDelivery = rbDelivery.Checked;
                    lblTableNum.Visible = isDineIn;
                    txtTableNum.Visible = isDineIn;
                    cboDeliveryDriver.Visible = isDelivery;
                    lblDriverRest.Visible = isDelivery;
                });
                rbDineIn.CheckedChanged += (s, e) => toggleVisibility();
                rbTakeaway.CheckedChanged += (s, e) => toggleVisibility();
                rbDelivery.CheckedChanged += (s, e) => toggleVisibility();

                var flowOT = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true, AutoSize = true };
                flowOT.Controls.AddRange(new Control[] { rbDineIn, rbTakeaway, rbDelivery, lblTableNum, txtTableNum, lblDriverRest, cboDeliveryDriver });
                pnlOrderType.Controls.Add(flowOT);
                this.Controls.Add(pnlOrderType);
            }

            // ── لوحة الأصناف السريعة (يمين) ──────────────────
            pnlQuick = new Panel { Location = new Point(660, 150), Size = new Size(420, 335), BackColor = Color.FromArgb(240, 242, 245), Padding = new Padding(4) };
            pnlQuick.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlQuick);

            // شريط أقسام للمطاعم
            flowCategories = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = AppConfig.IsRestaurant ? 36 : 0,
                AutoScroll = false,
                WrapContents = false,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.FromArgb(30, 30, 46),
                Visible = AppConfig.IsRestaurant
            };

            var lQuick = new Label { Text = "⚡ أصناف سريعة", Dock = DockStyle.Top, Height = 28, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            flowQuickItems = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent, FlowDirection = FlowDirection.RightToLeft, RightToLeft = RightToLeft.Yes };
            pnlQuick.Controls.Add(flowQuickItems);
            pnlQuick.Controls.Add(flowCategories);
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

            _btnPrint = Theme.MakeButton("🖨️ طباعة (F6)", Theme.Primary, new Point(680, 130), new Size(110, 55));
            _btnPrint.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            _btnPrint.Click += (s, e) => { if (_lastSaleID > 0) PrintReceipt(_lastSaleID); };

            _btnWhatsApp = Theme.MakeButton("💬 واتساب", Color.FromArgb(37, 211, 102), new Point(795, 130), new Size(95, 55));
            _btnWhatsApp.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            _btnWhatsApp.ForeColor = Color.White;
            _btnWhatsApp.Click += (s, e) => { if (_lastSaleID > 0) SendWhatsAppReceipt(_lastSaleID); };

            btnOpenDrawer = Theme.MakeButton("🔓 فتح الدرج\n(Ctrl+D)", Color.FromArgb(70, 70, 70), new Point(895, 130), new Size(150, 55));
            btnOpenDrawer.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnOpenDrawer.Click += (s, e) => { RawPrinterHelper.OpenCashDrawer(); };

            pnlTotals.Controls.Add(lblItemCount);
            pnlTotals.Controls.Add(lblTotal);
            pnlTotals.Controls.Add(_lPaid);
            pnlTotals.Controls.Add(txtPaid);
            pnlTotals.Controls.Add(lblChange);
            pnlTotals.Controls.Add(btnPay);
            pnlTotals.Controls.Add(btnNew);
            pnlTotals.Controls.Add(btnCancel);
            pnlTotals.Controls.Add(_btnPrint);
            pnlTotals.Controls.Add(_btnWhatsApp);
            pnlTotals.Controls.Add(btnOpenDrawer);

            if (AppConfig.IsRestaurant)
            {
                var btnKitchen = Theme.MakeButton("🍳 بون\nمطبخ", Color.FromArgb(230, 120, 20), new Point(0, 130), new Size(95, 55));
                btnKitchen.Name = "btnKitchenPrint";
                btnKitchen.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                btnKitchen.Click += (s, e) => { if (_lastSaleID > 0) try { new FrmKitchenPrint(_lastSaleID); } catch { } };
                pnlTotals.Controls.Add(btnKitchen);

                btnSuspend = Theme.MakeButton("⏳ تعليق\nالطلب (F3)", Color.FromArgb(230, 126, 34), new Point(0, 130), new Size(130, 55));
                btnSuspend.Name = "btnSuspend";
                btnSuspend.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                btnSuspend.Click += (s, e) => SuspendCurrentOrder();
                pnlTotals.Controls.Add(btnSuspend);

                btnRecall = Theme.MakeButton("📋 الطلبات\nالمعلقة (F4)", Color.FromArgb(52, 152, 219), new Point(0, 130), new Size(140, 55));
                btnRecall.Name = "btnRecall";
                btnRecall.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                btnRecall.Click += (s, e) => RecallDraftSale();
                pnlTotals.Controls.Add(btnRecall);
            }

            this.Controls.Add(pnlTotals);

            this.FormClosing += FrmPOS_FormClosing;
            this.Resize += (s, e) => LayoutPanels();
            LayoutPanels();
        }

        private void LayoutPanels()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            int rightW = Math.Max(300, (int)(w * 0.38));
            int leftW = w - rightW - 30;

            // ضبط مواقع لوحات العميل والأصناف السريعة لتكون على اليمين (X = 10)
            if (pnlClient != null) { pnlClient.Location = new Point(10, 85); pnlClient.Size = new Size(rightW, 55); }

            if (pnlOrderType != null)
            {
                pnlOrderType.Location = new Point(10, 142);
                pnlOrderType.Size = new Size(rightW, 40);
                pnlQuick.Location = new Point(10, 184);
                pnlQuick.Size = new Size(rightW, h - 394);
            }
            else
            {
                pnlQuick.Location = new Point(10, 150);
                pnlQuick.Size = new Size(rightW, h - 360);
            }

            // ضبط موقع جدول الأصناف ليكون على اليسار (X = rightW + 20)
            dgItems.Location = new Point(rightW + 20, 85);
            dgItems.Size = new Size(leftW, h - 290);

            pnlTotals.Location = new Point(10, h - 210);
            pnlTotals.Size = new Size(w - 20, 200);

            // ضبط مواقع عناصر الشريط العلوي لتكون على اليمين (لأن الـ Panel لا تعكس الاتجاه تلقائياً)
            if (txtBarcode != null) txtBarcode.Location = new Point(w - 320, 35);
            if (btnSearchProduct != null) btnSearchProduct.Location = new Point(w - 365, 35);
            if (btnCustomizeCols != null) btnCustomizeCols.Location = new Point(w - 465, 35);

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
            // الأزرار: توزيع ديناميكي من اليمين ليسار لتفادي أي تداخل
            var btnKitchenCtrl = pnlTotals.Controls["btnKitchenPrint"];
            var btnSuspendCtrl = pnlTotals.Controls["btnSuspend"];
            var btnRecallCtrl = pnlTotals.Controls["btnRecall"];

            if (_btnPrint    != null) { _btnPrint.Location    = new Point(totW - 135, 130);             _btnPrint.Size    = new Size(115, 55); }
            if (_btnWhatsApp != null) { _btnWhatsApp.Location = new Point(totW - 250, 130);             _btnWhatsApp.Size = new Size(110, 55); }
            if (btnOpenDrawer!= null) { btnOpenDrawer.Location= new Point(totW - 395, 130);             btnOpenDrawer.Size= new Size(140, 55); }

            int currentX = totW - 395;
            if (btnKitchenCtrl != null)
            {
                currentX -= 95;
                btnKitchenCtrl.Location = new Point(currentX, 130);
                btnKitchenCtrl.Size = new Size(90, 55);
            }
            if (btnSuspendCtrl != null)
            {
                currentX -= 135;
                btnSuspendCtrl.Location = new Point(currentX, 130);
                btnSuspendCtrl.Size = new Size(130, 55);
            }
            if (btnRecallCtrl != null)
            {
                currentX -= 145;
                btnRecallCtrl.Location = new Point(currentX, 130);
                btnRecallCtrl.Size = new Size(140, 55);
            }

            if (btnCancel != null)
            {
                currentX -= 180;
                btnCancel.Location = new Point(currentX, 130);
                btnCancel.Size = new Size(175, 55);
            }
            if (btnNew != null)
            {
                currentX -= 215;
                btnNew.Location = new Point(currentX, 130);
                btnNew.Size = new Size(210, 55);
            }
            if (btnPay != null)
            {
                btnPay.Location = new Point(20, 130);
                btnPay.Size = new Size(Math.Max(150, currentX - 30), 55);
            }
        }

        // ── اختصارات لوحة المفاتيح ───────────────────────────
        private void FrmPOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) { NewInvoice(); e.Handled = true; }
            else if (e.KeyCode == Keys.F3 && AppConfig.IsRestaurant) { SuspendCurrentOrder(); e.Handled = true; }
            else if (e.KeyCode == Keys.F4 && AppConfig.IsRestaurant) { RecallDraftSale(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { BtnPay_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.F6) { if (_lastSaleID > 0) PrintReceipt(_lastSaleID); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape && _items.Count == 0) { this.Close(); e.Handled = true; }
            else if (e.KeyCode == Keys.F12) { txtBarcode.Focus(); txtBarcode.SelectAll(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.D) { RawPrinterHelper.OpenCashDrawer(); e.Handled = true; }
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
                txtBarcode.Focus();
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (dgItems.Rows.Count > 0)
                {
                    dgItems.Focus();
                    if (dgItems.CurrentCell == null)
                    {
                        dgItems.CurrentCell = dgItems.Rows[0].Cells[0];
                    }
                    e.Handled = true;
                }
            }
        }

        private void AddProductByCode(string code)
        {
            // بحث بالباركود أو الكود
            string trimmedC = code.TrimStart('0');
            if (string.IsNullOrEmpty(trimmedC)) trimmedC = "0";
            string paddedC = code;
            if (int.TryParse(code, out int cVal))
            {
                paddedC = cVal.ToString("D8");
            }

            var dt = DbHelper.Query(@"
                SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice,
                       p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice,
                       p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2Factor,
                       p.Unit3Factor, p.DefaultSaleUnit,
                       p.InternationalCode, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                FROM Products p
                WHERE p.IsActive = 1 AND (p.ProductCode = @c OR p.ProductCode = @trimmed OR p.ProductCode = @padded OR p.InternationalCode = @c OR p.Unit1Barcode = @c OR p.Unit2Barcode = @c)",
                DbHelper.P("@c", code), DbHelper.P("@trimmed", trimmedC), DbHelper.P("@padded", paddedC));

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

                    string trimmedItemCode = itemCode.TrimStart('0');
                    if (string.IsNullOrEmpty(trimmedItemCode)) trimmedItemCode = "0";
                    string paddedItemCode = itemCode;
                    if (int.TryParse(itemCode, out int itemCodeVal))
                    {
                        paddedItemCode = itemCodeVal.ToString("D8");
                    }

                    dt = DbHelper.Query(@"
                        SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                               p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, 
                               p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2Factor,
                               p.Unit3Factor, p.DefaultSaleUnit,
                               p.InternationalCode, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays
                        FROM Products p 
                        WHERE p.IsActive = 1 AND (p.ProductCode = @c OR p.ProductCode = @trimmed OR p.ProductCode = @padded)", 
                        DbHelper.P("@c", itemCode), DbHelper.P("@trimmed", trimmedItemCode), DbHelper.P("@padded", paddedItemCode));
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
                                    else
                                    {
                                        if (batches.Rows.Count > 1)
                                        {
                                            bid2 = Convert.ToInt32(batches.Rows[1]["BatchID"]);
                                            exp2 = batches.Rows[1]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[1]["ExpiryDate"]) : (DateTime?)null;
                                        }
                                        else
                                        {
                                            bid2 = oldestId; exp2 = oldestExp;
                                        }
                                    }
                                }
                                else
                                {
                                    bid2 = oldestId; exp2 = oldestExp;
                                }
                            }
                            else
                            {
                                MessageBox.Show("❌ عجز: لا توجد أي تشغيلات (صلاحيات) متوفرة لهذا الصنف في هذا المخزن حالياً!", "عجز الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
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
                        else
                        {
                            if (batches.Rows.Count > 1)
                            {
                                batchID = Convert.ToInt32(batches.Rows[1]["BatchID"]);
                                expiryDate = batches.Rows[1]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[1]["ExpiryDate"]) : (DateTime?)null;
                            }
                            else
                            {
                                batchID = oldestId; expiryDate = oldestExp;
                            }
                        }
                    }
                    else
                    {
                        batchID = oldestId; expiryDate = oldestExp;
                    }
                }
                else
                {
                    MessageBox.Show("❌ عجز: لا توجد أي تشغيلات (صلاحيات) متوفرة لهذا الصنف في هذا المخزن حالياً!", "عجز الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
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

            if (string.IsNullOrEmpty(unitName))
            {
                string defUnit = row.Table.Columns.Contains("DefaultSaleUnit") && row["DefaultSaleUnit"] != DBNull.Value 
                    ? row["DefaultSaleUnit"].ToString() : "";
                if (string.IsNullOrEmpty(defUnit)) defUnit = "الكبرى";

                string u1Name = row.Table.Columns.Contains("Unit1Name") && row["Unit1Name"] != DBNull.Value ? row["Unit1Name"].ToString() : null;
                string u2Name = row.Table.Columns.Contains("Unit2Name") && row["Unit2Name"] != DBNull.Value ? row["Unit2Name"].ToString() : null;
                string baseUnit = row.Table.Columns.Contains("Unit") && row["Unit"] != DBNull.Value ? row["Unit"].ToString() : null;

                if (defUnit == "الوسطى" && !string.IsNullOrEmpty(u2Name))
                {
                    unitName = u2Name;
                    if (row["Unit2SalePrice"] != DBNull.Value) price = Convert.ToDecimal(row["Unit2SalePrice"]);
                    if (row["Unit2Factor"] != DBNull.Value) factor = Convert.ToDecimal(row["Unit2Factor"]);
                }
                else if (defUnit == "الصغرى" && !string.IsNullOrEmpty(u1Name))
                {
                    unitName = u1Name;
                    if (row["Unit1SalePrice"] != DBNull.Value) price = Convert.ToDecimal(row["Unit1SalePrice"]);
                    factor = 1m;
                }
                else // "الكبرى" or default
                {
                    unitName = !string.IsNullOrEmpty(baseUnit) ? baseUnit : u1Name;
                    decimal u2f = row.Table.Columns.Contains("Unit2Factor") && row["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(row["Unit2Factor"]) : 1m;
                    decimal u3f = row.Table.Columns.Contains("Unit3Factor") && row["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(row["Unit3Factor"]) : 1m;
                    factor = u2f * u3f;
                    price = overridePrice > 0 ? overridePrice : Convert.ToDecimal(row["SalePrice"]);
                }
            }

            bool hasExpiry = row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]);

            // Check if item already in list (same product + unit + same batch if hasExpiry)
            var existing = _items.Find(i => i.ProductID == productID && 
                                            i.UnitName == unitName && 
                                            (!hasExpiry || (i.BatchID == batchID && i.ExpiryDate == expiryDate)));

            decimal targetQty = qty;
            if (existing != null)
            {
                targetQty += existing.Qty;
            }

            if (!CheckAvailableStock(productID, batchID, targetQty * factor, out decimal available, out string err))
            {
                MessageBox.Show(err, "تنبيه عجز رصيد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (existing != null)
            {
                existing.Qty = targetQty;
                existing.Total = (existing.Qty * existing.Price) - existing.DiscountAmt;
                RefreshGrid();
                FocusQtyCell(existing);
                return;
            }

            var newItem = new POSItem
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
                Total = (qty * price),
                DiscountAmt = 0,
                HasExpiry = row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]),
                DefaultExpiryDays = row["DefaultExpiryDays"] != DBNull.Value ? Convert.ToInt32(row["DefaultExpiryDays"]) : (int?)null,
                BatchID = batchID,
                ExpiryDate = expiryDate
            };
            _items.Add(newItem);
            RefreshGrid();
            FocusQtyCell(newItem);
        }

        private void FocusQtyCell(POSItem item)
        {
            try
            {
                int rowIndex = _items.IndexOf(item);
                if (rowIndex >= 0 && rowIndex < dgItems.Rows.Count)
                {
                    dgItems.Focus();
                    dgItems.CurrentCell = dgItems.Rows[rowIndex].Cells[2]; // Cell 2 is Qty
                    dgItems.BeginEdit(true);
                }
            }
            catch { }
        }

        private void RefreshGrid()
        {
            dgItems.Rows.Clear();
            decimal total = 0;
            foreach (var item in _items)
            {
                item.Total = (item.Qty * item.Price) - item.DiscountAmt;
                if (AppConfig.IsRestaurant)
                {
                    dgItems.Rows.Add(item.Code, item.Name + (string.IsNullOrEmpty(item.UnitName) ? "" : $" ({item.UnitName})"), item.Qty.ToString("G"), item.Price.ToString("N2"), item.DiscountAmt.ToString("N2"), item.Total.ToString("N2"), item.KitchenNotes);
                }
                else
                {
                    dgItems.Rows.Add(item.Code, item.Name + (string.IsNullOrEmpty(item.UnitName) ? "" : $" ({item.UnitName})"), item.Qty.ToString("G"), item.Price.ToString("N2"), item.DiscountAmt.ToString("N2"), item.Total.ToString("N2"));
                }
                total += item.Total;
            }

            decimal loyaltyDiscount = 0;
            if (chkRedeemPoints != null && chkRedeemPoints.Checked && AppConfig.LoyaltyEnabled && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                var pts = DbHelper.Scalar("SELECT ISNULL(LoyaltyPoints,0) FROM Clients WHERE ClientID=@id", DbHelper.P("@id", ci.ID));
                decimal points = pts != null && pts != DBNull.Value ? Convert.ToDecimal(pts) : 0;
                loyaltyDiscount = Math.Min(points * AppConfig.LoyaltyRedemptionRate, total);
            }

            lblTotal.Text = $"الإجمالي: {(total - loyaltyDiscount):N2} ج";
            lblItemCount.Text = $"عدد الأصناف: {_items.Count}   |   عدد القطع: {_items.ConvertAll(i => i.Qty).FindAll(q => q > 0).Count}";
            txtPaid.Text = (total - loyaltyDiscount).ToString("N2");
            RecalcChange();
        }

        private void RecalcChange()
        {
            decimal total = 0;
            foreach (var item in _items)
            {
                item.Total = (item.Qty * item.Price) - item.DiscountAmt;
                total += item.Total;
            }

            // Loyalty redemption
            decimal loyaltyDiscount = 0;
            if (chkRedeemPoints != null && chkRedeemPoints.Checked && AppConfig.LoyaltyEnabled && cboClient.SelectedItem is ComboItem ci && ci.ID > 0)
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

        private bool CheckAvailableStock(int productID, int? batchID, decimal qtyInFactor, out decimal available, out string errorMessage)
        {
            available = 0;
            errorMessage = "";

            var isServiceObj = DbHelper.Scalar("SELECT IsService FROM Products WHERE ProductID=@pid", DbHelper.P("@pid", productID));
            if (isServiceObj != null && isServiceObj != DBNull.Value && Convert.ToBoolean(isServiceObj))
            {
                return true;
            }

            if (batchID.HasValue)
            {
                var qtyObj = DbHelper.Scalar("SELECT Quantity FROM ProductBatches WHERE BatchID=@bid", DbHelper.P("@bid", batchID.Value));
                available = qtyObj != null && qtyObj != DBNull.Value ? Convert.ToDecimal(qtyObj) : 0m;
                if (qtyInFactor > available)
                {
                    errorMessage = $"❌ عجز: الكمية المطلوبة ({qtyInFactor:G29}) أكبر من الكمية المتاحة في تشغيلية الصلاحية المحددة ({available:G29})!";
                    return false;
                }
            }
            else
            {
                available = InventoryDAL.GetProductStock(productID, 1);
                if (qtyInFactor > available)
                {
                    errorMessage = $"❌ عجز: الكمية المطلوبة ({qtyInFactor:G29}) أكبر من الكمية المتاحة في المخزن حالياً ({available:G29})!";
                    return false;
                }
            }
            return true;
        }

        // ── تعديل الكمية من الجدول ────────────────────────────
        private void DgItems_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var item = _items[e.RowIndex];

            if (e.ColumnIndex == 2) // Qty column
            {
                if (decimal.TryParse(dgItems.Rows[e.RowIndex].Cells[2].Value?.ToString(), out decimal newQty) && newQty > 0)
                {
                    if (!CheckAvailableStock(item.ProductID, item.BatchID, newQty * item.Factor, out decimal available, out string err))
                    {
                        MessageBox.Show(err, "تنبيه عجز رصيد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        dgItems.Rows[e.RowIndex].Cells[2].Value = item.Qty.ToString("G");
                        return;
                    }

                    item.Qty = newQty;
                    item.Total = (newQty * item.Price) - item.DiscountAmt;
                    RefreshGrid();
                }
                else
                {
                    dgItems.Rows[e.RowIndex].Cells[2].Value = item.Qty.ToString("G");
                }
            }
            else if (e.ColumnIndex == 3) // Price column
            {
                if (decimal.TryParse(dgItems.Rows[e.RowIndex].Cells[3].Value?.ToString(), out decimal newPrice) && newPrice >= 0)
                {
                    item.Price = newPrice;
                    item.Total = (item.Qty * newPrice) - item.DiscountAmt;
                    RefreshGrid();
                }
                else
                {
                    dgItems.Rows[e.RowIndex].Cells[3].Value = item.Price.ToString("N2");
                }
            }
            else if (e.ColumnIndex == 4) // Discount column
            {
                if (decimal.TryParse(dgItems.Rows[e.RowIndex].Cells[4].Value?.ToString(), out decimal newDisc) && newDisc >= 0)
                {
                    item.DiscountAmt = newDisc;
                    item.Total = (item.Qty * item.Price) - newDisc;
                    RefreshGrid();
                }
                else
                {
                    dgItems.Rows[e.RowIndex].Cells[4].Value = item.DiscountAmt.ToString("N2");
                }
            }
            else if (AppConfig.IsRestaurant && e.ColumnIndex == 6) // Kitchen Notes column
            {
                item.KitchenNotes = dgItems.Rows[e.RowIndex].Cells[6].Value?.ToString() ?? "";
            }

            // إعادة التركيز وتحديد خانة الباركود تلقائياً
            this.BeginInvoke(new Action(() => {
                txtBarcode.Focus();
                txtBarcode.SelectAll();
            }));
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
            else if (e.KeyCode == Keys.Down)
            {
                if (dgItems.CurrentCell != null && dgItems.CurrentCell.RowIndex == dgItems.Rows.Count - 1)
                {
                    txtBarcode.Focus();
                    txtBarcode.SelectAll();
                    e.Handled = true;
                }
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

            // Extract restaurant fields if active
            string orderType = null;
            string tableNum = null;
            int? selectedDriver = null;
            if (AppConfig.IsRestaurant)
            {
                orderType = rbDineIn.Checked ? "DineIn" : rbDelivery.Checked ? "Delivery" : "Takeaway";
                tableNum = rbDineIn.Checked ? txtTableNum.Text.Trim() : null;
                if (rbDelivery.Checked && cboDeliveryDriver.SelectedItem is ComboItem driverItem && driverItem.ID > 0)
                {
                    selectedDriver = driverItem.ID;
                }
            }

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    if (_loadedDraftSaleID > 0)
                    {
                        DbHelper.ExecuteTrans(trans, "DELETE FROM SaleItems WHERE SaleID=@id", DbHelper.P("@id", _loadedDraftSaleID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM Sales WHERE SaleID=@id AND IsPosted=0", DbHelper.P("@id", _loadedDraftSaleID));
                    }

                    var nextSaleResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                    string saleCode = nextSaleResult != null ? nextSaleResult.ToString() : "1";
                    int warehouseID = 1;

                    decimal sumItemDiscounts = 0;
                    foreach (var item in _items) sumItemDiscounts += item.DiscountAmt;
                    decimal totalDisc = loyaltyDiscount + sumItemDiscounts;

                    int saleID = DbHelper.ExecuteInsertTrans(trans,
                        @"INSERT INTO Sales (SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,DiscountAmount,DiscountPct,Notes,CreatedBy,IsPosted,WarehouseID,PriceTier,ShiftID,CashPaid,ShippingCharge,OrderType,TableNumber)
                          VALUES (@sc,GETDATE(),'Cash',@cid,@did,@tot,@disc,0,'POS',@emp,1,@wid,N'قطاعي',@sid,@paid,0,@ot,@tn)",
                        DbHelper.P("@sc", saleCode), DbHelper.P("@cid", clientID > 0 ? (object)clientID : DBNull.Value),
                        DbHelper.P("@did", selectedDriver.HasValue ? (object)selectedDriver.Value : DBNull.Value),
                        DbHelper.P("@tot", total), DbHelper.P("@disc", totalDisc),
                        DbHelper.P("@emp", Session.EmpID), DbHelper.P("@wid", warehouseID),
                        DbHelper.P("@sid", Session.CurrentShiftID.HasValue ? (object)Session.CurrentShiftID.Value : DBNull.Value),
                        DbHelper.P("@paid", decimal.TryParse(txtPaid.Text.Replace(",",""), out decimal pd) ? pd : total),
                        DbHelper.P("@ot", string.IsNullOrEmpty(orderType) ? DBNull.Value : (object)orderType),
                        DbHelper.P("@tn", string.IsNullOrEmpty(tableNum) ? DBNull.Value : (object)tableNum));

                    if (saleID <= 0) throw new Exception("فشل حفظ الفاتورة.");

                    // 2. Save items + update stock
                    foreach (var item in _items)
                    {
                        DbHelper.ExecuteInsertTrans(trans,
                            @"INSERT INTO SaleItems (SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier,UnitName,Factor,ExpiryDate,BatchID,KitchenNotes)
                              VALUES (@sid,@pid,@qty,@up,@tp,0,@discAmt,N'قطاعي',@un,@f,@exp,@bid,@kn)",
                            DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                            DbHelper.P("@qty", item.Qty), DbHelper.P("@up", item.Price), DbHelper.P("@tp", item.Total),
                            DbHelper.P("@discAmt", item.DiscountAmt),
                            DbHelper.P("@un", (object)item.UnitName ?? DBNull.Value),
                            DbHelper.P("@f", item.Factor),
                            DbHelper.P("@exp", item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value : DBNull.Value),
                            DbHelper.P("@bid", item.BatchID.HasValue ? (object)item.BatchID.Value : DBNull.Value),
                            DbHelper.P("@kn", string.IsNullOrEmpty(item.KitchenNotes) ? DBNull.Value : (object)item.KitchenNotes));

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
                    decimal cashPaidVal = decimal.TryParse(txtPaid.Text.Replace(",", ""), out decimal pdVal) ? pdVal : total;
                    DbHelper.ExecuteInsertTrans(trans,
                        "INSERT INTO CashBox (TransDate,TransType,Notes,AmountIn,AmountOut,RefID,CreatedBy,AccountID) VALUES (GETDATE(),'Sale',@desc,@amt,0,@ref,@emp,1)",
                        DbHelper.P("@desc", $"فاتورة POS #{saleCode}"), DbHelper.P("@amt", cashPaidVal),
                        DbHelper.P("@ref", saleID), DbHelper.P("@emp", Session.EmpID));

                    // Client ledger statement entries
                    if (clientID > 0)
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO ClientTransactions (ClientID, TransDate, TransType, Debit, RefID, Notes, CreatedBy) VALUES (@cid, GETDATE(), 'Sale', @amt, @ref, @notes, @by)",
                            DbHelper.P("@cid", clientID),
                            DbHelper.P("@amt", total),
                            DbHelper.P("@ref", saleID),
                            DbHelper.P("@notes", $"فاتورة POS #{saleCode}"),
                            DbHelper.P("@by", Session.EmpID));

                        if (cashPaidVal > 0)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions (ClientID, TransDate, TransType, Credit, RefID, Notes, CreatedBy) VALUES (@cid, GETDATE(), 'Payment', @amt, @ref, @notes, @by)",
                                DbHelper.P("@cid", clientID),
                                DbHelper.P("@amt", cashPaidVal),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@notes", $"سداد نقدي فاتورة POS #{saleCode}"),
                                DbHelper.P("@by", Session.EmpID));
                        }
                    }

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
                if (AppConfig.IsRestaurant)
                {
                    try { new FrmKitchenPrint(_lastSaleID); } catch { }
                }
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
            _loadedDraftSaleID = 0;
            if (AppConfig.IsRestaurant)
            {
                if (txtTableNum != null) txtTableNum.Clear();
                if (rbDineIn != null) rbDineIn.Checked = true;
                if (cboDeliveryDriver != null && cboDeliveryDriver.Items.Count > 0) cboDeliveryDriver.SelectedIndex = 0;
            }
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
                               p.Unit3Factor, p.DefaultSaleUnit,
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
            FilterQuickItems(null);
        }

        private void FilterQuickItems(int? categoryID)
        {
            flowQuickItems.Controls.Clear();
            string query = "SELECT ProductID, ProductCode, ProductName, SalePrice FROM Products WHERE IsActive=1 AND IsQuickItem=1";
            var pList = new List<System.Data.SqlClient.SqlParameter>();

            if (categoryID.HasValue)
            {
                query += " AND CategoryID=@catId";
                pList.Add(DbHelper.P("@catId", categoryID.Value));
            }

            query += " ORDER BY ProductName";
            DataTable dt = DbHelper.Query(query, pList.ToArray());

            var colors = new Color[] {
                Color.FromArgb(13, 110, 253),  // Royal Blue
                Color.FromArgb(253, 126, 20),  // Vibrant Orange
                Color.FromArgb(25, 135, 84),   // Green
                Color.FromArgb(111, 66, 193),  // Purple
                Color.FromArgb(23, 162, 184),  // Teal
                Color.FromArgb(220, 53, 69)    // Red
            };
            int colorIndex = 0;

            foreach (DataRow row in dt.Rows)
            {
                int pid = Convert.ToInt32(row["ProductID"]);
                string name = row["ProductName"].ToString();
                decimal price = Convert.ToDecimal(row["SalePrice"]);

                Color btnColor = colors[colorIndex++ % colors.Length];
                var btn = new Button
                {
                    Text = $"{name}\n\n{price:N2} ج",
                    Size = new Size(95, 85), FlatStyle = FlatStyle.Flat,
                    BackColor = btnColor, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                    Cursor = Cursors.Hand, Margin = new Padding(4),
                    Tag = pid
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += QuickItemBtn_Click;
                flowQuickItems.Controls.Add(btn);
            }
        }

        private void QuickItemBtn_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            int pid = (int)btn.Tag;
            var dtP = DbHelper.Query("SELECT p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit2Name, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2Factor, p.Unit3Factor, p.DefaultSaleUnit, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays FROM Products p WHERE p.ProductID=@id", DbHelper.P("@id", pid));
            if (dtP.Rows.Count > 0)
            {
                var row = dtP.Rows[0];
                int? bid = null;
                DateTime? exp = null;
                if (row["HasExpiry"] != DBNull.Value && Convert.ToBoolean(row["HasExpiry"]))
                {
                    var batches = DbHelper.Query("SELECT BatchID, ExpiryDate FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=1 AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", Convert.ToInt32(row["ProductID"])));
                    if (batches.Rows.Count > 0)
                    {
                        bid = Convert.ToInt32(batches.Rows[0]["BatchID"]);
                        exp = batches.Rows[0]["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(batches.Rows[0]["ExpiryDate"]) : (DateTime?)null;
                    }
                    else
                    {
                        MessageBox.Show("❌ عجز: لا توجد أي تشغيلات (صلاحيات) متوفرة لهذا الصنف في هذا المخزن حالياً!", "عجز الصلاحية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                AddItemFromRow(row, 1, null, 1m, 0, bid, exp);
            }
        }

        private void LoadCategories()
        {
            if (!AppConfig.IsRestaurant) return;
            flowCategories.Controls.Clear();

            var btnAll = new Button
            {
                Text = "الكل",
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 4, 3, 4)
            };
            btnAll.FlatAppearance.BorderSize = 0;
            btnAll.Click += (s, e) => {
                FilterQuickItems(null);
                HighlightCategoryButton((Button)s);
            };
            flowCategories.Controls.Add(btnAll);

            var dt = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories WHERE IsActive=1 ORDER BY CategoryName");
            foreach (DataRow row in dt.Rows)
            {
                int catId = Convert.ToInt32(row["CategoryID"]);
                string catName = row["CategoryName"].ToString();

                var btnCat = new Button
                {
                    Text = catName,
                    Size = new Size(80, 28),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 70, 85),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3, 4, 3, 4),
                    Tag = catId
                };
                btnCat.FlatAppearance.BorderSize = 0;
                btnCat.Click += (s, e) => {
                    FilterQuickItems((int)((Button)s).Tag);
                    HighlightCategoryButton((Button)s);
                };
                flowCategories.Controls.Add(btnCat);
            }
        }

        private void HighlightCategoryButton(Button selected)
        {
            foreach (Control ctrl in flowCategories.Controls)
            {
                if (ctrl is Button btn)
                {
                    if (btn == selected)
                    {
                        btn.BackColor = Theme.Primary;
                        btn.ForeColor = Color.White;
                    }
                    else
                    {
                        btn.BackColor = Color.FromArgb(60, 70, 85);
                        btn.ForeColor = Color.White;
                    }
                }
            }
        }

        private void LoadDeliveryDrivers()
        {
            if (!AppConfig.IsRestaurant) return;
            cboDeliveryDriver.BeginUpdate();
            cboDeliveryDriver.Items.Clear();

            List<ComboItem> driverItems = new List<ComboItem>();
            driverItems.Add(new ComboItem(0, "-- اختر طيار --"));

            try
            {
                DataTable drivers = EmployeeDAL.GetDrivers();
                foreach (DataRow row in drivers.Rows)
                {
                    driverItems.Add(new ComboItem((int)row["EmpID"], row["EmpName"].ToString()));
                }
            }
            catch { }

            cboDeliveryDriver.Items.AddRange(driverItems.ToArray());
            cboDeliveryDriver.DisplayMember = "Text";
            cboDeliveryDriver.SelectedIndex = 0;
            cboDeliveryDriver.EndUpdate();
        }

        private void LoadClients()
        {
            cboClient.BeginUpdate();
            cboClient.Items.Clear();
            List<ComboItem> clientItems = new List<ComboItem>();
            clientItems.Add(new ComboItem(0, "-- بدون عميل --"));
            var dt = DbHelper.Query("SELECT ClientID, ClientName FROM Clients WHERE IsActive=1 ORDER BY ClientName");
            foreach (DataRow row in dt.Rows) clientItems.Add(new ComboItem(Convert.ToInt32(row["ClientID"]), row["ClientName"].ToString()));
            cboClient.Items.AddRange(clientItems.ToArray());
            cboClient.SelectedIndex = 0;
            cboClient.EndUpdate();
            SetupSearchableCombo(cboClient);
        }

        private void SetupSearchableCombo(ComboBox cbo)
        {
            cbo.AutoCompleteMode = AutoCompleteMode.None;
            cbo.TextUpdate += delegate
            {
                if (cbo.Tag == null)
                {
                    List<ComboItem> list = new List<ComboItem>();
                    foreach (ComboItem item in cbo.Items)
                    {
                        list.Add(item);
                    }
                    cbo.Tag = list;
                }
                List<ComboItem> list2 = (List<ComboItem>)cbo.Tag;
                string text = cbo.Text;
                cbo.BeginUpdate();
                cbo.Items.Clear();
                if (string.IsNullOrWhiteSpace(text))
                {
                    cbo.Items.AddRange(list2.ToArray());
                }
                else
                {
                    List<ComboItem> filtered = new List<ComboItem>();
                    if (list2.Count > 0 && list2[0].ID == 0)
                    {
                        filtered.Add(list2[0]);
                    }
                    int count = 0;
                    foreach (ComboItem item2 in list2)
                    {
                        if (item2.ID == 0) continue;
                        if (item2.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            filtered.Add(item2);
                            count++;
                            if (count >= 100)
                                break;
                        }
                    }
                    cbo.Items.AddRange(filtered.ToArray());
                }
                cbo.EndUpdate();
                cbo.SelectionStart = text.Length;
                cbo.SelectionLength = 0;
                if (!cbo.DroppedDown)
                {
                    cbo.DroppedDown = true;
                    Cursor.Current = Cursors.Default;
                }
            };
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
            RefreshGrid();
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
            public decimal DiscountAmt;
            public bool HasExpiry;
            public int? DefaultExpiryDays;
            public DateTime? ExpiryDate;
            public int? BatchID;
            public string KitchenNotes = "";
        }

        public class ComboItem
        {
            public int ID; public string Text; public string Phone;
            public ComboItem(int id, string text, string phone = "") { ID = id; Text = text; Phone = phone; }
            public override string ToString() => Text;
        }

        private void SendWhatsAppReceipt(int saleID)
        {
            try
            {
                // 1. Query sale details and client phone
                var dtSale = DbHelper.Query(@"
                    SELECT s.SaleCode, s.TotalAmount, s.DiscountAmount, s.CashPaid, s.SaleDate,
                           c.ClientName, c.Phone
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE s.SaleID = @id", DbHelper.P("@id", saleID));

                if (dtSale.Rows.Count == 0) return;

                var row = dtSale.Rows[0];
                string saleCode = row["SaleCode"].ToString();
                decimal total = Convert.ToDecimal(row["TotalAmount"]);
                decimal discount = Convert.ToDecimal(row["DiscountAmount"]);
                decimal paid = Convert.ToDecimal(row["CashPaid"]);
                decimal remaining = total - paid;
                string clientName = row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "عميل نقدي";
                string phone = row["Phone"] != DBNull.Value ? row["Phone"].ToString() : "";

                // Query sale items
                var dtItems = DbHelper.Query(@"
                    SELECT p.ProductName, si.Quantity, si.UnitName, si.UnitPrice, si.TotalPrice
                    FROM SaleItems si
                    JOIN Products p ON si.ProductID = p.ProductID
                    WHERE si.SaleID = @id", DbHelper.P("@id", saleID));

                // 2. If phone is empty, prompt the user to enter it
                if (string.IsNullOrWhiteSpace(phone))
                {
                    string inputVal = "";
                    if (ShowPhoneInputDialog("إرسال عبر واتساب", "يرجى إدخال رقم هاتف العميل:", ref inputVal))
                    {
                        phone = inputVal;
                    }
                    else
                    {
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(phone)) return;

                // Normalize phone number (remove spaces, plus sign, ensure country code)
                phone = phone.Replace(" ", "").Replace("+", "").Trim();
                if (phone.StartsWith("0"))
                {
                    if (phone.Length == 11 && phone.StartsWith("01"))
                    {
                        phone = "2" + phone;
                    }
                    else if (phone.Length == 10 && phone.StartsWith("05"))
                    {
                        phone = "966" + phone.Substring(1);
                    }
                }

                // 3. Format message
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"📄 *فاتورة مبيعات رقم: {saleCode}*");
                sb.AppendLine($"📅 *التاريخ:* {Convert.ToDateTime(row["SaleDate"]):yyyy-MM-dd HH:mm}");
                sb.AppendLine($"👤 *العميل:* {clientName}");
                sb.AppendLine();
                sb.AppendLine("📋 *الأصناف:*");
                
                foreach (DataRow item in dtItems.Rows)
                {
                    string prodName = item["ProductName"].ToString();
                    decimal qty = Convert.ToDecimal(item["Quantity"]);
                    string unit = item["UnitName"] != DBNull.Value ? item["UnitName"].ToString() : "";
                    decimal price = Convert.ToDecimal(item["UnitPrice"]);
                    decimal itemTotal = Convert.ToDecimal(item["TotalPrice"]);
                    sb.AppendLine($"- {prodName} ({qty} {unit} × {price:N2}) = {itemTotal:N2} ج");
                }

                sb.AppendLine();
                sb.AppendLine($"💵 *الإجمالي:* {total:N2} ج");
                if (discount > 0) sb.AppendLine($"🎁 *الخصم:* {discount:N2} ج");
                sb.AppendLine($"💳 *المدفوع:* {paid:N2} ج");
                if (remaining > 0) sb.AppendLine($"⚠️ *المتبقي:* {remaining:N2} ج");
                
                sb.AppendLine();
                sb.AppendLine("شكراً لتعاملكم معنا! 🙏");

                string message = sb.ToString();

                // 4. Open WhatsApp URL
                string url = $"https://api.whatsapp.com/send?phone={phone}&text={Uri.EscapeDataString(message)}";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPOS.SendWhatsAppReceipt", ex);
                MessageBox.Show("فشل فتح واتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool ShowPhoneInputDialog(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "موافق";
            buttonCancel.Text = "إلغاء";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;
            
            form.RightToLeft = RightToLeft.Yes;
            form.RightToLeftLayout = true;
            form.Font = Theme.FontMain;
            form.BackColor = Theme.BgMain;
            label.ForeColor = Theme.TextMain;
            textBox.BackColor = Theme.BgInput;
            textBox.ForeColor = Theme.TextMain;

            var result = form.ShowDialog();
            value = textBox.Text;
            return result == DialogResult.OK;
        }

        private void FrmPOS_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Session.CanOrderColumns("POS"))
            {
                Session.SaveColumnOrder(dgItems, "POS");
            }
        }

        private void ShowColumnCustomizer()
        {
            var dlg = new Form
            {
                Text            = "⚙️ تخصيص أعمدة المبيعات السريعة",
                Size            = new Size(360, 480),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                RightToLeft     = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor       = Color.FromArgb(30, 30, 45),
                Font            = new Font("Segoe UI", 10f)
            };

            var lblHint = new Label
            {
                Text      = "✅ تفعيل/إيقاف الأعمدة  |  ▲▼ لتغيير الترتيب",
                Dock      = DockStyle.Top,
                Height    = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(150, 200, 255),
                Font      = new Font("Segoe UI", 9f)
            };

            var clb = new CheckedListBox
            {
                Dock            = DockStyle.Fill,
                CheckOnClick    = true,
                BackColor       = Color.FromArgb(40, 42, 58),
                ForeColor       = Color.White,
                BorderStyle     = BorderStyle.None,
                Font            = new Font("Segoe UI", 10f),
                RightToLeft     = RightToLeft.Yes
            };

            foreach (DataGridViewColumn col in dgItems.Columns)
            {
                clb.Items.Add(new ColEntry(col.Name, col.HeaderText), col.Visible);
            }

            var btnUp   = new Button { Text = "▲ أعلى",   Width = 90, Height = 30, BackColor = Color.FromArgb(55,65,81), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnDown = new Button { Text = "▼ أسفل",   Width = 90, Height = 30, BackColor = Color.FromArgb(55,65,81), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnUp.FlatAppearance.BorderSize = btnDown.FlatAppearance.BorderSize = 0;

            btnUp.Click += (s, e) =>
            {
                int i = clb.SelectedIndex;
                if (i <= 0) return;
                var item    = clb.Items[i];
                bool chk    = clb.GetItemChecked(i);
                clb.Items.RemoveAt(i);
                clb.Items.Insert(i - 1, item);
                clb.SetItemChecked(i - 1, chk);
                clb.SelectedIndex = i - 1;
            };
            btnDown.Click += (s, e) =>
            {
                int i = clb.SelectedIndex;
                if (i < 0 || i >= clb.Items.Count - 1) return;
                var item    = clb.Items[i];
                bool chk    = clb.GetItemChecked(i);
                clb.Items.RemoveAt(i);
                clb.Items.Insert(i + 1, item);
                clb.SetItemChecked(i + 1, chk);
                clb.SelectedIndex = i + 1;
            };

            var btnOk     = new Button { Text = "✅ حفظ",   Width = 100, Height = 32, BackColor = Color.FromArgb(46,204,113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "❌ إلغاء", Width = 80,  Height = 32, BackColor = Color.FromArgb(200,50,50),  ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnOk.FlatAppearance.BorderSize = btnCancel.FlatAppearance.BorderSize = 0;

            var pnlArrows = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 40,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = Color.Transparent,
                Padding       = new Padding(5, 5, 5, 0)
            };
            pnlArrows.Controls.AddRange(new Control[] { btnDown, btnUp });

            var pnlFooter = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 44,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = Color.Transparent,
                Padding       = new Padding(5, 5, 5, 0)
            };
            pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnOk });

            dlg.Controls.Add(clb);
            dlg.Controls.Add(pnlArrows);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(lblHint);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                int displayIndex = 0;
                var hiddenNames  = new List<string>();
                var orderedNames = new List<string>();

                for (int i = 0; i < clb.Items.Count; i++)
                {
                    if (!(clb.Items[i] is ColEntry ce)) continue;
                    orderedNames.Add(ce.ColName);
                    bool visible = clb.GetItemChecked(i);
                    if (!visible) hiddenNames.Add(ce.ColName);

                    if (dgItems.Columns.Contains(ce.ColName))
                    {
                        dgItems.Columns[ce.ColName].Visible      = visible;
                        dgItems.Columns[ce.ColName].DisplayIndex = displayIndex++;
                    }
                }

                SaveColumnSettings(orderedNames, hiddenNames);
            }
        }

        private void SaveColumnSettings(List<string> ordered = null, List<string> hidden = null)
        {
            try
            {
                if (ordered == null)
                {
                    ordered = new List<string>();
                    hidden = new List<string>();
                    foreach (DataGridViewColumn col in dgItems.Columns)
                    {
                        ordered.Add(col.Name);
                        if (!col.Visible) hidden.Add(col.Name);
                    }
                }
                Core.LicenseManager.WriteIniValue("POSGridColumns", "Order",  string.Join(",", ordered));
                Core.LicenseManager.WriteIniValue("POSGridColumns", "Hidden", string.Join(",", hidden));
            }
            catch { }
        }

        private void LoadColumnSettings()
        {
            try
            {
                string orderVal  = Core.LicenseManager.ReadIniValue("POSGridColumns", "Order",  "");
                string hiddenVal = Core.LicenseManager.ReadIniValue("POSGridColumns", "Hidden", "");

                if (string.IsNullOrWhiteSpace(orderVal)) return;

                var ordered = new List<string>(orderVal.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));
                var hidden  = new List<string>(string.IsNullOrEmpty(hiddenVal) ? new string[0] : hiddenVal.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));

                foreach (DataGridViewColumn col in dgItems.Columns)
                {
                    if (!ordered.Contains(col.Name))
                    {
                        ordered.Add(col.Name);
                    }
                }

                int displayIndex = 0;
                foreach (string colName in ordered)
                {
                    if (!dgItems.Columns.Contains(colName)) continue;
                    dgItems.Columns[colName].Visible      = !hidden.Contains(colName);
                    dgItems.Columns[colName].DisplayIndex = displayIndex++;
                }
            }
            catch { }
        }

        private void SuspendCurrentOrder()
        {
            if (_items.Count == 0) { MessageBox.Show("لا يوجد أصناف في الفاتورة لتعليقها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // Extract restaurant fields if active
            string orderType = null;
            string tableNum = null;
            int? selectedDriver = null;
            if (AppConfig.IsRestaurant)
            {
                orderType = rbDineIn.Checked ? "DineIn" : rbDelivery.Checked ? "Delivery" : "Takeaway";
                tableNum = rbDineIn.Checked ? txtTableNum.Text.Trim() : null;
                if (rbDelivery.Checked && cboDeliveryDriver.SelectedItem is ComboItem driverItem && driverItem.ID > 0)
                {
                    selectedDriver = driverItem.ID;
                }
            }
            int clientID = 0;
            if (cboClient.SelectedItem is ComboItem ci) clientID = ci.ID;

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    // If we are updating an existing draft, we can delete the old one first
                    if (_loadedDraftSaleID > 0)
                    {
                        DbHelper.ExecuteTrans(trans, "DELETE FROM SaleItems WHERE SaleID=@id", DbHelper.P("@id", _loadedDraftSaleID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM Sales WHERE SaleID=@id AND IsPosted=0", DbHelper.P("@id", _loadedDraftSaleID));
                    }

                    var nextSaleResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                    string saleCode = nextSaleResult != null ? nextSaleResult.ToString() : "1";
                    int warehouseID = 1;
                    decimal total = 0;
                    foreach (var item in _items) total += item.Total;

                    // Insert Sales as IsPosted = 0 (Draft)
                    int saleID = DbHelper.ExecuteInsertTrans(trans,
                        @"INSERT INTO Sales (SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,DiscountAmount,DiscountPct,Notes,CreatedBy,IsPosted,WarehouseID,PriceTier,ShiftID,CashPaid,ShippingCharge,OrderType,TableNumber)
                          VALUES (@sc,GETDATE(),'Cash',@cid,@did,@tot,0,0,'POS_DRAFT',@emp,0,@wid,N'قطاعي',@sid,0,0,@ot,@tn)",
                        DbHelper.P("@sc", saleCode), DbHelper.P("@cid", clientID > 0 ? (object)clientID : DBNull.Value),
                        DbHelper.P("@did", selectedDriver.HasValue ? (object)selectedDriver.Value : DBNull.Value),
                        DbHelper.P("@tot", total),
                        DbHelper.P("@emp", Session.EmpID), DbHelper.P("@wid", warehouseID),
                        DbHelper.P("@sid", Session.CurrentShiftID.HasValue ? (object)Session.CurrentShiftID.Value : DBNull.Value),
                        DbHelper.P("@ot", string.IsNullOrEmpty(orderType) ? DBNull.Value : (object)orderType),
                        DbHelper.P("@tn", string.IsNullOrEmpty(tableNum) ? DBNull.Value : (object)tableNum));

                    if (saleID <= 0) throw new Exception("فشل حفظ تعليق الطلب.");

                    foreach (var item in _items)
                    {
                        DbHelper.ExecuteInsertTrans(trans,
                            @"INSERT INTO SaleItems (SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier,UnitName,Factor,ExpiryDate,BatchID,KitchenNotes)
                              VALUES (@sid,@pid,@qty,@up,@tp,0,@discAmt,N'قطاعي',@un,@f,@exp,@bid,@kn)",
                            DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                            DbHelper.P("@qty", item.Qty), DbHelper.P("@up", item.Price), DbHelper.P("@tp", item.Total),
                            DbHelper.P("@discAmt", item.DiscountAmt),
                            DbHelper.P("@un", (object)item.UnitName ?? DBNull.Value),
                            DbHelper.P("@f", item.Factor),
                            DbHelper.P("@exp", item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value : DBNull.Value),
                            DbHelper.P("@bid", item.BatchID.HasValue ? (object)item.BatchID.Value : DBNull.Value),
                            DbHelper.P("@kn", string.IsNullOrEmpty(item.KitchenNotes) ? DBNull.Value : (object)item.KitchenNotes));
                    }
                    _lastSaleID = saleID;
                });

                // Print kitchen boun
                try { new FrmKitchenPrint(_lastSaleID); } catch { }

                MessageBox.Show("تم تعليق الطلب وطباعة البون بنجاح.", "تم التعليق", MessageBoxButtons.OK, MessageBoxIcon.Information);
                NewInvoice();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPOS.SuspendCurrentOrder", ex);
                MessageBox.Show("حدث خطأ أثناء تعليق الطلب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RecallDraftSale()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "📋 الطلبات المعلقة والطاولات النشطة";
                dlg.Size = new Size(760, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.Font = this.Font;
                dlg.BackColor = Theme.BgMain;

                // ── شريط الأزرار السفلي ──────────────────────────── (يُضاف أولاً)
                var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = Theme.BgHeader, Padding = new Padding(10, 12, 10, 10) };
                var btnLoad = Theme.MakeButton("✅ استرجاع الطلب", Theme.Primary, new Point(10, 10), new Size(165, 42));
                var btnDelete = Theme.MakeButton("❌ حذف المعلق", Theme.Danger, new Point(185, 10), new Size(145, 42));
                var btnCancelDraft = Theme.MakeButton("رجوع", Color.FromArgb(70,70,70), new Point(340, 10), new Size(105, 42));
                pnlButtons.Controls.Add(btnLoad);
                pnlButtons.Controls.Add(btnDelete);
                pnlButtons.Controls.Add(btnCancelDraft);
                dlg.Controls.Add(pnlButtons);

                // ── جدول الطلبات المعلقة ─────────────────────────── (يُضاف ثانياً - Fill)
                var dg = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = Color.White,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    Font = new Font("Segoe UI", 11f),
                    GridColor = Color.FromArgb(210, 215, 220),
                    ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                    EnableHeadersVisualStyles = false,
                    ColumnHeadersHeight = 38,
                    RowTemplate = { Height = 34 },
                    ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Theme.Primary,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Padding = new Padding(4)
                    },
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.White,
                        ForeColor = Theme.TextMain,
                        SelectionBackColor = Theme.Accent,
                        SelectionForeColor = Color.White,
                        Padding = new Padding(4),
                        Alignment = DataGridViewContentAlignment.MiddleCenter
                    },
                    AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(245, 247, 250),
                        ForeColor = Theme.TextMain,
                        SelectionBackColor = Theme.Accent,
                        SelectionForeColor = Color.White
                    }
                };
                dlg.Controls.Add(dg);

                // ── شريط البحث العلوي ─────────────────────────────── (يُضاف أخيراً - Top)
                var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.BgCard, Padding = new Padding(10, 10, 10, 6) };
                var txtSearch = new TextBox { Width = 260, Height = 30, Font = new Font("Segoe UI", 11f), BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.BgInput };
                var lblSearch = new Label { Text = "🔍 بحث (طاولة / عميل):", Width = 165, Height = 28, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain };
                var flowSearch = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
                flowSearch.Controls.Add(txtSearch);
                flowSearch.Controls.Add(lblSearch);
                pnlSearch.Controls.Add(flowSearch);
                dlg.Controls.Add(pnlSearch);

                // Fetch Draft Sales
                Action refreshDrafts = () =>
                {
                    DataTable dt = DbHelper.Query(
                        @"SELECT s.SaleID, s.SaleCode AS [رقم الفاتورة], 
                                 s.SaleDate AS [التاريخ والوقت],
                                 CASE s.OrderType 
                                    WHEN 'DineIn' THEN N'🍽️ صالة (' + ISNULL(s.TableNumber,'') + ')'
                                    WHEN 'Delivery' THEN N'🛵 توصيل'
                                    ELSE N'🛍️ تيك أواي'
                                 END AS [نوع الطلب],
                                 ISNULL(s.TableNumber, '') AS [رقم الطاولة],
                                 ISNULL(c.ClientName, N'---') AS [العميل],
                                 s.TotalAmount AS [الإجمالي]
                          FROM Sales s
                          LEFT JOIN Clients c ON s.ClientID = c.ClientID
                          WHERE s.IsPosted = 0 AND (s.Notes = 'POS_DRAFT' OR s.Notes = 'POS')
                          ORDER BY s.SaleDate DESC");
                    
                    dg.DataSource = dt;
                    if (dg.Columns.Contains("SaleID")) dg.Columns["SaleID"].Visible = false;
                    if (dg.Columns.Contains("رقم الطاولة")) dg.Columns["رقم الطاولة"].Visible = false;
                };

                txtSearch.TextChanged += (s, e) =>
                {
                    if (dg.DataSource is DataTable dt)
                    {
                        string val = txtSearch.Text.Trim().Replace("'", "''");
                        dt.DefaultView.RowFilter = string.Format("[نوع الطلب] LIKE '%{0}%' OR [العميل] LIKE '%{0}%' OR [رقم الفاتورة] LIKE '%{0}%'", val);
                    }
                };

                btnLoad.Click += (s, e) =>
                {
                    if (dg.SelectedRows.Count == 0) return;
                    int saleId = Convert.ToInt32(dg.SelectedRows[0].Cells["SaleID"].Value);

                    // Load client info, order type, table num, driver ID
                    DataRow saleRow = DbHelper.Query("SELECT ClientID, OrderType, TableNumber, DriverID FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleId)).Rows[0];
                    
                    int clientId = saleRow["ClientID"] != DBNull.Value ? Convert.ToInt32(saleRow["ClientID"]) : 0;
                    string ot = saleRow["OrderType"]?.ToString();
                    string tn = saleRow["TableNumber"]?.ToString();
                    int driverId = saleRow["DriverID"] != DBNull.Value ? Convert.ToInt32(saleRow["DriverID"]) : 0;

                    // Set client
                    if (cboClient != null)
                    {
                        cboClient.SelectedIndex = 0;
                        for (int i = 0; i < cboClient.Items.Count; i++)
                        {
                            if (cboClient.Items[i] is ComboItem ci && ci.ID == clientId)
                            {
                                cboClient.SelectedIndex = i;
                                break;
                            }
                        }
                    }

                    // Set Order Type and Table Number
                    if (AppConfig.IsRestaurant)
                    {
                        if (ot == "DineIn") { rbDineIn.Checked = true; txtTableNum.Text = tn; }
                        else if (ot == "Delivery") { rbDelivery.Checked = true; }
                        else { rbTakeaway.Checked = true; }

                        if (cboDeliveryDriver != null)
                        {
                            cboDeliveryDriver.SelectedIndex = 0;
                            for (int i = 0; i < cboDeliveryDriver.Items.Count; i++)
                            {
                                if (cboDeliveryDriver.Items[i] is ComboItem di && di.ID == driverId)
                                {
                                    cboDeliveryDriver.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }

                    // Load items
                    var itemsDt = SaleDAL.GetItems(saleId);
                    _items.Clear();
                    foreach (DataRow iRow in itemsDt.Rows)
                    {
                        _items.Add(new POSItem
                        {
                            ProductID = Convert.ToInt32(iRow["ProductID"]),
                            Code = iRow["PartNumber"]?.ToString() ?? iRow["ProductID"].ToString(),
                            Name = iRow["ProductName"].ToString(),
                            UnitName = iRow["UnitName"]?.ToString() ?? "",
                            Factor = Convert.ToDecimal(iRow["Factor"]),
                            Qty = Convert.ToDecimal(iRow["Quantity"]),
                            Price = Convert.ToDecimal(iRow["UnitPrice"]),
                            Total = Convert.ToDecimal(iRow["TotalPrice"]),
                            DiscountAmt = Convert.ToDecimal(iRow["DiscountAmt"]),
                            KitchenNotes = iRow.Table.Columns.Contains("KitchenNotes") && iRow["KitchenNotes"] != DBNull.Value ? iRow["KitchenNotes"].ToString() : ""
                        });
                    }

                    _loadedDraftSaleID = saleId;
                    RefreshGrid();
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                btnDelete.Click += (s, e) =>
                {
                    if (dg.SelectedRows.Count == 0) return;
                    if (MessageBox.Show("هل أنت متأكد من حذف هذا الطلب المعلق نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        int saleId = Convert.ToInt32(dg.SelectedRows[0].Cells["SaleID"].Value);
                        SaleDAL.DeleteDraftSale(saleId);
                        refreshDrafts();
                    }
                };

                btnCancelDraft.Click += (s, e) => dlg.Close();
                dg.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) btnLoad.PerformClick(); };

                refreshDrafts();
                dlg.ShowDialog();
            }
        }

        private class ColEntry
        {
            public string ColName { get; }
            public string HeaderText { get; }
            public ColEntry(string name, string header)
            {
                ColName = name;
                HeaderText = header;
            }
            public override string ToString() => HeaderText;
        }
    }
}
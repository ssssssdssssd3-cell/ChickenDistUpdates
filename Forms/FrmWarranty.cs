using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmWarranty : Form
    {
        private DataGridView dgTickets;
        private TextBox txtSearch;
        private ComboBox cboStatusFilter;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private CheckBox chkFilterDates;
        private Button btnRefresh;

        // Stats labels
        private Label lblValTotal;
        private Label lblValAtShop;
        private Label lblValAtVendor;
        private Label lblValReady;
        private Label lblValDelivered;

        // Action buttons
        private Button btnAdd;
        private Button btnEdit;
        private Button btnSendToVendor;
        private Button btnReceiveFromVendor;
        private Button btnDeliver;
        private Button btnPrintReceipt;
        private Button btnWhatsApp;
        private Button btnDelete;

        public FrmWarranty()
        {
            InitUI();
            LoadTickets();
            UpdateStats();
        }

        private void InitUI()
        {
            this.Text = "🛡️ سجل وتتبع أجهزة الضمان وخدمة ما بعد البيع";
            this.Size = new Size(1220, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmWarranty_KeyDown;

            // ── Top Title Bar ──
            var pnlTitle = Theme.MakeTitleBar("🛡️ سجل وتتبع أجهزة الضمان وخدمات التوكيل", "متابعة استلام أجهزة العملاء تحت الضمان وإرسالها للتوكيلات وتسليمها دون التأثير على المخزون أو الحسابات.");
            this.Controls.Add(pnlTitle);

            // ── Stats Panel (Top Cards) ──
            var pnlStats = new TableLayoutPanel
            {
                Location = new Point(15, 65),
                Size = new Size(1175, 65),
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 5; i++) pnlStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            this.Controls.Add(pnlStats);

            pnlStats.Controls.Add(CreateStatCard("📋 إجمالي الأجهزة", out lblValTotal, Color.FromArgb(70, 80, 95)), 0, 0);
            pnlStats.Controls.Add(CreateStatCard("📥 بالمحل (قيد الإرسال)", out lblValAtShop, Color.FromArgb(217, 119, 6)), 1, 0);
            pnlStats.Controls.Add(CreateStatCard("🚚 عند التوكيل (قيد الفحص)", out lblValAtVendor, Color.FromArgb(14, 116, 144)), 2, 0);
            pnlStats.Controls.Add(CreateStatCard("⏳ جاهز للاستلام", out lblValReady, Color.FromArgb(124, 58, 237)), 3, 0);
            pnlStats.Controls.Add(CreateStatCard("✅ تم التسليم للزبون", out lblValDelivered, Color.FromArgb(16, 185, 129)), 4, 0);

            // ── Search & Filter Bar ──
            var pnlFilter = new Panel
            {
                Location = new Point(15, 138),
                Size = new Size(1175, 45),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlFilter);

            pnlFilter.Controls.Add(new Label { Text = "🔍 بحث سريع:", Location = new Point(10, 12), Width = 85, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtSearch = new TextBox { Location = new Point(100, 9), Width = 230, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtSearch.TextChanged += (s, e) => LoadTickets();
            pnlFilter.Controls.Add(txtSearch);

            pnlFilter.Controls.Add(new Label { Text = "الحالة:", Location = new Point(345, 12), Width = 50, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            cboStatusFilter = new ComboBox
            {
                Location = new Point(400, 9),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "مستلم بالمحل", "مرسل للتوكيل", "مستلم من التوكيل (جاهز)", "تم التسليم للزبون", "مرفوض الضمان" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => LoadTickets();
            pnlFilter.Controls.Add(cboStatusFilter);

            chkFilterDates = new CheckBox { Text = "الفترة من:", Location = new Point(575, 11), Width = 85, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            chkFilterDates.CheckedChanged += (s, e) => { dtpFrom.Enabled = dtpTo.Enabled = chkFilterDates.Checked; LoadTickets(); };
            pnlFilter.Controls.Add(chkFilterDates);

            dtpFrom = new DateTimePicker { Location = new Point(665, 9), Width = 115, Format = DateTimePickerFormat.Short, Font = Theme.FontMain, Enabled = false };
            dtpFrom.Value = DateTime.Today.AddMonths(-1);
            dtpFrom.ValueChanged += (s, e) => { if (chkFilterDates.Checked) LoadTickets(); };
            pnlFilter.Controls.Add(dtpFrom);

            pnlFilter.Controls.Add(new Label { Text = "إلى:", Location = new Point(785, 12), Width = 30, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            dtpTo = new DateTimePicker { Location = new Point(820, 9), Width = 115, Format = DateTimePickerFormat.Short, Font = Theme.FontMain, Enabled = false };
            dtpTo.Value = DateTime.Today;
            dtpTo.ValueChanged += (s, e) => { if (chkFilterDates.Checked) LoadTickets(); };
            pnlFilter.Controls.Add(dtpTo);

            btnRefresh = new Button { Text = "🔄 تحديث", Location = new Point(950, 8), Width = 90, Height = 28, BackColor = Color.FromArgb(70, 80, 95), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRefresh.Click += (s, e) => { LoadTickets(); UpdateStats(); };
            pnlFilter.Controls.Add(btnRefresh);

            // ── Grid Layout ──
            dgTickets = new DataGridView
            {
                Location = new Point(15, 192),
                Size = new Size(980, 475),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None
            };
            Theme.ApplyGridTheme(dgTickets);
            dgTickets.DoubleClick += (s, e) => EditSelectedTicket();
            dgTickets.CellFormatting += DgTickets_CellFormatting;
            this.Controls.Add(dgTickets);

            // ── Actions Sidebar ──
            var pnlActions = new Panel
            {
                Location = new Point(1005, 192),
                Size = new Size(185, 475),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };
            this.Controls.Add(pnlActions);

            int by = 10;
            btnAdd = CreateActionButton("➕ استلام جهاز (F2)", by, Theme.Success);
            btnAdd.Click += (s, e) => AddNewTicket();
            pnlActions.Controls.Add(btnAdd);
            by += 44;

            btnEdit = CreateActionButton("📝 تعديل الكرت (F3)", by, Theme.Accent);
            btnEdit.Click += (s, e) => EditSelectedTicket();
            pnlActions.Controls.Add(btnEdit);
            by += 44;

            btnSendToVendor = CreateActionButton("🚚 تحويل للتوكيل", by, Color.FromArgb(14, 116, 144));
            btnSendToVendor.Click += (s, e) => QuickSendToVendor();
            pnlActions.Controls.Add(btnSendToVendor);
            by += 44;

            btnReceiveFromVendor = CreateActionButton("📥 استلام من التوكيل", by, Color.FromArgb(124, 58, 237));
            btnReceiveFromVendor.Click += (s, e) => QuickReceiveFromVendor();
            pnlActions.Controls.Add(btnReceiveFromVendor);
            by += 44;

            btnDeliver = CreateActionButton("🤝 تسليم للزبون (F4)", by, Color.FromArgb(16, 185, 129));
            btnDeliver.Click += (s, e) => QuickDeliverToCustomer();
            pnlActions.Controls.Add(btnDeliver);
            by += 52;

            btnPrintReceipt = CreateActionButton("🖨️ طباعة إيصال (F6)", by, Color.FromArgb(30, 80, 180));
            btnPrintReceipt.Click += (s, e) => PrintSelectedReceipt();
            pnlActions.Controls.Add(btnPrintReceipt);
            by += 44;

            btnWhatsApp = CreateActionButton("💬 إرسال واتساب (F7)", by, Color.FromArgb(37, 211, 102));
            btnWhatsApp.Click += (s, e) => SendWhatsApp();
            pnlActions.Controls.Add(btnWhatsApp);
            by += 44;

            btnDelete = CreateActionButton("🗑️ حذف السجل (Del)", by, Color.FromArgb(220, 38, 38));
            btnDelete.Click += (s, e) => DeleteSelectedTicket();
            pnlActions.Controls.Add(btnDelete);

            // Context Menu
            var ctx = new ContextMenuStrip { RightToLeft = RightToLeft.Yes };
            ctx.Items.Add("📝 تعديل كرت الضمان", null, (s, e) => EditSelectedTicket());
            ctx.Items.Add("🖨️ طباعة إيصال استلام", null, (s, e) => PrintSelectedReceipt());
            ctx.Items.Add("💬 إرسال إشعار واتساب للزبون", null, (s, e) => SendWhatsApp());
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add("🚚 تحويل إلى التوكيل", null, (s, e) => QuickSendToVendor());
            ctx.Items.Add("📥 استلام من التوكيل (جاهز)", null, (s, e) => QuickReceiveFromVendor());
            ctx.Items.Add("🤝 تم التسليم للزبون", null, (s, e) => QuickDeliverToCustomer());
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add("🗑️ حذف التذكرة", null, (s, e) => DeleteSelectedTicket());
            dgTickets.ContextMenuStrip = ctx;
        }

        private Panel CreateStatCard(string title, out Label valLabel, Color barColor)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(3),
                BorderStyle = BorderStyle.FixedSingle
            };

            var bar = new Panel { Dock = DockStyle.Right, Width = 6, BackColor = barColor };
            pnl.Controls.Add(bar);

            var lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 22, ForeColor = Theme.TextMuted, Font = new Font(Theme.FontMain.FontFamily, 8.5f), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 3, 10, 0) };
            valLabel = new Label { Text = "0", Dock = DockStyle.Fill, ForeColor = Theme.TextMain, Font = new Font(Theme.FontMain.FontFamily, 13f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };

            pnl.Controls.AddRange(new Control[] { valLabel, lblTitle });
            return pnl;
        }

        private Button CreateActionButton(string text, int y, Color bgColor)
        {
            var btn = Theme.MakeButton(text, 10, y, 160, 36, bgColor);
            btn.Font = new Font(Theme.FontMain.FontFamily, 9f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void FrmWarranty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) { AddNewTicket(); e.Handled = true; }
            else if (e.KeyCode == Keys.F3) { EditSelectedTicket(); e.Handled = true; }
            else if (e.KeyCode == Keys.F4) { QuickDeliverToCustomer(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { LoadTickets(); UpdateStats(); e.Handled = true; }
            else if (e.KeyCode == Keys.F6) { PrintSelectedReceipt(); e.Handled = true; }
            else if (e.KeyCode == Keys.F7) { SendWhatsApp(); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete) { DeleteSelectedTicket(); e.Handled = true; }
        }

        private void LoadTickets()
        {
            try
            {
                string s = txtSearch.Text.Trim();
                string st = cboStatusFilter.SelectedItem?.ToString();
                DateTime? from = chkFilterDates.Checked ? (DateTime?)dtpFrom.Value : null;
                DateTime? to = chkFilterDates.Checked ? (DateTime?)dtpTo.Value : null;

                var dt = WarrantyDAL.GetAll(s, st, from, to);
                dgTickets.DataSource = dt;

                // Adjust columns
                if (dgTickets.Columns.Contains("TicketID")) dgTickets.Columns["TicketID"].Visible = false;
                if (dgTickets.Columns.Contains("ClientID")) dgTickets.Columns["ClientID"].Visible = false;
                if (dgTickets.Columns.Contains("ProductID")) dgTickets.Columns["ProductID"].Visible = false;
                if (dgTickets.Columns.Contains("CreatedBy")) dgTickets.Columns["CreatedBy"].Visible = false;
                if (dgTickets.Columns.Contains("CreatedAt")) dgTickets.Columns["CreatedAt"].Visible = false;
                if (dgTickets.Columns.Contains("UpdatedAt")) dgTickets.Columns["UpdatedAt"].Visible = false;
                if (dgTickets.Columns.Contains("CustomerAddress")) dgTickets.Columns["CustomerAddress"].Visible = false;
                if (dgTickets.Columns.Contains("VendorTrackingNo")) dgTickets.Columns["VendorTrackingNo"].Visible = false;

                if (dgTickets.Columns.Contains("TicketCode")) { dgTickets.Columns["TicketCode"].HeaderText = "رقم الإيصال"; dgTickets.Columns["TicketCode"].FillWeight = 55; }
                if (dgTickets.Columns.Contains("ReceiveDate")) { dgTickets.Columns["ReceiveDate"].HeaderText = "تاريخ الاستلام"; dgTickets.Columns["ReceiveDate"].FillWeight = 55; dgTickets.Columns["ReceiveDate"].DefaultCellStyle.Format = "yyyy/MM/dd"; }
                if (dgTickets.Columns.Contains("CustomerName")) { dgTickets.Columns["CustomerName"].HeaderText = "اسم العميل"; dgTickets.Columns["CustomerName"].FillWeight = 85; }
                if (dgTickets.Columns.Contains("CustomerPhone")) { dgTickets.Columns["CustomerPhone"].HeaderText = "الهاتف"; dgTickets.Columns["CustomerPhone"].FillWeight = 65; }
                if (dgTickets.Columns.Contains("ProductName")) { dgTickets.Columns["ProductName"].HeaderText = "الجهاز / الصنف"; dgTickets.Columns["ProductName"].FillWeight = 95; }
                if (dgTickets.Columns.Contains("Brand")) { dgTickets.Columns["Brand"].HeaderText = "الماركة"; dgTickets.Columns["Brand"].FillWeight = 50; }
                if (dgTickets.Columns.Contains("SerialNumber")) { dgTickets.Columns["SerialNumber"].HeaderText = "السيريال / الضمان"; dgTickets.Columns["SerialNumber"].FillWeight = 70; }
                if (dgTickets.Columns.Contains("Status")) { dgTickets.Columns["Status"].HeaderText = "الحالة"; dgTickets.Columns["Status"].FillWeight = 65; }
                if (dgTickets.Columns.Contains("VendorName")) { dgTickets.Columns["VendorName"].HeaderText = "التوكيل / الصيانة"; dgTickets.Columns["VendorName"].FillWeight = 70; }
                if (dgTickets.Columns.Contains("ProblemDescription")) { dgTickets.Columns["ProblemDescription"].HeaderText = "العطل"; dgTickets.Columns["ProblemDescription"].FillWeight = 90; }
                if (dgTickets.Columns.Contains("ResolutionType")) { dgTickets.Columns["ResolutionType"].HeaderText = "النتيجة"; dgTickets.Columns["ResolutionType"].FillWeight = 65; }
                if (dgTickets.Columns.Contains("DeliveredToCustomerDate")) { dgTickets.Columns["DeliveredToCustomerDate"].HeaderText = "تاريخ التسليم"; dgTickets.Columns["DeliveredToCustomerDate"].FillWeight = 55; dgTickets.Columns["DeliveredToCustomerDate"].DefaultCellStyle.Format = "yyyy/MM/dd"; }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmWarranty.LoadTickets", ex);
            }
        }

        private void DgTickets_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgTickets.Rows.Count) return;
            var col = dgTickets.Columns[e.ColumnIndex];
            if (col.Name == "Status" && e.Value != null)
            {
                string st = e.Value.ToString();
                if (st == "مستلم بالمحل")
                {
                    e.CellStyle.BackColor = Color.FromArgb(254, 243, 199);
                    e.CellStyle.ForeColor = Color.FromArgb(146, 64, 14);
                    e.CellStyle.Font = new Font(Theme.FontMain, FontStyle.Bold);
                }
                else if (st == "مرسل للتوكيل")
                {
                    e.CellStyle.BackColor = Color.FromArgb(224, 242, 254);
                    e.CellStyle.ForeColor = Color.FromArgb(7, 89, 133);
                    e.CellStyle.Font = new Font(Theme.FontMain, FontStyle.Bold);
                }
                else if (st.Contains("جاهز") || st.Contains("مستلم من التوكيل"))
                {
                    e.CellStyle.BackColor = Color.FromArgb(243, 232, 255);
                    e.CellStyle.ForeColor = Color.FromArgb(107, 33, 168);
                    e.CellStyle.Font = new Font(Theme.FontMain, FontStyle.Bold);
                }
                else if (st.Contains("تم التسليم"))
                {
                    e.CellStyle.BackColor = Color.FromArgb(220, 252, 231);
                    e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                    e.CellStyle.Font = new Font(Theme.FontMain, FontStyle.Bold);
                }
                else if (st.Contains("مرفوض"))
                {
                    e.CellStyle.BackColor = Color.FromArgb(254, 226, 226);
                    e.CellStyle.ForeColor = Color.FromArgb(153, 27, 27);
                    e.CellStyle.Font = new Font(Theme.FontMain, FontStyle.Bold);
                }
            }
        }

        private void UpdateStats()
        {
            try
            {
                var s = WarrantyDAL.GetStats();
                lblValTotal.Text = s.TotalCount.ToString("N0");
                lblValAtShop.Text = s.AtShopCount.ToString("N0");
                lblValAtVendor.Text = s.AtVendorCount.ToString("N0");
                lblValReady.Text = s.ReadyCount.ToString("N0");
                lblValDelivered.Text = s.DeliveredCount.ToString("N0");
            }
            catch { }
        }

        private int GetSelectedTicketID()
        {
            if (dgTickets.SelectedRows.Count == 0) return 0;
            var r = (dgTickets.SelectedRows[0].DataBoundItem as DataRowView)?.Row;
            if (r == null || !r.Table.Columns.Contains("TicketID")) return 0;
            return Convert.ToInt32(r["TicketID"]);
        }

        private void AddNewTicket()
        {
            using (var card = new FrmWarrantyCard(0))
            {
                if (card.ShowDialog() == DialogResult.OK)
                {
                    LoadTickets();
                    UpdateStats();
                    if (card.ShouldPrintReceipt && card.SavedTicketID > 0)
                    {
                        PrintReceiptByID(card.SavedTicketID);
                    }
                }
            }
        }

        private void EditSelectedTicket()
        {
            int tid = GetSelectedTicketID();
            if (tid <= 0)
            {
                MessageBox.Show("يرجى اختيار تذكرة من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var card = new FrmWarrantyCard(tid))
            {
                if (card.ShowDialog() == DialogResult.OK)
                {
                    LoadTickets();
                    UpdateStats();
                    if (card.ShouldPrintReceipt)
                    {
                        PrintReceiptByID(tid);
                    }
                }
            }
        }

        private void QuickSendToVendor()
        {
            int tid = GetSelectedTicketID();
            if (tid <= 0)
            {
                MessageBox.Show("يرجى اختيار جهاز من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ticket = WarrantyDAL.GetByID(tid);
            if (ticket == null) return;

            string vendor = InputBoxHelper.Show("أدخل اسم التوكيل أو مركز الصيانة المحول إليه الجهاز:", "تحويل إلى التوكيل", ticket.VendorName ?? "");
            if (string.IsNullOrWhiteSpace(vendor)) return;

            string tracking = InputBoxHelper.Show("أدخل رقم بوليصة الشحن أو إيصال التوكيل (اختياري):", "رقم البوليصة / الإيصال", ticket.VendorTrackingNo ?? "");

            WarrantyDAL.UpdateStatus(tid, "مرسل للتوكيل", vendor.Trim(), tracking.Trim());
            LoadTickets();
            UpdateStats();
            MessageBox.Show($"✅ تم تحديث حالة الجهاز رقم [{ticket.TicketCode}] إلى (مرسل للتوكيل: {vendor}).", "تم التحديث", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void QuickReceiveFromVendor()
        {
            int tid = GetSelectedTicketID();
            if (tid <= 0)
            {
                MessageBox.Show("يرجى اختيار جهاز من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ticket = WarrantyDAL.GetByID(tid);
            if (ticket == null) return;

            string result = InputBoxHelper.Show("أدخل نتيجة فحص التوكيل (مثال: تم الإصلاح بنجاح / تم استبدال جهاز جديد / تم تغيير قطعة غيار / مرفوض سوء استخدام):", "نتيجة الفحص", "تم الإصلاح بنجاح");
            if (string.IsNullOrWhiteSpace(result)) return;

            WarrantyDAL.UpdateStatus(tid, "مستلم من التوكيل (جاهز)", null, null, null, result.Trim());
            LoadTickets();
            UpdateStats();

            var ask = MessageBox.Show($"✅ تم استلام الجهاز من التوكيل وأصبح جاهزاً للتسليم.\n\nهل ترغب في إرسال رسالة واتساب للزبون لإبلاغه بجاهزية الاستلام؟", "إرسال واتساب للزبون", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ask == DialogResult.Yes)
            {
                SendWhatsApp();
            }
        }

        private void QuickDeliverToCustomer()
        {
            int tid = GetSelectedTicketID();
            if (tid <= 0)
            {
                MessageBox.Show("يرجى اختيار جهاز من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ticket = WarrantyDAL.GetByID(tid);
            if (ticket == null) return;

            if (ticket.Status == "تم التسليم للزبون")
            {
                MessageBox.Show("هذا الجهاز تم تسليمه للزبون بالفعل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var res = MessageBox.Show($"هل أنت متأكد من تسليم الجهاز [{ticket.ProductName}] للعميل [{ticket.CustomerName}] وإغلاق التذكرة؟", "تأكيد التسليم للزبون", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;

            WarrantyDAL.UpdateStatus(tid, "تم التسليم للزبون", deliveredDate: DateTime.Now);
            LoadTickets();
            UpdateStats();
            MessageBox.Show($"🤝 تم تسليم الجهاز للعميل [{ticket.CustomerName}] بنجاح وإغلاق التذكرة.", "تم التسليم", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteSelectedTicket()
        {
            int tid = GetSelectedTicketID();
            if (tid <= 0) return;

            var ticket = WarrantyDAL.GetByID(tid);
            if (ticket == null) return;

            var res = MessageBox.Show($"⚠️ هل أنت متأكد تماماً من حذف تذكرة الضمان رقم [{ticket.TicketCode}] الخاصة بالجهاز [{ticket.ProductName}]؟\n\nلن يمكن التراجع عن هذا الإجراء.", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                WarrantyDAL.Delete(tid);
                LoadTickets();
                UpdateStats();
                MessageBox.Show("تم حذف التذكرة بنجاح.", "تم الحذف", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PrintSelectedReceipt()
        {
            int tid = GetSelectedTicketID();
            if (tid <= 0)
            {
                MessageBox.Show("يرجى اختيار تذكرة لطباعة الإيصال الخاص بها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintReceiptByID(tid);
        }

        private void PrintReceiptByID(int ticketID)
        {
            var ticket = WarrantyDAL.GetByID(ticketID);
            if (ticket == null) return;

            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintController = new StandardPrintController();
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);

                pd.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    float y = 10;
                    float receiptWidth = ev.PageBounds.Width > 0 ? ev.PageBounds.Width : 280;
                    if (receiptWidth > 300) receiptWidth = 280; // Standard 80mm thermal width

                    Font fontShop = new Font("Arial", 11f, FontStyle.Bold);
                    Font fontTitle = new Font("Arial", 10.5f, FontStyle.Bold);
                    Font fontBold = new Font("Arial", 8.5f, FontStyle.Bold);
                    Font fontRegular = new Font("Arial", 8f, FontStyle.Regular);
                    Font fontSmall = new Font("Arial", 7f, FontStyle.Regular);

                    StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                    StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

                    // Shop Name & Header
                    g.DrawString(AppConfig.CompanyName, fontShop, Brushes.Black, new RectangleF(0, y, receiptWidth, 22), sfCenter); y += 22;
                    if (!string.IsNullOrEmpty(AppConfig.CompanyPhone))
                    {
                        g.DrawString("هاتف: " + AppConfig.CompanyPhone, fontRegular, Brushes.Black, new RectangleF(0, y, receiptWidth, 16), sfCenter); y += 16;
                    }

                    // Title
                    bool isDelivered = ticket.Status.Contains("تم التسليم");
                    string titleText = isDelivered ? "=== إيصال تسليم جهاز للعميل ===" : "=== إيصال استلام جهاز تحت الضمان ===";
                    g.DrawString(titleText, fontTitle, Brushes.Black, new RectangleF(0, y, receiptWidth, 20), sfCenter); y += 22;

                    // Barcode
                    try
                    {
                        FrmPrintProductBarcode.DrawCode39(g, ticket.TicketCode, (receiptWidth - 190) / 2, y, 190, 26);
                        y += 28;
                        g.DrawString("*" + ticket.TicketCode + "*", fontBold, Brushes.Black, new RectangleF(0, y, receiptWidth, 14), sfCenter);
                        y += 18;
                    }
                    catch
                    {
                        g.DrawString($"رقم الإيصال: {ticket.TicketCode}", fontBold, Brushes.Black, new RectangleF(0, y, receiptWidth, 18), sfCenter);
                        y += 20;
                    }

                    // Divider
                    g.DrawLine(Pens.Black, 5, y, receiptWidth - 5, y); y += 8;

                    // Meta Info
                    Action<string, string> drawRow = (lbl, val) =>
                    {
                        if (string.IsNullOrWhiteSpace(val)) return;
                        g.DrawString(lbl, fontBold, Brushes.Black, new RectangleF(receiptWidth - 110, y, 105, 18), sfRight);
                        g.DrawString(val, fontRegular, Brushes.Black, new RectangleF(5, y, receiptWidth - 120, 18), sfLeft);
                        y += 18;
                    };

                    drawRow("تاريخ الاستلام:", ticket.ReceiveDate.ToString("yyyy/MM/dd HH:mm"));
                    drawRow("اسم العميل:", ticket.CustomerName);
                    drawRow("هاتف العميل:", ticket.CustomerPhone);
                    drawRow("اسم الجهاز:", ticket.ProductName);
                    if (!string.IsNullOrEmpty(ticket.Brand)) drawRow("الماركة:", ticket.Brand);
                    if (!string.IsNullOrEmpty(ticket.SerialNumber)) drawRow("السيريال/الضمان:", ticket.SerialNumber);
                    if (!string.IsNullOrEmpty(ticket.InvoiceNumber)) drawRow("رقم الفاتورة:", ticket.InvoiceNumber);
                    if (!string.IsNullOrEmpty(ticket.Accessories)) drawRow("الملحقات:", ticket.Accessories);
                    if (!string.IsNullOrEmpty(ticket.DeviceCondition)) drawRow("الحالة الظاهرية:", ticket.DeviceCondition);
                    drawRow("العطل والشكوى:", ticket.ProblemDescription);

                    if (!string.IsNullOrEmpty(ticket.VendorName)) drawRow("التوكيل الموجه له:", ticket.VendorName);
                    if (!string.IsNullOrEmpty(ticket.ResolutionType)) drawRow("نتيجة الفحص:", ticket.ResolutionType);
                    drawRow("حالة التذكرة:", ticket.Status);

                    if (ticket.DeliveredToCustomerDate.HasValue)
                        drawRow("تاريخ التسليم:", ticket.DeliveredToCustomerDate.Value.ToString("yyyy/MM/dd HH:mm"));

                    y += 5;
                    g.DrawLine(Pens.Black, 5, y, receiptWidth - 5, y); y += 8;

                    // Terms & Notice
                    string terms = "• يُرجى إحضار هذا الإيصال عند استلام الجهاز.\n• المحل غير مسؤول عن الأجهزة بعد مرور 30 يوماً من إبلاغ العميل بجاهزيتها.";
                    g.DrawString(terms, fontSmall, Brushes.Black, new RectangleF(5, y, receiptWidth - 10, 36), sfRight);
                    y += 40;

                    // Signatures
                    g.DrawString("توقيع المستلم: .................", fontSmall, Brushes.Black, new RectangleF(receiptWidth - 130, y, 125, 16), sfRight);
                    g.DrawString("توقيع العميل: .................", fontSmall, Brushes.Black, new RectangleF(5, y, 120, 16), sfLeft);
                    y += 25;

                    g.DrawString("نشكركم على ثقتكم الغالية", fontBold, Brushes.Black, new RectangleF(0, y, receiptWidth, 16), sfCenter);
                };

                PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
                ppd.StartPosition = FormStartPosition.CenterScreen;
                ppd.Size = new Size(500, 650);
                ppd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء الطباعة: " + ex.Message, "خطأ طباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendWhatsApp()
        {
            int tid = GetSelectedTicketID();
            if (tid <= 0)
            {
                MessageBox.Show("يرجى اختيار جهاز من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ticket = WarrantyDAL.GetByID(tid);
            if (ticket == null) return;

            string phone = (ticket.CustomerPhone ?? "").Trim();
            string cleanPhone = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
            if (string.IsNullOrEmpty(cleanPhone) || cleanPhone.Length < 10)
            {
                MessageBox.Show("رقم هاتف العميل غير صحيح أو غير مسجل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Egypt standard formatting
            if (cleanPhone.StartsWith("01") && cleanPhone.Length == 11)
            {
                cleanPhone = "2" + cleanPhone;
            }

            string company = AppConfig.CompanyName ?? "المحل";
            string msg = "";

            if (ticket.Status.Contains("جاهز") || ticket.Status.Contains("مستلم من التوكيل"))
            {
                msg = $"مرحباً بك أستاذ/ة *{ticket.CustomerName}* 🌹\n" +
                      $"يسعدنا إبلاغكم بأن جهازكم: *{ticket.ProductName}*\n" +
                      $"رقم تذكرة الضمان: *{ticket.TicketCode}*\n" +
                      $"النتيجة: *{ticket.ResolutionType ?? "تم الفحص بنجاح"}*\n\n" +
                      $"الجهاز جاهز للاستلام الآن بمحل *{company}*.\n" +
                      $"بانتظار تشريفكم في أي وقت مع إحضار إيصال الاستلام.";
            }
            else if (ticket.Status == "مرسل للتوكيل")
            {
                msg = $"مرحباً بك أستاذ/ة *{ticket.CustomerName}* 🌹\n" +
                      $"نحيطكم علماً بأنه تم تحويل جهازكم: *{ticket.ProductName}*\n" +
                      $"إلى التوكيل المعتمد (*{ticket.VendorName}*) للفحص والمتابعة.\n" +
                      $"رقم تذكرة الضمان: *{ticket.TicketCode}*\n\n" +
                      $"سنوافيكم بالرد فور الانتهاء من الفحص بمحل *{company}*.";
            }
            else
            {
                msg = $"مرحباً بك أستاذ/ة *{ticket.CustomerName}* 🌹\n" +
                      $"تم استلام جهازكم: *{ticket.ProductName}*\n" +
                      $"برقم تذكرة ضمان: *{ticket.TicketCode}*\n" +
                      $"العطل المسجل: *{ticket.ProblemDescription}*\n\n" +
                      $"بمحل *{company}*. جاري المتابعة وسنوافيكم بكافة المستجدات.";
            }

            try
            {
                string url = $"https://api.whatsapp.com/send?phone={cleanPhone}&text={Uri.EscapeDataString(msg)}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في فتح تطبيق واتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

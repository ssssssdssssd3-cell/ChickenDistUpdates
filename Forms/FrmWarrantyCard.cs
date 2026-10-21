using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmWarrantyCard : Form
    {
        private int _ticketID;
        public int SavedTicketID { get; private set; }
        public bool ShouldPrintReceipt { get; private set; }

        // Controls
        private Label lblTicketCode;
        private DateTimePicker dtpReceiveDate;
        private TextBox txtCustomerName;
        private TextBox txtCustomerPhone;
        private TextBox txtCustomerAddress;
        private TextBox txtProductName;
        private TextBox txtBrand;
        private TextBox txtSerialNumber;
        private TextBox txtInvoiceNumber;
        private DateTimePicker dtpInvoiceDate;
        private CheckBox chkHasInvoiceDate;
        private TextBox txtProblemDescription;
        private TextBox txtAccessories;
        private TextBox txtDeviceCondition;
        private ComboBox cboStatus;
        private TextBox txtVendorName;
        private DateTimePicker dtpSentToVendor;
        private CheckBox chkSentToVendor;
        private TextBox txtVendorTrackingNo;
        private DateTimePicker dtpReceivedFromVendor;
        private CheckBox chkReceivedFromVendor;
        private ComboBox cboResolutionType;
        private TextBox txtVendorReport;
        private DateTimePicker dtpDelivered;
        private CheckBox chkDelivered;
        private TextBox txtNotes;

        // Quick invoice search
        private TextBox txtQuickSearch;
        private Button btnQuickSearch;

        private Button btnSave;
        private Button btnSaveAndPrint;
        private Button btnCancel;

        public FrmWarrantyCard(int ticketID = 0)
        {
            _ticketID = ticketID;
            InitUI();
            LoadAutocomplete();
            if (_ticketID > 0)
            {
                LoadTicketData();
            }
            else
            {
                lblTicketCode.Text = WarrantyDAL.GetNextTicketCode();
                dtpReceiveDate.Value = DateTime.Now;
                cboStatus.SelectedItem = "مستلم بالمحل";
            }
        }

        private void InitUI()
        {
            this.Text = _ticketID > 0 ? "📝 تعديل كرت جهاز تحت الضمان" : "➕ استلام جهاز جديد تحت الضمان";
            this.Size = new Size(820, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2) { btnSave.PerformClick(); e.Handled = true; }
                else if (e.KeyCode == Keys.F3) { btnSaveAndPrint.PerformClick(); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) { this.Close(); e.Handled = true; }
            };

            // Title Bar
            var pnlTitle = Theme.MakeTitleBar(
                _ticketID > 0 ? "📝 تعديل بيانات كرت الضمان" : "🛡️ استلام جهاز تحت الضمان والصيانة",
                "سجل إداري لمتابعة الأجهزة مع العملاء والتوكيلات الخارجية دون التأثير على المخزون أو الحسابات.");
            this.Controls.Add(pnlTitle);

            // Main Scrollable Panel
            var pnlBody = new Panel
            {
                Location = new Point(15, 65),
                Size = new Size(775, 580),
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlBody);

            int y = 5;

            // ── Top Ticket Bar ──
            var pnlHeader = new Panel
            {
                Location = new Point(5, y),
                Size = new Size(745, 45),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlBody.Controls.Add(pnlHeader);

            pnlHeader.Controls.Add(new Label { Text = "رقم الإيصال:", Location = new Point(15, 12), Width = 90, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            lblTicketCode = new Label { Text = "WR-00000", Location = new Point(110, 10), Width = 150, AutoSize = false, ForeColor = Theme.Primary, Font = new Font(Theme.FontMain.FontFamily, 12f, FontStyle.Bold) };
            pnlHeader.Controls.Add(lblTicketCode);

            pnlHeader.Controls.Add(new Label { Text = "تاريخ الاستلام:", Location = new Point(320, 12), Width = 100, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            dtpReceiveDate = new DateTimePicker { Location = new Point(425, 10), Width = 140, Format = DateTimePickerFormat.Short, Font = Theme.FontMain };
            pnlHeader.Controls.Add(dtpReceiveDate);

            // Quick Invoice / Customer Lookup
            pnlHeader.Controls.Add(new Label { Text = "🔍 جلب سريع:", Location = new Point(575, 12), Width = 80, AutoSize = false, ForeColor = Theme.Accent, Font = Theme.FontBold });
            txtQuickSearch = new TextBox { Location = new Point(655, 10), Width = 110, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtQuickSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { DoQuickSearch(); e.Handled = true; e.SuppressKeyPress = true; } };
            btnQuickSearch = new Button { Text = "🔎", Location = new Point(770, 9), Width = 30, Height = 25, BackColor = Theme.Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnQuickSearch.Click += (s, e) => DoQuickSearch();
            // Add tooltip
            var tip = new ToolTip();
            tip.SetToolTip(txtQuickSearch, "اكتب رقم فاتورة أو هاتف الزبون واضغط Enter لجلب البيانات فوراً");
            pnlHeader.Controls.AddRange(new Control[] { txtQuickSearch, btnQuickSearch });

            y += 55;

            // ── GroupBox 1: بيانات العميل ──
            var grpCustomer = CreateSectionBox("👤 بيانات العميل / الزبون", 5, y, 745, 90);
            pnlBody.Controls.Add(grpCustomer);

            grpCustomer.Controls.Add(new Label { Text = "اسم العميل *:", Location = new Point(15, 25), Width = 95, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtCustomerName = new TextBox { Location = new Point(115, 22), Width = 230, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpCustomer.Controls.Add(txtCustomerName);

            grpCustomer.Controls.Add(new Label { Text = "رقم الهاتف *:", Location = new Point(365, 25), Width = 95, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtCustomerPhone = new TextBox { Location = new Point(465, 22), Width = 255, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpCustomer.Controls.Add(txtCustomerPhone);

            grpCustomer.Controls.Add(new Label { Text = "العنوان:", Location = new Point(15, 57), Width = 95, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtCustomerAddress = new TextBox { Location = new Point(115, 54), Width = 605, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpCustomer.Controls.Add(txtCustomerAddress);

            y += 100;

            // ── GroupBox 2: بيانات الجهاز والأصناف ──
            var grpDevice = CreateSectionBox("📦 بيانات الجهاز / الصنف والضمان", 5, y, 745, 125);
            pnlBody.Controls.Add(grpDevice);

            grpDevice.Controls.Add(new Label { Text = "اسم الصنف / الجهاز *:", Location = new Point(15, 25), Width = 145, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtProductName = new TextBox { Location = new Point(165, 22), Width = 280, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpDevice.Controls.Add(txtProductName);

            grpDevice.Controls.Add(new Label { Text = "الماركة / الشركة:", Location = new Point(465, 25), Width = 100, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtBrand = new TextBox { Location = new Point(570, 22), Width = 150, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpDevice.Controls.Add(txtBrand);

            grpDevice.Controls.Add(new Label { Text = "رقم السيريال / كرت الضمان:", Location = new Point(15, 58), Width = 145, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtSerialNumber = new TextBox { Location = new Point(165, 55), Width = 280, Font = new Font("Consolas", 10.5f, FontStyle.Bold), BackColor = Color.FromArgb(240, 245, 255), ForeColor = Color.FromArgb(20, 60, 160) };
            grpDevice.Controls.Add(txtSerialNumber);

            grpDevice.Controls.Add(new Label { Text = "رقم فاتورة الشراء:", Location = new Point(465, 58), Width = 100, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtInvoiceNumber = new TextBox { Location = new Point(570, 55), Width = 150, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpDevice.Controls.Add(txtInvoiceNumber);

            chkHasInvoiceDate = new CheckBox { Text = "تاريخ الشراء:", Location = new Point(15, 90), Width = 145, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            dtpInvoiceDate = new DateTimePicker { Location = new Point(165, 88), Width = 160, Format = DateTimePickerFormat.Short, Font = Theme.FontMain, Enabled = false };
            chkHasInvoiceDate.CheckedChanged += (s, e) => dtpInvoiceDate.Enabled = chkHasInvoiceDate.Checked;
            grpDevice.Controls.AddRange(new Control[] { chkHasInvoiceDate, dtpInvoiceDate });

            y += 135;

            // ── GroupBox 3: الفحص المبدئي والعطل ──
            var grpInspection = CreateSectionBox("🔍 الفحص المبدئي والاستلام من العميل", 5, y, 745, 125);
            pnlBody.Controls.Add(grpInspection);

            grpInspection.Controls.Add(new Label { Text = "وصف العطل والشكوى *:", Location = new Point(15, 25), Width = 145, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtProblemDescription = new TextBox { Location = new Point(165, 22), Width = 555, Height = 42, Multiline = true, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpInspection.Controls.Add(txtProblemDescription);

            grpInspection.Controls.Add(new Label { Text = "الملحقات المستلمة:", Location = new Point(15, 72), Width = 145, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtAccessories = new TextBox { Location = new Point(165, 69), Width = 230, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            // Add quick suggestions for accessories
            var btnAccBox = new Button { Text = "📦 بالكرتونة", Location = new Point(400, 68), Width = 75, Height = 25, Font = new Font(Theme.FontMain.FontFamily, 8f), FlatStyle = FlatStyle.Flat };
            btnAccBox.Click += (s, e) => AppendAccessory("بالكرتونة");
            var btnAccCable = new Button { Text = "🔌 شاحن/كابل", Location = new Point(480, 68), Width = 80, Height = 25, Font = new Font(Theme.FontMain.FontFamily, 8f), FlatStyle = FlatStyle.Flat };
            btnAccCable.Click += (s, e) => AppendAccessory("شاحن/سلك");
            var btnAccNone = new Button { Text = "🚫 جهاز فقط", Location = new Point(565, 68), Width = 75, Height = 25, Font = new Font(Theme.FontMain.FontFamily, 8f), FlatStyle = FlatStyle.Flat };
            btnAccNone.Click += (s, e) => txtAccessories.Text = "الجهاز فقط بدون ملحقات";
            grpInspection.Controls.AddRange(new Control[] { txtAccessories, btnAccBox, btnAccCable, btnAccNone });

            grpInspection.Controls.Add(new Label { Text = "حالة الجهاز الظاهرية:", Location = new Point(15, 99), Width = 145, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtDeviceCondition = new TextBox { Location = new Point(165, 96), Width = 555, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtDeviceCondition.Text = "بحالة جيدة - لا توجد كسور ظاهرة";
            grpInspection.Controls.Add(txtDeviceCondition);

            y += 135;

            // ── GroupBox 4: متابعة التوكيل والصيانة ──
            var grpVendor = CreateSectionBox("🏢 متابعة التوكيل / مركز الصيانة الخارجي", 5, y, 745, 140);
            pnlBody.Controls.Add(grpVendor);

            grpVendor.Controls.Add(new Label { Text = "حالة الجهاز الحالية *:", Location = new Point(15, 25), Width = 135, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            cboStatus = new ComboBox
            {
                Location = new Point(155, 22),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboStatus.Items.AddRange(new object[] { "مستلم بالمحل", "مرسل للتوكيل", "مستلم من التوكيل (جاهز)", "تم التسليم للزبون", "مرفوض الضمان" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += CboStatus_SelectedIndexChanged;
            grpVendor.Controls.Add(cboStatus);

            grpVendor.Controls.Add(new Label { Text = "التوكيل / جهة الصيانة:", Location = new Point(375, 25), Width = 130, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtVendorName = new TextBox { Location = new Point(510, 22), Width = 210, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpVendor.Controls.Add(txtVendorName);

            chkSentToVendor = new CheckBox { Text = "تاريخ الإرسال للتوكيل:", Location = new Point(15, 58), Width = 135, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            dtpSentToVendor = new DateTimePicker { Location = new Point(155, 56), Width = 140, Format = DateTimePickerFormat.Short, Font = Theme.FontMain, Enabled = false };
            chkSentToVendor.CheckedChanged += (s, e) => dtpSentToVendor.Enabled = chkSentToVendor.Checked;
            grpVendor.Controls.AddRange(new Control[] { chkSentToVendor, dtpSentToVendor });

            grpVendor.Controls.Add(new Label { Text = "رقم البوليصة / الإيصال:", Location = new Point(375, 58), Width = 130, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtVendorTrackingNo = new TextBox { Location = new Point(510, 56), Width = 210, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpVendor.Controls.Add(txtVendorTrackingNo);

            chkReceivedFromVendor = new CheckBox { Text = "تاريخ الاستلام من التوكيل:", Location = new Point(15, 90), Width = 135, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            dtpReceivedFromVendor = new DateTimePicker { Location = new Point(155, 88), Width = 140, Format = DateTimePickerFormat.Short, Font = Theme.FontMain, Enabled = false };
            chkReceivedFromVendor.CheckedChanged += (s, e) => dtpReceivedFromVendor.Enabled = chkReceivedFromVendor.Checked;
            grpVendor.Controls.AddRange(new Control[] { chkReceivedFromVendor, dtpReceivedFromVendor });

            grpVendor.Controls.Add(new Label { Text = "نتيجة الفحص / الحل:", Location = new Point(375, 90), Width = 130, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            cboResolutionType = new ComboBox
            {
                Location = new Point(510, 88),
                Width = 210,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboResolutionType.Items.AddRange(new object[] { "قيد الفحص", "تم الإصلاح بنجاح", "تم استبدال جهاز جديد", "تم استبدال قطعة غيار", "مرفوض - سوء استخدام", "مرفوض - خارج فترة الضمان", "لا يمكن إصلاحه" });
            cboResolutionType.SelectedIndex = 0;
            grpVendor.Controls.Add(cboResolutionType);

            grpVendor.Controls.Add(new Label { Text = "تقرير وملاحظات التوكيل:", Location = new Point(15, 120), Width = 135, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtVendorReport = new TextBox { Location = new Point(155, 117), Width = 565, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpVendor.Controls.Add(txtVendorReport);

            y += 150;

            // ── GroupBox 5: التسليم للزبون والملاحظات ──
            var grpDelivery = CreateSectionBox("🤝 التسليم للزبون والملاحظات", 5, y, 745, 95);
            pnlBody.Controls.Add(grpDelivery);

            chkDelivered = new CheckBox { Text = "تم التسليم للزبون بتاريخ:", Location = new Point(15, 25), Width = 155, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            dtpDelivered = new DateTimePicker { Location = new Point(175, 23), Width = 140, Format = DateTimePickerFormat.Short, Font = Theme.FontMain, Enabled = false };
            chkDelivered.CheckedChanged += (s, e) =>
            {
                dtpDelivered.Enabled = chkDelivered.Checked;
                if (chkDelivered.Checked && cboStatus.SelectedItem?.ToString() != "تم التسليم للزبون")
                {
                    cboStatus.SelectedItem = "تم التسليم للزبون";
                }
            };
            grpDelivery.Controls.AddRange(new Control[] { chkDelivered, dtpDelivered });

            grpDelivery.Controls.Add(new Label { Text = "ملاحظات إضافية:", Location = new Point(15, 58), Width = 120, AutoSize = false, ForeColor = Theme.TextMain, Font = Theme.FontMain });
            txtNotes = new TextBox { Location = new Point(140, 55), Width = 580, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            grpDelivery.Controls.Add(txtNotes);

            // ── Footer Panel (Save / Print / Cancel) ──
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlFooter);

            btnSave = Theme.MakeButton("💾 حفظ الكرت (F2)", 570, 14, 180, 38, Theme.Success);
            btnSave.Click += (s, e) => SaveTicket(false);

            btnSaveAndPrint = Theme.MakeButton("🖨️ حفظ وطباعة الإيصال (F3)", 330, 14, 230, 38, Color.FromArgb(30, 80, 180));
            btnSaveAndPrint.Click += (s, e) => SaveTicket(true);

            btnCancel = Theme.MakeButton("❌ إلغاء (Esc)", 190, 14, 130, 38, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();

            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnSaveAndPrint, btnCancel });
        }

        private GroupBox CreateSectionBox(string title, int x, int y, int width, int height)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                ForeColor = Theme.Primary,
                BackColor = Theme.BgCard
            };
        }

        private void AppendAccessory(string acc)
        {
            if (string.IsNullOrWhiteSpace(txtAccessories.Text))
                txtAccessories.Text = acc;
            else if (!txtAccessories.Text.Contains(acc))
                txtAccessories.Text += " + " + acc;
        }

        private void CboStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            string st = cboStatus.SelectedItem?.ToString();
            if (st == "مرسل للتوكيل")
            {
                chkSentToVendor.Checked = true;
                dtpSentToVendor.Enabled = true;
            }
            else if (st == "مستلم من التوكيل (جاهز)")
            {
                chkReceivedFromVendor.Checked = true;
                dtpReceivedFromVendor.Enabled = true;
            }
            else if (st == "تم التسليم للزبون")
            {
                chkDelivered.Checked = true;
                dtpDelivered.Enabled = true;
            }
        }

        private void LoadAutocomplete()
        {
            try
            {
                // Products
                var dtProd = DbHelper.Query("SELECT DISTINCT TOP 500 ProductName FROM Products WHERE IsActive = 1");
                var prodColl = new AutoCompleteStringCollection();
                foreach (DataRow r in dtProd.Rows)
                {
                    string p = r["ProductName"]?.ToString();
                    if (!string.IsNullOrEmpty(p)) prodColl.Add(p);
                }
                txtProductName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtProductName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtProductName.AutoCompleteCustomSource = prodColl;

                // Clients
                var dtClients = DbHelper.Query("SELECT DISTINCT TOP 500 ClientName FROM Clients WHERE IsActive = 1");
                var clientColl = new AutoCompleteStringCollection();
                foreach (DataRow r in dtClients.Rows)
                {
                    string c = r["ClientName"]?.ToString();
                    if (!string.IsNullOrEmpty(c)) clientColl.Add(c);
                }
                txtCustomerName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtCustomerName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtCustomerName.AutoCompleteCustomSource = clientColl;

                // Suppliers / Vendors
                var dtSuppliers = DbHelper.Query("SELECT DISTINCT TOP 300 SupplierName FROM Suppliers WHERE IsActive = 1");
                var suppColl = new AutoCompleteStringCollection();
                foreach (DataRow r in dtSuppliers.Rows)
                {
                    string s = r["SupplierName"]?.ToString();
                    if (!string.IsNullOrEmpty(s)) suppColl.Add(s);
                }
                txtVendorName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtVendorName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtVendorName.AutoCompleteCustomSource = suppColl;
            }
            catch { }
        }

        private void DoQuickSearch()
        {
            string q = txtQuickSearch.Text.Trim();
            if (string.IsNullOrEmpty(q))
            {
                MessageBox.Show("يرجى كتابة رقم فاتورة أو رقم هاتف الزبون للبحث.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var dt = WarrantyDAL.SearchSalesByQuery(q);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("لم يتم العثور على فواتير أو مبيعات مطابقة لرقم البحث.", "بحث فواتير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (dt.Rows.Count == 1)
                {
                    ApplySaleRow(dt.Rows[0]);
                }
                else
                {
                    // Show a quick picker dialog
                    using (var dlg = new Form())
                    {
                        dlg.Text = "🔍 اختر الصنف / الفاتورة المطابقة";
                        dlg.Size = new Size(680, 380);
                        dlg.StartPosition = FormStartPosition.CenterParent;
                        dlg.RightToLeft = RightToLeft.Yes;
                        dlg.RightToLeftLayout = true;
                        dlg.FormBorderStyle = FormBorderStyle.FixedDialog;

                        var dgv = new DataGridView
                        {
                            Dock = DockStyle.Fill,
                            DataSource = dt,
                            ReadOnly = true,
                            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                            MultiSelect = false,
                            AllowUserToAddRows = false,
                            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                        };
                        Theme.ApplyGridTheme(dgv);

                        var btnChoose = Theme.MakeButton("اختيار الصنف المكتمل ✔", 20, 10, 200, 35, Theme.Success);
                        btnChoose.Dock = DockStyle.Bottom;
                        btnChoose.Click += (s, e) =>
                        {
                            if (dgv.SelectedRows.Count > 0)
                            {
                                var r = (dgv.SelectedRows[0].DataBoundItem as DataRowView)?.Row;
                                if (r != null) ApplySaleRow(r);
                                dlg.DialogResult = DialogResult.OK;
                                dlg.Close();
                            }
                        };

                        dlg.Controls.Add(dgv);
                        dlg.Controls.Add(btnChoose);
                        dlg.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء البحث: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySaleRow(DataRow r)
        {
            if (r.Table.Columns.Contains("ClientName") && r["ClientName"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["ClientName"].ToString()))
                txtCustomerName.Text = r["ClientName"].ToString();

            if (r.Table.Columns.Contains("Phone") && r["Phone"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["Phone"].ToString()))
                txtCustomerPhone.Text = r["Phone"].ToString();

            if (r.Table.Columns.Contains("ProductName") && r["ProductName"] != DBNull.Value)
                txtProductName.Text = r["ProductName"].ToString();

            if (r.Table.Columns.Contains("SaleCode") && r["SaleCode"] != DBNull.Value)
                txtInvoiceNumber.Text = r["SaleCode"].ToString();

            if (r.Table.Columns.Contains("SaleDate") && r["SaleDate"] != DBNull.Value)
            {
                chkHasInvoiceDate.Checked = true;
                dtpInvoiceDate.Value = Convert.ToDateTime(r["SaleDate"]);
                dtpInvoiceDate.Enabled = true;
            }

            if (r.Table.Columns.Contains("IMEI") && r["IMEI"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["IMEI"].ToString()))
                txtSerialNumber.Text = r["IMEI"].ToString();

            MessageBox.Show("✅ تم جلب بيانات العميل والفاتورة والصنف بنجاح!", "تم الجلب", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadTicketData()
        {
            var t = WarrantyDAL.GetByID(_ticketID);
            if (t == null)
            {
                MessageBox.Show("لم يتم العثور على التذكرة المطلوبة.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                return;
            }

            lblTicketCode.Text = t.TicketCode;
            dtpReceiveDate.Value = t.ReceiveDate;
            txtCustomerName.Text = t.CustomerName;
            txtCustomerPhone.Text = t.CustomerPhone;
            txtCustomerAddress.Text = t.CustomerAddress;
            txtProductName.Text = t.ProductName;
            txtBrand.Text = t.Brand;
            txtSerialNumber.Text = t.SerialNumber;
            txtInvoiceNumber.Text = t.InvoiceNumber;

            if (t.InvoiceDate.HasValue)
            {
                chkHasInvoiceDate.Checked = true;
                dtpInvoiceDate.Value = t.InvoiceDate.Value;
                dtpInvoiceDate.Enabled = true;
            }

            txtProblemDescription.Text = t.ProblemDescription;
            txtAccessories.Text = t.Accessories;
            txtDeviceCondition.Text = t.DeviceCondition;

            if (!string.IsNullOrEmpty(t.Status) && cboStatus.Items.Contains(t.Status))
                cboStatus.SelectedItem = t.Status;

            txtVendorName.Text = t.VendorName;

            if (t.SentToVendorDate.HasValue)
            {
                chkSentToVendor.Checked = true;
                dtpSentToVendor.Value = t.SentToVendorDate.Value;
                dtpSentToVendor.Enabled = true;
            }

            txtVendorTrackingNo.Text = t.VendorTrackingNo;

            if (t.ReceivedFromVendorDate.HasValue)
            {
                chkReceivedFromVendor.Checked = true;
                dtpReceivedFromVendor.Value = t.ReceivedFromVendorDate.Value;
                dtpReceivedFromVendor.Enabled = true;
            }

            if (!string.IsNullOrEmpty(t.ResolutionType) && cboResolutionType.Items.Contains(t.ResolutionType))
                cboResolutionType.SelectedItem = t.ResolutionType;

            txtVendorReport.Text = t.VendorReport;

            if (t.DeliveredToCustomerDate.HasValue)
            {
                chkDelivered.Checked = true;
                dtpDelivered.Value = t.DeliveredToCustomerDate.Value;
                dtpDelivered.Enabled = true;
            }

            txtNotes.Text = t.Notes;
        }

        private void SaveTicket(bool printAfterSave)
        {
            string custName = txtCustomerName.Text.Trim();
            if (string.IsNullOrWhiteSpace(custName))
            {
                MessageBox.Show("يرجى إدخال اسم العميل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCustomerName.Focus();
                return;
            }

            string prodName = txtProductName.Text.Trim();
            if (string.IsNullOrWhiteSpace(prodName))
            {
                MessageBox.Show("يرجى إدخال اسم الصنف / الجهاز المستلم.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtProductName.Focus();
                return;
            }

            string problem = txtProblemDescription.Text.Trim();
            if (string.IsNullOrWhiteSpace(problem))
            {
                MessageBox.Show("يرجى كتابة وصف العطل وشكوى العميل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtProblemDescription.Focus();
                return;
            }

            try
            {
                var t = new WarrantyTicket
                {
                    TicketID = _ticketID,
                    TicketCode = lblTicketCode.Text,
                    ReceiveDate = dtpReceiveDate.Value,
                    CustomerName = custName,
                    CustomerPhone = txtCustomerPhone.Text.Trim(),
                    CustomerAddress = txtCustomerAddress.Text.Trim(),
                    ProductName = prodName,
                    Brand = txtBrand.Text.Trim(),
                    SerialNumber = txtSerialNumber.Text.Trim(),
                    InvoiceNumber = txtInvoiceNumber.Text.Trim(),
                    InvoiceDate = chkHasInvoiceDate.Checked ? (DateTime?)dtpInvoiceDate.Value : null,
                    ProblemDescription = problem,
                    Accessories = txtAccessories.Text.Trim(),
                    DeviceCondition = txtDeviceCondition.Text.Trim(),
                    Status = cboStatus.SelectedItem?.ToString() ?? "مستلم بالمحل",
                    VendorName = txtVendorName.Text.Trim(),
                    SentToVendorDate = chkSentToVendor.Checked ? (DateTime?)dtpSentToVendor.Value : null,
                    VendorTrackingNo = txtVendorTrackingNo.Text.Trim(),
                    ReceivedFromVendorDate = chkReceivedFromVendor.Checked ? (DateTime?)dtpReceivedFromVendor.Value : null,
                    ResolutionType = cboResolutionType.SelectedItem?.ToString(),
                    VendorReport = txtVendorReport.Text.Trim(),
                    DeliveredToCustomerDate = chkDelivered.Checked ? (DateTime?)dtpDelivered.Value : null,
                    Notes = txtNotes.Text.Trim()
                };

                int savedID = WarrantyDAL.Save(t);
                if (savedID > 0)
                {
                    SavedTicketID = savedID;
                    ShouldPrintReceipt = printAfterSave;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("حدث خطأ أثناء حفظ كرت الضمان.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

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
        private int? _selectedClientID = null;
        private int? _selectedProductID = null;
        public int SavedTicketID { get; private set; }
        public bool ShouldPrintReceipt { get; private set; }
        public bool ShouldSendWhatsApp { get; private set; }

        // Controls
        private Label lblTicketCode;
        private DateTimePicker dtpReceiveDate;
        private TextBox txtCustomerName;
        private Button btnSearchClient;
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
        private Button btnSaveAndWhatsApp;
        private Button btnDirectPrint;
        private Button btnDirectWhatsApp;
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
            this.Text = _ticketID > 0 ? "تعديل كرت جهاز تحت الضمان" : "استلام جهاز جديد تحت الضمان";
            int screenH = Screen.FromControl(this)?.WorkingArea.Height ?? Screen.PrimaryScreen.WorkingArea.Height;
            int screenW = Screen.FromControl(this)?.WorkingArea.Width ?? Screen.PrimaryScreen.WorkingArea.Width;
            int formH = Math.Min(720, screenH - 40);
            int formW = Math.Min(980, screenW - 40);
            this.Size = new Size(formW, formH);
            this.MinimumSize = new Size(860, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.WindowState = FormWindowState.Maximized;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false; // Prevents WinForms GDI mirroring glitches with child containers
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F1) { OpenGeneralProductSearch(); e.Handled = true; }
                else if (e.KeyCode == Keys.F2) { btnSave.PerformClick(); e.Handled = true; }
                else if (e.KeyCode == Keys.F3) { btnSaveAndPrint.PerformClick(); e.Handled = true; }
                else if (e.KeyCode == Keys.F4) { OpenClientSearch(); e.Handled = true; }
                else if (e.KeyCode == Keys.F5) { btnSaveAndWhatsApp?.PerformClick(); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) { this.Close(); e.Handled = true; }
            };

            // ── Top Title Bar (Docked Top) ──
            Panel pnlTitle = Theme.MakeTitleBar(
                _ticketID > 0 ? "تعديل بيانات كرت الضمان" : "استلام جهاز تحت الضمان والصيانة",
                "سجل إداري لمتابعة الأجهزة مع العملاء والتوكيلات الخارجية دون التأثير على المخزون أو الحسابات.");
            pnlTitle.Dock = DockStyle.Top;

            // ── Footer Panel for Action Buttons (Docked Bottom) ──
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };

            FlowLayoutPanel pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(15, 8, 15, 8),
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnSave = Theme.MakeButton("حفظ فقط (F2)", Theme.Success);
            btnSave.Size = new Size(130, 38);
            btnSave.Margin = new Padding(8, 0, 0, 0);
            btnSave.Click += (s, e) => SaveTicket(printAfterSave: false, sendWhatsAppAfterSave: false);

            btnSaveAndPrint = Theme.MakeButton("حفظ وطباعة (F3)", Color.FromArgb(30, 80, 180));
            btnSaveAndPrint.Size = new Size(160, 38);
            btnSaveAndPrint.Margin = new Padding(8, 0, 0, 0);
            btnSaveAndPrint.Click += (s, e) => SaveTicket(printAfterSave: true, sendWhatsAppAfterSave: false);

            btnSaveAndWhatsApp = Theme.MakeButton("حفظ وواتساب (F5)", Color.FromArgb(37, 211, 102));
            btnSaveAndWhatsApp.Size = new Size(160, 38);
            btnSaveAndWhatsApp.ForeColor = Color.White;
            btnSaveAndWhatsApp.Margin = new Padding(8, 0, 0, 0);
            btnSaveAndWhatsApp.Click += (s, e) => SaveTicket(printAfterSave: false, sendWhatsAppAfterSave: true);

            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnSaveAndPrint);
            pnlButtons.Controls.Add(btnSaveAndWhatsApp);

            if (_ticketID > 0)
            {
                btnDirectPrint = Theme.MakeButton("طباعة فورية", Color.FromArgb(52, 73, 94));
                btnDirectPrint.Size = new Size(130, 38);
                btnDirectPrint.Margin = new Padding(8, 0, 0, 0);
                btnDirectPrint.Click += (s, e) =>
                {
                    var ticket = BuildTicketFromUI();
                    ticket.TicketID = _ticketID;
                    WarrantyReceiptHelper.PrintReceipt(ticket, showPreview: true);
                };

                btnDirectWhatsApp = Theme.MakeButton("إرسال واتساب", Color.FromArgb(18, 140, 126));
                btnDirectWhatsApp.Size = new Size(130, 38);
                btnDirectWhatsApp.ForeColor = Color.White;
                btnDirectWhatsApp.Margin = new Padding(8, 0, 0, 0);
                btnDirectWhatsApp.Click += (s, e) =>
                {
                    var ticket = BuildTicketFromUI();
                    ticket.TicketID = _ticketID;
                    WarrantyReceiptHelper.SendWhatsApp(this, ticket);
                };

                pnlButtons.Controls.Add(btnDirectPrint);
                pnlButtons.Controls.Add(btnDirectWhatsApp);
            }

            btnCancel = Theme.MakeButton("إلغاء (Esc)", Color.FromArgb(100, 110, 120));
            btnCancel.Size = new Size(110, 38);
            btnCancel.Margin = new Padding(8, 0, 0, 0);
            btnCancel.Click += (s, e) => this.Close();

            pnlButtons.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(pnlButtons);

            // ── Main Scrollable Body ──
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(15, 10, 15, 10)
            };

            int curY = 10;
            const int boxWidth = 825;

            // ── 1. Top Ticket Header Bar ──
            Panel pnlHeader = new Panel
            {
                Location = new Point(15, curY),
                Size = new Size(boxWidth, 48),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlBody.Controls.Add(pnlHeader);

            // Right side of Header: Ticket Number & Date
            pnlHeader.Controls.Add(new Label
            {
                Text = "رقم الإيصال:",
                Location = new Point(720, 13),
                Width = 90,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            });

            lblTicketCode = new Label
            {
                Text = "WR-00000",
                Location = new Point(600, 10),
                Width = 115,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.Primary,
                Font = new Font(Theme.FontMain.FontFamily, 12f, FontStyle.Bold)
            };
            pnlHeader.Controls.Add(lblTicketCode);

            pnlHeader.Controls.Add(new Label
            {
                Text = "تاريخ الاستلام:",
                Location = new Point(485, 13),
                Width = 100,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            });

            dtpReceiveDate = new DateTimePicker
            {
                Location = new Point(345, 11),
                Width = 135,
                Format = DateTimePickerFormat.Short,
                Font = Theme.FontMain
            };
            pnlHeader.Controls.Add(dtpReceiveDate);

            // Left side of Header: Quick invoice lookup
            pnlHeader.Controls.Add(new Label
            {
                Text = "بحث سريع:",
                Location = new Point(230, 13),
                Width = 95,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.Accent,
                Font = Theme.FontBold
            });

            txtQuickSearch = new TextBox
            {
                Location = new Point(55, 11),
                Width = 170,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            txtQuickSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { DoQuickSearch(); e.Handled = true; e.SuppressKeyPress = true; } };

            btnQuickSearch = new Button
            {
                Text = "بحث",
                Location = new Point(15, 10),
                Width = 36,
                Height = 27,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnQuickSearch.FlatAppearance.BorderSize = 0;
            btnQuickSearch.Click += (s, e) => DoQuickSearch();

            var tip = new ToolTip();
            tip.SetToolTip(txtQuickSearch, "اكتب رقم فاتورة أو هاتف الزبون واضغط Enter لجلب البيانات فوراً");
            pnlHeader.Controls.AddRange(new Control[] { txtQuickSearch, btnQuickSearch });

            curY += 58;

            // ── 2. GroupBox: بيانات العميل ──
            GroupBox grpCustomer = CreateSectionBox("بيانات العميل / الزبون", 15, curY, boxWidth, 105);
            pnlBody.Controls.Add(grpCustomer);

            txtCustomerName = new TextBox { Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            btnSearchClient = new Button
            {
                Text = "بحث",
                Size = new Size(34, 26),
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSearchClient.FlatAppearance.BorderSize = 0;
            var tipClient = new ToolTip();
            tipClient.SetToolTip(btnSearchClient, "بحث واختيار عميل مسجل من الدليل (F4)");
            btnSearchClient.Click += (s, e) => OpenClientSearch();

            txtCustomerPhone = new TextBox { Width = 250, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtCustomerAddress = new TextBox { Width = 660, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            // Row 1: Right Side (Customer Name + Search Button) / Left Side (Phone)
            Label lblCust = new Label
            {
                Text = "اسم العميل *:",
                Location = new Point(675, 24 + 2),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            btnSearchClient.Location = new Point(415, 24);
            txtCustomerName.Location = new Point(453, 24);
            txtCustomerName.Width = 212;

            Label lblPhone = new Label
            {
                Text = "رقم الهاتف *:",
                Location = new Point(275, 24 + 2),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            txtCustomerPhone.Location = new Point(15, 24);
            txtCustomerPhone.Width = 250;

            grpCustomer.Controls.AddRange(new Control[] { lblCust, txtCustomerName, btnSearchClient, lblPhone, txtCustomerPhone });
            AddOneFieldRow(grpCustomer, 62, "العنوان بالتفصيل:", txtCustomerAddress);

            curY += 115;

            // ── 3. GroupBox: بيانات الجهاز والأصناف ──
            GroupBox grpDevice = CreateSectionBox("بيانات الجهاز / الصنف والضمان", 15, curY, boxWidth, 142);
            pnlBody.Controls.Add(grpDevice);

            txtProductName = new TextBox { Width = 175, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            Button btnSearchProduct = new Button
            {
                Text = "بحث",
                Size = new Size(34, 26),
                Location = new Point(452, 24),
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSearchProduct.FlatAppearance.BorderSize = 0;
            var tipProd = new ToolTip();
            tipProd.SetToolTip(btnSearchProduct, "بحث واختيار صنف من دليل كل الأصناف (بدون فاتورة) [F1]");
            btnSearchProduct.Click += (s, e) => OpenGeneralProductSearch();

            Button btnClientPurchases = new Button
            {
                Text = "سجل",
                Size = new Size(34, 26),
                Location = new Point(415, 24),
                BackColor = Color.FromArgb(30, 80, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClientPurchases.FlatAppearance.BorderSize = 0;
            var tipPurchases = new ToolTip();
            tipPurchases.SetToolTip(btnClientPurchases, "جلب جهاز مسجل من فواتير ومشتريات العميل السابقة");
            btnClientPurchases.Click += (s, e) => OpenClientPurchasesSearch();

            Label lblProd = new Label
            {
                Text = "الصنف / الجهاز *:",
                Location = new Point(675, 24 + 2),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            txtProductName.Location = new Point(490, 24);

            txtBrand = new TextBox { Width = 250, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            Label lblBrand = new Label
            {
                Text = "الماركة / الشركة:",
                Location = new Point(275, 24 + 2),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            txtBrand.Location = new Point(15, 24);

            grpDevice.Controls.AddRange(new Control[] { lblProd, txtProductName, btnSearchProduct, btnClientPurchases, lblBrand, txtBrand });

            txtSerialNumber = new TextBox { Width = 250, Font = new Font("Consolas", 10.5f, FontStyle.Bold), BackColor = Color.FromArgb(240, 245, 255), ForeColor = Color.FromArgb(20, 60, 160) };
            txtInvoiceNumber = new TextBox { Width = 250, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            AddTwoFieldRow(grpDevice, 62, "السيريال / IMEI *:", txtSerialNumber, "رقم فاتورة الشراء:", txtInvoiceNumber);

            // Row 3: تاريخ الشراء
            chkHasInvoiceDate = new CheckBox
            {
                Text = "تاريخ الشراء:",
                Location = new Point(675, 99),
                Width = 130,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            dtpInvoiceDate = new DateTimePicker
            {
                Location = new Point(415, 98),
                Width = 250,
                Format = DateTimePickerFormat.Short,
                Font = Theme.FontMain,
                Enabled = false
            };
            chkHasInvoiceDate.CheckedChanged += (s, e) => dtpInvoiceDate.Enabled = chkHasInvoiceDate.Checked;
            grpDevice.Controls.AddRange(new Control[] { chkHasInvoiceDate, dtpInvoiceDate });

            curY += 152;

            // ── 4. GroupBox: الفحص المبدئي والاستلام من العميل ──
            GroupBox grpInspection = CreateSectionBox("الفحص المبدئي والاستلام من العميل", 15, curY, boxWidth, 150);
            pnlBody.Controls.Add(grpInspection);

            txtProblemDescription = new TextBox
            {
                Width = 660,
                Height = 44,
                Multiline = true,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            AddOneFieldRow(grpInspection, 24, "وصف العطل *:", txtProblemDescription);

            txtAccessories = new TextBox { Width = 370, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            // Helper quick buttons for accessories
            Button btnAccBox = new Button { Text = "بالكرتونة", Location = new Point(205, 75), Width = 85, Height = 26, Font = new Font(Theme.FontMain.FontFamily, 8.5f), FlatStyle = FlatStyle.Flat };
            btnAccBox.Click += (s, e) => AppendAccessory("بالكرتونة");
            Button btnAccCable = new Button { Text = "شاحن/كابل", Location = new Point(110, 75), Width = 90, Height = 26, Font = new Font(Theme.FontMain.FontFamily, 8.5f), FlatStyle = FlatStyle.Flat };
            btnAccCable.Click += (s, e) => AppendAccessory("شاحن/سلك");
            Button btnAccNone = new Button { Text = "جهاز فقط", Location = new Point(15, 75), Width = 90, Height = 26, Font = new Font(Theme.FontMain.FontFamily, 8.5f), FlatStyle = FlatStyle.Flat };
            btnAccNone.Click += (s, e) => txtAccessories.Text = "الجهاز فقط بدون ملحقات";

            Label lblAcc = new Label
            {
                Text = "الملحقات المستلمة:",
                Location = new Point(675, 77),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            txtAccessories.Location = new Point(295, 75);
            grpInspection.Controls.AddRange(new Control[] { lblAcc, txtAccessories, btnAccBox, btnAccCable, btnAccNone });

            txtDeviceCondition = new TextBox { Width = 660, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "بحالة جيدة - لا توجد كسور ظاهرة" };
            AddOneFieldRow(grpInspection, 108, "الحالة الظاهرية:", txtDeviceCondition);

            curY += 160;

            // ── 5. GroupBox: متابعة التوكيل والصيانة الخارجية ──
            GroupBox grpVendor = CreateSectionBox("متابعة التوكيل / مركز الصيانة الخارجي", 15, curY, boxWidth, 180);
            pnlBody.Controls.Add(grpVendor);

            cboStatus = new ComboBox
            {
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboStatus.Items.AddRange(new object[] { "مستلم بالمحل", "مرسل للتوكيل", "مستلم من التوكيل (جاهز)", "تم التسليم للزبون", "مرفوض الضمان" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += CboStatus_SelectedIndexChanged;

            txtVendorName = new TextBox { Width = 250, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            AddTwoFieldRow(grpVendor, 24, "حالة الجهاز الحالية *:", cboStatus, "التوكيل / الورشة:", txtVendorName);

            // Row 2: Sent to vendor date & tracking no
            chkSentToVendor = new CheckBox
            {
                Text = "تاريخ الإرسال:",
                Location = new Point(675, 62),
                Width = 130,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            dtpSentToVendor = new DateTimePicker
            {
                Location = new Point(415, 60),
                Width = 250,
                Format = DateTimePickerFormat.Short,
                Font = Theme.FontMain,
                Enabled = false
            };
            chkSentToVendor.CheckedChanged += (s, e) => dtpSentToVendor.Enabled = chkSentToVendor.Checked;

            txtVendorTrackingNo = new TextBox { Width = 250, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };

            grpVendor.Controls.AddRange(new Control[] { chkSentToVendor, dtpSentToVendor });
            AddSingleFieldOnLeft(grpVendor, 60, "رقم البوليصة / الشحن:", txtVendorTrackingNo);

            // Row 3: Received from vendor date & resolution
            chkReceivedFromVendor = new CheckBox
            {
                Text = "تاريخ الاستلام:",
                Location = new Point(675, 98),
                Width = 130,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            dtpReceivedFromVendor = new DateTimePicker
            {
                Location = new Point(415, 96),
                Width = 250,
                Format = DateTimePickerFormat.Short,
                Font = Theme.FontMain,
                Enabled = false
            };
            chkReceivedFromVendor.CheckedChanged += (s, e) => dtpReceivedFromVendor.Enabled = chkReceivedFromVendor.Checked;

            cboResolutionType = new ComboBox
            {
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboResolutionType.Items.AddRange(new object[] { "قيد الفحص", "تم الإصلاح بنجاح", "تم استبدال جهاز جديد", "تم استبدال قطعة غيار", "مرفوض - سوء استخدام", "مرفوض - خارج فترة الضمان", "لا يمكن إصلاحه" });
            cboResolutionType.SelectedIndex = 0;

            grpVendor.Controls.AddRange(new Control[] { chkReceivedFromVendor, dtpReceivedFromVendor });
            AddSingleFieldOnLeft(grpVendor, 96, "نتيجة الفحص / الحل:", cboResolutionType);

            // Row 4: Vendor report
            txtVendorReport = new TextBox { Width = 660, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            AddOneFieldRow(grpVendor, 134, "تقرير وملاحظات التوكيل:", txtVendorReport);

            curY += 190;

            // ── 6. GroupBox: التسليم للزبون والملاحظات ──
            GroupBox grpDelivery = CreateSectionBox("التسليم للزبون والملاحظات", 15, curY, boxWidth, 105);
            pnlBody.Controls.Add(grpDelivery);

            chkDelivered = new CheckBox
            {
                Text = "تم التسليم للزبون بتاريخ:",
                Location = new Point(625, 24),
                Width = 180,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            dtpDelivered = new DateTimePicker
            {
                Location = new Point(415, 22),
                Width = 205,
                Format = DateTimePickerFormat.Short,
                Font = Theme.FontMain,
                Enabled = false
            };
            chkDelivered.CheckedChanged += (s, e) =>
            {
                dtpDelivered.Enabled = chkDelivered.Checked;
                if (chkDelivered.Checked && cboStatus.SelectedItem?.ToString() != "تم التسليم للزبون")
                {
                    cboStatus.SelectedItem = "تم التسليم للزبون";
                }
            };
            grpDelivery.Controls.AddRange(new Control[] { chkDelivered, dtpDelivered });

            txtNotes = new TextBox { Width = 660, Font = Theme.FontMain, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            AddOneFieldRow(grpDelivery, 60, "ملاحظات إضافية:", txtNotes);

            curY += 120;

            // Add main docked components with proper WinForms Z-order to prevent title overlap
            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlTitle);
            pnlTitle.SendToBack();
            pnlFooter.SendToBack();
            pnlBody.BringToFront();

            Action layoutCards = () =>
            {
                int startX = Math.Max(15, (pnlBody.ClientSize.Width - boxWidth) / 2);
                pnlBody.SuspendLayout();
                foreach (Control c in pnlBody.Controls)
                {
                    if (c is GroupBox || c == pnlHeader)
                    {
                        c.Left = startX;
                    }
                }
                pnlBody.ResumeLayout();
            };
            pnlBody.Resize += (s, e) => layoutCards();
            layoutCards();
        }

        // ==========================================
        // RTL FORM LAYOUT HELPERS
        // ==========================================
        // In Arabic RTL, reading is strictly RIGHT TO LEFT:
        // Right side (Col 1): [Label 1 (X=675..805)] followed by its [Input Control 1 (X=415..665)]
        // Left side  (Col 2): [Label 2 (X=275..405)] followed by its [Input Control 2 (X=15..265)]

        private void AddTwoFieldRow(GroupBox grp, int y, string lblRightText, Control ctrlRight, string lblLeftText, Control ctrlLeft)
        {
            // Right Side
            Label lblRight = new Label
            {
                Text = lblRightText,
                Location = new Point(675, y + 2),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            ctrlRight.Location = new Point(415, y);
            ctrlRight.Width = 250;

            // Left Side
            Label lblLeft = new Label
            {
                Text = lblLeftText,
                Location = new Point(275, y + 2),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            ctrlLeft.Location = new Point(15, y);
            ctrlLeft.Width = 250;

            grp.Controls.AddRange(new Control[] { lblRight, ctrlRight, lblLeft, ctrlLeft });
        }

        private void AddOneFieldRow(GroupBox grp, int y, string lblText, Control ctrl)
        {
            Label lbl = new Label
            {
                Text = lblText,
                Location = new Point(675, y + 2),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            ctrl.Location = new Point(15, y);
            ctrl.Width = 650;

            grp.Controls.AddRange(new Control[] { lbl, ctrl });
        }

        private void AddSingleFieldOnLeft(GroupBox grp, int y, string lblLeftText, Control ctrlLeft)
        {
            Label lblLeft = new Label
            {
                Text = lblLeftText,
                Location = new Point(275, y + 2),
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            ctrlLeft.Location = new Point(15, y);
            ctrlLeft.Width = 250;

            grp.Controls.AddRange(new Control[] { lblLeft, ctrlLeft });
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

        private void OpenClientSearch()
        {
            using (var frm = new FrmClientSearch())
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedClientID > 0)
                {
                    ApplyClientSelection(frm.SelectedClientID, frm.SelectedClientName, frm.SelectedClientPhone);
                }
            }
        }

        private void ApplyClientSelection(int clientId, string clientName, string clientPhone)
        {
            _selectedClientID = clientId;
            txtCustomerName.Text = clientName;
            txtCustomerPhone.Text = clientPhone;

            try
            {
                var dt = DbHelper.Query("SELECT Address FROM Clients WHERE ClientID = @id", DbHelper.P("@id", clientId));
                if (dt.Rows.Count > 0 && dt.Rows[0]["Address"] != DBNull.Value)
                {
                    txtCustomerAddress.Text = dt.Rows[0]["Address"]?.ToString() ?? "";
                }
            }
            catch { }
        }

        private void OpenClientPurchasesSearch()
        {
            // If client is not specified yet, prompt user to select a registered client first
            if (!_selectedClientID.HasValue && string.IsNullOrWhiteSpace(txtCustomerPhone.Text) && string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                var res = MessageBox.Show(
                    "لم تقم بتحديد عميل بعد لعرض سجل مشترياته.\n\nهل ترغب في اختيار عميل مسجل الآن؟",
                    "اختيار عميل لعرض المشتريات",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    OpenClientSearch();
                    if (_selectedClientID.HasValue && _selectedClientID.Value > 0)
                    {
                        OpenClientPurchasesSearch();
                    }
                }
                return;
            }

            // Try fetching client purchased items from previous invoices
            var dtPurchases = WarrantyDAL.GetClientPurchasedItems(_selectedClientID, txtCustomerPhone.Text, txtCustomerName.Text);
            if (dtPurchases != null && dtPurchases.Rows.Count > 0)
            {
                using (var dlg = new FrmSelectClientDeviceDialog(dtPurchases, txtCustomerName.Text, txtCustomerPhone.Text))
                {
                    var dr = dlg.ShowDialog();
                    if (dr == DialogResult.OK && dlg.SelectedRow != null)
                    {
                        ApplyPurchasedItem(dlg.SelectedRow);
                        return;
                    }
                    else if (dr == DialogResult.Retry || dlg.SwitchedToCatalogSearch)
                    {
                        OpenGeneralProductSearch();
                        return;
                    }
                }
            }
            else
            {
                // No previous purchases found for this client
                var res = MessageBox.Show(
                    "لم يتم العثور على أجهزة مسجلة في فواتير سابقة لهذا العميل.\nهل تريد فتح دليل كل الأصناف لاختيار الصنف (بدون فاتورة)؟",
                    "لا توجد مشتريات سابقة",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (res == DialogResult.Yes)
                {
                    OpenGeneralProductSearch();
                }
            }
        }

        private void OpenGeneralProductSearch()
        {
            try
            {
                using (var frm = new FrmProductSearch(allowZeroStockSelection: true, defaultShowZeroStock: true))
                {
                    if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                    {
                        _selectedProductID = frm.SelectedProductID;

                        // Use selected product details directly from the search dialog
                        if (!string.IsNullOrWhiteSpace(frm.SelectedProductName))
                        {
                            txtProductName.Text = frm.SelectedProductName;
                        }
                        if (!string.IsNullOrWhiteSpace(frm.SelectedBrand))
                        {
                            txtBrand.Text = frm.SelectedBrand;
                        }

                        // Safe fallback query if product name or brand is not yet populated
                        if (string.IsNullOrWhiteSpace(txtProductName.Text) || string.IsNullOrWhiteSpace(txtBrand.Text))
                        {
                            try
                            {
                                string sql = @"
                                    SELECT ProductID, ProductName, 
                                           CASE WHEN COL_LENGTH('Products', 'Brand') IS NOT NULL THEN ISNULL(Brand, N'') ELSE N'' END AS Brand 
                                    FROM Products 
                                    WHERE ProductID = @id";
                                var dt = DbHelper.Query(sql, DbHelper.P("@id", frm.SelectedProductID));

                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    var r = dt.Rows[0];
                                    if (string.IsNullOrWhiteSpace(txtProductName.Text))
                                        txtProductName.Text = r["ProductName"]?.ToString() ?? "";
                                    if (string.IsNullOrWhiteSpace(txtBrand.Text))
                                    {
                                        string brand = r["Brand"]?.ToString() ?? "";
                                        if (!string.IsNullOrEmpty(brand)) txtBrand.Text = brand;
                                    }
                                }
                            }
                            catch (Exception dbEx)
                            {
                                AppLogger.Error("FrmWarrantyCard.OpenGeneralProductSearch.DbFallback", dbEx);
                            }
                        }

                        txtSerialNumber?.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmWarrantyCard.OpenGeneralProductSearch", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة البحث: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyPurchasedItem(DataRow r)
        {
            if (r == null) return;

            if (r.Table.Columns.Contains("ProductID") && r["ProductID"] != DBNull.Value)
            {
                int.TryParse(r["ProductID"].ToString(), out int pid);
                if (pid > 0) _selectedProductID = pid;
            }

            if (r.Table.Columns.Contains("ProductName") && r["ProductName"] != DBNull.Value)
                txtProductName.Text = r["ProductName"].ToString();

            if (r.Table.Columns.Contains("Brand") && r["Brand"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["Brand"].ToString()))
                txtBrand.Text = r["Brand"].ToString();

            if (r.Table.Columns.Contains("IMEI") && r["IMEI"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["IMEI"].ToString()))
                txtSerialNumber.Text = r["IMEI"].ToString();

            if (r.Table.Columns.Contains("SaleCode") && r["SaleCode"] != DBNull.Value)
                txtInvoiceNumber.Text = r["SaleCode"].ToString();

            if (r.Table.Columns.Contains("SaleDate") && r["SaleDate"] != DBNull.Value)
            {
                chkHasInvoiceDate.Checked = true;
                dtpInvoiceDate.Value = Convert.ToDateTime(r["SaleDate"]);
                dtpInvoiceDate.Enabled = true;
            }

            if (r.Table.Columns.Contains("ClientName") && r["ClientName"] != DBNull.Value && string.IsNullOrWhiteSpace(txtCustomerName.Text))
                txtCustomerName.Text = r["ClientName"].ToString();

            if (r.Table.Columns.Contains("Phone") && r["Phone"] != DBNull.Value && string.IsNullOrWhiteSpace(txtCustomerPhone.Text))
                txtCustomerPhone.Text = r["Phone"].ToString();
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
                    // فحص العملاء المسجلين بالاسم أو الهاتف أو الكود
                    var dtClients = DbHelper.Query(@"
                        SELECT TOP 10 ClientID, ClientName, Phone, Address 
                        FROM Clients 
                        WHERE IsActive = 1 AND (ClientName LIKE @q OR Phone LIKE @q OR ClientCode = @exact)",
                        DbHelper.P("@q", "%" + q + "%"), DbHelper.P("@exact", q));

                    if (dtClients.Rows.Count == 1)
                    {
                        var cr = dtClients.Rows[0];
                        ApplyClientSelection(Convert.ToInt32(cr["ClientID"]), cr["ClientName"]?.ToString() ?? "", cr["Phone"]?.ToString() ?? "");
                        MessageBox.Show("تم العثور على العميل المسجل وجلب بياناته بنجاح!", "تم العثور على العميل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else if (dtClients.Rows.Count > 1)
                    {
                        using (var frm = new FrmClientSearch())
                        {
                            if (frm.ShowDialog() == DialogResult.OK && frm.SelectedClientID > 0)
                            {
                                ApplyClientSelection(frm.SelectedClientID, frm.SelectedClientName, frm.SelectedClientPhone);
                            }
                        }
                        return;
                    }

                    MessageBox.Show("لم يتم العثور على فواتير أو عملاء مسجلين مطابقين لنص البحث.", "بحث سريع", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        dlg.Text = "اختر الصنف / الفاتورة المطابقة";
                        dlg.Size = new Size(680, 380);
                        dlg.StartPosition = FormStartPosition.CenterParent;
                        dlg.RightToLeft = RightToLeft.Yes;
                        dlg.RightToLeftLayout = false;
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

                        var btnChoose = Theme.MakeButton("اختيار الصنف المكتمل", Theme.Success);
                        btnChoose.Dock = DockStyle.Bottom;
                        btnChoose.Height = 38;
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

            MessageBox.Show("تم جلب بيانات العميل والفاتورة والصنف بنجاح!", "تم الجلب", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _selectedClientID = t.ClientID;
            _selectedProductID = t.ProductID;
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

        private WarrantyTicket BuildTicketFromUI()
        {
            return new WarrantyTicket
            {
                TicketID = _ticketID,
                TicketCode = lblTicketCode.Text,
                ReceiveDate = dtpReceiveDate.Value,
                ClientID = _selectedClientID,
                CustomerName = txtCustomerName.Text.Trim(),
                CustomerPhone = txtCustomerPhone.Text.Trim(),
                CustomerAddress = txtCustomerAddress.Text.Trim(),
                ProductID = _selectedProductID,
                ProductName = txtProductName.Text.Trim(),
                Brand = txtBrand.Text.Trim(),
                SerialNumber = txtSerialNumber.Text.Trim(),
                InvoiceNumber = txtInvoiceNumber.Text.Trim(),
                InvoiceDate = chkHasInvoiceDate.Checked ? (DateTime?)dtpInvoiceDate.Value : null,
                ProblemDescription = txtProblemDescription.Text.Trim(),
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
        }

        private void SaveTicket(bool printAfterSave, bool sendWhatsAppAfterSave = false)
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
                var t = BuildTicketFromUI();

                int savedID = WarrantyDAL.Save(t);
                if (savedID > 0)
                {
                    t.TicketID = savedID;
                    SavedTicketID = savedID;
                    ShouldPrintReceipt = printAfterSave;
                    ShouldSendWhatsApp = sendWhatsAppAfterSave;

                    if (printAfterSave)
                    {
                        WarrantyReceiptHelper.PrintReceipt(t, showPreview: true);
                    }
                    else if (sendWhatsAppAfterSave)
                    {
                        WarrantyReceiptHelper.SendWhatsApp(this, t);
                    }
                    else if (_ticketID == 0)
                    {
                        // New ticket - ask for print
                        var ask = MessageBox.Show("تم استلام وحفظ كرت الضمان بنجاح!\n\nهل ترغب في طباعة إيصال الاستلام الآن للعميل؟", "طباعة إيصال الضمان", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (ask == DialogResult.Yes)
                        {
                            WarrantyReceiptHelper.PrintReceipt(t, showPreview: true);
                        }
                    }

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

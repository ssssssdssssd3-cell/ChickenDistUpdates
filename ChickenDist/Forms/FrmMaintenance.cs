using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmMaintenance : Form
    {
        private DataGridView dgTickets;
        private TextBox txtSearch;
        private ComboBox cboStatusFilter;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnPrint;
        private DataTable _allTicketsDt;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private TableLayoutPanel pnlStats;
        private Label lblValTotal;
        private Label lblValRepair;
        private Label lblValReady;
        private Label lblValRevenue;
        private string _barcodeBuffer = "";
        private DateTime _lastKeystroke = DateTime.MinValue;

        public FrmMaintenance()
        {
            InitUI();
            LoadTickets();
        }

        private void InitUI()
        {
            this.Text = "صيانة الأجهزة والهواتف";
            this.Size = new Size(1100, 650);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmMaintenance_KeyDown;

            // ── الهيكل الرئيسي للشاشة ──
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Theme.BgMain
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));  // Row 0: filterPanel
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Row 1: contentLayout
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // Row 2: pnlStats

            TableLayoutPanel contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // Column 0: dgTickets
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));  // Column 1: actionPanel
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ── شريط الفلتر العلوي ──
            FlowLayoutPanel filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10)
            };

            Label lblSearch = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtSearch = new TextBox { Width = 150, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.TextChanged += (s, e) => FilterData();
            filterPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch });

            Label lblStatus = new Label { Text = "الحالة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            cboStatusFilter = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextDark };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "قيد الإصلاح", "تم الإصلاح - جاهز", "تم التسليم", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => FilterData();
            filterPanel.Controls.AddRange(new Control[] { lblStatus, cboStatusFilter });

            Label lblFrom = new Label { Text = "من تاريخ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            dtpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            dtpFrom.Value = DateTime.Today.AddDays(-365);
            dtpFrom.ValueChanged += (s, e) => FilterData();
            filterPanel.Controls.AddRange(new Control[] { lblFrom, dtpFrom });

            Label lblTo = new Label { Text = "إلى تاريخ:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            dtpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            dtpTo.Value = DateTime.Today;
            dtpTo.ValueChanged += (s, e) => FilterData();
            filterPanel.Controls.AddRange(new Control[] { lblTo, dtpTo });

            mainLayout.Controls.Add(filterPanel, 0, 0);

            // ── شريط العمليات الجانبي ──
            FlowLayoutPanel actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 15, 10, 15),
                FlowDirection = FlowDirection.TopDown
            };

            btnAdd = Theme.MakeButton("➕ إضافة تذكرة صيانة", Color.FromArgb(40, 150, 80));
            btnAdd.Size = new Size(160, 36);
            btnAdd.Margin = new Padding(0, 0, 0, 10);
            btnAdd.Click += (s, e) => { if (new FrmMaintenanceCard().ShowDialog() == DialogResult.OK) LoadTickets(); };
            actionPanel.Controls.Add(btnAdd);

            btnEdit = Theme.MakeButton("📝 تعديل التذكرة", Theme.Accent);
            btnEdit.Size = new Size(160, 36);
            btnEdit.Margin = new Padding(0, 0, 0, 10);
            btnEdit.Click += BtnEdit_Click;
            actionPanel.Controls.Add(btnEdit);

            btnDelete = Theme.MakeButton("🗑️ حذف التذكرة", Theme.Danger);
            btnDelete.Size = new Size(160, 36);
            btnDelete.Margin = new Padding(0, 0, 0, 10);
            btnDelete.Click += BtnDelete_Click;
            actionPanel.Controls.Add(btnDelete);

            btnPrint = Theme.MakeButton("🖨️ طباعة إيصال صيانة", Color.FromArgb(30, 80, 180));
            btnPrint.Size = new Size(160, 36);
            btnPrint.Margin = new Padding(0, 0, 0, 10);
            btnPrint.Click += BtnPrint_Click;
            actionPanel.Controls.Add(btnPrint);

            contentLayout.Controls.Add(actionPanel, 1, 0);

            // ── لوحة الإحصائيات السفلية ──
            pnlStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 4,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 5, 10, 5),
                RightToLeft = RightToLeft.Yes
            };
            pnlStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            pnlStats.Controls.Add(MakeStatLabel("🔧 إجمالي التذاكر", "0", Theme.TextMain, out lblValTotal), 0, 0);
            pnlStats.Controls.Add(MakeStatLabel("⏳ قيد الإصلاح", "0", Color.Orange, out lblValRepair), 1, 0);
            pnlStats.Controls.Add(MakeStatLabel("✅ جاهز للتسليم", "0", Color.LimeGreen, out lblValReady), 2, 0);
            pnlStats.Controls.Add(MakeStatLabel("💵 إجمالي إيرادات الصيانة (المسلمة)", "0.00 ج", Theme.Success, out lblValRevenue), 3, 0);

            mainLayout.Controls.Add(pnlStats, 0, 2);

            // ── جدول عرض البيانات ──
            dgTickets = new DataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgTickets.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.BgCard,
                ForeColor = Theme.TextMain,
                SelectionBackColor = Theme.Primary,
                SelectionForeColor = Color.White,
                Font = Theme.FontNormal
            };
            dgTickets.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = Theme.FontBold,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgTickets.EnableHeadersVisualStyles = false;

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "TicketID", HeaderText = "رقم التذكرة", FillWeight = 40 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "اسم العميل", FillWeight = 110 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerPhone", HeaderText = "رقم الهاتف", FillWeight = 80 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "DeviceModel", HeaderText = "الجهاز/الموديل", FillWeight = 100 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "DeviceSerial", HeaderText = "الرقم التسلسلي/IMEI", FillWeight = 90 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Problem", HeaderText = "المشكلة", FillWeight = 130 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "PartsCost", HeaderText = "قطع الغيار (ج)", FillWeight = 60 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "LaborCost", HeaderText = "أجرة اليد (ج)", FillWeight = 60 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cost", HeaderText = "الإجمالي (ج)", FillWeight = 60 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 70 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarrantyPeriod", HeaderText = "الضمان", FillWeight = 80 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 110 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "التاريخ", FillWeight = 90 });

            contentLayout.Controls.Add(dgTickets, 0, 0);
            mainLayout.Controls.Add(contentLayout, 0, 1);
            this.Controls.Add(mainLayout);
        }

        private void LoadTickets()
        {
            try
            {
                _allTicketsDt = DbHelper.Query("SELECT * FROM MaintenanceTickets ORDER BY TicketID DESC");
                FilterData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في قراءة البيانات: " + ex.Message);
            }
        }

        private void FilterData()
        {
            dgTickets.Rows.Clear();
            if (lblValTotal == null) return; // UI not fully initialized yet

            if (_allTicketsDt == null || _allTicketsDt.Rows.Count == 0)
            {
                lblValTotal.Text = "0";
                lblValRepair.Text = "0";
                lblValReady.Text = "0";
                lblValRevenue.Text = "0.00 ج";
                return;
            }

            string query = txtSearch.Text.Trim().ToLower();
            string statusFilter = cboStatusFilter.SelectedItem?.ToString() ?? "الكل";

            int totalTickets = 0;
            int repairTickets = 0;
            int readyTickets = 0;
            decimal totalRevenue = 0m;

            DateTime fromDate = dtpFrom.Value.Date;
            DateTime toDate = dtpTo.Value.Date.AddDays(1);

            foreach (DataRow r in _allTicketsDt.Rows)
            {
                string id = r["TicketID"].ToString();
                string name = r["CustomerName"]?.ToString() ?? "";
                string phone = r["CustomerPhone"]?.ToString() ?? "";
                string model = r["DeviceModel"]?.ToString() ?? "";
                string serial = r["DeviceSerial"]?.ToString() ?? "";
                string problem = r["Problem"]?.ToString() ?? "";
                decimal costAmt = Convert.ToDecimal(r["Cost"]);
                string cost = costAmt.ToString("N2");
                decimal partsCostAmt = r.Table.Columns.Contains("PartsCost") && r["PartsCost"] != DBNull.Value ? Convert.ToDecimal(r["PartsCost"]) : 0m;
                decimal laborCostAmt = r.Table.Columns.Contains("LaborCost") && r["LaborCost"] != DBNull.Value ? Convert.ToDecimal(r["LaborCost"]) : 0m;
                string partsCost = partsCostAmt.ToString("N2");
                string laborCost = laborCostAmt.ToString("N2");
                string status = r["Status"]?.ToString() ?? "قيد الإصلاح";
                string warranty = r.Table.Columns.Contains("WarrantyPeriod") && r["WarrantyPeriod"] != DBNull.Value ? r["WarrantyPeriod"].ToString() : "بدون ضمان";
                string notes = r["Notes"]?.ToString() ?? "";
                DateTime ticketDate = Convert.ToDateTime(r["CreatedAt"]);
                string date = ticketDate.ToString("dd/MM/yyyy HH:mm");

                // Filter by Date Range
                if (ticketDate < fromDate || ticketDate >= toDate) continue;

                // Filter by Status
                if (statusFilter != "الكل" && status != statusFilter) continue;

                // Filter by Query
                if (!string.IsNullOrEmpty(query))
                {
                    if (!id.Contains(query) && !name.ToLower().Contains(query) && !phone.Contains(query) && !serial.ToLower().Contains(query) && !model.ToLower().Contains(query))
                        continue;
                }

                // Add to Grid
                dgTickets.Rows.Add(id, name, phone, model, serial, problem, partsCost, laborCost, cost, status, warranty, notes, date);

                // Add to Stats
                totalTickets++;
                if (status == "قيد الإصلاح") repairTickets++;
                else if (status == "تم الإصلاح - جاهز") readyTickets++;
                else if (status == "تم التسليم") totalRevenue += costAmt;
            }

            // Update Labels
            lblValTotal.Text = totalTickets.ToString();
            lblValRepair.Text = repairTickets.ToString();
            lblValReady.Text = readyTickets.ToString();
            lblValRevenue.Text = totalRevenue.ToString("N2") + " ج";
        }

        private Panel MakeStatLabel(string title, string value, Color valueColor, out Label valLabel)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Margin = new Padding(5), BackColor = Color.FromArgb(45, 55, 72) };
            var lblTitle = new Label 
            { 
                Text = title, 
                Dock = DockStyle.Top, 
                Height = 22, 
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), 
                ForeColor = Color.FromArgb(200, 200, 200),
                TextAlign = ContentAlignment.MiddleCenter
            };
            valLabel = new Label 
            { 
                Text = value, 
                Dock = DockStyle.Fill, 
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), 
                ForeColor = valueColor,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnl.Controls.Add(valLabel);
            pnl.Controls.Add(lblTitle);
            return pnl;
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار تذكرة صيانة لتعديلها");
                return;
            }
            int ticketID = Convert.ToInt32(dgTickets.SelectedRows[0].Cells["TicketID"].Value);
            var frm = new FrmMaintenanceCard(ticketID);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadTickets();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار تذكرة صيانة لحذفها");
                return;
            }
            int ticketID = Convert.ToInt32(dgTickets.SelectedRows[0].Cells["TicketID"].Value);
            if (MessageBox.Show("⚠️ هل أنت متأكد من حذف هذه التذكرة نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    DbHelper.Execute("DELETE FROM MaintenanceTickets WHERE TicketID = @tid", DbHelper.P("@tid", ticketID));
                    MessageBox.Show("✅ تم حذف التذكرة بنجاح");
                    LoadTickets();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل عملية الحذف: " + ex.Message);
                }
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار تذكرة صيانة لطباعتها");
                return;
            }

            // طباعة التذكرة
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (s, ev) =>
            {
                var row = dgTickets.SelectedRows[0];
                string company = AppConfig.CompanyName;
                string ticketID = row.Cells["TicketID"].Value.ToString();
                string name = row.Cells["CustomerName"].Value.ToString();
                string phone = row.Cells["CustomerPhone"].Value.ToString();
                string model = row.Cells["DeviceModel"].Value.ToString();
                string serial = row.Cells["DeviceSerial"].Value.ToString();
                string problem = row.Cells["Problem"].Value.ToString();
                string cost = row.Cells["Cost"].Value.ToString();
                string partsCost = row.Cells["PartsCost"].Value?.ToString() ?? "0.00";
                string laborCost = row.Cells["LaborCost"].Value?.ToString() ?? "0.00";
                string warranty = row.Cells["WarrantyPeriod"].Value?.ToString() ?? "بدون ضمان";
                string notes = row.Cells["Notes"].Value.ToString();
                string date = row.Cells["CreatedAt"].Value.ToString();

                int y = 20;
                Font fontTitle = new Font("Segoe UI", 14f, FontStyle.Bold);
                Font fontLabel = new Font("Segoe UI", 10f, FontStyle.Bold);
                Font fontVal = new Font("Segoe UI", 10f);

                ev.Graphics.DrawString(company, fontTitle, Brushes.Black, new PointF(100, y)); y += 40;
                ev.Graphics.DrawString("🧾 إيصال استلام صيانة جهاز", fontLabel, Brushes.Black, new PointF(80, y)); y += 30;
                ev.Graphics.DrawLine(Pens.Black, 20, y, 280, y); y += 15;

                ev.Graphics.DrawString($"رقم التذكرة: {ticketID}", fontLabel, Brushes.Black, new PointF(20, y)); y += 22;
                ev.Graphics.DrawString($"التاريخ: {date}", fontVal, Brushes.Black, new PointF(20, y)); y += 22;
                ev.Graphics.DrawString($"العميل: {name}", fontLabel, Brushes.Black, new PointF(20, y)); y += 22;
                if (!string.IsNullOrEmpty(phone)) { ev.Graphics.DrawString($"الهاتف: {phone}", fontVal, Brushes.Black, new PointF(20, y)); y += 22; }
                ev.Graphics.DrawLine(Pens.LightGray, 20, y, 280, y); y += 15;

                ev.Graphics.DrawString($"الجهاز: {model}", fontLabel, Brushes.Black, new PointF(20, y)); y += 22;
                if (!string.IsNullOrEmpty(serial)) { ev.Graphics.DrawString($"IMEI/Serial: {serial}", fontVal, Brushes.Black, new PointF(20, y)); y += 22; }
                ev.Graphics.DrawString($"المشكلة: {problem}", fontVal, Brushes.Black, new PointF(20, y)); y += 25;
                
                decimal.TryParse(partsCost, out decimal pc);
                decimal.TryParse(laborCost, out decimal lc);
                if (pc > 0) { ev.Graphics.DrawString($"قطع الغيار: {partsCost} ج", fontVal, Brushes.Black, new PointF(20, y)); y += 22; }
                if (lc > 0) { ev.Graphics.DrawString($"أجرة اليد: {laborCost} ج", fontVal, Brushes.Black, new PointF(20, y)); y += 22; }
                
                ev.Graphics.DrawString($"إجمالي التكلفة: {cost} ج", fontLabel, Brushes.Black, new PointF(20, y)); y += 25;
                ev.Graphics.DrawString($"مدة الضمان: {warranty}", fontLabel, Brushes.Black, new PointF(20, y)); y += 25;
                
                if (!string.IsNullOrEmpty(notes))
                {
                    ev.Graphics.DrawString($"ملاحظات: {notes}", fontVal, Brushes.Black, new PointF(20, y)); y += 22;
                }
                
                ev.Graphics.DrawLine(Pens.Black, 20, y, 280, y); y += 15;
                ev.Graphics.DrawString("نشكركم لثقتكم بنا!", fontVal, Brushes.Black, new PointF(100, y)); y += 25;

                // Draw Barcode for TicketID
                FrmPrintProductBarcode.DrawCode39(ev.Graphics, ticketID, 30, y, 240, 30); y += 35;
                ev.Graphics.DrawString($"*{ticketID}*", fontVal, Brushes.Black, new PointF(130, y));
            };

            try
            {
                PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
                ppd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تشغيل الطابعة: " + ex.Message);
            }
        }

        private void FrmMaintenance_KeyDown(object sender, KeyEventArgs e)
        {
            // Barcode scanners send keys very rapidly
            TimeSpan elapsed = DateTime.Now - _lastKeystroke;
            if (elapsed.TotalMilliseconds > 100)
            {
                _barcodeBuffer = ""; // Reset buffer if typing slow (manually)
            }

            _lastKeystroke = DateTime.Now;

            if (e.KeyCode == Keys.Enter)
            {
                if (_barcodeBuffer.Length >= 2)
                {
                    string code = _barcodeBuffer.Trim();
                    _barcodeBuffer = "";
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    txtSearch.Text = code;
                    FilterData();

                    if (dgTickets.Rows.Count == 1)
                    {
                        dgTickets.Rows[0].Selected = true;
                    }
                }
            }
            else
            {
                char c = (char)e.KeyValue;
                if (char.IsLetterOrDigit(c) || c == '-')
                {
                    _barcodeBuffer += c;
                }
            }
        }
    }
}

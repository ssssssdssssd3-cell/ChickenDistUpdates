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

        public FrmMaintenance()
        {
            InitUI();
            LoadTickets();
        }

        private void InitUI()
        {
            this.Text = "صيانة الأجهزة والهواتف";
            this.Size = new Size(1000, 600);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── شريط الفلتر العلوي ──
            FlowLayoutPanel filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 10, 10, 10)
            };

            Label lblSearch = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            txtSearch = new TextBox { Width = 180, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.TextChanged += (s, e) => FilterData();
            filterPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch });

            Label lblStatus = new Label { Text = "الحالة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            cboStatusFilter = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextDark };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "قيد الإصلاح", "تم الإصلاح - جاهز", "تم التسليم", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => FilterData();
            filterPanel.Controls.AddRange(new Control[] { lblStatus, cboStatusFilter });

            this.Controls.Add(filterPanel);

            // ── شريط العمليات الجانبي ──
            FlowLayoutPanel actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 180,
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

            this.Controls.Add(actionPanel);

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
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cost", HeaderText = "التكلفة (ج)", FillWeight = 60 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 70 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 110 });
            dgTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "التاريخ", FillWeight = 90 });

            this.Controls.Add(dgTickets);
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
            if (_allTicketsDt == null || _allTicketsDt.Rows.Count == 0) return;

            string query = txtSearch.Text.Trim().ToLower();
            string statusFilter = cboStatusFilter.SelectedItem?.ToString() ?? "الكل";

            foreach (DataRow r in _allTicketsDt.Rows)
            {
                string id = r["TicketID"].ToString();
                string name = r["CustomerName"]?.ToString() ?? "";
                string phone = r["CustomerPhone"]?.ToString() ?? "";
                string model = r["DeviceModel"]?.ToString() ?? "";
                string serial = r["DeviceSerial"]?.ToString() ?? "";
                string problem = r["Problem"]?.ToString() ?? "";
                string cost = Convert.ToDecimal(r["Cost"]).ToString("N2");
                string status = r["Status"]?.ToString() ?? "قيد الإصلاح";
                string notes = r["Notes"]?.ToString() ?? "";
                string date = Convert.ToDateTime(r["CreatedAt"]).ToString("dd/MM/yyyy HH:mm");

                if (statusFilter != "الكل" && status != statusFilter) continue;
                if (!string.IsNullOrEmpty(query))
                {
                    if (!name.ToLower().Contains(query) && !phone.Contains(query) && !serial.ToLower().Contains(query) && !model.ToLower().Contains(query))
                        continue;
                }

                dgTickets.Rows.Add(id, name, phone, model, serial, problem, cost, status, notes, date);
            }
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
                ev.Graphics.DrawString($"التكلفة المقدرة: {cost} ج", fontLabel, Brushes.Black, new PointF(20, y)); y += 25;
                
                if (!string.IsNullOrEmpty(notes))
                {
                    ev.Graphics.DrawString($"ملاحظات: {notes}", fontVal, Brushes.Black, new PointF(20, y)); y += 22;
                }
                
                ev.Graphics.DrawLine(Pens.Black, 20, y, 280, y); y += 15;
                ev.Graphics.DrawString("نشكركم لثقتكم بنا!", fontVal, Brushes.Black, new PointF(100, y));
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
    }
}

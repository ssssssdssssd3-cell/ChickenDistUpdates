using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public partial class FrmWarranty : Form
    {
        private DataGridView dgTickets;
        private TextBox txtSearch;
        private ComboBox cboStatusFilter;
        private ComboBox cboResolutionFilter;
        private Label lblTotalCount;
        private Label lblReceivedCount;
        private Label lblVendorCount;
        private Label lblReadyCount;
        private Label lblDeliveredCount;
        private DataTable dtTickets;

        public FrmWarranty()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "إدارة الضمان وخدمة ما بعد البيع";
            this.Size = new Size(1250, 750);
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // ==========================================
            // 1. TOP HEADER (48px)
            // ==========================================
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(24, 43, 73),
                Padding = new Padding(15, 0, 15, 0)
            };

            Label lblTitle = new Label
            {
                Text = "🛡️ سجل أجهزة الضمان وخدمة ما بعد البيع (استلام / تسليم / صيانة)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitle);

            // ==========================================
            // 2. STATS CARDS (80px)
            // ==========================================
            Panel pnlStatsWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(12, 8, 12, 6)
            };

            TableLayoutPanel tblStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            tblStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tblStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

            lblTotalCount = new Label();
            lblReceivedCount = new Label();
            lblVendorCount = new Label();
            lblReadyCount = new Label();
            lblDeliveredCount = new Label();

            tblStats.Controls.Add(CreateStatCard("إجمالي الإيصالات", lblTotalCount, Color.FromArgb(52, 73, 94)), 0, 0);
            tblStats.Controls.Add(CreateStatCard("مستلم بالمحل", lblReceivedCount, Color.FromArgb(41, 128, 185)), 1, 0);
            tblStats.Controls.Add(CreateStatCard("مرسل للتوكيل / صيانة", lblVendorCount, Color.FromArgb(230, 126, 34)), 2, 0);
            tblStats.Controls.Add(CreateStatCard("جاهز للتسليم", lblReadyCount, Color.FromArgb(39, 174, 96)), 3, 0);
            tblStats.Controls.Add(CreateStatCard("تم تسليمه للعميل", lblDeliveredCount, Color.FromArgb(127, 140, 141)), 4, 0);

            pnlStatsWrap.Controls.Add(tblStats);

            // ==========================================
            // 3. SEARCH & FILTERS (48px)
            // ==========================================
            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8)
            };

            FlowLayoutPanel flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = false
            };

            Label lblSearch = new Label { Text = "🔍 بحث سريع:", AutoSize = true, Margin = new Padding(0, 6, 8, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtSearch = new TextBox { Width = 210, Height = 26, Margin = new Padding(0, 3, 5, 0) };
            txtSearch.TextChanged += (s, e) => ApplyClientFilters();

            Button btnSearchClient = new Button
            {
                Text = "👤 اختيار عميل",
                Width = 110,
                Height = 28,
                BackColor = Color.FromArgb(236, 240, 241),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 15, 0)
            };
            btnSearchClient.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
            btnSearchClient.Click += (s, e) =>
            {
                using (var frm = new FrmClientSearch())
                {
                    if (frm.ShowDialog() == DialogResult.OK && frm.SelectedClientID > 0)
                    {
                        txtSearch.Text = frm.SelectedClientName;
                    }
                }
            };

            Label lblStatus = new Label { Text = "الحالة:", AutoSize = true, Margin = new Padding(0, 6, 6, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cboStatusFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Margin = new Padding(0, 3, 15, 0) };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "مستلم بالمحل", "مرسل للتوكيل", "مستلم من التوكيل (جاهز)", "تم التسليم للزبون", "مرفوض الضمان" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => ApplyClientFilters();

            Label lblRes = new Label { Text = "النتيجة:", AutoSize = true, Margin = new Padding(0, 6, 6, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cboResolutionFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Margin = new Padding(0, 3, 15, 0) };
            cboResolutionFilter.Items.AddRange(new object[] { "الكل", "قيد الفحص", "تم الإصلاح بنجاح", "تم استبدال جهاز جديد", "تم استبدال قطعة غيار", "مرفوض - سوء استخدام", "مرفوض - خارج فترة الضمان", "لا يمكن إصلاحه" });
            cboResolutionFilter.SelectedIndex = 0;
            cboResolutionFilter.SelectedIndexChanged += (s, e) => ApplyClientFilters();

            Button btnRefresh = new Button
            {
                Text = "🔄 تحديث",
                Width = 90,
                Height = 28,
                BackColor = Color.FromArgb(236, 240, 241),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 0)
            };
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
            btnRefresh.Click += (s, e) => LoadTickets();

            flowFilters.Controls.Add(lblSearch);
            flowFilters.Controls.Add(txtSearch);
            flowFilters.Controls.Add(btnSearchClient);
            flowFilters.Controls.Add(lblStatus);
            flowFilters.Controls.Add(cboStatusFilter);
            flowFilters.Controls.Add(lblRes);
            flowFilters.Controls.Add(cboResolutionFilter);
            flowFilters.Controls.Add(btnRefresh);

            pnlFilter.Controls.Add(flowFilters);

            // ==========================================
            // 4. MAIN BODY (Grid + Actions Sidebar)
            // ==========================================
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 10, 12, 12)
            };

            // Left Action Sidebar (RightToLeft layout makes Left dock on the left side)
            Panel pnlActions = new Panel
            {
                Dock = DockStyle.Left,
                Width = 185,
                Padding = new Padding(0, 0, 10, 0),
                BackColor = Color.Transparent
            };

            Button btnNew = CreateActionButton("➕ استلام جهاز جديد", Color.FromArgb(39, 174, 96), Color.White);
            btnNew.Click += (s, e) => OpenTicketEditor(0);

            Button btnEdit = CreateActionButton("✏️ تعديل / تفاصيل", Color.FromArgb(41, 128, 185), Color.White);
            btnEdit.Click += (s, e) => EditSelectedTicket();

            Button btnSendVendor = CreateActionButton("🚚 إرسال للتوكيل", Color.FromArgb(230, 126, 34), Color.White);
            btnSendVendor.Click += (s, e) => QuickSendToVendor();

            Button btnReceiveVendor = CreateActionButton("📦 استلام من التوكيل", Color.FromArgb(142, 68, 173), Color.White);
            btnReceiveVendor.Click += (s, e) => QuickReceiveFromVendor();

            Button btnDeliverCustomer = CreateActionButton("🤝 تسليم للعميل", Color.FromArgb(22, 160, 133), Color.White);
            btnDeliverCustomer.Click += (s, e) => QuickDeliverCustomer();

            Button btnPrint = CreateActionButton("🖨️ طباعة إيصال استلام", Color.FromArgb(52, 73, 94), Color.White);
            btnPrint.Click += (s, e) => PrintReceipt();

            Button btnDelete = CreateActionButton("🗑️ حذف الإيصال", Color.FromArgb(192, 57, 43), Color.White);
            btnDelete.Click += (s, e) => DeleteTicket();

            // Stack buttons nicely inside pnlActions
            btnDelete.Dock = DockStyle.Top;
            btnPrint.Dock = DockStyle.Top;
            btnDeliverCustomer.Dock = DockStyle.Top;
            btnReceiveVendor.Dock = DockStyle.Top;
            btnSendVendor.Dock = DockStyle.Top;
            btnEdit.Dock = DockStyle.Top;
            btnNew.Dock = DockStyle.Top;

            pnlActions.Controls.Add(btnDelete);
            pnlActions.Controls.Add(btnPrint);
            pnlActions.Controls.Add(btnDeliverCustomer);
            pnlActions.Controls.Add(btnReceiveVendor);
            pnlActions.Controls.Add(btnSendVendor);
            pnlActions.Controls.Add(btnEdit);
            pnlActions.Controls.Add(btnNew);

            // Grid Container
            Panel pnlGridBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };

            dgTickets = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };

            SetupGridColumns();
            Theme.ApplyGridTheme(dgTickets);
            dgTickets.DoubleClick += (s, e) => EditSelectedTicket();

            pnlGridBox.Controls.Add(dgTickets);

            pnlBody.Controls.Add(pnlGridBox);
            pnlBody.Controls.Add(pnlActions);

            // Add all main docked panels in reverse dock order
            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlStatsWrap);
            this.Controls.Add(pnlHeader);

            this.Load += (s, e) => LoadTickets();
            this.ResumeLayout(false);
        }

        private Panel CreateStatCard(string title, Label lblValue, Color accentColor)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(4, 0, 4, 0)
            };

            Panel bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 3,
                BackColor = accentColor
            };

            lblValue.Text = "0";
            lblValue.Dock = DockStyle.Fill;
            lblValue.TextAlign = ContentAlignment.MiddleCenter;
            lblValue.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblValue.ForeColor = accentColor;

            Label lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(120, 130, 140)
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            card.Controls.Add(bar);
            return card;
        }

        private Button CreateActionButton(string text, Color bg, Color fg)
        {
            Button btn = new Button
            {
                Text = text,
                Height = 38,
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 6)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetupGridColumns()
        {
            dgTickets.Columns.Clear();

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TicketID",
                DataPropertyName = "TicketID",
                Visible = false
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TicketCode",
                DataPropertyName = "TicketCode",
                HeaderText = "رقم الإيصال",
                Width = 105
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ReceiveDate",
                DataPropertyName = "ReceiveDate",
                HeaderText = "تاريخ الاستلام",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CustomerName",
                DataPropertyName = "CustomerName",
                HeaderText = "اسم العميل",
                Width = 165
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CustomerPhone",
                DataPropertyName = "CustomerPhone",
                HeaderText = "رقم الهاتف",
                Width = 120
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProductName",
                DataPropertyName = "ProductName",
                HeaderText = "الصنف / الجهاز",
                Width = 180
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Brand",
                DataPropertyName = "Brand",
                HeaderText = "الماركة",
                Width = 95
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SerialNumber",
                DataPropertyName = "SerialNumber",
                HeaderText = "السيريال / IMEI",
                Width = 140
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProblemDescription",
                DataPropertyName = "ProblemDescription",
                HeaderText = "وصف العطل / الشكوى",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                DataPropertyName = "Status",
                HeaderText = "الحالة الحالية",
                Width = 135
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "VendorName",
                DataPropertyName = "VendorName",
                HeaderText = "التوكيل / الورشة",
                Width = 130
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ResolutionType",
                DataPropertyName = "ResolutionType",
                HeaderText = "نتيجة الصيانة",
                Width = 130
            });

            dgTickets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DeliveredToCustomerDate",
                DataPropertyName = "DeliveredToCustomerDate",
                HeaderText = "تاريخ التسليم للعميل",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            dgTickets.CellFormatting += DgTickets_CellFormatting;
        }

        private void DgTickets_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = dgTickets.Columns[e.ColumnIndex].Name;
            if (colName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status == "مستلم بالمحل")
                {
                    e.CellStyle.BackColor = Color.FromArgb(235, 245, 251);
                    e.CellStyle.ForeColor = Color.FromArgb(41, 128, 185);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (status == "مرسل للتوكيل")
                {
                    e.CellStyle.BackColor = Color.FromArgb(254, 249, 231);
                    e.CellStyle.ForeColor = Color.FromArgb(211, 84, 0);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (status == "مستلم من التوكيل (جاهز)" || status.Contains("جاهز"))
                {
                    e.CellStyle.BackColor = Color.FromArgb(234, 250, 241);
                    e.CellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (status == "تم التسليم للزبون")
                {
                    e.CellStyle.BackColor = Color.FromArgb(242, 243, 244);
                    e.CellStyle.ForeColor = Color.FromArgb(127, 140, 141);
                }
                else if (status == "مرفوض الضمان" || status == "مرفوض")
                {
                    e.CellStyle.BackColor = Color.FromArgb(253, 237, 236);
                    e.CellStyle.ForeColor = Color.FromArgb(192, 57, 43);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
            }
        }

        private void LoadTickets()
        {
            try
            {
                dtTickets = WarrantyDAL.GetAll();
                ApplyClientFilters();
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل سجلات الضمان: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStats()
        {
            try
            {
                var stats = WarrantyDAL.GetStats();
                lblTotalCount.Text = stats.TotalCount.ToString();
                lblReceivedCount.Text = stats.AtShopCount.ToString();
                lblVendorCount.Text = stats.AtVendorCount.ToString();
                lblReadyCount.Text = stats.ReadyCount.ToString();
                lblDeliveredCount.Text = stats.DeliveredCount.ToString();
            }
            catch { }
        }

        private void ApplyClientFilters()
        {
            if (dtTickets == null) return;

            string search = txtSearch?.Text?.Trim()?.Replace("'", "''") ?? "";
            string status = cboStatusFilter?.SelectedItem?.ToString() ?? "الكل";
            string resolution = cboResolutionFilter?.SelectedItem?.ToString() ?? "الكل";

            string filter = "1=1";

            if (!string.IsNullOrEmpty(search))
            {
                filter += $" AND (TicketCode LIKE '%{search}%' OR CustomerName LIKE '%{search}%' OR CustomerPhone LIKE '%{search}%' OR ProductName LIKE '%{search}%' OR SerialNumber LIKE '%{search}%' OR Brand LIKE '%{search}%')";
            }

            if (status != "الكل")
            {
                filter += $" AND Status = '{status}'";
            }

            if (resolution != "الكل")
            {
                filter += $" AND ResolutionType = '{resolution}'";
            }

            DataView dv = dtTickets.DefaultView;
            dv.RowFilter = filter;
            dgTickets.DataSource = dv;
        }

        private int GetSelectedTicketID()
        {
            if (dgTickets.CurrentRow == null || dgTickets.CurrentRow.Index < 0)
                return 0;

            object val = dgTickets.CurrentRow.Cells["TicketID"]?.Value;
            if (val != null && int.TryParse(val.ToString(), out int id))
                return id;

            return 0;
        }

        private void OpenTicketEditor(int ticketId)
        {
            using (FrmWarrantyCard frm = new FrmWarrantyCard(ticketId))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadTickets();
                }
            }
        }

        private void EditSelectedTicket()
        {
            int id = GetSelectedTicketID();
            if (id <= 0)
            {
                MessageBox.Show("يرجى اختيار إيصال من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OpenTicketEditor(id);
        }

        private void QuickSendToVendor()
        {
            int id = GetSelectedTicketID();
            if (id <= 0)
            {
                MessageBox.Show("يرجى اختيار إيصال أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string vendor = InputBoxHelper.Show("أدخل اسم التوكيل / مركز الصيانة:", "إرسال للتوكيل", "");
            if (string.IsNullOrWhiteSpace(vendor)) return;

            try
            {
                WarrantyDAL.UpdateStatus(id, "مرسل للتوكيل", vendorName: vendor);
                MessageBox.Show("تم تحديث حالة الجهاز إلى (مرسل للتوكيل) بنجاح.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTickets();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء التحديث: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void QuickReceiveFromVendor()
        {
            int id = GetSelectedTicketID();
            if (id <= 0)
            {
                MessageBox.Show("يرجى اختيار إيصال أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string report = InputBoxHelper.Show("أدخل تقرير التوكيل / نتيجة الإصلاح:", "استلام من التوكيل", "تم الإصلاح بنجاح");
            if (report == null) return;

            try
            {
                WarrantyDAL.UpdateStatus(id, "مستلم من التوكيل (جاهز)", vendorReport: report, resolutionType: "تم الإصلاح بنجاح");
                MessageBox.Show("تم استلام الجهاز بالمحل بنجاح وهو الآن جاهز لتسليمه للعميل.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTickets();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء التحديث: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void QuickDeliverCustomer()
        {
            int id = GetSelectedTicketID();
            if (id <= 0)
            {
                MessageBox.Show("يرجى اختيار إيصال أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("هل تود تأكيد تسليم الجهاز للعميل وإنهاء الإيصال؟", "تأكيد التسليم", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    WarrantyDAL.UpdateStatus(id, "تم التسليم للزبون", deliveredDate: DateTime.Now);
                    MessageBox.Show("تم تسليم الجهاز للعميل وإغلاق الإيصال بنجاح.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTickets();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ أثناء التحديث: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteTicket()
        {
            int id = GetSelectedTicketID();
            if (id <= 0)
            {
                MessageBox.Show("يرجى اختيار إيصال لحذفه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("هل أنت متأكد من حذف هذا الإيصال نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    WarrantyDAL.Delete(id);
                    MessageBox.Show("تم حذف الإيصال بنجاح.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTickets();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ أثناء الحذف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PrintReceipt()
        {
            int id = GetSelectedTicketID();
            if (id <= 0)
            {
                MessageBox.Show("يرجى اختيار إيصال لطباعته.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ticket = WarrantyDAL.GetByID(id);
            if (ticket == null) return;

            string info = $"========================================\n" +
                          $"       إيصال استلام جهاز للضمان والصيانة     \n" +
                          $"========================================\n" +
                          $"رقم الإيصال: {ticket.TicketCode}\n" +
                          $"التاريخ: {ticket.ReceiveDate:yyyy-MM-dd HH:mm}\n" +
                          $"العميل: {ticket.CustomerName}\n" +
                          $"الهاتف: {ticket.CustomerPhone}\n" +
                          $"الجهاز: {ticket.ProductName} - {ticket.Brand}\n" +
                          $"السيريال: {ticket.SerialNumber}\n" +
                          $"العطل: {ticket.ProblemDescription}\n" +
                          $"المشتملات: {ticket.Accessories}\n" +
                          $"الحالة الحالية: {ticket.Status}\n" +
                          $"========================================\n" +
                          $"ملاحظات: {ticket.Notes}\n";

            MessageBox.Show(info, "إيصال استلام جهاز", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

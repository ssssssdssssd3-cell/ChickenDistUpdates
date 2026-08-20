using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmReservations : Form
    {
        private ComboBox cboStatusFilter;
        private TextBox txtSearch;
        private DataGridView dgReservations;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnSetReady;
        private Button btnConvertToSale;
        private Button btnCancelRes;
        private Button btnPrintReceipt;
        private Button btnRefresh;

        public FrmReservations()
        {
            InitializeComponentCustom();
            LoadReservations();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "📋 سجل وإدارة حجوزات الأصناف والطلبيات الخاصة";
            this.Size = new Size(1180, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlTop = Theme.MakeTitleBar("📋 سجل وإدارة حجوزات الأصناف والطلبيات الخاصة", "متابعة حجوزات الأصناف وتعديلها، طباعة الإيصالات، تحويل الحجوزات لمبيعات، وتنبيهات توفير البضائع.");
            this.Controls.Add(pnlTop);

            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgCard,
                RightToLeft = RightToLeft.No,
                Padding = new Padding(10, 10, 10, 10)
            };

            btnAdd = Theme.MakeButton("➕ حجز جديد", 10, 12, 110, 36, Theme.Success);
            btnAdd.Click += (s, e) =>
            {
                using (var dlg = new FrmAddReservation())
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) LoadReservations();
                }
            };
            pnlFilter.Controls.Add(btnAdd);

            btnEdit = Theme.MakeButton("✏️ تعديل الحجز", 125, 12, 115, 36, Color.FromArgb(41, 128, 185));
            btnEdit.Click += BtnEdit_Click;
            pnlFilter.Controls.Add(btnEdit);

            btnPrintReceipt = Theme.MakeButton("🖨️ طباعة الإيصال", 245, 12, 130, 36, Color.FromArgb(142, 68, 173));
            btnPrintReceipt.Click += BtnPrintReceipt_Click;
            pnlFilter.Controls.Add(btnPrintReceipt);

            btnSetReady = Theme.MakeButton("🟢 جاهز للتسليم", 380, 12, 125, 36, Color.FromArgb(46, 204, 113));
            btnSetReady.Click += BtnSetReady_Click;
            pnlFilter.Controls.Add(btnSetReady);

            btnConvertToSale = Theme.MakeButton("🛒 تحويل لفاتورة مبيعات", 510, 12, 160, 36, Theme.Primary);
            btnConvertToSale.Click += BtnConvertToSale_Click;
            pnlFilter.Controls.Add(btnConvertToSale);

            btnCancelRes = Theme.MakeButton("❌ إلغاء", 675, 12, 85, 36, Theme.Danger);
            btnCancelRes.Click += BtnCancelRes_Click;
            pnlFilter.Controls.Add(btnCancelRes);

            var lblStatus = new Label { Text = "الحالة:", Location = new Point(765, 20), AutoSize = true, ForeColor = Theme.TextMain };
            pnlFilter.Controls.Add(lblStatus);

            cboStatusFilter = new ComboBox
            {
                Location = new Point(810, 16),
                Width = 115,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "قيد الانتظار", "جاهز للتسليم", "تم التسليم", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => LoadReservations();
            pnlFilter.Controls.Add(cboStatusFilter);

            var lblSearch = new Label { Text = "🔍 بحث:", Location = new Point(930, 20), AutoSize = true, ForeColor = Theme.TextMain };
            pnlFilter.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location = new Point(785, 16),
                Width = 135,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            txtSearch.TextChanged += (s, e) => LoadReservations();
            pnlFilter.Controls.Add(txtSearch);

            btnRefresh = Theme.MakeButton("🔄", 735, 15, 40, 36, Color.FromArgb(70, 80, 95));
            btnRefresh.Click += (s, e) => LoadReservations();
            pnlFilter.Controls.Add(btnRefresh);

            this.Controls.Add(pnlFilter);

            dgReservations = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                // تمييز السطور التبادلية لإراحة العين
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(42, 48, 62) : Color.FromArgb(238, 243, 250),
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeight = 38,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };

            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReservationID", Visible = false });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReservationNumber", HeaderText = "رقم الحجز", FillWeight = 90 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReservationDate", HeaderText = "تاريخ الحجز", FillWeight = 85 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "اسم العميل", FillWeight = 115 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientPhone", HeaderText = "الهاتف", FillWeight = 85 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الأصناف المحجوزة", FillWeight = 160 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "الإجمالي", FillWeight = 75 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "DepositAmount", HeaderText = "العربون", FillWeight = 75 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "RemainingAmount", HeaderText = "المتبقي", FillWeight = 75 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpectedDate", HeaderText = "تاريخ التوفير", FillWeight = 85 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 85 });

            this.Controls.Add(dgReservations);

            pnlTop.SendToBack();
            pnlFilter.SendToBack();
            dgReservations.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private void LoadReservations()
        {
            dgReservations.Rows.Clear();
            string status = cboStatusFilter.SelectedItem?.ToString() ?? "الكل";
            string q = txtSearch.Text.Trim();

            string sql = @"
                SELECT ReservationID, ReservationNumber, ReservationDate, ClientName, ClientPhone, ProductName, TotalAmount, DepositAmount, RemainingAmount, ExpectedDate, Status
                FROM CustomerReservations
                WHERE 1=1 ";

            if (status != "الكل")
            {
                sql += " AND Status = @status ";
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                sql += " AND (ClientName LIKE @q OR ClientPhone LIKE @q OR ProductName LIKE @q OR ReservationNumber LIKE @q) ";
            }
            sql += " ORDER BY ReservationID DESC";

            var dt = DbHelper.Query(sql,
                DbHelper.P("@status", status),
                DbHelper.P("@q", "%" + q + "%")
            );

            foreach (DataRow r in dt.Rows)
            {
                string st = r["Status"].ToString();
                DateTime rDate = Convert.ToDateTime(r["ReservationDate"]);
                DateTime? expDate = r["ExpectedDate"] != DBNull.Value ? Convert.ToDateTime(r["ExpectedDate"]) : (DateTime?)null;

                int ri = dgReservations.Rows.Add(
                    r["ReservationID"],
                    r["ReservationNumber"],
                    rDate.ToString("yyyy/MM/dd"),
                    r["ClientName"],
                    r["ClientPhone"],
                    r["ProductName"],
                    Convert.ToDecimal(r["TotalAmount"]).ToString("N2") + " ج",
                    Convert.ToDecimal(r["DepositAmount"]).ToString("N2") + " ج",
                    Convert.ToDecimal(r["RemainingAmount"]).ToString("N2") + " ج",
                    expDate.HasValue ? expDate.Value.ToString("yyyy/MM/dd") : "-",
                    st
                );

                var row = dgReservations.Rows[ri];
                if (st == "قيد الانتظار")
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(230, 126, 34); // برتقالي زاهي
                    row.Cells["Status"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (st == "جاهز للتسليم")
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(46, 204, 113); // أخضر مريح
                    row.Cells["Status"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (st == "تم التسليم")
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(52, 152, 219); // أزرق
                }
                else if (st == "ملغي")
                {
                    row.Cells["Status"].Style.ForeColor = Color.Gray;
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (!Session.CanEdit("Reservations")) { MessageBox.Show("⛔ ليس لديك صلاحية تعديل الحجوزات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (dgReservations.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى تحديد فاتورة الحجز المراد تعديلها من الجدول", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(dgReservations.SelectedRows[0].Cells["ReservationID"].Value);
            using (var dlg = new FrmAddReservation(id))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadReservations();
                }
            }
        }

        private void BtnSetReady_Click(object sender, EventArgs e)
        {
            if (dgReservations.SelectedRows.Count == 0) return;
            if (!Session.CanEdit("Reservations")) { MessageBox.Show("⛔ ليس لديك صلاحية تحديث حالة الحجوز.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgReservations.SelectedRows[0].Cells["ReservationID"].Value);
            try
            {
                DbHelper.Execute("UPDATE CustomerReservations SET Status=N'جاهز للتسليم' WHERE ReservationID=@id", DbHelper.P("@id", id));
                MessageBox.Show("تم تحديث حالة الحجز إلى (جاهز للتسليم). يمكنك الاتصال بالعميل الآن.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadReservations();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancelRes_Click(object sender, EventArgs e)
        {
            if (dgReservations.SelectedRows.Count == 0) return;
            if (!Session.CanDelete("Reservations")) { MessageBox.Show("⛔ ليس لديك صلاحية إلغاء الحجوزات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgReservations.SelectedRows[0].Cells["ReservationID"].Value);

            if (MessageBox.Show("هل أنت تأكد من إلغاء هذا الحجز؟", "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    DbHelper.Execute("UPDATE CustomerReservations SET Status=N'ملغي' WHERE ReservationID=@id", DbHelper.P("@id", id));
                    MessageBox.Show("تم إلغاء الحجز.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadReservations();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnConvertToSale_Click(object sender, EventArgs e)
        {
            if (dgReservations.SelectedRows.Count == 0) return;
            if (!Session.CanEdit("Reservations")) { MessageBox.Show("⛔ ليس لديك صلاحية تحويل الحجز لفاتورة مبيعات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgReservations.SelectedRows[0].Cells["ReservationID"].Value);

            var dt = DbHelper.Query("SELECT * FROM CustomerReservations WHERE ReservationID=@id", DbHelper.P("@id", id));
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];

            string status = r["Status"].ToString();
            if (status == "تم التسليم")
            {
                MessageBox.Show("هذا الحجز تم تسليمه وتحويله مسبقاً بفاتورة مبيعات.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (status == "ملغي")
            {
                MessageBox.Show("هذا الحجز ملغي ولا يمكن تحويله لفاتورة مبيعات.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? clientID = r["ClientID"] != DBNull.Value ? Convert.ToInt32(r["ClientID"]) : (int?)null;
            decimal total = Convert.ToDecimal(r["TotalAmount"]);
            decimal deposit = Convert.ToDecimal(r["DepositAmount"]);
            decimal remaining = Convert.ToDecimal(r["RemainingAmount"]);
            string productName = r["ProductName"].ToString();
            string resNo = r["ReservationNumber"].ToString();

            if (MessageBox.Show($"تأكيد تحويل الحجز [{resNo}] إلى فاتورة مبيعات نهائية وسداد المتبقي ({remaining:N2} ج)؟", "تأكيد البيع والتسليم", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    // 1. إنشاء فاتورة مبيعات
                    string saleCode = "SAL-" + DateTime.Now.ToString("yyMMddHHmmss");
                    int saleID = DbHelper.ExecuteInsert(@"
                        INSERT INTO Sales(SaleCode, SaleDate, SaleType, ClientID, TotalAmount, DiscountAmount, CashPaid, Notes, CreatedBy, IsPosted)
                        VALUES(@code, GETDATE(), N'Cash', @cid, @tot, @disc, @paid, @notes, @by, 1)",
                        DbHelper.P("@code", saleCode),
                        DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                        DbHelper.P("@tot", total),
                        DbHelper.P("@disc", deposit),
                        DbHelper.P("@paid", remaining),
                        DbHelper.P("@notes", $"تسليم حجز رقم ({resNo}) - {productName}"),
                        DbHelper.P("@by", Session.EmpID)
                    );

                    if (saleID <= 0)
                    {
                        MessageBox.Show("❌ حدث خطأ أثناء إنشاء فاتورة المبيعات الخاصة بالحجز.", "خطأ في عملية البيع", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 2. إدراج أصناف الحجز في SaleItems
                    var dtItems = DbHelper.Query("SELECT * FROM CustomerReservationItems WHERE ReservationID=@id", DbHelper.P("@id", id));
                    if (dtItems.Rows.Count > 0)
                    {
                        foreach (DataRow ri in dtItems.Rows)
                        {
                            int pId = ri["ProductID"] != DBNull.Value ? Convert.ToInt32(ri["ProductID"]) : 0;
                            decimal q = Convert.ToDecimal(ri["Quantity"]);
                            decimal up = Convert.ToDecimal(ri["UnitPrice"]);
                            decimal tp = Convert.ToDecimal(ri["TotalPrice"]);

                            if (pId > 0)
                            {
                                DbHelper.Execute(@"
                                    INSERT INTO SaleItems(SaleID, ProductID, Quantity, UnitPrice, TotalPrice)
                                    VALUES(@sid, @pid, @qty, @up, @tp)",
                                    DbHelper.P("@sid", saleID),
                                    DbHelper.P("@pid", pId),
                                    DbHelper.P("@qty", q),
                                    DbHelper.P("@up", up),
                                    DbHelper.P("@tp", tp)
                                );
                            }
                        }
                    }
                    else
                    {
                        // الحجوزات القديمة
                        int? productID = r["ProductID"] != DBNull.Value ? Convert.ToInt32(r["ProductID"]) : (int?)null;
                        decimal qty = Convert.ToDecimal(r["Quantity"]);
                        if (productID.HasValue && productID.Value > 0)
                        {
                            DbHelper.Execute(@"
                                INSERT INTO SaleItems(SaleID, ProductID, Quantity, UnitPrice, TotalPrice)
                                VALUES(@sid, @pid, @qty, @up, @tp)",
                                DbHelper.P("@sid", saleID),
                                DbHelper.P("@pid", productID.Value),
                                DbHelper.P("@qty", qty),
                                DbHelper.P("@up", total / (qty > 0 ? qty : 1)),
                                DbHelper.P("@tp", total)
                            );
                        }
                    }

                    // 3. تحديث حالة الحجز
                    DbHelper.Execute("UPDATE CustomerReservations SET Status=N'تم التسليم', SaleID=@sid WHERE ReservationID=@id",
                        DbHelper.P("@sid", saleID),
                        DbHelper.P("@id", id)
                    );

                    MessageBox.Show($"تم تسليم الحجز بنجاح وإنشاء فاتورة مبيعات رقم [{saleID}].", "تم التسليم بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadReservations();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("فشل تحويل الحجز لمبيعات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (dgReservations.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار حجز من الجدول لطباعة إيصاله", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(dgReservations.SelectedRows[0].Cells["ReservationID"].Value);
            PrintReservationReceipt(id);
        }

        private void PrintReservationReceipt(int reservationID)
        {
            try
            {
                var dt = DbHelper.Query("SELECT * FROM CustomerReservations WHERE ReservationID=@id", DbHelper.P("@id", reservationID));
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];

                var dtItems = DbHelper.Query("SELECT * FROM CustomerReservationItems WHERE ReservationID=@id", DbHelper.P("@id", reservationID));

                using (var pd = new PrintDocument())
                {
                    pd.PrintController = new StandardPrintController();
                    AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
                    pd.PrintPage += (s, e) =>
                    {
                        Graphics g = e.Graphics;
                        Font fontTitle = new Font("Segoe UI", 14f, FontStyle.Bold);
                        Font fontHeader = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                        Font fontBody = new Font("Segoe UI", 9.5f);
                        Font fontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);

                        float y = 20;
                        float leftMargin = 20;
                        float rightMargin = e.PageBounds.Width - 20;
                        float contentWidth = rightMargin - leftMargin;

                        StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                        StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };
                        StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near };

                        // Header
                        g.DrawString(AppConfig.CompanyName ?? "المحل التجاري", fontTitle, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 30), sfCenter);
                        y += 30;
                        g.DrawString("📋 إيصال حجز صنف / طلبية عميل", fontHeader, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 25), sfCenter);
                        y += 30;

                        g.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
                        y += 10;

                        // Info
                        string resNo = r["ReservationNumber"].ToString();
                        string clientName = r["ClientName"].ToString();
                        string clientPhone = r["ClientPhone"].ToString();
                        string resDate = Convert.ToDateTime(r["ReservationDate"]).ToString("yyyy/MM/dd");
                        string expDate = r["ExpectedDate"] != DBNull.Value ? Convert.ToDateTime(r["ExpectedDate"]).ToString("yyyy/MM/dd") : "-";

                        g.DrawString($"رقم الحجز: {resNo}", fontBold, Brushes.Black, rightMargin, y, sfRight);
                        g.DrawString($"التاريخ: {resDate}", fontBody, Brushes.Black, leftMargin, y, sfLeft);
                        y += 22;

                        g.DrawString($"العميل: {clientName}", fontBold, Brushes.Black, rightMargin, y, sfRight);
                        g.DrawString($"الهاتف: {clientPhone}", fontBody, Brushes.Black, leftMargin, y, sfLeft);
                        y += 22;

                        g.DrawString($"موعد التوفير المتوقع: {expDate}", fontBold, Brushes.Black, rightMargin, y, sfRight);
                        y += 28;

                        g.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
                        y += 8;

                        // Table Headers
                        g.DrawString("الصنف / الموديل", fontHeader, Brushes.Black, rightMargin, y, sfRight);
                        g.DrawString("الكمية", fontHeader, Brushes.Black, leftMargin + 150, y, sfLeft);
                        g.DrawString("السعر", fontHeader, Brushes.Black, leftMargin + 80, y, sfLeft);
                        g.DrawString("الإجمالي", fontHeader, Brushes.Black, leftMargin, y, sfLeft);
                        y += 24;

                        g.DrawLine(Pens.Gray, leftMargin, y, rightMargin, y);
                        y += 6;

                        if (dtItems.Rows.Count > 0)
                        {
                            foreach (DataRow item in dtItems.Rows)
                            {
                                string name = item["ProductName"].ToString();
                                decimal q = Convert.ToDecimal(item["Quantity"]);
                                decimal price = Convert.ToDecimal(item["UnitPrice"]);
                                decimal total = Convert.ToDecimal(item["TotalPrice"]);

                                g.DrawString(name, fontBody, Brushes.Black, rightMargin, y, sfRight);
                                g.DrawString(q.ToString("N0"), fontBody, Brushes.Black, leftMargin + 150, y, sfLeft);
                                g.DrawString(price.ToString("N2"), fontBody, Brushes.Black, leftMargin + 80, y, sfLeft);
                                g.DrawString(total.ToString("N2"), fontBody, Brushes.Black, leftMargin, y, sfLeft);
                                y += 22;
                            }
                        }
                        else
                        {
                            string name = r["ProductName"].ToString();
                            decimal q = Convert.ToDecimal(r["Quantity"]);
                            decimal total = Convert.ToDecimal(r["TotalAmount"]);
                            decimal price = total / (q > 0 ? q : 1);

                            g.DrawString(name, fontBody, Brushes.Black, rightMargin, y, sfRight);
                            g.DrawString(q.ToString("N0"), fontBody, Brushes.Black, leftMargin + 150, y, sfLeft);
                            g.DrawString(price.ToString("N2"), fontBody, Brushes.Black, leftMargin + 80, y, sfLeft);
                            g.DrawString(total.ToString("N2"), fontBody, Brushes.Black, leftMargin, y, sfLeft);
                            y += 22;
                        }

                        g.DrawLine(Pens.Black, leftMargin, y, rightMargin, y);
                        y += 12;

                        decimal totalAmt = Convert.ToDecimal(r["TotalAmount"]);
                        decimal depositAmt = Convert.ToDecimal(r["DepositAmount"]);
                        decimal remAmt = Convert.ToDecimal(r["RemainingAmount"]);

                        g.DrawString($"إجمالي الفاتورة: {totalAmt:N2} ج", fontBold, Brushes.Black, rightMargin, y, sfRight);
                        y += 22;
                        g.DrawString($"العربون المدفوع: {depositAmt:N2} ج", fontBold, Brushes.Black, rightMargin, y, sfRight);
                        y += 22;
                        g.DrawString($"المتبقي عند التسليم: {remAmt:N2} ج", fontBold, Brushes.DarkRed, rightMargin, y, sfRight);
                        y += 30;

                        if (r["Notes"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["Notes"].ToString()))
                        {
                            g.DrawString($"ملاحظات: {r["Notes"]}", fontBody, Brushes.Black, rightMargin, y, sfRight);
                            y += 25;
                        }

                        g.DrawString("شكراً لتعاملكم معنا 💖", fontBold, Brushes.Black, new RectangleF(leftMargin, y, contentWidth, 25), sfCenter);
                    };

                    using (var dlg = new PrintPreviewDialog { Document = pd, Width = 800, Height = 600, StartPosition = FormStartPosition.CenterParent })
                    {
                        dlg.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء طباعة الإيصال:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System;
using System.Data;
using System.Drawing;
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
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlTop = Theme.MakeTitleBar("📋 سجل وإدارة حجوزات الأصناف والطلبيات الخاصة", "متابعة حجوزات الأصناف غير المتوفرة، تحويل الحجوزات لمبيعات، وتنبيهات توفير البضائع.");
            this.Controls.Add(pnlTop);

            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            btnAdd = Theme.MakeButton("➕ حجز جديد", 15, 12, 120, 36, Theme.Success);
            btnAdd.Click += (s, e) =>
            {
                using (var dlg = new FrmAddReservation())
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) LoadReservations();
                }
            };
            pnlFilter.Controls.Add(btnAdd);

            btnSetReady = Theme.MakeButton("🟢 جاهز للتسليم", 145, 12, 130, 36, Color.FromArgb(46, 204, 113));
            btnSetReady.Click += BtnSetReady_Click;
            pnlFilter.Controls.Add(btnSetReady);

            btnConvertToSale = Theme.MakeButton("🛒 تحويل لفاتورة مبيعات", 285, 12, 170, 36, Theme.Primary);
            btnConvertToSale.Click += BtnConvertToSale_Click;
            pnlFilter.Controls.Add(btnConvertToSale);

            btnCancelRes = Theme.MakeButton("❌ إلغاء الحجز", 465, 12, 110, 36, Theme.Danger);
            btnCancelRes.Click += BtnCancelRes_Click;
            pnlFilter.Controls.Add(btnCancelRes);

            var lblStatus = new Label { Text = "الحالة:", Location = new Point(585, 20), AutoSize = true, ForeColor = Theme.TextMain };
            pnlFilter.Controls.Add(lblStatus);

            cboStatusFilter = new ComboBox
            {
                Location = new Point(630, 16),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboStatusFilter.Items.AddRange(new object[] { "الكل", "قيد الانتظار", "جاهز للتسليم", "تم التسليم", "ملغي" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => LoadReservations();
            pnlFilter.Controls.Add(cboStatusFilter);

            var lblSearch = new Label { Text = "🔍 بحث:", Location = new Point(775, 20), AutoSize = true, ForeColor = Theme.TextMain };
            pnlFilter.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location = new Point(825, 16),
                Width = 180,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            txtSearch.TextChanged += (s, e) => LoadReservations();
            pnlFilter.Controls.Add(txtSearch);

            btnRefresh = Theme.MakeButton("🔄", 1015, 15, 45, 36, Color.FromArgb(70, 80, 95));
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
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReservationDate", HeaderText = "تاريخ الحجز", FillWeight = 90 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "اسم العميل", FillWeight = 120 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientPhone", HeaderText = "الهاتف", FillWeight = 85 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف / الموديل", FillWeight = 140 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 50 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "الإجمالي", FillWeight = 75 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "DepositAmount", HeaderText = "العربون", FillWeight = 75 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "RemainingAmount", HeaderText = "المتبقي", FillWeight = 75 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpectedDate", HeaderText = "تاريخ التوفير", FillWeight = 85 });
            dgReservations.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 85 });

            this.Controls.Add(dgReservations);

            pnlTop.SendToBack();
            pnlFilter.SendToBack();
            dgReservations.BringToFront();
        }

        private void LoadReservations()
        {
            dgReservations.Rows.Clear();
            string status = cboStatusFilter.SelectedItem?.ToString() ?? "الكل";
            string q = txtSearch.Text.Trim();

            string sql = @"
                SELECT ReservationID, ReservationNumber, ReservationDate, ClientName, ClientPhone, ProductName, Quantity, TotalAmount, DepositAmount, RemainingAmount, ExpectedDate, Status
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
                    Convert.ToDecimal(r["Quantity"]).ToString("N0"),
                    Convert.ToDecimal(r["TotalAmount"]).ToString("N2") + " ج",
                    Convert.ToDecimal(r["DepositAmount"]).ToString("N2") + " ج",
                    Convert.ToDecimal(r["RemainingAmount"]).ToString("N2") + " ج",
                    expDate.HasValue ? expDate.Value.ToString("yyyy/MM/dd") : "-",
                    st
                );

                var row = dgReservations.Rows[ri];
                if (st == "قيد الانتظار")
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(230, 126, 34); // البرتقالي
                }
                else if (st == "جاهز للتسليم")
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(46, 204, 113); // الأخضر
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (st == "تم التسليم")
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(52, 152, 219); // الأزرق
                }
                else if (st == "ملغي")
                {
                    row.DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
        }

        private void BtnSetReady_Click(object sender, EventArgs e)
        {
            if (dgReservations.SelectedRows.Count == 0) return;
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
            int? productID = r["ProductID"] != DBNull.Value ? Convert.ToInt32(r["ProductID"]) : (int?)null;
            decimal qty = Convert.ToDecimal(r["Quantity"]);
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
                        INSERT INTO Sales(SaleCode, SaleDate, SaleType, ClientID, TotalAmount, DiscountAmount, NetAmount, PaidAmount, Notes, CreatedBy, IsPosted)
                        VALUES(@code, GETDATE(), N'Cash', @cid, @tot, @disc, @net, @paid, @notes, @by, 1)",
                        DbHelper.P("@code", saleCode),
                        DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                        DbHelper.P("@tot", total),
                        DbHelper.P("@disc", deposit),
                        DbHelper.P("@net", remaining),
                        DbHelper.P("@paid", remaining),
                        DbHelper.P("@notes", $"تسليم حجز رقم ({resNo}) - صنف: {productName}"),
                        DbHelper.P("@by", Session.EmpID)
                    );

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

                    // 2. تحديث حالة الحجز
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
    }
}

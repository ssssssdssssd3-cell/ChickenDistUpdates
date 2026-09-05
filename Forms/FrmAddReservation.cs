using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmAddReservation : Form
    {
        private ComboBox cboClient;
        private TextBox txtClientName;
        private TextBox txtClientPhone;

        private ComboBox cboProduct;
        private Button btnSearchProduct;
        private NumericUpDown nudQty;
        private NumericUpDown nudUnitPrice;
        private Button btnAddItem;

        private DataGridView dgItems;
        private Label lblTotal;
        private NumericUpDown nudDeposit;
        private Label lblRemaining;
        private DateTimePicker dtpExpected;
        private TextBox txtNotes;

        private int _selectedProductID = 0;
        private string _selectedProductCode = "";

        private int _editReservationID = 0;
        private string _editingResNumber = "";
        private decimal _originalDeposit = 0;

        public FrmAddReservation()
        {
            InitializeComponentCustom();
            LoadClients();
            LoadProducts();
        }

        public FrmAddReservation(int reservationID)
        {
            _editReservationID = reservationID;
            InitializeComponentCustom();
            LoadClients();
            LoadProducts();
            LoadReservationForEdit();
        }

        private void InitializeComponentCustom()
        {
            this.Text = _editReservationID > 0 ? "✏️ تعديل فاتورة حجز عميل" : "📝 تسجيل حجز صنف / طلبية جديدة";
            this.Size = new Size(780, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var titleText = _editReservationID > 0 ? "✏️ تعديل بيانات وفاتورة حجز العميل" : "📝 حجز صنف / طلبية جديدة للعميل";
            var descText = "سجل بيانات العميل والأصناف المحجوزة مع العربون المدفوع وتاريخ التسليم المتوقع.";
            var pnlTop = Theme.MakeTitleBar(titleText, descText);
            this.Controls.Add(pnlTop);

            int y = 80;

            // 1. العميل
            AddLabel("العميل المسجل:", 20, y);
            cboClient = new ComboBox
            {
                Location = new Point(140, y - 4),
                Width = 225,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboClient.SelectedIndexChanged += CboClient_SelectedIndexChanged;
            this.Controls.Add(cboClient);

            var btnClientSearch = Theme.MakeButton("🔍", 370, y - 4, 35, 27, Theme.Primary);
            btnClientSearch.Click += (s, e) =>
            {
                using (var frm = new FrmClientSearch())
                {
                    if (frm.ShowDialog(this) == DialogResult.OK && frm.SelectedClientID > 0)
                    {
                        int cid = frm.SelectedClientID;
                        bool found = false;
                        for (int i = 0; i < cboClient.Items.Count; i++)
                        {
                            if (cboClient.Items[i] is ClientItem ci && ci.ID == cid)
                            {
                                cboClient.SelectedIndex = i;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            txtClientName.Text = frm.SelectedClientName;
                            txtClientPhone.Text = frm.SelectedClientPhone;
                        }
                    }
                }
            };
            this.Controls.Add(btnClientSearch);

            AddLabel("اسم العميل:", 415, y);
            txtClientName = new TextBox
            {
                Location = new Point(500, y - 4),
                Width = 240,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            this.Controls.Add(txtClientName);

            y += 40;
            AddLabel("رقم الهاتف:", 20, y);
            txtClientPhone = new TextBox
            {
                Location = new Point(140, y - 4),
                Width = 260,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            this.Controls.Add(txtClientPhone);

            y += 45;

            // 2. اختيار وتجهيز الصنف
            var pnlAddItem = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(735, 75),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblProductHeader = new Label
            {
                Text = "🛒 اختيار إضافة أصناف الفاتورة:",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.Primary
            };
            pnlAddItem.Controls.Add(lblProductHeader);

            cboProduct = new ComboBox
            {
                Location = new Point(10, 36),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboProduct.SelectedIndexChanged += CboProduct_SelectedIndexChanged;
            pnlAddItem.Controls.Add(cboProduct);

            btnSearchProduct = Theme.MakeButton("🔍 بحث", 275, 35, 75, 30, Theme.Primary);
            btnSearchProduct.Click += BtnSearchProduct_Click;
            pnlAddItem.Controls.Add(btnSearchProduct);

            var lblQ = new Label { Text = "الكمية:", Location = new Point(360, 40), AutoSize = true, ForeColor = Theme.TextMain };
            pnlAddItem.Controls.Add(lblQ);

            nudQty = new NumericUpDown
            {
                Location = new Point(405, 36),
                Width = 70,
                Minimum = 1,
                Maximum = 10000,
                Value = 1,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            pnlAddItem.Controls.Add(nudQty);

            var lblP = new Label { Text = "السعر:", Location = new Point(485, 40), AutoSize = true, ForeColor = Theme.TextMain };
            pnlAddItem.Controls.Add(lblP);

            nudUnitPrice = new NumericUpDown
            {
                Location = new Point(530, 36),
                Width = 85,
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Value = 0,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            pnlAddItem.Controls.Add(nudUnitPrice);

            btnAddItem = Theme.MakeButton("➕ إضافة", 625, 34, 95, 32, Theme.Success);
            btnAddItem.Click += BtnAddItem_Click;
            pnlAddItem.Controls.Add(btnAddItem);

            this.Controls.Add(pnlAddItem);

            y += 85;

            // 3. جدول الأصناف الحالية بالحجز
            dgItems = new DataGridView
            {
                Location = new Point(15, y),
                Size = new Size(735, 185),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
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
                ColumnHeadersHeight = 34,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف / الموديل", FillWeight = 240 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 60 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر القطعة", FillWeight = 90 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "الإجمالي", FillWeight = 95 });

            var btnDeleteCol = new DataGridViewButtonColumn
            {
                Name = "btnDelete",
                HeaderText = "إجراء",
                Text = "❌ حذف",
                UseColumnTextForButtonValue = true,
                FillWeight = 60
            };
            dgItems.Columns.Add(btnDeleteCol);

            dgItems.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dgItems.Columns["btnDelete"].Index)
                {
                    dgItems.Rows.RemoveAt(e.RowIndex);
                    RecalcTotals();
                }
            };

            this.Controls.Add(dgItems);

            y += 195;

            // 4. الإجمالي والعربون والمتبقي
            lblTotal = new Label
            {
                Text = "إجمالي الفاتورة: 0.00 ج",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.Success
            };
            this.Controls.Add(lblTotal);

            AddLabel("العربون المدفوع:", 320, y);
            nudDeposit = new NumericUpDown
            {
                Location = new Point(430, y - 4),
                Width = 120,
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Value = 0,
                BackColor = Theme.BgInput,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            nudDeposit.ValueChanged += (s, e) => RecalcTotals();
            this.Controls.Add(nudDeposit);

            y += 42;

            lblRemaining = new Label
            {
                Text = "المتبقي عند التسليم: 0.00 ج",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.Danger
            };
            this.Controls.Add(lblRemaining);

            AddLabel("تاريخ التوفير المتوقع:", 320, y);
            dtpExpected = new DateTimePicker
            {
                Location = new Point(440, y - 4),
                Width = 140,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(3),
                Font = Theme.FontMain
            };
            this.Controls.Add(dtpExpected);

            y += 48;

            // 5. ملاحظات
            AddLabel("ملاحظات الحجز:", 20, y);
            txtNotes = new TextBox
            {
                Location = new Point(140, y - 4),
                Width = 595,
                Height = 50,
                Multiline = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            this.Controls.Add(txtNotes);

            y += 65;

            // 6. أزرار التحكم
            var btnSave = Theme.MakeButton(_editReservationID > 0 ? "💾 حفظ التعديلات" : "💾 حفظ الحجز وتأكيده", 220, y, 190, 42, Theme.Success);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            var btnCancel = Theme.MakeButton("إلغاء", 425, y, 110, 42, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            Theme.ApplyFormRTL(this);
        }

        private void AddLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            this.Controls.Add(lbl);
        }

        private void LoadClients()
        {
            try
            {
                cboClient.Items.Clear();
                cboClient.Items.Add("(عميل جديد / غير مسجل)");
                var dt = DbHelper.Query("SELECT ClientID, ClientName, Phone FROM Clients WHERE IsActive=1 ORDER BY ClientName");
                foreach (DataRow r in dt.Rows)
                {
                    cboClient.Items.Add(new ClientItem
                    {
                        ID = Convert.ToInt32(r["ClientID"]),
                        Name = r["ClientName"].ToString(),
                        Phone = r["Phone"]?.ToString() ?? ""
                    });
                }
                cboClient.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadProducts()
        {
            try
            {
                cboProduct.Items.Clear();
                var dt = DbHelper.Query("SELECT ProductID, ProductName, ProductCode, SalePrice FROM Products WHERE IsActive=1 ORDER BY ProductName");
                foreach (DataRow r in dt.Rows)
                {
                    cboProduct.Items.Add(new ProductResItem
                    {
                        ID = Convert.ToInt32(r["ProductID"]),
                        Name = r["ProductName"].ToString(),
                        Code = r["ProductCode"]?.ToString() ?? "",
                        Price = Convert.ToDecimal(r["SalePrice"])
                    });
                }
            }
            catch { }
        }

        private void LoadReservationForEdit()
        {
            if (_editReservationID <= 0) return;
            try
            {
                var dt = DbHelper.Query("SELECT * FROM CustomerReservations WHERE ReservationID=@id", DbHelper.P("@id", _editReservationID));
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];

                _editingResNumber = r["ReservationNumber"].ToString();
                txtClientName.Text = r["ClientName"].ToString();
                txtClientPhone.Text = r["ClientPhone"].ToString();
                txtNotes.Text = r["Notes"]?.ToString() ?? "";
                _originalDeposit = Convert.ToDecimal(r["DepositAmount"]);
                nudDeposit.Value = _originalDeposit;

                if (r["ExpectedDate"] != DBNull.Value)
                    dtpExpected.Value = Convert.ToDateTime(r["ExpectedDate"]);

                int? clientID = r["ClientID"] != DBNull.Value ? Convert.ToInt32(r["ClientID"]) : (int?)null;
                if (clientID.HasValue)
                {
                    for (int i = 0; i < cboClient.Items.Count; i++)
                    {
                        if (cboClient.Items[i] is ClientItem ci && ci.ID == clientID.Value)
                        {
                            cboClient.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // تحميل الأصناف
                dgItems.Rows.Clear();
                var dtItems = DbHelper.Query("SELECT * FROM CustomerReservationItems WHERE ReservationID=@id", DbHelper.P("@id", _editReservationID));
                if (dtItems.Rows.Count > 0)
                {
                    foreach (DataRow ri in dtItems.Rows)
                    {
                        int pId = ri["ProductID"] != DBNull.Value ? Convert.ToInt32(ri["ProductID"]) : 0;
                        string pCode = ri["ProductCode"]?.ToString() ?? "";
                        string pName = ri["ProductName"].ToString();
                        decimal qty = Convert.ToDecimal(ri["Quantity"]);
                        decimal price = Convert.ToDecimal(ri["UnitPrice"]);
                        decimal tot = Convert.ToDecimal(ri["TotalPrice"]);

                        dgItems.Rows.Add(pId, pCode, pName, qty, price, tot);
                    }
                }
                else
                {
                    // الحجوزات القديمة (Single Item Legacy)
                    if (r["ProductName"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["ProductName"].ToString()))
                    {
                        int pId = r["ProductID"] != DBNull.Value ? Convert.ToInt32(r["ProductID"]) : 0;
                        string pCode = r["ProductCode"]?.ToString() ?? "";
                        string pName = r["ProductName"].ToString();
                        decimal qty = Convert.ToDecimal(r["Quantity"]);
                        decimal price = Convert.ToDecimal(r["UnitPrice"]);
                        decimal tot = Convert.ToDecimal(r["TotalAmount"]);

                        dgItems.Rows.Add(pId, pCode, pName, qty, price, tot);
                    }
                }

                RecalcTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل بيانات الحجز للتعديل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CboClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboClient.SelectedItem is ClientItem ci)
            {
                txtClientName.Text = ci.Name;
                txtClientPhone.Text = ci.Phone;
            }
        }

        private void CboProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboProduct.SelectedItem is ProductResItem pi)
            {
                _selectedProductID = pi.ID;
                _selectedProductCode = pi.Code;
                nudUnitPrice.Value = pi.Price;
            }
        }

        private void BtnSearchProduct_Click(object sender, EventArgs e)
        {
            using (var dlg = new FrmModelLookup())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedProductID > 0)
                {
                    _selectedProductID = dlg.SelectedProductID;
                    var dt = DbHelper.Query("SELECT ProductID, ProductName, ProductCode, SalePrice FROM Products WHERE ProductID=@id", DbHelper.P("@id", _selectedProductID));
                    if (dt.Rows.Count > 0)
                    {
                        DataRow r = dt.Rows[0];
                        string pName = r["ProductName"].ToString();
                        _selectedProductCode = r["ProductCode"]?.ToString() ?? "";
                        nudUnitPrice.Value = Convert.ToDecimal(r["SalePrice"]);

                        for (int i = 0; i < cboProduct.Items.Count; i++)
                        {
                            if (cboProduct.Items[i] is ProductResItem pri && pri.ID == _selectedProductID)
                            {
                                cboProduct.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            string pName = cboProduct.Text.Trim();
            if (string.IsNullOrWhiteSpace(pName))
            {
                MessageBox.Show("يرجى اختيار أو كتابة اسم الصنف المراد إضافته للحجز", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboProduct.Focus();
                return;
            }

            decimal qty = nudQty.Value;
            decimal unitPrice = nudUnitPrice.Value;
            decimal total = qty * unitPrice;

            // التحقق من تكرار الصنف
            foreach (DataGridViewRow r in dgItems.Rows)
            {
                string existingName = r.Cells["ProductName"].Value?.ToString();
                if (existingName == pName)
                {
                    decimal currentQty = Convert.ToDecimal(r.Cells["Quantity"].Value);
                    r.Cells["Quantity"].Value = currentQty + qty;
                    r.Cells["TotalPrice"].Value = (currentQty + qty) * unitPrice;
                    RecalcTotals();
                    return;
                }
            }

            dgItems.Rows.Add(_selectedProductID, _selectedProductCode, pName, qty, unitPrice, total);
            RecalcTotals();

            cboProduct.Text = "";
            nudQty.Value = 1;
            _selectedProductID = 0;
            _selectedProductCode = "";
        }

        private void RecalcTotals()
        {
            decimal total = 0;
            foreach (DataGridViewRow r in dgItems.Rows)
            {
                total += Convert.ToDecimal(r.Cells["TotalPrice"].Value);
            }

            decimal deposit = nudDeposit.Value;
            if (deposit > total && total > 0)
            {
                deposit = total;
                nudDeposit.Value = deposit;
            }
            decimal remaining = total - deposit;

            lblTotal.Text = $"إجمالي الفاتورة: {total:N2} ج";
            lblRemaining.Text = $"المتبقي عند التسليم: {remaining:N2} ج";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string clientName = txtClientName.Text.Trim();
            if (string.IsNullOrWhiteSpace(clientName))
            {
                MessageBox.Show("يرجى إدخال اسم العميل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtClientName.Focus();
                return;
            }

            if (dgItems.Rows.Count == 0)
            {
                MessageBox.Show("يرجى إضافة صنف واحد على الأقل في فاتورة الحجز", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboProduct.Focus();
                return;
            }

            int? clientID = null;
            if (cboClient.SelectedItem is ClientItem ci) clientID = ci.ID;

            decimal totalSum = 0;
            string primaryProductName = dgItems.Rows[0].Cells["ProductName"].Value?.ToString() ?? "";
            if (dgItems.Rows.Count > 1)
            {
                primaryProductName += $" (+ {dgItems.Rows.Count - 1} أصناف أخرى)";
            }

            foreach (DataGridViewRow r in dgItems.Rows)
            {
                totalSum += Convert.ToDecimal(r.Cells["TotalPrice"].Value);
            }

            decimal deposit = nudDeposit.Value;
            decimal remaining = totalSum - deposit;

            string resNo = _editReservationID > 0 ? _editingResNumber : ("RES-" + DateTime.Now.ToString("yyMMddHHmmss"));

            try
            {
                if (_editReservationID > 0)
                {
                    // 1. تحديث الهيدر في وضع التعديل
                    string updateSql = @"
                        UPDATE CustomerReservations
                        SET ClientID=@cid, ClientName=@cname, ClientPhone=@cphone, ProductName=@pname, TotalAmount=@total, DepositAmount=@deposit, RemainingAmount=@rem, ExpectedDate=@expected, Notes=@notes
                        WHERE ReservationID=@id";

                    DbHelper.Execute(updateSql,
                        DbHelper.P("@cid", (object)clientID ?? DBNull.Value),
                        DbHelper.P("@cname", clientName),
                        DbHelper.P("@cphone", txtClientPhone.Text.Trim()),
                        DbHelper.P("@pname", primaryProductName),
                        DbHelper.P("@total", totalSum),
                        DbHelper.P("@deposit", deposit),
                        DbHelper.P("@rem", remaining),
                        DbHelper.P("@expected", dtpExpected.Value),
                        DbHelper.P("@notes", txtNotes.Text.Trim()),
                        DbHelper.P("@id", _editReservationID)
                    );

                    // 2. تحديث البنود التفصيلية
                    DbHelper.Execute("DELETE FROM CustomerReservationItems WHERE ReservationID=@id", DbHelper.P("@id", _editReservationID));

                    foreach (DataGridViewRow r in dgItems.Rows)
                    {
                        int pId = Convert.ToInt32(r.Cells["ProductID"].Value);
                        string pCode = r.Cells["ProductCode"].Value?.ToString() ?? "";
                        string pName = r.Cells["ProductName"].Value?.ToString() ?? "";
                        decimal q = Convert.ToDecimal(r.Cells["Quantity"].Value);
                        decimal up = Convert.ToDecimal(r.Cells["UnitPrice"].Value);
                        decimal tp = Convert.ToDecimal(r.Cells["TotalPrice"].Value);

                        DbHelper.Execute(@"
                            INSERT INTO CustomerReservationItems (ReservationID, ProductID, ProductName, ProductCode, Quantity, UnitPrice, TotalPrice)
                            VALUES (@rid, @pid, @pname, @pcode, @qty, @up, @tp)",
                            DbHelper.P("@rid", _editReservationID),
                            DbHelper.P("@pid", pId > 0 ? (object)pId : DBNull.Value),
                            DbHelper.P("@pname", pName),
                            DbHelper.P("@pcode", pCode),
                            DbHelper.P("@qty", q),
                            DbHelper.P("@up", up),
                            DbHelper.P("@tp", tp)
                        );
                    }

                    MessageBox.Show($"تم تحديث بيانات فاتورة الحجز برقم [{resNo}] بنجاح.", "تم التعديل بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // 1. إضافة حجز جديد
                    int firstProductID = Convert.ToInt32(dgItems.Rows[0].Cells["ProductID"].Value);
                    string firstProductCode = dgItems.Rows[0].Cells["ProductCode"].Value?.ToString() ?? "";
                    decimal firstQty = Convert.ToDecimal(dgItems.Rows[0].Cells["Quantity"].Value);
                    decimal firstPrice = Convert.ToDecimal(dgItems.Rows[0].Cells["UnitPrice"].Value);

                    string insertSql = @"
                        INSERT INTO CustomerReservations
                        (ReservationNumber, ClientID, ClientName, ClientPhone, ProductID, ProductName, ProductCode, Quantity, UnitPrice, TotalAmount, DepositAmount, RemainingAmount, ExpectedDate, Notes, CreatedBy, Status)
                        VALUES
                        (@no, @cid, @cname, @cphone, @pid, @pname, @pcode, @qty, @uprice, @total, @deposit, @rem, @expected, @notes, @user, N'قيد الانتظار')";

                    int resID = DbHelper.ExecuteInsert(insertSql,
                        DbHelper.P("@no", resNo),
                        DbHelper.P("@cid", (object)clientID ?? DBNull.Value),
                        DbHelper.P("@cname", clientName),
                        DbHelper.P("@cphone", txtClientPhone.Text.Trim()),
                        DbHelper.P("@pid", firstProductID > 0 ? (object)firstProductID : DBNull.Value),
                        DbHelper.P("@pname", primaryProductName),
                        DbHelper.P("@pcode", firstProductCode),
                        DbHelper.P("@qty", firstQty),
                        DbHelper.P("@uprice", firstPrice),
                        DbHelper.P("@total", totalSum),
                        DbHelper.P("@deposit", deposit),
                        DbHelper.P("@rem", remaining),
                        DbHelper.P("@expected", dtpExpected.Value),
                        DbHelper.P("@notes", txtNotes.Text.Trim()),
                        DbHelper.P("@user", Session.EmpName ?? Session.UserName ?? "System")
                    );

                    // 2. إدراج بنود الأصناف
                    if (resID > 0)
                    {
                        foreach (DataGridViewRow r in dgItems.Rows)
                        {
                            int pId = Convert.ToInt32(r.Cells["ProductID"].Value);
                            string pCode = r.Cells["ProductCode"].Value?.ToString() ?? "";
                            string pName = r.Cells["ProductName"].Value?.ToString() ?? "";
                            decimal q = Convert.ToDecimal(r.Cells["Quantity"].Value);
                            decimal up = Convert.ToDecimal(r.Cells["UnitPrice"].Value);
                            decimal tp = Convert.ToDecimal(r.Cells["TotalPrice"].Value);

                            DbHelper.Execute(@"
                                INSERT INTO CustomerReservationItems (ReservationID, ProductID, ProductName, ProductCode, Quantity, UnitPrice, TotalPrice)
                                VALUES (@rid, @pid, @pname, @pcode, @qty, @up, @tp)",
                                DbHelper.P("@rid", resID),
                                DbHelper.P("@pid", pId > 0 ? (object)pId : DBNull.Value),
                                DbHelper.P("@pname", pName),
                                DbHelper.P("@pcode", pCode),
                                DbHelper.P("@qty", q),
                                DbHelper.P("@up", up),
                                DbHelper.P("@tp", tp)
                            );
                        }

                        // تسويق العربون بالخزينة وحساب العميل
                        if (deposit > 0)
                        {
                            DbHelper.Execute(@"
                                INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, Notes, CreatedBy)
                                VALUES (GETDATE(), N'ReservationDeposit', @amt, 0, @notes, @by)",
                                DbHelper.P("@amt", deposit),
                                DbHelper.P("@notes", $"عربون حجز: {primaryProductName} للعميل ({clientName}) (حجز #{resNo})"),
                                DbHelper.P("@by", Session.EmpID)
                            );

                            if (clientID.HasValue && clientID.Value > 0)
                            {
                                DbHelper.Execute(@"
                                    INSERT INTO ClientTransactions (ClientID, TransDate, TransType, Debit, Credit, Notes, CreatedBy)
                                    VALUES (@cid, GETDATE(), N'عربون حجز', 0, @deposit, @notes, @by)",
                                    DbHelper.P("@cid", clientID.Value),
                                    DbHelper.P("@deposit", deposit),
                                    DbHelper.P("@notes", $"عربون حجز: {primaryProductName} (حجز #{resNo})"),
                                    DbHelper.P("@by", Session.EmpID)
                                );
                            }
                        }
                    }

                    MessageBox.Show($"تم تسجيل وتأكيد الحجز بنجاح برقم [{resNo}].", "نجاح الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل حفظ فاتورة الحجز:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class ClientItem
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public override string ToString() => $"{Name} ({Phone})";
        }

        private class ProductResItem
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public decimal Price { get; set; }
            public override string ToString() => Name;
        }
    }
}

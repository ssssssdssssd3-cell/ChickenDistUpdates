using System;
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
        private Label lblTotal;
        private NumericUpDown nudDeposit;
        private Label lblRemaining;
        private DateTimePicker dtpExpected;
        private TextBox txtNotes;

        private int _selectedProductID = 0;
        private string _selectedProductCode = "";

        public FrmAddReservation()
        {
            InitializeComponentCustom();
            LoadClients();
            LoadProducts();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "📝 تسجيل حجز صنف / طلبية جديدة";
            this.Size = new Size(620, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var pnlTop = Theme.MakeTitleBar("📝 حجز صنف / طلبية جديدة للعميل", "سجل بيانات العميل والصنف غير المتوفر مع العربون المدفوع وتاريخ التسليم المتوقع.");
            this.Controls.Add(pnlTop);

            int y = 80;

            // 1. العميل
            AddLabel("العميل المسجل:", 20, y);
            cboClient = new ComboBox
            {
                Location = new Point(140, y - 4),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboClient.SelectedIndexChanged += CboClient_SelectedIndexChanged;
            this.Controls.Add(cboClient);

            AddLabel("اسم العميل:", 20, y + 38);
            txtClientName = new TextBox
            {
                Location = new Point(140, y + 34),
                Width = 260,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            this.Controls.Add(txtClientName);

            AddLabel("رقم الهاتف:", 420, y + 38);
            txtClientPhone = new TextBox
            {
                Location = new Point(480, y + 34),
                Width = 110,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            this.Controls.Add(txtClientPhone);

            y += 80;

            // 2. الصنف / الموديل
            AddLabel("الصنف / الموديل:", 20, y);
            cboProduct = new ComboBox
            {
                Location = new Point(140, y - 4),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboProduct.SelectedIndexChanged += CboProduct_SelectedIndexChanged;
            this.Controls.Add(cboProduct);

            btnSearchProduct = Theme.MakeButton("👗 فحص الموديل", 410, y - 5, 120, 30, Theme.Primary);
            btnSearchProduct.Click += BtnSearchProduct_Click;
            this.Controls.Add(btnSearchProduct);

            y += 42;

            // 3. الكمية والسعر
            AddLabel("الكمية المطلوبة:", 20, y);
            nudQty = new NumericUpDown
            {
                Location = new Point(140, y - 4),
                Width = 100,
                Minimum = 1,
                Maximum = 10000,
                Value = 1,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            nudQty.ValueChanged += (s, e) => RecalcTotals();
            this.Controls.Add(nudQty);

            AddLabel("سعر القطعة:", 260, y);
            nudUnitPrice = new NumericUpDown
            {
                Location = new Point(340, y - 4),
                Width = 110,
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Value = 0,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            nudUnitPrice.ValueChanged += (s, e) => RecalcTotals();
            this.Controls.Add(nudUnitPrice);

            y += 42;

            // 4. الإجمالي والعربون والمتبقي
            lblTotal = new Label
            {
                Text = "إجمالي المبلغ: 0.00 ج",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Success
            };
            this.Controls.Add(lblTotal);

            AddLabel("العربون المدفوع:", 260, y);
            nudDeposit = new NumericUpDown
            {
                Location = new Point(370, y - 4),
                Width = 110,
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
                Text = "المتبقي عند الاستلام: 0.00 ج",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Danger
            };
            this.Controls.Add(lblRemaining);

            AddLabel("تاريخ التوفير المتوقع:", 260, y);
            dtpExpected = new DateTimePicker
            {
                Location = new Point(380, y - 4),
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
                Width = 440,
                Height = 55,
                Multiline = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            };
            this.Controls.Add(txtNotes);

            y += 70;

            // 6. أزرار التحكم
            var btnSave = Theme.MakeButton("💾 حفظ الحجز وتأكيده", 140, y, 190, 42, Theme.Success);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            var btnCancel = Theme.MakeButton("إلغاء", 345, y, 110, 42, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
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
                RecalcTotals();
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
                        RecalcTotals();
                    }
                }
            }
        }

        private void RecalcTotals()
        {
            decimal total = nudQty.Value * nudUnitPrice.Value;
            decimal deposit = nudDeposit.Value;
            if (deposit > total)
            {
                deposit = total;
                nudDeposit.Value = deposit;
            }
            decimal remaining = total - deposit;

            lblTotal.Text = $"إجمالي المبلغ: {total:N2} ج";
            lblRemaining.Text = $"المتبقي عند الاستلام: {remaining:N2} ج";
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

            string productName = cboProduct.Text.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show("يرجى اختيار أو كتابة اسم الصنف/الموديل المطلوب حجزه", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboProduct.Focus();
                return;
            }

            int? clientID = null;
            if (cboClient.SelectedItem is ClientItem ci) clientID = ci.ID;

            decimal qty = nudQty.Value;
            decimal unitPrice = nudUnitPrice.Value;
            decimal total = qty * unitPrice;
            decimal deposit = nudDeposit.Value;
            decimal remaining = total - deposit;

            string resNo = "RES-" + DateTime.Now.ToString("yyMMddHHmmss");

            try
            {
                string sql = @"
                    INSERT INTO CustomerReservations
                    (ReservationNumber, ClientID, ClientName, ClientPhone, ProductID, ProductName, ProductCode, Quantity, UnitPrice, TotalAmount, DepositAmount, RemainingAmount, ExpectedDate, Notes, CreatedBy, Status)
                    VALUES
                    (@no, @cid, @cname, @cphone, @pid, @pname, @pcode, @qty, @uprice, @total, @deposit, @rem, @expected, @notes, @user, N'قيد الانتظار')";

                DbHelper.Execute(sql,
                    DbHelper.P("@no", resNo),
                    DbHelper.P("@cid", (object)clientID ?? DBNull.Value),
                    DbHelper.P("@cname", clientName),
                    DbHelper.P("@cphone", txtClientPhone.Text.Trim()),
                    DbHelper.P("@pid", _selectedProductID > 0 ? (object)_selectedProductID : DBNull.Value),
                    DbHelper.P("@pname", productName),
                    DbHelper.P("@pcode", _selectedProductCode),
                    DbHelper.P("@qty", qty),
                    DbHelper.P("@uprice", unitPrice),
                    DbHelper.P("@total", total),
                    DbHelper.P("@deposit", deposit),
                    DbHelper.P("@rem", remaining),
                    DbHelper.P("@expected", dtpExpected.Value),
                    DbHelper.P("@notes", txtNotes.Text.Trim()),
                    DbHelper.P("@user", Session.EmpName ?? Session.UserName ?? "System")
                );

                // تسويق العربون في الخزينة وحساب العميل إذا وُجد عربون
                if (deposit > 0 && clientID.HasValue && clientID.Value > 0)
                {
                    DbHelper.Execute(@"
                        INSERT INTO ClientTransactions (ClientID, TransDate, TransType, Debit, Credit, Notes, CreatedBy)
                        VALUES (@cid, GETDATE(), N'عربون حجز', 0, @deposit, @notes, @by)",
                        DbHelper.P("@cid", clientID.Value),
                        DbHelper.P("@deposit", deposit),
                        DbHelper.P("@notes", $"عربون حجز صنف: {productName} (حجز #{resNo})"),
                        DbHelper.P("@by", Session.EmpID)
                    );
                }

                MessageBox.Show($"تم تسجيل وتأكيد الحجز بنجاح برقم [{resNo}].", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل حفظ الحجز:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

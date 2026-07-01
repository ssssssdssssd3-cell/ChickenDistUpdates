using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmMaintenanceCard : Form
    {
        private TextBox txtCustomerName;
        private TextBox txtCustomerPhone;
        private TextBox txtDeviceModel;
        private TextBox txtDeviceSerial;
        private TextBox txtProblem;
        private NumericUpDown nudCost;
        private ComboBox cboStatus;
        private TextBox txtNotes;
        private NumericUpDown nudPartsCost;
        private NumericUpDown nudLaborCost;
        private TextBox txtWarrantyPeriod;
        private NumericUpDown nudPrepaidAmount;
        private Button btnSave;
        private Button btnCancel;
        private int _ticketID = 0;

        public FrmMaintenanceCard(int ticketID = 0)
        {
            _ticketID = ticketID;
            InitUI();
            if (_ticketID > 0)
            {
                LoadTicketData();
            }
        }

                private void InitUI()
        {
            bool isCar = AppConfig.BusinessType == "CarService";
            this.Text = _ticketID > 0 ? (isCar ? "\ud83d\udcdd \u062a\u0639\u062f\u064a\u0644 \u0643\u0631\u062a \u0627\u0644\u0635\u064a\u0627\u0646\u0629" : "\ud83d\udcdd \u062a\u0639\u062f\u064a\u0644 \u062a\u0630\u0643\u0631\u0629 \u0627\u0644\u0635\u064a\u0627\u0646\u0629") : (isCar ? "\u0625\u0636\u0627\u0641\u0629 \u0643\u0631\u062a \u0635\u064a\u0627\u0646\u0629 \u062c\u062f\u064a\u062f" : "\u0625\u0636\u0627\u0641\u0629 \u062a\u0630\u0643\u0631\u0629 \u0635\u064a\u0627\u0646\u0629 \u062c\u062f\u064a\u062f\u0629");
            this.Size = new Size(480, 580);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;

            var pnlTitle = Theme.MakeTitleBar(_ticketID > 0 ? (isCar ? "\ud83d\udcdd \u062a\u0639\u062f\u064a\u0644 \u0643\u0631\u062a \u0627\u0644\u0635\u064a\u0627\u0646\u0629" : "\ud83d\udcdd \u062a\u0639\u062f\u064a\u0644 \u062a\u0630\u0643\u0631\u0629 \u0627\u0644\u0635\u064a\u0627\u0646\u0629") : (isCar ? "\u2795 \u0625\u0636\u0627\u0641\u0629 \u0643\u0631\u062a \u0635\u064a\u0627\u0646\u0629" : "\u2795 \u0625\u0636\u0627\u0641\u0629 \u062a\u0630\u0643\u0631\u0629 \u0635\u064a\u0627\u0646\u0629"), isCar ? "\u0625\u062f\u062e\u0627\u0644 \u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0633\u062a\u0644\u0627\u0645 \u0635\u064a\u0627\u0646\u0629 \u0627\u0644\u0633\u064a\u0627\u0631\u0629" : "\u0625\u062f\u062e\u0627\u0644 \u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u062c\u0647\u0627\u0632 \u0648\u0627\u0644\u0639\u0645\u064a\u0644 \u0648\u062a\u0641\u0627\u0635\u064a\u0644 \u0627\u0644\u0635\u064a\u0627\u0646\u0629");
            this.Controls.Add(pnlTitle);

            int y = 80;

            // --- Customer Name & Customer Phone (Side by Side) ---
            var lblCustName = new Label { Text = "اسم العميل *:", Location = new Point(20, y), Width = 190, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            var lblCustPhone = new Label { Text = "رقم الهاتف:", Location = new Point(230, y), Width = 210, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.AddRange(new Control[] { lblCustName, lblCustPhone });
            y += 22;

            var btnSearchClient = new Button
            {
                Text = "ðŸ”",
                Location = new Point(20, y),
                Size = new Size(35, 23),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSearchClient.FlatAppearance.BorderSize = 0;
            btnSearchClient.Click += (s, e) =>
            {
                using (var frm = new FrmClientSearch())
                {
                    if (frm.ShowDialog() == DialogResult.OK && frm.SelectedClientID > 0)
                    {
                        DataTable dt = DbHelper.Query("SELECT ClientName, Phone FROM Clients WHERE ClientID = @cid", DbHelper.P("@cid", frm.SelectedClientID));
                        if (dt.Rows.Count > 0)
                        {
                            txtCustomerName.Text = dt.Rows[0]["ClientName"]?.ToString() ?? "";
                            txtCustomerPhone.Text = dt.Rows[0]["Phone"]?.ToString() ?? "";
                        }
                    }
                }
            };
            this.Controls.Add(btnSearchClient);

            txtCustomerName = new TextBox { Location = new Point(60, y), Width = 150, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            txtCustomerPhone = new TextBox { Location = new Point(230, y), Width = 210, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.AddRange(new Control[] { txtCustomerName, txtCustomerPhone });
            y += 35;

                        // --- Device Model & Device Serial (Side by Side) ---
            var lblDevModel = new Label { Text = isCar ? "\u0646\u0648\u0639 \u0648\u0645\u0648\u062f\u064a\u0644 \u0627\u0644\u0633\u064a\u0627\u0631\u0629 *:" : "\u0646\u0648\u0639 \u0648\u0645\u0648\u062f\u064a\u0644 \u0627\u0644\u062c\u0647\u0627\u0632 *:", Location = new Point(20, y), Width = 190, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            var lblDevSerial = new Label { Text = isCar ? "\u0631\u0642\u0645 \u0627\u0644\u0644\u0648\u062d\u0629 / \u0627\u0644\u0634\u0627\u0633\u064a\u0647:" : "\u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u062a\u0633\u0644\u0633\u064a / IMEI:", Location = new Point(230, y), Width = 210, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.AddRange(new Control[] { lblDevModel, lblDevSerial });
            y += 22;

            txtDeviceModel = new TextBox { Location = new Point(20, y), Width = 190, BackColor = Theme.BgInput, ForeColor = Theme.TextDark, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            txtDeviceSerial = new TextBox { Location = new Point(230, y), Width = 210, BackColor = Theme.BgInput, ForeColor = Theme.TextDark, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.AddRange(new Control[] { txtDeviceModel, txtDeviceSerial });
            y += 35;

            // --- Problem ---
            var lblProblem = new Label { Text = isCar ? "\u0627\u0644\u062e\u062f\u0645\u0629 \u0627\u0644\u0645\u0637\u0644\u0648\u0628\u0629 \u0628\u0627\u0644\u062a\u0641\u0635\u064a\u0644 *:" : "\u0627\u0644\u0639\u0637\u0644 / \u0627\u0644\u0645\u0634\u0643\u0644\u0629 \u0628\u0627\u0644\u062a\u0641\u0635\u064a\u0644 *:", Location = new Point(20, y), Width = 420, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.Add(lblProblem);
            y += 22;

            txtProblem = new TextBox { Location = new Point(20, y), Width = 420, Height = 45, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtProblem);
            y += 50;

            // --- Parts Cost & Labor Cost (Side by Side) ---
            var lblParts = new Label { Text = "تكلفة قطع الغيار (ج):", Location = new Point(20, y), Width = 190, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            var lblLabor = new Label { Text = "أجرة اليد / المصنعية (ج):", Location = new Point(230, y), Width = 210, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.AddRange(new Control[] { lblParts, lblLabor });
            y += 22;

            nudPartsCost = new NumericUpDown 
            { 
                Location = new Point(20, y), 
                Width = 190, 
                Maximum = 100000, 
                DecimalPlaces = 2, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                Font = Theme.FontNormal
            };
            nudPartsCost.ValueChanged += (s, e) => UpdateTotalCost();
            this.Controls.Add(nudPartsCost);

            nudLaborCost = new NumericUpDown 
            { 
                Location = new Point(230, y), 
                Width = 210, 
                Maximum = 100000, 
                DecimalPlaces = 2, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                Font = Theme.FontNormal
            };
            nudLaborCost.ValueChanged += (s, e) => UpdateTotalCost();
            this.Controls.Add(nudLaborCost);
            y += 35;

            // --- Total Cost & Status (Side by Side) ---
            var lblCost = new Label { Text = "إجمالي تكلفة الإصلاح (ج):", Location = new Point(20, y), Width = 190, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            var lblStatus = new Label { Text = "حالة الإصلاح:", Location = new Point(230, y), Width = 210, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.AddRange(new Control[] { lblCost, lblStatus });
            y += 22;

            nudCost = new NumericUpDown 
            { 
                Location = new Point(20, y), 
                Width = 190, 
                Maximum = 100000, 
                DecimalPlaces = 2, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                Font = Theme.FontNormal,
                Enabled = false
            };
            this.Controls.Add(nudCost);

            cboStatus = new ComboBox 
            { 
                Location = new Point(230, y), 
                Width = 210, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextDark,
                Font = Theme.FontNormal
            };
            cboStatus.Items.AddRange(new object[] { "قيد الإصلاح", "تم الإصلاح - جاهز", "تم التسليم", "ملغي" });
            cboStatus.SelectedIndex = 0;
            this.Controls.Add(cboStatus);
            y += 35;

            // --- Prepaid Amount & Warranty Period (Side by Side) ---
            var lblPrepaid = new Label { Text = "المدفوع مقدماً / العربون (ج):", Location = new Point(20, y), Width = 190, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            var lblWarranty = new Label { Text = "مدة الضمان بعد الإصلاح:", Location = new Point(230, y), Width = 210, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.AddRange(new Control[] { lblPrepaid, lblWarranty });
            y += 22;

            nudPrepaidAmount = new NumericUpDown
            {
                Location = new Point(20, y),
                Width = 190,
                Maximum = 100000,
                DecimalPlaces = 2,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontNormal
            };
            this.Controls.Add(nudPrepaidAmount);

            txtWarrantyPeriod = new TextBox 
            { 
                Location = new Point(230, y), 
                Width = 210, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                BorderStyle = BorderStyle.FixedSingle, 
                Font = Theme.FontNormal 
            };
            txtWarrantyPeriod.Text = "بدون ضمان";
            this.Controls.Add(txtWarrantyPeriod);
            y += 35;

            // --- Notes (Wide) ---
            var lblNotes = new Label { Text = "ملاحظات إضافية:", Location = new Point(20, y), Width = 420, Height = 18, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.Add(lblNotes);
            y += 22;

            txtNotes = new TextBox 
            { 
                Location = new Point(20, y), 
                Width = 420, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                BorderStyle = BorderStyle.FixedSingle, 
                Font = Theme.FontNormal 
            };
            this.Controls.Add(txtNotes);
            y += 45;

            // أزرار الحفظ والإلغاء
            btnSave = Theme.MakeButton("💾 حفظ", 260, y, 180, 36, Theme.Accent);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = Theme.MakeButton("❌ إلغاء", 60, y, 180, 36, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void UpdateTotalCost()
        {
            if (nudPartsCost != null && nudLaborCost != null && nudCost != null)
            {
                nudCost.Value = nudPartsCost.Value + nudLaborCost.Value;
            }
        }

        private void AddLabel(string text, int x, ref int y)
        {
            var lbl = new Label 
            { 
                Text = text, 
                Location = new Point(x, y), 
                Width = 300, 
                Height = 18, 
                ForeColor = Theme.TextMain, 
                Font = Theme.FontBold, 
                TextAlign = ContentAlignment.TopRight 
            };
            this.Controls.Add(lbl);
            y += 22;
        }

        private void LoadTicketData()
        {
            try
            {
                DataTable dt = DbHelper.Query("SELECT * FROM MaintenanceTickets WHERE TicketID = @tid", DbHelper.P("@tid", _ticketID));
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    txtCustomerName.Text = r["CustomerName"]?.ToString() ?? "";
                    txtCustomerPhone.Text = r["CustomerPhone"]?.ToString() ?? "";
                    txtDeviceModel.Text = r["DeviceModel"]?.ToString() ?? "";
                    txtDeviceSerial.Text = r["DeviceSerial"]?.ToString() ?? "";
                    txtProblem.Text = r["Problem"]?.ToString() ?? "";
                    nudPartsCost.Value = r.Table.Columns.Contains("PartsCost") && r["PartsCost"] != DBNull.Value ? Convert.ToDecimal(r["PartsCost"]) : 0m;
                    nudLaborCost.Value = r.Table.Columns.Contains("LaborCost") && r["LaborCost"] != DBNull.Value ? Convert.ToDecimal(r["LaborCost"]) : 0m;
                    nudCost.Value = Convert.ToDecimal(r["Cost"]);
                    cboStatus.SelectedItem = r["Status"]?.ToString() ?? "قيد الإصلاح";
                    if (cboStatus.SelectedItem?.ToString() == "تم التسليم")
                    {
                        cboStatus.Enabled = false;
                    }
                    nudPrepaidAmount.Value = r.Table.Columns.Contains("PrepaidAmount") && r["PrepaidAmount"] != DBNull.Value ? Convert.ToDecimal(r["PrepaidAmount"]) : 0m;
                    txtWarrantyPeriod.Text = r.Table.Columns.Contains("WarrantyPeriod") && r["WarrantyPeriod"] != DBNull.Value ? r["WarrantyPeriod"].ToString() : "بدون ضمان";
                    txtNotes.Text = r["Notes"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل البيانات: " + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCustomerName.Text)) { MessageBox.Show("يرجى إدخال اسم العميل"); return; }
                        bool isCar = AppConfig.BusinessType == "CarService";
            if (string.IsNullOrWhiteSpace(txtDeviceModel.Text)) { MessageBox.Show(isCar ? "\u064a\u0631\u062c\u0649 \u0625\u062f\u062e\u0627\u0644 \u0646\u0648\u0639 \u0648\u0645\u0648\u062f\u064a\u0644 \u0627\u0644\u0633\u064a\u0627\u0631\u0629" : "\u064a\u0631\u062c\u0649 \u0625\u062f\u062e\u0627\u0644 \u0646\u0648\u0639 \u0648\u0645\u0648\u062f\u064a\u0644 \u0627\u0644\u062c\u0647\u0627\u0632"); return; }
            if (string.IsNullOrWhiteSpace(txtProblem.Text)) { MessageBox.Show(isCar ? "\u064a\u0631\u062c\u0649 \u0648\u0635\u0641 \u0627\u0644\u062e\u062f\u0645\u0629 \u0627\u0644\u0645\u0637\u0644\u0648\u0628\u0629" : "\u064a\u0631\u062c\u0649 \u0648\u0635\u0641 \u0627\u0644\u0645\u0634\u0643\u0644\u0629 \u0623\u0648 \u0627\u0644\u0639\u0637\u0644"); return; }

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    bool shouldLogIncome = false;
                    decimal cost = nudCost.Value;
                    decimal newPrepaid = nudPrepaidAmount.Value;

                    if (_ticketID == 0)
                    {
                        if (cboStatus.SelectedItem.ToString() == "تم التسليم" && cost > 0)
                        {
                            shouldLogIncome = true;
                        }

                        int ticketID = DbHelper.ExecuteInsertTrans(trans, @"
                            INSERT INTO MaintenanceTickets (CustomerName, CustomerPhone, DeviceModel, DeviceSerial, Problem, Cost, Status, Notes, PartsCost, LaborCost, WarrantyPeriod, PrepaidAmount)
                            VALUES (@name, @phone, @model, @serial, @prob, @cost, @status, @notes, @parts, @labor, @warranty, @prepaid)",
                            DbHelper.P("@name", txtCustomerName.Text.Trim()),
                            DbHelper.P("@phone", txtCustomerPhone.Text.Trim()),
                            DbHelper.P("@model", txtDeviceModel.Text.Trim()),
                            DbHelper.P("@serial", txtDeviceSerial.Text.Trim()),
                            DbHelper.P("@prob", txtProblem.Text.Trim()),
                            DbHelper.P("@cost", cost),
                            DbHelper.P("@status", cboStatus.SelectedItem.ToString()),
                            DbHelper.P("@notes", txtNotes.Text.Trim()),
                            DbHelper.P("@parts", nudPartsCost.Value),
                            DbHelper.P("@labor", nudLaborCost.Value),
                            DbHelper.P("@warranty", txtWarrantyPeriod.Text.Trim()),
                            DbHelper.P("@prepaid", newPrepaid));

                        if (ticketID > 0)
                        {
                            // Log the initial prepaid deposit
                            if (newPrepaid > 0)
                            {
                                DbHelper.ExecuteTrans(trans, @"
                                    INSERT INTO CashBox (TransDate, TransType, Notes, AmountIn, AmountOut, RefID, CreatedBy, AccountID)
                                    VALUES (GETDATE(), 'MaintenanceDeposit', @notes, @amt, 0, @ref, @emp, 1)",
                                    DbHelper.P("@notes", $"عربون صيانة تذكرة #{ticketID} - جهاز {txtDeviceModel.Text.Trim()}"),
                                    DbHelper.P("@amt", newPrepaid),
                                    DbHelper.P("@ref", ticketID),
                                    DbHelper.P("@emp", Session.EmpID));
                            }

                            // Log final collection if delivered
                            if (shouldLogIncome)
                            {
                                decimal remainingAmt = cost - newPrepaid;
                                if (remainingAmt > 0)
                                {
                                    DbHelper.ExecuteTrans(trans, @"
                                        INSERT INTO CashBox (TransDate, TransType, Notes, AmountIn, AmountOut, RefID, CreatedBy, AccountID)
                                        VALUES (GETDATE(), 'Maintenance', @notes, @amt, 0, @ref, @emp, 1)",
                                        DbHelper.P("@notes", $"تحصيل نهائي صيانة تذكرة #{ticketID} - جهاز {txtDeviceModel.Text.Trim()}"),
                                        DbHelper.P("@amt", remainingAmt),
                                        DbHelper.P("@ref", ticketID),
                                        DbHelper.P("@emp", Session.EmpID));
                                }
                            }
                        }
                    }
                    else
                    {
                        object oldStatusObj = DbHelper.ScalarTrans(trans, "SELECT Status FROM MaintenanceTickets WHERE TicketID = @tid", DbHelper.P("@tid", _ticketID));
                        string oldStatus = oldStatusObj != null ? oldStatusObj.ToString() : "";

                        object opObj = DbHelper.ScalarTrans(trans, "SELECT PrepaidAmount FROM MaintenanceTickets WHERE TicketID = @tid", DbHelper.P("@tid", _ticketID));
                        decimal oldPrepaid = opObj != null ? Convert.ToDecimal(opObj) : 0m;

                        if (oldStatus != "تم التسليم" && cboStatus.SelectedItem.ToString() == "تم التسليم" && cost > 0)
                        {
                            shouldLogIncome = true;
                        }

                        DbHelper.ExecuteTrans(trans, @"
                            UPDATE MaintenanceTickets 
                            SET CustomerName = @name, CustomerPhone = @phone, DeviceModel = @model, DeviceSerial = @serial, 
                                Problem = @prob, Cost = @cost, Status = @status, Notes = @notes,
                                PartsCost = @parts, LaborCost = @labor, WarrantyPeriod = @warranty, PrepaidAmount = @prepaid
                            WHERE TicketID = @tid",
                            DbHelper.P("@name", txtCustomerName.Text.Trim()),
                            DbHelper.P("@phone", txtCustomerPhone.Text.Trim()),
                            DbHelper.P("@model", txtDeviceModel.Text.Trim()),
                            DbHelper.P("@serial", txtDeviceSerial.Text.Trim()),
                            DbHelper.P("@prob", txtProblem.Text.Trim()),
                            DbHelper.P("@cost", cost),
                            DbHelper.P("@status", cboStatus.SelectedItem.ToString()),
                            DbHelper.P("@notes", txtNotes.Text.Trim()),
                            DbHelper.P("@parts", nudPartsCost.Value),
                            DbHelper.P("@labor", nudLaborCost.Value),
                            DbHelper.P("@warranty", txtWarrantyPeriod.Text.Trim()),
                            DbHelper.P("@prepaid", newPrepaid),
                            DbHelper.P("@tid", _ticketID));

                        // Log deposit updates
                        decimal prepaidDiff = newPrepaid - oldPrepaid;
                        if (prepaidDiff != 0)
                        {
                            if (prepaidDiff > 0)
                            {
                                DbHelper.ExecuteTrans(trans, @"
                                    INSERT INTO CashBox (TransDate, TransType, Notes, AmountIn, AmountOut, RefID, CreatedBy, AccountID)
                                    VALUES (GETDATE(), 'MaintenanceDeposit', @notes, @amt, 0, @ref, @emp, 1)",
                                    DbHelper.P("@notes", $"تعديل عربون (زيادة) تذكرة #{_ticketID} - جهاز {txtDeviceModel.Text.Trim()}"),
                                    DbHelper.P("@amt", prepaidDiff),
                                    DbHelper.P("@ref", _ticketID),
                                    DbHelper.P("@emp", Session.EmpID));
                            }
                            else
                            {
                                DbHelper.ExecuteTrans(trans, @"
                                    INSERT INTO CashBox (TransDate, TransType, Notes, AmountIn, AmountOut, RefID, CreatedBy, AccountID)
                                    VALUES (GETDATE(), 'MaintenanceDepositRefund', @notes, 0, @amt, @ref, @emp, 1)",
                                    DbHelper.P("@notes", $"تعديل عربون (استرداد) تذكرة #{_ticketID} - جهاز {txtDeviceModel.Text.Trim()}"),
                                    DbHelper.P("@amt", -prepaidDiff),
                                    DbHelper.P("@ref", _ticketID),
                                    DbHelper.P("@emp", Session.EmpID));
                            }
                        }

                        // Log final collection if newly delivered
                        if (shouldLogIncome)
                        {
                            decimal remainingAmt = cost - newPrepaid;
                            if (remainingAmt > 0)
                            {
                                DbHelper.ExecuteTrans(trans, @"
                                    INSERT INTO CashBox (TransDate, TransType, Notes, AmountIn, AmountOut, RefID, CreatedBy, AccountID)
                                    VALUES (GETDATE(), 'Maintenance', @notes, @amt, 0, @ref, @emp, 1)",
                                    DbHelper.P("@notes", $"تحصيل نهائي صيانة تذكرة #{_ticketID} - جهاز {txtDeviceModel.Text.Trim()}"),
                                    DbHelper.P("@amt", remainingAmt),
                                    DbHelper.P("@ref", _ticketID),
                                    DbHelper.P("@emp", Session.EmpID));
                            }
                        }
                    }
                });

                MessageBox.Show("✅ تم الحفظ بنجاح وتسجيل الحركة بالخزينة");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ فشل الحفظ: " + ex.Message);
            }
        }
    }
}

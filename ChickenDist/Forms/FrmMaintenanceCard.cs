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
            this.Text = _ticketID > 0 ? "تعديل تذكرة الصيانة" : "إضافة تذكرة صيانة جديدة";
            this.Size = new Size(480, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTitle = Theme.MakeTitleBar(_ticketID > 0 ? "📝 تعديل تذكرة صيانة" : "➕ إضافة تذكرة صيانة", "إدخال بيانات الجهاز والعميل وتكلفة وحالة الإصلاح");
            this.Controls.Add(pnlTitle);

            int y = 80;

            // اسم العميل
            AddLabel("اسم العميل *:", 20, ref y);
            txtCustomerName = new TextBox { Location = new Point(20, y), Width = 420, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtCustomerName);
            y += 35;

            // رقم الهاتف
            AddLabel("رقم الهاتف:", 20, ref y);
            txtCustomerPhone = new TextBox { Location = new Point(20, y), Width = 420, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtCustomerPhone);
            y += 35;

            // نوع الجهاز
            AddLabel("نوع وموديل الجهاز *:", 20, ref y);
            txtDeviceModel = new TextBox { Location = new Point(20, y), Width = 420, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtDeviceModel);
            y += 35;

            // الرقم التسلسلي / IMEI
            AddLabel("الرقم التسلسلي / IMEI:", 20, ref y);
            txtDeviceSerial = new TextBox { Location = new Point(20, y), Width = 420, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtDeviceSerial);
            y += 35;

            // المشكلة / العطل
            AddLabel("العطل / المشكلة بالتفصيل *:", 20, ref y);
            txtProblem = new TextBox { Location = new Point(20, y), Width = 420, Height = 60, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtProblem);
            y += 75;

            // تكلفة الإصلاح
            AddLabel("تكلفة الإصلاح المقدرة (ج):", 20, ref y);
            nudCost = new NumericUpDown 
            { 
                Location = new Point(20, y), 
                Width = 190, 
                Maximum = 100000, 
                DecimalPlaces = 2, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain,
                Font = Theme.FontNormal
            };
            this.Controls.Add(nudCost);

            // حالة التذكرة
            var lblStatus = new Label { Text = "حالة الإصلاح:", Location = new Point(230, y - 22), Width = 150, ForeColor = Theme.TextMain, Font = Theme.FontBold, TextAlign = ContentAlignment.TopRight };
            this.Controls.Add(lblStatus);

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

            // ملاحظات إضافية
            AddLabel("ملاحظات إضافية:", 20, ref y);
            txtNotes = new TextBox { Location = new Point(20, y), Width = 420, Height = 50, Multiline = true, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontNormal };
            this.Controls.Add(txtNotes);
            y += 65;

            // أزرار الحفظ والإلغاء
            btnSave = Theme.MakeButton("💾 حفظ", 260, y, 180, 36, Theme.Accent);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = Theme.MakeButton("❌ إلغاء", 60, y, 180, 36, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
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
                    nudCost.Value = Convert.ToDecimal(r["Cost"]);
                    cboStatus.SelectedItem = r["Status"]?.ToString() ?? "قيد الإصلاح";
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
            if (string.IsNullOrWhiteSpace(txtDeviceModel.Text)) { MessageBox.Show("يرجى إدخال نوع وموديل الجهاز"); return; }
            if (string.IsNullOrWhiteSpace(txtProblem.Text)) { MessageBox.Show("يرجى وصف المشكلة أو العطل"); return; }

            try
            {
                if (_ticketID == 0)
                {
                    // إدراج تذكرة جديدة
                    DbHelper.ExecuteInsert(@"
                        INSERT INTO MaintenanceTickets (CustomerName, CustomerPhone, DeviceModel, DeviceSerial, Problem, Cost, Status, Notes)
                        VALUES (@name, @phone, @model, @serial, @prob, @cost, @status, @notes)",
                        DbHelper.P("@name", txtCustomerName.Text.Trim()),
                        DbHelper.P("@phone", txtCustomerPhone.Text.Trim()),
                        DbHelper.P("@model", txtDeviceModel.Text.Trim()),
                        DbHelper.P("@serial", txtDeviceSerial.Text.Trim()),
                        DbHelper.P("@prob", txtProblem.Text.Trim()),
                        DbHelper.P("@cost", nudCost.Value),
                        DbHelper.P("@status", cboStatus.SelectedItem.ToString()),
                        DbHelper.P("@notes", txtNotes.Text.Trim())
                    );
                }
                else
                {
                    // تحديث تذكرة موجودة
                    DbHelper.Execute(@"
                        UPDATE MaintenanceTickets 
                        SET CustomerName = @name, CustomerPhone = @phone, DeviceModel = @model, DeviceSerial = @serial, 
                            Problem = @prob, Cost = @cost, Status = @status, Notes = @notes
                        WHERE TicketID = @tid",
                        DbHelper.P("@name", txtCustomerName.Text.Trim()),
                        DbHelper.P("@phone", txtCustomerPhone.Text.Trim()),
                        DbHelper.P("@model", txtDeviceModel.Text.Trim()),
                        DbHelper.P("@serial", txtDeviceSerial.Text.Trim()),
                        DbHelper.P("@prob", txtProblem.Text.Trim()),
                        DbHelper.P("@cost", nudCost.Value),
                        DbHelper.P("@status", cboStatus.SelectedItem.ToString()),
                        DbHelper.P("@notes", txtNotes.Text.Trim()),
                        DbHelper.P("@tid", _ticketID)
                    );
                }

                MessageBox.Show("✅ تم الحفظ بنجاح");
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

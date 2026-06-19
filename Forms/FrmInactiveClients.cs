using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmInactiveClients : Form
    {
        private Panel pnlHeader;
        private NumericUpDown nudDays;
        private Button btnLoad;
        private DataGridView dgClients;
        private TextBox txtMessageTemplate;
        private Button btnSendWhatsApp;

        public FrmInactiveClients()
        {
            InitUI();
            LoadInactiveClients();
        }

        private void InitUI()
        {
            this.Text = "العملاء الرواكد (تنشيط المبيعات)";
            this.Size = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Title Bar
            var pnlTitle = Theme.MakeTitleBar("📢 تنشيط العملاء الرواكد", "عرض العملاء الذين انقطعوا عن الشراء لفترة وإرسال رسائل واتساب سريعة لهم");
            this.Controls.Add(pnlTitle);

            // Filter Panel
            var pnlFilter = new Panel
            {
                Location = new Point(12, 75),
                Size = new Size(910, 50),
                BackColor = Theme.BgCard,
                Padding = new Padding(6)
            };

            var lblDays = new Label
            {
                Text = "العملاء الذين لم يشتروا منذ (يوم):",
                Location = new Point(680, 16),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            pnlFilter.Controls.Add(lblDays);

            nudDays = new NumericUpDown
            {
                Location = new Point(550, 12),
                Width = 120,
                Minimum = 1,
                Maximum = 9999,
                Value = 30,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            pnlFilter.Controls.Add(nudDays);

            btnLoad = Theme.MakeButton("🔍 عرض العملاء", 400, 10, 130, 30, Theme.Primary);
            btnLoad.Click += (s, e) => LoadInactiveClients();
            pnlFilter.Controls.Add(btnLoad);

            this.Controls.Add(pnlFilter);

            // Grid Layout
            dgClients = new DataGridView
            {
                Location = new Point(12, 135),
                Size = new Size(910, 280),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientID", Visible = false });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientCode", HeaderText = "كود العميل", FillWeight = 40 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "اسم العميل", FillWeight = 120 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "رقم الهاتف", FillWeight = 80 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastSaleDate", HeaderText = "تاريخ آخر شراء", FillWeight = 80 });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد الحالي", FillWeight = 60 });

            this.Controls.Add(dgClients);

            // Bottom Panel for WhatsApp template & button
            var pnlActions = new Panel
            {
                Location = new Point(12, 425),
                Size = new Size(910, 125),
                BackColor = Theme.BgCard,
                Padding = new Padding(8)
            };

            var lblTemplate = new Label
            {
                Text = "نص رسالة التنشيط (يمكنك التعديل):",
                Location = new Point(550, 10),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            pnlActions.Controls.Add(lblTemplate);

            txtMessageTemplate = new TextBox
            {
                Location = new Point(200, 35),
                Width = 690,
                Height = 75,
                Multiline = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Text = "السلام عليكم يا أستاذ {العميل}، نأمل أن تكون بخير وبصحة جيدة. افتقدنا تعاملكم معنا في {الشركة} ونحب أن نطمئن عليكم. يسعدنا دائماً تواصلكم وتلبية طلباتكم في أي وقت!"
            };
            pnlActions.Controls.Add(txtMessageTemplate);

            btnSendWhatsApp = Theme.MakeButton("📱 إرسال واتساب", 15, 35, 170, 75, Theme.Success);
            btnSendWhatsApp.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnSendWhatsApp.Click += BtnSendWhatsApp_Click;
            pnlActions.Controls.Add(btnSendWhatsApp);

            this.Controls.Add(pnlActions);

            Theme.ApplyFormRTL(this);
        }

        private void LoadInactiveClients()
        {
            try
            {
                dgClients.Rows.Clear();
                int days = (int)nudDays.Value;

                string sql = @"
                    SELECT c.ClientID, c.ClientCode, c.ClientName, c.Phone,
                           (SELECT MAX(SaleDate) FROM Sales WHERE ClientID = c.ClientID) AS LastSaleDate,
                           ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                    FROM Clients c
                    LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                    WHERE c.IsActive = 1
                      AND NOT EXISTS (
                          SELECT 1 FROM Sales 
                          WHERE ClientID = c.ClientID 
                            AND SaleDate >= DATEADD(day, -@days, GETDATE())
                      )
                    ORDER BY LastSaleDate ASC";

                DataTable dt = DbHelper.Query(sql, DbHelper.P("@days", days));
                foreach (DataRow r in dt.Rows)
                {
                    string lastSaleStr = r["LastSaleDate"] != DBNull.Value 
                        ? Convert.ToDateTime(r["LastSaleDate"]).ToString("dd/MM/yyyy") 
                        : "لا يوجد مبيعات سابقة";
                    decimal balance = Convert.ToDecimal(r["Balance"]);

                    dgClients.Rows.Add(r["ClientID"], r["ClientCode"], r["ClientName"], r["Phone"], lastSaleStr, balance.ToString("N2") + " ج");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل العملاء الرواكد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSendWhatsApp_Click(object sender, EventArgs e)
        {
            if (dgClients.SelectedRows.Count == 0)
            {
                MessageBox.Show("الرجاء اختيار عميل من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgClients.SelectedRows[0];
            string clientName = row.Cells["ClientName"].Value.ToString();
            string phone = row.Cells["Phone"].Value.ToString().Trim();

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("هذا العميل لا يمتلك رقم هاتف مسجل!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // تخصيص الرسالة
            string msg = txtMessageTemplate.Text
                .Replace("{العميل}", clientName)
                .Replace("{ClientName}", clientName)
                .Replace("{الشركة}", AppConfig.CompanyName)
                .Replace("{CompanyName}", AppConfig.CompanyName);

            try
            {
                string clean = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
                if (clean.StartsWith("0")) clean = "20" + clean.Substring(1);

                string encoded = Uri.EscapeDataString(msg);
                string waUrl = $"https://wa.me/{clean}?text={encoded}";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(waUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل فتح واتساب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

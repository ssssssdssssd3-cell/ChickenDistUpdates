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
        private ComboBox cboTransType;
        private CheckBox chkSelectAll;

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

            var lblTransType = new Label
            {
                Text = "نوع الحركة:",
                Location = new Point(295, 16),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            pnlFilter.Controls.Add(lblTransType);

            cboTransType = new ComboBox
            {
                Location = new Point(145, 12),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            cboTransType.Items.AddRange(new string[] { "بيع (مبيعات)", "توريد (تحصيلات)", "مرتجع (مرتجعات)" });
            cboTransType.SelectedIndex = 0;
            cboTransType.SelectedIndexChanged += (s, e) => LoadInactiveClients();
            pnlFilter.Controls.Add(cboTransType);

            chkSelectAll = new CheckBox
            {
                Text = "تحديد الكل",
                Location = new Point(15, 14),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            chkSelectAll.CheckedChanged += (s, e) =>
            {
                foreach (DataGridViewRow row in dgClients.Rows)
                {
                    row.Cells["SelectCol"].Value = chkSelectAll.Checked;
                }
            };
            pnlFilter.Controls.Add(chkSelectAll);

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
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
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
            dgClients.Columns.Add(new DataGridViewCheckBoxColumn { Name = "SelectCol", HeaderText = "تحديد", FillWeight = 30, ReadOnly = false });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientID", Visible = false, ReadOnly = true });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientCode", HeaderText = "كود العميل", FillWeight = 40, ReadOnly = true });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "اسم العميل", FillWeight = 120, ReadOnly = true });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "رقم الهاتف", FillWeight = 80, ReadOnly = true });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastSaleDate", HeaderText = "تاريخ آخر شراء", FillWeight = 80, ReadOnly = true });
            dgClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد الحالي", FillWeight = 60, ReadOnly = true });

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

                string transType = "Sale";
                if (cboTransType.SelectedIndex == 1) transType = "Payment";
                else if (cboTransType.SelectedIndex == 2) transType = "Return";

                string sql = @"
                    SELECT c.ClientID, c.ClientCode, c.ClientName, c.Phone,
                           (SELECT MAX(TransDate) FROM ClientTransactions WHERE ClientID = c.ClientID AND TransType = @transType) AS LastSaleDate,
                           ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                    FROM Clients c
                    LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                    WHERE c.IsActive = 1
                      AND NOT EXISTS (
                          SELECT 1 FROM ClientTransactions 
                          WHERE ClientID = c.ClientID 
                            AND TransType = @transType
                            AND TransDate >= DATEADD(day, -@days, GETDATE())
                      )
                    ORDER BY LastSaleDate ASC";

                DataTable dt = DbHelper.Query(sql, DbHelper.P("@days", days), DbHelper.P("@transType", transType));

                if (cboTransType.SelectedIndex == 0)
                    dgClients.Columns["LastSaleDate"].HeaderText = "تاريخ آخر شراء";
                else if (cboTransType.SelectedIndex == 1)
                    dgClients.Columns["LastSaleDate"].HeaderText = "تاريخ آخر توريد";
                else if (cboTransType.SelectedIndex == 2)
                    dgClients.Columns["LastSaleDate"].HeaderText = "تاريخ آخر مرتجع";

                string lastDateLabel = "لا يوجد حركة سابقة";
                if (transType == "Sale") lastDateLabel = "لا يوجد مبيعات سابقة";
                else if (transType == "Payment") lastDateLabel = "لا يوجد توريدات سابقة";
                else if (transType == "Return") lastDateLabel = "لا يوجد مرتجعات سابقة";

                foreach (DataRow r in dt.Rows)
                {
                    string lastSaleStr = r["LastSaleDate"] != DBNull.Value 
                        ? Convert.ToDateTime(r["LastSaleDate"]).ToString("dd/MM/yyyy") 
                        : lastDateLabel;
                    decimal balance = Convert.ToDecimal(r["Balance"]);

                    dgClients.Rows.Add(false, r["ClientID"], r["ClientCode"], r["ClientName"], r["Phone"], lastSaleStr, balance.ToString("N2") + " ج");
                }

                chkSelectAll.Checked = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل العملاء الرواكد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSendWhatsApp_Click(object sender, EventArgs e)
        {
            var selectedClients = new System.Collections.Generic.List<DataGridViewRow>();
            foreach (DataGridViewRow row in dgClients.Rows)
            {
                if (row.Cells["SelectCol"].Value != null && (bool)row.Cells["SelectCol"].Value)
                {
                    selectedClients.Add(row);
                }
            }

            if (selectedClients.Count == 0 && dgClients.SelectedRows.Count > 0)
            {
                selectedClients.Add(dgClients.SelectedRows[0]);
            }

            if (selectedClients.Count == 0)
            {
                MessageBox.Show("الرجاء تحديد عميل واحد على الأقل عبر علامة الصح أو اختيار سطر من الجدول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedClients.Count > 1)
            {
                var confirm = MessageBox.Show($"هل أنت متأكد من إرسال رسائل تنشيط لعدد {selectedClients.Count} من العملاء؟\nسيتم فتح محادثات واتساب بالتوالي.", "تأكيد الإرسال المجمع", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;
            }

            int sendCount = 0;
            foreach (var row in selectedClients)
            {
                string clientName = row.Cells["ClientName"].Value?.ToString();
                string phone = row.Cells["Phone"].Value?.ToString().Trim();

                if (string.IsNullOrWhiteSpace(phone))
                {
                    continue;
                }

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
                    sendCount++;
                }
                catch (Exception ex)
                {
                    AppLogger.Error("فشل فتح واتساب للعميل " + clientName, ex, "FrmInactiveClients");
                }
            }

            if (selectedClients.Count > 1)
            {
                MessageBox.Show($"تم فتح محادثات واتساب لـ {sendCount} عميل بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

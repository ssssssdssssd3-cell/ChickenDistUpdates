using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmInstallments : Form
    {
        private int _currentPage = 1;
        private const int PAGE_SIZE = 15;
        private int _totalPages = 1;
        private int _selectedContractID = 0;

        private Label lblActiveCount;
        private Label lblRemainingVal;
        private Label lblDueTodayVal;
        private Label lblOverdueVal;
        private Label lblCollectedTodayVal;

        private ComboBox cboSearchClient;
        private ComboBox cboSearchStatus;
        private TextBox txtSearchCode;
        private Button btnSearch;
        private Button btnReset;
        private Button btnTopDebtors;

        private DataGridView dgContracts;
        private Label lblPageInfo;
        private Button btnPrevPage;
        private Button btnNextPage;

        private DataGridView dgSchedule;
        private Label lblContractTitle;

        private Button btnCollectSingle;
        private Button btnCollectAmount;
        private Button btnEarlyPayoff;
        private Button btnReschedule;
        private Button btnCancelContract;
        private Button btnAuditLog;

        public FrmInstallments()
        {
            InitUI();
            LoadCombos();
            LoadDashboard();
            SearchContracts();
        }

        private void InitUI()
        {
            this.Text = "نظام عقود التقسيط الشرعي";
            this.Size = new Size(1180, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var mainTbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain
            };
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));  // 1. Dashboard Metrics (scaled down)
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));  // 2. Filters Bar (scaled down)
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // 3. Grids & Actions Split

            // 1. Dashboard Metrics Panel
            var pnlDashboard = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Theme.BgMain,
                Padding = new Padding(10, 5, 10, 5)
            };
            
            pnlDashboard.Controls.Add(MakeMetricCard("💰 إجمالي المتبقي للتحصيل", out lblRemainingVal, Theme.Accent));
            pnlDashboard.Controls.Add(MakeMetricCard("📋 عقود التقسيط النشطة", out lblActiveCount, Color.FromArgb(52, 152, 219)));
            pnlDashboard.Controls.Add(MakeMetricCard("🔔 أقساط مستحقة اليوم", out lblDueTodayVal, Theme.Primary));
            pnlDashboard.Controls.Add(MakeMetricCard("⚠️ الأقساط المتأخرة", out lblOverdueVal, Theme.Danger));
            pnlDashboard.Controls.Add(MakeMetricCard("💵 تحصيلات اليوم الفعلي", out lblCollectedTodayVal, Theme.Success));

            mainTbl.Controls.Add(pnlDashboard, 0, 0);

            // 2. Filters Bar Panel
            var pnlFilters = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            AddLabel(pnlFilters, "العميل:", 15, 13);
            cboSearchClient = new ComboBox
            {
                Location = new Point(65, 9),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            SetupSearchableCombo(cboSearchClient);
            pnlFilters.Controls.Add(cboSearchClient);

            AddLabel(pnlFilters, "الحالة:", 280, 13);
            cboSearchStatus = new ComboBox
            {
                Location = new Point(330, 9),
                Width = 110,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboSearchStatus.Items.AddRange(new object[] { "All", "Active", "Completed", "Cancelled" });
            cboSearchStatus.SelectedIndex = 1; // Default to Active
            pnlFilters.Controls.Add(cboSearchStatus);

            AddLabel(pnlFilters, "رقم العقد:", 455, 13);
            txtSearchCode = new TextBox
            {
                Location = new Point(520, 9),
                Width = 120,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlFilters.Controls.Add(txtSearchCode);

            btnSearch = Theme.MakeButton("🔍 بحث", 660, 8, 90, 26, Theme.Primary);
            btnSearch.Click += (s, e) => { _currentPage = 1; SearchContracts(); };
            btnReset = Theme.MakeButton("🔄 إعادة تعيين", 760, 8, 100, 26, Color.FromArgb(100, 100, 100));
            btnReset.Click += BtnReset_Click;

            btnTopDebtors = Theme.MakeButton("📊 أعلى المدينين", 870, 8, 130, 26, Color.FromArgb(120, 120, 80));
            btnTopDebtors.Click += (s, e) => new FrmTopDebtors().ShowDialog();

            pnlFilters.Controls.AddRange(new Control[] { btnSearch, btnReset, btnTopDebtors });
            mainTbl.Controls.Add(pnlFilters, 0, 1);

            // 3. Grids & Actions Split Table
            var splitTbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain
            };
            splitTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f)); // Column 0 (Right): Contracts List (55%)
            splitTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f)); // Column 1 (Left): Schedule & Actions (45%)
            splitTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // 3a. Right Column: Contracts Grid Table Layout
            var pnlContracts = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10),
                Margin = new Padding(10, 10, 5, 10),
                BackColor = Theme.BgCard
            };
            pnlContracts.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // dgContracts (docked fill)
            pnlContracts.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));  // pnlPagination

            dgContracts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContractID", Visible = false });
            dgContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContractCode", HeaderText = "رقم العقد", FillWeight = 40 });
            dgContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "العميل", FillWeight = 80 });
            dgContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContractAmount", HeaderText = "إجمالي العقد", FillWeight = 45 });
            dgContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remaining", HeaderText = "المتبقي", FillWeight = 45 });
            dgContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "الأقساط", FillWeight = 30 });
            dgContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 35 });
            dgContracts.SelectionChanged += DgContracts_SelectionChanged;
            pnlContracts.Controls.Add(dgContracts, 0, 0);

            // Pagination Table Layout (100% responsive)
            var pnlPagination = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Theme.BgCard,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            pnlPagination.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f)); // btnPrevPage
            pnlPagination.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f)); // btnNextPage
            pnlPagination.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // lblPageInfo
            pnlPagination.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            btnPrevPage = Theme.MakeButton("السابق ◀", Color.FromArgb(70, 70, 70));
            btnPrevPage.Dock = DockStyle.Fill;
            btnPrevPage.Margin = new Padding(2);
            btnPrevPage.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; SearchContracts(); } };

            btnNextPage = Theme.MakeButton("▶ التالي", Color.FromArgb(70, 70, 70));
            btnNextPage.Dock = DockStyle.Fill;
            btnNextPage.Margin = new Padding(2);
            btnNextPage.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; SearchContracts(); } };

            lblPageInfo = new Label
            {
                Text = "صفحة 1 من 1",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextSub,
                Font = Theme.FontSmall
            };
            pnlPagination.Controls.Add(btnPrevPage, 0, 0);
            pnlPagination.Controls.Add(btnNextPage, 1, 0);
            pnlPagination.Controls.Add(lblPageInfo, 2, 0);

            pnlContracts.Controls.Add(pnlPagination, 0, 1);
            splitTbl.Controls.Add(pnlContracts, 0, 0);

            // 3b. Left Column: Details & Operations Table Layout
            var pnlDetails = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10),
                Margin = new Padding(5, 10, 10, 10),
                BackColor = Theme.BgCard
            };
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f)); // lblContractTitle
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // dgSchedule
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 85f));  // pnlActions

            lblContractTitle = new Label
            {
                Text = "اختر عقداً لعرض جدول السداد تفصيلياً",
                Font = new Font(Theme.FontBold.FontFamily, 11f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlDetails.Controls.Add(lblContractTitle, 0, 0);

            dgSchedule = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "رقم", FillWeight = 15 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "DueDate", HeaderText = "الاستحقاق", FillWeight = 45 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "المبلغ", FillWeight = 35 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaidAmount", HeaderText = "المدفوع", FillWeight = 35 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remaining", HeaderText = "المتبقي", FillWeight = 35 });
            dgSchedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 35 });
            pnlDetails.Controls.Add(dgSchedule, 0, 1);

            // Operation Buttons Table Layout (100% responsive)
            var pnlActions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Theme.BgCard
            };
            pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            pnlActions.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            pnlActions.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            btnCollectSingle = Theme.MakeButton("💵 سداد قسط", Theme.Success);
            btnCollectSingle.Dock = DockStyle.Fill;
            btnCollectSingle.Margin = new Padding(3);
            btnCollectSingle.Click += BtnCollectSingle_Click;

            btnCollectAmount = Theme.MakeButton("💰 تحصيل مبلغ", Theme.Accent);
            btnCollectAmount.Dock = DockStyle.Fill;
            btnCollectAmount.Margin = new Padding(3);
            btnCollectAmount.Click += BtnCollectAmount_Click;

            btnEarlyPayoff = Theme.MakeButton("⚡ سداد مبكر", Color.FromArgb(160, 50, 160));
            btnEarlyPayoff.Dock = DockStyle.Fill;
            btnEarlyPayoff.Margin = new Padding(3);
            btnEarlyPayoff.Click += BtnEarlyPayoff_Click;

            btnReschedule = Theme.MakeButton("📅 إعادة جدولة", Theme.Primary);
            btnReschedule.Dock = DockStyle.Fill;
            btnReschedule.Margin = new Padding(3);
            btnReschedule.Click += BtnReschedule_Click;

            btnCancelContract = Theme.MakeButton("❌ إلغاء العقد", Theme.Danger);
            btnCancelContract.Dock = DockStyle.Fill;
            btnCancelContract.Margin = new Padding(3);
            btnCancelContract.Click += BtnCancelContract_Click;

            btnAuditLog = Theme.MakeButton("🔐 سجل التدقيق", Color.FromArgb(120, 120, 80));
            btnAuditLog.Dock = DockStyle.Fill;
            btnAuditLog.Margin = new Padding(3);
            btnAuditLog.Click += BtnAuditLog_Click;

            pnlActions.Controls.Add(btnCollectSingle, 0, 0);
            pnlActions.Controls.Add(btnCollectAmount, 1, 0);
            pnlActions.Controls.Add(btnEarlyPayoff, 2, 0);
            pnlActions.Controls.Add(btnReschedule, 0, 1);
            pnlActions.Controls.Add(btnCancelContract, 1, 1);
            pnlActions.Controls.Add(btnAuditLog, 2, 1);

            pnlDetails.Controls.Add(pnlActions, 0, 2);
            splitTbl.Controls.Add(pnlDetails, 1, 0);

            mainTbl.Controls.Add(splitTbl, 0, 2);
            this.Controls.Add(mainTbl);

            Theme.ApplyFormRTL(this);
        }

        private Panel MakeMetricCard(string title, out Label lblVal, Color edgeColor)
        {
            var pnl = new Panel
            {
                Size = new Size(215, 58),
                BackColor = Theme.BgCard,
                Margin = new Padding(4),
                Cursor = Cursors.Hand
            };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(edgeColor), 0, 0, 5, 58);
            };

            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(10, 6),
                AutoSize = true,
                ForeColor = Theme.TextSub,
                Font = Theme.FontSmall
            };
            lblVal = new Label
            {
                Text = "0.00 ج",
                Location = new Point(10, 26),
                AutoSize = true,
                ForeColor = edgeColor,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
            };
            pnl.Controls.AddRange(new Control[] { lblTitle, lblVal });
            return pnl;
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain
            });
        }

        private void SetupSearchableCombo(ComboBox cbo)
        {
            cbo.AutoCompleteMode = AutoCompleteMode.None;
            cbo.TextUpdate += delegate
            {
                if (cbo.Tag == null)
                {
                    List<ComboItem> list = new List<ComboItem>();
                    foreach (ComboItem item in cbo.Items) list.Add(item);
                    cbo.Tag = list;
                }
                List<ComboItem> allItems = (List<ComboItem>)cbo.Tag;
                string text = cbo.Text;
                cbo.BeginUpdate();
                cbo.Items.Clear();
                if (string.IsNullOrWhiteSpace(text))
                {
                    cbo.Items.AddRange(allItems.ToArray());
                }
                else
                {
                    foreach (ComboItem item in allItems)
                    {
                        if (item.ID == 0 || item.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cbo.Items.Add(item);
                        }
                    }
                }
                cbo.EndUpdate();
                cbo.SelectionStart = text.Length;
                cbo.SelectionLength = 0;
                cbo.DroppedDown = true;
            };
        }

        private void LoadCombos()
        {
            DataTable clients = ClientDAL.GetAll(activeOnly: true);
            cboSearchClient.Items.Clear();
            cboSearchClient.Items.Add(new ComboItem(0, "-- الكل --"));
            foreach (DataRow r in clients.Rows)
            {
                cboSearchClient.Items.Add(new ComboItem(Convert.ToInt32(r["ClientID"]), r["ClientName"].ToString()));
            }
            cboSearchClient.DisplayMember = "Text";
            cboSearchClient.SelectedIndex = 0;
        }

        private void LoadDashboard()
        {
            // Load dashboard for branch 1
            DataTable dt = InstallmentDAL.GetDashboardData(1);
            if (dt.Rows.Count > 0)
            {
                int activeCount = Convert.ToInt32(dt.Rows[0]["ActiveContracts"]);
                decimal totalRem = Convert.ToDecimal(dt.Rows[0]["TotalRemaining"]);
                decimal dueToday = Convert.ToDecimal(dt.Rows[0]["DueToday"]);
                decimal overdue = Convert.ToDecimal(dt.Rows[0]["Overdue"]);
                decimal colToday = Convert.ToDecimal(dt.Rows[0]["CollectedToday"]);

                lblActiveCount.Text = activeCount.ToString() + " عقد";
                lblRemainingVal.Text = totalRem.ToString("N2") + " ج";
                lblDueTodayVal.Text = dueToday.ToString("N2") + " ج";
                lblOverdueVal.Text = overdue.ToString("N2") + " ج";
                lblCollectedTodayVal.Text = colToday.ToString("N2") + " ج";
            }
        }

        private void SearchContracts()
        {
            int? customerID = null;
            if (cboSearchClient.SelectedItem is ComboItem ci && ci.ID > 0) customerID = ci.ID;

            string status = cboSearchStatus.Text;
            string code = txtSearchCode.Text.Trim();

            int totalRecords = 0;
            DataTable dt = InstallmentDAL.GetContracts(customerID, status, code, _currentPage, PAGE_SIZE, out totalRecords);
            
            _totalPages = (int)Math.Ceiling((double)totalRecords / PAGE_SIZE);
            if (_totalPages == 0) _totalPages = 1;

            lblPageInfo.Text = $"صفحة {_currentPage} من {_totalPages} (إجمالي السجلات: {totalRecords})";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;

            dgContracts.Rows.Clear();
            foreach (DataRow r in dt.Rows)
            {
                decimal amt = Convert.ToDecimal(r["ContractAmount"]);
                decimal down = Convert.ToDecimal(r["DownPayment"]);
                decimal financed = Convert.ToDecimal(r["FinancedAmount"]);
                
                // Fetch remaining sum
                int contractID = Convert.ToInt32(r["ContractID"]);
                decimal rem = 0m;
                var remObj = DbHelper.Scalar("SELECT SUM(RemainingAmount) FROM InstallmentSchedules WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                if (remObj != DBNull.Value && remObj != null) rem = Convert.ToDecimal(remObj);

                dgContracts.Rows.Add(
                    r["ContractID"],
                    r["ContractCode"],
                    r["CustomerName"],
                    amt.ToString("N2") + " ج",
                    rem.ToString("N2") + " ج",
                    r["InstallmentCount"],
                    r["Status"]
                );
            }

            if (dgContracts.Rows.Count == 0)
            {
                dgSchedule.Rows.Clear();
                lblContractTitle.Text = "لا توجد نتائج بحث.";
                _selectedContractID = 0;
                ToggleActionButtons(false);
            }
            else
            {
                ToggleActionButtons(true);
            }
        }

        private void ToggleActionButtons(bool enabled)
        {
            btnCollectSingle.Enabled = enabled;
            btnCollectAmount.Enabled = enabled;
            btnEarlyPayoff.Enabled = enabled;
            btnReschedule.Enabled = enabled;
            btnCancelContract.Enabled = enabled;
            btnAuditLog.Enabled = enabled;
        }

        private void DgContracts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgContracts.SelectedRows.Count == 0) return;

            _selectedContractID = Convert.ToInt32(dgContracts.SelectedRows[0].Cells["ContractID"].Value);
            string code = dgContracts.SelectedRows[0].Cells["ContractCode"].Value.ToString();
            string client = dgContracts.SelectedRows[0].Cells["CustomerName"].Value.ToString();
            string status = dgContracts.SelectedRows[0].Cells["Status"].Value.ToString();

            lblContractTitle.Text = $"جدول السداد لعقد {code} - العميل: {client} ({status})";

            LoadSchedule(_selectedContractID);

            // Lock cancel button if already cancelled or completed
            btnCancelContract.Enabled = (status == "Active" || status == "Defaulted");
            btnReschedule.Enabled = (status == "Active" || status == "Defaulted");
            btnEarlyPayoff.Enabled = (status == "Active" || status == "Defaulted");
            btnCollectSingle.Enabled = (status == "Active" || status == "Defaulted");
            btnCollectAmount.Enabled = (status == "Active" || status == "Defaulted");
        }

        private void LoadSchedule(int contractID)
        {
            dgSchedule.Rows.Clear();
            DataTable dt = InstallmentDAL.GetContractSchedule(contractID);
            foreach (DataRow r in dt.Rows)
            {
                int no = Convert.ToInt32(r["InstallmentNo"]);
                DateTime due = Convert.ToDateTime(r["DueDate"]);
                decimal amount = Convert.ToDecimal(r["Amount"]);
                decimal paid = Convert.ToDecimal(r["PaidAmount"]);
                decimal remaining = Convert.ToDecimal(r["RemainingAmount"]);
                string status = r["Status"].ToString();

                int ri = dgSchedule.Rows.Add(
                    no,
                    due.ToString("yyyy-MM-dd"),
                    amount.ToString("F2") + " ج",
                    paid.ToString("F2") + " ج",
                    remaining.ToString("F2") + " ج",
                    status
                );

                // Styling statuses
                if (status == "Paid")
                {
                    dgSchedule.Rows[ri].Cells["Status"].Style.ForeColor = Theme.Success;
                }
                else if (status == "Overdue")
                {
                    dgSchedule.Rows[ri].Cells["Status"].Style.ForeColor = Theme.Danger;
                    dgSchedule.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                }
                else if (status == "Partially Paid")
                {
                    dgSchedule.Rows[ri].Cells["Status"].Style.ForeColor = Color.Orange;
                }
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            cboSearchClient.SelectedIndex = 0;
            cboSearchStatus.SelectedIndex = 1; // Default to Active
            txtSearchCode.Clear();
            _currentPage = 1;
            SearchContracts();
        }

        private void BtnCollectSingle_Click(object sender, EventArgs e)
        {
            if (_selectedContractID == 0) return;
            if (!Session.CanAdd("Installments")) { MessageBox.Show("⛔ ليس لديك صلاحية تحصيل الأقساط.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (dgSchedule.SelectedRows.Count == 0)
            {
                MessageBox.Show("من فضلك اختر القسط المستحق المطلوب سداده من جدول الأقساط أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int no = Convert.ToInt32(dgSchedule.SelectedRows[0].Cells["No"].Value);
            string status = dgSchedule.SelectedRows[0].Cells["Status"].Value.ToString();
            string remStr = dgSchedule.SelectedRows[0].Cells["Remaining"].Value.ToString().Replace(" ج", "").Trim();
            decimal remaining = Convert.ToDecimal(remStr);

            if (status == "Paid")
            {
                MessageBox.Show("هذا القسط مسدد بالفعل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var frm = new FrmCollectPrompt(remaining, $"سداد القسط رقم {no}"))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bool ok = InstallmentDAL.CollectPayment(_selectedContractID, 1, frm.CollectedAmount, frm.PaymentMethod, frm.SelectedSafeID, frm.Notes);
                        if (ok)
                        {
                            MessageBox.Show("✅ تم سداد القسط وتحديث القيود بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDashboard();
                            SearchContracts();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ فشل التحصيل: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnCollectAmount_Click(object sender, EventArgs e)
        {
            if (_selectedContractID == 0) return;
            if (!Session.CanAdd("Installments")) { MessageBox.Show("⛔ ليس لديك صلاحية تحصيل دفعات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // Get total remaining for contract
            decimal remTotal = 0m;
            var remObj = DbHelper.Scalar("SELECT SUM(RemainingAmount) FROM InstallmentSchedules WHERE ContractID=@cid AND Status <> 'Paid'", DbHelper.P("@cid", _selectedContractID));
            if (remObj != DBNull.Value && remObj != null) remTotal = Convert.ToDecimal(remObj);

            if (remTotal <= 0m)
            {
                MessageBox.Show("العقد مسدد بالكامل بالفعل ولا توجد أقساط مستحقة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var frm = new FrmCollectPrompt(remTotal, "تحصيل دفعة مالية وتوزيعها بالتسلسل"))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bool ok = InstallmentDAL.CollectPayment(_selectedContractID, 1, frm.CollectedAmount, frm.PaymentMethod, frm.SelectedSafeID, frm.Notes);
                        if (ok)
                        {
                            MessageBox.Show("✅ تم تحصيل المبلغ وتوزيعه بنجاح على الأقساط من الأقدم للأحدث.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDashboard();
                            SearchContracts();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ فشل التحصيل: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnEarlyPayoff_Click(object sender, EventArgs e)
        {
            if (_selectedContractID == 0) return;
            if (!Session.CanAdd("Installments")) { MessageBox.Show("⛔ ليس لديك صلاحية إجراء سداد مبكر.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // Get remaining sum
            decimal remaining = 0m;
            var remObj = DbHelper.Scalar("SELECT SUM(RemainingAmount) FROM InstallmentSchedules WHERE ContractID=@cid AND Status <> 'Paid'", DbHelper.P("@cid", _selectedContractID));
            if (remObj != DBNull.Value && remObj != null) remaining = Convert.ToDecimal(remObj);

            if (remaining <= 0)
            {
                MessageBox.Show("العقد مغلق ومسدد بالكامل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var frm = new FrmEarlyPayoffPrompt(remaining))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // سداد العقد بالكامل (مخصوماً منه التنازل الاختياري)
                        // في سياق الشريعة: التنازل الطوعي عن جزء من الدين عند السداد المبكر جائز شريطة ألا يكون مشروطاً مسبقاً في صلب العقد.
                        decimal payoffAmount = remaining - frm.WaiverAmount;
                        
                        // نقوم بالتحصيل
                        bool ok = false;
                        DbHelper.RunInTransaction((con, trans) =>
                        {
                            // 1. إذا كان هناك تنازل/خصم طوعي، نخفض الأقساط غير المسددة أولاً بقيمة الخصم
                            if (frm.WaiverAmount > 0)
                            {
                                // تخفيض الأقساط بالتنازل بدءاً من الأخير تنازلياً
                                var dtSchedule = DbHelper.Query(
                                    "SELECT ScheduleID, Amount, RemainingAmount FROM InstallmentSchedules WHERE ContractID=@cid AND Status <> 'Paid' ORDER BY InstallmentNo DESC",
                                    DbHelper.P("@cid", _selectedContractID));

                                decimal toReduce = frm.WaiverAmount;
                                foreach (DataRow row in dtSchedule.Rows)
                                {
                                    if (toReduce <= 0) break;

                                    int sid = Convert.ToInt32(row["ScheduleID"]);
                                    decimal rem = Convert.ToDecimal(row["RemainingAmount"]);
                                    decimal amt = Convert.ToDecimal(row["Amount"]);

                                    decimal reduceThis = Math.Min(toReduce, rem);
                                    decimal newAmt = amt - reduceThis;
                                    decimal newRem = rem - reduceThis;

                                    DbHelper.ExecuteTrans(trans,
                                        "UPDATE InstallmentSchedules SET Amount=@a, RemainingAmount=@ra, Status=@st WHERE ScheduleID=@sid",
                                        DbHelper.P("@a", newAmt),
                                        DbHelper.P("@ra", newRem),
                                        DbHelper.P("@st", newRem == 0 ? "Paid" : "Partially Paid"),
                                        DbHelper.P("@sid", sid));

                                    toReduce -= reduceThis;
                                }

                                // مراجعة إجمالي العقد وتعديل القيمة
                                DbHelper.ExecuteTrans(trans,
                                    "UPDATE InstallmentContracts SET ContractAmount = ContractAmount - @wv, FinancedAmount = FinancedAmount - @wv WHERE ContractID=@cid",
                                    DbHelper.P("@wv", frm.WaiverAmount),
                                    DbHelper.P("@cid", _selectedContractID));
                            }

                            // 2. نقوم بتحصيل القيمة المتبقية بالكامل لإغلاق العقد
                            if (payoffAmount > 0)
                            {
                                // نستدعي السداد المجمع داخل نفس الترانزأكشن
                                // سنقوم بتمرير التحصيل
                                // بما أننا في ترانزأكشن حالية، يمكن كتابة كود التحصيل مباشرة لتجنب تكرار فتح الترانزأكشن
                                var dtContract = DbHelper.QueryTrans(trans, "SELECT CustomerID, ContractCode FROM InstallmentContracts WHERE ContractID=@cid", DbHelper.P("@cid", _selectedContractID));
                                int customerID = Convert.ToInt32(dtContract.Rows[0]["CustomerID"]);
                                string contractCode = dtContract.Rows[0]["ContractCode"].ToString();

                                var dtSchedule = DbHelper.QueryTrans(trans,
                                    "SELECT ScheduleID, InstallmentNo, RemainingAmount, PaidAmount FROM InstallmentSchedules WHERE ContractID=@cid AND Status <> 'Paid' ORDER BY InstallmentNo",
                                    DbHelper.P("@cid", _selectedContractID));

                                decimal remToPay = payoffAmount;
                                foreach (DataRow row in dtSchedule.Rows)
                                {
                                    if (remToPay <= 0) break;
                                    int sid = Convert.ToInt32(row["ScheduleID"]);
                                    int instNo = Convert.ToInt32(row["InstallmentNo"]);
                                    decimal rem = Convert.ToDecimal(row["RemainingAmount"]);

                                    decimal payThis = Math.Min(remToPay, rem);
                                    decimal newPaid = Convert.ToDecimal(row["PaidAmount"]) + payThis;
                                    decimal newRem = rem - payThis;

                                    DbHelper.ExecuteTrans(trans,
                                        "UPDATE InstallmentSchedules SET PaidAmount=@pa, RemainingAmount=@ra, Status='Paid', PaidDate=GETDATE() WHERE ScheduleID=@sid",
                                        DbHelper.P("@pa", newPaid),
                                        DbHelper.P("@ra", newRem),
                                        DbHelper.P("@sid", sid));

                                    DbHelper.ExecuteTrans(trans,
                                        @"INSERT INTO InstallmentPayments (ContractID, ScheduleID, BranchID, PaymentDate, Amount, PaymentMethod, SafeID, UserID, Notes)
                                          VALUES (@cid, @sid, 1, GETDATE(), @amt, @pm, @safeId, @uid, @notes)",
                                        DbHelper.P("@cid", _selectedContractID),
                                        DbHelper.P("@sid", sid),
                                        DbHelper.P("@amt", payThis),
                                        DbHelper.P("@pm", frm.PaymentMethod),
                                        DbHelper.P("@safeId", frm.SelectedSafeID),
                                        DbHelper.P("@uid", Session.EmpID),
                                        DbHelper.P("@notes", $"سداد مبكر وتسوية قسط {instNo} للعقد {contractCode}"));

                                    remToPay -= payThis;
                                }

                                DbHelper.ExecuteTrans(trans, "UPDATE InstallmentContracts SET Status='Completed' WHERE ContractID=@cid", DbHelper.P("@cid", _selectedContractID));
                                InstallmentDAL.AddAuditLogTrans(trans, "EarlyPayoff", _selectedContractID, "", $"سداد مبكر بمبلغ: {payoffAmount:N2} ج (خصم طوعي: {frm.WaiverAmount:N2} ج)");

                                // القيد المحاسبي للعميل والخزنة
                                string payDesc = $"سداد مبكر وتسوية لعقد التقسيط {contractCode} (الخصم: {frm.WaiverAmount:N2} ج)";
                                
                                DbHelper.ExecuteTrans(trans,
                                    "INSERT INTO ClientTransactions(ClientID, TransType, Credit, RefID, Notes, CreatedBy, TransDate) VALUES(@cid, 'Payment', @amt, @ref, @notes, @uid, GETDATE())",
                                    DbHelper.P("@cid", customerID),
                                    DbHelper.P("@amt", payoffAmount),
                                    DbHelper.P("@ref", _selectedContractID),
                                    DbHelper.P("@notes", payDesc),
                                    DbHelper.P("@uid", Session.EmpID));

                                DbHelper.ExecuteTrans(trans,
                                    "INSERT INTO CashBox(TransType, AmountIn, RefID, Notes, CreatedBy, TransDate, AccountID) VALUES('ClientPayment', @amt, @ref, @notes, @uid, GETDATE(), @accId)",
                                    DbHelper.P("@amt", payoffAmount),
                                    DbHelper.P("@ref", _selectedContractID),
                                    DbHelper.P("@notes", $"تسوية وسداد مبكر للعقد {contractCode} للعميل ID:{customerID}"),
                                    DbHelper.P("@uid", Session.EmpID),
                                    DbHelper.P("@accId", frm.SelectedSafeID));
                            }
                            ok = true;
                        });

                        if (ok)
                        {
                            MessageBox.Show("✅ تم إجراء التسوية والسداد المبكر وإغلاق العقد بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDashboard();
                            SearchContracts();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ فشل إجراء السداد المبكر: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnReschedule_Click(object sender, EventArgs e)
        {
            if (_selectedContractID == 0) return;
            if (!Session.CanEdit("Installments")) { MessageBox.Show("⛔ ليس لديك صلاحية إعادة جدولة الأقساط.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            DataTable dt = InstallmentDAL.GetContractSchedule(_selectedContractID);
            List<InstallmentScheduleDTO> list = new List<InstallmentScheduleDTO>();
            bool hasUnpaid = false;

            foreach (DataRow r in dt.Rows)
            {
                if (r["Status"].ToString() != "Paid")
                {
                    hasUnpaid = true;
                    list.Add(new InstallmentScheduleDTO
                    {
                        ScheduleID = Convert.ToInt32(r["ScheduleID"]),
                        InstallmentNo = Convert.ToInt32(r["InstallmentNo"]),
                        DueDate = Convert.ToDateTime(r["DueDate"]),
                        Amount = Convert.ToDecimal(r["Amount"]),
                        PaidAmount = Convert.ToDecimal(r["PaidAmount"]),
                        RemainingAmount = Convert.ToDecimal(r["RemainingAmount"]),
                        Status = r["Status"].ToString()
                    });
                }
            }

            if (!hasUnpaid)
            {
                MessageBox.Show("لا توجد أقساط غير مسددة لإعادة جدولتها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var frm = new FrmReschedulePrompt(list))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bool ok = InstallmentDAL.RescheduleInstallments(_selectedContractID, frm.UpdatedSchedule);
                        if (ok)
                        {
                            MessageBox.Show("✅ تم إعادة جدولة تواريخ استحقاق الأقساط بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            SearchContracts();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ فشل إعادة الجدولة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnCancelContract_Click(object sender, EventArgs e)
        {
            if (_selectedContractID == 0) return;
            if (!Session.CanDelete("Installments")) { MessageBox.Show("⛔ ليس لديك صلاحية إلغاء عقود التقسيط.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // التحقق من وجود دفعات مسجلة
            if (InstallmentDAL.HasPaymentsCollected(_selectedContractID))
            {
                MessageBox.Show("❌ يمنع إلغاء أو حذف هذا العقد لوجود تحصيلات وأقساط مسددة مسجلة عليه.\nيُسمح فقط بإلغاء العقود غير المدفوعة أو إرجاع البضاعة عبر فاتورة مرتجع.", "إلغاء غير مسموح", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            DialogResult confirm = MessageBox.Show("⚠️ هل أنت متأكد من إلغاء هذا العقد بالكامل؟\nسيؤدي ذلك إلى توليد قيود عكسية وتصفير المديونية وإلغاء الأقساط المتبقية.", "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                string reason = InputBoxHelper.Show("من فضلك أدخل سبب الإلغاء:", "سبب إلغاء العقد", "إلغاء بطلب من العميل");
                if (string.IsNullOrWhiteSpace(reason)) return;

                try
                {
                    bool ok = InstallmentDAL.CancelContract(_selectedContractID, reason);
                    if (ok)
                    {
                        MessageBox.Show("✅ تم إلغاء عقد التقسيط وتصفير جدول الأقساط وعكس القيود بنجاح.", "تم الإلغاء", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDashboard();
                        SearchContracts();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل الإلغاء: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnAuditLog_Click(object sender, EventArgs e)
        {
            if (_selectedContractID == 0) return;
            new FrmAuditLogs(_selectedContractID).ShowDialog();
        }
    }

    // Prompt Dialog for collecting payments
    public class FrmCollectPrompt : Form
    {
        public decimal CollectedAmount { get; private set; }
        public int SelectedSafeID { get; private set; }
        public string PaymentMethod { get; private set; }
        public string Notes { get; private set; }

        private NumericUpDown nudAmount;
        private ComboBox cboMethod;
        private TextBox txtNotes;
        private Button btnOk;
        private Button btnCancel;

        public FrmCollectPrompt(decimal maxAmount, string titleText)
        {
            this.Text = titleText;
            this.Size = new Size(350, 240);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            int y = 20;
            var lblAmount = new Label { Text = "القيمة المراد تحصيلها (ج):", Location = new Point(20, y), AutoSize = true, ForeColor = Theme.TextMain };
            nudAmount = new NumericUpDown
            {
                Location = new Point(160, y - 3),
                Width = 140,
                Minimum = 0.01m,
                Maximum = maxAmount,
                Value = maxAmount,
                DecimalPlaces = 2,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            this.Controls.AddRange(new Control[] { lblAmount, nudAmount });

            y += 40;
            var lblMethod = new Label { Text = "حساب التحصيل:", Location = new Point(20, y), AutoSize = true, ForeColor = Theme.TextMain };
            cboMethod = new ComboBox
            {
                Location = new Point(160, y - 3),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            // تحميل الحسابات النشطة
            try
            {
                DataTable safes = AccountDAL.GetActiveSafeAccounts();
                foreach (DataRow row in safes.Rows)
                {
                    cboMethod.Items.Add(new ComboItem(
                        Convert.ToInt32(row["AccountID"]),
                        row["AccountName"].ToString()
                    ));
                }
                cboMethod.DisplayMember = "Text";
                if (cboMethod.Items.Count > 0) cboMethod.SelectedIndex = 0;
            }
            catch { }
            this.Controls.AddRange(new Control[] { lblMethod, cboMethod });

            y += 40;
            var lblNotes = new Label { Text = "ملاحظات:", Location = new Point(20, y), AutoSize = true, ForeColor = Theme.TextMain };
            txtNotes = new TextBox
            {
                Location = new Point(160, y - 3),
                Width = 140,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.AddRange(new Control[] { lblNotes, txtNotes });

            y += 50;
            btnOk = Theme.MakeButton("💾 سداد", 200, y, 100, 30, Theme.Success);
            btnOk.Click += (s, e) =>
            {
                CollectedAmount = nudAmount.Value;
                SelectedSafeID = 1;
                PaymentMethod = "Cash";
                if (cboMethod.SelectedItem is ComboItem safeItem)
                {
                    SelectedSafeID = safeItem.ID;
                    PaymentMethod = safeItem.Text;
                }
                Notes = txtNotes.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            btnCancel = Theme.MakeButton("إلغاء", 90, y, 100, 30, Color.FromArgb(100, 100, 100));
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
        }
    }

    // Prompt for early payoff
    public class FrmEarlyPayoffPrompt : Form
    {
        public decimal WaiverAmount { get; private set; }
        public int SelectedSafeID { get; private set; }
        public string PaymentMethod { get; private set; }

        private Label lblTotal;
        private Label lblPayoff;
        private NumericUpDown nudWaiver;
        private ComboBox cboMethod;
        private Button btnOk;
        private Button btnCancel;
        private decimal _totalRemaining;

        public FrmEarlyPayoffPrompt(decimal totalRemaining)
        {
            _totalRemaining = totalRemaining;
            this.Text = "تسوية وسداد مبكر للعقد";
            this.Size = new Size(380, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            int y = 20;
            lblTotal = new Label
            {
                Text = $"إجمالي الأقساط المتبقية القائمة: {totalRemaining:N2} ج",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            this.Controls.Add(lblTotal);

            y += 40;
            var lblWaiver = new Label { Text = "خصم التنازل الطوعي (ج):", Location = new Point(20, y), AutoSize = true, ForeColor = Theme.TextMain };
            nudWaiver = new NumericUpDown
            {
                Location = new Point(180, y - 3),
                Width = 150,
                Minimum = 0m,
                Maximum = totalRemaining - 1m,
                Value = 0m,
                DecimalPlaces = 2,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            nudWaiver.ValueChanged += (s, e) => UpdatePayoffLabel();
            this.Controls.AddRange(new Control[] { lblWaiver, nudWaiver });

            y += 40;
            lblPayoff = new Label
            {
                Text = $"المبلغ المطلوب تحصيله للإغلاق: {totalRemaining:N2} ج",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Theme.Accent,
                Font = Theme.FontBold
            };
            this.Controls.Add(lblPayoff);

            y += 40;
            var lblMethod = new Label { Text = "حساب السداد:", Location = new Point(20, y), AutoSize = true, ForeColor = Theme.TextMain };
            cboMethod = new ComboBox
            {
                Location = new Point(180, y - 3),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            // تحميل الحسابات النشطة
            try
            {
                DataTable safes = AccountDAL.GetActiveSafeAccounts();
                foreach (DataRow row in safes.Rows)
                {
                    cboMethod.Items.Add(new ComboItem(
                        Convert.ToInt32(row["AccountID"]),
                        row["AccountName"].ToString()
                    ));
                }
                cboMethod.DisplayMember = "Text";
                if (cboMethod.Items.Count > 0) cboMethod.SelectedIndex = 0;
            }
            catch { }
            this.Controls.AddRange(new Control[] { lblMethod, cboMethod });

            y += 50;
            btnOk = Theme.MakeButton("💾 إغلاق العقد", 230, y, 110, 30, Theme.Success);
            btnOk.Click += (s, e) =>
            {
                WaiverAmount = nudWaiver.Value;
                SelectedSafeID = 1;
                PaymentMethod = "Cash";
                if (cboMethod.SelectedItem is ComboItem safeItem)
                {
                    SelectedSafeID = safeItem.ID;
                    PaymentMethod = safeItem.Text;
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            btnCancel = Theme.MakeButton("إلغاء", 110, y, 110, 30, Color.FromArgb(100, 100, 100));
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
        }

        private void UpdatePayoffLabel()
        {
            decimal waiver = nudWaiver.Value;
            lblPayoff.Text = $"المبلغ المطلوب تحصيله للإغلاق: {(_totalRemaining - waiver):N2} ج";
        }
    }

    // Prompt for rescheduling due dates
    public class FrmReschedulePrompt : Form
    {
        public List<InstallmentScheduleDTO> UpdatedSchedule { get; private set; }
        private DataGridView dgReschedule;
        private Button btnOk;
        private Button btnCancel;

        public FrmReschedulePrompt(List<InstallmentScheduleDTO> list)
        {
            UpdatedSchedule = list;
            this.Text = "إعادة جدولة التواريخ للأقساط المتبقية";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            var lblInfo = new Label
            {
                Text = "💡 يمكنك النقر المزدوج على خانة تاريخ الاستحقاق لتغيير تاريخ السداد.",
                Location = new Point(15, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 180, 100),
                Font = Theme.FontSmall
            };
            this.Controls.Add(lblInfo);

            dgReschedule = new DataGridView
            {
                Location = new Point(15, 35),
                Size = new Size(400, 260),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgReschedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "SID", Visible = false });
            dgReschedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "رقم القسط", ReadOnly = true, FillWeight = 30 });
            dgReschedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "القيمة (ثابتة)", ReadOnly = true, FillWeight = 35 });
            dgReschedule.Columns.Add(new DataGridViewTextBoxColumn { Name = "DueDate", HeaderText = "تاريخ الاستحقاق", ReadOnly = false, FillWeight = 50 });
            
            // Format column types & add DateTimePicker editing support or string parse
            dgReschedule.CellEndEdit += DgReschedule_CellEndEdit;

            foreach (var item in UpdatedSchedule)
            {
                dgReschedule.Rows.Add(
                    item.ScheduleID,
                    item.InstallmentNo,
                    item.Amount.ToString("N2") + " ج",
                    item.DueDate.ToString("yyyy-MM-dd")
                );
            }
            this.Controls.Add(dgReschedule);

            int y = 310;
            btnOk = Theme.MakeButton("💾 حفظ التواريخ المحدثة", 230, y, 180, 32, Theme.Success);
            btnOk.Click += BtnOk_Click;
            btnCancel = Theme.MakeButton("إلغاء", 120, y, 100, 32, Color.FromArgb(100, 100, 100));
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
        }

        private void DgReschedule_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgReschedule.Columns[e.ColumnIndex].Name == "DueDate")
            {
                string val = dgReschedule.Rows[e.RowIndex].Cells["DueDate"].Value?.ToString();
                if (DateTime.TryParse(val, out DateTime dt))
                {
                    dgReschedule.Rows[e.RowIndex].Cells["DueDate"].Value = dt.ToString("yyyy-MM-dd");
                }
                else
                {
                    MessageBox.Show("من فضلك أدخل تاريخاً صحيحاً بصيغة YYYY-MM-DD", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dgReschedule.Rows[e.RowIndex].Cells["DueDate"].Value = UpdatedSchedule[e.RowIndex].DueDate.ToString("yyyy-MM-dd");
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Update the list before closing
            for (int i = 0; i < dgReschedule.Rows.Count; i++)
            {
                int sid = Convert.ToInt32(dgReschedule.Rows[i].Cells["SID"].Value);
                DateTime dt = DateTime.Parse(dgReschedule.Rows[i].Cells["DueDate"].Value.ToString());
                
                foreach (var item in UpdatedSchedule)
                {
                    if (item.ScheduleID == sid)
                    {
                        item.DueDate = dt;
                        break;
                    }
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // Form to display Audit Logs
    public class FrmAuditLogs : Form
    {
        private int _contractID;
        private DataGridView dgLogs;

        public FrmAuditLogs(int contractID)
        {
            _contractID = contractID;
            this.Text = "سجل تدقيق عمليات العقد (Audit Trail)";
            this.Size = new Size(620, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            dgLogs = new DataGridView
            {
                Location = new Point(15, 15),
                Size = new Size(570, 300),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontSmall },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "العملية", FillWeight = 30 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "User", HeaderText = "المستخدم", FillWeight = 40 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "التاريخ والوقت", FillWeight = 55 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Old", HeaderText = "القيمة القديمة", FillWeight = 50 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "New", HeaderText = "القيمة الجديدة", FillWeight = 50 });
            this.Controls.Add(dgLogs);

            LoadLogs();
        }

        private void LoadLogs()
        {
            dgLogs.Rows.Clear();
            DataTable dt = InstallmentDAL.GetAuditLogs(_contractID);
            foreach (DataRow r in dt.Rows)
            {
                dgLogs.Rows.Add(
                    r["Action"],
                    r["UserName"] != DBNull.Value ? r["UserName"] : "---",
                    Convert.ToDateTime(r["LogDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                    r["OldValue"],
                    r["NewValue"]
                );
            }
        }
    }

    public static class InputBoxHelper
    {
        public static string Show(string promptText, string titleText, string defaultText = "")
        {
            using (Form form = new Form())
            {
                Label label = new Label();
                TextBox textBox = new TextBox();
                Button buttonOk = new Button();
                Button buttonCancel = new Button();

                form.Text = titleText;
                label.Text = promptText;
                textBox.Text = defaultText;

                buttonOk.Text = "موافق";
                buttonCancel.Text = "إلغاء";
                buttonOk.DialogResult = DialogResult.OK;
                buttonCancel.DialogResult = DialogResult.Cancel;

                label.SetBounds(9, 20, 372, 13);
                textBox.SetBounds(12, 45, 372, 20);
                buttonOk.SetBounds(228, 80, 75, 23);
                buttonCancel.SetBounds(309, 80, 75, 23);

                label.AutoSize = true;
                textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
                buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                form.ClientSize = new Size(396, 117);
                form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
                form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;
                form.RightToLeft = RightToLeft.Yes;
                form.RightToLeftLayout = true;
                form.BackColor = Theme.BgCard;
                form.ForeColor = Theme.TextMain;
                textBox.BackColor = Theme.BgInput;
                textBox.ForeColor = Theme.TextMain;
                buttonOk.BackColor = Theme.Accent;
                buttonOk.ForeColor = Color.White;
                buttonOk.FlatStyle = FlatStyle.Flat;
                buttonOk.FlatAppearance.BorderSize = 0;
                buttonCancel.BackColor = Color.FromArgb(100, 100, 100);
                buttonCancel.ForeColor = Color.White;
                buttonCancel.FlatStyle = FlatStyle.Flat;
                buttonCancel.FlatAppearance.BorderSize = 0;

                DialogResult dialogResult = form.ShowDialog();
                return dialogResult == DialogResult.OK ? textBox.Text : "";
            }
        }
    }

    public class FrmTopDebtors : Form
    {
        public FrmTopDebtors()
        {
            this.Text = "أعلى 10 عملاء مديونية بالتقسيط";
            this.Size = new Size(420, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            var dg = new DataGridView
            {
                Location = new Point(15, 15),
                Size = new Size(375, 300),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "اسم العميل", FillWeight = 60 });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "DebtAmount", HeaderText = "قيمة المديونية القائمة", FillWeight = 40 });

            DataTable dt = InstallmentDAL.GetTop10Debtors(1);
            foreach (DataRow r in dt.Rows)
            {
                dg.Rows.Add(r["ClientName"], Convert.ToDecimal(r["DebtAmount"]).ToString("N2") + " ج");
            }

            this.Controls.Add(dg);
        }
    }
}

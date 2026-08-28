using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة إدارة وحساب عمولات الموظفين والمناديب وشرائح البيع وعمولات الأصناف
    /// </summary>
    public class FrmEmployeeCommissions : Form
    {
        private TabControl tabControl;

        // ══════ تبويب 1: عمولات الأصناف ══════
        private ComboBox cboProdEmp;
        private DataGridView dgProductRules;
        private Button btnAddProdRule, btnDeleteProdRule;

        // ══════ تبويب 2: شرائح المبيعات ══════
        private ComboBox cboTierEmp;
        private DataGridView dgTiers;
        private Button btnAddTier, btnDeleteTier;

        // ══════ تبويب 3: محرك احتساب العمولات ══════
        private ComboBox cboCalcEmp;
        private DateTimePicker dtpCalcFrom, dtpCalcTo;
        private DataGridView dgCalcDetails;
        private Button btnRunCalc, btnSettleCommission, btnPrintCommReport;
        private Label lblCalcTotalSales, lblCalcTotalCommission, lblCalcInvoicesCount;

        private decimal _lastCalculatedCommission = 0m;
        private int _lastCalculatedEmpID = 0;

        public FrmEmployeeCommissions()
        {
            InitUI();
            LoadEmployeesDropdowns();
        }

        private void InitUI()
        {
            this.Text = "💼 نظام العمولات وشرائح البيع وعمولات الأصناف";
            this.Size = new Size(1200, 740);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // الشريط العلوي
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblTitle = new Label
            {
                Text = "💼 محرك عمولات الموظفين والمناديب (عمولات الأصناف + شرائح البيع + الاحتساب الآلي)",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 14)
            };
            pnlTop.Controls.Add(lblTitle);

            // التبويبات
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Padding = new Point(16, 8)
            };

            var tabCalc = new TabPage("⚡ احتساب وترحيل العمولات من المبيعات");
            var tabProdRules = new TabPage("🏷️ عمولات أصناف محددة للموظف");
            var tabTiers = new TabPage("📈 شرائح المبيعات والتارجت");

            BuildCalcTab(tabCalc);
            BuildProductRulesTab(tabProdRules);
            BuildTiersTab(tabTiers);

            tabControl.TabPages.Add(tabCalc);
            tabControl.TabPages.Add(tabProdRules);
            tabControl.TabPages.Add(tabTiers);
            Theme.StyleTabControl(tabControl);

            this.Controls.Add(tabControl);
            this.Controls.Add(pnlTop);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // التبويب الأول: احتساب العمولات آلياً من المبيعات
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildCalcTab(TabPage tab)
        {
            tab.BackColor = Theme.BgMain;

            var pnlToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            var lblEmp = new Label { Text = "👤 الموظف / المندوب:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 7, 0, 0), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            cboCalcEmp = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = new Font("Segoe UI", 10f) };

            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 7, 0, 0) };
            dtpCalcFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), Width = 120 };

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 7, 0, 0) };
            dtpCalcTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 120 };

            btnRunCalc = Theme.MakeButton("⚡ احتساب العمولات الآن", 0, 0, 170, 36, Theme.Primary);
            btnRunCalc.Click += (s, e) => RunCommissionsCalculation();

            btnSettleCommission = Theme.MakeButton("💰 ترحيل العمولة لحساب الموظف", 0, 0, 220, 36, Theme.Success);
            btnSettleCommission.Click += BtnSettleCommission_Click;

            btnPrintCommReport = Theme.MakeButton("🖨️ طباعة التقرير", 0, 0, 120, 36, Theme.Secondary);
            btnPrintCommReport.Click += BtnPrintCommReport_Click;

            pnlToolbar.Controls.AddRange(new Control[] { lblEmp, cboCalcEmp, lblFrom, dtpCalcFrom, lblTo, dtpCalcTo, btnRunCalc, btnSettleCommission, btnPrintCommReport });

            // شريط الإحصائيات أسفل الجدول
            var pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(15, 10, 15, 10),
                RightToLeft = RightToLeft.Yes
            };

            lblCalcTotalSales = new Label { Text = "🛒 إجمالي المبيعات: 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Theme.Primary, Margin = new Padding(10, 0, 30, 0) };
            lblCalcTotalCommission = new Label { Text = "💰 إجمالي العمولة المستحقة: 0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.DarkGreen, Margin = new Padding(10, 0, 30, 0) };
            lblCalcInvoicesCount = new Label { Text = "📋 عدد الفواتير: 0", AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Theme.TextMain, Margin = new Padding(10, 0, 30, 0) };

            pnlSummary.Controls.AddRange(new Control[] { lblCalcTotalSales, lblCalcTotalCommission, lblCalcInvoicesCount });

            // جدول تفاصيل العمولات
            dgCalcDetails = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 }
            };

            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", HeaderText = "رقم الفاتورة", FillWeight = 60 });
            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleDate", HeaderText = "التاريخ", FillWeight = 55 });
            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "العميل", FillWeight = 90 });
            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", FillWeight = 110 });
            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 45 });
            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر البيع", FillWeight = 50 });
            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "الإجمالي", FillWeight = 60 });
            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "CommissionRule", HeaderText = "نوع وقاعدة العمولة", FillWeight = 120 });
            dgCalcDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "CommissionAmount", HeaderText = "مبلغ العمولة", FillWeight = 65 });

            tab.Controls.Add(dgCalcDetails);
            tab.Controls.Add(pnlSummary);
            tab.Controls.Add(pnlToolbar);
        }

        private void RunCommissionsCalculation()
        {
            if (cboCalcEmp.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                _lastCalculatedEmpID = ci.ID;
                var res = EmployeeHRDAL.CalculateCommissions(ci.ID, dtpCalcFrom.Value, dtpCalcTo.Value);
                _lastCalculatedCommission = res.TotalCommission;

                dgCalcDetails.Rows.Clear();
                var countedSales = new System.Collections.Generic.HashSet<string>();

                foreach (DataRow r in res.DetailsTable.Rows)
                {
                    string code = r["SaleCode"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(code) && code != "---") countedSales.Add(code);

                    decimal qty = Convert.ToDecimal(r["Quantity"]);
                    decimal price = Convert.ToDecimal(r["UnitPrice"]);
                    decimal total = Convert.ToDecimal(r["TotalPrice"]);
                    decimal comm = Convert.ToDecimal(r["CommissionAmount"]);

                    int idx = dgCalcDetails.Rows.Add(
                        code,
                        r["SaleDate"]?.ToString(),
                        r["ClientName"]?.ToString(),
                        r["ProductName"]?.ToString(),
                        qty.ToString("N2"),
                        price.ToString("N2"),
                        total.ToString("N2"),
                        r["CommissionRule"]?.ToString(),
                        comm.ToString("N2") + " ج"
                    );

                    if (comm > 0)
                    {
                        dgCalcDetails.Rows[idx].Cells["CommissionAmount"].Style.ForeColor = Color.DarkGreen;
                        dgCalcDetails.Rows[idx].Cells["CommissionAmount"].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    }
                }

                lblCalcTotalSales.Text = $"🛒 إجمالي المبيعات: {res.TotalSalesAmount:N2} ج";
                lblCalcTotalCommission.Text = $"💰 إجمالي العمولة المستحقة: {res.TotalCommission:N2} ج";
                lblCalcInvoicesCount.Text = $"📋 عدد الفواتير: {countedSales.Count:N0}";

                if (dgCalcDetails.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد فواتير مبيعات مسجلة لهذا الموظف في الفترة المحددة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("يرجى اختيار الموظف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSettleCommission_Click(object sender, EventArgs e)
        {
            if (_lastCalculatedEmpID <= 0 || _lastCalculatedCommission <= 0)
            {
                MessageBox.Show("يرجى احتساب العمولات أولاً والتأكد من وجود مبلغ عمولة مستحق (> 0).", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string empName = (cboCalcEmp.SelectedItem is ComboItem ci) ? ci.Name : "";
            string msg = $"هل تريد ترحيل عمولة المبيعات وقدرها ({_lastCalculatedCommission:N2} ج) لحساب الموظف [{empName}] عن الفترة من {dtpCalcFrom.Value:yyyy/MM/dd} إلى {dtpCalcTo.Value:yyyy/MM/dd}؟";

            if (MessageBox.Show(msg, "تأكيد ترحيل العمولة", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    string pMonth = dtpCalcTo.Value.ToString("yyyy-MM");
                    string reason = $"عمولة مبيعات عن الفترة {dtpCalcFrom.Value:yyyy/MM/dd} إلى {dtpCalcTo.Value:yyyy/MM/dd}";

                    int itemId = EmployeeHRDAL.SaveSalaryItem(_lastCalculatedEmpID, DateTime.Now, "عمولة", _lastCalculatedCommission, reason, pMonth, affectCash: false);

                    // تسجيل قيد دائن في حساب الموظف
                    EmployeeDAL.SaveTransaction(_lastCalculatedEmpID, DateTime.Now, "Commission", 0, _lastCalculatedCommission, reason, affectCash: false);

                    MessageBox.Show($"✅ تم ترحيل العمولة ({_lastCalculatedCommission:N2} ج) بنجاح إلى حساب الموظف ومسير رواتب شهر {pMonth}.", "تم الترحيل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء ترحيل العمولة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // التبويب الثاني: عمولات الأصناف المحددة
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildProductRulesTab(TabPage tab)
        {
            tab.BackColor = Theme.BgMain;

            var pnlToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            var lblEmp = new Label { Text = "👤 الموظف / المندوب:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 7, 0, 0), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            cboProdEmp = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = new Font("Segoe UI", 10f) };
            cboProdEmp.SelectedIndexChanged += (s, e) => LoadProductRules();

            btnAddProdRule = Theme.MakeButton("➕ إضافة عمولة صنف جديد", 0, 0, 180, 36, Theme.Primary);
            btnAddProdRule.Click += BtnAddProdRule_Click;

            btnDeleteProdRule = Theme.MakeButton("🗑️ حذف العمولة المحددة", 0, 0, 160, 36, Color.FromArgb(185, 28, 28));
            btnDeleteProdRule.Click += BtnDeleteProdRule_Click;

            pnlToolbar.Controls.AddRange(new Control[] { lblEmp, cboProdEmp, btnAddProdRule, btnDeleteProdRule });

            dgProductRules = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 }
            };

            dgProductRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "RuleID", Visible = false });
            dgProductRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 60 });
            dgProductRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 140 });
            dgProductRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 45 });
            dgProductRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 55 });
            dgProductRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "CommissionType", HeaderText = "نوع العمولة", FillWeight = 65 });
            dgProductRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "CommissionValue", HeaderText = "قيمة العمولة", FillWeight = 65 });
            dgProductRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 100 });

            tab.Controls.Add(dgProductRules);
            tab.Controls.Add(pnlToolbar);
        }

        private void LoadProductRules()
        {
            if (cboProdEmp.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                var dt = EmployeeHRDAL.GetProductCommissions(ci.ID);
                dgProductRules.Rows.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    string cType = r["CommissionType"]?.ToString() == "Percentage" ? "نسبة مئوية (%)" : "مبلغ ثابت للقطعة";
                    decimal val = Convert.ToDecimal(r["CommissionValue"]);
                    decimal sp = r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m;
                    string valStr = r["CommissionType"]?.ToString() == "Percentage" ? $"{val:N1}%" : $"{val:N2} ج";

                    dgProductRules.Rows.Add(
                        r["RuleID"],
                        r["ProductCode"],
                        r["ProductName"],
                        r["Unit"],
                        sp.ToString("N2"),
                        cType,
                        valStr,
                        r["Notes"]
                    );
                }
            }
            else
            {
                dgProductRules.Rows.Clear();
            }
        }

        private void BtnAddProdRule_Click(object sender, EventArgs e)
        {
            if (cboProdEmp.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                using (var dlg = new FrmAddProductCommissionDialog(ci.ID, ci.Name))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadProductRules();
                    }
                }
            }
            else
            {
                MessageBox.Show("يرجى اختيار الموظف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDeleteProdRule_Click(object sender, EventArgs e)
        {
            if (dgProductRules.SelectedRows.Count == 0) return;
            int ruleId = Convert.ToInt32(dgProductRules.SelectedRows[0].Cells["RuleID"].Value);
            string pName = dgProductRules.SelectedRows[0].Cells["ProductName"].Value?.ToString() ?? "";

            if (MessageBox.Show($"هل تريد حذف قاعدة عمولة الصنف [{pName}]؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (EmployeeHRDAL.DeleteProductCommission(ruleId))
                {
                    LoadProductRules();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // التبويب الثالث: شرائح المبيعات والتارجت
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildTiersTab(TabPage tab)
        {
            tab.BackColor = Theme.BgMain;

            var pnlToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            var lblEmp = new Label { Text = "👤 الموظف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 7, 0, 0), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            cboTierEmp = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = new Font("Segoe UI", 10f) };
            cboTierEmp.SelectedIndexChanged += (s, e) => LoadTiers();

            btnAddTier = Theme.MakeButton("➕ إضافة شريحة مبيعات جديدة", 0, 0, 200, 36, Theme.Primary);
            btnAddTier.Click += BtnAddTier_Click;

            btnDeleteTier = Theme.MakeButton("🗑️ حذف الشريحة", 0, 0, 130, 36, Color.FromArgb(185, 28, 28));
            btnDeleteTier.Click += BtnDeleteTier_Click;

            pnlToolbar.Controls.AddRange(new Control[] { lblEmp, cboTierEmp, btnAddTier, btnDeleteTier });

            dgTiers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 }
            };

            dgTiers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TierID", Visible = false });
            dgTiers.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinTarget", HeaderText = "الحد الأدنى للمبيعات (من)", FillWeight = 80 });
            dgTiers.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaxTarget", HeaderText = "الحد الأقصى للمبيعات (إلى)", FillWeight = 80 });
            dgTiers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CommissionRate", HeaderText = "نسبة العمولة (%)", FillWeight = 60 });
            dgTiers.Columns.Add(new DataGridViewTextBoxColumn { Name = "BonusAmount", HeaderText = "مكافأة إضافية للتارجت", FillWeight = 70 });

            tab.Controls.Add(dgTiers);
            tab.Controls.Add(pnlToolbar);
        }

        private void LoadTiers()
        {
            int empId = (cboTierEmp.SelectedItem is ComboItem ci) ? ci.ID : 0;
            var dt = EmployeeHRDAL.GetCommissionTiers(empId);
            dgTiers.Rows.Clear();
            foreach (DataRow r in dt.Rows)
            {
                decimal min = Convert.ToDecimal(r["MinTarget"]);
                decimal max = Convert.ToDecimal(r["MaxTarget"]);
                decimal rate = Convert.ToDecimal(r["CommissionRate"]);
                decimal bonus = Convert.ToDecimal(r["BonusAmount"]);

                dgTiers.Rows.Add(
                    r["TierID"],
                    min.ToString("N2") + " ج",
                    max > 0 ? max.ToString("N2") + " ج" : "بلا حد أقصى",
                    $"{rate:N1}%",
                    bonus > 0 ? bonus.ToString("N2") + " ج" : "—"
                );
            }
        }

        private void BtnAddTier_Click(object sender, EventArgs e)
        {
            int? empId = (cboTierEmp.SelectedItem is ComboItem ci && ci.ID > 0) ? (int?)ci.ID : null;

            var dlg = new Form
            {
                Text = "➕ إضافة شريحة مبيعات جديدة",
                Size = new Size(420, 320),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                RightToLeft = RightToLeft.Yes, RightToLeftLayout = true,
                BackColor = Theme.BgMain, Font = Theme.FontMain
            };

            var lbl1 = new Label { Text = "الحد الأدنى للمبيعات (من ج.م):", Location = new Point(20, 20), AutoSize = true };
            var txtMin = new TextBox { Location = new Point(20, 45), Width = 360, Font = new Font("Segoe UI", 10.5f) };

            var lbl2 = new Label { Text = "الحد الأقصى للمبيعات (إلى ج.م - اختياري):", Location = new Point(20, 80), AutoSize = true };
            var txtMax = new TextBox { Location = new Point(20, 105), Width = 360, Font = new Font("Segoe UI", 10.5f), Text = "0" };

            var lbl3 = new Label { Text = "نسبة العمولة (%):", Location = new Point(20, 140), AutoSize = true };
            var txtRate = new TextBox { Location = new Point(20, 165), Width = 170, Font = new Font("Segoe UI", 10.5f), Text = "2" };

            var lbl4 = new Label { Text = "مكافأة التارجت (ج.م):", Location = new Point(210, 140), AutoSize = true };
            var txtBonus = new TextBox { Location = new Point(210, 165), Width = 170, Font = new Font("Segoe UI", 10.5f), Text = "0" };

            var btnSave = Theme.MakeButton("💾 حفظ الشريحة", 200, 220, 180, 36, Theme.Success);
            btnSave.Click += (s2, e2) =>
            {
                if (decimal.TryParse(txtMin.Text.Trim(), out decimal min) &&
                    decimal.TryParse(txtRate.Text.Trim(), out decimal rate))
                {
                    decimal.TryParse(txtMax.Text.Trim(), out decimal max);
                    decimal.TryParse(txtBonus.Text.Trim(), out decimal bonus);

                    if (EmployeeHRDAL.SaveCommissionTier(0, empId, min, max, rate, bonus))
                    {
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                }
                else
                {
                    MessageBox.Show("يرجى إدخال قيم رقمية صحيحة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            dlg.Controls.AddRange(new Control[] { lbl1, txtMin, lbl2, txtMax, lbl3, txtRate, lbl4, txtBonus, btnSave });

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadTiers();
            }
        }

        private void BtnDeleteTier_Click(object sender, EventArgs e)
        {
            if (dgTiers.SelectedRows.Count == 0) return;
            int tid = Convert.ToInt32(dgTiers.SelectedRows[0].Cells["TierID"].Value);
            if (MessageBox.Show("هل تريد حذف شريحة المبيعات المحددة؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (EmployeeHRDAL.DeleteCommissionTier(tid))
                {
                    LoadTiers();
                }
            }
        }

        private void LoadEmployeesDropdowns()
        {
            var dt = EmployeeDAL.GetAll();

            cboCalcEmp.Items.Clear();
            cboProdEmp.Items.Clear();
            cboTierEmp.Items.Clear();

            cboCalcEmp.Items.Add(new ComboItem(0, "-- اختر الموظف / المندوب --"));
            cboProdEmp.Items.Add(new ComboItem(0, "-- اختر الموظف / المندوب --"));
            cboTierEmp.Items.Add(new ComboItem(0, "-- الشريحة العامة (لكل الموظفين) --"));

            foreach (DataRow r in dt.Rows)
            {
                int id = (int)r["EmpID"];
                string name = r["EmpName"].ToString();
                cboCalcEmp.Items.Add(new ComboItem(id, name));
                cboProdEmp.Items.Add(new ComboItem(id, name));
                cboTierEmp.Items.Add(new ComboItem(id, name));
            }

            cboCalcEmp.DisplayMember = "Text";
            cboProdEmp.DisplayMember = "Text";
            cboTierEmp.DisplayMember = "Text";

            cboCalcEmp.SelectedIndex = 0;
            cboProdEmp.SelectedIndex = 0;
            cboTierEmp.SelectedIndex = 0;
        }

        private void BtnPrintCommReport_Click(object sender, EventArgs e)
        {
            if (dgCalcDetails.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PrintDocument doc = new PrintDocument();
            doc.DefaultPageSettings.Landscape = true;
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 40;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 10f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);

                string empName = (cboCalcEmp.SelectedItem is ComboItem ci) ? ci.Name : "";
                g.DrawString($"تقرير عمولات المبيعات المفصل - الموظف: {empName}", fontTitle, Brushes.Black, new PointF(ev.PageBounds.Width / 2 - 180, y));
                y += 35;
                g.DrawString($"الفترة من: {dtpCalcFrom.Value:yyyy/MM/dd}  إلى: {dtpCalcTo.Value:yyyy/MM/dd}   |   {lblCalcTotalCommission.Text}", fontBody, Brushes.Gray, new PointF(ev.PageBounds.Width / 2 - 160, y));
                y += 35;

                float[] colWidths = { 90, 80, 140, 160, 60, 70, 80, 160, 80 };
                string[] headers = { "الفاتورة", "التاريخ", "العميل", "الصنف", "الكمية", "السعر", "الإجمالي", "قاعدة العمولة", "العمولة" };

                float x = 40;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightGray, x, y, colWidths[i], 26);
                    g.DrawRectangle(Pens.Gray, x, y, colWidths[i], 26);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 4, y + 4);
                    x += colWidths[i];
                }
                y += 26;

                foreach (DataGridViewRow r in dgCalcDetails.Rows)
                {
                    if (y > ev.PageBounds.Height - 60) break;
                    x = 40;
                    string[] vals = {
                        r.Cells["SaleCode"].Value?.ToString() ?? "",
                        r.Cells["SaleDate"].Value?.ToString() ?? "",
                        r.Cells["ClientName"].Value?.ToString() ?? "",
                        r.Cells["ProductName"].Value?.ToString() ?? "",
                        r.Cells["Quantity"].Value?.ToString() ?? "",
                        r.Cells["UnitPrice"].Value?.ToString() ?? "",
                        r.Cells["TotalPrice"].Value?.ToString() ?? "",
                        r.Cells["CommissionRule"].Value?.ToString() ?? "",
                        r.Cells["CommissionAmount"].Value?.ToString() ?? ""
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 24);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 3, y + 4);
                        x += colWidths[i];
                    }
                    y += 24;
                }
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 950, Height = 650 })
            {
                dlg.ShowDialog(this);
            }
        }
    }

    /// <summary>
    /// نافذة منبثقة لإضافة عمولة صنف مخصص لموظف
    /// </summary>
    public class FrmAddProductCommissionDialog : Form
    {
        private int _empID;
        private ComboBox cboProduct, cboType;
        private TextBox txtValue, txtNotes;
        private Button btnSave, btnCancel;

        public FrmAddProductCommissionDialog(int empID, string empName)
        {
            _empID = empID;
            this.Text = $"➕ إضافة عمولة صنف للموظف: [{empName}]";
            this.Size = new Size(460, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes; this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain; this.Font = Theme.FontMain;

            var lblP = new Label { Text = "📦 اختر الصنف:", Location = new Point(25, 20), AutoSize = true };
            cboProduct = new ComboBox
            {
                Location = new Point(25, 45),
                Width = 390,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Font = new Font("Segoe UI", 10f)
            };

            var lblT = new Label { Text = "نوع العمولة:", Location = new Point(25, 85), AutoSize = true };
            cboType = new ComboBox
            {
                Location = new Point(25, 110),
                Width = 390,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f)
            };
            cboType.Items.Add("مبلغ ثابت لكل قطعة/وحدة مباعة (ج.م)");
            cboType.Items.Add("نسبة مئوية من سعر بيع الصنف (%)");
            cboType.SelectedIndex = 0;

            var lblV = new Label { Text = "قيمة العمولة:", Location = new Point(25, 150), AutoSize = true };
            txtValue = new TextBox { Location = new Point(25, 175), Width = 390, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = "5" };

            var lblN = new Label { Text = "ملاحظات:", Location = new Point(25, 215), AutoSize = true };
            txtNotes = new TextBox { Location = new Point(25, 240), Width = 390, Font = new Font("Segoe UI", 9.5f) };

            btnSave = Theme.MakeButton("💾 حفظ العمولة", 235, 275, 180, 36, Theme.Success);
            btnCancel = Theme.MakeButton("إلغاء", 25, 275, 100, 36, Color.FromArgb(100, 110, 120));

            btnSave.Click += (s, e) =>
            {
                if (cboProduct.SelectedItem is ComboItem pi && pi.ID > 0)
                {
                    if (decimal.TryParse(txtValue.Text.Trim(), out decimal val) && val > 0)
                    {
                        string cType = cboType.SelectedIndex == 1 ? "Percentage" : "Fixed";
                        if (EmployeeHRDAL.SaveProductCommission(0, _empID, pi.ID, cType, val, txtNotes.Text.Trim()))
                        {
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show("يرجى إدخال قيمة عمولة صحيحة أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("يرجى اختيار الصنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { lblP, cboProduct, lblT, cboType, lblV, txtValue, lblN, txtNotes, btnSave, btnCancel });

            LoadProducts();
        }

        private void LoadProducts()
        {
            var dt = DbHelper.Query("SELECT ProductID, ProductName, ProductCode FROM Products WHERE IsActive = 1 ORDER BY ProductName");
            cboProduct.Items.Clear();
            cboProduct.Items.Add(new ComboItem(0, "-- اختر الصنف --"));
            foreach (DataRow r in dt.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                string name = r["ProductName"]?.ToString() ?? "";
                string code = r["ProductCode"]?.ToString() ?? "";
                cboProduct.Items.Add(new ComboItem(pid, $"{name} [{code}]"));
            }
            cboProduct.DisplayMember = "Text";
            cboProduct.SelectedIndex = 0;
        }
    }
}

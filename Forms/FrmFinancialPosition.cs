using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmFinancialPosition : Form
    {
        private Label lblTotalCash;
        private Label lblInventoryPurchase;
        private Label lblClientReceivables;
        private Label lblSupplierPayables;
        private Label lblNetWorth;
        private Label lblExpectedAssets;

        private DataGridView dgSafes;
        private DataGridView dgTopClients;
        private DataGridView dgTopSuppliers;

        public FrmFinancialPosition()
        {
            InitUI();
            LoadFinancialData();
        }

        private void InitUI()
        {
            this.Text = "📊 تقرير الموقف المالي للمكان (ميزانية عمومية مصغرة)";
            this.Size = new Size(1180, 740);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── العنوان الرئيسي ──
            var pnlTitle = Theme.MakeTitleBar("📊 الموقف المالي ورأس المال العامل للمكان", "عرض شامل وتحليلي لكافة الأصول السائلة، البضائع، مديونيات العملاء، والتزامات الموردين");
            pnlTitle.Dock = DockStyle.Top;
            this.Controls.Add(pnlTitle);

            // ── الهيكل الرئيسي ──
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(15)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f)); // الصف العلوي: الكروت الإحصائية
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // الصف السفلي: تفاصيل الجداول
            this.Controls.Add(mainLayout);
            mainLayout.BringToFront();

            // ── 1. لوحة كروت الموقف المالي ──
            TableLayoutPanel pnlCards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            mainLayout.Controls.Add(pnlCards, 0, 0);

            pnlCards.Controls.Add(CreateFinancialCard("💵 النقدية بالخزائن والبنوك", "0.00 ج", Theme.Primary, out lblTotalCash), 0, 0);
            pnlCards.Controls.Add(CreateFinancialCard("📦 قيمة المخزون (بالشراء)", "0.00 ج", Theme.Accent, out lblInventoryPurchase), 1, 0);
            pnlCards.Controls.Add(CreateFinancialCard("👥 مستحقات العملاء طرفنا", "0.00 ج", Theme.Success, out lblClientReceivables), 2, 0);
            pnlCards.Controls.Add(CreateFinancialCard("🏢 مطلوبات الموردين منا", "0.00 ج", Theme.Danger, out lblSupplierPayables), 3, 0);
            
            // كارت صافي القيمة المالية (رأس المال الفعلي)
            pnlCards.Controls.Add(CreateFinancialCard("📈 صافي رأس المال الفعلي", "0.00 ج", Color.FromArgb(111, 66, 193), out lblNetWorth), 4, 0);

            // ── 2. لوحة التفاصيل والجداول ──
            TableLayoutPanel pnlDetails = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(0, 15, 0, 0)
            };
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f)); // الخزائن والبنوك
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f)); // كبار العملاء المدينين
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f)); // كبار الموردين الدائنين
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.Controls.Add(pnlDetails, 0, 1);

            // الخزائن والبنوك التفصيلي
            pnlDetails.Controls.Add(BuildDetailSection("🏦 أرصدة الخزائن والحسابات البنكية", out dgSafes, new[] {
                ("SafeName", "اسم الحساب/الخزنة", 120),
                ("SafeType", "النوع", 80),
                ("Balance", "الرصيد الحالي", 90)
            }), 0, 0);

            // العملاء المدينين
            pnlDetails.Controls.Add(BuildDetailSection("👥 كبار العملاء (مديونيات مستحقة)", out dgTopClients, new[] {
                ("ClientName", "اسم العميل", 120),
                ("Phone", "الهاتف", 90),
                ("Balance", "المبلغ المستحق", 90)
            }), 1, 0);

            // الموردين الدائنين
            pnlDetails.Controls.Add(BuildDetailSection("🏢 كبار الموردين (التزامات دفع)", out dgTopSuppliers, new[] {
                ("SupplierName", "اسم المورد", 120),
                ("Phone", "الهاتف", 90),
                ("Balance", "المطلوب سداده", 90)
            }), 2, 0);

            // إضافة تسمية فرعية لقيمة المخزون بالبيع
            lblExpectedAssets = new Label
            {
                Text = "إجمالي تكلفة المخزون (بالشراء): 0.00 ج   |   القيمة البيعية المتوقعة: 0.00 ج   |   أرباح المخزون المتوقعة: 0.00 ج",
                Dock = DockStyle.Bottom,
                Height = 35,
                ForeColor = Theme.TextSub,
                Font = new Font(Theme.FontBold.FontFamily, 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.BgCard
            };
            this.Controls.Add(lblExpectedAssets);

            Theme.ApplyFormRTL(this);
        }

        private Panel CreateFinancialCard(string title, string value, Color color, out Label valLabel)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(6),
                Padding = new Padding(10)
            };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Theme.TextSub,
                Font = Theme.FontNormal,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnl.Controls.Add(lblTitle);

            valLabel = new Label
            {
                Text = value,
                Dock = DockStyle.Fill,
                ForeColor = color,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnl.Controls.Add(valLabel);

            return pnl;
        }

        private Panel BuildDetailSection(string title, out DataGridView dg, (string name, string headerText, int width)[] columns)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(8),
                Padding = new Padding(10)
            };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 35,
                ForeColor = Theme.Accent,
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnl.Controls.Add(lblTitle);

            dg = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dg.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.BgCard,
                ForeColor = Theme.TextMain,
                SelectionBackColor = Theme.Primary,
                SelectionForeColor = Color.White,
                Font = Theme.FontNormal
            };
            dg.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = Theme.FontBold,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dg.EnableHeadersVisualStyles = false;

            foreach (var col in columns)
            {
                dg.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = col.name,
                    HeaderText = col.headerText,
                    FillWeight = col.width
                });
            }

            pnl.Controls.Add(dg);
            lblTitle.BringToFront();

            return pnl;
        }

        private void LoadFinancialData()
        {
            try
            {
                // 1. حساب إجمالي النقدية
                object cashObj = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn - AmountOut), 0) FROM CashBox");
                decimal totalCash = cashObj != null ? Convert.ToDecimal(cashObj) : 0m;
                lblTotalCash.Text = $"{totalCash:N2} ج";

                // 2. حساب قيمة البضاعة بسعر الشراء وسعر البيع
                object purObj = DbHelper.Scalar("SELECT ISNULL(SUM(ps.Quantity * p.PurchasePrice), 0) FROM ProductStock ps JOIN Products p ON ps.ProductID = p.ProductID");
                decimal invPurchase = purObj != null ? Convert.ToDecimal(purObj) : 0m;
                lblInventoryPurchase.Text = $"{invPurchase:N2} ج";

                object saleObj = DbHelper.Scalar("SELECT ISNULL(SUM(ps.Quantity * p.SalePrice), 0) FROM ProductStock ps JOIN Products p ON ps.ProductID = p.ProductID");
                decimal invSale = saleObj != null ? Convert.ToDecimal(saleObj) : 0m;

                decimal expectedProfit = invSale - invPurchase;
                lblExpectedAssets.Text = $"إجمالي تكلفة المخزون (بالشراء): {invPurchase:N2} ج   |   القيمة البيعية المتوقعة: {invSale:N2} ج   |   أرباح المخزون المتوقعة: {expectedProfit:N2} ج";

                // 3. حساب مستحقات العملاء (المدينين فقط)
                object clientObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(CurrentBalance), 0) FROM (
                        SELECT c.OpeningBalance + 
                               ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                               ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) AS CurrentBalance
                        FROM Clients c
                    ) t WHERE CurrentBalance > 0");
                decimal clientReceivables = clientObj != null ? Convert.ToDecimal(clientObj) : 0m;
                lblClientReceivables.Text = $"{clientReceivables:N2} ج";

                // 4. حساب مطلوبات الموردين (الدائنين فقط)
                object supplierObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(Balance), 0) FROM (
                        SELECT s.OpeningBalance + 
                               ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                               ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) AS Balance
                        FROM Suppliers s
                    ) t WHERE Balance > 0");
                decimal supplierPayables = supplierObj != null ? Convert.ToDecimal(supplierObj) : 0m;
                lblSupplierPayables.Text = $"{supplierPayables:N2} ج";

                // 5. صافي رأس المال الفعلي = النقدية + قيمة البضاعة بالشراء + مستحقات العملاء - مطلوبات الموردين
                decimal netWorth = totalCash + invPurchase + clientReceivables - supplierPayables;
                lblNetWorth.Text = $"{netWorth:N2} ج";

                // 6. تحميل تفاصيل الحسابات البنكية والخزائن
                var dtSafes = DbHelper.Query(@"
                    SELECT 
                        sa.AccountName AS SafeName,
                        CASE sa.AccountType 
                            WHEN 'Cash' THEN N'خزينة نقدية' 
                            WHEN 'Bank' THEN N'حساب بنكي' 
                            ELSE N'شبكة/فيزا' END AS SafeType,
                        ISNULL((SELECT SUM(AmountIn - AmountOut) FROM CashBox WHERE AccountID = sa.AccountID), 0) AS Balance
                    FROM SafeAccounts sa
                    ORDER BY sa.AccountName");
                
                dgSafes.Rows.Clear();
                foreach (DataRow r in dtSafes.Rows)
                {
                    dgSafes.Rows.Add(r["SafeName"], r["SafeType"], Convert.ToDecimal(r["Balance"]).ToString("N2") + " ج");
                }

                // 7. تحميل كبار العملاء المدينين
                var dtClients = DbHelper.Query(@"
                    SELECT TOP 10
                        c.ClientName,
                        c.Phone,
                        (c.OpeningBalance + 
                         ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                         ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0)) AS Balance
                    FROM Clients c
                    WHERE (c.OpeningBalance + 
                           ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                           ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0)) > 0
                    ORDER BY Balance DESC");

                dgTopClients.Rows.Clear();
                foreach (DataRow r in dtClients.Rows)
                {
                    dgTopClients.Rows.Add(r["ClientName"], r["Phone"], Convert.ToDecimal(r["Balance"]).ToString("N2") + " ج");
                }

                // 8. تحميل كبار الموردين الدائنين
                var dtSuppliers = DbHelper.Query(@"
                    SELECT TOP 10
                        s.SupplierName,
                        s.Phone,
                        (s.OpeningBalance + 
                         ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                         ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0)) AS Balance
                    FROM Suppliers s
                    WHERE (s.OpeningBalance + 
                           ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                           ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0)) > 0
                    ORDER BY Balance DESC");

                dgTopSuppliers.Rows.Clear();
                foreach (DataRow r in dtSuppliers.Rows)
                {
                    dgTopSuppliers.Rows.Add(r["SupplierName"], r["Phone"], Convert.ToDecimal(r["Balance"]).ToString("N2") + " ج");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل تحميل بيانات الموقف المالي: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة تفاصيل حركة الدرج خلال الوردية (محمية بالصلاحيات)
    /// تعرض: إجمالي البيع الكاش + التوريدات - (المصروفات + المرتجعات)
    /// </summary>
    public class FrmShiftDrawerDetails : Form
    {
        private int _shiftID;
        private DataRow _shiftRow;

        private Label lblHeaderInfo;
        private Label lblOpeningCashVal, lblCashSalesVal, lblCashInVal, lblExpensesVal, lblReturnsVal, lblNetMovementVal, lblExpectedVal;
        private DataGridView dgDrawerDetails;
        private Button btnClose, btnPrint;

        public FrmShiftDrawerDetails(int shiftID)
        {
            if (!Session.CanViewShiftDetails())
            {
                MessageBox.Show("⛔ غير مصرح لك بعرض تفاصيل وحركة درج الوردية.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }
            _shiftID = shiftID;
            InitUI();
            LoadDrawerData();
        }

        private void InitUI()
        {
            this.Text = $"🔍 تفاصيل حركة الدرج للوردية #{_shiftID}";
            this.Size = new Size(860, 640);
            this.MinimumSize = new Size(800, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // 1. Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };
            Label lblTitle = new Label
            {
                Text = $"🔍 حركة وتفاصيل صندوق/درج الوردية #{_shiftID}",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 24
            };
            lblHeaderInfo = new Label
            {
                Text = "جاري تحميل البيانات...",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.Accent,
                Dock = DockStyle.Top,
                Height = 20
            };
            pnlHeader.Controls.Add(lblHeaderInfo);
            pnlHeader.Controls.Add(lblTitle);

            // 2. Summary KPI Grid (2 rows of 4 cards)
            TableLayoutPanel tblKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 130,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(10, 6, 10, 6),
                RightToLeft = RightToLeft.Yes
            };
            for (int i = 0; i < 4; i++) tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            lblOpeningCashVal = MakeCard(tblKpi, "💵 رصيد فتح الوردية", "0.00 ج", Theme.TextMain, 0, 0);
            lblCashSalesVal   = MakeCard(tblKpi, "🛒 مبيعات فواتير الكاش", "0.00 ج", Theme.Success, 1, 0);
            lblCashInVal      = MakeCard(tblKpi, "➕ إيرادات وتوريدات الدرج", "0.00 ج", Color.FromArgb(52, 152, 219), 2, 0);
            lblReturnsVal     = MakeCard(tblKpi, "↩ مرتجعات المبيعات", "0.00 ج", Theme.Danger, 3, 0);

            lblExpensesVal    = MakeCard(tblKpi, "💸 مصروفات وسحبيات", "0.00 ج", Color.FromArgb(230, 126, 34), 0, 1);
            lblNetMovementVal = MakeCard(tblKpi, "⚖️ صافي حركة الدرج", "0.00 ج", Color.FromArgb(155, 89, 182), 1, 1);
            lblExpectedVal    = MakeCard(tblKpi, "💰 الصافي المتوقع بالدرج", "0.00 ج", Theme.Accent, 2, 1);
            
            // Card 4 Row 1 (Empty space or note)
            MakeCard(tblKpi, "📋 معادلة الدرج", "(كاش + توريد) - (مصروفات + مرتجع)", Theme.TextSub, 3, 1);

            // 3. Ledger Grid
            dgDrawerDetails = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5f),
                RowTemplate = { Height = 30 },
                EnableHeadersVisualStyles = false
            };
            dgDrawerDetails.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 50, 65);
            dgDrawerDetails.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgDrawerDetails.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgDrawerDetails.ColumnHeadersHeight = 34;
            dgDrawerDetails.DefaultCellStyle.BackColor = Theme.BgCard;

            dgDrawerDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "نوع الحركة", FillWeight = 60f });
            dgDrawerDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "الوقت", FillWeight = 60f });
            dgDrawerDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefCode", HeaderText = "رقم المرجع", FillWeight = 60f });
            dgDrawerDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "البيان والتفاصيل", FillWeight = 140f });
            dgDrawerDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "In", HeaderText = "وارد للدرج (+)", FillWeight = 60f });
            dgDrawerDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Out", HeaderText = "صادر من الدرج (-)", FillWeight = 60f });

            Panel pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 4, 10, 4) };
            pnlGrid.Controls.Add(dgDrawerDetails);

            // 4. Footer Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnClose = Theme.MakeButton("❌ إغلاق", Theme.Danger, new Point(0, 0), new Size(110, 38));
            btnPrint = Theme.MakeButton("🖨️ طباعة الحركة", Theme.Primary, new Point(0, 0), new Size(140, 38));

            btnClose.Click += (s, e) => this.Close();
            btnPrint.Click += BtnPrint_Click;

            FlowLayoutPanel flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };
            btnPrint.Margin = new Padding(6, 0, 0, 0);
            btnClose.Margin = new Padding(6, 0, 0, 0);
            flowBottom.Controls.Add(btnPrint);
            flowBottom.Controls.Add(btnClose);
            pnlBottom.Controls.Add(flowBottom);

            // Assembly Order
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(tblKpi);
            this.Controls.Add(pnlHeader);
            pnlGrid.BringToFront();
        }

        private Label MakeCard(TableLayoutPanel parent, string title, string val, Color valColor, int col, int row)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(2),
                Padding = new Padding(4)
            };
            pnl.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnl);

            Label lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblV = new Label
            {
                Text = val,
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.Add(lblV);
            pnl.Controls.Add(lblT);
            parent.Controls.Add(pnl, col, row);
            return lblV;
        }

        private void LoadDrawerData()
        {
            try
            {
                DbHelper.EnsureShiftSchema();
                DataTable dtShift = DbHelper.Query(@"
                    SELECT s.*, e.EmpName AS OpenedByName, sa.AccountName AS SafeName
                    FROM Shifts s
                    LEFT JOIN Employees e ON s.OpenedBy = e.EmpID
                    LEFT JOIN SafeAccounts sa ON s.SafeAccountID = sa.AccountID
                    WHERE s.ShiftID = @sid",
                    DbHelper.P("@sid", _shiftID));

                if (dtShift.Rows.Count == 0) return;
                _shiftRow = dtShift.Rows[0];

                DateTime openTime = Convert.ToDateTime(_shiftRow["OpenTime"]);
                string emp = _shiftRow["OpenedByName"] != DBNull.Value ? _shiftRow["OpenedByName"].ToString() : "الكاشير";
                string safe = _shiftRow["SafeName"] != DBNull.Value ? _shiftRow["SafeName"].ToString() : "درج الكاشير";
                decimal openingCash = _shiftRow["OpeningCash"] != DBNull.Value ? Convert.ToDecimal(_shiftRow["OpeningCash"]) : 0m;

                lblHeaderInfo.Text = $"👤 الكاشير: {emp}  |  🏦 الدرج: {safe}  |  📅 وقت الفتح: {openTime:yyyy-MM-dd HH:mm}";

                // 1. فواتير البيع الكاش
                var dtSales = DbHelper.Query(@"
                    SELECT ISNULL(SUM(TotalAmount), 0) AS CashSales
                    FROM Sales 
                    WHERE (ShiftID = @sid OR (ShiftID IS NULL AND SaleDate >= @dt)) AND SaleType = 'Cash' AND IsPosted = 1",
                    DbHelper.P("@sid", _shiftID), DbHelper.P("@dt", openTime));
                decimal cashSales = dtSales.Rows.Count > 0 ? Convert.ToDecimal(dtSales.Rows[0]["CashSales"]) : 0m;

                // 2. التوريدات النقدية الواردة للدرج
                var dtCashIn = DbHelper.Query(@"
                    SELECT ISNULL(SUM(AmountIn), 0) AS TotalCashIn
                    FROM CashBox 
                    WHERE TransDate >= @dt AND TransType NOT IN ('Sale', 'SaleReturn', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen') AND AmountIn > 0",
                    DbHelper.P("@dt", openTime));
                decimal cashIn = dtCashIn.Rows.Count > 0 ? Convert.ToDecimal(dtCashIn.Rows[0]["TotalCashIn"]) : 0m;

                // 3. مرتجعات المبيعات المدفوعة نقدياً
                var dtReturns = DbHelper.Query(@"
                    SELECT ISNULL(SUM(sr.TotalAmount), 0) AS CashReturns
                    FROM SalesReturns sr
                    JOIN Sales s ON sr.SaleID = s.SaleID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt)) AND s.SaleType = 'Cash'",
                    DbHelper.P("@sid", _shiftID), DbHelper.P("@dt", openTime));
                decimal cashReturns = dtReturns.Rows.Count > 0 ? Convert.ToDecimal(dtReturns.Rows[0]["CashReturns"]) : 0m;

                // 4. المصروفات والسحبيات النقدية من الدرج
                var dtExp = DbHelper.Query(@"
                    SELECT ISNULL(SUM(AmountOut), 0) AS TotalExpenses
                    FROM CashBox 
                    WHERE TransDate >= @dt AND TransType NOT IN ('Sale', 'SaleReturn', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen') AND AmountOut > 0",
                    DbHelper.P("@dt", openTime));
                decimal expenses = dtExp.Rows.Count > 0 ? Convert.ToDecimal(dtExp.Rows[0]["TotalExpenses"]) : 0m;

                // الصافي والمتوقع
                decimal netMovement = (cashSales + cashIn) - (expenses + cashReturns);
                decimal expectedCash = openingCash + netMovement;

                lblOpeningCashVal.Text = openingCash.ToString("N2") + " ج";
                lblCashSalesVal.Text   = cashSales.ToString("N2")   + " ج";
                lblCashInVal.Text      = cashIn.ToString("N2")      + " ج";
                lblReturnsVal.Text     = cashReturns.ToString("N2") + " ج";
                lblExpensesVal.Text    = expenses.ToString("N2")    + " ج";

                lblNetMovementVal.Text = netMovement >= 0 ? $"+{netMovement:N2} ج" : $"{netMovement:N2} ج";
                lblNetMovementVal.ForeColor = netMovement >= 0 ? Theme.Success : Theme.Danger;

                lblExpectedVal.Text    = expectedCash.ToString("N2") + " ج";

                // ملء شبكة الحركة
                dgDrawerDetails.Rows.Clear();
                var dtDetails = DbHelper.Query(@"
                    SELECT 'فاتورة كاش (+)' AS TransType, s.SaleDate AS TransTime, s.SaleCode AS RefCode, ISNULL(c.ClientName, N'عميل نقدي') AS Details, s.TotalAmount AS AmountIn, 0.00 AS AmountOut
                    FROM Sales s
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt)) AND s.SaleType = 'Cash' AND s.IsPosted = 1
                    UNION ALL
                    SELECT 'مرتجع كاش (-)' AS TransType, sr.ReturnDate AS TransTime, CAST(sr.ReturnID AS NVARCHAR) AS RefCode, N'مرتجع فاتورة كاش' AS Details, 0.00 AS AmountIn, sr.TotalAmount AS AmountOut
                    FROM SalesReturns sr
                    JOIN Sales s ON sr.SaleID = s.SaleID
                    WHERE (s.ShiftID = @sid OR (s.ShiftID IS NULL AND s.SaleDate >= @dt)) AND s.SaleType = 'Cash'
                    UNION ALL
                    SELECT 
                        CASE 
                            WHEN TransType = 'ShiftCloseOut' THEN 'تحويل تقفيل صادر (-)'
                            WHEN TransType = 'ShiftCloseIn'  THEN 'استلام تقفيل وارد (+)'
                            WHEN TransType = 'ShiftClose'    THEN 'تقفيل وردية (إبقاء)'
                            WHEN TransType = 'ShiftDeficit'  THEN 'تسوية عجز وردية (-)'
                            WHEN TransType = 'ShiftSurplus'  THEN 'تسوية زيادة وردية (+)'
                            WHEN AmountIn > 0 THEN 'توريد/إيراد (+)' 
                            ELSE 'مصروف/سحب (-)' 
                        END AS TransType, 
                        TransDate AS TransTime, 
                        CAST(CashID AS NVARCHAR) AS RefCode, 
                        Notes AS Details, 
                        AmountIn, 
                        AmountOut
                    FROM CashBox
                    WHERE TransDate >= @dt AND TransType NOT IN ('Sale', 'SaleReturn')
                    ORDER BY TransTime DESC",
                    DbHelper.P("@sid", _shiftID), DbHelper.P("@dt", openTime));

                foreach (DataRow r in dtDetails.Rows)
                {
                    dgDrawerDetails.Rows.Add(
                        r["TransType"],
                        Convert.ToDateTime(r["TransTime"]).ToString("HH:mm:ss"),
                        r["RefCode"],
                        r["Details"],
                        Convert.ToDecimal(r["AmountIn"])  > 0 ? Convert.ToDecimal(r["AmountIn"]).ToString("N2")  : "0.00",
                        Convert.ToDecimal(r["AmountOut"]) > 0 ? Convert.ToDecimal(r["AmountOut"]).ToString("N2") : "0.00");
                }
            }
            catch (Exception ex) { AppLogger.Error("FrmShiftDrawerDetails.LoadDrawerData", ex); }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            MessageBox.Show("جاري إرسال حركة الدرج للطابعة...", "طباعة الحركة", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

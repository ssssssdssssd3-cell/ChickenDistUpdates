using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة تقرير إجمالي مبيعات كل صنف بالوردية في شاشة مستقلة
    /// </summary>
    public class FrmShiftProductSales : Form
    {
        private ComboBox cboShifts;
        private TextBox txtSearch;
        private Label lblShiftInfo;
        private Label lblTotalItemsVal, lblTotalSoldQtyVal, lblTotalReturnQtyVal, lblNetQtyVal, lblNetTotalVal;
        private DataGridView dgItems;
        private Button btnPrint, btnExport, btnRefresh, btnClose;
        private int? _targetShiftID;
        private DataTable _dtRaw;

        public FrmShiftProductSales(int? shiftID = null)
        {
            _targetShiftID = shiftID;
            InitUI();
            LoadShiftsList();
            if (_targetShiftID.HasValue && _targetShiftID.Value > 0)
            {
                SelectShiftInCombo(_targetShiftID.Value);
            }
            else
            {
                // إذا لم يُمرر رقم وردية، نحدد الوردية الحالية المفتوحة أو أول وردية بالقائمة
                try
                {
                    int? activeSid = ShiftDAL.GetActiveShiftID();
                    if (activeSid.HasValue && activeSid.Value > 0)
                    {
                        SelectShiftInCombo(activeSid.Value);
                    }
                }
                catch { }
            }
        }

        private void InitUI()
        {
            this.Text = "📦 تقرير إجمالي مبيعات أصناف الوردية";
            this.Size = new Size(1100, 750);
            this.MinimumSize = new Size(950, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // 1. رأس الشاشة
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "📦 تقرير إجمالي مبيعات كل صنف بالوردية",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 26
            };

            var lblSub = new Label
            {
                Text = "حصر وتجميع كميات ومبالغ المبيعات والمرتجعات وصافي كل صنف تم بيعه خلال الوردية المحددة",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // 2. شريط اختيار الوردية والبحث
            var pnlSelect = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8),
                Margin = new Padding(0, 0, 0, 6)
            };

            var flowFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = false
            };

            var lblSelTitle = new Label
            {
                Text = "🔍 اختر الوردية:",
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain,
                AutoSize = true,
                Margin = new Padding(4, 9, 2, 0)
            };

            cboShifts = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 360,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(4, 5, 12, 0)
            };
            cboShifts.SelectedIndexChanged += (s, e) => LoadShiftItemsData();

            var lblSearchTitle = new Label
            {
                Text = "🔎 بحث سريع بالصنف:",
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain,
                AutoSize = true,
                Margin = new Padding(12, 9, 2, 0)
            };

            txtSearch = new TextBox
            {
                Width = 220,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(4, 5, 8, 0)
            };
            txtSearch.TextChanged += (s, e) => FilterItemsGrid();

            btnRefresh = Theme.MakeButton("🔄 تحديث", Color.FromArgb(60, 70, 85), new Point(0, 0), new Size(95, 32));
            btnRefresh.Margin = new Padding(8, 3, 4, 0);
            btnRefresh.Click += (s, e) => LoadShiftItemsData();

            lblShiftInfo = new Label
            {
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Margin = new Padding(12, 9, 4, 0)
            };

            flowFilter.Controls.Add(lblSelTitle);
            flowFilter.Controls.Add(cboShifts);
            flowFilter.Controls.Add(lblSearchTitle);
            flowFilter.Controls.Add(txtSearch);
            flowFilter.Controls.Add(btnRefresh);
            flowFilter.Controls.Add(lblShiftInfo);

            pnlSelect.Controls.Add(flowFilter);

            // 3. المحتوى الرئيسي (جدول + كروت إحصائية)
            TableLayoutPanel tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12, 8, 12, 8),
                RightToLeft = RightToLeft.Yes
            };
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));  // كروت المؤشرات
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // جدول الأصناف

            // كروت المؤشرات KPI (5 كروت بارزة)
            Panel pnlKpis = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
            TableLayoutPanel tblKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            for (int i = 0; i < 5; i++) tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

            lblTotalItemsVal    = MakeKpiCard(tblKpis, "📦 عدد الأصناف المباعة", "0 صنف", Theme.TextMain, 0);
            lblTotalSoldQtyVal  = MakeKpiCard(tblKpis, "🔢 إجمالي كميات البيع", "0.00", Theme.Success, 1);
            lblTotalReturnQtyVal= MakeKpiCard(tblKpis, "↩️ إجمالي كميات المرتجع", "0.00", Theme.Danger, 2);
            lblNetQtyVal        = MakeKpiCard(tblKpis, "⚖️ صافي الكمية المباعة", "0.00", Color.FromArgb(52, 152, 219), 3);
            lblNetTotalVal      = MakeKpiCard(tblKpis, "💰 صافي قيمة المبيعات", "0.00 ج", Color.FromArgb(245, 158, 11), 4);

            pnlKpis.Controls.Add(tblKpis);
            tblMain.Controls.Add(pnlKpis, 0, 0);

            // DataGridView جدول مبيعات الأصناف
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5f),
                RowTemplate = { Height = 34 },
                EnableHeadersVisualStyles = false
            };
            dgItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgItems.ColumnHeadersHeight = 38;
            dgItems.DefaultCellStyle.BackColor = Theme.BgCard;
            dgItems.DefaultCellStyle.SelectionBackColor = Theme.Accent;
            dgItems.DefaultCellStyle.SelectionForeColor = Color.White;

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 55f });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 160f });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 45f });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldQty", HeaderText = "كمية البيع", FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.Success } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvgPrice", HeaderText = "متوسط السعر", FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldAmount", HeaderText = "إجمالي البيع (ج)", FillWeight = 65f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnQty", HeaderText = "المرتجع", FillWeight = 50f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Theme.Danger } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnAmount", HeaderText = "قيمة المرتجع (ج)", FillWeight = 60f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Theme.Danger } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetQty", HeaderText = "صافي الكمية", FillWeight = 55f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(52, 152, 219) } });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetAmount", HeaderText = "صافي الإجمالي (ج)", FillWeight = 75f, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(245, 158, 11) } });

            tblMain.Controls.Add(dgItems, 0, 1);

            // 4. الشريط السفلي
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnPrint  = Theme.MakeButton("🖨️ طباعة التقرير", Theme.Primary, new Point(0, 0), new Size(160, 40));
            btnExport = Theme.MakeButton("📥 تصدير Excel", Color.FromArgb(16, 185, 129), new Point(0, 0), new Size(140, 40));
            btnClose  = Theme.MakeButton("❌ إغلاق", Theme.Danger, new Point(0, 0), new Size(110, 40));

            btnPrint.Click  += BtnPrint_Click;
            btnExport.Click += BtnExport_Click;
            btnClose.Click  += (s, e) => this.Close();

            FlowLayoutPanel flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };
            btnPrint.Margin  = new Padding(6, 0, 0, 0);
            btnExport.Margin = new Padding(6, 0, 0, 0);
            btnClose.Margin  = new Padding(6, 0, 0, 0);

            flowBottom.Controls.Add(btnPrint);
            flowBottom.Controls.Add(btnExport);
            flowBottom.Controls.Add(btnClose);
            pnlBottom.Controls.Add(flowBottom);

            this.Controls.Add(pnlBottom);
            this.Controls.Add(tblMain);
            this.Controls.Add(pnlSelect);
            this.Controls.Add(pnlHeader);
            tblMain.BringToFront();
        }

        private Label MakeKpiCard(TableLayoutPanel parent, string title, string val, Color valColor, int colIdx)
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
                Height = 24,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblV = new Label
            {
                Text = val,
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.Add(lblV);
            pnl.Controls.Add(lblT);
            parent.Controls.Add(pnl, colIdx, 0);
            return lblV;
        }

        private void LoadShiftsList()
        {
            try
            {
                DataTable dt = DbHelper.Query(@"
                    SELECT TOP 60 
                        s.ShiftID, s.Status, s.OpenTime, s.CloseTime, e.EmpName
                    FROM Shifts s
                    LEFT JOIN Employees e ON s.OpenedBy = e.EmpID
                    ORDER BY s.ShiftID DESC");

                cboShifts.Items.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    int sid = Convert.ToInt32(r["ShiftID"]);
                    string status = r["Status"].ToString() == "Open" ? "🟢 مفتوحة" : "🔴 مغلقة";
                    string emp = r["EmpName"] != DBNull.Value ? r["EmpName"].ToString() : "كاشير";
                    DateTime dtOpen = Convert.ToDateTime(r["OpenTime"]);
                    string text = $"وردية #{sid} ({status}) - {emp} [{dtOpen:yyyy-MM-dd HH:mm}]";
                    cboShifts.Items.Add(new ComboItem(sid, text));
                }

                if (cboShifts.Items.Count > 0)
                    cboShifts.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmShiftProductSales.LoadShiftsList", ex);
            }
        }

        private void SelectShiftInCombo(int shiftID)
        {
            for (int i = 0; i < cboShifts.Items.Count; i++)
            {
                if (cboShifts.Items[i] is ComboItem item && item.ID == shiftID)
                {
                    cboShifts.SelectedIndex = i;
                    break;
                }
            }
        }

        private void LoadShiftItemsData()
        {
            if (!(cboShifts.SelectedItem is ComboItem selectedItem) || selectedItem.ID <= 0) return;
            int shiftID = selectedItem.ID;

            try
            {
                string sql = @"
                    WITH ShiftMovements AS (
                        -- 1. المبيعات
                        SELECT 
                            si.ProductID,
                            COALESCE(NULLIF(si.UnitName, N''), p.Unit, N'قطعة') AS UnitName,
                            si.Quantity AS SoldQty,
                            si.TotalPrice AS SoldAmount,
                            0.0 AS ReturnQty,
                            0.0 AS ReturnAmount
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        JOIN Products p ON si.ProductID = p.ProductID
                        WHERE s.ShiftID = @shiftID AND (COL_LENGTH('Sales', 'IsPosted') IS NULL OR ISNULL(s.IsPosted, 1) = 1)

                        UNION ALL

                        -- 2. المرتجعات
                        SELECT 
                            ri.ProductID,
                            COALESCE(NULLIF(ri.UnitName, N''), p.Unit, N'قطعة') AS UnitName,
                            0.0 AS SoldQty,
                            0.0 AS SoldAmount,
                            ri.Quantity AS ReturnQty,
                            ri.TotalPrice AS ReturnAmount
                        FROM ReturnItems ri
                        JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                        JOIN Products p ON ri.ProductID = p.ProductID
                        WHERE sr.ShiftID = @shiftID
                    )
                    SELECT 
                        sm.ProductID,
                        p.ProductCode,
                        p.ProductName,
                        sm.UnitName,
                        SUM(sm.SoldQty) AS SoldQty,
                        CASE WHEN SUM(sm.SoldQty) > 0 THEN ROUND(SUM(sm.SoldAmount) / SUM(sm.SoldQty), 2) ELSE 0 END AS AvgPrice,
                        SUM(sm.SoldAmount) AS SoldAmount,
                        SUM(sm.ReturnQty) AS ReturnQty,
                        SUM(sm.ReturnAmount) AS ReturnAmount,
                        SUM(sm.SoldQty) - SUM(sm.ReturnQty) AS NetQty,
                        SUM(sm.SoldAmount) - SUM(sm.ReturnAmount) AS NetAmount
                    FROM ShiftMovements sm
                    JOIN Products p ON sm.ProductID = p.ProductID
                    GROUP BY sm.ProductID, p.ProductCode, p.ProductName, sm.UnitName
                    ORDER BY NetAmount DESC, p.ProductName ASC";

                _dtRaw = DbHelper.Query(sql, DbHelper.P("@shiftID", shiftID));
                FilterItemsGrid();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmShiftProductSales.LoadShiftItemsData", ex);
                MessageBox.Show($"❌ حدث خطأ أثناء تحميل بيانات مبيعات أصناف الوردية:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterItemsGrid()
        {
            if (_dtRaw == null) return;
            dgItems.Rows.Clear();

            string search = txtSearch != null ? txtSearch.Text.Trim().ToLower() : "";

            decimal totSoldQty = 0;
            decimal totReturnQty = 0;
            decimal totNetQty = 0;
            decimal totNetAmount = 0;
            int distinctCount = 0;

            foreach (DataRow r in _dtRaw.Rows)
            {
                string code = r["ProductCode"] != DBNull.Value ? r["ProductCode"].ToString() : "";
                string name = r["ProductName"] != DBNull.Value ? r["ProductName"].ToString() : "";

                if (!string.IsNullOrEmpty(search))
                {
                    if (!code.ToLower().Contains(search) && !name.ToLower().Contains(search))
                        continue;
                }

                distinctCount++;
                decimal sQty = Convert.ToDecimal(r["SoldQty"]);
                decimal sAmt = Convert.ToDecimal(r["SoldAmount"]);
                decimal avgP = Convert.ToDecimal(r["AvgPrice"]);
                decimal rQty = Convert.ToDecimal(r["ReturnQty"]);
                decimal rAmt = Convert.ToDecimal(r["ReturnAmount"]);
                decimal nQty = Convert.ToDecimal(r["NetQty"]);
                decimal nAmt = Convert.ToDecimal(r["NetAmount"]);
                string unit  = r["UnitName"] != DBNull.Value ? r["UnitName"].ToString() : "";

                totSoldQty   += sQty;
                totReturnQty += rQty;
                totNetQty    += nQty;
                totNetAmount += nAmt;

                dgItems.Rows.Add(
                    code,
                    name,
                    unit,
                    sQty.ToString("N2"),
                    avgP.ToString("N2"),
                    sAmt.ToString("N2"),
                    rQty > 0 ? rQty.ToString("N2") : "-",
                    rAmt > 0 ? rAmt.ToString("N2") : "-",
                    nQty.ToString("N2"),
                    nAmt.ToString("N2")
                );
            }

            lblTotalItemsVal.Text     = $"{distinctCount} صنف";
            lblTotalSoldQtyVal.Text   = totSoldQty.ToString("N2");
            lblTotalReturnQtyVal.Text = totReturnQty.ToString("N2");
            lblNetQtyVal.Text         = totNetQty.ToString("N2");
            lblNetTotalVal.Text       = totNetAmount.ToString("N2") + " ج";
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (dgItems.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = $"مبيعات_أصناف_الوردية_{DateTime.Now:yyyyMMdd_HHmm}.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("\uFEFFكود الصنف,اسم الصنف,الوحدة,كمية البيع,متوسط السعر,إجمالي البيع,كمية المرتجع,قيمة المرتجع,صافي الكمية,صافي الإجمالي");

                        foreach (DataGridViewRow row in dgItems.Rows)
                        {
                            if (row.IsNewRow) continue;
                            string c0 = EscapeCsv(row.Cells["ProductCode"].Value?.ToString());
                            string c1 = EscapeCsv(row.Cells["ProductName"].Value?.ToString());
                            string c2 = EscapeCsv(row.Cells["UnitName"].Value?.ToString());
                            string c3 = EscapeCsv(row.Cells["SoldQty"].Value?.ToString());
                            string c4 = EscapeCsv(row.Cells["AvgPrice"].Value?.ToString());
                            string c5 = EscapeCsv(row.Cells["SoldAmount"].Value?.ToString());
                            string c6 = EscapeCsv(row.Cells["ReturnQty"].Value?.ToString());
                            string c7 = EscapeCsv(row.Cells["ReturnAmount"].Value?.ToString());
                            string c8 = EscapeCsv(row.Cells["NetQty"].Value?.ToString());
                            string c9 = EscapeCsv(row.Cells["NetAmount"].Value?.ToString());

                            sb.AppendLine($"{c0},{c1},{c2},{c3},{c4},{c5},{c6},{c7},{c8},{c9}");
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("✅ تم تصدير التقرير إلى Excel بنجاح.", "تم التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ فشل التصدير: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private static string EscapeCsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return "\"\"";
            return "\"" + val.Replace("\"", "\"\"") + "\"";
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (dgItems.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لطباعتها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                PrintDocument pd = new PrintDocument();
                pd.DocumentName = "تقرير مبيعات أصناف الوردية";
                pd.DefaultPageSettings.Margins = new Margins(30, 30, 40, 40);

                int printRowIdx = 0;
                int pageNumber = 1;

                pd.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    float y = ev.MarginBounds.Top;
                    float left = ev.MarginBounds.Left;
                    float right = ev.MarginBounds.Right;
                    float width = ev.MarginBounds.Width;

                    Font fontTitle = new Font("Segoe UI", 14f, FontStyle.Bold);
                    Font fontSub = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                    Font fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    Font fontRow = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                    Font fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);

                    StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                    StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

                    // Header on first page
                    if (pageNumber == 1)
                    {
                        string companyName = AppConfig.CompanyName ?? "نظام البرمجيات المتكامل";
                        g.DrawString(companyName, fontTitle, Brushes.Black, new RectangleF(left, y, width, 25), sfCenter);
                        y += 26;

                        string shiftTitle = cboShifts.SelectedItem != null ? cboShifts.SelectedItem.ToString() : "تقرير الوردية";
                        g.DrawString($"📦 تقرير إجمالي مبيعات أصناف الوردية: {shiftTitle}", fontHeader, Brushes.DarkBlue, new RectangleF(left, y, width, 22), sfCenter);
                        y += 24;

                        g.DrawString($"تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm} | الأصناف: {lblTotalItemsVal.Text} | صافي القيمة: {lblNetTotalVal.Text}", fontSub, Brushes.Gray, new RectangleF(left, y, width, 18), sfCenter);
                        y += 26;

                        g.DrawLine(Pens.DarkGray, left, y, right, y);
                        y += 6;
                    }

                    // Table Columns Widths (Total 100%)
                    float colCode = width * 0.10f;
                    float colName = width * 0.30f;
                    float colUnit = width * 0.08f;
                    float colSoldQty = width * 0.10f;
                    float colPrice = width * 0.10f;
                    float colRetQty = width * 0.09f;
                    float colNetQty = width * 0.10f;
                    float colNetAmt = width * 0.13f;

                    // Table Header
                    g.FillRectangle(Brushes.LightGray, left, y, width, 24);
                    g.DrawRectangle(Pens.Gray, left, y, width, 24);

                    float x = right;
                    x -= colCode; g.DrawString("الكود", fontHeader, Brushes.Black, new RectangleF(x, y, colCode, 24), sfCenter);
                    x -= colName; g.DrawString("اسم الصنف", fontHeader, Brushes.Black, new RectangleF(x, y, colName, 24), sfRight);
                    x -= colUnit; g.DrawString("الوحدة", fontHeader, Brushes.Black, new RectangleF(x, y, colUnit, 24), sfCenter);
                    x -= colSoldQty; g.DrawString("كمية البيع", fontHeader, Brushes.Black, new RectangleF(x, y, colSoldQty, 24), sfCenter);
                    x -= colPrice; g.DrawString("السعر", fontHeader, Brushes.Black, new RectangleF(x, y, colPrice, 24), sfCenter);
                    x -= colRetQty; g.DrawString("المرتجع", fontHeader, Brushes.Black, new RectangleF(x, y, colRetQty, 24), sfCenter);
                    x -= colNetQty; g.DrawString("الصافي", fontHeader, Brushes.Black, new RectangleF(x, y, colNetQty, 24), sfCenter);
                    x -= colNetAmt; g.DrawString("صافي الإجمالي", fontHeader, Brushes.Black, new RectangleF(x, y, colNetAmt, 24), sfCenter);
                    y += 24;

                    // Rows
                    while (printRowIdx < dgItems.Rows.Count)
                    {
                        if (y + 22 > ev.MarginBounds.Bottom)
                        {
                            ev.HasMorePages = true;
                            pageNumber++;
                            return;
                        }

                        var row = dgItems.Rows[printRowIdx];
                        float rowH = 22;

                        if (printRowIdx % 2 == 1)
                        {
                            g.FillRectangle(new SolidBrush(Color.FromArgb(248, 249, 250)), left, y, width, rowH);
                        }
                        g.DrawRectangle(Pens.LightGray, left, y, width, rowH);

                        x = right;
                        x -= colCode; g.DrawString(row.Cells["ProductCode"].Value?.ToString(), fontRow, Brushes.Black, new RectangleF(x, y, colCode, rowH), sfCenter);
                        x -= colName; g.DrawString(row.Cells["ProductName"].Value?.ToString(), fontRow, Brushes.Black, new RectangleF(x + 2, y, colName - 4, rowH), sfRight);
                        x -= colUnit; g.DrawString(row.Cells["UnitName"].Value?.ToString(), fontRow, Brushes.Black, new RectangleF(x, y, colUnit, rowH), sfCenter);
                        x -= colSoldQty; g.DrawString(row.Cells["SoldQty"].Value?.ToString(), fontRow, Brushes.Black, new RectangleF(x, y, colSoldQty, rowH), sfCenter);
                        x -= colPrice; g.DrawString(row.Cells["AvgPrice"].Value?.ToString(), fontRow, Brushes.Black, new RectangleF(x, y, colPrice, rowH), sfCenter);
                        x -= colRetQty; g.DrawString(row.Cells["ReturnQty"].Value?.ToString(), fontRow, Brushes.Black, new RectangleF(x, y, colRetQty, rowH), sfCenter);
                        x -= colNetQty; g.DrawString(row.Cells["NetQty"].Value?.ToString(), fontBold, Brushes.DarkBlue, new RectangleF(x, y, colNetQty, rowH), sfCenter);
                        x -= colNetAmt; g.DrawString(row.Cells["NetAmount"].Value?.ToString(), fontBold, Brushes.Black, new RectangleF(x, y, colNetAmt, rowH), sfCenter);

                        y += rowH;
                        printRowIdx++;
                    }

                    // Totals Footer on last page
                    if (y + 35 < ev.MarginBounds.Bottom)
                    {
                        y += 6;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(235, 245, 255)), left, y, width, 26);
                        g.DrawRectangle(Pens.SteelBlue, left, y, width, 26);

                        string summaryText = $"الإجماليات: عدد الأصناف = {lblTotalItemsVal.Text} | إجمالي البيع = {lblTotalSoldQtyVal.Text} | المرتجع = {lblTotalReturnQtyVal.Text} | صافي المبيعات = {lblNetTotalVal.Text}";
                        g.DrawString(summaryText, fontBold, Brushes.DarkBlue, new RectangleF(left, y, width, 26), sfCenter);
                    }

                    ev.HasMorePages = false;
                };

                using (var ppd = new PrintPreviewDialog())
                {
                    ppd.Document = pd;
                    ppd.WindowState = FormWindowState.Maximized;
                    ppd.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ أثناء الطباعة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

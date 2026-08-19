using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSalesAuditList : Form
    {
        // ─── شريط الفلتر ───
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboActionType;
        private TextBox txtSearch;
        private Button btnLoad;

        // ─── جريد السجل الرئيسي ───
        private DataGridView dgAudit;

        // ─── لوحة المقارنة السفلية ───
        private Panel pnlCompare;
        private Label lblCompareTitle;
        private DataGridView dgOld, dgNew;
        private Label lblOldTitle, lblNewTitle;
        private Label lblSummary;

        public FrmSalesAuditList()
        {
            InitUI();
            LoadAuditLogs();
        }

        // ══════════════════════════════════════════════
        //  بناء الواجهة
        // ══════════════════════════════════════════════
        private void InitUI()
        {
            Text = "سجل تعديلات وعمليات الفواتير";
            Size = new Size(1200, 760);
            MinimumSize = new Size(900, 580);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // ─── شريط الفلتر ───
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 12, 8, 8),
                WrapContents = false
            };

            dtpFrom = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0), Margin = new Padding(4, 0, 4, 0) };
            dtpFrom.ValueChanged += (s, e) => LoadAuditLogs();

            dtpTo = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = DateTime.Now, Margin = new Padding(4, 0, 4, 0) };
            dtpTo.ValueChanged += (s, e) => LoadAuditLogs();

            cboActionType = new ComboBox { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(4, 0, 4, 0) };
            cboActionType.Items.AddRange(new string[] { "الكل", "CREATE", "EDIT", "DELETE" });
            cboActionType.SelectedIndex = 0;

            txtSearch = new TextBox { Width = 140, Margin = new Padding(4, 0, 4, 0) };

            btnLoad = MkBtn("🔍 عرض السجل", Theme.Accent, 120, 30);
            btnLoad.Click += (s, e) => LoadAuditLogs();

            var btnPrint = MkBtn("🖨️ طباعة", Color.FromArgb(40, 100, 180), 95, 30);
            btnPrint.Click += BtnPrint_Click;

            var btnExportExcel = MkBtn("📥 إكسيل", Color.FromArgb(0, 120, 80), 95, 30);
            btnExportExcel.Click += BtnExportExcel_Click;

            var btnExportPdf = MkBtn("📄 PDF", Color.FromArgb(200, 40, 40), 85, 30);
            btnExportPdf.Click += BtnExportPdf_Click;

            pnlFilter.Controls.Clear();
            pnlFilter.Controls.AddRange(new Control[] {
                btnLoad, btnPrint, btnExportExcel, btnExportPdf, txtSearch,
                new Label { Text = "بحث كود/ملاحظات:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(4,4,0,0) },
                cboActionType,
                new Label { Text = "العملية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(12,4,0,0) },
                dtpTo,
                new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(12,4,0,0) },
                dtpFrom,
                new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(4,4,0,0) }
            });

            // ─── لوحة المقارنة السفلية (مخفية حتى يُضغط على سجل) ───
            pnlCompare = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 300,
                BackColor = Theme.BgMain,
                Visible = false
            };

            // عنوان اللوحة
            lblCompareTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(0, 0, 10, 0)
            };

            // ملخص التغييرات
            lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = Theme.FontMain,
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(0, 0, 10, 0)
            };

            // جريد الأصناف - تخطيط جانبي
            var tblGrids = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(4)
            };
            tblGrids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblGrids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // جريد "قبل التعديل"
            var pnlOld = new Panel { Dock = DockStyle.Fill };
            lblOldTitle = new Label
            {
                Text = "📋 الأصناف قبل التعديل",
                Dock = DockStyle.Top, Height = 26,
                Font = Theme.FontBold, ForeColor = Color.OrangeRed,
                BackColor = Color.FromArgb(50, 30, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0)
            };
            dgOld = BuildItemsGrid();
            pnlOld.Controls.Add(dgOld);
            pnlOld.Controls.Add(lblOldTitle);

            // جريد "بعد التعديل"
            var pnlNew = new Panel { Dock = DockStyle.Fill };
            lblNewTitle = new Label
            {
                Text = "✅ الأصناف بعد التعديل",
                Dock = DockStyle.Top, Height = 26,
                Font = Theme.FontBold, ForeColor = Color.LightGreen,
                BackColor = Color.FromArgb(20, 50, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0)
            };
            dgNew = BuildItemsGrid();
            pnlNew.Controls.Add(dgNew);
            pnlNew.Controls.Add(lblNewTitle);

            tblGrids.Controls.Add(pnlNew, 0, 0); // يسار الشاشة = بعد التعديل
            tblGrids.Controls.Add(pnlOld, 1, 0); // يمين الشاشة = قبل التعديل

            pnlCompare.Controls.Add(tblGrids);
            pnlCompare.Controls.Add(lblSummary);
            pnlCompare.Controls.Add(lblCompareTitle);

            // ─── الجريد الرئيسي ───
            dgAudit = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
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
                    Font = Theme.FontMain,
                    Padding = new Padding(2)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = Theme.FontBold
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 28 }
            };

            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "AuditID",    Visible = false });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleID",     HeaderText = "مسلسل الفاتورة", FillWeight = 40 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode",   HeaderText = "كود الفاتورة",   FillWeight = 55 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActionType", HeaderText = "العملية",        FillWeight = 38 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "EditDate",   HeaderText = "تاريخ العملية",  FillWeight = 75 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserName",   HeaderText = "المستخدم",       FillWeight = 60 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "MachineName",HeaderText = "الجهاز",         FillWeight = 60 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "OldTotal",   HeaderText = "الإجمالي السابق",FillWeight = 48 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewTotal",   HeaderText = "الإجمالي الجديد",FillWeight = 48 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",      HeaderText = "ملاحظات وتفاصيل التعديل", FillWeight = 110 });

            // الضغط على صف = تحميل المقارنة تلقائياً
            dgAudit.SelectionChanged += DgAudit_SelectionChanged;
            dgAudit.RowPrePaint += (s, e) => e.PaintParts &= ~DataGridViewPaintParts.Focus;

            // تجميع الشاشة
            this.Controls.Add(dgAudit);
            this.Controls.Add(pnlCompare);
            this.Controls.Add(pnlFilter);
        }

        // ══════════════════════════════════════════════
        //  تحميل سجل التعديلات
        // ══════════════════════════════════════════════
        private void LoadAuditLogs()
        {
            try
            {
                string actionFilter = cboActionType.SelectedItem?.ToString() ?? "الكل";
                string searchVal    = txtSearch.Text.Trim();

                string sql = @"
                    SELECT a.AuditID, a.SaleID,
                           COALESCE(s.SaleCode, N'(مسلسل: ' + CAST(a.SaleID AS NVARCHAR) + N')') AS SaleCode,
                           a.EditDate, a.OldTotal, a.NewTotal, a.Notes, a.MachineName, a.ActionType,
                           e.EmpName AS UserName
                    FROM SalesAudit a
                    LEFT JOIN Sales s ON a.SaleID = s.SaleID
                    LEFT JOIN Employees e ON a.UserID = e.EmpID
                    WHERE CAST(a.EditDate AS DATE) BETWEEN @from AND @to";

                if (actionFilter != "الكل") sql += " AND a.ActionType = @act";
                if (!string.IsNullOrEmpty(searchVal))
                    sql += " AND (s.SaleCode LIKE @q OR a.Notes LIKE @q OR a.MachineName LIKE @q OR e.EmpName LIKE @q)";

                sql += " ORDER BY a.EditDate DESC";

                DateTime f = dtpFrom.Value;
                DateTime t = dtpTo.Value;
                if (t.TimeOfDay == TimeSpan.Zero) t = t.Date.AddDays(1).AddTicks(-1);

                var dt = DbHelper.Query(sql,
                    DbHelper.P("@from", f),
                    DbHelper.P("@to",   t),
                    DbHelper.P("@act",  actionFilter),
                    DbHelper.P("@q",    "%" + searchVal + "%"));

                dgAudit.Rows.Clear();
                pnlCompare.Visible = false;

                foreach (DataRow row in dt.Rows)
                {
                    string act = row["ActionType"].ToString();
                    decimal oldTot = row["OldTotal"] != DBNull.Value ? Convert.ToDecimal(row["OldTotal"]) : 0m;
                    decimal newTot = row["NewTotal"] != DBNull.Value ? Convert.ToDecimal(row["NewTotal"]) : 0m;

                    int rIdx = dgAudit.Rows.Add(
                        row["AuditID"],
                        row["SaleID"],
                        row["SaleCode"],
                        act,
                        Convert.ToDateTime(row["EditDate"]).ToString("yyyy-MM-dd hh:mm tt"),
                        row["UserName"],
                        row["MachineName"],
                        oldTot.ToString("N2") + " ج",
                        newTot.ToString("N2") + " ج",
                        row["Notes"]);

                    // ألوان العمليات
                    var cellAct = dgAudit.Rows[rIdx].Cells["ActionType"];
                    if      (act == "CREATE") { cellAct.Style.ForeColor = Color.LightGreen;  cellAct.Value = "✅ CREATE"; }
                    else if (act == "DELETE") { cellAct.Style.ForeColor = Color.Tomato;       cellAct.Value = "🗑 DELETE"; }
                    else if (act == "EDIT")   { cellAct.Style.ForeColor = Color.Orange;       cellAct.Value = "✏ EDIT"; }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل سجل التعديلات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════
        //  عند اختيار سجل → تحميل المقارنة تلقائياً
        // ══════════════════════════════════════════════
        private void DgAudit_SelectionChanged(object sender, EventArgs e)
        {
            if (dgAudit.SelectedRows.Count == 0) { pnlCompare.Visible = false; return; }

            var row = dgAudit.SelectedRows[0];
            if (row.Cells["AuditID"].Value == null) { pnlCompare.Visible = false; return; }

            int    auditID    = Convert.ToInt32(row.Cells["AuditID"].Value);
            int    saleID     = Convert.ToInt32(row.Cells["SaleID"].Value);
            string saleCode   = row.Cells["SaleCode"].Value?.ToString() ?? "";
            string actionType = row.Cells["ActionType"].Value?.ToString()
                                    .Replace("✅ ", "").Replace("🗑 ", "").Replace("✏ ", "") ?? "";

            try
            {
                LoadComparison(auditID, saleID, saleCode, actionType);
                pnlCompare.Visible = true;
            }
            catch (Exception ex)
            {
                pnlCompare.Visible = false;
                MessageBox.Show("خطأ في تحميل التفاصيل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════
        //  تحميل بيانات المقارنة
        // ══════════════════════════════════════════════
        private void LoadComparison(int auditID, int saleID, string saleCode, string actionType)
        {
            dgOld.Rows.Clear();
            dgNew.Rows.Clear();

            lblCompareTitle.Text = $"  📊 مقارنة بنود الفاتورة: {saleCode}  |  العملية: {actionType}";

            // ─── 1. جلب الأصناف القديمة (المؤرشفة) ───
            var dtOld = DbHelper.Query(
                @"SELECT p.ProductName, h.ProductID, h.Quantity, h.UnitPrice, h.TotalPrice, h.PriceTier
                  FROM SaleItemsHistory h
                  JOIN Products p ON h.ProductID = p.ProductID
                  WHERE h.AuditID = @aid
                  ORDER BY p.ProductName",
                DbHelper.P("@aid", auditID));

            // ─── 2. جلب الأصناف الجديدة (الحالية) ───
            DataTable dtNew = null;
            if (actionType != "DELETE")
            {
                dtNew = DbHelper.Query(
                    @"SELECT p.ProductName, si.ProductID, si.Quantity, si.UnitPrice, si.TotalPrice,
                             COALESCE(si.PriceTier, N'قطاعي') AS PriceTier
                      FROM SaleItems si
                      JOIN Products p ON si.ProductID = p.ProductID
                      WHERE si.SaleID = @sid
                      ORDER BY p.ProductName",
                    DbHelper.P("@sid", saleID));
            }

            // ─── 3. بناء قاموس للمقارنة ───
            var oldMap = BuildMap(dtOld);
            var newMap = dtNew != null ? BuildMap(dtNew) : new Dictionary<int, DataRow>();

            // ─── 4. ملء جريد "قبل التعديل" مع تلوين ───
            foreach (DataRow r in dtOld.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                decimal oldQty   = Convert.ToDecimal(r["Quantity"]);
                decimal oldPrice = Convert.ToDecimal(r["UnitPrice"]);
                decimal oldTotal = Convert.ToDecimal(r["TotalPrice"]);

                RowStatus status = RowStatus.Unchanged;
                if (!newMap.ContainsKey(pid))
                    status = RowStatus.Removed;   // موجود في القديم ومحذوف من الجديد
                else
                {
                    var nr = newMap[pid];
                    if (Convert.ToDecimal(nr["Quantity"]) != oldQty ||
                        Convert.ToDecimal(nr["UnitPrice"]) != oldPrice)
                        status = RowStatus.Modified;
                }

                int ri = dgOld.Rows.Add(
                    r["ProductName"],
                    oldQty.ToString("N2"),
                    oldPrice.ToString("N2") + " ج",
                    oldTotal.ToString("N2") + " ج",
                    r["PriceTier"]);

                ApplyRowColor(dgOld.Rows[ri], status);
            }

            // ─── 5. ملء جريد "بعد التعديل" مع تلوين ───
            if (dtNew != null && dtNew.Rows.Count > 0)
            {
                foreach (DataRow r in dtNew.Rows)
                {
                    int pid = Convert.ToInt32(r["ProductID"]);
                    decimal newQty   = Convert.ToDecimal(r["Quantity"]);
                    decimal newPrice = Convert.ToDecimal(r["UnitPrice"]);
                    decimal newTotal = Convert.ToDecimal(r["TotalPrice"]);

                    RowStatus status = RowStatus.Unchanged;
                    if (!oldMap.ContainsKey(pid))
                        status = RowStatus.Added;    // صنف جديد أُضيف بعد التعديل
                    else
                    {
                        var or = oldMap[pid];
                        if (Convert.ToDecimal(or["Quantity"]) != newQty ||
                            Convert.ToDecimal(or["UnitPrice"]) != newPrice)
                            status = RowStatus.Modified;
                    }

                    // عرض الفرق في الكمية
                    string qtyDisplay = newQty.ToString("N2");
                    if (status == RowStatus.Modified && oldMap.ContainsKey(pid))
                    {
                        decimal oldQty = Convert.ToDecimal(oldMap[pid]["Quantity"]);
                        decimal diff = newQty - oldQty;
                        qtyDisplay += diff >= 0 ? $"  (▲{diff:N2})" : $"  (▼{Math.Abs(diff):N2})";
                    }

                    int ri = dgNew.Rows.Add(
                        r["ProductName"],
                        qtyDisplay,
                        newPrice.ToString("N2") + " ج",
                        newTotal.ToString("N2") + " ج",
                        r["PriceTier"]);

                    ApplyRowColor(dgNew.Rows[ri], status);
                }
            }
            else if (actionType == "DELETE")
            {
                lblNewTitle.Text = "🗑 لا توجد أصناف حالية (تم حذف الفاتورة)";
                lblNewTitle.ForeColor = Color.Tomato;
            }

            // ─── 6. ملخص عدد التغييرات ───
            int added    = CountStatus(dgNew, Color.FromArgb(20, 80, 20));
            int removed  = CountStatus(dgOld, Color.FromArgb(80, 20, 20));
            int modified = CountStatus(dgNew, Color.FromArgb(80, 60, 10));
            lblSummary.Text = $"  ملخص: {dgOld.Rows.Count} صنف قبل | {(dtNew?.Rows.Count ?? 0)} صنف بعد" +
                              $"   |   🟢 مُضاف: {added}   |   🔴 محذوف: {removed}   |   🟡 معدّل: {modified}";
        }

        // ══════════════════════════════════════════════
        //  دوال مساعدة
        // ══════════════════════════════════════════════
        private enum RowStatus { Unchanged, Added, Removed, Modified }

        private void ApplyRowColor(DataGridViewRow row, RowStatus status)
        {
            switch (status)
            {
                case RowStatus.Added:
                    row.DefaultCellStyle.BackColor = Color.FromArgb(20, 80, 20);
                    row.DefaultCellStyle.ForeColor = Color.LightGreen;
                    break;
                case RowStatus.Removed:
                    row.DefaultCellStyle.BackColor = Color.FromArgb(80, 20, 20);
                    row.DefaultCellStyle.ForeColor = Color.Tomato;
                    break;
                case RowStatus.Modified:
                    row.DefaultCellStyle.BackColor = Color.FromArgb(80, 60, 10);
                    row.DefaultCellStyle.ForeColor = Color.Gold;
                    break;
                default:
                    row.DefaultCellStyle.BackColor = Theme.BgCard;
                    row.DefaultCellStyle.ForeColor = Theme.TextMain;
                    break;
            }
        }

        private Dictionary<int, DataRow> BuildMap(DataTable dt)
        {
            var map = new Dictionary<int, DataRow>();
            if (dt == null) return map;
            foreach (DataRow r in dt.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                if (!map.ContainsKey(pid)) map[pid] = r;
            }
            return map;
        }

        private int CountStatus(DataGridView dg, Color backColor)
        {
            int count = 0;
            foreach (DataGridViewRow r in dg.Rows)
                if (r.DefaultCellStyle.BackColor == backColor) count++;
            return count;
        }

        private DataGridView BuildItemsGrid()
        {
            var dg = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary, ForeColor = Color.White, Font = Theme.FontBold
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowTemplate = { Height = 26 }
            };
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف",      FillWeight = 120 });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",    HeaderText = "الكمية",     FillWeight = 60  });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",   HeaderText = "سعر الوحدة", FillWeight = 55  });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice",  HeaderText = "الإجمالي",   FillWeight = 55  });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "PriceTier",   HeaderText = "الفئة",      FillWeight = 40  });
            return dg;
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (dgAudit.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات متاحة للتصدير في السجل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Title = "تصدير سجل التعديلات والعمليات إلى Excel",
                Filter = "Excel Files (*.xls)|*.xls",
                FileName = $"سجل_تعديلات_الفواتير_{DateTime.Now:yyyyMMdd_HHmm}.xls"
            })
            {
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        FrmReports.ExportDataGridViewToXls(dgAudit, sfd.FileName, "سجل تعديلات وعمليات الفواتير", AppConfig.CompanyName);
                        var res = MessageBox.Show("✅ تم تصدير ملف Excel بنجاح!\n\nهل ترغب في فتح الملف الآن؟", "نجاح التصدير", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(sfd.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل تصدير ملف Excel:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnExportPdf_Click(object sender, EventArgs e)
        {
            if (dgAudit.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات متاحة للتصدير في السجل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Title = "تصدير سجل التعديلات والعمليات إلى PDF",
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"سجل_تعديلات_الفواتير_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            })
            {
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var pages = GenerateAuditPagesBitmaps();
                        PdfReportHelper.SaveBitmapsAsPdf(pages, sfd.FileName);
                        var res = MessageBox.Show("✅ تم تصدير ملف PDF بنجاح!\n\nهل ترغب في فتح الملف الآن؟", "نجاح التصدير", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(sfd.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل تصدير ملف PDF:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (dgAudit.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات متاحة للطباعة في السجل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pd = new System.Drawing.Printing.PrintDocument();
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);

                int rowIndex = 0;
                int pageNumber = 0;

                pd.PrintPage += (s, pe) =>
                {
                    pageNumber++;
                    RenderAuditPage(pe.Graphics, pe.MarginBounds, ref rowIndex, pageNumber, false);
                    pe.HasMorePages = (rowIndex < dgAudit.Rows.Count);
                };

                using (var dlg = new PrintPreviewDialog { Document = pd, Width = 950, Height = 700, StartPosition = FormStartPosition.CenterScreen })
                {
                    ((Form)dlg).Text = "معاينة طباعة سجل تعديلات وعمليات الفواتير";
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل إعداد طباعة السجل:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private System.Collections.Generic.List<Bitmap> GenerateAuditPagesBitmaps()
        {
            var list = new System.Collections.Generic.List<Bitmap>();
            int pageW = 1240;
            int pageH = 1754;
            int rowIndex = 0;
            int pageNum = 0;

            while (rowIndex < dgAudit.Rows.Count)
            {
                pageNum++;
                Bitmap bmp = new Bitmap(pageW, pageH);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    Rectangle bounds = new Rectangle(60, 60, pageW - 120, pageH - 120);
                    RenderAuditPage(g, bounds, ref rowIndex, pageNum, true);
                }
                list.Add(bmp);
            }
            return list;
        }

        private void RenderAuditPage(Graphics g, Rectangle bounds, ref int rowIndex, int pageNum, bool isPdf)
        {
            float scale = isPdf ? 1.4f : 1.0f;
            using (Font fHeader = new Font("Segoe UI", 16f * scale, FontStyle.Bold))
            using (Font fSub = new Font("Segoe UI", 10f * scale, FontStyle.Regular))
            using (Font fCol = new Font("Segoe UI", 9.5f * scale, FontStyle.Bold))
            using (Font fCell = new Font("Segoe UI", 9f * scale, FontStyle.Regular))
            using (Font fFoot = new Font("Segoe UI", 8.5f * scale, FontStyle.Italic))
            using (StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            using (StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                float curY = bounds.Top;

                // Header Title Banner
                g.FillRectangle(new SolidBrush(Theme.Primary), bounds.Left, curY, bounds.Width, 45 * scale);
                g.DrawString("سجل تعديلات وعمليات الفواتير", fHeader, Brushes.White, new RectangleF(bounds.Left, curY, bounds.Width, 45 * scale), sfCenter);
                curY += 50 * scale;

                // Period Info Subtitle
                string periodStr = $"الفترة من: {dtpFrom.Value:yyyy/MM/dd}  إلى: {dtpTo.Value:yyyy/MM/dd} | نوع العملية: {cboActionType.SelectedItem}";
                g.DrawString(periodStr, fSub, Brushes.DarkSlateGray, new RectangleF(bounds.Left, curY, bounds.Width, 24 * scale), sfRight);
                curY += 30 * scale;

                // Columns Definition (RTL Layout)
                float tableW = bounds.Width;
                float[] colWidths = new float[] { tableW * 0.12f, tableW * 0.18f, tableW * 0.11f, tableW * 0.15f, tableW * 0.12f, tableW * 0.12f, tableW * 0.20f };
                string[] colHeaders = new string[] { "رقم الفاتورة", "التاريخ والوقت", "العملية", "المستخدم", "الإجمالي القديم", "الإجمالي الجديد", "الملاحظات/السبب" };

                // Draw Table Header Row
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 243, 246)), bounds.Left, curY, tableW, 28 * scale);
                g.DrawRectangle(Pens.Gray, bounds.Left, curY, tableW, 28 * scale);

                float curX = bounds.Right;
                for (int c = 0; c < colHeaders.Length; c++)
                {
                    curX -= colWidths[c];
                    g.DrawString(colHeaders[c], fCol, Brushes.Black, new RectangleF(curX, curY, colWidths[c], 28 * scale), sfCenter);
                    g.DrawLine(Pens.LightGray, curX, curY, curX, curY + 28 * scale);
                }
                curY += 28 * scale;

                // Draw Rows
                float rowH = 26 * scale;
                int totalRows = dgAudit.Rows.Count;

                while (rowIndex < totalRows && (curY + rowH) < (bounds.Bottom - 40 * scale))
                {
                    var r = dgAudit.Rows[rowIndex];

                    string code = r.Cells["SaleCode"].Value?.ToString() ?? "";
                    string date = r.Cells["EditDate"].Value?.ToString() ?? "";
                    string act  = r.Cells["ActionType"].Value?.ToString()?.Replace("✅ ", "")?.Replace("🗑 ", "")?.Replace("✏ ", "") ?? "";
                    string user = r.Cells["UserName"].Value?.ToString() ?? "";
                    string oldT = r.Cells["OldTotal"].Value?.ToString() ?? "";
                    string newT = r.Cells["NewTotal"].Value?.ToString() ?? "";
                    string note = r.Cells["Notes"].Value?.ToString() ?? "";

                    string[] vals = new string[] { code, date, act, user, oldT, newT, note };

                    if (rowIndex % 2 == 1)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), bounds.Left, curY, tableW, rowH);
                    }
                    g.DrawRectangle(Pens.LightGray, bounds.Left, curY, tableW, rowH);

                    curX = bounds.Right;
                    for (int c = 0; c < vals.Length; c++)
                    {
                        curX -= colWidths[c];
                        g.DrawString(vals[c], fCell, Brushes.Black, new RectangleF(curX + 2, curY, colWidths[c] - 4, rowH), sfCenter);
                        g.DrawLine(Pens.LightGray, curX, curY, curX, curY + rowH);
                    }

                    curY += rowH;
                    rowIndex++;
                }

                // Footer Page Info
                float footY = bounds.Bottom - 24 * scale;
                g.DrawLine(Pens.Gray, bounds.Left, footY, bounds.Right, footY);
                g.DrawString($"صفحة {pageNum}", fFoot, Brushes.Gray, new RectangleF(bounds.Left, footY + 4, bounds.Width / 2, 20 * scale), sfRight);
                g.DrawString($"تاريخ الاستخراج: {DateTime.Now:yyyy-MM-dd HH:mm}", fFoot, Brushes.Gray, new RectangleF(bounds.Left + bounds.Width / 2, footY + 4, bounds.Width / 2, 20 * scale), sfCenter);
            }
        }

        private Button MkBtn(string text, Color back, int w, int h)
        {
            var btn = new Button
            {
                Text = text, Size = new Size(w, h),
                BackColor = back, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = Theme.FontBold,
                Cursor = Cursors.Hand, Margin = new Padding(4, 0, 4, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}

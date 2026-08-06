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

            Lbl("من:"); dtpFrom = Dtp(-30);
            Lbl("إلى:"); dtpTo   = Dtp(0);

            Lbl("العملية:");
            cboActionType = new ComboBox { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(4, 0, 4, 0) };
            cboActionType.Items.AddRange(new string[] { "الكل", "CREATE", "EDIT", "DELETE" });
            cboActionType.SelectedIndex = 0;

            Lbl("بحث كود/ملاحظات:");
            txtSearch = new TextBox { Width = 140, Margin = new Padding(4, 0, 4, 0) };

            btnLoad = MkBtn("🔍 عرض السجل", Theme.Accent, 130, 30);
            btnLoad.Click += (s, e) => LoadAuditLogs();

            void Lbl(string t) => pnlFilter.Controls.Add(new Label
                { Text = t, AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(4, 4, 0, 0) });
            DateTimePicker Dtp(int addDays)
            {
                var d = new DateTimePicker { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd   hh:mm tt", Value = addDays == 0 ? DateTime.Now : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0), Margin = new Padding(4, 0, 4, 0) };
                d.ValueChanged += (s, e) => LoadAuditLogs();
                pnlFilter.Controls.Add(d);
                return d;
            }

            pnlFilter.Controls.AddRange(new Control[] { cboActionType, txtSearch, btnLoad });
            pnlFilter.Controls.Add(cboActionType);
            // تصحيح الترتيب
            pnlFilter.Controls.Clear();
            pnlFilter.Controls.AddRange(new Control[] {
                btnLoad, txtSearch,
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

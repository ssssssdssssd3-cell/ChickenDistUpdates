using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSalesAuditList : Form
    {
        private DataGridView dgAudit;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private ComboBox cboActionType;
        private TextBox txtSearch;
        private Button btnLoad;
        private Button btnShowDetails;

        public FrmSalesAuditList()
        {
            InitUI();
            LoadAuditLogs();
        }

        private void InitUI()
        {
            Text = "سجل تعديلات وعمليات الفواتير";
            base.Size = new Size(1150, 700);
            base.StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // ===== Filter Panel =====
            FlowLayoutPanel pnlFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 15, 10, 10),
                WrapContents = false
            };

            Label lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 5, 0, 0) };
            dtpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7) };
            
            Label lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            dtpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            Label lblAction = new Label { Text = "العملية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            cboActionType = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cboActionType.Items.AddRange(new string[] { "الكل", "CREATE", "EDIT", "DELETE" });
            cboActionType.SelectedIndex = 0;

            Label lblSearch = new Label { Text = "بحث كود/ملاحظات:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 5, 0, 0) };
            txtSearch = new TextBox { Width = 130 };

            btnLoad = Theme.MakeButton("🔍 عرض السجل", 0, 0, 120, 28, Theme.Accent);
            btnLoad.Click += (s, e) => LoadAuditLogs();

            btnShowDetails = Theme.MakeButton("🔍 تفاصيل البنود", 0, 0, 130, 28, Theme.Primary);
            btnShowDetails.Click += (s, e) => ShowAuditDetails();

            pnlFilters.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblAction, cboActionType, lblSearch, txtSearch, btnLoad, btnShowDetails });

            // ===== Grid =====
            dgAudit = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
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
                    Font = Theme.FontBold
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "AuditID", HeaderText = "رقم الحركة", Visible = false });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleID", HeaderText = "مسلسل الفاتورة", FillWeight = 40 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", HeaderText = "كود الفاتورة", FillWeight = 50 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActionType", HeaderText = "العملية", FillWeight = 40 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "EditDate", HeaderText = "تاريخ العملية", FillWeight = 70 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserName", HeaderText = "المستخدم", FillWeight = 60 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "MachineName", HeaderText = "الجهاز", FillWeight = 60 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "OldTotal", HeaderText = "الإجمالي السابق", FillWeight = 45 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewTotal", HeaderText = "الإجمالي الجديد", FillWeight = 45 });
            dgAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات وتفاصيل التعديل", FillWeight = 100 });

            dgAudit.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ShowAuditDetails(); };

            this.Controls.Add(dgAudit);
            this.Controls.Add(pnlFilters);
        }

        private void LoadAuditLogs()
        {
            try
            {
                string actionFilter = cboActionType.SelectedItem.ToString();
                string searchVal = txtSearch.Text.Trim();

                string sql = @"
                    SELECT a.AuditID, a.SaleID, COALESCE(s.SaleCode, N'محذوفة (مسلسل: ' + CAST(a.SaleID AS NVARCHAR) + N')') AS SaleCode,
                           a.EditDate, a.OldTotal, a.NewTotal, a.Notes, a.MachineName, a.ActionType,
                           e.EmpName AS UserName
                    FROM SalesAudit a
                    LEFT JOIN Sales s ON a.SaleID = s.SaleID
                    LEFT JOIN Employees e ON a.UserID = e.EmpID
                    WHERE CAST(a.EditDate AS DATE) BETWEEN @from AND @to";

                if (actionFilter != "الكل")
                {
                    sql += " AND a.ActionType = @act";
                }

                if (!string.IsNullOrEmpty(searchVal))
                {
                    sql += " AND (s.SaleCode LIKE @q OR a.Notes LIKE @q OR a.MachineName LIKE @q OR e.EmpName LIKE @q)";
                }

                sql += " ORDER BY a.EditDate DESC";

                var dt = DbHelper.Query(sql,
                    DbHelper.P("@from", dtpFrom.Value.Date),
                    DbHelper.P("@to", dtpTo.Value.Date),
                    DbHelper.P("@act", actionFilter),
                    DbHelper.P("@q", "%" + searchVal + "%"));

                dgAudit.Rows.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    int rIdx = dgAudit.Rows.Add(
                        row["AuditID"],
                        row["SaleID"],
                        row["SaleCode"],
                        row["ActionType"],
                        Convert.ToDateTime(row["EditDate"]).ToString("yyyy-MM-dd hh:mm tt"),
                        row["UserName"],
                        row["MachineName"],
                        Convert.ToDecimal(row["OldTotal"]).ToString("N2") + " ج",
                        Convert.ToDecimal(row["NewTotal"]).ToString("N2") + " ج",
                        row["Notes"]
                    );

                    // تلوين العمليات لتمييزها بسهولة
                    string act = row["ActionType"].ToString();
                    if (act == "CREATE") dgAudit.Rows[rIdx].Cells["ActionType"].Style.ForeColor = Color.ForestGreen;
                    else if (act == "DELETE") dgAudit.Rows[rIdx].Cells["ActionType"].Style.ForeColor = Color.Crimson;
                    else if (act == "EDIT") dgAudit.Rows[rIdx].Cells["ActionType"].Style.ForeColor = Color.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل سجل التعديلات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAuditDetails()
        {
            if (dgAudit.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار حركة مبيعات من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgAudit.SelectedRows[0];
            int auditID = Convert.ToInt32(row.Cells["AuditID"].Value);
            int saleID = Convert.ToInt32(row.Cells["SaleID"].Value);
            string saleCode = row.Cells["SaleCode"].Value.ToString();
            string actionType = row.Cells["ActionType"].Value.ToString();

            using (var dlg = new Form())
            {
                dlg.Text = $" تفاصيل بنود الحركة للفاتورة: {saleCode} | العملية: {actionType}";
                dlg.Size = new Size(1000, 500);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain;
                dlg.Font = Theme.FontMain;

                TableLayoutPanel mainTbl = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(10)
                };
                mainTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                mainTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

                // ─── الطرف الأيمن: البنود المؤرشفة في هذه الحركة ───
                Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
                Label lblLeftTitle = new Label
                {
                    Text = actionType == "CREATE" ? " لا توجد بنود سابقة (فاتورة جديدة)" : "📋 بنود الفاتورة قبل التعديل/الحذف (الأرشيف)",
                    Font = Theme.FontBold,
                    ForeColor = actionType == "CREATE" ? Color.Gray : Theme.Accent,
                    Dock = DockStyle.Top,
                    Height = 25
                };
                DataGridView dgLeft = CreateItemsGrid();
                pnlLeft.Controls.Add(dgLeft);
                pnlLeft.Controls.Add(lblLeftTitle);

                // ─── الطرف الأيسر: البنود الحالية في قاعدة البيانات ───
                Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
                Label lblRightTitle = new Label
                {
                    Text = actionType == "DELETE" ? " لا توجد بنود حالية (تم حذف الفاتورة)" : "📋 بنود الفاتورة الحالية في النظام",
                    Font = Theme.FontBold,
                    ForeColor = actionType == "DELETE" ? Color.Crimson : Color.ForestGreen,
                    Dock = DockStyle.Top,
                    Height = 25
                };
                DataGridView dgRight = CreateItemsGrid();
                pnlRight.Controls.Add(dgRight);
                pnlRight.Controls.Add(lblRightTitle);

                mainTbl.Controls.Add(pnlLeft, 0, 0);
                mainTbl.Controls.Add(pnlRight, 1, 0);
                dlg.Controls.Add(mainTbl);

                // تحميل البيانات
                // 1. تحميل البنود المؤرشفة
                if (actionType != "CREATE")
                {
                    var dtArchived = DbHelper.Query(
                        @"SELECT p.ProductName, h.Quantity, h.UnitPrice, h.TotalPrice, h.PriceTier 
                          FROM SaleItemsHistory h 
                          JOIN Products p ON h.ProductID = p.ProductID 
                          WHERE h.AuditID = @aid",
                        DbHelper.P("@aid", auditID));
                    
                    foreach (DataRow r in dtArchived.Rows)
                    {
                        dgLeft.Rows.Add(
                            r["ProductName"],
                            Convert.ToDecimal(r["Quantity"]).ToString("N2"),
                            Convert.ToDecimal(r["UnitPrice"]).ToString("N2") + " ج",
                            Convert.ToDecimal(r["TotalPrice"]).ToString("N2") + " ج",
                            r["PriceTier"].ToString()
                        );
                    }
                }

                // 2. تحميل البنود الحالية (إذا كانت الفاتورة لم تُحذف)
                if (actionType != "DELETE")
                {
                    var dtCurrent = DbHelper.Query(
                        @"SELECT p.ProductName, si.Quantity, si.UnitPrice, si.TotalPrice, si.PriceTier 
                          FROM SaleItems si 
                          JOIN Products p ON si.ProductID = p.ProductID 
                          WHERE si.SaleID = @sid",
                        DbHelper.P("@sid", saleID));

                    if (dtCurrent.Rows.Count > 0)
                    {
                        foreach (DataRow r in dtCurrent.Rows)
                        {
                            dgRight.Rows.Add(
                                r["ProductName"],
                                Convert.ToDecimal(r["Quantity"]).ToString("N2"),
                                Convert.ToDecimal(r["UnitPrice"]).ToString("N2") + " ج",
                                Convert.ToDecimal(r["TotalPrice"]).ToString("N2") + " ج",
                                r["PriceTier"].ToString()
                            );
                        }
                    }
                    else
                    {
                        lblRightTitle.Text = "⚠️ لا توجد بنود حالية (تم حذف أو تعديل الفاتورة لاحقاً)";
                        lblRightTitle.ForeColor = Color.Orange;
                    }
                }

                dlg.ShowDialog(this);
            }
        }

        private DataGridView CreateItemsGrid()
        {
            var dg = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = Theme.FontBold },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", FillWeight = 100 });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 40 });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر الوحدة", FillWeight = 50 });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "الإجمالي", FillWeight = 50 });
            dg.Columns.Add(new DataGridViewTextBoxColumn { Name = "PriceTier", HeaderText = "فئة السعر", FillWeight = 50 });
            return dg;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPriceChangeHistory : Form
    {
        private DateTimePicker dtpFrom, dtpTo;
        private TextBox txtSearch;
        private ComboBox cboChangeType;
        private Button btnFilter, btnExportExcel, btnClose;
        private DataGridView dgvHistory;
        private Label lblCount, lblEmptyNotice;

        public FrmPriceChangeHistory(int? initialProductId = null)
        {
            InitUI();
            if (initialProductId.HasValue && initialProductId.Value > 0)
            {
                var codeObj = DbHelper.Scalar("SELECT ProductCode FROM Products WHERE ProductID=@id", DbHelper.P("@id", initialProductId.Value));
                if (codeObj != null) txtSearch.Text = codeObj.ToString();
            }
            LoadHistory();
        }

        private void InitUI()
        {
            this.Text = "سجل وتقرير تغيرات وتعديلات الأسعار";
            this.Size = new Size(1150, 680);
            this.MinimumSize = new Size(950, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── Top Filter Panel ──
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 15, 10, 10)
            };

            int x = 15;
            var lblFrom = new Label { Text = "من تاريخ:", Location = new Point(x, 22), AutoSize = true, ForeColor = Theme.TextMain };
            x += 65;
            dtpFrom = new DateTimePicker { Location = new Point(x, 19), Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            x += 125;

            var lblTo = new Label { Text = "إلى تاريخ:", Location = new Point(x, 22), AutoSize = true, ForeColor = Theme.TextMain };
            x += 65;
            dtpTo = new DateTimePicker { Location = new Point(x, 19), Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1) };
            x += 125;

            var lblType = new Label { Text = "النوع:", Location = new Point(x, 22), AutoSize = true, ForeColor = Theme.TextMain };
            x += 45;
            cboChangeType = new ComboBox { Location = new Point(x, 19), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            cboChangeType.Items.AddRange(new string[] { "-- الكل --", "تعديل جماعي", "تعديل من كارت الصنف", "فاتورة شراء", "تسوية أسعار" });
            cboChangeType.SelectedIndex = 0;
            x += 140;

            var lblSrch = new Label { Text = "بحث:", Location = new Point(x, 22), AutoSize = true, ForeColor = Theme.TextMain };
            x += 40;
            txtSearch = new TextBox { Location = new Point(x, 19), Width = 150, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadHistory(); };
            x += 160;

            btnFilter = Theme.MakeButton("بحث", x, 16, 95, 32, Theme.Accent);
            btnFilter.Click += (s, e) => LoadHistory();
            x += 105;

            btnExportExcel = Theme.MakeButton("تصدير إكسيل", x, 16, 120, 32, Color.FromArgb(16, 130, 80));
            btnExportExcel.Click += BtnExportExcel_Click;

            pnlTop.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblType, cboChangeType, lblSrch, txtSearch, btnFilter, btnExportExcel });
            this.Controls.Add(pnlTop);

            // ── Grid ──
            dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 }
            };

            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font(Theme.FontMain, FontStyle.Bold);
            dgvHistory.ColumnHeadersHeight = 36;
            dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChangeID", HeaderText = "#", FillWeight = 30 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChangeDate", HeaderText = "التاريخ والوقت", FillWeight = 75 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 55 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 130 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "OldPurchasePrice", HeaderText = "شراء سابق", FillWeight = 50 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewPurchasePrice", HeaderText = "شراء جديد", FillWeight = 50 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "OldSalePrice", HeaderText = "بيع سابق", FillWeight = 50 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewSalePrice", HeaderText = "بيع جديد", FillWeight = 50 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffSale", HeaderText = "فرق البيع", FillWeight = 45 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChangeType", HeaderText = "نوع الإجراء", FillWeight = 65 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "المورد", FillWeight = 65 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserName", HeaderText = "المستخدم", FillWeight = 55 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 90 });

            lblEmptyNotice = new Label
            {
                Text = "لا توجد تعديلات أسعار مسجلة خلال الفترة المحددة.\nيمكنك توسيع نطاق التاريخ أو استخدام زر [تغيير أسعار البيع والتكلفة] لتطبيق أسعار جديدة.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextSub,
                Font = new Font(Theme.FontMain.FontFamily, 12f, FontStyle.Bold),
                BackColor = Theme.BgCard,
                Visible = false
            };

            this.Controls.Add(lblEmptyNotice);
            this.Controls.Add(dgvHistory);

            // ── Bottom Panel ──
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            lblCount = new Label
            {
                Text = "عدد العمليات: 0",
                Dock = DockStyle.Right,
                AutoSize = false,
                Width = 250,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };

            btnClose = Theme.MakeButton("إغلاق", 15, 9, 100, 32, Color.FromArgb(100, 110, 120));
            btnClose.Click += (s, e) => this.Close();

            pnlBottom.Controls.AddRange(new Control[] { lblCount, btnClose });
            this.Controls.Add(pnlBottom);

            dgvHistory.BringToFront();

            Theme.ApplyFormRTL(this);
            this.RightToLeftLayout = false;
        }

        private void LoadHistory()
        {
            try
            {
                DbHelper.EnsurePriceMarginAndHistorySchema();
                string sql = @"
                    SELECT ChangeID, ChangeDate, ProductID, ProductCode, ProductName,
                           OldPurchasePrice, NewPurchasePrice, OldSalePrice, NewSalePrice,
                           (NewSalePrice - OldSalePrice) AS DiffSale,
                           ChangeType, SupplierName, UserName, Notes
                    FROM PriceChangeHistory WITH (NOLOCK)
                    WHERE ChangeDate >= @from AND ChangeDate <= @to";

                var pars = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>
                {
                    DbHelper.P("@from", dtpFrom.Value.Date),
                    DbHelper.P("@to", dtpTo.Value.Date.AddDays(1).AddSeconds(-1))
                };

                if (cboChangeType.SelectedIndex > 0)
                {
                    sql += " AND ChangeType LIKE @type";
                    pars.Add(DbHelper.P("@type", "%" + cboChangeType.Text.Trim() + "%"));
                }

                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    sql += " AND (ProductCode LIKE @srch OR ProductName LIKE @srch OR UserName LIKE @srch)";
                    pars.Add(DbHelper.P("@srch", "%" + txtSearch.Text.Trim() + "%"));
                }

                sql += " ORDER BY ChangeID DESC";

                var dt = DbHelper.Query(sql, pars.ToArray());
                dgvHistory.Rows.Clear();

                foreach (DataRow r in dt.Rows)
                {
                    decimal oldSale = Convert.ToDecimal(r["OldSalePrice"] == DBNull.Value ? 0 : r["OldSalePrice"]);
                    decimal newSale = Convert.ToDecimal(r["NewSalePrice"] == DBNull.Value ? 0 : r["NewSalePrice"]);
                    decimal diff = newSale - oldSale;

                    int rowIdx = dgvHistory.Rows.Add(
                        r["ChangeID"],
                        Convert.ToDateTime(r["ChangeDate"]).ToString("yyyy-MM-dd HH:mm"),
                        r["ProductCode"],
                        r["ProductName"],
                        Convert.ToDecimal(r["OldPurchasePrice"] == DBNull.Value ? 0 : r["OldPurchasePrice"]).ToString("N2"),
                        Convert.ToDecimal(r["NewPurchasePrice"] == DBNull.Value ? 0 : r["NewPurchasePrice"]).ToString("N2"),
                        oldSale.ToString("N2"),
                        newSale.ToString("N2"),
                        (diff >= 0 ? "+" : "") + diff.ToString("N2"),
                        r["ChangeType"],
                        r["SupplierName"],
                        r["UserName"],
                        r["Notes"]
                    );

                    var diffCell = dgvHistory.Rows[rowIdx].Cells["DiffSale"];
                    if (diff > 0)
                    {
                        diffCell.Style.ForeColor = Color.FromArgb(22, 101, 52);
                        diffCell.Style.Font = new Font(Theme.FontMain, FontStyle.Bold);
                    }
                    else if (diff < 0)
                    {
                        diffCell.Style.ForeColor = Color.FromArgb(185, 28, 28);
                        diffCell.Style.Font = new Font(Theme.FontMain, FontStyle.Bold);
                    }
                }

                lblCount.Text = $"عدد العمليات المسجلة: {dt.Rows.Count}";
                if (lblEmptyNotice != null)
                {
                    lblEmptyNotice.Visible = (dt.Rows.Count == 0);
                    dgvHistory.Visible = (dt.Rows.Count > 0);
                    if (lblEmptyNotice.Visible) lblEmptyNotice.BringToFront();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadHistory in FrmPriceChangeHistory", ex);
                MessageBox.Show("حدث خطأ أثناء تحميل سجل الأسعار: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvHistory == null || dgvHistory.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "ملف CSV (*.csv)|*.csv|كل الملفات (*.*)|*.*";
                    sfd.FileName = $"سجل_تغيرات_الاسعار_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var sb = new System.Text.StringBuilder();
                        var headers = new List<string>();
                        foreach (DataGridViewColumn col in dgvHistory.Columns)
                        {
                            if (col.Visible) headers.Add("\"" + col.HeaderText.Replace("\"", "\"\"") + "\"");
                        }
                        sb.AppendLine(string.Join(",", headers));

                        foreach (DataGridViewRow row in dgvHistory.Rows)
                        {
                            if (row.IsNewRow) continue;
                            var cells = new List<string>();
                            foreach (DataGridViewColumn col in dgvHistory.Columns)
                            {
                                if (col.Visible)
                                {
                                    string val = row.Cells[col.Index].Value?.ToString() ?? "";
                                    cells.Add("\"" + val.Replace("\"", "\"\"") + "\"");
                                }
                            }
                            sb.AppendLine(string.Join(",", cells));
                        }

                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        MessageBox.Show("تم تصدير السجل بنجاح!\nالمسار: " + sfd.FileName, "نجاح التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        try { System.Diagnostics.Process.Start(sfd.FileName); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل التصدير: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

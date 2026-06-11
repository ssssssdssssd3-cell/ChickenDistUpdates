using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmPriceChanges : Form
    {
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboSource;
        private TextBox txtSearch;
        private Button btnSearch, btnReset;
        private DataGridView dgLogs;

        public FrmPriceChanges()
        {
            InitUI();
            LoadLogs();
        }

        private void InitUI()
        {
            this.Text = "📊 سجل حركات وتعديلات الأسعار";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Main Layout
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            int x = 15;
            pnlTop.Controls.Add(new Label
            {
                Text = "من تاريخ:",
                Location = new Point(x, 15),
                Width = 70,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleLeft
            });
            dtpFrom = new DateTimePicker
            {
                Location = new Point(x + 75, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30)
            };
            pnlTop.Controls.Add(dtpFrom);

            pnlTop.Controls.Add(new Label
            {
                Text = "إلى تاريخ:",
                Location = new Point(x + 210, 15),
                Width = 70,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleLeft
            });
            dtpTo = new DateTimePicker
            {
                Location = new Point(x + 285, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            pnlTop.Controls.Add(dtpTo);

            pnlTop.Controls.Add(new Label
            {
                Text = "المصدر:",
                Location = new Point(x + 420, 15),
                Width = 60,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleLeft
            });
            cboSource = new ComboBox
            {
                Location = new Point(x + 485, 12),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboSource.Items.AddRange(new object[] { "الكل", "كارت الصنف", "فاتورة شراء", "فاتورة بيع" });
            cboSource.SelectedIndex = 0;
            pnlTop.Controls.Add(cboSource);

            pnlTop.Controls.Add(new Label
            {
                Text = "بحث بالمنتج:",
                Location = new Point(x, 48),
                Width = 85,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleLeft
            });
            txtSearch = new TextBox
            {
                Location = new Point(x + 90, 46),
                Width = 315,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadLogs(); };
            pnlTop.Controls.Add(txtSearch);

            btnSearch = Theme.MakeButton("🔍 بحث وفلترة", x + 420, 44, 115, 30, Theme.Accent);
            btnReset = Theme.MakeButton("🔄 إعادة تعيين", x + 545, 44, 110, 30, Color.FromArgb(70, 70, 70));

            btnSearch.Click += (s, e) => LoadLogs();
            btnReset.Click += (s, e) =>
            {
                dtpFrom.Value = DateTime.Today.AddDays(-30);
                dtpTo.Value = DateTime.Today;
                cboSource.SelectedIndex = 0;
                txtSearch.Clear();
                LoadLogs();
            };

            pnlTop.Controls.AddRange(new Control[] { btnSearch, btnReset });

            // Grid Layout
            dgLogs = new DataGridView
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
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChangeDate", HeaderText = "تاريخ التعديل", FillWeight = 90 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 80 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 160 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 50 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "المصدر", FillWeight = 80 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefCode", HeaderText = "المرجع", FillWeight = 70 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "OldPrice", HeaderText = "السعر القديم", FillWeight = 70 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewPrice", HeaderText = "السعر الجديد", FillWeight = 70 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diff", HeaderText = "الفارق", FillWeight = 65 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "User", HeaderText = "المستخدم", FillWeight = 85 });
            dgLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات وتفاصيل", FillWeight = 180 });

            this.Controls.Add(dgLogs);
            this.Controls.Add(pnlTop);

            Theme.ApplyFormRTL(this);
        }

        private void LoadLogs()
        {
            dgLogs.Rows.Clear();

            string sourceFilter = "All";
            if (cboSource.SelectedIndex == 1) sourceFilter = "ProductCard";
            else if (cboSource.SelectedIndex == 2) sourceFilter = "PurchaseInvoice";
            else if (cboSource.SelectedIndex == 3) sourceFilter = "SalesInvoice";

            string searchPattern = txtSearch.Text.Trim();

            string sql = @"
                SELECT 
                    pcl.ChangeDate,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    pcl.ChangeSource,
                    pcl.SourceRefID,
                    pcl.OldPrice,
                    pcl.NewPrice,
                    (pcl.NewPrice - pcl.OldPrice) AS PriceDiff,
                    COALESCE(e.EmpName, N'---') AS EmpName,
                    pcl.Notes,
                    CASE pcl.ChangeSource
                        WHEN 'PurchaseInvoice' THEN (SELECT TOP 1 PurchaseCode FROM Purchases WHERE PurchaseID = pcl.SourceRefID)
                        WHEN 'SalesInvoice' THEN (SELECT TOP 1 SaleCode FROM Sales WHERE SaleID = pcl.SourceRefID)
                        ELSE NULL
                    END AS RefCode
                FROM PriceChangesLog pcl
                JOIN Products p ON pcl.ProductID = p.ProductID
                LEFT JOIN Employees e ON pcl.UserID = e.EmpID
                WHERE CAST(pcl.ChangeDate AS DATE) BETWEEN @from AND @to
                  AND (@source = 'All' OR pcl.ChangeSource = @source)
                  AND (@term = '' OR p.ProductName LIKE @term OR p.ProductCode LIKE @term)
                ORDER BY pcl.ChangeDate DESC";

            var dt = DbHelper.Query(sql,
                DbHelper.P("@from", dtpFrom.Value.Date),
                DbHelper.P("@to", dtpTo.Value.Date),
                DbHelper.P("@source", sourceFilter),
                DbHelper.P("@term", string.IsNullOrEmpty(searchPattern) ? "" : "%" + searchPattern + "%"));

            foreach (DataRow r in dt.Rows)
            {
                string srcArabic = "";
                string src = r["ChangeSource"].ToString();
                if (src == "ProductCard") srcArabic = "كارت الصنف";
                else if (src == "PurchaseInvoice") srcArabic = "فاتورة شراء";
                else if (src == "SalesInvoice") srcArabic = "فاتورة بيع";
                else srcArabic = src;

                string refCode = "";
                if (r["RefCode"] != DBNull.Value)
                {
                    refCode = r["RefCode"].ToString();
                }
                else if (r["SourceRefID"] != DBNull.Value)
                {
                    refCode = "#" + r["SourceRefID"].ToString();
                }
                else
                {
                    refCode = "---";
                }

                decimal oldPrice = Convert.ToDecimal(r["OldPrice"]);
                decimal newPrice = Convert.ToDecimal(r["NewPrice"]);
                decimal diff = Convert.ToDecimal(r["PriceDiff"]);

                int ri = dgLogs.Rows.Add(
                    Convert.ToDateTime(r["ChangeDate"]).ToString("yyyy-MM-dd HH:mm"),
                    r["ProductCode"]?.ToString() ?? "---",
                    r["ProductName"]?.ToString() ?? "---",
                    r["Unit"]?.ToString() ?? "---",
                    srcArabic,
                    refCode,
                    oldPrice.ToString("N2") + " ج",
                    newPrice.ToString("N2") + " ج",
                    (diff > 0 ? "+" : "") + diff.ToString("N2") + " ج",
                    r["EmpName"]?.ToString() ?? "---",
                    r["Notes"]?.ToString() ?? "---"
                );

                // Highlight differences
                var cellDiff = dgLogs.Rows[ri].Cells["Diff"];
                if (diff > 0)
                {
                    cellDiff.Style.ForeColor = Color.FromArgb(100, 220, 100);
                }
                else if (diff < 0)
                {
                    cellDiff.Style.ForeColor = Color.FromArgb(255, 100, 100);
                }
            }
        }
    }
}

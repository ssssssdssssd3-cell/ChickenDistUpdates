using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>قائمة عروض وبيانات الأسعار المعلقة مع خاصية البحث والتحكم الكامل</summary>
    public class FrmPriceQuotesList : Form
    {
        private DataGridView dgQuotes;
        private TextBox txtSearch;
        private Label lblCount;
        private Button btnRecall, btnConvertToSale, btnDelete, btnRefresh, btnClose;
        private DataTable _dtQuotes;

        public int SelectedQuoteID { get; private set; } = 0;
        public string ActionType { get; private set; } = ""; // "Edit" or "Convert"

        public FrmPriceQuotesList()
        {
            InitUI();
            LoadData();
        }

        private void InitUI()
        {
            Text = "📋 قائمة عروض وبيانات الأسعار المعلقة";
            Size = new Size(950, 560);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // ── Main Layout (TableLayoutPanel) ─────────────────────
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));  // Row 0: Search Panel
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Row 1: DataGridView
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));  // Row 2: Bottom Actions

            // ── Panel Top (Search & Info) ──────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 8)
            };
            Theme.StyleSearchHeaderPanel(pnlTop);

            var lblSearch = new Label
            {
                Text = "🔍 بحث:",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextSearchLabel,
                Margin = new Padding(0, 6, 5, 0)
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Left,
                Width = 260,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark
            };
            txtSearch.TextChanged += (s, e) => FilterData();

            lblCount = new Label
            {
                Text = "عدد العروض المعلقة: 0",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.Accent,
                Margin = new Padding(0, 6, 0, 0)
            };

            pnlTop.Controls.Add(txtSearch);
            pnlTop.Controls.Add(lblSearch);
            pnlTop.Controls.Add(lblCount);

            // ── DataGridView ────────────────────────────────────────
            dgQuotes = new DataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersVisible = true,
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    Font = new Font("Segoe UI", 9.5f),
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White
                }
            };

            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuoteID", HeaderText = "#", Visible = false });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuoteCode", HeaderText = "رقم العرض", FillWeight = 30 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuoteDate", HeaderText = "التاريخ والوقت", FillWeight = 45 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "DisplayClient", HeaderText = "اسم العميل", FillWeight = 65 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemCount", HeaderText = "عدد الأصناف", FillWeight = 25 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "إجمالي المبلغ", FillWeight = 40 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "PriceTier", HeaderText = "فئة السعر", FillWeight = 30 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "مُسجِّل العرض", FillWeight = 40 });

            // Apply system grid header style (Dark Slate Blue header background with White bold text)
            Theme.StyleGridHeader(dgQuotes);

            // ── Panel Bottom (Actions) ──────────────────────────────
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            btnRecall = Theme.MakeButton("📥 استرجاع للتعديل (Enter)", 10, 10, 200, 36, Theme.Accent);
            btnRecall.Click += (s, e) => DoRecall();

            btnConvertToSale = Theme.MakeButton("🔄 تحويل لفاتورة بيع", 220, 10, 180, 36, Theme.Success);
            btnConvertToSale.Click += (s, e) => DoConvertToSale();

            btnDelete = Theme.MakeButton("🗑️ حذف العرض", 410, 10, 130, 36, Color.Brown);
            btnDelete.Click += (s, e) => DoDelete();

            btnRefresh = Theme.MakeButton("🔄 تحديث", 550, 10, 100, 36, Color.Gray);
            btnRefresh.Click += (s, e) => LoadData();

            btnClose = Theme.MakeButton("إغلاق", 660, 10, 100, 36, Color.DarkSlateGray);
            btnClose.Click += (s, e) => this.Close();

            pnlBottom.Controls.AddRange(new Control[] { btnRecall, btnConvertToSale, btnDelete, btnRefresh, btnClose });

            // Add controls into TableLayoutPanel rows
            mainLayout.Controls.Add(pnlTop, 0, 0);
            mainLayout.Controls.Add(dgQuotes, 0, 1);
            mainLayout.Controls.Add(pnlBottom, 0, 2);

            Controls.Add(mainLayout);

            dgQuotes.DoubleClick += (s, e) => DoRecall();
            dgQuotes.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { DoRecall(); e.Handled = true; }
            };

            Theme.ApplyFormRTL(this);
        }

        private void LoadData()
        {
            try
            {
                _dtQuotes = PriceQuoteDAL.GetPendingQuotes();
                FilterData();
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadData in FrmPriceQuotesList failed", ex, "FrmPriceQuotesList");
            }
        }

        private void FilterData()
        {
            if (_dtQuotes == null) return;

            dgQuotes.Rows.Clear();
            string q = txtSearch?.Text?.Trim() ?? "";

            int count = 0;
            foreach (DataRow r in _dtQuotes.Rows)
            {
                string code   = r["QuoteCode"]?.ToString() ?? "";
                string client = r["DisplayClient"]?.ToString() ?? "";
                string tier   = r["PriceTier"]?.ToString() ?? "";
                string user   = r["CreatedByName"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(q))
                {
                    bool match = code.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 client.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 tier.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 user.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) continue;
                }

                dgQuotes.Rows.Add(
                    r["QuoteID"],
                    code,
                    Convert.ToDateTime(r["QuoteDate"]).ToString("yyyy/MM/dd HH:mm"),
                    client,
                    r["ItemCount"],
                    Convert.ToDecimal(r["TotalAmount"]).ToString("N2") + " ج",
                    tier,
                    user
                );
                count++;
            }

            lblCount.Text = $"عدد العروض المعلقة: {count}";
        }

        private int GetSelectedID()
        {
            if (dgQuotes.CurrentRow != null && dgQuotes.CurrentRow.Cells["QuoteID"].Value != null)
            {
                return Convert.ToInt32(dgQuotes.CurrentRow.Cells["QuoteID"].Value);
            }
            return 0;
        }

        private void DoRecall()
        {
            int id = GetSelectedID();
            if (id <= 0)
            {
                MessageBox.Show("اختر عرض أسعار أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SelectedQuoteID = id;
            ActionType = "Edit";
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DoConvertToSale()
        {
            int id = GetSelectedID();
            if (id <= 0)
            {
                MessageBox.Show("اختر عرض أسعار أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SelectedQuoteID = id;
            ActionType = "Convert";
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DoDelete()
        {
            int id = GetSelectedID();
            if (id <= 0) return;

            if (MessageBox.Show("هل أنت تأكد من حذف عرض الأسعار المحدد؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                PriceQuoteDAL.DeleteQuote(id);
                LoadData();
            }
        }
    }
}

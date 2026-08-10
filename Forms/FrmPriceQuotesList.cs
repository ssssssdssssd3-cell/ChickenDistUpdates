using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>قائمة عروض وبيانات الأسعار المعلقة</summary>
    public class FrmPriceQuotesList : Form
    {
        private DataGridView dgQuotes;
        private Button btnRecall, btnConvertToSale, btnDelete, btnRefresh, btnClose;
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
            Size = new Size(880, 520);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Theme.BgCard,
                Padding = new Padding(8)
            };

            var lblTitle = new Label
            {
                Text = "اختر عرض أسعار لاسترجاعه وتعديله أو تحويله مباشرة لفاتورة بيع:",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };
            pnlTop.Controls.Add(lblTitle);
            Controls.Add(pnlTop);

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
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    Font = Theme.FontMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false
            };

            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuoteID", HeaderText = "#", Visible = false });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuoteCode", HeaderText = "رقم العرض", FillWeight = 30 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuoteDate", HeaderText = "التاريخ والوقت", FillWeight = 50 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "DisplayClient", HeaderText = "العميل", FillWeight = 60 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemCount", HeaderText = "عدد الأصناف", FillWeight = 30 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "إجمالي المبلغ", FillWeight = 40 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "PriceTier", HeaderText = "فئة السعر", FillWeight = 30 });
            dgQuotes.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "مُسجِّل العرض", FillWeight = 40 });

            Controls.Add(dgQuotes);

            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            btnRecall = Theme.MakeButton("📥 استرجاع للتعديل (Enter)", 10, 10, 190, 35, Theme.Accent);
            btnRecall.Click += (s, e) => DoRecall();

            btnConvertToSale = Theme.MakeButton("🔄 تحويل لفاتورة بيع", 210, 10, 170, 35, Theme.Success);
            btnConvertToSale.Click += (s, e) => DoConvertToSale();

            btnDelete = Theme.MakeButton("🗑️ حذف العرض", 390, 10, 130, 35, Color.Brown);
            btnDelete.Click += (s, e) => DoDelete();

            btnRefresh = Theme.MakeButton("🔄 تحديث", 530, 10, 100, 35, Color.Gray);
            btnRefresh.Click += (s, e) => LoadData();

            btnClose = Theme.MakeButton("إغلاق", 640, 10, 90, 35, Color.DarkSlateGray);
            btnClose.Click += (s, e) => this.Close();

            pnlBottom.Controls.AddRange(new Control[] { btnRecall, btnConvertToSale, btnDelete, btnRefresh, btnClose });
            Controls.Add(pnlBottom);

            dgQuotes.DoubleClick += (s, e) => DoRecall();
            dgQuotes.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { DoRecall(); e.Handled = true; }
            };
        }

        private void LoadData()
        {
            DataTable dt = PriceQuoteDAL.GetPendingQuotes();
            dgQuotes.Rows.Clear();
            foreach (DataRow r in dt.Rows)
            {
                dgQuotes.Rows.Add(
                    r["QuoteID"],
                    r["QuoteCode"],
                    Convert.ToDateTime(r["QuoteDate"]).ToString("yyyy/MM/dd HH:mm"),
                    r["DisplayClient"],
                    r["ItemCount"],
                    Convert.ToDecimal(r["TotalAmount"]).ToString("N2") + " ج",
                    r["PriceTier"],
                    r["CreatedByName"]
                );
            }
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

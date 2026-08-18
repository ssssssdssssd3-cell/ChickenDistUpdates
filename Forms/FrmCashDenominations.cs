using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة جرد وعد النقدية بالفئات النقدية (200، 100، 50، 20، 10، 5، 1، 0.50)
    /// </summary>
    public class FrmCashDenominations : Form
    {
        public decimal TotalCash { get; private set; } = 0;
        public string DenominationsSummaryJson { get; private set; } = "";

        private readonly decimal[] _denoms = new decimal[] { 200m, 100m, 50m, 20m, 10m, 5m, 1m, 0.50m };
        private readonly string[] _denomLabels = new string[] 
        { 
            "فئة 200 جنيه", 
            "فئة 100 جنيه", 
            "فئة 50 جنيه", 
            "فئة 20 جنيه", 
            "فئة 10 جنيه", 
            "فئة 5 جنيه", 
            "فئة 1 جنيه", 
            "فئة 0.50 جنيه (نصف)" 
        };

        private TextBox[] _txtCounts;
        private Label[] _lblTotals;
        private Label _lblGrandTotal;
        private Label _lblBanknoteCount;

        public FrmCashDenominations(string initialJson = null, decimal? expectedAmount = null)
        {
            InitializeComponent(expectedAmount);
            if (!string.IsNullOrEmpty(initialJson))
            {
                LoadFromJson(initialJson);
            }
        }

        private void InitializeComponent(decimal? expectedAmount)
        {
            this.Text = "🧮 جرد وعد النقدية بالفئات - درج الكاشير";
            this.Size = new Size(580, 680);
            this.MinimumSize = new Size(540, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 1. رأس النافذة
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            Label lblTitle = new Label
            {
                Text = "💵 جرد النقدية الفعلية بالدرج بالفئات",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 25
            };

            string subText = expectedAmount.HasValue
                ? $"المتوقع دفترياً بالدرج: {expectedAmount.Value:N2} ج  |  أدخل عدد كل فئة لحساب الفعلي تلقائياً"
                : "أدخل عدد الأوراق والقطع النقدية لكل فئة لحساب إجمالي الدرج الفعلي بدقة";

            Label lblSub = new Label
            {
                Text = subText,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // 2. المحتوى وجدول الفئات
            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 10, 15, 10),
                AutoScroll = true
            };

            TableLayoutPanel tblGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = _denoms.Length + 1,
                AutoSize = true,
                BackColor = Theme.BgCard,
                Padding = new Padding(10),
                RightToLeft = RightToLeft.Yes
            };
            tblGrid.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, tblGrid);

            tblGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f)); // اسم الفئة
            tblGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // عدد الأوراق
            tblGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // الإجمالي

            // ترويسة الجدول
            tblGrid.Controls.Add(MakeHeaderLabel("الفئة النقدية"), 0, 0);
            tblGrid.Controls.Add(MakeHeaderLabel("عدد الأوراق / القطع"), 1, 0);
            tblGrid.Controls.Add(MakeHeaderLabel("الإجمالي (ج)"), 2, 0);

            _txtCounts = new TextBox[_denoms.Length];
            _lblTotals = new Label[_denoms.Length];

            for (int i = 0; i < _denoms.Length; i++)
            {
                int rowIdx = i + 1;
                decimal denom = _denoms[i];

                // الفئة
                Label lblDenom = new Label
                {
                    Text = _denomLabels[i],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Theme.TextMain,
                    Height = 34
                };
                tblGrid.Controls.Add(lblDenom, 0, rowIdx);

                // حقل الإدخال للعدد
                TextBox txtCount = new TextBox
                {
                    Text = "0",
                    Dock = DockStyle.Fill,
                    TextAlign = HorizontalAlignment.Center,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    BackColor = Theme.BgInput,
                    ForeColor = Theme.TextMain,
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = i,
                    Height = 28
                };
                txtCount.Enter += (s, e) => { if (txtCount.Text == "0") txtCount.SelectAll(); };
                txtCount.TextChanged += (s, e) => Recalculate();
                txtCount.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down)
                    {
                        int nextIdx = (int)txtCount.Tag + 1;
                        if (nextIdx < _txtCounts.Length) _txtCounts[nextIdx].Focus();
                        else btnApply.Focus();
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Up)
                    {
                        int prevIdx = (int)txtCount.Tag - 1;
                        if (prevIdx >= 0) _txtCounts[prevIdx].Focus();
                        e.Handled = true;
                    }
                };
                _txtCounts[i] = txtCount;
                tblGrid.Controls.Add(txtCount, 1, rowIdx);

                // حقل إجمالي الفئة
                Label lblTotal = new Label
                {
                    Text = "0.00 ج",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    ForeColor = Theme.Accent,
                    Height = 34
                };
                _lblTotals[i] = lblTotal;
                tblGrid.Controls.Add(lblTotal, 2, rowIdx);
            }

            pnlContent.Controls.Add(tblGrid);

            // 3. كارت الإجمالي العام
            Panel pnlTotalCard = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };
            pnlTotalCard.Paint += (s, e) => Theme.DrawCardBorder(e.Graphics, pnlTotalCard);

            _lblGrandTotal = new Label
            {
                Text = "إجمالي النقدية المحسوبة: 0.00 ج",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.Success,
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblBanknoteCount = new Label
            {
                Text = "إجمالي عدد الأوراق / القطع: 0 ورقة",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlTotalCard.Controls.Add(_lblBanknoteCount);
            pnlTotalCard.Controls.Add(_lblGrandTotal);

            // 4. أزرار التحكم السفلية
            Panel pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            btnApply = Theme.MakeButton("✅ اعتماد ونقل الإجمالي للدرج", Theme.Success, new Point(0, 0), new Size(240, 42));
            btnClear = Theme.MakeButton("🔄 تصفير العداد", Color.FromArgb(100, 110, 125), new Point(0, 0), new Size(130, 42));
            btnCancel = Theme.MakeButton("❌ إلغاء", Theme.Danger, new Point(0, 0), new Size(100, 42));

            btnApply.Click += (s, e) =>
            {
                Recalculate();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            btnClear.Click += (s, e) =>
            {
                foreach (var txt in _txtCounts) txt.Text = "0";
                _txtCounts[0].Focus();
            };

            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            FlowLayoutPanel flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };
            btnApply.Margin = new Padding(6, 0, 0, 0);
            btnClear.Margin = new Padding(6, 0, 0, 0);
            btnCancel.Margin = new Padding(6, 0, 0, 0);

            flowBtns.Controls.Add(btnApply);
            flowBtns.Controls.Add(btnClear);
            flowBtns.Controls.Add(btnCancel);
            pnlButtons.Controls.Add(flowBtns);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTotalCard);
            this.Controls.Add(pnlButtons);
            this.Controls.Add(pnlHeader);

            this.Shown += (s, e) => { if (_txtCounts.Length > 0) _txtCounts[0].Focus(); };
        }

        private Button btnApply, btnClear, btnCancel;

        private Label MakeHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Height = 30
            };
        }

        private void Recalculate()
        {
            decimal grandTotal = 0;
            int totalPieces = 0;
            var sb = new StringBuilder();
            sb.Append("{");

            for (int i = 0; i < _denoms.Length; i++)
            {
                decimal denom = _denoms[i];
                int.TryParse(_txtCounts[i].Text.Trim(), out int count);
                if (count < 0) count = 0;

                decimal subTotal = denom * count;
                grandTotal += subTotal;
                totalPieces += count;

                _lblTotals[i].Text = $"{subTotal:N2} ج";

                if (i > 0) sb.Append(",");
                sb.Append($"\"{denom}\":{count}");
            }
            sb.Append("}");

            TotalCash = grandTotal;
            DenominationsSummaryJson = sb.ToString();

            _lblGrandTotal.Text = $"إجمالي النقدية المحسوبة: {grandTotal:N2} ج";
            _lblBanknoteCount.Text = $"إجمالي عدد الأوراق / القطع: {totalPieces:N0} ورقة/قطعة";
        }

        private void LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                for (int i = 0; i < _denoms.Length; i++)
                {
                    string key = $"\"{_denoms[i]}\":";
                    int idx = json.IndexOf(key);
                    if (idx >= 0)
                    {
                        int start = idx + key.Length;
                        int end = json.IndexOfAny(new char[] { ',', '}' }, start);
                        if (end > start)
                        {
                            string valStr = json.Substring(start, end - start).Trim();
                            if (int.TryParse(valStr, out int cnt))
                            {
                                _txtCounts[i].Text = cnt.ToString();
                            }
                        }
                    }
                }
                Recalculate();
            }
            catch { }
        }

        /// <summary>
        /// توليد نص عربي منسق لجرد الفئات للطباعة أو المعاينة
        /// </summary>
        public static string FormatDenominationsForPrint(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "{}" || !json.Contains(":")) return "";
            try
            {
                var sb = new StringBuilder();
                decimal[] denoms = new decimal[] { 200m, 100m, 50m, 20m, 10m, 5m, 1m, 0.50m };
                foreach (var d in denoms)
                {
                    string key = $"\"{d}\":";
                    int idx = json.IndexOf(key);
                    if (idx >= 0)
                    {
                        int start = idx + key.Length;
                        int end = json.IndexOfAny(new char[] { ',', '}' }, start);
                        if (end > start)
                        {
                            string valStr = json.Substring(start, end - start).Trim();
                            if (int.TryParse(valStr, out int cnt) && cnt > 0)
                            {
                                decimal val = d * cnt;
                                sb.AppendLine($"• فئة {d:N0} ج: {cnt} ورقة = {val:N2} ج");
                            }
                        }
                    }
                }
                return sb.ToString();
            }
            catch { return ""; }
        }
    }
}

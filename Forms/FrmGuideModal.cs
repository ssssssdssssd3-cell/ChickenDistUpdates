using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة عرض الدليل والشرح التفصيلي مع التنسيق والبحث والطباعة
    /// </summary>
    public class FrmGuideModal : Form
    {
        private RichTextBox rtbGuide;
        private TextBox txtSearch;
        private string _guideTitle;

        public FrmGuideModal(string guideTitle, Action<RichTextBox> contentBuilder)
        {
            _guideTitle = guideTitle;
            InitUI();
            contentBuilder?.Invoke(rtbGuide);
            rtbGuide.SelectionStart = 0;
            rtbGuide.ScrollToCaret();
        }

        private void InitUI()
        {
            this.Text = $"📖 {_guideTitle}";
            this.Size = new Size(950, 720);
            this.MinimumSize = new Size(800, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Top Bar
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(15, 12, 15, 12)
            };
            this.Controls.Add(pnlTop);

            var lblTitle = new Label
            {
                Text = _guideTitle,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 198, 35),
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(0, 5, 0, 0)
            };
            pnlTop.Controls.Add(lblTitle);

            var pnlTools = new Panel
            {
                Dock = DockStyle.Left,
                Width = 460,
                BackColor = Color.Transparent
            };
            pnlTop.Controls.Add(pnlTools);

            var btnClose = Theme.MakeButton("إغلاق", 370, 4, 80, 34, Color.FromArgb(51, 65, 85));
            btnClose.Click += (s, e) => this.Close();
            pnlTools.Controls.Add(btnClose);

            var btnPrint = Theme.MakeButton("🖨️ طباعة الدليل", 240, 4, 120, 34, Color.FromArgb(40, 120, 180));
            btnPrint.Click += (s, e) => PrintGuide();
            pnlTools.Controls.Add(btnPrint);

            txtSearch = new TextBox
            {
                Location = new Point(10, 6),
                Width = 140,
                Height = 30,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            txtSearch.TextChanged += (s, e) => SearchInGuide(txtSearch.Text.Trim());
            pnlTools.Controls.Add(txtSearch);

            var lblSearch = new Label
            {
                Text = "🔍 بحث:",
                Location = new Point(155, 10),
                AutoSize = true,
                ForeColor = Color.Silver,
                Font = Theme.FontSmall
            };
            pnlTools.Controls.Add(lblSearch);

            // Rich Content Box
            rtbGuide = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(25, 20, 25, 20)
            };
            this.Controls.Add(rtbGuide);
            rtbGuide.BringToFront();
        }

        private void SearchInGuide(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                rtbGuide.SelectAll();
                rtbGuide.SelectionBackColor = Color.FromArgb(248, 250, 252);
                rtbGuide.DeselectAll();
                return;
            }

            int start = 0;
            while (start < rtbGuide.TextLength)
            {
                int index = rtbGuide.Find(query, start, RichTextBoxFinds.None);
                if (index != -1)
                {
                    rtbGuide.Select(index, query.Length);
                    rtbGuide.SelectionBackColor = Color.Yellow;
                    start = index + query.Length;
                }
                else break;
            }
        }

        private void PrintGuide()
        {
            var pd = new PrintDocument();
            string fullText = rtbGuide.Text;
            int textPos = 0;

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                float y = 50;
                var fontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 10.5f);

                g.DrawString(_guideTitle, fontTitle, Brushes.DarkBlue, new PointF(150, y));
                y += 40;

                int charsFitted, linesFilled;
                var format = new StringFormat(StringFormatFlags.DirectionRightToLeft);
                var layoutRect = new RectangleF(40, y, e.MarginBounds.Width, e.MarginBounds.Height - 80);

                string pageText = fullText.Substring(textPos);
                g.MeasureString(pageText, fontBody, layoutRect.Size, format, out charsFitted, out linesFilled);
                g.DrawString(pageText.Substring(0, charsFitted), fontBody, Brushes.Black, layoutRect, format);

                textPos += charsFitted;
                e.HasMorePages = (textPos < fullText.Length);
            };

            using (var ppd = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700 })
            {
                ppd.ShowDialog();
            }
        }

        #region Formatting Helpers
        public static void AppendHeader1(RichTextBox rtb, string text)
        {
            rtb.SelectionFont = new Font("Segoe UI", 15f, FontStyle.Bold);
            rtb.SelectionColor = Color.FromArgb(30, 64, 175);
            rtb.AppendText("\n" + text + "\n");
            rtb.SelectionFont = new Font("Segoe UI", 10.5f);
            rtb.SelectionColor = Color.FromArgb(100, 116, 139);
            rtb.AppendText(new string('─', 75) + "\n");
        }

        public static void AppendHeader2(RichTextBox rtb, string text)
        {
            rtb.SelectionFont = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            rtb.SelectionColor = Color.FromArgb(180, 83, 9);
            rtb.AppendText("\n🔹 " + text + "\n");
        }

        public static void AppendStep(RichTextBox rtb, string stepNum, string title, string detail)
        {
            rtb.SelectionFont = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            rtb.SelectionColor = Color.FromArgb(15, 23, 42);
            rtb.AppendText($"   {stepNum}. {title}\n");

            rtb.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            rtb.SelectionColor = Color.FromArgb(51, 65, 85);
            rtb.AppendText($"      {detail}\n\n");
        }

        public static void AppendParagraph(RichTextBox rtb, string text)
        {
            rtb.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            rtb.SelectionColor = Color.FromArgb(30, 41, 59);
            rtb.AppendText("   " + text + "\n");
        }

        public static void AppendTip(RichTextBox rtb, string text)
        {
            rtb.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            rtb.SelectionColor = Color.FromArgb(22, 101, 52);
            rtb.AppendText("\n   💡 معلومة هامة: ");

            rtb.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            rtb.SelectionColor = Color.FromArgb(22, 101, 52);
            rtb.AppendText(text + "\n\n");
        }
        #endregion
    }
}

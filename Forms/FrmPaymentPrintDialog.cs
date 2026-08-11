using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>نافذة اختيار إجراءات سند التوريد / الصرف (طباعة أو واتساب)</summary>
    public class FrmPaymentPrintDialog : Form
    {
        private Action _onPrintReceipt;
        private Action _onPrintA4;
        private Action _onSendWAText;
        private Action _onSendWAImage;

        public FrmPaymentPrintDialog(string title, string partyName, decimal amount, Action onPrintReceipt, Action onPrintA4, Action onSendWAText, Action onSendWAImage)
        {
            _onPrintReceipt = onPrintReceipt;
            _onPrintA4 = onPrintA4;
            _onSendWAText = onSendWAText;
            _onSendWAImage = onSendWAImage;

            InitUI(title, partyName, amount);
        }

        private void InitUI(string title, string partyName, decimal amount)
        {
            Text = title;
            Size = new Size(550, 360);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgCard;
            Font = Theme.FontMain;

            // ── Top Header Panel ──────────────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(24, 43, 73),
                Padding = new Padding(10)
            };

            var lblHeader = new Label
            {
                Text = $"✅ {title}\nالطرف: {partyName}   |   المبلغ: {amount:N2} ج",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 80)
            };
            pnlTop.Controls.Add(lblHeader);

            // ── Bottom Panel ──────────────────────────────────────
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 6, 10, 6)
            };

            var btnClose = Theme.MakeButton("إغلاق", 0, 0, 110, 34, Color.FromArgb(100, 110, 120));
            btnClose.Dock = DockStyle.Left;
            btnClose.Click += (s, e) => Close();
            pnlBottom.Controls.Add(btnClose);

            // ── Buttons Grid Panel (Fill) ─────────────────────────
            var tblBtns = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Theme.BgCard
            };
            tblBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblBtns.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblBtns.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var btnReceipt = Theme.MakeButton("🧾 طباعة سند ريسيت (80mm)", 0, 0, 0, 0, Theme.Primary);
            btnReceipt.Dock = DockStyle.Fill;
            btnReceipt.Margin = new Padding(6);
            btnReceipt.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnReceipt.Click += (s, e) => { _onPrintReceipt?.Invoke(); };

            var btnA4 = Theme.MakeButton("📄 طباعة سند (A4 / A5)", 0, 0, 0, 0, Color.FromArgb(88, 58, 148));
            btnA4.Dock = DockStyle.Fill;
            btnA4.Margin = new Padding(6);
            btnA4.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnA4.Click += (s, e) => { _onPrintA4?.Invoke(); };

            var btnWAText = Theme.MakeButton("💬 إرسال واتساب (نص)", 0, 0, 0, 0, Color.FromArgb(37, 211, 102));
            btnWAText.Dock = DockStyle.Fill;
            btnWAText.Margin = new Padding(6);
            btnWAText.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnWAText.Click += (s, e) => { _onSendWAText?.Invoke(); };

            var btnWAImage = Theme.MakeButton("🖼️ إرسال واتساب (صورة)", 0, 0, 0, 0, Color.FromArgb(18, 140, 126));
            btnWAImage.Dock = DockStyle.Fill;
            btnWAImage.Margin = new Padding(6);
            btnWAImage.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnWAImage.Click += (s, e) => { _onSendWAImage?.Invoke(); };

            tblBtns.Controls.Add(btnReceipt, 0, 0);
            tblBtns.Controls.Add(btnA4, 1, 0);
            tblBtns.Controls.Add(btnWAText, 0, 1);
            tblBtns.Controls.Add(btnWAImage, 1, 1);

            // ── Dock order: Add Fill FIRST, then Top & Bottom ──
            Controls.Add(tblBtns);
            Controls.Add(pnlTop);
            Controls.Add(pnlBottom);

            tblBtns.BringToFront();
            pnlTop.BringToFront();
            pnlBottom.BringToFront();

            Theme.ApplyFormRTL(this);
        }
    }
}

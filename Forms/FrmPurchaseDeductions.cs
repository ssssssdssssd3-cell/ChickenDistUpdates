using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة تسجيل استقطاعات وتنزيلات فاتورة الشراء (الوكالة: عمولة المكان، النولون، وهبة العمال)
    /// </summary>
    public class FrmPurchaseDeductions : Form
    {
        public decimal MarketCommission { get; private set; }
        public decimal FreightCost { get; private set; }
        public decimal PorterageFee { get; private set; }
        public string DeductionsNotes { get; private set; }
        public decimal TotalDeductions => MarketCommission + FreightCost + PorterageFee;

        private TextBox txtCommission;
        private TextBox txtFreight;
        private TextBox txtPorterage;
        private TextBox txtNotes;
        private Label lblTotalVal;

        public FrmPurchaseDeductions(decimal commission = 0m, decimal freight = 0m, decimal porterage = 0m, string notes = "")
        {
            MarketCommission = commission;
            FreightCost = freight;
            PorterageFee = porterage;
            DeductionsNotes = notes ?? "";

            InitUI();
        }

        private void InitUI()
        {
            Text = "📉 استقطاعات وتنزيلات فاتورة الشراء (الوكالة)";
            Size = new Size(480, 440);
            StartPosition = FormStartPosition.CenterParent;
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
                Height = 60,
                BackColor = Color.FromArgb(24, 43, 73),
                Padding = new Padding(12)
            };

            var lblHeader = new Label
            {
                Text = "📉 استقطاعات فاتورة الشراء (تنزيل من حساب المورد)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 205, 90)
            };
            pnlTop.Controls.Add(lblHeader);

            // ── Main Content Panel ────────────────────────────────
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 10),
                BackColor = Color.Transparent
            };

            int curY = 15;
            int lblW = 190;
            int inputX = 210;
            int inputW = 220;

            // 1. عمولة المكان / الوكالة
            var lblComm = new Label
            {
                Text = "🏪 عمولة المكان / الوكالة:",
                Location = new Point(15, curY + 4),
                Size = new Size(lblW, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain
            };
            txtCommission = CreateNumericTextBox(MarketCommission, inputX, curY, inputW);
            pnlContent.Controls.Add(lblComm);
            pnlContent.Controls.Add(txtCommission);
            curY += 40;

            // 2. النولون (أجرة النقل)
            var lblFreight = new Label
            {
                Text = "🚚 النولون (أجرة النقل / العربية):",
                Location = new Point(15, curY + 4),
                Size = new Size(lblW, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain
            };
            txtFreight = CreateNumericTextBox(FreightCost, inputX, curY, inputW);
            pnlContent.Controls.Add(lblFreight);
            pnlContent.Controls.Add(txtFreight);
            curY += 40;

            // 3. وهبة العمال / الشيالة
            var lblPorterage = new Label
            {
                Text = "👷 وهبة العمال / الشيالة:",
                Location = new Point(15, curY + 4),
                Size = new Size(lblW, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain
            };
            txtPorterage = CreateNumericTextBox(PorterageFee, inputX, curY, inputW);
            pnlContent.Controls.Add(lblPorterage);
            pnlContent.Controls.Add(txtPorterage);
            curY += 40;

            // 4. ملاحظات وبيان
            var lblNotes = new Label
            {
                Text = "📝 بيان وملاحظات الاستقطاع:",
                Location = new Point(15, curY + 4),
                Size = new Size(lblW, 24),
                Font = Theme.FontMain,
                ForeColor = Theme.TextMain
            };
            txtNotes = new TextBox
            {
                Location = new Point(inputX, curY),
                Size = new Size(inputW, 28),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                Text = DeductionsNotes
            };
            pnlContent.Controls.Add(lblNotes);
            pnlContent.Controls.Add(txtNotes);
            curY += 45;

            // فاصل
            var sep = new Panel
            {
                Location = new Point(15, curY),
                Size = new Size(415, 1),
                BackColor = Color.FromArgb(210, 220, 230)
            };
            pnlContent.Controls.Add(sep);
            curY += 10;

            // 5. إجمالي الاستقطاعات
            var pnlTotal = new Panel
            {
                Location = new Point(15, curY),
                Size = new Size(415, 42),
                BackColor = Color.FromArgb(254, 242, 242),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblTotalTitle = new Label
            {
                Text = "إجمالي الاستقطاعات المخصومة من الفاتورة:",
                Location = new Point(135, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28)
            };
            lblTotalVal = new Label
            {
                Text = "0.00 ج",
                Location = new Point(10, 8),
                Size = new Size(120, 26),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28)
            };
            pnlTotal.Controls.Add(lblTotalTitle);
            pnlTotal.Controls.Add(lblTotalVal);
            pnlContent.Controls.Add(pnlTotal);
            curY += 50;

            // 6. ملاحظة توجيهية
            var lblTip = new Label
            {
                Text = "💡 ملاحظة: يتم خصم هذه المبالغ تلقائياً من صافي حساب المورد دون التأثير على كميات أو تكلفة المخزون.",
                Location = new Point(15, curY),
                Size = new Size(415, 34),
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };
            pnlContent.Controls.Add(lblTip);

            // ── Bottom Buttons Panel ──────────────────────────────
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(240, 243, 246),
                Padding = new Padding(15, 10, 15, 10)
            };

            var btnConfirm = new Button
            {
                Text = "💾 تأكيد وتطبيق [Enter]",
                Size = new Size(160, 35),
                Location = new Point(275, 10),
                BackColor = Theme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += (s, e) => ConfirmAndClose();

            var btnClear = new Button
            {
                Text = "🗑️ تصفير الاستقطاعات",
                Size = new Size(130, 35),
                Location = new Point(135, 10),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) =>
            {
                txtCommission.Text = "0";
                txtFreight.Text = "0";
                txtPorterage.Text = "0";
                txtNotes.Clear();
                RecalcTotal();
            };

            var btnCancel = new Button
            {
                Text = "إلغاء [Esc]",
                Size = new Size(95, 35),
                Location = new Point(30, 10),
                BackColor = Color.FromArgb(220, 224, 230),
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontMain,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlBottom.Controls.Add(btnConfirm);
            pnlBottom.Controls.Add(btnClear);
            pnlBottom.Controls.Add(btnCancel);

            Controls.Add(pnlContent);
            Controls.Add(pnlBottom);
            Controls.Add(pnlTop);

            RecalcTotal();

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !txtNotes.Focused)
                {
                    e.Handled = true;
                    ConfirmAndClose();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        private TextBox CreateNumericTextBox(decimal val, int x, int y, int w)
        {
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 28),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Text = val > 0 ? val.ToString("G29") : "0"
            };
            tb.TextChanged += (s, e) => RecalcTotal();
            tb.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                    e.Handled = true;
                if (e.KeyChar == '.' && (s as TextBox).Text.IndexOf('.') > -1)
                    e.Handled = true;
            };
            tb.Enter += (s, e) => tb.SelectAll();
            return tb;
        }

        private void RecalcTotal()
        {
            decimal.TryParse(txtCommission.Text, out decimal comm);
            decimal.TryParse(txtFreight.Text, out decimal freight);
            decimal.TryParse(txtPorterage.Text, out decimal porter);

            decimal tot = Math.Max(0m, comm) + Math.Max(0m, freight) + Math.Max(0m, porter);
            lblTotalVal.Text = tot.ToString("N2") + " ج";
        }

        private void ConfirmAndClose()
        {
            decimal.TryParse(txtCommission.Text, out decimal comm);
            decimal.TryParse(txtFreight.Text, out decimal freight);
            decimal.TryParse(txtPorterage.Text, out decimal porter);

            MarketCommission = Math.Max(0m, comm);
            FreightCost = Math.Max(0m, freight);
            PorterageFee = Math.Max(0m, porter);
            DeductionsNotes = txtNotes.Text?.Trim() ?? "";

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

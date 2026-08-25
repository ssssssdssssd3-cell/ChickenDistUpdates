using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة حوار موحدة لاختيار نوع وحجم الطباعة (ريسيت / A4 / A5 / إذن تحضير / إلغاء)
    /// </summary>
    public class FrmPrintChoiceDialog : Form
    {
        public string SelectedChoice { get; private set; } = null; // "Receipt", "A4", "A5", "Prep", null

        public FrmPrintChoiceDialog(string message = "هل تريد طباعة الفاتورة الآن؟ يرجى اختيار نوع الطباعة المطلوب:", bool allowPrep = true)
        {
            InitializeDialog(message, allowPrep);
        }

        private void InitializeDialog(string message, bool allowPrep)
        {
            this.Text = "🖨️ اختيار نوع وحجم الطباعة";
            this.Size = new Size(520, allowPrep ? 380 : 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    SelectedChoice = null;
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };

            // Top message panel
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(15, 12, 15, 10),
                BackColor = Color.FromArgb(20, 35, 60)
            };

            var lblIcon = new Label
            {
                Text = "🖨️",
                Font = new Font("Segoe UI", 26f),
                AutoSize = true,
                Location = new Point(440, 15),
                ForeColor = Color.Gold
            };

            var lblMsg = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(410, 55),
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlTop.Controls.Add(lblIcon);
            pnlTop.Controls.Add(lblMsg);
            this.Controls.Add(pnlTop);

            // Buttons Container
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 15, 30, 15),
                BackColor = Theme.BgCard
            };

            int btnY = 15;
            int btnHeight = 42;
            int btnSpacing = 10;

            // 1. Receipt Button
            var btnReceipt = Theme.MakeButton("🧾 طباعة ريسيت حراري (Receipt 80mm)", Color.FromArgb(30, 90, 160));
            btnReceipt.Location = new Point(30, btnY);
            btnReceipt.Size = new Size(445, btnHeight);
            btnReceipt.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnReceipt.Click += (s, e) => { SelectedChoice = "Receipt"; this.DialogResult = DialogResult.OK; this.Close(); };
            pnlButtons.Controls.Add(btnReceipt);
            btnY += btnHeight + btnSpacing;

            // 2. A4 Button
            var btnA4 = Theme.MakeButton("📄 طباعة فاتورة A4 كاملة (A4 Sheet)", Color.FromArgb(35, 120, 75));
            btnA4.Location = new Point(30, btnY);
            btnA4.Size = new Size(445, btnHeight);
            btnA4.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnA4.Click += (s, e) => { SelectedChoice = "A4"; this.DialogResult = DialogResult.OK; this.Close(); };
            pnlButtons.Controls.Add(btnA4);
            btnY += btnHeight + btnSpacing;

            // 3. A5 Button
            var btnA5 = Theme.MakeButton("📑 طباعة فاتورة A5 نصف صفحة (A5 Sheet)", Color.FromArgb(140, 85, 25));
            btnA5.Location = new Point(30, btnY);
            btnA5.Size = new Size(445, btnHeight);
            btnA5.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnA5.Click += (s, e) => { SelectedChoice = "A5"; this.DialogResult = DialogResult.OK; this.Close(); };
            pnlButtons.Controls.Add(btnA5);
            btnY += btnHeight + btnSpacing;

            // 4. Preparation Slip Button (Optional)
            if (allowPrep)
            {
                var btnPrep = Theme.MakeButton("📋 طباعة إذن تحضير مخزن (Preparation Slip)", Color.FromArgb(90, 60, 140));
                btnPrep.Location = new Point(30, btnY);
                btnPrep.Size = new Size(445, btnHeight);
                btnPrep.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                btnPrep.Click += (s, e) => { SelectedChoice = "Prep"; this.DialogResult = DialogResult.OK; this.Close(); };
                pnlButtons.Controls.Add(btnPrep);
                btnY += btnHeight + btnSpacing;
            }

            // 5. Skip / Cancel Button
            var btnCancel = Theme.MakeButton("❌ عدم الطباعة (إلغاء)", Color.FromArgb(80, 80, 90));
            btnCancel.Location = new Point(30, btnY);
            btnCancel.Size = new Size(445, btnHeight - 4);
            btnCancel.Font = new Font("Segoe UI", 10f);
            btnCancel.Click += (s, e) => { SelectedChoice = null; this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlButtons.Controls.Add(btnCancel);

            this.Controls.Add(pnlButtons);
        }

        /// <summary>
        /// يُظهر نافذة الاختيار وينفذ الطباعة فوراً وفق اختيار المستخدم لفاتورة بيع
        /// </summary>
        public static void PromptAndPrintSale(IWin32Window owner, int saleID, string customTitle = null)
        {
            if (saleID <= 0) return;

            string msg = customTitle != null
                ? $"{customTitle}\nاختر مقاس / نوع الطباعة المطلوب:"
                : $"تم حفظ الفاتورة بنجاح رقم [{saleID}]!\nاختر نوع الطباعة المطلوب:";

            using (var dlg = new FrmPrintChoiceDialog(msg, allowPrep: true))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedChoice))
                {
                    try
                    {
                        if (dlg.SelectedChoice == "Prep")
                        {
                            PrintHelper.PrintSalePreparationSlip(saleID);
                        }
                        else
                        {
                            new FrmPrintSale(saleID, dlg.SelectedChoice, showPreview: false);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("خطأ أثناء الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}

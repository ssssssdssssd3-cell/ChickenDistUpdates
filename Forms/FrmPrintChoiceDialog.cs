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
        private CheckBox chkRememberDirect;

        public FrmPrintChoiceDialog(string message = "هل تريد طباعة الفاتورة الآن؟ يرجى اختيار نوع الطباعة المطلوب:", bool allowPrep = true)
        {
            InitializeDialog(message, allowPrep);
        }

        private void InitializeDialog(string message, bool allowPrep)
        {
            this.Text = "🖨️ اختيار نوع وحجم الطباعة";
            this.Size = new Size(540, allowPrep ? 440 : 380);
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

            // 1. Top message panel
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.FromArgb(20, 35, 60)
            };

            var lblIcon = new Label
            {
                Text = "🖨️",
                Font = new Font("Segoe UI", 26f),
                AutoSize = true,
                Location = new Point(450, 15),
                ForeColor = Color.Gold
            };

            var lblMsg = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                Size = new Size(425, 56),
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlTop.Controls.Add(lblIcon);
            pnlTop.Controls.Add(lblMsg);

            // 2. Buttons Container (Dock Fill)
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25, 10, 25, 10),
                BackColor = Theme.BgCard
            };

            int btnY = 12;
            int btnHeight = 44;
            int btnSpacing = 10;
            int btnWidth = 470;
            int btnX = 25;

            // 1. Receipt Button
            var btnReceipt = Theme.MakeButton("🧾 طباعة ريسيت حراري (Receipt 80mm)", Color.FromArgb(30, 90, 160));
            btnReceipt.Location = new Point(btnX, btnY);
            btnReceipt.Size = new Size(btnWidth, btnHeight);
            btnReceipt.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnReceipt.Click += (s, e) => { OnChoiceSelected("Receipt"); };
            pnlButtons.Controls.Add(btnReceipt);
            btnY += btnHeight + btnSpacing;

            // 2. A4 Button
            var btnA4 = Theme.MakeButton("📄 طباعة فاتورة A4 كاملة (A4 Sheet)", Color.FromArgb(35, 120, 75));
            btnA4.Location = new Point(btnX, btnY);
            btnA4.Size = new Size(btnWidth, btnHeight);
            btnA4.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnA4.Click += (s, e) => { OnChoiceSelected("A4"); };
            pnlButtons.Controls.Add(btnA4);
            btnY += btnHeight + btnSpacing;

            // 3. A5 Button
            var btnA5 = Theme.MakeButton("📑 طباعة فاتورة A5 نصف صفحة (A5 Sheet)", Color.FromArgb(140, 85, 25));
            btnA5.Location = new Point(btnX, btnY);
            btnA5.Size = new Size(btnWidth, btnHeight);
            btnA5.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnA5.Click += (s, e) => { OnChoiceSelected("A5"); };
            pnlButtons.Controls.Add(btnA5);
            btnY += btnHeight + btnSpacing;

            // 4. Preparation Slip Button (Optional)
            if (allowPrep)
            {
                var btnPrep = Theme.MakeButton("📋 طباعة إذن تحضير مخزن (Preparation Slip)", Color.FromArgb(90, 60, 140));
                btnPrep.Location = new Point(btnX, btnY);
                btnPrep.Size = new Size(btnWidth, btnHeight);
                btnPrep.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                btnPrep.Click += (s, e) => { OnChoiceSelected("Prep"); };
                pnlButtons.Controls.Add(btnPrep);
                btnY += btnHeight + btnSpacing;
            }

            // 5. Skip / Cancel Button
            var btnCancel = Theme.MakeButton("❌ عدم الطباعة (إلغاء)", Color.FromArgb(75, 85, 99));
            btnCancel.Location = new Point(btnX, btnY);
            btnCancel.Size = new Size(btnWidth, btnHeight - 4);
            btnCancel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnCancel.Click += (s, e) => { SelectedChoice = null; this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlButtons.Controls.Add(btnCancel);
            btnY += btnHeight + 4;

            // 6. Optional Direct Print CheckBox
            chkRememberDirect = new CheckBox
            {
                Text = "تثبيت هذا المقاس كطباعة مباشرة فورية لاحقاً (بدون سؤال)",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 195, 215),
                Location = new Point(btnX + 5, btnY),
                Size = new Size(btnWidth, 24),
                RightToLeft = RightToLeft.Yes,
                Cursor = Cursors.Hand
            };
            pnlButtons.Controls.Add(chkRememberDirect);

            // Adding Fill control first and Top control second ensures no overlap in WinForms
            this.Controls.Add(pnlButtons);
            this.Controls.Add(pnlTop);
        }

        private void OnChoiceSelected(string choice)
        {
            SelectedChoice = choice;
            if (chkRememberDirect != null && chkRememberDirect.Checked && choice != "Prep")
            {
                AppConfig.PrintBehaviorOnSave = "Direct";
                AppConfig.DefaultInvoiceFormat = choice;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// يُظهر نافذة الاختيار أو يطبع مباشرة وفق إعدادات النظام لفاتورة بيع
        /// </summary>
        public static void PromptAndPrintSale(IWin32Window owner, int saleID, string customTitle = null)
        {
            if (saleID <= 0) return;

            // 1. إذا تم ضبط الإعداد على عدم الطباعة تلقائياً
            if (AppConfig.PrintBehaviorOnSave == "None")
            {
                return;
            }

            // 2. إذا تم ضبط الإعداد على الطباعة المباشرة دون سؤال
            if (AppConfig.PrintBehaviorOnSave == "Direct")
            {
                try
                {
                    string format = AppConfig.DefaultInvoiceFormat;
                    if (string.IsNullOrEmpty(format)) format = "Receipt";
                    new FrmPrintSale(saleID, format, showPreview: false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ أثناء الطباعة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            // 3. الإعداد الافتراضي: إظهار نافذة الحوار للاختيار
            string msg = customTitle != null
                ? $"{customTitle}\nاختر مقاس / نوع الطباعة المطلوب:"
                : $"تم حفظ الفاتورة بنجاح رقم [{saleID}]!\nاختر مقاس / نوع الطباعة المطلوب:";

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

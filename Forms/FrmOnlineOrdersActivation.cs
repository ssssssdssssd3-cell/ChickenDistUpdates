using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة تفعيل اشتراك شاشة الطلبات الأونلاين.
    /// تُعرض لما يكون الاشتراك منتهياً أو غير مُفعَّل.
    /// الكود الصحيح: Pro@soft2026 — يفتح الشاشة لـ 30 يوم إضافي.
    /// </summary>
    public class FrmOnlineOrdersActivation : Form
    {
        private const string ACTIVATION_CODE = "Pro@soft2026";

        private TextBox txtCode;
        private Button btnActivate;
        private Button btnCancel;
        private Label lblStatus;
        private Label lblExpiry;

        public FrmOnlineOrdersActivation()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "🔒 تفعيل اشتراك شاشة الطلبات الأونلاين";
            this.Size = new Size(460, 360);
            this.MinimumSize = new Size(420, 340);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.Font = new Font("Segoe UI", 9.5f);

            // ── أيقونة القفل الكبيرة ──
            var lblIcon = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI Emoji", 44f),
                ForeColor = Color.FromArgb(239, 68, 68),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── العنوان ──
            var lblTitle = new Label
            {
                Text = "شاشة الطلبات الأونلاين",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 0)
            };

            var lblSub = new Label
            {
                Text = "الاشتراك الشهري منتهٍ أو غير مُفعَّل",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(248, 113, 113),
                AutoSize = true,
                Padding = new Padding(0, 2, 0, 8)
            };

            // ── تاريخ انتهاء آخر اشتراك ──
            lblExpiry = new Label
            {
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 12)
            };

            DateTime exp = AppConfig.OnlineOrdersExpiryDate;
            if (exp == DateTime.MinValue)
                lblExpiry.Text = "لم يتم التفعيل من قبل";
            else
                lblExpiry.Text = $"آخر اشتراك انتهى في: {exp:yyyy/MM/dd}";

            // ── خانة إدخال الكود ──
            var lblCodePrompt = new Label
            {
                Text = "أدخل كود التفعيل:",
                ForeColor = Color.FromArgb(226, 232, 240),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f)
            };

            txtCode = new TextBox
            {
                Size = new Size(300, 32),
                Font = new Font("Consolas", 12f),
                UseSystemPasswordChar = false,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) TryActivate();
            };

            // ── رسالة الحالة ──
            lblStatus = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(239, 68, 68),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            // ── أزرار ──
            btnActivate = new Button
            {
                Text = "✅ تفعيل الاشتراك",
                Size = new Size(160, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnActivate.FlatAppearance.BorderSize = 0;
            btnActivate.Click += (s, e) => TryActivate();

            btnCancel = new Button
            {
                Text = "إلغاء",
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            // ── تجميع الـ Layout ──
            var pnlCenter = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Padding = new Padding(30, 20, 30, 10)
            };

            var pnlButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 0)
            };
            pnlButtons.Controls.Add(btnActivate);
            pnlButtons.Controls.Add(btnCancel);

            // أيقونة القفل في المنتصف أفقياً
            lblIcon.Margin = new Padding(this.ClientSize.Width / 2 - 50, 0, 0, 0);

            pnlCenter.Controls.Add(lblIcon);
            pnlCenter.Controls.Add(lblTitle);
            pnlCenter.Controls.Add(lblSub);
            pnlCenter.Controls.Add(lblExpiry);
            pnlCenter.Controls.Add(lblCodePrompt);
            pnlCenter.Controls.Add(txtCode);
            pnlCenter.Controls.Add(lblStatus);
            pnlCenter.Controls.Add(pnlButtons);

            this.Controls.Add(pnlCenter);
            this.AcceptButton = btnActivate;
            this.CancelButton = btnCancel;
        }

        private void TryActivate()
        {
            string entered = txtCode.Text.Trim();

            if (string.IsNullOrEmpty(entered))
            {
                lblStatus.Text = "⚠️ أدخل كود التفعيل أولاً";
                lblStatus.ForeColor = Color.FromArgb(251, 191, 36);
                return;
            }

            if (entered == ACTIVATION_CODE)
            {
                // ✅ صحيح — تفعيل 30 يوم
                AppConfig.ActivateOnlineOrdersSubscription();
                DateTime newExpiry = AppConfig.OnlineOrdersExpiryDate;

                MessageBox.Show(
                    $"✅ تم تفعيل الاشتراك بنجاح!\n\nينتهي الاشتراك في: {newExpiry:yyyy/MM/dd}",
                    "تم التفعيل",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                // ❌ خطأ
                lblStatus.Text = "❌ كود التفعيل غير صحيح";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                txtCode.Clear();
                txtCode.Focus();
            }
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة الإدخال السريع للأصناف (تسجيل متتالي سريع جداً)
    /// </summary>
    public class FrmQuickAdd : Form
    {
        private TextBox txtName, txtCode;
        private NumericUpDown nudPurchasePrice, nudPrice;
        private CheckBox chkIsService, chkIsQuickItem;
        private Button btnSave, btnClose;
        private Label lblStatus;
        private Timer statusTimer;

        public FrmQuickAdd()
        {
            InitUI();
            
            // مؤقت لمسح حالة الحفظ بعد ثانيتين
            statusTimer = new Timer { Interval = 2500 };
            statusTimer.Tick += (s, e) => {
                lblStatus.Text = "";
                statusTimer.Stop();
            };
        }

        private void InitUI()
        {
            this.Text = "⚡ الإدخال السريع للأصناف";
            this.Size = new Size(500, 485);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlTitle = Theme.MakeTitleBar("⚡ الإدخال السريع للأصناف", "إضافة أصناف جديدة متتالية بسرعة فائقة مع التحقق التلقائي.");
            this.Controls.Add(pnlTitle);

            var pnlForm = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(445, 275),
                BackColor = Theme.BgCard,
                Padding = new Padding(15)
            };

            int y = 15;

            // الباركود / الكود
            pnlForm.Controls.Add(new Label { Text = "الباركود / الكود:", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtCode = new TextBox { Location = new Point(165, y), Width = 260, Height = 30, Font = new Font("Segoe UI", 11f), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(txtCode);
            y += 40;

            // اسم الصنف
            pnlForm.Controls.Add(new Label { Text = "اسم الصنف:", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtName = new TextBox { Location = new Point(165, y), Width = 260, Height = 30, Font = new Font("Segoe UI", 11f), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(txtName);
            y += 40;

            // سعر الشراء
            pnlForm.Controls.Add(new Label { Text = "سعر الشراء:", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            nudPurchasePrice = new NumericUpDown { Location = new Point(165, y), Width = 260, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, Font = new Font("Segoe UI", 11f), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(nudPurchasePrice);
            y += 40;

            // سعر البيع
            pnlForm.Controls.Add(new Label { Text = "سعر البيع (قطاعي):", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            nudPrice = new NumericUpDown { Location = new Point(165, y), Width = 260, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, Font = new Font("Segoe UI", 11f), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(nudPrice);
            y += 40;

            // صنف بيع سريع
            chkIsQuickItem = new CheckBox
            {
                Text = "📌 صنف بيع سريع (شاشة POS)",
                Location = new Point(20, y),
                Width = 280,
                Height = 26,
                ForeColor = Color.FromArgb(0, 120, 180),
                Checked = false,
                AutoSize = false,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };
            pnlForm.Controls.Add(chkIsQuickItem);
            y += 32;

            // صنف يباع بالسالب
            chkIsService = new CheckBox
            {
                Text = "⚡ صنف يباع بالسالب (خدمات / مصروفات)",
                Location = new Point(20, y),
                Width = 280,
                Height = 26,
                ForeColor = Color.FromArgb(180, 120, 0),
                Checked = false,
                AutoSize = false,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };
            pnlForm.Controls.Add(chkIsService);

            this.Controls.Add(pnlForm);

            // حالة الحفظ
            lblStatus = new Label
            {
                Text = "",
                Location = new Point(20, 365),
                Size = new Size(445, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Success
            };
            this.Controls.Add(lblStatus);

            // الأزرار
            btnSave = Theme.MakeButton("💾 حفظ الصنف (Ctrl+S)", 250, 395, 215, 36, Theme.Success);
            btnSave.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnClose = Theme.MakeButton("إغلاق ↩", 20, 395, 220, 36, Color.FromArgb(70, 80, 95));
            btnClose.Click += (s, e) => {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnClose);

            // اختصارات لوحة المفاتيح
            this.KeyPreview = true;
            this.KeyDown += FrmQuickAdd_KeyDown;

            // عند الضغط على Enter في سعر البيع يتم الحفظ مباشرة
            nudPrice.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    BtnSave_Click(this, EventArgs.Empty);
                }
            };

            // تهيئة الحقول عند التحميل
            this.Load += (s, e) => {
                ResetForm();
            };

            Theme.ApplyFormRTL(this);
        }

        private void FrmQuickAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                BtnSave_Click(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void ResetForm()
        {
            txtCode.Text = ProductDAL.GetNextProductCode();
            txtName.Clear();
            nudPurchasePrice.Value = 0;
            nudPrice.Value = 0;
            chkIsQuickItem.Checked = false;
            chkIsService.Checked = false;
            txtCode.Focus();
            txtCode.SelectAll();
        }

        private void ShowStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            statusTimer.Stop();
            statusTimer.Start();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string code = txtCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowStatus("❌ يرجى إدخال اسم الصنف", Theme.Danger);
                txtName.Focus();
                return;
            }

            if (nudPrice.Value < 0 || nudPurchasePrice.Value < 0)
            {
                ShowStatus("❌ لا يمكن إدخال أسعار سالبة", Theme.Danger);
                return;
            }

            // التحقق من تكرار الباركود
            if (!string.IsNullOrEmpty(code) && ProductDAL.IsCodeExists(code, 0))
            {
                ShowStatus($"❌ الباركود '{code}' مستخدم لصنف آخر", Theme.Danger);
                txtCode.Focus();
                txtCode.SelectAll();
                return;
            }

            // التحقق من تكرار الاسم
            if (ProductDAL.IsNameExists(name, 0))
            {
                ShowStatus($"❌ اسم الصنف '{name}' مستخدم بالفعل", Theme.Danger);
                txtName.Focus();
                txtName.SelectAll();
                return;
            }

            // حفظ الصنف
            try
            {
                int id = ProductDAL.Save(
                    0, code, name, "قطعة", nudPrice.Value, true,
                    nudPurchasePrice.Value, 0, "", "", null, "", "", "", 
                    0, 0, null, true,
                    chkIsService.Checked,
                    null, null, null, null,
                    null, null, null, null, null,
                    null, chkIsQuickItem.Checked
                );

                if (id > 0)
                {
                    ShowStatus($"✅ تم حفظ '{name}' بنجاح!", Theme.Success);
                    ResetForm();
                }
                else
                {
                    ShowStatus("❌ فشل حفظ الصنف في قاعدة البيانات", Theme.Danger);
                }
            }
            catch (Exception ex)
            {
                ShowStatus("❌ خطأ: " + ex.Message, Theme.Danger);
            }
        }
    }
}

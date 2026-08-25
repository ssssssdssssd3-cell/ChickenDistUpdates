using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmAdjustment : Form
    {
        private int _id;
        private bool _isClient;
        private decimal _currentBalance;
        private RadioButton rdoDiscount, rdoAddition;
        private TextBox txtAmount;
        private TextBox txtNotes;
        private Label lblCurrentBalVal;
        private Label lblNewBalVal;
        private Button btnOk, btnCancel;

        public FrmAdjustment(int id, string name, bool isClient)
        {
            _id = id;
            _isClient = isClient;
            
            try
            {
                _currentBalance = _isClient ? ClientDAL.GetClientBalance(_id) : SupplierDAL.GetBalance(_id);
            }
            catch
            {
                _currentBalance = 0m;
            }

            this.Text = (_isClient ? "⚖️ تسوية حساب العميل: " : "⚖️ تسوية حساب المورد: ") + name;
            this.Size = new Size(420, 390);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            int y = 15;

            // 1. بطاقة الأرصدة (الرصيد الحالي والرصيد بعد التسوية)
            var pnlBalPreview = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(375, 75),
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(10, 8, 10, 8)
            };

            var lblCurTitle = new Label
            {
                Text = "الرصيد الحالي:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(245, 10),
                AutoSize = true
            };
            lblCurrentBalVal = new Label
            {
                Text = _currentBalance.ToString("N2") + " ج",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = _currentBalance < 0 ? Color.FromArgb(248, 113, 113) : (_currentBalance > 0 ? Color.FromArgb(74, 222, 128) : Color.White),
                Location = new Point(10, 8),
                Width = 230,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblNewTitle = new Label
            {
                Text = "الرصيد بعد الحركة:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21), // Gold
                Location = new Point(245, 42),
                AutoSize = true
            };
            lblNewBalVal = new Label
            {
                Text = _currentBalance.ToString("N2") + " ج",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Location = new Point(10, 38),
                Width = 230,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlBalPreview.Controls.AddRange(new Control[] { lblCurTitle, lblCurrentBalVal, lblNewTitle, lblNewBalVal });
            this.Controls.Add(pnlBalPreview);
            y += 85;

            // 2. خيارات نوع التسوية
            var pnlType = new GroupBox
            {
                Text = "نوع حركة التسوية",
                Location = new Point(15, y),
                Size = new Size(375, 75),
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            
            rdoDiscount = new RadioButton
            {
                Text = _isClient ? "خصم من العميل (تقليل مديونيته)" : "خصم من المورد (تقليل مستحقاته)",
                Location = new Point(15, 22),
                Size = new Size(345, 22),
                Checked = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            rdoDiscount.CheckedChanged += (s, e) => UpdateBalancePreview();
            
            rdoAddition = new RadioButton
            {
                Text = _isClient ? "إضافة للعميل (زيادة مديونيته)" : "إضافة للمورد (زيادة مستحقاته)",
                Location = new Point(15, 46),
                Size = new Size(345, 22),
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            rdoAddition.CheckedChanged += (s, e) => UpdateBalancePreview();

            pnlType.Controls.Add(rdoDiscount);
            pnlType.Controls.Add(rdoAddition);
            this.Controls.Add(pnlType);
            y += 85;

            // 3. المبلغ (كتابة يدوية بدون أسهم)
            this.Controls.Add(new Label { 
                Text = "مبلغ التسوية (ج):", 
                Location = new Point(15, y + 6), 
                AutoSize = true, 
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            });
            
            txtAmount = new TextBox
            {
                Location = new Point(140, y),
                Width = 250,
                Height = 32,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Theme.BgInput,
                ForeColor = Color.FromArgb(250, 204, 21),
                TextAlign = HorizontalAlignment.Center
            };
            txtAmount.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                    e.Handled = true;
                if (e.KeyChar == '.' && (s as TextBox).Text.IndexOf('.') > -1)
                    e.Handled = true;
            };
            txtAmount.TextChanged += (s, e) => UpdateBalancePreview();
            txtAmount.Enter += (s, e) => txtAmount.SelectAll();
            this.Controls.Add(txtAmount);
            y += 45;

            // 4. ملاحظات
            this.Controls.Add(new Label { 
                Text = "ملاحظات / البيان:", 
                Location = new Point(15, y + 4), 
                AutoSize = true, 
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f)
            });
            
            txtNotes = new TextBox
            {
                Location = new Point(140, y),
                Width = 250,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                RightToLeft = RightToLeft.Yes,
                Font = new Font("Segoe UI", 10f)
            };
            this.Controls.Add(txtNotes);
            y += 50;

            // 5. أزرار
            btnOk = Theme.MakeButton("✅ تأكيد التسوية", 235, y, 155, 38, Theme.Accent);
            btnCancel = Theme.MakeButton("❌ إلغاء", 140, y, 90, 38, Color.FromArgb(90, 90, 90));
            btnOk.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });

            this.Shown += (s, e) => txtAmount.Focus();
        }

        private void UpdateBalancePreview()
        {
            decimal amt = 0m;
            decimal.TryParse(txtAmount.Text.Trim(), out amt);

            decimal newBal = _currentBalance;
            if (rdoDiscount.Checked)
            {
                newBal = _currentBalance - amt;
            }
            else
            {
                newBal = _currentBalance + amt;
            }

            lblNewBalVal.Text = newBal.ToString("N2") + " ج";
            lblNewBalVal.ForeColor = newBal < 0 ? Color.FromArgb(248, 113, 113) : (newBal > 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(250, 204, 21));
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amt) || amt <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ تسوية صحيح أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAmount.Focus();
                return;
            }

            string note = txtNotes.Text.Trim();
            if (string.IsNullOrEmpty(note))
            {
                note = rdoDiscount.Checked ? "تسوية خصم رصيد" : "تسوية إضافة رصيد";
            }

            try
            {
                if (_isClient)
                {
                    ClientDAL.AddAdjustment(_id, amt, rdoDiscount.Checked, note);
                    MessageBox.Show("✅ تم تسجيل تسوية العميل بنجاح (دون التأثير على الخزينة).", "تمت التسوية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SupplierDAL.AddAdjustment(_id, amt, rdoDiscount.Checked, note);
                    MessageBox.Show("✅ تم تسجيل تسوية المورد بنجاح (دون التأثير على الخزينة).", "تمت التسوية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ فشل تسجيل التسوية:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

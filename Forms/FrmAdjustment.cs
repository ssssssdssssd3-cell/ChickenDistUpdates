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
        private RadioButton rdoDiscount, rdoAddition;
        private NumericUpDown nudAmount;
        private TextBox txtNotes;
        private Button btnOk, btnCancel;

        public FrmAdjustment(int id, string name, bool isClient)
        {
            _id = id;
            _isClient = isClient;
            
            this.Text = (_isClient ? "⚖️ تسوية حساب العميل: " : "⚖️ تسوية حساب المورد: ") + name;
            this.Size = new Size(380, 290);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgCard;
            this.Font = Theme.FontMain;

            int y = 20;

            // خيارات نوع التسوية
            var pnlType = new GroupBox
            {
                Text = "نوع التسوية",
                Location = new Point(20, y),
                Size = new Size(320, 70),
                ForeColor = Theme.TextMain
            };
            
            rdoDiscount = new RadioButton
            {
                Text = _isClient ? "خصم من العميل (تقليل مديونيته)" : "خصم من المورد (تقليل مستحقاته)",
                Location = new Point(10, 20),
                Size = new Size(300, 20),
                Checked = true,
                ForeColor = Theme.TextMain
            };
            
            rdoAddition = new RadioButton
            {
                Text = _isClient ? "إضافة للعميل (زيادة مديونيته)" : "إضافة للمورد (زيادة مستحقاته)",
                Location = new Point(10, 42),
                Size = new Size(300, 20),
                ForeColor = Theme.TextMain
            };

            pnlType.Controls.Add(rdoDiscount);
            pnlType.Controls.Add(rdoAddition);
            this.Controls.Add(pnlType);
            y += 85;

            // المبلغ
            this.Controls.Add(new Label { Text = "مبلغ التسوية (ج):", Location = new Point(20, y + 4), AutoSize = true, ForeColor = Theme.TextMain });
            nudAmount = new NumericUpDown
            {
                Location = new Point(170, y),
                Width = 170,
                Minimum = 0.01m,
                Maximum = 9999999,
                DecimalPlaces = 2,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            this.Controls.Add(nudAmount);
            y += 40;

            // ملاحظات
            this.Controls.Add(new Label { Text = "ملاحظات / البيان:", Location = new Point(20, y + 4), AutoSize = true, ForeColor = Theme.TextMain });
            txtNotes = new TextBox
            {
                Location = new Point(120, y),
                Width = 220,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                RightToLeft = RightToLeft.Yes
            };
            this.Controls.Add(txtNotes);
            y += 50;

            // أزرار
            btnOk = Theme.MakeButton("✅ تأكيد التسوية", 210, y, 130, 36, Theme.Accent);
            btnCancel = Theme.MakeButton("إلغاء", 100, y, 90, 36, Color.FromArgb(90, 90, 90));
            
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (nudAmount.Value <= 0)
            {
                MessageBox.Show("أدخل مبلغاً صحيحاً أكبر من صفر.");
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
                    ClientDAL.AddAdjustment(_id, nudAmount.Value, rdoDiscount.Checked, note);
                    MessageBox.Show("✅ تم تسجيل تسوية العميل بنجاح (دون التأثير على الخزينة).", "تمت التسوية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SupplierDAL.AddAdjustment(_id, nudAmount.Value, rdoDiscount.Checked, note);
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

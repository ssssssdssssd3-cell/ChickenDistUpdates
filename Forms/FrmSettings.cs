using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSettings : Form
    {
        private TextBox txtCompanyName;

        public FrmSettings()
        {
            this.Text = "إعدادات النظام";
            this.Size = new Size(500, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("⚙️ إعدادات النظام", "تعديل بيانات الشركة والإعدادات الأساسية");
            this.Controls.Add(pnlTop);

            var lblComp = new Label { Text = "اسم الشركة / المؤسسة:", Location = new Point(20, 80), AutoSize = true, ForeColor = Theme.TextMain };
            this.Controls.Add(lblComp);

            txtCompanyName = new TextBox { Location = new Point(20, 105), Width = 440, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12f) };
            txtCompanyName.Text = AppConfig.CompanyName;
            this.Controls.Add(txtCompanyName);

            var btnSave = Theme.MakeButton("💾 حفظ الإعدادات", 20, 160, 150, 40, Theme.Accent);
            btnSave.Click += (s, e) => {
                if(string.IsNullOrWhiteSpace(txtCompanyName.Text)) { MessageBox.Show("أدخل اسم الشركة"); return; }
                AppConfig.CompanyName = txtCompanyName.Text.Trim();
                MessageBox.Show("✅ تم حفظ الإعدادات بنجاح!\nقد تحتاج لإعادة فتح بعض الشاشات ليتم تحديث الاسم فيها.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                if (Application.OpenForms["FrmMain"] is FrmMain main)
                {
                    main.UpdateCompanyName(AppConfig.CompanyName);
                }
            };
            this.Controls.Add(btnSave);
        }
    }
}

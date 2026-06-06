using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmActivation : Form
    {
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblStatus;
        private TextBox txtMachineId;
        private TextBox txtHddSerial;
        private Button btnCopyMachine;
        private Button btnCopyHdd;
        private GroupBox grpActivation;
        private TextBox txtActivationCode;
        private Button btnActivate;
        private Button btnImportFile;
        private Button btnClose;

        public FrmActivation(string statusMessage)
        {
            InitializeComponent(statusMessage);
            LoadHardwareDetails();
        }

        private void InitializeComponent(string statusMessage)
        {
            this.SuspendLayout();

            this.Text = "🔑 تفعيل برنامج ChickenDist";
            this.Size = new Size(540, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Header Panel
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(30, 60, 114) };
            lblTitle = new Label
            {
                Text = "🔑  تفعيل رخصة برنامج ChickenDist",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Status Label
            lblStatus = new Label
            {
                Text = string.IsNullOrEmpty(statusMessage) ? "البرنامج غير مفعّل. يرجى إدخال كود التفعيل لتشغيل البرنامج." : statusMessage,
                Location = new Point(20, 80),
                Size = new Size(480, 40),
                ForeColor = Color.FromArgb(200, 50, 50),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblStatus);

            // GroupBox for Hardware Details
            var grpHardware = new GroupBox
            {
                Text = "💻  بيانات جهازك الحالي (أرسلها للمطور)",
                Location = new Point(20, 130),
                Size = new Size(480, 150),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            // Machine ID Row
            var lblMachine = new Label { Text = "معرّف الجهاز (Machine ID):", Location = new Point(15, 28), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            txtMachineId = new TextBox { Location = new Point(15, 50), Size = new Size(330, 25), ReadOnly = true, BackColor = Color.White, Font = new Font("Consolas", 10F), TextAlign = HorizontalAlignment.Center };
            btnCopyMachine = new Button { Text = "نسخ", Location = new Point(360, 48), Size = new Size(95, 28), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCopyMachine.Click += (s, e) => { Clipboard.SetText(txtMachineId.Text); MessageBox.Show("تم نسخ معرّف الجهاز بنجاح!", "نسخ", MessageBoxButtons.OK, MessageBoxIcon.Information); };
            
            grpHardware.Controls.Add(lblMachine);
            grpHardware.Controls.Add(txtMachineId);
            grpHardware.Controls.Add(btnCopyMachine);

            // HDD Serial Row
            var lblHdd = new Label { Text = "رقم القرص (HDD Serial):", Location = new Point(15, 88), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            txtHddSerial = new TextBox { Location = new Point(15, 110), Size = new Size(330, 25), ReadOnly = true, BackColor = Color.White, Font = new Font("Consolas", 10F), TextAlign = HorizontalAlignment.Center };
            btnCopyHdd = new Button { Text = "نسخ", Location = new Point(360, 108), Size = new Size(95, 28), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCopyHdd.Click += (s, e) => { Clipboard.SetText(txtHddSerial.Text); MessageBox.Show("تم نسخ رقم القرص بنجاح!", "نسخ", MessageBoxButtons.OK, MessageBoxIcon.Information); };

            grpHardware.Controls.Add(lblHdd);
            grpHardware.Controls.Add(txtHddSerial);
            grpHardware.Controls.Add(btnCopyHdd);

            this.Controls.Add(grpHardware);

            // GroupBox for Activation input
            grpActivation = new GroupBox
            {
                Text = "🔑  إدخال كود التفعيل",
                Location = new Point(20, 290),
                Size = new Size(480, 185),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            var lblCodeHint = new Label { Text = "الرجاء لصق كود التفعيل هنا:", Location = new Point(15, 25), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            grpActivation.Controls.Add(lblCodeHint);

            btnImportFile = new Button
            {
                Text = "📂 استيراد من ملف",
                Location = new Point(320, 20),
                Size = new Size(135, 28),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImportFile.Click += BtnImportFile_Click;
            grpActivation.Controls.Add(btnImportFile);

            txtActivationCode = new TextBox
            {
                Multiline = true,
                Location = new Point(15, 55),
                Size = new Size(440, 65),
                Font = new Font("Consolas", 9.5F),
                ScrollBars = ScrollBars.Vertical
            };
            grpActivation.Controls.Add(txtActivationCode);

            btnActivate = new Button
            {
                Text = "⚡  تفعيل البرنامج الآن",
                Location = new Point(15, 130),
                Size = new Size(440, 40),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnActivate.Click += BtnActivate_Click;
            grpActivation.Controls.Add(btnActivate);

            this.Controls.Add(grpActivation);

            // Close Button
            btnClose = new Button
            {
                Text = "خروج",
                Location = new Point(200, 490),
                Size = new Size(130, 35),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnClose);

            this.ResumeLayout(false);
        }

        private void LoadHardwareDetails()
        {
            txtMachineId.Text = LicenseManager.GetCurrentMachineId();
            txtHddSerial.Text = LicenseManager.GetCurrentHddSerial();
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            string code = txtActivationCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("الرجاء لصق كود التفعيل أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string errorReason;
            if (LicenseManager.ValidateLicense(code, out errorReason))
            {
                try
                {
                    // حفظ المفاتيح كـ AES base64 مباشر (بدون DPAPI) - متوافق مع ملفات أداة التفعيل
                    var parts = code.Split('|');
                    LicenseManager.WriteIniValue("General", "Key1", parts[0], encrypt: false);
                    LicenseManager.WriteIniValue("General", "Key2", parts[1], encrypt: false);
                    LicenseManager.WriteIniValue("General", "Key3", parts[2], encrypt: false);
                    LicenseManager.WriteIniValue("General", "Key4", parts[3], encrypt: false);
                    LicenseManager.WriteIniValue("General", "Key5", parts[4], encrypt: false);
                    LicenseManager.WriteIniValue("General", "Key6", parts[5], encrypt: false);

                    MessageBox.Show("✅ تم تفعيل البرنامج بنجاح! سيتم فتح البرنامج الآن.", "تم التفعيل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ أثناء حفظ الترخيص: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show($"❌ كود التفعيل غير صالح:\n{errorReason}", "خطأ التفعيل", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImportFile_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "ملفات التفعيل (*.license;*.txt;*.ini)|*.license;*.txt;*.ini|كل الملفات (*.*)|*.*";
                dlg.Title = "اختر ملف التفعيل أو الإعدادات Settings.ini";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string content = File.ReadAllText(dlg.FileName).Trim();
                        if (dlg.FileName.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                        {
                            string parsedLicense = ParseLicenseFromIni(dlg.FileName);
                            if (!string.IsNullOrEmpty(parsedLicense))
                            {
                                txtActivationCode.Text = parsedLicense;
                                MessageBox.Show("تم استيراد كود التفعيل بنجاح من ملف INI! اضغط على زر التفعيل لإتمام العملية.", "تم الاستيراد", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }
                        
                        txtActivationCode.Text = content;
                        MessageBox.Show("تم تحميل كود التفعيل بنجاح! اضغط على زر التفعيل لإتمام العملية.", "تم الاستيراد", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("حدث خطأ أثناء قراءة الملف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string ParseLicenseFromIni(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string k1 = "", k2 = "", k3 = "", k4 = "", k5 = "", k6 = "";
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("Key1=", StringComparison.OrdinalIgnoreCase)) k1 = trimmed.Substring(5).Trim();
                    if (trimmed.StartsWith("Key2=", StringComparison.OrdinalIgnoreCase)) k2 = trimmed.Substring(5).Trim();
                    if (trimmed.StartsWith("Key3=", StringComparison.OrdinalIgnoreCase)) k3 = trimmed.Substring(5).Trim();
                    if (trimmed.StartsWith("Key4=", StringComparison.OrdinalIgnoreCase)) k4 = trimmed.Substring(5).Trim();
                    if (trimmed.StartsWith("Key5=", StringComparison.OrdinalIgnoreCase)) k5 = trimmed.Substring(5).Trim();
                    if (trimmed.StartsWith("Key6=", StringComparison.OrdinalIgnoreCase)) k6 = trimmed.Substring(5).Trim();
                }
                
                // إزالة بادئة "ENC:" إن وجدت
                if (k1.StartsWith("ENC:", StringComparison.OrdinalIgnoreCase)) k1 = k1.Substring(4);
                if (k2.StartsWith("ENC:", StringComparison.OrdinalIgnoreCase)) k2 = k2.Substring(4);
                if (k3.StartsWith("ENC:", StringComparison.OrdinalIgnoreCase)) k3 = k3.Substring(4);
                if (k4.StartsWith("ENC:", StringComparison.OrdinalIgnoreCase)) k4 = k4.Substring(4);
                if (k5.StartsWith("ENC:", StringComparison.OrdinalIgnoreCase)) k5 = k5.Substring(4);
                if (k6.StartsWith("ENC:", StringComparison.OrdinalIgnoreCase)) k6 = k6.Substring(4);

                if (!string.IsNullOrEmpty(k1) && !string.IsNullOrEmpty(k6))
                {
                    return $"{k1}|{k2}|{k3}|{k4}|{k5}|{k6}";
                }
            }
            catch {}
            return null;
        }
    }
}

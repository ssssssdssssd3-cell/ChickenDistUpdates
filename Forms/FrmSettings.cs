using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO.Ports;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSettings : Form
    {
        private TextBox txtCompanyName;
        private CheckBox chkScaleEnabled;
        private ComboBox cboScalePort;
        private ComboBox cboScaleBaud;
        private CheckBox chkThermalEnabled;
        private ComboBox cboThermalPrinter;
        private ComboBox cboThermalWidth;
        
        private Label lblTestWeightResult;
        private Button btnTestScale;
        private Timer scaleTestTimer;

        public FrmSettings()
        {
            this.Text = "إعدادات النظام";
            // تصغير الارتفاع إذا كانت الشاشة صغيرة
            int settH = ScreenHelper.IsSmallScreen ? 500 : 550;
            this.Size = new Size(620, settH);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            var pnlTop = Theme.MakeTitleBar("⚙️ إعدادات النظام", "تعديل بيانات الشركة، وإعدادات الميزان والطابعة الحرارية");
            this.Controls.Add(pnlTop);

            // Tab Control
            TabControl tc = new TabControl
            {
                Location = new Point(20, 85),
                Size = new Size(545, 335),
                BackColor = Theme.BgCard,
                ForeColor = Theme.TextMain,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            // Tab 1: General Settings
            TabPage tpGeneral = new TabPage { Text = "البيانات العامة", BackColor = Theme.BgCard };
            var lblComp = new Label { Text = "اسم الشركة / المؤسسة:", Location = new Point(20, 30), AutoSize = true, ForeColor = Theme.TextMain };
            txtCompanyName = new TextBox { Location = new Point(20, 55), Width = 480, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12f) };
            txtCompanyName.Text = AppConfig.CompanyName;
            tpGeneral.Controls.Add(lblComp);
            tpGeneral.Controls.Add(txtCompanyName);
            tc.TabPages.Add(tpGeneral);

            // Tab 2: Scale Settings
            TabPage tpScale = new TabPage { Text = "إعدادات الميزان", BackColor = Theme.BgCard };
            
            chkScaleEnabled = new CheckBox 
            { 
                Text = "تفعيل قراءة الميزان الإلكتروني", 
                Location = new Point(20, 20), 
                AutoSize = true, 
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ScaleEnabled 
            };
            
            var lblPort = new Label { Text = "منفذ الاتصال (COM Port):", Location = new Point(20, 65), AutoSize = true, ForeColor = Theme.TextMain };
            cboScalePort = new ComboBox 
            { 
                Location = new Point(20, 90), 
                Width = 220, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                FlatStyle = FlatStyle.Flat 
            };
            
            // Fill ports
            try
            {
                cboScalePort.Items.Clear();
                foreach (string p in SerialPort.GetPortNames())
                {
                    cboScalePort.Items.Add(p);
                }
                if (cboScalePort.Items.Contains(AppConfig.ScaleComPort))
                    cboScalePort.SelectedItem = AppConfig.ScaleComPort;
                else if (cboScalePort.Items.Count > 0)
                    cboScalePort.SelectedIndex = 0;
            }
            catch { }

            var lblBaud = new Label { Text = "سرعة النقل (Baud Rate):", Location = new Point(265, 65), AutoSize = true, ForeColor = Theme.TextMain };
            cboScaleBaud = new ComboBox 
            { 
                Location = new Point(265, 90), 
                Width = 220, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                FlatStyle = FlatStyle.Flat 
            };
            cboScaleBaud.Items.AddRange(new object[] { "2400", "4800", "9600", "19200", "38400", "115200" });
            cboScaleBaud.SelectedItem = AppConfig.ScaleBaudRate.ToString();

            btnTestScale = Theme.MakeButton("🔌 اختبار قراءة الوزن", 20, 150, 180, 35, Theme.Primary);
            lblTestWeightResult = new Label 
            { 
                Text = "الوزن الحالي: ---", 
                Location = new Point(215, 158), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), 
                ForeColor = Theme.Accent 
            };

            btnTestScale.Click += BtnTestScale_Click;

            // Timer to show live weight during test
            scaleTestTimer = new Timer { Interval = 200 };
            scaleTestTimer.Tick += ScaleTestTimer_Tick;

            tpScale.Controls.AddRange(new Control[] { chkScaleEnabled, lblPort, cboScalePort, lblBaud, cboScaleBaud, btnTestScale, lblTestWeightResult });
            tc.TabPages.Add(tpScale);

            // Tab 3: Printer Settings
            TabPage tpPrinter = new TabPage { Text = "إعدادات الطابعة", BackColor = Theme.BgCard };
            
            chkThermalEnabled = new CheckBox 
            { 
                Text = "تفعيل الطباعة الحرارية المباشرة (بدون معاينة)", 
                Location = new Point(20, 20), 
                AutoSize = true, 
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ThermalPrinterEnabled 
            };

            var lblPrinter = new Label { Text = "اسم الطابعة الحرارية (Xprinter/Zebra):", Location = new Point(20, 65), AutoSize = true, ForeColor = Theme.TextMain };
            cboThermalPrinter = new ComboBox 
            { 
                Location = new Point(20, 90), 
                Width = 465, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                FlatStyle = FlatStyle.Flat 
            };

            // Fill printers
            try
            {
                cboThermalPrinter.Items.Clear();
                foreach (string prt in PrinterSettings.InstalledPrinters)
                {
                    cboThermalPrinter.Items.Add(prt);
                }
                if (cboThermalPrinter.Items.Contains(AppConfig.ThermalPrinterName))
                    cboThermalPrinter.SelectedItem = AppConfig.ThermalPrinterName;
                else if (cboThermalPrinter.Items.Count > 0)
                    cboThermalPrinter.SelectedIndex = 0;
            }
            catch { }

            var lblWidth = new Label { Text = "عرض ورق الطباعة:", Location = new Point(20, 140), AutoSize = true, ForeColor = Theme.TextMain };
            cboThermalWidth = new ComboBox 
            { 
                Location = new Point(20, 165), 
                Width = 220, 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Theme.BgInput, 
                ForeColor = Theme.TextMain, 
                FlatStyle = FlatStyle.Flat 
            };
            cboThermalWidth.Items.AddRange(new object[] { "80 مم", "58 مم" });
            cboThermalWidth.SelectedIndex = AppConfig.ThermalPaperWidth == 58 ? 1 : 0;

            var btnTestPrint = Theme.MakeButton("🖨️ طباعة فاتورة تجريبية", 20, 225, 200, 35, Theme.Success);
            btnTestPrint.Click += BtnTestPrint_Click;

            tpPrinter.Controls.AddRange(new Control[] { chkThermalEnabled, lblPrinter, cboThermalPrinter, lblWidth, cboThermalWidth, btnTestPrint });
            tc.TabPages.Add(tpPrinter);

            this.Controls.Add(tc);

            // Save settings button
            var btnSave = Theme.MakeButton("💾 حفظ الإعدادات", 20, 440, 160, 42, Theme.Accent);
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSave.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtCompanyName.Text)) { MessageBox.Show("أدخل اسم الشركة"); return; }
                
                // Save General Settings
                AppConfig.CompanyName = txtCompanyName.Text.Trim();

                // Save Scale Settings
                AppConfig.ScaleEnabled = chkScaleEnabled.Checked;
                if (cboScalePort.SelectedItem != null)
                    AppConfig.ScaleComPort = cboScalePort.SelectedItem.ToString();
                if (cboScaleBaud.SelectedItem != null)
                    AppConfig.ScaleBaudRate = int.Parse(cboScaleBaud.SelectedItem.ToString());

                // Save Printer Settings
                AppConfig.ThermalPrinterEnabled = chkThermalEnabled.Checked;
                if (cboThermalPrinter.SelectedItem != null)
                    AppConfig.ThermalPrinterName = cboThermalPrinter.SelectedItem.ToString();
                AppConfig.ThermalPaperWidth = cboThermalWidth.SelectedIndex == 1 ? 58 : 80;

                MessageBox.Show("✅ تم حفظ الإعدادات بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // If main form is open, update the title/company name
                if (Application.OpenForms["FrmMain"] is FrmMain main)
                {
                    main.UpdateCompanyName(AppConfig.CompanyName);
                }

                // If sale form is open, reset or reconnect the scale service
                if (chkScaleEnabled.Checked)
                {
                    ScaleService.Instance.Disconnect();
                    ScaleService.Instance.Connect(AppConfig.ScaleComPort, AppConfig.ScaleBaudRate);
                }
                else
                {
                    ScaleService.Instance.Disconnect();
                }

                this.Close();
            };
            this.Controls.Add(btnSave);

            // Apply theme RTL styles recursively
            Theme.ApplyRTL(this.Controls);
        }

        private void BtnTestScale_Click(object sender, EventArgs e)
        {
            if (scaleTestTimer.Enabled)
            {
                // Stop testing
                scaleTestTimer.Stop();
                ScaleService.Instance.Disconnect();
                btnTestScale.Text = "🔌 اختبار قراءة الوزن";
                btnTestScale.BackColor = Theme.Primary;
                lblTestWeightResult.Text = "الوزن الحالي: ---";
            }
            else
            {
                if (cboScalePort.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار منفذ COM أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string port = cboScalePort.SelectedItem.ToString();
                int baud = int.Parse(cboScaleBaud.SelectedItem.ToString());

                lblTestWeightResult.Text = "جاري الاتصال...";
                btnTestScale.Text = "⏹️ إيقاف الاختبار";
                btnTestScale.BackColor = Theme.Danger;

                if (ScaleService.Instance.Connect(port, baud))
                {
                    scaleTestTimer.Start();
                }
                else
                {
                    lblTestWeightResult.Text = "❌ فشل الاتصال!";
                    btnTestScale.Text = "🔌 اختبار قراءة الوزن";
                    btnTestScale.BackColor = Theme.Primary;
                }
            }
        }

        private void ScaleTestTimer_Tick(object sender, EventArgs e)
        {
            if (ScaleService.Instance.IsConnected)
            {
                decimal w = ScaleService.Instance.CurrentWeight;
                bool stable = ScaleService.Instance.IsStable;
                lblTestWeightResult.Text = $"الوزن الحالي: {w:F3} كجم {(stable ? "🟢" : "🟡")}";
            }
            else
            {
                lblTestWeightResult.Text = "❌ انقطع الاتصال!";
                scaleTestTimer.Stop();
                btnTestScale.Text = "🔌 اختبار قراءة الوزن";
                btnTestScale.BackColor = Theme.Primary;
            }
        }

        private void BtnTestPrint_Click(object sender, EventArgs e)
        {
            if (cboThermalPrinter.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار طابعة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string printerName = cboThermalPrinter.SelectedItem.ToString();
            int width = cboThermalWidth.SelectedIndex == 1 ? 58 : 80;

            try
            {
                var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics;
                    var titleFont = new Font("Arial", 12, FontStyle.Bold);
                    var normalFont = new Font("Arial", 9);
                    
                    int pageW = width == 58 ? 200 : 280; // approximate width in dots
                    int y = 10;
                    var center = new StringFormat { Alignment = StringAlignment.Center };
                    var right = new StringFormat { Alignment = StringAlignment.Far };

                    g.DrawString("فاتورة اختبار طابعة", titleFont, Brushes.Black, new RectangleF(0, y, pageW, 20), center);
                    y += 25;
                    g.DrawString($"الشركة: {AppConfig.CompanyName}", normalFont, Brushes.Black, new RectangleF(0, y, pageW, 18), right);
                    y += 20;
                    g.DrawString($"التاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}", normalFont, Brushes.Black, new RectangleF(0, y, pageW, 18), right);
                    y += 20;
                    g.DrawString("----------------------------------", normalFont, Brushes.Black, new RectangleF(0, y, pageW, 15), center);
                    y += 15;
                    g.DrawString("اختبار الطباعة الحرارية ناجح 100%", normalFont, Brushes.Black, new RectangleF(0, y, pageW, 20), center);
                    y += 20;
                    g.DrawString("----------------------------------", normalFont, Brushes.Black, new RectangleF(0, y, pageW, 15), center);
                };

                pd.Print();
                MessageBox.Show("✅ تم إرسال صفحة الاختبار للطابعة بنجاح!", "نجاح الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشلت الطباعة:\n{ex.Message}", "خطأ طباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (scaleTestTimer.Enabled)
            {
                scaleTestTimer.Stop();
                ScaleService.Instance.Disconnect();
            }
            base.OnFormClosing(e);
        }
    }
}

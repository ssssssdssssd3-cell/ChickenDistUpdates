using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة إعدادات الميزان الإلكتروني اللحظي وميزان الباركود
    /// </summary>
    public class FrmScaleSettings : Form
    {
        // ── الميزان الإلكتروني ──────────────────────────────────
        private CheckBox chkScaleEnabled;
        private ComboBox cboScalePort;
        private ComboBox cboScaleBaud;
        private Button btnTestScale;
        private Label lblTestWeightResult;

        // ── ميزان الباركود ──────────────────────────────────────
        private TextBox txtBarcodePrefix;
        private NumericUpDown nudCodeLen;
        private NumericUpDown nudWeightLen;
        private NumericUpDown nudDiv;

        public FrmScaleSettings()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "⚖️ إعدادات الموازين والأجهزة";
            this.Size = new Size(620, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTop = Theme.MakeTitleBar("⚖️ إعدادات الموازين والأجهزة", "إعدادات الميزان الإلكتروني المتصل بالكاشير وميزان طباعة ملصقات الباركود");
            this.Controls.Add(pnlTop);

            var pnlBody = new Panel
            {
                Location = new Point(15, 75),
                Size = new Size(575, 455),
                AutoScroll = true,
                BackColor = Theme.BgMain
            };
            this.Controls.Add(pnlBody);

            int y = 10;

            // ── 1. الميزان الإلكتروني (COM Port) ───────────────────
            var lblScaleTitle = new Label
            {
                Text = "⚖️ إعدادات الميزان الإلكتروني اللحظي (Serial COM Port):",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlBody.Controls.Add(lblScaleTitle);
            y += 28;

            chkScaleEnabled = new CheckBox
            {
                Text = "تفعيل قراءة الميزان الإلكتروني في شاشة الكاشير (POS)",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextMain,
                Checked = AppConfig.ScaleEnabled
            };
            pnlBody.Controls.Add(chkScaleEnabled);
            y += 32;

            AddLabel(pnlBody, "منفذ الاتصال (COM Port):", 15, y);
            AddLabel(pnlBody, "سرعة النقل (Baud Rate):", 290, y);
            y += 24;

            cboScalePort = new ComboBox
            {
                Location = new Point(15, y),
                Width = 255,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            foreach (string p in SerialPort.GetPortNames()) cboScalePort.Items.Add(p);
            if (cboScalePort.Items.Contains(AppConfig.ScaleComPort)) cboScalePort.SelectedItem = AppConfig.ScaleComPort;
            else if (cboScalePort.Items.Count > 0) cboScalePort.SelectedIndex = 0;
            pnlBody.Controls.Add(cboScalePort);

            cboScaleBaud = new ComboBox
            {
                Location = new Point(290, y),
                Width = 255,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboScaleBaud.Items.AddRange(new object[] { "2400", "4800", "9600", "19200", "38400", "115200" });
            cboScaleBaud.SelectedItem = AppConfig.ScaleBaudRate.ToString();
            if (cboScaleBaud.SelectedIndex == -1) cboScaleBaud.SelectedIndex = 2; // 9600
            pnlBody.Controls.Add(cboScaleBaud);
            y += 40;

            btnTestScale = Theme.MakeButton("⚖️ اختبار قراءة الميزان", 15, y, 190, 36, Theme.Primary);
            btnTestScale.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnTestScale.Click += BtnTestScale_Click;
            pnlBody.Controls.Add(btnTestScale);

            lblTestWeightResult = new Label
            {
                Location = new Point(220, y + 6),
                AutoSize = true,
                ForeColor = Theme.Success,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Text = ""
            };
            pnlBody.Controls.Add(lblTestWeightResult);
            y += 50;

            var sep = new Panel { Location = new Point(15, y), Size = new Size(530, 2), BackColor = Theme.BorderColor };
            pnlBody.Controls.Add(sep);
            y += 15;

            // ── 2. ميزان الباركود (الاستيكرات) ─────────────────────
            var lblBarcodeScaleTitle = new Label
            {
                Text = "🏷️ إعدادات ميزان الباركود الملصق (استيكرات الوزن):",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Accent
            };
            pnlBody.Controls.Add(lblBarcodeScaleTitle);
            y += 28;

            AddLabel(pnlBody, "بداية باركود الميزان (Prefix):", 15, y);
            AddLabel(pnlBody, "طول كود الصنف:", 290, y);
            y += 24;

            txtBarcodePrefix = new TextBox
            {
                Location = new Point(15, y),
                Width = 255,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f),
                Text = AppConfig.BarcodeScalePrefix
            };
            pnlBody.Controls.Add(txtBarcodePrefix);

            nudCodeLen = new NumericUpDown
            {
                Location = new Point(290, y),
                Width = 255,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Minimum = 1,
                Maximum = 10,
                Value = AppConfig.BarcodeScaleItemCodeLength
            };
            pnlBody.Controls.Add(nudCodeLen);
            y += 40;

            AddLabel(pnlBody, "طول الوزن في الباركود:", 15, y);
            AddLabel(pnlBody, "عامل القسمة للوزن (مثال 1000 للجرام):", 290, y);
            y += 24;

            nudWeightLen = new NumericUpDown
            {
                Location = new Point(15, y),
                Width = 255,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Minimum = 1,
                Maximum = 10,
                Value = AppConfig.BarcodeScaleWeightLength
            };
            pnlBody.Controls.Add(nudWeightLen);

            nudDiv = new NumericUpDown
            {
                Location = new Point(290, y),
                Width = 255,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Minimum = 1,
                Maximum = 10000,
                Value = AppConfig.BarcodeScaleDivideBy
            };
            pnlBody.Controls.Add(nudDiv);

            // ── شريط الأزرار السفلي ─────────────────────────────────
            var pnlFooter = new Panel
            {
                Location = new Point(0, 535),
                Size = new Size(620, 55),
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlFooter);

            var btnSave = Theme.MakeButton("💾 حفظ إعدادات الموازين", 15, 8, 210, 38, Theme.Primary);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;
            pnlFooter.Controls.Add(btnSave);

            var btnCancel = Theme.MakeButton("إغلاق", 235, 8, 100, 38, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) =>
            {
                if (ScaleService.Instance.IsConnected) ScaleService.Instance.Disconnect();
                this.Close();
            };
            pnlFooter.Controls.Add(btnCancel);

            this.FormClosing += (s, e) =>
            {
                if (ScaleService.Instance.IsConnected) ScaleService.Instance.Disconnect();
            };

            Theme.ApplyFormRTL(this);
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            parent.Controls.Add(lbl);
        }

        private void BtnTestScale_Click(object sender, EventArgs e)
        {
            if (ScaleService.Instance.IsConnected)
            {
                ScaleService.Instance.Disconnect();
                btnTestScale.Text = "⚖️ اختبار قراءة الميزان";
                btnTestScale.BackColor = Theme.Primary;
                lblTestWeightResult.Text = "";
            }
            else
            {
                if (cboScalePort.SelectedItem == null)
                {
                    MessageBox.Show("الرجاء اختيار منفذ الاتصال أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnTestScale.Text = "🛑 إيقاف الاختبار";
                btnTestScale.BackColor = Theme.Danger;
                lblTestWeightResult.Text = "جاري الاتصال بالميزان...";
                lblTestWeightResult.ForeColor = Theme.Accent;

                if (ScaleService.Instance.Connect(cboScalePort.SelectedItem.ToString(), int.Parse(cboScaleBaud.SelectedItem.ToString())))
                {
                    ScaleService.Instance.WeightChanged += (w, stable) =>
                    {
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            this.Invoke(new Action(() =>
                            {
                                lblTestWeightResult.ForeColor = stable ? Theme.Success : Color.FromArgb(245, 158, 11);
                                lblTestWeightResult.Text = $"الوزن الحي: {w:F3} كجم {(stable ? "✅ مستقر" : "⏳ غير مستقر")}";
                            }));
                        }
                    };
                }
                else
                {
                    lblTestWeightResult.ForeColor = Theme.Danger;
                    lblTestWeightResult.Text = "❌ فشل الاتصال بالميزان. تأكد من الكابل ورقم المنفذ.";
                    btnTestScale.Text = "⚖️ اختبار قراءة الميزان";
                    btnTestScale.BackColor = Theme.Primary;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            AppConfig.ScaleEnabled = chkScaleEnabled.Checked;
            if (cboScalePort.SelectedItem != null) AppConfig.ScaleComPort = cboScalePort.SelectedItem.ToString();
            if (cboScaleBaud.SelectedItem != null) AppConfig.ScaleBaudRate = int.Parse(cboScaleBaud.SelectedItem.ToString());

            AppConfig.BarcodeScalePrefix = txtBarcodePrefix.Text.Trim();
            AppConfig.BarcodeScaleItemCodeLength = (int)nudCodeLen.Value;
            AppConfig.BarcodeScaleWeightLength = (int)nudWeightLen.Value;
            AppConfig.BarcodeScaleDivideBy = nudDiv.Value;

            if (ScaleService.Instance.IsConnected) ScaleService.Instance.Disconnect();
            if (AppConfig.ScaleEnabled && !string.IsNullOrEmpty(AppConfig.ScaleComPort))
            {
                ScaleService.Instance.Connect(AppConfig.ScaleComPort, AppConfig.ScaleBaudRate);
            }

            MessageBox.Show("✅ تم حفظ إعدادات الموازين والأجهزة بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}

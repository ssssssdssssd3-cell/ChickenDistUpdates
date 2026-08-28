using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmClientUpdatePrompt : Form
    {
        private readonly string _currentVersion;
        private readonly string _serverVersion;
        private readonly string _serverMachine;
        private readonly DateTime _updatedAt;
        private readonly bool _hasBinaryInDb;

        private ProgressBar _progressBar;
        private Label _lblStatus;
        private Button _btnUpdate;
        private Button _btnBrowse;
        private Button _btnExit;
        private BackgroundWorker _worker;

        public FrmClientUpdatePrompt(string currentVersion, string serverVersion, string serverMachine, DateTime updatedAt, bool hasBinaryInDb)
        {
            _currentVersion = currentVersion;
            _serverVersion = serverVersion;
            _serverMachine = serverMachine;
            _updatedAt = updatedAt;
            _hasBinaryInDb = hasBinaryInDb;

            InitializeCustomUI();
        }

        public static bool ShowUpdateDialog(string currentVersion, string serverVersion, string serverMachine, DateTime updatedAt, bool hasBinaryInDb)
        {
            using (var frm = new FrmClientUpdatePrompt(currentVersion, serverVersion, serverMachine, updatedAt, hasBinaryInDb))
            {
                var dr = frm.ShowDialog();
                return dr == DialogResult.OK;
            }
        }

        private void InitializeCustomUI()
        {
            this.Text = "تحديث إصدار الجهاز الفرعي - ProSoft";
            this.Size = new Size(540, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // ── 1. Top Header Panel ──────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(30, 41, 59)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var br = new LinearGradientBrush(pnlHeader.ClientRectangle, Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(br, pnlHeader.ClientRectangle);
                }
            };

            var lblHeaderTitle = new Label
            {
                Text = "🚀 تحديث إصدار البرنامج من السيرفر الرئيسي",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(500, 30),
                Location = new Point(20, 14),
                BackColor = Color.Transparent
            };

            var lblHeaderSub = new Label
            {
                Text = $"يتوفر إصدار معتمد أحدث على السيرفر الرئيسي (v{_serverVersion}) يجب ترقية هذا الجهاز إليه",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = false,
                Size = new Size(500, 25),
                Location = new Point(20, 46),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);
            this.Controls.Add(pnlHeader);

            // ── 2. Information Card Panel ────────────────────────────────────
            var pnlCard = new Panel
            {
                Location = new Point(20, 100),
                Size = new Size(485, 130),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            pnlCard.Paint += (s, e) =>
            {
                using (var p = new Pen(Color.FromArgb(226, 232, 240), 1.5f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
                }
            };

            var lblOldVer = new Label
            {
                Text = $"🖥️ إصدار هذا الجهاز (قديم):  v{_currentVersion}",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(15, 12),
                Size = new Size(450, 24)
            };

            var lblNewVer = new Label
            {
                Text = $"🏢 إصدار السيرفر الرئيسي (المعتمد):  v{_serverVersion}",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Location = new Point(15, 40),
                Size = new Size(450, 24)
            };

            string serverInfo = !string.IsNullOrWhiteSpace(_serverMachine) ? $" | جهاز: {_serverMachine}" : "";
            var lblServerDetails = new Label
            {
                Text = $"🕒 تم الاعتماد: {_updatedAt:yyyy/MM/dd hh:mm tt}{serverInfo}",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(15, 68),
                Size = new Size(450, 22)
            };

            string sourceDesc = _hasBinaryInDb ? "⚡ النسخة متوفرة وجاهزة للتحميل الفوري عبر الشبكة المحلية (LAN)" : "🌐 سيتم التحميل التلقائي عبر خادم التحديثات المعتمد";
            var lblSource = new Label
            {
                Text = sourceDesc,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = _hasBinaryInDb ? Color.FromArgb(37, 99, 235) : Color.FromArgb(147, 51, 234),
                Location = new Point(15, 96),
                Size = new Size(450, 22)
            };

            pnlCard.Controls.Add(lblOldVer);
            pnlCard.Controls.Add(lblNewVer);
            pnlCard.Controls.Add(lblServerDetails);
            pnlCard.Controls.Add(lblSource);
            this.Controls.Add(pnlCard);

            // ── 3. Description Note ──────────────────────────────────────────
            var lblDesc = new Label
            {
                Text = "تم إيقاف تشغيل هذا الجهاز لحماية الفواتير والحسابات من التضارب.\nاضغط على زر التحديث أدناه لتحميل وتثبيت أحدث نسخة تلقائياً دون الحاجة لنقلها يدوياً.",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 238),
                Size = new Size(485, 40),
                TextAlign = ContentAlignment.TopRight
            };
            this.Controls.Add(lblDesc);

            // ── 4. Progress Area ─────────────────────────────────────────────
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 282),
                Size = new Size(485, 24),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visible = false
            };
            this.Controls.Add(_progressBar);

            _lblStatus = new Label
            {
                Text = "جاهز للتحميل والتثبيت",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 310),
                Size = new Size(485, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            this.Controls.Add(_lblStatus);

            // ── 5. Action Buttons ────────────────────────────────────
            _btnUpdate = new Button
            {
                Text = "📥 تحميل وتثبيت إصدار الرئيسي الآن (تلقائي)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 340),
                Size = new Size(485, 46),
                Cursor = Cursors.Hand
            };
            _btnUpdate.FlatAppearance.BorderSize = 0;
            _btnUpdate.Click += BtnUpdate_Click;
            this.Controls.Add(_btnUpdate);

            _btnBrowse = new Button
            {
                Text = "📁 أو اختيار ملف البرنامج المحدث يدوياً من الشبكة...",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 394),
                Size = new Size(330, 34),
                Cursor = Cursors.Hand
            };
            _btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(_btnBrowse);

            _btnExit = new Button
            {
                Text = "خروج",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(360, 394),
                Size = new Size(145, 34),
                Cursor = Cursors.Hand
            };
            _btnExit.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnExit.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(_btnExit);
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            _btnUpdate.Enabled = false;
            _btnBrowse.Enabled = false;
            _btnExit.Enabled = false;

            _progressBar.Visible = true;
            _lblStatus.Visible = true;
            _progressBar.Value = 10;
            _lblStatus.Text = "جاري الاتصال والبدء في التحميل...";

            _worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

            string updateError = "";
            bool updateOk = false;

            _worker.DoWork += (s, ev) =>
            {
                updateOk = UpdateManager.DownloadAndInstallClientUpdate(_serverVersion, (pct, status) =>
                {
                    _worker.ReportProgress(pct, status);
                }, out updateError);
            };

            _worker.ProgressChanged += (s, ev) =>
            {
                _progressBar.Value = Math.Min(Math.Max(ev.ProgressPercentage, 0), 100);
                if (ev.UserState is string msg)
                {
                    _lblStatus.Text = msg;
                }
            };

            _worker.RunWorkerCompleted += (s, ev) =>
            {
                if (!updateOk)
                {
                    _progressBar.Visible = false;
                    _lblStatus.Text = "❌ فشل التحميل";
                    _lblStatus.ForeColor = Color.Red;
                    _btnUpdate.Enabled = true;
                    _btnBrowse.Enabled = true;
                    _btnExit.Enabled = true;

                    MessageBox.Show(
                        $"تعذر تحميل وتثبيت التحديث تلقائياً:\n{updateError}\n\nيمكنك استخدام خيار (اختيار ملف البرنامج يدوياً) أو إعادة المحاولة.",
                        "فشل التحديث التلقائي",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else
                {
                    _lblStatus.Text = "✅ اكتمل التحديث! جاري إعادة التشغيل...";
                    _lblStatus.ForeColor = Color.Green;
                    this.DialogResult = DialogResult.OK;
                }
            };

            _worker.RunWorkerAsync();
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "اختر ملف البرنامج المحدث (ProSoft.exe)";
                ofd.Filter = "ملفات البرامج التنفيذية (*.exe)|*.exe";
                ofd.CheckFileExists = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(ofd.FileName);
                        if (fi.Length < 500000)
                        {
                            MessageBox.Show("الملف المحدد صغير جداً ولا يبدو أنه ملف البرنامج المعتمد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        var dr = MessageBox.Show(
                            $"هل تريد تثبيت الملف المحدد واستبدال نسخة هذا الجهاز فوراً؟\n\nالملف: {ofd.FileName}",
                            "تأكيد التثبيت",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (dr == DialogResult.Yes)
                        {
                            _btnUpdate.Enabled = false;
                            _btnBrowse.Enabled = false;
                            _btnExit.Enabled = false;
                            _lblStatus.Visible = true;
                            _lblStatus.Text = "جاري تثبيت الملف المختار وإعادة التشغيل...";

                            UpdateManager.ApplyAndReplaceExe(ofd.FileName);
                            this.DialogResult = DialogResult.OK;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل تثبيت الملف المحدد: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}

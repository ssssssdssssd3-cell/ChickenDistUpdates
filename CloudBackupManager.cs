using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace CloudBackupManager
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }
    }

    public class FrmMain : Form
    {
        private TextBox txtSearch;
        private DataGridView dgvBackups;
        private Button btnRefresh;
        private Button btnDownload;
        private ProgressBar progressBar;
        private Label lblStatus;
        private List<BackupItem> allItems = new List<BackupItem>();
        private string Bucket = "checkin-192ab.firebasestorage.app";

        public FrmMain()
        {
            LoadSettings();
            InitializeComponent();
            LoadData();
        }

        private void LoadSettings()
        {
            string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.ini");
            if (File.Exists(iniPath))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(iniPath, Encoding.UTF8))
                    {
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                            continue;
                        int idx = trimmed.IndexOf('=');
                        if (idx > 0)
                        {
                            string k = trimmed.Substring(0, idx).Trim();
                            string v = trimmed.Substring(idx + 1).Trim();
                            if (string.Equals(k, "FirebaseStorageBucket", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(v))
                            {
                                Bucket = v;
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "مدير النسخ الاحتياطية السحابية - Cloud Backup Manager";
            this.Size = new Size(950, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Title Panel
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(45, 45, 48) };
            Label lblTitle = new Label {
                Text = "☁️ لوحة تحكم وإدارة النسخ الاحتياطية السحابية للعملاء",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 173, 181),
                AutoSize = true,
                Location = new Point(15, 20)
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Control Panel
            Panel pnlControls = new Panel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(10) };
            
            Label lblSearch = new Label { Text = "بحث باسم العميل:", AutoSize = true, Location = new Point(15, 22) };
            txtSearch = new TextBox { 
                Location = new Point(130, 19), 
                Width = 280, 
                BackColor = Color.FromArgb(45, 45, 45), 
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            btnRefresh = new Button {
                Text = "🔄 تحديث القائمة",
                Location = new Point(430, 15),
                Size = new Size(140, 32),
                BackColor = Color.FromArgb(0, 173, 181),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += BtnRefresh_Click;

            btnDownload = new Button {
                Text = "📥 تحميل النسخة المحددة",
                Location = new Point(585, 15),
                Size = new Size(190, 32),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDownload.FlatAppearance.BorderSize = 0;
            btnDownload.Click += BtnDownload_Click;

            pnlControls.Controls.Add(lblSearch);
            pnlControls.Controls.Add(txtSearch);
            pnlControls.Controls.Add(btnRefresh);
            pnlControls.Controls.Add(btnDownload);
            this.Controls.Add(pnlControls);

            // Grid
            dgvBackups = new DataGridView {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Black,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None
            };
            
            dgvBackups.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
            dgvBackups.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvBackups.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvBackups.EnableHeadersVisualStyles = false;
            dgvBackups.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 173, 181);
            dgvBackups.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvBackups.Columns.Add("Company", "العميل / المحل");
            dgvBackups.Columns.Add("FileName", "اسم ملف الباكب");
            dgvBackups.Columns.Add("Size", "الحجم (ميغابايت)");
            dgvBackups.Columns.Add("Date", "تاريخ الرفع");
            
            this.Controls.Add(dgvBackups);

            // Status Strip
            StatusStrip statusStrip = new StatusStrip { BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, Height = 30 };
            lblStatus = new Label { Text = "جاهز.", AutoSize = true, ForeColor = Color.White };
            progressBar = new ProgressBar { Width = 250, Visible = false, Style = ProgressBarStyle.Continuous };
            
            statusStrip.Items.Add(new ToolStripControlHost(lblStatus));
            statusStrip.Items.Add(new ToolStripControlHost(progressBar));
            this.Controls.Add(statusStrip);
        }

        private void LoadData()
        {
            btnRefresh.Enabled = false;
            lblStatus.Text = "جاري الاتصال بالسحابة وجلب قائمة النسخ الاحتياطية...";
            
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) => {
                try
                {
                    string url = $"https://firebasestorage.googleapis.com/v0/b/{Bucket}/o?prefix=backups/";
                    using (var client = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        // Ensure Tls12 is enabled for secure connection to Google APIs
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        string json = client.DownloadString(url);
                        e.Result = ParseJson(json);
                    }
                }
                catch (Exception ex)
                {
                    e.Result = ex;
                }
            };

            worker.RunWorkerCompleted += (s, e) => {
                btnRefresh.Enabled = true;
                if (e.Result is Exception ex)
                {
                    lblStatus.Text = "فشل الاتصال بالسحابة.";
                    MessageBox.Show("خطأ أثناء جلب البيانات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Result is List<BackupItem> list)
                {
                    allItems = list;
                    FilterList();
                    lblStatus.Text = $"تم جلب القائمة بنجاح. إجمالي النسخ: {allItems.Count}";
                }
            };

            worker.RunWorkerAsync();
        }

        private List<BackupItem> ParseJson(string json)
        {
            var list = new List<BackupItem>();
            int itemsIdx = json.IndexOf("\"items\":");
            if (itemsIdx == -1) return list;

            int idx = itemsIdx;
            while (true)
            {
                idx = json.IndexOf("{", idx + 1);
                if (idx == -1) break;
                
                string itemBlock = GetBlock(json, idx);
                if (string.IsNullOrEmpty(itemBlock)) break;
                
                string name = GetVal(itemBlock, "name");
                string sizeStr = GetVal(itemBlock, "size");
                string updated = GetVal(itemBlock, "updated");

                if (!string.IsNullOrEmpty(name) && name.StartsWith("backups/") && !name.EndsWith("test.txt"))
                {
                    string[] parts = name.Split('/');
                    if (parts.Length >= 3)
                    {
                        string company = parts[1].Replace("_", " ");
                        string filename = parts[2];
                        
                        long bytes = 0;
                        long.TryParse(sizeStr, out bytes);
                        double mb = Math.Round((double)bytes / (1024 * 1024), 2);

                        DateTime date = DateTime.MinValue;
                        DateTime.TryParse(updated, out date);

                        list.Add(new BackupItem {
                            Company = company,
                            FileName = filename,
                            FullPath = name,
                            SizeMB = mb,
                            Date = date.ToLocalTime()
                        });
                    }
                }
                idx += itemBlock.Length;
            }

            list.Sort((a, b) => b.Date.CompareTo(a.Date));
            return list;
        }

        private string GetBlock(string json, int startIdx)
        {
            int braces = 0;
            for (int i = startIdx; i < json.Length; i++)
            {
                if (json[i] == '{') braces++;
                else if (json[i] == '}')
                {
                    braces--;
                    if (braces == 0)
                    {
                        return json.Substring(startIdx, i - startIdx + 1);
                    }
                }
            }
            return "";
        }

        private string GetVal(string block, string key)
        {
            int kIdx = block.IndexOf("\"" + key + "\":");
            if (kIdx == -1) return "";
            int vStart = block.IndexOf(":", kIdx) + 1;
            
            int strStart = block.IndexOf("\"", vStart);
            if (strStart != -1 && strStart - vStart < 3)
            {
                int strEnd = block.IndexOf("\"", strStart + 1);
                return block.Substring(strStart + 1, strEnd - strStart - 1);
            }
            else
            {
                int commaIdx = block.IndexOf(",", vStart);
                if (commaIdx == -1) commaIdx = block.IndexOf("}", vStart);
                if (commaIdx != -1)
                {
                    return block.Substring(vStart, commaIdx - vStart).Trim();
                }
            }
            return "";
        }

        private void FilterList()
        {
            dgvBackups.Rows.Clear();
            string filter = txtSearch.Text.Trim().ToLower();

            foreach (var item in allItems)
            {
                if (string.IsNullOrEmpty(filter) || 
                    item.Company.ToLower().Contains(filter) || 
                    item.FileName.ToLower().Contains(filter))
                {
                    dgvBackups.Rows.Add(item.Company, item.FileName, item.SizeMB + " MB", item.Date.ToString("yyyy-MM-dd hh:mm tt"));
                    dgvBackups.Rows[dgvBackups.Rows.Count - 1].Tag = item;
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            FilterList();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void BtnDownload_Click(object sender, EventArgs e)
        {
            if (dgvBackups.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار نسخة احتياطية من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var item = dgvBackups.SelectedRows[0].Tag as BackupItem;
            if (item == null) return;

            SaveFileDialog sfd = new SaveFileDialog {
                FileName = item.FileName,
                Filter = "ZIP file (*.zip)|*.zip",
                Title = "حفظ النسخة الاحتياطية"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string savePath = sfd.FileName;
                btnDownload.Enabled = false;
                btnRefresh.Enabled = false;
                progressBar.Visible = true;
                progressBar.Value = 0;
                lblStatus.Text = "جاري تحميل الملف...";

                using (var webClient = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    string encodedPath = Uri.EscapeDataString(item.FullPath);
                    string downloadUrl = $"https://firebasestorage.googleapis.com/v0/b/{Bucket}/o/{encodedPath}?alt=media";

                    webClient.DownloadProgressChanged += (s, ev) => {
                        progressBar.Value = ev.ProgressPercentage;
                        lblStatus.Text = $"جاري تحميل الملف... ({ev.ProgressPercentage}%)";
                    };

                    webClient.DownloadFileCompleted += (s, ev) => {
                        btnDownload.Enabled = true;
                        btnRefresh.Enabled = true;
                        progressBar.Visible = false;

                        if (ev.Error != null)
                        {
                            lblStatus.Text = "فشل التحميل.";
                            MessageBox.Show("خطأ أثناء تحميل الملف:\n" + ev.Error.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            lblStatus.Text = "اكتمل التحميل بنجاح.";
                            MessageBox.Show("تم تحميل النسخة الاحتياطية للعميل وحفظها بنجاح! ✅\n\nالمسار:\n" + savePath, "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    };

                    webClient.DownloadFileAsync(new Uri(downloadUrl), savePath);
                }
            }
        }
    }

    public class BackupItem
    {
        public string Company { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public double SizeMB { get; set; }
        public DateTime Date { get; set; }
    }
}

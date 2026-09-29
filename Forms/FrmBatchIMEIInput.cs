using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة إدخال ومسح سيريالات الأجهزة (IMEI) السريعة بنشاط المحمول والأجهزة
    /// تدعم القراءة المتتابعة بجهاز الاسكنر، كشف التكرار اللحظي، واللصق الجماعي من إكسيل
    /// </summary>
    public class FrmBatchIMEIInput : Form
    {
        private readonly string _productName;
        private readonly int _targetQty;
        private readonly int _productID;
        private readonly int? _currentPurchaseID;

        private Label lblHeaderTitle;
        private Label lblCounterBadge;
        private TextBox txtScan;
        private DataGridView dgSerials;
        private Button btnPaste;
        private Button btnClear;
        private Button btnApply;
        private Button btnCancel;

        public List<string> ResultSerials { get; private set; } = new List<string>();

        public FrmBatchIMEIInput(string productName, int targetQty, List<string> existingSerials = null, int productID = 0, int? currentPurchaseID = null)
        {
            _productName = productName ?? "الصنف";
            _targetQty = targetQty > 0 ? targetQty : 1;
            _productID = productID;
            _currentPurchaseID = currentPurchaseID;

            if (existingSerials != null)
            {
                foreach (var s in existingSerials)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        ResultSerials.Add(s.Trim());
                }
            }

            InitializeComponent();
            RefreshGrid();
        }

        private void InitializeComponent()
        {
            this.Text = "📱 مسح وإدخال سيريالات الأجهزة (IMEI)";
            this.Size = new Size(580, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Segoe UI", 9.5f);
            this.BackColor = Color.FromArgb(248, 250, 252);

            // ── رأس النافذة ─────────────────────────────────────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(24, 34, 53),
                Padding = new Padding(15, 10, 15, 10)
            };

            lblHeaderTitle = new Label
            {
                Text = $"📦 الصنف: {_productName}",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                Location = new Point(15, 12),
                AutoSize = true
            };

            lblCounterBadge = new Label
            {
                Text = $"الممسوح: {ResultSerials.Count} من أصل {_targetQty}",
                ForeColor = Color.FromArgb(254, 240, 138),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(15, 45),
                AutoSize = true
            };

            pnlTop.Controls.Add(lblHeaderTitle);
            pnlTop.Controls.Add(lblCounterBadge);

            // ── لوحة الإدخال السريع بالاسكنر ───────────────────────────────────────
            var pnlInput = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.White
            };

            var lblPrompt = new Label
            {
                Text = "🔍 امسح السيريال بجهاز الاسكنر (IMEI) أو اكتبه واضغط Enter:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(15, 8),
                AutoSize = true
            };

            txtScan = new TextBox
            {
                Font = new Font("Consolas", 13f, FontStyle.Bold),
                Location = new Point(15, 33),
                Size = new Size(390, 32),
                BackColor = Color.FromArgb(240, 249, 255),
                ForeColor = Color.FromArgb(3, 105, 161)
            };
            txtScan.KeyDown += TxtScan_KeyDown;

            var btnAddManual = new Button
            {
                Text = "➕ إضافة",
                Location = new Point(415, 32),
                Size = new Size(70, 34),
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddManual.FlatAppearance.BorderSize = 0;
            btnAddManual.Click += (s, e) => AddCurrentScanText();

            btnPaste = new Button
            {
                Text = "📋 لصق",
                Location = new Point(490, 32),
                Size = new Size(65, 34),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPaste.FlatAppearance.BorderSize = 0;
            btnPaste.Click += BtnPaste_Click;

            pnlInput.Controls.Add(lblPrompt);
            pnlInput.Controls.Add(txtScan);
            pnlInput.Controls.Add(btnAddManual);
            pnlInput.Controls.Add(btnPaste);

            // ── لوحة الأزرار السفلية ───────────────────────────────────────────────
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.White
            };

            btnApply = new Button
            {
                Text = "✅ تطبيق واعتماد السيريالات",
                Dock = DockStyle.Right,
                Width = 200,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;

            btnClear = new Button
            {
                Text = "🗑️ مسح الكل",
                Dock = DockStyle.Left,
                Width = 100,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnClear.Click += (s, e) =>
            {
                if (ResultSerials.Count > 0 && MessageBox.Show("هل أنت متأكد من مسح جميع السيريالات المدخلة؟", "تأكيد المسح", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ResultSerials.Clear();
                    RefreshGrid();
                    txtScan.Focus();
                }
            };

            btnCancel = new Button
            {
                Text = "إلغاء",
                Dock = DockStyle.Right,
                Width = 90,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            pnlBottom.Controls.Add(btnClear);
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnApply);

            // ── جدول عرض السيريالات ───────────────────────────────────────────────
            dgSerials = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 10f),
                RowTemplate = { Height = 34 }
            };

            dgSerials.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "#", FillWeight = 15, ReadOnly = true });
            dgSerials.Columns.Add(new DataGridViewTextBoxColumn { Name = "IMEI", HeaderText = "رقم السيريال (IMEI)", FillWeight = 65, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Consolas", 11f, FontStyle.Bold) } });
            
            var delCol = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "إجراء",
                Text = "❌ حذف",
                UseColumnTextForButtonValue = true,
                FillWeight = 20
            };
            delCol.DefaultCellStyle.ForeColor = Color.Red;
            dgSerials.Columns.Add(delCol);

            dgSerials.CellClick += DgSerials_CellClick;

            // تركيب العناصر
            this.Controls.Add(dgSerials);
            this.Controls.Add(pnlInput);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);

            this.Shown += (s, e) => txtScan.Focus();
        }

        private void TxtScan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                AddCurrentScanText();
            }
        }

        private void AddCurrentScanText()
        {
            string raw = txtScan.Text?.Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            // إذا كان يحتوي على فواصل أو أسطر متعددة (مثل لصق سريع في الخانة)
            if (raw.Contains("\n") || raw.Contains(",") || raw.Contains(";"))
            {
                AddMultipleSerials(raw);
                txtScan.Clear();
                return;
            }

            if (AddSingleSerial(raw))
            {
                try { SystemSounds.Asterisk.Play(); } catch { }
                txtScan.Clear();
                txtScan.Focus();

                // إذا اكتمل العدد المطلوب نعطي تنبيهاً إيجابياً
                if (ResultSerials.Count == _targetQty)
                {
                    lblCounterBadge.ForeColor = Color.FromArgb(74, 222, 128); // Green
                }
            }
        }

        private bool AddSingleSerial(string imei)
        {
            imei = imei.Trim();
            if (string.IsNullOrWhiteSpace(imei)) return false;

            // 1. فحص التكرار في القائمة الحالية
            if (ResultSerials.Any(s => string.Equals(s, imei, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"⚠️ السيريال [{imei}] موجود بالفعل في القائمة المدخلة!", "تكرار سيريال", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtScan.SelectAll();
                return false;
            }

            // 2. فحص هل هو مسجل مسبقاً في قاعدة البيانات
            if (PurchaseDAL.IsIMEIAlreadyExists(imei, _currentPurchaseID))
            {
                var choice = MessageBox.Show($"⚠️ تنبيه: السيريال [{imei}] مسجل بالفعل في فاتورة مشتريات سابقة بالنظام!\nهل أنت متأكد من تكرار إدخاله؟", "سيريال مسجل مسبقاً", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (choice != DialogResult.Yes)
                {
                    txtScan.SelectAll();
                    return false;
                }
            }

            ResultSerials.Add(imei);
            RefreshGrid();
            return true;
        }

        private void AddMultipleSerials(string raw)
        {
            var parts = raw.Split(new[] { '\r', '\n', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;
            int dupCount = 0;

            foreach (var p in parts)
            {
                string s = p.Trim();
                if (string.IsNullOrWhiteSpace(s)) continue;

                if (ResultSerials.Any(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)))
                {
                    dupCount++;
                    continue;
                }

                ResultSerials.Add(s);
                addedCount++;
            }

            RefreshGrid();
            string msg = $"تمت إضافة {addedCount} سيريال بنجاح.";
            if (dupCount > 0) msg += $"\nتم تجاهل {dupCount} سيريال مكرر.";
            MessageBox.Show(msg, "نتيجة اللصق", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnPaste_Click(object sender, EventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string clip = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(clip))
                    {
                        AddMultipleSerials(clip);
                    }
                }
                else
                {
                    MessageBox.Show("لا يوجد نص منسوخ في الحافظة للصقه.", "الحافظة فارغة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء قراءة الحافظة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgSerials_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgSerials.Columns[e.ColumnIndex].Name == "Delete")
            {
                if (e.RowIndex < ResultSerials.Count)
                {
                    ResultSerials.RemoveAt(e.RowIndex);
                    RefreshGrid();
                    txtScan.Focus();
                }
            }
        }

        private void RefreshGrid()
        {
            dgSerials.Rows.Clear();
            for (int i = 0; i < ResultSerials.Count; i++)
            {
                dgSerials.Rows.Add(i + 1, ResultSerials[i]);
            }

            lblCounterBadge.Text = $"الممسوح: {ResultSerials.Count} من أصل {_targetQty}";
            if (ResultSerials.Count >= _targetQty)
            {
                lblCounterBadge.ForeColor = Color.FromArgb(74, 222, 128); // Green
            }
            else
            {
                lblCounterBadge.ForeColor = Color.FromArgb(254, 240, 138); // Yellow
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (ResultSerials.Count == 0)
            {
                if (MessageBox.Show("لم تقم بإدخال أي سيريال. هل تريد الخروج دون تطبيق أي سيريال؟", "تنبيه", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
                return;
            }

            if (ResultSerials.Count < _targetQty)
            {
                var res = MessageBox.Show($"الكمية المحددة في الفاتورة ({_targetQty}) أكبر من عدد السيريالات الممسوحة ({ResultSerials.Count}).\nهل تريد تطبيق الـ {ResultSerials.Count} سيريال واعتمادها؟", "تأكيد النواقص", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

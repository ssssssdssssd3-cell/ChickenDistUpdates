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
        private Label lblScanStatus;
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
            this.Size = new Size(670, 590);
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
                Height = 105,
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
                Size = new Size(330, 32),
                BackColor = Color.FromArgb(240, 249, 255),
                ForeColor = Color.FromArgb(3, 105, 161)
            };
            txtScan.KeyDown += TxtScan_KeyDown;

            var btnAddManual = new Button
            {
                Text = "➕ إضافة",
                Location = new Point(352, 32),
                Size = new Size(65, 34),
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
                Location = new Point(422, 32),
                Size = new Size(60, 34),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPaste.FlatAppearance.BorderSize = 0;
            btnPaste.Click += BtnPaste_Click;

            var btnScannerConfig = new Button
            {
                Text = "⚙️ برمجة الاسكانر لـ IMEI",
                Location = new Point(488, 32),
                Size = new Size(155, 34),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnScannerConfig.FlatAppearance.BorderSize = 0;
            btnScannerConfig.Click += (s, e) =>
            {
                using (var dlg = new FrmScannerConfigGuide())
                {
                    dlg.ShowDialog(this);
                }
            };

            lblScanStatus = new Label
            {
                Text = "💡 فلترة ذكية نشطة: يقبل كود IMEI 1 (15 رقم) ويتجاوز الباركود الدولي و S/N تلقائياً وبصمت.",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(15, 75),
                AutoSize = true
            };

            pnlInput.Controls.Add(lblPrompt);
            pnlInput.Controls.Add(txtScan);
            pnlInput.Controls.Add(btnAddManual);
            pnlInput.Controls.Add(btnPaste);
            pnlInput.Controls.Add(btnScannerConfig);
            pnlInput.Controls.Add(lblScanStatus);

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
                Width = 190,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;

            var btnPrint = new Button
            {
                Text = "🖨️ طباعة ملصقات الـ IMEI",
                Dock = DockStyle.Left,
                Width = 185,
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) =>
            {
                if (ResultSerials.Count == 0)
                {
                    MessageBox.Show("يرجى إدخال أو مسح سيريال واحد على الأقل للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    using (var dlg = new FrmPrintProductBarcode(_productID, _productName, ResultSerials))
                    {
                        dlg.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء فتح شاشة طباعة الباركود:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnClear = new Button
            {
                Text = "🗑️ مسح الكل",
                Dock = DockStyle.Left,
                Width = 90,
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
                Width = 80,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            pnlBottom.Controls.Add(btnPrint);
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

            if (!CleanAndExtractIMEI(raw, out string imei, out string statusMessage, out Color statusColor, out bool isIgnoredNonIMEI))
            {
                lblScanStatus.Text = statusMessage;
                lblScanStatus.ForeColor = statusColor;
                // لا نصدر صوت خطأ منفر عند التقاط الكود الدولي أو كود S/N، بل نتجاهلهما بصمت
                if (!isIgnoredNonIMEI)
                {
                    try { SystemSounds.Beep.Play(); } catch { }
                }
                txtScan.Clear();
                txtScan.Focus();
                return;
            }

            if (AddSingleSerial(imei))
            {
                lblScanStatus.Text = $"✅ تم تسجيل IMEI 1: [{imei}] بنجاح";
                lblScanStatus.ForeColor = Color.FromArgb(16, 185, 129); // Green
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

        private bool CleanAndExtractIMEI(string raw, out string validIMEI, out string statusMessage, out Color statusColor, out bool isIgnoredNonIMEI)
        {
            validIMEI = null;
            statusMessage = null;
            statusColor = Color.FromArgb(239, 68, 68);
            isIgnoredNonIMEI = false;

            if (string.IsNullOrWhiteSpace(raw)) return false;

            // 0. فحص ذكي مسبق: هل يحتوي النص الممسوح على كود IMEI صريح بطول 15 رقماً؟
            // أحياناً يرسل الاسكانر الكود الدولي متبوعاً بكود IMEI في تدفق سريع واحد
            var imeiRegexMatch = System.Text.RegularExpressions.Regex.Match(raw, @"(86\d{13}|35\d{13}|\b\d{15}\b)");
            string s = imeiRegexMatch.Success ? imeiRegexMatch.Value : PurchaseDAL.SanitizeIMEIString(raw);

            // 1. فحص هل يحتوي على حروف غير رقمية أو رموز (مثل لوحة مفاتيح عربية أو باركود S/N)
            if (string.IsNullOrEmpty(s) || !s.All(char.IsDigit))
            {
                isIgnoredNonIMEI = true;
                statusMessage = $"⚡ تم تجاوز كود السيريال ({raw}) تلقائياً — يرجى تمرير الاسكانر على باركود IMEI 1";
                statusColor = Color.FromArgb(2, 132, 199); // Soft Blue
                return false;
            }

            // 2. فحص الباركود الدولي EAN-13 (12 أو 13 رقماً مثل 6936520877331)
            if (s.Length == 12 || s.Length == 13)
            {
                isIgnoredNonIMEI = true;
                statusMessage = $"⚡ تم تجاوز الباركود الدولي ({s}) تلقائياً — يرجى تمرير الاسكانر على باركود IMEI 1";
                statusColor = Color.FromArgb(2, 132, 199); // Soft Blue
                return false;
            }

            // 3. فحص الأكواد القصيرة (مثل سيريال المصنع S/N الأقصر من 14 رقماً)
            if (s.Length < 14)
            {
                isIgnoredNonIMEI = true;
                statusMessage = $"⚡ تم تجاوز كود قصير ({s}) — بانتظار مسح باركود IMEI 1 المكون من 15 رقماً";
                statusColor = Color.FromArgb(2, 132, 199);
                return false;
            }

            // 4. فحص الأكواد الطويلة غير المعيارية
            if (s.Length > 16)
            {
                isIgnoredNonIMEI = true;
                statusMessage = $"⚡ تم تجاوز كود طويل ({s.Length} رقم) — بانتظار مسح باركود IMEI 1 المكون من 15 رقماً";
                statusColor = Color.FromArgb(2, 132, 199);
                return false;
            }

            if (s.Length == 14)
            {
                int chk = PurchaseDAL.CalculateLuhnCheckDigit(s);
                if (chk >= 0) s += chk.ToString();
            }
            else if (s.Length == 16)
            {
                s = s.Substring(0, 15);
            }

            // 5. فحص ما إذا كان هذا الكود هو IMEI 2 لجهاز مسجل مسبقاً في القائمة (نفس الهاتف):
            if (ResultSerials.Any(existing => PurchaseDAL.GetCandidatePairedIMEIs(existing).Contains(s)))
            {
                isIgnoredNonIMEI = true;
                statusMessage = $"⚡ تم تجاهل IMEI 2 لنفس الهاتف الممسوح مسبقاً";
                statusColor = Color.FromArgb(202, 138, 4); // Yellow/Gold
                return false;
            }

            // إذا كان هذا كود IMEI 2 (الرقم الـ 14 زوجي)، نحاول تحويله تلقائياً إلى IMEI 1 المطابق
            // في هواتف شاومي/ريدمي وغيرها IMEI 1 يكون فردياً و IMEI 2 زوجياً
            int digit14 = s[13] - '0';
            if (digit14 % 2 == 0) // زوجي -> على الأرجح IMEI 2
            {
                string base13 = s.Substring(0, 13);
                int target14 = digit14 + 1;
                if (target14 <= 9)
                {
                    string cand14 = base13 + target14.ToString();
                    int chk = PurchaseDAL.CalculateLuhnCheckDigit(cand14);
                    if (chk >= 0)
                    {
                        string candidateIMEI1 = cand14 + chk.ToString();
                        // إذا كان IMEI 1 المقابل موجود بالفعل نتجاهل
                        if (ResultSerials.Contains(candidateIMEI1))
                        {
                            isIgnoredNonIMEI = true;
                            statusMessage = $"⚡ تم تجاهل IMEI 2 لنفس الهاتف المسجل بالفعل ({candidateIMEI1})";
                            statusColor = Color.FromArgb(202, 138, 4);
                            return false;
                        }
                        // تحويل تلقائي إلى IMEI 1
                        s = candidateIMEI1;
                    }
                }
            }

            // 6. فحص التكرار المباشر في القائمة
            if (ResultSerials.Any(existing => string.Equals(existing, s, StringComparison.OrdinalIgnoreCase)))
            {
                statusMessage = $"⚠️ السيريال [{s}] مسجل بالفعل في هذه القائمة";
                statusColor = Color.FromArgb(202, 138, 4);
                return false;
            }

            validIMEI = s;
            return true;
        }

        private bool AddSingleSerial(string imei)
        {
            imei = imei.Trim();
            if (string.IsNullOrWhiteSpace(imei)) return false;

            // فحص هل هو مسجل مسبقاً في قاعدة البيانات
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
            int ignoredCount = 0;

            foreach (var p in parts)
            {
                if (CleanAndExtractIMEI(p, out string imei, out _, out _, out _))
                {
                    ResultSerials.Add(imei);
                    addedCount++;
                }
                else
                {
                    ignoredCount++;
                }
            }

            RefreshGrid();
            string msg = $"تمت إضافة {addedCount} سيريال بنجاح.";
            if (ignoredCount > 0) msg += $"\nتم تجاهل {ignoredCount} كود غير صالح أو مكرر أو باركود دولي.";
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

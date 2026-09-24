using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة إدارة بونات الخصم — إنشاء / تعديل / حذف بونات صالحة لشاشات البيع والموقع الإلكتروني
    /// </summary>
    public class FrmDiscountVouchers : Form
    {
        // ── Grid ──
        private DataGridView dgvVouchers;

        // ── Edit Panel ──
        private TextBox txtCode;
        private TextBox txtDescription;
        private ComboBox cboDiscountType;
        private NumericUpDown nudDiscountValue;
        private NumericUpDown nudMaxUses;
        private CheckBox chkUnlimitedUses;
        private DateTimePicker dtpExpiryDate;
        private CheckBox chkNoExpiry;
        private CheckBox chkIsActive;
        private Button btnSave;
        private Button btnNew;
        private Button btnDelete;
        private Button btnClose;
        private Label lblStatus;

        private int _editingVoucherID = 0;

        public FrmDiscountVouchers()
        {
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            this.Text = "🎟️ إدارة بونات الخصم";
            this.Size = new Size(1000, 600);
            this.MinimumSize = new Size(860, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5f);

            // ── Header ──
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(109, 40, 217)
            };
            var lblTitle = new Label
            {
                Text = "🎟️ إدارة بونات الخصم — Discount Vouchers",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 16, 0)
            };
            pnlHeader.Controls.Add(lblTitle);

            // ── Main Split ──
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 580,
                Panel1MinSize = 400,
                Panel2MinSize = 280,
                BorderStyle = BorderStyle.None
            };

            // ── Left: Grid ──
            dgvVouchers = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 34
            };
            dgvVouchers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvVouchers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(109, 40, 217);
            dgvVouchers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvVouchers.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(109, 40, 217);
            dgvVouchers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 246, 255);
            dgvVouchers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(196, 181, 253);
            dgvVouchers.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 20, 60);
            dgvVouchers.SelectionChanged += DgvVouchers_SelectionChanged;

            // أزرار تحت الجدول
            var pnlGridButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(6, 4, 6, 4),
                BackColor = Color.FromArgb(237, 233, 254)
            };
            btnNew = CreateButton("➕ بون جديد", Color.FromArgb(109, 40, 217));
            btnNew.Click += (s, e) => ClearForm();

            btnDelete = CreateButton("🗑️ حذف", Color.FromArgb(239, 68, 68));
            btnDelete.Click += BtnDelete_Click;

            btnClose = CreateButton("✖ إغلاق", Color.FromArgb(71, 85, 105));
            btnClose.Click += (s, e) => this.Close();

            pnlGridButtons.Controls.Add(btnClose);
            pnlGridButtons.Controls.Add(btnDelete);
            pnlGridButtons.Controls.Add(btnNew);

            var pnlLeft = new Panel { Dock = DockStyle.Fill };
            pnlLeft.Controls.Add(dgvVouchers);
            pnlLeft.Controls.Add(pnlGridButtons);
            split.Panel1.Controls.Add(pnlLeft);

            // ── Right: Edit Form ──
            var pnlEdit = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14, 12, 14, 12)
            };

            int y = 16;
            int labelW = 130;
            int controlX = 14 + labelW + 8;
            int controlW = 200;

            void AddRow(string labelText, Control ctrl)
            {
                var lbl = new Label
                {
                    Text = labelText,
                    Location = new Point(14, y + 3),
                    Size = new Size(labelW, 22),
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9.5f)
                };
                ctrl.Location = new Point(controlX, y);
                if (ctrl.Width == 0) ctrl.Width = controlW;
                pnlEdit.Controls.Add(lbl);
                pnlEdit.Controls.Add(ctrl);
                y += ctrl.Height + 10;
            }

            // كود البون
            txtCode = new TextBox { Size = new Size(controlW, 26), CharacterCasing = CharacterCasing.Upper, Font = new Font("Consolas", 10f) };
            AddRow("كود البون:", txtCode);

            // الوصف
            txtDescription = new TextBox { Size = new Size(controlW, 26) };
            AddRow("الوصف (اختياري):", txtDescription);

            // نوع الخصم
            cboDiscountType = new ComboBox { Size = new Size(controlW, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboDiscountType.Items.AddRange(new object[] { "نسبة مئوية %", "قيمة ثابتة ج" });
            cboDiscountType.SelectedIndex = 0;
            AddRow("نوع الخصم:", cboDiscountType);

            // قيمة الخصم
            nudDiscountValue = new NumericUpDown { Size = new Size(controlW, 26), Minimum = 0, Maximum = 100000, DecimalPlaces = 2, Value = 10 };
            cboDiscountType.SelectedIndexChanged += (s, e) =>
            {
                nudDiscountValue.Maximum = cboDiscountType.SelectedIndex == 0 ? 100 : 999999;
                nudDiscountValue.Increment = cboDiscountType.SelectedIndex == 0 ? 1 : 5;
            };
            AddRow("قيمة الخصم:", nudDiscountValue);

            // حد الاستخدامات
            var pnlUses = new Panel { Size = new Size(controlW, 26) };
            nudMaxUses = new NumericUpDown { Location = new Point(0, 0), Size = new Size(120, 26), Minimum = 1, Maximum = 999999, Value = 1 };
            chkUnlimitedUses = new CheckBox { Text = "بلا حد", Location = new Point(128, 3), AutoSize = true };
            chkUnlimitedUses.CheckedChanged += (s, e) => nudMaxUses.Enabled = !chkUnlimitedUses.Checked;
            pnlUses.Controls.Add(nudMaxUses);
            pnlUses.Controls.Add(chkUnlimitedUses);
            AddRow("حد الاستخدام:", pnlUses);

            // تاريخ الانتهاء
            var pnlExpiry = new Panel { Size = new Size(controlW, 26) };
            dtpExpiryDate = new DateTimePicker { Location = new Point(0, 0), Size = new Size(120, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(1) };
            chkNoExpiry = new CheckBox { Text = "لا تنتهي", Location = new Point(128, 3), AutoSize = true };
            chkNoExpiry.CheckedChanged += (s, e) => dtpExpiryDate.Enabled = !chkNoExpiry.Checked;
            pnlExpiry.Controls.Add(dtpExpiryDate);
            pnlExpiry.Controls.Add(chkNoExpiry);
            AddRow("تاريخ الانتهاء:", pnlExpiry);

            // مفعّل
            chkIsActive = new CheckBox { Text = "مفعّل", Checked = true, AutoSize = true };
            AddRow("الحالة:", chkIsActive);

            y += 8;

            // رسالة الحالة
            lblStatus = new Label
            {
                Location = new Point(14, y),
                Size = new Size(320, 22),
                ForeColor = Color.FromArgb(22, 163, 74),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            pnlEdit.Controls.Add(lblStatus);
            y += 30;

            // زر الحفظ
            btnSave = new Button
            {
                Text = "💾 حفظ البون",
                Location = new Point(14, y),
                Size = new Size(160, 36),
                BackColor = Color.FromArgb(109, 40, 217),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            pnlEdit.Controls.Add(btnSave);

            // عنوان الـ Panel
            var lblPanelTitle = new Label
            {
                Text = "✏️ تفاصيل البون",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(109, 40, 217),
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 8, 0)
            };

            split.Panel2.Controls.Add(pnlEdit);
            split.Panel2.Controls.Add(lblPanelTitle);

            this.Controls.Add(split);
            this.Controls.Add(pnlHeader);
        }

        private Button CreateButton(string text, Color back)
        {
            var btn = new Button
            {
                Text = text,
                Height = 32,
                AutoSize = true,
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 4, 4, 4)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadGrid()
        {
            try
            {
                var dt = DiscountVouchersDAL.GetAll();
                dgvVouchers.DataSource = null;
                dgvVouchers.Columns.Clear();
                dgvVouchers.DataSource = dt;

                // إخفاء VoucherID — نستخدمه برمجياً
                if (dgvVouchers.Columns.Contains("VoucherID"))
                    dgvVouchers.Columns["VoucherID"].Visible = false;

                // عناوين عربية
                var headers = new[]
                {
                    ("Code",          "كود البون"),
                    ("Description",   "الوصف"),
                    ("DiscountType",  "النوع"),
                    ("DiscountValue", "الخصم"),
                    ("MaxUses",       "حد الاستخدام"),
                    ("UsedCount",     "المُستخدم"),
                    ("ExpiryDate",    "ينتهي في"),
                    ("IsActive",      "مفعّل"),
                    ("CreatedAt",     "تاريخ الإنشاء")
                };
                foreach (var (col, header) in headers)
                {
                    if (dgvVouchers.Columns.Contains(col))
                    {
                        dgvVouchers.Columns[col].HeaderText = header;
                        if (col == "Description") dgvVouchers.Columns[col].FillWeight = 60f;
                        else if (col == "Code") dgvVouchers.Columns[col].FillWeight = 40f;
                    }
                }

                // تلوين الصفوف حسب الحالة
                foreach (DataGridViewRow row in dgvVouchers.Rows)
                {
                    bool active = row.Cells["IsActive"]?.Value as bool? ?? false;
                    if (!active)
                    {
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(156, 163, 175);
                    }
                    // تلوين الخانة DiscountType
                    if (row.Cells["DiscountType"] != null)
                    {
                        row.Cells["DiscountType"].Value = row.Cells["DiscountType"].Value?.ToString() == "Percent"
                            ? "نسبة %"
                            : "قيمة ج";
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadGrid vouchers failed", ex, "FrmDiscountVouchers");
            }
        }

        private void DgvVouchers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvVouchers.SelectedRows.Count == 0) return;
            var row = dgvVouchers.SelectedRows[0];
            if (row.DataBoundItem == null && row.Cells["VoucherID"]?.Value == null) return;

            if (!int.TryParse(row.Cells["VoucherID"]?.Value?.ToString(), out int vid)) return;

            var dto = DiscountVouchersDAL.GetByID(vid);
            if (dto == null) return;

            _editingVoucherID = dto.VoucherID;
            txtCode.Text = dto.Code;
            txtDescription.Text = dto.Description;
            cboDiscountType.SelectedIndex = dto.DiscountType == "Percent" ? 0 : 1;
            nudDiscountValue.Value = dto.DiscountValue;

            if (dto.MaxUses.HasValue)
            {
                chkUnlimitedUses.Checked = false;
                nudMaxUses.Value = dto.MaxUses.Value;
            }
            else
            {
                chkUnlimitedUses.Checked = true;
            }

            if (dto.ExpiryDate.HasValue)
            {
                chkNoExpiry.Checked = false;
                dtpExpiryDate.Value = dto.ExpiryDate.Value;
            }
            else
            {
                chkNoExpiry.Checked = true;
            }

            chkIsActive.Checked = dto.IsActive;
            lblStatus.Text = "";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string code = txtCode.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(code))
            {
                lblStatus.Text = "⚠️ يجب إدخال كود البون";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                txtCode.Focus();
                return;
            }
            if (nudDiscountValue.Value <= 0)
            {
                lblStatus.Text = "⚠️ يجب إدخال قيمة خصم أكبر من صفر";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }

            var dto = new DiscountVoucherDTO
            {
                VoucherID     = _editingVoucherID,
                Code          = code,
                Description   = txtDescription.Text.Trim(),
                DiscountType  = cboDiscountType.SelectedIndex == 0 ? "Percent" : "Amount",
                DiscountValue = nudDiscountValue.Value,
                MaxUses       = chkUnlimitedUses.Checked ? (int?)null : (int)nudMaxUses.Value,
                ExpiryDate    = chkNoExpiry.Checked ? (DateTime?)null : dtpExpiryDate.Value.Date,
                IsActive      = chkIsActive.Checked
            };

            try
            {
                DiscountVouchersDAL.Save(dto);
                lblStatus.Text = "✅ تم الحفظ بنجاح";
                lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                LoadGrid();
                ClearForm();

                // مزامنة فورية مع السحاب للمتجر الإلكتروني
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try { await ChickenDist.Services.CloudSyncService.SyncVouchersToFirebaseAsync(); } catch { }
                });
            }
            catch (Exception ex)
            {
                lblStatus.Text = "❌ خطأ: " + ex.Message;
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                AppLogger.Error("Save voucher failed", ex, "FrmDiscountVouchers");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvVouchers.SelectedRows.Count == 0) return;
            if (!int.TryParse(dgvVouchers.SelectedRows[0].Cells["VoucherID"]?.Value?.ToString(), out int vid)) return;

            var dto = DiscountVouchersDAL.GetByID(vid);
            if (dto == null) return;

            if (MessageBox.Show($"حذف البون [{dto.Code}]؟\n\nهذا الإجراء لا يمكن التراجع عنه.",
                    "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                DiscountVouchersDAL.Delete(vid);
                LoadGrid();
                ClearForm();
                lblStatus.Text = "✅ تم الحذف";
                lblStatus.ForeColor = Color.FromArgb(22, 163, 74);

                // مزامنة فورية مع السحاب للمتجر الإلكتروني
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try { await ChickenDist.Services.CloudSyncService.SyncVouchersToFirebaseAsync(); } catch { }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل الحذف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearForm()
        {
            _editingVoucherID = 0;
            txtCode.Text = "";
            txtDescription.Text = "";
            cboDiscountType.SelectedIndex = 0;
            nudDiscountValue.Value = 10;
            chkUnlimitedUses.Checked = false;
            nudMaxUses.Value = 1;
            chkNoExpiry.Checked = false;
            dtpExpiryDate.Value = DateTime.Today.AddMonths(1);
            chkIsActive.Checked = true;
            lblStatus.Text = "";
            txtCode.Focus();
            dgvVouchers.ClearSelection();
        }
    }
}

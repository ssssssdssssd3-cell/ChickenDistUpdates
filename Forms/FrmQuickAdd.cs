using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة الإدخال السريع للأصناف (تسجيل متتالي سريع جداً)
    /// </summary>
    public class FrmQuickAdd : Form
    {
        private TextBox txtName, txtCode;
        private ComboBox cboCategory, cboUnit;
        private NumericUpDown nudPurchasePrice, nudPrice;
        private CheckBox chkIsService, chkIsQuickItem;
        private Button btnSave, btnClose;
        private Label lblStatus;
        private Timer statusTimer;

        // اسم/كود أولي اختياري (لاستخدامه عند الفتح من شاشة الشراء لصنف غير موجود)
        private readonly string _initialName;
        private readonly string _initialCode;

        public FrmQuickAdd(string initialName = null, string initialCode = null)
        {
            _initialName = initialName;
            _initialCode = initialCode;
            InitUI();
            
            // مؤقت لمسح حالة الحفظ بعد ثانيتين
            statusTimer = new Timer { Interval = 2500 };
            statusTimer.Tick += (s, e) => {
                lblStatus.Text = "";
                statusTimer.Stop();
            };
        }

        private void InitUI()
        {
            this.Text = "⚡ الإدخال السريع للأصناف";
            this.Size = new Size(500, 565);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlTitle = Theme.MakeTitleBar("⚡ الإدخال السريع للأصناف", "إضافة أصناف جديدة متتالية بسرعة فائقة مع التحقق التلقائي.");
            this.Controls.Add(pnlTitle);

            var pnlForm = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(445, 355),
                BackColor = Theme.BgCard,
                Padding = new Padding(15)
            };

            int y = 15;

            // كود الصنف (تلقائي للقراءة فقط)
            pnlForm.Controls.Add(new Label { Text = "كود الصنف (تلقائي):", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtCode = new TextBox { Location = new Point(165, y), Width = 260, Height = 30, Font = new Font("Segoe UI", 11f), BackColor = Color.FromArgb(240, 240, 240), ForeColor = Color.FromArgb(80, 80, 80), BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, TabStop = false };
            pnlForm.Controls.Add(txtCode);
            y += 40;

            // اسم الصنف
            pnlForm.Controls.Add(new Label { Text = "اسم الصنف:", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtName = new TextBox { Location = new Point(165, y), Width = 260, Height = 30, Font = new Font("Segoe UI", 11f), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(txtName);
            y += 40;

            // التصنيف
            pnlForm.Controls.Add(new Label { Text = "التصنيف:", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            cboCategory = new ComboBox
            {
                Location = new Point(165, y),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            pnlForm.Controls.Add(cboCategory);
            y += 40;

            // الوحدة
            pnlForm.Controls.Add(new Label { Text = "الوحدة الأساسية:", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            cboUnit = new ComboBox
            {
                Location = new Point(165, y),
                Width = 260,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            pnlForm.Controls.Add(cboUnit);
            y += 40;

            // سعر الشراء
            bool canSeeCost = Session.CanViewCost("Products");
            var lblPurchase = new Label { Text = "سعر الشراء:", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold, Visible = canSeeCost };
            nudPurchasePrice = new NumericUpDown { Location = new Point(165, y), Width = 260, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, Font = new Font("Segoe UI", 11f), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Visible = canSeeCost };
            pnlForm.Controls.Add(lblPurchase);
            pnlForm.Controls.Add(nudPurchasePrice);
            if (canSeeCost) y += 40;

            // سعر البيع
            pnlForm.Controls.Add(new Label { Text = "سعر البيع (قطاعي):", Location = new Point(15, y + 4), Width = 140, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            nudPrice = new NumericUpDown { Location = new Point(165, y), Width = 260, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, Font = new Font("Segoe UI", 11f), BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(nudPrice);
            y += 40;

            // صنف بيع سريع
            chkIsQuickItem = new CheckBox
            {
                Text = "📌 صنف بيع سريع (شاشة POS)",
                Location = new Point(20, y),
                Width = 280,
                Height = 26,
                ForeColor = Color.FromArgb(0, 120, 180),
                Checked = false,
                AutoSize = false,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };
            pnlForm.Controls.Add(chkIsQuickItem);
            y += 32;

            // صنف يباع بالسالب
            chkIsService = new CheckBox
            {
                Text = "⚡ صنف يباع بالسالب (خدمات / مصروفات)",
                Location = new Point(20, y),
                Width = 280,
                Height = 26,
                ForeColor = Color.FromArgb(180, 120, 0),
                Checked = false,
                AutoSize = false,
                CheckAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(Theme.FontMain, FontStyle.Bold)
            };
            pnlForm.Controls.Add(chkIsService);

            this.Controls.Add(pnlForm);

            // حالة الحفظ
            lblStatus = new Label
            {
                Text = "",
                Location = new Point(20, 445),
                Size = new Size(445, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.Success
            };
            this.Controls.Add(lblStatus);

            // الأزرار
            btnSave = Theme.MakeButton("💾 حفظ الصنف (Ctrl+S)", 250, 475, 215, 36, Theme.Success);
            btnSave.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnClose = Theme.MakeButton("إغلاق ↩", 20, 475, 220, 36, Color.FromArgb(70, 80, 95));
            btnClose.Click += (s, e) => {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnClose);

            // اختصارات لوحة المفاتيح
            this.KeyPreview = true;
            this.KeyDown += FrmQuickAdd_KeyDown;

            // عند الضغط على Enter في سعر البيع يتم الحفظ مباشرة
            nudPrice.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    BtnSave_Click(this, EventArgs.Empty);
                }
            };

            // تهيئة الحقول عند التحميل
            this.Load += (s, e) => {
                LoadCombos();
                ResetForm();
            };

            Theme.ApplyFormRTL(this);
        }

        private void LoadCombos()
        {
            try
            {
                // تحميل التصنيفات
                cboCategory.Items.Clear();
                cboCategory.Items.Add(new ComboItem(0, "-- بدون تصنيف --"));
                DataTable dtCat = CategoryDAL.GetAll(true);
                foreach (DataRow r in dtCat.Rows)
                {
                    cboCategory.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
                }
                cboCategory.DisplayMember = "Text";
                cboCategory.SelectedIndex = 0;

                // تحميل الوحدات
                cboUnit.Items.Clear();
                DataTable dtUnits = UnitDAL.GetAll();
                foreach (DataRow r in dtUnits.Rows)
                {
                    cboUnit.Items.Add(r["UnitName"].ToString());
                }
                if (cboUnit.Items.Count == 0)
                {
                    cboUnit.Items.AddRange(new string[] { "قطعة", "علبة", "كرتونة", "كيلو", "لتر", "متر" });
                }
                cboUnit.Text = "قطعة";
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadCombos in FrmQuickAdd failed", ex);
            }
        }

        private void FrmQuickAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                BtnSave_Click(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void ResetForm()
        {
            txtCode.Text = ProductDAL.GetNextProductCode();
            txtName.Clear();
            nudPurchasePrice.Value = 0;
            nudPrice.Value = 0;
            chkIsQuickItem.Checked = false;
            chkIsService.Checked = false;
            if (string.IsNullOrEmpty(cboUnit.Text)) cboUnit.Text = "قطعة";

            // تعبئة الاسم والكود الأولي إن وُجدا (عند الفتح من شاشة الشراء)
            if (!string.IsNullOrWhiteSpace(_initialName))
                txtName.Text = _initialName;
            if (!string.IsNullOrWhiteSpace(_initialCode))
                txtCode.Text = _initialCode;

            txtName.Focus();
            txtName.SelectAll();
        }

        private void ShowStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            statusTimer.Stop();
            statusTimer.Start();
        }

        public static event EventHandler ProductSaved;

        public static void RaiseProductSaved()
        {
            try
            {
                ProductSaved?.Invoke(null, EventArgs.Empty);
            }
            catch { }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Session.IsAdmin && !Session.CanAdd("Products") && !Session.CanAdd("ProductCard"))
            {
                ShowStatus("❌ لا تملك صلاحية إضافة أصناف جديدة", Theme.Danger);
                return;
            }

            string name = txtName.Text.Trim();
            string code = txtCode.Text.Trim();
            string unit = string.IsNullOrWhiteSpace(cboUnit.Text) ? "قطعة" : cboUnit.Text.Trim();

            int? categoryID = null;
            if (cboCategory.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                categoryID = ci.ID;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowStatus("❌ يرجى إدخال اسم الصنف", Theme.Danger);
                txtName.Focus();
                return;
            }

            if (nudPrice.Value < 0 || nudPurchasePrice.Value < 0)
            {
                ShowStatus("❌ لا يمكن إدخال أسعار سالبة", Theme.Danger);
                return;
            }

            // التحقق من تكرار الباركود
            if (!string.IsNullOrEmpty(code) && ProductDAL.IsCodeExists(code, 0))
            {
                ShowStatus($"❌ الباركود '{code}' مستخدم لصنف آخر", Theme.Danger);
                txtCode.Focus();
                txtCode.SelectAll();
                return;
            }

            // التحقق من تكرار الاسم
            if (ProductDAL.IsNameExists(name, 0))
            {
                ShowStatus($"❌ اسم الصنف '{name}' مستخدم بالفعل", Theme.Danger);
                txtName.Focus();
                txtName.SelectAll();
                return;
            }

            // حفظ الصنف
            try
            {
                int id = ProductDAL.Save(
                    0, code, name, unit, nudPrice.Value, true,
                    nudPurchasePrice.Value, 0, "", "", categoryID, "", "", "", 
                    0, 0, null, true,
                    chkIsService.Checked,
                    null, null, null, null,
                    null, null, null, null, null,
                    null, chkIsQuickItem.Checked
                );

                if (id > 0)
                {
                    ProductCache.Refresh();
                    ShowStatus($"✅ تم حفظ '{name}' بنجاح!", Theme.Success);
                    RaiseProductSaved();
                    ResetForm();
                }
                else
                {
                    ShowStatus("❌ فشل حفظ الصنف في قاعدة البيانات", Theme.Danger);
                }
            }
            catch (Exception ex)
            {
                ShowStatus("❌ خطأ: " + ex.Message, Theme.Danger);
            }
        }
    }
}

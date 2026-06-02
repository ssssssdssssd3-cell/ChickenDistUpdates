using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة فاتورة المشتريات الاحترافية — مع تعليق/استرجاع وخصم الصنف والضريبة</summary>
    public class FrmPurchase : Form
    {
        // ── أنواع الفاتورة ─────────────────────────────────────────────────────
        private Button btnTypeCredit, btnTypeCash;
        private string _purchaseType = "Credit";

        // ── حقول الرأس ─────────────────────────────────────────────────────────
        private ComboBox cboSupplier, cboProduct;
        private DateTimePicker dtpDate;
        private TextBox txtNotes;
        private Label lblCashBalance;

        // ── حقول إضافة صنف ─────────────────────────────────────────────────────
        private NumericUpDown nudQty, nudPrice, nudItemDisc;
        private Button btnAddItem;

        // ── جدول الأصناف ───────────────────────────────────────────────────────
        private DataGridView dgItems;
        private Panel pnlItems, pnlFooter;

        // ── الذيل — خصم الفاتورة ───────────────────────────────────────────────
        private Label lblTotalVal, lblNetVal, lblDiscType, lblDiscVal;
        private TextBox txtInvoiceDiscount;
        private ComboBox cboInvoiceDiscountType;

        // ── الذيل — ضريبة الشراء (جديد) ───────────────────────────────────────
        private NumericUpDown nudTaxPct;
        private Label lblTaxAmt;

        // ── أزرار الذيل ────────────────────────────────────────────────────────
        private Button btnSave, btnNew, btnPrint, btnHold, btnLoadHold;

        // ── بيانات الفاتورة ────────────────────────────────────────────────────
        private List<PurchaseItemDTO> _items = new List<PurchaseItemDTO>();
        private int _lastPurchaseID = 0;
        private bool _isDirty = false;
        private int _draftPurchaseID = 0; // 0 = فاتورة جديدة، >0 = مسودة محملة

        // ══════════════════════════════════════════════════════════════════════
        public FrmPurchase()
        {
            InitUI();
            LoadCombos();
            ClearInvoice();
        }

        // ── مساعد Label ────────────────────────────────────────────────────────
        private Label MakeLabel(string text, int x, int y, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = color ?? Theme.TextMain,
                Font = Theme.FontMain
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        private void InitUI()
        {
            this.Text = "فاتورة مشتريات";
            this.Size = new Size(1100, 730);
            this.MinimumSize = new Size(900, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmPurchase_KeyDown;
            this.FormClosing += FrmPurchase_FormClosing;

            // ── الرأس ──────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 148,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            // نوع الفاتورة
            var lblType = MakeLabel("نوع الفاتورة:", 940, 12);
            btnTypeCredit = Theme.MakeButton("📋 آجل",  830, 8, 100, 30, Theme.Primary);
            btnTypeCash   = Theme.MakeButton("💵 نقدي", 720, 8, 100, 30, Color.FromArgb(60, 60, 60));
            btnTypeCredit.Click += (s, e) => { _purchaseType = "Credit"; ToggleType(); };
            btnTypeCash.Click   += (s, e) => { _purchaseType = "Cash";   ToggleType(); };

            // المورد
            var lblSupp = MakeLabel("المورد:", 940, 48);
            cboSupplier = new ComboBox
            {
                Location = new Point(640, 44), Width = 290,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };

            // التاريخ
            var lblDate = MakeLabel("التاريخ:", 560, 48);
            dtpDate = new DateTimePicker
            {
                Location = new Point(405, 44), Width = 145,
                Format = DateTimePickerFormat.Short,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            // رصيد الخزنة (يظهر للنقدي)
            lblCashBalance = new Label
            {
                Text = "",
                Location = new Point(405, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 180, 100),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            // ملاحظات
            var lblNotes = MakeLabel("ملاحظات:", 940, 86);
            txtNotes = new TextBox
            {
                Location = new Point(640, 82), Width = 290,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            // ─ صف إضافة الصنف ─────────────────────────────────────────────────
            var lblProd = MakeLabel("الصنف:", 560, 86);
            cboProduct = new ComboBox
            {
                Location = new Point(315, 82), Width = 235,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };

            var lblQty = MakeLabel("الكمية:", 290, 86);
            nudQty = new NumericUpDown
            {
                Location = new Point(205, 80), Width = 77,
                DecimalPlaces = 3, Minimum = 0.001m, Maximum = 999999, Value = 1,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            var lblPrice = MakeLabel("السعر:", 178, 86);
            nudPrice = new NumericUpDown
            {
                Location = new Point(108, 80), Width = 62,
                DecimalPlaces = 2, Minimum = 0, Maximum = 9999999,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            var lblItemDiscLbl = MakeLabel("خصم%:", 88, 86);
            nudItemDisc = new NumericUpDown
            {
                Location = new Point(8, 80), Width = 72,
                DecimalPlaces = 2, Minimum = 0, Maximum = 100,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            nudItemDisc.Value = 0;

            btnAddItem = Theme.MakeButton("➕ إضافة", 0, 0, 110, 30, Theme.Accent);
            btnAddItem.Location = new Point(8, 114);
            btnAddItem.Click += BtnAddItem_Click;

            pnlHeader.Controls.AddRange(new Control[]
            {
                lblType, btnTypeCredit, btnTypeCash,
                lblSupp, cboSupplier, lblDate, dtpDate, lblCashBalance,
                lblNotes, txtNotes,
                lblProd, cboProduct,
                lblQty, nudQty, lblPrice, nudPrice,
                lblItemDiscLbl, nudItemDisc,
                btnAddItem
            });

            // ── جدول الأصناف ───────────────────────────────────────────────────
            pnlItems = new Panel { Dock = DockStyle.Fill };
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID",   Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName",  HeaderText = "الصنف",       ReadOnly = true, FillWeight = 120 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",     HeaderText = "الكمية",      FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",    HeaderText = "سعر الشراء",  FillWeight = 65 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountPct",  HeaderText = "خصم %",       FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice",   HeaderText = "الإجمالي",    ReadOnly = true, FillWeight = 65 });
            dgItems.Columns.Add(new DataGridViewButtonColumn  { Name = "Delete",       HeaderText = "حذف",
                Text = "❌", UseColumnTextForButtonValue = true, FillWeight = 30 });

            dgItems.CellValueChanged  += DgItems_CellValueChanged;
            dgItems.CellClick         += DgItems_CellClick;
            dgItems.CellEndEdit       += (s, e) => RecalcTotals();

            pnlItems.Controls.Add(dgItems);

            // ── الذيل ──────────────────────────────────────────────────────────
            pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 135,
                BackColor = Theme.BgCard
            };

            // ─ صف 1: إجمالي الأصناف + خصم الفاتورة (y≈10) ─────────────────────
            var lblItemsTotalLbl = new Label
            {
                Text = "إجمالي الأصناف:",
                ForeColor = Theme.TextSub,
                Location = new Point(920, 12), AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblTotalVal = new Label
            {
                Text = "0.00 ج",
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(810, 10), AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            lblDiscType = MakeLabel("خصم:", 710, 12, Theme.TextSub);
            cboInvoiceDiscountType = new ComboBox
            {
                Location = new Point(635, 9), Width = 65,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat
            };
            cboInvoiceDiscountType.Items.AddRange(new object[] { "مبلغ", "%" });
            cboInvoiceDiscountType.SelectedIndex = 0;
            cboInvoiceDiscountType.SelectedIndexChanged += (s, e) => RecalcTotals();

            lblDiscVal = MakeLabel("قيمة:", 583, 12, Theme.TextSub);
            txtInvoiceDiscount = new TextBox
            {
                Location = new Point(490, 9), Width = 85,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Text = "0"
            };
            txtInvoiceDiscount.TextChanged += (s, e) => RecalcTotals();

            // ─ صف 2: ضريبة الشراء (y≈47) ────────────────────────────────────────
            var lblTaxLbl = new Label
            {
                Text = "ضريبة %:",
                ForeColor = Theme.TextSub,
                Location = new Point(920, 48), AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            nudTaxPct = new NumericUpDown
            {
                Location = new Point(840, 45), Width = 70,
                DecimalPlaces = 2, Minimum = 0, Maximum = 100,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            nudTaxPct.ValueChanged += (s, e) => RecalcTotals();

            var lblTaxAmtLbl = new Label
            {
                Text = "= قيمة الضريبة:",
                ForeColor = Theme.TextSub,
                Location = new Point(753, 48), AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblTaxAmt = new Label
            {
                Text = "0.00 ج",
                ForeColor = Color.FromArgb(230, 162, 60),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(640, 46), AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // ─ صف 3: الصافي النهائي (y≈82) ─────────────────────────────────────
            var lblNetTitle = new Label
            {
                Text = "📦 صافي الفاتورة:",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(920, 83), AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblNetVal = new Label
            {
                Text = "0.00 ج",
                ForeColor = Color.FromArgb(46, 204, 113),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Location = new Point(750, 79), AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // ─ أزرار الذيل (FlowLayoutPanel يسار) ─────────────────────────────
            btnSave     = Theme.MakeButton("💾 حفظ [F5]",    0, 0, 125, 33, Theme.Accent);
            btnHold     = Theme.MakeButton("⏸️ تعليق [F7]",  0, 0, 120, 33, Color.FromArgb(200, 140, 50));
            btnLoadHold = Theme.MakeButton("📂 معلقات [F8]", 0, 0, 128, 33, Color.FromArgb(100, 100, 160));
            btnNew      = Theme.MakeButton("🆕 جديد [F2]",   0, 0, 115, 33, Color.FromArgb(60, 100, 60));
            btnPrint    = Theme.MakeButton("🖨️ طباعة",       0, 0, 90,  33, Color.FromArgb(80, 80, 80));

            btnSave.Click     += BtnSave_Click;
            btnHold.Click     += BtnHold_Click;
            btnLoadHold.Click += BtnLoadHold_Click;
            btnNew.Click      += (s, e) => ClearInvoice();

            var pnlButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(5, 90),
                Size     = new Size(600, 43),
                BackColor = Color.Transparent,
                WrapContents = false,
                Padding  = new Padding(0)
            };
            foreach (var b in new[] { btnSave, btnHold, btnLoadHold, btnNew, btnPrint })
            {
                b.Margin = new Padding(3, 3, 3, 3);
                pnlButtons.Controls.Add(b);
            }

            // ─ نص الاختصارات ─────────────────────────────────────────────────
            var lblHotkeys = new Label
            {
                Text = "[F2] جديد  |  [F5] حفظ  |  [F7] تعليق  |  [F8] معلقات  |  [F12] بحث صنف",
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(5, 115),
                AutoSize = true
            };

            pnlFooter.Controls.AddRange(new Control[]
            {
                lblItemsTotalLbl, lblTotalVal,
                lblDiscType, cboInvoiceDiscountType, lblDiscVal, txtInvoiceDiscount,
                lblTaxLbl, nudTaxPct, lblTaxAmtLbl, lblTaxAmt,
                lblNetTitle, lblNetVal,
                pnlButtons, lblHotkeys
            });

            // ── تجميع عناصر النموذج ────────────────────────────────────────────
            base.Controls.Add(pnlItems);
            base.Controls.Add(pnlFooter);
            base.Controls.Add(pnlHeader);
            pnlItems.BringToFront();
            ToggleType();
            Theme.ApplyFormRTL(this);
        }

        // ══════════════════════════════════════════════════════════════════════
        // اختصارات لوحة المفاتيح
        private void FrmPurchase_KeyDown(object sender, KeyEventArgs e)
        {
            if (dgItems.IsCurrentCellInEditMode &&
                (e.KeyCode == Keys.F2 || e.KeyCode == Keys.F5 ||
                 e.KeyCode == Keys.F7 || e.KeyCode == Keys.F8))
            {
                dgItems.EndEdit();
            }

            if      (e.KeyCode == Keys.F2)  { ClearInvoice();          e.Handled = true; }
            else if (e.KeyCode == Keys.F5)  { BtnSave_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F7)  { BtnHold_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F8)  { BtnLoadHold_Click(null,null); e.Handled = true; }
            else if (e.KeyCode == Keys.F12) { cboProduct.Focus();       e.Handled = true; }
        }

        // تنقل بمفتاح Enter داخل الجدول
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter &&
                (dgItems.Focused || dgItems.EditingControl != null))
            {
                var cur = dgItems.CurrentCell;
                if (cur != null)
                {
                    dgItems.EndEdit();
                    for (int col = cur.ColumnIndex + 1; col < dgItems.ColumnCount; col++)
                    {
                        if (!dgItems.Columns[col].ReadOnly && dgItems.Columns[col].Visible)
                        {
                            dgItems.CurrentCell = dgItems.Rows[cur.RowIndex].Cells[col];
                            dgItems.BeginEdit(true);
                            return true;
                        }
                    }
                    cboProduct.Focus();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // تحذير عند الإغلاق بفاتورة غير محفوظة
        private void FrmPurchase_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isDirty && _items.Count > 0)
            {
                var res = MessageBox.Show(
                    "توجد فاتورة قيد الإدخال لم يتم حفظها.\nهل تريد الإغلاق بدون حفظ؟",
                    "تنبيه", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (res == DialogResult.No)
                    e.Cancel = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // تحديد نوع الفاتورة (آجل / نقدي)
        private void ToggleType()
        {
            bool isCredit = _purchaseType == "Credit";
            btnTypeCredit.BackColor = isCredit ? Theme.Primary : Color.FromArgb(60, 60, 60);
            btnTypeCash.BackColor   = !isCredit ? Theme.Accent : Color.FromArgb(60, 60, 60);

            if (!isCredit)
            {
                var cashResult = DbHelper.Scalar(
                    "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                decimal cashBal = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                lblCashBalance.Text = $"💰 رصيد الخزنة: {cashBal:N2} ج";
                lblCashBalance.ForeColor = cashBal > 0 ? Color.FromArgb(100,180,100) : Color.OrangeRed;
            }
            else
            {
                lblCashBalance.Text = "";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // تحميل الكومبوات
        private void LoadCombos()
        {
            // الموردون
            DataTable dtSup = SupplierDAL.GetAll(true);
            cboSupplier.Items.Clear();
            cboSupplier.Items.Add(new ComboItem(0, "-- اختر المورد --"));
            foreach (DataRow r in dtSup.Rows)
                cboSupplier.Items.Add(new ComboItem(
                    Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
            cboSupplier.DisplayMember = "Text";
            cboSupplier.SelectedIndex = 0;

            // الأصناف
            DataTable dtProd = ProductDAL.GetAll(true);
            cboProduct.Items.Clear();
            cboProduct.Items.Add(new ComboItem(0, "-- اختر الصنف --"));
            foreach (DataRow r in dtProd.Rows)
            {
                var ci = new ComboItem(
                    Convert.ToInt32(r["ProductID"]),
                    r["ProductName"].ToString());
                ci.Extra = r["PurchasePrice"] != DBNull.Value
                    ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                cboProduct.Items.Add(ci);
            }
            cboProduct.DisplayMember = "Text";
            cboProduct.SelectedIndex = 0;
            cboProduct.SelectedIndexChanged += (s, e) =>
            {
                if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0)
                    nudPrice.Value = ci.Extra;
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        // إضافة صنف
        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (!(cboProduct.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر صنفاً أولاً"); return;
            }
            decimal qty   = nudQty.Value;
            decimal price = nudPrice.Value;
            decimal disc  = nudItemDisc.Value;

            if (qty <= 0 || price <= 0)
            {
                MessageBox.Show("أدخل كمية وسعر صحيحين"); return;
            }

            // دمج إذا كان الصنف موجوداً مسبقاً
            foreach (var item in _items)
            {
                if (item.ProductID == ci.ID)
                {
                    item.Quantity   += qty;
                    if (disc > 0) item.DiscountPct = disc; // تحديث الخصم
                    RefreshGrid();
                    ResetAddRow();
                    return;
                }
            }

            _items.Add(new PurchaseItemDTO
            {
                ProductID   = ci.ID,
                ProductName = ci.Text,
                Quantity    = qty,
                UnitPrice   = price,
                DiscountPct = disc
            });

            RefreshGrid();
            ResetAddRow();
            _isDirty = true;
        }

        private void ResetAddRow()
        {
            cboProduct.SelectedIndex = 0;
            nudQty.Value   = 1;
            nudItemDisc.Value = 0;
            cboProduct.Focus();
        }

        // ══════════════════════════════════════════════════════════════════════
        // تحديث الجدول
        private void RefreshGrid()
        {
            dgItems.CellValueChanged -= DgItems_CellValueChanged;
            dgItems.Rows.Clear();
            foreach (var item in _items)
            {
                dgItems.Rows.Add(
                    item.ProductID,
                    item.ProductName,
                    item.Quantity.ToString("F3"),
                    item.UnitPrice.ToString("F2"),
                    item.DiscountPct.ToString("F2"),
                    item.TotalPrice.ToString("F2"));
            }
            dgItems.CellValueChanged += DgItems_CellValueChanged;
            RecalcTotals();
        }

        // ══════════════════════════════════════════════════════════════════════
        // تعديل الكميات/الأسعار/الخصم من الجدول مباشرة
        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var item    = _items[e.RowIndex];
            var colName = dgItems.Columns[e.ColumnIndex].Name;
            var cellVal = dgItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();

            if (colName == "Quantity")
            {
                if (decimal.TryParse(cellVal, out decimal q) && q > 0)
                    item.Quantity = q;
                else
                    dgItems.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity.ToString("F3");
            }
            else if (colName == "UnitPrice")
            {
                if (decimal.TryParse(cellVal, out decimal p) && p > 0)
                    item.UnitPrice = p;
                else
                    dgItems.Rows[e.RowIndex].Cells["UnitPrice"].Value = item.UnitPrice.ToString("F2");
            }
            else if (colName == "DiscountPct")
            {
                if (decimal.TryParse(cellVal, out decimal d) && d >= 0 && d <= 100)
                {
                    item.DiscountPct = d;
                    item.DiscountAmt = 0m; // مسح القيمة المباشرة عند وجود نسبة
                }
                else
                    dgItems.Rows[e.RowIndex].Cells["DiscountPct"].Value = item.DiscountPct.ToString("F2");
            }

            // تحديث عمود الإجمالي
            dgItems.Rows[e.RowIndex].Cells["TotalPrice"].Value = item.TotalPrice.ToString("F2");
            _isDirty = true;
            RecalcTotals();
        }

        // حذف صف بضغطة الزر
        private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "Delete")
            {
                _items.RemoveAt(e.RowIndex);
                RefreshGrid();
                _isDirty = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // حساب الإجماليات (أصناف + خصم الفاتورة + ضريبة = الصافي)
        private void RecalcTotals()
        {
            decimal itemsTotal = 0m;
            foreach (var item in _items) itemsTotal += item.TotalPrice;
            lblTotalVal.Text = itemsTotal.ToString("N2") + " ج";

            // خصم الفاتورة
            decimal discVal = 0m;
            decimal.TryParse(txtInvoiceDiscount.Text, out discVal);
            decimal discAmt = cboInvoiceDiscountType.SelectedIndex == 1
                ? itemsTotal * discVal / 100m
                : discVal;
            decimal afterDisc = Math.Max(0m, itemsTotal - discAmt);

            // ضريبة الشراء
            decimal taxPct = nudTaxPct.Value;
            decimal taxAmt = Math.Round(afterDisc * taxPct / 100m, 2);
            lblTaxAmt.Text = taxAmt.ToString("N2") + " ج";

            // الصافي النهائي
            decimal net = afterDisc + taxAmt;
            lblNetVal.Text = net.ToString("N2") + " ج";
        }

        // ══════════════════════════════════════════════════════════════════════
        // مسح الفاتورة (جديدة)
        private void ClearInvoice()
        {
            _items.Clear();
            RefreshGrid();
            if (cboSupplier.Items.Count > 0) cboSupplier.SelectedIndex = 0;
            if (cboProduct.Items.Count  > 0) cboProduct.SelectedIndex  = 0;
            txtNotes.Clear();
            txtInvoiceDiscount.Text = "0";
            nudTaxPct.Value  = 0;
            nudQty.Value     = 1;
            nudPrice.Value   = 0;
            nudItemDisc.Value = 0;
            _purchaseType    = "Credit";
            _isDirty         = false;
            _draftPurchaseID = 0;
            dtpDate.Value    = DateTime.Today;
            ToggleType();
            this.Text = "فاتورة مشتريات";
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── تعليق الفاتورة ────────────────────────────────────────────────────
        private void BtnHold_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("أضف أصنافاً أولاً للتعليق", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // حذف المسودة القديمة إذا كنا نعيد تعليق مسودة محملة
            if (_draftPurchaseID > 0)
            {
                try { PurchaseDAL.DeleteDraftPurchase(_draftPurchaseID); }
                catch { /* تجاهل */ }
            }

            int? supplierID = GetSelectedSupplier();
            decimal gross, discAmt, discPct, net, taxPct, taxAmt;
            CalcAmounts(out gross, out discAmt, out discPct, out net, out taxPct, out taxAmt);

            try
            {
                int draftID = PurchaseDAL.SavePurchase(
                    _purchaseType, supplierID, net, txtNotes.Text, _items,
                    discAmt, discPct, taxPct, taxAmt, isDraft: true);

                if (draftID > 0)
                {
                    MessageBox.Show(
                        $"✅ تم تعليق الفاتورة بنجاح.\nيمكنك استدعاؤها لاحقاً من زر 📂 معلقات.",
                        "تعليق", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearInvoice();
                }
                else
                {
                    MessageBox.Show("❌ فشل تعليق الفاتورة.", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل تعليق فاتورة المشتريات", ex, "FrmPurchase.BtnHold_Click");
                MessageBox.Show("❌ حدث خطأ:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── استرجاع الفواتير المعلقة ──────────────────────────────────────────
        private void BtnLoadHold_Click(object sender, EventArgs e)
        {
            DataTable dt = PurchaseDAL.GetDraftPurchases();
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد فواتير مشتريات معلقة حالياً.",
                    "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // نافذة قائمة المعلقات
            var dlg = new Form
            {
                Width = 850, Height = 460,
                Text = "📂 فواتير المشتريات المعلقة",
                StartPosition = FormStartPosition.CenterParent,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain
            };

            var dgDrafts = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = dt,
                BackgroundColor = Theme.BgCard,
                RowHeadersVisible = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = Theme.FontMain, BackColor = Theme.BgCard, ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = Theme.FontBold, BackColor = Theme.Primary, ForeColor = Color.White
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dlg.Load += (sl, el) =>
            {
                foreach (DataGridViewColumn col in dgDrafts.Columns)
                {
                    string cn = col.Name;
                    if (cn == "PurchaseID" || cn == "SupplierID" ||
                        cn == "DiscountAmount" || cn == "DiscountPct" ||
                        cn == "TaxPct" || cn == "TaxAmount")
                    {
                        col.Visible = false; continue;
                    }
                    switch (cn)
                    {
                        case "PurchaseCode":  col.HeaderText = "كود الفاتورة";  break;
                        case "PurchaseDate":  col.HeaderText = "التاريخ";       break;
                        case "PurchaseType":  col.HeaderText = "النوع";         break;
                        case "SupplierName":  col.HeaderText = "المورد";        break;
                        case "TotalAmount":   col.HeaderText = "الإجمالي";     break;
                        case "Notes":         col.HeaderText = "ملاحظات";       break;
                    }
                }
            };

            var pnlBtns = new Panel
            {
                Dock = DockStyle.Bottom, Height = 48,
                BackColor = Theme.BgCard, Padding = new Padding(8)
            };

            var btnLoad = Theme.MakeButton("✅ استدعاء الفاتورة", 0, 6, 175, 35, Theme.Success);
            btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoad.Click += (s2, e2) =>
            {
                if (dgDrafts.SelectedRows.Count == 0) return;
                var row = (DataRowView)dgDrafts.SelectedRows[0].DataBoundItem;

                if (_isDirty && _items.Count > 0)
                {
                    if (MessageBox.Show(
                        "توجد فاتورة حالية قيد الإدخال — سيتم مسحها لتحميل الفاتورة المعلقة.\nهل أنت متأكد؟",
                        "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                }

                int pid = Convert.ToInt32(row["PurchaseID"]);
                ClearInvoice();
                _draftPurchaseID = pid;

                // نوع الفاتورة
                string typeStr = row["PurchaseType"].ToString();
                _purchaseType = typeStr;
                ToggleType();

                // المورد
                if (row["SupplierID"] != DBNull.Value)
                {
                    int sid = Convert.ToInt32(row["SupplierID"]);
                    for (int i = 0; i < cboSupplier.Items.Count; i++)
                    {
                        if (cboSupplier.Items[i] is ComboItem ci && ci.ID == sid)
                        {
                            cboSupplier.SelectedIndex = i; break;
                        }
                    }
                }

                // التاريخ والملاحظات
                dtpDate.Value = Convert.ToDateTime(row["PurchaseDate"]);
                txtNotes.Text = row["Notes"].ToString();

                // الخصم
                decimal dAmt = Convert.ToDecimal(row["DiscountAmount"]);
                decimal dPct = Convert.ToDecimal(row["DiscountPct"]);
                if (dPct > 0)
                {
                    cboInvoiceDiscountType.SelectedIndex = 1;
                    txtInvoiceDiscount.Text = dPct.ToString("G29");
                }
                else
                {
                    cboInvoiceDiscountType.SelectedIndex = 0;
                    txtInvoiceDiscount.Text = dAmt.ToString("G29");
                }

                // الضريبة
                decimal tPct = Convert.ToDecimal(row["TaxPct"]);
                nudTaxPct.Value = tPct > 100 ? 100 : tPct;

                // الأصناف
                var itemsDt = PurchaseDAL.GetItems(pid);
                _items.Clear();
                foreach (DataRow iRow in itemsDt.Rows)
                {
                    _items.Add(new PurchaseItemDTO
                    {
                        ProductID   = Convert.ToInt32(iRow["ProductID"]),
                        ProductName = iRow["ProductName"].ToString(),
                        Quantity    = Convert.ToDecimal(iRow["Quantity"]),
                        UnitPrice   = Convert.ToDecimal(iRow["UnitPrice"]),
                        DiscountPct = Convert.ToDecimal(iRow["DiscountPct"]),
                        DiscountAmt = Convert.ToDecimal(iRow["DiscountAmt"])
                    });
                }
                RefreshGrid();
                _isDirty = true;
                this.Text = $"فاتورة مشتريات — مستردة [مسودة #{pid}]";

                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            var btnDelDraft = Theme.MakeButton("❌ حذف المعلقة", 185, 6, 145, 35,
                Color.FromArgb(180, 60, 60));
            btnDelDraft.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDelDraft.Click += (s2, e2) =>
            {
                if (dgDrafts.SelectedRows.Count == 0) return;
                var row = (DataRowView)dgDrafts.SelectedRows[0].DataBoundItem;
                if (MessageBox.Show("هل أنت متأكد من حذف هذه الفاتورة المعلقة نهائياً؟",
                    "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    PurchaseDAL.DeleteDraftPurchase(Convert.ToInt32(row["PurchaseID"]));
                    dgDrafts.DataSource = PurchaseDAL.GetDraftPurchases();
                    if (((DataTable)dgDrafts.DataSource).Rows.Count == 0)
                        dlg.Close();
                }
            };

            pnlBtns.Controls.Add(btnLoad);
            pnlBtns.Controls.Add(btnDelDraft);
            dlg.Controls.Add(dgDrafts);
            dlg.Controls.Add(pnlBtns);
            dlg.ShowDialog(this);
        }

        // ══════════════════════════════════════════════════════════════════════
        // حفظ الفاتورة النهائية
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("أضف أصنافاً أولاً", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? supplierID = GetSelectedSupplier();
            if (_purchaseType == "Credit" && !supplierID.HasValue)
            {
                MessageBox.Show("اختر المورد أولاً للفواتير الآجلة", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal gross, discAmt, discPct, net, taxPct, taxAmt;
            CalcAmounts(out gross, out discAmt, out discPct, out net, out taxPct, out taxAmt);

            // إذا كنا نحفظ مسودة محملة — نحذفها أولاً
            if (_draftPurchaseID > 0)
            {
                try { PurchaseDAL.DeleteDraftPurchase(_draftPurchaseID); }
                catch { /* تجاهل */ }
                _draftPurchaseID = 0;
            }

            try
            {
                int id = PurchaseDAL.SavePurchase(
                    _purchaseType, supplierID, net, txtNotes.Text, _items,
                    discAmt, discPct, taxPct, taxAmt, isDraft: false);

                if (id > 0)
                {
                    _lastPurchaseID = id;
                    MessageBox.Show(
                        $"✅ تم حفظ فاتورة المشتريات بنجاح\nرقم الفاتورة: PUR-{id}" +
                        (taxAmt > 0 ? $"\n(شاملة ضريبة {taxPct:N2}% = {taxAmt:N2} ج)" : ""),
                        "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearInvoice();
                    LoadCombos();
                }
                else
                {
                    MessageBox.Show("❌ فشل حفظ الفاتورة", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل حفظ فاتورة المشتريات", ex, "FrmPurchase.BtnSave_Click");
                MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // مساعدات خاصة

        private int? GetSelectedSupplier()
        {
            if (cboSupplier.SelectedItem is ComboItem ci && ci.ID > 0)
                return ci.ID;
            return null;
        }

        private void CalcAmounts(
            out decimal gross, out decimal discAmt, out decimal discPct,
            out decimal net,   out decimal taxPct,  out decimal taxAmt)
        {
            gross = 0m;
            foreach (var item in _items) gross += item.TotalPrice;

            decimal rawDisc = 0m;
            decimal.TryParse(txtInvoiceDiscount.Text, out rawDisc);

            if (cboInvoiceDiscountType.SelectedIndex == 1) // نسبة %
            {
                discPct = rawDisc;
                discAmt = Math.Round(gross * discPct / 100m, 2);
            }
            else // مبلغ
            {
                discAmt = rawDisc;
                discPct = gross > 0 ? Math.Round(discAmt / gross * 100m, 2) : 0m;
            }

            decimal afterDisc = Math.Max(0m, gross - discAmt);

            taxPct = nudTaxPct.Value;
            taxAmt = Math.Round(afterDisc * taxPct / 100m, 2);

            net = afterDisc + taxAmt;
        }
    }
}

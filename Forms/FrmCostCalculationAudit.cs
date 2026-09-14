using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة فحص ومراجعة وتدقيق معادلة متوسط تكلفة الشراء المرجح للرصيد المتاح
    /// </summary>
    public class FrmCostCalculationAudit : Form
    {
        private readonly int _productId;
        private readonly string _productCode;
        private readonly string _productName;
        private readonly string _unitName;
        private readonly decimal _currentFactor;
        private readonly decimal _currentStock;
        private readonly decimal _currentCost;

        private decimal _calculatedAvgBase = 0m;
        private decimal _calculatedAvgDisplay = 0m;
        private string _formulaSummaryText = "";
        private List<CostAuditDetailItem> _auditItems = new List<CostAuditDetailItem>();

        private Label lblStockVal, lblCurrentCostVal, lblCurrentTotalVal, lblCalcCostVal;
        private Label lblAuditStatus;
        private TextBox txtFormula;
        private DataGridView dgInvoices;
        private Button btnApplyCost, btnCopyFormula, btnClose;

        public FrmCostCalculationAudit(
            int productId,
            string productCode,
            string productName,
            string unitName,
            decimal currentFactor,
            decimal currentStock,
            decimal currentCost)
        {
            _productId = productId;
            _productCode = productCode ?? "";
            _productName = productName ?? "";
            _unitName = !string.IsNullOrWhiteSpace(unitName) ? unitName : "وحدة";
            _currentFactor = currentFactor > 0m ? currentFactor : 1.0m;
            _currentStock = currentStock;
            _currentCost = currentCost;

            InitUI();
            LoadAuditData();
        }

        private void InitUI()
        {
            this.Text = $"🧮 مراجعة واحتساب متوسط تكلفة الصنف - {_productName}";
            this.Size = new Size(1020, 740);
            this.MinimumSize = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── 1. العنوان الرئيسي ──────────────────────────────────────────
            var pnlTitle = Theme.MakeTitleBar(
                "🧮 مراجعة وتدقيق معادلة متوسط تكلفة الشراء المرجح",
                $"{_productName}  |  كود: {_productCode}  |  الوحدة: {_unitName}  |  الرصيد المتاح: {_currentStock:G29} {_unitName}");
            pnlTitle.Dock = DockStyle.Top;
            this.Controls.Add(pnlTitle);

            // ── 2. بطاقات الإحصائيات العلوية ──────────────────────────────────
            var pnlCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 10, 12, 5),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = false
            };

            pnlCards.Controls.Add(MakeSummaryCard("📦 الرصيد المتاح حالياً", $"{_currentStock:N2} {_unitName}", Color.FromArgb(13, 110, 253), out lblStockVal));
            pnlCards.Controls.Add(MakeSummaryCard("💵 متوسط التكلفة المسجل", $"{_currentCost:N2} ج", Color.FromArgb(108, 117, 125), out lblCurrentCostVal));
            pnlCards.Controls.Add(MakeSummaryCard("💰 إجمالي قيمة المخزون", $"{(_currentStock * _currentCost):N2} ج", Color.FromArgb(25, 135, 84), out lblCurrentTotalVal));
            pnlCards.Controls.Add(MakeSummaryCard("🧮 التكلفة المحسوبة دفترياً", "0.00 ج", Color.FromArgb(217, 119, 6), out lblCalcCostVal));

            this.Controls.Add(pnlCards);

            // ── 3. شريط أزرار التحكم السفلي ───────────────────────────────────
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            lblAuditStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "جاري فحص وتدقيق الفواتير..."
            };

            var pnlBottomBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnApplyCost = new Button
            {
                Text = "🔄 اعتماد وتحديث متوسط التكلفة للصنف",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(25, 135, 84),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(240, 36),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 0)
            };
            btnApplyCost.FlatAppearance.BorderSize = 0;
            btnApplyCost.Click += (s, e) => ApplyCalculatedCost();

            btnCopyFormula = new Button
            {
                Text = "📋 نسخ المعادلة",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 36),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 0)
            };
            btnCopyFormula.FlatAppearance.BorderSize = 0;
            btnCopyFormula.Click += (s, e) => CopyFormulaToClipboard();

            btnClose = new Button
            {
                Text = "❌ إغلاق",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(95, 36),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlBottomBtns.Controls.Add(btnApplyCost);
            pnlBottomBtns.Controls.Add(btnCopyFormula);
            pnlBottomBtns.Controls.Add(btnClose);

            pnlBottom.Controls.Add(lblAuditStatus);
            pnlBottom.Controls.Add(pnlBottomBtns);
            this.Controls.Add(pnlBottom);

            // ── 4. المحتوى الأوسط (المعادلة وجدول الفواتير) ───────────────────
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 210,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(226, 232, 240)
            };

            // النصف العلوي: بطاقة المعادلة والتعويض بالأرقام
            var pnlFormulaBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 10, 12, 10)
            };

            var lblFormulaHeader = new Label
            {
                Text = "📐 تفاصيل المعادلة الحسابية والتعويض الرقمي من واقع الفواتير:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            txtFormula = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Consolas", 10f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlFormulaBox.Controls.Add(txtFormula);
            pnlFormulaBox.Controls.Add(lblFormulaHeader);
            splitContainer.Panel1.Controls.Add(pnlFormulaBox);

            // النصف السفلي: جدول فواتير الشراء
            var pnlGridBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 10, 12, 10)
            };

            var lblGridHeader = new Label
            {
                Text = "📋 سجل فواتير الشراء السابقة وحصة كل فاتورة في تغطية الرصيد المتاح:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            dgInvoices = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Theme.ApplyGridTheme(dgInvoices);

            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseCode",         HeaderText = "رقم الفاتورة",     FillWeight = 42 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierInvoiceNo",    HeaderText = "فاتورة المورد",    FillWeight = 45 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseDate",          HeaderText = "التاريخ",          FillWeight = 48 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName",          HeaderText = "المورد",           FillWeight = 75 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",              HeaderText = "الكمية",           FillWeight = 40 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "BonusQuantity",         HeaderText = "بونص مجاني",       FillWeight = 38 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName",              HeaderText = "الوحدة",           FillWeight = 38 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",             HeaderText = "سعر الشراء",       FillWeight = 44 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineDiscount",          HeaderText = "خصم البند",        FillWeight = 40 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "HeaderDiscountShare",   HeaderText = "حصة خصم الفاتورة", FillWeight = 45 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShippingShare",         HeaderText = "حصة النقل (+)",    FillWeight = 42 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetUnitCost",           HeaderText = "صافي الوحدة الواصلة", FillWeight = 52 });
            dgInvoices.Columns.Add(new DataGridViewTextBoxColumn { Name = "CoverageStatus",        HeaderText = "حالة تغطية الرصيد", FillWeight = 75 });

            pnlGridBox.Controls.Add(dgInvoices);
            pnlGridBox.Controls.Add(lblGridHeader);
            splitContainer.Panel2.Controls.Add(pnlGridBox);

            this.Controls.Add(splitContainer);
            splitContainer.BringToFront();
        }

        private Panel MakeSummaryCard(string title, string val, Color accentColor, out Label lblValue)
        {
            var card = new Panel
            {
                Size = new Size(230, 68),
                BackColor = Color.FromArgb(248, 250, 252),
                Margin = new Padding(6, 0, 6, 0),
                Padding = new Padding(10, 8, 10, 8)
            };

            var pnlIndicator = new Panel
            {
                Dock = DockStyle.Right,
                Width = 4,
                BackColor = accentColor
            };

            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Height = 20
            };

            lblValue = new Label
            {
                Text = val,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = accentColor,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblT);
            card.Controls.Add(pnlIndicator);

            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            return card;
        }

        private void LoadAuditData()
        {
            try
            {
                decimal availableBaseQty = _currentStock * _currentFactor;
                var result = PurchaseDAL.AuditProductCostFromHistory(_productId, availableBaseQty, _currentFactor);

                _calculatedAvgBase = result.avgCostInBaseUnit;
                _calculatedAvgDisplay = result.avgCostInDisplayUnit;
                _formulaSummaryText = result.formulaSummary;
                _auditItems = result.items;

                lblCalcCostVal.Text = $"{_calculatedAvgDisplay:N2} ج";
                txtFormula.Text = _formulaSummaryText;

                // ملء الجدول
                dgInvoices.Rows.Clear();
                foreach (var item in _auditItems)
                {
                    int ri = dgInvoices.Rows.Add(
                        item.PurchaseCode,
                        item.SupplierInvoiceNo,
                        item.PurchaseDate.ToString("yyyy-MM-dd"),
                        item.SupplierName,
                        item.Quantity.ToString("G29"),
                        item.BonusQuantity.ToString("G29"),
                        item.UnitName,
                        item.UnitPrice.ToString("N2"),
                        item.LineDiscount.ToString("N2"),
                        item.HeaderDiscountShare.ToString("N2"),
                        item.ShippingShare.ToString("N2"),
                        item.NetUnitCostInDisplayUnit.ToString("N2"),
                        item.CoverageStatus
                    );

                    // تلوين السطور المغطية للرصيد
                    if (item.CoveredQtyInBaseUnit > 0m)
                    {
                        dgInvoices.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244);
                        dgInvoices.Rows[ri].DefaultCellStyle.ForeColor = Color.FromArgb(20, 83, 45);
                    }
                    else
                    {
                        dgInvoices.Rows[ri].DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
                    }
                }

                // تدقيق الحالة والمطابقة
                decimal diff = Math.Abs(_currentCost - _calculatedAvgDisplay);
                if (diff < 0.015m)
                {
                    lblAuditStatus.Text = $"✅ متوسط التكلفة المسجل ({_currentCost:N2} ج) مطابق تماماً لحساب فواتير المشتريات.";
                    lblAuditStatus.ForeColor = Color.FromArgb(22, 101, 52);
                }
                else
                {
                    lblAuditStatus.Text = $"⚠️ يوجد فارق: المسجل ({_currentCost:N2} ج) مقابل المحسوب بالفواتير ({_calculatedAvgDisplay:N2} ج) بفارق ({diff:N2} ج).";
                    lblAuditStatus.ForeColor = Color.FromArgb(180, 83, 9);
                }
            }
            catch (Exception ex)
            {
                txtFormula.Text = "حدث خطأ أثناء تحميل بيانات التدقيق: " + ex.Message;
                AppLogger.Error("Failed to audit product cost", ex, "FrmCostCalculationAudit.LoadAuditData");
            }
        }

        private void ApplyCalculatedCost()
        {
            if (_calculatedAvgBase <= 0m)
            {
                Theme.ShowMsg("لا توجد تكلفة محسوبة صالحة للاعتماد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"هل تريد بالتأكيد اعتماد متوسط التكلفة المحسوب دفترياً ({_calculatedAvgDisplay:N2} ج للوحدة {_unitName}) وتحديثه للصنف في النظام؟",
                "تأكيد تحديث التكلفة",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    PurchaseDAL.ApplyCalculatedAverageCost(_productId, _calculatedAvgBase);
                    Theme.ShowMsg("✅ تم تحديث وتصحيح متوسط التكلفة للصنف بنجاح.", "تم التحديث", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    Theme.ShowMsg("فشل تحديث التكلفة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CopyFormulaToClipboard()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_formulaSummaryText))
                {
                    Clipboard.SetText(_formulaSummaryText);
                    Theme.ShowMsg("تم نسخ تفاصيل المعادلة والتعويض الرقمي للحافظة بنجاح.", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch { }
        }
    }
}

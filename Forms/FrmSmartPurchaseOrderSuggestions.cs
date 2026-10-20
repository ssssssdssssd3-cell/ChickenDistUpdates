using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة الاقتراح الذكي لأصناف أمر الشراء والطلبيات بناءً على معدل المبيعات، الرصيد، وحد الطلب
    /// </summary>
    public class FrmSmartPurchaseOrderSuggestions : Form
    {
        public List<SuggestedPurchaseItemDTO> SelectedItems { get; private set; } = new List<SuggestedPurchaseItemDTO>();

        private int? _supplierID;
        private string _supplierName;

        private CheckBox chkMaxStock, chkFastMoving, chkShortages;
        private NumericUpDown nudMaxStock, nudSalesDays;
        private TextBox txtSearch;
        private Button btnRefresh, btnSelectAll, btnDeselectAll, btnAddSelected, btnClose;
        private DataGridView dgvSuggestions;
        private Label lblCount;

        private List<SuggestedPurchaseItemDTO> _currentSuggestions = new List<SuggestedPurchaseItemDTO>();

        public FrmSmartPurchaseOrderSuggestions(int? supplierID = null, string supplierName = "")
        {
            _supplierID = supplierID;
            _supplierName = supplierName;
            InitUI();
            LoadSuggestions();
        }

        private void InitUI()
        {
            this.Text = "💡 الاقتراح الذكي لأصناف أمر الشراء (حركة المبيعات والأرصدة)";
            this.Size = new Size(1180, 720);
            this.MinimumSize = new Size(980, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. ترويسة
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            string supTitle = !string.IsNullOrWhiteSpace(_supplierName) ? $" للمورد [{_supplierName}]" : "";
            var lblTitle = new Label
            {
                Text = $"💡 محرك الاقتراح الذكي لأمر الشراء{supTitle}",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 26
            };

            var lblSub = new Label
            {
                Text = "تحليل آلي فوري لحركة المبيعات وسرعة استهلاك الأصناف لحساب الكميات المطلوبة بدقة وتفادي نفاد المخزون",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // 2. شريط فلاتر الاقتراح الذكي
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 6, 12, 8),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            // فلتر أقصى رصيد
            chkMaxStock = new CheckBox
            {
                Text = "رصيد الصنف أقل من أو يساوي:",
                AutoSize = true,
                Checked = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain,
                Margin = new Padding(3, 7, 2, 0)
            };
            nudMaxStock = new NumericUpDown
            {
                Width = 70,
                Minimum = 0,
                Maximum = 99999,
                Value = 5,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(2, 4, 15, 4)
            };

            // فلتر سريعة البيع
            chkFastMoving = new CheckBox
            {
                Text = "أصناف سريعة البيع ومعدل استهلاكها عالي خلال آخر:",
                AutoSize = true,
                Checked = false,
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(180, 83, 9),
                Margin = new Padding(3, 7, 2, 0)
            };
            nudSalesDays = new NumericUpDown
            {
                Width = 60,
                Minimum = 1,
                Maximum = 365,
                Value = 14,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(2, 4, 2, 4)
            };
            var lblDays = new Label { Text = "يوم", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(1, 7, 15, 0) };

            // كشكول النواقص
            chkShortages = new CheckBox
            {
                Text = "تشمل مسجلات كشكول النواقص",
                AutoSize = true,
                Checked = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain,
                Margin = new Padding(3, 7, 15, 0)
            };

            // بحث سريع
            var lblSearch = new Label { Text = "🔍 بحث:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(2, 7, 2, 0) };
            txtSearch = new TextBox { Width = 140, Height = 26, Margin = new Padding(2, 4, 8, 4) };
            txtSearch.TextChanged += (s, e) => ApplyLocalSearch();

            btnRefresh = Theme.MakeButton("🔄 تحديث التحليل", 0, 0, 125, 30, Color.FromArgb(37, 99, 235));
            btnRefresh.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            btnRefresh.Click += (s, e) => LoadSuggestions();

            pnlFilter.Controls.Add(chkMaxStock);
            pnlFilter.Controls.Add(nudMaxStock);
            pnlFilter.Controls.Add(chkFastMoving);
            pnlFilter.Controls.Add(nudSalesDays);
            pnlFilter.Controls.Add(lblDays);
            pnlFilter.Controls.Add(chkShortages);
            pnlFilter.Controls.Add(lblSearch);
            pnlFilter.Controls.Add(txtSearch);
            pnlFilter.Controls.Add(btnRefresh);

            // 3. شريط التحكم السفلي
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnAddSelected = Theme.MakeButton("➕ إضافة الأصناف المحددة لأمر الشراء", 0, 0, 260, 36, Color.FromArgb(5, 150, 105));
            btnAddSelected.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnAddSelected.Dock = DockStyle.Left;
            btnAddSelected.Click += (s, e) => AddSelectedToOrder();

            btnSelectAll = Theme.MakeButton("✔️ تحديد الكل", 0, 0, 105, 36, Color.FromArgb(70, 80, 95));
            btnSelectAll.Dock = DockStyle.Right;
            btnSelectAll.Click += (s, e) => SetAllSelection(true);

            btnDeselectAll = Theme.MakeButton("❌ إلغاء التحديد", 0, 0, 115, 36, Color.FromArgb(100, 116, 139));
            btnDeselectAll.Dock = DockStyle.Right;
            btnDeselectAll.Click += (s, e) => SetAllSelection(false);

            btnClose = Theme.MakeButton("إغلاق", 0, 0, 90, 36, Color.FromArgb(148, 163, 184));
            btnClose.Dock = DockStyle.Right;
            btnClose.Click += (s, e) => this.Close();

            lblCount = new Label
            {
                Text = "عدد الأصناف المقترحة: 0",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };

            pnlBottom.Controls.Add(btnAddSelected);
            pnlBottom.Controls.Add(lblCount);
            pnlBottom.Controls.Add(btnSelectAll);
            pnlBottom.Controls.Add(btnDeselectAll);
            pnlBottom.Controls.Add(btnClose);

            // 4. جدول الاقتراحات
            dgvSuggestions = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RightToLeft = RightToLeft.Yes,
                RowTemplate = { Height = 34 }
            };

            SetupGridColumns();

            this.Controls.Add(dgvSuggestions);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlHeader);
        }

        private void SetupGridColumns()
        {
            dgvSuggestions.Columns.Clear();

            // 0. Checkbox
            var colCheck = new DataGridViewCheckBoxColumn
            {
                Name = "colSelect",
                HeaderText = "تحديد",
                Width = 60,
                ReadOnly = false,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dgvSuggestions.Columns.Add(colCheck);

            // 1. ProductID
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPID", Visible = false });

            // 2. كود
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCode",
                HeaderText = "كود الصنف",
                Width = 110,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 3. اسم الصنف
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "اسم الصنف",
                Width = 240,
                ReadOnly = true,
                DefaultCellStyle = { Font = Theme.FontBold }
            });

            // 4. القسم
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCat",
                HeaderText = "القسم",
                Width = 100,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 5. الوحدة
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colUnit",
                HeaderText = "الوحدة",
                Width = 70,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 6. الرصيد الحالي
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStock",
                HeaderText = "الرصيد الحالي",
                Width = 105,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = Theme.FontBold }
            });

            // 7. مبيعات الفترة
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSold",
                HeaderText = "مبيعات الفترة",
                Width = 100,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 8. الكمية المقترحة (قابلة للتعديل)
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSuggested",
                HeaderText = "الكمية المقترحة ✏️",
                Width = 130,
                ReadOnly = false,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10f, FontStyle.Bold), BackColor = Color.FromArgb(240, 253, 244), ForeColor = Color.FromArgb(21, 128, 61) }
            });

            // 9. سعر الشراء المتوقع (قابل للتعديل)
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrice",
                HeaderText = "سعر الشراء ✏️",
                Width = 120,
                ReadOnly = false,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2", Font = Theme.FontBold }
            });

            // 10. سبب الاقتراح
            dgvSuggestions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colReason",
                HeaderText = "سبب الاقتراح والتحليل الذكي 📊",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true,
                DefaultCellStyle = { ForeColor = Color.FromArgb(180, 83, 9) }
            });
        }

        private void LoadSuggestions()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                decimal? threshold = chkMaxStock.Checked ? (decimal?)nudMaxStock.Value : null;
                bool fastMoving = chkFastMoving.Checked;
                int days = (int)nudSalesDays.Value;
                bool shortages = chkShortages.Checked;

                _currentSuggestions = PurchaseOrderDAL.GetSmartSuggestedProducts(
                    supplierID: _supplierID,
                    maxStockThreshold: threshold,
                    fastMovingOnly: fastMoving,
                    salesDays: days,
                    includeShortages: shortages
                );

                RenderGridRows(_currentSuggestions);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحليل الاقتراحات الذكية:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error("FrmSmartPurchaseOrderSuggestions.LoadSuggestions", ex);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void ApplyLocalSearch()
        {
            string s = txtSearch.Text?.Trim().ToLowerInvariant() ?? "";
            if (string.IsNullOrEmpty(s))
            {
                RenderGridRows(_currentSuggestions);
                return;
            }

            var filtered = _currentSuggestions.Where(x =>
                (x.ProductName != null && x.ProductName.ToLowerInvariant().Contains(s)) ||
                (x.ProductCode != null && x.ProductCode.ToLowerInvariant().Contains(s)) ||
                (x.CategoryName != null && x.CategoryName.ToLowerInvariant().Contains(s))
            ).ToList();

            RenderGridRows(filtered);
        }

        private void RenderGridRows(List<SuggestedPurchaseItemDTO> items)
        {
            dgvSuggestions.Rows.Clear();
            foreach (var item in items)
            {
                int rIdx = dgvSuggestions.Rows.Add(
                    true, // محدد تلقائياً
                    item.ProductID,
                    item.ProductCode,
                    item.ProductName,
                    item.CategoryName,
                    item.UnitName,
                    item.CurrentStock.ToString("N2"),
                    item.SoldQtyInPeriod.ToString("N0"),
                    item.SuggestedQuantity.ToString("N0"),
                    item.LastPurchasePrice.ToString("N2"),
                    item.SuggestionReason
                );

                var row = dgvSuggestions.Rows[rIdx];
                row.Tag = item;

                if (item.CurrentStock <= 0)
                {
                    row.Cells["colStock"].Style.ForeColor = Color.FromArgb(220, 38, 38);
                }
                else
                {
                    row.Cells["colStock"].Style.ForeColor = Color.FromArgb(217, 119, 6);
                }
            }

            lblCount.Text = $"عدد الأصناف المقترحة: {dgvSuggestions.Rows.Count}";
        }

        private void SetAllSelection(bool select)
        {
            foreach (DataGridViewRow row in dgvSuggestions.Rows)
            {
                row.Cells["colSelect"].Value = select;
            }
        }

        private void AddSelectedToOrder()
        {
            SelectedItems.Clear();

            foreach (DataGridViewRow row in dgvSuggestions.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["colSelect"].Value ?? false);
                if (!isSelected) continue;

                var item = row.Tag as SuggestedPurchaseItemDTO;
                if (item == null) continue;

                // قراءة التعديلات التي قام بها المستخدم على الكمية والسعر في الجدول
                if (decimal.TryParse(row.Cells["colSuggested"].Value?.ToString(), out decimal editedQty) && editedQty > 0)
                {
                    item.SuggestedQuantity = editedQty;
                }
                if (decimal.TryParse(row.Cells["colPrice"].Value?.ToString(), out decimal editedPrice) && editedPrice >= 0)
                {
                    item.LastPurchasePrice = editedPrice;
                }

                SelectedItems.Add(item);
            }

            if (SelectedItems.Count == 0)
            {
                MessageBox.Show("يرجى تحديد صنف واحد على الأقل لإضافته لأمر الشراء.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

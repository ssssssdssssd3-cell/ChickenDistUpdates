using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة تحديد مواد التصنيع (شجرة المنتج - Bill of Materials / BOM)
    /// </summary>
    public class FrmBOM : Form
    {
        private int _currentBOMID = 0;
        private int _selectedFinishedProductID = 0;
        private string _selectedFinishedProductCode = "";
        private string _selectedFinishedProductName = "";

        // Controls - Top Panel (Finished Product)
        private TextBox txtFinishedProduct;
        private Button btnBrowseFinished;
        private NumericUpDown numOutputQty;
        private TextBox txtUnitName;
        private TextBox txtNotes;

        // Controls - Raw Material Quick Add Bar
        private int _selectedRawProductID = 0;
        private string _selectedRawProductCode = "";
        private string _selectedRawProductName = "";
        private decimal _selectedRawCostPrice = 0;
        private TextBox txtRawProduct;
        private Button btnBrowseRaw;
        private NumericUpDown numRawQty;
        private TextBox txtRawUnit;
        private Label lblRawCost;
        private Button btnAddRaw;

        // Grid
        private DataGridView dgItems;

        // Summary Cards
        private Label lblTotalRawCost;
        private Label lblUnitCost;
        private Label lblItemsCount;

        // Buttons
        private Button btnSave;
        private Button btnNew;
        private Button btnDelete;
        private Button btnPrint;

        // Side List of Saved BOMs
        private TextBox txtSearchBOM;
        private DataGridView dgBOMList;

        public FrmBOM(int preselectedProductID = 0)
        {
            _selectedFinishedProductID = preselectedProductID;
            InitUI();
            LoadSavedBOMsList();

            if (_selectedFinishedProductID > 0)
            {
                LoadFinishedProductByID(_selectedFinishedProductID);
            }
        }

        private void InitUI()
        {
            this.Text = "🏭 شجرة ومكونات التصنيع (BOM) - تحديد وتعديل وصفات الإنتاج";
            this.Size = new Size(1150, 720);
            this.MinimumSize = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Main Layout: Left side for BOM List (320px), Right side for active BOM editor
            var pnlMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.BgMain
            };
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330f)); // قائمة الوصفات المحفوظة
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // محرر الوصفة الحالية
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.Controls.Add(pnlMain);

            // ── 1. القائمة الجانبية (الوصفات المحفوظة) ──
            var pnlSide = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };
            pnlMain.Controls.Add(pnlSide, 0, 0);

            var lblSideTitle = new Label
            {
                Text = "📋 الوصفات وشجر الإنتاج المسجلة",
                Dock = DockStyle.Top,
                Height = 30,
                Font = Theme.FontBold,
                ForeColor = Theme.Accent
            };
            pnlSide.Controls.Add(lblSideTitle);

            txtSearchBOM = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            txtSearchBOM.TextChanged += (s, e) => LoadSavedBOMsList(txtSearchBOM.Text.Trim());
            pnlSide.Controls.Add(txtSearchBOM);

            var lblSearchHint = new Label
            {
                Text = "🔍 بحث بالاسم أو الكود:",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.Gray,
                Font = Theme.FontSmall
            };
            pnlSide.Controls.Add(lblSearchHint);

            dgBOMList = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "BOMID", Visible = false });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "المنتج النهائي", FillWeight = 60 });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "OutputQty", HeaderText = "الكمية", FillWeight = 20 });
            dgBOMList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemsCount", HeaderText = "الخامات", FillWeight = 20 });
            dgBOMList.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int bomId = Convert.ToInt32(dgBOMList.Rows[e.RowIndex].Cells["BOMID"].Value);
                    LoadBOMByID(bomId);
                }
            };
            pnlSide.Controls.Add(dgBOMList);
            dgBOMList.BringToFront();

            // ── 2. المحرر الرئيسي للوصفة ──
            var pnlEditor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgMain,
                Padding = new Padding(15, 10, 15, 10)
            };
            pnlMain.Controls.Add(pnlEditor, 1, 0);

            // Header of Editor
            var pnlTopHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 105,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };
            pnlEditor.Controls.Add(pnlTopHeader);

            var lblFinishedTitle = new Label
            {
                Text = "🎯 المنتج النهائي المصنع (Finished Product):",
                Location = new Point(15, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.Accent
            };
            pnlTopHeader.Controls.Add(lblFinishedTitle);

            txtFinishedProduct = new TextBox
            {
                Location = new Point(15, 38),
                Width = 320,
                Height = 32,
                ReadOnly = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };
            pnlTopHeader.Controls.Add(txtFinishedProduct);

            btnBrowseFinished = Theme.MakeButton("🔍 اختيار صنف", 345, 36, 120, 34, Theme.Primary);
            btnBrowseFinished.Click += (s, e) => SelectFinishedProduct();
            pnlTopHeader.Controls.Add(btnBrowseFinished);

            var lblQty = new Label { Text = "كمية الإنتاج المعيارية:", Location = new Point(480, 15), AutoSize = true };
            pnlTopHeader.Controls.Add(lblQty);

            numOutputQty = new NumericUpDown
            {
                Location = new Point(480, 38),
                Width = 100,
                Height = 32,
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 1000000m,
                Value = 1m,
                Font = Theme.FontBold,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            numOutputQty.ValueChanged += (s, e) => RecalculateTotals();
            pnlTopHeader.Controls.Add(numOutputQty);

            var lblUnit = new Label { Text = "الوحدة:", Location = new Point(595, 15), AutoSize = true };
            pnlTopHeader.Controls.Add(lblUnit);

            txtUnitName = new TextBox
            {
                Location = new Point(595, 38),
                Width = 90,
                Height = 32,
                Text = "قطعة",
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlTopHeader.Controls.Add(txtUnitName);

            var lblNotes = new Label { Text = "ملاحظات الوصفة المعيارية:", Location = new Point(700, 15), AutoSize = true };
            pnlTopHeader.Controls.Add(lblNotes);

            txtNotes = new TextBox
            {
                Location = new Point(700, 38),
                Width = 240,
                Height = 32,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlTopHeader.Controls.Add(txtNotes);

            // ── Quick Add Raw Material Bar ──
            var pnlQuickAdd = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10)
            };
            pnlEditor.Controls.Add(pnlQuickAdd);
            pnlQuickAdd.BringToFront();

            var lblAddRawTitle = new Label
            {
                Text = "📦 المادة الخام:",
                Location = new Point(15, 8),
                AutoSize = true,
                ForeColor = Color.WhiteSmoke
            };
            pnlQuickAdd.Controls.Add(lblAddRawTitle);

            txtRawProduct = new TextBox
            {
                Location = new Point(15, 28),
                Width = 260,
                Height = 30,
                ReadOnly = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlQuickAdd.Controls.Add(txtRawProduct);

            btnBrowseRaw = Theme.MakeButton("🔍 بحث خامات", 280, 26, 110, 32, Color.FromArgb(51, 65, 85));
            btnBrowseRaw.Click += (s, e) => SelectRawProduct();
            pnlQuickAdd.Controls.Add(btnBrowseRaw);

            var lblRawQtyTitle = new Label { Text = "الكمية المعيارية:", Location = new Point(400, 8), AutoSize = true, ForeColor = Color.WhiteSmoke };
            pnlQuickAdd.Controls.Add(lblRawQtyTitle);

            numRawQty = new NumericUpDown
            {
                Location = new Point(400, 28),
                Width = 90,
                Height = 30,
                DecimalPlaces = 3,
                Minimum = 0.001m,
                Maximum = 1000000m,
                Value = 1m,
                Font = Theme.FontBold,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlQuickAdd.Controls.Add(numRawQty);

            var lblRawUnitTitle = new Label { Text = "الوحدة:", Location = new Point(500, 8), AutoSize = true, ForeColor = Color.WhiteSmoke };
            pnlQuickAdd.Controls.Add(lblRawUnitTitle);

            txtRawUnit = new TextBox
            {
                Location = new Point(500, 28),
                Width = 80,
                Height = 30,
                Text = "قطعة",
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            pnlQuickAdd.Controls.Add(txtRawUnit);

            lblRawCost = new Label
            {
                Text = "التكلفة: 0.00 ج.م",
                Location = new Point(590, 30),
                Width = 140,
                ForeColor = Color.FromArgb(243, 198, 35),
                Font = Theme.FontBold
            };
            pnlQuickAdd.Controls.Add(lblRawCost);

            btnAddRaw = Theme.MakeButton("➕ إضافة للشجرة", 740, 24, 140, 34, Color.FromArgb(34, 197, 94));
            btnAddRaw.Click += (s, e) => AddCurrentRawToGrid();
            pnlQuickAdd.Controls.Add(btnAddRaw);

            // ── Grid of Raw Materials ──
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowNum", HeaderText = "م", FillWeight = 8, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductCode", HeaderText = "كود المادة الخام", FillWeight = 18, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "RawProductName", HeaderText = "اسم المادة الخام", FillWeight = 36, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المطلوبة", FillWeight = 16 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 12 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "سعر التكلفة", FillWeight = 15, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "إجمالي التكلفة", FillWeight = 18, ReadOnly = true });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 20 });

            var colDelete = new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "حذف",
                Text = "❌",
                UseColumnTextForButtonValue = true,
                FillWeight = 10
            };
            dgItems.Columns.Add(colDelete);

            dgItems.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "Quantity")
                {
                    UpdateRowTotal(e.RowIndex);
                    RecalculateTotals();
                }
            };
            dgItems.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dgItems.Columns["colDelete"].Index)
                {
                    dgItems.Rows.RemoveAt(e.RowIndex);
                    ReindexGrid();
                    RecalculateTotals();
                }
            };
            pnlEditor.Controls.Add(dgItems);
            dgItems.BringToFront();

            // ── Bottom Summary & Action Bar ──
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };
            pnlEditor.Controls.Add(pnlBottom);

            // Summary Labels
            lblItemsCount = new Label
            {
                Text = "📦 عدد المكونات: 0",
                Location = new Point(15, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };
            pnlBottom.Controls.Add(lblItemsCount);

            lblTotalRawCost = new Label
            {
                Text = "💰 إجمالي تكلفة الخامات: 0.00 ج.م",
                Location = new Point(180, 12),
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(243, 198, 35)
            };
            pnlBottom.Controls.Add(lblTotalRawCost);

            lblUnitCost = new Label
            {
                Text = "🏷️ تكلفة الوحدة المعيارية: 0.00 ج.م",
                Location = new Point(460, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94)
            };
            pnlBottom.Controls.Add(lblUnitCost);

            // Action Buttons
            btnSave = Theme.MakeButton("💾 حفظ شجرة التصنيع", 15, 42, 170, 36, Theme.Accent);
            btnSave.Click += (s, e) => SaveCurrentBOM();
            pnlBottom.Controls.Add(btnSave);

            btnNew = Theme.MakeButton("➕ وصفة جديدة", 195, 42, 130, 36, Color.FromArgb(51, 65, 85));
            btnNew.Click += (s, e) => ResetForm();
            pnlBottom.Controls.Add(btnNew);

            btnDelete = Theme.MakeButton("🗑️ حذف الوصفة", 335, 42, 130, 36, Color.FromArgb(220, 53, 69));
            btnDelete.Click += (s, e) => DeleteCurrentBOM();
            pnlBottom.Controls.Add(btnDelete);

            btnPrint = Theme.MakeButton("🖨️ طباعة قائمة المكونات", 475, 42, 180, 36, Color.FromArgb(40, 120, 180));
            btnPrint.Click += (s, e) => PrintBOM();
            pnlBottom.Controls.Add(btnPrint);
        }

        private void SelectFinishedProduct()
        {
            using (var frm = new FrmProductSearch())
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    LoadFinishedProductByID(frm.SelectedProductID);
                }
            }
        }

        private void LoadFinishedProductByID(int productId)
        {
            var dt = DbHelper.Query("SELECT ProductID, ProductCode, ProductName, CostPrice, UnitName FROM Products WHERE ProductID = @id",
                DbHelper.P("@id", productId));

            if (dt != null && dt.Rows.Count > 0)
            {
                _selectedFinishedProductID = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                _selectedFinishedProductCode = dt.Rows[0]["ProductCode"]?.ToString();
                _selectedFinishedProductName = dt.Rows[0]["ProductName"]?.ToString();
                txtFinishedProduct.Text = $"{_selectedFinishedProductCode} - {_selectedFinishedProductName}";
                txtUnitName.Text = dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة";

                // Check if existing BOM exists
                var existing = ProductionDAL.GetBOMByProductID(_selectedFinishedProductID);
                if (existing != null)
                {
                    LoadBOM(existing);
                }
                else
                {
                    _currentBOMID = 0;
                    dgItems.Rows.Clear();
                    numOutputQty.Value = 1m;
                    RecalculateTotals();
                }
            }
        }

        private void SelectRawProduct()
        {
            using (var frm = new FrmProductSearch())
            {
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    if (frm.SelectedProductID == _selectedFinishedProductID)
                    {
                        MessageBox.Show("لا يمكن اختيار نفس الصنف النهائي كمادة خام لنفسه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var dt = DbHelper.Query("SELECT ProductID, ProductCode, ProductName, CostPrice, UnitName FROM Products WHERE ProductID = @id",
                        DbHelper.P("@id", frm.SelectedProductID));

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        _selectedRawProductID = Convert.ToInt32(dt.Rows[0]["ProductID"]);
                        _selectedRawProductCode = dt.Rows[0]["ProductCode"]?.ToString();
                        _selectedRawProductName = dt.Rows[0]["ProductName"]?.ToString();
                        _selectedRawCostPrice = Convert.ToDecimal(dt.Rows[0]["CostPrice"] ?? 0);

                        txtRawProduct.Text = $"{_selectedRawProductCode} - {_selectedRawProductName}";
                        txtRawUnit.Text = dt.Rows[0]["UnitName"]?.ToString() ?? "قطعة";
                        lblRawCost.Text = $"التكلفة: {_selectedRawCostPrice:N2} ج.م";
                        numRawQty.Focus();
                    }
                }
            }
        }

        private void AddCurrentRawToGrid()
        {
            if (_selectedRawProductID <= 0)
            {
                MessageBox.Show("يرجى اختيار مادة خام أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal qty = numRawQty.Value;
            if (qty <= 0)
            {
                MessageBox.Show("الكمية يجب أن تكون أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if already in grid
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (Convert.ToInt32(row.Cells["RawProductID"].Value) == _selectedRawProductID)
                {
                    decimal cur = Convert.ToDecimal(row.Cells["Quantity"].Value);
                    row.Cells["Quantity"].Value = cur + qty;
                    UpdateRowTotal(row.Index);
                    RecalculateTotals();
                    ClearRawInputs();
                    return;
                }
            }

            decimal tot = qty * _selectedRawCostPrice;
            int rIdx = dgItems.Rows.Add(
                _selectedRawProductID,
                dgItems.Rows.Count + 1,
                _selectedRawProductCode,
                _selectedRawProductName,
                qty,
                txtRawUnit.Text.Trim(),
                _selectedRawCostPrice.ToString("N2"),
                tot.ToString("N2"),
                "",
                "❌"
            );

            ClearRawInputs();
            RecalculateTotals();
        }

        private void ClearRawInputs()
        {
            _selectedRawProductID = 0;
            _selectedRawProductCode = "";
            _selectedRawProductName = "";
            _selectedRawCostPrice = 0;
            txtRawProduct.Clear();
            numRawQty.Value = 1m;
            lblRawCost.Text = "التكلفة: 0.00 ج.م";
        }

        private void UpdateRowTotal(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgItems.Rows.Count) return;
            var row = dgItems.Rows[rowIndex];
            decimal qty = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0);
            decimal cost = Convert.ToDecimal(row.Cells["UnitCost"].Value ?? 0);
            row.Cells["TotalCost"].Value = (qty * cost).ToString("N2");
        }

        private void ReindexGrid()
        {
            for (int i = 0; i < dgItems.Rows.Count; i++)
            {
                dgItems.Rows[i].Cells["RowNum"].Value = i + 1;
            }
        }

        private void RecalculateTotals()
        {
            decimal totalCost = 0;
            int count = dgItems.Rows.Count;

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                decimal tot = Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0);
                totalCost += tot;
            }

            decimal outQty = numOutputQty.Value > 0 ? numOutputQty.Value : 1m;
            decimal unitCost = totalCost / outQty;

            lblItemsCount.Text = $"📦 عدد المكونات: {count}";
            lblTotalRawCost.Text = $"💰 إجمالي تكلفة الخامات: {totalCost:N2} ج.م";
            lblUnitCost.Text = $"🏷️ تكلفة الوحدة المعيارية: {unitCost:N2} ج.م";
        }

        private void SaveCurrentBOM()
        {
            if (_selectedFinishedProductID <= 0)
            {
                MessageBox.Show("يرجى اختيار المنتج النهائي المراد تحديد مواد تصنيعه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgItems.Rows.Count == 0)
            {
                MessageBox.Show("يجب إضافة مادة خام واحدة على الأقل في شجرة التصنيع.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var bom = new BOMModel
            {
                BOMID = _currentBOMID,
                ProductID = _selectedFinishedProductID,
                OutputQty = numOutputQty.Value,
                UnitName = txtUnitName.Text.Trim(),
                Notes = txtNotes.Text.Trim()
            };

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                bom.Items.Add(new BOMItemModel
                {
                    RawProductID = Convert.ToInt32(row.Cells["RawProductID"].Value),
                    Quantity = Convert.ToDecimal(row.Cells["Quantity"].Value),
                    UnitName = row.Cells["UnitName"].Value?.ToString(),
                    Notes = row.Cells["Notes"].Value?.ToString()
                });
            }

            try
            {
                _currentBOMID = ProductionDAL.SaveBOM(bom);
                MessageBox.Show("تم حفظ شجرة ومكونات التصنيع بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSavedBOMsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل حفظ شجرة التصنيع: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSavedBOMsList(string search = "")
        {
            try
            {
                var dt = ProductionDAL.GetAllBOMs(search);
                dgBOMList.Rows.Clear();
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        dgBOMList.Rows.Add(
                            r["BOMID"],
                            $"{r["ProductCode"]} - {r["ProductName"]}",
                            $"{r["OutputQty"]} {r["UnitName"]}",
                            $"{r["ItemsCount"]} صنف"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmBOM.LoadSavedBOMsList", ex);
            }
        }

        private void LoadBOMByID(int bomId)
        {
            var bom = ProductionDAL.GetBOMByID(bomId);
            if (bom != null)
            {
                LoadBOM(bom);
            }
        }

        private void LoadBOM(BOMModel bom)
        {
            _currentBOMID = bom.BOMID;
            _selectedFinishedProductID = bom.ProductID;
            _selectedFinishedProductCode = bom.ProductCode;
            _selectedFinishedProductName = bom.ProductName;

            txtFinishedProduct.Text = $"{bom.ProductCode} - {bom.ProductName}";
            numOutputQty.Value = bom.OutputQty > 0 ? bom.OutputQty : 1m;
            txtUnitName.Text = bom.UnitName ?? "قطعة";
            txtNotes.Text = bom.Notes ?? "";

            dgItems.Rows.Clear();
            int rowNum = 1;
            foreach (var itm in bom.Items)
            {
                dgItems.Rows.Add(
                    itm.RawProductID,
                    rowNum++,
                    itm.RawProductCode,
                    itm.RawProductName,
                    itm.Quantity,
                    itm.UnitName,
                    itm.RawCostPrice.ToString("N2"),
                    itm.TotalCost.ToString("N2"),
                    itm.Notes,
                    "❌"
                );
            }

            RecalculateTotals();
        }

        private void ResetForm()
        {
            _currentBOMID = 0;
            _selectedFinishedProductID = 0;
            _selectedFinishedProductCode = "";
            _selectedFinishedProductName = "";
            txtFinishedProduct.Clear();
            numOutputQty.Value = 1m;
            txtUnitName.Text = "قطعة";
            txtNotes.Clear();
            dgItems.Rows.Clear();
            ClearRawInputs();
            RecalculateTotals();
        }

        private void DeleteCurrentBOM()
        {
            if (_currentBOMID <= 0)
            {
                MessageBox.Show("لا توجد وصفة محفوظة محددة حالياً للحذف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var res = MessageBox.Show("هل أنت متأكد من رغبتك في حذف شجرة التصنيع المحددة؟", "تأكيد الحذف",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                if (ProductionDAL.DeleteBOM(_currentBOMID))
                {
                    MessageBox.Show("تم حذف شجرة التصنيع بنجاح.", "تم الحذف", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetForm();
                    LoadSavedBOMsList();
                }
            }
        }

        private void PrintBOM()
        {
            if (_selectedFinishedProductID <= 0 || dgItems.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات وصفة للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pd = new PrintDocument();
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                float y = 40;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 12f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 10f);
                var fontBold = new Font("Segoe UI", 10f, FontStyle.Bold);

                g.DrawString("بطاقة شجرة التصنيع والمعايير الفنية (BOM)", fontTitle, Brushes.DarkBlue, new PointF(220, y));
                y += 40;

                g.DrawString($"المنتج النهائي: {_selectedFinishedProductCode} - {_selectedFinishedProductName}", fontHeader, Brushes.Black, new PointF(40, y));
                y += 25;
                g.DrawString($"كمية الإنتاج المعيارية: {numOutputQty.Value} {txtUnitName.Text.Trim()} | تاريخ التحديث: {DateTime.Now:yyyy-MM-dd HH:mm}", fontBody, Brushes.DarkSlateGray, new PointF(40, y));
                y += 35;

                // Table Header
                g.FillRectangle(Brushes.LightGray, 40, y, 740, 26);
                g.DrawRectangle(Pens.Gray, 40, y, 740, 26);
                g.DrawString("م", fontBold, Brushes.Black, 50, y + 4);
                g.DrawString("كود الخام", fontBold, Brushes.Black, 90, y + 4);
                g.DrawString("اسم المادة الخام", fontBold, Brushes.Black, 220, y + 4);
                g.DrawString("الكمية", fontBold, Brushes.Black, 470, y + 4);
                g.DrawString("الوحدة", fontBold, Brushes.Black, 540, y + 4);
                g.DrawString("التكلفة", fontBold, Brushes.Black, 610, y + 4);
                g.DrawString("الإجمالي", fontBold, Brushes.Black, 680, y + 4);
                y += 26;

                int num = 1;
                decimal total = 0;
                foreach (DataGridViewRow row in dgItems.Rows)
                {
                    g.DrawRectangle(Pens.LightGray, 40, y, 740, 24);
                    g.DrawString(num++.ToString(), fontBody, Brushes.Black, 50, y + 3);
                    g.DrawString(row.Cells["RawProductCode"].Value?.ToString() ?? "", fontBody, Brushes.Black, 90, y + 3);
                    g.DrawString(row.Cells["RawProductName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 220, y + 3);
                    g.DrawString(row.Cells["Quantity"].Value?.ToString() ?? "", fontBody, Brushes.Black, 470, y + 3);
                    g.DrawString(row.Cells["UnitName"].Value?.ToString() ?? "", fontBody, Brushes.Black, 540, y + 3);
                    g.DrawString(row.Cells["UnitCost"].Value?.ToString() ?? "", fontBody, Brushes.Black, 610, y + 3);
                    g.DrawString(row.Cells["TotalCost"].Value?.ToString() ?? "", fontBody, Brushes.Black, 680, y + 3);

                    total += Convert.ToDecimal(row.Cells["TotalCost"].Value ?? 0);
                    y += 24;
                }

                y += 15;
                decimal uCost = numOutputQty.Value > 0 ? total / numOutputQty.Value : total;
                g.DrawString($"إجمالي تكلفة المواد الخام: {total:N2} ج.م", fontBold, Brushes.Black, 450, y);
                y += 25;
                g.DrawString($"تكلفة الوحدة المعيارية: {uCost:N2} ج.م", fontHeader, Brushes.DarkGreen, 450, y);
            };

            using (var ppd = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700 })
            {
                ppd.ShowDialog();
            }
        }
    }
}

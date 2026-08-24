using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Collections.Generic;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmBulkPrintBarcodes : Form
    {
        private ComboBox cboProduct;
        private Button btnSearchProduct;
        private NumericUpDown nudQty;
        private Button btnAdd;
        private DataGridView dgItems;

        private ComboBox cboPrinters;
        private ComboBox cboBarcodeTemplate;
        private ComboBox cboBarcodeEncoding;
        private CheckBox chkPrintPrice;
        private CheckBox chkPrintCompanyName;

        private Button btnPrint;
        private Button btnPreview;
        private Button btnCancel;
        private Button btnClearAll;

        private Label lblTotalProductsCount;
        private Label lblTotalLabelsCount;

        // Category filter controls
        private ComboBox cboCategoryFilter;
        private CheckBox chkOnlyWithStock;
        private CheckBox chkOnlyWithTransactions;
        private Button btnAddCategory;

        // Header and layout elements
        private Panel pnlTopCard;
        private Panel pnlBottom;
        private Label lblSelect;
        private Label lblQty;
        private Label lblCat;
        private Label lblPrinter;
        private Label lblTemplate;
        private Label lblEncoding;

        private bool _isSelectingCombo = false;

        // Print state tracking
        private List<BarcodePrintItem> _printList;
        private int _currentItemIndex = 0;
        private int _currentLabelIndex = 0;
        private string _printTemplate = "Standard";
        private bool _printIsCode128 = true;
        private bool _printPriceFlag = true;
        private bool _printCompanyNameFlag = true;
        private string _printCompanyNameText = "";
        private string _printBarcodeStickerSize = "50x30";

        public FrmBulkPrintBarcodes()
        {
            InitializeComponent();
            LoadCategories();
            LoadProducts();
            UpdateSummaryBadges();
        }

        public FrmBulkPrintBarcodes(List<BarcodePrintItem> initialItems) : this()
        {
            if (initialItems != null && initialItems.Count > 0)
            {
                foreach (var item in initialItems)
                {
                    dgItems.Rows.Add(item.ProductID, item.ProductName, item.ProductCode, item.Price.ToString("F2"), item.PrintQty > 0 ? item.PrintQty : 1, item.ShelfLocation, "\ud83d\uddd1");
                }
                UpdateSummaryBadges();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "🏷️ طباعة باركود الأصناف (مجمع)";
            this.Size = new Size(1150, 750);
            this.MinimumSize = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.KeyPreview = true;
            this.KeyDown += FrmBulkPrintBarcodes_KeyDown;

            // ── 1. Title Header (شريط العنوان المدمج) ────────────────────────
            var pnlTop = Theme.MakeTitleBar("🏷️ طباعة باركود الأصناف (مجمع)", "قم بإضافة الأصناف وتحديد سعر الطباعة وكمية الملصقات لكل منها.");

            // ── 2. Top Card: Single compact unified panel for item add, category add & badges ──
            pnlTopCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 4, 10, 4)
            };

            // Row 1 Controls: Individual Product Selection & Search
            lblSelect = new Label
            {
                Text = "🏷️ الصنف:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            cboProduct = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f)
            };
            cboProduct.SelectedIndexChanged += CboProduct_SelectedIndexChanged;

            btnSearchProduct = new Button
            {
                Text = "🔍 بحث متقدم (F3)",
                Height = 28,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnSearchProduct.FlatAppearance.BorderSize = 0;
            btnSearchProduct.Click += BtnSearchProduct_Click;

            lblQty = new Label
            {
                Text = "الكمية:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            nudQty = new NumericUpDown
            {
                Height = 26,
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            nudQty.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnAdd_Click(null, null);
                    e.Handled = true;
                }
            };

            btnAdd = new Button
            {
                Text = "➕ إضافة للقائمة",
                Height = 28,
                BackColor = Theme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            // Row 2 Controls: Category Filter + Summary Badges + Clear All
            lblCat = new Label
            {
                Text = "📂 تصنيف كامل:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            cboCategoryFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };

            chkOnlyWithStock = new CheckBox
            {
                Text = "ليها رصيد",
                AutoSize = true,
                ForeColor = Color.FromArgb(5, 150, 105),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Checked = false
            };

            chkOnlyWithTransactions = new CheckBox
            {
                Text = "تم التعامل",
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 64, 175),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Checked = false
            };

            btnAddCategory = new Button
            {
                Text = "➕ إضافة التصنيف",
                Height = 26,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnAddCategory.FlatAppearance.BorderSize = 0;
            btnAddCategory.Click += BtnAddCategory_Click;

            lblTotalProductsCount = new Label
            {
                Text = "📦 الأصناف: 0",
                AutoSize = true,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            lblTotalLabelsCount = new Label
            {
                Text = "🏷️ الملصقات: 0",
                AutoSize = true,
                ForeColor = Color.FromArgb(217, 119, 6),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            btnClearAll = new Button
            {
                Text = "🗑️ مسح الكل",
                Height = 26,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnClearAll.FlatAppearance.BorderSize = 0;
            btnClearAll.Click += (s, e) =>
            {
                if (dgItems.Rows.Count > 0 && MessageBox.Show("هل تريد مسح جميع الأصناف من القائمة؟", "تأكيد المسح", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    dgItems.Rows.Clear();
                    UpdateSummaryBadges();
                }
            };

            pnlTopCard.Controls.AddRange(new Control[] {
                lblSelect, cboProduct, btnSearchProduct, lblQty, nudQty, btnAdd,
                lblCat, cboCategoryFilter, chkOnlyWithStock, chkOnlyWithTransactions, btnAddCategory,
                lblTotalProductsCount, lblTotalLabelsCount, btnClearAll
            });

            // ── 3. Main Items DataGridView (مع تلوين تبادلي لراحة العين وتكبير المساحة) ──
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(42, 46, 56) : Color.White,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(34, 38, 46) : Color.FromArgb(240, 246, 252),
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                ColumnHeadersHeight = 32,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                RowTemplate = { Height = 27 },
                EnableHeadersVisualStyles = false
            };
            Theme.EnableDoubleBuffer(dgItems);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            
            var colProdName = new DataGridViewTextBoxColumn
            {
                Name = "ProductName",
                HeaderText = "اسم الصنف",
                ReadOnly = true,
                FillWeight = 190
            };
            colProdName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colProdName.DefaultCellStyle.Padding = new Padding(0, 0, 8, 0);
            dgItems.Columns.Add(colProdName);

            var colProdCode = new DataGridViewTextBoxColumn
            {
                Name = "ProductCode",
                HeaderText = "الباركود / الكود",
                ReadOnly = true,
                FillWeight = 95
            };
            colProdCode.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colProdCode.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgItems.Columns.Add(colProdCode);

            var colPrice = new DataGridViewTextBoxColumn
            {
                Name = "Price",
                HeaderText = "سعر البيع المطبوع",
                FillWeight = 85
            };
            colPrice.DefaultCellStyle.BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(50, 56, 68) : Color.FromArgb(248, 250, 252);
            colPrice.DefaultCellStyle.ForeColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(147, 197, 253) : Color.FromArgb(29, 78, 216);
            colPrice.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgItems.Columns.Add(colPrice);

            var colPrintQty = new DataGridViewTextBoxColumn
            {
                Name = "PrintQty",
                HeaderText = "عدد الملصقات",
                FillWeight = 80
            };
            colPrintQty.DefaultCellStyle.BackColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(60, 50, 25) : Color.FromArgb(254, 243, 199);
            colPrintQty.DefaultCellStyle.ForeColor = AppConfig.AppTheme == "Dark" ? Color.FromArgb(253, 224, 71) : Color.FromArgb(180, 83, 9);
            colPrintQty.DefaultCellStyle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            colPrintQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgItems.Columns.Add(colPrintQty);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLocation", Visible = false });

            var colDelete = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "حذف",
                Text = "❌",
                UseColumnTextForButtonValue = true,
                FillWeight = 35
            };
            dgItems.Columns.Add(colDelete);

            dgItems.CellClick += DgItems_CellClick;
            dgItems.CellValueChanged += (s, e) => UpdateSummaryBadges();

            var pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 2, 8, 2),
                BackColor = Theme.BgMain
            };
            pnlGridContainer.Controls.Add(dgItems);

            // ── 4. Bottom Settings & Action Panel (مضغوط ومرتب صفين خفيفين) ──
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 4, 10, 4)
            };

            // Settings Controls
            lblPrinter = new Label { Text = "🖨️ طابعة:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboPrinters = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            try
            {
                cboPrinters.Items.Add("(طابعة الباركود الافتراضية)");
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cboPrinters.Items.Add(printer);
                }
                cboPrinters.SelectedItem = string.IsNullOrEmpty(AppConfig.BarcodePrinterName) ? "(طابعة الباركود الافتراضية)" : AppConfig.BarcodePrinterName;
                if (cboPrinters.SelectedIndex == -1 && cboPrinters.Items.Count > 0)
                    cboPrinters.SelectedIndex = 0;
            }
            catch { }

            lblTemplate = new Label { Text = "📐 الشكل:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboBarcodeTemplate = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            cboBarcodeTemplate.Items.AddRange(new object[]
            {
                "الافتراضي (اسم صنف + سعر + باركود)",
                "سعر بارز (سعر كبير + باركود)",
                "ملصق صغير (سعر وباركود فقط)",
                "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)",
                "اسم صنف كبير + باركود (بدون سعر)"
            });
            cboBarcodeTemplate.SelectedItem = AppConfig.BarcodeTemplate == "PriceHeavy" ? "سعر بارز (سعر كبير + باركود)"
                                            : AppConfig.BarcodeTemplate == "Small" ? "ملصق صغير (سعر وباركود فقط)"
                                            : AppConfig.BarcodeTemplate == "Shelf" ? "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)"
                                            : (AppConfig.BarcodeTemplate == "NoPrice" || AppConfig.BarcodeTemplate == "NoPriceBigName") ? "اسم صنف كبير + باركود (بدون سعر)"
                                            : "الافتراضي (اسم صنف + سعر + باركود)";
            if (cboBarcodeTemplate.SelectedIndex == -1) cboBarcodeTemplate.SelectedIndex = 0;

            lblEncoding = new Label { Text = "🔒 التشفير:", AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboBarcodeEncoding = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            cboBarcodeEncoding.Items.AddRange(new object[]
            {
                "Code 128 (موصى به)",
                "Code 39 (أحادي عريض)"
            });
            cboBarcodeEncoding.SelectedIndex = AppConfig.BarcodeEncoding == "Code39" ? 1 : 0;

            chkPrintPrice = new CheckBox
            {
                Text = "طباعة السعر",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Checked = true
            };

            chkPrintCompanyName = new CheckBox
            {
                Text = "طباعة اسم المؤسسة",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Checked = true
            };

            // Action Buttons
            btnPrint = Theme.MakeButton("🖨️ طباعة مباشرة (Ctrl+P)", Theme.Success);
            btnPrint.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnPrint.Height = 36;
            btnPrint.Click += (s, e) => StartPrintJob(false);

            btnPreview = Theme.MakeButton("معاينة 👁️", Theme.Accent);
            btnPreview.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnPreview.Height = 36;
            btnPreview.Click += (s, e) => StartPrintJob(true);

            btnCancel = Theme.MakeButton("إلغاء ↩", Color.FromArgb(100, 116, 139));
            btnCancel.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnCancel.Height = 36;
            btnCancel.Click += (s, e) => CloseOrNavigateBack();

            pnlBottom.Controls.AddRange(new Control[] {
                lblPrinter, cboPrinters, lblTemplate, cboBarcodeTemplate, lblEncoding, cboBarcodeEncoding,
                chkPrintPrice, chkPrintCompanyName,
                btnPrint, btnPreview, btnCancel
            });

            // ── 5. Add controls in Z-Order ──
            this.Controls.Add(pnlGridContainer); // Fill
            this.Controls.Add(pnlBottom);        // Bottom
            this.Controls.Add(pnlTopCard);       // Top
            this.Controls.Add(pnlTop);           // Top

            this.Resize += (s, e) => { LayoutTopCard(); LayoutBottomBar(); };
            this.Load += (s, e) => { LayoutTopCard(); LayoutBottomBar(); };
            this.Shown += (s, e) => { LayoutTopCard(); LayoutBottomBar(); };
            LayoutTopCard();
            LayoutBottomBar();

            Theme.ApplyFormRTL(this);
        }

        private void LayoutTopCard()
        {
            if (pnlTopCard == null || cboProduct == null) return;
            int w = pnlTopCard.ClientSize.Width;
            if (w < 400) return;

            int rightMargin = 10;
            int leftMargin = 10;
            int curX = w - rightMargin;

            // ── Row 1 (Y = 6, H = 28) ──
            lblSelect.Location = new Point(curX - lblSelect.PreferredWidth, 10);
            curX -= lblSelect.PreferredWidth + 6;

            int leftControlsW = 120 + 8 + 60 + 6 + 45 + 8 + 130 + 10;
            int comboW = Math.Max(180, curX - (leftMargin + leftControlsW));

            cboProduct.Location = new Point(curX - comboW, 7);
            cboProduct.Width = comboW;
            curX -= comboW + 10;

            btnSearchProduct.Location = new Point(curX - 130, 6);
            btnSearchProduct.Width = 130;
            curX -= 130 + 8;

            lblQty.Location = new Point(curX - lblQty.PreferredWidth, 10);
            curX -= lblQty.PreferredWidth + 6;

            nudQty.Location = new Point(curX - 60, 7);
            nudQty.Width = 60;
            curX -= 60 + 8;

            btnAdd.Location = new Point(curX - 120, 6);
            btnAdd.Width = 120;

            // ── Row 2 (Y = 38, H = 28) ──
            curX = w - rightMargin;

            lblCat.Location = new Point(curX - lblCat.PreferredWidth, 42);
            curX -= lblCat.PreferredWidth + 6;

            int catComboW = Math.Min(220, Math.Max(140, (w - 600) / 3));
            cboCategoryFilter.Location = new Point(curX - catComboW, 39);
            cboCategoryFilter.Width = catComboW;
            curX -= catComboW + 8;

            chkOnlyWithStock.Location = new Point(curX - chkOnlyWithStock.PreferredSize.Width, 42);
            curX -= chkOnlyWithStock.PreferredSize.Width + 6;

            chkOnlyWithTransactions.Location = new Point(curX - chkOnlyWithTransactions.PreferredSize.Width, 42);
            curX -= chkOnlyWithTransactions.PreferredSize.Width + 6;

            btnAddCategory.Location = new Point(curX - 115, 38);
            btnAddCategory.Width = 115;

            // Badges & Clear button from left edge
            btnClearAll.Location = new Point(leftMargin, 38);
            btnClearAll.Width = 90;

            lblTotalLabelsCount.Location = new Point(leftMargin + 98, 42);
            lblTotalProductsCount.Location = new Point(lblTotalLabelsCount.Right + 12, 42);
        }

        private void LayoutBottomBar()
        {
            if (pnlBottom == null || btnPrint == null) return;
            int w = pnlBottom.ClientSize.Width;
            if (w < 400) return;

            int leftMargin = 10;
            int rightMargin = 10;

            // Action buttons on Left side (in RTL)
            int btnY = 11;
            btnCancel.Location = new Point(leftMargin, btnY);
            btnCancel.Width = 80;

            btnPreview.Location = new Point(btnCancel.Right + 6, btnY);
            btnPreview.Width = 95;

            btnPrint.Location = new Point(btnPreview.Right + 6, btnY);
            btnPrint.Width = 190;

            int actionRight = btnPrint.Right + 16;

            // Settings on Right side
            int curX = w - rightMargin;

            // Row 1 (Y = 6, H = 22): Printer, Template, Encoding
            lblPrinter.Location = new Point(curX - lblPrinter.PreferredWidth, 8);
            curX -= lblPrinter.PreferredWidth + 4;

            int availSettingW = Math.Max(300, curX - actionRight);
            int printerW = Math.Min(180, Math.Max(110, availSettingW / 3));
            cboPrinters.Location = new Point(curX - printerW, 6);
            cboPrinters.Width = printerW;
            curX -= printerW + 10;

            lblTemplate.Location = new Point(curX - lblTemplate.PreferredWidth, 8);
            curX -= lblTemplate.PreferredWidth + 4;

            int templateW = Math.Min(180, Math.Max(110, availSettingW / 3));
            cboBarcodeTemplate.Location = new Point(curX - templateW, 6);
            cboBarcodeTemplate.Width = templateW;
            curX -= templateW + 10;

            lblEncoding.Location = new Point(curX - lblEncoding.PreferredWidth, 8);
            curX -= lblEncoding.PreferredWidth + 4;

            int encW = Math.Min(150, Math.Max(90, availSettingW / 3));
            cboBarcodeEncoding.Location = new Point(curX - encW, 6);
            cboBarcodeEncoding.Width = encW;

            // Row 2 (Y = 33): Checkboxes
            curX = w - rightMargin;
            chkPrintPrice.Location = new Point(curX - chkPrintPrice.PreferredSize.Width, 34);
            curX -= chkPrintPrice.PreferredSize.Width + 16;

            chkPrintCompanyName.Location = new Point(curX - chkPrintCompanyName.PreferredSize.Width, 34);
        }

        private void FrmBulkPrintBarcodes_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3)
            {
                BtnSearchProduct_Click(null, null);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.P)
            {
                StartPrintJob(false);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CloseOrNavigateBack();
                e.Handled = true;
            }
        }

        private void LoadCategories()
        {
            try
            {
                cboCategoryFilter.Items.Clear();
                cboCategoryFilter.Items.Add(new CategoryFilterItem { ID = 0, Name = "-- اختر التصنيف --" });
                var dt = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories WHERE IsActive=1 ORDER BY CategoryName");
                foreach (DataRow r in dt.Rows)
                {
                    cboCategoryFilter.Items.Add(new CategoryFilterItem
                    {
                        ID = Convert.ToInt32(r["CategoryID"]),
                        Name = r["CategoryName"].ToString()
                    });
                }
                cboCategoryFilter.SelectedIndex = 0;
            }
            catch { cboCategoryFilter.Items.Add(new CategoryFilterItem { ID = 0, Name = "-- اختر التصنيف --" }); cboCategoryFilter.SelectedIndex = 0; }
        }

        private void BtnAddCategory_Click(object sender, EventArgs e)
        {
            var cat = cboCategoryFilter.SelectedItem as CategoryFilterItem;
            if (cat == null || cat.ID == 0)
            {
                MessageBox.Show("يرجى اختيار تصنيف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool onlyStock  = chkOnlyWithStock.Checked;
                bool onlyTxn    = chkOnlyWithTransactions.Checked;

                string stockJoin = (onlyStock)
                    ? @"INNER JOIN (
                            SELECT ProductID, SUM(CurrentQty) AS StockQty 
                            FROM vw_CurrentStockByWarehouse 
                            GROUP BY ProductID
                            HAVING SUM(CurrentQty) > 0
                        ) stk ON p.ProductID = stk.ProductID"
                    : "";

                string txnJoin = (onlyTxn)
                    ? @"INNER JOIN (
                            SELECT DISTINCT ProductID FROM SaleItems
                            UNION
                            SELECT DISTINCT ProductID FROM PurchaseItems
                        ) txn ON p.ProductID = txn.ProductID"
                    : "";

                string sql = $@"
                    SELECT p.ProductID, p.ProductName, 
                           COALESCE(p.InternationalCode, p.ProductCode, N'') AS BarcodeCode,
                           p.ProductCode,
                           COALESCE(p.SalePrice, 0) AS SalePrice,
                           COALESCE(p.ShelfLocation, N'') AS ShelfLocation
                    FROM Products p
                    {stockJoin}
                    {txnJoin}
                    WHERE p.IsActive = 1
                      AND p.CategoryID = {cat.ID}
                    ORDER BY p.ProductName";

                var dt = DbHelper.Query(sql);

                if (dt.Rows.Count == 0)
                {
                    string filters = "";
                    if (onlyStock) filters += " (ليها رصيد)";
                    if (onlyTxn)   filters += " (تم التعامل عليها)";
                    MessageBox.Show($"لا توجد أصناف في تصنيف '{cat.Name}'{filters}.", "نتيجة البحث", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int added = 0;
                int updated = 0;
                foreach (DataRow r in dt.Rows)
                {
                    int pid    = Convert.ToInt32(r["ProductID"]);
                    string name = r["ProductName"].ToString();
                    string code = r["BarcodeCode"].ToString();
                    if (string.IsNullOrWhiteSpace(code)) code = r["ProductCode"].ToString();
                    decimal price = Convert.ToDecimal(r["SalePrice"]);
                    string shelf  = r["ShelfLocation"].ToString();

                    // Check if already in grid
                    bool found = false;
                    foreach (DataGridViewRow row in dgItems.Rows)
                    {
                        if (Convert.ToInt32(row.Cells["ProductID"].Value) == pid)
                        {
                            found = true;
                            updated++;
                            break;
                        }
                    }

                    if (!found)
                    {
                        dgItems.Rows.Add(pid, name, code, price.ToString("F2"), 1, shelf);
                        added++;
                    }
                }

                UpdateSummaryBadges();

                string msg = $"تم إضافة {added} صنف من تصنيف '{cat.Name}'";
                if (updated > 0) msg += $" ({updated} صنف كان موجوداً مسبقاً وتم تجاهله)";
                msg += ".";
                MessageBox.Show(msg, "تمت الإضافة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل أصناف التصنيف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                DataTable dt = ProductDAL.GetAll(true);
                cboProduct.Items.Clear();
                cboProduct.Items.Add(new ComboItem(0, "-- اختر الصنف للطباعة --"));
                foreach (DataRow r in dt.Rows)
                {
                    var ci = new ComboItem(
                        Convert.ToInt32(r["ProductID"]),
                        r["ProductName"].ToString(),
                        r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]) : 0m);
                    
                    ci.ProductCode = r["ProductCode"]?.ToString() ?? "";
                    ci.InternationalCode = r["InternationalCode"]?.ToString() ?? "";
                    ci.ShelfLocation = r["ShelfLocation"]?.ToString() ?? "";
                    cboProduct.Items.Add(ci);
                }
                cboProduct.DisplayMember = "Text";
                cboProduct.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل قائمة الأصناف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CboProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isSelectingCombo) return;
            if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                _isSelectingCombo = true;
                int qty = (int)nudQty.Value;
                string codeToUse = string.IsNullOrWhiteSpace(ci.InternationalCode) ? ci.ProductCode : ci.InternationalCode;
                AddProductToGrid(ci.ID, ci.Text, codeToUse, ci.Price, qty, ci.ShelfLocation);

                cboProduct.SelectedIndex = 0;
                nudQty.Value = 1;
                _isSelectingCombo = false;
            }
        }

        private void BtnSearchProduct_Click(object sender, EventArgs e)
        {
            // Continuous search loop: opens search window repeatedly until user cancels or closes
            while (true)
            {
                using (var dlgSearch = new FrmProductSearch())
                {
                    if (dlgSearch.ShowDialog(this) == DialogResult.OK && dlgSearch.SelectedProductID > 0)
                    {
                        int qty = (int)Math.Max(1, dlgSearch.SelectedQuantity);
                        decimal priceToUse = dlgSearch.SelectedSalePrice > 0 ? dlgSearch.SelectedSalePrice : dlgSearch.SelectedPrice;

                        string prodName = "";
                        string codeToUse = "";
                        string shelfLocation = "";

                        // 1. Try finding in loaded cboProduct
                        foreach (var item in cboProduct.Items)
                        {
                            if (item is ComboItem ci && ci.ID == dlgSearch.SelectedProductID)
                            {
                                prodName = ci.Text;
                                codeToUse = string.IsNullOrWhiteSpace(ci.InternationalCode) ? ci.ProductCode : ci.InternationalCode;
                                shelfLocation = ci.ShelfLocation;
                                if (priceToUse <= 0) priceToUse = ci.Price;
                                break;
                            }
                        }

                        // 2. Fetch directly from DB if not in cboProduct
                        if (string.IsNullOrEmpty(prodName))
                        {
                            try
                            {
                                var dtProd = DbHelper.Query("SELECT ProductName, ProductCode, InternationalCode, SalePrice, ShelfLocation FROM Products WHERE ProductID=@id", DbHelper.P("@id", dlgSearch.SelectedProductID));
                                if (dtProd.Rows.Count > 0)
                                {
                                    var r = dtProd.Rows[0];
                                    prodName = r["ProductName"].ToString();
                                    string pCode = r["ProductCode"]?.ToString() ?? "";
                                    string intCode = r["InternationalCode"]?.ToString() ?? "";
                                    codeToUse = string.IsNullOrWhiteSpace(intCode) ? pCode : intCode;
                                    shelfLocation = r["ShelfLocation"]?.ToString() ?? "";
                                    if (priceToUse <= 0 && r["SalePrice"] != DBNull.Value) priceToUse = Convert.ToDecimal(r["SalePrice"]);
                                }
                            }
                            catch { }
                        }

                        if (!string.IsNullOrEmpty(prodName))
                        {
                            AddProductToGrid(dlgSearch.SelectedProductID, prodName, codeToUse, priceToUse, qty, shelfLocation);
                        }
                    }
                    else
                    {
                        // User closed or cancelled search window -> break loop
                        break;
                    }
                }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (cboProduct.SelectedItem is ComboItem ci && ci.ID > 0)
            {
                int qty = (int)nudQty.Value;
                string codeToUse = string.IsNullOrWhiteSpace(ci.InternationalCode) ? ci.ProductCode : ci.InternationalCode;
                AddProductToGrid(ci.ID, ci.Text, codeToUse, ci.Price, qty, ci.ShelfLocation);

                cboProduct.SelectedIndex = 0;
                nudQty.Value = 1;
                cboProduct.Focus();
            }
            else
            {
                BtnSearchProduct_Click(null, null);
            }
        }

        private void AddProductToGrid(int productID, string name, string code, decimal price, int qty, string shelfLocation)
        {
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (Convert.ToInt32(row.Cells["ProductID"].Value) == productID)
                {
                    int curQty = Convert.ToInt32(row.Cells["PrintQty"].Value);
                    row.Cells["PrintQty"].Value = curQty + qty;
                    if (price > 0) row.Cells["Price"].Value = price.ToString("F2");
                    UpdateSummaryBadges();
                    return;
                }
            }

            dgItems.Rows.Add(
                productID,
                name,
                code,
                price.ToString("F2"),
                qty,
                shelfLocation
            );
            UpdateSummaryBadges();
        }

        private void UpdateSummaryBadges()
        {
            if (dgItems == null || lblTotalProductsCount == null || lblTotalLabelsCount == null) return;
            int totalProducts = dgItems.Rows.Count;
            int totalLabels = 0;
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["PrintQty"].Value != null && int.TryParse(row.Cells["PrintQty"].Value.ToString(), out int q))
                {
                    totalLabels += q;
                }
            }
            lblTotalProductsCount.Text = $"📦 الأصناف المضافة: {totalProducts}";
            lblTotalLabelsCount.Text = $"🏷️ إجمالي الملصقات: {totalLabels} ملصق";
        }

        private void DgItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgItems.Columns[e.ColumnIndex].Name == "Delete")
            {
                dgItems.Rows.RemoveAt(e.RowIndex);
                UpdateSummaryBadges();
            }
        }

        private void StartPrintJob(bool isPreview)
        {
            _printList = new List<BarcodePrintItem>();

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["PrintQty"].Value == null) continue;
                int.TryParse(row.Cells["PrintQty"].Value.ToString(), out int pQty);

                if (pQty > 0)
                {
                    decimal.TryParse(row.Cells["Price"].Value?.ToString(), out decimal prc);
                    _printList.Add(new BarcodePrintItem
                    {
                        ProductName = row.Cells["ProductName"].Value.ToString(),
                        ProductCode = row.Cells["ProductCode"].Value.ToString(),
                        Price       = prc,
                        PrintQty    = pQty,
                        ShelfLocation = row.Cells["ShelfLocation"].Value?.ToString() ?? ""
                    });
                }
            }

            if (_printList.Count == 0)
            {
                MessageBox.Show("يرجى إضافة صنف واحد على الأقل وتحديد عدد ملصقات أكبر من الصفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _currentItemIndex = 0;
            _currentLabelIndex = 0;

            if (cboBarcodeTemplate.SelectedItem != null)
            {
                _printTemplate = cboBarcodeTemplate.SelectedIndex == 1 ? "PriceHeavy"
                               : cboBarcodeTemplate.SelectedIndex == 2 ? "Small"
                               : cboBarcodeTemplate.SelectedIndex == 3 ? "Shelf"
                               : cboBarcodeTemplate.SelectedIndex == 4 ? "NoPrice"
                               : "Standard";
            }
            if (cboBarcodeEncoding.SelectedItem != null)
            {
                _printIsCode128 = cboBarcodeEncoding.SelectedIndex == 0;
                AppConfig.BarcodeEncoding = _printIsCode128 ? "Code128" : "Code39";
            }
            _printPriceFlag = chkPrintPrice.Checked;
            _printCompanyNameFlag = chkPrintCompanyName.Checked;
            _printCompanyNameText = AppConfig.CompanyName;
            _printBarcodeStickerSize = AppConfig.BarcodeStickerSize;

            try
            {
                var pd = new PrintDocument();
                pd.PrintController = new StandardPrintController();
                
                if (cboPrinters.SelectedIndex > 0)
                {
                    pd.PrinterSettings.PrinterName = cboPrinters.SelectedItem.ToString();
                }
                else if (!string.IsNullOrEmpty(AppConfig.BarcodePrinterName))
                {
                    AppConfig.SetPrinter(pd, AppConfig.BarcodePrinterName);
                }
                else if (!string.IsNullOrEmpty(AppConfig.ReceiptPrinterName))
                {
                    AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
                }

                AppConfig.SetPaperSize(pd, _printBarcodeStickerSize);

                pd.PrintPage += Pd_PrintPage;

                if (isPreview)
                {
                    var prev = new PrintPreviewDialog
                    {
                        Document = pd,
                        Width = 550,
                        Height = 500,
                        Text = "معاينة ملصقات الباركود"
                    };
                    prev.ShowDialog(this);
                }
                else
                {
                    AppConfig.PrintInBackground(pd);
                    MessageBox.Show("تم إرسال أمر الطباعة في الخلفية بنجاح، يمكنك متابعة العمل بحرية دون أي انتظار.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CloseOrNavigateBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل بدء عملية الطباعة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_currentItemIndex >= _printList.Count)
            {
                e.HasMorePages = false;
                return;
            }

            var g = e.Graphics;

            string labelType = (_printBarcodeStickerSize == "38x26_double") ? "Split" : "Full";
            int labelsPerRow = (labelType == "Split") ? 2 : 1;

            float pageWidth = e.PageBounds.Width;
            float pageHeight = e.PageBounds.Height;
            float leftMargin = 5;
            float topMargin = 5;

            float labelWidth = labelType == "Full" ? pageWidth : (pageWidth / labelsPerRow);
            float labelHeight = pageHeight - (topMargin * 2);

            bool isSmallSticker = (_printBarcodeStickerSize == "38x26" || _printBarcodeStickerSize == "38x26_double");

            string template = _printTemplate;
            bool isCode128 = _printIsCode128;

            var fCompany  = new Font("Arial", isSmallSticker ? 6.5f : 8f, FontStyle.Bold);
            var fName     = new Font("Arial", isSmallSticker ? 6.5f : 7.5f, FontStyle.Bold);
            var fPrice    = new Font("Arial", isSmallSticker ? 7.5f : 8.5f, FontStyle.Bold);
            var fCode     = new Font("Courier New", isSmallSticker ? 6.5f : 7.5f, FontStyle.Regular);
            var fLocation = new Font("Arial", isSmallSticker ? 6.5f : 7.5f, FontStyle.Bold);

            var fPriceLarge    = new Font("Arial", isSmallSticker ? 11f : 14f, FontStyle.Bold);
            var fNameLarge     = new Font("Arial", isSmallSticker ? 8.5f : 10f, FontStyle.Bold);
            var fLocationLarge = new Font("Arial", isSmallSticker ? 7.5f : 9f, FontStyle.Bold);

            var center = new StringFormat { Alignment = StringAlignment.Center };
            var leftFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            var rightFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            for (int itemIndex = 0; itemIndex < labelsPerRow; itemIndex++)
            {
                if (_currentItemIndex >= _printList.Count)
                    break;

                var item = _printList[_currentItemIndex];

                int currentColumn = itemIndex % labelsPerRow;
                int currentRow = itemIndex / labelsPerRow;

                float startX = leftMargin + (currentColumn * labelWidth);
                float startY = topMargin + (currentRow * labelHeight);

                float x = startX;
                float y = startY;
                float w = labelWidth - (leftMargin * 2);

                if (template == "Shelf")
                {
                    if (_printCompanyNameFlag)
                    {
                        g.DrawString(_printCompanyNameText, fCompany, Brushes.Gray, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                        y += isSmallSticker ? 11 : 14;
                    }
                    g.DrawString(item.ProductName, fNameLarge, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 22 : 30), center);
                    y += isSmallSticker ? 24 : 32;
                    if (_printPriceFlag)
                    {
                        g.DrawString($"{item.Price:N2} ج", fPriceLarge, Brushes.DarkRed, new RectangleF(x, y, w, isSmallSticker ? 18 : 24), center);
                        y += isSmallSticker ? 20 : 26;
                    }
                    if (!string.IsNullOrWhiteSpace(item.ShelfLocation))
                    {
                        g.DrawString($"الرف / مكان الصنف: {item.ShelfLocation}", fLocationLarge, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 12 : 16), center);
                    }
                }
                else if (template == "Small")
                {
                    g.DrawString(item.ProductName, new Font("Arial", isSmallSticker ? 6f : 6.5f, FontStyle.Bold), Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 12 : 16), center);
                    y += isSmallSticker ? 12 : 16;

                    float barcodeHeight = isSmallSticker ? 22 : 32;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(item.ProductCode, new Font("Courier New", isSmallSticker ? 6f : 6.5f), Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 8 : 10), center);
                    y += isSmallSticker ? 8 : 10;

                    if (_printPriceFlag)
                    {
                        g.DrawString($"{item.Price:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    }
                }
                else if (template == "PriceHeavy")
                {
                    if (_printPriceFlag)
                    {
                        g.DrawString($"{item.Price:N2} ج", fPriceLarge, Brushes.DarkRed, new RectangleF(x + 5, y, w - 10, isSmallSticker ? 18 : 22), center);
                        y += isSmallSticker ? 20 : 24;
                    }
                    g.DrawString(item.ProductName, fName, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 14 : 18), center);
                    y += isSmallSticker ? 14 : 18;

                    float barcodeHeight = isSmallSticker ? 20 : 30;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(item.ProductCode, fCode, Brushes.Black, new RectangleF(x + 5, y, w / 2 - 5, isSmallSticker ? 10 : 12), leftFormat);
                    if (!string.IsNullOrWhiteSpace(item.ShelfLocation))
                    {
                        g.DrawString($"الرف: {item.ShelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, y, w / 2 - 5, isSmallSticker ? 10 : 12), rightFormat);
                    }
                }
                else if (template == "NoPrice" || template == "NoPriceBigName")
                {
                    if (_printCompanyNameFlag && !string.IsNullOrWhiteSpace(_printCompanyNameText))
                    {
                        g.DrawString(_printCompanyNameText, fCompany, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                        y += isSmallSticker ? 10 : 12;
                    }

                    // اسم الصنف بخط كبير وبارز
                    g.DrawString(item.ProductName, fNameLarge, Brushes.Black, new RectangleF(x + 1, y, w - 2, isSmallSticker ? 20 : 28), center);
                    y += isSmallSticker ? 20 : 28;

                    // رسم الباركود بدون سعر
                    float barcodeHeight = isSmallSticker ? 26 : 38;
                    float barcodeX = x + (w - (w - 16)) / 2;
                    if (isCode128)
                        DrawCode128(g, item.ProductCode, barcodeX, y, w - 16, barcodeHeight);
                    else
                        DrawCode39(g, item.ProductCode, barcodeX, y, w - 16, barcodeHeight);
                    y += barcodeHeight + 2;

                    // كود الباركود ورقم الرف إن وجد
                    if (!string.IsNullOrWhiteSpace(item.ShelfLocation))
                    {
                        g.DrawString(item.ProductCode, fCode, Brushes.Black, new RectangleF(x + 2, y, w / 2 - 2, isSmallSticker ? 10 : 12), leftFormat);
                        g.DrawString($"الرف: {item.ShelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, y, w / 2 - 2, isSmallSticker ? 10 : 12), rightFormat);
                    }
                    else
                    {
                        g.DrawString(item.ProductCode, fCode, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    }
                }
                else
                {
                    if (_printCompanyNameFlag)
                    {
                        g.DrawString(_printCompanyNameText, fCompany, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                        y += isSmallSticker ? 10 : 12;
                    }
                    g.DrawString(item.ProductName, fName, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 16 : 24), center);
                    y += isSmallSticker ? 16 : 24;

                    float barcodeHeight = isSmallSticker ? 24 : 36;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(item.ProductCode, fCode, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    y += isSmallSticker ? 10 : 12;

                    float bottomY = y;
                    if (_printPriceFlag)
                    {
                        g.DrawString($"السعر: {item.Price:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x + 5, bottomY, w / 2 - 5, isSmallSticker ? 11 : 14), leftFormat);
                    }
                    if (!string.IsNullOrWhiteSpace(item.ShelfLocation))
                    {
                        g.DrawString($"الرف: {item.ShelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, bottomY, w / 2 - 5, isSmallSticker ? 11 : 14), rightFormat);
                    }
                }

                _currentLabelIndex++;
                if (_currentLabelIndex >= item.PrintQty)
                {
                    _currentItemIndex++;
                    _currentLabelIndex = 0;
                }
            }

            e.HasMorePages = (_currentItemIndex < _printList.Count);
        }

        private void DrawCode128(Graphics g, string text, float x, float y, float width, float height)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = text.Trim();

            // Set GDI+ options for crisp, aliased rendering
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            List<int> pattern = EncodeCode128Auto(text);
            if (pattern == null || pattern.Count == 0) return;

            int totalModules = 0;
            foreach (int m in pattern) totalModules += m;

            // Safe quiet zone
            float quietZoneModules = 10f;
            float availableWidth = width;
            float moduleWidth = availableWidth / (totalModules + (quietZoneModules * 2f));
            
            float maxModuleWidth = (availableWidth < 130f) ? 1.5f : 2.0f;
            if (moduleWidth > maxModuleWidth) moduleWidth = maxModuleWidth;
            if (moduleWidth < 0.8f) moduleWidth = 0.8f;

            float actualBarcodeWidth = totalModules * moduleWidth;
            float curX = x + (width - actualBarcodeWidth) / 2f;
            bool bar = true;

            using (var brush = new SolidBrush(Color.Black))
            {
                foreach (int m in pattern)
                {
                    float w = m * moduleWidth;
                    if (bar)
                    {
                        g.FillRectangle(brush, curX, y, w, height);
                    }
                    curX += w;
                    bar = !bar;
                }
            }
        }

        private List<int> EncodeCode128Auto(string text)
        {
            var pattern = new List<int>();
            int[][] codeTable = GetCode128Table();

            // Check if the text is entirely numeric
            bool isAllDigits = true;
            foreach (char c in text)
            {
                if (c < '0' || c > '9')
                {
                    isAllDigits = false;
                    break;
                }
            }

            if (isAllDigits && text.Length >= 2 && text.Length % 2 == 0)
            {
                // Code 128 Subset C (Numeric pairs)
                int checksum = 105;
                pattern.AddRange(codeTable[105]); // Start C
                int pos = 1;
                for (int i = 0; i < text.Length; i += 2)
                {
                    int val = int.Parse(text.Substring(i, 2));
                    checksum += val * pos;
                    pattern.AddRange(codeTable[val]);
                    pos++;
                }
                checksum %= 103;
                pattern.AddRange(codeTable[checksum]);
                pattern.AddRange(new int[] { 2, 3, 3, 1, 1, 1, 2 }); // Stop
            }
            else if (isAllDigits && text.Length >= 3 && text.Length % 2 != 0)
            {
                // Odd digits: Start C, switch to B for last char
                int checksum = 105;
                pattern.AddRange(codeTable[105]); // Start C
                int pos = 1;
                for (int i = 0; i < text.Length - 1; i += 2)
                {
                    int val = int.Parse(text.Substring(i, 2));
                    checksum += val * pos;
                    pattern.AddRange(codeTable[val]);
                    pos++;
                }

                // Switch to Code B (Code B in Code C table is 100)
                checksum += 100 * pos;
                pattern.AddRange(codeTable[100]);
                pos++;

                // Last char
                int lastVal = text[text.Length - 1] - 32;
                if (lastVal < 0 || lastVal > 95) lastVal = 0;
                checksum += lastVal * pos;
                pattern.AddRange(codeTable[lastVal]);
                pos++;

                checksum %= 103;
                pattern.AddRange(codeTable[checksum]);
                pattern.AddRange(new int[] { 2, 3, 3, 1, 1, 1, 2 }); // Stop
            }
            else
            {
                // Code 128 Subset B (Alphanumeric)
                int checksum = 104;
                pattern.AddRange(codeTable[104]); // Start B

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    int val = c - 32;
                    if (val < 0 || val > 95) val = 0;
                    checksum += val * (i + 1);
                    pattern.AddRange(codeTable[val]);
                }

                checksum %= 103;
                pattern.AddRange(codeTable[checksum]);
                pattern.AddRange(new int[] { 2, 3, 3, 1, 1, 1, 2 }); // Stop
            }

            return pattern;
        }

        private int[][] GetCode128Table()
        {
            return new int[][]
            {
                new int[] {2,1,2,2,2,2}, new int[] {2,2,2,1,2,2}, new int[] {2,2,2,2,2,1}, new int[] {1,2,1,2,2,3}, new int[] {1,2,1,3,2,2},
                new int[] {1,3,1,2,2,2}, new int[] {1,2,2,2,1,3}, new int[] {1,2,2,3,1,2}, new int[] {1,3,2,2,1,2}, new int[] {2,2,1,2,1,3},
                new int[] {2,2,1,3,1,2}, new int[] {2,3,1,2,1,2}, new int[] {1,1,2,2,3,2}, new int[] {1,2,2,1,3,2}, new int[] {1,2,2,2,3,1},
                new int[] {1,1,3,2,2,2}, new int[] {1,2,3,1,2,2}, new int[] {1,2,3,2,2,1}, new int[] {2,2,3,2,1,1}, new int[] {2,2,1,1,3,2},
                new int[] {2,2,1,2,3,1}, new int[] {2,1,3,2,1,2}, new int[] {2,2,3,1,1,2}, new int[] {3,1,2,1,3,1}, new int[] {3,1,1,2,2,2},
                new int[] {3,2,1,1,2,2}, new int[] {3,2,1,2,2,1}, new int[] {3,1,2,2,1,2}, new int[] {3,2,2,1,1,2}, new int[] {3,2,2,2,1,1},
                new int[] {2,1,2,1,2,3}, new int[] {2,1,2,3,2,1}, new int[] {2,3,2,1,2,1}, new int[] {1,1,1,3,2,3}, new int[] {1,3,1,1,2,3},
                new int[] {1,3,1,3,2,1}, new int[] {1,1,2,3,1,3}, new int[] {1,3,2,1,1,3}, new int[] {1,3,2,3,1,1}, new int[] {2,1,1,3,1,3},
                new int[] {2,3,1,1,1,3}, new int[] {2,3,1,3,1,1}, new int[] {1,1,2,1,3,3}, new int[] {1,1,2,3,3,1}, new int[] {1,3,2,1,3,1},
                new int[] {1,1,3,1,2,3}, new int[] {1,1,3,3,2,1}, new int[] {1,3,3,1,2,1}, new int[] {3,1,3,1,2,1}, new int[] {2,1,1,3,3,1},
                new int[] {2,3,1,1,3,1}, new int[] {2,1,3,1,1,3}, new int[] {2,1,3,3,1,1}, new int[] {2,1,3,1,3,1}, new int[] {3,1,1,1,2,3},
                new int[] {3,1,1,3,2,1}, new int[] {3,3,1,1,2,1}, new int[] {3,1,2,1,1,3}, new int[] {3,1,2,3,1,1}, new int[] {3,3,2,1,1,1},
                new int[] {3,1,4,1,1,1}, new int[] {2,2,1,4,1,1}, new int[] {4,3,1,1,1,1}, new int[] {1,1,1,2,2,4}, new int[] {1,1,1,4,2,2},
                new int[] {1,2,1,1,2,4}, new int[] {1,2,1,4,2,1}, new int[] {1,4,1,1,2,2}, new int[] {1,4,1,2,2,1}, new int[] {1,1,2,2,1,4},
                new int[] {1,1,2,4,1,2}, new int[] {1,2,2,1,1,4}, new int[] {1,2,2,4,1,1}, new int[] {1,4,2,1,1,2}, new int[] {1,4,2,2,1,1},
                new int[] {2,4,1,2,1,1}, new int[] {2,2,1,1,1,4}, new int[] {4,1,1,1,1,2}, new int[] {1,3,4,1,1,1}, new int[] {1,1,1,2,4,2},
                new int[] {1,2,1,1,4,2}, new int[] {1,2,1,2,4,1}, new int[] {1,1,4,2,1,2}, new int[] {1,2,4,1,1,2}, new int[] {1,2,4,2,1,1},
                new int[] {4,1,1,2,1,2}, new int[] {4,2,1,1,1,2}, new int[] {4,2,1,2,1,1}, new int[] {2,1,2,1,4,1}, new int[] {2,1,4,1,2,1},
                new int[] {4,1,2,1,2,1}, new int[] {1,1,1,1,4,3}, new int[] {1,1,1,3,4,1}, new int[] {1,3,1,1,4,1}, new int[] {1,1,4,1,1,3},
                new int[] {1,1,4,3,1,1}, new int[] {4,1,1,1,3,1}, new int[] {2,1,1,4,1,2}, new int[] {2,1,1,2,1,4}, new int[] {2,1,1,2,3,2},
                new int[] {2,3,3,1,1,1,2}, new int[] {2,1,1,2,2,2}, new int[] {2,1,2,2,1,2}, new int[] {2,2,1,1,2,2}, new int[] {2,1,2,2,2,1}
            };
        }

        private void DrawCode39(Graphics g, string text, float x, float y, float width, float height)
        {
            if (string.IsNullOrEmpty(text)) return;
            string code = "*" + text.ToUpper() + "*";
            var patterns = GetCode39Patterns();

            int totalWidthModules = 0;
            foreach (char c in code)
            {
                if (patterns.ContainsKey(c))
                {
                    foreach (char bit in patterns[c])
                        totalWidthModules += (bit == 'W' || bit == 'w') ? 3 : 1;
                    totalWidthModules += 1;
                }
            }

            float moduleWidth = width / totalWidthModules;
            if (moduleWidth < 0.5f) moduleWidth = 0.5f;

            float curX = x + (width - (totalWidthModules * moduleWidth)) / 2;

            foreach (char c in code)
            {
                if (!patterns.ContainsKey(c)) continue;
                string pat = patterns[c];
                bool bar = true;
                foreach (char bit in pat)
                {
                    int wMod = (bit == 'W' || bit == 'w') ? 3 : 1;
                    float w = wMod * moduleWidth;
                    if (bar) g.FillRectangle(Brushes.Black, curX, y, w, height);
                    curX += w;
                    bar = !bar;
                }
                curX += moduleWidth;
            }
        }

        private Dictionary<char, string> GetCode39Patterns()
        {
            return new Dictionary<char, string>
            {
                {'0', "n n w w n n n w n"}, {'1', "w n n n n w n n w"}, {'2', "n n w n n w n n w"},
                {'3', "w n w n n n n n w"}, {'4', "n n n n w w n n w"}, {'5', "w n n n w n n n w"},
                {'6', "n n w n w n n n w"}, {'7', "n n n n n w w n w"}, {'8', "w n n n n w w n n"},
                {'9', "n n w n n w w n n"}, {'A', "w n n n n n n w w"}, {'B', "n n w n n n n w w"},
                {'C', "w n w n n n n w n"}, {'D', "n n n n w n n w w"}, {'E', "w n n n w n n w n"},
                {'F', "n n w n w n n w n"}, {'G', "n n n n n n w w w"}, {'H', "w n n n n n w w n"},
                {'I', "n n w n n n w w n"}, {'J', "n n n n w n w w n"}, {'K', "w n n n n n n n w"},
                {'L', "n n w n n n n n w"}, {'M', "w n w n n n n n n"}, {'N', "n n n n w n n n w"},
                {'O', "w n n n w n n n n"}, {'P', "n n w n w n n n n"}, {'Q', "n n n n n n w n w"},
                {'R', "w n n n n n w n n"}, {'S', "n n w n n n w n n"}, {'T', "n n n n w n w n n"},
                {'U', "w w n n n n n n w"}, {'V', "n w w n n n n n w"}, {'W', "w w w n n n n n n"},
                {'X', "n w n n w n n n w"}, {'Y', "w w n n w n n n n"}, {'Z', "n w w n w n n n n"},
                {'-', "n w n n n n w n w"}, {'.', "w w n n n n w n n"}, {' ', "n w w n n n w n n"},
                {'*', "n w n n w n w n n"}, {'$', "n w n w n w n n n"}, {'/', "n w n w n n n w n"},
                {'+', "n w n n n w n w n"}, {'%', "n n n w n w n w n"}
            };
        }

        private void CloseOrNavigateBack()
        {
            this.Close();
        }

        private class CategoryFilterItem
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }
    }
}

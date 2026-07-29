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

        private bool _isSelectingCombo = false;

        // Print state tracking
        private List<BarcodePrintItem> _printList;
        private int _currentItemIndex = 0;
        private int _currentLabelIndex = 0;

        public FrmBulkPrintBarcodes()
        {
            InitializeComponent();
            LoadProducts();
            UpdateSummaryBadges();
        }

        private void InitializeComponent()
        {
            this.Text = "🏷️ طباعة باركود الأصناف (مجمع)";
            this.Size = new Size(1020, 680);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.KeyPreview = true;
            this.KeyDown += FrmBulkPrintBarcodes_KeyDown;

            // ── 1. Title Header ──────────────────────────────────────────────
            var pnlTop = Theme.MakeTitleBar("🏷️ طباعة باركود الأصناف (مجمع)", "قم بإضافة الأصناف وتحديد سعر الطباعة وكمية الملصقات لكل منها.");

            // ── 2. Top Selection Panel (Search & Add) ────────────────────────
            var pnlSelection = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 10, 12, 10)
            };

            var lblSelect = new Label
            {
                Text = "اختر الصنف:",
                Location = new Point(915, 18),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = Theme.FontBold
            };

            cboProduct = new ComboBox
            {
                Location = new Point(480, 14),
                Width = 430,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f)
            };
            cboProduct.SelectedIndexChanged += CboProduct_SelectedIndexChanged;

            btnSearchProduct = new Button
            {
                Text = "🔍 بحث متقدم (F3)",
                Location = new Point(310, 13),
                Width = 160,
                Height = 34,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnSearchProduct.FlatAppearance.BorderSize = 0;
            btnSearchProduct.Click += BtnSearchProduct_Click;

            var lblQty = new Label
            {
                Text = "الكمية:",
                Location = new Point(255, 18),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = Theme.FontBold
            };

            nudQty = new NumericUpDown
            {
                Location = new Point(175, 14),
                Width = 75,
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
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
                Text = "➕ إضافة",
                Location = new Point(50, 13),
                Width = 115,
                Height = 34,
                BackColor = Theme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            pnlSelection.Controls.AddRange(new Control[] {
                lblSelect, cboProduct, btnSearchProduct, lblQty, nudQty, btnAdd
            });

            // ── 3. Grid Header & Badge Bar ───────────────────────────────────
            var pnlGridHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(12, 6, 12, 6)
            };

            lblTotalProductsCount = new Label
            {
                Text = "📦 الأصناف المضافة: 0",
                Location = new Point(810, 8),
                AutoSize = true,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            lblTotalLabelsCount = new Label
            {
                Text = "🏷️ إجمالي الملصقات: 0 ملصق",
                Location = new Point(580, 8),
                AutoSize = true,
                ForeColor = Color.FromArgb(217, 119, 6),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            btnClearAll = new Button
            {
                Text = "🗑️ مسح الكل",
                Location = new Point(12, 4),
                Width = 110,
                Height = 28,
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

            pnlGridHeader.Controls.AddRange(new Control[] { lblTotalProductsCount, lblTotalLabelsCount, btnClearAll });

            // ── 4. Main Items DataGridView ────────────────────────────────────
            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(226, 232, 240),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(15, 23, 42),
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeight = 36,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };
            Theme.EnableDoubleBuffer(dgItems);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", ReadOnly = true, FillWeight = 160 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الباركود / الكود", ReadOnly = true, FillWeight = 90 });

            var colPrice = new DataGridViewTextBoxColumn
            {
                Name = "Price",
                HeaderText = "سعر البيع المطبوع",
                FillWeight = 85
            };
            colPrice.DefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            colPrice.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            colPrice.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgItems.Columns.Add(colPrice);

            var colPrintQty = new DataGridViewTextBoxColumn
            {
                Name = "PrintQty",
                HeaderText = "عدد الملصقات",
                FillWeight = 85
            };
            colPrintQty.DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
            colPrintQty.DefaultCellStyle.ForeColor = Color.FromArgb(180, 83, 9);
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

            var pnlGridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 4, 12, 4) };
            pnlGridContainer.Controls.Add(dgItems);

            // ── 5. Bottom Settings & Action Panel ─────────────────────────────
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 8, 12, 8)
            };

            var pnlSettings = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(10, 8, 10, 8)
            };

            Color labelDark = Color.FromArgb(30, 41, 59);

            var lblPrinter = new Label { Text = "🖨️ طابعة الملصقات:", Location = new Point(840, 12), AutoSize = true, ForeColor = labelDark, Font = Theme.FontBold };
            cboPrinters = new ComboBox
            {
                Location = new Point(570, 8),
                Width = 265,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = labelDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
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

            chkPrintPrice = new CheckBox
            {
                Text = "طباعة السعر على الملصق",
                Location = new Point(340, 10),
                AutoSize = true,
                ForeColor = labelDark,
                Font = Theme.FontBold,
                Checked = true
            };

            chkPrintCompanyName = new CheckBox
            {
                Text = "طباعة اسم المؤسسة",
                Location = new Point(140, 10),
                AutoSize = true,
                ForeColor = labelDark,
                Font = Theme.FontBold,
                Checked = true
            };

            var lblTemplate = new Label { Text = "📐 شكل الملصق:", Location = new Point(840, 48), AutoSize = true, ForeColor = labelDark, Font = Theme.FontBold };
            cboBarcodeTemplate = new ComboBox
            {
                Location = new Point(570, 44),
                Width = 265,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = labelDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            cboBarcodeTemplate.Items.AddRange(new object[]
            {
                "الافتراضي (اسم صنف + سعر + باركود)",
                "سعر بارز (سعر كبير + باركود)",
                "ملصق صغير (سعر وباركود فقط)",
                "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)"
            });
            cboBarcodeTemplate.SelectedItem = AppConfig.BarcodeTemplate == "PriceHeavy" ? "سعر بارز (سعر كبير + باركود)"
                                            : AppConfig.BarcodeTemplate == "Small" ? "ملصق صغير (سعر وباركود فقط)"
                                            : AppConfig.BarcodeTemplate == "Shelf" ? "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)"
                                            : "الافتراضي (اسم صنف + سعر + باركود)";
            if (cboBarcodeTemplate.SelectedIndex == -1) cboBarcodeTemplate.SelectedIndex = 0;

            var lblEncoding = new Label { Text = "🔒 التشفير:", Location = new Point(445, 48), AutoSize = true, ForeColor = labelDark, Font = Theme.FontBold };
            cboBarcodeEncoding = new ComboBox
            {
                Location = new Point(140, 44),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = labelDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            cboBarcodeEncoding.Items.AddRange(new object[]
            {
                "Code 128 (موصى به - سريع وسهل القراءة)",
                "Code 39 (أحادي عريض)"
            });
            cboBarcodeEncoding.SelectedIndex = AppConfig.BarcodeEncoding == "Code39" ? 1 : 0;

            pnlSettings.Controls.AddRange(new Control[] {
                lblPrinter, cboPrinters, chkPrintPrice, chkPrintCompanyName,
                lblTemplate, cboBarcodeTemplate, lblEncoding, cboBarcodeEncoding
            });

            // Action Buttons Bar
            var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Color.Transparent };
            btnPrint = Theme.MakeButton("🖨️ طباعة مباشرة (Ctrl+P)", 370, 8, 220, 40, Theme.Success);
            btnPrint.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnPrint.Click += (s, e) => StartPrintJob(false);

            btnPreview = Theme.MakeButton("معاينة 👁️", 230, 8, 125, 40, Theme.Accent);
            btnPreview.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnPreview.Click += (s, e) => StartPrintJob(true);

            btnCancel = Theme.MakeButton("إلغاء ↩", 90, 8, 125, 40, Color.FromArgb(70, 80, 95));
            btnCancel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnCancel.Click += (s, e) => CloseOrNavigateBack();

            pnlActions.Controls.AddRange(new Control[] { btnPrint, btnPreview, btnCancel });

            pnlBottom.Controls.Add(pnlActions);
            pnlBottom.Controls.Add(pnlSettings);

            // Add Control Tree in correct Z-Order for WinForms Top-to-Bottom Docking
            this.Controls.Add(pnlGridContainer);  // Fill
            this.Controls.Add(pnlBottom);         // Bottom
            this.Controls.Add(pnlGridHeader);     // Top (3rd from top)
            this.Controls.Add(pnlSelection);      // Top (2nd from top)
            this.Controls.Add(pnlTop);            // Top (1st at top)

            Theme.ApplyFormRTL(this);
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

            try
            {
                var pd = new PrintDocument();
                
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

                AppConfig.SetPaperSize(pd, AppConfig.BarcodeStickerSize);

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
                    pd.Print();
                    MessageBox.Show("تم إرسال أمر الطباعة بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            string labelType = (AppConfig.BarcodeStickerSize == "38x26_double") ? "Split" : "Full";
            int labelsPerRow = (labelType == "Split") ? 2 : 1;

            float pageWidth = e.PageBounds.Width;
            float pageHeight = e.PageBounds.Height;
            float leftMargin = 5;
            float topMargin = 5;

            float labelWidth = labelType == "Full" ? pageWidth : (pageWidth / labelsPerRow);
            float labelHeight = pageHeight - (topMargin * 2);

            bool isSmallSticker = (AppConfig.BarcodeStickerSize == "38x26" || AppConfig.BarcodeStickerSize == "38x26_double");

            string template = "Standard";
            bool isCode128 = true;

            if (cboBarcodeTemplate.SelectedItem != null)
            {
                template = cboBarcodeTemplate.SelectedIndex == 1 ? "PriceHeavy"
                         : cboBarcodeTemplate.SelectedIndex == 2 ? "Small"
                         : cboBarcodeTemplate.SelectedIndex == 3 ? "Shelf"
                         : "Standard";
            }
            if (cboBarcodeEncoding.SelectedItem != null)
            {
                isCode128 = cboBarcodeEncoding.SelectedIndex == 0;
            }

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
                    if (chkPrintCompanyName.Checked)
                    {
                        g.DrawString(AppConfig.CompanyName, fCompany, Brushes.Gray, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                        y += isSmallSticker ? 11 : 14;
                    }
                    g.DrawString(item.ProductName, fNameLarge, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 22 : 30), center);
                    y += isSmallSticker ? 24 : 32;
                    if (chkPrintPrice.Checked)
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

                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"{item.Price:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    }
                }
                else if (template == "PriceHeavy")
                {
                    if (chkPrintPrice.Checked)
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
                else
                {
                    if (chkPrintCompanyName.Checked)
                    {
                        g.DrawString(AppConfig.CompanyName, fCompany, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
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
                    if (chkPrintPrice.Checked)
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
            List<int> pattern = EncodeCode128B(text);
            if (pattern == null || pattern.Count == 0) return;

            int totalModules = 0;
            foreach (int m in pattern) totalModules += m;

            float moduleWidth = width / totalModules;
            if (moduleWidth < 0.5f) moduleWidth = 0.5f;

            float curX = x + (width - (totalModules * moduleWidth)) / 2;
            bool bar = true;

            foreach (int m in pattern)
            {
                float w = m * moduleWidth;
                if (bar)
                {
                    g.FillRectangle(Brushes.Black, curX, y, w, height);
                }
                curX += w;
                bar = !bar;
            }
        }

        private List<int> EncodeCode128B(string text)
        {
            var pattern = new List<int>();
            int[][] codeTable = GetCode128Table();

            int checksum = 104;
            pattern.AddRange(codeTable[104]);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int val = c - 32;
                if (val < 0 || val > 94) val = 0;
                checksum += val * (i + 1);
                pattern.AddRange(codeTable[val]);
            }

            checksum %= 103;
            pattern.AddRange(codeTable[checksum]);

            pattern.AddRange(new int[] { 2, 3, 3, 1, 1, 1, 2 });
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
    }
}

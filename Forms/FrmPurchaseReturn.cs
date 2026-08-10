using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة مرتجع مشتريات مطورة لحساب الخصم وشبكة فواتير وأصناف مقسمة</summary>
    public class FrmPurchaseReturn : Form
    {
        private ComboBox cboMode, cboSupplier, cboWarehouse, cboReturnType, cboAllProducts;
        private TextBox txtNotes, txtSearch, txtGenQty, txtGenPrice;
        private DataGridView dgPurchases, dgItems;
        private Button btnSave, btnAddGenItem, btnSearch;
        private Label lblTotal, lblPurchaseInfo;
        private DateTimePicker dtpFrom, dtpTo;
        private SplitContainer _mainSplit;
        private FlowLayoutPanel pnlGeneralItemBar;
        private int _selectedPurchaseID = 0;

        public FrmPurchaseReturn()
        {
            InitUI();
            LoadCombos();
            LoadPurchasesGrid();
        }

        private void FrmPurchaseReturn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                if (dgItems.IsCurrentCellInEditMode) dgItems.EndEdit();
                btnSave.PerformClick();
                e.Handled = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                if (dgItems.Focused || dgItems.EditingControl != null)
                {
                    dgItems.EndEdit();
                    var curCell = dgItems.CurrentCell;
                    if (curCell != null && curCell.RowIndex >= 0 && curCell.RowIndex < dgItems.Rows.Count)
                    {
                        int nextCol = -1;
                        for (int col = curCell.ColumnIndex + 1; col < dgItems.ColumnCount; col++)
                        {
                            if (!dgItems.Columns[col].ReadOnly && dgItems.Columns[col].Visible)
                            { nextCol = col; break; }
                        }
                        if (nextCol != -1)
                        {
                            dgItems.CurrentCell = dgItems.Rows[curCell.RowIndex].Cells[nextCol];
                            dgItems.BeginEdit(true);
                            return true;
                        }
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitUI()
        {
            this.Text = "مرتجع مشتريات (على فاتورة / عام)";
            this.Size = new Size(1180, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += FrmPurchaseReturn_KeyDown;

            // ===== شريط العنوان =====
            var pnlTitle = Theme.MakeTitleBar("↩ مرتجع مشتريات", "إرجاع بضاعة بسعر الشراء الصافي بعد الخصم مع تقسيم الشاشة لفواتير وأصناف");
            pnlTitle.Dock = DockStyle.Top;

            // ===== شريط الفلتر الأعلى =====
            var pnlInfo = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                WrapContents = true
            };

            // نوع المرتجع
            var lblMode = new Label { Text = "نوع المرتجع:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 8, 0, 0), Font = Theme.FontBold };
            cboMode = new ComboBox
            {
                Width = 210, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            cboMode.Items.Add("🧾 مرتجع على فاتورة شراء معينة");
            cboMode.Items.Add("🌐 مرتجع شراء عام (بدون فاتورة)");
            cboMode.SelectedIndex = 0;
            cboMode.SelectedIndexChanged += (s, e) => ToggleReturnMode();

            // المخزن
            var lblWh = new Label { Text = "المخزن:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
            cboWarehouse = new ComboBox
            {
                Width = 140, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            // طريقة التسوية
            var lblRetType = new Label { Text = "التسوية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
            cboReturnType = new ComboBox
            {
                Width = 110, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };
            cboReturnType.Items.Add("📋 آجل");
            cboReturnType.Items.Add("💵 نقدي");
            cboReturnType.SelectedIndex = 0;

            // تواريخ البحث
            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
            dtpFrom = new DateTimePicker { Width = 110, Height = 26, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 8, 0, 0), Font = Theme.FontBold };
            dtpTo = new DateTimePicker { Width = 110, Height = 26, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            // جهة الشراء
            var lblSupplierLbl = new Label { Text = "جهة الشراء:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
            cboSupplier = new ComboBox
            {
                Width = 180, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            // نص البحث
            var lblSearch = new Label { Text = "بحث الفواتير:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(15, 8, 0, 0), Font = Theme.FontBold };
            txtSearch = new TextBox
            {
                Width = 150, Height = 26,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadPurchasesGrid(); e.Handled = true; e.SuppressKeyPress = true; } };

            btnSearch = Theme.MakeButton("🔍 جلب الفواتير", Theme.Accent);
            btnSearch.Size = new Size(110, 28);
            btnSearch.Margin = new Padding(10, 2, 0, 0);
            btnSearch.Click += (s, e) => LoadPurchasesGrid();

            pnlInfo.Controls.AddRange(new Control[] {
                lblMode, cboMode,
                lblWh, cboWarehouse,
                lblRetType, cboReturnType,
                lblFrom, dtpFrom, lblTo, dtpTo,
                lblSupplierLbl, cboSupplier,
                lblSearch, txtSearch, btnSearch
            });

            // ===== SplitContainer للتخطيط المقسم (فواتير فوق / أصناف تحت) =====
            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 210,
                BackColor = Theme.BorderColor,
                Padding = new Padding(5)
            };

            // ── الجزء الأعلى: جدول فواتير الشراء ──
            var pnlTop = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };
            var lblTopTitle = new Label
            {
                Text = "📋 فواتير الشراء المطابقة للبحث (اختر الفاتورة لعرض أصنافها والخصم بالأسفل):",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 80, 160),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            dgPurchases = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(35, 65, 110),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                },
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 32,
                EnableHeadersVisualStyles = false
            };

            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseID", Visible = false });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseCode", HeaderText = "رقم الفاتورة", FillWeight = 60 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierInvoiceNo", HeaderText = "فاتورة المورد", FillWeight = 60 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseDate", HeaderText = "التاريخ والوقت", FillWeight = 75 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "جهة الشراء", FillWeight = 90 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseTypeStr", HeaderText = "نوع الفاتورة", FillWeight = 50 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", HeaderText = "قبل الخصم", FillWeight = 55 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountAmount", HeaderText = "الخصم", FillWeight = 45 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "الصافي النهائي", FillWeight = 65 });
            dgPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات", FillWeight = 100 });

            dgPurchases.SelectionChanged += DgPurchases_SelectionChanged;
            pnlTop.Controls.Add(dgPurchases);
            pnlTop.Controls.Add(lblTopTitle);
            _mainSplit.Panel1.Controls.Add(pnlTop);

            // ── الجزء الأسفل: شريط المرتجع العام + جدول الأصناف ──
            var pnlBottom = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };

            pnlGeneralItemBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(5, 5, 5, 5),
                Visible = false
            };

            var lblGenProd = new Label { Text = "الصنف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 6, 0, 0), Font = Theme.FontBold };
            cboAllProducts = new ComboBox
            {
                Width = 220, Height = 26,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain
            };

            var lblGenQty = new Label { Text = "الكمية:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 6, 0, 0), Font = Theme.FontBold };
            txtGenQty = new TextBox { Width = 70, Height = 26, Text = "1", BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle };

            var lblGenPrice = new Label { Text = "سعر الشراء الصافي:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 6, 0, 0), Font = Theme.FontBold };
            txtGenPrice = new TextBox { Width = 80, Height = 26, Text = "0", BackColor = Theme.BgInput, ForeColor = Theme.TextMain, RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle };

            btnAddGenItem = Theme.MakeButton("➕ إضافة للصنف المرتجع", Theme.Success);
            btnAddGenItem.Size = new Size(160, 26);
            btnAddGenItem.Margin = new Padding(10, 1, 0, 0);
            btnAddGenItem.Click += BtnAddGenItem_Click;

            pnlGeneralItemBar.Controls.AddRange(new Control[] {
                lblGenProd, cboAllProducts,
                lblGenQty, txtGenQty,
                lblGenPrice, txtGenPrice,
                btnAddGenItem
            });

            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 90, 50),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                },
                GridColor = Theme.BorderColor,
                ColumnHeadersHeight = 34,
                EnableHeadersVisualStyles = false
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", ReadOnly = true, FillWeight = 110 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasedQty", HeaderText = "الكمية بالفاتورة", ReadOnly = true, FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrevReturnedQty", HeaderText = "المرتجع السابق", ReadOnly = true, FillWeight = 50 });

            var colNew = new DataGridViewTextBoxColumn
            {
                Name = "NewReturnedQty",
                HeaderText = "المرتجع الجديد (تعديل مباشر)",
                ReadOnly = false,
                FillWeight = 65,
                ValueType = typeof(decimal)
            };
            colNew.DefaultCellStyle.BackColor = Color.FromArgb(40, 60, 45);
            colNew.DefaultCellStyle.ForeColor = Color.LightGreen;
            colNew.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgItems.Columns.Add(colNew);

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "GrossUnitPrice", HeaderText = "سعر الشراء الأصلي", ReadOnly = true, FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountPct", HeaderText = "نسبة الخصم %", ReadOnly = true, FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetUnitPrice", HeaderText = "سعر الشراء الصافي", ReadOnly = true, FillWeight = 55 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "إجمالي المرتجع", ReadOnly = true, FillWeight = 60 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Factor", Visible = false });

            dgItems.CellValidating += DgItems_CellValidating;
            dgItems.CellValueChanged += DgItems_CellValueChanged;

            pnlBottom.Controls.Add(dgItems);
            pnlBottom.Controls.Add(pnlGeneralItemBar);
            _mainSplit.Panel2.Controls.Add(pnlBottom);

            // ===== شريط الذيل السفلي =====
            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblNotesLbl = new Label { Text = "ملاحظات المرتجع:", Location = new Point(15, 12), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            txtNotes = new TextBox
            {
                Location = new Point(125, 8),
                Width = 260, Height = 26,
                BackColor = Theme.BgInput, ForeColor = Theme.TextMain,
                RightToLeft = RightToLeft.Yes, BorderStyle = BorderStyle.FixedSingle
            };

            lblPurchaseInfo = new Label
            {
                Location = new Point(400, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(10, 120, 180),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = ""
            };

            lblTotal = new Label
            {
                Text = "إجمالي مرتجع الشراء: 0.00 ج",
                ForeColor = Color.FromArgb(20, 160, 80),
                Dock = DockStyle.Right,
                Width = 320,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            btnSave = Theme.MakeButton("💾 حفظ مرتجع الشراء (F5)", Color.FromArgb(30, 110, 50));
            btnSave.Location = new Point(15, 34);
            btnSave.Size = new Size(220, 28);
            btnSave.Font = Theme.FontBold;
            btnSave.Click += BtnSave_Click;

            pnlFoot.Controls.AddRange(new Control[] { lblNotesLbl, txtNotes, lblPurchaseInfo, lblTotal, btnSave });

            // ===== تجميع الشاشة =====
            this.Controls.Add(_mainSplit);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlFoot);
            this.Controls.Add(pnlTitle);

            _mainSplit.BringToFront();
            Theme.ApplyFormRTL(this);
        }

        private void ToggleReturnMode()
        {
            bool isGeneral = cboMode.SelectedIndex == 1;

            _mainSplit.Panel1Collapsed = isGeneral;
            pnlGeneralItemBar.Visible = isGeneral;

            dgItems.Rows.Clear();
            RecalcTotal();

            if (isGeneral)
            {
                cboSupplier.Enabled = true;
                lblPurchaseInfo.Text = "مرتجع عام دون التقيد ببيانات فاتورة محددة";
            }
            else
            {
                lblPurchaseInfo.Text = "";
                LoadPurchasesGrid();
            }
        }

        private void LoadCombos()
        {
            var dtS = SupplierDAL.GetAll(true);
            cboSupplier.Items.Clear();
            cboSupplier.Items.Add(new ComboItem(0, "-- الكل (الموردين والعملاء) --"));
            foreach (DataRow r in dtS.Rows)
                cboSupplier.Items.Add(new ComboItem((int)r["SupplierID"], r["SupplierName"].ToString()));
            cboSupplier.DisplayMember = "Text";
            cboSupplier.SelectedIndex = 0;
            cboSupplier.SelectedIndexChanged += (s, e) => LoadPurchasesGrid();

            // المخازن
            var dtWh = WarehouseDAL.GetAll(true);
            cboWarehouse.Items.Clear();
            foreach (DataRow r in dtWh.Rows)
                cboWarehouse.Items.Add(new ComboItem((int)r["WarehouseID"], r["WarehouseName"].ToString()));
            cboWarehouse.DisplayMember = "Text";
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;

            // جميع الأصناف للمرتجع العام
            var dtProd = ProductDAL.GetAll(true);
            cboAllProducts.Items.Clear();
            cboAllProducts.Items.Add(new ComboItem(0, "-- اختر صنف --"));
            foreach (DataRow r in dtProd.Rows)
            {
                var ci = new ComboItem((int)r["ProductID"], r["ProductName"].ToString());
                ci.Extra = r["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(r["PurchasePrice"]) : 0m;
                cboAllProducts.Items.Add(ci);
            }
            cboAllProducts.DisplayMember = "Text";
            if (cboAllProducts.Items.Count > 0) cboAllProducts.SelectedIndex = 0;
            cboAllProducts.SelectedIndexChanged += (s, e) =>
            {
                if (cboAllProducts.SelectedItem is ComboItem ci && ci.ID > 0)
                {
                    txtGenPrice.Text = ci.Extra.ToString("N2");
                }
            };
        }

        private void LoadPurchasesGrid()
        {
            if (cboMode.SelectedIndex == 1) return; // General Return

            int? supplierID = null;
            if (cboSupplier.SelectedItem is ComboItem cs && cs.ID > 0)
                supplierID = cs.ID;

            string searchStr = txtSearch != null ? txtSearch.Text.Trim() : null;
            if (string.IsNullOrEmpty(searchStr)) searchStr = null;

            var dtP = PurchaseDAL.GetAll(dtpFrom.Value.Date, dtpTo.Value.Date, supplierID, searchStr);
            dgPurchases.SelectionChanged -= DgPurchases_SelectionChanged;
            dgPurchases.Rows.Clear();
            dgItems.Rows.Clear();
            lblTotal.Text = "إجمالي مرتجع الشراء: 0.00 ج";
            lblPurchaseInfo.Text = "";

            foreach (DataRow r in dtP.Rows)
            {
                int rowIdx = dgPurchases.Rows.Add();
                var row = dgPurchases.Rows[rowIdx];
                row.Cells["PurchaseID"].Value        = r["PurchaseID"];
                row.Cells["PurchaseCode"].Value      = r["PurchaseCode"];
                row.Cells["SupplierInvoiceNo"].Value = r["SupplierInvoiceNo"] != DBNull.Value ? r["SupplierInvoiceNo"].ToString() : "";
                row.Cells["PurchaseDate"].Value      = Convert.ToDateTime(r["PurchaseDate"]).ToString("dd/MM/yyyy HH:mm");
                row.Cells["SupplierName"].Value      = r["SupplierName"];
                row.Cells["PurchaseTypeStr"].Value  = r["PurchaseType"].ToString() == "Cash" ? "🟡 نقدي" : "🔵 آجل";
                row.Cells["SubTotal"].Value         = Convert.ToDecimal(r["SubTotal"]).ToString("N2");
                row.Cells["DiscountAmount"].Value   = Convert.ToDecimal(r["DiscountAmount"]).ToString("N2");
                row.Cells["TotalAmount"].Value      = Convert.ToDecimal(r["TotalAmount"]).ToString("N2");
                row.Cells["Notes"].Value            = r["Notes"] != DBNull.Value ? r["Notes"].ToString() : "";
            }

            dgPurchases.SelectionChanged += DgPurchases_SelectionChanged;
            if (dgPurchases.Rows.Count > 0)
            {
                dgPurchases.Rows[0].Selected = true;
                DgPurchases_SelectionChanged(this, EventArgs.Empty);
            }
        }

        private void DgPurchases_SelectionChanged(object sender, EventArgs e)
        {
            if (cboMode.SelectedIndex == 1) return; // General return

            dgItems.Rows.Clear();
            lblTotal.Text = "إجمالي مرتجع الشراء: 0.00 ج";
            lblPurchaseInfo.Text = "";

            if (dgPurchases.SelectedRows.Count == 0)
            {
                _selectedPurchaseID = 0;
                return;
            }

            var selectedRow = dgPurchases.SelectedRows[0];
            int purchaseID = Convert.ToInt32(selectedRow.Cells["PurchaseID"].Value);
            _selectedPurchaseID = purchaseID;

            LoadPurchaseItems(purchaseID, selectedRow);
        }

        private void LoadPurchaseItems(int purchaseID, DataGridViewRow purRow)
        {
            dgItems.Rows.Clear();

            // حساب معامل خصم الفاتورة الإجمالي (Header Discount Factor)
            decimal subTotal = Convert.ToDecimal(purRow.Cells["SubTotal"].Value ?? 0);
            decimal headerDisc = Convert.ToDecimal(purRow.Cells["DiscountAmount"].Value ?? 0);
            decimal headerFactor = (subTotal > 0 && headerDisc > 0) ? ((subTotal - headerDisc) / subTotal) : 1.0m;

            string purTypeStr = purRow.Cells["PurchaseTypeStr"].Value?.ToString() ?? "";
            decimal netPurTotal = Convert.ToDecimal(purRow.Cells["TotalAmount"].Value ?? 0);
            lblPurchaseInfo.Text = $"الفاتورة رقم ({purRow.Cells["PurchaseCode"].Value}) | الصافي المسدد: {netPurTotal:N2} ج | النوع: {purTypeStr}";

            // تلقائياً حدد المورد من الفاتورة المختارة
            var dtPur = DbHelper.Query(
                "SELECT SupplierID, ClientID, PurchaseSource FROM Purchases WHERE PurchaseID=@pid",
                DbHelper.P("@pid", purchaseID));
            if (dtPur.Rows.Count > 0 && dtPur.Rows[0]["SupplierID"] != DBNull.Value)
            {
                int sid = Convert.ToInt32(dtPur.Rows[0]["SupplierID"]);
                for (int i = 0; i < cboSupplier.Items.Count; i++)
                {
                    if (cboSupplier.Items[i] is ComboItem ci && ci.ID == sid)
                    {
                        cboSupplier.SelectedIndex = i;
                        break;
                    }
                }
            }

            // تحميل أصناف الفاتورة مع المرتجع السابق
            var dtItems = PurchaseDAL.GetItems(purchaseID);
            var dtPrevRet = DbHelper.Query(
                @"SELECT pri.ProductID, ISNULL(SUM(pri.Quantity),0) AS ReturnedQty
                  FROM PurchaseReturnItems pri
                  JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                  WHERE pr.PurchaseID = @pid
                  GROUP BY pri.ProductID",
                DbHelper.P("@pid", purchaseID));

            var prevMap = new Dictionary<int, decimal>();
            foreach (DataRow r in dtPrevRet.Rows)
                prevMap[Convert.ToInt32(r["ProductID"])] = Convert.ToDecimal(r["ReturnedQty"]);

            foreach (DataRow r in dtItems.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                decimal purQty = Convert.ToDecimal(r["Quantity"]);
                decimal prevRet = prevMap.ContainsKey(pid) ? prevMap[pid] : 0m;
                decimal remaining = purQty - prevRet;

                if (remaining <= 0) continue; // تم إرجاع الفاتورة بالكامل مسبقاً

                decimal grossUnitPrice = Convert.ToDecimal(r["UnitPrice"]);
                decimal itemDiscPct = Convert.ToDecimal(r["DiscountPct"]);

                // حساب سعر الشراء الصافي الحقيقي بعد الخصم المباشر وخصم الفاتورة
                decimal itemPriceAfterDisc = grossUnitPrice * (1.0m - (itemDiscPct / 100.0m));
                decimal netUnitPrice = Math.Round(itemPriceAfterDisc * headerFactor, 4);

                // نسبة الخصم الإجمالية الفعلية
                decimal effectiveDiscPct = grossUnitPrice > 0 ? Math.Round(((grossUnitPrice - netUnitPrice) / grossUnitPrice) * 100.0m, 2) : 0m;

                int rowIdx = dgItems.Rows.Add();
                var row = dgItems.Rows[rowIdx];
                row.Cells["ProductID"].Value       = pid;
                row.Cells["ProductName"].Value     = r["ProductName"].ToString();
                row.Cells["PurchasedQty"].Value    = purQty.ToString("N3");
                row.Cells["PrevReturnedQty"].Value = prevRet.ToString("N3");
                row.Cells["NewReturnedQty"].Value  = 0;
                row.Cells["GrossUnitPrice"].Value  = grossUnitPrice.ToString("N2");
                row.Cells["DiscountPct"].Value     = effectiveDiscPct > 0 ? (effectiveDiscPct.ToString("N2") + "%") : "0%";
                row.Cells["NetUnitPrice"].Value    = netUnitPrice.ToString("N2");
                row.Cells["TotalPrice"].Value      = "0.00";
                row.Cells["UnitName"].Value        = r["UnitName"]?.ToString() ?? "";
                row.Cells["Factor"].Value          = r["Factor"] != DBNull.Value ? r["Factor"] : 1.0m;
            }

            if (dgItems.Rows.Count > 0)
                dgItems.CurrentCell = dgItems.Rows[0].Cells["NewReturnedQty"];
        }

        private void BtnAddGenItem_Click(object sender, EventArgs e)
        {
            if (!(cboAllProducts.SelectedItem is ComboItem ci) || ci.ID == 0)
            {
                MessageBox.Show("اختر صنفاً أولاً من القائمة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtGenQty.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("أدخل كمية صالحة أكبر من صفر", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtGenPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("أدخل سعر شراء صالح", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rowIdx = dgItems.Rows.Add();
            var row = dgItems.Rows[rowIdx];
            row.Cells["ProductID"].Value       = ci.ID;
            row.Cells["ProductName"].Value     = ci.Text;
            row.Cells["PurchasedQty"].Value    = "عام";
            row.Cells["PrevReturnedQty"].Value = "0";
            row.Cells["NewReturnedQty"].Value  = qty;
            row.Cells["GrossUnitPrice"].Value  = price.ToString("N2");
            row.Cells["DiscountPct"].Value     = "0%";
            row.Cells["NetUnitPrice"].Value    = price.ToString("N2");
            row.Cells["TotalPrice"].Value      = (qty * price).ToString("N2");
            row.Cells["UnitName"].Value        = "";
            row.Cells["Factor"].Value          = 1.0m;

            RecalcTotal();
        }

        private void DgItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dgItems.Columns[e.ColumnIndex].Name != "NewReturnedQty") return;
            if (e.FormattedValue?.ToString() == "") return;
            if (!decimal.TryParse(e.FormattedValue.ToString(), out decimal val) || val < 0)
            {
                MessageBox.Show("أدخل كمية صالحة (رقم موجب أو صفر)", "تحقق من الإدخال",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void DgItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgItems.Columns[e.ColumnIndex].Name != "NewReturnedQty") return;
            var row = dgItems.Rows[e.RowIndex];
            decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal qty);
            decimal.TryParse(row.Cells["NetUnitPrice"].Value?.ToString(), out decimal netPrice);
            row.Cells["TotalPrice"].Value = (qty * netPrice).ToString("N2");
            RecalcTotal();
        }

        private void RecalcTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["TotalPrice"].Value != null)
                {
                    decimal.TryParse(row.Cells["TotalPrice"].Value.ToString(), out decimal t);
                    total += t;
                }
            }
            lblTotal.Text = "إجمالي مرتجع الشراء: " + total.ToString("N2") + " ج";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Session.CanAdd("PurchaseReturn"))
            {
                MessageBox.Show("⛔ ليس لديك صلاحية حفظ مرتجعات المشتريات.", "رفض الوصول", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isGeneral = cboMode.SelectedIndex == 1;

            if (!isGeneral && _selectedPurchaseID <= 0)
            {
                MessageBox.Show("يجب اختيار فاتورة الشراء الأصلية من الجدول أعلى أولاً", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var returnItems = new List<PurchaseItemDTO>();
            decimal totalReturnAmount = 0;

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                int prodID  = Convert.ToInt32(row.Cells["ProductID"].Value);
                string name = row.Cells["ProductName"].Value.ToString();
                decimal.TryParse(row.Cells["NewReturnedQty"].Value?.ToString(), out decimal newQty);
                if (newQty <= 0) continue;

                if (!isGeneral)
                {
                    decimal.TryParse(row.Cells["PurchasedQty"].Value?.ToString(), out decimal purQty);
                    decimal.TryParse(row.Cells["PrevReturnedQty"].Value?.ToString(), out decimal prevQty);

                    if (newQty + prevQty > purQty)
                    {
                        MessageBox.Show(
                            $"الكمية المرتجعة للصنف ({name}) تتجاوز الكمية الأصلية بالفاتورة!\n" +
                            $"المشتريات: {purQty:N3} | السابق: {prevQty:N3} | الجديد: {newQty:N3}",
                            "تجاوز الكمية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                decimal.TryParse(row.Cells["NetUnitPrice"].Value?.ToString(), out decimal netPrice);
                string unitName = row.Cells["UnitName"].Value?.ToString() ?? "";
                decimal.TryParse(row.Cells["Factor"].Value?.ToString(), out decimal fac);
                if (fac <= 0) fac = 1.0m;

                returnItems.Add(new PurchaseItemDTO
                {
                    ProductID = prodID,
                    ProductName = name,
                    Quantity = newQty,
                    UnitPrice = netPrice,
                    UnitName = unitName,
                    Factor = fac
                });
                totalReturnAmount += newQty * netPrice;
            }

            if (returnItems.Count == 0)
            {
                MessageBox.Show("يرجى إدخال كمية مرتجعة صالحة (أكبر من صفر) لصنف واحد على الأقل.",
                    "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int purchaseID = isGeneral ? 0 : _selectedPurchaseID;
            int? supplierID = (cboSupplier.SelectedItem is ComboItem cs && cs.ID > 0) ? (int?)cs.ID : null;
            int? warehouseID = (cboWarehouse.SelectedItem is ComboItem cw && cw.ID > 0) ? (int?)cw.ID : 1;
            string returnType = cboReturnType.SelectedIndex == 1 ? "Cash" : "Credit";

            if (isGeneral && returnType == "Credit" && !supplierID.HasValue)
            {
                MessageBox.Show("يرجى اختيار المورد/العميل أولاً لمرتجع الشراء العام الآجل!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int id = PurchaseReturnDAL.SavePurchaseReturn(purchaseID, supplierID, totalReturnAmount,
                    txtNotes.Text, returnItems, warehouseID, returnType);
                if (id > 0)
                {
                    MessageBox.Show("✅ تم حفظ مرتجع الشراء بنجاح!\nتم احتساب الصافي وتحديث المخزن وحساب المورد/العميل تلقائياً.",
                        "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtNotes.Text = "";
                    dgItems.Rows.Clear();
                    lblTotal.Text = "إجمالي مرتجع الشراء: 0.00 ج";
                    if (lblPurchaseInfo != null) lblPurchaseInfo.Text = "";

                    if (!isGeneral)
                    {
                        LoadPurchasesGrid();
                    }
                }
                else
                {
                    MessageBox.Show("فشل حفظ المرتجع", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل حفظ مرتجع المشتريات", ex, "FrmPurchaseReturn.BtnSave_Click");
                MessageBox.Show($"❌ حدث خطأ أثناء الحفظ:\n{ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

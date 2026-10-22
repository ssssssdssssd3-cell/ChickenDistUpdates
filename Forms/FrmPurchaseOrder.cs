using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة إعداد طلبية مشتريات / أمر شراء للمورد (Purchase Order).
    /// تتيح تحديد المشتريات والتحكم في الكميات والأسعار مع الاقتراح الذكي بناءً على الأرصدة وسرعة البيع،
    /// وإرسالها للمورد عبر واتساب أو إكسيل أو طباعة A4، وتعليقها لاسترجاعها لاحقاً في فاتورة المشتريات.
    /// </summary>
    public class FrmPurchaseOrder : Form
    {
        private int _orderID = 0;
        private PurchaseOrderDTO _order;

        // ── أدوات الرأس ──
        private TextBox txtOrderCode;
        private DateTimePicker dtpOrderDate, dtpDeliveryDate;
        private ComboBox cboSupplier;
        private TextBox txtSupplierPhone;
        private Label lblStatusBadge;
        private TextBox txtNotes;

        // ── أدوات الإضافة السريعة ──
        private TextBox txtQuickSearch;
        private ListBox lstProductLookup;
        private NumericUpDown nudQuickQty;
        private TextBox txtQuickPrice;
        private Button btnQuickAdd;
        private Button btnSmartSuggest;
        private Button btnProductSearch;

        private int? _selectedQuickPID;
        private string _selectedQuickCode;
        private string _selectedQuickName;
        private string _selectedQuickUnit;
        private decimal _selectedQuickStock;

        // ── شبكة الأصناف ──
        private DataGridView dgvItems;

        // ── شريط الإجماليات ──
        private Label lblItemsCountVal, lblTotalQtyVal, lblTotalCostVal;

        // ── أزرار العمليات ──
        private Button btnSaveOrder, btnSendWhatsApp, btnPrintOrder, btnExportExcel, btnConvertToPurchase, btnOpenOrdersList, btnNewOrder;

        public FrmPurchaseOrder(int orderID = 0)
        {
            if (!Session.CanAdd("Purchases") && !Session.CanAccess("PurchaseOrder") && !Session.IsAdmin)
            {
                MessageBox.Show("⛔ غير مصرح لك بإنشاء أوامر وطلبيات الشراء.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            _orderID = orderID;
            InitUI();
            LoadSuppliers();

            if (_orderID > 0)
            {
                LoadExistingOrder(_orderID);
            }
            else
            {
                StartNewOrder();
            }
        }

        private void InitUI()
        {
            this.Text = "📋 إعداد طلبية مشتريات / أمر شراء للمورد";
            this.Size = new Size(1260, 800);
            this.MinimumSize = new Size(1024, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F1)
                {
                    OpenProductSearch();
                    e.Handled = true;
                }
            };

            // 1. ترويسة رئيسية
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "📋 إعداد طلبية مشتريات / أمر شراء للمورد (Purchase Order)",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblSub = new Label
            {
                Text = "تحديد أصناف الشراء والتحكم في الكميات والأسعار المتوقعة، إرسال أمر الشراء للمورد عبر واتساب أو ملف، وتعليق الطلبية للتوريد",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // 2. بطاقة بيانات الطلبية والمورد
            var pnlOrderInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 6, 12, 6),
                Margin = new Padding(0, 0, 0, 4)
            };

            var flowInfo = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent
            };

            // كود الطلبية
            txtOrderCode = new TextBox { Width = 115, ReadOnly = true, Font = Theme.FontBold, BackColor = Color.FromArgb(241, 245, 249), TextAlign = HorizontalAlignment.Center };
            flowInfo.Controls.Add(MakeInfoGroup("كود الطلبية 🔖:", txtOrderCode));

            // التاريخ
            dtpOrderDate = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            flowInfo.Controls.Add(MakeInfoGroup("تاريخ الطلب 📅:", dtpOrderDate));

            // المورد
            cboSupplier = new ComboBox { Width = 185, DropDownStyle = ComboBoxStyle.DropDownList };
            cboSupplier.SelectedIndexChanged += CboSupplier_SelectedIndexChanged;
            flowInfo.Controls.Add(MakeInfoGroup("المورد 👤:", cboSupplier));

            // هاتف المورد (للواتساب)
            txtSupplierPhone = new TextBox { Width = 125 };
            flowInfo.Controls.Add(MakeInfoGroup("هاتف الواتساب 📱:", txtSupplierPhone));

            // موعد التوريد المتوقع
            dtpDeliveryDate = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(2) };
            flowInfo.Controls.Add(MakeInfoGroup("التوريد المتوقع ⏳:", dtpDeliveryDate));

            // حالة الطلبية
            lblStatusBadge = new Label
            {
                Text = "معلقة 🟡",
                AutoSize = false,
                Width = 90,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(254, 243, 199),
                ForeColor = Color.FromArgb(180, 83, 9),
                Font = Theme.FontBold,
                Margin = new Padding(3, 18, 10, 2)
            };
            flowInfo.Controls.Add(MakeInfoGroup("الحالة:", lblStatusBadge));

            // ملاحظات
            txtNotes = new TextBox { Width = 220 };
            flowInfo.Controls.Add(MakeInfoGroup("ملاحظات الطلبية 📝:", txtNotes));

            pnlOrderInfo.Controls.Add(flowInfo);

            // 3. شريط الاقتراح الذكي والإضافة السريعة
            var pnlQuickAdd = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 6, 12, 6)
            };

            var flowQuick = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            // زر الاقتراح الذكي المميز
            btnSmartSuggest = Theme.MakeButton("💡 الاقتراح الذكي (حركة المبيعات والأرصدة)", 0, 0, 290, 34, Color.FromArgb(139, 92, 246));
            btnSmartSuggest.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSmartSuggest.Margin = new Padding(2, 6, 12, 2);
            btnSmartSuggest.Click += (s, e) => OpenSmartSuggestionsDialog();

            // زر بحث الأصناف الشامل المتقدم
            btnProductSearch = Theme.MakeButton("🔍 بحث الأصناف (F1)", 0, 0, 155, 34, Color.FromArgb(16, 185, 129));
            btnProductSearch.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnProductSearch.Margin = new Padding(2, 6, 16, 2);
            btnProductSearch.Click += (s, e) => OpenProductSearch();

            // بحث عن صنف يدوي
            var lblSearch = new Label { Text = "📦 صنف يدوي:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(2, 12, 2, 0) };
            txtQuickSearch = new TextBox { Width = 200, Height = 28, Margin = new Padding(2, 9, 4, 2) };
            txtQuickSearch.TextChanged += TxtQuickSearch_TextChanged;
            txtQuickSearch.KeyDown += TxtQuickSearch_KeyDown;

            var lblQty = new Label { Text = "الكمية:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(6, 12, 2, 0) };
            nudQuickQty = new NumericUpDown { Width = 75, Height = 28, Minimum = 1, Maximum = 99999, Value = 1, Margin = new Padding(2, 9, 4, 2) };

            var lblPrice = new Label { Text = "السعر:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(6, 12, 2, 0) };
            txtQuickPrice = new TextBox { Width = 85, Height = 28, Text = "0.00", Margin = new Padding(2, 9, 8, 2) };

            btnQuickAdd = Theme.MakeButton("➕ إضافة للبند", 0, 0, 110, 32, Color.FromArgb(37, 99, 235));
            btnQuickAdd.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnQuickAdd.Margin = new Padding(2, 7, 4, 2);
            btnQuickAdd.Click += (s, e) => AddQuickProductToGrid();

            flowQuick.Controls.Add(btnSmartSuggest);
            flowQuick.Controls.Add(btnProductSearch);
            flowQuick.Controls.Add(lblSearch);
            flowQuick.Controls.Add(txtQuickSearch);
            flowQuick.Controls.Add(lblQty);
            flowQuick.Controls.Add(nudQuickQty);
            flowQuick.Controls.Add(lblPrice);
            flowQuick.Controls.Add(txtQuickPrice);
            flowQuick.Controls.Add(btnQuickAdd);

            pnlQuickAdd.Controls.Add(flowQuick);

            // قائمة نتائج البحث السريع المنسدلة
            lstProductLookup = new ListBox
            {
                Visible = false,
                Width = 320,
                Height = 160,
                Font = Theme.FontMain,
                RightToLeft = RightToLeft.Yes
            };
            lstProductLookup.DoubleClick += (s, e) => SelectProductFromLookup();
            lstProductLookup.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) SelectProductFromLookup(); };

            // 4. شبكة أصناف الطلبية DataGridView
            dgvItems = new DataGridView
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

            EnableDoubleBuffer(dgvItems);
            SetupItemsGrid();
            dgvItems.CellEndEdit += DgvItems_CellEndEdit;
            dgvItems.CellContentClick += DgvItems_CellContentClick;

            // 5. شريط الإجماليات السفلي
            var pnlSummary = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 6, 15, 6)
            };

            var flowSum = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight
            };

            lblItemsCountVal = new Label { Text = "0", AutoSize = true, Font = Theme.FontBold, ForeColor = Color.FromArgb(37, 99, 235), Margin = new Padding(2, 6, 25, 0) };
            lblTotalQtyVal = new Label { Text = "0.00", AutoSize = true, Font = Theme.FontBold, ForeColor = Color.FromArgb(139, 92, 246), Margin = new Padding(2, 6, 25, 0) };
            lblTotalCostVal = new Label { Text = "0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(5, 150, 105), Margin = new Padding(2, 4, 10, 0) };

            flowSum.Controls.Add(new Label { Text = "عدد الأصناف:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextMain, Margin = new Padding(2, 6, 2, 0) });
            flowSum.Controls.Add(lblItemsCountVal);

            flowSum.Controls.Add(new Label { Text = "إجمالي الكمية المطلوبة:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextMain, Margin = new Padding(2, 6, 2, 0) });
            flowSum.Controls.Add(lblTotalQtyVal);

            flowSum.Controls.Add(new Label { Text = "إجمالي التكلفة التقديرية:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextMain, Margin = new Padding(2, 6, 2, 0) });
            flowSum.Controls.Add(lblTotalCostVal);

            pnlSummary.Controls.Add(flowSum);

            // 6. شريط أزرار العمليات النهائي
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 6, 12, 10),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            btnSaveOrder = Theme.MakeButton("💾 حفظ الطلبية معلقة", 0, 0, 180, 38, Color.FromArgb(5, 150, 105));
            btnSaveOrder.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSaveOrder.Click += (s, e) => SaveOrderToDb();

            btnSendWhatsApp = Theme.MakeButton("📱 إرسال للمورد واتساب", 0, 0, 175, 38, Color.FromArgb(37, 211, 102));
            btnSendWhatsApp.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSendWhatsApp.Click += (s, e) => SendWhatsAppOrder();

            btnPrintOrder = Theme.MakeButton("🖨️ طباعة ومعاينة A4", 0, 0, 150, 38, Color.FromArgb(70, 80, 95));
            btnPrintOrder.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPrintOrder.Click += (s, e) => PrintPurchaseOrder();

            btnExportExcel = Theme.MakeButton("📥 تصدير إكسيل", 0, 0, 130, 38, Color.FromArgb(13, 148, 136));
            btnExportExcel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnExportExcel.Click += (s, e) => ExportOrderToExcel();

            btnConvertToPurchase = Theme.MakeButton("🛒 تحويل مباشر لفاتورة مشتريات", 0, 0, 225, 38, Color.FromArgb(234, 88, 12));
            btnConvertToPurchase.Font = new Font("Segoe UI", 9.8f, FontStyle.Bold);
            btnConvertToPurchase.Click += (s, e) => ConvertDirectlyToPurchaseInvoice();

            btnOpenOrdersList = Theme.MakeButton("📑 سجل طلبيات الشراء", 0, 0, 155, 38, Color.FromArgb(71, 85, 105));
            btnOpenOrdersList.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnOpenOrdersList.Click += (s, e) => OpenOrdersArchive();

            btnNewOrder = Theme.MakeButton("🆕 طلبية جديدة", 0, 0, 115, 38, Color.FromArgb(100, 116, 139));
            btnNewOrder.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnNewOrder.Click += (s, e) => StartNewOrder();

            pnlActions.Controls.Add(btnSaveOrder);
            pnlActions.Controls.Add(btnSendWhatsApp);
            pnlActions.Controls.Add(btnPrintOrder);
            pnlActions.Controls.Add(btnExportExcel);
            pnlActions.Controls.Add(btnConvertToPurchase);
            pnlActions.Controls.Add(btnOpenOrdersList);
            pnlActions.Controls.Add(btnNewOrder);

            // تجميع العناصر بالنافذة
            this.Controls.Add(dgvItems);
            this.Controls.Add(lstProductLookup);
            this.Controls.Add(pnlSummary);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlQuickAdd);
            this.Controls.Add(pnlOrderInfo);
            this.Controls.Add(pnlHeader);

            lstProductLookup.BringToFront();
        }

        private void SetupItemsGrid()
        {
            dgvItems.Columns.Clear();

            // 0. #
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIdx", HeaderText = "#", Width = 45, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            // 1. ProductID
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPID", Visible = false });

            // 2. كود الصنف
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "كود الصنف", Width = 110, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            // 3. اسم الصنف
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "اسم الصنف", Width = 260, ReadOnly = true, DefaultCellStyle = { Font = Theme.FontBold } });

            // 4. التصنيف
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat", HeaderText = "القسم", Width = 110, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            // 5. الوحدة
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "الوحدة", Width = 80, ReadOnly = false, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            // 6. الرصيد الحالي
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock", HeaderText = "الرصيد بالمخزن", Width = 110, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = Theme.FontBold } });

            // 7. الكمية المطلوبة
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "الكمية المطلوبة ✏️", Width = 130, ReadOnly = false, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, BackColor = Color.FromArgb(240, 253, 244), ForeColor = Color.FromArgb(21, 128, 61), Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) } });

            // 8. سعر الشراء المتوقع
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "سعر الشراء المتوقع ✏️", Width = 130, ReadOnly = false, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2", Font = Theme.FontBold } });

            // 9. الإجمالي
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "الإجمالي التقديري", Width = 130, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2", Font = Theme.FontBold, ForeColor = Color.FromArgb(5, 150, 105) } });

            // 10. ملاحظات الصنف
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "ملاحظات على البند", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = false });

            // 11. حذف
            var colDel = new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "حذف",
                Text = "❌",
                UseColumnTextForButtonValue = true,
                Width = 55,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.Red }
            };
            dgvItems.Columns.Add(colDel);
        }

        private void LoadSuppliers()
        {
            try
            {
                cboSupplier.Items.Clear();
                cboSupplier.Items.Add(new POComboItem(0, "-- اختر المورد --"));
                var dt = DbHelper.Query("SELECT SupplierID, SupplierName, Phone FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName");
                foreach (DataRow r in dt.Rows)
                {
                    cboSupplier.Items.Add(new POComboItem(Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString(), r["Phone"]?.ToString() ?? ""));
                }
                cboSupplier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPurchaseOrder.LoadSuppliers", ex);
            }
        }

        private void CboSupplier_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboSupplier.SelectedItem is POComboItem ci && ci.ID > 0)
            {
                txtSupplierPhone.Text = ci.Phone ?? "";
            }
            else
            {
                txtSupplierPhone.Text = "";
            }
        }

        private void StartNewOrder()
        {
            _orderID = 0;
            _order = new PurchaseOrderDTO();
            txtOrderCode.Text = PurchaseOrderDAL.GenerateNextOrderCode();
            dtpOrderDate.Value = DateTime.Today;
            dtpDeliveryDate.Value = DateTime.Today.AddDays(2);
            cboSupplier.SelectedIndex = 0;
            txtSupplierPhone.Text = "";
            txtNotes.Text = "";
            lblStatusBadge.Text = "معلقة 🟡";
            lblStatusBadge.BackColor = Color.FromArgb(254, 243, 199);
            lblStatusBadge.ForeColor = Color.FromArgb(180, 83, 9);
            dgvItems.Rows.Clear();
            RecalculateTotals();
        }

        private void LoadExistingOrder(int orderId)
        {
            try
            {
                _order = PurchaseOrderDAL.GetOrder(orderId);
                if (_order == null)
                {
                    MessageBox.Show("لم يتم العثور على أمر الشراء المطلوب.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    StartNewOrder();
                    return;
                }

                _orderID = _order.OrderID;
                txtOrderCode.Text = _order.OrderCode;
                dtpOrderDate.Value = _order.OrderDate;
                if (_order.ExpectedDeliveryDate.HasValue)
                    dtpDeliveryDate.Value = _order.ExpectedDeliveryDate.Value;

                // تحديد المورد
                for (int i = 0; i < cboSupplier.Items.Count; i++)
                {
                    if (cboSupplier.Items[i] is POComboItem ci && ci.ID == _order.SupplierID)
                    {
                        cboSupplier.SelectedIndex = i;
                        break;
                    }
                }

                txtSupplierPhone.Text = _order.SupplierPhone;
                txtNotes.Text = _order.Notes;

                lblStatusBadge.Text = _order.Status;
                if (_order.Status.Contains("كامل"))
                {
                    lblStatusBadge.BackColor = Color.FromArgb(236, 253, 245);
                    lblStatusBadge.ForeColor = Color.FromArgb(6, 95, 70);
                }
                else
                {
                    lblStatusBadge.BackColor = Color.FromArgb(254, 243, 199);
                    lblStatusBadge.ForeColor = Color.FromArgb(180, 83, 9);
                }

                dgvItems.Rows.Clear();
                foreach (var itm in _order.Items)
                {
                    int rIdx = dgvItems.Rows.Add(
                        dgvItems.Rows.Count + 1,
                        itm.ProductID,
                        itm.ProductCode,
                        itm.ProductName,
                        itm.CategoryName,
                        itm.UnitName,
                        itm.CurrentStockAtOrder.ToString("N2"),
                        itm.Quantity.ToString("N2"),
                        itm.EstimatedUnitPrice.ToString("N2"),
                        itm.TotalPrice.ToString("N2"),
                        itm.Notes
                    );
                    var row = dgvItems.Rows[rIdx];
                    if (itm.CurrentStockAtOrder <= 0)
                        row.Cells["colStock"].Style.ForeColor = Color.FromArgb(220, 38, 38);
                }

                RecalculateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل أمر الشراء:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════
        //  الإضافة السريعة وبحث الأصناف والاقتراح الذكي
        // ══════════════════════════════════════════════

        private void OpenProductSearch()
        {
            string lastSearch = txtQuickSearch.Text?.Trim() ?? "";
            while (true)
            {
                using (var dlg = new FrmProductSearch(warehouseID: null, isPurchaseMode: true, defaultShowZeroStock: true, initialSearchText: lastSearch))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedProductID > 0)
                    {
                        lastSearch = dlg.SearchText;
                        int pid = dlg.SelectedProductID;
                        decimal qty = dlg.SelectedQuantity > 0 ? dlg.SelectedQuantity : 1m;
                        decimal price = dlg.SelectedPurchasePrice > 0 ? dlg.SelectedPurchasePrice : dlg.SelectedPrice;
                        string unit = !string.IsNullOrEmpty(dlg.SelectedUnitName) ? dlg.SelectedUnitName : "قطعة";

                        var dt = DbHelper.Query(@"
                            SELECT TOP 1 
                                p.ProductID, p.ProductCode, p.ProductName, p.Unit,
                                c.CategoryName,
                                COALESCE(stk.TotalStock, p.Quantity, 0) AS CurrentStock
                            FROM Products p
                            LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                            OUTER APPLY (
                                SELECT SUM(ps.Quantity) AS TotalStock FROM ProductStock ps WHERE ps.ProductID = p.ProductID
                            ) stk
                            WHERE p.ProductID = @id",
                            DbHelper.P("@id", pid));

                        if (dt.Rows.Count > 0)
                        {
                            var r = dt.Rows[0];
                            string code = r["ProductCode"]?.ToString() ?? "";
                            string name = r["ProductName"]?.ToString() ?? "";
                            string cat = r["CategoryName"]?.ToString() ?? "عام";
                            decimal stock = r["CurrentStock"] != DBNull.Value ? Convert.ToDecimal(r["CurrentStock"]) : 0m;
                            if (string.IsNullOrEmpty(unit) || unit == "قطعة")
                            {
                                unit = r["Unit"]?.ToString() ?? "قطعة";
                            }

                            // فحص التكرار في الجدول
                            bool exists = false;
                            foreach (DataGridViewRow row in dgvItems.Rows)
                            {
                                if (Convert.ToInt32(row.Cells["colPID"].Value) == pid)
                                {
                                    decimal oldQty = Convert.ToDecimal(row.Cells["colQty"].Value);
                                    decimal newQty = oldQty + qty;
                                    row.Cells["colQty"].Value = newQty.ToString("N2");
                                    row.Cells["colPrice"].Value = price.ToString("N2");
                                    row.Cells["colTotal"].Value = (newQty * price).ToString("N2");
                                    exists = true;
                                    break;
                                }
                            }

                            if (!exists)
                            {
                                decimal total = Math.Round(qty * price, 2);
                                int rIdx = dgvItems.Rows.Add(
                                    dgvItems.Rows.Count + 1,
                                    pid,
                                    code,
                                    name,
                                    cat,
                                    unit,
                                    stock.ToString("N2"),
                                    qty.ToString("N2"),
                                    price.ToString("N2"),
                                    total.ToString("N2"),
                                    ""
                                );

                                if (stock <= 0)
                                    dgvItems.Rows[rIdx].Cells["colStock"].Style.ForeColor = Color.FromArgb(220, 38, 38);
                            }

                            RecalculateTotals();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void OpenSmartSuggestionsDialog()
        {
            int? supId = null;
            string supName = "";
            if (cboSupplier.SelectedItem is POComboItem ci && ci.ID > 0)
            {
                supId = ci.ID;
                supName = ci.Text;
            }

            using (var dlg = new FrmSmartPurchaseOrderSuggestions(supId, supName))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedItems != null && dlg.SelectedItems.Count > 0)
                {
                    foreach (var sItem in dlg.SelectedItems)
                    {
                        // فحص هل الصنف موجود بالفعل في الجدول
                        bool exists = false;
                        foreach (DataGridViewRow r in dgvItems.Rows)
                        {
                            if (Convert.ToInt32(r.Cells["colPID"].Value) == sItem.ProductID)
                            {
                                // زيادة الكمية
                                decimal oldQty = Convert.ToDecimal(r.Cells["colQty"].Value);
                                decimal newQty = oldQty + sItem.SuggestedQuantity;
                                r.Cells["colQty"].Value = newQty.ToString("N2");
                                decimal prc = Convert.ToDecimal(r.Cells["colPrice"].Value);
                                r.Cells["colTotal"].Value = (newQty * prc).ToString("N2");
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            decimal total = Math.Round(sItem.SuggestedQuantity * sItem.LastPurchasePrice, 2);
                            int rIdx = dgvItems.Rows.Add(
                                dgvItems.Rows.Count + 1,
                                sItem.ProductID,
                                sItem.ProductCode,
                                sItem.ProductName,
                                sItem.CategoryName,
                                sItem.UnitName,
                                sItem.CurrentStock.ToString("N2"),
                                sItem.SuggestedQuantity.ToString("N2"),
                                sItem.LastPurchasePrice.ToString("N2"),
                                total.ToString("N2"),
                                sItem.SuggestionReason
                            );

                            if (sItem.CurrentStock <= 0)
                                dgvItems.Rows[rIdx].Cells["colStock"].Style.ForeColor = Color.FromArgb(220, 38, 38);
                        }
                    }

                    RecalculateTotals();
                    MessageBox.Show($"✅ تمت إضافة ({dlg.SelectedItems.Count}) صنف مقترح إلى أمر الشراء بنجاح!", "تمت الإضافة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void TxtQuickSearch_TextChanged(object sender, EventArgs e)
        {
            string q = txtQuickSearch.Text?.Trim() ?? "";
            if (q.Length < 1)
            {
                lstProductLookup.Visible = false;
                return;
            }

            try
            {
                var dt = DbHelper.Query(@"
                    SELECT TOP 10 
                        p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.PurchasePrice,
                        COALESCE(stk.TotalStock, p.Quantity, 0) AS CurrentStock
                    FROM Products p
                    OUTER APPLY (
                        SELECT SUM(ps.Quantity) AS TotalStock FROM ProductStock ps WHERE ps.ProductID = p.ProductID
                    ) stk
                    WHERE p.IsActive = 1 AND (p.ProductName LIKE @q OR p.ProductCode LIKE @q)
                    ORDER BY p.ProductName ASC",
                    DbHelper.P("@q", "%" + q + "%"));

                lstProductLookup.Items.Clear();
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        lstProductLookup.Items.Add(new POComboItem(
                            Convert.ToInt32(r["ProductID"]),
                            $"{r["ProductName"]} ({r["ProductCode"]})",
                            "",
                            r
                        ));
                    }

                    Point pt = txtQuickSearch.PointToScreen(Point.Empty);
                    Point parentPt = this.PointToScreen(Point.Empty);
                    lstProductLookup.Location = new Point(pt.X - parentPt.X, pt.Y - parentPt.Y + txtQuickSearch.Height + 2);
                    lstProductLookup.Visible = true;
                    lstProductLookup.BringToFront();
                }
                else
                {
                    lstProductLookup.Visible = false;
                }
            }
            catch
            {
                lstProductLookup.Visible = false;
            }
        }

        private void TxtQuickSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                OpenProductSearch();
                e.Handled = true;
                return;
            }
            else if (e.KeyCode == Keys.Down && lstProductLookup.Visible && lstProductLookup.Items.Count > 0)
            {
                lstProductLookup.Focus();
                lstProductLookup.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (lstProductLookup.Visible && lstProductLookup.Items.Count > 0)
                {
                    SelectProductFromLookup();
                }
                else
                {
                    AddQuickProductToGrid();
                }
            }
        }

        private void SelectProductFromLookup()
        {
            if (lstProductLookup.SelectedItem is POComboItem ci && ci.Tag is DataRow r)
            {
                _selectedQuickPID = ci.ID;
                _selectedQuickCode = r["ProductCode"]?.ToString() ?? "";
                _selectedQuickName = r["ProductName"]?.ToString() ?? "";
                _selectedQuickUnit = r["Unit"]?.ToString() ?? "قطعة";
                _selectedQuickStock = Convert.ToDecimal(r["CurrentStock"]);

                txtQuickSearch.Text = _selectedQuickName;
                decimal buyPrice = Convert.ToDecimal(r["PurchasePrice"]);
                txtQuickPrice.Text = buyPrice.ToString("N2");
                nudQuickQty.Focus();
                nudQuickQty.Select(0, nudQuickQty.Text.Length);
            }
            lstProductLookup.Visible = false;
        }

        private void AddQuickProductToGrid()
        {
            if (!_selectedQuickPID.HasValue || _selectedQuickPID.Value <= 0)
            {
                MessageBox.Show("يرجى اختيار صنف صحيح من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtQuickSearch.Focus();
                return;
            }

            decimal qty = nudQuickQty.Value;
            if (qty <= 0) qty = 1;

            decimal price = 0m;
            decimal.TryParse(txtQuickPrice.Text.Trim(), out price);

            // فحص التكرار
            foreach (DataGridViewRow r in dgvItems.Rows)
            {
                if (Convert.ToInt32(r.Cells["colPID"].Value) == _selectedQuickPID.Value)
                {
                    decimal oldQty = Convert.ToDecimal(r.Cells["colQty"].Value);
                    decimal newQty = oldQty + qty;
                    r.Cells["colQty"].Value = newQty.ToString("N2");
                    r.Cells["colPrice"].Value = price.ToString("N2");
                    r.Cells["colTotal"].Value = (newQty * price).ToString("N2");

                    ResetQuickSearch();
                    RecalculateTotals();
                    return;
                }
            }

            decimal total = Math.Round(qty * price, 2);
            int rIdx = dgvItems.Rows.Add(
                dgvItems.Rows.Count + 1,
                _selectedQuickPID.Value,
                _selectedQuickCode,
                _selectedQuickName,
                "عام",
                _selectedQuickUnit,
                _selectedQuickStock.ToString("N2"),
                qty.ToString("N2"),
                price.ToString("N2"),
                total.ToString("N2"),
                ""
            );

            if (_selectedQuickStock <= 0)
                dgvItems.Rows[rIdx].Cells["colStock"].Style.ForeColor = Color.FromArgb(220, 38, 38);

            ResetQuickSearch();
            RecalculateTotals();
        }

        private void ResetQuickSearch()
        {
            _selectedQuickPID = null;
            _selectedQuickCode = "";
            _selectedQuickName = "";
            _selectedQuickUnit = "قطعة";
            _selectedQuickStock = 0;
            txtQuickSearch.Text = "";
            nudQuickQty.Value = 1;
            txtQuickPrice.Text = "0.00";
            txtQuickSearch.Focus();
        }

        private void DgvItems_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvItems.Rows[e.RowIndex];

            decimal qty = 1;
            if (decimal.TryParse(row.Cells["colQty"].Value?.ToString(), out decimal q)) qty = q;
            if (qty <= 0) qty = 1;
            row.Cells["colQty"].Value = qty.ToString("N2");

            decimal price = 0;
            if (decimal.TryParse(row.Cells["colPrice"].Value?.ToString(), out decimal p)) price = p;
            row.Cells["colPrice"].Value = price.ToString("N2");

            decimal total = Math.Round(qty * price, 2);
            row.Cells["colTotal"].Value = total.ToString("N2");

            RecalculateTotals();
        }

        private void DgvItems_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvItems.Columns["colDelete"].Index)
            {
                dgvItems.Rows.RemoveAt(e.RowIndex);
                // إعادة ترقيم الصفوف
                for (int i = 0; i < dgvItems.Rows.Count; i++)
                {
                    dgvItems.Rows[i].Cells["colIdx"].Value = i + 1;
                }
                RecalculateTotals();
            }
        }

        private void RecalculateTotals()
        {
            int count = dgvItems.Rows.Count;
            decimal totalQty = 0;
            decimal totalCost = 0;

            foreach (DataGridViewRow r in dgvItems.Rows)
            {
                decimal q = 0;
                decimal.TryParse(r.Cells["colQty"].Value?.ToString(), out q);
                totalQty += q;

                decimal tot = 0;
                decimal.TryParse(r.Cells["colTotal"].Value?.ToString(), out tot);
                totalCost += tot;
            }

            lblItemsCountVal.Text = count.ToString("N0");
            lblTotalQtyVal.Text = totalQty.ToString("N2");
            lblTotalCostVal.Text = totalCost.ToString("N2") + " ج";
        }

        // ══════════════════════════════════════════════
        //  حفظ وإرسال ومشاركة أمر الشراء
        // ══════════════════════════════════════════════

        private bool ValidateAndPrepareOrder(out PurchaseOrderDTO order)
        {
            order = null;
            if (!(cboSupplier.SelectedItem is POComboItem ci) || ci.ID <= 0)
            {
                MessageBox.Show("يرجى اختيار المورد أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboSupplier.Focus();
                return false;
            }

            if (dgvItems.Rows.Count == 0)
            {
                MessageBox.Show("لا يمكن حفظ أمر شراء فارغ. يرجى إضافة صنف واحد على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            order = new PurchaseOrderDTO
            {
                OrderID = _orderID,
                OrderCode = txtOrderCode.Text.Trim(),
                OrderDate = dtpOrderDate.Value,
                SupplierID = ci.ID,
                SupplierName = ci.Text,
                SupplierPhone = txtSupplierPhone.Text.Trim(),
                ExpectedDeliveryDate = dtpDeliveryDate.Value,
                Status = _order != null && !string.IsNullOrEmpty(_order.Status) ? _order.Status : "معلقة",
                Notes = txtNotes.Text.Trim(),
                Items = new List<PurchaseOrderItemDTO>()
            };

            foreach (DataGridViewRow r in dgvItems.Rows)
            {
                decimal qty = Convert.ToDecimal(r.Cells["colQty"].Value ?? 1);
                decimal price = Convert.ToDecimal(r.Cells["colPrice"].Value ?? 0);
                decimal stock = Convert.ToDecimal(r.Cells["colStock"].Value ?? 0);

                order.Items.Add(new PurchaseOrderItemDTO
                {
                    ProductID = Convert.ToInt32(r.Cells["colPID"].Value),
                    ProductCode = r.Cells["colCode"].Value?.ToString() ?? "",
                    ProductName = r.Cells["colName"].Value?.ToString() ?? "",
                    CategoryName = r.Cells["colCat"].Value?.ToString() ?? "عام",
                    UnitName = r.Cells["colUnit"].Value?.ToString() ?? "قطعة",
                    Quantity = qty,
                    EstimatedUnitPrice = price,
                    CurrentStockAtOrder = stock,
                    Notes = r.Cells["colNotes"].Value?.ToString() ?? ""
                });
            }

            return true;
        }

        private bool SaveOrderToDb()
        {
            if (!ValidateAndPrepareOrder(out PurchaseOrderDTO order)) return false;

            try
            {
                int savedId = PurchaseOrderDAL.SaveOrder(order);
                _orderID = savedId;
                _order = PurchaseOrderDAL.GetOrder(_orderID);
                txtOrderCode.Text = _order.OrderCode;

                MessageBox.Show($"✅ تم حفظ أمر الشراء رقم [{_order.OrderCode}] بنجاح بحالة [معلقة]!\n\nيمكنك الآن إرسال الطلبية للمورد عبر الواتساب أو استدعاؤها لاحقاً في فاتورة المشتريات.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء حفظ أمر الشراء:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void SendWhatsAppOrder()
        {
            if (!ValidateAndPrepareOrder(out PurchaseOrderDTO order)) return;

            string phone = txtSupplierPhone.Text.Trim().Replace(" ", "").Replace("-", "");
            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("يرجى إدخال رقم هاتف المورد أولاً لتتمكن من إرسال الطلبية له عبر واتساب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtSupplierPhone.Focus();
                return;
            }

            // تنظيف كود الدولة لمصر إن كان بادئاً بـ 01
            if (phone.StartsWith("01")) phone = "20" + phone.Substring(1);

            var sb = new StringBuilder();
            sb.AppendLine($"📋 *طلب توريد بضاعة / أمر شراء جديد*");
            sb.AppendLine($"🏢 *الجهة:* {AppConfig.CompanyName}");
            sb.AppendLine($"🔖 *رقم الطلبية:* {order.OrderCode}");
            sb.AppendLine($"📅 *التاريخ:* {order.OrderDate:yyyy/MM/dd}");
            sb.AppendLine($"👤 *المطلوب من الأستاذ / الشركة:* {order.SupplierName}");
            if (dtpDeliveryDate.Value > DateTime.Today)
                sb.AppendLine($"⏳ *موعد التوريد المتوقع:* {dtpDeliveryDate.Value:yyyy/MM/dd}");
            sb.AppendLine("────────────────────");
            sb.AppendLine("📦 *بيان الأصناف والكميات المطلوبة:*");

            int idx = 1;
            foreach (var itm in order.Items)
            {
                string pricePart = itm.EstimatedUnitPrice > 0 ? $" | السعر: {itm.EstimatedUnitPrice:N2} ج" : "";
                sb.AppendLine($"{idx}. *{itm.ProductName}*");
                sb.AppendLine($"   الكمية: {itm.Quantity:N2} {itm.UnitName}{pricePart}");
                idx++;
            }

            sb.AppendLine("────────────────────");
            sb.AppendLine($"📊 *إجمالي الأصناف:* {order.Items.Count} صنف");
            sb.AppendLine($"📦 *إجمالي الكمية:* {order.Items.Sum(x => x.Quantity):N2}");
            if (order.Items.Any(x => x.EstimatedUnitPrice > 0))
                sb.AppendLine($"💰 *إجمالي التكلفة التقديرية:* {order.Items.Sum(x => x.TotalPrice):N2} ج");

            if (!string.IsNullOrWhiteSpace(order.Notes))
                sb.AppendLine($"📝 *ملاحظات خاصة:* {order.Notes}");

            sb.AppendLine("────────────────────");
            sb.AppendLine("يرجى تأكيد استلام الطلبية وموعد الشحن. شاكرين حسن تعاونكم الدائم.");

            string url = "https://api.whatsapp.com/send?phone=" + phone + "&text=" + Uri.EscapeDataString(sb.ToString());
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل فتح واتساب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportOrderToExcel()
        {
            if (dgvItems.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف لتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "تصدير أمر الشراء إلى Excel";
                dlg.FileName = $"PurchaseOrder_{txtOrderCode.Text.Trim()}_{DateTime.Now:yyyyMMdd}.xls";
                dlg.Filter = "Excel Files (*.xls)|*.xls|All Files (*.*)|*.*";
                dlg.DefaultExt = "xls";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        FrmReports.ExportDataGridViewToXls(dgvItems, dlg.FileName, $"أمر شراء {txtOrderCode.Text}", AppConfig.CompanyName);

                        var res = MessageBox.Show("✅ تم تصدير أمر الشراء إلى إكسيل بنجاح!\nهل تريد فتح الملف الآن؟", "تم التصدير", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ فشل التصدير:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void PrintPurchaseOrder()
        {
            if (dgvItems.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pd = new PrintDocument();
                pd.DefaultPageSettings.Landscape = false;
                pd.DefaultPageSettings.Margins = new Margins(30, 30, 40, 40);
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);

                int rowIndex = 0;
                int pageNum = 0;

                pd.PrintPage += (s, ev) =>
                {
                    pageNum++;
                    RenderOrderPage(ev.Graphics, ev.MarginBounds, ref rowIndex, pageNum);
                    ev.HasMorePages = (rowIndex < dgvItems.Rows.Count);
                };

                using (var dlg = new PrintPreviewDialog { Document = pd, Width = 1000, Height = 740, StartPosition = FormStartPosition.CenterScreen })
                {
                    ((Form)dlg).Text = "معاينة طباعة أمر الشراء للمورد";
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء إعداد الطباعة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderOrderPage(Graphics g, Rectangle bounds, ref int rowIndex, int pageNum)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
            using (var fSub = new Font("Segoe UI", 9.5f))
            using (var fHeader = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fRow = new Font("Segoe UI", 9f))
            using (var pBorder = new Pen(Color.FromArgb(203, 213, 225), 1))
            using (var bHeader = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (var bText = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                int y = bounds.Top;

                if (pageNum == 1)
                {
                    // ترويسة المؤسسة
                    g.DrawString(AppConfig.CompanyName, fTitle, bText, bounds.Right, y, new StringFormat { Alignment = StringAlignment.Far });
                    y += 30;

                    g.DrawString($"طلب توريد بضاعة / أمر شراء رسمي - {txtOrderCode.Text}", new Font("Segoe UI", 12f, FontStyle.Bold), Brushes.DarkSlateBlue, bounds.Right, y, new StringFormat { Alignment = StringAlignment.Far });
                    y += 26;

                    string supName = cboSupplier.Text;
                    string supPhone = txtSupplierPhone.Text;
                    g.DrawString($"المورد: {supName}  |  هاتف المورد: {supPhone}  |  التاريخ: {dtpOrderDate.Value:yyyy/MM/dd}", fSub, Brushes.Black, bounds.Right, y, new StringFormat { Alignment = StringAlignment.Far });
                    y += 24;

                    if (!string.IsNullOrWhiteSpace(txtNotes.Text))
                    {
                        g.DrawString($"ملاحظات: {txtNotes.Text}", fSub, Brushes.DarkRed, bounds.Right, y, new StringFormat { Alignment = StringAlignment.Far });
                        y += 22;
                    }

                    y += 8;
                }

                // ترويسة الجدول
                int[] colWidths = { 45, 110, 260, 75, 95, 95, 90 };
                string[] headers = { "#", "كود الصنف", "اسم الصنف", "الوحدة", "الكمية المطلوبة", "السعر المتوقع", "الإجمالي" };

                int x = bounds.Right;
                int rowH = 26;

                for (int i = 0; i < headers.Length; i++)
                {
                    int w = colWidths[i];
                    Rectangle r = new Rectangle(x - w, y, w, rowH);
                    g.FillRectangle(bHeader, r);
                    g.DrawRectangle(pBorder, r);
                    g.DrawString(headers[i], fHeader, bText, r, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    x -= w;
                }
                y += rowH;

                // صفوف الأصناف
                while (rowIndex < dgvItems.Rows.Count)
                {
                    if (y + rowH > bounds.Bottom - 60) break;

                    var r = dgvItems.Rows[rowIndex];
                    x = bounds.Right;

                    string idx = (rowIndex + 1).ToString();
                    string code = r.Cells["colCode"].Value?.ToString() ?? "";
                    string name = r.Cells["colName"].Value?.ToString() ?? "";
                    string unit = r.Cells["colUnit"].Value?.ToString() ?? "";
                    string qty = r.Cells["colQty"].Value?.ToString() ?? "";
                    string prc = r.Cells["colPrice"].Value?.ToString() ?? "";
                    string tot = r.Cells["colTotal"].Value?.ToString() ?? "";

                    string[] vals = { idx, code, name, unit, qty, prc, tot };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        int w = colWidths[i];
                        Rectangle cellRect = new Rectangle(x - w, y, w, rowH);
                        if (rowIndex % 2 == 1)
                            g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), cellRect);
                        g.DrawRectangle(pBorder, cellRect);
                        g.DrawString(vals[i], fRow, bText, cellRect, new StringFormat { Alignment = (i == 2 ? StringAlignment.Far : StringAlignment.Center), LineAlignment = StringAlignment.Center });
                        x -= w;
                    }

                    y += rowH;
                    rowIndex++;
                }

                // ذيل الصفحة وتوقيعات الاستلام
                if (rowIndex >= dgvItems.Rows.Count)
                {
                    y += 15;
                    g.DrawString($"إجمالي عدد الأصناف: {lblItemsCountVal.Text} صنف  |  إجمالي الكميات: {lblTotalQtyVal.Text}  |  إجمالي القيمة التقديرية: {lblTotalCostVal.Text}", fHeader, Brushes.Black, bounds.Right, y, new StringFormat { Alignment = StringAlignment.Far });
                    y += 35;

                    g.DrawString("توقيع مسؤول المشتريات: .......................................             توقيع وختم المورد / المندوب عند الاستلام: .......................................", fSub, Brushes.Gray, bounds.Right, y, new StringFormat { Alignment = StringAlignment.Far });
                }

                g.DrawString($"صفحة {pageNum}", fSub, Brushes.Gray, bounds.Left, bounds.Bottom - 20, new StringFormat { Alignment = StringAlignment.Near });
            }
        }

        private void ConvertDirectlyToPurchaseInvoice()
        {
            if (!ValidateAndPrepareOrder(out PurchaseOrderDTO order)) return;

            // حفظ الطلبية أولاً للتأكد من تسجيلها
            if (_orderID <= 0)
            {
                _orderID = PurchaseOrderDAL.SaveOrder(order);
                _order = PurchaseOrderDAL.GetOrder(_orderID);
            }

            var frmPur = new FrmPurchase();
            frmPur.LoadFromPurchaseOrder(_order);

            if (this.ParentForm is FrmMain main)
            {
                main.NavigateTo(frmPur);
            }
            else
            {
                frmPur.Show();
            }
        }

        private void OpenOrdersArchive()
        {
            var frmList = new FrmPurchaseOrdersList();
            if (this.ParentForm is FrmMain main)
            {
                main.NavigateTo(frmList);
            }
            else
            {
                frmList.ShowDialog(this);
            }
        }

        private Panel MakeInfoGroup(string label, Control ctrl)
        {
            var pnl = new Panel { AutoSize = false, Width = ctrl.Width, Height = 48, Margin = new Padding(3, 2, 3, 2) };
            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Width = ctrl.Width,
                Height = 16,
                Location = new Point(0, 0),
                ForeColor = Theme.TextGray,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            ctrl.Location = new Point(0, 18);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(ctrl);
            return pnl;
        }

        private static void EnableDoubleBuffer(DataGridView dgv)
        {
            try
            {
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                    null, dgv, new object[] { true });
            }
            catch { }
        }

        private class POComboItem
        {
            public int ID { get; set; }
            public string Text { get; set; }
            public string Phone { get; set; }
            public object Tag { get; set; }

            public POComboItem(int id, string text, string phone = "", object tag = null)
            {
                ID = id;
                Text = text;
                Phone = phone;
                Tag = tag;
            }

            public override string ToString() => Text;
        }
    }
}

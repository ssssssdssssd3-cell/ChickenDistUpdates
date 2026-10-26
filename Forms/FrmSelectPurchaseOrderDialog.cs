using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة اختيار واستدعاء طلبية مشتريات معلقة لتعبئتها مباشرة داخل فاتورة المشتريات
    /// </summary>
    public class FrmSelectPurchaseOrderDialog : Form
    {
        public PurchaseOrderDTO SelectedOrder { get; private set; }

        private DataGridView dgvOrders;
        private DataGridView dgvItemsPreview;
        private TextBox txtSearch;
        private CheckBox chkAllSuppliers;
        private Button btnSelect, btnCancel;
        private Label lblPreviewTitle;

        private int? _targetSupplierID;
        private DataTable _dtOrders;

        public FrmSelectPurchaseOrderDialog(int? supplierID = null)
        {
            _targetSupplierID = supplierID;
            InitUI();
            LoadOrders();
        }

        private void InitUI()
        {
            this.Text = "استدعاء طلبية مشتريات معلقة إلى فاتورة الشراء";
            this.Size = new Size(1000, 620);
            this.MinimumSize = new Size(880, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. ترويسة الشاشة
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "استدعاء وتحويل طلبية مشتريات معلقة",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 24
            };

            var lblSub = new Label
            {
                Text = "اختر الطلبية المعلقة ليتم إدراج أصنافها وكمياتها وأسعارها تلقائياً داخل فاتورة المشتريات للتأكيد والحفظ",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // 2. شريط البحث والفلترة
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 6, 12, 6),
                RightToLeft = RightToLeft.Yes
            };

            var lblSearch = new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(2, 6, 2, 0) };
            txtSearch = new TextBox { Width = 220, Height = 26, Margin = new Padding(2, 4, 15, 4) };
            txtSearch.TextChanged += (s, e) => FilterOrders();

            chkAllSuppliers = new CheckBox
            {
                Text = "عرض طلبيات جميع الموردين",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Checked = !_targetSupplierID.HasValue || _targetSupplierID.Value <= 0,
                Margin = new Padding(2, 6, 2, 0)
            };
            chkAllSuppliers.CheckedChanged += (s, e) => LoadOrders();

            pnlFilter.Controls.Add(lblSearch);
            pnlFilter.Controls.Add(txtSearch);
            pnlFilter.Controls.Add(chkAllSuppliers);

            // 3. شريط الأزرار السفلي
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnSelect = Theme.MakeButton("استدعاء وتعبئة الفاتورة", 0, 0, 190, 36, Color.FromArgb(5, 150, 105));
            btnSelect.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSelect.Dock = DockStyle.Left;
            btnSelect.Click += (s, e) => ConfirmSelection();

            btnCancel = Theme.MakeButton("إلغاء", 0, 0, 90, 36, Color.FromArgb(100, 116, 139));
            btnCancel.Dock = DockStyle.Right;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlBottom.Controls.Add(btnSelect);
            pnlBottom.Controls.Add(btnCancel);

            // 4. منطقة العرض المقسمة (جدول الطلبيات بالأعلى + معاينة البنود بالأسفل)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 240,
                BackColor = Theme.BorderColor
            };

            // جدول الطلبيات
            dgvOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                RowTemplate = { Height = 32 }
            };
            SetupOrdersGrid();
            dgvOrders.SelectionChanged += (s, e) => ShowOrderItemsPreview();
            dgvOrders.CellDoubleClick += (s, e) => ConfirmSelection();

            // جدول معاينة البنود
            var pnlPreview = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(8, 4, 8, 4) };
            lblPreviewTitle = new Label
            {
                Text = "الأصناف المتضمنة في الطلبية المحددة:",
                Dock = DockStyle.Top,
                Height = 24,
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };

            dgvItemsPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                RowTemplate = { Height = 28 }
            };
            SetupPreviewGrid();

            pnlPreview.Controls.Add(dgvItemsPreview);
            pnlPreview.Controls.Add(lblPreviewTitle);

            split.Panel1.Controls.Add(dgvOrders);
            split.Panel2.Controls.Add(pnlPreview);

            this.Controls.Add(split);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlHeader);
        }

        private void SetupOrdersGrid()
        {
            dgvOrders.Columns.Clear();
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OrderID", HeaderText = "#", Width = 55, Visible = false });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OrderCode", HeaderText = "كود الطلبية", Width = 135, DefaultCellStyle = { Font = Theme.FontBold, Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OrderDate", HeaderText = "تاريخ الطلب", Width = 140, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "yyyy/MM/dd hh:mm tt" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "المورد", Width = 180 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalItemsCount", HeaderText = "عدد الأصناف", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalQuantity", HeaderText = "إجمالي الكمية", Width = 105, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalEstimatedCost", HeaderText = "التكلفة التقديرية", Width = 125, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2", Font = Theme.FontBold } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "الحالة", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(180, 83, 9) } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Notes", HeaderText = "الملاحظات", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        }

        private void SetupPreviewGrid()
        {
            dgvItemsPreview.Columns.Clear();
            dgvItemsPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvItemsPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", Width = 220, DefaultCellStyle = { Font = Theme.FontBold } });
            dgvItemsPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", Width = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvItemsPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المطلوبة", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = Theme.FontBold } });
            dgvItemsPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstimatedUnitPrice", HeaderText = "سعر الشراء المتوقع", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2" } });
            dgvItemsPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "الإجمالي", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2", Font = Theme.FontBold } });
            dgvItemsPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        }

        private void LoadOrders()
        {
            int? supId = chkAllSuppliers.Checked ? null : _targetSupplierID;
            _dtOrders = PurchaseOrderDAL.GetPendingOrders(supId);
            FilterOrders();
        }

        private void FilterOrders()
        {
            if (_dtOrders == null) return;
            string search = txtSearch.Text?.Trim().Replace("'", "''") ?? "";
            var dv = _dtOrders.DefaultView;
            if (!string.IsNullOrEmpty(search))
            {
                dv.RowFilter = $"OrderCode LIKE '%{search}%' OR SupplierName LIKE '%{search}%' OR Notes LIKE '%{search}%'";
            }
            else
            {
                dv.RowFilter = "";
            }
            dgvOrders.DataSource = dv;
            ShowOrderItemsPreview();
        }

        private void ShowOrderItemsPreview()
        {
            dgvItemsPreview.Rows.Clear();
            if (dgvOrders.CurrentRow == null)
            {
                lblPreviewTitle.Text = "لم يتم تحديد أي طلبية.";
                return;
            }

            var drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int orderId = Convert.ToInt32(drv["OrderID"]);
            string orderCode = drv["OrderCode"]?.ToString() ?? "";
            string supName = drv["SupplierName"]?.ToString() ?? "";

            lblPreviewTitle.Text = $"أصناف الطلبية رقم [{orderCode}] للمورد [{supName}]:";

            var items = PurchaseOrderDAL.GetOrderItems(orderId);
            foreach (var item in items)
            {
                dgvItemsPreview.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.UnitName,
                    item.Quantity.ToString("N2"),
                    item.EstimatedUnitPrice.ToString("N2"),
                    item.TotalPrice.ToString("N2"),
                    item.Notes
                );
            }
        }

        private void ConfirmSelection()
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("يرجى اختيار الطلبية أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int orderId = Convert.ToInt32(drv["OrderID"]);
            SelectedOrder = PurchaseOrderDAL.GetOrder(orderId);

            if (SelectedOrder == null || SelectedOrder.Items == null || SelectedOrder.Items.Count == 0)
            {
                MessageBox.Show("هذه الطلبية لا تحتوي على أي أصناف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
